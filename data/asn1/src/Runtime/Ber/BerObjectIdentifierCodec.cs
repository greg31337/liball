namespace asn1;

internal class BerObjectIdentifierCodec
{
    internal AsnResourceLimits Limits;

    internal BerObjectIdentifierCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal BerPrimitiveValueResult EncodeObjectIdentifier(
        AsnObjectIdentifierValue value,
        bool relative)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (value.Relative != relative || !value.IsValid())
        {
            return this.CreateObjectIdentifierError(
                "ASNE0029",
                "The object identifier arcs or relative form are invalid.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            value.Arcs.Length))
        {
            return this.CreateObjectIdentifierError(
                "ASNR0011",
                "The object identifier exceeds the configured arc-count limit.",
                true);
        }

        ByteAccumulator content = new ByteAccumulator(value.Arcs.Length);
        int index = 0;
        if (!relative)
        {
            System.Numerics.BigInteger first = value.Arcs[0]
                * new System.Numerics.BigInteger(40) + value.Arcs[1];
            BerPrimitiveValueResult firstResult = this.AppendSubidentifier(
                content,
                first);
            if (!firstResult.Success)
            {
                return firstResult;
            }

            index = 2;
        }

        while (index < value.Arcs.Length)
        {
            BerPrimitiveValueResult appended = this.AppendSubidentifier(
                content,
                value.Arcs[index]);
            if (!appended.Success)
            {
                return appended;
            }

            index = index + 1;
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Content = content.ToArray()
        };
    }

    internal BerPrimitiveValueResult DecodeObjectIdentifier(
        byte[] content,
        bool relative)
    {
        if (content == null)
        {
            throw new System.ArgumentNullException("content");
        }

        if (content.Length == 0)
        {
            return this.CreateObjectIdentifierError(
                "ASND0034",
                "Object identifier content cannot be empty.",
                false);
        }

        System.Collections.Generic.List<System.Numerics.BigInteger> encodedArcs =
            new System.Collections.Generic.List<System.Numerics.BigInteger>();
        int index = 0;
        while (index < content.Length)
        {
            if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
                encodedArcs.Count + 1))
            {
                return this.CreateObjectIdentifierError(
                    "ASNR0011",
                    "The object identifier exceeds the configured arc-count limit.",
                    true);
            }

            if (content[index] == 128)
            {
                return this.CreateObjectIdentifierError(
                    "ASND0035",
                    "An object identifier subidentifier has a redundant leading group.",
                    false);
            }

            System.Numerics.BigInteger arc = System.Numerics.BigInteger.Zero;
            int bitCount = 0;
            bool hasSignificantGroup = false;
            bool ended = false;
            while (index < content.Length)
            {
                int group = content[index] & 127;
                if (!hasSignificantGroup && group != 0)
                {
                    hasSignificantGroup = true;
                    bitCount = this.GetGroupBitCount(group);
                }
                else if (hasSignificantGroup)
                {
                    if (bitCount > int.MaxValue - 7)
                    {
                        return this.CreateObjectIdentifierError(
                            "ASNR0010",
                            "An object identifier arc exceeds the supported integer range.",
                            true);
                    }

                    bitCount = bitCount + 7;
                }
                if (!new ResourceCounter(this.Limits).AllowsIntegerBits(
                    bitCount))
                {
                    return this.CreateObjectIdentifierError(
                        "ASNR0010",
                        "An object identifier arc exceeds the configured integer-bit limit.",
                        true);
                }

                arc = (arc << 7) + group;
                bool last = (content[index] & 128) == 0;
                index = index + 1;
                if (last)
                {
                    ended = true;
                    break;
                }
            }

            if (!ended)
            {
                return this.CreateObjectIdentifierError(
                    "ASND0036",
                    "The final object identifier subidentifier is truncated.",
                    false);
            }

            encodedArcs.Add(arc);
        }

