namespace raw;

internal static class LzwCodec
{
    internal const int ClearCode = 256;
    internal const int EndCode = 257;
    internal const int FirstCode = 258;
    internal const int MaximumCode = 4095;

    internal static byte[] Decode(System.ReadOnlyMemory<byte> source, int decodedLength, int earlyChange, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateEarlyChange(earlyChange);
        if (decodedLength < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The LZW decoded length cannot be negative.", -1);
        }
        CheckedMath.AllocationSize(decodedLength, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] destination = new byte[decodedLength];
        DecodeCore(source, destination, earlyChange, cancellationToken);
        return destination;
    }

    internal static byte[] DecodeTiff(System.ReadOnlyMemory<byte> source, int earlyChange, TiffPredictorParameters predictorParameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        int decodedLength = TiffPredictorCodec.GetDecodedLength(predictorParameters, limits);
        byte[] destination = Decode(source, decodedLength, earlyChange, limits, cancellationToken);
        TiffPredictorCodec.Decode(destination, predictorParameters, limits, cancellationToken);
        return destination;
    }

    internal static byte[] Encode(System.ReadOnlySpan<byte> source, int earlyChange, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        int encodedLength = GetEncodedLength(source, earlyChange, cancellationToken);
        CheckedMath.AllocationSize(encodedLength, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] destination = new byte[encodedLength];
        Encode(source, destination, earlyChange, cancellationToken);
        return destination;
    }

    internal static byte[] EncodeTiff(System.ReadOnlySpan<byte> source, int earlyChange, TiffPredictorParameters predictorParameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        int expectedLength = TiffPredictorCodec.GetDecodedLength(predictorParameters, limits);
        if (source.Length != expectedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The LZW input length does not match its TIFF predictor geometry.", -1);
        }
        CheckedMath.AllocationSize(source.Length, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] predicted = source.ToArray();
        TiffPredictorCodec.Encode(predicted, predictorParameters, limits, cancellationToken);
        return Encode(predicted, earlyChange, limits, cancellationToken);
    }

    internal static int Encode(System.ReadOnlySpan<byte> source, System.Memory<byte> destination, int earlyChange, System.Threading.CancellationToken cancellationToken)
    {
        int encodedLength = GetEncodedLength(source, earlyChange, cancellationToken);
        if (destination.Length < encodedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The LZW destination is too short for the encoded data.", -1);
        }
        cancellationToken.ThrowIfCancellationRequested();
        MsbBitWriter writer = new MsbBitWriter(destination.Slice(0, encodedLength));
        LzwCodeSink sink = new LzwCodeSink(writer);
        EncodeCore(source, earlyChange, sink, cancellationToken);
        int written = CheckedMath.ToInt32(writer.Finish(0));
        if (written != encodedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The LZW encoder produced an inconsistent byte count.", -1);
        }
        return written;
    }

    internal static int GetEncodedLength(System.ReadOnlySpan<byte> source, int earlyChange, System.Threading.CancellationToken cancellationToken)
    {
        ValidateEarlyChange(earlyChange);
        LzwCodeSink sink = new LzwCodeSink(null);
        EncodeCore(source, earlyChange, sink, cancellationToken);
        return sink.GetByteCount();
    }

