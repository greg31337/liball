using System;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgPublicApiModelTests
{
    [Test]
    public void ApplicationContract_ExposesConfigurationAndModelAccessors()
    {
        IArgApp app = new ArgApp("tool");
        ArgParserSettings settings = new ArgParserSettings();
        Func<string> provider = ProvideVersion;

        Assert.That(app.UseSettings(settings), Is.SameAs(app));
        Assert.That(app.Description("Build things."), Is.SameAs(app));
        Assert.That(app.Version("1.0.0"), Is.SameAs(app));
        Assert.That(app.GetVersionText(), Is.EqualTo("1.0.0"));
        Assert.That(app.VersionProvider(provider), Is.SameAs(app));

        ArgCommand command = app.Command("build");
        ArgOption option = app.Option("--verbose");
        ArgArgument argument = app.Argument("project");

        Assert.That(app.GetName(), Is.EqualTo("tool"));
        Assert.That(app.GetDescriptionText(), Is.EqualTo("Build things."));
        Assert.That(app.GetVersionText(), Is.EqualTo("2.0.0"));
        Assert.That(app.GetVersionProvider(), Is.SameAs(provider));
        Assert.That(app.GetSettings(), Is.SameAs(settings));
        Assert.That(app.GetRootCommand().GetName(), Is.EqualTo("tool"));
        Assert.That(app.GetRootCommand().GetDescriptionText(), Is.EqualTo("Build things."));
        Assert.That(app.GetRootCommand().GetCommands(), Is.EqualTo(new ArgCommand[] { command }));
        Assert.That(app.GetRootCommand().GetOptions(), Is.EqualTo(new ArgOption[] { option }));
        Assert.That(app.GetRootCommand().GetArguments(), Is.EqualTo(new ArgArgument[] { argument }));
    }

    [Test]
    public void CommandContract_ExposesEveryFluentMethodAndAccessor()
    {
        IArgApp app = new ArgApp("tool");
        IArgCommand command = app.Command("build");
        Func<ArgContext, int> handler = ReturnSeven;
        Func<ArgContext, int> before = ReturnOne;
        Func<ArgContext, int> after = ReturnTwo;
        Func<ArgContext, ArgParseError?> commandValidator = AcceptCommand;
        Func<ArgValues, ArgParseError?> optionValidator = AcceptOptions;

        Assert.That(command.Description("Build projects."), Is.SameAs(command));
        Assert.That(command.Summary("Build"), Is.SameAs(command));
        Assert.That(command.Usage("tool build [options]"), Is.SameAs(command));
        Assert.That(command.Group("Workflows"), Is.SameAs(command));
        Assert.That(command.Alias("b"), Is.SameAs(command));
        Assert.That(command.Example("tool build src"), Is.SameAs(command));
        Assert.That(command.Hidden(), Is.SameAs(command));
        Assert.That(command.Deprecated("Use compile."), Is.SameAs(command));
        Assert.That(command.RequiredSubcommand(), Is.SameAs(command));
        Assert.That(command.Branch(), Is.SameAs(command));
        Assert.That(command.Handler(handler), Is.SameAs(command));
        Assert.That(command.Before(before), Is.SameAs(command));
        Assert.That(command.After(after), Is.SameAs(command));
        Assert.That(command.ValidateCommand(commandValidator), Is.SameAs(command));
        Assert.That(command.ValidateOptions(optionValidator), Is.SameAs(command));

        ArgCommand child = command.Command("project").Alias("p");
        Assert.That(command.DefaultCommand("p"), Is.SameAs(command));
        ArgOption local = command.Option("--local").Flag();
        ArgOption global = command.GlobalOption("--global").Flag();
        ArgArgument argument = command.Argument("target");
        ArgOptionGroup exclusive = command.MutuallyExclusiveGroup("output", new string[] { "--local", "--global" });
        ArgOptionGroup required = command.AtLeastOneRequiredGroup("input", new string[] { "--local", "--global" });

        Assert.That(command.GetName(), Is.EqualTo("build"));
        Assert.That(command.GetKind(), Is.EqualTo(ArgSymbolKind.Command));
        Assert.That(command.GetParent(), Is.SameAs(app.GetRootCommand()));
        Assert.That(command.GetAliases(), Is.EqualTo(new string[] { "b" }));
        Assert.That(command.GetCommands(), Is.EqualTo(new ArgCommand[] { child }));
        Assert.That(command.GetOptions(), Is.EqualTo(new ArgOption[] { local }));
        Assert.That(command.GetGlobalOptions(), Is.EqualTo(new ArgOption[] { global }));
        Assert.That(command.GetArguments(), Is.EqualTo(new ArgArgument[] { argument }));
        Assert.That(command.GetOptionGroups(), Is.EqualTo(new ArgOptionGroup[] { exclusive, required }));
        Assert.That(command.GetExamples(), Is.EqualTo(new string[] { "tool build src" }));
        Assert.That(command.GetCommandValidators(), Is.EqualTo(new Func<ArgContext, ArgParseError?>[] { commandValidator }));
        Assert.That(command.GetCrossOptionValidators(), Is.EqualTo(new Func<ArgValues, ArgParseError?>[] { optionValidator }));
        Assert.That(command.GetSummaryText(), Is.EqualTo("Build"));
        Assert.That(command.GetDescriptionText(), Is.EqualTo("Build projects."));
        Assert.That(command.GetUsageText(), Is.EqualTo("tool build [options]"));
        Assert.That(command.GetVersionText(), Is.EqualTo(""));
        Assert.That(command.GetGroupName(), Is.EqualTo("Workflows"));
        Assert.That(command.GetDefaultCommand(), Is.SameAs(child));
        Assert.That(command.GetDeprecatedMessage(), Is.EqualTo("Use compile."));
        Assert.That(command.GetHandler(), Is.SameAs(handler));
        Assert.That(command.GetBefore(), Is.SameAs(before));
        Assert.That(command.GetAfter(), Is.SameAs(after));
        Assert.That(command.GetIsHidden(), Is.True);
        Assert.That(command.GetRequiresSubcommand(), Is.True);
        Assert.That(command.GetIsBranch(), Is.True);
        Assert.That(exclusive.ParentCommand, Is.SameAs(command));
        Assert.That(required.ParentCommand, Is.SameAs(command));
    }

    [Test]
    public void OptionContract_ExposesEveryFluentMethodAndAccessor()
    {
        IArgCommand command = new ArgApp("tool").GetRootCommand();
        IArgOption option = command.Option("--configuration");
        Func<string, string> normalizer = Normalize;
        Func<string, ArgConvertResult?> converter = ConvertChoice;

        Assert.That(option.Alias("-c"), Is.SameAs(option));
        Assert.That(option.Description("Build configuration."), Is.SameAs(option));
        Assert.That(option.Required(), Is.SameAs(option));
        Assert.That(option.Hidden(), Is.SameAs(option));
        Assert.That(option.Value("name"), Is.SameAs(option));
        Assert.That(option.Arity(1, 2), Is.SameAs(option));
        Assert.That(option.Repeatable(), Is.SameAs(option));
        Assert.That(option.Default("Debug"), Is.SameAs(option));
        Assert.That(option.Env("CONFIGURATION"), Is.SameAs(option));
        Assert.That(option.Choice("Debug"), Is.SameAs(option));
        Assert.That(option.Normalize(normalizer), Is.SameAs(option));
        Assert.That(option.ConvertChoices(converter), Is.SameAs(option));
        Assert.That(option.ValidateChoicesAt(ArgChoiceValidationStage.Converted), Is.SameAs(option));
        Assert.That(option.Conflicts("--fast"), Is.SameAs(option));
        Assert.That(option.Requires("--project"), Is.SameAs(option));
        Assert.That(option.Deprecated("Use --mode."), Is.SameAs(option));
        Assert.That(option.Implies("--color", "always"), Is.SameAs(option));
        Assert.That(option.Complete("Release"), Is.SameAs(option));

        Assert.That(option.GetName(), Is.EqualTo("--configuration"));
        Assert.That(option.GetKind(), Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(option.GetAliases(), Is.EqualTo(new string[] { "--configuration", "-c" }));
        Assert.That(option.GetChoices(), Is.EqualTo(new string[] { "Debug" }));
        Assert.That(option.GetValueNormalizer(), Is.SameAs(normalizer));
        Assert.That(option.GetChoiceConverter(), Is.SameAs(converter));
        Assert.That(option.GetChoiceValidationStage(), Is.EqualTo(ArgChoiceValidationStage.Converted));
        Assert.That(option.GetConflictNames(), Is.EqualTo(new string[] { "--fast" }));
        Assert.That(option.GetRequiredNames(), Is.EqualTo(new string[] { "--project" }));
        Assert.That(option.GetCompletions(), Is.EqualTo(new string[] { "Release" }));
        Assert.That(option.GetImplications().Count, Is.EqualTo(1));
        Assert.That(option.GetImplications()[0].OptionName, Is.EqualTo("--color"));
        Assert.That(option.GetImplications()[0].Value, Is.EqualTo("always"));
        Assert.That(option.GetDescriptionText(), Is.EqualTo("Build configuration."));
        Assert.That(option.GetValueName(), Is.EqualTo("name"));
        Assert.That(option.GetDefaultValue(), Is.EqualTo("Debug"));
        Assert.That(option.GetEnvName(), Is.EqualTo("CONFIGURATION"));
        Assert.That(option.GetDeprecatedMessage(), Is.EqualTo("Use --mode."));
        Assert.That(option.GetValueArity().Minimum, Is.EqualTo(1));
        Assert.That(option.GetValueArity().Maximum, Is.EqualTo(2));
        Assert.That(option.GetParentCommand(), Is.SameAs(command));
        Assert.That(option.GetIsGlobal(), Is.False);
        Assert.That(option.GetIsRequired(), Is.True);
        Assert.That(option.GetIsHidden(), Is.True);
        Assert.That(option.GetIsFlag(), Is.False);
        Assert.That(option.GetIsNegatable(), Is.False);
        Assert.That(option.GetHasValue(), Is.True);
        Assert.That(option.GetValueIsOptional(), Is.False);
        Assert.That(option.GetIsRepeatable(), Is.True);
        Assert.That(option.GetIsCount(), Is.False);
        Assert.That(option.GetHasDefault(), Is.True);
        Assert.That(option.GetHasFallback(), Is.False);
        Assert.That(option.GetHasEnv(), Is.True);

        IArgOption optional = command.Option("--log");
        Assert.That(optional.OptionalValue("level"), Is.SameAs(optional));
        Assert.That(optional.Fallback("info"), Is.SameAs(optional));
        Assert.That(optional.GetValueIsOptional(), Is.True);
        Assert.That(optional.GetFallbackValue(), Is.EqualTo("info"));
        Assert.That(optional.GetHasFallback(), Is.True);

        IArgOption global = command.GlobalOption("--verbose");
        Assert.That(global.GetIsGlobal(), Is.True);
    }

    [Test]
    public void OptionModes_LastConfigurationMethodClearsIncompatibleState()
    {
        IArgOption option = new ArgApp("tool").Option("--mode");

        option.Negatable();
        Assert.That(option.GetIsNegatable(), Is.True);
        Assert.That(option.GetIsCount(), Is.False);

        option.Count();
        Assert.That(option.GetIsCount(), Is.True);
        Assert.That(option.GetIsNegatable(), Is.False);

        option.Value("value");
        Assert.That(option.GetHasValue(), Is.True);
        Assert.That(option.GetIsFlag(), Is.False);
        Assert.That(option.GetIsCount(), Is.False);
        Assert.That(option.GetIsNegatable(), Is.False);

        option.Negatable();
        option.Flag();
        Assert.That(option.GetIsFlag(), Is.True);
        Assert.That(option.GetIsCount(), Is.False);
        Assert.That(option.GetIsNegatable(), Is.False);

        option.Count();
        option.OptionalValue("value");
        Assert.That(option.GetValueIsOptional(), Is.True);
        Assert.That(option.GetIsCount(), Is.False);
        Assert.That(option.GetIsNegatable(), Is.False);

        option.Negatable();
        option.Arity(0, ArgArity.Unbounded);
        Assert.That(option.GetHasValue(), Is.True);
        Assert.That(option.GetValueIsOptional(), Is.True);
        Assert.That(option.GetIsCount(), Is.False);
        Assert.That(option.GetIsNegatable(), Is.False);
    }

    [Test]
    public void ArgumentContract_ExposesEveryFluentMethodAndAccessor()
    {
        IArgArgument argument = new ArgApp("tool").Argument("files");
        Func<string, string> normalizer = Normalize;
        Func<string, ArgConvertResult?> converter = ConvertChoice;

        Assert.That(argument.Description("Input files."), Is.SameAs(argument));
        Assert.That(argument.Required(), Is.SameAs(argument));
        Assert.That(argument.Variadic(), Is.SameAs(argument));
        Assert.That(argument.Optional(), Is.SameAs(argument));
        Assert.That(argument.Arity(2, ArgArity.Unbounded), Is.SameAs(argument));
        Assert.That(argument.Default("src"), Is.SameAs(argument));
        Assert.That(argument.Env("FILES"), Is.SameAs(argument));
        Assert.That(argument.Choice("src"), Is.SameAs(argument));
        Assert.That(argument.Normalize(normalizer), Is.SameAs(argument));
        Assert.That(argument.ConvertChoices(converter), Is.SameAs(argument));
        Assert.That(argument.ValidateChoicesAt(ArgChoiceValidationStage.Normalized), Is.SameAs(argument));
        Assert.That(argument.Deprecated("Use inputs."), Is.SameAs(argument));
        Assert.That(argument.Complete("tests"), Is.SameAs(argument));

        Assert.That(argument.GetName(), Is.EqualTo("files"));
        Assert.That(argument.GetKind(), Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(argument.GetChoices(), Is.EqualTo(new string[] { "src" }));
        Assert.That(argument.GetValueNormalizer(), Is.SameAs(normalizer));
        Assert.That(argument.GetChoiceConverter(), Is.SameAs(converter));
        Assert.That(argument.GetChoiceValidationStage(), Is.EqualTo(ArgChoiceValidationStage.Normalized));
        Assert.That(argument.GetCompletions(), Is.EqualTo(new string[] { "tests" }));
        Assert.That(argument.GetDescriptionText(), Is.EqualTo("Input files."));
        Assert.That(argument.GetDefaultValue(), Is.EqualTo("src"));
        Assert.That(argument.GetEnvName(), Is.EqualTo("FILES"));
        Assert.That(argument.GetDeprecatedMessage(), Is.EqualTo("Use inputs."));
        Assert.That(argument.GetValueArity().Minimum, Is.EqualTo(2));
        Assert.That(argument.GetValueArity().Maximum, Is.EqualTo(ArgArity.Unbounded));
        Assert.That(argument.GetIsRequired(), Is.True);
        Assert.That(argument.GetIsVariadic(), Is.True);
        Assert.That(argument.GetHasDefault(), Is.True);
        Assert.That(argument.GetHasEnv(), Is.True);
    }

    [Test]
    public void ArgumentRequirementMethods_UpdateArityWithoutIncompatibleState()
    {
        IArgArgument argument = new ArgApp("tool").Argument("files");

        argument.Variadic().Required();
        Assert.That(argument.GetIsRequired(), Is.True);
        Assert.That(argument.GetIsVariadic(), Is.True);
        Assert.That(argument.GetValueArity().Minimum, Is.EqualTo(1));
        Assert.That(argument.GetValueArity().Maximum, Is.EqualTo(ArgArity.Unbounded));

        argument.Optional();
        Assert.That(argument.GetIsRequired(), Is.False);
        Assert.That(argument.GetIsVariadic(), Is.True);
        Assert.That(argument.GetValueArity().Minimum, Is.EqualTo(0));

        argument.Arity(0, 1);
        Assert.That(argument.GetIsRequired(), Is.False);
        Assert.That(argument.GetIsVariadic(), Is.False);
    }

    [Test]
    public void PublicContracts_AreUsableThroughTheirInterfaces()
    {
        IArgApp app = new ArgApp("tool");
        IArgCommand command = app.GetRootCommand();
        IArgOption option = app.Option("--verbose").Flag();
        IArgArgument argument = app.Argument("target");
        IArgCompletionProvider completion = new ArgCompletionProvider();
        IArgDiagnostics diagnostics = new ArgDiagnostics();
        IArgEnvironment environment = new ArgSystemEnvironment();
        IArgHelpBuilder help = new ArgHelpBuilder();
        IArgMessageProvider messages = new ArgEnglishMessageProvider();
        IArgResultStatus status = new ArgResultStatus();
        ArgParseResult parseResult = app.Parse(new string[] { "linux" });
        IArgValues values = new ArgValues(parseResult);

        Assert.That(command.GetKind(), Is.EqualTo(ArgSymbolKind.Command));
        Assert.That(option.GetKind(), Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(argument.GetKind(), Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(completion.GetSettings(), Is.Not.Null);
        Assert.That(completion.Complete(app.GetRootCommand(), new string[] { "" }), Is.Not.Null);
        Assert.That(completion.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Bash), Does.Contain("_tool_completion"));
        Assert.That(diagnostics.Describe((ArgApp)app), Does.Contain("command tool"));
        Assert.That(diagnostics.DescribeCommand(app.GetRootCommand()), Does.Contain("command tool"));
        Assert.That(diagnostics.MetadataJson((ArgApp)app), Does.Contain("\"name\":\"tool\""));
        Assert.That(diagnostics.CommandMetadataJson(app.GetRootCommand()), Does.Contain("\"name\":\"tool\""));
        Assert.That(environment, Is.TypeOf<ArgSystemEnvironment>());
        Assert.That(help.UseSettings(app.GetSettings()), Is.SameAs(help));
        Assert.That(help.Width(60), Is.SameAs(help));
        Assert.That(help.GetHelpWidth(), Is.EqualTo(60));
        Assert.That(help.GetSettings(), Is.SameAs(app.GetSettings()));
        Assert.That(help.Version((ArgApp)app), Is.EqualTo(""));
        Assert.That(help.CommandVersion(app.GetRootCommand()), Is.EqualTo(""));
        Assert.That(help.RootHelp((ArgApp)app), Does.StartWith("Usage: tool"));
        Assert.That(help.Help(app.GetRootCommand()), Does.StartWith("Usage: tool"));
        Assert.That(help.UsageLine(app.GetRootCommand()), Does.StartWith("Usage: tool"));
        Assert.That(messages.GetMessage(ArgMessageKeys.HelpOptionDescription, new string[] { }), Is.EqualTo("Show help."));
        Assert.That(status.ParseFailed(parseResult), Is.False);
        Assert.That(status.ValidationFailed(app.Validate(new string[] { "linux" })), Is.False);
        Assert.That(status.ConversionFailed(new ArgConvertResult(true, "ok", "")), Is.False);
        Assert.That(status.RunFailed(app.Run(new string[] { "linux" })), Is.False);
        Assert.That(values.GetParseResult(), Is.SameAs(parseResult));
        Assert.That(values.Has("target"), Is.True);
    }

    [Test]
    public void ApplicationAndCommand_RejectInvalidPublicInputs()
    {
        Assert.That(new TestDelegate(delegate { new ArgApp(""); }), Throws.TypeOf<ArgumentException>());
        Assert.That(new TestDelegate(delegate { new ArgApp("bad name"); }), Throws.TypeOf<ArgumentException>());

        IArgApp app = new ArgApp("tool");
        Assert.That(new TestDelegate(delegate { app.UseSettings(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { app.Version(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { app.VersionProvider(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { app.Parse(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { app.Validate(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { app.Run(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { app.Command("--build"); }), Throws.TypeOf<ArgumentException>());
        Assert.That(new TestDelegate(delegate { app.Option("verbose"); }), Throws.TypeOf<ArgumentException>());
        Assert.That(new TestDelegate(delegate { app.Argument("bad name"); }), Throws.TypeOf<ArgumentException>());

        IArgCommand root = app.GetRootCommand();
        root.Command("build").Alias("b");
        root.Option("--verbose").Alias("-v");
        root.Argument("target");
        root.MutuallyExclusiveGroup("format", new string[] { "--verbose" });

        Assert.That(new TestDelegate(delegate { root.Command("b"); }), Throws.TypeOf<InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { root.Option("-v"); }), Throws.TypeOf<InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { root.Argument("target"); }), Throws.TypeOf<InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { root.DefaultCommand("missing"); }), Throws.TypeOf<InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { root.ValidateCommand(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { root.ValidateOptions(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { root.MutuallyExclusiveGroup("", new string[] { "--verbose" }); }), Throws.TypeOf<ArgumentException>());
        Assert.That(new TestDelegate(delegate { root.AtLeastOneRequiredGroup("format", new string[] { "--verbose" }); }), Throws.TypeOf<InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { root.AtLeastOneRequiredGroup("input", null!); }), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void OptionAndArgument_RejectInvalidPublicInputs()
    {
        IArgCommand command = new ArgApp("tool").GetRootCommand();
        IArgOption option = command.Option("--value");
        IArgArgument argument = command.Argument("value");

        Assert.That(new TestDelegate(delegate { option.Alias("invalid"); }), Throws.TypeOf<ArgumentException>());
        Assert.That(new TestDelegate(delegate { option.Alias("--value"); }), Throws.TypeOf<InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { option.Arity(-1, 1); }), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(new TestDelegate(delegate { option.Arity(2, 1); }), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(new TestDelegate(delegate { option.Normalize(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { option.ConvertChoices(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { option.ValidateChoicesAt((ArgChoiceValidationStage)99); }), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(new TestDelegate(delegate { option.Conflicts("invalid"); }), Throws.TypeOf<ArgumentException>());
        Assert.That(new TestDelegate(delegate { option.Requires("invalid"); }), Throws.TypeOf<ArgumentException>());
        Assert.That(new TestDelegate(delegate { option.Implies("invalid", "yes"); }), Throws.TypeOf<ArgumentException>());
        Assert.That(new TestDelegate(delegate { option.Implies("--other", null!); }), Throws.TypeOf<ArgumentNullException>());

        Assert.That(new TestDelegate(delegate { argument.Arity(-1, 1); }), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(new TestDelegate(delegate { argument.Arity(2, 1); }), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(new TestDelegate(delegate { argument.Normalize(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { argument.ConvertChoices(null!); }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(new TestDelegate(delegate { argument.ValidateChoicesAt((ArgChoiceValidationStage)99); }), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static string ProvideVersion()
    {
        return "2.0.0";
    }

    static int ReturnSeven(ArgContext context)
    {
        return 7;
    }

    static int ReturnOne(ArgContext context)
    {
        return 1;
    }

    static int ReturnTwo(ArgContext context)
    {
        return 2;
    }

    static ArgParseError? AcceptCommand(ArgContext context)
    {
        return null;
    }

    static ArgParseError? AcceptOptions(ArgValues values)
    {
        return null;
    }

    static string Normalize(string value)
    {
        return value.ToUpperInvariant();
    }

    static ArgConvertResult? ConvertChoice(string value)
    {
        return new ArgConvertResult(true, value.Length, "");
    }
}
