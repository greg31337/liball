using System;

namespace gltf;

internal class JsonSyntaxValidator
{
    internal byte[] Bytes = [];
    internal int Offset;
    internal int TokenCount;
    internal GltfValidationReport Report = new GltfValidationReport();

    internal GltfValidationReport ValidateJson(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        Bytes = bytes;
        Offset = 0;
        TokenCount = 0;
        Report = new GltfValidationReport();
        if (Bytes.Length > GltfConstants.DefaultMaximumJsonByteLength)
        {
            SetError(
                GltfIssueCodes.InputLimitExceeded,
                $"JSON content length {Bytes.Length} exceeds the supported maximum {GltfConstants.DefaultMaximumJsonByteLength}.",
                GltfConstants.DefaultMaximumJsonByteLength);
            return Report;
        }

        if (Bytes.Length >= 3
            && Bytes[0] == 0xEF
            && Bytes[1] == 0xBB
            && Bytes[2] == 0xBF)
        {
            Offset = 3;
        }

        SkipWhitespace();
        if (Offset >= Bytes.Length)
        {
            SetError(GltfIssueCodes.InvalidJson, "JSON input does not contain a value.", Offset);
            return Report;
        }

        ReadValue(0);
        if (!Report.IsValid)
        {
            return Report;
        }

        SkipWhitespace();
        if (Offset != Bytes.Length)
        {
            SetError(
                GltfIssueCodes.InvalidJson,
                "Unexpected content follows the root JSON value.",
                Offset);
        }

        return Report;
    }

    internal void ReadValue(int depth)
    {
        SkipWhitespace();
        if (Offset >= Bytes.Length)
        {
            SetError(GltfIssueCodes.InvalidJson, "Expected a JSON value.", Offset);
            return;
        }

        TokenCount++;
        if (TokenCount > GltfConstants.DefaultMaximumJsonTokenCount)
        {
            SetError(
                GltfIssueCodes.JsonTokenLimitExceeded,
                $"JSON token count exceeds the supported maximum {GltfConstants.DefaultMaximumJsonTokenCount}.",
                Offset);
            return;
        }

        byte marker = Bytes[Offset];
        if (marker == 0x7B)
        {
            ReadObject(depth);
        }
        else if (marker == 0x5B)
        {
            ReadArray(depth);
        }
        else if (marker == 0x22)
        {
            ReadString();
        }
        else if (marker == 0x74)
        {
            ReadLiteral("true");
        }
        else if (marker == 0x66)
        {
            ReadLiteral("false");
        }
        else if (marker == 0x6E)
        {
            ReadLiteral("null");
        }
        else if (marker == 0x2D || IsDigit(marker))
        {
            ReadNumber();
        }
        else
        {
            SetError(GltfIssueCodes.InvalidJson, "Expected a JSON value.", Offset);
        }
    }

    internal void ReadObject(int depth)
    {
        if (depth >= GltfConstants.DefaultMaximumJsonDepth)
        {
            SetError(
                GltfIssueCodes.JsonDepthLimitExceeded,
                $"JSON depth exceeds the supported maximum {GltfConstants.DefaultMaximumJsonDepth}.",
                Offset);
            return;
        }

        Offset++;
        SkipWhitespace();
        if (ConsumeByte(0x7D))
        {
            return;
        }

        while (Report.IsValid)
        {
            if (Offset >= Bytes.Length || Bytes[Offset] != 0x22)
            {
                SetError(
                    GltfIssueCodes.InvalidJson,
                    "Expected a JSON object member name.",
                    Offset);
                return;
            }

            ReadString();
            if (!Report.IsValid)
            {
                return;
            }

            SkipWhitespace();
            if (!ConsumeByte(0x3A))
            {
                SetError(
                    GltfIssueCodes.InvalidJson,
                    "Expected ':' after the JSON object member name.",
                    Offset);
                return;
            }

            ReadValue(depth + 1);
            if (!Report.IsValid)
            {
                return;
            }

            SkipWhitespace();
            if (ConsumeByte(0x7D))
            {
                return;
            }

            if (!ConsumeByte(0x2C))
            {
                SetError(
                    GltfIssueCodes.InvalidJson,
                    "Expected ',' or '}' after the JSON object member value.",
                    Offset);
                return;
            }

            SkipWhitespace();
            if (Offset < Bytes.Length && Bytes[Offset] == 0x7D)
            {
                SetError(
                    GltfIssueCodes.InvalidJson,
                    "Trailing commas are not permitted in JSON objects.",
                    Offset);
                return;
            }
        }
    }

