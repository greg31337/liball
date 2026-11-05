namespace version;

internal static class NuGetTargetFrameworkParser
{
    internal static NuGetTargetFrameworkParseResult Parse(string? text)
    {
        if (text == null)
        {
            return Failure("", NuGetTargetFrameworkDiagnosticCodes.NullInput, "NuGet target framework cannot be null.", 0);
        }

        string trimmed = TrimText(text);
        if (trimmed.Length == 0)
        {
            return Failure(text, NuGetTargetFrameworkDiagnosticCodes.EmptyInput, "NuGet target framework cannot be empty.", 0);
        }

        string normalized = trimmed.ToLowerInvariant();

        if (StartsWith(normalized, "portable-"))
        {
            return ParsePortable(text, normalized);
        }

        if (StartsWith(normalized, "netstandard"))
        {
            return ParseNamedVersionFramework(text, normalized, "netstandard", NuGetTargetFrameworkKind.NetStandard);
        }

        if (StartsWith(normalized, "netcoreapp"))
        {
            return ParseNamedVersionFramework(text, normalized, "netcoreapp", NuGetTargetFrameworkKind.NetCoreApp);
        }

        if (StartsWith(normalized, "net"))
        {
            return ParseNetFrameworkOrModernNet(text, normalized);
        }

        if (StartsWith(normalized, "win"))
        {
            return ParseCompactVersionFramework(text, normalized, "win", NuGetTargetFrameworkKind.Windows);
        }

        if (StartsWith(normalized, "wpa"))
        {
            return ParseCompactVersionFramework(text, normalized, "wpa", NuGetTargetFrameworkKind.WindowsPhone);
        }

        if (StartsWith(normalized, "wp"))
        {
            return ParseCompactVersionFramework(text, normalized, "wp", NuGetTargetFrameworkKind.WindowsPhone);
        }

        if (StartsWith(normalized, "sl"))
        {
            return ParseCompactVersionFramework(text, normalized, "sl", NuGetTargetFrameworkKind.Silverlight);
        }

        if (StartsWith(normalized, "uap"))
        {
            return ParseNamedVersionFramework(text, normalized, "uap", NuGetTargetFrameworkKind.Uap);
        }

        return Failure(text, NuGetTargetFrameworkDiagnosticCodes.UnsupportedFramework, "Unsupported NuGet target framework.", GetTrimmedStartIndex(text));
    }

    internal static NuGetTargetFrameworkFallbackParseResult ParseFallbackList(string? text)
    {
        if (text == null)
        {
            return FallbackFailure("", NuGetTargetFrameworkDiagnosticCodes.NullInput, "NuGet target framework fallback list cannot be null.", 0);
        }

        if (TrimText(text).Length == 0)
        {
            return FallbackFailure(text, NuGetTargetFrameworkDiagnosticCodes.EmptyInput, "NuGet target framework fallback list cannot be empty.", 0);
        }

        System.Collections.Generic.List<NuGetTargetFramework> frameworks = new System.Collections.Generic.List<NuGetTargetFramework>();
        int startIndex = 0;
        int index = 0;

        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == ';')
            {
                string part = text.Substring(startIndex, index - startIndex);
                if (TrimText(part).Length == 0)
                {
                    return FallbackFailure(text, NuGetTargetFrameworkDiagnosticCodes.InvalidFallbackList, "NuGet target framework fallback list contains an empty entry.", startIndex);
                }

                NuGetTargetFrameworkParseResult result = Parse(part);
                if (result.IsFailure())
                {
                    return FallbackFailure(text, result.GetDiagnostic(0).GetCode(), result.GetDiagnostic(0).GetMessage(), startIndex + result.GetDiagnostic(0).GetIndex());
                }

                frameworks.Add(result.GetFramework());
                startIndex = index + 1;
            }

