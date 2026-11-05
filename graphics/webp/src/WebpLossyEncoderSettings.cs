using System;

namespace webp;

internal class WebpLossyEncodePlan
{
    internal WebpLossyPreset Preset;
    internal int RequestedQuality;
    internal int Quality;
    internal int Method;
    internal int FixedQuantizer;
    internal int FilterStrength;
    internal int FilterSharpness;
    internal bool AutoFilter;
    internal int SpatialNoiseShaping;
    internal WebpLossyPreprocessing Preprocessing;
    internal int TargetSize;
    internal double TargetPsnr;
    internal int PassCount;
    internal bool UsesTargetSize;
    internal bool UsesTargetPsnr;
    internal WebpLossyPassPlan[] Passes;
    internal WebpVp8LoopFilterHeader LoopFilter;

    internal WebpLossyEncodePlan()
    {
        Preset = WebpLossyPreset.Default;
        RequestedQuality = WebpConstants.DefaultQuality;
        Quality = WebpConstants.DefaultQuality;
        Method = WebpConstants.DefaultMethod;
        FixedQuantizer = 32;
        FilterStrength = WebpConstants.DefaultFilterStrength;
        FilterSharpness = WebpConstants.DefaultFilterSharpness;
        AutoFilter = WebpConstants.DefaultAutoFilter;
        SpatialNoiseShaping = WebpConstants.DefaultSpatialNoiseShaping;
        Preprocessing = WebpConstants.DefaultLossyPreprocessing;
        TargetSize = WebpConstants.DefaultTargetSize;
        TargetPsnr = WebpConstants.DefaultTargetPsnr;
        PassCount = WebpConstants.DefaultPassCount;
        UsesTargetSize = false;
        UsesTargetPsnr = false;
        Passes = Array.Empty<WebpLossyPassPlan>();
        LoopFilter = new WebpVp8LoopFilterHeader();
    }
}

internal class WebpLossyPassPlan
{
    internal int PassIndex;
    internal int Quality;
    internal int FixedQuantizer;
    internal bool IsFinalPass;

    internal WebpLossyPassPlan()
    {
        PassIndex = 0;
        Quality = WebpConstants.DefaultQuality;
        FixedQuantizer = 32;
        IsFinalPass = false;
    }
}

internal static class WebpLossyEncoderSettings
{
    internal static WebpLossyEncodePlan CreatePlan(WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        WebpLossyEncodePlan plan = new WebpLossyEncodePlan();
        plan.Preset = options.Preset;
        plan.RequestedQuality = options.Quality;
        plan.Quality = options.Quality;
        plan.Method = options.Method;
        plan.FilterStrength = options.FilterStrength;
        plan.FilterSharpness = options.FilterSharpness;
        plan.AutoFilter = options.AutoFilter;
        plan.SpatialNoiseShaping = options.SpatialNoiseShaping;
        plan.Preprocessing = options.Preprocessing;
        plan.TargetSize = options.TargetSize;
        plan.TargetPsnr = options.TargetPsnr;
        plan.UsesTargetSize = options.TargetSize > 0;
        plan.UsesTargetPsnr = options.TargetPsnr > 0.0;
        ApplyPreset(plan);
        plan.PassCount = SelectPassCount(options, plan);
        plan.FixedQuantizer = QualityToFixedQuantizer(plan.Quality);
        plan.Passes = CreatePasses(plan);
        plan.LoopFilter = CreateLoopFilter(plan);
        return plan;
    }

    internal static int QualityToFixedQuantizer(int quality)
    {
        ValidateQuality(quality);
        return (((100 - quality) * 127) + 50) / 100;
    }

    internal static int FilterStrengthToLoopFilterLevel(int filterStrength)
    {
        ValidatePercent(filterStrength, "filterStrength");
        return ((filterStrength * 63) + 50) / 100;
    }

