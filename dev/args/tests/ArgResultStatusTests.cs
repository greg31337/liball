using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgResultStatusTests
{
    [Test]
    public void ParseFailed_DistinguishesSuccessfulAndErroneousParseResults()
    {
        IArgResultStatus status = new ArgResultStatus();
        ArgApp app = new ArgApp("tool");
        ArgParseResult successful = app.Parse(new string[] { });
        ArgParseResult failed = app.Parse(new string[] { "--unknown" });

        Assert.That(status.ParseFailed(successful), Is.False);
        Assert.That(status.ParseFailed(failed), Is.True);
    }

    [Test]
    public void ValidationFailed_IncludesParseAndValidationErrorsButNotShortCircuits()
    {
        ArgResultStatus status = new ArgResultStatus();
        ArgApp app = new ArgApp("tool");
        app.Option("--required").Required().Value("value");

        ArgValidationResult missing = app.Validate(new string[] { });
        ArgValidationResult parseError = app.Validate(new string[] { "--unknown" });
        ArgValidationResult help = app.Validate(new string[] { "--help" });
        ArgValidationResult successful = app.Validate(new string[] { "--required", "value" });

        Assert.That(status.ValidationFailed(missing), Is.True);
        Assert.That(status.ValidationFailed(parseError), Is.True);
        Assert.That(status.ValidationFailed(help), Is.False);
        Assert.That(status.ValidationFailed(successful), Is.False);
    }

    [Test]
    public void ConversionQueries_ReportIndividualAndAccumulatedFailures()
    {
        ArgResultStatus status = new ArgResultStatus();
        ArgConvertResult successful = new ArgConvertResult(true, 42, "");
        ArgConvertResult failed = new ArgConvertResult(false, null, "Invalid integer.");
        ArgApp app = new ArgApp("tool");
        app.Option("--count").Value("value");
        IArgValues values = new ArgValues(app.Parse(new string[] { "--count", "many" }));

        Assert.That(status.ConversionFailed(successful), Is.False);
        Assert.That(status.ConversionFailed(failed), Is.True);
        Assert.That(values.HasConversionErrors(), Is.False);

        values.Int("--count");

        Assert.That(values.HasConversionErrors(), Is.True);

        values.ClearConversionErrors();

        Assert.That(values.HasConversionErrors(), Is.False);
    }

    [Test]
    public void RunFailed_IncludesValidationErrorsAndNonzeroExitCodesButNotHelp()
    {
        ArgResultStatus status = new ArgResultStatus();
        ArgApp successfulApp = new ArgApp("successful");
        successfulApp.RootCommand.Handler(delegate(ArgContext context) { return 0; });
        ArgApp nonzeroApp = new ArgApp("nonzero");
        nonzeroApp.RootCommand.Handler(delegate(ArgContext context) { return 7; });
        ArgApp invalidApp = new ArgApp("invalid");
        invalidApp.Option("--required").Required().Value("value");

        ArgRunResult successful = successfulApp.Run(new string[] { });
        ArgRunResult nonzero = nonzeroApp.Run(new string[] { });
        ArgRunResult invalid = invalidApp.Run(new string[] { });
        ArgRunResult help = invalidApp.Run(new string[] { "--help" });

        Assert.That(status.RunFailed(successful), Is.False);
        Assert.That(status.RunFailed(nonzero), Is.True);
        Assert.That(status.RunFailed(invalid), Is.True);
        Assert.That(status.RunFailed(help), Is.False);
    }

    [Test]
    public void Queries_RejectNullResults()
    {
        ArgResultStatus status = new ArgResultStatus();

        Assert.Throws<System.ArgumentNullException>(delegate { status.ParseFailed(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { status.ValidationFailed(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { status.ConversionFailed(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { status.RunFailed(null!); });
    }
}
