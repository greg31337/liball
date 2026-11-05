using token;

namespace token.Tests;

public class JsonObjectTests
{
    [Test]
    public void ReadBuildsAllJsonValueKinds()
    {
        string json = "{\"null\":null,\"false\":false,\"true\":true,\"number\":-1.2300e+04,\"text\":\"hé😀\",\"array\":[1,\"two\"],\"object\":{\"id\":7}}";

        JsonObjectResult result = JsonObject.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Object.Get("null").Kind, Is.EqualTo(TokenJsonValueKind.Null));
            Assert.That(result.Object.Get("false").Kind, Is.EqualTo(TokenJsonValueKind.Boolean));
            Assert.That(result.Object.Get("false").BooleanValue, Is.False);
            Assert.That(result.Object.Get("true").BooleanValue, Is.True);
            Assert.That(result.Object.Get("number").Kind, Is.EqualTo(TokenJsonValueKind.Number));
            Assert.That(result.Object.Get("number").NumberText, Is.EqualTo("-1.2300e+04"));
            Assert.That(result.Object.Get("text").StringValue, Is.EqualTo("hé😀"));
            Assert.That(result.Object.Get("array").ArrayValue, Has.Length.EqualTo(2));
            Assert.That(result.Object.Get("array").ArrayValue[0].NumberText, Is.EqualTo("1"));
            Assert.That(result.Object.Get("array").ArrayValue[1].StringValue, Is.EqualTo("two"));
            Assert.That(result.Object.Get("object").ObjectValue.Get("id").NumberText, Is.EqualTo("7"));
        });
    }

    [Test]
    public void ReadPreservesMemberOrderAndDecodedNames()
    {
        JsonObjectResult result = JsonObject.Read("{\"z\":1,\"\\u0061\":2,\"é\":3}");

        Assert.Multiple(() =>
        {
            Assert.That(result.Object.Members.Select(member => member.Name), Is.EqualTo(new string[] { "z", "a", "é" }));
            Assert.That(result.Object.Get("a").NumberText, Is.EqualTo("2"));
            Assert.That(result.Object.Contains("A"), Is.False);
            Assert.That(result.Object.Contains("é"), Is.True);
            Assert.That(result.Object.Get("missing"), Is.Null);
        });
    }

    [Test]
    public void ReadCreatesModelIndependentOfInputBytesAndJsonDocument()
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes("{\"name\":\"value\",\"nested\":{\"ok\":true}}");

        JsonObjectResult result = JsonObject.Read(utf8);
        Array.Fill(utf8, (byte)0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Object.Get("name").StringValue, Is.EqualTo("value"));
            Assert.That(result.Object.Get("nested").ObjectValue.Get("ok").BooleanValue, Is.True);
        });
    }

    [Test]
    public void ReadPropagatesStrictReaderFailures()
    {
        JsonObjectResult duplicate = JsonObject.Read("{\"a\":1,\"a\":2}");
        JsonObjectResult malformed = JsonObject.Read("{\"a\":}");
        JsonObjectResult tooDeep = JsonObject.Read("{\"a\":{}}", 1);
        JsonObjectResult invalidUtf8 = JsonObject.Read(new byte[] { 0x7B, 0x22, 0x61, 0x22, 0x3A, 0xC0, 0x80, 0x7D });

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Error.Code, Is.EqualTo("duplicate_json_member"));
            Assert.That(malformed.Error.Code, Is.EqualTo("invalid_json"));
            Assert.That(tooDeep.Error.Code, Is.EqualTo("json_depth_exceeded"));
            Assert.That(invalidUtf8.Error.Code, Is.EqualTo("invalid_utf8"));
            Assert.That(duplicate.Object, Is.Null);
        });
    }

    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("\"text\"")]
    [TestCase("1")]
    [TestCase("true")]
    public void ReadRejectsNonObjectRoots(string json)
    {
        JsonObjectResult result = JsonObject.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Object, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("json_root_not_object"));
            Assert.That(result.Error.Position, Is.Zero);
        });
    }

    [Test]
    public void ConstructorsProvideMutableModelContainersWithoutAliasingMemberArray()
    {
        JsonValue value = new JsonValue();
        value.Kind = TokenJsonValueKind.String;
        value.StringValue = "value";
        JsonMember member = new JsonMember("name", value);
        JsonMember[] members = new JsonMember[] { member };

        JsonObject jsonObject = new JsonObject(members);
        members[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(jsonObject.Members[0], Is.SameAs(member));
            Assert.That(jsonObject.Get("name"), Is.SameAs(value));
            Assert.That(JsonObject.CreateEmpty().Members, Is.Empty);
            Assert.That(new JsonValue().Kind, Is.EqualTo(TokenJsonValueKind.Null));
        });
    }

    [Test]
    public void PublicOperationsRejectNullAndInvalidArguments()
    {
        JsonObject jsonObject = JsonObject.CreateEmpty();

        Assert.Multiple(() =>
        {
            Assert.That(() => JsonObject.Read((string)null), Throws.ArgumentNullException);
            Assert.That(() => JsonObject.Read((byte[])null), Throws.ArgumentNullException);
            Assert.That(() => JsonObject.Read("{}", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new JsonObject(null), Throws.ArgumentNullException);
            Assert.That(() => new JsonMember(null, new JsonValue()), Throws.ArgumentNullException);
            Assert.That(() => new JsonMember("name", null), Throws.ArgumentNullException);
            Assert.That(() => jsonObject.Get(null), Throws.ArgumentNullException);
            Assert.That(() => jsonObject.Contains(null), Throws.ArgumentNullException);
        });
    }
}
