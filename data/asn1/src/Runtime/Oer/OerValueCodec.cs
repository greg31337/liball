namespace asn1;

internal class OerValueCodec
{
    internal AsnResourceLimits Limits;

    internal OerValueCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerCollectionResult EncodeValueAtDepth(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        if (type.Kind == AsnTypeKind.Sequence || type.Kind == AsnTypeKind.Set)
        {
            return new OerCollectionCodec(this.Limits)
                .EncodeCollectionAtDepth(
                    type,
                    value,
                    canonical,
                    depth,
                    policy);
        }

        if (type.Kind == AsnTypeKind.Choice)
        {
            return new OerAlternativeCodec(this.Limits).EncodeChoiceAtDepth(
                type,
                value,
                canonical,
                depth,
                policy);
        }

        if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            return new OerCollectionOfCodec(this.Limits)
                .EncodeCollectionOfAtDepth(
                    type,
                    value,
                    canonical,
                    depth,
                    policy);
        }

        if (type.Kind == AsnTypeKind.External
            || type.Kind == AsnTypeKind.EmbeddedPdv
            || type.Kind == AsnTypeKind.CharacterString
            || type.Kind == AsnTypeKind.InstanceOf)
        {
            return new OerUsefulTypeCodec(this.Limits).EncodeUsefulAtDepth(
                type.Kind,
                value,
                canonical,
                depth,
                policy);
        }

        if (type.Kind == AsnTypeKind.OpenType)
        {
            if (value.Kind != AsnRuntimeValueKind.OpenType
                || value.OpenTypeValue == null)
            {
                return new OerCollectionResult
                {
                    Status = AsnDecodeStatus.Error,
                    ErrorCode = "ASNO0099",
                    ErrorMessage = "An OER open type requires an open runtime value."
                };
            }

            OerOpenValueResult open = new OerOpenValueCodec(this.Limits)
                .EncodeOpenValue(
                    value.OpenTypeValue,
                    null,
                    policy,
                    canonical);
            OerCollectionResult result = this.CopyOpenValueResult(open);
            if (result.Success)
            {
                result.Value = value;
            }

            return result;
        }

        OerPrimitiveResult primitive = new OerPrimitiveCodec(this.Limits)
            .EncodePrimitive(type, value, canonical);
        return this.CopyPrimitiveValue(primitive);
    }

    internal OerCollectionResult DecodeValueAtDepth(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        if (type.Kind == AsnTypeKind.Sequence || type.Kind == AsnTypeKind.Set)
        {
            return new OerCollectionCodec(this.Limits)
                .DecodeCollectionAtDepth(
                    type,
                    data,
                    offset,
                    count,
                    canonical,
                    depth,
                    policy);
        }

        if (type.Kind == AsnTypeKind.Choice)
        {
            return new OerAlternativeCodec(this.Limits).DecodeChoiceAtDepth(
                type,
                data,
                offset,
                count,
                canonical,
                depth,
                policy);
        }

        if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            return new OerCollectionOfCodec(this.Limits)
                .DecodeCollectionOfAtDepth(
                    type,
                    data,
                    offset,
                    count,
                    canonical,
                    depth,
                    policy);
        }

        if (type.Kind == AsnTypeKind.External
            || type.Kind == AsnTypeKind.EmbeddedPdv
            || type.Kind == AsnTypeKind.CharacterString
            || type.Kind == AsnTypeKind.InstanceOf)
        {
            return new OerUsefulTypeCodec(this.Limits).DecodeUsefulAtDepth(
                type.Kind,
                data,
                offset,
                count,
                canonical,
                depth,
                policy);
        }

        if (type.Kind == AsnTypeKind.OpenType)
        {
            OerOpenValueResult open = new OerOpenValueCodec(this.Limits)
                .DecodeOpenValue(
                    null,
                    null,
                    data,
                    offset,
                    count,
                    policy,
                    canonical);
            OerCollectionResult result = this.CopyOpenValueResult(open);
            if (result.Success)
            {
                result.Value = new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OpenType,
                    OpenTypeValue = open.OpenValue
                };
            }

            return result;
        }

        OerPrimitiveResult primitive = new OerPrimitiveCodec(this.Limits)
            .DecodePrimitive(type, data, offset, count, canonical);
        return this.CopyPrimitiveValue(primitive);
    }

    internal OerCollectionResult CopyPrimitiveValue(OerPrimitiveResult source)
    {
        return new OerCollectionResult
        {
            Success = source.Success,
            Status = source.Status,
            Value = source.Value,
            Data = source.Data,
            ConsumedCount = source.ConsumedCount,
            HasViolationOffset = source.HasViolationOffset,
            ViolationOffset = source.ViolationOffset,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal OerCollectionResult CopyOpenValueResult(
        OerOpenValueResult source)
    {
        return new OerCollectionResult
        {
            Success = source.Success,
            Status = source.Status,
            Data = source.Data,
            ConsumedCount = source.ConsumedCount,
            HasViolationOffset = source.HasViolationOffset,
            ViolationOffset = source.ViolationOffset,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }
}
