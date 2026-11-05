# args

`args` is a small command-line parser for C# applications. It builds a command model with plain objects, parses raw `string[]` arguments, validates the result, and can invoke handlers that return integer exit codes.

The library has no runtime dependency on external command-line packages.

## Public API

Only symbols declared `public` are supported as the library API. The parsing, tokenization, validation, invocation, and low-level formatting engines that implement that API are `internal`; applications should enter through `ArgApp` and use the public model, result, help, completion, diagnostics, settings, and value-conversion types around it.

Behavioral API classes implement contracts from `src/Api/Contracts` and expose one constructor at most. Configure alternate settings through the instance `UseSettings` methods on `ArgApp`, `ArgCompletionProvider`, and `ArgHelpBuilder`.

All public symbols include XML documentation in the generated package documentation file. Passive API data is exposed as fields because the source syntax policy does not permit properties. Behavioral classes expose their state through interface-declared getter methods.

`ArgParserSettings` can be passed to `ArgApp` when an application needs non-default parsing behavior. Its `Environment` field accepts an `IArgEnvironment`, allowing environment-backed defaults to be tested without changing process-wide state. The default `ArgSystemEnvironment` implementation reads from `System.Environment`.

```csharp
ArgParserSettings settings = new ArgParserSettings();
settings.OptionsCaseInsensitive = true;
settings.Environment = new ArgSystemEnvironment();

ArgApp app = new ArgApp("builder").UseSettings(settings);
```

## Build And Run

```bash
dotnet test args.slnx
```

## Build Tool Example

This example defines a small build CLI with a global flag, a required positional argument, a repeatable option, version output, generated help, and a handler.

```csharp
using args;

ArgApp app = new ArgApp("builder")
    .Description("Build projects.")
    .Version("1.2.3");

app.GetRootCommand().GlobalOption("--verbose")
    .Alias("-v")
    .Flag()
    .Description("Show detailed output.");

ArgCommand build = app.Command("build")
    .Alias("b")
    .Summary("Compile a project.")
    .Description("Restores and compiles the selected project.");

build.Argument("project")
    .Required()
    .Description("Project file to build.");

build.Option("--configuration")
    .Alias("-c")
    .Value("name")
    .Default("Debug")
    .Choice("Debug")
    .Choice("Release")
    .Complete("Debug")
    .Complete("Release")
    .Description("Build configuration.");

build.Option("--property")
    .Alias("-p")
    .Value("name=value")
    .Repeatable()
    .Description("MSBuild property.");

build.Handler(delegate(ArgContext context)
{
    string project = context.Values.String("project");
    string configuration = context.Values.String("--configuration");
    string[] properties = context.Values.Strings("--property");
    bool verbose = context.Values.Bool("--verbose");

    return project.Length > 0 && configuration.Length > 0 ? 0 : 1;
});

ArgRunResult result = app.Run(args);
return result.ExitCode;
```

Example invocations:

```bash
builder build src/App.csproj --configuration Release
builder b src/App.csproj -p Version=1.2.3 -p ContinuousIntegrationBuild=true
builder --help
builder --version
```

## Deployment CLI Example

This example shows nested commands, inherited global options, required options, option dependencies, conflicts, and environment fallback.

```csharp
using args;

ArgApp app = new ArgApp("ship")
    .Description("Deploy services.")
    .Version("2026.8");

app.GetRootCommand().GlobalOption("--environment")
    .Alias("-e")
    .Value("name")
    .Required()
    .Env("SHIP_ENVIRONMENT")
    .Choice("staging")
    .Choice("production")
    .Description("Deployment environment.");

ArgCommand service = app.Command("service")
    .Summary("Service operations.");

ArgCommand deploy = service.Command("deploy")
    .Summary("Deploy a service.");

deploy.Argument("name")
    .Required()
    .Description("Service name.");

deploy.Option("--image")
    .Value("ref")
    .Required()
    .Description("Container image.");

deploy.Option("--canary")
    .Flag()
    .Conflicts("--force")
    .Description("Deploy as a canary.");

deploy.Option("--force")
    .Flag()
    .Description("Skip safety checks.");

deploy.Option("--wait")
    .OptionalValue("duration")
    .Fallback("5m")
    .Description("Wait for rollout completion.");

deploy.Handler(delegate(ArgContext context)
{
    string serviceName = context.Values.String("name");
    string image = context.Values.String("--image");
    string environment = context.Values.String("--environment");
    string wait = context.Values.String("--wait");

    return serviceName.Length > 0 && image.Length > 0 && environment.Length > 0 ? 0 : 1;
});

ArgRunResult result = app.Run(args);
return result.ExitCode;
```

Example invocations:

```bash
ship service deploy api --environment staging --image registry/api:42
SHIP_ENVIRONMENT=production ship service deploy api --image registry/api:43 --wait
ship help service deploy
```

## Completion Example

Completion candidates come from visible commands, visible option aliases, default help/version options, and static value completions configured on options or arguments.

```csharp
using args;
using System.Collections.Generic;

ArgApp app = new ArgApp("builder").Version("1.2.3");
ArgCommand build = app.Command("build").Summary("Compile a project.");

build.Option("--configuration")
    .Alias("-c")
    .Value("name")
    .Complete("Debug")
    .Complete("Release");

ArgCompletionProvider completionProvider = new ArgCompletionProvider();

List<ArgCompletion> rootCandidates = completionProvider.Complete(
    app.GetRootCommand(),
    new string[] { "b" });

List<ArgCompletion> valueCandidates = completionProvider.Complete(
    app.GetRootCommand(),
    new string[] { "build", "--configuration", "R" });
```

`rootCandidates` contains `build`; `valueCandidates` contains `Release`.
