using System;

namespace avif;

internal static class IccTransform
{
    static readonly double[] d50 = new double[] { 0.9642, 1, 0.8249 };

    internal static bool CanTransformToPcs(IccParsedProfile? profile)
    {
        if (profile == null)
        {
            return false;
        }
        return HasPipeline(profile.AToB)
            || profile.MatrixCurves != null;
    }

    internal static bool CanTransformFromPcs(IccParsedProfile? profile)
    {
        if (profile == null)
        {
            return false;
        }
        return HasPipeline(profile.BToA)
            || profile.MatrixCurves != null;
    }

    internal static AvifResultCode ToPcs(
        IccParsedProfile? profile,
        double[] source,
        double[] destination,
        AvifRenderingIntent intent)
    {
        if (!ValidateTransformArguments(
            profile, source, destination, intent,
            IccParser.GetChannelCount(profile == null
                ? string.Empty : profile.ColorSpace)))
        {
            return AvifResultCode.InvalidArgument;
        }
        double[]? xyz = TransformToRelativePcs(profile, source, intent);
        if (xyz == null)
        {
            return AvifResultCode.UnsupportedFeature;
        }
        if (intent == AvifRenderingIntent.AbsoluteColorimetric)
        {
            xyz = ScaleWhite(xyz, d50, (profile ?? throw new InvalidOperationException("Invalid internal state.")).MediaWhite);
        }
        CopyThree(xyz, destination);
        return AvifResultCode.Success;
    }

    internal static AvifResultCode FromPcs(
        IccParsedProfile? profile,
        double[] source,
        double[] destination,
        AvifRenderingIntent intent)
    {
        int outputChannels = IccParser.GetChannelCount(
            profile == null ? string.Empty : profile.ColorSpace);
        if (profile == null || source == null || destination == null
            || source.Length < 3 || outputChannels <= 0
            || destination.Length < outputChannels
            || !IsValidIntent(intent))
        {
            return AvifResultCode.InvalidArgument;
        }
        double[] relative = new double[]
        {
            source[0], source[1], source[2]
        };
        if (intent == AvifRenderingIntent.AbsoluteColorimetric)
        {
            relative = ScaleWhite(relative, profile.MediaWhite, d50);
        }
        double[]? device = TransformFromRelativePcs(
            profile, relative, intent);
        if (device == null)
        {
            return AvifResultCode.UnsupportedFeature;
        }
        int index = 0;
        while (index < outputChannels)
        {
            destination[index] = Clamp(device[index]);
            index++;
        }
        return AvifResultCode.Success;
    }

    static double[]? TransformToRelativePcs(
        IccParsedProfile? profile,
        double[] source,
        AvifRenderingIntent intent)
    {
        IccPipeline? pipeline = SelectPipeline((profile ?? throw new InvalidOperationException("Invalid internal state.")).AToB, intent);
        if (pipeline != null)
        {
            double[]? encodedPcs = pipeline.Evaluate(source);
            return DecodePcs(profile, encodedPcs);
        }
        if (profile.MatrixCurves == null)
        {
            return null;
        }
        if (profile.ColorSpace == "GRAY")
        {
            double luminance = profile.MatrixCurves[0].Evaluate(source[0]);
            return new double[]
            {
                (profile.MediaWhite ?? throw new InvalidOperationException("Invalid internal state."))[0] * luminance,
                profile.MediaWhite[1] * luminance,
                profile.MediaWhite[2] * luminance
            };
        }
        if (profile.ColorSpace != "RGB " || profile.Matrix == null)
        {
            return null;
        }
        double red = profile.MatrixCurves[0].Evaluate(source[0]);
        double green = profile.MatrixCurves[1].Evaluate(source[1]);
        double blue = profile.MatrixCurves[2].Evaluate(source[2]);
        return new double[]
        {
            profile.Matrix[0] * red + profile.Matrix[1] * green
                + profile.Matrix[2] * blue,
            profile.Matrix[3] * red + profile.Matrix[4] * green
                + profile.Matrix[5] * blue,
            profile.Matrix[6] * red + profile.Matrix[7] * green
                + profile.Matrix[8] * blue
        };
    }

    static double[]? TransformFromRelativePcs(
        IccParsedProfile profile,
        double[] xyz,
        AvifRenderingIntent intent)
    {
        IccPipeline? pipeline = SelectPipeline(profile.BToA, intent);
        if (pipeline != null)
        {
            return pipeline.Evaluate(EncodePcs(profile, xyz));
        }
        if (profile.MatrixCurves == null)
        {
            return null;
        }
        if (profile.ColorSpace == "GRAY")
        {
            double luminance = (profile.MediaWhite ?? throw new InvalidOperationException("Invalid internal state."))[1] == 0
                ? 0 : xyz[1] / profile.MediaWhite[1];
            return new double[]
            {
                profile.MatrixCurves[0].Invert(luminance)
            };
        }
        if (profile.ColorSpace != "RGB " || profile.Matrix == null)
        {
            return null;
        }
        ColorMatrix matrix = new ColorMatrix(profile.Matrix);
        ColorMatrix? inverse = matrix.Invert();
        if (inverse == null)
        {
            return null;
        }
        ColorVector linear = inverse.Transform(new ColorVector(
            xyz[0], xyz[1], xyz[2]));
        return new double[]
        {
            profile.MatrixCurves[0].Invert(linear.First),
            profile.MatrixCurves[1].Invert(linear.Second),
            profile.MatrixCurves[2].Invert(linear.Third)
        };
    }

