using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class Model2EncoderTests
{
    [Test]
    public void CompletePipelineMatchesKnownVersionOneCodewordsAndMetadata()
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            "01234567",
            -1,
            false,
            1000);
        QrEncodeOptions options = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Zero);

        QrSymbol symbol = Model2Encoder.EncodeSegments(segments, options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(symbol.Version, Is.EqualTo(1));
            Assert.That(symbol.Width, Is.EqualTo(21));
            Assert.That(symbol.Height, Is.EqualTo(21));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
            Assert.That(symbol.MaskPattern, Is.EqualTo(QrMaskPattern.Zero));
            Assert.That(symbol.Codewords, Is.EqualTo(new byte[]
            {
                16, 32, 12, 86, 97, 128, 236, 17, 236, 17,
                236, 17, 236, 17, 236, 17, 236, 17, 236,
                83, 85, 151, 103, 16, 5, 132
            }));
            Assert.That(symbol.DataCodewordCount, Is.EqualTo(19));
            Assert.That(symbol.ErrorCorrectionCodewordCount, Is.EqualTo(7));
            Assert.That(symbol.TotalCodewordCount, Is.EqualTo(26));
            Assert.That(symbol.RemainderBitCount, Is.EqualTo(0));
            Assert.That(symbol.DataBitCapacity, Is.EqualTo(152));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(41));
            Assert.That(symbol.UnusedDataBitCount, Is.EqualTo(111));
            Assert.That(symbol.Matrix, Is.Not.Null);
            Assert.That(symbol.Matrix!.Width, Is.EqualTo(29));
            Assert.That(symbol.Matrix.Height, Is.EqualTo(29));
            Assert.That(symbol.Matrix.QuietZoneLeft, Is.EqualTo(4));
            Assert.That(symbol.Matrix.Roles, Has.None.EqualTo(QrModuleRole.Unknown));
            Assert.That(symbol.Validation, Is.Not.Null);
            Assert.That(symbol.Validation!.IsConformant, Is.True);
            Assert.That(symbol.IsConformant, Is.True);
        });

        QrMatrix core = MatrixTransformer.RemoveQuietZone(symbol.Matrix!);
        BitMatrixBuffer bits = QrMatrixProcessor.ConvertQrMatrix(core);
        MaskEvaluator.ApplyModel2Mask(
            bits,
            FunctionPatternBuilder.CreateModel2ReservedMap(1),
            0);
        Assert.That(
            DataModuleWalker.ExtractModel2Codewords(1, bits),
            Is.EqualTo(symbol.Codewords));
    }

    [Test]
    public void MultiSegmentBitstreamSelectsTheFirstFittingVersion()
    {
        byte[] payload = new byte[18];
        for (int index = 0; index < payload.Length; index++)
        {
            payload[index] = (byte)(index * 13 + 7);
        }

        QrSegment[] segments = new QrSegment[]
        {
            EciEncoder.CreateControlSegment(26),
            SegmentOptimizer.CreateSingleModeByteSegment(payload, 26, 1000)
        };
        QrEncodeOptions options = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Three);
        options.MaximumVersion = 40;

        QrSymbol symbol = Model2Encoder.EncodeSegments(segments, options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Version, Is.EqualTo(2));
            Assert.That(symbol.Width, Is.EqualTo(25));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(168));
            Assert.That(symbol.DataBitCapacity, Is.EqualTo(272));
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void ErrorCorrectionBoostUsesSpareCapacityWithoutIncreasingVersion()
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            "01234567",
            -1,
            false,
            1000);
        QrEncodeOptions boostedOptions = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.One);
        boostedOptions.ForceErrorCorrectionLevel = false;
        boostedOptions.BoostErrorCorrectionLevel = true;
        QrEncodeOptions exactOptions = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.One);

        QrSymbol boosted = Model2Encoder.EncodeSegments(segments, boostedOptions);
        QrSymbol exact = Model2Encoder.EncodeSegments(segments, exactOptions);

        Assert.Multiple(() =>
        {
            Assert.That(boosted.Version, Is.EqualTo(1));
            Assert.That(exact.Version, Is.EqualTo(1));
            Assert.That(boosted.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
            Assert.That(exact.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
            Assert.That(boosted.DataCodewordCount, Is.LessThan(exact.DataCodewordCount));
            Assert.That(boosted.UsedDataBitCount, Is.EqualTo(exact.UsedDataBitCount));
            Assert.That(boosted.IsConformant, Is.True);
            Assert.That(exact.IsConformant, Is.True);
        });
    }

    [TestCase(1, QrErrorCorrectionLevel.Low, QrMaskPattern.Zero)]
    [TestCase(10, QrErrorCorrectionLevel.Quartile, QrMaskPattern.Two)]
    [TestCase(27, QrErrorCorrectionLevel.Low, QrMaskPattern.Four)]
    [TestCase(40, QrErrorCorrectionLevel.High, QrMaskPattern.Seven)]
    public void ForcedBoundaryVersionsCompleteEveryPipelineStage(
        int version,
        QrErrorCorrectionLevel level,
        QrMaskPattern mask)
    {
        QrEncodeOptions options = CreateForcedOptions(version, level, mask);

        QrSymbol symbol = Model2Encoder.EncodeSegments(new QrSegment[0], options);

        int dimension = Model2VersionTable.GetDimension(version);
        Assert.Multiple(() =>
        {
            Assert.That(symbol.Version, Is.EqualTo(version));
            Assert.That(symbol.Width, Is.EqualTo(dimension));
            Assert.That(symbol.Height, Is.EqualTo(dimension));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(level));
            Assert.That(symbol.MaskPattern, Is.EqualTo(mask));
            Assert.That(symbol.Codewords.Length,
                Is.EqualTo(Model2VersionTable.GetTotalCodewordCount(version)));
            Assert.That(symbol.UsedDataBitCount, Is.Zero);
            Assert.That(symbol.Matrix!.Roles, Has.None.EqualTo(QrModuleRole.Unknown));
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void Fnc1HanziAndAutomaticMaskMetadataAreRetained()
    {
        QrSegment[] alpha = SegmentOptimizer.CreateSingleModeTextSegments(
            "A%",
            -1,
            false,
            1000);
        QrSegment[] fnc1Segments = new QrSegment[]
        {
            Fnc1Encoder.CreateFirstPositionControlSegment(),
            alpha[0]
        };
        QrEncodeOptions fnc1Options = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Automatic);
        QrSymbol fnc1 = Model2Encoder.EncodeSegments(fnc1Segments, fnc1Options);

        QrSegment hanziSegment = HanziEncoder.CreateCompatibilitySegment("中", true);
        QrEncodeOptions hanziOptions = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Automatic);
        hanziOptions.EnableHanziCompatibility = true;
        QrSymbol hanzi = Model2Encoder.EncodeSegments(
            new QrSegment[] { hanziSegment },
            hanziOptions);

        Assert.Multiple(() =>
        {
            Assert.That(fnc1.UsedDataBitCount, Is.EqualTo(34));
            Assert.That(fnc1.MaskPattern, Is.InRange(QrMaskPattern.Zero, QrMaskPattern.Seven));
            Assert.That(fnc1.IsConformant, Is.True);
            Assert.That(hanzi.UsesHanziCompatibility, Is.True);
            Assert.That(hanzi.UsedDataBitCount, Is.EqualTo(29));
            Assert.That(hanzi.IsConformant, Is.True);
        });
    }

    [Test]
    public void ReturnedSegmentsAndMatricesDoNotAliasCallerInputsOrOtherResults()
    {
        QrStructuredAppendInfo info = new QrStructuredAppendInfo
        {
            Index = 0,
            Total = 2,
            Parity = 91,
            ApplicationIdentifier = "batch"
        };
        QrSegment structuredAppend = new QrSegment
        {
            Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = info
        };
        QrSegment payload = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 1, 2, 3 },
            -1,
            1000);
        QrSegment[] segments = new QrSegment[] { structuredAppend, payload };
        QrEncodeOptions options = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Five);

        QrSymbol first = Model2Encoder.EncodeSegments(segments, options);
        QrSymbol second = Model2Encoder.EncodeSegments(segments, options);
        payload.Data[0] = 200;
        info.Parity = 7;
        first.Codewords[0] = 255;
        first.Matrix!.Values[0] = QrModuleValue.Unknown;

        Assert.Multiple(() =>
        {
            Assert.That(first.Segments, Is.Not.SameAs(segments));
            Assert.That(first.Segments[1], Is.Not.SameAs(payload));
            Assert.That(first.Segments[1].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(first.Segments[0].StructuredAppend, Is.Not.SameAs(info));
            Assert.That(first.Segments[0].StructuredAppend!.Parity, Is.EqualTo(91));
            Assert.That(second.Codewords[0], Is.Not.EqualTo(255));
            Assert.That(second.Matrix!.Values[0], Is.Not.EqualTo(QrModuleValue.Unknown));
            Assert.That(second.Codewords, Is.Not.SameAs(first.Codewords));
            Assert.That(second.Matrix.Values, Is.Not.SameAs(first.Matrix.Values));
        });
    }

    [Test]
    public void CapacityFailuresReportTheLargestPermittedConfiguration()
    {
        QrSegment segment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[18],
            -1,
            1000);
        QrEncodeOptions options = CreateForcedOptions(
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Zero);

        QrCapacityException exception = Assert.Throws<QrCapacityException>(() =>
            Model2Encoder.EncodeSegments(new QrSegment[] { segment }, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(exception.RequiredBitCount, Is.EqualTo(156));
            Assert.That(exception.AvailableBitCount, Is.EqualTo(152));
        });
    }

    [Test]
    public void InvalidSegmentsOptionsAndResourceLimitsAreRejected()
    {
        QrSegment validSegment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 1 },
            -1,
            1000);
        QrEncodeOptions disabled = new QrEncodeOptions { AllowModel2 = false };
        QrEncodeOptions wrongForcedKind = new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Micro
        };
        QrEncodeOptions badVersionRange = new QrEncodeOptions
        {
            MinimumVersion = 2,
            MaximumVersion = 1
        };
        QrEncodeOptions badDimensions = new QrEncodeOptions
        {
            MinimumWidth = 30,
            MaximumWidth = 20
        };
        QrEncodeOptions noErrorCorrection = new QrEncodeOptions
        {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.None
        };
        QrEncodeOptions badMask = new QrEncodeOptions
        {
            MaskPattern = (QrMaskPattern)8
        };
        QrEncodeOptions badQuietZone = new QrEncodeOptions
        {
            QuietZoneModules = -2
        };
        QrEncodeOptions segmentLimit = new QrEncodeOptions
        {
            MaximumSegments = 1
        };
        QrEncodeOptions byteLimit = new QrEncodeOptions
        {
            MaximumInputBytes = 0
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Model2Encoder.EncodeSegments(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment },
                    null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment },
                    disabled),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment },
                    wrongForcedKind),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment },
                    badVersionRange),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment },
                    badDimensions),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment },
                    noErrorCorrection),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment },
                    badMask),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment },
                    badQuietZone),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment, validSegment },
                    segmentLimit),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(
                () => Model2Encoder.EncodeSegments(
                    new QrSegment[] { validSegment },
                    byteLimit),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static QrEncodeOptions CreateForcedOptions(
        int version,
        QrErrorCorrectionLevel level,
        QrMaskPattern mask)
    {
        return new QrEncodeOptions
        {
            AllowModel2 = true,
            AllowMicro = false,
            AllowRectangularMicro = false,
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Model2,
            MinimumVersion = version,
            MaximumVersion = version,
            ForceErrorCorrectionLevel = true,
            ErrorCorrectionLevel = level,
            BoostErrorCorrectionLevel = false,
            MaskPattern = mask
        };
    }
}
