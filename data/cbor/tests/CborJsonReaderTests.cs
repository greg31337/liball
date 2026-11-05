using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborJsonReaderTests
{
    [Test]
    public void ConvertsTheJsonDataModelAndPreservesNumericKindsWhereAvailable()
    {
        string json = "{\"integer\":1,\"float\":1.0,\"negativeZero\":-0,"
            + "\"big\":18446744073709551616,\"text\":\"A\\n\\uD83D\\uDE00\","
            + "\"values\":[true,false,null]}";

        CborReadResult result = new CborJsonConverter().ConvertFromJson(
            json,
            new CborJsonOptions());

        CborValue root = RequireValue(result);
        CborValue integer = FindMapValue(root, "integer", 0);
        CborValue floating = FindMapValue(root, "float", 0);
        CborValue negativeZero = FindMapValue(root, "negativeZero", 0);
        CborValue big = FindMapValue(root, "big", 0);
        CborValue text = FindMapValue(root, "text", 0);
        CborValue values = FindMapValue(root, "values", 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(json.Length));
            Assert.That(integer.Kind, Is.EqualTo(CborValueKind.UnsignedInteger));
            Assert.That(integer.IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(floating.Kind, Is.EqualTo(CborValueKind.Float));
            Assert.That(floating.FloatValue?.Value, Is.EqualTo(1.0));
            Assert.That(negativeZero.Kind, Is.EqualTo(CborValueKind.Float));
            Assert.That(negativeZero.FloatValue?.RawBits,
                Is.EqualTo(0x8000000000000000UL));
            Assert.That(big.IntegerValue,
                Is.EqualTo(BigInteger.Parse("18446744073709551616")));
            Assert.That(text.TextStringValue, Is.EqualTo("A\n😀"));
            Assert.That(values.ArrayItems, Has.Count.EqualTo(3));
            Assert.That(values.ArrayItems?[0].SimpleValue,
                Is.EqualTo(CborSimpleValues.True));
            Assert.That(values.ArrayItems?[1].SimpleValue,
                Is.EqualTo(CborSimpleValues.False));
            Assert.That(values.ArrayItems?[2].SimpleValue,
                Is.EqualTo(CborSimpleValues.Null));
        });
    }

    [Test]
    public void AppliesEveryDuplicatePropertyPolicy()
    {
        const string json = "{\"x\":1,\"x\":2}";
        CborJsonConverter converter = new CborJsonConverter();
        CborReadResult preserved = converter.ConvertFromJson(
            json,
            Options(CborDuplicateKeyPolicy.Preserve));
        CborReadResult rejected = converter.ConvertFromJson(
            json,
            Options(CborDuplicateKeyPolicy.Reject));
        CborReadResult first = converter.ConvertFromJson(
            json,
            Options(CborDuplicateKeyPolicy.KeepFirst));
        CborReadResult last = converter.ConvertFromJson(
            json,
            Options(CborDuplicateKeyPolicy.KeepLast));

        Assert.Multiple(() =>
        {
            Assert.That(RequireValue(preserved).MapEntries, Has.Count.EqualTo(2));
            Assert.That(FindMapValue(RequireValue(preserved), "x", 0).IntegerValue,
                Is.EqualTo(BigInteger.One));
            Assert.That(FindMapValue(RequireValue(preserved), "x", 1).IntegerValue,
                Is.EqualTo(new BigInteger(2)));
            AssertError(rejected, "CBOR_JSON_DUPLICATE_PROPERTY", "$.property[1]");
            Assert.That(RequireValue(first).MapEntries, Has.Count.EqualTo(1));
            Assert.That(FindMapValue(RequireValue(first), "x", 0).IntegerValue,
                Is.EqualTo(BigInteger.One));
            Assert.That(RequireValue(last).MapEntries, Has.Count.EqualTo(1));
            Assert.That(FindMapValue(RequireValue(last), "x", 0).IntegerValue,
                Is.EqualTo(new BigInteger(2)));
        });
    }

    [Test]
    public void AppliesConfiguredByteStringConventions()
    {
        CborJsonConverter converter = new CborJsonConverter();
        CborReadResult text = converter.ConvertFromJson(
            "\"AQI\"",
            new CborJsonOptions());
        CborReadResult bytes = converter.ConvertFromJson(
            "\"AQI\"",
            new CborJsonOptions
            {
                ByteStringConvention = CborJsonByteStringConvention.Base64UrlString
            });
        CborReadResult invalid = converter.ConvertFromJson(
            "\"A\"",
            new CborJsonOptions
            {
                ByteStringConvention = CborJsonByteStringConvention.Base64UrlString
            });
        const string wrapper = "{\"$cbor\":\"bytes\",\"encoding\":\"base64url\","
            + "\"value\":\"-_8A\"}";
        CborReadResult preserved = converter.ConvertFromJson(
            wrapper,
            new CborJsonOptions { Policy = CborJsonPolicy.PreserveInformation });
        CborReadResult explicitWrapper = converter.ConvertFromJson(
            wrapper,
            new CborJsonOptions
            {
                ByteStringConvention = CborJsonByteStringConvention.PreserveWrapper
            });

        Assert.Multiple(() =>
        {
            Assert.That(RequireValue(text).Kind, Is.EqualTo(CborValueKind.TextString));
            Assert.That(RequireValue(text).TextStringValue, Is.EqualTo("AQI"));
            Assert.That(RequireValue(bytes).ByteStringValue, Is.EqualTo(new byte[] { 1, 2 }));
            AssertError(invalid, "CBOR_JSON_BASE64URL_INVALID", "$" );
            Assert.That(RequireValue(preserved).ByteStringValue,
                Is.EqualTo(new byte[] { 0xfb, 0xff, 0x00 }));
            Assert.That(RequireValue(explicitWrapper).ByteStringValue,
                Is.EqualTo(new byte[] { 0xfb, 0xff, 0x00 }));
        });
    }

    [Test]
    public void AppliesConfiguredTagWrapperConvention()
    {
        const string json = "{\"$cbor\":\"tag\",\"number\":\"18446744073709551615\","
            + "\"value\":\"content\"}";
        CborJsonConverter converter = new CborJsonConverter();
        CborReadResult ordinary = converter.ConvertFromJson(json, new CborJsonOptions());
        CborReadResult explicitTag = converter.ConvertFromJson(
            json,
            new CborJsonOptions { TagConvention = CborJsonTagConvention.PreserveWrapper });
        CborReadResult preserved = converter.ConvertFromJson(
            json,
            new CborJsonOptions { Policy = CborJsonPolicy.PreserveInformation });

        Assert.Multiple(() =>
        {
            Assert.That(RequireValue(ordinary).Kind, Is.EqualTo(CborValueKind.Map));
            Assert.That(RequireValue(explicitTag).Kind, Is.EqualTo(CborValueKind.Tag));
            Assert.That(RequireValue(explicitTag).TagNumber, Is.EqualTo(ulong.MaxValue));
            Assert.That(RequireValue(explicitTag).TaggedValue?.TextStringValue,
                Is.EqualTo("content"));
            Assert.That(RequireValue(preserved).Kind, Is.EqualTo(CborValueKind.Tag));
        });
    }

    [Test]
    public void ReadsInformationPreservingWriterOutputWithoutLosingMapKeysOrValues()
    {
        CborValue original = Map(
        [
            Entry(Bytes([0xfb, 0xff]), Integer(BigInteger.Parse("18446744073709551616"))),
            Entry(Text("duplicate"), Tag(100, Simple(CborSimpleValues.Undefined))),
            Entry(Text("duplicate"), Float(double.NegativeInfinity)),
            Entry(Simple(42), Text("simple key"))
        ]);
        CborJsonOptions options = new CborJsonOptions
        {
            Policy = CborJsonPolicy.PreserveInformation
        };
        CborJsonConverter converter = new CborJsonConverter();

        string json = converter.ConvertToJson(original, options);
        CborReadResult result = converter.ConvertFromJson(json, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(CborValueComparer.AreEqual(original, result.Value), Is.True);
        });
    }

    [Test]
    public void ReportsMalformedJsonAndNumericRangeFailuresWithoutThrowing()
    {
        CborJsonConverter converter = new CborJsonConverter();
        CborReadResult empty = converter.ConvertFromJson("", new CborJsonOptions());
        CborReadResult missingValue = converter.ConvertFromJson(
            "{\"a\":}",
            new CborJsonOptions());
        CborReadResult trailingComma = converter.ConvertFromJson(
            "[1,]",
            new CborJsonOptions());
        CborReadResult invalidUnicode = converter.ConvertFromJson(
            "\"\\uD800\"",
            new CborJsonOptions());
        CborReadResult trailing = converter.ConvertFromJson(
            "null true",
            new CborJsonOptions());
        CborReadResult range = converter.ConvertFromJson(
            "1e400",
            new CborJsonOptions());

        Assert.Multiple(() =>
        {
            AssertError(empty, "CBOR_JSON_UNEXPECTED_END", "$" );
            AssertError(missingValue, "CBOR_JSON_TOKEN_INVALID", "$.property[0]");
            AssertError(trailingComma, "CBOR_JSON_TOKEN_INVALID", "$[1]");
            AssertError(invalidUnicode, "CBOR_JSON_STRING_UNICODE_INVALID", "$" );
            AssertError(trailing, "CBOR_JSON_TRAILING_DATA", "$" );
            AssertError(range, "CBOR_JSON_NUMBER_OUT_OF_RANGE", "$" );
        });
    }

    [Test]
    public void EnforcesInputAndDepthLimitsAtTheirBoundaries()
    {
        CborJsonConverter converter = new CborJsonConverter();
        CborReadResult exactInput = converter.ConvertFromJson(
            "null",
            new CborJsonOptions { MaxInputCharacters = 4 });
        CborReadResult input = converter.ConvertFromJson(
            "null",
            new CborJsonOptions { MaxInputCharacters = 3 });
        CborReadResult scalar = converter.ConvertFromJson(
            "1",
            new CborJsonOptions { MaxNestingDepth = 0 });
        CborReadResult depth = converter.ConvertFromJson(
            "[1]",
            new CborJsonOptions { MaxNestingDepth = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(exactInput.Status, Is.EqualTo(CborReadStatus.Done));
            AssertError(input, "CBOR_JSON_MAX_INPUT_CHARACTERS", "$" );
            Assert.That(scalar.Status, Is.EqualTo(CborReadStatus.Done));
            AssertError(depth, "CBOR_JSON_MAX_NESTING_DEPTH", "$[0]");
        });
    }

    [Test]
    public void RejectsInvalidOptionsAndMalformedPreservedWrappers()
    {
        CborJsonConverter converter = new CborJsonConverter();
        CborReadResult missingText = converter.ConvertFromJson(
            null!,
            new CborJsonOptions());
        CborReadResult missingOptions = converter.ConvertFromJson("null", null!);
        CborReadResult invalidDuplicate = converter.ConvertFromJson(
            "null",
            new CborJsonOptions
            {
                DuplicatePropertyPolicy = (CborDuplicateKeyPolicy)int.MaxValue
            });
        CborReadResult invalidByteConvention = converter.ConvertFromJson(
            "null",
            new CborJsonOptions
            {
                ByteStringConvention = (CborJsonByteStringConvention)int.MaxValue
            });
        CborReadResult invalidTagConvention = converter.ConvertFromJson(
            "null",
            new CborJsonOptions
            {
                TagConvention = (CborJsonTagConvention)int.MaxValue
            });
        CborReadResult malformedWrapper = converter.ConvertFromJson(
            "{\"$cbor\":\"bytes\",\"encoding\":\"base64url\"}",
            new CborJsonOptions { Policy = CborJsonPolicy.PreserveInformation });
        CborReadResult reservedSimple = converter.ConvertFromJson(
            "{\"$cbor\":\"simple\",\"value\":24}",
            new CborJsonOptions { Policy = CborJsonPolicy.PreserveInformation });

        Assert.Multiple(() =>
        {
            AssertError(missingText, "CBOR_JSON_TEXT_MISSING", "$" );
            AssertError(missingOptions, "CBOR_JSON_OPTIONS_MISSING", "$" );
            AssertError(
                invalidDuplicate,
                "CBOR_JSON_DUPLICATE_PROPERTY_POLICY_INVALID",
                "$" );
            AssertError(
                invalidByteConvention,
                "CBOR_JSON_BYTE_STRING_CONVENTION_INVALID",
                "$" );
            AssertError(
                invalidTagConvention,
                "CBOR_JSON_TAG_CONVENTION_INVALID",
                "$" );
            AssertError(malformedWrapper, "CBOR_JSON_BYTE_WRAPPER_INVALID", "$" );
            AssertError(reservedSimple, "CBOR_SIMPLE_VALUE_RESERVED", "$" );
        });
    }

    private static CborJsonOptions Options(CborDuplicateKeyPolicy policy)
    {
        return new CborJsonOptions { DuplicatePropertyPolicy = policy };
    }

    private static CborValue RequireValue(CborReadResult result)
    {
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done), result.ErrorMessage);
        Assert.That(result.Value, Is.Not.Null);
        return result.Value!;
    }

    private static CborValue FindMapValue(CborValue map, string name, int occurrence)
    {
        int found = 0;
        foreach (CborMapEntry entry in map.MapEntries!)
        {
            if (entry.Key?.TextStringValue == name)
            {
                if (found == occurrence)
                {
                    return entry.Value!;
                }
                found++;
            }
        }
        Assert.Fail($"Map property {name} occurrence {occurrence} was not found.");
        return new CborValue();
    }

    private static void AssertError(CborReadResult result, string code, string path)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo(code));
            Assert.That(result.ErrorPath, Is.EqualTo(path));
            Assert.That(result.Value, Is.Null);
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

    private static CborValue Tag(ulong number, CborValue value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = number,
            TaggedValue = value
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
                Width = CborFloatWidth.Double,
                RawBits = BitConverter.DoubleToUInt64Bits(value),
                IsNaN = double.IsNaN(value)
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
}
