using NUnit.Framework;

namespace args.Tests;

public enum ArgConversionErrorMode
{
    Debug,
    Release
}

[TestFixture]
public class ArgConversionErrorReportingTests
{
    [Test]
    public void Int_InvalidValueRecordsExpectedTypeAndActualValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--count").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--count", "many" });
        ArgValues values = new ArgValues(parseResult);

        int converted = values.Int("--count");

        Assert.That(converted, Is.EqualTo(0));
        AssertConversionError(values.LastConversionResult, "int", "many");
        Assert.That(values.ConversionErrors.Count, Is.EqualTo(1));
        Assert.That(values.ConversionErrors[0], Is.SameAs(values.LastConversionResult));
    }

    [Test]
    public void Bool_InvalidValueRecordsExpectedTypeAndActualValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--enabled").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--enabled", "maybe" });
        ArgValues values = new ArgValues(parseResult);

        bool converted = values.Bool("--enabled");

        Assert.That(converted, Is.False);
        AssertConversionError(values.LastConversionResult, "bool", "maybe");
    }

    [Test]
    public void EnumValue_InvalidValueRecordsEnumTypeAndActualValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--mode").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--mode", "staging" });
        ArgValues values = new ArgValues(parseResult);

        object? converted = values.EnumValue("--mode", typeof(ArgConversionErrorMode));

        Assert.That(converted, Is.Null);
        AssertConversionError(values.LastConversionResult, "enum ArgConversionErrorMode", "staging");
    }

    [Test]
    public void ValidConversionUpdatesLastResultWithoutAddingConversionError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--ratio").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--ratio", "2.5" });
        ArgValues values = new ArgValues(parseResult);

        double converted = values.Double("--ratio");

        Assert.That(converted, Is.EqualTo(2.5d));
        Assert.That(values.LastConversionResult.Success, Is.True);
        Assert.That(values.LastConversionResult.ExpectedType, Is.EqualTo("double"));
        Assert.That(values.LastConversionResult.ActualValue, Is.EqualTo("2.5"));
        Assert.That(values.LastConversionResult.Error, Is.EqualTo(""));
        Assert.That(values.ConversionErrors, Is.Empty);
    }

    [Test]
    public void ConversionErrorsAccumulateAndCanBeCleared()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--count").Value("value");
        command.Option("--duration").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--count", "many", "--duration", "later" });
        ArgValues values = new ArgValues(parseResult);

        values.Int("--count");
        values.TimeSpan("--duration");

        Assert.That(values.ConversionErrors.Count, Is.EqualTo(2));
        AssertConversionError(values.ConversionErrors[0], "int", "many");
        AssertConversionError(values.ConversionErrors[1], "TimeSpan", "later");

        values.ClearConversionErrors();

        Assert.That(values.ConversionErrors, Is.Empty);
    }

    [Test]
    public void BuiltInConversionFailuresRecordExpectedTypeAndActualValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--long").Value("value");
        command.Option("--decimal").Value("value");
        command.Option("--date").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--long", "many", "--decimal", "several", "--date", "later" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.Long("--long"), Is.EqualTo(0L));
        Assert.That(values.Decimal("--decimal"), Is.EqualTo(0m));
        Assert.That(values.DateTime("--date"), Is.EqualTo(new System.DateTime()));

        Assert.That(values.ConversionErrors.Count, Is.EqualTo(3));
        AssertConversionError(values.ConversionErrors[0], "long", "many");
        AssertConversionError(values.ConversionErrors[1], "decimal", "several");
        AssertConversionError(values.ConversionErrors[2], "DateTime", "later");
    }

    [Test]
    public void MissingScalarValueRecordsConversionFailure()
    {
        ArgValues values = ArgValues.Empty();

        int converted = values.Int("--missing");

        Assert.That(converted, Is.EqualTo(0));
        Assert.That(values.ConversionErrors.Count, Is.EqualTo(1));
        AssertConversionError(values.LastConversionResult, "int", "");
    }

    [Test]
    public void ConvertResult_EmptyExposesEmptyFailure()
    {
        ArgConvertResult result = ArgConvertResult.Empty();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo(""));
        Assert.That(result.ExpectedType, Is.EqualTo(""));
        Assert.That(result.ActualValue, Is.EqualTo(""));
    }

    static void AssertConversionError(ArgConvertResult result, string expectedType, string actualValue)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.ExpectedType, Is.EqualTo(expectedType));
        Assert.That(result.ActualValue, Is.EqualTo(actualValue));
        Assert.That(result.Error, Is.EqualTo("Expected " + expectedType + " value but got '" + actualValue + "'."));
    }
}
