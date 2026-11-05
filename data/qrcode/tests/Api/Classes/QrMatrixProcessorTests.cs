using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrMatrixProcessorTests
{
    [TestCase(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, 4)]
    [TestCase(QrSymbolKind.Micro, 3, QrErrorCorrectionLevel.Medium, 2)]
    [TestCase(QrSymbolKind.rMQR, 10, QrErrorCorrectionLevel.High, 2)]
    public void BuildsAndExtractsEveryGeneratedProfileThroughThePublicContract(
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel level,
        int expectedQuietZone)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrSymbol symbol = CreateEmptySymbol(symbolKind, version, level);
        byte[] originalCodewords = (byte[])symbol.Codewords.Clone();

        QrMatrix matrix = processor.BuildMatrix(symbol, -1);
        byte[] extracted = processor.ExtractCodewords(
            matrix,
            symbolKind,
            version,
            level,
            symbol.MaskPattern);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Matrix, Is.SameAs(matrix));
            Assert.That(symbol.MaskPattern, Is.Not.EqualTo(QrMaskPattern.Automatic));
            Assert.That(matrix.Width, Is.EqualTo(symbol.Width + expectedQuietZone * 2));
            Assert.That(matrix.Height, Is.EqualTo(symbol.Height + expectedQuietZone * 2));
            Assert.That(matrix.QuietZoneLeft, Is.EqualTo(expectedQuietZone));
            Assert.That(matrix.QuietZoneTop, Is.EqualTo(expectedQuietZone));
            Assert.That(matrix.QuietZoneRight, Is.EqualTo(expectedQuietZone));
            Assert.That(matrix.QuietZoneBottom, Is.EqualTo(expectedQuietZone));
            Assert.That(matrix.Roles, Is.Not.Null);
            Assert.That(matrix.Roles, Has.None.EqualTo(QrModuleRole.Unknown));
            Assert.That(extracted, Is.EqualTo(originalCodewords));
            Assert.That(symbol.Codewords, Is.EqualTo(originalCodewords));
            Assert.That(symbol.TotalCodewordCount, Is.EqualTo(originalCodewords.Length));
            Assert.That(
                symbol.ErrorCorrectionCodewordCount,
                Is.EqualTo(symbol.TotalCodewordCount - symbol.DataCodewordCount));
            Assert.That(symbol.UnusedDataBitCount, Is.EqualTo(symbol.DataBitCapacity));
        });
    }

    [Test]
    public void TransformPipelineAppliesCropOrientationQuietZoneInversionAndScaleInOrder()
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrMatrix source = new QrMatrix
        {
            Width = 3,
            Height = 2,
            Values = new QrModuleValue[]
            {
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Light
            },
            Roles = new QrModuleRole[]
            {
                QrModuleRole.Finder,
                QrModuleRole.Data,
                QrModuleRole.ErrorCorrection,
                QrModuleRole.Timing,
                QrModuleRole.Remainder,
                QrModuleRole.Format
            },
            Confidence = new byte[] { 10, 20, 30, 40, 50, 60 }
        };

        QrMatrix result = processor.TransformMatrix(
            source,
            QrSymbolKind.Model2,
            1,
            true,
            true,
            0,
            0,
            1,
            0,
            true,
            1,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(8));
            Assert.That(result.Height, Is.EqualTo(8));
            Assert.That(result.QuietZoneLeft, Is.EqualTo(2));
            Assert.That(result.QuietZoneTop, Is.EqualTo(2));
            Assert.That(result.QuietZoneRight, Is.EqualTo(2));
            Assert.That(result.QuietZoneBottom, Is.EqualTo(2));
            Assert.That(result.Values[0], Is.EqualTo(QrModuleValue.Dark));
            Assert.That(result.Roles![0], Is.EqualTo(QrModuleRole.QuietZone));
            Assert.That(result.Confidence![0], Is.EqualTo(255));
            Assert.That(result.Values[2 * result.Width + 2], Is.EqualTo(QrModuleValue.Dark));
            Assert.That(result.Roles[2 * result.Width + 2], Is.EqualTo(QrModuleRole.Timing));
            Assert.That(result.Confidence[2 * result.Width + 2], Is.EqualTo(40));
            Assert.That(source.Width, Is.EqualTo(3));
            Assert.That(source.Height, Is.EqualTo(2));
        });
    }

    [Test]
    public void ClassificationAndValidationUpdateTheSuppliedPublicObjects()
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrSymbol symbol = CreateEmptySymbol(
            QrSymbolKind.Model2,
            2,
            QrErrorCorrectionLevel.Medium);
        QrMatrix matrix = processor.BuildMatrix(symbol, -1);
        matrix.Roles = null;

        QrModuleRole[] roles = processor.ClassifyModules(
            matrix,
            symbol.SymbolKind,
            symbol.Version,
            symbol.ErrorCorrectionLevel);
        QrValidationResult validation = processor.ValidateMatrix(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(matrix.Roles, Is.SameAs(roles));
            Assert.That(roles, Has.None.EqualTo(QrModuleRole.Unknown));
            Assert.That(validation.IsConformant, Is.True);
            Assert.That(symbol.Validation, Is.SameAs(validation));
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void ReusableProcessorInstancesDoNotShareMutableMatrices()
    {
        IQrMatrixProcessor first = new QrMatrixProcessor();
        IQrMatrixProcessor second = new QrMatrixProcessor();
        QrSymbol firstSymbol = CreateEmptySymbol(
            QrSymbolKind.Micro,
            2,
            QrErrorCorrectionLevel.Low);
        QrSymbol secondSymbol = CreateEmptySymbol(
            QrSymbolKind.Micro,
            2,
            QrErrorCorrectionLevel.Low);

        QrMatrix firstMatrix = first.BuildMatrix(firstSymbol, 1);
        QrMatrix secondMatrix = second.BuildMatrix(secondSymbol, 1);
        QrModuleValue original = secondMatrix.Values[0];
        firstMatrix.Values[0] = QrModuleValue.Unknown;
        firstMatrix.Roles![0] = QrModuleRole.Unknown;

        Assert.Multiple(() =>
        {
            Assert.That(secondMatrix.Values[0], Is.EqualTo(original));
            Assert.That(secondMatrix.Roles![0], Is.EqualTo(QrModuleRole.QuietZone));
            Assert.That(secondMatrix.Values, Is.Not.SameAs(firstMatrix.Values));
            Assert.That(secondMatrix.Roles, Is.Not.SameAs(firstMatrix.Roles));
        });
    }

    [Test]
    public void InvalidPublicInputsUseTypedLibraryExceptions()
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrSymbol wrongCodewords = CreateEmptySymbol(
            QrSymbolKind.Model2,
            1,
            QrErrorCorrectionLevel.Low);
        wrongCodewords.Codewords = new byte[1];
        QrSymbol legacy = new QrSymbol
        {
            SymbolKind = QrSymbolKind.LegacyModel1,
            Version = 1,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            Codewords = new byte[26]
        };
        QrMatrix unknown = new QrMatrix
        {
            Width = 21,
            Height = 21,
            Values = new QrModuleValue[441]
        };
        QrMatrix tiny = new QrMatrix
        {
            Width = 2,
            Height = 2,
            Values = new QrModuleValue[4]
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => processor.BuildMatrix(null!, -1),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => processor.BuildMatrix(wrongCodewords, -1),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => processor.BuildMatrix(legacy, -1),
                Throws.TypeOf<QrUnsupportedFeatureException>());
            Assert.That(
                () => processor.ExtractCodewords(
                    unknown,
                    QrSymbolKind.Model2,
                    1,
                    QrErrorCorrectionLevel.Low,
                    QrMaskPattern.Zero),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => processor.ExtractCodewords(
                    CreateKnownMatrix(21, 21),
                    QrSymbolKind.Model2,
                    1,
                    QrErrorCorrectionLevel.Low,
                    QrMaskPattern.Automatic),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => processor.ExtractCodewords(
                    tiny,
                    QrSymbolKind.Model2,
                    1,
                    QrErrorCorrectionLevel.Low,
                    QrMaskPattern.Zero),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => processor.TransformMatrix(
                    null!,
                    QrSymbolKind.Model2,
                    0,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    false,
                    0,
                    1),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => processor.TransformMatrix(
                    tiny,
                    QrSymbolKind.Model2,
                    0,
                    false,
                    false,
                    -1,
                    0,
                    0,
                    0,
                    false,
                    0,
                    1),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => processor.TransformMatrix(
                    tiny,
                    QrSymbolKind.Model2,
                    0,
                    false,
                    false,
                    1,
                    0,
                    1,
                    0,
                    false,
                    0,
                    1),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => processor.TransformMatrix(
                    tiny,
                    QrSymbolKind.Model2,
                    0,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    false,
                    0,
                    0),
                Throws.TypeOf<QrInputException>());
        });
    }

    static QrSymbol CreateEmptySymbol(
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel level)
    {
        int errorCorrectionIndex = QrMatrixProcessor.GetErrorCorrectionIndex(
            symbolKind,
            version,
            level);
        byte[] data = CodewordFinalizer.CreateDataCodewords(
            new QrSegment[0],
            symbolKind,
            version,
            errorCorrectionIndex,
            false);
        byte[] codewords = BlockInterleaver.CreateCodewords(
            data,
            symbolKind,
            version,
            errorCorrectionIndex);
        return new QrSymbol
        {
            SymbolKind = symbolKind,
            Version = version,
            ErrorCorrectionLevel = level,
            MaskPattern = QrMaskPattern.Automatic,
            Codewords = codewords,
            Segments = new QrSegment[0],
            UsedDataBitCount = 0
        };
    }

    static QrMatrix CreateKnownMatrix(int width, int height)
    {
        QrMatrix result = new QrMatrix
        {
            Width = width,
            Height = height,
            Values = new QrModuleValue[width * height]
        };
        for (int index = 0; index < result.Values.Length; index++)
        {
            result.Values[index] = QrModuleValue.Light;
        }

        return result;
    }
}
