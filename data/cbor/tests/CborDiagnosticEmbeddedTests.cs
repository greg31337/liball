using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborDiagnosticEmbeddedTests
{
    [Test]
    public void Rfc8610EmbeddedExamplesProduceTheirExactBytes()
    {
        CborValue one = ParseDone("<<1>>");
        CborValue sequence = ParseDone("<<1, 2>>");
        CborValue textAndNull = ParseDone("<<\"foo\", null>>");
        CborValue empty = ParseDone("<<>>");

        Assert.Multiple(() =>
        {
            Assert.That(one.ByteStringValue, Is.EqualTo(new byte[] { 0x01 }));
            Assert.That(sequence.ByteStringValue,
                Is.EqualTo(new byte[] { 0x01, 0x02 }));
            Assert.That(textAndNull.ByteStringValue,
                Is.EqualTo(new byte[] { 0x63, 0x66, 0x6f, 0x6f, 0xf6 }));
            Assert.That(empty.ByteStringValue, Is.Empty);
        });
    }

    [Test]
    public void EmbeddedSequencesSupportEveryDiagnosticValueKind()
    {
        CborValue value = ParseDone(
            "<<0,-1,h'01',\"x\",[1],{2:3},4(null),1.5,true,simple(42)>>"
        );
        byte[] expected = new CborEncoder().EncodeSequence(
            [
                Unsigned(0),
                Negative(-1),
                Bytes([1]),
                Text("x"),
                Array([Unsigned(1)]),
                Map(Unsigned(2), Unsigned(3)),
                Tag(4, Simple(CborSimpleValues.Null)),
                Float(1.5),
                Simple(CborSimpleValues.True),
                Simple(42)
            ],
            new CborEncodeOptions()
        );

        Assert.That(value.ByteStringValue, Is.EqualTo(expected));
    }

    [Test]
    public void EmbeddedByteStringsNestAndParticipateInConcatenation()
    {
        CborValue nested = ParseDone("<<<<1>>>>");
        CborValue byteConcatenation = ParseDone("h'aa' <<1,2>> 'x'");
        CborValue textConcatenation = ParseDone("\"\" <<\"x\">> \"\"");
        CborValue chunks = ParseDone("(_ <<1>>,h'02')");

        Assert.Multiple(() =>
        {
            Assert.That(nested.ByteStringValue,
                Is.EqualTo(new byte[] { 0x41, 0x01 }));
            Assert.That(byteConcatenation.ByteStringValue,
                Is.EqualTo(new byte[] { 0xaa, 0x01, 0x02, 0x78 }));
            Assert.That(textConcatenation.TextStringValue, Is.EqualTo("ax"));
            Assert.That(chunks.ByteStringValue,
                Is.EqualTo(new byte[] { 0x01, 0x02 }));
        });
    }

    [Test]
    public void EmbeddedNotationAcceptsTriviaAtEverySequenceBoundary()
    {
        CborValue value = ParseDone(
            "/outer/ << /first/ 1 /after first/ , /second/ [2,3] /last/ >> /done/"
        );

        Assert.That(value.ByteStringValue,
            Is.EqualTo(new byte[] { 0x01, 0x82, 0x02, 0x03 }));
    }

    [Test]
    public void MalformedEmbeddedDelimitersReportExactOffsetsAndPaths()
    {
        CborReadResult opening = Parse("<1>>");
        CborReadResult unterminated = Parse("<<1");
        CborReadResult singleClose = Parse("<<1>");
        CborReadResult trailingComma = Parse("<<1,>>");
        CborReadResult missingComma = Parse("<<1 2>>");

        Assert.Multiple(() =>
        {
            AssertError(opening,
                "CBOR_DIAGNOSTIC_EMBEDDED_OPEN_INVALID", 0, "$" );
            AssertError(unterminated,
                "CBOR_DIAGNOSTIC_EMBEDDED_UNTERMINATED", 3, "$" );
            AssertError(singleClose,
                "CBOR_DIAGNOSTIC_EMBEDDED_DELIMITER_EXPECTED", 3,
                "$<embedded:0>" );
            AssertError(trailingComma,
                "CBOR_DIAGNOSTIC_EMBEDDED_VALUE_EXPECTED", 4,
                "$<embedded:1>" );
            AssertError(missingComma,
                "CBOR_DIAGNOSTIC_EMBEDDED_DELIMITER_EXPECTED", 4,
                "$<embedded:0>" );
        });
    }

    [Test]
    public void NestedParseAndEncodeFailuresRetainTheirExactSourceLocations()
    {
        CborReadResult lexeme = Parse("<< 1, [0, 01] >>");
        CborReadResult encode = Parse("<<[0,18446744073709551616]>>");

        Assert.Multiple(() =>
        {
            AssertError(lexeme,
                "CBOR_DIAGNOSTIC_NUMBER_INVALID", 10,
                "$<embedded:1>[1]" );
            AssertError(encode,
                "CBOR_UNSIGNED_INTEGER_OUT_OF_RANGE", 5,
                "$<embedded:0>[1]" );
        });
    }

    [Test]
    public void EmbeddedDepthItemAndByteBudgetsAreSharedRecursively()
    {
        CborReadResult depth = Parse(
            "<<<<1>>>>",
            new CborDiagnosticOptions { MaxEmbeddedDepth = 1 }
        );
        CborReadResult items = Parse(
            "<<1,2>>",
            new CborDiagnosticOptions { MaxEmbeddedItems = 1 }
        );
        CborReadResult nestedItems = Parse(
            "<<[1,2]>>",
            new CborDiagnosticOptions { MaxEmbeddedItems = 2 }
        );
        CborReadResult bytes = Parse(
            "<<24>>",
            new CborDiagnosticOptions { MaxEmbeddedBytes = 1 }
        );
        CborReadResult recursiveBytes = Parse(
            "<<<<1>>>>",
            new CborDiagnosticOptions { MaxEmbeddedBytes = 2 }
        );
        CborValue empty = ParseDone(
            "<<>>",
            new CborDiagnosticOptions
            {
                MaxEmbeddedItems = 0,
                MaxEmbeddedBytes = 0
            }
        );

        Assert.Multiple(() =>
        {
            AssertError(depth,
                "CBOR_DIAGNOSTIC_MAX_EMBEDDED_DEPTH", 2,
                "$<embedded:0>" );
            AssertError(items,
                "CBOR_DIAGNOSTIC_MAX_EMBEDDED_ITEMS", 4,
                "$<embedded:1>" );
            AssertError(nestedItems,
                "CBOR_DIAGNOSTIC_MAX_EMBEDDED_ITEMS", 5,
                "$<embedded:0>[1]" );
            AssertError(bytes,
                "CBOR_DIAGNOSTIC_MAX_EMBEDDED_BYTES", 2,
                "$<embedded:0>" );
            AssertError(recursiveBytes,
                "CBOR_DIAGNOSTIC_MAX_EMBEDDED_BYTES", 2,
                "$<embedded:0>" );
            Assert.That(empty.ByteStringValue, Is.Empty);
        });
    }

    [Test]
    public void InvalidEmbeddedLimitsFailBeforeParsingInput()
    {
        CborReadResult depth = Parse(
            "<<>>",
            new CborDiagnosticOptions { MaxEmbeddedDepth = -1 }
        );
        CborReadResult items = Parse(
            "<<>>",
            new CborDiagnosticOptions { MaxEmbeddedItems = -1 }
        );
        CborReadResult bytes = Parse(
            "<<>>",
            new CborDiagnosticOptions { MaxEmbeddedBytes = -1 }
        );

        Assert.Multiple(() =>
        {
            AssertError(depth,
                "CBOR_DIAGNOSTIC_EMBEDDED_DEPTH_INVALID", -1, "$" );
            AssertError(items,
                "CBOR_DIAGNOSTIC_EMBEDDED_ITEM_LIMIT_INVALID", -1, "$" );
            AssertError(bytes,
                "CBOR_DIAGNOSTIC_EMBEDDED_BYTE_LIMIT_INVALID", -1, "$" );
        });
    }

    private static CborReadResult Parse(
        string text,
        CborDiagnosticOptions? suppliedOptions = null)
    {
        CborDiagnosticOptions options = suppliedOptions ?? new CborDiagnosticOptions();
        options.UseExtendedNotation = true;
        return new CborDiagnosticConverter().ParseDiagnostic(text, options);
    }

    private static CborValue ParseDone(
        string text,
        CborDiagnosticOptions? options = null)
    {
        CborReadResult result = Parse(text, options);
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done),
            $"{result.ErrorCode} at {result.ErrorOffset} ({result.ErrorPath}): {result.ErrorMessage}");
        Assert.That(result.Value, Is.Not.Null);
        return result.Value!;
    }

    private static void AssertError(
        CborReadResult result,
        string code,
        long offset,
        string path)
    {
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
        Assert.That(result.ErrorCode, Is.EqualTo(code));
        Assert.That(result.ErrorOffset, Is.EqualTo(offset));
        Assert.That(result.ErrorPath, Is.EqualTo(path));
    }

    private static CborValue Unsigned(ulong number)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = number
        };
    }

    private static CborValue Negative(long number)
    {
        return new CborValue
        {
            Kind = CborValueKind.NegativeInteger,
            IntegerValue = number
        };
    }

    private static CborValue Bytes(byte[] bytes)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = bytes
        };
    }

    private static CborValue Text(string text)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = text
        };
    }

    private static CborValue Array(List<CborValue> items)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = items
        };
    }

    private static CborValue Map(CborValue key, CborValue value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = [new CborMapEntry { Key = key, Value = value }]
        };
    }

    private static CborValue Tag(ulong number, CborValue content)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = number,
            TaggedValue = content
        };
    }

    private static CborValue Float(double number)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = CborFloatWidth.Double,
            FloatValue = new CborFloatValue
            {
                Value = number,
                Width = CborFloatWidth.Double,
                RawBits = BitConverter.DoubleToUInt64Bits(number)
            }
        };
    }

    private static CborValue Simple(byte number)
    {
        return new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = number
        };
    }
}
