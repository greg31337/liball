using System.Collections.Generic;
using System.Text;

namespace fix;

/// <summary>Provides mutable operations over flat and structured FIX messages.</summary>
public class FixMessageOperations : IMessageOperations
{
    /// <summary>Creates FIX message operations.</summary>
    public FixMessageOperations()
    {
    }

    /// <summary>Creates a generic application message for a dictionary MsgType and scenario.</summary>
    /// <param name="dictionary">The application dictionary.</param>
    /// <param name="msgType">The exact dictionary MsgType.</param>
    /// <param name="scenario">The selected scenario, or empty to use the dictionary default.</param>
    /// <returns>The created message, matched definition, and application metadata.</returns>
    public ApplicationMessageResult CreateApplicationMessage(FixDictionary dictionary, string msgType, string scenario)
    {
        return ApplicationMessageOperations.CreateApplicationMessage(dictionary, msgType, scenario);
    }

    /// <summary>Identifies a generic application message in a dictionary and scenario.</summary>
    /// <param name="message">The message to identify.</param>
    /// <param name="dictionary">The application dictionary.</param>
    /// <param name="scenario">The selected scenario, or empty to use the dictionary default.</param>
    /// <returns>The inspected message, matched definition, and application metadata.</returns>
    public ApplicationMessageResult IdentifyApplicationMessage(FixMessage message, FixDictionary dictionary, string scenario)
    {
        return ApplicationMessageOperations.IdentifyApplicationMessage(message, dictionary, scenario);
    }

    /// <summary>Sets the first occurrence of a field or adds it to its routed section.</summary>
    /// <param name="message">The message to edit.</param>
    /// <param name="field">The field to set.</param>
    /// <returns>The explicit edit outcome.</returns>
    public MessageEditResult SetField(FixMessage message, FixField field)
    {
        if (!IsValidTag(field.Tag))
        {
            return CreateEditFailure(message, field.Tag, "invalid-tag");
        }
        if (field.Value == null)
        {
            return CreateEditFailure(message, field.Tag, "missing-field-value");
        }

        List<FixField> fields = SelectFieldSection(message, field.Tag);
        List<FixGroup> groups = SelectGroupSection(message, fields);
        FixField? existing = FindFlatField(fields, field.Tag);
        if (existing == null)
        {
            existing = FindAndRouteFlatField(message, fields, field.Tag);
        }
        if (existing == null)
        {
            existing = FindStructuredField(groups, field.Tag, 0);
        }
        if (existing == null)
        {
            existing = FindStructuredField(message.HeaderGroups, field.Tag, 0);
        }
        if (existing == null)
        {
            existing = FindStructuredField(message.BodyGroups, field.Tag, 0);
        }
        if (existing == null)
        {
            existing = FindStructuredField(message.TrailerGroups, field.Tag, 0);
        }
        if (existing == null)
        {
            if (field.WireIndex < 0)
            {
                field.WireIndex = FindNextWireIndex(message);
            }
            field.SourceOffset = -1;
            field.SourceLength = 0;
            fields.Add(field);
        }
        else
        {
            CopyFieldState(existing, field);
            UpdateFieldAliases(message.HeaderGroups, existing, field, 0);
            UpdateFieldAliases(message.BodyGroups, existing, field, 0);
            UpdateFieldAliases(message.TrailerGroups, existing, field, 0);
        }

        UpdateEnvelopeValue(message, field.Tag, field.Value);
        SynchronizeMessageGroups(message);
        ClearOriginalBytes(message);
        return CreateEditSuccess(message, field.Tag);
    }

