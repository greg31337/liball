using System.Collections.Generic;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgMessageProviderTests
{
    [Test]
    public void EnglishProvider_DefinesEveryPublicMessageKey()
    {
        ArgEnglishMessageProvider provider = new ArgEnglishMessageProvider();
        string[] keys = AllKeys();
        int index = 0;
        while (index < keys.Length)
        {
            Assert.That(provider.GetMessage(keys[index], new string[] { "first", "second" }), Is.Not.Null, keys[index]);
            index = index + 1;
        }

        Assert.That(provider.GetMessage("unknown.key", new string[] { }), Is.Null);
    }

    [Test]
    public void CustomProvider_ReplacesParserAndValidatorMessages()
    {
        ArgParserSettings settings = CustomSettings();
        ArgApp unknownOptionApp = new ArgApp("tool").UseSettings(settings);
        ArgParseResult parseResult = unknownOptionApp.Parse(new string[] { "--missing" });
        ArgApp requiredApp = new ArgApp("tool").UseSettings(settings);
        requiredApp.Option("--required").Flag().Required();
        ArgValidationResult validationResult = requiredApp.Validate(new string[] { });

        Assert.That(parseResult.Errors[0].Message, Is.EqualTo("custom:parser.option.unknown:--missing"));
        Assert.That(validationResult.Errors[0].Message, Is.EqualTo("custom:validation.option.required:--required"));
    }

    [Test]
    public void CustomProvider_ReplacesHelpInvocationAndCompletionText()
    {
        ArgParserSettings settings = CustomSettings();
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Command("build");
        app.Option("--required").Flag().Required();

        ArgRunResult runResult = app.Run(new string[] { "--help" });
        List<ArgCompletion> completions = new ArgCompletionProvider().UseSettings(settings).Complete(app.GetRootCommand(), new string[] { "--h" });

        Assert.That(runResult.HelpText, Does.StartWith("custom:help.heading.usage tool"));
        Assert.That(runResult.HelpText, Does.Contain("custom:help.heading.commands"));
        Assert.That(runResult.HelpText, Does.Contain("custom:help.heading.options"));
        Assert.That(runResult.HelpText, Does.Contain("custom:help.option.description"));
        Assert.That(runResult.HelpText, Does.Contain("custom:help.detail.required"));
        Assert.That(completions[0].DescriptionText, Is.EqualTo("custom:help.option.description"));
    }

    [Test]
    public void CustomProvider_ReplacesHelpCommandName()
    {
        ArgParserSettings settings = CustomSettings();
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        ArgCommand build = app.Command("build");

        ArgValidationResult result = app.Validate(new string[] { "custom:parser.helpCommand.name", "build" });

        Assert.That(result.HelpRequested, Is.True);
        Assert.That(result.ParseResult.Command, Is.SameAs(build));
    }

    [Test]
    public void MissingCustomMessagesAndNullProviderFallBackToEnglish()
    {
        ArgParserSettings partialSettings = new ArgParserSettings();
        partialSettings.MessageProvider = new PartialArgMessageProvider();
        ArgApp partialApp = new ArgApp("tool").UseSettings(partialSettings);
        ArgParseResult partialParse = partialApp.Parse(new string[] { "--missing" });
        string partialHelp = new ArgHelpBuilder().UseSettings(partialSettings).RootHelp(partialApp);
        ArgParserSettings nullSettings = new ArgParserSettings();
        nullSettings.MessageProvider = null!;
        ArgParseResult nullParse = new ArgApp("tool").UseSettings(nullSettings).Parse(new string[] { "--missing" });

        Assert.That(partialParse.Errors[0].Message, Is.EqualTo("Unknown option '--missing'."));
        Assert.That(partialHelp, Does.StartWith("Partial usage: tool"));
        Assert.That(partialHelp, Does.Contain("Options:"));
        Assert.That(partialHelp, Does.Contain("Show help."));
        Assert.That(nullParse.Errors[0].Message, Is.EqualTo("Unknown option '--missing'."));
        Assert.That(ArgMessageLookup.Get(partialSettings, "unknown.key", new string[] { }), Is.EqualTo("unknown.key"));
    }

    [Test]
    public void CustomProvider_ReplacesCompletionFailureText()
    {
        ArgCompletionProvider provider = new ArgCompletionProvider().UseSettings(CustomSettings());

        System.ArgumentException exception = Assert.Throws<System.ArgumentException>(delegate { provider.GenerateScript(new ArgCommand("tool"), (ArgCompletionShell)99); })!;

        Assert.That(exception.Message, Does.StartWith("custom:completion.shell.unsupported"));
    }

    static ArgParserSettings CustomSettings()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.MessageProvider = new TestArgMessageProvider();
        return settings;
    }

    static string[] AllKeys()
    {
        return new string[]
        {
            ArgMessageKeys.HelpOptionDescription,
            ArgMessageKeys.VersionOptionDescription,
            ArgMessageKeys.HelpCommandName,
            ArgMessageKeys.UnexpectedOptionValue,
            ArgMessageKeys.MissingOptionValue,
            ArgMessageKeys.UnknownOption,
            ArgMessageKeys.MissingRequiredOption,
            ArgMessageKeys.MissingRequiredArgument,
            ArgMessageKeys.MissingRequiredSubcommand,
            ArgMessageKeys.OptionTooFewValues,
            ArgMessageKeys.OptionTooManyValues,
            ArgMessageKeys.ArgumentTooFewValues,
            ArgMessageKeys.ArgumentTooManyValues,
            ArgMessageKeys.OneValue,
            ArgMessageKeys.MultipleValues,
            ArgMessageKeys.DuplicateOption,
            ArgMessageKeys.UnknownCommand,
            ArgMessageKeys.InvalidOptionChoice,
            ArgMessageKeys.InvalidArgumentChoice,
            ArgMessageKeys.ConflictingOption,
            ArgMessageKeys.MissingDependentOption,
            ArgMessageKeys.MutuallyExclusiveOptionGroup,
            ArgMessageKeys.MissingRequiredOptionGroup,
            ArgMessageKeys.UsageLabel,
            ArgMessageKeys.CommandsHeading,
            ArgMessageKeys.ArgumentsHeading,
            ArgMessageKeys.OptionsHeading,
            ArgMessageKeys.OptionGroupsHeading,
            ArgMessageKeys.ExamplesHeading,
            ArgMessageKeys.OptionsPlaceholder,
            ArgMessageKeys.RequiredCommandPlaceholder,
            ArgMessageKeys.OptionalCommandPlaceholder,
            ArgMessageKeys.RequiredDetail,
            ArgMessageKeys.DefaultDetail,
            ArgMessageKeys.EnvironmentDetail,
            ArgMessageKeys.ChoicesDetail,
            ArgMessageKeys.MutuallyExclusiveDetail,
            ArgMessageKeys.AtLeastOneRequiredDetail,
            ArgMessageKeys.ValuePlaceholder,
            ArgMessageKeys.UnsupportedCompletionShell
        };
    }
}

public class TestArgMessageProvider : IArgMessageProvider
{
    public string? GetMessage(string key, string[] values)
    {
        string message = "custom:" + key;
        int index = 0;
        while (index < values.Length)
        {
            message = message + ":" + values[index];
            index = index + 1;
        }

        return message;
    }
}

public class PartialArgMessageProvider : IArgMessageProvider
{
    public string? GetMessage(string key, string[] values)
    {
        if (key == ArgMessageKeys.UsageLabel)
        {
            return "Partial usage:";
        }

        return null;
    }
}
