namespace sdp.Tests.Unit.Operations;

public class SdpExtMapServiceTests
{
    [Test]
    public void ResolveMediaExtMaps_AppliesSessionMappingsDirectionAndEncryptionScope()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.ReceiveOnly));
        SdpAttribute inheritedSource = ExtMap("1 " + SdpHeaderExtensionUris.MediaId);
        SdpAttribute explicitSource = ExtMap("2/sendonly urn:example:explicit opaque data");
        SdpAttribute encryptedSource = ExtMap("3 " + SdpHeaderExtensionUris.Encryption + " urn:example:encrypted");
        description.Attributes.Add(inheritedSource);
        description.Attributes.Add(explicitSource);
        description.Attributes.Add(encryptedSource);
        description.MediaDescriptions.Add(Media(SdpTransportProtocols.RtpAvp));
        description.MediaDescriptions.Add(Media(SdpTransportProtocols.UdpTlsRtpSavp));
        ISdpExtMapService service = new SdpExtMapService();

        List<SdpExtMapAttribute> plain = service.ResolveMediaExtMaps(description, 0);
        List<SdpExtMapAttribute> secure = service.ResolveMediaExtMaps(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(plain.Select(mapping => mapping.Id), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(plain[0].HasDirection, Is.False);
            Assert.That(plain[0].Direction, Is.EqualTo(SdpDirection.ReceiveOnly));
            Assert.That(plain[0].SourceAttribute, Is.SameAs(inheritedSource));
            Assert.That(plain[1].HasDirection, Is.True);
            Assert.That(plain[1].Direction, Is.EqualTo(SdpDirection.SendOnly));
            Assert.That(plain[1].ExtensionAttributes, Is.EqualTo("opaque data"));
            Assert.That(plain[1].SourceAttribute, Is.SameAs(explicitSource));
            Assert.That(secure.Select(mapping => mapping.Id), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(secure[2].Encrypted, Is.True);
            Assert.That(secure[2].Uri, Is.EqualTo("urn:example:encrypted"));
            Assert.That(secure[2].SourceAttribute, Is.SameAs(encryptedSource));
        });
    }

    [Test]
    public void MediaMappings_OverrideSessionScopeAndSkipMalformedEntries()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.ReceiveOnly));
        description.Attributes.Add(ExtMap("1 urn:example:session"));
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(PropertyAttribute(SdpAttributeNames.SendOnly));
        media.Attributes.Add(ExtMap("malformed"));
        SdpAttribute mediaSource = ExtMap("2 urn:example:media extension configuration");
        media.Attributes.Add(mediaSource);
        ISdpExtMapService service = new SdpExtMapService();

        List<SdpExtMapAttribute> first = service.ResolveMediaExtMaps(description, 0);
        first[0].Uri = "changed";
        List<SdpExtMapAttribute> second = service.ResolveMediaExtMaps(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Count.EqualTo(1));
            Assert.That(second, Has.Count.EqualTo(1));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
            Assert.That(second[0].Id, Is.EqualTo(2));
            Assert.That(second[0].Uri, Is.EqualTo("urn:example:media"));
            Assert.That(second[0].Direction, Is.EqualTo(SdpDirection.SendOnly));
            Assert.That(second[0].HasDirection, Is.False);
            Assert.That(second[0].ExtensionAttributes, Is.EqualTo("extension configuration"));
            Assert.That(second[0].SourceAttribute, Is.SameAs(mediaSource));
            Assert.That(mediaSource.Value, Is.EqualTo("2 urn:example:media extension configuration"));
        });
    }

    [Test]
    public void InactiveAndRejectedMedia_ResolveImplicitMappingDirectionToSendReceive()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.SendOnly));
        description.Attributes.Add(ExtMap("1 urn:example:implicit"));
        SdpMediaDescription inactive = description.MediaDescriptions[0];
        inactive.Attributes.Add(PropertyAttribute(SdpAttributeNames.Inactive));
        SdpMediaDescription rejected = Media(SdpTransportProtocols.RtpAvp);
        rejected.Port = 0;
        rejected.Attributes.Add(PropertyAttribute(SdpAttributeNames.ReceiveOnly));
        description.MediaDescriptions.Add(rejected);
        ISdpExtMapService service = new SdpExtMapService();

        List<SdpExtMapAttribute> inactiveMappings = service.ResolveMediaExtMaps(description, 0);
        List<SdpExtMapAttribute> rejectedMappings = service.ResolveMediaExtMaps(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(inactiveMappings[0].Direction, Is.EqualTo(SdpDirection.SendReceive));
            Assert.That(rejectedMappings[0].Direction, Is.EqualTo(SdpDirection.SendReceive));
        });
    }

    [Test]
    public void ResolveMediaExtMapAllowMixed_UsesValidSessionOrMediaMarkers()
    {
        SdpSessionDescription sessionEnabled = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        sessionEnabled.Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        sessionEnabled.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMapAllowMixed, "invalid"));
        SdpSessionDescription mediaEnabled = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        mediaEnabled.MediaDescriptions[0].Attributes.Add(PropertyAttribute("EXTMAP-ALLOW-MIXED"));
        SdpSessionDescription disabled = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        disabled.Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMapAllowMixed, "invalid"));
        ISdpExtMapService service = new SdpExtMapService();

        Assert.Multiple(() =>
        {
            Assert.That(service.ResolveMediaExtMapAllowMixed(sessionEnabled, 0), Is.True);
            Assert.That(service.ResolveMediaExtMapAllowMixed(mediaEnabled, 0), Is.True);
            Assert.That(service.ResolveMediaExtMapAllowMixed(disabled, 0), Is.False);
        });
    }

    [Test]
    public void IdAndUriQueries_SearchEffectiveMappingsInSourceOrder()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ExtMap("4096 urn:example:first first-configuration"));
        media.Attributes.Add(ExtMap("7 urn:example:shared configuration-one"));
        media.Attributes.Add(ExtMap("8 urn:example:shared configuration-two"));
        ISdpExtMapService service = new SdpExtMapService();

        Assert.Multiple(() =>
        {
            Assert.That(service.FindUriById(description, 0, 4096), Is.EqualTo("urn:example:first"));
            Assert.That(service.FindIdByUri(description, 0, "urn:example:shared"), Is.EqualTo(7));
            Assert.That(service.FindUriById(description, 0, 999), Is.Null);
            Assert.That(service.FindUriById(description, 0, -1), Is.Null);
            Assert.That(service.FindIdByUri(description, 0, "URN:example:shared"), Is.EqualTo(-1));
            Assert.That(service.FindIdByUri(description, 0, "urn:example:missing"), Is.EqualTo(-1));
        });
    }

    [Test]
    public void NonRtpMedia_HasNoEffectiveMappingsOrMixedMode()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.UdpDtlsSctp);
        description.Attributes.Add(ExtMap("1 urn:example:session"));
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        description.MediaDescriptions[0].Attributes.Add(ExtMap("2 urn:example:media"));
        ISdpExtMapService service = new SdpExtMapService();

        Assert.Multiple(() =>
        {
            Assert.That(service.ResolveMediaExtMaps(description, 0), Is.Empty);
            Assert.That(service.ResolveMediaExtMapAllowMixed(description, 0), Is.False);
            Assert.That(service.FindUriById(description, 0, 1), Is.Null);
            Assert.That(service.FindIdByUri(description, 0, "urn:example:session"), Is.EqualTo(-1));
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
    public void SelectAnswerDirection_IntersectsOffererAndAnswererCapabilities(SdpDirection offered, SdpDirection local, SdpDirection expected)
    {
        SdpDirection result = new SdpExtMapService().SelectAnswerDirection(offered, local);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void IntersectExtMaps_PreservesOfferOrderAndSelectsPreferredAlternatives()
    {
        SdpExtMapAttribute first = TypedMapping(1, SdpDirection.SendOnly, false, "urn:example:first", "configuration");
        first.HasDirection = true;
        SdpExtMapAttribute encrypted = TypedMapping(2, SdpDirection.SendReceive, true, "urn:example:encrypted", "");
        SdpExtMapAttribute alternativeOne = TypedMapping(4096, SdpDirection.SendReceive, false, "urn:example:alternative-one", "");
        SdpExtMapAttribute alternativeTwo = TypedMapping(4096, SdpDirection.SendReceive, false, "urn:example:alternative-two", "");
        SdpExtMapAttribute unsupported = TypedMapping(4, SdpDirection.SendReceive, false, "urn:example:unsupported", "");
        List<SdpExtMapAttribute> offered = new List<SdpExtMapAttribute> { first, encrypted, alternativeOne, alternativeTwo, unsupported };
        List<SdpHeaderExtension> capabilities = new List<SdpHeaderExtension>
        {
            new SdpHeaderExtension { Uri = first.Uri, Direction = SdpDirection.SendReceive, Preference = 10 },
            new SdpHeaderExtension { Uri = first.Uri, Direction = SdpDirection.ReceiveOnly, Preference = 1 },
            new SdpHeaderExtension { Uri = encrypted.Uri, Direction = SdpDirection.SendOnly, SupportsEncryption = false, Preference = 0 },
            new SdpHeaderExtension { Uri = encrypted.Uri, Direction = SdpDirection.SendOnly, SupportsEncryption = true, Preference = 2 },
            new SdpHeaderExtension { Uri = alternativeOne.Uri, Direction = SdpDirection.SendReceive, Preference = 5 },
            new SdpHeaderExtension { Uri = alternativeTwo.Uri, Direction = SdpDirection.ReceiveOnly, Preference = 1 }
        };
        ISdpExtMapService service = new SdpExtMapService();

        List<SdpExtMapAttribute> result = service.IntersectExtMaps(offered, capabilities);

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(mapping => mapping.Id), Is.EqualTo(new[] { 1, 2, 4096 }));
            Assert.That(result.Select(mapping => mapping.Uri), Is.EqualTo(new[] { first.Uri, encrypted.Uri, alternativeTwo.Uri }));
            Assert.That(result.Select(mapping => mapping.Direction), Is.EqualTo(new[] { SdpDirection.ReceiveOnly, SdpDirection.SendOnly, SdpDirection.ReceiveOnly }));
            Assert.That(result[0].HasDirection, Is.True);
            Assert.That(result[0].ExtensionAttributes, Is.EqualTo("configuration"));
            Assert.That(result[1].Encrypted, Is.True);
            Assert.That(result[0], Is.Not.SameAs(first));
            Assert.That(result[2], Is.Not.SameAs(alternativeTwo));
        });
    }

    [Test]
    public void AllocateExtMapIds_PreservesUsableIdsAndFillsLowestGaps()
    {
        List<SdpExtMapAttribute> extMaps = new List<SdpExtMapAttribute>
        {
            TypedMapping(1, SdpDirection.SendReceive, false, "urn:example:one", ""),
            TypedMapping(3, SdpDirection.ReceiveOnly, true, "urn:example:three", "opaque"),
            TypedMapping(4096, SdpDirection.SendOnly, false, "urn:example:first-gap", ""),
            TypedMapping(4097, SdpDirection.Inactive, false, "urn:example:second-gap", "")
        };

        List<SdpHeaderExtensionMapping> result = new SdpExtMapService().AllocateExtMapIds(extMaps);

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(mapping => mapping.OfferedId), Is.EqualTo(new[] { 1, 3, 4096, 4097 }));
            Assert.That(result.Select(mapping => mapping.NegotiatedId), Is.EqualTo(new[] { 1, 3, 2, 4 }));
            Assert.That(result[1].Direction, Is.EqualTo(SdpDirection.ReceiveOnly));
            Assert.That(result[1].Encrypted, Is.True);
            Assert.That(result[1].Uri, Is.EqualTo("urn:example:three"));
            Assert.That(result[1].ExtensionAttributes, Is.EqualTo("opaque"));
        });
    }

    [Test]
    public void AllocateExtMapIds_ReportsCollisionsInvalidIdsAndExhaustion()
    {
        ISdpExtMapService service = new SdpExtMapService();
        List<SdpExtMapAttribute> duplicates = new List<SdpExtMapAttribute>
        {
            TypedMapping(1, SdpDirection.SendReceive, false, "urn:example:first", ""),
            TypedMapping(1, SdpDirection.SendReceive, false, "urn:example:second", "")
        };
        List<SdpExtMapAttribute> invalid = new List<SdpExtMapAttribute>
        {
            TypedMapping(257, SdpDirection.SendReceive, false, "urn:example:invalid", "")
        };
        List<SdpExtMapAttribute> duplicateConfiguration = new List<SdpExtMapAttribute>
        {
            TypedMapping(1, SdpDirection.SendOnly, false, "urn:example:duplicate", "same"),
            TypedMapping(2, SdpDirection.ReceiveOnly, false, "urn:example:duplicate", "same")
        };
        List<SdpExtMapAttribute> exhausted = new List<SdpExtMapAttribute>();
        int identifier = 1;
        while (identifier <= 256)
        {
            exhausted.Add(TypedMapping(identifier, SdpDirection.SendReceive, false, "urn:example:" + identifier, ""));
            identifier = identifier + 1;
        }
        exhausted.Add(TypedMapping(4096, SdpDirection.SendReceive, false, "urn:example:overflow", ""));

        SdpNegotiationException? duplicateException = Assert.Throws<SdpNegotiationException>(() => service.AllocateExtMapIds(duplicates));
        SdpNegotiationException? configurationException = Assert.Throws<SdpNegotiationException>(() => service.AllocateExtMapIds(duplicateConfiguration));
        SdpNegotiationException? exhaustionException = Assert.Throws<SdpNegotiationException>(() => service.AllocateExtMapIds(exhausted));

        Assert.Multiple(() =>
        {
            Assert.That(duplicateException!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed));
            Assert.That(configurationException!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed));
            Assert.That(exhaustionException!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed));
            Assert.That(() => service.AllocateExtMapIds(invalid), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mappings"));
        });
    }

    [Test]
    public void RemapExtMapIds_UpdatesTheFullGraphAndBackingAttributes()
    {
        SdpAttribute firstSource = ExtMap("4096/sendonly urn:example:first opaque");
        firstSource.Location.LineNumber = 17;
        SdpAttribute secondSource = ExtMap("5 urn:example:second");
        SdpExtMapAttribute first = TypedMapping(4096, SdpDirection.SendOnly, false, "urn:example:first", "opaque");
        first.HasDirection = true;
        first.SourceAttribute = firstSource;
        SdpExtMapAttribute second = TypedMapping(5, SdpDirection.SendReceive, true, "urn:example:second", "");
        second.SourceAttribute = secondSource;
        SdpEffectiveMedia media = new SdpEffectiveMedia();
        media.HeaderExtensions.Add(first);
        media.HeaderExtensions.Add(second);
        media.BundleTransport = new SdpBundleTransport();
        media.BundleTransport.HeaderExtensions.Add(first);
        SdpExtMapAttribute bundleSecond = TypedMapping(5, SdpDirection.SendReceive, true, "urn:example:second", "");
        bundleSecond.SourceAttribute = secondSource;
        media.BundleTransport.HeaderExtensions.Add(bundleSecond);
        List<SdpHeaderExtensionMapping> mappings = new List<SdpHeaderExtensionMapping>
        {
            IdentifierMapping(4096, 1, SdpDirection.ReceiveOnly, false, "urn:example:first", "opaque"),
            IdentifierMapping(5, 5, SdpDirection.SendReceive, true, "urn:example:second", "")
        };

        new SdpExtMapService().RemapExtMapIds(media, mappings);

        Assert.Multiple(() =>
        {
            Assert.That(media.HeaderExtensions.Select(extension => extension.Id), Is.EqualTo(new[] { 1, 5 }));
            Assert.That(media.HeaderExtensions[0].Direction, Is.EqualTo(SdpDirection.ReceiveOnly));
            Assert.That(media.HeaderExtensions[0].HasDirection, Is.True);
            Assert.That(media.BundleTransport.HeaderExtensions.Select(extension => extension.Id), Is.EqualTo(new[] { 1, 5 }));
            Assert.That(firstSource.Value, Is.EqualTo("1/recvonly urn:example:first opaque"));
            Assert.That(firstSource.RawText, Is.EqualTo("extmap:1/recvonly urn:example:first opaque"));
            Assert.That(firstSource.Location.LineNumber, Is.EqualTo(17));
            Assert.That(secondSource.Value, Is.EqualTo("5 " + SdpHeaderExtensionUris.Encryption + " urn:example:second"));
        });
    }

    [Test]
    public void RemapExtMapIds_ValidatesTheEntireGraphBeforeMutation()
    {
        SdpAttribute source = ExtMap("1 urn:example:first");
        SdpExtMapAttribute first = TypedMapping(1, SdpDirection.SendReceive, false, "urn:example:first", "");
        first.SourceAttribute = source;
        SdpExtMapAttribute missing = TypedMapping(2, SdpDirection.SendReceive, false, "urn:example:missing", "");
        SdpEffectiveMedia media = new SdpEffectiveMedia();
        media.HeaderExtensions.Add(first);
        media.HeaderExtensions.Add(missing);
        List<SdpHeaderExtensionMapping> incomplete = new List<SdpHeaderExtensionMapping>
        {
            IdentifierMapping(1, 7, SdpDirection.ReceiveOnly, false, "urn:example:first", "")
        };

        Assert.That(() => new SdpExtMapService().RemapExtMapIds(media, incomplete), Throws.ArgumentException);

        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo(1));
            Assert.That(first.Direction, Is.EqualTo(SdpDirection.SendReceive));
            Assert.That(missing.Id, Is.EqualTo(2));
            Assert.That(source.Value, Is.EqualTo("1 urn:example:first"));
        });
    }

    [Test]
    public void RemapBundleExtMapIds_AppliesOneIdentityPlanToEveryMember()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        SdpMediaDescription audio = description.MediaDescriptions[0];
        audio.Attributes.Add(Mid("audio"));
        SdpAttribute audioShared = ExtMap("1/sendonly urn:example:shared configuration");
        SdpAttribute audioSecond = ExtMap("2 urn:example:second");
        audio.Attributes.Add(audioShared);
        audio.Attributes.Add(audioSecond);
        SdpMediaDescription video = Media(SdpTransportProtocols.RtpAvp);
        video.Attributes.Add(Mid("video"));
        SdpAttribute videoShared = ExtMap("2/recvonly urn:example:shared configuration");
        SdpAttribute videoSecond = ExtMap("1/inactive urn:example:second");
        video.Attributes.Add(videoShared);
        video.Attributes.Add(videoSecond);
        description.MediaDescriptions.Add(video);
        SdpMediaDescription unrelated = Media(SdpTransportProtocols.RtpAvp);
        unrelated.Attributes.Add(Mid("data"));
        SdpAttribute unrelatedMapping = ExtMap("1 urn:example:shared configuration");
        unrelated.Attributes.Add(unrelatedMapping);
        description.MediaDescriptions.Add(unrelated);
        SdpGroupAttribute group = BundleGroup("audio", "video");
        List<SdpHeaderExtensionMapping> mappings = new List<SdpHeaderExtensionMapping>
        {
            IdentifierMapping(1, 3, SdpDirection.SendReceive, false, "urn:example:shared", "configuration"),
            IdentifierMapping(2, 4, SdpDirection.SendReceive, false, "urn:example:second", "")
        };
        ISdpExtMapService service = new SdpExtMapService();

        service.RemapBundleExtMapIds(description, group, mappings);

        List<SdpExtMapAttribute> remappedAudio = service.ResolveMediaExtMaps(description, 0);
        List<SdpExtMapAttribute> remappedVideo = service.ResolveMediaExtMaps(description, 1);
        Assert.Multiple(() =>
        {
            Assert.That(remappedAudio.Select(extension => extension.Id), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(remappedVideo.Select(extension => extension.Id), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(remappedAudio[0].Direction, Is.EqualTo(SdpDirection.SendOnly));
            Assert.That(remappedVideo[0].Direction, Is.EqualTo(SdpDirection.ReceiveOnly));
            Assert.That(remappedVideo[1].Direction, Is.EqualTo(SdpDirection.Inactive));
            Assert.That(audioShared.Value, Is.EqualTo("3/sendonly urn:example:shared configuration"));
            Assert.That(videoShared.Value, Is.EqualTo("3/recvonly urn:example:shared configuration"));
            Assert.That(audioSecond.Value, Is.EqualTo("4 urn:example:second"));
            Assert.That(videoSecond.Value, Is.EqualTo("4/inactive urn:example:second"));
            Assert.That(unrelatedMapping.Value, Is.EqualTo("1 urn:example:shared configuration"));
        });
    }

    [Test]
    public void RemapBundleExtMapIds_ValidatesAllMembersBeforeMutation()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);
        SdpMediaDescription audio = description.MediaDescriptions[0];
        audio.Attributes.Add(Mid("audio"));
        SdpAttribute audioSource = ExtMap("1 urn:example:shared");
        audio.Attributes.Add(audioSource);
        SdpMediaDescription video = Media(SdpTransportProtocols.RtpAvp);
        video.Attributes.Add(Mid("video"));
        SdpAttribute videoSource = ExtMap("2 urn:example:missing");
        video.Attributes.Add(videoSource);
        description.MediaDescriptions.Add(video);
        List<SdpHeaderExtensionMapping> incomplete = new List<SdpHeaderExtensionMapping>
        {
            IdentifierMapping(1, 7, SdpDirection.SendReceive, false, "urn:example:shared", "")
        };

        Assert.That(() => new SdpExtMapService().RemapBundleExtMapIds(description, BundleGroup("audio", "video"), incomplete), Throws.ArgumentException.With.Property("ParamName").EqualTo("mappings"));

        Assert.Multiple(() =>
        {
            Assert.That(audioSource.Value, Is.EqualTo("1 urn:example:shared"));
            Assert.That(videoSource.Value, Is.EqualTo("2 urn:example:missing"));
        });
    }

    [Test]
    public void PublicMethods_RejectNullDescriptionsInvalidIndexesAndNullUris()
    {
        ISdpExtMapService service = new SdpExtMapService();
        SdpSessionDescription empty = new SdpSessionDescription();
        SdpSessionDescription valid = DescriptionWithMedia(SdpTransportProtocols.RtpAvp);

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ResolveMediaExtMaps(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveMediaExtMapAllowMixed(empty, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.FindUriById(empty, 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.FindIdByUri(valid, 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("uri"));
            Assert.That(() => service.IntersectExtMaps(null!, new List<SdpHeaderExtension>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("offeredMappings"));
            Assert.That(() => service.IntersectExtMaps(new List<SdpExtMapAttribute>(), null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("localCapabilities"));
            Assert.That(() => service.SelectAnswerDirection(SdpDirection.Unknown, SdpDirection.SendReceive), Throws.ArgumentException.With.Property("ParamName").EqualTo("offeredDirection"));
            Assert.That(() => service.SelectAnswerDirection(SdpDirection.SendReceive, (SdpDirection)99), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("localDirection"));
            Assert.That(() => service.AllocateExtMapIds(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(() => service.RemapBundleExtMapIds(null!, new SdpGroupAttribute(), new List<SdpHeaderExtensionMapping>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.RemapBundleExtMapIds(valid, null!, new List<SdpHeaderExtensionMapping>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("bundleGroup"));
            Assert.That(() => service.RemapBundleExtMapIds(valid, new SdpGroupAttribute(), null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(() => service.RemapExtMapIds(null!, new List<SdpHeaderExtensionMapping>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("media"));
        });
    }

    static SdpGroupAttribute BundleGroup(string firstMediaId, string secondMediaId)
    {
        SdpGroupAttribute group = new SdpGroupAttribute
        {
            Semantic = SdpGroupSemantic.Bundle,
            SemanticToken = SdpGroupSemantics.Bundle
        };
        group.IdentificationTags.Add(firstMediaId);
        group.IdentificationTags.Add(secondMediaId);
        return group;
    }

    static SdpExtMapAttribute TypedMapping(int id, SdpDirection direction, bool encrypted, string uri, string extensionAttributes)
    {
        return new SdpExtMapAttribute
        {
            Id = id,
            Direction = direction,
            Encrypted = encrypted,
            Uri = uri,
            ExtensionAttributes = extensionAttributes
        };
    }

    static SdpHeaderExtensionMapping IdentifierMapping(int offeredId, int negotiatedId, SdpDirection direction, bool encrypted, string uri, string extensionAttributes)
    {
        return new SdpHeaderExtensionMapping
        {
            OfferedId = offeredId,
            NegotiatedId = negotiatedId,
            Direction = direction,
            Encrypted = encrypted,
            Uri = uri,
            ExtensionAttributes = extensionAttributes
        };
    }

    static SdpSessionDescription DescriptionWithMedia(string protocol)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(Media(protocol));
        return description;
    }

    static SdpMediaDescription Media(string protocol)
    {
        return new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = 5000,
            Protocol = protocol
        };
    }

    static SdpAttribute ExtMap(string value)
    {
        return ValueAttribute(SdpAttributeNames.ExtMap, value);
    }

    static SdpAttribute Mid(string value)
    {
        return ValueAttribute(SdpAttributeNames.MediaId, value);
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
}
