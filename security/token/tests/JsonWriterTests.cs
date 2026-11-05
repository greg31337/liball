using token;

namespace token.Tests;

public class JsonWriterTests
{
    [Test]
    public void WriteStringObjectUsesJwkThumbprintMemberOrder()
    {
        string[] names = new string[] { "n", "kty", "e" };
        string[] values = new string[] { "modulus", "RSA", "AQAB" };

        TokenTextResult result = token.JsonWriter.WriteStringObject(names, values);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Text, Is.EqualTo("{\"e\":\"AQAB\",\"kty\":\"RSA\",\"n\":\"modulus\"}"));
        });
    }

    [Test]
    public void WriteStringObjectProducesIdenticalOutputForEveryInputOrder()
    {
        TokenTextResult first = token.JsonWriter.WriteStringObject(
            new string[] { "y", "crv", "kty", "x" },
            new string[] { "why", "P-256", "EC", "ex" });
        TokenTextResult second = token.JsonWriter.WriteStringObject(
            new string[] { "x", "kty", "y", "crv" },
            new string[] { "ex", "EC", "why", "P-256" });

        Assert.Multiple(() =>
        {
            Assert.That(first.Text, Is.EqualTo("{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\"ex\",\"y\":\"why\"}"));
            Assert.That(second.Text, Is.EqualTo(first.Text));
        });
    }

    [Test]
    public void WriteStringObjectUsesUnicodeScalarOrdering()
    {
        string supplementary = "\U00010000";
        string privateUse = "\uE000";

        TokenTextResult result = token.JsonWriter.WriteStringObject(
            new string[] { privateUse, supplementary, "a" },
            new string[] { "private", "supplementary", "ascii" });

        Assert.That(
            result.Text,
            Is.EqualTo("{\"a\":\"ascii\",\"\uE000\":\"private\",\"\U00010000\":\"supplementary\"}"));
    }

    [Test]
    public void WriteStringObjectEscapesNamesAndValuesDeterministically()
    {
        string name = "quote\"slash\\";
        string value = new string(new char[] { '\b', '\t', '\n', '\f', '\r', '\u0000', '\u001F', '/', 'é', '\uD83D', '\uDE00' });

        TokenTextResult result = token.JsonWriter.WriteStringObject(new string[] { name }, new string[] { value });

        Assert.That(
            result.Text,
            Is.EqualTo("{\"quote\\\"slash\\\\\":\"\\b\\t\\n\\f\\r\\u0000\\u001f/é😀\"}"));
    }

    [Test]
    public void WriteStringObjectUtf8MatchesTextEncodingForHashing()
    {
        string[] names = new string[] { "kty", "k" };
        string[] values = new string[] { "oct", "AyM1SysPpbyDfgZld3umj1qzG9D0FQ" };

        TokenTextResult text = token.JsonWriter.WriteStringObject(names, values);
        TokenBytesResult utf8 = token.JsonWriter.WriteStringObjectUtf8(names, values);

        Assert.Multiple(() =>
        {
            Assert.That(utf8.Success, Is.True);
            Assert.That(utf8.Error, Is.Null);
            Assert.That(utf8.Bytes, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(text.Text)));
        });
    }

    [Test]
    public void WriteStringObjectSupportsEmptyObjectAndDoesNotModifyInputs()
    {
        string[] names = new string[] { "z", "a" };
        string[] values = new string[] { "last", "first" };

        TokenTextResult empty = token.JsonWriter.WriteStringObject(Array.Empty<string>(), Array.Empty<string>());
        token.JsonWriter.WriteStringObject(names, values);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Text, Is.EqualTo("{}"));
            Assert.That(names, Is.EqualTo(new string[] { "z", "a" }));
            Assert.That(values, Is.EqualTo(new string[] { "last", "first" }));
        });
    }

    [Test]
    public void WriteStringObjectRejectsDuplicateMemberNames()
    {
        TokenTextResult result = token.JsonWriter.WriteStringObject(
            new string[] { "kty", "n", "kty" },
            new string[] { "RSA", "value", "EC" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("duplicate_json_member"));
            Assert.That(result.Error.Position, Is.EqualTo(2));
        });
    }

    [Test]
    public void WriteStringObjectRejectsNullMembersAndInvalidUtf16()
    {
        string invalid = new string(new char[] { 'a', '\uD800' });
        TokenTextResult nullName = token.JsonWriter.WriteStringObject(new string[] { null }, new string[] { "value" });
        TokenTextResult nullValue = token.JsonWriter.WriteStringObject(new string[] { "name" }, new string[] { null });
        TokenTextResult invalidName = token.JsonWriter.WriteStringObject(new string[] { invalid }, new string[] { "value" });
        TokenTextResult invalidValue = token.JsonWriter.WriteStringObject(new string[] { "name" }, new string[] { invalid });

        Assert.Multiple(() =>
        {
            Assert.That(nullName.Error.Code, Is.EqualTo("invalid_json_member"));
            Assert.That(nullName.Error.Position, Is.Zero);
            Assert.That(nullValue.Error.Code, Is.EqualTo("invalid_json_member"));
            Assert.That(nullValue.Error.Position, Is.Zero);
            Assert.That(invalidName.Error.Code, Is.EqualTo("invalid_json_string"));
            Assert.That(invalidName.Error.Position, Is.EqualTo(1));
            Assert.That(invalidValue.Error.Code, Is.EqualTo("invalid_json_string"));
            Assert.That(invalidValue.Error.Position, Is.EqualTo(1));
        });
    }

    [Test]
    public void WriteStringObjectRejectsInvalidArrayArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => token.JsonWriter.WriteStringObject(null, Array.Empty<string>()), Throws.ArgumentNullException);
            Assert.That(() => token.JsonWriter.WriteStringObject(Array.Empty<string>(), null), Throws.ArgumentNullException);
            Assert.That(
                () => token.JsonWriter.WriteStringObject(new string[] { "a" }, Array.Empty<string>()),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("values"));
        });
    }

    [Test]
    public void WrittenOutputIsAcceptedByStrictJsonReader()
    {
        TokenTextResult written = token.JsonWriter.WriteStringObject(
            new string[] { "kty", "name" },
            new string[] { "RSA", "line\nquote\"😀" });

        JsonReadResult read = token.JsonReader.Read(written.Text);

        using (read.Document)
        {
            Assert.Multiple(() =>
            {
                Assert.That(read.Success, Is.True);
                Assert.That(read.Document.RootElement.GetProperty("name").GetString(), Is.EqualTo("line\nquote\"😀"));
            });
        }
    }
}
