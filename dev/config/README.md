# config

Initial scaffold created on 2026‑08‑01.

## Architecture Note

`config` is a dependency-free .NET configuration library. The runtime library
uses only built-in .NET APIs, with no NuGet runtime dependencies, network
providers, or external services in the v1 core. It loads
local configuration sources into one hierarchical value tree that can represent
missing values, nulls, strings, numbers, booleans, arrays, and objects while
preserving source metadata for diagnostics.

Source precedence is explicit and deterministic: sources are registered in
order, and later sources override earlier sources by default. Object values
merge deeply, scalar values replace earlier values, and arrays replace earlier
arrays unless an append mode is selected. Typical layering starts with defaults,
then files, then environment variables, then command-line values, and finally
programmatic overrides.

Implementation files in `src` must follow the simplified C# syntax rules in
`syntax.md`. In particular, production code avoids LINQ, properties, records,
class inheritance, attributes, optional parameters, generic type or method
declarations, `try`/`catch`, and runtime dependencies outside the built-in .NET
APIs. Public contracts, implementation classes, field-based data objects, and
enums are separated under `src/Api`. Operational classes implement method-only
interfaces, while public data objects expose fields and keep their helper
methods internal. Parsers, merge machinery, mutable orchestration state, and
other implementation details remain outside the API tree and are `internal`.
Every public type, constructor, method, field, enum, and enum member has XML
documentation using only the tags allowed by `syntax.md`.

The v1 non-goals are remote key-value stores, source-specific round-trip
writing, automatic network loading, hostname or instance-specific layering,
and full framework-style dependency injection integration. Those features can
be evaluated after the local loading, merging, diagnostics, conversion,
validation, export, and reload behavior is stable.

## Examples

### Application-defined sources

Implement `IConfigSource` when configuration comes from an application-owned
component. The source receives its deterministic precedence rank and returns a
root value plus diagnostics. It is loaded again by manual reload controllers,
which also makes this seam useful for small in-memory test doubles.

```csharp
public class FeatureSource : IConfigSource
{
    public ConfigSourceResult Load(int precedenceRank)
    {
        ConfigValues valueFactory = new ConfigValues();
        ConfigMap values = new ConfigMap();
        ConfigValue enabled = valueFactory.SetSource(
            valueFactory.Boolean(true),
            "features",
            precedenceRank,
            "Features.Beta",
            0,
            0);
        values.SetPathDefault("Features.Beta", enabled);

        ConfigValue root = valueFactory.SetSource(
            valueFactory.Object(values),
            "features",
            precedenceRank,
            "",
            0,
            0);
        return new ConfigSourceResult(root, new ConfigDiagnostics());
    }
}

Config config = new ConfigBuilder()
    .AddJsonFile("appsettings.json", ConfigOptional.Optional, ConfigReload.None)
    .AddSource(new FeatureSource())
    .Build();
```

### JSON plus environment plus command line

Register sources from lowest to highest precedence. In this stack, values from
`appsettings.json` are the baseline, `MYAPP_` environment variables override the
file, and command-line arguments override both.

`appsettings.json`:

```json
{
  "Service": {
    "Host": "json.example.com",
    "Port": 5000,
    "TimeoutSeconds": 15
  }
}
```

Environment:

```text
MYAPP_Service__Host=env.example.com
MYAPP_Service__TimeoutSeconds=30
```

Application setup:

```csharp
string[] args = new string[]
{
    "--Service.Host",
    "cli.example.com",
    "--Service.Port=8080"
};

Config config = new ConfigBuilder()
    .AddJsonFile("appsettings.json", ConfigOptional.Required, ConfigReload.None)
    .AddEnvironmentWithPrefix("MYAPP_", "__")
    .AddCommandLine(args)
    .Build();

string host = config.GetString("Service.Host");          // cli.example.com
int port = config.GetInt("Service.Port");                // 8080
int timeout = config.GetInt("Service.TimeoutSeconds");   // 30
```

For deterministic tests, use `AddEnvironmentMap` with the same prefix and
delimiter instead of reading the process environment.

### Dotenv local development

Use `.env` for developer-machine values that should sit above checked-in JSON
but below real environment variables and command-line arguments. Dotenv keys can
use dotted paths for nested config values.

`appsettings.json`:

```json
{
  "Database": {
    "Host": "db.internal",
    "Port": 5432
  },
  "FeatureFlags": {
    "UseFakeEmail": false
  }
}
```

`.env`:

```text
Database.Host=localhost
Database.Password=local-secret
FeatureFlags.UseFakeEmail=true
```

Application setup:

```csharp
Config config = new ConfigBuilder()
    .AddJsonFile("appsettings.json", ConfigOptional.Required, ConfigReload.None)
    .AddDotEnvFile(".env", ConfigOptional.Optional)
    .AddEnvironmentWithPrefix("MYAPP_", "__")
    .AddCommandLine(args)
    .Build();

string databaseHost = config.GetString("Database.Host");       // localhost
bool useFakeEmail = config.GetBool("FeatureFlags.UseFakeEmail"); // true
```

Do not commit `.env` when it contains secrets. Commit a `.env.example` file with
safe placeholder keys instead.

### Validation, binding, and diagnostics

Validation runs after sources have been loaded and merged. Rules can be collected
on a `ConfigValidator`, and the result carries ordinary diagnostics.

```csharp
Config config = new ConfigBuilder()
    .AddJsonFile("appsettings.json", ConfigOptional.Required, ConfigReload.None)
    .AddEnvironmentWithPrefix("MYAPP_", "__")
    .Build();

ConfigValidator validator = new ConfigValidator();
validator
    .RequireString("Service.Host")
    .RequireInt("Service.Port")
    .Range("Service.Port", 1m, 65535m)
    .Allowed("Service.Mode", new string[] { "dev", "prod" });

ConfigValidationResult validation = validator.Validate(config);
if (validation.Diagnostics.HasErrors())
{
    string validationReport = validation.Diagnostics.ToDisplayString();
}
```

Binding fills public fields on an existing object or a newly created target.

```csharp
public class ServiceOptions
{
    public string Host = "";
    public int Port;
    public string Mode = "";
}

ServiceOptions options = (ServiceOptions)config.BindSection("Service", typeof(ServiceOptions));
```

Use build diagnostics to inspect load and merge problems without separately
keeping the built config.

```csharp
ConfigDiagnostics diagnostics = new ConfigBuilder()
    .AddJsonFile("appsettings.json", ConfigOptional.Required, ConfigReload.None)
    .BuildDiagnostics();

string diagnosticReport = diagnostics.ToDisplayString();
```

### Reload

For reloadable files, build a `ConfigReloadController`. `Current()` returns a
snapshot copy, `Reload()` performs a manual reload, and `OnChange` registers
callbacks that run after a successful snapshot replacement.

```csharp
ConfigReloadController controller = new ConfigBuilder()
    .AddJsonFile("appsettings.json", ConfigOptional.Required, ConfigReload.OnChange)
    .BuildReloading()
    .SetDebounceMilliseconds(250);

controller.OnChange(updated =>
{
    string host = updated.GetString("Service.Host");
});

controller.Start();

Config current = controller.Current();
Config reloaded = controller.Reload();
ConfigDiagnostics lastReloadDiagnostics = controller.LastDiagnostics();
```

If a reload reports errors, the previous snapshot remains current and callbacks
are not invoked for that failed attempt.
