using System.Numerics;
using System.Text.Json;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborJsonWriterTests
{
    [Test]
    public void StrictPolicyWritesTheDirectJsonDataModel()
    {
        CborValue value = Map(
        [
            Entry(Text("name"), Text("CBOR\n😀")),
            Entry(
                Text("values"),
                Array(
                [
                    Integer(-9007199254740991L),
                    Integer(9007199254740991L),
                    Float(1.5),
                    Simple(20),
                    Simple(21),
                    Simple(22)
                ]))
        ]);

        string json = new CborJsonConverter().ConvertToJson(
            value,
            new CborJsonOptions());

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("name").GetString(), Is.EqualTo("CBOR\n😀"));
            Assert.That(root.GetProperty("values")[0].GetInt64(),
                Is.EqualTo(-9007199254740991L));
            Assert.That(root.GetProperty("values")[1].GetInt64(),
                Is.EqualTo(9007199254740991L));
            Assert.That(root.GetProperty("values")[2].GetDouble(), Is.EqualTo(1.5));
            Assert.That(root.GetProperty("values")[3].GetBoolean(), Is.False);
            Assert.That(root.GetProperty("values")[4].GetBoolean(), Is.True);
            Assert.That(root.GetProperty("values")[5].ValueKind, Is.EqualTo(JsonValueKind.Null));
        });
    }

    [Test]
    public void ByteStringsUsePolicySelectedRepresentations()
    {
        CborValue value = Bytes([0xfb, 0xff, 0x00]);
        CborJsonConverter converter = new CborJsonConverter();

        CborJsonException? strict = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(value, Options(CborJsonPolicy.Strict)));
        string preserved = converter.ConvertToJson(
            value,
            Options(CborJsonPolicy.PreserveInformation));
        string lossy = converter.ConvertToJson(
            value,
            Options(CborJsonPolicy.AllowLossy));

        Assert.Multiple(() =>
        {
            AssertException(strict, "CBOR_JSON_BYTE_STRING_UNREPRESENTABLE", "$" );
            Assert.That(preserved, Is.EqualTo(
                "{\"$cbor\":\"bytes\",\"encoding\":\"base64url\",\"value\":\"-_8A\"}"));
            Assert.That(lossy, Is.EqualTo("\"-_8A\""));
        });
    }

    [Test]
    public void LargeIntegersUsePolicySelectedRepresentations()
    {
        BigInteger positive = BigInteger.Parse("18446744073709551616");
        BigInteger negative = BigInteger.Parse("-18446744073709551617");
        CborValue value = Array([Integer(positive), Integer(negative)]);
        CborJsonConverter converter = new CborJsonConverter();

        CborJsonException? strict = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(value, Options(CborJsonPolicy.Strict)));
        string preserved = converter.ConvertToJson(
            value,
            Options(CborJsonPolicy.PreserveInformation));
        string lossy = converter.ConvertToJson(
            value,
            Options(CborJsonPolicy.AllowLossy));

        Assert.Multiple(() =>
        {
            AssertException(strict, "CBOR_JSON_INTEGER_OUT_OF_RANGE", "$[0]");
            Assert.That(preserved, Is.EqualTo(
                "[{\"$cbor\":\"integer\",\"value\":\"18446744073709551616\"},"
                + "{\"$cbor\":\"integer\",\"value\":\"-18446744073709551617\"}]"));
            Assert.That(lossy, Is.EqualTo(
                "[\"18446744073709551616\",\"-18446744073709551617\"]"));
        });
    }

    [Test]
    public void TagsAreRejectedPreservedOrRemovedByPolicy()
    {
        CborValue value = Tag(18446744073709551615UL, Text("content"));
        CborJsonConverter converter = new CborJsonConverter();

        CborJsonException? strict = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(value, Options(CborJsonPolicy.Strict)));
        string preserved = converter.ConvertToJson(
            value,
            Options(CborJsonPolicy.PreserveInformation));
        string lossy = converter.ConvertToJson(
            value,
            Options(CborJsonPolicy.AllowLossy));

        Assert.Multiple(() =>
        {
            AssertException(strict, "CBOR_JSON_TAG_UNREPRESENTABLE", "$" );
            Assert.That(preserved, Is.EqualTo(
                "{\"$cbor\":\"tag\",\"number\":\"18446744073709551615\","
                + "\"value\":\"content\"}"));
            Assert.That(lossy, Is.EqualTo("\"content\""));
        });
    }

    [Test]
    public void MapPoliciesHandleNonTextAndDuplicateKeysExplicitly()
    {
        CborValue nonText = Map(
        [
            Entry(Integer(1), Text("first")),
            Entry(Text("1"), Text("last")),
            Entry(Bytes([0xfb, 0xff]), Simple(21))
        ]);
        CborValue duplicate = Map(
        [
            Entry(Text("same"), Integer(1)),
            Entry(Text("same"), Integer(2))
        ]);
        CborJsonConverter converter = new CborJsonConverter();

        CborJsonException? nonTextStrict = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(nonText, Options(CborJsonPolicy.Strict)));
        CborJsonException? duplicateStrict = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(duplicate, Options(CborJsonPolicy.Strict)));
        string preserved = converter.ConvertToJson(
            nonText,
            Options(CborJsonPolicy.PreserveInformation));
        string preservedDuplicate = converter.ConvertToJson(
            duplicate,
            Options(CborJsonPolicy.PreserveInformation));
        string lossy = converter.ConvertToJson(
            nonText,
            Options(CborJsonPolicy.AllowLossy));

        Assert.Multiple(() =>
        {
            AssertException(
                nonTextStrict,
                "CBOR_JSON_NON_TEXT_KEY_UNREPRESENTABLE",
                "$[0].key");
            AssertException(
                duplicateStrict,
                "CBOR_JSON_DUPLICATE_PROPERTY",
                "$[1].key");
            Assert.That(preserved, Is.EqualTo(
                "{\"$cbor\":\"map\",\"entries\":[[1,\"first\"],[\"1\",\"last\"],"
                + "[{\"$cbor\":\"bytes\",\"encoding\":\"base64url\","
                + "\"value\":\"-_8\"},true]]}"));
            Assert.That(preservedDuplicate, Is.EqualTo(
                "{\"$cbor\":\"map\",\"entries\":[[\"same\",1],[\"same\",2]]}"));
            Assert.That(lossy, Is.EqualTo("{\"1\":\"last\",\"base64url:-_8\":true}"));
        });
    }

    [Test]
    public void UndefinedAndNonFiniteFloatsUsePolicySelectedRepresentations()
    {
        CborValue value = Array(
        [
            Simple(23),
            Float(double.NaN),
            Float(double.PositiveInfinity),
            Float(double.NegativeInfinity),
            Simple(42)
        ]);
        CborJsonConverter converter = new CborJsonConverter();

        CborJsonException? strict = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(value, Options(CborJsonPolicy.Strict)));
        CborJsonException? strictFloat = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(Float(double.NaN), Options(CborJsonPolicy.Strict)));
        CborJsonException? strictSimple = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(Simple(42), Options(CborJsonPolicy.Strict)));
        string preserved = converter.ConvertToJson(
            value,
            Options(CborJsonPolicy.PreserveInformation));
        string lossy = converter.ConvertToJson(
            value,
            Options(CborJsonPolicy.AllowLossy));

        Assert.Multiple(() =>
        {
            AssertException(strict, "CBOR_JSON_UNDEFINED_UNREPRESENTABLE", "$[0]");
            AssertException(
                strictFloat,
                "CBOR_JSON_NON_FINITE_FLOAT_UNREPRESENTABLE",
                "$" );
            AssertException(strictSimple, "CBOR_JSON_SIMPLE_VALUE_UNREPRESENTABLE", "$" );
            Assert.That(preserved, Is.EqualTo(
                "[{\"$cbor\":\"undefined\"},{\"$cbor\":\"float\",\"value\":\"NaN\"},"
                + "{\"$cbor\":\"float\",\"value\":\"Infinity\"},"
                + "{\"$cbor\":\"float\",\"value\":\"-Infinity\"},"
                + "{\"$cbor\":\"simple\",\"value\":42}]"));
            Assert.That(lossy, Is.EqualTo("[null,null,null,null,null]"));
        });
    }

    [Test]
    public void SupportsIndentationAndEnforcesConfiguredLimits()
    {
        CborJsonConverter converter = new CborJsonConverter();
        CborJsonOptions indented = new CborJsonOptions { Indented = true };
        CborJsonOptions exactOutput = new CborJsonOptions { MaxOutputCharacters = 3 };
        CborJsonOptions shortOutput = new CborJsonOptions { MaxOutputCharacters = 2 };
        CborJsonOptions noNesting = new CborJsonOptions { MaxNestingDepth = 0 };

        string formatted = converter.ConvertToJson(
            Map([Entry(Text("a"), Array([Integer(1)]))]),
            indented);
        string exact = converter.ConvertToJson(Text("a"), exactOutput);
        CborJsonException? output = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(Text("a"), shortOutput));
        CborJsonException? depth = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(Array([Integer(1)]), noNesting));

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Does.Contain("\n"));
            Assert.DoesNotThrow(() => JsonDocument.Parse(formatted));
            Assert.That(exact, Is.EqualTo("\"a\""));
            AssertException(output, "CBOR_JSON_MAX_OUTPUT_CHARACTERS", "$" );
            AssertException(depth, "CBOR_JSON_MAX_NESTING_DEPTH", "$[0]");
            Assert.That(converter.ConvertToJson(Integer(1), noNesting), Is.EqualTo("1"));
        });
    }

    [Test]
    public void RejectsInvalidOptionsMalformedValuesAndCycles()
    {
        CborJsonConverter converter = new CborJsonConverter();
        CborValue cycle = Array([]);
        cycle.ArrayItems!.Add(cycle);

        CborJsonException? missingValue = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(null!, new CborJsonOptions()));
        CborJsonException? missingOptions = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(Integer(1), null!));
        CborJsonException? invalidPolicy = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(
                Integer(1),
                new CborJsonOptions { Policy = (CborJsonPolicy)int.MaxValue }));
        CborJsonException? invalidDepth = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(
                Integer(1),
                new CborJsonOptions { MaxNestingDepth = -1 }));
        CborJsonException? invalidDuplicate = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(
                Integer(1),
                new CborJsonOptions
                {
                    DuplicatePropertyPolicy = (CborDuplicateKeyPolicy)int.MaxValue
                }));
        CborJsonException? invalidByteConvention = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(
                Integer(1),
                new CborJsonOptions
                {
                    ByteStringConvention = (CborJsonByteStringConvention)int.MaxValue
                }));
        CborJsonException? invalidTagConvention = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(
                Integer(1),
                new CborJsonOptions
                {
                    TagConvention = (CborJsonTagConvention)int.MaxValue
                }));
        CborJsonException? missingBytes = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(
                new CborValue { Kind = CborValueKind.ByteString },
                Options(CborJsonPolicy.AllowLossy)));
        CborJsonException? invalidText = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(Text("\ud800"), new CborJsonOptions()));
        CborJsonException? reservedSimple = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(Simple(24), Options(CborJsonPolicy.AllowLossy)));
        CborJsonException? graph = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(cycle, Options(CborJsonPolicy.PreserveInformation)));

        Assert.Multiple(() =>
        {
            AssertException(missingValue, "CBOR_VALUE_MISSING", "$" );
            AssertException(missingOptions, "CBOR_JSON_OPTIONS_MISSING", "$" );
            AssertException(invalidPolicy, "CBOR_JSON_POLICY_INVALID", "$" );
            AssertException(invalidDepth, "CBOR_JSON_MAX_NESTING_DEPTH_INVALID", "$" );
            AssertException(
                invalidDuplicate,
                "CBOR_JSON_DUPLICATE_PROPERTY_POLICY_INVALID",
                "$" );
            AssertException(
                invalidByteConvention,
                "CBOR_JSON_BYTE_STRING_CONVENTION_INVALID",
                "$" );
            AssertException(
                invalidTagConvention,
                "CBOR_JSON_TAG_CONVENTION_INVALID",
                "$" );
            AssertException(missingBytes, "CBOR_BYTE_STRING_MISSING", "$" );
            AssertException(invalidText, "CBOR_TEXT_INVALID_UTF16", "$" );
            AssertException(reservedSimple, "CBOR_SIMPLE_VALUE_RESERVED", "$" );
            AssertException(graph, "CBOR_JSON_CYCLE", "$[0]");
        });
    }

    private static CborJsonOptions Options(CborJsonPolicy policy)
    {
        return new CborJsonOptions { Policy = policy };
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
        CborJsonException? exception,
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
