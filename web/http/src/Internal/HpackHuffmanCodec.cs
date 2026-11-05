using System;

namespace http;

internal class HpackHuffmanResult
{
    internal HttpOperationStatus Status;
    internal byte[] Bytes = Array.Empty<byte>();
    internal int Consumed;
    internal int Written;
    internal int RequiredSize;
    internal bool HasErrorPosition;
    internal int ErrorPosition;
    internal long ErrorBitPosition;
    internal bool IsMalformed;
    internal bool IsEos;
    internal bool IsInvalidTransition;
    internal bool IsInvalidPadding;
    internal bool UsedHuffman;
    internal int RawLength;
    internal long HuffmanBitLength;
    internal long HuffmanByteLength;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal class HpackHuffmanDecoderTable
{
    internal int[] ZeroTransitions = Array.Empty<int>();
    internal int[] OneTransitions = Array.Empty<int>();
    internal int[] Symbols = Array.Empty<int>();
    internal int NodeCount;
}

internal static class HpackHuffmanCodec
{
    static readonly HpackHuffmanDecoderTable DecoderTable = CreateDecoderTable();

    internal static long CalculateEncodedLength(ReadOnlySpan<byte> input)
    {
        long bitLength = CalculateEncodedBitLength(input);
        return (bitLength + 7) / 8;
    }

    internal static HpackHuffmanResult Encode(ReadOnlySpan<byte> input)
    {
        long encodedLength = CalculateEncodedLength(input);
        if (encodedLength > int.MaxValue)
        {
            return RejectEncodedLengthOverflow(input.Length, encodedLength);
        }

        byte[] output = new byte[(int)encodedLength];
        HpackHuffmanResult result = Write(input, output, 0);
        if (result.Status == HttpOperationStatus.Done)
        {
            result.Bytes = output;
        }

        return result;
    }

    internal static HpackHuffmanResult Write(
        ReadOnlySpan<byte> input,
        Span<byte> output,
        int offset)
    {
        long bitLength = CalculateEncodedBitLength(input);
        long encodedLength = (bitLength + 7) / 8;
        if (encodedLength > int.MaxValue)
        {
            return RejectEncodedLengthOverflow(input.Length, encodedLength);
        }

        HpackHuffmanResult result = new HpackHuffmanResult();
        SetEncodingLengths(result, input.Length, bitLength, encodedLength);
        if (offset < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsMalformed = true;
            result.ErrorCode = "invalid-hpack-huffman-offset";
            result.ErrorMessage =
                "An HPACK Huffman output offset cannot be negative.";
            return result;
        }

        int encodedSize = (int)encodedLength;
        int requiredSize = int.MaxValue;
        if (encodedSize <= int.MaxValue - offset)
        {
            requiredSize = offset + encodedSize;
        }

        result.RequiredSize = requiredSize;
        if (offset > output.Length || encodedSize > output.Length - offset)
        {
            result.Status = HttpOperationStatus.NeedOutput;
            return result;
        }

        int outputIndex = offset;
        int current = 0;
        int currentLength = 0;
        int inputIndex = 0;
        while (inputIndex < input.Length)
        {
            int symbol = input[inputIndex];
            uint code = HpackStaticTable.HuffmanCodes[symbol];
            int codeLength = HpackStaticTable.HuffmanCodeLengths[symbol];
            int bitIndex = codeLength - 1;
            while (bitIndex >= 0)
            {
                current = current << 1;
                if ((code & (1u << bitIndex)) != 0)
                {
                    current = current | 1;
                }

                currentLength = currentLength + 1;
                if (currentLength == 8)
                {
                    output[outputIndex] = (byte)current;
                    outputIndex = outputIndex + 1;
                    current = 0;
                    currentLength = 0;
                }

                bitIndex = bitIndex - 1;
            }

            inputIndex = inputIndex + 1;
        }

        if (currentLength != 0)
        {
            int paddingLength = 8 - currentLength;
            current = current << paddingLength;
            current = current | (1 << paddingLength) - 1;
            output[outputIndex] = (byte)current;
        }

        result.Status = HttpOperationStatus.Done;
        result.Consumed = input.Length;
        result.Written = encodedSize;
        result.UsedHuffman = true;
        return result;
    }

    internal static HpackHuffmanResult SelectString(
        ReadOnlySpan<byte> input,
        HpackOptions options)
    {
        if (options == null)
        {
            HpackHuffmanResult invalid = new HpackHuffmanResult();
            invalid.Status = HttpOperationStatus.Rejected;
            invalid.IsMalformed = true;
            invalid.ErrorCode = "invalid-hpack-huffman-options";
            invalid.ErrorMessage = "HPACK string encoding options are required.";
            return invalid;
        }

        long bitLength = CalculateEncodedBitLength(input);
        long encodedLength = (bitLength + 7) / 8;
        bool useHuffman = options.UseHuffman &&
            (!options.UseHuffmanOnlyWhenSmaller || encodedLength < input.Length);
        if (useHuffman)
        {
            return Encode(input);
        }

        HpackHuffmanResult result = new HpackHuffmanResult();
        SetEncodingLengths(result, input.Length, bitLength, encodedLength);
        byte[] output = new byte[input.Length];
        int index = 0;
        while (index < input.Length)
        {
            output[index] = input[index];
            index = index + 1;
        }

        result.Status = HttpOperationStatus.Done;
        result.Bytes = output;
        result.Consumed = input.Length;
        result.Written = input.Length;
        result.RequiredSize = input.Length;
        return result;
    }

