using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCustomValueConverterTests
{
    [Test]
    public void Register_AddsCustomConverterByLogicalTypeName()
    {
        ArgValueConverter converter = new ArgValueConverter();

        converter.Register("upper", new System.Func<string, ArgConvertResult>(ConvertUpper));
        ArgConvertResult result = converter.ConvertCustom("upper", "alpha");

        Assert.That(converter.HasCustom("upper"), Is.True);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("ALPHA"));
        Assert.That(result.ExpectedType, Is.EqualTo("upper"));
        Assert.That(result.ActualValue, Is.EqualTo("alpha"));
        Assert.That(result.Error, Is.EqualTo(""));
    }

    [Test]
    public void Register_ReplacesExistingCustomConverter()
    {
        ArgValueConverter converter = new ArgValueConverter();

        converter.Register("case", new System.Func<string, ArgConvertResult>(ConvertUpper));
        converter.Register("case", new System.Func<string, ArgConvertResult>(ConvertLower));
        ArgConvertResult result = converter.ConvertCustom("case", "Alpha");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("alpha"));
    }

    [Test]
    public void Custom_UsesRegisteredConverterAndRecordsSuccess()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--name").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--name", "alpha" });
        ArgValues values = new ArgValues(parseResult);

        values.RegisterConverter("upper", new System.Func<string, ArgConvertResult>(ConvertUpper));
        object? converted = values.Custom("--name", "upper");

        Assert.That(converted, Is.EqualTo("ALPHA"));
        Assert.That(values.LastConversionResult.Success, Is.True);
        Assert.That(values.LastConversionResult.ExpectedType, Is.EqualTo("upper"));
        Assert.That(values.LastConversionResult.ActualValue, Is.EqualTo("alpha"));
        Assert.That(values.ConversionErrors, Is.Empty);
    }

    [Test]
    public void Custom_FailureRecordsExpectedTypeAndActualValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--color", "blue" });
        ArgValues values = new ArgValues(parseResult);

        values.RegisterConverter("color", new System.Func<string, ArgConvertResult>(RejectColor));
        object? converted = values.Custom("--color", "color");

        Assert.That(converted, Is.Null);
        Assert.That(values.ConversionErrors.Count, Is.EqualTo(1));
        Assert.That(values.LastConversionResult.ExpectedType, Is.EqualTo("color"));
        Assert.That(values.LastConversionResult.ActualValue, Is.EqualTo("blue"));
        Assert.That(values.LastConversionResult.Error, Is.EqualTo("Color rejected."));
    }

    [Test]
    public void Custom_MissingConverterRecordsStandardFailure()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--color", "blue" });
        ArgValues values = new ArgValues(parseResult);

        object? converted = values.Custom("--color", "color");

        Assert.That(converted, Is.Null);
        Assert.That(values.ConversionErrors.Count, Is.EqualTo(1));
        Assert.That(values.LastConversionResult.Success, Is.False);
        Assert.That(values.LastConversionResult.ExpectedType, Is.EqualTo("color"));
        Assert.That(values.LastConversionResult.ActualValue, Is.EqualTo("blue"));
        Assert.That(values.LastConversionResult.Error, Is.EqualTo("Expected color value but got 'blue'."));
    }

    [Test]
    public void Custom_NullConverterResultIsNormalizedToFailure()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--color", "blue" });
        ArgValues values = new ArgValues(parseResult);

        values.RegisterConverter("color", new System.Func<string, ArgConvertResult?>(ReturnNull));
        ArgConvertResult result = values.CustomResult("--color", "color");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.ExpectedType, Is.EqualTo("color"));
        Assert.That(result.ActualValue, Is.EqualTo("blue"));
        Assert.That(result.Error, Is.EqualTo("Expected color value but got 'blue'."));
    }

    static ArgConvertResult ConvertUpper(string value)
    {
        return new ArgConvertResult(true, value.ToUpperInvariant(), "");
    }

    static ArgConvertResult ConvertLower(string value)
    {
        return new ArgConvertResult(true, value.ToLowerInvariant(), "");
    }

    static ArgConvertResult RejectColor(string value)
    {
        return new ArgConvertResult(false, null, "Color rejected.");
    }

    static ArgConvertResult? ReturnNull(string value)
    {
        return null;
    }
}
