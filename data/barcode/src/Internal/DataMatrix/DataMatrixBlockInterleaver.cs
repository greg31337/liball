namespace barcode;

internal class DataMatrixInterleavedBlock
{
    internal int Index;
    internal int DataCodewordCount;
    internal int ErrorCodewordCount;
    internal int[] Codewords = new int[0];

    internal int[] CopyDataCodewords()
    {
        int[] copy = new int[DataCodewordCount];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = Codewords[index];
            index = index + 1;
        }

        return copy;
    }

    internal int[] CopyErrorCodewords()
    {
        int[] copy = new int[ErrorCodewordCount];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = Codewords[DataCodewordCount + index];
            index = index + 1;
        }

        return copy;
    }

    internal int[] CopyCodewords()
    {
        int[] copy = new int[Codewords.Length];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = Codewords[index];
            index = index + 1;
        }

        return copy;
    }
}

internal static class DataMatrixBlockInterleaver
{
    internal static int[] EncodeSymbolCodewords(
        int[] dataCodewords,
        DataMatrixSymbolInfo symbol)
    {
        DataMatrixInterleavedBlock[] blocks = CreateEncodedBlocks(dataCodewords, symbol);
        return InterleaveBlocks(blocks, symbol);
    }

    internal static DataMatrixInterleavedBlock[] CreateEncodedBlocks(
        int[] dataCodewords,
        DataMatrixSymbolInfo symbol)
    {
        ValidateSymbol(symbol);
        ValidateCodewordArray(
            dataCodewords,
            symbol.DataCodewordCapacity,
            "Data Matrix data codewords do not match the symbol capacity.",
            true);
        DataMatrixInterleavedBlock[] blocks = CreateEmptyBlocks(symbol);
        int blockIndex = 0;
        while (blockIndex < blocks.Length)
        {
            DataMatrixInterleavedBlock block = blocks[blockIndex];
            int[] blockData = new int[block.DataCodewordCount];
            int dataIndex = 0;
            int sourceIndex = blockIndex;
            while (sourceIndex < dataCodewords.Length)
            {
                if (dataIndex >= blockData.Length)
                {
                    throw new BarcodeValidationException("Data Matrix data block receives too many interleaved codewords.");
                }

                blockData[dataIndex] = dataCodewords[sourceIndex];
                dataIndex = dataIndex + 1;
                sourceIndex = sourceIndex + blocks.Length;
            }

            if (dataIndex != blockData.Length)
            {
                throw new BarcodeValidationException("Data Matrix data block receives too few interleaved codewords.");
            }

            block.Codewords = DataMatrixReedSolomon.EncodeCodewords(
                blockData,
                block.ErrorCodewordCount);
            blockIndex = blockIndex + 1;
        }

        return blocks;
    }

    internal static DataMatrixInterleavedBlock[] DeinterleaveSymbolCodewords(
        int[] symbolCodewords,
        DataMatrixSymbolInfo symbol)
    {
        ValidateSymbol(symbol);
        ValidateCodewordArray(
            symbolCodewords,
            symbol.TotalCodewordCount,
            "Data Matrix received codewords do not match the symbol's total codeword count.",
            false);
        DataMatrixInterleavedBlock[] blocks = CreateEmptyBlocks(symbol);
        int dataPosition = 0;
        while (dataPosition < symbol.DataCodewordCapacity)
        {
            int blockIndex = dataPosition % blocks.Length;
            int blockDataIndex = dataPosition / blocks.Length;
            DataMatrixInterleavedBlock block = blocks[blockIndex];
            if (blockDataIndex >= block.DataCodewordCount)
            {
                throw new BarcodeValidationException("Data Matrix data interleave exceeds a block's data length.");
            }

            block.Codewords[blockDataIndex] = symbolCodewords[dataPosition];
            dataPosition = dataPosition + 1;
        }

        int sourceIndex = symbol.DataCodewordCapacity;
        int errorIndex = 0;
        int maximumErrorCount = FindMaximumErrorCount(blocks);
        while (errorIndex < maximumErrorCount)
        {
            int interleaveIndex = 0;
            while (interleaveIndex < blocks.Length)
            {
                int blockIndex = GetParityBlockIndex(symbol, interleaveIndex);
                DataMatrixInterleavedBlock block = blocks[blockIndex];
                if (errorIndex < block.ErrorCodewordCount)
                {
                    if (sourceIndex >= symbolCodewords.Length)
                    {
                        throw new BarcodeValidationException("Data Matrix parity interleave ends before every block is filled.");
                    }

                    block.Codewords[block.DataCodewordCount + errorIndex]
                        = symbolCodewords[sourceIndex];
                    sourceIndex = sourceIndex + 1;
                }

                interleaveIndex = interleaveIndex + 1;
            }

            errorIndex = errorIndex + 1;
        }

        if (sourceIndex != symbolCodewords.Length)
        {
            throw new BarcodeValidationException("Data Matrix parity interleave does not consume every received codeword.");
        }

        return blocks;
    }

