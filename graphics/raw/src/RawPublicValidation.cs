namespace raw;

internal static class RawPublicValidation
{
    internal static RawValidationReport FromReport(RawValidationReport report, RawValidationOptions options, RawValidationCategory category)
    {
        RequireOptions(options);
        RawValidationReport optionReport = options.Validate();
        if (!optionReport.IsValid())
        {
            return optionReport;
        }
        options.CheckCancellation();
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (RawCodec.CategorySelected(options, category) && report != null && report.Messages != null)
        {
            int index = 0;
            while (index < report.Messages.Length)
            {
                RawValidationMessage message = report.Messages[index];
                if (message != null)
                {
                    messages.Add(new RawValidationMessage(category, message.Code, message.Severity, message.Format, message.Offset, message.Context, message.Message, message.Recovered));
                }
                index++;
            }
        }
        return RawCodec.CreateValidationReport(options, messages);
    }

    internal static RawValidationReport Missing(RawValidationOptions options, RawValidationCategory category, string context, string message)
    {
        RequireOptions(options);
        RawValidationReport optionReport = options.Validate();
        if (!optionReport.IsValid())
        {
            return optionReport;
        }
        options.CheckCancellation();
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (RawCodec.CategorySelected(options, category))
        {
            messages.Add(new RawValidationMessage(category, RawDiagnosticCode.InvalidArgument, RawSeverity.Error, RawFormat.Unknown, -1, context, message, false));
        }
        return RawCodec.CreateValidationReport(options, messages);
    }

    internal static RawValidationReport ValidateXmpPacket(RawXmpPacket packet, RawLimits limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (packet.RawBytes == null || packet.XmlText == null || packet.PrefixBytes == null || packet.XmlBytes == null || packet.PaddingBytes == null || packet.SuffixBytes == null || packet.IfdPath == null || packet.Fields == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Xmp.Collections", "An XMP packet requires retained bytes, decoded XML, wrapper parts, a path, and extracted fields.");
            return new RawValidationReport(true, messages.ToArray());
        }
        if (packet.Encoding < RawXmpEncoding.Utf8 || packet.Encoding > RawXmpEncoding.Utf32LittleEndian || packet.Offset < -1 || packet.XmlText.Length == 0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Xmp.Identity", "The XMP encoding, offset, or decoded XML identity is invalid.");
        }
        long partsLength = (long)packet.PrefixBytes.Length + packet.XmlBytes.Length + packet.PaddingBytes.Length + packet.SuffixBytes.Length;
        if (partsLength != packet.RawBytes.Length || !XmpPartsMatch(packet))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Xmp.Bytes", "The retained XMP packet does not equal its wrapper, XML, padding, and suffix parts.");
        }
        if (packet.RawBytes.Length > limits.MaxXmpBytes || packet.RawBytes.Length > limits.MaxMetadataBytes || packet.RawBytes.Length > limits.MaxAllocationBytes || packet.Fields.Length > limits.MaxTagCount)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.ResourceLimitExceeded, "Xmp.Limits", "The retained XMP packet or extracted field count exceeds validation limits.");
        }
        int index = 0;
        while (index < packet.Fields.Length)
        {
            RawXmpField field = packet.Fields[index];
            if (field == null || field.NamespaceUri == null || field.NamespaceUri.Length == 0 || field.Prefix == null || field.Name == null || field.Name.Length == 0 || field.Value == null || field.Language == null || field.Path == null || field.Path.Length == 0)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Xmp.Fields", "Every extracted XMP field requires a namespace, name, value, language, prefix, and stable path.");
                break;
            }
            index++;
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal static RawValidationReport ValidateIptcRecord(RawIptcRecord record, RawLimits limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (record.RawBytes == null || record.PaddingBytes == null || record.Datasets == null || record.IfdPath == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Iptc.Collections", "An IPTC record requires retained bytes, padding, ordered DataSets, and a stable path.");
            return new RawValidationReport(true, messages.ToArray());
        }
        if (record.Offset < -1 || record.RawBytes.Length > limits.MaxMetadataBytes || record.RawBytes.Length > limits.MaxAllocationBytes || record.Datasets.Length > limits.MaxTagCount)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.ResourceLimitExceeded, "Iptc.Limits", "The IPTC offset, retained bytes, or DataSet count exceeds validation limits.");
        }
        if (record.PaddingBytes.Length > record.RawBytes.Length || !IptcPaddingMatches(record))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Iptc.Padding", "The retained IPTC padding is not an exact zero-filled suffix of the original payload.");
        }
        bool declaresUtf8 = false;
        int index = 0;
        while (index < record.Datasets.Length)
        {
            RawIptcDataset dataset = record.Datasets[index];
            if (dataset == null || dataset.RecordNumber < 1 || dataset.RecordNumber > 9 || dataset.Name == null || dataset.Name.Length == 0 || dataset.RawBytes == null || dataset.Text == null || dataset.ValueKind < RawIptcValueKind.Binary || dataset.ValueKind > RawIptcValueKind.Time || dataset.RawBytes.Length > limits.MaxTagValueBytes || !dataset.Valid)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Iptc.Datasets", "Every IPTC DataSet requires a valid record, name, value form, typed interpretation, and bounded exact bytes.");
                break;
            }
            if (dataset.RecordNumber == 1 && dataset.DatasetNumber == 90 && IptcCodec.IsUtf8Declaration(dataset.RawBytes))
            {
                declaresUtf8 = true;
            }
            index++;
        }
        if (record.Utf8 != declaresUtf8)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "Iptc.Encoding", "The IPTC UTF-8 state does not match its coded-character-set declaration.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal static bool XmpPartsMatch(RawXmpPacket packet)
    {
        int position = 0;
        if (!PartMatches(packet.RawBytes ?? System.Array.Empty<byte>(), position, packet.PrefixBytes ?? System.Array.Empty<byte>())) return false;
        position += (packet.PrefixBytes ?? System.Array.Empty<byte>()).Length;
        if (!PartMatches(packet.RawBytes ?? System.Array.Empty<byte>(), position, packet.XmlBytes ?? System.Array.Empty<byte>())) return false;
        position += (packet.XmlBytes ?? System.Array.Empty<byte>()).Length;
        if (!PartMatches(packet.RawBytes ?? System.Array.Empty<byte>(), position, packet.PaddingBytes ?? System.Array.Empty<byte>())) return false;
        position += (packet.PaddingBytes ?? System.Array.Empty<byte>()).Length;
        return PartMatches(packet.RawBytes ?? System.Array.Empty<byte>(), position, packet.SuffixBytes ?? System.Array.Empty<byte>());
    }

    internal static bool PartMatches(byte[] source, int offset, byte[] part)
    {
        if (offset < 0 || offset > source.Length || part.Length > source.Length - offset)
        {
            return false;
        }
        int index = 0;
        while (index < part.Length)
        {
            if (source[offset + index] != part[index]) return false;
            index++;
        }
        return true;
    }

    internal static bool IptcPaddingMatches(RawIptcRecord record)
    {
        byte[] raw = record.RawBytes ?? System.Array.Empty<byte>();
        byte[] padding = record.PaddingBytes ?? System.Array.Empty<byte>();
        int start = raw.Length - padding.Length;
        int index = 0;
        while (index < padding.Length)
        {
            if (padding[index] != 0 || raw[start + index] != padding[index]) return false;
            index++;
        }
        return true;
    }

    internal static void RequireOptions(RawValidationOptions options)
    {
        if (options == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Raw validation options are required.", -1);
        }
    }
}
