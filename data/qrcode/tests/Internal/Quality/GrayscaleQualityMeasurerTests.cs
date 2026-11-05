using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class GrayscaleQualityMeasurerTests
{
    [Test]
    public void MeasuresFullContrastAndRetainsEachModuleModulation()
    {
        GrayscaleQualityMeasurement result = Measure([0, 255, 51, 204]);
        Assert.That(result.SampleCount, Is.EqualTo(4));
        Assert.That(result.MinimumSample, Is.Zero);
        Assert.That(result.MaximumSample, Is.EqualTo(255));
        Assert.That(result.GlobalThreshold, Is.EqualTo(127.5));
        Assert.That(result.SymbolContrast.Name, Is.EqualTo("SymbolContrast"));
        Assert.That(result.SymbolContrast.Value, Is.EqualTo(1));
        Assert.That(result.SymbolContrast.IsCalculated, Is.True);
        Assert.That(result.Modulation.Name, Is.EqualTo("Modulation"));
        Assert.That(result.Modulation.Value, Is.EqualTo(0.6).Within(1e-12));
        Assert.That(result.ModuleModulation, Is.EqualTo(new[] { 1.0, 1.0, 0.6, 0.6 }).Within(1e-12));
    }

    [TestCase(100, 200, 125, 0.5)]
    [TestCase(100, 200, 150, 0.0)]
    [TestCase(100, 200, 199, 0.98)]
    [TestCase(100, 101, 100, 1.0)]
    [TestCase(0, 255, 127, 1.0 / 255)]
    [TestCase(0, 255, 128, 1.0 / 255)]
    public void UsesAbsoluteContrastAndFractionalGlobalThreshold(int minimum, int maximum, int weakSample, double modulation)
    {
        GrayscaleQualityMeasurement result = Measure([(byte)minimum, (byte)maximum, (byte)weakSample]);
        Assert.That(result.SymbolContrast.Value, Is.EqualTo((maximum - minimum) / 255.0).Within(1e-12));
        Assert.That(result.GlobalThreshold, Is.EqualTo((minimum + maximum) / 2.0));
        Assert.That(result.Modulation.Value, Is.EqualTo(modulation).Within(1e-12));
        Assert.That(result.Modulation.IsCalculated, Is.True);
        Assert.That(result.ModuleModulation, Is.All.InRange(0.0, 1.0));
    }

    [TestCase(0)]
    [TestCase(128)]
    [TestCase(255)]
    public void UniformSamplesHaveCalculatedZeroContrastAndUndefinedModulation(byte value)
    {
        GrayscaleQualityMeasurement result = Measure([value, value, value]);
        Assert.That(result.SymbolContrast.IsCalculated, Is.True);
        Assert.That(result.SymbolContrast.Value, Is.Zero);
        Assert.That(result.GlobalThreshold, Is.EqualTo(value));
        Assert.That(result.Modulation.IsCalculated, Is.False);
        Assert.That(result.Modulation.Limitation, Does.Contain("contrast is zero"));
        Assert.That(result.ModuleModulation, Is.Empty);
        Assert.That(double.IsFinite(result.Modulation.Value), Is.True);
    }

    [Test]
    public void SingleSampleDoesNotInventContrastOrModulation()
    {
        GrayscaleQualityMeasurement result = Measure([75]);
        Assert.That(result.SampleCount, Is.EqualTo(1));
        Assert.That(result.SymbolContrast.Value, Is.Zero);
        Assert.That(result.Modulation.IsCalculated, Is.False);
    }

    [Test]
    public void IgnoresRowPaddingAndTrailingBytesAndAcceptsUnpaddedFinalRow()
    {
        byte[] samples = [40, 200, 0, 255, 80, 160];
        QrRaster raster = new QrRaster { Width = 2, Height = 2, Stride = 4, Pixels = samples };
        GrayscaleQualityMeasurement result = GrayscaleQualityMeasurer.Measure(raster, new QrQualityOptions());
        Assert.That(result.SampleCount, Is.EqualTo(4));
        Assert.That(result.MinimumSample, Is.EqualTo(40));
        Assert.That(result.MaximumSample, Is.EqualTo(200));
        Assert.That(result.GlobalThreshold, Is.EqualTo(120));
        Assert.That(result.SymbolContrast.Value, Is.EqualTo(160.0 / 255));
        Assert.That(result.ModuleModulation, Is.EqualTo(new[] { 1.0, 1.0, 0.5, 0.5 }));
        raster.Pixels = [.. samples, 0, 255, 0, 255];
        GrayscaleQualityMeasurement padded = GrayscaleQualityMeasurer.Measure(raster, new QrQualityOptions());
        Assert.That(padded.ModuleModulation, Is.EqualTo(result.ModuleModulation));
        Assert.That(padded.SymbolContrast.Value, Is.EqualTo(result.SymbolContrast.Value));
    }

    [Test]
    public void ReflectanceReversalPreservesContrastAndModulation()
    {
        byte[] samples = [20, 220, 30, 180, 50, 200];
        GrayscaleQualityMeasurement normal = Measure(samples);
        GrayscaleQualityMeasurement reversed = Measure(samples.Select(p => (byte)(255 - p)).ToArray());
        Assert.That(reversed.SymbolContrast.Value, Is.EqualTo(normal.SymbolContrast.Value));
        Assert.That(reversed.Modulation.Value, Is.EqualTo(normal.Modulation.Value));
        Assert.That(reversed.ModuleModulation, Is.EqualTo(normal.ModuleModulation));
        Assert.That(reversed.GlobalThreshold, Is.EqualTo(255 - normal.GlobalThreshold));
    }

    [Test]
    public void ReportsWeakerModulationWithoutChangingOverallContrast()
    {
        GrayscaleQualityMeasurement good = Measure([20, 220, 40, 200]);
        GrayscaleQualityMeasurement weak = Measure([20, 220, 110, 200]);
        Assert.That(good.SymbolContrast.Value, Is.EqualTo(weak.SymbolContrast.Value));
        Assert.That(good.Modulation.Value, Is.EqualTo(0.8));
        Assert.That(weak.Modulation.Value, Is.EqualTo(0.1));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void HonorsIndependentMeasurementOptions(bool contrast, bool modulation)
    {
        QrQualityOptions options = new QrQualityOptions { AnalyzeContrast = contrast, AnalyzeModulation = modulation, RequireCalibratedInputs = true };
        QrRaster samples = new QrRaster { Pixels = [20, 220, 70], Width = 3, Height = 1, Stride = 3 };
        GrayscaleQualityMeasurement result = GrayscaleQualityMeasurer.Measure(samples, options);
        Assert.That(result.SymbolContrast.IsCalculated, Is.EqualTo(contrast));
        Assert.That(result.Modulation.IsCalculated, Is.EqualTo(modulation));
        Assert.That(result.ModuleModulation, Has.Length.EqualTo(modulation ? 3 : 0));
        if (contrast) Assert.That(result.SymbolContrast.Value, Is.EqualTo(200.0 / 255));
        if (modulation) Assert.That(result.Modulation.Value, Is.EqualTo(0.5));
        foreach (QrQualityMetric metric in new[] { result.SymbolContrast, result.Modulation })
        {
            Assert.That(metric.Grade, Is.EqualTo(QrQualityGrade.NotGraded));
            Assert.That(metric.UsesCalibratedInputs, Is.False);
            Assert.That(metric.Limitation, Is.Not.Null.And.Not.Empty);
            Assert.That(metric.Unit, Is.EqualTo("fraction"));
        }
        Assert.That(options.AnalyzeContrast, Is.EqualTo(contrast));
        Assert.That(options.AnalyzeModulation, Is.EqualTo(modulation));
        Assert.That(options.RequireCalibratedInputs, Is.True);
    }

    [Test]
    public void MeasuresRenderedModuleSamplesWithoutStretchingTheirContrast()
    {
        QrSymbol symbol = new QrEncoder().EncodeText("12345", new QrEncodeOptions { ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2 });
        QrRaster samples = new QrRenderer().RenderSymbol(symbol,
            new QrRenderOptions { Scale = 1, QuietZoneModules = 0, DarkValue = 90, LightValue = 150 });
        byte[] before = (byte[])samples.Pixels.Clone();
        GrayscaleQualityMeasurement result = GrayscaleQualityMeasurer.Measure(samples, new QrQualityOptions());
        Assert.That(result.SymbolContrast.Value, Is.EqualTo(60.0 / 255));
        Assert.That(result.Modulation.Value, Is.EqualTo(1));
        Assert.That(result.ModuleModulation, Is.All.EqualTo(1));
        Assert.That(samples.Pixels, Is.EqualTo(before));
        result.ModuleModulation[0] = -1;
        result.SymbolContrast.Value = -1;
        GrayscaleQualityMeasurement again = GrayscaleQualityMeasurer.Measure(samples, new QrQualityOptions());
        Assert.That(again.SymbolContrast.Value, Is.EqualTo(60.0 / 255));
        Assert.That(again.ModuleModulation[0], Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void RejectsMalformedSampleStorageEvenWhenMeasurementsAreDisabled(int invalid)
    {
        QrRaster samples = new QrRaster { Pixels = [0, 255, 0, 255], Width = 2, Height = 2, Stride = 2 };
        if (invalid == 0) samples.Pixels = null!;
        if (invalid == 1) samples.Pixels = [0, 255, 0];
        if (invalid == 2) samples.Stride = 1;
        if (invalid == 3) samples.Width = 0;
        if (invalid == 4) samples.Height = -1;
        Assert.Throws<QrInputException>(() => GrayscaleQualityMeasurer.Measure(samples,
            new QrQualityOptions { AnalyzeContrast = false, AnalyzeModulation = false }));
    }

    [Test]
    public void EnforcesSampleAndAllocationLimitsAndRejectsMissingInputs()
    {
        QrRaster samples = new QrRaster { Pixels = [0, 255], Width = 2, Height = 1, Stride = 2 };
        Assert.Throws<QrInputException>(() => GrayscaleQualityMeasurer.Measure(null!, new QrQualityOptions()));
        Assert.Throws<QrInputException>(() => GrayscaleQualityMeasurer.Measure(samples, null!));
        Assert.Throws<QrInputException>(() => GrayscaleQualityMeasurer.Measure(samples, new QrQualityOptions { MaximumModuleSamples = 0 }));
        Assert.Throws<QrResourceLimitException>(() => GrayscaleQualityMeasurer.Measure(samples, new QrQualityOptions { MaximumModuleSamples = 1 }));
        Assert.That(GrayscaleQualityMeasurer.Measure(samples, new QrQualityOptions { MaximumModuleSamples = 2 }).SampleCount, Is.EqualTo(2));
        QrRaster excessive = new QrRaster { Width = 2048, Height = 1024, Stride = 2048, Pixels = new byte[2048 * 1024] };
        Assert.Throws<QrResourceLimitException>(() => GrayscaleQualityMeasurer.Measure(excessive, new QrQualityOptions { MaximumModuleSamples = excessive.Pixels.Length }));
        samples.Height = 3;
        samples.Stride = int.MaxValue;
        Assert.Throws<QrResourceLimitException>(() => GrayscaleQualityMeasurer.Measure(samples, new QrQualityOptions()));
    }

    static GrayscaleQualityMeasurement Measure(byte[] samples) => GrayscaleQualityMeasurer.Measure(
        new QrRaster { Pixels = samples, Width = samples.Length, Height = 1, Stride = samples.Length }, new QrQualityOptions());
}
