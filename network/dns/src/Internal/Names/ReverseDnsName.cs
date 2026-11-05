using System.Collections.Generic;

namespace dns;

internal class ReverseDnsNameResult
{
    internal bool Success;
    internal DnsName? Name;
    internal byte[] Address = System.Array.Empty<byte>();
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class ReverseDnsName
{
    internal static ReverseDnsNameResult BuildIpv4ReverseName(byte[] address)
    {
        if (address == null || address.Length != 4)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "address",
                "An IPv4 address must contain exactly four octets.");
        }

        List<byte[]> labels = new List<byte[]>();
        int index = address.Length - 1;
        while (index >= 0)
        {
            labels.Add(CreateDecimalLabel(address[index]));
            index = index - 1;
        }

        labels.Add([105, 110, 45, 97, 100, 100, 114]);
        labels.Add([97, 114, 112, 97]);
        RawDnsNameResult rawResult = RawDnsName.CreateAbsoluteName(labels);
        return FromRawName(rawResult);
    }

    internal static ReverseDnsNameResult BuildIpv6ReverseName(byte[] address)
    {
        if (address == null || address.Length != 16)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "address",
                "An IPv6 address must contain exactly sixteen octets.");
        }

        List<byte[]> labels = new List<byte[]>();
        int index = address.Length - 1;
        while (index >= 0)
        {
            int value = address[index];
            labels.Add([CreateHexDigit(value & 15)]);
            labels.Add([CreateHexDigit((value >> 4) & 15)]);
            index = index - 1;
        }

        labels.Add([105, 112, 54]);
        labels.Add([97, 114, 112, 97]);
        RawDnsNameResult rawResult = RawDnsName.CreateAbsoluteName(labels);
        return FromRawName(rawResult);
    }

    internal static ReverseDnsNameResult ParseIpv4ReverseAddress(DnsName name)
    {
        RawDnsNameResult rawResult = RawDnsName.ValidateName(name);
        if (!rawResult.Success)
        {
            return FromRawFailure(rawResult);
        }

        DnsName? optionalName = rawResult.Name;
        if (optionalName == null)
        {
            return Failure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        DnsName validatedName = optionalName;
        if (!validatedName.IsAbsolute || validatedName.Labels.Count != 6)
        {
            return Failure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "An IPv4 reverse name must have four address labels and an absolute in-addr.arpa suffix.");
        }

        if (!DnsNameOperations.LabelsEqual(
                validatedName.Labels[4],
                [105, 110, 45, 97, 100, 100, 114]) ||
            !DnsNameOperations.LabelsEqual(
                validatedName.Labels[5],
                [97, 114, 112, 97]))
        {
            return Failure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "An IPv4 reverse name must end with in-addr.arpa.");
        }

        byte[] address = new byte[4];
        int labelIndex = 0;
        while (labelIndex < 4)
        {
            int value = ParseDecimalLabel(validatedName.Labels[labelIndex]);
            if (value == -2)
            {
                return Failure(
                    DnsDiagnosticCode.ValueOutOfRange,
                    $"labels[{labelIndex}]",
                    "An IPv4 reverse label must represent an octet from 0 through 255.");
            }

            if (value < 0)
            {
                return Failure(
                    DnsDiagnosticCode.InvalidLabel,
                    $"labels[{labelIndex}]",
                    "An IPv4 reverse label must be a canonical decimal octet from 0 through 255.");
            }

            address[3 - labelIndex] = (byte)value;
            labelIndex = labelIndex + 1;
        }

        return AddressSuccess(address);
    }

    internal static ReverseDnsNameResult ParseIpv6ReverseAddress(DnsName name)
    {
        RawDnsNameResult rawResult = RawDnsName.ValidateName(name);
        if (!rawResult.Success)
        {
            return FromRawFailure(rawResult);
        }

        DnsName? optionalName = rawResult.Name;
        if (optionalName == null)
        {
            return Failure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.");
        }

        DnsName validatedName = optionalName;
        if (!validatedName.IsAbsolute || validatedName.Labels.Count != 34)
        {
            return Failure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "An IPv6 reverse name must have 32 nibble labels and an absolute ip6.arpa suffix.");
        }

        if (!DnsNameOperations.LabelsEqual(validatedName.Labels[32], [105, 112, 54]) ||
            !DnsNameOperations.LabelsEqual(
                validatedName.Labels[33],
                [97, 114, 112, 97]))
        {
            return Failure(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "An IPv6 reverse name must end with ip6.arpa.");
        }

        byte[] address = new byte[16];
        int addressIndex = 15;
        int labelIndex = 0;
        while (addressIndex >= 0)
        {
            int lowNibble = ParseHexLabel(validatedName.Labels[labelIndex]);
            int highNibble = ParseHexLabel(validatedName.Labels[labelIndex + 1]);
            if (lowNibble < 0)
            {
                return Failure(
                    DnsDiagnosticCode.InvalidLabel,
                    $"labels[{labelIndex}]",
                    "Each IPv6 reverse label must be one hexadecimal nibble.");
            }

            if (highNibble < 0)
            {
                return Failure(
                    DnsDiagnosticCode.InvalidLabel,
                    $"labels[{labelIndex + 1}]",
                    "Each IPv6 reverse label must be one hexadecimal nibble.");
            }

            address[addressIndex] = (byte)((highNibble * 16) + lowNibble);
            addressIndex = addressIndex - 1;
            labelIndex = labelIndex + 2;
        }

        return AddressSuccess(address);
    }

    internal static byte[] CreateDecimalLabel(byte value)
    {
        if (value >= 100)
        {
            return
            [
                (byte)(48 + (value / 100)),
                (byte)(48 + ((value / 10) % 10)),
                (byte)(48 + (value % 10))
            ];
        }

        if (value >= 10)
        {
            return
            [
                (byte)(48 + (value / 10)),
                (byte)(48 + (value % 10))
            ];
        }

        return [(byte)(48 + value)];
    }

    internal static int ParseDecimalLabel(byte[] label)
    {
        if (label.Length < 1 || label.Length > 3)
        {
            return -1;
        }

        if (label.Length > 1 && label[0] == 48)
        {
            return -1;
        }

        int value = 0;
        int index = 0;
        while (index < label.Length)
        {
            byte character = label[index];
            if (character < 48 || character > 57)
            {
                return -1;
            }

            value = (value * 10) + character - 48;
            index = index + 1;
        }

        if (value > 255)
        {
            return -2;
        }

        return value;
    }

    internal static byte CreateHexDigit(int value)
    {
        if (value < 10)
        {
            return (byte)(48 + value);
        }

        return (byte)(87 + value);
    }

    internal static int ParseHexLabel(byte[] label)
    {
        if (label.Length != 1)
        {
            return -1;
        }

        byte character = label[0];
        if (character >= 48 && character <= 57)
        {
            return character - 48;
        }

        if (character >= 65 && character <= 70)
        {
            return character - 55;
        }

        if (character >= 97 && character <= 102)
        {
            return character - 87;
        }

        return -1;
    }

    internal static ReverseDnsNameResult FromRawName(RawDnsNameResult rawResult)
    {
        if (!rawResult.Success)
        {
            return FromRawFailure(rawResult);
        }

        ReverseDnsNameResult result = new ReverseDnsNameResult();
        result.Success = true;
        result.Name = rawResult.Name;
        return result;
    }

    internal static ReverseDnsNameResult FromRawFailure(RawDnsNameResult rawResult)
    {
        ReverseDnsNameResult result = new ReverseDnsNameResult();
        int index = 0;
        while (index < rawResult.Diagnostics.Count)
        {
            result.Diagnostics.Add(rawResult.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    internal static ReverseDnsNameResult AddressSuccess(byte[] address)
    {
        ReverseDnsNameResult result = new ReverseDnsNameResult();
        result.Success = true;
        result.Address = address;
        return result;
    }

    internal static ReverseDnsNameResult Failure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        ReverseDnsNameResult result = new ReverseDnsNameResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
