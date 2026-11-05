using System.Collections;
using yaml;

// Load an editable service configuration, including nested health and logging settings.
string input = File.ReadAllText(SampleFiles.InputPath("service-settings.yaml"));
var processor = new YamlProcessor();
var settings = processor.Load(input) as IDictionary
    ?? throw new InvalidOperationException("Expected a YAML mapping.");
Console.WriteLine("Read YAML service settings");
Console.WriteLine(
    $"Service: {settings["service"]}; replicas: {settings["replicas"]}; enabled: {settings["enabled"]}");

// Change one value while retaining the nested configuration mappings.
settings["replicas"] = 3;
string updated = processor.Dump(settings);
Console.WriteLine("\nUpdated YAML:");
Console.WriteLine(updated.TrimEnd());
var reopened = processor.Load(updated) as IDictionary
    ?? throw new InvalidOperationException("Expected a YAML mapping.");
if (Convert.ToInt32(reopened["replicas"]) != 3 || !Equals(reopened["service"], "cafe-inventory"))
{
    throw new InvalidOperationException("Updated YAML failed to round trip.");
}

Console.WriteLine("YAML round trip: updated values preserved.");
