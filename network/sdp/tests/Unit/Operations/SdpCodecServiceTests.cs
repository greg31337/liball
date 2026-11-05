namespace sdp.Tests.Unit.Operations;

public class SdpCodecServiceTests
{
    [Test]
    public void ResolveMediaCodecs_JoinsTypedAttributesInMediaAndAttributeOrder()
    {
        SdpSessionDescription description = DescriptionWithMedia("UDP/TLS/RTP/SAVPF", "111", "0", "112");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "111 malformed"));
        media.Attributes.Add(ValueAttribute("rtpmap", "111 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("fmtp", "111 minptime=10; useinbandfec=1"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "* nack"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "111 transport-cc"));
        media.Attributes.Add(ValueAttribute("fmtp", "0 mode=sample"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "0 ccm fir"));
        media.Attributes.Add(ValueAttribute("ptime", "20.5"));
        media.Attributes.Add(ValueAttribute("ptime", "30"));
        media.Attributes.Add(ValueAttribute("maxptime", "60"));
        media.Attributes.Add(ValueAttribute("rtpmap", "112 telephone-event/8000"));
        ISdpCodecService service = new SdpCodecService();

        List<SdpCodec> codecs = service.ResolveMediaCodecs(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(codecs.Select(x => x.PayloadType), Is.EqualTo(new[] { 111, 0, 112 }));
            Assert.That(codecs.Select(x => x.MediaKind), Is.All.EqualTo(SdpMediaKind.Audio));
            Assert.That(codecs.Select(x => x.MediaType), Is.All.EqualTo("audio"));
            Assert.That(codecs[0].EncodingName, Is.EqualTo("opus"));
            Assert.That(codecs[0].ClockRate, Is.EqualTo(48000));
            Assert.That(codecs[0].HasChannelCount, Is.True);
            Assert.That(codecs[0].ChannelCount, Is.EqualTo(2));
            Assert.That(codecs[0].FormatParameters[0].Parameters.Select(x => x.Name), Is.EqualTo(new[] { "minptime", "useinbandfec" }));
            Assert.That(codecs[0].RtcpFeedback.Select(x => x.PayloadType), Is.EqualTo(new[] { "*", "111" }));
            Assert.That(codecs[1].EncodingName, Is.EqualTo("PCMU"));
            Assert.That(codecs[1].ClockRate, Is.EqualTo(8000));
            Assert.That(codecs[1].RtpMap, Is.Null);
            Assert.That(codecs[1].FormatParameters, Has.Count.EqualTo(1));
            Assert.That(codecs[1].RtcpFeedback.Select(x => x.PayloadType), Is.EqualTo(new[] { "*", "0" }));
            Assert.That(codecs[2].EncodingName, Is.EqualTo("telephone-event"));
            Assert.That(codecs.Select(x => x.PacketTime!.NumericValue), Is.All.EqualTo(20.5));
            Assert.That(codecs.Select(x => x.MaximumPacketTime!.NumericValue), Is.All.EqualTo(60));
        });
    }

    [Test]
    public void ResolveMediaCodecs_ProvidesEveryRfc3551StaticMapping()
    {
        SdpSessionDescription description = DescriptionWithMedia(
            "rtp/avp",
            "0", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18",
            "25", "26", "28", "31", "32", "33", "34");
        ISdpCodecService service = new SdpCodecService();

        List<SdpCodec> codecs = service.ResolveMediaCodecs(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(codecs.Select(x => x.EncodingName), Is.EqualTo(new[]
            {
                "PCMU", "GSM", "G723", "DVI4", "DVI4", "LPC", "PCMA", "G722", "L16", "L16", "QCELP", "CN", "MPA", "G728", "DVI4", "DVI4", "G729",
                "CelB", "JPEG", "nv", "H261", "MPV", "MP2T", "H263"
            }));
            Assert.That(codecs.Select(x => x.ClockRate), Is.EqualTo(new[]
            {
                8000, 8000, 8000, 8000, 16000, 8000, 8000, 8000, 44100, 44100, 8000, 8000, 90000, 8000, 11025, 22050, 8000,
                90000, 90000, 90000, 90000, 90000, 90000, 90000
            }));
            Assert.That(codecs.Select(x => x.ChannelCount), Is.EqualTo(new[]
            {
                1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1
            }));
            Assert.That(codecs.Select(x => x.HasChannelCount), Is.All.False);
        });
    }

    [Test]
    public void ResolveMediaCodecs_UsesExplicitMapInsteadOfStaticDefault()
    {
        SdpSessionDescription description = DescriptionWithMedia("RTP/AVPF", "0");
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtpmap", "0 X-CODEC/16000/4"));
        ISdpCodecService service = new SdpCodecService();

        SdpCodec codec = service.ResolveMediaCodecs(description, 0)[0];

        Assert.Multiple(() =>
        {
            Assert.That(codec.EncodingName, Is.EqualTo("X-CODEC"));
            Assert.That(codec.ClockRate, Is.EqualTo(16000));
            Assert.That(codec.HasChannelCount, Is.True);
            Assert.That(codec.ChannelCount, Is.EqualTo(4));
            Assert.That(codec.RtpMap, Is.Not.Null);
        });
    }

    [Test]
    public void ResolveMediaCodecs_SkipsNonNumericFormatsAndNonRtpMedia()
    {
        SdpSessionDescription rtp = DescriptionWithMedia("RTP/AVP", "webrtc-datachannel", "128", "-1", "96", "96");
        SdpSessionDescription nonRtp = DescriptionWithMedia("UDP/DTLS/SCTP", "0", "96", "webrtc-datachannel");
        ISdpCodecService service = new SdpCodecService();

        List<SdpCodec> rtpCodecs = service.ResolveMediaCodecs(rtp, 0);
        List<SdpCodec> nonRtpCodecs = service.ResolveMediaCodecs(nonRtp, 0);

        Assert.Multiple(() =>
        {
            Assert.That(rtpCodecs.Select(x => x.PayloadType), Is.EqualTo(new[] { 96, 96 }));
            Assert.That(rtpCodecs.Select(x => x.EncodingName), Is.All.Empty);
            Assert.That(nonRtpCodecs, Is.Empty);
        });
    }

    [Test]
    public void ResolveMediaCodecs_ReturnsIndependentTypedProjectionGraphs()
    {
        SdpSessionDescription description = DescriptionWithMedia("RTP/AVP", "111");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "111 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("fmtp", "111 useinbandfec=1"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "111 nack"));
        media.Attributes.Add(ValueAttribute("ptime", "20"));
        ISdpCodecService service = new SdpCodecService();

        SdpCodec first = service.ResolveMediaCodecs(description, 0)[0];
        SdpCodec second = service.ResolveMediaCodecs(description, 0)[0];
        first.RtpMap!.EncodingName = "changed";
        first.FormatParameters[0].Parameters[0].Value = "0";
        first.RtcpFeedback[0].Type = "changed";
        first.PacketTime!.NumericValue = 40;

        Assert.Multiple(() =>
        {
            Assert.That(second.RtpMap!.EncodingName, Is.EqualTo("opus"));
            Assert.That(second.FormatParameters[0].Parameters[0].Value, Is.EqualTo("1"));
            Assert.That(second.RtcpFeedback[0].Type, Is.EqualTo("nack"));
            Assert.That(second.PacketTime!.NumericValue, Is.EqualTo(20));
            Assert.That(media.Attributes.Select(x => x.Value), Is.EqualTo(new[] { "111 opus/48000/2", "111 useinbandfec=1", "111 nack", "20" }));
        });
    }

    [Test]
    public void ResolveMediaCodecs_WildcardFeedbackParticipatesInCompatibleAndIncompatiblePairs()
    {
        SdpSessionDescription description = DescriptionWithMedia("RTP/AVPF", "96", "97");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtpmap", "96 X-FIRST/8000"));
        media.Attributes.Add(ValueAttribute("rtpmap", "97 X-SECOND/8000"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "* nack pli"));
        media.Attributes.Add(ValueAttribute("rtcp-fb", "96 transport-cc"));
        ISdpCodecService service = new SdpCodecService();
        List<SdpCodec> codecs = service.ResolveMediaCodecs(description, 0);
        SdpCodecCapability firstCapability = Capability("X-FIRST", 8000);
        firstCapability.RtcpFeedbackTypes.Add("nack pli");
        firstCapability.RtcpFeedbackTypes.Add("transport-cc");
        SdpCodecCapability secondCapability = Capability("X-SECOND", 8000);
        secondCapability.RtcpFeedbackTypes.Add("nack pli");

        bool firstStrict = service.IsCodecMatch(codecs[0], firstCapability, SdpCodecMatchMode.Strict);
        bool secondStrict = service.IsCodecMatch(codecs[1], secondCapability, SdpCodecMatchMode.Strict);
        secondCapability.RtcpFeedbackTypes.Add("transport-cc");
        bool unavailableTargetedFeedback = service.IsCodecMatch(codecs[1], secondCapability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(codecs[0].RtcpFeedback.Select(x => x.PayloadType), Is.EqualTo(new[] { "*", "96" }));
            Assert.That(codecs[1].RtcpFeedback.Select(x => x.PayloadType), Is.EqualTo(new[] { "*" }));
            Assert.That(firstStrict, Is.True);
            Assert.That(secondStrict, Is.True);
            Assert.That(unavailableTargetedFeedback, Is.False);
        });
    }

    [Test]
    public void ResolveMediaCodecs_RejectsNullDescriptionAndInvalidMediaIndex()
    {
        ISdpCodecService service = new SdpCodecService();
        SdpSessionDescription empty = new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ResolveMediaCodecs(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveMediaCodecs(empty, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveMediaCodecs(empty, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
        });
    }

    [Test]
    public void IsCodecMatch_UsesMediaIdentityNameAndClockRate()
    {
        SdpCodec codec = Codec("opus", 48000);
        SdpCodecCapability capability = Capability("OPUS", 48000);
        capability.MediaType = "AUDIO";
        ISdpCodecService service = new SdpCodecService();

        bool matching = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.MediaKind = SdpMediaKind.Video;
        bool wrongKind = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.MediaKind = SdpMediaKind.Audio;
        capability.EncodingName = "PCMU";
        bool wrongName = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.EncodingName = "opus";
        capability.ClockRate = 8000;
        bool wrongRate = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.ClockRate = 48000;
        capability.MediaType = "video";
        bool wrongType = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(matching, Is.True);
            Assert.That(wrongKind, Is.False);
            Assert.That(wrongName, Is.False);
            Assert.That(wrongRate, Is.False);
            Assert.That(wrongType, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_UsesMediaTypeWhenRecognizedKindsAreUnavailable()
    {
        SdpCodec codec = Codec("x-codec", 90000);
        SdpCodecCapability capability = Capability("X-CODEC", 90000);
        codec.MediaKind = SdpMediaKind.Unknown;
        capability.MediaKind = SdpMediaKind.Unknown;

        bool sameType = new SdpCodecService().IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.MediaType = string.Empty;
        bool missingIdentity = new SdpCodecService().IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(sameType, Is.True);
            Assert.That(missingIdentity, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_ConstrainsChannelsOnlyWhenCapabilityRequestsIt()
    {
        SdpCodec codec = Codec("L16", 44100);
        codec.HasChannelCount = false;
        codec.ChannelCount = 2;
        SdpCodecCapability capability = Capability("L16", 44100);
        capability.ChannelCount = 99;
        ISdpCodecService service = new SdpCodecService();

        bool unconstrained = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.HasChannelCount = true;
        capability.ChannelCount = 2;
        bool stereo = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.ChannelCount = 1;
        bool mono = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.ChannelCount = 0;
        bool invalid = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(unconstrained, Is.True);
            Assert.That(stereo, Is.True);
            Assert.That(mono, Is.False);
            Assert.That(invalid, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_StrictModeRequiresExactParameterMultiset()
    {
        SdpCodec codec = Codec("x-codec", 48000);
        SdpFormatParametersAttribute first = new SdpFormatParametersAttribute();
        first.Parameters.Add(Parameter("UseInBandFec", true, "1"));
        SdpFormatParametersAttribute second = new SdpFormatParametersAttribute();
        second.Parameters.Add(Parameter("stereo", false, string.Empty));
        codec.FormatParameters.Add(first);
        codec.FormatParameters.Add(second);
        SdpCodecCapability capability = Capability("X-CODEC", 48000);
        capability.FormatParameters.Add(Parameter("STEREO", false, string.Empty));
        capability.FormatParameters.Add(Parameter("useinbandfec", true, "1"));
        ISdpCodecService service = new SdpCodecService();

        bool reordered = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.FormatParameters[1].Value = "0";
        bool differentValue = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.FormatParameters[1].Value = "1";
        capability.FormatParameters.Add(Parameter("maxplaybackrate", true, "48000"));
        bool missingParameter = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(reordered, Is.True);
            Assert.That(differentValue, Is.False);
            Assert.That(missingParameter, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_CompatibleModeRequiresCapabilitySubsetAndPreservesMultiplicity()
    {
        SdpCodec codec = Codec("opus", 48000);
        SdpFormatParametersAttribute format = new SdpFormatParametersAttribute();
        format.Parameters.Add(Parameter("useinbandfec", true, "1"));
        format.Parameters.Add(Parameter("stereo", true, "1"));
        format.Parameters.Add(Parameter("maxplaybackrate", true, "48000"));
        codec.FormatParameters.Add(format);
        SdpCodecCapability capability = Capability("opus", 48000);
        capability.FormatParameters.Add(Parameter("USEINBANDFEC", true, "1"));
        capability.FormatParameters.Add(Parameter("stereo", true, "1"));
        ISdpCodecService service = new SdpCodecService();

        bool subset = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        bool strict = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.FormatParameters.Add(Parameter("useinbandfec", true, "1"));
        bool duplicateRequirement = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.FormatParameters.RemoveAt(2);
        capability.FormatParameters[1] = Parameter("stereo", false, string.Empty);
        bool flagMismatch = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(subset, Is.True);
            Assert.That(strict, Is.False);
            Assert.That(duplicateRequirement, Is.False);
            Assert.That(flagMismatch, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_IntersectsRtcpFeedbackCapabilitiesBySelectedMode()
    {
        SdpCodec codec = Codec("x-codec", 48000);
        codec.RtcpFeedback.Add(new SdpRtcpFeedbackAttribute
        {
            PayloadType = "*",
            Type = "nack",
            HasParameter = true,
            Parameter = "pli"
        });
        codec.RtcpFeedback.Add(new SdpRtcpFeedbackAttribute
        {
            PayloadType = "96",
            Type = "transport-cc"
        });
        SdpCodecCapability capability = Capability("x-codec", 48000);
        capability.RtcpFeedbackTypes.Add("nack pli");
        capability.RtcpFeedbackTypes.Add("transport-cc");
        ISdpCodecService service = new SdpCodecService();

        bool strict = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.RtcpFeedbackTypes.RemoveAt(1);
        bool compatibleSubset = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        bool strictSubset = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        capability.RtcpFeedbackTypes[0] = "NACK pli";
        bool caseChanged = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.RtcpFeedbackTypes[0] = "nack sli";
        bool differentParameter = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        List<SdpCodec> unsupportedIntersection = service.IntersectCodecs(new List<SdpCodec> { codec }, new List<SdpCodecCapability> { capability }, SdpCodecMatchMode.Compatible);
        capability.RtcpFeedbackTypes[0] = "nack pli";
        List<SdpCodec> matchingIntersection = service.IntersectCodecs(new List<SdpCodec> { codec }, new List<SdpCodecCapability> { capability }, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(strict, Is.True);
            Assert.That(compatibleSubset, Is.True);
            Assert.That(strictSubset, Is.False);
            Assert.That(caseChanged, Is.False);
            Assert.That(differentParameter, Is.False);
            Assert.That(unsupportedIntersection, Is.Empty);
            Assert.That(matchingIntersection, Is.EqualTo(new[] { codec }));
        });
    }

    [Test]
    public void IsCodecMatch_StrictModeRejectsUnparsedFmtpWhileCompatibleModeAllowsNoRequirements()
    {
        SdpCodec codec = Codec("x-codec", 8000);
        codec.FormatParameters.Add(new SdpFormatParametersAttribute { RawParameters = "broken;" });
        SdpCodecCapability capability = Capability("x-codec", 8000);
        ISdpCodecService service = new SdpCodecService();

        bool strict = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);
        bool compatible = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(strict, Is.False);
            Assert.That(compatible, Is.True);
        });
    }

    [Test]
    public void IsCodecMatch_OpusCompatibleModeTreatsValidPreferencesAsOrthogonal()
    {
        SdpCodec codec = Codec("opus", 48000);
        codec.ChannelCount = 2;
        SdpFormatParametersAttribute format = new SdpFormatParametersAttribute();
        format.Parameters.Add(Parameter("stereo", true, "0"));
        format.Parameters.Add(Parameter("useinbandfec", true, "1"));
        format.Parameters.Add(Parameter("usedtx", true, "0"));
        format.Parameters.Add(Parameter("maxaveragebitrate", true, "32000"));
        format.Parameters.Add(Parameter("maxplaybackrate", true, "16000"));
        format.Parameters.Add(Parameter("minptime", true, "10"));
        format.Parameters.Add(Parameter("x-offer", true, "retained"));
        codec.FormatParameters.Add(format);
        codec.PacketTime = PacketTime("ptime", 20);
        codec.MaximumPacketTime = PacketTime("maxptime", 60);
        SdpCodecCapability capability = Capability("OPUS", 48000);
        capability.FormatParameters.Add(Parameter("stereo", true, "1"));
        capability.FormatParameters.Add(Parameter("useinbandfec", true, "0"));
        capability.FormatParameters.Add(Parameter("usedtx", true, "1"));
        capability.FormatParameters.Add(Parameter("maxaveragebitrate", true, "128000"));
        capability.FormatParameters.Add(Parameter("maxplaybackrate", true, "48000"));
        capability.FormatParameters.Add(Parameter("minptime", true, "40"));
        capability.FormatParameters.Add(Parameter("x-local", true, "ignored"));
        ISdpCodecService service = new SdpCodecService();

        bool compatible = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        bool strict = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(compatible, Is.True);
            Assert.That(strict, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_OpusRejectsInvalidKnownParametersAndSignaling()
    {
        SdpCodec codec = Codec("opus", 48000);
        codec.ChannelCount = 2;
        SdpCodecCapability capability = Capability("opus", 48000);
        capability.FormatParameters.Add(Parameter("stereo", true, "2"));
        ISdpCodecService service = new SdpCodecService();

        bool invalidCapability = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.FormatParameters[0].Value = "1";
        codec.PacketTime = PacketTime("ptime", 7);
        bool invalidPacketTime = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        codec.PacketTime = PacketTime("ptime", 20);
        codec.MaximumPacketTime = PacketTime("maxptime", 10);
        bool inconsistentMaximum = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        codec.MaximumPacketTime = PacketTime("maxptime", 121);
        bool invalidMaximum = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        codec.MaximumPacketTime = PacketTime("maxptime", 60);
        bool validPacketTimes = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        codec.ClockRate = 44100;
        capability.ClockRate = 44100;
        bool invalidClock = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(invalidCapability, Is.False);
            Assert.That(invalidPacketTime, Is.False);
            Assert.That(inconsistentMaximum, Is.False);
            Assert.That(invalidMaximum, Is.False);
            Assert.That(validPacketTimes, Is.True);
            Assert.That(invalidClock, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_H264CompatibleModeMatchesEquivalentProfilesAndSymmetricPacketization()
    {
        SdpCodec codec = Codec("H264", 90000);
        codec.MediaKind = SdpMediaKind.Video;
        codec.MediaType = "video";
        SdpFormatParametersAttribute format = new SdpFormatParametersAttribute();
        format.Parameters.Add(Parameter("profile-level-id", true, "42e01f"));
        format.Parameters.Add(Parameter("packetization-mode", true, "1"));
        format.Parameters.Add(Parameter("x-offer", true, "ignored"));
        codec.FormatParameters.Add(format);
        SdpCodecCapability capability = Capability("h264", 90000);
        capability.MediaKind = SdpMediaKind.Video;
        capability.MediaType = "video";
        capability.FormatParameters.Add(Parameter("profile-level-id", true, "4D801F"));
        capability.FormatParameters.Add(Parameter("packetization-mode", true, "1"));
        capability.FormatParameters.Add(Parameter("x-local", true, "also-ignored"));
        ISdpCodecService service = new SdpCodecService();

        bool equivalentProfile = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.FormatParameters[1].Value = "0";
        bool differentPacketization = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.FormatParameters[1].Value = "1";
        capability.FormatParameters[0].Value = "64001F";
        bool differentProfile = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(equivalentProfile, Is.True);
            Assert.That(differentPacketization, Is.False);
            Assert.That(differentProfile, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_H264NegotiatesDifferentLevelsAndLevelAsymmetryPreferences()
    {
        SdpCodec codec = Codec("H264", 90000);
        codec.MediaKind = SdpMediaKind.Video;
        codec.MediaType = "video";
        SdpFormatParametersAttribute format = new SdpFormatParametersAttribute();
        format.Parameters.Add(Parameter("profile-level-id", true, "42e01f"));
        format.Parameters.Add(Parameter("level-asymmetry-allowed", true, "1"));
        codec.FormatParameters.Add(format);
        SdpCodecCapability capability = Capability("H264", 90000);
        capability.MediaKind = SdpMediaKind.Video;
        capability.MediaType = "video";
        capability.FormatParameters.Add(Parameter("profile-level-id", true, "4d801e"));
        ISdpCodecService service = new SdpCodecService();

        bool oneSided = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.FormatParameters.Add(Parameter("level-asymmetry-allowed", true, "1"));
        bool bilateral = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        codec.FormatParameters[0].Parameters[1].Value = "0";
        bool disabled = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(oneSided, Is.True);
            Assert.That(bilateral, Is.True);
            Assert.That(disabled, Is.True);
        });
    }

    [Test]
    public void IsCodecMatch_H264AppliesDefaultsAndRejectsMalformedKnownParameters()
    {
        SdpCodec codec = Codec("H264", 90000);
        codec.MediaKind = SdpMediaKind.Video;
        codec.MediaType = "video";
        SdpCodecCapability capability = Capability("H264", 90000);
        capability.MediaKind = SdpMediaKind.Video;
        capability.MediaType = "video";
        capability.FormatParameters.Add(Parameter("profile-level-id", true, "42000A"));
        capability.FormatParameters.Add(Parameter("packetization-mode", true, "0"));
        ISdpCodecService service = new SdpCodecService();

        bool defaults = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.FormatParameters[0].Value = "42e0zz";
        bool malformedProfile = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.FormatParameters[0].Value = "42000A";
        capability.FormatParameters.Add(Parameter("packetization-mode", true, "0"));
        bool duplicateMode = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(defaults, Is.True);
            Assert.That(malformedProfile, Is.False);
            Assert.That(duplicateMode, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_VpCapabilitiesUseTypedLimitsAndVp9ProfileDefaults()
    {
        SdpCodec vp8 = Codec("VP8", 90000);
        vp8.MediaKind = SdpMediaKind.Video;
        vp8.MediaType = "video";
        SdpFormatParametersAttribute vp8Format = new SdpFormatParametersAttribute();
        vp8Format.Parameters.Add(Parameter("max-fr", true, "30"));
        vp8Format.Parameters.Add(Parameter("max-fs", true, "3600"));
        vp8.FormatParameters.Add(vp8Format);
        SdpCodecCapability vp8Capability = Capability("vp8", 90000);
        vp8Capability.MediaKind = SdpMediaKind.Video;
        vp8Capability.MediaType = "video";
        vp8Capability.FormatParameters.Add(Parameter("max-fr", true, "60"));
        vp8Capability.FormatParameters.Add(Parameter("max-fs", true, "1200"));
        SdpCodec vp9 = Codec("VP9", 90000);
        vp9.MediaKind = SdpMediaKind.Video;
        vp9.MediaType = "video";
        SdpCodecCapability vp9Capability = Capability("vp9", 90000);
        vp9Capability.MediaKind = SdpMediaKind.Video;
        vp9Capability.MediaType = "video";
        ISdpCodecService service = new SdpCodecService();

        bool vp8Preferences = service.IsCodecMatch(vp8, vp8Capability, SdpCodecMatchMode.Compatible);
        vp8Capability.FormatParameters[0].Value = "0";
        bool invalidVp8Limit = service.IsCodecMatch(vp8, vp8Capability, SdpCodecMatchMode.Compatible);
        bool vp9Defaults = service.IsCodecMatch(vp9, vp9Capability, SdpCodecMatchMode.Compatible);
        vp9Capability.FormatParameters.Add(Parameter("profile-id", true, "1"));
        bool differentVp9Profile = service.IsCodecMatch(vp9, vp9Capability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(vp8Preferences, Is.True);
            Assert.That(invalidVp8Limit, Is.False);
            Assert.That(vp9Defaults, Is.True);
            Assert.That(differentVp9Profile, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_HevcAliasesApplyConfigurationDefaultsAndIgnoreLevelDifferences()
    {
        SdpCodec codec = Codec("H265", 90000);
        codec.MediaKind = SdpMediaKind.Video;
        codec.MediaType = "video";
        SdpFormatParametersAttribute format = new SdpFormatParametersAttribute();
        format.Parameters.Add(Parameter("profile-id", true, "1"));
        format.Parameters.Add(Parameter("level-id", true, "120"));
        codec.FormatParameters.Add(format);
        SdpCodecCapability capability = Capability("HEVC", 90000);
        capability.MediaKind = SdpMediaKind.Video;
        capability.MediaType = "video";
        capability.FormatParameters.Add(Parameter("profile-compatibility-indicator", true, "40000000"));
        capability.FormatParameters.Add(Parameter("interop-constraints", true, "B00000000000"));
        capability.FormatParameters.Add(Parameter("level-id", true, "93"));
        ISdpCodecService service = new SdpCodecService();

        bool defaults = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);
        capability.FormatParameters[0].Value = "20000000";
        bool differentProfileCompatibility = service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(defaults, Is.True);
            Assert.That(differentProfileCompatibility, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_RtxAndTelephoneEventUseAssociationAndRangeSemantics()
    {
        SdpCodec rtx = Codec("rtx", 90000);
        rtx.MediaKind = SdpMediaKind.Video;
        rtx.MediaType = "video";
        SdpFormatParametersAttribute rtxFormat = new SdpFormatParametersAttribute();
        rtxFormat.Parameters.Add(Parameter("apt", true, "96"));
        rtxFormat.Parameters.Add(Parameter("rtx-time", true, "3000"));
        rtx.FormatParameters.Add(rtxFormat);
        SdpCodecCapability rtxCapability = Capability("RTX", 90000);
        rtxCapability.MediaKind = SdpMediaKind.Video;
        rtxCapability.MediaType = "video";
        rtxCapability.FormatParameters.Add(Parameter("apt", true, "96"));
        rtxCapability.FormatParameters.Add(Parameter("rtx-time", true, "1000"));
        SdpCodec telephone = Codec("telephone-event", 8000);
        telephone.FormatParameters.Add(new SdpFormatParametersAttribute { RawParameters = "0-15,66" });
        SdpCodecCapability telephoneCapability = Capability("telephone-event", 8000);
        telephoneCapability.FormatParameters.Add(Parameter("15-20", false, string.Empty));
        ISdpCodecService service = new SdpCodecService();

        bool matchingRtx = service.IsCodecMatch(rtx, rtxCapability, SdpCodecMatchMode.Compatible);
        rtxCapability.FormatParameters[0].Value = "97";
        bool differentAssociation = service.IsCodecMatch(rtx, rtxCapability, SdpCodecMatchMode.Compatible);
        bool overlappingEvents = service.IsCodecMatch(telephone, telephoneCapability, SdpCodecMatchMode.Compatible);
        telephoneCapability.FormatParameters[0].Name = "70-80";
        bool disjointEvents = service.IsCodecMatch(telephone, telephoneCapability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(matchingRtx, Is.True);
            Assert.That(differentAssociation, Is.False);
            Assert.That(overlappingEvents, Is.True);
            Assert.That(disjointEvents, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_RedAndFecUseTypedPayloadAndRepairConstraints()
    {
        SdpCodec red = Codec("red", 90000);
        red.MediaKind = SdpMediaKind.Video;
        red.MediaType = "video";
        red.FormatParameters.Add(new SdpFormatParametersAttribute { RawParameters = "96/97" });
        SdpCodecCapability redCapability = Capability("RED", 90000);
        redCapability.MediaKind = SdpMediaKind.Video;
        redCapability.MediaType = "video";
        redCapability.FormatParameters.Add(Parameter("96/97", false, string.Empty));
        SdpCodec flexFec = Codec("flexfec", 90000);
        flexFec.MediaKind = SdpMediaKind.Video;
        flexFec.MediaType = "video";
        SdpFormatParametersAttribute flexFormat = new SdpFormatParametersAttribute();
        flexFormat.Parameters.Add(Parameter("repair-window", true, "100000"));
        flexFec.FormatParameters.Add(flexFormat);
        SdpCodecCapability flexCapability = Capability("FLEXFEC", 90000);
        flexCapability.MediaKind = SdpMediaKind.Video;
        flexCapability.MediaType = "video";
        flexCapability.FormatParameters.Add(Parameter("repair-window", true, "200000"));
        SdpCodec ulpFec = Codec("ulpfec", 90000);
        ulpFec.MediaKind = SdpMediaKind.Video;
        ulpFec.MediaType = "video";
        SdpCodecCapability ulpCapability = Capability("ULPFEC", 90000);
        ulpCapability.MediaKind = SdpMediaKind.Video;
        ulpCapability.MediaType = "video";
        ulpCapability.FormatParameters.Add(Parameter("onelevelonly", true, "1"));
        ISdpCodecService service = new SdpCodecService();

        bool redMatch = service.IsCodecMatch(red, redCapability, SdpCodecMatchMode.Compatible);
        redCapability.FormatParameters[0].Name = "96/98";
        bool redMismatch = service.IsCodecMatch(red, redCapability, SdpCodecMatchMode.Compatible);
        bool flexWindow = service.IsCodecMatch(flexFec, flexCapability, SdpCodecMatchMode.Compatible);
        flexCapability.FormatParameters[0].Value = "50000";
        bool flexWindowTooSmall = service.IsCodecMatch(flexFec, flexCapability, SdpCodecMatchMode.Compatible);
        bool ulpPreference = service.IsCodecMatch(ulpFec, ulpCapability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(redMatch, Is.True);
            Assert.That(redMismatch, Is.False);
            Assert.That(flexWindow, Is.True);
            Assert.That(flexWindowTooSmall, Is.False);
            Assert.That(ulpPreference, Is.True);
        });
    }

    [Test]
    public void IsCodecMatch_ComfortNoiseAndUlpFecUseTypedMappingConstraints()
    {
        SdpCodec comfortNoise = Codec("CN", 8000);
        SdpCodecCapability comfortNoiseCapability = Capability("cn", 8000);
        SdpCodec ulpFec = Codec("ulpfec", 90000);
        SdpCodecCapability ulpFecCapability = Capability("ULPFEC", 90000);
        ulpFecCapability.FormatParameters.Add(Parameter("onelevelonly", true, "1"));
        ISdpCodecService service = new SdpCodecService();

        bool comfortNoiseMatch = service.IsCodecMatch(comfortNoise, comfortNoiseCapability, SdpCodecMatchMode.Compatible);
        comfortNoiseCapability.HasChannelCount = true;
        comfortNoiseCapability.ChannelCount = 2;
        bool stereoComfortNoise = service.IsCodecMatch(comfortNoise, comfortNoiseCapability, SdpCodecMatchMode.Compatible);
        bool ulpFecMatch = service.IsCodecMatch(ulpFec, ulpFecCapability, SdpCodecMatchMode.Compatible);
        ulpFecCapability.FormatParameters[0].Value = "2";
        bool invalidUlpFecPreference = service.IsCodecMatch(ulpFec, ulpFecCapability, SdpCodecMatchMode.Compatible);
        ulpFecCapability.FormatParameters[0].Value = "1";
        ulpFec.ClockRate = 1000;
        ulpFecCapability.ClockRate = 1000;
        bool invalidUlpFecClock = service.IsCodecMatch(ulpFec, ulpFecCapability, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(comfortNoiseMatch, Is.True);
            Assert.That(stereoComfortNoise, Is.False);
            Assert.That(ulpFecMatch, Is.True);
            Assert.That(invalidUlpFecPreference, Is.False);
            Assert.That(invalidUlpFecClock, Is.False);
        });
    }

    [Test]
    public void IsCodecMatch_RejectsNullArgumentsAndInvalidModes()
    {
        ISdpCodecService service = new SdpCodecService();
        SdpCodec codec = Codec("opus", 48000);
        SdpCodecCapability capability = Capability("opus", 48000);

        Assert.Multiple(() =>
        {
            Assert.That(() => service.IsCodecMatch(null!, capability, SdpCodecMatchMode.Strict), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("codec"));
            Assert.That(() => service.IsCodecMatch(codec, null!, SdpCodecMatchMode.Strict), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("capability"));
            Assert.That(() => service.IsCodecMatch(codec, capability, SdpCodecMatchMode.Unknown), Throws.ArgumentException.With.Property("ParamName").EqualTo("matchMode"));
            Assert.That(() => service.IsCodecMatch(codec, capability, (SdpCodecMatchMode)99), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("matchMode"));
        });
    }

    [Test]
    public void OrderCodecsByPreference_IsStableAndRetainsUnmatchedCodecs()
    {
        SdpCodec unknownFirst = Codec("x-first", 8000);
        unknownFirst.PayloadType = 120;
        SdpCodec pcmu = Codec("PCMU", 8000);
        pcmu.PayloadType = 0;
        SdpCodec opusFirst = Codec("opus", 48000);
        opusFirst.PayloadType = 111;
        SdpCodec unknownSecond = Codec("x-second", 16000);
        unknownSecond.PayloadType = 121;
        SdpCodec g722 = Codec("G722", 8000);
        g722.PayloadType = 9;
        SdpCodec opusSecond = Codec("OPUS", 48000);
        opusSecond.PayloadType = 112;
        List<SdpCodec> codecs = new List<SdpCodec> { unknownFirst, pcmu, opusFirst, unknownSecond, g722, opusSecond };
        List<SdpCodecCapability> capabilities = new List<SdpCodecCapability>
        {
            CapabilityWithPreference("opus", 48000, 0),
            CapabilityWithPreference("PCMU", 8000, 10),
            CapabilityWithPreference("G722", 8000, 10)
        };
        ISdpCodecService service = new SdpCodecService();

        List<SdpCodec> ordered = service.OrderCodecsByPreference(codecs, capabilities, SdpCodecMatchMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(ordered, Is.Not.SameAs(codecs));
            Assert.That(ordered.Select(x => x.PayloadType), Is.EqualTo(new[] { 111, 112, 0, 9, 120, 121 }));
            Assert.That(ordered[0], Is.SameAs(opusFirst));
            Assert.That(ordered[1], Is.SameAs(opusSecond));
            Assert.That(ordered[4], Is.SameAs(unknownFirst));
            Assert.That(ordered[5], Is.SameAs(unknownSecond));
            Assert.That(codecs.Select(x => x.PayloadType), Is.EqualTo(new[] { 120, 0, 111, 121, 9, 112 }));
        });
    }

    [Test]
    public void OrderCodecsByPreference_UsesBestMatchingCapabilityAndSupportsExtensionNames()
    {
        SdpCodec pcmu = Codec("PCMU", 8000);
        SdpCodec extension = Codec("X-CODEC", 90000);
        List<SdpCodec> codecs = new List<SdpCodec> { pcmu, extension };
        List<SdpCodecCapability> capabilities = new List<SdpCodecCapability>
        {
            CapabilityWithPreference("x-codec", 90000, 20),
            CapabilityWithPreference("PCMU", 8000, 0),
            CapabilityWithPreference("X-CODEC", 90000, -1)
        };

        List<SdpCodec> ordered = new SdpCodecService().OrderCodecsByPreference(codecs, capabilities, SdpCodecMatchMode.Compatible);

        Assert.That(ordered, Is.EqualTo(new[] { extension, pcmu }));
    }

    [Test]
    public void IntersectCodecs_PreservesRemoteFormatOrderAndIncludesMatchingExtensionsOnce()
    {
        SdpCodec vp8 = Codec("VP8", 90000);
        vp8.MediaKind = SdpMediaKind.Video;
        vp8.MediaType = "video";
        SdpCodec extension = Codec("X-AUDIO", 16000);
        SdpCodec opus = Codec("opus", 48000);
        SdpCodec pcmu = Codec("PCMU", 8000);
        List<SdpCodec> remote = new List<SdpCodec> { vp8, extension, opus, pcmu };
        List<SdpCodecCapability> local = new List<SdpCodecCapability>
        {
            CapabilityWithPreference("PCMU", 8000, 0),
            CapabilityWithPreference("X-AUDIO", 16000, 1),
            CapabilityWithPreference("x-audio", 16000, 2),
            CapabilityWithPreference("opus", 48000, 3)
        };
        ISdpCodecService service = new SdpCodecService();

        List<SdpCodec> intersection = service.IntersectCodecs(remote, local, SdpCodecMatchMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(intersection, Is.Not.SameAs(remote));
            Assert.That(intersection, Is.EqualTo(new[] { extension, opus, pcmu }));
            Assert.That(intersection[0], Is.SameAs(extension));
            Assert.That(remote, Is.EqualTo(new[] { vp8, extension, opus, pcmu }));
        });
    }

    [Test]
    public void IntersectCodecs_AppliesTheSelectedParameterMatchingMode()
    {
        SdpCodec codec = Codec("opus", 48000);
        SdpFormatParametersAttribute format = new SdpFormatParametersAttribute();
        format.Parameters.Add(Parameter("useinbandfec", true, "1"));
        format.Parameters.Add(Parameter("stereo", true, "1"));
        codec.FormatParameters.Add(format);
        SdpCodecCapability capability = Capability("opus", 48000);
        capability.FormatParameters.Add(Parameter("useinbandfec", true, "1"));
        List<SdpCodec> codecs = new List<SdpCodec> { codec };
        List<SdpCodecCapability> capabilities = new List<SdpCodecCapability> { capability };
        ISdpCodecService service = new SdpCodecService();

        List<SdpCodec> strict = service.IntersectCodecs(codecs, capabilities, SdpCodecMatchMode.Strict);
        List<SdpCodec> compatible = service.IntersectCodecs(codecs, capabilities, SdpCodecMatchMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(strict, Is.Empty);
            Assert.That(compatible, Is.EqualTo(new[] { codec }));
        });
    }

    [Test]
    public void OrderingAndIntersection_ValidateCollectionsAndModes()
    {
        ISdpCodecService service = new SdpCodecService();
        List<SdpCodec> codecs = new List<SdpCodec> { Codec("opus", 48000) };
        List<SdpCodecCapability> capabilities = new List<SdpCodecCapability> { Capability("opus", 48000) };
        List<SdpCodec> codecsWithNull = new List<SdpCodec> { null! };
        List<SdpCodecCapability> capabilitiesWithNull = new List<SdpCodecCapability> { null! };

        Assert.Multiple(() =>
        {
            Assert.That(() => service.OrderCodecsByPreference(null!, capabilities, SdpCodecMatchMode.Strict), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("codecs"));
            Assert.That(() => service.OrderCodecsByPreference(codecs, null!, SdpCodecMatchMode.Strict), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("capabilities"));
            Assert.That(() => service.OrderCodecsByPreference(codecsWithNull, capabilities, SdpCodecMatchMode.Strict), Throws.ArgumentException.With.Property("ParamName").EqualTo("codecs"));
            Assert.That(() => service.OrderCodecsByPreference(codecs, capabilitiesWithNull, SdpCodecMatchMode.Strict), Throws.ArgumentException.With.Property("ParamName").EqualTo("capabilities"));
            Assert.That(() => service.IntersectCodecs(null!, capabilities, SdpCodecMatchMode.Strict), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("remoteCodecs"));
            Assert.That(() => service.IntersectCodecs(codecs, null!, SdpCodecMatchMode.Strict), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("localCapabilities"));
            Assert.That(() => service.IntersectCodecs(codecs, capabilities, SdpCodecMatchMode.Unknown), Throws.ArgumentException.With.Property("ParamName").EqualTo("matchMode"));
            Assert.That(() => service.OrderCodecsByPreference(codecs, capabilities, (SdpCodecMatchMode)99), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("matchMode"));
        });
    }

    [Test]
    public void AllocatePayloadTypes_PreservesOfferedAssignmentsForSymmetricAnswers()
    {
        SdpCodec opus = Codec("opus", 48000);
        opus.PayloadType = 111;
        SdpCodec pcmu = Codec("PCMU", 8000);
        pcmu.PayloadType = 0;
        List<SdpCodec> codecs = new List<SdpCodec> { opus, pcmu };
        ISdpCodecService service = new SdpCodecService();

        List<SdpPayloadMapping> mappings = service.AllocatePayloadTypes(codecs, 3, true);

        Assert.Multiple(() =>
        {
            Assert.That(mappings.Select(x => x.MediaIndex), Is.All.EqualTo(3));
            Assert.That(mappings.Select(x => x.OfferedPayloadType), Is.EqualTo(new[] { 111, 0 }));
            Assert.That(mappings.Select(x => x.NegotiatedPayloadType), Is.EqualTo(new[] { 111, 0 }));
            Assert.That(mappings.Select(x => x.EncodingName), Is.EqualTo(new[] { "opus", "PCMU" }));
            Assert.That(codecs.Select(x => x.PayloadType), Is.EqualTo(new[] { 111, 0 }));
        });
    }

    [Test]
    public void AllocatePayloadTypes_UsesDynamicRangeInFormatOrder()
    {
        SdpCodec first = Codec("x-first", 8000);
        first.PayloadType = 42;
        SdpCodec second = Codec("x-second", 16000);
        second.PayloadType = 127;

        List<SdpPayloadMapping> mappings = new SdpCodecService().AllocatePayloadTypes(new List<SdpCodec> { first, second }, 0, false);

        Assert.Multiple(() =>
        {
            Assert.That(mappings.Select(x => x.OfferedPayloadType), Is.EqualTo(new[] { 42, 127 }));
            Assert.That(mappings.Select(x => x.NegotiatedPayloadType), Is.EqualTo(new[] { 96, 97 }));
            Assert.That(first.PayloadType, Is.EqualTo(42));
            Assert.That(second.PayloadType, Is.EqualTo(127));
        });
    }

    [Test]
    public void AllocatePayloadTypes_FillsTheEntireDynamicRangeAtExactCapacity()
    {
        List<SdpCodec> codecs = new List<SdpCodec>();
        int index = 0;
        while (index < 32)
        {
            SdpCodec codec = Codec("codec-" + index, 8000);
            codec.PayloadType = index;
            codecs.Add(codec);
            index = index + 1;
        }

        List<SdpPayloadMapping> mappings = new SdpCodecService().AllocatePayloadTypes(codecs, 4, false);

        Assert.Multiple(() =>
        {
            Assert.That(mappings, Has.Count.EqualTo(32));
            Assert.That(mappings[0].NegotiatedPayloadType, Is.EqualTo(96));
            Assert.That(mappings[31].NegotiatedPayloadType, Is.EqualTo(127));
            Assert.That(mappings.Select(x => x.MediaIndex), Is.All.EqualTo(4));
            Assert.That(codecs.Select(x => x.PayloadType), Is.EqualTo(Enumerable.Range(0, 32)));
        });
    }

    [Test]
    public void AllocatePayloadTypes_RejectsCollisionsExhaustionAndInvalidArguments()
    {
        ISdpCodecService service = new SdpCodecService();
        SdpCodec duplicateFirst = Codec("one", 8000);
        duplicateFirst.PayloadType = 100;
        SdpCodec duplicateSecond = Codec("two", 8000);
        duplicateSecond.PayloadType = 100;
        List<SdpCodec> exhausted = new List<SdpCodec>();
        for (int payloadType = 0; payloadType < 33; payloadType++)
        {
            SdpCodec codec = Codec("codec-" + payloadType, 8000);
            codec.PayloadType = payloadType;
            exhausted.Add(codec);
        }
        SdpCodec invalid = Codec("bad", 8000);
        invalid.PayloadType = 128;
        SdpNegotiationException collision = Assert.Throws<SdpNegotiationException>(() => service.AllocatePayloadTypes(new List<SdpCodec> { duplicateFirst, duplicateSecond }, 0, true))!;
        SdpNegotiationException exhaustion = Assert.Throws<SdpNegotiationException>(() => service.AllocatePayloadTypes(exhausted, 0, false))!;

        Assert.Multiple(() =>
        {
            Assert.That(() => service.AllocatePayloadTypes(null!, 0, true), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("codecs"));
            Assert.That(() => service.AllocatePayloadTypes(new List<SdpCodec>(), -1, true), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.AllocatePayloadTypes(new List<SdpCodec> { invalid }, 0, true), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("codecs"));
            Assert.That(collision.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationPayloadAllocationFailed));
            Assert.That(exhaustion.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationPayloadAllocationFailed));
        });
    }

    [Test]
    public void RemapPayloadTypes_UpdatesCodecAssociationFeedbackRidAndSourceGraphs()
    {
        SdpEffectiveMedia media = new SdpEffectiveMedia { MediaIndex = 2 };
        SdpCodec primary = RemappableCodec("opus", 111);
        SdpFormatParametersAttribute primaryFormat = FormatParameters(111, "stereo=1", Parameter("stereo", true, "1"));
        primary.FormatParameters.Add(primaryFormat);
        primary.RtcpFeedback.Add(Feedback("111", "nack"));
        primary.RtcpFeedback.Add(Feedback("*", "transport-cc"));

        SdpCodec rtx = RemappableCodec("rtx", 112);
        rtx.FormatParameters.Add(FormatParameters(112, "apt=111", Parameter("apt", true, "111")));
        rtx.RtcpFeedback.Add(Feedback("112", "nack"));

        SdpCodec red = RemappableCodec("red", 113);
        red.FormatParameters.Add(FormatParameters(113, "111/112", Parameter("111/112", false, string.Empty)));
        SdpCodec fec = RemappableCodec("ulpfec", 114);
        fec.FormatParameters.Add(FormatParameters(114, "111/113", Parameter("111/113", false, string.Empty)));
        media.Codecs.AddRange(new[] { primary, rtx, red, fec });

        SdpRidAttribute high = new SdpRidAttribute
        {
            SourceAttribute = ValueAttribute("rid", "high send pt=111,112;max-width=1280"),
            Id = "high",
            Direction = SdpDirection.SendOnly
        };
        high.PayloadTypes.AddRange(new[] { 111, 112 });
        SdpRidAttribute low = new SdpRidAttribute { Id = "low", Direction = SdpDirection.SendOnly };
        low.PayloadTypes.Add(113);
        media.Rids.AddRange(new[] { high, low });
        SdpSimulcastAlternative highAlternative = new SdpSimulcastAlternative { RidId = "high" };
        SdpSimulcastAlternative lowAlternative = new SdpSimulcastAlternative { RidId = "low", Paused = true };
        SdpSimulcastStream stream = new SdpSimulcastStream();
        stream.Alternatives.AddRange(new[] { highAlternative, lowAlternative });
        media.Simulcast = new SdpSimulcastAttribute();
        media.Simulcast.SendStreams.Add(stream);
        List<SdpPayloadMapping> mappings = PayloadMappings(2, 111, 96, 112, 97, 113, 98, 114, 99);

        new SdpCodecService().RemapPayloadTypes(media, mappings);

        Assert.Multiple(() =>
        {
            Assert.That(media.Codecs.Select(x => x.PayloadType), Is.EqualTo(new[] { 96, 97, 98, 99 }));
            Assert.That(media.Codecs.Select(x => x.RtpMap!.PayloadType), Is.EqualTo(new[] { 96, 97, 98, 99 }));
            Assert.That(media.Codecs.Select(x => x.RtpMap!.SourceAttribute!.Value), Is.EqualTo(new[] { "96 opus/90000", "97 rtx/90000", "98 red/90000", "99 ulpfec/90000" }));
            Assert.That(primary.FormatParameters[0].PayloadType, Is.EqualTo(96));
            Assert.That(rtx.FormatParameters[0].Parameters[0].Value, Is.EqualTo("96"));
            Assert.That(rtx.FormatParameters[0].RawParameters, Is.EqualTo("apt=96"));
            Assert.That(rtx.FormatParameters[0].SourceAttribute!.Value, Is.EqualTo("97 apt=96"));
            Assert.That(red.FormatParameters[0].RawParameters, Is.EqualTo("96/97"));
            Assert.That(red.FormatParameters[0].Parameters[0].Name, Is.EqualTo("96/97"));
            Assert.That(fec.FormatParameters[0].RawParameters, Is.EqualTo("96/98"));
            Assert.That(primary.RtcpFeedback.Select(x => x.PayloadType), Is.EqualTo(new[] { "96", "*" }));
            Assert.That(primary.RtcpFeedback[0].SourceAttribute!.Value, Is.EqualTo("96 nack"));
            Assert.That(rtx.RtcpFeedback[0].PayloadType, Is.EqualTo("97"));
            Assert.That(high.PayloadTypes, Is.EqualTo(new[] { 96, 97 }));
            Assert.That(high.SourceAttribute!.Value, Is.EqualTo("high send pt=96,97;max-width=1280"));
            Assert.That(low.PayloadTypes, Is.EqualTo(new[] { 98 }));
            Assert.That(stream.Alternatives, Is.EqualTo(new[] { highAlternative, lowAlternative }));
            Assert.That(stream.Alternatives.Select(x => x.RidId), Is.EqualTo(new[] { "high", "low" }));
        });
    }

    [Test]
    public void RemapPayloadTypes_ValidatesBeforeMutation()
    {
        SdpEffectiveMedia media = new SdpEffectiveMedia { MediaIndex = 1 };
        SdpCodec primary = RemappableCodec("opus", 111);
        SdpCodec rtx = RemappableCodec("rtx", 112);
        rtx.FormatParameters.Add(FormatParameters(112, "apt=111", Parameter("apt", true, "111")));
        media.Codecs.AddRange(new[] { primary, rtx });
        List<SdpPayloadMapping> colliding = PayloadMappings(1, 111, 96, 112, 96);
        List<SdpPayloadMapping> incomplete = PayloadMappings(1, 111, 96);
        List<SdpPayloadMapping> wrongMedia = PayloadMappings(9, 111, 96, 112, 97);

        Assert.Multiple(() =>
        {
            Assert.That(() => new SdpCodecService().RemapPayloadTypes(null!, colliding), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("media"));
            Assert.That(() => new SdpCodecService().RemapPayloadTypes(media, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(() => new SdpCodecService().RemapPayloadTypes(media, colliding), Throws.ArgumentException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(() => new SdpCodecService().RemapPayloadTypes(media, incomplete), Throws.ArgumentException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(() => new SdpCodecService().RemapPayloadTypes(media, wrongMedia), Throws.ArgumentException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(media.Codecs.Select(x => x.PayloadType), Is.EqualTo(new[] { 111, 112 }));
            Assert.That(rtx.FormatParameters[0].Parameters[0].Value, Is.EqualTo("111"));
        });
    }

    [Test]
    public void RemapPayloadTypes_RejectsEveryDanglingAssociatedPayloadBeforeMutation()
    {
        SdpEffectiveMedia rtxMedia = new SdpEffectiveMedia { MediaIndex = 0 };
        SdpCodec rtx = RemappableCodec("rtx", 112);
        rtx.FormatParameters.Add(FormatParameters(112, "apt=111", Parameter("apt", true, "111")));
        rtxMedia.Codecs.Add(rtx);

        SdpEffectiveMedia redMedia = new SdpEffectiveMedia { MediaIndex = 0 };
        SdpCodec red = RemappableCodec("red", 113);
        red.FormatParameters.Add(FormatParameters(113, "111/112", Parameter("111/112", false, string.Empty)));
        redMedia.Codecs.Add(red);

        SdpEffectiveMedia feedbackMedia = new SdpEffectiveMedia { MediaIndex = 0 };
        SdpCodec feedbackCodec = RemappableCodec("opus", 111);
        feedbackCodec.RtcpFeedback.Add(Feedback("112", "nack"));
        feedbackMedia.Codecs.Add(feedbackCodec);

        SdpEffectiveMedia ridMedia = new SdpEffectiveMedia { MediaIndex = 0 };
        SdpCodec ridCodec = RemappableCodec("VP8", 111);
        SdpRidAttribute rid = new SdpRidAttribute { Id = "high", Direction = SdpDirection.SendOnly };
        rid.PayloadTypes.Add(112);
        ridMedia.Codecs.Add(ridCodec);
        ridMedia.Rids.Add(rid);

        Assert.Multiple(() =>
        {
            Assert.That(() => new SdpCodecService().RemapPayloadTypes(rtxMedia, PayloadMappings(0, 112, 97)), Throws.ArgumentException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(() => new SdpCodecService().RemapPayloadTypes(redMedia, PayloadMappings(0, 113, 98)), Throws.ArgumentException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(() => new SdpCodecService().RemapPayloadTypes(feedbackMedia, PayloadMappings(0, 111, 96)), Throws.ArgumentException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(() => new SdpCodecService().RemapPayloadTypes(ridMedia, PayloadMappings(0, 111, 96)), Throws.ArgumentException.With.Property("ParamName").EqualTo("mappings"));
            Assert.That(rtx.PayloadType, Is.EqualTo(112));
            Assert.That(rtx.FormatParameters[0].Parameters[0].Value, Is.EqualTo("111"));
            Assert.That(red.FormatParameters[0].RawParameters, Is.EqualTo("111/112"));
            Assert.That(feedbackCodec.RtcpFeedback[0].PayloadType, Is.EqualTo("112"));
            Assert.That(rid.PayloadTypes, Is.EqualTo(new[] { 112 }));
        });
    }

    [Test]
    public void RemapPayloadTypes_RejectsDanglingSimulcastReferencesWithoutMutation()
    {
        SdpEffectiveMedia media = new SdpEffectiveMedia { MediaIndex = 0 };
        SdpCodec codec = RemappableCodec("VP8", 100);
        codec.MediaKind = SdpMediaKind.Video;
        codec.MediaType = "video";
        media.Codecs.Add(codec);
        SdpSimulcastStream stream = new SdpSimulcastStream();
        stream.Alternatives.Add(new SdpSimulcastAlternative { RidId = "missing" });
        media.Simulcast = new SdpSimulcastAttribute();
        media.Simulcast.SendStreams.Add(stream);

        Assert.That(() => new SdpCodecService().RemapPayloadTypes(media, PayloadMappings(0, 100, 96)), Throws.ArgumentException.With.Property("ParamName").EqualTo("media"));
        Assert.That(codec.PayloadType, Is.EqualTo(100));
    }

    private static SdpCodec Codec(string encodingName, int clockRate)
    {
        return new SdpCodec
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = encodingName,
            ClockRate = clockRate,
            ChannelCount = 1
        };
    }

    private static SdpCodecCapability Capability(string encodingName, int clockRate)
    {
        return new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = encodingName,
            ClockRate = clockRate,
            ChannelCount = 1
        };
    }

    private static SdpCodecCapability CapabilityWithPreference(string encodingName, int clockRate, int preference)
    {
        SdpCodecCapability capability = Capability(encodingName, clockRate);
        capability.Preference = preference;
        return capability;
    }

    private static SdpFormatParameter Parameter(string name, bool hasValue, string value)
    {
        return new SdpFormatParameter
        {
            Name = name,
            HasValue = hasValue,
            Value = value
        };
    }

    private static SdpCodec RemappableCodec(string encodingName, int payloadType)
    {
        SdpAttribute source = ValueAttribute("rtpmap", payloadType + " " + encodingName + "/90000");
        return new SdpCodec
        {
            PayloadType = payloadType,
            MediaKind = SdpMediaKind.Video,
            MediaType = "video",
            EncodingName = encodingName,
            ClockRate = 90000,
            ChannelCount = 1,
            RtpMap = new SdpRtpMapAttribute
            {
                SourceAttribute = source,
                PayloadType = payloadType,
                EncodingName = encodingName,
                ClockRate = 90000,
                ChannelCount = 1
            }
        };
    }

    private static SdpFormatParametersAttribute FormatParameters(int payloadType, string rawParameters, SdpFormatParameter parameter)
    {
        SdpFormatParametersAttribute format = new SdpFormatParametersAttribute
        {
            SourceAttribute = ValueAttribute("fmtp", payloadType + " " + rawParameters),
            PayloadType = payloadType,
            RawParameters = rawParameters
        };
        format.Parameters.Add(parameter);
        return format;
    }

    private static SdpRtcpFeedbackAttribute Feedback(string payloadType, string type)
    {
        return new SdpRtcpFeedbackAttribute
        {
            SourceAttribute = ValueAttribute("rtcp-fb", payloadType + " " + type),
            PayloadType = payloadType,
            Type = type
        };
    }

    private static SdpCommonAttribute PacketTime(string name, double value)
    {
        return new SdpCommonAttribute
        {
            Name = name,
            HasNumericValue = true,
            NumericValue = value
        };
    }

    private static List<SdpPayloadMapping> PayloadMappings(int mediaIndex, params int[] payloadPairs)
    {
        List<SdpPayloadMapping> mappings = new List<SdpPayloadMapping>();
        for (int index = 0; index < payloadPairs.Length; index += 2)
        {
            mappings.Add(new SdpPayloadMapping
            {
                MediaIndex = mediaIndex,
                OfferedPayloadType = payloadPairs[index],
                NegotiatedPayloadType = payloadPairs[index + 1]
            });
        }
        return mappings;
    }

    private static SdpSessionDescription DescriptionWithMedia(string protocol, params string[] formats)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "audio",
            MediaKind = SdpMediaKind.Audio,
            Protocol = protocol
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
            RawText = "a=" + name + ":" + value
        };
    }
}
