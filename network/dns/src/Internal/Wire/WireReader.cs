using System.Collections.Generic;

namespace dns;

internal class WireReadResult
{
    internal bool Success;
    internal ulong UnsignedValue;
    internal byte[] Bytes = [];
    internal List<int> IntegerValues = new List<int>();
    internal WireReader? Subregion;
    internal int StartOffset;
    internal int NextOffset;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal class WireReader
{
    byte[] source;
    int start;
    int offset;
    int end;

    internal WireReader(byte[] source, int offset, int length)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (offset < 0 || length < 0 || offset > source.Length - length)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        this.source = source;
        this.start = offset;
        this.offset = offset;
        this.end = offset + length;
    }

    internal int ReadOffset()
    {
        return this.offset;
    }

    internal int ConsumedLength()
    {
        return this.offset - this.start;
    }

    internal int RemainingLength()
    {
        return this.end - this.offset;
    }

    internal WireReadResult ReadUnsigned8()
    {
        int initialOffset = this.offset;
        if (!this.CanRead(1))
        {
            return this.FailAt(
                DnsDiagnosticCode.UnexpectedEnd,
                "unsigned8",
                "One octet is required.",
                this.offset);
        }

        WireReadResult result = new WireReadResult();
        result.Success = true;
        result.UnsignedValue = this.source[this.offset];
        result.StartOffset = initialOffset;
        this.offset = this.offset + 1;
        result.NextOffset = this.offset;
        return result;
    }

    internal WireReadResult ReadUnsigned16()
    {
        int initialOffset = this.offset;
        if (!this.CanRead(2))
        {
            return this.FailAt(
                DnsDiagnosticCode.UnexpectedEnd,
                "unsigned16",
                "Two octets are required.",
                this.offset);
        }

        ulong value = (ulong)this.source[this.offset] << 8;
        value = value | this.source[this.offset + 1];
        WireReadResult result = new WireReadResult();
        result.Success = true;
        result.UnsignedValue = value;
        result.StartOffset = initialOffset;
        this.offset = this.offset + 2;
        result.NextOffset = this.offset;
        return result;
    }

    internal WireReadResult ReadUnsigned32()
    {
        int initialOffset = this.offset;
        if (!this.CanRead(4))
        {
            return this.FailAt(
                DnsDiagnosticCode.UnexpectedEnd,
                "unsigned32",
                "Four octets are required.",
                this.offset);
        }

        ulong value = (ulong)this.source[this.offset] << 24;
        value = value | ((ulong)this.source[this.offset + 1] << 16);
        value = value | ((ulong)this.source[this.offset + 2] << 8);
        value = value | this.source[this.offset + 3];
        WireReadResult result = new WireReadResult();
        result.Success = true;
        result.UnsignedValue = value;
        result.StartOffset = initialOffset;
        this.offset = this.offset + 4;
        result.NextOffset = this.offset;
        return result;
    }

    internal WireReadResult ReadUnsigned48()
    {
        int initialOffset = this.offset;
        if (!this.CanRead(6))
        {
            return this.FailAt(
                DnsDiagnosticCode.UnexpectedEnd,
                "unsigned48",
                "Six octets are required.",
                this.offset);
        }

        ulong value = (ulong)this.source[this.offset] << 40;
        value = value | ((ulong)this.source[this.offset + 1] << 32);
        value = value | ((ulong)this.source[this.offset + 2] << 24);
        value = value | ((ulong)this.source[this.offset + 3] << 16);
        value = value | ((ulong)this.source[this.offset + 4] << 8);
        value = value | this.source[this.offset + 5];
        WireReadResult result = new WireReadResult();
        result.Success = true;
        result.UnsignedValue = value;
        result.StartOffset = initialOffset;
        this.offset = this.offset + 6;
        result.NextOffset = this.offset;
        return result;
    }

    internal WireReadResult ReadBytes(int count)
    {
        return this.ReadExactBytes(count, "bytes");
    }

    internal WireReadResult ReadCharacterString()
    {
        int initialOffset = this.offset;
        if (!this.CanRead(1))
        {
            return this.FailAt(
                DnsDiagnosticCode.UnexpectedEnd,
                "characterString",
                "A character-string length octet is required.",
                this.offset);
        }

        int length = this.source[this.offset];
        if (this.RemainingLength() - 1 < length)
        {
            return this.FailAt(
                DnsDiagnosticCode.UnexpectedEnd,
                "characterString",
                "The character-string payload is truncated.",
                this.offset + 1);
        }

        byte[] bytes = new byte[length];
        System.Array.Copy(this.source, this.offset + 1, bytes, 0, length);
        WireReadResult result = new WireReadResult();
        result.Success = true;
        result.UnsignedValue = (ulong)length;
        result.Bytes = bytes;
        result.StartOffset = initialOffset;
        this.offset = this.offset + length + 1;
        result.NextOffset = this.offset;
        return result;
    }

    internal WireReadResult ReadIpv4Address()
    {
        return this.ReadExactBytes(4, "ipv4Address");
    }

    internal WireReadResult ReadIpv6Address()
    {
        return this.ReadExactBytes(16, "ipv6Address");
    }

