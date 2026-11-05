namespace barcode.Tests;

public class DataMatrixReedSolomonTests
{
    [Test]
    public void GeneratorAndParityMatchEcc200AnnexExample()
    {
        GaloisPolynomial256 generator = DataMatrixReedSolomon.BuildGenerator(5);
        int[] firstData = new int[] { 142, 164, 186 };
        int[] secondData = new int[] { 66, 129, 70 };

        Assert.Multiple(() =>
        {
            Assert.That(generator.CopyCoefficients(), Is.EqualTo(new int[] { 1, 62, 111, 15, 48, 228 }));
            Assert.That(DataMatrixReedSolomon.EncodeParity(firstData, 5), Is.EqualTo(new int[] { 114, 25, 5, 88, 102 }));
            Assert.That(DataMatrixReedSolomon.EncodeCodewords(firstData, 5), Is.EqualTo(new int[] { 142, 164, 186, 114, 25, 5, 88, 102 }));
            Assert.That(DataMatrixReedSolomon.EncodeParity(secondData, 5), Is.EqualTo(new int[] { 138, 234, 82, 82, 95 }));
        });
    }

    [TestCase(5)]
    [TestCase(7)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(14)]
    [TestCase(18)]
    [TestCase(20)]
    [TestCase(24)]
    [TestCase(28)]
    [TestCase(36)]
    [TestCase(42)]
    [TestCase(48)]
    [TestCase(56)]
    [TestCase(62)]
    [TestCase(68)]
    public void EveryEcc200BlockSizeProducesZeroSyndromes(int errorCorrectionCount)
    {
        int[] data = CreateData(40);

        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(data, errorCorrectionCount);
        int[] syndromes = DataMatrixReedSolomon.CalculateSyndromes(encoded, errorCorrectionCount);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Length, Is.EqualTo(data.Length + errorCorrectionCount));
            Assert.That(syndromes, Is.EqualTo(new int[errorCorrectionCount]));
            Assert.That(DataMatrixReedSolomon.VerifyCodewords(encoded, errorCorrectionCount), Is.True);
            Assert.That(DataMatrixReedSolomon.IsSupportedErrorCorrectionCount(errorCorrectionCount), Is.True);
        });
    }

    [Test]
    public void SyndromeCalculationMatchesIndependentFieldEvaluation()
    {
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(new int[] { 142, 164, 186 }, 5);
        encoded[2] = encoded[2] ^ 77;

        int[] syndromes = DataMatrixReedSolomon.CalculateSyndromes(encoded, 5);
        int[] expected = CalculateSyndromesReference(encoded, 5);

        Assert.Multiple(() =>
        {
            Assert.That(syndromes, Is.EqualTo(expected));
            Assert.That(syndromes, Is.Not.EqualTo(new int[5]));
            Assert.That(DataMatrixReedSolomon.VerifyCodewords(encoded, 5), Is.False);
        });
    }

    [Test]
    public void CorrectionRepairsEverySinglePositionAndPreservesInput()
    {
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(new int[] { 142, 164, 186 }, 5);
        int position = 0;
        while (position < encoded.Length)
        {
            int[] damaged = CopyArray(encoded);
            damaged[position] = damaged[position] ^ (position + 1);

            DataMatrixCorrectionResult result = DataMatrixReedSolomon.CorrectCodewords(damaged, 5, null);

            Assert.That(result.Success, Is.True, $"Position {position} was not corrected: {result.FailureCode}.");
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(1));
            Assert.That(result.CorrectedIndices, Is.EqualTo(new int[] { position }));
            Assert.That(damaged[position], Is.Not.EqualTo(encoded[position]));
            position = position + 1;
        }
    }

    [TestCase(5)]
    [TestCase(7)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(14)]
    [TestCase(18)]
    [TestCase(20)]
    [TestCase(24)]
    [TestCase(28)]
    [TestCase(36)]
    [TestCase(42)]
    [TestCase(48)]
    [TestCase(56)]
    [TestCase(62)]
    [TestCase(68)]
    public void CorrectionRepairsMaximumUnknownErrorsForEveryBlockSize(int errorCorrectionCount)
    {
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(CreateData(40), errorCorrectionCount);
        int errorCount = errorCorrectionCount / 2;
        int[] indices = CreateSequentialIndices(errorCount, 0);
        int[] damaged = InjectErrors(encoded, indices);

        DataMatrixCorrectionResult result = DataMatrixReedSolomon.CorrectCodewords(damaged, errorCorrectionCount, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.FailureMessage);
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(errorCount));
            Assert.That(result.CorrectedIndices, Is.EquivalentTo(indices));
            Assert.That(result.ErasureCount, Is.Zero);
        });
    }

    [TestCase(5)]
    [TestCase(7)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(14)]
    [TestCase(18)]
    [TestCase(20)]
    [TestCase(24)]
    [TestCase(28)]
    [TestCase(36)]
    [TestCase(42)]
    [TestCase(48)]
    [TestCase(56)]
    [TestCase(62)]
    [TestCase(68)]
    public void CorrectionRepairsMaximumErasuresForEveryBlockSize(int errorCorrectionCount)
    {
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(CreateData(40), errorCorrectionCount);
        int[] erasures = CreateSequentialIndices(errorCorrectionCount, 0);
        int[] damaged = InjectErrors(encoded, erasures);

        DataMatrixCorrectionResult result = DataMatrixReedSolomon.CorrectCodewords(damaged, errorCorrectionCount, erasures);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.FailureMessage);
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(errorCorrectionCount));
            Assert.That(result.CorrectedIndices, Is.EquivalentTo(erasures));
            Assert.That(result.ErasureCount, Is.EqualTo(errorCorrectionCount));
        });
    }

    [TestCase(5)]
    [TestCase(7)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(14)]
    [TestCase(18)]
    [TestCase(20)]
    [TestCase(24)]
    [TestCase(28)]
    [TestCase(36)]
    [TestCase(42)]
    [TestCase(48)]
    [TestCase(56)]
    [TestCase(62)]
    [TestCase(68)]
    public void CorrectionRepairsMixedErrorsAndErasuresAtCapacity(int errorCorrectionCount)
    {
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(CreateData(40), errorCorrectionCount);
        int erasureCount = errorCorrectionCount % 2;
        if (erasureCount == 0)
        {
            erasureCount = 2;
        }

        int unknownCount = (errorCorrectionCount - erasureCount) / 2;
        int[] erasures = CreateSequentialIndices(erasureCount, 0);
        int[] unknown = CreateSequentialIndices(unknownCount, erasureCount);
        int[] all = CombineArrays(erasures, unknown);
        int[] damaged = InjectErrors(encoded, all);

        DataMatrixCorrectionResult result = DataMatrixReedSolomon.CorrectCodewords(damaged, errorCorrectionCount, erasures);

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
    public void KnownButUnchangedErasureDoesNotInflateCorrectedErrorCount()
    {
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(CreateData(10), 7);
        int[] damaged = CopyArray(encoded);
        damaged[4] = damaged[4] ^ 91;

        DataMatrixCorrectionResult result = DataMatrixReedSolomon.CorrectCodewords(damaged, 7, new int[] { 1 });

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
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(CreateData(10), 7);

        DataMatrixCorrectionResult result = DataMatrixReedSolomon.CorrectCodewords(encoded, 7, new int[] { 1 });
        encoded[0] = encoded[0] ^ 1;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.ErasureCount, Is.EqualTo(1));
            Assert.That(result.CorrectedIndices, Is.Empty);
            Assert.That(DataMatrixReedSolomon.VerifyCodewords(result.CorrectedCodewords, 7), Is.True);
            Assert.That(result.FailureCode, Is.Null);
            Assert.That(result.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void OverCapacityDamageReturnsFailureAndOriginalDamagedBlock()
    {
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(CreateData(20), 10);
        int[] indices = CreateSequentialIndices(6, 0);
        int[] damaged = InjectErrors(encoded, indices);

        DataMatrixCorrectionResult result = DataMatrixReedSolomon.CorrectCodewords(damaged, 10, null);
        int[] failureCopy = CopyArray(result.CorrectedCodewords);
        damaged[0] = damaged[0] ^ 1;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(failureCopy, Is.EqualTo(InjectErrors(encoded, indices)));
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
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(CreateData(10), 5);
        int[] erasures = CreateSequentialIndices(6, 0);

        DataMatrixCorrectionResult result = DataMatrixReedSolomon.CorrectCodewords(encoded, 5, erasures);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo("TOO_MANY_ERASURES"));
            Assert.That(result.CorrectedCodewords, Is.EqualTo(encoded));
            Assert.That(result.ErasureCount, Is.EqualTo(6));
        });
    }

    [Test]
    public void ReedSolomonOperationsRejectMalformedBlocksCountsAndErasures()
    {
        int[] encoded = DataMatrixReedSolomon.EncodeCodewords(CreateData(10), 5);

        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixReedSolomon.IsSupportedErrorCorrectionCount(6), Is.False);
            Assert.That(() => DataMatrixReedSolomon.BuildGenerator(6), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixReedSolomon.EncodeParity(null!, 5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixReedSolomon.EncodeParity(new int[0], 5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixReedSolomon.EncodeParity(new int[] { 256 }, 5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixReedSolomon.EncodeParity(new int[251], 5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixReedSolomon.CalculateSyndromes(null!, 5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixReedSolomon.CalculateSyndromes(new int[5], 5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixReedSolomon.CorrectCodewords(encoded, 5, new int[] { -1 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixReedSolomon.CorrectCodewords(encoded, 5, new int[] { encoded.Length }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixReedSolomon.CorrectCodewords(encoded, 5, new int[] { 1, 1 }), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    private static int[] CreateData(int length)
    {
        int[] data = new int[length];
        int index = 0;
        while (index < length)
        {
            data[index] = ((index * 149) + 31) & 0xFF;
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

    private static int[] InjectErrors(int[] source, int[] indices)
    {
        int[] damaged = CopyArray(source);
        int index = 0;
        while (index < indices.Length)
        {
            damaged[indices[index]] = damaged[indices[index]] ^ ((index % 255) + 1);
            index = index + 1;
        }

        return damaged;
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
            int point = PowerReference(2, syndromeIndex + 1);
            int value = 0;
            int codewordIndex = 0;
            while (codewordIndex < received.Length)
            {
                value = MultiplyReference(value, point) ^ received[codewordIndex];
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
            result = MultiplyReference(result, value);
            index = index + 1;
        }

        return result;
    }

    private static int MultiplyReference(int left, int right)
    {
        int product = 0;
        int multiplicand = left;
        int multiplier = right;
        int bit = 0;
        while (bit < 8)
        {
            if ((multiplier & 1) != 0)
            {
                product = product ^ multiplicand;
            }

            multiplier = multiplier >> 1;
            multiplicand = multiplicand << 1;
            if ((multiplicand & 0x100) != 0)
            {
                multiplicand = multiplicand ^ 0x12D;
            }

            bit = bit + 1;
        }

        return product & 0xFF;
    }
}
