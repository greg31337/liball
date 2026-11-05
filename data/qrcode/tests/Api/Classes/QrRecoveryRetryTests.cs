using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrRecoveryRetryTests
{
    [Test]
    public void ImageStrategiesRespectOrderAndTheExactSharedBudget(
        [Values("clean", "left", "right", "up", "down", "local", "lower", "higher", "refine", "alignment")] string scenario,
        [Values(false, true)] bool multiPoint)
    {
        Scene scene = CreateScene(scenario);
        scene.Sampling.UseMultiPointSampling = multiPoint;
        QrDecodeOptions options = new() { MaximumRecoveryAttempts = 512 };
        string before = Snapshot(scene);
        ImageRecoveryResult complete = Recover(scene, options);
        Assert.That(complete.Success, Is.True, $"{scenario}: {complete.FailureReason}; samples {complete.SampleCount}; attempts {complete.AttemptCount}");
        AssertStrategy(complete, scenario);
        int required = complete.AttemptCount;
        Assert.That(required, Is.InRange(1, 512));
        foreach (int maximum in new[] { 1, Math.Max(1, required - 1), required, required + 1, int.MaxValue }.Distinct())
        {
            options.MaximumRecoveryAttempts = maximum;
            string optionBefore = Snapshot(options);
            ImageRecoveryResult result = Recover(scene, options);
            Assert.That(result.Success, Is.EqualTo(maximum >= required), $"{scenario}: budget {maximum}, required {required}");
            Assert.That(result.AttemptCount, Is.EqualTo(Math.Min(maximum, required)));
            Assert.That(result.SampleCount, Is.LessThanOrEqualTo(complete.SampleCount));
            IQrDecoder decoder = new QrDecoder();
            QrAttemptResult publicResult = decoder.TryDecodeDetection(scene.Image, scene.Detection, scene.Sampling, options);
            Assert.That(publicResult.Success, Is.EqualTo(result.Success));
            Assert.That(publicResult.AttemptCount, Is.EqualTo(result.AttemptCount));
            if (result.Success)
            {
                AssertStrategy(result, scenario);
                Assert.That(result.SampleCount, Is.EqualTo(complete.SampleCount));
                Assert.That(result.AlignmentCandidateIndex, Is.EqualTo(complete.AlignmentCandidateIndex));
                Assert.That(Snapshot(result.Detection!), Is.EqualTo(Snapshot(complete.Detection!)));
                Assert.That(result.Recovery!.Codewords!.DataCodewords, Is.EqualTo(complete.Recovery!.Codewords!.DataCodewords));
                Assert.That(Snapshot(result.Recovery.Matrix!), Is.EqualTo(Snapshot(complete.Recovery.Matrix!)));
                Assert.That(publicResult.Text, Is.EqualTo("12345"));
                Assert.That(publicResult.Result!.Symbol!.Codewords, Is.EqualTo(scene.Symbol.Codewords));
                Assert.That(publicResult.Result.Detection!.CandidateIndex, Is.EqualTo(19));
            }
            else
            {
                Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
                Assert.That(result.Recovery, Is.Null);
                Assert.That(result.Detection, Is.Null);
                AssertEmpty(publicResult);
                QrDecodeException error = Assert.Throws<QrDecodeException>(() => decoder.DecodeDetection(scene.Image, scene.Detection, scene.Sampling, options))!;
                Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
                Assert.That(error.RecoveryAttemptCount, Is.EqualTo(maximum));
                Assert.That(error.CandidateIndex, Is.EqualTo(19));
            }
            Assert.That(Snapshot(options), Is.EqualTo(optionBefore));
        }
        options.EnableRecovery = false;
        ImageRecoveryResult disabled = Recover(scene, options);
        Assert.That(disabled.SampleCount, Is.EqualTo(1));
        Assert.That(disabled.Success, Is.EqualTo(scenario == "clean"));
        Assert.That(Snapshot(scene), Is.EqualTo(before));
    }

    [Test]
    public void CorrectPolarityAndMirrorChoicesDoNotRequireSpeculativeValueRetries(
        [Values(QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR)] QrSymbolKind kind,
        [Values(0, 1, 2, 3)] int rotation, [Values(false, true)] bool mirror, [Values(false, true)] bool invert)
    {
        QrSymbol symbol = Encode(kind, kind == QrSymbolKind.Model2 ? 1 : 0);
        QrMatrix matrix = MatrixTransformer.Rotate(symbol.Matrix!, rotation);
        if (mirror) matrix = MatrixTransformer.Mirror(matrix);
        if (invert) matrix = MatrixTransformer.InvertPolarity(matrix);
        string before = Snapshot(matrix);
        QrDecodeOptions options = Only(kind);
        options.MaximumRecoveryAttempts = 512;
        MatrixRecoveryResult complete = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        Assert.That(complete.Success, Is.True);
        Assert.That(complete.IsMirrored, Is.EqualTo(mirror));
        Assert.That(complete.IsInverted, Is.EqualTo(invert));
        Assert.That(complete.FlippedModuleIndices, Is.Empty);
        Assert.That(complete.UsedAlternateInformation, Is.False);
        options.EnableRecovery = false;
        options.MaximumRecoveryAttempts = complete.AttemptCount;
        MatrixRecoveryResult ordinary = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        Assert.That(ordinary.Success, Is.True, "Ordinary orientation discovery precedes uncertain-value search.");
        // Alternate metadata can consume attempts on incorrect orientations.
        Assert.That(ordinary.AttemptCount, Is.LessThanOrEqualTo(complete.AttemptCount));
        Assert.That(ordinary.QuarterTurnsClockwise, Is.EqualTo((4 - rotation) % 4));
        Assert.That(ordinary.IsMirrored, Is.EqualTo(mirror));
        Assert.That(ordinary.IsInverted, Is.EqualTo(invert));
        Assert.That(new QrDecoder().DecodeMatrix(matrix, options).Text, Is.EqualTo("12345"));
        foreach (bool recovery in new[] { false, true })
        {
            options.EnableRecovery = recovery;
            int required = recovery ? complete.AttemptCount : ordinary.AttemptCount;
            foreach (int maximum in new[] { Math.Max(1, required - 1), required, required + 1 })
            {
                options.MaximumRecoveryAttempts = maximum;
                QrAttemptResult result = new QrDecoder().TryDecodeMatrix(matrix, options);
                Assert.That(result.Success, Is.EqualTo(maximum >= required));
                Assert.That(result.AttemptCount, Is.EqualTo(Math.Min(maximum, required)));
                if (result.Success) Assert.That(result.Text, Is.EqualTo("12345"));
                else
                {
                    AssertEmpty(result);
                    Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
                }
            }
        }
        options.MaximumRecoveryAttempts = 512;
        options.TryMirrored = !mirror;
        options.TryInverted = !invert;
        if (mirror || invert) AssertEmpty(new QrDecoder().TryDecodeMatrix(matrix, options));
        Assert.That(Snapshot(matrix), Is.EqualTo(before));
    }

    [Test]
    public void TentativeSingleAndPairRepairsArePublishedOnlyAfterTheirSearchFinishes(
        [Values(1, 2)] int neededFlips, [Values(false, true)] bool reverseConfidence)
    {
        QrMatrix matrix = MatrixFixture();
        int[] corrupt = Enumerable.Range(0, 3 + neededFlips).Select(Cell).ToArray();
        foreach (int cell in corrupt) Flip(matrix, cell);
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        int[] suspects = [corrupt[0], neededFlips == 2 ? corrupt[1] : Cell(8)];
        matrix.Confidence[suspects[0]] = reverseConfidence ? (byte)200 : (byte)128;
        matrix.Confidence[suspects[1]] = reverseConfidence ? (byte)128 : (byte)200;
        string before = Snapshot(matrix);
        QrDecodeOptions options = Only(QrSymbolKind.Model2);
        options.MaximumRecoveryAttempts = 512;
        MatrixRecoveryResult complete = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        Assert.That(complete.Success, Is.True);
        Assert.That(complete.FlippedModuleIndices, Has.Length.EqualTo(neededFlips));
        Assert.That(complete.FlippedModuleIndices, Is.EquivalentTo(corrupt.Take(neededFlips)));
        for (int maximum = 1; maximum <= complete.AttemptCount + 1; maximum++)
        {
            options.MaximumRecoveryAttempts = maximum;
            QrAttemptResult result = new QrDecoder().TryDecodeMatrix(matrix, options);
            Assert.That(result.Success, Is.EqualTo(maximum >= complete.AttemptCount));
            Assert.That(result.AttemptCount, Is.EqualTo(Math.Min(maximum, complete.AttemptCount)));
            if (result.Success) Assert.That(result.Text, Is.EqualTo("123"));
            else
            {
                Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
                AssertEmpty(result);
            }
        }
        Assert.That(Snapshot(matrix), Is.EqualTo(before));
    }

    [Test]
    public void UncorrectableInputsTerminateAtTheBudgetOrFiniteSearchBoundary(
        [Values(QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR, QrSymbolKind.LegacyModel1)] QrSymbolKind kind,
        [Values("unknown", "corrupt", "format")] string damage)
    {
        int version = kind == QrSymbolKind.rMQR ? 0 : kind == QrSymbolKind.Micro ? 2 : 1;
        byte[] words = CodewordErasureRecoveryTests.Words(kind, version, 0);
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(words, kind, version, 0);
        for (int word = 0; word < words.Length; word++)
            foreach (int cell in CodewordErasureRecoveryTests.Cells(kind, version, 0, word))
            {
                if (damage == "unknown") matrix.Values[cell] = QrModuleValue.Unknown;
                else Flip(matrix, cell);
            }
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        foreach (int cell in Enumerable.Range(0, 4).Select(w => CodewordErasureRecoveryTests.Cells(kind, version, 0, w)[0]))
            matrix.Confidence[cell] = 128;
        if (damage == "format")
        {
            QrModuleRole[] roles = ModuleClassifier.CreateEncodingRoleMap(kind == QrSymbolKind.LegacyModel1 ? QrSymbolKind.Model2 : kind, version, 0);
            for (int i = 0; i < roles.Length; i++) if (roles[i] == QrModuleRole.Format) matrix.Values[i] = QrModuleValue.Unknown;
        }
        string before = Snapshot(matrix);
        QrDecodeOptions options = Only(kind);
        options.MaximumRecoveryAttempts = int.MaxValue;
        QrAttemptResult exhaustedSearch = new QrDecoder().TryDecodeMatrix(matrix, options);
        AssertEmpty(exhaustedSearch);
        Assert.That(exhaustedSearch.FailureReason, Is.Not.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(exhaustedSearch.AttemptCount, Is.InRange(1, 2000));
        foreach (int maximum in new[] { 1, 2, 7, 31, exhaustedSearch.AttemptCount, exhaustedSearch.AttemptCount + 1 })
        {
            options.MaximumRecoveryAttempts = maximum;
            QrAttemptResult result = new QrDecoder().TryDecodeMatrix(matrix, options);
            AssertEmpty(result);
            Assert.That(result.AttemptCount, Is.EqualTo(Math.Min(maximum, exhaustedSearch.AttemptCount)));
            Assert.That(Snapshot(new QrDecoder().TryDecodeMatrix(matrix, options)), Is.EqualTo(Snapshot(result)));
            if (maximum < exhaustedSearch.AttemptCount) Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        }
        Assert.That(Snapshot(matrix), Is.EqualTo(before));
    }

    [Test]
    public void BlankImagesCountAllThresholdAndOffsetTrialsWithoutResettingBudget(
        [Values(false, true)] bool binary, [Values(false, true)] bool refine)
    {
        Scene scene = CreateScene("clean");
        Array.Fill(scene.Image.Pixels, (byte)255);
        scene.Sampling.BinarizationMode = binary ? QrBinarizationMode.SuppliedBinary : QrBinarizationMode.GlobalHistogram;
        scene.Sampling.RefineGrid = refine;
        QrDecodeOptions options = new() { MaximumRecoveryAttempts = int.MaxValue };
        ImageRecoveryResult complete = Recover(scene, options);
        Assert.That(complete.Success, Is.False);
        int expectedSamples = (binary ? 1 : 4) * (refine ? 10 : 9);
        Assert.That(complete.SampleCount, Is.EqualTo(expectedSamples));
        Assert.That(complete.AttemptCount, Is.EqualTo(expectedSamples));
        foreach (int maximum in new[] { 1, 4, 8, expectedSamples - 1, expectedSamples, expectedSamples + 1 })
        {
            options.MaximumRecoveryAttempts = maximum;
            ImageRecoveryResult result = Recover(scene, options);
            Assert.That(result.Success, Is.False);
            Assert.That(result.AttemptCount, Is.EqualTo(Math.Min(maximum, expectedSamples)));
            Assert.That(result.SampleCount, Is.EqualTo(result.AttemptCount));
            Assert.That(result.Recovery, Is.Null);
            Assert.That(result.Detection, Is.Null);
            if (maximum < expectedSamples) Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        }
    }

    static void AssertStrategy(ImageRecoveryResult result, string scenario)
    {
        if (scenario == "clean") Assert.That(result.SampleCount, Is.EqualTo(1));
        if (scenario == "local" || scenario == "lower" || scenario == "higher")
        {
            int threshold = scenario == "local" ? 1 : scenario == "lower" ? 2 : 3;
            Assert.That(result.ThresholdCandidateIndex, Is.EqualTo(threshold));
            Assert.That(result.SampleCount, Is.EqualTo(threshold + 1));
            Assert.That(result.SamplingOffsetX, Is.Zero);
            Assert.That(result.SamplingOffsetY, Is.Zero);
        }
        string[] shifts = ["left", "right", "up", "down"];
        int shift = Array.IndexOf(shifts, scenario);
        if (shift >= 0)
        {
            Assert.That(result.SampleCount, Is.EqualTo(shift + 2));
            Assert.That(result.SamplingOffsetX, Is.EqualTo(shift < 2 ? shift == 0 ? -0.25 : 0.25 : 0));
            Assert.That(result.SamplingOffsetY, Is.EqualTo(shift >= 2 ? shift == 2 ? -0.25 : 0.25 : 0));
        }
        Assert.That(result.UsedRefinement, Is.EqualTo(scenario == "refine"));
        if (scenario == "alignment") Assert.That(result.AlignmentCandidateIndex, Is.GreaterThanOrEqualTo(0));
        else Assert.That(result.AlignmentCandidateIndex, Is.EqualTo(-1));
    }

    static Scene CreateScene(string scenario)
    {
        bool threshold = scenario == "lower" || scenario == "higher";
        QrSymbol symbol = Encode(threshold ? QrSymbolKind.Micro : QrSymbolKind.Model2, scenario == "alignment" ? 7 : 0);
        int scale = threshold ? 1 : 6;
        int border = scale * 4;
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = scale, QuietZoneModules = 4,
            DarkValue = threshold ? (byte)100 : (byte)0, LightValue = threshold ? (byte)140 : (byte)255 });
        QrMatrix matrix = symbol.Matrix!;
        Scene scene = new()
        {
            Symbol = symbol,
            Image = new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels },
            Sampling = new QrDetectionOptions { BinarizationMode = threshold || scenario == "local" ? QrBinarizationMode.GlobalHistogram : QrBinarizationMode.SuppliedBinary,
                RefineGrid = scenario == "refine" || scenario == "alignment" },
            Detection = new QrDetection { EstimatedWidth = matrix.Width, EstimatedHeight = matrix.Height, EstimatedVersion = symbol.Version,
                HasSymbolKindEstimate = true, EstimatedSymbolKind = symbol.SymbolKind, Polarity = QrPolarity.DarkOnLight, ModuleSize = scale,
                CandidateIndex = 19, Diagnostics = ["caller"], SampledMatrix = new QrMatrix { Values = [QrModuleValue.Unknown] },
                Corners = Corners(new PerspectiveTransform { M00 = scale, M11 = scale, M02 = border, M12 = border, M22 = 1 }, matrix.Width, matrix.Height) }
        };
        if (scenario == "local")
            for (int y = 0; y < raster.Height; y++)
                for (int x = 0; x < raster.Width; x++)
                    raster.Pixels[y * raster.Width + x] = (byte)(20 + x * 160 / raster.Width + (raster.Pixels[y * raster.Width + x] == 0 ? 0 : 60));
        if (threshold)
            for (int i = 0; i < matrix.Values.Length; i++)
                if ((matrix.Roles![i] == QrModuleRole.Data || matrix.Roles[i] == QrModuleRole.ErrorCorrection)
                    && matrix.Values[i] == (scenario == "higher" ? QrModuleValue.Dark : QrModuleValue.Light))
                    raster.Pixels[(i / matrix.Width + 4) * raster.Width + i % matrix.Width + 4] = scenario == "higher" ? (byte)130 : (byte)110;
        foreach (QrPoint p in Points(scene.Detection.Corners!))
        {
            if (scenario == "left") p.X += 3.6;
            if (scenario == "right") p.X -= 3.6;
            if (scenario == "up") p.Y += 3.6;
            if (scenario == "down") p.Y -= 3.6;
            if (scenario == "refine") p.X += 5;
        }
        if (scenario == "alignment")
        {
            int size = matrix.Width;
            QrQuadrilateral logical = new()
            {
                TopLeft = new QrPoint { X = 3.5, Y = 3.5 }, TopRight = new QrPoint { X = size - 3.5, Y = 3.5 },
                BottomRight = new QrPoint { X = size - 3.5, Y = size - 3.5 }, BottomLeft = new QrPoint { X = 3.5, Y = size - 3.5 }
            };
            QrPoint[] observed = Points(logical).Select(p => new QrPoint { X = border + scale * p.X, Y = border + scale * p.Y }).ToArray();
            observed[2].X += 14; observed[2].Y -= 10;
            scene.Detection.Corners = Corners(PerspectiveTransform.Create(logical, new QrQuadrilateral
                { TopLeft = observed[0], TopRight = observed[1], BottomRight = observed[2], BottomLeft = observed[3] }), size, size);
        }
        return scene;
    }

    sealed class Scene
    {
        public QrImage Image = new();
        public QrDetection Detection = new();
        public QrDetectionOptions Sampling = new();
        public QrSymbol Symbol = new();
    }
    static QrSymbol Encode(QrSymbolKind kind, int version) => new QrEncoder().EncodeText("12345", new QrEncodeOptions
        { ForceSymbolKind = true, SymbolKind = kind, QuietZoneModules = 0, MinimumVersion = version, MaximumVersion = version == 0 ? 40 : version });
    static QrDecodeOptions Only(QrSymbolKind kind) => new()
    {
        AllowModel2 = kind == QrSymbolKind.Model2, AllowMicro = kind == QrSymbolKind.Micro,
        AllowRectangularMicro = kind == QrSymbolKind.rMQR, AllowLegacyModel1 = kind == QrSymbolKind.LegacyModel1
    };
    static ImageRecoveryResult Recover(Scene scene, QrDecodeOptions options) => ImageDecodeRecovery.RecoverCodewords(scene.Image, scene.Detection, null, scene.Sampling, options);
    static QrPoint[] Points(QrQuadrilateral q) => [q.TopLeft!, q.TopRight!, q.BottomRight!, q.BottomLeft!];
    static QrQuadrilateral Corners(PerspectiveTransform t, int w, int h) => new()
    { TopLeft = t.MapPoint(new QrPoint()), TopRight = t.MapPoint(new QrPoint { X = w }), BottomRight = t.MapPoint(new QrPoint { X = w, Y = h }), BottomLeft = t.MapPoint(new QrPoint { Y = h }) };
    static QrMatrix MatrixFixture() => CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.Model2, 1, 0), QrSymbolKind.Model2, 1, 0);
    static int Cell(int word) => CodewordErasureRecoveryTests.Cells(QrSymbolKind.Model2, 1, 0, word)[0];
    static void Flip(QrMatrix m, int i) => m.Values[i] = m.Values[i] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
    static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
    static void AssertEmpty(QrAttemptResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.Not.EqualTo(QrFailureReason.None));
        Assert.That(result.Result, Is.Null);
        Assert.That(result.Text, Is.Null);
        Assert.That(result.RawBytes, Is.Empty);
        Assert.That(result.Segments, Is.Empty);
    }
}
