namespace sdp.Tests.Unit.Writing;

public class SdpLosslessFixtureCorpusTests
{
    static IEnumerable<TestCaseData> UnchangedFixtures()
    {
        yield return Fixture("CrLfRfc", true,
            "v=0\r\n"
            + "o=alice 2890844526 2890844527 IN IP4 192.0.2.10\r\n"
            + "s=Lossless RFC fixture\r\n"
            + "c=IN IP4 233.252.0.1/32/2\r\n"
            + "t=0 0\r\n"
            + "m=audio 49170 RTP/AVP 0 96\r\n"
            + "a=rtpmap:0 PCMU/8000\r\n"
            + "a=rtpmap:96 opus/48000/2\r\n");
        yield return Fixture("BareLfInteroperability", true,
            "v=0\n"
            + "o=- 20518 0 IN IP4 203.0.113.1\n"
            + "s=interop\n"
            + "t=0 0\n"
            + "c=IN IP4 203.0.113.1\n"
            + "m=video 54400 RTP/AVPF 96\n"
            + "a=rtpmap:96 VP8/90000");
        yield return Fixture("BareCr", true,
            "v=0\r"
            + "o=- 1 1 IN IP6 2001:db8::1\r"
            + "s=carriage return fixture\r"
            + "c=IN IP6 2001:db8::2\r"
            + "t=0 0\r"
            + "m=text 6000 RTP/AVP 98\r"
            + "a=rtpmap:98 t140/1000");
        yield return Fixture("UnknownFields", true,
            "v=0\r\n"
            + "o=- 1 1 IN IP4 192.0.2.1\r\n"
            + "s=unknown fields\r\n"
            + "x=session extension with  spacing\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "t=0 0\r\n"
            + "m=application 9 UDP data\r\n"
            + "y=media-extension\r\n"
            + "a=X-Vendor:opaque  value");
        yield return Fixture("RecoverableMalformed", false,
            "v=0\r\n"
            + "o=- 1 1 IN IP4 192.0.2.1\n"
            + "s=recovery fixture\r"
            + "BROKEN SESSION LINE\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "t=0 0\r\n"
            + "m=audio 9 RTP/AVP 0\r\n"
            + "BROKEN MEDIA LINE");
        yield return Fixture("Utf8Text", true,
            "v=0\r\n"
            + "o=- 1 1 IN IP4 192.0.2.1\r\n"
            + "s=Réunion vidéo ✓\r\n"
            + "i=東京からの配信\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "t=0 0\r\n"
            + "m=video 5004 RTP/AVP 96\r\n"
            + "a=rtpmap:96 H264/90000\r\n");
    }

    [TestCaseSource(nameof(UnchangedFixtures))]
    public void UnchangedFixture_ReemitsIdenticalTextAndUtf8Bytes(string input, bool expectedParseSuccess)
    {
        SdpFixtureLosslessAssert.AssertExact(input, expectedParseSuccess);
    }

    [Test]
    public void LocalizedEdits_PreserveUnknownAndMalformedSessionAndMediaLinesInPlace()
    {
        string input =
            "v=0\r\n"
            + "o=- 1 1 IN IP4 192.0.2.1\r\n"
            + "s=Old  session\n"
            + "x=session extension with  spacing\r"
            + "c=IN IP4 192.0.2.1\r\n"
            + "b=AS:not-a-number\r\n"
            + "BROKEN SESSION LINE\r\n"
            + "t=0 0\r\n"
            + "m=audio 49170 RTP/AVP 0 96\r\n"
            + "a=RTPMAP:96 opus/48000/2\r\n"
            + "y=media extension with  spacing\n"
            + "BROKEN MEDIA LINE";
        SdpParseResult parsed = new SdpParser().ParseText(input, LosslessParseOptions());
        SdpSessionDescription description = parsed.Description!;
        description.SessionName = "New session";
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Port = 50000;
        media.Attributes.Single(attribute => string.Equals(attribute.Name, SdpAttributeNames.RtpMap, StringComparison.OrdinalIgnoreCase)).Value = "96 OPUS/48000/2";

        SdpWriteResult written = new SdpWriter().WriteText(description, new SdpWriteOptions { Mode = SdpWriteMode.Lossless });
        string expected =
            "v=0\r\n"
            + "o=- 1 1 IN IP4 192.0.2.1\r\n"
            + "s=New session\n"
            + "x=session extension with  spacing\r"
            + "c=IN IP4 192.0.2.1\r\n"
            + "b=AS:not-a-number\r\n"
            + "BROKEN SESSION LINE\r\n"
            + "t=0 0\r\n"
            + "m=audio 50000 RTP/AVP 0 96\r\n"
            + "a=rtpmap:96 OPUS/48000/2\r\n"
            + "y=media extension with  spacing\n"
            + "BROKEN MEDIA LINE";

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.False);
            Assert.That(parsed.Diagnostics.Count(diagnostic => diagnostic.Code == SdpDiagnosticCodes.ParseRecoveredMalformedLine), Is.EqualTo(3));
            Assert.That(written.Success, Is.True, WriteDiagnostics(written));
            Assert.That(written.Text, Is.EqualTo(expected));
            Assert.That(written.Text.IndexOf("x=session extension with  spacing", StringComparison.Ordinal), Is.LessThan(written.Text.IndexOf("BROKEN SESSION LINE", StringComparison.Ordinal)));
            Assert.That(written.Text.IndexOf("y=media extension with  spacing", StringComparison.Ordinal), Is.LessThan(written.Text.IndexOf("BROKEN MEDIA LINE", StringComparison.Ordinal)));
        });
    }

    static TestCaseData Fixture(string name, bool expectedParseSuccess, string input)
    {
        return new TestCaseData(input, expectedParseSuccess).SetName("UnchangedFixture_ReemitsIdenticalTextAndUtf8Bytes_" + name);
    }

    static SdpParseOptions LosslessParseOptions()
    {
        return new SdpParseOptions
        {
            Mode = SdpParseMode.Lossless,
            AllowLfLineEndings = true,
            AllowCrLineEndings = true
        };
    }

    static string WriteDiagnostics(SdpWriteResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }
}
