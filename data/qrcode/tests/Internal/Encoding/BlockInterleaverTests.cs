using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class BlockInterleaverTests
{
    [Test]
    public void SingleBlockProfilesMatchCheckedInGoldenCodewords()
    {
        byte[] model2Data = new byte[]
        {
            16, 32, 12, 86, 97, 128, 236, 17, 236, 17,
            236, 17, 236, 17, 236, 17, 236, 17, 236
        };
        byte[] model2Expected = new byte[]
        {
            16, 32, 12, 86, 97, 128, 236, 17, 236, 17,
            236, 17, 236, 17, 236, 17, 236, 17, 236,
            83, 85, 151, 103, 16, 5, 132
        };
        byte[] microData = new byte[] { 163, 218, 208 };
        byte[] microExpected = new byte[] { 163, 218, 208, 110, 199 };
        byte[] rectangularData = new byte[] { 42, 61, 173, 0, 236, 17 };
        byte[] rectangularExpected = new byte[]
        {
            42, 61, 173, 0, 236, 17, 64, 56, 41, 63, 145, 161, 25
        };
        byte[] legacyData = new byte[]
        {
            4, 16, 81, 82, 32, 67, 111, 100, 101, 32,
            77, 111, 100, 101, 108, 32, 49, 32, 0, 236,
            17, 236, 17, 236, 17, 236, 17, 236, 17, 236
        };
        byte[] legacyExpected = new byte[]
        {
            4, 16, 81, 82, 32, 67, 111, 100, 101, 32,
            77, 111, 100, 101, 108, 32, 49, 32, 0, 236,
            17, 236, 17, 236, 17, 236, 17, 236, 17, 236,
            2, 66, 219, 19, 60, 192, 21, 80,
            244, 228, 19, 87, 213, 211, 143, 185
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                BlockInterleaver.CreateCodewords(
                    model2Data,
                    QrSymbolKind.Model2,
                    1,
                    EccBlockTable.Low),
                Is.EqualTo(model2Expected));
            Assert.That(
                BlockInterleaver.CreateCodewords(
                    microData,
                    QrSymbolKind.Micro,
                    1,
                    MicroVersionTable.DetectionOnly),
                Is.EqualTo(microExpected));
            Assert.That(
                BlockInterleaver.CreateCodewords(
                    rectangularData,
                    QrSymbolKind.rMQR,
                    0,
                    RectangularMicroVersionTable.Medium),
                Is.EqualTo(rectangularExpected));
            Assert.That(
                BlockInterleaver.CreateCodewords(
                    legacyData,
                    QrSymbolKind.LegacyModel1,
                    2,
                    EccBlockTable.Medium),
                Is.EqualTo(legacyExpected));
        });
    }

    [Test]
    public void Model2InterleavingCoversEveryVersionAndErrorCorrectionLevel()
    {
        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 40; version++)
            {
                int dataCount = EccBlockTable.GetDataCodewordCount(version, level);
                byte[] data = CreateData(dataCount, version * 17 + level);
                byte[] original = (byte[])data.Clone();
                byte[] result = BlockInterleaver.CreateModel2Codewords(data, version, level);

                Assert.That(
                    result.Length,
                    Is.EqualTo(Model2VersionTable.GetTotalCodewordCount(version)));
                AssertInterleavedLayout(
                    data,
                    result,
                    EccBlockTable.GetBlockCount(version, level),
                    EccBlockTable.GetShortBlockCount(version, level),
                    EccBlockTable.GetShortDataCodewordCount(version, level),
                    EccBlockTable.GetEccCodewordCountPerBlock(version, level));
                Assert.That(data, Is.EqualTo(original));
            }
        }
    }

    [Test]
    public void ShortAndLongModel2BlocksUseTheRequiredColumnOrder()
    {
        byte[] data = new byte[62];
        for (int index = 0; index < data.Length; index++)
        {
            data[index] = (byte)index;
        }

        byte[] result = BlockInterleaver.CreateModel2Codewords(
            data,
            5,
            EccBlockTable.Quartile);

        Assert.Multiple(() =>
        {
            Assert.That(
                result[0..8],
                Is.EqualTo(new byte[] { 0, 15, 30, 46, 1, 16, 31, 47 }));
            Assert.That(result[60], Is.EqualTo(45));
            Assert.That(result[61], Is.EqualTo(61));
        });
    }

    [Test]
    public void RectangularMicroInterleavingCoversEverySizeAndErrorCorrectionLevel()
    {
        for (int level = 0; level < 2; level++)
        {
            for (int version = 0; version < 32; version++)
            {
                int dataCount = RectangularMicroVersionTable.GetDataCodewordCount(
                    version,
                    level);
                byte[] data = CreateData(dataCount, version * 19 + level);
                byte[] result = BlockInterleaver.CreateRectangularMicroCodewords(
                    data,
                    version,
                    level);

                Assert.That(
                    result.Length,
                    Is.EqualTo(RectangularMicroVersionTable.GetTotalCodewordCount(version)));
                AssertInterleavedLayout(
                    data,
                    result,
                    RectangularMicroVersionTable.GetBlockCount(version, level),
                    RectangularMicroVersionTable.GetShortBlockCount(version, level),
                    RectangularMicroVersionTable.GetShortDataCodewordCount(version, level),
                    RectangularMicroVersionTable.GetEccCodewordCountPerBlock(version, level));
            }
        }
    }

    [Test]
    public void MicroInterleavingCoversEverySupportedConfiguration()
    {
        AssertMicroConfiguration(1, MicroVersionTable.DetectionOnly);
        for (int version = 2; version <= 3; version++)
        {
            AssertMicroConfiguration(version, EccBlockTable.Low);
            AssertMicroConfiguration(version, EccBlockTable.Medium);
        }

        AssertMicroConfiguration(4, EccBlockTable.Low);
        AssertMicroConfiguration(4, EccBlockTable.Medium);
        AssertMicroConfiguration(4, EccBlockTable.Quartile);
    }

    [Test]
    public void LegacyModel1KeepsBlocksSequentialAndAddsZeroRemainderCodewords()
    {
        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 14; version++)
            {
                int dataCount = LegacyModel1Table.GetDataCodewordCount(version, level);
                byte[] data = CreateData(dataCount, version * 23 + level);
                byte[] result = BlockInterleaver.CreateLegacyModel1Codewords(
                    data,
                    version,
                    level);

                Assert.That(
                    result.Length,
                    Is.EqualTo(LegacyModel1Table.GetTotalCodewordCount(version)));
                AssertLegacyLayout(data, result, version, level);
            }
        }
    }

    [Test]
    public void RejectsUnknownProfilesNullDataAndIncorrectDataCounts()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => BlockInterleaver.CreateCodewords(
                    new byte[0],
                    (QrSymbolKind)99,
                    1,
                    0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockInterleaver.CreateModel2Codewords(null!, 1, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockInterleaver.CreateModel2Codewords(new byte[18], 1, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockInterleaver.CreateMicroCodewords(new byte[4], 1, -1),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockInterleaver.CreateRectangularMicroCodewords(
                    new byte[5],
                    0,
                    0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockInterleaver.CreateLegacyModel1Codewords(
                    new byte[29],
                    2,
                    1),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static void AssertMicroConfiguration(int version, int level)
    {
        int dataCount = MicroVersionTable.GetDataSymbolCharacterCount(version, level);
        int eccCount = MicroVersionTable.GetEccCodewordCount(version, level);
        byte[] data = CreateData(dataCount, version * 29 + level);
        byte[] result = BlockInterleaver.CreateMicroCodewords(data, version, level);
        byte[] expectedParity = ReedSolomonEncoder.CreateParity(data, eccCount);

        Assert.That(result.Length, Is.EqualTo(dataCount + eccCount));
        Assert.That(result[0..dataCount], Is.EqualTo(data));
        Assert.That(result[dataCount..], Is.EqualTo(expectedParity));
    }

    private static void AssertInterleavedLayout(
        byte[] data,
        byte[] result,
        int blockCount,
        int shortBlockCount,
        int shortDataCount,
        int eccCount)
    {
        int[] starts = new int[blockCount];
        byte[][] parityBlocks = new byte[blockCount][];
        int dataOffset = 0;
        for (int block = 0; block < blockCount; block++)
        {
            starts[block] = dataOffset;
            int blockLength = shortDataCount + (block >= shortBlockCount ? 1 : 0);
            byte[] blockData = new byte[blockLength];
            Array.Copy(data, dataOffset, blockData, 0, blockLength);
            parityBlocks[block] = ReedSolomonEncoder.CreateParity(blockData, eccCount);
            dataOffset += blockLength;
        }

        int resultIndex = 0;
        int maximumDataCount = shortDataCount + (shortBlockCount < blockCount ? 1 : 0);
        for (int dataIndex = 0; dataIndex < maximumDataCount; dataIndex++)
        {
            for (int block = 0; block < blockCount; block++)
            {
                int blockLength = shortDataCount + (block >= shortBlockCount ? 1 : 0);
                if (dataIndex < blockLength)
                {
                    Assert.That(
                        result[resultIndex],
                        Is.EqualTo(data[starts[block] + dataIndex]),
                        "data index " + resultIndex);
                    resultIndex++;
                }
            }
        }

        for (int parityIndex = 0; parityIndex < eccCount; parityIndex++)
        {
            for (int block = 0; block < blockCount; block++)
            {
                Assert.That(
                    result[resultIndex],
                    Is.EqualTo(parityBlocks[block][parityIndex]),
                    "parity index " + resultIndex);
                resultIndex++;
            }
        }

        Assert.That(dataOffset, Is.EqualTo(data.Length));
        Assert.That(resultIndex, Is.EqualTo(result.Length));
    }

    private static void AssertLegacyLayout(
        byte[] data,
        byte[] result,
        int version,
        int level)
    {
        Assert.That(result[0..data.Length], Is.EqualTo(data));
        int blockCount = LegacyModel1Table.GetErrorCorrectionBlockCount(version, level);
        int dataPerBlock = LegacyModel1Table.GetDataCodewordCountPerBlock(version, level);
        int eccPerBlock = LegacyModel1Table.GetErrorCorrectionCodewordCountPerBlock(
            version,
            level);
        int resultIndex = data.Length;
        for (int block = 0; block < blockCount; block++)
        {
            byte[] blockData = new byte[dataPerBlock];
            Array.Copy(data, block * dataPerBlock, blockData, 0, dataPerBlock);
            byte[] parity = ReedSolomonEncoder.CreateParity(blockData, eccPerBlock);
            Assert.That(result[resultIndex..(resultIndex + eccPerBlock)], Is.EqualTo(parity));
            resultIndex += eccPerBlock;
        }

        while (resultIndex < result.Length)
        {
            Assert.That(result[resultIndex], Is.Zero, "remainder index " + resultIndex);
            resultIndex++;
        }
    }

    private static byte[] CreateData(int length, int seed)
    {
        byte[] data = new byte[length];
        for (int index = 0; index < data.Length; index++)
        {
            data[index] = (byte)((index * 149 + seed * 43 + 37) & 255);
        }

        return data;
    }
}
