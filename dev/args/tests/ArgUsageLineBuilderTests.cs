using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgUsageLineBuilderTests
{
    [Test]
    public void UsageLine_ForBareRootCommandUsesCommandName()
    {
        ArgCommand command = new ArgCommand("tool");

        string usage = new ArgHelpBuilder().UsageLine(command);

        Assert.That(usage, Is.EqualTo("Usage: tool [options]"));
    }

    [Test]
    public void UsageLine_IncludesNestedCommandPathOptionsAndRequiredArgument()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        ArgCommand build = project.Command("build");
        build.Option("--configuration").Value("name");
        build.Argument("project").Required();

        string usage = new ArgHelpBuilder().UsageLine(build);

        Assert.That(usage, Is.EqualTo("Usage: tool project build [options] <project>"));
    }

    [Test]
    public void UsageLine_IncludesInheritedGlobalOptions()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--verbose").Flag();
        ArgCommand build = root.Command("build");

        string usage = new ArgHelpBuilder().UsageLine(build);

        Assert.That(usage, Is.EqualTo("Usage: tool build [options]"));
    }

    [Test]
    public void UsageLine_LeavesOutOptionsWhenHelpDisabledAndNoVisibleOptionsExist()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpAliases.Clear();
        ArgCommand command = new ArgCommand("tool");
        command.Option("--secret").Flag().Hidden();

        string usage = new ArgHelpBuilder().UseSettings(settings).UsageLine(command);

        Assert.That(usage, Is.EqualTo("Usage: tool"));
    }

    [Test]
    public void UsageLine_FormatsOptionalAndRequiredVariadicArguments()
    {
        ArgCommand optional = new ArgCommand("tool");
        optional.Argument("files").Variadic();
        ArgCommand required = new ArgCommand("copy");
        required.Argument("files").Required().Variadic();
        ArgHelpBuilder builder = new ArgHelpBuilder();

        Assert.That(builder.UsageLine(optional), Is.EqualTo("Usage: tool [options] [files...]"));
        Assert.That(builder.UsageLine(required), Is.EqualTo("Usage: copy [options] <files...>"));
    }

    [Test]
    public void UsageLine_FormatsOptionalAndRequiredSubcommandPlaceholders()
    {
        ArgCommand optional = new ArgCommand("tool");
        optional.Command("build");
        ArgCommand required = new ArgCommand("app");
        required.RequiredSubcommand();
        required.Command("run");
        ArgHelpBuilder builder = new ArgHelpBuilder();

        Assert.That(builder.UsageLine(optional), Is.EqualTo("Usage: tool [options] [command]"));
        Assert.That(builder.UsageLine(required), Is.EqualTo("Usage: app [options] <command>"));
    }

    [Test]
    public void UsageLine_UsesExplicitUsageTextWhenPresent()
    {
        ArgCommand command = new ArgCommand("tool");
        command.UsageText = "tool [--custom] <input>";
        command.Option("--ignored").Flag();
        command.Argument("ignored").Required();

        string usage = new ArgHelpBuilder().UsageLine(command);

        Assert.That(usage, Is.EqualTo("Usage: tool [--custom] <input>"));
    }
}
