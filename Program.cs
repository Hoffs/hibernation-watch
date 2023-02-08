using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Text;
using Google.Apis.Auth.OAuth2;
using HibernationWatch;

ThreadPool.SetMaxThreads(10, 10);
ThreadPool.SetMinThreads(3, 3);

var mode = args.Length >= 1 ? args[0] : "client";
if (mode is not "client" and not "server")
{
    throw new ArgumentException($"Invalid mode provided: {mode}.");
}

var ip = args.Length >= 2 ? args[1] : "127.0.0.1";
var port = args.Length >= 3 ? int.Parse(args[2]) : 19332;

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, evt) =>
{
    Console.WriteLine("Cancel pressed");
    cts.Cancel();
};


var secretKeyString = args.Length >= 4 ? args[3] : "secret_key";
var secretKey = Encoding.UTF8.GetBytes(secretKeyString);

Console.WriteLine($"SecretKey: {string.Join(' ', secretKey.Select(b => b.ToString("x2")))}");

if (mode is "server")
{
    Console.WriteLine("Starting server mode...");
    var clientCreds = await GoogleClientSecrets.FromFileAsync("client_secrets.json");
    ArgumentNullException.ThrowIfNull(clientCreds);

    var gAssistant = new GoogleAssistant(clientCreds);
    await gAssistant.InitAsync(cts.Token);

    var server = new TinyHibernateServer(secretKey, gAssistant, port, TimeSpan.FromSeconds(10));
    await server.StartAsync(cts.Token);
}

if (mode is "client")
{
    Console.WriteLine("Starting client mode...");
    var client = new TinyHibernateClient(ip, port, secretKey, TimeSpan.FromSeconds(1));

    Microsoft.Win32.SystemEvents.PowerModeChanged += async (_, args) =>
    {
        if (args.Mode == Microsoft.Win32.PowerModes.Suspend)
        {
            Console.WriteLine("Suspending");
            try
            {
                //await gAssistant.ExecuteAsync("turn off technics-amp", cts.Token);
                await client.SendAsync(1, cts.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.GetType} / {e.Message}");
                Console.WriteLine(e.StackTrace);
            }
        }

        if (args.Mode == Microsoft.Win32.PowerModes.Resume)
        {
            Console.WriteLine("Resuming");
            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested && !NetworkInterface.GetIsNetworkAvailable())
                {
                    Console.WriteLine("Waiting for internet...");
                    await Task.Delay(500);
                    continue;
                }

                try
                {
                    //await gAssistant.ExecuteAsync("turn on technics-amp", cts.Token);
                    await client.SendAsync(1, cts.Token);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.GetType} / {e.Message}");
                    Console.WriteLine(e.StackTrace);
                }
            });
        }
    };

    var messagePump = new Thread(() =>
    {
        while (!cts.IsCancellationRequested)
        {
            var result = WinApi.GetMessage(out var msg, IntPtr.Zero, 0, 0);
            if (result == 0) break;
            if (result == -1) throw new Win32Exception();
            WinApi.TranslateMessage(msg);
            WinApi.DispatchMessage(msg);
        }
    });

    messagePump.Start();


    await client.SendAsync(1, cts.Token);
    //await gAssistant.ExecuteAsync("turn on technics-amp", cts.Token);
    Console.WriteLine("Delaying");
    await Task.Delay(-1);
}
