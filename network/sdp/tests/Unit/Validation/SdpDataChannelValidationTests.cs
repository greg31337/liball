namespace sdp.Tests.Unit.Validation;

public class SdpDataChannelValidationTests
{
    [Test]
    public void ModernDataChannelSignaling_AcceptsBoundsUnlimitedAndUnknownParameters()
    {
        SdpSessionDescription description = ModernDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("sctp-port", "5000"));
        media.Attributes.Add(ValueAttribute("max-message-size", "0"));
        media.Attributes.Add(ValueAttribute("dcmap", "0 subprotocol=\"msrp\";label=\"Messages\";ordered=false;max-retr=4294967295;priority=65535;X-Future=one;X-Future=two"));
        media.Attributes.Add(ValueAttribute("dcsa", "0 accept-types:message/cpim text/plain"));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void AbsentMaximumMessageSize_IsAValidDefault()
    {
        SdpSessionDescription description = ModernDescription();
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("sctp-port", "5000"));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Unknown, null);
        SdpSctpDescription resolved = new SdpDataChannelService().ResolveMediaSctp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(resolved.HasMaximumMessageSize, Is.False);
            Assert.That(resolved.MaximumMessageSize, Is.EqualTo(SdpDataChannelTokens.DefaultMaximumMessageSize));
        });
    }

    [Test]
    public void LegacySctpMap_IsAcceptedOnlyOnLegacySctpTransport()
    {
        SdpSessionDescription legacy = LegacyDescription();
        legacy.MediaDescriptions[0].Attributes.Add(ValueAttribute("sctpmap", "5000 webrtc-datachannel 16"));
        SdpSessionDescription modern = ModernDescription();
        modern.MediaDescriptions[0].Attributes.Add(ValueAttribute("sctp-port", "5000"));
        modern.MediaDescriptions[0].Attributes.Add(ValueAttribute("sctpmap", "5000 webrtc-datachannel 16"));

        SdpValidationResult legacyResult = Validate(legacy, SdpNegotiationRole.Unknown, null);
        SdpValidationResult modernResult = Validate(modern, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(legacyResult.Success, Is.True);
            Assert.That(modernResult.Success, Is.False);
            Assert.That(modernResult.Issues.Any(issue => issue.AttributeName == "sctpmap" && issue.Code == SdpDiagnosticCodes.ValidationDataChannel), Is.True);
        });
    }

    [Test]
    public void SessionScopeAndWrongTransport_AreReportedWithSourcePaths()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpAttribute sessionPort = ValueAttribute("sctp-port", "5000");
        sessionPort.Location.LineNumber = 4;
        description.Attributes.Add(sessionPort);
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "audio",
            MediaKind = SdpMediaKind.Audio,
            Protocol = SdpTransportProtocols.RtpAvp
        };
        media.Formats.Add("0");
        media.Attributes.Add(ValueAttribute("max-message-size", "1000"));
        media.Attributes.Add(ValueAttribute("dcmap", "1"));
        media.Attributes.Add(ValueAttribute("dcsa", "1 inactive"));
        media.Attributes.Add(ValueAttribute("sctpmap", "5000 webrtc-datachannel"));
        description.MediaDescriptions.Add(media);

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope && issue.Scope == SdpScope.Session && issue.Location.LineNumber == 4), Is.True);
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationDataChannel && issue.MediaIndex == 0), Is.GreaterThanOrEqualTo(4));
            Assert.That(result.Issues.All(issue => issue.Path.FieldName == 'a'), Is.True);
        });
    }

    [Test]
    public void ModernTransport_RequiresApplicationMediaValidPortAndDataChannelFormat()
    {
        SdpSessionDescription description = ModernDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.MediaType = "audio";
        media.MediaKind = SdpMediaKind.Audio;
        media.Formats.Clear();
        media.Formats.Add("custom-application");
        media.Attributes.Add(ValueAttribute("dcmap", "1"));
        media.Attributes.Add(ValueAttribute("dcsa", "1 inactive"));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.FieldName == 'm' && issue.Detail.Contains("application media", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == "sctp-port" && issue.Detail.Contains("requires one valid", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Count(issue => issue.Detail.Contains("not applicable", StringComparison.Ordinal)), Is.EqualTo(2));
        });
    }

    [Test]
    public void DuplicateSingletonsAndMalformedValues_RetainSpecificDiagnostics()
    {
        SdpSessionDescription description = ModernDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("sctp-port", "bad"));
        media.Attributes.Add(ValueAttribute("sctp-port", "5000"));
        media.Attributes.Add(ValueAttribute("max-message-size", "1000"));
        media.Attributes.Add(ValueAttribute("max-message-size", "2000"));
        media.Attributes.Add(ValueAttribute("dcmap", "1 label=plain"));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == "sctp-port" && issue.Code == SdpDiagnosticCodes.ParseInvalidNumber), Is.True);
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(2));
            Assert.That(result.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Code == SdpDiagnosticCodes.ValidationAttribute), Is.True);
        });
    }

    [TestCase("65535", "zero through 65534")]
    [TestCase("1 ordered=maybe", "true or false")]
    [TestCase("1 max-retr=01", "unsigned 32-bit")]
    [TestCase("1 max-retr=4294967296", "unsigned 32-bit")]
    [TestCase("1 max-time=4294967296", "unsigned 32-bit")]
    [TestCase("1 priority=65536", "unsigned 16-bit")]
    [TestCase("1 max-retr=5;max-time=10", "mutually exclusive")]
    [TestCase("1 ordered=true;ordered=false", "must not be repeated")]
    public void InvalidChannelParameters_AreReported(string mapValue, string detailFragment)
    {
        SdpSessionDescription description = ModernDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("sctp-port", "5000"));
        media.Attributes.Add(ValueAttribute("dcmap", mapValue));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Detail.Contains(detailFragment, StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void DuplicateChannelIdsAndMissingDcsaReferences_AreReported()
    {
        SdpSessionDescription description = ModernDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("sctp-port", "5000"));
        media.Attributes.Add(ValueAttribute("dcmap", "2"));
        media.Attributes.Add(ValueAttribute("dcmap", "2 label=\"duplicate\""));
        media.Attributes.Add(ValueAttribute("dcsa", "2 inactive"));
        media.Attributes.Add(ValueAttribute("dcsa", "4 inactive"));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality && issue.Detail.Contains("stream identifier", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == "dcsa" && issue.Path.OccurrenceIndex == 1 && issue.Detail.Contains("reference", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == "dcsa" && issue.Path.OccurrenceIndex == 0), Is.False);
        });
    }

    [TestCase("active", 2, 3)]
    [TestCase("passive", 1, 2)]
    public void OffererStreamIdentifiers_FollowItsDtlsRole(string setupRole, int validId, int invalidId)
    {
        SdpSessionDescription description = ModernDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("sctp-port", "5000"));
        media.Attributes.Add(ValueAttribute("setup", setupRole));
        media.Attributes.Add(ValueAttribute("dcmap", validId.ToString()));
        media.Attributes.Add(ValueAttribute("dcmap", invalidId.ToString()));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo("dcmap"));
            Assert.That(result.Issues[0].Detail, Does.Contain(setupRole == "active" ? "even" : "odd"));
        });
    }

    [Test]
    public void CompatibleAnswer_RetainsOfferedChannelsReliabilityAndTransport()
    {
        SdpSessionDescription offer = OfferWithChannel(2, "5");
        SdpSessionDescription answer = AnswerWithChannel(2, "5", "passive");

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void AnswerCannotChangeTransportIntroduceChannelsOrAlterReliability()
    {
        SdpSessionDescription offer = OfferWithChannel(2, "5");
        SdpSessionDescription answer = AnswerWithChannel(2, "6", "passive");
        answer.MediaDescriptions[0].Protocol = SdpTransportProtocols.TcpDtlsSctp;
        answer.MediaDescriptions[0].Attributes.Add(ValueAttribute("dcmap", "4"));

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.FieldName == 'm' && issue.Detail.Contains("retain the transport", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Detail.Contains("max-retr", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Detail.Contains("absent from the offer", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void AnswerParityUsesTheOfferersOppositeDtlsRole()
    {
        SdpSessionDescription offer = OfferWithChannel(3, "5");
        SdpSessionDescription answer = AnswerWithChannel(3, "5", "passive");

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == "dcmap" && issue.Detail.Contains("even identifier", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void ZeroPortOfferAndRejectedAnswerRequireZeroAnswerSctpPort()
    {
        SdpSessionDescription offer = OfferWithChannel(2, "5");
        ReplaceFirstAttributeValue(offer.MediaDescriptions[0], SdpAttributeNames.SctpPort, "0");
        SdpSessionDescription answer = AnswerWithChannel(2, "5", "passive");
        answer.MediaDescriptions[0].Port = 0;
        SdpSessionDescription cleanRejection = AnswerWithChannel(2, "5", "passive");
        cleanRejection.MediaDescriptions[0].Port = 0;
        cleanRejection.MediaDescriptions[0].Attributes.Clear();

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);
        SdpValidationResult cleanResult = Validate(cleanRejection, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Count(issue => issue.AttributeName == "sctp-port" && issue.Detail.Contains("zero SCTP port", StringComparison.Ordinal)), Is.EqualTo(2));
            Assert.That(cleanResult.Success, Is.True);
        });
    }

    [TestCase(SdpValidationProfile.DataChannel, true)]
    [TestCase(SdpValidationProfile.Rfc8866, false)]
    public void DataChannelRules_RunOnlyForApplicableProfiles(SdpValidationProfile profile, bool expectedIssue)
    {
        SdpSessionDescription description = ModernDescription();
        SdpValidationOptions options = new SdpValidationOptions { Profile = profile };

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, options);

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDataChannel), Is.EqualTo(expectedIssue));
    }

    static SdpValidationResult Validate(SdpSessionDescription description, SdpNegotiationRole role, SdpSessionDescription? offer)
    {
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.DataChannel,
            NegotiationRole = role,
            OfferDescription = offer
        };
        return new SdpValidator().ValidateDescription(description, options);
    }

    static SdpSessionDescription ModernDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Application,
            MediaKind = SdpMediaKind.Application,
            Port = 10000,
            Protocol = SdpTransportProtocols.UdpDtlsSctp
        };
        media.Formats.Add(SdpDataChannelTokens.WebRtcDataChannel);
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpSessionDescription LegacyDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Application,
            MediaKind = SdpMediaKind.Application,
            Port = 10000,
            Protocol = "DTLS/SCTP"
        };
        media.Formats.Add("5000");
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpSessionDescription OfferWithChannel(int channelId, string retransmissions)
    {
        SdpSessionDescription description = ModernDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("sctp-port", "5000"));
        media.Attributes.Add(ValueAttribute("setup", "actpass"));
        media.Attributes.Add(ValueAttribute("dcmap", channelId + " subprotocol=\"msrp\";max-retr=" + retransmissions));
        return description;
    }

    static SdpSessionDescription AnswerWithChannel(int channelId, string retransmissions, string setupRole)
    {
        SdpSessionDescription description = ModernDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("sctp-port", "5002"));
        media.Attributes.Add(ValueAttribute("setup", setupRole));
        media.Attributes.Add(ValueAttribute("dcmap", channelId + " subprotocol=\"msrp\";max-retr=" + retransmissions));
        return description;
    }

    static void ReplaceFirstAttributeValue(SdpMediaDescription media, string name, string value)
    {
        SdpAttribute attribute = media.Attributes.First(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        attribute.Value = value;
        attribute.RawText = name + ":" + value;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
