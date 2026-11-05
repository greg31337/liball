using System.Collections.Generic;

namespace fix;

internal static class RepeatingGroupValidator
{
    internal static List<ValidationIssue> ValidateRepeatingGroups(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
    {
        List<ValidationIssue> issues = [];
        if (!ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.Group))
        {
            return issues;
        }

        List<FixField> flatFields = EnvelopeValidator.CreateWireOrder(message);
        ValidateSectionGroups(message.HeaderGroups, dictionary.Header, dictionary, options, flatFields, issues, message.MsgType);
        FixMessageDefinition? messageDefinition = DictionaryStructureValidator.FindMessageForValidation(dictionary, message.MsgType, context.Scenario);
        if (messageDefinition == null)
        {
            ValidateSectionGroups(message.BodyGroups, [], dictionary, options, flatFields, issues, message.MsgType);
        }
        else
        {
            ValidateSectionGroups(message.BodyGroups, messageDefinition.Members, dictionary, options, flatFields, issues, message.MsgType);
        }
        ValidateSectionGroups(message.TrailerGroups, dictionary.Trailer, dictionary, options, flatFields, issues, message.MsgType);
        return issues;
    }

    internal static void ValidateSectionGroups(List<FixGroup> groups, List<FixMemberDefinition> members, FixDictionary dictionary, ValidationOptions options, List<FixField> flatFields, List<ValidationIssue> issues, string msgType)
    {
        for (int index = 0; index < groups.Count; index++)
        {
            FixGroup group = groups[index];
            string groupPath = group.CountTag.ToString();
            FixGroupDefinition? definition = GroupParser.FindReferencedGroup(members, dictionary, group.CountTag, 0);
            if (definition == null)
            {
                AddGroupIssue(issues, "group-not-allowed", "The message section does not allow this repeating group.", true, group.CountTag, FindCountField(flatFields, group), groupPath, msgType);
                continue;
            }
            if (options.RejectDuplicateFields && HasEarlierGroup(groups, index, group.CountTag))
            {
                AddGroupIssue(issues, "duplicate-group", "A repeating group occurs more than once in the same message section.", true, group.CountTag, FindCountField(flatFields, group), groupPath, msgType);
            }
            ValidateGroup(group, definition, dictionary, options, flatFields, issues, msgType, groupPath, 1);
        }
    }

    internal static bool HasEarlierGroup(List<FixGroup> groups, int exclusiveEnd, int countTag)
    {
        for (int index = 0; index < exclusiveEnd; index++)
        {
            if (groups[index].CountTag == countTag)
            {
                return true;
            }
        }
        return false;
    }

    internal static void ValidateGroup(FixGroup group, FixGroupDefinition definition, FixDictionary dictionary, ValidationOptions options, List<FixField> flatFields, List<ValidationIssue> issues, string msgType, string groupPath, int depth)
    {
        FixField? countField = FindCountField(flatFields, group);
        if (depth > FixDefaults.MaxGroupDepth)
        {
            AddGroupLimitIssue(issues, "group-depth-limit-exceeded", "Repeating-group nesting exceeds the supported depth.", group.CountTag, countField, groupPath, msgType);
            return;
        }
        if (group.Entries.Count > FixDefaults.MaxGroupEntries)
        {
            AddGroupLimitIssue(issues, "group-entry-limit-exceeded", "The repeating group contains more entries than the supported limit.", group.CountTag, countField, groupPath, msgType);
            return;
        }

        ValidateGroupCount(group, countField, issues, groupPath, msgType);
        if (group.Entries.Count == 0)
        {
            return;
        }
        int delimiterTag = GroupParser.ResolveDelimiterTag(definition, dictionary);
        GroupMemberLookupResult delimiterMember = GroupParser.FindGroupMember(definition, dictionary, delimiterTag);
        if (delimiterTag <= 0 || !delimiterMember.Found || delimiterMember.IsGroup)
        {
            AddGroupIssue(issues, "invalid-group-delimiter-definition", "The dictionary does not define a valid field delimiter for this group.", true, definition.CountTag, countField, groupPath, msgType);
            delimiterTag = 0;
        }
        else if (group.DelimiterTag != delimiterTag)
        {
            AddGroupIssue(issues, "invalid-group-delimiter", "The structured group delimiter does not match its dictionary definition.", true, delimiterTag, countField, groupPath, msgType);
        }

        for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
        {
            FixGroupEntry entry = group.Entries[entryIndex];
            string entryPath = groupPath + "[" + entryIndex.ToString() + "]";
            ValidateEntry(entry, definition, dictionary, options, flatFields, issues, msgType, entryPath, depth, delimiterTag);
        }
    }

