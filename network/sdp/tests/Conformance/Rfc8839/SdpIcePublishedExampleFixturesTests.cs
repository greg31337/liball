namespace sdp.Tests.Conformance.Rfc8839;

public class SdpIcePublishedExampleFixturesTests
{
    const string OfferUsername = "8hhY";
    const string OfferPassword = "asd88fgpdd777uzjYhagZg";
    const string AnswerUsername = "9uB6";
    const string AnswerPassword = "YH75Fviy6338Vbrhrlp8Yh";

    [Test]
    public void PublishedIpv6Offer_ParsesValidatesAndCanonicalRoundTrips()
    {
        string text =
            "v=0\r\n" +
            "o=jdoe 2890844526 2890842807 IN IP6 fe80::6676:baff:fe9c:ee4a\r\n" +
            "s=-\r\n" +
            "c=IN IP6 2001:db8:8101:3a55:4858:a2a9:22ff:99b9\r\n" +
            "t=0 0\r\n" +
            "a=ice-options:ice2\r\n" +
            "a=ice-pacing:50\r\n" +
            "a=ice-pwd:" + OfferPassword + "\r\n" +
            "a=ice-ufrag:" + OfferUsername + "\r\n" +
            "m=audio 45664 RTP/AVP 0\r\n" +
            "b=RS:0\r\n" +
            "b=RR:0\r\n" +
            "a=rtpmap:0 PCMU/8000\r\n" +
            "a=candidate:1 1 UDP 2130706431 fe80::6676:baff:fe9c:ee4a 8998 typ host\r\n" +
            "a=candidate:2 1 UDP 1694498815 2001:db8:8101:3a55:4858:a2a9:22ff:99b9 45664 typ srflx raddr fe80::6676:baff:fe9c:ee4a rport 8998\r\n";

        SdpIceDescription resolved = AssertConformingRoundTrip(text);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Credentials!.UsernameFragment, Is.EqualTo(OfferUsername));
            Assert.That(resolved.Candidates, Has.Count.EqualTo(2));
            Assert.That(resolved.Candidates.Select(candidate => candidate.CandidateType), Is.EqualTo(new[] { SdpIceCandidateType.Host, SdpIceCandidateType.ServerReflexive }));
            Assert.That(resolved.Candidates[1].RelatedAddress, Is.EqualTo("fe80::6676:baff:fe9c:ee4a"));
            Assert.That(resolved.Candidates[1].RelatedPort, Is.EqualTo(8998));
        });
    }

    [Test]
    public void PublishedIpv4Answer_ParsesValidatesAndCanonicalRoundTrips()
    {
        string text =
            "v=0\r\n" +
            "o=bob 2808844564 2808844564 IN IP4 192.0.2.1\r\n" +
            "s=-\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n" +
            "a=ice-options:ice2\r\n" +
            "a=ice-pacing:50\r\n" +
            "a=ice-pwd:" + AnswerPassword + "\r\n" +
            "a=ice-ufrag:" + AnswerUsername + "\r\n" +
            "m=audio 3478 RTP/AVP 0\r\n" +
            "b=RS:0\r\n" +
            "b=RR:0\r\n" +
            "a=rtpmap:0 PCMU/8000\r\n" +
            "a=candidate:1 1 UDP 2130706431 192.0.2.1 3478 typ host\r\n";

        SdpIceDescription resolved = AssertConformingRoundTrip(text);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Credentials!.UsernameFragment, Is.EqualTo(AnswerUsername));
            Assert.That(resolved.Credentials.Password, Is.EqualTo(AnswerPassword));
            Assert.That(resolved.Candidates, Has.Count.EqualTo(1));
            Assert.That(resolved.Candidates[0].Address, Is.EqualTo("192.0.2.1"));
            Assert.That(resolved.Candidates[0].Port, Is.EqualTo(3478));
        });
    }

    [Test]
    public void PublishedCandidateAndRemoteCandidates_KeepEveryEndpointSemantic()
    {
        string text =
            "v=0\r\n" +
            "o=jdoe 2890844526 2890842807 IN IP4 203.0.113.141\r\n" +
            "s=-\r\n" +
            "c=IN IP4 192.0.2.3\r\n" +
            "t=0 0\r\n" +
            "a=ice-options:ice2\r\n" +
            "a=ice-pacing:50\r\n" +
            "a=ice-pwd:" + OfferPassword + "\r\n" +
            "a=ice-ufrag:" + OfferUsername + "\r\n" +
            "m=audio 45664 RTP/AVP 0\r\n" +
            "a=rtcp:45665 IN IP4 192.0.2.3\r\n" +
            "a=rtpmap:0 PCMU/8000\r\n" +
            "a=candidate:2 1 UDP 1694498815 192.0.2.3 45664 typ srflx raddr 203.0.113.141 rport 8998\r\n" +
            "a=candidate:2 2 UDP 1694498814 192.0.2.3 45665 typ srflx raddr 203.0.113.141 rport 8999\r\n" +
            "a=remote-candidates:1 192.0.2.3 45664 2 192.0.2.3 45665\r\n";

        SdpIceDescription resolved = AssertConformingRoundTrip(text);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Candidates, Has.Count.EqualTo(2));
            Assert.That(resolved.Candidates.Select(candidate => candidate.Component), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(resolved.RemoteCandidates, Has.Count.EqualTo(2));
            Assert.That(resolved.RemoteCandidates.Select(candidate => candidate.Port), Is.EqualTo(new[] { 45664, 45665 }));
        });
    }

    [Test]
    public void IceMismatchAnswer_OmitsCandidatesAndRoundTripsItsFallbackSemantic()
    {
        string text = BasicDocument("192.0.2.9", 5000,
            "a=ice-pwd:" + AnswerPassword + "\r\n" +
            "a=ice-ufrag:" + AnswerUsername + "\r\n" +
            "a=ice-mismatch\r\n");

        SdpIceDescription resolved = AssertConformingRoundTrip(text);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.IceMismatch, Is.True);
            Assert.That(resolved.Candidates, Is.Empty);
        });
    }

    [Test]
    public void IceMismatchAnswer_WithCandidateRetainsAValidationFailureAcrossRoundTrip()
    {
        string text = BasicDocument("192.0.2.9", 5000,
            "a=ice-pwd:" + AnswerPassword + "\r\n" +
            "a=ice-ufrag:" + AnswerUsername + "\r\n" +
            "a=ice-mismatch\r\n" +
            "a=candidate:mismatch 1 UDP 1 192.0.2.9 5000 typ host\r\n");

        AssertInvalidRoundTrip(text, "ice-mismatch", "omit candidate");
    }

    [Test]
    public void IceRestart_ChangesBothCredentialsAndClearsStaleGatheringState()
    {
        string previousText = BasicDocument("0.0.0.0", 9,
            "a=ice-pwd:" + OfferPassword + "\r\n" +
            "a=ice-ufrag:" + OfferUsername + "\r\n" +
            "a=candidate:old 1 UDP 1 192.0.2.3 45664 typ host\r\n" +
            "a=remote-candidates:1 192.0.2.3 45664\r\n" +
            "a=end-of-candidates\r\n");
        string currentText = BasicDocument("0.0.0.0", 9,
            "a=ice-pwd:" + AnswerPassword + "\r\n" +
            "a=ice-ufrag:" + AnswerUsername + "\r\n");
        SdpFixtureRoundTripAssert.AssertCanonical(previousText, new SdpParseOptions());
        SdpFixtureRoundTripAssert.AssertCanonical(currentText, new SdpParseOptions());
        SdpParseResult previous = new SdpParser().ParseText(previousText, new SdpParseOptions());
        SdpParseResult current = new SdpParser().ParseText(currentText, new SdpParseOptions());
        ISdpIceService service = new SdpIceService();

        bool detected = service.IsIceRestart(previous.Description!, 0, current.Description!, 0);
        service.RestartIce(previous.Description!, 0, new SdpIceCredentials { UsernameFragment = AnswerUsername, Password = AnswerPassword });
        SdpIceDescription restarted = service.ResolveMediaIce(previous.Description!, 0);
        SdpValidationResult validation = Validate(previous.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(previous.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = new SdpParser().ParseText(written.Text, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(detected, Is.True);
            Assert.That(restarted.Credentials!.UsernameFragment, Is.EqualTo(AnswerUsername));
            Assert.That(restarted.Credentials.Password, Is.EqualTo(AnswerPassword));
            Assert.That(restarted.Candidates, Is.Empty);
            Assert.That(restarted.RemoteCandidates, Is.Empty);
            Assert.That(restarted.EndOfCandidates, Is.False);
            Assert.That(validation.Success, Is.True, ValidationText(validation));
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.Success, Is.True);
            Assert.That(Compare(previous.Description!, reparsed.Description!).Equal, Is.True);
        });
    }

    [Test]
    public void IceLiteAnswer_UsesOnlyHostCandidatesAndCanonicalRoundTrips()
    {
        string text =
            "v=0\r\n" +
            "o=bob 2808844564 2808844564 IN IP4 192.0.2.1\r\n" +
            "s=-\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n" +
            "a=ice-lite\r\n" +
            "a=ice-options:ice2\r\n" +
            "a=ice-pwd:" + AnswerPassword + "\r\n" +
            "a=ice-ufrag:" + AnswerUsername + "\r\n" +
            "m=audio 3478 RTP/AVP 0\r\n" +
            "b=RS:0\r\n" +
            "b=RR:0\r\n" +
            "a=rtpmap:0 PCMU/8000\r\n" +
            "a=candidate:1 1 UDP 2130706431 192.0.2.1 3478 typ host\r\n";

        SdpIceDescription resolved = AssertConformingRoundTrip(text);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.IceLite, Is.True);
            Assert.That(resolved.Candidates, Has.Count.EqualTo(1));
            Assert.That(resolved.Candidates[0].CandidateType, Is.EqualTo(SdpIceCandidateType.Host));
        });
    }

    [TestCase("192.0.2.1", 5000, "a=candidate:default 1 UDP 1 192.0.2.1 5000 typ host\r\n", true)]
    [TestCase("192.0.2.1", 5000, "a=candidate:other 1 UDP 1 192.0.2.2 5000 typ host\r\n", false)]
    [TestCase("0.0.0.0", 9, "", true)]
    [TestCase("::", 9, "", true)]
    [TestCase("peer.example", 5000, "a=candidate:resolved 1 UDP 1 192.0.2.2 5000 typ host\r\n", true)]
    public void DefaultDestinationFixture_ImplementsRfc8839Exceptions(string address, int port, string candidateLine, bool expectedValid)
    {
        string text = BasicDocument(address, port,
            "a=ice-pwd:" + OfferPassword + "\r\n" +
            "a=ice-ufrag:" + OfferUsername + "\r\n" +
            candidateLine);
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(text, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = new SdpParser().ParseText(written.Text, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(validation.Success, Is.EqualTo(expectedValid), ValidationText(validation));
            Assert.That(written.Success, Is.True, WriteDiagnosticText(written));
            Assert.That(written.Text, Is.EqualTo(text));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(Validate(reparsed.Description!).Success, Is.EqualTo(expectedValid));
            Assert.That(Compare(parsed.Description!, reparsed.Description!).Equal, Is.True);
        });
    }

    [TestCase("b=RS:0\r\nb=RR:0\r\n", true)]
    [TestCase("b=RS:0\r\n", false)]
    [TestCase("b=RR:0\r\n", false)]
    public void RtcpBandwidthFixture_ControlsComponentTwoRequirement(string bandwidthLines, bool expectedValid)
    {
        string text =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=-\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n" +
            "a=ice-options:ice2\r\n" +
            "a=ice-pwd:" + OfferPassword + "\r\n" +
            "a=ice-ufrag:" + OfferUsername + "\r\n" +
            "m=audio 5000 RTP/AVP 0\r\n" +
            bandwidthLines +
            "a=candidate:rtp 1 UDP 1 192.0.2.1 5000 typ host\r\n";
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(text, new SdpParseOptions());

        SdpValidationResult validation = Validate(parsed.Description!);

        Assert.That(validation.Success, Is.EqualTo(expectedValid), ValidationText(validation));
    }

    static SdpIceDescription AssertConformingRoundTrip(string text)
    {
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(text, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = new SdpParser().ParseText(written.Text, new SdpParseOptions());
        SdpValidationResult roundTripValidation = Validate(reparsed.Description!);
        SdpComparisonResult comparison = Compare(parsed.Description!, reparsed.Description!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, DiagnosticText(parsed));
            Assert.That(validation.Success, Is.True, ValidationText(validation));
            Assert.That(written.Success, Is.True, WriteDiagnosticText(written));
            Assert.That(written.Text, Is.EqualTo(text));
            Assert.That(reparsed.Success, Is.True, DiagnosticText(reparsed));
            Assert.That(roundTripValidation.Success, Is.True, ValidationText(roundTripValidation));
            Assert.That(comparison.Equal, Is.True, DifferenceText(comparison));
        });
        return new SdpIceService().ResolveMediaIce(reparsed.Description!, 0);
    }

    static void AssertInvalidRoundTrip(string text, string attributeName, string detail)
    {
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(text, new SdpParseOptions());
        SdpValidationResult validation = Validate(parsed.Description!);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = new SdpParser().ParseText(written.Text, new SdpParseOptions());
        SdpValidationResult roundTripValidation = Validate(reparsed.Description!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, DiagnosticText(parsed));
            Assert.That(validation.Issues.Any(issue => issue.AttributeName == attributeName && issue.Detail.Contains(detail, StringComparison.OrdinalIgnoreCase)), Is.True, ValidationText(validation));
            Assert.That(written.Success, Is.True, WriteDiagnosticText(written));
            Assert.That(written.Text, Is.EqualTo(text));
            Assert.That(reparsed.Success, Is.True, DiagnosticText(reparsed));
            Assert.That(roundTripValidation.Issues.Any(issue => issue.AttributeName == attributeName && issue.Detail.Contains(detail, StringComparison.OrdinalIgnoreCase)), Is.True, ValidationText(roundTripValidation));
            Assert.That(Compare(parsed.Description!, reparsed.Description!).Equal, Is.True);
        });
    }

    static string BasicDocument(string address, int port, string mediaAttributes)
    {
        string addressType = address.Contains(':', StringComparison.Ordinal) ? "IP6" : "IP4";
        return
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.10\r\n" +
            "s=-\r\n" +
            "c=IN " + addressType + " " + address + "\r\n" +
            "t=0 0\r\n" +
            "a=ice-options:ice2\r\n" +
            "m=application " + port + " UDP data\r\n" +
            mediaAttributes;
    }

    static SdpValidationResult Validate(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Ice });
    }

    static SdpComparisonResult Compare(SdpSessionDescription first, SdpSessionDescription second)
    {
        return new SdpComparer().CompareDescriptions(first, second, new SdpComparisonOptions
        {
            CompareSemanticValues = true,
            IgnoreInsignificantOrdering = true
        });
    }

    static string DiagnosticText(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string WriteDiagnosticText(SdpWriteResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string ValidationText(SdpValidationResult result)
    {
        return string.Join("|", result.Issues.Select(issue => issue.AttributeName + ":" + issue.Detail));
    }

    static string DifferenceText(SdpComparisonResult result)
    {
        return string.Join("|", result.Differences.Select(difference => difference.Path.AttributeName + ":" + difference.Detail));
    }
}
