using System;
using System.Collections.Generic;

namespace fix;

/// <summary>Decodes complete TagValue frames into flat and structured FIX messages.</summary>
public class TagValueMessageDecoder : IMessageDecoder
{
    /// <summary>Creates a TagValue message decoder.</summary>
    public TagValueMessageDecoder()
    {
    }

    /// <summary>Decodes one message without retaining the caller's span.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="dictionary">The dictionary used to identify fields and groups.</param>
    /// <param name="options">The decode options and resource limits.</param>
    /// <returns>The explicit decode outcome.</returns>
    public DecodeResult DecodeMessage(ReadOnlySpan<byte> source, FixDictionary dictionary, DecodeOptions options)
    {
        DecodeResult optionFailure = ValidateOptions(options);
        if (optionFailure.ErrorCode.Length != 0)
        {
            return optionFailure;
        }

        FrameOptions frameOptions = new FrameOptions();
        frameOptions.MaxFrameSize = options.MaxMessageSize;
        frameOptions.VerifyBodyLength = options.VerifyBodyLength;
        frameOptions.VerifyCheckSum = options.VerifyCheckSum;
        FrameResult frame = TagValueFrameScanner.ScanFrame(source, frameOptions);
        if (frame.Status != FrameStatus.Complete)
        {
            return CreateFrameFailure(frame);
        }

        DecodeResult decoded = MessageBuilder.DecodeFlatMessage(source.Slice(0, frame.FrameLength), dictionary, options);
        if (decoded.Status != DecodeStatus.Success)
        {
            return decoded;
        }
        decoded.Consumed = frame.Consumed;
        decoded.Required = frame.Required;
        if (!ValidateEnvelope(decoded, dictionary, options))
        {
            return decoded;
        }
        if (!ValidateKnownFieldValues(decoded, dictionary, options))
        {
            return decoded;
        }

        FixMessage? possibleMessage = decoded.Message;
        if (possibleMessage == null)
        {
            SetDecodeError(decoded, DecodeStatus.Invalid, 0, frame.FrameLength, "missing-decoded-message");
            return decoded;
        }
        FixMessage message = possibleMessage;
        if (!options.PreserveUnknownFields)
        {
            RemoveUnknownFields(message.HeaderFields);
            RemoveUnknownFields(message.BodyFields);
            RemoveUnknownFields(message.TrailerFields);
        }
        GroupStructureResult groups = GroupParser.BuildGroups(message, dictionary, options);
        if (!groups.Success)
        {
            decoded.Status = groups.Status;
            decoded.ErrorOffset = groups.ErrorOffset;
            decoded.ErrorCode = groups.ErrorCode;
            decoded.ErrorMessage = groups.ErrorMessage;
            return decoded;
        }

        decoded.Status = DecodeStatus.Success;
        return decoded;
    }

    internal static DecodeResult ValidateOptions(DecodeOptions options)
    {
        DecodeResult result = new DecodeResult();
        if (options.MaxMessageSize < 0 || options.MaxFieldSize < 0 || options.MaxFieldsPerMessage < 0 || options.MaxGroupDepth < 0 || options.MaxGroupEntries < 0)
        {
            SetDecodeError(result, DecodeStatus.Invalid, 0, 0, "invalid-limit");
            return result;
        }
        if (!FixFieldValueCodec.IsSupportedTextEncoding(options.TextEncodingName))
        {
            SetDecodeError(result, DecodeStatus.Invalid, 0, 0, "unsupported-text-encoding");
            return result;
        }
        return result;
    }

    internal static DecodeResult CreateFrameFailure(FrameResult frame)
    {
        DecodeResult result = new DecodeResult();
        if (frame.Status == FrameStatus.Incomplete)
        {
            result.Status = DecodeStatus.Incomplete;
        }
        else if (frame.Status == FrameStatus.LimitExceeded)
        {
            result.Status = DecodeStatus.LimitExceeded;
        }
        else
        {
            result.Status = DecodeStatus.Invalid;
        }
        result.Consumed = frame.Consumed;
        result.Required = frame.Required;
        result.ErrorOffset = frame.ErrorOffset;
        result.ErrorCode = frame.ErrorCode;
        result.ErrorMessage = frame.ErrorMessage;
        return result;
    }

