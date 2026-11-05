namespace barcode.Tests;

public class DataMatrixBlockInterleaverTests
{
    [Test]
    public void EveryStandardSizeSplitsEncodesInterleavesAndDeinterleaves()
    {
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            int[] data = CreateCodewords(symbol.DataCodewordCapacity, version);
            DataMatrixInterleavedBlock[] encodedBlocks = DataMatrixBlockInterleaver.CreateEncodedBlocks(
                data,
                symbol);
            int[] symbolCodewords = DataMatrixBlockInterleaver.InterleaveBlocks(encodedBlocks, symbol);
            DataMatrixInterleavedBlock[] decodedBlocks = DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
                symbolCodewords,
                symbol);
            int[] extracted = DataMatrixBlockInterleaver.ExtractDataCodewords(decodedBlocks, symbol);

            Assert.Multiple(() =>
            {
                Assert.That(encodedBlocks, Has.Length.EqualTo(symbol.InterleavedBlockCount), $"Version {version}");
                Assert.That(symbolCodewords, Has.Length.EqualTo(symbol.TotalCodewordCount), $"Version {version}");
                Assert.That(CopyPrefix(symbolCodewords, symbol.DataCodewordCapacity), Is.EqualTo(data), $"Version {version}");
                Assert.That(extracted, Is.EqualTo(data), $"Version {version}");
                Assert.That(
                    DataMatrixBlockInterleaver.EncodeSymbolCodewords(data, symbol),
                    Is.EqualTo(symbolCodewords),
                    $"Version {version}");
                Assert.That(
                    DataMatrixBlockInterleaver.InterleaveBlocks(decodedBlocks, symbol),
                    Is.EqualTo(symbolCodewords),
                    $"Version {version}");
            });

            int blockIndex = 0;
            while (blockIndex < encodedBlocks.Length)
            {
                DataMatrixInterleavedBlock encoded = encodedBlocks[blockIndex];
                DataMatrixInterleavedBlock decoded = decodedBlocks[blockIndex];
                Assert.Multiple(() =>
                {
                    Assert.That(encoded.Index, Is.EqualTo(blockIndex), $"Version {version}, block {blockIndex}");
                    Assert.That(encoded.DataCodewordCount, Is.EqualTo(symbol.GetDataCodewordCountForBlock(blockIndex)));
                    Assert.That(encoded.ErrorCodewordCount, Is.EqualTo(symbol.GetErrorCodewordCountForBlock(blockIndex)));
                    Assert.That(encoded.Codewords, Has.Length.EqualTo(encoded.DataCodewordCount + encoded.ErrorCodewordCount));
                    Assert.That(DataMatrixReedSolomon.VerifyCodewords(encoded.Codewords, encoded.ErrorCodewordCount), Is.True);
                    Assert.That(decoded.Codewords, Is.EqualTo(encoded.Codewords));
                });
                blockIndex++;
            }

