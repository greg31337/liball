using System;
using System.Text;

namespace http;

internal class StructuredFieldParser
{
    const int MaximumTopMembers = 1024;
    const int MaximumInnerMembers = 256;
    const int MaximumParameters = 256;

    byte[] input;
    int position;

    internal StructuredFieldParser(byte[] value)
    {
        if (value == null)
        {
            throw new HttpParseException(
                "The Structured Field value is null.",
                "invalid-structured-field",
                0);
        }

        input = value;
    }

    internal StructuredFieldMember ParseTopItem()
    {
        SkipLeadingSpaces();
        StructuredFieldMember result = ReadItem();
        SkipLeadingSpaces();
        RequireEnd();
        return result;
    }

    internal StructuredFieldValue ParseTopList()
    {
        SkipLeadingSpaces();
        StructuredFieldValue result = new StructuredFieldValue();
        result.IsList = true;
        if (position == input.Length)
        {
            return result;
        }

        int encountered = 0;
        while (position < input.Length)
        {
            encountered = encountered + 1;
            RequireWithinLimit(encountered, MaximumTopMembers, "structured-list-members");
            result.Members.Add(ReadItemOrInnerList());
            SkipOptionalWhitespace();
            if (position == input.Length)
            {
                return result;
            }

            if (input[position] != 0x2c)
            {
                throw CreateParseError();
            }

            position = position + 1;
            SkipOptionalWhitespace();
            if (position == input.Length)
            {
                throw CreateParseError();
            }
        }

        return result;
    }

    internal StructuredFieldValue ParseTopDictionary()
    {
        SkipLeadingSpaces();
        StructuredFieldValue result = new StructuredFieldValue();
        result.IsDictionary = true;
        if (position == input.Length)
        {
            return result;
        }

        int encountered = 0;
        while (position < input.Length)
        {
            encountered = encountered + 1;
            RequireWithinLimit(encountered, MaximumTopMembers, "structured-dictionary-members");
            string key = ReadKey();
            StructuredFieldMember member;
            if (position < input.Length && input[position] == 0x3d)
            {
                position = position + 1;
                member = ReadItemOrInnerList();
                member.HasExplicitDictionaryValue = true;
            }
            else
            {
                member = new StructuredFieldMember();
                member.ValueKind = HttpFieldValueKind.Boolean;
                member.BooleanValue = true;
                ReadParameters(member);
            }

            member.DictionaryKey = key;
            ReplaceDictionaryMember(result, member);
            SkipOptionalWhitespace();
            if (position == input.Length)
            {
                return result;
            }

            if (input[position] != 0x2c)
            {
                throw CreateParseError();
            }

            position = position + 1;
            SkipOptionalWhitespace();
            if (position == input.Length)
            {
                throw CreateParseError();
            }
        }

        return result;
    }

    StructuredFieldMember ReadItemOrInnerList()
    {
        if (position < input.Length && input[position] == 0x28)
        {
            return ReadInnerList();
        }

        return ReadItem();
    }

    StructuredFieldMember ReadInnerList()
    {
        if (position >= input.Length || input[position] != 0x28)
        {
            throw CreateParseError();
        }

        position = position + 1;
        StructuredFieldMember result = new StructuredFieldMember();
        result.IsInnerList = true;
        int encountered = 0;
        while (position < input.Length)
        {
            SkipLeadingSpaces();
            if (position < input.Length && input[position] == 0x29)
            {
                position = position + 1;
                ReadParameters(result);
                return result;
            }

            encountered = encountered + 1;
            RequireWithinLimit(encountered, MaximumInnerMembers, "structured-inner-list-members");
            result.InnerList.Add(ReadItem());
            if (position >= input.Length || input[position] != 0x20 && input[position] != 0x29)
            {
                throw CreateParseError();
            }
        }

        throw CreateParseError();
    }

    StructuredFieldMember ReadItem()
    {
        StructuredFieldMember result = ReadBareItem();
        ReadParameters(result);
        return result;
    }

    StructuredFieldMember ReadBareItem()
    {
        if (position >= input.Length)
        {
            throw CreateParseError();
        }

        byte first = input[position];
        if (first == 0x2d || IsDigit(first))
        {
            return ReadNumber();
        }

        if (first == 0x22)
        {
            return ReadString();
        }

        if (IsAlpha(first) || first == 0x2a)
        {
            return ReadToken();
        }

        if (first == 0x3a)
        {
            return ReadByteSequence();
        }

        if (first == 0x3f)
        {
            return ReadBoolean();
        }

        if (first == 0x40)
        {
            return ReadDate();
        }

        if (first == 0x25)
        {
            return ReadDisplayString();
        }

        throw CreateParseError();
    }

