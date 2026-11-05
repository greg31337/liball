using System;
using System.Text;

namespace xml;

internal enum UriFragmentPolicy
{
    Preserve = 0,
    Remove = 1,
    Reject = 2
}

internal static class UriTools
{
    internal static Uri CreateAbsolute(string value)
    {
        Guard.NotNullOrEmpty(value, "value");
        ValidateReference(value);
        return new Uri(value, UriKind.Absolute);
    }

    internal static Uri CreateReference(string value)
    {
        Guard.NotNull(value, "value");
        ValidateReference(value);
        return new Uri(value, UriKind.RelativeOrAbsolute);
    }

    internal static Uri ResolveReference(Uri baseUri, string reference)
    {
        Guard.NotNull(baseUri, "baseUri");
        Guard.NotNull(reference, "reference");
        if (!baseUri.IsAbsoluteUri)
        {
            throw new ArgumentException("The base URI must be absolute.", "baseUri");
        }

        ValidateReference(reference);
        if (HasUriScheme(reference))
        {
            return CreateAbsolute(reference);
        }

        return new Uri(baseUri, reference);
    }

    internal static Uri ResolveReferenceWithFragmentPolicy(Uri baseUri, string reference, UriFragmentPolicy fragmentPolicy)
    {
        Uri resolved = ResolveReference(baseUri, reference);
        return ApplyFragmentPolicy(resolved, fragmentPolicy);
    }

    internal static Uri ApplyFragmentPolicy(Uri value, UriFragmentPolicy fragmentPolicy)
    {
        Guard.NotNull(value, "value");
        if (!value.IsAbsoluteUri)
        {
            throw new ArgumentException("The URI must be absolute.", "value");
        }

        if (fragmentPolicy != UriFragmentPolicy.Preserve && fragmentPolicy != UriFragmentPolicy.Remove && fragmentPolicy != UriFragmentPolicy.Reject)
        {
            throw new ArgumentOutOfRangeException("fragmentPolicy");
        }

        string absolute = CanonicalAbsoluteUri(value);
        bool hasFragment = HasFragment(value.OriginalString);
        if (!hasFragment)
        {
            return CreateAbsolute(absolute);
        }

        if (fragmentPolicy == UriFragmentPolicy.Reject)
        {
            throw new UriFormatException("A fragment identifier is not permitted for this URI.");
        }

        if (fragmentPolicy == UriFragmentPolicy.Remove)
        {
            return CreateAbsolute(RemoveFragment(absolute));
        }

        return CreateAbsolute(absolute);
    }

    internal static bool HasFragment(string value)
    {
        Guard.NotNull(value, "value");
        return value.IndexOf('#') >= 0;
    }

    internal static string RemoveFragment(string value)
    {
        Guard.NotNull(value, "value");
        int fragment = value.IndexOf('#');
        if (fragment < 0)
        {
            return value;
        }

        return value.Substring(0, fragment);
    }

    internal static string ResourceKey(Uri value)
    {
        Guard.NotNull(value, "value");
        if (!value.IsAbsoluteUri)
        {
            throw new ArgumentException("The URI must be absolute.", "value");
        }

        return RemoveFragment(CanonicalAbsoluteUri(value));
    }

    internal static string CanonicalAbsoluteUri(Uri value)
    {
        Guard.NotNull(value, "value");
        if (!value.IsAbsoluteUri)
        {
            throw new ArgumentException("The URI must be absolute.", "value");
        }

        if (value.IsFile)
        {
            return NormalizeFileUri(value);
        }

        string absolute;
        string host = value.Host;
        string idnHost = value.IdnHost;
        if (host.Length > 0 && !string.Equals(host, idnHost, StringComparison.OrdinalIgnoreCase))
        {
            UriBuilder builder = new UriBuilder(value);
            builder.Host = idnHost;
            absolute = builder.Uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
        }
        else
        {
            absolute = value.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
        }

        return NormalizePercentEncoding(IriToUriString(absolute));
    }

    internal static string NormalizePercentEncoding(string value)
    {
        Guard.NotNull(value, "value");
        StringBuilder normalized = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != '%')
            {
                normalized.Append(current);
                index = index + 1;
                continue;
            }

            if (index + 2 >= value.Length || HexValue(value[index + 1]) < 0 || HexValue(value[index + 2]) < 0)
            {
                throw new UriFormatException("A percent escape must contain two hexadecimal digits.");
            }