            index = index + 1;
        }

        return NuGetTargetFrameworkFallbackParseResult.CreateSuccess(text, new NuGetTargetFrameworkFallbackList(frameworks.ToArray()));
    }

    internal static NuGetTargetFrameworkParseResult ParseNetFrameworkOrModernNet(string sourceText, string normalized)
    {
        string body = normalized.Substring("net".Length);
        int hyphenIndex = FindCharacter(body, '-');

        if (hyphenIndex >= 0 || ContainsCharacter(body, '.'))
        {
            return ParseModernNet(sourceText, normalized, body, hyphenIndex);
        }

        string version = ParseCompactVersion(body);
        if (version.Length == 0)
        {
            return Failure(sourceText, NuGetTargetFrameworkDiagnosticCodes.InvalidVersion, "Invalid NuGet target framework version.", GetTrimmedStartIndex(sourceText) + "net".Length);
        }

        return Success(sourceText, normalized, NuGetTargetFrameworkKind.NetFramework, version, "", "", "", new string[0]);
    }

    internal static NuGetTargetFrameworkParseResult ParseModernNet(string sourceText, string normalized, string body, int hyphenIndex)
    {
        string versionPart = body;
        string platformPart = "";

        if (hyphenIndex >= 0)
        {
            versionPart = body.Substring(0, hyphenIndex);
            platformPart = body.Substring(hyphenIndex + 1);
        }

        if (!IsDottedVersion(versionPart))
        {
            return Failure(sourceText, NuGetTargetFrameworkDiagnosticCodes.InvalidVersion, "Invalid NuGet target framework version.", GetTrimmedStartIndex(sourceText) + "net".Length);
        }

        string platform = "";
        string platformVersion = "";
        if (platformPart.Length > 0)
        {
            int platformVersionIndex = FindFirstDigit(platformPart);
            if (platformVersionIndex < 0)
            {
                platform = platformPart;
            }
            else
            {
                platform = platformPart.Substring(0, platformVersionIndex);
                platformVersion = platformPart.Substring(platformVersionIndex);
                if (platform.Length == 0 || !IsDottedVersion(platformVersion))
                {
                    return Failure(sourceText, NuGetTargetFrameworkDiagnosticCodes.UnsupportedFramework, "Unsupported NuGet target framework platform.", GetTrimmedStartIndex(sourceText) + normalized.Length - platformPart.Length);
                }
            }
        }
        else if (hyphenIndex >= 0)
        {
            return Failure(sourceText, NuGetTargetFrameworkDiagnosticCodes.UnsupportedFramework, "Unsupported NuGet target framework platform.", GetTrimmedStartIndex(sourceText) + normalized.Length);
        }

        return Success(sourceText, normalized, NuGetTargetFrameworkKind.Net, versionPart, platform, platformVersion, "", new string[0]);
    }

    internal static NuGetTargetFrameworkParseResult ParseNamedVersionFramework(
        string sourceText,
        string normalized,
        string prefix,
        NuGetTargetFrameworkKind kind)
    {
        string versionPart = normalized.Substring(prefix.Length);
        if (!IsDottedVersion(versionPart))
        {
            return Failure(sourceText, NuGetTargetFrameworkDiagnosticCodes.InvalidVersion, "Invalid NuGet target framework version.", GetTrimmedStartIndex(sourceText) + prefix.Length);
        }

        return Success(sourceText, normalized, kind, versionPart, "", "", "", new string[0]);
    }

    internal static NuGetTargetFrameworkParseResult ParseCompactVersionFramework(
        string sourceText,
        string normalized,
        string prefix,
        NuGetTargetFrameworkKind kind)
    {
        string versionPart = ParseCompactVersion(normalized.Substring(prefix.Length));
        if (versionPart.Length == 0)
        {
            return Failure(sourceText, NuGetTargetFrameworkDiagnosticCodes.InvalidVersion, "Invalid NuGet target framework version.", GetTrimmedStartIndex(sourceText) + prefix.Length);
        }

        return Success(sourceText, normalized, kind, versionPart, "", "", "", new string[0]);
    }

    internal static NuGetTargetFrameworkParseResult ParsePortable(string sourceText, string normalized)
    {
        string profile = normalized.Substring("portable-".Length);
        if (profile.Length == 0)
        {
            return Failure(sourceText, NuGetTargetFrameworkDiagnosticCodes.InvalidProfile, "Portable NuGet target framework profile cannot be empty.", GetTrimmedStartIndex(sourceText) + "portable-".Length);
        }

        string[] frameworks = SplitProfile(profile);
        if (frameworks.Length == 0)
        {
            return Failure(sourceText, NuGetTargetFrameworkDiagnosticCodes.InvalidProfile, "Invalid portable NuGet target framework profile.", GetTrimmedStartIndex(sourceText) + "portable-".Length);
        }

        int index = 0;
        while (index < frameworks.Length)
        {
            if (!IsValidPortableProfileFramework(frameworks[index]))
            {
                return Failure(sourceText, NuGetTargetFrameworkDiagnosticCodes.InvalidProfile, "Invalid portable NuGet target framework profile.", GetTrimmedStartIndex(sourceText) + "portable-".Length);
            }

            index = index + 1;
        }

        return Success(sourceText, normalized, NuGetTargetFrameworkKind.Portable, "", "", "", profile, frameworks);
    }

    internal static NuGetTargetFrameworkParseResult Success(
        string sourceText,
        string normalized,
        NuGetTargetFrameworkKind kind,
        string version,
        string platform,
        string platformVersion,
        string profile,
        string[] portableFrameworks)
    {
        return NuGetTargetFrameworkParseResult.CreateSuccess(
            sourceText,
            new NuGetTargetFramework(sourceText, normalized, kind, version, platform, platformVersion, profile, portableFrameworks));
    }

    internal static NuGetTargetFrameworkParseResult Failure(string sourceText, string code, string message, int diagnosticIndex)
    {
        int safeIndex = diagnosticIndex;
        if (safeIndex > sourceText.Length)
        {
            safeIndex = sourceText.Length;
        }

        return NuGetTargetFrameworkParseResult.CreateFailure(
            sourceText,
            VersionDiagnostic.FromIndex(code, message, sourceText, safeIndex));
    }

    internal static NuGetTargetFrameworkFallbackParseResult FallbackFailure(string sourceText, string code, string message, int diagnosticIndex)
    {
        int safeIndex = diagnosticIndex;
        if (safeIndex > sourceText.Length)
        {
            safeIndex = sourceText.Length;
        }

        return NuGetTargetFrameworkFallbackParseResult.CreateFailure(
            sourceText,
            VersionDiagnostic.FromIndex(code, message, sourceText, safeIndex));
    }

    internal static string ParseCompactVersion(string text)
    {
        if (!ContainsOnlyDigits(text))
        {
            return "";
        }

        if (text.Length == 1)
        {
            return text;
        }

        if (text.Length == 2)
        {
            return text.Substring(0, 1) + "." + text.Substring(1, 1);
        }

        if (text.Length == 3)
        {
            return text.Substring(0, 1) + "." + text.Substring(1, 1) + "." + text.Substring(2, 1);
        }

        return "";
    }

    internal static bool IsDottedVersion(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        bool previousWasDot = true;
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '.')
            {
                if (previousWasDot)
                {
                    return false;
                }

                previousWasDot = true;
            }
            else if (VersionChar.IsDigit(text[index]))
            {
                previousWasDot = false;
            }
            else
            {
                return false;
            }

            index = index + 1;
        }

        return !previousWasDot;
    }

    internal static string[] SplitProfile(string text)
    {
        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
        int startIndex = 0;
        int index = 0;

        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == '+')
            {
                if (index == startIndex)
                {
                    return new string[0];
                }

                parts.Add(text.Substring(startIndex, index - startIndex));
                startIndex = index + 1;
            }

            index = index + 1;
        }

        return parts.ToArray();
    }

    internal static bool IsValidPortableProfileFramework(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsAlphaNumeric(text[index]) && text[index] != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ContainsOnlyDigits(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsDigit(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool StartsWith(string text, string prefix)
    {
        if (prefix.Length > text.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (text[index] != prefix[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ContainsCharacter(string text, char value)
    {
        return FindCharacter(text, value) >= 0;
    }

    internal static int FindCharacter(string text, char value)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == value)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindFirstDigit(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (VersionChar.IsDigit(text[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int GetTrimmedStartIndex(string text)
    {
        int index = 0;

        while (index < text.Length && VersionChar.IsWhitespace(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal static string TrimText(string text)
    {
        int startIndex = 0;
        int endIndex = text.Length;

        while (startIndex < endIndex && VersionChar.IsWhitespace(text[startIndex]))
        {
            startIndex = startIndex + 1;
        }

        while (endIndex > startIndex && VersionChar.IsWhitespace(text[endIndex - 1]))
        {
            endIndex = endIndex - 1;
        }

        return text.Substring(startIndex, endIndex - startIndex);
    }
}
