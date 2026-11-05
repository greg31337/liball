namespace odf;

internal static class OdfIriResolver
{
    internal static bool IsAbsoluteUri(string? value)
    {
        if (!IsIriText(value) || !IsAscii(value))
        {
            return false;
        }
        return global::System.Uri.IsWellFormedUriString(value, global::System.UriKind.Absolute);
    }

    internal static bool IsAbsoluteIri(string? value)
    {
        if (!IsIriText(value))
        {
            return false;
        }
        return global::System.Uri.IsWellFormedUriString(value, global::System.UriKind.Absolute);
    }

    internal static bool IsIriReference(string? value)
    {
        if (value == null)
        {
            return false;
        }
        if (value.Length == 0)
        {
            return true;
        }
        if (!IsIriText(value))
        {
            return false;
        }
        if (global::System.Uri.IsWellFormedUriString(value, global::System.UriKind.Absolute))
        {
            return true;
        }
        if (value.StartsWith("//", global::System.StringComparison.Ordinal))
        {
            return global::System.Uri.IsWellFormedUriString("iri:" + value, global::System.UriKind.Absolute);
        }

        int fragment = value.IndexOf('#');
        if (fragment >= 0 && value.IndexOf('#', fragment + 1) >= 0)
        {
            return false;
        }
        int pathEnd = ReferencePathEnd(value);
        int slash = value.IndexOf('/');
        int colon = value.IndexOf(':');
        if (colon >= 0 && colon < pathEnd && (slash < 0 || colon < slash))
        {
            return false;
        }
        return true;
    }

    internal static bool IsPackageRelativeReference(string? value)
    {
        if (value == null || !IsIriReference(value))
        {
            return false;
        }
        if (value.StartsWith("/", global::System.StringComparison.Ordinal) || value.StartsWith("//", global::System.StringComparison.Ordinal))
        {
            return false;
        }
        if (value.IndexOf('\\') >= 0)
        {
            return false;
        }

        int end = ReferencePathEnd(value);
        int slash = value.IndexOf('/');
        int colon = value.IndexOf(':');
        if (colon >= 0 && colon < end && (slash < 0 || colon < slash))
        {
            return false;
        }
        return true;
    }

    internal static string ResolvePackageReference(string? basePartName, string? reference)
    {
        return PackagePathRules.ResolveRelativeReference(basePartName, reference);
    }

    internal static OdfCurieValue ParseCurie(string? value)
    {
        return ParseCurieCore(value, false);
    }

    internal static OdfCurieValue ParseSafeCurie(string? value)
    {
        return ParseCurieCore(value, true);
    }

    internal static string FormatCurie(OdfCurieValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return "";
        }
        string lexical = value.Reference;
        if (value.HasPrefixSeparator)
        {
            lexical = value.Prefix + ":" + value.Reference;
        }
        if (value.IsSafe)
        {
            return "[" + lexical + "]";
        }
        return lexical;
    }

    static OdfCurieValue ParseCurieCore(string? value, bool safe)
    {
        OdfCurieValue result = new OdfCurieValue();
        result.IsSafe = safe;
        if (value == null)
        {
            return result;
        }
        result.LexicalValue = value;

        string lexical = value;
        if (safe)
        {
            if (value.Length < 2 || value[0] != '[' || value[value.Length - 1] != ']')
            {
                return result;
            }
            lexical = value.Substring(1, value.Length - 2);
        }
        else if (value.IndexOf('[') >= 0 || value.IndexOf(']') >= 0)
        {
            return result;
        }

        int separator = lexical.IndexOf(':');
        string prefix = "";
        string reference = lexical;
        if (separator >= 0)
        {
            prefix = lexical.Substring(0, separator);
            reference = lexical.Substring(separator + 1);
            result.HasPrefixSeparator = true;
        }
        if (prefix.Length > 0 && !OdfNameRules.IsNcName(prefix))
        {
            return result;
        }
        if (!IsIriReference(reference))
        {
            return result;
        }

        result.Prefix = prefix;
        result.Reference = reference;
        result.IsValid = true;
        return result;
    }

    static int ReferencePathEnd(string value)
    {
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        if (query < 0)
        {
            return fragment < 0 ? value.Length : fragment;
        }
        if (fragment < 0)
        {
            return query;
        }
        return query < fragment ? query : fragment;
    }

    static bool IsIriText(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            int character = current;
            int width = 1;
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }
                character = char.ConvertToUtf32(current, value[index + 1]);
                width = 2;
            }
            else if (char.IsLowSurrogate(current))
            {
                return false;
            }
            if (character <= 0x20 || character == 0x7F)
            {
                return false;
            }
            if (character == 0x22 || character == 0x3C || character == 0x3E || character == 0x5C || character == 0x5E || character == 0x60 || character == 0x7B || character == 0x7C || character == 0x7D)
            {
                return false;
            }
            if (character == 0x25)
            {
                if (index + 2 >= value.Length || !IsHexDigit(value[index + 1]) || !IsHexDigit(value[index + 2]))
                {
                    return false;
                }
            }
            int planeCharacter = character & 0xFFFF;
            if (planeCharacter >= 0xFFFE || character >= 0xFDD0 && character <= 0xFDEF)
            {
                return false;
            }
            index = index + width;
        }
        return true;
    }

    static bool IsAscii(string? value)
    {
        if (value == null)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7F)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return true;
        }
        if (value >= 'A' && value <= 'F')
        {
            return true;
        }
        return value >= 'a' && value <= 'f';
    }
}