            int octet = HexValue(value[index + 1]) * 16 + HexValue(value[index + 2]);
            if (IsUnreservedAscii(octet))
            {
                normalized.Append((char)octet);
            }
            else
            {
                const string hexadecimal = "0123456789ABCDEF";
                normalized.Append('%');
                normalized.Append(hexadecimal[octet / 16]);
                normalized.Append(hexadecimal[octet % 16]);
            }

            index = index + 3;
        }

        return normalized.ToString();
    }

    internal static string IriToUriString(string value)
    {
        Guard.NotNull(value, "value");
        ValidateReference(value);
        StringBuilder converted = new StringBuilder(value.Length);
        const string hexadecimal = "0123456789ABCDEF";
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current <= 127)
            {
                converted.Append(current);
                index = index + 1;
                continue;
            }

            int characterLength = char.IsHighSurrogate(current) ? 2 : 1;
            byte[] bytes = Encoding.UTF8.GetBytes(value.Substring(index, characterLength));
            int byteIndex = 0;
            while (byteIndex < bytes.Length)
            {
                int octet = bytes[byteIndex];
                converted.Append('%');
                converted.Append(hexadecimal[octet / 16]);
                converted.Append(hexadecimal[octet % 16]);
                byteIndex = byteIndex + 1;
            }

            index = index + characterLength;
        }

        return NormalizePercentEncoding(converted.ToString());
    }

    internal static string NormalizeFileUri(Uri value)
    {
        Guard.NotNull(value, "value");
        if (!value.IsAbsoluteUri || !value.IsFile)
        {
            throw new ArgumentException("The URI must be an absolute file URI.", "value");
        }

        if (value.UserInfo.Length > 0 || !value.IsDefaultPort)
        {
            throw new UriFormatException("A file URI must not contain user information or a port.");
        }

        string host = value.IdnHost;
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            host = "";
        }
        else
        {
            host = host.ToLowerInvariant();
        }

        if (host.IndexOf(':') >= 0)
        {
            host = "[" + host + "]";
        }

        string path = value.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
        if (!path.StartsWith("/", StringComparison.Ordinal))
        {
            path = "/" + path;
        }

        path = NormalizePercentEncoding(IriToUriString(path));
        if (path.Length >= 4 && path[0] == '/' && IsAsciiLetter(path[1]) && path[2] == ':' && path[3] == '/')
        {
            path = "/" + char.ToUpperInvariant(path[1]) + path.Substring(2);
        }

        string normalized = "file://" + host + path;
        string query = value.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
        if (query.Length > 0)
        {
            normalized = normalized + "?" + NormalizePercentEncoding(IriToUriString(query));
        }

        string fragment = value.GetComponents(UriComponents.Fragment, UriFormat.UriEscaped);
        if (fragment.Length > 0)
        {
            normalized = normalized + "#" + NormalizePercentEncoding(IriToUriString(fragment));
        }

        return normalized;
    }

    internal static void ValidateReference(string value)
    {
        int fragmentCount = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current <= ' ' || current == '\u007f' || current == '<' || current == '>' || current == '"' || current == '\\' || current == '^' || current == '`' || current == '{' || current == '|' || current == '}')
            {
                throw new UriFormatException("The URI reference contains a character that must be percent-encoded.");
            }

            if (current == '%')
            {
                if (index + 2 >= value.Length || HexValue(value[index + 1]) < 0 || HexValue(value[index + 2]) < 0)
                {
                    throw new UriFormatException("A percent escape must contain two hexadecimal digits.");
                }

                index = index + 3;
                continue;
            }

            if (current == '#')
            {
                fragmentCount = fragmentCount + 1;
                if (fragmentCount > 1)
                {
                    throw new UriFormatException("A URI reference can contain only one fragment delimiter.");
                }
            }

            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    throw new UriFormatException("The URI reference contains an unmatched surrogate.");
                }

                index = index + 2;
                continue;
            }

            if (char.IsLowSurrogate(current))
            {
                throw new UriFormatException("The URI reference contains an unmatched surrogate.");
            }

            index = index + 1;
        }
    }

    internal static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        return -1;
    }

    internal static bool IsUnreservedAscii(int value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '-' || value == '.' || value == '_' || value == '~';
    }

    internal static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    internal static bool HasUriScheme(string value)
    {
        if (value.Length == 0 || !IsAsciiLetter(value[0]))
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == ':')
            {
                return true;
            }

            if (!IsAsciiLetter(current) && !(current >= '0' && current <= '9') && current != '+' && current != '-' && current != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return false;
    }
}
