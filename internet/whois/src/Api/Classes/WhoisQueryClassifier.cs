namespace whois;

/// <summary>
/// Classifies and normalizes domain, address, prefix, ASN, reverse-domain, and free-form WHOIS queries.
/// </summary>
public class WhoisQueryClassifier : IWhoisQueryClassifier
{
    /// <summary>
    /// Initializes the built-in WHOIS query classifier.
    /// </summary>
    public WhoisQueryClassifier()
    {
    }

    /// <summary>
    /// Classifies and normalizes caller lookup input.
    /// </summary>
    /// <param name="request">The caller lookup request.</param>
    /// <returns>The classified query and its normalization context.</returns>
    /// <exception cref="WhoisQueryException">The query is empty, malformed, out of range, or conflicts with its explicit kind hint.</exception>
    public WhoisQuery Classify(WhoisLookupRequest request)
    {
        InputGuard.RequireReference(request, "request");
        InputGuard.ValidateQueryText(request.Text, request.Kind);

        switch (request.Kind)
        {
            case WhoisQueryKind.Automatic:
                return ClassifyAutomatic(request.Text);
            case WhoisQueryKind.Domain:
                return NormalizeDomainHint(request.Text);
            case WhoisQueryKind.IPv4Address:
                return NormalizeAddress(request.Text, WhoisQueryKind.IPv4Address);
            case WhoisQueryKind.IPv6Address:
                return NormalizeAddress(request.Text, WhoisQueryKind.IPv6Address);
            case WhoisQueryKind.IPv4Prefix:
                return NormalizePrefix(request.Text, WhoisQueryKind.IPv4Prefix);
            case WhoisQueryKind.IPv6Prefix:
                return NormalizePrefix(request.Text, WhoisQueryKind.IPv6Prefix);
            case WhoisQueryKind.AutonomousSystem:
                return NormalizeAsn(request.Text);
            case WhoisQueryKind.ReverseDomain:
                return NormalizeReverseDomain(request.Text);
            case WhoisQueryKind.FreeForm:
                return NormalizeFreeForm(request.Text);
            default:
                throw InputGuard.CreateQueryError("The query-kind hint is not supported.", "Kind", request.Text, request.Kind, null);
        }
    }

    internal static WhoisQuery ClassifyAutomatic(string originalText)
    {
        string candidate = originalText.Trim();
        if (candidate.IndexOf('/') >= 0)
        {
            return CidrParser.ParsePrefix(originalText);
        }

        if (IsAsnCandidate(candidate))
        {
            return NormalizeAsn(originalText);
        }

        System.UriHostNameType hostType = System.Uri.CheckHostName(candidate);
        if (hostType == System.UriHostNameType.IPv4)
        {
            return NormalizeAddress(originalText, WhoisQueryKind.IPv4Address);
        }
        if (hostType == System.UriHostNameType.IPv6 && candidate.IndexOf('%') < 0 && candidate.IndexOf('[') < 0 && candidate.IndexOf(']') < 0)
        {
            return NormalizeAddress(originalText, WhoisQueryKind.IPv6Address);
        }

        if (LooksLikeReverseDomain(candidate))
        {
            return NormalizeReverseDomain(originalText);
        }

        if (IsDomainCandidate(candidate))
        {
            return IdnNormalizer.NormalizeDomain(originalText);
        }

        return NormalizeFreeForm(originalText);
    }

    internal static WhoisQuery NormalizeDomainHint(string originalText)
    {
        string candidate = originalText.Trim();
        System.UriHostNameType hostType = System.Uri.CheckHostName(candidate);
        if (hostType == System.UriHostNameType.IPv4 || hostType == System.UriHostNameType.IPv6 || candidate.IndexOf('/') >= 0)
        {
            throw InputGuard.CreateQueryError("The query does not match the domain kind hint.", "Kind", originalText, WhoisQueryKind.Domain, null);
        }
        return IdnNormalizer.NormalizeDomain(originalText);
    }

    internal static WhoisQuery NormalizeAddress(string originalText, WhoisQueryKind expectedKind)
    {
        string candidate = originalText.Trim();
        if (candidate.IndexOf('%') >= 0 || candidate.IndexOf('[') >= 0 || candidate.IndexOf(']') >= 0)
        {
            throw InputGuard.CreateQueryError("An address query must be an unscoped, unbracketed IP literal.", "QueryText", originalText, expectedKind, null);
        }

        System.UriHostNameType hostType = System.Uri.CheckHostName(candidate);
        if (expectedKind == WhoisQueryKind.IPv4Address && (hostType != System.UriHostNameType.IPv4 || !IsStrictIpv4Literal(candidate)))
        {
            throw InputGuard.CreateQueryError("The query does not contain an IPv4 address.", "Kind", originalText, expectedKind, null);
        }
        if (expectedKind == WhoisQueryKind.IPv6Address && hostType != System.UriHostNameType.IPv6)
        {
            throw InputGuard.CreateQueryError("The query does not contain an IPv6 address.", "Kind", originalText, expectedKind, null);
        }

        System.Net.IPAddress address = System.Net.IPAddress.Parse(candidate);
        string normalized = address.ToString().ToLowerInvariant();
        WhoisQuery query = new WhoisQuery();
        query.OriginalText = originalText;
        query.NormalizedText = normalized;
        query.RoutingKey = normalized;
        query.Kind = expectedKind;
        query.IsValid = true;
        query.ValidationMessage = string.Empty;
        return query;
    }

