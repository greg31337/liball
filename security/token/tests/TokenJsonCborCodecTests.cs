using System.Text;
using token;

namespace token.Tests;

public class TokenJsonCborCodecTests
{
    [Test]
    public void JsonCodecReadsEveryJsonValueKind()
    {
        ITokenJsonCodec codec = new TokenJsonCodec();

        JsonValueResult result = codec.ReadJsonText(
            "{\"null\":null,\"boolean\":true,\"number\":-1.25e2,\"string\":\"hé😀\",\"array\":[false],\"object\":{}}",
            256,
            4);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value!.Kind, Is.EqualTo(TokenJsonValueKind.Object));
        JsonMember[] members = result.Value.ObjectValue!.Members;
        Assert.Multiple(() =>
        {
            Assert.That(members[0].Value!.Kind, Is.EqualTo(TokenJsonValueKind.Null));
            Assert.That(members[1].Value!.BooleanValue, Is.True);
            Assert.That(members[2].Value!.NumberText, Is.EqualTo("-1.25e2"));
            Assert.That(members[3].Value!.StringValue, Is.EqualTo("hé😀"));
            Assert.That(members[4].Value!.ArrayValue![0].BooleanValue, Is.False);
            Assert.That(members[5].Value!.ObjectValue!.Members, Is.Empty);
        });
    }

    [Test]
    public void JsonCodecReadsStrictUtf8AndRejectsDuplicateMembers()
    {
        ITokenJsonCodec codec = new TokenJsonCodec();

        JsonValueResult valid = codec.ReadJsonUtf8(Encoding.UTF8.GetBytes("[\"hé\"]"), 16, 2);
        JsonValueResult duplicate = codec.ReadJsonText("{\"a\":1,\"a\":2}", 32, 2);
        JsonValueResult invalidUtf8 = codec.ReadJsonUtf8(new byte[] { 0x22, 0xC0, 0x80, 0x22 }, 8, 1);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Value!.ArrayValue![0].StringValue, Is.EqualTo("hé"));
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Error!.Code, Is.EqualTo("duplicate_json_member"));
            Assert.That(invalidUtf8.Success, Is.False);
            Assert.That(invalidUtf8.Error!.Code, Is.EqualTo("invalid_utf8"));
        });
    }

    [Test]
    public void JsonCodecWritesCompactDeterministicJsonRecursively()
    {
        ITokenJsonCodec codec = new TokenJsonCodec();
        JsonValue root = JsonObjectValue(
            new JsonMember("z", JsonString("line\n")),
            new JsonMember("a", JsonObjectValue(
                new JsonMember("two", JsonNumber("2.0")),
                new JsonMember("one", JsonBoolean(true)))),
            new JsonMember("items", JsonArray(new JsonValue(), JsonString("é"))));

        TokenTextResult text = codec.WriteJsonText(root, 128, 3);
        TokenBytesResult utf8 = codec.WriteJsonUtf8(root, 128, 3);

        Assert.Multiple(() =>
        {
            Assert.That(text.Success, Is.True);
            Assert.That(text.Text, Is.EqualTo("{\"a\":{\"one\":true,\"two\":2.0},\"items\":[null,\"é\"],\"z\":\"line\\n\"}"));
            Assert.That(utf8.Success, Is.True);
            Assert.That(utf8.Bytes, Is.EqualTo(Encoding.UTF8.GetBytes(text.Text!)));
        });
    }

    [Test]
    public void JsonCodecEnforcesInputAndOutputLimits()
    {
        ITokenJsonCodec codec = new TokenJsonCodec();
        JsonValue nested = JsonArray(JsonArray(new JsonValue()));

        JsonValueResult readSize = codec.ReadJsonText("\"é\"", 3, 1);
        JsonValueResult readDepth = codec.ReadJsonText("[[null]]", 16, 1);
        TokenTextResult writeSize = codec.WriteJsonText(JsonString("é"), 3, 1);
        TokenTextResult writeDepth = codec.WriteJsonText(nested, 16, 1);

        Assert.Multiple(() =>
        {
            Assert.That(readSize.Error!.Code, Is.EqualTo("json_size_exceeded"));
            Assert.That(readDepth.Error!.Code, Is.EqualTo("json_depth_exceeded"));
            Assert.That(writeSize.Error!.Code, Is.EqualTo("json_size_exceeded"));
            Assert.That(writeDepth.Error!.Code, Is.EqualTo("json_depth_exceeded"));
        });
    }

    [Test]
    public void JsonCodecRejectsInvalidObjectModels()
    {
        ITokenJsonCodec codec = new TokenJsonCodec();
        JsonValue duplicate = JsonObjectValue(
            new JsonMember("a", new JsonValue()),
            new JsonMember("a", JsonBoolean(true)));
        JsonValue invalidNumber = JsonNumber(" 1 ");
        JsonValue missingArray = new JsonValue();
        missingArray.Kind = TokenJsonValueKind.Array;

        Assert.Multiple(() =>
        {
            Assert.That(codec.WriteJsonText(duplicate, 64, 2).Error!.Code, Is.EqualTo("duplicate_json_member"));
            Assert.That(codec.WriteJsonText(invalidNumber, 64, 1).Error!.Code, Is.EqualTo("invalid_json_value"));
            Assert.That(codec.WriteJsonText(missingArray, 64, 1).Error!.Code, Is.EqualTo("invalid_json_value"));
        });
    }

    [Test]
    public void JsonCodecWritesAndRoundTripsEveryValueKindAtExactLimits()
    {
        ITokenJsonCodec codec = new TokenJsonCodec();
        JsonValue value = JsonArray(
            new JsonValue(),
            JsonBoolean(false),
            JsonNumber("-0.125e+2"),
            JsonString("é"),
            JsonArray(),
            JsonObjectValue());
        const string expected = "[null,false,-0.125e+2,\"é\",[],{}]";
        int exactLength = Encoding.UTF8.GetByteCount(expected);

        TokenTextResult written = codec.WriteJsonText(value, exactLength, 2);
        JsonValueResult read = codec.ReadJsonUtf8(
            Encoding.UTF8.GetBytes(expected),
            exactLength,
            2);
        TokenTextResult oneByteShort = codec.WriteJsonText(value, exactLength - 1, 2);
        TokenTextResult scalarAtZeroDepth = codec.WriteJsonText(new JsonValue(), 4, 0);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Text, Is.EqualTo(expected));
            Assert.That(read.Success, Is.True);
            Assert.That(read.Value!.ArrayValue, Has.Length.EqualTo(6));
            Assert.That(oneByteShort.Error!.Code, Is.EqualTo("json_size_exceeded"));
            Assert.That(scalarAtZeroDepth.Text, Is.EqualTo("null"));
        });
    }

    [Test]
    public void JsonCodecRejectsMalformedDocumentsAndSortsUnicodeNamesDeterministically()
    {
        ITokenJsonCodec codec = new TokenJsonCodec();
        JsonValue value = JsonObjectValue(
            new JsonMember("\U00010000", JsonNumber("2")),
            new JsonMember("\uE000", JsonNumber("1")));

        JsonValueResult truncated = codec.ReadJsonText("{\"a\":", 8, 2);
        JsonValueResult trailing = codec.ReadJsonText("null true", 9, 1);
        TokenTextResult deterministic = codec.WriteJsonText(value, 64, 1);

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Error!.Code, Is.EqualTo("invalid_json"));
            Assert.That(trailing.Error!.Code, Is.EqualTo("invalid_json"));
            Assert.That(deterministic.Text, Is.EqualTo("{\"\uE000\":1,\"\U00010000\":2}"));
        });
    }

    [Test]
    public void CborCodecRoundTripsEverySupportedValueKindAndTag()
    {
        ITokenCborCodec codec = new TokenCborCodec();
        CborItem tagged = CborItemValue(TokenCborValueKind.Tag);
        tagged.TagValue = 18;
        tagged.TaggedValue = CborText("payload");
        CborItem floatingPoint = CborItemValue(TokenCborValueKind.FloatingPoint);
        floatingPoint.FloatingPointValue = 1.5;
        floatingPoint.FloatingPointWidth = 4;
        CborItem root = CborArray(
            CborUnsigned(24),
            CborNegative(0),
            CborBytes(1, 2),
            CborText("hé"),
            CborBoolean(true),
            CborItemValue(TokenCborValueKind.Null),
            CborItemValue(TokenCborValueKind.Undefined),
            CborSimple(16),
            floatingPoint,
            tagged);

        TokenBytesResult written = codec.WriteCbor(root, 128, 3);
        CborReadResult read = codec.ReadCbor(written.Bytes!, 128, 3);

        Assert.That(read.Success, Is.True);
        CborItem[] values = read.Item!.ArrayValue!;
        Assert.Multiple(() =>
        {
            Assert.That(values.Select(value => value.Kind), Is.EqualTo(new[]
            {
                TokenCborValueKind.UnsignedInteger,
                TokenCborValueKind.NegativeInteger,
                TokenCborValueKind.ByteString,
                TokenCborValueKind.TextString,
                TokenCborValueKind.Boolean,
                TokenCborValueKind.Null,
                TokenCborValueKind.Undefined,
                TokenCborValueKind.Simple,
                TokenCborValueKind.FloatingPoint,
                TokenCborValueKind.Tag
            }));
            Assert.That(values[2].Bytes, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(values[3].Text, Is.EqualTo("hé"));
            Assert.That(values[8].FloatingPointValue, Is.EqualTo(1.5));
            Assert.That(values[9].TagValue, Is.EqualTo(18));
            Assert.That(values[9].TaggedValue!.Text, Is.EqualTo("payload"));
        });
    }

    [Test]
    public void CborCodecRejectsDuplicateMapKeysAndTrailingData()
    {
        ITokenCborCodec codec = new TokenCborCodec();

        CborReadResult duplicate = codec.ReadCbor(new byte[] { 0xA2, 0x01, 0x00, 0x01, 0x01 }, 5, 2);
        CborReadResult trailing = codec.ReadCbor(new byte[] { 0x01, 0x02 }, 2, 1);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Error!.Code, Is.EqualTo("duplicate_cbor_map_key"));
            Assert.That(trailing.Success, Is.False);
            Assert.That(trailing.Error!.Code, Is.EqualTo("trailing_cbor_data"));
            Assert.That(trailing.Error.Position, Is.EqualTo(1));
        });
    }

    [Test]
    public void CborCodecWritesAndSortsMapsInDeterministicKeyOrderWithoutMutation()
    {
        ITokenCborCodec codec = new TokenCborCodec();
        CborMapEntry textEntry = new CborMapEntry(CborText("a"), CborItemValue(TokenCborValueKind.Null));
        CborMapEntry integerEntry = new CborMapEntry(CborUnsigned(10), CborItemValue(TokenCborValueKind.Null));
        CborMapEntry[] entries = new[] { textEntry, integerEntry };
        CborItem map = CborItemValue(TokenCborValueKind.Map);
        map.MapValue = entries;

        TokenBytesResult ordinary = codec.WriteCbor(map, 16, 2);
        TokenBytesResult deterministic = codec.WriteDeterministicCbor(map, 16, 2);
        CborMapSortResult sorted = codec.SortCborMap(entries, 8, 2);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.Bytes, Is.EqualTo(new byte[] { 0xA2, 0x61, 0x61, 0xF6, 0x0A, 0xF6 }));
            Assert.That(deterministic.Bytes, Is.EqualTo(new byte[] { 0xA2, 0x0A, 0xF6, 0x61, 0x61, 0xF6 }));
            Assert.That(sorted.Success, Is.True);
            Assert.That(sorted.Entries![0], Is.SameAs(integerEntry));
            Assert.That(entries[0], Is.SameAs(textEntry));
        });
    }

    [Test]
    public void CborCodecEnforcesDepthAndSizeLimits()
    {
        ITokenCborCodec codec = new TokenCborCodec();
        CborItem nested = CborArray(CborArray(CborUnsigned(1)));

        CborReadResult readSize = codec.ReadCbor(new byte[] { 0x18, 0x18 }, 1, 1);
        CborReadResult readDepth = codec.ReadCbor(new byte[] { 0x81, 0x81, 0x01 }, 3, 1);
        TokenBytesResult writeSize = codec.WriteCbor(CborUnsigned(24), 1, 1);
        TokenBytesResult writeDepth = codec.WriteDeterministicCbor(nested, 8, 1);

        Assert.Multiple(() =>
        {
            Assert.That(readSize.Error!.Code, Is.EqualTo("cbor_size_exceeded"));
            Assert.That(readDepth.Error!.Code, Is.EqualTo("cbor_depth_exceeded"));
            Assert.That(writeSize.Error!.Code, Is.EqualTo("buffer_limit_exceeded"));
            Assert.That(writeDepth.Error!.Code, Is.EqualTo("cbor_depth_exceeded"));
        });
    }

    [Test]
    public void CborCodecUsesPreferredIntegerBoundariesAndExactLimits()
    {
        ITokenCborCodec codec = new TokenCborCodec();
        CborItem values = CborArray(
            CborUnsigned(23),
            CborUnsigned(24),
            CborUnsigned(255),
            CborUnsigned(256),
            CborNegative(23),
            CborNegative(24));
        byte[] expected = new byte[]
        {
            0x86, 0x17, 0x18, 0x18, 0x18, 0xFF, 0x19, 0x01, 0x00, 0x37, 0x38, 0x18
        };

        TokenBytesResult exact = codec.WriteCbor(values, expected.Length, 1);
        CborReadResult read = codec.ReadCbor(expected, expected.Length, 1);
        TokenBytesResult shortLimit = codec.WriteCbor(values, expected.Length - 1, 1);
        CborReadResult scalarAtZeroDepth = codec.ReadCbor(new byte[] { 0x00 }, 1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Bytes, Is.EqualTo(expected));
            Assert.That(read.Success, Is.True);
            Assert.That(read.Item!.ArrayValue, Has.Length.EqualTo(6));
            Assert.That(shortLimit.Error!.Code, Is.EqualTo("buffer_limit_exceeded"));
            Assert.That(scalarAtZeroDepth.Success, Is.True);
        });
    }

    [Test]
    public void CborCodecRejectsMalformedDataAndEquivalentDuplicateKeys()
    {
        ITokenCborCodec codec = new TokenCborCodec();
        CborReadResult truncated = codec.ReadCbor(new byte[] { 0x19, 0x01 }, 2, 0);
        CborReadResult invalidUtf8 = codec.ReadCbor(new byte[] { 0x61, 0xFF }, 2, 0);
        CborReadResult equivalentKeys = codec.ReadCbor(
            new byte[] { 0xA2, 0x01, 0x00, 0x18, 0x01, 0x01 },
            6,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Error!.Code, Is.EqualTo("unexpected_end_of_cbor"));
            Assert.That(invalidUtf8.Error!.Code, Is.EqualTo("invalid_cbor_utf8"));
            Assert.That(equivalentKeys.Error!.Code, Is.EqualTo("duplicate_cbor_map_key"));
        });
    }

    [Test]
    public void CodecServicesRejectNullValuesAndNegativeLimits()
    {
        ITokenJsonCodec json = new TokenJsonCodec();
        ITokenCborCodec cbor = new TokenCborCodec();

        Assert.Multiple(() =>
        {
            Assert.That(() => json.ReadJsonText(null!, 1, 1), Throws.ArgumentNullException);
            Assert.That(() => json.ReadJsonUtf8(null!, 1, 1), Throws.ArgumentNullException);
            Assert.That(() => json.WriteJsonText(null!, 1, 1), Throws.ArgumentNullException);
            Assert.That(() => json.ReadJsonText("null", -1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => json.ReadJsonText("null", 4, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => cbor.ReadCbor(null!, 1, 1), Throws.ArgumentNullException);
            Assert.That(() => cbor.WriteCbor(null!, 1, 1), Throws.ArgumentNullException);
            Assert.That(() => cbor.SortCborMap(null!, 1, 1), Throws.ArgumentNullException);
            Assert.That(() => cbor.ReadCbor(new byte[] { 0 }, -1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => cbor.ReadCbor(new byte[] { 0 }, 1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    static JsonValue JsonString(string text)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.String;
        result.StringValue = text;
        return result;
    }

    static JsonValue JsonNumber(string text)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Number;
        result.NumberText = text;
        return result;
    }

    static JsonValue JsonBoolean(bool value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Boolean;
        result.BooleanValue = value;
        return result;
    }

    static JsonValue JsonArray(params JsonValue[] values)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Array;
        result.ArrayValue = values;
        return result;
    }

    static JsonValue JsonObjectValue(params JsonMember[] members)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Object;
        result.ObjectValue = new JsonObject(members);
        return result;
    }

    static CborItem CborItemValue(TokenCborValueKind kind)
    {
        CborItem result = new CborItem();
        result.Kind = kind;
        return result;
    }

    static CborItem CborUnsigned(ulong value)
    {
        CborItem result = CborItemValue(TokenCborValueKind.UnsignedInteger);
        result.IntegerArgument = value;
        return result;
    }

    static CborItem CborNegative(ulong argument)
    {
        CborItem result = CborItemValue(TokenCborValueKind.NegativeInteger);
        result.IntegerArgument = argument;
        return result;
    }

    static CborItem CborBytes(params byte[] bytes)
    {
        CborItem result = CborItemValue(TokenCborValueKind.ByteString);
        result.Bytes = bytes;
        return result;
    }

    static CborItem CborText(string text)
    {
        CborItem result = CborItemValue(TokenCborValueKind.TextString);
        result.Text = text;
        return result;
    }

    static CborItem CborBoolean(bool value)
    {
        CborItem result = CborItemValue(TokenCborValueKind.Boolean);
        result.BooleanValue = value;
        return result;
    }

    static CborItem CborSimple(byte value)
    {
        CborItem result = CborItemValue(TokenCborValueKind.Simple);
        result.SimpleValue = value;
        return result;
    }

    static CborItem CborArray(params CborItem[] items)
    {
        CborItem result = CborItemValue(TokenCborValueKind.Array);
        result.ArrayValue = items;
        return result;
    }
}
