using System.Collections.Generic;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCompletionProviderTests
{
    [Test]
    public void UseSettingsAndCompleteRejectNullInputs()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgParserSettings settings = new ArgParserSettings();

        Assert.Throws<System.ArgumentNullException>(delegate { new ArgCompletionProvider().UseSettings(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { new ArgCompletionProvider().UseSettings(settings).Complete(null!, new string[] { "" }); });
        Assert.Throws<System.ArgumentNullException>(delegate { new ArgCompletionProvider().UseSettings(settings).Complete(root, null!); });
    }

    [Test]
    public void Complete_ReturnsRootCommandAndOptionCandidatesForEmptyInput()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build").Alias("b").Summary("Build projects.");
        root.Option("--verbose").Alias("-v").Flag().Description("Enable verbose output.");

        List<ArgCompletion> completions = new ArgCompletionProvider().Complete(root, new string[] { "" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "build", "b", "--verbose", "-v", "--help", "-h" }));
        Assert.That(completions[0].Kind, Is.EqualTo(ArgCompletionKind.Command));
        Assert.That(completions[0].DescriptionText, Is.EqualTo("Build projects."));
        Assert.That(completions[2].Kind, Is.EqualTo(ArgCompletionKind.Option));
        Assert.That(completions[2].DescriptionText, Is.EqualTo("Enable verbose output."));
    }

    [Test]
    public void Complete_EmptyArgvUsesEmptyIncompleteToken()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build");

        List<ArgCompletion> completions = new ArgCompletionProvider().Complete(root, new string[] { });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "build", "--help", "-h" }));
    }

    [Test]
    public void Complete_FiltersCommandCandidatesByIncompleteToken()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build");
        root.Command("test");

        List<ArgCompletion> completions = new ArgCompletionProvider().Complete(root, new string[] { "b" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "build" }));
    }

    [Test]
    public void Complete_UsesResolvedCommandPathForSubcommandCandidates()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        project.Command("build");
        root.Command("deploy");

        List<ArgCompletion> completions = new ArgCompletionProvider().Complete(root, new string[] { "project", "b" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "build" }));
        Assert.That(completions[0].Command, Is.SameAs(project.Commands[0]));
    }

    [Test]
    public void Complete_ReturnsLocalInheritedAndDefaultOptionsForOptionPrefix()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--verbose").Alias("-v").Flag();
        ArgCommand build = root.Command("build");
        build.Option("--configuration").Alias("-c").Value("name");

        List<ArgCompletion> completions = new ArgCompletionProvider().Complete(root, new string[] { "build", "--" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "--configuration", "--verbose", "--help" }));
        Assert.That(completions[0].Option, Is.SameAs(build.Options[0]));
        Assert.That(completions[1].Option, Is.SameAs(root.GlobalOptions[0]));
    }

    [Test]
    public void Complete_ReturnsStaticOptionValuesAfterOptionToken()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        ArgOption configuration = build.Option("--configuration").Value("name");
        configuration.Complete("Debug");
        configuration.Complete("Release");

        List<ArgCompletion> completions = new ArgCompletionProvider().Complete(root, new string[] { "build", "--configuration", "R" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "Release" }));
        Assert.That(completions[0].Kind, Is.EqualTo(ArgCompletionKind.Value));
        Assert.That(completions[0].Option, Is.SameAs(configuration));
    }

    [Test]
    public void Complete_ReturnsStaticArgumentValuesForNextPositionalArgument()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgArgument target = root.Argument("target");
        target.Complete("linux");
        target.Complete("windows");

        List<ArgCompletion> completions = new ArgCompletionProvider().Complete(root, new string[] { "w" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "windows" }));
        Assert.That(completions[0].Kind, Is.EqualTo(ArgCompletionKind.Value));
        Assert.That(completions[0].Argument, Is.SameAs(target));
    }

    [Test]
    public void Complete_RespectsCaseInsensitiveCommandAndOptionSettings()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.CommandsCaseInsensitive = true;
        settings.OptionsCaseInsensitive = true;
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("Build");
        build.Option("--Configuration").Value("name");

        List<ArgCompletion> commandCompletions = new ArgCompletionProvider().UseSettings(settings).Complete(root, new string[] { "b" });
        List<ArgCompletion> optionCompletions = new ArgCompletionProvider().UseSettings(settings).Complete(root, new string[] { "BUILD", "--c" });

        Assert.That(Texts(commandCompletions), Is.EqualTo(new string[] { "Build" }));
        Assert.That(Texts(optionCompletions), Is.EqualTo(new string[] { "--Configuration" }));
    }

    [Test]
    public void Complete_OmitsHiddenCommandsAndOptionsWhenDetailedHelpIsEnabled()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.IncludeHiddenInHelp = true;
        ArgCommand root = new ArgCommand("tool");
        root.Command("build").Hidden();
        root.Command("test");
        root.Option("--secret").Flag().Hidden();
        root.Option("--verbose").Flag();

        List<ArgCompletion> completions = new ArgCompletionProvider().UseSettings(settings).Complete(root, new string[] { "" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "test", "--verbose", "--help", "-h" }));
    }

    [Test]
    public void Complete_ReturnsDefaultVersionOptionsWhenVersionIsConfigured()
    {
        ArgApp app = new ArgApp("tool").Version("1.2.3");

        List<ArgCompletion> longCompletions = new ArgCompletionProvider().Complete(app.RootCommand, new string[] { "--v" });
        List<ArgCompletion> shortCompletions = new ArgCompletionProvider().Complete(app.RootCommand, new string[] { "-V" });

        Assert.That(Texts(longCompletions), Is.EqualTo(new string[] { "--version" }));
        Assert.That(Texts(shortCompletions), Is.EqualTo(new string[] { "-V" }));
        Assert.That(longCompletions[0].DescriptionText, Is.EqualTo("Show version."));
    }

    [Test]
    public void Complete_UsesConfiguredHelpAliasesAndCanDisableDefaults()
    {
        ArgParserSettings configured = new ArgParserSettings();
        configured.HelpAliases.Clear();
        configured.HelpAliases.Add("--assist");
        configured.HelpAliases.Add("-?");
        ArgParserSettings disabled = new ArgParserSettings();
        disabled.HelpAliases.Clear();
        ArgCommand root = new ArgCommand("tool");

        List<ArgCompletion> configuredCompletions = new ArgCompletionProvider().UseSettings(configured).Complete(root, new string[] { "--a" });
        List<ArgCompletion> disabledCompletions = new ArgCompletionProvider().UseSettings(disabled).Complete(root, new string[] { "" });

        Assert.That(Texts(configuredCompletions), Is.EqualTo(new string[] { "--assist" }));
        Assert.That(Texts(disabledCompletions), Is.Empty);
    }

    [Test]
    public void Complete_OnlyReturnsSlashOptionAliasesWhenSlashOptionsAreEnabled()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Option("/verbose").Flag();
        ArgParserSettings slashSettings = new ArgParserSettings();
        slashSettings.EnableSlashOptions = true;

        List<ArgCompletion> disabledCompletions = new ArgCompletionProvider().Complete(root, new string[] { "/" });
        List<ArgCompletion> enabledCompletions = new ArgCompletionProvider().UseSettings(slashSettings).Complete(root, new string[] { "/" });

        Assert.That(Texts(disabledCompletions), Is.Empty);
        Assert.That(Texts(enabledCompletions), Is.EqualTo(new string[] { "/verbose" }));
    }

    static string[] Texts(List<ArgCompletion> completions)
    {
        string[] values = new string[completions.Count];
        int index = 0;
        while (index < completions.Count)
        {
            values[index] = completions[index].Text;
            index = index + 1;
        }

        return values;
    }
}
