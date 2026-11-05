using System.Collections.Generic;

namespace dns;

internal class WireWriteResult
{
    internal bool Success;
    internal int BytesWritten;
    internal int StartOffset;
    internal int NextOffset;
    internal int PatchOffset;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal class WireWriter
{
    byte[]? destination;
    int start;
    int offset;
    int end;

    internal WireWriter(byte[]? destination, int offset, int length)
    {
        if (destination == null)
        {
            if (offset != 0 || length < 0)
            {
                throw new System.ArgumentOutOfRangeException("offset");
            }

            this.destination = null;
            this.start = 0;
            this.offset = 0;
            this.end = length;
            return;
        }

        if (offset < 0 || length < 0 || offset > destination.Length - length)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        this.destination = destination;
        this.start = offset;
        this.offset = offset;
        this.end = offset + length;
    }

    internal bool IsCounting()
    {
        return this.destination == null;
    }

    internal int WriteOffset()
    {
        return this.offset;
    }

    internal int WrittenLength()
    {
        return this.offset - this.start;
    }

    internal int RemainingLength()
    {
        return this.end - this.offset;
    }

    internal byte[] CopyWrittenBytes()
    {
        if (this.destination == null)
        {
            return [];
        }

        byte[] bytes = new byte[this.offset - this.start];
        System.Array.Copy(this.destination, this.start, bytes, 0, bytes.Length);
        return bytes;
    }

    internal WireWriteResult WriteUnsigned8(ulong value)
    {
        return this.WriteFixedUnsigned(value, 1, 255, "unsigned8");
    }

    internal WireWriteResult WriteUnsigned16(ulong value)
    {
        return this.WriteFixedUnsigned(value, 2, 65535, "unsigned16");
    }

    internal WireWriteResult WriteUnsigned32(ulong value)
    {
        return this.WriteFixedUnsigned(value, 4, 4294967295, "unsigned32");
    }

    internal WireWriteResult WriteUnsigned48(ulong value)
    {
        return this.WriteFixedUnsigned(value, 6, 281474976710655, "unsigned48");
    }

    internal WireWriteResult WriteBytes(byte[] bytes)
    {
        return this.WriteByteArray(bytes, "bytes");
    }

    internal WireWriteResult WriteCharacterString(byte[] bytes)
    {
        int initialOffset = this.offset;
        if (bytes == null)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "characterString",
                "Character-string bytes are required.",
                this.offset);
        }

