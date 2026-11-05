using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgUnknownValidationTests
{
    [Test]
    public void Validate_CopiesParserUnknownOptionError()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--missing" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertUnknownOption(result.Errors[0], "--missing", 0);
    }

    [Test]
    public void Validate_CollectUnmatchedUnknownOptionAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParserSettings settings = new ArgParserSettings();
        settings.CollectUnmatched = true;
        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { "--missing=value" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertUnknownOption(result.Errors[0], "--missing", 0);
    }

    [Test]
    public void Validate_CollectUnmatchedShortTokenWithKnownPrefixAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Alias("-c").Value("name");
        ArgParserSettings settings = new ArgParserSettings();
        settings.CollectUnmatched = true;
        settings.EnablePosixShortClusters = false;
        settings.EnableShortOptionAttachedValues = false;
        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { "-cRelease" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertUnknownOption(result.Errors[0], "-cRelease", 0);
    }

    [Test]
    public void Validate_CollectUnmatchedSlashOptionWithColonValueAddsValidationErrorForAlias()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        settings.CollectUnmatched = true;
        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { "/missing:value" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertUnknownOption(result.Errors[0], "/missing", 0);
    }

    [Test]
    public void Validate_StopAtFirstUnmatchedDoesNotReportKnownTrailingOptionAsUnknown()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        ArgParserSettings settings = new ArgParserSettings();
        settings.StopAtFirstUnmatched = true;
        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { "--missing", "--verbose", "file.txt" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertUnknownOption(result.Errors[0], "--missing", 0);
    }

    [Test]
    public void Validate_UnknownRootCommandAddsValidationError()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "deploy" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertUnknownCommand(result.Errors[0], "deploy", 0);
    }

    [Test]
    public void Validate_UnknownNestedCommandAddsValidationError()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        project.Command("build");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "project", "deploy" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertUnknownCommand(result.Errors[0], "deploy", 1);
    }

    [Test]
    public void Validate_PositionalArgumentIsNotReportedAsUnknownCommand()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build");
        root.Argument("project");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "deploy" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    static void AssertUnknownOption(ArgParseError error, string symbolName, int tokenIndex)
    {
        Assert.That(error.Code, Is.EqualTo("unknown_option"));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(error.Message, Is.EqualTo("Unknown option '" + symbolName + "'."));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }

    static void AssertUnknownCommand(ArgParseError error, string symbolName, int tokenIndex)
    {
        Assert.That(error.Code, Is.EqualTo("unknown_command"));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.Command));
        Assert.That(error.Message, Is.EqualTo("Unknown command '" + symbolName + "'."));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
