using System.Collections.Generic;
using System.Text;

namespace dns;

internal static class PresentationDnsName
{
    internal static DnsTextResult ParseName(string text, DnsName? origin)
    {
        if (text == null)
        {
            return FailText(
                DnsDiagnosticCode.InvalidText,
                "name",
                "Presentation name text is required.",
                0);
        }

        if (text == ".")
        {
            RawDnsNameResult root = RawDnsName.CreateRootName();
            return FromRawName(root, text);
        }

        List<byte[]> labels = new List<byte[]>();
        List<byte> currentLabel = new List<byte>();
        bool isAbsolute = false;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '.')
            {
                if (currentLabel.Count == 0)
                {
                    return FailText(
                        DnsDiagnosticCode.InvalidLabel,
                        "name",
                        "A presentation name contains an empty label.",
                        index);
                }

                labels.Add(currentLabel.ToArray());
                currentLabel = new List<byte>();
                index = index + 1;
                if (index == text.Length)
                {
                    isAbsolute = true;
                }

                continue;
            }

            if (character == '\\')
            {
                if (index + 1 >= text.Length)
                {
                    return FailText(
                        DnsDiagnosticCode.InvalidText,
                        "name",
                        "A presentation escape requires a following character.",
                        index);
                }

                char escaped = text[index + 1];
                if (IsAsciiDigit(escaped))
                {
                    if (text.Length - index < 4)
                    {
                        return FailText(
                            DnsDiagnosticCode.InvalidText,
                            "name",
                            "A decimal escape requires exactly three digits.",
                            index);
                    }

                    char secondDigit = text[index + 2];
                    char thirdDigit = text[index + 3];
                    if (!IsAsciiDigit(secondDigit) || !IsAsciiDigit(thirdDigit))
                    {
                        return FailText(
                            DnsDiagnosticCode.InvalidText,
                            "name",
                            "A decimal escape requires exactly three digits.",
                            index);
                    }

                    int decimalValue = ((escaped - '0') * 100) +
                        ((secondDigit - '0') * 10) +
                        (thirdDigit - '0');
                    if (decimalValue > 255)
                    {
                        return FailText(
                            DnsDiagnosticCode.ValueOutOfRange,
                            "name",
                            "A decimal escape must be between 000 and 255.",
                            index);
                    }

                    if (currentLabel.Count >= DnsProtocolLimits.MaximumLabelLength)
                    {
                        return FailText(
                            DnsDiagnosticCode.InvalidLabel,
                            "name",
                            "A decoded DNS label exceeds 63 octets.",
                            index);
                    }

                    currentLabel.Add((byte)decimalValue);
                    index = index + 4;
                    continue;
                }

                if (escaped < 33 || escaped > 126)
                {
                    return FailText(
                        DnsDiagnosticCode.InvalidText,
                        "name",
                        "A non-ASCII or control character must use a decimal escape.",
                        index);
                }

                if (currentLabel.Count >= DnsProtocolLimits.MaximumLabelLength)
                {
                    return FailText(
                        DnsDiagnosticCode.InvalidLabel,
                        "name",
                        "A decoded DNS label exceeds 63 octets.",
                        index);
                }

                currentLabel.Add((byte)escaped);
                index = index + 2;
                continue;
            }

            if (character < 33 || character > 126)
            {
                return FailText(
                    DnsDiagnosticCode.InvalidText,
                    "name",
                    "A non-ASCII or control character must use a decimal escape.",
                    index);
            }

            if (IsPresentationSpecial(character))
            {
                return FailText(
                    DnsDiagnosticCode.InvalidText,
                    "name",
                    "A DNS presentation metacharacter must be escaped.",
                    index);
            }

            if (currentLabel.Count >= DnsProtocolLimits.MaximumLabelLength)
            {
                return FailText(
                    DnsDiagnosticCode.InvalidLabel,
                    "name",
                    "A decoded DNS label exceeds 63 octets.",
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
            RawDnsNameResult validatedOrigin = RawDnsName.ValidateName(origin);
            if (!validatedOrigin.Success)
            {
                return FromRawName(validatedOrigin, text);
            }

            if (!origin.IsAbsolute)
            {
                return FailText(
                    DnsDiagnosticCode.InvalidText,
                    "origin",
                    "A presentation-name origin must be absolute.",
                    0);
            }

            int originIndex = 0;
            while (originIndex < origin.Labels.Count)
            {
                labels.Add(origin.Labels[originIndex]);
                originIndex = originIndex + 1;
            }

            isAbsolute = true;
        }

        RawDnsNameResult rawResult;
        if (isAbsolute)
        {
            rawResult = RawDnsName.CreateAbsoluteName(labels);
        }
        else
        {
            rawResult = RawDnsName.CreateRelativeName(labels);
        }

        return FromRawName(rawResult, text);
    }

    internal static DnsTextResult FormatName(DnsName name)
    {
        RawDnsNameResult rawResult = RawDnsName.ValidateName(name);
        if (!rawResult.Success)
        {
            return FromRawName(rawResult, string.Empty);
        }

        DnsName? optionalName = rawResult.Name;
        if (optionalName == null)
        {
            return FailText(
                DnsDiagnosticCode.InvalidText,
                "name",
                "Validated name data is unavailable.",
                0);
        }

        DnsName validatedName = optionalName;
        if (validatedName.IsAbsolute && validatedName.Labels.Count == 0)
        {
            DnsTextResult rootResult = new DnsTextResult();
            rootResult.Success = true;
            rootResult.Name = validatedName;
            rootResult.Text = ".";
            return rootResult;
        }

        StringBuilder builder = new StringBuilder();
        int labelIndex = 0;
        while (labelIndex < validatedName.Labels.Count)
        {
            if (labelIndex > 0)
            {
                builder.Append('.');
            }

            byte[] label = validatedName.Labels[labelIndex];
            int byteIndex = 0;
            while (byteIndex < label.Length)
            {
                AppendEscapedByte(builder, label[byteIndex]);
                byteIndex = byteIndex + 1;
            }

            labelIndex = labelIndex + 1;
        }

        if (validatedName.IsAbsolute)
        {
            builder.Append('.');
        }

        DnsTextResult result = new DnsTextResult();
        result.Success = true;
        result.Name = validatedName;
        result.Text = builder.ToString();
        return result;
    }

    internal static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsPresentationSpecial(char value)
    {
        return value == '"' ||
            value == '$' ||
            value == '(' ||
            value == ')' ||
            value == '.' ||
            value == ';' ||
            value == '@' ||
            value == '\\';
    }

    internal static void AppendEscapedByte(StringBuilder builder, byte value)
    {
        if (value < 33 || value > 126)
        {
            builder.Append('\\');
            builder.Append((char)('0' + (value / 100)));
            builder.Append((char)('0' + ((value / 10) % 10)));
            builder.Append((char)('0' + (value % 10)));
            return;
        }

        char character = (char)value;
        if (IsPresentationSpecial(character))
        {
            builder.Append('\\');
        }

        builder.Append(character);
    }

    internal static DnsTextResult FromRawName(
        RawDnsNameResult rawResult,
        string text)
    {
        DnsTextResult result = new DnsTextResult();
        result.Success = rawResult.Success;
        result.Name = rawResult.Name;
        result.Text = text;
        int index = 0;
        while (index < rawResult.Diagnostics.Count)
        {
            result.Diagnostics.Add(rawResult.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    internal static DnsTextResult FailText(
        DnsDiagnosticCode code,
        string field,
        string reason,
        int offset)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.HasOffset = true;
        diagnostic.Offset = offset;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        DnsTextResult result = new DnsTextResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
