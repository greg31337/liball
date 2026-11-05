using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MatrixExceptionContractTests
{
    [Test]
    public void MissingCodewordsRejectsRebuildWithoutChangingTheExistingSymbol()
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrSymbol symbol = new QrEncoder().EncodeText("HELLO", new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MaximumVersion = 1
        });
        QrMatrix original = symbol.Matrix!;
        QrModuleValue[] values = (QrModuleValue[])original.Values.Clone();
        byte[] codewords = symbol.Codewords;
        QrMaskPattern mask = symbol.MaskPattern;
        int width = symbol.Width;
        int height = symbol.Height;
        symbol.Codewords = null!;

        QrInputException error = Assert.Throws<QrInputException>(() => processor.BuildMatrix(symbol, 0))!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidInput));
        Assert.That(error.InputName, Is.EqualTo("Codewords"));
        Assert.That(symbol.Matrix, Is.SameAs(original));
        Assert.That(original.Values, Is.EqualTo(values));
        Assert.That(symbol.MaskPattern, Is.EqualTo(mask));
        Assert.That(symbol.Width, Is.EqualTo(width));
        Assert.That(symbol.Height, Is.EqualTo(height));
        Assert.That(symbol.Codewords, Is.Null);

        symbol.Codewords = codewords;
        QrMatrix rebuilt = processor.BuildMatrix(symbol, -1);
        Assert.That(rebuilt, Is.SameAs(symbol.Matrix).And.Not.SameAs(original));
        Assert.That(rebuilt.Values, Is.EqualTo(values));
        Assert.That(new QrDecoder().DecodeMatrix(rebuilt, new QrDecodeOptions()).Text, Is.EqualTo("HELLO"));
    }

    [TestCase("BuildMatrix")]
    [TestCase("ExtractCodewords")]
    [TestCase("ClassifyModules")]
    [TestCase("ValidateMatrix")]
    public void MatrixOperationsRejectLegacyEncodingAndInvalidMetadata(string member)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrSymbol symbol = new QrEncoder().EncodeText("HELLO", new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2,
            MaximumVersion = 1, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low, BoostErrorCorrectionLevel = false
        });
        object Call() => member switch
        {
            "BuildMatrix" => processor.BuildMatrix(symbol, 4),
            "ExtractCodewords" => processor.ExtractCodewords(symbol.Matrix!, symbol.SymbolKind, symbol.Version,
                symbol.ErrorCorrectionLevel, symbol.MaskPattern),
            "ClassifyModules" => processor.ClassifyModules(symbol.Matrix!, symbol.SymbolKind, symbol.Version, symbol.ErrorCorrectionLevel),
            "ValidateMatrix" => processor.ValidateMatrix(symbol),
            _ => throw new AssertionException(member)
        };
        symbol.SymbolKind = QrSymbolKind.LegacyModel1;
        QrUnsupportedFeatureException unsupported = Assert.Throws<QrUnsupportedFeatureException>(() => Call())!;
        Assert.That(unsupported.FeatureName, Is.Not.Empty);
        symbol.SymbolKind = (QrSymbolKind)99;
        QrInputException invalid = Assert.Throws<QrInputException>(() => Call())!;
        Assert.That(invalid.FailureReason, Is.EqualTo(QrFailureReason.InvalidInput));
        symbol.SymbolKind = QrSymbolKind.Model2;
        switch (Call())
        {
            case QrMatrix matrix: Assert.That(matrix.Width, Is.EqualTo(29)); break;
            case byte[] codewords: Assert.That(codewords, Is.EqualTo(symbol.Codewords)); break;
            case QrModuleRole[] roles: Assert.That(roles, Does.Contain(QrModuleRole.Data)); break;
            case QrValidationResult validation: Assert.That(validation.IsConformant, Is.True); break;
            default: Assert.Fail("Unexpected public result type."); break;
        }
    }

    [TestCase("BuildMatrix")]
    [TestCase("ExtractCodewords")]
    [TestCase("ClassifyModules")]
    [TestCase("ValidateMatrix")]
    [TestCase("TransformMatrix")]
    public void MatrixOperationsEnforceGeometryResourceLimits(string member)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrSymbol symbol = new QrEncoder().EncodeText("1", new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MaximumVersion = 1
        });
        // Invalid dimensions are rejected before inspecting or allocating oversized storage.
        QrMatrix oversized = new QrMatrix { Width = 16385, Height = 1, Values = [] };
        QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(() =>
        {
            switch (member)
            {
                case "BuildMatrix": processor.BuildMatrix(symbol, 16384); break;
                case "ExtractCodewords": processor.ExtractCodewords(oversized, QrSymbolKind.Model2, 1, symbol.ErrorCorrectionLevel, symbol.MaskPattern); break;
                case "ClassifyModules": processor.ClassifyModules(oversized, QrSymbolKind.Model2, 1, symbol.ErrorCorrectionLevel); break;
                case "ValidateMatrix": symbol.Matrix = oversized; processor.ValidateMatrix(symbol); break;
                case "TransformMatrix": processor.TransformMatrix(symbol.Matrix!, QrSymbolKind.Model2, 0, false, false, 0, 0, 0, 0, false, 0, 1000); break;
                default: throw new AssertionException(member);
            }
        })!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(error.LimitName, Is.EqualTo("MaximumWidth"));
        Assert.That(error.RequestedValue, Is.GreaterThan(error.MaximumValue));
        Assert.That(error.MaximumValue, Is.EqualTo(16384));
    }

    [Test]
    public void ReedSolomonCorrectionEnforcesBlockSizeAndErasureBounds()
    {
        IQrReedSolomonCodec codec = new QrReedSolomonCodec();
        QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(() =>
            codec.CorrectCodewords(new byte[256], 7, []))!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(error.LimitName, Is.EqualTo("ReedSolomonBlockLength"));
        Assert.That(error.RequestedValue, Is.EqualTo(256));
        Assert.That(error.MaximumValue, Is.EqualTo(255));
        foreach (int position in new[] { -1, 255 })
        {
            QrInputException invalid = Assert.Throws<QrInputException>(() =>
                codec.CorrectCodewords(new byte[255], 7, [position]))!;
            Assert.That(invalid.InputName, Is.EqualTo("erasurePositions"));
        }
        QrCorrectionResult corrected = codec.CorrectCodewords(new byte[255], 7, [254]);
        Assert.That(corrected.Success, Is.True);
        Assert.That(corrected.Verified, Is.True);
        Assert.That(corrected.Codewords, Is.EqualTo(new byte[255]));
    }
}
