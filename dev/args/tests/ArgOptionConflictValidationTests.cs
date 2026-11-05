using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgOptionConflictValidationTests
{
    [Test]
    public void Validate_ConflictingOptionsAddValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--fast").Flag().Conflicts("--safe");
        command.Option("--safe").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--fast", "--safe" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertConflictError(result.Errors[0], "--fast", "--safe", 0);
    }

    [Test]
    public void Validate_AbsentConflictingOptionDoesNotAddValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--fast").Flag().Conflicts("--safe");
        command.Option("--safe").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--fast" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_ConflictNameCanUseAlias()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--fast").Flag().Conflicts("-s");
        command.Option("--safe").Alias("-s").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--fast", "-s" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertConflictError(result.Errors[0], "--fast", "--safe", 0);
    }

    [Test]
    public void Validate_InheritedGlobalOptionCanConflictWithLocalOption()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--workspace").Flag().Conflicts("--local");
        ArgCommand build = root.Command("build");
        build.Option("--local").Flag();
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build", "--workspace", "--local" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertConflictError(result.Errors[0], "--workspace", "--local", 1);
    }

    [Test]
    public void Validate_DefaultedOptionCanConflictWithSuppliedOption()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--mode").Value("name").Default("safe").Conflicts("--fast");
        command.Option("--fast").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--fast" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertConflictError(result.Errors[0], "--mode", "--fast", -1);
    }

    [Test]
    public void Validate_MutualConflictsReportSingleValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--fast").Flag().Conflicts("--safe");
        command.Option("--safe").Flag().Conflicts("--fast");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--fast", "--safe" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertConflictError(result.Errors[0], "--fast", "--safe", 0);
    }

    static void AssertConflictError(ArgParseError error, string symbolName, string conflictName, int tokenIndex)
    {
        Assert.That(error.Code, Is.EqualTo("conflicting_option"));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(error.Message, Is.EqualTo("Option '" + symbolName + "' cannot be used with option '" + conflictName + "'."));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
