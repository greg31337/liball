namespace dns;

internal static class DnsNameServiceOperations
{
    internal static DnsName RequireRawName(
        RawDnsNameResult result,
        string fallback)
    {
        if (!result.Success || result.Name == null)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics, fallback));
        }

        return result.Name;
    }

    internal static DnsName RequireTextName(
        DnsTextResult result,
        string fallback)
    {
        if (!result.Success || result.Name == null)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics, fallback));
        }

        return result.Name;
    }

    internal static string RequireTextValue(
        DnsTextResult result,
        string fallback)
    {
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics, fallback));
        }

        return result.Text;
    }

    internal static DnsName RequireOperationName(
        DnsNameOperationResult result,
        string fallback)
    {
        if (!result.Success || result.Name == null)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics, fallback));
        }

        return result.Name;
    }

    internal static bool RequireOperationBoolean(
        DnsNameOperationResult result,
        string fallback)
    {
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics, fallback));
        }

        return result.BooleanValue;
    }

    internal static int RequireOperationComparison(
        DnsNameOperationResult result,
        string fallback)
    {
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics, fallback));
        }

        return result.Comparison;
    }

    internal static int RequireOperationLabelCount(
        DnsNameOperationResult result,
        string fallback)
    {
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics, fallback));
        }

        return result.LabelCount;
    }

    internal static DnsName RequireReverseName(
        ReverseDnsNameResult result,
        string fallback)
    {
        if (!result.Success || result.Name == null)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics, fallback));
        }

        return result.Name;
    }

    internal static byte[] RequireReverseAddress(
        ReverseDnsNameResult result,
        string fallback)
    {
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics, fallback));
        }

        return result.Address;
    }
}
