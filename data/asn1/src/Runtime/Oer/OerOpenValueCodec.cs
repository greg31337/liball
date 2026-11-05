namespace asn1;

internal class OerOpenValueCodec
{
    internal AsnResourceLimits Limits;

    internal OerOpenValueCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerOpenValueResult DecodeOpenValue(
        AsnTypeDescriptor? resolvedType,
        AsnRuntimeValue? dispatchKey,
        byte[] data,
        int offset,
        int count,
        AsnUnknownDataPolicy policy,
        bool canonical)
    {
        OerContentResult open = new OerOpenTypeCodec(this.Limits)
            .DecodeOpenType(data, offset, count, canonical);
        if (!open.Success)
        {
            return this.CopyContentFailure(open);
        }

        AsnEncodingRules rules = canonical
            ? AsnEncodingRules.Coer
            : AsnEncodingRules.Oer;
        if (resolvedType != null)
        {
            OerCollectionResult decoded = new OerValueCodec(this.Limits)
                .DecodeValueAtDepth(
                    resolvedType,
                    open.Content,
                    0,
                    open.Content.Length,
                    canonical,
                    0,
                    policy);
            if (!decoded.Success || decoded.Value == null)
            {
                return this.ShiftOpenValueFailure(
                    this.CopyValueFailure(decoded),
                    open.ConsumedCount - open.Content.Length);
            }

            if (decoded.ConsumedCount != open.Content.Length)
            {
                return this.CreateOpenValueErrorAt(
                    "ASNO0090",
                    "The resolved OER open value contains trailing data.",
                    false,
                    open.ConsumedCount - open.Content.Length
                        + decoded.ConsumedCount);
            }

            string identity = resolvedType.StableId.Length > 0
                ? resolvedType.StableId
                : resolvedType.Name;
            return new OerOpenValueResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                ResolvedType = resolvedType,
                OpenValue = new AsnOpenTypeValue
                {
                    Resolved = true,
                    ResolvedTypeId = identity,
                    TypeName = resolvedType.Name,
                    Value = decoded.Value,
                    DispatchKey = dispatchKey,
                    HasEncodingRules = true,
                    EncodingRules = rules,
                    HasOriginalData = true,
                    OriginalData = this.CopyBytes(open.Content),
                    HasEncodedLocation = true,
                    EncodedOffset = offset + open.ConsumedCount
                        - open.Content.Length,
                    EncodedLength = open.Content.Length
                },
                Data = open.Data,
                ConsumedCount = open.ConsumedCount
            };
        }

        if (policy == AsnUnknownDataPolicy.Reject)
        {
            return this.CreateOpenValueErrorAt(
                "ASNO0091",
                "An unresolved OER open value is not permitted by policy.",
                false,
                open.ConsumedCount - open.Content.Length);
        }

        bool preserve = policy == AsnUnknownDataPolicy.Preserve;
        AsnOpenTypeValue unresolved = new AsnOpenTypeValue
        {
            Unresolved = true,
            DispatchKey = dispatchKey,
            HasEncodingRules = preserve,
            EncodingRules = rules,
            HasOriginalData = preserve,
            OriginalData = preserve ? this.CopyBytes(open.Content) : []
        };
        if (preserve)
        {
            unresolved.HasEncodedLocation = true;
            unresolved.EncodedOffset = offset + open.ConsumedCount
                - open.Content.Length;
            unresolved.EncodedLength = open.Content.Length;
        }

        return new OerOpenValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            OpenValue = unresolved,
            Data = open.Data,
            ConsumedCount = open.ConsumedCount
        };
    }

    internal OerOpenValueResult EncodeOpenValue(
        AsnOpenTypeValue open,
        AsnTypeDescriptor? resolvedType,
        AsnUnknownDataPolicy policy,
        bool canonical)
    {
        if (open == null || !open.IsValid())
        {
            return this.CreateOpenValueError(
                "ASNO0092",
                "The OER open value is inconsistent.",
                false);
        }

        AsnEncodingRules rules = canonical
            ? AsnEncodingRules.Coer
            : AsnEncodingRules.Oer;
        byte[] payload;
        if (open.Unresolved)
        {
            if (policy != AsnUnknownDataPolicy.Preserve
                || !new OpaquePayloadPolicy().CanReuseOpenType(
                    open,
                    rules))
            {
                return this.CreateOpenValueError(
                    "ASNO0093",
                    "The unresolved OER open value has no compatible preserved payload.",
                    false);
            }

            payload = open.OriginalData;
        }
        else
        {
            if (resolvedType == null || open.Value == null
                || !this.TypeMatches(open, resolvedType))
            {
                return this.CreateOpenValueError(
                    "ASNO0094",
                    "The resolved OER open value does not match its selected type.",
                    false);
            }

            OerCollectionResult encoded = new OerValueCodec(this.Limits)
                .EncodeValueAtDepth(
                    resolvedType,
                    open.Value,
                    canonical,
                    0,
                    policy);
            if (!encoded.Success)
            {
                return this.CopyValueFailure(encoded);
            }

            payload = encoded.Data;
        }

        OerContentResult wrapped = new OerOpenTypeCodec(this.Limits)
            .EncodeOpenType(payload);
        if (!wrapped.Success)
        {
            return this.CopyContentFailure(wrapped);
        }

        return new OerOpenValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ResolvedType = resolvedType,
            OpenValue = open,
            Data = wrapped.Data,
            ConsumedCount = wrapped.ConsumedCount
        };
    }

    internal bool TypeMatches(
        AsnOpenTypeValue open,
        AsnTypeDescriptor type)
    {
        string identity = open.GetResolvedTypeIdentity();
        return identity == type.StableId
            || identity == type.Name
            || identity.EndsWith(
                "." + type.Name,
                System.StringComparison.Ordinal)
            || open.TypeName == type.Name;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Buffer.BlockCopy(source, 0, result, 0, source.Length);
        return result;
    }

    internal OerOpenValueResult CopyContentFailure(OerContentResult source)
    {
        return new OerOpenValueResult
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

    internal OerOpenValueResult CopyValueFailure(OerCollectionResult source)
    {
        return new OerOpenValueResult
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

    internal OerOpenValueResult CreateOpenValueError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerOpenValueResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerOpenValueResult CreateOpenValueErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset)
    {
        OerOpenValueResult result = this.CreateOpenValueError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = violationOffset;
        return result;
    }

    internal OerOpenValueResult ShiftOpenValueFailure(
        OerOpenValueResult result,
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
