using System.Numerics;
using System.Text.Json;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborJsonComprehensiveTests
{
    [Test]
    public void SafeIntegerBoundariesAndAdjacentValuesFollowEveryPolicy()
    {
        BigInteger maximum = new BigInteger(9007199254740991L);
        BigInteger minimum = new BigInteger(-9007199254740991L);
        CborValue safe = Array(Integer(minimum), Integer(maximum));
        CborValue outside = Array(
            Integer(minimum - BigInteger.One),
            Integer(maximum + BigInteger.One));
        CborJsonConverter converter = new CborJsonConverter();

        string strict = converter.ConvertToJson(safe, Options(CborJsonPolicy.Strict));
        CborJsonException strictFailure = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(outside, Options(CborJsonPolicy.Strict)))!;
        string preserved = converter.ConvertToJson(
            outside,
            Options(CborJsonPolicy.PreserveInformation));
        string lossy = converter.ConvertToJson(
            outside,
            Options(CborJsonPolicy.AllowLossy));

        Assert.Multiple(() =>
        {
            Assert.That(strict,
                Is.EqualTo("[-9007199254740991,9007199254740991]"));
            AssertException(strictFailure, "CBOR_JSON_INTEGER_OUT_OF_RANGE", "$[0]");
            Assert.That(preserved, Is.EqualTo(
                "[{\"$cbor\":\"integer\",\"value\":\"-9007199254740992\"},"
                + "{\"$cbor\":\"integer\",\"value\":\"9007199254740992\"}]"));
            Assert.That(lossy,
                Is.EqualTo("[\"-9007199254740992\",\"9007199254740992\"]"));
        });
    }

    [TestCase("", "")]
    [TestCase("_w", "FF")]
    [TestCase("_wA", "FF00")]
    [TestCase("_wAB", "FF0001")]
    public void Base64UrlConventionHandlesEveryTailLength(
        string text,
        string hexadecimal)
    {
        CborJsonOptions options = new CborJsonOptions
        {
            ByteStringConvention = CborJsonByteStringConvention.Base64UrlString
        };
        CborReadResult read = new CborJsonConverter().ConvertFromJson(
            JsonSerializer.Serialize(text),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(read.Value!.Kind, Is.EqualTo(CborValueKind.ByteString));
            Assert.That(read.Value.ByteStringValue,
                Is.EqualTo(Convert.FromHexString(hexadecimal)));
        });
    }

    [TestCase("A")]
    [TestCase("_x")]
    [TestCase("+/8")]
    [TestCase("_w==")]
    [TestCase("_w*")]
    public void Base64UrlConventionRejectsLengthsBitsPaddingAndOtherAlphabets(
        string text)
    {
        CborReadResult result = new CborJsonConverter().ConvertFromJson(
            JsonSerializer.Serialize(text),
            new CborJsonOptions
            {
                ByteStringConvention = CborJsonByteStringConvention.Base64UrlString
            });

        AssertError(result, "CBOR_JSON_BASE64URL_INVALID", "$" );
    }

    [Test]
    public void LossyMapStringifiesEverySupportedScalarKeyAndKeepsLastCollision()
    {
        CborValue map = Map(
            Entry(Text("1"), Text("text")),
            Entry(Integer(1), Text("integer")),
            Entry(Bytes([0xff]), Text("bytes")),
            Entry(Float(1.5), Text("float")),
            Entry(Simple(CborSimpleValues.False), Text("false value")),
            Entry(Simple(CborSimpleValues.True), Text("true value")),
            Entry(Simple(CborSimpleValues.Null), Text("null value")),
            Entry(Simple(CborSimpleValues.Undefined), Text("undefined value")),
            Entry(Simple(42), Text("simple value")));

        string json = new CborJsonConverter().ConvertToJson(
            map,
            Options(CborJsonPolicy.AllowLossy));

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.EnumerateObject().Count(), Is.EqualTo(8));
            Assert.That(root.GetProperty("1").GetString(), Is.EqualTo("integer"));
            Assert.That(root.GetProperty("base64url:_w").GetString(), Is.EqualTo("bytes"));
            Assert.That(root.GetProperty("1.5").GetString(), Is.EqualTo("float"));
            Assert.That(root.GetProperty("false").GetString(), Is.EqualTo("false value"));
            Assert.That(root.GetProperty("true").GetString(), Is.EqualTo("true value"));
            Assert.That(root.GetProperty("null").GetString(), Is.EqualTo("null value"));
            Assert.That(root.GetProperty("undefined").GetString(),
                Is.EqualTo("undefined value"));
            Assert.That(root.GetProperty("simple:42").GetString(),
                Is.EqualTo("simple value"));
        });
    }

    [Test]
    public void LossyMapRejectsComplexReservedAndNonFiniteKeysAtExactPaths()
    {
        CborJsonConverter converter = new CborJsonConverter();
        CborValue[] keys =
        [
            Array(Integer(1)),
            Map(Entry(Text("x"), Integer(1))),
            Tag(1, Integer(1)),
            Float(double.NaN),
            Float(double.PositiveInfinity)
        ];
        string[] codes =
        [
            "CBOR_JSON_NON_TEXT_KEY_UNREPRESENTABLE",
            "CBOR_JSON_NON_TEXT_KEY_UNREPRESENTABLE",
            "CBOR_JSON_NON_TEXT_KEY_UNREPRESENTABLE",
            "CBOR_JSON_NON_TEXT_KEY_UNREPRESENTABLE",
            "CBOR_JSON_NON_TEXT_KEY_UNREPRESENTABLE"
        ];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < keys.Length)
            {
                CborJsonException error = Assert.Throws<CborJsonException>(() =>
                    converter.ConvertToJson(
                        Map(Entry(keys[index], Integer(0))),
                        Options(CborJsonPolicy.AllowLossy)))!;
                AssertException(error, codes[index], "$[0].key");
                index++;
            }
            CborJsonException reserved = Assert.Throws<CborJsonException>(() =>
                converter.ConvertToJson(
                    Map(Entry(Simple(24), Integer(0))),
                    Options(CborJsonPolicy.AllowLossy)))!;
            AssertException(reserved, "CBOR_SIMPLE_VALUE_RESERVED", "$[0].key");
        });
    }

    [Test]
    public void PreservePolicyRecognizesEveryWrapperKind()
    {
        const string json = "["
            + "{\"$cbor\":\"bytes\",\"encoding\":\"base64url\",\"value\":\"_w\"},"
            + "{\"$cbor\":\"integer\",\"value\":\"18446744073709551616\"},"
            + "{\"$cbor\":\"tag\",\"number\":\"18446744073709551615\",\"value\":0},"
            + "{\"$cbor\":\"map\",\"entries\":[[1,\"one\"],[1,\"again\"]]},"
            + "{\"$cbor\":\"undefined\"},"
            + "{\"$cbor\":\"float\",\"value\":\"NaN\"},"
            + "{\"$cbor\":\"float\",\"value\":\"Infinity\"},"
            + "{\"$cbor\":\"float\",\"value\":\"-Infinity\"},"
            + "{\"$cbor\":\"simple\",\"value\":42}]";

        CborReadResult result = new CborJsonConverter().ConvertFromJson(
            json,
            Options(CborJsonPolicy.PreserveInformation));
        List<CborValue> items = result.Value!.ArrayItems!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(items[0].ByteStringValue, Is.EqualTo(new byte[] { 0xff }));
            Assert.That(items[1].IntegerValue,
                Is.EqualTo(BigInteger.Parse("18446744073709551616")));
            Assert.That(items[2].TagNumber, Is.EqualTo(ulong.MaxValue));
            Assert.That(items[3].MapEntries, Has.Count.EqualTo(2));
            Assert.That(items[3].MapEntries![0].Key!.IntegerValue,
                Is.EqualTo(BigInteger.One));
            Assert.That(items[3].MapEntries![1].Key!.IntegerValue,
                Is.EqualTo(BigInteger.One));
            Assert.That(items[4].SimpleValue, Is.EqualTo(CborSimpleValues.Undefined));
            Assert.That(items[5].FloatValue!.IsNaN, Is.True);
            Assert.That(items[6].FloatValue!.Value, Is.EqualTo(double.PositiveInfinity));
            Assert.That(items[7].FloatValue!.Value, Is.EqualTo(double.NegativeInfinity));
            Assert.That(items[8].SimpleValue, Is.EqualTo((byte)42));
        });
    }

    [TestCase("{\"$cbor\":\"bytes\",\"encoding\":\"base64\",\"value\":\"AA\"}",
        "CBOR_JSON_BYTE_WRAPPER_INVALID", "$")]
    [TestCase("{\"$cbor\":\"integer\",\"value\":\"01\"}",
        "CBOR_JSON_INTEGER_TEXT_INVALID", "$")]
    [TestCase("{\"$cbor\":\"integer\",\"value\":\"-0\"}",
        "CBOR_JSON_INTEGER_TEXT_INVALID", "$")]
    [TestCase("{\"$cbor\":\"tag\",\"number\":\"-1\",\"value\":0}",
        "CBOR_JSON_TAG_NUMBER_INVALID", "$")]
    [TestCase("{\"$cbor\":\"tag\",\"number\":\"18446744073709551616\",\"value\":0}",
        "CBOR_JSON_TAG_NUMBER_INVALID", "$")]
    [TestCase("{\"$cbor\":\"map\",\"entries\":[[1]]}",
        "CBOR_JSON_MAP_ENTRY_WRAPPER_INVALID", "$[0]")]
    [TestCase("{\"$cbor\":\"undefined\",\"extra\":0}",
        "CBOR_JSON_UNDEFINED_WRAPPER_INVALID", "$")]
    [TestCase("{\"$cbor\":\"float\",\"value\":\"0\"}",
        "CBOR_JSON_FLOAT_WRAPPER_INVALID", "$")]
    [TestCase("{\"$cbor\":\"simple\",\"value\":1.0}",
        "CBOR_JSON_SIMPLE_WRAPPER_INVALID", "$")]
    [TestCase("{\"$cbor\":\"simple\",\"value\":-1}",
        "CBOR_JSON_SIMPLE_WRAPPER_INVALID", "$")]
    public void MalformedPreservedWrappersReturnSpecificDiagnostics(
        string json,
        string code,
        string path)
    {
        CborReadResult result = new CborJsonConverter().ConvertFromJson(
            json,
            Options(CborJsonPolicy.PreserveInformation));

        AssertError(result, code, path);
    }

    [Test]
    public void EscapedDuplicatePropertyNamesFollowEveryDuplicatePolicy()
    {
        const string json = "{\"name\":1,\"na\\u006de\":2}";
        CborJsonConverter converter = new CborJsonConverter();

        CborReadResult preserved = converter.ConvertFromJson(
            json,
            DuplicateOptions(CborDuplicateKeyPolicy.Preserve));
        CborReadResult rejected = converter.ConvertFromJson(
            json,
            DuplicateOptions(CborDuplicateKeyPolicy.Reject));
        CborReadResult first = converter.ConvertFromJson(
            json,
            DuplicateOptions(CborDuplicateKeyPolicy.KeepFirst));
        CborReadResult last = converter.ConvertFromJson(
            json,
            DuplicateOptions(CborDuplicateKeyPolicy.KeepLast));

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Value!.MapEntries, Has.Count.EqualTo(2));
            AssertError(rejected, "CBOR_JSON_DUPLICATE_PROPERTY", "$.property[1]");
            Assert.That(first.Value!.MapEntries, Has.Count.EqualTo(1));
            Assert.That(first.Value.MapEntries![0].Value!.IntegerValue,
                Is.EqualTo(BigInteger.One));
            Assert.That(last.Value!.MapEntries, Has.Count.EqualTo(1));
            Assert.That(last.Value.MapEntries![0].Value!.IntegerValue,
                Is.EqualTo(new BigInteger(2)));
        });
    }

    [Test]
    public void DeepJsonAndCborGraphsHonorTheExactNestingBoundary()
    {
        const int depth = 64;
        string json = new string('[', depth) + "0" + new string(']', depth);
        CborJsonConverter converter = new CborJsonConverter();
        CborReadResult accepted = converter.ConvertFromJson(
            json,
            new CborJsonOptions { MaxNestingDepth = depth });
        CborReadResult rejected = converter.ConvertFromJson(
            json,
            new CborJsonOptions { MaxNestingDepth = depth - 1 });
        CborValue graph = Integer(0);
        int index = 0;
        while (index < depth)
        {
            graph = Array(graph);
            index++;
        }
        string written = converter.ConvertToJson(
            graph,
            new CborJsonOptions { MaxNestingDepth = depth });
        CborJsonException writeFailure = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(
                graph,
                new CborJsonOptions { MaxNestingDepth = depth - 1 }))!;

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(CborReadStatus.Done));
            AssertError(rejected,
                "CBOR_JSON_MAX_NESTING_DEPTH",
                "$" + string.Concat(Enumerable.Repeat("[0]", depth)));
            Assert.That(written, Is.EqualTo(json));
            AssertException(
                writeFailure,
                "CBOR_JSON_MAX_NESTING_DEPTH",
                "$" + string.Concat(Enumerable.Repeat("[0]", depth)));
        });
    }

    [Test]
    public void InformationPreservingRoundTripRetainsEveryLossyCborDistinction()
    {
        CborValue original = Array(
            Integer(BigInteger.Parse("18446744073709551616")),
            Bytes([0xff, 0x00]),
            Tag(100, Simple(CborSimpleValues.Undefined)),
            Float(double.NaN),
            Float(double.PositiveInfinity),
            Float(double.NegativeInfinity),
            Simple(0),
            Map(
                Entry(Integer(1), Text("first")),
                Entry(Integer(1), Text("second"))));
        CborJsonOptions options = Options(CborJsonPolicy.PreserveInformation);
        CborJsonConverter converter = new CborJsonConverter();

        string json = converter.ConvertToJson(original, options);
        CborReadResult result = converter.ConvertFromJson(json, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(CborValueComparer.AreEqual(original, result.Value), Is.True);
            Assert.DoesNotThrow(() => JsonDocument.Parse(json));
        });
    }

    [Test]
    public void RepeatedAcyclicValuesAreAllowedButCyclesRemainRejected()
    {
        CborValue shared = Array(Integer(1));
        CborValue repeated = Array(shared, shared);
        CborValue cycle = Array();
        cycle.ArrayItems!.Add(cycle);
        CborJsonConverter converter = new CborJsonConverter();

        string json = converter.ConvertToJson(repeated, new CborJsonOptions());
        CborJsonException failure = Assert.Throws<CborJsonException>(() =>
            converter.ConvertToJson(cycle, new CborJsonOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(json, Is.EqualTo("[[1],[1]]"));
            AssertException(failure, "CBOR_JSON_CYCLE", "$[0]");
        });
    }

    private static CborJsonOptions Options(CborJsonPolicy policy)
    {
        return new CborJsonOptions { Policy = policy };
    }

    private static CborJsonOptions DuplicateOptions(CborDuplicateKeyPolicy policy)
    {
        return new CborJsonOptions { DuplicatePropertyPolicy = policy };
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

    private static void AssertException(
        CborJsonException exception,
        string code,
        string path)
    {
        Assert.That(exception.ErrorCode, Is.EqualTo(code));
        Assert.That(exception.ErrorPath, Is.EqualTo(path));
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

    private static CborValue Array(params CborValue[] values)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [.. values]
        };
    }

    private static CborValue Map(params CborMapEntry[] entries)
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
