namespace asn1;

internal class PerBitReader
{
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;
    internal byte[] Buffer = [];
    internal int BufferedBitCount;
    internal int Position;
    internal bool FinalBlock;
    internal bool Failed;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
    internal bool LimitExceeded;
    internal bool HasViolationBitOffset;
    internal int ViolationBitOffset;

    internal PerBitReader(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Resources = new ResourceCounter(limits);
    }

    internal PerBitReadResult AppendInputBits(
        byte[] data,
        int offset,
        int bitCount,
        bool finalBlock)
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

        if (this.Failed)
        {
            return this.CopyFailure();
        }

        if (this.FinalBlock)
        {
            return this.CreateError(
                "ASNP0003",
                "No bit input can follow the final block.",
                false,
                false,
                0);
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
            this.AppendUncheckedBit(
                ((data[sourceByte] >> sourceShift) & 1) != 0);
            index = index + 1;
        }

        this.FinalBlock = finalBlock;
        return new PerBitReadResult
        {
            Status = AsnDecodeStatus.Done,
            ConsumedBitCount = bitCount,
            BitOffset = this.Position
        };
    }

    internal PerBitReadResult ReadBit()
    {
        return this.ReadBits(1);
    }

    internal PerBitReadResult ReadBits(int bitCount)
    {
        if (bitCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        if (this.Failed)
        {
            return this.CopyFailure();
        }

        if (!new ResourceCounter(this.Limits).AllowsIntegerBits(bitCount))
        {
            return this.CreateError(
                "ASNR0010",
                "The requested bit field exceeds the configured integer-bit limit.",
                true,
                false,
                0);
        }

        if (bitCount > this.BufferedBitCount - this.Position)
        {
            if (!this.FinalBlock)
            {
                return new PerBitReadResult
                {
                    Status = AsnDecodeStatus.NeedMoreData,
                    BitOffset = this.Position
                };
            }

            return this.CreateError(
                "ASNP0004",
                "The final bit input ends before the requested field is complete.",
                false,
                true,
                this.BufferedBitCount);
        }

        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        int index = 0;
        while (index < bitCount)
        {
            value = value << 1;
            if (this.GetBit(this.Position + index))
            {
                value = value | System.Numerics.BigInteger.One;
            }

            index = index + 1;
        }

        this.Position = this.Position + bitCount;
        return new PerBitReadResult
        {
            Status = AsnDecodeStatus.Done,
            Value = value,
            ConsumedBitCount = bitCount,
            BitOffset = this.Position
        };
    }

    internal PerBitReadResult ReadAlignedOctets(int count)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        if (this.Failed)
        {
            return this.CopyFailure();
        }

        if (this.Position % 8 != 0)
        {
            return this.CreateError(
                "ASNP0005",
                "Aligned octets require an octet-aligned input position.",
                false,
                true,
                this.Position);
        }

        if (count > int.MaxValue / 8)
        {
            return this.CreateError(
                "ASNR0008",
                "The aligned octet count exceeds the supported bit range.",
                true,
                false,
                0);
        }

        int bitCount = count * 8;
        if (bitCount > this.BufferedBitCount - this.Position)
        {
            if (!this.FinalBlock)
            {
                return new PerBitReadResult
                {
                    Status = AsnDecodeStatus.NeedMoreData,
                    BitOffset = this.Position
                };
            }

            return this.CreateError(
                "ASNP0004",
                "The final bit input ends before the requested octets are complete.",
                false,
                true,
                this.BufferedBitCount);
        }

        if (!this.Resources.TryChargeAllocationBytes(count))
        {
            return this.CreateError(
                "ASNR0005",
                "Copying aligned octets exceeds the configured allocation limit.",
                true,
                false,
                0);
        }

        byte[] data = new byte[count];
        if (count > 0)
        {
            System.Buffer.BlockCopy(
                this.Buffer,
                this.Position / 8,
                data,
                0,
                count);
        }

        this.Position = this.Position + bitCount;
        return new PerBitReadResult
        {
            Status = AsnDecodeStatus.Done,
            Data = data,
            ConsumedBitCount = bitCount,
            BitOffset = this.Position
        };
    }

    internal PerBitReadResult AlignInput(bool requireZeroPadding)
    {
        if (this.Failed)
        {
            return this.CopyFailure();
        }

        int padding = (8 - this.Position % 8) % 8;
        if (padding > this.BufferedBitCount - this.Position)
        {
            if (!this.FinalBlock)
            {
                return new PerBitReadResult
                {
                    Status = AsnDecodeStatus.NeedMoreData,
                    BitOffset = this.Position
                };
            }

            return this.CreateError(
                "ASNP0006",
                "The final bit input ends within octet-alignment padding.",
                false,
                true,
                this.BufferedBitCount);
        }

        int index = 0;
        while (requireZeroPadding && index < padding)
        {
            if (this.GetBit(this.Position + index))
            {
                return this.CreateError(
                    "ASNP0007",
                    "Octet-alignment padding bits must be zero.",
                    false,
                    true,
                    this.Position + index);
            }

            index = index + 1;
        }

        this.Position = this.Position + padding;
        return new PerBitReadResult
        {
            Status = AsnDecodeStatus.Done,
            ConsumedBitCount = padding,
            BitOffset = this.Position
        };
    }

    internal PerBitReadResult CompleteInput(bool requireZeroPadding)
    {
        if (this.Failed)
        {
            return this.CopyFailure();
        }

        this.FinalBlock = true;
        int remaining = this.BufferedBitCount - this.Position;
        int expectedPadding = (8 - this.Position % 8) % 8;
        if (remaining > expectedPadding)
        {
            return this.CreateError(
                "ASNP0008",
                "Significant trailing bits remain after the decoded value.",
                false,
                true,
                this.Position + expectedPadding);
        }

        return this.AlignInput(requireZeroPadding);
    }

    internal int GetBitOffset()
    {
        return this.Position;
    }

    internal int GetAvailableBitCount()
    {
        return this.BufferedBitCount - this.Position;
    }

    internal void ResetReader()
    {
        this.Buffer = [];
        this.BufferedBitCount = 0;
        this.Position = 0;
        this.FinalBlock = false;
        this.Failed = false;
        this.ErrorCode = "";
        this.ErrorMessage = "";
        this.LimitExceeded = false;
        this.HasViolationBitOffset = false;
        this.ViolationBitOffset = 0;
        this.Resources = new ResourceCounter(this.Limits);
    }

    internal bool PrepareAdditionalBits(int additionalBits)
    {
        if (additionalBits > int.MaxValue - this.BufferedBitCount)
        {
            this.SetFailure(
                "ASNR0008",
                "The bit input exceeds the supported bit-count range.",
                true,
                false,
                0);
            return false;
        }

        int requiredBits = this.BufferedBitCount + additionalBits;
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
                "The bit input exceeds the configured byte limit.",
                true,
                false,
                0);
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
                "Growing the bit input exceeds the configured allocation limit.",
                true,
                false,
                0);
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

    internal void AppendUncheckedBit(bool value)
    {
        int byteIndex = this.BufferedBitCount / 8;
        int shift = 7 - this.BufferedBitCount % 8;
        int mask = 1 << shift;
        if (value)
        {
            this.Buffer[byteIndex] = (byte)(this.Buffer[byteIndex] | mask);
        }
        else
        {
            this.Buffer[byteIndex] = (byte)(this.Buffer[byteIndex] & ~mask);
        }

        this.BufferedBitCount = this.BufferedBitCount + 1;
    }

    internal bool GetBit(int bitOffset)
    {
        int byteIndex = bitOffset / 8;
        int shift = 7 - bitOffset % 8;
        return ((this.Buffer[byteIndex] >> shift) & 1) != 0;
    }

    internal void SetFailure(
        string code,
        string message,
        bool limitExceeded,
        bool hasViolationBitOffset,
        int violationBitOffset)
    {
        this.Failed = true;
        this.ErrorCode = code;
        this.ErrorMessage = message;
        this.LimitExceeded = limitExceeded;
        this.HasViolationBitOffset = hasViolationBitOffset;
        this.ViolationBitOffset = violationBitOffset;
    }

    internal PerBitReadResult CreateError(
        string code,
        string message,
        bool limitExceeded,
        bool hasViolationBitOffset,
        int violationBitOffset)
    {
        this.SetFailure(
            code,
            message,
            limitExceeded,
            hasViolationBitOffset,
            violationBitOffset);
        return this.CopyFailure();
    }

    internal PerBitReadResult CopyFailure()
    {
        return new PerBitReadResult
        {
            Status = AsnDecodeStatus.Error,
            BitOffset = this.Position,
            HasViolationBitOffset = this.HasViolationBitOffset,
            ViolationBitOffset = this.ViolationBitOffset,
            LimitExceeded = this.LimitExceeded,
            ErrorCode = this.ErrorCode,
            ErrorMessage = this.ErrorMessage
        };
    }
}
