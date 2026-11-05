using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlAdvancedTypeParserTests
{
    [Test]
    public void ParsesRangesControlsAndTypeChoicesWithCorrectPrecedence()
    {
        CddlDocument document = ParseDone(
            "inclusive = 1..10\n"
            + "exclusive = 1...10\n"
            + "choice = 0 / 1..10 / 20\n"
            + "controlled = tstr .size (1..10) / uint");
        CddlNode inclusive = document.Rules[0].Value!;
        CddlNode exclusive = document.Rules[1].Value!;
        CddlNode choice = document.Rules[2].Value!;
        CddlNode controlledChoice = document.Rules[3].Value!;
        CddlNode control = controlledChoice.Children[0];
        CddlNode controller = control.Children[1];

        Assert.Multiple(() =>
        {
            AssertRange(inclusive, false, 1, 10);
            AssertRange(exclusive, true, 1, 10);
            Assert.That(choice.Kind, Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(choice.Children, Has.Count.EqualTo(3));
            Assert.That(choice.Children[0].IntegerValue, Is.EqualTo(BigInteger.Zero));
            AssertRange(choice.Children[1], false, 1, 10);
            Assert.That(controlledChoice.Kind, Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(control.Kind, Is.EqualTo(CddlNodeKind.Control));
            Assert.That(control.ControlName, Is.EqualTo(CddlControlNames.Size));
            Assert.That(control.Children[0].Name, Is.EqualTo("tstr"));
            Assert.That(controller.Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedType));
            AssertRange(controller.Children[0], false, 1, 10);
            Assert.That(controlledChoice.Children[1].Name, Is.EqualTo("uint"));
        });
    }

    [Test]
    public void AllowsTypeOneRangeAndControlExpressionsAsArrowKeys()
    {
        CddlNode map = ParseRoot(
            "root = {1..10 => uint, tstr .size 4 => text}");
        CddlNode group = map.Children[0];
        CddlNode rangeEntry = group.Children[0];
        CddlNode controlEntry = group.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(rangeEntry.Children[0].Kind, Is.EqualTo(CddlNodeKind.Range));
            Assert.That(EntryValue(rangeEntry).Name, Is.EqualTo("uint"));
            Assert.That(controlEntry.Children[0].Kind,
                Is.EqualTo(CddlNodeKind.Control));
            Assert.That(controlEntry.Children[0].ControlName,
                Is.EqualTo(CddlControlNames.Size));
            Assert.That(EntryValue(controlEntry).Name, Is.EqualTo("text"));
        });
    }

    [Test]
    public void ParsesUnwrappingAndNamedOrInlineGroupEnumeration()
    {
        CddlDocument document = ParseDone(
            "unwrapped = ~base\n"
            + "named = &fields\n"
            + "inline = &(a, b // c)");
        CddlNode unwrap = document.Rules[0].Value!;
        CddlNode named = document.Rules[1].Value!;
        CddlNode inline = document.Rules[2].Value!;
        CddlNode parenthesized = inline.Children[0];
        CddlNode choice = parenthesized.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(unwrap.Kind, Is.EqualTo(CddlNodeKind.Unwrap));
            Assert.That(unwrap.Name, Is.EqualTo("base"));
            Assert.That(named.Kind, Is.EqualTo(CddlNodeKind.GroupEnumeration));
            Assert.That(named.Name, Is.EqualTo("fields"));
            Assert.That(inline.Kind, Is.EqualTo(CddlNodeKind.GroupEnumeration));
            Assert.That(parenthesized.Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
            Assert.That(choice.Kind, Is.EqualTo(CddlNodeKind.GroupChoice));
            Assert.That(choice.Children[0].Children, Has.Count.EqualTo(2));
            Assert.That(choice.Children[1].Children, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ParsesAnyMajorAndAdditionalInformationRepresentations()
    {
        CddlDocument document = ParseDone(
            "any = #\n"
            + "unsigned = #0\n"
            + "negative = #1.27\n"
            + "tagMajor = #6.24\n"
            + "simple = #7.20\n"
            + "typedSimple = #7.<simple-number>");
        CddlNode any = document.Rules[0].Value!;
        CddlNode unsigned = document.Rules[1].Value!;
        CddlNode negative = document.Rules[2].Value!;
        CddlNode tagMajor = document.Rules[3].Value!;
        CddlNode simple = document.Rules[4].Value!;
        CddlNode typedSimple = document.Rules[5].Value!;

        Assert.Multiple(() =>
        {
            AssertRepresentation(any, -1, false, BigInteger.Zero);
            AssertRepresentation(unsigned, 0, false, BigInteger.Zero);
            AssertRepresentation(negative, 1, true, 27);
            AssertRepresentation(tagMajor, 6, true, 24);
            AssertRepresentation(simple, 7, true, 20);
            AssertRepresentation(typedSimple, 7, false, BigInteger.Zero);
            Assert.That(typedSimple.Children, Has.Count.EqualTo(1));
            Assert.That(typedSimple.Children[0].Name, Is.EqualTo("simple-number"));
            Assert.That(unsigned.Span.Length, Is.EqualTo(2));
            Assert.That(negative.Span.Length, Is.EqualTo(5));
        });
    }

    [Test]
    public void ParsesLiteralAndRfc9682TypeValuedTagArguments()
    {
        CddlDocument document = ParseDone(
            "dated = #6.0(tstr)\n"
            + "anyTag = #6(any)\n"
            + "typedTag = #6.<tag-number>(uint / tstr)\n"
            + "rangedTag = #6.<1..10>(bstr)");
        CddlNode dated = document.Rules[0].Value!;
        CddlNode anyTag = document.Rules[1].Value!;
        CddlNode typedTag = document.Rules[2].Value!;
        CddlNode rangedTag = document.Rules[3].Value!;

        Assert.Multiple(() =>
        {
            Assert.That(dated.Kind, Is.EqualTo(CddlNodeKind.Tag));
            Assert.That(dated.MajorType, Is.EqualTo(6));
            Assert.That(dated.HasArgumentValue, Is.True);
            Assert.That(dated.ArgumentValue, Is.EqualTo(BigInteger.Zero));
            Assert.That(dated.Children, Has.Count.EqualTo(1));
            Assert.That(dated.Children[0].Name, Is.EqualTo("tstr"));
            Assert.That(anyTag.HasArgumentValue, Is.False);
            Assert.That(anyTag.Children[0].Name, Is.EqualTo("any"));
            Assert.That(typedTag.HasArgumentValue, Is.False);
            Assert.That(typedTag.Children, Has.Count.EqualTo(2));
            Assert.That(typedTag.Children[0].Name, Is.EqualTo("tag-number"));
            Assert.That(typedTag.Children[1].Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(rangedTag.Children[0].Kind, Is.EqualTo(CddlNodeKind.Range));
            Assert.That(rangedTag.Children[1].Name, Is.EqualTo("bstr"));
        });
    }

    [TestCase("root = 1..", "CDDL_PARSE_TYPE_EXPECTED", 10)]
    [TestCase("root = tstr . 1", "CDDL_PARSE_CONTROL_NAME_EXPECTED", 14)]
    [TestCase("root = tstr . size 1", "CDDL_PARSE_CONTROL_NAME_EXPECTED", 14)]
    [TestCase("root = tstr .size", "CDDL_PARSE_TYPE_EXPECTED", 17)]
    [TestCase("root = ~1", "CDDL_PARSE_UNWRAP_NAME_EXPECTED", 8)]
    [TestCase("root = &[]", "CDDL_PARSE_GROUP_ENUMERATION_EXPECTED", 8)]
    [TestCase("root = #8", "CDDL_PARSE_MAJOR_TYPE_INVALID", 8)]
    [TestCase("root = #6.", "CDDL_PARSE_HEAD_ARGUMENT_EXPECTED", 10)]
    [TestCase("root = #6. 24", "CDDL_PARSE_HEAD_ARGUMENT_EXPECTED", 11)]
    [TestCase("root = #7.< uint>", "CDDL_PARSE_HEAD_ARGUMENT_EXPECTED", 12)]
    [TestCase("root = #7.<uint >", "CDDL_PARSE_HEAD_ARGUMENT_CLOSE_EXPECTED", 16)]
    [TestCase("root = #0.<uint>", "CDDL_PARSE_REPRESENTATION_ARGUMENT_INVALID", 10)]
    [TestCase("root = #6.<uint>", "CDDL_PARSE_TAG_CONTENT_EXPECTED", 16)]
    [TestCase("root = #7.<uint", "CDDL_PARSE_HEAD_ARGUMENT_CLOSE_EXPECTED", 15)]
    [TestCase("root = #6.24(tstr", "CDDL_PARSE_TAG_CONTENT_CLOSE_EXPECTED", 17)]
    public void ReportsAdvancedTypeErrorsAtExactOffsets(
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
    public void RejectsASecondTypeOneOperatorWithoutParentheses()
    {
        CddlParseResult rangeThenControl = Parse("root = 1..10 .size 2");
        CddlParseResult controlThenRange = Parse("root = uint .size 2..4");

        Assert.Multiple(() =>
        {
            Assert.That(rangeThenControl.IsSuccess, Is.False);
            Assert.That(rangeThenControl.Diagnostics[0].Code,
                Is.EqualTo("CDDL_PARSE_RULE_END_EXPECTED"));
            Assert.That(controlThenRange.IsSuccess, Is.False);
            Assert.That(controlThenRange.Diagnostics[0].Code,
                Is.EqualTo("CDDL_PARSE_RULE_END_EXPECTED"));
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

    private static CddlParseResult Parse(string source)
    {
        return new CddlParser().TryParseDocument(source, new CddlParseOptions());
    }

    private static CddlDocument ParseDone(string source)
    {
        CddlParseResult result = Parse(source);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Document, Is.Not.Null);
        return result.Document!;
    }

    private static void AssertRange(
        CddlNode range,
        bool exclusive,
        int lower,
        int upper)
    {
        Assert.Multiple(() =>
        {
            Assert.That(range.Kind, Is.EqualTo(CddlNodeKind.Range));
            Assert.That(range.IsRangeUpperExclusive, Is.EqualTo(exclusive));
            Assert.That(range.Children, Has.Count.EqualTo(2));
            Assert.That(range.Children[0].IntegerValue,
                Is.EqualTo(new BigInteger(lower)));
            Assert.That(range.Children[1].IntegerValue,
                Is.EqualTo(new BigInteger(upper)));
        });
    }

    private static void AssertRepresentation(
        CddlNode representation,
        int majorType,
        bool hasArgument,
        BigInteger argument)
    {
        Assert.Multiple(() =>
        {
            Assert.That(representation.Kind, Is.EqualTo(CddlNodeKind.Representation));
            Assert.That(representation.MajorType, Is.EqualTo(majorType));
            Assert.That(representation.HasArgumentValue, Is.EqualTo(hasArgument));
            Assert.That(representation.ArgumentValue, Is.EqualTo(argument));
        });
    }
}
