namespace asn1;

internal class PerWholeNumberCodec
{
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;

    internal PerWholeNumberCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Resources = new ResourceCounter(limits);
    }

    internal PerWholeNumberResult EncodeNonNegativeBinary(
        System.Numerics.BigInteger value,
        int bitCount)
    {
        if (bitCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        if (value < System.Numerics.BigInteger.Zero)
        {
            return this.CreateError(
                "ASNP0010",
                "A non-negative binary integer cannot encode a negative value.",
                false);
        }

        if (!this.AllowsIntegerValue(value)
            || !new ResourceCounter(this.Limits).AllowsIntegerBits(bitCount))
        {
            return this.CreateError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        if (value >> bitCount != System.Numerics.BigInteger.Zero)
        {
            return this.CreateError(
                "ASNP0011",
                "The integer does not fit the requested non-negative binary field.",
                false);
        }

        int byteCount = this.GetByteCount(bitCount);
        if (!this.AllowsIntegerOctets(byteCount)
            || !this.Resources.TryChargeAllocationBytes(byteCount))
        {
            return this.CreateError(
                "ASNR0005",
                "The integer field exceeds the configured resource limits.",
                true);
        }

        byte[] data = new byte[byteCount];
        int index = 0;
        while (index < bitCount)
        {
            int sourceShift = bitCount - index - 1;
            if (((value >> sourceShift) & System.Numerics.BigInteger.One)
                != System.Numerics.BigInteger.Zero)
            {
                int targetShift = 7 - index % 8;
                data[index / 8] = (byte)(data[index / 8]
                    | 1 << targetShift);
            }

            index = index + 1;
        }

        return new PerWholeNumberResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            BitCount = bitCount,
            OctetCount = byteCount
        };
    }

    internal PerWholeNumberResult DecodeNonNegativeBinary(
        byte[] data,
        int bitCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (bitCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        int byteCount = this.GetByteCount(bitCount);
        if (data.Length != byteCount)
        {
            return this.CreateError(
                "ASNP0012",
                "The non-negative binary field length does not match its bit count.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsIntegerBits(bitCount)
            || !this.AllowsIntegerOctets(byteCount))
        {
            return this.CreateError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        int index = 0;
        while (index < bitCount)
        {
            value = value << 1;
            int shift = 7 - index % 8;
            if (((data[index / 8] >> shift) & 1) != 0)
            {
                value = value | System.Numerics.BigInteger.One;
            }

            index = index + 1;
        }

        return new PerWholeNumberResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            BitCount = bitCount,
            OctetCount = byteCount
        };
    }

    internal PerWholeNumberResult EncodeMinimumNonNegative(
        System.Numerics.BigInteger value)
    {
        if (value < System.Numerics.BigInteger.Zero)
        {
            return this.CreateError(
                "ASNP0010",
                "A non-negative binary integer cannot encode a negative value.",
                false);
        }

        if (!this.AllowsIntegerValue(value))
        {
            return this.CreateError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        byte[] data = BigIntegerConversions.ToUnsignedBigEndian(value);
        if (!this.AllowsIntegerOctets(data.Length)
            || !this.Resources.TryChargeAllocationBytes(data.Length))
        {
            return this.CreateError(
                "ASNR0005",
                "The integer field exceeds the configured resource limits.",
                true);
        }

        return this.CreateField(value, data, data.Length * 8, false, false);
    }

    internal PerWholeNumberResult DecodeMinimumNonNegative(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (data.Length == 0)
        {
            return this.CreateError(
                "ASNP0013",
                "A minimum non-negative binary integer needs at least one octet.",
                false);
        }

        if (data.Length > 1 && data[0] == 0)
        {
            return this.CreateError(
                "ASNP0014",
                "The non-negative binary integer has a redundant leading octet.",
                false);
        }

        if (!this.AllowsIntegerOctets(data.Length))
        {
            return this.CreateError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        System.Numerics.BigInteger value =
            BigIntegerConversions.FromUnsignedBigEndian(data);
        if (!this.AllowsIntegerValue(value))
        {
            return this.CreateError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        return this.CreateDecodedField(value, data, false);
    }

    internal PerWholeNumberResult EncodeTwosComplementInteger(
        System.Numerics.BigInteger value)
    {
        if (!this.AllowsIntegerValue(value))
        {
            return this.CreateError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        byte[] data = BigIntegerConversions.ToSignedBigEndian(value);
        if (!this.AllowsIntegerOctets(data.Length)
            || !this.Resources.TryChargeAllocationBytes(data.Length))
        {
            return this.CreateError(
                "ASNR0005",
                "The integer field exceeds the configured resource limits.",
                true);
        }

        return this.CreateField(value, data, data.Length * 8, false, false);
    }

    internal PerWholeNumberResult DecodeTwosComplementInteger(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (data.Length == 0)
        {
            return this.CreateError(
                "ASNP0015",
                "A two's-complement integer needs at least one octet.",
                false);
        }

        if (data.Length > 1
            && (data[0] == 0 && (data[1] & 128) == 0
                || data[0] == 255 && (data[1] & 128) != 0))
        {
            return this.CreateError(
                "ASNP0016",
                "The two's-complement integer has a redundant leading octet.",
                false);
        }

        if (!this.AllowsIntegerOctets(data.Length))
        {
            return this.CreateError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        System.Numerics.BigInteger value =
            BigIntegerConversions.FromSignedBigEndian(data);
        if (!this.AllowsIntegerValue(value))
        {
            return this.CreateError(
                "ASNR0010",
                "The integer exceeds the configured integer-bit limit.",
                true);
        }

        return this.CreateDecodedField(value, data, false);
    }

    internal PerWholeNumberResult EncodeConstrainedWholeNumber(
        System.Numerics.BigInteger value,
        System.Numerics.BigInteger lowerBound,
        System.Numerics.BigInteger upperBound,
        bool aligned)
    {
        if (upperBound < lowerBound)
        {
            return this.CreateError(
                "ASNP0017",
                "The constrained whole-number bounds are reversed.",
                false);
        }

        if (value < lowerBound || value > upperBound)
        {
            return this.CreateError(
                "ASNP0018",
                "The whole number is outside the constrained range.",
                false);
        }

        if (!this.AllowsIntegerValue(value)
            || !this.AllowsIntegerValue(lowerBound)
            || !this.AllowsIntegerValue(upperBound))
        {
            return this.CreateError(
                "ASNR0010",
                "The constrained whole number exceeds the configured integer-bit limit.",
                true);
        }

        System.Numerics.BigInteger range = upperBound - lowerBound
            + System.Numerics.BigInteger.One;
        System.Numerics.BigInteger offset = value - lowerBound;
        if (range == System.Numerics.BigInteger.One)
        {
            return this.CreateField(value, [], 0, false, false);
        }

        int bitCount = this.GetUnsignedBitCount(range - 1);
        if (bitCount == int.MaxValue)
        {
            return this.CreateError(
                "ASNR0010",
                "The constrained range exceeds the configured integer-bit limit.",
                true);
        }

        bool octetAligned = false;
        bool requiresLength = false;
        if (aligned)
        {
            if (range == new System.Numerics.BigInteger(256))
            {
                bitCount = 8;
                octetAligned = true;
            }
            else if (range > new System.Numerics.BigInteger(256)
                && range <= new System.Numerics.BigInteger(65536))
            {
                bitCount = 16;
                octetAligned = true;
            }
            else if (range > new System.Numerics.BigInteger(65536))
            {
                PerWholeNumberResult indefinite =
                    this.EncodeMinimumNonNegative(offset);
                if (indefinite.Success)
                {
                    indefinite.Value = value;
                    indefinite.OctetAligned = true;
                    indefinite.RequiresLengthDeterminant = true;
                }

                return indefinite;
            }
        }

        PerWholeNumberResult encoded =
            this.EncodeNonNegativeBinary(offset, bitCount);
        if (!encoded.Success)
        {
            return encoded;
        }

        encoded.Value = value;
        encoded.OctetAligned = octetAligned;
        encoded.RequiresLengthDeterminant = requiresLength;
        return encoded;
    }

    internal PerWholeNumberResult DecodeConstrainedWholeNumber(
        byte[] data,
        int bitCount,
        System.Numerics.BigInteger lowerBound,
        System.Numerics.BigInteger upperBound,
        bool aligned)
    {
        if (upperBound < lowerBound)
        {
            return this.CreateError(
                "ASNP0017",
                "The constrained whole-number bounds are reversed.",
                false);
        }

        if (!this.AllowsIntegerValue(lowerBound)
            || !this.AllowsIntegerValue(upperBound))
        {
            return this.CreateError(
                "ASNR0010",
                "The constrained whole-number bounds exceed the integer-bit limit.",
                true);
        }

        System.Numerics.BigInteger range = upperBound - lowerBound
            + System.Numerics.BigInteger.One;
        int expectedBits = range == System.Numerics.BigInteger.One
            ? 0
            : this.GetUnsignedBitCount(range - 1);
        if (expectedBits == int.MaxValue)
        {
            return this.CreateError(
                "ASNR0010",
                "The constrained range exceeds the configured integer-bit limit.",
                true);
        }

        bool octetAligned = false;
        bool requiresLength = false;
        if (aligned && range == new System.Numerics.BigInteger(256))
        {
            expectedBits = 8;
            octetAligned = true;
        }
        else if (aligned && range > new System.Numerics.BigInteger(256)
            && range <= new System.Numerics.BigInteger(65536))
        {
            expectedBits = 16;
            octetAligned = true;
        }
        else if (aligned && range > new System.Numerics.BigInteger(65536))
        {
            expectedBits = bitCount;
            octetAligned = true;
            requiresLength = true;
            if (bitCount <= 0 || bitCount % 8 != 0)
            {
                return this.CreateError(
                    "ASNP0019",
                    "An indefinite constrained integer must contain whole octets.",
                    false);
            }
        }

        if (bitCount != expectedBits)
        {
            return this.CreateError(
                "ASNP0020",
                "The constrained integer field has the wrong width.",
                false);
        }

        PerWholeNumberResult decoded;
        if (requiresLength)
        {
            decoded = this.DecodeMinimumNonNegative(data);
        }
        else
        {
            decoded = this.DecodeNonNegativeBinary(data, bitCount);
        }

        if (!decoded.Success)
        {
            return decoded;
        }

        if (decoded.Value >= range)
        {
            return this.CreateError(
                "ASNP0021",
                "The constrained integer field encodes an unused range value.",
                false);
        }

        decoded.Value = lowerBound + decoded.Value;
        decoded.OctetAligned = octetAligned;
        decoded.RequiresLengthDeterminant = requiresLength;
        return decoded;
    }

    internal PerWholeNumberResult EncodeSemiConstrainedWholeNumber(
        System.Numerics.BigInteger value,
        System.Numerics.BigInteger lowerBound,
        bool aligned)
    {
        if (!this.AllowsIntegerValue(value)
            || !this.AllowsIntegerValue(lowerBound))
        {
            return this.CreateError(
                "ASNR0010",
                "The semi-constrained whole number exceeds the integer-bit limit.",
                true);
        }

        if (value < lowerBound)
        {
            return this.CreateError(
                "ASNP0022",
                "The whole number is below the semi-constrained lower bound.",
                false);
        }

        PerWholeNumberResult result =
            this.EncodeMinimumNonNegative(value - lowerBound);
        if (result.Success)
        {
            result.Value = value;
            result.OctetAligned = aligned;
            result.RequiresLengthDeterminant = true;
        }

        return result;
    }

    internal PerWholeNumberResult DecodeSemiConstrainedWholeNumber(
        byte[] data,
        System.Numerics.BigInteger lowerBound,
        bool aligned)
    {
        if (!this.AllowsIntegerValue(lowerBound))
        {
            return this.CreateError(
                "ASNR0010",
                "The semi-constrained lower bound exceeds the integer-bit limit.",
                true);
        }

        PerWholeNumberResult result = this.DecodeMinimumNonNegative(data);
        if (result.Success)
        {
            result.Value = lowerBound + result.Value;
            if (!this.AllowsIntegerValue(result.Value))
            {
                return this.CreateError(
                    "ASNR0010",
                    "The semi-constrained whole number exceeds the configured integer-bit limit.",
                    true);
            }

            result.OctetAligned = aligned;
            result.RequiresLengthDeterminant = true;
        }

        return result;
    }

    internal PerWholeNumberResult EncodeUnconstrainedWholeNumber(
        System.Numerics.BigInteger value,
        bool aligned)
    {
        PerWholeNumberResult result =
            this.EncodeTwosComplementInteger(value);
        if (result.Success)
        {
            result.OctetAligned = aligned;
            result.RequiresLengthDeterminant = true;
        }

        return result;
    }

    internal PerWholeNumberResult DecodeUnconstrainedWholeNumber(
        byte[] data,
        bool aligned)
    {
        PerWholeNumberResult result =
            this.DecodeTwosComplementInteger(data);
        if (result.Success)
        {
            result.OctetAligned = aligned;
            result.RequiresLengthDeterminant = true;
        }

        return result;
    }

    internal PerWholeNumberResult WriteNormallySmallWholeNumber(
        PerBitWriter writer,
        System.Numerics.BigInteger value,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (value < System.Numerics.BigInteger.Zero)
        {
            return this.CreateError(
                "ASNP0023",
                "A normally-small whole number must be non-negative.",
                false);
        }

        if (!this.AllowsIntegerValue(value))
        {
            return this.CreateError(
                "ASNR0010",
                "The normally-small whole number exceeds the integer-bit limit.",
                true);
        }

        if (value <= new System.Numerics.BigInteger(63))
        {
            if (!writer.PrepareAdditionalBits(7))
            {
                return this.CopyWriterFailure(writer);
            }

            writer.WriteUncheckedBit(false);
            this.WriteUncheckedValue(writer, value, 6);
            return this.CreateWriteSuccess(7, 0);
        }

        byte[] content = BigIntegerConversions.ToUnsignedBigEndian(value);
        if (!this.AllowsIntegerOctets(content.Length))
        {
            return this.CreateError(
                "ASNR0010",
                "The normally-small whole number exceeds the integer-bit limit.",
                true);
        }

        if (!this.Resources.TryChargeAllocationBytes(content.Length))
        {
            return this.CreateError(
                "ASNR0005",
                "The normally-small integer exceeds the allocation limit.",
                true);
        }

        int padding = aligned
            ? (8 - (writer.GetWrittenBitCount() + 1) % 8) % 8
            : 0;
        int lengthOctets = this.GetFragmentLengthOctets(content.Length);
        if (lengthOctets < 0
            || content.Length > (int.MaxValue - 1 - padding) / 8
            || lengthOctets > (int.MaxValue - 1 - padding
                - content.Length * 8) / 8)
        {
            return this.CreateError(
                "ASNR0008",
                "The normally-small encoding exceeds the supported bit range.",
                true);
        }

        int totalBits = 1 + padding
            + lengthOctets * 8 + content.Length * 8;
        if (!writer.PrepareAdditionalBits(totalBits))
        {
            return this.CopyWriterFailure(writer);
        }

        writer.WriteUncheckedBit(true);
        int index = 0;
        while (index < padding)
        {
            writer.WriteUncheckedBit(false);
            index = index + 1;
        }

        this.WriteFragmentedOctets(writer, content);
        return this.CreateWriteSuccess(totalBits, content.Length);
    }

    internal PerWholeNumberResult ReadNormallySmallWholeNumber(
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

            return new PerWholeNumberResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Value = small.Value,
                BitCount = reader.Position - start
            };
        }

        if (aligned)
        {
            PerBitReadResult alignment = reader.AlignInput(true);
            if (alignment.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, alignment, start);
            }
        }

        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        int contentOctets = 0;
        int firstContent = -1;
        bool moreFragments = true;
        while (moreFragments)
        {
            PerBitReadResult firstLength = reader.ReadBits(8);
            if (firstLength.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, firstLength, start);
            }

            int first = (int)firstLength.Value;
            int fragmentLength;
            if ((first & 128) == 0)
            {
                fragmentLength = first;
                moreFragments = false;
            }
            else if ((first & 64) == 0)
            {
                PerBitReadResult secondLength = reader.ReadBits(8);
                if (secondLength.Status != AsnDecodeStatus.Done)
                {
                    return this.HandleIncompleteRead(reader, secondLength, start);
                }

                fragmentLength = (first & 63) * 256
                    + (int)secondLength.Value;
                if (fragmentLength < 128)
                {
                    return this.CreateReaderError(
                        reader,
                        "ASNP0024",
                        "The normally-small length determinant is not minimal.",
                        false,
                        start);
                }

                moreFragments = false;
            }
            else
            {
                int units = first & 63;
                if (units < 1 || units > 4)
                {
                    return this.CreateReaderError(
                        reader,
                        "ASNP0025",
                        "The normally-small fragment multiplier is invalid.",
                        false,
                        start);
                }

                fragmentLength = units * 16384;
            }

            if (fragmentLength > int.MaxValue - contentOctets)
            {
                return this.CreateReaderError(
                    reader,
                    "ASNR0008",
                    "The normally-small integer length exceeds the supported range.",
                    true,
                    start);
            }

            int newContentOctets = contentOctets + fragmentLength;
            if (!this.AllowsIntegerOctets(newContentOctets))
            {
                return this.CreateReaderError(
                    reader,
                    "ASNR0010",
                    "The normally-small whole number exceeds the integer-bit limit.",
                    true,
                    start);
            }

            int index = 0;
            while (index < fragmentLength)
            {
                PerBitReadResult octet = reader.ReadBits(8);
                if (octet.Status != AsnDecodeStatus.Done)
                {
                    return this.HandleIncompleteRead(reader, octet, start);
                }

                int current = (int)octet.Value;
                if (firstContent < 0)
                {
                    firstContent = current;
                }

                value = value << 8;
                value = value | new System.Numerics.BigInteger(current);
                index = index + 1;
            }

            contentOctets = newContentOctets;
        }

        if (contentOctets == 0)
        {
            return this.CreateReaderError(
                reader,
                "ASNP0026",
                "The large normally-small form cannot have empty integer content.",
                false,
                start);
        }

        if (contentOctets > 1 && firstContent == 0)
        {
            return this.CreateReaderError(
                reader,
                "ASNP0014",
                "The non-negative binary integer has a redundant leading octet.",
                false,
                start);
        }

        if (value <= new System.Numerics.BigInteger(63))
        {
            return this.CreateReaderError(
                reader,
                "ASNP0027",
                "The large normally-small form encodes a value below 64.",
                false,
                start);
        }

        if (!this.AllowsIntegerValue(value))
        {
            return this.CreateReaderError(
                reader,
                "ASNR0010",
                "The normally-small whole number exceeds the integer-bit limit.",
                true,
                start);
        }

        return new PerWholeNumberResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            BitCount = reader.Position - start,
            OctetCount = contentOctets,
            OctetAligned = aligned,
            RequiresLengthDeterminant = true
        };
    }

    internal int GetUnsignedBitCount(System.Numerics.BigInteger value)
    {
        if (value < System.Numerics.BigInteger.Zero)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        int count = 0;
        System.Numerics.BigInteger remaining = value;
        while (remaining > System.Numerics.BigInteger.Zero)
        {
            count = count + 1;
            if (!new ResourceCounter(this.Limits).AllowsIntegerBits(count))
            {
                return int.MaxValue;
            }

            remaining = remaining >> 1;
        }

        return count;
    }

    internal bool AllowsIntegerValue(System.Numerics.BigInteger value)
    {
        System.Numerics.BigInteger remaining =
            System.Numerics.BigInteger.Abs(value);
        int count = 0;
        while (remaining > System.Numerics.BigInteger.Zero)
        {
            count = count + 1;
            if (!new ResourceCounter(this.Limits).AllowsIntegerBits(count))
            {
                return false;
            }

            remaining = remaining >> 1;
        }

        return new ResourceCounter(this.Limits).AllowsIntegerBits(count);
    }

    internal bool AllowsIntegerOctets(int count)
    {
        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(count))
        {
            return false;
        }

        if (this.Limits.Unlimited)
        {
            return true;
        }

        int maximumBits = this.Limits.MaximumIntegerBits;
        int maximumOctets = maximumBits / 8;
        if (maximumBits % 8 != 0)
        {
            maximumOctets = maximumOctets + 1;
        }

        return count <= maximumOctets + 1;
    }

    internal int GetByteCount(int bitCount)
    {
        int count = bitCount / 8;
        return bitCount % 8 == 0 ? count : count + 1;
    }

    internal int GetFragmentLengthOctets(int contentLength)
    {
        int remaining = contentLength;
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

    internal void WriteFragmentedOctets(PerBitWriter writer, byte[] content)
    {
        int offset = 0;
        int remaining = content.Length;
        while (remaining >= 16384)
        {
            int units = remaining / 16384;
            if (units > 4)
            {
                units = 4;
            }

            this.WriteUncheckedOctet(writer, 192 | units);
            int fragmentLength = units * 16384;
            this.WriteUncheckedOctets(writer, content, offset, fragmentLength);
            offset = offset + fragmentLength;
            remaining = remaining - fragmentLength;
        }

        if (remaining <= 127)
        {
            this.WriteUncheckedOctet(writer, remaining);
        }
        else
        {
            this.WriteUncheckedOctet(writer, 128 | remaining >> 8);
            this.WriteUncheckedOctet(writer, remaining & 255);
        }

        this.WriteUncheckedOctets(writer, content, offset, remaining);
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

    internal void WriteUncheckedOctet(PerBitWriter writer, int value)
    {
        this.WriteUncheckedValue(
            writer,
            new System.Numerics.BigInteger(value),
            8);
    }

    internal void WriteUncheckedValue(
        PerBitWriter writer,
        System.Numerics.BigInteger value,
        int bitCount)
    {
        int index = bitCount - 1;
        while (index >= 0)
        {
            writer.WriteUncheckedBit(
                ((value >> index) & System.Numerics.BigInteger.One)
                != System.Numerics.BigInteger.Zero);
            index = index - 1;
        }
    }

    internal PerWholeNumberResult HandleIncompleteRead(
        PerBitReader reader,
        PerBitReadResult input,
        int start)
    {
        if (input.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
            return new PerWholeNumberResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        return this.CopyReaderFailure(reader);
    }

    internal PerWholeNumberResult CreateReaderError(
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
        PerWholeNumberResult result = this.CopyReaderFailure(reader);
        result.BitCount = reader.Position - start;
        return result;
    }

    internal PerWholeNumberResult CopyReaderFailure(PerBitReader reader)
    {
        return new PerWholeNumberResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = reader.LimitExceeded,
            ErrorCode = reader.ErrorCode,
            ErrorMessage = reader.ErrorMessage
        };
    }

    internal PerWholeNumberResult CopyWriterFailure(PerBitWriter writer)
    {
        return new PerWholeNumberResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = writer.LimitExceeded,
            ErrorCode = writer.ErrorCode,
            ErrorMessage = writer.ErrorMessage
        };
    }

    internal PerWholeNumberResult CreateWriteSuccess(
        int bitCount,
        int octetCount)
    {
        return new PerWholeNumberResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            BitCount = bitCount,
            OctetCount = octetCount
        };
    }

    internal PerWholeNumberResult CreateField(
        System.Numerics.BigInteger value,
        byte[] data,
        int bitCount,
        bool octetAligned,
        bool requiresLength)
    {
        return new PerWholeNumberResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            BitCount = bitCount,
            OctetCount = data.Length,
            OctetAligned = octetAligned,
            RequiresLengthDeterminant = requiresLength
        };
    }

    internal PerWholeNumberResult CreateDecodedField(
        System.Numerics.BigInteger value,
        byte[] data,
        bool requiresLength)
    {
        return new PerWholeNumberResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = data,
            BitCount = data.Length * 8,
            OctetCount = data.Length,
            RequiresLengthDeterminant = requiresLength
        };
    }

    internal PerWholeNumberResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerWholeNumberResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
