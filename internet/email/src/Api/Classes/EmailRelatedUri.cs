using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with related URI.
/// </summary>
public class EmailRelatedUri : IEmailRelatedUriContract
{
    internal static string NormalizeContentIdImplementationWithValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string normalized = value.Trim();
        if (normalized.StartsWith("cid:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(4);
        }

        if (normalized.Length >= 2 && normalized[0] == '<' && normalized[normalized.Length - 1] == '>')
        {
            normalized = normalized.Substring(1, normalized.Length - 2).Trim();
        }

        if (normalized.Length == 0)
        {
            throw new ArgumentException("A content ID cannot be empty.", "value");
        }

        EmailPercentDecodeResult decoded = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytes(Encoding.UTF8.GetBytes(normalized), EmailRfc2231DecodeMode.Tolerant, Encoding.UTF8.GetByteCount(normalized));
        normalized = Encoding.UTF8.GetString(decoded.GetDecodedBytes());
        int separator = normalized.LastIndexOf('@');
        if (separator < 1 || separator + 1 >= normalized.Length)
        {
            throw new ArgumentException("A content ID must contain a local and domain part.", "value");
        }

        string local = normalized.Substring(0, separator);
        string domain = normalized.Substring(separator + 1);
        if (domain.Length >= 2 && domain[0] == '[' && domain[domain.Length - 1] == ']')
        {
            return local + "@" + domain;
        }

        return local + "@" + domain.ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes the content ID.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string NormalizeContentIdWithValue(string value)
    {
        return EmailRelatedUri.NormalizeContentIdImplementationWithValue(value);
    }

    internal static string NormalizeContentIdImplementationWithIdentifier(EmailMessageId? identifier)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException("identifier");
        }

