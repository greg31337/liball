using System.Net;
using System.Net.Sockets;

namespace websocket.Tests.Autobahn;

internal static class AutobahnServerAdapter
{
    static bool stopping;

    internal static void Run(string address, int port)
    {
        IPAddress ipAddress = address == "0.0.0.0"
            ? IPAddress.Any
            : IPAddress.Parse(address);
        TcpListener listener = new TcpListener(ipAddress, port);
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            stopping = true;
            listener.Stop();
        };
        listener.Start();
        Console.WriteLine(
            "Autobahn server adapter listening on " + address + ":" + port + ".");

        while (!stopping)
        {
            try
            {
                using TcpClient client = listener.AcceptTcpClient();
                AutobahnOpenedConnection opened =
                    AutobahnHttp1Transport.AcceptServer(client);
                AutobahnConnectionDriver.RunEcho(opened);
            }
            catch (SocketException) when (stopping)
            {
                return;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Autobahn server session failed: " + exception);
            }
        }
    }
}
