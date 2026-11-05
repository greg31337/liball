namespace raw;

internal static class PackBitsCodec
{
    internal static byte[] Decode(System.ReadOnlySpan<byte> source, int decodedLength, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        if (decodedLength < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The PackBits decoded length cannot be negative.", -1);
        }
        CheckedMath.AllocationSize(decodedLength, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] destination = new byte[decodedLength];
        Decode(source, destination, cancellationToken);
        return destination;
    }

    internal static int Decode(System.ReadOnlySpan<byte> source, System.Span<byte> destination, System.Threading.CancellationToken cancellationToken)
    {
        ValidateDecode(source, destination.Length, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        int inputOffset = 0;
        int outputOffset = 0;
        while (inputOffset < source.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int control = BitUtility.ToInt8Bits(source[inputOffset]);
            inputOffset++;
            if (control >= 0)
            {
                int literalLength = control + 1;
                source.Slice(inputOffset, literalLength).CopyTo(destination.Slice(outputOffset, literalLength));
                inputOffset += literalLength;
                outputOffset += literalLength;
            }
            else if (control >= -127)
            {
                int repeatLength = 1 - control;
                byte value = source[inputOffset];
                inputOffset++;
                destination.Slice(outputOffset, repeatLength).Fill(value);
                outputOffset += repeatLength;
            }
        }
        return inputOffset;
    }

    internal static byte[] Encode(System.ReadOnlySpan<byte> source, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        int encodedLength = GetEncodedLength(source, cancellationToken);
        CheckedMath.AllocationSize(encodedLength, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] destination = new byte[encodedLength];
        Encode(source, destination, cancellationToken);
        return destination;
    }

    internal static int Encode(System.ReadOnlySpan<byte> source, System.Span<byte> destination, System.Threading.CancellationToken cancellationToken)
    {
        int encodedLength = GetEncodedLength(source, cancellationToken);
        if (destination.Length < encodedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The PackBits destination is too short for the encoded data.", -1);
        }
        cancellationToken.ThrowIfCancellationRequested();

        int inputOffset = 0;
        int outputOffset = 0;
        while (inputOffset < source.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int repeatLength = GetRepeatLength(source, inputOffset);
            if (repeatLength >= 3)
            {
                destination[outputOffset] = BitUtility.WrapUInt8(1 - repeatLength);
                destination[outputOffset + 1] = source[inputOffset];
                inputOffset += repeatLength;
                outputOffset += 2;
                continue;
            }

            int literalStart = inputOffset;
            int literalLength = GetLiteralLength(source, inputOffset);
            destination[outputOffset] = (byte)(literalLength - 1);
            source.Slice(literalStart, literalLength).CopyTo(destination.Slice(outputOffset + 1, literalLength));
            inputOffset += literalLength;
            outputOffset += literalLength + 1;
        }
        return outputOffset;
    }

    internal static int GetEncodedLength(System.ReadOnlySpan<byte> source, System.Threading.CancellationToken cancellationToken)
    {
        long encodedLength = 0;
        int inputOffset = 0;
        while (inputOffset < source.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int repeatLength = GetRepeatLength(source, inputOffset);
            if (repeatLength >= 3)
            {
                encodedLength = CheckedMath.Add(encodedLength, 2);
                inputOffset += repeatLength;
            }
            else
            {
                int literalLength = GetLiteralLength(source, inputOffset);
                encodedLength = CheckedMath.Add(encodedLength, literalLength + 1);
                inputOffset += literalLength;
            }
        }
        return CheckedMath.ToInt32(encodedLength);
    }

    internal static void ValidateDecode(System.ReadOnlySpan<byte> source, int decodedLength, System.Threading.CancellationToken cancellationToken)
    {
        if (decodedLength < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The PackBits decoded length cannot be negative.", -1);
        }

        int inputOffset = 0;
        int outputOffset = 0;
        while (inputOffset < source.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int controlOffset = inputOffset;
            int control = BitUtility.ToInt8Bits(source[inputOffset]);
            inputOffset++;
            if (control == -128)
            {
                continue;
            }

            int runLength;
            if (control >= 0)
            {
                runLength = control + 1;
                if (source.Length - inputOffset < runLength)
                {
                    throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "A PackBits literal run extends beyond the compressed input.", source.Length);
                }
                inputOffset += runLength;
            }
            else
            {
                runLength = 1 - control;
                if (inputOffset >= source.Length)
                {
                    throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "A PackBits repeat run is missing its byte value.", source.Length);
                }
                inputOffset++;
            }

            if (runLength > decodedLength - outputOffset)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A PackBits run exceeds the declared decoded length.", controlOffset);
            }
            outputOffset += runLength;
        }

        if (outputOffset != decodedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The PackBits input ends before the declared decoded length is produced.", source.Length);
        }
    }

    internal static int GetRepeatLength(System.ReadOnlySpan<byte> source, int offset)
    {
        int length = 1;
        while (length < 128 && offset + length < source.Length && source[offset + length] == source[offset])
        {
            length++;
        }
        return length;
    }

    internal static int GetLiteralLength(System.ReadOnlySpan<byte> source, int offset)
    {
        int length = 0;
        int position = offset;
        while (position < source.Length && length < 128)
        {
            int repeatLength = GetRepeatLength(source, position);
            if (repeatLength >= 3)
            {
                break;
            }
            int available = 128 - length;
            int consumed = repeatLength;
            if (consumed > available)
            {
                consumed = available;
            }
            position += consumed;
            length += consumed;
        }
        return length;
    }
}
