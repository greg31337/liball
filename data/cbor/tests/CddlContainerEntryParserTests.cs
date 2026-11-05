using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlContainerEntryParserTests
{
    [Test]
    public void ParsesNestedArraysMapsKeysAndEmptyContainers()
    {
        string source =
            "root = [uint, {name: tstr, \"age\": uint, 0 => bool}, []]";
        CddlNode array = ParseRoot(source);
        CddlNode arrayGroup = array.Children[0];
        CddlNode map = EntryValue(arrayGroup.Children[1]);
        CddlNode mapGroup = map.Children[0];
        CddlNode bareword = mapGroup.Children[0];
        CddlNode textKey = mapGroup.Children[1];
        CddlNode arrowKey = mapGroup.Children[2];
        CddlNode emptyArray = EntryValue(arrayGroup.Children[2]);

        Assert.Multiple(() =>
        {
            Assert.That(array.Kind, Is.EqualTo(CddlNodeKind.Array));
            Assert.That(array.Span.Start, Is.EqualTo(7));
            Assert.That(array.Span.Length, Is.EqualTo(source.Length - 7));
            Assert.That(arrayGroup.Kind, Is.EqualTo(CddlNodeKind.Group));
            Assert.That(arrayGroup.Children, Has.Count.EqualTo(3));
            Assert.That(map.Kind, Is.EqualTo(CddlNodeKind.Map));
            Assert.That(mapGroup.Children, Has.Count.EqualTo(3));
            Assert.That(bareword.Name, Is.EqualTo("name"));
            Assert.That(bareword.Children, Has.Count.EqualTo(1));
            Assert.That(EntryValue(bareword).Name, Is.EqualTo("tstr"));
            Assert.That(textKey.Name, Is.Null);
            Assert.That(textKey.Children, Has.Count.EqualTo(2));
            Assert.That(textKey.Children[0].TextValue, Is.EqualTo("age"));
            Assert.That(EntryValue(textKey).Name, Is.EqualTo("uint"));
            Assert.That(arrowKey.Children[0].IntegerValue,
                Is.EqualTo(BigInteger.Zero));
            Assert.That(EntryValue(arrowKey).Name, Is.EqualTo("bool"));
            Assert.That(emptyArray.Kind, Is.EqualTo(CddlNodeKind.Array));
            Assert.That(emptyArray.Children[0].Kind, Is.EqualTo(CddlNodeKind.Group));
            Assert.That(emptyArray.Children[0].Children, Is.Empty);
        });
    }

    [Test]
    public void ParsesGroupChoicesWithMultipleAndEmptyAlternatives()
    {
        CddlNode array = ParseRoot("root = [a, b // c //]");
        CddlNode choice = array.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(choice.Kind, Is.EqualTo(CddlNodeKind.GroupChoice));
            Assert.That(choice.Children, Has.Count.EqualTo(3));
            Assert.That(choice.Children[0].Kind, Is.EqualTo(CddlNodeKind.Group));
            Assert.That(choice.Children[0].Children, Has.Count.EqualTo(2));
            Assert.That(EntryValue(choice.Children[0].Children[0]).Name,
                Is.EqualTo("a"));
            Assert.That(EntryValue(choice.Children[0].Children[1]).Name,
                Is.EqualTo("b"));
            Assert.That(choice.Children[1].Children, Has.Count.EqualTo(1));
            Assert.That(EntryValue(choice.Children[1].Children[0]).Name,
                Is.EqualTo("c"));
            Assert.That(choice.Children[2].Children, Is.Empty);
        });
    }

    [Test]
    public void DistinguishesParenthesizedTypesAndGroupsByGrammarPreemption()
    {
        CddlDocument document = ParseDone(
            "type = (uint / tstr)\n"
            + "group = (first, ? second,)\n"
            + "empty = ()\n"
            + "nested = [(uint), (? item)]");
        CddlNode type = document.Rules[0].Value!;
        CddlNode groupEntry = document.Rules[1].Value!;
        CddlNode group = EntryValue(groupEntry);
        CddlNode emptyGroup = EntryValue(document.Rules[2].Value!);
        CddlNode nested = document.Rules[3].Value!;
        CddlNode nestedGroup = nested.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(type.Kind, Is.EqualTo(CddlNodeKind.ParenthesizedType));
            Assert.That(type.Children[0].Kind, Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(document.Rules[0].Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(document.Rules[1].Kind, Is.EqualTo(CddlRuleKind.Group));
            Assert.That(group.Kind, Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
            Assert.That(group.Children[0].Kind, Is.EqualTo(CddlNodeKind.Group));
            Assert.That(group.Children[0].Children, Has.Count.EqualTo(2));
            Assert.That(group.Children[0].Children[1].Occurrence,
                Is.EqualTo(CddlOccurrenceKind.Optional));
            Assert.That(emptyGroup.Kind, Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
            Assert.That(emptyGroup.Children[0].Children, Is.Empty);
            Assert.That(EntryValue(nestedGroup.Children[0]).Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedType));
            Assert.That(EntryValue(nestedGroup.Children[1]).Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
        });
    }

    [Test]
    public void ParsesBarewordExplicitArrowAndCutMapKeys()
    {
        CddlNode map = ParseRoot(
            "root = {label: uint, \"name\": tstr, 1: bool, "
            + "uint => any, tstr ^ => any}");
        CddlNode group = map.Children[0];
        CddlNode bareword = group.Children[0];
        CddlNode text = group.Children[1];
        CddlNode integer = group.Children[2];
        CddlNode arrow = group.Children[3];
        CddlNode cut = group.Children[4];

        Assert.Multiple(() =>
        {
            Assert.That(bareword.Name, Is.EqualTo("label"));
            Assert.That(bareword.Children, Has.Count.EqualTo(1));
            Assert.That(text.Children[0].LiteralKind,
                Is.EqualTo(CddlLiteralKind.TextString));
            Assert.That(integer.Children[0].IntegerValue,
                Is.EqualTo(BigInteger.One));
            Assert.That(arrow.Children[0].Name, Is.EqualTo("uint"));
            Assert.That(EntryValue(arrow).Name, Is.EqualTo("any"));
            Assert.That(arrow.IsCut, Is.False);
            Assert.That(cut.Children[0].Name, Is.EqualTo("tstr"));
            Assert.That(cut.IsCut, Is.True);
        });
    }

    [Test]
    public void ParsesEveryOccurrenceFormAndBoundRadix()
    {
        CddlNode array = ParseRoot(
            "root = [value, ? optional, * many, + some, 2* lower, "
            + "*3 upper, 2*3 bounded, 0x2*0b11 based]");
        CddlNode group = array.Children[0];

        Assert.Multiple(() =>
        {
            AssertOccurrence(group.Children[0], CddlOccurrenceKind.ExactlyOne, 1, 1);
            AssertOccurrence(group.Children[1], CddlOccurrenceKind.Optional, 0, 1);
            AssertOccurrence(group.Children[2], CddlOccurrenceKind.ZeroOrMore, 0, -1);
            AssertOccurrence(group.Children[3], CddlOccurrenceKind.OneOrMore, 1, -1);
            AssertOccurrence(group.Children[4], CddlOccurrenceKind.Bounded, 2, -1);
            AssertOccurrence(group.Children[5], CddlOccurrenceKind.Bounded, 0, 3);
            AssertOccurrence(group.Children[6], CddlOccurrenceKind.Bounded, 2, 3);
            AssertOccurrence(group.Children[7], CddlOccurrenceKind.Bounded, 2, 3);
        });
    }

    [Test]
    public void ParsesSyntacticallyDistinctBaseAndExtensionGroupEntries()
    {
        CddlDocument document = ParseDone(
            "fields = name: tstr\n"
            + "fields //= ? age: uint\n"
            + "root = {fields}");

        Assert.Multiple(() =>
        {
            Assert.That(document.Rules[0].Kind, Is.EqualTo(CddlRuleKind.Group));
            Assert.That(document.Rules[0].Value!.Name, Is.EqualTo("name"));
            Assert.That(document.Rules[1].Kind,
                Is.EqualTo(CddlRuleKind.GroupExtension));
            Assert.That(document.Rules[1].Value!.Occurrence,
                Is.EqualTo(CddlOccurrenceKind.Optional));
            Assert.That(document.Rules[1].Value!.Name, Is.EqualTo("age"));
            Assert.That(document.Rules[2].Kind, Is.EqualTo(CddlRuleKind.Type));
        });
    }

    [TestCase("root = [a", "CDDL_PARSE_CONTAINER_CLOSE_EXPECTED", 9)]
    [TestCase("root = {a]", "CDDL_PARSE_CONTAINER_CLOSE_EXPECTED", 9)]
    [TestCase("root = (a,b]", "CDDL_PARSE_PARENTHESIS_CLOSE_EXPECTED", 11)]
    [TestCase("root = [a,,b]", "CDDL_PARSE_CONTAINER_CLOSE_EXPECTED", 10)]
    [TestCase("root = [a ^ b]", "CDDL_PARSE_CUT_ARROW_EXPECTED", 12)]
    [TestCase("root = [[]: int]", "CDDL_PARSE_MEMBER_KEY_INVALID", 8)]
    [TestCase("root = [?]", "CDDL_PARSE_TYPE_EXPECTED", 9)]
    [TestCase("root = [key:]", "CDDL_PARSE_TYPE_EXPECTED", 12)]
    [TestCase("root = [-1*2 value]", "CDDL_PARSE_OCCURRENCE_BOUND_INVALID", 8)]
    [TestCase("root = [uint / tstr => any]", "CDDL_PARSE_MEMBER_KEY_INVALID", 8)]
    public void ReportsMalformedContainerAndEntrySyntaxAtExactOffsets(
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
        });
    }

    [Test]
    public void EnforcesConfiguredNestingDepth()
    {
        CddlParseResult result = Parse(
            "root = [{[a]}]",
            new CddlParseOptions { MaxNestingDepth = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo("CDDL_PARSE_DEPTH_LIMIT"));
            Assert.That(result.Diagnostics[0].Span.Start, Is.EqualTo(9));
        });
    }

    private static CddlNode EntryValue(CddlNode entry)
    {
        return entry.Children[entry.Children.Count - 1];
    }

    private static CddlNode ParseRoot(string source)
    {
        return ParseDone(source).Rules[0].Value!;
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

    private static void AssertOccurrence(
        CddlNode entry,
        CddlOccurrenceKind kind,
        int minimum,
        int maximum)
    {
        Assert.Multiple(() =>
        {
            Assert.That(entry.Occurrence, Is.EqualTo(kind));
            Assert.That(entry.MinimumOccurrence, Is.EqualTo(minimum));
            Assert.That(entry.MaximumOccurrence, Is.EqualTo(maximum));
        });
    }
}
