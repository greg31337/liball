using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class RectangularMicroEncoderTests
{
    [Test]
    public void MediumPipelineMatchesR7x43GoldenVector()
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            "12345",
            -1,
            false,
            1000);
        QrEncodeOptions options = CreateForcedSizeOptions(
            7,
            43,
            QrErrorCorrectionLevel.Medium,
            QrMaskPattern.Four);

        QrSymbol symbol = RectangularMicroEncoder.EncodeSegments(segments, options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.rMQR));
            Assert.That(symbol.Version, Is.EqualTo(0));
            Assert.That(symbol.Width, Is.EqualTo(43));
            Assert.That(symbol.Height, Is.EqualTo(7));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Medium));
            Assert.That(symbol.MaskPattern, Is.EqualTo(QrMaskPattern.Four));
            Assert.That(symbol.Codewords, Is.EqualTo(new byte[]
            {
                42, 61, 173, 0, 236, 17, 64, 56, 41, 63, 145, 161, 25
            }));
            Assert.That(symbol.DataCodewordCount, Is.EqualTo(6));
            Assert.That(symbol.ErrorCorrectionCodewordCount, Is.EqualTo(7));
            Assert.That(symbol.TotalCodewordCount, Is.EqualTo(13));
            Assert.That(symbol.RemainderBitCount, Is.Zero);
            Assert.That(symbol.DataBitCapacity, Is.EqualTo(48));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(24));
            Assert.That(symbol.UnusedDataBitCount, Is.EqualTo(24));
            Assert.That(symbol.Matrix, Is.Not.Null);
            Assert.That(symbol.Matrix!.Width, Is.EqualTo(47));
            Assert.That(symbol.Matrix.Height, Is.EqualTo(11));
            Assert.That(symbol.Matrix.QuietZoneLeft, Is.EqualTo(2));
            Assert.That(symbol.Matrix.Roles, Has.None.EqualTo(QrModuleRole.Unknown));
            Assert.That(symbol.IsConformant, Is.True);
            Assert.That(symbol.Validation!.IsConformant, Is.True);
        });
        AssertExtractedCodewords(symbol);
    }

    [Test]
    public void HighPipelineMatchesR7x59GoldenVector()
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            "12345",
            -1,
            false,
            1000);
        QrEncodeOptions options = CreateForcedSizeOptions(
            7,
            59,
            QrErrorCorrectionLevel.High,
            QrMaskPattern.Automatic);

        QrSymbol symbol = RectangularMicroEncoder.EncodeSegments(segments, options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Version, Is.EqualTo(1));
            Assert.That(symbol.Width, Is.EqualTo(59));
            Assert.That(symbol.Height, Is.EqualTo(7));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
            Assert.That(symbol.MaskPattern, Is.EqualTo(QrMaskPattern.Four));
            Assert.That(symbol.Codewords, Is.EqualTo(new byte[]
            {
                37, 30, 214, 128, 236, 17, 236,
                187, 107, 88, 12, 250, 32, 254, 191, 159, 101, 18, 69, 187, 117
            }));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(25));
            Assert.That(symbol.DataBitCapacity, Is.EqualTo(56));
            Assert.That(symbol.IsConformant, Is.True);
        });
        AssertExtractedCodewords(symbol);
    }

    [Test]
    public void EveryStandardizedSizeAndEccOptionCompletesThePipeline()
    {
        int completed = 0;
        for (int version = RectangularMicroVersionTable.MinimumVersionIndex;
            version <= RectangularMicroVersionTable.MaximumVersionIndex;
            version++)
        {
            int width = RectangularMicroVersionTable.GetWidth(version);
            int height = RectangularMicroVersionTable.GetHeight(version);
            for (int level = RectangularMicroVersionTable.Medium;
                level <= RectangularMicroVersionTable.High;
                level++)
            {
                QrErrorCorrectionLevel publicLevel = level == RectangularMicroVersionTable.Medium
                    ? QrErrorCorrectionLevel.Medium
                    : QrErrorCorrectionLevel.High;
                QrEncodeOptions options = CreateForcedSizeOptions(
                    height,
                    width,
                    publicLevel,
                    QrMaskPattern.Four);

                QrSymbol symbol = RectangularMicroEncoder.EncodeSegments(
                    new QrSegment[0],
                    options);

                Assert.Multiple(() =>
                {
                    Assert.That(symbol.Version, Is.EqualTo(version));
                    Assert.That(symbol.Width, Is.EqualTo(width));
                    Assert.That(symbol.Height, Is.EqualTo(height));
                    Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(publicLevel));
                    Assert.That(symbol.MaskPattern, Is.EqualTo(QrMaskPattern.Four));
                    Assert.That(symbol.Codewords.Length,
                        Is.EqualTo(RectangularMicroVersionTable.GetTotalCodewordCount(version)));
                    Assert.That(symbol.DataCodewordCount,
                        Is.EqualTo(RectangularMicroVersionTable.GetDataCodewordCount(
                            version,
                            level)));
                    Assert.That(symbol.UsedDataBitCount, Is.Zero);
                    Assert.That(symbol.IsConformant, Is.True);
                });
                completed++;
            }
        }

        Assert.That(completed, Is.EqualTo(64));
    }

    [Test]
    public void AutomaticSelectionUsesAreaAndCompactnessOrdering()
    {
        QrEncodeOptions tinyOptions = CreateRangeOptions(QrErrorCorrectionLevel.Medium);
        QrSymbol tiny = RectangularMicroEncoder.EncodeSegments(
            SegmentOptimizer.CreateSingleModeTextSegments("1", -1, false, 1000),
            tinyOptions);

        QrSegment byteSegment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[26],
            -1,
            1000);
        QrEncodeOptions tieOptions = CreateRangeOptions(QrErrorCorrectionLevel.Medium);
        tieOptions.MaximumHeight = 9;
        QrSymbol compact = RectangularMicroEncoder.EncodeSegments(
            new QrSegment[] { byteSegment },
            tieOptions);

        Assert.Multiple(() =>
        {
            Assert.That(tiny.Width, Is.EqualTo(27));
            Assert.That(tiny.Height, Is.EqualTo(11));
            Assert.That(tiny.Version, Is.EqualTo(10));
            Assert.That(compact.Width, Is.EqualTo(77));
            Assert.That(compact.Height, Is.EqualTo(9));
            Assert.That(compact.Version, Is.EqualTo(7));
        });
    }

    [Test]
    public void ErrorCorrectionBoostDoesNotChangeTheSelectedRectangle()
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            "1",
            -1,
            false,
            1000);
        QrEncodeOptions boostedOptions = CreateRangeOptions(QrErrorCorrectionLevel.Medium);
        boostedOptions.ForceErrorCorrectionLevel = false;
        boostedOptions.BoostErrorCorrectionLevel = true;
        QrEncodeOptions exactOptions = CreateRangeOptions(QrErrorCorrectionLevel.Medium);

        QrSymbol boosted = RectangularMicroEncoder.EncodeSegments(segments, boostedOptions);
        QrSymbol exact = RectangularMicroEncoder.EncodeSegments(segments, exactOptions);

        Assert.Multiple(() =>
        {
            Assert.That(boosted.Version, Is.EqualTo(exact.Version));
            Assert.That(boosted.Width, Is.EqualTo(exact.Width));
            Assert.That(boosted.Height, Is.EqualTo(exact.Height));
            Assert.That(boosted.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.High));
            Assert.That(exact.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Medium));
            Assert.That(boosted.UsedDataBitCount, Is.EqualTo(exact.UsedDataBitCount));
            Assert.That(boosted.IsConformant, Is.True);
        });
    }

    [Test]
    public void EciFnc1AndMultipleDataSegmentsUseExactBitAccounting()
    {
        QrSegment[] alpha = SegmentOptimizer.CreateSingleModeTextSegments(
            "A%",
            -1,
            false,
            1000);
        QrSegment byteSegment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 9 },
            26,
            1000);
        QrSegment[] segments = new QrSegment[]
        {
            Fnc1Encoder.CreateFirstPositionControlSegment(),
            EciEncoder.CreateControlSegment(26),
            alpha[0],
            byteSegment
        };
        QrEncodeOptions options = CreateForcedSizeOptions(
            11,
            43,
            QrErrorCorrectionLevel.Medium,
            QrMaskPattern.Four);

        QrSymbol symbol = RectangularMicroEncoder.EncodeSegments(segments, options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Version, Is.EqualTo(11));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(55));
            Assert.That(symbol.IsConformant, Is.True);
            Assert.That(symbol.Segments.Length, Is.EqualTo(4));
        });
    }

    [Test]
    public void ReturnedSegmentsAndMatricesDoNotAliasInputsOrOtherResults()
    {
        QrSegment segment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 1, 2 },
            -1,
            1000);
        QrSegment[] segments = new QrSegment[] { segment };
        QrEncodeOptions options = CreateForcedSizeOptions(
            11,
            43,
            QrErrorCorrectionLevel.Medium,
            QrMaskPattern.Four);

        QrSymbol first = RectangularMicroEncoder.EncodeSegments(segments, options);
        QrSymbol second = RectangularMicroEncoder.EncodeSegments(segments, options);
        segment.Data[0] = 99;
        first.Codewords[0] = 255;
        first.Matrix!.Values[0] = QrModuleValue.Unknown;

        Assert.Multiple(() =>
        {
            Assert.That(first.Segments, Is.Not.SameAs(segments));
            Assert.That(first.Segments[0], Is.Not.SameAs(segment));
            Assert.That(first.Segments[0].Data, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(second.Codewords[0], Is.Not.EqualTo(255));
            Assert.That(second.Matrix!.Values[0], Is.Not.EqualTo(QrModuleValue.Unknown));
            Assert.That(second.Codewords, Is.Not.SameAs(first.Codewords));
            Assert.That(second.Matrix.Values, Is.Not.SameAs(first.Matrix.Values));
        });
    }

    [Test]
    public void CapacityFailureReportsTheForcedRectanglesLimit()
    {
        QrSegment segment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 1, 2, 3 },
            -1,
            1000);
        QrEncodeOptions options = CreateForcedSizeOptions(
            7,
            43,
            QrErrorCorrectionLevel.High,
            QrMaskPattern.Four);

        QrCapacityException exception = Assert.Throws<QrCapacityException>(() =>
            RectangularMicroEncoder.EncodeSegments(
                new QrSegment[] { segment },
                options))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.SymbolKind, Is.EqualTo(QrSymbolKind.rMQR));
            Assert.That(exception.RequiredBitCount, Is.EqualTo(30));
            Assert.That(exception.AvailableBitCount, Is.EqualTo(24));
        });
    }

    [Test]
    public void UnsupportedLevelsMasksControlsAndOptionsAreRejected()
    {
        QrSegment numeric = SegmentOptimizer.CreateSingleModeTextSegments(
            "1",
            -1,
            false,
            1000)[0];
        QrEncodeOptions low = CreateRangeOptions(QrErrorCorrectionLevel.Low);
        QrEncodeOptions disabled = new QrEncodeOptions
        {
            AllowRectangularMicro = false,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium
        };
        QrEncodeOptions wrongKind = new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Model2,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium
        };
        QrEncodeOptions badMask = CreateRangeOptions(QrErrorCorrectionLevel.Medium);
        badMask.MaskPattern = QrMaskPattern.Three;
        QrEncodeOptions impossibleDimensions = CreateRangeOptions(
            QrErrorCorrectionLevel.Medium);
        impossibleDimensions.MaximumWidth = 20;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RectangularMicroEncoder.EncodeSegments(
                    null!,
                    CreateRangeOptions(QrErrorCorrectionLevel.Medium)),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RectangularMicroEncoder.EncodeSegments(
                    new QrSegment[] { numeric },
                    null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RectangularMicroEncoder.EncodeSegments(
                    new QrSegment[] { numeric },
                    low),
                Throws.TypeOf<QrCapacityException>());
            Assert.That(
                () => RectangularMicroEncoder.EncodeSegments(
                    new QrSegment[] { numeric },
                    disabled),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RectangularMicroEncoder.EncodeSegments(
                    new QrSegment[] { numeric },
                    wrongKind),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RectangularMicroEncoder.EncodeSegments(
                    new QrSegment[] { numeric },
                    badMask),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RectangularMicroEncoder.EncodeSegments(
                    new QrSegment[] { numeric },
                    impossibleDimensions),
                Throws.TypeOf<QrCapacityException>());
            Assert.That(
                () => RectangularMicroEncoder.EncodeSegments(
                    new QrSegment[]
                    {
                        new QrSegment
                        {
                            Mode = QrEncodingMode.StructuredAppend,
                            StructuredAppend = new QrStructuredAppendInfo
                            {
                                Index = 0,
                                Total = 2,
                                Parity = 0
                            }
                        },
                        numeric
                    },
                    CreateRangeOptions(QrErrorCorrectionLevel.Medium)),
                Throws.TypeOf<QrInputException>());
        });
    }

    static QrEncodeOptions CreateForcedSizeOptions(
        int height,
        int width,
        QrErrorCorrectionLevel level,
        QrMaskPattern mask)
    {
        return new QrEncodeOptions
        {
            AllowModel2 = false,
            AllowMicro = false,
            AllowRectangularMicro = true,
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.rMQR,
            MinimumWidth = width,
            MaximumWidth = width,
            MinimumHeight = height,
            MaximumHeight = height,
            ForceErrorCorrectionLevel = true,
            ErrorCorrectionLevel = level,
            BoostErrorCorrectionLevel = false,
            MaskPattern = mask
        };
    }

    static QrEncodeOptions CreateRangeOptions(QrErrorCorrectionLevel level)
    {
        return new QrEncodeOptions
        {
            AllowModel2 = false,
            AllowMicro = false,
            AllowRectangularMicro = true,
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.rMQR,
            ForceErrorCorrectionLevel = true,
            ErrorCorrectionLevel = level,
            BoostErrorCorrectionLevel = false,
            MaskPattern = QrMaskPattern.Automatic
        };
    }

    static void AssertExtractedCodewords(QrSymbol symbol)
    {
        QrMatrix core = MatrixTransformer.RemoveQuietZone(symbol.Matrix!);
        BitMatrixBuffer bits = QrMatrixProcessor.ConvertQrMatrix(core);
        MaskEvaluator.ApplyRectangularMicroMask(
            bits,
            FunctionPatternBuilder.CreateRectangularMicroReservedMap(symbol.Version));
        byte[] extracted = DataModuleWalker.ExtractRectangularMicroCodewords(
            symbol.Version,
            bits);
        Assert.That(extracted, Is.EqualTo(symbol.Codewords));
    }
}
