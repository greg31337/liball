using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpIceServiceCore
{
    internal static SdpIceDescription ResolveMediaIce(SdpSessionDescription description, int mediaIndex)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        SdpIceDescription resolved = new SdpIceDescription();
        resolved.Credentials = ResolveCredentials(description.Attributes, media.Attributes);
        resolved.IceLite = ContainsValidMarker(description.Attributes, SdpAttributeNames.IceLite);
        resolved.IceMismatch = ContainsValidMarker(media.Attributes, SdpAttributeNames.IceMismatch);
        resolved.EndOfCandidates = ContainsValidMarker(media.Attributes, SdpAttributeNames.EndOfCandidates);
        ResolveOptions(description.Attributes, media.Attributes, resolved);
        ResolveCandidates(media.Attributes, resolved);
        ResolveRemoteCandidates(media.Attributes, resolved);
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

    internal static bool AddCandidate(SdpMediaDescription media, SdpIceCandidate candidate)
    {
        SdpAttribute encoded = EncodeCandidateForMutation(candidate);
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(media.Attributes[index]);
            if (decoded.Success && decoded.IceCandidate != null && CandidatesEqual(decoded.IceCandidate, candidate))
            {
                return false;
            }
            index = index + 1;
        }
        int insertionIndex = FindCompletionIndex(media.Attributes);
        media.Attributes.Insert(insertionIndex, encoded);
        return true;
    }

    internal static int RemoveCandidate(SdpMediaDescription media, SdpIceCandidate candidate)
    {
        EncodeCandidateForMutation(candidate);
        int removed = 0;
        int index = media.Attributes.Count - 1;
        while (index >= 0)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(media.Attributes[index]);
            if (decoded.Success && decoded.IceCandidate != null && CandidatesEqual(decoded.IceCandidate, candidate))
            {
                media.Attributes.RemoveAt(index);
                removed = removed + 1;
            }
            index = index - 1;
        }
        return removed;
    }

    internal static void ValidateCandidateFilter(int component, SdpIceTransportProtocol transportProtocol, SdpIceCandidateType candidateType)
    {
        if (component < 0 || component > 256)
        {
            throw new ArgumentOutOfRangeException("component");
        }
        if (transportProtocol < SdpIceTransportProtocol.Unknown || transportProtocol > SdpIceTransportProtocol.Tcp)
        {
            throw new ArgumentOutOfRangeException("transportProtocol");
        }
        if (candidateType < SdpIceCandidateType.Unknown || candidateType > SdpIceCandidateType.Relayed)
        {
            throw new ArgumentOutOfRangeException("candidateType");
        }
    }

    internal static int RemoveCandidatesByFilter(SdpMediaDescription media, int component, SdpIceTransportProtocol transportProtocol, SdpIceCandidateType candidateType)
    {
        int removed = 0;
        int index = media.Attributes.Count - 1;
        while (index >= 0)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(media.Attributes[index]);
            if (decoded.Success && decoded.IceCandidate != null && CandidateMatchesFilter(decoded.IceCandidate, component, transportProtocol, candidateType))
            {
                media.Attributes.RemoveAt(index);
                removed = removed + 1;
            }
            index = index - 1;
        }
        return removed;
    }

    internal static void SortCandidatesByPriority(SdpMediaDescription media)
    {
        List<SdpAttribute> attributes = new List<SdpAttribute>();
        List<SdpIceCandidate> candidates = new List<SdpIceCandidate>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(media.Attributes[index]);
            if (decoded.Success && decoded.IceCandidate != null)
            {
                attributes.Add(media.Attributes[index]);
                candidates.Add(decoded.IceCandidate);
            }
            index = index + 1;
        }

        index = 1;
        while (index < candidates.Count)
        {
            SdpIceCandidate candidate = candidates[index];
            SdpAttribute attribute = attributes[index];
            int insertionIndex = index;
            while (insertionIndex > 0 && candidates[insertionIndex - 1].Priority < candidate.Priority)
            {
                candidates[insertionIndex] = candidates[insertionIndex - 1];
                attributes[insertionIndex] = attributes[insertionIndex - 1];
                insertionIndex = insertionIndex - 1;
            }
            candidates[insertionIndex] = candidate;
            attributes[insertionIndex] = attribute;
            index = index + 1;
        }

        int candidateIndex = 0;
        index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(media.Attributes[index]);
            if (decoded.Success && decoded.IceCandidate != null)
            {
                media.Attributes[index] = attributes[candidateIndex];
                candidateIndex = candidateIndex + 1;
            }
            index = index + 1;
        }
    }

    internal static void MarkGatheringComplete(SdpMediaDescription media)
    {
        int index = media.Attributes.Count - 1;
        while (index >= 0)
        {
            if (string.Equals(media.Attributes[index].Name, SdpAttributeNames.EndOfCandidates, StringComparison.OrdinalIgnoreCase))
            {
                media.Attributes.RemoveAt(index);
            }
            index = index - 1;
        }
        SdpAttribute marker = new SdpAttribute();
        marker.Kind = SdpAttributeKind.Property;
        marker.Name = SdpAttributeNames.EndOfCandidates;
        marker.RawText = SdpAttributeNames.EndOfCandidates;
        media.Attributes.Add(marker);
    }

    internal static void SetRemoteCandidates(SdpMediaDescription media, List<SdpRemoteCandidate> candidates)
    {
        SdpAttribute? encoded = null;
        if (candidates.Count > 0)
        {
            SdpRemoteCandidatesAttribute projection = new SdpRemoteCandidatesAttribute();
            int candidateIndex = 0;
            while (candidateIndex < candidates.Count)
            {
                if (candidates[candidateIndex] == null)
                {
                    throw new ArgumentException("The remote candidate list must not contain null entries.", "candidates");
                }
                projection.Candidates.Add(candidates[candidateIndex]);
                candidateIndex = candidateIndex + 1;
            }
            SdpAttributeEncodeResult result = SdpRemoteCandidatesCodecCore.EncodeRemoteCandidates(projection);
            if (!result.Success || result.Attribute == null)
            {
                throw new ArgumentException("The remote candidates cannot be represented as an SDP attribute.", "candidates");
            }
            encoded = result.Attribute;
        }

        int insertionIndex = -1;
        int index = 0;
        while (index < media.Attributes.Count)
        {
            if (string.Equals(media.Attributes[index].Name, SdpAttributeNames.RemoteCandidates, StringComparison.OrdinalIgnoreCase))
            {
                if (insertionIndex < 0)
                {
                    insertionIndex = index;
                }
                media.Attributes.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
        if (encoded != null)
        {
            if (insertionIndex < 0)
            {
                insertionIndex = FindCompletionIndex(media.Attributes);
            }
            media.Attributes.Insert(insertionIndex, encoded);
        }
    }

    internal static bool CredentialsIndicateRestart(SdpIceCredentials? previous, SdpIceCredentials? current)
    {
        if (previous == null || current == null)
        {
            return false;
        }
        if (previous.UsernameFragment.Length == 0 || previous.Password.Length == 0 || current.UsernameFragment.Length == 0 || current.Password.Length == 0)
        {
            return false;
        }
        return previous.UsernameFragment != current.UsernameFragment && previous.Password != current.Password;
    }

    internal static void RestartIce(SdpMediaDescription media, SdpIceCredentials? existing, SdpIceCredentials credentials)
    {
        SdpAttribute usernameAttribute = EncodeRestartUsername(credentials);
        SdpAttribute passwordAttribute = EncodeRestartPassword(credentials);
        if (existing != null && existing.UsernameFragment.Length > 0 && existing.Password.Length > 0 && (existing.UsernameFragment == credentials.UsernameFragment || existing.Password == credentials.Password))
        {
            throw new ArgumentException("An ICE restart must change both effective credential values.", "credentials");
        }

        int insertionIndex = FindFirstRestartAttributeIndex(media.Attributes);
        int index = media.Attributes.Count - 1;
        while (index >= 0)
        {
            if (IsRestartAttributeName(media.Attributes[index].Name))
            {
                media.Attributes.RemoveAt(index);
            }
            index = index - 1;
        }
        if (insertionIndex > media.Attributes.Count)
        {
            insertionIndex = media.Attributes.Count;
        }
        media.Attributes.Insert(insertionIndex, usernameAttribute);
        media.Attributes.Insert(insertionIndex + 1, passwordAttribute);
    }

    internal static SdpAttribute EncodeRestartUsername(SdpIceCredentials credentials)
    {
        SdpIceUsernameFragmentAttribute projection = new SdpIceUsernameFragmentAttribute();
        projection.UsernameFragment = credentials.UsernameFragment;
        SdpAttributeEncodeResult result = SdpIceAttributeCodecCore.EncodeUsernameFragment(projection);
        if (!result.Success || result.Attribute == null)
        {
            throw new ArgumentException("The ICE username fragment cannot be represented as an SDP attribute.", "credentials");
        }
        return result.Attribute;
    }

    internal static SdpAttribute EncodeRestartPassword(SdpIceCredentials credentials)
    {
        SdpIcePasswordAttribute projection = new SdpIcePasswordAttribute();
        projection.Password = credentials.Password;
        SdpAttributeEncodeResult result = SdpIceAttributeCodecCore.EncodePassword(projection);
        if (!result.Success || result.Attribute == null)
        {
            throw new ArgumentException("The ICE password cannot be represented as an SDP attribute.", "credentials");
        }
        return result.Attribute;
    }

    internal static int FindFirstRestartAttributeIndex(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (IsRestartAttributeName(attributes[index].Name))
            {
                return index;
            }
            index = index + 1;
        }
        return attributes.Count;
    }

    internal static bool IsRestartAttributeName(string name)
    {
        return string.Equals(name, SdpAttributeNames.IceUsernameFragment, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.IcePassword, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Candidate, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.RemoteCandidates, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.EndOfCandidates, StringComparison.OrdinalIgnoreCase);
    }

    internal static SdpAttribute EncodeCandidateForMutation(SdpIceCandidate candidate)
    {
        SdpAttributeEncodeResult result = SdpIceCandidateCodecCore.EncodeCandidate(candidate);
        if (!result.Success || result.Attribute == null)
        {
            throw new ArgumentException("The candidate cannot be represented as an SDP candidate attribute.", "candidate");
        }
        return result.Attribute;
    }

    internal static int FindCompletionIndex(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, SdpAttributeNames.EndOfCandidates, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
            index = index + 1;
        }
        return attributes.Count;
    }

    internal static bool CandidateMatchesFilter(SdpIceCandidate candidate, int component, SdpIceTransportProtocol transportProtocol, SdpIceCandidateType candidateType)
    {
        bool componentMatches = component == 0 || candidate.Component == component;
        bool transportMatches = transportProtocol == SdpIceTransportProtocol.Unknown || candidate.TransportProtocol == transportProtocol;
        bool typeMatches = candidateType == SdpIceCandidateType.Unknown || candidate.CandidateType == candidateType;
        return componentMatches && transportMatches && typeMatches;
    }

    internal static bool CandidatesEqual(SdpIceCandidate first, SdpIceCandidate second)
    {
        if (first.Foundation != second.Foundation || first.Component != second.Component || first.Priority != second.Priority || !string.Equals(first.Address, second.Address, StringComparison.OrdinalIgnoreCase) || first.Port != second.Port)
        {
            return false;
        }
        if (!CandidateTransportsEqual(first, second) || !CandidateTypesEqual(first, second) || first.HasRelatedAddress != second.HasRelatedAddress || first.HasRelatedPort != second.HasRelatedPort || !CandidateTcpTypesEqual(first, second))
        {
            return false;
        }
        if (first.HasRelatedAddress && !string.Equals(first.RelatedAddress, second.RelatedAddress, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (first.HasRelatedPort && first.RelatedPort != second.RelatedPort)
        {
            return false;
        }
        if (first.Extensions.Count != second.Extensions.Count)
        {
            return false;
        }
        int index = 0;
        while (index < first.Extensions.Count)
        {
            if (!string.Equals(first.Extensions[index].Name, second.Extensions[index].Name, StringComparison.OrdinalIgnoreCase) || first.Extensions[index].Value != second.Extensions[index].Value)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool CandidateTransportsEqual(SdpIceCandidate first, SdpIceCandidate second)
    {
        if (first.TransportProtocol != second.TransportProtocol)
        {
            return false;
        }
        return first.TransportProtocol != SdpIceTransportProtocol.Unknown || string.Equals(first.TransportToken, second.TransportToken, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool CandidateTypesEqual(SdpIceCandidate first, SdpIceCandidate second)
    {
        if (first.CandidateType != second.CandidateType)
        {
            return false;
        }
        return first.CandidateType != SdpIceCandidateType.Unknown || string.Equals(first.CandidateTypeToken, second.CandidateTypeToken, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool CandidateTcpTypesEqual(SdpIceCandidate first, SdpIceCandidate second)
    {
        if (first.TcpType != second.TcpType)
        {
            return false;
        }
        return first.TcpType != SdpIceTcpType.Unknown || string.Equals(first.TcpTypeToken, second.TcpTypeToken, StringComparison.OrdinalIgnoreCase);
    }

    internal static SdpIceCredentials? ResolveCredentials(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes)
    {
        bool hasUsername = ContainsAttribute(mediaAttributes, SdpAttributeNames.IceUsernameFragment) || ContainsAttribute(sessionAttributes, SdpAttributeNames.IceUsernameFragment);
        bool hasPassword = ContainsAttribute(mediaAttributes, SdpAttributeNames.IcePassword) || ContainsAttribute(sessionAttributes, SdpAttributeNames.IcePassword);
        if (!hasUsername && !hasPassword)
        {
            return null;
        }
        SdpIceCredentials credentials = new SdpIceCredentials();
        credentials.UsernameFragment = ReadEffectiveCredential(sessionAttributes, mediaAttributes, SdpAttributeNames.IceUsernameFragment);
        credentials.Password = ReadEffectiveCredential(sessionAttributes, mediaAttributes, SdpAttributeNames.IcePassword);
        return credentials;
    }

    internal static string ReadEffectiveCredential(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, string name)
    {
        if (ContainsAttribute(mediaAttributes, name))
        {
            return ReadCredential(mediaAttributes, name);
        }
        return ReadCredential(sessionAttributes, name);
    }

    internal static string ReadCredential(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.IceUsernameFragmentAttribute != null)
                {
                    return decoded.IceUsernameFragmentAttribute.UsernameFragment;
                }
                if (decoded.Success && decoded.IcePasswordAttribute != null)
                {
                    return decoded.IcePasswordAttribute.Password;
                }
                return string.Empty;
            }
            index = index + 1;
        }
        return string.Empty;
    }

    internal static void ResolveOptions(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, SdpIceDescription resolved)
    {
        if (ContainsAttribute(mediaAttributes, SdpAttributeNames.IceOptions))
        {
            AddOptions(mediaAttributes, resolved);
            return;
        }
        AddOptions(sessionAttributes, resolved);
    }

    internal static void AddOptions(List<SdpAttribute> attributes, SdpIceDescription resolved)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.IceOptions, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.IceOptionsAttribute != null)
                {
                    int optionIndex = 0;
                    while (optionIndex < decoded.IceOptionsAttribute.Options.Count)
                    {
                        resolved.Options.Add(decoded.IceOptionsAttribute.Options[optionIndex]);
                        optionIndex = optionIndex + 1;
                    }
                }
                return;
            }
            index = index + 1;
        }
    }

    internal static void ResolveCandidates(List<SdpAttribute> attributes, SdpIceDescription resolved)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Candidate, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.IceCandidate != null)
                {
                    resolved.Candidates.Add(decoded.IceCandidate);
                }
            }
            index = index + 1;
        }
    }

    internal static void ResolveRemoteCandidates(List<SdpAttribute> attributes, SdpIceDescription resolved)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.RemoteCandidates, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RemoteCandidatesAttribute != null)
                {
                    int candidateIndex = 0;
                    while (candidateIndex < decoded.RemoteCandidatesAttribute.Candidates.Count)
                    {
                        resolved.RemoteCandidates.Add(decoded.RemoteCandidatesAttribute.Candidates[candidateIndex]);
                        candidateIndex = candidateIndex + 1;
                    }
                }
                return;
            }
            index = index + 1;
        }
    }

    internal static bool ContainsValidMarker(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                return decoded.Success && decoded.CommonAttribute != null;
            }
            index = index + 1;
        }
        return false;
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
