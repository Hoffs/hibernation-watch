using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using HibernationWatch;

// Set CurrentDirectory as assembly location. Makes relative URL's to always be relative to executable (e.g. Windows Services use Sys32 as working dir)
Environment.CurrentDirectory = Path.GetDirectoryName(AppContext.BaseDirectory) ?? Environment.CurrentDirectory;

ThreadPool.SetMaxThreads(10, 10);
ThreadPool.SetMinThreads(3, 3);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, evt) =>
{
    Console.WriteLine("Cancel pressed");
    cts.Cancel();
};

Config? config;
using (var configStream = File.OpenRead("config.json"))
{
    config = await JsonSerializer.DeserializeAsync<Config>(configStream, cancellationToken: cts.Token);
}

ArgumentNullException.ThrowIfNull(config);

var secretKey = Encoding.UTF8.GetBytes(config.SecretKey);

if (config.Mode is "server")
{
    Console.WriteLine("Starting server mode...");
    var clientCreds = await GoogleClientSecrets.FromFileAsync("client_secrets.json");
    ArgumentNullException.ThrowIfNull(clientCreds);

    var gAssistant = new GoogleAssistant(clientCreds, config.DeviceModelId, config.Debug);
    await gAssistant.InitAsync(cts.Token);

    var server = new TinyHibernateServer(config.Port, secretKey, gAssistant, TimeSpan.FromSeconds(10));
    await server.StartAsync(cts.Token);
}
else if (config.Mode is "client")
{
    Console.WriteLine("Starting client mode...");
    var client = new TinyHibernateClient(config.Ip, config.Port, secretKey, TimeSpan.FromSeconds(1));

    Microsoft.Win32.SystemEvents.PowerModeChanged += async (_, args) =>
    {
        if (args.Mode == Microsoft.Win32.PowerModes.Suspend)
        {
            Console.WriteLine("Suspending...");

            try
            {
                await client.SendAsync(2, cts.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.GetType()} / {e.Message}");
                Console.WriteLine(e.StackTrace);
            }
        }

        if (args.Mode == Microsoft.Win32.PowerModes.Resume)
        {
            Console.WriteLine("Resuming...");

            try
            {
                // Only "ensure" on resuming, as we might send before network is re-established,
                // and we can also allow us to wait to make this more reliable.
                // On suspend we either have internet or not.
                await client.EnsureReachableAsync(10, TimeSpan.FromSeconds(2.5), cts.Token);
                await client.SendAsync(1, cts.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.GetType()} / {e.Message}");
                Console.WriteLine(e.StackTrace);
            }
        }
    };

    Console.WriteLine("Starting Windows message pump...");
    var messagePump = WinMessagePump.Start(cts.Token);
    
    

    Console.WriteLine("Sending resumed on startup...");
    await client.EnsureReachableAsync(5, TimeSpan.FromMilliseconds(1000), cts.Token);
    await client.SendAsync(1, cts.Token);
    Console.WriteLine("Delaying...");
    await Task.Delay(-1);
} else {
    throw new NotSupportedException($"Received unexpected mode ${config.Mode}.");
}
