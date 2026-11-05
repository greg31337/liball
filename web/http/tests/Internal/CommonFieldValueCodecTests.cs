using System.Text;

namespace http.Tests;

public class CommonFieldValueCodecTests
{
    [Test]
    public void TokenListFieldsPreserveMembersAndCanonicalizeSeparators()
    {
        FieldValueParseResult allow = FieldValueParser.ParseAllow(
            "GET, , PATCH,"u8.ToArray(),
            32,
            8);
        FieldValueParseResult connection = FieldValueParser.ParseConnection(
            "keep-alive, Upgrade"u8.ToArray(),
            32,
            4);
        FieldValueParseResult trailer = FieldValueParser.ParseTrailer(
            "Digest, X-Trace"u8.ToArray(),
            32,
            4);
        FieldValueParseResult vary = FieldValueParser.ParseVary(
            "Accept-Encoding, *"u8.ToArray(),
            32,
            4);
        FieldValueWriteResult written = FieldValueWriter.SerializeAllow(
            new List<string> { "GET", "PATCH" },
            32,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(allow.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(allow.EmptyElements, Is.EqualTo(2));
            Assert.That(allow.Values, Has.Count.EqualTo(2));
            Assert.That(allow.Values[1], Is.EqualTo("PATCH"u8.ToArray()));
            Assert.That(connection.Values[1], Is.EqualTo("Upgrade"u8.ToArray()));
            Assert.That(trailer.Values[0], Is.EqualTo("Digest"u8.ToArray()));
            Assert.That(vary.Values[1], Is.EqualTo("*"u8.ToArray()));
            Assert.That(Encoding.ASCII.GetString(written.Bytes), Is.EqualTo("GET, PATCH"));
        });
    }

