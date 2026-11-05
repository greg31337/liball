using System.Collections.Generic;

namespace cbor;

internal class CborDomBuilder
{
    internal List<CborDomFrame> Frames = new List<CborDomFrame>();
    internal CborValue? Value;
    internal bool Completed;
    internal bool Failed;
    internal string? ErrorCode;
    internal string? ErrorMessage;

    internal CborDomBuilder()
    {
    }

    internal bool AddToken(CborToken token)
    {
        if (Completed || Failed)
        {
            return Fail(
                "CBOR_DOM_ALREADY_FINISHED",
                "The DOM builder cannot accept tokens after it has finished."
            );
        }

        switch (token.Kind)
        {
            case CborTokenKind.UnsignedInteger:
            case CborTokenKind.NegativeInteger:
            case CborTokenKind.ByteString:
            case CborTokenKind.TextString:
            case CborTokenKind.SimpleValue:
            case CborTokenKind.Float:
                return CompleteValue(CreateScalarValue(token));
            case CborTokenKind.StartByteString:
            case CborTokenKind.StartTextString:
            case CborTokenKind.StartArray:
            case CborTokenKind.StartMap:
            case CborTokenKind.Tag:
                return OpenValue(token);
            case CborTokenKind.ByteStringChunk:
            case CborTokenKind.TextStringChunk:
                return AddStringChunk(token);
            case CborTokenKind.EndByteString:
            case CborTokenKind.EndTextString:
            case CborTokenKind.EndArray:
            case CborTokenKind.EndMap:
                return CloseValue(token);
            default:
                return Fail(
                    "CBOR_DOM_INVALID_TOKEN",
                    "The token kind cannot be added to a CBOR value."
                );
        }
    }

    internal void Reset()
    {
        Frames.Clear();
        Value = null;
        Completed = false;
        Failed = false;
        ErrorCode = null;
        ErrorMessage = null;
    }

    internal CborValue CreateScalarValue(CborToken token)
    {
        CborValue value = new CborValue();
        value.LengthKind = token.LengthKind;
        value.Representation = token.Representation;
        switch (token.Kind)
        {
            case CborTokenKind.UnsignedInteger:
                value.Kind = CborValueKind.UnsignedInteger;
                value.IntegerValue = token.IntegerValue;
                break;
            case CborTokenKind.NegativeInteger:
                value.Kind = CborValueKind.NegativeInteger;
                value.IntegerValue = token.IntegerValue;
                break;
            case CborTokenKind.ByteString:
                value.Kind = CborValueKind.ByteString;
                value.ByteStringValue = token.ByteStringValue;
                break;
            case CborTokenKind.TextString:
                value.Kind = CborValueKind.TextString;
                value.TextStringValue = token.TextStringValue;
                break;
            case CborTokenKind.SimpleValue:
                value.Kind = CborValueKind.SimpleValue;
                value.SimpleValue = token.SimpleValue;
                break;
            case CborTokenKind.Float:
                value.Kind = CborValueKind.Float;
                value.FloatWidth = token.FloatWidth;
                value.FloatValue = token.FloatValue;
                break;
            default:
                break;
        }
        return value;
    }

    internal bool OpenValue(CborToken token)
    {
        CborValue value = new CborValue();
        value.LengthKind = token.LengthKind;
        value.Representation = token.Representation;
        CborDomFrame frame = new CborDomFrame();
        frame.Kind = token.Kind;
        frame.Value = value;
        switch (token.Kind)
        {
            case CborTokenKind.StartByteString:
                value.Kind = CborValueKind.ByteString;
                frame.ByteStringBytes = new List<byte>();
                break;
            case CborTokenKind.StartTextString:
                value.Kind = CborValueKind.TextString;
                frame.TextStringBuilder = new System.Text.StringBuilder();
                break;
            case CborTokenKind.StartArray:
                value.Kind = CborValueKind.Array;
                value.ArrayItems = new List<CborValue>();
                break;
            case CborTokenKind.StartMap:
                value.Kind = CborValueKind.Map;
                value.MapEntries = new List<CborMapEntry>();
                break;
            case CborTokenKind.Tag:
                value.Kind = CborValueKind.Tag;
                value.TagNumber = token.TagNumber;
                break;
            default:
                return Fail(
                    "CBOR_DOM_INVALID_START_TOKEN",
                    "The token cannot start a compound CBOR value."
                );
        }
        InitializeSourceRange(frame, token.Representation);
        Frames.Add(frame);
        return true;
    }

