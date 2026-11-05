using System;

namespace markdown;

/// <summary>
/// Classifies link and image destinations according to the library's safe-default URI policy.
/// </summary>
internal class MarkdownUriPolicy
{
    /// <summary>
    /// Normalizes and classifies a destination, then evaluates its link and image safety.
    /// </summary>
    /// <param name="uri">The URI text to classify.</param>
    /// <returns>The resulting <see cref="MarkdownUriClassification"/> value.</returns>
    internal static MarkdownUriClassification Classify(string uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        string normalized = RemoveAsciiTabsAndLineEndings(TrimForPolicy(uri));
        string scheme = GetScheme(normalized);
        MarkdownUriKind kind = MarkdownUriKind.Relative;

        if (normalized.Length == 0)
        {
            kind = MarkdownUriKind.Empty;
        }
        else if (IsDangerousScheme(scheme))
        {
            kind = MarkdownUriKind.Unsafe;
        }
        else if (StringEqualsIgnoreCase(scheme, "data"))
        {
            kind = MarkdownUriKind.Data;
        }
        else if (StringEqualsIgnoreCase(scheme, "mailto"))
        {
            kind = MarkdownUriKind.Mailto;
        }
        else if (scheme.Length > 0)
        {
            kind = MarkdownUriKind.Absolute;
        }
        else if (normalized[0] == '#')
        {
            kind = MarkdownUriKind.Fragment;
        }

        bool safeForLink = IsKindSafeForLink(kind, scheme);
        bool safeForImage = safeForLink || IsSafeDataImageUri(normalized);

        return MarkdownUriClassification.Create(uri, normalized, scheme, kind, safeForLink, safeForImage);
    }

    /// <summary>
    /// Reports whether the URI is allowed as a link destination by the safe-default policy.
    /// </summary>
    /// <param name="uri">The URI text to classify.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal static bool IsSafeLink(string uri)
    {
        MarkdownUriClassification classification = Classify(uri);
        return classification.SafeForLink;
    }

    /// <summary>
    /// Reports whether the URI is allowed as an image source by the safe-default policy.
    /// </summary>
    /// <param name="uri">The URI text to classify.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal static bool IsSafeImage(string uri)
    {
        MarkdownUriClassification classification = Classify(uri);
        return classification.SafeForImage;
    }

    internal static string TrimForPolicy(string uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        int start = 0;
        int end = uri.Length;

        while (start < end && IsPolicyBoundaryTrimCharacter(uri[start]))
        {
            start = start + 1;
        }

        while (end > start && IsPolicyBoundaryTrimCharacter(uri[end - 1]))
        {
            end = end - 1;
        }

        return uri.Substring(start, end - start);
    }

    internal static bool IsPolicyBoundaryTrimCharacter(char character)
    {
        return MarkdownChar.IsWhitespace(character) || MarkdownChar.IsAsciiControl(character);
    }

    internal static string RemoveAsciiTabsAndLineEndings(string uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        int removedCount = 0;
        int index = 0;
        while (index < uri.Length)
        {
            char character = uri[index];
            if (character == '\t' || character == '\n' || character == '\r')
            {
                removedCount = removedCount + 1;
            }

            index = index + 1;
        }

        if (removedCount == 0)
        {
            return uri;
        }

        char[] characters = new char[uri.Length - removedCount];
        int outputIndex = 0;
        index = 0;
        while (index < uri.Length)
        {
            char character = uri[index];
            if (character != '\t' && character != '\n' && character != '\r')
            {
                characters[outputIndex] = character;
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return new string(characters);
    }

    internal static string GetScheme(string uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        int schemeEnd = GetSchemeEnd(uri);
        if (schemeEnd < 0)
        {
            return "";
        }

        return uri.Substring(0, schemeEnd).ToLowerInvariant();
    }

    internal static int GetSchemeEnd(string uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        if (uri.Length == 0 || !IsAsciiLetter(uri[0]))
        {
            return -1;
        }

        int index = 1;
        while (index < uri.Length)
        {
            char character = uri[index];
            if (character == ':')
            {
                return index;
            }

            if (character == '/' || character == '?' || character == '#')
            {
                return -1;
            }

            if (!IsSchemeCharacter(character))
            {
                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsKindSafeForLink(MarkdownUriKind kind, string scheme)
    {
        if (scheme == null)
        {
            throw new ArgumentNullException("scheme");
        }

        if (kind == MarkdownUriKind.Empty || kind == MarkdownUriKind.Fragment || kind == MarkdownUriKind.Relative || kind == MarkdownUriKind.Mailto)
        {
            return true;
        }

        if (kind == MarkdownUriKind.Absolute)
        {
            return StringEqualsIgnoreCase(scheme, "http") || StringEqualsIgnoreCase(scheme, "https");
        }

        return false;
    }

    internal static bool IsDangerousScheme(string scheme)
    {
        if (scheme == null)
        {
            throw new ArgumentNullException("scheme");
        }

        return StringEqualsIgnoreCase(scheme, "javascript")
            || StringEqualsIgnoreCase(scheme, "vbscript")
            || StringEqualsIgnoreCase(scheme, "file");
    }

    internal static bool IsSafeDataImageUri(string uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        string normalized = RemoveAsciiTabsAndLineEndings(TrimForPolicy(uri));
        if (!StartsWithIgnoreCase(normalized, "data:"))
        {
            return false;
        }

        int mediaTypeStart = 5;
        int mediaTypeEnd = mediaTypeStart;
        while (mediaTypeEnd < normalized.Length && normalized[mediaTypeEnd] != ';' && normalized[mediaTypeEnd] != ',')
        {
            mediaTypeEnd = mediaTypeEnd + 1;
        }

        if (mediaTypeEnd == mediaTypeStart)
        {
            return false;
        }

        string mediaType = normalized.Substring(mediaTypeStart, mediaTypeEnd - mediaTypeStart).ToLowerInvariant();
        return mediaType == "image/png"
            || mediaType == "image/gif"
            || mediaType == "image/jpeg"
            || mediaType == "image/jpg"
            || mediaType == "image/webp"
            || mediaType == "image/avif";
    }

    internal static bool IsAsciiLetter(char character)
    {
        return character >= 'a' && character <= 'z'
            || character >= 'A' && character <= 'Z';
    }

    internal static bool IsSchemeCharacter(char character)
    {
        return IsAsciiLetter(character)
            || character >= '0' && character <= '9'
            || character == '+'
            || character == '-'
            || character == '.';
    }

    internal static bool StringEqualsIgnoreCase(string left, string right)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool StartsWithIgnoreCase(string text, string prefix)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