    StructuredFieldMember ReadNumber()
    {
        if (position >= input.Length)
        {
            throw CreateParseError();
        }

        bool negative = false;
        if (input[position] == 0x2d)
        {
            negative = true;
            position = position + 1;
        }

        int integerStart = position;
        long integer = 0;
        while (position < input.Length && IsDigit(input[position]))
        {
            integer = integer * 10 + input[position] - 0x30;
            position = position + 1;
            if (position - integerStart > 15)
            {
                throw CreateParseError();
            }
        }

        int integerDigits = position - integerStart;
        if (integerDigits == 0)
        {
            throw CreateParseError();
        }

        StructuredFieldMember result = new StructuredFieldMember();
        if (position >= input.Length || input[position] != 0x2e)
        {
            result.ValueKind = HttpFieldValueKind.Integer;
            result.IntegerValue = negative ? -integer : integer;
            return result;
        }

        if (integerDigits > 12)
        {
            throw CreateParseError();
        }

        position = position + 1;
        int fractionStart = position;
        int fraction = 0;
        while (position < input.Length && IsDigit(input[position]))
        {
            fraction = fraction * 10 + input[position] - 0x30;
            position = position + 1;
            if (position - fractionStart > 3)
            {
                throw CreateParseError();
            }
        }

        int fractionDigits = position - fractionStart;
        if (fractionDigits == 0)
        {
            throw CreateParseError();
        }

        if (fractionDigits == 1)
        {
            fraction = fraction * 100;
        }
        else if (fractionDigits == 2)
        {
            fraction = fraction * 10;
        }

        long scaled = integer * 1000 + fraction;
        result.ValueKind = HttpFieldValueKind.Decimal;
        result.DecimalThousandths = negative ? -scaled : scaled;
        return result;
    }

    StructuredFieldMember ReadString()
    {
        position = position + 1;
        BoundedByteBuffer decoded = new BoundedByteBuffer(input.Length - position);
        while (position < input.Length)
        {
            byte character = input[position];
            position = position + 1;
            if (character == 0x5c)
            {
                if (position >= input.Length || input[position] != 0x22 && input[position] != 0x5c)
                {
                    throw CreateParseError();
                }

                decoded.AppendByte(input[position]);
                position = position + 1;
            }
            else if (character == 0x22)
            {
                StructuredFieldMember result = new StructuredFieldMember();
                result.ValueKind = HttpFieldValueKind.String;
                result.TextValue = Encoding.ASCII.GetString(decoded.ToArray());
                return result;
            }
            else
            {
                if (character < 0x20 || character > 0x7e)
                {
                    throw CreateParseError();
                }

                decoded.AppendByte(character);
            }
        }

        throw CreateParseError();
    }

    StructuredFieldMember ReadToken()
    {
        int start = position;
        position = position + 1;
        while (position < input.Length && IsTokenCharacter(input[position]))
        {
            position = position + 1;
        }

        StructuredFieldMember result = new StructuredFieldMember();
        result.ValueKind = HttpFieldValueKind.Token;
        result.TextValue = Encoding.ASCII.GetString(input, start, position - start);
        return result;
    }

    StructuredFieldMember ReadByteSequence()
    {
        position = position + 1;
        int contentStart = position;
        while (position < input.Length && input[position] != 0x3a)
        {
            if (!IsBase64Character(input[position]))
            {
                throw CreateParseError();
            }

            position = position + 1;
        }

        if (position >= input.Length)
        {
            throw CreateParseError();
        }

        int contentEnd = position;
        position = position + 1;
        StructuredFieldMember result = new StructuredFieldMember();
        result.ValueKind = HttpFieldValueKind.ByteSequence;
        result.ByteSequenceValue = DecodeBase64(contentStart, contentEnd);
        return result;
    }

    StructuredFieldMember ReadBoolean()
    {
        position = position + 1;
        if (position >= input.Length || input[position] != 0x30 && input[position] != 0x31)
        {
            throw CreateParseError();
        }

        StructuredFieldMember result = new StructuredFieldMember();
        result.ValueKind = HttpFieldValueKind.Boolean;
        result.BooleanValue = input[position] == 0x31;
        position = position + 1;
        return result;
    }

