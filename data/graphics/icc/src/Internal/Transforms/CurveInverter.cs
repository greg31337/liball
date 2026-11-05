namespace icc;

internal static class CurveInverter
{
    internal static IccCurveInversionResult InvertCurve(IccCurve curve, double output)
    {
        if (curve == null)
        {
            throw new IccProfileValidationException("Curve data cannot be null.");
        }

        ValidateFiniteOutput(output);
        switch (curve.Kind)
        {
            case IccCurveKind.Identity:
                return CreateSuccessfulResult(CurveMath.ClampNormalized(output), "Inverted an identity curve analytically.");
            case IccCurveKind.Gamma:
                return InvertGammaCurve(curve.Gamma, output);
            case IccCurveKind.Sampled:
                return InvertSampledValues(curve.Samples, output, 0.0, 1.0, 0);
            case IccCurveKind.Parametric:
                if (curve.Parametric == null)
                {
                    throw new IccProfileValidationException("Parametric curve data cannot be null.");
                }

                return InvertParametricCurve(curve.Parametric, output);
            case IccCurveKind.Segmented:
                return InvertSegmentedCurve(curve.Segments, output);
            case IccCurveKind.SingleSampled:
                return InvertSingleSampledCurve(curve, output);
            default:
                throw new IccProfileValidationException("The curve kind is unsupported.");
        }
    }

    internal static double InvertSampledCurve(double[] samples, double output)
    {
        IccCurveInversionResult result = InvertSampledValues(samples, output, 0.0, 1.0, 0);
        if (!result.HasInput)
        {
            throw new IccProfileValidationException(result.Result.Message);
        }

        return result.Input;
    }

    internal static IccCurveInversionResult InvertGammaCurve(double gamma, double output)
    {
        ValidateFiniteOutput(output);
        if (!System.Double.IsFinite(gamma) || gamma <= 0.0)
        {
            throw new IccProfileValidationException("A gamma curve exponent must be finite and positive.");
        }

        double input = System.Math.Pow(CurveMath.ClampNormalized(output), 1.0 / gamma);
        return CreateSuccessfulResult(input, "Inverted a gamma curve analytically.");
    }

    internal static IccCurveInversionResult InvertParametricCurve(IccParametricCurve curve, double output)
    {
        if (curve == null || curve.Parameters == null)
        {
            throw new IccProfileValidationException("Parametric curve data and its parameters cannot be null.");
        }

        ValidateFiniteOutput(output);
        int parameterCount = CurveTagCodec.GetParameterCount(curve.FunctionType);
        if (parameterCount < 0 || curve.Parameters.Length != parameterCount)
        {
            throw new IccProfileValidationException($"Parametric curve function {curve.FunctionType} does not have its exact ICC.1 parameter count.");
        }

        CurveMath.ValidateSamples(curve.Parameters, parameterCount, "parametric curve parameters");
        double[] p = curve.Parameters;
        if (p[0] <= 0.0)
        {
            return CreateFailedResult("The parametric curve is not monotonically invertible because its exponent is not positive.");
        }

        if (curve.FunctionType > 0 && p[1] <= 0.0)
        {
            return CreateFailedResult("The parametric curve is not monotonically invertible because its primary scale is not positive.");
        }

        if ((curve.FunctionType == 3 || curve.FunctionType == 4) && p[3] < 0.0)
        {
            return CreateFailedResult("The parametric curve is not monotonically invertible because its linear slope is negative.");
        }

        if (HasDecreasingParametricBoundary(curve))
        {
            return CreateFailedResult("The parametric curve is non-monotonic because its piecewise boundary decreases.");
        }

        double minimum = CurveMath.EvaluateParametricCurve(curve, 0.0);
        double maximum = CurveMath.EvaluateParametricCurve(curve, 1.0);
        if (maximum <= minimum)
        {
            return CreateFailedResult("The parametric curve is constant or decreasing over the normalized domain.");
        }

        if (output < minimum)
        {
            return CreateSuccessfulResult(0.0, "The requested output is below the curve range and maps to its lower endpoint.");
        }

        if (output > maximum)
        {
            return CreateSuccessfulResult(1.0, "The requested output is above the curve range and maps to its upper endpoint.");
        }

        double plateau = ResolveParametricPlateau(curve, output, minimum, maximum);
        if (plateau >= 0.0)
        {
            return CreateSuccessfulResult(plateau, "The requested output lies on a parametric plateau and maps to its midpoint.");
        }

        double candidate = SolveParametricCurve(curve, output);
        if (!System.Double.IsFinite(candidate))
        {
            return CreateFailedResult("The analytical parametric inverse did not produce a finite input.");
        }

        candidate = CurveMath.ClampNormalized(candidate);
        double evaluated = CurveMath.EvaluateParametricCurve(curve, candidate);
        if (System.Math.Abs(evaluated - output) > 0.00000001)
        {
            return CreateFailedResult("The parametric curve parameters do not define a consistent monotonic inverse.");
        }

        return CreateSuccessfulResult(candidate, "Inverted an ICC parametric curve analytically.");
    }

