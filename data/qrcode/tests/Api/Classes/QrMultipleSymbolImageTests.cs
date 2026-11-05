using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrMultipleSymbolImageTests
{
    [Test]
    public void MixedSceneRetainsEveryPhysicalSymbolAndItsSourceCoordinates(
        [Values(0, 1, 2, 3)] int turns, [Values(false, true)] bool invert,
        [Values(1, 4)] int levels, [Values(false, true)] bool binary)
    {
        Scene scene = MixedScene();
        RotateScene(scene, turns);
        if (invert)
        {
            for (int i = 0; i < scene.Image.Pixels.Length; i++) scene.Image.Pixels[i] = (byte)(255 - scene.Image.Pixels[i]);
            foreach (Placed item in scene.Symbols) item.Inverted = !item.Inverted;
        }
        string imageBefore = Snapshot(scene.Image);
        QrDetectionOptions options = Options(levels);
        string optionsBefore = Snapshot(options);
        QrDetectionSet result = Detect(scene.Image, options, binary);
        AssertScene(result, scene, options);
        Assert.That(result.Polarity, Is.EqualTo(QrPolarity.Automatic));
        Assert.That(result.BinaryRaster, Is.Null);
        Assert.That(result.Detections.Select(d => d.CandidateIndex), Is.Unique);
        Assert.That(result.CandidateCount, Is.GreaterThanOrEqualTo(scene.Symbols.Count));
        Assert.That(Snapshot(scene.Image), Is.EqualTo(imageBefore));
        Assert.That(Snapshot(options), Is.EqualTo(optionsBefore));
    }

    [Test]
    public void DuplicateObservationsDoNotConsumeLimitsOrCollapseSeparateCopies(
        [Values(3, 7, 10)] int limit)
    {
        Scene scene = MixedScene();
        QrDetectionOptions options = Options(4);
        QrDetection[] originals = Detect(scene.Image, options, false).Detections;
        Assert.That(originals, Has.Length.EqualTo(7));
        List<QrDetection> observations = [];
        for (int index = 0; index < originals.Length; index++)
        {
            QrDetection original = originals[index];
            original.Confidence = 0.5 + index * 0.05;
            observations.Add(original);
            for (int copy = 1; copy <= 2; copy++)
            {
                QrDetection duplicate = JsonSerializer.Deserialize<QrDetection>(Snapshot(original),
                    new JsonSerializerOptions { IncludeFields = true })!;
                duplicate.Confidence -= 0.1;
                duplicate.CandidateIndex = 100 + index * 2 + copy;
                QrPoint[] points = Points(duplicate);
                foreach (QrPoint point in points) { point.X += 0.1 * copy; point.Y -= 0.1 * copy; }
                duplicate.Corners = new QrQuadrilateral
                {
                    TopLeft = points[copy], TopRight = points[(copy + 1) % 4],
                    BottomRight = points[(copy + 2) % 4], BottomLeft = points[(copy + 3) % 4]
                };
                observations.Add(duplicate);
            }
        }
        options.MaximumCandidates = limit;
        QrDetection[] input = observations.ToArray();
        string before = Snapshot(input);
        QrDetection[] selected = DetectionDeduplicator.SelectDetections(input, options);
        Assert.That(selected, Is.EqualTo(originals.TakeLast(Math.Min(limit, originals.Length))));
        Assert.That(Snapshot(input), Is.EqualTo(before));
        Array.Reverse(input);
        Assert.That(DetectionDeduplicator.SelectDetections(input, options), Is.EqualTo(selected));
        Placed[] matched = selected.Select(d => AssertMatches(d, scene, options)).ToArray();
        if (limit >= 7)
            Assert.That(matched.Count(s => s.Text == "REPEAT"), Is.EqualTo(2));
    }

    [Test]
    public void RepeatedCallsHaveStableOrderingAndIndependentMetadata([Values(false, true)] bool binary)
    {
        Scene scene = MixedScene();
        QrDetectionOptions options = Options(4);
        QrDetectionSet first = Detect(scene.Image, options, binary);
        string before = Snapshot(first);
        QrDetectionSet second = Detect(scene.Image, options, binary);
        Assert.That(Snapshot(second), Is.EqualTo(before));
        first.Detections[0].Corners!.TopLeft!.X = -1000;
        first.Detections[0].Diagnostics[0] = "changed";
        first.Detections[0].QuietZone!.RequiredModules = 99;
        Assert.That(Snapshot(second), Is.EqualTo(before));
        Assert.That(Snapshot(Detect(scene.Image, options, binary)), Is.EqualTo(before));
    }

    [Test]
    public void ExplicitPolaritySelectsOnlyMatchingSymbolsAndRetainsNormalizedRaster(
        [Values(QrPolarity.DarkOnLight, QrPolarity.LightOnDark)] QrPolarity polarity,
        [Values(false, true)] bool binary)
    {
        Scene scene = MixedScene();
        scene.Symbols.RemoveAll(s => s.Inverted != (polarity == QrPolarity.LightOnDark));
        QrDetectionOptions options = Options(2);
        options.Polarity = polarity;
        QrDetectionSet result = Detect(scene.Image, options, binary);
        AssertScene(result, scene, options);
        Assert.That(result.Polarity, Is.EqualTo(polarity));
        Assert.That(result.BinaryRaster, Is.Not.Null);
        Assert.That(result.BinaryRaster!.Width, Is.EqualTo(scene.Image.Width));
        Assert.That(result.BinaryRaster.Height, Is.EqualTo(scene.Image.Height));
        Assert.That(result.BinaryRaster.Pixels, Is.EqualTo(scene.Image.Pixels.Select(p =>
            polarity == QrPolarity.LightOnDark ? (byte)(255 - p) : p).ToArray()));
    }

    [Test]
    public void FamilyRestrictionsApplyAcrossTheWholeScene(
        [Values(QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR)] QrSymbolKind kind)
    {
        Scene scene = MixedScene();
        // Legacy and Model 2 share three-finder geometry; the provisional estimate
        // cannot distinguish them until payload sampling/decoding.
        scene.Symbols.RemoveAll(s => s.Kind != kind && !(kind == QrSymbolKind.Model2 && s.Kind == QrSymbolKind.LegacyModel1));
        QrDetectionOptions options = Options(2);
        options.AllowModel2 = kind == QrSymbolKind.Model2;
        options.AllowLegacyModel1 = false;
        options.AllowMicro = kind == QrSymbolKind.Micro;
        options.AllowRectangularMicro = kind == QrSymbolKind.rMQR;
        QrDetectionSet result = Detect(scene.Image, options, false);
        Assert.That(result.Detections.Select(d => d.EstimatedSymbolKind), Is.All.EqualTo(kind));
        foreach (Placed expected in scene.Symbols)
        {
            QrDetection matched = result.Detections.Single(d =>
                Math.Abs(Points(d).Average(p => p.X) - expected.Left - expected.Width / 2.0) < 2
                && Math.Abs(Points(d).Average(p => p.Y) - expected.Top - expected.Height / 2.0) < 2);
            AssertMatches(matched, scene, options);
        }
    }

    [Test]
    public void CandidateLimitsBoundResultsAndSingleSelectionCompetesAcrossProfiles(
        [Values(1, 2, 3, 4, 8, 16, 64)] int limit, [Values(false, true)] bool single)
    {
        Scene scene = MixedScene();
        QrDetectionOptions options = Options(4);
        options.MaximumCandidates = limit;
        QrDetectionSet multiple = Detect(scene.Image, options, false);
        Assert.That(multiple.Detections.Length, Is.LessThanOrEqualTo(limit));
        options.DetectMultiple = !single;
        string before = Snapshot(options);
        QrDetectionSet result = Detect(scene.Image, options, false);
        Assert.That(result.Detections.Length, Is.EqualTo(single ? Math.Min(1, multiple.Detections.Length) : multiple.Detections.Length));
        Assert.That(result.CandidateCount, Is.EqualTo(multiple.CandidateCount));
        Assert.That(Snapshot(options), Is.EqualTo(before));
        if (single && result.Detections.Length != 0)
        {
            Assert.That(result.Detections[0].Confidence, Is.EqualTo(multiple.Detections.Max(d => d.Confidence)));
            Assert.That(result.BinaryRaster, Is.Not.Null);
        }
        // Small finder budgets may leave incomplete three-finder evidence and
        // provisional single-finder hypotheses; geometry alone is not a decode.
        Assert.That(result.Detections.Select(d => d.CandidateIndex), Is.Unique);
        Assert.That(result.Detections.All(d => d.SampledMatrix == null && d.Confidence >= 0 && d.Confidence <= 1), Is.True);
        if (limit >= 16)
            foreach (QrDetection detection in result.Detections) AssertMatches(detection, scene, options);
        if (limit == 64 && !single) AssertScene(result, scene, options);
    }

    [Test]
    public void OverlappingBordersAndPartialOcclusionRetainDistinctSymbols(
        [Values(false, true)] bool occludePayload, [Values(false, true)] bool binary)
    {
        Scene scene = Blank(270, 270);
        Add(scene, Encode("FIRST", QrSymbolKind.Model2, 1), "FIRST", 20, 20, 4, 0, false, false);
        int position = occludePayload ? 96 : 112;
        Add(scene, Encode("SECOND", QrSymbolKind.Model2, 1), "SECOND", position, position, 4, 0, false, false);
        QrDetectionOptions options = Options(1);
        QrDetectionSet result = Detect(scene.Image, options, binary);
        AssertScene(result, scene, options);
        Assert.That(result.Detections, Has.Length.EqualTo(2));
        Assert.That(result.Detections.Any(d => !d.QuietZone!.IsConformant), Is.True);
        options.AllowCroppedQuietZone = false;
        QrDetectionSet strict = Detect(scene.Image, options, binary);
        Assert.That(strict.Detections.Select(d => d.CandidateIndex),
            Is.EqualTo(result.Detections.Where(d => d.QuietZone!.IsConformant).Select(d => d.CandidateIndex)));
        Assert.That(strict.Detections, Has.Length.EqualTo(1));
        Assert.That(AssertMatches(strict.Detections[0], scene, options).Text, Is.EqualTo("SECOND"));
    }

    private static void AssertScene(QrDetectionSet result, Scene scene, QrDetectionOptions options)
    {
        Assert.That(result.Detections, Has.Length.EqualTo(scene.Symbols.Count),
            string.Join("; ", result.Detections.Select(d => $"{d.EstimatedSymbolKind} {d.EstimatedWidth}x{d.EstimatedHeight} at {MinimumX(d):F1},{MinimumY(d):F1}")));
        Placed[] expectedOrder = scene.Symbols.OrderBy(s => s.Top).ThenBy(s => s.Left).ToArray();
        for (int i = 0; i < result.Detections.Length; i++)
        {
            Placed matched = AssertMatches(result.Detections[i], scene, options);
            Assert.That(matched, Is.SameAs(expectedOrder[i]));
        }
    }

    private static Placed AssertMatches(QrDetection detection, Scene scene, QrDetectionOptions options)
    {
        double centerX = Points(detection).Average(p => p.X);
        double centerY = Points(detection).Average(p => p.Y);
        Placed expected = scene.Symbols.MinBy(s => Math.Pow(s.Left + s.Width / 2.0 - centerX, 2)
            + Math.Pow(s.Top + s.Height / 2.0 - centerY, 2))!;
        Assert.Multiple(() =>
        {
            Assert.That(centerX, Is.EqualTo(expected.Left + expected.Width / 2.0).Within(2));
            Assert.That(centerY, Is.EqualTo(expected.Top + expected.Height / 2.0).Within(2));
            Assert.That(detection.EstimatedWidth, Is.EqualTo(expected.ModuleWidth));
            Assert.That(detection.EstimatedHeight, Is.EqualTo(expected.ModuleHeight));
            Assert.That(detection.Polarity, Is.EqualTo(expected.Inverted ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight));
            Assert.That(detection.EstimatedSymbolKind, Is.EqualTo(expected.Kind == QrSymbolKind.LegacyModel1 ? QrSymbolKind.Model2 : expected.Kind));
            Assert.That(detection.SampledMatrix, Is.Null);
        });
        QrDetection sampled = new QrPerspectiveSampler().SampleDetection(scene.Image, detection, options);
        QrAttemptResult attempt = new QrDecoder().TryDecodeMatrix(sampled.SampledMatrix!, new QrDecodeOptions());
        Assert.That(attempt.Success, Is.True, $"{expected.Kind} '{expected.Text}' at {expected.Left},{expected.Top}: {attempt.FailureReason}; corners {Snapshot(detection.Corners!)}");
        QrDecodeResult decoded = attempt.Result!;
        Assert.That(decoded.Text, Is.EqualTo(expected.Text));
        Assert.That(decoded.Symbol!.SymbolKind, Is.EqualTo(expected.Kind));
        return expected;
    }

    private static Scene MixedScene()
    {
        Scene scene = Blank(600, 530);
        QrSymbol repeated = Encode("REPEAT", QrSymbolKind.Model2, 1);
        Add(scene, repeated, "REPEAT", 20, 20, 3, 0, false, false);
        Add(scene, Encode("12345", QrSymbolKind.Micro, 4), "12345", 230, 30, 4, 1, true, true);
        Add(scene, Encode("123", QrSymbolKind.rMQR, 0), "123", 420, 45, 3, 1, false, false);
        Add(scene, Encode("LARGE", QrSymbolKind.Model2, 7), "LARGE", 20, 250, 4, 3, false, true);
        Add(scene, Encode("123", QrSymbolKind.Micro, 2), "123", 300, 260, 3, 2, false, false);
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "../../../../tests/Fixtures/Golden/matrices/legacy-model1-v02-m.txt"));
        string[] rows = File.ReadAllLines(path);
        QrSymbol legacy = new() { SymbolKind = QrSymbolKind.LegacyModel1, Version = 2, Width = 25, Height = 25,
            Matrix = new QrMatrix { Width = 25, Height = 25,
                Values = string.Concat(rows).Select(c => c == '1' ? QrModuleValue.Dark : QrModuleValue.Light).ToArray() } };
        Add(scene, legacy, "QR Code Model 1 ", 440, 270, 3, 2, false, false);
        Add(scene, repeated, "REPEAT", 440, 440, 2, 2, false, false);
        return scene;
    }

    private static QrSymbol Encode(string text, QrSymbolKind kind, int version)
    {
        QrEncodeOptions options = new()
        {
            ForceSymbolKind = true, SymbolKind = kind, QuietZoneModules = 0,
            ErrorCorrectionLevel = kind == QrSymbolKind.Model2 ? QrErrorCorrectionLevel.High : QrErrorCorrectionLevel.Medium,
            ForceErrorCorrectionLevel = true, BoostErrorCorrectionLevel = false
        };
        if (kind != QrSymbolKind.rMQR) { options.MinimumVersion = version; options.MaximumVersion = version; }
        else { options.MinimumWidth = 43; options.MaximumWidth = 43; options.MinimumHeight = 7; options.MaximumHeight = 7; }
        return new QrEncoder().EncodeText(text, options);
    }

    private static void Add(Scene scene, QrSymbol symbol, string text, int left, int top, int scale, int turns, bool mirror, bool invert)
    {
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = scale, QuietZoneModules = 4, Invert = invert });
        QrImage image = new() { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels };
        if (mirror)
            for (int row = 0; row < image.Height; row++) Array.Reverse(image.Pixels, row * image.Width, image.Width);
        for (int turn = 0; turn < turns; turn++) RotateImage(image);
        for (int row = 0; row < image.Height; row++)
            Array.Copy(image.Pixels, row * image.Width, scene.Image.Pixels, (top + row) * scene.Image.Width + left, image.Width);
        scene.Symbols.Add(new Placed
        {
            Kind = symbol.SymbolKind, Text = text, Inverted = invert,
            Left = left + scale * 4, Top = top + scale * 4,
            Width = image.Width - scale * 8, Height = image.Height - scale * 8,
            ModuleWidth = symbol.Width, ModuleHeight = symbol.Height
        });
    }

    private static void RotateScene(Scene scene, int turns)
    {
        for (int turn = 0; turn < turns; turn++)
        {
            foreach (Placed item in scene.Symbols)
            {
                (item.Left, item.Top) = (scene.Image.Height - item.Top - item.Height, item.Left);
                (item.Width, item.Height) = (item.Height, item.Width);
            }
            RotateImage(scene.Image);
        }
    }

    private static void RotateImage(QrImage image)
    {
        byte[] pixels = new byte[image.Pixels.Length];
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++) pixels[x * image.Height + image.Height - 1 - y] = image.Pixels[y * image.Width + x];
        (image.Width, image.Height) = (image.Height, image.Width);
        image.Stride = image.Width;
        image.Pixels = pixels;
    }

    private sealed class Scene
    {
        public QrImage Image = new();
        public List<Placed> Symbols = [];
    }

    private sealed class Placed
    {
        public QrSymbolKind Kind;
        public string Text = "";
        public int Left, Top, Width, Height, ModuleWidth, ModuleHeight;
        public bool Inverted;
    }

    private static Scene Blank(int width, int height) => new()
    {
        Image = new QrImage { Width = width, Height = height, Stride = width, Pixels = Enumerable.Repeat((byte)255, width * height).ToArray() }
    };

    private static QrDetectionOptions Options(int levels) => new() { MaximumPyramidLevels = levels, BinarizationMode = QrBinarizationMode.GlobalHistogram };
    private static QrDetectionSet Detect(QrImage image, QrDetectionOptions options, bool binary) => binary
        ? new QrDetector().DetectBinaryMatrix(new QrMatrix { Width = image.Width, Height = image.Height,
            Values = image.Pixels.Select(p => p == 0 ? QrModuleValue.Dark : QrModuleValue.Light).ToArray() }, options)
        : new QrDetector().DetectImage(image, options);
    private static QrPoint[] Points(QrDetection detection) => [detection.Corners!.TopLeft!, detection.Corners.TopRight!, detection.Corners.BottomRight!, detection.Corners.BottomLeft!];
    private static double MinimumX(QrDetection detection) => Points(detection).Min(p => p.X);
    private static double MinimumY(QrDetection detection) => Points(detection).Min(p => p.Y);
    private static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
}
