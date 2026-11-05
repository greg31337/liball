using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgOptionArgumentHelpListingTests
{
    [Test]
    public void Help_IncludesArgumentDescriptionDefaultsEnvRequiredAndChoices()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project")
            .Required()
            .Description("Project path.")
            .Default(".")
            .Env("PROJECT")
            .Choice("src")
            .Choice("tests");

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options] <project>\n\nArguments:\n  <project> - Project path. (required; default: .; env: PROJECT; choices: src,\n    tests)\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_IncludesOptionAliasesValueDescriptionDefaultsEnvRequiredAndChoices()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration")
            .Alias("-c")
            .Value("name")
            .Required()
            .Description("Build configuration.")
            .Default("Debug")
            .Env("CONFIGURATION")
            .Choice("Debug")
            .Choice("Release");

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --configuration, -c <name> - Build configuration. (required; default: Debug;\n    env: CONFIGURATION; choices: Debug, Release)\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_IncludesInheritedGlobalOptionsAndOmitsHiddenOptions()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--verbose").Flag().Description("Enable verbose output.");
        root.GlobalOption("--secret").Flag().Hidden();
        ArgCommand build = root.Command("build");
        build.Option("--local").Flag().Description("Use local settings.");

        string help = new ArgHelpBuilder().Help(build);

        Assert.That(help, Is.EqualTo("Usage: tool build [options]\n\nOptions:\n  --local - Use local settings.\n  --verbose - Enable verbose output.\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_FormatsOptionalAndVariadicOptionValues()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").OptionalValue("when");
        command.Option("--include").Arity(1, ArgArity.Unbounded);

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --color [<when>]\n  --include <value...>\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_DoesNotShowOptionsSectionWhenOnlyHiddenOptionsExist()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--secret").Flag().Hidden();

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_HiddenUserDefinedHelpAndVersionOptionsDoNotSuppressDefaults()
    {
        ArgApp app = new ArgApp("tool").Version("1.2.3");
        app.Option("--help").Alias("-h").Flag().Hidden();
        app.Option("--version").Alias("-V").Flag().Hidden();

        string help = new ArgHelpBuilder().RootHelp(app);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --version, -V - Show version.\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_OmitsOptionsSectionWhenHelpDisabledAndOnlyHiddenOptionsExist()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpAliases.Clear();
        ArgCommand command = new ArgCommand("tool");
        command.Option("--secret").Flag().Hidden();

        string help = new ArgHelpBuilder().UseSettings(settings).Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool"));
    }
}
