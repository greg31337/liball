using System;
using System.Collections.Generic;
using System.Text;

namespace fix;

internal static class CanonicalMessageEncoder
{
    internal static MessageEncodingResult EncodeCanonicalMessage(FixMessage message, FixDictionary dictionary, EncodeOptions options)
    {
        MessageEncodingResult result = new MessageEncodingResult();
        if (options.MaxMessageSize < 0 || options.MaxFieldSize < 0)
        {
            SetEncodingError(result, EncodeStatus.Invalid, 0, "invalid-limit");
            return result;
        }
        if (!FixFieldValueCodec.IsSupportedTextEncoding(options.TextEncodingName))
        {
            SetEncodingError(result, EncodeStatus.Invalid, 0, "unsupported-text-encoding");
            return result;
        }

        string beginString = ResolveEnvelopeText(message.BeginString, message.HeaderFields, FixTags.BeginString);
        string msgType = ResolveEnvelopeText(message.MsgType, message.HeaderFields, FixTags.MsgType);
        if (beginString.Length == 0)
        {
            SetEncodingError(result, EncodeStatus.Invalid, 0, "missing-begin-string");
            return result;
        }
        if (msgType.Length == 0)
        {
            SetEncodingError(result, EncodeStatus.Invalid, 0, "missing-msg-type");
            return result;
        }

        FieldSequenceResult header = BuildSectionSequence(message.HeaderFields, message.HeaderGroups, dictionary.Header, dictionary, options);
        if (!header.Success)
        {
            SetEncodingError(result, EncodeStatus.Invalid, 0, header.ErrorCode);
            return result;
        }
        FixMessageDefinition? definition = GroupParser.FindMessageDefinition(dictionary, msgType);
        List<FixMemberDefinition> bodyMembers = definition == null ? new List<FixMemberDefinition>() : definition.Members;
        FieldSequenceResult body = BuildSectionSequence(message.BodyFields, message.BodyGroups, bodyMembers, dictionary, options);
        if (!body.Success)
        {
            SetEncodingError(result, EncodeStatus.Invalid, 0, body.ErrorCode);
            return result;
        }
        FieldSequenceResult trailer = BuildSectionSequence(message.TrailerFields, message.TrailerGroups, dictionary.Trailer, dictionary, options);
        if (!trailer.Success)
        {
            SetEncodingError(result, EncodeStatus.Invalid, 0, trailer.ErrorCode);
            return result;
        }

        List<FixField> bodyFields = [];
        bodyFields.Add(CreateTextField(FixTags.MsgType, msgType));
        AppendNonEnvelopeFields(bodyFields, header.Fields, dictionary, options);
        AppendNonEnvelopeFields(bodyFields, body.Fields, dictionary, options);
        AppendNonEnvelopeFields(bodyFields, trailer.Fields, dictionary, options);
        List<byte> encodedBody = [];
        if (!AppendEncodedFields(encodedBody, bodyFields, dictionary, options, result))
        {
            return result;
        }

        List<byte> encoded = [];
        FixField beginField = CreateTextField(FixTags.BeginString, beginString);
        if (!AppendRegularField(encoded, beginField, FixFieldType.String, options, result))
        {
            return result;
        }
        if (options.RecalculateBodyLength)
        {
            FixField calculatedLength = CreateIntegerField(FixTags.BodyLength, encodedBody.Count, FixFieldType.Length);
            if (!AppendRegularField(encoded, calculatedLength, FixFieldType.Length, options, result))
            {
                return result;
            }
        }
        else
        {
            FixField? retainedLength = FindFirstField(message.HeaderFields, FixTags.BodyLength);
            if (retainedLength == null)
            {
                SetEncodingError(result, EncodeStatus.Invalid, 0, "missing-body-length");
                return result;
            }
            if (!AppendRegularField(encoded, retainedLength, FixFieldType.Length, options, result))
            {
                return result;
            }
        }
        AddByteList(encoded, encodedBody);

        if (options.RecalculateCheckSum)
        {
            int checksum = CalculateListChecksum(encoded);
            AppendChecksumField(encoded, checksum);
        }
        else
        {
            FixField? retainedChecksum = FindFirstField(message.TrailerFields, FixTags.CheckSum);
            if (retainedChecksum == null)
            {
                SetEncodingError(result, EncodeStatus.Invalid, 0, "missing-checksum");
                return result;
            }
            AsciiIntegerResult parsedChecksum = ChecksumCalculator.ParseChecksum(retainedChecksum.Value);
            if (!parsedChecksum.Success)
            {
                SetEncodingError(result, EncodeStatus.Invalid, 0, "invalid-checksum-field");
                return result;
            }
            AppendChecksumField(encoded, parsedChecksum.Value);
        }

        result.Required = encoded.Count;
        if (encoded.Count > options.MaxMessageSize)
        {
            SetEncodingError(result, EncodeStatus.LimitExceeded, encoded.Count, "limit-exceeded");
            return result;
        }
        result.Bytes = encoded.ToArray();
        result.Success = true;
        result.Status = EncodeStatus.Success;
        return result;
    }

