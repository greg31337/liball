using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MatrixDecodeRecoveryTests
{
    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Model2, 7, 2)]
    [TestCase(QrSymbolKind.Model2, 40, 3)]
    [TestCase(QrSymbolKind.Micro, 1, -1)]
    [TestCase(QrSymbolKind.Micro, 2, 1)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.Micro, 4, 2)]
    [TestCase(QrSymbolKind.rMQR, 0, 0)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    public void RecoversAllRotationsMirrorsAndPolarities(QrSymbolKind kind, int version, int level)
    {
        QrMatrix canonical = Fixture(kind, version, level);
        canonical = MatrixTransformer.AddQuietZone(canonical, kind, kind == QrSymbolKind.Model2 ? 4 : 2);
        canonical = MatrixTransformer.Crop(canonical, 1, 0, canonical.Width - 1, canonical.Height - 1);
        canonical.Confidence = Enumerable.Repeat((byte)255, canonical.Values.Length).ToArray();
        canonical.Roles = Enumerable.Repeat(QrModuleRole.Data, canonical.Values.Length).ToArray();
        QrModuleValue[] expected = (QrModuleValue[])canonical.Values.Clone();
        for (int rotation = 0; rotation < 4; rotation++)
            foreach (bool mirror in new[] { false, true })
                foreach (bool invert in new[] { false, true })
                {
                    QrMatrix input = MatrixTransformer.Rotate(canonical, rotation);
                    if (mirror) input = MatrixTransformer.Mirror(input);
                    if (invert) input = MatrixTransformer.InvertPolarity(input);
                    QrModuleValue[] before = (QrModuleValue[])input.Values.Clone();
                    QrDecodeOptions options = Options(kind);
                    MatrixRecoveryResult recovery = MatrixDecodeRecovery.RecoverCodewords(input, options);
                    Assert.That(recovery.Success, Is.True, $"{kind} {rotation} {mirror} {invert}: {recovery.FailureReason}");
                    Assert.That(recovery.IsMirrored, Is.EqualTo(mirror));
                    Assert.That(recovery.IsInverted, Is.EqualTo(invert));
                    Assert.That(recovery.QuarterTurnsClockwise, Is.EqualTo((4 - rotation) % 4));
                    Assert.That(recovery.Matrix!.Values, Is.EqualTo(expected));
                    Assert.That(recovery.Matrix.Confidence, Is.All.EqualTo(255));
                    Assert.That(recovery.Matrix.Roles, Is.EqualTo(canonical.Roles));
                    Assert.That(recovery.Matrix.QuietZoneLeft, Is.EqualTo(canonical.QuietZoneLeft));
                    Assert.That(recovery.Matrix.QuietZoneBottom, Is.EqualTo(canonical.QuietZoneBottom));
                    Assert.That(recovery.AttemptCount, Is.InRange(1, options.MaximumRecoveryAttempts));
                    QrDecodeResult decoded = MatrixDecodeRecovery.Decode(input, options);
                    Assert.That(decoded.Text, Is.EqualTo("123"));
                    Assert.That(decoded.RawBytes, Is.EqualTo(new byte[] { 49, 50, 51 }));
                    Assert.That(decoded.Symbol!.SymbolKind, Is.EqualTo(kind));
                    Assert.That(decoded.Symbol.Version, Is.EqualTo(kind == QrSymbolKind.rMQR ? 0 : version));
                    Assert.That(decoded.Symbol.Matrix!.Values, Is.EqualTo(expected));
                    Assert.That(decoded.CorrectedErrorCount, Is.Zero);
                    Assert.That(decoded.IsConformant, Is.False);
                    Assert.That(input.Values, Is.EqualTo(before));
                }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RetriesTheOtherFormatCopyAfterAValidButWrongMask(bool wrongPrimary)
    {
        QrMatrix matrix = Fixture(QrSymbolKind.Model2, 2, 0);
        SetModel2Code(matrix, false, !wrongPrimary, FormatCodeTable.GetCode(0, 7));
        FlipModel2Bit(matrix, false, wrongPrimary, 0); // Correct copy has weaker BCH evidence.
        Assert.That(MatrixCodewordReader.Read(matrix, QrSymbolKind.Model2, Options(QrSymbolKind.Model2)).Success, Is.False);
        MatrixRecoveryResult recovered = MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.Model2));
        Assert.That(recovered.Success, Is.True);
        Assert.That(recovered.UsedAlternateInformation, Is.True);
        Assert.That(recovered.AttemptCount, Is.EqualTo(2));
        Assert.That(recovered.Codewords!.MaskPattern, Is.EqualTo(QrMaskPattern.Zero));
        Assert.That(MatrixDecodeRecovery.Decode(matrix, Options(QrSymbolKind.Model2)).Text, Is.EqualTo("123"));
        QrDecodeOptions disabled = Options(QrSymbolKind.Model2);
        disabled.EnableRecovery = false;
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(matrix, disabled).Success, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ResolvesConflictingVersionAndFormatCopiesTogether(bool wrongPrimary)
    {
        QrMatrix matrix = Fixture(QrSymbolKind.Model2, 7, 0);
        SetModel2Code(matrix, true, !wrongPrimary, VersionCodeTable.GetCode(8));
        SetModel2Code(matrix, false, wrongPrimary, FormatCodeTable.GetCode(0, 7));
        Assert.That(Model2InformationReader.Read(matrix, Options(QrSymbolKind.Model2)).Success, Is.False);
        MatrixRecoveryResult recovered = MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.Model2));
        Assert.That(recovered.Success, Is.True);
        Assert.That(recovered.UsedAlternateInformation, Is.True);
        Assert.That(recovered.Codewords!.Version, Is.EqualTo(7));
        Assert.That(MatrixDecodeRecovery.Decode(matrix, Options(QrSymbolKind.Model2)).Text, Is.EqualTo("123"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NeverReplacesMissingOrContradictingVersionEvidenceWithDimensions(bool missing)
    {
        QrMatrix matrix = Fixture(QrSymbolKind.Model2, 7, 0);
        SetModel2Code(matrix, true, false, missing ? 0 : VersionCodeTable.GetCode(8));
        SetModel2Code(matrix, true, true, missing ? 0 : VersionCodeTable.GetCode(8));
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.Model2)).Success, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RectangularFormatAlternativesMustAgreeWithSizeAndPassRs(bool wrongSize)
    {
        QrMatrix matrix = Fixture(QrSymbolKind.rMQR, 31, 0);
        SetRectangularFormat(matrix, 31, true, wrongSize ? 30 : 31, wrongSize ? 0 : 1);
        MatrixRecoveryResult recovered = MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.rMQR));
        Assert.That(recovered.Success, Is.True);
        Assert.That(recovered.UsedAlternateInformation, Is.True);
        Assert.That(MatrixDecodeRecovery.Decode(matrix, Options(QrSymbolKind.rMQR)).Text, Is.EqualTo("123"));
        SetRectangularFormat(matrix, 31, false, 30, 0);
        SetRectangularFormat(matrix, 31, true, 30, 0);
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.rMQR)).Success, Is.False);
    }

    [Test]
    public void LegacyAllowsRotationAndFormatRecoveryButNotMirrorOrInversion()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../tests/Fixtures/Golden/matrices/legacy-model1-v02-m.txt"));
        string[] rows = File.ReadAllLines(path);
        QrMatrix matrix = new QrMatrix { Width = 25, Height = 25,
            Values = string.Concat(rows).Select(c => c == '1' ? QrModuleValue.Dark : QrModuleValue.Light).ToArray() };
        for (int turn = 0; turn < 4; turn++)
            Assert.That(MatrixDecodeRecovery.Decode(MatrixTransformer.Rotate(matrix, turn), Options(QrSymbolKind.LegacyModel1)).Text, Is.EqualTo("QR Code Model 1 "));
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(MatrixTransformer.Mirror(matrix), Options(QrSymbolKind.LegacyModel1)).Success, Is.False);
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(MatrixTransformer.InvertPolarity(matrix), Options(QrSymbolKind.LegacyModel1)).Success, Is.False);
        SetModel2Code(matrix, false, true, LegacyModel1Table.GetFormatCode(1, 0));
        MatrixRecoveryResult recovered = MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.LegacyModel1));
        Assert.That(recovered.Success, Is.True);
        Assert.That(recovered.UsedAlternateInformation, Is.True);
        Assert.That(MatrixDecodeRecovery.Decode(matrix, Options(QrSymbolKind.LegacyModel1)).SymbologyIdentifier, Is.EqualTo("]Q0"));
    }

    [TestCase(QrSymbolKind.Model2, 2)]
    [TestCase(QrSymbolKind.Micro, 4)]
    [TestCase(QrSymbolKind.rMQR, 31)]
    public void StrictPaddingFailureDoesNotSilentlyEnableLenientPolicy(QrSymbolKind kind, int version)
    {
        byte[] data = Data(kind, version, 0);
        data[^1] ^= 1;
        QrMatrix matrix = Build(BlockInterleaver.CreateCodewords(data, kind, version, 0), kind, version, 0);
        matrix = MatrixTransformer.InvertPolarity(MatrixTransformer.Rotate(matrix, 1));
        QrDecodeOptions options = Options(kind);
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(matrix, options).Success, Is.True);
        Assert.Throws<QrDecodeException>(() => MatrixDecodeRecovery.Decode(matrix, options));
        options.Strict = false;
        QrDecodeResult decoded = MatrixDecodeRecovery.Decode(matrix, options);
        Assert.That(decoded.Text, Is.EqualTo("123"));
        Assert.That(decoded.Warnings, Has.Some.Contains("padding validation was disabled"));
        Assert.That(decoded.Warnings, Has.Some.Contains("reversed-reflectance"));
    }

    [Test]
    public void LenientPaddingStillRejectsAnInvalidMode()
    {
        byte[] data = Data(QrSymbolKind.Model2, 2, 0);
        data[0] |= 0xf0;
        QrMatrix matrix = Build(BlockInterleaver.CreateModel2Codewords(data, 2, 0), QrSymbolKind.Model2, 2, 0);
        QrDecodeOptions options = Options(QrSymbolKind.Model2);
        options.Strict = false;
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => MatrixDecodeRecovery.Decode(matrix, options))!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidMode));
    }

    [Test]
    public void HonorsMirrorPolarityAndFamilyConstraints()
    {
        QrMatrix matrix = Fixture(QrSymbolKind.Model2, 2, 0);
        QrDecodeOptions options = Options(QrSymbolKind.Model2);
        options.TryMirrored = false;
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(MatrixTransformer.Mirror(matrix), options).Success, Is.False);
        options.TryInverted = false;
        QrMatrix inverted = MatrixTransformer.InvertPolarity(matrix);
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(inverted, options).Success, Is.False);
        options.Polarity = QrPolarity.LightOnDark;
        Assert.That(MatrixDecodeRecovery.Decode(inverted, options).Text, Is.EqualTo("123"));
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(matrix, options).Success, Is.False);
        options.Polarity = QrPolarity.Automatic;
        options.AllowModel2 = false;
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(matrix, options).AttemptCount, Is.Zero);
    }

    [Test]
    public void EnforcesAttemptAndAllocationLimitsAndReportsStableFailure()
    {
        QrMatrix matrix = Fixture(QrSymbolKind.Model2, 2, 0);
        SetModel2Code(matrix, false, true, FormatCodeTable.GetCode(0, 7));
        QrDecodeOptions options = Options(QrSymbolKind.Model2);
        options.MaximumRecoveryAttempts = 1;
        MatrixRecoveryResult first = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        MatrixRecoveryResult second = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        Assert.That(first.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(first.AttemptCount, Is.EqualTo(1));
        Assert.That(first.Codewords, Is.Null);
        Assert.That(first.Matrix, Is.Null);
        Assert.That(second.FailureReason, Is.EqualTo(first.FailureReason));
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => MatrixDecodeRecovery.Decode(matrix, options))!;
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(1));
        Assert.Throws<QrResourceLimitException>(() => MatrixDecodeRecovery.Decode(matrix, new QrDecodeOptions { MaximumAllocationBytes = 1024 }));
        Assert.Throws<QrInputException>(() => MatrixDecodeRecovery.Decode(matrix, new QrDecodeOptions { MaximumRecoveryAttempts = 0 }));
        Assert.Throws<QrInputException>(() => MatrixDecodeRecovery.Decode(matrix, new QrDecodeOptions { Polarity = (QrPolarity)99 }));
        Assert.Throws<QrInputException>(() => MatrixDecodeRecovery.Decode(null!, options));
        Assert.Throws<QrInputException>(() => MatrixDecodeRecovery.Decode(matrix, null!));
    }

    [Test]
    public void UncorrectableDataNeverExposesAPartialRecoveredResult()
    {
        byte[] codewords = BlockInterleaver.CreateModel2Codewords(Data(QrSymbolKind.Model2, 2, 0), 2, 0);
        for (int index = 0; index < 20; index++) codewords[index] ^= (byte)(index + 0x81);
        QrMatrix matrix = Build(codewords, QrSymbolKind.Model2, 2, 0);
        SetModel2Code(matrix, false, true, FormatCodeTable.GetCode(0, 7));
        MatrixRecoveryResult result = MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.Model2));
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(result.Codewords, Is.Null);
        Assert.That(result.Matrix, Is.Null);
    }

    [Test]
    public void OrdinaryRotationAndRsRepairWorkWithoutOptionalMetadataRecovery()
    {
        byte[] original = BlockInterleaver.CreateModel2Codewords(Data(QrSymbolKind.Model2, 2, 0), 2, 0);
        byte[] damaged = (byte[])original.Clone();
        damaged[1] ^= 0x7f;
        damaged[^1] ^= 0x53;
        QrMatrix matrix = MatrixTransformer.Rotate(Build(damaged, QrSymbolKind.Model2, 2, 0), 1);
        QrDecodeOptions options = Options(QrSymbolKind.Model2);
        options.EnableRecovery = false;
        options.TryMirrored = false;
        options.TryInverted = false;
        QrDecodeResult decoded = MatrixDecodeRecovery.Decode(matrix, options);
        Assert.That(decoded.Text, Is.EqualTo("123"));
        Assert.That(decoded.CorrectedErrorCount, Is.EqualTo(2));
        Assert.That(decoded.Symbol!.Codewords, Is.EqualTo(original));
        Assert.That(decoded.Symbol.DataBitCapacity, Is.EqualTo(272));
        Assert.That(decoded.Symbol.UsedDataBitCount, Is.EqualTo(24));
        Assert.That(decoded.Symbol.UnusedDataBitCount, Is.EqualTo(248));
        Assert.That(decoded.Symbol.ErrorCorrectionCodewordCount, Is.EqualTo(10));
        Assert.That(decoded.Warnings, Has.Some.Contains("270 clockwise degrees"));
    }

    [Test]
    public void ForcedMetadataChoiceStillRequiresBchVerificationAndValidIndex()
    {
        QrMatrix matrix = Fixture(QrSymbolKind.Model2, 2, 0);
        SetModel2Code(matrix, false, false, 0);
        Assert.That(MatrixCodewordReader.ReadWithCopyChoices(matrix, QrSymbolKind.Model2, Options(QrSymbolKind.Model2), 0, -1).Success, Is.False);
        Assert.That(MatrixCodewordReader.ReadWithCopyChoices(matrix, QrSymbolKind.Model2, Options(QrSymbolKind.Model2), 1, -1).Success, Is.True);
        Assert.Throws<QrInputException>(() => Model2InformationReader.ReadWithCopyChoices(matrix, Options(QrSymbolKind.Model2), 2, -1));
        Assert.Throws<QrInputException>(() => Model2InformationReader.ReadWithCopyChoices(matrix, Options(QrSymbolKind.Model2), -2, -1));
    }

    static QrDecodeOptions Options(QrSymbolKind kind) => new QrDecodeOptions
    {
        MaximumMatrixDimension = 200,
        AllowModel2 = kind == QrSymbolKind.Model2, AllowMicro = kind == QrSymbolKind.Micro,
        AllowRectangularMicro = kind == QrSymbolKind.rMQR, AllowLegacyModel1 = kind == QrSymbolKind.LegacyModel1
    };

    static byte[] Data(QrSymbolKind kind, int version, int level) => CodewordFinalizer.CreateDataCodewords(
        [new QrSegment { Mode = QrEncodingMode.Numeric, CharacterCount = 3, Text = "123" }], kind, version, level, false);

    static QrMatrix Fixture(QrSymbolKind kind, int version, int level) => Build(
        BlockInterleaver.CreateCodewords(Data(kind, version, level), kind, version, level), kind, version, level);

    static QrMatrix Build(byte[] codewords, QrSymbolKind kind, int version, int level)
    {
        BitMatrixBuffer bits = kind == QrSymbolKind.Model2 ? Model2MatrixBuilder.CreateSelectedModel2Matrix(codewords, version, level, QrMaskPattern.Zero)
            : kind == QrSymbolKind.Micro ? MicroMatrixBuilder.CreateSelectedMicroMatrix(codewords, version, level, QrMaskPattern.Zero)
            : RectangularMicroMatrixBuilder.CreateSelectedRectangularMicroMatrix(codewords, version, level, QrMaskPattern.Four);
        QrMatrix matrix = new QrMatrix { Width = bits.GetWidth(), Height = bits.GetHeight(), Values = new QrModuleValue[bits.GetWidth() * bits.GetHeight()] };
        for (int row = 0; row < matrix.Height; row++)
            for (int col = 0; col < matrix.Width; col++) matrix.Values[row * matrix.Width + col] = bits.GetModule(row, col) ? QrModuleValue.Dark : QrModuleValue.Light;
        return matrix;
    }

    static void SetModel2Code(QrMatrix matrix, bool versionInfo, bool secondary, int code)
    {
        int version = (matrix.Width - 17) / 4;
        for (int bit = 0; bit < (versionInfo ? 18 : 15); bit++)
        {
            int row = versionInfo ? secondary ? Model2PlacementTable.GetVersionSecondaryRow(version, bit) : Model2PlacementTable.GetVersionPrimaryRow(version, bit)
                : secondary ? Model2PlacementTable.GetFormatSecondaryRow(version, bit) : Model2PlacementTable.GetFormatPrimaryRow(bit);
            int col = versionInfo ? secondary ? Model2PlacementTable.GetVersionSecondaryColumn(version, bit) : Model2PlacementTable.GetVersionPrimaryColumn(version, bit)
                : secondary ? Model2PlacementTable.GetFormatSecondaryColumn(version, bit) : Model2PlacementTable.GetFormatPrimaryColumn(bit);
            matrix.Values[row * matrix.Width + col] = (code & (1 << bit)) != 0 ? QrModuleValue.Dark : QrModuleValue.Light;
        }
    }

    static void FlipModel2Bit(QrMatrix matrix, bool versionInfo, bool secondary, int bit)
    {
        int version = (matrix.Width - 17) / 4;
        int row = versionInfo ? secondary ? Model2PlacementTable.GetVersionSecondaryRow(version, bit) : Model2PlacementTable.GetVersionPrimaryRow(version, bit)
            : secondary ? Model2PlacementTable.GetFormatSecondaryRow(version, bit) : Model2PlacementTable.GetFormatPrimaryRow(bit);
        int col = versionInfo ? secondary ? Model2PlacementTable.GetVersionSecondaryColumn(version, bit) : Model2PlacementTable.GetVersionPrimaryColumn(version, bit)
            : secondary ? Model2PlacementTable.GetFormatSecondaryColumn(version, bit) : Model2PlacementTable.GetFormatPrimaryColumn(bit);
        int index = row * matrix.Width + col;
        matrix.Values[index] = matrix.Values[index] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
    }

    static void SetRectangularFormat(QrMatrix matrix, int version, bool secondary, int encodedVersion, int level)
    {
        int code = secondary ? RectangularMicroFormatCodeTable.GetSubfinderSideCode(encodedVersion, level) : RectangularMicroFormatCodeTable.GetFinderSideCode(encodedVersion, level);
        for (int bit = 0; bit < 18; bit++)
        {
            int row = secondary ? RectangularMicroPlacementTable.GetSubfinderSideFormatRow(version, bit) : RectangularMicroPlacementTable.GetFinderSideFormatRow(bit);
            int col = secondary ? RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(version, bit) : RectangularMicroPlacementTable.GetFinderSideFormatColumn(bit);
            matrix.Values[row * matrix.Width + col] = (code & (1 << bit)) != 0 ? QrModuleValue.Dark : QrModuleValue.Light;
        }
    }
}
