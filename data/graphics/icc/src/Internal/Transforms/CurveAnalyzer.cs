namespace icc;

internal static class CurveAnalyzer
{
    internal const int MaximumSampleCount = 1048576;

    internal static double[] SampleCurve(IccCurve curve, int sampleCount)
    {
        ValidateSampleCount(sampleCount);
        if (curve == null)
        {
            throw new IccProfileValidationException("Curve data cannot be null.");
        }

        double minimum = GetOperationalDomainMinimum(curve);
        double maximum = GetOperationalDomainMaximum(curve);
        double[] samples = new double[sampleCount];
        int index = 0;
        while (index < sampleCount)
        {
            double amount = index / (double)(sampleCount - 1);
            double input = minimum + ((maximum - minimum) * amount);
            samples[index] = CurveMath.EvaluateCurve(curve, input);
            index = index + 1;
        }

        return samples;
    }

    internal static IccCurve ResampleCurve(IccCurve curve, int sampleCount)
    {
        double[] samples = SampleCurve(curve, sampleCount);
        IccCurve result = new IccCurve();
        result.Samples = samples;
        if (curve.Kind == IccCurveKind.SingleSampled)
        {
            result.Kind = IccCurveKind.SingleSampled;
            result.InputMinimum = curve.InputMinimum;
            result.InputMaximum = curve.InputMaximum;
            result.Extension = curve.Extension;
            result.Encoding = curve.Encoding;
        }
        else
        {
            result.Kind = IccCurveKind.Sampled;
        }

        result.Analysis = AnalyzeCurve(curve, sampleCount);
        return result;
    }

    internal static double[] SampleGammaCurve(double gamma, int sampleCount)
    {
        ValidateSampleCount(sampleCount);
        double[] samples = new double[sampleCount];
        int index = 0;
        while (index < sampleCount)
        {
            double input = index / (double)(sampleCount - 1);
            samples[index] = CurveMath.EvaluateGammaCurve(gamma, input);
            index = index + 1;
        }

        return samples;
    }

    internal static double EvaluateCurveSlope(IccCurve curve, double input)
    {
        if (curve == null)
        {
            throw new IccProfileValidationException("Curve data cannot be null.");
        }

        CurveMath.ValidateFiniteInput(input, "curve slope");
        switch (curve.Kind)
        {
            case IccCurveKind.Identity:
                return input < 0.0 || input > 1.0 ? 0.0 : 1.0;
            case IccCurveKind.Gamma:
                return EvaluateGammaSlope(curve.Gamma, input);
            case IccCurveKind.Sampled:
                return EvaluateSampledSlope(curve.Samples, input);
            case IccCurveKind.SingleSampled:
                return EvaluateSingleSampledSlope(curve, input);
            case IccCurveKind.Parametric:
                if (input < 0.0 || input > 1.0)
                {
                    return 0.0;
                }

                return EvaluateNumericalSlope(curve, input);
            case IccCurveKind.Segmented:
                return EvaluateNumericalSlope(curve, input);
            default:
                throw new IccProfileValidationException("The curve kind is unsupported.");
        }
    }

    internal static double EvaluateGammaSlope(double gamma, double input)
    {
        if (!System.Double.IsFinite(gamma) || gamma <= 0.0)
        {
            throw new IccProfileValidationException("A gamma curve exponent must be finite and positive.");
        }

        if (input < 0.0 || input > 1.0)
        {
            return 0.0;
        }

        if (input == 0.0)
        {
            if (gamma < 1.0)
            {
                return System.Double.PositiveInfinity;
            }

            return gamma == 1.0 ? 1.0 : 0.0;
        }

        return gamma * System.Math.Pow(input, gamma - 1.0);
    }

    internal static double EvaluateSampledSlope(double[] samples, double input)
    {
        CurveMath.ValidateSamples(samples, 2, "sampled curve");
        return EvaluateLinearSampleSlope(samples, input, 0.0, 1.0, 0);
    }

    internal static double EvaluateSingleSampledSlope(IccCurve curve, double input)
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