    internal static HpackHuffmanResult Decode(
        ReadOnlySpan<byte> input,
        int maximumDecodedSize)
    {
        if (maximumDecodedSize < 0)
        {
            HpackHuffmanResult invalid = new HpackHuffmanResult();
            invalid.Status = HttpOperationStatus.Rejected;
            invalid.ErrorCode = "invalid-hpack-huffman-output-limit";
            invalid.ErrorMessage =
                "An HPACK Huffman output limit cannot be negative.";
            return invalid;
        }

        HpackHuffmanResult measured = DecodeCore(
            input,
            Span<byte>.Empty,
            false);
        if (measured.Status != HttpOperationStatus.Done)
        {
            return measured;
        }

        if (measured.Written > maximumDecodedSize)
        {
            measured.Status = HttpOperationStatus.Rejected;
            measured.ErrorCode = "hpack-string-limit-exceeded";
            measured.ErrorMessage =
                "The decoded HPACK Huffman string exceeds the configured limit.";
            return measured;
        }

        byte[] output = new byte[measured.Written];
        HpackHuffmanResult decoded = DecodeCore(input, output, true);
        if (decoded.Status == HttpOperationStatus.Done)
        {
            decoded.Bytes = output;
        }

        return decoded;
    }

    static long CalculateEncodedBitLength(ReadOnlySpan<byte> input)
    {
        long bitLength = 0;
        int index = 0;
        while (index < input.Length)
        {
            bitLength = bitLength +
                HpackStaticTable.HuffmanCodeLengths[input[index]];
            index = index + 1;
        }

        return bitLength;
    }

    static HpackHuffmanResult DecodeCore(
        ReadOnlySpan<byte> input,
        Span<byte> output,
        bool writeOutput)
    {
        HpackHuffmanResult result = new HpackHuffmanResult();
        int state = 0;
        int outputCount = 0;
        int terminalBitCount = 0;
        bool terminalBitsAreOnes = true;
        int inputIndex = 0;
        while (inputIndex < input.Length)
        {
            byte current = input[inputIndex];
            int mask = 0x80;
            int bitOffset = 0;
            while (mask != 0)
            {
                bool isOne = (current & mask) != 0;
                int nextState = DecoderTable.ZeroTransitions[state];
                if (isOne)
                {
                    nextState = DecoderTable.OneTransitions[state];
                }

                if (nextState == 0)
                {
                    return RejectInvalidTransition(
                        result,
                        inputIndex,
                        bitOffset,
                        outputCount);
                }

                terminalBitCount = terminalBitCount + 1;
                if (!isOne)
                {
                    terminalBitsAreOnes = false;
                }

                int symbolMarker = DecoderTable.Symbols[nextState];
                if (symbolMarker != 0)
                {
                    int symbol = symbolMarker - 1;
                    if (symbol == 256)
                    {
                        return RejectEos(
                            result,
                            inputIndex,
                            bitOffset,
                            outputCount);
                    }

                    if (outputCount == int.MaxValue)
                    {
                        return RejectOutputOverflow(
                            result,
                            inputIndex,
                            bitOffset,
                            outputCount);
                    }

                    if (writeOutput)
                    {
                        output[outputCount] = (byte)symbol;
                    }

                    outputCount = outputCount + 1;
                    state = 0;
                    terminalBitCount = 0;
                    terminalBitsAreOnes = true;
                }
                else
                {
                    state = nextState;
                }

                mask = mask >> 1;
                bitOffset = bitOffset + 1;
            }

            inputIndex = inputIndex + 1;
        }

        if (state != 0 &&
            (terminalBitCount > 7 || !terminalBitsAreOnes))
        {
            return RejectInvalidPadding(
                result,
                input.Length,
                outputCount,
                terminalBitCount);
        }

        result.Status = HttpOperationStatus.Done;
        result.Bytes = Array.Empty<byte>();
        result.Consumed = input.Length;
        result.Written = outputCount;
        result.RequiredSize = outputCount;
        return result;
    }

