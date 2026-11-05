using NUnit.Framework;

namespace args.Tests;

public enum ArgValuesArrayMode
{
    Debug,
    Release
}

[TestFixture]
public class ArgValuesArrayConversionTests
{
    [Test]
    public void NumericArrays_FlattenOccurrencesPreserveOrderAndRecordEachFailure()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--int").Alias("-i").Arity(1, 3).Repeatable();
        app.Option("--long").Arity(1, 2);
        app.Option("--double").Arity(1, 2);
        app.Option("--decimal").Arity(1, 2);
        ArgValues values = new ArgValues(app.Parse(new string[] { "--int", "1", "bad-int", "3", "-i", "4", "--long", "5", "bad-long", "--double", "1.5", "bad-double", "--decimal", "bad-decimal", "2.5" }));

        Assert.That(values.Ints("-i"), Is.EqualTo(new int[] { 1, 0, 3, 4 }));
        Assert.That(values.Longs("--long"), Is.EqualTo(new long[] { 5L, 0L }));
        Assert.That(values.Doubles("--double"), Is.EqualTo(new double[] { 1.5d, 0d }));
        Assert.That(values.Decimals("--decimal"), Is.EqualTo(new decimal[] { 0m, 2.5m }));
        Assert.That(values.GetConversionErrors().Count, Is.EqualTo(4));
        AssertFailure(values.GetConversionErrors()[0], "int", "bad-int");
        AssertFailure(values.GetConversionErrors()[1], "long", "bad-long");
        AssertFailure(values.GetConversionErrors()[2], "double", "bad-double");
        AssertFailure(values.GetConversionErrors()[3], "decimal", "bad-decimal");
        Assert.That(values.GetLastConversionResult().Success, Is.True);
        Assert.That(values.GetLastConversionResult().ActualValue, Is.EqualTo("2.5"));
    }

    [Test]
    public void BoolArrays_ConvertTextAndEmitValuesForEachFlagOccurrence()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--bool").Arity(1, 3).Normalize(TrimLower);
        app.Option("--verbose").Flag().Repeatable();
        app.Option("--color").Negatable().Repeatable();
        ArgValues values = new ArgValues(app.Parse(new string[] { "--bool", " YES ", "maybe", "off", "--verbose", "--verbose", "--color", "--no-color" }));

        Assert.That(values.Bools("--bool"), Is.EqualTo(new bool[] { true, false, false }));
        Assert.That(values.Bools("--verbose"), Is.EqualTo(new bool[] { true, true }));
        Assert.That(values.Bools("--color"), Is.EqualTo(new bool[] { true, false }));
        Assert.That(values.GetConversionErrors().Count, Is.EqualTo(1));
        AssertFailure(values.GetConversionErrors()[0], "bool", "maybe");
    }

    [Test]
    public void TemporalArrays_UseConfiguredCultureAndRetainFailedPositions()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.ConversionCulture = new System.Globalization.CultureInfo("fr-FR");
        ArgApp app = new ArgApp("tool");
        app.UseSettings(settings);
        app.Option("--date").Arity(1, 2);
        app.Option("--duration").Arity(1, 2);
        ArgValues values = new ArgValues(app.Parse(new string[] { "--date", "25/08/2026", "bad-date", "--duration", "01:02:03,5", "bad-duration" }));

        Assert.That(values.DateTimes("--date"), Is.EqualTo(new System.DateTime[] { new System.DateTime(2026, 8, 25), new System.DateTime() }));
        Assert.That(values.TimeSpans("--duration"), Is.EqualTo(new System.TimeSpan[] { new System.TimeSpan(0, 1, 2, 3, 500), new System.TimeSpan() }));
        Assert.That(values.GetConversionErrors().Count, Is.EqualTo(2));
        AssertFailure(values.GetConversionErrors()[0], "DateTime", "bad-date");
        AssertFailure(values.GetConversionErrors()[1], "TimeSpan", "bad-duration");
    }

    [Test]
    public void PathAndEnumArrays_ConvertEveryValueAndRetainInvalidEnumPositions()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--file").Arity(1, 2);
        app.Option("--directory").Arity(1, 2);
        app.Option("--mode").Arity(1, 3);
        ArgValues values = new ArgValues(app.Parse(new string[] { "--file", "src/one.cs", "tests/two.cs", "--directory", "src", "tests", "--mode", "debug", "staging", "RELEASE" }));

        System.IO.FileInfo[] files = values.FileInfos("--file");
        System.IO.DirectoryInfo[] directories = values.DirectoryInfos("--directory");
        object?[] modes = values.EnumValues("--mode", typeof(ArgValuesArrayMode));

        Assert.That(files.Length, Is.EqualTo(2));
        Assert.That(files[0].Name, Is.EqualTo("one.cs"));
        Assert.That(files[1].Name, Is.EqualTo("two.cs"));
        Assert.That(directories.Length, Is.EqualTo(2));
        Assert.That(directories[0].Name, Is.EqualTo("src"));
        Assert.That(directories[1].Name, Is.EqualTo("tests"));
        Assert.That(modes, Is.EqualTo(new object?[] { ArgValuesArrayMode.Debug, null, ArgValuesArrayMode.Release }));
        Assert.That(values.GetConversionErrors().Count, Is.EqualTo(1));
        AssertFailure(values.GetConversionErrors()[0], "enum ArgValuesArrayMode", "staging");
    }

    [Test]
    public void Arrays_ReturnEmptyWithoutRecordingFailuresWhenSymbolIsAbsent()
    {
        ArgValues values = ArgValues.Empty();

        Assert.That(values.Bools("missing"), Is.Empty);
        Assert.That(values.Ints("missing"), Is.Empty);
        Assert.That(values.Longs("missing"), Is.Empty);
        Assert.That(values.Doubles("missing"), Is.Empty);
        Assert.That(values.Decimals("missing"), Is.Empty);
        Assert.That(values.DateTimes("missing"), Is.Empty);
        Assert.That(values.TimeSpans("missing"), Is.Empty);
        Assert.That(values.FileInfos("missing"), Is.Empty);
        Assert.That(values.DirectoryInfos("missing"), Is.Empty);
        Assert.That(values.EnumValues("missing", typeof(ArgValuesArrayMode)), Is.Empty);
        Assert.That(values.GetConversionErrors(), Is.Empty);
        Assert.That(values.GetLastConversionResult().ExpectedType, Is.EqualTo(""));
        Assert.Throws<System.ArgumentNullException>(delegate { values.EnumValues("missing", null!); });
        Assert.Throws<System.ArgumentException>(delegate { values.EnumValues("missing", typeof(string)); });
    }

    static string TrimLower(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    static void AssertFailure(ArgConvertResult result, string expectedType, string actualValue)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ExpectedType, Is.EqualTo(expectedType));
        Assert.That(result.ActualValue, Is.EqualTo(actualValue));
    }
}
