namespace raw;

internal class RawInterpolatedColorProfile
{
    internal double[] Matrix;
    internal double[] WhitePoint;
    internal double TemperatureKelvin;
    internal double Fraction;

    internal RawInterpolatedColorProfile(double[] matrix, double[] whitePoint, double temperatureKelvin, double fraction)
    {
        Matrix = matrix;
        WhitePoint = whitePoint;
        TemperatureKelvin = temperatureKelvin;
        Fraction = fraction;
    }
}

internal static class RawCameraColorProcessor
{
    internal static RawRgbImage Convert(RawRgbImage image, RawColorProfile[] profiles, double illuminantTemperatureKelvin, RawRenderOptions options)
    {
        RawRgbChannelIndices channels = Validate(image, profiles, illuminantTemperatureKelvin, options);
        RawInterpolatedColorProfile profile = InterpolateProfiles(profiles, illuminantTemperatureKelvin);
        RawColorIlluminant targetIlluminant = options.ColorSpace == RawColorSpace.XyzD50 ? RawColorIlluminant.D50 : RawColorIlluminant.D65;
        double[] targetWhite = RawChromaticAdaptation.WhitePoint(targetIlluminant, 0.0);
        double[] adaptation = RawChromaticAdaptation.Create(profile.WhitePoint, targetWhite, options.ChromaticAdaptation);
        double[] transform = RawMatrixMath.Multiply(adaptation, profile.Matrix);
        int outputChannels = image.ChannelCount;
        long pixelCount = CheckedMath.PixelCount(image.Width, image.Height, options.Limits);
        long outputCountLong = CheckedMath.Multiply(pixelCount, outputChannels);
        CheckedMath.DecodedSize(outputCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(outputCountLong, 4, options.Limits);
        float[] output = new float[CheckedMath.ToInt32(outputCountLong)];
        int alpha = FindAlpha(image);
        int y = 0;
        while (y < image.Height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < image.Width)
            {
                int source = y * image.RowStrideSamples + x * image.ChannelCount;
                int destination = (y * image.Width + x) * outputChannels;
                double red = image.FloatingSamples[source + channels.Red];
                double green = image.FloatingSamples[source + channels.Green];
                double blue = image.FloatingSamples[source + channels.Blue];
                if (!RawModelValidation.IsFinite(red) || !RawModelValidation.IsFinite(green) || !RawModelValidation.IsFinite(blue))
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Camera color conversion requires finite RGB samples.", -1);
                }
                double convertedX = transform[0] * red + transform[1] * green + transform[2] * blue;
                double convertedY = transform[3] * red + transform[4] * green + transform[5] * blue;
                double convertedZ = transform[6] * red + transform[7] * green + transform[8] * blue;
                Store(output, destination, convertedX);
                Store(output, destination + 1, convertedY);
                Store(output, destination + 2, convertedZ);
                if (outputChannels == 4)
                {
                    double alphaValue = image.FloatingSamples[source + alpha];
                    Store(output, destination + 3, alphaValue);
                }
                x++;
            }
            y++;
        }
        RawChannelDescription[] descriptions;
        if (outputChannels == 4)
        {
            descriptions = new RawChannelDescription[]
            {
                new RawChannelDescription("X", RawChannelKind.Custom),
                new RawChannelDescription("Y", RawChannelKind.Custom),
                new RawChannelDescription("Z", RawChannelKind.Custom),
                new RawChannelDescription("A", RawChannelKind.Alpha)
            };
        }
        else
        {
            descriptions = new RawChannelDescription[]
            {
                new RawChannelDescription("X", RawChannelKind.Custom),
                new RawChannelDescription("Y", RawChannelKind.Custom),
                new RawChannelDescription("Z", RawChannelKind.Custom)
            };
        }
        return new RawRgbImage(image.Width, image.Height, outputChannels, image.Width * outputChannels, null, null, output, descriptions, options.ColorSpace, RawTransferFunction.Linear, image.Orientation, image.OrientationApplied, true, image.Diagnostics);
    }

    internal static RawRgbChannelIndices Validate(RawRgbImage image, RawColorProfile[] profiles, double illuminantTemperatureKelvin, RawRenderOptions options)
    {
        if (image == null || options == null || !options.Validate().IsValid() || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid camera-linear image and render options are required for color conversion.", -1);
        }
        if (image.FloatingSamples == null || image.FloatingSamples.Length == 0 || image.ColorSpace != RawColorSpace.RawCamera || image.TransferFunction != RawTransferFunction.Linear || image.ChannelCount != 3 && image.ChannelCount != 4)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Camera color conversion requires floating camera-linear RGB or RGBA samples.", -1);
        }
        if ((options ?? throw new System.InvalidOperationException("A required value is unavailable.")).ColorSpace != RawColorSpace.XyzD50 && options.ColorSpace != RawColorSpace.XyzD65)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "This color stage outputs only D50 or D65 XYZ.", -1);
        }
        if (!RawModelValidation.IsFinite(illuminantTemperatureKelvin) || illuminantTemperatureKelvin < 1000.0 || illuminantTemperatureKelvin > 50000.0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Profile interpolation requires an illuminant temperature between 1000 and 50000 kelvin.", -1);
        }
        RawRgbChannelIndices channels = RawWhiteBalanceProcessor.FindChannels(image);
        if (channels.Red < 0 || channels.Green < 0 || channels.Blue < 0 || image.ChannelCount == 4 && FindAlpha(image) < 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Camera color conversion requires unique RGB channels and an alpha channel for four-channel input.", -1);
        }
        ValidateProfiles(profiles);
        options.CheckCancellation();
        return channels;
    }

    internal static void ValidateProfiles(RawColorProfile[] profiles)
    {
        if (profiles == null)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "A color-profile collection is required.", -1);
        }
        int matrixCount = 0;
        int index = 0;
        while (index < profiles.Length)
        {
            if (profiles[index] == null || !profiles[index].Validate().IsValid())
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Every color profile must be valid.", -1);
            }
            if (profiles[index].HasMatrixProfile())
            {
                matrixCount++;
            }
            index++;
        }
        if (matrixCount < 1 || matrixCount > 3)
        {
            throw new RawProfileException(RawDiagnosticCode.UnsupportedProfile, "Camera conversion requires between one and three matrix color profiles.", -1);
        }
    }

    internal static RawInterpolatedColorProfile InterpolateProfiles(RawColorProfile[] profiles, double temperatureKelvin)
    {
        ValidateProfiles(profiles);
        RawColorProfile[] selected = new RawColorProfile[3];
        double[] temperatures = new double[3];
        int count = 0;
        int index = 0;
        while (index < profiles.Length)
        {
            if (profiles[index].HasMatrixProfile())
            {
                selected[count] = profiles[index];
                temperatures[count] = RawChromaticAdaptation.Temperature(profiles[index]);
                count++;
            }
            index++;
        }
        int outer = 1;
        while (outer < count)
        {
            RawColorProfile profile = selected[outer];
            double temperature = temperatures[outer];
            int inner = outer - 1;
            while (inner >= 0 && temperatures[inner] > temperature)
            {
                selected[inner + 1] = selected[inner];
                temperatures[inner + 1] = temperatures[inner];
                inner--;
            }
            selected[inner + 1] = profile;
            temperatures[inner + 1] = temperature;
            outer++;
        }
        index = 1;
        while (index < count)
        {
            if (System.Math.Abs(temperatures[index] - temperatures[index - 1]) < 0.000001)
            {
                throw new RawProfileException(RawDiagnosticCode.AmbiguousProfile, "Matrix color profiles require distinct illuminant temperatures.", -1);
            }
            index++;
        }
        if (count == 1 || temperatureKelvin <= temperatures[0])
        {
            return Interpolated(selected[0], temperatures[0], 0.0);
        }
        if (temperatureKelvin >= temperatures[count - 1])
        {
            return Interpolated(selected[count - 1], temperatures[count - 1], 0.0);
        }
        int upper = 1;
        while (upper < count && temperatureKelvin > temperatures[upper])
        {
            upper++;
        }
        int lower = upper - 1;
        double requestedReciprocal = 1.0 / temperatureKelvin;
        double lowerReciprocal = 1.0 / temperatures[lower];
        double upperReciprocal = 1.0 / temperatures[upper];
        double fraction = (requestedReciprocal - lowerReciprocal) / (upperReciprocal - lowerReciprocal);
        double[] firstMatrix = EffectiveMatrix(selected[lower]);
        double[] secondMatrix = EffectiveMatrix(selected[upper]);
        double[] firstWhite = ProfileWhitePoint(selected[lower]);
        double[] secondWhite = ProfileWhitePoint(selected[upper]);
        return new RawInterpolatedColorProfile(RawMatrixMath.Interpolate(firstMatrix, secondMatrix, fraction), RawMatrixMath.InterpolateVector(firstWhite, secondWhite, fraction), temperatureKelvin, fraction);
    }

    internal static RawInterpolatedColorProfile Interpolated(RawColorProfile profile, double temperatureKelvin, double fraction)
    {
        double[] matrix = EffectiveMatrix(profile);
        double[] white = ProfileWhitePoint(profile);
        return new RawInterpolatedColorProfile(matrix, white, temperatureKelvin, fraction);
    }

    internal static double[] EffectiveMatrix(RawColorProfile profile)
    {
        double[] matrix;
        if (profile.ForwardMatrix != null && profile.ForwardMatrix.Length == 9)
        {
            matrix = Copy(profile.ForwardMatrix);
        }
        else if (profile.ColorMatrix != null && profile.ColorMatrix.Length == 9)
        {
            matrix = Copy(profile.ColorMatrix);
        }
        else if (profile.ReductionMatrix != null && profile.ReductionMatrix.Length == 9)
        {
            matrix = RawMatrixMath.Invert(profile.ReductionMatrix);
        }
        else
        {
            throw new RawProfileException(RawDiagnosticCode.UnsupportedProfile, "A camera-to-XYZ, forward, or invertible reduction matrix is required.", -1);
        }
        if (profile.CameraCalibration != null && profile.CameraCalibration.Length == 9)
        {
            matrix = RawMatrixMath.Multiply(matrix, profile.CameraCalibration);
        }
        RawMatrixMath.Invert(matrix);
        return matrix;
    }

    internal static double[] ProfileWhitePoint(RawColorProfile profile)
    {
        if (profile.WhitePoint != null && profile.WhitePoint.Length == 3)
        {
            return RawChromaticAdaptation.NormalizeWhite(profile.WhitePoint);
        }
        return RawChromaticAdaptation.WhitePoint(profile.Illuminant, profile.IlluminantTemperatureKelvin);
    }

    internal static double[] Copy(double[] values)
    {
        double[] result = new double[values.Length];
        System.Array.Copy(values, result, values.Length);
        return result;
    }

    internal static int FindAlpha(RawRgbImage image)
    {
        int found = -1;
        int index = 0;
        while (index < image.Channels.Length)
        {
            if (image.Channels[index].Kind == RawChannelKind.Alpha)
            {
                if (found >= 0)
                {
                    return -1;
                }
                found = index;
            }
            index++;
        }
        return found;
    }

    internal static void Store(float[] output, int index, double value)
    {
        if (!RawModelValidation.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "A converted XYZ sample exceeds the floating-point output range.", -1);
        }
        output[index] = (float)value;
    }
}
