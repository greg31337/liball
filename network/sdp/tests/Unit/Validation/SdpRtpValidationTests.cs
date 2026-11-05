namespace sdp.Tests.Unit.Validation;

public class SdpRtpValidationTests
{
    [Test]
    public void ValidRtpDescription_AcceptsStaticDynamicAndWildcardFeedback()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVPF", "0", "96", "97");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("fmtp", "96 minptime=10;useinbandfec=1"));
        media.Attributes.Add(ValueAttribute("rtpmap", "97 telephone-event/8000"));
        media.Attributes.Add(ValueAttribute("fmtp", "97 0-16"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "* nack"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "96 transport-cc"));
        media.Attributes.Add(ValueAttribute("ptime", "20"));
        media.Attributes.Add(ValueAttribute("maxptime", "60"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ErrorCount, Is.Zero);
        });
    }

    [Test]
    public void MediaFormats_RequireCanonicalUniquePayloadTypes()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "96", "96", "012", "128", "opus");
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(4));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationPayloadType), Is.True);
            Assert.That(result.Issues.All(x => x.FieldName == 'm' && x.Path.FieldName == 'm'), Is.True);
            Assert.That(result.Issues.Select(x => x.Path.ValueIndex), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void NonStaticPayloads_RequireValidRtpMaps()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "0", "35", "96", "97");
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtpmap", "97 telephone-event/8000"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap), Is.True);
            Assert.That(result.Issues.All(x => x.AttributeName == "rtpmap" && x.MediaIndex == 0), Is.True);
        });
    }

    [Test]
    public void RejectedMedia_SkipsPayloadValidationUnlessItIsBundleOnly()
    {
        SdpSessionDescription rejected = DescriptionWithMedia("video", "RTP/AVP", "100");
        rejected.MediaDescriptions[0].Port = 0;
        SdpSessionDescription bundleOnly = DescriptionWithMedia("video", "RTP/AVP", "100");
        bundleOnly.MediaDescriptions[0].Port = 0;
        bundleOnly.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));

        SdpValidationResult rejectedResult = ValidateRtp(rejected);
        SdpValidationResult bundleOnlyResult = ValidateRtp(bundleOnly);

        Assert.Multiple(() =>
        {
            Assert.That(rejectedResult.Success, Is.True);
            Assert.That(rejectedResult.Issues, Is.Empty);
            Assert.That(bundleOnlyResult.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap), Is.True);
        });
    }

    [Test]
    public void DuplicateAndConflictingMaps_AreDistinguishedAndFmtpIsSingletonPerPayload()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "96");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("rtpmap", "96 OPUS/48000/2"));
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/44100/2"));
        media.Attributes.Add(ValueAttribute("rtpmap", "97 telephone-event/8000"));
        media.Attributes.Add(ValueAttribute("fmtp", "96 useinbandfec=1"));
        media.Attributes.Add(ValueAttribute("fmtp", "96 minptime=10"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(4));
            Assert.That(result.Issues.Select(x => x.Code), Is.EqualTo(new[]
            {
                SdpDiagnosticCodes.ValidationAttributeCardinality,
                SdpDiagnosticCodes.ValidationPayloadType,
                SdpDiagnosticCodes.ValidationDanglingPayloadReference,
                SdpDiagnosticCodes.ValidationAttributeCardinality
            }));
            Assert.That(result.Issues[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Issues[1].Path.OccurrenceIndex, Is.EqualTo(2));
            Assert.That(result.Issues[2].AttributeName, Is.EqualTo("rtpmap"));
            Assert.That(result.Issues[3].AttributeName, Is.EqualTo("fmtp"));
        });
    }

    [Test]
    public void DanglingFmtpAndTargetedFeedback_AreRejectedWhileWildcardAppliesToAllFormats()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVPF", "96");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("fmtp", "97 minptime=10"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "* nack"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "97 nack pli"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationDanglingPayloadReference), Is.True);
            Assert.That(result.Issues.Select(x => x.AttributeName), Is.EqualTo(new[] { "fmtp", "rtcp-fb" }));
            Assert.That(result.Issues.Any(x => x.Detail.Contains("wildcard", StringComparison.OrdinalIgnoreCase)), Is.False);
        });
    }

    [Test]
    public void ClockRatesAndAudioChannelCounts_MustBePositiveAndNumeric()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "96", "97", "98");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/0"));
        media.Attributes.Add(ValueAttribute("rtpmap", "97 opus/48000/0"));
        media.Attributes.Add(ValueAttribute("rtpmap", "98 opus/48000/stereo"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(5));
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttribute), Is.EqualTo(3));
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap), Is.EqualTo(2));
            Assert.That(result.Issues.Any(x => x.Detail.Contains("clock rate", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(result.Issues.Count(x => x.Detail.Contains("channel", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(2));
        });
    }

    [Test]
    public void VideoRtpMaps_RejectAudioChannelParameters()
    {
        SdpSessionDescription description = DescriptionWithMedia("video", "RTP/AVP", "96");
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtpmap", "96 VP8/90000/2"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(result.Issues[0].Detail, Does.Contain("video"));
            Assert.That(result.Issues[0].Detail, Does.Contain("channel"));
        });
    }

    [Test]
    public void StaticPayloadMappings_MustMatchTheMediaKindUnlessExplicitlyRebound()
    {
        SdpSessionDescription audio = DescriptionWithMedia("audio", "RTP/AVP", "25", "33");
        SdpSessionDescription video = DescriptionWithMedia("video", "RTP/AVP", "0", "33");
        SdpSessionDescription rebound = DescriptionWithMedia("video", "RTP/AVP", "0");
        rebound.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtpmap", "0 X-VIDEO/90000"));

        SdpValidationResult audioResult = ValidateRtp(audio);
        SdpValidationResult videoResult = ValidateRtp(video);
        SdpValidationResult reboundResult = ValidateRtp(rebound);

        Assert.Multiple(() =>
        {
            Assert.That(audioResult.Issues.Select(x => x.Code), Is.EqualTo(new[] { SdpDiagnosticCodes.ValidationPayloadType }));
            Assert.That(videoResult.Issues.Select(x => x.Code), Is.EqualTo(new[] { SdpDiagnosticCodes.ValidationPayloadType }));
            Assert.That(reboundResult.Success, Is.True);
            Assert.That(reboundResult.Issues, Is.Empty);
        });
    }

    [Test]
    public void AttributeScopeAndTransportApplicability_AreEnforced()
    {
        SdpSessionDescription sessionScoped = DescriptionWithMedia("audio", "RTP/AVPF", "0");
        sessionScoped.Attributes.Add(ValueAttribute("rtpmap", "0 PCMU/8000"));
        sessionScoped.Attributes.Add(ValueAttribute("fmtp", "0 mode=sample"));
        sessionScoped.Attributes.Add(ValueAttribute("rtcp-fb", "* nack"));
        sessionScoped.Attributes.Add(ValueAttribute("ptime", "20"));
        sessionScoped.Attributes.Add(ValueAttribute("maxptime", "60"));
        SdpSessionDescription nonRtp = DescriptionWithMedia("application", "UDP/DTLS/SCTP", "webrtc-datachannel");
        nonRtp.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        nonRtp.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtcp-fb", "* nack"));
        SdpSessionDescription avp = DescriptionWithMedia("audio", "RTP/AVP", "0");
        avp.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtcp-fb", "* nack"));

        SdpValidationResult sessionResult = ValidateRtp(sessionScoped);
        SdpValidationResult nonRtpResult = ValidateRtp(nonRtp);
        SdpValidationResult avpResult = ValidateRtp(avp);

        Assert.Multiple(() =>
        {
            Assert.That(sessionResult.ErrorCount, Is.EqualTo(5));
            Assert.That(sessionResult.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttributeScope && x.Scope == SdpScope.Session), Is.True);
            Assert.That(nonRtpResult.ErrorCount, Is.EqualTo(2));
            Assert.That(nonRtpResult.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttributeScope && x.Scope == SdpScope.Media), Is.True);
            Assert.That(avpResult.Issues.Select(x => x.Code), Is.EqualTo(new[] { SdpDiagnosticCodes.ValidationAttributeScope }));
        });
    }

    [Test]
    public void OpusParameters_AcceptRegisteredBoundsPreferencesAndUnknownExtensions()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "96");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("fmtp", "96 maxplaybackrate=8000;sprop-maxcapturerate=48000;maxaveragebitrate=1;stereo=1;sprop-stereo=0;cbr=1;useinbandfec=1;usedtx=0;minptime=8;x-extension=value"));
        media.Attributes.Add(ValueAttribute("ptime", "3"));
        media.Attributes.Add(ValueAttribute("maxptime", "120"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void OpusRtpMap_RequiresAudioFortyEightKhzAndTwoChannels()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "96", "97");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/44100/2"));
        media.Attributes.Add(ValueAttribute("rtpmap", "97 opus/48000"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttribute), Is.True);
            Assert.That(result.Issues.All(x => x.Detail.Contains("Opus", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void OpusFmtp_RejectsInvalidKnownValuesAndDuplicates()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "96");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("fmtp", "96 stereo=2;useinbandfec=yes;usedtx;cbr=01;maxplaybackrate=7999;sprop-maxcapturerate=48001;maxaveragebitrate=0;minptime=7;stereo=1"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(9));
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttribute), Is.EqualTo(8));
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(1));
            Assert.That(result.Issues.All(x => x.AttributeName == "fmtp"), Is.True);
        });
    }

    [Test]
    public void OpusFmtp_RejectsOpaqueKnownCodecParameterSyntax()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "96");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("fmtp", "96 stereo=1;;usedtx=0"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(result.Issues[0].Detail, Does.Contain("Opus"));
        });
    }

    [Test]
    public void OpusPacketTimes_RequireSupportedDurationsAndConsistentMaximum()
    {
        SdpSessionDescription unsupported = DescriptionWithMedia("audio", "RTP/AVP", "96");
        unsupported.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        unsupported.MediaDescriptions[0].Attributes.Add(ValueAttribute("ptime", "2.5"));
        unsupported.MediaDescriptions[0].Attributes.Add(ValueAttribute("maxptime", "7"));
        SdpSessionDescription inconsistent = DescriptionWithMedia("audio", "RTP/AVP", "96");
        inconsistent.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        inconsistent.MediaDescriptions[0].Attributes.Add(ValueAttribute("ptime", "60"));
        inconsistent.MediaDescriptions[0].Attributes.Add(ValueAttribute("maxptime", "40"));

        SdpValidationResult unsupportedResult = ValidateRtp(unsupported);
        SdpValidationResult inconsistentResult = ValidateRtp(inconsistent);

        Assert.Multiple(() =>
        {
            Assert.That(unsupportedResult.ErrorCount, Is.EqualTo(2));
            Assert.That(unsupportedResult.Issues.Select(x => x.AttributeName), Is.EqualTo(new[] { "ptime", "maxptime" }));
            Assert.That(inconsistentResult.ErrorCount, Is.EqualTo(1));
            Assert.That(inconsistentResult.Issues[0].AttributeName, Is.EqualTo("maxptime"));
            Assert.That(inconsistentResult.Issues[0].Detail, Does.Contain("must not be less"));
        });
    }

    [Test]
    public void H264Parameters_AcceptRegisteredValuesAndUnknownExtensions()
    {
        SdpSessionDescription description = DescriptionWithMedia("video", "RTP/AVP", "96");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 H264/90000"));
        media.Attributes.Add(ValueAttribute("fmtp", "96 profile-level-id=42e01f;packetization-mode=1;level-asymmetry-allowed=1;x-extension=value"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void H264RtpMap_RequiresVideoNinetyKhzAndNoEncodingParameters()
    {
        SdpSessionDescription description = DescriptionWithMedia("video", "RTP/AVP", "96", "97");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 H264/8000"));
        media.Attributes.Add(ValueAttribute("rtpmap", "97 H264/90000/2"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttribute), Is.True);
            Assert.That(result.Issues.Any(x => x.Detail.Contains("H.264", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(x => x.Detail.Contains("channel", StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    public void H264Fmtp_RejectsInvalidKnownValuesAndDuplicates()
    {
        SdpSessionDescription description = DescriptionWithMedia("video", "RTP/AVP", "96");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 H264/90000"));
        media.Attributes.Add(ValueAttribute("fmtp", "96 profile-level-id=42e00g;packetization-mode=3;level-asymmetry-allowed=2;profile-level-id=42e01f"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(4));
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttribute), Is.EqualTo(3));
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(1));
            Assert.That(result.Issues.All(x => x.AttributeName == "fmtp"), Is.True);
        });
    }

    [Test]
    public void H264Fmtp_RejectsOpaqueKnownCodecParameterSyntax()
    {
        SdpSessionDescription description = DescriptionWithMedia("video", "RTP/AVP", "96");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 H264/90000"));
        media.Attributes.Add(ValueAttribute("fmtp", "96 packetization-mode=1;;profile-level-id=42e01f"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(result.Issues[0].Detail, Does.Contain("H.264"));
        });
    }

    [Test]
    public void Vp8AndVp9Parameters_ValidateMappingsLimitsAndProfiles()
    {
        SdpSessionDescription valid = DescriptionWithMedia("video", "RTP/AVP", "96", "97");
        SdpMediaDescription validMedia = valid.MediaDescriptions[0];
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "96 VP8/90000"));
        validMedia.Attributes.Add(ValueAttribute("fmtp", "96 max-fr=30;max-fs=3600"));
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "97 VP9/90000"));
        validMedia.Attributes.Add(ValueAttribute("fmtp", "97 max-fr=60;max-fs=7200;profile-id=3;x-extension=value"));
        SdpSessionDescription invalid = DescriptionWithMedia("video", "RTP/AVP", "98", "99");
        SdpMediaDescription invalidMedia = invalid.MediaDescriptions[0];
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "98 VP8/8000"));
        invalidMedia.Attributes.Add(ValueAttribute("fmtp", "98 max-fr=0"));
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "99 VP9/90000"));
        invalidMedia.Attributes.Add(ValueAttribute("fmtp", "99 max-fr=30;max-fs=3600;profile-id=4"));

        SdpValidationResult validResult = ValidateRtp(valid);
        SdpValidationResult invalidResult = ValidateRtp(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
            Assert.That(invalidResult.ErrorCount, Is.EqualTo(4));
            Assert.That(invalidResult.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttribute), Is.True);
        });
    }

    [Test]
    public void HevcParameters_AcceptRegisteredBoundsAndRejectInvalidValues()
    {
        SdpSessionDescription valid = DescriptionWithMedia("video", "RTP/AVP", "96");
        SdpMediaDescription validMedia = valid.MediaDescriptions[0];
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "96 H265/90000"));
        validMedia.Attributes.Add(ValueAttribute("fmtp", "96 profile-space=3;tier-flag=1;profile-id=31;profile-compatibility-indicator=80000000;interop-constraints=B00000000000;level-id=120;sprop-sub-layer-id=6;recv-sub-layer-id=6;max-recv-level-id=150;tx-mode=MRMT;max-dpb=16;sprop-max-don-diff=32767;include-dph=1;x-extension=value"));
        SdpSessionDescription invalid = DescriptionWithMedia("video", "RTP/AVP", "97");
        SdpMediaDescription invalidMedia = invalid.MediaDescriptions[0];
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "97 HEVC/90000"));
        invalidMedia.Attributes.Add(ValueAttribute("fmtp", "97 profile-space=4;tier-flag=2;profile-id=32;profile-compatibility-indicator=xyz;interop-constraints=0000;level-id=256;sprop-sub-layer-id=7;tx-mode=mst;max-dpb=0;include-dph=2"));

        SdpValidationResult validResult = ValidateRtp(valid);
        SdpValidationResult invalidResult = ValidateRtp(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
            Assert.That(invalidResult.ErrorCount, Is.EqualTo(10));
            Assert.That(invalidResult.Issues.All(x => x.AttributeName == "fmtp"), Is.True);
        });
    }

    [Test]
    public void RtxParameters_RequireAValidSameRateAssociation()
    {
        SdpSessionDescription valid = DescriptionWithMedia("video", "RTP/AVP", "96", "97");
        SdpMediaDescription validMedia = valid.MediaDescriptions[0];
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "96 VP8/90000"));
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "97 rtx/90000"));
        validMedia.Attributes.Add(ValueAttribute("fmtp", "97 apt=96;rtx-time=3000"));
        SdpSessionDescription invalid = DescriptionWithMedia("video", "RTP/AVP", "96", "97", "98", "99");
        SdpMediaDescription invalidMedia = invalid.MediaDescriptions[0];
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "96 VP8/90000"));
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "97 rtx/8000"));
        invalidMedia.Attributes.Add(ValueAttribute("fmtp", "97 apt=96"));
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "98 rtx/90000"));
        invalidMedia.Attributes.Add(ValueAttribute("fmtp", "98 apt=99"));
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "99 rtx/90000"));

        SdpValidationResult validResult = ValidateRtp(valid);
        SdpValidationResult invalidResult = ValidateRtp(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
            Assert.That(invalidResult.ErrorCount, Is.EqualTo(3));
            Assert.That(invalidResult.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationDanglingPayloadReference), Is.EqualTo(2));
            Assert.That(invalidResult.Issues.Count(x => x.Detail.Contains("requires an fmtp", StringComparison.Ordinal)), Is.EqualTo(1));
        });
    }

    [Test]
    public void RedUlpFecAndFlexFec_ValidatePayloadAssociationsAndRepairParameters()
    {
        SdpSessionDescription valid = DescriptionWithMedia("video", "RTP/AVP", "96", "97", "98", "99");
        SdpMediaDescription validMedia = valid.MediaDescriptions[0];
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "96 VP8/90000"));
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "97 ulpfec/90000"));
        validMedia.Attributes.Add(ValueAttribute("fmtp", "97 onelevelonly=1"));
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "98 red/90000"));
        validMedia.Attributes.Add(ValueAttribute("fmtp", "98 96/97"));
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "99 flexfec/90000"));
        validMedia.Attributes.Add(ValueAttribute("fmtp", "99 repair-window=200000"));
        SdpSessionDescription invalid = DescriptionWithMedia("video", "RTP/AVP", "96", "97", "98", "99");
        SdpMediaDescription invalidMedia = invalid.MediaDescriptions[0];
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "96 VP8/90000"));
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "97 ulpfec/1000"));
        invalidMedia.Attributes.Add(ValueAttribute("fmtp", "97 onelevelonly=2"));
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "98 red/90000"));
        invalidMedia.Attributes.Add(ValueAttribute("fmtp", "98 96/100"));
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "99 flexfec/90000"));
        invalidMedia.Attributes.Add(ValueAttribute("fmtp", "99 repair-window=0"));

        SdpValidationResult validResult = ValidateRtp(valid);
        SdpValidationResult invalidResult = ValidateRtp(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
            Assert.That(invalidResult.ErrorCount, Is.EqualTo(4));
            Assert.That(invalidResult.Issues.Any(x => x.Code == SdpDiagnosticCodes.ValidationDanglingPayloadReference), Is.True);
        });
    }

    [Test]
    public void ComfortNoiseAndTelephoneEvents_RequireMatchingAudioAndValidEventRanges()
    {
        SdpSessionDescription valid = DescriptionWithMedia("audio", "RTP/AVP", "0", "96", "97");
        SdpMediaDescription validMedia = valid.MediaDescriptions[0];
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "96 CN/8000"));
        validMedia.Attributes.Add(ValueAttribute("rtpmap", "97 telephone-event/8000"));
        validMedia.Attributes.Add(ValueAttribute("fmtp", "97 0-15,66,70"));
        SdpSessionDescription invalid = DescriptionWithMedia("audio", "RTP/AVP", "0", "96", "97");
        SdpMediaDescription invalidMedia = invalid.MediaDescriptions[0];
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "96 CN/16000"));
        invalidMedia.Attributes.Add(ValueAttribute("rtpmap", "97 telephone-event/16000"));
        invalidMedia.Attributes.Add(ValueAttribute("fmtp", "97 15-0,256"));

        SdpValidationResult validResult = ValidateRtp(valid);
        SdpValidationResult invalidResult = ValidateRtp(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
            Assert.That(invalidResult.ErrorCount, Is.EqualTo(2));
            Assert.That(invalidResult.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationDanglingPayloadReference), Is.EqualTo(1));
            Assert.That(invalidResult.Issues.Any(x => x.Detail.Contains("range list", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void RtcpTransportAttributes_AcceptValidMuxOnlyOfferAndEnforceMediaScope()
    {
        SdpSessionDescription valid = DescriptionWithMedia("audio", "RTP/AVPF", "0");
        valid.Connection = new SdpConnectionData { NetworkType = "IN", AddressType = "IP4", Address = "192.0.2.1" };
        SdpMediaDescription validMedia = valid.MediaDescriptions[0];
        validMedia.Attributes.Add(ValueAttribute("rtcp", "5000 IN IP4 192.0.2.1"));
        validMedia.Attributes.Add(PropertyAttribute("rtcp-mux"));
        validMedia.Attributes.Add(PropertyAttribute("rtcp-mux-only"));
        validMedia.Attributes.Add(PropertyAttribute("rtcp-rsize"));
        SdpValidationOptions offerOptions = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Rtp,
            NegotiationRole = SdpNegotiationRole.Offerer
        };
        SdpSessionDescription wrongScope = DescriptionWithMedia("audio", "RTP/AVP", "0");
        wrongScope.Attributes.Add(ValueAttribute("rtcp", "5001"));
        wrongScope.Attributes.Add(PropertyAttribute("rtcp-mux"));
        wrongScope.Attributes.Add(PropertyAttribute("rtcp-mux-only"));
        wrongScope.Attributes.Add(PropertyAttribute("rtcp-rsize"));
        SdpSessionDescription nonRtp = DescriptionWithMedia("application", "UDP/DTLS/SCTP", "webrtc-datachannel");
        nonRtp.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtcp", "5001"));
        nonRtp.MediaDescriptions[0].Attributes.Add(PropertyAttribute("rtcp-mux"));
        nonRtp.MediaDescriptions[0].Attributes.Add(PropertyAttribute("rtcp-mux-only"));
        nonRtp.MediaDescriptions[0].Attributes.Add(PropertyAttribute("rtcp-rsize"));

        SdpValidationResult validResult = new SdpValidator().ValidateDescription(valid, offerOptions);
        SdpValidationResult scopeResult = ValidateRtp(wrongScope);
        SdpValidationResult applicabilityResult = ValidateRtp(nonRtp);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
            Assert.That(scopeResult.ErrorCount, Is.EqualTo(4));
            Assert.That(scopeResult.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttributeScope && x.Scope == SdpScope.Session), Is.True);
            Assert.That(applicabilityResult.ErrorCount, Is.EqualTo(4));
            Assert.That(applicabilityResult.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttributeScope && x.Scope == SdpScope.Media), Is.True);
        });
    }

    [Test]
    public void RtcpTransportAttributes_EnforceSyntaxCardinalityAndMuxOnlyRules()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "0");
        description.Connection = new SdpConnectionData { NetworkType = "IN", AddressType = "IP4", Address = "192.0.2.1" };
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtcp", "5001 IN IP4 192.0.2.2"));
        media.Attributes.Add(ValueAttribute("rtcp", "5002"));
        media.Attributes.Add(ValueAttribute("rtcp-mux", string.Empty));
        media.Attributes.Add(PropertyAttribute("rtcp-mux"));
        media.Attributes.Add(PropertyAttribute("rtcp-mux-only"));
        media.Attributes.Add(PropertyAttribute("rtcp-rsize"));
        media.Attributes.Add(PropertyAttribute("rtcp-rsize"));

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Rtp,
            NegotiationRole = SdpNegotiationRole.Offerer
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(6));
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(3));
            Assert.That(result.Issues.Any(x => x.Code == SdpDiagnosticCodes.ValidationAttribute && x.Detail.Contains("property syntax", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(x => x.Code == SdpDiagnosticCodes.ValidationPort && x.AttributeName == "rtcp"), Is.True);
            Assert.That(result.Issues.Any(x => x.Code == SdpDiagnosticCodes.ValidationConnection && x.AttributeName == "rtcp"), Is.True);
        });
    }

    [Test]
    public void RtcpOfferAnswerRules_RejectMuxOnlyInAnswersAndUnadvertisedCapabilities()
    {
        SdpSessionDescription offer = DescriptionWithMedia("audio", "RTP/AVPF", "0", "96");
        SdpMediaDescription offeredMedia = offer.MediaDescriptions[0];
        offeredMedia.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        offeredMedia.Attributes.Add(PropertyAttribute("rtcp-mux"));
        offeredMedia.Attributes.Add(PropertyAttribute("rtcp-rsize"));
        offeredMedia.Attributes.Add(ValueAttribute("rtcp-fb", "* nack pli"));
        offeredMedia.Attributes.Add(ValueAttribute("rtcp-fb", "96 transport-cc"));
        SdpSessionDescription answer = DescriptionWithMedia("audio", "RTP/AVPF", "0", "96");
        SdpMediaDescription answerMedia = answer.MediaDescriptions[0];
        answerMedia.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        answerMedia.Attributes.Add(PropertyAttribute("rtcp-mux"));
        answerMedia.Attributes.Add(PropertyAttribute("rtcp-rsize"));
        answerMedia.Attributes.Add(PropertyAttribute("rtcp-mux-only"));
        answerMedia.Attributes.Add(ValueAttribute("rtcp-fb", "96 nack pli"));
        answerMedia.Attributes.Add(ValueAttribute("rtcp-fb", "96 Transport-CC"));
        SdpValidationOptions answerOptions = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Rtp,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer
        };
        SdpSessionDescription unadvertisedAnswer = DescriptionWithMedia("audio", "RTP/AVPF", "0");
        unadvertisedAnswer.MediaDescriptions[0].Attributes.Add(PropertyAttribute("rtcp-mux"));
        unadvertisedAnswer.MediaDescriptions[0].Attributes.Add(PropertyAttribute("rtcp-rsize"));
        unadvertisedAnswer.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtcp-fb", "* nack"));
        SdpSessionDescription plainOffer = DescriptionWithMedia("audio", "RTP/AVPF", "0");

        SdpValidationResult answerResult = new SdpValidator().ValidateDescription(answer, answerOptions);
        answerOptions.OfferDescription = plainOffer;
        SdpValidationResult unadvertisedResult = new SdpValidator().ValidateDescription(unadvertisedAnswer, answerOptions);

        Assert.Multiple(() =>
        {
            Assert.That(answerResult.ErrorCount, Is.EqualTo(2));
            Assert.That(answerResult.Issues.Any(x => x.AttributeName == "rtcp-mux-only" && x.Detail.Contains("forbidden", StringComparison.Ordinal)), Is.True);
            Assert.That(answerResult.Issues.Any(x => x.AttributeName == "rtcp-fb" && x.Detail.Contains("add or alter", StringComparison.Ordinal)), Is.True);
            Assert.That(answerResult.Issues.Any(x => x.AttributeName == "rtcp-fb" && x.Path.OccurrenceIndex == 0), Is.False);
            Assert.That(unadvertisedResult.ErrorCount, Is.EqualTo(3));
            Assert.That(unadvertisedResult.Issues.Select(x => x.AttributeName), Is.EquivalentTo(new[] { "rtcp-mux", "rtcp-rsize", "rtcp-fb" }));
        });
    }

    [TestCase(SdpValidationProfile.Rtp, true)]
    [TestCase(SdpValidationProfile.WebRtc, true)]
    [TestCase(SdpValidationProfile.CombinedStrict, true)]
    [TestCase(SdpValidationProfile.Rfc8866, false)]
    public void RtpRules_AreAppliedOnlyByProfilesThatIncludeRtp(SdpValidationProfile profile, bool expectedIssue)
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", "RTP/AVP", "96");

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = profile });

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap), Is.EqualTo(expectedIssue));
    }

    private static SdpSessionDescription DescriptionWithMedia(string mediaType, string protocol, params string[] formats)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = mediaType,
            MediaKind = mediaType == "audio" ? SdpMediaKind.Audio : mediaType == "video" ? SdpMediaKind.Video : SdpMediaKind.Application,
            Port = 5000,
            Protocol = protocol,
            Location = new SdpSourceLocation { LineNumber = 5, ColumnNumber = 1 }
        };
        media.Formats.AddRange(formats);
        description.MediaDescriptions.Add(media);
        return description;
    }

    private static SdpAttribute ValueAttribute(string name, string value)
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

    private static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = name,
            RawText = name,
            Location = new SdpSourceLocation { LineNumber = 6, ColumnNumber = 1 }
        };
    }

    private static SdpValidationResult ValidateRtp(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Rtp });
    }
}