    [Test]
    public void TokenListFieldsRejectNonTokensAndEnforceMemberLimits()
    {
        FieldValueParseResult badMethod = FieldValueParser.ParseAllow(
            "GET, BAD METHOD"u8.ToArray(),
            32,
            4);
        FieldValueParseResult badName = FieldValueParser.ParseTrailer(
            "Digest, bad:name"u8.ToArray(),
            32,
            4);
        FieldValueParseResult limited = FieldValueParser.ParseConnection(
            "a,b,c"u8.ToArray(),
            32,
            2);
        FieldValueWriteResult badOutput = FieldValueWriter.SerializeVary(
            new List<string> { "Accept Encoding" },
            32,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(badMethod.IsMalformed, Is.True);
            Assert.That(badName.IsMalformed, Is.True);
            Assert.That(limited.IsLimitExceeded, Is.True);
            Assert.That(badOutput.IsMalformed, Is.True);
        });
    }

    [Test]
    public void UpgradeParserAcceptsNamedAndVersionedProtocols()
    {
        FieldValueParseResult parsed = FieldValueParser.ParseUpgrade(
            "websocket, IRC/6.9, RTA/x11"u8.ToArray(),
            64,
            6);
        FieldValueWriteResult written = FieldValueWriter.SerializeUpgrade(
            new List<string> { "websocket", "IRC/6.9" },
            64,
            4);
        FieldValueParseResult missingVersion = FieldValueParser.ParseUpgrade(
            "websocket/"u8.ToArray(),
            32,
            2);
        FieldValueParseResult extraSlash = FieldValueParser.ParseUpgrade(
            "one/two/three"u8.ToArray(),
            32,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Values, Has.Count.EqualTo(3));
            Assert.That(Encoding.ASCII.GetString(written.Bytes), Is.EqualTo("websocket, IRC/6.9"));
            Assert.That(missingVersion.IsMalformed, Is.True);
            Assert.That(extraSlash.IsMalformed, Is.True);
        });
    }

    [Test]
    public void TeParserSupportsTransferParametersBadWhitespaceWeightsAndTrailers()
    {
        MediaTypeParseResult parsed = FieldValueParser.ParseTe(
            "gzip; level = \"fast\"; q = 0.5, trailers, deflate"u8.ToArray(),
            96,
            6,
            4);
        FieldValueWriteResult written = FieldValueWriter.SerializeTe(
            parsed.Values,
            96,
            6,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Values, Has.Count.EqualTo(3));
            Assert.That(parsed.Values[0].Value, Is.EqualTo("gzip"));
            Assert.That(parsed.Values[0].Parameters, Has.Count.EqualTo(1));
            Assert.That(parsed.Values[0].Parameters[0].Value, Is.EqualTo("fast"u8.ToArray()));
            Assert.That(parsed.Values[0].Quality, Is.EqualTo(500));
            Assert.That(parsed.Values[1].Value, Is.EqualTo("trailers"));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("gzip; level=\"fast\"; q=0.5, trailers, deflate"));
        });
    }

    [Test]
    public void TeParserRejectsParametersOnTrailersAndNonTerminalOrDuplicateWeights()
    {
        MediaTypeParseResult trailers = FieldValueParser.ParseTe(
            "trailers;q=1"u8.ToArray(),
            32,
            3,
            3);
        MediaTypeParseResult nonTerminal = FieldValueParser.ParseTe(
            "gzip;q=0.5;level=1"u8.ToArray(),
            48,
            3,
            3);
        MediaTypeParseResult duplicate = FieldValueParser.ParseTe(
            "gzip;q=0.5;q=0.4"u8.ToArray(),
            48,
            3,
            3);
        HttpWeightedValue invalidTrailers = new HttpWeightedValue();
        invalidTrailers.Value = "trailers";
        invalidTrailers.Quality = 500;
        FieldValueWriteResult written = FieldValueWriter.SerializeTe(
            new List<HttpWeightedValue> { invalidTrailers },
            32,
            3,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(trailers.IsMalformed, Is.True);
            Assert.That(nonTerminal.IsMalformed, Is.True);
            Assert.That(duplicate.IsMalformed, Is.True);
            Assert.That(written.IsMalformed, Is.True);
        });
    }

    [Test]
    public void ExpectParserPreservesExtensionValuesAndParameters()
    {
        CommonFieldParseResult parsed = FieldValueParser.ParseExpect(
            "100-continue, custom=\"yes\"; mode=fast"u8.ToArray(),
            96,
            4,
            4);
        FieldValueWriteResult written = FieldValueWriter.SerializeExpect(
            parsed.Expectations,
            96,
            4,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Expectations, Has.Count.EqualTo(2));
            Assert.That(parsed.Expectations[0].Name, Is.EqualTo("100-continue"));
            Assert.That(parsed.Expectations[0].HasValue, Is.False);
            Assert.That(parsed.Expectations[1].Name, Is.EqualTo("custom"));
            Assert.That(parsed.Expectations[1].Value, Is.EqualTo("yes"u8.ToArray()));
            Assert.That(parsed.Expectations[1].IsQuoted, Is.True);
            Assert.That(parsed.Expectations[1].Parameters[0].Name, Is.EqualTo("mode"));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("100-continue, custom=\"yes\"; mode=fast"));
        });
    }

    [Test]
    public void ExpectParserRejectsMalformedValuesAndEnforcesLimits()
    {
        CommonFieldParseResult whitespace = FieldValueParser.ParseExpect(
            "custom =yes"u8.ToArray(),
            32,
            2,
            2);
        CommonFieldParseResult missing = FieldValueParser.ParseExpect(
            "custom="u8.ToArray(),
            32,
            2,
            2);
        CommonFieldParseResult memberLimit = FieldValueParser.ParseExpect(
            "one,two"u8.ToArray(),
            32,
            1,
            2);
        CommonFieldParseResult parameterLimit = FieldValueParser.ParseExpect(
            "custom=yes;a=1;b=2"u8.ToArray(),
            48,
            2,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(whitespace.IsMalformed, Is.True);
            Assert.That(missing.IsMalformed, Is.True);
            Assert.That(memberLimit.IsLimitExceeded, Is.True);
            Assert.That(parameterLimit.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void NumericFieldsAcceptTheFullSignedStorageRangeAndSerializeCanonically()
    {
        byte[] maximum = Encoding.ASCII.GetBytes(long.MaxValue.ToString());
        CommonFieldParseResult forwards = FieldValueParser.ParseMaxForwards(maximum, 32);
        CommonFieldParseResult retry = FieldValueParser.ParseRetryAfter("00120"u8.ToArray(), 32);
        FieldValueWriteResult forwardsOutput = FieldValueWriter.SerializeMaxForwards(
            forwards.Number,
            32);
        FieldValueWriteResult retryOutput = FieldValueWriter.SerializeRetryAfterDelay(
            retry.Number,
            32);

        Assert.Multiple(() =>
        {
            Assert.That(forwards.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(forwards.Number, Is.EqualTo(long.MaxValue));
            Assert.That(retry.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(retry.IsDate, Is.False);
            Assert.That(retry.Number, Is.EqualTo(120));
            Assert.That(Encoding.ASCII.GetString(forwardsOutput.Bytes), Is.EqualTo(long.MaxValue.ToString()));
            Assert.That(Encoding.ASCII.GetString(retryOutput.Bytes), Is.EqualTo("120"));
        });
    }

    [Test]
    public void RetryAfterAcceptsHttpDatesAndNumericFieldsRejectInvalidValues()
    {
        CommonFieldParseResult date = FieldValueParser.ParseRetryAfter(
            "Sun, 06 Nov 1994 08:49:37 GMT"u8.ToArray(),
            64);
        FieldValueWriteResult written = FieldValueWriter.SerializeRetryAfterDate(date.Date, 64);
        CommonFieldParseResult signed = FieldValueParser.ParseMaxForwards("+1"u8.ToArray(), 16);
        CommonFieldParseResult overflow = FieldValueParser.ParseRetryAfter(
            "9223372036854775808"u8.ToArray(),
            32);
        FieldValueWriteResult negative = FieldValueWriter.SerializeMaxForwards(-1, 16);

        Assert.Multiple(() =>
        {
            Assert.That(date.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(date.IsDate, Is.True);
            Assert.That(date.Date, Is.EqualTo(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero)));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("Sun, 06 Nov 1994 08:49:37 GMT"));
            Assert.That(signed.IsMalformed, Is.True);
            Assert.That(overflow.IsMalformed, Is.True);
            Assert.That(negative.IsMalformed, Is.True);
        });
    }

    [Test]
    public void LocationFieldsApplyTheirDistinctUriReferenceGrammars()
    {
        FieldValueParseResult location = FieldValueParser.ParseLocation(
            "../next?q=a,b#part"u8.ToArray(),
            64);
        FieldValueParseResult content = FieldValueParser.ParseContentLocation(
            "https://example.test/items/1?q=yes"u8.ToArray(),
            64);
        FieldValueParseResult contentFragment = FieldValueParser.ParseContentLocation(
            "/items/1#part"u8.ToArray(),
            64);
        FieldValueWriteResult locationOutput = FieldValueWriter.SerializeLocation(
            "../next?q=a,b#part",
            64);
        FieldValueWriteResult contentOutput = FieldValueWriter.SerializeContentLocation(
            "/items/1?q=yes",
            64);

        Assert.Multiple(() =>
        {
            Assert.That(location.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(location.Value, Is.EqualTo("../next?q=a,b#part"u8.ToArray()));
            Assert.That(content.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(contentFragment.IsMalformed, Is.True);
            Assert.That(Encoding.ASCII.GetString(locationOutput.Bytes),
                Is.EqualTo("../next?q=a,b#part"));
            Assert.That(Encoding.ASCII.GetString(contentOutput.Bytes),
                Is.EqualTo("/items/1?q=yes"));
        });
    }

    [Test]
    public void ViaParserHandlesProtocolsPortsCommentsAndEmptyListMembers()
    {
        CommonFieldParseResult parsed = FieldValueParser.ParseViaEntries(
            "1.0 fred, , HTTP/1.1 p.example.net:8080 (edge \\(west\\)),"u8.ToArray(),
            128,
            8,
            4);
        FieldValueWriteResult written = FieldValueWriter.SerializeViaEntries(
            parsed.ViaEntries,
            128,
            8,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.EmptyElements, Is.EqualTo(2));
            Assert.That(parsed.ViaEntries, Has.Count.EqualTo(2));
            Assert.That(parsed.ViaEntries[0].ReceivedProtocol, Is.EqualTo("1.0"));
            Assert.That(parsed.ViaEntries[0].ReceivedBy, Is.EqualTo("fred"));
            Assert.That(parsed.ViaEntries[0].HasPort, Is.False);
            Assert.That(parsed.ViaEntries[1].ReceivedProtocol, Is.EqualTo("HTTP/1.1"));
            Assert.That(parsed.ViaEntries[1].Port, Is.EqualTo(8080));
            Assert.That(parsed.ViaEntries[1].Comment, Is.EqualTo("edge (west)"u8.ToArray()));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("1.0 fred, HTTP/1.1 p.example.net:8080 (edge \\(west\\))"));
        });
    }

    [Test]
    public void ViaCodecRejectsInvalidSeparatorsPortsCommentsAndBounds()
    {
        CommonFieldParseResult noWhitespace = FieldValueParser.ParseViaEntries(
            "1.1proxy"u8.ToArray(),
            32,
            2,
            2);
        CommonFieldParseResult badPort = FieldValueParser.ParseViaEntries(
            "1.1 proxy:65536"u8.ToArray(),
            32,
            2,
            2);
        CommonFieldParseResult deepComment = FieldValueParser.ParseViaEntries(
            "1.1 proxy ((deep))"u8.ToArray(),
            48,
            2,
            1);
        CommonFieldParseResult entryLimit = FieldValueParser.ParseViaEntries(
            "1.0 one, 1.1 two"u8.ToArray(),
            48,
            1,
            2);
        HttpViaEntry invalid = new HttpViaEntry();
        invalid.ReceivedProtocol = "1.1";
        invalid.ReceivedBy = "bad host";
        FieldValueWriteResult written = FieldValueWriter.SerializeViaEntries(
            new List<HttpViaEntry> { invalid },
            48,
            2,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(noWhitespace.IsMalformed, Is.True);
            Assert.That(badPort.IsMalformed, Is.True);
            Assert.That(deepComment.IsLimitExceeded, Is.True);
            Assert.That(entryLimit.IsLimitExceeded, Is.True);
            Assert.That(written.IsMalformed, Is.True);
        });
    }
}
