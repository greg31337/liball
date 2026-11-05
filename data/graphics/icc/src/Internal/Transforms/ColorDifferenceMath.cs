namespace icc;

internal static class ColorDifferenceMath
{
    internal static double CalculateColorDifference(double[] firstLab, double[] secondLab, IccColorDifferenceFormula formula)
    {
        ColorMathCore.ValidateThreeComponents(firstLab, "first Lab colour");
        ColorMathCore.ValidateThreeComponents(secondLab, "second Lab colour");
        double result;
        switch (formula)
        {
            case IccColorDifferenceFormula.Cie76:
                result = CalculateCie76(firstLab, secondLab);
                break;
            case IccColorDifferenceFormula.Cie94GraphicArts:
                result = CalculateCie94(firstLab, secondLab, 1.0, 0.045, 0.015);
                break;
            case IccColorDifferenceFormula.Cie94Textiles:
                result = CalculateCie94(firstLab, secondLab, 2.0, 0.048, 0.014);
                break;
            case IccColorDifferenceFormula.Ciede2000:
                result = CalculateCiede2000(firstLab, secondLab);
                break;
            default:
                throw new IccTransformException("The colour-difference formula is unsupported.");
        }

        if (!System.Double.IsFinite(result))
        {
            throw new IccTransformException("Colour-difference calculation produced a value outside the finite double range.");
        }

        return result;
    }

    internal static double CalculateCie76(double[] firstLab, double[] secondLab)
    {
        double lightnessDifference = secondLab[0] - firstLab[0];
        double aDifference = secondLab[1] - firstLab[1];
        double bDifference = secondLab[2] - firstLab[2];
        return ComputeStableMagnitudeThree(lightnessDifference, aDifference, bDifference);
    }

    internal static double CalculateCie94(double[] firstLab, double[] secondLab, double lightnessWeight, double chromaScale, double hueScale)
    {
        double firstChroma = ColorMathCore.ComputeStableMagnitude(firstLab[1], firstLab[2]);
        double secondChroma = ColorMathCore.ComputeStableMagnitude(secondLab[1], secondLab[2]);
        double lightnessDifference = secondLab[0] - firstLab[0];
        double chromaDifference = secondChroma - firstChroma;
        double hueTerm = 0.0;
        if (firstChroma != 0.0 && secondChroma != 0.0)
        {
            double firstUnitA = firstLab[1] / firstChroma;
            double firstUnitB = firstLab[2] / firstChroma;
            double secondUnitA = secondLab[1] / secondChroma;
            double secondUnitB = secondLab[2] / secondChroma;
            double unitChord = ColorMathCore.ComputeStableMagnitude(secondUnitA - firstUnitA, secondUnitB - firstUnitB);
            double hueWeight = 1.0 + (hueScale * firstChroma);
            double scaledFirstChroma = firstChroma / hueWeight;
            double scaledSecondChroma = secondChroma / hueWeight;
            hueTerm = System.Math.Sqrt(scaledFirstChroma) * System.Math.Sqrt(scaledSecondChroma) * unitChord;
        }

        double lightnessTerm = lightnessDifference / lightnessWeight;
        double chromaTerm = chromaDifference / (1.0 + (chromaScale * firstChroma));
        return ComputeStableMagnitudeThree(lightnessTerm, chromaTerm, hueTerm);
    }

