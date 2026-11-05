using System.Collections.Generic;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgParserModeTests
{
    [Test]
    public void PosixMode_UsesClustersAttachedValuesCaseSensitiveMatchingAndOptionsBeforeOperands()
    {
        ArgParserSettings settings = ContrarySettings(ArgParserMode.Posix);
        ArgApp app = OptionApp(settings);

        ArgParseResult options = app.Parse(new string[] { "-ab", "-n5" });
        ArgParseResult ordering = app.Parse(new string[] { "operand", "--verbose", "/trace" });
        ArgParseResult slash = app.Parse(new string[] { "/trace" });
        ArgParseResult optionCase = app.Parse(new string[] { "--VERBOSE" });
        ArgParseResult commandCase = app.Parse(new string[] { "build" });

        Assert.That(SymbolNames(options), Is.EqualTo(new string[] { "--all", "--brief", "--number" }));
        Assert.That(options.Occurrences[2].Values, Is.EqualTo(new string[] { "5" }));
        Assert.That(ordering.Occurrences, Is.Empty);
        Assert.That(RawTexts(ordering.Positionals), Is.EqualTo(new string[] { "operand", "--verbose", "/trace" }));
        Assert.That(slash.Occurrences, Is.Empty);
        Assert.That(RawTexts(slash.Positionals), Is.EqualTo(new string[] { "/trace" }));
        Assert.That(optionCase.Errors.Count, Is.EqualTo(1));
        Assert.That(optionCase.Errors[0].SymbolName, Is.EqualTo("--VERBOSE"));
        Assert.That(commandCase.Command, Is.SameAs(app.RootCommand));
    }

    [Test]
    public void GnuFriendlyMode_UsesClustersAttachedValuesCaseSensitiveMatchingAndInterspersedOptions()
    {
        ArgParserSettings settings = ContrarySettings(ArgParserMode.GnuFriendly);
        ArgApp app = OptionApp(settings);

        ArgParseResult result = app.Parse(new string[] { "operand", "-ab", "-n5", "--verbose", "/trace" });
        ArgParseResult optionCase = app.Parse(new string[] { "--VERBOSE" });
        ArgParseResult commandCase = app.Parse(new string[] { "build" });

        Assert.That(SymbolNames(result), Is.EqualTo(new string[] { "--all", "--brief", "--number", "--verbose" }));
        Assert.That(result.Occurrences[2].Values, Is.EqualTo(new string[] { "5" }));
        Assert.That(RawTexts(result.Positionals), Is.EqualTo(new string[] { "operand", "/trace" }));
        Assert.That(optionCase.Errors.Count, Is.EqualTo(1));
        Assert.That(commandCase.Command, Is.SameAs(app.RootCommand));
    }

    [Test]
    public void WindowsFriendlyMode_UsesSlashAliasesCaseInsensitiveMatchingAndInterspersedOptionsWithoutShortCompaction()
    {
        ArgParserSettings settings = ContrarySettings(ArgParserMode.WindowsFriendly);
        ArgApp app = OptionApp(settings);
        ArgCommand build = app.RootCommand.Commands[0];

        ArgParseResult result = app.Parse(new string[] { "build", "operand", "/trace", "/name:Value", "--VERBOSE", "-ab", "-n5" });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(SymbolNames(result), Is.EqualTo(new string[] { "/Trace", "/Name", "--verbose" }));
        Assert.That(result.Occurrences[1].Values, Is.EqualTo(new string[] { "Value" }));
        Assert.That(RawTexts(result.Positionals), Is.EqualTo(new string[] { "operand" }));
        Assert.That(result.Errors.Count, Is.EqualTo(2));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("-ab"));
        Assert.That(result.Errors[1].SymbolName, Is.EqualTo("-n5"));
    }

    [Test]
    public void Completion_UsesModeSpecificSlashAndMatchingBehavior()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("Build");
        root.Option("/Verbose").Flag();
        ArgParserSettings posix = ContrarySettings(ArgParserMode.Posix);
        ArgParserSettings windows = ContrarySettings(ArgParserMode.WindowsFriendly);

        List<ArgCompletion> posixCommands = new ArgCompletionProvider().UseSettings(posix).Complete(root, new string[] { "b" });
        List<ArgCompletion> posixOptions = new ArgCompletionProvider().UseSettings(posix).Complete(root, new string[] { "/v" });
        List<ArgCompletion> windowsCommands = new ArgCompletionProvider().UseSettings(windows).Complete(root, new string[] { "b" });
        List<ArgCompletion> windowsOptions = new ArgCompletionProvider().UseSettings(windows).Complete(root, new string[] { "/v" });

        Assert.That(CompletionTexts(posixCommands), Does.Not.Contain("Build"));
        Assert.That(CompletionTexts(posixOptions), Does.Not.Contain("/Verbose"));
        Assert.That(CompletionTexts(windowsCommands), Does.Contain("Build"));
        Assert.That(CompletionTexts(windowsOptions), Does.Contain("/Verbose"));
    }

    static ArgParserSettings ContrarySettings(ArgParserMode mode)
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.Mode = mode;
        settings.EnablePosixShortClusters = mode == ArgParserMode.WindowsFriendly;
        settings.EnableShortOptionAttachedValues = mode == ArgParserMode.WindowsFriendly;
        settings.EnableSlashOptions = mode != ArgParserMode.WindowsFriendly;
        settings.OptionsCaseInsensitive = mode != ArgParserMode.WindowsFriendly;
        settings.CommandsCaseInsensitive = mode != ArgParserMode.WindowsFriendly;
        settings.StopAtFirstPositional = mode != ArgParserMode.Posix;
        return settings;
    }

    static ArgApp OptionApp(ArgParserSettings settings)
    {
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        ArgCommand root = app.GetRootCommand();
        root.GlobalOption("--all").Alias("-a").Flag();
        root.GlobalOption("--brief").Alias("-b").Flag();
        root.GlobalOption("--number").Alias("-n").Value("count");
        root.GlobalOption("--verbose").Flag();
        root.GlobalOption("/Trace").Flag();
        root.GlobalOption("/Name").Value("value");
        root.Command("Build");
        return app;
    }

    static string[] SymbolNames(ArgParseResult result)
    {
        string[] names = new string[result.Occurrences.Count];
        int index = 0;
        while (index < result.Occurrences.Count)
        {
            names[index] = result.Occurrences[index].SymbolName;
            index = index + 1;
        }

        return names;
    }

    static string[] RawTexts(List<ArgToken> tokens)
    {
        string[] texts = new string[tokens.Count];
        int index = 0;
        while (index < tokens.Count)
        {
            texts[index] = tokens[index].RawText;
            index = index + 1;
        }

        return texts;
    }

    static string[] CompletionTexts(List<ArgCompletion> completions)
    {
        string[] texts = new string[completions.Count];
        int index = 0;
        while (index < completions.Count)
        {
            texts[index] = completions[index].Text;
            index = index + 1;
        }

        return texts;
    }
}
