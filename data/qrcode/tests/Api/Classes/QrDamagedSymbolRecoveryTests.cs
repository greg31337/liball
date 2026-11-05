using System.Numerics;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrDamagedSymbolRecoveryTests
{
    static IEnumerable<(QrSymbolKind Kind, int Version, int Level)> Profiles()
    {
        foreach (int version in new[] { 1, 5, 7, 10, 27, 40 })
            for (int level = 0; level < 4; level++) yield return (QrSymbolKind.Model2, version, level);
        for (int version = 2; version <= 4; version++)
            for (int level = 0; level <= (version == 4 ? 2 : 1); level++) yield return (QrSymbolKind.Micro, version, level);
        for (int version = 0; version < 32; version++)
            for (int level = 0; level < 2; level++) yield return (QrSymbolKind.rMQR, version, level);
        foreach (int version in new[] { 1, 7, 14 })
            for (int level = 0; level < 4; level++) yield return (QrSymbolKind.LegacyModel1, version, level);
    }

    static IEnumerable<TestCaseData> RsCases()
    {
        foreach (var p in Profiles())
            foreach (string pattern in new[] { "errors", "erasures", "mixed" })
                foreach (int below in new[] { 0, 1 })
                    yield return new(p.Kind, p.Version, p.Level, pattern, below);
    }

    [TestCaseSource(nameof(RsCases))]
    public void RepairsDataAndParityAtAndBelowEachBlockBoundary(QrSymbolKind kind, int version, int level, string pattern, int below)
    {
        byte[] words = CodewordErasureRecoveryTests.Words(kind, version, level);
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(words, kind, version, level);
        int ecc = CodewordErasureRecoveryTests.EccCount(kind, version, level);
        int errors = pattern == "errors" ? ecc / 2 - below : pattern == "mixed" ? 1 : 0;
        int erasures = pattern == "errors" ? 0 : ecc - 2 * errors - below;
        List<int>[] blocks = CodewordErasureRecoveryTests.BlockPositions(kind, version, level);
        // Damage both ends of the block list, including unequal short/long blocks.
        // Interleave data and parity positions so neither region is left untested.
        int[] damagedBlocks = new[] { 0, blocks.Length - 1 }.Distinct().ToArray();
        foreach (int block in damagedBlocks)
        {
            int[] positions = AlternatingEnds(blocks[block]);
            for (int i = 0; i < errors + erasures; i++)
                DamageWord(matrix, kind, version, level, positions[i], i >= errors, false);
        }
        QrModuleValue[] values = (QrModuleValue[])matrix.Values.Clone();
        byte[] confidence = (byte[])matrix.Confidence!.Clone();
        QrDecodeResult result = Decode(matrix, kind);
        Assert.That(result.Symbol!.Codewords, Is.EqualTo(words));
        Assert.That(result.CorrectedErrorCount, Is.EqualTo(errors * damagedBlocks.Length));
        Assert.That(result.CorrectedErasureCount, Is.EqualTo(erasures * damagedBlocks.Length));
        Assert.That(result.ErrorCorrectionBlocks, Has.Length.EqualTo(blocks.Length));
        for (int block = 0; block < blocks.Length; block++)
        {
            QrCorrectionResult correction = result.ErrorCorrectionBlocks[block];
            Assert.That(correction.Verified && correction.Success, Is.True);
            Assert.That(correction.ErrorCorrectionCodewordCount, Is.EqualTo(ecc));
            Assert.That(correction.CorrectedErrorCount, Is.EqualTo(damagedBlocks.Contains(block) ? errors : 0));
            Assert.That(correction.CorrectedErasureCount, Is.EqualTo(damagedBlocks.Contains(block) ? erasures : 0));
        }
        Assert.That(matrix.Values, Is.EqualTo(values));
        Assert.That(matrix.Confidence, Is.EqualTo(confidence));
    }

    static IEnumerable<TestCaseData> ExcessErasureCases() => Profiles().Select(p => new TestCaseData(p.Kind, p.Version, p.Level));

    [TestCaseSource(nameof(ExcessErasureCases))]
    public void ExtraUnknownLocationErrorIsRejectedForTheseFixedDamagePatterns(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Fixture(kind, version, level);
        int ecc = CodewordErasureRecoveryTests.EccCount(kind, version, level);
        int[] positions = AlternatingEnds(CodewordErasureRecoveryTests.BlockPositions(kind, version, level)[0]);
        // These specific patterns are uncorrectable. This does not claim that
        // every error pattern outside the guaranteed radius can be detected.
        foreach (int word in positions.Take(ecc / 2 + 1))
            DamageWord(matrix, kind, version, level, word, false, false);
        AssertFailure(matrix, kind, QrFailureReason.ChecksumFailure);
    }

    [TestCaseSource(nameof(ExcessErasureCases))]
    public void OneBlockBeyondItsErasureBudgetCannotBorrowCapacityFromCleanBlocks(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Fixture(kind, version, level);
        List<int>[] blocks = CodewordErasureRecoveryTests.BlockPositions(kind, version, level);
        int ecc = CodewordErasureRecoveryTests.EccCount(kind, version, level);
        foreach (int word in AlternatingEnds(blocks[^1]).Take(ecc + 1))
            DamageWord(matrix, kind, version, level, word, true, false);
        AssertFailure(matrix, kind, QrFailureReason.ChecksumFailure);
    }

    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.Micro, 4, 2)]
    [TestCase(QrSymbolKind.rMQR, 0, 0)]
    [TestCase(QrSymbolKind.LegacyModel1, 1, 0)]
    public void EveryMixedErrorErasureSplitRecoversAtTheGuaranteedBoundary(QrSymbolKind kind, int version, int level)
    {
        int ecc = CodewordErasureRecoveryTests.EccCount(kind, version, level);
        int[] positions = AlternatingEnds(CodewordErasureRecoveryTests.BlockPositions(kind, version, level)[0]);
        for (int errors = 0; errors <= ecc / 2; errors++)
        {
            int erasures = ecc - errors * 2;
            QrMatrix matrix = Fixture(kind, version, level);
            for (int i = 0; i < errors + erasures; i++)
                DamageWord(matrix, kind, version, level, positions[i], i >= errors, false);
            QrDecodeResult result = Decode(matrix, kind);
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(errors));
            Assert.That(result.CorrectedErasureCount, Is.EqualTo(erasures));
        }
    }

    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.rMQR, 0, 0)]
    [TestCase(QrSymbolKind.LegacyModel1, 1, 0)]
    public void UnknownModulesEraseWholeCodewordsWithoutCountingEachBitSeparately(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Fixture(kind, version, level);
        int ecc = CodewordErasureRecoveryTests.EccCount(kind, version, level);
        int[] positions = AlternatingEnds(CodewordErasureRecoveryTests.BlockPositions(kind, version, level)[0]);
        // Leave one parity equation spare and mix full-word and one-bit erasures.
        for (int i = 0; i < ecc - 1; i++)
            DamageWord(matrix, kind, version, level, positions[i], true, i % 2 == 0);
        QrDecodeResult result = Decode(matrix, kind);
        Assert.That(result.Symbol!.Codewords, Is.EqualTo(CodewordErasureRecoveryTests.Words(kind, version, level)));
        Assert.That(result.CorrectedErrorCount, Is.Zero);
        Assert.That(result.CorrectedErasureCount, Is.EqualTo(ecc - 1));
    }

    [Test]
    public void MicroM1DetectsDamageInEveryDataOrParityWordWithoutClaimingCorrection()
    {
        byte[] words = CodewordErasureRecoveryTests.Words(QrSymbolKind.Micro, 1, -1);
        for (int word = 0; word < words.Length; word++)
            foreach (bool erased in new[] { false, true })
            {
                QrMatrix matrix = CodewordErasureRecoveryTests.Build(words, QrSymbolKind.Micro, 1, -1);
                DamageWord(matrix, QrSymbolKind.Micro, 1, -1, word, erased, false);
                AssertFailure(matrix, QrSymbolKind.Micro, QrFailureReason.ChecksumFailure);
            }
    }

    static IEnumerable<TestCaseData> MetadataCases()
    {
        foreach (var p in new[] { (QrSymbolKind.Model2, 1, 0), (QrSymbolKind.Model2, 40, 3),
            (QrSymbolKind.Micro, 1, -1), (QrSymbolKind.Micro, 4, 2), (QrSymbolKind.rMQR, 0, 0),
            (QrSymbolKind.rMQR, 31, 1), (QrSymbolKind.LegacyModel1, 1, 0), (QrSymbolKind.LegacyModel1, 14, 3) })
            foreach (bool versionBits in p.Item1 == QrSymbolKind.Model2 && p.Item2 >= 7 ? new[] { false, true } : new[] { false })
                foreach (bool confidence in new[] { false, true })
                    yield return new(p.Item1, p.Item2, p.Item3, versionBits, confidence);
    }

    [TestCaseSource(nameof(MetadataCases))]
    public void MetadataErrorsAndErasuresRespectBchBoundaryAndIndependentCopies(
        QrSymbolKind kind, int version, int level, bool versionBits, bool confidence)
    {
        QrMatrix clean = Fixture(kind, version, level);
        int[][] copies = MetadataCells(kind, version, clean.Width, versionBits);
        for (int errors = 0; errors <= 3; errors++)
            for (int erased = 0; erased <= 6 - 2 * errors; erased++)
            {
                QrMatrix matrix = Fixture(kind, version, level);
                foreach (int[] cells in copies)
                {
                    foreach (int cell in cells.Take(errors)) Flip(matrix, cell);
                    foreach (int cell in cells.Skip(errors).Take(erased)) Erase(matrix, cell, confidence);
                }
                QrDecodeResult result = Decode(matrix, kind);
                Assert.That(result.Symbol!.Codewords, Is.EqualTo(CodewordErasureRecoveryTests.Words(kind, version, level)));
                Assert.That(result.CorrectedErrorCount, Is.Zero, "BCH repairs are not RS codeword errors.");
                Assert.That(result.CorrectedErasureCount, Is.Zero);
            }
        // Seven missing BCH bits exceed the guaranteed budget. Either complete
        // partner can recover; losing both copies must not invent metadata.
        QrMatrix missing = Fixture(kind, version, level);
        foreach (int[] cells in copies)
            foreach (int cell in cells.Take(7)) Erase(missing, cell, confidence);
        AssertFailure(missing, kind, versionBits ? QrFailureReason.InvalidVersion : QrFailureReason.InvalidFormat);
        if (copies.Length == 2)
            foreach (int surviving in new[] { 0, 1 })
            {
                QrMatrix matrix = Fixture(kind, version, level);
                foreach (int cell in copies[1 - surviving]) Erase(matrix, cell, confidence);
                foreach (int cell in copies[surviving].Take(3)) Flip(matrix, cell);
                Decode(matrix, kind);
            }
    }

    [TestCaseSource(nameof(MetadataCases))]
    public void FourMetadataErrorsOutsideEveryBchDecodingSphereAreRejected(
        QrSymbolKind kind, int version, int level, bool versionBits, bool confidence)
    {
        QrMatrix matrix = Fixture(kind, version, level);
        int[][] copies = MetadataCells(kind, version, matrix.Width, versionBits);
        for (int copy = 0; copy < copies.Length; copy++)
        {
            int received = 0;
            for (int bit = 0; bit < copies[copy].Length; bit++)
                if (matrix.Values[copies[copy][bit]] == QrModuleValue.Dark) received |= 1 << bit;
            int[] validCodes = MetadataCodebook(kind, versionBits, copy);
            // Four errors need not always fail: they can approach a different
            // valid BCH word. Choose a word provably outside every radius-3 ball.
            int damage = Enumerable.Range(1, (1 << copies[copy].Length) - 1)
                .First(mask => BitOperations.PopCount((uint)mask) == 4
                    && validCodes.All(code => BitOperations.PopCount((uint)(received ^ mask ^ code)) > 3));
            for (int bit = 0; bit < copies[copy].Length; bit++)
                if ((damage & (1 << bit)) != 0) Flip(matrix, copies[copy][bit]);
        }
        // Known nonzero confidence remains hard-decision evidence for BCH.
        if (confidence) matrix.Confidence = Enumerable.Repeat((byte)128, matrix.Values.Length).ToArray();
        AssertFailure(matrix, kind, versionBits ? QrFailureReason.InvalidVersion : QrFailureReason.InvalidFormat);
    }

    static int[] MetadataCodebook(QrSymbolKind kind, bool versionBits, int copy)
    {
        if (versionBits) return Enumerable.Range(7, 34).Select(VersionCodeTable.GetCode).ToArray();
        if (kind == QrSymbolKind.rMQR) return Enumerable.Range(0, 64).Select(i => copy == 0
            ? RectangularMicroFormatCodeTable.GetFinderSideCode(i % 32, i / 32)
            : RectangularMicroFormatCodeTable.GetSubfinderSideCode(i % 32, i / 32)).ToArray();
        if (kind == QrSymbolKind.Micro) return Enumerable.Range(0, 32).Select(i => MicroFormatCodeTable.GetCode(
            new[] { 1, 2, 2, 3, 3, 4, 4, 4 }[i / 4], new[] { -1, 0, 1, 0, 1, 0, 1, 2 }[i / 4], i % 4)).ToArray();
        return Enumerable.Range(0, 32).Select(i => kind == QrSymbolKind.LegacyModel1
            ? LegacyModel1Table.GetFormatCode(i / 8, i % 8) : FormatCodeTable.GetCode(i / 8, i % 8)).ToArray();
    }

    static IEnumerable<TestCaseData> FunctionCases()
    {
        foreach (var p in new[] { (QrSymbolKind.Model2, 2), (QrSymbolKind.Micro, 4),
            (QrSymbolKind.rMQR, 31), (QrSymbolKind.LegacyModel1, 1) })
            foreach (bool distinctive in p.Item1 == QrSymbolKind.Micro ? new[] { false } : new[] { false, true })
                foreach (string damage in new[] { "flip", "unknown", "confidence" })
                    foreach (int beyond in new[] { 0, 1 }) yield return new(p.Item1, p.Item2, distinctive, damage, beyond);
    }

    [TestCaseSource(nameof(FunctionCases))]
    public void FunctionDamageUsesRecognitionThresholdsAndNeverConsumesPayloadParity(
        QrSymbolKind kind, int version, bool distinctive, string damage, int beyond)
    {
        QrMatrix matrix = Fixture(kind, version, 0);
        QrModuleRole[] roles = ModuleClassifier.CreateEncodingRoleMap(
            kind == QrSymbolKind.LegacyModel1 ? QrSymbolKind.Model2 : kind, version, 0);
        int[] cells = Enumerable.Range(0, roles.Length).Where(i =>
        {
            bool special = kind == QrSymbolKind.LegacyModel1
                ? LegacyModel1Table.IsExtensionModule(version, i / matrix.Width, i % matrix.Width)
                : roles[i] == QrModuleRole.Alignment || roles[i] == QrModuleRole.Version;
            return distinctive ? special : !special && (roles[i] == QrModuleRole.Finder || roles[i] == QrModuleRole.Separator
                || roles[i] == QrModuleRole.Timing || roles[i] == QrModuleRole.DarkModule);
        }).ToArray();
        Assert.That(cells, Is.Not.Empty);
        int count = (damage == "flip" ? cells.Length / 5 : cells.Length / 2) + beyond;
        foreach (int cell in cells.Take(count))
            if (damage == "flip") Flip(matrix, cell);
            else Erase(matrix, cell, damage == "confidence");
        // A caller cannot relabel damaged function cells as payload to bypass evidence checks.
        matrix.Roles = Enumerable.Repeat(QrModuleRole.Data, matrix.Values.Length).ToArray();
        SymbolFamilyCandidate evidence = SymbolFamilyDiscriminator.Discriminate(matrix, Options(kind)).Candidates.Single();
        Assert.That(evidence.Rejected || !evidence.HasSufficientEvidence, Is.EqualTo(beyond != 0));
        MatrixCodewordResult codewords = MatrixCodewordReader.Read(matrix, kind, Options(kind));
        if (kind == QrSymbolKind.LegacyModel1 && beyond != 0)
        {
            // The legacy reader requires family evidence before extracting bits.
            Assert.That(codewords.FailureReason, Is.EqualTo(QrFailureReason.InvalidGeometry));
            Assert.That(codewords.Correction, Is.Null);
        }
        else
        {
            Assert.That(codewords.Success, Is.True, "Unprotected function damage must not be counted as RS payload damage.");
            Assert.That(codewords.Correction!.CorrectedErrorCount, Is.Zero);
            Assert.That(codewords.Correction.CorrectedErasureCount, Is.Zero);
        }
        if (beyond == 0)
        {
            QrDecodeResult result = Decode(matrix, kind);
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.CorrectedErasureCount, Is.Zero);
        }
        else AssertPublicRejection(matrix, kind);
    }

    [TestCase(QrSymbolKind.Model2, 7, 2)]
    [TestCase(QrSymbolKind.Micro, 4, 2)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    [TestCase(QrSymbolKind.LegacyModel1, 14, 3)]
    public void MetadataAndPayloadBudgetsRemainIndependentWhenDamagedTogether(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Fixture(kind, version, level);
        foreach (int[] copy in MetadataCells(kind, version, matrix.Width, false))
        {
            foreach (int cell in copy.Take(2)) Flip(matrix, cell);
            foreach (int cell in copy.Skip(2).Take(2)) Erase(matrix, cell, false);
        }
        if (kind == QrSymbolKind.Model2)
            foreach (int[] copy in MetadataCells(kind, version, matrix.Width, true))
                foreach (int cell in copy.Take(3)) Flip(matrix, cell);
        int ecc = CodewordErasureRecoveryTests.EccCount(kind, version, level);
        int[] positions = AlternatingEnds(CodewordErasureRecoveryTests.BlockPositions(kind, version, level)[0]);
        for (int i = 0; i < ecc - 1; i++) DamageWord(matrix, kind, version, level, positions[i], i != 0, false);
        QrDecodeResult result = Decode(matrix, kind);
        Assert.That(result.CorrectedErrorCount, Is.EqualTo(1));
        Assert.That(result.CorrectedErasureCount, Is.EqualTo(ecc - 2));
    }

    static QrDecodeResult Decode(QrMatrix matrix, QrSymbolKind kind)
    {
        IQrDecoder decoder = new QrDecoder();
        QrAttemptResult attempt = decoder.TryDecodeMatrix(matrix, Options(kind));
        Assert.That(attempt.Success, Is.True, attempt.FailureReason.ToString());
        Assert.That(attempt.Result!.Text, Is.EqualTo("123"));
        Assert.That(attempt.Result.RawBytes, Is.EqualTo(new byte[] { 49, 50, 51 }));
        Assert.That(attempt.Result.Symbol!.SymbolKind, Is.EqualTo(kind));
        return attempt.Result;
    }

    static void AssertFailure(QrMatrix matrix, QrSymbolKind kind, QrFailureReason reason)
    {
        MatrixCodewordResult canonical = MatrixCodewordReader.Read(matrix, kind, Options(kind));
        Assert.That(canonical.Success, Is.False);
        Assert.That(canonical.FailureReason, Is.EqualTo(reason));
        Assert.That(canonical.DataCodewords, Is.Empty);
        AssertPublicRejection(matrix, kind);
    }

    static void AssertPublicRejection(QrMatrix matrix, QrSymbolKind kind)
    {
        IQrDecoder decoder = new QrDecoder();
        QrAttemptResult attempt = decoder.TryDecodeMatrix(matrix, Options(kind));
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.FailureReason, Is.Not.EqualTo(QrFailureReason.None));
        Assert.That(attempt.Result, Is.Null);
        Assert.That(attempt.Text, Is.Null);
        Assert.That(attempt.RawBytes, Is.Empty);
        Assert.That(attempt.Segments, Is.Empty);
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => decoder.DecodeMatrix(matrix, Options(kind)))!;
        Assert.That(error.FailureReason, Is.EqualTo(attempt.FailureReason));
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(attempt.AttemptCount));
    }

    static QrDecodeOptions Options(QrSymbolKind kind) => new()
    {
        AllowModel2 = kind == QrSymbolKind.Model2, AllowMicro = kind == QrSymbolKind.Micro,
        AllowRectangularMicro = kind == QrSymbolKind.rMQR, AllowLegacyModel1 = kind == QrSymbolKind.LegacyModel1,
        TryMirrored = false, TryInverted = false, EnableRecovery = false
    };

    static QrMatrix Fixture(QrSymbolKind kind, int version, int level) => CodewordErasureRecoveryTests.Build(
        CodewordErasureRecoveryTests.Words(kind, version, level), kind, version, level);

    static int[] AlternatingEnds(List<int> block) => Enumerable.Range(0, block.Count)
        .Select(i => i % 2 == 0 ? block[i / 2] : block[block.Count - 1 - i / 2]).ToArray();

    static void DamageWord(QrMatrix matrix, QrSymbolKind kind, int version, int level, int word, bool erased, bool unknown)
    {
        int[] cells = CodewordErasureRecoveryTests.Cells(kind, version, level, word);
        matrix.Confidence ??= Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        foreach (int cell in unknown ? cells : cells.Take(1))
        {
            if (unknown) matrix.Values[cell] = QrModuleValue.Unknown;
            else Flip(matrix, cell);
            if (erased) matrix.Confidence[cell] = 0;
        }
    }

    static void Flip(QrMatrix matrix, int cell) => matrix.Values[cell] = matrix.Values[cell] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
    static void Erase(QrMatrix matrix, int cell, bool confidence)
    {
        if (!confidence) matrix.Values[cell] = QrModuleValue.Unknown;
        else
        {
            matrix.Confidence ??= Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
            Flip(matrix, cell);
            matrix.Confidence[cell] = 0;
        }
    }

    static int[][] MetadataCells(QrSymbolKind kind, int version, int width, bool versionBits)
    {
        int[][] result = new int[kind == QrSymbolKind.Micro ? 1 : 2][];
        for (int copy = 0; copy < result.Length; copy++)
        {
            result[copy] = new int[versionBits || kind == QrSymbolKind.rMQR ? 18 : 15];
            for (int bit = 0; bit < result[copy].Length; bit++)
            {
                int row, column;
                if (kind == QrSymbolKind.Micro)
                {
                    row = bit <= 7 ? MicroPlacementTable.GetFormatVerticalRow(bit) : MicroPlacementTable.GetFormatHorizontalRow(bit);
                    column = bit <= 7 ? MicroPlacementTable.GetFormatVerticalColumn(bit) : MicroPlacementTable.GetFormatHorizontalColumn(bit);
                }
                else if (kind == QrSymbolKind.rMQR)
                {
                    row = copy == 0 ? RectangularMicroPlacementTable.GetFinderSideFormatRow(bit) : RectangularMicroPlacementTable.GetSubfinderSideFormatRow(version, bit);
                    column = copy == 0 ? RectangularMicroPlacementTable.GetFinderSideFormatColumn(bit) : RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(version, bit);
                }
                else if (kind == QrSymbolKind.LegacyModel1)
                {
                    row = copy == 0 ? LegacyModel1Table.GetFormatPrimaryRow(bit) : LegacyModel1Table.GetFormatSecondaryRow(version, bit);
                    column = copy == 0 ? LegacyModel1Table.GetFormatPrimaryColumn(bit) : LegacyModel1Table.GetFormatSecondaryColumn(version, bit);
                }
                else if (versionBits)
                {
                    row = copy == 0 ? Model2PlacementTable.GetVersionPrimaryRow(version, bit) : Model2PlacementTable.GetVersionSecondaryRow(version, bit);
                    column = copy == 0 ? Model2PlacementTable.GetVersionPrimaryColumn(version, bit) : Model2PlacementTable.GetVersionSecondaryColumn(version, bit);
                }
                else
                {
                    row = copy == 0 ? Model2PlacementTable.GetFormatPrimaryRow(bit) : Model2PlacementTable.GetFormatSecondaryRow(version, bit);
                    column = copy == 0 ? Model2PlacementTable.GetFormatPrimaryColumn(bit) : Model2PlacementTable.GetFormatSecondaryColumn(version, bit);
                }
                result[copy][bit] = row * width + column;
            }
        }
        return result;
    }
}
