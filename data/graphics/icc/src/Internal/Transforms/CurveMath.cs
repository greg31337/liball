namespace icc;

internal static class CurveMath
{
    internal static double EvaluateCurve(IccCurve curve, double input)
    {
        if (curve == null)
        {
            throw new IccProfileValidationException("Curve data cannot be null.");
        }

        ValidateFiniteInput(input, "curve");
        switch (curve.Kind)
        {
            case IccCurveKind.Identity:
                return ClampNormalized(input);
            case IccCurveKind.Gamma:
                return EvaluateGammaCurve(curve.Gamma, input);
            case IccCurveKind.Sampled:
                return EvaluateSampledCurve(curve.Samples, input);
            case IccCurveKind.Parametric:
                if (curve.Parametric == null)
                {
                    throw new IccProfileValidationException("Parametric curve data cannot be null.");
                }

                return EvaluateParametricCurve(curve.Parametric, input);
            case IccCurveKind.Segmented:
                return EvaluateSegmentedCurve(curve.Segments, input);
            case IccCurveKind.SingleSampled:
                return EvaluateSingleSampledCurve(curve, input);
            default:
                throw new IccProfileValidationException("The curve kind is unsupported.");
        }
    }

    internal static double[] EvaluateAdaptiveGainCurve(IccAdaptiveGainCurve curve, double[] inputs)
    {
        if (curve == null)
        {
            throw new IccProfileValidationException("Adaptive gain curve data cannot be null.");
        }

        if (inputs == null || inputs.Length != 3)
        {
            throw new IccProfileValidationException("Adaptive gain curve evaluation requires exactly three input values.");
        }

        double[] result = new double[3];
        result[0] = AdaptiveGainCurveTagCodec.EvaluateCurve(curve.RedNodes, inputs[0]);
        result[1] = AdaptiveGainCurveTagCodec.EvaluateCurve(curve.GreenNodes, inputs[1]);
        result[2] = AdaptiveGainCurveTagCodec.EvaluateCurve(curve.BlueNodes, inputs[2]);
        int index = 0;
        while (index < result.Length)
        {
            if (!System.Double.IsFinite(result[index]))
            {
                throw new IccProfileValidationException($"Adaptive gain curve channel {index} produced a non-finite value.");
            }

            index = index + 1;
        }

        return result;
    }

    internal static double EvaluateGammaCurve(double gamma, double input)
    {
        ValidateFiniteInput(input, "gamma curve");
        if (!System.Double.IsFinite(gamma) || gamma <= 0.0)
        {
            throw new IccProfileValidationException("A gamma curve exponent must be finite and positive.");
        }

        return System.Math.Pow(ClampNormalized(input), gamma);
    }

    internal static double EvaluateSampledCurve(double[] samples, double input)
    {
        return ClutInterpolator.InterpolateOneDimensional(samples, input, 0.0, 1.0, false);
    }

    internal static double EvaluateSingleSampledCurve(IccCurve curve, double input)
    {
        ValidateFiniteInput(input, "single-sampled curve");
        ValidateSamples(curve.Samples, 2, "single-sampled curve");
        if (!System.Double.IsFinite(curve.InputMinimum) || !System.Double.IsFinite(curve.InputMaximum) || curve.InputMinimum >= curve.InputMaximum)
        {
            throw new IccProfileValidationException("A single-sampled curve requires a finite, increasing input domain.");
        }

        if (curve.Extension < 0 || curve.Extension > 1)
        {
            throw new IccProfileValidationException("A single-sampled curve extension selector must be zero or one.");
        }

        return ClutInterpolator.InterpolateOneDimensional(
            curve.Samples,
            input,
            curve.InputMinimum,
            curve.InputMaximum,
            curve.Extension == 1);
    }

    internal static double EvaluateSegmentedCurve(IccCurveSegment[] segments, double input)
    {
        ValidateFiniteInput(input, "segmented curve");
        ValidateSegments(segments);
        int index = 0;
        while (index < segments.Length - 1 && input > segments[index + 1].BreakPoint)
        {
            index = index + 1;
        }

        IccCurveSegment segment = segments[index];
        if (segment.Kind == IccCurveSegmentKind.Formula)
        {
            return EvaluateFormulaSegment(segment, input);
        }

        return EvaluateSampledSegment(segments, index, input);
    }

