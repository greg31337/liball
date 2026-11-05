using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class BlockDeinterleaverTests
{
    [Test]
    public void Model2RoundTripsEveryVersionAndErrorCorrectionLevel()
    {
        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 40; version++)
            {
                byte[] data = CreateData(
                    EccBlockTable.GetDataCodewordCount(version, level),
                    version * 17 + level);
                byte[] codewords = BlockInterleaver.CreateModel2Codewords(
                    data,
                    version,
                    level);
                BlockCorrectionResult result = BlockDeinterleaver.CorrectModel2Codewords(
                    codewords,
                    version,
                    level,
                    new int[0]);

                AssertCleanResult(
                    result,
                    data,
                    codewords,
                    EccBlockTable.GetBlockCount(version, level));
            }
        }
    }

    [Test]
    public void RectangularMicroRoundTripsEverySizeAndErrorCorrectionLevel()
    {
        for (int level = 0; level < 2; level++)
        {
            for (int version = 0; version < 32; version++)
            {
                byte[] data = CreateData(
                    RectangularMicroVersionTable.GetDataCodewordCount(version, level),
                    version * 19 + level);
                byte[] codewords = BlockInterleaver.CreateRectangularMicroCodewords(
                    data,
                    version,
                    level);
                BlockCorrectionResult result =
                    BlockDeinterleaver.CorrectRectangularMicroCodewords(
                        codewords,
                        version,
                        level,
                        new int[0]);

                AssertCleanResult(
                    result,
                    data,
                    codewords,
                    RectangularMicroVersionTable.GetBlockCount(version, level));
            }
        }
    }

    [Test]
    public void MicroRoundTripsEverySupportedConfiguration()
    {
        AssertMicroRoundTrip(1, MicroVersionTable.DetectionOnly);
        for (int version = 2; version <= 3; version++)
        {
            AssertMicroRoundTrip(version, EccBlockTable.Low);
            AssertMicroRoundTrip(version, EccBlockTable.Medium);
        }

        AssertMicroRoundTrip(4, EccBlockTable.Low);
        AssertMicroRoundTrip(4, EccBlockTable.Medium);
        AssertMicroRoundTrip(4, EccBlockTable.Quartile);
    }

    [Test]
    public void LegacyModel1RoundTripsEveryVersionAndErrorCorrectionLevel()
    {
        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 14; version++)
            {
                byte[] data = CreateData(
                    LegacyModel1Table.GetDataCodewordCount(version, level),
                    version * 23 + level);
                byte[] codewords = BlockInterleaver.CreateLegacyModel1Codewords(
                    data,
                    version,
                    level);
                BlockCorrectionResult result =
                    BlockDeinterleaver.CorrectLegacyModel1Codewords(
                        codewords,
                        version,
                        level,
                        new int[0]);

                AssertCleanResult(
                    result,
                    data,
                    codewords,
                    LegacyModel1Table.GetErrorCorrectionBlockCount(version, level));
            }
        }
    }

    [Test]
    public void MixedInterleavedDamageProducesPerBlockAndAggregateDiagnostics()
    {
        int version = 5;
        int level = EccBlockTable.Quartile;
        int blockCount = EccBlockTable.GetBlockCount(version, level);
        int shortBlockCount = EccBlockTable.GetShortBlockCount(version, level);
        int shortDataCount = EccBlockTable.GetShortDataCodewordCount(version, level);
        int eccCount = EccBlockTable.GetEccCodewordCountPerBlock(version, level);
        byte[] data = CreateData(EccBlockTable.GetDataCodewordCount(version, level), 91);
        byte[] original = BlockInterleaver.CreateModel2Codewords(data, version, level);
        byte[] damaged = (byte[])original.Clone();
        int[][] positions = CreateInterleavedPositionMap(
            blockCount,
            shortBlockCount,
            shortDataCount,
            eccCount);

        int[] erasures = new int[]
        {
            positions[2][33], positions[0][0], positions[2][0],
            positions[0][32], positions[2][16]
        };
        Damage(damaged, positions[0][0], 29);
        Damage(damaged, positions[0][32], 71);
        Damage(damaged, positions[0][5], 113);
        Damage(damaged, positions[0][20], 167);
        Damage(damaged, positions[1][1], 43);
        Damage(damaged, positions[2][33], 53);
        Damage(damaged, positions[2][0], 97);
        Damage(damaged, positions[2][16], 139);
        Damage(damaged, positions[2][7], 181);
        Damage(damaged, positions[2][25], 223);

        byte[] supplied = (byte[])damaged.Clone();
        int[] suppliedErasures = (int[])erasures.Clone();
        BlockCorrectionResult result = BlockDeinterleaver.CorrectCodewords(
            supplied,
            QrSymbolKind.Model2,
            version,
            level,
            suppliedErasures);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(result.DataCodewords, Is.EqualTo(data));
            Assert.That(result.Codewords, Is.EqualTo(original));
            Assert.That(result.BlockResults, Has.Length.EqualTo(4));
            Assert.That(result.BlockResults[0].CorrectedErrorCount, Is.EqualTo(2));
            Assert.That(result.BlockResults[0].CorrectedErasureCount, Is.EqualTo(2));
            Assert.That(result.BlockResults[1].CorrectedErrorCount, Is.EqualTo(1));
            Assert.That(result.BlockResults[1].CorrectedErasureCount, Is.Zero);
            Assert.That(result.BlockResults[2].CorrectedErrorCount, Is.EqualTo(2));
            Assert.That(result.BlockResults[2].CorrectedErasureCount, Is.EqualTo(3));
            Assert.That(result.BlockResults[3].CorrectedErrorCount, Is.Zero);
            Assert.That(result.BlockResults[3].CorrectedErasureCount, Is.Zero);
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(5));
            Assert.That(result.CorrectedErasureCount, Is.EqualTo(5));
            Assert.That(supplied, Is.EqualTo(damaged));
            Assert.That(suppliedErasures, Is.EqualTo(erasures));
        });
    }

    [Test]
    public void LegacyDamageMapsToSequentialDataAndParityBlocks()
    {
        int version = 14;
        int level = EccBlockTable.High;
        int blockCount = LegacyModel1Table.GetErrorCorrectionBlockCount(version, level);
        int dataPerBlock = LegacyModel1Table.GetDataCodewordCountPerBlock(version, level);
        int eccPerBlock = LegacyModel1Table.GetErrorCorrectionCodewordCountPerBlock(
            version,
            level);
        int dataCount = LegacyModel1Table.GetDataCodewordCount(version, level);
        byte[] data = CreateData(dataCount, 143);
        byte[] original = BlockInterleaver.CreateLegacyModel1Codewords(
            data,
            version,
            level);
        byte[] damaged = (byte[])original.Clone();
        int unknownPosition = 2 * dataPerBlock + 7;
        int erasurePosition = dataCount + 5 * eccPerBlock + 41;
        Damage(damaged, unknownPosition, 109);
        Damage(damaged, erasurePosition, 211);

        BlockCorrectionResult result = BlockDeinterleaver.CorrectLegacyModel1Codewords(
            damaged,
            version,
            level,
            new int[] { erasurePosition });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DataCodewords, Is.EqualTo(data));
            Assert.That(result.Codewords, Is.EqualTo(original));
            Assert.That(result.BlockResults, Has.Length.EqualTo(blockCount));
            Assert.That(result.BlockResults[2].CorrectedErrorCount, Is.EqualTo(1));
            Assert.That(result.BlockResults[5].CorrectedErasureCount, Is.EqualTo(1));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(1));
            Assert.That(result.CorrectedErasureCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void FailedBlockWithholdsAggregateDataAndPreservesReceivedCodewords()
    {
        int version = 5;
        int level = EccBlockTable.Quartile;
        int blockCount = EccBlockTable.GetBlockCount(version, level);
        int shortBlockCount = EccBlockTable.GetShortBlockCount(version, level);
        int shortDataCount = EccBlockTable.GetShortDataCodewordCount(version, level);
        int eccCount = EccBlockTable.GetEccCodewordCountPerBlock(version, level);
        byte[] data = CreateData(EccBlockTable.GetDataCodewordCount(version, level), 177);
        byte[] codewords = BlockInterleaver.CreateModel2Codewords(data, version, level);
        byte[] damaged = (byte[])codewords.Clone();
        int[][] positions = CreateInterleavedPositionMap(
            blockCount,
            shortBlockCount,
            shortDataCount,
            eccCount);
        for (int index = 0; index < 10; index++)
        {
            Damage(damaged, positions[1][index], index * 23 + 7);
        }

        BlockCorrectionResult result = BlockDeinterleaver.CorrectModel2Codewords(
            damaged,
            version,
            level,
            new int[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
            Assert.That(result.DataCodewords, Is.Empty);
            Assert.That(result.Codewords, Is.EqualTo(damaged));
            Assert.That(result.Codewords, Is.Not.SameAs(damaged));
            Assert.That(result.BlockResults[0].Success, Is.True);
            Assert.That(result.BlockResults[1].Success, Is.False);
            Assert.That(result.BlockResults[2].Success, Is.True);
            Assert.That(result.BlockResults[3].Success, Is.True);
        });
    }

    [Test]
    public void RejectsInvalidAggregateInputsAndLegacyRemainderErasures()
    {
        byte[] model2 = new byte[26];
        byte[] legacy = new byte[610];
        int legacyProtectedCount = LegacyModel1Table.GetDataCodewordCount(14, 3)
            + LegacyModel1Table.GetErrorCorrectionCodewordCount(14, 3);
        Assert.Multiple(() =>
        {
            Assert.That(() => BlockDeinterleaver.CorrectCodewords(
                    model2,
                    (QrSymbolKind)99,
                    1,
                    0,
                    new int[0]),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockDeinterleaver.CorrectModel2Codewords(
                    null!,
                    1,
                    0,
                    new int[0]),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockDeinterleaver.CorrectModel2Codewords(
                    model2,
                    1,
                    0,
                    null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockDeinterleaver.CorrectModel2Codewords(
                    new byte[25],
                    1,
                    0,
                    new int[0]),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockDeinterleaver.CorrectModel2Codewords(
                    model2,
                    1,
                    0,
                    new int[] { 3, 3 }),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockDeinterleaver.CorrectModel2Codewords(
                    model2,
                    1,
                    0,
                    new int[] { 26 }),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BlockDeinterleaver.CorrectLegacyModel1Codewords(
                    legacy,
                    14,
                    3,
                    new int[] { legacyProtectedCount }),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static void AssertMicroRoundTrip(int version, int level)
    {
        byte[] data = CreateData(
            MicroVersionTable.GetDataSymbolCharacterCount(version, level),
            version * 29 + level);
        byte[] codewords = BlockInterleaver.CreateMicroCodewords(data, version, level);
        BlockCorrectionResult result = BlockDeinterleaver.CorrectMicroCodewords(
            codewords,
            version,
            level,
            new int[0]);
        AssertCleanResult(result, data, codewords, 1);
    }

    private static void AssertCleanResult(
        BlockCorrectionResult result,
        byte[] data,
        byte[] codewords,
        int blockCount)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(result.DataCodewords, Is.EqualTo(data));
        Assert.That(result.Codewords, Is.EqualTo(codewords));
        Assert.That(result.Codewords, Is.Not.SameAs(codewords));
        Assert.That(result.BlockResults, Has.Length.EqualTo(blockCount));
        Assert.That(result.BlockResults, Has.All.Matches<QrCorrectionResult>(
            block => block.Success && block.Verified));
        Assert.That(result.CorrectedErrorCount, Is.Zero);
        Assert.That(result.CorrectedErasureCount, Is.Zero);
    }

    private static int[][] CreateInterleavedPositionMap(
        int blockCount,
        int shortBlockCount,
        int shortDataCount,
        int eccCount)
    {
        int[][] positions = new int[blockCount][];
        int[] dataLengths = new int[blockCount];
        for (int block = 0; block < blockCount; block++)
        {
            dataLengths[block] = shortDataCount + (block >= shortBlockCount ? 1 : 0);
            positions[block] = new int[dataLengths[block] + eccCount];
        }

        int interleavedPosition = 0;
        int maximumDataCount = shortDataCount + (shortBlockCount < blockCount ? 1 : 0);
        for (int dataIndex = 0; dataIndex < maximumDataCount; dataIndex++)
        {
            for (int block = 0; block < blockCount; block++)
            {
                if (dataIndex < dataLengths[block])
                {
                    positions[block][dataIndex] = interleavedPosition;
                    interleavedPosition++;
                }
            }
        }

        for (int parityIndex = 0; parityIndex < eccCount; parityIndex++)
        {
            for (int block = 0; block < blockCount; block++)
            {
                positions[block][dataLengths[block] + parityIndex] = interleavedPosition;
                interleavedPosition++;
            }
        }

        return positions;
    }

    private static void Damage(byte[] codewords, int position, int magnitude)
    {
        codewords[position] ^= (byte)magnitude;
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
