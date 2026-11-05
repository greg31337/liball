namespace sdp.Tests.Unit.Validation;

public class SdpExtMapValidationTests
{
    [Test]
    public void UsableIdentifiersAndExclusiveTwoByteUse_AreValidWithoutAllowMixed()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ExtMap("1 urn:example:one"));
        media.Attributes.Add(ExtMap("14 urn:example:fourteen"));
        media.Attributes.Add(ExtMap("15 urn:example:fifteen"));
        media.Attributes.Add(ExtMap("256 urn:example:appbits"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void InvalidAndNonNegotiatedIdentifiers_AreRejected()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ExtMap("0 urn:example:zero"));
        media.Attributes.Add(ExtMap("257 urn:example:257"));
        media.Attributes.Add(ExtMap("4095 urn:example:4095"));
        media.Attributes.Add(ExtMap("4352 urn:example:4352"));
        media.Attributes.Add(ExtMap("4096 urn:example:negotiation"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(5));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationExtMap), Is.True);
            Assert.That(result.Issues.All(issue => issue.AttributeName == SdpAttributeNames.ExtMap), Is.True);
        });
    }

    [Test]
    public void ExtendedIdentifierAlternatives_AreAllowedOnlyInOffers()
    {
        SdpSessionDescription offer = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        offer.MediaDescriptions[0].Attributes.Add(ExtMap("4096 urn:example:string"));
        offer.MediaDescriptions[0].Attributes.Add(ExtMap("4096 urn:example:binary"));
        SdpSessionDescription answer = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        answer.MediaDescriptions[0].Attributes.Add(ExtMap("4096 urn:example:string"));
        answer.MediaDescriptions[0].Attributes.Add(ExtMap("4096 urn:example:binary"));

        SdpValidationResult offerResult = ValidateRtp(offer, SdpNegotiationRole.Offerer, null);
        SdpValidationResult answerResult = ValidateRtp(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(offerResult.Success, Is.True);
            Assert.That(offerResult.Issues, Is.Empty);
            Assert.That(answerResult.ErrorCount, Is.EqualTo(1));
            Assert.That(answerResult.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationExtMap));
            Assert.That(answerResult.Issues[0].Detail, Does.Contain("assigned more than once"));
        });
    }

    [Test]
    public void DuplicateIdentifiersAndEquivalentExtensions_AreRejectedIndependently()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ExtMap("1 urn:example:first alpha"));
        media.Attributes.Add(ExtMap("1 urn:example:second beta"));
        media.Attributes.Add(ExtMap("2 urn:example:shared same"));
        media.Attributes.Add(ExtMap("3/recvonly urn:example:shared same"));
        media.Attributes.Add(ExtMap("4 urn:example:shared different"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationExtMap), Is.True);
            Assert.That(result.Issues[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Issues[0].Detail, Does.Contain("identifier"));
            Assert.That(result.Issues[1].Path.OccurrenceIndex, Is.EqualTo(3));
            Assert.That(result.Issues[1].Detail, Does.Contain("URI"));
        });
    }

    [Test]
    public void SessionAndMediaMappingStyles_MustNotBeMixed()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        description.Attributes.Add(ExtMap("1 urn:example:session"));
        description.MediaDescriptions[0].Attributes.Add(ExtMap("2 urn:example:media"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttributeScope));
            Assert.That(result.Issues[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Issues[0].MediaIndex, Is.Zero);
        });
    }

    [Test]
    public void ExtMapAttributes_RequireAnApplicableRtpStream()
    {
        SdpSessionDescription mediaScoped = DescriptionWithMedia(SdpTransportProtocols.UdpDtlsSctp);
        mediaScoped.MediaDescriptions[0].Attributes.Add(ExtMap("1 urn:example:media"));
        mediaScoped.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        SdpSessionDescription sessionScoped = DescriptionWithMedia(SdpTransportProtocols.UdpDtlsSctp);
        sessionScoped.Attributes.Add(ExtMap("1 urn:example:session"));
        sessionScoped.Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));

        SdpValidationResult mediaResult = ValidateRtp(mediaScoped);
        SdpValidationResult sessionResult = ValidateRtp(sessionScoped);

        Assert.Multiple(() =>
        {
            Assert.That(mediaResult.ErrorCount, Is.EqualTo(2));
            Assert.That(mediaResult.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.True);
            Assert.That(sessionResult.ErrorCount, Is.EqualTo(2));
            Assert.That(sessionResult.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.True);
        });
    }

    [Test]
    public void ExplicitMappingDirections_MustBeCompatibleWithMediaDirection()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(PropertyAttribute(SdpAttributeNames.SendOnly));
        media.Attributes.Add(ExtMap("1 urn:example:inherited"));
        media.Attributes.Add(ExtMap("2/sendonly urn:example:send"));
        media.Attributes.Add(ExtMap("3/inactive urn:example:inactive"));
        media.Attributes.Add(ExtMap("4/recvonly urn:example:receive"));
        media.Attributes.Add(ExtMap("5/sendrecv urn:example:both"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationExtMap), Is.True);
            Assert.That(result.Issues.Select(issue => issue.Path.OccurrenceIndex), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(result.Issues.All(issue => issue.Detail.Contains("direction", StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    public void SessionMappingDirection_MustBeCompatibleWithEveryApplicableStream()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.SendOnly));
        description.Attributes.Add(ExtMap("1/recvonly urn:example:session"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationExtMap));
        });
    }

    [Test]
    public void RejectedAndInactiveStreams_AcceptExplicitExtensionDirections()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        SdpMediaDescription rejected = description.MediaDescriptions[0];
        rejected.Port = 0;
        rejected.Attributes.Add(PropertyAttribute(SdpAttributeNames.ReceiveOnly));
        rejected.Attributes.Add(ExtMap("1/sendonly urn:example:rejected"));
        SdpMediaDescription inactive = Media(SdpTransportProtocols.RtpAvp);
        inactive.Attributes.Add(PropertyAttribute(SdpAttributeNames.Inactive));
        inactive.Attributes.Add(ExtMap("2/recvonly urn:example:inactive"));
        description.MediaDescriptions.Add(inactive);

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void EncryptedMappings_RequireSrtpAndMustNotEncryptTheMarker()
    {
        SdpSessionDescription valid = DescriptionWithMedia(SdpTransportProtocols.RtpSavp);
        valid.MediaDescriptions[0].Attributes.Add(EncryptedExtMap(1, "urn:example:secure"));
        SdpSessionDescription insecure = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        insecure.MediaDescriptions[0].Attributes.Add(EncryptedExtMap(1, "urn:example:insecure"));
        SdpSessionDescription recursive = DescriptionWithMedia(SdpTransportProtocols.RtpSavp);
        recursive.MediaDescriptions[0].Attributes.Add(EncryptedExtMap(1, SdpHeaderExtensionUris.Encryption));

        SdpValidationResult validResult = ValidateRtp(valid);
        SdpValidationResult insecureResult = ValidateRtp(insecure);
        SdpValidationResult recursiveResult = ValidateRtp(recursive);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
            Assert.That(insecureResult.ErrorCount, Is.EqualTo(1));
            Assert.That(insecureResult.Issues[0].Detail, Does.Contain("SRTP"));
            Assert.That(recursiveResult.ErrorCount, Is.EqualTo(1));
            Assert.That(recursiveResult.Issues[0].Detail, Does.Contain("recursively"));
        });
    }

    [Test]
    public void SessionEncryptedMapping_RequiresAtLeastOneSrtpStream()
    {
        SdpSessionDescription valid = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        valid.MediaDescriptions.Add(Media(SdpTransportProtocols.UdpTlsRtpSavp));
        valid.Attributes.Add(EncryptedExtMap(1, "urn:example:secure"));
        SdpSessionDescription invalid = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        invalid.Attributes.Add(EncryptedExtMap(1, "urn:example:insecure"));

        SdpValidationResult validResult = ValidateRtp(valid);
        SdpValidationResult invalidResult = ValidateRtp(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
            Assert.That(invalidResult.ErrorCount, Is.EqualTo(1));
            Assert.That(invalidResult.Issues[0].Detail, Does.Contain("SRTP"));
        });
    }

    [Test]
    public void AllowMixed_IsSingletonWithinEachScope()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.True);
            Assert.That(result.Issues.Select(issue => issue.Scope), Is.EqualTo(new[] { SdpScope.Session, SdpScope.Media }));
        });
    }

    [Test]
    public void AnswerAllowMixed_MustBeEnabledByTheOfferForItsEffectiveScope()
    {
        SdpSessionDescription mediaOffer = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        mediaOffer.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        SdpSessionDescription validMediaAnswer = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        validMediaAnswer.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        SdpSessionDescription invalidSessionAnswer = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        invalidSessionAnswer.MediaDescriptions.Add(Media(SdpTransportProtocols.RtpAvp));
        invalidSessionAnswer.Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        SdpSessionDescription sessionOffer = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        sessionOffer.Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        SdpSessionDescription validInheritedAnswer = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        validInheritedAnswer.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));

        SdpValidationResult validMediaResult = ValidateRtp(validMediaAnswer, SdpNegotiationRole.Answerer, mediaOffer);
        SdpValidationResult invalidSessionResult = ValidateRtp(invalidSessionAnswer, SdpNegotiationRole.Answerer, mediaOffer);
        SdpValidationResult validInheritedResult = ValidateRtp(validInheritedAnswer, SdpNegotiationRole.Answerer, sessionOffer);

        Assert.Multiple(() =>
        {
            Assert.That(validMediaResult.Success, Is.True);
            Assert.That(invalidSessionResult.ErrorCount, Is.EqualTo(1));
            Assert.That(invalidSessionResult.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationExtMap));
            Assert.That(invalidSessionResult.Issues[0].Detail, Does.Contain("offer"));
            Assert.That(validInheritedResult.Success, Is.True);
        });
    }

    [Test]
    public void MalformedMappingAndAllowMixedForms_UseExtMapDiagnostics()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        description.MediaDescriptions[0].Attributes.Add(ExtMap("1 " + SdpHeaderExtensionUris.Encryption));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMapAllowMixed, "unexpected"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationExtMap), Is.True);
            Assert.That(result.Issues.Select(issue => issue.Path.OccurrenceIndex), Is.EqualTo(new[] { 0, 0 }));
        });
    }

    [Test]
    public void BundleMappings_RequireCompatibleIdentifiersAndExtensionConfigurations()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        SdpMediaDescription audio = description.MediaDescriptions[0];
        audio.Attributes.Add(Mid("audio"));
        audio.Attributes.Add(ExtMap("1/sendonly urn:example:shared configuration"));
        audio.Attributes.Add(ExtMap("2 urn:example:audio-only"));
        SdpMediaDescription video = Media(SdpTransportProtocols.RtpAvp);
        video.Attributes.Add(Mid("video"));
        video.Attributes.Add(ExtMap("2/recvonly urn:example:shared configuration"));
        video.Attributes.Add(ExtMap("1 urn:example:video-only"));
        description.MediaDescriptions.Add(video);
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationExtMap), Is.True);
            Assert.That(result.Issues.All(issue => issue.MediaIndex == 1), Is.True);
            Assert.That(result.Issues.Select(issue => issue.Path.OccurrenceIndex), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.Issues.All(issue => issue.Detail.Contains("Bundled", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void BundleMappings_AllowMediaLocalDirectionsAndIndependentNonMembers()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpSavp);
        SdpMediaDescription audio = description.MediaDescriptions[0];
        audio.Attributes.Add(Mid("audio"));
        audio.Attributes.Add(ExtMap("3/sendonly urn:example:shared configuration"));
        SdpMediaDescription video = Media(SdpTransportProtocols.RtpSavp);
        video.Attributes.Add(Mid("video"));
        video.Attributes.Add(ExtMap("3/recvonly urn:example:shared configuration"));
        description.MediaDescriptions.Add(video);
        SdpMediaDescription text = Media(SdpTransportProtocols.RtpSavp);
        text.Attributes.Add(Mid("text"));
        text.Attributes.Add(ExtMap("3 urn:example:unbundled"));
        description.MediaDescriptions.Add(text);
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
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
            Protocol = protocol,
            Location = new SdpSourceLocation { LineNumber = 5, ColumnNumber = 1 }
        };
        media.Formats.Add("0");
        return media;
    }

    static SdpAttribute ExtMap(string value)
    {
        return ValueAttribute(SdpAttributeNames.ExtMap, value);
    }

    static SdpAttribute Mid(string value)
    {
        return ValueAttribute(SdpAttributeNames.MediaId, value);
    }

    static SdpAttribute EncryptedExtMap(int id, string uri)
    {
        return ExtMap(id + " " + SdpHeaderExtensionUris.Encryption + " " + uri);
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value,
            Location = new SdpSourceLocation { LineNumber = 6, ColumnNumber = 1 }
        };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = name,
            RawText = name,
            Location = new SdpSourceLocation { LineNumber = 6, ColumnNumber = 1 }
        };
    }

    static SdpValidationResult ValidateRtp(SdpSessionDescription description)
    {
        return ValidateRtp(description, SdpNegotiationRole.Unknown, null);
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
