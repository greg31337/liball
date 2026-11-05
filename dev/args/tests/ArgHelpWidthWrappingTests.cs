using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgHelpWidthWrappingTests
{
    [Test]
    public void Builder_UsesDefaultHelpWidth()
    {
        ArgHelpBuilder builder = new ArgHelpBuilder();

        Assert.That(builder.HelpWidth, Is.EqualTo(80));
    }

    [Test]
    public void Builder_UsesHelpWidthFromSettings()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpWidth = 20;

        ArgHelpBuilder builder = new ArgHelpBuilder().UseSettings(settings);

        Assert.That(builder.GetHelpWidth(), Is.EqualTo(20));
    }

    [Test]
    public void Help_WrapsDescriptionToConfiguredWidth()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpWidth = 20;
        ArgCommand command = new ArgCommand("tool");
        command.Description("Alpha beta gamma delta epsilon");

        string help = new ArgHelpBuilder().UseSettings(settings).Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nAlpha beta gamma\ndelta epsilon\n\nOptions:\n  --help, -h - Show\n    help."));
    }

    [Test]
    public void Help_WrapsOptionListingWithContinuationIndent()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpWidth = 32;
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Description("Select build configuration for release output");

        string help = new ArgHelpBuilder().UseSettings(settings).Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --configuration <name> -\n    Select build configuration\n    for release output\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_DoesNotWrapExamples()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpWidth = 20;
        ArgCommand command = new ArgCommand("tool");
        command.Example("tool build --configuration Release src");

        string help = new ArgHelpBuilder().UseSettings(settings).Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --help, -h - Show\n    help.\n\nExamples:\n  tool build --configuration Release src"));
    }
}
