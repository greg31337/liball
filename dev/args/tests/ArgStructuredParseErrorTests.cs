using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgStructuredParseErrorTests
{
    [Test]
    public void Parse_UnknownOptionErrorIncludesCodeKindTokenIndexAndSymbolName()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--missing" });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertParseError(result.Errors[0], "unknown_option", ArgSymbolKind.Option, "Unknown option '--missing'.", 0, "--missing");
    }

    [Test]
    public void Parse_MissingValueErrorIncludesCodeKindTokenIndexAndSymbolName()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--configuration" });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertParseError(result.Errors[0], "missing_value", ArgSymbolKind.Option, "Option '--configuration' requires a value.", 0, "--configuration");
    }

    [Test]
    public void Parse_UnexpectedValueErrorIncludesCodeKindTokenIndexAndSymbolName()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--verbose=true" });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertParseError(result.Errors[0], "unexpected_value", ArgSymbolKind.Option, "Option '--verbose' does not accept a value.", 0, "--verbose");
    }

    [Test]
    public void DefaultParseErrorStartsUnclassified()
    {
        ArgParseError error = new ArgParseError("", ArgSymbolKind.None, "", -1, "");

        Assert.That(error.Code, Is.EqualTo(""));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.None));
        Assert.That(error.TokenIndex, Is.EqualTo(-1));
        Assert.That(error.SymbolName, Is.EqualTo(""));
    }

    static void AssertParseError(ArgParseError error, string code, ArgSymbolKind kind, string message, int tokenIndex, string symbolName)
    {
        Assert.That(error.Code, Is.EqualTo(code));
        Assert.That(error.SymbolKind, Is.EqualTo(kind));
        Assert.That(error.Message, Is.EqualTo(message));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
