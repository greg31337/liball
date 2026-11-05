using cbor;
using System.Numerics;
using System.Text;

namespace cbor.Tests;

[TestFixture]
public class CddlComputedLiteralControlTests
{
    [Test]
    public void AddsTheCrossProductOfNumericLiteralChoices()
    {
        string source =
            "root = bases .plus offsets\n"
            + "bases = 1 / 3\n"
            + "offsets = 2 / 4";

        AssertValid(source, Unsigned(3));
        AssertValid(source, Unsigned(5));
        AssertValid(source, Unsigned(7));
        AssertInvalid(source, Unsigned(4));
    }

    [Test]
    public void ConvertsMixedNumericSumsToTheTargetType()
    {
        AssertValid("root = 2 .plus -2.2", Negative(-1));
        AssertInvalid("root = 2 .plus -2.2", Unsigned(0));
        AssertValid("root = 1.5 .plus 2", Float(3.5));
        AssertInvalid("root = 1.5 .plus 2", Unsigned(3));

        BigInteger large = BigInteger.Parse("184467440737095516160");
        AssertValid(
            "root = 184467440737095516160 .plus 0.9",
            Integer(large));
    }

    [Test]
    public void UsesComputedNumbersAsGenericMapLabels()
    {
        string source =
            "root = {interval<0>, interval<3>}\n"
            + "interval<BASE> = (\n"
            + "  BASE => int,\n"
            + "  (BASE .plus 1) => int,\n"
            + "  ? (BASE .plus 2) => int\n"
            + ")";
        CborValue required = Map(
            Entry(Unsigned(0), Unsigned(10)),
            Entry(Unsigned(1), Unsigned(11)),
            Entry(Unsigned(3), Unsigned(20)),
            Entry(Unsigned(4), Unsigned(21))
        );
        CborValue withTolerance = Map(
            Entry(Unsigned(0), Unsigned(10)),
            Entry(Unsigned(1), Unsigned(11)),
            Entry(Unsigned(2), Unsigned(12)),
            Entry(Unsigned(3), Unsigned(20)),
            Entry(Unsigned(4), Unsigned(21)),
            Entry(Unsigned(5), Unsigned(22))
        );

        AssertValid(source, required);
        AssertValid(source, withTolerance);
        AssertInvalid(
            source,
            Map(
                Entry(Unsigned(0), Unsigned(10)),
                Entry(Unsigned(2), Unsigned(12)),
                Entry(Unsigned(3), Unsigned(20)),
                Entry(Unsigned(4), Unsigned(21))
            ));
    }

    [Test]
    public void ConcatenatesTextAndByteStringsUsingTheTargetType()
    {
        AssertValid("root = \"foo\" .cat 'bar'", Text("foobar"));
        AssertInvalid("root = \"foo\" .cat 'bar'", Bytes("foobar"));
        AssertValid(
            "root = 'prefix:' .cat \"é\"",
            Bytes("prefix:é"));
        AssertValid(
            "root = (\"a\" / \"b\") .cat (\"1\" / \"2\")",
            Text("b2"));
        AssertInvalid(
            "root = (\"a\" / \"b\") .cat (\"1\" / \"2\")",
            Text("c1"));
    }