    /// <summary>Removes every flat and structured occurrence of a field tag.</summary>
    /// <param name="message">The message to edit.</param>
    /// <param name="tag">The tag to remove.</param>
    /// <returns>The explicit edit outcome.</returns>
    public MessageEditResult RemoveField(FixMessage message, int tag)
    {
        if (!IsValidTag(tag))
        {
            return CreateEditFailure(message, tag, "invalid-tag");
        }

        List<FixGroup> removedHeader = [];
        List<FixGroup> removedBody = [];
        List<FixGroup> removedTrailer = [];
        int changes = RemoveMatchingGroups(message.HeaderGroups, tag, removedHeader, 0);
        changes += RemoveMatchingGroups(message.BodyGroups, tag, removedBody, 0);
        changes += RemoveMatchingGroups(message.TrailerGroups, tag, removedTrailer, 0);
        changes += RemoveGroupFlatFields(message.HeaderFields, removedHeader, tag);
        changes += RemoveGroupFlatFields(message.BodyFields, removedBody, tag);
        changes += RemoveGroupFlatFields(message.TrailerFields, removedTrailer, tag);
        changes += RemoveFlatFields(message.HeaderFields, tag);
        changes += RemoveFlatFields(message.BodyFields, tag);
        changes += RemoveFlatFields(message.TrailerFields, tag);
        changes += RemoveStructuredFields(message.HeaderGroups, tag, 0);
        changes += RemoveStructuredFields(message.BodyGroups, tag, 0);
        changes += RemoveStructuredFields(message.TrailerGroups, tag, 0);

        if (tag == FixTags.BeginString)
        {
            if (message.BeginString.Length != 0)
            {
                changes++;
            }
            message.BeginString = "";
        }
        else if (tag == FixTags.MsgType)
        {
            if (message.MsgType.Length != 0)
            {
                changes++;
            }
            message.MsgType = "";
        }
        if (changes != 0)
        {
            SynchronizeMessageGroups(message);
            ClearOriginalBytes(message);
        }
        return CreateEditSuccess(message, tag);
    }

    /// <summary>Adds a validated structured group and its synchronized flat representation to the body.</summary>
    /// <param name="message">The message to edit.</param>
    /// <param name="group">The group to add.</param>
    /// <returns>The explicit edit outcome.</returns>
    public MessageEditResult AddGroup(FixMessage message, FixGroup group)
    {
        MessageEditResult validation = new MessageEditResult();
        validation.Message = message;
        validation.AffectedTag = group.CountTag;
        if (!ValidateGroup(group, 1, validation))
        {
            return validation;
        }

        List<FixField> noFlatFields = [];
        SynchronizeGroup(group, noFlatFields, 1);
        int[] nextWireIndex = [FindNextWireIndex(message)];
        List<FixField> flattened = [];
        if (!AppendFlatGroup(flattened, group, nextWireIndex, 1, validation))
        {
            return validation;
        }
        for (int index = 0; index < flattened.Count; index++)
        {
            message.BodyFields.Add(flattened[index]);
        }
        message.BodyGroups.Add(group);
        ClearOriginalBytes(message);
        return CreateEditSuccess(message, group.CountTag);
    }

    /// <summary>Removes every structured group with the supplied count tag and its flat fields.</summary>
    /// <param name="message">The message to edit.</param>
    /// <param name="countTag">The group count tag to remove.</param>
    /// <returns>The explicit edit outcome.</returns>
    public MessageEditResult RemoveGroup(FixMessage message, int countTag)
    {
        if (!IsValidTag(countTag))
        {
            return CreateEditFailure(message, countTag, "invalid-tag");
        }

        List<FixGroup> removedHeader = [];
        List<FixGroup> removedBody = [];
        List<FixGroup> removedTrailer = [];
        int changes = RemoveMatchingGroups(message.HeaderGroups, countTag, removedHeader, 0);
        changes += RemoveMatchingGroups(message.BodyGroups, countTag, removedBody, 0);
        changes += RemoveMatchingGroups(message.TrailerGroups, countTag, removedTrailer, 0);
        changes += RemoveGroupFlatFields(message.HeaderFields, removedHeader, countTag);
        changes += RemoveGroupFlatFields(message.BodyFields, removedBody, countTag);
        changes += RemoveGroupFlatFields(message.TrailerFields, removedTrailer, countTag);
        if (changes != 0)
        {
            SynchronizeMessageGroups(message);
            ClearOriginalBytes(message);
        }
        return CreateEditSuccess(message, countTag);
    }

