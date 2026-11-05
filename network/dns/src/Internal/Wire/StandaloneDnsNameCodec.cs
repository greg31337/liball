namespace dns;

internal static class StandaloneDnsNameCodec
{
    internal static DnsEncodeResult EncodeName(
        DnsName name,
        DnsEncodeOptions options)
    {
        if (options == null)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS encode options are required.",
                0);
        }

        if (options.MaximumMessageLength < 0 ||
            options.MaximumMessageLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            return DnsHeaderCodec.EncodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum name length must fit the DNS message size.",
                0);
        }

        byte[] destination = new byte[options.MaximumMessageLength];
        WireWriter writer = new WireWriter(
            destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        WireWriteResult write = table.WriteName(
            writer, name, options, false);
        if (!write.Success)
        {
            DnsEncodeResult failure = new DnsEncodeResult();
            int diagnosticIndex = 0;
            while (diagnosticIndex < write.Diagnostics.Count)
            {
                failure.Diagnostics.Add(write.Diagnostics[diagnosticIndex]);
                diagnosticIndex = diagnosticIndex + 1;
            }

            return failure;
        }

        DnsEncodeResult result = new DnsEncodeResult();
        result.Success = true;
        result.Bytes = writer.CopyWrittenBytes();
        result.BytesWritten = writer.WrittenLength();
        return result;
    }
}