    StructuredFieldMember ReadDate()
    {
        position = position + 1;
        StructuredFieldMember number = ReadNumber();
        if (number.ValueKind != HttpFieldValueKind.Integer)
        {
            throw CreateParseError();
        }

        number.ValueKind = HttpFieldValueKind.Date;
        number.DateSeconds = number.IntegerValue;
        number.IntegerValue = 0;
        return number;
    }

    StructuredFieldMember ReadDisplayString()
    {
        if (position + 1 >= input.Length || input[position + 1] != 0x22)
        {
            throw CreateParseError();
        }

        position = position + 2;
        BoundedByteBuffer decoded = new BoundedByteBuffer(input.Length - position);
        while (position < input.Length)
        {
            byte character = input[position];
            position = position + 1;
            if (character < 0x20 || character > 0x7e)
            {
                throw CreateParseError();
            }

            if (character == 0x25)
            {
                if (position + 1 >= input.Length || !IsLowerHex(input[position]) ||
                    !IsLowerHex(input[position + 1]))
                {
                    throw CreateParseError();
                }

                int high = GetHexValue(input[position]);
                int low = GetHexValue(input[position + 1]);
                decoded.AppendByte((byte)(high * 16 + low));
                position = position + 2;
            }
            else if (character == 0x22)
            {
                byte[] bytes = decoded.ToArray();
                if (!IsValidUtf8(bytes))
                {
                    throw CreateParseError();
                }

                StructuredFieldMember result = new StructuredFieldMember();
                result.ValueKind = HttpFieldValueKind.DisplayString;
                result.TextValue = Encoding.UTF8.GetString(bytes);
                return result;
            }
            else
            {
                decoded.AppendByte(character);
            }
        }

        throw CreateParseError();
    }

    void ReadParameters(StructuredFieldMember member)
    {
        int encountered = 0;
        while (position < input.Length && input[position] == 0x3b)
        {
            encountered = encountered + 1;
            RequireWithinLimit(encountered, MaximumParameters, "structured-parameters");
            position = position + 1;
            SkipLeadingSpaces();
            string key = ReadKey();
            StructuredFieldMember value;
            if (position < input.Length && input[position] == 0x3d)
            {
                position = position + 1;
                value = ReadBareItem();
            }
            else
            {
                value = new StructuredFieldMember();
                value.ValueKind = HttpFieldValueKind.Boolean;
                value.BooleanValue = true;
            }

            StructuredFieldParameter parameter = new StructuredFieldParameter();
            parameter.Name = key;
            parameter.BareItem = value;
            ReplaceParameter(member, parameter);
        }
    }

    string ReadKey()
    {
        if (position >= input.Length || !IsKeyFirst(input[position]))
        {
            throw CreateParseError();
        }

        int start = position;
        position = position + 1;
        while (position < input.Length && IsKeyCharacter(input[position]))
        {
            position = position + 1;
        }

        return Encoding.ASCII.GetString(input, start, position - start);
    }

    byte[] DecodeBase64(int start, int end)
    {
        int paddingStart = end;
        int index = start;
        while (index < end)
        {
            if (input[index] == 0x3d)
            {
                paddingStart = index;
                break;
            }

            index = index + 1;
        }

        index = paddingStart;
        while (index < end)
        {
            if (input[index] != 0x3d)
            {
                throw CreateParseError();
            }

            index = index + 1;
        }

        int coreLength = paddingStart - start;
        int suppliedPadding = end - paddingStart;
        int remainder = coreLength % 4;
        if (remainder == 1)
        {
            throw CreateParseError();
        }

        int requiredPadding = remainder == 0 ? 0 : 4 - remainder;
        if (suppliedPadding > requiredPadding)
        {
            throw CreateParseError();
        }

        long outputLength = (long)coreLength * 6 / 8;
        if (outputLength > int.MaxValue)
        {
            throw CreateLimitError("structured-byte-sequence", int.MaxValue, outputLength);
        }

        byte[] output = new byte[(int)outputLength];
        int outputIndex = 0;
        int bits = 0;
        int accumulator = 0;
        index = start;
        while (index < paddingStart)
        {
            accumulator = accumulator * 64 + GetBase64Value(input[index]);
            bits = bits + 6;
            if (bits >= 8)
            {
                bits = bits - 8;
                output[outputIndex] = (byte)(accumulator >> bits);
                outputIndex = outputIndex + 1;
                if (bits == 0)
                {
                    accumulator = 0;
                }
                else
                {
                    accumulator = accumulator & ((1 << bits) - 1);
                }
            }

            index = index + 1;
        }

        return output;
    }

