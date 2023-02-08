using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace HibernationWatch;

public class TinyHibernateServer
{
    private readonly IPEndPoint _endpoint;
    private readonly TcpListener _listener;
    private readonly TimeSpan _timeout;
    private readonly byte[] _secretKey;
    private readonly GoogleAssistant _assistant;

    public TinyHibernateServer(byte[] secretkey, GoogleAssistant assistant, int port, TimeSpan requestTimeout)
    {
        _secretKey = secretkey;
        _assistant = assistant;
        _endpoint = new IPEndPoint(IPAddress.Any, port);
        _listener = new TcpListener(_endpoint);
        _timeout = requestTimeout;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Start(5);

        while (!cancellationToken.IsCancellationRequested)
        {
            using var handler = await _listener.AcceptTcpClientAsync(cancellationToken);
            await HandleRequestAsync(handler, cancellationToken);
        }
    }

    private async Task HandleRequestAsync(TcpClient handler, CancellationToken cancellationToken)
    {
        Console.WriteLine("Received connection.");
        await using var stream = handler.GetStream();
        var buffer = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            var read = 0;
            using (var ctsRequest = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                ctsRequest.CancelAfter(_timeout);
                var ctsToken = ctsRequest.Token;

                var endOfMessageReached = false;
                var offset = 0;
                while (!endOfMessageReached)
                {
                    var readBytes = await stream.ReadAsync(buffer, offset, buffer.Length - offset, ctsToken);
                    read += readBytes;
                    endOfMessageReached = readBytes == 0;
                }

                stream.WriteByte(1);
                handler.Close();
            }
            
            Console.WriteLine($"Read finished with {read} bytes.");
            var slice = buffer[0..read];
            Console.WriteLine(string.Join(' ', slice.Select(b => b.ToString("x2"))));
            var keySlice = slice[0..(_secretKey.Length)];
            if (!keySlice.SequenceEqual(_secretKey))
            {
                Console.WriteLine("Received message with incorrect key.");
                return;
            }
            
            var dataSlice = slice[_secretKey.Length..];
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
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}