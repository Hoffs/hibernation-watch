using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace HibernationWatch;

public class TinyHibernateServer : IDisposable
{
    private readonly IPEndPoint _endpoint;
    private readonly UdpClient _udp;
    private readonly TimeSpan _timeout;
    private readonly byte[] _secretKey;
    private readonly GoogleAssistant _assistant;

    public TinyHibernateServer(int port, byte[] secretkey, GoogleAssistant assistant, TimeSpan requestTimeout)
    {
        _endpoint = new IPEndPoint(IPAddress.Any, port);
        _secretKey = secretkey;
        _assistant = assistant;
        _timeout = requestTimeout;

        _udp = new UdpClient(_endpoint);
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await _udp.ReceiveAsync(cancellationToken);

            await HandleRequestAsync(result.Buffer, cancellationToken);
        }
    }

    private async Task HandleRequestAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Read finished with {buffer.Length} bytes.");
        var keySlice = buffer[0..(_secretKey.Length)];
        if (!keySlice.SequenceEqual(_secretKey))
        {
            Console.WriteLine("Received message with incorrect key.");
            return;
        }

        var dataSlice = buffer[_secretKey.Length..];
        var action = dataSlice[0];

        Console.WriteLine($"Received action {action:x2}.");

        using var ctsAssist = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ctsAssist.CancelAfter(_timeout);

        if (action == 1)
        {
            await _assistant.ExecuteAsync("turn on technics-amp", ctsAssist.Token);
        }
        else if (action == 2)
        {
            await _assistant.ExecuteAsync("turn off technics-amp", ctsAssist.Token);
        }
    }

    public void Dispose()
    {
        _udp.Dispose();
        GC.SuppressFinalize(this);
    }
}