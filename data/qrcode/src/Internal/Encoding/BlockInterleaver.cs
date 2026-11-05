namespace qrcode;

internal class BlockInterleaver
{
    internal static byte[] CreateCodewords(
        byte[] dataCodewords,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return CreateModel2Codewords(dataCodewords, version, errorCorrectionIndex);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return CreateMicroCodewords(dataCodewords, version, errorCorrectionIndex);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return CreateRectangularMicroCodewords(
                dataCodewords,
                version,
                errorCorrectionIndex);
        }

        if (symbolKind == QrSymbolKind.LegacyModel1)
        {
            return CreateLegacyModel1Codewords(dataCodewords, version, errorCorrectionIndex);
        }

        throw new QrInputException("The symbol kind is not defined.", "symbolKind");
    }

    internal static byte[] CreateModel2Codewords(
        byte[] dataCodewords,
        int version,
        int errorCorrectionIndex)
    {
        int expectedDataCount = EccBlockTable.GetDataCodewordCount(
            version,
            errorCorrectionIndex);
        ValidateDataCodewords(dataCodewords, expectedDataCount);
        int blockCount = EccBlockTable.GetBlockCount(version, errorCorrectionIndex);
        int shortBlockCount = EccBlockTable.GetShortBlockCount(
            version,
            errorCorrectionIndex);
        int shortDataCount = EccBlockTable.GetShortDataCodewordCount(
            version,
            errorCorrectionIndex);
        int eccCount = EccBlockTable.GetEccCodewordCountPerBlock(
            version,
            errorCorrectionIndex);
        return CreateInterleavedCodewords(
            dataCodewords,
            blockCount,
            shortBlockCount,
            shortDataCount,
            eccCount);
    }

    internal static byte[] CreateMicroCodewords(
        byte[] dataCodewords,
        int version,
        int errorCorrectionIndex)
    {
        int expectedDataCount = MicroVersionTable.GetDataSymbolCharacterCount(
            version,
            errorCorrectionIndex);
        ValidateDataCodewords(dataCodewords, expectedDataCount);
        int eccCount = MicroVersionTable.GetEccCodewordCount(
            version,
            errorCorrectionIndex);
        return CreateInterleavedCodewords(
            dataCodewords,
            1,
            1,
            expectedDataCount,
            eccCount);
    }

    internal static byte[] CreateRectangularMicroCodewords(
        byte[] dataCodewords,
        int versionIndex,
        int errorCorrectionIndex)
    {
        int expectedDataCount = RectangularMicroVersionTable.GetDataCodewordCount(
            versionIndex,
            errorCorrectionIndex);
        ValidateDataCodewords(dataCodewords, expectedDataCount);
        int blockCount = RectangularMicroVersionTable.GetBlockCount(
            versionIndex,
            errorCorrectionIndex);
        int shortBlockCount = RectangularMicroVersionTable.GetShortBlockCount(
            versionIndex,
            errorCorrectionIndex);
        int shortDataCount = RectangularMicroVersionTable.GetShortDataCodewordCount(
            versionIndex,
            errorCorrectionIndex);
        int eccCount = RectangularMicroVersionTable.GetEccCodewordCountPerBlock(
            versionIndex,
            errorCorrectionIndex);
        return CreateInterleavedCodewords(
            dataCodewords,
            blockCount,
            shortBlockCount,
            shortDataCount,
            eccCount);
    }

    internal static byte[] CreateLegacyModel1Codewords(
        byte[] dataCodewords,
        int version,
        int errorCorrectionIndex)
    {
        int expectedDataCount = LegacyModel1Table.GetDataCodewordCount(
            version,
            errorCorrectionIndex);
        ValidateDataCodewords(dataCodewords, expectedDataCount);
        int blockCount = LegacyModel1Table.GetErrorCorrectionBlockCount(
            version,
            errorCorrectionIndex);
        int dataCountPerBlock = LegacyModel1Table.GetDataCodewordCountPerBlock(
            version,
            errorCorrectionIndex);
        int eccCount = LegacyModel1Table.GetErrorCorrectionCodewordCountPerBlock(
            version,
            errorCorrectionIndex);
        byte[][] dataBlocks = SplitDataBlocks(
            dataCodewords,
            blockCount,
            blockCount,
            dataCountPerBlock);
        byte[][] parityBlocks = CreateParityBlocks(dataBlocks, eccCount);
        int remainderCount = LegacyModel1Table.GetRemainderCodewordCount(
            version,
            errorCorrectionIndex);
        return CreateSequentialCodewords(dataCodewords, parityBlocks, remainderCount);
    }

    static byte[] CreateInterleavedCodewords(
        byte[] dataCodewords,
        int blockCount,
        int shortBlockCount,
        int shortDataCount,
        int eccCount)
    {
        byte[][] dataBlocks = SplitDataBlocks(
            dataCodewords,
            blockCount,
            shortBlockCount,
            shortDataCount);
        byte[][] parityBlocks = CreateParityBlocks(dataBlocks, eccCount);
        byte[] result = new byte[dataCodewords.Length + blockCount * eccCount];
        int resultIndex = 0;
        int maximumDataCount = shortDataCount;
        if (shortBlockCount < blockCount)
        {
            maximumDataCount++;
        }

        for (int dataIndex = 0; dataIndex < maximumDataCount; dataIndex++)
        {
            for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                if (dataIndex < dataBlocks[blockIndex].Length)
                {
                    result[resultIndex] = dataBlocks[blockIndex][dataIndex];
                    resultIndex++;
                }
            }
        }

        for (int parityIndex = 0; parityIndex < eccCount; parityIndex++)
        {
            for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                result[resultIndex] = parityBlocks[blockIndex][parityIndex];
                resultIndex++;
            }
        }

        return result;
    }

    static byte[][] SplitDataBlocks(
        byte[] dataCodewords,
        int blockCount,
        int shortBlockCount,
        int shortDataCount)
    {
        byte[][] blocks = new byte[blockCount][];
        int dataIndex = 0;
        for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            int blockDataCount = shortDataCount;
            if (blockIndex >= shortBlockCount)
            {
                blockDataCount++;
            }

            byte[] block = new byte[blockDataCount];
            for (int blockDataIndex = 0;
                blockDataIndex < blockDataCount;
                blockDataIndex++)
            {
                block[blockDataIndex] = dataCodewords[dataIndex];
                dataIndex++;
            }

            blocks[blockIndex] = block;
        }

        return blocks;
    }

    static byte[][] CreateParityBlocks(byte[][] dataBlocks, int eccCount)
    {
        byte[][] parityBlocks = new byte[dataBlocks.Length][];
        for (int blockIndex = 0; blockIndex < dataBlocks.Length; blockIndex++)
        {
            parityBlocks[blockIndex] = ReedSolomonEncoder.CreateParity(
                dataBlocks[blockIndex],
                eccCount);
        }

        return parityBlocks;
    }

    static byte[] CreateSequentialCodewords(
        byte[] dataCodewords,
        byte[][] parityBlocks,
        int remainderCount)
    {
        int parityCount = 0;
        for (int blockIndex = 0; blockIndex < parityBlocks.Length; blockIndex++)
        {
            parityCount += parityBlocks[blockIndex].Length;
        }

        byte[] result = new byte[dataCodewords.Length + parityCount + remainderCount];
        int resultIndex = 0;
        for (int dataIndex = 0; dataIndex < dataCodewords.Length; dataIndex++)
        {
            result[resultIndex] = dataCodewords[dataIndex];
            resultIndex++;
        }

        for (int blockIndex = 0; blockIndex < parityBlocks.Length; blockIndex++)
        {
            for (int parityIndex = 0;
                parityIndex < parityBlocks[blockIndex].Length;
                parityIndex++)
            {
                result[resultIndex] = parityBlocks[blockIndex][parityIndex];
                resultIndex++;
            }
        }

        return result;
    }

    static void ValidateDataCodewords(byte[] dataCodewords, int expectedDataCount)
    {
        if (dataCodewords == null)
        {
            throw new QrInputException("The data codewords cannot be null.", "dataCodewords");
        }

        if (dataCodewords.Length != expectedDataCount)
        {
            throw new QrInputException(
                "The data codeword count does not match the selected symbol configuration.",
                "dataCodewords");
        }
    }
}