    internal WireReadResult ReadSubregion(int count)
    {
        int initialOffset = this.offset;
        if (count < 0)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "subregion",
                "A subregion length cannot be negative.",
                this.offset);
        }

        if (!this.CanRead(count))
        {
            return this.FailAt(
                DnsDiagnosticCode.UnexpectedEnd,
                "subregion",
                "The requested subregion exceeds the containing region.",
                this.offset);
        }

        WireReadResult result = new WireReadResult();
        result.Success = true;
        result.Subregion = new WireReader(this.source, this.offset, count);
        result.StartOffset = initialOffset;
        this.offset = this.offset + count;
        result.NextOffset = this.offset;
        return result;
    }

    internal WireReadResult ReadTypeBitmap(int length)
    {
        int initialOffset = this.offset;
        if (length < 0)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "typeBitmap",
                "A type bitmap length cannot be negative.",
                this.offset);
        }

        if (!this.CanRead(length))
        {
            return this.FailAt(
                DnsDiagnosticCode.UnexpectedEnd,
                "typeBitmap",
                "The type bitmap exceeds the containing region.",
                this.offset);
        }

        int bitmapEnd = this.offset + length;
        int cursor = this.offset;
        int previousWindow = -1;
        List<int> values = new List<int>();
        while (cursor < bitmapEnd)
        {
            if (bitmapEnd - cursor < 2)
            {
                return this.FailAt(
                    DnsDiagnosticCode.UnexpectedEnd,
                    "typeBitmap",
                    "A type bitmap window header is truncated.",
                    cursor);
            }

            int window = this.source[cursor];
            int bitmapLength = this.source[cursor + 1];
            if (window <= previousWindow)
            {
                return this.FailAt(
                    DnsDiagnosticCode.InvalidTypeBitmap,
                    "typeBitmap",
                    "Type bitmap windows must be strictly increasing.",
                    cursor);
            }

            if (bitmapLength < 1 || bitmapLength > 32)
            {
                return this.FailAt(
                    DnsDiagnosticCode.InvalidTypeBitmap,
                    "typeBitmap",
                    "A type bitmap window length must be between one and thirty-two.",
                    cursor + 1);
            }

            if (bitmapEnd - cursor - 2 < bitmapLength)
            {
                return this.FailAt(
                    DnsDiagnosticCode.UnexpectedEnd,
                    "typeBitmap",
                    "A type bitmap window payload is truncated.",
                    cursor + 2);
            }

            if (this.source[cursor + 1 + bitmapLength] == 0)
            {
                return this.FailAt(
                    DnsDiagnosticCode.InvalidTypeBitmap,
                    "typeBitmap",
                    "A type bitmap window has a redundant trailing zero octet.",
                    cursor + 1 + bitmapLength);
            }

            if (window == 0 && (this.source[cursor + 2] & 128) != 0)
            {
                return this.FailAt(
                    DnsDiagnosticCode.InvalidTypeBitmap,
                    "typeBitmap",
                    "Type zero cannot be present in a type bitmap.",
                    cursor + 2);
            }

            int byteIndex = 0;
            while (byteIndex < bitmapLength)
            {
                int octet = this.source[cursor + 2 + byteIndex];
                int bitIndex = 0;
                while (bitIndex < 8)
                {
                    int mask = 128 >> bitIndex;
                    if ((octet & mask) != 0)
                    {
                        values.Add((window * 256) + (byteIndex * 8) + bitIndex);
                    }

                    bitIndex = bitIndex + 1;
                }

                byteIndex = byteIndex + 1;
            }

            previousWindow = window;
            cursor = cursor + 2 + bitmapLength;
        }

        byte[] bytes = new byte[length];
        System.Array.Copy(this.source, initialOffset, bytes, 0, length);
        WireReadResult result = new WireReadResult();
        result.Success = true;
        result.Bytes = bytes;
        result.IntegerValues = values;
        result.StartOffset = initialOffset;
        this.offset = bitmapEnd;
        result.NextOffset = this.offset;
        return result;
    }

    internal bool CanRead(int count)
    {
        return count >= 0 && count <= this.end - this.offset;
    }

    internal WireReadResult ReadExactBytes(int count, string field)
    {
        int initialOffset = this.offset;
        if (count < 0)
        {
            return this.FailAt(
                DnsDiagnosticCode.ValueOutOfRange,
                field,
                "A byte count cannot be negative.",
                this.offset);
        }

        if (!this.CanRead(count))
        {
            return this.FailAt(
                DnsDiagnosticCode.UnexpectedEnd,
                field,
                "The requested bytes exceed the containing region.",
                this.offset);
        }

        byte[] bytes = new byte[count];
        System.Array.Copy(this.source, this.offset, bytes, 0, count);
        WireReadResult result = new WireReadResult();
        result.Success = true;
        result.Bytes = bytes;
        result.StartOffset = initialOffset;
        this.offset = this.offset + count;
        result.NextOffset = this.offset;
        return result;
    }

    internal WireReadResult FailAt(
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
        WireReadResult result = new WireReadResult();
        result.StartOffset = this.offset;
        result.NextOffset = this.offset;
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
