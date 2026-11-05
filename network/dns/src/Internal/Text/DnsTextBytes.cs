using System.Collections.Generic;

namespace dns;

internal class DnsTextBytesResult
{
    internal bool Success;
    internal byte[] Bytes = [];
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class DnsTextBytes
{
    internal static DnsTextBytesResult ParseHex(string text, string field)
    {
        if (text == null || (text.Length & 1) != 0)
        {
            return Failure(field, "Hexadecimal text requires pairs of digits.");
        }

        byte[] bytes = new byte[text.Length / 2];
        int index = 0;
        while (index < bytes.Length)
        {
            int high = ReadHexNibble(text[index * 2]);
            int low = ReadHexNibble(text[(index * 2) + 1]);
            if (high < 0 || low < 0)
            {
                return Failure(field, "The value contains a non-hexadecimal digit.");
            }

            bytes[index] = (byte)((high << 4) | low);
            index = index + 1;
        }

        return Success(bytes);
    }

    internal static DnsTextBytesResult ParseBase64(string text, string field)
    {
        if (text == null || (text.Length & 3) != 0)
        {
            return Failure(field, "Base64 text must contain complete four-character groups.");
        }

        int padding = 0;
        if (text.Length > 0 && text[text.Length - 1] == '=') padding = 1;
        if (text.Length > 1 && text[text.Length - 2] == '=') padding = 2;
        int length = ((text.Length / 4) * 3) - padding;
        byte[] bytes = new byte[length];
        int textIndex = 0;
        int byteIndex = 0;
        while (textIndex < text.Length)
        {
            int first = ReadBase64Value(text[textIndex]);
            int second = ReadBase64Value(text[textIndex + 1]);
            int third = ReadBase64Value(text[textIndex + 2]);
            int fourth = ReadBase64Value(text[textIndex + 3]);
            bool last = textIndex + 4 == text.Length;
            if (first < 0 || second < 0 ||
                (third < 0 && !(last && third == -2)) ||
                (fourth < 0 && !(last && fourth == -2)) ||
                third == -2 && fourth != -2)
            {
                return Failure(field, "The value is not valid base64 text.");
            }

            int thirdValue = third;
            int fourthValue = fourth;
            if (thirdValue < 0) thirdValue = 0;
            if (fourthValue < 0) fourthValue = 0;
            if (byteIndex < bytes.Length)
            {
                bytes[byteIndex] = (byte)((first << 2) | (second >> 4));
                byteIndex = byteIndex + 1;
            }

            if (byteIndex < bytes.Length)
            {
                bytes[byteIndex] = (byte)(((second << 4) |
                    (thirdValue >> 2)) & 255);
                byteIndex = byteIndex + 1;
            }

            if (byteIndex < bytes.Length)
            {
                bytes[byteIndex] = (byte)(((thirdValue << 6) |
                    fourthValue) & 255);
                byteIndex = byteIndex + 1;
            }

            textIndex = textIndex + 4;
        }

        return Success(bytes);
    }

    internal static DnsTextBytesResult ParseCharacterString(
        string text,
        string field)
    {
        if (text == null || text.Length < 2 ||
            text[0] != '"' || text[text.Length - 1] != '"')
        {
            return Failure(field, "A quoted character string is required.");
        }

        List<byte> bytes = new List<byte>();
        int index = 1;
        while (index < text.Length - 1)
        {
            char character = text[index];
            if (character == '\\')
            {
                if (index + 1 >= text.Length - 1)
                {
                    return Failure(field, "A character-string escape is incomplete.");
                }

                char next = text[index + 1];
                if (next >= '0' && next <= '9')
                {
                    if (index + 3 >= text.Length - 1)
                    {
                        return Failure(field, "A decimal escape requires three digits.");
                    }

                    int second = text[index + 2] - '0';
                    int third = text[index + 3] - '0';
                    if (second < 0 || second > 9 || third < 0 || third > 9)
                    {
                        return Failure(field, "A decimal escape requires three digits.");
                    }

                    int value = ((next - '0') * 100) + (second * 10) + third;
                    if (value > 255)
                    {
                        return Failure(field, "A decimal escape exceeds one octet.");
                    }

                    bytes.Add((byte)value);
                    index = index + 4;
                    continue;
                }

                character = next;
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }

            if (character > 255)
            {
                return Failure(field, "Character strings contain only octets.");
            }

            bytes.Add((byte)character);
        }

        return Success(bytes.ToArray());
    }

    internal static DnsTextBytesResult ParseAddress(
        string text,
        int expectedLength)
    {
        if (expectedLength == 4)
        {
            return ParseIpv4(text);
        }

        return ParseIpv6(text);
    }

    static DnsTextBytesResult ParseIpv4(string text)
    {
        List<string> parts = SplitText(text, '.');
        if (parts.Count != 4)
        {
            return Failure("address", "An IPv4 address requires four decimal octets.");
        }

        byte[] bytes = new byte[4];
        int index = 0;
        while (index < 4)
        {
            DnsTextNumberResult number = DnsTextNumber.ParseDecimal(
                parts[index], 255, "address");
            if (!number.Success)
            {
                return FromNumberFailure(number);
            }

            bytes[index] = (byte)number.Value;
            index = index + 1;
        }

        return Success(bytes);
    }

    static DnsTextBytesResult ParseIpv6(string text)
    {
        int compression = text.IndexOf("::", System.StringComparison.Ordinal);
        if (compression >= 0 &&
            text.IndexOf("::", compression + 2, System.StringComparison.Ordinal) >= 0)
        {
            return Failure("address", "An IPv6 address has multiple compression markers.");
        }

        string leftText = text;
        string rightText = string.Empty;
        if (compression >= 0)
        {
            leftText = text.Substring(0, compression);
            rightText = text.Substring(compression + 2);
        }

        List<string> left = SplitIpv6Side(leftText);
        List<string> right = SplitIpv6Side(rightText);
        int groupCount = left.Count + right.Count;
        if (compression < 0 && groupCount != 8 ||
            compression >= 0 && groupCount >= 8)
        {
            return Failure("address", "The IPv6 address has an invalid group count.");
        }

        byte[] bytes = new byte[16];
        int index = 0;
        while (index < left.Count)
        {
            DnsTextNumberResult group = ParseHexGroup(left[index]);
            if (!group.Success) return FromNumberFailure(group);
            bytes[index * 2] = (byte)(group.Value >> 8);
            bytes[(index * 2) + 1] = (byte)(group.Value & 255);
            index = index + 1;
        }

        int rightIndex = 0;
        int rightStart = 8 - right.Count;
        while (rightIndex < right.Count)
        {
            DnsTextNumberResult group = ParseHexGroup(right[rightIndex]);
            if (!group.Success) return FromNumberFailure(group);
            int destination = (rightStart + rightIndex) * 2;
            bytes[destination] = (byte)(group.Value >> 8);
            bytes[destination + 1] = (byte)(group.Value & 255);
            rightIndex = rightIndex + 1;
        }

        return Success(bytes);
    }

    static DnsTextNumberResult ParseHexGroup(string text)
    {
        if (text.Length == 0 || text.Length > 4)
        {
            return DnsTextNumber.Failure(
                "address", "An IPv6 group requires one to four hexadecimal digits.", 0);
        }

        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int nibble = ReadHexNibble(text[index]);
            if (nibble < 0)
            {
                return DnsTextNumber.Failure(
                    "address", "An IPv6 group contains a non-hexadecimal digit.", index);
            }

            value = (value << 4) | nibble;
            index = index + 1;
        }

        DnsTextNumberResult result = new DnsTextNumberResult();
        result.Success = true;
        result.Value = value;
        return result;
    }

