using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborDiagnosticParserTests
{
    [Test]
    public void ParsesBaseScalarsAndPreservesNumericSemantics()
    {
        CborValue value = ParseDone(
            "[18446744073709551616,-18446744073709551617,1.5,-0.0," +
            "Infinity,-Infinity,NaN,false,true,null,undefined,simple(42)]"
        );
        List<CborValue> items = value.ArrayItems!;

        Assert.Multiple(() =>
        {
            Assert.That(items[0].Kind, Is.EqualTo(CborValueKind.UnsignedInteger));
            Assert.That(items[0].IntegerValue,
                Is.EqualTo(BigInteger.Parse("18446744073709551616")));
            Assert.That(items[1].Kind, Is.EqualTo(CborValueKind.NegativeInteger));
            Assert.That(items[1].IntegerValue,
                Is.EqualTo(BigInteger.Parse("-18446744073709551617")));
            Assert.That(items[2].FloatValue!.Value, Is.EqualTo(1.5));
            Assert.That(items[2].FloatValue!.Width, Is.EqualTo(CborFloatWidth.Double));
            Assert.That(items[2].FloatValue!.RawBits,
                Is.EqualTo(BitConverter.DoubleToUInt64Bits(1.5)));
            Assert.That(items[3].FloatValue!.RawBits,
                Is.EqualTo(BitConverter.DoubleToUInt64Bits(-0.0)));
            Assert.That(items[4].FloatValue!.Value, Is.EqualTo(double.PositiveInfinity));
            Assert.That(items[5].FloatValue!.Value, Is.EqualTo(double.NegativeInfinity));
            Assert.That(items[6].FloatValue!.IsNaN, Is.True);
            Assert.That(double.IsNaN(items[6].FloatValue!.Value), Is.True);
            Assert.That(items[7].SimpleValue, Is.EqualTo(CborSimpleValues.False));
            Assert.That(items[8].SimpleValue, Is.EqualTo(CborSimpleValues.True));
            Assert.That(items[9].SimpleValue, Is.EqualTo(CborSimpleValues.Null));
            Assert.That(items[10].SimpleValue, Is.EqualTo(CborSimpleValues.Undefined));
            Assert.That(items[11].SimpleValue, Is.EqualTo((byte)42));
        });
    }

    [Test]
    public void ParsesNestedContainersTagsAndArbitraryMapKeys()
    {
        CborValue map = ParseDone(
            "{[1,\"key\"]:100(simple(42)),h'01':[true,null]}"
        );
        List<CborMapEntry> entries = map.MapEntries!;

        Assert.Multiple(() =>
        {
            Assert.That(map.Kind, Is.EqualTo(CborValueKind.Map));
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries[0].Key!.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(entries[0].Key!.ArrayItems![1].TextStringValue, Is.EqualTo("key"));
            Assert.That(entries[0].Value!.Kind, Is.EqualTo(CborValueKind.Tag));
            Assert.That(entries[0].Value!.TagNumber, Is.EqualTo(100));
            Assert.That(entries[0].Value!.TaggedValue!.SimpleValue, Is.EqualTo((byte)42));
            Assert.That(entries[1].Key!.ByteStringValue, Is.EqualTo(new byte[] { 1 }));
            Assert.That(entries[1].Value!.ArrayItems![0].SimpleValue,
                Is.EqualTo(CborSimpleValues.True));
        });
    }

    [Test]
    public void ParsesEscapedTextAndEveryBaseByteStringEncoding()
    {
        CborValue value = ParseDone(
            "[\"a\\\"b\\\\c\\/d\\b\\f\\n\\r\\t\\u263a\\ud83d\\ude00\"," +
            "h'12345678',b32'CI2FM6A',h32'28Q5CU0',b64'EjRWeA']"
        );
        List<CborValue> items = value.ArrayItems!;
        byte[] expected = [0x12, 0x34, 0x56, 0x78];

        Assert.Multiple(() =>
        {
            Assert.That(items[0].TextStringValue,
                Is.EqualTo("a\"b\\c/d\b\f\n\r\t☺😀"));
            Assert.That(items[1].ByteStringValue, Is.EqualTo(expected));
            Assert.That(items[2].ByteStringValue, Is.EqualTo(expected));
            Assert.That(items[3].ByteStringValue, Is.EqualTo(expected));
            Assert.That(items[4].ByteStringValue, Is.EqualTo(expected));
        });
    }

    [Test]
    public void IgnoresEncodingIndicatorsAndCombinesIndefiniteStringChunks()
    {
        CborValue value = ParseDone(
            "[_ 1_0,1.5_1,h'00'_0,{_ \"x\":2_3}," +
            "(_ \"Hello \"_0,\"world\"_0),(_ h'4869'_0,h'21'_0),''_,\"\"_]"
        );
        List<CborValue> items = value.ArrayItems!;

        Assert.Multiple(() =>
        {
            Assert.That(items[0].IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(items[1].FloatValue!.Value, Is.EqualTo(1.5));
            Assert.That(items[2].ByteStringValue, Is.EqualTo(new byte[] { 0 }));
            Assert.That(items[3].MapEntries![0].Value!.IntegerValue,
                Is.EqualTo(new BigInteger(2)));
            Assert.That(items[4].TextStringValue, Is.EqualTo("Hello world"));
            Assert.That(items[5].ByteStringValue,
                Is.EqualTo(new byte[] { 0x48, 0x69, 0x21 }));
            Assert.That(items[6].ByteStringValue, Is.Empty);
            Assert.That(items[7].TextStringValue, Is.Empty);
        });
    }

    [Test]
    public void ParsesExtendedNumericBasesAndHexadecimalFloats()
    {
        CborValue value = ParseDone(
            "[4711,0x1267,0o11147,0b1001001100111,-0x1267," +
            "1.5,0x1.8p0,0x18p-4,-0x1.8p+1]",
            true
        );
        List<CborValue> items = value.ArrayItems!;

        Assert.Multiple(() =>
        {
            Assert.That(items.Take(4).Select(item => item.IntegerValue),
                Is.All.EqualTo(new BigInteger(4711)));
            Assert.That(items[4].IntegerValue, Is.EqualTo(new BigInteger(-4711)));
            Assert.That(items.Skip(5).Take(3).Select(item => item.FloatValue!.Value),
                Is.All.EqualTo(1.5));
            Assert.That(items[8].FloatValue!.Value, Is.EqualTo(-3.0));
        });
    }

    [Test]
    public void ExtendedNotationSupportsCommentsWhitespaceAndStringConcatenation()
    {
        CborValue value = ParseDone(
            "/root/ [/text/ \"Hello \" \"world\", " +
            "\"Hello\" h'20' \"world\", /bytes/ 'Hello ' h'776f726c64'," +
            "h'4 86 56c 6c6f' /join/ h' 20776 f726c64'," +
            "h'68 65 6c /doubled l!/ 6c 6f /space/ 20 77 6f 72 6c 64'," +
            "b64'/w'] /end/",
            true
        );
        List<CborValue> items = value.ArrayItems!;
        byte[] helloWorld = System.Text.Encoding.UTF8.GetBytes("Hello world");
        byte[] lowerHelloWorld = System.Text.Encoding.UTF8.GetBytes("hello world");

        Assert.Multiple(() =>
        {
            Assert.That(items[0].TextStringValue, Is.EqualTo("Hello world"));
            Assert.That(items[1].TextStringValue, Is.EqualTo("Hello world"));
            Assert.That(items[2].ByteStringValue, Is.EqualTo(helloWorld));
            Assert.That(items[3].ByteStringValue, Is.EqualTo(helloWorld));
            Assert.That(items[4].ByteStringValue, Is.EqualTo(lowerHelloWorld));
            Assert.That(items[5].ByteStringValue, Is.EqualTo(new byte[] { 0xff }));
        });
    }

    [Test]
    public void ExtendedTextConcatenationValidatesEncodedUtf8Segments()
    {
        CborValue value = ParseDone("\"snow \" h'e29883' \"!\"", true);
        CborReadResult invalid = Parse("\"x\" h'ff' \"y\"", true);

        Assert.Multiple(() =>
        {
            Assert.That(value.TextStringValue, Is.EqualTo("snow ☃!"));
            Assert.That(invalid.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(invalid.ErrorCode, Is.EqualTo("CBOR_UTF8_OUT_OF_RANGE"));
            Assert.That(invalid.ErrorPath, Is.EqualTo("$"));
        });
    }

    [Test]
    public void ExtendedFeaturesAreRejectedWhenExtendedNotationIsDisabled()
    {
        CborReadResult number = Parse("0x1267");
        CborReadResult comment = Parse("/comment/ 1");
        CborReadResult textBytes = Parse("'hello'");
        CborReadResult emptyTextBytes = Parse("''");
        CborReadResult concatenated = Parse("\"a\" \"b\"");
        CborReadResult spacedBytes = Parse("h'00 01'");

        Assert.Multiple(() =>
        {
            Assert.That(number.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_EXTENDED_NOTATION_DISABLED"));
            Assert.That(comment.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(textBytes.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_EXTENDED_NOTATION_DISABLED"));
            Assert.That(emptyTextBytes.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_EXTENDED_NOTATION_DISABLED"));
            Assert.That(concatenated.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_TRAILING_DATA"));
            Assert.That(spacedBytes.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_BYTE_STRING_WHITESPACE"));
        });
    }

    [Test]
    public void InvalidLexemesAndStructuresReturnStableDiagnostics()
    {
        CborReadResult nestedNumber = Parse("[1,01]");
        CborReadResult escape = Parse("\"\\q\"");
        CborReadResult oddHex = Parse("h'0'");
        CborReadResult map = Parse("{\"x\" 1}");
        CborReadResult array = Parse("[1 2]");
        CborReadResult comment = Parse("/never ends", true);
        CborReadResult chunks = Parse("(_ \"a\",h'62')");

        Assert.Multiple(() =>
        {
            Assert.That(nestedNumber.ErrorCode, Is.EqualTo("CBOR_DIAGNOSTIC_NUMBER_INVALID"));
            Assert.That(nestedNumber.ErrorOffset, Is.EqualTo(3));
            Assert.That(nestedNumber.ErrorPath, Is.EqualTo("$[1]"));
            Assert.That(escape.ErrorCode, Is.EqualTo("CBOR_DIAGNOSTIC_ESCAPE_INVALID"));
            Assert.That(escape.ErrorOffset, Is.EqualTo(2));
            Assert.That(oddHex.ErrorCode, Is.EqualTo("CBOR_DIAGNOSTIC_HEX_LENGTH_INVALID"));
            Assert.That(map.ErrorCode, Is.EqualTo("CBOR_DIAGNOSTIC_MAP_COLON_EXPECTED"));
            Assert.That(map.ErrorPath, Is.EqualTo("$[0].key"));
            Assert.That(array.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_ARRAY_DELIMITER_EXPECTED"));
            Assert.That(comment.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_COMMENT_UNTERMINATED"));
            Assert.That(chunks.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_CHUNK_TYPE_MISMATCH"));
        });
    }

    [Test]
    public void RejectsInvalidNumberAndEncodedByteStringForms()
    {
        CborReadResult leadingPlus = Parse("+1", true);
        CborReadResult badBinary = Parse("0b102", true);
        CborReadResult badHexFloat = Parse("0x1.p0", true);
        CborReadResult badBase32 = Parse("b32'A'");
        CborReadResult badBase64 = Parse("b64'A'");
        CborReadResult mixedBase64 = Parse("b64'+_'");
        CborReadResult reservedSimple = Parse("simple(24)");
        CborReadResult emptyChunks = Parse("(_ )");

        Assert.Multiple(() =>
        {
            Assert.That(leadingPlus.ErrorCode, Is.EqualTo("CBOR_DIAGNOSTIC_NUMBER_INVALID"));
            Assert.That(badBinary.ErrorCode, Is.EqualTo("CBOR_DIAGNOSTIC_NUMBER_INVALID"));
            Assert.That(badHexFloat.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_HEX_FLOAT_INVALID"));
            Assert.That(badBase32.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_BASE32_LENGTH_INVALID"));
            Assert.That(badBase64.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_BASE64_LENGTH_INVALID"));
            Assert.That(mixedBase64.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_BASE64_INVALID"));
            Assert.That(reservedSimple.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_SIMPLE_VALUE_INVALID"));
            Assert.That(emptyChunks.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_CHUNK_TYPE_AMBIGUOUS"));
        });
    }

    [Test]
    public void EnforcesInputAndNestingLimitsAndValidatesRequiredInputs()
    {
        CborReadResult characters = Parse(
            "123",
            false,
            new CborDiagnosticOptions { MaxCharacters = 2 }
        );
        CborReadResult depth = Parse(
            "[0]",
            false,
            new CborDiagnosticOptions { MaxNestingDepth = 0 }
        );
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborReadResult missingText = converter.ParseDiagnostic(
            null!,
            new CborDiagnosticOptions()
        );
        CborReadResult missingOptions = converter.ParseDiagnostic("0", null!);

        Assert.Multiple(() =>
        {
            Assert.That(characters.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_MAX_CHARACTERS"));
            Assert.That(characters.ErrorOffset, Is.EqualTo(2));
            Assert.That(depth.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_MAX_NESTING_DEPTH"));
            Assert.That(depth.ErrorPath, Is.EqualTo("$[0]"));
            Assert.That(missingText.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_TEXT_MISSING"));
            Assert.That(missingOptions.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_OPTIONS_MISSING"));
        });
    }

    [Test]
    public void FormatterOutputParsesAndFormatsToTheSameNotation()
    {
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticOptions options = new CborDiagnosticOptions();
        string notation = "[0,-1,1.5,\"text\",h'00ff',42([true,null]),simple(42)]";
        CborReadResult parsed = converter.ParseDiagnostic(notation, options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(parsed.Consumed, Is.EqualTo(notation.Length));
            Assert.That(parsed.Remaining, Is.Zero);
            Assert.That(converter.FormatDiagnostic(parsed.Value!, options), Is.EqualTo(notation));
        });
    }

    [Test]
    public void EmbeddedNotationRequiresExtendedMode()
    {
        CborReadResult result = Parse("<<1>>");

        Assert.That(result.ErrorCode,
            Is.EqualTo("CBOR_DIAGNOSTIC_EXTENDED_NOTATION_DISABLED"));
    }

    private static CborReadResult Parse(
        string text,
        bool extended = false,
        CborDiagnosticOptions? suppliedOptions = null)
    {
        CborDiagnosticOptions options = suppliedOptions ?? new CborDiagnosticOptions();
        options.UseExtendedNotation = extended;
        return new CborDiagnosticConverter().ParseDiagnostic(text, options);
    }

    private static CborValue ParseDone(string text, bool extended = false)
    {
        CborReadResult result = Parse(text, extended);
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done),
            $"{result.ErrorCode} at {result.ErrorOffset} ({result.ErrorPath}): {result.ErrorMessage}");
        Assert.That(result.Value, Is.Not.Null);
        return result.Value!;
    }
}