    static double[]? DecodePcs(
        IccParsedProfile profile,
        double[]? encoded)
    {
        if (encoded == null || encoded.Length < 3)
        {
            return null;
        }
        if (profile.Pcs == "XYZ ")
        {
            double scale = 65535.0 / 32768;
            return new double[]
            {
                encoded[0] * scale,
                encoded[1] * scale,
                encoded[2] * scale
            };
        }
        double lightness = encoded[0] * 100;
        double a = encoded[1] * 255 - 128;
        double b = encoded[2] * 255 - 128;
        return LabToXyz(lightness, a, b);
    }

    static double[] EncodePcs(
        IccParsedProfile profile,
        double[] xyz)
    {
        if (profile.Pcs == "XYZ ")
        {
            double scale = 32768.0 / 65535;
            return new double[]
            {
                Clamp(xyz[0] * scale),
                Clamp(xyz[1] * scale),
                Clamp(xyz[2] * scale)
            };
        }
        double[] lab = XyzToLab(xyz);
        return new double[]
        {
            Clamp(lab[0] / 100),
            Clamp((lab[1] + 128) / 255),
            Clamp((lab[2] + 128) / 255)
        };
    }

    static double[] LabToXyz(double lightness, double a, double b)
    {
        double fy = (lightness + 16) / 116;
        double fx = fy + a / 500;
        double fz = fy - b / 200;
        return new double[]
        {
            d50[0] * LabInverseFunction(fx),
            d50[1] * LabInverseFunction(fy),
            d50[2] * LabInverseFunction(fz)
        };
    }

    static double[] XyzToLab(double[] xyz)
    {
        double fx = LabFunction(xyz[0] / d50[0]);
        double fy = LabFunction(xyz[1] / d50[1]);
        double fz = LabFunction(xyz[2] / d50[2]);
        return new double[]
        {
            116 * fy - 16,
            500 * (fx - fy),
            200 * (fy - fz)
        };
    }

    static double LabFunction(double value)
    {
        double epsilon = 216.0 / 24389;
        double kappa = 24389.0 / 27;
        return value > epsilon
            ? System.Math.Pow(value, 1.0 / 3)
            : (kappa * value + 16) / 116;
    }

    static double LabInverseFunction(double value)
    {
        double cube = value * value * value;
        double epsilon = 216.0 / 24389;
        double kappa = 24389.0 / 27;
        return cube > epsilon
            ? cube : (116 * value - 16) / kappa;
    }

    static IccPipeline? SelectPipeline(
        IccPipeline?[] pipelines,
        AvifRenderingIntent intent)
    {
        int requested = intent == AvifRenderingIntent.Saturation
            ? 2
            : intent == AvifRenderingIntent.Perceptual ? 0 : 1;
        if (pipelines[requested] != null)
        {
            return pipelines[requested];
        }
        if (pipelines[0] != null)
        {
            return pipelines[0];
        }
        if (pipelines[1] != null)
        {
            return pipelines[1];
        }
        return pipelines[2];
    }

    static double[] ScaleWhite(
        double[] xyz,
        double[]? sourceWhite,
        double[]? destinationWhite)
    {
        return new double[]
        {
            (sourceWhite ?? throw new InvalidOperationException("Invalid internal state."))[0] == 0 ? 0
                : xyz[0] * (destinationWhite ?? throw new InvalidOperationException("Invalid internal state."))[0] / sourceWhite[0],
            sourceWhite[1] == 0 ? 0
                : xyz[1] * (destinationWhite ?? throw new InvalidOperationException("Invalid internal state."))[1] / sourceWhite[1],
            sourceWhite[2] == 0 ? 0
                : xyz[2] * (destinationWhite ?? throw new InvalidOperationException("Invalid internal state."))[2] / sourceWhite[2]
        };
    }

    static bool ValidateTransformArguments(
        IccParsedProfile? profile,
        double[] source,
        double[] destination,
        AvifRenderingIntent intent,
        int sourceChannels)
    {
        return profile != null && source != null && destination != null
            && sourceChannels > 0 && source.Length >= sourceChannels
            && destination.Length >= 3 && IsValidIntent(intent);
    }

    static bool IsValidIntent(AvifRenderingIntent intent)
    {
        return intent == AvifRenderingIntent.Perceptual
            || intent == AvifRenderingIntent.RelativeColorimetric
            || intent == AvifRenderingIntent.Saturation
            || intent == AvifRenderingIntent.AbsoluteColorimetric;
    }

    static bool HasPipeline(IccPipeline?[] pipelines)
    {
        return pipelines[0] != null
            || pipelines[1] != null
            || pipelines[2] != null;
    }

    static void CopyThree(double[] source, double[] destination)
    {
        destination[0] = source[0];
        destination[1] = source[1];
        destination[2] = source[2];
    }

    static double Clamp(double value)
    {
        return System.Math.Max(0, System.Math.Min(1, value));
    }
}
