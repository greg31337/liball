using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace http;

internal static class StructuredFieldWriter
{
    const long MaximumNumericValue = 999999999999999;

    internal static byte[] SerializeValue(StructuredFieldValue value)
    {
        if (value == null)
        {
            throw CreateFieldError();
        }

        int kinds = (value.IsItem ? 1 : 0) + (value.IsList ? 1 : 0) +
            (value.IsDictionary ? 1 : 0);
        if (kinds != 1 || value.Members == null)
        {
            throw CreateFieldError();
        }

        BoundedByteBuffer output = new BoundedByteBuffer(int.MaxValue);
        if (value.IsItem)
        {
            if (value.Item == null || value.Members.Count != 0)
            {
                throw CreateFieldError();
            }

            WriteItem(value.Item, output, false);
        }
        else
        {
            if (value.Item != null)
            {
                throw CreateFieldError();
            }

            if (value.IsList)
            {
                WriteList(value.Members, output);
            }
            else
            {
                WriteDictionary(value.Members, output);
            }
        }

        return output.ToArray();
    }

    static void WriteList(List<StructuredFieldMember> members, BoundedByteBuffer output)
    {
        int index = 0;
        while (index < members.Count)
        {
            StructuredFieldMember member = members[index];
            if (member == null || member.DictionaryKey != null)
            {
                throw CreateFieldError();
            }

            if (index > 0)
            {
                AppendAscii(", ", output);
            }

            WriteItemOrInnerList(member, output);
            index = index + 1;
        }
    }

    static void WriteDictionary(List<StructuredFieldMember> members, BoundedByteBuffer output)
    {
        int index = 0;
        while (index < members.Count)
        {
            StructuredFieldMember member = members[index];
            if (member == null || member.DictionaryKey == null ||
                HasDictionaryKeyBefore(members, index, member.DictionaryKey))
            {
                throw CreateFieldError();
            }

            ValidateKey(member.DictionaryKey);
            if (index > 0)
            {
                AppendAscii(", ", output);
            }

            AppendAscii(member.DictionaryKey, output);
            if (member.ValueKind == HttpFieldValueKind.Boolean && member.BooleanValue &&
                !member.IsInnerList)
            {
                WriteParameters(member.Parameters, output);
            }
            else
            {
                output.AppendByte(0x3d);
                WriteItemOrInnerList(member, output);
            }

            index = index + 1;
        }
    }

    static void WriteItemOrInnerList(StructuredFieldMember member, BoundedByteBuffer output)
    {
        if (member.IsInnerList)
        {
            WriteInnerList(member, output);
        }
        else
        {
            WriteItem(member, output, true);
        }
    }

    static void WriteInnerList(StructuredFieldMember member, BoundedByteBuffer output)
    {
        if (member.InnerList == null || member.Parameters == null)
        {
            throw CreateFieldError();
        }

        output.AppendByte(0x28);
        int index = 0;
        while (index < member.InnerList.Count)
        {
            StructuredFieldMember item = member.InnerList[index];
            if (item == null || item.IsInnerList || item.DictionaryKey != null)
            {
                throw CreateFieldError();
            }

            if (index > 0)
            {
                output.AppendByte(0x20);
            }

            WriteItem(item, output, false);
            index = index + 1;
        }

        output.AppendByte(0x29);
        WriteParameters(member.Parameters, output);
    }

    static void WriteItem(
        StructuredFieldMember member,
        BoundedByteBuffer output,
        bool allowDictionaryKey)
    {
        if (member == null || member.IsInnerList || member.Parameters == null ||
            !allowDictionaryKey && member.DictionaryKey != null)
        {
            throw CreateFieldError();
        }

        WriteBareItem(member, output);
        WriteParameters(member.Parameters, output);
    }

    static void WriteBareItem(StructuredFieldMember member, BoundedByteBuffer output)
    {
        switch (member.ValueKind)
        {
            case HttpFieldValueKind.Token:
                WriteToken(member.TextValue, output);
                return;
            case HttpFieldValueKind.String:
                WriteString(member.TextValue, output);
                return;
            case HttpFieldValueKind.ByteSequence:
                WriteByteSequence(member.ByteSequenceValue, output);
                return;
            case HttpFieldValueKind.Integer:
                WriteInteger(member.IntegerValue, output);
                return;
            case HttpFieldValueKind.Decimal:
                WriteDecimal(member.DecimalThousandths, output);
                return;
            case HttpFieldValueKind.Boolean:
                AppendAscii(member.BooleanValue ? "?1" : "?0", output);
                return;
            case HttpFieldValueKind.Date:
                output.AppendByte(0x40);
                WriteInteger(member.DateSeconds, output);
                return;
            case HttpFieldValueKind.DisplayString:
                WriteDisplayString(member.TextValue, output);
                return;
            default:
                throw CreateFieldError();
        }
    }

    static void WriteParameters(
        List<StructuredFieldParameter> parameters,
        BoundedByteBuffer output)
    {
        if (parameters == null)
        {
            throw CreateFieldError();
        }

        int index = 0;
        while (index < parameters.Count)
        {
            StructuredFieldParameter parameter = parameters[index];
            if (parameter == null || parameter.Name == null || parameter.BareItem == null ||
                HasParameterKeyBefore(parameters, index, parameter.Name))
            {
                throw CreateFieldError();
            }

            StructuredFieldMember bareItem = parameter.BareItem;
            if (bareItem.IsInnerList || bareItem.Parameters == null || bareItem.Parameters.Count != 0 ||
                bareItem.DictionaryKey != null)
            {
                throw CreateFieldError();
            }

            ValidateKey(parameter.Name);
            output.AppendByte(0x3b);
            AppendAscii(parameter.Name, output);
            if (bareItem.ValueKind != HttpFieldValueKind.Boolean || !bareItem.BooleanValue)
            {
                output.AppendByte(0x3d);
                WriteBareItem(bareItem, output);
            }

            index = index + 1;
        }
    }

