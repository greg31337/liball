namespace sdp.Tests.Conformance.Rfc8866;

public class SdpPublishedExampleConformanceTests
{
    [TestCase("cat:foo.bar", true, false)]
    [TestCase("keywds:SDP session description protocol", true, false)]
    [TestCase("tool:foobar V3.2", true, false)]
    [TestCase("ptime:20", false, false)]
    [TestCase("maxptime:20", false, false)]
    [TestCase("recvonly", true, false)]
    [TestCase("sendrecv", false, false)]
    [TestCase("sendonly", false, false)]
    [TestCase("inactive", false, false)]
    [TestCase("orient:portrait", false, true)]
    [TestCase("type:moderated", true, false)]
    [TestCase("charset:ISO-8859-1", true, false)]
    [TestCase("sdplang:fr", true, false)]
    [TestCase("sdplang:fr", false, false)]
    [TestCase("lang:de", true, false)]
    [TestCase("lang:de", false, false)]
    [TestCase("framerate:60", false, true)]
    [TestCase("quality:10", false, true)]
    [TestCase("fmtp:96 profile-level-id=42e016;max-mbps=108000;max-fs=3600", false, true)]
    public void RegisteredAttributeExample_ParsesValidatesAndGeneratesCanonically(string attribute, bool sessionScope, bool video)
    {
        string input = Header();
        if (sessionScope)
        {
            input = input + "a=" + attribute + "\r\n";
        }
        input = input + (video ? VideoMedia() : AudioMedia());
        if (!sessionScope)
        {
            input = input + "a=" + attribute + "\r\n";
        }

        AssertConformingCanonicalDocument(input, "a=" + attribute + "\r\n");
    }

    [Test]
    public void PublishedSessionExample_ParsesValidatesAndRegeneratesExactly()
    {
        string input =
            "v=0\r\n" +
            "o=jdoe 3724394400 3724394405 IN IP4 198.51.100.1\r\n" +
            "s=Call to John Smith\r\n" +
            "i=SDP Offer #1\r\n" +
            "u=http://www.jdoe.example.com/home.html\r\n" +
            "e=Jane Doe <jane@jdoe.example.com>\r\n" +
            "p=+1 617 555-6011\r\n" +
            "c=IN IP4 198.51.100.1\r\n" +
            "t=0 0\r\n" +
            "m=audio 49170 RTP/AVP 0\r\n" +
            "m=audio 49180 RTP/AVP 0\r\n" +
            "m=video 51372 RTP/AVP 99\r\n" +
            "c=IN IP6 2001:db8::2\r\n" +
            "a=rtpmap:99 h263-1998/90000\r\n";

        AssertConformingCanonicalDocument(input, input);
    }

    [Test]
    public void PublishedContactAndExperimentalBandwidthExamples_RegenerateExactly()
    {
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=RFC 8866 contacts\r\n" +
            "e=j.doe@example.com (Jane Doe)\r\n" +
            "e=Jane Doe <j.doe@example.com>\r\n" +
            "p=+1 617 555-6011\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "b=X-YZ:128\r\n" +
            "t=0 0\r\n" +
            "m=audio 49232 RTP/AVP 0\r\n";

        AssertConformingCanonicalDocument(input, input);
    }

