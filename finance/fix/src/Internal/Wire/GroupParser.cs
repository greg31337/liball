using System.Collections.Generic;

namespace fix;

internal static class GroupParser
{
    internal static GroupStructureResult BuildGroups(FixMessage message, FixDictionary dictionary, DecodeOptions options)
    {
        GroupStructureResult result = new GroupStructureResult();
        if (options.MaxGroupDepth < 0 || options.MaxGroupEntries < 0)
        {
            SetStructureError(result, DecodeStatus.Invalid, -1, -1, "invalid-group-limit");
            return result;
        }

        message.HeaderGroups.Clear();
        message.BodyGroups.Clear();
        message.TrailerGroups.Clear();

        GroupStructureResult header = ParseSection(message.HeaderFields, message.HeaderGroups, dictionary.Header, dictionary, options);
        if (!header.Success)
        {
            return header;
        }
        FixMessageDefinition? messageDefinition = FindMessageDefinition(dictionary, message.MsgType);
        if (messageDefinition != null)
        {
            GroupStructureResult body = ParseSection(message.BodyFields, message.BodyGroups, messageDefinition.Members, dictionary, options);
            if (!body.Success)
            {
                return body;
            }
        }
        GroupStructureResult trailer = ParseSection(message.TrailerFields, message.TrailerGroups, dictionary.Trailer, dictionary, options);
        if (!trailer.Success)
        {
            return trailer;
        }

        result.Success = true;
        result.Status = DecodeStatus.Success;
        return result;
    }

    internal static GroupStructureResult ParseSection(List<FixField> fields, List<FixGroup> groups, List<FixMemberDefinition> members, FixDictionary dictionary, DecodeOptions options)
    {
        GroupStructureResult result = new GroupStructureResult();
        int index = 0;
        while (index < fields.Count)
        {
            FixGroupDefinition? definition = FindReferencedGroup(members, dictionary, fields[index].Tag, 0);
            if (definition == null)
            {
                index++;
                continue;
            }
            GroupParseResult parsed = ParseGroup(fields, index, definition, dictionary, options, 1);
            if (!parsed.Success)
            {
                CopyParseError(result, parsed);
                return result;
            }
            FixGroup? parsedGroup = parsed.Group;
            if (parsedGroup == null)
            {
                SetStructureError(result, DecodeStatus.Invalid, fields[index].WireIndex, fields[index].SourceOffset, "missing-parsed-group");
                return result;
            }
            groups.Add(parsedGroup);
            index = parsed.NextIndex;
        }
        result.Success = true;
        result.Status = DecodeStatus.Success;
        return result;
    }

