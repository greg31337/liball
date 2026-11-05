using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpDtlsValidationCore
{
    internal static void ValidateDtlsDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        ValidateTlsIdSessionScope(description.Attributes, result, limiter);
        ValidateDtlsSingletons(description.Attributes, SdpScope.Session, -1, result, limiter);
        ValidateDtlsAttributeValues(description.Attributes, SdpScope.Session, -1, result, limiter);

        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            ValidateDtlsSingletons(media.Attributes, SdpScope.Media, mediaIndex, result, limiter);
            ValidateDtlsAttributeValues(media.Attributes, SdpScope.Media, mediaIndex, result, limiter);
            ValidateMediaTransport(description, media, mediaIndex, result, limiter);
            mediaIndex = mediaIndex + 1;
        }
        ValidateSessionAttributeApplicability(description, result, limiter);
        ValidateTlsIdUniqueness(description, result, limiter);
        ValidateAnswerTlsIdContinuity(description, result, limiter);
        ValidateBundleConsistency(description, result, limiter);
    }

    internal static void ValidateTlsIdSessionScope(List<SdpAttribute> attributes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.TlsId, StringComparison.OrdinalIgnoreCase))
            {
                int occurrenceIndex = ReadOccurrenceIndex(attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Session, attribute.Name, -1, occurrenceIndex, "The tls-id attribute is permitted only at media scope.", attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateDtlsSingletons(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (IsSingletonDtlsAttributeName(attribute.Name))
            {
                int occurrenceIndex = ReadOccurrenceIndex(attributes, index);
                if (occurrenceIndex > 0)
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, "The TLS or DTLS attribute may appear at most once in this scope.", attribute.Location);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateDtlsAttributeValues(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (IsBaseDtlsAttributeName(attribute.Name))
            {
                int occurrenceIndex = ReadOccurrenceIndex(attributes, index);
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success)
                {
                    AddDtlsIssue(result, limiter, scope, attribute.Name, mediaIndex, occurrenceIndex, ReadDecodeFailureDetail(decoded), attribute.Location);
                }
                else if (decoded.SetupAttribute != null)
                {
                    ValidateSetupRole(decoded.SetupAttribute, scope, mediaIndex, occurrenceIndex, attribute.Location, result, limiter);
                }
                else if (decoded.ConnectionAttribute != null)
                {
                    ValidateConnectionRole(decoded.ConnectionAttribute, scope, mediaIndex, occurrenceIndex, attribute.Location, result, limiter);
                }
                else if (decoded.FingerprintAttribute != null)
                {
                    ValidateFingerprint(decoded.FingerprintAttribute, scope, mediaIndex, occurrenceIndex, attribute.Location, result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateSetupRole(SdpSetupAttribute setup, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpSourceLocation location, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (setup.Role == SdpDtlsSetupRole.Unknown)
        {
            AddDtlsIssue(result, limiter, scope, SdpAttributeNames.Setup, mediaIndex, occurrenceIndex, "The setup attribute must use a registered connection setup role.", location);
        }
    }

    internal static void ValidateConnectionRole(SdpConnectionAttribute connection, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpSourceLocation location, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool known = string.Equals(connection.ConnectionRole, "new", StringComparison.OrdinalIgnoreCase) || string.Equals(connection.ConnectionRole, "existing", StringComparison.OrdinalIgnoreCase);
        if (!known)
        {
            AddDtlsIssue(result, limiter, scope, SdpAttributeNames.Connection, mediaIndex, occurrenceIndex, "The connection attribute must use the registered new or existing value.", location);
        }
    }

    internal static void ValidateFingerprint(SdpFingerprintAttribute fingerprint, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpSourceLocation location, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (string.Equals(fingerprint.Algorithm, "md2", StringComparison.OrdinalIgnoreCase) || string.Equals(fingerprint.Algorithm, "md5", StringComparison.OrdinalIgnoreCase))
        {
            AddDtlsIssue(result, limiter, scope, SdpAttributeNames.Fingerprint, mediaIndex, occurrenceIndex, "RFC 8122 prohibits MD2 and MD5 certificate fingerprints.", location);
            return;
        }

        int expectedLength = ReadFingerprintLength(fingerprint.Algorithm);
        if (expectedLength > 0 && fingerprint.Digest.Count != expectedLength)
        {
            AddDtlsIssue(result, limiter, scope, SdpAttributeNames.Fingerprint, mediaIndex, occurrenceIndex, "The certificate fingerprint length does not match its hash algorithm.", location);
        }
    }

    internal static int ReadFingerprintLength(string algorithm)
    {
        if (string.Equals(algorithm, SdpFingerprintAlgorithms.Sha1, StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }
        if (string.Equals(algorithm, SdpFingerprintAlgorithms.Sha224, StringComparison.OrdinalIgnoreCase))
        {
            return 28;
        }
        if (string.Equals(algorithm, SdpFingerprintAlgorithms.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            return 32;
        }
        if (string.Equals(algorithm, SdpFingerprintAlgorithms.Sha384, StringComparison.OrdinalIgnoreCase))
        {
            return 48;
        }
        if (string.Equals(algorithm, SdpFingerprintAlgorithms.Sha512, StringComparison.OrdinalIgnoreCase))
        {
            return 64;
        }
        return 0;
    }

    internal static void ValidateMediaTransport(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool connectionOriented = IsConnectionOrientedTransport(media.Protocol);
        bool secure = IsTlsTransport(media.Protocol);
        bool dtls = IsDtlsTransport(media.Protocol);
        int transportMediaIndex = SdpDtlsServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        SdpMediaDescription transportMedia = description.MediaDescriptions[transportMediaIndex];
        ValidateMediaAttributeApplicability(media, mediaIndex, SdpAttributeNames.Setup, connectionOriented || secure, "The setup attribute requires a connection-oriented TLS or DTLS-capable media transport.", result, limiter);
        ValidateMediaAttributeApplicability(media, mediaIndex, SdpAttributeNames.Connection, connectionOriented, "The connection attribute can request transport reuse only for connection-oriented media.", result, limiter);
        ValidateMediaAttributeApplicability(media, mediaIndex, SdpAttributeNames.Fingerprint, secure, "The fingerprint attribute requires a TLS- or DTLS-capable media transport.", result, limiter);
        ValidateMediaAttributeApplicability(media, mediaIndex, SdpAttributeNames.TlsId, secure, "The tls-id attribute requires a TLS- or DTLS-capable media transport.", result, limiter);

        if (secure && media.Port != 0 && !HasEffectiveFingerprint(description.Attributes, transportMedia.Attributes))
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationDtls, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.Fingerprint, mediaIndex, -1, "The secure media description does not have an effective certificate fingerprint.", media.Location);
        }
        if (dtls && media.Port != 0)
        {
            ValidateEffectiveDtlsSetup(description.Attributes, transportMedia, mediaIndex, result, limiter);
            if (result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Offerer && !ContainsAttribute(transportMedia.Attributes, SdpAttributeNames.TlsId))
            {
                AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.TlsId, mediaIndex, -1, "A DTLS offer must include a unique media-level tls-id attribute.", media.Location);
            }
        }
        if (dtls && IsRtpTransport(media.Protocol) && !IsDtlsSrtpTransport(media.Protocol))
        {
            AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.Setup, mediaIndex, -1, "DTLS-SRTP media must use the UDP/TLS/RTP/SAVP or UDP/TLS/RTP/SAVPF transport profile.", media.Location);
        }
        bool tlsConnection = connectionOriented && secure && !dtls;
        if (tlsConnection && ContainsAttribute(media.Attributes, SdpAttributeNames.TlsId) && !HasEffectiveAttribute(description.Attributes, media.Attributes, SdpAttributeNames.Connection))
        {
            AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.Connection, mediaIndex, -1, "A TLS connection that uses tls-id must explicitly include an effective connection attribute.", media.Location);
        }
    }

    internal static void ValidateEffectiveDtlsSetup(List<SdpAttribute> sessionAttributes, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!HasEffectiveAttribute(sessionAttributes, media.Attributes, SdpAttributeNames.Setup))
        {
            AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.Setup, mediaIndex, -1, "The DTLS media description does not have an effective setup attribute.", media.Location);
            return;
        }

        SdpSetupAttribute? setup = ReadEffectiveSetup(sessionAttributes, media.Attributes);
        if (setup == null)
        {
            return;
        }
        if (setup.Role == SdpDtlsSetupRole.HoldConnection)
        {
            AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.Setup, mediaIndex, -1, "A DTLS association must not use the holdconn setup role.", ReadSetupLocation(setup, media));
            return;
        }
        SdpNegotiationRole negotiationRole = result.AppliedOptions.NegotiationRole;
        if (negotiationRole == SdpNegotiationRole.Offerer && setup.Role != SdpDtlsSetupRole.ActPass)
        {
            AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.Setup, mediaIndex, -1, "A DTLS offer must use the actpass setup role.", ReadSetupLocation(setup, media));
        }
        else if (negotiationRole == SdpNegotiationRole.Answerer && setup.Role != SdpDtlsSetupRole.Active && setup.Role != SdpDtlsSetupRole.Passive)
        {
            AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.Setup, mediaIndex, -1, "A DTLS answer must use either the active or passive setup role.", ReadSetupLocation(setup, media));
        }
    }

    internal static SdpSetupAttribute? ReadEffectiveSetup(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes)
    {
        List<SdpAttribute> attributes = sessionAttributes;
        if (ContainsAttribute(mediaAttributes, SdpAttributeNames.Setup))
        {
            attributes = mediaAttributes;
        }
        SdpAttribute? attribute = ReadFirstAttribute(attributes, SdpAttributeNames.Setup);
        if (attribute == null)
        {
            return null;
        }
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        return decoded.SetupAttribute;
    }

    internal static SdpSourceLocation ReadSetupLocation(SdpSetupAttribute setup, SdpMediaDescription media)
    {
        if (setup.SourceAttribute != null)
        {
            return setup.SourceAttribute.Location;
        }
        return media.Location;
    }

    internal static void ValidateTlsIdUniqueness(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (media.Port != 0 && IsTlsTransport(media.Protocol))
            {
                SdpAttribute? attribute = ReadFirstAttribute(media.Attributes, SdpAttributeNames.TlsId);
                string id = ReadValidTlsId(attribute);
                if (id.Length > 0 && attribute != null)
                {
                    int transportMediaIndex = SdpDtlsServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
                    int earlierIndex = 0;
                    bool duplicate = false;
                    while (earlierIndex < mediaIndex && !duplicate)
                    {
                        SdpMediaDescription earlierMedia = description.MediaDescriptions[earlierIndex];
                        if (earlierMedia.Port != 0 && IsTlsTransport(earlierMedia.Protocol))
                        {
                            SdpAttribute? earlierAttribute = ReadFirstAttribute(earlierMedia.Attributes, SdpAttributeNames.TlsId);
                            string earlierId = ReadValidTlsId(earlierAttribute);
                            int earlierTransportMediaIndex = SdpDtlsServiceCore.ResolveTransportMediaIndex(description, earlierIndex);
                            duplicate = transportMediaIndex != earlierTransportMediaIndex && string.Equals(id, earlierId, StringComparison.Ordinal);
                        }
                        earlierIndex = earlierIndex + 1;
                    }
                    if (duplicate)
                    {
                        AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.TlsId, mediaIndex, 0, "A tls-id value must be unique to its TLS connection or DTLS association.", attribute.Location);
                    }
                }
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateBundleConsistency(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int attributeIndex = 0;
        while (attributeIndex < description.Attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(description.Attributes[attributeIndex]);
            if (decoded.Success && decoded.GroupAttribute != null && decoded.GroupAttribute.Semantic == SdpGroupSemantic.Bundle)
            {
                ValidateBundleGroupConsistency(description, decoded.GroupAttribute, result, limiter);
            }
            attributeIndex = attributeIndex + 1;
        }
    }

    internal static void ValidateBundleGroupConsistency(SdpSessionDescription description, SdpGroupAttribute group, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (group.IdentificationTags.Count == 0)
        {
            return;
        }
        int taggedMediaIndex = SdpDtlsServiceCore.FindMediaIndex(description, group.IdentificationTags[0]);
        if (taggedMediaIndex < 0)
        {
            return;
        }
        SdpDtlsDescription taggedDtls = SdpDtlsServiceCore.ResolveMediaDtls(description, taggedMediaIndex);
        int tagIndex = 1;
        while (tagIndex < group.IdentificationTags.Count)
        {
            int mediaIndex = SdpDtlsServiceCore.FindMediaIndex(description, group.IdentificationTags[tagIndex]);
            if (mediaIndex >= 0 && mediaIndex != taggedMediaIndex)
            {
                ValidateBundledMediaConsistency(description, mediaIndex, taggedDtls, result, limiter);
            }
            tagIndex = tagIndex + 1;
        }
    }

    internal static void ValidateBundledMediaConsistency(SdpSessionDescription description, int mediaIndex, SdpDtlsDescription taggedDtls, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        SdpDtlsDescription mediaDtls = SdpDtlsServiceCore.ResolveMediaDtls(description, mediaIndex);
        ValidateBundledAttributeConsistency(media, mediaIndex, SdpAttributeNames.Fingerprint, FingerprintsEqual(taggedDtls.Fingerprints, mediaDtls.Fingerprints), result, limiter);
        ValidateBundledAttributeConsistency(media, mediaIndex, SdpAttributeNames.Setup, SetupValuesEqual(taggedDtls, mediaDtls), result, limiter);
        ValidateBundledAttributeConsistency(media, mediaIndex, SdpAttributeNames.Connection, string.Equals(taggedDtls.ConnectionRole, mediaDtls.ConnectionRole, StringComparison.OrdinalIgnoreCase), result, limiter);
        ValidateBundledAttributeConsistency(media, mediaIndex, SdpAttributeNames.TlsId, string.Equals(taggedDtls.TlsId, mediaDtls.TlsId, StringComparison.Ordinal), result, limiter);
        ValidateBundledAttributeConsistency(media, mediaIndex, SdpAttributeNames.Crypto, CryptoAttributesEqual(taggedDtls.CryptoAttributes, mediaDtls.CryptoAttributes), result, limiter);
    }

    internal static void ValidateBundledAttributeConsistency(SdpMediaDescription media, int mediaIndex, string attributeName, bool equal, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!ContainsAttribute(media.Attributes, attributeName) || equal)
        {
            return;
        }
        SdpAttribute? attribute = ReadFirstAttribute(media.Attributes, attributeName);
        SdpSourceLocation location = media.Location;
        if (attribute != null)
        {
            location = attribute.Location;
        }
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBundle, SdpValidationSeverity.Error, SdpScope.Media, attributeName, mediaIndex, 0, "The bundled media attribute conflicts with the DTLS signaling of the tagged media transport.", location);
    }

    internal static bool SetupValuesEqual(SdpDtlsDescription first, SdpDtlsDescription second)
    {
        return first.SetupRole == second.SetupRole && string.Equals(first.SetupRoleToken, second.SetupRoleToken, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool FingerprintsEqual(List<SdpFingerprintAttribute> first, List<SdpFingerprintAttribute> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }
        bool[] used = new bool[second.Count];
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            bool matched = false;
            int secondIndex = 0;
            while (secondIndex < second.Count && !matched)
            {
                if (!used[secondIndex] && FingerprintValuesEqual(first[firstIndex], second[secondIndex]))
                {
                    used[secondIndex] = true;
                    matched = true;
                }
                secondIndex = secondIndex + 1;
            }
            if (!matched)
            {
                return false;
            }
            firstIndex = firstIndex + 1;
        }
        return true;
    }

    internal static bool FingerprintValuesEqual(SdpFingerprintAttribute first, SdpFingerprintAttribute second)
    {
        if (!string.Equals(first.Algorithm, second.Algorithm, StringComparison.OrdinalIgnoreCase) || first.Digest.Count != second.Digest.Count)
        {
            return false;
        }
        int index = 0;
        while (index < first.Digest.Count)
        {
            if (first.Digest[index] != second.Digest[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool CryptoAttributesEqual(List<SdpCryptoAttribute> first, List<SdpCryptoAttribute> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }
        int index = 0;
        while (index < first.Count)
        {
            if (!CryptoAttributeValuesEqual(first[index], second[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool CryptoAttributeValuesEqual(SdpCryptoAttribute first, SdpCryptoAttribute second)
    {
        if (first.Tag != second.Tag || first.Suite != second.Suite || !StringListsEqual(first.KeyParameters, second.KeyParameters) || !StringListsEqual(first.SessionParameters, second.SessionParameters))
        {
            return false;
        }
        return true;
    }

    internal static bool StringListsEqual(List<string> first, List<string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }
        int index = 0;
        while (index < first.Count)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static void ValidateAnswerTlsIdContinuity(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpSessionDescription? offer = result.AppliedOptions.OfferDescription;
        if (result.AppliedOptions.NegotiationRole != SdpNegotiationRole.Answerer || offer == null)
        {
            return;
        }
        int mediaCount = description.MediaDescriptions.Count;
        if (offer.MediaDescriptions.Count < mediaCount)
        {
            mediaCount = offer.MediaDescriptions.Count;
        }
        int mediaIndex = 0;
        while (mediaIndex < mediaCount)
        {
            SdpMediaDescription answerMedia = description.MediaDescriptions[mediaIndex];
            SdpMediaDescription offerMedia = offer.MediaDescriptions[mediaIndex];
            bool secure = IsTlsTransport(answerMedia.Protocol) || IsTlsTransport(offerMedia.Protocol);
            bool offerHasId = ContainsAttribute(offerMedia.Attributes, SdpAttributeNames.TlsId);
            bool answerHasId = ContainsAttribute(answerMedia.Attributes, SdpAttributeNames.TlsId);
            if (secure && answerMedia.Port != 0 && IsDtlsTransport(answerMedia.Protocol))
            {
                ValidateAnswerSetupPair(offer, offerMedia, description.Attributes, answerMedia, mediaIndex, result, limiter);
            }
            if (secure && answerMedia.Port != 0 && offerHasId != answerHasId)
            {
                SdpSourceLocation location = answerMedia.Location;
                SdpAttribute? answerAttribute = ReadFirstAttribute(answerMedia.Attributes, SdpAttributeNames.TlsId);
                if (answerAttribute != null)
                {
                    location = answerAttribute.Location;
                }
                string detail = "An answer must include tls-id when the corresponding offer includes it.";
                if (!offerHasId)
                {
                    detail = "An answer must not introduce tls-id when the corresponding offer omitted it.";
                }
                AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.TlsId, mediaIndex, answerHasId ? 0 : -1, detail, location);
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateAnswerSetupPair(SdpSessionDescription offer, SdpMediaDescription offerMedia, List<SdpAttribute> answerSessionAttributes, SdpMediaDescription answerMedia, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpSetupAttribute? offeredSetup = ReadEffectiveSetup(offer.Attributes, offerMedia.Attributes);
        SdpSetupAttribute? answeredSetup = ReadEffectiveSetup(answerSessionAttributes, answerMedia.Attributes);
        if (offeredSetup == null || answeredSetup == null)
        {
            return;
        }
        if (answeredSetup.Role != SdpDtlsSetupRole.Active && answeredSetup.Role != SdpDtlsSetupRole.Passive)
        {
            return;
        }
        if (offeredSetup.Role != SdpDtlsSetupRole.ActPass && offeredSetup.Role != SdpDtlsSetupRole.Active && offeredSetup.Role != SdpDtlsSetupRole.Passive)
        {
            AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.Setup, mediaIndex, -1, "The DTLS answer setup role is not compatible with the corresponding offered role.", ReadSetupLocation(answeredSetup, answerMedia));
            return;
        }
        bool valid = offeredSetup.Role == SdpDtlsSetupRole.ActPass && (answeredSetup.Role == SdpDtlsSetupRole.Active || answeredSetup.Role == SdpDtlsSetupRole.Passive) || offeredSetup.Role == SdpDtlsSetupRole.Active && answeredSetup.Role == SdpDtlsSetupRole.Passive || offeredSetup.Role == SdpDtlsSetupRole.Passive && answeredSetup.Role == SdpDtlsSetupRole.Active;
        if (!valid)
        {
            AddDtlsIssue(result, limiter, SdpScope.Media, SdpAttributeNames.Setup, mediaIndex, -1, "The DTLS answer setup role is not compatible with the corresponding offered role.", ReadSetupLocation(answeredSetup, answerMedia));
        }
    }

    internal static string ReadValidTlsId(SdpAttribute? attribute)
    {
        if (attribute == null)
        {
            return string.Empty;
        }
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (decoded.TlsIdAttribute == null)
        {
            return string.Empty;
        }
        return decoded.TlsIdAttribute.Id;
    }

    internal static void ValidateMediaAttributeApplicability(SdpMediaDescription media, int mediaIndex, string name, bool applicable, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (applicable)
        {
            return;
        }
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                int occurrenceIndex = ReadOccurrenceIndex(media.Attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateSessionAttributeApplicability(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool connectionOriented = false;
        bool secure = false;
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            string protocol = description.MediaDescriptions[mediaIndex].Protocol;
            connectionOriented = connectionOriented || IsConnectionOrientedTransport(protocol);
            secure = secure || IsTlsTransport(protocol);
            mediaIndex = mediaIndex + 1;
        }
        ValidateSessionAttributeApplicabilityByName(description.Attributes, SdpAttributeNames.Setup, connectionOriented || secure, "The session-level setup attribute has no applicable connection-oriented TLS or DTLS-capable media transport.", result, limiter);
        ValidateSessionAttributeApplicabilityByName(description.Attributes, SdpAttributeNames.Connection, connectionOriented, "The session-level connection attribute has no applicable connection-oriented media transport.", result, limiter);
        ValidateSessionAttributeApplicabilityByName(description.Attributes, SdpAttributeNames.Fingerprint, secure, "The session-level fingerprint attribute has no applicable TLS- or DTLS-capable media transport.", result, limiter);
    }

    internal static void ValidateSessionAttributeApplicabilityByName(List<SdpAttribute> attributes, string name, bool applicable, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (applicable)
        {
            return;
        }
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                int occurrenceIndex = ReadOccurrenceIndex(attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Session, attribute.Name, -1, occurrenceIndex, detail, attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static bool IsConnectionOrientedTransport(string protocol)
    {
        return string.Equals(protocol, "TCP", StringComparison.OrdinalIgnoreCase) || protocol.StartsWith("TCP/", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsTlsTransport(string protocol)
    {
        return ContainsProtocolComponent(protocol, "TLS") || ContainsProtocolComponent(protocol, "DTLS");
    }

    internal static bool IsDtlsTransport(string protocol)
    {
        return ContainsProtocolComponent(protocol, "DTLS") || protocol.StartsWith("UDP/TLS/", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsRtpTransport(string protocol)
    {
        return ContainsProtocolComponent(protocol, "RTP");
    }

    internal static bool IsDtlsSrtpTransport(string protocol)
    {
        return string.Equals(protocol, "UDP/TLS/RTP/SAVP", StringComparison.OrdinalIgnoreCase) || string.Equals(protocol, "UDP/TLS/RTP/SAVPF", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ContainsProtocolComponent(string protocol, string expected)
    {
        int start = 0;
        int index = 0;
        while (index <= protocol.Length)
        {
            if (index == protocol.Length || protocol[index] == '/')
            {
                if (index > start && string.Equals(protocol.Substring(start, index - start), expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                start = index + 1;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasEffectiveFingerprint(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes)
    {
        if (ContainsAttribute(mediaAttributes, SdpAttributeNames.Fingerprint))
        {
            return true;
        }
        return ContainsAttribute(sessionAttributes, SdpAttributeNames.Fingerprint);
    }

    internal static bool HasEffectiveAttribute(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, string name)
    {
        return ContainsAttribute(mediaAttributes, name) || ContainsAttribute(sessionAttributes, name);
    }

    internal static SdpAttribute? ReadFirstAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return attributes[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool ContainsAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsBaseDtlsAttributeName(string name)
    {
        return string.Equals(name, SdpAttributeNames.Setup, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Connection, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Fingerprint, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.TlsId, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsSingletonDtlsAttributeName(string name)
    {
        return string.Equals(name, SdpAttributeNames.Setup, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Connection, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.TlsId, StringComparison.OrdinalIgnoreCase);
    }

    internal static int ReadOccurrenceIndex(List<SdpAttribute> attributes, int targetIndex)
    {
        string name = attributes[targetIndex].Name;
        int occurrenceIndex = 0;
        int index = 0;
        while (index < targetIndex)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                occurrenceIndex = occurrenceIndex + 1;
            }
            index = index + 1;
        }
        return occurrenceIndex;
    }

    internal static string ReadDecodeFailureDetail(SdpAttributeDecodeResult decoded)
    {
        if (decoded.Diagnostics.Count > 0)
        {
            return decoded.Diagnostics[0].Detail;
        }
        return "The TLS or DTLS attribute value does not satisfy its registered syntax.";
    }

    internal static void AddDtlsIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpScope scope, string attributeName, int mediaIndex, int occurrenceIndex, string detail, SdpSourceLocation location)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationDtls, SdpValidationSeverity.Error, scope, attributeName, mediaIndex, occurrenceIndex, detail, location);
    }
}
