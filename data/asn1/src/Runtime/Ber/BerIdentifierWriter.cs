namespace asn1;

internal class BerIdentifierWriter
{
    internal ResourceCounter Resources;

    internal BerIdentifierWriter(AsnResourceLimits limits)
    {
        this.Resources = new ResourceCounter(limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits);
    }

    internal BerIdentifierWriteResult WriteIdentifier(
        AsnTag tag,
        bool constructed)
    {
        if (tag == null)
        {
            throw new System.ArgumentNullException("tag");
        }

        int tagClass = (int)tag.Class;
        if (tagClass < (int)AsnTagClass.Universal
            || tagClass > (int)AsnTagClass.Private)
        {
            return this.CreateInvalidResult(
                "ASNE0001",
                "The tag class is not supported.");
        }

        if (tag.Number < System.Numerics.BigInteger.Zero)
        {
            return this.CreateInvalidResult(
                "ASNE0002",
                "The tag number must be non-negative.");
        }

        int first = tagClass << 6;
        if (constructed)
        {
            first = first | 32;
        }

        if (tag.Number < new System.Numerics.BigInteger(31))
        {
            int shortNumber = (int)tag.Number;
            int shortBits = this.GetValueBitCount(shortNumber);
            if (!this.Resources.AllowsIntegerBits(shortBits))
            {
                return this.CreateLimitResult(
                    "ASNR0002",
                    "The tag number exceeds the configured integer-bit limit.");
            }

            if (!this.Resources.AllowsTagOctets(1))
            {
                return this.CreateLimitResult(
                    "ASNR0001",
                    "The encoded identifier exceeds the configured tag-octet limit.");
            }

            return new BerIdentifierWriteResult
            {
                Success = true,
                Data = [(byte)(first | shortNumber)],
                WrittenCount = 1
            };
        }

        System.Numerics.BigInteger remaining = tag.Number;
        int groupCount = 0;
        int topBits = 0;
        while (remaining > System.Numerics.BigInteger.Zero)
        {
            int group = (int)(remaining & new System.Numerics.BigInteger(127));
            remaining = remaining >> 7;
            groupCount = groupCount + 1;
            if (!this.Resources.AllowsTagOctets(groupCount + 1))
            {
                return this.CreateLimitResult(
                    "ASNR0001",
                    "The encoded identifier exceeds the configured tag-octet limit.");
            }

            if (remaining == System.Numerics.BigInteger.Zero)
            {
                topBits = this.GetValueBitCount(group);
            }
            else if (groupCount > (int.MaxValue - 1) / 7)
            {
                return this.CreateLimitResult(
                    "ASNR0002",
                    "The tag number cannot be represented within the supported BigInteger bit range.");
            }
            else
            {
                int minimumBits = groupCount * 7 + 1;
                if (!this.Resources.AllowsIntegerBits(minimumBits))
                {
                    return this.CreateLimitResult(
                        "ASNR0002",
                        "The tag number exceeds the configured integer-bit limit.");
                }
            }
        }

        if (groupCount - 1 > (int.MaxValue - topBits) / 7)
        {
            return this.CreateLimitResult(
                "ASNR0002",
                "The tag number cannot be represented within the supported BigInteger bit range.");
        }

        int bitCount = (groupCount - 1) * 7 + topBits;
        if (!this.Resources.AllowsIntegerBits(bitCount))
        {
            return this.CreateLimitResult(
                "ASNR0002",
                "The tag number exceeds the configured integer-bit limit.");
        }

        byte[] data = new byte[groupCount + 1];
        data[0] = (byte)(first | 31);
        remaining = tag.Number;
        int index = data.Length - 1;
        while (index > 0)
        {
            int group = (int)(remaining & new System.Numerics.BigInteger(127));
            if (index < data.Length - 1)
            {
                group = group | 128;
            }

            data[index] = (byte)group;
            remaining = remaining >> 7;
            index = index - 1;
        }

        return new BerIdentifierWriteResult
        {
            Success = true,
            Data = data,
            WrittenCount = data.Length
        };
    }

    internal int GetValueBitCount(int value)
    {
        int bits = 0;
        while (value > 0)
        {
            bits = bits + 1;
            value = value >> 1;
        }

        return bits;
    }

    internal BerIdentifierWriteResult CreateInvalidResult(
        string code,
        string message)
    {
        return new BerIdentifierWriteResult
        {
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal BerIdentifierWriteResult CreateLimitResult(
        string code,
        string message)
    {
        return new BerIdentifierWriteResult
        {
            LimitExceeded = true,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