    internal static void ValidateGroupCount(FixGroup group, FixField? countField, List<ValidationIssue> issues, string groupPath, string msgType)
    {
        bool invalidCount = group.DeclaredCount < 0;
        bool countMismatch = group.DeclaredCount != group.Entries.Count;
        if (countField != null)
        {
            ScalarParseResult parsed = ScalarParser.ParseScalar(countField.Value, FixFieldType.NumInGroup, int.MaxValue);
            if (!parsed.Success)
            {
                invalidCount = true;
            }
            else if (parsed.IntegerValue != group.DeclaredCount || parsed.IntegerValue != group.Entries.Count)
            {
                countMismatch = true;
            }
        }
        if (invalidCount)
        {
            AddGroupIssue(issues, "invalid-group-count", "The repeating-group count is not a valid non-negative integer.", true, group.CountTag, countField, groupPath, msgType);
        }
        else if (countMismatch)
        {
            AddGroupIssue(issues, "group-count-mismatch", "The declared repeating-group count does not match its structured entries.", true, group.CountTag, countField, groupPath, msgType);
        }
    }

    internal static void ValidateEntry(FixGroupEntry entry, FixGroupDefinition definition, FixDictionary dictionary, ValidationOptions options, List<FixField> flatFields, List<ValidationIssue> issues, string msgType, string entryPath, int depth, int delimiterTag)
    {
        List<GroupValidationMember> members = CreateEntryMembers(entry, definition, dictionary);
        if (delimiterTag > 0 && (members.Count == 0 || members[0].Tag != delimiterTag || members[0].Field == null))
        {
            FixField? firstField = members.Count == 0 ? null : ResolveMemberField(members[0], flatFields);
            AddGroupIssue(issues, "missing-group-delimiter", "A repeating-group entry does not begin with its required delimiter field.", true, delimiterTag, firstField, entryPath, msgType);
        }

        ValidateEntryMembers(members, definition, dictionary, options, flatFields, issues, msgType, entryPath);
        ValidateRequiredMembers(definition.Members, entry, dictionary, options, issues, msgType, entryPath, true, 0);
        for (int groupIndex = 0; groupIndex < entry.Groups.Count; groupIndex++)
        {
            FixGroup nested = entry.Groups[groupIndex];
            GroupMemberLookupResult lookup = GroupParser.FindGroupMember(definition, dictionary, nested.CountTag);
            if (!lookup.Found || !lookup.IsGroup || lookup.GroupDefinition == null)
            {
                continue;
            }
            string nestedPath = entryPath + "/" + nested.CountTag.ToString();
            ValidateGroup(nested, lookup.GroupDefinition, dictionary, options, flatFields, issues, msgType, nestedPath, depth + 1);
        }
    }

    internal static List<GroupValidationMember> CreateEntryMembers(FixGroupEntry entry, FixGroupDefinition definition, FixDictionary dictionary)
    {
        List<GroupValidationMember> members = [];
        bool allWireIndexes = entry.Fields.Count + entry.Groups.Count > 0;
        int sequence = 0;
        for (int index = 0; index < entry.Fields.Count; index++)
        {
            FixField field = entry.Fields[index];
            GroupMemberLookupResult lookup = GroupParser.FindGroupMember(definition, dictionary, field.Tag);
            GroupValidationMember member = new GroupValidationMember();
            member.Tag = field.Tag;
            member.Position = lookup.Found && !lookup.IsGroup ? lookup.Position : -1;
            member.WireIndex = field.WireIndex;
            member.Sequence = sequence;
            member.Field = field;
            members.Add(member);
            sequence++;
            if (field.WireIndex < 0)
            {
                allWireIndexes = false;
            }
        }
        for (int index = 0; index < entry.Groups.Count; index++)
        {
            FixGroup group = entry.Groups[index];
            GroupMemberLookupResult lookup = GroupParser.FindGroupMember(definition, dictionary, group.CountTag);
            GroupValidationMember member = new GroupValidationMember();
            member.Tag = group.CountTag;
            member.Position = lookup.Found && lookup.IsGroup ? lookup.Position : -1;
            member.WireIndex = group.WireIndex;
            member.Sequence = sequence;
            member.Group = group;
            members.Add(member);
            sequence++;
            if (group.WireIndex < 0)
            {
                allWireIndexes = false;
            }
        }
        if (allWireIndexes)
        {
            SortMembersByWireIndex(members);
        }
        else
        {
            PositionGroupsWithoutWireIndexes(members);
        }
        return members;
    }

