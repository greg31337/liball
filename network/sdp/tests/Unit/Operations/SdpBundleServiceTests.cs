namespace sdp.Tests.Unit.Operations;

public class SdpBundleServiceTests
{
    [Test]
    public void ResolveBundleGroups_FiltersOtherSemanticsAndPreservesSourceOrder()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpAttribute firstSource = ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video");
        SdpAttribute emptySource = ValueAttribute(SdpAttributeNames.Group, "BUNDLE");
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "FID primary repair"));
        description.Attributes.Add(firstSource);
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.Group));
        description.Attributes.Add(emptySource);
        ISdpBundleService service = new SdpBundleService();

        List<SdpBundleGroup> first = service.ResolveBundleGroups(description);
        first[0].MediaIds[0] = "changed";
        List<SdpBundleGroup> second = service.ResolveBundleGroups(description);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Count.EqualTo(2));
            Assert.That(second, Has.Count.EqualTo(2));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
            Assert.That(second[0].MediaIds, Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(second[0].TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(second[0].SourceGroup!.SourceAttribute, Is.SameAs(firstSource));
            Assert.That(second[1].MediaIds, Is.Empty);
            Assert.That(second[1].TaggedMediaId, Is.Empty);
            Assert.That(second[1].SourceGroup!.SourceAttribute, Is.SameAs(emptySource));
        });
    }

    [Test]
    public void BundleMembershipAndTaggedIndex_UseExactFirstMatchingIdentifiers()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        description.MediaDescriptions.Add(Media("audio"));
        description.MediaDescriptions.Add(Media("video"));
        description.MediaDescriptions.Add(Media("data"));
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup? videoGroup = service.FindBundleGroupForMedia(description, 1);
        SdpBundleGroup? dataGroup = service.FindBundleGroupForMedia(description, 2);

        Assert.Multiple(() =>
        {
            Assert.That(videoGroup, Is.Not.Null);
            Assert.That(videoGroup!.MediaIds, Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(service.ResolveBundleTaggedMediaIndex(description, 0), Is.Zero);
            Assert.That(service.ResolveBundleTaggedMediaIndex(description, 1), Is.Zero);
            Assert.That(service.ResolveBundleTaggedMediaIndex(description, 2), Is.EqualTo(2));
            Assert.That(dataGroup, Is.Null);
        });
    }

    [Test]
    public void UnresolvedBundleTag_DoesNotBorrowAnotherMediaTransport()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE missing video"));
        description.MediaDescriptions.Add(Media("video"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "video"));
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup? group = service.FindBundleGroupForMedia(description, 0);
        int taggedIndex = service.ResolveBundleTaggedMediaIndex(description, 0);
        SdpBundleTransport? transport = service.ResolveBundleTransport(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(group, Is.Not.Null);
            Assert.That(group!.TaggedMediaId, Is.EqualTo("missing"));
            Assert.That(taggedIndex, Is.Zero);
            Assert.That(transport, Is.Null);
        });
    }

    [Test]
    public void ResolveBundleTransport_UsesEveryEffectiveTaggedTransportCategory()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.IceLite));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.IceOptions, "trickle ice2"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Fingerprint, "sha-256 AA:BB"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMap, "1 " + SdpHeaderExtensionUris.MediaId));
        description.Connection = Connection("192.0.2.1");
        SdpMediaDescription audio = Media("audio");
        audio.Protocol = SdpTransportProtocols.UdpTlsRtpSavp;
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "owner"));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "abcdefghijklmnopqrstuv"));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.Candidate, "one 1 UDP 100 192.0.2.1 5000 typ host"));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "actpass"));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.TlsId, "owner-association-001"));
        audio.Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        SdpMediaDescription video = Media("video");
        video.Protocol = SdpTransportProtocols.UdpTlsRtpSavp;
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "other"));
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "zyxwvutsrqponmlkjihgfe"));
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.Fingerprint, "sha-256 CC:DD"));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        ISdpBundleService service = new SdpBundleService();

        SdpBundleTransport? transport = service.ResolveBundleTransport(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(transport, Is.Not.Null);
            Assert.That(transport!.TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(transport.Connection!.Address, Is.EqualTo("192.0.2.1"));
            Assert.That(transport.Connection, Is.Not.SameAs(description.Connection));
            Assert.That(transport.Ice, Is.Not.Null);
            Assert.That(transport.Ice!.Credentials!.UsernameFragment, Is.EqualTo("owner"));
            Assert.That(transport.Ice.Options, Is.EqualTo(new[] { "trickle", "ice2" }));
            Assert.That(transport.Ice.IceLite, Is.True);
            Assert.That(transport.Ice.Candidates.Single().Foundation, Is.EqualTo("one"));
            Assert.That(transport.Dtls, Is.Not.Null);
            Assert.That(transport.Dtls!.Fingerprints.Single().Fingerprint, Is.EqualTo("AA:BB"));
            Assert.That(transport.Dtls.SetupRole, Is.EqualTo(SdpDtlsSetupRole.ActPass));
            Assert.That(transport.Dtls.TlsId, Is.EqualTo("owner-association-001"));
            Assert.That(transport.HeaderExtensions.Single().Uri, Is.EqualTo(SdpHeaderExtensionUris.MediaId));
            Assert.That(transport.RtcpMux, Is.True);
        });
    }

    [Test]
    public void TransportProjection_UsesMediaConnectionAndReturnsIndependentState()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        description.Connection = Connection("192.0.2.1");
        SdpMediaDescription audio = Media("audio");
        audio.Connections.Add(Connection("203.0.113.1"));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(Media("video"));
        ISdpBundleService service = new SdpBundleService();

        SdpBundleTransport? first = service.ResolveBundleTransport(description, 1);
        first!.Connection!.Address = "changed";
        SdpBundleTransport? second = service.ResolveBundleTransport(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.Connection!.Address, Is.EqualTo("203.0.113.1"));
            Assert.That(second.Connection, Is.Not.SameAs(audio.Connections[0]));
            Assert.That(second.Ice, Is.Null);
            Assert.That(second.Dtls, Is.Null);
            Assert.That(second.HeaderExtensions, Is.Empty);
            Assert.That(second.RtcpMux, Is.False);
            Assert.That(description.MediaDescriptions[0].Connections[0].Address, Is.EqualTo("203.0.113.1"));
        });
    }

    [Test]
    public void TransportProjection_UsesTaggedSharedAttributesAndSelectedNormalAttributes()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        SdpMediaDescription audio = Media("audio");
        audio.Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.Fingerprint, "sha-256 AA:BB"));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.Content, "slides"));
        audio.Attributes.Add(PropertyAttribute("x-owner"));
        SdpMediaDescription video = Media("video");
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.Fingerprint, "sha-256 CC:DD"));
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.Content, "speaker"));
        video.Attributes.Add(PropertyAttribute("x-video"));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        ISdpBundleService service = new SdpBundleService();

        SdpBundleTransport? transport = service.ResolveBundleTransport(description, 1);

        Assert.That(transport, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(transport!.SharedAttributes.Select(attribute => attribute.Name), Is.EqualTo(new[] { SdpAttributeNames.RtcpMux, SdpAttributeNames.Fingerprint }));
            Assert.That(transport.SharedAttributes[1].Value, Is.EqualTo("sha-256 AA:BB"));
            Assert.That(transport.MediaAttributes.Select(attribute => attribute.Name), Is.EqualTo(new[] { SdpAttributeNames.MediaId, SdpAttributeNames.Content }));
            Assert.That(transport.MediaAttributes[1].Value, Is.EqualTo("speaker"));
            Assert.That(transport.SharedAttributes.All(attribute => attribute.Name != "x-owner"), Is.True);
            Assert.That(transport.MediaAttributes.All(attribute => attribute.Name != "x-video"), Is.True);
            Assert.That(transport.SharedAttributes[0], Is.Not.SameAs(audio.Attributes[1]));
            Assert.That(transport.MediaAttributes[0], Is.Not.SameAs(video.Attributes[0]));
        });

        transport!.SharedAttributes[1].Value = "changed";
        transport.MediaAttributes[1].Value = "changed";
        SdpBundleTransport? repeated = service.ResolveBundleTransport(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(repeated!.SharedAttributes[1].Value, Is.EqualTo("sha-256 AA:BB"));
            Assert.That(repeated.MediaAttributes[1].Value, Is.EqualTo("speaker"));
            Assert.That(audio.Attributes[2].Value, Is.EqualTo("sha-256 AA:BB"));
            Assert.That(video.Attributes[2].Value, Is.EqualTo("speaker"));
        });
    }

    [Test]
    public void TransportProjection_AggregatesSumBandwidthAndKeepsNormalBandwidthPerMedia()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        SdpMediaDescription audio = Media("audio");
        audio.Bandwidths.Add(Bandwidth(SdpBandwidthTypes.ConferenceTotal, 10));
        audio.Bandwidths.Add(Bandwidth(SdpBandwidthTypes.ApplicationSpecific, 64));
        audio.Bandwidths.Add(Bandwidth(SdpBandwidthTypes.RtcpSender, 2));
        SdpMediaDescription video = Media("video");
        video.Bandwidths.Add(Bandwidth(SdpBandwidthTypes.ConferenceTotal, 20));
        video.Bandwidths.Add(Bandwidth(SdpBandwidthTypes.ApplicationSpecific, 256));
        video.Bandwidths.Add(Bandwidth(SdpBandwidthTypes.RtcpReceiver, 3));
        video.Bandwidths.Add(Bandwidth(SdpBandwidthTypes.TransportIndependentApplicationSpecific, 1000));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        ISdpBundleService service = new SdpBundleService();

        SdpBundleTransport? audioTransport = service.ResolveBundleTransport(description, 0);
        SdpBundleTransport? videoTransport = service.ResolveBundleTransport(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(audioTransport!.Bandwidths.Select(bandwidth => bandwidth.Type), Is.EqualTo(new[] { "CT", "AS", "RS", "RR" }));
            Assert.That(audioTransport.Bandwidths.Select(bandwidth => bandwidth.Value), Is.EqualTo(new long[] { 10, 320, 2, 3 }));
            Assert.That(videoTransport!.Bandwidths.Select(bandwidth => bandwidth.Type), Is.EqualTo(new[] { "CT", "AS", "RS", "RR" }));
            Assert.That(videoTransport.Bandwidths.Select(bandwidth => bandwidth.Value), Is.EqualTo(new long[] { 20, 320, 2, 3 }));
            Assert.That(videoTransport.Bandwidths[1].RawValue, Is.EqualTo("320"));
            Assert.That(videoTransport.Bandwidths[1].RawText, Is.EqualTo("AS:320"));
            Assert.That(videoTransport.Bandwidths.All(bandwidth => bandwidth.Type != "TIAS"), Is.True);
            Assert.That(videoTransport.Bandwidths[0], Is.Not.SameAs(video.Bandwidths[0]));
        });

        videoTransport!.Bandwidths[1].Value = 1;
        Assert.That(service.ResolveBundleTransport(description, 1)!.Bandwidths[1].Value, Is.EqualTo(320));
    }

    [Test]
    public void UnbundledMedia_HasNoBundleTransport()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(Media("audio"));
        ISdpBundleService service = new SdpBundleService();

        Assert.Multiple(() =>
        {
            Assert.That(service.FindBundleGroupForMedia(description, 0), Is.Null);
            Assert.That(service.ResolveBundleTaggedMediaIndex(description, 0), Is.Zero);
            Assert.That(service.ResolveBundleTransport(description, 0), Is.Null);
        });
    }

    [Test]
    public void CreateInitialBundleOffer_AllocatesMidsAndOrdersTheTagFirst()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Connection = Connection("192.0.2.1");
        description.Attributes.Add(PropertyAttribute("x-before"));
        description.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Audio, 5000));
        description.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Audio, 5002));
        description.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Video, 5000));
        List<int> mediaIndexes = new List<int> { 2, 0, 1 };
        List<int> bundleOnlyIndexes = new List<int> { 2 };
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup group = service.CreateInitialBundleOffer(description, mediaIndexes, 1, bundleOnlyIndexes);

        Assert.Multiple(() =>
        {
            Assert.That(group.TaggedMediaId, Is.EqualTo("audio-2"));
            Assert.That(group.MediaIds, Is.EqualTo(new[] { "audio-2", "video", "audio" }));
            Assert.That(group.SourceGroup!.SourceAttribute, Is.SameAs(description.Attributes[1]));
            Assert.That(description.Attributes[1].RawText, Is.EqualTo("group:BUNDLE audio-2 video audio"));
            Assert.That(new SdpGroupService().ResolveMediaId(description, 0)!.Id, Is.EqualTo("audio"));
            Assert.That(new SdpGroupService().ResolveMediaId(description, 1)!.Id, Is.EqualTo("audio-2"));
            Assert.That(new SdpGroupService().ResolveMediaId(description, 2)!.Id, Is.EqualTo("video"));
            Assert.That(description.MediaDescriptions[0].Port, Is.EqualTo(5000));
            Assert.That(description.MediaDescriptions[1].Port, Is.EqualTo(5002));
            Assert.That(description.MediaDescriptions[2].Port, Is.Zero);
            Assert.That(HasAttribute(description.MediaDescriptions[2].Attributes, SdpAttributeNames.BundleOnly), Is.True);
            Assert.That(HasAttribute(description.MediaDescriptions[1].Attributes, SdpAttributeNames.BundleOnly), Is.False);
            Assert.That(mediaIndexes, Is.EqualTo(new[] { 2, 0, 1 }));
            Assert.That(bundleOnlyIndexes, Is.EqualTo(new[] { 2 }));
        });
    }

    [Test]
    public void CreateInitialBundleOffer_PreservesExistingMidsAndAllocatesEveryMediaSection()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription audio = OfferMedia(SdpMediaTypes.Audio, 5000);
        audio.Connections.Add(Connection("192.0.2.1"));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "primary"));
        SdpMediaDescription video = OfferMedia(SdpMediaTypes.Video, 5000);
        video.Connections.Add(Connection("192.0.2.2"));
        SdpMediaDescription data = OfferMedia(SdpMediaTypes.Application, 6000);
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        description.MediaDescriptions.Add(data);
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup group = service.CreateInitialBundleOffer(description, new List<int> { 0, 1 }, 0, new List<int>());

        Assert.Multiple(() =>
        {
            Assert.That(group.MediaIds, Is.EqualTo(new[] { "primary", "video" }));
            Assert.That(audio.Attributes.Count(attribute => attribute.Name == SdpAttributeNames.MediaId), Is.EqualTo(1));
            Assert.That(new SdpGroupService().ResolveMediaId(description, 0)!.Id, Is.EqualTo("primary"));
            Assert.That(new SdpGroupService().ResolveMediaId(description, 1)!.Id, Is.EqualTo("video"));
            Assert.That(new SdpGroupService().ResolveMediaId(description, 2)!.Id, Is.EqualTo("application"));
            Assert.That(description.MediaDescriptions.All(media => !HasAttribute(media.Attributes, SdpAttributeNames.BundleOnly)), Is.True);
        });
    }

    [Test]
    public void CreateInitialBundleOffer_ValidatesFallbacksBeforeMutation()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Connection = Connection("192.0.2.1");
        description.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Audio, 5000));
        description.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Video, 5000));
        ISdpBundleService service = new SdpBundleService();

        SdpNegotiationException? exception = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleOffer(description, new List<int> { 0, 1 }, 0, new List<int>()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(description.Attributes, Is.Empty);
            Assert.That(description.MediaDescriptions[0].Attributes, Is.Empty);
            Assert.That(description.MediaDescriptions[1].Attributes, Is.Empty);
            Assert.That(description.MediaDescriptions[0].Port, Is.EqualTo(5000));
            Assert.That(description.MediaDescriptions[1].Port, Is.EqualTo(5000));
        });
    }

    [Test]
    public void CreateInitialBundleOffer_RejectsOverlappingFallbackPortRanges()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Connection = Connection("192.0.2.1");
        SdpMediaDescription first = OfferMedia(SdpMediaTypes.Audio, 5000);
        first.HasPortCount = true;
        first.PortCount = 2;
        description.MediaDescriptions.Add(first);
        description.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Video, 5001));

        SdpNegotiationException? exception = Assert.Throws<SdpNegotiationException>(() => new SdpBundleService().CreateInitialBundleOffer(description, new List<int> { 0, 1 }, 0, new List<int>()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(description.Attributes, Is.Empty);
            Assert.That(description.MediaDescriptions.All(media => media.Attributes.Count == 0), Is.True);
        });
    }

    [Test]
    public void CreateInitialBundleOffer_RejectsInvalidMembershipAndExistingState()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Connection = Connection("192.0.2.1");
        description.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Audio, 5000));
        description.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Video, 5002));
        ISdpBundleService service = new SdpBundleService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.CreateInitialBundleOffer(description, new List<int>(), 0, new List<int>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("mediaIndexes"));
            Assert.That(() => service.CreateInitialBundleOffer(description, new List<int> { 0, 0 }, 0, new List<int>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("mediaIndexes"));
            Assert.That(() => service.CreateInitialBundleOffer(description, new List<int> { 0 }, 1, new List<int>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("taggedMediaIndex"));
            Assert.That(() => service.CreateInitialBundleOffer(description, new List<int> { 0, 1 }, 0, new List<int> { 0 }), Throws.ArgumentException.With.Property("ParamName").EqualTo("taggedMediaIndex"));
            Assert.That(() => service.CreateInitialBundleOffer(description, new List<int> { 0 }, 0, new List<int> { 1 }), Throws.ArgumentException.With.Property("ParamName").EqualTo("bundleOnlyMediaIndexes"));
        });

        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        SdpNegotiationException? existingException = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleOffer(description, new List<int> { 0, 1 }, 0, new List<int>()));

        Assert.Multiple(() =>
        {
            Assert.That(existingException!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(description.Attributes, Has.Count.EqualTo(1));
            Assert.That(description.MediaDescriptions.All(media => media.Attributes.Count == 0), Is.True);
        });
    }

    [Test]
    public void CreateInitialBundleOffer_RejectsMalformedOrDuplicateExistingMidsAtomically()
    {
        SdpSessionDescription malformed = new SdpSessionDescription();
        malformed.Connection = Connection("192.0.2.1");
        malformed.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Audio, 5000));
        malformed.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.MediaId));
        SdpSessionDescription duplicate = new SdpSessionDescription();
        duplicate.Connection = Connection("192.0.2.1");
        duplicate.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Audio, 5000));
        duplicate.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Video, 5002));
        duplicate.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "shared"));
        duplicate.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "shared"));
        ISdpBundleService service = new SdpBundleService();

        SdpNegotiationException? malformedException = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleOffer(malformed, new List<int> { 0 }, 0, new List<int>()));
        SdpNegotiationException? duplicateException = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleOffer(duplicate, new List<int> { 0, 1 }, 0, new List<int>()));

        Assert.Multiple(() =>
        {
            Assert.That(malformedException!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(duplicateException!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(malformed.Attributes, Is.Empty);
            Assert.That(malformed.MediaDescriptions[0].Attributes, Has.Count.EqualTo(1));
            Assert.That(duplicate.Attributes, Is.Empty);
            Assert.That(duplicate.MediaDescriptions.SelectMany(media => media.Attributes).All(attribute => attribute.Name == SdpAttributeNames.MediaId), Is.True);
        });
    }

    [Test]
    public void CreateInitialBundleAnswer_AcceptsSubsetAndSelectsFirstEligibleOfferedTag()
    {
        SdpSessionDescription offer = InitialBundleOffer();
        SdpSessionDescription answer = InitialBundleAnswer();
        answer.MediaDescriptions[0].Port = 0;
        answer.MediaDescriptions[1].Connections.Add(Connection("203.0.113.20"));
        answer.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "video"));
        answer.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "tag-owner"));
        answer.MediaDescriptions[1].Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        answer.MediaDescriptions[2].Port = 0;
        answer.MediaDescriptions[2].Connections.Add(Connection("203.0.113.30"));
        answer.MediaDescriptions[2].Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));
        answer.MediaDescriptions[2].Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "abcdefghijklmnopqrstuv"));
        answer.MediaDescriptions[2].Attributes.Add(ValueAttribute(SdpAttributeNames.TlsId, "data-association-001"));
        answer.MediaDescriptions[2].Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        answer.MediaDescriptions[2].Attributes.Add(ValueAttribute(SdpAttributeNames.RtpMap, "101 telephone-event/8000"));
        answer.MediaDescriptions[2].Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMap, "1 " + SdpHeaderExtensionUris.MediaId));
        answer.MediaDescriptions[2].Attributes.Add(PropertyAttribute("x-media"));
        List<int> accepted = new List<int> { 2, 1 };
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup? group = service.CreateInitialBundleAnswer(offer, answer, 0, accepted);

        Assert.Multiple(() =>
        {
            Assert.That(group, Is.Not.Null);
            Assert.That(group!.TaggedMediaId, Is.EqualTo("video"));
            Assert.That(group.MediaIds, Is.EqualTo(new[] { "video", "data" }));
            Assert.That(group.SourceGroup!.SourceAttribute, Is.SameAs(answer.Attributes[0]));
            Assert.That(answer.Attributes[0].RawText, Is.EqualTo("group:BUNDLE video data"));
            Assert.That(new SdpGroupService().ResolveMediaId(answer, 0)!.Id, Is.EqualTo("audio"));
            Assert.That(new SdpGroupService().ResolveMediaId(answer, 1)!.Id, Is.EqualTo("video"));
            Assert.That(new SdpGroupService().ResolveMediaId(answer, 2)!.Id, Is.EqualTo("data"));
            Assert.That(answer.MediaDescriptions[0].Port, Is.Zero);
            Assert.That(answer.MediaDescriptions[1].Port, Is.EqualTo(20002));
            Assert.That(answer.MediaDescriptions[2].Port, Is.EqualTo(20002));
            Assert.That(answer.MediaDescriptions[2].Connections.Single().Address, Is.EqualTo("203.0.113.20"));
            Assert.That(answer.MediaDescriptions[2].Connections[0], Is.Not.SameAs(answer.MediaDescriptions[1].Connections[0]));
            Assert.That(HasAttribute(answer.MediaDescriptions[1].Attributes, SdpAttributeNames.IceUsernameFragment), Is.True);
            Assert.That(HasAttribute(answer.MediaDescriptions[1].Attributes, SdpAttributeNames.RtcpMux), Is.True);
            Assert.That(HasAttribute(answer.MediaDescriptions[2].Attributes, SdpAttributeNames.IcePassword), Is.False);
            Assert.That(HasAttribute(answer.MediaDescriptions[2].Attributes, SdpAttributeNames.TlsId), Is.False);
            Assert.That(HasAttribute(answer.MediaDescriptions[2].Attributes, SdpAttributeNames.RtcpMux), Is.False);
            Assert.That(HasAttribute(answer.MediaDescriptions[2].Attributes, SdpAttributeNames.RtpMap), Is.True);
            Assert.That(HasAttribute(answer.MediaDescriptions[2].Attributes, SdpAttributeNames.ExtMap), Is.True);
            Assert.That(HasAttribute(answer.MediaDescriptions[2].Attributes, "x-media"), Is.True);
            Assert.That(answer.MediaDescriptions.All(media => !HasAttribute(media.Attributes, SdpAttributeNames.BundleOnly)), Is.True);
            Assert.That(offer.MediaDescriptions[2].Port, Is.Zero);
            Assert.That(HasAttribute(offer.MediaDescriptions[2].Attributes, SdpAttributeNames.BundleOnly), Is.True);
            Assert.That(accepted, Is.EqualTo(new[] { 2, 1 }));
        });
    }

    [Test]
    public void CreateInitialBundleAnswer_MovesOrdinaryMediaAndRejectsOmittedBundleOnlyMedia()
    {
        SdpSessionDescription offer = InitialBundleOffer();
        SdpSessionDescription answer = InitialBundleAnswer();
        answer.MediaDescriptions[1].Connections.Add(Connection("203.0.113.22"));
        answer.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "standalone"));
        answer.MediaDescriptions[2].Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup? group = service.CreateInitialBundleAnswer(offer, answer, 0, new List<int> { 0 });

        Assert.Multiple(() =>
        {
            Assert.That(group, Is.Not.Null);
            Assert.That(group!.MediaIds, Is.EqualTo(new[] { "audio" }));
            Assert.That(answer.MediaDescriptions[0].Port, Is.EqualTo(20000));
            Assert.That(answer.MediaDescriptions[1].Port, Is.EqualTo(20002));
            Assert.That(answer.MediaDescriptions[1].Connections.Single().Address, Is.EqualTo("203.0.113.22"));
            Assert.That(HasAttribute(answer.MediaDescriptions[1].Attributes, SdpAttributeNames.IceUsernameFragment), Is.True);
            Assert.That(answer.MediaDescriptions[2].Port, Is.Zero);
            Assert.That(answer.MediaDescriptions[2].HasPortCount, Is.False);
            Assert.That(answer.MediaDescriptions[2].PortCount, Is.EqualTo(1));
            Assert.That(HasAttribute(answer.MediaDescriptions[2].Attributes, SdpAttributeNames.BundleOnly), Is.False);
            Assert.That(new SdpGroupService().ResolveMediaId(answer, 2)!.Id, Is.EqualTo("data"));
        });
    }

    [Test]
    public void CreateInitialBundleAnswer_WithNoAcceptedMembersOmitsTheGroup()
    {
        SdpSessionDescription offer = InitialBundleOffer();
        SdpSessionDescription answer = InitialBundleAnswer();
        answer.MediaDescriptions[2].Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));
        answer.MediaDescriptions[2].HasPortCount = true;
        answer.MediaDescriptions[2].PortCount = 3;
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup? group = service.CreateInitialBundleAnswer(offer, answer, 0, new List<int>());

        Assert.Multiple(() =>
        {
            Assert.That(group, Is.Null);
            Assert.That(answer.Attributes, Is.Empty);
            Assert.That(answer.MediaDescriptions[0].Port, Is.EqualTo(20000));
            Assert.That(answer.MediaDescriptions[1].Port, Is.EqualTo(20002));
            Assert.That(answer.MediaDescriptions[2].Port, Is.Zero);
            Assert.That(answer.MediaDescriptions[2].HasPortCount, Is.False);
            Assert.That(answer.MediaDescriptions[2].PortCount, Is.EqualTo(1));
            Assert.That(answer.MediaDescriptions.All(media => !HasAttribute(media.Attributes, SdpAttributeNames.BundleOnly)), Is.True);
            Assert.That(answer.MediaDescriptions.Select(media => new SdpGroupService().ResolveMediaId(answer, answer.MediaDescriptions.IndexOf(media))!.Id), Is.EqualTo(new[] { "audio", "video", "data" }));
        });
    }

    [Test]
    public void CreateInitialBundleAnswer_RejectsInvalidSelectionsBeforeMutation()
    {
        SdpSessionDescription offer = InitialBundleOffer();
        SdpSessionDescription answer = InitialBundleAnswer();
        answer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "wrong"));
        ISdpBundleService service = new SdpBundleService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.CreateInitialBundleAnswer(offer, answer, -1, new List<int>()), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("offeredBundleGroupIndex"));
            Assert.That(() => service.CreateInitialBundleAnswer(offer, answer, 0, new List<int> { 0, 0 }), Throws.ArgumentException.With.Property("ParamName").EqualTo("acceptedMediaIndexes"));
            Assert.That(() => service.CreateInitialBundleAnswer(offer, answer, 0, new List<int> { 3 }), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("acceptedMediaIndexes"));
        });

        SdpNegotiationException? mismatch = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleAnswer(offer, answer, 0, new List<int> { 0 }));
        SdpNegotiationException? bundleOnlyTag = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleAnswer(offer, InitialBundleAnswer(), 0, new List<int> { 2 }));

        Assert.Multiple(() =>
        {
            Assert.That(mismatch!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(bundleOnlyTag!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(answer.Attributes, Is.Empty);
            Assert.That(answer.MediaDescriptions[0].Attributes, Has.Count.EqualTo(1));
            Assert.That(answer.MediaDescriptions[1].Attributes, Is.Empty);
            Assert.That(answer.MediaDescriptions[2].Attributes, Is.Empty);
            Assert.That(answer.MediaDescriptions[0].Port, Is.EqualTo(20000));
            Assert.That(answer.MediaDescriptions[2].Port, Is.EqualTo(20004));
        });
    }

    [Test]
    public void CreateInitialBundleAnswer_RequiresUsableTaggedAnswerTransport()
    {
        SdpSessionDescription offer = InitialBundleOffer();
        SdpSessionDescription answer = InitialBundleAnswer();
        answer.Connection = null;
        ISdpBundleService service = new SdpBundleService();

        SdpNegotiationException? exception = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleAnswer(offer, answer, 0, new List<int> { 0, 1 }));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(answer.Attributes, Is.Empty);
            Assert.That(answer.MediaDescriptions.All(media => media.Attributes.Count == 0), Is.True);
            Assert.That(answer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 20000, 20002, 20004 }));
        });
    }

    [Test]
    public void UpdateSubsequentBundleOffer_AddsMovesDisablesAndReactivatesWithoutChangingIdentity()
    {
        SdpSessionDescription offer = SubsequentBundleOffer();
        SdpAttribute groupSource = offer.Attributes[0];
        List<SdpMediaDescription> mediaOrder = new List<SdpMediaDescription>(offer.MediaDescriptions);
        offer.MediaDescriptions[1].Port = 6000;
        offer.MediaDescriptions[1].Connections.Clear();
        offer.MediaDescriptions[1].Connections.Add(Connection("203.0.113.60"));
        offer.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "standalone"));
        offer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.Fingerprint, "sha-256 AA:BB"));
        offer.MediaDescriptions[2].Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));
        offer.MediaDescriptions[4].Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));
        offer.MediaDescriptions[4].Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "abcdefghijklmnopqrstuv"));
        offer.MediaDescriptions[4].Attributes.Add(ValueAttribute(SdpAttributeNames.RtpMap, "102 H264/90000"));
        List<int> bundled = new List<int> { 4, 0, 3 };
        List<int> disabled = new List<int> { 2 };
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup? group = service.UpdateSubsequentBundleOffer(offer, 0, bundled, 3, disabled);

        Assert.Multiple(() =>
        {
            Assert.That(group, Is.Not.Null);
            Assert.That(group!.TaggedMediaId, Is.EqualTo("control"));
            Assert.That(group.MediaIds, Is.EqualTo(new[] { "control", "screen", "audio" }));
            Assert.That(group.SourceGroup!.SourceAttribute, Is.SameAs(groupSource));
            Assert.That(groupSource.RawText, Is.EqualTo("group:BUNDLE control screen audio"));
            Assert.That(offer.MediaDescriptions, Is.EqualTo(mediaOrder));
            Assert.That(offer.MediaDescriptions.Select(media => new SdpGroupService().ResolveMediaId(offer, offer.MediaDescriptions.IndexOf(media))!.Id), Is.EqualTo(new[] { "audio", "video", "data", "control", "screen" }));
            Assert.That(offer.MediaDescriptions[0].Port, Is.EqualTo(5000));
            Assert.That(offer.MediaDescriptions[4].Port, Is.EqualTo(5000));
            Assert.That(offer.MediaDescriptions[0].Connections.Single().Address, Is.EqualTo("203.0.113.50"));
            Assert.That(offer.MediaDescriptions[4].Connections.Single().Address, Is.EqualTo("203.0.113.50"));
            Assert.That(offer.MediaDescriptions[0].Connections[0], Is.Not.SameAs(offer.MediaDescriptions[3].Connections[0]));
            Assert.That(offer.MediaDescriptions[1].Port, Is.EqualTo(6000));
            Assert.That(offer.MediaDescriptions[1].Connections.Single().Address, Is.EqualTo("203.0.113.60"));
            Assert.That(HasAttribute(offer.MediaDescriptions[1].Attributes, SdpAttributeNames.IceUsernameFragment), Is.True);
            Assert.That(HasAttribute(offer.MediaDescriptions[0].Attributes, SdpAttributeNames.Fingerprint), Is.False);
            Assert.That(HasAttribute(offer.MediaDescriptions[3].Attributes, SdpAttributeNames.Fingerprint), Is.True);
            Assert.That(offer.MediaDescriptions[2].Port, Is.Zero);
            Assert.That(offer.MediaDescriptions[2].HasPortCount, Is.False);
            Assert.That(HasAttribute(offer.MediaDescriptions[4].Attributes, SdpAttributeNames.IcePassword), Is.False);
            Assert.That(HasAttribute(offer.MediaDescriptions[4].Attributes, SdpAttributeNames.RtpMap), Is.True);
            Assert.That(offer.MediaDescriptions.All(media => !HasAttribute(media.Attributes, SdpAttributeNames.BundleOnly)), Is.True);
            Assert.That(bundled, Is.EqualTo(new[] { 4, 0, 3 }));
            Assert.That(disabled, Is.EqualTo(new[] { 2 }));
        });
    }

    [Test]
    public void UpdateSubsequentBundleOffer_CanDissolveTheGroup()
    {
        SdpSessionDescription offer = SubsequentBundleOffer();
        offer.MediaDescriptions[0].Port = 6100;
        offer.MediaDescriptions[0].Connections.Add(Connection("203.0.113.61"));
        offer.MediaDescriptions[1].Port = 6200;
        offer.MediaDescriptions[1].Connections.Add(Connection("203.0.113.62"));
        offer.MediaDescriptions[3].Port = 6300;
        offer.MediaDescriptions[3].Connections.Clear();
        offer.MediaDescriptions[3].Connections.Add(Connection("203.0.113.63"));
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup? group = service.UpdateSubsequentBundleOffer(offer, 0, new List<int>(), -1, new List<int> { 2 });

        Assert.Multiple(() =>
        {
            Assert.That(group, Is.Null);
            Assert.That(service.ResolveBundleGroups(offer), Is.Empty);
            Assert.That(offer.MediaDescriptions, Has.Count.EqualTo(5));
            Assert.That(offer.MediaDescriptions[0].Port, Is.EqualTo(6100));
            Assert.That(offer.MediaDescriptions[1].Port, Is.EqualTo(6200));
            Assert.That(offer.MediaDescriptions[2].Port, Is.Zero);
            Assert.That(offer.MediaDescriptions[3].Port, Is.EqualTo(6300));
            Assert.That(offer.MediaDescriptions.Select(media => new SdpGroupService().ResolveMediaId(offer, offer.MediaDescriptions.IndexOf(media))!.Id), Is.EqualTo(new[] { "audio", "video", "data", "control", "screen" }));
        });
    }

    [Test]
    public void UpdateSubsequentBundleOffer_RejectsNonUniqueMovedTransportBeforeMutation()
    {
        SdpSessionDescription offer = SubsequentBundleOffer();
        SdpAttribute groupSource = offer.Attributes[0];
        ISdpBundleService service = new SdpBundleService();

        SdpNegotiationException? exception = Assert.Throws<SdpNegotiationException>(() => service.UpdateSubsequentBundleOffer(offer, 0, new List<int> { 0, 2, 3 }, 0, new List<int>()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(offer.Attributes.Single(), Is.SameAs(groupSource));
            Assert.That(groupSource.RawText, Is.EqualTo("group:BUNDLE audio video data control"));
            Assert.That(offer.MediaDescriptions.All(media => !HasAttribute(media.Attributes, SdpAttributeNames.BundleOnly)), Is.True);
            Assert.That(offer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 5000, 5000, 5000, 5000, 0 }));
        });
    }

    [Test]
    public void UpdateSubsequentBundleAnswer_RejectsOmittedMembersAndUpdatesExistingGroup()
    {
        SdpSessionDescription offer = SubsequentAnswerOffer();
        SdpSessionDescription answer = SubsequentAnswerDescription();
        SdpAttribute groupSource = answer.Attributes[0];
        List<SdpMediaDescription> mediaOrder = new List<SdpMediaDescription>(answer.MediaDescriptions);
        answer.MediaDescriptions[2].Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "abcdefghijklmnopqrstuv"));
        answer.MediaDescriptions[2].Attributes.Add(ValueAttribute(SdpAttributeNames.RtpMap, "103 VP8/90000"));
        answer.MediaDescriptions[3].Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));
        answer.MediaDescriptions[3].Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup group = service.UpdateSubsequentBundleAnswer(offer, answer, 0, new List<int> { 3, 2, 0 });

        Assert.Multiple(() =>
        {
            Assert.That(group.TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(group.MediaIds, Is.EqualTo(new[] { "audio", "data", "control" }));
            Assert.That(group.SourceGroup!.SourceAttribute, Is.SameAs(groupSource));
            Assert.That(groupSource.RawText, Is.EqualTo("group:BUNDLE audio data control"));
            Assert.That(answer.MediaDescriptions, Is.EqualTo(mediaOrder));
            Assert.That(answer.MediaDescriptions.Select(media => new SdpGroupService().ResolveMediaId(answer, answer.MediaDescriptions.IndexOf(media))!.Id), Is.EqualTo(new[] { "audio", "video", "data", "control" }));
            Assert.That(answer.MediaDescriptions[0].Port, Is.EqualTo(7000));
            Assert.That(answer.MediaDescriptions[2].Port, Is.EqualTo(7000));
            Assert.That(answer.MediaDescriptions[3].Port, Is.EqualTo(7000));
            Assert.That(answer.MediaDescriptions[2].Connections.Single().Address, Is.EqualTo("203.0.113.70"));
            Assert.That(answer.MediaDescriptions[3].Connections.Single().Address, Is.EqualTo("203.0.113.70"));
            Assert.That(answer.MediaDescriptions[1].Port, Is.Zero);
            Assert.That(answer.MediaDescriptions[1].HasPortCount, Is.False);
            Assert.That(HasAttribute(answer.MediaDescriptions[2].Attributes, SdpAttributeNames.IcePassword), Is.False);
            Assert.That(HasAttribute(answer.MediaDescriptions[2].Attributes, SdpAttributeNames.RtpMap), Is.True);
            Assert.That(HasAttribute(answer.MediaDescriptions[3].Attributes, SdpAttributeNames.RtcpMux), Is.False);
            Assert.That(answer.MediaDescriptions.All(media => !HasAttribute(media.Attributes, SdpAttributeNames.BundleOnly)), Is.True);
        });
    }

    [Test]
    public void UpdateSubsequentBundleAnswer_CannotRejectTheEstablishedTag()
    {
        SdpSessionDescription offer = SubsequentAnswerOffer();
        SdpSessionDescription answer = SubsequentAnswerDescription();
        SdpAttribute groupSource = answer.Attributes[0];
        ISdpBundleService service = new SdpBundleService();

        SdpNegotiationException? exception = Assert.Throws<SdpNegotiationException>(() => service.UpdateSubsequentBundleAnswer(offer, answer, 0, new List<int> { 1, 2 }));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(answer.Attributes.Single(), Is.SameAs(groupSource));
            Assert.That(groupSource.RawText, Is.EqualTo("group:BUNDLE audio video data"));
            Assert.That(answer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 7000, 7000, 7000, 0 }));
        });
    }

    [Test]
    public void MultipleBundleGroups_ResolveIndependentlyAndRejectCrossGroupMoves()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE data control"));
        SdpMediaDescription audio = OfferMedia(SdpMediaTypes.Audio, 5000);
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "audio"));
        audio.Connections.Add(Connection("203.0.113.10"));
        SdpMediaDescription video = OfferMedia(SdpMediaTypes.Video, 5000);
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "video"));
        SdpMediaDescription data = OfferMedia(SdpMediaTypes.Application, 6000);
        data.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "data"));
        data.Connections.Add(Connection("203.0.113.20"));
        SdpMediaDescription control = OfferMedia(SdpMediaTypes.Application, 6000);
        control.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "control"));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        description.MediaDescriptions.Add(data);
        description.MediaDescriptions.Add(control);
        ISdpBundleService service = new SdpBundleService();

        SdpBundleTransport? videoTransport = service.ResolveBundleTransport(description, 1);
        SdpBundleTransport? controlTransport = service.ResolveBundleTransport(description, 3);
        SdpNegotiationException? exception = Assert.Throws<SdpNegotiationException>(() => service.UpdateSubsequentBundleOffer(description, 0, new List<int> { 0, 2 }, 0, new List<int>()));

        Assert.Multiple(() =>
        {
            Assert.That(service.ResolveBundleGroups(description).Select(group => group.MediaIds), Is.EqualTo(new[] { new[] { "audio", "video" }, new[] { "data", "control" } }));
            Assert.That(videoTransport!.TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(videoTransport.Connection!.Address, Is.EqualTo("203.0.113.10"));
            Assert.That(controlTransport!.TaggedMediaId, Is.EqualTo("data"));
            Assert.That(controlTransport.Connection!.Address, Is.EqualTo("203.0.113.20"));
            Assert.That(exception!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(description.Attributes.Select(attribute => attribute.RawText), Is.EqualTo(new[] { "group:BUNDLE audio video", "group:BUNDLE data control" }));
        });
    }

    [Test]
    public void InitialBundleAnswer_RejectsMalformedReferencesAndOverlappingAnswerGroupsAtomically()
    {
        SdpSessionDescription missingOffer = InitialBundleOffer();
        missingOffer.Attributes[0] = ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio missing");
        SdpSessionDescription missingAnswer = InitialBundleAnswer();
        SdpSessionDescription duplicateOffer = InitialBundleOffer();
        duplicateOffer.Attributes[0] = ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio audio");
        SdpSessionDescription duplicateAnswer = InitialBundleAnswer();
        SdpSessionDescription overlappingOffer = InitialBundleOffer();
        SdpSessionDescription overlappingAnswer = InitialBundleAnswer();
        overlappingAnswer.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio outside"));
        ISdpBundleService service = new SdpBundleService();

        SdpNegotiationException? missing = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleAnswer(missingOffer, missingAnswer, 0, new List<int> { 0 }));
        SdpNegotiationException? duplicate = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleAnswer(duplicateOffer, duplicateAnswer, 0, new List<int> { 0 }));
        SdpNegotiationException? overlapping = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleAnswer(overlappingOffer, overlappingAnswer, 0, new List<int> { 0 }));

        Assert.Multiple(() =>
        {
            Assert.That(missing!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(duplicate!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(overlapping!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(missingAnswer.Attributes, Is.Empty);
            Assert.That(duplicateAnswer.Attributes, Is.Empty);
            Assert.That(overlappingAnswer.Attributes.Single().RawText, Is.EqualTo("group:BUNDLE audio outside"));
            Assert.That(missingAnswer.MediaDescriptions.All(media => media.Attributes.Count == 0), Is.True);
            Assert.That(duplicateAnswer.MediaDescriptions.All(media => media.Attributes.Count == 0), Is.True);
            Assert.That(overlappingAnswer.MediaDescriptions.All(media => media.Attributes.Count == 0), Is.True);
        });
    }

    [Test]
    public void InitialBundleAnswer_CannotAcceptDisabledOrdinaryOfferedMedia()
    {
        SdpSessionDescription offer = InitialBundleOffer();
        offer.MediaDescriptions[1].Port = 0;
        SdpSessionDescription answer = InitialBundleAnswer();
        ISdpBundleService service = new SdpBundleService();

        SdpNegotiationException? exception = Assert.Throws<SdpNegotiationException>(() => service.CreateInitialBundleAnswer(offer, answer, 0, new List<int> { 1 }));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(answer.Attributes, Is.Empty);
            Assert.That(answer.MediaDescriptions.All(media => media.Attributes.Count == 0), Is.True);
            Assert.That(answer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 20000, 20002, 20004 }));
        });
    }

    [Test]
    public void SubsequentBundleAnswer_CreatesMissingGroupAndRejectsOmittedMembers()
    {
        SdpSessionDescription offer = SubsequentAnswerOffer();
        SdpSessionDescription answer = SubsequentAnswerDescription();
        answer.Attributes.Clear();
        ISdpBundleService service = new SdpBundleService();

        SdpBundleGroup group = service.UpdateSubsequentBundleAnswer(offer, answer, 0, new List<int> { 1, 0 });

        Assert.Multiple(() =>
        {
            Assert.That(group.TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(group.MediaIds, Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(answer.Attributes.Single(), Is.SameAs(group.SourceGroup!.SourceAttribute));
            Assert.That(answer.Attributes.Single().RawText, Is.EqualTo("group:BUNDLE audio video"));
            Assert.That(answer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 7000, 7000, 0, 0 }));
            Assert.That(answer.MediaDescriptions[2].HasPortCount, Is.False);
            Assert.That(answer.MediaDescriptions[3].HasPortCount, Is.False);
        });
    }

    [Test]
    public void SubsequentBundleOffer_RejectsInvalidSelectionsWithoutMutation()
    {
        SdpSessionDescription offer = SubsequentBundleOffer();
        SdpAttribute groupSource = offer.Attributes[0];
        ISdpBundleService service = new SdpBundleService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.UpdateSubsequentBundleOffer(offer, 1, new List<int> { 0 }, 0, new List<int>()), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("bundleGroupIndex"));
            Assert.That(() => service.UpdateSubsequentBundleOffer(offer, 0, new List<int> { 0, 0 }, 0, new List<int>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("bundledMediaIndexes"));
            Assert.That(() => service.UpdateSubsequentBundleOffer(offer, 0, new List<int> { 0 }, 1, new List<int>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("taggedMediaIndex"));
            Assert.That(() => service.UpdateSubsequentBundleOffer(offer, 0, new List<int>(), 0, new List<int>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("taggedMediaIndex"));
            Assert.That(() => service.UpdateSubsequentBundleOffer(offer, 0, new List<int> { 0, 1, 3 }, 0, new List<int> { 2, 2 }), Throws.ArgumentException.With.Property("ParamName").EqualTo("disabledMediaIndexes"));
            Assert.That(() => service.UpdateSubsequentBundleOffer(offer, 0, new List<int> { 0, 1, 3 }, 0, new List<int> { 4 }), Throws.ArgumentException.With.Property("ParamName").EqualTo("disabledMediaIndexes"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(offer.Attributes.Single(), Is.SameAs(groupSource));
            Assert.That(groupSource.RawText, Is.EqualTo("group:BUNDLE audio video data control"));
            Assert.That(offer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 5000, 5000, 5000, 5000, 0 }));
        });
    }

    [Test]
    public void SubsequentBundleAnswer_RejectsMultipleOverlappingExistingGroupsAtomically()
    {
        SdpSessionDescription offer = SubsequentAnswerOffer();
        SdpSessionDescription answer = SubsequentAnswerDescription();
        answer.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE video control"));
        List<string> originalGroups = answer.Attributes.Select(attribute => attribute.RawText).ToList();
        ISdpBundleService service = new SdpBundleService();

        SdpNegotiationException? exception = Assert.Throws<SdpNegotiationException>(() => service.UpdateSubsequentBundleAnswer(offer, answer, 0, new List<int> { 0, 1 }));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
            Assert.That(answer.Attributes.Select(attribute => attribute.RawText), Is.EqualTo(originalGroups));
            Assert.That(answer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 7000, 7000, 7000, 0 }));
        });
    }

    [Test]
    public void PublicMethods_RejectNullDescriptionsAndInvalidIndexes()
    {
        ISdpBundleService service = new SdpBundleService();
        SdpSessionDescription empty = new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ResolveBundleGroups(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.FindBundleGroupForMedia(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveBundleTaggedMediaIndex(empty, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveBundleTransport(empty, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.CreateInitialBundleOffer(null!, new List<int>(), 0, new List<int>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.CreateInitialBundleOffer(empty, null!, 0, new List<int>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mediaIndexes"));
            Assert.That(() => service.CreateInitialBundleOffer(empty, new List<int>(), 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("bundleOnlyMediaIndexes"));
            Assert.That(() => service.CreateInitialBundleAnswer(null!, empty, 0, new List<int>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("offer"));
            Assert.That(() => service.CreateInitialBundleAnswer(empty, null!, 0, new List<int>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("answer"));
            Assert.That(() => service.CreateInitialBundleAnswer(empty, empty, 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("acceptedMediaIndexes"));
            Assert.That(() => service.UpdateSubsequentBundleOffer(null!, 0, new List<int>(), -1, new List<int>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("offer"));
            Assert.That(() => service.UpdateSubsequentBundleOffer(empty, 0, null!, -1, new List<int>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("bundledMediaIndexes"));
            Assert.That(() => service.UpdateSubsequentBundleOffer(empty, 0, new List<int>(), -1, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("disabledMediaIndexes"));
            Assert.That(() => service.UpdateSubsequentBundleAnswer(null!, empty, 0, new List<int>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("offer"));
            Assert.That(() => service.UpdateSubsequentBundleAnswer(empty, null!, 0, new List<int>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("answer"));
            Assert.That(() => service.UpdateSubsequentBundleAnswer(empty, empty, 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("acceptedMediaIndexes"));
        });
    }

    static SdpSessionDescription SubsequentBundleOffer()
    {
        SdpSessionDescription offer = new SdpSessionDescription();
        offer.Connection = Connection("203.0.113.40");
        offer.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video data control"));
        string[] mediaIds = new[] { "audio", "video", "data", "control", "screen" };
        string[] mediaTypes = new[] { SdpMediaTypes.Audio, SdpMediaTypes.Video, SdpMediaTypes.Application, SdpMediaTypes.Application, SdpMediaTypes.Video };
        int index = 0;
        while (index < mediaIds.Length)
        {
            int port = index == 4 ? 0 : 5000;
            SdpMediaDescription media = OfferMedia(mediaTypes[index], port);
            media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, mediaIds[index]));
            offer.MediaDescriptions.Add(media);
            index = index + 1;
        }
        offer.MediaDescriptions[3].Connections.Add(Connection("203.0.113.50"));
        offer.MediaDescriptions[3].Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "group-owner"));
        offer.MediaDescriptions[3].Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        return offer;
    }

    static SdpSessionDescription SubsequentAnswerOffer()
    {
        SdpSessionDescription offer = new SdpSessionDescription();
        offer.Connection = Connection("192.0.2.70");
        offer.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video data control"));
        string[] mediaIds = new[] { "audio", "video", "data", "control" };
        string[] mediaTypes = new[] { SdpMediaTypes.Audio, SdpMediaTypes.Video, SdpMediaTypes.Application, SdpMediaTypes.Application };
        int index = 0;
        while (index < mediaIds.Length)
        {
            SdpMediaDescription media = OfferMedia(mediaTypes[index], 5000);
            media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, mediaIds[index]));
            offer.MediaDescriptions.Add(media);
            index = index + 1;
        }
        return offer;
    }

    static SdpSessionDescription SubsequentAnswerDescription()
    {
        SdpSessionDescription answer = new SdpSessionDescription();
        answer.Connection = Connection("203.0.113.71");
        answer.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video data"));
        string[] mediaIds = new[] { "audio", "video", "data", "control" };
        string[] mediaTypes = new[] { SdpMediaTypes.Audio, SdpMediaTypes.Video, SdpMediaTypes.Application, SdpMediaTypes.Application };
        int index = 0;
        while (index < mediaIds.Length)
        {
            int port = index == 3 ? 0 : 7000;
            SdpMediaDescription media = OfferMedia(mediaTypes[index], port);
            media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, mediaIds[index]));
            answer.MediaDescriptions.Add(media);
            index = index + 1;
        }
        answer.MediaDescriptions[0].Connections.Add(Connection("203.0.113.70"));
        answer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "answer-owner"));
        answer.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        return answer;
    }

    static SdpSessionDescription InitialBundleOffer()
    {
        SdpSessionDescription offer = new SdpSessionDescription();
        offer.Connection = Connection("192.0.2.10");
        offer.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video data"));
        SdpMediaDescription audio = OfferMedia(SdpMediaTypes.Audio, 10000);
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "audio"));
        SdpMediaDescription video = OfferMedia(SdpMediaTypes.Video, 10002);
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "video"));
        SdpMediaDescription data = OfferMedia(SdpMediaTypes.Application, 0);
        data.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "data"));
        data.Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));
        offer.MediaDescriptions.Add(audio);
        offer.MediaDescriptions.Add(video);
        offer.MediaDescriptions.Add(data);
        return offer;
    }

    static SdpSessionDescription InitialBundleAnswer()
    {
        SdpSessionDescription answer = new SdpSessionDescription();
        answer.Connection = Connection("203.0.113.10");
        answer.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Audio, 20000));
        answer.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Video, 20002));
        answer.MediaDescriptions.Add(OfferMedia(SdpMediaTypes.Application, 20004));
        return answer;
    }

    static SdpMediaDescription OfferMedia(string mediaType, int port)
    {
        return new SdpMediaDescription
        {
            MediaType = mediaType,
            Port = port,
            Protocol = SdpTransportProtocols.RtpAvp
        };
    }

    static bool HasAttribute(List<SdpAttribute> attributes, string name)
    {
        return attributes.Any(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    static SdpMediaDescription Media(string mediaId)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = 5000,
            Protocol = SdpTransportProtocols.RtpAvp
        };
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, mediaId));
        return media;
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
            RawText = SdpNetworkTypes.Internet + " " + SdpAddressTypes.Ip4 + " " + address,
            Location = new SdpSourceLocation { LineNumber = 4, ColumnNumber = 1 }
        };
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

    static SdpBandwidth Bandwidth(string type, long value)
    {
        return new SdpBandwidth
        {
            Type = type,
            Value = value,
            RawValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            RawText = type + ":" + value.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
