using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlDocumentParserTests
{
    [Test]
    public void ParsesEmptyDocumentsAndRetainsRequestedTrivia()
    {
        CddlParseResult empty = Parse("");
        CddlParseResult trivia = Parse(
            " ; heading\n  ",
            new CddlParseOptions
            {
                SourceName = "empty.cddl",
                RetainTrivia = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsSuccess, Is.True);
            Assert.That(empty.Document, Is.Not.Null);
            Assert.That(empty.Document!.Rules, Is.Empty);
            Assert.That(empty.Document.RootRuleName, Is.Null);
            Assert.That(empty.Diagnostics, Is.Empty);
            Assert.That(trivia.IsSuccess, Is.True);
            Assert.That(trivia.Document!.Trivia,
                Is.EqualTo(new string[] { " ", "; heading\n", "  " }));
            Assert.That(trivia.Document.TriviaSpans[1].SourceName,
                Is.EqualTo("empty.cddl"));
            Assert.That(trivia.Document.TriviaSpans[1].Start,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ParsesOrderedTypeRulesChoicesExtensionsAndLiteralPayloads()
    {
        string source =
            "root = uint / -1 / 2.5\n"
            + "root /= tstr\n"
            + "bytes = h'00ff'\n"
            + "text = \"hi\\n\"";
        CddlDocument document = ParseDone(source);
        CddlRule root = document.Rules[0];
        CddlNode rootChoice = root.Value!;
        CddlNode negative = rootChoice.Children[1];
        CddlNode floating = rootChoice.Children[2];
        CddlNode bytes = document.Rules[2].Value!;
        CddlNode text = document.Rules[3].Value!;

        Assert.Multiple(() =>
        {
            Assert.That(document.RootRuleName, Is.EqualTo("root"));
            Assert.That(document.Rules.Select(rule => rule.Name),
                Is.EqualTo(new string[] { "root", "root", "bytes", "text" }));
            Assert.That(document.Rules.Select(rule => rule.SourceOrder),
                Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(root.Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(rootChoice.Kind, Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(rootChoice.Children, Has.Count.EqualTo(3));
            Assert.That(rootChoice.Children[0].Kind,
                Is.EqualTo(CddlNodeKind.RuleReference));
            Assert.That(rootChoice.Children[0].Name, Is.EqualTo("uint"));
            Assert.That(negative.IntegerValue, Is.EqualTo(new BigInteger(-1)));
            Assert.That(negative.HasIntegerValue, Is.True);
            Assert.That(floating.FloatValue, Is.EqualTo(2.5));
            Assert.That(floating.HasFloatValue, Is.True);
            Assert.That(document.Rules[1].Kind,
                Is.EqualTo(CddlRuleKind.TypeExtension));
            Assert.That(bytes.ByteStringValue, Is.EqualTo(new byte[] { 0, 255 }));
            Assert.That(text.TextValue, Is.EqualTo("hi\n"));
        });
    }

    [Test]
    public void ParsesOrderedGroupExtensionsAsGroupEntries()
    {
        CddlDocument document = ParseDone(
            "extension //= first\n"
            + "extension //= second\n"
            + "single //= member\n"
            + "root = final");
        CddlRule firstExtension = document.Rules[0];
        CddlRule secondExtension = document.Rules[1];
        CddlNode singleGroup = document.Rules[2].Value!;

        Assert.Multiple(() =>
        {
            Assert.That(firstExtension.Kind,
                Is.EqualTo(CddlRuleKind.GroupExtension));
            Assert.That(secondExtension.Kind,
                Is.EqualTo(CddlRuleKind.GroupExtension));
            AssertGroupEntry(firstExtension.Value!, "first");
            AssertGroupEntry(secondExtension.Value!, "second");
            AssertGroupEntry(singleGroup, "member");
            Assert.That(document.RootRuleName, Is.EqualTo("root"));
            Assert.That(document.Rules.Select(rule => rule.SourceOrder),
                Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        });
    }

    [Test]
    public void BuildsGroupChoicesFromOrderedGroupAlternatives()
    {
        CddlDocumentParser parser = new CddlDocumentParser(
            "first // second // 42",
            new CddlParseOptions());
        parser.ReadParserToken();
        CddlNode choice = parser.ParseGroupChoiceNode()!;

        Assert.Multiple(() =>
        {
            Assert.That(parser.HasError, Is.False);
            Assert.That(parser.Current.Kind, Is.EqualTo(CddlTokenKind.EndOfFile));
            Assert.That(choice.Kind, Is.EqualTo(CddlNodeKind.GroupChoice));
            Assert.That(choice.Children, Has.Count.EqualTo(3));
            AssertGroupAlternative(choice.Children[0], "first");
            AssertGroupAlternative(choice.Children[1], "second");
            Assert.That(
                choice.Children[2].Children[0].Children[0].IntegerValue,
                Is.EqualTo(new BigInteger(42)));
        });
    }

    [Test]
    public void SelectsFirstBaseRuleAfterExtensionsAndPreservesExactSpans()
    {
        string source = "root /= a\r\nactual = b\r\nroot = c";
        CddlDocument document = ParseDone(
            source,
            new CddlParseOptions { SourceName = "ordered.cddl" });

        Assert.Multiple(() =>
        {
            Assert.That(document.RootRuleName, Is.EqualTo("actual"));
            Assert.That(document.Rules[0].Kind,
                Is.EqualTo(CddlRuleKind.TypeExtension));
            Assert.That(document.Rules[1].Span.Start, Is.EqualTo(11));
            Assert.That(document.Rules[1].Span.Length, Is.EqualTo(10));
            Assert.That(document.Rules[1].Span.StartLine, Is.EqualTo(2));
            Assert.That(document.Rules[1].Span.StartColumn, Is.EqualTo(1));
            Assert.That(document.Rules[1].Span.EndLine, Is.EqualTo(2));
            Assert.That(document.Rules[1].Span.EndColumn, Is.EqualTo(11));
            Assert.That(document.Rules[1].Span.SourceName,
                Is.EqualTo("ordered.cddl"));
            Assert.That(document.Rules[2].SourceOrder, Is.EqualTo(2));
        });
    }

    [TestCase("root", "CDDL_PARSE_ASSIGNMENT_EXPECTED", 4)]
    [TestCase("root =", "CDDL_PARSE_TYPE_EXPECTED", 6)]
    [TestCase("root = a /", "CDDL_PARSE_TYPE_EXPECTED", 10)]
    [TestCase("root //=", "CDDL_PARSE_TYPE_EXPECTED", 8)]
    [TestCase("root = a // b", "CDDL_PARSE_RULE_END_EXPECTED", 9)]
    [TestCase("root = a , b", "CDDL_PARSE_RULE_END_EXPECTED", 9)]
    public void ReportsMalformedRulesAndChoiceTailsAtExactOffsets(
        string source,
        string code,
        int offset)
    {
        CddlParseResult result = Parse(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Span.Start, Is.EqualTo(offset));
            Assert.That(result.Document, Is.Not.Null);
        });
    }

    [Test]
    public void ThrowingParseUsesTheFirstStructuredDiagnostic()
    {
        CddlParser parser = new CddlParser();
        CddlParseException exception = Assert.Throws<CddlParseException>(() =>
            parser.ParseDocument("root = a /", new CddlParseOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("CDDL_PARSE_TYPE_EXPECTED"));
            Assert.That(exception.Span, Is.Not.Null);
            Assert.That(exception.Span!.Start, Is.EqualTo(10));
        });
    }

    private static CddlParseResult Parse(
        string source,
        CddlParseOptions? options = null)
    {
        return new CddlParser().TryParseDocument(
            source,
            options ?? new CddlParseOptions());
    }

    private static CddlDocument ParseDone(
        string source,
        CddlParseOptions? options = null)
    {
        CddlParseResult result = Parse(source, options);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Document, Is.Not.Null);
        return result.Document!;
    }

    private static void AssertGroupAlternative(CddlNode group, string name)
    {
        Assert.Multiple(() =>
        {
            Assert.That(group.Kind, Is.EqualTo(CddlNodeKind.Group));
            Assert.That(group.Children, Has.Count.EqualTo(1));
            Assert.That(group.Children[0].Kind, Is.EqualTo(CddlNodeKind.GroupEntry));
            Assert.That(group.Children[0].Children, Has.Count.EqualTo(1));
            Assert.That(group.Children[0].Children[0].Name, Is.EqualTo(name));
        });
    }

    private static void AssertGroupEntry(CddlNode entry, string name)
    {
        Assert.Multiple(() =>
        {
            Assert.That(entry.Kind, Is.EqualTo(CddlNodeKind.GroupEntry));
            Assert.That(entry.Children, Has.Count.EqualTo(1));
            Assert.That(entry.Children[0].Name, Is.EqualTo(name));
        });
    }
}