    internal static bool ValidateEnvelope(DecodeResult decoded, FixDictionary dictionary, DecodeOptions options)
    {
        int beginCount = 0;
        int lengthCount = 0;
        int typeCount = 0;
        int checksumCount = 0;
        for (int index = 0; index < decoded.FieldResults.Count; index++)
        {
            FixField? field = decoded.FieldResults[index].Field;
            if (field != null)
            {
                if (field.Tag == FixTags.BeginString)
                {
                    beginCount++;
                }
                else if (field.Tag == FixTags.BodyLength)
                {
                    lengthCount++;
                }
                else if (field.Tag == FixTags.MsgType)
                {
                    typeCount++;
                }
                else if (field.Tag == FixTags.CheckSum)
                {
                    checksumCount++;
                }
            }
        }
        if (beginCount == 0)
        {
            SetDecodeError(decoded, DecodeStatus.Invalid, 0, decoded.Required, "missing-begin-string");
            return false;
        }
        if (lengthCount == 0)
        {
            SetDecodeError(decoded, DecodeStatus.Invalid, 0, decoded.Required, "missing-body-length");
            return false;
        }
        if (typeCount == 0)
        {
            SetDecodeError(decoded, DecodeStatus.Invalid, 0, decoded.Required, "missing-msg-type");
            return false;
        }
        if (checksumCount == 0)
        {
            SetDecodeError(decoded, DecodeStatus.Invalid, decoded.Required - 1, decoded.Required, "missing-checksum");
            return false;
        }

        FixField? first = GetScannedField(decoded.FieldResults, 0);
        FixField? second = GetScannedField(decoded.FieldResults, 1);
        FixField? third = GetScannedField(decoded.FieldResults, 2);
        FixField? last = GetScannedField(decoded.FieldResults, decoded.FieldResults.Count - 1);
        if (first == null || first.Tag != FixTags.BeginString || second == null || second.Tag != FixTags.BodyLength || last == null || last.Tag != FixTags.CheckSum)
        {
            SetDecodeError(decoded, DecodeStatus.Invalid, 0, decoded.Required, "invalid-envelope-order");
            return false;
        }
        if (options.RequireCanonicalEnvelope)
        {
            if (third == null || third.Tag != FixTags.MsgType)
            {
                int offset = third == null ? 0 : third.SourceOffset;
                SetDecodeError(decoded, DecodeStatus.Invalid, offset, decoded.Required, "invalid-envelope-order");
                return false;
            }
            if (beginCount != 1 || lengthCount != 1 || typeCount != 1 || checksumCount != 1)
            {
                SetDecodeError(decoded, DecodeStatus.Invalid, 0, decoded.Required, "duplicate-envelope-field");
                return false;
            }
        }

        FixMessage? message = decoded.Message;
        if (message == null || message.MsgType.Length == 0)
        {
            SetDecodeError(decoded, DecodeStatus.Invalid, 0, decoded.Required, "empty-msg-type");
            return false;
        }
        if (dictionary.Identity.BeginString.Length != 0 && message.BeginString != dictionary.Identity.BeginString)
        {
            SetDecodeError(decoded, DecodeStatus.Invalid, first.SourceOffset, decoded.Required, "begin-string-mismatch");
            return false;
        }
        return true;
    }

    internal static bool ValidateKnownFieldValues(DecodeResult decoded, FixDictionary dictionary, DecodeOptions options)
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        for (int index = 0; index < decoded.FieldResults.Count; index++)
        {
            FixField? field = decoded.FieldResults[index].Field;
            if (field == null)
            {
                continue;
            }
            if (EnvelopeValidator.IsEnvelopeTag(field.Tag))
            {
                continue;
            }
            FixFieldDefinition? definition = MessageBuilder.FindFieldDefinition(dictionary, field.Tag);
            if (definition == null || definition.FieldType == FixFieldType.Unknown)
            {
                continue;
            }
            FieldParseResult parsed = codec.DecodeFieldValue(field.Value, field.Tag, definition.FieldType, options);
            if (parsed.Status != DecodeStatus.Success)
            {
                DecodeStatus status = parsed.Status == DecodeStatus.Incomplete ? DecodeStatus.Invalid : parsed.Status;
                int relativeOffset = parsed.ErrorOffset < 0 ? 0 : parsed.ErrorOffset;
                int valueOffset = field.SourceOffset + DataFieldScanner.CountDecimalDigits(field.Tag) + 1;
                SetDecodeError(decoded, status, valueOffset + relativeOffset, decoded.Required, parsed.ErrorCode);
                return false;
            }
        }
        return true;
    }

    internal static FixField? GetScannedField(List<FieldParseResult> fields, int index)
    {
        if (index < 0 || index >= fields.Count)
        {
            return null;
        }
        return fields[index].Field;
    }

    internal static void RemoveUnknownFields(List<FixField> fields)
    {
        int index = fields.Count - 1;
        while (index >= 0)
        {
            if (fields[index].IsUnknown)
            {
                fields.RemoveAt(index);
            }
            index--;
        }
    }

    internal static void SetDecodeError(DecodeResult result, DecodeStatus status, int errorOffset, int required, string errorCode)
    {
        result.Status = status;
        result.Required = required;
        result.ErrorOffset = errorOffset;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The TagValue message could not be decoded: {errorCode}.";
    }
}
