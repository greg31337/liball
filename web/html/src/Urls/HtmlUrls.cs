using System;
using System.Threading;

namespace html;

/// <summary>Parses, normalizes, resolves, and classifies URLs used in HTML content.</summary>
internal static class HtmlUrls
{
    /// <summary>Extracts and deobfuscates the leading scheme from a URL-like value.</summary>
    /// <param name="value">The URL-like text to inspect.</param>
    /// <returns>The detected scheme and flags describing ambiguity or deobfuscation.</returns>
    /// <remarks>HTML character references are decoded before ASCII control characters and spaces are removed. Scheme recognition then follows the ASCII letter, digit, plus, hyphen, and period grammar used by URLs.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public static HtmlUrlSchemeResult ExtractScheme(string value)
    {
        return ExtractScheme(value, HtmlLimits.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Extracts and deobfuscates a URL scheme while enforcing resource limits and cancellation.</summary>
    /// <param name="value">The URL-like text to inspect.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The detected scheme and flags describing ambiguity or deobfuscation.</returns>
    /// <remarks>HTML character references are decoded before ASCII control characters and spaces are removed. Scheme recognition then follows the ASCII letter, digit, plus, hyphen, and period grammar used by URLs.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlUrlSchemeResult ExtractScheme(string value,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        if (!budget.Allows(HtmlLimitKind.UrlCharacters, value.Length))
        {
            return CreateLimitResult();
        }

        string decoded = DecodeAttributeValue(value, limits,
            cancellationToken);
        if (!budget.Allows(HtmlLimitKind.UrlCharacters, decoded.Length))
        {
            return CreateLimitResult();
        }

        char[] characters = new char[decoded.Length];
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < decoded.Length)
        {
            budget.CheckCancellation();
            char current = decoded[inputIndex];
            if (!IsSecurityIgnorable(current))
            {
                characters[outputIndex] = current;
                outputIndex = outputIndex + 1;
            }

            inputIndex = inputIndex + 1;
        }

        string deobfuscatedValue = outputIndex == decoded.Length
            ? decoded : new string(characters, 0, outputIndex);
        string scheme = ExtractAsciiScheme(deobfuscatedValue, budget);
        bool hasScheme = scheme.Length != 0;
        bool deobfuscated = decoded != value
            || outputIndex != decoded.Length;
        return HtmlUrlSchemeResult.Create(deobfuscatedValue, scheme,
            hasScheme, deobfuscated, true, false);
    }

