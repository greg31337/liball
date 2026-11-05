using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborDiagnosticFormatterTests
{
    [Test]
    public void FormatsIntegersBytesTextAndSimpleValues()
    {
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticOptions options = new CborDiagnosticOptions();
        BigInteger large = BigInteger.One << 100;

        Assert.Multiple(() =>
        {
            Assert.That(converter.FormatDiagnostic(Integer(large), options),
                Is.EqualTo("1267650600228229401496703205376"));
            Assert.That(converter.FormatDiagnostic(Integer(-large), options),
                Is.EqualTo("-1267650600228229401496703205376"));
            Assert.That(converter.FormatDiagnostic(Bytes([0x00, 0xab, 0xff]), options),
                Is.EqualTo("h'00abff'"));
            Assert.That(converter.FormatDiagnostic(Text("CBOR"), options),
                Is.EqualTo("\"CBOR\""));
            Assert.That(converter.FormatDiagnostic(Simple(20), options),
                Is.EqualTo("false"));
            Assert.That(converter.FormatDiagnostic(Simple(21), options),
                Is.EqualTo("true"));
            Assert.That(converter.FormatDiagnostic(Simple(22), options),
                Is.EqualTo("null"));
            Assert.That(converter.FormatDiagnostic(Simple(23), options),
                Is.EqualTo("undefined"));
            Assert.That(converter.FormatDiagnostic(Simple(42), options),
                Is.EqualTo("simple(42)"));
        });
    }

    [Test]
    public void EscapesTextWithoutChangingValidUnicodeScalars()
    {
        string value = "\"\\\b\f\n\r\t\0\u001fé😀";

        string formatted = new CborDiagnosticConverter().FormatDiagnostic(
            Text(value),
            new CborDiagnosticOptions());

        Assert.That(
            formatted,
            Is.EqualTo("\"\\\"\\\\\\b\\f\\n\\r\\t\\u0000\\u001fé😀\""));
    }

    [Test]
    public void FormatsFiniteAndSpecialFloatingPointValuesDistinctly()
    {
        CborValue values = Array(
        [
            Float(0.0),
            Float(-0.0),
            Float(1.0),
            Float(-4.0),
            Float(1.5),
            Float(1.0e300),
            Float(double.PositiveInfinity),
            Float(double.NegativeInfinity),
            Float(double.NaN)
        ]);

        string formatted = new CborDiagnosticConverter().FormatDiagnostic(
            values,
            new CborDiagnosticOptions());

        Assert.That(
            formatted,
            Is.EqualTo(
                "[0.0,-0.0,1.0,-4.0,1.5,1.0e+300,Infinity,-Infinity,NaN]"));
    }

    [Test]
    public void CompactFormattingSupportsArbitraryMapKeysAndNestedTags()
    {
        CborValue value = Map(
        [
            Entry(
                Array([Integer(1), Text("key")]),
                Tag(100, Simple(42))),
            Entry(
                Bytes([0x01]),
                Array([Simple(21), Simple(22)]))
        ]);

        string formatted = new CborDiagnosticConverter().FormatDiagnostic(
            value,
            new CborDiagnosticOptions());

        Assert.That(
            formatted,
            Is.EqualTo("{[1,\"key\"]:100(simple(42)),h'01':[true,null]}"));
    }

    [Test]
    public void IndentedFormattingUsesConfiguredWhitespaceAtEveryLevel()
    {
        CborValue value = Array(
        [
            Integer(1),
            Map(
            [
                Entry(Text("name"), Text("CBOR")),
                Entry(Bytes([0x01]), Array([Simple(21), Simple(22)]))
            ])
        ]);
        CborDiagnosticOptions options = new CborDiagnosticOptions
        {
            Style = CborDiagnosticStyle.Indented,
            IndentSize = 2
        };

        string formatted = new CborDiagnosticConverter().FormatDiagnostic(value, options);

        Assert.That(
            formatted,
            Is.EqualTo(
                "[\n"
                + "  1,\n"
                + "  {\n"
                + "    \"name\": \"CBOR\",\n"
                + "    h'01': [\n"
                + "      true,\n"
                + "      null\n"
                + "    ]\n"
                + "  }\n"
                + "]"));
    }

    [Test]
    public void EmptyCollectionsRemainCompactInIndentedMode()
    {
        CborDiagnosticOptions options = new CborDiagnosticOptions
        {
            Style = CborDiagnosticStyle.Indented
        };
        CborDiagnosticConverter converter = new CborDiagnosticConverter();

        Assert.Multiple(() =>
        {
            Assert.That(converter.FormatDiagnostic(Array([]), options),
                Is.EqualTo("[]"));
            Assert.That(converter.FormatDiagnostic(Map([]), options),
                Is.EqualTo("{}"));
        });
    }

    [Test]
    public void EnforcesCharacterAndDepthLimitsWithExactPaths()
    {
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticOptions exactCharacters = new CborDiagnosticOptions
        {
            MaxCharacters = 3
        };
        CborDiagnosticOptions tooFewCharacters = new CborDiagnosticOptions
        {
            MaxCharacters = 2
        };
        CborDiagnosticOptions noNestedValues = new CborDiagnosticOptions
        {
            MaxNestingDepth = 0
        };

        string exact = converter.FormatDiagnostic(Text("a"), exactCharacters);
        CborDiagnosticException? characters = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(Text("a"), tooFewCharacters));
        CborDiagnosticException? escapedCharacters = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(Text("\n"), tooFewCharacters));
        CborDiagnosticException? byteCharacters = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(Bytes([0x00]), exactCharacters));
        CborDiagnosticException? depth = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(Array([Integer(1)]), noNestedValues));

        Assert.Multiple(() =>
        {
            Assert.That(exact, Is.EqualTo("\"a\""));
            AssertException(characters, "CBOR_DIAGNOSTIC_MAX_CHARACTERS", "$" );
            AssertException(
                escapedCharacters,
                "CBOR_DIAGNOSTIC_MAX_CHARACTERS",
                "$" );
            AssertException(
                byteCharacters,
                "CBOR_DIAGNOSTIC_MAX_CHARACTERS",
                "$" );
            AssertException(depth, "CBOR_DIAGNOSTIC_MAX_NESTING_DEPTH", "$[0]");
            Assert.That(converter.FormatDiagnostic(Integer(1), noNestedValues),
                Is.EqualTo("1"));
        });
    }

    [Test]
    public void RejectsInvalidOptionsAndIncompleteValues()
    {
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborDiagnosticException? missingValue = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(null!, new CborDiagnosticOptions()));
        CborDiagnosticException? missingOptions = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(Integer(1), null!));
        CborDiagnosticException? invalidStyle = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(
                Integer(1),
                new CborDiagnosticOptions
                {
                    Style = (CborDiagnosticStyle)int.MaxValue
                }));
        CborDiagnosticException? invalidIndent = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(
                Integer(1),
                new CborDiagnosticOptions { IndentSize = -1 }));
        CborDiagnosticException? invalidDepth = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(
                Integer(1),
                new CborDiagnosticOptions { MaxNestingDepth = -1 }));
        CborDiagnosticException? invalidCharacters = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(
                Integer(1),
                new CborDiagnosticOptions { MaxCharacters = -1 }));
        CborDiagnosticException? missingBytes = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(
                new CborValue { Kind = CborValueKind.ByteString },
                new CborDiagnosticOptions()));
        CborDiagnosticException? missingTag = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(
                new CborValue { Kind = CborValueKind.Tag, TagNumber = 5 },
                new CborDiagnosticOptions()));
        CborDiagnosticException? reservedSimple = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(Simple(24), new CborDiagnosticOptions()));

        Assert.Multiple(() =>
        {
            AssertException(missingValue, "CBOR_VALUE_MISSING", "$" );
            AssertException(
                missingOptions,
                "CBOR_DIAGNOSTIC_OPTIONS_MISSING",
                "$" );
            AssertException(invalidStyle, "CBOR_DIAGNOSTIC_STYLE_INVALID", "$" );
            AssertException(invalidIndent, "CBOR_DIAGNOSTIC_INDENT_INVALID", "$" );
            AssertException(invalidDepth, "CBOR_DIAGNOSTIC_DEPTH_INVALID", "$" );
            AssertException(
                invalidCharacters,
                "CBOR_DIAGNOSTIC_CHARACTER_LIMIT_INVALID",
                "$" );
            AssertException(missingBytes, "CBOR_BYTE_STRING_VALUE_MISSING", "$" );
            AssertException(missingTag, "CBOR_TAG_CONTENT_MISSING", "$" );
            AssertException(reservedSimple, "CBOR_SIMPLE_VALUE_RESERVED", "$" );
        });
    }

    [Test]
    public void RejectsInvalidUtf16AndCyclicGraphs()
    {
        CborDiagnosticConverter converter = new CborDiagnosticConverter();
        CborValue cycle = Array([]);
        cycle.ArrayItems!.Add(cycle);

        CborDiagnosticException? text = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(
                Text("\ud800"),
                new CborDiagnosticOptions()));
        CborDiagnosticException? graph = Assert.Throws<CborDiagnosticException>(() =>
            converter.FormatDiagnostic(cycle, new CborDiagnosticOptions()));

        Assert.Multiple(() =>
        {
            AssertException(text, "CBOR_TEXT_STRING_INVALID_UTF16", "$" );
            AssertException(graph, "CBOR_DIAGNOSTIC_CYCLE", "$[0]");
        });
    }

    private static CborValue Integer(BigInteger value)
    {
        return new CborValue
        {
            Kind = value < BigInteger.Zero
                ? CborValueKind.NegativeInteger
                : CborValueKind.UnsignedInteger,
            IntegerValue = value
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

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Array(CborValue[] values)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [.. values]
        };
    }

    private static CborValue Map(CborMapEntry[] entries)
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = [.. entries]
        };
    }

    private static CborMapEntry Entry(CborValue key, CborValue value)
    {
        return new CborMapEntry { Key = key, Value = value };
    }

    private static CborValue Tag(ulong tagNumber, CborValue content)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = tagNumber,
            TaggedValue = content
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

    private static void AssertException(
        CborDiagnosticException? exception,
        string code,
        string path)
    {
        Assert.Multiple(() =>
        {
            Assert.That(exception?.ErrorCode, Is.EqualTo(code));
            Assert.That(exception?.ErrorOffset, Is.EqualTo(-1));
            Assert.That(exception?.ErrorPath, Is.EqualTo(path));
        });
    }
}
