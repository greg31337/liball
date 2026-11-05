using System.Collections.Generic;

namespace dns;

internal class CompressionEntry
{
    internal int Offset;
    internal List<byte[]> Labels = new List<byte[]>();
}

internal class CompressionMatch
{
    internal int LabelIndex = -1;
    internal CompressionEntry? Entry;
}

internal class CompressionTable
{
    int messageStartOffset;
    List<CompressionEntry> entries = new List<CompressionEntry>();

    internal CompressionTable(int messageStartOffset)
    {
        if (messageStartOffset < 0)
        {
            throw new System.ArgumentOutOfRangeException("messageStartOffset");
        }

        this.messageStartOffset = messageStartOffset;
    }

    internal WireWriteResult WriteName(
        WireWriter writer,
        DnsName name,
        DnsEncodeOptions options,
        bool compressionPermitted)
    {
        if (writer == null)
        {
            return FailureAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "writer",
                "A wire writer is required.",
                this.messageStartOffset);
        }

        int writeOffset = writer.WriteOffset();
        if (options == null)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS encode options are required.",
                writeOffset);
        }

        if (options.MaximumMessageLength < 0)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.MaximumMessageLength",
                "The maximum message length cannot be negative.",
                writeOffset);
        }

        if (writeOffset < this.messageStartOffset)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "writer",
                "The writer offset precedes the DNS message start.",
                writeOffset);
        }

        RawDnsNameResult rawResult = RawDnsName.ValidateName(name);
        if (!rawResult.Success || rawResult.Name == null)
        {
            return FromRawFailure(writer, rawResult);
        }

        DnsName validatedName = rawResult.Name;
        if (!validatedName.IsAbsolute)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Only absolute DNS names can be written to wire format.",
                writeOffset);
        }

        if (options.Canonical && options.CanonicalOrder == null)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "options.CanonicalOrder",
                "Canonical ordering options are required in canonical mode.",
                writeOffset);
        }

        bool lowercase = options.Canonical &&
            options.CanonicalOrder != null &&
            options.CanonicalOrder.LowercaseNames;
        bool compressionEnabled = options.EnableCompression && compressionPermitted;
        if (options.Canonical &&
            options.CanonicalOrder != null &&
            options.CanonicalOrder.DisableCompression)
        {
            compressionEnabled = false;
        }

        int relativeWriteOffset = writeOffset - this.messageStartOffset;
        List<byte[]> encodedLabels = CopyLabels(validatedName.Labels, lowercase);
        CompressionMatch match = new CompressionMatch();
        if (compressionEnabled)
        {
            match = this.FindLongestSuffix(
                encodedLabels,
                lowercase,
                relativeWriteOffset);
        }

        int literalLabelCount = encodedLabels.Count;
        CompressionEntry? matchedEntry = match.Entry;
        if (matchedEntry != null)
        {
            literalLabelCount = match.LabelIndex;
        }

        List<byte> encoded = new List<byte>();
        int labelIndex = 0;
        while (labelIndex < literalLabelCount)
        {
            byte[] label = encodedLabels[labelIndex];
            encoded.Add((byte)label.Length);
            int byteIndex = 0;
            while (byteIndex < label.Length)
            {
                encoded.Add(label[byteIndex]);
                byteIndex = byteIndex + 1;
            }

            labelIndex = labelIndex + 1;
        }

        if (matchedEntry == null)
        {
            encoded.Add(0);
        }
        else
        {
            encoded.Add((byte)(192 | (matchedEntry.Offset >> 8)));
            encoded.Add((byte)(matchedEntry.Offset & 255));
        }

        if (relativeWriteOffset > options.MaximumMessageLength - encoded.Count)
        {
            return writer.FailAt(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "name",
                "The encoded DNS name exceeds the configured message length.",
                writeOffset);
        }

        WireWriteResult writeResult = writer.WriteBytes(encoded.ToArray());
        if (!writeResult.Success)
        {
            return writeResult;
        }

        List<byte[]> wireLabels = BuildWireLabels(
            encodedLabels,
            literalLabelCount,
            matchedEntry);
        this.RegisterLiteralSuffixes(
            wireLabels,
            literalLabelCount,
            writeOffset);
        return writeResult;
    }

    internal CompressionMatch FindLongestSuffix(
        List<byte[]> labels,
        bool exactCase,
        int currentOffset)
    {
        CompressionMatch result = new CompressionMatch();
        int suffixIndex = 0;
        while (suffixIndex < labels.Count)
        {
            int entryIndex = 0;
            while (entryIndex < this.entries.Count)
            {
                CompressionEntry entry = this.entries[entryIndex];
                if (entry.Offset < currentOffset &&
                    SuffixEqual(labels, suffixIndex, entry.Labels, exactCase))
                {
                    result.LabelIndex = suffixIndex;
                    result.Entry = entry;
                    return result;
                }

                entryIndex = entryIndex + 1;
            }

            suffixIndex = suffixIndex + 1;
        }

        return result;
    }

    internal void RegisterLiteralSuffixes(
        List<byte[]> wireLabels,
        int literalLabelCount,
        int absoluteWriteOffset)
    {
        int labelOffset = absoluteWriteOffset;
        int labelIndex = 0;
        while (labelIndex < literalLabelCount)
        {
            int relativeOffset = labelOffset - this.messageStartOffset;
            if (relativeOffset <= DnsProtocolLimits.MaximumCompressionPointerOffset &&
                !this.ContainsExactSuffix(wireLabels, labelIndex))
            {
                CompressionEntry entry = new CompressionEntry();
                entry.Offset = relativeOffset;
                int copyIndex = labelIndex;
                while (copyIndex < wireLabels.Count)
                {
                    byte[] sourceLabel = wireLabels[copyIndex];
                    byte[] labelCopy = new byte[sourceLabel.Length];
                    System.Array.Copy(
                        sourceLabel,
                        0,
                        labelCopy,
                        0,
                        sourceLabel.Length);
                    entry.Labels.Add(labelCopy);
                    copyIndex = copyIndex + 1;
                }

                this.entries.Add(entry);
            }

            labelOffset = labelOffset + wireLabels[labelIndex].Length + 1;
            labelIndex = labelIndex + 1;
        }
    }

    internal bool ContainsExactSuffix(List<byte[]> labels, int suffixIndex)
    {
        int entryIndex = 0;
        while (entryIndex < this.entries.Count)
        {
            if (SuffixEqual(labels, suffixIndex, this.entries[entryIndex].Labels, true))
            {
                return true;
            }

            entryIndex = entryIndex + 1;
        }

        return false;
    }

    internal static List<byte[]> CopyLabels(List<byte[]> labels, bool lowercase)
    {
        List<byte[]> copies = new List<byte[]>();
        int labelIndex = 0;
        while (labelIndex < labels.Count)
        {
            byte[] sourceLabel = labels[labelIndex];
            byte[] labelCopy = new byte[sourceLabel.Length];
            int byteIndex = 0;
            while (byteIndex < sourceLabel.Length)
            {
                byte value = sourceLabel[byteIndex];
                if (lowercase && value >= 65 && value <= 90)
                {
                    value = (byte)(value + 32);
                }

                labelCopy[byteIndex] = value;
                byteIndex = byteIndex + 1;
            }

            copies.Add(labelCopy);
            labelIndex = labelIndex + 1;
        }

        return copies;
    }

    internal static List<byte[]> BuildWireLabels(
        List<byte[]> encodedLabels,
        int literalLabelCount,
        CompressionEntry? matchedEntry)
    {
        List<byte[]> labels = new List<byte[]>();
        int literalIndex = 0;
        while (literalIndex < literalLabelCount)
        {
            labels.Add(encodedLabels[literalIndex]);
            literalIndex = literalIndex + 1;
        }

        if (matchedEntry != null)
        {
            int suffixIndex = 0;
            while (suffixIndex < matchedEntry.Labels.Count)
            {
                labels.Add(matchedEntry.Labels[suffixIndex]);
                suffixIndex = suffixIndex + 1;
            }
        }

        return labels;
    }

    internal static bool SuffixEqual(
        List<byte[]> labels,
        int suffixIndex,
        List<byte[]> suffix,
        bool exactCase)
    {
        if (labels.Count - suffixIndex != suffix.Count)
        {
            return false;
        }

        int index = 0;
        while (index < suffix.Count)
        {
            byte[] left = labels[suffixIndex + index];
            byte[] right = suffix[index];
            bool equal;
            if (exactCase)
            {
                equal = ExactLabelEqual(left, right);
            }
            else
            {
                equal = DnsNameOperations.LabelsEqual(left, right);
            }

            if (!equal)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ExactLabelEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static WireWriteResult FromRawFailure(
        WireWriter writer,
        RawDnsNameResult rawResult)
    {
        if (rawResult.Diagnostics.Count == 0)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidLabel,
                "name",
                "Validated name data is unavailable.",
                writer.WriteOffset());
        }

        DnsDiagnostic diagnostic = rawResult.Diagnostics[0];
        return writer.FailAt(
            diagnostic.Code,
            diagnostic.Field == null ? "name" : diagnostic.Field,
            diagnostic.Reason,
            writer.WriteOffset());
    }

    internal static WireWriteResult FailureAt(
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
        WireWriteResult result = new WireWriteResult();
        result.StartOffset = offset;
        result.NextOffset = offset;
        result.PatchOffset = -1;
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