    internal static void DecodeCore(System.ReadOnlyMemory<byte> source, System.Span<byte> destination, int earlyChange, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LzwBitReader reader = new LzwBitReader(source);
        int[] prefixes = new int[4096];
        byte[] suffixes = new byte[4096];
        byte[] stack = new byte[4096];
        int codeWidth = 9;
        int nextCode = FirstCode;
        int previousCode = -1;
        int outputOffset = 0;
        int first = reader.ReadCode(codeWidth);
        if (first != ClearCode)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A TIFF LZW strip must begin with a Clear code.", 0);
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int codeOffset = reader.GetByteOffset();
            int code = reader.ReadCode(codeWidth);
            if (code == ClearCode)
            {
                codeWidth = 9;
                nextCode = FirstCode;
                previousCode = -1;
                continue;
            }
            if (code == EndCode)
            {
                if (outputOffset != destination.Length)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The LZW EndOfInformation code occurs before the declared output is complete.", codeOffset);
                }
                reader.ValidateEnd();
                return;
            }
            if (code < 0 || code > nextCode || code == ClearCode || code == EndCode)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The LZW stream references a code that is not in the string table.", codeOffset);
            }
            if (previousCode < 0 && code >= 256)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The first LZW code after Clear must be a literal byte.", codeOffset);
            }

            int stackLength = Expand(code, nextCode, previousCode, prefixes, suffixes, stack, codeOffset);
            if (stackLength > destination.Length - outputOffset)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "An LZW string exceeds the declared decoded length.", codeOffset);
            }
            byte firstByte = stack[stackLength - 1];
            for (int index = stackLength - 1; index >= 0; index--)
            {
                destination[outputOffset] = stack[index];
                outputOffset++;
            }

            if (previousCode >= 0)
            {
                int maximumDecoderEntry = MaximumCode - earlyChange - 1;
                if (nextCode > maximumDecoderEntry)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The LZW string table was not cleared before it overflowed.", codeOffset);
                }
                prefixes[nextCode] = previousCode;
                suffixes[nextCode] = firstByte;
                nextCode++;
                int widthThreshold = (1 << codeWidth) - earlyChange;
                if (codeWidth < 12 && nextCode == widthThreshold)
                {
                    codeWidth++;
                }
            }
            previousCode = code;
        }
    }

    internal static int Expand(int code, int nextCode, int previousCode, int[] prefixes, byte[] suffixes, byte[] stack, int codeOffset)
    {
        int stackLength = 0;
        int current = code;
        if (code == nextCode)
        {
            if (previousCode < 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "An LZW special-case code has no preceding string.", codeOffset);
            }
            current = previousCode;
            int firstLength = Expand(previousCode, nextCode, -1, prefixes, suffixes, stack, codeOffset);
            byte firstByte = stack[firstLength - 1];
            stackLength = 1;
            stack[0] = firstByte;
        }

        int traversed = 0;
        while (current >= FirstCode)
        {
            if (current >= nextCode || stackLength >= stack.Length || traversed >= stack.Length)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The LZW string table contains an invalid prefix chain.", codeOffset);
            }
            stack[stackLength] = suffixes[current];
            stackLength++;
            current = prefixes[current];
            traversed++;
        }
        if (current < 0 || current > 255 || stackLength >= stack.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The LZW string table does not terminate in a literal byte.", codeOffset);
        }
        stack[stackLength] = (byte)current;
        stackLength++;
        return stackLength;
    }

    internal static void EncodeCore(System.ReadOnlySpan<byte> source, int earlyChange, LzwCodeSink sink, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        System.Collections.Generic.Dictionary<int, int> table = new System.Collections.Generic.Dictionary<int, int>();
        int codeWidth = 9;
        int nextCode = FirstCode;
        bool hasDataCode = false;
        sink.Write(ClearCode, codeWidth);
        if (source.Length == 0)
        {
            sink.Write(EndCode, codeWidth);
            return;
        }

        int prefix = source[0];
        for (int index = 1; index < source.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte suffix = source[index];
            int key = (prefix << 8) | suffix;
            if (table.ContainsKey(key))
            {
                prefix = table[key];
                continue;
            }

            sink.Write(prefix, codeWidth);
            hasDataCode = true;
            int maximumEncoderEntry = MaximumCode - earlyChange;
            if (nextCode <= maximumEncoderEntry)
            {
                table.Add(key, nextCode);
                int addedCode = nextCode;
                nextCode++;
                int widthThreshold = (1 << codeWidth) - earlyChange + 1;
                if (codeWidth < 12 && nextCode == widthThreshold)
                {
                    codeWidth++;
                }
                if (addedCode == maximumEncoderEntry)
                {
                    sink.Write(ClearCode, codeWidth);
                    table.Clear();
                    codeWidth = 9;
                    nextCode = FirstCode;
                    hasDataCode = false;
                }
            }
            prefix = suffix;
        }

        sink.Write(prefix, codeWidth);
        if (hasDataCode && codeWidth < 12 && nextCode == (1 << codeWidth) - earlyChange)
        {
            codeWidth++;
        }
        sink.Write(EndCode, codeWidth);
    }

    internal static void ValidateEarlyChange(int earlyChange)
    {
        if (earlyChange != 0 && earlyChange != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "LZW EarlyChange must be zero or one.", -1);
        }
    }
}

internal class LzwBitReader
{
    internal System.ReadOnlyMemory<byte> Source;
    internal int InputOffset;
    internal ulong Buffer;
    internal int BufferedBits;
    internal long BitPosition;

    internal LzwBitReader(System.ReadOnlyMemory<byte> source)
    {
        Source = source;
        InputOffset = 0;
        Buffer = 0;
        BufferedBits = 0;
        BitPosition = 0;
    }

    internal int ReadCode(int width)
    {
        while (BufferedBits < width)
        {
            if (InputOffset >= Source.Length)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The LZW input ends before a complete code is available.", Source.Length);
            }
            Buffer = (Buffer << 8) | Source.Span[InputOffset];
            BufferedBits += 8;
            InputOffset++;
        }
        int code = (int)((Buffer >> (BufferedBits - width)) & BitUtility.Mask(width));
        BufferedBits -= width;
        Buffer &= BitUtility.Mask(BufferedBits);
        BitPosition = CheckedMath.Add(BitPosition, width);
        return code;
    }

    internal int GetByteOffset()
    {
        return CheckedMath.ToInt32(BitPosition / 8);
    }

    internal void ValidateEnd()
    {
        int remainingBits = BufferedBits + (Source.Length - InputOffset) * 8;
        if (remainingBits > 7)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Bytes remain after the LZW EndOfInformation code.", GetByteOffset());
        }
        if (Buffer != 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Nonzero padding follows the LZW EndOfInformation code.", GetByteOffset());
        }
    }
}

internal class LzwCodeSink
{
    internal MsbBitWriter? Writer;
    internal long BitCount;

    internal LzwCodeSink(MsbBitWriter? writer)
    {
        Writer = writer;
        BitCount = 0;
    }

    internal void Write(int code, int width)
    {
        if (code < 0 || code >= 1 << width)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "An LZW code does not fit its active width.", -1);
        }
        if (Writer != null)
        {
            Writer.WriteBits((uint)code, width);
        }
        BitCount = CheckedMath.Add(BitCount, width);
    }

    internal int GetByteCount()
    {
        long bytes = CheckedMath.Add(BitCount, 7) / 8;
        return CheckedMath.ToInt32(bytes);
    }
}
