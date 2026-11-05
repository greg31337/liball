using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class RecoveryBudgetTests
{
    [TestCase(2)]
    [TestCase(int.MaxValue)]
    public void NestedMatrixAndFlipTrialsShareTheLastAttemptsWithoutOverflow(int maximum)
    {
        QrMatrix clean = Fixture();
        QrDecodeOptions options = Model2Options();
        RecoveryBudget budget = new RecoveryBudget(maximum) { AttemptCount = maximum - 2 };
        MatrixRecoveryResult first = MatrixDecodeRecovery.RecoverWithinBudget(clean, options, budget);
        Assert.That(first.Success, Is.True);
        Assert.That(first.AttemptCount, Is.EqualTo(1));
        Assert.That(budget.AttemptCount, Is.EqualTo(maximum - 1));

        QrMatrix damaged = Fixture();
        for (int word = 0; word < 4; word++) Flip(damaged, Cells(word)[0]);
        damaged.Confidence = Enumerable.Repeat((byte)255, damaged.Values.Length).ToArray();
        damaged.Confidence[Cells(0)[0]] = 153;
        MatrixRecoveryResult repaired = new MatrixRecoveryResult();
        Assert.That(UncertainModuleRecovery.RecoverWithinBudget(damaged, QrSymbolKind.Model2, 1, options, repaired, budget), Is.True);
        Assert.That(repaired.AttemptCount, Is.EqualTo(1));
        Assert.That(MatrixDecodeRecovery.CreateDecodedResult(repaired, options).Text, Is.EqualTo("123"));
        Assert.That(budget.AttemptCount, Is.EqualTo(maximum));

        MatrixRecoveryResult stopped = MatrixDecodeRecovery.RecoverWithinBudget(clean, options, budget);
        Assert.That(stopped.Success, Is.False);
        Assert.That(stopped.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(stopped.AttemptCount, Is.Zero);
        Assert.That(stopped.Codewords, Is.Null);
        Assert.That(stopped.Matrix, Is.Null);
        Assert.That(budget.AttemptCount, Is.EqualTo(maximum));
        Assert.That(options.MaximumRecoveryAttempts, Is.EqualTo(64));
    }

    [TestCase(1, QrFailureReason.ResourceLimit)]
    [TestCase(3, QrFailureReason.ResourceLimit)]
    [TestCase(9, QrFailureReason.NotFound)]
    [TestCase(int.MaxValue, QrFailureReason.NotFound)]
    public void BlankImageRetriesStopAtTheBudgetOrTheFiniteSearchBoundary(int maximum, QrFailureReason reason)
    {
        var (image, detection) = ImageFixture();
        Array.Fill(image.Pixels, (byte)255);
        QrDetectionOptions sampling = new QrDetectionOptions
        {
            BinarizationMode = QrBinarizationMode.SuppliedBinary, RefineGrid = false,
            MaximumCandidates = int.MaxValue
        };
        QrDecodeOptions options = new QrDecodeOptions { MaximumRecoveryAttempts = maximum };
        ImageRecoveryResult first = ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, options);
        ImageRecoveryResult repeat = ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, options);
        Assert.That(first.Success, Is.False);
        Assert.That(first.FailureReason, Is.EqualTo(reason));
        Assert.That(first.AttemptCount, Is.EqualTo(Math.Min(maximum, 9)));
        Assert.That(first.SampleCount, Is.EqualTo(first.AttemptCount));
        Assert.That(first.Recovery, Is.Null);
        Assert.That(first.Detection, Is.Null);
        Assert.That(repeat.FailureReason, Is.EqualTo(first.FailureReason));
        Assert.That(repeat.AttemptCount, Is.EqualTo(first.AttemptCount));
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => ImageDecodeRecovery.Decode(image, detection, null, sampling, options))!;
        Assert.That(error.FailureReason, Is.EqualTo(reason));
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(first.AttemptCount));
        Assert.That(options.MaximumRecoveryAttempts, Is.EqualTo(maximum));
    }

    [TestCase(1)]
    [TestCase(4)]
    [TestCase(32)]
    public void AdversarialUnknownPayloadCannotResetTheRetryBudget(int maximum)
    {
        QrMatrix matrix = Fixture();
        QrModuleRole[] roles = ModuleClassifier.CreateModel2RoleMap(1, 0);
        for (int index = 0; index < roles.Length; index++)
            if (roles[index] == QrModuleRole.Data || roles[index] == QrModuleRole.ErrorCorrection)
                matrix.Values[index] = QrModuleValue.Unknown;
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        QrDecodeOptions options = Model2Options();
        options.MaximumRecoveryAttempts = maximum;
        MatrixRecoveryResult first = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        MatrixRecoveryResult repeat = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        Assert.That(first.Success, Is.False);
        Assert.That(first.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(first.AttemptCount, Is.EqualTo(maximum));
        Assert.That(repeat.FailureReason, Is.EqualTo(first.FailureReason));
        Assert.That(repeat.AttemptCount, Is.EqualTo(first.AttemptCount));
        Assert.That(first.Codewords, Is.Null);
        Assert.That(first.Matrix, Is.Null);
        Assert.That(first.FlippedModuleIndices, Is.Empty);
        Assert.That(matrix.Values, Is.EqualTo(before));
        options.MaximumRecoveryAttempts = int.MaxValue;
        MatrixRecoveryResult complete = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        Assert.That(complete.Success, Is.False);
        Assert.That(complete.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        // Four orientations, each with a default plus two metadata choices
        // for the baseline and at most 32 uncertain-value assignments.
        Assert.That(complete.AttemptCount, Is.InRange(33, 4 * 3 * 33));
    }

    [Test]
    public void FailedFlipTrialsRetainRsFailureAfterRecoveringFormatEvidence()
    {
        byte[] words = CodewordErasureRecoveryTests.Words(QrSymbolKind.Micro, 2, 0);
        for (int word = 0; word < 9; word++) words[word] ^= 0xa5;
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(words, QrSymbolKind.Micro, 2, 0);
        for (int bit = 0; bit < 7; bit++)
            matrix.Values[MicroPlacementTable.GetFormatVerticalRow(bit) * matrix.Width + MicroPlacementTable.GetFormatVerticalColumn(bit)] = QrModuleValue.Unknown;
        QrDecodeOptions options = new QrDecodeOptions
        {
            AllowModel2 = false, AllowRectangularMicro = false, AllowLegacyModel1 = false,
            TryMirrored = false, TryInverted = false, MaximumRecoveryAttempts = 128
        };
        Assert.That(MatrixCodewordReader.Read(matrix, QrSymbolKind.Micro, options).FailureReason, Is.EqualTo(QrFailureReason.InvalidFormat));
        MatrixRecoveryResult result = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(result.Codewords, Is.Null);
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(MatrixTransformer.Rotate(matrix, 2), options).FailureReason,
            Is.EqualTo(result.FailureReason));
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => MatrixDecodeRecovery.Decode(matrix, options))!;
        Assert.That(error.FailureReason, Is.EqualTo(result.FailureReason));
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(result.AttemptCount));
    }

    static QrMatrix Fixture() => CodewordErasureRecoveryTests.Build(
        CodewordErasureRecoveryTests.Words(QrSymbolKind.Model2, 1, 0), QrSymbolKind.Model2, 1, 0);
    static int[] Cells(int word) => CodewordErasureRecoveryTests.Cells(QrSymbolKind.Model2, 1, 0, word);
    static void Flip(QrMatrix matrix, int cell) => matrix.Values[cell] = matrix.Values[cell] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
    static QrDecodeOptions Model2Options() => new QrDecodeOptions
    {
        AllowMicro = false, AllowRectangularMicro = false, AllowLegacyModel1 = false,
        TryMirrored = false, TryInverted = false
    };

    static (QrImage, QrDetection) ImageFixture()
    {
        QrSymbol symbol = new QrEncoder().EncodeText("123", new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, QuietZoneModules = 0
        });
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = 4, QuietZoneModules = 4 });
        int end = 16 + 4 * symbol.Matrix!.Width;
        return (new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels },
            new QrDetection
            {
                EstimatedWidth = symbol.Matrix.Width, EstimatedHeight = symbol.Matrix.Height,
                HasSymbolKindEstimate = true, EstimatedSymbolKind = QrSymbolKind.Model2, EstimatedVersion = 1,
                ModuleSize = 4, Polarity = QrPolarity.DarkOnLight,
                Corners = new QrQuadrilateral
                {
                    TopLeft = new QrPoint { X = 16, Y = 16 }, TopRight = new QrPoint { X = end, Y = 16 },
                    BottomRight = new QrPoint { X = end, Y = end }, BottomLeft = new QrPoint { X = 16, Y = end }
                }
            });
    }
}