    static HpackHuffmanDecoderTable CreateDecoderTable()
    {
        uint[] codes = HpackStaticTable.HuffmanCodes;
        byte[] lengths = HpackStaticTable.HuffmanCodeLengths;
        if (codes.Length != 257 || lengths.Length != 257)
        {
            throw new InvalidOperationException(
                "The HPACK Huffman code table must contain 257 symbols.");
        }

        int maximumNodeCount = 1;
        int lengthIndex = 0;
        while (lengthIndex < lengths.Length)
        {
            maximumNodeCount = maximumNodeCount + lengths[lengthIndex];
            lengthIndex = lengthIndex + 1;
        }

        HpackHuffmanDecoderTable table = new HpackHuffmanDecoderTable();
        table.ZeroTransitions = new int[maximumNodeCount];
        table.OneTransitions = new int[maximumNodeCount];
        table.Symbols = new int[maximumNodeCount];
        table.NodeCount = 1;

        int symbol = 0;
        while (symbol < codes.Length)
        {
            int state = 0;
            int bitIndex = lengths[symbol] - 1;
            while (bitIndex >= 0)
            {
                if (table.Symbols[state] != 0)
                {
                    throw new InvalidOperationException(
                        "The HPACK Huffman table contains a prefix collision.");
                }

                bool isOne = (codes[symbol] & (1u << bitIndex)) != 0;
                int nextState = table.ZeroTransitions[state];
                if (isOne)
                {
                    nextState = table.OneTransitions[state];
                }

                if (nextState == 0)
                {
                    nextState = table.NodeCount;
                    table.NodeCount = table.NodeCount + 1;
                    if (isOne)
                    {
                        table.OneTransitions[state] = nextState;
                    }
                    else
                    {
                        table.ZeroTransitions[state] = nextState;
                    }
                }

                state = nextState;
                bitIndex = bitIndex - 1;
            }

            if (table.Symbols[state] != 0 ||
                table.ZeroTransitions[state] != 0 ||
                table.OneTransitions[state] != 0)
            {
                throw new InvalidOperationException(
                    "The HPACK Huffman table contains a duplicate or prefix collision.");
            }

            table.Symbols[state] = symbol + 1;
            symbol = symbol + 1;
        }

        return table;
    }

    static HpackHuffmanResult RejectInvalidTransition(
        HpackHuffmanResult result,
        int inputIndex,
        int bitOffset,
        int outputCount)
    {
        SetErrorPosition(result, inputIndex, bitOffset, outputCount);
        result.IsInvalidTransition = true;
        result.ErrorCode = "invalid-hpack-huffman-transition";
        result.ErrorMessage =
            "The HPACK Huffman input does not follow a defined code path.";
        return result;
    }

    static HpackHuffmanResult RejectEos(
        HpackHuffmanResult result,
        int inputIndex,
        int bitOffset,
        int outputCount)
    {
        SetErrorPosition(result, inputIndex, bitOffset, outputCount);
        result.IsEos = true;
        result.ErrorCode = "hpack-huffman-eos-symbol";
        result.ErrorMessage =
            "The reserved HPACK Huffman EOS symbol cannot occur in encoded data.";
        return result;
    }

    static HpackHuffmanResult RejectOutputOverflow(
        HpackHuffmanResult result,
        int inputIndex,
        int bitOffset,
        int outputCount)
    {
        SetErrorPosition(result, inputIndex, bitOffset, outputCount);
        result.ErrorCode = "hpack-huffman-output-overflow";
        result.ErrorMessage =
            "The decoded HPACK Huffman output exceeds the supported size.";
        return result;
    }

    static HpackHuffmanResult RejectEncodedLengthOverflow(
        int inputLength,
        long encodedLength)
    {
        HpackHuffmanResult result = new HpackHuffmanResult();
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = inputLength;
        result.RawLength = inputLength;
        result.HuffmanByteLength = encodedLength;
        result.IsMalformed = true;
        result.ErrorCode = "hpack-huffman-output-overflow";
        result.ErrorMessage =
            "The encoded HPACK Huffman output exceeds the supported size.";
        return result;
    }

    static HpackHuffmanResult RejectInvalidPadding(
        HpackHuffmanResult result,
        int inputLength,
        int outputCount,
        int terminalBitCount)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = inputLength;
        result.Written = outputCount;
        result.RequiredSize = outputCount;
        result.HasErrorPosition = true;
        result.ErrorPosition = inputLength == 0 ? 0 : inputLength - 1;
        result.ErrorBitPosition = (long)inputLength * 8 - terminalBitCount;
        result.IsMalformed = true;
        result.IsInvalidPadding = true;
        result.ErrorCode = "invalid-hpack-huffman-padding";
        result.ErrorMessage =
            "HPACK Huffman padding must be at most seven leading EOS bits.";
        return result;
    }

    static void SetErrorPosition(
        HpackHuffmanResult result,
        int inputIndex,
        int bitOffset,
        int outputCount)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = inputIndex + 1;
        result.Written = outputCount;
        result.RequiredSize = outputCount;
        result.HasErrorPosition = true;
        result.ErrorPosition = inputIndex;
        result.ErrorBitPosition = (long)inputIndex * 8 + bitOffset;
        result.IsMalformed = true;
    }

    static void SetEncodingLengths(
        HpackHuffmanResult result,
        int rawLength,
        long bitLength,
        long byteLength)
    {
        result.RawLength = rawLength;
        result.HuffmanBitLength = bitLength;
        result.HuffmanByteLength = byteLength;
    }
}
