using config;

var values = new ConfigValues();
var defaults = new ConfigMap();
defaults.SetPathDefault("app.name", values.String("Catalog"));
defaults.SetPathDefault("server.port", values.Number(5000));
defaults.SetPathDefault("features.search", values.Boolean(false));

// Pass an explicit environment map to make the example independent of the host.
var environment = new Dictionary<string, string>
{
    ["DEMO_server__port"] = "9090"
};

var builder = new ConfigBuilder()
    .AddDefaults(defaults)
    .AddString(
        """{"server":{"port":8080},"features":{"search":true}}""",
        ConfigFormat.Json,
        "app.json");
int jsonPort = builder.BuildOrThrow().RequireInt("server.port");
builder.AddEnvironmentDictionary(environment, "DEMO_", "__");
int environmentPort = builder.BuildOrThrow().RequireInt("server.port");

// Later sources override earlier values without discarding unrelated settings.
var settings = builder.AddOverride("server.port", "9443").BuildOrThrow();
Console.WriteLine("Configuration precedence: defaults < JSON < environment map < override");
Console.WriteLine(
    $"Port layers: 5000 -> {jsonPort} -> {environmentPort} -> {settings.RequireInt("server.port")}");
Console.WriteLine($"Application:    {settings.RequireString("app.name")}");
Console.WriteLine($"Effective port: {settings.RequireInt("server.port")}");
Console.WriteLine($"Search enabled: {settings.RequireBool("features.search")}");
Console.WriteLine($"Missing theme:  {settings.GetStringOrDefault("ui.theme", "light")}");
