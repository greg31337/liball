using System;
using System.Text;

namespace fix;

internal static class MessageBuilder
{
    internal static DecodeResult DecodeFlatMessage(ReadOnlySpan<byte> source, FixDictionary dictionary, DecodeOptions options)
    {
        DecodeResult result = new DecodeResult();
        if (options.MaxMessageSize < 0 || options.MaxFieldSize < 0 || options.MaxFieldsPerMessage < 0)
        {
            SetDecodeFailure(result, DecodeStatus.Invalid, 0, 0, "invalid-limit");
            return result;
        }
        if (source.Length > options.MaxMessageSize)
        {
            SetDecodeFailure(result, DecodeStatus.LimitExceeded, 0, source.Length, "limit-exceeded");
            return result;
        }
        if (source.Length == 0)
        {
            SetDecodeFailure(result, DecodeStatus.Incomplete, 0, 1, "incomplete");
            return result;
        }

        FixMessage message = new FixMessage();
        int offset = 0;
        int wireIndex = 0;
        while (offset < source.Length)
        {
            if (wireIndex >= options.MaxFieldsPerMessage)
            {
                SetDecodeFailure(result, DecodeStatus.LimitExceeded, offset, offset, "field-count-limit-exceeded");
                result.Consumed = offset;
                return result;
            }

            FieldParseResult scanned = FieldScanner.ScanField(source, offset, wireIndex, options.MaxFieldSize, options.AllowEmptyValues);
            if (scanned.Status != DecodeStatus.Success)
            {
                result.FieldResults.Add(scanned);
                CopyFieldFailure(result, scanned, offset);
                return result;
            }
            FixField? possibleField = scanned.Field;
            if (possibleField == null)
            {
                SetDecodeFailure(result, DecodeStatus.Invalid, offset, offset, "missing-scanned-field");
                return result;
            }
            FixField field = possibleField;
            FixFieldDefinition? definition = FindFieldDefinition(dictionary, field.Tag);
            field.IsUnknown = definition == null;

            if (definition != null && definition.HasLengthTag && (definition.FieldType == FixFieldType.Data || definition.FieldType == FixFieldType.XmlData))
            {
                FieldParseResult orphan = new FieldParseResult();
                FieldScanner.SetFieldError(orphan, DecodeStatus.Invalid, offset, offset, "data-field-not-adjacent");
                result.FieldResults.Add(orphan);
                CopyFieldFailure(result, orphan, offset);
                return result;
            }
            if (definition != null && definition.HasDataTag)
            {
                if (wireIndex + 1 >= options.MaxFieldsPerMessage)
                {
                    SetDecodeFailure(result, DecodeStatus.LimitExceeded, offset, offset, "field-count-limit-exceeded");
                    result.Consumed = offset;
                    return result;
                }
                FixFieldDefinition? dataDefinition = FindFieldDefinition(dictionary, definition.DataTag);
                if (dataDefinition == null)
                {
                    FieldParseResult missingDefinition = new FieldParseResult();
                    FieldScanner.SetFieldError(missingDefinition, DecodeStatus.Invalid, offset, offset, "missing-data-field-definition");
                    result.FieldResults.Add(missingDefinition);
                    CopyFieldFailure(result, missingDefinition, offset);
                    return result;
                }
                DataFieldScanResult dataPair = DataFieldScanner.ScanAssociatedData(source.Slice(offset), definition, dataDefinition, options.MaxFieldSize);
                if (!dataPair.Success)
                {
                    result.FieldResults.Add(scanned);
                    FieldParseResult pairFailure = CreatePairFailure(dataPair, offset);
                    result.FieldResults.Add(pairFailure);
                    CopyFieldFailure(result, pairFailure, offset);
                    return result;
                }

                AddField(message, field, dictionary);
                result.FieldResults.Add(scanned);
                int dataOffset = offset + scanned.Consumed;
                FixField dataField = new FixField();
                dataField.Tag = dataDefinition.Tag;
                dataField.Value = dataPair.Value;
                dataField.WireIndex = wireIndex + 1;
                dataField.SourceOffset = dataOffset;
                dataField.SourceLength = dataPair.Consumed - scanned.Consumed;
                dataField.IsEncodedData = true;
                AddField(message, dataField, dictionary);
                FieldParseResult dataSuccess = CreateFieldSuccess(dataField);
                result.FieldResults.Add(dataSuccess);
                offset += dataPair.Consumed;
                wireIndex += 2;
                continue;
            }

            AddField(message, field, dictionary);
            result.FieldResults.Add(scanned);
            offset += scanned.Consumed;
            wireIndex++;
        }

        if (options.PreserveOriginalBytes)
        {
            message.OriginalBytes = new byte[source.Length];
            source.CopyTo(message.OriginalBytes);
        }
        result.Status = DecodeStatus.Success;
        result.Message = message;
        result.Consumed = source.Length;
        result.Required = source.Length;
        return result;
    }