        string domain = identifier.IsRightDomainLiteral() ? "[" + identifier.GetIdRight() + "]" : identifier.GetIdRight().ToLowerInvariant();
        return identifier.GetIdLeft() + "@" + domain;
    }

    /// <summary>
    /// Normalizes the content ID.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string NormalizeContentIdWithIdentifier(EmailMessageId? identifier)
    {
        return EmailRelatedUri.NormalizeContentIdImplementationWithIdentifier(identifier);
    }

    internal static string NormalizeLocationImplementation(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A content location cannot be empty.", "value");
        }

        int fragment = normalized.IndexOf('#');
        if (fragment >= 0)
        {
            normalized = normalized.Substring(0, fragment);
        }

        normalized = NormalizePercentEscapes(normalized);
        int queryIndex = normalized.IndexOf('?');
        string query = queryIndex >= 0 ? normalized.Substring(queryIndex) : "";
        string resource = queryIndex >= 0 ? normalized.Substring(0, queryIndex) : normalized;
        string prefix = "";
        string path = resource;
        int scheme = FindSchemeSeparator(resource);
        if (resource.StartsWith("//", StringComparison.Ordinal))
        {
            int authorityEnd = FindAuthorityEnd(resource, 2);
            prefix = "//" + NormalizeAuthority(resource.Substring(2, authorityEnd - 2));
            path = resource.Substring(authorityEnd);
        }
        else if (scheme >= 0)
        {
            string schemeName = resource.Substring(0, scheme).ToLowerInvariant();
            int afterScheme = scheme + 1;
            prefix = schemeName + ":";
            path = resource.Substring(afterScheme);
            if (path.StartsWith("//", StringComparison.Ordinal))
            {
                int authorityEnd = FindAuthorityEnd(path, 2);
                string authority = path.Substring(2, authorityEnd - 2);
                prefix += "//" + NormalizeAuthority(authority);
                path = path.Substring(authorityEnd);
            }
        }

        path = NormalizePath(path);
        string result = prefix + path + query;
        if (result.Length == 0)
        {
            throw new ArgumentException("A content location cannot contain only a fragment.", "value");
        }

        return result;
    }

    /// <summary>
    /// Normalizes the location.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string NormalizeLocation(string? value)
    {
        return EmailRelatedUri.NormalizeLocationImplementation(value);
    }

    internal static bool IsAbsoluteImplementation(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value.StartsWith("//", StringComparison.Ordinal) || FindSchemeSeparator(value) >= 0;
    }

    /// <summary>
    /// Determines whether the current value or state is absolute.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is absolute; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool IsAbsolute(string value)
    {
        return EmailRelatedUri.IsAbsoluteImplementation(value);
    }

    internal static string ResolveLocationImplementation(string value, string? baseLocation)
    {
        string reference = NormalizeLocationImplementation(value);
        if (IsAbsoluteImplementation(reference) || baseLocation == null || baseLocation.Trim().Length == 0)
        {
            return reference;
        }

        string baseValue = NormalizeLocationImplementation(baseLocation);
        int baseQuery = baseValue.IndexOf('?');
        string baseResource = baseQuery >= 0 ? baseValue.Substring(0, baseQuery) : baseValue;
        if (reference[0] == '?')
        {
            return baseResource + reference;
        }

        int scheme = FindSchemeSeparator(baseResource);
        int authorityEnd = -1;
        if (scheme >= 0 && baseResource.Substring(scheme + 1).StartsWith("//", StringComparison.Ordinal))
        {
            authorityEnd = FindAuthorityEnd(baseResource, scheme + 3);
        }

        if (reference[0] == '/' && authorityEnd >= 0)
        {
            return NormalizeLocationImplementation(baseResource.Substring(0, authorityEnd) + reference);
        }

        if (reference[0] == '/')
        {
            return reference;
        }

        int slash = baseResource.LastIndexOf('/');
        string directory = slash >= 0 ? baseResource.Substring(0, slash + 1) : "";
        if (authorityEnd == baseResource.Length)
        {
            directory = baseResource + "/";
        }

        return NormalizeLocationImplementation(directory + reference);
    }

    /// <summary>
    /// Resolves the location.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "baseLocation">The base location used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ResolveLocation(string value, string? baseLocation)
    {
        return EmailRelatedUri.ResolveLocationImplementation(value, baseLocation);
    }

    static string NormalizePercentEscapes(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%' && index + 2 < value.Length && IsHex(value[index + 1]) && IsHex(value[index + 2]))
            {
                int decoded = HexValue(value[index + 1]) * 16 + HexValue(value[index + 2]);
                if (IsUnreserved(decoded))
                {
                    result.Append((char)decoded);
                }
                else
                {
                    result.Append('%');
                    result.Append(UpperHex(decoded >> 4));
                    result.Append(UpperHex(decoded & 15));
                }

                index += 3;
                continue;
            }

            result.Append(value[index]);
            index++;
        }

        return result.ToString();
    }

    static string NormalizePath(string value)
    {
        bool absolute = value.StartsWith("/", StringComparison.Ordinal);
        bool trailing = value.EndsWith("/", StringComparison.Ordinal);
        List<string> segments = new List<string>();
        int start = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '/')
            {
                string segment = value.Substring(start, index - start);
                if (segment.Length == 0 || segment == ".")
                {
                }
                else if (segment == "..")
                {
                    if (segments.Count > 0 && segments[segments.Count - 1] != "..")
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }
                    else if (!absolute)
                    {
                        segments.Add(segment);
                    }
                }
                else
                {
                    segments.Add(segment);
                }

                start = index + 1;
            }

            index++;
        }

        StringBuilder result = new StringBuilder();
        if (absolute)
        {
            result.Append('/');
        }

        index = 0;
        while (index < segments.Count)
        {
            if (index > 0)
            {
                result.Append('/');
            }

            result.Append(segments[index]);
            index++;
        }

        if (trailing && result.Length > 0 && result[result.Length - 1] != '/')
        {
            result.Append('/');
        }

        return result.ToString();
    }

    static int FindSchemeSeparator(string value)
    {
        if (value.Length == 0 || !IsAsciiLetter(value[0]))
        {
            return -1;
        }

        int index = 1;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == ':')
            {
                return index;
            }

            if (!IsAsciiLetter(current) && !char.IsDigit(current) && current != '+' && current != '-' && current != '.')
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    static int FindAuthorityEnd(string value, int start)
    {
        int index = start;
        while (index < value.Length && value[index] != '/')
        {
            index++;
        }

        return index;
    }

    static string NormalizeAuthority(string value)
    {
        int user = value.LastIndexOf('@');
        return user < 0 ? value.ToLowerInvariant() : value.Substring(0, user + 1) + value.Substring(user + 1).ToLowerInvariant();
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static bool IsHex(char value)
    {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'F' || value >= 'a' && value <= 'f';
    }

    static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        return value >= 'A' && value <= 'F' ? value - 'A' + 10 : value - 'a' + 10;
    }

    static bool IsUnreserved(int value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '-' || value == '.' || value == '_' || value == '~';
    }

    static char UpperHex(int value)
    {
        return (char)(value < 10 ? '0' + value : 'A' + value - 10);
    }
}
