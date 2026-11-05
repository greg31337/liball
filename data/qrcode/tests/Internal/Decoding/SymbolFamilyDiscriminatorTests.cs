using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class SymbolFamilyDiscriminatorTests
{
    static IEnumerable<TestCaseData> ModernSizes()
    {
        for (int version = 1; version <= 40; version++) yield return new TestCaseData(QrSymbolKind.Model2, version);
        for (int version = 1; version <= 4; version++) yield return new TestCaseData(QrSymbolKind.Micro, version);
        for (int version = 0; version < 32; version++) yield return new TestCaseData(QrSymbolKind.rMQR, version);
    }

    static IEnumerable<TestCaseData> LegacySizes()
    {
        for (int version = 1; version <= 14; version++)
        {
            yield return new TestCaseData(version, false);
            yield return new TestCaseData(version, true);
        }
    }

    [TestCaseSource(nameof(ModernSizes))]
    public void RecognizesFixedPatternsAtEveryModernSize(QrSymbolKind kind, int version)
    {
        QrMatrix matrix = ModernPatterns(kind, version);
        SymbolFamilyResult result = SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions());
        SymbolFamilyCandidate candidate = result.Candidates.Single(c => c.SymbolKind == kind);
        Assert.That(candidate.Rejected, Is.False);
        Assert.That(candidate.HasSufficientEvidence, Is.True);
        Assert.That(candidate.Version, Is.EqualTo(version));
        Assert.That(candidate.Width, Is.EqualTo(matrix.Width));
        Assert.That(candidate.Height, Is.EqualTo(matrix.Height));
        Assert.That(candidate.MismatchedModuleCount, Is.Zero);
        Assert.That(candidate.ObservedModuleCount, Is.EqualTo(candidate.FixedModuleCount));
        Assert.That(result.Status, Is.EqualTo(SymbolFamilyStatus.Identified));
    }

    [TestCaseSource(nameof(LegacySizes))]
    public void RecognizesBothLegacyExtensionAppearances(int version, bool early)
    {
        QrMatrix matrix = LegacyPatterns(version, early);
        SymbolFamilyResult result = SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions());
        SymbolFamilyCandidate legacy = result.Candidates.Single(c => c.SymbolKind == QrSymbolKind.LegacyModel1);
        Assert.That(legacy.Rejected, Is.False);
        Assert.That(legacy.HasSufficientEvidence, Is.True);
        Assert.That(legacy.Version, Is.EqualTo(version));
        Assert.That(legacy.MismatchedModuleCount, Is.Zero);
        Assert.That(legacy.DistinctiveModuleCount, Is.EqualTo(4 + version / 2 * 16));
        // Version 1 has no Model 2 alignment/version cells. Legacy extensions
        // occupy unconstrained Model 2 payload positions, so both remain viable.
        Assert.That(result.Status, Is.EqualTo(version == 1 ? SymbolFamilyStatus.Ambiguous : SymbolFamilyStatus.Identified));
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    public void AcceptsEncodedSymbolsAcrossMasksAndErrorCorrectionLevels(QrSymbolKind kind)
    {
        foreach (QrErrorCorrectionLevel level in new[] { QrErrorCorrectionLevel.Low, QrErrorCorrectionLevel.Medium, QrErrorCorrectionLevel.Quartile, QrErrorCorrectionLevel.High })
        {
            if (kind == QrSymbolKind.rMQR && level != QrErrorCorrectionLevel.Medium && level != QrErrorCorrectionLevel.High) continue;
            if (kind == QrSymbolKind.Micro && level == QrErrorCorrectionLevel.High) continue;
            int masks = kind == QrSymbolKind.Model2 ? 8 : kind == QrSymbolKind.Micro ? 4 : 1;
            for (int mask = 0; mask < masks; mask++)
            {
                QrEncodeOptions options = new QrEncodeOptions
                {
                    ForceSymbolKind = true, SymbolKind = kind, ErrorCorrectionLevel = level,
                    MaskPattern = kind == QrSymbolKind.rMQR ? QrMaskPattern.Four : (QrMaskPattern)mask,
                    QuietZoneModules = 0, BoostErrorCorrectionLevel = false, ForceErrorCorrectionLevel = true
                };
                if (kind != QrSymbolKind.rMQR) options.MinimumVersion = options.MaximumVersion = kind == QrSymbolKind.Micro ? 4 : 7;
                QrMatrix matrix = new QrEncoder().EncodeText("12345", options).Matrix!;
                SymbolFamilyCandidate candidate = SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions())
                    .Candidates.Single(c => c.SymbolKind == kind);
                Assert.That(candidate.Rejected, Is.False);
                Assert.That(candidate.HasSufficientEvidence, Is.True);
                Assert.That(candidate.MismatchedModuleCount, Is.Zero);
            }
        }
    }

    [Test]
    public void LeavesContradictoryFamilyHypothesesExplicitlyAmbiguous()
    {
        QrMatrix matrix = ModernPatterns(QrSymbolKind.Model2, 2);
        QrMatrix legacy = LegacyPatterns(2, false);
        for (int row = 0; row < matrix.Height; row++)
            for (int column = 0; column < matrix.Width; column++)
                if (LegacyModel1Table.IsExtensionModule(2, row, column))
                    matrix.Values[row * matrix.Width + column] = legacy.Values[row * matrix.Width + column];
        SymbolFamilyResult first = SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions());
        SymbolFamilyResult second = SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions());
        Assert.That(first.Status, Is.EqualTo(SymbolFamilyStatus.Ambiguous));
        Assert.That(first.Candidates.Select(c => c.SymbolKind), Is.EqualTo(new[] { QrSymbolKind.Model2, QrSymbolKind.LegacyModel1 }));
        Assert.That(first.Candidates.All(c => c.HasSufficientEvidence && !c.Rejected), Is.True);
        Assert.That(second.Candidates.Select(c => c.MismatchedModuleCount), Is.EqualTo(first.Candidates.Select(c => c.MismatchedModuleCount)));
    }

    [TestCase(QrSymbolKind.Model2, 2)]
    [TestCase(QrSymbolKind.Micro, 4)]
    [TestCase(QrSymbolKind.rMQR, 10)]
    public void RejectsBlankImagesDespiteLegalDimensions(QrSymbolKind kind, int version)
    {
        QrMatrix matrix = ModernPatterns(kind, version);
        foreach (QrModuleValue value in new[] { QrModuleValue.Light, QrModuleValue.Dark })
        {
            Array.Fill(matrix.Values, value);
            Assert.That(SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions()).Status,
                Is.EqualTo(SymbolFamilyStatus.NoMatch));
        }
    }

    [TestCase(QrSymbolKind.Model2, 1)]
    [TestCase(QrSymbolKind.Micro, 1)]
    [TestCase(QrSymbolKind.rMQR, 0)]
    public void UnknownOrZeroConfidenceEvidenceCannotIdentifyAFamily(QrSymbolKind kind, int version)
    {
        QrMatrix matrix = ModernPatterns(kind, version);
        matrix.Confidence = new byte[matrix.Values.Length];
        SymbolFamilyResult result = SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions());
        Assert.That(result.Status, Is.EqualTo(SymbolFamilyStatus.InsufficientEvidence));
        Assert.That(result.Candidates.All(c => c.ObservedModuleCount == 0 && !c.Rejected), Is.True);
        matrix.Confidence = null;
        Array.Fill(matrix.Values, QrModuleValue.Unknown);
        Assert.That(SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions()).Status,
            Is.EqualTo(SymbolFamilyStatus.InsufficientEvidence));
    }

    [Test]
    public void MissingLegacyExtensionsDoNotSilentlyChooseModel2()
    {
        QrMatrix matrix = ModernPatterns(QrSymbolKind.Model2, 2);
        for (int row = 0; row < matrix.Height; row++)
            for (int column = 0; column < matrix.Width; column++)
                if (LegacyModel1Table.IsExtensionModule(2, row, column)) matrix.Values[row * matrix.Width + column] = QrModuleValue.Unknown;
        SymbolFamilyResult result = SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions());
        Assert.That(result.Status, Is.EqualTo(SymbolFamilyStatus.Ambiguous));
        Assert.That(result.Candidates[0].HasSufficientEvidence, Is.True);
        Assert.That(result.Candidates[1].HasSufficientEvidence, Is.False);
        Assert.That(result.Candidates[1].Rejected, Is.False);
    }

    [Test]
    public void ToleratesLimitedDamageButRejectsContradictoryDistinctivePatterns()
    {
        QrMatrix matrix = ModernPatterns(QrSymbolKind.Model2, 2);
        matrix.Values[0] = QrModuleValue.Light;
        matrix.Values[18 * 25 + 18] = QrModuleValue.Light;
        QrDecodeOptions options = new QrDecodeOptions { AllowLegacyModel1 = false };
        SymbolFamilyCandidate candidate = SymbolFamilyDiscriminator.Discriminate(matrix, options).Candidates[0];
        Assert.That(candidate.MismatchedModuleCount, Is.EqualTo(2));
        Assert.That(candidate.MismatchedDistinctiveModuleCount, Is.EqualTo(1));
        Assert.That(candidate.Rejected, Is.False);
        for (int row = 16; row <= 20; row++)
            for (int column = 16; column <= 20; column++) matrix.Values[row * 25 + column] = QrModuleValue.Light;
        SymbolFamilyResult result = SymbolFamilyDiscriminator.Discriminate(matrix, options);
        Assert.That(result.Status, Is.EqualTo(SymbolFamilyStatus.NoMatch));
        Assert.That(result.Candidates[0].Rejected, Is.True);
    }

    [Test]
    public void HonorsFamilyFiltersWithoutPromotingInsufficientEvidence()
    {
        QrMatrix matrix = LegacyPatterns(1, false);
        QrDecodeOptions options = new QrDecodeOptions { AllowModel2 = false };
        SymbolFamilyResult result = SymbolFamilyDiscriminator.Discriminate(matrix, options);
        Assert.That(result.Status, Is.EqualTo(SymbolFamilyStatus.Identified));
        Assert.That(result.Candidates.Single().SymbolKind, Is.EqualTo(QrSymbolKind.LegacyModel1));
        Array.Fill(matrix.Values, QrModuleValue.Unknown);
        Assert.That(SymbolFamilyDiscriminator.Discriminate(matrix, options).Status, Is.EqualTo(SymbolFamilyStatus.InsufficientEvidence));
        options.AllowLegacyModel1 = false;
        Assert.That(SymbolFamilyDiscriminator.Discriminate(matrix, options).Candidates, Is.Empty);
        options.AllowMicro = false;
        Assert.That(SymbolFamilyDiscriminator.Discriminate(ModernPatterns(QrSymbolKind.Micro, 2), options).Status, Is.EqualTo(SymbolFamilyStatus.NoMatch));
        options.AllowRectangularMicro = false;
        Assert.That(SymbolFamilyDiscriminator.Discriminate(ModernPatterns(QrSymbolKind.rMQR, 0), options).Status, Is.EqualTo(SymbolFamilyStatus.NoMatch));
    }

    [Test]
    public void UsesDeclaredAsymmetricQuietZoneAndDoesNotTrustRolesOrMutateBuffers()
    {
        QrMatrix source = ModernPatterns(QrSymbolKind.Micro, 3);
        QrMatrix matrix = new QrMatrix
        {
            Width = 21, Height = 21, QuietZoneLeft = 2, QuietZoneRight = 4, QuietZoneTop = 5, QuietZoneBottom = 1,
            Values = new QrModuleValue[441], Confidence = new byte[441], Roles = new QrModuleRole[441]
        };
        Array.Fill(matrix.Values, QrModuleValue.Dark); // Quiet-zone quality is a separate concern.
        Array.Fill(matrix.Roles, QrModuleRole.Data);
        Array.Fill(matrix.Confidence, (byte)255);
        for (int row = 0; row < 15; row++)
            Array.Copy(source.Values, row * 15, matrix.Values, (row + 5) * 21 + 2, 15);
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        SymbolFamilyResult result = SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions());
        Assert.That(result.Status, Is.EqualTo(SymbolFamilyStatus.Identified));
        Assert.That(result.Candidates.Single().SymbolKind, Is.EqualTo(QrSymbolKind.Micro));
        Assert.That(matrix.Values, Is.EqualTo(before));
        Assert.That(matrix.Confidence, Is.All.EqualTo(255));
        Assert.That(matrix.Roles, Is.All.EqualTo(QrModuleRole.Data));
    }

    [TestCase(10, 10)]
    [TestCase(19, 19)]
    [TestCase(22, 22)]
    [TestCase(25, 21)]
    [TestCase(43, 8)]
    [TestCase(7, 43)]
    public void UnsupportedDimensionsReturnNoMatch(int width, int height)
    {
        SymbolFamilyResult result = SymbolFamilyDiscriminator.Discriminate(new QrMatrix
        {
            Width = width, Height = height, Values = new QrModuleValue[width * height]
        }, new QrDecodeOptions());
        Assert.That(result.Status, Is.EqualTo(SymbolFamilyStatus.NoMatch));
        Assert.That(result.Candidates, Is.Empty);
    }

    [Test]
    public void IgnoresFormatAndPayloadValuesWhenAssessingFixedPatterns()
    {
        QrMatrix matrix = ModernPatterns(QrSymbolKind.Model2, 7);
        QrModuleRole[] roles = ModuleClassifier.CreateModel2RoleMap(7, 0);
        for (int index = 0; index < roles.Length; index++)
            if (roles[index] == QrModuleRole.Format || roles[index] == QrModuleRole.Data
                || roles[index] == QrModuleRole.ErrorCorrection || roles[index] == QrModuleRole.Remainder)
                matrix.Values[index] = QrModuleValue.Unknown;
        SymbolFamilyResult result = SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions { AllowLegacyModel1 = false });
        Assert.That(result.Status, Is.EqualTo(SymbolFamilyStatus.Identified));
        Assert.That(result.Candidates.Single().MismatchedModuleCount, Is.Zero);
    }

    [Test]
    public void RejectsMalformedStorageAndInvalidConfiguration()
    {
        Assert.Throws<QrInputException>(() => SymbolFamilyDiscriminator.Discriminate(null!, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => SymbolFamilyDiscriminator.Discriminate(ModernPatterns(QrSymbolKind.Micro, 1), null!));
        Action<QrMatrix>[] corruptions =
        [
            m => m.Width = 0, m => m.Height = -1, m => m.Values = null!, m => m.Values = [],
            m => m.Confidence = [], m => m.Roles = [], m => m.Values[0] = (QrModuleValue)99,
            m => m.QuietZoneLeft = -1, m => m.QuietZoneRight = int.MaxValue,
            m => { m.QuietZoneTop = int.MaxValue; m.QuietZoneBottom = int.MaxValue; }
        ];
        foreach (Action<QrMatrix> corrupt in corruptions)
        {
            QrMatrix matrix = ModernPatterns(QrSymbolKind.Micro, 1);
            corrupt(matrix);
            Assert.Throws<QrInputException>(() => SymbolFamilyDiscriminator.Discriminate(matrix, new QrDecodeOptions()));
        }
        QrMatrix valid = ModernPatterns(QrSymbolKind.Micro, 1);
        Assert.Throws<QrInputException>(() => SymbolFamilyDiscriminator.Discriminate(valid, new QrDecodeOptions { MaximumMatrixDimension = 0 }));
        Assert.Throws<QrInputException>(() => SymbolFamilyDiscriminator.Discriminate(valid, new QrDecodeOptions { MaximumAllocationBytes = 0 }));
        Assert.Throws<QrResourceLimitException>(() => SymbolFamilyDiscriminator.Discriminate(valid, new QrDecodeOptions { MaximumMatrixDimension = 10 }));
        Assert.Throws<QrResourceLimitException>(() => SymbolFamilyDiscriminator.Discriminate(valid, new QrDecodeOptions { MaximumAllocationBytes = 100 }));
    }

    static QrMatrix ModernPatterns(QrSymbolKind kind, int version)
    {
        BitMatrixBuffer bits = kind == QrSymbolKind.Model2 ? FunctionPatternBuilder.CreateModel2FunctionPatterns(version, 0, 0)
            : kind == QrSymbolKind.Micro ? FunctionPatternBuilder.CreateMicroFunctionPatterns(version, version == 1 ? -1 : 0, 0)
            : FunctionPatternBuilder.CreateRectangularMicroFunctionPatterns(version, 0);
        QrMatrix result = new QrMatrix { Width = bits.GetWidth(), Height = bits.GetHeight(), Values = new QrModuleValue[bits.GetWidth() * bits.GetHeight()] };
        for (int row = 0; row < result.Height; row++)
            for (int column = 0; column < result.Width; column++)
                result.Values[row * result.Width + column] = bits.GetModule(row, column) ? QrModuleValue.Dark : QrModuleValue.Light;
        return result;
    }

    // Independent legacy fixed-pattern fixture: finder/separator squares, timing,
    // fixed dark module, and the standard or early extension blocks. Payload is light.
    static QrMatrix LegacyPatterns(int version, bool early)
    {
        int size = 17 + version * 4;
        QrMatrix matrix = new QrMatrix { Width = size, Height = size, Values = new QrModuleValue[size * size] };
        Array.Fill(matrix.Values, QrModuleValue.Light);
        foreach ((int x, int y) in new[] { (0, 0), (size - 7, 0), (0, size - 7) })
            for (int dy = 0; dy < 7; dy++)
                for (int dx = 0; dx < 7; dx++)
                    if (dx == 0 || dx == 6 || dy == 0 || dy == 6 || dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4)
                        matrix.Values[(y + dy) * size + x + dx] = QrModuleValue.Dark;
        for (int index = 8; index < size - 8; index += 2)
        {
            matrix.Values[6 * size + index] = QrModuleValue.Dark;
            matrix.Values[index * size + 6] = QrModuleValue.Dark;
        }
        matrix.Values[(size - 8) * size + 8] = QrModuleValue.Dark;
        matrix.Values[size * size - 1] = QrModuleValue.Dark;
        for (int block = 0; block < version / 2; block++)
        {
            int start = (version % 2 == 0 ? 13 : 17) + block * 8;
            for (int offset = 0; offset < 4; offset++)
            {
                matrix.Values[(size - 1) * size + start + offset] = QrModuleValue.Dark;
                matrix.Values[(start + offset) * size + size - 1] = QrModuleValue.Dark;
                if (early && (offset == 0 || offset == 3))
                {
                    matrix.Values[(size - 2) * size + start + offset] = QrModuleValue.Dark;
                    matrix.Values[(start + offset) * size + size - 2] = QrModuleValue.Dark;
                }
            }
        }
        return matrix;
    }
}
