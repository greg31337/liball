using System;
using System.Text;

namespace xml;

internal class XdmBinaryDatatype
{
    internal XdmAtomicValue ParseXdmBinary(string lexicalValue, XdmAtomicType targetType)
    {
        XdmNumericParseResult result = TryParseXdmBinary(lexicalValue, targetType);
        return new XdmNumericDatatype().RequireXdmNumericParseResult(result);
    }

    internal XdmNumericParseResult TryParseXdmBinary(string lexicalValue, XdmAtomicType targetType)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        RequireXdmBinaryType(targetType);
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        byte[]? bytes;
        if (targetType == XdmAtomicType.HexBinary)
        {
            bytes = ParseXdmHexBinaryBytes(normalized);
        }
        else
        {
            bytes = ParseXdmBase64BinaryBytes(normalized);
        }

        if (bytes == null)
        {
            return InvalidXdmBinaryLexical(targetType);
        }

        return new XdmNumericParseResult(true, CreateXdmBinaryValue(bytes, targetType, lexicalValue), null, null);
    }

    internal byte[]? ParseXdmHexBinaryBytes(string lexicalValue)
    {
        if (lexicalValue.Length % 2 != 0)
        {
            return null;
        }

        byte[] result = new byte[lexicalValue.Length / 2];
        int index = 0;
        while (index < result.Length)
        {
            int high = UriTools.HexValue(lexicalValue[index * 2]);
            int low = UriTools.HexValue(lexicalValue[index * 2 + 1]);
            if (high < 0 || low < 0)
            {
                return null;
            }

            result[index] = (byte)(high * 16 + low);
            index = index + 1;
        }

        return result;
    }

    internal byte[]? ParseXdmBase64BinaryBytes(string lexicalValue)
    {
        StringBuilder compactBuilder = new StringBuilder(lexicalValue.Length);
        int sourceIndex = 0;
        while (sourceIndex < lexicalValue.Length)
        {
            char character = lexicalValue[sourceIndex];
            if (character != ' ')
            {
                compactBuilder.Append(character);
            }

            sourceIndex = sourceIndex + 1;
        }

        string compact = compactBuilder.ToString();
        if (compact.Length % 4 != 0)
        {
            return null;
        }

        int padding = 0;
        if (compact.Length > 0 && compact[compact.Length - 1] == '=')
        {
            padding = 1;
            if (compact.Length > 1 && compact[compact.Length - 2] == '=')
            {
                padding = 2;
            }
        }

        int dataLength = compact.Length - padding;
        int index = 0;
        while (index < compact.Length)
        {
            char character = compact[index];
            if (index < dataLength)
            {
                if (XdmBase64Value(character) < 0)
                {
                    return null;
                }
            }
            else if (character != '=')
            {
                return null;
            }

            index = index + 1;
        }

        if (padding == 1 && (XdmBase64Value(compact[compact.Length - 2]) & 3) != 0)
        {
            return null;
        }

        if (padding == 2 && (XdmBase64Value(compact[compact.Length - 3]) & 15) != 0)
        {
            return null;
        }

        return DecodeXdmBase64(compact, padding);
    }

    internal byte[] DecodeXdmBase64(string compact, int padding)
    {
        byte[] result = new byte[compact.Length / 4 * 3 - padding];
        int sourceIndex = 0;
        int targetIndex = 0;
        while (sourceIndex < compact.Length)
        {
            int first = XdmBase64Value(compact[sourceIndex]);
            int second = XdmBase64Value(compact[sourceIndex + 1]);
            int third = compact[sourceIndex + 2] == '=' ? 0 : XdmBase64Value(compact[sourceIndex + 2]);
            int fourth = compact[sourceIndex + 3] == '=' ? 0 : XdmBase64Value(compact[sourceIndex + 3]);
            result[targetIndex] = (byte)(first * 4 + second / 16);
            targetIndex = targetIndex + 1;
            if (targetIndex < result.Length)
            {
                result[targetIndex] = (byte)((second & 15) * 16 + third / 4);
                targetIndex = targetIndex + 1;
            }

            if (targetIndex < result.Length)
            {
                result[targetIndex] = (byte)((third & 3) * 64 + fourth);
                targetIndex = targetIndex + 1;
            }

            sourceIndex = sourceIndex + 4;
        }

        return result;
    }

    internal int XdmBase64Value(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return character - 'A';
        }

        if (character >= 'a' && character <= 'z')
        {
            return character - 'a' + 26;
        }

        if (character >= '0' && character <= '9')
        {
            return character - '0' + 52;
        }

        if (character == '+')
        {
            return 62;
        }

        return character == '/' ? 63 : -1;
    }

    internal XdmAtomicValue CreateXdmBinaryValue(byte[] source, XdmAtomicType type, string? lexicalValue)
    {
        Guard.NotNull(source, "source");
        RequireXdmBinaryType(type);
        byte[] storage = CopyXdmBinaryBytes(source);
        string canonical = CanonicalizeXdmBinary(storage, type);
        XdmAtomicValue result = new XdmAtomicValue();
        result.Type = type;
        result.LexicalValue = lexicalValue == null ? canonical : lexicalValue;
        result.CanonicalValue = canonical;
        result.StringValue = canonical;
        result.BinaryValue = storage;
        result.IsImmutable = true;
        return result;
    }

    internal byte[] CopyXdmBinaryBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal string CanonicalizeXdmBinary(byte[] source, XdmAtomicType type)
    {
        Guard.NotNull(source, "source");
        RequireXdmBinaryType(type);
        if (type == XdmAtomicType.Base64Binary)
        {
            return Convert.ToBase64String(source);
        }

        StringBuilder result = new StringBuilder(source.Length * 2);
        const string hexadecimal = "0123456789ABCDEF";
        int index = 0;
        while (index < source.Length)
        {
            int value = source[index];
            result.Append(hexadecimal[value / 16]);
            result.Append(hexadecimal[value % 16]);
            index = index + 1;
        }

        return result.ToString();
    }

    internal int CompareXdmBinary(XdmAtomicValue left, XdmAtomicValue right)
    {
        byte[] leftBytes = ReadXdmBinaryOperand(left);
        byte[] rightBytes = ReadXdmBinaryOperand(right);
        if (left.Type != right.Type)
        {
            throw new ArgumentException("Binary comparison operands must have the same atomic type.", "right");
        }

        int commonLength = leftBytes.Length < rightBytes.Length ? leftBytes.Length : rightBytes.Length;
        int index = 0;
        while (index < commonLength)
        {
            if (leftBytes[index] < rightBytes[index])
            {
                return -1;
            }

            if (leftBytes[index] > rightBytes[index])
            {
                return 1;
            }

            index = index + 1;
        }

        if (leftBytes.Length < rightBytes.Length)
        {
            return -1;
        }

        return leftBytes.Length > rightBytes.Length ? 1 : 0;
    }

    internal bool EqualXdmBinary(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CompareXdmBinary(left, right) == 0;
    }

    internal bool LessThanXdmBinary(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CompareXdmBinary(left, right) < 0;
    }

    internal bool GreaterThanXdmBinary(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CompareXdmBinary(left, right) > 0;
    }

    internal byte[] ReadXdmBinaryOperand(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        RequireXdmBinaryType(value.Type);
        if (value.BinaryValue == null)
        {
            throw new ArgumentException("A binary atomic value must provide binary storage.", "value");
        }

        return value.BinaryValue;
    }

    internal void RequireXdmBinaryType(XdmAtomicType type)
    {
        if (type != XdmAtomicType.HexBinary && type != XdmAtomicType.Base64Binary)
        {
            throw new ArgumentException("The requested atomic type is not a binary datatype.", "type");
        }
    }

    internal XdmNumericParseResult InvalidXdmBinaryLexical(XdmAtomicType type)
    {
        string name = type == XdmAtomicType.HexBinary ? "xs:hexBinary" : "xs:base64Binary";
        return new XdmNumericParseResult(false, null, XPathErrorCodes.FORG0001, "The lexical value is not valid for " + name + ".");
    }
}