        System.Collections.Generic.List<System.Numerics.BigInteger> arcs =
            new System.Collections.Generic.List<System.Numerics.BigInteger>();
        int decodedArcCount = relative
            ? encodedArcs.Count
            : encodedArcs.Count + 1;
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            decodedArcCount))
        {
            return this.CreateObjectIdentifierError(
                "ASNR0011",
                "The object identifier exceeds the configured arc-count limit.",
                true);
        }

        if (relative)
        {
            int relativeIndex = 0;
            while (relativeIndex < encodedArcs.Count)
            {
                arcs.Add(encodedArcs[relativeIndex]);
                relativeIndex = relativeIndex + 1;
            }
        }
        else
        {
            System.Numerics.BigInteger first = encodedArcs[0];
            if (first < new System.Numerics.BigInteger(40))
            {
                arcs.Add(System.Numerics.BigInteger.Zero);
                arcs.Add(first);
            }
            else if (first < new System.Numerics.BigInteger(80))
            {
                arcs.Add(System.Numerics.BigInteger.One);
                arcs.Add(first - new System.Numerics.BigInteger(40));
            }
            else
            {
                arcs.Add(new System.Numerics.BigInteger(2));
                arcs.Add(first - new System.Numerics.BigInteger(80));
            }

            int absoluteIndex = 1;
            while (absoluteIndex < encodedArcs.Count)
            {
                arcs.Add(encodedArcs[absoluteIndex]);
                absoluteIndex = absoluteIndex + 1;
            }
        }

        return new BerPrimitiveValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = relative
                    ? AsnRuntimeValueKind.RelativeObjectIdentifier
                    : AsnRuntimeValueKind.ObjectIdentifier,
                ObjectIdentifierValue = new AsnObjectIdentifierValue
                {
                    Relative = relative,
                    Arcs = arcs.ToArray()
                }
            }
        };
    }

    internal BerPrimitiveValueResult AppendSubidentifier(
        ByteAccumulator output,
        System.Numerics.BigInteger value)
    {
        if (value < System.Numerics.BigInteger.Zero)
        {
            return this.CreateObjectIdentifierError(
                "ASNE0030",
                "Object identifier arcs must be non-negative.",
                false);
        }

        if (!this.FitsArcLimit(value))
        {
            return this.CreateObjectIdentifierError(
                "ASNR0010",
                "An object identifier arc exceeds the configured integer-bit limit.",
                true);
        }

        System.Collections.Generic.List<byte> groups =
            new System.Collections.Generic.List<byte>();
        System.Numerics.BigInteger remaining = value;
        do
        {
            groups.Add((byte)(remaining & new System.Numerics.BigInteger(127)));
            remaining = remaining >> 7;
        }
        while (remaining > System.Numerics.BigInteger.Zero);

        int index = groups.Count - 1;
        while (index >= 0)
        {
            int group = groups[index];
            if (index != 0)
            {
                group = group | 128;
            }

            output.AppendByte((byte)group);
            index = index - 1;
        }

        return new BerPrimitiveValueResult { Success = true };
    }

    internal bool FitsArcLimit(System.Numerics.BigInteger value)
    {
        System.Numerics.BigInteger remaining = value;
        int bitCount = 0;
        while (remaining > System.Numerics.BigInteger.Zero)
        {
            bitCount = bitCount + 1;
            if (!new ResourceCounter(this.Limits).AllowsIntegerBits(bitCount))
            {
                return false;
            }

            remaining = remaining >> 1;
        }

        return new ResourceCounter(this.Limits).AllowsIntegerBits(bitCount);
    }

    internal int GetGroupBitCount(int value)
    {
        int bits = 0;
        int remaining = value;
        while (remaining > 0)
        {
            bits = bits + 1;
            remaining = remaining >> 1;
        }

        return bits;
    }

    internal BerPrimitiveValueResult CreateObjectIdentifierError(
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