    /// <summary>Normalizes an absolute URL without supplying a base for relative references.</summary>
    /// <param name="value">The absolute or relative URL text to normalize.</param>
    /// <returns>The normalized absolute URL and classification flags, or a failed result when the input is relative or malformed.</returns>
    /// <remarks>Use <see cref="Resolve(string,string)"/> when relative references are expected.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public static HtmlUrlResult Normalize(string value)
    {
        return Normalize(value, HtmlLimits.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Normalizes an absolute URL while enforcing resource limits and cancellation.</summary>
    /// <param name="value">The absolute or relative URL text to normalize.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The normalized absolute URL and classification flags, or a failed result when the input is relative, malformed, or exceeds a limit.</returns>
    /// <remarks>Use <see cref="Resolve(string,string)"/> when relative references are expected.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlUrlResult Normalize(string value, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        return ResolveCore(value, "", limits, cancellationToken);
    }

    /// <summary>Resolves a URL reference against the supplied base URL.</summary>
    /// <param name="value">The URL reference to resolve.</param>
    /// <param name="baseUrl">The trusted base URL used to resolve relative references.</param>
    /// <returns>The resolved URL, classification flags, and diagnostics.</returns>
    /// <remarks>The base must be an absolute URL capable of resolving a path. Absolute input is normalized directly and does not depend on the base.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlUrlResult Resolve(string value, string baseUrl)
    {
        return Resolve(value, baseUrl, HtmlLimits.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Resolves a URL reference against the supplied base URL.</summary>
    /// <param name="value">The URL reference to resolve.</param>
    /// <param name="baseUrl">The trusted base URL used to resolve relative references.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The resolved URL, classification flags, and diagnostics.</returns>
    /// <remarks>The base must be an absolute URL capable of resolving a path. Absolute input is normalized directly and does not depend on the base.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlUrlResult Resolve(string value, string baseUrl,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        if (baseUrl == null)
        {
            throw new ArgumentNullException("baseUrl");
        }

        return ResolveCore(value, baseUrl, limits, cancellationToken);
    }

    internal static HtmlUrlResult ResolveCore(string value, string baseUrl,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        HtmlUrlSchemeResult valueScheme = ExtractScheme(value, limits,
            cancellationToken);
        if (!valueScheme.IsComplete())
        {
            return CreateUrlLimitResult(false,
                valueScheme.WasDeobfuscated());
        }

        string candidate = valueScheme.GetDeobfuscatedValue();
        if (valueScheme.HasScheme())
        {
            return NormalizeAbsolute(candidate, false,
                valueScheme.WasDeobfuscated(), budget);
        }

        if (HasColonInFirstPathSegment(candidate))
        {
            return CreateInvalidUrlResult(true,
                valueScheme.WasDeobfuscated(),
                "The relative URL has an invalid first path segment.");
        }

        if (baseUrl.Length == 0)
        {
            return CreateInvalidUrlResult(true,
                valueScheme.WasDeobfuscated(),
                "A relative URL requires an absolute base URL.");
        }

        HtmlUrlSchemeResult baseScheme = ExtractScheme(baseUrl, limits,
            cancellationToken);
        if (!baseScheme.IsComplete())
        {
            return CreateUrlLimitResult(true,
                valueScheme.WasDeobfuscated()
                    || baseScheme.WasDeobfuscated());
        }

        bool deobfuscated = valueScheme.WasDeobfuscated()
            || baseScheme.WasDeobfuscated();
        string baseCandidate = baseScheme.GetDeobfuscatedValue();
        if (!baseScheme.HasScheme()
            || !Uri.IsWellFormedUriString(baseCandidate,
                UriKind.Absolute))
        {
            return CreateInvalidUrlResult(true, deobfuscated,
                "The base URL must be a well-formed absolute URL.");
        }

        Uri absoluteBase = new Uri(baseCandidate, UriKind.Absolute);
        if (!CanResolveRelative(absoluteBase))
        {
            return CreateInvalidUrlResult(true, deobfuscated,
                "The base URL cannot resolve relative paths.");
        }

        if (!IsWellFormedRelativeReference(candidate))
        {
            return CreateInvalidUrlResult(true, deobfuscated,
                "The relative URL is not well-formed.");
        }

        if (StartsWithNetworkPath(candidate)
            && !IsWellFormedNetworkPath(candidate, baseScheme.GetScheme(),
                budget))
        {
            return budget.HasExceededLimit()
                ? CreateUrlLimitResult(true, deobfuscated)
                : CreateInvalidUrlResult(true, deobfuscated,
                    "The relative URL has an invalid authority.");
        }

        Uri resolved = candidate.Length == 0
            ? absoluteBase : new Uri(absoluteBase, candidate);
        return CreateSuccessfulUrlResult(resolved, true, deobfuscated,
            budget);
    }

    internal static HtmlUrlResult NormalizeAbsolute(string value, bool relative,
        bool deobfuscated, HtmlBudget budget)
    {
        if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            return CreateInvalidUrlResult(relative, deobfuscated,
                "The URL is not a well-formed absolute URL.");
        }

        Uri absolute = new Uri(value, UriKind.Absolute);
        return CreateSuccessfulUrlResult(absolute, relative, deobfuscated,
            budget);
    }

    internal static HtmlUrlResult CreateSuccessfulUrlResult(Uri value, bool relative,
        bool deobfuscated, HtmlBudget budget)
    {
        string normalized = value.AbsoluteUri;
        if (!budget.Allows(HtmlLimitKind.UrlCharacters, normalized.Length))
        {
            return CreateUrlLimitResult(relative, deobfuscated);
        }

        return new HtmlUrlResult(normalized, value.Scheme, true, relative,
            deobfuscated, false, new HtmlDiagnostic[0]);
    }

    internal static bool CanResolveRelative(Uri value)
    {
        string path = value.AbsolutePath;
        return path.Length != 0 && path[0] == '/';
    }

    internal static bool StartsWithNetworkPath(string value)
    {
        return value.Length >= 2 && value[0] == '/' && value[1] == '/';
    }

    internal static bool IsWellFormedNetworkPath(string value, string baseScheme,
        HtmlBudget budget)
    {
        long probeLength = (long)baseScheme.Length + 1L
            + (long)value.Length;
        if (probeLength > budget.GetLimits().GetMaximumUrlCharacters())
        {
            RecordUrlLimit(budget);
            return false;
        }

        string probe = baseScheme + ":" + value;
        return Uri.IsWellFormedUriString(probe, UriKind.Absolute);
    }

    internal static bool IsWellFormedRelativeReference(string value)
    {
        if (value.Length == 0
            || Uri.IsWellFormedUriString(value, UriKind.Relative))
        {
            return true;
        }

        int fragment = value.IndexOf('#');
        if (fragment < 0)
        {
            return false;
        }

        string prefix = value.Substring(0, fragment);
        if (prefix.Length != 0
            && !Uri.IsWellFormedUriString(prefix, UriKind.Relative))
        {
            return false;
        }

        return IsWellFormedFragment(value, fragment + 1);
    }

    internal static bool IsWellFormedFragment(string value, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '%')
            {
                if (index + 2 >= value.Length
                    || !Ascii.IsHexDigit(value[index + 1])
                    || !Ascii.IsHexDigit(value[index + 2]))
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (UnicodeTools.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length
                    || !UnicodeTools.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (UnicodeTools.IsLowSurrogate(current))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void RecordUrlLimit(HtmlBudget budget)
    {
        int maximum = budget.GetLimits().GetMaximumUrlCharacters();
        budget.TryConsume(HtmlLimitKind.UrlCharacters, maximum);
        budget.TryConsume(HtmlLimitKind.UrlCharacters, 1);
    }

    internal static bool HasColonInFirstPathSegment(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == ':')
            {
                return true;
            }

            if (current == '/' || current == '?' || current == '#')
            {
                return false;
            }

            index = index + 1;
        }

        return false;
    }

    internal static HtmlUrlResult CreateInvalidUrlResult(bool relative,
        bool deobfuscated, string message)
    {
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.UrlInvalid, HtmlDiagnosticSeverity.Error,
            message, HtmlSourceSpan.CreateUnknown());
        return new HtmlUrlResult("", "", false, relative, deobfuscated,
            false, new[] { diagnostic });
    }

