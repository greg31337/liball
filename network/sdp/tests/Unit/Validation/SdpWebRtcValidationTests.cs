namespace sdp.Tests.Unit.Validation;

public class SdpWebRtcValidationTests
{
    [Test]
    public void CompleteSecureRtpDescription_IsValidForWebRtc()
    {
        SdpSessionDescription description = SecureRtpDescription();

        SdpValidationResult result = ValidateWebRtc(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void ActiveRtpMedia_RequiresSecureTransportAndWebRtcAttributes()
    {
        SdpSessionDescription description = SecureRtpDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Protocol = SdpTransportProtocols.RtpAvp;
        media.Attributes.Clear();
        description.Attributes.Clear();

        SdpValidationResult result = ValidateWebRtc(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationMedia && issue.FieldName == 'm'), Is.True);
            Assert.That(HasMissingAttributeIssue(result, SdpAttributeNames.MediaId), Is.True);
            Assert.That(HasMissingAttributeIssue(result, SdpAttributeNames.IceUsernameFragment), Is.True);
            Assert.That(HasMissingAttributeIssue(result, SdpAttributeNames.IcePassword), Is.True);
            Assert.That(HasMissingAttributeIssue(result, SdpAttributeNames.Fingerprint), Is.True);
            Assert.That(HasMissingAttributeIssue(result, SdpAttributeNames.Setup), Is.True);
            Assert.That(HasMissingAttributeIssue(result, SdpAttributeNames.RtcpMux), Is.True);
            Assert.That(HasMissingAttributeIssue(result, SdpAttributeNames.MediaStreamId), Is.True);
        });
    }

    [Test]
    public void RejectedRtpMedia_KeepsMidButDoesNotRequireActiveTransportState()
    {
        SdpSessionDescription description = SecureRtpDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Port = 0;
        media.Protocol = SdpTransportProtocols.RtpAvp;
        media.Attributes.Clear();
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "audio"));
        description.Attributes.Clear();

        SdpValidationResult result = ValidateWebRtc(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void ReceiveOnlyRtpMedia_DoesNotRequireAnMsidAssociation()
    {
        SdpSessionDescription description = SecureRtpDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        RemoveAttributes(media.Attributes, SdpAttributeNames.MediaStreamId);
        media.Attributes.Add(PropertyAttribute(SdpAttributeNames.ReceiveOnly));

        SdpValidationResult result = ValidateWebRtc(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void MultipleActiveMedia_RequireOneBundleGroupAndMediaMidExtensions()
    {
        SdpSessionDescription description = SecureRtpDescription();
        SdpMediaDescription video = SecureRtpMedia("video", SdpMediaKind.Video, "video", "31");
        description.MediaDescriptions.Add(video);

        SdpValidationResult unbundled = ValidateWebRtc(description);
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        SdpValidationResult missingExtensions = ValidateWebRtc(description);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMap, "1 " + SdpHeaderExtensionUris.MediaId));
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMap, "1 " + SdpHeaderExtensionUris.MediaId));
        SdpValidationResult bundled = ValidateWebRtc(description);

        Assert.Multiple(() =>
        {
            Assert.That(unbundled.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle && issue.AttributeName == SdpAttributeNames.Group), Is.True);
            Assert.That(missingExtensions.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle && issue.AttributeName == SdpAttributeNames.ExtMap), Is.EqualTo(2));
            Assert.That(bundled.Success, Is.True);
            Assert.That(bundled.Issues, Is.Empty);
        });
    }

    [Test]
    public void MissingMidInBundle_IsReportedOnceAcrossGroupAndWebRtcRules()
    {
        SdpSessionDescription description = SecureRtpDescription();
        SdpMediaDescription video = SecureRtpMedia("video", SdpMediaKind.Video, "video", "31");
        description.MediaDescriptions.Add(video);
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        RemoveAttributes(video.Attributes, SdpAttributeNames.MediaId);

        SdpValidationResult result = ValidateWebRtc(description);

        Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationGroup && issue.AttributeName == SdpAttributeNames.MediaId && issue.MediaIndex == 1), Is.EqualTo(1));
    }

    [Test]
    public void WebRtcDataChannel_RequiresModernShapeAndTcpConnectionSignaling()
    {
        SdpSessionDescription validDescription = SecureDataChannelDescription(SdpTransportProtocols.UdpDtlsSctp);
        SdpSessionDescription legacyDescription = SecureDataChannelDescription("DTLS/SCTP");
        SdpSessionDescription tcpDescription = SecureDataChannelDescription(SdpTransportProtocols.TcpDtlsSctp);

        SdpValidationResult valid = ValidateWebRtc(validDescription);
        SdpValidationResult legacy = ValidateWebRtc(legacyDescription);
        SdpValidationResult tcpWithoutConnection = ValidateWebRtc(tcpDescription);
        tcpDescription.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.Connection, "new"));
        SdpValidationResult tcpWithConnection = ValidateWebRtc(tcpDescription);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(legacy.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDataChannel && issue.FieldName == 'm'), Is.True);
            Assert.That(tcpWithoutConnection.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDataChannel && issue.AttributeName == SdpAttributeNames.Connection), Is.True);
            Assert.That(tcpWithConnection.Success, Is.True);
        });
    }

    [TestCase(SdpValidationProfile.WebRtc, true)]
    [TestCase(SdpValidationProfile.CombinedStrict, true)]
    [TestCase(SdpValidationProfile.Rtp, false)]
    public void WebRtcRequirements_AreRoutedOnlyToApplicableProfiles(SdpValidationProfile profile, bool expectedIssue)
    {
        SdpSessionDescription description = SecureRtpDescription();
        description.MediaDescriptions[0].Protocol = SdpTransportProtocols.RtpAvp;
        description.MediaDescriptions[0].Attributes.Clear();
        description.Attributes.Clear();

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = profile });

        Assert.That(result.Issues.Any(issue => issue.Detail.Contains("Active WebRTC RTP", StringComparison.Ordinal)), Is.EqualTo(expectedIssue));
    }

    static SdpSessionDescription SecureRtpDescription()
    {
        SdpSessionDescription description = BaseDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "ufragOne"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "passwordPasswordPassword12"));
        description.MediaDescriptions.Add(SecureRtpMedia("audio", SdpMediaKind.Audio, "audio", "0"));
        return description;
    }

    static SdpMediaDescription SecureRtpMedia(string mediaType, SdpMediaKind mediaKind, string mediaId, string format)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = mediaType,
            MediaKind = mediaKind,
            Port = 9,
            Protocol = SdpTransportProtocols.UdpTlsRtpSavpf
        };
        media.Formats.Add(format);
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, mediaId));
        media.Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaStreamId, "stream " + mediaId + "Track"));
        AddDtlsAttributes(media);
        return media;
    }

    static SdpSessionDescription SecureDataChannelDescription(string protocol)
    {
        SdpSessionDescription description = BaseDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "ufragOne"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "passwordPasswordPassword12"));
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Application,
            MediaKind = SdpMediaKind.Application,
            Port = 9,
            Protocol = protocol
        };
        media.Formats.Add(SdpDataChannelTokens.WebRtcDataChannel);
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "data"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.SctpPort, "5000"));
        AddDtlsAttributes(media);
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpSessionDescription BaseDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription
        {
            HasProtocolVersion = true,
            ProtocolVersion = 0,
            Origin = new SdpOrigin
            {
                Username = "-",
                SessionId = "1000000000",
                SessionVersion = "1000000001",
                NetworkType = "IN",
                AddressType = "IP4",
                UnicastAddress = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            HasSessionName = true,
            SessionName = "WebRTC",
            Connection = new SdpConnectionData
            {
                NetworkType = "IN",
                AddressType = "IP4",
                Address = "0.0.0.0",
                AddressKind = SdpAddressKind.Ipv4
            }
        };
        description.Timings.Add(new SdpTiming { StartTime = "0", StopTime = "0" });
        return description;
    }

    static void AddDtlsAttributes(SdpMediaDescription media)
    {
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Fingerprint, "sha-256 " + Fingerprint(32)));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "actpass"));
    }

    static string Fingerprint(int byteCount)
    {
        return string.Join(":", Enumerable.Repeat("AA", byteCount));
    }

    static bool HasMissingAttributeIssue(SdpValidationResult result, string attributeName)
    {
        return result.Issues.Any(issue => issue.AttributeName == attributeName);
    }

    static void RemoveAttributes(List<SdpAttribute> attributes, string name)
    {
        attributes.RemoveAll(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }

    static SdpValidationResult ValidateWebRtc(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.WebRtc });
    }
}
