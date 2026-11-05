using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpStreamServiceCore
{
    internal static List<SdpSourceAttribute> ResolveSourceAttributes(SdpMediaDescription media)
    {
        List<SdpSourceAttribute> projections = new List<SdpSourceAttribute>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.SynchronizationSource, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.SynchronizationSourceAttribute != null)
                {
                    projections.Add(CopySourceAttribute(decoded.SynchronizationSourceAttribute));
                }
            }
            index = index + 1;
        }
        return projections;
    }

    internal static List<SdpSourceGroupAttribute> ResolveSourceGroups(SdpMediaDescription media)
    {
        List<SdpSourceGroupAttribute> projections = new List<SdpSourceGroupAttribute>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.SynchronizationSourceGroup, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.SourceGroupAttribute != null)
                {
                    projections.Add(CopySourceGroup(decoded.SourceGroupAttribute));
                }
            }
            index = index + 1;
        }
        return projections;
    }

    internal static List<SdpMediaStreamId> ResolveMediaStreamIds(SdpMediaDescription media)
    {
        List<SdpMediaStreamId> projections = new List<SdpMediaStreamId>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.MediaStreamId, StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Name, SdpAttributeNames.SynchronizationSource, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.MediaStreamId != null)
                {
                    projections.Add(CopyMediaStreamId(decoded.MediaStreamId));
                }
            }
            index = index + 1;
        }
        return projections;
    }

    internal static List<SdpMediaStreamSemanticAttribute> ResolveMediaStreamSemantics(SdpSessionDescription description)
    {
        List<SdpMediaStreamSemanticAttribute> projections = new List<SdpMediaStreamSemanticAttribute>();
        int index = 0;
        while (index < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.MediaStreamIdSemantic, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.MediaStreamSemanticAttribute != null)
                {
                    projections.Add(CopyMediaStreamSemantic(decoded.MediaStreamSemanticAttribute));
                }
            }
            index = index + 1;
        }
        return projections;
    }

    internal static List<SdpRidAttribute> ResolveRids(SdpMediaDescription media)
    {
        List<SdpRidAttribute> projections = new List<SdpRidAttribute>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Rid, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RidAttribute != null)
                {
                    projections.Add(CopyRid(decoded.RidAttribute));
                }
            }
            index = index + 1;
        }
        return projections;
    }

    internal static SdpSimulcastAttribute? ResolveSimulcast(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Simulcast, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.SimulcastAttribute != null)
                {
                    return CopySimulcast(decoded.SimulcastAttribute);
                }
            }
            index = index + 1;
        }
        return null;
    }

    internal static SdpRidAttribute RenameRid(SdpMediaDescription media, string currentId, string replacementId)
    {
        List<SdpRidAttribute> rids = ReadEditableRids(media);
        ValidateEditableRidGraph(rids);
        SdpRidAttribute? current = FindRid(rids, currentId);
        if (current == null)
        {
            throw new ArgumentException("The current RID identifier does not identify a media RID.", "currentId");
        }
        if (currentId != replacementId && FindRid(rids, replacementId) != null)
        {
            throw new ArgumentException("The replacement RID identifier must be unique within the media section.", "replacementId");
        }
        SdpSimulcastAttribute? simulcast = ReadEditableSimulcast(media);
        ValidateEditableSimulcast(simulcast, rids);
        if (currentId == replacementId)
        {
            return current;
        }

        List<bool> changedRids = new List<bool>();
        List<SdpAttribute> encodedRids = new List<SdpAttribute>();
        SdpRidAttribute? renamed = null;
        int ridIndex = 0;
        while (ridIndex < rids.Count)
        {
            SdpRidAttribute rid = rids[ridIndex];
            bool changed = false;
            if (rid.Id == currentId)
            {
                rid.Id = replacementId;
                changed = true;
                renamed = rid;
            }
            if (RenameDependencyReferences(rid, currentId, replacementId))
            {
                changed = true;
            }
            changedRids.Add(changed);
            encodedRids.Add(EncodeRidForEdit(rid));
            ridIndex = ridIndex + 1;
        }

        bool simulcastChanged = RenameSimulcastReferences(simulcast, currentId, replacementId);
        SdpAttribute? encodedSimulcast = null;
        if (simulcast != null)
        {
            encodedSimulcast = EncodeSimulcastForEdit(simulcast);
        }
        CommitRidUpdates(rids, changedRids, encodedRids);
        if (simulcastChanged && simulcast != null && encodedSimulcast != null && simulcast.SourceAttribute != null)
        {
            CopyEncodedAttribute(simulcast.SourceAttribute, encodedSimulcast);
        }
        return renamed == null ? current : renamed;
    }

    internal static bool RemoveRid(SdpMediaDescription media, string ridId)
    {
        List<SdpRidAttribute> rids = ReadEditableRids(media);
        ValidateEditableRidGraph(rids);
        SdpRidAttribute? removed = FindRid(rids, ridId);
        if (removed == null)
        {
            return false;
        }
        SdpSimulcastAttribute? simulcast = ReadEditableSimulcast(media);
        ValidateEditableSimulcast(simulcast, rids);

        List<string> removedIds = new List<string>();
        removedIds.Add(ridId);
        List<bool> changedRids = new List<bool>();
        List<SdpAttribute> encodedRids = new List<SdpAttribute>();
        int ridIndex = 0;
        while (ridIndex < rids.Count)
        {
            SdpRidAttribute rid = rids[ridIndex];
            bool changed = rid.Id != ridId && RemoveDependencyReferences(rid, removedIds);
            changedRids.Add(changed);
            encodedRids.Add(EncodeRidForEdit(rid));
            ridIndex = ridIndex + 1;
        }

        bool simulcastChanged = FilterSimulcast(simulcast, ReadIdentifiersExcept(rids, removedIds));
        SdpAttribute? encodedSimulcast = EncodeRemainingSimulcast(simulcast);
        CommitRidUpdates(rids, changedRids, encodedRids);
        RemoveSourceAttribute(media.Attributes, removed.SourceAttribute);
        CommitSimulcastFilter(media, simulcast, simulcastChanged, encodedSimulcast);
        return true;
    }

    internal static SdpSimulcastAttribute? FilterSimulcastLayers(SdpMediaDescription media, List<string> retainedRidIds)
    {
        List<SdpRidAttribute> rids = ReadEditableRids(media);
        ValidateEditableRidGraph(rids);
        SdpSimulcastAttribute? simulcast = ReadEditableSimulcast(media);
        ValidateEditableSimulcast(simulcast, rids);
        if (simulcast == null)
        {
            if (retainedRidIds.Count > 0)
            {
                throw new ArgumentException("Retained RID identifiers require modern simulcast signaling.", "retainedRidIds");
            }
            return null;
        }
        ValidateRetainedSimulcastIds(simulcast, retainedRidIds);

        List<string> simulcastIds = ReadSimulcastIdentifiers(simulcast);
        List<string> removedIds = new List<string>();
        int identifierIndex = 0;
        while (identifierIndex < simulcastIds.Count)
        {
            if (!retainedRidIds.Contains(simulcastIds[identifierIndex]))
            {
                removedIds.Add(simulcastIds[identifierIndex]);
            }
            identifierIndex = identifierIndex + 1;
        }

        List<bool> changedRids = new List<bool>();
        List<SdpAttribute> encodedRids = new List<SdpAttribute>();
        int ridIndex = 0;
        while (ridIndex < rids.Count)
        {
            SdpRidAttribute rid = rids[ridIndex];
            bool changed = !removedIds.Contains(rid.Id) && RemoveDependencyReferences(rid, removedIds);
            changedRids.Add(changed);
            encodedRids.Add(EncodeRidForEdit(rid));
            ridIndex = ridIndex + 1;
        }

        bool simulcastChanged = FilterSimulcast(simulcast, retainedRidIds);
        SdpAttribute? encodedSimulcast = EncodeRemainingSimulcast(simulcast);
        CommitRidUpdates(rids, changedRids, encodedRids);
        ridIndex = 0;
        while (ridIndex < rids.Count)
        {
            if (removedIds.Contains(rids[ridIndex].Id))
            {
                RemoveSourceAttribute(media.Attributes, rids[ridIndex].SourceAttribute);
            }
            ridIndex = ridIndex + 1;
        }
        CommitSimulcastFilter(media, simulcast, simulcastChanged, encodedSimulcast);
        if (encodedSimulcast == null)
        {
            return null;
        }
        return simulcast;
    }

    internal static List<SdpRidAttribute> ReadEditableRids(SdpMediaDescription media)
    {
        List<SdpRidAttribute> rids = new List<SdpRidAttribute>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Rid, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.RidAttribute == null)
                {
                    throw new ArgumentException("Every RID attribute must be valid before the referenced RID graph can be edited.", "description");
                }
                rids.Add(CopyRid(decoded.RidAttribute));
            }
            index = index + 1;
        }
        return rids;
    }

    internal static SdpSimulcastAttribute? ReadEditableSimulcast(SdpMediaDescription media)
    {
        SdpSimulcastAttribute? simulcast = null;
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Simulcast, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.SimulcastAttribute == null || decoded.SimulcastAttribute.LegacySyntax || simulcast != null)
                {
                    throw new ArgumentException("RID graph editing requires at most one valid modern simulcast attribute.", "description");
                }
                simulcast = CopySimulcast(decoded.SimulcastAttribute);
            }
            index = index + 1;
        }
        return simulcast;
    }

    internal static void ValidateEditableRidGraph(List<SdpRidAttribute> rids)
    {
        List<string> identifiers = new List<string>();
        int ridIndex = 0;
        while (ridIndex < rids.Count)
        {
            SdpRidAttribute rid = rids[ridIndex];
            if (identifiers.Contains(rid.Id))
            {
                throw new ArgumentException("RID identifiers must be unique before the referenced RID graph can be edited.", "description");
            }
            identifiers.Add(rid.Id);
            ridIndex = ridIndex + 1;
        }
        ridIndex = 0;
        while (ridIndex < rids.Count)
        {
            SdpRidAttribute rid = rids[ridIndex];
            bool dependencyFound = false;
            int restrictionIndex = 0;
            while (restrictionIndex < rid.Restrictions.Count)
            {
                SdpRidRestriction restriction = rid.Restrictions[restrictionIndex];
                if (restriction.Name == "depend")
                {
                    if (dependencyFound)
                    {
                        throw new ArgumentException("A RID must not contain multiple dependency restrictions before editing.", "description");
                    }
                    dependencyFound = true;
                    List<string> dependencies = ReadDependencyIdentifiers(restriction);
                    int dependencyIndex = 0;
                    while (dependencyIndex < dependencies.Count)
                    {
                        if (!identifiers.Contains(dependencies[dependencyIndex]))
                        {
                            throw new ArgumentException("Every RID dependency must identify another RID in the editable media graph.", "description");
                        }
                        dependencyIndex = dependencyIndex + 1;
                    }
                }
                restrictionIndex = restrictionIndex + 1;
            }
            ridIndex = ridIndex + 1;
        }
        List<int> states = new List<int>();
        ridIndex = 0;
        while (ridIndex < rids.Count)
        {
            states.Add(0);
            ridIndex = ridIndex + 1;
        }
        ridIndex = 0;
        while (ridIndex < rids.Count)
        {
            if (states[ridIndex] == 0 && SdpRidValidationCore.VisitDependencyGraph(rids, ridIndex, states))
            {
                throw new ArgumentException("RID dependency relationships must be acyclic before editing.", "description");
            }
            ridIndex = ridIndex + 1;
        }
    }

    internal static void ValidateEditableSimulcast(SdpSimulcastAttribute? simulcast, List<SdpRidAttribute> rids)
    {
        if (simulcast == null)
        {
            return;
        }
        List<string> identifiers = new List<string>();
        ValidateEditableSimulcastStreams(simulcast.SendStreams, SdpDirection.SendOnly, rids, identifiers);
        ValidateEditableSimulcastStreams(simulcast.ReceiveStreams, SdpDirection.ReceiveOnly, rids, identifiers);
    }

    internal static void ValidateEditableSimulcastStreams(List<SdpSimulcastStream> streams, SdpDirection direction, List<SdpRidAttribute> rids, List<string> identifiers)
    {
        int streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            SdpSimulcastStream stream = streams[streamIndex];
            int alternativeIndex = 0;
            while (alternativeIndex < stream.Alternatives.Count)
            {
                string identifier = stream.Alternatives[alternativeIndex].RidId;
                SdpRidAttribute? rid = FindRid(rids, identifier);
                if (identifiers.Contains(identifier) || rid == null || rid.Direction != direction)
                {
                    throw new ArgumentException("Every simulcast alternative must uniquely reference a direction-compatible RID before editing.", "description");
                }
                identifiers.Add(identifier);
                alternativeIndex = alternativeIndex + 1;
            }
            streamIndex = streamIndex + 1;
        }
    }

    internal static SdpRidAttribute? FindRid(List<SdpRidAttribute> rids, string identifier)
    {
        int index = 0;
        while (index < rids.Count)
        {
            if (rids[index].Id == identifier)
            {
                return rids[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static List<string> ReadDependencyIdentifiers(SdpRidRestriction restriction)
    {
        if (!restriction.HasValue || restriction.Value.Length == 0)
        {
            throw new ArgumentException("A RID dependency must contain a non-empty identifier list before editing.", "description");
        }
        string[] values = restriction.Value.Split(',');
        List<string> identifiers = new List<string>();
        int index = 0;
        while (index < values.Length)
        {
            if (!SdpRidAttributeCodecCore.IsRidIdentifier(values[index]) || identifiers.Contains(values[index]))
            {
                throw new ArgumentException("RID dependencies must contain distinct valid identifiers before editing.", "description");
            }
            identifiers.Add(values[index]);
            index = index + 1;
        }
        return identifiers;
    }

    internal static bool RenameDependencyReferences(SdpRidAttribute rid, string currentId, string replacementId)
    {
        bool changed = false;
        int restrictionIndex = 0;
        while (restrictionIndex < rid.Restrictions.Count)
        {
            SdpRidRestriction restriction = rid.Restrictions[restrictionIndex];
            if (restriction.Name == "depend")
            {
                List<string> identifiers = ReadDependencyIdentifiers(restriction);
                bool restrictionChanged = false;
                int identifierIndex = 0;
                while (identifierIndex < identifiers.Count)
                {
                    if (identifiers[identifierIndex] == currentId)
                    {
                        identifiers[identifierIndex] = replacementId;
                        changed = true;
                        restrictionChanged = true;
                    }
                    identifierIndex = identifierIndex + 1;
                }
                if (restrictionChanged)
                {
                    SetDependencyIdentifiers(restriction, identifiers);
                }
            }
            restrictionIndex = restrictionIndex + 1;
        }
        return changed;
    }

    internal static bool RemoveDependencyReferences(SdpRidAttribute rid, List<string> removedIds)
    {
        bool changed = false;
        int restrictionIndex = 0;
        while (restrictionIndex < rid.Restrictions.Count)
        {
            SdpRidRestriction restriction = rid.Restrictions[restrictionIndex];
            if (restriction.Name == "depend")
            {
                List<string> identifiers = ReadDependencyIdentifiers(restriction);
                bool restrictionChanged = false;
                int identifierIndex = identifiers.Count - 1;
                while (identifierIndex >= 0)
                {
                    if (removedIds.Contains(identifiers[identifierIndex]))
                    {
                        identifiers.RemoveAt(identifierIndex);
                        changed = true;
                        restrictionChanged = true;
                    }
                    identifierIndex = identifierIndex - 1;
                }
                if (identifiers.Count == 0)
                {
                    rid.Restrictions.RemoveAt(restrictionIndex);
                    restrictionIndex = restrictionIndex - 1;
                }
                else if (restrictionChanged)
                {
                    SetDependencyIdentifiers(restriction, identifiers);
                }
            }
            restrictionIndex = restrictionIndex + 1;
        }
        return changed;
    }

    internal static void SetDependencyIdentifiers(SdpRidRestriction restriction, List<string> identifiers)
    {
        string value = string.Empty;
        int index = 0;
        while (index < identifiers.Count)
        {
            if (index > 0)
            {
                value = value + ",";
            }
            value = value + identifiers[index];
            index = index + 1;
        }
        restriction.Value = value;
        restriction.RawText = restriction.Name + "=" + value;
    }

    internal static bool RenameSimulcastReferences(SdpSimulcastAttribute? simulcast, string currentId, string replacementId)
    {
        if (simulcast == null)
        {
            return false;
        }
        bool sendChanged = RenameSimulcastStreamReferences(simulcast.SendStreams, currentId, replacementId);
        bool receiveChanged = RenameSimulcastStreamReferences(simulcast.ReceiveStreams, currentId, replacementId);
        return sendChanged || receiveChanged;
    }

    internal static bool RenameSimulcastStreamReferences(List<SdpSimulcastStream> streams, string currentId, string replacementId)
    {
        bool changed = false;
        int streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            int alternativeIndex = 0;
            while (alternativeIndex < streams[streamIndex].Alternatives.Count)
            {
                SdpSimulcastAlternative alternative = streams[streamIndex].Alternatives[alternativeIndex];
                if (alternative.RidId == currentId)
                {
                    alternative.RidId = replacementId;
                    changed = true;
                }
                alternativeIndex = alternativeIndex + 1;
            }
            streamIndex = streamIndex + 1;
        }
        return changed;
    }

    internal static bool FilterSimulcast(SdpSimulcastAttribute? simulcast, List<string> retainedIds)
    {
        if (simulcast == null)
        {
            return false;
        }
        bool sendChanged = FilterSimulcastStreams(simulcast.SendStreams, retainedIds);
        bool receiveChanged = FilterSimulcastStreams(simulcast.ReceiveStreams, retainedIds);
        return sendChanged || receiveChanged;
    }

    internal static bool FilterSimulcastStreams(List<SdpSimulcastStream> streams, List<string> retainedIds)
    {
        bool changed = false;
        int streamIndex = streams.Count - 1;
        while (streamIndex >= 0)
        {
            SdpSimulcastStream stream = streams[streamIndex];
            int alternativeIndex = stream.Alternatives.Count - 1;
            while (alternativeIndex >= 0)
            {
                if (!retainedIds.Contains(stream.Alternatives[alternativeIndex].RidId))
                {
                    stream.Alternatives.RemoveAt(alternativeIndex);
                    changed = true;
                }
                alternativeIndex = alternativeIndex - 1;
            }
            if (stream.Alternatives.Count == 0)
            {
                streams.RemoveAt(streamIndex);
                changed = true;
            }
            streamIndex = streamIndex - 1;
        }
        return changed;
    }

    internal static List<string> ReadSimulcastIdentifiers(SdpSimulcastAttribute simulcast)
    {
        List<string> identifiers = new List<string>();
        AddSimulcastIdentifiers(simulcast.SendStreams, identifiers);
        AddSimulcastIdentifiers(simulcast.ReceiveStreams, identifiers);
        return identifiers;
    }

    internal static void AddSimulcastIdentifiers(List<SdpSimulcastStream> streams, List<string> identifiers)
    {
        int streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            int alternativeIndex = 0;
            while (alternativeIndex < streams[streamIndex].Alternatives.Count)
            {
                identifiers.Add(streams[streamIndex].Alternatives[alternativeIndex].RidId);
                alternativeIndex = alternativeIndex + 1;
            }
            streamIndex = streamIndex + 1;
        }
    }

    internal static void ValidateRetainedSimulcastIds(SdpSimulcastAttribute simulcast, List<string> retainedIds)
    {
        List<string> available = ReadSimulcastIdentifiers(simulcast);
        int index = 0;
        while (index < retainedIds.Count)
        {
            if (!available.Contains(retainedIds[index]))
            {
                throw new ArgumentException("Every retained RID identifier must identify a current simulcast layer.", "retainedRidIds");
            }
            index = index + 1;
        }
    }

    internal static List<string> ReadIdentifiersExcept(List<SdpRidAttribute> rids, List<string> excludedIds)
    {
        List<string> identifiers = new List<string>();
        int index = 0;
        while (index < rids.Count)
        {
            if (!excludedIds.Contains(rids[index].Id))
            {
                identifiers.Add(rids[index].Id);
            }
            index = index + 1;
        }
        return identifiers;
    }

    internal static SdpAttribute EncodeRidForEdit(SdpRidAttribute rid)
    {
        SdpAttributeEncodeResult encoded = SdpRidAttributeCodecCore.EncodeRid(rid);
        if (!encoded.Success || encoded.Attribute == null)
        {
            throw new ArgumentException("The updated RID graph cannot be represented as SDP.", "description");
        }
        return encoded.Attribute;
    }

    internal static SdpAttribute EncodeSimulcastForEdit(SdpSimulcastAttribute simulcast)
    {
        SdpAttributeEncodeResult encoded = SdpSimulcastAttributeCodecCore.EncodeSimulcast(simulcast);
        if (!encoded.Success || encoded.Attribute == null)
        {
            throw new ArgumentException("The updated simulcast graph cannot be represented as SDP.", "description");
        }
        return encoded.Attribute;
    }

    internal static SdpAttribute? EncodeRemainingSimulcast(SdpSimulcastAttribute? simulcast)
    {
        if (simulcast == null || simulcast.SendStreams.Count == 0 && simulcast.ReceiveStreams.Count == 0)
        {
            return null;
        }
        return EncodeSimulcastForEdit(simulcast);
    }

    internal static void CommitRidUpdates(List<SdpRidAttribute> rids, List<bool> changedRids, List<SdpAttribute> encodedRids)
    {
        int index = 0;
        while (index < rids.Count)
        {
            SdpAttribute? source = rids[index].SourceAttribute;
            if (changedRids[index] && source != null)
            {
                CopyEncodedAttribute(source, encodedRids[index]);
            }
            index = index + 1;
        }
    }

    internal static void CommitSimulcastFilter(SdpMediaDescription media, SdpSimulcastAttribute? simulcast, bool changed, SdpAttribute? encodedSimulcast)
    {
        if (!changed || simulcast == null || simulcast.SourceAttribute == null)
        {
            return;
        }
        if (encodedSimulcast == null)
        {
            RemoveSourceAttribute(media.Attributes, simulcast.SourceAttribute);
        }
        else
        {
            CopyEncodedAttribute(simulcast.SourceAttribute, encodedSimulcast);
        }
    }

    internal static void CopyEncodedAttribute(SdpAttribute target, SdpAttribute source)
    {
        target.Kind = source.Kind;
        target.Name = source.Name;
        target.Value = source.Value;
        target.RawText = source.RawText;
    }

    internal static bool RemoveSourceAttribute(List<SdpAttribute> attributes, SdpAttribute? source)
    {
        if (source == null)
        {
            return false;
        }
        int index = 0;
        while (index < attributes.Count)
        {
            if (object.ReferenceEquals(attributes[index], source))
            {
                attributes.RemoveAt(index);
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static List<SdpImageAttribute> ResolveImageAttributes(SdpMediaDescription media)
    {
        List<SdpImageAttribute> projections = new List<SdpImageAttribute>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.ImageAttribute, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.ImageAttribute != null)
                {
                    projections.Add(CopyImageAttribute(decoded.ImageAttribute));
                }
            }
            index = index + 1;
        }
        return projections;
    }

    internal static SdpSourceAttribute CopySourceAttribute(SdpSourceAttribute source)
    {
        SdpSourceAttribute result = new SdpSourceAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.SynchronizationSource = source.SynchronizationSource;
        result.Name = source.Name;
        result.HasValue = source.HasValue;
        result.Value = source.Value;
        return result;
    }

    internal static SdpSourceGroupAttribute CopySourceGroup(SdpSourceGroupAttribute source)
    {
        SdpSourceGroupAttribute result = new SdpSourceGroupAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.Semantic = source.Semantic;
        int index = 0;
        while (index < source.SynchronizationSources.Count)
        {
            result.SynchronizationSources.Add(source.SynchronizationSources[index]);
            index = index + 1;
        }
        return result;
    }

    internal static SdpMediaStreamId CopyMediaStreamId(SdpMediaStreamId source)
    {
        SdpMediaStreamId result = new SdpMediaStreamId();
        result.SourceAttribute = source.SourceAttribute;
        result.StreamId = source.StreamId;
        result.HasTrackId = source.HasTrackId;
        result.TrackId = source.TrackId;
        result.SourceLevel = source.SourceLevel;
        result.SynchronizationSource = source.SynchronizationSource;
        return result;
    }

    internal static SdpMediaStreamSemanticAttribute CopyMediaStreamSemantic(SdpMediaStreamSemanticAttribute source)
    {
        SdpMediaStreamSemanticAttribute result = new SdpMediaStreamSemanticAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.Semantic = source.Semantic;
        int index = 0;
        while (index < source.StreamIds.Count)
        {
            result.StreamIds.Add(source.StreamIds[index]);
            index = index + 1;
        }
        return result;
    }

    internal static SdpRidAttribute CopyRid(SdpRidAttribute source)
    {
        SdpRidAttribute result = new SdpRidAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.Id = source.Id;
        result.Direction = source.Direction;
        int payloadIndex = 0;
        while (payloadIndex < source.PayloadTypes.Count)
        {
            result.PayloadTypes.Add(source.PayloadTypes[payloadIndex]);
            payloadIndex = payloadIndex + 1;
        }
        int restrictionIndex = 0;
        while (restrictionIndex < source.Restrictions.Count)
        {
            result.Restrictions.Add(CopyRidRestriction(source.Restrictions[restrictionIndex]));
            restrictionIndex = restrictionIndex + 1;
        }
        return result;
    }

    internal static SdpRidRestriction CopyRidRestriction(SdpRidRestriction source)
    {
        SdpRidRestriction result = new SdpRidRestriction();
        result.Name = source.Name;
        result.HasValue = source.HasValue;
        result.Value = source.Value;
        result.RawText = source.RawText;
        return result;
    }

    internal static SdpSimulcastAttribute CopySimulcast(SdpSimulcastAttribute source)
    {
        SdpSimulcastAttribute result = new SdpSimulcastAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.SendFirst = source.SendFirst;
        result.LegacySyntax = source.LegacySyntax;
        result.RawValue = source.RawValue;
        CopySimulcastStreams(source.SendStreams, result.SendStreams);
        CopySimulcastStreams(source.ReceiveStreams, result.ReceiveStreams);
        return result;
    }

    internal static void CopySimulcastStreams(List<SdpSimulcastStream> source, List<SdpSimulcastStream> destination)
    {
        int streamIndex = 0;
        while (streamIndex < source.Count)
        {
            SdpSimulcastStream sourceStream = source[streamIndex];
            SdpSimulcastStream stream = new SdpSimulcastStream();
            int alternativeIndex = 0;
            while (alternativeIndex < sourceStream.Alternatives.Count)
            {
                SdpSimulcastAlternative sourceAlternative = sourceStream.Alternatives[alternativeIndex];
                SdpSimulcastAlternative alternative = new SdpSimulcastAlternative();
                alternative.RidId = sourceAlternative.RidId;
                alternative.Paused = sourceAlternative.Paused;
                stream.Alternatives.Add(alternative);
                alternativeIndex = alternativeIndex + 1;
            }
            destination.Add(stream);
            streamIndex = streamIndex + 1;
        }
    }

    internal static SdpImageAttribute CopyImageAttribute(SdpImageAttribute source)
    {
        SdpImageAttribute result = new SdpImageAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.PayloadType = source.PayloadType;
        result.HasSendConstraints = source.HasSendConstraints;
        result.SendConstraints = source.SendConstraints;
        result.HasReceiveConstraints = source.HasReceiveConstraints;
        result.ReceiveConstraints = source.ReceiveConstraints;
        return result;
    }
}
