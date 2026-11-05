namespace websocket.Tests.Autobahn;

internal static class Program
{
    static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 2;
            }

            if (args[0] == "client")
            {
                string url = args.Length > 1
                    ? args[1]
                    : "ws://127.0.0.1:9001";
                AutobahnClientAdapter.Run(url);
                return 0;
            }

            if (args[0] == "server")
            {
                string address = args.Length > 1 ? args[1] : "0.0.0.0";
                int port = args.Length > 2 ? int.Parse(args[2]) : 9001;
                AutobahnServerAdapter.Run(address, port);
                return 0;
            }

            if (args[0] == "verify" && args.Length == 2)
            {
                return AutobahnReportVerifier.Verify(args[1]);
            }

            PrintUsage();
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.Error.WriteLine(
            "Usage: websocket.Autobahn client [ws://host:port] | " +
            "server [address] [port] | verify <report-directory>");
    }
}
