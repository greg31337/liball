namespace icc;

internal static class ColorMathCore
{
    internal const double LabDelta = 6.0 / 29.0;

    internal static double[] ConvertXyzToLab(double[] xyz, double[] whitePoint)
    {
        ValidateThreeComponents(xyz, "XYZ colour");
        ValidateWhitePoint(whitePoint);
        double x = EvaluateLabFunction(xyz[0] / whitePoint[0]);
        double y = EvaluateLabFunction(xyz[1] / whitePoint[1]);
        double z = EvaluateLabFunction(xyz[2] / whitePoint[2]);
        double[] result = new double[]
        {
            (116.0 * y) - 16.0,
            500.0 * (x - y),
            200.0 * (y - z)
        };
        ValidateFiniteResult(result, "XYZ-to-Lab conversion");
        return result;
    }

    internal static double[] ConvertLabToXyz(double[] lab, double[] whitePoint)
    {
        ValidateThreeComponents(lab, "Lab colour");
        ValidateWhitePoint(whitePoint);
        double y = (lab[0] + 16.0) / 116.0;
        double x = y + (lab[1] / 500.0);
        double z = y - (lab[2] / 200.0);
        double[] result = new double[]
        {
            whitePoint[0] * EvaluateInverseLabFunction(x),
            whitePoint[1] * EvaluateInverseLabFunction(y),
            whitePoint[2] * EvaluateInverseLabFunction(z)
        };
        ValidateFiniteResult(result, "Lab-to-XYZ conversion");
        return result;
    }

    internal static double[] ConvertXyzToXyY(double[] xyz)
    {
        ValidateThreeComponents(xyz, "XYZ colour");
        double sum = xyz[0] + xyz[1] + xyz[2];
        if (!System.Double.IsFinite(sum))
        {
            throw new IccTransformException("XYZ-to-xyY conversion requires a finite tristimulus sum.");
        }

        if (sum == 0.0)
        {
            if (xyz[0] == 0.0 && xyz[1] == 0.0 && xyz[2] == 0.0)
            {
                return new double[] { 0.0, 0.0, 0.0 };
            }

            throw new IccTransformException("XYZ-to-xyY conversion is singular when non-zero tristimulus values sum to zero.");
        }

        double[] result = new double[] { xyz[0] / sum, xyz[1] / sum, xyz[1] };
        ValidateFiniteResult(result, "XYZ-to-xyY conversion");
        return result;
    }

    internal static double[] ConvertXyYToXyz(double[] xyY)
    {
        ValidateThreeComponents(xyY, "xyY colour");
        double x = xyY[0];
        double y = xyY[1];
        double luminance = xyY[2];
        if (y == 0.0)
        {
            if (luminance == 0.0)
            {
                return new double[] { 0.0, 0.0, 0.0 };
            }

            throw new IccTransformException("xyY-to-XYZ conversion is singular when y is zero and luminance is non-zero.");
        }

        double scale = luminance / y;
        double[] result = new double[] { x * scale, luminance, (1.0 - x - y) * scale };
        ValidateFiniteResult(result, "xyY-to-XYZ conversion");
        return result;
    }

    internal static double[] ConvertLabToLch(double[] lab)
    {
        ValidateThreeComponents(lab, "Lab colour");
        double chroma = ComputeStableMagnitude(lab[1], lab[2]);
        if (!System.Double.IsFinite(chroma))
        {
            throw new IccTransformException("Lab-to-LCh conversion produced a chroma outside the finite double range.");
        }

        double hue = 0.0;
        if (chroma != 0.0)
        {
            hue = System.Math.Atan2(lab[2], lab[1]) * (180.0 / System.Math.PI);
            if (hue < 0.0)
            {
                hue = hue + 360.0;
            }

            if (hue >= 360.0 || hue == 0.0)
            {
                hue = 0.0;
            }
        }

        return new double[] { lab[0], chroma, hue };
    }

    internal static double[] ConvertLchToLab(double[] lch)
    {
        ValidateThreeComponents(lch, "LCh colour");
        if (lch[1] < 0.0)
        {
            throw new IccTransformException("LCh chroma cannot be negative.");
        }

        if (lch[1] == 0.0)
        {
            return new double[] { lch[0], 0.0, 0.0 };
        }

        double hue = lch[2] % 360.0;
        if (hue < 0.0)
        {
            hue = hue + 360.0;
        }

        double radians = hue * (System.Math.PI / 180.0);
        double[] result = new double[] { lch[0], lch[1] * System.Math.Cos(radians), lch[1] * System.Math.Sin(radians) };
        ValidateFiniteResult(result, "LCh-to-Lab conversion");
        return result;
    }

    internal static double EvaluateLabFunction(double value)
    {
        if (value > IccStandardColorimetry.LabEpsilon)
        {
            return System.Math.Cbrt(value);
        }

        return ((IccStandardColorimetry.LabKappa * value) + 16.0) / 116.0;
    }

    internal static double EvaluateInverseLabFunction(double value)
    {
        if (value > LabDelta)
        {
            return value * value * value;
        }

        return ((116.0 * value) - 16.0) / IccStandardColorimetry.LabKappa;
    }

    internal static double ComputeStableMagnitude(double first, double second)
    {
        double firstMagnitude = System.Math.Abs(first);
        double secondMagnitude = System.Math.Abs(second);
        double maximum = firstMagnitude > secondMagnitude ? firstMagnitude : secondMagnitude;
        double minimum = firstMagnitude > secondMagnitude ? secondMagnitude : firstMagnitude;
        if (maximum == 0.0)
        {
            return 0.0;
        }

        double ratio = minimum / maximum;
        return maximum * System.Math.Sqrt(1.0 + (ratio * ratio));
    }

    internal static void ValidateWhitePoint(double[] whitePoint)
    {
        ValidateThreeComponents(whitePoint, "XYZ white point");
        if (whitePoint[0] <= 0.0 || whitePoint[1] <= 0.0 || whitePoint[2] <= 0.0)
        {
            throw new IccTransformException("XYZ white-point components must be positive finite values.");
        }
    }

    internal static void ValidateThreeComponents(double[] values, string description)
    {
        if (values == null || values.Length != 3)
        {
            throw new IccTransformException($"The {description} must contain exactly three components.");
        }

        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                throw new IccTransformException($"The {description} component {index} must be finite.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateFiniteResult(double[] values, string description)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                throw new IccTransformException($"The {description} produced component {index} outside the finite double range.");
            }

            index = index + 1;
        }
    }
}
