using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpOfferAnswerTransportCore
{
    internal static SdpRejectionReason SelectRejectionReason(SdpSessionDescription offer, int mediaIndex, int transportMediaIndex, SdpMediaCapability capability, SdpEndpointCapabilities endpoint, SdpNegotiationOptions options)
    {
        SdpMediaDescription offeredMedia = offer.MediaDescriptions[mediaIndex];
        SdpMediaDescription offeredTransportMedia = offer.MediaDescriptions[transportMediaIndex];
        bool rtp = SdpRtpValidationCore.IsRtpProtocol(offeredMedia.Protocol);
        if (rtp && !CanNegotiateRtcpMux(offeredTransportMedia, capability, options))
        {
            return SdpRejectionReason.RtcpMuxRequired;
        }
        bool offeredIce = SdpBundleServiceCore.HasIceSignaling(offer, offeredTransportMedia);
        SdpIceDescription? localIce = ReadLocalIce(capability, endpoint);
        if (options.RequireIce && (!offeredIce || !endpoint.SupportsIce || !HasUsableOfferedIce(offer, transportMediaIndex) || !HasUsableLocalIce(localIce)))
        {
            return SdpRejectionReason.IceRequirementsNotMet;
        }
        if (offeredIce && endpoint.SupportsIce)
        {
            SdpIceDescription offeredDescription = SdpIceServiceCore.ResolveMediaIce(offer, transportMediaIndex);
            if (!HasUsableOfferedIce(offer, transportMediaIndex) || !HasUsableLocalIce(localIce) || localIce != null && localIce.IceLite && offeredDescription.IceLite)
            {
                return SdpRejectionReason.IceRequirementsNotMet;
            }
        }
        bool secure = SdpDtlsValidationCore.IsTlsTransport(offeredMedia.Protocol);
        if (options.RequireDtls && !secure)
        {
            return SdpRejectionReason.DtlsRequirementsNotMet;
        }
        if (secure && (!endpoint.SupportsDtls || CreateAnswerDtls(offer, mediaIndex, transportMediaIndex, capability, endpoint) == null))
        {
            return SdpRejectionReason.DtlsRequirementsNotMet;
        }
        if (rtp && !CanNegotiateRequiredExtMaps(offer, mediaIndex, capability, endpoint, options))
        {
            return SdpRejectionReason.RejectedByPolicy;
        }
        return SdpRejectionReason.Unknown;
    }

    internal static bool CanNegotiateRtcpMux(SdpMediaDescription offeredMedia, SdpMediaCapability capability, SdpNegotiationOptions options)
    {
        bool offeredMux = SdpRtcpValidationCore.ContainsValidAttribute(offeredMedia.Attributes, SdpAttributeNames.RtcpMux);
        bool offeredMuxOnly = SdpRtcpValidationCore.ContainsValidAttribute(offeredMedia.Attributes, SdpAttributeNames.RtcpMuxOnly);
        bool localMux = capability.SupportsRtcpMux || capability.SupportsRtcpMuxOnly;
        if (offeredMuxOnly && !localMux)
        {
            return false;
        }
        if (capability.SupportsRtcpMuxOnly && !offeredMux)
        {
            return false;
        }
        return !options.RequireRtcpMux || offeredMux && localMux;
    }

    internal static SdpIceDescription? ReadLocalIce(SdpMediaCapability capability, SdpEndpointCapabilities endpoint)
    {
        return capability.Ice == null ? endpoint.Ice : capability.Ice;
    }

    internal static bool HasUsableOfferedIce(SdpSessionDescription offer, int mediaIndex)
    {
        SdpIceDescription offered = SdpIceServiceCore.ResolveMediaIce(offer, mediaIndex);
        SdpIceCredentials? credentials = offered.Credentials;
        return credentials != null
            && SdpIceAttributeCodecCore.IsIceCredentialValue(credentials.UsernameFragment, 4, 256)
            && SdpIceAttributeCodecCore.IsIceCredentialValue(credentials.Password, 22, 256);
    }

    internal static bool HasUsableLocalIce(SdpIceDescription? ice)
    {
        SdpIceCredentials? credentials = ice == null ? null : ice.Credentials;
        return credentials != null
            && SdpIceAttributeCodecCore.IsIceCredentialValue(credentials.UsernameFragment, 4, 32)
            && SdpIceAttributeCodecCore.IsIceCredentialValue(credentials.Password, 22, 256);
    }

    internal static SdpDtlsDescription? ReadLocalDtls(SdpMediaCapability capability, SdpEndpointCapabilities endpoint)
    {
        return capability.Dtls == null ? endpoint.Dtls : capability.Dtls;
    }

    internal static SdpDtlsDescription? CreateAnswerDtls(SdpSessionDescription offer, int mediaIndex, int transportMediaIndex, SdpMediaCapability capability, SdpEndpointCapabilities endpoint)
    {
        SdpDtlsDescription offered = SdpDtlsServiceCore.ResolveMediaDtls(offer, transportMediaIndex);
        if (!IsUsableOfferedSetupRole(offered.SetupRole) || offered.Fingerprints.Count == 0)
        {
            return null;
        }
        SdpDtlsDescription? local = ReadLocalDtls(capability, endpoint);
        SdpFingerprintAttribute? fingerprint = SelectLocalFingerprint(offered.Fingerprints, local, endpoint.Fingerprints);
        if (fingerprint == null)
        {
            return null;
        }
        SdpDtlsSetupRole preferredRole = SdpDtlsSetupRole.Active;
        if (local != null && (local.SetupRole == SdpDtlsSetupRole.Active || local.SetupRole == SdpDtlsSetupRole.Passive))
        {
            preferredRole = local.SetupRole;
        }
        SdpDtlsDescription result = new SdpDtlsDescription();
        result.Fingerprints.Add(fingerprint);
        result.SetupRole = SdpDtlsServiceCore.SelectAnswerSetupRole(offered.SetupRole, preferredRole);
        result.SetupRoleToken = ReadSetupRoleToken(result.SetupRole);
        if (offered.TlsId.Length > 0)
        {
            if (local == null || !IsUsableTlsId(local.TlsId))
            {
                return null;
            }
            result.TlsId = local.TlsId;
        }
        if (SdpDtlsValidationCore.IsConnectionOrientedTransport(offer.MediaDescriptions[mediaIndex].Protocol))
        {
            result.ConnectionRole = local == null || local.ConnectionRole.Length == 0 ? "new" : local.ConnectionRole;
        }
        return result;
    }

    internal static bool IsUsableOfferedSetupRole(SdpDtlsSetupRole role)
    {
        return role == SdpDtlsSetupRole.ActPass || role == SdpDtlsSetupRole.Active || role == SdpDtlsSetupRole.Passive;
    }

    internal static string ReadSetupRoleToken(SdpDtlsSetupRole role)
    {
        if (role == SdpDtlsSetupRole.Active)
        {
            return "active";
        }
        if (role == SdpDtlsSetupRole.Passive)
        {
            return "passive";
        }
        return string.Empty;
    }

    internal static SdpFingerprintAttribute? SelectLocalFingerprint(List<SdpFingerprintAttribute> offered, SdpDtlsDescription? local, List<SdpFingerprintAttribute> endpointFingerprints)
    {
        List<SdpFingerprintAttribute> candidates = local != null && local.Fingerprints.Count > 0 ? local.Fingerprints : endpointFingerprints;
        int candidateIndex = 0;
        while (candidateIndex < candidates.Count)
        {
            SdpFingerprintAttribute candidate = candidates[candidateIndex];
            if (IsUsableFingerprint(candidate) && ContainsOfferedFingerprintAlgorithm(offered, candidate.Algorithm))
            {
                return candidate;
            }
            candidateIndex = candidateIndex + 1;
        }
        return null;
    }

    internal static bool ContainsOfferedFingerprintAlgorithm(List<SdpFingerprintAttribute> fingerprints, string algorithm)
    {
        int fingerprintIndex = 0;
        while (fingerprintIndex < fingerprints.Count)
        {
            SdpFingerprintAttribute fingerprint = fingerprints[fingerprintIndex];
            if (IsUsableFingerprint(fingerprint) && string.Equals(fingerprint.Algorithm, algorithm, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            fingerprintIndex = fingerprintIndex + 1;
        }
        return false;
    }

    internal static bool IsUsableFingerprint(SdpFingerprintAttribute fingerprint)
    {
        if (fingerprint == null || SdpDtlsServiceCore.IsProhibitedFingerprintAlgorithm(fingerprint.Algorithm) || fingerprint.Digest == null || fingerprint.Digest.Count == 0)
        {
            return false;
        }
        int expectedLength = SdpDtlsValidationCore.ReadFingerprintLength(fingerprint.Algorithm);
        return expectedLength == 0 || fingerprint.Digest.Count == expectedLength;
    }

    internal static bool IsUsableTlsId(string tlsId)
    {
        if (tlsId.Length == 0)
        {
            return false;
        }
        SdpTlsIdAttribute projection = new SdpTlsIdAttribute();
        projection.Id = tlsId;
        return SdpDtlsAttributeCodecCore.EncodeTlsId(projection).Success;
    }

    internal static List<SdpHeaderExtension> ReadHeaderExtensionCapabilities(SdpMediaCapability capability, SdpEndpointCapabilities endpoint)
    {
        return capability.HeaderExtensions.Count > 0 ? capability.HeaderExtensions : endpoint.HeaderExtensions;
    }

    internal static bool CanNegotiateRequiredExtMaps(SdpSessionDescription offer, int mediaIndex, SdpMediaCapability capability, SdpEndpointCapabilities endpoint, SdpNegotiationOptions options)
    {
        List<SdpHeaderExtension> capabilities = ReadHeaderExtensionCapabilities(capability, endpoint);
        List<SdpExtMapAttribute> offered = SdpExtMapServiceCore.ResolveMediaExtMaps(offer, mediaIndex);
        List<SdpExtMapAttribute> intersection = SdpExtMapServiceCore.IntersectExtMaps(offered, capabilities);
        int mappingIndex = 0;
        while (mappingIndex < intersection.Count)
        {
            if (!options.AllowHeaderExtensionIdRemapping && SdpExtMapServiceCore.IsNegotiationIdentifier(intersection[mappingIndex].Id))
            {
                return false;
            }
            mappingIndex = mappingIndex + 1;
        }
        int capabilityIndex = 0;
        while (capabilityIndex < capabilities.Count)
        {
            SdpHeaderExtension required = capabilities[capabilityIndex];
            if (required.Required && !ContainsNegotiatedExtMap(intersection, required))
            {
                return false;
            }
            capabilityIndex = capabilityIndex + 1;
        }
        return true;
    }

    internal static bool ContainsNegotiatedExtMap(List<SdpExtMapAttribute> mappings, SdpHeaderExtension capability)
    {
        int mappingIndex = 0;
        while (mappingIndex < mappings.Count)
        {
            SdpExtMapAttribute mapping = mappings[mappingIndex];
            bool attributesMatch = capability.ExtensionAttributes.Length == 0 || mapping.ExtensionAttributes == capability.ExtensionAttributes;
            if (mapping.Uri == capability.Uri && attributesMatch && (!mapping.Encrypted || capability.SupportsEncryption))
            {
                return true;
            }
            mappingIndex = mappingIndex + 1;
        }
        return false;
    }

    internal static void ApplyAnswerAttributes(SdpSessionDescription answer, SdpSessionDescription offer, int mediaIndex, int transportMediaIndex, SdpMediaDescription answerMedia, SdpMediaCapability capability, SdpEndpointCapabilities endpoint, SdpNegotiationOptions options, List<SdpHeaderExtensionMapping> extensionMappings)
    {
        SdpMediaDescription offeredMedia = offer.MediaDescriptions[mediaIndex];
        SdpMediaDescription offeredTransportMedia = offer.MediaDescriptions[transportMediaIndex];
        if (SdpRtpValidationCore.IsRtpProtocol(offeredMedia.Protocol))
        {
            ApplyRtcpAttributes(offeredTransportMedia, answerMedia, capability);
        }
        if (endpoint.SupportsIce && SdpBundleServiceCore.HasIceSignaling(offer, offeredTransportMedia))
        {
            SdpIceDescription? localIce = ReadLocalIce(capability, endpoint);
            if (localIce != null)
            {
                ApplyIceAttributes(answer, answerMedia, SdpIceServiceCore.ResolveMediaIce(offer, transportMediaIndex), localIce);
            }
        }
        if (SdpDtlsValidationCore.IsTlsTransport(offeredMedia.Protocol))
        {
            SdpDtlsDescription? dtls = CreateAnswerDtls(offer, mediaIndex, transportMediaIndex, capability, endpoint);
            if (dtls != null)
            {
                SdpSemanticComparisonCore.AddEffectiveDtls(answerMedia.Attributes, dtls);
            }
        }
        if (SdpRtpValidationCore.IsRtpProtocol(offeredMedia.Protocol))
        {
            ApplyExtMapAttributes(offer, mediaIndex, answerMedia, capability, endpoint, extensionMappings);
        }
    }

    internal static void ApplyRtcpAttributes(SdpMediaDescription offeredMedia, SdpMediaDescription answerMedia, SdpMediaCapability capability)
    {
        bool offeredMux = SdpRtcpValidationCore.ContainsValidAttribute(offeredMedia.Attributes, SdpAttributeNames.RtcpMux);
        bool offeredReducedSize = SdpRtcpValidationCore.ContainsValidAttribute(offeredMedia.Attributes, SdpAttributeNames.RtcpReducedSize);
        if (offeredMux && (capability.SupportsRtcpMux || capability.SupportsRtcpMuxOnly))
        {
            answerMedia.Attributes.Add(SdpSemanticComparisonCore.CreatePropertyAttribute(SdpAttributeNames.RtcpMux));
        }
        if (offeredReducedSize && capability.SupportsReducedSizeRtcp)
        {
            answerMedia.Attributes.Add(SdpSemanticComparisonCore.CreatePropertyAttribute(SdpAttributeNames.RtcpReducedSize));
        }
    }

    internal static void ApplyIceAttributes(SdpSessionDescription answer, SdpMediaDescription answerMedia, SdpIceDescription offered, SdpIceDescription local)
    {
        if (local.IceLite && !SdpDtlsServiceCore.ContainsAttribute(answer.Attributes, SdpAttributeNames.IceLite))
        {
            answer.Attributes.Add(SdpSemanticComparisonCore.CreatePropertyAttribute(SdpAttributeNames.IceLite));
        }
        SdpIceDescription signaling = new SdpIceDescription();
        signaling.Credentials = local.Credentials;
        signaling.EndOfCandidates = local.EndOfCandidates;
        int optionIndex = 0;
        while (optionIndex < local.Options.Count)
        {
            if (ContainsIceOption(offered.Options, local.Options[optionIndex]))
            {
                signaling.Options.Add(local.Options[optionIndex]);
            }
            optionIndex = optionIndex + 1;
        }
        int candidateIndex = 0;
        while (candidateIndex < local.Candidates.Count)
        {
            signaling.Candidates.Add(local.Candidates[candidateIndex]);
            candidateIndex = candidateIndex + 1;
        }
        SdpSemanticComparisonCore.AddEffectiveIce(answerMedia.Attributes, signaling, false);
    }

    internal static bool ContainsIceOption(List<string> options, string expected)
    {
        int optionIndex = 0;
        while (optionIndex < options.Count)
        {
            if (string.Equals(options[optionIndex], expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            optionIndex = optionIndex + 1;
        }
        return false;
    }

    internal static void ApplyExtMapAttributes(SdpSessionDescription offer, int mediaIndex, SdpMediaDescription answerMedia, SdpMediaCapability capability, SdpEndpointCapabilities endpoint, List<SdpHeaderExtensionMapping> extensionMappings)
    {
        List<SdpExtMapAttribute> offered = SdpExtMapServiceCore.ResolveMediaExtMaps(offer, mediaIndex);
        List<SdpExtMapAttribute> intersection = SdpExtMapServiceCore.IntersectExtMaps(offered, ReadHeaderExtensionCapabilities(capability, endpoint));
        List<SdpHeaderExtensionMapping> mappings = SdpExtMapServiceCore.AllocateExtMapIds(intersection);
        int mappingIndex = 0;
        while (mappingIndex < mappings.Count)
        {
            SdpHeaderExtensionMapping mapping = mappings[mappingIndex];
            SdpExtMapAttribute extension = new SdpExtMapAttribute();
            extension.Id = mapping.NegotiatedId;
            extension.HasDirection = mapping.Direction != SdpDirection.SendReceive;
            extension.Direction = mapping.Direction;
            extension.Encrypted = mapping.Encrypted;
            extension.Uri = mapping.Uri;
            extension.ExtensionAttributes = mapping.ExtensionAttributes;
            SdpSemanticComparisonCore.AddEncodedAttribute(answerMedia.Attributes, extension);
            extensionMappings.Add(mapping);
            mappingIndex = mappingIndex + 1;
        }
    }
}
