namespace icc;

internal class IccCamState
{
    internal double WhiteY;
    internal double DegreeOfAdaptation;
    internal double LuminanceAdaptation;
    internal double BackgroundFactor;
    internal double BrightnessInduction;
    internal double SurroundExponent;
    internal double WhiteAchromaticResponse;
    internal double LinearThreshold;
    internal double LinearSlope;
    internal double[] WhiteResponses;
    internal double[] AdaptationScales;

    internal IccCamState()
    {
        WhiteY = 0.0;
        DegreeOfAdaptation = 0.0;
        LuminanceAdaptation = 0.0;
        BackgroundFactor = 0.0;
        BrightnessInduction = 0.0;
        SurroundExponent = 0.0;
        WhiteAchromaticResponse = 0.0;
        LinearThreshold = 0.0;
        LinearSlope = 0.0;
        WhiteResponses = new double[3];
        AdaptationScales = new double[3];
    }
}

internal static class IccCamMath
{
    internal const double Alpha = 0.2307;
    internal const double ResponseExponent = 0.3169;

    internal static void ExecuteElement(IccProcessingElement element, System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        if (element == null || element.CamViewingConditions == null || element.InputChannels != 3 || element.OutputChannels != 3 || inputs.Length != 3 || outputs.Length < 3)
        {
            throw new IccTransformException("An ICC appearance element requires three input channels, three output channels, and viewing conditions.");
        }

        if (element.Kind == IccProcessingElementKind.XyzToJab && element.Signature == IccProcessingElementSignatures.XyzToJab)
        {
            ConvertXyzToJab(element.CamViewingConditions, inputs, outputs);
            return;
        }
        if (element.Kind == IccProcessingElementKind.JabToXyz && element.Signature == IccProcessingElementSignatures.JabToXyz)
        {
            ConvertJabToXyz(element.CamViewingConditions, inputs, outputs);
            return;
        }

        throw new IccTransformException("The ICC appearance element kind and signature do not agree.");
    }

    internal static void ConvertXyzToJab(IccCamViewingConditions conditions, System.ReadOnlySpan<double> xyz, System.Span<double> jab)
    {
        if (xyz.Length != 3 || jab.Length < 3)
        {
            throw new IccTransformException("ICC CAM XYZ-to-Jab conversion requires three input and output values.");
        }
        ValidateFiniteInput(xyz, "XYZ");
        IccCamState state = CreateState(conditions);
        double[] responses = MultiplyForward(xyz);
        int channel = 0;
        while (channel < 3)
        {
            if (responses[channel] < 0.0)
            {
                responses[channel] = 0.0;
            }
            responses[channel] = CompressResponse(state.LuminanceAdaptation * state.AdaptationScales[channel] * responses[channel] / 100.0, state) + 0.1;
            channel = channel + 1;
        }

        double opponentA = responses[0] - (12.0 * responses[1] / 11.0) + (responses[2] / 11.0);
        double opponentB = (responses[0] + responses[1] - (2.0 * responses[2])) / 9.0;
        double chromaResponse = StableMagnitude(opponentA, opponentB);
        double hueRadians = System.Math.Atan2(opponentB, opponentA);
        double achromaticResponse = ((2.0 * responses[0]) + responses[1] + (responses[2] / 20.0) - 0.305) * state.BrightnessInduction;
        double ratio = achromaticResponse / state.WhiteAchromaticResponse;
        if (ratio < 0.0)
        {
            ratio = 0.0;
        }
        double lightness = 100.0 * System.Math.Pow(ratio, conditions.ImpactOfSurround * state.SurroundExponent);
        double eccentricity = (System.Math.Cos(hueRadians + 2.0) + 3.8) / 4.0;
        double denominator = responses[0] + responses[1] + (21.0 * responses[2] / 20.0);
        double temporary = denominator == 0.0 ? 0.0 : (50000.0 / 13.0) * conditions.ChromaticInductionFactor * state.BrightnessInduction * eccentricity * chromaResponse / denominator;
        double chroma = System.Math.Pow(temporary, 0.9) * System.Math.Sqrt(lightness / 100.0) * state.BackgroundFactor;

        jab[0] = lightness;
        jab[1] = chroma * System.Math.Cos(hueRadians);
        jab[2] = chroma * System.Math.Sin(hueRadians);
        ValidateFiniteOutput(jab, "XYZ-to-Jab");
    }

