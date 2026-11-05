namespace sdp.Tests.Conformance.Rtp;

public class SdpExtMapRfc8285ConformanceTests
{
    [TestCase("0 urn:example:zero", 0, false, SdpDirection.SendReceive, false, "")]
    [TestCase("1/sendrecv urn:example:one", 1, true, SdpDirection.SendReceive, false, "")]
    [TestCase("14/sendonly urn:example:fourteen", 14, true, SdpDirection.SendOnly, false, "")]
    [TestCase("15/recvonly urn:example:fifteen", 15, true, SdpDirection.ReceiveOnly, false, "")]
    [TestCase("256/inactive urn:example:appbits opaque", 256, true, SdpDirection.Inactive, false, "opaque")]
    [TestCase("4096 urn:example:negotiation-low", 4096, false, SdpDirection.SendReceive, false, "")]
    [TestCase("4351/sendonly urn:ietf:params:rtp-hdrext:encrypt urn:example:secure configuration", 4351, true, SdpDirection.SendOnly, true, "configuration")]
    [TestCase("99999 urn:example:grammar-high", 99999, false, SdpDirection.SendReceive, false, "")]
    public void EveryGrammarFormAndIdentifierBoundary_RoundTrips(string value, int id, bool hasDirection, SdpDirection direction, bool encrypted, string extensionAttributes)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(ValueAttribute(SdpAttributeNames.ExtMap, value));
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.ExtMapAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.ExtMapAttribute!.Id, Is.EqualTo(id));
            Assert.That(decoded.ExtMapAttribute.HasDirection, Is.EqualTo(hasDirection));
            Assert.That(decoded.ExtMapAttribute.Direction, Is.EqualTo(direction));
            Assert.That(decoded.ExtMapAttribute.Encrypted, Is.EqualTo(encrypted));
            Assert.That(decoded.ExtMapAttribute.ExtensionAttributes, Is.EqualTo(extensionAttributes));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void SemanticIdentifierBoundaries_AcceptUsableAndNegotiationRangesOnly()
    {
        SdpSessionDescription offer = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        offer.MediaDescriptions[0].Attributes.Add(ExtMap("1 urn:example:usable-low"));
        offer.MediaDescriptions[0].Attributes.Add(ExtMap("256 urn:example:usable-high"));
        offer.MediaDescriptions[0].Attributes.Add(ExtMap("4096 urn:example:negotiation-low"));
        offer.MediaDescriptions[0].Attributes.Add(ExtMap("4351 urn:example:negotiation-high"));
        SdpSessionDescription invalid = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        invalid.MediaDescriptions[0].Attributes.Add(ExtMap("0 urn:example:below-usable"));
        invalid.MediaDescriptions[0].Attributes.Add(ExtMap("257 urn:example:above-usable"));
        invalid.MediaDescriptions[0].Attributes.Add(ExtMap("4095 urn:example:below-negotiation"));
        invalid.MediaDescriptions[0].Attributes.Add(ExtMap("4352 urn:example:above-negotiation"));
        SdpSessionDescription unnegotiated = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        unnegotiated.MediaDescriptions[0].Attributes.Add(ExtMap("4096 urn:example:negotiation-low"));
        unnegotiated.MediaDescriptions[0].Attributes.Add(ExtMap("4351 urn:example:negotiation-high"));

        SdpValidationResult offerResult = ValidateRtp(offer, SdpNegotiationRole.Offerer, null);
        SdpValidationResult invalidResult = ValidateRtp(invalid, SdpNegotiationRole.Offerer, null);
        SdpValidationResult unnegotiatedResult = ValidateRtp(unnegotiated, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(offerResult.Success, Is.True);
            Assert.That(offerResult.Issues, Is.Empty);
            Assert.That(invalidResult.ErrorCount, Is.EqualTo(4));
            Assert.That(invalidResult.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationExtMap), Is.True);
            Assert.That(unnegotiatedResult.ErrorCount, Is.EqualTo(2));
            Assert.That(unnegotiatedResult.Issues.All(issue => issue.Detail.Contains("negotiation", StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    public void KnownAndUnknownUris_AreResolvedAndQueriedWithoutNormalization()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        description.Attributes.Add(ExtMap("1 " + SdpHeaderExtensionUris.MediaId));
        description.Attributes.Add(ExtMap("2 urn:example:future-extension vendor configuration"));
        ISdpExtMapService service = new SdpExtMapService();
        ISdpRegistry registry = new SdpRegistry();

        List<SdpExtMapAttribute> mappings = service.ResolveMediaExtMaps(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(mappings.Select(mapping => mapping.Uri), Is.EqualTo(new[] { SdpHeaderExtensionUris.MediaId, "urn:example:future-extension" }));
            Assert.That(mappings[1].ExtensionAttributes, Is.EqualTo("vendor configuration"));
            Assert.That(service.FindUriById(description, 0, 2), Is.EqualTo("urn:example:future-extension"));
            Assert.That(service.FindIdByUri(description, 0, "urn:example:future-extension"), Is.EqualTo(2));
            Assert.That(registry.IsRegisteredHeaderExtensionUri(SdpHeaderExtensionUris.MediaId), Is.True);
            Assert.That(registry.IsRegisteredHeaderExtensionUri("urn:example:future-extension"), Is.False);
        });
    }

    [Test]
    public void BundleIdentity_IncludesConfigurationAndEncryptionButNotDirection()
    {
        SdpSessionDescription valid = BundledDescription();
        valid.MediaDescriptions[0].Attributes.Add(ExtMap("1/sendonly urn:example:shared alpha"));
        valid.MediaDescriptions[0].Attributes.Add(EncryptedExtMap("2/sendonly", "urn:example:secure", "configuration"));
        valid.MediaDescriptions[1].Attributes.Add(ExtMap("3/recvonly urn:example:shared beta"));
        valid.MediaDescriptions[1].Attributes.Add(EncryptedExtMap("2/recvonly", "urn:example:secure", "configuration"));
        SdpSessionDescription invalid = BundledDescription();
        invalid.MediaDescriptions[0].Attributes.Add(ExtMap("1/sendonly urn:example:shared alpha"));
        invalid.MediaDescriptions[0].Attributes.Add(ExtMap("2 urn:example:secure configuration"));
        invalid.MediaDescriptions[1].Attributes.Add(ExtMap("1/recvonly urn:example:shared beta"));
        invalid.MediaDescriptions[1].Attributes.Add(EncryptedExtMap("2", "urn:example:secure", "configuration"));

        SdpValidationResult validResult = ValidateRtp(valid, SdpNegotiationRole.Unknown, null);
        SdpValidationResult invalidResult = ValidateRtp(invalid, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
            Assert.That(invalidResult.ErrorCount, Is.EqualTo(2));
            Assert.That(invalidResult.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationExtMap), Is.True);
            Assert.That(invalidResult.Issues.All(issue => issue.MediaIndex == 1), Is.True);
        });
    }

    [Test]
    public void BundleNegotiationIdentifiers_AllowOfferAlternativesButNotAnswerCollisions()
    {
        SdpSessionDescription offer = BundledDescription();
        offer.MediaDescriptions[0].Attributes.Add(ExtMap("4096 urn:example:audio-alternative"));
        offer.MediaDescriptions[1].Attributes.Add(ExtMap("4096 urn:example:video-alternative"));
        SdpSessionDescription answer = BundledDescription();
        answer.MediaDescriptions[0].Attributes.Add(ExtMap("4096 urn:example:audio-alternative"));
        answer.MediaDescriptions[1].Attributes.Add(ExtMap("4096 urn:example:video-alternative"));

        SdpValidationResult offerResult = ValidateRtp(offer, SdpNegotiationRole.Offerer, null);
        SdpValidationResult answerResult = ValidateRtp(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(offerResult.Success, Is.True);
            Assert.That(offerResult.Issues, Is.Empty);
            Assert.That(answerResult.ErrorCount, Is.EqualTo(1));
            Assert.That(answerResult.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationExtMap));
            Assert.That(answerResult.Issues[0].MediaIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void AllocationAndRemapping_HandleBothNegotiationBoundariesIdempotently()
    {
        SdpAttribute lowSource = ExtMap("4096 urn:example:low");
        SdpAttribute highSource = ExtMap("4351 urn:example:high");
        SdpExtMapAttribute preserved = TypedMapping(256, "urn:example:preserved", null);
        SdpExtMapAttribute low = TypedMapping(4096, "urn:example:low", lowSource);
        SdpExtMapAttribute high = TypedMapping(4351, "urn:example:high", highSource);
        List<SdpExtMapAttribute> selected = new List<SdpExtMapAttribute> { preserved, low, high };
        ISdpExtMapService service = new SdpExtMapService();

        List<SdpHeaderExtensionMapping> mappings = service.AllocateExtMapIds(selected);
        SdpEffectiveMedia media = new SdpEffectiveMedia();
        media.HeaderExtensions.Add(low);
        media.HeaderExtensions.Add(high);
        service.RemapExtMapIds(media, mappings);
        service.RemapExtMapIds(media, mappings);

        Assert.Multiple(() =>
        {
            Assert.That(mappings.Select(mapping => mapping.OfferedId), Is.EqualTo(new[] { 256, 4096, 4351 }));
            Assert.That(mappings.Select(mapping => mapping.NegotiatedId), Is.EqualTo(new[] { 256, 1, 2 }));
            Assert.That(media.HeaderExtensions.Select(mapping => mapping.Id), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(lowSource.Value, Is.EqualTo("1 urn:example:low"));
            Assert.That(highSource.Value, Is.EqualTo("2 urn:example:high"));
        });
    }

    [Test]
    public void BundleRemapping_UpdatesOneInheritedMappingForEveryMember()
    {
        SdpSessionDescription description = BundledDescription();
        SdpAttribute source = ExtMap("4096 " + SdpHeaderExtensionUris.MediaId);
        description.Attributes.Add(source);
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        List<SdpHeaderExtensionMapping> mappings = new SdpExtMapService().AllocateExtMapIds(new List<SdpExtMapAttribute>
        {
            TypedMapping(4096, SdpHeaderExtensionUris.MediaId, source)
        });
        SdpGroupAttribute group = DecodeBundleGroup(description.Attributes[0]);
        group.IdentificationTags.Clear();
        group.IdentificationTags.Add("audio");
        group.IdentificationTags.Add("video");
        ISdpExtMapService service = new SdpExtMapService();

        service.RemapBundleExtMapIds(description, group, mappings);

        Assert.Multiple(() =>
        {
            Assert.That(source.Value, Is.EqualTo("1 " + SdpHeaderExtensionUris.MediaId));
            Assert.That(service.ResolveMediaExtMaps(description, 0).Single().Id, Is.EqualTo(1));
            Assert.That(service.ResolveMediaExtMaps(description, 1).Single().Id, Is.EqualTo(1));
            Assert.That(service.ResolveMediaExtMapAllowMixed(description, 0), Is.True);
            Assert.That(service.ResolveMediaExtMapAllowMixed(description, 1), Is.True);
        });
    }

    static SdpSessionDescription BundledDescription()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpSavp);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "audio"));
        SdpMediaDescription video = Media(SdpTransportProtocols.RtpSavp);
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "video"));
        description.MediaDescriptions.Add(video);
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        return description;
    }

    static SdpGroupAttribute DecodeBundleGroup(SdpAttribute attribute)
    {
        SdpAttributeDecodeResult decoded = new SdpAttributeCodec().DecodeAttribute(attribute);
        return decoded.GroupAttribute ?? new SdpGroupAttribute();
    }

    static SdpSessionDescription DescriptionWithMedia(string protocol)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(Media(protocol));
        return description;
    }

    static SdpMediaDescription Media(string protocol)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = 5000,
            Protocol = protocol
        };
        media.Formats.Add("0");
        return media;
    }

    static SdpExtMapAttribute TypedMapping(int id, string uri, SdpAttribute? source)
    {
        return new SdpExtMapAttribute
        {
            SourceAttribute = source,
            Id = id,
            Direction = SdpDirection.SendReceive,
            Uri = uri,
            ExtensionAttributes = string.Empty
        };
    }

    static SdpAttribute ExtMap(string value)
    {
        return ValueAttribute(SdpAttributeNames.ExtMap, value);
    }

    static SdpAttribute EncryptedExtMap(string mapEntry, string uri, string attributes)
    {
        return ExtMap(mapEntry + " " + SdpHeaderExtensionUris.Encryption + " " + uri + " " + attributes);
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value
        };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = name,
            RawText = name
        };
    }

    static SdpValidationResult ValidateRtp(SdpSessionDescription description, SdpNegotiationRole role, SdpSessionDescription? offer)
    {
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Rtp,
            NegotiationRole = role,
            OfferDescription = offer
        };
        return new SdpValidator().ValidateDescription(description, options);
    }
}
