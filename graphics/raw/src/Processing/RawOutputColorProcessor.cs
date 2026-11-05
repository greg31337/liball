namespace raw;

internal static class RawOutputColorProcessor
{
    internal static RawRgbImage Convert(RawRgbImage image, RawRenderOptions options)
    {
        Validate(image, options);
        bool rawTarget = options.ColorSpace == RawColorSpace.RawCamera;
        bool xyzTarget = options.ColorSpace == RawColorSpace.XyzD50 || options.ColorSpace == RawColorSpace.XyzD65;
        int alpha = image.ChannelCount == 4 ? 3 : -1;
        RawRgbChannelIndices? rgbChannels = null;
        double[] transform = RawMatrixMath.Identity();
        if (rawTarget)
        {
            rgbChannels = RawWhiteBalanceProcessor.FindChannels(image);
            alpha = image.ChannelCount == 4 ? RawCameraColorProcessor.FindAlpha(image) : -1;
        }
        else
        {
            RawColorIlluminant sourceIlluminant = image.ColorSpace == RawColorSpace.XyzD50 ? RawColorIlluminant.D50 : RawColorIlluminant.D65;
            RawColorIlluminant targetIlluminant = TargetIlluminant(options.ColorSpace);
            double[] sourceWhite = RawChromaticAdaptation.WhitePoint(sourceIlluminant, 0.0);
            double[] targetWhite = RawChromaticAdaptation.WhitePoint(targetIlluminant, 0.0);
            double[] adaptation = RawChromaticAdaptation.Create(sourceWhite, targetWhite, options.ChromaticAdaptation);
            if (xyzTarget)
            {
                transform = adaptation;
            }
            else
            {
                transform = RawMatrixMath.Multiply(OutputMatrix(options.ColorSpace), adaptation);
            }
        }
        int channelCount = image.ChannelCount;
        long pixelCount = CheckedMath.PixelCount(image.Width, image.Height, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, channelCount);
        CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        float[] output = new float[CheckedMath.ToInt32(sampleCountLong)];
        int y = 0;
        while (y < image.Height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < image.Width)
            {
                int source = y * image.RowStrideSamples + x * channelCount;
                int destination = (y * image.Width + x) * channelCount;
                double first;
                double second;
                double third;
                if (rawTarget)
                {
                    first = image.FloatingSamples[source + (rgbChannels ?? throw new System.InvalidOperationException("A required value is unavailable.")).Red];
                    second = image.FloatingSamples[source + rgbChannels.Green];
                    third = image.FloatingSamples[source + rgbChannels.Blue];
                }
                else
                {
                    first = image.FloatingSamples[source];
                    second = image.FloatingSamples[source + 1];
                    third = image.FloatingSamples[source + 2];
                }
                if (!RawModelValidation.IsFinite(first) || !RawModelValidation.IsFinite(second) || !RawModelValidation.IsFinite(third))
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Output color conversion requires finite floating-point color samples.", -1);
                }
                double convertedFirst = transform[0] * first + transform[1] * second + transform[2] * third;
                double convertedSecond = transform[3] * first + transform[4] * second + transform[5] * third;
                double convertedThird = transform[6] * first + transform[7] * second + transform[8] * third;
                Store(output, destination, Encode(convertedFirst, options.TransferFunction, options.Gamma));
                Store(output, destination + 1, Encode(convertedSecond, options.TransferFunction, options.Gamma));
                Store(output, destination + 2, Encode(convertedThird, options.TransferFunction, options.Gamma));
                if (channelCount == 4)
                {
                    Store(output, destination + 3, image.FloatingSamples[source + alpha]);
                }
                x++;
            }
            y++;
        }
        RawChannelDescription[] channels = xyzTarget ? XyzChannels(channelCount) : RgbChannels(channelCount);
        return new RawRgbImage(image.Width, image.Height, channelCount, image.Width * channelCount, null, null, output, channels, options.ColorSpace, options.TransferFunction, image.Orientation, image.OrientationApplied, true, image.Diagnostics);
    }

    internal static void Validate(RawRgbImage image, RawRenderOptions options)
    {
        if (image == null || options == null || !options.Validate().IsValid() || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid floating color image and render options are required for output conversion.", -1);
        }
        if (image.FloatingSamples == null || image.FloatingSamples.Length == 0 || image.TransferFunction != RawTransferFunction.Linear || image.ChannelCount != 3 && image.ChannelCount != 4)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Output color conversion requires floating linear RGB, RGBA, XYZ, or XYZA samples.", -1);
        }
        bool rawTarget = (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).ColorSpace == RawColorSpace.RawCamera;
        bool xyzInput = image.ColorSpace == RawColorSpace.XyzD50 || image.ColorSpace == RawColorSpace.XyzD65;
        if (rawTarget)
        {
            RawRgbChannelIndices channels = RawWhiteBalanceProcessor.FindChannels(image);
            if (image.ColorSpace != RawColorSpace.RawCamera || channels.Red < 0 || channels.Green < 0 || channels.Blue < 0 || image.ChannelCount == 4 && RawCameraColorProcessor.FindAlpha(image) < 0)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Raw-camera output requires unique RGB channels and optional alpha.", -1);
            }
        }
        else if (!xyzInput || !XyzOrder(image))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "XYZ or built-in RGB output requires linear XYZ input in X, Y, Z order.", -1);
        }
        ValidateTransfer(options.ColorSpace, options.TransferFunction);
        options.CheckCancellation();
    }

    internal static bool XyzOrder(RawRgbImage image)
    {
        if (image.Channels == null || image.Channels.Length != image.ChannelCount || image.Channels[0].Name != "X" || image.Channels[1].Name != "Y" || image.Channels[2].Name != "Z")
        {
            return false;
        }
        return image.ChannelCount == 3 || image.Channels[3].Kind == RawChannelKind.Alpha;
    }

    internal static void ValidateTransfer(RawColorSpace space, RawTransferFunction transfer)
    {
        bool valid = transfer == RawTransferFunction.Linear;
        if (space == RawColorSpace.Srgb || space == RawColorSpace.DisplayP3)
        {
            valid = valid || transfer == RawTransferFunction.Srgb || transfer == RawTransferFunction.Gamma;
        }
        else if (space == RawColorSpace.AdobeRgb)
        {
            valid = valid || transfer == RawTransferFunction.Gamma;
        }
        else if (space == RawColorSpace.ProPhotoRgb)
        {
            valid = valid || transfer == RawTransferFunction.ProPhotoRgb || transfer == RawTransferFunction.Gamma;
        }
        else if (space == RawColorSpace.Rec2020)
        {
            valid = valid || transfer == RawTransferFunction.Rec2020 || transfer == RawTransferFunction.Gamma;
        }
        else if (space != RawColorSpace.RawCamera && space != RawColorSpace.XyzD50 && space != RawColorSpace.XyzD65 && space != RawColorSpace.AcesCg)
        {
            valid = false;
        }
        if (!valid)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The selected transfer function is not defined for the requested output color space.", -1);
        }
    }

    internal static RawColorIlluminant TargetIlluminant(RawColorSpace space)
    {
        if (space == RawColorSpace.XyzD50 || space == RawColorSpace.ProPhotoRgb)
        {
            return RawColorIlluminant.D50;
        }
        if (space == RawColorSpace.AcesCg)
        {
            return RawColorIlluminant.D60;
        }
        if (space == RawColorSpace.XyzD65 || space == RawColorSpace.Srgb || space == RawColorSpace.DisplayP3 || space == RawColorSpace.AdobeRgb || space == RawColorSpace.Rec2020)
        {
            return RawColorIlluminant.D65;
        }
        throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The requested output color space has no built-in white point.", -1);
    }

    internal static double[] OutputMatrix(RawColorSpace space)
    {
        switch (space)
        {
            case RawColorSpace.Srgb:
                return new double[]
                {
                    3.2404542, -1.5371385, -0.4985314,
                    -0.9692660, 1.8760108, 0.0415560,
                    0.0556434, -0.2040259, 1.0572252
                };
            case RawColorSpace.DisplayP3:
                return new double[]
                {
                    2.4934969, -0.9313836, -0.4027108,
                    -0.8294890, 1.7626640, 0.0236247,
                    0.0358458, -0.0761724, 0.9568845
                };
            case RawColorSpace.AdobeRgb:
                return new double[]
                {
                    2.0413690, -0.5649464, -0.3446944,
                    -0.9692660, 1.8760108, 0.0415560,
                    0.0134474, -0.1183897, 1.0154096
                };
            case RawColorSpace.ProPhotoRgb:
                return new double[]
                {
                    1.3459433, -0.2556075, -0.0511118,
                    -0.5445989, 1.5081673, 0.0205351,
                    0.0, 0.0, 1.2118128
                };
            case RawColorSpace.Rec2020:
                return new double[]
                {
                    1.7166512, -0.3556708, -0.2533663,
                    -0.6666844, 1.6164812, 0.0157685,
                    0.0176399, -0.0427706, 0.9421031
                };
            case RawColorSpace.AcesCg:
                return new double[]
                {
                    1.6410234, -0.3248033, -0.2364247,
                    -0.6636629, 1.6153316, 0.0167563,
                    0.0117219, -0.0082844, 0.9883949
                };
            default:
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The requested output color space has no built-in XYZ transform.", -1);
        }
    }

    internal static double Encode(double value, RawTransferFunction transfer, double gamma)
    {
        if (!RawModelValidation.IsFinite(value))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A linear color value is not finite.", -1);
        }
        if (transfer == RawTransferFunction.Linear)
        {
            return value;
        }
        double sign = value < 0.0 ? -1.0 : 1.0;
        double magnitude = System.Math.Abs(value);
        double encoded;
        if (transfer == RawTransferFunction.Srgb)
        {
            encoded = magnitude <= 0.0031308 ? magnitude * 12.92 : 1.055 * System.Math.Pow(magnitude, 1.0 / 2.4) - 0.055;
        }
        else if (transfer == RawTransferFunction.Gamma)
        {
            encoded = System.Math.Pow(magnitude, 1.0 / gamma);
        }
        else if (transfer == RawTransferFunction.Rec2020)
        {
            double alpha = 1.09929682680944;
            double beta = 0.018053968510807;
            encoded = magnitude < beta ? magnitude * 4.5 : alpha * System.Math.Pow(magnitude, 0.45) - (alpha - 1.0);
        }
        else if (transfer == RawTransferFunction.ProPhotoRgb)
        {
            encoded = magnitude < 0.001953125 ? magnitude * 16.0 : System.Math.Pow(magnitude, 1.0 / 1.8);
        }
        else
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The transfer function has no managed encoder.", -1);
        }
        return sign * encoded;
    }

    internal static RawChannelDescription[] RgbChannels(int channelCount)
    {
        if (channelCount == 4)
        {
            return new RawChannelDescription[]
            {
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("B", RawChannelKind.Blue),
                new RawChannelDescription("A", RawChannelKind.Alpha)
            };
        }
        return new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
    }

    internal static RawChannelDescription[] XyzChannels(int channelCount)
    {
        if (channelCount == 4)
        {
            return new RawChannelDescription[]
            {
                new RawChannelDescription("X", RawChannelKind.Custom),
                new RawChannelDescription("Y", RawChannelKind.Custom),
                new RawChannelDescription("Z", RawChannelKind.Custom),
                new RawChannelDescription("A", RawChannelKind.Alpha)
            };
        }
        return new RawChannelDescription[]
        {
            new RawChannelDescription("X", RawChannelKind.Custom),
            new RawChannelDescription("Y", RawChannelKind.Custom),
            new RawChannelDescription("Z", RawChannelKind.Custom)
        };
    }

    internal static void Store(float[] output, int index, double value)
    {
        if (!RawModelValidation.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "An output color sample exceeds the floating-point range.", -1);
        }
        output[index] = (float)value;
    }
}
