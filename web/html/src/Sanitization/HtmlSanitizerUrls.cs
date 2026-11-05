using System;
using System.Threading;

namespace html;

internal static class HtmlSanitizerUrls
{
    internal const string ValidationBaseUrl = "https://sanitizer.invalid/";

    internal static HtmlSanitizerUrlDecision Evaluate(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy)
    {
        return Evaluate(element, attribute, policy,
            HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerUrlDecision Evaluate(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy,
        HtmlLimits limits)
    {
        return Evaluate(element, attribute, policy, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerUrlDecision Evaluate(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        Validate(element, attribute, policy);
        ValidateLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();
        if (HtmlDomUrls.GetAttributeKind(element, attribute)
            != HtmlUrlAttributeKind.Single)
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        return EvaluateValue(element, attribute, attribute.GetValue(), policy,
            limits, cancellationToken);
    }

    internal static HtmlSanitizerUrlDecision EvaluateValue(HtmlNode element,
        HtmlAttribute attribute, string value,
        HtmlCompiledSanitizerPolicy policy)
    {
        return EvaluateValue(element, attribute, value, policy,
            HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerUrlDecision EvaluateValue(HtmlNode element,
        HtmlAttribute attribute, string value,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits)
    {
        return EvaluateValue(element, attribute, value, policy, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerUrlDecision EvaluateValue(HtmlNode element,
        HtmlAttribute attribute, string value,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        Validate(element, attribute, policy);
        ValidateLimits(limits);
        HtmlSanitizerUrlPolicy urlPolicy = policy.GetUrlPolicy(
            element.GetNamespaceUriStorage(), element.GetLocalNameStorage(),
            attribute.GetNamespaceUri(), attribute.GetLocalName());
        return EvaluateValue(value, urlPolicy, limits, cancellationToken);
    }

    internal static HtmlSanitizerUrlDecision EvaluateValue(string value,
        HtmlSanitizerUrlPolicy policy)
    {
        return EvaluateValue(value, policy, HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerUrlDecision EvaluateValue(string value,
        HtmlSanitizerUrlPolicy policy, HtmlLimits limits)
    {
        return EvaluateValue(value, policy, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerUrlDecision EvaluateValue(string value,
        HtmlSanitizerUrlPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        ValidateLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();

        HtmlUrlSchemeResult scheme = HtmlUrls.ExtractScheme(value, limits,
            cancellationToken);
        if (!scheme.IsComplete())
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        string candidate = scheme.GetDeobfuscatedValue();
        bool fragment = !scheme.HasScheme() && StartsWith(candidate, "#");
        bool protocolRelative = !scheme.HasScheme()
            && StartsWith(candidate, "//");
        bool relative = !scheme.HasScheme();
        if (fragment && !policy.GetAllowFragmentUrls()
            || protocolRelative
                && !policy.GetAllowProtocolRelativeUrls()
            || relative && !fragment && !protocolRelative
                && !policy.GetAllowRelativeUrls())
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        if (scheme.HasScheme() && scheme.GetScheme() == "data"
            && !AllowsDataUrl(candidate, policy,
                limits.GetMaximumDataUriBytes(), cancellationToken))
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        string baseUrl = policy.GetBaseUrl();
        bool syntheticBase = relative && baseUrl.Length == 0;
        string resolutionBase = syntheticBase ? ValidationBaseUrl : baseUrl;
        HtmlUrlResult resolved = scheme.HasScheme()
            ? HtmlUrls.Resolve(candidate, "", limits,
                cancellationToken)
            : HtmlUrls.Resolve(candidate, resolutionBase, limits,
                cancellationToken);
        if (!resolved.IsSuccess())
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        if ((!syntheticBase || protocolRelative)
            && !AllowsResolvedUrl(resolved.GetUrl(), policy))
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        string outputValue = relative && !policy.GetRewriteRelativeUrls()
            ? candidate : resolved.GetUrl();
        return new HtmlSanitizerUrlDecision(true, outputValue,
            outputValue != value);
    }

    internal static bool AllowsResolvedUrl(string value,
        HtmlSanitizerUrlPolicy policy)
    {
        Uri uri = new Uri(value, UriKind.Absolute);
        if (!policy.IsSchemeAllowed(uri.Scheme))
        {
            return false;
        }

        if (uri.Scheme == "data")
        {
            return true;
        }

        if (!policy.GetAllowCredentials() && HasAuthority(value)
            && uri.UserInfo.Length > 0)
        {
            return false;
        }

        string[] hosts = policy.GetAllowedHosts();
        if (hosts.Length > 0
            && (uri.Host.Length == 0 || !policy.IsHostAllowed(uri.Host)))
        {
            return false;
        }

        return uri.IsDefaultPort || uri.Port < 0
            || uri.Port > 0 && policy.IsPortAllowed(uri.Port);
    }

    internal static bool AllowsDataUrl(string value, HtmlSanitizerUrlPolicy policy)
    {
        return AllowsDataUrl(value, policy, int.MaxValue);
    }

    internal static bool AllowsDataUrl(string value,
        HtmlSanitizerUrlPolicy policy, int maximumBytes)
    {
        return AllowsDataUrl(value, policy, maximumBytes,
            CancellationToken.None);
    }

    internal static bool AllowsDataUrl(string value,
        HtmlSanitizerUrlPolicy policy, int maximumBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!policy.IsSchemeAllowed("data"))
        {
            return false;
        }

        int comma = value.IndexOf(',');
        if (comma < 5)
        {
            return false;
        }

        string header = value.Substring(5, comma - 5);
        int semicolon = header.IndexOf(';');
        string mediaType = semicolon < 0 ? header
            : header.Substring(0, semicolon);
        if (mediaType.Length == 0)
        {
            mediaType = "text/plain";
        }

        if (!HtmlSanitizerUrlPolicy.IsValidMediaType(mediaType)
            || !policy.IsDataMediaTypeAllowed(mediaType))
        {
            return false;
        }

        bool base64 = EndsWithBase64Marker(header);
        int payloadEnd = value.IndexOf('#', comma + 1);
        if (payloadEnd < 0)
        {
            payloadEnd = value.Length;
        }

        int size = base64
            ? GetBase64DecodedSize(value, comma + 1, payloadEnd,
                cancellationToken)
            : GetPercentDecodedSize(value, comma + 1, payloadEnd,
                cancellationToken);
        return size >= 0 && size <= maximumBytes
            && size <= policy.GetMaximumDataUrlBytes();
    }

    internal static void ValidateLimits(HtmlLimits limits)
    {
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
    }

    internal static bool EndsWithBase64Marker(string value)
    {
        const string Marker = ";base64";
        if (value.Length < Marker.Length)
        {
            return false;
        }

        int start = value.Length - Marker.Length;
        int index = 0;
        while (index < Marker.Length)
        {
            if (Ascii.ToLower(value[start + index]) != Marker[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int GetBase64DecodedSize(string value, int start, int end)
    {
        return GetBase64DecodedSize(value, start, end,
            CancellationToken.None);
    }

    internal static int GetBase64DecodedSize(string value, int start, int end,
        CancellationToken cancellationToken)
    {
        int significant = 0;
        int padding = 0;
        bool sawPadding = false;
        int index = start;
        while (index < end)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int character = value[index];
            if (character == '%')
            {
                if (index + 2 >= end
                    || !Ascii.IsHexDigit(value[index + 1])
                    || !Ascii.IsHexDigit(value[index + 2]))
                {
                    return -1;
                }

                character = HexValue(value[index + 1]) * 16
                    + HexValue(value[index + 2]);
                index = index + 3;
            }
            else
            {
                index = index + 1;
            }

            if (character == ' ' || character == '\t'
                || character == '\n' || character == '\r'
                || character == '\f')
            {
                continue;
            }

            if (character == '=')
            {
                sawPadding = true;
                padding = padding + 1;
                if (padding > 2)
                {
                    return -1;
                }

                continue;
            }

            if (sawPadding || !IsBase64Character(character))
            {
                return -1;
            }

            significant = significant + 1;
        }

        int total = significant + padding;
        if (total % 4 != 0)
        {
            return -1;
        }

        return total / 4 * 3 - padding;
    }

    internal static int GetPercentDecodedSize(string value, int start, int end)
    {
        return GetPercentDecodedSize(value, start, end,
            CancellationToken.None);
    }

    internal static int GetPercentDecodedSize(string value, int start, int end,
        CancellationToken cancellationToken)
    {
        int size = 0;
        int index = start;
        while (index < end)
        {
            cancellationToken.ThrowIfCancellationRequested();
            char current = value[index];
            if (current == '%')
            {
                if (index + 2 >= end
                    || !Ascii.IsHexDigit(value[index + 1])
                    || !Ascii.IsHexDigit(value[index + 2]))
                {
                    return -1;
                }

                size = size + 1;
                index = index + 3;
                continue;
            }

            if (UnicodeTools.IsHighSurrogate(current))
            {
                if (index + 1 >= end
                    || !UnicodeTools.IsLowSurrogate(value[index + 1]))
                {
                    return -1;
                }

                size = size + 4;
                index = index + 2;
                continue;
            }

            if (UnicodeTools.IsLowSurrogate(current))
            {
                return -1;
            }

            size = size + GetUtf8Length(current);
            index = index + 1;
        }

        return size;
    }

    internal static int GetUtf8Length(int value)
    {
        if (value <= 127)
        {
            return 1;
        }

        return value <= 2047 ? 2 : 3;
    }

    internal static bool IsBase64Character(int value)
    {
        return value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z'
            || value >= '0' && value <= '9'
            || value == '+' || value == '/';
    }

    internal static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        char lower = Ascii.ToLower(value);
        return lower - 'a' + 10;
    }

    internal static bool HasAuthority(string value)
    {
        int colon = value.IndexOf(':');
        return colon >= 0 && colon + 2 < value.Length
            && value[colon + 1] == '/' && value[colon + 2] == '/';
    }

    internal static bool StartsWith(string value, string prefix)
    {
        if (value.Length < prefix.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (value[index] != prefix[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void Validate(HtmlNode element, HtmlAttribute attribute,
        HtmlCompiledSanitizerPolicy policy)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (attribute == null)
        {
            throw new ArgumentNullException("attribute");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (element.GetKindStorage() != HtmlNodeKind.Element)
        {
            throw new ArgumentException(
                "URL sanitization requires an element node.", "element");
        }
    }
}
