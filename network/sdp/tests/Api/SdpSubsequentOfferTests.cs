using System.Text.Json;

namespace sdp.Tests.Api;

public class SdpSubsequentOfferTests
{
    [Test]
    public void CreateSubsequentOffer_PreservesSlotsMappingsAndBundleStateWhileAppendingNewMedia()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities initialCapabilities = new SdpEndpointCapabilities { SupportsBundle = true };
        SdpMediaCapability audio = RtpCapability("audio", "audio", 5000, 111, "opus", 48000, 3);
        audio.Codecs[0].HasChannelCount = true;
        audio.Codecs[0].ChannelCount = 2;
        SdpMediaCapability video = RtpCapability("video", "video", 5002, 96, "VP8", 90000, 7);
        initialCapabilities.Media.Add(audio);
        initialCapabilities.Media.Add(video);
        SdpSessionDescription previous = service.CreateInitialOffer(SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = initialCapabilities }).Description!;
        string previousSnapshot = Snapshot(previous);

        SdpEndpointCapabilities currentCapabilities = new SdpEndpointCapabilities { SupportsBundle = true };
        SdpMediaCapability continuedVideo = RtpCapability("video", "video", 6002, 120, "VP8", 90000, 2);
        SdpMediaCapability addedAudio = RtpCapability("audio", "data", 6004, 110, "PCMA", 8000, 5);
        currentCapabilities.Media.Add(continuedVideo);
        currentCapabilities.Media.Add(addedAudio);
        SdpNegotiationOptions options = new SdpNegotiationOptions { LocalCapabilities = currentCapabilities };

        SdpNegotiationResult result = service.CreateSubsequentOffer(previous, options);
        SdpSessionDescription offer = result.Description!;
        SdpNegotiationResult repeated = service.CreateSubsequentOffer(offer, options);
        SdpValidationResult validation = new SdpValidator().ValidateDescription(offer, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Bundle,
            NegotiationRole = SdpNegotiationRole.Offerer,
            MaximumDiagnosticCount = 128
        });

        Assert.Multiple(() =>
        {
            Assert.That(offer.MediaDescriptions, Has.Count.EqualTo(3));
            Assert.That(MediaId(offer.MediaDescriptions[0]), Is.EqualTo("audio"));
            Assert.That(offer.MediaDescriptions[0].Port, Is.Zero);
            Assert.That(offer.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "111" }));
            Assert.That(MediaId(offer.MediaDescriptions[1]), Is.EqualTo("video"));
            Assert.That(offer.MediaDescriptions[1].Port, Is.EqualTo(6002));
            Assert.That(offer.MediaDescriptions[1].Formats, Is.EqualTo(new[] { "96" }));
            Assert.That(AttributeValue(offer.MediaDescriptions[1], SdpAttributeNames.ExtMap), Does.StartWith("7 "));
            Assert.That(MediaId(offer.MediaDescriptions[2]), Is.EqualTo("data"));
            Assert.That(GroupValue(offer), Is.EqualTo("BUNDLE video data"));
            Assert.That(offer.Origin!.SessionVersion, Is.EqualTo("2"));
            Assert.That(result.Media.Select(media => media.Accepted), Is.EqualTo(new[] { false, true, true }));
            Assert.That(result.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.RejectedByPolicy));
            Assert.That(result.Media[0].Rejection!.ReasonToken, Is.EqualTo("rejected-by-policy"));
            Assert.That(result.Rejections.Single(), Is.SameAs(result.Media[0].Rejection));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationRejectedByPolicy));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(repeated.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.OfferedAsRejected));
            Assert.That(repeated.Diagnostics.Single().Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationMediaAlreadyRejected));
            Assert.That(result.Media[1].PayloadMappings[0].NegotiatedPayloadType, Is.EqualTo(96));
            Assert.That(result.Media[1].EffectiveMedia!.Codecs.Single().PayloadType, Is.EqualTo(96));
            Assert.That(result.Media[1].EffectiveMedia!.HeaderExtensions.Single().Id, Is.EqualTo(7));
            Assert.That(result.BundleGroups.Single().TaggedMediaId, Is.EqualTo("video"));
            Assert.That(result.BundleTransports.Single().TaggedMediaId, Is.EqualTo("video"));
            Assert.That(result.Media[2].EffectiveMedia!.BundleTransport!.TaggedMediaId, Is.EqualTo("video"));
            Assert.That(validation.Issues.Where(issue => issue.Severity == SdpValidationSeverity.Error || issue.Severity == SdpValidationSeverity.Fatal), Is.Empty, string.Join("|", validation.Issues.Select(issue => issue.Code + ":" + issue.Detail)));
            Assert.That(Snapshot(previous), Is.EqualTo(previousSnapshot));
            Assert.That(continuedVideo.Codecs[0].PreferredPayloadType, Is.EqualTo(120));
            Assert.That(continuedVideo.HeaderExtensions[0].PreferredId, Is.EqualTo(2));
        });
    }

    [Test]
    public void CreateSubsequentOffer_UsesCredentialChangesAsIceRestartIntentAndPreservesDtlsAssociation()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpFingerprintAttribute oldFingerprint = Fingerprint(0x11);
        SdpEndpointCapabilities initialCapabilities = SecureCapabilities("oldUfrag", "oldPasswordValue1234567890", oldFingerprint, "stable-tls-identifier-1");
        SdpSessionDescription previous = service.CreateInitialOffer(SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = initialCapabilities }).Description!;
        SdpFingerprintAttribute newFingerprint = Fingerprint(0x22);
        SdpEndpointCapabilities currentCapabilities = SecureCapabilities("newUfrag", "newPasswordValue1234567890", newFingerprint, "replacement-tls-identifier-1");

        SdpNegotiationResult retained = service.CreateSubsequentOffer(previous, new SdpNegotiationOptions
        {
            LocalCapabilities = currentCapabilities,
            AllowIceRestart = false
        });
        SdpNegotiationResult restarted = service.CreateSubsequentOffer(previous, new SdpNegotiationOptions
        {
            LocalCapabilities = currentCapabilities,
            AllowIceRestart = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(retained.Description!.MediaDescriptions[0], SdpAttributeNames.IceUsernameFragment), Is.EqualTo("oldUfrag"));
            Assert.That(AttributeValue(retained.Description.MediaDescriptions[0], SdpAttributeNames.IcePassword), Is.EqualTo("oldPasswordValue1234567890"));
            Assert.That(AttributeValue(retained.Description.MediaDescriptions[0], SdpAttributeNames.TlsId), Is.EqualTo("stable-tls-identifier-1"));
            Assert.That(AttributeValue(retained.Description.MediaDescriptions[0], SdpAttributeNames.Fingerprint), Is.EqualTo(AttributeValue(previous.MediaDescriptions[0], SdpAttributeNames.Fingerprint)));
            Assert.That(retained.Description.Origin!.SessionVersion, Is.EqualTo("1"));
            Assert.That(AttributeValue(restarted.Description!.MediaDescriptions[0], SdpAttributeNames.IceUsernameFragment), Is.EqualTo("newUfrag"));
            Assert.That(AttributeValue(restarted.Description.MediaDescriptions[0], SdpAttributeNames.IcePassword), Is.EqualTo("newPasswordValue1234567890"));
            Assert.That(AttributeValue(restarted.Description.MediaDescriptions[0], SdpAttributeNames.TlsId), Is.EqualTo("stable-tls-identifier-1"));
            Assert.That(restarted.Description.Origin!.SessionVersion, Is.EqualTo("2"));
            Assert.That(newFingerprint.SourceAttribute, Is.Null);
        });
    }

    [Test]
    public void CreateSubsequentOffer_DoesNotReuseRetiredPayloadOrExtensionAssignments()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities initialCapabilities = new SdpEndpointCapabilities();
        initialCapabilities.Media.Add(RtpCapability("video", "video", 5002, 96, "VP8", 90000, 7));
        SdpSessionDescription previous = service.CreateInitialOffer(SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = initialCapabilities }).Description!;
        SdpEndpointCapabilities currentCapabilities = new SdpEndpointCapabilities();
        SdpMediaCapability replacement = RtpCapability("video", "video", 5002, 96, "H264", 90000, 7);
        replacement.HeaderExtensions[0].Uri = "urn:example:replacement-extension";
        currentCapabilities.Media.Add(replacement);

        SdpSessionDescription current = service.CreateSubsequentOffer(previous, new SdpNegotiationOptions { LocalCapabilities = currentCapabilities }).Description!;

        Assert.Multiple(() =>
        {
            Assert.That(current.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "97" }));
            Assert.That(AttributeValue(current.MediaDescriptions[0], SdpAttributeNames.RtpMap), Does.StartWith("97 H264/90000"));
            Assert.That(AttributeValue(current.MediaDescriptions[0], SdpAttributeNames.ExtMap), Does.StartWith("1 "));
            Assert.That(previous.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "96" }));
            Assert.That(AttributeValue(previous.MediaDescriptions[0], SdpAttributeNames.ExtMap), Does.StartWith("7 "));
        });
    }

    [Test]
    public void CreateSubsequentOffer_RejectsPartialIceRestartWithoutMutatingInputs()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities initialCapabilities = SecureCapabilities("oldUfrag", "oldPasswordValue1234567890", Fingerprint(0x11), "stable-tls-identifier-1");
        SdpSessionDescription previous = service.CreateInitialOffer(SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = initialCapabilities }).Description!;
        SdpEndpointCapabilities invalidCapabilities = SecureCapabilities("newUfrag", "oldPasswordValue1234567890", Fingerprint(0x22), "replacement-tls-identifier-1");
        SdpNegotiationOptions options = new SdpNegotiationOptions { LocalCapabilities = invalidCapabilities, AllowIceRestart = true };
        string previousSnapshot = Snapshot(previous);
        string optionsSnapshot = Snapshot(options);

        SdpNegotiationException exception = Assert.Throws<SdpNegotiationException>(() => service.CreateSubsequentOffer(previous, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationIceIncompatible));
            Assert.That(Snapshot(previous), Is.EqualTo(previousSnapshot));
            Assert.That(Snapshot(options), Is.EqualTo(optionsSnapshot));
        });
    }

    [Test]
    public void CreateSubsequentOffer_ValidatesArgumentsRoleAndChangedOriginVersion()
    {
        ISdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(RtpCapability("audio", "audio", 5000, 111, "opus", 48000, 3));
        SdpSessionDescription previous = service.CreateInitialOffer(SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = capabilities }).Description!;
        previous.Origin!.SessionVersion = ulong.MaxValue.ToString();
        capabilities.Media[0].Port = 6000;
        SdpNegotiationOptions answerer = new SdpNegotiationOptions { LocalCapabilities = capabilities, Role = SdpNegotiationRole.Answerer };

        Assert.Multiple(() =>
        {
            Assert.That(() => service.CreateSubsequentOffer(null!, new SdpNegotiationOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("previousOffer"));
            Assert.That(() => service.CreateSubsequentOffer(previous, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => service.CreateSubsequentOffer(previous, answerer), Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => service.CreateSubsequentOffer(previous, new SdpNegotiationOptions { LocalCapabilities = capabilities }), Throws.InvalidOperationException);
            Assert.That(previous.Origin.SessionVersion, Is.EqualTo(ulong.MaxValue.ToString()));
        });
    }

    static SdpEndpointCapabilities SecureCapabilities(string username, string password, SdpFingerprintAttribute fingerprint, string tlsId)
    {
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities
        {
            SupportsIce = true,
            SupportsDtls = true
        };
        SdpMediaCapability media = RtpCapability("audio", "audio", 5000, 111, "opus", 48000, 3);
        media.Codecs[0].HasChannelCount = true;
        media.Codecs[0].ChannelCount = 2;
        media.Protocols[0] = SdpTransportProtocols.UdpTlsRtpSavpf;
        media.Ice = new SdpIceDescription
        {
            Credentials = new SdpIceCredentials { UsernameFragment = username, Password = password }
        };
        media.Dtls = new SdpDtlsDescription
        {
            SetupRole = SdpDtlsSetupRole.ActPass,
            SetupRoleToken = "actpass",
            TlsId = tlsId
        };
        media.Dtls.Fingerprints.Add(fingerprint);
        capabilities.Media.Add(media);
        return capabilities;
    }

    static SdpMediaCapability RtpCapability(string mediaType, string mediaId, int port, int payloadType, string encodingName, int clockRate, int extensionId)
    {
        SdpMediaCapability media = new SdpMediaCapability
        {
            MediaKind = mediaType == "audio" ? SdpMediaKind.Audio : SdpMediaKind.Video,
            MediaType = mediaType,
            MediaId = mediaId,
            Port = port,
            SupportsRtcpMux = true
        };
        media.Protocols.Add(SdpTransportProtocols.RtpAvpf);
        media.Codecs.Add(new SdpCodecCapability
        {
            MediaKind = media.MediaKind,
            MediaType = mediaType,
            EncodingName = encodingName,
            ClockRate = clockRate,
            HasPreferredPayloadType = true,
            PreferredPayloadType = payloadType
        });
        media.HeaderExtensions.Add(new SdpHeaderExtension
        {
            Uri = SdpHeaderExtensionUris.MediaId,
            HasPreferredId = true,
            PreferredId = extensionId
        });
        return media;
    }

    static SdpFingerprintAttribute Fingerprint(byte value)
    {
        SdpFingerprintAttribute fingerprint = new SdpFingerprintAttribute { Algorithm = SdpFingerprintAlgorithms.Sha256 };
        int index = 0;
        while (index < 32)
        {
            fingerprint.Digest.Add(value);
            index = index + 1;
        }
        return fingerprint;
    }

    static SdpSessionDescription SessionTemplate()
    {
        return new SdpSessionFactory().CreateSession(
            new SdpOrigin
            {
                Username = "-",
                SessionId = "1234",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            "Subsequent offer",
            new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            new SdpTiming { StartTime = "0", StopTime = "0" });
    }

    static string MediaId(SdpMediaDescription media)
    {
        return AttributeValue(media, SdpAttributeNames.MediaId) ?? string.Empty;
    }

    static string GroupValue(SdpSessionDescription description)
    {
        return description.Attributes.Single(attribute => string.Equals(attribute.Name, SdpAttributeNames.Group, StringComparison.OrdinalIgnoreCase)).Value;
    }

    static string? AttributeValue(SdpMediaDescription media, string name)
    {
        return media.Attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static string Snapshot(object value)
    {
        return JsonSerializer.Serialize(value, value.GetType(), new JsonSerializerOptions { IncludeFields = true });
    }
}
