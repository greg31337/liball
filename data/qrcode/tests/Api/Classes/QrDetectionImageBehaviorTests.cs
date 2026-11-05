using System.Text;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrDetectionImageBehaviorTests
{
    private const string Payload = "IMAGE-141";

    private static readonly string[] Conditions =
    [
        "clean", "low-resolution", "high-resolution", "rotate-90", "rotate-180", "rotate-270",
        "rotate-17", "rotate-minus-13", "skew", "perspective", "mirrored", "inverted",
        "cropped-border", "blurred", "noisy", "uneven-lighting"
    ];

    private static IEnumerable<TestCaseData> ImageCases()
    {
        foreach (int version in new[] { 1, 2, 7 })
            foreach (string condition in Conditions)
                yield return new TestCaseData(version, condition).SetName($"DetectSampleDecode_v{version}_{condition}");
    }

    [TestCaseSource(nameof(ImageCases))]
    public void DetectsSamplesAndDecodesDistortedImages(int version, string condition)
    {
        QrSymbol symbol = Encode(version);
        Scene scene = CreateScene(symbol, condition);
        QrDetectionOptions options = Options(condition);
        string imageBefore = Snapshot(scene.Image);
        string optionsBefore = Snapshot(options);
        IQrDetector detector = new QrDetector();
        QrDetectionSet detected = detector.DetectImage(scene.Image, options);
        Assert.That(detected.Detections, Has.Length.EqualTo(1), condition);
        QrDetection geometry = detected.Detections[0];
        Assert.Multiple(() =>
        {
            Assert.That(geometry.EstimatedSymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(geometry.EstimatedVersion, Is.EqualTo(version));
            Assert.That(geometry.EstimatedWidth, Is.EqualTo(symbol.Width));
            Assert.That(geometry.EstimatedHeight, Is.EqualTo(symbol.Height));
            Assert.That(geometry.Polarity, Is.EqualTo(scene.Geometry.Polarity));
            Assert.That(geometry.SampledMatrix, Is.Null);
            Assert.That(geometry.Confidence, Is.InRange(0.0, 1.0));
            Assert.That(geometry.ModuleSize, Is.GreaterThan(0));
        });
        // Geometry must remain near the independently projected symbol boundary.
        // Finder ordering may choose a different orientation for reflected symbols.
        foreach (QrPoint corner in Points(geometry.Corners!))
        {
            double distance = Points(scene.Geometry.Corners!).Min(p => Math.Sqrt(
                (p.X - corner.X) * (p.X - corner.X) + (p.Y - corner.Y) * (p.Y - corner.Y)));
            Assert.That(distance, Is.LessThan(scene.Scale * 2.5), condition);
        }
        string geometryBefore = Snapshot(geometry);
        IQrPerspectiveSampler sampler = new QrPerspectiveSampler();
        QrDetection sampled = sampler.SampleDetection(scene.Image, geometry, options);
        AssertDecoded(sampled.SampledMatrix!);
        Assert.That(Snapshot(geometry), Is.EqualTo(geometryBefore));
        Assert.That(Snapshot(scene.Image), Is.EqualTo(imageBefore));
        Assert.That(Snapshot(options), Is.EqualTo(optionsBefore));
    }

    [Test]
    public void SamplesKnownGeometryAndDecodesAcrossImageConditions(
        [Values(1, 2, 7)] int version,
        [ValueSource(nameof(Conditions))] string condition,
        [Values(false, true)] bool multiPoint)
    {
        QrSymbol symbol = Encode(version);
        Scene scene = CreateScene(symbol, condition);
        QrDetectionOptions options = Options(condition);
        options.UseMultiPointSampling = multiPoint;
        string before = Snapshot(scene.Geometry);
        IQrPerspectiveSampler sampler = new QrPerspectiveSampler();
        QrDetection sampled = sampler.SampleDetection(scene.Image, scene.Geometry, options);
        if (condition == "uneven-lighting")
        {
            // Thresholding under this gradient may damage a few modules; require
            // less than one percent disagreement as well as successful decoding.
            Assert.That(Differences(sampled.SampledMatrix!, symbol.Matrix!),
                Is.LessThan(symbol.Width * symbol.Height * 0.01));
        }
        else
        {
            Assert.That(sampled.SampledMatrix!.Values, Is.EqualTo(symbol.Matrix!.Values));
        }
        Assert.That(sampled.SampledMatrix!.Confidence, Has.Length.EqualTo(symbol.Width * symbol.Height));
        Assert.That(sampled.SampledMatrix.Confidence, Is.All.GreaterThan(0));
        AssertDecoded(sampled.SampledMatrix);
        Assert.That(Snapshot(scene.Geometry), Is.EqualTo(before));
        options.RefineGrid = false;
        QrDetection resampled = sampler.ResampleDetection(scene.Image, scene.Geometry, symbol.Matrix!, options);
        Assert.That(resampled.SampledMatrix!.Values, Is.EqualTo(sampled.SampledMatrix.Values));
        Assert.That(resampled.SampledMatrix.Values, Is.Not.SameAs(sampled.SampledMatrix.Values));
    }

    [Test]
    public void RefinesDisplacedGeometryOnRotatedAndSkewedImages(
        [Values(2, 7)] int version,
        [Values("rotate-17", "skew", "perspective")] string condition)
    {
        QrSymbol symbol = Encode(version);
        Scene scene = CreateScene(symbol, condition);
        foreach (QrPoint point in Points(scene.Geometry.Corners!))
        {
            point.X += scene.Scale * 0.6;
            point.Y -= scene.Scale * 0.55;
        }
        string before = Snapshot(scene.Geometry);
        string evidenceBefore = Snapshot(symbol.Matrix!);
        QrDetectionOptions options = Options(condition);
        IQrPerspectiveSampler sampler = new QrPerspectiveSampler();
        QrDetection initial = sampler.SampleDetection(scene.Image, scene.Geometry, options);
        QrDetection refined = sampler.ResampleDetection(scene.Image, scene.Geometry, symbol.Matrix!, options);
        Assert.That(Differences(initial.SampledMatrix!, symbol.Matrix!), Is.GreaterThan(0));
        Assert.That(Differences(refined.SampledMatrix!, symbol.Matrix!),
            Is.LessThan(Differences(initial.SampledMatrix!, symbol.Matrix!)));
        AssertDecoded(refined.SampledMatrix!);
        Assert.That(Snapshot(scene.Geometry), Is.EqualTo(before));
        Assert.That(Snapshot(symbol.Matrix!), Is.EqualTo(evidenceBefore));
        QrDetection repeated = sampler.SampleDetection(scene.Image, refined, options);
        Assert.That(repeated.SampledMatrix!.Values, Is.EqualTo(refined.SampledMatrix!.Values));
    }

    [Test]
    public void SuppliedBinaryDetectionRetainsImageCoordinatesAcrossResolutionAndOrientation(
        [Values("low-resolution", "high-resolution", "rotate-90", "mirrored", "inverted", "cropped-border")] string condition)
    {
        QrSymbol symbol = Encode(2);
        Scene scene = CreateScene(symbol, condition);
        QrMatrix binary = new()
        {
            Width = scene.Image.Width, Height = scene.Image.Height,
            Values = scene.Image.Pixels.Select(p => p < 128 ? QrModuleValue.Dark : QrModuleValue.Light).ToArray()
        };
        QrModuleValue[] before = (QrModuleValue[])binary.Values.Clone();
        QrDetectionOptions options = Options(condition);
        QrDetectionSet result = new QrDetector().DetectBinaryMatrix(binary, options);
        Assert.That(result.Detections, Has.Length.EqualTo(1));
        Assert.That(result.Thresholds, Is.Empty);
        Assert.That(result.BinarizationMode, Is.EqualTo(QrBinarizationMode.SuppliedBinary));
        AssertDecoded(new QrPerspectiveSampler().SampleDetection(scene.Image, result.Detections[0], options).SampledMatrix!);
        Assert.That(binary.Values, Is.EqualTo(before));
    }

    [Test]
    public void CroppedPayloadProducesUnknownSamplesOnlyOutsideTheImage([Values(false, true)] bool multiPoint)
    {
        QrSymbol symbol = Encode(2);
        Scene scene = CreateScene(symbol, "clean");
        int removedColumns = (int)scene.Geometry.Corners!.TopLeft!.X + scene.Scale * 2;
        int width = scene.Image.Width - removedColumns;
        byte[] pixels = new byte[width * scene.Image.Height];
        for (int row = 0; row < scene.Image.Height; row++)
            Array.Copy(scene.Image.Pixels, row * scene.Image.Stride + removedColumns, pixels, row * width, width);
        scene.Image = new QrImage { Width = width, Height = scene.Image.Height, Stride = width, Pixels = pixels };
        foreach (QrPoint point in Points(scene.Geometry.Corners)) point.X -= removedColumns;
        QrDetectionOptions options = Options("clean");
        options.UseMultiPointSampling = multiPoint;
        QrMatrix sampled = new QrPerspectiveSampler().SampleDetection(scene.Image, scene.Geometry, options).SampledMatrix!;
        for (int y = 0; y < symbol.Height; y++)
            for (int x = 0; x < symbol.Width; x++)
            {
                int index = y * symbol.Width + x;
                Assert.That(sampled.Values[index], Is.EqualTo(x < 2 ? QrModuleValue.Unknown : symbol.Matrix!.Values[index]));
                Assert.That(sampled.Confidence![index], Is.EqualTo(x < 2 ? 0 : 255));
            }
    }

    private static void AssertDecoded(QrMatrix matrix)
    {
        QrDecodeResult result = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(result.Text, Is.EqualTo(Payload));
        Assert.That(result.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes(Payload)));
    }

    private static QrSymbol Encode(int version) => new QrEncoder().EncodeText(Payload, new QrEncodeOptions
    {
        ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2,
        MinimumVersion = version, MaximumVersion = version,
        ErrorCorrectionLevel = QrErrorCorrectionLevel.High, ForceErrorCorrectionLevel = true,
        BoostErrorCorrectionLevel = false, MaskPattern = QrMaskPattern.Three, QuietZoneModules = 0
    });

    private static QrDetectionOptions Options(string condition) => new()
    {
        AllowMicro = false, AllowRectangularMicro = false, AllowLegacyModel1 = false,
        BinarizationMode = condition == "uneven-lighting" ? QrBinarizationMode.HybridLocal : QrBinarizationMode.GlobalHistogram,
        MaximumPyramidLevels = condition == "high-resolution" ? 4 : 1
    };

    private sealed class Scene
    {
        public QrImage Image = new();
        public QrDetection Geometry = new();
        public int Scale;
    }

    private static Scene CreateScene(QrSymbol symbol, string condition)
    {
        int scale = condition == "low-resolution" ? 1 : condition == "high-resolution" ? 16 : 4;
        double angle = condition switch
        {
            "rotate-90" => 90, "rotate-180" => 180, "rotate-270" => 270,
            "rotate-17" => 17, "rotate-minus-13" => -13, _ => 0
        };
        double radians = angle * Math.PI / 180;
        double mirror = condition == "mirrored" ? -1 : 1;
        double shear = condition == "skew" ? 0.18 : 0;
        // Test-owned homography, independent of production transform and renderer code.
        double[] h = [scale * Math.Cos(radians) * mirror, scale * (shear - Math.Sin(radians)), 0,
            scale * Math.Sin(radians) * mirror, scale * Math.Cos(radians), 0,
            condition == "perspective" ? 0.0015 : 0, condition == "perspective" ? 0.001 : 0, 1];
        double border = condition == "cropped-border" ? 0.5 : 5;
        QrPoint[] bounds = [Map(h, -border, -border), Map(h, symbol.Width + border, -border),
            Map(h, symbol.Width + border, symbol.Height + border), Map(h, -border, symbol.Height + border)];
        double offsetX = -Math.Floor(bounds.Min(p => p.X));
        double offsetY = -Math.Floor(bounds.Min(p => p.Y));
        int width = (int)Math.Ceiling(bounds.Max(p => p.X) + offsetX);
        int height = (int)Math.Ceiling(bounds.Max(p => p.Y) + offsetY);
        h[0] += offsetX * h[6]; h[1] += offsetX * h[7]; h[2] += offsetX;
        h[3] += offsetY * h[6]; h[4] += offsetY * h[7]; h[5] += offsetY;
        double[] inverse = Inverse(h);
        byte[] pixels = new byte[width * height];
        Random noise = new(141);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                QrPoint p = Map(inverse, x + 0.5, y + 0.5);
                bool dark = p.X >= 0 && p.Y >= 0 && p.X < symbol.Width && p.Y < symbol.Height
                    && symbol.Matrix!.Values[(int)p.Y * symbol.Width + (int)p.X] == QrModuleValue.Dark;
                int value = dark ? 0 : 255;
                if (condition == "uneven-lighting") value = 90 + x * 140 / width - (dark ? 80 : 0);
                if (condition == "noisy") value = (dark ? 30 : 225) + noise.Next(-25, 26);
                if (condition == "inverted") value = 255 - value;
                pixels[y * width + x] = (byte)value;
            }
        if (condition == "blurred") pixels = Blur(pixels, width, height);
        return new Scene
        {
            Scale = scale,
            Image = new QrImage { Width = width, Height = height, Stride = width, PixelFormat = QrPixelFormat.Gray8, Pixels = pixels },
            Geometry = new QrDetection
            {
                EstimatedWidth = symbol.Width, EstimatedHeight = symbol.Height, EstimatedVersion = symbol.Version,
                HasSymbolKindEstimate = true, EstimatedSymbolKind = QrSymbolKind.Model2,
                Polarity = condition == "inverted" ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight,
                ModuleSize = scale, Confidence = 1,
                Corners = new QrQuadrilateral
                {
                    TopLeft = Map(h, 0, 0), TopRight = Map(h, symbol.Width, 0),
                    BottomRight = Map(h, symbol.Width, symbol.Height), BottomLeft = Map(h, 0, symbol.Height)
                }
            }
        };
    }

    private static byte[] Blur(byte[] pixels, int width, int height)
    {
        byte[] blurred = new byte[pixels.Length];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int sum = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        sum += pixels[Math.Clamp(y + dy, 0, height - 1) * width + Math.Clamp(x + dx, 0, width - 1)];
                blurred[y * width + x] = (byte)((sum + 4) / 9);
            }
        return blurred;
    }

    private static QrPoint Map(double[] h, double x, double y)
    {
        double denominator = h[6] * x + h[7] * y + h[8];
        return new QrPoint { X = (h[0] * x + h[1] * y + h[2]) / denominator, Y = (h[3] * x + h[4] * y + h[5]) / denominator };
    }

    private static double[] Inverse(double[] a) =>
    [
        a[4] * a[8] - a[5] * a[7], a[2] * a[7] - a[1] * a[8], a[1] * a[5] - a[2] * a[4],
        a[5] * a[6] - a[3] * a[8], a[0] * a[8] - a[2] * a[6], a[2] * a[3] - a[0] * a[5],
        a[3] * a[7] - a[4] * a[6], a[1] * a[6] - a[0] * a[7], a[0] * a[4] - a[1] * a[3]
    ];

    private static QrPoint[] Points(QrQuadrilateral corners) => [corners.TopLeft!, corners.TopRight!, corners.BottomRight!, corners.BottomLeft!];
    private static int Differences(QrMatrix actual, QrMatrix expected) =>
        actual.Values.Zip(expected.Values).Count(pair => pair.First != pair.Second);
    private static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
}
