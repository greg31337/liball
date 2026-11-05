using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrDetectionEdgeCaseTests
{
    private static IEnumerable<TestCaseData> IndependentProfiles()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(GoldenPath("vectors.json")));
        foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            string id = vector.GetProperty("id").GetString()!;
            string profile = vector.GetProperty("profile").GetString()!;
            if (profile != "Micro" && profile != "rMQR" && id != "legacy-model1-v02-m") continue;
            for (int turns = 0; turns < 4; turns++)
                yield return new TestCaseData(id, turns).SetName($"DetectIndependent_{id}_turn{turns}");
        }
    }

    [TestCaseSource(nameof(IndependentProfiles))]
    public void DetectsIndependentSingleFinderAndLegacyProfiles(string id, int turns)
    {
        Fixture fixture = Load(id);
        bool mirror = turns % 2 != 0;
        bool invert = turns >= 2;
        QrImage image = Render(fixture.Symbol, 3, 5, invert);
        Transform(image, turns, mirror);
        string before = Snapshot(image);
        QrDetectionOptions options = Options(fixture.Symbol.SymbolKind);
        IQrDetector detector = new QrDetector();
        foreach (bool binary in new[] { false, true })
        {
            QrDetectionSet result = binary
                ? detector.DetectBinaryMatrix(Binary(image), options)
                : detector.DetectImage(image, options);
            Assert.That(result.Detections, Has.Length.EqualTo(1));
            QrDetection detection = result.Detections[0];
            Assert.Multiple(() =>
            {
                Assert.That(detection.EstimatedSymbolKind, Is.EqualTo(fixture.Symbol.SymbolKind));
                Assert.That(detection.EstimatedWidth, Is.EqualTo(fixture.Symbol.Width));
                Assert.That(detection.EstimatedHeight, Is.EqualTo(fixture.Symbol.Height));
                Assert.That(detection.Polarity, Is.EqualTo(invert ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight));
                Assert.That(detection.QuietZone!.IsConformant, Is.True);
                Assert.That(detection.SampledMatrix, Is.Null);
            });
            if (fixture.Symbol.SymbolKind != QrSymbolKind.LegacyModel1)
            {
                Assert.That(detection.HasMirrorEstimate, Is.True);
                Assert.That(detection.IsMirrored, Is.EqualTo(mirror));
            }
            QrDetection sampled = new QrPerspectiveSampler().SampleDetection(image, detection, options);
            if (fixture.Symbol.SymbolKind == QrSymbolKind.LegacyModel1 && mirror)
            {
                // Three-finder geometry preserves the reflection. Model 1 decoding
                // intentionally requires ordinary geometry, even when detection succeeds.
                Assert.That(Differences(sampled.SampledMatrix!, fixture.Symbol.Matrix!, true), Is.Zero);
                Assert.That(() => Decode(sampled.SampledMatrix!, QrSymbolKind.LegacyModel1),
                    Throws.TypeOf<QrDecodeException>());
            }
            else
            {
                QrDecodeResult decoded = Decode(sampled.SampledMatrix!, fixture.Symbol.SymbolKind);
                Assert.That(decoded.Text, Is.EqualTo(fixture.Text));
                Assert.That(decoded.CorrectedErrorCount, Is.Zero);
            }
        }
        Assert.That(Snapshot(image), Is.EqualTo(before));
    }

    [Test]
    public void QuietZoneToleranceNeverUpgradesConformance(
        [Values(QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR, QrSymbolKind.LegacyModel1)] QrSymbolKind kind,
        [Values(-2, -1, 0, 1)] int marginDelta,
        [Values(false, true)] bool tolerate,
        [Values(false, true)] bool invert)
    {
        Fixture fixture = Representative(kind);
        int required = kind is QrSymbolKind.Micro or QrSymbolKind.rMQR ? 2 : 4;
        int margin = marginDelta == -2 ? 0 : required + marginDelta;
        QrImage image = Render(fixture.Symbol, 3, margin, invert);
        QrDetectionOptions options = Options(kind);
        options.AllowCroppedQuietZone = tolerate;
        bool conformant = margin >= required;
        QrDetectionSet result = new QrDetector().DetectImage(image, options);
        Assert.That(result.Detections, Has.Length.EqualTo(tolerate || conformant ? 1 : 0));
        if (result.Detections.Length != 0)
        {
            QrQuietZoneMeasurement measured = result.Detections[0].QuietZone!;
            Assert.That(measured.RequiredModules, Is.EqualTo(required));
            Assert.That(measured.IsConformant, Is.EqualTo(conformant));
            Assert.That(measured.IsCropped, Is.EqualTo(!conformant));
            Assert.That(measured.AcceptForDetection, Is.True);
        }
        // Exact caller geometry makes the observed pixel counts independently calculable.
        QrDetection sampled = new QrPerspectiveSampler().SampleDetection(image, Geometry(fixture.Symbol, 3, margin, invert), options);
        QrQuietZoneMeasurement quiet = sampled.QuietZone!;
        int observedMargin = Math.Min(margin, required);
        int expectedPixels = ((fixture.Symbol.Width + 2 * observedMargin) * (fixture.Symbol.Height + 2 * observedMargin)
            - fixture.Symbol.Width * fixture.Symbol.Height) * 9;
        Assert.Multiple(() =>
        {
            Assert.That(quiet.IsConformant, Is.EqualTo(conformant));
            Assert.That(quiet.IsFullyObserved, Is.EqualTo(conformant));
            Assert.That(quiet.IsCropped, Is.EqualTo(!conformant));
            Assert.That(quiet.AcceptForDetection, Is.EqualTo(tolerate || conformant));
            Assert.That(quiet.ObservedPixelCount, Is.EqualTo(expectedPixels));
            Assert.That(quiet.ContaminatedPixelCount, Is.Zero);
            Assert.That(sampled.SampledMatrix!.Values, Is.EqualTo(fixture.Symbol.Matrix!.Values));
        });
    }

    [Test]
    public void QuietZoneContaminationIsReportedForEveryProfileAndPolarity(
        [Values(QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR, QrSymbolKind.LegacyModel1)] QrSymbolKind kind,
        [Values(false, true)] bool invert,
        [Values(false, true)] bool tolerate)
    {
        Fixture fixture = Representative(kind);
        int required = kind is QrSymbolKind.Micro or QrSymbolKind.rMQR ? 2 : 4;
        QrImage image = Render(fixture.Symbol, 3, required, invert);
        image.Pixels[0] = invert ? (byte)255 : (byte)0;
        QrDetectionOptions options = Options(kind);
        options.AllowCroppedQuietZone = tolerate;
        QrDetectionSet detected = new QrDetector().DetectImage(image, options);
        Assert.That(detected.Detections, Has.Length.EqualTo(tolerate ? 1 : 0));
        QrDetection sampled = new QrPerspectiveSampler().SampleDetection(image, Geometry(fixture.Symbol, 3, required, invert), options);
        QrQuietZoneMeasurement quiet = sampled.QuietZone!;
        Assert.Multiple(() =>
        {
            Assert.That(quiet.ContaminatedPixelCount, Is.EqualTo(1));
            Assert.That(quiet.IsFullyObserved, Is.True);
            Assert.That(quiet.IsCropped, Is.False);
            Assert.That(quiet.IsConformant, Is.False);
            Assert.That(quiet.AcceptForDetection, Is.EqualTo(tolerate));
        });
    }

    [Test]
    public void MissingOrDamagedAlignmentDoesNotDiscardUsableThreeFinderGeometry(
        [Values(2, 7, 14)] int version, [Values(false, true)] bool erase)
    {
        QrSymbol symbol = new QrEncoder().EncodeText("ALIGNMENT 142", new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = version, MaximumVersion = version,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.High, MaskPattern = QrMaskPattern.Two, QuietZoneModules = 0
        });
        QrModuleValue[] before = (QrModuleValue[])symbol.Matrix!.Values.Clone();
        int changed = 0;
        for (int i = 0; i < before.Length; i++)
            if (symbol.Matrix.Roles![i] == QrModuleRole.Alignment && (erase || (i / symbol.Width + i % symbol.Width) % 2 == 0))
            {
                symbol.Matrix.Values[i] = QrModuleValue.Light;
                if (before[i] != QrModuleValue.Light) changed++;
            }
        Assert.That(changed, Is.GreaterThan(0));
        QrImage image = Render(symbol, 4, 5, false);
        QrDetectionOptions options = Options(QrSymbolKind.Model2);
        QrDetectionSet result = new QrDetector().DetectImage(image, options);
        Assert.That(result.Detections, Has.Length.EqualTo(1));
        QrDetection detection = result.Detections[0];
        Assert.That(detection.EstimatedVersion, Is.EqualTo(version));
        Assert.That(detection.Corners!.TopLeft!.X, Is.EqualTo(20).Within(2));
        Assert.That(detection.Corners.BottomRight!.X, Is.EqualTo(20 + symbol.Width * 4).Within(4));
        QrMatrix sampled = new QrPerspectiveSampler().SampleDetection(image, detection, options).SampledMatrix!;
        Assert.That(sampled.Values, Is.EqualTo(symbol.Matrix.Values));
        // Damage is confined to fixed patterns. Geometry fallback must preserve all
        // payload and parity bits, even if family discrimination rejects the damage.
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        Assert.That(processor.ExtractCodewords(sampled, QrSymbolKind.Model2, version,
            symbol.ErrorCorrectionLevel, symbol.MaskPattern), Is.EqualTo(symbol.Codewords));
    }

    [Test]
    public void RejectsFinderLookalikesWithoutSupportingSymbolEvidence(
        [Values("single", "three", "cross", "wrong-ratio")] string shape,
        [Values(false, true)] bool invert)
    {
        QrMatrix matrix = new() { Width = 35, Height = 35, Values = Enumerable.Repeat(QrModuleValue.Light, 1225).ToArray() };
        foreach ((int left, int top) in shape == "three" ? new[] { (3, 3), (25, 3), (3, 25) } : new[] { (3, 3) })
            for (int y = 0; y < 7; y++)
                for (int x = 0; x < 7; x++)
                {
                    bool dark = x == 0 || y == 0 || x == 6 || y == 6 || x is >= 2 and <= 4 && y is >= 2 and <= 4;
                    if (shape == "cross") dark &= x is >= 2 and <= 4 || y is >= 2 and <= 4;
                    if (shape == "wrong-ratio") dark = x == 0 || y == 0 || x == 6 || y == 6 || x == 3 && y == 3;
                    matrix.Values[(top + y) * 35 + left + x] = dark ? QrModuleValue.Dark : QrModuleValue.Light;
                }
        QrImage image = Render(new QrSymbol { SymbolKind = QrSymbolKind.Model2, Matrix = matrix, Width = 35, Height = 35 }, 3, 4, invert);
        IQrDetector detector = new QrDetector();
        QrDetectionOptions options = new() { BinarizationMode = QrBinarizationMode.SuppliedBinary };
        Assert.That(detector.DetectImage(image, options).Detections, Is.Empty);
        Assert.That(detector.DetectBinaryMatrix(Binary(image), options).Detections, Is.Empty);
    }

    [Test]
    public void SamplesStrongForeshorteningWithKnownGeometry(
        [Values(0.02, 0.04, 0.06)] double perspective,
        [Values(false, true)] bool mirror, [Values(false, true)] bool invert)
    {
        Fixture fixture = Representative(QrSymbolKind.Model2);
        QrMatrix source = fixture.Symbol.Matrix!;
        const int scale = 16;
        const int offset = 80;
        int width = source.Width * scale + 2 * offset;
        int height = source.Height * scale + 2 * offset;
        byte[] pixels = new byte[width * height];
        // Analytic inverse of X=80+16*u/(1+p*u), Y=80+16*v/(1+p*u).
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                double horizontal = ((mirror ? width - x - 0.5 : x + 0.5) - offset) / scale;
                double denominator = 1 - perspective * horizontal;
                double u = horizontal / denominator;
                double v = (y + 0.5 - offset) / scale / denominator;
                bool dark = denominator > 0 && u >= 0 && u < source.Width && v >= 0 && v < source.Height
                    && source.Values[(int)v * source.Width + (int)u] == QrModuleValue.Dark;
                pixels[y * width + x] = dark != invert ? (byte)0 : (byte)255;
            }
        QrImage image = new() { Width = width, Height = height, Stride = width, Pixels = pixels };
        QrDetection geometry = Geometry(fixture.Symbol, scale, 5, invert);
        foreach (QrPoint point in Points(geometry.Corners!))
        {
            double u = (point.X - offset) / scale;
            double v = (point.Y - offset) / scale;
            point.X = offset + scale * u / (1 + perspective * u);
            point.Y = offset + scale * v / (1 + perspective * u);
            if (mirror) point.X = width - point.X;
        }
        QrDetection sampled = new QrPerspectiveSampler().SampleDetection(image, geometry, Options(QrSymbolKind.Model2));
        Assert.That(sampled.SampledMatrix!.Values, Is.EqualTo(source.Values));
        Assert.That(Decode(sampled.SampledMatrix, QrSymbolKind.Model2).Text, Is.EqualTo(fixture.Text));
        Assert.That(sampled.QuietZone!.HasUnmappableBoundary, Is.False);
    }

    [TestCase("collapsed")]
    [TestCase("crossed")]
    [TestCase("nonfinite")]
    public void RejectsDegeneratePerspectiveGeometry(string shape)
    {
        Fixture fixture = Representative(QrSymbolKind.Micro);
        QrImage image = Render(fixture.Symbol, 3, 4, false);
        QrDetection detection = Geometry(fixture.Symbol, 3, 4, false);
        if (shape == "collapsed") detection.Corners!.TopRight = detection.Corners.TopLeft;
        if (shape == "crossed") (detection.Corners!.TopRight, detection.Corners.BottomRight) = (detection.Corners.BottomRight, detection.Corners.TopRight);
        if (shape == "nonfinite") detection.Corners!.BottomLeft!.Y = double.PositiveInfinity;
        Assert.That(() => new QrPerspectiveSampler().SampleDetection(image, detection, Options(QrSymbolKind.Micro)),
            Throws.TypeOf<QrInputException>());
    }

    private sealed record Fixture(QrSymbol Symbol, string Text);

    private static Fixture Representative(QrSymbolKind kind) => Load(kind switch
    {
        QrSymbolKind.Micro => "micro-m4-l", QrSymbolKind.rMQR => "rmqr-r7x43-m",
        QrSymbolKind.LegacyModel1 => "legacy-model1-v02-m", _ => "model2-v01-l"
    });

    private static Fixture Load(string id)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(GoldenPath("vectors.json")));
        JsonElement vector = document.RootElement.GetProperty("vectors").EnumerateArray().Single(v => v.GetProperty("id").GetString() == id);
        string[] rows = File.ReadAllLines(GoldenPath(vector.GetProperty("matrix_file").GetString()!));
        QrMatrix matrix = new() { Width = rows[0].Length, Height = rows.Length,
            Values = string.Concat(rows).Select(c => c == '1' ? QrModuleValue.Dark : QrModuleValue.Light).ToArray() };
        QrSymbolKind kind = Enum.Parse<QrSymbolKind>(vector.GetProperty("profile").GetString()!);
        return new Fixture(new QrSymbol
        {
            SymbolKind = kind,
            Version = kind == QrSymbolKind.Micro ? (matrix.Width - 9) / 2 : kind == QrSymbolKind.rMQR ? 0 : vector.GetProperty("version").GetInt32(),
            Width = matrix.Width, Height = matrix.Height, Matrix = matrix
        }, vector.GetProperty("payload_utf8").GetString()!);
    }

    private static QrImage Render(QrSymbol symbol, int scale, int margin, bool invert)
    {
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions
        { Scale = scale, QuietZoneModules = margin, Invert = invert });
        return new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels };
    }

    private static void Transform(QrImage image, int turns, bool mirror)
    {
        if (mirror)
            for (int y = 0; y < image.Height; y++) Array.Reverse(image.Pixels, y * image.Stride, image.Width);
        for (int turn = 0; turn < turns; turn++)
        {
            byte[] pixels = new byte[image.Pixels.Length];
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++) pixels[x * image.Height + image.Height - y - 1] = image.Pixels[y * image.Width + x];
            (image.Width, image.Height) = (image.Height, image.Width);
            image.Stride = image.Width;
            image.Pixels = pixels;
        }
    }

    private static QrDetection Geometry(QrSymbol symbol, int scale, int margin, bool invert) => new()
    {
        HasSymbolKindEstimate = true, EstimatedSymbolKind = symbol.SymbolKind, EstimatedVersion = symbol.Version,
        EstimatedWidth = symbol.Width, EstimatedHeight = symbol.Height, ModuleSize = scale,
        Polarity = invert ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight,
        Corners = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = margin * scale, Y = margin * scale },
            TopRight = new QrPoint { X = (margin + symbol.Width) * scale, Y = margin * scale },
            BottomRight = new QrPoint { X = (margin + symbol.Width) * scale, Y = (margin + symbol.Height) * scale },
            BottomLeft = new QrPoint { X = margin * scale, Y = (margin + symbol.Height) * scale }
        }
    };

    private static QrDetectionOptions Options(QrSymbolKind kind) => new()
    {
        AllowModel2 = kind == QrSymbolKind.Model2, AllowMicro = kind == QrSymbolKind.Micro,
        AllowRectangularMicro = kind == QrSymbolKind.rMQR, AllowLegacyModel1 = kind == QrSymbolKind.LegacyModel1,
        BinarizationMode = QrBinarizationMode.SuppliedBinary, MaximumPyramidLevels = 1
    };

    private static QrDecodeResult Decode(QrMatrix matrix, QrSymbolKind kind) => new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions
    {
        AllowModel2 = kind == QrSymbolKind.Model2, AllowMicro = kind == QrSymbolKind.Micro,
        AllowRectangularMicro = kind == QrSymbolKind.rMQR, AllowLegacyModel1 = kind == QrSymbolKind.LegacyModel1
    });

    private static QrMatrix Binary(QrImage image) => new()
    {
        Width = image.Width, Height = image.Height,
        Values = image.Pixels.Select(p => p == 0 ? QrModuleValue.Dark : QrModuleValue.Light).ToArray()
    };
    private static QrPoint[] Points(QrQuadrilateral corners) => [corners.TopLeft!, corners.TopRight!, corners.BottomRight!, corners.BottomLeft!];
    private static int Differences(QrMatrix actual, QrMatrix expected, bool transpose) =>
        actual.Values.Where((value, i) => value != expected.Values[transpose ? i % expected.Width * expected.Width + i / expected.Width : i]).Count();
    private static string GoldenPath(string relative) => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../tests/Fixtures/Golden", relative));
    private static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
}
