using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MicroInformationReaderTests
{
    static readonly int[] MicroVersions = [1, 2, 2, 3, 3, 4, 4, 4];
    static readonly QrErrorCorrectionLevel[] MicroLevels =
    [
        QrErrorCorrectionLevel.None, QrErrorCorrectionLevel.Low, QrErrorCorrectionLevel.Medium,
        QrErrorCorrectionLevel.Low, QrErrorCorrectionLevel.Medium, QrErrorCorrectionLevel.Low,
        QrErrorCorrectionLevel.Medium, QrErrorCorrectionLevel.Quartile
    ];
    static readonly int[] Widths = [43, 59, 77, 99, 139, 43, 59, 77, 99, 139, 27, 43, 59, 77, 99, 139, 27, 43, 59, 77, 99, 139, 43, 59, 77, 99, 139, 43, 59, 77, 99, 139];
    static readonly int[] Heights = [7, 7, 7, 7, 7, 9, 9, 9, 9, 9, 11, 11, 11, 11, 11, 11, 13, 13, 13, 13, 13, 13, 15, 15, 15, 15, 15, 17, 17, 17, 17, 17];
    static IEnumerable<int> MicroData() => Enumerable.Range(0, 32);
    static IEnumerable<int> RectangularData() => Enumerable.Range(0, 64);

    [TestCaseSource(nameof(MicroData))]
    public void ReadsEveryMicroVersionEccAndMaskWithUpToThreeErrors(int data)
    {
        int version = MicroVersions[data / 4];
        for (int errors = 0; errors <= 3; errors++)
            for (int start = 0; start < 15; start++)
            {
                QrMatrix matrix = MicroFixture(data);
                Damage(matrix, false, false, start, errors, false);
                MicroInformationResult result = MicroInformationReader.Read(matrix, new QrDecodeOptions { EnableRecovery = false });
                Assert.That(result.Success, Is.True);
                Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
                Assert.That(result.DimensionVersion, Is.EqualTo(version));
                Assert.That(result.Version, Is.EqualTo(version));
                Assert.That(result.ErrorCorrectionLevel, Is.EqualTo(MicroLevels[data / 4]));
                Assert.That(result.MaskPattern, Is.EqualTo((QrMaskPattern)(data % 4)));
                Assert.That(result.FormatCopy.Data, Is.EqualTo(data));
                Assert.That(result.FormatCopy.CorrectedCode, Is.EqualTo(Code(data, false, false)));
                Assert.That(result.FormatCopy.CorrectedErrorCount, Is.EqualTo(errors));
                Assert.That(result.FormatCopy.MissingBitCount, Is.Zero);
                Assert.That(result.FormatCopy.KnownBitMask, Is.EqualTo(0x7fff));
            }
    }

    [TestCaseSource(nameof(RectangularData))]
    public void ReadsEveryRectangularSizeAndEccWithBothMasksAndCorrection(int data)
    {
        int version = data % 32;
        for (int errors = 0; errors <= 3; errors++)
            for (int start = 0; start < 18; start++)
            {
                QrMatrix matrix = RectangularFixture(data);
                Damage(matrix, true, false, start, errors, false);
                Damage(matrix, true, true, start + 7, errors, false);
                RectangularMicroInformationResult result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
                Assert.That(result.Success, Is.True);
                Assert.That(result.VersionIndex, Is.EqualTo(version));
                Assert.That(result.DimensionVersionIndex, Is.EqualTo(version));
                Assert.That(result.Width, Is.EqualTo(Widths[version]));
                Assert.That(result.Height, Is.EqualTo(Heights[version]));
                Assert.That(result.ErrorCorrectionLevel, Is.EqualTo(data < 32 ? QrErrorCorrectionLevel.Medium : QrErrorCorrectionLevel.High));
                Assert.That(result.MaskPattern, Is.EqualTo(QrMaskPattern.Four));
                Assert.That(result.SelectedFormatCopy, Is.Zero);
                Assert.That(result.FormatCopiesAgree, Is.True);
                Assert.That(result.FormatCopies[0].CorrectedCode, Is.EqualTo(Code(data, true, false)));
                Assert.That(result.FormatCopies[1].CorrectedCode, Is.EqualTo(Code(data, true, true)));
                Assert.That(result.FormatCopies[0].CorrectedCode, Is.Not.EqualTo(result.FormatCopies[1].CorrectedCode));
                Assert.That(result.FormatCopies.All(c => c.Data == data && c.CorrectedErrorCount == errors), Is.True);
            }
    }

    [TestCaseSource(nameof(MicroData))]
    public void RejectsMicroFormatVersionsThatDoNotMatchDimensions(int data)
    {
        QrMatrix matrix = MicroFixture(data);
        int otherSymbol = MicroVersions[data / 4] == 1 ? 1 : 0;
        SetCode(matrix, false, false, Code(otherSymbol * 4, false, false));
        MicroInformationResult result = MicroInformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
        Assert.That(result.FormatCopy.Success, Is.True);
        Assert.That(result.FormatCopy.Data, Is.EqualTo(otherSymbol * 4));
        Assert.That(result.Version, Is.Zero);
        Assert.That(result.MaskPattern, Is.EqualTo(QrMaskPattern.Automatic));
    }

    [TestCaseSource(nameof(RectangularData))]
    public void RejectsRectangularFormatSizesThatDoNotMatchDimensions(int data)
    {
        QrMatrix matrix = RectangularFixture(data);
        int other = (data + 1) % 32 + data / 32 * 32;
        foreach (bool secondary in new[] { false, true }) SetCode(matrix, true, secondary, Code(other, true, secondary));
        RectangularMicroInformationResult result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
        Assert.That(result.DimensionVersionIndex, Is.EqualTo(data % 32));
        Assert.That(result.VersionIndex, Is.EqualTo(-1));
        Assert.That(result.FormatCopiesAgree, Is.True);
        Assert.That(result.FormatCopies.All(c => c.Data == other), Is.True);
        Assert.That(result.Width, Is.Zero);
        Assert.That(result.Height, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EitherRectangularCopyCanRecoverAnUnavailablePartner(bool secondary)
    {
        QrMatrix matrix = RectangularFixture(42);
        Damage(matrix, true, secondary, 0, 18, true);
        RectangularMicroInformationResult result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.SelectedFormatCopy, Is.EqualTo(secondary ? 0 : 1));
        Assert.That(result.FormatCopiesAgree, Is.False);
        Assert.That(result.FormatCopies[secondary ? 1 : 0].Success, Is.False);
        Assert.That(result.VersionIndex, Is.EqualTo(10));
        Assert.That(result.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
    }

    [Test]
    public void ConflictingRectangularEccCopiesFailOnTiesAndPreferStrongerEvidence()
    {
        QrMatrix matrix = RectangularFixture(10);
        SetCode(matrix, true, true, Code(42, true, true));
        RectangularMicroInformationResult result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidFormat));
        Assert.That(result.SelectedFormatCopy, Is.EqualTo(-1));
        Assert.That(result.FormatCopies.All(c => c.Success), Is.True);
        Damage(matrix, true, false, 0, 1, false);
        result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.SelectedFormatCopy, Is.EqualTo(1));
        Assert.That(result.FormatCopiesAgree, Is.False);
        Assert.That(result.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
    }

    [Test]
    public void DimensionsDoNotOverrideAStrongerButInconsistentRectangularCopy()
    {
        QrMatrix matrix = RectangularFixture(10);
        SetCode(matrix, true, true, Code(11, true, true));
        RectangularMicroInformationResult result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidFormat));
        Damage(matrix, true, false, 0, 1, false);
        result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
        Assert.That(result.SelectedFormatCopy, Is.EqualTo(1));
        Assert.That(result.FormatCopies[0].Data, Is.EqualTo(10));
        Assert.That(result.FormatCopies[1].Data, Is.EqualTo(11));
    }

    [TestCase(false, 0, 6)]
    [TestCase(false, 1, 4)]
    [TestCase(false, 2, 2)]
    [TestCase(true, 0, 6)]
    [TestCase(true, 1, 4)]
    [TestCase(true, 2, 2)]
    public void CorrectsErrorsAndErasuresWithinBudget(bool rectangular, int errors, int erasures)
    {
        QrMatrix matrix = rectangular ? RectangularFixture(42) : MicroFixture(27);
        foreach (bool secondary in rectangular ? new[] { false, true } : new[] { false })
        {
            Damage(matrix, rectangular, secondary, 0, erasures, true);
            Damage(matrix, rectangular, secondary, 9, errors, false);
        }
        MatrixInformationCopy copy;
        if (rectangular)
        {
            RectangularMicroInformationResult result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
            Assert.That(result.Success, Is.True);
            Assert.That(result.FormatCopiesAgree, Is.True);
            copy = result.FormatCopies[0];
        }
        else
        {
            MicroInformationResult result = MicroInformationReader.Read(matrix, new QrDecodeOptions());
            Assert.That(result.Success, Is.True);
            copy = result.FormatCopy;
        }
        Assert.That(copy.MissingBitCount, Is.EqualTo(erasures));
        Assert.That(copy.CorrectedErrorCount, Is.EqualTo(errors));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RejectsMissingOrZeroConfidenceInformation(bool rectangular)
    {
        QrMatrix matrix = rectangular ? RectangularFixture(0) : MicroFixture(0);
        matrix.Confidence = new byte[matrix.Values.Length];
        AssertInvalidFormat(matrix, rectangular);
        matrix.Confidence = null;
        foreach (bool secondary in rectangular ? new[] { false, true } : new[] { false })
            Damage(matrix, rectangular, secondary, 0, 7, true);
        AssertInvalidFormat(matrix, rectangular);
    }

    [Test]
    public void MicroFormatElbowIsOneObservation()
    {
        QrMatrix matrix = MicroFixture(0);
        // The horizontal and vertical format arms meet at bit 7, cell (8, 8).
        matrix.Values[8 * matrix.Width + 8] = QrModuleValue.Unknown;
        MicroInformationResult result = MicroInformationReader.Read(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.FormatCopy.MissingBitCount, Is.EqualTo(1));
        Assert.That(result.FormatCopy.KnownBitMask, Is.EqualTo(0x7fff ^ (1 << 7)));
        Assert.That(result.Version, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RejectsUncorrectableObservedWords(bool rectangular)
    {
        QrMatrix matrix = rectangular ? RectangularFixture(0) : MicroFixture(0);
        // Find received words outside every radius-three sphere; this avoids the
        // false assumption that every four-bit error is detectable by BCH.
        foreach (bool secondary in rectangular ? new[] { false, true } : new[] { false })
        {
            int count = rectangular ? 64 : 32;
            int word = 0;
            while (Enumerable.Range(0, count).Any(data => System.Numerics.BitOperations.PopCount((uint)(word ^ Code(data, rectangular, secondary))) <= 3)) word++;
            SetCode(matrix, rectangular, secondary, word);
        }
        AssertInvalidFormat(matrix, rectangular);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HonorsAsymmetricQuietZonesAndPreservesCallerBuffers(bool rectangular)
    {
        QrMatrix source = rectangular ? RectangularFixture(63) : MicroFixture(0);
        QrMatrix matrix = new QrMatrix
        {
            Width = source.Width + 6, Height = source.Height + 8,
            QuietZoneLeft = 2, QuietZoneRight = 4, QuietZoneTop = 5, QuietZoneBottom = 3
        };
        matrix.Values = new QrModuleValue[matrix.Width * matrix.Height];
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        matrix.Roles = Enumerable.Repeat(QrModuleRole.Data, matrix.Values.Length).ToArray();
        for (int row = 0; row < source.Height; row++) Array.Copy(source.Values, row * source.Width, matrix.Values, (row + 5) * matrix.Width + 2, source.Width);
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        Assert.That(ReadSuccess(matrix, rectangular, new QrDecodeOptions()), Is.True);
        Assert.That(matrix.Values, Is.EqualTo(before));
        Assert.That(matrix.Confidence, Is.All.EqualTo(255));
        Assert.That(matrix.Roles, Is.All.EqualTo(QrModuleRole.Data));
    }

    [TestCase(false, 10, 10)]
    [TestCase(false, 12, 12)]
    [TestCase(false, 21, 21)]
    [TestCase(false, 11, 13)]
    [TestCase(true, 43, 8)]
    [TestCase(true, 41, 7)]
    [TestCase(true, 7, 43)]
    [TestCase(true, 17, 17)]
    public void RejectsUnsupportedDimensions(bool rectangular, int width, int height)
    {
        QrMatrix matrix = new QrMatrix { Width = width, Height = height, Values = new QrModuleValue[width * height] };
        if (rectangular)
        {
            RectangularMicroInformationResult result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
            Assert.That(result.FormatCopies, Is.Empty);
        }
        else Assert.That(MicroInformationReader.Read(matrix, new QrDecodeOptions()).FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RejectsInvalidInputsAndResourceLimits(bool rectangular)
    {
        Assert.Throws<QrInputException>(() => ReadSuccess(null!, rectangular, new QrDecodeOptions()));
        Action<QrMatrix>[] corruptions =
        [
            m => m.Width = 0, m => m.Height = -1, m => m.Values = null!, m => m.Values = [],
            m => m.Confidence = [], m => m.Roles = [], m => m.Values[0] = (QrModuleValue)99,
            m => m.QuietZoneLeft = -1, m => m.QuietZoneRight = m.Width,
            m => { m.QuietZoneTop = int.MaxValue; m.QuietZoneBottom = int.MaxValue; }
        ];
        foreach (Action<QrMatrix> corrupt in corruptions)
        {
            QrMatrix matrix = rectangular ? RectangularFixture(0) : MicroFixture(0);
            corrupt(matrix);
            Assert.Throws<QrInputException>(() => ReadSuccess(matrix, rectangular, new QrDecodeOptions()));
        }
        QrMatrix valid = rectangular ? RectangularFixture(0) : MicroFixture(0);
        Assert.Throws<QrInputException>(() => ReadSuccess(valid, rectangular, null!));
        Assert.Throws<QrInputException>(() => ReadSuccess(valid, rectangular, new QrDecodeOptions { MaximumMatrixDimension = 0 }));
        Assert.Throws<QrInputException>(() => ReadSuccess(valid, rectangular, new QrDecodeOptions { MaximumAllocationBytes = 0 }));
        Assert.Throws<QrResourceLimitException>(() => ReadSuccess(valid, rectangular, new QrDecodeOptions { MaximumMatrixDimension = 10 }));
        Assert.Throws<QrResourceLimitException>(() => ReadSuccess(valid, rectangular, new QrDecodeOptions { MaximumAllocationBytes = 100 }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReadsActualEncodedSymbols(bool rectangular)
    {
        QrSymbol symbol = new QrEncoder().EncodeText("123", new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = rectangular ? QrSymbolKind.rMQR : QrSymbolKind.Micro,
            ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = rectangular ? QrErrorCorrectionLevel.High : QrErrorCorrectionLevel.None,
            MinimumVersion = 1, MaximumVersion = rectangular ? 40 : 1
        });
        if (rectangular)
        {
            RectangularMicroInformationResult result = RectangularMicroInformationReader.Read(symbol.Matrix!, new QrDecodeOptions());
            Assert.That(result.Success, Is.True);
            Assert.That(result.Width, Is.EqualTo(symbol.Matrix!.Width - symbol.Matrix.QuietZoneLeft - symbol.Matrix.QuietZoneRight));
            Assert.That(result.Height, Is.EqualTo(symbol.Matrix.Height - symbol.Matrix.QuietZoneTop - symbol.Matrix.QuietZoneBottom));
            Assert.That(result.ErrorCorrectionLevel, Is.EqualTo(symbol.ErrorCorrectionLevel));
            Assert.That(result.MaskPattern, Is.EqualTo(symbol.MaskPattern));
        }
        else
        {
            MicroInformationResult result = MicroInformationReader.Read(symbol.Matrix!, new QrDecodeOptions());
            Assert.That(result.Success, Is.True);
            Assert.That(result.Version, Is.EqualTo(1));
            Assert.That(result.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.None));
            Assert.That(result.MaskPattern, Is.EqualTo(symbol.MaskPattern));
        }
    }

    static bool ReadSuccess(QrMatrix matrix, bool rectangular, QrDecodeOptions options) => rectangular
        ? RectangularMicroInformationReader.Read(matrix, options).Success : MicroInformationReader.Read(matrix, options).Success;

    static void AssertInvalidFormat(QrMatrix matrix, bool rectangular)
    {
        if (rectangular)
        {
            RectangularMicroInformationResult result = RectangularMicroInformationReader.Read(matrix, new QrDecodeOptions());
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidFormat));
            Assert.That(result.VersionIndex, Is.EqualTo(-1));
        }
        else
        {
            MicroInformationResult result = MicroInformationReader.Read(matrix, new QrDecodeOptions());
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidFormat));
            Assert.That(result.Version, Is.Zero);
        }
    }

    static QrMatrix MicroFixture(int data)
    {
        int size = 9 + 2 * MicroVersions[data / 4];
        QrMatrix matrix = new QrMatrix { Width = size, Height = size, Values = new QrModuleValue[size * size] };
        SetCode(matrix, false, false, Code(data, false, false));
        return matrix;
    }

    static QrMatrix RectangularFixture(int data)
    {
        int version = data % 32;
        QrMatrix matrix = new QrMatrix { Width = Widths[version], Height = Heights[version], Values = new QrModuleValue[Widths[version] * Heights[version]] };
        SetCode(matrix, true, false, Code(data, true, false));
        SetCode(matrix, true, true, Code(data, true, true));
        return matrix;
    }

    // Independent coordinate formulas and polynomial division for fixtures.
    static int Offset(QrMatrix matrix, bool rectangular, bool secondary, int bit)
    {
        int width = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int height = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        int row, column;
        if (!rectangular) { row = bit < 8 ? bit + 1 : 8; column = bit < 8 ? 8 : 15 - bit; }
        else if (!secondary) { row = bit < 15 ? bit % 5 + 1 : bit - 14; column = bit < 15 ? bit / 5 + 8 : 11; }
        else { row = bit < 15 ? height - 6 + bit % 5 : height - 6; column = bit < 15 ? width - 8 + bit / 5 : width + bit - 20; }
        return (row + matrix.QuietZoneTop) * matrix.Width + column + matrix.QuietZoneLeft;
    }

    static void SetCode(QrMatrix matrix, bool rectangular, bool secondary, int code)
    {
        for (int bit = 0; bit < (rectangular ? 18 : 15); bit++)
            matrix.Values[Offset(matrix, rectangular, secondary, bit)] = (code & (1 << bit)) != 0 ? QrModuleValue.Dark : QrModuleValue.Light;
    }

    static void Damage(QrMatrix matrix, bool rectangular, bool secondary, int start, int count, bool erase)
    {
        for (int bit = 0; bit < count; bit++)
        {
            int offset = Offset(matrix, rectangular, secondary, (start + bit) % (rectangular ? 18 : 15));
            matrix.Values[offset] = erase ? QrModuleValue.Unknown : matrix.Values[offset] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
        }
    }

    static int Code(int data, bool rectangular, bool secondary)
    {
        int parity = rectangular ? 12 : 10;
        int generator = rectangular ? 0x1f25 : 0x537;
        int xorMask = rectangular ? secondary ? 0x20a7b : 0x1fab2 : 0x4445;
        int shifted = data << parity;
        int remainder = shifted;
        for (int bit = 30; bit >= parity; bit--)
            if ((remainder & (1 << bit)) != 0) remainder ^= generator << (bit - parity);
        return (shifted | remainder) ^ xorMask;
    }
}
