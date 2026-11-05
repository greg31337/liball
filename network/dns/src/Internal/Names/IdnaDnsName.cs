using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace dns;

internal static class IdnaDnsName
{
    internal static DnsTextResult ConvertIdnaToName(
        string text,
        DnsName? origin)
    {
        if (text == null)
        {
            return PresentationDnsName.FailText(
                DnsDiagnosticCode.InvalidText,
                "name",
                "An IDNA domain name is required.",
                0);
        }

        if (text.Length == 0)
        {
            return BuildAsciiName(string.Empty, origin);
        }

        if (text.Length == 1 && IsIdnaSeparator(text[0]))
        {
            return BuildAsciiName(".", origin);
        }

        IdnMapping mapping = CreateMapping();
        string asciiText = mapping.GetAscii(text);
        return BuildAsciiName(asciiText, origin);
    }

    internal static DnsTextResult ConvertNameToUnicode(DnsName name)
    {
        RawDnsNameResult rawResult = RawDnsName.ValidateName(name);
        if (!rawResult.Success)
        {
            return PresentationDnsName.FromRawName(rawResult, string.Empty);
        }

        DnsName? optionalName = rawResult.Name;
        if (optionalName == null)
        {
            return PresentationDnsName.FailText(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.",
                0);
        }

        DnsName validatedName = optionalName;
        if (validatedName.IsAbsolute && validatedName.Labels.Count == 0)
        {
            return UnicodeSuccess(validatedName, ".");
        }

        IdnMapping mapping = CreateMapping();
        StringBuilder builder = new StringBuilder();
        int labelIndex = 0;
        while (labelIndex < validatedName.Labels.Count)
        {
            if (labelIndex > 0)
            {
                builder.Append('.');
            }

            byte[] label = validatedName.Labels[labelIndex];
            StringBuilder asciiLabel = new StringBuilder();
            int byteIndex = 0;
            while (byteIndex < label.Length)
            {
                byte value = label[byteIndex];
                if (!IsIdnaAscii(value))
                {
                    return PresentationDnsName.FailText(
                        DnsDiagnosticCode.InvalidLabel,
                        $"labels[{labelIndex}]",
                        "An IDNA label must contain only ASCII letters, digits, or hyphens.",
                        byteIndex);
                }

                asciiLabel.Append((char)value);
                byteIndex = byteIndex + 1;
            }

            builder.Append(mapping.GetUnicode(asciiLabel.ToString()));
            labelIndex = labelIndex + 1;
        }

        if (validatedName.IsAbsolute)
        {
            builder.Append('.');
        }

        return UnicodeSuccess(validatedName, builder.ToString());
    }

    internal static DnsTextResult BuildAsciiName(
        string asciiText,
        DnsName? origin)
    {
        if (asciiText == ".")
        {
            RawDnsNameResult rootResult = RawDnsName.CreateRootName();
            return PresentationDnsName.FromRawName(rootResult, asciiText);
        }

        List<byte[]> labels = new List<byte[]>();
        List<byte> currentLabel = new List<byte>();
        bool isAbsolute = false;
        int index = 0;
        while (index < asciiText.Length)
        {
            char character = asciiText[index];
            if (character == '.')
            {
                if (currentLabel.Count == 0)
                {
                    return PresentationDnsName.FailText(
                        DnsDiagnosticCode.InvalidLabel,
                        "name",
                        "An IDNA domain name contains an empty label.",
                        index);
                }

                labels.Add(currentLabel.ToArray());
                currentLabel = new List<byte>();
                index = index + 1;
                if (index == asciiText.Length)
                {
                    isAbsolute = true;
                }

                continue;
            }

            if (character > 127)
            {
                return PresentationDnsName.FailText(
                    DnsDiagnosticCode.InvalidText,
                    "name",
                    "IDNA conversion produced a non-ASCII character.",
                    index);
            }

            currentLabel.Add((byte)character);
            index = index + 1;
        }

        if (!isAbsolute && currentLabel.Count > 0)
        {
            labels.Add(currentLabel.ToArray());
        }

        if (!isAbsolute && origin != null)
        {
            RawDnsNameResult originResult = RawDnsName.ValidateName(origin);
            if (!originResult.Success)
            {
                return PresentationDnsName.FromRawName(originResult, asciiText);
            }

            DnsName? optionalOrigin = originResult.Name;
            if (optionalOrigin == null || !optionalOrigin.IsAbsolute)
            {
                return PresentationDnsName.FailText(
                    DnsDiagnosticCode.InvalidText,
                    "origin",
                    "An IDNA origin must be an absolute DNS name.",
                    0);
            }

            int originIndex = 0;
            while (originIndex < optionalOrigin.Labels.Count)
            {
                labels.Add(optionalOrigin.Labels[originIndex]);
                originIndex = originIndex + 1;
            }

            isAbsolute = true;
        }

        RawDnsNameResult result = RawDnsName.BuildName(labels, isAbsolute);
        return PresentationDnsName.FromRawName(result, asciiText);
    }

    internal static IdnMapping CreateMapping()
    {
        IdnMapping mapping = new IdnMapping();
        mapping.UseStd3AsciiRules = true;
        return mapping;
    }

    internal static bool IsIdnaAscii(byte value)
    {
        return value >= 48 && value <= 57 ||
            value >= 65 && value <= 90 ||
            value >= 97 && value <= 122 ||
            value == 45;
    }

    internal static bool IsIdnaSeparator(char value)
    {
        return value == '.' ||
            value == '\u3002' ||
            value == '\uff0e' ||
            value == '\uff61';
    }

    internal static DnsTextResult UnicodeSuccess(DnsName name, string text)
    {
        DnsTextResult result = new DnsTextResult();
        result.Success = true;
        result.Name = name;
        result.Text = text;
        return result;
    }
}
