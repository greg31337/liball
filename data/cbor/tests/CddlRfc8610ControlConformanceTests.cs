using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlRfc8610ControlConformanceTests
{
    [Test]
    public void MatchesRfcSizeAndBitsExamplesAtTheirBoundaries()
    {
        AssertValid("audio-sample = uint .size 3", Unsigned(0));
        AssertValid("audio-sample = uint .size 3", Unsigned(16777215));
        AssertInvalid("audio-sample = uint .size 3", Unsigned(16777216));

        string flags =
            "tcpflagbytes = bstr .bits flags\n"
            + "flags = &(fin: 8, syn: 9, rst: 10, psh: 11, ack: 12, "
            + "urg: 13, ece: 14, cwr: 15, ns: 0) / (4..7)";
        AssertValid(flags, Bytes());
        AssertValid(flags, Bytes(0x90, 0x6d));
        AssertInvalid(flags, Bytes(0x02, 0x00));
    }

    [Test]
    public void MatchesTheRfcXsdRegularExpressionExampleAndDifferences()
    {
        string nai = "nai = tstr .regexp "
            + "\"[A-Za-z0-9]+@[A-Za-z0-9]+(\\\\.[A-Za-z0-9]+)+\"";
        AssertValid(nai, Text("N1@CH57HF.4Znqe0.dYJRN.igjf"));
        AssertInvalid(nai, Text("N1@CH57HF"));

        AssertValid("root = tstr .regexp \"^a$\"", Text("^a$"));
        AssertInvalid("root = tstr .regexp \"^a$\"", Text("a"));
        AssertValid(
            "root = tstr .regexp \"[a-z-[aeiou]]+\"",
            Text("cddl"));
        AssertInvalid(
            "root = tstr .regexp \"[a-z-[aeiou]]+\"",
            Text("code"));
        AssertValid("root = tstr .regexp \"\\\\d+\"", Text("١٢٣"));
    }

    [Test]
    public void AppliesEmbeddedCborAndSequenceControlsToRfcModels()
    {
        string item =
            "root = bytes .cbor item\n"
            + "item = {id: uint, name: tstr}";
        AssertValid(
            item,
            Bytes(
                0xa2, 0x62, 0x69, 0x64, 0x01,
                0x64, 0x6e, 0x61, 0x6d, 0x65, 0x62, 0x6f, 0x6b));
        AssertInvalid(item, Bytes());
        AssertInvalid(item, Bytes(0x01));

        string sequence = "root = bytes .cborseq [uint, tstr]";
        AssertValid(sequence, Bytes(0x01, 0x62, 0x6f, 0x6b));
        AssertInvalid(sequence, Bytes(0x01));
        AssertInvalid(sequence, Bytes(0x01, 0x62, 0x6f, 0x6b, 0xf6));
    }

    [Test]
    public void MatchesTheRfcWithinSocketExample()
    {
        string source =
            "message = $message .within message-structure\n"
            + "message-structure = [message-type, * message-option]\n"
            + "message-type = 0..255\n"
            + "message-option = any\n"
            + "$message /= [3, dough: text, topping: [* text]]\n"
            + "$message /= [4, noodles: text, sauce: text, parmesan: bool]";

        AssertValid(
            source,
            Array(Unsigned(3), Text("wheat"), Array(Text("cheese"))));
        AssertValid(
            source,
            Array(Unsigned(4), Text("udon"), Text("soy"), Simple(21)));
        AssertInvalid(
            source,
            Array(Unsigned(3), Text("wheat"), Array(Unsigned(1))));
        AssertInvalid(source, Array(Unsigned(256)));
    }

    [Test]
    public void MatchesRfcRelationalAndDefaultExamples()
    {
        AssertValid("speed = number .ge 0", Float(0.5));
        AssertValid("speed = number .ge 0", Unsigned(0));
        AssertInvalid("speed = number .ge 0", Negative(-1));

        string timer =
            "timer = {time: uint, "
            + "? displayed-step: (number .gt 0) .default 1}";
        AssertValid(
            timer,
            Map(Entry(Text("time"), Unsigned(10))));
        AssertValid(
            timer,
            Map(
                Entry(Text("time"), Unsigned(10)),
                Entry(Text("displayed-step"), Unsigned(2))));
        AssertInvalid(
            timer,
            Map(
                Entry(Text("time"), Unsigned(10)),
                Entry(Text("displayed-step"), Unsigned(1))));
        AssertInvalid(
            timer,
            Map(
                Entry(Text("time"), Unsigned(10)),
                Entry(Text("displayed-step"), Unsigned(0))));
    }

    [TestCase("root = bool .size 1",
        "CDDL_COMPILE_CONTROL_TARGET_INVALID")]
    [TestCase("root = tstr .size tstr",
        "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID")]
    [TestCase("root = tstr .bits 1",
        "CDDL_COMPILE_CONTROL_TARGET_INVALID")]
    [TestCase("root = uint .bits tstr",
        "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID")]
    [TestCase("root = bstr .regexp \"x\"",
        "CDDL_COMPILE_CONTROL_TARGET_INVALID")]
    [TestCase("root = tstr .regexp (\"x\" / \"y\")",
        "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID")]
    [TestCase("root = tstr .cbor any",
        "CDDL_COMPILE_CONTROL_TARGET_INVALID")]
    [TestCase("root = tstr .cborseq any",
        "CDDL_COMPILE_CONTROL_TARGET_INVALID")]
    [TestCase("root = tstr .lt 1",
        "CDDL_COMPILE_CONTROL_TARGET_INVALID")]
    [TestCase("root = uint .ge tstr",
        "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID")]
    [TestCase("root = uint .eq (1 / 2)",
        "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID")]
    [TestCase("root = uint .default 0",
        "CDDL_COMPILE_DEFAULT_CONTEXT_INVALID")]
    public void RejectsInvalidControlOperands(
        string source,
        string expectedCode)
    {
        CddlCompileResult result = Compile(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Schema, Is.Null);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Does.Contain(expectedCode));
        });
    }

    [Test]
    public void ReportsStructuredControlDiagnosticsAndResourceLimits()
    {
        CddlValidationResult mismatch = Validate(
            "root = [uint, tstr .regexp \"[a-z]+\"]",
            Array(Unsigned(1), Text("BAD")),
            new CddlValidationOptions());
        CddlValidationIssue issue = FindIssue(
            mismatch.Issues[0],
            CddlValidationCode.ControlMismatch);
        CddlValidationResult regexLimit = Validate(
            "root = tstr .regexp \"abcdef\"",
            Text("abcdef"),
            new CddlValidationOptions { MaxPatternStates = 2 });
        CddlValidationResult embeddedLimit = Validate(
            "root = bstr .cbor [uint, uint]",
            Bytes(0x82, 0x01, 0x02),
            new CddlValidationOptions { MaxEmbeddedBytes = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(issue.InstancePath, Is.EqualTo("$[1]"));
            Assert.That(issue.SchemaPath, Does.Contain("/control(.regexp)"));
            Assert.That(issue.Expected, Does.Contain("[a-z]+"));
            Assert.That(issue.Actual, Does.Contain("BAD"));
            Assert.That(regexLimit.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
            Assert.That(embeddedLimit.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ResourceLimit));
        });
    }

    private static CddlValidationIssue FindIssue(
        CddlValidationIssue issue,
        CddlValidationCode code)
    {
        if (issue.Code == code)
        {
            return issue;
        }
        int index = 0;
        while (index < issue.Details.Length)
        {
            CddlValidationIssue found = FindIssue(issue.Details[index], code);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        return null!;
    }

    private static CddlCompileResult Compile(string source)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "rfc8610-controls.cddl" });
        Assert.That(parse.Diagnostics, Is.Empty);
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
