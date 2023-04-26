using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RSASignatureServer;

internal class Server
{
    private readonly TcpListener _listener;
    private readonly List<TcpClient> _clients = new List<TcpClient>();

    private Server(IPAddress ipAddress, int port)
    {
        _listener = new TcpListener(ipAddress, port);
    }
    
    private static void Main()
    {
        var ipHostInfo = Dns.GetHostEntry("127.0.0.1");
        var ipAddress = ipHostInfo.AddressList[0];
        var server = new Server(ipAddress, 11000);
        server.StartAsync().Wait();
    }

    private async Task StartAsync()
    {
        _listener.Start();

        Console.WriteLine($"Server started listening on {_listener.LocalEndpoint}");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _clients.Add(client);
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");

            Task.Run(async () =>
            {
                var stream = client.GetStream();

                while (true)
                {
                    try
                    {
                        var bytes = new byte[1024];
                        var receive = await stream.ReadAsync(bytes, 0, bytes.Length);
                        if (receive == 0) break;

                        var data = Encoding.ASCII.GetString(bytes, 0, receive);
                        Console.WriteLine($"Received: {data}");
                        
                        foreach (var c in _clients)
                        {
                            if (c != client)
                            {
                                await c.GetStream().WriteAsync(bytes, 0, receive);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        break;
                    }
                }

                _clients.Remove(client);
                Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
            });
        }
    }
}