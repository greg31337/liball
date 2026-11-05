using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlCoreMatcherTests
{
    [Test]
    public void MatchesEveryLiteralKindAndReportsExactMismatches()
    {
        AssertValid("root = 42", Unsigned(42), NoPrelude());
        AssertValid("root = -2", Negative(-2), NoPrelude());
        AssertValid("root = 1.5", Float(1.5, CborFloatWidth.Double),
            NoPrelude());
        AssertValid("root = \"hello\"", Text("hello"), NoPrelude());
        AssertValid("root = h'0102'", Bytes(new byte[] { 1, 2 }), NoPrelude());

        CddlValidationResult wrongInteger = Validate(
            "root = 42",
            Unsigned(41),
            NoPrelude(),
            new CddlValidationOptions());
        CddlValidationResult wrongKind = Validate(
            "root = 42",
            Float(42, CborFloatWidth.Double),
            NoPrelude(),
            new CddlValidationOptions());
        CddlValidationResult wrongBytes = Validate(
            "root = h'0102'",
            Bytes(new byte[] { 1, 3 }),
            NoPrelude(),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(wrongInteger.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.LiteralMismatch));
            Assert.That(wrongInteger.Issues[0].Expected, Is.EqualTo("42"));
            Assert.That(wrongKind.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.LiteralMismatch));
            Assert.That(wrongBytes.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.LiteralMismatch));
        });
    }

    [Test]
    public void MatchesPreludeScalarsAliasesAndTaggedScalars()
    {
        CddlCompileOptions prelude = new CddlCompileOptions();
        AssertValid("root = any", Array(), prelude);
        AssertValid("root = uint", Unsigned(1), prelude);
        AssertValid("root = nint", Negative(-1), prelude);
        AssertValid("root = int", Negative(-9), prelude);
        AssertValid("root = bstr", Bytes(new byte[] { 4 }), prelude);
        AssertValid("root = text", Text("value"), prelude);
        AssertValid("root = number", Float(2.5, CborFloatWidth.Single),
            prelude);
        AssertValid("root = float16", Float(1, CborFloatWidth.Half),
            prelude);
        AssertValid("root = true", Simple(CborSimpleValues.True), prelude);
        AssertValid("root = null", Simple(CborSimpleValues.Null), prelude);
        AssertValid(
            "root = tdate",
            Tag(0, Text("2026-08-28T00:00:00Z")),
            prelude);

        CddlValidationResult wrongPrelude = Validate(
            "root = uint",
            Negative(-1),
            prelude,
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(wrongPrelude.IsValid, Is.False);
            Assert.That(wrongPrelude.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.TypeMismatch));
            Assert.That(wrongPrelude.Issues[0].RuleName, Is.EqualTo("uint"));
        });
    }

    [Test]
    public void MatchesMajorTypesAndExactAdditionalInformation()
    {
        CborValue preferredOneByte = Unsigned(24);
        CborValue retainedOneByte = Unsigned(1);
        retainedOneByte.Representation = new CborRepresentation
        {
            HeadArgumentWidth = 1
        };
        CborValue indefiniteBytes = Bytes(new byte[] { 1 });
        indefiniteBytes.LengthKind = CborLengthKind.Indefinite;

        AssertValid("root = #0.24", preferredOneByte, NoPrelude());
        AssertValid("root = #0.24", retainedOneByte, NoPrelude());
        AssertValid("root = #2.31", indefiniteBytes, NoPrelude());
        AssertValid("root = #4", Array(), NoPrelude());
        AssertValid("root = #5", Map(), NoPrelude());
        AssertValid("root = #7.25", Float(1, CborFloatWidth.Half),
            NoPrelude());
        AssertValid("root = #7.24", Simple(32), NoPrelude());
        AssertValid("root = #7.255", Simple(255), NoPrelude());
        AssertValid("root = #7.<20..23>", Simple(21), NoPrelude());

        CddlValidationResult wrongWidth = Validate(
            "root = #0.24",
            Unsigned(256),
            NoPrelude(),
            new CddlValidationOptions());
        CddlValidationResult wrongFloatWidth = Validate(
            "root = #7.25",
            Float(1, CborFloatWidth.Single),
            NoPrelude(),
            new CddlValidationOptions());
        CddlValidationResult definite = Validate(
            "root = #2.31",
            Bytes(new byte[] { 1 }),
            NoPrelude(),
            new CddlValidationOptions());
        CddlValidationResult reservedSimple = Validate(
            "root = #7.24",
            Simple(24),
            NoPrelude(),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(wrongWidth.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.RepresentationMismatch));
            Assert.That(wrongFloatWidth.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.RepresentationMismatch));
            Assert.That(definite.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.RepresentationMismatch));
            Assert.That(reservedSimple.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.RepresentationMismatch));
        });
    }

    [Test]
    public void MatchesIntegerAndFloatRangesWithReferencedEndpoints()
    {
        string integerSource =
            "root = lower ... upper\nlower = -2\nupper = 3";
        AssertValid(integerSource, Negative(-2), NoPrelude());
        AssertValid(integerSource, Unsigned(2), NoPrelude());
        AssertInvalid(
            integerSource,
            Unsigned(3),
            NoPrelude(),
            CddlValidationCode.RangeMismatch);
        AssertValid("root = 1.5..2.5", Float(2.5, CborFloatWidth.Double),
            NoPrelude());
        AssertInvalid(
            "root = 1.5...2.5",
            Float(2.5, CborFloatWidth.Double),
            NoPrelude(),
            CddlValidationCode.RangeMismatch);
        AssertInvalid(
            "root = 1..3",
            Float(2, CborFloatWidth.Double),
            NoPrelude(),
            CddlValidationCode.RangeMismatch);
    }

    [Test]
    public void MatchesChoicesReferencesAndMemoizedGenericRules()
    {
        string source =
            "root = alias / identity<7> / \"ok\"\n"
            + "alias = 1\n"
            + "identity<t> = t";
        AssertValid(source, Unsigned(1), NoPrelude());
        AssertValid(source, Unsigned(7), NoPrelude());
        AssertValid(source, Text("ok"), NoPrelude());
        CddlValidationResult mismatch = Validate(
            source,
            Text("no"),
            NoPrelude(),
            new CddlValidationOptions());
        CddlValidationOptions limitedOptions = new CddlValidationOptions
        {
            MaxAlternatives = 1
        };
        CddlValidationResult limited = Validate(
            source,
            Unsigned(7),
            NoPrelude(),
            limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(mismatch.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ChoiceMismatch));
            Assert.That(mismatch.Issues[0].Details, Has.Length.EqualTo(3));
            Assert.That(mismatch.Issues[0].Details.Select(issue =>
                    issue.SchemaPath),
                Is.All.Contains("/choice["));
            Assert.That(limited.IsValid, Is.False);
            Assert.That(limited.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    [Test]
    public void MatchesLiteralAndTypeValuedSemanticTags()
    {
        AssertValid(
            "root = #6.42(#3)",
            Tag(42, Text("inside")),
            NoPrelude());
        AssertValid(
            "root = #6.<40..50>(#0)",
            Tag(45, Unsigned(3)),
            NoPrelude());
        CddlValidationResult wrongTag = Validate(
            "root = #6.42(#3)",
            Tag(43, Text("inside")),
            NoPrelude(),
            new CddlValidationOptions());
        CddlValidationResult wrongTypedTag = Validate(
            "root = #6.<40..50>(#0)",
            Tag(60, Unsigned(3)),
            NoPrelude(),
            new CddlValidationOptions());
        CddlValidationResult wrongContent = Validate(
            "root = #6.42(#3)",
            Tag(42, Unsigned(3)),
            NoPrelude(),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(wrongTag.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.LiteralMismatch));
            Assert.That(wrongTypedTag.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.RangeMismatch));
            Assert.That(wrongContent.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.TypeMismatch));
            Assert.That(wrongContent.Issues[0].InstancePath,
                Is.EqualTo("$.value"));
        });
    }

    [Test]
    public void MatchesUnwrappedTaggedContent()
    {
        string source =
            "root = exposed\n"
            + "exposed = ~wrapped\n"
            + "wrapped = #6.1(#0)";
        AssertValid(source, Unsigned(3), NoPrelude());
        CddlValidationResult mismatch = Validate(
            source,
            Negative(-1),
            NoPrelude(),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(mismatch.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.TypeMismatch));
            Assert.That(mismatch.Issues[0].RuleName, Is.EqualTo("wrapped"));
        });
    }

    [Test]
    public void MatchesNamedInlineAndIncludedGroupEnumerations()
    {
        string named =
            "root = &colors\n"
            + "colors = (black: 0, red: 1, extra)\n"
            + "extra = (blue: 2,)";
        AssertValid(named, Unsigned(0), NoPrelude());
        AssertValid(named, Unsigned(2), NoPrelude());
        AssertValid("root = &(green: 3, yellow: 4)", Unsigned(4),
            NoPrelude());
        CddlValidationResult mismatch = Validate(
            named,
            Unsigned(9),
            NoPrelude(),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(mismatch.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ChoiceMismatch));
            Assert.That(mismatch.Issues[0].Details, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void ProtectsNonConsumingRecursiveReferences()
    {
        string source = "root = 1 / root";
        AssertValid(source, Unsigned(1), NoPrelude());
        CddlValidationResult mismatch = Validate(
            source,
            Unsigned(2),
            NoPrelude(),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(mismatch.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ChoiceMismatch));
            Assert.That(FlattenCodes(mismatch.Issues[0]),
                Does.Contain(CddlValidationCode.RecursionLimit));
        });
    }

    [Test]
    public void HonorsSelectedRootsStepLimitsAndMissingInputs()
    {
        CddlSchema schema = Compile(
            "first = 1\nselected = 2",
            NoPrelude());
        CddlValidationOptions selectedOptions = new CddlValidationOptions
        {
            RootRuleName = "selected"
        };
        CddlValidationResult selected = new CddlValidator().ValidateCbor(
            schema,
            Unsigned(2),
            selectedOptions);
        CddlValidationOptions limitedOptions = new CddlValidationOptions
        {
            MaxSteps = 0
        };
        CddlValidationResult limited = new CddlValidator().ValidateCbor(
            schema,
            Unsigned(1),
            limitedOptions);
        ICddlValidator validator = new CddlValidator();
        CddlValidationResult missingSchema = validator.ValidateCbor(
            null!,
            Unsigned(1),
            new CddlValidationOptions());
        CddlValidationResult missingValue = validator.ValidateCbor(
            schema,
            null!,
            new CddlValidationOptions());
        CddlValidationResult missingOptions = validator.ValidateCbor(
            schema,
            Unsigned(1),
            null!);

        Assert.Multiple(() =>
        {
            Assert.That(selected.IsValid, Is.True);
            Assert.That(selected.RuleName, Is.EqualTo("selected"));
            Assert.That(limited.IsValid, Is.False);
            Assert.That(limited.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(missingSchema.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
            Assert.That(missingValue.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.TypeMismatch));
            Assert.That(missingOptions.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
        });
    }

    private static CddlValidationCode[] FlattenCodes(CddlValidationIssue issue)
    {
        List<CddlValidationCode> codes = new List<CddlValidationCode>();
        AddCodes(issue, codes);
        return codes.ToArray();
    }

    private static void AddCodes(
        CddlValidationIssue issue,
        List<CddlValidationCode> codes)
    {
        codes.Add(issue.Code);
        int index = 0;
        while (index < issue.Details.Length)
        {
            AddCodes(issue.Details[index], codes);
            index = index + 1;
        }
    }

    private static void AssertValid(
        string source,
        CborValue value,
        CddlCompileOptions compileOptions)
    {
        CddlValidationResult result = Validate(
            source,
            value,
            compileOptions,
            new CddlValidationOptions());
        Assert.That(result.IsValid, Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertInvalid(
        string source,
        CborValue value,
        CddlCompileOptions compileOptions,
        CddlValidationCode code)
    {
        CddlValidationResult result = Validate(
            source,
            value,
            compileOptions,
            new CddlValidationOptions());
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        CddlCompileOptions compileOptions,
        CddlValidationOptions validationOptions)
    {
        return new CddlValidator().ValidateCbor(
            Compile(source, compileOptions),
            value,
            validationOptions);
    }

    private static CddlSchema Compile(
        string source,
        CddlCompileOptions options)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "match.cddl" });
        Assert.That(parse.IsSuccess, Is.True);
        CddlCompileResult compile = new CddlCompiler().TryCompileSchema(
            parse.Document!,
            options);
        Assert.That(compile.IsSuccess, Is.True,
            compile.Diagnostics.Length == 0
                ? ""
                : compile.Diagnostics[0].Message);
        return compile.Schema!;
    }

    private static CddlCompileOptions NoPrelude()
    {
        return new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
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

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Bytes(byte[] value)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = value
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

    private static CborValue Tag(ulong number, CborValue value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = number,
            TaggedValue = value
        };
    }

    private static CborValue Array()
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = new List<CborValue>()
        };
    }

    private static CborValue Map()
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = new List<CborMapEntry>()
        };
    }
}
