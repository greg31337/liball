using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborDiagnosticRepresentationSequenceTests
{
    [Test]
    public void ParsingIndicatorsRetainsEveryRepresentableDistinction()
    {
        CborValue root = ParseDone(
            "[_ 1_0,1.5_1,h'00'_0,{_ \"x\":2_3}," +
            "simple(42)_0,100(null)_1,\"\"_]"
        );
        List<CborValue> items = root.ArrayItems!;
        CborValue definiteArray = ParseDone("[_0 1]");

        Assert.Multiple(() =>
        {
            Assert.That(root.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(root.Representation!.LengthKind,
                Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(items[0].Representation!.HeadArgumentWidth, Is.EqualTo(1));
            Assert.That(items[1].FloatWidth, Is.EqualTo(CborFloatWidth.Half));
            Assert.That(items[1].FloatValue!.RawBits, Is.EqualTo(0x3e00));
            Assert.That(items[1].Representation!.FloatWidth,
                Is.EqualTo(CborFloatWidth.Half));
            Assert.That(items[2].LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(items[2].Representation!.HeadArgumentWidth, Is.EqualTo(1));
            Assert.That(items[3].LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(items[3].MapEntries![0].Value!.Representation!.HeadArgumentWidth,
                Is.EqualTo(8));
            Assert.That(items[4].Representation!.HeadArgumentWidth, Is.EqualTo(1));
            Assert.That(items[5].Representation!.HeadArgumentWidth, Is.EqualTo(2));
            Assert.That(items[6].LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(items[6].Representation!.ChunkByteLengths, Is.Empty);
            Assert.That(definiteArray.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(definiteArray.Representation!.HeadArgumentWidth, Is.EqualTo(1));
            Assert.That(new CborDiagnosticConverter().FormatDiagnostic(
                definiteArray,
                RepresentationOptions()), Is.EqualTo("[_0 1]"));
        });
    }

    [Test]
    public void IncludedRepresentationIndicatorsRoundTripStably()
    {
        const string notation =
            "[_ 1_0,1.5_1,h'00'_0,{_ \"x\":2_3}," +
            "simple(42)_0,100(null)_1,\"\"_]";
        CborDiagnosticOptions options = RepresentationOptions();
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborValue first = ParseDone(notation);
        string formatted = converter.FormatDiagnostic(first, options);
        CborValue second = converter.ParseDiagnostic(formatted, options).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Is.EqualTo(notation));
            Assert.That(second.Representation!.LengthKind,
                Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(second.ArrayItems![1].FloatWidth,
                Is.EqualTo(CborFloatWidth.Half));
            Assert.That(second.ArrayItems![3].MapEntries![0].Value!
                .Representation!.HeadArgumentWidth, Is.EqualTo(8));
        });
    }

    [Test]
    public void RepresentationOutputRemainsOptIn()
    {
        CborValue value = ParseDone("[_ 1_0,1.5_1,h''_]");
        string basic = new CborDiagnosticConverter().FormatDiagnostic(
            value,
            new CborDiagnosticOptions()
        );

        Assert.That(basic, Is.EqualTo("[1,1.5,h'']"));
    }

    [Test]
    public void IndefiniteStringChunkBoundariesRoundTripForBytesAndUtf8Text()
    {
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticOptions options = RepresentationOptions();
        CborValue bytes = ParseDone("(_ h'01',h'',h'0203')");
        CborValue text = ParseDone("(_ \"a\",\"\",\"☃\")");

        string formattedBytes = converter.FormatDiagnostic(bytes, options);
        string formattedText = converter.FormatDiagnostic(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Representation!.ChunkByteLengths,
                Is.EqualTo(new[] { 1, 0, 2 }));
            Assert.That(text.Representation!.ChunkByteLengths,
                Is.EqualTo(new[] { 1, 0, 3 }));
            Assert.That(formattedBytes, Is.EqualTo("(_ h'01',h'',h'0203')"));
            Assert.That(formattedText, Is.EqualTo("(_ \"a\",\"\",\"☃\")"));
            Assert.That(ParseDone(formattedBytes).Representation!.ChunkByteLengths,
                Is.EqualTo(new[] { 1, 0, 2 }));
            Assert.That(ParseDone(formattedText).Representation!.ChunkByteLengths,
                Is.EqualTo(new[] { 1, 0, 3 }));
        });
    }

    [Test]
    public void EmbeddedCborHonorsParsedRepresentationIndicators()
    {
        CborValue value = ParseDone("<<1_0,1.5_1,[_ 2]>>");

        Assert.That(value.ByteStringValue, Is.EqualTo(new byte[]
        {
            0x18, 0x01,
            0xf9, 0x3e, 0x00,
            0x9f, 0x02, 0xff
        }));
    }

    [Test]
    public void InvalidOrInconsistentIndicatorsReturnExactDiagnostics()
    {
        CborReadResult reserved = Parse("1_4");
        CborReadResult narrowInteger = Parse("300_0");
        CborReadResult narrowFloat = Parse("1.1_1");
        CborReadResult nonemptyIndefinite = Parse("\"x\"_");
        CborReadResult simple = Parse("false_0");

        Assert.Multiple(() =>
        {
            AssertError(reserved,
                "CBOR_DIAGNOSTIC_REPRESENTATION_INDICATOR_INVALID", 1, "$" );
            AssertError(narrowInteger,
                "CBOR_DIAGNOSTIC_REPRESENTATION_MISMATCH", 3, "$" );
            AssertError(narrowFloat,
                "CBOR_DIAGNOSTIC_REPRESENTATION_MISMATCH", 3, "$" );
            AssertError(nonemptyIndefinite,
                "CBOR_DIAGNOSTIC_REPRESENTATION_INDICATOR_INVALID", 3, "$" );
            AssertError(simple,
                "CBOR_DIAGNOSTIC_REPRESENTATION_MISMATCH", 5, "$" );
        });
    }

    [Test]
    public void UnknownIndicatorsRemainIgnorable()
    {
        CborValue value = ParseDone("1_application_hint");
        string formatted = new CborDiagnosticConverter().FormatDiagnostic(
            value,
            RepresentationOptions()
        );

        Assert.Multiple(() =>
        {
            Assert.That(value.IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(value.Representation, Is.Null);
            Assert.That(formatted, Is.EqualTo("1"));
        });
    }

    [Test]
    public void DeterministicMapDisplaySupportsBothRfcKeyOrders()
    {
        CborValue map = Map(
            Entry(Text("a"), Text("text")),
            Entry(Unsigned(100), Text("hundred")),
            Entry(Bytes([]), Text("bytes")),
            Entry(Negative(-1), Text("negative")),
            Entry(Unsigned(10), Text("ten"))
        );
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticOptions lexical = new CborDiagnosticOptions
        {
            MapKeyOrder = CborMapKeyOrder.Lexicographic
        };
        CborDiagnosticOptions lengthFirst = new CborDiagnosticOptions
        {
            MapKeyOrder = CborMapKeyOrder.LengthFirst
        };

        Assert.Multiple(() =>
        {
            Assert.That(converter.FormatDiagnostic(map, lexical), Is.EqualTo(
                "{10:\"ten\",100:\"hundred\",-1:\"negative\"," +
                "h'':\"bytes\",\"a\":\"text\"}"
            ));
            Assert.That(converter.FormatDiagnostic(map, lengthFirst), Is.EqualTo(
                "{10:\"ten\",-1:\"negative\",h'':\"bytes\"," +
                "100:\"hundred\",\"a\":\"text\"}"
            ));
            Assert.That(map.MapEntries![0].Key!.TextStringValue, Is.EqualTo("a"));
        });
    }

    [Test]
    public void DeterministicMapDisplayRejectsDuplicateEncodedKeys()
    {
        CborValue map = Map(
            Entry(Unsigned(1), Text("first")),
            Entry(Unsigned(1), Text("second"))
        );
        CborDiagnosticException exception = Assert.Throws<CborDiagnosticException>(() =>
            new CborDiagnosticConverter().FormatDiagnostic(
                map,
                new CborDiagnosticOptions
                {
                    MapKeyOrder = CborMapKeyOrder.Lexicographic
                }
            ))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_DETERMINISTIC_DUPLICATE_KEY"));
            Assert.That(exception.ErrorPath, Is.EqualTo("$[1].key"));
        });
    }

    [Test]
    public void RepresentationAndOrderingValidationFailuresAreStructured()
    {
        CborValue invalidRepresentation = Unsigned(1);
        invalidRepresentation.Representation = new CborRepresentation
        {
            HeadArgumentWidth = 3
        };
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticException representation = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(
                invalidRepresentation,
                RepresentationOptions()
            ))!;
        CborDiagnosticException order = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(
                Unsigned(1),
                new CborDiagnosticOptions
                {
                    MapKeyOrder = (CborMapKeyOrder)int.MaxValue
                }
            ))!;

        Assert.Multiple(() =>
        {
            Assert.That(representation.ErrorCode,
                Is.EqualTo("CBOR_REPRESENTATION_HEAD_WIDTH_INVALID"));
            Assert.That(representation.ErrorPath, Is.EqualTo("$"));
            Assert.That(order.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_MAP_KEY_ORDER_INVALID"));
        });
    }

    [Test]
    public void DiagnosticSequenceFormatsAndParsesEmptyAndMultipleItems()
    {
        ICborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticOptions options = new CborDiagnosticOptions();
        string empty = converter.FormatDiagnosticSequence([], options);
        string formatted = converter.FormatDiagnosticSequence(
            [Unsigned(1), Text("x"), Array(Unsigned(2))],
            options
        );
        List<CborReadResult> emptyResults = converter.ParseDiagnosticSequence(
            empty,
            options
        );
        List<CborReadResult> results = converter.ParseDiagnosticSequence(
            formatted,
            options
        );

        Assert.Multiple(() =>
        {
            Assert.That(empty, Is.Empty);
            Assert.That(emptyResults, Is.Empty);
            Assert.That(formatted, Is.EqualTo("1,\"x\",[2]"));
            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(results.Select(result => result.Status),
                Is.All.EqualTo(CborReadStatus.Done));
            Assert.That(results.Select(result => result.ItemIndex),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(results.Select(result => result.SourceOffset),
                Is.EqualTo(new long[] { 0, 2, 6 }));
            Assert.That(results[0].Value!.IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(results[1].Value!.TextStringValue, Is.EqualTo("x"));
            Assert.That(results[2].Value!.ArrayItems![0].IntegerValue,
                Is.EqualTo(new BigInteger(2)));
            Assert.That(results[2].Remaining, Is.Zero);
        });
    }

    [Test]
    public void DiagnosticSequencesRoundTripPerItemRepresentation()
    {
        ICborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticOptions options = RepresentationOptions();
        string formatted = converter.FormatDiagnosticSequence(
            [ParseDone("1_0"), ParseDone("1.5_1"), ParseDone("h''_")],
            options
        );
        List<CborReadResult> parsed = converter.ParseDiagnosticSequence(
            formatted,
            options
        );

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Is.EqualTo("1_0,1.5_1,h''_"));
            Assert.That(parsed[0].Value!.Representation!.HeadArgumentWidth,
                Is.EqualTo(1));
            Assert.That(parsed[1].Value!.FloatWidth, Is.EqualTo(CborFloatWidth.Half));
            Assert.That(parsed[2].Value!.LengthKind,
                Is.EqualTo(CborLengthKind.Indefinite));
        });
    }

    [Test]
    public void IndentedSequencesUseOneTopLevelItemPerLine()
    {
        string formatted = new CborDiagnosticConverter().FormatDiagnosticSequence(
            [Unsigned(1), Array(Unsigned(2), Unsigned(3))],
            new CborDiagnosticOptions
            {
                Style = CborDiagnosticStyle.Indented,
                IndentSize = 2
            }
        );

        Assert.That(formatted, Is.EqualTo("1,\n[\n  2,\n  3\n]"));
    }

    [Test]
    public void SequenceParsingPreservesCompletedItemsBeforeExactFailures()
    {
        ICborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticOptions options = new CborDiagnosticOptions();
        List<CborReadResult> invalidNumber = converter.ParseDiagnosticSequence(
            "1,01",
            options
        );
        List<CborReadResult> missingComma = converter.ParseDiagnosticSequence(
            "1 2",
            options
        );
        List<CborReadResult> trailingComma = converter.ParseDiagnosticSequence(
            "1,",
            options
        );

        Assert.Multiple(() =>
        {
            Assert.That(invalidNumber, Has.Count.EqualTo(2));
            Assert.That(invalidNumber[0].Status, Is.EqualTo(CborReadStatus.Done));
            AssertError(invalidNumber[1],
                "CBOR_DIAGNOSTIC_NUMBER_INVALID", 2, "$[1]" );
            Assert.That(invalidNumber[1].ItemIndex, Is.EqualTo(1));
            Assert.That(invalidNumber[1].SourceOffset, Is.EqualTo(2));
            AssertError(missingComma[1],
                "CBOR_DIAGNOSTIC_SEQUENCE_DELIMITER_EXPECTED", 2, "$[1]" );
            AssertError(trailingComma[1],
                "CBOR_DIAGNOSTIC_SEQUENCE_VALUE_EXPECTED", 2, "$[1]" );
        });
    }

    [Test]
    public void SequenceOperationsValidateMissingInputsAndCharacterLimits()
    {
        ICborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticException missingValues = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnosticSequence(null!, new CborDiagnosticOptions()))!;
        List<CborReadResult> missingText = converter.ParseDiagnosticSequence(
            null!,
            new CborDiagnosticOptions()
        );
        List<CborReadResult> limited = converter.ParseDiagnosticSequence(
            "1,2",
            new CborDiagnosticOptions { MaxCharacters = 2 }
        );

        Assert.Multiple(() =>
        {
            Assert.That(missingValues.ErrorCode,
                Is.EqualTo("CBOR_DIAGNOSTIC_SEQUENCE_VALUES_MISSING"));
            AssertError(missingText[0],
                "CBOR_DIAGNOSTIC_TEXT_MISSING", -1, "$[0]" );
            AssertError(limited[0],
                "CBOR_DIAGNOSTIC_MAX_CHARACTERS", 2, "$" );
        });
    }

    private static CborDiagnosticOptions RepresentationOptions()
    {
        return new CborDiagnosticOptions
        {
            UseExtendedNotation = true,
            IncludeRepresentation = true
        };
    }

    private static CborReadResult Parse(string text)
    {
        return new CborDiagnosticConverter().ParseDiagnostic(
            text,
            new CborDiagnosticOptions { UseExtendedNotation = true }
        );
    }

    private static CborValue ParseDone(string text)
    {
        CborReadResult result = Parse(text);
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done),
            $"{result.ErrorCode} at {result.ErrorOffset} ({result.ErrorPath}): {result.ErrorMessage}");
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

    private static CborValue Array(params CborValue[] values)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = values.ToList()
        };
    }

    private static CborMapEntry Entry(CborValue key, CborValue value)
    {
        return new CborMapEntry { Key = key, Value = value };
    }

    private static CborValue Map(params CborMapEntry[] entries)
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = entries.ToList()
        };
    }
}
