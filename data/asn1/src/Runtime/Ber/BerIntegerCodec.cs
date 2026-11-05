namespace asn1;

internal class BerIntegerCodec
{
    internal AsnResourceLimits Limits;

    internal BerIntegerCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerPrimitiveValueResult EncodeInteger(
        System.Numerics.BigInteger value)
    {
        if (!this.FitsIntegerLimit(value))
        {
            return this.CreateIntegerError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Content = BigIntegerConversions.ToSignedBigEndian(value)
        };
    }

    internal BerPrimitiveValueResult DecodeInteger(byte[] content)
    {
        if (content == null)
        {
            throw new System.ArgumentNullException("content");
        }

        if (content.Length == 0)
        {
            return this.CreateIntegerError(
                "ASND0032",
                "INTEGER and ENUMERATED content cannot be empty.",
                false);
        }

        int maximumBytes = this.Limits.Unlimited
            ? int.MaxValue
            : this.Limits.MaximumIntegerBits / 8 + 2;
        if (content.Length > maximumBytes)
        {
            return this.CreateIntegerError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        if (content.Length > 1
            && (content[0] == 0 && (content[1] & 128) == 0
                || content[0] == 255 && (content[1] & 128) != 0))
        {
            return this.CreateIntegerError(
                "ASND0033",
                "The signed integer content contains a redundant leading octet.",
                false);
        }

        System.Numerics.BigInteger value =
            BigIntegerConversions.FromSignedBigEndian(content);
        if (!this.FitsIntegerLimit(value))
        {
            return this.CreateIntegerError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                HasIntegerValue = true,
                IntegerValue = value
            }
        };
    }

    internal bool FitsIntegerLimit(System.Numerics.BigInteger value)
    {
        System.Numerics.BigInteger remaining = System.Numerics.BigInteger.Abs(
            value);
        int bits = 0;
        while (remaining > System.Numerics.BigInteger.Zero)
        {
            bits = bits + 1;
            if (!new ResourceCounter(this.Limits).AllowsIntegerBits(bits))
            {
                return false;
            }

            remaining = remaining >> 1;
        }

        return new ResourceCounter(this.Limits).AllowsIntegerBits(bits);
    }

    internal BerPrimitiveValueResult CreateIntegerError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new BerPrimitiveValueResult
        {
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