            version++;
        }
    }

    [Test]
    public void EveryStandardSizeCorrectsMaximumSpreadErrorsInEveryInterleavedBlock()
    {
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            int[] data = CreateCodewords(symbol.DataCodewordCapacity, version + 100);
            int[] encoded = DataMatrixBlockInterleaver.EncodeSymbolCodewords(data, symbol);
            ModuleMatrix mappingMatrix = DataMatrixPlacement.PlaceCodewords(encoded, symbol);
            int[] extracted = DataMatrixPlacement.ExtractCodewords(mappingMatrix, symbol);
            DataMatrixInterleavedBlock[] blocks = DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
                extracted,
                symbol);

            int blockIndex = 0;
            while (blockIndex < blocks.Length)
            {
                DataMatrixInterleavedBlock block = blocks[blockIndex];
                int correctionLimit = block.ErrorCodewordCount / 2;
                int[] errorIndices = CreateSpreadIndices(block.Codewords.Length, correctionLimit);
                int[] damaged = InjectErrors(block.Codewords, errorIndices, version + blockIndex);

                DataMatrixCorrectionResult result = DataMatrixReedSolomon.CorrectCodewords(
                    damaged,
                    block.ErrorCodewordCount,
                    null);

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, $"Version {version}, block {blockIndex}: {result.FailureMessage}");
                    Assert.That(result.CorrectedCodewords, Is.EqualTo(block.Codewords), $"Version {version}, block {blockIndex}");
                    Assert.That(result.CorrectedErrorCount, Is.EqualTo(correctionLimit), $"Version {version}, block {blockIndex}");
                    Assert.That(result.CorrectedIndices, Is.EquivalentTo(errorIndices), $"Version {version}, block {blockIndex}");
                });
                block.Codewords = result.CorrectedCodewords;
                blockIndex++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(
                    DataMatrixBlockInterleaver.ExtractDataCodewords(blocks, symbol),
                    Is.EqualTo(data),
                    $"Version {version} data");
                Assert.That(
                    DataMatrixBlockInterleaver.InterleaveBlocks(blocks, symbol),
                    Is.EqualTo(encoded),
                    $"Version {version} stream");
            });
            version++;
        }
    }

    [Test]
    public void SingleBlockEncodingMatchesTheEcc200AnnexVector()
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(1);
        int[] data = new int[] { 142, 164, 186 };

        int[] encoded = DataMatrixBlockInterleaver.EncodeSymbolCodewords(data, symbol);
        DataMatrixInterleavedBlock[] blocks = DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
            encoded,
            symbol);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new int[] { 142, 164, 186, 114, 25, 5, 88, 102 }));
            Assert.That(blocks, Has.Length.EqualTo(1));
            Assert.That(blocks[0].CopyDataCodewords(), Is.EqualTo(data));
            Assert.That(blocks[0].CopyErrorCodewords(), Is.EqualTo(new int[] { 114, 25, 5, 88, 102 }));
        });
    }

    [Test]
    public void UniformMultipleBlocksUseRoundRobinDataAndParity()
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(15);
        int[] data = CreateSequentialCodewords(symbol.DataCodewordCapacity);
        DataMatrixInterleavedBlock[] blocks = DataMatrixBlockInterleaver.CreateEncodedBlocks(data, symbol);
        int[] interleaved = DataMatrixBlockInterleaver.InterleaveBlocks(blocks, symbol);
        int[] evenData = CreateStride(data, 0, 2);
        int[] oddData = CreateStride(data, 1, 2);
        int[] firstParity = blocks[0].CopyErrorCodewords();
        int[] secondParity = blocks[1].CopyErrorCodewords();

        Assert.Multiple(() =>
        {
            Assert.That(blocks, Has.Length.EqualTo(2));
            Assert.That(blocks[0].CopyDataCodewords(), Is.EqualTo(evenData));
            Assert.That(blocks[1].CopyDataCodewords(), Is.EqualTo(oddData));
            Assert.That(CopyPrefix(interleaved, data.Length), Is.EqualTo(data));
            Assert.That(interleaved[data.Length], Is.EqualTo(firstParity[0]));
            Assert.That(interleaved[data.Length + 1], Is.EqualTo(secondParity[0]));
            Assert.That(interleaved[data.Length + 2], Is.EqualTo(firstParity[1]));
            Assert.That(interleaved[data.Length + 3], Is.EqualTo(secondParity[1]));
        });
    }

    [Test]
    public void LargestSquareUsesEightLongBlocksAndRotatedParityOrder()
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(24);
        int[] data = CreateCodewords(symbol.DataCodewordCapacity, 24);
        DataMatrixInterleavedBlock[] blocks = DataMatrixBlockInterleaver.CreateEncodedBlocks(data, symbol);
        int[] interleaved = DataMatrixBlockInterleaver.InterleaveBlocks(blocks, symbol);
        DataMatrixInterleavedBlock[] decoded = DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
            interleaved,
            symbol);
        int parityStart = symbol.DataCodewordCapacity;

        Assert.Multiple(() =>
        {
            Assert.That(blocks, Has.Length.EqualTo(10));
            Assert.That(blocks[0].DataCodewordCount, Is.EqualTo(156));
            Assert.That(blocks[7].DataCodewordCount, Is.EqualTo(156));
            Assert.That(blocks[8].DataCodewordCount, Is.EqualTo(155));
            Assert.That(blocks[9].DataCodewordCount, Is.EqualTo(155));
            Assert.That(blocks[0].Codewords[155], Is.EqualTo(data[1550]));
            Assert.That(blocks[7].Codewords[155], Is.EqualTo(data[1557]));
            Assert.That(interleaved[parityStart], Is.EqualTo(blocks[8].Codewords[155]));
            Assert.That(interleaved[parityStart + 1], Is.EqualTo(blocks[9].Codewords[155]));
            Assert.That(interleaved[parityStart + 2], Is.EqualTo(blocks[0].Codewords[156]));
            Assert.That(interleaved[parityStart + 9], Is.EqualTo(blocks[7].Codewords[156]));
            Assert.That(decoded[0].Codewords, Is.EqualTo(blocks[0].Codewords));
            Assert.That(decoded[8].Codewords, Is.EqualTo(blocks[8].Codewords));
            Assert.That(DataMatrixBlockInterleaver.ExtractDataCodewords(decoded, symbol), Is.EqualTo(data));
        });
    }

    [Test]
    public void DeinterleavingMapsReceivedParityDamageToTheCorrectRotatedBlock()
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(24);
        int[] data = CreateCodewords(symbol.DataCodewordCapacity, 99);
        int[] encoded = DataMatrixBlockInterleaver.EncodeSymbolCodewords(data, symbol);
        int parityStart = symbol.DataCodewordCapacity;
        int original = encoded[parityStart];
        encoded[parityStart] = original ^ 91;

        DataMatrixInterleavedBlock[] blocks = DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
            encoded,
            symbol);

        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixReedSolomon.VerifyCodewords(blocks[8].Codewords, 62), Is.False);
            Assert.That(DataMatrixReedSolomon.VerifyCodewords(blocks[9].Codewords, 62), Is.True);
            Assert.That(DataMatrixReedSolomon.VerifyCodewords(blocks[0].Codewords, 62), Is.True);
            Assert.That(blocks[8].Codewords[155], Is.EqualTo(original ^ 91));
        });
    }

    [Test]
    public void BlockCopiesDoNotAliasAndMalformedInputsAreRejected()
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(15);
        int[] data = CreateCodewords(symbol.DataCodewordCapacity, 15);
        DataMatrixInterleavedBlock[] blocks = DataMatrixBlockInterleaver.CreateEncodedBlocks(data, symbol);
        int[] dataCopy = blocks[0].CopyDataCodewords();
        int[] errorCopy = blocks[0].CopyErrorCodewords();
        int[] blockCopy = blocks[0].CopyCodewords();
        dataCopy[0] = dataCopy[0] ^ 1;
        errorCopy[0] = errorCopy[0] ^ 1;
        blockCopy[0] = blockCopy[0] ^ 1;
        DataMatrixInterleavedBlock[] wrongBlockCount = new DataMatrixInterleavedBlock[] { blocks[0] };
        DataMatrixInterleavedBlock[] missingBlock = new DataMatrixInterleavedBlock[] { blocks[0], null! };
        DataMatrixInterleavedBlock[] badMetadata = DataMatrixBlockInterleaver.CreateEncodedBlocks(data, symbol);
        badMetadata[1].Index = 0;
        DataMatrixInterleavedBlock[] badLength = DataMatrixBlockInterleaver.CreateEncodedBlocks(data, symbol);
        badLength[1].Codewords = new int[1];
        DataMatrixSymbolInfo badSymbol = symbol.CopySymbolInfo();
        badSymbol.DataCodewordsPerBlock[1] = 103;

        Assert.Multiple(() =>
        {
            Assert.That(blocks[0].CopyDataCodewords()[0], Is.Not.EqualTo(dataCopy[0]));
            Assert.That(blocks[0].CopyErrorCodewords()[0], Is.Not.EqualTo(errorCopy[0]));
            Assert.That(blocks[0].Codewords[0], Is.Not.EqualTo(blockCopy[0]));
            Assert.That(
                () => DataMatrixBlockInterleaver.CreateEncodedBlocks(null!, symbol),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.CreateEncodedBlocks(new int[data.Length - 1], symbol),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.CreateEncodedBlocks(CreateInvalidCodewords(data.Length), symbol),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(new int[symbol.TotalCodewordCount - 1], symbol),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.InterleaveBlocks(null!, symbol),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.InterleaveBlocks(wrongBlockCount, symbol),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.InterleaveBlocks(missingBlock, symbol),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.InterleaveBlocks(badMetadata, symbol),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.InterleaveBlocks(badLength, symbol),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.CreateEncodedBlocks(data, null!),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixBlockInterleaver.CreateEncodedBlocks(data, badSymbol),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static int[] CreateCodewords(int count, int seed)
    {
        int[] codewords = new int[count];
        int index = 0;
        while (index < codewords.Length)
        {
            codewords[index] = ((index * 149) + (seed * 31)) & 0xFF;
            index++;
        }

        return codewords;
    }

    static int[] CreateSequentialCodewords(int count)
    {
        int[] codewords = new int[count];
        int index = 0;
        while (index < codewords.Length)
        {
            codewords[index] = index & 0xFF;
            index++;
        }

        return codewords;
    }

    static int[] CreateStride(int[] source, int start, int stride)
    {
        int length = (source.Length - start + stride - 1) / stride;
        int[] result = new int[length];
        int sourceIndex = start;
        int targetIndex = 0;
        while (sourceIndex < source.Length)
        {
            result[targetIndex] = source[sourceIndex];
            sourceIndex += stride;
            targetIndex++;
        }

        return result;
    }

    static int[] CopyPrefix(int[] source, int count)
    {
        int[] copy = new int[count];
        global::System.Array.Copy(source, 0, copy, 0, count);
        return copy;
    }

    static int[] CreateInvalidCodewords(int count)
    {
        int[] codewords = new int[count];
        codewords[0] = 256;
        return codewords;
    }

    static int[] CreateSpreadIndices(int length, int count)
    {
        int[] indices = new int[count];
        int index = 0;
        while (index < count)
        {
            indices[index] = (index * (length - 1)) / (count - 1);
            index++;
        }

        return indices;
    }

    static int[] InjectErrors(int[] source, int[] indices, int seed)
    {
        int[] damaged = new int[source.Length];
        global::System.Array.Copy(source, damaged, source.Length);
        int index = 0;
        while (index < indices.Length)
        {
            int magnitude = ((index * 73) + seed) % 255 + 1;
            damaged[indices[index]] = damaged[indices[index]] ^ magnitude;
            index++;
        }

        return damaged;
    }
}
