namespace asn1;

internal class PerContentsCodec
{
    internal AsnResourceLimits Limits;
    internal PerPrimitiveCodec Primitives;

    internal PerContentsCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Primitives = new PerPrimitiveCodec(limits);
    }

    internal PerContentsResult WriteContents(
        PerBitWriter writer,
        byte[] encoding,
        int encodingBitCount,
        bool octetStringContainer,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (encoding == null)
        {
            throw new System.ArgumentNullException("encoding");
        }

        if (encodingBitCount < 0)
        {
            return this.CreateError(
                "ASNP0078",
                "The contained encoding bit count cannot be negative.",
                false);
        }

        int requiredBytes = encodingBitCount / 8;
        if (encodingBitCount % 8 != 0)
        {
            requiredBytes = requiredBytes + 1;
        }

        if (requiredBytes > encoding.Length)
        {
            return this.CreateError(
                "ASNP0078",
                "The contained encoding does not contain its declared bits.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
            requiredBytes))
        {
            return this.CreateError(
                "ASNR0006",
                "The contained encoding exceeds the configured byte limit.",
                true);
        }

        int start = writer.GetWrittenBitCount();
        if (octetStringContainer)
        {
            byte[] complete = this.CopyAndMask(encoding, encodingBitCount);

            PerPrimitiveResult octets = this.Primitives.WriteOctetString(
                writer,
                complete,
                0,
                false,
                0,
                aligned);
            if (!octets.Success)
            {
                return this.CopyPrimitiveFailure(octets);
            }

            return new PerContentsResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Data = complete,
                ContentBitCount = complete.Length * 8,
                BitCount = writer.GetWrittenBitCount() - start
            };
        }

        AsnBitStringValue bits = new AsnBitStringValue
        {
            Bytes = this.CopyAndMask(encoding, encodingBitCount),
            BitLength = encodingBitCount
        };
        PerPrimitiveResult bitString = this.Primitives.WriteBitString(
            writer,
            bits,
            0,
            false,
            0,
            aligned);
        if (!bitString.Success)
        {
            return this.CopyPrimitiveFailure(bitString);
        }

        return new PerContentsResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = bits.Bytes,
            ContentBitCount = bits.BitLength,
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerContentsResult ReadContents(
        PerBitReader reader,
        bool octetStringContainer,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int start = reader.Position;
        PerPrimitiveResult content = octetStringContainer
            ? this.Primitives.ReadOctetString(
                reader,
                0,
                false,
                0,
                aligned)
            : this.Primitives.ReadBitString(
                reader,
                0,
                false,
                0,
                aligned);
        if (!content.Success || content.Value == null)
        {
            if (content.Status == AsnDecodeStatus.NeedMoreData)
            {
                reader.Position = start;
            }

            return this.CopyPrimitiveFailure(content);
        }

        byte[] data;
        int bitCount;
        if (octetStringContainer)
        {
            data = content.Value.Octets;
            bitCount = data.Length * 8;
        }
        else if (content.Value.BitStringValue != null)
        {
            data = content.Value.BitStringValue.Bytes;
            bitCount = content.Value.BitStringValue.BitLength;
        }
        else
        {
            return this.CreateError(
                "ASNP0078",
                "The decoded contents container has no payload.",
                false);
        }

        return new PerContentsResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = data,
            ContentBitCount = bitCount,
            BitCount = reader.Position - start
        };
    }

    internal PerContentsResult CreateContainedValue(
        byte[] data,
        AsnEncodingRules rules,
        string resolvedTypeId,
        AsnRuntimeValue? decodedValue,
        AsnUnknownDataPolicy policy)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        AsnOpenTypeValue open;
        if (decodedValue != null && resolvedTypeId.Length > 0)
        {
            open = new AsnOpenTypeValue
            {
                Resolved = true,
                ResolvedTypeId = resolvedTypeId,
                TypeName = resolvedTypeId,
                Value = decodedValue,
                HasEncodingRules = true,
                EncodingRules = rules,
                HasOriginalData = true,
                OriginalData = this.CopyBytes(data)
            };
        }
        else
        {
            if (policy == AsnUnknownDataPolicy.Reject)
            {
                return this.CreateError(
                    "ASNP0079",
                    "The constrained contents could not be decoded as the contained type.",
                    false);
            }

            bool preserve = policy == AsnUnknownDataPolicy.Preserve;
            open = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = preserve,
                EncodingRules = rules,
                HasOriginalData = preserve,
                OriginalData = preserve ? this.CopyBytes(data) : []
            };
        }

        return new PerContentsResult
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
        if (bitCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        int byteCount = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            byteCount = byteCount + 1;
        }

        if (byteCount > data.Length)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        byte[] result = new byte[byteCount];
        if (byteCount > 0)
        {
            System.Buffer.BlockCopy(data, 0, result, 0, byteCount);
            if (bitCount % 8 != 0)
            {
                int mask = 255 << (8 - bitCount % 8);
                result[byteCount - 1] = (byte)(result[byteCount - 1] & mask);
            }
        }

        return result;
    }

    internal byte[] CopyBytes(byte[] data)
    {
        byte[] result = new byte[data.Length];
        if (data.Length > 0)
        {
            System.Buffer.BlockCopy(data, 0, result, 0, data.Length);
        }

        return result;
    }

    internal PerContentsResult CopyPrimitiveFailure(PerPrimitiveResult source)
    {
        return new PerContentsResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerContentsResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerContentsResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
