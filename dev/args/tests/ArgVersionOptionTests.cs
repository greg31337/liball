using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgVersionOptionTests
{
    [Test]
    public void Parse_DefaultLongVersionOptionWhenVersionTextIsConfigured()
    {
        ArgApp app = new ArgApp("tool");
        app.Version("1.2.3");
        app.RootCommand.RequiredSubcommand();
        app.Command("build");

        ArgParseResult parseResult = new ArgParser(app.RootCommand).Parse(new string[] { "--version" });
        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].SymbolName, Is.EqualTo("--version"));
        Assert.That(parseResult.Occurrences[0].Alias, Is.EqualTo("--version"));
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.HelpRequested, Is.False);
        Assert.That(validation.VersionRequested, Is.True);
        Assert.That(validation.VersionText, Is.EqualTo("1.2.3"));
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_DefaultShortVersionOptionAppliesToSubcommands()
    {
        ArgApp app = new ArgApp("tool");
        app.Version("1.2.3");
        ArgCommand build = app.Command("build");
        build.Option("--configuration").Required().Value("name");

        ArgParseResult parseResult = new ArgParser(app.RootCommand).Parse(new string[] { "build", "-V" });
        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Command, Is.SameAs(build));
        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].SymbolName, Is.EqualTo("--version"));
        Assert.That(parseResult.Occurrences[0].Alias, Is.EqualTo("-V"));
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.VersionRequested, Is.True);
        Assert.That(validation.VersionText, Is.EqualTo("1.2.3"));
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_DefaultVersionOptionIsUnknownWithoutVersionText()
    {
        ArgCommand command = new ArgCommand("tool");

        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--version" });
        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Occurrences, Is.Empty);
        Assert.That(parseResult.Errors.Count, Is.EqualTo(1));
        Assert.That(parseResult.Errors[0].Code, Is.EqualTo("unknown_option"));
        Assert.That(parseResult.Errors[0].SymbolName, Is.EqualTo("--version"));
        Assert.That(validation.VersionRequested, Is.False);
        Assert.That(validation.VersionText, Is.EqualTo(""));
    }

    [Test]
    public void Parse_UserDefinedVersionOptionTakesPrecedence()
    {
        ArgApp app = new ArgApp("tool");
        app.Version("1.2.3");
        ArgOption version = app.Option("--version").Alias("-V").Flag();

        ArgParseResult parseResult = new ArgParser(app.RootCommand).Parse(new string[] { "--version" });

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].Option, Is.SameAs(version));
    }

    [Test]
    public void Parse_UserDefinedSlashVersionOptionShortCircuitsValidation()
    {
        ArgApp app = new ArgApp("tool");
        app.Version("1.2.3");
        app.Option("/version").Flag();
        app.Argument("project").Required();
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;

        ArgParseResult parseResult = ArgParser.WithSettings(app.RootCommand, settings).Parse(new string[] { "/version" });
        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].SymbolName, Is.EqualTo("/version"));
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.VersionRequested, Is.True);
        Assert.That(validation.VersionText, Is.EqualTo("1.2.3"));
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Help_IncludesDefaultVersionOptionWhenVersionTextIsConfigured()
    {
        ArgApp app = new ArgApp("tool");
        app.Version("1.2.3");

        string help = new ArgHelpBuilder().RootHelp(app);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --version, -V - Show version.\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_DoesNotDuplicateUserDefinedVersionOption()
    {
        ArgApp app = new ArgApp("tool");
        app.Version("1.2.3");
        app.Option("--version").Alias("-V").Flag().Description("Print version.");

        string help = new ArgHelpBuilder().RootHelp(app);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --version, -V - Print version.\n  --help, -h - Show help."));
    }

    [Test]
    public void Version_ReturnsConfiguredVersionText()
    {
        ArgApp app = new ArgApp("tool");
        app.Version("1.2.3");
        ArgCommand build = app.Command("build");

        ArgHelpBuilder builder = new ArgHelpBuilder();

        Assert.That(builder.Version(app), Is.EqualTo("1.2.3"));
        Assert.That(builder.CommandVersion(build), Is.EqualTo("1.2.3"));
    }
}