        if (bytes.Length > 255)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "characterString",
                "A character string cannot exceed 255 octets.",
                this.offset);
        }

        int required = bytes.Length + 1;
        if (!this.CanWrite(required))
        {
            return this.FailAt(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "characterString",
                "The character string exceeds the output region.",
                this.offset);
        }

        if (this.destination != null)
        {
            this.destination[this.offset] = (byte)bytes.Length;
            System.Array.Copy(bytes, 0, this.destination, this.offset + 1, bytes.Length);
        }

        this.offset = this.offset + required;
        return this.Succeed(initialOffset, required, -1);
    }

    internal WireWriteResult WriteIpv4Address(byte[] address)
    {
        if (address == null || address.Length != 4)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "ipv4Address",
                "An IPv4 address must contain exactly four octets.",
                this.offset);
        }

        return this.WriteByteArray(address, "ipv4Address");
    }

    internal WireWriteResult WriteIpv6Address(byte[] address)
    {
        if (address == null || address.Length != 16)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "ipv6Address",
                "An IPv6 address must contain exactly sixteen octets.",
                this.offset);
        }

        return this.WriteByteArray(address, "ipv6Address");
    }

    internal WireWriteResult WriteTypeBitmap(List<int> recordTypeValues)
    {
        int initialOffset = this.offset;
        if (recordTypeValues == null)
        {
            return this.FailAt(
                DnsDiagnosticCode.InvalidTypeBitmap,
                "typeBitmap",
                "Record type values are required.",
                this.offset);
        }

        List<int> sortedValues = new List<int>();
        int validationIndex = 0;
        while (validationIndex < recordTypeValues.Count)
        {
            int recordTypeValue = recordTypeValues[validationIndex];
            if (recordTypeValue < 1 || recordTypeValue > 65535)
            {
                return this.FailAt(
                    DnsDiagnosticCode.InvalidTypeBitmap,
                    "typeBitmap",
                    "A record type value must be between one and 65535.",
                    this.offset);
            }

            sortedValues.Add(recordTypeValue);
            validationIndex = validationIndex + 1;
        }

        sortedValues.Sort();
        List<byte> encoded = new List<byte>();
        int index = 0;
        while (index < sortedValues.Count)
        {
            int window = sortedValues[index] / 256;
            int windowEnd = index;
            int lastValue = sortedValues[index];
            while (windowEnd < sortedValues.Count && sortedValues[windowEnd] / 256 == window)
            {
                lastValue = sortedValues[windowEnd];
                windowEnd = windowEnd + 1;
            }

            int bitmapLength = ((lastValue % 256) / 8) + 1;
            encoded.Add((byte)window);
            encoded.Add((byte)bitmapLength);
            int bitmapStart = encoded.Count;
            int zeroIndex = 0;
            while (zeroIndex < bitmapLength)
            {
                encoded.Add(0);
                zeroIndex = zeroIndex + 1;
            }

            int valueIndex = index;
            while (valueIndex < windowEnd)
            {
                int lowValue = sortedValues[valueIndex] % 256;
                int byteIndex = lowValue / 8;
                int bitIndex = lowValue % 8;
                int mask = 128 >> bitIndex;
                encoded[bitmapStart + byteIndex] =
                    (byte)(encoded[bitmapStart + byteIndex] | mask);
                valueIndex = valueIndex + 1;
            }

            index = windowEnd;
        }

        byte[] bytes = encoded.ToArray();
        if (!this.CanWrite(bytes.Length))
        {
            return this.FailAt(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "typeBitmap",
                "The type bitmap exceeds the output region.",
                this.offset);
        }

        if (this.destination != null)
        {
            System.Array.Copy(bytes, 0, this.destination, this.offset, bytes.Length);
        }

        this.offset = this.offset + bytes.Length;
        return this.Succeed(initialOffset, bytes.Length, -1);
    }

    internal WireWriteResult ReserveSectionCount()
    {
        return this.ReserveUnsigned16Field("sectionCount");
    }

    internal WireWriteResult PatchSectionCount(int patchOffset, int count)
    {
        return this.PatchUnsigned16Value(patchOffset, count, "sectionCount");
    }

    internal WireWriteResult ReserveRecordDataLength()
    {
        return this.ReserveUnsigned16Field("recordDataLength");
    }

    internal WireWriteResult PatchRecordDataLength(
        int patchOffset,
        int recordDataStartOffset)
    {
        if (patchOffset < this.start || patchOffset > this.offset)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "recordDataLength",
                "The RDATA length patch offset is outside the written region.",
                patchOffset);
        }

        if (recordDataStartOffset < patchOffset ||
            recordDataStartOffset - patchOffset < 2 ||
            recordDataStartOffset > this.offset)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "recordDataLength",
                "The RDATA start must follow its reserved length and precede the current offset.",
                patchOffset);
        }

        int length = this.offset - recordDataStartOffset;
        return this.PatchUnsigned16Value(patchOffset, length, "recordDataLength");
    }

    internal bool CanWrite(int count)
    {
        return count >= 0 && count <= this.end - this.offset;
    }

    internal WireWriteResult WriteFixedUnsigned(
        ulong value,
        int width,
        ulong maximum,
        string field)
    {
        int initialOffset = this.offset;
        if (value > maximum)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                field,
                "The unsigned value exceeds the requested wire width.",
                this.offset);
        }

        if (!this.CanWrite(width))
        {
            return this.FailAt(
                DnsDiagnosticCode.ResourceLimitExceeded,
                field,
                "The unsigned value exceeds the output region.",
                this.offset);
        }

        int index = 0;
        while (index < width)
        {
            int shift = (width - index - 1) * 8;
            if (this.destination != null)
            {
                this.destination[this.offset + index] =
                    (byte)((value >> shift) & 255);
            }

            index = index + 1;
        }

        this.offset = this.offset + width;
        return this.Succeed(initialOffset, width, -1);
    }

    internal WireWriteResult WriteByteArray(byte[] bytes, string field)
    {
        int initialOffset = this.offset;
        if (bytes == null)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                field,
                "Byte data is required.",
                this.offset);
        }

        if (!this.CanWrite(bytes.Length))
        {
            return this.FailAt(
                DnsDiagnosticCode.ResourceLimitExceeded,
                field,
                "The byte data exceeds the output region.",
                this.offset);
        }

        if (this.destination != null)
        {
            System.Array.Copy(bytes, 0, this.destination, this.offset, bytes.Length);
        }

        this.offset = this.offset + bytes.Length;
        return this.Succeed(initialOffset, bytes.Length, -1);
    }

    internal WireWriteResult ReserveUnsigned16Field(string field)
    {
        int patchOffset = this.offset;
        if (!this.CanWrite(2))
        {
            return this.FailAt(
                DnsDiagnosticCode.ResourceLimitExceeded,
                field,
                "A two-octet patch field exceeds the output region.",
                this.offset);
        }

        if (this.destination != null)
        {
            this.destination[this.offset] = 0;
            this.destination[this.offset + 1] = 0;
        }

        this.offset = this.offset + 2;
        return this.Succeed(patchOffset, 2, patchOffset);
    }

    internal WireWriteResult PatchUnsigned16Value(
        int patchOffset,
        int value,
        string field)
    {
        if (value < 0 || value > 65535)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                field,
                "A patched value must fit in sixteen bits.",
                patchOffset);
        }

        if (patchOffset < this.start || this.offset - patchOffset < 2)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                field,
                "The patch offset does not identify two previously written octets.",
                patchOffset);
        }

        if (this.destination != null)
        {
            this.destination[patchOffset] = (byte)(value >> 8);
            this.destination[patchOffset + 1] = (byte)(value & 255);
        }

        return this.Succeed(this.offset, 0, patchOffset);
    }

    internal WireWriteResult Succeed(
        int initialOffset,
        int bytesWritten,
        int patchOffset)
    {
        WireWriteResult result = new WireWriteResult();
        result.Success = true;
        result.BytesWritten = bytesWritten;
        result.StartOffset = initialOffset;
        result.NextOffset = this.offset;
        result.PatchOffset = patchOffset;
        return result;
    }

    internal WireWriteResult FailAt(
        DnsDiagnosticCode code,
        string field,
        string reason,
        int failureOffset)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.HasOffset = true;
        diagnostic.Offset = failureOffset;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        WireWriteResult result = new WireWriteResult();
        result.StartOffset = this.offset;
        result.NextOffset = this.offset;
        result.PatchOffset = -1;
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
