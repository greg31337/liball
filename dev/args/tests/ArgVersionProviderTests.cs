using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgVersionProviderTests
{
    [Test]
    public void Configuration_StoresProviderAndUsesLastStaticOrDynamicVersionSetting()
    {
        ArgApp app = new ArgApp("tool");
        System.Func<string> provider = delegate { return "2.0.0-dynamic"; };

        Assert.That(app.VersionProvider(provider), Is.SameAs(app));
        Assert.That(app.GetVersionProvider(), Is.SameAs(provider));
        Assert.That(app.GetVersionText(), Is.EqualTo("2.0.0-dynamic"));
        Assert.That(app.VersionText, Is.EqualTo(""));
        Assert.That(app.RootCommand.VersionText, Is.EqualTo(""));
        Assert.That(app.RootCommand.VersionProviderFunc, Is.SameAs(provider));

        app.Version("3.0.0-static");

        Assert.That(app.GetVersionProvider(), Is.Null);
        Assert.That(app.GetVersionText(), Is.EqualTo("3.0.0-static"));
        Assert.That(app.RootCommand.VersionProviderFunc, Is.Null);

        app.VersionProvider(provider);

        Assert.That(app.GetVersionText(), Is.EqualTo("2.0.0-dynamic"));
        Assert.That(app.VersionText, Is.EqualTo(""));
    }

    [Test]
    public void ValidateAndRun_SnapshotOneProviderValuePerParseOperation()
    {
        ArgApp app = new ArgApp("tool");
        int calls = 0;
        app.VersionProvider(delegate
        {
            calls = calls + 1;
            return "1.0." + calls.ToString(System.Globalization.CultureInfo.InvariantCulture);
        });

        ArgValidationResult validation = app.Validate(new string[] { "--version" });

        Assert.That(validation.VersionRequested, Is.True);
        Assert.That(validation.VersionText, Is.EqualTo("1.0.1"));
        Assert.That(calls, Is.EqualTo(1));

        ArgRunResult run = app.Run(new string[] { "--version" });

        Assert.That(run.ExitCode, Is.EqualTo(0));
        Assert.That(run.HandlerInvoked, Is.False);
        Assert.That(run.ValidationResult.VersionRequested, Is.True);
        Assert.That(run.ValidationResult.VersionText, Is.EqualTo("1.0.2"));
        Assert.That(run.VersionText, Is.EqualTo("1.0.2"));
        Assert.That(calls, Is.EqualTo(2));
    }

    [Test]
    public void HelpAndCompletion_UseProviderForTextAndDefaultVersionAvailability()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        app.VersionProvider(delegate { return "9.8.7"; });
        ArgHelpBuilder helpBuilder = new ArgHelpBuilder();

        string rootHelp = helpBuilder.RootHelp(app);
        System.Collections.Generic.List<ArgCompletion> completions = new ArgCompletionProvider().Complete(build, new string[] { "--v" });

        Assert.That(helpBuilder.Version(app), Is.EqualTo("9.8.7"));
        Assert.That(helpBuilder.CommandVersion(build), Is.EqualTo("9.8.7"));
        Assert.That(rootHelp, Does.Contain("--version, -V - Show version."));
        Assert.That(completions.Count, Is.EqualTo(1));
        Assert.That(completions[0].Text, Is.EqualTo("--version"));
    }

    [Test]
    public void EmptyProviderTextDisablesDefaultVersionParsingHelpAndCompletion()
    {
        ArgApp app = new ArgApp("tool");
        app.VersionProvider(delegate { return ""; });

        ArgParseResult parse = app.Parse(new string[] { "--version" });
        string help = new ArgHelpBuilder().RootHelp(app);
        System.Collections.Generic.List<ArgCompletion> completions = new ArgCompletionProvider().Complete(app.RootCommand, new string[] { "--v" });

        Assert.That(parse.Errors.Count, Is.EqualTo(1));
        Assert.That(parse.Errors[0].Code, Is.EqualTo("unknown_option"));
        Assert.That(help, Does.Not.Contain("--version"));
        Assert.That(completions, Is.Empty);
    }

    [Test]
    public void Configuration_RejectsNullProviderTextAndArguments()
    {
        ArgApp app = new ArgApp("tool");

        Assert.Throws<System.ArgumentNullException>(delegate { app.Version(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { app.VersionProvider(null!); });

        app.VersionProvider(ReturnNull);

        Assert.Throws<System.InvalidOperationException>(delegate { app.GetVersionText(); });
        Assert.Throws<System.InvalidOperationException>(delegate { app.Parse(new string[] { }); });
        Assert.Throws<System.InvalidOperationException>(delegate { new ArgHelpBuilder().RootHelp(app); });
        Assert.Throws<System.InvalidOperationException>(delegate { new ArgCompletionProvider().Complete(app.RootCommand, new string[] { "" }); });
    }

    static string ReturnNull()
    {
        return null!;
    }
}
