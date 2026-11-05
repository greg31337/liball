namespace sip;

internal static class SipSdpSyntax
{
    internal static bool IsToken(string? value)
    {
        if (value == null || value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            bool alphaNumeric = character >= 'A' && character <= 'Z'
                || character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9';
            if (!alphaNumeric && character != '!' && character != '#'
                && character != '$' && character != '%' && character != '&'
                && character != '\'' && character != '*' && character != '+'
                && character != '-' && character != '.' && character != '^'
                && character != '_' && character != '`' && character != '{'
                && character != '|' && character != '}' && character != '~') return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool IsNonWhitespace(string? value)
    {
        if (value == null || value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character <= 32 || character == 127) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool IsByteString(string? value)
    {
        if (value == null || value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character == 0 || character == '\r' || character == '\n') return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool IsUriReference(string? value)
    {
        return IsNonWhitespace(value) && value != null
            && System.Uri.IsWellFormedUriString(value, System.UriKind.RelativeOrAbsolute);
    }

    internal static bool IsEncryptionKey(string? value)
    {
        if (value == "prompt") return true;
        if (value == null) return false;
        if (value.StartsWith("clear:", System.StringComparison.Ordinal))
            return IsByteString(value.Substring(6));
        if (value.StartsWith("uri:", System.StringComparison.Ordinal))
            return IsUriReference(value.Substring(4));
        if (!value.StartsWith("base64:", System.StringComparison.Ordinal)) return false;
        return IsBase64(value.Substring(7));
    }

    internal static bool IsDigits(string? value)
    {
        if (value == null || value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9') return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool IsPositiveDigits(string? value)
    {
        return IsDigits(value) && value != null && value[0] != '0';
    }

    internal static bool IsTime(string? value)
    {
        return value == "0" || IsPositiveDigits(value) && value != null && value.Length >= 10;
    }

    internal static bool IsTypedTime(string? value, bool positive)
    {
        if (value == null || value.Length == 0) return false;
        int length = value.Length;
        int last = value[length - 1];
        if (last == 'd' || last == 'h' || last == 'm' || last == 's') length = length - 1;
        if (length == 0) return false;
        string digits = value.Substring(0, length);
        return positive ? IsPositiveDigits(digits) : IsDigits(digits);
    }

    internal static string[] SplitFields(string value)
    {
        return value.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
    }

    internal static bool HasCanonicalSpaces(string value)
    {
        return value.Length > 0 && value[0] != ' ' && value[value.Length - 1] != ' '
            && value.IndexOf("  ", System.StringComparison.Ordinal) < 0
            && value.IndexOf('\t') < 0;
    }

    internal static string NormalizeFields(string value)
    {
        string[] fields = value.Split(new char[] { ' ', '\t' },
            System.StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", fields);
    }

    internal static bool IsOrigin(string? value)
    {
        if (value == null) return false;
        string[] fields = SplitFields(value);
        if (fields.Length != 6 || !HasCanonicalSpaces(value)) return false;
        if (!IsNonWhitespace(fields[0]) || !IsDigits(fields[1]) || !IsDigits(fields[2])) return false;
        if (!IsToken(fields[3]) || !IsToken(fields[4])) return false;
        return IsAddress(fields[3], fields[4], fields[5], false);
    }

    internal static bool IsConnection(string? value)
    {
        if (value == null) return false;
        string[] fields = SplitFields(value);
        if (fields.Length != 3 || !HasCanonicalSpaces(value)) return false;
        if (!IsToken(fields[0]) || !IsToken(fields[1])) return false;
        return IsAddress(fields[0], fields[1], fields[2], true);
    }

    internal static bool IsSessionConnection(string? value)
    {
        if (!IsConnection(value) || value == null) return false;
        string[] fields = SplitFields(value);
        string[] addressParts = fields[2].Split('/');
        if (SipAscii.EqualsIgnoreCase(fields[1], "IP4")) return addressParts.Length <= 2;
        if (SipAscii.EqualsIgnoreCase(fields[1], "IP6")) return addressParts.Length == 1;
        return true;
    }

    internal static bool IsBandwidth(string? value)
    {
        if (value == null) return false;
        int separator = value.IndexOf(':');
        if (separator <= 0 || separator == value.Length - 1) return false;
        string type = value.Substring(0, separator);
        return IsToken(type) && IsDigits(value.Substring(separator + 1));
    }

    internal static bool IsTiming(string? value)
    {
        if (value == null || !HasCanonicalSpaces(value)) return false;
        string[] fields = SplitFields(value);
        return fields.Length == 2 && IsTime(fields[0]) && IsTime(fields[1]);
    }

    internal static bool IsRepeatTime(string? value)
    {
        if (value == null || !HasCanonicalSpaces(value)) return false;
        string[] fields = SplitFields(value);
        if (fields.Length < 3 || !IsTypedTime(fields[0], true)) return false;
        int index = 1;
        while (index < fields.Length)
        {
            if (!IsTypedTime(fields[index], false)) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool IsTimeZones(string? value)
    {
        if (value == null || !HasCanonicalSpaces(value)) return false;
        string[] fields = SplitFields(value);
        if (fields.Length < 2 || fields.Length % 2 != 0) return false;
        int index = 0;
        while (index < fields.Length)
        {
            if (!IsTime(fields[index]) || fields[index] == "0") return false;
            string offset = fields[index + 1];
            if (offset.Length > 0 && offset[0] == '-') offset = offset.Substring(1);
            if (!IsTypedTime(offset, false)) return false;
            index = index + 2;
        }
        return true;
    }

    internal static bool IsAttribute(SipSdpAttribute? attribute)
    {
        if (attribute == null || !IsToken(attribute.Name)) return false;
        if (!attribute.HasValue) return attribute.Value == null;
        return IsByteString(attribute.Value);
    }

    internal static bool IsProtocol(string? value)
    {
        if (value == null || value.Length == 0) return false;
        string[] parts = value.Split('/');
        int index = 0;
        while (index < parts.Length)
        {
            if (!IsToken(parts[index])) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool IsUnknownLine(string? value)
    {
        return value != null && value.Length >= 3 && value[1] == '='
            && value[0] >= 'a' && value[0] <= 'z' && IsByteString(value.Substring(2));
    }

    internal static bool ParseBoundedInteger(string? value, int maximum, bool allowZero, int failure)
    {
        if (!IsDigits(value) || value == null || value.Length > 10) return false;
        int number = 0;
        int index = 0;
        while (index < value.Length)
        {
            int digit = value[index] - '0';
            if (number > (maximum - digit) / 10) return false;
            number = number * 10 + digit;
            index = index + 1;
        }
        if (!allowZero && number == 0) return false;
        return number != failure;
    }

    internal static int ToBoundedInteger(string value)
    {
        int number = 0;
        int index = 0;
        while (index < value.Length)
        {
            number = number * 10 + value[index] - '0';
            index = index + 1;
        }
        return number;
    }

    static bool IsBase64(string value)
    {
        if (value.Length % 4 != 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            bool alphabet = character >= 'A' && character <= 'Z'
                || character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9'
                || character == '+' || character == '/';
            if (!alphabet)
            {
                if (character != '=' || index < value.Length - 2) return false;
                if (index == value.Length - 2 && value[index + 1] != '=') return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsAddress(string networkType, string addressType, string value, bool connection)
    {
        if (!IsNonWhitespace(value)) return false;
        if (!SipAscii.EqualsIgnoreCase(networkType, "IN")) return true;
        if (!SipAscii.EqualsIgnoreCase(addressType, "IP4")
            && !SipAscii.EqualsIgnoreCase(addressType, "IP6")) return true;
        string[] parts = value.Split('/');
        string address = parts[0];
        bool ipv4 = SipAscii.EqualsIgnoreCase(addressType, "IP4");
        bool literal = ipv4 ? SipUriSyntax.IsIpv4Address(address) : SipUriSyntax.IsIpv6Address(address);
        if (!literal && !SipUriSyntax.IsHostname(address)) return false;
        bool multicast = ipv4 ? IsIpv4Multicast(address) : IsIpv6Multicast(address);
        if (ipv4 && literal && Ipv4FirstOctet(address) >= 240) return false;
        if (!connection) return parts.Length == 1 && !multicast;
        if (!literal) return parts.Length == 1;
        if (!multicast) return parts.Length == 1;
        if (ipv4)
        {
            if (parts.Length < 2 || parts.Length > 3
                || !ParseBoundedInteger(parts[1], 255, true, -1)) return false;
            return parts.Length == 2 || ParseBoundedInteger(parts[2], int.MaxValue, false, -1);
        }
        return parts.Length == 1
            || parts.Length == 2 && ParseBoundedInteger(parts[1], int.MaxValue, false, -1);
    }

    static bool IsIpv4Multicast(string address)
    {
        int first = Ipv4FirstOctet(address);
        return first >= 224 && first <= 239;
    }

    static int Ipv4FirstOctet(string address)
    {
        int dot = address.IndexOf('.');
        if (dot <= 0) return -1;
        return ToBoundedInteger(address.Substring(0, dot));
    }

    static bool IsIpv6Multicast(string address)
    {
        return address.Length >= 2 && (address[0] == 'f' || address[0] == 'F')
            && (address[1] == 'f' || address[1] == 'F');
    }
}
