using System.Text;
using token;

namespace token.Tests;

public class JoseHeaderTests
{
    [Test]
    public void ReadParsesRegisteredAndPrivateParameters()
    {
        string json = "{\"alg\":\"HS256\",\"enc\":\"A128GCM\",\"kid\":\"key-1\""
            + ",\"crit\":[\"urn:example:flag\"],\"b64\":false,\"p2c\":4096"
            + ",\"aud\":[\"one\",\"two\"],\"svt\":[\"token.one\",\"token.two\"]"
            + ",\"jwk\":{\"kty\":\"oct\"}"
            + ",\"urn:example:flag\":{\"enabled\":true,\"level\":2}"
            + ",\"urn:example:null\":null}";

        JoseHeaderResult result = JoseHeader.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Header.Parameters.Members, Has.Length.EqualTo(11));
            Assert.That(result.Header.Get(JoseHeaderNames.Algorithm).StringValue,
                Is.EqualTo(JoseAlgorithms.HmacSha256));
            Assert.That(result.Header.Get(JoseHeaderNames.Critical).ArrayValue[0].StringValue,
                Is.EqualTo("urn:example:flag"));
            Assert.That(result.Header.Get(JoseHeaderNames.Base64UrlEncodePayload).BooleanValue,
                Is.False);
            Assert.That(result.Header.Get(JoseHeaderNames.Pbes2Count).NumberText,
                Is.EqualTo("4096"));
            Assert.That(result.Header.Get(JoseHeaderNames.Audience).ArrayValue
                .Select(value => value.StringValue), Is.EqualTo(new[] { "one", "two" }));
            Assert.That(result.Header.Get(JoseHeaderNames.SignatureValidationToken)
                .ArrayValue.Select(value => value.StringValue),
                Is.EqualTo(new[] { "token.one", "token.two" }));
            Assert.That(result.Header.Get(JoseHeaderNames.Jwk).ObjectValue
                .Get(JwkNames.KeyType).StringValue, Is.EqualTo(TokenKeyTypes.Symmetric));
            Assert.That(result.Header.Get("urn:example:flag").ObjectValue
                .Get("level").NumberText, Is.EqualTo("2"));
            Assert.That(result.Header.Get("urn:example:null").Kind,
                Is.EqualTo(TokenJsonValueKind.Null));
        });
    }

    [Test]
    public void WriteProducesCompactJsonAndRoundTripsEveryValueKind()
    {
        string source = "{ \"kid\" : \"line\\n😀\", \"custom\" : "
            + "[null,true,-1.25e+2,\"text\",{\"x\":1}] , \"alg\" : \"ES256\" }";
        JoseHeader header = JoseHeader.Read(source).Header;

        TokenTextResult written = header.Write();
        JoseHeaderResult reparsed = JoseHeader.Read(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Error, Is.Null);
            Assert.That(written.Text, Is.EqualTo(
                "{\"kid\":\"line\\n😀\",\"custom\":[null,true,-1.25e+2,\"text\",{\"x\":1}],\"alg\":\"ES256\"}"));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(reparsed.Header.Parameters.Members
                .Select(member => member.Name), Is.EqualTo(new[] { "kid", "custom", "alg" }));
            Assert.That(reparsed.Header.Get("custom").ArrayValue[2].NumberText,
                Is.EqualTo("-1.25e+2"));
        });
    }

    [Test]
    public void Utf8ReadAndWriteUseStrictIndependentBytes()
    {
        byte[] source = Encoding.UTF8.GetBytes("{\"typ\":\"JWT\",\"custom\":\"hé😀\"}");

        JoseHeaderResult parsed = JoseHeader.Read(source);
        Array.Fill(source, (byte)0);
        TokenBytesResult written = parsed.Header.WriteUtf8();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(written.Success, Is.True);
            Assert.That(Encoding.UTF8.GetString(written.Bytes),
                Is.EqualTo("{\"typ\":\"JWT\",\"custom\":\"hé😀\"}"));
        });
    }

    [Test]
    public void SetReplacesInPlaceAppendsPrivateParametersAndRemovePreservesOrder()
    {
        JoseHeader header = JoseHeader.Read("{\"alg\":\"HS256\",\"kid\":\"old\"}").Header;
        JsonValue replacement = StringValue("new");
        JsonValue custom = new JsonValue();
        custom.Kind = TokenJsonValueKind.Boolean;
        custom.BooleanValue = true;

        header.Set(JoseHeaderNames.KeyId, replacement);
        header.Set("private", custom);
        bool removed = header.Remove(JoseHeaderNames.Algorithm);
        bool missing = header.Remove("missing");
        TokenTextResult written = JoseHeader.Write(header);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(missing, Is.False);
            Assert.That(header.Get(JoseHeaderNames.KeyId), Is.SameAs(replacement));
            Assert.That(header.Get(JoseHeaderNames.Algorithm), Is.Null);
            Assert.That(written.Text, Is.EqualTo("{\"kid\":\"new\",\"private\":true}"));
        });
    }

    [TestCase("{\"alg\":1}")]
    [TestCase("{\"jwk\":\"not-an-object\"}")]
    [TestCase("{\"epk\":[]}")]
    [TestCase("{\"crit\":\"custom\"}")]
    [TestCase("{\"crit\":[\"custom\",1]}")]
    [TestCase("{\"x5c\":\"certificate\"}")]
    [TestCase("{\"svt\":\"token\"}")]
    [TestCase("{\"aud\":false}")]
    [TestCase("{\"b64\":\"false\"}")]
    [TestCase("{\"p2c\":\"4096\"}")]
    public void ReadRejectsInvalidRegisteredParameterTypes(string json)
    {
        JoseHeaderResult result = JoseHeader.Read(json);

        AssertFailure(result, "invalid_header_parameter_type");
    }

    [Test]
    public void ReadAllowsArbitraryJsonForPrivateParameters()
    {
        JoseHeaderResult result = JoseHeader.Read(
            "{\"private-null\":null,\"private-number\":1.50,"
            + "\"private-array\":[false,{\"nested\":\"yes\"}]}");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Header.Get("private-null").Kind,
                Is.EqualTo(TokenJsonValueKind.Null));
            Assert.That(result.Header.Get("private-number").NumberText,
                Is.EqualTo("1.50"));
            Assert.That(result.Header.Get("private-array").ArrayValue[1]
                .ObjectValue.Get("nested").StringValue, Is.EqualTo("yes"));
        });
    }

    [Test]
    public void ReadPropagatesStrictJsonFailuresAndDepthLimits()
    {
        JoseHeaderResult duplicate = JoseHeader.Read("{\"alg\":\"HS256\",\"alg\":\"RS256\"}");
        JoseHeaderResult malformed = JoseHeader.Read("{\"alg\":}");
        JoseHeaderResult nonObject = JoseHeader.Read("[]");
        JoseHeaderResult tooDeep = JoseHeader.Read("{\"custom\":{}}", 1);
        JoseHeaderResult invalidUtf8 = JoseHeader.Read(
            new byte[] { 0x7B, 0x22, 0x61, 0x22, 0x3A, 0xC0, 0x80, 0x7D });

        Assert.Multiple(() =>
        {
            AssertFailure(duplicate, "duplicate_json_member");
            AssertFailure(malformed, "invalid_json");
            AssertFailure(nonObject, "json_root_not_object");
            AssertFailure(tooDeep, "json_depth_exceeded");
            AssertFailure(invalidUtf8, "invalid_utf8");
        });
    }

    [Test]
    public void WriteRejectsInvalidMutableModels()
    {
        JoseHeader missingParameters = new JoseHeader();
        missingParameters.Parameters = null;
        JoseHeader duplicate = JoseHeader.Create(new JsonObject(new[]
        {
            new JsonMember("private", StringValue("one")),
            new JsonMember("private", StringValue("two"))
        }));
        JoseHeader invalidRegistered = JoseHeader.Create(new JsonObject(new[]
        {
            new JsonMember(JoseHeaderNames.Algorithm, NumberValue("1"))
        }));
        JoseHeader invalidNumber = JoseHeader.Create(new JsonObject(new[]
        {
            new JsonMember("private", NumberValue("01"))
        }));
        JoseHeader invalidString = JoseHeader.Create(new JsonObject(new[]
        {
            new JsonMember("private", StringValue("\uD800"))
        }));

        Assert.Multiple(() =>
        {
            AssertWriteFailure(missingParameters.Write(), "invalid_header");
            AssertWriteFailure(duplicate.Write(), "duplicate_json_member");
            AssertWriteFailure(invalidRegistered.Write(), "invalid_header_parameter_type");
            AssertWriteFailure(invalidNumber.Write(), "invalid_json_number");
            AssertWriteFailure(invalidString.Write(), "invalid_json_string");
        });
    }

    [Test]
    public void FromObjectCopiesTheMemberArray()
    {
        JsonMember member = new JsonMember(JoseHeaderNames.Type, StringValue("JWT"));
        JsonMember[] members = new[] { member };
        JsonObject parameters = new JsonObject(members);

        JoseHeaderResult result = JoseHeader.FromObject(parameters);
        parameters.Members[0] = null;
        members[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Header.Parameters.Members[0], Is.SameAs(member));
            Assert.That(result.Header.Get(JoseHeaderNames.Type).StringValue,
                Is.EqualTo("JWT"));
        });
    }

    [Test]
    public void PublicOperationsRejectNullAndInvalidArguments()
    {
        JoseHeader header = new JoseHeader();

        Assert.Multiple(() =>
        {
            Assert.That(() => JoseHeader.Read((string)null), Throws.ArgumentNullException);
            Assert.That(() => JoseHeader.Read((byte[])null), Throws.ArgumentNullException);
            Assert.That(() => JoseHeader.Read("{}", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => JoseHeader.FromObject(null), Throws.ArgumentNullException);
            Assert.That(() => JoseHeader.Create(null), Throws.ArgumentNullException);
            Assert.That(() => JoseHeader.Write(null), Throws.ArgumentNullException);
            Assert.That(() => JoseHeader.WriteUtf8(null), Throws.ArgumentNullException);
            Assert.That(() => header.Get(null), Throws.ArgumentNullException);
            Assert.That(() => header.Set(null, new JsonValue()), Throws.ArgumentNullException);
            Assert.That(() => header.Set("name", null), Throws.ArgumentNullException);
            Assert.That(() => header.Remove(null), Throws.ArgumentNullException);
        });
    }

    static JsonValue StringValue(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.String;
        result.StringValue = value;
        return result;
    }

    static JsonValue NumberValue(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Number;
        result.NumberText = value;
        return result;
    }

    static void AssertFailure(JoseHeaderResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Header, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }

    static void AssertWriteFailure(TokenTextResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
