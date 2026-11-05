using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class UnusedErrorCorrectionMeasurerTests
{
    [TestCase(QrSymbolKind.Model2, 5, 2)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    [TestCase(QrSymbolKind.LegacyModel1, 14, 3)]
    public void DecodingRetainsActualBlockCapacityAndUsage(QrSymbolKind kind, int version, int level)
    {
        byte[] words = CodewordErasureRecoveryTests.Words(kind, version, level);
        words[0] ^= 0x55;
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(words, kind, version, level);
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        QrDecodeResult decoded = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions());
        UnusedErrorCorrectionMeasurement result = Measure(decoded);
        Assert.That(decoded.Text, Is.EqualTo("123"));
        Assert.That(decoded.CorrectedErrorCount, Is.EqualTo(1));
        Assert.That(decoded.ErrorCorrectionBlocks.Sum(b => b.CorrectedErrorCount), Is.EqualTo(1));
        Assert.That(decoded.ErrorCorrectionBlocks.Sum(b => b.ErrorCorrectionCodewordCount), Is.EqualTo(decoded.Symbol!.ErrorCorrectionCodewordCount));
        Assert.That(result.ConsumedCapacity.Sum(), Is.EqualTo(2));
        Assert.That(result.LimitingBlockIndex, Is.Zero);
        Assert.That(result.UnusedErrorCorrection.Value, Is.EqualTo(1.0 - 2.0 / result.BlockCapacities[0]).Within(1e-12));
        Assert.That(matrix.Values, Is.EqualTo(before));
        AssertMetric(result);

        // A later decode owns independent block diagnostics and codeword arrays.
        QrDecodeResult repeated = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(repeated.ErrorCorrectionBlocks, Is.Not.SameAs(decoded.ErrorCorrectionBlocks));
        Assert.That(repeated.ErrorCorrectionBlocks[0], Is.Not.SameAs(decoded.ErrorCorrectionBlocks[0]));
        Assert.That(repeated.ErrorCorrectionBlocks[0].Codewords, Is.Not.SameAs(decoded.ErrorCorrectionBlocks[0].Codewords));
        Assert.That(Measure(repeated).UnusedErrorCorrection.Value, Is.EqualTo(result.UnusedErrorCorrection.Value));
    }

    [TestCase(0, 0, 1.0)]
    [TestCase(1, 0, 0.8)]
    [TestCase(0, 1, 0.9)]
    [TestCase(2, 3, 0.3)]
    [TestCase(3, 4, 0.0)]
    [TestCase(0, 10, 0.0)]
    public void WeightsActualErrorsTwiceAndErasuresOnce(int errors, int erasures, double unused)
    {
        QrCorrectionResult block = Correct(10, errors, erasures);
        Assert.That(block.Success, Is.True);
        Assert.That(block.Verified, Is.True);
        Assert.That(block.CorrectedErrorCount, Is.EqualTo(errors));
        Assert.That(block.CorrectedErasureCount, Is.EqualTo(erasures));
        Assert.That(block.ErrorCorrectionCodewordCount, Is.EqualTo(10));
        UnusedErrorCorrectionMeasurement result = Measure(Decoded(block));
        Assert.That(result.BlockCapacities, Is.EqualTo(new[] { 10 }));
        Assert.That(result.ConsumedCapacity, Is.EqualTo(new[] { errors * 2 + erasures }));
        Assert.That(result.UnusedFractions[0], Is.EqualTo(unused).Within(1e-12));
        Assert.That(result.UnusedErrorCorrection.Value, Is.EqualTo(unused).Within(1e-12));
        AssertMetric(result);
    }

    [TestCase(QrSymbolKind.Model2, 5, 2)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    [TestCase(QrSymbolKind.LegacyModel1, 14, 3)]
    public void MatrixDecodingPreservesErrorsAndErasuresSeparately(QrSymbolKind kind, int version, int level)
    {
        byte[] words = CodewordErasureRecoveryTests.Words(kind, version, level);
        words[0] ^= 0x55;
        words[1] ^= 0x55;
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(words, kind, version, level);
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        foreach (int cell in CodewordErasureRecoveryTests.Cells(kind, version, level, 0)) matrix.Confidence[cell] = 0;
        QrDecodeResult decoded = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(decoded.Text, Is.EqualTo("123"));
        Assert.That(decoded.CorrectedErrorCount, Is.EqualTo(1));
        Assert.That(decoded.CorrectedErasureCount, Is.EqualTo(1));
        Assert.That(decoded.ErrorCorrectionBlocks.Sum(b => b.CorrectedErrorCount), Is.EqualTo(1));
        Assert.That(decoded.ErrorCorrectionBlocks.Sum(b => b.CorrectedErasureCount), Is.EqualTo(1));
        UnusedErrorCorrectionMeasurement result = Measure(decoded);
        Assert.That(result.ConsumedCapacity.Sum(), Is.EqualTo(3));
        Assert.That(result.UnusedErrorCorrection.Value, Is.EqualTo(result.UnusedFractions.Min()));
        AssertMetric(result);
    }

    [Test]
    public void LegacyRemainderCodewordsAreExcludedFromCapacity()
    {
        const int version = 7, level = 3;
        byte[] words = CodewordErasureRecoveryTests.Words(QrSymbolKind.LegacyModel1, version, level);
        QrDecodeResult decoded = new QrDecoder().DecodeMatrix(
            CodewordErasureRecoveryTests.Build(words, QrSymbolKind.LegacyModel1, version, level), new QrDecodeOptions());
        int remainder = LegacyModel1Table.GetRemainderCodewordCount(version, level);
        Assert.That(remainder, Is.GreaterThan(0));
        UnusedErrorCorrectionMeasurement result = Measure(decoded);
        Assert.That(result.BlockCapacities.Sum(), Is.EqualTo(words.Length - LegacyModel1Table.GetDataCodewordCount(version, level) - remainder));
        Assert.That(result.UnusedErrorCorrection.Value, Is.EqualTo(1));
        AssertMetric(result);
    }

    [Test]
    public void OddParityCapacityRetainsTheLastErasureUnit()
    {
        UnusedErrorCorrectionMeasurement result = Measure(Decoded(Correct(7, 3, 0)));
        Assert.That(result.UnusedErrorCorrection.Value, Is.EqualTo(1.0 / 7).Within(1e-12));
        Assert.That(result.ConsumedCapacity, Is.EqualTo(new[] { 6 }));
    }

    [Test]
    public void CleanLowConfidenceCodewordsDoNotInventCorrectionUsage()
    {
        byte[] data = Enumerable.Range(1, 12).Select(i => (byte)i).ToArray();
        QrReedSolomonCodec codec = new QrReedSolomonCodec();
        byte[] words = data.Concat(codec.CreateErrorCorrection(data, 7)).ToArray();
        QrCorrectionResult block = codec.CorrectCodewords(words, 7, new[] { 0, 3, 5 });
        Assert.That(block.CorrectedErasureCount, Is.Zero);
        Assert.That(Measure(Decoded(block)).UnusedErrorCorrection.Value, Is.EqualTo(1));
    }

    [Test]
    public void EqualTotalErrorsHaveDifferentRemainingCapacityWhenConcentratedInOneBlock()
    {
        QrDecodeResult concentrated = DecodeDamagedModel2(3, 2, new[] { 0, 2 });
        QrDecodeResult distributed = DecodeDamagedModel2(3, 2, new[] { 0, 1 });
        Assert.That(concentrated.CorrectedErrorCount, Is.EqualTo(2));
        Assert.That(distributed.CorrectedErrorCount, Is.EqualTo(2));
        UnusedErrorCorrectionMeasurement first = Measure(concentrated);
        UnusedErrorCorrectionMeasurement second = Measure(distributed);
        Assert.That(first.UnusedFractions, Is.EqualTo(new[] { 14.0 / 18, 1.0 }));
        Assert.That(second.UnusedFractions, Is.EqualTo(new[] { 16.0 / 18, 16.0 / 18 }));
        Assert.That(first.UnusedErrorCorrection.Value, Is.LessThan(second.UnusedErrorCorrection.Value));
    }

    [Test]
    public void UnequalBlockLengthsAndAggregateCountersDoNotChangeParityBudgets()
    {
        QrDecodeResult decoded = DecodeDamagedModel2(5, 2, new[] { 3, 7 });
        Assert.That(decoded.ErrorCorrectionBlocks.Select(b => b.Codewords.Length).Distinct().Count(), Is.EqualTo(2));
        decoded.CorrectedErrorCount = int.MaxValue;
        decoded.CorrectedErasureCount = int.MaxValue;
        UnusedErrorCorrectionMeasurement result = Measure(decoded);
        Assert.That(result.BlockCapacities, Is.EqualTo(new[] { 18, 18, 18, 18 }));
        Assert.That(result.ConsumedCapacity, Is.EqualTo(new[] { 0, 0, 0, 4 }));
        Assert.That(result.LimitingBlockIndex, Is.EqualTo(3));
        Assert.That(result.UnusedErrorCorrection.Value, Is.EqualTo(14.0 / 18));
    }

    [Test]
    public void CleanMicroM1HasNoCorrectingCapacity()
    {
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.Micro, 1, -1), QrSymbolKind.Micro, 1, -1);
        QrDecodeResult decoded = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(decoded.Text, Is.EqualTo("123"));
        Assert.That(decoded.ErrorCorrectionBlocks[0].Verified, Is.True);
        AssertUnavailable(Measure(decoded));
    }

    [Test]
    public void FailedBlockDoesNotReportUnusedCapacityEvenWhenOtherBlocksAreClean()
    {
        QrCorrectionResult failed = Correct(7, 0, 8);
        Assert.That(failed.Success, Is.False);
        Assert.That(failed.ErrorCorrectionCodewordCount, Is.EqualTo(7));
        AssertUnavailable(Measure(Decoded(Correct(7, 0, 0), failed)));
    }

    [Test]
    public void RequiresVerifiedIndividualBlockEvidenceAndHonorsDisabledOption()
    {
        QrCorrectionResult block = Correct(7, 0, 0);
        block.Verified = false;
        AssertUnavailable(Measure(Decoded(block)));
        AssertUnavailable(Measure(new QrDecodeResult()));
        AssertUnavailable(Measure(new QrDecodeResult { Symbol = new QrSymbol() }));
        AssertUnavailable(Measure(new QrDecodeResult { ErrorCorrectionBlocks = new[] { Correct(7, 0, 0) } }));
        AssertUnavailable(UnusedErrorCorrectionMeasurer.Measure(Decoded(Correct(7, 0, 0)), new QrQualityOptions { AnalyzeUnusedErrorCorrection = false }));
    }

    [Test]
    public void RejectsMalformedBlockDiagnosticsWithoutOverflow()
    {
        Action<QrCorrectionResult>[] mutations =
        {
            b => b.ErrorCorrectionCodewordCount = 0,
            b => b.ErrorCorrectionCodewordCount = b.Codewords.Length,
            b => b.CorrectedErrorCount = -1,
            b => b.CorrectedErasureCount = -1,
            b => b.CorrectedErrorCount = int.MaxValue,
            b => b.CorrectedErasureCount = 8,
            b => b.Codewords = null!,
            b => b.Codewords = new byte[256],
            b => b.Syndromes = null!,
            b => b.Syndromes = new int[1]
        };
        foreach (Action<QrCorrectionResult> mutate in mutations)
        {
            QrCorrectionResult block = Correct(7, 0, 0);
            mutate(block);
            Assert.Throws<QrInputException>(() => Measure(Decoded(block)));
        }
        Assert.Throws<QrInputException>(() => Measure(Decoded(new QrCorrectionResult[] { null! })));
        Assert.Throws<QrInputException>(() => Measure(new QrDecodeResult { ErrorCorrectionBlocks = null! }));
        Assert.Throws<QrInputException>(() => Measure(null!));
        Assert.Throws<QrInputException>(() => UnusedErrorCorrectionMeasurer.Measure(Decoded(Correct(7, 0, 0)), null!));
    }

    static QrCorrectionResult Correct(int parity, int errors, int erasures)
    {
        QrReedSolomonCodec codec = new QrReedSolomonCodec();
        byte[] data = Enumerable.Range(1, 12).Select(i => (byte)i).ToArray();
        byte[] original = data.Concat(codec.CreateErrorCorrection(data, parity)).ToArray();
        byte[] words = (byte[])original.Clone();
        for (int index = 0; index < errors + erasures; index++) words[index] ^= (byte)(index + 77);
        byte[] before = (byte[])words.Clone();
        QrCorrectionResult result = codec.CorrectCodewords(words, parity, Enumerable.Range(0, erasures).ToArray());
        Assert.That(words, Is.EqualTo(before));
        if (result.Success) Assert.That(result.Codewords, Is.EqualTo(original));
        return result;
    }

    static QrDecodeResult DecodeDamagedModel2(int version, int level, int[] positions)
    {
        byte[] words = CodewordErasureRecoveryTests.Words(QrSymbolKind.Model2, version, level);
        foreach (int position in positions) words[position] ^= 0x55;
        return new QrDecoder().DecodeMatrix(CodewordErasureRecoveryTests.Build(words, QrSymbolKind.Model2, version, level), new QrDecodeOptions());
    }

    static QrDecodeResult Decoded(params QrCorrectionResult[] blocks) => new QrDecodeResult
    {
        Symbol = new QrSymbol { SymbolKind = QrSymbolKind.Model2, Version = 1 },
        ErrorCorrectionBlocks = blocks
    };

    static UnusedErrorCorrectionMeasurement Measure(QrDecodeResult decoded) => UnusedErrorCorrectionMeasurer.Measure(decoded, new QrQualityOptions());

    static void AssertMetric(UnusedErrorCorrectionMeasurement result)
    {
        Assert.That(result.UnusedErrorCorrection.IsCalculated, Is.True);
        Assert.That(result.UnusedErrorCorrection.Grade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(result.UnusedErrorCorrection.UsesCalibratedInputs, Is.False);
        Assert.That(result.UnusedErrorCorrection.Unit, Is.EqualTo("fraction"));
        Assert.That(result.UnusedErrorCorrection.Limitation, Is.Not.Empty);
    }

    static void AssertUnavailable(UnusedErrorCorrectionMeasurement result)
    {
        Assert.That(result.UnusedErrorCorrection.IsCalculated, Is.False);
        Assert.That(result.UnusedErrorCorrection.Grade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(result.UnusedErrorCorrection.Limitation, Is.Not.Empty);
        Assert.That(result.BlockCapacities, Is.Empty);
        Assert.That(result.UnusedFractions, Is.Empty);
        Assert.That(result.LimitingBlockIndex, Is.EqualTo(-1));
    }
}