    internal bool AddStringChunk(CborToken token)
    {
        if (Frames.Count == 0)
        {
            return Fail(
                "CBOR_DOM_UNEXPECTED_STRING_CHUNK",
                "A string chunk requires an open indefinite-length string."
            );
        }
        CborDomFrame frame = Frames[Frames.Count - 1];
        if (token.Kind == CborTokenKind.ByteStringChunk)
        {
            if (frame.Kind != CborTokenKind.StartByteString
                || frame.ByteStringBytes == null
                || token.ByteStringValue == null)
            {
                return Fail(
                    "CBOR_DOM_UNEXPECTED_STRING_CHUNK",
                    "The byte string chunk does not match the open value."
                );
            }
            frame.ByteStringBytes.AddRange(token.ByteStringValue);
        }
        else
        {
            if (frame.Kind != CborTokenKind.StartTextString
                || frame.TextStringBuilder == null
                || token.TextStringValue == null)
            {
                return Fail(
                    "CBOR_DOM_UNEXPECTED_STRING_CHUNK",
                    "The text string chunk does not match the open value."
                );
            }
            frame.TextStringBuilder.Append(token.TextStringValue);
        }
        ExtendSourceRange(frame, token.Representation);
        return true;
    }

    internal bool CloseValue(CborToken token)
    {
        if (Frames.Count == 0)
        {
            return Fail(
                "CBOR_DOM_UNEXPECTED_END_TOKEN",
                "An end token requires an open CBOR value."
            );
        }
        CborDomFrame frame = Frames[Frames.Count - 1];
        if (!EndTokenMatches(frame.Kind, token.Kind))
        {
            return Fail(
                "CBOR_DOM_MISMATCHED_END_TOKEN",
                "The end token does not match the open CBOR value."
            );
        }
        if (frame.Kind == CborTokenKind.StartMap && frame.MapKey != null)
        {
            return Fail(
                "CBOR_DOM_INCOMPLETE_MAP_ENTRY",
                "A map cannot end without a value for its final key."
            );
        }
        if (frame.Value == null)
        {
            return Fail(
                "CBOR_DOM_VALUE_MISSING",
                "The open DOM frame does not contain a value."
            );
        }

        ExtendSourceRange(frame, token.Representation);
        if (frame.Kind == CborTokenKind.StartByteString)
        {
            if (frame.ByteStringBytes == null)
            {
                return Fail(
                    "CBOR_DOM_STRING_STATE_INVALID",
                    "The byte string frame has no chunk buffer."
                );
            }
            frame.Value.ByteStringValue = frame.ByteStringBytes.ToArray();
        }
        if (frame.Kind == CborTokenKind.StartTextString)
        {
            if (frame.TextStringBuilder == null)
            {
                return Fail(
                    "CBOR_DOM_STRING_STATE_INVALID",
                    "The text string frame has no chunk buffer."
                );
            }
            frame.Value.TextStringValue = frame.TextStringBuilder.ToString();
        }
        FinishSourceRange(frame);
        Frames.RemoveAt(Frames.Count - 1);
        return CompleteValue(frame.Value);
    }

