using webp;

namespace webp.Tests;

[TestFixture]
public class WebpLossyEncoderSettingsTests
{
    [Test]
    public void CreatePlan_DefaultsMatchLossyEncoderDefaults()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();

        WebpLossyEncodePlan plan = WebpLossyEncoderSettings.CreatePlan(options);

        Assert.That(plan.Quality, Is.EqualTo(75));
        Assert.That(plan.Method, Is.EqualTo(4));
        Assert.That(plan.FixedQuantizer, Is.EqualTo(32));
        Assert.That(plan.FilterStrength, Is.EqualTo(60));
        Assert.That(plan.FilterSharpness, Is.EqualTo(0));
        Assert.That(plan.AutoFilter, Is.False);
        Assert.That(plan.SpatialNoiseShaping, Is.EqualTo(50));
        Assert.That(plan.Preprocessing, Is.EqualTo(WebpLossyPreprocessing.None));
        Assert.That(plan.TargetSize, Is.EqualTo(0));
        Assert.That(plan.TargetPsnr, Is.EqualTo(0.0));
        Assert.That(plan.PassCount, Is.EqualTo(1));
        Assert.That(plan.UsesTargetSize, Is.False);
        Assert.That(plan.UsesTargetPsnr, Is.False);
        Assert.That(plan.Passes.Length, Is.EqualTo(1));
        Assert.That(plan.Passes[0].Quality, Is.EqualTo(75));
        Assert.That(plan.Passes[0].FixedQuantizer, Is.EqualTo(32));
        Assert.That(plan.Passes[0].IsFinalPass, Is.True);
        Assert.That(plan.LoopFilter.Level, Is.EqualTo(38));
        Assert.That(plan.LoopFilter.Sharpness, Is.EqualTo(0));
    }

    [Test]
    public void CreatePlan_AppliesPhotoPresetSettings()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Preset = WebpLossyPreset.Photo;

        WebpLossyEncodePlan plan = WebpLossyEncoderSettings.CreatePlan(options);

        Assert.That(plan.Preset, Is.EqualTo(WebpLossyPreset.Photo));
        Assert.That(plan.SpatialNoiseShaping, Is.EqualTo(80));
        Assert.That(plan.FilterStrength, Is.EqualTo(30));
        Assert.That(plan.FilterSharpness, Is.EqualTo(3));
        Assert.That(plan.Preprocessing, Is.EqualTo(WebpLossyPreprocessing.Dithering));
        Assert.That(plan.LoopFilter.Level, Is.EqualTo(19));
        Assert.That(plan.LoopFilter.Sharpness, Is.EqualTo(3));
    }

    [Test]
    public void CreatePlan_AppliesSharpPresets()
    {
        WebpEncodeOptions icon = new WebpEncodeOptions();
        WebpEncodeOptions text = new WebpEncodeOptions();
        icon.Preset = WebpLossyPreset.Icon;
        text.Preset = WebpLossyPreset.Text;

        WebpLossyEncodePlan iconPlan = WebpLossyEncoderSettings.CreatePlan(icon);
        WebpLossyEncodePlan textPlan = WebpLossyEncoderSettings.CreatePlan(text);

        Assert.That(iconPlan.SpatialNoiseShaping, Is.EqualTo(0));
        Assert.That(iconPlan.FilterStrength, Is.EqualTo(0));
        Assert.That(iconPlan.LoopFilter.Level, Is.EqualTo(0));
        Assert.That(textPlan.SpatialNoiseShaping, Is.EqualTo(0));
        Assert.That(textPlan.FilterStrength, Is.EqualTo(0));
        Assert.That(textPlan.LoopFilter.Level, Is.EqualTo(0));
    }

    [Test]
    public void QualityAndFilterMappings_CoverDocumentedRanges()
    {
        Assert.That(WebpLossyEncoderSettings.QualityToFixedQuantizer(100), Is.EqualTo(0));
        Assert.That(WebpLossyEncoderSettings.QualityToFixedQuantizer(75), Is.EqualTo(32));
        Assert.That(WebpLossyEncoderSettings.QualityToFixedQuantizer(0), Is.EqualTo(127));
        Assert.That(WebpLossyEncoderSettings.FilterStrengthToLoopFilterLevel(0), Is.EqualTo(0));
        Assert.That(WebpLossyEncoderSettings.FilterStrengthToLoopFilterLevel(60), Is.EqualTo(38));
        Assert.That(WebpLossyEncoderSettings.FilterStrengthToLoopFilterLevel(100), Is.EqualTo(63));
    }

    [Test]
    public void CreatePlan_AutoFilterCanRaiseWeakManualFilter()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Quality = 10;
        options.Method = 6;
        options.FilterStrength = 0;
        options.AutoFilter = true;

        WebpLossyEncodePlan plan = WebpLossyEncoderSettings.CreatePlan(options);

        Assert.That(plan.LoopFilter.Level, Is.GreaterThan(0));
    }

    [Test]
    public void CreatePlan_TargetSizeBuildsDescendingQualityPasses()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Quality = 80;
        options.TargetSize = 12000;
        options.PassCount = 4;

        WebpLossyEncodePlan plan = WebpLossyEncoderSettings.CreatePlan(options);

        Assert.That(plan.UsesTargetSize, Is.True);
        Assert.That(plan.UsesTargetPsnr, Is.False);
        Assert.That(plan.TargetSize, Is.EqualTo(12000));
        Assert.That(plan.PassCount, Is.EqualTo(4));
        Assert.That(plan.Passes[0].Quality, Is.EqualTo(80));
        Assert.That(plan.Passes[1].Quality, Is.EqualTo(53));
        Assert.That(plan.Passes[2].Quality, Is.EqualTo(27));
        Assert.That(plan.Passes[3].Quality, Is.EqualTo(0));
        Assert.That(plan.Passes[3].FixedQuantizer, Is.EqualTo(127));
        Assert.That(plan.Passes[3].IsFinalPass, Is.True);
    }

    [Test]
    public void CreatePlan_TargetSizeUsesDefaultMultiPassCountWhenUnset()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.TargetSize = 2000;

        WebpLossyEncodePlan plan = WebpLossyEncoderSettings.CreatePlan(options);

        Assert.That(plan.PassCount, Is.EqualTo(6));
        Assert.That(plan.Passes.Length, Is.EqualTo(6));
        Assert.That(plan.Passes[0].Quality, Is.EqualTo(75));
        Assert.That(plan.Passes[5].Quality, Is.EqualTo(0));
    }

    [Test]
    public void CreatePlan_TargetPsnrBuildsAscendingSearchPasses()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.TargetPsnr = 42.5;
        options.PassCount = 3;

        WebpLossyEncodePlan plan = WebpLossyEncoderSettings.CreatePlan(options);

        Assert.That(plan.UsesTargetSize, Is.False);
        Assert.That(plan.UsesTargetPsnr, Is.True);
        Assert.That(plan.TargetPsnr, Is.EqualTo(42.5));
        Assert.That(plan.PassCount, Is.EqualTo(3));
        Assert.That(plan.Passes[0].Quality, Is.EqualTo(65));
        Assert.That(plan.Passes[1].Quality, Is.EqualTo(80));
        Assert.That(plan.Passes[2].Quality, Is.EqualTo(95));
        Assert.That(plan.Passes[2].FixedQuantizer, Is.EqualTo(6));
        Assert.That(plan.Passes[2].IsFinalPass, Is.True);
    }

    [Test]
    public void ApplyPreprocessing_SmoothsAndDithersRgbButPreservesAlpha()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(2, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        SetPixel(buffer, 1, 0, 70, 80, 90, 128);
        WebpLossyEncodePlan plan = new WebpLossyEncodePlan();
        plan.Preprocessing = WebpLossyPreprocessing.SegmentSmoothAndDithering;

        WebpPixelBuffer output = WebpLossyEncoderSettings.ApplyPreprocessing(buffer, plan, false);

        Assert.That(output.Rgba[0], Is.EqualTo(39));
        Assert.That(output.Rgba[1], Is.EqualTo(49));
        Assert.That(output.Rgba[2], Is.EqualTo(59));
        Assert.That(output.Rgba[3], Is.EqualTo(255));
        Assert.That(output.Rgba[4], Is.EqualTo(41));
        Assert.That(output.Rgba[5], Is.EqualTo(51));
        Assert.That(output.Rgba[6], Is.EqualTo(61));
        Assert.That(output.Rgba[7], Is.EqualTo(128));
    }

    [Test]
    public void ApplyPreprocessing_ExactTransparentRgbPreservesTransparentColors()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(2, 1);
        SetPixel(buffer, 0, 0, 200, 30, 40, 0);
        SetPixel(buffer, 1, 0, 20, 40, 60, 255);
        WebpLossyEncodePlan plan = new WebpLossyEncodePlan();
        plan.Preprocessing = WebpLossyPreprocessing.SegmentSmoothAndDithering;

        WebpPixelBuffer output = WebpLossyEncoderSettings.ApplyPreprocessing(buffer, plan, true);

        Assert.That(output.Rgba[0], Is.EqualTo(200));
        Assert.That(output.Rgba[1], Is.EqualTo(30));
        Assert.That(output.Rgba[2], Is.EqualTo(40));
        Assert.That(output.Rgba[3], Is.EqualTo(0));
        Assert.That(output.Rgba[7], Is.EqualTo(255));
    }

    [Test]
    public void KeyFrameEncoder_UsesLossyOptionsForQuantizerAndFilter()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(1, 1);
        buffer.Rgba[3] = 255;
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Quality = 100;
        options.FilterStrength = 100;
        options.FilterSharpness = 7;

        byte[] payload = WebpVp8KeyFrameEncoder.Encode(buffer, options);
        WebpVp8FrameHeaderDataParseResult result = WebpVp8FrameHeaderDataParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HeaderData.Quantizer.BaseQIndex, Is.EqualTo(0));
        Assert.That(result.HeaderData.LoopFilter.Level, Is.EqualTo(63));
        Assert.That(result.HeaderData.LoopFilter.Sharpness, Is.EqualTo(7));
    }

    [Test]
    public void LossyEncoderSettings_RejectsInvalidOptions()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();

        options.Quality = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.Quality = 75;
        options.Method = 7;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.Method = 4;
        options.FilterStrength = 101;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.FilterStrength = 60;
        options.FilterSharpness = 8;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.FilterSharpness = 0;
        options.SpatialNoiseShaping = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.SpatialNoiseShaping = 50;
        options.Preset = (WebpLossyPreset)99;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.Preset = WebpLossyPreset.Default;
        options.Preprocessing = (WebpLossyPreprocessing)99;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.Preprocessing = WebpLossyPreprocessing.None;
        options.TargetSize = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.TargetSize = 10;
        options.TargetPsnr = 40.0;
        Assert.Throws<ArgumentException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.TargetSize = 0;
        options.TargetPsnr = 100.0;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.TargetPsnr = 0.0;
        options.PassCount = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        options.PassCount = 11;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyEncoderSettings.CreatePlan(options));
        Assert.Throws<ArgumentNullException>(() => WebpLossyEncoderSettings.CreatePlan(null!));
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int offset = (y * buffer.Stride) + (x * 4);
        buffer.Rgba[offset] = red;
        buffer.Rgba[offset + 1] = green;
        buffer.Rgba[offset + 2] = blue;
        buffer.Rgba[offset + 3] = alpha;
    }
}
