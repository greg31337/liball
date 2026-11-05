namespace raw;

internal static class RawChromaticAdaptation
{
    internal static double[] Create(double[] sourceWhite, double[] targetWhite, RawChromaticAdaptationMode mode)
    {
        double[] source = NormalizeWhite(sourceWhite);
        double[] target = NormalizeWhite(targetWhite);
        if (mode == RawChromaticAdaptationMode.None)
        {
            return RawMatrixMath.Identity();
        }
        double[] cone;
        if (mode == RawChromaticAdaptationMode.Bradford)
        {
            cone = new double[]
            {
                0.8951, 0.2664, -0.1614,
                -0.7502, 1.7135, 0.0367,
                0.0389, -0.0685, 1.0296
            };
        }
        else if (mode == RawChromaticAdaptationMode.Cat02)
        {
            cone = new double[]
            {
                0.7328, 0.4296, -0.1624,
                -0.7036, 1.6975, 0.0061,
                0.0030, 0.0136, 0.9834
            };
        }
        else
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The chromatic adaptation mode is not defined.", -1);
        }
        double[] sourceCone = RawMatrixMath.MultiplyVector(cone, source);
        double[] targetCone = RawMatrixMath.MultiplyVector(cone, target);
        int index = 0;
        while (index < 3)
        {
            if (System.Math.Abs(sourceCone[index]) < 0.000000000001)
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The source white point cannot be represented by the selected adaptation transform.", -1);
            }
            index++;
        }
        double[] scale = RawMatrixMath.Diagonal(targetCone[0] / sourceCone[0], targetCone[1] / sourceCone[1], targetCone[2] / sourceCone[2]);
        return RawMatrixMath.Multiply(RawMatrixMath.Invert(cone), RawMatrixMath.Multiply(scale, cone));
    }

    internal static double[] NormalizeWhite(double[] white)
    {
        RawMatrixMath.RequireVector(white, "The XYZ white point");
        if (white[0] <= 0.0 || white[1] <= 0.0 || white[2] <= 0.0)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "XYZ white-point components must be positive.", -1);
        }
        return new double[] { white[0] / white[1], 1.0, white[2] / white[1] };
    }

    internal static double[] WhitePoint(RawColorIlluminant illuminant, double customTemperatureKelvin)
    {
        switch (illuminant)
        {
            case RawColorIlluminant.StandardLightA:
                return new double[] { 1.09850, 1.0, 0.35585 };
            case RawColorIlluminant.D50:
                return new double[] { 0.96422, 1.0, 0.82521 };
            case RawColorIlluminant.D55:
                return new double[] { 0.95682, 1.0, 0.92149 };
            case RawColorIlluminant.D65:
                return new double[] { 0.95047, 1.0, 1.08883 };
            case RawColorIlluminant.D75:
                return new double[] { 0.94972, 1.0, 1.22638 };
            case RawColorIlluminant.D60:
                return new double[] { 0.95265, 1.0, 1.00883 };
            case RawColorIlluminant.Custom:
                return TemperatureWhitePoint(customTemperatureKelvin);
            default:
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "A defined profile illuminant is required.", -1);
        }
    }

    internal static double[] TemperatureWhitePoint(double temperatureKelvin)
    {
        double[] multipliers = RawWhiteBalanceProcessor.TemperatureMultipliers(temperatureKelvin, 0.0);
        double red = 1.0 / multipliers[0];
        double green = 1.0 / multipliers[1];
        double blue = 1.0 / multipliers[2];
        double x = 0.4124564 * red + 0.3575761 * green + 0.1804375 * blue;
        double y = 0.2126729 * red + 0.7151522 * green + 0.0721750 * blue;
        double z = 0.0193339 * red + 0.1191920 * green + 0.9503041 * blue;
        return NormalizeWhite(new double[] { x, y, z });
    }

    internal static double Temperature(RawColorProfile profile)
    {
        if (profile.Illuminant == RawColorIlluminant.Custom)
        {
            return profile.IlluminantTemperatureKelvin;
        }
        switch (profile.Illuminant)
        {
            case RawColorIlluminant.StandardLightA:
                return 2856.0;
            case RawColorIlluminant.D50:
                return 5003.0;
            case RawColorIlluminant.D55:
                return 5503.0;
            case RawColorIlluminant.D65:
                return 6504.0;
            case RawColorIlluminant.D75:
                return 7504.0;
            case RawColorIlluminant.D60:
                return 6000.0;
            default:
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The profile illuminant temperature is not defined.", -1);
        }
    }
}
