using System.Text;
using token;

namespace token.Tests;

public class JwtClaimsTests
{
    [Test]
    public void ReadParsesRegisteredCommonAndPrivateClaims()
    {
        string json = "{\"iss\":\"https://issuer.example\",\"sub\":\"subject\""
            + ",\"aud\":[\"api-one\",\"api-two\"],\"exp\":1700000000.25"
            + ",\"nbf\":1699999900,\"iat\":1699999800,\"jti\":\"id-1\""
            + ",\"azp\":\"client\",\"nonce\":\"nonce-1\",\"auth_time\":1699999700"
            + ",\"acr\":\"urn:acr\",\"amr\":[\"pwd\",\"otp\"]"
            + ",\"at_hash\":\"access-hash\",\"c_hash\":\"code-hash\""
            + ",\"cnf\":{\"jkt\":\"thumbprint\"},\"scope\":\"read write\""
            + ",\"client_id\":\"client\",\"private\":{\"enabled\":true}}";

        JwtClaimsResult result = JwtClaims.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Claims.Values.Members, Has.Length.EqualTo(18));
            Assert.That(result.Claims.Get(JwtClaimNames.Issuer).StringValue,
                Is.EqualTo("https://issuer.example"));
            Assert.That(result.Claims.Get(JwtClaimNames.Audience).ArrayValue
                .Select(value => value.StringValue),
                Is.EqualTo(new[] { "api-one", "api-two" }));
            Assert.That(result.Claims.Get(JwtClaimNames.ExpirationTime).NumberText,
                Is.EqualTo("1700000000.25"));
            Assert.That(result.Claims.Get(JwtClaimNames.AuthenticationMethodsReferences)
                .ArrayValue.Select(value => value.StringValue),
                Is.EqualTo(new[] { "pwd", "otp" }));
            Assert.That(result.Claims.Get(JwtClaimNames.Confirmation).ObjectValue
                .Get("jkt").StringValue, Is.EqualTo("thumbprint"));
            Assert.That(result.Claims.Get("private").ObjectValue
                .Get("enabled").BooleanValue, Is.True);
        });
    }

    [Test]
    public void ReadAcceptsBothAudienceFormsAndArbitraryPrivateValues()
    {
        JwtClaimsResult single = JwtClaims.Read(
            "{\"aud\":\"api\",\"private-null\":null,\"private-array\":[1,false]}");
        JwtClaimsResult multiple = JwtClaims.Read("{\"aud\":[\"api\",\"admin\"]}");

        Assert.Multiple(() =>
        {
            Assert.That(single.Success, Is.True);
            Assert.That(single.Claims.Get(JwtClaimNames.Audience).StringValue,
                Is.EqualTo("api"));
            Assert.That(single.Claims.Get("private-null").Kind,
                Is.EqualTo(TokenJsonValueKind.Null));
            Assert.That(single.Claims.Get("private-array").ArrayValue[0].NumberText,
                Is.EqualTo("1"));
            Assert.That(multiple.Success, Is.True);
            Assert.That(multiple.Claims.Get(JwtClaimNames.Audience).ArrayValue,
                Has.Length.EqualTo(2));
        });
    }

    [TestCase("{\"iss\":1}")]
    [TestCase("{\"sub\":false}")]
    [TestCase("{\"aud\":1}")]
    [TestCase("{\"aud\":[\"api\",1]}")]
    [TestCase("{\"exp\":\"1700000000\"}")]
    [TestCase("{\"nbf\":null}")]
    [TestCase("{\"iat\":true}")]
    [TestCase("{\"jti\":[]}")]
    [TestCase("{\"azp\":1}")]
    [TestCase("{\"nonce\":null}")]
    [TestCase("{\"auth_time\":\"1699999700\"}")]
    [TestCase("{\"acr\":{}}")]
    [TestCase("{\"amr\":\"pwd\"}")]
    [TestCase("{\"amr\":[\"pwd\",false]}")]
    [TestCase("{\"at_hash\":1}")]
    [TestCase("{\"c_hash\":1}")]
    [TestCase("{\"cnf\":\"thumbprint\"}")]
    [TestCase("{\"scope\":[\"read\"]}")]
    [TestCase("{\"client_id\":1}")]
    public void ReadRejectsInvalidRegisteredClaimTypes(string json)
    {
        AssertFailure(JwtClaims.Read(json), "invalid_claim_type");
    }

    [Test]
    public void WriteProducesCompactJsonAndRoundTripsEveryJsonKind()
    {
        string source = "{ \"iss\" : \"line\\n😀\", \"private\" : "
            + "[null,true,-1.25e+2,\"text\",{\"x\":1}], \"aud\" : \"api\" }";
        JwtClaims claims = JwtClaims.Read(source).Claims;

        TokenTextResult written = claims.Write();
        JwtClaimsResult reparsed = JwtClaims.Read(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Error, Is.Null);
            Assert.That(written.Text, Is.EqualTo(
                "{\"iss\":\"line\\n😀\",\"private\":[null,true,-1.25e+2,\"text\",{\"x\":1}],\"aud\":\"api\"}"));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(reparsed.Claims.Values.Members.Select(member => member.Name),
                Is.EqualTo(new[] { "iss", "private", "aud" }));
            Assert.That(reparsed.Claims.Get("private").ArrayValue[2].NumberText,
                Is.EqualTo("-1.25e+2"));
        });
    }

    [Test]
    public void Utf8ReadAndWriteUseIndependentInputBytes()
    {
        byte[] source = Encoding.UTF8.GetBytes("{\"sub\":\"hé😀\",\"exp\":12}");

        JwtClaimsResult parsed = JwtClaims.Read(source);
        Array.Fill(source, (byte)0);
        TokenBytesResult written = parsed.Claims.WriteUtf8();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(written.Success, Is.True);
            Assert.That(Encoding.UTF8.GetString(written.Bytes),
                Is.EqualTo("{\"sub\":\"hé😀\",\"exp\":12}"));
        });
    }

    [Test]
    public void SetReplacesInPlaceAppendsClaimsAndRemovePreservesOrder()
    {
        JwtClaims claims = JwtClaims.Read("{\"iss\":\"old\",\"sub\":\"subject\"}").Claims;
        JsonValue replacement = StringValue("new");
        JsonValue custom = BooleanValue(true);

        claims.Set(JwtClaimNames.Issuer, replacement);
        claims.Set("private", custom);
        bool removed = claims.Remove(JwtClaimNames.Subject);
        bool missing = claims.Remove("missing");
        TokenTextResult written = JwtClaims.Write(claims);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(missing, Is.False);
            Assert.That(claims.Get(JwtClaimNames.Issuer), Is.SameAs(replacement));
            Assert.That(claims.Get(JwtClaimNames.Subject), Is.Null);
            Assert.That(written.Text, Is.EqualTo("{\"iss\":\"new\",\"private\":true}"));
        });
    }

    [Test]
    public void FromObjectCopiesTheMemberArray()
    {
        JsonMember member = new JsonMember(JwtClaimNames.Issuer, StringValue("issuer"));
        JsonMember[] members = new[] { member };
        JsonObject values = new JsonObject(members);

        JwtClaimsResult result = JwtClaims.FromObject(values);
        values.Members[0] = null;
        members[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Claims.Values.Members[0], Is.SameAs(member));
            Assert.That(result.Claims.Get(JwtClaimNames.Issuer).StringValue,
                Is.EqualTo("issuer"));
        });
    }

    [Test]
    public void ReadPropagatesStrictJsonFailuresAndDepthLimits()
    {
        JwtClaimsResult duplicate = JwtClaims.Read("{\"iss\":\"one\",\"iss\":\"two\"}");
        JwtClaimsResult malformed = JwtClaims.Read("{\"iss\":}");
        JwtClaimsResult nonObject = JwtClaims.Read("[]");
        JwtClaimsResult tooDeep = JwtClaims.Read("{\"private\":{}}", 1);
        JwtClaimsResult invalidUtf8 = JwtClaims.Read(
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
    public void PolicyOverloadsEnforceUtf8SizeAndDepthForReadAndWrite()
    {
        string json = "{\"iss\":\"é\",\"private\":{\"nested\":true}}";
        TokenPolicy exact = new TokenPolicy();
        exact.MaximumTokenSizeInBytes = Encoding.UTF8.GetByteCount(json);
        TokenPolicy small = new TokenPolicy();
        small.MaximumTokenSizeInBytes = exact.MaximumTokenSizeInBytes - 1;
        TokenPolicy shallow = new TokenPolicy();
        shallow.MaximumJsonDepth = 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumJsonDepth = 0;
        JwtClaims claims = JwtClaims.Read(json).Claims;

        JwtClaimsResult exactText = JwtClaims.Read(json, exact);
        JwtClaimsResult exactBytes = JwtClaims.Read(Encoding.UTF8.GetBytes(json), exact);
        JwtClaimsResult smallText = JwtClaims.Read(json, small);
        JwtClaimsResult shallowText = JwtClaims.Read(json, shallow);
        JwtClaimsResult invalidPolicy = JwtClaims.Read(json, invalid);
        TokenTextResult exactWrite = claims.WriteWithPolicy(exact);
        TokenTextResult smallWrite = claims.WriteWithPolicy(small);
        TokenBytesResult shallowWrite = claims.WriteUtf8WithPolicy(shallow);

        Assert.Multiple(() =>
        {
            Assert.That(exactText.Success, Is.True);
            Assert.That(exactBytes.Success, Is.True);
            AssertFailure(smallText, "token_too_large");
            AssertFailure(shallowText, "json_depth_exceeded");
            AssertFailure(invalidPolicy, "invalid_policy");
            Assert.That(exactWrite.Success, Is.True);
            AssertWriteFailure(smallWrite, "token_too_large");
            AssertBytesFailure(shallowWrite, "json_depth_exceeded");
        });
    }

    [Test]
    public void WriteRejectsInvalidMutableModels()
    {
        JwtClaims missingValues = new JwtClaims();
        missingValues.Values = null;
        JwtClaims duplicate = JwtClaims.Create(new JsonObject(new[]
        {
            new JsonMember("private", StringValue("one")),
            new JsonMember("private", StringValue("two"))
        }));
        JwtClaims invalidRegistered = JwtClaims.Create(new JsonObject(new[]
        {
            new JsonMember(JwtClaimNames.ExpirationTime, StringValue("1"))
        }));
        JwtClaims invalidNumber = JwtClaims.Create(new JsonObject(new[]
        {
            new JsonMember("private", NumberValue("01"))
        }));
        JwtClaims invalidString = JwtClaims.Create(new JsonObject(new[]
        {
            new JsonMember("private", StringValue("\uD800"))
        }));
        JsonObject nestedDuplicateObject = new JsonObject(new[]
        {
            new JsonMember("x", NumberValue("1")),
            new JsonMember("x", NumberValue("2"))
        });
        JwtClaims nestedDuplicate = JwtClaims.Create(new JsonObject(new[]
        {
            new JsonMember("private", ObjectValue(nestedDuplicateObject))
        }));

        Assert.Multiple(() =>
        {
            AssertWriteFailure(missingValues.Write(), "invalid_claim_set");
            AssertWriteFailure(duplicate.Write(), "duplicate_json_member");
            AssertWriteFailure(invalidRegistered.Write(), "invalid_claim_type");
            AssertWriteFailure(invalidNumber.Write(), "invalid_json_number");
            AssertWriteFailure(invalidString.Write(), "invalid_json_string");
            AssertWriteFailure(nestedDuplicate.Write(), "duplicate_json_member");
        });
    }

    [Test]
    public void PublicOperationsRejectNullAndInvalidArguments()
    {
        JwtClaims claims = new JwtClaims();

        Assert.Multiple(() =>
        {
            Assert.That(() => JwtClaims.Read((string)null), Throws.ArgumentNullException);
            Assert.That(() => JwtClaims.Read((byte[])null), Throws.ArgumentNullException);
            Assert.That(() => JwtClaims.Read("{}", -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => JwtClaims.Read("{}", (TokenPolicy)null),
                Throws.ArgumentNullException);
            Assert.That(() => JwtClaims.Read((string)null, new TokenPolicy()),
                Throws.ArgumentNullException);
            Assert.That(() => JwtClaims.Read((byte[])null, new TokenPolicy()),
                Throws.ArgumentNullException);
            Assert.That(() => JwtClaims.FromObject(null), Throws.ArgumentNullException);
            Assert.That(() => JwtClaims.Create(null), Throws.ArgumentNullException);
            Assert.That(() => JwtClaims.Write(null), Throws.ArgumentNullException);
            Assert.That(() => JwtClaims.WriteUtf8(null), Throws.ArgumentNullException);
            Assert.That(() => claims.WriteWithPolicy(null), Throws.ArgumentNullException);
            Assert.That(() => claims.WriteUtf8WithPolicy(null), Throws.ArgumentNullException);
            Assert.That(() => claims.Get(null), Throws.ArgumentNullException);
            Assert.That(() => claims.Set(null, new JsonValue()), Throws.ArgumentNullException);
            Assert.That(() => claims.Set("name", null), Throws.ArgumentNullException);
            Assert.That(() => claims.Remove(null), Throws.ArgumentNullException);
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

    static JsonValue BooleanValue(bool value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Boolean;
        result.BooleanValue = value;
        return result;
    }

    static JsonValue ObjectValue(JsonObject value)
    {
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Object;
        result.ObjectValue = value;
        return result;
    }

    static void AssertFailure(JwtClaimsResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Claims, Is.Null);
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

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
