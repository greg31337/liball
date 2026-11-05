namespace qrcode;

internal class BlockDeinterleaver
{
    internal static BlockCorrectionResult CorrectCodewords(
        byte[] codewords,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex,
        int[] erasurePositions)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return CorrectModel2Codewords(
                codewords,
                version,
                errorCorrectionIndex,
                erasurePositions);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return CorrectMicroCodewords(
                codewords,
                version,
                errorCorrectionIndex,
                erasurePositions);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return CorrectRectangularMicroCodewords(
                codewords,
                version,
                errorCorrectionIndex,
                erasurePositions);
        }

        if (symbolKind == QrSymbolKind.LegacyModel1)
        {
            return CorrectLegacyModel1Codewords(
                codewords,
                version,
                errorCorrectionIndex,
                erasurePositions);
        }

        throw new QrInputException("The symbol kind is not defined.", "symbolKind");
    }

    internal static BlockCorrectionResult CorrectModel2Codewords(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        int[] erasurePositions)
    {
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
        int totalCodewordCount = Model2VersionTable.GetTotalCodewordCount(version);
        return CorrectInterleavedBlocks(
            codewords,
            erasurePositions,
            blockCount,
            shortBlockCount,
            shortDataCount,
            eccCount,
            totalCodewordCount);
    }

    internal static BlockCorrectionResult CorrectMicroCodewords(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        int[] erasurePositions)
    {
        int dataCount = MicroVersionTable.GetDataSymbolCharacterCount(
            version,
            errorCorrectionIndex);
        int eccCount = MicroVersionTable.GetEccCodewordCount(
            version,
            errorCorrectionIndex);
        return CorrectInterleavedBlocks(
            codewords,
            erasurePositions,
            1,
            1,
            dataCount,
            eccCount,
            dataCount + eccCount);
    }

    internal static BlockCorrectionResult CorrectRectangularMicroCodewords(
        byte[] codewords,
        int versionIndex,
        int errorCorrectionIndex,
        int[] erasurePositions)
    {
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
        int totalCodewordCount = RectangularMicroVersionTable.GetTotalCodewordCount(
            versionIndex);
        return CorrectInterleavedBlocks(
            codewords,
            erasurePositions,
            blockCount,
            shortBlockCount,
            shortDataCount,
            eccCount,
            totalCodewordCount);
    }

    internal static BlockCorrectionResult CorrectLegacyModel1Codewords(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        int[] erasurePositions)
    {
        int totalCodewordCount = LegacyModel1Table.GetTotalCodewordCount(version);
        int dataCount = LegacyModel1Table.GetDataCodewordCount(
            version,
            errorCorrectionIndex);
        int blockCount = LegacyModel1Table.GetErrorCorrectionBlockCount(
            version,
            errorCorrectionIndex);
        int dataCountPerBlock = LegacyModel1Table.GetDataCodewordCountPerBlock(
            version,
            errorCorrectionIndex);
        int eccCount = LegacyModel1Table.GetErrorCorrectionCodewordCountPerBlock(
            version,
            errorCorrectionIndex);
        int protectedCodewordCount = dataCount + blockCount * eccCount;
        ValidateInputs(
            codewords,
            erasurePositions,
            totalCodewordCount,
            protectedCodewordCount);

        byte[][] blocks = new byte[blockCount][];
        int[] dataLengths = new int[blockCount];
        int[] positionBlocks = new int[totalCodewordCount];
        int[] positionOffsets = new int[totalCodewordCount];
        for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            dataLengths[blockIndex] = dataCountPerBlock;
            byte[] block = new byte[dataCountPerBlock + eccCount];
            for (int dataIndex = 0; dataIndex < dataCountPerBlock; dataIndex++)
            {
                int sourceIndex = blockIndex * dataCountPerBlock + dataIndex;
                block[dataIndex] = codewords[sourceIndex];
                positionBlocks[sourceIndex] = blockIndex;
                positionOffsets[sourceIndex] = dataIndex;
            }

            for (int parityIndex = 0; parityIndex < eccCount; parityIndex++)
            {
                int sourceIndex = dataCount + blockIndex * eccCount + parityIndex;
                block[dataCountPerBlock + parityIndex] = codewords[sourceIndex];
                positionBlocks[sourceIndex] = blockIndex;
                positionOffsets[sourceIndex] = dataCountPerBlock + parityIndex;
            }

            blocks[blockIndex] = block;
        }

        int[][] blockErasures = CreateBlockErasures(
            erasurePositions,
            positionBlocks,
            positionOffsets,
            blockCount);
        QrCorrectionResult[] blockResults = CorrectBlocks(blocks, blockErasures, eccCount);
        int remainderCount = LegacyModel1Table.GetRemainderCodewordCount(
            version,
            errorCorrectionIndex);
        return CreateAggregateResult(
            codewords,
            blockResults,
            dataLengths,
            eccCount,
            false,
            remainderCount);
    }

    static BlockCorrectionResult CorrectInterleavedBlocks(
        byte[] codewords,
        int[] erasurePositions,
        int blockCount,
        int shortBlockCount,
        int shortDataCount,
        int eccCount,
        int totalCodewordCount)
    {
        ValidateInputs(
            codewords,
            erasurePositions,
            totalCodewordCount,
            totalCodewordCount);
        byte[][] blocks = new byte[blockCount][];
        int[] dataLengths = new int[blockCount];
        for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            int dataLength = shortDataCount;
            if (blockIndex >= shortBlockCount)
            {
                dataLength++;
            }

            dataLengths[blockIndex] = dataLength;
            blocks[blockIndex] = new byte[dataLength + eccCount];
        }

        int[] positionBlocks = new int[totalCodewordCount];
        int[] positionOffsets = new int[totalCodewordCount];
        int sourceIndex = 0;
        int maximumDataCount = shortDataCount;
        if (shortBlockCount < blockCount)
        {
            maximumDataCount++;
        }

        for (int dataIndex = 0; dataIndex < maximumDataCount; dataIndex++)
        {
            for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                if (dataIndex < dataLengths[blockIndex])
                {
                    blocks[blockIndex][dataIndex] = codewords[sourceIndex];
                    positionBlocks[sourceIndex] = blockIndex;
                    positionOffsets[sourceIndex] = dataIndex;
                    sourceIndex++;
                }
            }
        }

        for (int parityIndex = 0; parityIndex < eccCount; parityIndex++)
        {
            for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                int blockOffset = dataLengths[blockIndex] + parityIndex;
                blocks[blockIndex][blockOffset] = codewords[sourceIndex];
                positionBlocks[sourceIndex] = blockIndex;
                positionOffsets[sourceIndex] = blockOffset;
                sourceIndex++;
            }
        }

        int[][] blockErasures = CreateBlockErasures(
            erasurePositions,
            positionBlocks,
            positionOffsets,
            blockCount);
        QrCorrectionResult[] blockResults = CorrectBlocks(blocks, blockErasures, eccCount);
        return CreateAggregateResult(
            codewords,
            blockResults,
            dataLengths,
            eccCount,
            true,
            0);
    }

    static int[][] CreateBlockErasures(
        int[] erasurePositions,
        int[] positionBlocks,
        int[] positionOffsets,
        int blockCount)
    {
        int[] counts = new int[blockCount];
        for (int index = 0; index < erasurePositions.Length; index++)
        {
            counts[positionBlocks[erasurePositions[index]]]++;
        }

        int[][] blockErasures = new int[blockCount][];
        for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            blockErasures[blockIndex] = new int[counts[blockIndex]];
            counts[blockIndex] = 0;
        }

        for (int index = 0; index < erasurePositions.Length; index++)
        {
            int sourcePosition = erasurePositions[index];
            int blockIndex = positionBlocks[sourcePosition];
            blockErasures[blockIndex][counts[blockIndex]] = positionOffsets[sourcePosition];
            counts[blockIndex]++;
        }

        return blockErasures;
    }

    static QrCorrectionResult[] CorrectBlocks(
        byte[][] blocks,
        int[][] blockErasures,
        int eccCount)
    {
        QrCorrectionResult[] results = new QrCorrectionResult[blocks.Length];
        for (int blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
        {
            results[blockIndex] = ReedSolomonDecoder.CorrectCodewordsWithErasures(
                blocks[blockIndex],
                eccCount,
                blockErasures[blockIndex]);
        }

        return results;
    }

    static BlockCorrectionResult CreateAggregateResult(
        byte[] originalCodewords,
        QrCorrectionResult[] blockResults,
        int[] dataLengths,
        int eccCount,
        bool interleaved,
        int remainderCount)
    {
        bool success = true;
        int correctedErrorCount = 0;
        int correctedErasureCount = 0;
        int dataCount = 0;
        for (int blockIndex = 0; blockIndex < blockResults.Length; blockIndex++)
        {
            if (!blockResults[blockIndex].Success || !blockResults[blockIndex].Verified)
            {
                success = false;
            }

            correctedErrorCount += blockResults[blockIndex].CorrectedErrorCount;
            correctedErasureCount += blockResults[blockIndex].CorrectedErasureCount;
            dataCount += dataLengths[blockIndex];
        }

        byte[] dataCodewords = new byte[0];
        byte[] repairedCodewords = CopyCodewords(originalCodewords);
        if (success)
        {
            dataCodewords = CreateDataCodewords(blockResults, dataLengths, dataCount);
            if (interleaved)
            {
                repairedCodewords = CreateInterleavedCodewords(
                    blockResults,
                    dataLengths,
                    eccCount,
                    originalCodewords.Length);
            }
            else
            {
                repairedCodewords = CreateSequentialCodewords(
                    blockResults,
                    dataLengths,
                    eccCount,
                    remainderCount,
                    originalCodewords);
            }
        }

        return new BlockCorrectionResult
        {
            Success = success,
            FailureReason = success
                ? QrFailureReason.None
                : QrFailureReason.ChecksumFailure,
            DataCodewords = dataCodewords,
            Codewords = repairedCodewords,
            BlockResults = blockResults,
            CorrectedErrorCount = correctedErrorCount,
            CorrectedErasureCount = correctedErasureCount
        };
    }

    static byte[] CreateDataCodewords(
        QrCorrectionResult[] blockResults,
        int[] dataLengths,
        int dataCount)
    {
        byte[] dataCodewords = new byte[dataCount];
        int resultIndex = 0;
        for (int blockIndex = 0; blockIndex < blockResults.Length; blockIndex++)
        {
            for (int dataIndex = 0; dataIndex < dataLengths[blockIndex]; dataIndex++)
            {
                dataCodewords[resultIndex] = blockResults[blockIndex].Codewords[dataIndex];
                resultIndex++;
            }
        }

        return dataCodewords;
    }

    static byte[] CreateInterleavedCodewords(
        QrCorrectionResult[] blockResults,
        int[] dataLengths,
        int eccCount,
        int totalCodewordCount)
    {
        byte[] codewords = new byte[totalCodewordCount];
        int resultIndex = 0;
        int maximumDataCount = 0;
        for (int blockIndex = 0; blockIndex < dataLengths.Length; blockIndex++)
        {
            if (dataLengths[blockIndex] > maximumDataCount)
            {
                maximumDataCount = dataLengths[blockIndex];
            }
        }

        for (int dataIndex = 0; dataIndex < maximumDataCount; dataIndex++)
        {
            for (int blockIndex = 0; blockIndex < blockResults.Length; blockIndex++)
            {
                if (dataIndex < dataLengths[blockIndex])
                {
                    codewords[resultIndex] = blockResults[blockIndex].Codewords[dataIndex];
                    resultIndex++;
                }
            }
        }

        for (int parityIndex = 0; parityIndex < eccCount; parityIndex++)
        {
            for (int blockIndex = 0; blockIndex < blockResults.Length; blockIndex++)
            {
                codewords[resultIndex] = blockResults[blockIndex].Codewords[
                    dataLengths[blockIndex] + parityIndex];
                resultIndex++;
            }
        }

        return codewords;
    }

    static byte[] CreateSequentialCodewords(
        QrCorrectionResult[] blockResults,
        int[] dataLengths,
        int eccCount,
        int remainderCount,
        byte[] originalCodewords)
    {
        byte[] codewords = new byte[originalCodewords.Length];
        int resultIndex = 0;
        for (int blockIndex = 0; blockIndex < blockResults.Length; blockIndex++)
        {
            for (int dataIndex = 0; dataIndex < dataLengths[blockIndex]; dataIndex++)
            {
                codewords[resultIndex] = blockResults[blockIndex].Codewords[dataIndex];
                resultIndex++;
            }
        }

        for (int blockIndex = 0; blockIndex < blockResults.Length; blockIndex++)
        {
            for (int parityIndex = 0; parityIndex < eccCount; parityIndex++)
            {
                codewords[resultIndex] = blockResults[blockIndex].Codewords[
                    dataLengths[blockIndex] + parityIndex];
                resultIndex++;
            }
        }

        int remainderStart = originalCodewords.Length - remainderCount;
        while (resultIndex < codewords.Length)
        {
            codewords[resultIndex] = originalCodewords[remainderStart];
            resultIndex++;
            remainderStart++;
        }

        return codewords;
    }

    static byte[] CopyCodewords(byte[] codewords)
    {
        byte[] copy = new byte[codewords.Length];
        for (int index = 0; index < codewords.Length; index++)
        {
            copy[index] = codewords[index];
        }

        return copy;
    }

    static void ValidateInputs(
        byte[] codewords,
        int[] erasurePositions,
        int expectedCodewordCount,
        int protectedCodewordCount)
    {
        if (codewords == null)
        {
            throw new QrInputException("The codewords cannot be null.", "codewords");
        }

        if (erasurePositions == null)
        {
            throw new QrInputException(
                "The erasure positions cannot be null.",
                "erasurePositions");
        }

        if (codewords.Length != expectedCodewordCount)
        {
            throw new QrInputException(
                "The codeword count does not match the selected symbol configuration.",
                "codewords");
        }

        bool[] seen = new bool[codewords.Length];
        for (int index = 0; index < erasurePositions.Length; index++)
        {
            int position = erasurePositions[index];
            if (position < 0 || position >= protectedCodewordCount)
            {
                throw new QrInputException(
                    "An erasure position is outside the protected codeword region.",
                    "erasurePositions");
            }

            if (seen[position])
            {
                throw new QrInputException(
                    "Erasure positions must be unique.",
                    "erasurePositions");
            }

            seen[position] = true;
        }
    }
}