    internal static WhoisQuery NormalizePrefix(string originalText, WhoisQueryKind expectedKind)
    {
        WhoisQuery query = CidrParser.ParsePrefix(originalText);
        if (query.Kind != expectedKind)
        {
            throw InputGuard.CreateQueryError("The CIDR address family conflicts with the prefix kind hint.", "Kind", originalText, expectedKind, null);
        }
        return query;
    }

    internal static WhoisQuery NormalizeAsn(string originalText)
    {
        string candidate = originalText.Trim();
        int startIndex = 0;
        if (candidate.Length >= 2 && (candidate[0] == 'A' || candidate[0] == 'a') && (candidate[1] == 'S' || candidate[1] == 's'))
        {
            startIndex = 2;
        }

        if (startIndex >= candidate.Length || candidate.Length - startIndex > 10)
        {
            throw InputGuard.CreateQueryError("An autonomous system number must contain one to ten decimal digits.", "QueryText", originalText, WhoisQueryKind.AutonomousSystem, null);
        }

        int index = startIndex;
        long value = 0;
        while (index < candidate.Length)
        {
            char digit = candidate[index];
            if (digit < '0' || digit > '9')
            {
                throw InputGuard.CreateQueryError("An autonomous system number must contain only decimal digits.", "QueryText", originalText, WhoisQueryKind.AutonomousSystem, null);
            }
            value = value * 10 + digit - '0';
            if (value > 4294967295L)
            {
                throw InputGuard.CreateQueryError("The autonomous system number exceeds the unsigned 32-bit range.", "QueryText", originalText, WhoisQueryKind.AutonomousSystem, null);
            }
            index = index + 1;
        }

        string normalized = "AS" + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        WhoisQuery query = new WhoisQuery();
        query.OriginalText = originalText;
        query.NormalizedText = normalized;
        query.RoutingKey = normalized.ToLowerInvariant();
        query.Kind = WhoisQueryKind.AutonomousSystem;
        query.IsValid = true;
        query.ValidationMessage = string.Empty;
        return query;
    }

    internal static WhoisQuery NormalizeReverseDomain(string originalText)
    {
        WhoisQuery query = IdnNormalizer.NormalizeDomain(originalText);
        if (!query.NormalizedText.EndsWith(".in-addr.arpa", System.StringComparison.Ordinal) && !query.NormalizedText.EndsWith(".ip6.arpa", System.StringComparison.Ordinal))
        {
            throw InputGuard.CreateQueryError("The query does not belong to an IPv4 or IPv6 reverse-DNS zone.", "Kind", originalText, WhoisQueryKind.ReverseDomain, null);
        }
        query.Kind = WhoisQueryKind.ReverseDomain;
        return query;
    }

    internal static WhoisQuery NormalizeFreeForm(string originalText)
    {
        WhoisQuery query = new WhoisQuery();
        query.OriginalText = originalText;
        query.NormalizedText = originalText;
        query.RoutingKey = originalText;
        query.Kind = WhoisQueryKind.FreeForm;
        query.IsValid = true;
        query.ValidationMessage = string.Empty;
        return query;
    }

    internal static bool IsAsnCandidate(string candidate)
    {
        if (IsAllDecimal(candidate))
        {
            return true;
        }
        if (candidate.Length < 2 || (candidate[0] != 'A' && candidate[0] != 'a') || (candidate[1] != 'S' && candidate[1] != 's'))
        {
            return false;
        }
        return candidate.Length == 2 || candidate[2] >= '0' && candidate[2] <= '9';
    }

    internal static bool IsAllDecimal(string candidate)
    {
        if (candidate.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < candidate.Length)
        {
            if (candidate[index] < '0' || candidate[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool LooksLikeReverseDomain(string candidate)
    {
        if (candidate.Length > 0 && IdnNormalizer.IsLabelSeparator(candidate[candidate.Length - 1]))
        {
            candidate = candidate.Substring(0, candidate.Length - 1);
        }
        string lowered = candidate.ToLowerInvariant();
        return lowered.EndsWith(".in-addr.arpa", System.StringComparison.Ordinal) || lowered.EndsWith(".ip6.arpa", System.StringComparison.Ordinal);
    }

    internal static bool IsDomainCandidate(string candidate)
    {
        if (candidate.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < candidate.Length)
        {
            char value = candidate[index];
            if (char.IsWhiteSpace(value))
            {
                return false;
            }
            if (value < 128 && !IdnNormalizer.IsAsciiLetterOrDigit(value) && value != '-' && value != '.')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsStrictIpv4Literal(string candidate)
    {
        int index = 0;
        int partCount = 0;
        int partStart = 0;
        int digitCount = 0;
        int value = 0;
        while (index <= candidate.Length)
        {
            if (index == candidate.Length || candidate[index] == '.')
            {
                if (digitCount == 0 || value > 255 || digitCount > 1 && candidate[partStart] == '0')
                {
                    return false;
                }
                partCount = partCount + 1;
                partStart = index + 1;
                digitCount = 0;
                value = 0;
            }
            else
            {
                char digit = candidate[index];
                if (digit < '0' || digit > '9')
                {
                    return false;
                }
                digitCount = digitCount + 1;
                if (digitCount > 3)
                {
                    return false;
                }
                value = value * 10 + digit - '0';
            }
            index = index + 1;
        }
        return partCount == 4;
    }
}
