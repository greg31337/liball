namespace asn1;

internal class OerLengthCodec
{
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;
    internal byte[] ValueOctets = [];
    internal int ValueOctetCount;
    internal int ExpectedValueOctetCount;
    internal int TotalConsumedCount;
    internal bool HeaderRead;
    internal bool Terminal;
    internal OerLengthResult TerminalResult = new OerLengthResult();

    internal OerLengthCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Resources = new ResourceCounter(limits);
    }

    internal OerLengthResult WriteLength(long length)
    {
        if (length < 0)
        {
            return this.CreateWriteError(
                "ASNO0001",
                "An OER length determinant cannot be negative.",
                false);
        }

        if (length < 128)
        {
            if (!this.Resources.AllowsLengthOctets(1))
            {
                return this.CreateWriteError(
                    "ASNR0003",
                    "The OER length determinant exceeds the configured length-octet limit.",
                    true);
            }

            if (!this.Resources.TryChargeAllocationBytes(1))
            {
                return this.CreateWriteError(
                    "ASNR0005",
                    "Allocating the OER length determinant exceeds the configured limit.",
                    true);
            }

            return new OerLengthResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Length = length,
                Data = [(byte)length],
                TotalConsumedCount = 1
            };
        }

        long remaining = length;
        int valueOctets = 0;
        while (remaining > 0)
        {
            valueOctets = valueOctets + 1;
            remaining = remaining >> 8;
        }

        int totalOctets = valueOctets + 1;
        if (!this.Resources.AllowsLengthOctets(totalOctets))
        {
            return this.CreateWriteError(
                "ASNR0003",
                "The OER length determinant exceeds the configured length-octet limit.",
                true);
        }

        if (!this.Resources.TryChargeAllocationBytes(totalOctets))
        {
            return this.CreateWriteError(
                "ASNR0005",
                "Allocating the OER length determinant exceeds the configured limit.",
                true);
        }

        byte[] data = new byte[totalOctets];
        data[0] = (byte)(128 | valueOctets);
        remaining = length;
        int index = data.Length - 1;
        while (index > 0)
        {
            data[index] = (byte)(remaining & 255);
            remaining = remaining >> 8;
            index = index - 1;
        }

        return new OerLengthResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Length = length,
            Data = data,
            TotalConsumedCount = totalOctets
        };
    }

    internal OerLengthResult ConsumeLength(
        byte[] data,
        int offset,
        int count,
        bool finalBlock,
        bool canonical)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (this.Terminal)
        {
            return this.CreateTransientError(
                "ASNO0008",
                "The OER length decoder is terminal until reset.",
                false,
                0,
                this.TotalConsumedCount);
        }

        int consumed = 0;
        if (!this.HeaderRead && consumed < count)
        {
            if (!this.Resources.AllowsLengthOctets(1))
            {
                return this.SetTerminalError(
                    "ASNR0003",
                    "The OER length determinant exceeds the configured length-octet limit.",
                    true,
                    0,
                    consumed);
            }

            int first = data[offset + consumed];
            consumed = consumed + 1;
            this.TotalConsumedCount = this.TotalConsumedCount + 1;
            this.HeaderRead = true;
            if (first < 128)
            {
                return this.SetTerminalSuccess(first, true, consumed);
            }

            this.ExpectedValueOctetCount = first & 127;
            if (this.ExpectedValueOctetCount == 0)
            {
                return this.SetTerminalError(
                    "ASNO0002",
                    "The OER long-form length must contain at least one value octet.",
                    false,
                    0,
                    consumed);
            }

            int totalOctets = this.ExpectedValueOctetCount + 1;
            if (!this.Resources.AllowsLengthOctets(totalOctets))
            {
                return this.SetTerminalError(
                    "ASNR0003",
                    "The OER length determinant exceeds the configured length-octet limit.",
                    true,
                    0,
                    consumed);
            }

            if (!this.Resources.TryChargeAllocationBytes(
                this.ExpectedValueOctetCount))
            {
                return this.SetTerminalError(
                    "ASNR0005",
                    "Buffering the OER length determinant exceeds the configured allocation limit.",
                    true,
                    0,
                    consumed);
            }

            this.ValueOctets = new byte[this.ExpectedValueOctetCount];
        }

        while (consumed < count
            && this.ValueOctetCount < this.ExpectedValueOctetCount)
        {
            this.ValueOctets[this.ValueOctetCount] = data[offset + consumed];
            this.ValueOctetCount = this.ValueOctetCount + 1;
            consumed = consumed + 1;
            this.TotalConsumedCount = this.TotalConsumedCount + 1;
        }

        if (!this.HeaderRead
            || this.ValueOctetCount < this.ExpectedValueOctetCount)
        {
            if (finalBlock)
            {
                return this.SetTerminalError(
                    "ASNO0003",
                    "The final OER input ends within a length determinant.",
                    false,
                    this.TotalConsumedCount,
                    consumed);
            }

            return new OerLengthResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = consumed,
                TotalConsumedCount = this.TotalConsumedCount
            };
        }

        return this.DecodeValueOctets(canonical, consumed);
    }

    internal OerLengthResult CompleteLength(bool canonical)
    {
        return this.ConsumeLength([], 0, 0, true, canonical);
    }

    internal void ResetLength()
    {
        this.ValueOctets = [];
        this.ValueOctetCount = 0;
        this.ExpectedValueOctetCount = 0;
        this.TotalConsumedCount = 0;
        this.HeaderRead = false;
        this.Terminal = false;
        this.TerminalResult = new OerLengthResult();
        this.Resources = new ResourceCounter(this.Limits);
    }

    internal OerLengthResult DecodeValueOctets(
        bool canonical,
        int consumedCount)
    {
        bool leadingZero = this.ValueOctets[0] == 0;
        long length = 0;
        int index = 0;
        while (index < this.ValueOctets.Length)
        {
            int current = this.ValueOctets[index];
            if (length > (long.MaxValue - current) / 256)
            {
                return this.SetTerminalError(
                    "ASNO0005",
                    "The OER length exceeds the supported Int64 range.",
                    false,
                    index + 1,
                    consumedCount);
            }

            length = length * 256 + current;
            index = index + 1;
        }

        bool minimal = !leadingZero && length >= 128;
        if (canonical && !minimal)
        {
            return this.SetTerminalError(
                "ASNO0004",
                "The COER length determinant does not use the shortest form.",
                false,
                leadingZero ? 1 : 0,
                consumedCount);
        }

        return this.SetTerminalSuccess(length, minimal, consumedCount);
    }

    internal OerLengthResult SetTerminalSuccess(
        long length,
        bool minimal,
        int consumedCount)
    {
        OerLengthResult result = new OerLengthResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Length = length,
            Minimal = minimal,
            ConsumedCount = consumedCount,
            TotalConsumedCount = this.TotalConsumedCount
        };
        this.Terminal = true;
        this.TerminalResult = result;
        return result;
    }

    internal OerLengthResult SetTerminalError(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset,
        int consumedCount)
    {
        OerLengthResult result = this.CreateTransientError(
            code,
            message,
            limitExceeded,
            violationOffset,
            consumedCount);
        this.Terminal = true;
        this.TerminalResult = result;
        return result;
    }

    internal OerLengthResult CreateTransientError(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset,
        int consumedCount)
    {
        return new OerLengthResult
        {
            Status = AsnDecodeStatus.Error,
            Minimal = false,
            ConsumedCount = consumedCount,
            TotalConsumedCount = this.TotalConsumedCount,
            HasViolationOffset = true,
            ViolationOffset = violationOffset,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerLengthResult CreateWriteError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerLengthResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
