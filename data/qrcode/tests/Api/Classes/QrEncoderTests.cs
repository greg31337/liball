using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrEncoderTests
{
    [Test]
    public void EncodesTextThroughPublicContractWithExactForcedParameters()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = CreateModel2Options();

        QrSymbol symbol = encoder.EncodeText("01234567", options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(symbol.Version, Is.EqualTo(1));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
            Assert.That(symbol.MaskPattern, Is.EqualTo(QrMaskPattern.Zero));
            Assert.That(symbol.Segments, Has.Length.EqualTo(1));
            Assert.That(symbol.Segments[0].Mode, Is.EqualTo(QrEncodingMode.Numeric));
            Assert.That(symbol.Codewords, Is.EqualTo(new byte[]
            {
                16, 32, 12, 86, 97, 128, 236, 17, 236, 17,
                236, 17, 236, 17, 236, 17, 236, 17, 236,
                83, 85, 151, 103, 16, 5, 132
            }));
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void AutomaticSelectionChoosesTheSmallestPermittedFamily()
    {
        IQrEncoder encoder = new QrEncoder();

        QrSymbol symbol = encoder.EncodeText("12345", new QrEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.Micro));
            Assert.That(symbol.Version, Is.EqualTo(2));
            Assert.That(symbol.Width, Is.EqualTo(13));
            Assert.That(symbol.Height, Is.EqualTo(13));
            Assert.That(symbol.Segments[0].Mode, Is.EqualTo(QrEncodingMode.Numeric));
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void AutomaticSelectionCanChooseRectangularMicroForHeightBound()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = new QrEncodeOptions
        {
            MaximumHeight = 9,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium
        };

        QrSymbol symbol = encoder.EncodeText("12345", options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.rMQR));
            Assert.That(symbol.Height, Is.LessThanOrEqualTo(9));
            Assert.That(symbol.Width, Is.GreaterThan(symbol.Height));
            Assert.That(symbol.MaskPattern, Is.EqualTo(QrMaskPattern.Four));
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void OptimizedTextUsesMultiplePayloadModesWhenSmaller()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = CreateModel2Options();

        QrSymbol symbol = encoder.EncodeText("1234567890a1234567890", options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Segments, Has.Length.EqualTo(3));
            Assert.That(symbol.Segments[0].Mode, Is.EqualTo(QrEncodingMode.Numeric));
            Assert.That(symbol.Segments[1].Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(symbol.Segments[2].Mode, Is.EqualTo(QrEncodingMode.Numeric));
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void EncodesBytesWithRequestedFnc1AndEciControls()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = CreateModel2Options();
        options.EciAssignment = QrEciAssignments.Binary;
        options.Fnc1SecondApplicationIndicator = 42;

        QrSymbol symbol = encoder.EncodeBytes(new byte[] { 1, 2, 3 }, options);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Segments, Has.Length.EqualTo(3));
            Assert.That(symbol.Segments[0].Mode, Is.EqualTo(QrEncodingMode.Fnc1Second));
            Assert.That(symbol.Segments[0].Fnc1ApplicationIndicator, Is.EqualTo(42));
            Assert.That(symbol.Segments[1].Mode, Is.EqualTo(QrEncodingMode.Eci));
            Assert.That(symbol.Segments[1].EciAssignment,
                Is.EqualTo(QrEciAssignments.Binary));
            Assert.That(symbol.Segments[2].Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(symbol.Segments[2].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(symbol.Segments[2].EciAssignment,
                Is.EqualTo(QrEciAssignments.Binary));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(68));
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void EncodesExplicitSegmentsWithoutChangingCallerData()
    {
        IQrEncoder encoder = new QrEncoder();
        byte[] data = new byte[] { 9, 8, 7 };
        QrSegment segment = new QrSegment
        {
            Mode = QrEncodingMode.Byte,
            CharacterCount = data.Length,
            Data = data
        };
        QrEncodeOptions options = new QrEncodeOptions
        {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium,
            MaskPattern = QrMaskPattern.Automatic
        };

        QrSymbol symbol = encoder.EncodeSegments(new QrSegment[] { segment }, options);
        data[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.Micro));
            Assert.That(symbol.Segments[0].Data, Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(symbol.Segments[0].Data, Is.Not.SameAs(data));
            Assert.That(symbol.IsConformant, Is.True);
        });
    }

    [Test]
    public void EncodesAutomaticAndManualStructuredAppendSequences()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = CreateModel2Options();
        QrSymbolSequence automatic = encoder.EncodeStructuredAppend(
            new byte[] { 1, 2, 3, 4 },
            options);
        QrSegment[][] parts = new QrSegment[][]
        {
            CreateManualPart(0, 3, "A"),
            CreateManualPart(1, 3, "B")
        };
        QrSymbolSequence manual = encoder.EncodeManualStructuredAppend(parts, options);

        Assert.Multiple(() =>
        {
            Assert.That(automatic.Symbols, Has.Length.EqualTo(2));
            Assert.That(automatic.RawBytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(automatic.IsComplete, Is.True);
            Assert.That(manual.Symbols, Has.Length.EqualTo(2));
            Assert.That(manual.RawBytes, Is.EqualTo(new byte[] { 65, 66 }));
            Assert.That(manual.Text, Is.EqualTo("AB"));
            Assert.That(manual.StructuredAppend!.Parity, Is.EqualTo(3));
            Assert.That(manual.IsComplete, Is.True);
        });
    }

    [Test]
    public void RejectsInvalidConfigurationInputAndCapacityThroughPublicMethods()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions noFamilies = new QrEncodeOptions
        {
            AllowModel2 = false,
            AllowMicro = false,
            AllowRectangularMicro = false
        };
        QrEncodeOptions legacy = new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.LegacyModel1,
            AllowLegacyModel1 = true
        };
        QrEncodeOptions tiny = CreateModel2Options();
        byte[] oversized = new byte[18];

        Assert.Multiple(() =>
        {
            Assert.That(() => encoder.EncodeText(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => encoder.EncodeBytes(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => encoder.EncodeSegments(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => encoder.EncodeText("A", null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => encoder.EncodeText("A", noFamilies),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => encoder.EncodeText("A", legacy),
                Throws.TypeOf<QrUnsupportedFeatureException>());
            Assert.That(() => encoder.EncodeBytes(oversized, tiny),
                Throws.TypeOf<QrCapacityException>());
        });
    }

    [Test]
    public void TryEncodeMethodsReturnSuccessfulSymbolsAndMatrices()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeAttemptResult textAttempt = encoder.TryEncodeText(
            "01234567",
            CreateModel2Options());
        QrSegment segment = new QrSegment
        {
            Mode = QrEncodingMode.Byte,
            CharacterCount = 3,
            Data = new byte[] { 1, 2, 3 }
        };
        QrEncodeAttemptResult segmentAttempt = encoder.TryEncodeSegments(
            new QrSegment[] { segment },
            CreateModel2Options());

        Assert.Multiple(() =>
        {
            Assert.That(textAttempt.Success, Is.True);
            Assert.That(textAttempt.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(textAttempt.Symbol, Is.Not.Null);
            Assert.That(textAttempt.Matrix, Is.SameAs(textAttempt.Symbol!.Matrix));
            Assert.That(textAttempt.RequiredBitCount,
                Is.EqualTo(textAttempt.Symbol.UsedDataBitCount));
            Assert.That(textAttempt.AvailableBitCount,
                Is.EqualTo(textAttempt.Symbol.DataBitCapacity));
            Assert.That(textAttempt.Sequence, Is.Null);
            Assert.That(textAttempt.SequenceMatrices, Is.Empty);
            Assert.That(segmentAttempt.Success, Is.True);
            Assert.That(segmentAttempt.Symbol, Is.Not.Null);
            Assert.That(segmentAttempt.Symbol!.Segments[0].Data,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void TryEncodeSegmentsReturnsExactCapacityFailureWithoutThrowing()
    {
        IQrEncoder encoder = new QrEncoder();
        byte[] data = new byte[18];
        QrSegment segment = new QrSegment
        {
            Mode = QrEncodingMode.Byte,
            CharacterCount = data.Length,
            Data = data
        };

        QrEncodeAttemptResult attempt = encoder.TryEncodeSegments(
            new QrSegment[] { segment },
            CreateModel2Options());

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.False);
            Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.CapacityExceeded));
            Assert.That(attempt.Symbol, Is.Null);
            Assert.That(attempt.Sequence, Is.Null);
            Assert.That(attempt.Matrix, Is.Null);
            Assert.That(attempt.SequenceMatrices, Is.Empty);
            Assert.That(attempt.RequiredBitCount, Is.EqualTo(156));
            Assert.That(attempt.AvailableBitCount, Is.EqualTo(152));
        });
    }

    [Test]
    public void TryEncodeTextReportsUnsupportedTranscodingAndMicroRepresentation()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions unsupportedEci = CreateModel2Options();
        unsupportedEci.EciAssignment = QrEciAssignments.Binary;
        QrEncodeOptions micro = new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Micro,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low
        };

        QrEncodeAttemptResult eciAttempt = encoder.TryEncodeText("é", unsupportedEci);
        QrEncodeAttemptResult nonByteAttempt = encoder.TryEncodeText("12345", unsupportedEci);
        QrEncodeAttemptResult microAttempt = encoder.TryEncodeText("😀", micro);

        Assert.Multiple(() =>
        {
            Assert.That(eciAttempt.Success, Is.False);
            Assert.That(eciAttempt.FailureReason,
                Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
            Assert.That(eciAttempt.Symbol, Is.Null);
            Assert.That(nonByteAttempt.Success, Is.True);
            Assert.That(nonByteAttempt.Symbol!.Segments[0].Mode,
                Is.EqualTo(QrEncodingMode.Numeric));
            Assert.That(microAttempt.Success, Is.False);
            Assert.That(microAttempt.FailureReason,
                Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
            Assert.That(microAttempt.Symbol, Is.Null);
        });
    }

    [Test]
    public void TryEncodeMethodsStillThrowForInvalidProgrammerInputAndConfiguration()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions noFamilies = new QrEncodeOptions
        {
            AllowModel2 = false,
            AllowMicro = false,
            AllowRectangularMicro = false
        };
        QrSegment malformed = new QrSegment
        {
            Mode = QrEncodingMode.Byte,
            CharacterCount = 2,
            Data = new byte[] { 1 }
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => encoder.TryEncodeText(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => encoder.TryEncodeText("A", null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => encoder.TryEncodeText("A", noFamilies),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => encoder.TryEncodeSegments(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => encoder.TryEncodeSegments(
                    new QrSegment[] { malformed },
                    new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static QrEncodeOptions CreateModel2Options()
    {
        return new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Model2,
            MinimumVersion = 1,
            MaximumVersion = 1,
            ForceErrorCorrectionLevel = true,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            BoostErrorCorrectionLevel = false,
            MaskPattern = QrMaskPattern.Zero
        };
    }

    private static QrSegment[] CreateManualPart(int index, int parity, string text)
    {
        byte[] data = global::System.Text.Encoding.UTF8.GetBytes(text);
        return new QrSegment[]
        {
            new QrSegment
            {
                Mode = QrEncodingMode.StructuredAppend,
                StructuredAppend = new QrStructuredAppendInfo
                {
                    Index = index,
                    Total = 2,
                    Parity = parity,
                    ApplicationIdentifier = "public-api"
                }
            },
            new QrSegment
            {
                Mode = QrEncodingMode.Byte,
                CharacterCount = data.Length,
                Data = data,
                Text = text
            }
        };
    }
}
