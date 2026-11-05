using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgAppEntryPointTests
{
    [Test]
    public void Parse_ReturnsParseResultWithoutValidationOrInvocation()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--configuration").Required().Value("name");
        bool called = false;
        app.RootCommand.Handler(delegate(ArgContext context)
        {
            called = true;
            return 0;
        });

        ArgParseResult parseResult = app.Parse(new string[] { });

        Assert.That(called, Is.False);
        Assert.That(parseResult.Command, Is.SameAs(app.RootCommand));
        Assert.That(parseResult.HasErrors(), Is.False);
        Assert.That(parseResult.Occurrences, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsValidationResultWithoutInvocation()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--configuration").Required().Value("name");
        bool called = false;
        app.RootCommand.Handler(delegate(ArgContext context)
        {
            called = true;
            return 0;
        });

        ArgValidationResult validationResult = app.Validate(new string[] { });

        Assert.That(called, Is.False);
        Assert.That(validationResult.ParseResult.Command, Is.SameAs(app.RootCommand));
        Assert.That(validationResult.Errors.Count, Is.EqualTo(1));
        Assert.That(validationResult.Errors[0].Code, Is.EqualTo("missing_required_option"));
    }

    [Test]
    public void Run_ParsesValidatesAndInvokes()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--configuration").Value("name");
        app.RootCommand.Handler(delegate(ArgContext context)
        {
            if (context.Values.String("--configuration") == "Release")
            {
                return 6;
            }

            return 1;
        });

        ArgRunResult runResult = app.Run(new string[] { "--configuration", "Release" });

        Assert.That(runResult.ExitCode, Is.EqualTo(6));
        Assert.That(runResult.HandlerInvoked, Is.True);
        Assert.That(runResult.ParseResult.Command, Is.SameAs(app.RootCommand));
        Assert.That(runResult.ValidationResult.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_ResolvesSubcommandsAndValues()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        build.Argument("project").Required();

        ArgParseResult parseResult = app.Parse(new string[] { "build", "src" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(parseResult.Command, Is.SameAs(build));
        Assert.That(parseResult.CommandPath, Is.EqualTo(new ArgCommand[] { app.RootCommand, build }));
        Assert.That(values.String("project"), Is.EqualTo("src"));
    }

    [Test]
    public void UseSettings_UsesConfiguredParserBehavior()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.OptionsCaseInsensitive = true;
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--configuration").Value("name");

        ArgParseResult parseResult = app.Parse(new string[] { "--CONFIGURATION", "Release" });

        Assert.That(app.Settings, Is.SameAs(settings));
        Assert.That(parseResult.HasErrors(), Is.False);
        Assert.That(new ArgValues(parseResult).String("--configuration"), Is.EqualTo("Release"));
    }

    [Test]
    public void Parse_UseSettingsUsesConfiguredEnvironmentReader()
    {
        string name = "CONFIGURATION";
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = new TestArgEnvironment(name, "Release");
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--configuration").Value("name").Env(name);

        ArgParseResult parseResult = app.Parse(new string[] { });

        Assert.That(new ArgValues(parseResult).String("--configuration"), Is.EqualTo("Release"));
        Assert.That(new ArgValues(parseResult).Source("--configuration"), Is.EqualTo(ArgValueSource.Environment));
    }

    [Test]
    public void UseSettings_RejectsNullSettings()
    {
        Assert.Throws<System.ArgumentNullException>(delegate { new ArgApp("tool").UseSettings(null!); });
    }
}
