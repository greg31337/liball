using NUnit.Framework;

namespace args.Tests;

public enum ArgValuesConversionMode
{
    Debug,
    Release
}

[TestFixture]
public class ArgValuesConversionTests
{
    [Test]
    public void Bool_ConvertsBooleanTextAndFlags()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--enabled").Value("value");
        command.Option("--verbose").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--enabled", "yes", "--verbose" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.Bool("--enabled"), Is.True);
        Assert.That(values.Bool("--verbose"), Is.True);
        Assert.That(values.Bool("--missing"), Is.False);
    }

    [Test]
    public void NumericMethods_ConvertInvariantNumbers()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--int").Value("value");
        command.Option("--long").Value("value");
        command.Option("--double").Value("value");
        command.Option("--decimal").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--int", "42", "--long", "9000000000", "--double", "3.5", "--decimal", "12.75" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.Int("--int"), Is.EqualTo(42));
        Assert.That(values.Long("--long"), Is.EqualTo(9000000000L));
        Assert.That(values.Double("--double"), Is.EqualTo(3.5d));
        Assert.That(values.Decimal("--decimal"), Is.EqualTo(12.75m));
    }

    [Test]
    public void DateTimeAndTimeSpan_ConvertInvariantValues()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--date").Value("value");
        command.Option("--duration").Value("value");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--date", "2026-08-02", "--duration", "01:30:00" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.DateTime("--date"), Is.EqualTo(new System.DateTime(2026, 8, 2)));
        Assert.That(values.TimeSpan("--duration"), Is.EqualTo(new System.TimeSpan(1, 30, 0)));
    }

    [Test]
    public void FileInfoAndDirectoryInfo_ConvertPaths()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--file").Value("path");
        command.Option("--directory").Value("path");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--file", "src/ArgValues.cs", "--directory", "tests" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.FileInfo("--file").Name, Is.EqualTo("ArgValues.cs"));
        Assert.That(values.DirectoryInfo("--directory").Name, Is.EqualTo("tests"));
    }

    [Test]
    public void EnumValue_ConvertsEnumIgnoringCase()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--mode").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--mode", "release" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.EnumValue("--mode", typeof(ArgValuesConversionMode)), Is.EqualTo(ArgValuesConversionMode.Release));
    }

    [Test]
    public void Converter_ReturnsDefaultValuesForMissingOrInvalidInput()
    {
        ArgValues values = ArgValues.Empty();

        Assert.That(values.Int("--missing"), Is.EqualTo(0));
        Assert.That(values.Long("--missing"), Is.EqualTo(0L));
        Assert.That(values.Double("--missing"), Is.EqualTo(0d));
        Assert.That(values.Decimal("--missing"), Is.EqualTo(0m));
        Assert.That(values.DateTime("--missing"), Is.EqualTo(new System.DateTime()));
        Assert.That(values.TimeSpan("--missing"), Is.EqualTo(new System.TimeSpan()));
    }
}
