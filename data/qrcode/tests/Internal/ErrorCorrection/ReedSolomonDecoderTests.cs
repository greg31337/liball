using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ReedSolomonDecoderTests
{
    [Test]
    public void SyndromeCalculationUsesEveryConsecutiveQrRoot()
    {
        byte[] block = CreateBlock(CreateData(23), 10);
        Assert.That(ReedSolomonDecoder.CalculateSyndromes(block, 10), Is.All.Zero);

        int errorPosition = 7;
        int errorMagnitude = 173;
        block[errorPosition] ^= (byte)errorMagnitude;
        int[] syndromes = ReedSolomonDecoder.CalculateSyndromes(block, 10);
        int location = PowerReference(2, block.Length - 1 - errorPosition);
        int expected = errorMagnitude;
        for (int index = 0; index < syndromes.Length; index++)
        {
            Assert.That(syndromes[index], Is.EqualTo(expected), "syndrome " + index);
            expected = MultiplyReference(expected, location);
        }
    }

    [Test]
    public void LocatorEvaluatorChienSearchAndForneyRecoverKnownErrors()
    {
        byte[] original = CreateBlock(CreateData(37), 10);
        byte[] damaged = (byte[])original.Clone();
        int[] expectedPositions = new int[] { 0, 13, damaged.Length - 1 };
        int[] expectedMagnitudes = new int[] { 1, 83, 255 };
        for (int index = 0; index < expectedPositions.Length; index++)
        {
            damaged[expectedPositions[index]] ^= (byte)expectedMagnitudes[index];
        }

        int[] syndromes = ReedSolomonDecoder.CalculateSyndromes(damaged, 10);
        int[] locator = ErrorLocationSolver.SolveLocator(syndromes);
        int[] evaluator = ErrorLocationSolver.CreateEvaluator(syndromes, locator);
        int[] positions = ErrorLocationSolver.FindErrorPositions(locator, damaged.Length);
        int[] magnitudes = ErrorLocationSolver.CreateMagnitudes(
            locator,
            evaluator,
            positions,
            damaged.Length);

        Assert.Multiple(() =>
        {
            Assert.That(locator.Length - 1, Is.EqualTo(3));
            Assert.That(positions, Is.EqualTo(expectedPositions));
            Assert.That(magnitudes, Is.EqualTo(expectedMagnitudes));
        });
    }

    [Test]
    public void EveryStandardizedParityLengthCorrectsItsFullErrorBudget()
    {
        bool[] standardizedLengths = GetStandardizedLengths();
        for (int parityLength = 1; parityLength < standardizedLengths.Length; parityLength++)
        {
            if (!standardizedLengths[parityLength])
            {
                continue;
            }

            int dataLength = Math.Min(73, 255 - parityLength);
            byte[] original = CreateBlock(CreateData(dataLength), parityLength);
            byte[] damaged = (byte[])original.Clone();
            int errorCount = parityLength / 2;
            for (int errorIndex = 0; errorIndex < errorCount; errorIndex++)
            {
                int position = errorIndex * damaged.Length / errorCount;
                int magnitude = (errorIndex * 73 + 17) % 255 + 1;
                damaged[position] ^= (byte)magnitude;
            }

            byte[] supplied = (byte[])damaged.Clone();
            QrCorrectionResult result = ReedSolomonDecoder.CorrectCodewords(
                supplied,
                parityLength);

            Assert.That(result.Success, Is.True, "parity length " + parityLength);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(result.Codewords, Is.EqualTo(original));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(errorCount));
            Assert.That(result.CorrectedErasureCount, Is.Zero);
            Assert.That(result.Verified, Is.True);
            Assert.That(result.Syndromes, Is.Not.All.Zero);
            Assert.That(supplied, Is.EqualTo(damaged), "caller buffer was modified");
            Assert.That(result.Codewords, Is.Not.SameAs(supplied));
            Assert.That(
                ReedSolomonDecoder.CalculateSyndromes(result.Codewords, parityLength),
                Is.All.Zero);
        }
    }

    [Test]
    public void ValidBlockNeedsNoRepairAndIsReturnedAsACopy()
    {
        byte[] original = CreateBlock(CreateData(19), 7);
        QrCorrectionResult result = ReedSolomonDecoder.CorrectCodewords(original, 7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(result.Codewords, Is.EqualTo(original));
            Assert.That(result.Codewords, Is.Not.SameAs(original));
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.CorrectedErasureCount, Is.Zero);
            Assert.That(result.Syndromes, Is.All.Zero);
            Assert.That(result.Verified, Is.True);
        });
    }

    [Test]
    public void ErrorsBeyondTheCorrectionBoundReturnVerifiedFailureWithoutFabricatedData()
    {
        byte[] original = CreateBlock(CreateData(29), 7);
        byte[] damaged = (byte[])original.Clone();
        int[] positions = new int[] { 1, 8, 17, 31 };
        int[] magnitudes = new int[] { 19, 87, 166, 241 };
        for (int index = 0; index < positions.Length; index++)
        {
            damaged[positions[index]] ^= (byte)magnitudes[index];
        }

        byte[] supplied = (byte[])damaged.Clone();
        QrCorrectionResult result = ReedSolomonDecoder.CorrectCodewords(supplied, 7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
            Assert.That(result.Codewords, Is.EqualTo(damaged));
            Assert.That(result.Codewords, Is.Not.SameAs(supplied));
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.CorrectedErasureCount, Is.Zero);
            Assert.That(result.Syndromes, Is.Not.All.Zero);
            Assert.That(result.Verified, Is.False);
            Assert.That(supplied, Is.EqualTo(damaged));
        });
    }

    [Test]
    public void MixedErrorsAndErasuresAreCorrectedAtTheExactBound()
    {
        byte[] original = CreateBlock(CreateData(37), 10);
        byte[] damaged = (byte[])original.Clone();
        int[] erasurePositions = new int[] { 35, 0, 46, 12 };
        int[] errorPositions = new int[] { 7, 19, 28 };
        for (int index = 0; index < erasurePositions.Length; index++)
        {
            damaged[erasurePositions[index]] ^= (byte)(index * 41 + 29);
        }

        for (int index = 0; index < errorPositions.Length; index++)
        {
            damaged[errorPositions[index]] ^= (byte)(index * 67 + 53);
        }

        byte[] supplied = (byte[])damaged.Clone();
        int[] suppliedErasures = (int[])erasurePositions.Clone();
        QrCorrectionResult result = ReedSolomonDecoder.CorrectCodewordsWithErasures(
            supplied,
            10,
            suppliedErasures);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(result.Codewords, Is.EqualTo(original));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(3));
            Assert.That(result.CorrectedErasureCount, Is.EqualTo(4));
            Assert.That(result.Syndromes, Is.Not.All.Zero);
            Assert.That(result.Verified, Is.True);
            Assert.That(supplied, Is.EqualTo(damaged));
            Assert.That(suppliedErasures, Is.EqualTo(erasurePositions));
        });
    }

    [Test]
    public void EntireParityBudgetCanBeUsedForKnownErasures()
    {
        byte[] original = CreateBlock(CreateData(21), 7);
        byte[] damaged = (byte[])original.Clone();
        int[] erasurePositions = new int[] { 27, 2, 19, 7, 14, 0, 24 };
        for (int index = 0; index < erasurePositions.Length; index++)
        {
            damaged[erasurePositions[index]] ^= (byte)(index * 31 + 11);
        }

        QrCorrectionResult result = ReedSolomonDecoder.CorrectCodewordsWithErasures(
            damaged,
            7,
            erasurePositions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Codewords, Is.EqualTo(original));
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.CorrectedErasureCount, Is.EqualTo(7));
            Assert.That(result.Verified, Is.True);
        });
    }

    [Test]
    public void MixedDamageBeyondTheWeightedBoundIsRejected()
    {
        byte[] original = CreateBlock(CreateData(31), 10);
        byte[] damaged = (byte[])original.Clone();
        int[] erasurePositions = new int[] { 0, 5, 10, 15, 20 };
        int[] errorPositions = new int[] { 25, 30, 35 };
        for (int index = 0; index < erasurePositions.Length; index++)
        {
            damaged[erasurePositions[index]] ^= (byte)(index * 43 + 7);
        }

        for (int index = 0; index < errorPositions.Length; index++)
        {
            damaged[errorPositions[index]] ^= (byte)(index * 59 + 23);
        }

        QrCorrectionResult result = ReedSolomonDecoder.CorrectCodewordsWithErasures(
            damaged,
            10,
            erasurePositions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
            Assert.That(result.Codewords, Is.EqualTo(damaged));
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.CorrectedErasureCount, Is.Zero);
            Assert.That(result.Verified, Is.False);
        });
    }

    [Test]
    public void ErasureCountBeyondParityIsRejectedEvenForAValidBlock()
    {
        byte[] block = CreateBlock(CreateData(19), 7);
        QrCorrectionResult result = ReedSolomonDecoder.CorrectCodewordsWithErasures(
            block,
            7,
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
            Assert.That(result.Codewords, Is.EqualTo(block));
            Assert.That(result.Verified, Is.False);
        });
    }

    [Test]
    public void RejectsInvalidErasurePositionLists()
    {
        byte[] block = CreateBlock(CreateData(19), 7);
        Assert.Multiple(() =>
        {
            Assert.That(() => ReedSolomonDecoder.CorrectCodewordsWithErasures(
                    block,
                    7,
                    null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonDecoder.CorrectCodewordsWithErasures(
                    block,
                    7,
                    new int[] { 3, 3 }),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonDecoder.CorrectCodewordsWithErasures(
                    block,
                    7,
                    new int[] { -1 }),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonDecoder.CorrectCodewordsWithErasures(
                    block,
                    7,
                    new int[] { block.Length }),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsInvalidCodewordBlocksAndParityLengths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => ReedSolomonDecoder.CorrectCodewords(null!, 7),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonDecoder.CorrectCodewords(new byte[7], 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonDecoder.CorrectCodewords(new byte[7], 256),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonDecoder.CorrectCodewords(new byte[6], 7),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonDecoder.CorrectCodewords(new byte[256], 7),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    private static byte[] CreateData(int length)
    {
        byte[] data = new byte[length];
        for (int index = 0; index < data.Length; index++)
        {
            data[index] = (byte)((index * 149 + length * 11 + 37) & 255);
        }

        return data;
    }

    private static byte[] CreateBlock(byte[] data, int parityLength)
    {
        byte[] parity = ReedSolomonEncoder.CreateParity(data, parityLength);
        byte[] block = new byte[data.Length + parity.Length];
        Array.Copy(data, block, data.Length);
        Array.Copy(parity, 0, block, data.Length, parity.Length);
        return block;
    }

    private static bool[] GetStandardizedLengths()
    {
        bool[] lengths = new bool[256];
        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 40; version++)
            {
                lengths[EccBlockTable.GetEccCodewordCountPerBlock(version, level)] = true;
            }
        }

        lengths[MicroVersionTable.GetEccCodewordCount(1, -1)] = true;
        for (int version = 2; version <= 3; version++)
        {
            for (int level = 0; level <= 1; level++)
            {
                lengths[MicroVersionTable.GetEccCodewordCount(version, level)] = true;
            }
        }

        for (int level = 0; level <= 2; level++)
        {
            lengths[MicroVersionTable.GetEccCodewordCount(4, level)] = true;
        }

        for (int level = 0; level < 2; level++)
        {
            for (int version = 0; version < 32; version++)
            {
                lengths[RectangularMicroVersionTable.GetEccCodewordCountPerBlock(version, level)] = true;
            }
        }

        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 14; version++)
            {
                lengths[LegacyModel1Table.GetErrorCorrectionCodewordCountPerBlock(version, level)] = true;
            }
        }

        return lengths;
    }

    private static int PowerReference(int value, int exponent)
    {
        int result = 1;
        for (int index = 0; index < exponent; index++)
        {
            result = MultiplyReference(result, value);
        }

        return result;
    }

    private static int MultiplyReference(int left, int right)
    {
        int product = 0;
        int multiplicand = left;
        int multiplier = right;
        while (multiplier != 0)
        {
            if ((multiplier & 1) != 0)
            {
                product ^= multiplicand;
            }

            multiplier >>= 1;
            multiplicand <<= 1;
            if ((multiplicand & 0x100) != 0)
            {
                multiplicand ^= 0x11D;
            }
        }

        return product;
    }
}
