using System.Collections.Generic;

namespace sdp.Tests.Api;

public class SdpPublicDataCollectionTests
{
    [Test]
    public void EveryMutableCollection_IsInitializedEmptyAndPerInstance()
    {
        AssertIndependent(new SdpAttributeRegistration().Scopes, new SdpAttributeRegistration().Scopes);
        AssertIndependent(new SdpAttributeRegistration().References, new SdpAttributeRegistration().References);
        AssertIndependent(new SdpRegisteredToken().References, new SdpRegisteredToken().References);
        AssertIndependent(new SdpBfcpDescription().FloorIds, new SdpBfcpDescription().FloorIds);
        AssertIndependent(new SdpBfcpDescription().Versions, new SdpBfcpDescription().Versions);
        AssertIndependent(new SdpBufferWriteResult().Diagnostics, new SdpBufferWriteResult().Diagnostics);
        AssertIndependent(new SdpBundleGroup().MediaIds, new SdpBundleGroup().MediaIds);
        AssertIndependent(new SdpBundleTransport().HeaderExtensions, new SdpBundleTransport().HeaderExtensions);
        AssertIndependent(new SdpBundleTransport().SharedAttributes, new SdpBundleTransport().SharedAttributes);
        AssertIndependent(new SdpBundleTransport().MediaAttributes, new SdpBundleTransport().MediaAttributes);
        AssertIndependent(new SdpBundleTransport().Bandwidths, new SdpBundleTransport().Bandwidths);
        AssertIndependent(new SdpCodec().FormatParameters, new SdpCodec().FormatParameters);
        AssertIndependent(new SdpCodec().RtcpFeedback, new SdpCodec().RtcpFeedback);
        AssertIndependent(new SdpCodecCapability().FormatParameters, new SdpCodecCapability().FormatParameters);
        AssertIndependent(new SdpCodecCapability().RtcpFeedbackTypes, new SdpCodecCapability().RtcpFeedbackTypes);
        AssertIndependent(new SdpCodecCapability().RtcpFeedback, new SdpCodecCapability().RtcpFeedback);
        AssertIndependent(new SdpComparisonOptions().IgnoredAttributeNames, new SdpComparisonOptions().IgnoredAttributeNames);
        AssertIndependent(new SdpComparisonResult().Differences, new SdpComparisonResult().Differences);
        AssertIndependent(new SdpCryptoAttribute().KeyParameters, new SdpCryptoAttribute().KeyParameters);
        AssertIndependent(new SdpCryptoAttribute().SessionParameters, new SdpCryptoAttribute().SessionParameters);
        AssertIndependent(new SdpDataChannelAttribute().Parameters, new SdpDataChannelAttribute().Parameters);
        AssertIndependent(new SdpDataChannelAttribute().SubprotocolAttributes, new SdpDataChannelAttribute().SubprotocolAttributes);
        AssertIndependent(new SdpDtlsDescription().Fingerprints, new SdpDtlsDescription().Fingerprints);
        AssertIndependent(new SdpDtlsDescription().CryptoAttributes, new SdpDtlsDescription().CryptoAttributes);
        AssertIndependent(new SdpEffectiveMedia().Bandwidths, new SdpEffectiveMedia().Bandwidths);
        AssertIndependent(new SdpEffectiveMedia().Languages, new SdpEffectiveMedia().Languages);
        AssertIndependent(new SdpEffectiveMedia().SessionDescriptionLanguages, new SdpEffectiveMedia().SessionDescriptionLanguages);
        AssertIndependent(new SdpEffectiveMedia().Codecs, new SdpEffectiveMedia().Codecs);
        AssertIndependent(new SdpEffectiveMedia().RtcpExtendedReports, new SdpEffectiveMedia().RtcpExtendedReports);
        AssertIndependent(new SdpEffectiveMedia().SourceAttributes, new SdpEffectiveMedia().SourceAttributes);
        AssertIndependent(new SdpEffectiveMedia().SourceGroups, new SdpEffectiveMedia().SourceGroups);
        AssertIndependent(new SdpEffectiveMedia().MediaStreamIds, new SdpEffectiveMedia().MediaStreamIds);
        AssertIndependent(new SdpEffectiveMedia().SourceFilters, new SdpEffectiveMedia().SourceFilters);
        AssertIndependent(new SdpEffectiveMedia().HeaderExtensions, new SdpEffectiveMedia().HeaderExtensions);
        AssertIndependent(new SdpEffectiveMedia().Rids, new SdpEffectiveMedia().Rids);
        AssertIndependent(new SdpEffectiveMedia().ImageAttributes, new SdpEffectiveMedia().ImageAttributes);
        AssertIndependent(new SdpEffectiveMedia().ClockReferences, new SdpEffectiveMedia().ClockReferences);
        AssertIndependent(new SdpEffectiveMedia().MediaClocks, new SdpEffectiveMedia().MediaClocks);
        AssertIndependent(new SdpEffectiveMedia().DataChannels, new SdpEffectiveMedia().DataChannels);
        AssertIndependent(new SdpEndpointCapabilities().Media, new SdpEndpointCapabilities().Media);
        AssertIndependent(new SdpEndpointCapabilities().HeaderExtensions, new SdpEndpointCapabilities().HeaderExtensions);
        AssertIndependent(new SdpEndpointCapabilities().Fingerprints, new SdpEndpointCapabilities().Fingerprints);
        AssertIndependent(new SdpFingerprintAttribute().Digest, new SdpFingerprintAttribute().Digest);
        AssertIndependent(new SdpFormatParametersAttribute().Parameters, new SdpFormatParametersAttribute().Parameters);
        AssertIndependent(new SdpMediaStreamSemanticAttribute().StreamIds, new SdpMediaStreamSemanticAttribute().StreamIds);
        AssertIndependent(new SdpGroupAttribute().IdentificationTags, new SdpGroupAttribute().IdentificationTags);
        AssertIndependent(new SdpIceCandidate().Extensions, new SdpIceCandidate().Extensions);
        AssertIndependent(new SdpIceDescription().Options, new SdpIceDescription().Options);
        AssertIndependent(new SdpIceDescription().Candidates, new SdpIceDescription().Candidates);
        AssertIndependent(new SdpIceDescription().RemoteCandidates, new SdpIceDescription().RemoteCandidates);
        AssertIndependent(new SdpMediaCapability().Protocols, new SdpMediaCapability().Protocols);
        AssertIndependent(new SdpMediaCapability().Formats, new SdpMediaCapability().Formats);
        AssertIndependent(new SdpMediaCapability().Codecs, new SdpMediaCapability().Codecs);
        AssertIndependent(new SdpMediaCapability().HeaderExtensions, new SdpMediaCapability().HeaderExtensions);
        AssertIndependent(new SdpMediaCapability().MediaStreamIds, new SdpMediaCapability().MediaStreamIds);
        AssertIndependent(new SdpMediaCapability().SourceAttributes, new SdpMediaCapability().SourceAttributes);
        AssertIndependent(new SdpMediaCapability().SourceGroups, new SdpMediaCapability().SourceGroups);
        AssertIndependent(new SdpMediaCapability().Rids, new SdpMediaCapability().Rids);
        AssertIndependent(new SdpMediaCapability().ImageAttributes, new SdpMediaCapability().ImageAttributes);
        AssertIndependent(new SdpMediaCapability().DataChannels, new SdpMediaCapability().DataChannels);
        AssertIndependent(new SdpMediaDescription().Formats, new SdpMediaDescription().Formats);
        AssertIndependent(new SdpMediaDescription().Connections, new SdpMediaDescription().Connections);
        AssertIndependent(new SdpMediaDescription().Bandwidths, new SdpMediaDescription().Bandwidths);
        AssertIndependent(new SdpMediaDescription().Attributes, new SdpMediaDescription().Attributes);
        AssertIndependent(new SdpMediaDescription().RawLines, new SdpMediaDescription().RawLines);
        AssertIndependent(new SdpMediaNegotiation().Codecs, new SdpMediaNegotiation().Codecs);
        AssertIndependent(new SdpMediaNegotiation().PayloadMappings, new SdpMediaNegotiation().PayloadMappings);
        AssertIndependent(new SdpMediaNegotiation().HeaderExtensionMappings, new SdpMediaNegotiation().HeaderExtensionMappings);
        AssertIndependent(new SdpNegotiationResult().Media, new SdpNegotiationResult().Media);
        AssertIndependent(new SdpNegotiationResult().Rejections, new SdpNegotiationResult().Rejections);
        AssertIndependent(new SdpNegotiationResult().BundleGroups, new SdpNegotiationResult().BundleGroups);
        AssertIndependent(new SdpNegotiationResult().BundleTransports, new SdpNegotiationResult().BundleTransports);
        AssertIndependent(new SdpNegotiationResult().Diagnostics, new SdpNegotiationResult().Diagnostics);
        AssertIndependent(new SdpParseResult().Diagnostics, new SdpParseResult().Diagnostics);
        AssertIndependent(new SdpRepeatTime().Offsets, new SdpRepeatTime().Offsets);
        AssertIndependent(new SdpRidAttribute().PayloadTypes, new SdpRidAttribute().PayloadTypes);
        AssertIndependent(new SdpRidAttribute().Restrictions, new SdpRidAttribute().Restrictions);
        AssertIndependent(new SdpRtcpExtendedReportAttribute().Blocks, new SdpRtcpExtendedReportAttribute().Blocks);
        AssertIndependent(new SdpSessionDescription().Emails, new SdpSessionDescription().Emails);
        AssertIndependent(new SdpSessionDescription().Phones, new SdpSessionDescription().Phones);
        AssertIndependent(new SdpSessionDescription().Bandwidths, new SdpSessionDescription().Bandwidths);
        AssertIndependent(new SdpSessionDescription().Timings, new SdpSessionDescription().Timings);
        AssertIndependent(new SdpSessionDescription().TimeZoneAdjustments, new SdpSessionDescription().TimeZoneAdjustments);
        AssertIndependent(new SdpSessionDescription().Attributes, new SdpSessionDescription().Attributes);
        AssertIndependent(new SdpSessionDescription().MediaDescriptions, new SdpSessionDescription().MediaDescriptions);
        AssertIndependent(new SdpSessionDescription().RawLines, new SdpSessionDescription().RawLines);
        AssertIndependent(new SdpSimulcastAttribute().SendStreams, new SdpSimulcastAttribute().SendStreams);
        AssertIndependent(new SdpSimulcastAttribute().ReceiveStreams, new SdpSimulcastAttribute().ReceiveStreams);
        AssertIndependent(new SdpSimulcastStream().Alternatives, new SdpSimulcastStream().Alternatives);
        AssertIndependent(new SdpSourceFilterAttribute().SourceAddresses, new SdpSourceFilterAttribute().SourceAddresses);
        AssertIndependent(new SdpSourceGroupAttribute().SynchronizationSources, new SdpSourceGroupAttribute().SynchronizationSources);
        AssertIndependent(new SdpTiming().RepeatTimes, new SdpTiming().RepeatTimes);
        AssertIndependent(new SdpValidationOptions().SuppressedDiagnosticCodes, new SdpValidationOptions().SuppressedDiagnosticCodes);
        AssertIndependent(new SdpValidationOptions().PromotedDiagnosticCodes, new SdpValidationOptions().PromotedDiagnosticCodes);
        AssertIndependent(new SdpValidationResult().Issues, new SdpValidationResult().Issues);
        AssertIndependent(new SdpWriteResult().Utf8Bytes, new SdpWriteResult().Utf8Bytes);
        AssertIndependent(new SdpWriteResult().Diagnostics, new SdpWriteResult().Diagnostics);
    }

    static void AssertIndependent<T>(ICollection<T> first, ICollection<T> second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Empty);
            Assert.That(second, Is.Empty);
            Assert.That(first, Is.Not.SameAs(second));
        });
    }
}