    [Test]
    public void PublishedDirectionInheritanceExample_ResolvesAndRegeneratesEveryMediaDirection()
    {
        string input =
            "v=0\r\n" +
            "o=jdoe 3724395000 3724395001 IN IP6 2001:db8::1\r\n" +
            "s=-\r\n" +
            "c=IN IP6 2001:db8::1\r\n" +
            "t=0 0\r\n" +
            "a=inactive\r\n" +
            "m=audio 49170 RTP/AVP 0\r\n" +
            "a=sendrecv\r\n" +
            "m=audio 49180 RTP/AVP 0\r\n" +
            "m=video 51372 RTP/AVP 99\r\n" +
            "a=rtpmap:99 h263-1998/90000\r\n";
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = parsed.Description!;
        ISdpQueryService query = new SdpQueryService();
        SdpValidationResult validation = Validate(description);
        SdpWriteResult written = new SdpWriter().WriteText(description, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(validation.Issues.Where(IsError), Is.Empty, Diagnostics(validation.Issues));
            Assert.That(query.ResolveEffectiveDirection(description, 0), Is.EqualTo(SdpDirection.SendReceive));
            Assert.That(query.ResolveEffectiveDirection(description, 1), Is.EqualTo(SdpDirection.Inactive));
            Assert.That(query.ResolveEffectiveDirection(description, 2), Is.EqualTo(SdpDirection.Inactive));
            Assert.That(written.Success, Is.True, Diagnostics(written.Diagnostics));
            Assert.That(written.Text, Is.EqualTo(input));
        });
    }

    [TestCase("c=IN IP4 233.252.0.1/127\r\n", "233.252.0.1", 127, 1)]
    [TestCase("c=IN IP4 233.252.0.1/127/3\r\n", "233.252.0.1", 127, 3)]
    [TestCase("c=IN IP6 ff00::db8:0:101/3\r\n", "ff00::db8:0:101", 0, 3)]
    public void PublishedMulticastConnectionExample_PreservesTtlAndAddressCount(string connectionLine, string address, int ttl, int addressCount)
    {
        string input = HeaderWithoutConnection() + AudioMedia(connectionLine);
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());
        SdpConnectionData connection = parsed.Description!.MediaDescriptions[0].Connections.Single();
        SdpValidationResult validation = Validate(parsed.Description);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(connection.Address, Is.EqualTo(address));
            Assert.That(connection.TimeToLive, Is.EqualTo(ttl));
            Assert.That(connection.AddressCount, Is.EqualTo(addressCount));
            Assert.That(validation.Issues.Where(IsError), Is.Empty, Diagnostics(validation.Issues));
            Assert.That(written.Success, Is.True, Diagnostics(written.Diagnostics));
            Assert.That(written.Text, Is.EqualTo(input));
        });
    }

    [Test]
    public void PublishedEquivalentLayeredConnections_PreserveAllMediaConnectionLines()
    {
        string input = HeaderWithoutConnection() + AudioMedia(
            "c=IN IP4 233.252.0.1/127\r\n" +
            "c=IN IP4 233.252.0.2/127\r\n" +
            "c=IN IP4 233.252.0.3/127\r\n");
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(parsed.Description!.MediaDescriptions[0].Connections.Select(connection => connection.Address), Is.EqualTo(new[] { "233.252.0.1", "233.252.0.2", "233.252.0.3" }));
            Assert.That(validation.Issues.Where(IsError), Is.Empty, Diagnostics(validation.Issues));
            Assert.That(written.Success, Is.True, Diagnostics(written.Diagnostics));
            Assert.That(written.Text, Is.EqualTo(input));
        });
    }

    [Test]
    public void PublishedEquivalentIpv6LayeredConnections_PreserveAllMediaConnectionLines()
    {
        string input = HeaderWithoutConnection() + AudioMedia(
            "c=IN IP6 ff00::db8:0:101\r\n" +
            "c=IN IP6 ff00::db8:0:102\r\n" +
            "c=IN IP6 ff00::db8:0:103\r\n");
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(parsed.Description!.MediaDescriptions[0].Connections.Select(connection => connection.Address), Is.EqualTo(new[] { "ff00::db8:0:101", "ff00::db8:0:102", "ff00::db8:0:103" }));
            Assert.That(validation.Issues.Where(IsError), Is.Empty, Diagnostics(validation.Issues));
            Assert.That(written.Success, Is.True, Diagnostics(written.Diagnostics));
            Assert.That(written.Text, Is.EqualTo(input));
        });
    }

    [TestCase("m=video 49170/2 RTP/AVP 31\r\n", 0)]
    [TestCase("m=video 49170/2 RTP/AVP 31\r\nc=IN IP4 233.252.0.1/127/2\r\n", 1)]
    [TestCase("m=video 49170/2 RTP/AVP 31\r\nc=IN IP6 ff00::db8:0:101\r\nc=IN IP6 ff00::db8:0:102\r\n", 2)]
    public void PublishedPortCountAndLayeredMediaExamples_RegenerateExactly(string mediaLines, int mediaConnectionCount)
    {
        string input = mediaConnectionCount == 0 ? Header() + mediaLines : HeaderWithoutConnection() + mediaLines;
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(parsed.Description!.MediaDescriptions[0].PortCount, Is.EqualTo(2));
            Assert.That(parsed.Description.MediaDescriptions[0].Connections, Has.Count.EqualTo(mediaConnectionCount));
            Assert.That(validation.Issues.Where(IsError), Is.Empty, Diagnostics(validation.Issues));
            Assert.That(written.Success, Is.True, Diagnostics(written.Diagnostics));
            Assert.That(written.Text, Is.EqualTo(input));
        });
    }

    [TestCase("t=3724394400 3724398000\r\nt=3724484400 3724488000\r\n", 2, 0, 0)]
    [TestCase("t=3724394400 3730536000\r\nr=604800 3600 0 90000\r\n", 1, 1, 0)]
    [TestCase("t=3724394400 3730536000\r\nr=7d 1h 0 25h\r\n", 1, 1, 0)]
    [TestCase("t=3724394400 3754123200\r\nr=604800 3600 0 90000\r\nz=3730928400 -1h 3749680800 0\r\n", 1, 1, 2)]
    public void PublishedTimingExample_PreservesEveryTimingProduction(string timingLines, int timingCount, int repeatCount, int zoneCount)
    {
        string input = HeaderBeforeTiming() + timingLines + AudioMedia();
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(parsed.Description!.Timings, Has.Count.EqualTo(timingCount));
            Assert.That(parsed.Description.Timings[0].RepeatTimes, Has.Count.EqualTo(repeatCount));
            Assert.That(parsed.Description.TimeZoneAdjustments, Has.Count.EqualTo(zoneCount));
            Assert.That(validation.Issues.Where(IsError), Is.Empty, Diagnostics(validation.Issues));
            Assert.That(written.Success, Is.True, Diagnostics(written.Diagnostics));
            Assert.That(written.Text, Is.EqualTo(input));
        });
    }

    [TestCase("m=audio 49232 RTP/AVP 0\r\n", 1)]
    [TestCase("m=audio 49232 RTP/AVP 98\r\na=rtpmap:98 L16/16000/2\r\n", 1)]
    [TestCase("m=audio 49230 RTP/AVP 96 97 98\r\na=rtpmap:96 L8/8000\r\na=rtpmap:97 L16/8000\r\na=rtpmap:98 L16/11025/2\r\n", 3)]
    public void PublishedRtpMapExample_ResolvesEveryStaticAndDynamicFormat(string mediaLines, int codecCount)
    {
        string input = Header() + mediaLines;
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        List<SdpCodec> codecs = new SdpCodecService().ResolveMediaCodecs(parsed.Description!, 0);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(codecs, Has.Count.EqualTo(codecCount));
            Assert.That(validation.Issues.Where(IsError), Is.Empty, Diagnostics(validation.Issues));
            Assert.That(written.Success, Is.True, Diagnostics(written.Diagnostics));
            Assert.That(written.Text, Is.EqualTo(input));
        });
    }

    [Test]
    public void AbnfUpperBoundDocument_ParsesValidatesAndCanonicalizesWithoutOverflow()
    {
        string input =
            "v=000\r\n" +
            "o=user/name 18446744073709551615 18446744073709551615 X-NET X-ADDR opaque:value\r\n" +
            "s=Résumé ✓\r\n" +
            "u=../next?name=value#part\r\n" +
            "e=Jane Doe <jane@example.test>\r\n" +
            "p=+1 617 555-6011\r\n" +
            "c=X-NET X-ADDR opaque:value/segment\r\n" +
            "b=XBW:9223372036854775807\r\n" +
            "t=184467440737095516160 184467440737095516161\r\n" +
            "r=1844674407370955161 1 0\r\n" +
            "z=184467440737095516160 -1d\r\n" +
            "a=AZaz09!#$%&'*+-.^_`{|}~:opaque:value\r\n" +
            "m=x-media 65535/1 X-PROTO/One fmt.one\r\n";
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        SdpSessionDescription created = new SdpSessionFactory().CreateSession(parsed.Description!.Origin!, parsed.Description.SessionName, parsed.Description.Connection!, parsed.Description.Timings[0]);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(validation.Issues.Where(IsError), Is.Empty, Diagnostics(validation.Issues));
            Assert.That(written.Success, Is.True, Diagnostics(written.Diagnostics));
            Assert.That(written.Text, Does.StartWith("v=0\r\n"));
            Assert.That(written.Text, Does.Contain("b=XBW:9223372036854775807\r\n"));
            Assert.That(written.Text, Does.Contain("t=184467440737095516160 184467440737095516161\r\n"));
            Assert.That(written.Text, Does.EndWith("m=x-media 65535/1 X-PROTO/One fmt.one\r\n"));
            Assert.That(created.Timings[0].StartTime, Is.EqualTo("184467440737095516160"));
            Assert.That(created.Timings[0].StopTime, Is.EqualTo("184467440737095516161"));
        });
    }

    static void AssertConformingCanonicalDocument(string input, string expected)
    {
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, Diagnostics(parsed.Diagnostics));
            Assert.That(validation.Issues.Where(IsError), Is.Empty, Diagnostics(validation.Issues));
            Assert.That(written.Success, Is.True, Diagnostics(written.Diagnostics));
            Assert.That(written.Text, expected == input ? Is.EqualTo(expected) : Does.Contain(expected));
            Assert.That(new SdpParser().ParseText(written.Text, new SdpParseOptions()).Success, Is.True);
        });
    }

    static SdpValidationResult Validate(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Rfc8866 });
    }

    static bool IsError(SdpValidationIssue issue)
    {
        return issue.Severity == SdpValidationSeverity.Error || issue.Severity == SdpValidationSeverity.Fatal;
    }

    static string Header()
    {
        return HeaderBeforeTiming() + "t=0 0\r\n";
    }

    static string HeaderBeforeTiming()
    {
        return
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=RFC 8866 example\r\n" +
            "c=IN IP4 192.0.2.1\r\n";
    }

    static string HeaderWithoutConnection()
    {
        return
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=RFC 8866 example\r\n" +
            "t=0 0\r\n";
    }

    static string AudioMedia()
    {
        return AudioMedia(string.Empty);
    }

    static string AudioMedia(string connectionLines)
    {
        return "m=audio 49232 RTP/AVP 0\r\n" + connectionLines;
    }

    static string VideoMedia()
    {
        return "m=video 51372 RTP/AVP 96\r\na=rtpmap:96 H264/90000\r\n";
    }

    static string Diagnostics(IEnumerable<SdpDiagnostic> diagnostics)
    {
        return string.Join("|", diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string Diagnostics(IEnumerable<SdpValidationIssue> issues)
    {
        return string.Join("|", issues.Select(issue => issue.Code + ":" + issue.Detail));
    }
}
