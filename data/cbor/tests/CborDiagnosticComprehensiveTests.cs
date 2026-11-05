using System.Text;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborDiagnosticComprehensiveTests
{
    [TestCase("b32''", "")]
    [TestCase("b32'MY'", "f")]
    [TestCase("b32'MZXQ'", "fo")]
    [TestCase("b32'MZXW6'", "foo")]
    [TestCase("b32'MZXW6YQ'", "foob")]
    [TestCase("b32'MZXW6YTB'", "fooba")]
    [TestCase("b32'MZXW6YTBOI'", "foobar")]
    [TestCase("h32''", "")]
    [TestCase("h32'CO'", "f")]
    [TestCase("h32'CPNG'", "fo")]
    [TestCase("h32'CPNMU'", "foo")]
    [TestCase("h32'CPNMUOG'", "foob")]
    [TestCase("h32'CPNMUOJ1'", "fooba")]
    [TestCase("h32'CPNMUOJ1E8'", "foobar")]
    [TestCase("b64''", "")]
    [TestCase("b64'Zg'", "f")]
    [TestCase("b64'Zm8'", "fo")]
    [TestCase("b64'Zm9v'", "foo")]
    [TestCase("b64'Zm9vYg'", "foob")]
    [TestCase("b64'Zm9vYmE'", "fooba")]
    [TestCase("b64'Zm9vYmFy'", "foobar")]
    public void ParsesEveryUnpaddedBaseEncodingLength(
        string notation,
        string expectedText)
    {
        CborValue value = ParseDone(notation);

        Assert.That(value.ByteStringValue,
            Is.EqualTo(Encoding.ASCII.GetBytes(expectedText)));
    }

    [Test]
    public void RfcByteStringSpellingsAndEdnTriviaAreEquivalent()
    {
        string[] notations =
        [
            "h'68656c6c6f20776f726c64'",
            "h'68 65 6c 6c 6f 20 77 6f 72 6c 64'",
            "h'6 86 56c 6c6f\n20776 f726c64'",
            "h'68 65 6c /doubled l!/ 6c 6f /hello/ 20 /space/ 77 6f 72 6c 64'",
            "'hello world'"
        ];
        byte[] expected = Encoding.UTF8.GetBytes("hello world");

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < notations.Length)
            {
                Assert.That(ParseDone(notations[index], true).ByteStringValue,
                    Is.EqualTo(expected), notations[index]);
                index++;
            }
        });
    }

    [TestCase("h'0'", "CBOR_DIAGNOSTIC_HEX_LENGTH_INVALID")]
    [TestCase("h'gg'", "CBOR_DIAGNOSTIC_HEX_INVALID")]
    [TestCase("b32'A'", "CBOR_DIAGNOSTIC_BASE32_LENGTH_INVALID")]
    [TestCase("b32'M1'", "CBOR_DIAGNOSTIC_BASE32_INVALID")]
    [TestCase("b32'MZ'", "CBOR_DIAGNOSTIC_BASE32_PADDING_INVALID")]
    [TestCase("h32'W0'", "CBOR_DIAGNOSTIC_BASE32_INVALID")]
    [TestCase("b64'A'", "CBOR_DIAGNOSTIC_BASE64_LENGTH_INVALID")]
    [TestCase("b64'A*'", "CBOR_DIAGNOSTIC_BASE64_INVALID")]
    [TestCase("b64'+_'", "CBOR_DIAGNOSTIC_BASE64_INVALID")]
    [TestCase("b64'Zh'", "CBOR_DIAGNOSTIC_BASE64_PADDING_INVALID")]
    [TestCase("b64'Zg='", "CBOR_DIAGNOSTIC_BASE64_INVALID")]
    [TestCase("h'00", "CBOR_DIAGNOSTIC_BYTE_STRING_UNTERMINATED")]
    public void InvalidByteStringLexemesReturnSpecificDiagnostics(
        string notation,
        string expectedCode)
    {
        CborReadResult result = Parse(notation, true);

        AssertError(result, expectedCode, "$" );
    }

    [Test]
    public void ParsesEverySimpleValueBoundaryAndRejectsReservedValues()
    {
        CborValue values = ParseDone(
            "[simple(0),simple(19),false,true,null,undefined,simple(32),simple(255)]"
        );
        byte[] expected = [0, 19, 20, 21, 22, 23, 32, 255];
        string[] invalid =
        [
            "simple(-1)",
            "simple(1.0)",
            "simple(24)",
            "simple(31)",
            "simple(256)"
        ];

        Assert.Multiple(() =>
        {
            Assert.That(values.ArrayItems!.Select(item => item.SimpleValue),
                Is.EqualTo(expected));
            int index = 0;
            while (index < invalid.Length)
            {
                AssertError(
                    Parse(invalid[index]),
                    "CBOR_DIAGNOSTIC_SIMPLE_VALUE_INVALID",
                    "$"
                );
                index++;
            }
        });
    }

    [TestCase("+1", "CBOR_DIAGNOSTIC_NUMBER_INVALID")]
    [TestCase("00", "CBOR_DIAGNOSTIC_NUMBER_INVALID")]
    [TestCase("1.", "CBOR_DIAGNOSTIC_NUMBER_INVALID")]
    [TestCase("1e", "CBOR_DIAGNOSTIC_NUMBER_INVALID")]
    [TestCase("1e+", "CBOR_DIAGNOSTIC_NUMBER_INVALID")]
    [TestCase("0x", "CBOR_DIAGNOSTIC_NUMBER_INVALID")]
    [TestCase("0o9", "CBOR_DIAGNOSTIC_NUMBER_INVALID")]
    [TestCase("0b2", "CBOR_DIAGNOSTIC_NUMBER_INVALID")]
    [TestCase("0x.p1", "CBOR_DIAGNOSTIC_HEX_FLOAT_INVALID")]
    [TestCase("0x1.p0", "CBOR_DIAGNOSTIC_HEX_FLOAT_INVALID")]
    [TestCase("0x1p", "CBOR_DIAGNOSTIC_HEX_FLOAT_INVALID")]
    [TestCase("1e999999", "CBOR_DIAGNOSTIC_NUMBER_OUT_OF_RANGE")]
    public void InvalidNumericLexemesReturnSpecificDiagnostics(
        string notation,
        string expectedCode)
    {
        CborReadResult result = Parse(notation, true);

        AssertError(result, expectedCode, "$" );
    }

    [TestCase("\"line\nbreak\"", "CBOR_DIAGNOSTIC_STRING_CONTROL")]
    [TestCase("\"\\", "CBOR_DIAGNOSTIC_ESCAPE_INCOMPLETE")]
    [TestCase("\"\\q\"", "CBOR_DIAGNOSTIC_ESCAPE_INVALID")]
    [TestCase("\"\\u12\"", "CBOR_DIAGNOSTIC_UNICODE_ESCAPE_INCOMPLETE")]
    [TestCase("\"\\u12xz\"", "CBOR_DIAGNOSTIC_UNICODE_ESCAPE_INVALID")]
    [TestCase("\"\\ud800\"", "CBOR_DIAGNOSTIC_STRING_SURROGATE")]
    [TestCase("\"\\udc00\"", "CBOR_DIAGNOSTIC_STRING_SURROGATE")]
    public void InvalidStringAndEscapeLexemesReturnSpecificDiagnostics(
        string notation,
        string expectedCode)
    {
        CborReadResult result = Parse(notation);

        AssertError(result, expectedCode, "$" );
    }

    [Test]
    public void EveryDefinedEncodingIndicatorWidthRoundTrips()
    {
        const string notation =
            "[_0 23_0,24_0,256_1,65536_2,4294967296_3," +
            "-24_0,-25_0,h'00'_0,\"x\"_0,24(null)_0," +
            "1.5_1,1000000.5_2,1.1_3]";
        CborDiagnosticOptions options = new CborDiagnosticOptions
        {
            UseExtendedNotation = true,
            IncludeRepresentation = true
        };
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborReadResult parsed = converter.ParseDiagnostic(notation, options);

        string formatted = converter.FormatDiagnostic(parsed.Value!, options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(formatted, Is.EqualTo(notation));
            Assert.That(parsed.Value!.Representation!.HeadArgumentWidth,
                Is.EqualTo(1));
            Assert.That(parsed.Value.ArrayItems![11].FloatWidth,
                Is.EqualTo(CborFloatWidth.Single));
            Assert.That(parsed.Value.ArrayItems![12].FloatWidth,
                Is.EqualTo(CborFloatWidth.Double));
        });
    }

    [Test]
    public void UnknownIndicatorsAreIgnoredAtScalarStringAndContainerPositions()
    {
        CborValue value = ParseDone(
            "[_vendor [1]_array,\"x\"_text,h'01'_bytes,7_number]"
        );

        Assert.Multiple(() =>
        {
            Assert.That(value.Representation, Is.Null);
            Assert.That(value.ArrayItems![0].Representation, Is.Null);
            Assert.That(value.ArrayItems[1].Representation, Is.Null);
            Assert.That(value.ArrayItems[2].Representation, Is.Null);
            Assert.That(value.ArrayItems[3].Representation, Is.Null);
        });
    }

    [Test]
    public void ExtendedSequenceTriviaAndEveryValueKindRoundTripSemantically()
    {
        const string notation =
            "/first/ 0x18, /second/ -0o31, /third/ 0x1.8p0," +
            "\"hello \" h'e29883', h'01' 'x', [true,null]," +
            "{h'00':simple(42)}, 100(\"tag\"), <<1,2>>";
        CborDiagnosticOptions extended = new CborDiagnosticOptions
        {
            UseExtendedNotation = true
        };
        CborDiagnosticConverter converter = new CborDiagnosticConverter();

        List<CborReadResult> first = converter.ParseDiagnosticSequence(
            notation,
            extended);
        List<CborValue> values = first.Select(result => result.Value!).ToList();
        string formatted = converter.FormatDiagnosticSequence(values, extended);
        List<CborReadResult> second = converter.ParseDiagnosticSequence(
            formatted,
            extended);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Count.EqualTo(9));
            Assert.That(first, Has.All.Matches<CborReadResult>(result =>
                result.Status == CborReadStatus.Done));
            Assert.That(second, Has.Count.EqualTo(first.Count));
            Assert.That(second, Has.All.Matches<CborReadResult>(result =>
                result.Status == CborReadStatus.Done));
            Assert.That(formatted,
                Is.EqualTo("24,-25,1.5,\"hello ☃\",h'0178'," +
                    "[true,null],{h'00':simple(42)},100(\"tag\"),h'0102'"));
            Assert.That(second[8].Value!.ByteStringValue,
                Is.EqualTo(new byte[] { 1, 2 }));
        });
    }

    [Test]
    public void StructuralFailuresRetainNestedPaths()
    {
        CborReadResult array = Parse("[0,1 2]");
        CborReadResult mapColon = Parse("{0 1}");
        CborReadResult mapValue = Parse("{0:}");
        CborReadResult tag = Parse("1([0,01])");
        CborReadResult chunks = Parse("(_ h'00',\"x\")");

        Assert.Multiple(() =>
        {
            AssertError(array,
                "CBOR_DIAGNOSTIC_ARRAY_DELIMITER_EXPECTED", "$" );
            AssertError(mapColon,
                "CBOR_DIAGNOSTIC_MAP_COLON_EXPECTED", "$[0].key" );
            AssertError(mapValue,
                "CBOR_DIAGNOSTIC_TOKEN_INVALID", "$[0].value" );
            AssertError(tag,
                "CBOR_DIAGNOSTIC_NUMBER_INVALID", "$<tag:1>[1]" );
            AssertError(chunks,
                "CBOR_DIAGNOSTIC_CHUNK_TYPE_MISMATCH", "$" );
        });
    }

    private static CborReadResult Parse(string notation, bool extended = false)
    {
        return new CborDiagnosticConverter().ParseDiagnostic(
            notation,
            new CborDiagnosticOptions { UseExtendedNotation = extended });
    }

    private static CborValue ParseDone(string notation, bool extended = false)
    {
        CborReadResult result = Parse(notation, extended);
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done),
            $"{result.ErrorCode} at {result.ErrorOffset} ({result.ErrorPath}): {result.ErrorMessage}");
        return result.Value!;
    }

    private static void AssertError(
        CborReadResult result,
        string expectedCode,
        string expectedPath)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(result.ErrorPath, Is.EqualTo(expectedPath));
            Assert.That(result.ErrorOffset, Is.GreaterThanOrEqualTo(0));
        });
    }
}
