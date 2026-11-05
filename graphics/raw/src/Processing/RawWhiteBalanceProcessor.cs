namespace raw;

internal class RawRgbChannelIndices
{
    internal int Red;
    internal int Green;
    internal int Blue;

    internal RawRgbChannelIndices()
    {
        Red = -1;
        Green = -1;
        Blue = -1;
    }
}

internal static class RawWhiteBalanceProcessor
{
    internal static RawRgbImage Apply(RawRgbImage image, RawWhiteBalance? cameraWhiteBalance, RawRenderOptions options)
    {
        RawRgbChannelIndices channels = Validate(image, cameraWhiteBalance, options);
        double[] multipliers = ResolveMultipliers(image, cameraWhiteBalance, options, channels);
        long requiredLong = RawImageBufferValidation.RequiredSamples(image.Width, image.Height, image.ChannelCount, image.RowStrideSamples);
        CheckedMath.DecodedSize(requiredLong, 4, options.Limits);
        CheckedMath.AllocationSize(requiredLong, 4, options.Limits);
        int required = CheckedMath.ToInt32(requiredLong);
        float[] output = new float[required];
        int y = 0;
        while (y < image.Height)
        {
            options.CheckCancellation();
            int row = y * image.RowStrideSamples;
            int x = 0;
            while (x < image.Width)
            {
                int pixel = row + x * image.ChannelCount;
                int channel = 0;
                while (channel < image.ChannelCount)
                {
                    double value = image.FloatingSamples[pixel + channel];
                    if (!RawModelValidation.IsFinite(value))
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "White balance requires finite floating-point input samples.", -1);
                    }
                    double multiplier = 1.0;
                    if (channel == channels.Red)
                    {
                        multiplier = multipliers[0];
                    }
                    else if (channel == channels.Green)
                    {
                        multiplier = multipliers[1];
                    }
                    else if (channel == channels.Blue)
                    {
                        multiplier = multipliers[2];
                    }
                    double balanced = value * multiplier;
                    if (!RawModelValidation.IsFinite(balanced) || balanced < -float.MaxValue || balanced > float.MaxValue)
                    {
                        throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "A white-balanced sample exceeds the floating-point output range.", -1);
                    }
                    output[pixel + channel] = (float)balanced;
                    channel++;
                }
                x++;
            }
            y++;
        }
        RawChannelDescription[] descriptions = new RawChannelDescription[image.Channels.Length];
        System.Array.Copy(image.Channels, descriptions, descriptions.Length);
        return new RawRgbImage(image.Width, image.Height, image.ChannelCount, image.RowStrideSamples, null, null, output, descriptions, image.ColorSpace, image.TransferFunction, image.Orientation, image.OrientationApplied, true, image.Diagnostics);
    }

    internal static RawRgbChannelIndices Validate(RawRgbImage image, RawWhiteBalance? cameraWhiteBalance, RawRenderOptions options)
    {
        if (image == null || options == null || !options.Validate().IsValid() || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid reconstructed image and render options are required for white balance.", -1);
        }
        if (image.FloatingSamples == null || image.FloatingSamples.Length == 0 || image.ColorSpace != RawColorSpace.RawCamera || image.TransferFunction != RawTransferFunction.Linear)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "White balance requires floating camera-linear samples.", -1);
        }
        RawRgbChannelIndices channels = FindChannels(image);
        if ((options ?? throw new System.InvalidOperationException("A required value is unavailable.")).WhiteBalanceMode != RawWhiteBalanceMode.None && (image.ChannelCount != 3 && image.ChannelCount != 4 || channels.Red < 0 || channels.Green < 0 || channels.Blue < 0))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "White balance requires exactly one red, green, and blue output channel.", -1);
        }
        if (options.WhiteBalanceMode == RawWhiteBalanceMode.Camera && (cameraWhiteBalance == null || cameraWhiteBalance.Source != RawWhiteBalanceMode.Camera || !ValidCalibration(cameraWhiteBalance, true) || cameraWhiteBalance.Multipliers.Length != 3 && cameraWhiteBalance.Multipliers.Length != 4))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Camera white balance requires three RGB or four RGGB profile multipliers.", -1);
        }
        if (options.WhiteBalanceArea != null && !options.WhiteBalanceArea.Validate(image.Width, image.Height).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The auto-white-balance region must be contained in the reconstructed image.", -1);
        }
        options.CheckCancellation();
        return channels;
    }

    internal static RawRgbChannelIndices FindChannels(RawRgbImage image)
    {
        RawRgbChannelIndices result = new RawRgbChannelIndices();
        int index = 0;
        while (index < image.Channels.Length)
        {
            RawChannelKind kind = image.Channels[index].Kind;
            if (kind == RawChannelKind.Red)
            {
                if (result.Red >= 0)
                {
                    result.Red = -2;
                }
                else
                {
                    result.Red = index;
                }
            }
            else if (kind == RawChannelKind.Green)
            {
                if (result.Green >= 0)
                {
                    result.Green = -2;
                }
                else
                {
                    result.Green = index;
                }
            }
            else if (kind == RawChannelKind.Blue)
            {
                if (result.Blue >= 0)
                {
                    result.Blue = -2;
                }
                else
                {
                    result.Blue = index;
                }
            }
            index++;
        }
        return result;
    }

    internal static double[] ResolveMultipliers(RawRgbImage image, RawWhiteBalance? cameraWhiteBalance, RawRenderOptions options, RawRgbChannelIndices channels)
    {
        switch (options.WhiteBalanceMode)
        {
            case RawWhiteBalanceMode.None:
                return new double[] { 1.0, 1.0, 1.0 };
            case RawWhiteBalanceMode.Camera:
                return CalibrationMultipliers(cameraWhiteBalance, true);
            case RawWhiteBalanceMode.Daylight:
                if (options.WhiteBalance != null)
                {
                    return CalibrationMultipliers(options.WhiteBalance, false);
                }
                return TemperatureMultipliers(6500.0, 0.0);
            case RawWhiteBalanceMode.Auto:
                return AutoMultipliers(image, options, channels);
            case RawWhiteBalanceMode.UserMultipliers:
                return CalibrationMultipliers(options.WhiteBalance, false);
            case RawWhiteBalanceMode.TemperatureAndTint:
                return TemperatureMultipliers((options.WhiteBalance ?? throw new System.InvalidOperationException("A required value is unavailable.")).TemperatureKelvin, options.WhiteBalance.Tint);
            default:
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The white-balance mode is not defined.", -1);
        }
    }

    internal static double[] CalibrationMultipliers(RawWhiteBalance? whiteBalance, bool allowFour)
    {
        if (!ValidCalibration(whiteBalance, allowFour))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "White-balance calibration requires finite positive RGB multipliers.", -1);
        }
        double[] normalized = (whiteBalance ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetNormalizedMultipliers();
        if (normalized.Length == 3)
        {
            return normalized;
        }
        double[] rgb = new double[] { normalized[0], (normalized[1] + normalized[2]) * 0.5, normalized[3] };
        return Normalize(rgb);
    }

    internal static double[] AutoMultipliers(RawRgbImage image, RawRenderOptions options, RawRgbChannelIndices channels)
    {
        RawArea? area = options.WhiteBalanceArea;
        int left = area == null ? 0 : area.X;
        int top = area == null ? 0 : area.Y;
        int right = area == null ? image.Width : area.X + area.Width;
        int bottom = area == null ? image.Height : area.Y + area.Height;
        double red = 0.0;
        double green = 0.0;
        double blue = 0.0;
        long count = 0;
        int y = top;
        while (y < bottom)
        {
            options.CheckCancellation();
            int x = left;
            while (x < right)
            {
                int pixel = y * image.RowStrideSamples + x * image.ChannelCount;
                double redValue = image.FloatingSamples[pixel + channels.Red];
                double greenValue = image.FloatingSamples[pixel + channels.Green];
                double blueValue = image.FloatingSamples[pixel + channels.Blue];
                if (!RawModelValidation.IsFinite(redValue) || !RawModelValidation.IsFinite(greenValue) || !RawModelValidation.IsFinite(blueValue) || redValue < 0.0 || greenValue < 0.0 || blueValue < 0.0)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Auto white balance requires finite nonnegative RGB samples.", -1);
                }
                double minimum = System.Math.Min(redValue, System.Math.Min(greenValue, blueValue));
                double maximum = System.Math.Max(redValue, System.Math.Max(greenValue, blueValue));
                if (minimum > 0.000001 && maximum < 0.98)
                {
                    red += redValue;
                    green += greenValue;
                    blue += blueValue;
                    count++;
                }
                x++;
            }
            y++;
        }
        if (count == 0 || red <= 0.0 || green <= 0.0 || blue <= 0.0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "The auto-white-balance region contains no usable unclipped RGB samples.", -1);
        }
        double redMean = red / count;
        double greenMean = green / count;
        double blueMean = blue / count;
        double maximumMean = System.Math.Max(redMean, System.Math.Max(greenMean, blueMean));
        return Normalize(new double[] { maximumMean / redMean, maximumMean / greenMean, maximumMean / blueMean });
    }

    internal static double[] TemperatureMultipliers(double temperatureKelvin, double tint)
    {
        if (!RawModelValidation.IsFinite(temperatureKelvin) || !RawModelValidation.IsFinite(tint) || temperatureKelvin < 1000.0 || temperatureKelvin > 50000.0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "White-balance temperature must be between 1000 and 50000 kelvin and tint must be finite.", -1);
        }
        double temperature = temperatureKelvin;
        if (temperature < 1667.0)
        {
            temperature = 1667.0;
        }
        if (temperature > 25000.0)
        {
            temperature = 25000.0;
        }
        double squared = temperature * temperature;
        double cubed = squared * temperature;
        double x;
        if (temperature <= 4000.0)
        {
            x = -0.2661239e9 / cubed - 0.2343580e6 / squared + 0.8776956e3 / temperature + 0.179910;
        }
        else
        {
            x = -3.0258469e9 / cubed + 2.1070379e6 / squared + 0.2226347e3 / temperature + 0.240390;
        }
        double y;
        if (temperature <= 2222.0)
        {
            y = -1.1063814 * x * x * x - 1.34811020 * x * x + 2.18555832 * x - 0.20219683;
        }
        else if (temperature <= 4000.0)
        {
            y = -0.9549476 * x * x * x - 1.37418593 * x * x + 2.09137015 * x - 0.16748867;
        }
        else
        {
            y = 3.0817580 * x * x * x - 5.87338670 * x * x + 3.75112997 * x - 0.37001483;
        }
        double illuminantX = x / y;
        double illuminantY = 1.0;
        double illuminantZ = (1.0 - x - y) / y;
        double red = 3.2406 * illuminantX - 1.5372 * illuminantY - 0.4986 * illuminantZ;
        double green = -0.9689 * illuminantX + 1.8758 * illuminantY + 0.0415 * illuminantZ;
        double blue = 0.0557 * illuminantX - 0.2040 * illuminantY + 1.0570 * illuminantZ;
        if (red < 0.000001)
        {
            red = 0.000001;
        }
        if (green < 0.000001)
        {
            green = 0.000001;
        }
        if (blue < 0.000001)
        {
            blue = 0.000001;
        }
        double tintScale = System.Math.Pow(2.0, tint);
        double[] multipliers = new double[] { 1.0 / red, tintScale / green, 1.0 / blue };
        return Normalize(multipliers);
    }

    internal static double[] Normalize(double[] multipliers)
    {
        double minimum = double.MaxValue;
        int index = 0;
        while (index < multipliers.Length)
        {
            if (!RawModelValidation.IsFinite(multipliers[index]) || multipliers[index] <= 0.0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "White-balance multipliers must be finite and positive.", -1);
            }
            if (multipliers[index] < minimum)
            {
                minimum = multipliers[index];
            }
            index++;
        }
        double[] normalized = new double[multipliers.Length];
        index = 0;
        while (index < multipliers.Length)
        {
            normalized[index] = multipliers[index] / minimum;
            index++;
        }
        return normalized;
    }

    internal static bool ValidCalibration(RawWhiteBalance? whiteBalance, bool allowFour)
    {
        if (whiteBalance == null || whiteBalance.Source < RawWhiteBalanceMode.None || whiteBalance.Source > RawWhiteBalanceMode.TemperatureAndTint || whiteBalance.Multipliers == null || !RawModelValidation.IsFinite(whiteBalance.TemperatureKelvin) || !RawModelValidation.IsFinite(whiteBalance.Tint) || whiteBalance.TemperatureKelvin < 0.0 || whiteBalance.TemperatureKelvin != 0.0 && (whiteBalance.TemperatureKelvin < 1000.0 || whiteBalance.TemperatureKelvin > 50000.0))
        {
            return false;
        }
        if (whiteBalance.Multipliers.Length != 0 && whiteBalance.Multipliers.Length != 3 && (!allowFour || whiteBalance.Multipliers.Length != 4))
        {
            return false;
        }
        int index = 0;
        while (index < whiteBalance.Multipliers.Length)
        {
            if (!RawModelValidation.IsFinite(whiteBalance.Multipliers[index]) || whiteBalance.Multipliers[index] <= 0.0)
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
