using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgValueConverterBuiltInTests
{
    [Test]
    public void ConvertBool_AcceptsCommonTrueAndFalseValues()
    {
        ArgValueConverter converter = new ArgValueConverter();

        AssertSuccess(converter.ConvertBool("true"), true, "bool", "true");
        AssertSuccess(converter.ConvertBool("FALSE"), false, "bool", "FALSE");
        AssertSuccess(converter.ConvertBool("1"), true, "bool", "1");
        AssertSuccess(converter.ConvertBool("0"), false, "bool", "0");
        AssertSuccess(converter.ConvertBool("yes"), true, "bool", "yes");
        AssertSuccess(converter.ConvertBool("No"), false, "bool", "No");
        AssertSuccess(converter.ConvertBool("on"), true, "bool", "on");
        AssertSuccess(converter.ConvertBool("OFF"), false, "bool", "OFF");
    }

    [Test]
    public void ConvertNumericTypes_UseInvariantCultureAndReportFailures()
    {
        ArgValueConverter converter = new ArgValueConverter();

        AssertSuccess(converter.ConvertInt("-42"), -42, "int", "-42");
        AssertSuccess(converter.ConvertLong("9000000000"), 9000000000L, "long", "9000000000");
        AssertSuccess(converter.ConvertDouble("1.25e2"), 125d, "double", "1.25e2");
        AssertSuccess(converter.ConvertDecimal("12.75"), 12.75m, "decimal", "12.75");
        AssertFailure(converter.ConvertInt("1.5"), "int", "1.5");
        AssertFailure(converter.ConvertLong("9.1"), "long", "9.1");
        AssertFailure(converter.ConvertDouble("many"), "double", "many");
        AssertFailure(converter.ConvertDecimal("many"), "decimal", "many");
    }

    [Test]
    public void ConvertTemporalTypes_ConvertInvariantValuesAndReportFailures()
    {
        ArgValueConverter converter = new ArgValueConverter();

        AssertSuccess(converter.ConvertDateTime("2026-08-02"), new System.DateTime(2026, 8, 2), "DateTime", "2026-08-02");
        AssertSuccess(converter.ConvertTimeSpan("01:02:03"), new System.TimeSpan(1, 2, 3), "TimeSpan", "01:02:03");
        AssertFailure(converter.ConvertDateTime("not-a-date"), "DateTime", "not-a-date");
        AssertFailure(converter.ConvertTimeSpan("not-a-duration"), "TimeSpan", "not-a-duration");
    }

    [Test]
    public void ConvertFileInfoAndDirectoryInfo_CreatePathObjects()
    {
        ArgValueConverter converter = new ArgValueConverter();

        ArgConvertResult file = converter.ConvertFileInfo("src/ArgValues.cs");
        ArgConvertResult directory = converter.ConvertDirectoryInfo("tests");
        ArgConvertResult emptyFile = converter.ConvertFileInfo("");
        ArgConvertResult emptyDirectory = converter.ConvertDirectoryInfo("");

        Assert.That(file.Success, Is.True);
        Assert.That(((System.IO.FileInfo)file.Value!).Name, Is.EqualTo("ArgValues.cs"));
        Assert.That(directory.Success, Is.True);
        Assert.That(((System.IO.DirectoryInfo)directory.Value!).Name, Is.EqualTo("tests"));
        Assert.That(emptyFile.Success, Is.True);
        Assert.That(((System.IO.FileInfo)emptyFile.Value!).FullName, Is.EqualTo(new System.IO.FileInfo(".").FullName));
        Assert.That(emptyDirectory.Success, Is.True);
        Assert.That(((System.IO.DirectoryInfo)emptyDirectory.Value!).FullName, Is.EqualTo(new System.IO.DirectoryInfo(".").FullName));
    }

    [Test]
    public void ConvertEnumValue_ConvertsIgnoringCaseAndReportsFailures()
    {
        ArgValueConverter converter = new ArgValueConverter();

        AssertSuccess(converter.ConvertEnumValue("release", typeof(ArgValuesConversionMode)), ArgValuesConversionMode.Release, "enum ArgValuesConversionMode", "release");
        AssertFailure(converter.ConvertEnumValue("staging", typeof(ArgValuesConversionMode)), "enum ArgValuesConversionMode", "staging");
        Assert.Throws<System.ArgumentNullException>(delegate { converter.ConvertEnumValue("debug", null!); });
    }

    static void AssertSuccess(ArgConvertResult result, object expectedValue, string expectedType, string actualValue)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(expectedValue));
        Assert.That(result.Error, Is.EqualTo(""));
        Assert.That(result.ExpectedType, Is.EqualTo(expectedType));
        Assert.That(result.ActualValue, Is.EqualTo(actualValue));
    }

    static void AssertFailure(ArgConvertResult result, string expectedType, string actualValue)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.ExpectedType, Is.EqualTo(expectedType));
        Assert.That(result.ActualValue, Is.EqualTo(actualValue));
        Assert.That(result.Error, Is.EqualTo("Expected " + expectedType + " value but got '" + actualValue + "'."));
    }
}
