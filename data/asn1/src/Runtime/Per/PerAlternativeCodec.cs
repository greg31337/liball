namespace asn1;

internal class PerAlternativeCodec
{
    internal AsnResourceLimits Limits;
    internal PerPrimitiveCodec Primitives;
    internal PerWholeNumberCodec WholeNumbers;
    internal PerOpenTypeCodec OpenTypes;

    internal PerAlternativeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Primitives = new PerPrimitiveCodec(limits);
        this.WholeNumbers = new PerWholeNumberCodec(limits);
        this.OpenTypes = new PerOpenTypeCodec(limits);
    }

    internal PerAlternativeResult WriteEnumeratedIndex(
        PerBitWriter writer,
        bool extension,
        int index,
        int rootCount,
        int extensionCount,
        bool extensible,
        bool aligned)
    {
        return this.WriteIndex(
            writer,
            extension,
            index,
            rootCount,
            extensionCount,
            extensible,
            aligned);
    }

    internal PerAlternativeResult ReadEnumeratedIndex(
        PerBitReader reader,
        int rootCount,
        int extensionCount,
        bool extensible,
        bool aligned)
    {
        return this.ReadIndex(
            reader,
            rootCount,
            extensionCount,
            extensible,
            aligned);
    }

    internal PerAlternativeResult WriteChoiceAlternative(
        PerBitWriter writer,
        bool extension,
        int index,
        int rootCount,
        int extensionCount,
        bool extensible,
        byte[] encoding,
        int encodingBitCount,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        PerAlternativeResult payloadValidation = this.ValidatePayload(
            encoding,
            encodingBitCount);
        if (!payloadValidation.Success)
        {
            return payloadValidation;
        }

        int start = writer.GetWrittenBitCount();
        PerAlternativeResult indexResult = this.WriteIndex(
            writer,
            extension,
            index,
            rootCount,
            extensionCount,
            extensible,
            aligned);
        if (!indexResult.Success)
        {
            return indexResult;
        }

        if (extension)
        {
            PerOpenTypeResult openType = this.OpenTypes.WriteOpenType(
                writer,
                encoding,
                encodingBitCount,
                aligned);
            if (!openType.Success)
            {
                return this.CopyOpenTypeFailure(openType);
            }
        }
        else
        {
            PerBitWriteResult payload = writer.WriteInputBits(
                encoding,
                0,
                encodingBitCount);
            if (!payload.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        indexResult.Data = this.CopyPayload(encoding, encodingBitCount);
        indexResult.ContentBitCount = encodingBitCount;
        indexResult.BitCount = writer.GetWrittenBitCount() - start;
        return indexResult;
    }

    internal PerAlternativeResult ReadChoiceAlternative(
        PerBitReader reader,
        int rootCount,
        int extensionCount,
        bool extensible,
        int[] rootAlternativeBitCounts,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        if (rootAlternativeBitCounts == null)
        {
            throw new System.ArgumentNullException("rootAlternativeBitCounts");
        }

        if (rootAlternativeBitCounts.Length != rootCount)
        {
            return this.CreateError(
                "ASNP0065",
                "The root CHOICE payload metadata does not match the alternatives.",
                false);
        }

        int countIndex = 0;
        while (countIndex < rootAlternativeBitCounts.Length)
        {
            if (rootAlternativeBitCounts[countIndex] < 0)
            {
                return this.CreateError(
                    "ASNP0065",
                    "A root CHOICE payload bit count cannot be negative.",
                    false);
            }

            countIndex = countIndex + 1;
        }

        int start = reader.Position;
        PerAlternativeResult indexResult = this.ReadIndex(
            reader,
            rootCount,
            extensionCount,
            extensible,
            aligned);
        if (!indexResult.Success)
        {
            return indexResult;
        }

        if (indexResult.Extension)
        {
            PerOpenTypeResult openType = this.OpenTypes.ReadOpenType(
                reader,
                aligned);
            if (!openType.Success)
            {
                if (openType.Status == AsnDecodeStatus.NeedMoreData)
                {
                    reader.Position = start;
                }

                return this.CopyOpenTypeFailure(openType);
            }

            indexResult.Data = openType.Data;
            indexResult.ContentBitCount = openType.ContentBitCount;
        }
        else
        {
            PerAlternativeResult payload = this.ReadPayload(
                reader,
                rootAlternativeBitCounts[indexResult.Index],
                start);
            if (!payload.Success)
            {
                return payload;
            }

            indexResult.Data = payload.Data;
            indexResult.ContentBitCount = payload.ContentBitCount;
        }

        indexResult.BitCount = reader.Position - start;
        return indexResult;
    }

    internal PerAlternativeResult WriteIndex(
        PerBitWriter writer,
        bool extension,
        int index,
        int rootCount,
        int extensionCount,
        bool extensible,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        PerAlternativeResult validation = this.ValidateIndex(
            extension,
            index,
            rootCount,
            extensionCount,
            extensible);
        if (!validation.Success)
        {
            return validation;
        }

        int start = writer.GetWrittenBitCount();
        if (extensible)
        {
            PerBitWriteResult marker = writer.WriteBit(extension);
            if (!marker.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        if (extension)
        {
            PerWholeNumberResult extensionIndex =
                this.WholeNumbers.WriteNormallySmallWholeNumber(
                    writer,
                    new System.Numerics.BigInteger(index),
                    aligned);
            if (!extensionIndex.Success)
            {
                return this.CopyWholeNumberFailure(extensionIndex);
            }
        }
        else
        {
            PerPrimitiveResult rootIndex = this.Primitives.WriteEnumeratedIndex(
                writer,
                index,
                rootCount,
                aligned);
            if (!rootIndex.Success)
            {
                return this.CopyPrimitiveFailure(rootIndex);
            }
        }

        return new PerAlternativeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Extension = extension,
            Index = index,
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerAlternativeResult ReadIndex(
        PerBitReader reader,
        int rootCount,
        int extensionCount,
        bool extensible,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        PerAlternativeResult metadata = this.ValidateIndexMetadata(
            rootCount,
            extensionCount,
            extensible);
        if (!metadata.Success)
        {
            return metadata;
        }

        int start = reader.Position;
        bool extension = false;
        if (extensible)
        {
            PerBitReadResult marker = reader.ReadBit();
            if (marker.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, marker, start);
            }

            extension = marker.Value != System.Numerics.BigInteger.Zero;
        }

        int index;
        if (extension)
        {
            int indexStart = reader.Position;
            PerWholeNumberResult decoded =
                this.WholeNumbers.ReadNormallySmallWholeNumber(
                    reader,
                    aligned);
            if (!decoded.Success)
            {
                if (decoded.Status == AsnDecodeStatus.NeedMoreData)
                {
                    reader.Position = start;
                }

                return this.CopyWholeNumberFailure(decoded);
            }

            if (decoded.Value > new System.Numerics.BigInteger(int.MaxValue))
            {
                return this.CreateReaderError(
                    reader,
                    "ASNP0064",
                    "The extension alternative index exceeds the supported range.",
                    false,
                    indexStart);
            }

            index = (int)decoded.Value;
            if (index >= extensionCount)
            {
                return this.CreateReaderError(
                    reader,
                    "ASNP0064",
                    "The extension alternative index is not declared by the schema.",
                    false,
                    indexStart);
            }
        }
        else
        {
            PerPrimitiveResult decoded = this.Primitives.ReadEnumeratedIndex(
                reader,
                rootCount,
                aligned);
            if (!decoded.Success || decoded.Value == null)
            {
                if (decoded.Status == AsnDecodeStatus.NeedMoreData)
                {
                    reader.Position = start;
                }

                return this.CopyPrimitiveFailure(decoded);
            }

            index = (int)decoded.Value.IntegerValue;
        }

        return new PerAlternativeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Extension = extension,
            Index = index,
            BitCount = reader.Position - start
        };
    }

    internal PerAlternativeResult ValidateIndex(
        bool extension,
        int index,
        int rootCount,
        int extensionCount,
        bool extensible)
    {
        PerAlternativeResult metadata = this.ValidateIndexMetadata(
            rootCount,
            extensionCount,
            extensible);
        if (!metadata.Success)
        {
            return metadata;
        }

        if (extension && !extensible)
        {
            return this.CreateError(
                "ASNP0063",
                "A non-extensible alternative list cannot select an extension.",
                false);
        }

        int count = extension ? extensionCount : rootCount;
        if (index < 0 || index >= count)
        {
            return this.CreateError(
                "ASNP0064",
                "The selected alternative index is outside its partition.",
                false);
        }

        return new PerAlternativeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerAlternativeResult ValidateIndexMetadata(
        int rootCount,
        int extensionCount,
        bool extensible)
    {
        if (rootCount < 1 || extensionCount < 0
            || !extensible && extensionCount != 0)
        {
            return this.CreateError(
                "ASNP0063",
                "The root and extension alternative counts are invalid.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
                rootCount)
            || !new ResourceCounter(this.Limits).AllowsCollectionElements(
                extensionCount))
        {
            return this.CreateError(
                "ASNR0011",
                "The alternative count exceeds the configured element limit.",
                true);
        }

        return new PerAlternativeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerAlternativeResult ValidatePayload(byte[] data, int bitCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (bitCount < 0)
        {
            return this.CreateError(
                "ASNP0066",
                "A CHOICE payload bit count cannot be negative.",
                false);
        }

        int byteCount = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            byteCount = byteCount + 1;
        }

        if (byteCount > data.Length)
        {
            return this.CreateError(
                "ASNP0066",
                "The CHOICE payload does not contain its declared bits.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(byteCount))
        {
            return this.CreateError(
                "ASNR0006",
                "The CHOICE payload exceeds the configured byte limit.",
                true);
        }

        return new PerAlternativeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerAlternativeResult ReadPayload(
        PerBitReader reader,
        int bitCount,
        int start)
    {
        int byteCount = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            byteCount = byteCount + 1;
        }

        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(byteCount)
            || !reader.Resources.TryChargeAllocationBytes(byteCount))
        {
            return this.CreateError(
                "ASNR0006",
                "The CHOICE payload exceeds the configured resource limits.",
                true);
        }

        byte[] data = new byte[byteCount];
        int index = 0;
        while (index < bitCount)
        {
            PerBitReadResult bit = reader.ReadBit();
            if (bit.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, bit, start);
            }

            if (bit.Value != System.Numerics.BigInteger.Zero)
            {
                data[index / 8] = (byte)(data[index / 8]
                    | 1 << (7 - index % 8));
            }

            index = index + 1;
        }

        return new PerAlternativeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = data,
            ContentBitCount = bitCount
        };
    }

    internal byte[] CopyPayload(byte[] data, int bitCount)
    {
        int byteCount = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            byteCount = byteCount + 1;
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

    internal PerAlternativeResult HandleIncompleteRead(
        PerBitReader reader,
        PerBitReadResult input,
        int start)
    {
        if (input.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
            return new PerAlternativeResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        return this.CopyReaderFailure(reader);
    }

    internal PerAlternativeResult CopyPrimitiveFailure(
        PerPrimitiveResult source)
    {
        return new PerAlternativeResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerAlternativeResult CopyWholeNumberFailure(
        PerWholeNumberResult source)
    {
        return new PerAlternativeResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerAlternativeResult CopyOpenTypeFailure(
        PerOpenTypeResult source)
    {
        return new PerAlternativeResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerAlternativeResult CopyWriterFailure(PerBitWriter writer)
    {
        return new PerAlternativeResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = writer.LimitExceeded,
            ErrorCode = writer.ErrorCode,
            ErrorMessage = writer.ErrorMessage
        };
    }

    internal PerAlternativeResult CopyReaderFailure(PerBitReader reader)
    {
        return new PerAlternativeResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = reader.LimitExceeded,
            ErrorCode = reader.ErrorCode,
            ErrorMessage = reader.ErrorMessage
        };
    }

    internal PerAlternativeResult CreateReaderError(
        PerBitReader reader,
        string code,
        string message,
        bool limitExceeded,
        int bitOffset)
    {
        reader.CreateError(
            code,
            message,
            limitExceeded,
            true,
            bitOffset);
        return this.CopyReaderFailure(reader);
    }

    internal PerAlternativeResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerAlternativeResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
