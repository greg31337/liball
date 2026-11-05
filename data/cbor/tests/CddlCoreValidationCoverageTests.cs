using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlCoreValidationCoverageTests
{
    [Test]
    public void ValidatesEveryStandardPreludeRuleAtRuntime()
    {
        Dictionary<string, CborValue> values = CreatePreludeValues();

        Assert.That(values.Keys, Is.EquivalentTo(CddlPrelude.RuleNames));
        int index = 0;
        while (index < CddlPrelude.RuleNames.Length)
        {
            string name = CddlPrelude.RuleNames[index];
            CddlValidationResult result = Validate(
                "root = " + name,
                values[name],
                true);
            Assert.That(result.IsValid, Is.True,
                name + ": " + FirstMessage(result));
            Assert.That(result.Issues, Is.Empty, name);
            index = index + 1;
        }
    }

    [Test]
    public void RejectsWrongKindsAcrossPreludeFamilies()
    {
        AssertInvalid("root = uint", Negative(-1), true);
        AssertInvalid("root = bstr", Text("bytes"), true);
        AssertInvalid(
            "root = tdate",
            Tag(CborTagNumbers.EpochDateTime, Unsigned(0)),
            true);
        AssertInvalid(
            "root = float16",
            Float(1.0, CborFloatWidth.Single),
            true);
        AssertInvalid("root = bool", Simple(CborSimpleValues.Null), true);
        AssertInvalid(
            "root = decfrac",
            Tag(
                CborTagNumbers.DecimalFraction,
                Array(Text("bad"), Unsigned(1))),
            true);
    }

    [Test]
    public void ValidatesEveryHeadAndFloatingPointRepresentationWidth()
    {
        AssertValid("root = #0.0", WithHeadWidth(Unsigned(0), 0), false);
        AssertValid("root = #0.24", WithHeadWidth(Unsigned(1), 1), false);
        AssertValid("root = #0.25", WithHeadWidth(Unsigned(1), 2), false);
        AssertValid("root = #0.26", WithHeadWidth(Unsigned(1), 4), false);
        AssertValid("root = #0.27", WithHeadWidth(Unsigned(1), 8), false);
        AssertValid("root = #1.27", WithHeadWidth(Negative(-1), 8), false);
        AssertValid("root = #2.25", WithHeadWidth(Bytes(1), 2), false);
        AssertValid("root = #3.26", WithHeadWidth(Text("x"), 4), false);
        AssertValid(
            "root = #4.27",
            WithHeadWidth(Array(Unsigned(1)), 8),
            false);
        AssertValid(
            "root = #5.24",
            WithHeadWidth(Map(Entry("x", Unsigned(1))), 1),
            false);
        AssertValid(
            "root = #6.25",
            WithHeadWidth(Tag(1, Unsigned(0)), 2),
            false);
        AssertValid(
            "root = #7.25",
            Float(1.0, CborFloatWidth.Half),
            false);
        AssertValid(
            "root = #7.26",
            Float(1.0, CborFloatWidth.Single),
            false);
        AssertValid(
            "root = #7.27",
            Float(1.0, CborFloatWidth.Double),
            false);
        AssertValid("root = #7.24", Simple(32), false);

        CddlValidationResult wrong = Validate(
            "root = #0.25",
            WithHeadWidth(Unsigned(1), 4),
            false);
        Assert.That(wrong.IsValid, Is.False);
        Assert.That(wrong.Issues[0].Code,
            Is.EqualTo(CddlValidationCode.RepresentationMismatch));
    }

    [Test]
    public void ValidatesEveryIndefiniteStringAndContainerRepresentation()
    {
        CborValue bytes = Bytes(1, 2);
        bytes.LengthKind = CborLengthKind.Indefinite;
        CborValue text = Text("ab");
        text.LengthKind = CborLengthKind.Indefinite;
        CborValue array = Array(Unsigned(1));
        array.LengthKind = CborLengthKind.Indefinite;
        CborValue map = Map(Entry("x", Unsigned(1)));
        map.LengthKind = CborLengthKind.Indefinite;

        AssertValid("root = #2.31", bytes, false);
        AssertValid("root = #3.31", text, false);
        AssertValid("root = #4.31", array, false);
        AssertValid("root = #5.31", map, false);
        AssertInvalid("root = #4.31", Array(Unsigned(1)), false);
    }

    [Test]
    public void BacktracksAcrossOrderedAndUnorderedOccurrenceAmbiguityAndCuts()
    {
        AssertValid(
            "root = [* (1 / 2), 2, 3]",
            Array(Unsigned(1), Unsigned(2), Unsigned(3)),
            false);
        AssertValid(
            "root = {#3 => 1, \"x\" => 1}",
            Map(Entry("x", Unsigned(1)), Entry("y", Unsigned(1))),
            false);
        AssertValid(
            "root = {#3 => #0 // \"x\" => #3}",
            Map(Entry("x", Text("value"))),
            false);
        AssertInvalid(
            "root = {#3 ^ => #0 // \"x\" => #3}",
            Map(Entry("x", Text("value"))),
            false);
    }

    [Test]
    public void PreservesPathsThroughGenericTaggedRecursiveStructures()
    {
        string source =
            "root = envelope<node>\n"
            + "envelope<t> = #6.100({payload: t})\n"
            + "node = #3 / [node]";
        CborValue validValue = Tag(
            100,
            Map(Entry("payload", Array(Array(Text("done"))))));
        AssertValid(
            source,
            validValue,
            false);

        CborValue invalidValue = Tag(
            100,
            Map(Entry("payload", Array(Array(Unsigned(1))))));
        CddlValidationResult mismatch = Validate(
            source,
            invalidValue,
            false);
        CddlValidationIssue? issue = FindIssueAtPath(
            mismatch.Issues[0],
            CddlValidationCode.TypeMismatch,
            "$.value[\"payload\"][0][0]");

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue!.InstancePath,
                Is.EqualTo("$.value[\"payload\"][0][0]"));
            Assert.That(issue.SchemaPath, Does.Contain("/rule(node)"));
        });
    }

    private static Dictionary<string, CborValue> CreatePreludeValues()
    {
        Dictionary<string, CborValue> values =
            new Dictionary<string, CborValue>();
        values.Add("any", Map());
        values.Add("uint", Unsigned(1));
        values.Add("nint", Negative(-1));
        values.Add("int", Negative(-2));
        values.Add("bstr", Bytes(1, 2));
        values.Add("bytes", Bytes(3));
        values.Add("tstr", Text("text"));
        values.Add("text", Text("alias"));
        values.Add("tdate", Tag(CborTagNumbers.DateTimeString,
            Text("2026-08-28T00:00:00Z")));
        values.Add("time", Tag(CborTagNumbers.EpochDateTime, Unsigned(0)));
        values.Add("number", Float(1.5, CborFloatWidth.Double));
        values.Add("biguint", Tag(CborTagNumbers.PositiveBignum, Bytes(1)));
        values.Add("bignint", Tag(CborTagNumbers.NegativeBignum, Bytes(1)));
        values.Add("bigint", Tag(CborTagNumbers.PositiveBignum, Bytes(1)));
        values.Add("integer", Tag(CborTagNumbers.NegativeBignum, Bytes(1)));
        values.Add("unsigned", Tag(CborTagNumbers.PositiveBignum, Bytes(1)));
        values.Add("decfrac", Tag(CborTagNumbers.DecimalFraction,
            Array(Unsigned(0), Unsigned(1))));
        values.Add("bigfloat", Tag(CborTagNumbers.Bigfloat,
            Array(Negative(-1), Unsigned(2))));
        values.Add("eb64url", Tag(CborTagNumbers.ExpectedBase64Url,
            Bytes(1)));
        values.Add("eb64legacy", Tag(CborTagNumbers.ExpectedBase64,
            Text("value")));
        values.Add("eb16", Tag(CborTagNumbers.ExpectedBase16, Unsigned(1)));
        values.Add("encoded-cbor", Tag(CborTagNumbers.EncodedCbor,
            Bytes(1)));
        values.Add("uri", Tag(CborTagNumbers.Uri,
            Text("https://example.com")));
        values.Add("b64url", Tag(CborTagNumbers.Base64Url, Text("AQ")));
        values.Add("b64legacy", Tag(CborTagNumbers.Base64, Text("AQ==")));
        values.Add("regexp", Tag(CborTagNumbers.RegularExpression,
            Text("[a-z]+")));
        values.Add("mime-message", Tag(CborTagNumbers.MimeMessage,
            Text("text/plain")));
        values.Add("cbor-any", Tag(CborTagNumbers.SelfDescribedCbor,
            Array()));
        values.Add("float16", Float(1.0, CborFloatWidth.Half));
        values.Add("float32", Float(1.0, CborFloatWidth.Single));
        values.Add("float64", Float(1.0, CborFloatWidth.Double));
        values.Add("float16-32", Float(1.0, CborFloatWidth.Half));
        values.Add("float32-64", Float(1.0, CborFloatWidth.Single));
        values.Add("float", Float(1.0, CborFloatWidth.Double));
        values.Add("false", Simple(CborSimpleValues.False));
        values.Add("true", Simple(CborSimpleValues.True));
        values.Add("bool", Simple(CborSimpleValues.False));
        values.Add("nil", Simple(CborSimpleValues.Null));
        values.Add("null", Simple(CborSimpleValues.Null));
        values.Add("undefined", Simple(CborSimpleValues.Undefined));
        return values;
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        bool includePrelude)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "coverage.cddl" });
        Assert.That(parse.IsSuccess, Is.True,
            parse.Diagnostics.Length == 0 ? "" : parse.Diagnostics[0].Message);
        CddlCompileResult compile = new CddlCompiler().TryCompileSchema(
            parse.Document!,
            new CddlCompileOptions
            {
                IncludeStandardPrelude = includePrelude
            });
        Assert.That(compile.IsSuccess, Is.True,
            compile.Diagnostics.Length == 0
                ? ""
                : compile.Diagnostics[0].Message);
        return new CddlValidator().ValidateCbor(
            compile.Schema!,
            value,
            new CddlValidationOptions());
    }

    private static void AssertValid(
        string source,
        CborValue value,
        bool includePrelude)
    {
        CddlValidationResult result = Validate(source, value, includePrelude);
        Assert.That(result.IsValid, Is.True, FirstMessage(result));
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertInvalid(
        string source,
        CborValue value,
        bool includePrelude)
    {
        CddlValidationResult result = Validate(source, value, includePrelude);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Is.Not.Empty);
    }

    private static string FirstMessage(CddlValidationResult result)
    {
        return result.Issues.Length == 0 ? "" : result.Issues[0].Message;
    }

    private static CddlValidationIssue? FindIssueAtPath(
        CddlValidationIssue issue,
        CddlValidationCode code,
        string path)
    {
        if (issue.Code == code && issue.InstancePath == path)
        {
            return issue;
        }
        int index = 0;
        while (index < issue.Details.Length)
        {
            CddlValidationIssue? found = FindIssueAtPath(
                issue.Details[index],
                code,
                path);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        return null;
    }

    private static CborValue WithHeadWidth(CborValue value, int width)
    {
        value.Representation = new CborRepresentation
        {
            HeadArgumentWidth = width
        };
        return value;
    }

    private static CborValue Unsigned(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Negative(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.NegativeInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Float(double value, CborFloatWidth width)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = width,
            FloatValue = new CborFloatValue
            {
                Value = value,
                Width = width
            }
        };
    }

    private static CborValue Bytes(params byte[] value)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = value
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Simple(byte value)
    {
        return new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = value
        };
    }

    private static CborValue Tag(ulong tag, CborValue value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = tag,
            TaggedValue = value
        };
    }

    private static CborValue Array(params CborValue[] values)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = new List<CborValue>(values)
        };
    }

    private static CborMapEntry Entry(string key, CborValue value)
    {
        return new CborMapEntry
        {
            Key = Text(key),
            Value = value
        };
    }

    private static CborValue Map(params CborMapEntry[] entries)
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = new List<CborMapEntry>(entries)
        };
    }
}