    internal static bool HasDecreasingParametricBoundary(IccParametricCurve curve)
    {
        if (curve.FunctionType != 3 && curve.FunctionType != 4)
        {
            return false;
        }

        double[] p = curve.Parameters;
        double boundary = p[4];
        if (boundary <= 0.0 || boundary >= 1.0)
        {
            return false;
        }

        double lower = p[3] * boundary;
        double upper = System.Math.Pow((p[1] * boundary) + p[2], p[0]);
        if (curve.FunctionType == 4)
        {
            lower = lower + p[6];
            upper = upper + p[5];
        }

        if (!System.Double.IsFinite(lower) || !System.Double.IsFinite(upper))
        {
            throw new IccProfileValidationException("Parametric curve boundary evaluation produced a non-finite value.");
        }

        return CurveMath.ClampNormalized(upper) < CurveMath.ClampNormalized(lower);
    }

    internal static double ResolveParametricPlateau(IccParametricCurve curve, double output, double minimum, double maximum)
    {
        bool lowerPlateau = output == minimum;
        if (!lowerPlateau && output != maximum)
        {
            return -1.0;
        }

        double lower = 0.0;
        double upper = 1.0;
        int iteration = 0;
        while (iteration < 80)
        {
            double middle = (lower + upper) / 2.0;
            double value = CurveMath.EvaluateParametricCurve(curve, middle);
            if ((lowerPlateau && value == output) || (!lowerPlateau && value != output))
            {
                lower = middle;
            }
            else
            {
                upper = middle;
            }

            iteration = iteration + 1;
        }

        if (lowerPlateau)
        {
            return lower == 0.0 ? -1.0 : lower / 2.0;
        }

        return upper == 1.0 ? -1.0 : (upper + 1.0) / 2.0;
    }

    internal static double SolveParametricCurve(IccParametricCurve curve, double output)
    {
        double[] p = curve.Parameters;
        switch (curve.FunctionType)
        {
            case 0:
                return System.Math.Pow(output, 1.0 / p[0]);
            case 1:
                return (System.Math.Pow(output, 1.0 / p[0]) - p[2]) / p[1];
            case 2:
                return (System.Math.Pow(output - p[3], 1.0 / p[0]) - p[2]) / p[1];
            case 3:
                if (output < p[3] * p[4] && p[3] != 0.0)
                {
                    return output / p[3];
                }

                return (System.Math.Pow(output, 1.0 / p[0]) - p[2]) / p[1];
            case 4:
                if (output < (p[3] * p[4]) + p[6] && p[3] != 0.0)
                {
                    return (output - p[6]) / p[3];
                }

                return (System.Math.Pow(output - p[5], 1.0 / p[0]) - p[2]) / p[1];
            default:
                return System.Double.NaN;
        }
    }

