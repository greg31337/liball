using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MatrixCodewordReaderTests
{
    static IEnumerable<TestCaseData> Configurations()
    {
        for (int version = 1; version <= 40; version++)
            for (int level = 0; level < 4; level++) yield return new TestCaseData(QrSymbolKind.Model2, version, level);
        yield return new TestCaseData(QrSymbolKind.Micro, 1, -1);
        for (int version = 2; version <= 4; version++)
            for (int level = 0; level <= (version == 4 ? 2 : 1); level++) yield return new TestCaseData(QrSymbolKind.Micro, version, level);
        for (int version = 0; version < 32; version++)
            for (int level = 0; level < 2; level++) yield return new TestCaseData(QrSymbolKind.rMQR, version, level);
    }

    [TestCaseSource(nameof(Configurations))]
    public void ExtractsAndDeinterleavesEveryConfigurationAndMask(QrSymbolKind kind, int version, int level)
    {
        byte[] data = Data(kind, version, level);
        byte[] codewords = BlockInterleaver.CreateCodewords(data, kind, version, level);
        int maskCount = kind == QrSymbolKind.Model2 ? 8 : kind == QrSymbolKind.Micro ? 4 : 1;
        for (int mask = 0; mask < maskCount; mask++)
        {
            QrMatrix matrix = Build(codewords, kind, version, level, mask);
            QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
            MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions());
            Assert.That(result.Success, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(result.Version, Is.EqualTo(version));
            Assert.That(result.SymbolKind, Is.EqualTo(kind));
            Assert.That(result.ExtractedCodewords, Is.EqualTo(codewords));
            Assert.That(result.DataCodewords, Is.EqualTo(data));
            Assert.That(result.DataBitCount, Is.EqualTo(data.Length * 8 - (kind == QrSymbolKind.Micro && version % 2 == 1 ? 4 : 0)));
            Assert.That(result.InvalidRemainderBitCount, Is.Zero);
            Assert.That(result.RemainderBitCount, Is.EqualTo(kind == QrSymbolKind.Model2 ? Model2VersionTable.GetRemainderBitCount(version)
                : kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetRemainderBitCount(version) : 0));
            Assert.That(result.Correction!.CorrectedErrorCount, Is.Zero);
            Assert.That(result.Correction.Codewords, Is.EqualTo(codewords));
            Assert.That(matrix.Values, Is.EqualTo(before));
            result.DataCodewords[0] ^= 255;
            Assert.That(data[0], Is.EqualTo(19));
        }
    }

    [TestCase(QrSymbolKind.Model2, 5, 2)]
    [TestCase(QrSymbolKind.Model2, 40, 3)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.Micro, 4, 2)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    public void RepairsDataAndParityErrorsAcrossBlocks(QrSymbolKind kind, int version, int level)
    {
        byte[] data = Data(kind, version, level);
        byte[] original = BlockInterleaver.CreateCodewords(data, kind, version, level);
        byte[] damaged = (byte[])original.Clone();
        int blocks = kind == QrSymbolKind.Model2 ? EccBlockTable.GetBlockCount(version, level)
            : kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetBlockCount(version, level) : 1;
        for (int index = 0; index < blocks; index++)
        {
            damaged[index] ^= 0x53;
            damaged[damaged.Length - 1 - index] ^= 0xac;
        }
        MatrixCodewordResult result = MatrixCodewordReader.Read(Build(damaged, kind, version, level, 0), kind,
            new QrDecodeOptions { EnableRecovery = false });
        Assert.That(result.Success, Is.True);
        Assert.That(result.ExtractedCodewords, Is.EqualTo(damaged));
        Assert.That(result.Correction!.Codewords, Is.EqualTo(original));
        Assert.That(result.DataCodewords, Is.EqualTo(data));
        Assert.That(result.Correction.CorrectedErrorCount, Is.EqualTo(blocks * 2));
        Assert.That(result.Correction.BlockResults.All(b => b.Verified), Is.True);
    }

    [TestCase(QrSymbolKind.Model2, 1, 3)]
    [TestCase(QrSymbolKind.Micro, 4, 2)]
    [TestCase(QrSymbolKind.rMQR, 0, 1)]
    public void UncorrectableDamageDoesNotExposePayload(QrSymbolKind kind, int version, int level)
    {
        byte[] codewords = BlockInterleaver.CreateCodewords(Data(kind, version, level), kind, version, level);
        for (int index = 0; index < codewords.Length / 2; index++) codewords[index] ^= (byte)(0x81 + index);
        MatrixCodewordResult result = MatrixCodewordReader.Read(Build(codewords, kind, version, level, 0), kind, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(result.DataCodewords, Is.Empty);
        Assert.That(result.Correction!.Success, Is.False);
    }

    [Test]
    public void M1DetectsDamageWithoutAcceptingCorrectedPayload()
    {
        byte[] codewords = BlockInterleaver.CreateMicroCodewords(Data(QrSymbolKind.Micro, 1, -1), 1, -1);
        codewords[0] ^= 0x80;
        MatrixCodewordResult result = MatrixCodewordReader.Read(Build(codewords, QrSymbolKind.Micro, 1, -1, 0), QrSymbolKind.Micro, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(result.Correction!.CorrectedErrorCount, Is.EqualTo(1));
        Assert.That(result.DataCodewords, Is.Empty);
    }

    [Test]
    public void RejectsNonzeroImplicitMicroDataNibbleAfterRepair()
    {
        byte[] data = Data(QrSymbolKind.Micro, 3, 0);
        data[^1] |= 1;
        byte[] codewords = BlockInterleaver.CreateMicroCodewords(data, 3, 0);
        MatrixCodewordResult result = MatrixCodewordReader.Read(Build(codewords, QrSymbolKind.Micro, 3, 0, 0), QrSymbolKind.Micro, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
        Assert.That(result.Correction!.Success, Is.True);
        Assert.That(result.Correction.DataCodewords[^1] & 15, Is.EqualTo(1));
        Assert.That(result.DataCodewords, Is.Empty);
    }

    [TestCase(QrSymbolKind.Model2, 2, 0)]
    [TestCase(QrSymbolKind.rMQR, 1, 0)]
    public void RemainderBitsMustBeKnownZeroAfterUnmasking(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Build(BlockInterleaver.CreateCodewords(Data(kind, version, level), kind, version, level), kind, version, level, 0);
        QrModuleRole[] roles = ModuleClassifier.CreateEncodingRoleMap(kind, version, level);
        int remainder = Array.IndexOf(roles, QrModuleRole.Remainder);
        Assert.That(remainder, Is.GreaterThanOrEqualTo(0));
        matrix.Values[remainder] = matrix.Values[remainder] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.InvalidRemainderBitCount, Is.EqualTo(1));
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
        Assert.That(result.Correction, Is.Null);
        matrix.Values[remainder] = QrModuleValue.Unknown;
        result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.InvalidRemainderBitCount, Is.EqualTo(1));
    }

    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Micro, 3, 0)]
    [TestCase(QrSymbolKind.rMQR, 10, 0)]
    public void UnknownPayloadBecomesAnErasureButUnrelatedFunctionCellsDoNot(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Build(BlockInterleaver.CreateCodewords(Data(kind, version, level), kind, version, level), kind, version, level, 0);
        QrModuleRole[] roles = ModuleClassifier.CreateEncodingRoleMap(kind, version, level);
        matrix.Values[Array.IndexOf(roles, QrModuleRole.Finder)] = QrModuleValue.Unknown;
        Assert.That(MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions()).Success, Is.True);
        int payload = Array.IndexOf(roles, QrModuleRole.Data);
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        matrix.Confidence[payload] = 0;
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.UnknownPayloadModuleCount, Is.EqualTo(1));
        Assert.That(result.ErasurePositions, Has.Length.EqualTo(1));
        Assert.That(result.DataCodewords, Is.EqualTo(Data(kind, version, level)));
        matrix.Confidence = null;
        matrix.Values[payload] = QrModuleValue.Unknown;
        Assert.That(MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions()).Success, Is.True);
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    public void ReadsEncoderOutputWithQuietZoneAndIgnoresCallerRoleClaims(QrSymbolKind kind)
    {
        QrSymbol symbol = new QrEncoder().EncodeText("12345", new QrEncodeOptions { ForceSymbolKind = true, SymbolKind = kind });
        QrMatrix matrix = symbol.Matrix!;
        matrix.Roles = Enumerable.Repeat(QrModuleRole.Unknown, matrix.Values.Length).ToArray();
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.ExtractedCodewords, Is.EqualTo(symbol.Codewords));
        Assert.That(result.DataBitCount, Is.EqualTo(symbol.DataBitCapacity));
        Assert.That(matrix.Values, Is.EqualTo(before));
        Assert.That(matrix.Roles, Is.All.EqualTo(QrModuleRole.Unknown));
        // Erase all format cells using a fresh classification map.
        QrModuleRole[] roles = ModuleClassifier.ClassifyModules(matrix, kind, result.Version,
            kind == QrSymbolKind.rMQR ? result.ErrorCorrectionLevel == QrErrorCorrectionLevel.Medium ? 0 : 1 : (int)result.ErrorCorrectionLevel);
        for (int index = 0; index < roles.Length; index++) if (roles[index] == QrModuleRole.Format) matrix.Values[index] = QrModuleValue.Unknown;
        result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions());
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidFormat));
        Assert.That(result.Correction, Is.Null);
    }

    [Test]
    public void ValidatesStorageGeometryAndWorkBudgetBeforeExtraction()
    {
        QrMatrix matrix = Build(BlockInterleaver.CreateModel2Codewords(Data(QrSymbolKind.Model2, 1, 0), 1, 0), QrSymbolKind.Model2, 1, 0, 0);
        Assert.Throws<QrInputException>(() => MatrixCodewordReader.Read(null!, QrSymbolKind.Model2, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => MatrixCodewordReader.Read(matrix, QrSymbolKind.Model2, null!));
        Assert.That(MatrixCodewordReader.Read(matrix, QrSymbolKind.LegacyModel1, new QrDecodeOptions()).Success, Is.False);
        Assert.Throws<QrInputException>(() => MatrixCodewordReader.Read(matrix, (QrSymbolKind)99, new QrDecodeOptions()));
        Assert.Throws<QrResourceLimitException>(() => MatrixCodewordReader.Read(matrix, QrSymbolKind.Model2, new QrDecodeOptions { MaximumAllocationBytes = 1024 }));
        Assert.Throws<QrResourceLimitException>(() => MatrixCodewordReader.Read(matrix, QrSymbolKind.Model2, new QrDecodeOptions { MaximumMatrixDimension = 20 }));
        matrix.Values = [];
        Assert.Throws<QrInputException>(() => MatrixCodewordReader.Read(matrix, QrSymbolKind.Model2, new QrDecodeOptions()));
        MatrixCodewordResult wrong = MatrixCodewordReader.Read(new QrMatrix { Width = 19, Height = 19, Values = new QrModuleValue[361] }, QrSymbolKind.Model2, new QrDecodeOptions());
        Assert.That(wrong.FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
    }

    static byte[] Data(QrSymbolKind kind, int version, int level)
    {
        int count = kind == QrSymbolKind.Model2 ? EccBlockTable.GetDataCodewordCount(version, level)
            : kind == QrSymbolKind.Micro ? MicroVersionTable.GetDataSymbolCharacterCount(version, level)
            : RectangularMicroVersionTable.GetDataCodewordCount(version, level);
        byte[] data = new byte[count];
        for (int index = 0; index < count; index++) data[index] = (byte)((index * 73 + 19) % 256);
        if (kind == QrSymbolKind.Micro && version % 2 == 1) data[^1] &= 0xf0;
        return data;
    }

    static QrMatrix Build(byte[] codewords, QrSymbolKind kind, int version, int level, int mask)
    {
        BitMatrixBuffer bits = kind == QrSymbolKind.Model2 ? Model2MatrixBuilder.CreateSelectedModel2Matrix(codewords, version, level, (QrMaskPattern)mask)
            : kind == QrSymbolKind.Micro ? MicroMatrixBuilder.CreateSelectedMicroMatrix(codewords, version, level, (QrMaskPattern)mask)
            : RectangularMicroMatrixBuilder.CreateSelectedRectangularMicroMatrix(codewords, version, level, QrMaskPattern.Four);
        QrMatrix matrix = new QrMatrix { Width = bits.GetWidth(), Height = bits.GetHeight(), Values = new QrModuleValue[bits.GetWidth() * bits.GetHeight()] };
        for (int row = 0; row < matrix.Height; row++)
            for (int column = 0; column < matrix.Width; column++)
                matrix.Values[row * matrix.Width + column] = bits.GetModule(row, column) ? QrModuleValue.Dark : QrModuleValue.Light;
        return matrix;
    }
}
