using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class SymbolParameterSelectorTests
{
    [TestCase(QrErrorCorrectionLevel.None, QrSymbolKind.Micro, 1, 11, 11)]
    [TestCase(QrErrorCorrectionLevel.Low, QrSymbolKind.Micro, 2, 13, 13)]
    [TestCase(QrErrorCorrectionLevel.Medium, QrSymbolKind.Micro, 2, 13, 13)]
    [TestCase(QrErrorCorrectionLevel.Quartile, QrSymbolKind.Micro, 4, 17, 17)]
    [TestCase(QrErrorCorrectionLevel.High, QrSymbolKind.rMQR, 0, 27, 11)]
    public void SelectsTheSmallestAreaAcrossPermittedFamilies(
        QrErrorCorrectionLevel errorCorrectionLevel,
        QrSymbolKind expectedKind,
        int expectedVersion,
        int expectedWidth,
        int expectedHeight)
    {
        QrCapacityRequest request = CreateRequest(
            errorCorrectionLevel,
            QrEncodingMode.Numeric,
            1,
            4,
            0);

        QrCapacityRequest selected = SymbolParameterSelector.SelectSmallestFitting(
            request,
            new QrEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(selected.SymbolKind, Is.EqualTo(expectedKind));
            Assert.That(selected.Version, Is.EqualTo(expectedVersion));
            Assert.That(selected.Width, Is.EqualTo(expectedWidth));
            Assert.That(selected.Height, Is.EqualTo(expectedHeight));
            Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(errorCorrectionLevel));
        });
    }

    [Test]
    public void ForcedFamiliesGrowToTheirFirstFittingSize()
    {
        QrEncodeOptions model2Options = ForceFamily(QrSymbolKind.Model2);
        QrCapacityRequest model2 = SymbolParameterSelector.SelectSmallestFitting(
            CreateRequest(QrErrorCorrectionLevel.Low, QrEncodingMode.Byte, 18, 144, 0),
            model2Options);
        QrEncodeOptions microOptions = ForceFamily(QrSymbolKind.Micro);
        QrCapacityRequest micro = SymbolParameterSelector.SelectSmallestFitting(
            CreateRequest(QrErrorCorrectionLevel.Low, QrEncodingMode.Byte, 10, 80, 0),
            microOptions);
        QrEncodeOptions rmqrOptions = ForceFamily(QrSymbolKind.rMQR);
        QrCapacityRequest rmqr = SymbolParameterSelector.SelectSmallestFitting(
            CreateRequest(QrErrorCorrectionLevel.Medium, QrEncodingMode.Byte, 6, 48, 0),
            rmqrOptions);

        Assert.Multiple(() =>
        {
            Assert.That(model2.Version, Is.EqualTo(2));
            Assert.That(model2.Width, Is.EqualTo(25));
            Assert.That(model2.Height, Is.EqualTo(25));
            Assert.That(micro.Version, Is.EqualTo(4));
            Assert.That(micro.Width, Is.EqualTo(17));
            Assert.That(micro.Height, Is.EqualTo(17));
            Assert.That(rmqr.Version, Is.Zero);
            Assert.That(rmqr.Width, Is.EqualTo(27));
            Assert.That(rmqr.Height, Is.EqualTo(11));
        });
    }

    [Test]
    public void HonorsNumberedVersionAndDimensionBounds()
    {
        QrEncodeOptions model2Options = ForceFamily(QrSymbolKind.Model2);
        model2Options.MinimumVersion = 10;
        model2Options.MaximumVersion = 12;
        QrCapacityRequest model2 = SymbolParameterSelector.SelectSmallestFitting(
            CreateRequest(QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 1, 4, 0),
            model2Options);

        QrEncodeOptions microOptions = ForceFamily(QrSymbolKind.Micro);
        microOptions.MinimumVersion = 4;
        microOptions.MaximumVersion = 4;
        QrCapacityRequest micro = SymbolParameterSelector.SelectSmallestFitting(
            CreateRequest(QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 1, 4, 0),
            microOptions);

        QrEncodeOptions rmqrOptions = ForceFamily(QrSymbolKind.rMQR);
        rmqrOptions.MinimumWidth = 59;
        rmqrOptions.MaximumHeight = 7;
        QrCapacityRequest rmqr = SymbolParameterSelector.SelectSmallestFitting(
            CreateRequest(QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 1, 4, 0),
            rmqrOptions);

        Assert.Multiple(() =>
        {
            Assert.That(model2.Version, Is.EqualTo(10));
            Assert.That(model2.Width, Is.EqualTo(57));
            Assert.That(micro.Version, Is.EqualTo(4));
            Assert.That(rmqr.Width, Is.EqualTo(59));
            Assert.That(rmqr.Height, Is.EqualTo(7));
        });
    }

    [Test]
    public void ResolvesEqualAreaCandidatesByCompactnessDeterministically()
    {
        QrEncodeOptions options = ForceFamily(QrSymbolKind.rMQR);
        options.MaximumHeight = 9;
        QrCapacityRequest request = CreateRequest(
            QrErrorCorrectionLevel.Medium,
            QrEncodingMode.Byte,
            26,
            208,
            0);

        QrCapacityRequest first = SymbolParameterSelector.SelectSmallestFitting(request, options);
        QrCapacityRequest second = SymbolParameterSelector.SelectSmallestFitting(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Width * first.Height, Is.EqualTo(693));
            Assert.That(first.Width, Is.EqualTo(77));
            Assert.That(first.Height, Is.EqualTo(9));
            Assert.That(second.Width, Is.EqualTo(first.Width));
            Assert.That(second.Height, Is.EqualTo(first.Height));
        });
    }

    [Test]
    public void ControlAndHanziRequirementsExcludeUnsupportedFamilies()
    {
        QrCapacityRequest controlled = SymbolParameterSelector.SelectSmallestFitting(
            CreateRequest(
                QrErrorCorrectionLevel.Medium,
                QrEncodingMode.Numeric,
                1,
                4,
                3),
            new QrEncodeOptions());
        QrCapacityRequest hanzi = SymbolParameterSelector.SelectSmallestFitting(
            CreateRequest(
                QrErrorCorrectionLevel.Medium,
                QrEncodingMode.Hanzi,
                1,
                13,
                0),
            new QrEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(controlled.SymbolKind, Is.EqualTo(QrSymbolKind.rMQR));
            Assert.That(controlled.Width, Is.EqualTo(27));
            Assert.That(controlled.Height, Is.EqualTo(11));
            Assert.That(hanzi.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(hanzi.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReturnsANewRequestWithoutChangingPayloadMetadata()
    {
        QrCapacityRequest source = CreateRequest(
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Alphanumeric,
            2,
            11,
            0);

        QrCapacityRequest selected = SymbolParameterSelector.SelectSmallestFitting(
            source,
            new QrEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.Not.SameAs(source));
            Assert.That(source.Width, Is.Zero);
            Assert.That(source.Height, Is.Zero);
            Assert.That(selected.EncodingMode, Is.EqualTo(source.EncodingMode));
            Assert.That(selected.CharacterCount, Is.EqualTo(source.CharacterCount));
            Assert.That(selected.PayloadBitCount, Is.EqualTo(source.PayloadBitCount));
            Assert.That(selected.ControlBitCount, Is.EqualTo(source.ControlBitCount));
        });
    }

    [Test]
    public void ReportsCapacityWhenNoPermittedCandidateFits()
    {
        QrEncodeOptions options = ForceFamily(QrSymbolKind.Model2);
        options.MaximumVersion = 1;
        QrCapacityRequest request = CreateRequest(
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Byte,
            100,
            800,
            0);

        QrCapacityException exception = Assert.Throws<QrCapacityException>(() =>
            SymbolParameterSelector.SelectSmallestFitting(request, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(exception.RequiredBitCount, Is.EqualTo(812));
            Assert.That(exception.AvailableBitCount, Is.EqualTo(152));
        });
    }

    [Test]
    public void RejectsInvalidRequestsAndContradictoryOptions()
    {
        QrCapacityRequest request = CreateRequest(
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Numeric,
            1,
            4,
            0);
        QrEncodeOptions noFamilies = new QrEncodeOptions
        {
            AllowModel2 = false,
            AllowMicro = false,
            AllowRectangularMicro = false
        };
        QrEncodeOptions forcedDisabled = ForceFamily(QrSymbolKind.Micro);
        forcedDisabled.AllowMicro = false;
        QrEncodeOptions legacy = ForceFamily(QrSymbolKind.LegacyModel1);
        legacy.AllowLegacyModel1 = true;
        QrEncodeOptions versionRange = new QrEncodeOptions
        {
            MinimumVersion = 5,
            MaximumVersion = 4
        };
        QrEncodeOptions widthRange = new QrEncodeOptions
        {
            MinimumWidth = 30,
            MaximumWidth = 20
        };
        QrCapacityRequest wrongPayload = CreateRequest(
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Numeric,
            3,
            9,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(() => SymbolParameterSelector.SelectSmallestFitting(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => SymbolParameterSelector.SelectSmallestFitting(request, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => SymbolParameterSelector.SelectSmallestFitting(request, noFamilies),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => SymbolParameterSelector.SelectSmallestFitting(request, forcedDisabled),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => SymbolParameterSelector.SelectSmallestFitting(request, legacy),
                Throws.TypeOf<QrUnsupportedFeatureException>());
            Assert.That(() => SymbolParameterSelector.SelectSmallestFitting(request, versionRange),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => SymbolParameterSelector.SelectSmallestFitting(request, widthRange),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => SymbolParameterSelector.SelectSmallestFitting(wrongPayload, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static QrCapacityRequest CreateRequest(
        QrErrorCorrectionLevel errorCorrectionLevel,
        QrEncodingMode encodingMode,
        int characterCount,
        int payloadBitCount,
        int controlBitCount)
    {
        return new QrCapacityRequest
        {
            SymbolKind = QrSymbolKind.Model2,
            ErrorCorrectionLevel = errorCorrectionLevel,
            EncodingMode = encodingMode,
            CharacterCount = characterCount,
            PayloadBitCount = payloadBitCount,
            ControlBitCount = controlBitCount
        };
    }

    private static QrEncodeOptions ForceFamily(QrSymbolKind symbolKind)
    {
        return new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = symbolKind
        };
    }
}