    internal static FixFieldDefinition? FindFieldDefinition(FixDictionary dictionary, int tag)
    {
        for (int index = 0; index < dictionary.Fields.Count; index++)
        {
            if (dictionary.Fields[index].Tag == tag)
            {
                return dictionary.Fields[index];
            }
        }
        return null;
    }

    internal static void AddField(FixMessage message, FixField field, FixDictionary dictionary)
    {
        if (field.Tag == FixTags.BeginString && message.BeginString.Length == 0)
        {
            message.BeginString = Encoding.Latin1.GetString(field.Value);
        }
        if (field.Tag == FixTags.MsgType && message.MsgType.Length == 0)
        {
            message.MsgType = Encoding.Latin1.GetString(field.Value);
        }
        if (IsHeaderField(dictionary, field.Tag))
        {
            message.HeaderFields.Add(field);
            return;
        }
        if (IsTrailerField(dictionary, field.Tag))
        {
            message.TrailerFields.Add(field);
            return;
        }
        message.BodyFields.Add(field);
    }

    internal static bool IsHeaderField(FixDictionary dictionary, int tag)
    {
        if (tag == FixTags.BeginString || tag == FixTags.BodyLength || tag == FixTags.MsgType)
        {
            return true;
        }
        for (int index = 0; index < dictionary.Header.Count; index++)
        {
            if (MemberContainsTag(dictionary, dictionary.Header[index], tag, 0))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool IsTrailerField(FixDictionary dictionary, int tag)
    {
        if (tag == FixTags.CheckSum)
        {
            return true;
        }
        for (int index = 0; index < dictionary.Trailer.Count; index++)
        {
            if (MemberContainsTag(dictionary, dictionary.Trailer[index], tag, 0))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool MemberContainsTag(FixDictionary dictionary, FixMemberDefinition member, int tag, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth)
        {
            return false;
        }
        if (member.Kind == FixMemberKind.Field)
        {
            return member.ReferenceId == tag;
        }
        if (member.Kind == FixMemberKind.Component)
        {
            for (int componentIndex = 0; componentIndex < dictionary.Components.Count; componentIndex++)
            {
                FixComponentDefinition component = dictionary.Components[componentIndex];
                if (component.Id == member.ReferenceId)
                {
                    for (int memberIndex = 0; memberIndex < component.Members.Count; memberIndex++)
                    {
                        if (MemberContainsTag(dictionary, component.Members[memberIndex], tag, depth + 1))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }
        if (member.Kind == FixMemberKind.Group)
        {
            for (int groupIndex = 0; groupIndex < dictionary.Groups.Count; groupIndex++)
            {
                FixGroupDefinition group = dictionary.Groups[groupIndex];
                if (group.Id == member.ReferenceId)
                {
                    if (group.CountTag == tag)
                    {
                        return true;
                    }
                    for (int memberIndex = 0; memberIndex < group.Members.Count; memberIndex++)
                    {
                        if (MemberContainsTag(dictionary, group.Members[memberIndex], tag, depth + 1))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }
        return false;
    }

    internal static FieldParseResult CreateFieldSuccess(FixField field)
    {
        FieldParseResult result = new FieldParseResult();
        result.Status = DecodeStatus.Success;
        result.Field = field;
        result.Consumed = field.SourceLength;
        result.Required = field.SourceLength;
        return result;
    }

    internal static FieldParseResult CreatePairFailure(DataFieldScanResult pair, int offset)
    {
        FieldParseResult result = new FieldParseResult();
        DecodeStatus status = DecodeStatus.Invalid;
        if (pair.ErrorCode == "incomplete")
        {
            status = DecodeStatus.Incomplete;
        }
        else if (pair.ErrorCode == "limit-exceeded")
        {
            status = DecodeStatus.LimitExceeded;
        }
        int errorOffset = pair.ErrorOffset < 0 ? offset : offset + pair.ErrorOffset;
        FieldScanner.SetFieldError(result, status, errorOffset, offset + pair.Required, pair.ErrorCode);
        return result;
    }

    internal static void CopyFieldFailure(DecodeResult result, FieldParseResult field, int consumed)
    {
        result.Status = field.Status;
        result.Consumed = consumed;
        result.Required = field.Required;
        result.ErrorOffset = field.ErrorOffset;
        result.ErrorCode = field.ErrorCode;
        result.ErrorMessage = field.ErrorMessage;
    }

    internal static void SetDecodeFailure(DecodeResult result, DecodeStatus status, int errorOffset, int required, string errorCode)
    {
        result.Status = status;
        result.ErrorOffset = errorOffset;
        result.Required = required;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The flat FIX message could not be decoded: {errorCode}.";
    }
}
