using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrCapacityCalculatorTests
{
    [Test]
    public void CalculatesExactModel2CapacityThroughThePublicContract()
    {
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        QrCapacityRequest request = CreateRequest(
            QrSymbolKind.Model2,
            1,
            21,
            21,
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Numeric,
            41,
            137,
            1);

        QrCapacityResult result = calculator.CalculateCapacity(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Fits, Is.True);
            Assert.That(result.RequiredBitCount, Is.EqualTo(152));
            Assert.That(result.DataBitCapacity, Is.EqualTo(152));
            Assert.That(result.UnusedBitCount, Is.Zero);
            Assert.That(result.DataCodewordCount, Is.EqualTo(19));
            Assert.That(result.ErrorCorrectionCodewordCount, Is.EqualTo(7));
            Assert.That(result.TotalCodewordCount, Is.EqualTo(26));
            Assert.That(result.RemainderBitCount, Is.Zero);
        });
    }

    [Test]
    public void CalculatesExactMicroCapacityThroughThePublicContract()
    {
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        QrCapacityRequest request = CreateRequest(
            QrSymbolKind.Micro,
            1,
            11,
            11,
            QrErrorCorrectionLevel.None,
            QrEncodingMode.Numeric,
            5,
            17,
            0);

        QrCapacityResult result = calculator.CalculateCapacity(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Fits, Is.True);
            Assert.That(result.RequiredBitCount, Is.EqualTo(20));
            Assert.That(result.DataBitCapacity, Is.EqualTo(20));
            Assert.That(result.DataCodewordCount, Is.EqualTo(3));
            Assert.That(result.ErrorCorrectionCodewordCount, Is.EqualTo(2));
            Assert.That(result.TotalCodewordCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void CalculatesExactRmqrCapacityThroughThePublicContract()
    {
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        QrCapacityRequest request = CreateRequest(
            QrSymbolKind.rMQR,
            0,
            43,
            7,
            QrErrorCorrectionLevel.Medium,
            QrEncodingMode.Numeric,
            12,
            40,
            0);

        QrCapacityResult result = calculator.CalculateCapacity(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Fits, Is.True);
            Assert.That(result.RequiredBitCount, Is.EqualTo(47));
            Assert.That(result.DataBitCapacity, Is.EqualTo(48));
            Assert.That(result.UnusedBitCount, Is.EqualTo(1));
            Assert.That(result.DataCodewordCount, Is.EqualTo(6));
            Assert.That(result.ErrorCorrectionCodewordCount, Is.EqualTo(7));
            Assert.That(result.TotalCodewordCount, Is.EqualTo(13));
        });
    }

    [Test]
    public void SelectsCompleteFamilySizeAndBoostedEccParameters()
    {
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        QrCapacityRequest request = CreateRequest(
            QrSymbolKind.Model2,
            0,
            0,
            0,
            QrErrorCorrectionLevel.High,
            QrEncodingMode.Numeric,
            1,
            4,
            0);
        QrEncodeOptions options = new QrEncodeOptions
        {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            BoostErrorCorrectionLevel = true
        };

        QrCapacityRequest selected = calculator.SelectSymbolParameters(request, options);
        QrCapacityResult capacity = calculator.CalculateCapacity(selected);

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.Not.SameAs(request));
            Assert.That(selected.SymbolKind, Is.EqualTo(QrSymbolKind.Micro));
            Assert.That(selected.Version, Is.EqualTo(2));
            Assert.That(selected.Width, Is.EqualTo(13));
            Assert.That(selected.Height, Is.EqualTo(13));
            Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Medium));
            Assert.That(capacity.Fits, Is.True);
        });
    }

    [Test]
    public void HonorsForcedParametersAndDoesNotEnlargeForBoosting()
    {
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        QrCapacityRequest request = CreateRequest(
            QrSymbolKind.Model2,
            0,
            0,
            0,
            QrErrorCorrectionLevel.High,
            QrEncodingMode.Byte,
            17,
            136,
            0);
        QrEncodeOptions options = new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Model2,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            BoostErrorCorrectionLevel = true,
            MinimumVersion = 1,
            MaximumVersion = 1
        };

        QrCapacityRequest selected = calculator.SelectSymbolParameters(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(selected.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(selected.Version, Is.EqualTo(1));
            Assert.That(selected.Width, Is.EqualTo(21));
            Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
        });
    }

    [Test]
    public void PropagatesInvalidAndUnsupportedRequests()
    {
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        QrCapacityRequest legacy = CreateRequest(
            QrSymbolKind.LegacyModel1,
            1,
            21,
            21,
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Numeric,
            1,
            4,
            0);
        QrCapacityRequest undefined = CreateRequest(
            (QrSymbolKind)99,
            1,
            21,
            21,
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Numeric,
            1,
            4,
            0);
        QrCapacityRequest tooLarge = CreateRequest(
            QrSymbolKind.Model2,
            0,
            0,
            0,
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Byte,
            100,
            800,
            0);
        QrEncodeOptions bounded = new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Model2,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            MaximumVersion = 1
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => calculator.CalculateCapacity(null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => calculator.CalculateCapacity(legacy),
                Throws.TypeOf<QrUnsupportedFeatureException>());
            Assert.That(() => calculator.CalculateCapacity(undefined),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => calculator.SelectSymbolParameters(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => calculator.SelectSymbolParameters(tooLarge, bounded),
                Throws.TypeOf<QrCapacityException>());
        });
    }

    private static QrCapacityRequest CreateRequest(
        QrSymbolKind symbolKind,
        int version,
        int width,
        int height,
        QrErrorCorrectionLevel errorCorrectionLevel,
        QrEncodingMode encodingMode,
        int characterCount,
        int payloadBitCount,
        int controlBitCount)
    {
        return new QrCapacityRequest
        {
            SymbolKind = symbolKind,
            Version = version,
            Width = width,
            Height = height,
            ErrorCorrectionLevel = errorCorrectionLevel,
            EncodingMode = encodingMode,
            CharacterCount = characterCount,
            PayloadBitCount = payloadBitCount,
            ControlBitCount = controlBitCount
        };
    }
}