    /// <summary>Creates a deep independent copy while preserving flat-to-structured field aliases.</summary>
    /// <param name="message">The message to copy.</param>
    /// <returns>The explicit edit outcome containing the copy.</returns>
    public MessageEditResult CloneMessage(FixMessage message)
    {
        Dictionary<FixField, FixField> fieldCopies = new Dictionary<FixField, FixField>();
        Dictionary<FixGroup, FixGroup> groupCopies = new Dictionary<FixGroup, FixGroup>();
        FixMessage copy = new FixMessage();
        copy.BeginString = message.BeginString;
        copy.MsgType = message.MsgType;
        CloneFieldList(message.HeaderFields, copy.HeaderFields, fieldCopies);
        CloneFieldList(message.BodyFields, copy.BodyFields, fieldCopies);
        CloneFieldList(message.TrailerFields, copy.TrailerFields, fieldCopies);
        CloneGroupList(message.HeaderGroups, copy.HeaderGroups, fieldCopies, groupCopies, 0);
        CloneGroupList(message.BodyGroups, copy.BodyGroups, fieldCopies, groupCopies, 0);
        CloneGroupList(message.TrailerGroups, copy.TrailerGroups, fieldCopies, groupCopies, 0);
        copy.OriginalBytes = CopyBytes(message.OriginalBytes);
        return CreateEditSuccess(copy, 0);
    }

    internal static List<FixField> SelectFieldSection(FixMessage message, int tag)
    {
        if (IsHeaderTag(tag))
        {
            return message.HeaderFields;
        }
        if (IsTrailerTag(tag))
        {
            return message.TrailerFields;
        }
        if (ContainsTag(message.HeaderFields, tag))
        {
            return message.HeaderFields;
        }
        if (ContainsTag(message.TrailerFields, tag))
        {
            return message.TrailerFields;
        }
        return message.BodyFields;
    }

    internal static List<FixGroup> SelectGroupSection(FixMessage message, List<FixField> fields)
    {
        if (fields == message.HeaderFields)
        {
            return message.HeaderGroups;
        }
        if (fields == message.TrailerFields)
        {
            return message.TrailerGroups;
        }
        return message.BodyGroups;
    }

    internal static bool IsHeaderTag(int tag)
    {
        return tag == 8 || tag == 9 || tag == 34 || tag == 35 || tag == 43 || tag == 49 || tag == 50 || tag == 52 || tag == 56 || tag == 57 || tag == 90 || tag == 91 || tag == 97 || tag == 115 || tag == 116 || tag == 122 || tag == 128 || tag == 129 || tag == 142 || tag == 143 || tag == 144 || tag == 145 || tag == 212 || tag == 213 || tag == 347 || tag == 369 || tag == 370 || tag == 627 || tag == 628 || tag == 629 || tag == 630 || tag == 1128 || tag == 1129 || tag == 1156;
    }

    internal static bool IsTrailerTag(int tag)
    {
        return tag == 10 || tag == 89 || tag == 93;
    }

    internal static bool IsValidTag(int tag)
    {
        return tag >= 1 && tag <= FixDefaults.MaxTagNumber;
    }

    internal static bool ContainsTag(List<FixField> fields, int tag)
    {
        return FindFlatField(fields, tag) != null;
    }

