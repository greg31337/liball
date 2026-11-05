namespace barcode.Tests;

public class Pdf417ReedSolomonTests
{
    [Test]
    public void GeneratorAndParityMatchPublishedLevelFiveVector()
    {
        int[] data = CreatePublishedData();
        int[] expectedParity = new int[]
        {
            769, 843, 591, 910, 605, 206, 706, 917, 371, 469, 79, 718, 47, 777, 249, 262,
            193, 620, 597, 477, 450, 806, 908, 309, 153, 871, 686, 838, 185, 674, 68, 679,
            691, 794, 497, 479, 234, 250, 496, 43, 347, 582, 882, 536, 322, 317, 273, 194,
            917, 237, 420, 859, 340, 115, 222, 808, 866, 836, 417, 121, 833, 459, 64, 159
        };

        Assert.Multiple(() =>
        {
            Assert.That(Pdf417ReedSolomon.BuildGenerator(0).CopyCoefficients(), Is.EqualTo(new int[] { 1, 917, 27 }));
            Assert.That(Pdf417ReedSolomon.EncodeParity(data, 5), Is.EqualTo(expectedParity));
            Assert.That(Pdf417ReedSolomon.EncodeCodewords(data, 5), Is.EqualTo(CombineArrays(data, expectedParity)));
        });
    }

    [TestCase(0, 2)]
    [TestCase(1, 4)]
    [TestCase(2, 8)]
    [TestCase(3, 16)]
    [TestCase(4, 32)]
    [TestCase(5, 64)]
    [TestCase(6, 128)]
    [TestCase(7, 256)]
    [TestCase(8, 512)]
    public void EveryPdf417LevelProducesExpectedParityCountAndZeroSyndromes(int level, int expectedCount)
    {
        int[] data = CreateData(40);

        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(data, level);
        int[] syndromes = Pdf417ReedSolomon.CalculateSyndromes(encoded, level);

        Assert.Multiple(() =>
        {
            Assert.That(Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(level), Is.EqualTo(expectedCount));
            Assert.That(encoded.Length, Is.EqualTo(data.Length + expectedCount));
            Assert.That(syndromes, Is.EqualTo(new int[expectedCount]));
            Assert.That(Pdf417ReedSolomon.VerifyCodewords(encoded, level), Is.True);
        });
    }

    [Test]
    public void SyndromeCalculationMatchesIndependentModuloEvaluation()
    {
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(20), 3);
        encoded[2] = (encoded[2] + 77) % 929;

        int[] syndromes = Pdf417ReedSolomon.CalculateSyndromes(encoded, 3);
        int[] expected = CalculateSyndromesReference(encoded, 16);