    internal static FieldSequenceResult BuildSectionSequence(List<FixField> flatFields, List<FixGroup> groups, List<FixMemberDefinition> members, FixDictionary dictionary, EncodeOptions options)
    {
        FieldSequenceResult result = new FieldSequenceResult();
        if (options.PreserveFieldOrder)
        {
            for (int index = 0; index < flatFields.Count; index++)
            {
                result.Fields.Add(flatFields[index]);
            }
            if (options.FlattenGroups && groups.Count != 0)
            {
                result.Fields.Clear();
                AppendPreservedSection(result, flatFields, groups, dictionary);
            }
        }
        else
        {
            List<FixGroup> orderedGroups = options.FlattenGroups ? groups : new List<FixGroup>();
            AppendCanonicalSection(result, flatFields, orderedGroups, members, dictionary, 0);
        }
        if (result.ErrorCode.Length == 0)
        {
            result.Success = true;
        }
        return result;
    }

    internal static void AppendPreservedSection(FieldSequenceResult result, List<FixField> flatFields, List<FixGroup> groups, FixDictionary dictionary)
    {
        bool[] emittedGroups = new bool[groups.Count];
        for (int fieldIndex = 0; fieldIndex < flatFields.Count; fieldIndex++)
        {
            FixField field = flatFields[fieldIndex];
            int groupIndex = FindGroupAtField(groups, field);
            if (groupIndex >= 0)
            {
                if (!AppendFlattenedGroup(result, groups[groupIndex], dictionary, true, 1))
                {
                    return;
                }
                emittedGroups[groupIndex] = true;
                continue;
            }
            if (!IsRepresentedByGroups(field, groups))
            {
                result.Fields.Add(field);
            }
        }
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            if (!emittedGroups[groupIndex])
            {
                if (!AppendFlattenedGroup(result, groups[groupIndex], dictionary, true, 1))
                {
                    return;
                }
            }
        }
    }

    internal static void AppendCanonicalSection(FieldSequenceResult result, List<FixField> flatFields, List<FixGroup> groups, List<FixMemberDefinition> members, FixDictionary dictionary, int depth)
    {
        bool[] emittedFields = new bool[flatFields.Count];
        bool[] emittedGroups = new bool[groups.Count];
        AppendCanonicalMembers(result, flatFields, groups, members, dictionary, emittedFields, emittedGroups, depth);
        if (result.ErrorCode.Length != 0)
        {
            return;
        }
        for (int fieldIndex = 0; fieldIndex < flatFields.Count; fieldIndex++)
        {
            if (!emittedFields[fieldIndex] && !IsRepresentedByGroups(flatFields[fieldIndex], groups))
            {
                result.Fields.Add(flatFields[fieldIndex]);
            }
        }
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            if (!emittedGroups[groupIndex])
            {
                if (!AppendFlattenedGroup(result, groups[groupIndex], dictionary, false, depth + 1))
                {
                    return;
                }
            }
        }
    }

    internal static void AppendCanonicalMembers(FieldSequenceResult result, List<FixField> flatFields, List<FixGroup> groups, List<FixMemberDefinition> members, FixDictionary dictionary, bool[] emittedFields, bool[] emittedGroups, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            result.ErrorCode = "dictionary-component-depth-exceeded";
            return;
        }
        for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
        {
            FixMemberDefinition member = members[memberIndex];
            if (member.Kind == FixMemberKind.Field)
            {
                AppendMatchingFields(result.Fields, flatFields, emittedFields, groups, member.ReferenceId);
            }
            else if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = GroupParser.FindComponentDefinition(dictionary, member.ReferenceId);
                if (component != null)
                {
                    AppendCanonicalMembers(result, flatFields, groups, component.Members, dictionary, emittedFields, emittedGroups, depth + 1);
                    if (result.ErrorCode.Length != 0)
                    {
                        return;
                    }
                }
            }
            else if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? definition = GroupParser.FindGroupDefinition(dictionary, member.ReferenceId);
                if (definition != null)
                {
                    for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                    {
                        if (!emittedGroups[groupIndex] && groups[groupIndex].CountTag == definition.CountTag)
                        {
                            if (!AppendFlattenedGroup(result, groups[groupIndex], dictionary, false, depth + 1))
                            {
                                return;
                            }
                            emittedGroups[groupIndex] = true;
                        }
                    }
                    AppendMatchingFields(result.Fields, flatFields, emittedFields, groups, definition.CountTag);
                }
            }
        }
    }

    internal static bool AppendFlattenedGroup(FieldSequenceResult result, FixGroup group, FixDictionary dictionary, bool preserveFieldOrder, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            result.ErrorCode = "group-depth-limit-exceeded";
            return false;
        }
        FixField countField = CreateIntegerField(group.CountTag, group.Entries.Count, FixFieldType.NumInGroup);
        countField.WireIndex = group.WireIndex;
        result.Fields.Add(countField);
        for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
        {
            FixGroupEntry entry = group.Entries[entryIndex];
            int entryStart = result.Fields.Count;
            if (!preserveFieldOrder)
            {
                FixGroupDefinition? definition = FindGroupDefinitionByCountTag(dictionary, group.CountTag);
                if (definition != null)
                {
                    FieldSequenceResult canonicalEntry = new FieldSequenceResult();
                    AppendCanonicalSection(canonicalEntry, entry.Fields, entry.Groups, definition.Members, dictionary, depth);
                    if (canonicalEntry.ErrorCode.Length != 0)
                    {
                        result.ErrorCode = canonicalEntry.ErrorCode;
                        return false;
                    }
                    for (int canonicalIndex = 0; canonicalIndex < canonicalEntry.Fields.Count; canonicalIndex++)
                    {
                        result.Fields.Add(canonicalEntry.Fields[canonicalIndex]);
                    }
                    if (result.Fields.Count == entryStart || result.Fields[entryStart].Tag != group.DelimiterTag)
                    {
                        result.ErrorCode = "missing-group-delimiter";
                        return false;
                    }
                    continue;
                }
            }
            bool[] emittedNested = new bool[entry.Groups.Count];
            for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
            {
                FixField field = entry.Fields[fieldIndex];
                for (int nestedIndex = 0; nestedIndex < entry.Groups.Count; nestedIndex++)
                {
                    FixGroup nested = entry.Groups[nestedIndex];
                    if (!emittedNested[nestedIndex] && nested.WireIndex >= 0 && field.WireIndex >= 0 && nested.WireIndex < field.WireIndex)
                    {
                        if (!AppendFlattenedGroup(result, nested, dictionary, preserveFieldOrder, depth + 1))
                        {
                            return false;
                        }
                        emittedNested[nestedIndex] = true;
                    }
                }
                int groupAtField = FindGroupAtField(entry.Groups, field);
                if (groupAtField >= 0)
                {
                    if (!AppendFlattenedGroup(result, entry.Groups[groupAtField], dictionary, preserveFieldOrder, depth + 1))
                    {
                        return false;
                    }
                    emittedNested[groupAtField] = true;
                }
                else if (!IsRepresentedByGroups(field, entry.Groups))
                {
                    result.Fields.Add(field);
                }
            }
            for (int nestedIndex = 0; nestedIndex < entry.Groups.Count; nestedIndex++)
            {
                if (!emittedNested[nestedIndex])
                {
                    if (!AppendFlattenedGroup(result, entry.Groups[nestedIndex], dictionary, preserveFieldOrder, depth + 1))
                    {
                        return false;
                    }
                }
            }
            if (result.Fields.Count == entryStart || result.Fields[entryStart].Tag != group.DelimiterTag)
            {
                result.ErrorCode = "missing-group-delimiter";
                return false;
            }
        }
        return true;
    }

    internal static FixGroupDefinition? FindGroupDefinitionByCountTag(FixDictionary dictionary, int countTag)
    {
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            if (dictionary.Groups[index].CountTag == countTag)
            {
                return dictionary.Groups[index];
            }
        }
        return null;
    }

    internal static int FindGroupAtField(List<FixGroup> groups, FixField field)
    {
        for (int index = 0; index < groups.Count; index++)
        {
            if (groups[index].WireIndex >= 0 && groups[index].WireIndex == field.WireIndex && groups[index].CountTag == field.Tag)
            {
                return index;
            }
        }
        return -1;
    }

    internal static bool IsRepresentedByGroups(FixField field, List<FixGroup> groups)
    {
        for (int index = 0; index < groups.Count; index++)
        {
            if (IsRepresentedByGroup(field, groups[index]))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool IsRepresentedByGroup(FixField field, FixGroup group)
    {
        if (group.WireIndex >= 0 && group.WireIndex == field.WireIndex && group.CountTag == field.Tag)
        {
            return true;
        }
        for (int entryIndex = 0; entryIndex < group.Entries.Count; entryIndex++)
        {
            FixGroupEntry entry = group.Entries[entryIndex];
            for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
            {
                if (entry.Fields[fieldIndex] == field || field.WireIndex >= 0 && entry.Fields[fieldIndex].WireIndex == field.WireIndex)
                {
                    return true;
                }
            }
            if (IsRepresentedByGroups(field, entry.Groups))
            {
                return true;
            }
        }
        return false;
    }

    internal static void AppendMatchingFields(List<FixField> output, List<FixField> flatFields, bool[] emitted, List<FixGroup> groups, int tag)
    {
        for (int index = 0; index < flatFields.Count; index++)
        {
            if (!emitted[index] && flatFields[index].Tag == tag && !IsRepresentedByGroups(flatFields[index], groups))
            {
                output.Add(flatFields[index]);
                emitted[index] = true;
            }
        }
    }

    internal static void AppendNonEnvelopeFields(List<FixField> output, List<FixField> fields, FixDictionary dictionary, EncodeOptions options)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            if (field.Tag == FixTags.BeginString || field.Tag == FixTags.BodyLength || field.Tag == FixTags.MsgType || field.Tag == FixTags.CheckSum)
            {
                continue;
            }
            if (!options.IncludeUnknownFields && MessageBuilder.FindFieldDefinition(dictionary, field.Tag) == null)
            {
                continue;
            }
            output.Add(field);
        }
    }

    internal static bool AppendEncodedFields(List<byte> output, List<FixField> fields, FixDictionary dictionary, EncodeOptions options, MessageEncodingResult result)
    {
        int index = 0;
        while (index < fields.Count)
        {
            FixField field = fields[index];
            FixFieldDefinition? definition = MessageBuilder.FindFieldDefinition(dictionary, field.Tag);
            if (definition != null && definition.HasLengthTag && (definition.FieldType == FixFieldType.Data || definition.FieldType == FixFieldType.XmlData))
            {
                SetEncodingError(result, EncodeStatus.Invalid, 0, "data-field-not-adjacent");
                return false;
            }
            if (definition != null && definition.HasDataTag)
            {
                FixFieldDefinition? dataDefinition = MessageBuilder.FindFieldDefinition(dictionary, definition.DataTag);
                if (dataDefinition == null || index + 1 >= fields.Count || fields[index + 1].Tag != definition.DataTag)
                {
                    SetEncodingError(result, EncodeStatus.Invalid, 0, "data-field-not-adjacent");
                    return false;
                }
                FixField dataField = fields[index + 1];
                ScalarFormatResult measured = DataFieldScanner.WriteAssociatedData(definition, dataDefinition, dataField.Value, Span<byte>.Empty, options.MaxFieldSize);
                if (measured.ErrorCode != "destination-too-small")
                {
                    EncodeStatus status = measured.ErrorCode == "limit-exceeded" ? EncodeStatus.LimitExceeded : EncodeStatus.Invalid;
                    SetEncodingError(result, status, measured.Required, measured.ErrorCode);
                    return false;
                }
                byte[] pair = new byte[measured.Required];
                ScalarFormatResult written = DataFieldScanner.WriteAssociatedData(definition, dataDefinition, dataField.Value, pair, options.MaxFieldSize);
                if (!written.Success)
                {
                    SetEncodingError(result, EncodeStatus.Invalid, written.Required, written.ErrorCode);
                    return false;
                }
                AddByteArray(output, pair);
                index += 2;
                continue;
            }
            FixFieldType fieldType = definition == null ? FixFieldType.Unknown : definition.FieldType;
            if (!AppendRegularField(output, field, fieldType, options, result))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool AppendRegularField(List<byte> output, FixField field, FixFieldType fieldType, EncodeOptions options, MessageEncodingResult result)
    {
        if (field.Tag < 1 || field.Tag > FixDefaults.MaxTagNumber)
        {
            SetEncodingError(result, EncodeStatus.Invalid, 0, "invalid-tag");
            return false;
        }
        byte[] value = field.Value;
        if (fieldType != FixFieldType.Unknown)
        {
            FixFieldValueCodec codec = new FixFieldValueCodec();
            EncodeResult measured = codec.EncodeFieldValue(field, fieldType, Span<byte>.Empty, options);
            if (measured.Status != EncodeStatus.DestinationTooSmall)
            {
                SetEncodingError(result, measured.Status, measured.RequiredSize, measured.ErrorCode);
                return false;
            }
            value = new byte[measured.RequiredSize];
            EncodeResult encoded = codec.EncodeFieldValue(field, fieldType, value, options);
            if (encoded.Status != EncodeStatus.Success)
            {
                SetEncodingError(result, encoded.Status, encoded.RequiredSize, encoded.ErrorCode);
                return false;
            }
        }
        else
        {
            if (value.Length == 0)
            {
                SetEncodingError(result, EncodeStatus.Invalid, 1, "empty-value");
                return false;
            }
            if (value.Length > options.MaxFieldSize)
            {
                SetEncodingError(result, EncodeStatus.LimitExceeded, value.Length, "limit-exceeded");
                return false;
            }
        }
        AppendTag(output, field.Tag);
        output.Add(61);
        AddByteArray(output, value);
        output.Add(FixDefaults.Soh);
        return true;
    }

    internal static string ResolveEnvelopeText(string value, List<FixField> fields, int tag)
    {
        if (value.Length != 0)
        {
            return value;
        }
        FixField? field = FindFirstField(fields, tag);
        return field == null ? "" : Encoding.Latin1.GetString(field.Value);
    }

    internal static FixField? FindFirstField(List<FixField> fields, int tag)
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

    internal static FixField CreateTextField(int tag, string value)
    {
        FixField field = new FixField();
        field.Tag = tag;
        field.Value = Encoding.Latin1.GetBytes(value);
        return field;
    }

    internal static FixField CreateIntegerField(int tag, int value, FixFieldType fieldType)
    {
        byte[] buffer = new byte[11];
        ScalarFormatResult formatted = ScalarFormatter.FormatInteger(value, fieldType, buffer);
        FixField field = new FixField();
        field.Tag = tag;
        field.Value = buffer.AsSpan(0, formatted.Written).ToArray();
        return field;
    }

    internal static void AppendTag(List<byte> output, int tag)
    {
        byte[] buffer = new byte[6];
        AsciiIntegerResult written = AsciiDigits.WriteUnsignedInt(tag, buffer);
        for (int index = 0; index < written.Count; index++)
        {
            output.Add(buffer[index]);
        }
    }

    internal static void AppendChecksumField(List<byte> output, int checksum)
    {
        output.Add(49);
        output.Add(48);
        output.Add(61);
        byte[] digits = new byte[3];
        ChecksumCalculator.WriteChecksum(checksum, digits);
        AddByteArray(output, digits);
        output.Add(FixDefaults.Soh);
    }

    internal static int CalculateListChecksum(List<byte> bytes)
    {
        int checksum = 0;
        for (int index = 0; index < bytes.Count; index++)
        {
            checksum = (checksum + bytes[index]) & 255;
        }
        return checksum;
    }

    internal static void AddByteList(List<byte> output, List<byte> value)
    {
        for (int index = 0; index < value.Count; index++)
        {
            output.Add(value[index]);
        }
    }

    internal static void AddByteArray(List<byte> output, byte[] value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            output.Add(value[index]);
        }
    }

    internal static void SetEncodingError(MessageEncodingResult result, EncodeStatus status, int required, string errorCode)
    {
        result.Status = status;
        result.Required = required;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The canonical FIX message could not be encoded: {errorCode}.";
    }
}