    static void WriteInteger(long value, BoundedByteBuffer output)
    {
        if (value < -MaximumNumericValue || value > MaximumNumericValue)
        {
            throw CreateFieldError();
        }

        AppendAscii(value.ToString(CultureInfo.InvariantCulture), output);
    }

    static void WriteDecimal(long value, BoundedByteBuffer output)
    {
        if (value < -MaximumNumericValue || value > MaximumNumericValue)
        {
            throw CreateFieldError();
        }

        bool negative = value < 0;
        long magnitude = negative ? -value : value;
        long integer = magnitude / 1000;
        int fraction = (int)(magnitude % 1000);
        if (negative)
        {
            output.AppendByte(0x2d);
        }

        AppendAscii(integer.ToString(CultureInfo.InvariantCulture), output);
        output.AppendByte(0x2e);
        if (fraction == 0)
        {
            output.AppendByte(0x30);
            return;
        }

        byte[] digits = new byte[3];
        digits[0] = (byte)(0x30 + fraction / 100);
        digits[1] = (byte)(0x30 + fraction / 10 % 10);
        digits[2] = (byte)(0x30 + fraction % 10);
        int count = 3;
        while (count > 1 && digits[count - 1] == 0x30)
        {
            count = count - 1;
        }

        output.AppendBytes(digits.AsSpan(0, count));
    }

    static void WriteString(string? value, BoundedByteBuffer output)
    {
        if (value == null)
        {
            throw CreateFieldError();
        }

        output.AppendByte(0x22);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 0x20 || character > 0x7e)
            {
                throw CreateFieldError();
            }

            if (character == 0x22 || character == 0x5c)
            {
                output.AppendByte(0x5c);
            }

            output.AppendByte((byte)character);
            index = index + 1;
        }

        output.AppendByte(0x22);
    }

    static void WriteToken(string? value, BoundedByteBuffer output)
    {
        if (value == null || value.Length == 0 ||
            !IsAlpha(value[0]) && value[0] != '*')
        {
            throw CreateFieldError();
        }

        int index = 1;
        while (index < value.Length)
        {
            if (value[index] > 0x7f || !IsTokenCharacter((byte)value[index]))
            {
                throw CreateFieldError();
            }

            index = index + 1;
        }

        AppendAscii(value, output);
    }

    static void WriteByteSequence(byte[] value, BoundedByteBuffer output)
    {
        if (value == null)
        {
            throw CreateFieldError();
        }

        output.AppendByte(0x3a);
        AppendAscii(Convert.ToBase64String(value), output);
        output.AppendByte(0x3a);
    }

    static void WriteDisplayString(string? value, BoundedByteBuffer output)
    {
        if (value == null || !HasValidUtf16(value))
        {
            throw CreateFieldError();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        output.AppendByte(0x25);
        output.AppendByte(0x22);
        int index = 0;
        while (index < bytes.Length)
        {
            byte character = bytes[index];
            if (character == 0x25 || character == 0x22 || character < 0x20 || character >= 0x7f)
            {
                output.AppendByte(0x25);
                output.AppendByte(GetHexCharacter(character / 16));
                output.AppendByte(GetHexCharacter(character % 16));
            }
            else
            {
                output.AppendByte(character);
            }

            index = index + 1;
        }

        output.AppendByte(0x22);
    }

    static void ValidateKey(string value)
    {
        if (value == null || value.Length == 0 || !IsKeyFirst(value[0]))
        {
            throw CreateFieldError();
        }

        int index = 1;
        while (index < value.Length)
        {
            if (!IsKeyCharacter(value[index]))
            {
                throw CreateFieldError();
            }

            index = index + 1;
        }
    }

    static bool HasDictionaryKeyBefore(
        List<StructuredFieldMember> members,
        int end,
        string key)
    {
        int index = 0;
        while (index < end)
        {
            if (members[index] != null && members[index].DictionaryKey == key)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool HasParameterKeyBefore(
        List<StructuredFieldParameter> parameters,
        int end,
        string key)
    {
        int index = 0;
        while (index < end)
        {
            if (parameters[index] != null && parameters[index].Name == key)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void AppendAscii(string value, BoundedByteBuffer output)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                throw CreateFieldError();
            }

            output.AppendByte((byte)value[index]);
            index = index + 1;
        }
    }

    static bool IsAlpha(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static bool IsTokenCharacter(byte value)
    {
        return HttpGrammar.IsTokenCharacter(value) || value == 0x3a || value == 0x2f;
    }

    static bool IsKeyFirst(char value)
    {
        return value >= 'a' && value <= 'z' || value == '*';
    }

    static bool IsKeyCharacter(char value)
    {
        return IsKeyFirst(value) || value >= '0' && value <= '9' || value == '_' ||
            value == '-' || value == '.';
    }

    static bool HasValidUtf16(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character >= 0xd800 && character <= 0xdbff)
            {
                if (index + 1 >= value.Length || value[index + 1] < 0xdc00 ||
                    value[index + 1] > 0xdfff)
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (character >= 0xdc00 && character <= 0xdfff)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static byte GetHexCharacter(int value)
    {
        if (value < 10)
        {
            return (byte)(0x30 + value);
        }

        return (byte)(0x61 + value - 10);
    }

    static HttpFieldException CreateFieldError()
    {
        return new HttpFieldException(
            "The Structured Field value cannot be serialized.",
            "invalid-structured-field",
            "");
    }
}
