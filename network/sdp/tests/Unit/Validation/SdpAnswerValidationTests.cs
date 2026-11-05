namespace sdp.Tests.Unit.Validation;

public class SdpAnswerValidationTests
{
    [Test]
    public void ExactOfferValidation_AcceptsNegotiatedSubsetsAndPayloadRemapping()
    {
        (SdpSessionDescription Offer, SdpSessionDescription Answer) pair = ValidPair();

        SdpValidationResult result = Validate(pair.Answer, pair.Offer);

        Assert.That(result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer), Is.Empty, string.Join("|", result.Issues.Select(issue => issue.Detail)));
    }

    [Test]
    public void ExactOfferValidation_RejectsChangedMediaCount()
    {
        (SdpSessionDescription Offer, SdpSessionDescription Answer) pair = ValidPair();
        pair.Answer.MediaDescriptions.Clear();

        SdpValidationResult result = Validate(pair.Answer, pair.Offer);

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.FieldName == 'm' && issue.Scope == SdpScope.Session && issue.Detail.Contains("exactly", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ExactOfferValidation_RejectsUnrelatedCodecDirectionTransportAndMediaId()
    {
        (SdpSessionDescription Offer, SdpSessionDescription Answer) codecPair = ValidPair();
        codecPair.Answer.MediaDescriptions[0].Attributes[1].Value = "96 X-NOVEL/48000/2";
        (SdpSessionDescription Offer, SdpSessionDescription Answer) directionPair = ValidPair();
        directionPair.Answer.MediaDescriptions[0].Attributes[0].Name = SdpAttributeNames.SendOnly;
        (SdpSessionDescription Offer, SdpSessionDescription Answer) transportPair = ValidPair();
        transportPair.Answer.MediaDescriptions[0].Protocol = SdpTransportProtocols.RtpAvpf;
        (SdpSessionDescription Offer, SdpSessionDescription Answer) mediaIdPair = ValidPair();
        FindAttribute(mediaIdPair.Answer.MediaDescriptions[0], SdpAttributeNames.MediaId).Value = "other";

        SdpValidationResult codec = Validate(codecPair.Answer, codecPair.Offer);
        SdpValidationResult direction = Validate(directionPair.Answer, directionPair.Offer);
        SdpValidationResult transport = Validate(transportPair.Answer, transportPair.Offer);
        SdpValidationResult mediaId = Validate(mediaIdPair.Answer, mediaIdPair.Offer);

        Assert.Multiple(() =>
        {
            Assert.That(codec.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.Detail.Contains("codec", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(direction.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.AttributeName == SdpAttributeNames.SendOnly), Is.True);
            Assert.That(transport.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.Detail.Contains("transport protocol", StringComparison.Ordinal)), Is.True);
            Assert.That(mediaId.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.AttributeName == SdpAttributeNames.MediaId), Is.True);
        });
    }

    [Test]
    public void ExactOfferValidation_RejectsIntroducedIceExtMapGroupAndRidCapabilities()
    {
        (SdpSessionDescription Offer, SdpSessionDescription Answer) pair = ValidPair();
        FindAttribute(pair.Answer.MediaDescriptions[0], SdpAttributeNames.IceOptions).Value = "trickle renomination";
        FindAttribute(pair.Answer.MediaDescriptions[0], SdpAttributeNames.ExtMap).Value = "1/recvonly urn:example:unoffered";
        FindAttribute(pair.Answer, SdpAttributeNames.Group).Value = "LS a";
        pair.Offer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.Rid, "offered send pt=120"));
        pair.Answer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.Rid, "introduced recv pt=96"));

        SdpValidationResult result = Validate(pair.Answer, pair.Offer);
        List<string> attributes = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer).Select(issue => issue.AttributeName).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(attributes, Does.Contain(SdpAttributeNames.IceOptions));
            Assert.That(attributes, Does.Contain(SdpAttributeNames.ExtMap));
            Assert.That(attributes, Does.Contain(SdpAttributeNames.Group));
            Assert.That(attributes, Does.Contain(SdpAttributeNames.Rid));
        });
    }

    [Test]
    public void ExactOfferValidation_RejectsChangedMulticastDestinationAndSctpFormat()
    {
        (SdpSessionDescription Offer, SdpSessionDescription Answer) multicastPair = ValidPair();
        multicastPair.Offer.Connection = MulticastConnection("224.2.1.1");
        multicastPair.Answer.Connection = MulticastConnection("224.2.1.2");
        (SdpSessionDescription Offer, SdpSessionDescription Answer) sctpPair = SctpPair();
        sctpPair.Answer.MediaDescriptions[0].Formats[0] = "unoffered-channel";

        SdpValidationResult multicast = Validate(multicastPair.Answer, multicastPair.Offer);
        SdpValidationResult sctp = Validate(sctpPair.Answer, sctpPair.Offer);

        Assert.Multiple(() =>
        {
            Assert.That(multicast.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.FieldName == 'c'), Is.True);
            Assert.That(sctp.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.FieldName == 'm' && issue.Detail.Contains("format", StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    static SdpValidationResult Validate(SdpSessionDescription answer, SdpSessionDescription offer)
    {
        return new SdpValidator().ValidateDescription(answer, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.CombinedStrict,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer,
            MaximumDiagnosticCount = 128
        });
    }

    static (SdpSessionDescription Offer, SdpSessionDescription Answer) ValidPair()
    {
        SdpSessionDescription offer = Session("192.0.2.1");
        SdpMediaDescription offerMedia = Media("audio", 4000, SdpTransportProtocols.RtpAvp, "120");
        offerMedia.Attributes.Add(PropertyAttribute(SdpAttributeNames.SendOnly));
        offerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.RtpMap, "120 X-CODEC/48000/2"));
        offerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "a"));
        offerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "offerufrag"));
        offerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "offer-password-1234567890"));
        offerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.IceOptions, "trickle"));
        offerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMap, "1/sendonly urn:example:extension"));
        offer.MediaDescriptions.Add(offerMedia);
        offer.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE a"));

        SdpSessionDescription answer = Session("192.0.2.2");
        SdpMediaDescription answerMedia = Media("audio", 5000, SdpTransportProtocols.RtpAvp, "96");
        answerMedia.Attributes.Add(PropertyAttribute(SdpAttributeNames.ReceiveOnly));
        answerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.RtpMap, "96 X-CODEC/48000/2"));
        answerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "a"));
        answerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "answerufrag"));
        answerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "answer-password-123456789"));
        answerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.IceOptions, "trickle"));
        answerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMap, "1/recvonly urn:example:extension"));
        answer.MediaDescriptions.Add(answerMedia);
        answer.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE a"));
        return (offer, answer);
    }

    static (SdpSessionDescription Offer, SdpSessionDescription Answer) SctpPair()
    {
        SdpSessionDescription offer = Session("192.0.2.1");
        SdpMediaDescription offerMedia = Media("application", 4000, SdpTransportProtocols.UdpDtlsSctp, SdpDataChannelTokens.WebRtcDataChannel);
        offerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.SctpPort, "5000"));
        offer.MediaDescriptions.Add(offerMedia);
        SdpSessionDescription answer = Session("192.0.2.2");
        SdpMediaDescription answerMedia = Media("application", 5000, SdpTransportProtocols.UdpDtlsSctp, SdpDataChannelTokens.WebRtcDataChannel);
        answerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.SctpPort, "6000"));
        answer.MediaDescriptions.Add(answerMedia);
        return (offer, answer);
    }

    static SdpSessionDescription Session(string address)
    {
        SdpSessionDescription description = new SdpSessionDescription
        {
            HasProtocolVersion = true,
            ProtocolVersion = 0,
            Origin = new SdpOrigin
            {
                Username = "-",
                SessionId = "1",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = address,
                AddressKind = SdpAddressKind.Ipv4
            },
            HasSessionName = true,
            SessionName = "answer-validation",
            Connection = UnicastConnection(address)
        };
        description.Timings.Add(new SdpTiming { StartTime = "0", StopTime = "0" });
        return description;
    }

    static SdpMediaDescription Media(string mediaType, int port, string protocol, string format)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = mediaType,
            MediaKind = mediaType == "audio" ? SdpMediaKind.Audio : SdpMediaKind.Application,
            Port = port,
            Protocol = protocol
        };
        media.Formats.Add(format);
        return media;
    }

    static SdpConnectionData UnicastConnection(string address)
    {
        return new SdpConnectionData
        {
            NetworkType = SdpNetworkTypes.Internet,
            AddressType = SdpAddressTypes.Ip4,
            Address = address,
            AddressKind = SdpAddressKind.Ipv4,
            AddressCount = 1,
            RawAddress = address
        };
    }

    static SdpConnectionData MulticastConnection(string address)
    {
        return new SdpConnectionData
        {
            NetworkType = SdpNetworkTypes.Internet,
            AddressType = SdpAddressTypes.Ip4,
            Address = address,
            AddressKind = SdpAddressKind.Ipv4Multicast,
            HasTimeToLive = true,
            TimeToLive = 127,
            AddressCount = 1,
            RawAddress = address + "/127"
        };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpAttribute FindAttribute(SdpMediaDescription media, string name)
    {
        return media.Attributes.Single(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    static SdpAttribute FindAttribute(SdpSessionDescription description, string name)
    {
        return description.Attributes.Single(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
