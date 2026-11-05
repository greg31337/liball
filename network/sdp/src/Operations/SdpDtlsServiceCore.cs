using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpDtlsServiceCore
{
    internal static SdpDtlsDescription ResolveMediaDtls(SdpSessionDescription description, int mediaIndex)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        SdpDtlsDescription resolved = new SdpDtlsDescription();
        ResolveFingerprints(description.Attributes, media.Attributes, resolved);
        ResolveSetup(description.Attributes, media.Attributes, resolved);
        ResolveConnection(description.Attributes, media.Attributes, resolved);
        ResolveTlsId(media.Attributes, resolved);
        ResolveCryptoAttributes(description.Attributes, media.Attributes, resolved);
        return resolved;
    }

    internal static int ResolveTransportMediaIndex(SdpSessionDescription description, int mediaIndex)
    {
        string mediaId = ReadMediaId(description.MediaDescriptions[mediaIndex].Attributes);
        if (mediaId.Length == 0)
        {
            return mediaIndex;
        }
        int attributeIndex = 0;
        while (attributeIndex < description.Attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(description.Attributes[attributeIndex]);
            if (decoded.Success && decoded.GroupAttribute != null && decoded.GroupAttribute.Semantic == SdpGroupSemantic.Bundle && ContainsIdentificationTag(decoded.GroupAttribute.IdentificationTags, mediaId))
            {
                if (decoded.GroupAttribute.IdentificationTags.Count > 0)
                {
                    int taggedIndex = FindMediaIndex(description, decoded.GroupAttribute.IdentificationTags[0]);
                    if (taggedIndex >= 0)
                    {
                        return taggedIndex;
                    }
                }
                return mediaIndex;
            }
            attributeIndex = attributeIndex + 1;
        }
        return mediaIndex;
    }

    internal static void ResolveFingerprints(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, SdpDtlsDescription resolved)
    {
        List<SdpAttribute> attributes = sessionAttributes;
        if (ContainsAttribute(mediaAttributes, SdpAttributeNames.Fingerprint))
        {
            attributes = mediaAttributes;
        }
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Fingerprint, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.FingerprintAttribute != null)
                {
                    resolved.Fingerprints.Add(decoded.FingerprintAttribute);
                }
            }
            index = index + 1;
        }
    }

    internal static void ResolveSetup(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, SdpDtlsDescription resolved)
    {
        SdpAttributeDecodeResult? decoded = ReadEffectiveAttribute(sessionAttributes, mediaAttributes, SdpAttributeNames.Setup);
        if (decoded != null && decoded.Success && decoded.SetupAttribute != null)
        {
            resolved.SetupRole = decoded.SetupAttribute.Role;
            resolved.SetupRoleToken = decoded.SetupAttribute.RoleToken;
        }
    }

    internal static void ResolveConnection(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, SdpDtlsDescription resolved)
    {
        SdpAttributeDecodeResult? decoded = ReadEffectiveAttribute(sessionAttributes, mediaAttributes, SdpAttributeNames.Connection);
        if (decoded != null && decoded.Success && decoded.ConnectionAttribute != null)
        {
            resolved.ConnectionRole = decoded.ConnectionAttribute.ConnectionRole;
        }
    }

    internal static void ResolveTlsId(List<SdpAttribute> mediaAttributes, SdpDtlsDescription resolved)
    {
        SdpAttributeDecodeResult? decoded = ReadFirstAttribute(mediaAttributes, SdpAttributeNames.TlsId);
        if (decoded != null && decoded.Success && decoded.TlsIdAttribute != null)
        {
            resolved.TlsId = decoded.TlsIdAttribute.Id;
        }
    }

    internal static void ResolveCryptoAttributes(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, SdpDtlsDescription resolved)
    {
        List<SdpAttribute> attributes = sessionAttributes;
        if (ContainsAttribute(mediaAttributes, SdpAttributeNames.Crypto))
        {
            attributes = mediaAttributes;
        }
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Crypto, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.CryptoAttribute != null)
                {
                    resolved.CryptoAttributes.Add(decoded.CryptoAttribute);
                }
            }
            index = index + 1;
        }
    }

    internal static SdpFingerprintAttribute? SelectPreferredFingerprint(SdpDtlsDescription description, List<string> preferredAlgorithms)
    {
        int preferenceIndex = 0;
        while (preferenceIndex < preferredAlgorithms.Count)
        {
            string algorithm = preferredAlgorithms[preferenceIndex];
            if (algorithm == null || !SdpCommonAttributeCodecCore.IsTokenValue(algorithm))
            {
                throw new ArgumentException("Preferred fingerprint algorithms must use non-empty token syntax.", "preferredAlgorithms");
            }
            preferenceIndex = preferenceIndex + 1;
        }
        preferenceIndex = 0;
        while (preferenceIndex < preferredAlgorithms.Count)
        {
            string algorithm = preferredAlgorithms[preferenceIndex];
            int fingerprintIndex = 0;
            while (fingerprintIndex < description.Fingerprints.Count)
            {
                SdpFingerprintAttribute fingerprint = description.Fingerprints[fingerprintIndex];
                if (!IsProhibitedFingerprintAlgorithm(algorithm) && fingerprint != null && string.Equals(fingerprint.Algorithm, algorithm, StringComparison.OrdinalIgnoreCase))
                {
                    return fingerprint;
                }
                fingerprintIndex = fingerprintIndex + 1;
            }
            preferenceIndex = preferenceIndex + 1;
        }
        return null;
    }

    internal static bool IsProhibitedFingerprintAlgorithm(string algorithm)
    {
        return string.Equals(algorithm, "md2", StringComparison.OrdinalIgnoreCase) || string.Equals(algorithm, "md5", StringComparison.OrdinalIgnoreCase);
    }

    internal static SdpDtlsSetupRole SelectAnswerSetupRole(SdpDtlsSetupRole offeredRole, SdpDtlsSetupRole preferredAnswerRole)
    {
        ValidateDefinedSetupRole(offeredRole, "offeredRole");
        ValidateDefinedSetupRole(preferredAnswerRole, "preferredAnswerRole");
        if (preferredAnswerRole != SdpDtlsSetupRole.Active && preferredAnswerRole != SdpDtlsSetupRole.Passive)
        {
            throw new ArgumentException("The preferred DTLS answer role must be active or passive.", "preferredAnswerRole");
        }
        if (offeredRole == SdpDtlsSetupRole.ActPass)
        {
            return preferredAnswerRole;
        }
        if (offeredRole == SdpDtlsSetupRole.Active)
        {
            return SdpDtlsSetupRole.Passive;
        }
        if (offeredRole == SdpDtlsSetupRole.Passive)
        {
            return SdpDtlsSetupRole.Active;
        }
        throw new ArgumentException("The offered setup role cannot select a DTLS answer role.", "offeredRole");
    }

    internal static void ValidateDefinedSetupRole(SdpDtlsSetupRole role, string parameterName)
    {
        bool defined = role == SdpDtlsSetupRole.Unknown || role == SdpDtlsSetupRole.Active || role == SdpDtlsSetupRole.Passive || role == SdpDtlsSetupRole.ActPass || role == SdpDtlsSetupRole.HoldConnection;
        if (!defined)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    internal static void ReplaceFingerprints(SdpMediaDescription media, List<SdpFingerprintAttribute> fingerprints)
    {
        List<SdpAttribute> encoded = new List<SdpAttribute>();
        int fingerprintIndex = 0;
        while (fingerprintIndex < fingerprints.Count)
        {
            SdpFingerprintAttribute fingerprint = fingerprints[fingerprintIndex];
            if (fingerprint == null)
            {
                throw new ArgumentException("The fingerprint list must not contain null entries.", "fingerprints");
            }
            SdpAttributeEncodeResult result = SdpFingerprintAttributeCodecCore.EncodeFingerprint(fingerprint);
            if (!result.Success || result.Attribute == null)
            {
                throw new ArgumentException("A fingerprint cannot be represented as an SDP attribute.", "fingerprints");
            }
            encoded.Add(result.Attribute);
            fingerprintIndex = fingerprintIndex + 1;
        }
        ReplaceAttributes(media.Attributes, SdpAttributeNames.Fingerprint, encoded);
    }

    internal static void ReplaceTlsId(SdpMediaDescription media, SdpTlsIdAttribute tlsId)
    {
        SdpAttributeEncodeResult result = SdpDtlsAttributeCodecCore.EncodeTlsId(tlsId);
        if (!result.Success || result.Attribute == null)
        {
            throw new ArgumentException("The TLS identifier cannot be represented as an SDP attribute.", "tlsId");
        }
        List<SdpAttribute> encoded = new List<SdpAttribute>();
        encoded.Add(result.Attribute);
        ReplaceAttributes(media.Attributes, SdpAttributeNames.TlsId, encoded);
    }

    internal static void ReplaceAttributes(List<SdpAttribute> attributes, string name, List<SdpAttribute> replacements)
    {
        int insertionIndex = -1;
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                if (insertionIndex < 0)
                {
                    insertionIndex = index;
                }
                attributes.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
        if (insertionIndex < 0)
        {
            insertionIndex = attributes.Count;
        }
        int replacementIndex = 0;
        while (replacementIndex < replacements.Count)
        {
            attributes.Insert(insertionIndex + replacementIndex, replacements[replacementIndex]);
            replacementIndex = replacementIndex + 1;
        }
    }

    internal static SdpAttributeDecodeResult? ReadEffectiveAttribute(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, string name)
    {
        if (ContainsAttribute(mediaAttributes, name))
        {
            return ReadFirstAttribute(mediaAttributes, name);
        }
        return ReadFirstAttribute(sessionAttributes, name);
    }

    internal static SdpAttributeDecodeResult? ReadFirstAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return SdpAttributeDecoderCore.DecodeAttribute(attributes[index]);
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

    internal static string ReadMediaId(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attributes[index]);
            if (decoded.Success && decoded.MediaIdAttribute != null)
            {
                return decoded.MediaIdAttribute.Id;
            }
            index = index + 1;
        }
        return string.Empty;
    }

    internal static bool ContainsIdentificationTag(List<string> tags, string mediaId)
    {
        int index = 0;
        while (index < tags.Count)
        {
            if (tags[index] == mediaId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static int FindMediaIndex(SdpSessionDescription description, string mediaId)
    {
        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            if (ReadMediaId(description.MediaDescriptions[index].Attributes) == mediaId)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }
}
