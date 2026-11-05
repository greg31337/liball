using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgPublicApiEndToEndTests
{
    [Test]
    public void ValidDeployment_ProducesDeterministicResultsAcrossThePublicPipeline()
    {
        List<string> calls = new List<string>();
        ArgApp concreteApp = DeploymentApp(calls);
        IArgApp app = concreteApp;
        string[] argv = new string[]
        {
            "deploy",
            "-vv",
            "--image", "registry.example/app:v1",
            "--environment=production",
            "--replicas", "3",
            "--tag", "blue",
            "--tag=green",
            "--execute",
            "--legacy",
            "api",
            "worker"
        };

        ArgParseResult firstParse = app.Parse(argv);
        ArgParseResult secondParse = app.Parse(argv);
        IArgValues values = new ArgValues(firstParse);

        Assert.That(ParseSignature(secondParse), Is.EqualTo(ParseSignature(firstParse)));
        Assert.That(firstParse.Errors, Is.Empty);
        Assert.That(firstParse.Command.GetName(), Is.EqualTo("deploy"));
        Assert.That(CommandNames(firstParse.CommandPath), Is.EqualTo(new string[] { "ship", "deploy" }));
        Assert.That(firstParse.Tokens.Count, Is.EqualTo(argv.Length));
        Assert.That(values.Count("--verbose"), Is.EqualTo(2));
        Assert.That(values.String("--workspace"), Is.EqualTo("."));
        Assert.That(values.Source("--workspace"), Is.EqualTo(ArgValueSource.Default));
        Assert.That(values.String("--image"), Is.EqualTo("registry.example/app:v1"));
        Assert.That(values.String("--environment"), Is.EqualTo("production"));
        Assert.That(values.Int("--replicas"), Is.EqualTo(3));
        Assert.That(values.Strings("--tag"), Is.EqualTo(new string[] { "blue", "green" }));
        Assert.That(values.Bool("--legacy"), Is.True);
        Assert.That(values.Strings("services"), Is.EqualTo(new string[] { "api", "worker" }));
        Assert.That(values.GetConversionErrors(), Is.Empty);
        Assert.That(firstParse.Diagnostics.Count, Is.EqualTo(1));
        AssertDiagnostic(firstParse.Diagnostics[0], "deprecated_option", ArgSymbolKind.Option, "Use --compatibility.", 11, "--legacy");

        ArgValidationResult firstValidation = app.Validate(argv);
        ArgValidationResult secondValidation = app.Validate(argv);

        Assert.That(ValidationSignature(secondValidation), Is.EqualTo(ValidationSignature(firstValidation)));
        Assert.That(firstValidation.Errors, Is.Empty);
        Assert.That(firstValidation.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(calls, Is.Empty);

        IArgHelpBuilder helpBuilder = new ArgHelpBuilder().UseSettings(app.GetSettings());
        string firstHelp = helpBuilder.Help(firstParse.Command);
        string secondHelp = helpBuilder.Help(firstParse.Command);
        Assert.That(secondHelp, Is.EqualTo(firstHelp));
        Assert.That(firstHelp, Does.StartWith("Usage: ship deploy [options] <services...>"));
        Assert.That(firstHelp, Does.Contain("Deploy one or more services."));
        Assert.That(firstHelp, Does.Contain("--image, -i <reference>"));
        Assert.That(firstHelp, Does.Contain("--workspace <path>"));

        IArgCompletionProvider completionProvider = new ArgCompletionProvider().UseSettings(app.GetSettings());
        List<ArgCompletion> optionCompletions = completionProvider.Complete(app.GetRootCommand(), new string[] { "deploy", "--en" });
        List<ArgCompletion> valueCompletions = completionProvider.Complete(app.GetRootCommand(), new string[] { "deploy", "--environment", "p" });
        Assert.That(CompletionTexts(optionCompletions), Is.EqualTo(new string[] { "--environment" }));
        Assert.That(CompletionTexts(valueCompletions), Is.EqualTo(new string[] { "production" }));
        Assert.That(optionCompletions[0].SourceName, Is.EqualTo("--environment"));
        Assert.That(valueCompletions[0].SourceName, Is.EqualTo("--environment"));
        string firstScript = completionProvider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Bash);
        string secondScript = completionProvider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Bash);
        Assert.That(secondScript, Is.EqualTo(firstScript));
        Assert.That(firstScript, Does.Contain("'deploy'"));
        Assert.That(firstScript, Does.Contain("'production'"));

        IArgDiagnostics diagnostics = new ArgDiagnostics();
        string firstDescription = diagnostics.Describe(concreteApp);
        string secondDescription = diagnostics.Describe(concreteApp);
        string firstMetadata = diagnostics.MetadataJson(concreteApp);
        string secondMetadata = diagnostics.MetadataJson(concreteApp);
        Assert.That(secondDescription, Is.EqualTo(firstDescription));
        Assert.That(secondMetadata, Is.EqualTo(firstMetadata));
        Assert.That(firstDescription, Does.Contain("command ship deploy"));
        using (JsonDocument document = JsonDocument.Parse(firstMetadata))
        {
            Assert.That(document.RootElement.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(document.RootElement.GetProperty("command").GetProperty("commands")[0].GetProperty("name").GetString(), Is.EqualTo("deploy"));
        }

        ArgRunResult run = app.Run(argv);
        IArgResultStatus status = new ArgResultStatus();

        Assert.That(run.ExitCode, Is.EqualTo(0));
        Assert.That(run.HandlerInvoked, Is.True);
        Assert.That(status.ParseFailed(run.ParseResult), Is.False);
        Assert.That(status.ValidationFailed(run.ValidationResult), Is.False);
        Assert.That(status.RunFailed(run), Is.False);
        Assert.That(run.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(calls, Is.EqualTo(new string[]
        {
            "ship-before",
            "deploy-before",
            "handler:registry.example/app:v1:production:3:api,worker",
            "deploy-after",
            "ship-after"
        }));
    }

    [Test]
    public void InvalidDeployment_AggregatesDeterministicFailuresAndNeverInvokes()
    {
        List<string> calls = new List<string>();
        ArgApp concreteApp = DeploymentApp(calls);
        IArgApp app = concreteApp;
        string[] argv = new string[]
        {
            "deploy",
            "--environment=qa",
            "--replicas=0",
            "--profile=one",
            "--profile=two",
            "--dry-run",
            "--execute",
            "--unknown"
        };

        ArgParseResult firstParse = app.Parse(argv);
        ArgParseResult secondParse = app.Parse(argv);
        ArgValidationResult firstValidation = app.Validate(argv);
        ArgValidationResult secondValidation = app.Validate(argv);
        ArgRunResult run = app.Run(argv);
        IArgResultStatus status = new ArgResultStatus();

        Assert.That(ParseSignature(secondParse), Is.EqualTo(ParseSignature(firstParse)));
        Assert.That(firstParse.Errors.Count, Is.EqualTo(1));
        AssertError(firstParse.Errors[0], "unknown_option", ArgSymbolKind.Option, 7, "--unknown");
        Assert.That(ValidationSignature(secondValidation), Is.EqualTo(ValidationSignature(firstValidation)));
        Assert.That(firstValidation.Errors.Count, Is.EqualTo(7));
        AssertHasError(firstValidation, "unknown_option", "--unknown");
        AssertHasError(firstValidation, "missing_required_option", "--image");
        AssertHasError(firstValidation, "missing_required_argument", "services");
        AssertHasError(firstValidation, "duplicate_option", "--profile");
        AssertHasError(firstValidation, "invalid_choice", "--environment");
        AssertHasError(firstValidation, "mutually_exclusive_group", "execution-mode");
        AssertHasError(firstValidation, "invalid_replicas", "--replicas");
        Assert.That(run.ExitCode, Is.EqualTo(1));
        Assert.That(run.HandlerInvoked, Is.False);
        Assert.That(ValidationSignature(run.ValidationResult), Is.EqualTo(ValidationSignature(firstValidation)));
        Assert.That(status.ParseFailed(firstParse), Is.True);
        Assert.That(status.ValidationFailed(firstValidation), Is.True);
        Assert.That(status.RunFailed(run), Is.True);
        Assert.That(calls, Is.Empty);

        IArgHelpBuilder helpBuilder = new ArgHelpBuilder().UseSettings(app.GetSettings());
        string help = helpBuilder.Help(firstParse.Command);
        Assert.That(help, Is.EqualTo(helpBuilder.Help(firstParse.Command)));
        Assert.That(help, Does.Contain("execution (at least one required): --dry-run, --execute"));
        IArgCompletionProvider completionProvider = new ArgCompletionProvider().UseSettings(app.GetSettings());
        Assert.That(CompletionTexts(completionProvider.Complete(app.GetRootCommand(), new string[] { "deploy", "--pr" })), Is.EqualTo(new string[] { "--profile" }));
        IArgDiagnostics diagnostics = new ArgDiagnostics();
        string firstDescription = diagnostics.Describe(concreteApp);
        string secondDescription = diagnostics.Describe(concreteApp);
        string firstMetadata = diagnostics.MetadataJson(concreteApp);
        string secondMetadata = diagnostics.MetadataJson(concreteApp);
        Assert.That(secondDescription, Is.EqualTo(firstDescription));
        Assert.That(secondMetadata, Is.EqualTo(firstMetadata));
    }

    [Test]
    public void InformationalRequests_ShortCircuitInvalidRequirementsAndRemainDeterministic()
    {
        List<string> calls = new List<string>();
        ArgApp concreteApp = DeploymentApp(calls);
        IArgApp app = concreteApp;
        ArgCommand administration = app.Command("admin").Branch().Description("Administrative commands.");

        ArgRunResult firstHelp = app.Run(new string[] { "deploy", "--help" });
        ArgRunResult secondHelp = app.Run(new string[] { "deploy", "--help" });
        ArgRunResult firstVersion = app.Run(new string[] { "deploy", "--version" });
        ArgRunResult secondVersion = app.Run(new string[] { "deploy", "--version" });
        ArgRunResult firstBranch = app.Run(new string[] { "admin" });
        ArgRunResult secondBranch = app.Run(new string[] { "admin" });

        Assert.That(firstHelp.ExitCode, Is.EqualTo(0));
        Assert.That(firstHelp.HandlerInvoked, Is.False);
        Assert.That(firstHelp.ValidationResult.HelpRequested, Is.True);
        Assert.That(firstHelp.ValidationResult.Errors, Is.Empty);
        Assert.That(secondHelp.HelpText, Is.EqualTo(firstHelp.HelpText));
        Assert.That(firstHelp.HelpText, Does.Contain("Deploy one or more services."));
        Assert.That(firstVersion.ExitCode, Is.EqualTo(0));
        Assert.That(firstVersion.HandlerInvoked, Is.False);
        Assert.That(firstVersion.ValidationResult.VersionRequested, Is.True);
        Assert.That(firstVersion.VersionText, Is.EqualTo("2.4.0"));
        Assert.That(secondVersion.VersionText, Is.EqualTo(firstVersion.VersionText));
        Assert.That(firstBranch.ExitCode, Is.EqualTo(0));
        Assert.That(firstBranch.HandlerInvoked, Is.False);
        Assert.That(firstBranch.Context.Command, Is.SameAs(administration));
        Assert.That(firstBranch.ValidationResult.HelpRequested, Is.True);
        Assert.That(secondBranch.HelpText, Is.EqualTo(firstBranch.HelpText));
        Assert.That(firstBranch.HelpText, Does.Contain("Administrative commands."));
        Assert.That(calls, Is.Empty);
    }

    static ArgApp DeploymentApp(List<string> calls)
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.Mode = ArgParserMode.GnuFriendly;
        ArgApp app = new ArgApp("ship").UseSettings(settings).Description("Deploy services deterministically.").Version("2.4.0");
        ArgCommand root = app.GetRootCommand();
        root.GlobalOption("--verbose").Alias("-v").Count().Description("Increase verbosity.");
        root.GlobalOption("--workspace").Value("path").Default(".").Description("Workspace path.");
        ArgCommand deploy = app.Command("deploy").Alias("d").Summary("Deploy services.").Description("Deploy one or more services.").Example("ship deploy --image app:v1 api");
        deploy.Option("--image").Alias("-i").Value("reference").Required().Description("Container image.");
        deploy.Option("--environment")
            .Alias("-e")
            .Value("name")
            .Default("staging")
            .Choice("staging")
            .Choice("production")
            .Complete("staging")
            .Complete("production")
            .Description("Deployment environment.");
        deploy.Option("--replicas").Value("count").Default("1").Description("Replica count.");
        deploy.Option("--tag").Alias("-t").Value("name").Repeatable().Description("Deployment tag.");
        deploy.Option("--legacy").Flag().Deprecated("Use --compatibility.");
        deploy.Option("--profile").Value("name");
        deploy.Option("--dry-run").Flag();
        deploy.Option("--execute").Flag();
        deploy.AtLeastOneRequiredGroup("execution", new string[] { "--dry-run", "--execute" });
        deploy.MutuallyExclusiveGroup("execution-mode", new string[] { "--dry-run", "--execute" });
        deploy.Argument("services").Required().Variadic().Description("Services to deploy.");
        deploy.ValidateCommand(delegate(ArgContext context)
        {
            if (context.Values.String("--replicas") == "0")
            {
                return new ArgParseError("invalid_replicas", ArgSymbolKind.Option, "Replica count must be greater than zero.", -1, "--replicas");
            }

            return null;
        });
        root.Before(delegate(ArgContext context)
        {
            calls.Add("ship-before");
            return 0;
        });
        deploy.Before(delegate(ArgContext context)
        {
            calls.Add("deploy-before");
            return 0;
        });
        deploy.Handler(delegate(ArgContext context)
        {
            calls.Add("handler:" + context.Values.String("--image") + ":" + context.Values.String("--environment") + ":" + context.Values.String("--replicas") + ":" + string.Join(",", context.Values.Strings("services")));
            return 0;
        });
        deploy.After(delegate(ArgContext context)
        {
            calls.Add("deploy-after");
            return 0;
        });
        root.After(delegate(ArgContext context)
        {
            calls.Add("ship-after");
            return 0;
        });
        return app;
    }

    static string ParseSignature(ArgParseResult result)
    {
        StringBuilder text = new StringBuilder();
        text.Append("command=").Append(result.Command.GetName());
        int index = 0;
        while (index < result.CommandPath.Count)
        {
            text.Append("|path=").Append(result.CommandPath[index].GetName());
            index = index + 1;
        }

        index = 0;
        while (index < result.Tokens.Count)
        {
            ArgToken token = result.Tokens[index];
            text.Append("|token=").Append(token.Index).Append(':').Append((int)token.Kind).Append(':').Append(token.Consumed).Append(':').Append(token.RawText);
            index = index + 1;
        }

        index = 0;
        while (index < result.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.Occurrences[index];
            text.Append("|occurrence=").Append((int)occurrence.SymbolKind).Append(':').Append(occurrence.SymbolName).Append(':').Append(occurrence.Alias).Append(':').Append(occurrence.TokenIndex).Append(':').Append((int)occurrence.Source).Append(':').Append(occurrence.Count).Append(':').Append(string.Join(",", occurrence.Values));
            index = index + 1;
        }

        index = 0;
        while (index < result.Errors.Count)
        {
            text.Append("|error=").Append(ErrorSignature(result.Errors[index]));
            index = index + 1;
        }

        index = 0;
        while (index < result.Diagnostics.Count)
        {
            ArgDiagnostic diagnostic = result.Diagnostics[index];
            text.Append("|diagnostic=").Append(diagnostic.Code).Append(':').Append((int)diagnostic.SymbolKind).Append(':').Append(diagnostic.TokenIndex).Append(':').Append(diagnostic.SymbolName).Append(':').Append(diagnostic.Message);
            index = index + 1;
        }

        return text.ToString();
    }

    static string ValidationSignature(ArgValidationResult result)
    {
        StringBuilder text = new StringBuilder();
        text.Append(ParseSignature(result.ParseResult));
        text.Append("|short=").Append(result.ShortCircuited);
        text.Append("|help=").Append(result.HelpRequested);
        text.Append("|version=").Append(result.VersionRequested).Append(':').Append(result.VersionText);
        int index = 0;
        while (index < result.Errors.Count)
        {
            text.Append("|validation-error=").Append(ErrorSignature(result.Errors[index]));
            index = index + 1;
        }

        return text.ToString();
    }

    static string ErrorSignature(ArgParseError error)
    {
        return error.Code + ":" + ((int)error.SymbolKind).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + error.TokenIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + error.SymbolName + ":" + error.Message;
    }

    static string[] CommandNames(List<ArgCommand> commands)
    {
        string[] names = new string[commands.Count];
        int index = 0;
        while (index < names.Length)
        {
            names[index] = commands[index].GetName();
            index = index + 1;
        }

        return names;
    }

    static string[] CompletionTexts(List<ArgCompletion> completions)
    {
        string[] values = new string[completions.Count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = completions[index].Text;
            index = index + 1;
        }

        return values;
    }

    static void AssertDiagnostic(ArgDiagnostic diagnostic, string code, ArgSymbolKind kind, string message, int tokenIndex, string symbolName)
    {
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.SymbolKind, Is.EqualTo(kind));
        Assert.That(diagnostic.Message, Is.EqualTo(message));
        Assert.That(diagnostic.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(diagnostic.SymbolName, Is.EqualTo(symbolName));
    }

    static void AssertError(ArgParseError error, string code, ArgSymbolKind kind, int tokenIndex, string symbolName)
    {
        Assert.That(error.Code, Is.EqualTo(code));
        Assert.That(error.SymbolKind, Is.EqualTo(kind));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
        Assert.That(error.Message, Is.Not.Empty);
    }

    static void AssertHasError(ArgValidationResult result, string code, string symbolName)
    {
        int index = 0;
        while (index < result.Errors.Count)
        {
            ArgParseError error = result.Errors[index];
            if (error.Code == code && error.SymbolName == symbolName)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Missing error " + code + " for " + symbolName + ".");
    }
}