    internal static double EvaluateFormulaSegment(IccCurveSegment segment, double input)
    {
        int parameterCount = GetFormulaParameterCount(segment.FormulaType);
        if (segment.Parameters == null || parameterCount < 0 || segment.Parameters.Length != parameterCount)
        {
            throw new IccProfileValidationException($"Formula segment selector {segment.FormulaType} does not have its exact ICC parameter count.");
        }

        ValidateSamples(segment.Parameters, parameterCount, "formula segment parameters");
        double[] p = segment.Parameters;
        double output;
        switch (segment.FormulaType)
        {
            case 0:
                output = System.Math.Pow((p[1] * input) + p[2], p[0]) + p[3];
                break;
            case 1:
                output = (p[1] * System.Math.Log10((p[2] * System.Math.Pow(input, p[0])) + p[3])) + p[4];
                break;
            case 2:
                output = (p[0] * System.Math.Pow(p[1], (p[2] * input) + p[3])) + p[4];
                break;
            case 3:
                output = (p[1] * System.Math.Pow(System.Math.Max((p[2] * input) + p[3], 0.0), p[0])) + p[4];
                break;
            case 4:
                output = (p[1] * System.Math.Log((p[4] * System.Math.Pow(input, p[0])) - p[2])) + p[3];
                break;
            case 5:
                if (p[1] == 0.0)
                {
                    output = p[2];
                }
                else
                {
                    output = (p[5] * System.Math.Exp(((p[4] * System.Math.Pow(input, p[0])) - p[3]) / p[1])) + p[2];
                }

                break;
            case 6:
                output = EvaluateFormulaSix(p, input);
                break;
            case 7:
                output = EvaluateFormulaSeven(p, input);
                break;
            default:
                throw new IccProfileValidationException("The formula segment selector is unsupported.");
        }

        if (!System.Double.IsFinite(output))
        {
            throw new IccProfileValidationException($"Formula segment selector {segment.FormulaType} produced a non-finite value.");
        }

        return output;
    }

    internal static double EvaluateSampledSegment(IccCurveSegment[] segments, int segmentIndex, double input)
    {
        IccCurveSegment segment = segments[segmentIndex];
        double start = segment.BreakPoint;
        double end = segments[segmentIndex + 1].BreakPoint;
        double position = (input - start) / (end - start) * segment.Samples.Length;
        if (position <= 0.0)
        {
            return ResolveSampledStartValue(segments, segmentIndex, start);
        }

        if (position >= segment.Samples.Length)
        {
            return segment.Samples[segment.Samples.Length - 1];
        }

        int index = (int)System.Math.Floor(position);
        double amount = position - index;
        double lower = index == 0 ? ResolveSampledStartValue(segments, segmentIndex, start) : segment.Samples[index - 1];
        double upper = segment.Samples[index];
        return lower + ((upper - lower) * amount);
    }

    internal static double ResolveSampledStartValue(IccCurveSegment[] segments, int segmentIndex, double start)
    {
        IccCurveSegment previous = segments[segmentIndex - 1];
        if (previous.Kind == IccCurveSegmentKind.Formula)
        {
            return EvaluateFormulaSegment(previous, start);
        }

        return previous.Samples[previous.Samples.Length - 1];
    }

    internal static double EvaluateFormulaSix(double[] parameters, double input)
    {
        double powered = System.Math.Pow(input, parameters[1]);
        double denominator = parameters[3] - (parameters[4] * powered);
        if (denominator == 0.0)
        {
            return 0.0;
        }

        double numerator = System.Math.Max((parameters[6] * powered) - parameters[2], 0.0);
        return parameters[5] * System.Math.Pow(numerator / denominator, parameters[0]);
    }

    internal static double EvaluateFormulaSeven(double[] parameters, double input)
    {
        double powered = System.Math.Pow(input, parameters[1]);
        double denominator = 1.0 + (parameters[4] * powered);
        if (denominator == 0.0)
        {
            return 0.0;
        }

        double numerator = parameters[2] + (parameters[3] * powered);
        return parameters[5] * System.Math.Pow(numerator / denominator, parameters[0]);
    }