    internal static GroupParseResult ParseGroup(List<FixField> fields, int countIndex, FixGroupDefinition definition, FixDictionary dictionary, DecodeOptions options, int depth)
    {
        GroupParseResult result = new GroupParseResult();
        if (depth > options.MaxGroupDepth)
        {
            FixField depthField = fields[countIndex];
            SetParseError(result, DecodeStatus.LimitExceeded, depthField.WireIndex, depthField.SourceOffset, "group-depth-limit-exceeded");
            return result;
        }
        FixField countField = fields[countIndex];
        ScalarParseResult parsedCount = ScalarParser.ParseScalar(countField.Value, FixFieldType.NumInGroup, options.MaxFieldSize);
        if (!parsedCount.Success)
        {
            DecodeStatus status = parsedCount.ErrorCode == "limit-exceeded" ? DecodeStatus.LimitExceeded : DecodeStatus.Invalid;
            SetParseError(result, status, countField.WireIndex, countField.SourceOffset, "invalid-group-count");
            return result;
        }
        if (parsedCount.IntegerValue > options.MaxGroupEntries)
        {
            SetParseError(result, DecodeStatus.LimitExceeded, countField.WireIndex, countField.SourceOffset, "group-entry-limit-exceeded");
            return result;
        }

        int delimiterTag = ResolveDelimiterTag(definition, dictionary);
        if (delimiterTag <= 0)
        {
            SetParseError(result, DecodeStatus.Invalid, countField.WireIndex, countField.SourceOffset, "missing-group-delimiter-definition");
            return result;
        }
        FixGroup group = new FixGroup();
        group.CountTag = definition.CountTag;
        group.DelimiterTag = delimiterTag;
        group.DeclaredCount = parsedCount.IntegerValue;
        group.WireIndex = countField.WireIndex;
        int index = countIndex + 1;
        if (group.DeclaredCount == 0)
        {
            if (index < fields.Count && fields[index].Tag == delimiterTag)
            {
                SetParseError(result, DecodeStatus.Invalid, fields[index].WireIndex, fields[index].SourceOffset, "group-count-mismatch");
                return result;
            }
            result.Success = true;
            result.Status = DecodeStatus.Success;
            result.Group = group;
            result.NextIndex = index;
            return result;
        }

        GroupMemberLookupResult delimiterMember = FindGroupMember(definition, dictionary, delimiterTag);
        if (!delimiterMember.Found || delimiterMember.IsGroup)
        {
            SetParseError(result, DecodeStatus.Invalid, countField.WireIndex, countField.SourceOffset, "invalid-group-delimiter-definition");
            return result;
        }

        for (int entryIndex = 0; entryIndex < group.DeclaredCount; entryIndex++)
        {
            if (index >= fields.Count)
            {
                int endOffset = countField.SourceOffset + countField.SourceLength;
                if (fields.Count > 0)
                {
                    FixField last = fields[fields.Count - 1];
                    endOffset = last.SourceOffset + last.SourceLength;
                }
                SetParseError(result, DecodeStatus.Invalid, countField.WireIndex, endOffset, "premature-group-termination");
                return result;
            }
            if (fields[index].Tag != delimiterTag)
            {
                SetParseError(result, DecodeStatus.Invalid, fields[index].WireIndex, fields[index].SourceOffset, "missing-group-delimiter");
                return result;
            }

            FixGroupEntry entry = new FixGroupEntry();
            entry.EntryIndex = entryIndex;
            int lastPosition = -1;
            while (index < fields.Count)
            {
                FixField current = fields[index];
                if (entry.Fields.Count > 0 && current.Tag == delimiterTag)
                {
                    break;
                }
                GroupMemberLookupResult member = FindGroupMember(definition, dictionary, current.Tag);
                if (!member.Found)
                {
                    break;
                }
                if (member.Position < lastPosition)
                {
                    SetParseError(result, DecodeStatus.Invalid, current.WireIndex, current.SourceOffset, "group-member-out-of-order");
                    return result;
                }
                if (member.Position == lastPosition)
                {
                    SetParseError(result, DecodeStatus.Invalid, current.WireIndex, current.SourceOffset, "duplicate-group-member");
                    return result;
                }
                if (member.IsGroup)
                {
                    FixGroupDefinition? nestedDefinition = member.GroupDefinition;
                    if (nestedDefinition == null)
                    {
                        SetParseError(result, DecodeStatus.Invalid, current.WireIndex, current.SourceOffset, "missing-group-definition");
                        return result;
                    }
                    GroupParseResult nested = ParseGroup(fields, index, nestedDefinition, dictionary, options, depth + 1);
                    if (!nested.Success)
                    {
                        return nested;
                    }
                    FixGroup? nestedGroup = nested.Group;
                    if (nestedGroup == null)
                    {
                        SetParseError(result, DecodeStatus.Invalid, current.WireIndex, current.SourceOffset, "missing-parsed-group");
                        return result;
                    }
                    entry.Groups.Add(nestedGroup);
                    index = nested.NextIndex;
                }
                else
                {
                    entry.Fields.Add(current);
                    index++;
                }
                lastPosition = member.Position;
            }
            group.Entries.Add(entry);

            if (entryIndex + 1 < group.DeclaredCount)
            {
                if (index >= fields.Count)
                {
                    int endOffset = countField.SourceOffset + countField.SourceLength;
                    if (entry.Fields.Count > 0)
                    {
                        FixField lastEntryField = entry.Fields[entry.Fields.Count - 1];
                        endOffset = lastEntryField.SourceOffset + lastEntryField.SourceLength;
                    }
                    SetParseError(result, DecodeStatus.Invalid, countField.WireIndex, endOffset, "premature-group-termination");
                    return result;
                }
                if (fields[index].Tag != delimiterTag)
                {
                    SetParseError(result, DecodeStatus.Invalid, fields[index].WireIndex, fields[index].SourceOffset, "premature-group-termination");
                    return result;
                }
            }
        }

        if (index < fields.Count && fields[index].Tag == delimiterTag)
        {
            SetParseError(result, DecodeStatus.Invalid, fields[index].WireIndex, fields[index].SourceOffset, "group-count-mismatch");
            return result;
        }
        result.Success = true;
        result.Status = DecodeStatus.Success;
        result.Group = group;
        result.NextIndex = index;
        return result;
    }