    internal static void SortMembersByWireIndex(List<GroupValidationMember> members)
    {
        for (int index = 1; index < members.Count; index++)
        {
            GroupValidationMember current = members[index];
            int position = index;
            while (position > 0 && (members[position - 1].WireIndex > current.WireIndex || members[position - 1].WireIndex == current.WireIndex && members[position - 1].Sequence > current.Sequence))
            {
                members[position] = members[position - 1];
                position--;
            }
            members[position] = current;
        }
    }

    internal static void PositionGroupsWithoutWireIndexes(List<GroupValidationMember> members)
    {
        for (int index = 0; index < members.Count; index++)
        {
            GroupValidationMember current = members[index];
            if (current.Group == null || current.Position < 0)
            {
                continue;
            }
            int target = index;
            for (int candidate = 0; candidate < index; candidate++)
            {
                if (members[candidate].Position > current.Position)
                {
                    target = candidate;
                    break;
                }
            }
            if (target != index)
            {
                for (int move = index; move > target; move--)
                {
                    members[move] = members[move - 1];
                }
                members[target] = current;
            }
        }
    }

    internal static void ValidateEntryMembers(List<GroupValidationMember> members, FixGroupDefinition definition, FixDictionary dictionary, ValidationOptions options, List<FixField> flatFields, List<ValidationIssue> issues, string msgType, string entryPath)
    {
        int lastPosition = -1;
        for (int index = 0; index < members.Count; index++)
        {
            GroupValidationMember member = members[index];
            FixField? field = ResolveMemberField(member, flatFields);
            if (member.Position < 0)
            {
                AddGroupIssue(issues, "group-member-not-allowed", "The repeating-group entry contains a member not allowed by its dictionary definition.", true, member.Tag, field, entryPath, msgType);
                continue;
            }
            if (options.CheckFieldOrder && member.Position < lastPosition)
            {
                AddGroupIssue(issues, "group-member-out-of-order", "A repeating-group member appears before an earlier dictionary member.", true, member.Tag, field, entryPath, msgType);
            }
            else if (member.Position > lastPosition)
            {
                lastPosition = member.Position;
            }
            if (options.RejectDuplicateFields && HasEarlierEntryMember(members, index, member.Tag))
            {
                AddGroupIssue(issues, "duplicate-group-member", "A repeating-group entry member occurs more than once.", true, member.Tag, field, entryPath, msgType);
            }
        }
    }

    internal static bool HasEarlierEntryMember(List<GroupValidationMember> members, int exclusiveEnd, int tag)
    {
        for (int index = 0; index < exclusiveEnd; index++)
        {
            if (members[index].Tag == tag)
            {
                return true;
            }
        }
        return false;
    }

