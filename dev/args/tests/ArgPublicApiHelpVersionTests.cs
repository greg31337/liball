using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgPublicApiHelpVersionTests
{
    [Test]
    public void RootHelp_IncludesUsageDescriptionGroupedCommandsSummariesAliasesAndExamples()
    {
        IArgApp app = new ArgApp("tool");
        app.Description("Build and publish projects.");
        app.Command("build").Alias("b").Summary("Build projects.").Group("Project");
        app.Command("test").Summary("Run tests.").Group("Project");
        app.Command("publish").Summary("Publish packages.").Group("Release");
        app.GetRootCommand().Example("tool build src");
        app.GetRootCommand().Example("tool test --filter unit");
        IArgHelpBuilder builder = new ArgHelpBuilder();

        string help = builder.RootHelp((ArgApp)app);

        Assert.That(help, Does.StartWith("Usage: tool [options] [command]"));
        Assert.That(help, Does.Contain("Build and publish projects."));
        Assert.That(help, Does.Contain("Commands:\n  Project:\n    build, b - Build projects.\n    test - Run tests.\n  Release:\n    publish - Publish packages."));
        Assert.That(help, Does.Contain("Examples:\n  tool build src\n  tool test --filter unit"));
    }

    [Test]
    public void CommandHelp_IncludesSummaryDescriptionAndCustomOrGeneratedUsage()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project");
        ArgCommand build = project.Command("build").Summary("Build projects.").Description("Runs the compiler.");
        build.Argument("target").Required();
        IArgHelpBuilder builder = new ArgHelpBuilder();

        string generated = builder.Help(build);
        build.Usage("tool project build --custom <target>");
        string custom = builder.Help(build);

        Assert.That(generated, Does.StartWith("Usage: tool project build [options] <target>"));
        Assert.That(generated, Does.Contain("\n\nBuild projects.\n\nRuns the compiler."));
        Assert.That(custom, Does.StartWith("Usage: tool project build --custom <target>"));
        Assert.That(builder.UsageLine(build), Is.EqualTo("Usage: tool project build --custom <target>"));
    }

    [Test]
    public void Help_ListsOptionAndArgumentDetailsAndInheritedOptions()
    {
        IArgApp app = new ArgApp("tool");
        app.GetRootCommand().GlobalOption("--verbose").Flag().Description("Enable verbose output.");
        ArgCommand build = app.Command("build");
        build.Option("--configuration")
            .Alias("-c")
            .Value("name")
            .Required()
            .Description("Build configuration.")
            .Default("Debug")
            .Env("CONFIGURATION")
            .Choice("Debug")
            .Choice("Release");
        build.Argument("project")
            .Required()
            .Description("Project path.")
            .Default(".")
            .Env("PROJECT")
            .Choice("src")
            .Choice("tests");
        IArgHelpBuilder builder = new ArgHelpBuilder().Width(140);

        string help = builder.Help(build);

        Assert.That(help, Does.Contain("Arguments:\n  <project> - Project path. (required; default: .; env: PROJECT; choices: src, tests)"));
        Assert.That(help, Does.Contain("Options:\n  --configuration, -c <name> - Build configuration. (required; default: Debug; env: CONFIGURATION; choices: Debug, Release)"));
        Assert.That(help, Does.Contain("  --verbose - Enable verbose output."));
        Assert.That(help, Does.Contain("  --help, -h - Show help."));
    }

    [Test]
    public void CustomHelpAliases_ControlParsingAndGeneratedHelp()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpAliases.Clear();
        settings.HelpAliases.Add("--assist");
        settings.HelpAliases.Add("-?");
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--required").Required().Flag();
        IArgHelpBuilder builder = new ArgHelpBuilder().UseSettings(settings);

        ArgValidationResult longResult = app.Validate(new string[] { "--assist" });
        ArgValidationResult shortResult = app.Validate(new string[] { "-?" });
        ArgParseResult removedDefault = app.Parse(new string[] { "--help" });
        string help = builder.RootHelp((ArgApp)app);

        Assert.That(longResult.HelpRequested, Is.True);
        Assert.That(longResult.Errors, Is.Empty);
        Assert.That(shortResult.HelpRequested, Is.True);
        Assert.That(shortResult.Errors, Is.Empty);
        Assert.That(removedDefault.Errors.Count, Is.EqualTo(1));
        Assert.That(removedDefault.Errors[0].Code, Is.EqualTo("unknown_option"));
        Assert.That(help, Does.Contain("--assist, -? - Show help."));
        Assert.That(help, Does.Not.Contain("--help, -h"));
    }

    [Test]
    public void HelpCommand_ResolvesNestedAliasesAndAnExplicitHelpCommandTakesPrecedence()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project").Alias("p");
        ArgCommand build = project.Command("build").Alias("b").Description("Build help.");

        ArgValidationResult result = app.Validate(new string[] { "help", "p", "b" });
        string help = new ArgHelpBuilder().Help(result.ParseResult.Command);

        Assert.That(result.ShortCircuited, Is.True);
        Assert.That(result.HelpRequested, Is.True);
        Assert.That(result.ParseResult.HelpCommandRequested, Is.True);
        Assert.That(result.ParseResult.Command, Is.SameAs(build));
        Assert.That(result.ParseResult.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), project, build }));
        Assert.That(help, Does.StartWith("Usage: tool project build [options]"));
        Assert.That(help, Does.Contain("Build help."));

        IArgApp explicitApp = new ArgApp("tool");
        ArgCommand explicitHelp = explicitApp.Command("help");
        ArgValidationResult explicitResult = explicitApp.Validate(new string[] { "help" });

        Assert.That(explicitResult.ParseResult.HelpCommandRequested, Is.False);
        Assert.That(explicitResult.ParseResult.Command, Is.SameAs(explicitHelp));
        Assert.That(explicitResult.HelpRequested, Is.False);
    }

    [Test]
    public void HiddenAndDetailedViews_FilterCommandsOptionsAndGroups()
    {
        IArgApp app = new ArgApp("tool");
        app.Command("internal").Summary("Internal command.").Group("Administration").Hidden();
        app.Option("--public").Flag();
        app.Option("--secret").Flag().Description("Internal option.").Hidden();
        app.GetRootCommand().GlobalOption("--global-secret").Flag().Hidden();
        app.GetRootCommand().MutuallyExclusiveGroup("mixed", new string[] { "--public", "--secret" });
        app.GetRootCommand().AtLeastOneRequiredGroup("hidden-only", new string[] { "--secret" });

        string ordinary = new ArgHelpBuilder().RootHelp((ArgApp)app);
        ArgParserSettings detailedSettings = new ArgParserSettings();
        detailedSettings.IncludeHiddenInHelp = true;
        string detailed = new ArgHelpBuilder().UseSettings(detailedSettings).RootHelp((ArgApp)app);

        Assert.That(ordinary, Does.Not.Contain("internal"));
        Assert.That(ordinary, Does.Not.Contain("--secret"));
        Assert.That(ordinary, Does.Not.Contain("--global-secret"));
        Assert.That(ordinary, Does.Not.Contain("hidden-only"));
        Assert.That(ordinary, Does.Contain("mixed (mutually exclusive): --public"));
        Assert.That(detailed, Does.Contain("Administration:\n    internal - Internal command."));
        Assert.That(detailed, Does.Contain("--secret - Internal option."));
        Assert.That(detailed, Does.Contain("--global-secret"));
        Assert.That(detailed, Does.Contain("mixed (mutually exclusive): --public, --secret"));
        Assert.That(detailed, Does.Contain("hidden-only (at least one required): --secret"));
    }

    [Test]
    public void Help_WrapsTextAndListingsButLeavesExamplesIntact()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpWidth = 32;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Description("Alpha beta gamma delta epsilon zeta eta theta");
        app.Option("--configuration").Value("name").Description("Select build configuration for release output");
        app.GetRootCommand().Example("tool build --configuration Release src");

        string help = new ArgHelpBuilder().UseSettings(settings).RootHelp((ArgApp)app);
        string[] lines = help.Split('\n');
        int index = 0;
        while (index < lines.Length)
        {
            if (lines[index] != "  tool build --configuration Release src")
            {
                Assert.That(lines[index].Length, Is.LessThanOrEqualTo(32), lines[index]);
            }

            index = index + 1;
        }

        Assert.That(help, Does.Contain("Alpha beta gamma delta epsilon\nzeta eta theta"));
        Assert.That(help, Does.Contain("  --configuration <name> -\n    Select build configuration"));
        Assert.That(help, Does.Contain("Examples:\n  tool build --configuration Release src"));
    }

    [Test]
    public void AnsiStyling_StylesHeadingsWithoutChangingPlainTextLayout()
    {
        ArgParserSettings plainSettings = new ArgParserSettings();
        plainSettings.HelpWidth = 32;
        ArgParserSettings styledSettings = new ArgParserSettings();
        styledSettings.HelpWidth = 32;
        styledSettings.EnableAnsiHelpStyling = true;
        IArgApp app = new ArgApp("tool");
        app.Command("build").Summary("Build projects.");
        app.Argument("input");
        app.Option("--configuration").Value("name").Description("Select build configuration for release output");
        app.GetRootCommand().Example("tool build");

        string plain = new ArgHelpBuilder().UseSettings(plainSettings).RootHelp((ArgApp)app);
        string styled = new ArgHelpBuilder().UseSettings(styledSettings).RootHelp((ArgApp)app);

        Assert.That(plain.IndexOf("\u001b[", System.StringComparison.Ordinal), Is.EqualTo(-1));
        Assert.That(styled, Does.StartWith("\u001b[1mUsage:\u001b[0m tool"));
        Assert.That(styled, Does.Contain("\u001b[1mCommands:\u001b[0m"));
        Assert.That(styled, Does.Contain("\u001b[1mArguments:\u001b[0m"));
        Assert.That(styled, Does.Contain("\u001b[1mOptions:\u001b[0m"));
        Assert.That(styled, Does.Contain("\u001b[1mExamples:\u001b[0m"));
        Assert.That(RemoveAnsi(styled), Is.EqualTo(plain));
    }

    [Test]
    public void MessageProvider_LocalizesHelpTextAndHelpCommandName()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.MessageProvider = new PublicApiHelpMessageProvider();
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        ArgCommand build = app.Command("build");
        app.Option("--required").Required().Flag();

        string help = new ArgHelpBuilder().UseSettings(settings).RootHelp((ArgApp)app);
        ArgValidationResult helpCommand = app.Validate(new string[] { "ayuda", "build" });

        Assert.That(help, Does.StartWith("Uso: tool [opciones] [command]"));
        Assert.That(help, Does.Contain("Comandos:"));
        Assert.That(help, Does.Contain("Opciones:"));
        Assert.That(help, Does.Contain("--help, -h - Mostrar ayuda."));
        Assert.That(help, Does.Contain("--required (required)"));
        Assert.That(helpCommand.HelpRequested, Is.True);
        Assert.That(helpCommand.ParseResult.Command, Is.SameAs(build));
    }

    [Test]
    public void DynamicVersionText_IsResolvedForHelpAndEachVersionRequest()
    {
        string version = "1.0.0";
        IArgApp app = new ArgApp("tool");
        app.VersionProvider(delegate
        {
            return version;
        });
        ArgCommand build = app.Command("build");
        IArgHelpBuilder builder = new ArgHelpBuilder();

        Assert.That(builder.Version((ArgApp)app), Is.EqualTo("1.0.0"));
        Assert.That(builder.CommandVersion(build), Is.EqualTo("1.0.0"));
        Assert.That(builder.RootHelp((ArgApp)app), Does.Contain("--version, -V - Show version."));

        version = "2.0.0";
        ArgValidationResult result = app.Validate(new string[] { "--version" });

        Assert.That(result.ShortCircuited, Is.True);
        Assert.That(result.VersionRequested, Is.True);
        Assert.That(result.VersionText, Is.EqualTo("2.0.0"));
        Assert.That(app.GetVersionText(), Is.EqualTo("2.0.0"));
    }

    [Test]
    public void EmptyDynamicVersion_DisablesTheBuiltInVersionOption()
    {
        IArgApp app = new ArgApp("tool").VersionProvider(delegate
        {
            return "";
        });

        ArgParseResult parse = app.Parse(new string[] { "--version" });
        string help = new ArgHelpBuilder().RootHelp((ArgApp)app);

        Assert.That(parse.Errors.Count, Is.EqualTo(1));
        Assert.That(parse.Errors[0].Code, Is.EqualTo("unknown_option"));
        Assert.That(help, Does.Not.Contain("--version"));
    }

    static string RemoveAnsi(string text)
    {
        return text.Replace("\u001b[1m", "").Replace("\u001b[0m", "");
    }
}

public class PublicApiHelpMessageProvider : IArgMessageProvider
{
    public string? GetMessage(string key, string[] values)
    {
        if (key == ArgMessageKeys.UsageLabel)
        {
            return "Uso:";
        }

        if (key == ArgMessageKeys.CommandsHeading)
        {
            return "Comandos:";
        }

        if (key == ArgMessageKeys.OptionsHeading)
        {
            return "Opciones:";
        }

        if (key == ArgMessageKeys.OptionsPlaceholder)
        {
            return "[opciones]";
        }

        if (key == ArgMessageKeys.HelpOptionDescription)
        {
            return "Mostrar ayuda.";
        }

        if (key == ArgMessageKeys.HelpCommandName)
        {
            return "ayuda";
        }

        return null;
    }
}
