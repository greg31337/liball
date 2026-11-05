using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgValuesStringRetrievalTests
{
    [Test]
    public void String_ReturnsFirstSuppliedOptionValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Alias("-c").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "-c", "Release" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.String("--configuration"), Is.EqualTo("Release"));
        Assert.That(values.String("-c"), Is.EqualTo("Release"));
    }

    [Test]
    public void String_ReturnsDefaultedArgumentValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Default(".");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.String("project"), Is.EqualTo("."));
    }

    [Test]
    public void String_ReturnsEmptyForMissingOrValuelessSymbols()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--verbose" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.String("--verbose"), Is.EqualTo(""));
        Assert.That(values.String("--missing"), Is.EqualTo(""));
    }

    [Test]
    public void Strings_FlattensRepeatableOptionValuesInParseOrder()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--include").Alias("-I").Value("path").Repeatable();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--include", "src", "-I", "tests" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.Strings("--include"), Is.EqualTo(new string[] { "src", "tests" }));
        Assert.That(values.Strings("-I"), Is.EqualTo(new string[] { "src", "tests" }));
    }

    [Test]
    public void Strings_ReturnsVariadicArgumentValues()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("files").Variadic();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "one.txt", "two.txt" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.String("files"), Is.EqualTo("one.txt"));
        Assert.That(values.Strings("files"), Is.EqualTo(new string[] { "one.txt", "two.txt" }));
    }

    [Test]
    public void Strings_ReturnsEmptyArrayWhenSymbolIsMissing()
    {
        ArgValues values = ArgValues.Empty();

        Assert.That(values.Strings("--missing"), Is.Empty);
    }
}
