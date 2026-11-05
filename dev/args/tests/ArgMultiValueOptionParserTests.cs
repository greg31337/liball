using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgMultiValueOptionParserTests
{
    [Test]
    public void Parse_FixedArityCollectsConsecutiveLongOptionValues()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--point").Arity(3, 3);

        ArgParseResult result = app.Parse(new string[] { "--point", "10", "20", "30" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "10", "20", "30" }));
        Assert.That(result.Tokens[1].Consumed, Is.True);
        Assert.That(result.Tokens[2].Consumed, Is.True);
        Assert.That(result.Tokens[3].Consumed, Is.True);
    }

    [Test]
    public void Parse_BoundedArityStopsAtMaximumAndLeavesPositionals()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--include").Arity(1, 2);
        app.Argument("target");

        ArgParseResult result = app.Parse(new string[] { "--include", "src", "tests", "project" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "src", "tests" }));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("target"));
        Assert.That(result.Occurrences[1].Values, Is.EqualTo(new string[] { "project" }));
    }

    [Test]
    public void Parse_UnboundedArityStopsBeforeFollowingOption()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--include").Arity(1, ArgArity.Unbounded);
        app.Option("--verbose").Flag();

        ArgParseResult result = app.Parse(new string[] { "--include", "src", "tests", "--verbose" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "src", "tests" }));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.Tokens[3].Consumed, Is.True);
    }

    [Test]
    public void Parse_UnboundedArityStopsBeforeEndOfOptionsDelimiter()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--include").Arity(1, ArgArity.Unbounded);
        app.Argument("files").Variadic();

        ArgParseResult result = app.Parse(new string[] { "--include", "src", "--", "--literal", "file.txt" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "src" }));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("files"));
        Assert.That(result.Occurrences[1].Values, Is.EqualTo(new string[] { "--literal", "file.txt" }));
        Assert.That(result.Tokens[2].Kind, Is.EqualTo(ArgTokenKind.EndOfOptions));
        Assert.That(result.Tokens[2].Consumed, Is.True);
    }

    [Test]
    public void Parse_MultiValueOptionReservesRequiredPositionalValues()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--include").Arity(1, ArgArity.Unbounded);
        app.Argument("source").Required();
        app.Argument("destination").Required();

        ArgParseResult result = app.Parse(new string[] { "--include", "one", "two", "src", "dist" });
        ArgValidationResult validation = app.Validate(new string[] { "--include", "one", "two", "src", "dist" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(result.Occurrences[1].Values, Is.EqualTo(new string[] { "src" }));
        Assert.That(result.Occurrences[2].Values, Is.EqualTo(new string[] { "dist" }));
        Assert.That(validation.Errors, Is.Empty);
    }

    [Test]
    public void Parse_MultiValueOptionDoesNotConsumeSubcommandToken()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--values").Arity(0, ArgArity.Unbounded);
        app.Command("build");

        ArgParseResult result = app.Parse(new string[] { "--values", "one", "build" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "one" }));
        Assert.That(result.Tokens[2].Consumed, Is.False);
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[2] }));
    }

    [Test]
    public void Parse_InlineValueCountsTowardBoundedArity()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--pair").Arity(2, 2);

        ArgParseResult result = app.Parse(new string[] { "--pair=left", "right", "extra" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "left", "right" }));
        Assert.That(result.Tokens[1].Consumed, Is.True);
        Assert.That(result.Tokens[2].Consumed, Is.False);
    }

    [Test]
    public void Parse_AttachedShortClusterValueContinuesToFixedArity()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--verbose").Alias("-v").Flag();
        app.Option("--number").Alias("-n").Arity(2, 2);

        ArgParseResult result = app.Parse(new string[] { "-vn5", "6" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("--number"));
        Assert.That(result.Occurrences[1].Values, Is.EqualTo(new string[] { "5", "6" }));
    }

    [Test]
    public void Parse_SlashInlineValueContinuesToFixedArity()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("/pair").Arity(2, 2);

        ArgParseResult result = app.Parse(new string[] { "/pair:left", "right" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "left", "right" }));
    }

    [Test]
    public void Validate_RequiredPositionalReservationCanLeaveOptionBelowMinimum()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--pair").Arity(2, 2);
        app.Argument("target").Required();

        ArgValidationResult result = app.Validate(new string[] { "--pair", "left", "project" });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("too_few_values"));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("--pair"));
        Assert.That(result.ParseResult.Occurrences[1].SymbolName, Is.EqualTo("target"));
        Assert.That(result.ParseResult.Occurrences[1].Values, Is.EqualTo(new string[] { "project" }));
    }
}