    internal static int[] InterleaveBlocks(
        DataMatrixInterleavedBlock[] blocks,
        DataMatrixSymbolInfo symbol)
    {
        ValidateSymbol(symbol);
        ValidateBlocks(blocks, symbol);
        int[] codewords = new int[symbol.TotalCodewordCount];
        int dataPosition = 0;
        while (dataPosition < symbol.DataCodewordCapacity)
        {
            int blockIndex = dataPosition % blocks.Length;
            int blockDataIndex = dataPosition / blocks.Length;
            codewords[dataPosition] = blocks[blockIndex].Codewords[blockDataIndex];
            dataPosition = dataPosition + 1;
        }

        int targetIndex = symbol.DataCodewordCapacity;
        int errorIndex = 0;
        int maximumErrorCount = FindMaximumErrorCount(blocks);
        while (errorIndex < maximumErrorCount)
        {
            int interleaveIndex = 0;
            while (interleaveIndex < blocks.Length)
            {
                int blockIndex = GetParityBlockIndex(symbol, interleaveIndex);
                DataMatrixInterleavedBlock block = blocks[blockIndex];
                if (errorIndex < block.ErrorCodewordCount)
                {
                    codewords[targetIndex] = block.Codewords[block.DataCodewordCount + errorIndex];
                    targetIndex = targetIndex + 1;
                }

                interleaveIndex = interleaveIndex + 1;
            }

            errorIndex = errorIndex + 1;
        }

        if (targetIndex != codewords.Length)
        {
            throw new BarcodeValidationException("Data Matrix block interleave does not fill the symbol codeword stream.");
        }

        return codewords;
    }

    internal static int[] ExtractDataCodewords(
        DataMatrixInterleavedBlock[] blocks,
        DataMatrixSymbolInfo symbol)
    {
        ValidateSymbol(symbol);
        ValidateBlocks(blocks, symbol);
        int[] dataCodewords = new int[symbol.DataCodewordCapacity];
        int position = 0;
        while (position < dataCodewords.Length)
        {
            int blockIndex = position % blocks.Length;
            int blockDataIndex = position / blocks.Length;
            dataCodewords[position] = blocks[blockIndex].Codewords[blockDataIndex];
            position = position + 1;
        }

        return dataCodewords;
    }

    static DataMatrixInterleavedBlock[] CreateEmptyBlocks(DataMatrixSymbolInfo symbol)
    {
        DataMatrixInterleavedBlock[] blocks = new DataMatrixInterleavedBlock[symbol.InterleavedBlockCount];
        int blockIndex = 0;
        while (blockIndex < blocks.Length)
        {
            int dataCount = symbol.GetDataCodewordCountForBlock(blockIndex);
            int errorCount = symbol.GetErrorCodewordCountForBlock(blockIndex);
            DataMatrixInterleavedBlock block = new DataMatrixInterleavedBlock();
            block.Index = blockIndex;
            block.DataCodewordCount = dataCount;
            block.ErrorCodewordCount = errorCount;
            block.Codewords = new int[CheckedMath.Add(dataCount, errorCount)];
            blocks[blockIndex] = block;
            blockIndex = blockIndex + 1;
        }

        return blocks;
    }

    static int GetParityBlockIndex(
        DataMatrixSymbolInfo symbol,
        int interleaveIndex)
    {
        if (symbol.Version == 24)
        {
            return (interleaveIndex + 8) % symbol.InterleavedBlockCount;
        }

        return interleaveIndex;
    }

    static int FindMaximumErrorCount(DataMatrixInterleavedBlock[] blocks)
    {
        int maximum = 0;
        int index = 0;
        while (index < blocks.Length)
        {
            if (blocks[index].ErrorCodewordCount > maximum)
            {
                maximum = blocks[index].ErrorCodewordCount;
            }

            index = index + 1;
        }

        return maximum;
    }