    internal static double CalculateCiede2000(double[] firstLab, double[] secondLab)
    {
        double firstChroma = ColorMathCore.ComputeStableMagnitude(firstLab[1], firstLab[2]);
        double secondChroma = ColorMathCore.ComputeStableMagnitude(secondLab[1], secondLab[2]);
        double arithmeticChroma = (firstChroma + secondChroma) / 2.0;
        double compensation = 0.5 * (1.0 - System.Math.Sqrt(ComputeSeventhPowerRatio(arithmeticChroma)));
        double firstPrimeA = (1.0 + compensation) * firstLab[1];
        double secondPrimeA = (1.0 + compensation) * secondLab[1];
        double firstPrimeChroma = ColorMathCore.ComputeStableMagnitude(firstPrimeA, firstLab[2]);
        double secondPrimeChroma = ColorMathCore.ComputeStableMagnitude(secondPrimeA, secondLab[2]);
        double firstPrimeHue = CalculatePrimeHue(firstPrimeA, firstLab[2]);
        double secondPrimeHue = CalculatePrimeHue(secondPrimeA, secondLab[2]);
        double chromaProduct = firstPrimeChroma * secondPrimeChroma;

        double lightnessDifference = secondLab[0] - firstLab[0];
        double chromaDifference = secondPrimeChroma - firstPrimeChroma;
        double hueAngleDifference = secondPrimeHue - firstPrimeHue;
        if (chromaProduct == 0.0)
        {
            hueAngleDifference = 0.0;
        }
        else if (hueAngleDifference > System.Math.PI)
        {
            hueAngleDifference = hueAngleDifference - (2.0 * System.Math.PI);
        }
        else if (hueAngleDifference < -System.Math.PI)
        {
            hueAngleDifference = hueAngleDifference + (2.0 * System.Math.PI);
        }

        double hueDifference = 2.0 * System.Math.Sqrt(chromaProduct) * System.Math.Sin(hueAngleDifference / 2.0);
        double meanLightness = (firstLab[0] + secondLab[0]) / 2.0;
        double meanPrimeChroma = (firstPrimeChroma + secondPrimeChroma) / 2.0;
        double meanPrimeHue = CalculateMeanPrimeHue(firstPrimeHue, secondPrimeHue, chromaProduct);
        double lightnessOffset = meanLightness - 50.0;
        double lightnessScale = 1.0 + ((0.015 * lightnessOffset * lightnessOffset) / System.Math.Sqrt(20.0 + (lightnessOffset * lightnessOffset)));
        double chromaWeight = 1.0 + (0.045 * meanPrimeChroma);
        double hueShape = 1.0
            - (0.17 * System.Math.Cos(meanPrimeHue - (System.Math.PI / 6.0)))
            + (0.24 * System.Math.Cos(2.0 * meanPrimeHue))
            + (0.32 * System.Math.Cos((3.0 * meanPrimeHue) + (System.Math.PI / 30.0)))
            - (0.20 * System.Math.Cos((4.0 * meanPrimeHue) - (63.0 * System.Math.PI / 180.0)));
        double hueWeight = 1.0 + (0.015 * meanPrimeChroma * hueShape);
        double meanHueDegrees = meanPrimeHue * (180.0 / System.Math.PI);
        double hueRotationOffset = (meanHueDegrees - 275.0) / 25.0;
        double hueRotation = (System.Math.PI / 6.0) * System.Math.Exp(-(hueRotationOffset * hueRotationOffset));
        double rotationChroma = 2.0 * System.Math.Sqrt(ComputeSeventhPowerRatio(meanPrimeChroma));
        double rotationTerm = -System.Math.Sin(2.0 * hueRotation) * rotationChroma;
        double normalizedLightness = lightnessDifference / lightnessScale;
        double normalizedChroma = chromaDifference / chromaWeight;
        double normalizedHue = hueDifference / hueWeight;
        double squaredDifference = (normalizedLightness * normalizedLightness)
            + (normalizedChroma * normalizedChroma)
            + (normalizedHue * normalizedHue)
            + (rotationTerm * normalizedChroma * normalizedHue);
        if (squaredDifference < 0.0 && squaredDifference > -0.000000000001)
        {
            squaredDifference = 0.0;
        }

        return System.Math.Sqrt(squaredDifference);
    }

    internal static double CalculatePrimeHue(double primeA, double b)
    {
        if (primeA == 0.0 && b == 0.0)
        {
            return 0.0;
        }

        double hue = System.Math.Atan2(b, primeA);
        if (hue < 0.0)
        {
            hue = hue + (2.0 * System.Math.PI);
        }

        return hue;
    }

    internal static double CalculateMeanPrimeHue(double firstHue, double secondHue, double chromaProduct)
    {
        if (chromaProduct == 0.0)
        {
            return firstHue + secondHue;
        }

        double mean = (firstHue + secondHue) / 2.0;
        if (System.Math.Abs(firstHue - secondHue) > System.Math.PI)
        {
            mean = mean - System.Math.PI;
        }

        if (mean < 0.0)
        {
            mean = mean + (2.0 * System.Math.PI);
        }

        return mean;
    }

    internal static double ComputeSeventhPowerRatio(double value)
    {
        if (value <= 0.0)
        {
            return 0.0;
        }

        if (value >= 25.0)
        {
            double ratio = 25.0 / value;
            return 1.0 / (1.0 + System.Math.Pow(ratio, 7.0));
        }

        double scaled = value / 25.0;
        double seventh = System.Math.Pow(scaled, 7.0);
        return seventh / (seventh + 1.0);
    }

    internal static double ComputeStableMagnitudeThree(double first, double second, double third)
    {
        double firstMagnitude = System.Math.Abs(first);
        double secondMagnitude = System.Math.Abs(second);
        double thirdMagnitude = System.Math.Abs(third);
        double maximum = System.Math.Max(firstMagnitude, System.Math.Max(secondMagnitude, thirdMagnitude));
        if (maximum == 0.0)
        {
            return 0.0;
        }

        double firstRatio = firstMagnitude / maximum;
        double secondRatio = secondMagnitude / maximum;
        double thirdRatio = thirdMagnitude / maximum;
        return maximum * System.Math.Sqrt((firstRatio * firstRatio) + (secondRatio * secondRatio) + (thirdRatio * thirdRatio));
    }
}
