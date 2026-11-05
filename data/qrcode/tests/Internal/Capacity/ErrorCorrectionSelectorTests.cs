using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ErrorCorrectionSelectorTests
{
    [Test]
    public void AutomaticSelectionUsesTheRequestedMinimumAndBoostsInPlace()
    {
        QrEncodeOptions options = new QrEncodeOptions
        {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            ForceErrorCorrectionLevel = false,
            BoostErrorCorrectionLevel = true
        };

        QrCapacityRequest selected = ErrorCorrectionSelector.SelectParameters(
            CreateRequest(QrErrorCorrectionLevel.High, QrEncodingMode.Numeric, 1, 4),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(selected.SymbolKind, Is.EqualTo(QrSymbolKind.Micro));
            Assert.That(selected.Version, Is.EqualTo(2));
            Assert.That(selected.Width, Is.EqualTo(13));
            Assert.That(selected.Height, Is.EqualTo(13));
            Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Medium));
        });
    }

    [Test]
    public void ForcedErrorCorrectionRemainsExactEvenWhenBoostingIsEnabled()
    {
        QrEncodeOptions options = new QrEncodeOptions
        {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            ForceErrorCorrectionLevel = true,
            BoostErrorCorrectionLevel = true
        };

        QrCapacityRequest selected = ErrorCorrectionSelector.SelectParameters(
            CreateRequest(QrErrorCorrectionLevel.High, QrEncodingMode.Numeric, 1, 4),
            options);

        Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
    }

    [Test]
    public void DisabledBoostingKeepsTheAutomaticMinimumLevel()
    {
        QrEncodeOptions options = new QrEncodeOptions
        {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            ForceErrorCorrectionLevel = false,
            BoostErrorCorrectionLevel = false
        };

        QrCapacityRequest selected = ErrorCorrectionSelector.SelectParameters(
            CreateRequest(QrErrorCorrectionLevel.High, QrEncodingMode.Numeric, 1, 4),
            options);

        Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
    }

    [Test]
    public void Model2BoostsToTheStrongestLevelThatFits()
    {
        QrEncodeOptions options = ForceFamily(QrSymbolKind.Model2);
        options.ErrorCorrectionLevel = QrErrorCorrectionLevel.Low;

        QrCapacityRequest selected = ErrorCorrectionSelector.SelectParameters(
            CreateRequest(QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 1, 4),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(selected.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(selected.Version, Is.EqualTo(1));
            Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
        });
    }

    [Test]
    public void BoostingNeverEnlargesTheSelectedSymbol()
    {
        QrCapacityRequest request = CreateRequest(
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Byte,
            17,
            136);
        QrEncodeOptions boostedOptions = ForceFamily(QrSymbolKind.Model2);
        boostedOptions.ErrorCorrectionLevel = QrErrorCorrectionLevel.Low;
        QrCapacityRequest boosted = ErrorCorrectionSelector.SelectParameters(
            request,
            boostedOptions);

        QrEncodeOptions forcedHighOptions = ForceFamily(QrSymbolKind.Model2);
        forcedHighOptions.ErrorCorrectionLevel = QrErrorCorrectionLevel.High;
        forcedHighOptions.ForceErrorCorrectionLevel = true;
        QrCapacityRequest forcedHigh = ErrorCorrectionSelector.SelectParameters(
            request,
            forcedHighOptions);

        Assert.Multiple(() =>
        {
            Assert.That(boosted.Version, Is.EqualTo(1));
            Assert.That(boosted.Width, Is.EqualTo(21));
            Assert.That(boosted.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
            Assert.That(forcedHigh.Version, Is.EqualTo(3));
            Assert.That(forcedHigh.Width, Is.EqualTo(29));
            Assert.That(forcedHigh.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
        });
    }

    [Test]
    public void BoostsToEachProfilesStrongestSupportedLevel()
    {
        QrEncodeOptions microOptions = ForceFamily(QrSymbolKind.Micro);
        microOptions.MinimumVersion = 4;
        microOptions.MaximumVersion = 4;
        microOptions.ErrorCorrectionLevel = QrErrorCorrectionLevel.Low;
        QrCapacityRequest micro = ErrorCorrectionSelector.SelectParameters(
            CreateRequest(QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 21, 70),
            microOptions);

        QrEncodeOptions rmqrOptions = ForceFamily(QrSymbolKind.rMQR);
        rmqrOptions.ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium;
        QrCapacityRequest rmqr = ErrorCorrectionSelector.SelectParameters(
            CreateRequest(QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 1, 4),
            rmqrOptions);

        QrEncodeOptions m1Options = ForceFamily(QrSymbolKind.Micro);
        m1Options.MinimumVersion = 1;
        m1Options.MaximumVersion = 1;
        m1Options.ErrorCorrectionLevel = QrErrorCorrectionLevel.None;
        QrCapacityRequest m1 = ErrorCorrectionSelector.SelectParameters(
            CreateRequest(QrErrorCorrectionLevel.None, QrEncodingMode.Numeric, 1, 4),
            m1Options);

        Assert.Multiple(() =>
        {
            Assert.That(micro.Version, Is.EqualTo(4));
            Assert.That(micro.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Quartile));
            Assert.That(rmqr.Width, Is.EqualTo(27));
            Assert.That(rmqr.Height, Is.EqualTo(11));
            Assert.That(rmqr.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
            Assert.That(m1.Version, Is.EqualTo(1));
            Assert.That(m1.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.None));
        });
    }

    [Test]
    public void ReturnsACopyAndLeavesTheCallerRequestUnchanged()
    {
        QrCapacityRequest request = CreateRequest(
            QrErrorCorrectionLevel.High,
            QrEncodingMode.Alphanumeric,
            2,
            11);
        QrEncodeOptions options = new QrEncodeOptions
        {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            BoostErrorCorrectionLevel = false
        };

        QrCapacityRequest selected = ErrorCorrectionSelector.SelectParameters(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.Not.SameAs(request));
            Assert.That(request.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
            Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
            Assert.That(selected.EncodingMode, Is.EqualTo(request.EncodingMode));
            Assert.That(selected.PayloadBitCount, Is.EqualTo(request.PayloadBitCount));
        });
    }

    [Test]
    public void RejectsUnsupportedFixedLevelsAndInvalidInputs()
    {
        QrCapacityRequest request = CreateRequest(
            QrErrorCorrectionLevel.Low,
            QrEncodingMode.Numeric,
            1,
            4);
        QrEncodeOptions microHigh = ForceFamily(QrSymbolKind.Micro);
        microHigh.ErrorCorrectionLevel = QrErrorCorrectionLevel.High;
        microHigh.ForceErrorCorrectionLevel = true;
        QrEncodeOptions rmqrLow = ForceFamily(QrSymbolKind.rMQR);
        rmqrLow.ErrorCorrectionLevel = QrErrorCorrectionLevel.Low;
        rmqrLow.ForceErrorCorrectionLevel = true;
        QrEncodeOptions invalid = new QrEncodeOptions
        {
            ErrorCorrectionLevel = (QrErrorCorrectionLevel)99
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => ErrorCorrectionSelector.SelectParameters(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ErrorCorrectionSelector.SelectParameters(request, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ErrorCorrectionSelector.SelectParameters(request, microHigh),
                Throws.TypeOf<QrCapacityException>());
            Assert.That(() => ErrorCorrectionSelector.SelectParameters(request, rmqrLow),
                Throws.TypeOf<QrCapacityException>());
            Assert.That(() => ErrorCorrectionSelector.SelectParameters(request, invalid),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static QrCapacityRequest CreateRequest(
        QrErrorCorrectionLevel errorCorrectionLevel,
        QrEncodingMode encodingMode,
        int characterCount,
        int payloadBitCount)
    {
        return new QrCapacityRequest
        {
            SymbolKind = QrSymbolKind.Model2,
            ErrorCorrectionLevel = errorCorrectionLevel,
            EncodingMode = encodingMode,
            CharacterCount = characterCount,
            PayloadBitCount = payloadBitCount
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
