namespace asn1;

internal class PerCollectionOfCodec
{
    internal AsnResourceLimits Limits;
    internal PerLengthCodec Lengths;
    internal PerOpenTypeCodec OpenTypes;

    internal PerCollectionOfCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Lengths = new PerLengthCodec(limits);
        this.OpenTypes = new PerOpenTypeCodec(limits);
    }

    internal PerCollectionOfResult WriteCollection(
        PerBitWriter writer,
        byte[][] encodings,
        int[] bitCounts,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        bool aligned,
        bool canonicalSetOf)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        PerCollectionOfResult validation = this.ValidateElements(
            encodings,
            bitCounts,
            lowerBound,
            hasUpperBound,
            upperBound);
        if (!validation.Success)
        {
            return validation;
        }

        PerCollectionOfResult ordering = this.GetEncodingOrder(
            encodings,
            bitCounts,
            canonicalSetOf);
        if (!ordering.Success)
        {
            return ordering;
        }

        int start = writer.GetWrittenBitCount();
        System.Collections.Generic.List<int> fragments =
            new System.Collections.Generic.List<int>();
        if (hasUpperBound && upperBound < 65536)
        {
            PerLengthResult length = this.Lengths.WriteConstrainedLength(
                writer,
                encodings.Length,
                lowerBound,
                upperBound,
                aligned);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            fragments.Add(encodings.Length);
            PerCollectionOfResult payload = this.WriteElementRange(
                writer,
                encodings,
                bitCounts,
                ordering.Order,
                0,
                encodings.Length);
            if (!payload.Success)
            {
                return payload;
            }
        }
        else
        {
            int offset = 0;
            int remaining = encodings.Length;
            while (remaining >= 16384)
            {
                int units = remaining / 16384;
                if (units > 4)
                {
                    units = 4;
                }

                int count = units * 16384;
                PerLengthResult length = this.Lengths.WriteLengthComponent(
                    writer,
                    count,
                    aligned);
                if (!length.Success)
                {
                    return this.CopyLengthFailure(length);
                }

                fragments.Add(count);
                PerCollectionOfResult payload = this.WriteElementRange(
                    writer,
                    encodings,
                    bitCounts,
                    ordering.Order,
                    offset,
                    count);
                if (!payload.Success)
                {
                    return payload;
                }

                offset = offset + count;
                remaining = remaining - count;
            }

            PerLengthResult finalLength = this.Lengths.WriteLengthComponent(
                writer,
                remaining,
                aligned);
            if (!finalLength.Success)
            {
                return this.CopyLengthFailure(finalLength);
            }

            fragments.Add(remaining);
            PerCollectionOfResult finalPayload = this.WriteElementRange(
                writer,
                encodings,
                bitCounts,
                ordering.Order,
                offset,
                remaining);
            if (!finalPayload.Success)
            {
                return finalPayload;
            }
        }

        return new PerCollectionOfResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ElementData = this.ReorderData(encodings, ordering.Order),
            ElementBitCounts = this.ReorderCounts(bitCounts, ordering.Order),
            FragmentLengths = fragments.ToArray(),
            Order = ordering.Order,
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerCollectionOfResult ReadCollection(
        PerBitReader reader,
        int[] encodedElementBitCounts,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        bool aligned,
        bool canonicalSetOf)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        if (encodedElementBitCounts == null)
        {
            throw new System.ArgumentNullException("encodedElementBitCounts");
        }

        PerCollectionOfResult metadata = this.ValidateElementCounts(
            encodedElementBitCounts,
            lowerBound,
            hasUpperBound,
            upperBound);
        if (!metadata.Success)
        {
            return metadata;
        }

        ResourceCounter resources = new ResourceCounter(this.Limits);
        if (!resources.TryChargeAllocationBytes(
            (long)encodedElementBitCounts.Length * 8))
        {
            return this.CreateError(
                "ASNR0005",
                "Allocating the collection result exceeds the configured limit.",
                true);
        }

        int start = reader.Position;
        byte[][] data = new byte[encodedElementBitCounts.Length][];
        System.Collections.Generic.List<int> fragments =
            new System.Collections.Generic.List<int>();
        int offset = 0;
        if (hasUpperBound && upperBound < 65536)
        {
            PerLengthResult length = this.Lengths.ReadConstrainedLength(
                reader,
                lowerBound,
                upperBound,
                aligned);
            if (!length.Success)
            {
                return this.HandleLengthFailure(reader, length, start);
            }

            if (length.Length != encodedElementBitCounts.Length)
            {
                return this.CreateError(
                    "ASNP0069",
                    "The decoded collection length does not match its element metadata.",
                    false);
            }

            fragments.Add(length.Length);
            PerCollectionOfResult payload = this.ReadElementRange(
                reader,
                data,
                encodedElementBitCounts,
                0,
                length.Length,
                start,
                resources);
            if (!payload.Success)
            {
                return payload;
            }
        }
        else
        {
            bool more = true;
            while (more)
            {
                int lengthOffset = reader.Position;
                PerLengthResult length = this.Lengths.ReadLengthComponent(
                    reader,
                    aligned);
                if (!length.Success)
                {
                    return this.HandleLengthFailure(reader, length, start);
                }

                if (length.Length > encodedElementBitCounts.Length - offset)
                {
                    return this.CreateReaderError(
                        reader,
                        "ASNP0069",
                        "A collection fragment exceeds the declared element count.",
                        false,
                        lengthOffset);
                }

                int remainingExpected =
                    encodedElementBitCounts.Length - offset;
                if (length.MoreFragments)
                {
                    int expectedUnits = remainingExpected / 16384;
                    if (expectedUnits > 4)
                    {
                        expectedUnits = 4;
                    }

                    if (expectedUnits < 1
                        || length.Length != expectedUnits * 16384)
                    {
                        return this.CreateReaderError(
                            reader,
                            "ASNP0071",
                            "A collection fragment does not use the required maximum multiplier.",
                            false,
                            lengthOffset);
                    }
                }
                else if (length.Length != remainingExpected)
                {
                    return this.CreateReaderError(
                        reader,
                        "ASNP0069",
                        "The final collection fragment has the wrong element count.",
                        false,
                        lengthOffset);
                }

                fragments.Add(length.Length);
                PerCollectionOfResult payload = this.ReadElementRange(
                    reader,
                    data,
                    encodedElementBitCounts,
                    offset,
                    length.Length,
                    start,
                    resources);
                if (!payload.Success)
                {
                    return payload;
                }

                offset = offset + length.Length;
                more = length.MoreFragments;
            }

            if (offset != encodedElementBitCounts.Length)
            {
                return this.CreateReaderError(
                    reader,
                    "ASNP0069",
                    "The decoded collection length does not match its element metadata.",
                    false,
                    start);
            }
        }

        if (canonicalSetOf)
        {
            PerCollectionOfResult canonical = this.ValidateCanonicalOrder(
                data,
                encodedElementBitCounts);
            if (!canonical.Success)
            {
                return canonical;
            }
        }

        return new PerCollectionOfResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ElementData = data,
            ElementBitCounts = this.CopyCounts(encodedElementBitCounts),
            FragmentLengths = fragments.ToArray(),
            Order = this.CreateIdentityOrder(data.Length),
            BitCount = reader.Position - start
        };
    }

    internal PerCollectionOfResult GetEncodingOrder(
        byte[][] encodings,
        int[] bitCounts,
        bool canonicalSetOf)
    {
        if (!canonicalSetOf)
        {
            return new PerCollectionOfResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Order = this.CreateIdentityOrder(encodings.Length)
            };
        }

        byte[][] complete = new byte[encodings.Length][];
        int index = 0;
        while (index < complete.Length)
        {
            PerOpenTypeResult encoded = this.OpenTypes.CompleteEncoding(
                encodings[index],
                bitCounts[index]);
            if (!encoded.Success)
            {
                return this.CopyOpenTypeFailure(encoded);
            }

            complete[index] = encoded.Data;
            index = index + 1;
        }

        return new PerCollectionOfResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Order = StableOrder.GetByteArrayOrder(complete)
        };
    }

    internal PerCollectionOfResult ValidateCanonicalOrder(
        byte[][] encodings,
        int[] bitCounts)
    {
        byte[]? previous = null;
        int index = 0;
        while (index < encodings.Length)
        {
            PerOpenTypeResult complete = this.OpenTypes.CompleteEncoding(
                encodings[index],
                bitCounts[index]);
            if (!complete.Success)
            {
                return this.CopyOpenTypeFailure(complete);
            }

            if (previous != null
                && StableOrder.CompareByteArrays(previous, complete.Data) > 0)
            {
                return this.CreateError(
                    "ASNP0070",
                    "Canonical PER SET OF elements are not in ascending encoding order.",
                    false);
            }

            previous = complete.Data;
            index = index + 1;
        }

        return new PerCollectionOfResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Order = this.CreateIdentityOrder(encodings.Length)
        };
    }

    internal PerCollectionOfResult WriteElementRange(
        PerBitWriter writer,
        byte[][] encodings,
        int[] bitCounts,
        int[] order,
        int offset,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            int source = order[offset + index];
            PerBitWriteResult written = writer.WriteInputBits(
                encodings[source],
                0,
                bitCounts[source]);
            if (!written.Success)
            {
                return this.CopyWriterFailure(writer);
            }

            index = index + 1;
        }

        return new PerCollectionOfResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerCollectionOfResult ReadElementRange(
        PerBitReader reader,
        byte[][] data,
        int[] bitCounts,
        int offset,
        int count,
        int start,
        ResourceCounter resources)
    {
        int index = 0;
        while (index < count)
        {
            int target = offset + index;
            int bitCount = bitCounts[target];
            int byteCount = bitCount / 8;
            if (bitCount % 8 != 0)
            {
                byteCount = byteCount + 1;
            }

            if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(byteCount)
                || !resources.TryChargeAllocationBytes(byteCount))
            {
                return this.CreateError(
                    "ASNR0006",
                    "A collection element exceeds the configured resource limits.",
                    true);
            }

            byte[] element = new byte[byteCount];
            int bitIndex = 0;
            while (bitIndex < bitCount)
            {
                PerBitReadResult bit = reader.ReadBit();
                if (bit.Status != AsnDecodeStatus.Done)
                {
                    return this.HandleIncompleteRead(reader, bit, start);
                }

                if (bit.Value != System.Numerics.BigInteger.Zero)
                {
                    element[bitIndex / 8] = (byte)(element[bitIndex / 8]
                        | 1 << (7 - bitIndex % 8));
                }

                bitIndex = bitIndex + 1;
            }

            data[target] = element;
            index = index + 1;
        }

        return new PerCollectionOfResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerCollectionOfResult ValidateElements(
        byte[][] encodings,
        int[] bitCounts,
        int lowerBound,
        bool hasUpperBound,
        int upperBound)
    {
        if (encodings == null)
        {
            throw new System.ArgumentNullException("encodings");
        }

        if (bitCounts == null)
        {
            throw new System.ArgumentNullException("bitCounts");
        }

        if (encodings.Length != bitCounts.Length)
        {
            return this.CreateError(
                "ASNP0067",
                "Collection element encodings and bit counts do not match.",
                false);
        }

        PerCollectionOfResult counts = this.ValidateElementCounts(
            bitCounts,
            lowerBound,
            hasUpperBound,
            upperBound);
        if (!counts.Success)
        {
            return counts;
        }

        int index = 0;
        while (index < encodings.Length)
        {
            if (encodings[index] == null)
            {
                return this.CreateError(
                    "ASNP0067",
                    "A collection element encoding cannot be null.",
                    false);
            }

            int byteCount = bitCounts[index] / 8;
            if (bitCounts[index] % 8 != 0)
            {
                byteCount = byteCount + 1;
            }

            if (byteCount > encodings[index].Length)
            {
                return this.CreateError(
                    "ASNP0067",
                    "A collection element does not contain its declared bits.",
                    false);
            }

            index = index + 1;
        }

        return new PerCollectionOfResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerCollectionOfResult ValidateElementCounts(
        int[] bitCounts,
        int lowerBound,
        bool hasUpperBound,
        int upperBound)
    {
        if (lowerBound < 0 || hasUpperBound && upperBound < lowerBound)
        {
            return this.CreateError(
                "ASNP0068",
                "The collection size bounds are invalid.",
                false);
        }

        if (bitCounts.Length < lowerBound
            || hasUpperBound && bitCounts.Length > upperBound)
        {
            return this.CreateError(
                "ASNP0068",
                "The collection element count is outside its size bounds.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            bitCounts.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The collection exceeds the configured element limit.",
                true);
        }

        int index = 0;
        while (index < bitCounts.Length)
        {
            if (bitCounts[index] < 0)
            {
                return this.CreateError(
                    "ASNP0067",
                    "A collection element bit count cannot be negative.",
                    false);
            }

            index = index + 1;
        }

        return new PerCollectionOfResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal int[] CreateIdentityOrder(int count)
    {
        int[] order = new int[count];
        int index = 0;
        while (index < count)
        {
            order[index] = index;
            index = index + 1;
        }

        return order;
    }

    internal byte[][] ReorderData(byte[][] source, int[] order)
    {
        byte[][] result = new byte[order.Length][];
        int index = 0;
        while (index < order.Length)
        {
            result[index] = source[order[index]];
            index = index + 1;
        }

        return result;
    }

    internal int[] ReorderCounts(int[] source, int[] order)
    {
        int[] result = new int[order.Length];
        int index = 0;
        while (index < order.Length)
        {
            result[index] = source[order[index]];
            index = index + 1;
        }

        return result;
    }

    internal int[] CopyCounts(int[] source)
    {
        int[] result = new int[source.Length];
        if (source.Length > 0)
        {
            System.Buffer.BlockCopy(
                source,
                0,
                result,
                0,
                source.Length * 4);
        }

        return result;
    }

    internal PerCollectionOfResult HandleIncompleteRead(
        PerBitReader reader,
        PerBitReadResult input,
        int start)
    {
        if (input.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
            return new PerCollectionOfResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        return this.CopyReaderFailure(reader);
    }

    internal PerCollectionOfResult HandleLengthFailure(
        PerBitReader reader,
        PerLengthResult source,
        int start)
    {
        if (source.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
        }

        return this.CopyLengthFailure(source);
    }

    internal PerCollectionOfResult CopyLengthFailure(PerLengthResult source)
    {
        return new PerCollectionOfResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerCollectionOfResult CopyOpenTypeFailure(
        PerOpenTypeResult source)
    {
        return new PerCollectionOfResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerCollectionOfResult CopyWriterFailure(PerBitWriter writer)
    {
        return new PerCollectionOfResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = writer.LimitExceeded,
            ErrorCode = writer.ErrorCode,
            ErrorMessage = writer.ErrorMessage
        };
    }

    internal PerCollectionOfResult CopyReaderFailure(PerBitReader reader)
    {
        return new PerCollectionOfResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = reader.LimitExceeded,
            ErrorCode = reader.ErrorCode,
            ErrorMessage = reader.ErrorMessage
        };
    }

    internal PerCollectionOfResult CreateReaderError(
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

    internal PerCollectionOfResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerCollectionOfResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
