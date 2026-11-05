using System.Text.Json;

namespace websocket.Tests.Autobahn;

internal static class AutobahnReportVerifier
{
    internal static int Verify(string reportDirectory)
    {
        if (!Directory.Exists(reportDirectory))
        {
            Console.Error.WriteLine(
                "Autobahn report directory does not exist: " + reportDirectory);
            return 1;
        }

        int resultCount = 0;
        List<string> failures = [];
        foreach (string file in Directory.EnumerateFiles(
            reportDirectory,
            "*.json",
            SearchOption.AllDirectories))
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file));
            Inspect(document.RootElement, file, "$", failures, ref resultCount);
        }

        if (resultCount == 0)
        {
            Console.Error.WriteLine(
                "No Autobahn behavior results were found under " + reportDirectory + ".");
            return 1;
        }

        if (failures.Count != 0)
        {
            Console.Error.WriteLine(
                "Autobahn strict verification failed for " + failures.Count +
                " of " + resultCount + " behavior results:");
            foreach (string failure in failures)
            {
                Console.Error.WriteLine(failure);
            }

            return 1;
        }

        Console.WriteLine(
            "Autobahn strict verification passed for " + resultCount +
            " behavior results.");
        return 0;
    }

    static void Inspect(
        JsonElement element,
        string file,
        string path,
        List<string> failures,
        ref int resultCount)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                string propertyPath = path + "." + property.Name;
                if (property.Name == "behavior")
                {
                    resultCount++;
                    string value = property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? ""
                        : property.Value.GetRawText();
                    if (value != "OK" && value != "INFORMATIONAL")
                    {
                        failures.Add(
                            file + ": " + propertyPath + " = " + value);
                    }
                }

                Inspect(property.Value, file, propertyPath, failures, ref resultCount);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                Inspect(
                    item,
                    file,
                    path + "[" + index + "]",
                    failures,
                    ref resultCount);
                index++;
            }
        }
    }
}
