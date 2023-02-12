using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Google.Assistant.Embedded.V1Alpha2;
using Grpc.Auth;
using Grpc.Core;
using Grpc.Net.Client;
using static Google.Assistant.Embedded.V1Alpha2.EmbeddedAssistant;

namespace HibernationWatch;

public class GoogleAssistant : IDisposable
{
    private const string ServiceUrl = "https://embeddedassistant.googleapis.com";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
    private static readonly string[] Scopes = new[] { "https://www.googleapis.com/auth/assistant-sdk-prototype" };
    private readonly GoogleClientSecrets _secrets;
    private readonly string _deviceModelId;
    private readonly bool _debug;
    private UserCredential? _oauth;
    private ChannelCredentials? _channelCredentials;
    private Task? _refreshTask;

    private CancellationTokenSource _cts = new CancellationTokenSource();

    public GoogleAssistant(GoogleClientSecrets secrets, string deviceModelId, bool debug)
    {
        _secrets = secrets;
        _deviceModelId = deviceModelId;
        _debug = debug;
    }

    public async Task InitAsync(CancellationToken cancellationToken)
    {
        _oauth = await GoogleWebAuthorizationBroker.AuthorizeAsync(_secrets.Secrets, Scopes, "user", cancellationToken, codeReceiver: new BasicPromptCodeReceiver());
        _channelCredentials = GoogleGrpcCredentials.ToChannelCredentials(_oauth);
        _refreshTask = Task.Run(RefreshToken);
    }

    public async Task ExecuteAsync(string query, CancellationToken cancellationToken)
    {
        var executedAt = DateTimeOffset.UtcNow;
        if (_channelCredentials is null)
        {
            throw new InvalidOperationException("Assistant must be initialized first.");
        }

        Console.WriteLine($"Assisting '{query}' at {executedAt:o}.");

        var stop = new Stopwatch();
        stop.Start();

        using var channel = GrpcChannel.ForAddress(ServiceUrl, new Grpc.Net.Client.GrpcChannelOptions() { Credentials = _channelCredentials });
        var client = new EmbeddedAssistantClient(channel);
        
        var request = BuildRequest(query);
        var call = client.Assist();
        await call.RequestStream.WriteAsync(request, CancellationToken.None); // cancellation not supported
        await call.RequestStream.CompleteAsync();
        
        if (_debug)
        {
            using var fs = File.Open($"{executedAt:yyyy_MM_dd__HH_mm_ss_ffffff}.mp3", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            await foreach (var response in call.ResponseStream.ReadAllAsync<AssistResponse>().WithCancellation(cancellationToken))
            {
                if (response.AudioOut is not null)
                {
                    await fs.WriteAsync(response.AudioOut.AudioData.Memory, cancellationToken);
                }
            }
        }
        else
        {
            var next = true;
            while (next)
            {
                next = await call.ResponseStream.MoveNext(cancellationToken);
            }
        }
        
        var status = call.GetStatus();
        Console.WriteLine($"Google Assistant Response: Status {status.StatusCode}, Detail {status.Detail}.");
        Console.WriteLine($"Took {stop.ElapsedMilliseconds}ms for '{query}'.");
    }

    private async Task RefreshToken()
    {
        var cancellationToken = _cts.Token;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_oauth is not null)
            {
                await _oauth.RefreshTokenAsync(cancellationToken);
            }

            await Task.Delay(RefreshInterval, cancellationToken);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }

    private AssistRequest BuildRequest(string query)
    {
        return new Google.Assistant.Embedded.V1Alpha2.AssistRequest()
        {
            Config = new Google.Assistant.Embedded.V1Alpha2.AssistConfig
            {
                TextQuery = query,
                AudioOutConfig = new AudioOutConfig()
                {
                    Encoding = AudioOutConfig.Types.Encoding.Mp3,
                    SampleRateHertz = 16000,
                    VolumePercentage = 100,
                },
                DeviceConfig = new DeviceConfig()
                {
                    DeviceId = "virtual-001",
                    DeviceModelId = _deviceModelId,
                },
            },
        };
    }
}