    internal static WebpPixelBuffer ApplyPreprocessing(WebpPixelBuffer buffer, WebpLossyEncodePlan plan, bool exactTransparentRgb)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(plan);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        if (!UsesSegmentSmoothing(plan.Preprocessing) && !UsesDithering(plan.Preprocessing))
        {
            return WebpPixelBufferHelper.Copy(buffer);
        }

        WebpPixelBuffer smoothed = buffer;
        if (UsesSegmentSmoothing(plan.Preprocessing))
        {
            smoothed = ApplySegmentSmoothing(buffer, exactTransparentRgb);
        }

        if (UsesDithering(plan.Preprocessing))
        {
            return ApplyDithering(smoothed, exactTransparentRgb);
        }

        return smoothed;
    }

    internal static void ValidateOptions(WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateQuality(options.Quality);
        ValidateMethod(options.Method);
        ValidatePercent(options.FilterStrength, "FilterStrength");
        ValidateSharpness(options.FilterSharpness);
        ValidatePercent(options.SpatialNoiseShaping, "SpatialNoiseShaping");
        ValidatePreset(options.Preset);
        ValidatePreprocessing(options.Preprocessing);
        ValidateTargetSize(options.TargetSize);
        ValidateTargetPsnr(options.TargetPsnr);
        ValidatePassCount(options.PassCount);
        ValidatePercent(options.AlphaQuality, "AlphaQuality");
        ValidateAlphaCompression(options.AlphaCompression);
        ValidateAlphaFilter(options.AlphaFilter);
        WebpLossyAlphaEncoder.ValidateLossyAlphaMode(options.LossyAlphaMode);
        if (options.TargetSize > 0 && options.TargetPsnr > 0.0)
        {
            throw new ArgumentException("lossy target size and target PSNR cannot both be requested", "options");
        }

        WebpNearLosslessPreprocessor.ComputeQuantizationStep(options.NearLosslessQuality);
    }

    internal static int SelectPassCount(WebpEncodeOptions options, WebpLossyEncodePlan plan)
    {
        if (plan.UsesTargetSize || plan.UsesTargetPsnr)
        {
            if (options.PassCount < 2)
            {
                return WebpConstants.DefaultTargetPassCount;
            }
        }

        return options.PassCount;
    }

    internal static WebpLossyPassPlan[] CreatePasses(WebpLossyEncodePlan plan)
    {
        WebpLossyPassPlan[] passes = new WebpLossyPassPlan[plan.PassCount];
        int index = 0;
        while (index < plan.PassCount)
        {
            WebpLossyPassPlan pass = new WebpLossyPassPlan();
            pass.PassIndex = index;
            pass.Quality = SelectPassQuality(plan, index);
            pass.FixedQuantizer = QualityToFixedQuantizer(pass.Quality);
            pass.IsFinalPass = index == plan.PassCount - 1;
            passes[index] = pass;
            index = index + 1;
        }

        return passes;
    }

    internal static int SelectPassQuality(WebpLossyEncodePlan plan, int passIndex)
    {
        if (plan.PassCount == 1)
        {
            return plan.Quality;
        }

        if (plan.UsesTargetSize)
        {
            return RoundDivide(plan.RequestedQuality * (plan.PassCount - passIndex - 1), plan.PassCount - 1);
        }

        if (plan.UsesTargetPsnr)
        {
            int targetQuality = TargetPsnrToQuality(plan.TargetPsnr);
            int low = ClampPercent(targetQuality - 20);
            int high = ClampPercent(targetQuality + 10);
            return low + RoundDivide((high - low) * passIndex, plan.PassCount - 1);
        }

        return plan.Quality;
    }

    internal static int TargetPsnrToQuality(double targetPsnr)
    {
        int quality = (int)((targetPsnr * 2.0) + 0.5);
        return ClampPercent(quality);
    }

    internal static int RoundDivide(int value, int divisor)
    {
        return (value + (divisor / 2)) / divisor;
    }

    internal static void ApplyPreset(WebpLossyEncodePlan plan)
    {
        if (plan.Preset == WebpLossyPreset.Picture)
        {
            plan.SpatialNoiseShaping = 80;
            plan.FilterSharpness = 4;
            plan.FilterStrength = 35;
            plan.Preprocessing = WebpLossyPreprocessing.None;
        }

        if (plan.Preset == WebpLossyPreset.Photo)
        {
            plan.SpatialNoiseShaping = 80;
            plan.FilterSharpness = 3;
            plan.FilterStrength = 30;
            plan.Preprocessing = WebpLossyPreprocessing.Dithering;
        }

        if (plan.Preset == WebpLossyPreset.Drawing)
        {
            plan.SpatialNoiseShaping = 25;
            plan.FilterSharpness = 6;
            plan.FilterStrength = 10;
            plan.Preprocessing = WebpLossyPreprocessing.None;
        }

        if (plan.Preset == WebpLossyPreset.Icon || plan.Preset == WebpLossyPreset.Text)
        {
            plan.SpatialNoiseShaping = 0;
            plan.FilterStrength = 0;
            plan.Preprocessing = WebpLossyPreprocessing.None;
        }
    }

    internal static WebpVp8LoopFilterHeader CreateLoopFilter(WebpLossyEncodePlan plan)
    {
        WebpVp8LoopFilterHeader header = new WebpVp8LoopFilterHeader();
        header.UseSimpleFilter = true;
        header.Level = SelectLoopFilterLevel(plan);
        header.Sharpness = plan.FilterSharpness;
        header.DeltaEnabled = false;
        header.DeltaUpdate = false;
        return header;
    }

    internal static int SelectLoopFilterLevel(WebpLossyEncodePlan plan)
    {
        int level = FilterStrengthToLoopFilterLevel(plan.FilterStrength);
        if (plan.AutoFilter)
        {
            int autoLevel = ((100 - plan.Quality) + (plan.Method * 8) + (plan.SpatialNoiseShaping / 4)) / 4;
            if (autoLevel > level)
            {
                level = autoLevel;
            }
        }

        if (level > 63)
        {
            return 63;
        }

        return level;
    }

    internal static WebpPixelBuffer ApplySegmentSmoothing(WebpPixelBuffer buffer, bool exactTransparentRgb)
    {
        WebpPixelBuffer output = WebpPixelBufferHelper.Copy(buffer);
        int y = 0;
        while (y < buffer.Height)
        {
            int x = 0;
            while (x < buffer.Width)
            {
                int offset = (y * buffer.Stride) + (x * 4);
                if (!ShouldPreserveTransparentRgb(buffer, offset, exactTransparentRgb))
                {
                    SmoothPixel(buffer, output, x, y);
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return output;
    }

    internal static void SmoothPixel(WebpPixelBuffer source, WebpPixelBuffer output, int x, int y)
    {
        int red = 0;
        int green = 0;
        int blue = 0;
        int count = 0;
        int yy = y - 1;
        while (yy <= y + 1)
        {
            int xx = x - 1;
            while (xx <= x + 1)
            {
                if (xx >= 0 && xx < source.Width && yy >= 0 && yy < source.Height)
                {
                    int neighborOffset = (yy * source.Stride) + (xx * 4);
                    red = red + source.Rgba[neighborOffset];
                    green = green + source.Rgba[neighborOffset + 1];
                    blue = blue + source.Rgba[neighborOffset + 2];
                    count = count + 1;
                }

                xx = xx + 1;
            }

            yy = yy + 1;
        }

        int offset = (y * output.Stride) + (x * 4);
        output.Rgba[offset] = (byte)((red + (count / 2)) / count);
        output.Rgba[offset + 1] = (byte)((green + (count / 2)) / count);
        output.Rgba[offset + 2] = (byte)((blue + (count / 2)) / count);
    }

    internal static WebpPixelBuffer ApplyDithering(WebpPixelBuffer buffer, bool exactTransparentRgb)
    {
        WebpPixelBuffer output = WebpPixelBufferHelper.Copy(buffer);
        int y = 0;
        while (y < output.Height)
        {
            int x = 0;
            while (x < output.Width)
            {
                int offset = (y * output.Stride) + (x * 4);
                if (!ShouldPreserveTransparentRgb(output, offset, exactTransparentRgb))
                {
                    int delta = ((x * 17) + (y * 31)) % 3;
                    ApplyRgbDelta(output, offset, delta - 1);
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return output;
    }

    internal static void ApplyRgbDelta(WebpPixelBuffer buffer, int offset, int delta)
    {
        buffer.Rgba[offset] = ClampToByte(buffer.Rgba[offset] + delta);
        buffer.Rgba[offset + 1] = ClampToByte(buffer.Rgba[offset + 1] + delta);
        buffer.Rgba[offset + 2] = ClampToByte(buffer.Rgba[offset + 2] + delta);
    }

    internal static bool ShouldPreserveTransparentRgb(WebpPixelBuffer buffer, int offset, bool exactTransparentRgb)
    {
        return exactTransparentRgb && buffer.Rgba[offset + 3] == 0;
    }

    internal static bool UsesSegmentSmoothing(WebpLossyPreprocessing preprocessing)
    {
        return preprocessing == WebpLossyPreprocessing.SegmentSmooth || preprocessing == WebpLossyPreprocessing.SegmentSmoothAndDithering;
    }

    internal static bool UsesDithering(WebpLossyPreprocessing preprocessing)
    {
        return preprocessing == WebpLossyPreprocessing.Dithering || preprocessing == WebpLossyPreprocessing.SegmentSmoothAndDithering;
    }

    internal static byte ClampToByte(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return (byte)value;
    }

    internal static int ClampPercent(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 100)
        {
            return 100;
        }

        return value;
    }

    internal static void ValidateQuality(int quality)
    {
        ValidatePercent(quality, "quality");
    }

    internal static void ValidateMethod(int method)
    {
        if (method < 0 || method > 6)
        {
            throw new ArgumentOutOfRangeException("Method");
        }
    }

    internal static void ValidateSharpness(int sharpness)
    {
        if (sharpness < 0 || sharpness > 7)
        {
            throw new ArgumentOutOfRangeException("FilterSharpness");
        }
    }

    internal static void ValidatePreset(WebpLossyPreset preset)
    {
        if (preset < WebpLossyPreset.Default || preset > WebpLossyPreset.Text)
        {
            throw new ArgumentOutOfRangeException("Preset");
        }
    }

    internal static void ValidatePreprocessing(WebpLossyPreprocessing preprocessing)
    {
        if (preprocessing < WebpLossyPreprocessing.None || preprocessing > WebpLossyPreprocessing.SegmentSmoothAndDithering)
        {
            throw new ArgumentOutOfRangeException("Preprocessing");
        }
    }

    internal static void ValidateTargetSize(int targetSize)
    {
        if (targetSize < 0)
        {
            throw new ArgumentOutOfRangeException("TargetSize");
        }
    }

    internal static void ValidateTargetPsnr(double targetPsnr)
    {
        if (double.IsNaN(targetPsnr) || targetPsnr < 0.0 || targetPsnr > 99.0)
        {
            throw new ArgumentOutOfRangeException("TargetPsnr");
        }
    }

    internal static void ValidateAlphaCompression(WebpAlphaCompressionMethod compression)
    {
        if (compression != WebpAlphaCompressionMethod.None && compression != WebpAlphaCompressionMethod.Lossless)
        {
            throw new ArgumentOutOfRangeException("AlphaCompression");
        }
    }

    internal static void ValidateAlphaFilter(WebpAlphaFilterMode filter)
    {
        if (filter != WebpAlphaFilterMode.None && filter != WebpAlphaFilterMode.Fast && filter != WebpAlphaFilterMode.Best)
        {
            throw new ArgumentOutOfRangeException("AlphaFilter");
        }
    }

    internal static void ValidatePassCount(int passCount)
    {
        if (passCount < 1 || passCount > 10)
        {
            throw new ArgumentOutOfRangeException("PassCount");
        }
    }

    internal static void ValidatePercent(int value, string name)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
