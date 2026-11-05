namespace asn1;

internal class EcnBitFieldOperations
{
    internal EcnBitField CreateFromBytes(byte[] data, int bitCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (bitCount < 0 || bitCount > data.Length * 8)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }

        byte[] copied = new byte[(bitCount + 7) / 8];
        if (copied.Length > 0)
        {
            System.Buffer.BlockCopy(data, 0, copied, 0, copied.Length);
            int remainder = bitCount % 8;
            if (remainder != 0)
            {
                copied[copied.Length - 1] = (byte)(copied[copied.Length - 1]
                    & 255 << 8 - remainder);
            }
        }

        return new EcnBitField
        {
            Data = copied,
            BitCount = bitCount
        };
    }

    internal EcnBitField FromBits(bool[] bits)
    {
        byte[] data = new byte[(bits.Length + 7) / 8];
        int index = 0;
        while (index < bits.Length)
        {
            if (bits[index])
            {
                data[index / 8] = (byte)(data[index / 8]
                    | 1 << 7 - index % 8);
            }

            index = index + 1;
        }

        return new EcnBitField
        {
            Data = data,
            BitCount = bits.Length
        };
    }

    internal bool[] ToBits(EcnBitField field)
    {
        bool[] bits = new bool[field.BitCount];
        int index = 0;
        while (index < bits.Length)
        {
            bits[index] = (field.Data[index / 8]
                & 1 << 7 - index % 8) != 0;
            index = index + 1;
        }

        return bits;
    }

    internal EcnBitField AppendFields(EcnBitField first, EcnBitField second)
    {
        bool[] left = this.ToBits(first);
        bool[] right = this.ToBits(second);
        bool[] combined = new bool[left.Length + right.Length];
        int index = 0;
        while (index < left.Length)
        {
            combined[index] = left[index];
            index = index + 1;
        }

        int rightIndex = 0;
        while (rightIndex < right.Length)
        {
            combined[left.Length + rightIndex] = right[rightIndex];
            rightIndex = rightIndex + 1;
        }

        return this.FromBits(combined);
    }

    internal EcnBitTransformResult FitField(
        EcnBitField field,
        EcnPrimitiveSettings settings,
        bool signExtension)
    {
        if (settings.SizeBits < 0)
        {
            return this.CreateTransformSuccess(field);
        }

        if (field.BitCount > settings.SizeBits)
        {
            return this.CreateTransformError(
                "The primitive value does not fit the ECN encoding space.");
        }

        int paddingCount = settings.SizeBits - field.BitCount;
        if (paddingCount == 0)
        {
            return this.CreateTransformSuccess(field);
        }

        bool[] source = this.ToBits(field);
        bool[] padded = new bool[settings.SizeBits];
        bool[] pattern = this.GetPaddingBits(settings, signExtension);
        bool right = settings.Justification != "left";
        int sourceStart = right ? paddingCount : 0;
        int index = 0;
        while (index < source.Length)
        {
            padded[sourceStart + index] = source[index];
            index = index + 1;
        }

        int paddingIndex = 0;
        while (paddingIndex < paddingCount)
        {
            int target = right
                ? paddingIndex
                : source.Length + paddingIndex;
            padded[target] = pattern[paddingIndex % pattern.Length];
            paddingIndex = paddingIndex + 1;
        }

        return this.CreateTransformSuccess(this.FromBits(padded));
    }

    internal bool[] GetPaddingBits(
        EcnPrimitiveSettings settings,
        bool signExtension)
    {
        if (signExtension)
        {
            return [true];
        }

        if (settings.Padding == "one")
        {
            return [true];
        }

        if (settings.Padding == "pattern")
        {
            bool[] pattern = this.ToBits(settings.Pattern);
            return pattern.Length == 0 ? [false] : pattern;
        }

        return [false];
    }

    internal EcnBitTransformResult RemoveUnusedBits(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        if (!settings.HasUnusedBits || settings.UnusedBits == 0)
        {
            return this.CreateTransformSuccess(field);
        }

        if (settings.UnusedBits > field.BitCount)
        {
            return this.CreateTransformError(
                "UNUSED BITS exceeds the decoded ECN field size.");
        }

        int start = settings.Justification == "left"
            ? 0
            : settings.UnusedBits;
        return this.CreateTransformSuccess(this.SliceField(
            field,
            start,
            field.BitCount - settings.UnusedBits));
    }

    internal EcnBitTransformResult RemoveTerminator(
        EcnBitField field,
        EcnBitField terminator)
    {
        if (terminator.BitCount == 0)
        {
            return this.CreateTransformSuccess(field);
        }

        if (field.BitCount < terminator.BitCount
            || !this.EndsWith(field, terminator))
        {
            return this.CreateTransformError(
                "The ECN primitive field does not end with its termination pattern.");
        }

        return this.CreateTransformSuccess(this.SliceField(
            field,
            0,
            field.BitCount - terminator.BitCount));
    }

    internal bool EndsWith(EcnBitField field, EcnBitField suffix)
    {
        bool[] bits = this.ToBits(field);
        bool[] expected = this.ToBits(suffix);
        int start = bits.Length - expected.Length;
        if (start < 0)
        {
            return false;
        }

        int index = 0;
        while (index < expected.Length)
        {
            if (bits[start + index] != expected[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal EcnBitField SliceField(EcnBitField field, int start, int count)
    {
        bool[] bits = this.ToBits(field);
        bool[] sliced = new bool[count];
        int index = 0;
        while (index < count)
        {
            sliced[index] = bits[start + index];
            index = index + 1;
        }

        return this.FromBits(sliced);
    }

    internal EcnBitTransformResult ApplyEncodingTransforms(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnBitField transformed = field;
        if (settings.ValueReversal)
        {
            transformed = this.ReverseField(transformed);
        }

        EcnBitTransformResult reversal = this.ApplyBitReversal(
            transformed,
            settings);
        if (!reversal.Success || reversal.Field == null)
        {
            return reversal;
        }

        transformed = reversal.Field;
        if (settings.ByteOrder == "little-endian")
        {
            if (transformed.BitCount % 8 != 0)
            {
                return this.CreateTransformError(
                    "Little-endian byte order requires an octet-sized ECN field.");
            }

            transformed = this.ReverseUnits(transformed, 8, true);
        }

        if (settings.BitOrder == "least-significant-first"
            || settings.BitOrder == "lsb-first")
        {
            if (transformed.BitCount % 8 != 0)
            {
                return this.CreateTransformError(
                    "Least-significant-first bit order requires an octet-sized ECN field.");
            }

            transformed = this.ReverseUnits(transformed, 8, false);
        }

        return this.CreateTransformSuccess(transformed);
    }

    internal EcnBitTransformResult ApplyDecodingTransforms(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        EcnBitField transformed = field;
        if (settings.BitOrder == "least-significant-first"
            || settings.BitOrder == "lsb-first")
        {
            if (transformed.BitCount % 8 != 0)
            {
                return this.CreateTransformError(
                    "Least-significant-first bit order requires an octet-sized ECN field.");
            }

            transformed = this.ReverseUnits(transformed, 8, false);
        }

        if (settings.ByteOrder == "little-endian")
        {
            if (transformed.BitCount % 8 != 0)
            {
                return this.CreateTransformError(
                    "Little-endian byte order requires an octet-sized ECN field.");
            }

            transformed = this.ReverseUnits(transformed, 8, true);
        }

        EcnBitTransformResult reversal = this.ApplyBitReversal(
            transformed,
            settings);
        if (!reversal.Success || reversal.Field == null)
        {
            return reversal;
        }

        transformed = reversal.Field;
        if (settings.ValueReversal)
        {
            transformed = this.ReverseField(transformed);
        }

        return this.CreateTransformSuccess(transformed);
    }

    internal EcnBitTransformResult ApplyBitReversal(
        EcnBitField field,
        EcnPrimitiveSettings settings)
    {
        if (settings.BitReversal == "no-reversal"
            || settings.BitReversal.Length == 0)
        {
            return this.CreateTransformSuccess(field);
        }

        int unit = settings.UnitBits <= 0 ? 8 : settings.UnitBits;
        if (field.BitCount % unit != 0)
        {
            return this.CreateTransformError(
                "Bit reversal requires a field that is a whole number of units.");
        }

        if (settings.BitReversal == "reverse-bits-in-units")
        {
            return this.CreateTransformSuccess(this.ReverseUnits(
                field,
                unit,
                false));
        }

        if (settings.BitReversal == "reverse-half-units")
        {
            if (unit % 2 != 0)
            {
                return this.CreateTransformError(
                    "Half-unit reversal requires an even-sized unit.");
            }

            return this.CreateTransformSuccess(this.ReverseHalves(
                field,
                unit,
                false));
        }

        if (settings.BitReversal == "reverse-bits-in-half-units")
        {
            if (unit % 2 != 0)
            {
                return this.CreateTransformError(
                    "Half-unit reversal requires an even-sized unit.");
            }

            return this.CreateTransformSuccess(this.ReverseHalves(
                field,
                unit,
                true));
        }

        return this.CreateTransformError(
            "The ECN bit-reversal setting is not supported.");
    }

    internal EcnBitField ReverseField(EcnBitField field)
    {
        bool[] bits = this.ToBits(field);
        int index = 0;
        while (index < bits.Length / 2)
        {
            bool temporary = bits[index];
            bits[index] = bits[bits.Length - index - 1];
            bits[bits.Length - index - 1] = temporary;
            index = index + 1;
        }

        return this.FromBits(bits);
    }

    internal EcnBitField ReverseUnits(
        EcnBitField field,
        int unit,
        bool reverseUnitOrder)
    {
        bool[] source = this.ToBits(field);
        bool[] result = new bool[source.Length];
        int unitCount = source.Length / unit;
        int unitIndex = 0;
        while (unitIndex < unitCount)
        {
            int targetUnit = reverseUnitOrder
                ? unitCount - unitIndex - 1
                : unitIndex;
            int bitIndex = 0;
            while (bitIndex < unit)
            {
                int sourceBit = reverseUnitOrder
                    ? bitIndex
                    : unit - bitIndex - 1;
                result[targetUnit * unit + bitIndex] =
                    source[unitIndex * unit + sourceBit];
                bitIndex = bitIndex + 1;
            }

            unitIndex = unitIndex + 1;
        }

        return this.FromBits(result);
    }

    internal EcnBitField ReverseHalves(
        EcnBitField field,
        int unit,
        bool reverseWithinHalves)
    {
        bool[] source = this.ToBits(field);
        bool[] result = new bool[source.Length];
        int half = unit / 2;
        int unitStart = 0;
        while (unitStart < source.Length)
        {
            int index = 0;
            while (index < unit)
            {
                int halfStart = index < half ? 0 : half;
                int within = index - halfStart;
                int sourceWithin = reverseWithinHalves
                    ? half - within - 1
                    : within;
                int sourceHalf = reverseWithinHalves
                    ? halfStart
                    : half - halfStart;
                result[unitStart + index] = source[
                    unitStart + sourceHalf + sourceWithin];
                index = index + 1;
            }

            unitStart = unitStart + unit;
        }

        return this.FromBits(result);
    }

    internal bool FieldsEqual(EcnBitField left, EcnBitField right)
    {
        if (left.BitCount != right.BitCount)
        {
            return false;
        }

        bool[] leftBits = this.ToBits(left);
        bool[] rightBits = this.ToBits(right);
        int index = 0;
        while (index < leftBits.Length)
        {
            if (leftBits[index] != rightBits[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal EcnBitTransformResult CreateTransformSuccess(EcnBitField field)
    {
        return new EcnBitTransformResult
        {
            Success = true,
            Field = field
        };
    }

    internal EcnBitTransformResult CreateTransformError(string message)
    {
        return new EcnBitTransformResult
        {
            ErrorMessage = message
        };
    }
}