    internal static void ConvertJabToXyz(IccCamViewingConditions conditions, System.ReadOnlySpan<double> jab, System.Span<double> xyz)
    {
        if (jab.Length != 3 || xyz.Length < 3)
        {
            throw new IccTransformException("ICC CAM Jab-to-XYZ conversion requires three input and output values.");
        }
        ValidateFiniteInput(jab, "Jab");
        IccCamState state = CreateState(conditions);
        double[] compressed = new double[3];
        if (jab[0] < 0.00001)
        {
            compressed[0] = 0.1;
            compressed[1] = 0.1;
            compressed[2] = 0.1;
        }
        else
        {
            CalculateInverseCompressed(jab, conditions, state, compressed);
        }

        double[] adapted = new double[3];
        int channel = 0;
        while (channel < 3)
        {
            if (compressed[channel] < 0.0)
            {
                compressed[channel] = 0.0;
            }
            adapted[channel] = 100.0 * ExpandResponse(compressed[channel] - 0.1, state) / state.LuminanceAdaptation;
            adapted[channel] = adapted[channel] / state.AdaptationScales[channel];
            channel = channel + 1;
        }

        double[] result = MultiplyInverse(adapted);
        new System.ReadOnlySpan<double>(result).CopyTo(xyz);
        ValidateFiniteOutput(xyz, "Jab-to-XYZ");
    }

    internal static IccCamState CreateState(IccCamViewingConditions conditions)
    {
        ValidateViewingConditions(conditions);
        IccCamState state = new IccCamState();
        state.WhiteY = conditions.WhitePoint.Y;
        state.DegreeOfAdaptation = conditions.AdaptationFactor * (1.0 - (System.Math.Exp(-(conditions.Luminance + 42.0) / 92.0) / 3.6));
        double k = 1.0 / ((5.0 * conditions.Luminance) + 1.0);
        k = k * k * k * k;
        double luminanceFive = 5.0 * conditions.Luminance;
        state.LuminanceAdaptation = (0.2 * k * luminanceFive) + (0.1 * (1.0 - k) * (1.0 - k) * System.Math.Cbrt(luminanceFive));
        double relativeBackground = conditions.BackgroundLuminance / conditions.WhitePoint.Y;
        state.BackgroundFactor = System.Math.Pow(1.64 - System.Math.Pow(0.29, relativeBackground), 0.73);
        state.BrightnessInduction = 0.725 / System.Math.Pow(relativeBackground, 0.2);
        state.SurroundExponent = 1.48 + System.Math.Sqrt(relativeBackground);
        double[] white = new double[] { conditions.WhitePoint.X, conditions.WhitePoint.Y, conditions.WhitePoint.Z };
        state.WhiteResponses = MultiplyForward(white);
        int channel = 0;
        while (channel < 3)
        {
            if (!System.Double.IsFinite(state.WhiteResponses[channel]) || state.WhiteResponses[channel] <= 0.0)
            {
                throw new IccTransformException("ICC CAM white point must have strictly positive Hunt-Pointer-Estevez responses.");
            }
            state.AdaptationScales[channel] = (state.DegreeOfAdaptation * state.WhiteY / state.WhiteResponses[channel]) + 1.0 - state.DegreeOfAdaptation;
            if (!System.Double.IsFinite(state.AdaptationScales[channel]) || state.AdaptationScales[channel] <= 0.0)
            {
                throw new IccTransformException("ICC CAM viewing conditions produce an invalid chromatic-adaptation scale.");
            }
            channel = channel + 1;
        }

        state.LinearThreshold = state.LuminanceAdaptation * 4.0 / 255.0;
        double responseAtLuminance = HResponse(state.LuminanceAdaptation);
        double responseAtThreshold = HResponse(state.LinearThreshold);
        double standardAtLuminance = FResponse(state.LuminanceAdaptation);
        state.LinearSlope = ((1.0 + Alpha) * responseAtThreshold / responseAtLuminance - Alpha) * standardAtLuminance / state.LinearThreshold;

        double[] compressedWhite = new double[3];
        channel = 0;
        while (channel < 3)
        {
            compressedWhite[channel] = CompressResponse(state.LuminanceAdaptation * state.AdaptationScales[channel] * state.WhiteResponses[channel] / 100.0, state) + 0.1;
            channel = channel + 1;
        }
        state.WhiteAchromaticResponse = ((2.0 * compressedWhite[0]) + compressedWhite[1] + (compressedWhite[2] / 20.0) - 0.305) * state.BrightnessInduction;
        if (!System.Double.IsFinite(state.DegreeOfAdaptation) || !System.Double.IsFinite(state.LuminanceAdaptation) || state.LuminanceAdaptation <= 0.0
            || !System.Double.IsFinite(state.BackgroundFactor) || state.BackgroundFactor <= 0.0
            || !System.Double.IsFinite(state.BrightnessInduction) || state.BrightnessInduction <= 0.0
            || !System.Double.IsFinite(state.SurroundExponent) || state.SurroundExponent <= 0.0
            || !System.Double.IsFinite(state.LinearThreshold) || state.LinearThreshold <= 0.0
            || !System.Double.IsFinite(state.LinearSlope) || state.LinearSlope <= 0.0
            || !System.Double.IsFinite(state.WhiteAchromaticResponse) || state.WhiteAchromaticResponse <= 0.0)
        {
            throw new IccTransformException("ICC CAM viewing conditions produce a degenerate transform.");
        }

        return state;
    }