    internal static HtmlUrlResult CreateUrlLimitResult(bool relative,
        bool deobfuscated)
    {
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.ResourceLimitExceeded,
            HtmlDiagnosticSeverity.Error,
            "The URL character limit was exceeded.",
            HtmlSourceSpan.CreateUnknown());
        return new HtmlUrlResult("", "", false, relative, deobfuscated,
            true, new[] { diagnostic });
    }

    internal static string DecodeAttributeValue(string value, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        int maximum = limits.GetMaximumUrlCharacters();
        HtmlLimits entityLimits = limits.Copy();
        entityLimits.SetMaximumInputCharacters(maximum);
        entityLimits.SetMaximumOutputCharacters(maximum);
        return HtmlEntities.DecodeAttribute(value, entityLimits,
            cancellationToken).GetString();
    }

    internal static string ExtractAsciiScheme(string value, HtmlBudget budget)
    {
        if (value.Length == 0 || !Ascii.IsAlpha(value[0]))
        {
            return "";
        }

        int index = 1;
        while (index < value.Length)
        {
            budget.CheckCancellation();
            char current = value[index];
            if (current == ':')
            {
                return FoldScheme(value, index);
            }

            if (!Ascii.IsAlphanumeric(current) && current != '+'
                && current != '-' && current != '.')
            {
                return "";
            }

            index = index + 1;
        }

        return "";
    }

    internal static string FoldScheme(string value, int length)
    {
        char[] characters = new char[length];
        int index = 0;
        while (index < length)
        {
            characters[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string(characters);
    }

    internal static bool IsSecurityIgnorable(char value)
    {
        return Ascii.IsControl(value) || value == ' ';
    }

    internal static HtmlUrlSchemeResult CreateLimitResult()
    {
        return HtmlUrlSchemeResult.Create("", "", false, false, false,
            true);
    }
}
