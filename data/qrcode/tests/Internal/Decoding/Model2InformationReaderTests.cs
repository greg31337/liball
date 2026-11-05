using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class Model2InformationReaderTests
{
    static IEnumerable<TestCaseData> Formats()
    {
        for (int level = 0; level < 4; level++)
            for (int mask = 0; mask < 8; mask++)
                yield return new TestCaseData(level, mask);
    }

    static IEnumerable<int> Versions() => Enumerable.Range(1, 40);

    static IEnumerable<int> ExplicitVersions() => Enumerable.Range(7, 34);

    [TestCaseSource(nameof(Formats))]
    public void ReadsEveryEccAndMaskAtSmallAndLargeDimensions(int level, int mask)
    {
        foreach (int version in new[] { 1, 7, 40 })
        {
            QrMatrix matrix = Fixture(version, level, mask);
            Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
            Assert.That(result.Success, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(result.Version, Is.EqualTo(version));
            Assert.That(result.DimensionVersion, Is.EqualTo(version));
            Assert.That(result.ErrorCorrectionLevel, Is.EqualTo((QrErrorCorrectionLevel)level));
            Assert.That(result.MaskPattern, Is.EqualTo((QrMaskPattern)mask));
            Assert.That(result.SelectedFormatCopy, Is.Zero);
            Assert.That(result.FormatCopiesAgree, Is.True);
            foreach (MatrixInformationCopy copy in result.FormatCopies)
            {
                Assert.That(copy.ReceivedCode, Is.EqualTo(FormatCode(level, mask)));
                Assert.That(copy.CorrectedCode, Is.EqualTo(copy.ReceivedCode));
                Assert.That(copy.CorrectedErrorCount, Is.Zero);
                Assert.That(copy.KnownBitMask, Is.EqualTo(0x7fff));
            }
        }
    }

    [TestCaseSource(nameof(Versions))]
    public void ReadsEveryVersionAndInfersOnlyVersionsWithoutVersionFields(int version)
    {
        Model2InformationResult result = Model2InformationReader.Read(Fixture(version, 1, 3), new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.Version, Is.EqualTo(version));
        if (version < 7)
        {
            Assert.That(result.VersionCopies, Is.Empty);
            Assert.That(result.SelectedVersionCopy, Is.EqualTo(-1));
            Assert.That(result.VersionCopiesAgree, Is.False);
        }
        else
        {
            Assert.That(result.VersionCopiesAgree, Is.True);
            Assert.That(result.SelectedVersionCopy, Is.Zero);
            Assert.That(result.VersionCopies.All(c => c.Data == version && c.CorrectedErrorCount == 0), Is.True);
            Assert.That(result.VersionCopies[0].ReceivedCode, Is.EqualTo(Bch(version, 12, 0x1f25, 0)));
        }
    }

    [TestCaseSource(nameof(Formats))]
    public void CorrectsUpToThreeFormatErrorsIncludingEveryBitPosition(int level, int mask)
    {
        for (int start = 0; start < 15; start++)
            for (int errors = 1; errors <= 3; errors++)
            {
                QrMatrix matrix = Fixture(1, level, mask);
                Damage(matrix, false, false, start, errors, QrModuleValue.Unknown, false);
                Damage(matrix, false, true, start + 5, errors, QrModuleValue.Unknown, false);
                Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions { EnableRecovery = false });
                Assert.That(result.Success, Is.True);
                Assert.That(result.FormatCopiesAgree, Is.True);
                Assert.That(result.FormatCopies[0].CorrectedErrorCount, Is.EqualTo(errors));
                Assert.That(result.FormatCopies[1].CorrectedErrorCount, Is.EqualTo(errors));
                Assert.That(result.ErrorCorrectionLevel, Is.EqualTo((QrErrorCorrectionLevel)level));
                Assert.That(result.MaskPattern, Is.EqualTo((QrMaskPattern)mask));
            }
    }

    [TestCaseSource(nameof(ExplicitVersions))]
    public void CorrectsVersionCopiesWithoutUsingDimensionsAsAReplacement(int version)
    {
        for (int errors = 1; errors <= 3; errors++)
        {
            QrMatrix matrix = Fixture(version, 0, 0);
            Damage(matrix, true, false, version % 18, errors, QrModuleValue.Unknown, false);
            Damage(matrix, true, true, (version + 7) % 18, errors, QrModuleValue.Unknown, false);
            Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
            Assert.That(result.Success, Is.True);
            Assert.That(result.Version, Is.EqualTo(version));
            Assert.That(result.VersionCopiesAgree, Is.True);
            Assert.That(result.VersionCopies.All(c => c.CorrectedErrorCount == errors), Is.True);
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void EitherCopyCanRecoverAnUnreadablePartner(bool versionInformation, bool secondary)
    {
        QrMatrix matrix = Fixture(7, 2, 6);
        SetCode(matrix, versionInformation, secondary, 0);
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(versionInformation ? result.SelectedVersionCopy : result.SelectedFormatCopy, Is.EqualTo(secondary ? 0 : 1));
        Assert.That(versionInformation ? result.VersionCopiesAgree : result.FormatCopiesAgree, Is.False);
        MatrixInformationCopy[] copies = versionInformation ? result.VersionCopies : result.FormatCopies;
        Assert.That(copies[secondary ? 1 : 0].Success, Is.False);
        Assert.That(copies[secondary ? 1 : 0].Data, Is.EqualTo(-1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EqualStrengthConflictingCopiesFailAndRetainBothHypotheses(bool versionInformation)
    {
        QrMatrix matrix = Fixture(7, 0, 0);
        SetCode(matrix, versionInformation, true, versionInformation ? Bch(8, 12, 0x1f25, 0) : FormatCode(3, 7));
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(versionInformation ? QrFailureReason.InvalidVersion : QrFailureReason.InvalidFormat));
        MatrixInformationCopy[] copies = versionInformation ? result.VersionCopies : result.FormatCopies;
        Assert.That(copies.All(c => c.Success), Is.True);
        Assert.That(copies[0].Data, Is.Not.EqualTo(copies[1].Data));
        Assert.That(versionInformation ? result.SelectedVersionCopy : result.SelectedFormatCopy, Is.EqualTo(-1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StrongerFormatCopyWinsWithoutHidingTheConflictingAlternative(bool secondaryStronger)
    {
        QrMatrix matrix = Fixture(1, 0, 0);
        SetCode(matrix, false, true, FormatCode(3, 7));
        Damage(matrix, false, !secondaryStronger, 4, 1, QrModuleValue.Unknown, false);
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.SelectedFormatCopy, Is.EqualTo(secondaryStronger ? 1 : 0));
        Assert.That(result.FormatCopiesAgree, Is.False);
        Assert.That(result.MaskPattern, Is.EqualTo(secondaryStronger ? QrMaskPattern.Seven : QrMaskPattern.Zero));
    }

    [Test]
    public void VersionDimensionMismatchFailsEvenWhenBothBchCopiesAgree()
    {
        QrMatrix matrix = Fixture(7, 0, 0);
        SetCode(matrix, true, false, Bch(8, 12, 0x1f25, 0));
        SetCode(matrix, true, true, Bch(8, 12, 0x1f25, 0));
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
        Assert.That(result.DimensionVersion, Is.EqualTo(7));
        Assert.That(result.Version, Is.Zero);
        Assert.That(result.VersionCopiesAgree, Is.True);
        Assert.That(result.VersionCopies[0].Data, Is.EqualTo(8));
    }

    [Test]
    public void DoesNotSilentlyOverrideStrongerWrongVersionWithDimensionMatchingCopy()
    {
        QrMatrix matrix = Fixture(7, 0, 0);
        SetCode(matrix, true, true, Bch(8, 12, 0x1f25, 0));
        Damage(matrix, true, false, 0, 1, QrModuleValue.Unknown, false);
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
        Assert.That(result.SelectedVersionCopy, Is.EqualTo(1));
        Assert.That(result.VersionCopies[0].Data, Is.EqualTo(7));
        Assert.That(result.Version, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UncorrectableCopiesFailWithoutInventingInformation(bool versionInformation)
    {
        QrMatrix matrix = Fixture(7, 0, 0);
        SetCode(matrix, versionInformation, false, 0);
        SetCode(matrix, versionInformation, true, 0);
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(versionInformation ? QrFailureReason.InvalidVersion : QrFailureReason.InvalidFormat));
        if (!versionInformation)
        {
            Assert.That(result.MaskPattern, Is.EqualTo(QrMaskPattern.Automatic));
            Assert.That(result.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.None));
        }
        else Assert.That(result.Version, Is.Zero);
    }

    [TestCase(false, 0, 6)]
    [TestCase(false, 1, 4)]
    [TestCase(false, 2, 2)]
    [TestCase(true, 0, 6)]
    [TestCase(true, 1, 4)]
    [TestCase(true, 2, 2)]
    public void CorrectsErrorsAndErasuresWithinTheBchBudget(bool versionInformation, int errors, int erasures)
    {
        QrMatrix matrix = Fixture(7, 2, 5);
        foreach (bool secondary in new[] { false, true })
        {
            Damage(matrix, versionInformation, secondary, 0, erasures, QrModuleValue.Unknown, true);
            Damage(matrix, versionInformation, secondary, 8, errors, QrModuleValue.Unknown, false);
        }
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        MatrixInformationCopy[] copies = versionInformation ? result.VersionCopies : result.FormatCopies;
        Assert.That(copies.All(c => c.MissingBitCount == erasures && c.CorrectedErrorCount == errors), Is.True);
        Assert.That(result.Version, Is.EqualTo(7));
        Assert.That(result.MaskPattern, Is.EqualTo(QrMaskPattern.Five));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TooManyErasuresAndZeroConfidenceAreNotAssumedLight(bool versionInformation)
    {
        QrMatrix matrix = Fixture(7, 0, 0);
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        for (int bit = 0; bit < 7; bit++)
        {
            matrix.Values[Offset(matrix, versionInformation, false, bit)] = QrModuleValue.Unknown;
            matrix.Confidence[Offset(matrix, versionInformation, true, bit)] = 0;
        }
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        MatrixInformationCopy[] copies = versionInformation ? result.VersionCopies : result.FormatCopies;
        Assert.That(copies.All(c => !c.Success && c.MissingBitCount == 7), Is.True);
    }

    [Test]
    public void RanksCopiesByCombinedErrorAndErasureCost()
    {
        QrMatrix matrix = Fixture(1, 0, 0);
        SetCode(matrix, false, true, FormatCode(3, 7));
        Damage(matrix, false, false, 0, 1, QrModuleValue.Unknown, false);
        Damage(matrix, false, true, 0, 1, QrModuleValue.Unknown, true);
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.SelectedFormatCopy, Is.EqualTo(1));
        Assert.That(result.MaskPattern, Is.EqualTo(QrMaskPattern.Seven));
        // One error and two erasures have equal cost, so this conflict is unresolved.
        Damage(matrix, false, true, 1, 1, QrModuleValue.Unknown, true);
        result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidFormat));
        Assert.That(result.SelectedFormatCopy, Is.EqualTo(-1));
    }

    [Test]
    public void ReadsDeclaredQuietZoneOffsetsWithoutMutatingOrUsingUnrelatedCells()
    {
        QrMatrix matrix = new QrMatrix
        {
            Width = 51, Height = 53, QuietZoneLeft = 2, QuietZoneRight = 4, QuietZoneTop = 5, QuietZoneBottom = 3,
            Values = new QrModuleValue[51 * 53], Confidence = Enumerable.Repeat((byte)255, 51 * 53).ToArray(),
            Roles = Enumerable.Repeat(QrModuleRole.Data, 51 * 53).ToArray()
        };
        WriteInformation(matrix, 7, 3, 2);
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        Model2InformationResult result = Model2InformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.Version, Is.EqualTo(7));
        Assert.That(result.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
        Assert.That(result.MaskPattern, Is.EqualTo(QrMaskPattern.Two));
        Assert.That(matrix.Values, Is.EqualTo(before));
        Assert.That(matrix.Confidence, Is.All.EqualTo(255));
        Assert.That(matrix.Roles, Is.All.EqualTo(QrModuleRole.Data));
    }

    [TestCase(17, 17)]
    [TestCase(22, 22)]
    [TestCase(45, 41)]
    public void NonModel2DimensionsReturnVersionFailure(int width, int height)
    {
        Model2InformationResult result = Model2InformationReader.Read(new QrMatrix
        {
            Width = width, Height = height, Values = new QrModuleValue[width * height]
        }, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
        Assert.That(result.FormatCopies, Is.Empty);
    }

    [Test]
    public void ReadsActualEncoderOutput()
    {
        QrSymbol symbol = new QrEncoder().EncodeText("MODEL 2 FORMAT TEST", new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = 7, MaximumVersion = 7,
            ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = QrErrorCorrectionLevel.Quartile, MaskPattern = QrMaskPattern.Six
        });
        Model2InformationResult result = Model2InformationReader.Read(symbol.Matrix!, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.Version, Is.EqualTo(symbol.Version));
        Assert.That(result.ErrorCorrectionLevel, Is.EqualTo(symbol.ErrorCorrectionLevel));
        Assert.That(result.MaskPattern, Is.EqualTo(symbol.MaskPattern));
    }

    [Test]
    public void ValidatesInputBuffersQuietZonesAndResourceLimits()
    {
        Assert.Throws<QrInputException>(() => Model2InformationReader.Read(null!, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => Model2InformationReader.Read(Fixture(1, 0, 0), null!));
        Action<QrMatrix>[] invalid =
        [
            m => m.Width = 0, m => m.Height = -1, m => m.Values = null!, m => m.Values = [],
            m => m.Confidence = [], m => m.Roles = [], m => m.Values[0] = (QrModuleValue)99,
            m => m.QuietZoneLeft = -1, m => m.QuietZoneRight = m.Width,
            m => { m.QuietZoneTop = int.MaxValue; m.QuietZoneBottom = int.MaxValue; }
        ];
        foreach (Action<QrMatrix> corrupt in invalid)
        {
            QrMatrix matrix = Fixture(1, 0, 0);
            corrupt(matrix);
            Assert.Throws<QrInputException>(() => Model2InformationReader.Read(matrix, new QrDecodeOptions()));
        }
        QrMatrix valid = Fixture(1, 0, 0);
        Assert.Throws<QrInputException>(() => Model2InformationReader.Read(valid, new QrDecodeOptions { MaximumMatrixDimension = 0 }));
        Assert.Throws<QrInputException>(() => Model2InformationReader.Read(valid, new QrDecodeOptions { MaximumAllocationBytes = 0 }));
        Assert.Throws<QrResourceLimitException>(() => Model2InformationReader.Read(valid, new QrDecodeOptions { MaximumMatrixDimension = 20 }));
        Assert.Throws<QrResourceLimitException>(() => Model2InformationReader.Read(valid, new QrDecodeOptions { MaximumAllocationBytes = 100 }));
    }

    static QrMatrix Fixture(int version, int level, int mask)
    {
        int size = 17 + 4 * version;
        QrMatrix matrix = new QrMatrix { Width = size, Height = size, Values = new QrModuleValue[size * size] };
        WriteInformation(matrix, version, level, mask);
        return matrix;
    }

    static void WriteInformation(QrMatrix matrix, int version, int level, int mask)
    {
        foreach (bool secondary in new[] { false, true })
        {
            SetCode(matrix, false, secondary, FormatCode(level, mask));
            if (version >= 7) SetCode(matrix, true, secondary, Bch(version, 12, 0x1f25, 0));
        }
    }

    // Fixture coordinates and polynomial division are independent of production tables/readers.
    static int Offset(QrMatrix matrix, bool versionInformation, bool secondary, int bit)
    {
        int size = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int row;
        int column;
        if (versionInformation)
        {
            row = bit / 3;
            column = size - 11 + bit % 3;
            if (secondary) (row, column) = (column, row);
        }
        else if (secondary)
        {
            row = bit < 8 ? 8 : size - 15 + bit;
            column = bit < 8 ? size - 1 - bit : 8;
        }
        else
        {
            int[] rows = [0, 1, 2, 3, 4, 5, 7, 8, 8, 8, 8, 8, 8, 8, 8];
            int[] columns = [8, 8, 8, 8, 8, 8, 8, 8, 7, 5, 4, 3, 2, 1, 0];
            row = rows[bit]; column = columns[bit];
        }
        return (row + matrix.QuietZoneTop) * matrix.Width + column + matrix.QuietZoneLeft;
    }

    static void SetCode(QrMatrix matrix, bool versionInformation, bool secondary, int code)
    {
        for (int bit = 0; bit < (versionInformation ? 18 : 15); bit++)
            matrix.Values[Offset(matrix, versionInformation, secondary, bit)] = (code & (1 << bit)) != 0 ? QrModuleValue.Dark : QrModuleValue.Light;
    }

    static void Damage(QrMatrix matrix, bool versionInformation, bool secondary, int start, int count, QrModuleValue replacement, bool replace)
    {
        for (int bit = 0; bit < count; bit++)
        {
            int offset = Offset(matrix, versionInformation, secondary, (start + bit) % (versionInformation ? 18 : 15));
            matrix.Values[offset] = replace ? replacement : matrix.Values[offset] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
        }
    }

    static int FormatCode(int level, int mask) => Bch((new[] { 1, 0, 3, 2 }[level] << 3) | mask, 10, 0x537, 0x5412);

    static int Bch(int data, int parity, int generator, int xorMask)
    {
        int shifted = data << parity;
        int remainder = shifted;
        for (int bit = 30; bit >= parity; bit--)
            if ((remainder & (1 << bit)) != 0) remainder ^= generator << (bit - parity);
        return (shifted | remainder) ^ xorMask;
    }
}
