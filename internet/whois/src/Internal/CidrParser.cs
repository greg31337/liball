namespace whois;

internal static class CidrParser
{
    internal static WhoisQuery ParsePrefix(string originalText)
    {
        InputGuard.ValidateQueryText(originalText, WhoisQueryKind.Automatic);
        string candidate = originalText.Trim();
        int separatorIndex = candidate.IndexOf('/');

        if (separatorIndex <= 0 || separatorIndex >= candidate.Length - 1)
        {
            throw InputGuard.CreateQueryError("A CIDR query must contain an address and prefix width.", "QueryText", originalText, WhoisQueryKind.Automatic, null);
        }

        if (candidate.IndexOf('/', separatorIndex + 1) >= 0)
        {
            throw InputGuard.CreateQueryError("A CIDR query must contain exactly one prefix separator.", "QueryText", originalText, WhoisQueryKind.Automatic, null);
        }

        string addressText = candidate.Substring(0, separatorIndex);
        string prefixText = candidate.Substring(separatorIndex + 1);
        if (addressText.IndexOf('%') >= 0 || addressText.IndexOf('[') >= 0 || addressText.IndexOf(']') >= 0)
        {
            throw InputGuard.CreateQueryError("A CIDR address must be an unscoped, unbracketed IP literal.", "QueryText", originalText, WhoisQueryKind.Automatic, null);
        }

        System.UriHostNameType hostType = System.Uri.CheckHostName(addressText);
        WhoisQueryKind queryKind = WhoisQueryKind.Automatic;
        int maximumWidth = 0;
        if (hostType == System.UriHostNameType.IPv4)
        {
            if (!WhoisQueryClassifier.IsStrictIpv4Literal(addressText))
            {
                throw InputGuard.CreateQueryError("An IPv4 CIDR query must contain exactly four decimal octets.", "QueryText", originalText, WhoisQueryKind.Automatic, null);
            }
            queryKind = WhoisQueryKind.IPv4Prefix;
            maximumWidth = 32;
        }
        else if (hostType == System.UriHostNameType.IPv6)
        {
            queryKind = WhoisQueryKind.IPv6Prefix;
            maximumWidth = 128;
        }
        else
        {
            throw InputGuard.CreateQueryError("A CIDR query must begin with an IPv4 or IPv6 literal.", "QueryText", originalText, WhoisQueryKind.Automatic, null);
        }

        int prefixLength = ParsePrefixLength(prefixText, originalText, queryKind);
        if (prefixLength > maximumWidth)
        {
            throw InputGuard.CreateQueryError("The CIDR prefix width exceeds the address-family width.", "QueryText", originalText, queryKind, null);
        }

        System.Net.IPAddress address = System.Net.IPAddress.Parse(addressText);
        System.Net.IPAddress network = MaskAddress(address, prefixLength);
        string normalized = network.ToString().ToLowerInvariant() + "/" + prefixLength.ToString(System.Globalization.CultureInfo.InvariantCulture);

        WhoisQuery query = new WhoisQuery();
        query.OriginalText = originalText;
        query.NormalizedText = normalized;
        query.RoutingKey = normalized;
        query.Kind = queryKind;
        query.IsValid = true;
        query.ValidationMessage = string.Empty;
        return query;
    }

    internal static int ParsePrefixLength(string prefixText, string originalText, WhoisQueryKind queryKind)
    {
        if (prefixText.Length == 0 || prefixText.Length > 3)
        {
            throw InputGuard.CreateQueryError("A CIDR prefix width must contain one to three decimal digits.", "QueryText", originalText, queryKind, null);
        }

        int index = 0;
        int value = 0;
        while (index < prefixText.Length)
        {
            char digit = prefixText[index];
            if (digit < '0' || digit > '9')
            {
                throw InputGuard.CreateQueryError("A CIDR prefix width must contain only decimal digits.", "QueryText", originalText, queryKind, null);
            }
            value = value * 10 + digit - '0';
            index = index + 1;
        }
        return value;
    }

    internal static System.Net.IPAddress MaskAddress(System.Net.IPAddress address, int prefixLength)
    {
        byte[] bytes = address.GetAddressBytes();
        int index = 0;
        while (index < bytes.Length)
        {
            int remainingBits = prefixLength - index * 8;
            if (remainingBits <= 0)
            {
                bytes[index] = 0;
            }
            else if (remainingBits < 8)
            {
                int mask = 255 << 8 - remainingBits;
                bytes[index] = (byte)(bytes[index] & mask);
            }
            index = index + 1;
        }
        return new System.Net.IPAddress(bytes);
    }
}
