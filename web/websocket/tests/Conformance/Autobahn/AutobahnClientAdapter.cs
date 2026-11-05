namespace websocket.Tests.Autobahn;

internal static class AutobahnClientAdapter
{
    const string Agent = "libs11-websocket";

    internal static void Run(string baseUrl)
    {
        string countUrl = Endpoint(baseUrl, "/getCaseCount");
        AutobahnOpenedConnection countConnection =
            AutobahnHttp1Transport.OpenClient(countUrl);
        int caseCount = int.Parse(
            AutobahnConnectionDriver.ReadOneTextMessage(countConnection));
        Console.WriteLine("Autobahn client will run " + caseCount + " cases.");

        int caseNumber = 1;
        while (caseNumber <= caseCount)
        {
            string path = "/runCase?case=" + caseNumber +
                "&agent=" + Uri.EscapeDataString(Agent);
            AutobahnOpenedConnection testConnection =
                AutobahnHttp1Transport.OpenClient(Endpoint(baseUrl, path));
            AutobahnConnectionDriver.RunEcho(testConnection);
            Console.WriteLine("Completed client case " + caseNumber + ".");
            caseNumber++;
        }

        string reportPath = "/updateReports?agent=" +
            Uri.EscapeDataString(Agent);
        AutobahnOpenedConnection reportConnection =
            AutobahnHttp1Transport.OpenClient(Endpoint(baseUrl, reportPath));
        AutobahnConnectionDriver.RunWithoutEcho(reportConnection);
    }

    static string Endpoint(string baseUrl, string path)
    {
        Uri root = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        return new Uri(root, path).AbsoluteUri;
    }
}
