using System.Diagnostics;
using System.Net.Sockets;

namespace HibernationWatch;

public class TinyHibernateClient : IDisposable
{
    private readonly byte[] _secretKey;
    private readonly TimeSpan _requestTimeout;
    private readonly string _ip;
    private readonly int _port;
    private readonly TcpClient _client;

    public TinyHibernateClient(string serverIp, int serverPort, byte[] secretKey, TimeSpan requestTimeout)
    {
        _secretKey = secretKey;
        _requestTimeout = requestTimeout;
        _ip = serverIp;
        _port = serverPort;
    }
    
    public async Task SendAsync(byte action, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Sending {action} at {DateTimeOffset.UtcNow}");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        using var client = new TcpClient(_ip, _port);
        client.NoDelay = true;
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

        using var stream = client.GetStream();
        await stream.WriteAsync(_secretKey);
        stream.WriteByte(action);
        stream.WriteByte(0);
        client.Close();

        Console.WriteLine($"Sent {action} at {DateTimeOffset.UtcNow} in {stopwatch.ElapsedMilliseconds}ms");
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}