    static void ValidateBlocks(
        DataMatrixInterleavedBlock[] blocks,
        DataMatrixSymbolInfo symbol)
    {
        if (blocks == null)
        {
            throw new BarcodeValidationException("Data Matrix interleaved blocks must not be null.");
        }

        if (blocks.Length != symbol.InterleavedBlockCount)
        {
            throw new BarcodeValidationException("Data Matrix interleaved block count does not match the symbol.");
        }

        int index = 0;
        while (index < blocks.Length)
        {
            DataMatrixInterleavedBlock block = blocks[index];
            if (block == null)
            {
                throw new BarcodeValidationException("Data Matrix interleaved block must not be null.");
            }

            int expectedDataCount = symbol.GetDataCodewordCountForBlock(index);
            int expectedErrorCount = symbol.GetErrorCodewordCountForBlock(index);
            if (block.Index != index
                || block.DataCodewordCount != expectedDataCount
                || block.ErrorCodewordCount != expectedErrorCount)
            {
                throw new BarcodeValidationException("Data Matrix interleaved block metadata does not match the symbol.");
            }

            ValidateCodewordArray(
                block.Codewords,
                CheckedMath.Add(expectedDataCount, expectedErrorCount),
                "Data Matrix interleaved block length does not match the symbol.",
                false);
            index = index + 1;
        }
    }

    static void ValidateCodewordArray(
        int[] codewords,
        int expectedCount,
        string countMessage,
        bool encodingInput)
    {
        if (codewords == null)
        {
            if (encodingInput)
            {
                throw new BarcodeEncodingException("Data Matrix data codewords must not be null.");
            }

            throw new BarcodeValidationException("Data Matrix codewords must not be null.");
        }

        if (codewords.Length != expectedCount)
        {
            if (encodingInput)
            {
                throw new BarcodeEncodingException(countMessage);
            }

            throw new BarcodeValidationException(countMessage);
        }

        int index = 0;
        while (index < codewords.Length)
        {
            if (codewords[index] < 0 || codewords[index] > 255)
            {
                if (encodingInput)
                {
                    throw new BarcodeEncodingException("Data Matrix data codeword is outside the byte range.");
                }

                throw new BarcodeValidationException("Data Matrix codeword is outside the byte range.");
            }

            index = index + 1;
        }
    }

    static void ValidateSymbol(DataMatrixSymbolInfo symbol)
    {
        if (symbol == null)
        {
            throw new BarcodeValidationException("Data Matrix block symbol information must not be null.");
        }

        if (symbol.InterleavedBlockCount <= 0
            || symbol.DataCodewordsPerBlock == null
            || symbol.ErrorCodewordsPerBlock == null
            || symbol.DataCodewordsPerBlock.Length != symbol.InterleavedBlockCount
            || symbol.ErrorCodewordsPerBlock.Length != symbol.InterleavedBlockCount)
        {
            throw new BarcodeValidationException("Data Matrix block metadata is incomplete.");
        }

        int dataTotal = 0;
        int errorTotal = 0;
        int maximumDataCount = 0;
        int minimumDataCount = int.MaxValue;
        int previousDataCount = int.MaxValue;
        int index = 0;
        while (index < symbol.InterleavedBlockCount)
        {
            int dataCount = symbol.DataCodewordsPerBlock[index];
            int errorCount = symbol.ErrorCodewordsPerBlock[index];
            if (dataCount <= 0
                || !DataMatrixReedSolomon.IsSupportedErrorCorrectionCount(errorCount))
            {
                throw new BarcodeValidationException("Data Matrix block sizes are invalid.");
            }

            if (dataCount > previousDataCount)
            {
                throw new BarcodeValidationException("Data Matrix longer data blocks must precede shorter blocks.");
            }

            if (dataCount > maximumDataCount)
            {
                maximumDataCount = dataCount;
            }

            if (dataCount < minimumDataCount)
            {
                minimumDataCount = dataCount;
            }

            dataTotal = CheckedMath.Add(dataTotal, dataCount);
            errorTotal = CheckedMath.Add(errorTotal, errorCount);
            previousDataCount = dataCount;
            index = index + 1;
        }

        if (maximumDataCount - minimumDataCount > 1
            || dataTotal != symbol.DataCodewordCapacity
            || errorTotal != symbol.ErrorCodewordCount
            || CheckedMath.Add(dataTotal, errorTotal) != symbol.TotalCodewordCount)
        {
            throw new BarcodeValidationException("Data Matrix block totals do not match the symbol capacity.");
        }

        if (symbol.Version == 24
            && (symbol.InterleavedBlockCount != 10
                || symbol.DataCodewordsPerBlock[7] != 156
                || symbol.DataCodewordsPerBlock[8] != 155))
        {
            throw new BarcodeValidationException("Data Matrix 144 by 144 block rotation metadata is invalid.");
        }
    }
}