    internal static GroupMemberLookupResult FindGroupMember(FixGroupDefinition definition, FixDictionary dictionary, int tag)
    {
        GroupMemberLookupResult result = new GroupMemberLookupResult();
        SearchMembers(definition.Members, dictionary, tag, result, 0);
        return result;
    }

    internal static void SearchMembers(List<FixMemberDefinition> members, FixDictionary dictionary, int tag, GroupMemberLookupResult result, int depth)
    {
        if (result.Found || depth > FixDefaults.MaxGroupDepth)
        {
            return;
        }
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            if (member.Kind == FixMemberKind.Field)
            {
                if (member.ReferenceId == tag)
                {
                    result.Found = true;
                    result.Position = result.PositionCursor;
                    return;
                }
                result.PositionCursor++;
            }
            else if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = FindComponentDefinition(dictionary, member.ReferenceId);
                if (component != null)
                {
                    SearchMembers(component.Members, dictionary, tag, result, depth + 1);
                    if (result.Found)
                    {
                        return;
                    }
                }
            }
            else if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? group = FindGroupDefinition(dictionary, member.ReferenceId);
                if (group != null && group.CountTag == tag)
                {
                    result.Found = true;
                    result.IsGroup = true;
                    result.Position = result.PositionCursor;
                    result.GroupDefinition = group;
                    return;
                }
                result.PositionCursor++;
            }
        }
    }

    internal static FixGroupDefinition? FindReferencedGroup(List<FixMemberDefinition> members, FixDictionary dictionary, int countTag, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return null;
        }
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? group = FindGroupDefinition(dictionary, member.ReferenceId);
                if (group != null && group.CountTag == countTag)
                {
                    return group;
                }
            }
            else if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = FindComponentDefinition(dictionary, member.ReferenceId);
                if (component != null)
                {
                    FixGroupDefinition? nested = FindReferencedGroup(component.Members, dictionary, countTag, depth + 1);
                    if (nested != null)
                    {
                        return nested;
                    }
                }
            }
        }
        return null;
    }

    internal static int ResolveDelimiterTag(FixGroupDefinition definition, FixDictionary dictionary)
    {
        if (definition.DelimiterTag > 0)
        {
            return definition.DelimiterTag;
        }
        return FindFirstFieldTag(definition.Members, dictionary, 0);
    }

    internal static int FindFirstFieldTag(List<FixMemberDefinition> members, FixDictionary dictionary, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return 0;
        }
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            if (member.Kind == FixMemberKind.Field)
            {
                return member.ReferenceId;
            }
            if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = FindComponentDefinition(dictionary, member.ReferenceId);
                if (component != null)
                {
                    int tag = FindFirstFieldTag(component.Members, dictionary, depth + 1);
                    if (tag > 0)
                    {
                        return tag;
                    }
                }
            }
        }
        return 0;
    }

    internal static FixMessageDefinition? FindMessageDefinition(FixDictionary dictionary, string msgType)
    {
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            if (dictionary.Messages[index].MsgType == msgType)
            {
                return dictionary.Messages[index];
            }
        }
        return null;
    }

    internal static FixGroupDefinition? FindGroupDefinition(FixDictionary dictionary, int id)
    {
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            if (dictionary.Groups[index].Id == id)
            {
                return dictionary.Groups[index];
            }
        }
        return null;
    }

    internal static FixComponentDefinition? FindComponentDefinition(FixDictionary dictionary, int id)
    {
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            if (dictionary.Components[index].Id == id)
            {
                return dictionary.Components[index];
            }
        }
        return null;
    }

    internal static void CopyParseError(GroupStructureResult result, GroupParseResult parsed)
    {
        result.Status = parsed.Status;
        result.ErrorWireIndex = parsed.ErrorWireIndex;
        result.ErrorOffset = parsed.ErrorOffset;
        result.ErrorCode = parsed.ErrorCode;
        result.ErrorMessage = parsed.ErrorMessage;
    }

    internal static void SetParseError(GroupParseResult result, DecodeStatus status, int wireIndex, int offset, string errorCode)
    {
        result.Status = status;
        result.ErrorWireIndex = wireIndex;
        result.ErrorOffset = offset;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The FIX repeating group could not be decoded: {errorCode}.";
    }

    internal static void SetStructureError(GroupStructureResult result, DecodeStatus status, int wireIndex, int offset, string errorCode)
    {
        result.Status = status;
        result.ErrorWireIndex = wireIndex;
        result.ErrorOffset = offset;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The FIX repeating groups could not be structured: {errorCode}.";
    }
}