    internal static void ValidateViewingConditions(IccCamViewingConditions conditions)
    {
        if (conditions == null || conditions.WhitePoint == null)
        {
            throw new IccTransformException("ICC CAM viewing conditions and white point are required.");
        }
        if (!System.Double.IsFinite(conditions.WhitePoint.X) || !System.Double.IsFinite(conditions.WhitePoint.Y) || !System.Double.IsFinite(conditions.WhitePoint.Z)
            || conditions.WhitePoint.X <= 0.0 || conditions.WhitePoint.Y <= 0.0 || conditions.WhitePoint.Z <= 0.0)
        {
            throw new IccTransformException("ICC CAM white-point XYZ values must be finite and strictly positive.");
        }
        if (!System.Double.IsFinite(conditions.Luminance) || conditions.Luminance <= 0.0)
        {
            throw new IccTransformException("ICC CAM adapting-field luminance must be finite and strictly positive.");
        }
        if (!System.Double.IsFinite(conditions.BackgroundLuminance) || conditions.BackgroundLuminance <= 0.0)
        {
            throw new IccTransformException("ICC CAM background luminance must be finite and strictly positive.");
        }
        if (!System.Double.IsFinite(conditions.ImpactOfSurround) || conditions.ImpactOfSurround <= 0.0 || conditions.ImpactOfSurround > 1.0)
        {
            throw new IccTransformException("ICC CAM impact of surround must be greater than zero and no greater than one.");
        }
        if (!System.Double.IsFinite(conditions.ChromaticInductionFactor) || conditions.ChromaticInductionFactor <= 0.0)
        {
            throw new IccTransformException("ICC CAM chromatic-induction factor must be finite and strictly positive.");
        }
        if (!System.Double.IsFinite(conditions.AdaptationFactor) || conditions.AdaptationFactor < 0.0 || conditions.AdaptationFactor > 1.0)
        {
            throw new IccTransformException("ICC CAM adaptation factor must range from zero to one.");
        }
    }

    internal static double[] MultiplyForward(System.ReadOnlySpan<double> values)
    {
        return new double[]
        {
            (0.38970 * values[0]) + (0.68898 * values[1]) - (0.07868 * values[2]),
            (-0.22981 * values[0]) + (1.18340 * values[1]) + (0.04641 * values[2]),
            values[2]
        };
    }

    internal static double[] MultiplyInverse(System.ReadOnlySpan<double> values)
    {
        return new double[]
        {
            (1.9102333232684989 * values[0]) - (1.1121451369490709 * values[1]) + (0.20191181368057187 * values[2]),
            (0.37095717426088703 * values[0]) + (0.6290501318892463 * values[1]) - (0.0000073061501333259085 * values[2]),
            values[2]
        };
    }

    internal static double FResponse(double value)
    {
        double power = System.Math.Pow(value, 0.42);
        return 400.0 * power / (27.13 + power);
    }

    internal static double HResponse(double value)
    {
        double power = System.Math.Pow(value, ResponseExponent);
        return 400.0 * power / (27.13 + power);
    }

    internal static double InverseHResponse(double value)
    {
        if (!System.Double.IsFinite(value) || value < 0.0 || value >= 400.0)
        {
            return 0.0;
        }
        return System.Math.Pow(27.13 * value / (400.0 - value), 1.0 / ResponseExponent);
    }

    internal static double CompressResponse(double value, IccCamState state)
    {
        double magnitude = System.Math.Abs(value);
        if (magnitude <= state.LinearThreshold)
        {
            return state.LinearSlope * value;
        }
        double compressed = ((1.0 + Alpha) * HResponse(magnitude) / HResponse(state.LuminanceAdaptation) - Alpha) * FResponse(state.LuminanceAdaptation);
        return value < 0.0 ? -compressed : compressed;
    }

    internal static double ExpandResponse(double value, IccCamState state)
    {
        double magnitude = System.Math.Abs(value);
        if (magnitude <= state.LinearSlope * state.LinearThreshold)
        {
            return value / state.LinearSlope;
        }
        double standardAtLuminance = FResponse(state.LuminanceAdaptation);
        double response = ((magnitude / standardAtLuminance) + Alpha) / (1.0 + Alpha) * HResponse(state.LuminanceAdaptation);
        double expanded = InverseHResponse(response);
        return value < 0.0 ? -expanded : expanded;
    }