    internal static void ValidateRequiredMembers(List<FixMemberDefinition> members, FixGroupEntry entry, FixDictionary dictionary, ValidationOptions options, List<ValidationIssue> issues, string msgType, string entryPath, bool parentActive, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            AddGroupIssue(issues, "dictionary-expansion-limit", "Dictionary member expansion exceeded the supported nesting depth.", false, 0, null, entryPath, msgType);
            return;
        }
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            if (member.IsRemoved)
            {
                continue;
            }
            bool conditional = member.PresenceRule.Length != 0 || member.Presence == FixPresence.Conditional;
            bool required = !conditional && (member.Presence == FixPresence.Required || member.Presence == FixPresence.Constant);
            if (member.Kind == FixMemberKind.Field)
            {
                FixField? field = FindEntryField(entry, member.ReferenceId);
                if (options.CheckRequiredFields && parentActive && required && field == null)
                {
                    AddGroupIssue(issues, "required-group-member-missing", "A required repeating-group field is missing.", true, member.ReferenceId, null, entryPath, msgType);
                }
                if (!conditional && member.Presence == FixPresence.Forbidden && field != null)
                {
                    AddGroupIssue(issues, "forbidden-group-member-present", "A forbidden repeating-group field is present.", true, member.ReferenceId, field, entryPath, msgType);
                }
            }
            else if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? groupDefinition = GroupParser.FindGroupDefinition(dictionary, member.ReferenceId);
                FixGroup? group = groupDefinition == null ? null : FindEntryGroup(entry, groupDefinition.CountTag);
                int countTag = groupDefinition == null ? 0 : groupDefinition.CountTag;
                if (options.CheckRequiredFields && parentActive && required && group == null)
                {
                    AddGroupIssue(issues, "required-group-member-missing", "A required nested repeating group is missing.", countTag > 0, countTag, null, entryPath, msgType);
                }
                if (!conditional && member.Presence == FixPresence.Forbidden && group != null)
                {
                    AddGroupIssue(issues, "forbidden-group-member-present", "A forbidden nested repeating group is present.", true, countTag, null, entryPath, msgType);
                }
            }
            else if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = GroupParser.FindComponentDefinition(dictionary, member.ReferenceId);
                if (component == null)
                {
                    continue;
                }
                bool present = ComponentIsPresent(component.Members, entry, dictionary, depth + 1);
                if (!conditional && member.Presence == FixPresence.Forbidden && present)
                {
                    AddGroupIssue(issues, "forbidden-group-member-present", "A forbidden repeating-group component is present.", false, member.ReferenceId, null, entryPath, msgType);
                }
                bool active = parentActive && required || present;
                ValidateRequiredMembers(component.Members, entry, dictionary, options, issues, msgType, entryPath, active, depth + 1);
            }
        }
    }

    internal static bool ComponentIsPresent(List<FixMemberDefinition> members, FixGroupEntry entry, FixDictionary dictionary, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return false;
        }
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            if (member.Kind == FixMemberKind.Field && FindEntryField(entry, member.ReferenceId) != null)
            {
                return true;
            }
            if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? group = GroupParser.FindGroupDefinition(dictionary, member.ReferenceId);
                if (group != null && FindEntryGroup(entry, group.CountTag) != null)
                {
                    return true;
                }
            }
            if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = GroupParser.FindComponentDefinition(dictionary, member.ReferenceId);
                if (component != null && ComponentIsPresent(component.Members, entry, dictionary, depth + 1))
                {
                    return true;
                }
            }
        }
        return false;
    }

    internal static FixField? FindEntryField(FixGroupEntry entry, int tag)
    {
        for (int index = 0; index < entry.Fields.Count; index++)
        {
            if (entry.Fields[index].Tag == tag)
            {
                return entry.Fields[index];
            }
        }
        return null;
    }

    internal static FixGroup? FindEntryGroup(FixGroupEntry entry, int countTag)
    {
        for (int index = 0; index < entry.Groups.Count; index++)
        {
            if (entry.Groups[index].CountTag == countTag)
            {
                return entry.Groups[index];
            }
        }
        return null;
    }

    internal static FixField? FindCountField(List<FixField> fields, FixGroup group)
    {
        if (group.WireIndex < 0)
        {
            return null;
        }
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == group.CountTag && fields[index].WireIndex == group.WireIndex)
            {
                return fields[index];
            }
        }
        return null;
    }

    internal static FixField? ResolveMemberField(GroupValidationMember member, List<FixField> flatFields)
    {
        if (member.Field != null)
        {
            return member.Field;
        }
        FixGroup? group = member.Group;
        if (group == null)
        {
            return null;
        }
        return FindCountField(flatFields, group);
    }

    internal static void AddGroupIssue(List<ValidationIssue> issues, string code, string description, bool hasTag, int tag, FixField? field, string groupPath, string msgType)
    {
        ValidationIssue issue = new ValidationIssue();
        issue.Layer = ValidationLayer.Group;
        issue.Severity = ValidationSeverity.Error;
        issue.Code = code;
        issue.Message = description;
        issue.HasTag = hasTag;
        issue.Tag = tag;
        issue.GroupPath = groupPath;
        issue.MsgType = msgType;
        if (field != null && field.SourceOffset >= 0)
        {
            issue.HasSourceOffset = true;
            issue.SourceOffset = field.SourceOffset;
        }
        issues.Add(issue);
    }

    internal static void AddGroupLimitIssue(List<ValidationIssue> issues, string code, string description, int tag, FixField? field, string groupPath, string msgType)
    {
        AddGroupIssue(issues, code, description, true, tag, field, groupPath, msgType);
        issues[issues.Count - 1].Severity = ValidationSeverity.Fatal;
    }
}
