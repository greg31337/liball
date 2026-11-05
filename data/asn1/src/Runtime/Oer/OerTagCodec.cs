namespace asn1;

internal class OerTagCodec
{
    internal AsnResourceLimits Limits;

    internal OerTagCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerTagResult EncodeTag(AsnTag tag)
    {
        if (tag == null)
        {
            throw new System.ArgumentNullException("tag");
        }

        if (tag.Number < System.Numerics.BigInteger.Zero)
        {
            return this.CreateTagError(
                "ASNO0070",
                "An OER tag number cannot be negative.",
                false);
        }

        int tagClass = (int)tag.Class;
        if (tagClass < 0 || tagClass > 3)
        {
            return this.CreateTagError(
                "ASNO0070",
                "An OER tag class is outside the defined range.",
                false);
        }

        int classBits = tagClass << 6;
        if (tag.Number < 63)
        {
            if (!new ResourceCounter(this.Limits).AllowsTagOctets(1))
            {
                return this.CreateTagError(
                    "ASNR0004",
                    "The OER tag exceeds the configured tag-octet limit.",
                    true);
            }

            return this.CreateTagSuccess(
                tag,
                [(byte)(classBits | (int)tag.Number)]);
        }

        System.Collections.Generic.List<byte> digits =
            new System.Collections.Generic.List<byte>();
        System.Numerics.BigInteger remaining = tag.Number;
        while (remaining > System.Numerics.BigInteger.Zero)
        {
            digits.Add((byte)(remaining & 127));
            remaining = remaining >> 7;
        }

        int count = digits.Count + 1;
        if (!new ResourceCounter(this.Limits).AllowsTagOctets(count))
        {
            return this.CreateTagError(
                "ASNR0004",
                "The OER tag exceeds the configured tag-octet limit.",
                true);
        }

        byte[] data = new byte[count];
        data[0] = (byte)(classBits | 63);
        int index = 0;
        while (index < digits.Count)
        {
            int source = digits.Count - index - 1;
            data[index + 1] = (byte)(digits[source]
                | (index < digits.Count - 1 ? 128 : 0));
            index = index + 1;
        }

        return this.CreateTagSuccess(tag, data);
    }

    internal OerTagResult DecodeTag(
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

        if (count == 0)
        {
            return new OerTagResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                HasViolationOffset = true,
                ViolationOffset = 0
            };
        }

        int first = data[offset];
        AsnTagClass tagClass = (AsnTagClass)(first >> 6);
        int shortNumber = first & 63;
        if (shortNumber < 63)
        {
            return this.CreateTagSuccess(
                new AsnTag
                {
                    Class = tagClass,
                    Number = shortNumber
                },
                [(byte)first]);
        }

        System.Numerics.BigInteger number = System.Numerics.BigInteger.Zero;
        int consumed = 1;
        bool firstDigit = true;
        bool complete = false;
        while (consumed < count && !complete)
        {
            if (!new ResourceCounter(this.Limits).AllowsTagOctets(
                consumed + 1))
            {
                return this.CreateTagErrorAt(
                    "ASNR0004",
                    "The OER tag exceeds the configured tag-octet limit.",
                    true,
                    consumed);
            }

            int current = data[offset + consumed];
            if (firstDigit && (current & 127) == 0)
            {
                return this.CreateTagErrorAt(
                    "ASNO0071",
                    "The first subsequent OER tag octet cannot be zero.",
                    false,
                    consumed);
            }

            number = (number << 7) | (current & 127);
            consumed = consumed + 1;
            firstDigit = false;
            complete = (current & 128) == 0;
        }

        if (!complete)
        {
            return new OerTagResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = count,
                HasViolationOffset = true,
                ViolationOffset = count
            };
        }

        if (number < 63)
        {
            return this.CreateTagErrorAt(
                "ASNO0072",
                "An OER tag below 63 must use the short form.",
                false,
                0);
        }

        byte[] encoded = new byte[consumed];
        System.Buffer.BlockCopy(data, offset, encoded, 0, consumed);
        return this.CreateTagSuccess(
            new AsnTag { Class = tagClass, Number = number },
            encoded);
    }

    internal OerTagResult CreateTagSuccess(AsnTag tag, byte[] data)
    {
        return new OerTagResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Tag = tag,
            Data = data,
            ConsumedCount = data.Length
        };
    }

    internal OerTagResult CreateTagError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerTagResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerTagResult CreateTagErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset)
    {
        OerTagResult result = this.CreateTagError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = violationOffset;
        return result;
    }
}
