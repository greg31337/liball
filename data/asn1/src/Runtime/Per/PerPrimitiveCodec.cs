namespace asn1;

internal class PerPrimitiveCodec
{
    internal AsnResourceLimits Limits;
    internal PerLengthCodec Lengths;
    internal PerWholeNumberCodec WholeNumbers;

    internal PerPrimitiveCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Lengths = new PerLengthCodec(limits);
        this.WholeNumbers = new PerWholeNumberCodec(limits);
    }

    internal PerPrimitiveResult WriteBoolean(
        PerBitWriter writer,
        bool value)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        int start = writer.GetWrittenBitCount();
        PerBitWriteResult written = writer.WriteBit(value);
        return written.Success
            ? this.CreateWriteSuccess(writer, start)
            : this.CopyWriterFailure(writer);
    }

    internal PerPrimitiveResult ReadBoolean(PerBitReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int start = reader.Position;
        PerBitReadResult input = reader.ReadBit();
        if (input.Status != AsnDecodeStatus.Done)
        {
            return this.HandleIncompleteRead(reader, input, start);
        }

        return this.CreateReadSuccess(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = input.Value != System.Numerics.BigInteger.Zero
            },
            reader.Position - start,
            0);
    }

    internal PerPrimitiveResult WriteNull(PerBitWriter writer)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        return this.CreateWriteSuccess(writer, writer.GetWrittenBitCount());
    }

    internal PerPrimitiveResult ReadNull(PerBitReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        return this.CreateReadSuccess(
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null },
            0,
            0);
    }

    internal PerPrimitiveResult WriteInteger(
        PerBitWriter writer,
        System.Numerics.BigInteger value,
        bool hasLowerBound,
        System.Numerics.BigInteger lowerBound,
        bool hasUpperBound,
        System.Numerics.BigInteger upperBound,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (hasUpperBound && !hasLowerBound)
        {
            return this.CreateError(
                "ASNP0040",
                "A PER integer upper bound requires a lower bound.",
                false);
        }

        PerWholeNumberResult field = hasUpperBound
            ? this.WholeNumbers.EncodeConstrainedWholeNumber(
                value,
                lowerBound,
                upperBound,
                aligned)
            : hasLowerBound
                ? this.WholeNumbers.EncodeSemiConstrainedWholeNumber(
                    value,
                    lowerBound,
                    aligned)
                : this.WholeNumbers.EncodeUnconstrainedWholeNumber(
                    value,
                    aligned);
        if (!field.Success)
        {
            return this.CopyWholeNumberFailure(field);
        }

        int start = writer.GetWrittenBitCount();
        PerPrimitiveResult written;
        System.Numerics.BigInteger range = hasUpperBound
            ? upperBound - lowerBound + System.Numerics.BigInteger.One
            : System.Numerics.BigInteger.Zero;
        if (hasUpperBound && aligned
            && range > new System.Numerics.BigInteger(65536))
        {
            int maximumBits = this.WholeNumbers.GetUnsignedBitCount(
                range - 1);
            int maximumOctets = (maximumBits + 7) / 8;
            PerLengthResult length = this.Lengths.WriteConstrainedLength(
                writer,
                field.Data.Length,
                1,
                maximumOctets,
                true);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            PerBitWriteResult padding = writer.AlignOutput();
            if (!padding.Success)
            {
                return this.CopyWriterFailure(writer);
            }

            PerBitWriteResult payload = writer.WriteInputBits(
                field.Data,
                0,
                field.BitCount);
            written = payload.Success
                ? this.CreateWriteSuccess(writer, start)
                : this.CopyWriterFailure(writer);
        }
        else
        {
            written = this.WriteWholeNumberField(writer, field, aligned);
        }
        if (!written.Success)
        {
            return written;
        }

        written.BitCount = writer.GetWrittenBitCount() - start;
        return written;
    }

    internal PerPrimitiveResult ReadInteger(
        PerBitReader reader,
        bool hasLowerBound,
        System.Numerics.BigInteger lowerBound,
        bool hasUpperBound,
        System.Numerics.BigInteger upperBound,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        if (hasUpperBound && !hasLowerBound)
        {
            return this.CreateError(
                "ASNP0040",
                "A PER integer upper bound requires a lower bound.",
                false);
        }

        int start = reader.Position;
        PerWholeNumberResult decoded;
        if (hasUpperBound)
        {
            System.Numerics.BigInteger range = upperBound - lowerBound
                + System.Numerics.BigInteger.One;
            if (range <= System.Numerics.BigInteger.Zero)
            {
                return this.CreateError(
                    "ASNP0017",
                    "The constrained whole-number bounds are reversed.",
                    false);
            }

            if (aligned
                && range > new System.Numerics.BigInteger(65536))
            {
                int maximumBits = this.WholeNumbers.GetUnsignedBitCount(
                    range - 1);
                int maximumOctets = (maximumBits + 7) / 8;
                PerLengthResult length = this.Lengths.ReadConstrainedLength(
                    reader,
                    1,
                    maximumOctets,
                    true);
                if (!length.Success)
                {
                    return this.CopyLengthFailure(length);
                }

                PerBitReadResult payload = this.ReadPayloadBits(
                    reader,
                    length.Length * 8,
                    true);
                if (payload.Status != AsnDecodeStatus.Done)
                {
                    return this.HandleIncompleteRead(reader, payload, start);
                }

                decoded = this.WholeNumbers.DecodeConstrainedWholeNumber(
                    payload.Data,
                    length.Length * 8,
                    lowerBound,
                    upperBound,
                    aligned);
            }
            else
            {
                int fieldBits = range == System.Numerics.BigInteger.One
                    ? 0
                    : this.WholeNumbers.GetUnsignedBitCount(range - 1);
                bool octetAligned = aligned
                    && range >= new System.Numerics.BigInteger(256);
                if (octetAligned && fieldBits < 8)
                {
                    fieldBits = 8;
                }
                else if (octetAligned && fieldBits > 8)
                {
                    fieldBits = 16;
                }

                PerBitReadResult input = this.ReadFieldBits(
                    reader,
                    fieldBits,
                    octetAligned);
                if (input.Status != AsnDecodeStatus.Done)
                {
                    return this.HandleIncompleteRead(reader, input, start);
                }

                decoded = this.WholeNumbers.DecodeConstrainedWholeNumber(
                    input.Data,
                    fieldBits,
                    lowerBound,
                    upperBound,
                    aligned);
            }
        }
        else
        {
            PerPrimitiveResult content = this.ReadOctetContent(reader, aligned);
            if (!content.Success)
            {
                return content;
            }

            byte[] data = content.Value == null ? [] : content.Value.Octets;
            decoded = hasLowerBound
                ? this.WholeNumbers.DecodeSemiConstrainedWholeNumber(
                    data,
                    lowerBound,
                    aligned)
                : this.WholeNumbers.DecodeUnconstrainedWholeNumber(
                    data,
                    aligned);
        }

        if (!decoded.Success)
        {
            reader.CreateError(
                decoded.ErrorCode,
                decoded.ErrorMessage,
                decoded.LimitExceeded,
                true,
                start);
            return this.CopyReaderFailure(reader);
        }

        return this.CreateReadSuccess(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = decoded.Value
            },
            reader.Position - start,
            0);
    }

    internal PerPrimitiveResult WriteEnumeratedIndex(
        PerBitWriter writer,
        int index,
        int rootCount,
        bool aligned)
    {
        if (rootCount < 1 || index < 0 || index >= rootCount)
        {
            return this.CreateError(
                "ASNP0041",
                "The enumeration index is outside the root enumeration.",
                false);
        }

        return this.WriteInteger(
            writer,
            new System.Numerics.BigInteger(index),
            true,
            System.Numerics.BigInteger.Zero,
            true,
            new System.Numerics.BigInteger(rootCount - 1),
            aligned);
    }

    internal PerPrimitiveResult ReadEnumeratedIndex(
        PerBitReader reader,
        int rootCount,
        bool aligned)
    {
        if (rootCount < 1)
        {
            return this.CreateError(
                "ASNP0041",
                "The root enumeration must contain at least one value.",
                false);
        }

        PerPrimitiveResult result = this.ReadInteger(
            reader,
            true,
            System.Numerics.BigInteger.Zero,
            true,
            new System.Numerics.BigInteger(rootCount - 1),
            aligned);
        if (result.Success && result.Value != null)
        {
            result.Value.Kind = AsnRuntimeValueKind.Enumerated;
        }

        return result;
    }

    internal PerPrimitiveResult WriteOctetString(
        PerBitWriter writer,
        byte[] value,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (!writer.IsStreaming()
            && !new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
                value.Length))
        {
            return this.CreateError(
                "ASNR0006",
                "The octet-string value exceeds the configured byte limit.",
                true);
        }

        PerPrimitiveResult validation = this.ValidateSize(
            value.Length,
            lowerBound,
            hasUpperBound,
            upperBound);
        if (!validation.Success)
        {
            return validation;
        }

        int start = writer.GetWrittenBitCount();
        if (hasUpperBound && lowerBound == upperBound
            && upperBound < 65536)
        {
            if (aligned && value.Length > 2)
            {
                PerBitWriteResult padding = writer.AlignOutput();
                if (!padding.Success)
                {
                    return this.CopyWriterFailure(writer);
                }
            }

            PerBitWriteResult fixedValue = writer.WriteInputBits(
                value,
                0,
                value.Length * 8);
            return fixedValue.Success
                ? this.CreateWriteSuccess(writer, start)
                : this.CopyWriterFailure(writer);
        }

        if (!hasUpperBound || upperBound >= 65536)
        {
            PerLengthResult fragmented = this.Lengths.WriteFragmentedOctets(
                writer,
                value,
                0,
                value.Length,
                aligned);
            return fragmented.Success
                ? this.CreateWriteSuccess(writer, start)
                : this.CopyLengthFailure(fragmented);
        }

        PerLengthResult length = this.Lengths.WriteConstrainedLength(
            writer,
            value.Length,
            lowerBound,
            upperBound,
            aligned);
        if (!length.Success)
        {
            return this.CopyLengthFailure(length);
        }

        if (aligned && value.Length > 0)
        {
            PerBitWriteResult padding = writer.AlignOutput();
            if (!padding.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        PerBitWriteResult payload = writer.WriteInputBits(
            value,
            0,
            value.Length * 8);
        return payload.Success
            ? this.CreateWriteSuccess(writer, start)
            : this.CopyWriterFailure(writer);
    }

    internal PerPrimitiveResult ReadOctetString(
        PerBitReader reader,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int start = reader.Position;
        if (hasUpperBound && lowerBound == upperBound
            && upperBound < 65536)
        {
            PerBitReadResult input = this.ReadPayloadBits(
                reader,
                lowerBound * 8,
                aligned && lowerBound > 2);
            if (input.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, input, start);
            }

            return this.CreateOctetReadSuccess(
                input.Data,
                reader.Position - start,
                0);
        }

        if (!hasUpperBound || upperBound >= 65536)
        {
            PerLengthResult fragmented = this.Lengths.ReadFragmentedOctets(
                reader,
                aligned);
            if (!fragmented.Success)
            {
                return this.CopyLengthFailure(fragmented);
            }

            return this.CreateOctetReadSuccess(
                fragmented.Data,
                reader.Position - start,
                fragmented.PaddingBitCount);
        }

        PerLengthResult length = this.Lengths.ReadConstrainedLength(
            reader,
            lowerBound,
            upperBound,
            aligned);
        if (!length.Success)
        {
            return this.CopyLengthFailure(length);
        }

        PerBitReadResult payload = this.ReadPayloadBits(
            reader,
            length.Length * 8,
            aligned && length.Length > 0);
        if (payload.Status != AsnDecodeStatus.Done)
        {
            return this.HandleIncompleteRead(reader, payload, start);
        }

        return this.CreateOctetReadSuccess(
            payload.Data,
            reader.Position - start,
            0);
    }

    internal PerPrimitiveResult WriteBitString(
        PerBitWriter writer,
        AsnBitStringValue value,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (value == null || !value.IsValid())
        {
            return this.CreateError(
                "ASNP0042",
                "The bit-string value is invalid.",
                false);
        }

        if (!writer.IsStreaming()
            && !new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
                value.Bytes.Length))
        {
            return this.CreateError(
                "ASNR0006",
                "The bit-string value exceeds the configured byte limit.",
                true);
        }

        PerPrimitiveResult validation = this.ValidateSize(
            value.BitLength,
            lowerBound,
            hasUpperBound,
            upperBound);
        if (!validation.Success)
        {
            return validation;
        }

        int start = writer.GetWrittenBitCount();
        if (hasUpperBound && lowerBound == upperBound
            && upperBound < 65536)
        {
            if (aligned && value.BitLength > 16)
            {
                PerBitWriteResult padding = writer.AlignOutput();
                if (!padding.Success)
                {
                    return this.CopyWriterFailure(writer);
                }
            }

            PerBitWriteResult fixedValue = writer.WriteInputBits(
                value.Bytes,
                0,
                value.BitLength);
            return fixedValue.Success
                ? this.CreateWriteSuccess(writer, start)
                : this.CopyWriterFailure(writer);
        }

        if (hasUpperBound && upperBound < 65536)
        {
            PerLengthResult length = this.Lengths.WriteConstrainedLength(
                writer,
                value.BitLength,
                lowerBound,
                upperBound,
                aligned);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            if (aligned && value.BitLength > 0)
            {
                PerBitWriteResult padding = writer.AlignOutput();
                if (!padding.Success)
                {
                    return this.CopyWriterFailure(writer);
                }
            }

            PerBitWriteResult payload = writer.WriteInputBits(
                value.Bytes,
                0,
                value.BitLength);
            return payload.Success
                ? this.CreateWriteSuccess(writer, start)
                : this.CopyWriterFailure(writer);
        }

        return this.WriteFragmentedBits(writer, value, aligned, start);
    }

    internal PerPrimitiveResult ReadBitString(
        PerBitReader reader,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int start = reader.Position;
        if (hasUpperBound && lowerBound == upperBound
            && upperBound < 65536)
        {
            PerBitReadResult fixedValue = this.ReadPayloadBits(
                reader,
                lowerBound,
                aligned && lowerBound > 16);
            if (fixedValue.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, fixedValue, start);
            }

            return this.CreateBitStringReadSuccess(
                fixedValue.Data,
                lowerBound,
                reader.Position - start);
        }

        if (hasUpperBound && upperBound < 65536)
        {
            PerLengthResult length = this.Lengths.ReadConstrainedLength(
                reader,
                lowerBound,
                upperBound,
                aligned);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            PerBitReadResult payload = this.ReadPayloadBits(
                reader,
                length.Length,
                aligned && length.Length > 0);
            if (payload.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, payload, start);
            }

            return this.CreateBitStringReadSuccess(
                payload.Data,
                length.Length,
                reader.Position - start);
        }

        return this.ReadFragmentedBits(reader, aligned, start);
    }

    internal PerPrimitiveResult WriteReal(
        PerBitWriter writer,
        AsnRealValue value,
        bool aligned)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        BerPrimitiveValueResult encoded = new BerRealCodec(this.Limits)
            .EncodeReal(value, AsnEncodingRules.Der);
        return encoded.Success
            ? this.WriteOctetString(writer, encoded.Content, 0, false, 0, aligned)
            : this.CopyBerFailure(encoded);
    }

    internal PerPrimitiveResult ReadReal(
        PerBitReader reader,
        bool aligned)
    {
        PerPrimitiveResult content = this.ReadOctetString(
            reader,
            0,
            false,
            0,
            aligned);
        if (!content.Success || content.Value == null)
        {
            return content;
        }

        BerPrimitiveValueResult decoded = new BerRealCodec(this.Limits)
            .DecodeReal(content.Value.Octets);
        return this.ReplaceDecodedValue(content, decoded);
    }

    internal PerPrimitiveResult WriteObjectIdentifier(
        PerBitWriter writer,
        AsnObjectIdentifierValue value,
        bool relative,
        bool aligned)
    {
        BerPrimitiveValueResult encoded = new BerObjectIdentifierCodec(
            this.Limits).EncodeObjectIdentifier(value, relative);
        return encoded.Success
            ? this.WriteOctetString(writer, encoded.Content, 0, false, 0, aligned)
            : this.CopyBerFailure(encoded);
    }

    internal PerPrimitiveResult ReadObjectIdentifier(
        PerBitReader reader,
        bool relative,
        bool aligned)
    {
        PerPrimitiveResult content = this.ReadOctetString(
            reader,
            0,
            false,
            0,
            aligned);
        if (!content.Success || content.Value == null)
        {
            return content;
        }

        BerPrimitiveValueResult decoded = new BerObjectIdentifierCodec(
            this.Limits).DecodeObjectIdentifier(
                content.Value.Octets,
                relative);
        return this.ReplaceDecodedValue(content, decoded);
    }

    internal PerPrimitiveResult WriteIri(
        PerBitWriter writer,
        string value,
        bool relative,
        bool aligned)
    {
        AsnTypeKind kind = relative
            ? AsnTypeKind.RelativeObjectIdentifierIri
            : AsnTypeKind.ObjectIdentifierIri;
        BerPrimitiveValueResult encoded = new BerCharacterStringCodec(
            this.Limits).EncodeText(kind, value);
        return encoded.Success
            ? this.WriteOctetString(writer, encoded.Content, 0, false, 0, aligned)
            : this.CopyBerFailure(encoded);
    }

    internal PerPrimitiveResult ReadIri(
        PerBitReader reader,
        bool relative,
        bool aligned)
    {
        PerPrimitiveResult content = this.ReadOctetString(
            reader,
            0,
            false,
            0,
            aligned);
        if (!content.Success || content.Value == null)
        {
            return content;
        }

        AsnTypeKind kind = relative
            ? AsnTypeKind.RelativeObjectIdentifierIri
            : AsnTypeKind.ObjectIdentifierIri;
        BerPrimitiveValueResult decoded = new BerCharacterStringCodec(
            this.Limits).DecodeText(kind, content.Value.Octets);
        return this.ReplaceDecodedValue(content, decoded);
    }

    internal PerPrimitiveResult WriteCharacterString(
        PerBitWriter writer,
        AsnTypeKind kind,
        string value,
        bool aligned)
    {
        BerPrimitiveValueResult encoded = new BerCharacterStringCodec(
            this.Limits).EncodeText(kind, value);
        return encoded.Success
            ? this.WriteOctetString(writer, encoded.Content, 0, false, 0, aligned)
            : this.CopyBerFailure(encoded);
    }

    internal PerPrimitiveResult ReadCharacterString(
        PerBitReader reader,
        AsnTypeKind kind,
        bool aligned)
    {
        PerPrimitiveResult content = this.ReadOctetString(
            reader,
            0,
            false,
            0,
            aligned);
        if (!content.Success || content.Value == null)
        {
            return content;
        }

        BerPrimitiveValueResult decoded = new BerCharacterStringCodec(
            this.Limits).DecodeText(kind, content.Value.Octets);
        return this.ReplaceDecodedValue(content, decoded);
    }

    internal PerPrimitiveResult WriteTime(
        PerBitWriter writer,
        AsnTypeKind kind,
        AsnTimeValue value,
        bool aligned)
    {
        BerPrimitiveValueResult encoded = new BerTimeCodec(this.Limits)
            .EncodeTime(kind, value, AsnEncodingRules.Ber);
        return encoded.Success
            ? this.WriteOctetString(writer, encoded.Content, 0, false, 0, aligned)
            : this.CopyBerFailure(encoded);
    }

    internal PerPrimitiveResult ReadTime(
        PerBitReader reader,
        AsnTypeKind kind,
        bool aligned)
    {
        PerPrimitiveResult content = this.ReadOctetString(
            reader,
            0,
            false,
            0,
            aligned);
        if (!content.Success || content.Value == null)
        {
            return content;
        }

        BerPrimitiveValueResult decoded = new BerTimeCodec(this.Limits)
            .DecodeTime(kind, content.Value.Octets);
        return this.ReplaceDecodedValue(content, decoded);
    }

    internal PerPrimitiveResult WriteWholeNumberField(
        PerBitWriter writer,
        PerWholeNumberResult field,
        bool aligned)
    {
        if (field.RequiresLengthDeterminant)
        {
            return this.WriteOctetString(
                writer,
                field.Data,
                0,
                false,
                0,
                aligned);
        }

        if (field.OctetAligned)
        {
            PerBitWriteResult padding = writer.AlignOutput();
            if (!padding.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        int start = writer.GetWrittenBitCount();
        PerBitWriteResult written = writer.WriteInputBits(
            field.Data,
            0,
            field.BitCount);
        return written.Success
            ? this.CreateWriteSuccess(writer, start)
            : this.CopyWriterFailure(writer);
    }

    internal PerPrimitiveResult WriteFragmentedBits(
        PerBitWriter writer,
        AsnBitStringValue value,
        bool aligned,
        int start)
    {
        int offset = 0;
        int remaining = value.BitLength;
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

            if (aligned)
            {
                PerBitWriteResult padding = writer.AlignOutput();
                if (!padding.Success)
                {
                    return this.CopyWriterFailure(writer);
                }
            }

            PerPrimitiveResult fragment = this.WriteBitSlice(
                writer,
                value.Bytes,
                offset,
                count);
            if (!fragment.Success)
            {
                return fragment;
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

        if (aligned)
        {
            PerBitWriteResult padding = writer.AlignOutput();
            if (!padding.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        PerPrimitiveResult finalFragment = this.WriteBitSlice(
            writer,
            value.Bytes,
            offset,
            remaining);
        return finalFragment.Success
            ? this.CreateWriteSuccess(writer, start)
            : finalFragment;
    }

    internal PerPrimitiveResult ReadFragmentedBits(
        PerBitReader reader,
        bool aligned,
        int start)
    {
        System.Collections.Generic.List<bool> bits =
            new System.Collections.Generic.List<bool>();
        int totalBitCount = 0;
        bool more = true;
        while (more)
        {
            PerLengthResult length = this.Lengths.ReadLengthComponent(
                reader,
                aligned);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            if (length.Length > int.MaxValue - totalBitCount)
            {
                return this.CreateError(
                    "ASNR0008",
                    "The reassembled bit string exceeds the supported range.",
                    true);
            }

            totalBitCount = totalBitCount + length.Length;
            int totalByteCount = totalBitCount / 8;
            if (totalBitCount % 8 != 0)
            {
                totalByteCount = totalByteCount + 1;
            }
            if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
                    totalByteCount))
            {
                return this.CreateError(
                    "ASNR0006",
                    "The reassembled bit string exceeds the configured byte limit.",
                    true);
            }

            PerBitReadResult payload = this.ReadPayloadBits(
                reader,
                length.Length,
                aligned);
            if (payload.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, payload, start);
            }

            int index = 0;
            while (index < length.Length)
            {
                int shift = 7 - index % 8;
                bits.Add(((payload.Data[index / 8] >> shift) & 1) != 0);
                index = index + 1;
            }

            more = length.MoreFragments;
        }

        byte[] data = new byte[(bits.Count + 7) / 8];
        int bitIndex = 0;
        while (bitIndex < bits.Count)
        {
            if (bits[bitIndex])
            {
                data[bitIndex / 8] = (byte)(data[bitIndex / 8]
                    | 1 << (7 - bitIndex % 8));
            }

            bitIndex = bitIndex + 1;
        }

        return this.CreateBitStringReadSuccess(
            data,
            bits.Count,
            reader.Position - start);
    }

    internal PerPrimitiveResult WriteBitSlice(
        PerBitWriter writer,
        byte[] data,
        int bitOffset,
        int bitCount)
    {
        int index = 0;
        while (index < bitCount)
        {
            int source = bitOffset + index;
            bool bit = ((data[source / 8] >> (7 - source % 8)) & 1) != 0;
            PerBitWriteResult written = writer.WriteBit(bit);
            if (!written.Success)
            {
                return this.CopyWriterFailure(writer);
            }

            index = index + 1;
        }

        return new PerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            BitCount = bitCount
        };
    }

    internal PerBitReadResult ReadFieldBits(
        PerBitReader reader,
        int bitCount,
        bool octetAligned)
    {
        int padding = 0;
        if (octetAligned)
        {
            padding = (8 - reader.Position % 8) % 8;
            PerBitReadResult aligned = reader.AlignInput(true);
            if (aligned.Status != AsnDecodeStatus.Done)
            {
                return aligned;
            }
        }

        PerBitReadResult input = reader.ReadBits(bitCount);
        if (input.Status == AsnDecodeStatus.Done)
        {
            input.Data = this.ToBitField(input.Value, bitCount);
            input.ConsumedBitCount = bitCount + padding;
        }

        return input;
    }

    internal PerBitReadResult ReadPayloadBits(
        PerBitReader reader,
        int bitCount,
        bool octetAligned)
    {
        if (bitCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        if (octetAligned)
        {
            PerBitReadResult aligned = reader.AlignInput(true);
            if (aligned.Status != AsnDecodeStatus.Done)
            {
                return aligned;
            }
        }

        int byteCount = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            byteCount = byteCount + 1;
        }
        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(byteCount))
        {
            return reader.CreateError(
                "ASNR0006",
                "The primitive payload exceeds the configured resource limits.",
                true,
                false,
                0);
        }

        if (bitCount > reader.GetAvailableBitCount())
        {
            if (!reader.FinalBlock)
            {
                return new PerBitReadResult
                {
                    Status = AsnDecodeStatus.NeedMoreData,
                    BitOffset = reader.Position
                };
            }

            return reader.CreateError(
                "ASNP0004",
                "The final bit input ends before the requested field is complete.",
                false,
                true,
                reader.BufferedBitCount);
        }

        if (!reader.Resources.TryChargeAllocationBytes(byteCount))
        {
            return reader.CreateError(
                "ASNR0006",
                "The primitive payload exceeds the configured resource limits.",
                true,
                false,
                0);
        }

        byte[] data = new byte[byteCount];
        int index = 0;
        while (index < bitCount)
        {
            PerBitReadResult bit = reader.ReadBit();
            if (bit.Status != AsnDecodeStatus.Done)
            {
                return bit;
            }

            if (bit.Value != System.Numerics.BigInteger.Zero)
            {
                data[index / 8] = (byte)(data[index / 8]
                    | 1 << (7 - index % 8));
            }

            index = index + 1;
        }

        return new PerBitReadResult
        {
            Status = AsnDecodeStatus.Done,
            Data = data,
            ConsumedBitCount = bitCount
        };
    }

    internal PerPrimitiveResult ReadOctetContent(
        PerBitReader reader,
        bool aligned)
    {
        return this.ReadOctetString(reader, 0, false, 0, aligned);
    }

    internal byte[] ToBitField(
        System.Numerics.BigInteger value,
        int bitCount)
    {
        byte[] data = new byte[(bitCount + 7) / 8];
        int index = 0;
        while (index < bitCount)
        {
            int sourceShift = bitCount - index - 1;
            if (((value >> sourceShift) & System.Numerics.BigInteger.One)
                != System.Numerics.BigInteger.Zero)
            {
                data[index / 8] = (byte)(data[index / 8]
                    | 1 << (7 - index % 8));
            }

            index = index + 1;
        }

        return data;
    }

    internal PerPrimitiveResult ValidateSize(
        int length,
        int lowerBound,
        bool hasUpperBound,
        int upperBound)
    {
        if (lowerBound < 0
            || (hasUpperBound && upperBound < lowerBound))
        {
            return this.CreateError(
                "ASNP0043",
                "The size bounds are invalid.",
                false);
        }

        if (length < lowerBound
            || (hasUpperBound && length > upperBound))
        {
            return this.CreateError(
                "ASNP0044",
                "The value length is outside the permitted size range.",
                false);
        }

        return new PerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerPrimitiveResult CreateBitStringReadSuccess(
        byte[] data,
        int bitLength,
        int consumed)
    {
        return this.CreateReadSuccess(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = data,
                    BitLength = bitLength
                }
            },
            consumed,
            0);
    }

    internal PerPrimitiveResult CreateOctetReadSuccess(
        byte[] data,
        int consumed,
        int padding)
    {
        return this.CreateReadSuccess(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = data
            },
            consumed,
            padding);
    }

    internal PerPrimitiveResult ReplaceDecodedValue(
        PerPrimitiveResult source,
        BerPrimitiveValueResult decoded)
    {
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CopyBerFailure(decoded);
        }

        source.Value = decoded.Value;
        return source;
    }

    internal PerPrimitiveResult HandleIncompleteRead(
        PerBitReader reader,
        PerBitReadResult input,
        int start)
    {
        if (input.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
            return new PerPrimitiveResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        return this.CopyReaderFailure(reader);
    }

    internal PerPrimitiveResult CreateWriteSuccess(
        PerBitWriter writer,
        int start)
    {
        return new PerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerPrimitiveResult CreateReadSuccess(
        AsnRuntimeValue value,
        int bitCount,
        int padding)
    {
        return new PerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            BitCount = bitCount,
            PaddingBitCount = padding
        };
    }

    internal PerPrimitiveResult CopyBerFailure(
        BerPrimitiveValueResult source)
    {
        return this.CreateError(
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal PerPrimitiveResult CopyLengthFailure(PerLengthResult source)
    {
        return new PerPrimitiveResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerPrimitiveResult CopyWholeNumberFailure(
        PerWholeNumberResult source)
    {
        return new PerPrimitiveResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerPrimitiveResult CopyReaderFailure(PerBitReader reader)
    {
        return new PerPrimitiveResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = reader.LimitExceeded,
            ErrorCode = reader.ErrorCode,
            ErrorMessage = reader.ErrorMessage
        };
    }

    internal PerPrimitiveResult CopyWriterFailure(PerBitWriter writer)
    {
        return new PerPrimitiveResult
        {
            LimitExceeded = writer.LimitExceeded,
            ErrorCode = writer.ErrorCode,
            ErrorMessage = writer.ErrorMessage
        };
    }

    internal PerPrimitiveResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerPrimitiveResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
