using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace HibernationWatch;

public class TinyHibernateClient : IDisposable
{
    private readonly byte[] _secretKey;
    private readonly TimeSpan _requestTimeout;
    private readonly UdpClient _udp;
    private readonly IPEndPoint _endpoint;

    public TinyHibernateClient(string serverIp, int serverPort, byte[] secretKey, TimeSpan requestTimeout)
    {
        _secretKey = secretKey;
        _requestTimeout = requestTimeout;
        _endpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        _udp = new UdpClient();
    }
    
    public async Task SendAsync(byte action, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Sending {action} at {DateTimeOffset.UtcNow}");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var datagram = new byte[_secretKey.Length+1];
        Array.Copy(_secretKey, datagram, _secretKey.Length);
        datagram[^1] = action; 
        await _udp.SendAsync(datagram, _endpoint);

        Console.WriteLine($"Sent {action} at {DateTimeOffset.UtcNow} in {stopwatch.ElapsedMilliseconds}ms");
    }

    public void Dispose()
    {
        _udp.Dispose();
        GC.SuppressFinalize(this);
    }
}