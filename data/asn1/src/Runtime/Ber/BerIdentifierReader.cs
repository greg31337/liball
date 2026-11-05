namespace asn1;

internal class BerIdentifierReader
{
    internal ResourceCounter Resources;

    internal BerIdentifierReader(AsnResourceLimits limits)
    {
        this.Resources = new ResourceCounter(limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits);
    }

    internal BerIdentifierReadResult ReadIdentifier(
        System.ReadOnlySpan<byte> input,
        bool requireMinimal)
    {
        if (input.Length == 0)
        {
            return new BerIdentifierReadResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        if (!this.Resources.AllowsTagOctets(1))
        {
            return this.CreateLimitResult(
                0,
                "ASNR0001",
                "The encoded identifier exceeds the configured tag-octet limit.");
        }

        byte first = input[0];
        AsnTag tag = new AsnTag
        {
            Class = (AsnTagClass)((first >> 6) & 3)
        };
        bool constructed = (first & 32) != 0;
        int shortNumber = first & 31;
        if (shortNumber != 31)
        {
            int shortBits = this.GetGroupBitCount(shortNumber);
            if (!this.Resources.AllowsIntegerBits(shortBits))
            {
                return this.CreateRangeResult(tag, constructed, 1);
            }

            tag.Number = new System.Numerics.BigInteger(shortNumber);
            return new BerIdentifierReadResult
            {
                Status = AsnDecodeStatus.Done,
                Tag = tag,
                Constructed = constructed,
                ConsumedCount = 1
            };
        }

        System.Numerics.BigInteger number = System.Numerics.BigInteger.Zero;
        bool minimal = true;
        bool significant = false;
        int bitCount = 0;
        int index = 1;
        while (index < input.Length)
        {
            int octetCount = index + 1;
            if (!this.Resources.AllowsTagOctets(octetCount))
            {
                tag.Number = number;
                BerIdentifierReadResult limited = this.CreateLimitResult(
                    index,
                    "ASNR0001",
                    "The encoded identifier exceeds the configured tag-octet limit.");
                limited.Tag = tag;
                limited.Constructed = constructed;
                limited.Minimal = minimal;
                return limited;
            }

            byte current = input[index];
            int group = current & 127;
            if (index == 1 && group == 0)
            {
                minimal = false;
            }

            if (!significant && group != 0)
            {
                significant = true;
                bitCount = this.GetGroupBitCount(group);
            }
            else if (significant)
            {
                if (bitCount > int.MaxValue - 7)
                {
                    tag.Number = number;
                    return this.CreateOverflowResult(
                        tag,
                        constructed,
                        minimal,
                        octetCount);
                }

                bitCount = bitCount + 7;
            }

            if (!this.Resources.AllowsIntegerBits(bitCount))
            {
                tag.Number = number;
                BerIdentifierReadResult ranged = this.CreateRangeResult(
                    tag,
                    constructed,
                    octetCount);
                ranged.Minimal = minimal;
                return ranged;
            }

            number = (number << 7) | new System.Numerics.BigInteger(group);
            index = index + 1;
            if ((current & 128) == 0)
            {
                if (number < new System.Numerics.BigInteger(31))
                {
                    minimal = false;
                }

                tag.Number = number;
                if (requireMinimal && !minimal)
                {
                    return new BerIdentifierReadResult
                    {
                        Status = AsnDecodeStatus.Error,
                        Tag = tag,
                        Constructed = constructed,
                        Minimal = false,
                        ConsumedCount = index,
                        ErrorCode = "ASND0001",
                        ErrorMessage = "The identifier does not use the minimal tag-number form."
                    };
                }

                return new BerIdentifierReadResult
                {
                    Status = AsnDecodeStatus.Done,
                    Tag = tag,
                    Constructed = constructed,
                    Minimal = minimal,
                    ConsumedCount = index
                };
            }
        }

        return new BerIdentifierReadResult
        {
            Status = AsnDecodeStatus.NeedMoreData,
            Tag = tag,
            Constructed = constructed,
            Minimal = minimal,
            ConsumedCount = input.Length
        };
    }

    internal int GetGroupBitCount(int value)
    {
        int bits = 0;
        while (value > 0)
        {
            bits = bits + 1;
            value = value >> 1;
        }

        return bits;
    }

    internal BerIdentifierReadResult CreateLimitResult(
        int consumedCount,
        string code,
        string message)
    {
        return new BerIdentifierReadResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = true,
            ConsumedCount = consumedCount,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal BerIdentifierReadResult CreateRangeResult(
        AsnTag tag,
        bool constructed,
        int consumedCount)
    {
        return new BerIdentifierReadResult
        {
            Status = AsnDecodeStatus.Error,
            Tag = tag,
            Constructed = constructed,
            LimitExceeded = true,
            ConsumedCount = consumedCount,
            ErrorCode = "ASNR0002",
            ErrorMessage = "The tag number exceeds the configured integer-bit limit."
        };
    }

    internal BerIdentifierReadResult CreateOverflowResult(
        AsnTag tag,
        bool constructed,
        bool minimal,
        int consumedCount)
    {
        return new BerIdentifierReadResult
        {
            Status = AsnDecodeStatus.Error,
            Tag = tag,
            Constructed = constructed,
            Minimal = minimal,
            LimitExceeded = true,
            ConsumedCount = consumedCount,
            ErrorCode = "ASNR0002",
            ErrorMessage = "The tag number cannot be represented within the supported BigInteger bit range."
        };
    }
}