    internal bool CompleteValue(CborValue completedValue)
    {
        CborValue currentValue = completedValue;
        while (Frames.Count > 0)
        {
            CborDomFrame frame = Frames[Frames.Count - 1];
            if (frame.Kind == CborTokenKind.Tag)
            {
                if (frame.Value == null)
                {
                    return Fail(
                        "CBOR_DOM_VALUE_MISSING",
                        "The tag frame does not contain a value."
                    );
                }
                frame.Value.TaggedValue = currentValue;
                ExtendSourceRange(frame, currentValue.Representation);
                FinishSourceRange(frame);
                Frames.RemoveAt(Frames.Count - 1);
                currentValue = frame.Value;
                continue;
            }
            if (frame.Kind == CborTokenKind.StartArray)
            {
                if (frame.Value == null || frame.Value.ArrayItems == null)
                {
                    return Fail(
                        "CBOR_DOM_ARRAY_STATE_INVALID",
                        "The array frame has no item collection."
                    );
                }
                frame.Value.ArrayItems.Add(currentValue);
                ExtendSourceRange(frame, currentValue.Representation);
                return true;
            }
            if (frame.Kind == CborTokenKind.StartMap)
            {
                if (frame.Value == null || frame.Value.MapEntries == null)
                {
                    return Fail(
                        "CBOR_DOM_MAP_STATE_INVALID",
                        "The map frame has no entry collection."
                    );
                }
                ExtendSourceRange(frame, currentValue.Representation);
                if (frame.MapKey == null)
                {
                    frame.MapKey = currentValue;
                    return true;
                }
                CborMapEntry entry = new CborMapEntry();
                entry.Key = frame.MapKey;
                entry.Value = currentValue;
                frame.Value.MapEntries.Add(entry);
                frame.MapKey = null;
                return true;
            }
            return Fail(
                "CBOR_DOM_UNEXPECTED_VALUE",
                "A completed value cannot be added inside an open string."
            );
        }

        Value = currentValue;
        Completed = true;
        return true;
    }

    internal static bool EndTokenMatches(CborTokenKind startKind, CborTokenKind endKind)
    {
        return (startKind == CborTokenKind.StartByteString
                && endKind == CborTokenKind.EndByteString)
            || (startKind == CborTokenKind.StartTextString
                && endKind == CborTokenKind.EndTextString)
            || (startKind == CborTokenKind.StartArray
                && endKind == CborTokenKind.EndArray)
            || (startKind == CborTokenKind.StartMap
                && endKind == CborTokenKind.EndMap);
    }

    internal static void InitializeSourceRange(
        CborDomFrame frame,
        CborRepresentation? representation)
    {
        if (representation == null
            || !representation.HasSourceRange
            || representation.SourceOffset < 0
            || representation.SourceLength < 0)
        {
            return;
        }
        if (representation.SourceLength <= long.MaxValue - representation.SourceOffset)
        {
            frame.SourceEnd = representation.SourceOffset + representation.SourceLength;
        }
        else
        {
            frame.SourceEnd = long.MaxValue;
        }
    }

    internal static void ExtendSourceRange(
        CborDomFrame frame,
        CborRepresentation? representation)
    {
        if (representation == null
            || !representation.HasSourceRange
            || representation.SourceOffset < 0
            || representation.SourceLength < 0)
        {
            return;
        }
        long sourceEnd;
        if (representation.SourceLength <= long.MaxValue - representation.SourceOffset)
        {
            sourceEnd = representation.SourceOffset + representation.SourceLength;
        }
        else
        {
            sourceEnd = long.MaxValue;
        }
        if (sourceEnd > frame.SourceEnd)
        {
            frame.SourceEnd = sourceEnd;
        }
    }

    internal static void FinishSourceRange(CborDomFrame frame)
    {
        if (frame.Value == null
            || frame.Value.Representation == null
            || !frame.Value.Representation.HasSourceRange)
        {
            return;
        }
        long sourceOffset = frame.Value.Representation.SourceOffset;
        if (frame.SourceEnd >= sourceOffset)
        {
            frame.Value.Representation.SourceLength = frame.SourceEnd - sourceOffset;
        }
    }

    internal bool Fail(string errorCode, string errorMessage)
    {
        Failed = true;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        return false;
    }
}