    [Test]
    public void RejectsAConcatenatedTextResultThatIsNotUtf8()
    {
        CddlValidationResult result = Validate(
            "root = \"ok\" .cat h'ff'",
            Text("ok"),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
        });
    }

    [Test]
    public void DedentsBothOperandsIndependentlyBeforeConcatenating()
    {
        string source =
            "root = \"  a\\n    b\\n\" .det "
            + "\"    c\\n      d\\n\"";
        AssertValid(source, Text("a\n  b\nc\n  d\n"));
        AssertInvalid(source, Text("  a\n    b\n    c\n      d\n"));

        AssertValid(
            "root = \"\" .det \"  x\\n    y\"",
            Text("x\n  y"));
        AssertValid(
            "root = \"head\\n\" .cat (\"\" .det \"  body\\n\")",
            Text("head\nbody\n"));
        AssertValid(
            "root = \"  a\\n \\n    b\" .det \"\"",
            Text("a\n\n  b"));
    }

    [Test]
    public void DedentsByteStringsWithoutChangingTheirResultType()
    {
        AssertValid(
            "root = '  a\\n    b' .det ''",
            Bytes("a\n  b"));
        AssertInvalid(
            "root = '  a\\n    b' .det ''",
            Text("a\n  b"));
    }

    [Test]
    public void UsesComputedLiteralsInsideRangesAndOtherControls()
    {
        AssertValid("root = 0..(1 .plus 2)", Unsigned(3));
        AssertInvalid("root = 0..(1 .plus 2)", Unsigned(4));
        AssertValid("root = uint .lt (1 .plus 2)", Unsigned(2));
        AssertInvalid("root = uint .lt (1 .plus 2)", Unsigned(3));
        AssertValid("root = bstr .size (1 .plus 2)", Bytes("abc"));
        AssertValid(
            "root = tstr .regexp (\"[a-\" .cat \"z]+\")",
            Text("cddl"));
        AssertInvalid(
            "root = tstr .regexp (\"[a-\" .cat \"z]+\")",
            Text("123"));
        AssertValid("root = 3 .eq (1 .plus 2)", Unsigned(3));
    }

    [TestCase("root = \"a\" .plus 1",
        "CDDL_COMPILE_CONTROL_TARGET_INVALID")]
    [TestCase("root = 1 .plus \"a\"",
        "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID")]
    [TestCase("root = 1 .cat \"a\"",
        "CDDL_COMPILE_CONTROL_TARGET_INVALID")]
    [TestCase("root = \"a\" .det 1",
        "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID")]
    public void RejectsIncompatibleComputedLiteralOperands(
        string source,
        string expectedCode)
    {
        CddlCompileResult result = Compile(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain(expectedCode));
        });
    }

    [Test]
    public void ReportsComputedMismatchesAndAlternativeLimits()
    {
        CddlValidationResult mismatch = Validate(
            "root = 1 .plus 2",
            Unsigned(4),
            new CddlValidationOptions());
        CddlValidationResult limited = Validate(
            "root = (1 / 2 / 3) .plus (4 / 5)",
            Unsigned(5),
            new CddlValidationOptions { MaxAlternatives = 4 });

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(mismatch.Issues[0].SchemaPath,
                Does.Contain("/control(.plus)"));
            Assert.That(mismatch.Issues[0].Expected,
                Does.Contain(".plus"));
            Assert.That(limited.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    private static CddlCompileResult Compile(string source)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions
            {
                SourceName = "computed-literals.cddl"
            });
        Assert.That(
            parse.Diagnostics,
            Is.Empty,
            parse.Diagnostics.Length == 0
                ? ""
                : parse.Diagnostics[0].Message);
        return new CddlCompiler().TryCompileSchema(
            parse.Document!,
            new CddlCompileOptions());
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        CddlValidationOptions options)
    {
        CddlCompileResult compile = Compile(source);
        Assert.That(
            compile.IsSuccess,
            Is.True,
            compile.Diagnostics.Length == 0
                ? ""
                : compile.Diagnostics[0].Message);
        return new CddlValidator().ValidateCbor(
            compile.Schema!,
            value,
            options);
    }

    private static void AssertValid(string source, CborValue value)
    {
        CddlValidationResult result = Validate(
            source,
            value,
            new CddlValidationOptions());
        Assert.That(
            result.IsValid,
            Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
    }

    private static void AssertInvalid(string source, CborValue value)
    {
        CddlValidationResult result = Validate(
            source,
            value,
            new CddlValidationOptions());
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Is.Not.Empty);
    }

    private static CborValue Unsigned(int value)
    {
        return Integer(new BigInteger(value));
    }

    private static CborValue Negative(int value)
    {
        return Integer(new BigInteger(value));
    }

    private static CborValue Integer(BigInteger value)
    {
        return new CborValue
        {
            Kind = value.Sign < 0
                ? CborValueKind.NegativeInteger
                : CborValueKind.UnsignedInteger,
            IntegerValue = value
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

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Bytes(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = Encoding.UTF8.GetBytes(value)
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