    internal static void CalculateInverseCompressed(System.ReadOnlySpan<double> jab, IccCamViewingConditions conditions, IccCamState state, double[] compressed)
    {
        double chroma = StableMagnitude(jab[1], jab[2]);
        double chromaRatioDenominator = System.Math.Sqrt(jab[0] / 100.0) * state.BackgroundFactor;
        double chromaRatio = chromaRatioDenominator == 0.0 ? 0.0 : chroma / chromaRatioDenominator;
        double temporary = System.Math.Pow(chromaRatio, 1.0 / 0.9);
        double achromaticResponse = state.WhiteAchromaticResponse * System.Math.Pow(jab[0] / 100.0, 1.0 / (conditions.ImpactOfSurround * state.SurroundExponent));
        double p2 = ((achromaticResponse / state.BrightnessInduction) + 0.305) * 460.0 / 1403.0;
        if (temporary < 0.00001)
        {
            compressed[0] = p2;
            compressed[1] = p2;
            compressed[2] = p2;
            return;
        }

        double hueRadians = System.Math.Atan2(jab[2], jab[1]);
        double hueDegrees = hueRadians * 180.0 / System.Math.PI;
        if (hueDegrees < 0.0)
        {
            hueDegrees = hueDegrees + 360.0;
        }
        double eccentricity = (System.Math.Cos(hueRadians + 2.0) + 3.8) / 4.0;
        double p1 = (50000.0 / 13.0) * conditions.ChromaticInductionFactor * state.BrightnessInduction * eccentricity / temporary;
        double p3 = 21.0 / 20.0;
        double numerator = p2 * (2.0 + p3);
        double opponentA;
        double opponentB;
        if (hueDegrees <= 45.0 || hueDegrees >= 315.0 || (hueDegrees > 135.0 && hueDegrees < 225.0))
        {
            double tangent = System.Math.Tan(hueRadians);
            double direction = hueDegrees > 90.0 && hueDegrees < 270.0 ? -1.0 : 1.0;
            double p5 = p1 * direction * System.Math.Sqrt(1.0 + (tangent * tangent));
            double denominator = p5 + ((2.0 + p3) * 220.0 / 1403.0) - (((27.0 / 1403.0) - (p3 * 6300.0 / 1403.0)) * tangent);
            opponentA = denominator == 0.0 ? 0.0 : numerator / denominator;
            opponentB = opponentA * tangent;
        }
        else
        {
            double cotangent = System.Math.Abs(hueDegrees - 90.0) < 0.00001 || System.Math.Abs(hueDegrees - 270.0) < 0.00001 ? 0.0 : 1.0 / System.Math.Tan(hueRadians);
            double direction = hueDegrees > 180.0 ? -1.0 : 1.0;
            double p4 = p1 * direction * System.Math.Sqrt(1.0 + (cotangent * cotangent));
            double denominator = p4 + (((2.0 + p3) * 220.0 / 1403.0) * cotangent) - (27.0 / 1403.0) + (p3 * 6300.0 / 1403.0);
            opponentB = denominator == 0.0 ? 0.0 : numerator / denominator;
            opponentA = opponentB * cotangent;
        }

        compressed[0] = p2 + ((451.0 * opponentA) + (288.0 * opponentB)) / 1403.0;
        compressed[1] = p2 + ((-891.0 * opponentA) - (261.0 * opponentB)) / 1403.0;
        compressed[2] = p2 + ((-220.0 * opponentA) - (6300.0 * opponentB)) / 1403.0;
    }

    internal static double StableMagnitude(double first, double second)
    {
        double firstMagnitude = System.Math.Abs(first);
        double secondMagnitude = System.Math.Abs(second);
        double maximum = System.Math.Max(firstMagnitude, secondMagnitude);
        if (maximum == 0.0)
        {
            return 0.0;
        }
        double normalizedFirst = firstMagnitude / maximum;
        double normalizedSecond = secondMagnitude / maximum;
        return maximum * System.Math.Sqrt((normalizedFirst * normalizedFirst) + (normalizedSecond * normalizedSecond));
    }

    internal static void ValidateFiniteInput(System.ReadOnlySpan<double> values, string description)
    {
        int index = 0;
        while (index < 3)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                throw new IccTransformException($"ICC CAM {description} input {index} must be finite.");
            }
            index = index + 1;
        }
    }

    internal static void ValidateFiniteOutput(System.ReadOnlySpan<double> values, string description)
    {
        int index = 0;
        while (index < 3)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                throw new IccTransformException($"ICC CAM {description} output {index} is outside the finite range.");
            }
            index = index + 1;
        }
    }
}
