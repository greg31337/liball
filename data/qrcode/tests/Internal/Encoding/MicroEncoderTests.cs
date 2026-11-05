using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MicroEncoderTests
{
    [Test]
    public void M1NumericPipelineMatchesDetectionOnlyGoldenVector()
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            "12345",
            -1,
            false,
            1000);
        QrEncodeOptions options = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.None,
            QrMaskPattern.Zero);

        QrSymbol symbol = MicroEncoder.EncodeSegments(segments, options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.Micro));
            Assert.That(symbol.Version, Is.EqualTo(1));
            Assert.That(symbol.Width, Is.EqualTo(11));
            Assert.That(symbol.Height, Is.EqualTo(11));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.None));
            Assert.That(symbol.MaskPattern, Is.EqualTo(QrMaskPattern.Zero));
            Assert.That(symbol.Codewords, Is.EqualTo(new byte[] { 163, 218, 208, 110, 199 }));
            Assert.That(symbol.DataCodewordCount, Is.EqualTo(3));
            Assert.That(symbol.ErrorCorrectionCodewordCount, Is.EqualTo(2));
            Assert.That(symbol.TotalCodewordCount, Is.EqualTo(5));
            Assert.That(symbol.DataBitCapacity, Is.EqualTo(20));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(20));
            Assert.That(symbol.UnusedDataBitCount, Is.Zero);
            Assert.That(symbol.Matrix, Is.Not.Null);
            Assert.That(symbol.Matrix!.Width, Is.EqualTo(15));
            Assert.That(symbol.Matrix.Height, Is.EqualTo(15));
            Assert.That(symbol.Matrix.QuietZoneLeft, Is.EqualTo(2));
            Assert.That(symbol.Matrix.Roles, Has.None.EqualTo(QrModuleRole.Unknown));
            Assert.That(symbol.IsConformant, Is.True);
            Assert.That(symbol.Validation!.IsConformant, Is.True);
        });

        AssertExtractedCodewords(symbol);
    }

    [TestCase(2, QrErrorCorrectionLevel.Low, QrMaskPattern.One, "HELLO")]
    [TestCase(3, QrErrorCorrectionLevel.Low, QrMaskPattern.Two, "micro3")]
    [TestCase(4, QrErrorCorrectionLevel.Low, QrMaskPattern.Three, "MICRO FOUR")]
    public void M2ThroughM4PipelinesMatchGoldenCodewords(
        int version,
        QrErrorCorrectionLevel level,
        QrMaskPattern mask,
        string text)
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            text,
            -1,
            false,
            1000);
        QrEncodeOptions options = CreateForcedOptions(version, level, mask);

        QrSymbol symbol = MicroEncoder.EncodeSegments(segments, options);

        byte[] expected = GetGoldenCodewords(version);
        Assert.Multiple(() =>
        {
            Assert.That(symbol.Version, Is.EqualTo(version));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(level));
            Assert.That(symbol.MaskPattern, Is.EqualTo(mask));
            Assert.That(symbol.Codewords, Is.EqualTo(expected));
            Assert.That(symbol.Width, Is.EqualTo(MicroVersionTable.GetDimension(version)));
            Assert.That(symbol.TotalCodewordCount,
                Is.EqualTo(MicroVersionTable.GetSymbolCharacterCount(version)));
            Assert.That(symbol.IsConformant, Is.True);
        });
        AssertExtractedCodewords(symbol);
    }

    [TestCase(1, QrErrorCorrectionLevel.None)]
    [TestCase(2, QrErrorCorrectionLevel.Low)]
    [TestCase(2, QrErrorCorrectionLevel.Medium)]
    [TestCase(3, QrErrorCorrectionLevel.Low)]
    [TestCase(3, QrErrorCorrectionLevel.Medium)]
    [TestCase(4, QrErrorCorrectionLevel.Low)]
    [TestCase(4, QrErrorCorrectionLevel.Medium)]
    [TestCase(4, QrErrorCorrectionLevel.Quartile)]
    public void EveryStandardMicroEccConfigurationCompletesThePipeline(
        int version,
        QrErrorCorrectionLevel level)
    {
        QrEncodeOptions options = CreateForcedOptions(
            version,
            level,
            QrMaskPattern.Automatic);

        QrSymbol symbol = MicroEncoder.EncodeSegments(new QrSegment[0], options);

        int errorCorrectionIndex = (int)level;
        Assert.Multiple(() =>
        {
            Assert.That(symbol.Version, Is.EqualTo(version));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(level));
            Assert.That(symbol.MaskPattern, Is.InRange(QrMaskPattern.Zero, QrMaskPattern.Three));
            Assert.That(symbol.DataCodewordCount,
                Is.EqualTo(MicroVersionTable.GetDataSymbolCharacterCount(
                    version,
                    errorCorrectionIndex)));
            Assert.That(symbol.ErrorCorrectionCodewordCount,
                Is.EqualTo(MicroVersionTable.GetEccCodewordCount(
                    version,
                    errorCorrectionIndex)));
            Assert.That(symbol.DataBitCapacity,
                Is.EqualTo(MicroVersionTable.GetDataBitCapacity(
                    version,
                    errorCorrectionIndex)));
            Assert.That(symbol.UsedDataBitCount, Is.Zero);
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void AutomaticSelectionHonorsProgressiveModeAndEccRestrictions()
    {
        QrEncodeOptions detectionOnly = CreateRangeOptions(QrErrorCorrectionLevel.None);
        QrSymbol numeric = MicroEncoder.EncodeSegments(
            SegmentOptimizer.CreateSingleModeTextSegments("1", -1, false, 1000),
            detectionOnly);

        QrEncodeOptions low = CreateRangeOptions(QrErrorCorrectionLevel.Low);
        QrSymbol alphanumeric = MicroEncoder.EncodeSegments(
            SegmentOptimizer.CreateSingleModeTextSegments("A", -1, false, 1000),
            low);
        QrSymbol bytes = MicroEncoder.EncodeSegments(
            new QrSegment[]
            {
                SegmentOptimizer.CreateSingleModeByteSegment(
                    new byte[] { 65 },
                    -1,
                    1000)
            },
            low);

        QrEncodeOptions quartile = CreateRangeOptions(QrErrorCorrectionLevel.Quartile);
        QrSymbol quartileSymbol = MicroEncoder.EncodeSegments(
            SegmentOptimizer.CreateSingleModeTextSegments("1", -1, false, 1000),
            quartile);

        Assert.Multiple(() =>
        {
            Assert.That(numeric.Version, Is.EqualTo(1));
            Assert.That(numeric.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.None));
            Assert.That(alphanumeric.Version, Is.EqualTo(2));
            Assert.That(bytes.Version, Is.EqualTo(3));
            Assert.That(quartileSymbol.Version, Is.EqualTo(4));
        });
    }

    [Test]
    public void ErrorCorrectionBoostStaysWithinTheSelectedMicroVersion()
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            "1",
            -1,
            false,
            1000);
        QrEncodeOptions boostedOptions = CreateRangeOptions(QrErrorCorrectionLevel.Low);
        boostedOptions.ForceErrorCorrectionLevel = false;
        boostedOptions.BoostErrorCorrectionLevel = true;
        QrEncodeOptions exactOptions = CreateRangeOptions(QrErrorCorrectionLevel.Low);

        QrSymbol boosted = MicroEncoder.EncodeSegments(segments, boostedOptions);
        QrSymbol exact = MicroEncoder.EncodeSegments(segments, exactOptions);

        Assert.Multiple(() =>
        {
            Assert.That(boosted.Version, Is.EqualTo(2));
            Assert.That(exact.Version, Is.EqualTo(2));
            Assert.That(boosted.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Medium));
            Assert.That(exact.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
            Assert.That(boosted.UsedDataBitCount, Is.EqualTo(exact.UsedDataBitCount));
            Assert.That(boosted.IsConformant, Is.True);
        });
    }

    [Test]
    public void MultipleSupportedSegmentsAndReturnedDataAreIndependent()
    {
        QrSegment firstSegment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 1 },
            -1,
            1000);
        QrSegment secondSegment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 2 },
            -1,
            1000);
        QrSegment[] segments = new QrSegment[] { firstSegment, secondSegment };
        QrEncodeOptions options = CreateForcedOptions(
            3,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Zero);

        QrSymbol first = MicroEncoder.EncodeSegments(segments, options);
        QrSymbol second = MicroEncoder.EncodeSegments(segments, options);
        firstSegment.Data[0] = 99;
        first.Codewords[0] = 255;
        first.Matrix!.Values[0] = QrModuleValue.Unknown;

        Assert.Multiple(() =>
        {
            Assert.That(first.Segments, Is.Not.SameAs(segments));
            Assert.That(first.Segments[0], Is.Not.SameAs(firstSegment));
            Assert.That(first.Segments[0].Data, Is.EqualTo(new byte[] { 1 }));
            Assert.That(second.Codewords[0], Is.Not.EqualTo(255));
            Assert.That(second.Matrix!.Values[0], Is.Not.EqualTo(QrModuleValue.Unknown));
            Assert.That(second.Codewords, Is.Not.SameAs(first.Codewords));
            Assert.That(second.Matrix.Values, Is.Not.SameAs(first.Matrix.Values));
        });
    }

    [Test]
    public void M1CapacityFailureReportsItsExactTwentyBitLimit()
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            "123456",
            -1,
            false,
            1000);
        QrEncodeOptions options = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.None,
            QrMaskPattern.Zero);

        QrCapacityException exception = Assert.Throws<QrCapacityException>(() =>
            MicroEncoder.EncodeSegments(segments, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.SymbolKind, Is.EqualTo(QrSymbolKind.Micro));
            Assert.That(exception.RequiredBitCount, Is.EqualTo(23));
            Assert.That(exception.AvailableBitCount, Is.EqualTo(20));
        });
    }

    [Test]
    public void UnsupportedVersionModesLevelsControlsAndOptionsAreRejected()
    {
        QrSegment numeric = SegmentOptimizer.CreateSingleModeTextSegments(
            "1",
            -1,
            false,
            1000)[0];
        QrSegment bytes = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 1 },
            -1,
            1000);
        QrEncodeOptions m1 = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.None,
            QrMaskPattern.Zero);
        QrEncodeOptions m1Low = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Zero);
        QrEncodeOptions high = CreateRangeOptions(QrErrorCorrectionLevel.High);
        QrEncodeOptions disabled = new QrEncodeOptions { AllowMicro = false };
        QrEncodeOptions wrongKind = new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Model2
        };
        QrEncodeOptions badMask = new QrEncodeOptions
        {
            MaskPattern = QrMaskPattern.Four
        };
        QrEncodeOptions invalidLevel = new QrEncodeOptions
        {
            ErrorCorrectionLevel = (QrErrorCorrectionLevel)99
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => MicroEncoder.EncodeSegments(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MicroEncoder.EncodeSegments(new QrSegment[] { numeric }, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MicroEncoder.EncodeSegments(new QrSegment[] { bytes }, m1),
                Throws.TypeOf<QrCapacityException>());
            Assert.That(
                () => MicroEncoder.EncodeSegments(new QrSegment[] { numeric }, m1Low),
                Throws.TypeOf<QrCapacityException>());
            Assert.That(
                () => MicroEncoder.EncodeSegments(new QrSegment[] { numeric }, high),
                Throws.TypeOf<QrCapacityException>());
            Assert.That(
                () => MicroEncoder.EncodeSegments(
                    new QrSegment[] { EciEncoder.CreateControlSegment(26), bytes },
                    CreateRangeOptions(QrErrorCorrectionLevel.Low)),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MicroEncoder.EncodeSegments(new QrSegment[] { numeric }, disabled),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MicroEncoder.EncodeSegments(new QrSegment[] { numeric }, wrongKind),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MicroEncoder.EncodeSegments(new QrSegment[] { numeric }, badMask),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MicroEncoder.EncodeSegments(new QrSegment[] { numeric }, invalidLevel),
                Throws.TypeOf<QrInputException>());
        });
    }

    static QrEncodeOptions CreateForcedOptions(
        int version,
        QrErrorCorrectionLevel level,
        QrMaskPattern mask)
    {
        return new QrEncodeOptions
        {
            AllowModel2 = false,
            AllowMicro = true,
            AllowRectangularMicro = false,
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Micro,
            MinimumVersion = version,
            MaximumVersion = version,
            ForceErrorCorrectionLevel = true,
            ErrorCorrectionLevel = level,
            BoostErrorCorrectionLevel = false,
            MaskPattern = mask
        };
    }

    static QrEncodeOptions CreateRangeOptions(QrErrorCorrectionLevel level)
    {
        QrEncodeOptions options = CreateForcedOptions(
            1,
            level,
            QrMaskPattern.Automatic);
        options.MaximumVersion = 4;
        return options;
    }

    static byte[] GetGoldenCodewords(int version)
    {
        if (version == 2)
        {
            return new byte[] { 214, 22, 241, 152, 0, 162, 189, 75, 152, 101 };
        }

        if (version == 3)
        {
            return new byte[]
            {
                153, 181, 165, 141, 201, 188, 204, 0, 236, 17, 0,
                132, 160, 211, 197, 214, 164
            };
        }

        return new byte[]
        {
            42, 126, 8, 222, 46, 43, 186, 194, 0, 236, 17, 236,
            17, 236, 17, 236, 56, 146, 53, 119, 43, 252, 247, 38
        };
    }

    static void AssertExtractedCodewords(QrSymbol symbol)
    {
        QrMatrix core = MatrixTransformer.RemoveQuietZone(symbol.Matrix!);
        BitMatrixBuffer bits = QrMatrixProcessor.ConvertQrMatrix(core);
        MaskEvaluator.ApplyMicroMask(
            bits,
            FunctionPatternBuilder.CreateMicroReservedMap(symbol.Version),
            (int)symbol.MaskPattern);
        byte[] extracted = DataModuleWalker.ExtractMicroCodewords(
            symbol.Version,
            (int)symbol.ErrorCorrectionLevel,
            bits);
        Assert.That(extracted, Is.EqualTo(symbol.Codewords));
    }
}
