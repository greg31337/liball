using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlParserRecoveryTests
{
    [Test]
    public void RecoversAtCrediblePlainAndGenericRuleStarts()
    {
        string source =
            "good = uint\n"
            + "broken = [a,, nested<uint>]\n"
            + "generic<t> = {value: t}\n"
            + "missing =\n"
            + "tail = [int]\n"
            + "bad = ~1\n"
            + "last = tstr";
        CddlParseResult result = Parse(source, new CddlParseOptions());
        CddlDocument document = result.Document!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code), Is.EqualTo(
                new string[]
                {
                    "CDDL_PARSE_CONTAINER_CLOSE_EXPECTED",
                    "CDDL_PARSE_TYPE_EXPECTED",
                    "CDDL_PARSE_UNWRAP_NAME_EXPECTED"
                }));
            Assert.That(result.Diagnostics.Select(item => item.Span.Start), Is.EqualTo(
                new int[]
                {
                    source.IndexOf(",,", StringComparison.Ordinal) + 1,
                    source.IndexOf("tail =", StringComparison.Ordinal),
                    source.IndexOf("~1", StringComparison.Ordinal) + 1
                }));
            Assert.That(document.Rules.Select(rule => rule.Name), Is.EqualTo(
                new string[]
                {
                    "good", "broken", "generic", "missing", "tail", "bad", "last"
                }));
            Assert.That(document.Rules[1].Value, Is.Null);
            Assert.That(document.Rules[2].GenericParameters,
                Is.EqualTo(new string[] { "t" }));
            Assert.That(document.Rules[3].Value, Is.Null);
            Assert.That(document.Rules[4].Value!.Kind, Is.EqualTo(CddlNodeKind.Array));
            Assert.That(document.Rules[5].Value, Is.Null);
            Assert.That(document.Rules[6].Value!.Name, Is.EqualTo("tstr"));
            Assert.That(document.RootRuleName, Is.EqualTo("good"));
            Assert.That(document.Diagnostics, Is.EqualTo(result.Diagnostics));
        });
        AssertNonOverlapping(result.Diagnostics);
    }

    [Test]
    public void RecoversAcrossIndependentLexicalFailures()
    {
        string source =
            "one = uint\n"
            + "!\n"
            + "two = tstr\n"
            + "\t\n"
            + "three = [int]";
        CddlParseResult result = Parse(source, new CddlParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code), Is.EqualTo(
                new string[]
                {
                    "CDDL_LEX_CHARACTER_INVALID",
                    "CDDL_LEX_WHITESPACE_INVALID"
                }));
            Assert.That(result.Document!.Rules.Select(rule => rule.Name),
                Is.EqualTo(new string[] { "one", "two", "three" }));
            Assert.That(result.Document.Rules[2].Value!.Kind,
                Is.EqualTo(CddlNodeKind.Array));
        });
        AssertNonOverlapping(result.Diagnostics);
    }

    [Test]
    public void RecoveryCanBeDisabledAfterTheFirstFailedRule()
    {
        CddlParseResult result = Parse(
            "good = uint\nbroken = [a,,b]\ntail = tstr",
            new CddlParseOptions { Recover = false });

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Document!.Rules.Select(rule => rule.Name),
                Is.EqualTo(new string[] { "good", "broken" }));
            Assert.That(result.Document.Rules[1].Value, Is.Null);
        });
    }

    [Test]
    public void DiagnosticLimitBoundsRecoveryAndKeepsStableOrdering()
    {
        string source = "a = [x,,y]\nb = ~1\nc = #8\nd = uint";
        CddlParseResult limited = Parse(
            source,
            new CddlParseOptions { MaxDiagnostics = 2 });
        CddlParseResult none = Parse(
            source,
            new CddlParseOptions { MaxDiagnostics = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(limited.IsSuccess, Is.False);
            Assert.That(limited.Diagnostics.Select(item => item.Code), Is.EqualTo(
                new string[]
                {
                    "CDDL_PARSE_CONTAINER_CLOSE_EXPECTED",
                    "CDDL_PARSE_UNWRAP_NAME_EXPECTED"
                }));
            Assert.That(limited.Document!.Rules.Select(rule => rule.Name),
                Is.EqualTo(new string[] { "a", "b" }));
            Assert.That(none.IsSuccess, Is.False);
            Assert.That(none.Diagnostics, Is.Empty);
            Assert.That(none.Document!.Rules.Select(rule => rule.Name),
                Is.EqualTo(new string[] { "a" }));
        });
        AssertNonOverlapping(limited.Diagnostics);
    }

    [Test]
    public void NodeLimitStopsConstructionWithoutParsingLaterRules()
    {
        CddlParseResult exact = Parse(
            "root = uint",
            new CddlParseOptions { MaxNodes = 1 });
        string source = "root = [a,b,c]\ntail = tstr";
        CddlParseResult exceeded = Parse(
            source,
            new CddlParseOptions { MaxNodes = 5 });
        CddlParseResult zero = Parse(
            "root = uint",
            new CddlParseOptions { MaxNodes = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(exact.IsSuccess, Is.True);
            Assert.That(exact.Document!.Rules[0].Value!.Name, Is.EqualTo("uint"));
            Assert.That(exceeded.IsSuccess, Is.False);
            Assert.That(exceeded.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(exceeded.Diagnostics[0].Code,
                Is.EqualTo("CDDL_PARSE_NODE_LIMIT"));
            Assert.That(exceeded.Diagnostics[0].Span.Start,
                Is.EqualTo(source.IndexOf("c]", StringComparison.Ordinal)));
            Assert.That(exceeded.Document!.Rules.Select(rule => rule.Name),
                Is.EqualTo(new string[] { "root" }));
            Assert.That(exceeded.Document.Rules[0].Value, Is.Null);
            Assert.That(zero.IsSuccess, Is.False);
            Assert.That(zero.Diagnostics[0].Code, Is.EqualTo("CDDL_PARSE_NODE_LIMIT"));
            Assert.That(zero.Document!.Rules[0].Value, Is.Null);
        });
    }

    [Test]
    public void ThrowingParseStillUsesFirstDiagnosticAfterRecovery()
    {
        CddlParseException exception = Assert.Throws<CddlParseException>(() =>
            new CddlParser().ParseDocument(
                "first = [a,,b]\nsecond = ~1",
                new CddlParseOptions()))!;

        Assert.That(exception.ErrorCode,
            Is.EqualTo("CDDL_PARSE_CONTAINER_CLOSE_EXPECTED"));
    }

    private static CddlParseResult Parse(
        string source,
        CddlParseOptions options)
    {
        return new CddlParser().TryParseDocument(source, options);
    }

    private static void AssertNonOverlapping(CddlDiagnostic[] diagnostics)
    {
        int index = 1;
        while (index < diagnostics.Length)
        {
            int previousEnd = diagnostics[index - 1].Span.Start
                + diagnostics[index - 1].Span.Length;
            Assert.That(diagnostics[index].Span.Start,
                Is.GreaterThanOrEqualTo(previousEnd));
            index = index + 1;
        }
    }
}
