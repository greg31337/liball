using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlCoreControlMatcherTests
{
    [Test]
    public void MatchesByteAndTextStringSizesInBytes()
    {
        AssertValid("root = #2 .size 3", Bytes(1, 2, 3));
        AssertInvalid("root = #2 .size 3", Bytes(1, 2));
        AssertValid("root = #3 .size 2", Text("é"));
        AssertInvalid("root = #3 .size 1", Text("é"));
        AssertValid("root = #3 .size (2..4)", Text("éa"));

        CddlValidationResult mismatch = Validate(
            "root = #3 .size 1",
            Text("é"));
        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.SizeMismatch));
            Assert.That(mismatch.Issues[0].SchemaPath,
                Does.Contain("/control(.size)"));
        });
    }

    [Test]
    public void AppliesUnsignedIntegerSizeAsAnUpperByteBound()
    {
        AssertValid("root = #0 .size 0", Unsigned(0));
        AssertInvalid("root = #0 .size 0", Unsigned(1));
        AssertValid("root = #0 .size 1", Unsigned(0));
        AssertValid("root = #0 .size 1", Unsigned(255));
        AssertInvalid("root = #0 .size 1", Unsigned(256));
        AssertValid("root = #0 .size 3", Unsigned(1));
        AssertValid("root = #0 .size (2..3)", Unsigned(256));
        AssertValid(
            "root = #0 .size allowed\nallowed = 1 / 3",
            Unsigned(65536));
        AssertInvalid(
            "root = #0 .size allowed\nallowed = 1 / 3",
            Unsigned(16777216));
    }

    [Test]
    public void AllowsOnlyControllerSelectedIntegerBits()
    {
        string source =
            "root = #0 .bits flags\n"
            + "flags = &(read: 0, write: 2, high: 8)";
        AssertValid(source, Unsigned(0));
        AssertValid(source, Unsigned(1 | 4 | 256));
        CddlValidationResult invalid = Validate(source, Unsigned(2));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(invalid.Issues[0].Actual, Is.EqualTo("bit 1 is set"));
            Assert.That(invalid.Issues[0].SchemaPath,
                Does.Contain("/control(.bits)"));
        });
    }

    [Test]
    public void NumbersByteStringBitsLeastSignificantBitFirst()
    {
        string source = "root = #2 .bits (0 / 8 / 15)";
        AssertValid(source, Bytes());
        AssertValid(source, Bytes(0, 0));
        AssertValid(source, Bytes(1, 129));
        AssertInvalid(source, Bytes(2));
        AssertInvalid(source, Bytes(0, 1, 1));
    }

    [Test]
    public void UsesImplicitWholeStringXsdMatchingAndLiteralAnchors()
    {
        AssertValid("root = #3 .regexp \"a\"", Text("a"));
        AssertInvalid("root = #3 .regexp \"a\"", Text("ba"));
        AssertInvalid("root = #3 .regexp \"a\"", Text("ab"));
        AssertValid("root = #3 .regexp \".*a.*\"", Text("ba!"));
        AssertValid("root = #3 .regexp \"^a$\"", Text("^a$"));
        AssertInvalid("root = #3 .regexp \"^a$\"", Text("a"));
        AssertInvalid("root = #3 .regexp \".\"", Text("\r"));
        AssertInvalid("root = #3 .regexp \".\"", Text("\n"));
    }

    [Test]
    public void MatchesXsdClassesSubtractionCategoriesAndNameEscapes()
    {
        AssertValid(
            "root = #3 .regexp \"[a-z-[aeiou]]+\"",
            Text("bcdf"));
        AssertInvalid(
            "root = #3 .regexp \"[a-z-[aeiou]]+\"",
            Text("face"));
        AssertValid(
            "root = #3 .regexp \"\\\\p{Lu}+\"",
            Text("ABCÉ"));
        AssertInvalid(
            "root = #3 .regexp \"\\\\p{Lu}+\"",
            Text("AbC"));
        AssertValid(
            "root = #3 .regexp \"\\\\p{IsBasicLatin}+\"",
            Text("Az09"));
        AssertInvalid(
            "root = #3 .regexp \"\\\\p{IsBasicLatin}+\"",
            Text("Ω"));
        AssertValid(
            "root = #3 .regexp \"\\\\i\\\\c*\"",
            Text("name-2"));
        AssertInvalid(
            "root = #3 .regexp \"\\\\i\\\\c*\"",
            Text("2name"));
    }

    [Test]
    public void UsesXsdUnicodeMeaningsForDigitWordAndScalarCharacters()
    {
        AssertValid(
            "root = #3 .regexp \"\\\\d+\"",
            Text("١٢"));
        AssertInvalid(
            "root = #3 .regexp \"\\\\d+\"",
            Text("²"));
        AssertValid(
            "root = #3 .regexp \"\\\\w+\"",
            Text("A€١"));
        AssertInvalid(
            "root = #3 .regexp \"\\\\w+\"",
            Text("A_B"));
        AssertValid("root = #3 .regexp \".\"", Text("😀"));
    }

    [Test]
    public void MatchesXsdAlternationGroupingAndQuantifierForms()
    {
        string source = "root = #3 .regexp \"(ab|c){2,3}d?\"";
        AssertValid(source, Text("abcd"));
        AssertValid(source, Text("ccc"));
        AssertInvalid(source, Text("ab"));
        AssertInvalid(source, Text("abababab"));
        AssertValid("root = #3 .regexp \"a{2,}\"", Text("aaaa"));
        AssertValid("root = #3 .regexp \"\"", Text(""));
        AssertInvalid("root = #3 .regexp \"\"", Text("a"));
    }

    [TestCase("[")]
    [TestCase("a{2,1}")]
    [TestCase("\\x41")]
    [TestCase("a??")]
    [TestCase("\\p{NotACategory}")]
    public void RejectsMalformedXsdPatternsWithoutRegexFallback(string pattern)
    {
        string escaped = pattern.Replace("\\", "\\\\",
            StringComparison.Ordinal).Replace("\"", "\\\"",
            StringComparison.Ordinal);
        CddlValidationResult result = Validate(
            "root = #3 .regexp \"" + escaped + "\"",
            Text("anything"));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
        });
    }

    [Test]
    public void AppliesPatternStateBudgetsToParsingAndMatching()
    {
        CddlValidationResult parsing = Validate(
            "root = #3 .regexp \"abcdef\"",
            Text("abcdef"),
            new CddlValidationOptions { MaxPatternStates = 2 });
        CddlValidationResult matching = Validate(
            "root = #3 .regexp \"(a|aa)*b\"",
            Text("aaaaaaaaab"),
            new CddlValidationOptions { MaxPatternStates = 15 });
        CddlValidationResult boundedEmpty = Validate(
            "root = #3 .regexp \"(){1000000}\"",
            Text(""),
            new CddlValidationOptions { MaxPatternStates = 20 });

        Assert.Multiple(() =>
        {
            Assert.That(parsing.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(matching.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(boundedEmpty.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    [Test]
    public void DecodesExactlyOneEmbeddedCborItem()
    {
        string source = "root = #2 .cbor [1, #3]";
        AssertValid(source, Bytes(0x82, 0x01, 0x62, 0x6f, 0x6b));
        AssertInvalid(source, Bytes(0x82, 0x01, 0x02));

        CddlValidationResult trailing = Validate(
            "root = #2 .cbor 1",
            Bytes(0x01, 0x02));

        Assert.Multiple(() =>
        {
            Assert.That(trailing.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(trailing.Issues[0].InstancePath,
                Is.EqualTo("$.embedded"));
            Assert.That(trailing.Issues[0].Actual,
                Does.Contain("CBOR_TRAILING_DATA"));
        });
    }

    [Test]
    public void DecodesEmbeddedCborSequencesAsArrays()
    {
        string source = "root = #2 .cborseq [1, #3]";
        AssertValid(source, Bytes(0x01, 0x62, 0x6f, 0x6b));
        AssertInvalid(source, Bytes(0x01, 0x02));
        AssertValid("root = #2 .cborseq []", Bytes());

        CddlValidationResult malformed = Validate(
            source,
            Bytes(0x01, 0x18));

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(malformed.Issues[0].InstancePath,
                Is.EqualTo("$.embedded[1]"));
            Assert.That(malformed.Issues[0].Actual,
                Does.Contain("CBOR_UNEXPECTED_END_OF_DATA"));
        });
    }

    [Test]
    public void SharesEmbeddedByteBudgetsAcrossNestedControls()
    {
        string source = "root = #2 .cbor (#2 .cbor 1)";
        CborValue value = Bytes(0x41, 0x01);

        CddlValidationResult exact = Validate(
            source,
            value,
            new CddlValidationOptions { MaxEmbeddedBytes = 3 });
        CddlValidationResult limited = Validate(
            source,
            value,
            new CddlValidationOptions { MaxEmbeddedBytes = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(exact.IsValid, Is.True);
            Assert.That(limited.IsValid, Is.False);
            Assert.That(limited.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(limited.Issues[0].InstancePath,
                Is.EqualTo("$.embedded"));
        });
    }

    [Test]
    public void ReportsControllerMismatchesAtNestedEmbeddedPaths()
    {
        CddlValidationResult mismatch = Validate(
            "root = #2 .cborseq [1, [#3]]",
            Bytes(0x01, 0x81, 0x02));
        CddlValidationIssue? nested = FindIssueAtPath(
            mismatch.Issues[0],
            "$.embedded[1][0]");

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(mismatch.Issues[0].InstancePath,
                Is.EqualTo("$.embedded"));
            Assert.That(nested, Is.Not.Null);
            Assert.That(nested!.Code,
                Is.EqualTo(CddlValidationCode.TypeMismatch));
        });
    }

    [Test]
    public void IntersectsWithinAndAndControlOperands()
    {
        AssertValid("root = #0 .and (1..3)", Unsigned(2));
        AssertInvalid("root = #0 .and (1..3)", Unsigned(4));
        AssertValid("root = #0 .within (1..3)", Unsigned(1));

        CddlValidationResult mismatch = Validate(
            "root = #0 .within (1..3)",
            Unsigned(4));

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(mismatch.Issues[0].SchemaPath,
                Does.Contain("/control(.within)"));
            Assert.That(mismatch.Issues[0].Details,
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ComparesIntegerAndFloatingPointNumbersExactly()
    {
        AssertValid("root = #0 .lt 2", Unsigned(1));
        AssertInvalid("root = #0 .lt 2", Unsigned(2));
        AssertValid("root = #0 .le 2", Unsigned(2));
        AssertValid("root = #1 .gt -3", Negative(-2));
        AssertValid("root = #1 .ge -2", Negative(-2));
        AssertValid("root = #0 .lt 1.5", Unsigned(1));
        AssertValid(
            "root = (#0 / #7.27) .gt 1",
            Float(1.5));
        AssertInvalid(
            "root = (#0 / #7.27) .gt 1",
            Float(double.NaN));
    }

    [Test]
    public void EqualityRequiresTheExactCborValueAndType()
    {
        string equal = "root = (#0 / #7.27) .eq 1";
        string notEqual = "root = (#0 / #7.27) .ne 1";

        AssertValid(equal, Unsigned(1));
        AssertInvalid(equal, Float(1.0));
        AssertInvalid(notEqual, Unsigned(1));
        AssertValid(notEqual, Float(1.0));
        AssertValid("root = #7.21 .eq #7.21", Simple(21));
    }

    [Test]
    public void EqualityMatchesCompoundSingletonValuesRecursively()
    {
        string arraySource = "root = [#0, #3] .eq [1, \"x\"]";
        AssertValid(arraySource, Array(Unsigned(1), Text("x")));
        AssertInvalid(arraySource, Array(Unsigned(1), Text("y")));

        string mapSource = "root = {#3 => #0} .eq {\"a\": 1}";
        AssertValid(mapSource, Map(Entry(Text("a"), Unsigned(1))));
        AssertInvalid(mapSource, Map(Entry(Text("a"), Unsigned(2))));

        string pairMapSource =
            "root = {2* (#3 => #0)} .eq {\"a\": 1, \"b\": 2}";
        AssertValid(
            pairMapSource,
            Map(
                Entry(Text("b"), Unsigned(2)),
                Entry(Text("a"), Unsigned(1))));
        AssertInvalid(
            pairMapSource,
            Map(
                Entry(Text("b"), Unsigned(3)),
                Entry(Text("a"), Unsigned(1))));
    }

    [Test]
    public void DefaultAllowsOmissionAndRejectsAnExplicitDefaultValue()
    {
        string source =
            "root = {? displayed-step: (#0 .gt 0) .default 1}";

        AssertValid(source, Map());
        AssertValid(
            source,
            Map(Entry(Text("displayed-step"), Unsigned(2))));
        CddlValidationResult explicitDefault = Validate(
            source,
            Map(Entry(Text("displayed-step"), Unsigned(1))));
        AssertInvalid(
            source,
            Map(Entry(Text("displayed-step"), Unsigned(0))));

        CddlValidationIssue? issue = FindIssueByCodeAtPath(
            explicitDefault.Issues[0],
            "$[\"displayed-step\"]",
            CddlValidationCode.ControlMismatch);
        Assert.Multiple(() =>
        {
            Assert.That(explicitDefault.IsValid, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue!.Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(issue.SchemaPath,
                Does.Contain("/control(.default)"));
        });
    }

    private static CddlValidationIssue? FindIssueAtPath(
        CddlValidationIssue issue,
        string path)
    {
        if (issue.InstancePath == path)
        {
            return issue;
        }
        int index = 0;
        while (index < issue.Details.Length)
        {
            CddlValidationIssue? found = FindIssueAtPath(
                issue.Details[index],
                path);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        return null;
    }

    private static CddlValidationIssue? FindIssueByCodeAtPath(
        CddlValidationIssue issue,
        string path,
        CddlValidationCode code)
    {
        if (issue.InstancePath == path && issue.Code == code)
        {
            return issue;
        }
        int index = 0;
        while (index < issue.Details.Length)
        {
            CddlValidationIssue? found = FindIssueByCodeAtPath(
                issue.Details[index],
                path,
                code);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        return null;
    }

    private static void AssertValid(string source, CborValue value)
    {
        CddlValidationResult result = Validate(source, value);
        Assert.That(result.IsValid, Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertInvalid(string source, CborValue value)
    {
        CddlValidationResult result = Validate(source, value);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Is.Not.Empty);
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        CddlValidationOptions? options = null)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "controls.cddl" });
        Assert.That(parse.IsSuccess, Is.True,
            parse.Diagnostics.Length == 0 ? "" : parse.Diagnostics[0].Message);
        CddlCompileResult compile = new CddlCompiler().TryCompileSchema(
            parse.Document!,
            new CddlCompileOptions { IncludeStandardPrelude = false });
        Assert.That(compile.IsSuccess, Is.True,
            compile.Diagnostics.Length == 0
                ? ""
                : compile.Diagnostics[0].Message);
        return new CddlValidator().ValidateCbor(
            compile.Schema!,
            value,
            options ?? new CddlValidationOptions());
    }

    private static CborValue Unsigned(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
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

    private static CborValue Negative(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.NegativeInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Float(double value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = CborFloatWidth.Double,
            FloatValue = new CborFloatValue
            {
                Value = value,
                Width = CborFloatWidth.Double
            }
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

    private static CborValue Array(params CborValue[] values)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = new List<CborValue>(values)
        };
    }

    private static CborMapEntry Entry(CborValue key, CborValue value)
    {
        return new CborMapEntry
        {
            Key = key,
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
