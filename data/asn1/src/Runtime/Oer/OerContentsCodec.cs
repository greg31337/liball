namespace asn1;

internal class OerContentsCodec
{
    internal AsnResourceLimits Limits;

    internal OerContentsCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerContentsResult EncodeContents(
        byte[] encoding,
        int encodingBitCount,
        bool octetStringContainer)
    {
        if (encoding == null)
        {
            throw new System.ArgumentNullException("encoding");
        }

        int requiredBytes = encodingBitCount < 0
            ? 0
            : encodingBitCount / 8
                + (encodingBitCount % 8 == 0 ? 0 : 1);
        if (encodingBitCount < 0
            || requiredBytes > encoding.Length
            || octetStringContainer && encodingBitCount % 8 != 0)
        {
            return this.CreateContentsError(
                "ASNO0080",
                "The contained OER encoding does not fit the selected string container.",
                false);
        }

        int byteCount = requiredBytes;
        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(byteCount))
        {
            return this.CreateContentsError(
                "ASNR0020",
                "The contained OER encoding exceeds the configured byte limit.",
                true);
        }

        byte[] content = this.CopyAndMask(encoding, encodingBitCount);
        AsnTypeDescriptor container = new AsnTypeDescriptor
        {
            Kind = octetStringContainer
                ? AsnTypeKind.OctetString
                : AsnTypeKind.BitString
        };
        AsnRuntimeValue value = octetStringContainer
            ? new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = content
            }
            : new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = content,
                    BitLength = encodingBitCount
                }
            };
        OerPrimitiveResult encoded = new OerPrimitiveCodec(this.Limits)
            .EncodePrimitive(container, value, false);
        if (!encoded.Success)
        {
            return this.CopyPrimitiveFailure(encoded);
        }

        return new OerContentsResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = encoded.Data,
            ContentBitCount = encodingBitCount,
            ConsumedCount = encoded.ConsumedCount
        };
    }

    internal OerContentsResult DecodeContents(
        byte[] data,
        int offset,
        int count,
        bool octetStringContainer,
        bool canonical)
    {
        AsnTypeDescriptor container = new AsnTypeDescriptor
        {
            Kind = octetStringContainer
                ? AsnTypeKind.OctetString
                : AsnTypeKind.BitString
        };
        OerPrimitiveResult decoded = new OerPrimitiveCodec(this.Limits)
            .DecodePrimitive(
                container,
                data,
                offset,
                count,
                canonical);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CopyPrimitiveFailure(decoded);
        }

        byte[] content;
        int bitCount;
        if (octetStringContainer)
        {
            content = decoded.Value.Octets;
            bitCount = content.Length * 8;
        }
        else if (decoded.Value.BitStringValue != null)
        {
            content = decoded.Value.BitStringValue.Bytes;
            bitCount = decoded.Value.BitStringValue.BitLength;
        }
        else
        {
            return this.CreateContentsError(
                "ASNO0081",
                "The decoded OER contents container has no payload.",
                false);
        }

        return new OerContentsResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = content,
            ContentBitCount = bitCount,
            ConsumedCount = decoded.ConsumedCount
        };
    }

    internal OerContentsResult CreateContainedValue(
        byte[] data,
        AsnEncodingRules rules,
        string resolvedTypeId,
        AsnRuntimeValue? decodedValue)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        AsnOpenTypeValue open = decodedValue != null
            && resolvedTypeId.Length > 0
            ? new AsnOpenTypeValue
            {
                Resolved = true,
                ResolvedTypeId = resolvedTypeId,
                TypeName = resolvedTypeId,
                Value = decodedValue,
                HasEncodingRules = true,
                EncodingRules = rules,
                HasOriginalData = true,
                OriginalData = this.CopyBytes(data)
            }
            : new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = true,
                EncodingRules = rules,
                HasOriginalData = true,
                OriginalData = this.CopyBytes(data)
            };
        return new OerContentsResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = this.CopyBytes(data),
            ContentBitCount = data.Length * 8,
            OpenValue = open
        };
    }

    internal byte[] CopyAndMask(byte[] data, int bitCount)
    {
        int byteCount = (bitCount + 7) / 8;
        byte[] result = new byte[byteCount];
        if (byteCount > 0)
        {
            System.Buffer.BlockCopy(data, 0, result, 0, byteCount);
            if (bitCount % 8 != 0)
            {
                result[byteCount - 1] = (byte)(result[byteCount - 1]
                    & 255 << (8 - bitCount % 8));
            }
        }

        return result;
    }

    internal byte[] CopyBytes(byte[] data)
    {
        byte[] result = new byte[data.Length];
        System.Buffer.BlockCopy(data, 0, result, 0, data.Length);
        return result;
    }

    internal OerContentsResult CopyPrimitiveFailure(OerPrimitiveResult source)
    {
        return new OerContentsResult
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

    internal OerContentsResult CreateContentsError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerContentsResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
