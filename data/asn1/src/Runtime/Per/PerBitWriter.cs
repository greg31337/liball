namespace asn1;

internal class PerBitWriter
{
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;
    internal byte[] Buffer = [];
    internal int BitCount;
    internal bool Failed;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
    internal bool LimitExceeded;
    internal System.IO.Stream? OutputStream;
    internal int EmittedByteCount;

    internal PerBitWriter(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Resources = new ResourceCounter(limits);
    }

    internal PerBitWriteResult WriteBit(bool value)
    {
        if (!this.PrepareAdditionalBits(1))
        {
            return this.CopyFailure();
        }

        this.WriteUncheckedBit(value);
        return this.CreateSuccess(1, 0);
    }

    internal PerBitWriteResult WriteBits(
        System.Numerics.BigInteger value,
        int bitCount)
    {
        if (bitCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        if (!new ResourceCounter(this.Limits).AllowsIntegerBits(bitCount))
        {
            return this.CreateError(
                "ASNR0010",
                "The requested bit field exceeds the configured integer-bit limit.",
                true);
        }

        if (value < System.Numerics.BigInteger.Zero
            || value >> bitCount != System.Numerics.BigInteger.Zero)
        {
            return this.CreateError(
                "ASNP0001",
                "The non-negative value does not fit the requested bit count.",
                false);
        }

        if (!this.PrepareAdditionalBits(bitCount))
        {
            return this.CopyFailure();
        }

        int index = bitCount - 1;
        while (index >= 0)
        {
            bool bit = ((value >> index) & System.Numerics.BigInteger.One)
                != System.Numerics.BigInteger.Zero;
            this.WriteUncheckedBit(bit);
            index = index - 1;
        }

        return this.CreateSuccess(bitCount, 0);
    }

    internal PerBitWriteResult WriteInputBits(
        byte[] data,
        int offset,
        int bitCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (bitCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        int byteCount = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            byteCount = byteCount + 1;
        }

        if (!CursorMath.IsRangeValid(offset, byteCount, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (this.OutputStream != null
            && this.BitCount % 8 == 0
            && bitCount % 8 == 0)
        {
            return this.WriteStreamingOctets(data, offset, byteCount);
        }

        if (!this.PrepareAdditionalBits(bitCount))
        {
            return this.CopyFailure();
        }

        int index = 0;
        while (index < bitCount)
        {
            int sourceByte = offset + index / 8;
            int sourceShift = 7 - index % 8;
            this.WriteUncheckedBit(
                ((data[sourceByte] >> sourceShift) & 1) != 0);
            index = index + 1;
        }

        return this.CreateSuccess(bitCount, 0);
    }

    internal PerBitWriteResult WriteAlignedOctets(
        byte[] data,
        int offset,
        int count)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (this.BitCount % 8 != 0)
        {
            return this.CreateError(
                "ASNP0002",
                "Aligned octets require an octet-aligned output position.",
                false);
        }

        if (count > int.MaxValue / 8)
        {
            return this.CreateError(
                "ASNR0008",
                "The aligned octet count exceeds the supported bit range.",
                true);
        }

        int bitCount = count * 8;
        if (this.OutputStream != null)
        {
            return this.WriteStreamingOctets(data, offset, count);
        }

        if (!this.PrepareAdditionalBits(bitCount))
        {
            return this.CopyFailure();
        }

        if (count > 0)
        {
            System.Buffer.BlockCopy(
                data,
                offset,
                this.Buffer,
                this.BitCount / 8,
                count);
            this.BitCount = this.BitCount + bitCount;
        }

        return this.CreateSuccess(bitCount, 0);
    }

    internal PerBitWriteResult AlignOutput()
    {
        int padding = (8 - this.BitCount % 8) % 8;
        if (!this.PrepareAdditionalBits(padding))
        {
            return this.CopyFailure();
        }

        int index = 0;
        while (index < padding)
        {
            this.WriteUncheckedBit(false);
            index = index + 1;
        }

        return this.CreateSuccess(padding, padding);
    }

    internal PerBitWriteResult GetOutput()
    {
        if (this.Failed)
        {
            return this.CopyFailure();
        }

        if (this.OutputStream != null)
        {
            return new PerBitWriteResult
            {
                Success = true,
                WrittenBitCount = this.BitCount
            };
        }

        int byteCount = this.GetByteCount();
        if (!this.Resources.TryChargeAllocationBytes(byteCount))
        {
            return this.CreateError(
                "ASNR0005",
                "Copying the bit output exceeds the configured allocation limit.",
                true);
        }

        byte[] data = new byte[byteCount];
        if (byteCount > 0)
        {
            System.Buffer.BlockCopy(this.Buffer, 0, data, 0, byteCount);
        }

        return new PerBitWriteResult
        {
            Success = true,
            Data = data,
            WrittenBitCount = this.BitCount
        };
    }

    internal int GetWrittenBitCount()
    {
        return this.BitCount;
    }

    internal int GetByteCount()
    {
        int byteCount = this.BitCount / 8;
        return this.BitCount % 8 == 0 ? byteCount : byteCount + 1;
    }

    internal int GetEmittedByteCount()
    {
        return this.EmittedByteCount;
    }

    internal bool IsStreaming()
    {
        return this.OutputStream != null;
    }

    internal void SetOutputStream(System.IO.Stream output)
    {
        if (output == null)
        {
            throw new System.ArgumentNullException("output");
        }

        if (this.BitCount != 0 || this.Buffer.Length != 0)
        {
            throw new System.InvalidOperationException(
                "A PER output stream must be configured before writing begins.");
        }

        this.OutputStream = output;
    }

    internal void ResetWriter()
    {
        this.Buffer = [];
        this.BitCount = 0;
        this.Failed = false;
        this.ErrorCode = "";
        this.ErrorMessage = "";
        this.LimitExceeded = false;
        this.OutputStream = null;
        this.EmittedByteCount = 0;
        this.Resources = new ResourceCounter(this.Limits);
    }

    internal bool PrepareAdditionalBits(int additionalBits)
    {
        if (this.Failed)
        {
            return false;
        }

        if (additionalBits < 0 || additionalBits > int.MaxValue - this.BitCount)
        {
            this.SetFailure(
                "ASNR0008",
                "The bit output exceeds the supported bit-count range.",
                true);
            return false;
        }

        int requiredBits = this.BitCount + additionalBits;
        if (this.OutputStream != null)
        {
            if (additionalBits == 0)
            {
                return true;
            }

            if (this.Buffer.Length == 0)
            {
                if (!this.Resources.TryChargeAllocationBytes(1))
                {
                    this.SetFailure(
                        "ASNR0005",
                        "Buffering the partial output octet exceeds the configured allocation limit.",
                        true);
                    return false;
                }

                this.Buffer = new byte[1];
            }

            return true;
        }

        int requiredBytes = requiredBits / 8;
        if (requiredBits % 8 != 0)
        {
            requiredBytes = requiredBytes + 1;
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
            requiredBytes))
        {
            this.SetFailure(
                "ASNR0006",
                "The bit output exceeds the configured byte limit.",
                true);
            return false;
        }

        if (requiredBytes <= this.Buffer.Length)
        {
            return true;
        }

        int capacity = this.Buffer.Length == 0 ? 4 : this.Buffer.Length;
        while (capacity < requiredBytes)
        {
            if (capacity > int.MaxValue / 2)
            {
                capacity = requiredBytes;
            }
            else
            {
                capacity = capacity * 2;
            }
        }

        if (!this.Limits.Unlimited
            && capacity > this.Limits.MaximumOpenTypeBytes)
        {
            capacity = requiredBytes;
        }

        if (!this.Resources.TryChargeAllocationBytes(capacity))
        {
            this.SetFailure(
                "ASNR0005",
                "Growing the bit output exceeds the configured allocation limit.",
                true);
            return false;
        }

        byte[] replacement = new byte[capacity];
        if (this.Buffer.Length > 0)
        {
            System.Buffer.BlockCopy(
                this.Buffer,
                0,
                replacement,
                0,
                this.Buffer.Length);
        }

        this.Buffer = replacement;
        return true;
    }

    internal void WriteUncheckedBit(bool value)
    {
        int byteIndex = this.OutputStream == null
            ? this.BitCount / 8
            : 0;
        int shift = 7 - this.BitCount % 8;
        int mask = 1 << shift;
        if (value)
        {
            this.Buffer[byteIndex] = (byte)(this.Buffer[byteIndex] | mask);
        }
        else
        {
            this.Buffer[byteIndex] = (byte)(this.Buffer[byteIndex] & ~mask);
        }

        this.BitCount = this.BitCount + 1;
        if (this.OutputStream != null && this.BitCount % 8 == 0)
        {
            this.OutputStream.Write(this.Buffer, 0, 1);
            this.EmittedByteCount = this.EmittedByteCount + 1;
            this.Buffer[0] = 0;
        }
    }

    internal PerBitWriteResult WriteStreamingOctets(
        byte[] data,
        int offset,
        int count)
    {
        if (this.Failed)
        {
            return this.CopyFailure();
        }

        if (this.OutputStream == null || this.BitCount % 8 != 0)
        {
            return this.CreateError(
                "ASNP0002",
                "Streaming octets require an octet-aligned output position.",
                false);
        }

        if (count > (int.MaxValue - this.BitCount) / 8)
        {
            return this.CreateError(
                "ASNR0008",
                "The streamed octet count exceeds the supported bit range.",
                true);
        }

        if (count > 0)
        {
            int index = 0;
            while (index < count)
            {
                this.OutputStream.Write(data, offset + index, 1);
                index = index + 1;
            }
            this.BitCount = this.BitCount + count * 8;
            this.EmittedByteCount = this.EmittedByteCount + count;
        }

        return this.CreateSuccess(count * 8, 0);
    }

    internal void SetFailure(
        string code,
        string message,
        bool limitExceeded)
    {
        this.Failed = true;
        this.ErrorCode = code;
        this.ErrorMessage = message;
        this.LimitExceeded = limitExceeded;
    }

    internal PerBitWriteResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        this.SetFailure(code, message, limitExceeded);
        return this.CopyFailure();
    }

    internal PerBitWriteResult CopyFailure()
    {
        return new PerBitWriteResult
        {
            WrittenBitCount = this.BitCount,
            LimitExceeded = this.LimitExceeded,
            ErrorCode = this.ErrorCode,
            ErrorMessage = this.ErrorMessage
        };
    }

    internal PerBitWriteResult CreateSuccess(
        int writtenBitCount,
        int paddedBitCount)
    {
        return new PerBitWriteResult
        {
            Success = true,
            WrittenBitCount = writtenBitCount,
            PaddedBitCount = paddedBitCount
        };
    }
}
