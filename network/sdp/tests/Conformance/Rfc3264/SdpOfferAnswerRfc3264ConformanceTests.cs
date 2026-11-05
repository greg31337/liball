namespace sdp.Tests.Conformance.Rfc3264;

public class SdpOfferAnswerRfc3264ConformanceTests
{
    [TestCase(SdpDirection.SendReceive, "sendrecv")]
    [TestCase(SdpDirection.SendOnly, "sendonly")]
    [TestCase(SdpDirection.ReceiveOnly, "recvonly")]
    [TestCase(SdpDirection.Inactive, "inactive")]
    public void InitialOffer_AdvertisesConfiguredDirection(SdpDirection direction, string attributeName)
    {
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(Capability(direction));

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateInitialOffer(Session("192.0.2.1", "0", "0"), new SdpNegotiationOptions { LocalCapabilities = capabilities });

        Assert.Multiple(() =>
        {
            Assert.That(result.Media[0].OfferedDirection, Is.EqualTo(direction));
            Assert.That(result.Media[0].Direction, Is.EqualTo(direction));
            Assert.That(result.Media[0].EffectiveMedia!.Direction, Is.EqualTo(direction));
            Assert.That(result.Description!.MediaDescriptions[0].Attributes.Any(attribute => attribute.Name == attributeName), Is.True);
        });
    }

    [TestCase(SdpDirection.SendReceive, SdpDirection.SendReceive, SdpDirection.SendReceive)]
    [TestCase(SdpDirection.SendReceive, SdpDirection.SendOnly, SdpDirection.SendOnly)]
    [TestCase(SdpDirection.SendReceive, SdpDirection.ReceiveOnly, SdpDirection.ReceiveOnly)]
    [TestCase(SdpDirection.SendReceive, SdpDirection.Inactive, SdpDirection.Inactive)]
    [TestCase(SdpDirection.SendOnly, SdpDirection.SendReceive, SdpDirection.ReceiveOnly)]
    [TestCase(SdpDirection.SendOnly, SdpDirection.SendOnly, SdpDirection.Inactive)]
    [TestCase(SdpDirection.SendOnly, SdpDirection.ReceiveOnly, SdpDirection.ReceiveOnly)]
    [TestCase(SdpDirection.SendOnly, SdpDirection.Inactive, SdpDirection.Inactive)]
    [TestCase(SdpDirection.ReceiveOnly, SdpDirection.SendReceive, SdpDirection.SendOnly)]
    [TestCase(SdpDirection.ReceiveOnly, SdpDirection.SendOnly, SdpDirection.SendOnly)]
    [TestCase(SdpDirection.ReceiveOnly, SdpDirection.ReceiveOnly, SdpDirection.Inactive)]
    [TestCase(SdpDirection.ReceiveOnly, SdpDirection.Inactive, SdpDirection.Inactive)]
    [TestCase(SdpDirection.Inactive, SdpDirection.SendReceive, SdpDirection.Inactive)]
    [TestCase(SdpDirection.Inactive, SdpDirection.SendOnly, SdpDirection.Inactive)]
    [TestCase(SdpDirection.Inactive, SdpDirection.ReceiveOnly, SdpDirection.Inactive)]
    [TestCase(SdpDirection.Inactive, SdpDirection.Inactive, SdpDirection.Inactive)]
    public void AnswerDirection_IsOfferDirectionIntersectedWithLocalCapability(SdpDirection offered, SdpDirection local, SdpDirection expected)
    {
        SdpSessionDescription offer = Session("192.0.2.1", "0", "0");
        SdpMediaDescription media = Media(4000, "120", "X-CODEC");
        media.Attributes.Add(DirectionAttribute(offered));
        offer.MediaDescriptions.Add(media);
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(Capability(local));

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, Session("192.0.2.2", "0", "0"), new SdpNegotiationOptions { LocalCapabilities = capabilities });

