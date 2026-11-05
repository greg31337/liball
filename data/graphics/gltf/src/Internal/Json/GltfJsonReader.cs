using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfJsonReader
{
    internal byte[] Bytes = [];
    internal int Offset;
    internal GltfReadLimits Limits = new GltfReadLimits();
    internal GltfDuplicateMemberPolicy DuplicateMemberPolicy;
    internal GltfJsonReadResult Result = new GltfJsonReadResult();
    internal JsonPointerBuilder PointerBuilder = new JsonPointerBuilder();

    internal GltfJsonReadResult Read(
        byte[] jsonBytes,
        GltfReadLimits limits,
        GltfDuplicateMemberPolicy duplicateMemberPolicy)
    {
        if (jsonBytes == null)
        {
            throw new ArgumentNullException("jsonBytes");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        ValidateLimits(limits, duplicateMemberPolicy);
        if (jsonBytes.Length > limits.MaximumJsonByteLength)
        {
            throw new GltfParseException(
                $"JSON content length {jsonBytes.Length} exceeds the configured maximum {limits.MaximumJsonByteLength}.");
        }

        Bytes = jsonBytes;
        Limits = limits;
        DuplicateMemberPolicy = duplicateMemberPolicy;
        Result = new GltfJsonReadResult();
        Offset = 0;
        if (Bytes.Length >= 3 && Bytes[0] == 0xEF && Bytes[1] == 0xBB && Bytes[2] == 0xBF)
        {
            Offset = 3;
        }

        SkipWhitespace();
        if (Offset >= Bytes.Length)
        {
            throw CreateError("JSON input does not contain a value", Offset, "");
        }

        Result.Root = ReadValue("", 0);
        Result.HasRoot = true;
        SkipWhitespace();
        if (Offset != Bytes.Length)
        {
            throw CreateError("Unexpected content after the root JSON value", Offset, "");
        }

        return Result;
    }

    internal void ValidateLimits(
        GltfReadLimits limits,
        GltfDuplicateMemberPolicy duplicateMemberPolicy)
    {
        if (limits.MaximumJsonByteLength < 0)
        {
            throw new ArgumentOutOfRangeException("limits");
        }

        if (limits.MaximumJsonDepth < 0)
        {
            throw new ArgumentOutOfRangeException("limits");
        }

        if (limits.MaximumJsonTokenCount < 0)
        {
            throw new ArgumentOutOfRangeException("limits");
        }

        if (limits.MaximumJsonStringByteLength < 0)
        {
            throw new ArgumentOutOfRangeException("limits");
        }

        if (limits.MaximumJsonNumberByteLength < 0)
        {
            throw new ArgumentOutOfRangeException("limits");
        }

        if (duplicateMemberPolicy != GltfDuplicateMemberPolicy.Reject
            && duplicateMemberPolicy != GltfDuplicateMemberPolicy.KeepFirst
            && duplicateMemberPolicy != GltfDuplicateMemberPolicy.KeepLast)
        {
            throw new ArgumentOutOfRangeException("duplicateMemberPolicy");
        }
    }

    internal JsonNode? ReadValue(string jsonPointer, int depth)
    {
        SkipWhitespace();
        if (Offset >= Bytes.Length)
        {
            throw CreateError("Expected a JSON value", Offset, jsonPointer);
        }

        byte marker = Bytes[Offset];
        if (marker == 0x7B)
        {
            return ReadObject(jsonPointer, depth);
        }

        if (marker == 0x5B)
        {
            return ReadArray(jsonPointer, depth);
        }

        if (marker == 0x22)
        {
            GltfJsonString value = ReadString(jsonPointer);
            AddToken(
                JsonTokenType.String,
                jsonPointer,
                value.ByteOffset,
                value.ByteLength,
                value.Value,
                false,
                0.0,
                false);
            return JsonValue.Create(value.Value);
        }

        if (marker == 0x74)
        {
            return ReadLiteral("true", JsonTokenType.True, jsonPointer, JsonValue.Create(true));
        }

        if (marker == 0x66)
        {
            return ReadLiteral("false", JsonTokenType.False, jsonPointer, JsonValue.Create(false));
        }

        if (marker == 0x6E)
        {
            return ReadLiteral("null", JsonTokenType.Null, jsonPointer, null);
        }

        if (marker == 0x2D || (marker >= 0x30 && marker <= 0x39))
        {
            return ReadNumber(jsonPointer);
        }

        throw CreateError("Expected a JSON value", Offset, jsonPointer);
    }

    internal JsonObject ReadObject(string jsonPointer, int depth)
    {
        if (depth >= Limits.MaximumJsonDepth)
        {
            throw CreateError(
                $"JSON depth exceeds the configured maximum {Limits.MaximumJsonDepth}",
                Offset,
                jsonPointer);
        }

        int startOffset = Offset;
        Offset++;
        AddToken(JsonTokenType.StartObject, jsonPointer, startOffset, 1, null, false, 0.0, false);
        JsonObject value = new JsonObject();
        Dictionary<string, bool> memberNames = new Dictionary<string, bool>();
        SkipWhitespace();
        if (Offset < Bytes.Length && Bytes[Offset] == 0x7D)
        {
            int endOffset = Offset;
            Offset++;
            AddToken(JsonTokenType.EndObject, jsonPointer, endOffset, 1, null, false, 0.0, false);
            return value;
        }

        while (true)
        {
            SkipWhitespace();
            if (Offset >= Bytes.Length || Bytes[Offset] != 0x22)
            {
                throw CreateError("Expected a JSON object member name", Offset, jsonPointer);
            }

            GltfJsonString memberName = ReadString(jsonPointer);
            string memberPointer = PointerBuilder.AppendProperty(jsonPointer, memberName.Value);
            bool duplicateMember = memberNames.ContainsKey(memberName.Value);
            if (duplicateMember && DuplicateMemberPolicy == GltfDuplicateMemberPolicy.Reject)
            {
                throw CreateError(
                    $"Duplicate JSON object member '{memberName.Value}'",
                    memberName.ByteOffset,
                    memberPointer);
            }

            memberNames[memberName.Value] = true;
            AddToken(
                JsonTokenType.PropertyName,
                memberPointer,
                memberName.ByteOffset,
                memberName.ByteLength,
                memberName.Value,
                false,
                0.0,
                duplicateMember);
            SkipWhitespace();
            RequireByte(0x3A, "Expected ':' after the JSON object member name", memberPointer);
            Offset++;
            JsonNode? memberValue = ReadValue(memberPointer, depth + 1);
            if (!duplicateMember || DuplicateMemberPolicy == GltfDuplicateMemberPolicy.KeepLast)
            {
                value[memberName.Value] = memberValue;
            }

            SkipWhitespace();
            if (Offset >= Bytes.Length)
            {
                throw CreateError("JSON object is not terminated", Offset, jsonPointer);
            }

            if (Bytes[Offset] == 0x7D)
            {
                int endOffset = Offset;
                Offset++;
                AddToken(JsonTokenType.EndObject, jsonPointer, endOffset, 1, null, false, 0.0, false);
                return value;
            }

            if (Bytes[Offset] != 0x2C)
            {
                throw CreateError("Expected ',' or '}' after the JSON object member value", Offset, jsonPointer);
            }

            Offset++;
            SkipWhitespace();
            if (Offset < Bytes.Length && Bytes[Offset] == 0x7D)
            {
                throw CreateError("Trailing commas are not permitted in JSON objects", Offset, jsonPointer);
            }
        }
    }

    internal JsonArray ReadArray(string jsonPointer, int depth)
    {
        if (depth >= Limits.MaximumJsonDepth)
        {
            throw CreateError(
                $"JSON depth exceeds the configured maximum {Limits.MaximumJsonDepth}",
                Offset,
                jsonPointer);
        }

        int startOffset = Offset;
        Offset++;
        AddToken(JsonTokenType.StartArray, jsonPointer, startOffset, 1, null, false, 0.0, false);
        JsonArray value = new JsonArray();
        SkipWhitespace();
        if (Offset < Bytes.Length && Bytes[Offset] == 0x5D)
        {
            int endOffset = Offset;
            Offset++;
            AddToken(JsonTokenType.EndArray, jsonPointer, endOffset, 1, null, false, 0.0, false);
            return value;
        }

        int index = 0;
        while (true)
        {
            string itemPointer = PointerBuilder.AppendIndex(jsonPointer, index);
            JsonNode? itemValue = ReadValue(itemPointer, depth + 1);
            value.Add(itemValue);
            index++;
            SkipWhitespace();
            if (Offset >= Bytes.Length)
            {
                throw CreateError("JSON array is not terminated", Offset, jsonPointer);
            }

            if (Bytes[Offset] == 0x5D)
            {
                int endOffset = Offset;
                Offset++;
                AddToken(JsonTokenType.EndArray, jsonPointer, endOffset, 1, null, false, 0.0, false);
                return value;
            }

            if (Bytes[Offset] != 0x2C)
            {
                throw CreateError("Expected ',' or ']' after the JSON array value", Offset, jsonPointer);
            }

            Offset++;
            SkipWhitespace();
            if (Offset < Bytes.Length && Bytes[Offset] == 0x5D)
            {
                throw CreateError("Trailing commas are not permitted in JSON arrays", Offset, jsonPointer);
            }
        }
    }

    internal GltfJsonString ReadString(string jsonPointer)
    {
        int startOffset = Offset;
        Offset++;
        int decodedByteLength = 0;
        StringBuilder value = new StringBuilder();
        while (Offset < Bytes.Length)
        {
            byte current = Bytes[Offset];
            if (current == 0x22)
            {
                Offset++;
                GltfJsonString result = new GltfJsonString();
                result.Value = value.ToString();
                result.ByteOffset = startOffset;
                result.ByteLength = Offset - startOffset;
                result.DecodedByteLength = decodedByteLength;
                return result;
            }

            if (current == 0x5C)
            {
                int escapeOffset = Offset;
                Offset++;
                if (Offset >= Bytes.Length)
                {
                    throw CreateError("JSON string ends inside an escape sequence", escapeOffset, jsonPointer);
                }

                byte escapeMarker = Bytes[Offset];
                Offset++;
                if (escapeMarker == 0x22 || escapeMarker == 0x5C || escapeMarker == 0x2F)
                {
                    value.Append((char)escapeMarker);
                    decodedByteLength++;
                }
                else if (escapeMarker == 0x62)
                {
                    value.Append('\b');
                    decodedByteLength++;
                }
                else if (escapeMarker == 0x66)
                {
                    value.Append('\f');
                    decodedByteLength++;
                }
                else if (escapeMarker == 0x6E)
                {
                    value.Append('\n');
                    decodedByteLength++;
                }
                else if (escapeMarker == 0x72)
                {
                    value.Append('\r');
                    decodedByteLength++;
                }
                else if (escapeMarker == 0x74)
                {
                    value.Append('\t');
                    decodedByteLength++;
                }
                else if (escapeMarker == 0x75)
                {
                    int codeUnit = ReadHexCodeUnit(Offset, jsonPointer);
                    Offset += 4;
                    if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF)
                    {
                        if (Offset + 6 > Bytes.Length || Bytes[Offset] != 0x5C || Bytes[Offset + 1] != 0x75)
                        {
                            throw CreateError("JSON string contains an unpaired high surrogate", escapeOffset, jsonPointer);
                        }

                        int lowSurrogate = ReadHexCodeUnit(Offset + 2, jsonPointer);
                        if (lowSurrogate < 0xDC00 || lowSurrogate > 0xDFFF)
                        {
                            throw CreateError("JSON string contains an unpaired high surrogate", escapeOffset, jsonPointer);
                        }

                        Offset += 6;
                        int codePoint = 0x10000
                            + ((codeUnit - 0xD800) << 10)
                            + (lowSurrogate - 0xDC00);
                        value.Append(char.ConvertFromUtf32(codePoint));
                        decodedByteLength += 4;
                    }
                    else if (codeUnit >= 0xDC00 && codeUnit <= 0xDFFF)
                    {
                        throw CreateError("JSON string contains an unpaired low surrogate", escapeOffset, jsonPointer);
                    }
                    else
                    {
                        value.Append((char)codeUnit);
                        if (codeUnit <= 0x7F)
                        {
                            decodedByteLength++;
                        }
                        else if (codeUnit <= 0x7FF)
                        {
                            decodedByteLength += 2;
                        }
                        else
                        {
                            decodedByteLength += 3;
                        }
                    }
                }
                else
                {
                    throw CreateError("JSON string contains an invalid escape sequence", escapeOffset, jsonPointer);
                }

                EnsureStringLimit(decodedByteLength, startOffset, jsonPointer);
                continue;
            }

            if (current < 0x20)
            {
                throw CreateError("JSON string contains an unescaped control character", Offset, jsonPointer);
            }

            if (current < 0x80)
            {
                value.Append((char)current);
                decodedByteLength++;
                Offset++;
            }
            else
            {
                int sequenceByteLength = GetUtf8SequenceByteLength(Offset, jsonPointer);
                value.Append(Encoding.UTF8.GetString(Bytes, Offset, sequenceByteLength));
                decodedByteLength += sequenceByteLength;
                Offset += sequenceByteLength;
            }

            EnsureStringLimit(decodedByteLength, startOffset, jsonPointer);
        }

        throw CreateError("JSON string is not terminated", startOffset, jsonPointer);
    }

    internal JsonNode ReadNumber(string jsonPointer)
    {
        int startOffset = Offset;
        if (Bytes[Offset] == 0x2D)
        {
            Offset++;
            if (Offset >= Bytes.Length)
            {
                throw CreateError("JSON number is incomplete", startOffset, jsonPointer);
            }
        }

        if (Bytes[Offset] == 0x30)
        {
            Offset++;
            if (Offset < Bytes.Length && Bytes[Offset] >= 0x30 && Bytes[Offset] <= 0x39)
            {
                throw CreateError("JSON numbers cannot contain leading zeroes", Offset, jsonPointer);
            }
        }
        else if (Bytes[Offset] >= 0x31 && Bytes[Offset] <= 0x39)
        {
            Offset++;
            while (Offset < Bytes.Length && Bytes[Offset] >= 0x30 && Bytes[Offset] <= 0x39)
            {
                Offset++;
            }
        }
        else
        {
            throw CreateError("JSON number requires an integer part", Offset, jsonPointer);
        }

        if (Offset < Bytes.Length && Bytes[Offset] == 0x2E)
        {
            Offset++;
            if (Offset >= Bytes.Length || Bytes[Offset] < 0x30 || Bytes[Offset] > 0x39)
            {
                throw CreateError("JSON number requires a digit after the decimal point", Offset, jsonPointer);
            }

            while (Offset < Bytes.Length && Bytes[Offset] >= 0x30 && Bytes[Offset] <= 0x39)
            {
                Offset++;
            }
        }

        if (Offset < Bytes.Length && (Bytes[Offset] == 0x65 || Bytes[Offset] == 0x45))
        {
            Offset++;
            if (Offset < Bytes.Length && (Bytes[Offset] == 0x2B || Bytes[Offset] == 0x2D))
            {
                Offset++;
            }

            if (Offset >= Bytes.Length || Bytes[Offset] < 0x30 || Bytes[Offset] > 0x39)
            {
                throw CreateError("JSON number requires an exponent digit", Offset, jsonPointer);
            }

            while (Offset < Bytes.Length && Bytes[Offset] >= 0x30 && Bytes[Offset] <= 0x39)
            {
                Offset++;
            }
        }

        int byteLength = Offset - startOffset;
        if (byteLength > Limits.MaximumJsonNumberByteLength)
        {
            throw CreateError(
                $"JSON number byte length {byteLength} exceeds the configured maximum {Limits.MaximumJsonNumberByteLength}",
                startOffset,
                jsonPointer);
        }

        string text = Encoding.ASCII.GetString(Bytes, startOffset, byteLength);
        double numberValue = double.Parse(text, CultureInfo.InvariantCulture);
        if (!FiniteNumber.IsFiniteDouble(numberValue))
        {
            throw CreateError("JSON number is not finite", startOffset, jsonPointer);
        }

        AddToken(
            JsonTokenType.Number,
            jsonPointer,
            startOffset,
            byteLength,
            text,
            true,
            numberValue,
            false);
        return JsonValue.Create(numberValue);
    }

    internal JsonNode? ReadLiteral(
        string literal,
        JsonTokenType tokenType,
        string jsonPointer,
        JsonNode? value)
    {
        int startOffset = Offset;
        int index = 0;
        while (index < literal.Length)
        {
            int sourceOffset = startOffset + index;
            if (sourceOffset >= Bytes.Length || Bytes[sourceOffset] != (byte)literal[index])
            {
                throw CreateError($"Invalid JSON literal; expected '{literal}'", sourceOffset, jsonPointer);
            }

            index++;
        }

        Offset += literal.Length;
        AddToken(tokenType, jsonPointer, startOffset, literal.Length, literal, false, 0.0, false);
        return value;
    }

    internal void AddToken(
        JsonTokenType tokenType,
        string jsonPointer,
        int byteOffset,
        int byteLength,
        string? text,
        bool hasNumberValue,
        double numberValue,
        bool duplicateMember)
    {
        if (Result.Tokens.Count >= Limits.MaximumJsonTokenCount)
        {
            throw CreateError(
                $"JSON token count exceeds the configured maximum {Limits.MaximumJsonTokenCount}",
                byteOffset,
                jsonPointer);
        }

        GltfJsonToken token = new GltfJsonToken();
        token.Type = tokenType;
        token.JsonPointer = jsonPointer;
        token.ByteOffset = byteOffset;
        token.ByteLength = byteLength;
        token.Text = text;
        token.HasNumberValue = hasNumberValue;
        token.NumberValue = numberValue;
        token.DuplicateMember = duplicateMember;
        Result.Tokens.Add(token);
    }

    internal void SkipWhitespace()
    {
        while (Offset < Bytes.Length
            && (Bytes[Offset] == 0x20
                || Bytes[Offset] == 0x09
                || Bytes[Offset] == 0x0A
                || Bytes[Offset] == 0x0D))
        {
            Offset++;
        }
    }

    internal void RequireByte(byte expected, string message, string jsonPointer)
    {
        if (Offset >= Bytes.Length || Bytes[Offset] != expected)
        {
            throw CreateError(message, Offset, jsonPointer);
        }
    }

    internal int ReadHexCodeUnit(int startOffset, string jsonPointer)
    {
        if (startOffset + 4 > Bytes.Length)
        {
            throw CreateError("JSON Unicode escape is incomplete", startOffset, jsonPointer);
        }

        int value = 0;
        int index = 0;
        while (index < 4)
        {
            int digit = GetHexValue(Bytes[startOffset + index]);
            if (digit < 0)
            {
                throw CreateError("JSON Unicode escape contains a non-hexadecimal digit", startOffset + index, jsonPointer);
            }

            value = (value << 4) | digit;
            index++;
        }

        return value;
    }

    internal int GetHexValue(byte value)
    {
        if (value >= 0x30 && value <= 0x39)
        {
            return value - 0x30;
        }

        if (value >= 0x41 && value <= 0x46)
        {
            return value - 0x41 + 10;
        }

        if (value >= 0x61 && value <= 0x66)
        {
            return value - 0x61 + 10;
        }

        return -1;
    }

    internal int GetUtf8SequenceByteLength(int startOffset, string jsonPointer)
    {
        byte first = Bytes[startOffset];
        int byteLength = 0;
        if (first >= 0xC2 && first <= 0xDF)
        {
            byteLength = 2;
        }
        else if (first >= 0xE0 && first <= 0xEF)
        {
            byteLength = 3;
        }
        else if (first >= 0xF0 && first <= 0xF4)
        {
            byteLength = 4;
        }
        else
        {
            throw CreateError("JSON string contains invalid UTF-8", startOffset, jsonPointer);
        }

        if (startOffset + byteLength > Bytes.Length)
        {
            throw CreateError("JSON string contains truncated UTF-8", startOffset, jsonPointer);
        }

        int index = 1;
        while (index < byteLength)
        {
            if (Bytes[startOffset + index] < 0x80 || Bytes[startOffset + index] > 0xBF)
            {
                throw CreateError("JSON string contains invalid UTF-8", startOffset + index, jsonPointer);
            }

            index++;
        }

        byte second = Bytes[startOffset + 1];
        if ((first == 0xE0 && second < 0xA0)
            || (first == 0xED && second > 0x9F)
            || (first == 0xF0 && second < 0x90)
            || (first == 0xF4 && second > 0x8F))
        {
            throw CreateError("JSON string contains invalid UTF-8", startOffset, jsonPointer);
        }

        return byteLength;
    }

    internal void EnsureStringLimit(int decodedByteLength, int startOffset, string jsonPointer)
    {
        if (decodedByteLength > Limits.MaximumJsonStringByteLength)
        {
            throw CreateError(
                $"JSON string byte length {decodedByteLength} exceeds the configured maximum {Limits.MaximumJsonStringByteLength}",
                startOffset,
                jsonPointer);
        }
    }

    internal GltfParseException CreateError(string message, int byteOffset, string jsonPointer)
    {
        return new GltfParseException($"{message} at byte {byteOffset} and JSON pointer '{jsonPointer}'.");
    }
}