    internal static FixField? FindFlatField(List<FixField> fields, int tag)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == tag)
            {
                return fields[index];
            }
        }
        return null;
    }

    internal static FixField? FindAndRouteFlatField(FixMessage message, List<FixField> target, int tag)
    {
        List<FixField>[] sections = [message.HeaderFields, message.BodyFields, message.TrailerFields];
        for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
        {
            List<FixField> section = sections[sectionIndex];
            if (section == target)
            {
                continue;
            }
            for (int fieldIndex = 0; fieldIndex < section.Count; fieldIndex++)
            {
                if (section[fieldIndex].Tag == tag)
                {
                    FixField found = section[fieldIndex];
                    section.RemoveAt(fieldIndex);
                    target.Add(found);
                    return found;
                }
            }
        }
        return null;
    }

    internal static FixField? FindStructuredField(List<FixGroup> groups, int tag, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return null;
        }
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            FixGroup group = groups[groupIndex];
            for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
            {
                FixGroupEntry entry = group.Entries[entryIndex];
                FixField? found = FindFlatField(entry.Fields, tag);
                if (found != null)
                {
                    return found;
                }
                found = FindStructuredField(entry.Groups, tag, depth + 1);
                if (found != null)
                {
                    return found;
                }
            }
        }
        return null;
    }

    internal static void CopyFieldState(FixField target, FixField source)
    {
        target.Value = source.Value;
        target.SourceOffset = -1;
        target.SourceLength = 0;
        target.IsUnknown = source.IsUnknown;
        target.IsEncodedData = source.IsEncodedData;
    }

    internal static void UpdateFieldAliases(List<FixGroup> groups, FixField existing, FixField replacement, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return;
        }
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            FixGroup group = groups[groupIndex];
            for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
            {
                FixGroupEntry entry = group.Entries[entryIndex];
                for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
                {
                    FixField candidate = entry.Fields[fieldIndex];
                    if (candidate == existing || candidate.Tag == existing.Tag && candidate.WireIndex >= 0 && candidate.WireIndex == existing.WireIndex)
                    {
                        CopyFieldState(candidate, replacement);
                    }
                }
                UpdateFieldAliases(entry.Groups, existing, replacement, depth + 1);
            }
        }
    }

    internal static void UpdateEnvelopeValue(FixMessage message, int tag, byte[] value)
    {
        if (tag == FixTags.BeginString)
        {
            message.BeginString = Encoding.Latin1.GetString(value);
        }
        else if (tag == FixTags.MsgType)
        {
            message.MsgType = Encoding.Latin1.GetString(value);
        }
    }

    internal static int RemoveFlatFields(List<FixField> fields, int tag)
    {
        int removed = 0;
        for (int index = fields.Count - 1; index >= 0; index--)
        {
            if (fields[index].Tag == tag)
            {
                fields.RemoveAt(index);
                removed++;
            }
        }
        return removed;
    }

    internal static int RemoveStructuredFields(List<FixGroup> groups, int tag, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return 0;
        }
        int removed = 0;
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            FixGroup group = groups[groupIndex];
            for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
            {
                FixGroupEntry entry = group.Entries[entryIndex];
                removed += RemoveFlatFields(entry.Fields, tag);
                removed += RemoveStructuredFields(entry.Groups, tag, depth + 1);
            }
        }
        return removed;
    }

    internal static int RemoveMatchingGroups(List<FixGroup> groups, int countTag, List<FixGroup> removed, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return 0;
        }
        int changes = 0;
        for (int groupIndex = groups.Count - 1; groupIndex >= 0; groupIndex--)
        {
            FixGroup group = groups[groupIndex];
            if (group.CountTag == countTag)
            {
                removed.Add(group);
                groups.RemoveAt(groupIndex);
                changes++;
                continue;
            }
            for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
            {
                changes += RemoveMatchingGroups(group.Entries[entryIndex].Groups, countTag, removed, depth + 1);
            }
        }
        return changes;
    }

    internal static int RemoveGroupFlatFields(List<FixField> fields, List<FixGroup> removed, int countTag)
    {
        if (removed.Count == 0)
        {
            return 0;
        }
        int changes = 0;
        for (int fieldIndex = fields.Count - 1; fieldIndex >= 0; fieldIndex--)
        {
            FixField field = fields[fieldIndex];
            bool remove = field.Tag == countTag;
            for (int groupIndex = 0; groupIndex < removed.Count && !remove; groupIndex++)
            {
                remove = CanonicalMessageEncoder.IsRepresentedByGroup(field, removed[groupIndex]);
            }
            if (remove)
            {
                fields.RemoveAt(fieldIndex);
                changes++;
            }
        }
        return changes;
    }

    internal static bool ValidateGroup(FixGroup group, int depth, MessageEditResult result)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            SetEditFailure(result, "group-depth-limit-exceeded");
            return false;
        }
        if (!IsValidTag(group.CountTag) || !IsValidTag(group.DelimiterTag) || group.CountTag == group.DelimiterTag)
        {
            SetEditFailure(result, "invalid-group-definition");
            return false;
        }
        for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
        {
            FixGroupEntry entry = group.Entries[entryIndex];
            if (entry.Fields.Count == 0 || entry.Fields[0].Tag != group.DelimiterTag)
            {
                SetEditFailure(result, "missing-group-delimiter");
                return false;
            }
            for (int nestedIndex = 0; nestedIndex < entry.Groups.Count; nestedIndex++)
            {
                if (!ValidateGroup(entry.Groups[nestedIndex], depth + 1, result))
                {
                    return false;
                }
            }
        }
        return true;
    }

    internal static void SynchronizeMessageGroups(FixMessage message)
    {
        SynchronizeGroupList(message.HeaderGroups, message.HeaderFields, 0);
        SynchronizeGroupList(message.BodyGroups, message.BodyFields, 0);
        SynchronizeGroupList(message.TrailerGroups, message.TrailerFields, 0);
    }

    internal static void SynchronizeGroupList(List<FixGroup> groups, List<FixField> flatFields, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return;
        }
        for (int index = 0; index < groups.Count; index++)
        {
            SynchronizeGroup(groups[index], flatFields, depth + 1);
        }
    }

    internal static void SynchronizeGroup(FixGroup group, List<FixField> flatFields, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return;
        }
        group.DeclaredCount = group.Entries.Count;
        UpdateFlatCountField(flatFields, group);
        for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
        {
            FixGroupEntry entry = group.Entries[entryIndex];
            entry.EntryIndex = entryIndex;
            for (int nestedIndex = 0; nestedIndex < entry.Groups.Count; nestedIndex++)
            {
                SynchronizeGroup(entry.Groups[nestedIndex], flatFields, depth + 1);
            }
        }
    }

    internal static void UpdateFlatCountField(List<FixField> fields, FixGroup group)
    {
        FixField? fallback = null;
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            if (field.Tag == group.CountTag)
            {
                if (field.WireIndex == group.WireIndex && group.WireIndex >= 0)
                {
                    field.Value = CreateCountValue(group.Entries.Count);
                    return;
                }
                if (fallback == null)
                {
                    fallback = field;
                }
            }
        }
        if (fallback != null)
        {
            fallback.Value = CreateCountValue(group.Entries.Count);
        }
    }

    internal static byte[] CreateCountValue(int count)
    {
        return CanonicalMessageEncoder.CreateIntegerField(1, count, FixFieldType.NumInGroup).Value;
    }

    internal static bool AppendFlatGroup(List<FixField> fields, FixGroup group, int[] nextWireIndex, int depth, MessageEditResult result)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            SetEditFailure(result, "group-depth-limit-exceeded");
            return false;
        }
        int originalGroupWireIndex = group.WireIndex;
        FixField countField = new FixField();
        countField.Tag = group.CountTag;
        countField.Value = CreateCountValue(group.Entries.Count);
        countField.WireIndex = nextWireIndex[0];
        nextWireIndex[0]++;
        group.WireIndex = countField.WireIndex;
        fields.Add(countField);
        for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
        {
            FixGroupEntry entry = group.Entries[entryIndex];
            bool[] emittedNested = new bool[entry.Groups.Count];
            for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
            {
                FixField field = entry.Fields[fieldIndex];
                int originalFieldWireIndex = field.WireIndex;
                for (int nestedIndex = 0; nestedIndex < entry.Groups.Count; nestedIndex++)
                {
                    FixGroup nested = entry.Groups[nestedIndex];
                    if (!emittedNested[nestedIndex] && nested.WireIndex >= 0 && originalFieldWireIndex >= 0 && nested.WireIndex < originalFieldWireIndex)
                    {
                        if (!AppendFlatGroup(fields, nested, nextWireIndex, depth + 1, result))
                        {
                            group.WireIndex = originalGroupWireIndex;
                            return false;
                        }
                        emittedNested[nestedIndex] = true;
                    }
                }
                int groupAtField = CanonicalMessageEncoder.FindGroupAtField(entry.Groups, field);
                if (groupAtField >= 0 && !emittedNested[groupAtField])
                {
                    if (!AppendFlatGroup(fields, entry.Groups[groupAtField], nextWireIndex, depth + 1, result))
                    {
                        group.WireIndex = originalGroupWireIndex;
                        return false;
                    }
                    emittedNested[groupAtField] = true;
                }
                else if (!CanonicalMessageEncoder.IsRepresentedByGroups(field, entry.Groups))
                {
                    field.WireIndex = nextWireIndex[0];
                    field.SourceOffset = -1;
                    field.SourceLength = 0;
                    nextWireIndex[0]++;
                    fields.Add(field);
                }
            }
            for (int nestedIndex = 0; nestedIndex < entry.Groups.Count; nestedIndex++)
            {
                if (!emittedNested[nestedIndex])
                {
                    if (!AppendFlatGroup(fields, entry.Groups[nestedIndex], nextWireIndex, depth + 1, result))
                    {
                        group.WireIndex = originalGroupWireIndex;
                        return false;
                    }
                }
            }
        }
        return true;
    }

    internal static int FindNextWireIndex(FixMessage message)
    {
        int maximum = -1;
        maximum = FindMaximumWireIndex(message.HeaderFields, maximum);
        maximum = FindMaximumWireIndex(message.BodyFields, maximum);
        maximum = FindMaximumWireIndex(message.TrailerFields, maximum);
        return maximum + 1;
    }

    internal static int FindMaximumWireIndex(List<FixField> fields, int maximum)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].WireIndex > maximum)
            {
                maximum = fields[index].WireIndex;
            }
        }
        return maximum;
    }

    internal static void CloneFieldList(List<FixField> source, List<FixField> destination, Dictionary<FixField, FixField> copies)
    {
        for (int index = 0; index < source.Count; index++)
        {
            destination.Add(CloneMappedField(source[index], copies));
        }
    }

    internal static FixField CloneMappedField(FixField source, Dictionary<FixField, FixField> copies)
    {
        if (copies.ContainsKey(source))
        {
            return copies[source];
        }
        FixField copy = new FixField();
        copy.Tag = source.Tag;
        copy.Value = CopyBytes(source.Value);
        copy.WireIndex = source.WireIndex;
        copy.SourceOffset = source.SourceOffset;
        copy.SourceLength = source.SourceLength;
        copy.IsUnknown = source.IsUnknown;
        copy.IsEncodedData = source.IsEncodedData;
        copies.Add(source, copy);
        return copy;
    }

    internal static void CloneGroupList(List<FixGroup> source, List<FixGroup> destination, Dictionary<FixField, FixField> fieldCopies, Dictionary<FixGroup, FixGroup> groupCopies, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return;
        }
        for (int index = 0; index < source.Count; index++)
        {
            destination.Add(CloneMappedGroup(source[index], fieldCopies, groupCopies, depth));
        }
    }

    internal static FixGroup CloneMappedGroup(FixGroup source, Dictionary<FixField, FixField> fieldCopies, Dictionary<FixGroup, FixGroup> groupCopies, int depth)
    {
        if (groupCopies.ContainsKey(source))
        {
            return groupCopies[source];
        }
        FixGroup copy = new FixGroup();
        copy.CountTag = source.CountTag;
        copy.DelimiterTag = source.DelimiterTag;
        copy.DeclaredCount = source.DeclaredCount;
        copy.WireIndex = source.WireIndex;
        groupCopies.Add(source, copy);
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return copy;
        }
        for (int entryIndex = 0; entryIndex < source.Entries.Count; entryIndex++)
        {
            FixGroupEntry sourceEntry = source.Entries[entryIndex];
            FixGroupEntry copyEntry = new FixGroupEntry();
            copyEntry.EntryIndex = sourceEntry.EntryIndex;
            CloneFieldList(sourceEntry.Fields, copyEntry.Fields, fieldCopies);
            CloneGroupList(sourceEntry.Groups, copyEntry.Groups, fieldCopies, groupCopies, depth + 1);
            copy.Entries.Add(copyEntry);
        }
        return copy;
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            copy[index] = source[index];
        }
        return copy;
    }

    internal static void ClearOriginalBytes(FixMessage message)
    {
        message.OriginalBytes = [];
    }

    internal static MessageEditResult CreateEditSuccess(FixMessage message, int affectedTag)
    {
        MessageEditResult result = new MessageEditResult();
        result.Success = true;
        result.Message = message;
        result.AffectedTag = affectedTag;
        return result;
    }

    internal static MessageEditResult CreateEditFailure(FixMessage message, int affectedTag, string errorCode)
    {
        MessageEditResult result = new MessageEditResult();
        result.Message = message;
        result.AffectedTag = affectedTag;
        SetEditFailure(result, errorCode);
        return result;
    }

    internal static void SetEditFailure(MessageEditResult result, string errorCode)
    {
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The FIX message could not be edited: {errorCode}.";
    }
}
