using token;

namespace token.Tests;

public class JsonReaderTests
{
    [Test]
    public void ReadParsesObjectsArraysAndPrimitiveValues()
    {
        string json = " {\"text\":\"value\",\"number\":-12.5e+2,\"flags\":[true,false,null]} ";

        JsonReadResult result = token.JsonReader.Read(json);

        using (result.Document)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Error, Is.Null);
                Assert.That(result.Document.RootElement.GetProperty("text").GetString(), Is.EqualTo("value"));
                Assert.That(result.Document.RootElement.GetProperty("number").GetDecimal(), Is.EqualTo(-1250m));
                Assert.That(result.Document.RootElement.GetProperty("flags").GetArrayLength(), Is.EqualTo(3));
            });
        }
    }

    [TestCase("{\"a\":1,\"a\":2}", 7)]
    [TestCase("{\"outer\":{\"a\":1,\"a\":2}}", 16)]
    [TestCase("[{\"a\":1},{\"a\":2,\"a\":3}]", 16)]
    public void ReadRejectsDuplicateMembersAtEveryObjectDepth(string json, int position)
    {
        JsonReadResult result = token.JsonReader.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("duplicate_json_member"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }

    [TestCase("{\"a\":1,\"\\u0061\":2}", 7)]
    [TestCase("{\"/\":1,\"\\/\":2}", 7)]
    [TestCase("{\"😀\":1,\"\\uD83D\\uDE00\":2}", 8)]
    public void ReadDetectsDuplicatesAfterUnescapingNames(string json, int position)
    {
        JsonReadResult result = token.JsonReader.Read(json);

        Assert.That(result.Error.Code, Is.EqualTo("duplicate_json_member"));
        Assert.That(result.Error.Position, Is.EqualTo(position));
    }

    [Test]
    public void EqualNamesInDifferentObjectsAreAllowed()
    {
        JsonReadResult result = token.JsonReader.Read("{\"left\":{\"id\":1},\"right\":{\"id\":2}}");

        using (result.Document)
        {
            Assert.That(result.Success, Is.True);
        }
    }

    [TestCase("", 0)]
    [TestCase("{", 1)]
    [TestCase("[1,]", 3)]
    [TestCase("{\"a\" 1}", 5)]
    [TestCase("{\"a\":01}", 6)]
    [TestCase("{\"a\":1.}", 7)]
    [TestCase("{\"a\":1e}", 7)]
    [TestCase("true false", 5)]
    [TestCase("\"unterminated", 13)]
    [TestCase("\"bad\\x\"", 5)]
    [TestCase("\"bad\\u12xz\"", 8)]
    public void ReadReturnsInvalidJsonForMalformedInput(string json, int position)
    {
        JsonReadResult result = token.JsonReader.Read(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_json"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }

    [Test]
    public void ReadRejectsUnescapedControlsAndUnpairedSurrogates()
    {
        string control = new string(new char[] { '"', 'a', '\u0001', '"' });
        string surrogate = new string(new char[] { '"', '\uD800', '"' });

        JsonReadResult controlResult = token.JsonReader.Read(control);
        JsonReadResult surrogateResult = token.JsonReader.Read(surrogate);

        Assert.Multiple(() =>
        {
            Assert.That(controlResult.Error.Code, Is.EqualTo("invalid_json"));
            Assert.That(controlResult.Error.Position, Is.EqualTo(2));
            Assert.That(surrogateResult.Error.Code, Is.EqualTo("invalid_json"));
            Assert.That(surrogateResult.Error.Position, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReadEnforcesMaximumContainerDepth()
    {
        JsonReadResult allowed = token.JsonReader.Read("{\"a\":[1]}", 2);
        JsonReadResult rejected = token.JsonReader.Read("{\"a\":[1]}", 1);
        JsonReadResult scalar = token.JsonReader.Read("1", 0);

        using (allowed.Document)
        using (scalar.Document)
        {
            Assert.Multiple(() =>
            {
                Assert.That(allowed.Success, Is.True);
                Assert.That(rejected.Success, Is.False);
                Assert.That(rejected.Error.Code, Is.EqualTo("json_depth_exceeded"));
                Assert.That(rejected.Error.Position, Is.EqualTo(5));
                Assert.That(scalar.Success, Is.True);
            });
        }
    }

    [Test]
    public void ReadUtf8BytesRejectsMalformedUtf8BeforeJsonParsing()
    {
        byte[] invalidUtf8 = new byte[] { 0x7B, 0x22, 0x61, 0x22, 0x3A, 0xC0, 0x80, 0x7D };

        JsonReadResult result = token.JsonReader.Read(invalidUtf8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_utf8"));
            Assert.That(result.Error.Position, Is.EqualTo(5));
        });
    }

    [Test]
    public void ReadUtf8BytesParsesUnicodeAndDetectsDuplicates()
    {
        byte[] valid = System.Text.Encoding.UTF8.GetBytes("{\"é\":\"😀\"}");
        byte[] duplicate = System.Text.Encoding.UTF8.GetBytes("{\"é\":1,\"\\u00E9\":2}");

        JsonReadResult validResult = token.JsonReader.Read(valid);
        JsonReadResult duplicateResult = token.JsonReader.Read(duplicate);

        using (validResult.Document)
        {
            Assert.Multiple(() =>
            {
                Assert.That(validResult.Success, Is.True);
                Assert.That(validResult.Document.RootElement.GetProperty("é").GetString(), Is.EqualTo("😀"));
                Assert.That(duplicateResult.Error.Code, Is.EqualTo("duplicate_json_member"));
            });
        }
    }

    [Test]
    public void ReadRejectsInvalidArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => token.JsonReader.Read((string)null), Throws.ArgumentNullException);
            Assert.That(() => token.JsonReader.Read((byte[])null), Throws.ArgumentNullException);
            Assert.That(() => token.JsonReader.Read("{}", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => token.JsonReader.Read(new byte[] { 0x7B, 0x7D }, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }
}
