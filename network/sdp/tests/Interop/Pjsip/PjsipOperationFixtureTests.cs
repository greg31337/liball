namespace sdp.Tests.Interop.Pjsip;

public class PjsipOperationFixtureTests
{
    [Test]
    public void ParsePrintValidateFixture_PreservesPjmediaSessionAndMediaData()
    {
        string text = PjsipDocument();
        ISdpParser parser = new SdpParser();

        SdpParseResult parsed = parser.ParseText(text, new SdpParseOptions());
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        SdpValidationResult coreValidation = Validate(parsed.Description!, SdpValidationProfile.Rfc8866);
        SdpValidationResult rtpValidation = Validate(parsed.Description!, SdpValidationProfile.Rtp);
        SdpWriteResult printed = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, ParseDiagnosticText(parsed));
            Assert.That(coreValidation.Success, Is.True, ValidationText(coreValidation));
            Assert.That(rtpValidation.Success, Is.True, ValidationText(rtpValidation));
            Assert.That(printed.Success, Is.True, WriteDiagnosticText(printed));
            Assert.That(printed.Text, Is.EqualTo(text));
            Assert.That(parsed.Description!.Origin!.Username, Is.EqualTo("pjmedia"));
            Assert.That(parsed.Description.MediaDescriptions.Select(media => media.MediaType), Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(parsed.Description.MediaDescriptions[0].Connections.Single().Address, Is.EqualTo("192.0.2.31"));
        });
    }

    [Test]
    public void AttributeFindAddAndRemoveFixture_UsesNameAndPayloadIdentity()
    {
        SdpSessionDescription description = Parse(PjsipDocument());
        SdpMediaDescription media = description.MediaDescriptions[0];
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        ISdpEditor editor = new SdpEditor();

        SdpAttribute dynamicMapping = media.Attributes
            .Where(attribute => string.Equals(attribute.Name, SdpAttributeNames.RtpMap, StringComparison.OrdinalIgnoreCase))
            .Single(attribute => codec.DecodeAttribute(attribute).RtpMapAttribute!.PayloadType == 96);
        SdpRtpMapAttribute projected = codec.DecodeAttribute(dynamicMapping).RtpMapAttribute!;
        SdpAttribute added = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = SdpAttributeNames.PacketTime,
            Value = "30",
            RawText = "ptime:30"
        };
        editor.AddMediaAttribute(description, 0, added);
        int addedIndex = media.Attributes.IndexOf(added);
        SdpAttribute removed = editor.RemoveMediaAttributeAt(description, 0, addedIndex);
        int removedMappings = editor.RemoveMediaAttributesByName(description, 0, "RTPMAP");

        Assert.Multiple(() =>
        {
            Assert.That(projected.PayloadType, Is.EqualTo(96));
            Assert.That(projected.EncodingName, Is.EqualTo("telephone-event"));
            Assert.That(removed, Is.SameAs(added));
            Assert.That(removedMappings, Is.EqualTo(3));
            Assert.That(media.Attributes.Any(attribute => string.Equals(attribute.Name, SdpAttributeNames.RtpMap, StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(media.Attributes.Single(attribute => attribute.Name == SdpAttributeNames.FormatParameters).Value, Is.EqualTo("96 0-16"));
            Assert.That(media.Formats, Is.EqualTo(new[] { "0", "8", "96" }));
        });
    }

    [Test]
    public void TransportComparisonFixture_NormalizesTokenCaseButRejectsDifferentProfiles()
    {
        ISdpEditor editor = new SdpEditor();
        SdpSessionDescription original = Parse(PjsipDocument());
        SdpSessionDescription caseVariant = editor.CloneDescription(original);
        SdpSessionDescription secureVariant = editor.CloneDescription(original);
        caseVariant.MediaDescriptions[0].Protocol = "rtp/avp";
        secureVariant.MediaDescriptions[0].Protocol = SdpTransportProtocols.RtpSavp;
        ISdpComparer comparer = new SdpComparer();
        SdpComparisonOptions options = new SdpComparisonOptions
        {
            CompareSemanticValues = true,
            IgnoreRawFormatting = true
        };

        SdpComparisonResult caseComparison = comparer.CompareDescriptions(original, caseVariant, options);
        SdpComparisonResult secureComparison = comparer.CompareDescriptions(original, secureVariant, options);

        Assert.Multiple(() =>
        {
            Assert.That(caseComparison.Equal, Is.True, DifferenceText(caseComparison));
            Assert.That(secureComparison.Equal, Is.False);
            Assert.That(secureComparison.Differences.Any(difference => difference.Path.Scope == SdpScope.Media && difference.Path.FieldName == 'm' && difference.Detail.Contains("transport protocol", StringComparison.OrdinalIgnoreCase)), Is.True, DifferenceText(secureComparison));
        });
    }

    [Test]
    public void MediaDeactivationFixture_ClonesAndSetsZeroPortWithoutLosingFormats()
    {
        SdpSessionDescription original = Parse(PjsipDocument());
        ISdpEditor editor = new SdpEditor();
        SdpSessionDescription deactivated = editor.CloneDescription(original);

        editor.RejectMedia(deactivated, 1);
        SdpDirection rejectedDirection = new SdpQueryService().ResolveEffectiveDirection(deactivated, 1);
        SdpWriteResult printed = new SdpWriter().WriteText(deactivated, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(original.MediaDescriptions[1].Port, Is.EqualTo(4002));
            Assert.That(deactivated.MediaDescriptions[1].Port, Is.Zero);
            Assert.That(deactivated.MediaDescriptions[1].Formats, Is.EqualTo(new[] { "31", "100" }));
            Assert.That(deactivated.MediaDescriptions[1].Attributes.Select(attribute => attribute.RawText), Is.EqualTo(original.MediaDescriptions[1].Attributes.Select(attribute => attribute.RawText)));
            Assert.That(rejectedDirection, Is.EqualTo(SdpDirection.Inactive));
            Assert.That(printed.Success, Is.True, WriteDiagnosticText(printed));
            Assert.That(printed.Text, Does.Contain("m=video 0 RTP/AVP 31 100\r\n"));
        });

        editor.ReactivateMedia(deactivated, 1, 6002);

        Assert.That(deactivated.MediaDescriptions[1].Port, Is.EqualTo(6002));
    }

    [Test]
    public void StaticAndDynamicPayloadFixture_RequiresOnlyDynamicRtpMap()
    {
        string validText = Header("192.0.2.30")
            + "m=audio 4000 RTP/AVP 0 8\r\n"
            + "m=video 4002 RTP/AVP 31 100\r\n"
            + "a=rtpmap:100 H264/90000\r\n";
        string invalidText = Header("192.0.2.30")
            + "m=audio 4000 RTP/AVP 0 96\r\n";
        SdpSessionDescription valid = Parse(validText);
        SdpSessionDescription invalid = Parse(invalidText);

        SdpValidationResult validResult = Validate(valid, SdpValidationProfile.Rtp);
        SdpValidationResult invalidResult = Validate(invalid, SdpValidationProfile.Rtp);
        List<SdpCodec> audioCodecs = new SdpCodecService().ResolveMediaCodecs(valid, 0);
        List<SdpCodec> videoCodecs = new SdpCodecService().ResolveMediaCodecs(valid, 1);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True, ValidationText(validResult));
            Assert.That(audioCodecs.Select(codec => codec.PayloadType), Is.EqualTo(new[] { 0, 8 }));
            Assert.That(audioCodecs.Select(codec => codec.EncodingName), Is.EqualTo(new[] { "PCMU", "PCMA" }));
            Assert.That(videoCodecs.Single(codec => codec.PayloadType == 100).EncodingName, Is.EqualTo("H264"));
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap && issue.AttributeName == SdpAttributeNames.RtpMap), Is.True, ValidationText(invalidResult));
        });
    }

    [Test]
    public void OfferAnswerFixture_PrefersRemoteCodecOrderAndRejectsUnsupportedMedia()
    {
        SdpSessionDescription offer = Parse(PjsipDocument());
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities
        {
            Connection = Connection("192.0.2.40")
        };
        SdpMediaCapability audio = new SdpMediaCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            Port = 5000,
            Direction = SdpDirection.SendReceive
        };
        audio.Protocols.Add(SdpTransportProtocols.RtpAvp);
        audio.Codecs.Add(Codec(SdpMediaKind.Audio, "audio", "PCMU", 8000, 0));
        audio.Codecs.Add(Codec(SdpMediaKind.Audio, "audio", "PCMA", 8000, 8));
        audio.Codecs.Add(Codec(SdpMediaKind.Audio, "audio", "telephone-event", 8000, 96));
        capabilities.Media.Add(audio);
        SdpNegotiationOptions options = new SdpNegotiationOptions
        {
            Role = SdpNegotiationRole.Answerer,
            LocalCapabilities = capabilities,
            CodecMatchMode = SdpCodecMatchMode.Compatible,
            PreferLocalCodecOrder = false,
            PreservePayloadTypes = true,
            AllowMediaRejection = true
        };

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate("192.0.2.40"), options);
        SdpValidationResult validation = new SdpValidator().ValidateDescription(result.Description!, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Rtp,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, NegotiationDiagnosticText(result));
            Assert.That(result.Media, Has.Count.EqualTo(2));
            Assert.That(result.Media[0].Accepted, Is.True);
            Assert.That(result.Media[0].Codecs.Select(codec => codec.PayloadType), Is.EqualTo(new[] { 0, 8, 96 }));
            Assert.That(result.Description!.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "0", "8", "96" }));
            Assert.That(result.Media[1].Accepted, Is.False);
            Assert.That(result.Media[1].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.UnsupportedMedia));
            Assert.That(result.Description.MediaDescriptions[1].Port, Is.Zero);
            Assert.That(validation.Success, Is.True, ValidationText(validation));
            Assert.That(offer.MediaDescriptions[0].Port, Is.EqualTo(4000));
            Assert.That(offer.MediaDescriptions[1].Port, Is.EqualTo(4002));
        });
    }

    static string PjsipDocument()
    {
        return Header("192.0.2.30")
            + "m=audio 4000 RTP/AVP 0 8 96\r\n"
            + "c=IN IP4 192.0.2.31\r\n"
            + "a=rtcp:4001 IN IP4 192.0.2.31\r\n"
            + "a=rtpmap:0 PCMU/8000\r\n"
            + "a=rtpmap:8 PCMA/8000\r\n"
            + "a=rtpmap:96 telephone-event/8000\r\n"
            + "a=fmtp:96 0-16\r\n"
            + "a=sendrecv\r\n"
            + "m=video 4002 RTP/AVP 31 100\r\n"
            + "a=rtpmap:31 H261/90000\r\n"
            + "a=rtpmap:100 H264/90000\r\n"
            + "a=fmtp:100 profile-level-id=42e01f;packetization-mode=1\r\n"
            + "a=sendrecv\r\n";
    }

    static string Header(string address)
    {
        return "v=0\r\n"
            + "o=pjmedia 123456 654321 IN IP4 " + address + "\r\n"
            + "s=pjmedia\r\n"
            + "c=IN IP4 " + address + "\r\n"
            + "t=0 0\r\n";
    }

    static SdpSessionDescription Parse(string text)
    {
        SdpParseResult result = new SdpParser().ParseText(text, new SdpParseOptions());
        Assert.That(result.Success, Is.True, ParseDiagnosticText(result));
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        return result.Description!;
    }

    static SdpValidationResult Validate(SdpSessionDescription description, SdpValidationProfile profile)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = profile });
    }

    static SdpSessionDescription SessionTemplate(string address)
    {
        return Parse(Header(address));
    }

    static SdpConnectionData Connection(string address)
    {
        return new SdpConnectionData
        {
            NetworkType = SdpNetworkTypes.Internet,
            AddressType = SdpAddressTypes.Ip4,
            Address = address,
            AddressKind = SdpAddressKind.Ipv4,
            RawAddress = address,
            AddressCount = 1
        };
    }

    static SdpCodecCapability Codec(SdpMediaKind kind, string mediaType, string encodingName, int clockRate, int payloadType)
    {
        return new SdpCodecCapability
        {
            MediaKind = kind,
            MediaType = mediaType,
            EncodingName = encodingName,
            ClockRate = clockRate,
            HasPreferredPayloadType = true,
            PreferredPayloadType = payloadType
        };
    }

    static string ParseDiagnosticText(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string WriteDiagnosticText(SdpWriteResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string ValidationText(SdpValidationResult result)
    {
        return string.Join("|", result.Issues.Select(issue => issue.Code + ":" + issue.Detail));
    }

    static string DifferenceText(SdpComparisonResult result)
    {
        return string.Join("|", result.Differences.Select(difference => difference.Path.FieldName + ":" + difference.Detail));
    }

    static string NegotiationDiagnosticText(SdpNegotiationResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }
}