    void ReplaceParameter(StructuredFieldMember member, StructuredFieldParameter parameter)
    {
        int index = 0;
        while (index < member.Parameters.Count)
        {
            if (member.Parameters[index].Name == parameter.Name)
            {
                member.Parameters[index] = parameter;
                return;
            }

            index = index + 1;
        }

        member.Parameters.Add(parameter);
    }

    void ReplaceDictionaryMember(StructuredFieldValue value, StructuredFieldMember member)
    {
        int index = 0;
        while (index < value.Members.Count)
        {
            if (value.Members[index].DictionaryKey == member.DictionaryKey)
            {
                value.Members[index] = member;
                return;
            }

            index = index + 1;
        }

        value.Members.Add(member);
    }

    void SkipLeadingSpaces()
    {
        while (position < input.Length && input[position] == 0x20)
        {
            position = position + 1;
        }
    }

    void SkipOptionalWhitespace()
    {
        while (position < input.Length && (input[position] == 0x20 || input[position] == 0x09))
        {
            position = position + 1;
        }
    }

    void RequireEnd()
    {
        if (position != input.Length)
        {
            throw CreateParseError();
        }
    }

    void RequireWithinLimit(int actual, int limit, string name)
    {
        if (actual > limit)
        {
            throw CreateLimitError(name, limit, actual);
        }
    }

    HttpParseException CreateParseError()
    {
        return new HttpParseException(
            "The Structured Field value is malformed.",
            "invalid-structured-field",
            position);
    }

    HttpLimitExceededException CreateLimitError(string name, long limit, long actual)
    {
        return new HttpLimitExceededException(
            "A Structured Field resource limit was exceeded.",
            name,
            limit,
            actual);
    }

    static bool IsAlpha(byte value)
    {
        return value >= 0x41 && value <= 0x5a || value >= 0x61 && value <= 0x7a;
    }

    static bool IsDigit(byte value)
    {
        return value >= 0x30 && value <= 0x39;
    }

    static bool IsKeyFirst(byte value)
    {
        return value >= 0x61 && value <= 0x7a || value == 0x2a;
    }

    static bool IsKeyCharacter(byte value)
    {
        return IsKeyFirst(value) || IsDigit(value) || value == 0x5f || value == 0x2d ||
            value == 0x2e;
    }

    static bool IsTokenCharacter(byte value)
    {
        return HttpGrammar.IsTokenCharacter(value) || value == 0x3a || value == 0x2f;
    }

    static bool IsBase64Character(byte value)
    {
        return IsAlpha(value) || IsDigit(value) || value == 0x2b || value == 0x2f ||
            value == 0x3d;
    }

    static int GetBase64Value(byte value)
    {
        if (value >= 0x41 && value <= 0x5a)
        {
            return value - 0x41;
        }

        if (value >= 0x61 && value <= 0x7a)
        {
            return value - 0x61 + 26;
        }

        if (value >= 0x30 && value <= 0x39)
        {
            return value - 0x30 + 52;
        }

        if (value == 0x2b)
        {
            return 62;
        }

        return 63;
    }

    static bool IsLowerHex(byte value)
    {
        return IsDigit(value) || value >= 0x61 && value <= 0x66;
    }

    static int GetHexValue(byte value)
    {
        if (IsDigit(value))
        {
            return value - 0x30;
        }

        return value - 0x61 + 10;
    }

    static bool IsValidUtf8(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            byte first = value[index];
            if (first <= 0x7f)
            {
                index = index + 1;
                continue;
            }

            if (first >= 0xc2 && first <= 0xdf)
            {
                if (index + 1 >= value.Length || !IsContinuation(value[index + 1]))
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (first >= 0xe0 && first <= 0xef)
            {
                if (index + 2 >= value.Length || !IsContinuation(value[index + 1]) ||
                    !IsContinuation(value[index + 2]))
                {
                    return false;
                }

                if (first == 0xe0 && value[index + 1] < 0xa0 ||
                    first == 0xed && value[index + 1] > 0x9f)
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (first >= 0xf0 && first <= 0xf4)
            {
                if (index + 3 >= value.Length || !IsContinuation(value[index + 1]) ||
                    !IsContinuation(value[index + 2]) || !IsContinuation(value[index + 3]))
                {
                    return false;
                }

                if (first == 0xf0 && value[index + 1] < 0x90 ||
                    first == 0xf4 && value[index + 1] > 0x8f)
                {
                    return false;
                }

                index = index + 4;
                continue;
            }

            return false;
        }

        return true;
    }

    static bool IsContinuation(byte value)
    {
        return value >= 0x80 && value <= 0xbf;
    }
}
