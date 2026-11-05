using System.Text;

namespace http.Tests;

public class FieldValueWriterTests
{
    [Test]
    public void RawFieldValueSerializationPreservesExactSafeOctets()
    {
        byte[] input = [0x09, 0x20, 0x41, 0x7e, 0x80, 0xff];

        FieldValueWriteResult result = FieldValueWriter.SerializeFieldValue(input, 16);
        result.Bytes[2] = 0x42;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Written, Is.EqualTo(input.Length));
            Assert.That(result.RequiredSize, Is.EqualTo(input.Length));
            Assert.That(input, Is.EqualTo(new byte[] { 0x09, 0x20, 0x41, 0x7e, 0x80, 0xff }));
        });
    }

    [Test]
    public void RawFieldValueSerializationRejectsInjectionAndControlOctets()
    {
        byte[][] invalidValues =
        [
            new byte[] { 0x00 },
            new byte[] { 0x0a },
            new byte[] { 0x0d },
            new byte[] { 0x1f },
            new byte[] { 0x7f },
            "safe\r\ninjected"u8.ToArray()
        ];

        foreach (byte[] value in invalidValues)
        {
            FieldValueWriteResult result = FieldValueWriter.SerializeFieldValue(value, 64);
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsMalformed, Is.True);
            Assert.That(result.Bytes, Is.Empty);
        }
    }

    [Test]
    public void BoundedFieldValueWritingIsTransactional()
    {
        byte[] tooSmall = [0xcc, 0xcc, 0xcc];
        byte[] injected = [0xcc, 0xcc, 0xcc, 0xcc];
        byte[] success = [0xcc, 0xcc, 0xcc, 0xcc, 0xcc];

        FieldValueWriteResult insufficient = FieldValueWriter.WriteFieldValue(
            "abc"u8.ToArray(),
            tooSmall,
            1,
            8);
        FieldValueWriteResult malformed = FieldValueWriter.WriteFieldValue(
            "a\r\n"u8.ToArray(),
            injected,
            0,
            8);
        FieldValueWriteResult written = FieldValueWriter.WriteFieldValue(
            "abc"u8.ToArray(),
            success,
            1,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(insufficient.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(insufficient.RequiredSize, Is.EqualTo(4));
            Assert.That(tooSmall, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc }));
            Assert.That(malformed.IsMalformed, Is.True);
            Assert.That(injected, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc, 0xcc }));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(3));
            Assert.That(success, Is.EqualTo(new byte[] { 0xcc, 0x61, 0x62, 0x63, 0xcc }));
        });
    }

    [Test]
    public void FieldValueAndTokenSerializationEnforceTheirLimitsAndGrammar()
    {
        FieldValueWriteResult fieldLimit = FieldValueWriter.SerializeFieldValue("abcd"u8.ToArray(), 3);
        FieldValueWriteResult token = FieldValueWriter.SerializeToken("X-Extension"u8.ToArray(), 32);
        FieldValueWriteResult emptyToken = FieldValueWriter.SerializeToken(Array.Empty<byte>(), 32);
        FieldValueWriteResult spacedToken = FieldValueWriter.SerializeToken("not a token"u8.ToArray(), 32);
        FieldValueWriteResult tokenLimit = FieldValueWriter.SerializeToken("abcd"u8.ToArray(), 3);

        Assert.Multiple(() =>
        {
            Assert.That(fieldLimit.IsLimitExceeded, Is.True);
            Assert.That(fieldLimit.IsMalformed, Is.False);
            Assert.That(token.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(token.Bytes, Is.EqualTo("X-Extension"u8.ToArray()));
            Assert.That(emptyToken.IsMalformed, Is.True);
            Assert.That(spacedToken.IsMalformed, Is.True);
            Assert.That(tokenLimit.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void QuotedStringSerializationUsesMinimalEscapingAndRoundTripsBytes()
    {
        byte[] value = [0x61, 0x22, 0x62, 0x5c, 0x09, 0x80];

        FieldValueWriteResult written = FieldValueWriter.SerializeQuotedString(value, 32);
        FieldValueParseResult parsed = FieldValueParser.ParseQuotedString(written.Bytes, 32);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Bytes, Is.EqualTo(new byte[]
            {
                0x22,
                0x61,
                0x5c, 0x22,
                0x62,
                0x5c, 0x5c,
                0x09,
                0x80,
                0x22
            }));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void QuotedStringSerializationRejectsControlsAndExpansionPastTheLimit()
    {
        FieldValueWriteResult carriageReturn = FieldValueWriter.SerializeQuotedString(
            new byte[] { 0x61, 0x0d },
            16);
        FieldValueWriteResult delete = FieldValueWriter.SerializeQuotedString(new byte[] { 0x7f }, 16);
        FieldValueWriteResult limit = FieldValueWriter.SerializeQuotedString(
            new byte[] { 0x22, 0x5c },
            5);

        Assert.Multiple(() =>
        {
            Assert.That(carriageReturn.IsMalformed, Is.True);
            Assert.That(carriageReturn.ErrorPosition, Is.EqualTo(1));
            Assert.That(delete.IsMalformed, Is.True);
            Assert.That(limit.IsLimitExceeded, Is.True);
            Assert.That(limit.RequiredSize, Is.EqualTo(6));
        });
    }

    [Test]
    public void QuotedPairSerializationAcceptsOnlyQuotedPairOctets()
    {
        FieldValueWriteResult quote = FieldValueWriter.SerializeQuotedPair(0x22, 2);
        FieldValueWriteResult obsText = FieldValueWriter.SerializeQuotedPair(0xff, 2);
        FieldValueWriteResult control = FieldValueWriter.SerializeQuotedPair(0x1f, 2);
        FieldValueWriteResult limit = FieldValueWriter.SerializeQuotedPair(0x22, 1);

        Assert.Multiple(() =>
        {
            Assert.That(quote.Bytes, Is.EqualTo(new byte[] { 0x5c, 0x22 }));
            Assert.That(obsText.Bytes, Is.EqualTo(new byte[] { 0x5c, 0xff }));
            Assert.That(control.IsMalformed, Is.True);
            Assert.That(limit.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void CommentSerializationEscapesDelimitersAndRoundTripsContent()
    {
        byte[] value = [0x61, 0x28, 0x62, 0x29, 0x5c, 0x22, 0x80];

        FieldValueWriteResult written = FieldValueWriter.SerializeComment(value, 32);
        FieldValueParseResult parsed = FieldValueParser.ParseComment(written.Bytes, 32, 4);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(written.Bytes, 0, 9), Is.EqualTo("(a\\(b\\)\\\\"));
            Assert.That(written.Bytes[9], Is.EqualTo(0x22));
            Assert.That(written.Bytes[10], Is.EqualTo(0x80));
            Assert.That(written.Bytes[11], Is.EqualTo(0x29));
            Assert.That(parsed.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void CommentSerializationRejectsInjectionAndHonorsExpandedSize()
    {
        FieldValueWriteResult lineFeed = FieldValueWriter.SerializeComment(new byte[] { 0x0a }, 16);
        FieldValueWriteResult limit = FieldValueWriter.SerializeComment("()\\"u8.ToArray(), 7);

        Assert.Multiple(() =>
        {
            Assert.That(lineFeed.IsMalformed, Is.True);
            Assert.That(limit.IsLimitExceeded, Is.True);
            Assert.That(limit.RequiredSize, Is.EqualTo(8));
        });
    }

    [Test]
    public void ListSerializationUsesCanonicalSeparatorsAndPreservesMembers()
    {
        List<byte[]> values =
        [
            " X-Token "u8.ToArray(),
            "\"quoted, value\""u8.ToArray(),
            "item(a,b)"u8.ToArray()
        ];

        FieldValueWriteResult written = FieldValueWriter.SerializeCommaList(values, 64, 3, 4);
        FieldValueParseResult parsed = FieldValueParser.ParseCommaList(written.Bytes, 64, 3, 4);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("X-Token, \"quoted, value\", item(a,b)"));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Values, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void HashListSerializationUsesTheSameSenderCanonicalForm()
    {
        List<byte[]> values = ["one"u8.ToArray(), "two"u8.ToArray()];

        FieldValueWriteResult written = FieldValueWriter.SerializeHashList(values, 16, 2, 2);

        Assert.That(written.Bytes, Is.EqualTo("one, two"u8.ToArray()));
    }

    [Test]
    public void ListSerializationRejectsUnsafeOrAmbiguousMembersAndLimits()
    {
        FieldValueWriteResult empty = FieldValueWriter.SerializeCommaList(
            new List<byte[]> { Array.Empty<byte>() },
            32,
            4,
            2);
        FieldValueWriteResult comma = FieldValueWriter.SerializeCommaList(
            new List<byte[]> { "one,two"u8.ToArray() },
            32,
            4,
            2);
        FieldValueWriteResult injected = FieldValueWriter.SerializeCommaList(
            new List<byte[]> { "one\r\ntwo"u8.ToArray() },
            32,
            4,
            2);
        FieldValueWriteResult members = FieldValueWriter.SerializeCommaList(
            new List<byte[]> { "one"u8.ToArray(), "two"u8.ToArray() },
            32,
            1,
            2);
        FieldValueWriteResult size = FieldValueWriter.SerializeCommaList(
            new List<byte[]> { "one"u8.ToArray(), "two"u8.ToArray() },
            7,
            2,
            2);
        FieldValueWriteResult depth = FieldValueWriter.SerializeCommaList(
            new List<byte[]> { "one((two))"u8.ToArray() },
            32,
            2,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsMalformed, Is.True);
            Assert.That(comma.IsMalformed, Is.True);
            Assert.That(injected.IsMalformed, Is.True);
            Assert.That(members.IsLimitExceeded, Is.True);
            Assert.That(size.IsLimitExceeded, Is.True);
            Assert.That(size.RequiredSize, Is.EqualTo(8));
            Assert.That(depth.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void ParameterSerializationPreservesOrderTokensAndQuotedValueBytes()
    {
        HttpParameter token = Parameter("X-Mode", true, false, "Fast"u8.ToArray());
        HttpParameter quoted = Parameter("title", true, true, "a\"b\\c"u8.ToArray());
        HttpParameter flag = Parameter("extension-flag", false, false, Array.Empty<byte>());
        List<HttpParameter> parameters = [token, quoted, flag];

        FieldValueWriteResult written = FieldValueWriter.SerializeParameters(parameters, 128, 3);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("; X-Mode=Fast; title=\"a\\\"b\\\\c\"; extension-flag"));
            Assert.That(token.Name, Is.EqualTo("X-Mode"));
            Assert.That(quoted.Value, Is.EqualTo("a\"b\\c"u8.ToArray()));
        });
    }

    [Test]
    public void ValueBearingParametersRoundTripThroughCanonicalSerialization()
    {
        List<HttpParameter> parameters =
        [
            Parameter("charset", true, false, "utf-8"u8.ToArray()),
            Parameter("title", true, true, "a b"u8.ToArray())
        ];

        FieldValueWriteResult written = FieldValueWriter.SerializeParameters(parameters, 64, 4);
        FieldValueParseResult parsed = FieldValueParser.ParseParameters(written.Bytes, 64, 4);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Parameters, Has.Count.EqualTo(2));
            Assert.That(parsed.Parameters[0].Name, Is.EqualTo("charset"));
            Assert.That(parsed.Parameters[0].Value, Is.EqualTo("utf-8"u8.ToArray()));
            Assert.That(parsed.Parameters[1].IsQuoted, Is.True);
            Assert.That(parsed.Parameters[1].Value, Is.EqualTo("a b"u8.ToArray()));
        });
    }

    [Test]
    public void ParameterSerializationRejectsInvalidStateAndEnforcesLimits()
    {
        FieldValueWriteResult badName = FieldValueWriter.SerializeParameters(
            new List<HttpParameter> { Parameter("bad name", true, false, "x"u8.ToArray()) },
            32,
            2);
        FieldValueWriteResult badToken = FieldValueWriter.SerializeParameters(
            new List<HttpParameter> { Parameter("name", true, false, "not token"u8.ToArray()) },
            32,
            2);
        FieldValueWriteResult badFlag = FieldValueWriter.SerializeParameters(
            new List<HttpParameter> { Parameter("name", false, true, Array.Empty<byte>()) },
            32,
            2);
        FieldValueWriteResult count = FieldValueWriter.SerializeParameters(
            new List<HttpParameter>
            {
                Parameter("a", true, false, "b"u8.ToArray()),
                Parameter("c", true, false, "d"u8.ToArray())
            },
            32,
            1);
        FieldValueWriteResult size = FieldValueWriter.SerializeParameters(
            new List<HttpParameter> { Parameter("a", true, false, "b"u8.ToArray()) },
            4,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(badName.IsMalformed, Is.True);
            Assert.That(badToken.IsMalformed, Is.True);
            Assert.That(badFlag.IsMalformed, Is.True);
            Assert.That(count.IsLimitExceeded, Is.True);
            Assert.That(size.IsLimitExceeded, Is.True);
            Assert.That(size.RequiredSize, Is.EqualTo(5));
        });
    }

    static HttpParameter Parameter(string name, bool hasValue, bool isQuoted, byte[] value)
    {
        HttpParameter parameter = new HttpParameter();
        parameter.Name = name;
        parameter.HasValue = hasValue;
        parameter.IsQuoted = isQuoted;
        parameter.Value = value;
        return parameter;
    }
}
