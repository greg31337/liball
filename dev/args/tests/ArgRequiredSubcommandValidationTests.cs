using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgRequiredSubcommandValidationTests
{
    [Test]
    public void Validate_MissingRequiredRootSubcommandAddsValidationError()
    {
        ArgCommand root = new ArgCommand("tool");
        root.RequiredSubcommand();
        root.Command("build");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertValidationError(result.Errors[0], "tool");
    }

    [Test]
    public void Validate_ResolvedSubcommandSatisfiesParentRequirement()
    {
        ArgCommand root = new ArgCommand("tool");
        root.RequiredSubcommand();
        root.Command("build");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_MissingRequiredNestedSubcommandAddsValidationError()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        project.RequiredSubcommand();
        project.Command("build");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "project" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertValidationError(result.Errors[0], "project");
    }

    [Test]
    public void Validate_ResolvedNestedSubcommandSatisfiesImmediateParentRequirement()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        project.RequiredSubcommand();
        project.Command("build");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "project", "build" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_CommandWithoutRequiredSubcommandDoesNotAddValidationError()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    static void AssertValidationError(ArgParseError error, string symbolName)
    {
        Assert.That(error.Code, Is.EqualTo("missing_required_subcommand"));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.Command));
        Assert.That(error.Message, Is.EqualTo("Missing required subcommand for command '" + symbolName + "'."));
        Assert.That(error.TokenIndex, Is.EqualTo(-1));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
