using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgExampleHelpListingTests
{
    [Test]
    public void Help_IncludesExamplesInDeclarationOrder()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Example("tool build src");
        command.Example("tool test --filter unit");

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --help, -h - Show help.\n\nExamples:\n  tool build src\n  tool test --filter unit"));
    }

    [Test]
    public void Help_AppendsExamplesAfterOptionAndArgumentSections()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name");
        command.Argument("project").Required();
        command.Example("tool --configuration Release src");

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options] <project>\n\nArguments:\n  <project> (required)\n\nOptions:\n  --configuration <name>\n  --help, -h - Show help.\n\nExamples:\n  tool --configuration Release src"));
    }

    [Test]
    public void Help_CommandWithoutExamplesOmitsExamplesSection()
    {
        ArgCommand command = new ArgCommand("tool");

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_RootCommandExamplesAppearInAppHelp()
    {
        ArgApp app = new ArgApp("tool");
        app.RootCommand.Example("tool --help");

        string help = new ArgHelpBuilder().RootHelp(app);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --help, -h - Show help.\n\nExamples:\n  tool --help"));
    }
}
