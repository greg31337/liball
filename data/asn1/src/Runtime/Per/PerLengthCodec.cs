namespace asn1;

internal class PerLengthCodec
{
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;
    internal PerWholeNumberCodec WholeNumbers;

    internal PerLengthCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Resources = new ResourceCounter(limits);
        this.WholeNumbers = new PerWholeNumberCodec(limits);
    }

    internal PerLengthResult WriteConstrainedLength(
        PerBitWriter writer,
        int length,
        int lowerBound,
        int upperBound,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        PerLengthResult validation =
            this.ValidateConstrainedLength(length, lowerBound, upperBound);
        if (!validation.Success)
        {
            return validation;
        }

        if (upperBound >= 65536)
        {
            return this.WriteLengthComponent(writer, length, aligned);
        }

        PerWholeNumberResult field =
            this.WholeNumbers.EncodeConstrainedWholeNumber(
                new System.Numerics.BigInteger(length),
                new System.Numerics.BigInteger(lowerBound),
                new System.Numerics.BigInteger(upperBound),
                aligned);
        if (!field.Success)
        {
            return this.CopyWholeNumberError(field);
        }

        int padding = field.OctetAligned && field.BitCount > 0
            ? (8 - writer.GetWrittenBitCount() % 8) % 8
            : 0;
        if (field.BitCount > int.MaxValue - padding
            || !writer.PrepareAdditionalBits(field.BitCount + padding))
        {
            return this.CopyWriterFailure(writer);
        }

        this.WriteZeroBits(writer, padding);
        this.WriteFieldBits(writer, field.Data, field.BitCount);
        return new PerLengthResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Length = length,
            FragmentLength = length,
            DeterminantOmitted = field.BitCount == 0,
            BitCount = field.BitCount + padding,
            PaddingBitCount = padding
        };
    }

    internal PerLengthResult ReadConstrainedLength(
        PerBitReader reader,
        int lowerBound,
        int upperBound,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        PerLengthResult validation = this.ValidateConstrainedLength(
            lowerBound,
            lowerBound,
            upperBound);
        if (!validation.Success)
        {
            return validation;
        }

        int start = reader.Position;
        if (upperBound >= 65536)
        {
            PerLengthResult component =
                this.ReadLengthComponent(reader, aligned);
            if (component.Success && !component.MoreFragments
                && (component.Length < lowerBound
                    || component.Length > upperBound))
            {
                return this.CreateReaderError(
                    reader,
                    "ASNP0031",
                    "The decoded length is outside the constrained size range.",
                    false,
                    start);
            }

            return component;
        }

        int range = upperBound - lowerBound + 1;
        if (range == 1)
        {
            return new PerLengthResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Length = lowerBound,
                FragmentLength = lowerBound,
                DeterminantOmitted = true
            };
        }

        int fieldBits = this.GetRangeBitCount(range);
        bool octetAligned = false;
        if (aligned && range == 256)
        {
            fieldBits = 8;
            octetAligned = true;
        }
        else if (aligned && range > 256)
        {
            fieldBits = 16;
            octetAligned = true;
        }

        int padding = 0;
        if (octetAligned)
        {
            padding = (8 - reader.Position % 8) % 8;
            PerBitReadResult alignment = reader.AlignInput(true);
            if (alignment.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, alignment, start);
            }
        }

        PerBitReadResult input = reader.ReadBits(fieldBits);
        if (input.Status != AsnDecodeStatus.Done)
        {
            return this.HandleIncompleteRead(reader, input, start);
        }

        if (input.Value >= new System.Numerics.BigInteger(range))
        {
            return this.CreateReaderError(
                reader,
                "ASNP0032",
                "The constrained length uses an unused range value.",
                false,
                start);
        }

        int length = lowerBound + (int)input.Value;
        return new PerLengthResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Length = length,
            FragmentLength = length,
            BitCount = reader.Position - start,
            PaddingBitCount = padding
        };
    }

    internal PerLengthResult WriteNormallySmallLength(
        PerBitWriter writer,
        int length,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (length < 1)
        {
            return this.CreateError(
                "ASNP0033",
                "A normally-small length must be at least one.",
                false);
        }

        if (length <= 64)
        {
            if (!writer.PrepareAdditionalBits(7))
            {
                return this.CopyWriterFailure(writer);
            }

            writer.WriteUncheckedBit(false);
            this.WriteUncheckedInteger(writer, length - 1, 6);
            return new PerLengthResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Length = length,
                FragmentLength = length,
                NormallySmall = true,
                BitCount = 7
            };
        }

        int padding = aligned
            ? (8 - (writer.GetWrittenBitCount() + 1) % 8) % 8
            : 0;
        int headerBits = length < 128 ? 8
            : length < 16384 ? 16 : 8;
        int totalBits = 1 + padding + headerBits;
        if (!writer.PrepareAdditionalBits(totalBits))
        {
            return this.CopyWriterFailure(writer);
        }

        writer.WriteUncheckedBit(true);
        this.WriteZeroBits(writer, padding);
        PerLengthResult component =
            this.WriteUncheckedLengthComponent(writer, length);
        component.NormallySmall = true;
        component.BitCount = totalBits;
        component.PaddingBitCount = padding;
        return component;
    }

    internal PerLengthResult ReadNormallySmallLength(
        PerBitReader reader,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int start = reader.Position;
        PerBitReadResult indicator = reader.ReadBit();
        if (indicator.Status != AsnDecodeStatus.Done)
        {
            return this.HandleIncompleteRead(reader, indicator, start);
        }

        if (indicator.Value == System.Numerics.BigInteger.Zero)
        {
            PerBitReadResult small = reader.ReadBits(6);
            if (small.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, small, start);
            }

            int length = (int)small.Value + 1;
            return new PerLengthResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Length = length,
                FragmentLength = length,
                NormallySmall = true,
                BitCount = reader.Position - start
            };
        }

        PerLengthResult component = this.ReadLengthComponent(reader, aligned);
        if (component.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
            return component;
        }

        if (!component.Success)
        {
            return component;
        }

        if (!component.MoreFragments && component.Length <= 64)
        {
            return this.CreateReaderError(
                reader,
                "ASNP0034",
                "The large normally-small length form encodes a value below 65.",
                false,
                start);
        }

        component.NormallySmall = true;
        component.BitCount = reader.Position - start;
        return component;
    }

    internal PerLengthResult WriteLengthComponent(
        PerBitWriter writer,
        int length,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (length < 0)
        {
            return this.CreateError(
                "ASNP0035",
                "A length determinant cannot be negative.",
                false);
        }

        int padding = aligned
            ? (8 - writer.GetWrittenBitCount() % 8) % 8
            : 0;
        int headerBits = length < 128 ? 8
            : length < 16384 ? 16 : 8;
        if (!writer.PrepareAdditionalBits(padding + headerBits))
        {
            return this.CopyWriterFailure(writer);
        }

        this.WriteZeroBits(writer, padding);
        PerLengthResult result =
            this.WriteUncheckedLengthComponent(writer, length);
        result.BitCount = padding + headerBits;
        result.PaddingBitCount = padding;
        return result;
    }

    internal PerLengthResult ReadLengthComponent(
        PerBitReader reader,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        if (reader.Failed)
        {
            return this.CopyReaderFailure(reader);
        }

        int start = reader.Position;
        int padding = 0;
        if (aligned)
        {
            padding = (8 - reader.Position % 8) % 8;
            PerBitReadResult alignment = reader.AlignInput(true);
            if (alignment.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, alignment, start);
            }
        }

        PerBitReadResult firstInput = reader.ReadBits(8);
        if (firstInput.Status != AsnDecodeStatus.Done)
        {
            return this.HandleIncompleteRead(reader, firstInput, start);
        }

        int first = (int)firstInput.Value;
        if ((first & 128) == 0)
        {
            return this.CreateReadSuccess(
                first,
                false,
                reader.Position - start,
                padding);
        }

        if ((first & 64) == 0)
        {
            PerBitReadResult secondInput = reader.ReadBits(8);
            if (secondInput.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, secondInput, start);
            }

            int length = (first & 63) * 256 + (int)secondInput.Value;
            if (length < 128)
            {
                return this.CreateReaderError(
                    reader,
                    "ASNP0036",
                    "The two-octet length determinant is not minimal.",
                    false,
                    start);
            }

            return this.CreateReadSuccess(
                length,
                false,
                reader.Position - start,
                padding);
        }

        int units = first & 63;
        if (units < 1 || units > 4)
        {
            return this.CreateReaderError(
                reader,
                "ASNP0037",
                "The fragment multiplier must be between one and four.",
                false,
                start);
        }

        return this.CreateReadSuccess(
            units * 16384,
            true,
            reader.Position - start,
            padding);
    }

    internal PerLengthResult WriteFragmentedOctets(
        PerBitWriter writer,
        byte[] data,
        int offset,
        int count,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (!writer.IsStreaming()
            && !new ResourceCounter(this.Limits).AllowsOpenTypeBytes(count))
        {
            return this.CreateError(
                "ASNR0006",
                "The fragmented payload exceeds the configured byte limit.",
                true);
        }

        int padding = aligned
            ? (8 - writer.GetWrittenBitCount() % 8) % 8
            : 0;
        int headerOctets = this.GetFragmentHeaderOctets(count);
        if (count > (int.MaxValue - padding) / 8
            || headerOctets > (int.MaxValue - padding - count * 8) / 8)
        {
            return this.CreateError(
                "ASNR0008",
                "The fragmented payload exceeds the supported bit range.",
                true);
        }

        int totalBits = padding + count * 8 + headerOctets * 8;
        if (!writer.PrepareAdditionalBits(totalBits))
        {
            return this.CopyWriterFailure(writer);
        }

        this.WriteZeroBits(writer, padding);
        int sourceOffset = offset;
        int remaining = count;
        while (remaining >= 16384)
        {
            int units = remaining / 16384;
            if (units > 4)
            {
                units = 4;
            }

            this.WriteUncheckedOctet(writer, 192 | units);
            int fragmentLength = units * 16384;
            this.WriteUncheckedOctets(
                writer,
                data,
                sourceOffset,
                fragmentLength);
            sourceOffset = sourceOffset + fragmentLength;
            remaining = remaining - fragmentLength;
        }

        this.WriteUncheckedFinalLength(writer, remaining);
        this.WriteUncheckedOctets(writer, data, sourceOffset, remaining);
        return new PerLengthResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Length = count,
            FragmentLength = count,
            BitCount = totalBits,
            PaddingBitCount = padding
        };
    }

    internal PerLengthResult ReadFragmentedOctets(
        PerBitReader reader,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        if (reader.Failed)
        {
            return this.CopyReaderFailure(reader);
        }

        int start = reader.Position;
        int total = 0;
        bool more = true;
        bool first = true;
        int padding = 0;
        while (more)
        {
            PerLengthResult component =
                this.ReadLengthComponent(reader, aligned);
            if (component.Status == AsnDecodeStatus.NeedMoreData)
            {
                reader.Position = start;
                return component;
            }

            if (!component.Success)
            {
                return component;
            }

            if (first)
            {
                padding = component.PaddingBitCount;
                first = false;
            }

            if (component.Length > int.MaxValue - total)
            {
                return this.CreateReaderError(
                    reader,
                    "ASNR0008",
                    "The reassembled payload length exceeds the supported range.",
                    true,
                    start);
            }

            total = total + component.Length;
            if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(total))
            {
                return this.CreateReaderError(
                    reader,
                    "ASNR0006",
                    "The reassembled payload exceeds the configured byte limit.",
                    true,
                    start);
            }

            int index = 0;
            while (index < component.Length)
            {
                PerBitReadResult octet = reader.ReadBits(8);
                if (octet.Status != AsnDecodeStatus.Done)
                {
                    return this.HandleIncompleteRead(reader, octet, start);
                }

                index = index + 1;
            }

            more = component.MoreFragments;
        }

        int end = reader.Position;
        reader.Position = start;
        int validatedOffset = 0;
        more = true;
        while (more)
        {
            int componentStart = reader.Position;
            PerLengthResult component =
                this.ReadLengthComponent(reader, aligned);
            int remainingTotal = total - validatedOffset;
            if (component.MoreFragments)
            {
                int expectedUnits = remainingTotal / 16384;
                if (expectedUnits > 4)
                {
                    expectedUnits = 4;
                }

                if (expectedUnits < 1
                    || component.Length != expectedUnits * 16384)
                {
                    return this.CreateReaderError(
                        reader,
                        "ASNP0038",
                        "The fragment does not use the required maximum multiplier.",
                        false,
                        componentStart);
                }
            }
            else if (component.Length != remainingTotal)
            {
                return this.CreateReaderError(
                    reader,
                    "ASNP0039",
                    "The final fragment length does not match the reassembled payload.",
                    false,
                    componentStart);
            }

            int index = 0;
            while (index < component.Length)
            {
                reader.ReadBits(8);
                validatedOffset = validatedOffset + 1;
                index = index + 1;
            }

            more = component.MoreFragments;
        }

        if (!this.Resources.TryChargeAllocationBytes(total))
        {
            return this.CreateReaderError(
                reader,
                "ASNR0005",
                "Reassembling the payload exceeds the allocation limit.",
                true,
                start);
        }

        byte[] data = new byte[total];
        reader.Position = start;
        int outputOffset = 0;
        more = true;
        while (more)
        {
            PerLengthResult component =
                this.ReadLengthComponent(reader, aligned);
            int index = 0;
            while (index < component.Length)
            {
                PerBitReadResult octet = reader.ReadBits(8);
                data[outputOffset] = (byte)octet.Value;
                outputOffset = outputOffset + 1;
                index = index + 1;
            }

            more = component.MoreFragments;
        }

        return new PerLengthResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Length = total,
            FragmentLength = total,
            Data = data,
            BitCount = end - start,
            PaddingBitCount = padding
        };
    }

    internal PerLengthResult ValidateConstrainedLength(
        int length,
        int lowerBound,
        int upperBound)
    {
        if (lowerBound < 0 || upperBound < lowerBound)
        {
            return this.CreateError(
                "ASNP0028",
                "The constrained size bounds are invalid.",
                false);
        }

        if (length < lowerBound || length > upperBound)
        {
            return this.CreateError(
                "ASNP0029",
                "The length is outside the constrained size range.",
                false);
        }

        return new PerLengthResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Length = length
        };
    }

    internal int GetRangeBitCount(int range)
    {
        int maximumOffset = range - 1;
        int count = 0;
        while (maximumOffset > 0)
        {
            count = count + 1;
            maximumOffset = maximumOffset >> 1;
        }

        return count;
    }

    internal int GetFragmentHeaderOctets(int length)
    {
        int remaining = length;
        int count = 0;
        while (remaining >= 16384)
        {
            int units = remaining / 16384;
            if (units > 4)
            {
                units = 4;
            }

            remaining = remaining - units * 16384;
            count = count + 1;
        }

        return count + (remaining <= 127 ? 1 : 2);
    }

    internal PerLengthResult WriteUncheckedLengthComponent(
        PerBitWriter writer,
        int length)
    {
        if (length < 128)
        {
            this.WriteUncheckedOctet(writer, length);
            return this.CreateWriteSuccess(length, false);
        }

        if (length < 16384)
        {
            this.WriteUncheckedOctet(writer, 128 | length >> 8);
            this.WriteUncheckedOctet(writer, length & 255);
            return this.CreateWriteSuccess(length, false);
        }

        int units = length / 16384;
        if (units > 4)
        {
            units = 4;
        }

        int fragmentLength = units * 16384;
        this.WriteUncheckedOctet(writer, 192 | units);
        return this.CreateWriteSuccess(fragmentLength, true);
    }

    internal void WriteUncheckedFinalLength(PerBitWriter writer, int length)
    {
        if (length < 128)
        {
            this.WriteUncheckedOctet(writer, length);
        }
        else
        {
            this.WriteUncheckedOctet(writer, 128 | length >> 8);
            this.WriteUncheckedOctet(writer, length & 255);
        }
    }

    internal void WriteFieldBits(
        PerBitWriter writer,
        byte[] data,
        int bitCount)
    {
        int index = 0;
        while (index < bitCount)
        {
            int shift = 7 - index % 8;
            writer.WriteUncheckedBit(((data[index / 8] >> shift) & 1) != 0);
            index = index + 1;
        }
    }

    internal void WriteZeroBits(PerBitWriter writer, int count)
    {
        int index = 0;
        while (index < count)
        {
            writer.WriteUncheckedBit(false);
            index = index + 1;
        }
    }

    internal void WriteUncheckedInteger(
        PerBitWriter writer,
        int value,
        int bitCount)
    {
        int index = bitCount - 1;
        while (index >= 0)
        {
            writer.WriteUncheckedBit(((value >> index) & 1) != 0);
            index = index - 1;
        }
    }

    internal void WriteUncheckedOctet(PerBitWriter writer, int value)
    {
        this.WriteUncheckedInteger(writer, value, 8);
    }

    internal void WriteUncheckedOctets(
        PerBitWriter writer,
        byte[] data,
        int offset,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            this.WriteUncheckedOctet(writer, data[offset + index]);
            index = index + 1;
        }
    }

    internal PerLengthResult HandleIncompleteRead(
        PerBitReader reader,
        PerBitReadResult input,
        int start)
    {
        if (input.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
            return new PerLengthResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        return this.CopyReaderFailure(reader);
    }

    internal PerLengthResult CreateReaderError(
        PerBitReader reader,
        string code,
        string message,
        bool limitExceeded,
        int start)
    {
        reader.CreateError(
            code,
            message,
            limitExceeded,
            true,
            start);
        PerLengthResult result = this.CopyReaderFailure(reader);
        result.BitCount = reader.Position - start;
        return result;
    }

    internal PerLengthResult CopyReaderFailure(PerBitReader reader)
    {
        return new PerLengthResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = reader.LimitExceeded,
            ErrorCode = reader.ErrorCode,
            ErrorMessage = reader.ErrorMessage
        };
    }

    internal PerLengthResult CopyWriterFailure(PerBitWriter writer)
    {
        return new PerLengthResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = writer.LimitExceeded,
            ErrorCode = writer.ErrorCode,
            ErrorMessage = writer.ErrorMessage
        };
    }

    internal PerLengthResult CopyWholeNumberError(
        PerWholeNumberResult source)
    {
        return new PerLengthResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerLengthResult CreateWriteSuccess(
        int length,
        bool moreFragments)
    {
        return new PerLengthResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Length = length,
            FragmentLength = length,
            MoreFragments = moreFragments
        };
    }

    internal PerLengthResult CreateReadSuccess(
        int length,
        bool moreFragments,
        int bitCount,
        int padding)
    {
        return new PerLengthResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Length = length,
            FragmentLength = length,
            MoreFragments = moreFragments,
            BitCount = bitCount,
            PaddingBitCount = padding
        };
    }

    internal PerLengthResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerLengthResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