    internal void ReadArray(int depth)
    {
        if (depth >= GltfConstants.DefaultMaximumJsonDepth)
        {
            SetError(
                GltfIssueCodes.JsonDepthLimitExceeded,
                $"JSON depth exceeds the supported maximum {GltfConstants.DefaultMaximumJsonDepth}.",
                Offset);
            return;
        }

        Offset++;
        SkipWhitespace();
        if (ConsumeByte(0x5D))
        {
            return;
        }

        while (Report.IsValid)
        {
            ReadValue(depth + 1);
            if (!Report.IsValid)
            {
                return;
            }

            SkipWhitespace();
            if (ConsumeByte(0x5D))
            {
                return;
            }

            if (!ConsumeByte(0x2C))
            {
                SetError(
                    GltfIssueCodes.InvalidJson,
                    "Expected ',' or ']' after the JSON array value.",
                    Offset);
                return;
            }

            SkipWhitespace();
            if (Offset < Bytes.Length && Bytes[Offset] == 0x5D)
            {
                SetError(
                    GltfIssueCodes.InvalidJson,
                    "Trailing commas are not permitted in JSON arrays.",
                    Offset);
                return;
            }
        }
    }

    internal void ReadString()
    {
        int startOffset = Offset;
        int decodedLength = 0;
        Offset++;
        while (Offset < Bytes.Length)
        {
            byte current = Bytes[Offset];
            if (current == 0x22)
            {
                Offset++;
                return;
            }

            if (current == 0x5C)
            {
                int escapeOffset = Offset;
                Offset++;
                if (Offset >= Bytes.Length)
                {
                    SetError(
                        GltfIssueCodes.InvalidJson,
                        "JSON string ends inside an escape sequence.",
                        escapeOffset);
                    return;
                }

                byte escape = Bytes[Offset];
                Offset++;
                if (escape == 0x22 || escape == 0x5C || escape == 0x2F
                    || escape == 0x62 || escape == 0x66 || escape == 0x6E
                    || escape == 0x72 || escape == 0x74)
                {
                    decodedLength++;
                }
                else if (escape == 0x75)
                {
                    int codeUnit = ReadHexCodeUnit();
                    if (!Report.IsValid)
                    {
                        return;
                    }

                    Offset += 4;
                    if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF)
                    {
                        if (Offset + 6 > Bytes.Length
                            || Bytes[Offset] != 0x5C
                            || Bytes[Offset + 1] != 0x75)
                        {
                            SetError(
                                GltfIssueCodes.InvalidJson,
                                "JSON string contains an unpaired high surrogate.",
                                escapeOffset);
                            return;
                        }

                        Offset += 2;
                        int lowSurrogate = ReadHexCodeUnit();
                        if (!Report.IsValid)
                        {
                            return;
                        }

                        if (lowSurrogate < 0xDC00 || lowSurrogate > 0xDFFF)
                        {
                            SetError(
                                GltfIssueCodes.InvalidJson,
                                "JSON string contains an unpaired high surrogate.",
                                escapeOffset);
                            return;
                        }

                        Offset += 4;
                        decodedLength += 4;
                    }
                    else if (codeUnit >= 0xDC00 && codeUnit <= 0xDFFF)
                    {
                        SetError(
                            GltfIssueCodes.InvalidJson,
                            "JSON string contains an unpaired low surrogate.",
                            escapeOffset);
                        return;
                    }
                    else if (codeUnit <= 0x7F)
                    {
                        decodedLength++;
                    }
                    else if (codeUnit <= 0x7FF)
                    {
                        decodedLength += 2;
                    }
                    else
                    {
                        decodedLength += 3;
                    }
                }
                else
                {
                    SetError(
                        GltfIssueCodes.InvalidJson,
                        "JSON string contains an invalid escape sequence.",
                        escapeOffset);
                    return;
                }
            }
            else if (current < 0x20)
            {
                SetError(
                    GltfIssueCodes.InvalidJson,
                    "JSON string contains an unescaped control character.",
                    Offset);
                return;
            }
            else if (current < 0x80)
            {
                Offset++;
                decodedLength++;
            }
            else
            {
                int sequenceLength = GetUtf8SequenceLength();
                if (!Report.IsValid)
                {
                    return;
                }

                Offset += sequenceLength;
                decodedLength += sequenceLength;
            }

            if (decodedLength > GltfConstants.DefaultMaximumJsonStringByteLength)
            {
                SetError(
                    GltfIssueCodes.JsonStringLimitExceeded,
                    $"JSON string byte length exceeds the supported maximum {GltfConstants.DefaultMaximumJsonStringByteLength}.",
                    startOffset);
                return;
            }
        }