        return EvaluateLinearSampleSlope(curve.Samples, input, curve.InputMinimum, curve.InputMaximum, curve.Extension);
    }

    internal static double EvaluateLinearSampleSlope(double[] samples, double input, double minimum, double maximum, int extension)
    {
        if (input < minimum || input > maximum)
        {
            if (extension == 0)
            {
                return 0.0;
            }

            return input < minimum
                ? (samples[1] - samples[0]) * (samples.Length - 1) / (maximum - minimum)
                : (samples[samples.Length - 1] - samples[samples.Length - 2]) * (samples.Length - 1) / (maximum - minimum);
        }

        double position = (input - minimum) / (maximum - minimum) * (samples.Length - 1);
        int index = (int)System.Math.Floor(position);
        if (index >= samples.Length - 1)
        {
            index = samples.Length - 2;
        }

        double slope = (samples[index + 1] - samples[index]) * (samples.Length - 1) / (maximum - minimum);
        if (position == index && index > 0)
        {
            double previous = (samples[index] - samples[index - 1]) * (samples.Length - 1) / (maximum - minimum);
            slope = (previous + slope) / 2.0;
        }

        return slope;
    }

    internal static double EvaluateNumericalSlope(IccCurve curve, double input)
    {
        double step = System.Math.Max(System.Math.Abs(input) * 0.000001, 0.000001);
        double lower = input - step;
        double upper = input + step;
        if (curve.Kind == IccCurveKind.Parametric)
        {
            if (lower < 0.0)
            {
                lower = 0.0;
            }

            if (upper > 1.0)
            {
                upper = 1.0;
            }
        }

        double lowerValue = CurveMath.EvaluateCurve(curve, lower);
        double upperValue = CurveMath.EvaluateCurve(curve, upper);
        double slope = (upperValue - lowerValue) / (upper - lower);
        if (!System.Double.IsFinite(slope))
        {
            throw new IccProfileValidationException("Curve slope evaluation produced a non-finite value.");
        }

        return slope;
    }

    internal static IccCurveAnalysis AnalyzeCurve(IccCurve curve, int sampleCount)
    {
        double[] samples = SampleCurve(curve, sampleCount);
        IccCurveAnalysis analysis = new IccCurveAnalysis();
        analysis.DomainMinimum = GetOperationalDomainMinimum(curve);
        analysis.DomainMaximum = GetOperationalDomainMaximum(curve);
        analysis.SampleCount = sampleCount;
        analysis.Minimum = samples[0];
        analysis.Maximum = samples[0];
        analysis.MinimumSlope = EvaluateCurveSlope(curve, analysis.DomainMinimum);
        analysis.MaximumSlope = analysis.MinimumSlope;
        int direction = CurveInverter.DetermineMonotonicDirection(samples);
        analysis.IsMonotonic = direction != 2;
        analysis.IsIncreasing = direction == 0 || direction == 1;
        analysis.IsDecreasing = direction == 0 || direction == -1;
        analysis.IsContinuous = CheckCurveContinuity(curve);
        bool isStrict = HasStrictSampleDirection(samples, direction);
        analysis.IsInvertible = analysis.IsMonotonic && analysis.IsContinuous && isStrict;

        int index = 0;
        while (index < samples.Length)
        {
            if (samples[index] < analysis.Minimum)
            {
                analysis.Minimum = samples[index];
            }

            if (samples[index] > analysis.Maximum)
            {
                analysis.Maximum = samples[index];
            }

            double amount = index / (double)(samples.Length - 1);
            double input = analysis.DomainMinimum + ((analysis.DomainMaximum - analysis.DomainMinimum) * amount);
            double slope = EvaluateCurveSlope(curve, input);
            if (slope < analysis.MinimumSlope)
            {
                analysis.MinimumSlope = slope;
            }

            if (slope > analysis.MaximumSlope)
            {
                analysis.MaximumSlope = slope;
            }

            index = index + 1;
        }

        if (analysis.IsMonotonic && direction != 0)
        {
            analysis.MaximumRoundTripError = MeasureRoundTripError(curve, samples, analysis.DomainMinimum, analysis.DomainMaximum);
        }

        analysis.Result.Status = IccOperationStatus.Success;
        analysis.Result.Message = "Analyzed curve sampling, monotonicity, continuity, slope, range, and inverse round-trip behavior.";
        return analysis;
    }

    internal static IccOperationResult AnalyzeSampledCurve(double[] samples)
    {
        IccOperationResult invalid = ValidateSampledCurveAnalysisInput(samples);
        if (invalid.Status != IccOperationStatus.Success)
        {
            return invalid;
        }
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Sampled;
        curve.Samples = samples;
        IccCurveAnalysis analysis = AnalyzeCurve(curve, samples.Length);
        IccOperationResult result = new IccOperationResult();
        if (analysis.IsInvertible)
        {
            result.Status = IccOperationStatus.Success;
            result.Message = "The sampled curve is continuous, strictly monotonic, and invertible.";
        }
        else
        {
            result.Status = IccOperationStatus.Conflict;
            result.Message = "The sampled curve is not strictly monotonic and uniquely invertible.";
        }

        return result;
    }

    internal static IccOperationResult ValidateSampledCurveAnalysisInput(
        double[] samples)
    {
        IccOperationResult result = new IccOperationResult();
        if (samples == null || samples.Length < 2)
        {
            result.Status = IccOperationStatus.InvalidData;
            result.Message = "Sampled-curve analysis requires at least two finite samples.";
            return result;
        }
        int index = 0;
        while (index < samples.Length)
        {
            if (!System.Double.IsFinite(samples[index]))
            {
                result.Status = IccOperationStatus.InvalidData;
                result.Message = $"Sampled-curve analysis sample {index} must be finite.";
                return result;
            }
            index = index + 1;
        }
        result.Status = IccOperationStatus.Success;
        result.Message = "The sampled-curve analysis input is valid.";
        return result;
    }

    internal static double MeasureRoundTripError(IccCurve curve, double[] samples, double minimum, double maximum)
    {
        double maximumError = 0.0;
        int index = 0;
        while (index < samples.Length)
        {
            IccCurveInversionResult inverse = CurveInverter.InvertCurve(curve, samples[index]);
            if (!inverse.HasInput)
            {
                return maximum - minimum;
            }

            double amount = index / (double)(samples.Length - 1);
            double input = minimum + ((maximum - minimum) * amount);
            double error = System.Math.Abs(inverse.Input - input);
            if (error > maximumError)
            {
                maximumError = error;
            }

            index = index + 1;
        }

        return maximumError;
    }

    internal static bool CheckCurveContinuity(IccCurve curve)
    {
        if (curve.Kind == IccCurveKind.Parametric)
        {
            return CheckParametricContinuity(curve.Parametric);
        }

        if (curve.Kind == IccCurveKind.Segmented)
        {
            return CheckSegmentedContinuity(curve.Segments);
        }

        return true;
    }

    internal static bool CheckParametricContinuity(IccParametricCurve? curve)
    {
        if (curve == null || curve.Parameters == null)
        {
            throw new IccProfileValidationException("Parametric curve data and its parameters cannot be null.");
        }

        int count = CurveTagCodec.GetParameterCount(curve.FunctionType);
        if (count < 0 || curve.Parameters.Length != count)
        {
            throw new IccProfileValidationException("The parametric curve selector or parameter count is invalid.");
        }

        if (curve.FunctionType < 3)
        {
            return true;
        }

        double[] p = curve.Parameters;
        double boundary = p[4];
        if (boundary <= 0.0 || boundary >= 1.0)
        {
            return true;
        }

        double lower;
        double upper;
        if (curve.FunctionType == 3)
        {
            lower = p[3] * boundary;
            upper = System.Math.Pow((p[1] * boundary) + p[2], p[0]);
        }
        else
        {
            lower = (p[3] * boundary) + p[6];
            upper = System.Math.Pow((p[1] * boundary) + p[2], p[0]) + p[5];
        }

        if (!System.Double.IsFinite(lower) || !System.Double.IsFinite(upper))
        {
            throw new IccProfileValidationException("Parametric curve continuity evaluation produced a non-finite value.");
        }

        return AreValuesClose(CurveMath.ClampNormalized(lower), CurveMath.ClampNormalized(upper));
    }

    internal static bool CheckSegmentedContinuity(IccCurveSegment[] segments)
    {
        CurveMath.ValidateSegments(segments);
        int index = 1;
        while (index < segments.Length)
        {
            double boundary = segments[index].BreakPoint;
            IccCurveSegment previous = segments[index - 1];
            IccCurveSegment current = segments[index];
            double left = previous.Kind == IccCurveSegmentKind.Formula
                ? CurveMath.EvaluateFormulaSegment(previous, boundary)
                : previous.Samples[previous.Samples.Length - 1];
            double right = current.Kind == IccCurveSegmentKind.Formula
                ? CurveMath.EvaluateFormulaSegment(current, boundary)
                : CurveMath.ResolveSampledStartValue(segments, index, boundary);
            if (!AreValuesClose(left, right))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasStrictSampleDirection(double[] samples, int direction)
    {
        if (direction != 1 && direction != -1)
        {
            return false;
        }

        int index = 1;
        while (index < samples.Length)
        {
            if (samples[index] == samples[index - 1])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool AreValuesClose(double first, double second)
    {
        double scale = System.Math.Max(1.0, System.Math.Max(System.Math.Abs(first), System.Math.Abs(second)));
        return System.Math.Abs(first - second) <= 0.000000001 * scale;
    }

    internal static double GetOperationalDomainMinimum(IccCurve curve)
    {
        if (curve.Kind == IccCurveKind.SingleSampled)
        {
            if (!System.Double.IsFinite(curve.InputMinimum))
            {
                throw new IccProfileValidationException("A single-sampled curve input minimum must be finite.");
            }

            return curve.InputMinimum;
        }

        return 0.0;
    }

    internal static double GetOperationalDomainMaximum(IccCurve curve)
    {
        if (curve.Kind == IccCurveKind.SingleSampled)
        {
            if (!System.Double.IsFinite(curve.InputMaximum) || curve.InputMaximum <= curve.InputMinimum)
            {
                throw new IccProfileValidationException("A single-sampled curve input maximum must be finite and greater than its minimum.");
            }

            return curve.InputMaximum;
        }

        return 1.0;
    }

    internal static void ValidateSampleCount(int sampleCount)
    {
        if (sampleCount < 2 || sampleCount > MaximumSampleCount)
        {
            throw new IccProfileValidationException($"A curve sample count must be between 2 and {MaximumSampleCount}.");
        }
    }
}
