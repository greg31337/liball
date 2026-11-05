using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgSubcommandHelpListingTests
{
    [Test]
    public void Help_IncludesSubcommandsWithAliasesAndSummaries()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build").Alias("b").Summary("Build projects.");
        root.Command("test").Summary("Run tests.");

        string help = new ArgHelpBuilder().Help(root);

        Assert.That(help, Is.EqualTo("Usage: tool [options] [command]\n\nCommands:\n  build, b - Build projects.\n  test - Run tests.\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_GroupsSubcommandsByGroupName()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build").Summary("Build projects.");
        build.GroupName = "Project";
        ArgCommand test = root.Command("test").Summary("Run tests.");
        test.GroupName = "Project";
        ArgCommand publish = root.Command("publish").Summary("Publish packages.");
        publish.GroupName = "Release";

        string help = new ArgHelpBuilder().Help(root);

        Assert.That(help, Is.EqualTo("Usage: tool [options] [command]\n\nCommands:\n  Project:\n    build - Build projects.\n    test - Run tests.\n  Release:\n    publish - Publish packages.\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_OmitsHiddenGroupedSubcommandsAndEmptyGroups()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand internalCommand = root.Command("internal").Summary("Internal command.").Hidden();
        internalCommand.GroupName = "Project";
        ArgCommand publish = root.Command("publish").Summary("Publish packages.");
        publish.GroupName = "Release";

        string help = new ArgHelpBuilder().Help(root);

        Assert.That(help, Is.EqualTo("Usage: tool [options] [command]\n\nCommands:\n  Release:\n    publish - Publish packages.\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_OmitsHiddenSubcommandsFromListing()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build").Summary("Build projects.");
        root.Command("internal").Summary("Internal command.").Hidden();

        string help = new ArgHelpBuilder().Help(root);

        Assert.That(help, Is.EqualTo("Usage: tool [options] [command]\n\nCommands:\n  build - Build projects.\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_OmitsCommandsSectionWhenAllSubcommandsAreHidden()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("internal").Hidden();

        string help = new ArgHelpBuilder().Help(root);

        Assert.That(help, Is.EqualTo("Usage: tool [options] [command]\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_RendersSubcommandWithoutSummaryWithoutSeparator()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("clean").Alias("c");

        string help = new ArgHelpBuilder().Help(root);

        Assert.That(help, Is.EqualTo("Usage: tool [options] [command]\n\nCommands:\n  clean, c\n\nOptions:\n  --help, -h - Show help."));
    }
}