        SetError(GltfIssueCodes.InvalidJson, "JSON string is not terminated.", startOffset);
    }

    internal void ReadNumber()
    {
        int startOffset = Offset;
        if (Bytes[Offset] == 0x2D)
        {
            Offset++;
            if (Offset >= Bytes.Length)
            {
                SetError(GltfIssueCodes.JsonNumberInvalid, "JSON number is incomplete.", startOffset);
                return;
            }
        }

        if (Bytes[Offset] == 0x30)
        {
            Offset++;
            if (Offset < Bytes.Length && IsDigit(Bytes[Offset]))
            {
                SetError(
                    GltfIssueCodes.JsonNumberInvalid,
                    "JSON numbers cannot contain leading zeroes.",
                    Offset);
                return;
            }
        }
        else if (Bytes[Offset] >= 0x31 && Bytes[Offset] <= 0x39)
        {
            while (Offset < Bytes.Length && IsDigit(Bytes[Offset]))
            {
                Offset++;
            }
        }
        else
        {
            SetError(
                GltfIssueCodes.JsonNumberInvalid,
                "JSON number requires an integer part.",
                Offset);
            return;
        }

        if (Offset < Bytes.Length && Bytes[Offset] == 0x2E)
        {
            Offset++;
            if (Offset >= Bytes.Length || !IsDigit(Bytes[Offset]))
            {
                SetError(
                    GltfIssueCodes.JsonNumberInvalid,
                    "JSON number requires a digit after the decimal point.",
                    Offset);
                return;
            }

            while (Offset < Bytes.Length && IsDigit(Bytes[Offset]))
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

            if (Offset >= Bytes.Length || !IsDigit(Bytes[Offset]))
            {
                SetError(
                    GltfIssueCodes.JsonNumberInvalid,
                    "JSON number requires an exponent digit.",
                    Offset);
                return;
            }

            while (Offset < Bytes.Length && IsDigit(Bytes[Offset]))
            {
                Offset++;
            }
        }

        if (Offset - startOffset > GltfConstants.DefaultMaximumJsonNumberByteLength)
        {
            SetError(
                GltfIssueCodes.JsonNumberInvalid,
                $"JSON number byte length exceeds the supported maximum {GltfConstants.DefaultMaximumJsonNumberByteLength}.",
                startOffset);
        }
        else if (WouldOverflowDouble(startOffset, Offset))
        {
            SetError(
                GltfIssueCodes.JsonNumberInvalid,
                "JSON number is outside the finite double-precision range.",
                startOffset);
        }
    }

    internal bool WouldOverflowDouble(int startOffset, int endOffset)
    {
        int mantissaStart = Bytes[startOffset] == 0x2D
            ? startOffset + 1
            : startOffset;
        int exponentMarker = mantissaStart;
        int decimalOffset = -1;
        while (exponentMarker < endOffset
            && Bytes[exponentMarker] != 0x65
            && Bytes[exponentMarker] != 0x45)
        {
            if (Bytes[exponentMarker] == 0x2E)
            {
                decimalOffset = exponentMarker;
            }

            exponentMarker++;
        }

        if (decimalOffset < 0)
        {
            decimalOffset = exponentMarker;
        }

        int explicitExponent = 0;
        int exponentSign = 1;
        int exponentOffset = exponentMarker + 1;
        if (exponentOffset < endOffset && Bytes[exponentOffset] == 0x2D)
        {
            exponentSign = -1;
            exponentOffset++;
        }
        else if (exponentOffset < endOffset && Bytes[exponentOffset] == 0x2B)
        {
            exponentOffset++;
        }

        while (exponentOffset < endOffset && explicitExponent < 10000)
        {
            explicitExponent = explicitExponent * 10 + Bytes[exponentOffset] - 0x30;
            exponentOffset++;
        }

        explicitExponent *= exponentSign;
        int significantIndex = -1;
        int digitIndex = 0;
        int offset = mantissaStart;
        while (offset < exponentMarker)
        {
            if (Bytes[offset] != 0x2E)
            {
                if (significantIndex < 0 && Bytes[offset] != 0x30)
                {
                    significantIndex = digitIndex;
                }

                digitIndex++;
            }

            offset++;
        }

        if (significantIndex < 0)
        {
            return false;
        }

        int integerDigitCount = decimalOffset - mantissaStart;
        int decimalExponent = integerDigitCount - significantIndex - 1 + explicitExponent;
        if (decimalExponent > 308)
        {
            return true;
        }

        if (decimalExponent < 308)
        {
            return false;
        }

        string maximumDigits = "17976931348623157";
        int maximumIndex = 0;
        int currentDigit = 0;
        offset = mantissaStart;
        while (offset < exponentMarker && maximumIndex < maximumDigits.Length)
        {
            if (Bytes[offset] != 0x2E)
            {
                if (currentDigit >= significantIndex)
                {
                    byte maximum = (byte)maximumDigits[maximumIndex];
                    if (Bytes[offset] > maximum)
                    {
                        return true;
                    }

                    if (Bytes[offset] < maximum)
                    {
                        return false;
                    }

                    maximumIndex++;
                }

                currentDigit++;
            }

            offset++;
        }

        while (offset < exponentMarker)
        {
            if (Bytes[offset] != 0x2E && Bytes[offset] != 0x30)
            {
                return true;
            }

            offset++;
        }

        return false;
    }

    internal void ReadLiteral(string literal)
    {
        int startOffset = Offset;
        int index = 0;
        while (index < literal.Length)
        {
            int sourceOffset = startOffset + index;
            if (sourceOffset >= Bytes.Length || Bytes[sourceOffset] != (byte)literal[index])
            {
                SetError(
                    GltfIssueCodes.InvalidJson,
                    $"Invalid JSON literal; expected '{literal}'.",
                    sourceOffset);
                return;
            }

            index++;
        }

        Offset += literal.Length;
    }

    internal int ReadHexCodeUnit()
    {
        if (Offset + 4 > Bytes.Length)
        {
            SetError(
                GltfIssueCodes.InvalidJson,
                "JSON Unicode escape is incomplete.",
                Offset);
            return 0;
        }

        int result = 0;
        int index = 0;
        while (index < 4)
        {
            int value = GetHexValue(Bytes[Offset + index]);
            if (value < 0)
            {
                SetError(
                    GltfIssueCodes.InvalidJson,
                    "JSON Unicode escape contains a non-hexadecimal digit.",
                    Offset + index);
                return 0;
            }

            result = result * 16 + value;
            index++;
        }

        return result;
    }

    internal int GetUtf8SequenceLength()
    {
        int startOffset = Offset;
        byte first = Bytes[startOffset];
        int length = 0;
        if (first >= 0xC2 && first <= 0xDF)
        {
            length = 2;
        }
        else if (first >= 0xE0 && first <= 0xEF)
        {
            length = 3;
        }
        else if (first >= 0xF0 && first <= 0xF4)
        {
            length = 4;
        }
        else
        {
            SetError(GltfIssueCodes.InvalidJson, "JSON string contains invalid UTF-8.", startOffset);
            return 0;
        }

        if (startOffset + length > Bytes.Length)
        {
            SetError(GltfIssueCodes.InvalidJson, "JSON string contains truncated UTF-8.", startOffset);
            return 0;
        }

        int index = 1;
        while (index < length)
        {
            byte continuation = Bytes[startOffset + index];
            if (continuation < 0x80 || continuation > 0xBF)
            {
                SetError(GltfIssueCodes.InvalidJson, "JSON string contains invalid UTF-8.", startOffset);
                return 0;
            }

            index++;
        }

        byte second = Bytes[startOffset + 1];
        if ((first == 0xE0 && second < 0xA0)
            || (first == 0xED && second > 0x9F)
            || (first == 0xF0 && second < 0x90)
            || (first == 0xF4 && second > 0x8F))
        {
            SetError(GltfIssueCodes.InvalidJson, "JSON string contains invalid UTF-8.", startOffset);
            return 0;
        }

        return length;
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

    internal bool IsDigit(byte value)
    {
        return value >= 0x30 && value <= 0x39;
    }

    internal bool ConsumeByte(byte value)
    {
        if (Offset < Bytes.Length && Bytes[Offset] == value)
        {
            Offset++;
            return true;
        }

        return false;
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

    internal void SetError(string code, string message, int byteOffset)
    {
        if (!Report.IsValid)
        {
            return;
        }

        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.HasByteOffset = true;
        issue.ByteOffset = byteOffset;
        Report.Issues.Add(issue);
        Report.IsValid = false;
    }
}
