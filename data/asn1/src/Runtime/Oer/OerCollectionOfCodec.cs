namespace asn1;

internal class OerCollectionOfCodec
{
    internal AsnResourceLimits Limits;

    internal OerCollectionOfCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerCollectionResult EncodeCollectionOf(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical)
    {
        return this.EncodeCollectionOfAtDepth(
            type,
            value,
            canonical,
            0,
            AsnUnknownDataPolicy.Preserve);
    }

    internal OerCollectionResult DecodeCollectionOf(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        return this.DecodeCollectionOfAtDepth(
            type,
            data,
            offset,
            count,
            canonical,
            0,
            AsnUnknownDataPolicy.Preserve);
    }

    internal OerCollectionResult EncodeCollectionOfAtDepth(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        AsnRuntimeValueKind expected = type.Kind == AsnTypeKind.SetOf
            ? AsnRuntimeValueKind.SetOf
            : AsnRuntimeValueKind.SequenceOf;
        if (!this.AllowsDepth(depth)
            || type.Kind != AsnTypeKind.SequenceOf
                && type.Kind != AsnTypeKind.SetOf
            || value.Kind != expected
            || type.ElementType == null)
        {
            return this.CreateCollectionOfError(
                !this.AllowsDepth(depth) ? "ASNR0001" : "ASNO0078",
                !this.AllowsDepth(depth)
                    ? "The OER collection-of value exceeds the configured nesting depth."
                    : "The OER collection-of value or element type is invalid.",
                !this.AllowsDepth(depth));
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            value.Items.Length))
        {
            return this.CreateCollectionOfError(
                "ASNR0011",
                "The OER collection exceeds the configured element limit.",
                true);
        }

        OerContentResult quantity = new OerContentCodec(this.Limits)
            .EncodeVariableContent(this.EncodeUnsignedCount(
                value.Items.Length));
        if (!quantity.Success)
        {
            return this.CopyContentFailure(quantity);
        }

        System.Collections.Generic.List<byte> output =
            new System.Collections.Generic.List<byte>();
        this.AppendData(output, quantity.Data);
        byte[][] encodings = new byte[value.Items.Length][];
        int index = 0;
        while (index < value.Items.Length)
        {
            OerCollectionResult item = new OerValueCodec(this.Limits)
                .EncodeValueAtDepth(
                    type.ElementType,
                    value.Items[index],
                    canonical,
                    depth + 1,
                    policy);
            if (!item.Success)
            {
                return item;
            }

            encodings[index] = item.Data;
            index = index + 1;
        }