    internal static double EvaluateParametricCurve(IccParametricCurve curve, double input)
    {
        if (curve == null || curve.Parameters == null)
        {
            throw new IccProfileValidationException("Parametric curve data and its parameters cannot be null.");
        }

        int parameterCount = CurveTagCodec.GetParameterCount(curve.FunctionType);
        if (parameterCount < 0 || curve.Parameters.Length != parameterCount)
        {
            throw new IccProfileValidationException($"Parametric curve function {curve.FunctionType} does not have its exact ICC.1 parameter count.");
        }

        if (!CurveTagCodec.IsFinite(input))
        {
            throw new IccProfileValidationException("A parametric curve input must be finite.");
        }

        ValidateSamples(curve.Parameters, parameterCount, "parametric curve parameters");

        double x = input;
        if (x < 0.0)
        {
            x = 0.0;
        }
        else if (x > 1.0)
        {
            x = 1.0;
        }

        double[] p = curve.Parameters;
        double output;
        switch (curve.FunctionType)
        {
            case 0:
                output = System.Math.Pow(x, p[0]);
                break;
            case 1:
                if (x >= -p[2] / p[1])
                {
                    output = System.Math.Pow((p[1] * x) + p[2], p[0]);
                }
                else
                {
                    output = 0.0;
                }

                break;
            case 2:
                if (x >= -p[2] / p[1])
                {
                    output = System.Math.Pow((p[1] * x) + p[2], p[0]) + p[3];
                }
                else
                {
                    output = p[3];
                }

                break;
            case 3:
                if (x >= p[4])
                {
                    output = System.Math.Pow((p[1] * x) + p[2], p[0]);
                }
                else
                {
                    output = p[3] * x;
                }

                break;
            case 4:
                if (x >= p[4])
                {
                    output = System.Math.Pow((p[1] * x) + p[2], p[0]) + p[5];
                }
                else
                {
                    output = (p[3] * x) + p[6];
                }

                break;
            default:
                throw new IccProfileValidationException("The parametric curve function is unsupported.");
        }

        if (!System.Double.IsFinite(output))
        {
            throw new IccProfileValidationException($"Parametric curve function {curve.FunctionType} produced a non-finite value.");
        }

        if (output < 0.0)
        {
            return 0.0;
        }

        if (output > 1.0)
        {
            return 1.0;
        }

        return output;
    }

    internal static void ValidateSegments(IccCurveSegment[] segments)
    {
        if (segments == null || segments.Length == 0)
        {
            throw new IccProfileValidationException("A segmented curve requires at least one segment.");
        }

        int index = 0;
        while (index < segments.Length)
        {
            IccCurveSegment segment = segments[index];
            if (segment == null)
            {
                throw new IccProfileValidationException($"Segmented curve segment {index} cannot be null.");
            }

            if (index == 0)
            {
                if (!System.Double.IsNegativeInfinity(segment.BreakPoint))
                {
                    throw new IccProfileValidationException("The first segmented curve breakpoint must represent negative infinity.");
                }
            }
            else if (!System.Double.IsFinite(segment.BreakPoint) || segment.BreakPoint <= segments[index - 1].BreakPoint)
            {
                throw new IccProfileValidationException("Segmented curve breakpoints must be finite and strictly increasing after the first segment.");
            }

            if (segment.Kind == IccCurveSegmentKind.Formula)
            {
                int parameterCount = GetFormulaParameterCount(segment.FormulaType);
                if (segment.Parameters == null || parameterCount < 0 || segment.Parameters.Length != parameterCount)
                {
                    throw new IccProfileValidationException($"Segmented curve formula {index} has an invalid selector or parameter count.");
                }

                ValidateSamples(segment.Parameters, parameterCount, $"segmented curve formula {index} parameters");
            }
            else if (segment.Kind == IccCurveSegmentKind.Sampled)
            {
                if (index == 0 || index == segments.Length - 1)
                {
                    throw new IccProfileValidationException("A sampled segment requires preceding and following segments with finite breakpoints.");
                }

                ValidateSamples(segment.Samples, 1, $"segmented curve sampled segment {index}");
            }
            else
            {
                throw new IccProfileValidationException($"Segmented curve segment {index} has an unsupported kind.");
            }

            index = index + 1;
        }
    }

    internal static int GetFormulaParameterCount(int formulaType)
    {
        switch (formulaType)
        {
            case 0:
                return 4;
            case 1:
            case 2:
            case 3:
            case 4:
                return 5;
            case 5:
            case 7:
                return 6;
            case 6:
                return 7;
            default:
                return -1;
        }
    }

    internal static double InterpolateSamples(double[] samples, double normalizedPosition)
    {
        return ClutInterpolator.InterpolateOneDimensional(samples, normalizedPosition, 0.0, 1.0, false);
    }

    internal static void ValidateSamples(double[] samples, int minimumCount, string description)
    {
        if (samples == null || samples.Length < minimumCount)
        {
            throw new IccProfileValidationException($"The {description} requires at least {minimumCount} finite values.");
        }

        int index = 0;
        while (index < samples.Length)
        {
            if (!System.Double.IsFinite(samples[index]))
            {
                throw new IccProfileValidationException($"The {description} value {index} must be finite.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateFiniteInput(double input, string description)
    {
        if (!System.Double.IsFinite(input))
        {
            throw new IccProfileValidationException($"A {description} input must be finite.");
        }
    }

    internal static double ClampNormalized(double value)
    {
        if (value < 0.0)
        {
            return 0.0;
        }

        if (value > 1.0)
        {
            return 1.0;
        }

        return value;
    }
}