    internal static IccCurveInversionResult InvertSingleSampledCurve(IccCurve curve, double output)
    {
        CurveMath.ValidateSamples(curve.Samples, 2, "single-sampled curve");
        if (!System.Double.IsFinite(curve.InputMinimum) || !System.Double.IsFinite(curve.InputMaximum) || curve.InputMinimum >= curve.InputMaximum)
        {
            throw new IccProfileValidationException("A single-sampled curve requires a finite, increasing input domain.");
        }

        if (curve.Extension < 0 || curve.Extension > 1)
        {
            throw new IccProfileValidationException("A single-sampled curve extension selector must be zero or one.");
        }

        return InvertSampledValues(curve.Samples, output, curve.InputMinimum, curve.InputMaximum, curve.Extension);
    }

    internal static IccCurveInversionResult InvertSegmentedCurve(IccCurveSegment[] segments, double output)
    {
        ValidateFiniteOutput(output);
        CurveMath.ValidateSegments(segments);
        double[] samples = new double[4097];
        int index = 0;
        while (index < samples.Length)
        {
            double input = index / 4096.0;
            samples[index] = CurveMath.EvaluateSegmentedCurve(segments, input);
            index = index + 1;
        }

        IccCurveInversionResult approximation = InvertSampledValues(samples, output, 0.0, 1.0, 0);
        if (!approximation.HasInput)
        {
            return approximation;
        }

        int direction = DetermineMonotonicDirection(samples);
        double minimumOutput = direction > 0 ? samples[0] : samples[samples.Length - 1];
        double maximumOutput = direction > 0 ? samples[samples.Length - 1] : samples[0];
        if (output <= minimumOutput || output >= maximumOutput || HasDuplicateTarget(samples, output))
        {
            return approximation;
        }

        double lower = 0.0;
        double upper = 1.0;
        int iteration = 0;
        while (iteration < 80)
        {
            double middle = (lower + upper) / 2.0;
            double value = CurveMath.EvaluateSegmentedCurve(segments, middle);
            if ((direction > 0 && value < output) || (direction < 0 && value > output))
            {
                lower = middle;
            }
            else
            {
                upper = middle;
            }

            iteration = iteration + 1;
        }

        double inputValue = (lower + upper) / 2.0;
        double check = CurveMath.EvaluateSegmentedCurve(segments, inputValue);
        if (System.Math.Abs(check - output) > 0.00000001)
        {
            return CreateFailedResult("The segmented curve is discontinuous at the requested output and has no inverse value.");
        }

        return CreateSuccessfulResult(inputValue, "Inverted a monotonic segmented curve numerically.");
    }

    internal static IccCurveInversionResult InvertSampledValues(double[] samples, double output, double minimumInput, double maximumInput, int extension)
    {
        ValidateFiniteOutput(output);
        CurveMath.ValidateSamples(samples, 2, "sampled curve");
        int direction = DetermineMonotonicDirection(samples);
        if (direction == 0)
        {
            return CreateFailedResult("The sampled curve is constant and does not have a unique inverse.");
        }

        if (direction == 2)
        {
            return CreateFailedResult("The sampled curve is non-monotonic and cannot be inverted.");
        }

        int firstEqual = FindFirstEqualSample(samples, output);
        if (firstEqual >= 0)
        {
            int lastEqual = FindLastEqualSample(samples, output, firstEqual);
            double firstInput = SampleInputAt(firstEqual, samples.Length, minimumInput, maximumInput);
            double lastInput = SampleInputAt(lastEqual, samples.Length, minimumInput, maximumInput);
            return CreateSuccessfulResult((firstInput + lastInput) / 2.0, "The sampled output plateau maps to the midpoint of its input interval.");
        }

        double minimumOutput = direction > 0 ? samples[0] : samples[samples.Length - 1];
        double maximumOutput = direction > 0 ? samples[samples.Length - 1] : samples[0];
        if (output < minimumOutput)
        {
            bool lowerInput = direction > 0;
            return ResolveOutsideSampleRange(samples, output, minimumInput, maximumInput, extension, lowerInput);
        }

        if (output > maximumOutput)
        {
            bool lowerInput = direction < 0;
            return ResolveOutsideSampleRange(samples, output, minimumInput, maximumInput, extension, lowerInput);
        }

        int index = 0;
        while (index < samples.Length - 1)
        {
            double first = samples[index];
            double second = samples[index + 1];
            if ((direction > 0 && output > first && output < second) || (direction < 0 && output < first && output > second))
            {
                double amount = (output - first) / (second - first);
                double firstInput = SampleInputAt(index, samples.Length, minimumInput, maximumInput);
                double secondInput = SampleInputAt(index + 1, samples.Length, minimumInput, maximumInput);
                return CreateSuccessfulResult(firstInput + ((secondInput - firstInput) * amount), "Inverted a monotonic sampled curve by linear interpolation.");
            }

            index = index + 1;
        }

        return CreateFailedResult("The requested output could not be located in the monotonic sampled curve.");
    }