    static List<string> SplitIpv6Side(string text)
    {
        if (text.Length == 0) return new List<string>();
        return SplitText(text, ':');
    }

    static List<string> SplitText(string text, char separator)
    {
        List<string> values = new List<string>();
        int start = 0;
        int index = 0;
        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == separator)
            {
                values.Add(text.Substring(start, index - start));
                start = index + 1;
            }

            index = index + 1;
        }

        return values;
    }

    static int ReadHexNibble(char value)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        return -1;
    }

    static int ReadBase64Value(char value)
    {
        if (value >= 'A' && value <= 'Z') return value - 'A';
        if (value >= 'a' && value <= 'z') return value - 'a' + 26;
        if (value >= '0' && value <= '9') return value - '0' + 52;
        if (value == '+') return 62;
        if (value == '/') return 63;
        if (value == '=') return -2;
        return -1;
    }

    static DnsTextBytesResult FromNumberFailure(DnsTextNumberResult number)
    {
        DnsTextBytesResult result = new DnsTextBytesResult();
        int index = 0;
        while (index < number.Diagnostics.Count)
        {
            result.Diagnostics.Add(number.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    static DnsTextBytesResult Success(byte[] bytes)
    {
        DnsTextBytesResult result = new DnsTextBytesResult();
        result.Success = true;
        result.Bytes = bytes;
        return result;
    }

    static DnsTextBytesResult Failure(string field, string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidText;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        DnsTextBytesResult result = new DnsTextBytesResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
