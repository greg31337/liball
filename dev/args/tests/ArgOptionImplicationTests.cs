using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgOptionImplicationTests
{
    [Test]
    public void Option_ExposesPublicImplicationConfiguration()
    {
        ArgApp app = new ArgApp("tool");
        IArgOption option = app.Option("--production");

        ArgOption returned = option.Implies("--log-level", "warning");

        Assert.That(returned, Is.SameAs(option));
        Assert.That(option.GetImplications().Count, Is.EqualTo(1));
        Assert.That(option.GetImplications()[0].OptionName, Is.EqualTo("--log-level"));
        Assert.That(option.GetImplications()[0].Value, Is.EqualTo("warning"));
    }

    [Test]
    public void Parse_SuppliedOptionAddsImpliedValue()
    {
        ArgApp app = ImplicationApp();

        ArgParseResult result = app.Parse(new string[] { "--production" });
        ArgValues values = new ArgValues(result);

        Assert.That(result.Errors, Is.Empty);
        Assert.That(values.String("--log-level"), Is.EqualTo("warning"));
        Assert.That(values.Source("--log-level"), Is.EqualTo(ArgValueSource.Implied));
        Assert.That(values.IsSupplied("--log-level"), Is.False);
        Assert.That(values.Occurrence("--log-level")!.TokenIndex, Is.EqualTo(-1));
    }

    [Test]
    public void Parse_CommandLineTargetTakesPrecedenceOverImplication()
    {
        ArgApp app = ImplicationApp();

        ArgParseResult result = app.Parse(new string[] { "--production", "--log-level", "debug" });
        ArgValues values = new ArgValues(result);

        Assert.That(values.String("--log-level"), Is.EqualTo("debug"));
        Assert.That(values.Source("--log-level"), Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(values.Occurrences("--log-level").Count, Is.EqualTo(1));
    }

    [Test]
    public void Parse_FallbackTargetTakesPrecedenceOverImplication()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--production").Flag().Implies("--color", "always");
        app.Option("--color").OptionalValue("mode").Fallback("auto");

        ArgValues values = new ArgValues(app.Parse(new string[] { "--production", "--color" }));

        Assert.That(values.String("--color"), Is.EqualTo("auto"));
        Assert.That(values.Source("--color"), Is.EqualTo(ArgValueSource.Fallback));
        Assert.That(values.Occurrences("--color").Count, Is.EqualTo(1));
    }

    [Test]
    public void Parse_ImplicationTakesPrecedenceOverEnvironmentAndDefault()
    {
        string environmentName = "ARGS_TEST_IMPLIED_LOG_LEVEL";
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = new TestArgEnvironment(environmentName, "info");
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--production").Flag().Implies("--log-level", "warning");
        app.Option("--log-level").Value("level").Env(environmentName).Default("debug");

        ArgValues implied = new ArgValues(app.Parse(new string[] { "--production" }));
        ArgValues environment = new ArgValues(app.Parse(new string[] { }));

        Assert.That(implied.String("--log-level"), Is.EqualTo("warning"));
        Assert.That(implied.Source("--log-level"), Is.EqualTo(ArgValueSource.Implied));
        Assert.That(environment.String("--log-level"), Is.EqualTo("info"));
        Assert.That(environment.Source("--log-level"), Is.EqualTo(ArgValueSource.Environment));
    }

    [Test]
    public void Parse_ImplicationTakesPrecedenceOverDefaultWithoutEnvironment()
    {
        ArgApp app = ImplicationApp();
        app.GetRootCommand().GetOptions()[1].Default("debug");

        ArgValues implied = new ArgValues(app.Parse(new string[] { "--production" }));
        ArgValues defaulted = new ArgValues(app.Parse(new string[] { }));

        Assert.That(implied.String("--log-level"), Is.EqualTo("warning"));
        Assert.That(implied.Source("--log-level"), Is.EqualTo(ArgValueSource.Implied));
        Assert.That(defaulted.String("--log-level"), Is.EqualTo("debug"));
        Assert.That(defaulted.Source("--log-level"), Is.EqualTo(ArgValueSource.Default));
    }

    [Test]
    public void Parse_ChainedAndCyclicImplicationsTerminateDeterministically()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--first").Flag().Implies("--second", "two");
        app.Option("--second").Value("value").Implies("--third", "three");
        app.Option("--third").Value("value").Implies("--first", "true");

        ArgParseResult result = app.Parse(new string[] { "--first" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--first"));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("--second"));
        Assert.That(result.Occurrences[1].Source, Is.EqualTo(ArgValueSource.Implied));
        Assert.That(result.Occurrences[2].SymbolName, Is.EqualTo("--third"));
        Assert.That(result.Occurrences[2].Source, Is.EqualTo(ArgValueSource.Implied));
    }

    [Test]
    public void Parse_DefaultedOrEnvironmentSourceDoesNotTriggerImplication()
    {
        string environmentName = "ARGS_TEST_IMPLIED_SOURCE";
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = new TestArgEnvironment(environmentName, "true");
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--default-source").Value("enabled").Default("true").Implies("--target", "default");
        app.Option("--environment-source").Value("enabled").Env(environmentName).Implies("--target", "environment");
        app.Option("--target").Value("value");

        ArgValues values = new ArgValues(app.Parse(new string[] { }));

        Assert.That(values.Has("--default-source"), Is.True);
        Assert.That(values.Has("--environment-source"), Is.True);
        Assert.That(values.Has("--target"), Is.False);
    }

    [Test]
    public void Parse_LocalSourceCanImplyInheritedGlobalTargetByAlias()
    {
        ArgApp app = new ArgApp("tool");
        app.GetRootCommand().GlobalOption("--log-level").Alias("-l").Value("level");
        ArgCommand build = app.Command("build");
        build.Option("--production").Flag().Implies("-l", "warning");

        ArgValues values = new ArgValues(app.Parse(new string[] { "build", "--production" }));

        Assert.That(values.String("--log-level"), Is.EqualTo("warning"));
        Assert.That(values.Source("-l"), Is.EqualTo(ArgValueSource.Implied));
    }

    [Test]
    public void Validate_ImpliedFlagValueSatisfiesValidationWithoutArityError()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--production").Flag().Implies("--color", "true");
        app.Option("--color").Flag().Required();

        ArgValidationResult result = app.Validate(new string[] { "--production" });
        ArgValues values = new ArgValues(result.ParseResult);

        Assert.That(result.Errors, Is.Empty);
        Assert.That(values.Bool("--color"), Is.True);
        Assert.That(values.Source("--color"), Is.EqualTo(ArgValueSource.Implied));
    }

    [Test]
    public void Validate_ImpliedValueTakingOptionStillUsesConfiguredArity()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--source").Flag().Implies("--pair", "left");
        app.Option("--pair").Arity(2, 2);

        ArgValidationResult result = app.Validate(new string[] { "--source" });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("too_few_values"));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("--pair"));
    }

    [Test]
    public void Validate_ImpliedOptionParticipatesInGroupsAndDependencies()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--production").Flag().Implies("--color", "always").Requires("--color");
        app.Option("--color").Value("mode");
        app.GetRootCommand().AtLeastOneRequiredGroup("display", new string[] { "--color" });

        ArgValidationResult result = app.Validate(new string[] { "--production" });

        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Diagnostics_IncludeConfiguredImplications()
    {
        ArgApp app = ImplicationApp();

        string diagnostics = new ArgDiagnostics().Describe(app);

        Assert.That(diagnostics, Does.Contain("implies: --log-level=warning"));
    }

    [Test]
    public void Option_ImpliesRejectsInvalidTargetOrNullValue()
    {
        ArgOption option = new ArgApp("tool").Option("--production");

        Assert.That(new TestDelegate(() => option.Implies("invalid", "value")), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(() => option.Implies("--target", null!)), Throws.TypeOf<System.ArgumentNullException>());
    }

    static ArgApp ImplicationApp()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--production").Flag().Implies("--log-level", "warning");
        app.Option("--log-level").Value("level");
        return app;
    }
}