        Assert.Multiple(() =>
        {
            Assert.That(syndromes, Is.EqualTo(expected));
            Assert.That(syndromes, Is.Not.EqualTo(new int[16]));
            Assert.That(Pdf417ReedSolomon.VerifyCodewords(encoded, 3), Is.False);
        });
    }

    [Test]
    public void CorrectionRepairsEveryPositionAndPreservesInput()
    {
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(12), 2);
        int position = 0;
        while (position < encoded.Length)
        {
            int[] damaged = CopyArray(encoded);
            damaged[position] = (damaged[position] + position + 1) % 929;

            Pdf417CorrectionResult result = Pdf417ReedSolomon.CorrectCodewords(damaged, 2, null);

            Assert.That(result.Success, Is.True, $"Position {position} was not corrected: {result.FailureCode}.");
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(1));
            Assert.That(result.CorrectedIndices, Is.EqualTo(new int[] { position }));
            Assert.That(damaged[position], Is.Not.EqualTo(encoded[position]));
            position = position + 1;
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void CorrectionRepairsMaximumUnknownErrorsAtEveryLevel(int level)
    {
        int count = Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(level);
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(40), level);
        int[] indices = CreateSequentialIndices(count / 2, 0);
        int[] damaged = TestErrorInjector.ChangePdf417CodewordsAtIndices(
            encoded,
            indices);

        Pdf417CorrectionResult result = Pdf417ReedSolomon.CorrectCodewords(damaged, level, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.FailureMessage);
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(indices.Length));
            Assert.That(result.CorrectedIndices, Is.EquivalentTo(indices));
            Assert.That(result.ErasureCount, Is.Zero);
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void MaximumLengthBlocksCorrectSpreadErrorsAtEveryLevel(int level)
    {
        int errorCorrectionCount = Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(level);
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(
            CreateData(Pdf417ReedSolomon.MaximumBlockLength - errorCorrectionCount),
            level);
        int correctionLimit = errorCorrectionCount / 2;
        int[] indices = CreateSpreadIndices(encoded.Length, correctionLimit);
        int[] damaged = TestErrorInjector.ChangePdf417CodewordsAtIndices(
            encoded,
            indices);

        Pdf417CorrectionResult result = Pdf417ReedSolomon.CorrectCodewords(
            damaged,
            level,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Has.Length.EqualTo(Pdf417ReedSolomon.MaximumBlockLength));
            Assert.That(result.Success, Is.True, result.FailureMessage);
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(correctionLimit));
            Assert.That(result.CorrectedIndices, Is.EquivalentTo(indices));
            Assert.That(indices[indices.Length - 1], Is.EqualTo(encoded.Length - 1));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void CorrectionRepairsMaximumErasuresAtEveryLevel(int level)
    {
        int count = Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(level);
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(40), level);
        int[] erasures = CreateSequentialIndices(count, 0);
        int[] damaged = TestErrorInjector.ChangePdf417CodewordsAtIndices(
            encoded,
            erasures);

        Pdf417CorrectionResult result = Pdf417ReedSolomon.CorrectCodewords(damaged, level, erasures);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.FailureMessage);
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(erasures.Length));
            Assert.That(result.CorrectedIndices, Is.EquivalentTo(erasures));
            Assert.That(result.ErasureCount, Is.EqualTo(erasures.Length));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void CorrectionRepairsMixedErrorsAndErasuresAtCapacity(int level)
    {
        int count = Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(level);
        int erasureCount = 2;
        int unknownCount = (count - erasureCount) / 2;
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(40), level);
        int[] erasures = CreateSequentialIndices(erasureCount, 0);
        int[] unknown = CreateSequentialIndices(unknownCount, erasureCount);
        int[] all = CombineArrays(erasures, unknown);
        int[] damaged = TestErrorInjector.ChangePdf417CodewordsAtIndices(
            encoded,
            all);

        Pdf417CorrectionResult result = Pdf417ReedSolomon.CorrectCodewords(damaged, level, erasures);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.FailureMessage);
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(all.Length));
            Assert.That(result.CorrectedIndices, Is.EquivalentTo(all));
            Assert.That(result.ErasureCount, Is.EqualTo(erasureCount));
        });
    }

    [Test]
    public void KnownButUnchangedErasureDoesNotInflateCorrectionCount()
    {
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(10), 2);
        int[] damaged = CopyArray(encoded);
        damaged[4] = (damaged[4] + 91) % 929;

        Pdf417CorrectionResult result = Pdf417ReedSolomon.CorrectCodewords(damaged, 2, new int[] { 1 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.FailureMessage);
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(1));
            Assert.That(result.CorrectedIndices, Is.EqualTo(new int[] { 4 }));
            Assert.That(result.ErasureCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void CleanBlockReturnsDefensiveSuccessfulCopy()
    {
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(10), 2);

        Pdf417CorrectionResult result = Pdf417ReedSolomon.CorrectCodewords(encoded, 2, new int[] { 1 });
        encoded[0] = (encoded[0] + 1) % 929;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.ErasureCount, Is.EqualTo(1));
            Assert.That(result.CorrectedIndices, Is.Empty);
            Assert.That(Pdf417ReedSolomon.VerifyCodewords(result.CorrectedCodewords, 2), Is.True);
            Assert.That(result.FailureCode, Is.Null);
            Assert.That(result.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void OverCapacityDamageReturnsFailureAndOriginalDamagedBlock()
    {
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(20), 3);
        int[] indices = CreateSequentialIndices(9, 0);
        int[] damaged = TestErrorInjector.ChangePdf417CodewordsAtIndices(
            encoded,
            indices);

        Pdf417CorrectionResult result = Pdf417ReedSolomon.CorrectCodewords(damaged, 3, null);
        int[] failureCopy = CopyArray(result.CorrectedCodewords);
        damaged[0] = (damaged[0] + 1) % 929;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(failureCopy, Is.EqualTo(
                TestErrorInjector.ChangePdf417CodewordsAtIndices(
                    encoded,
                    indices)));
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.CorrectedIndices, Is.Empty);
            Assert.That(result.FailureCode, Is.Not.Null.And.Not.Empty);
            Assert.That(result.FailureMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(result.CorrectedCodewords, Is.EqualTo(failureCopy));
        });
    }

    [Test]
    public void TooManyErasuresReturnsStableFailure()
    {
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(10), 1);
        int[] erasures = CreateSequentialIndices(5, 0);

        Pdf417CorrectionResult result = Pdf417ReedSolomon.CorrectCodewords(encoded, 1, erasures);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo("TOO_MANY_ERASURES"));
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.ErasureCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void ReedSolomonOperationsRejectMalformedLevelsBlocksAndErasures()
    {
        int[] encoded = Pdf417ReedSolomon.EncodeCodewords(CreateData(10), 2);

        Assert.Multiple(() =>
        {
            Assert.That(() => Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(9), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.EncodeParity(null!, 2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.EncodeParity(new int[0], 2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.EncodeParity(new int[] { 929 }, 2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.EncodeParity(new int[417], 8), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.CalculateSyndromes(null!, 2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.CalculateSyndromes(new int[8], 2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.CorrectCodewords(encoded, 2, new int[] { -1 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.CorrectCodewords(encoded, 2, new int[] { encoded.Length }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417ReedSolomon.CorrectCodewords(encoded, 2, new int[] { 1, 1 }), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    private static int[] CreatePublishedData()
    {
        return new int[]
        {
            48, 901, 56, 141, 627, 856, 330, 69, 244, 900, 852, 169, 843, 895, 852, 895,
            913, 154, 845, 778, 387, 89, 869, 901, 219, 474, 543, 650, 169, 201, 9, 160,
            35, 70, 900, 900, 900, 900, 900, 900, 900, 900, 900, 900, 900, 900, 900, 900
        };
    }

    private static int[] CreateData(int length)
    {
        int[] data = new int[length];
        int index = 0;
        while (index < length)
        {
            data[index] = ((index * 487) + 31) % 929;
            index = index + 1;
        }

        return data;
    }

    private static int[] CreateSequentialIndices(int count, int start)
    {
        int[] indices = new int[count];
        int index = 0;
        while (index < count)
        {
            indices[index] = start + index;
            index = index + 1;
        }

        return indices;
    }

    private static int[] CreateSpreadIndices(int length, int count)
    {
        int[] indices = new int[count];
        if (count == 1)
        {
            indices[0] = length - 1;
            return indices;
        }

        int index = 0;
        while (index < count)
        {
            indices[index] = (index * (length - 1)) / (count - 1);
            index = index + 1;
        }

        return indices;
    }

    private static int[] CombineArrays(int[] first, int[] second)
    {
        int[] combined = new int[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    private static int[] CopyArray(int[] source)
    {
        int[] copy = new int[source.Length];
        source.CopyTo(copy, 0);
        return copy;
    }

    private static int[] CalculateSyndromesReference(int[] received, int count)
    {
        int[] syndromes = new int[count];
        int syndromeIndex = 0;
        while (syndromeIndex < count)
        {
            int point = PowerReference(3, syndromeIndex + 1);
            int value = 0;
            int codewordIndex = 0;
            while (codewordIndex < received.Length)
            {
                value = ((value * point) + received[codewordIndex]) % 929;
                codewordIndex = codewordIndex + 1;
            }

            syndromes[syndromeIndex] = value;
            syndromeIndex = syndromeIndex + 1;
        }

        return syndromes;
    }

    private static int PowerReference(int value, int exponent)
    {
        int result = 1;
        int index = 0;
        while (index < exponent)
        {
            result = (result * value) % 929;
            index = index + 1;
        }

        return result;
    }
}
