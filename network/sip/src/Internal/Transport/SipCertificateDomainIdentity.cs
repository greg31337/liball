namespace sip;

internal static class SipCertificateDomainIdentity
{
    internal static string[] ExtractDomains(
        System.Security.Cryptography.X509Certificates.X509Certificate2? certificate)
    {
        if (certificate == null)
        {
            return new string[0];
        }
        System.Collections.Generic.List<string> uriDomains =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> dnsDomains =
            new System.Collections.Generic.List<string>();
        byte[]? subjectAlternativeName = FindSubjectAlternativeName(certificate);
        if (subjectAlternativeName != null)
        {
            ExtractGeneralNames(subjectAlternativeName, uriDomains, dnsDomains);
            return uriDomains.Count > 0 ? uriDomains.ToArray() : dnsDomains.ToArray();
        }
        string commonName = certificate.GetNameInfo(
            System.Security.Cryptography.X509Certificates.X509NameType.DnsName, false);
        string? normalized = NormalizeDomain(commonName);
        return normalized == null ? new string[0] : new string[] { normalized };
    }

    internal static bool MatchesCertificate(
        System.Security.Cryptography.X509Certificates.X509Certificate? certificate,
        string? domain)
    {
        if (certificate == null)
        {
            return false;
        }
        byte[] bytes = certificate.Export(
            System.Security.Cryptography.X509Certificates.X509ContentType.Cert);
        using (System.Security.Cryptography.X509Certificates.X509Certificate2 value =
            System.Security.Cryptography.X509Certificates.X509CertificateLoader
                .LoadCertificate(bytes))
        {
            return MatchesDomains(ExtractDomains(value), domain);
        }
    }