        Assert.Multiple(() =>
        {
            Assert.That(result.Media[0].Accepted, Is.True);
            Assert.That(result.Media[0].OfferedDirection, Is.EqualTo(offered));
            Assert.That(result.Media[0].Direction, Is.EqualTo(expected));
            Assert.That(result.Media[0].EffectiveMedia!.Direction, Is.EqualTo(expected));
        });
    }

    [Test]
    public void OmittedOfferDirection_DefaultsToSendReceive()
    {
        SdpSessionDescription offer = Session("192.0.2.1", "0", "0");
        offer.MediaDescriptions.Add(Media(4000, "120", "X-CODEC"));
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(Capability(SdpDirection.SendOnly));

        SdpNegotiationResult result = CreateAnswer(offer, capabilities, SdpCodecMatchMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(result.Media[0].OfferedDirection, Is.EqualTo(SdpDirection.SendReceive));
            Assert.That(result.Media[0].Direction, Is.EqualTo(SdpDirection.SendOnly));
        });
    }

    [Test]
    public void AnswerCodecPolicies_ControlPreferenceAndSymmetricPayloadAssignments()
    {
        SdpSessionDescription offer = TwoCodecOffer();
        SdpEndpointCapabilities capabilities = TwoCodecCapabilities();

        SdpNegotiationResult remoteOrder = Answer(offer, capabilities, true, false);
        SdpNegotiationResult localOrder = Answer(offer, capabilities, true, true);
        SdpNegotiationResult remapped = Answer(offer, capabilities, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(remoteOrder.Description!.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "120", "121" }));
            Assert.That(remoteOrder.Media[0].Codecs.Select(codec => codec.EncodingName), Is.EqualTo(new[] { "X-LOW", "X-HIGH" }));
            Assert.That(localOrder.Description!.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "121", "120" }));
            Assert.That(localOrder.Media[0].Codecs.Select(codec => codec.EncodingName), Is.EqualTo(new[] { "X-HIGH", "X-LOW" }));
            Assert.That(remapped.Description!.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "96", "97" }));
            Assert.That(remapped.Media[0].PayloadMappings.Select(mapping => (mapping.OfferedPayloadType, mapping.NegotiatedPayloadType)), Is.EqualTo(new[] { (121, 96), (120, 97) }));
            Assert.That(offer.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "120", "121" }));
            Assert.That(capabilities.Media[0].Codecs.Select(codec => codec.PreferredPayloadType), Is.EqualTo(new[] { 96, 97 }));
        });
    }

    [Test]
    public void CodecMatchModes_DistinguishExactAndCompatibleFormatParameters()
    {
        SdpSessionDescription offer = Session("192.0.2.1", "0", "0");
        SdpMediaDescription media = Media(4000, "120", "X-CODEC");
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FormatParameters, "120 mode=1;remote-extension=enabled"));
        offer.MediaDescriptions.Add(media);
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        SdpMediaCapability local = Capability(SdpDirection.SendReceive);
        local.Codecs[0].FormatParameters.Add(new SdpFormatParameter { Name = "mode", HasValue = true, Value = "1" });
        capabilities.Media.Add(local);

        SdpNegotiationResult strict = CreateAnswer(offer, capabilities, SdpCodecMatchMode.Strict);
        SdpNegotiationResult compatible = CreateAnswer(offer, capabilities, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Media[0].Accepted, Is.False);
            Assert.That(strict.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.NoCompatibleCodec));
            Assert.That(strict.Description!.MediaDescriptions[0].Port, Is.Zero);
            Assert.That(strict.Description.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "120" }));
            Assert.That(compatible.Media[0].Accepted, Is.True);
            Assert.That(compatible.Description!.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "120" }));
        });
    }

    [Test]
    public void HoldOffers_UseDirectionsAndAcceptTheLegacyZeroAddress()
    {
        SdpSessionDescription directionalHold = Session("192.0.2.1", "0", "0");
        SdpMediaDescription directionalMedia = Media(4000, "120", "X-CODEC");
        directionalMedia.Attributes.Add(DirectionAttribute(SdpDirection.SendOnly));
        directionalHold.MediaDescriptions.Add(directionalMedia);
        SdpSessionDescription legacyHold = Session("0.0.0.0", "0", "0");
        legacyHold.MediaDescriptions.Add(Media(4000, "120", "X-CODEC"));
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(Capability(SdpDirection.SendReceive));

        SdpNegotiationResult directional = CreateAnswer(directionalHold, capabilities, SdpCodecMatchMode.Strict);
        SdpNegotiationResult legacy = CreateAnswer(legacyHold, capabilities, SdpCodecMatchMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(directional.Media[0].Direction, Is.EqualTo(SdpDirection.ReceiveOnly));
            Assert.That(legacy.Media[0].Accepted, Is.True);
            Assert.That(legacy.Description!.Connection!.Address, Is.EqualTo("192.0.2.2"));
            Assert.That(legacyHold.Connection!.Address, Is.EqualTo("0.0.0.0"));
        });
    }

    [Test]
    public void Answer_UsesLocalConnectionButCopiesTheOfferedSessionTiming()
    {
        SdpSessionDescription offer = Session("192.0.2.1", "100", "200");
        offer.MediaDescriptions.Add(Media(4000, "120", "X-CODEC"));
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(Capability(SdpDirection.SendReceive));

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, Session("192.0.2.2", "0", "0"), new SdpNegotiationOptions { LocalCapabilities = capabilities });
        SdpValidationResult validation = ValidateAnswer(result.Description!, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Description!.Connection!.Address, Is.EqualTo("192.0.2.2"));
            Assert.That(result.Description.Timings.Single().StartTime, Is.EqualTo("100"));
            Assert.That(result.Description.Timings.Single().StopTime, Is.EqualTo("200"));
            Assert.That(result.Description.Timings.Single(), Is.Not.SameAs(offer.Timings.Single()));
            Assert.That(validation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer), Is.False);
            Assert.That(offer.Connection!.Address, Is.EqualTo("192.0.2.1"));
        });
    }

    [Test]
    public void SubsequentOffer_ChangesTheMediaConnectionWithoutMutatingThePreviousOffer()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities initialCapabilities = new SdpEndpointCapabilities();
        initialCapabilities.Media.Add(Capability(SdpDirection.SendReceive));
        SdpSessionDescription previous = service.CreateInitialOffer(Session("192.0.2.1", "0", "0"), new SdpNegotiationOptions { LocalCapabilities = initialCapabilities }).Description!;
        SdpEndpointCapabilities changedCapabilities = new SdpEndpointCapabilities();
        SdpMediaCapability changedMedia = Capability(SdpDirection.SendReceive);
        changedMedia.Connection = Connection("192.0.2.9");
        changedCapabilities.Media.Add(changedMedia);

        SdpNegotiationResult result = service.CreateSubsequentOffer(previous, new SdpNegotiationOptions { LocalCapabilities = changedCapabilities });

        Assert.Multiple(() =>
        {
            Assert.That(result.Media[0].EffectiveMedia!.Connection!.Address, Is.EqualTo("192.0.2.9"));
            Assert.That(result.Description!.MediaDescriptions[0].Connections.Single().Address, Is.EqualTo("192.0.2.9"));
            Assert.That(result.Description.Origin!.SessionVersion, Is.EqualTo("2"));
            Assert.That(previous.MediaDescriptions[0].Connections, Is.Empty);
            Assert.That(previous.Connection!.Address, Is.EqualTo("192.0.2.1"));
            Assert.That(previous.Origin!.SessionVersion, Is.EqualTo("1"));
        });
    }

    [Test]
    public void RejectedAnswer_IgnoresItsRetainedFormatWhileActiveAnswerRequiresACommonCodec()
    {
        SdpSessionDescription offer = Session("192.0.2.1", "0", "0");
        offer.MediaDescriptions.Add(Media(4000, "120", "X-CODEC"));
        SdpSessionDescription rejected = Session("192.0.2.2", "0", "0");
        rejected.MediaDescriptions.Add(Media(0, "127", "UNRELATED"));
        SdpSessionDescription active = Session("192.0.2.2", "0", "0");
        active.MediaDescriptions.Add(Media(5000, "127", "UNRELATED"));

        SdpValidationResult rejectedValidation = ValidateAnswer(rejected, offer);
        SdpValidationResult activeValidation = ValidateAnswer(active, offer);

        Assert.Multiple(() =>
        {
            Assert.That(rejectedValidation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer), Is.False);
            Assert.That(activeValidation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.FieldName == 'm'), Is.True);
        });
    }

    [Test]
    public void ReceiveCapableAnswer_MayAdvertiseAnAdditionalCodecButStillNeedsOneCommonCodec()
    {
        SdpSessionDescription offer = Session("192.0.2.1", "0", "0");
        SdpMediaDescription offeredMedia = Media(4000, "120", "X-CODEC");
        offeredMedia.Attributes.Add(DirectionAttribute(SdpDirection.SendOnly));
        offer.MediaDescriptions.Add(offeredMedia);
        SdpSessionDescription answer = Session("192.0.2.2", "0", "0");
        SdpMediaDescription answerMedia = Media(5000, "96", "X-CODEC");
        answerMedia.Formats.Add("97");
        answerMedia.Attributes.Add(ValueAttribute(SdpAttributeNames.RtpMap, "97 X-RECEIVE-ONLY/8000"));
        answerMedia.Attributes.Add(DirectionAttribute(SdpDirection.ReceiveOnly));
        answer.MediaDescriptions.Add(answerMedia);

        SdpValidationResult result = ValidateAnswer(answer, offer);

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer), Is.False, string.Join("|", result.Issues.Select(issue => issue.Detail)));
    }

    [Test]
    public void IllegalAnswers_ReportOfferSpecificErrors()
    {
        SdpSessionDescription offer = Session("192.0.2.1", "100", "200");
        SdpMediaDescription offeredMedia = Media(4000, "120", "X-CODEC");
        offeredMedia.Attributes.Add(DirectionAttribute(SdpDirection.SendOnly));
        offer.MediaDescriptions.Add(offeredMedia);
        SdpSessionDescription missingMedia = ValidAnswer();
        missingMedia.MediaDescriptions.Clear();
        SdpSessionDescription changedMediaType = ValidAnswer();
        changedMediaType.MediaDescriptions[0].MediaType = "video";
        changedMediaType.MediaDescriptions[0].MediaKind = SdpMediaKind.Video;
        SdpSessionDescription changedProtocol = ValidAnswer();
        changedProtocol.MediaDescriptions[0].Protocol = SdpTransportProtocols.RtpAvpf;
        SdpSessionDescription invalidDirection = ValidAnswer();
        invalidDirection.MediaDescriptions[0].Attributes.RemoveAll(attribute => IsDirection(attribute.Name));
        invalidDirection.MediaDescriptions[0].Attributes.Add(DirectionAttribute(SdpDirection.SendOnly));
        SdpSessionDescription changedTiming = ValidAnswer();
        changedTiming.Timings[0].StartTime = "101";

        List<SdpValidationResult> results = new List<SdpValidationResult>
        {
            ValidateAnswer(missingMedia, offer),
            ValidateAnswer(changedMediaType, offer),
            ValidateAnswer(changedProtocol, offer),
            ValidateAnswer(invalidDirection, offer),
            ValidateAnswer(changedTiming, offer)
        };

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.All.Matches<SdpValidationResult>(result => result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer)));
            Assert.That(results[4].Issues.Any(issue => issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.FieldName == 't'), Is.True);
        });
    }

    static SdpNegotiationResult Answer(SdpSessionDescription offer, SdpEndpointCapabilities capabilities, bool preservePayloadTypes, bool preferLocalOrder)
    {
        return new SdpOfferAnswerService().CreateAnswer(offer, Session("192.0.2.2", "0", "0"), new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            CodecMatchMode = SdpCodecMatchMode.Compatible,
            PreservePayloadTypes = preservePayloadTypes,
            PreferLocalCodecOrder = preferLocalOrder
        });
    }

    static SdpNegotiationResult CreateAnswer(SdpSessionDescription offer, SdpEndpointCapabilities capabilities, SdpCodecMatchMode mode)
    {
        return new SdpOfferAnswerService().CreateAnswer(offer, Session("192.0.2.2", "0", "0"), new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            CodecMatchMode = mode
        });
    }

    static SdpValidationResult ValidateAnswer(SdpSessionDescription answer, SdpSessionDescription offer)
    {
        return new SdpValidator().ValidateDescription(answer, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.CombinedStrict,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer,
            MaximumDiagnosticCount = 128
        });
    }

    static SdpSessionDescription TwoCodecOffer()
    {
        SdpSessionDescription offer = Session("192.0.2.1", "0", "0");
        SdpMediaDescription media = Media(4000, "120", "X-LOW");
        media.Formats.Add("121");
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.RtpMap, "121 X-HIGH/8000"));
        offer.MediaDescriptions.Add(media);
        return offer;
    }

    static SdpEndpointCapabilities TwoCodecCapabilities()
    {
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        SdpMediaCapability media = Capability(SdpDirection.SendReceive);
        media.Codecs.Clear();
        media.Codecs.Add(Codec("X-HIGH", 96));
        media.Codecs.Add(Codec("X-LOW", 97));
        capabilities.Media.Add(media);
        return capabilities;
    }

    static SdpSessionDescription ValidAnswer()
    {
        SdpSessionDescription answer = Session("192.0.2.2", "100", "200");
        SdpMediaDescription media = Media(5000, "96", "X-CODEC");
        media.Attributes.Add(DirectionAttribute(SdpDirection.ReceiveOnly));
        answer.MediaDescriptions.Add(media);
        return answer;
    }

    static SdpSessionDescription Session(string address, string startTime, string stopTime)
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
            SessionName = "RFC 3264",
            Connection = Connection(address)
        };
        description.Timings.Add(new SdpTiming { StartTime = startTime, StopTime = stopTime });
        return description;
    }

    static SdpConnectionData Connection(string address)
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

    static SdpMediaDescription Media(int port, string payloadType, string encodingName)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "audio",
            MediaKind = SdpMediaKind.Audio,
            Port = port,
            Protocol = SdpTransportProtocols.RtpAvp
        };
        media.Formats.Add(payloadType);
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.RtpMap, payloadType + " " + encodingName + "/8000"));
        return media;
    }

    static SdpMediaCapability Capability(SdpDirection direction)
    {
        SdpMediaCapability media = new SdpMediaCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            MediaId = "audio",
            Port = 5000,
            Direction = direction
        };
        media.Protocols.Add(SdpTransportProtocols.RtpAvp);
        media.Codecs.Add(Codec("X-CODEC", 96));
        return media;
    }

    static SdpCodecCapability Codec(string encodingName, int payloadType)
    {
        return new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = encodingName,
            ClockRate = 8000,
            HasPreferredPayloadType = true,
            PreferredPayloadType = payloadType
        };
    }

    static SdpAttribute DirectionAttribute(SdpDirection direction)
    {
        string name = SdpAttributeNames.SendReceive;
        if (direction == SdpDirection.SendOnly)
        {
            name = SdpAttributeNames.SendOnly;
        }
        else if (direction == SdpDirection.ReceiveOnly)
        {
            name = SdpAttributeNames.ReceiveOnly;
        }
        else if (direction == SdpDirection.Inactive)
        {
            name = SdpAttributeNames.Inactive;
        }
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }

    static bool IsDirection(string name)
    {
        return name == SdpAttributeNames.SendReceive || name == SdpAttributeNames.SendOnly || name == SdpAttributeNames.ReceiveOnly || name == SdpAttributeNames.Inactive;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
