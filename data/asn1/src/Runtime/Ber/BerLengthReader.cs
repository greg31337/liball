namespace asn1;

internal class BerLengthReader
{
    internal ResourceCounter Resources;

    internal BerLengthReader(AsnResourceLimits limits)
    {
        this.Resources = new ResourceCounter(limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits);
    }

    internal BerLengthReadResult ReadLength(
        System.ReadOnlySpan<byte> input,
        bool requireMinimal)
    {
        if (input.Length == 0)
        {
            return new BerLengthReadResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        if (!this.Resources.AllowsLengthOctets(1))
        {
            return this.CreateLengthLimitResult(0);
        }

        byte first = input[0];
        if (first < 128)
        {
            return new BerLengthReadResult
            {
                Status = AsnDecodeStatus.Done,
                Length = first,
                ConsumedCount = 1
            };
        }

        if (first == 128)
        {
            return new BerLengthReadResult
            {
                Status = AsnDecodeStatus.Done,
                Indefinite = true,
                ConsumedCount = 1
            };
        }

        if (first == 255)
        {
            return new BerLengthReadResult
            {
                Status = AsnDecodeStatus.Error,
                ConsumedCount = 1,
                ErrorCode = "ASND0002",
                ErrorMessage = "The length determinant uses the reserved initial octet 0xFF."
            };
        }

        int valueOctets = first & 127;
        int totalOctets = valueOctets + 1;
        if (!this.Resources.AllowsLengthOctets(totalOctets))
        {
            return this.CreateLengthLimitResult(1);
        }

        if (input.Length < totalOctets)
        {
            return new BerLengthReadResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = input.Length
            };
        }

        bool minimal = input[1] != 0;
        long length = 0;
        int index = 1;
        while (index < totalOctets)
        {
            int current = input[index];
            if (length > (long.MaxValue - current) / 256)
            {
                return new BerLengthReadResult
                {
                    Status = AsnDecodeStatus.Error,
                    Minimal = minimal,
                    ConsumedCount = index + 1,
                    ErrorCode = "ASND0003",
                    ErrorMessage = "The definite length exceeds the supported Int64 range."
                };
            }

            length = length * 256 + current;
            index = index + 1;
        }

        if (length < 128)
        {
            minimal = false;
        }

        if (requireMinimal && !minimal)
        {
            return new BerLengthReadResult
            {
                Status = AsnDecodeStatus.Error,
                Length = length,
                Minimal = false,
                ConsumedCount = totalOctets,
                ErrorCode = "ASND0004",
                ErrorMessage = "The length determinant does not use the minimal definite form."
            };
        }

        return new BerLengthReadResult
        {
            Status = AsnDecodeStatus.Done,
            Length = length,
            Minimal = minimal,
            ConsumedCount = totalOctets
        };
    }

    internal BerEndOfContentsResult ReadEndOfContents(
        System.ReadOnlySpan<byte> input)
    {
        if (input.Length == 0)
        {
            return new BerEndOfContentsResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        if (input[0] != 0)
        {
            return new BerEndOfContentsResult
            {
                Status = AsnDecodeStatus.Done
            };
        }

        if (!this.Resources.AllowsTagOctets(1)
            || !this.Resources.AllowsLengthOctets(1))
        {
            return new BerEndOfContentsResult
            {
                Status = AsnDecodeStatus.Error,
                LimitExceeded = true,
                ErrorCode = "ASNR0003",
                ErrorMessage = "The end-of-contents marker exceeds the configured identifier or length limit."
            };
        }

        if (input.Length == 1)
        {
            return new BerEndOfContentsResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = 1
            };
        }

        if (input[1] != 0)
        {
            return new BerEndOfContentsResult
            {
                Status = AsnDecodeStatus.Error,
                ConsumedCount = 2,
                ErrorCode = "ASND0005",
                ErrorMessage = "The end-of-contents identifier must be followed by a zero length."
            };
        }

        return new BerEndOfContentsResult
        {
            Status = AsnDecodeStatus.Done,
            Matched = true,
            ConsumedCount = 2
        };
    }

    internal BerLengthReadResult CreateLengthLimitResult(int consumedCount)
    {
        return new BerLengthReadResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = true,
            ConsumedCount = consumedCount,
            ErrorCode = "ASNR0003",
            ErrorMessage = "The length determinant exceeds the configured length-octet limit."
        };
    }
}