    internal static IccCurveInversionResult ResolveOutsideSampleRange(double[] samples, double output, double minimumInput, double maximumInput, int extension, bool lowerInput)
    {
        if (extension == 0)
        {
            double endpoint = lowerInput ? minimumInput : maximumInput;
            return CreateSuccessfulResult(endpoint, "The requested output is outside the curve range and maps to the nearest input endpoint.");
        }

        int firstIndex = lowerInput ? 0 : samples.Length - 2;
        int secondIndex = firstIndex + 1;
        double firstInput = SampleInputAt(firstIndex, samples.Length, minimumInput, maximumInput);
        double secondInput = SampleInputAt(secondIndex, samples.Length, minimumInput, maximumInput);
        double difference = samples[secondIndex] - samples[firstIndex];
        if (difference == 0.0)
        {
            return CreateFailedResult("The requested output cannot be reached because the linear extension endpoint is flat.");
        }

        double amount = (output - samples[firstIndex]) / difference;
        double input = firstInput + ((secondInput - firstInput) * amount);
        if (!System.Double.IsFinite(input))
        {
            return CreateFailedResult("The sampled curve extension did not produce a finite inverse input.");
        }

        return CreateSuccessfulResult(input, "Inverted a single-sampled curve using its linear endpoint extension.");
    }

    internal static int DetermineMonotonicDirection(double[] samples)
    {
        int direction = 0;
        int index = 1;
        while (index < samples.Length)
        {
            if (samples[index] > samples[index - 1])
            {
                if (direction < 0)
                {
                    return 2;
                }

                direction = 1;
            }
            else if (samples[index] < samples[index - 1])
            {
                if (direction > 0)
                {
                    return 2;
                }

                direction = -1;
            }

            index = index + 1;
        }

        return direction;
    }

    internal static int FindFirstEqualSample(double[] samples, double output)
    {
        int index = 0;
        while (index < samples.Length)
        {
            if (samples[index] == output)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindLastEqualSample(double[] samples, double output, int firstIndex)
    {
        int index = firstIndex;
        while (index < samples.Length - 1 && samples[index + 1] == output)
        {
            index = index + 1;
        }

        return index;
    }

    internal static bool HasDuplicateTarget(double[] samples, double output)
    {
        int first = FindFirstEqualSample(samples, output);
        return first >= 0 && first < samples.Length - 1 && samples[first + 1] == output;
    }

    internal static double SampleInputAt(int index, int sampleCount, double minimumInput, double maximumInput)
    {
        return minimumInput + (((maximumInput - minimumInput) * index) / (sampleCount - 1));
    }

    internal static void ValidateFiniteOutput(double output)
    {
        if (!System.Double.IsFinite(output))
        {
            throw new IccProfileValidationException("A curve inversion output must be finite.");
        }
    }

    internal static IccCurveInversionResult CreateSuccessfulResult(double input, string message)
    {
        IccCurveInversionResult result = new IccCurveInversionResult();
        result.HasInput = true;
        result.Input = input;
        result.Result.Status = IccOperationStatus.Success;
        result.Result.Message = message;
        return result;
    }

    internal static IccCurveInversionResult CreateFailedResult(string message)
    {
        IccCurveInversionResult result = new IccCurveInversionResult();
        result.Result.Status = IccOperationStatus.Conflict;
        result.Result.Message = message;
        return result;
    }
}