    internal static bool MatchesDomains(string[]? identities, string? domain)
    {
        string? normalized = NormalizeDomain(domain);
        if (identities == null || normalized == null)
        {
            return false;
        }
        int index = 0;
        while (index < identities.Length)
        {
            string? identity = NormalizeDomain(identities[index]);
            if (identity != null && SipAscii.EqualsIgnoreCase(identity, normalized))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static string? NormalizeDomain(string? domain)
    {
        if (domain == null || domain.Length == 0)
        {
            return null;
        }
        string value = domain;
        if (value.Length > 1 && value[value.Length - 1] == '.')
        {
            value = value.Substring(0, value.Length - 1);
        }
        if (value.Length == 0 || value.Length > 253)
        {
            return null;
        }
        if (value.IndexOf('*') >= 0 || value[0] == '.')
        {
            return value.ToLowerInvariant();
        }
        if (!CanConvertDomain(value))
        {
            return null;
        }
        string ascii = new System.Globalization.IdnMapping().GetAscii(value)
            .ToLowerInvariant();
        if (ascii.Length == 0 || ascii.Length > 253)
        {
            return null;
        }
        string[] labels = ascii.Split('.');
        int labelIndex = 0;
        while (labelIndex < labels.Length)
        {
            string label = labels[labelIndex];
            if (label.Length == 0 || label.Length > 63
                || label[0] == '-' || label[label.Length - 1] == '-')
            {
                return null;
            }
            int characterIndex = 0;
            while (characterIndex < label.Length)
            {
                char valueCharacter = label[characterIndex];
                if (!(valueCharacter >= 'a' && valueCharacter <= 'z')
                    && !(valueCharacter >= '0' && valueCharacter <= '9')
                    && valueCharacter != '-')
                {
                    return null;
                }
                characterIndex = characterIndex + 1;
            }
            labelIndex = labelIndex + 1;
        }
        return ascii;
    }

    static byte[]? FindSubjectAlternativeName(
        System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
    {
        int index = 0;
        while (index < certificate.Extensions.Count)
        {
            System.Security.Cryptography.X509Certificates.X509Extension extension =
                certificate.Extensions[index];
            if (extension.Oid != null && extension.Oid.Value == "2.5.29.17")
            {
                return extension.RawData;
            }
            index = index + 1;
        }
        return null;
    }

    static void ExtractGeneralNames(byte[] source,
        System.Collections.Generic.List<string> uriDomains,
        System.Collections.Generic.List<string> dnsDomains)
    {
        int outerHeader = GetHeaderLength(source, 0);
        int outerLength = GetContentLength(source, 0);
        if (outerHeader < 0 || outerLength < 0 || source[0] != 48
            || outerHeader > source.Length - outerLength)
        {
            return;
        }
        int offset = outerHeader;
        int end = outerHeader + outerLength;
        while (offset < end)
        {
            int header = GetHeaderLength(source, offset);
            int length = GetContentLength(source, offset);
            if (header < 0 || length < 0 || offset + header > end - length)
            {
                return;
            }
            int tag = source[offset];
            if ((tag == 130 || tag == 134)
                && IsAsciiValue(source, offset + header, length))
            {
                string value = System.Text.Encoding.ASCII.GetString(
                    source, offset + header, length);
                string? domain = tag == 134
                    ? ParseSipUriDomain(value) : NormalizeDomain(value);
                if (domain != null)
                {
                    AddDistinct(tag == 134 ? uriDomains : dnsDomains, domain);
                }
            }
            offset = offset + header + length;
        }
    }

    static string? ParseSipUriDomain(string value)
    {
        if (value.Length <= 4
            || !SipAscii.EqualsIgnoreCase(value.Substring(0, 4), "sip:"))
        {
            return null;
        }
        int end = value.Length;
        int semicolon = value.IndexOf(';', 4);
        int question = value.IndexOf('?', 4);
        if (semicolon >= 0 && semicolon < end)
        {
            end = semicolon;
        }
        if (question >= 0 && question < end)
        {
            end = question;
        }
        if (end <= 4 || value.IndexOf('@', 4, end - 4) >= 0)
        {
            return null;
        }
        string host = value.Substring(4, end - 4);
        int colon = host.LastIndexOf(':');
        if (colon >= 0)
        {
            if (host.IndexOf(':') != colon
                || !ValidPortText(host.Substring(colon + 1)))
            {
                return null;
            }
            host = host.Substring(0, colon);
        }
        if (host.Length == 0 || host[0] == '['
            || SipUriSyntax.IsIpv4Address(host) || SipUriSyntax.IsIpv6Address(host))
        {
            return null;
        }
        return NormalizeDomain(host);
    }

    static bool CanConvertDomain(string value)
    {
        if (value[0] == '.' || value[value.Length - 1] == '.'
            || value.IndexOf("..", System.StringComparison.Ordinal) >= 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsControl(character) || char.IsWhiteSpace(character)
                || char.IsSurrogate(character)
                || character <= 127 && !char.IsLetterOrDigit(character)
                    && character != '-' && character != '.')
            {
                return false;
            }
            index = index + 1;
        }
        string[] labels = value.Split('.');
        int labelIndex = 0;
        while (labelIndex < labels.Length)
        {
            string label = labels[labelIndex];
            if (label.Length == 0 || label[0] == '-'
                || label[label.Length - 1] == '-')
            {
                return false;
            }
            labelIndex = labelIndex + 1;
        }
        return true;
    }

    static bool ValidPortText(string value)
    {
        if (value.Length == 0 || value.Length > 5)
        {
            return false;
        }
        int port = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            port = port * 10 + value[index] - '0';
            index = index + 1;
        }
        return port > 0 && port <= 65535;
    }

    static void AddDistinct(System.Collections.Generic.List<string> values,
        string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (SipAscii.EqualsIgnoreCase(values[index], value))
            {
                return;
            }
            index = index + 1;
        }
        values.Add(value);
    }

    static int GetHeaderLength(byte[] source, int offset)
    {
        if (offset < 0 || offset > source.Length - 2)
        {
            return -1;
        }
        int first = source[offset + 1];
        if (first < 128)
        {
            return 2;
        }
        int count = first & 127;
        return count <= 0 || count > 4 || offset > source.Length - 2 - count
            ? -1 : 2 + count;
    }

    static int GetContentLength(byte[] source, int offset)
    {
        int header = GetHeaderLength(source, offset);
        if (header < 0)
        {
            return -1;
        }
        int first = source[offset + 1];
        if (first < 128)
        {
            return first;
        }
        int count = first & 127;
        long length = 0;
        int index = 0;
        while (index < count)
        {
            length = length * 256 + source[offset + 2 + index];
            index = index + 1;
        }
        return length > int.MaxValue ? -1 : (int)length;
    }

    static bool IsAsciiValue(byte[] source, int offset, int length)
    {
        int index = 0;
        while (index < length)
        {
            if (source[offset + index] > 127)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