        int[] order = this.GetEncodingOrder(
            encodings,
            canonical && type.Kind == AsnTypeKind.SetOf);
        index = 0;
        while (index < order.Length)
        {
            this.AppendData(output, encodings[order[index]]);
            index = index + 1;
        }

        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = output.ToArray(),
            ConsumedCount = output.Count
        };
    }

    internal OerCollectionResult DecodeCollectionOfAtDepth(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        if (!this.AllowsDepth(depth)
            || type.Kind != AsnTypeKind.SequenceOf
                && type.Kind != AsnTypeKind.SetOf
            || type.ElementType == null)
        {
            return this.CreateCollectionOfError(
                !this.AllowsDepth(depth) ? "ASNR0001" : "ASNO0078",
                !this.AllowsDepth(depth)
                    ? "The OER collection-of value exceeds the configured nesting depth."
                    : "The OER collection-of type requires an element type.",
                !this.AllowsDepth(depth));
        }

        OerContentResult quantity = new OerContentCodec(this.Limits)
            .DecodeVariableContent(data, offset, count, canonical);
        if (!quantity.Success)
        {
            return this.CopyContentFailure(quantity);
        }

        OerCollectionResult countResult = this.DecodeUnsignedCount(
            quantity.Content,
            canonical);
        if (!countResult.Success)
        {
            return this.ShiftCollectionOfFailure(
                countResult,
                quantity.ConsumedCount - quantity.Content.Length);
        }

        int itemCount = countResult.ConsumedCount;
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            itemCount))
        {
            return this.CreateCollectionOfErrorAt(
                "ASNR0011",
                "The OER collection exceeds the configured element limit.",
                true,
                quantity.ConsumedCount - quantity.Content.Length);
        }

        int consumed = quantity.ConsumedCount;
        System.Collections.Generic.List<AsnRuntimeValue> items =
            new System.Collections.Generic.List<AsnRuntimeValue>();
        byte[] previousEncoding = [];
        bool hasPreviousEncoding = false;
        int index = 0;
        while (index < itemCount)
        {
            OerCollectionResult item = new OerValueCodec(this.Limits)
                .DecodeValueAtDepth(
                    type.ElementType,
                    data,
                    offset + consumed,
                    count - consumed,
                    canonical,
                    depth + 1,
                    policy);
            if (!item.Success || item.Value == null)
            {
                return this.ShiftCollectionOfFailure(item, consumed);
            }

            if (canonical && type.Kind == AsnTypeKind.SetOf)
            {
                byte[] encoding = new byte[item.ConsumedCount];
                System.Buffer.BlockCopy(
                    data,
                    offset + consumed,
                    encoding,
                    0,
                    item.ConsumedCount);
                if (hasPreviousEncoding
                    && StableOrder.CompareByteArrays(
                        previousEncoding,
                        encoding) > 0)
                {
                    return this.CreateCollectionOfErrorAt(
                        "ASNO0085",
                        "COER SET OF elements are not in ascending encoded order.",
                        false,
                        consumed);
                }

                previousEncoding = encoding;
                hasPreviousEncoding = true;
            }

            items.Add(item.Value);
            consumed = consumed + item.ConsumedCount;
            index = index + 1;
        }

        byte[] encoded = new byte[consumed];
        System.Buffer.BlockCopy(data, offset, encoded, 0, consumed);
        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = type.Kind == AsnTypeKind.SetOf
                    ? AsnRuntimeValueKind.SetOf
                    : AsnRuntimeValueKind.SequenceOf,
                Items = items.ToArray()
            },
            Data = encoded,
            ConsumedCount = consumed
        };
    }

    internal byte[] EncodeUnsignedCount(int count)
    {
        if (count == 0)
        {
            return [0];
        }

        int remaining = count;
        int octets = 0;
        while (remaining > 0)
        {
            octets = octets + 1;
            remaining = remaining >> 8;
        }

        byte[] result = new byte[octets];
        remaining = count;
        int index = result.Length - 1;
        while (index >= 0)
        {
            result[index] = (byte)(remaining & 255);
            remaining = remaining >> 8;
            index = index - 1;
        }

        return result;
    }

    internal OerCollectionResult DecodeUnsignedCount(
        byte[] content,
        bool canonical)
    {
        if (content.Length == 0)
        {
            return this.CreateCollectionOfError(
                "ASNO0079",
                "An OER quantity must contain at least one octet.",
                false);
        }

        if (canonical && content.Length > 1 && content[0] == 0)
        {
            return this.CreateCollectionOfError(
                "ASNO0084",
                "A COER collection quantity must use the shortest unsigned form.",
                false);
        }

        if (content.Length > int.MaxValue / 8
            || !new ResourceCounter(this.Limits).AllowsIntegerBits(
                content.Length * 8))
        {
            return this.CreateCollectionOfError(
                "ASNR0010",
                "The OER quantity exceeds the configured integer limit.",
                true);
        }

        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        int index = 0;
        while (index < content.Length)
        {
            value = (value << 8) | content[index];
            index = index + 1;
        }

        if (value > int.MaxValue)
        {
            return this.CreateCollectionOfError(
                "ASNR0011",
                "The OER quantity exceeds the supported element range.",
                true);
        }

        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ConsumedCount = (int)value
        };
    }

    internal int[] GetEncodingOrder(
        byte[][] encodings,
        bool canonicalSet)
    {
        if (canonicalSet)
        {
            return StableOrder.GetByteArrayOrder(encodings);
        }

        int[] order = new int[encodings.Length];
        int index = 0;
        while (index < order.Length)
        {
            order[index] = index;
            index = index + 1;
        }

        return order;
    }

    internal void AppendData(
        System.Collections.Generic.List<byte> target,
        byte[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            target.Add(data[index]);
            index = index + 1;
        }
    }

    internal bool AllowsDepth(int depth)
    {
        return this.Limits.Unlimited
            || depth <= this.Limits.MaximumNestingDepth;
    }

    internal OerCollectionResult CopyContentFailure(OerContentResult source)
    {
        return new OerCollectionResult
        {
            Status = source.Status,
            ConsumedCount = source.ConsumedCount,
            HasViolationOffset = source.HasViolationOffset,
            ViolationOffset = source.ViolationOffset,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal OerCollectionResult CreateCollectionOfError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerCollectionResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerCollectionResult CreateCollectionOfErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset)
    {
        OerCollectionResult result = this.CreateCollectionOfError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = violationOffset;
        return result;
    }

    internal OerCollectionResult ShiftCollectionOfFailure(
        OerCollectionResult result,
        int prefixCount)
    {
        if (!result.Success)
        {
            result.ConsumedCount = prefixCount + result.ConsumedCount;
            result.ViolationOffset = prefixCount
                + (result.HasViolationOffset
                    ? result.ViolationOffset
                    : 0);
            result.HasViolationOffset = true;
        }

        return result;
    }
}
