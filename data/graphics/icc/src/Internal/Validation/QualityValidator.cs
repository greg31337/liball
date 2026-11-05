namespace icc;

internal static class QualityValidator
{
    internal const int MaximumSampleCount = 65536;
    internal const int CurveSampleCount = 257;
    internal const double ContinuityStep = 0.000244140625;

    internal static IccQualityStatistics Analyze(IccProfile profile, int sampleCount)
    {
        IccOperationResult probe = Probe(profile);
        if (probe.Status != IccOperationStatus.Success)
        {
            throw new IccTransformException(probe.Message);
        }
        if (sampleCount < 3 || sampleCount > MaximumSampleCount)
        {
            throw new IccProfileValidationException(
                $"The quality sample count must be between 3 and {MaximumSampleCount}.");
        }

        ProfileDirectionPlan forward = ProfileTransformCompiler.CompileProfileDirection(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.AllowAny, IccInterpolationKind.Multilinear, 0);
        ProfileDirectionPlan reverse = ProfileTransformCompiler.CompileProfileDirection(profile,
            IccTransformDirection.PcsToDevice, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.AllowAny, IccInterpolationKind.Multilinear, 0);
        ValidateCompiledPair(forward, reverse);

        double[] firstValues = new double[sampleCount];
        double[] secondValues = new double[sampleCount];
        SampleRoundTrips(forward, reverse, firstValues, secondValues);
        double[] smoothnessValues = new double[sampleCount * forward.SourceChannels];
        double continuityMaximum = SampleSmoothness(forward, sampleCount, smoothnessValues);

        IccQualityStatistics statistics = new IccQualityStatistics();
        statistics.SampleCount = sampleCount;
        PopulateRoundTripStatistics(statistics, firstValues, secondValues);
        PopulateSmoothnessStatistics(statistics, smoothnessValues, continuityMaximum);
        AnalyzeCurves(statistics, forward.Stages, reverse.Stages);
        return statistics;
    }

    internal static IccOperationResult Probe(IccProfile profile)
    {
        if (profile == null || profile.Header == null || profile.Header.Version == null
            || profile.Tags == null)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "Profile quality analysis requires a profile with a header, version, and tag collection.");
        }
        if (profile.Header.ProfileClass != IccProfileClass.Input
            && profile.Header.ProfileClass != IccProfileClass.Display
            && profile.Header.ProfileClass != IccProfileClass.Output
            && profile.Header.ProfileClass != IccProfileClass.ColorSpace)
        {
            return CreateResult(IccOperationStatus.UnsupportedFeature,
                "Profile quality analysis supports bidirectional device and colour-space profiles.");
        }
        if (profile.Header.Version.Kind != IccVersionKind.Version2
            && profile.Header.Version.Kind != IccVersionKind.Version4
            && profile.Header.Version.Kind != IccVersionKind.Version5)
        {
            return CreateResult(IccOperationStatus.UnsupportedFeature,
                "Profile quality analysis supports ICC version 2, version 4, and version 5 transforms.");
        }

        IntentSelection forward = SelectTransform(profile,
            IccTransformDirection.DeviceToPcs);
        IntentSelection reverse = SelectTransform(profile,
            IccTransformDirection.PcsToDevice);
        if (!forward.Success || !reverse.Success)
        {
            return CreateResult(IccOperationStatus.NotFound,
                "Profile quality analysis requires selectable device-to-PCS and PCS-to-device transforms.");
        }
        return CreateResult(IccOperationStatus.Success,
            "The profile provides a bidirectional transform pair for deterministic quality sampling.");
    }

    internal static IntentSelection SelectTransform(IccProfile profile,
        IccTransformDirection direction)
    {
        if (profile.Header.Version.Kind == IccVersionKind.Version5)
        {
            return IntentSelector.SelectVersionFiveTransform(profile, direction,
                IccRenderingIntent.MediaRelativeColorimetric,
                IccIntentFallback.AllowAny);
        }
        return IntentSelector.SelectIcc1Transform(profile, direction,
            IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.AllowAny, false);
    }

    internal static void ValidateCompiledPair(ProfileDirectionPlan forward, ProfileDirectionPlan reverse)
    {
        if (forward.SourceChannels != reverse.DestinationChannels
            || forward.DestinationChannels != 3 || reverse.SourceChannels != 3)
        {
            throw new IccTransformException(
                "Profile quality analysis requires matching device channels and three-channel PCS transforms.");
        }
    }

    internal static void SampleRoundTrips(ProfileDirectionPlan forward, ProfileDirectionPlan reverse,
        double[] firstValues, double[] secondValues)
    {
        int index = 0;
        while (index < firstValues.Length)
        {
            double[] sourceLab = CreateLabSample(index, firstValues.Length);
            double[] firstLab = ExecuteRoundTrip(forward, reverse, sourceLab);
            double[] secondLab = ExecuteRoundTrip(forward, reverse, firstLab);
            firstValues[index] = ColorDifferenceMath.CalculateColorDifference(sourceLab, firstLab,
                IccColorDifferenceFormula.Ciede2000);
            secondValues[index] = ColorDifferenceMath.CalculateColorDifference(firstLab, secondLab,
                IccColorDifferenceFormula.Ciede2000);
            index = index + 1;
        }
    }

    internal static double[] ExecuteRoundTrip(ProfileDirectionPlan forward,
        ProfileDirectionPlan reverse, double[] sourceLab)
    {
        double[] pcs = BlackPointCompensation.ConvertLabToPcs(sourceLab, reverse.PcsKind);
        double[] device = BlackPointCompensation.ExecuteDirection(reverse, pcs);
        return BlackPointCompensation.ExecuteDeviceToLab(forward, device);
    }

    internal static double[] CreateLabSample(int sampleIndex, int sampleCount)
    {
        if (sampleIndex == 0)
        {
            return new double[] { 0.0, 0.0, 0.0 };
        }
        if (sampleIndex == 1 && sampleCount > 1)
        {
            return new double[] { 100.0, 0.0, 0.0 };
        }
        int value = sampleIndex - 1;
        return new double[]
        {
            ProfileSampleComparison.RadicalInverse(value, 2) * 100.0,
            (ProfileSampleComparison.RadicalInverse(value, 3) * 255.0) - 128.0,
            (ProfileSampleComparison.RadicalInverse(value, 5) * 255.0) - 128.0
        };
    }

    internal static double SampleSmoothness(ProfileDirectionPlan forward, int sampleCount,
        double[] smoothnessValues)
    {
        double continuityMaximum = 0.0;
        int valueIndex = 0;
        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            double[] center = ProfileSampleComparison.CreateSample(sampleIndex, sampleCount,
                forward.SourceChannels);
            int channel = 0;
            while (channel < forward.SourceChannels)
            {
                double[] lower = CopyValues(center);
                double[] localCenter = CopyValues(center);
                double[] upper = CopyValues(center);
                SetLocalInterval(center[channel], lower, localCenter, upper, channel);
                double[] lowerLab = BlackPointCompensation.ExecuteDeviceToLab(forward, lower);
                double[] centerLab = BlackPointCompensation.ExecuteDeviceToLab(forward, localCenter);
                double[] upperLab = BlackPointCompensation.ExecuteDeviceToLab(forward, upper);
                double continuity = ColorDifferenceMath.CalculateColorDifference(lowerLab, upperLab,
                    IccColorDifferenceFormula.Ciede2000);
                if (continuity > continuityMaximum)
                {
                    continuityMaximum = continuity;
                }
                double[] midpoint = new double[]
                {
                    (lowerLab[0] + upperLab[0]) / 2.0,
                    (lowerLab[1] + upperLab[1]) / 2.0,
                    (lowerLab[2] + upperLab[2]) / 2.0
                };
                smoothnessValues[valueIndex] = ColorDifferenceMath.CalculateColorDifference(centerLab,
                    midpoint, IccColorDifferenceFormula.Ciede2000);
                valueIndex = valueIndex + 1;
                channel = channel + 1;
            }
            sampleIndex = sampleIndex + 1;
        }
        return continuityMaximum;
    }

    internal static void SetLocalInterval(double value, double[] lower, double[] center,
        double[] upper, int channel)
    {
        if (value < ContinuityStep)
        {
            lower[channel] = value;
            center[channel] = value + ContinuityStep;
            upper[channel] = value + (2.0 * ContinuityStep);
        }
        else if (value > 1.0 - ContinuityStep)
        {
            lower[channel] = value - (2.0 * ContinuityStep);
            center[channel] = value - ContinuityStep;
            upper[channel] = value;
        }
        else
        {
            lower[channel] = value - ContinuityStep;
            upper[channel] = value + ContinuityStep;
        }
    }

    internal static double[] CopyValues(double[] source)
    {
        double[] copy = new double[source.Length];
        source.CopyTo(copy, 0);
        return copy;
    }

    internal static void PopulateRoundTripStatistics(IccQualityStatistics statistics,
        double[] firstValues, double[] secondValues)
    {
        statistics.FirstRoundTripMean = CalculateMean(firstValues);
        statistics.FirstRoundTripMaximum = CalculateMaximum(firstValues);
        statistics.FirstRoundTripPercentile95 = ProfileSampleComparison.CalculatePercentile95(firstValues);
        statistics.SecondRoundTripMean = CalculateMean(secondValues);
        statistics.SecondRoundTripMaximum = CalculateMaximum(secondValues);
        statistics.SecondRoundTripPercentile95 = ProfileSampleComparison.CalculatePercentile95(secondValues);
        double[] combined = new double[firstValues.Length + secondValues.Length];
        firstValues.CopyTo(combined, 0);
        secondValues.CopyTo(combined, firstValues.Length);
        statistics.Mean = CalculateMean(combined);
        statistics.Maximum = CalculateMaximum(combined);
        statistics.Percentile95 = ProfileSampleComparison.CalculatePercentile95(combined);
    }

    internal static void PopulateSmoothnessStatistics(IccQualityStatistics statistics,
        double[] values, double continuityMaximum)
    {
        statistics.TransformContinuityMaximum = continuityMaximum;
        statistics.TransformSmoothnessMean = CalculateMean(values);
        statistics.TransformSmoothnessMaximum = CalculateMaximum(values);
        statistics.TransformSmoothnessPercentile95 = ProfileSampleComparison.CalculatePercentile95(values);
    }

    internal static void AnalyzeCurves(IccQualityStatistics statistics, PipelineStage[] forward,
        PipelineStage[] reverse)
    {
        System.Collections.Generic.List<IccCurve> curves = new System.Collections.Generic.List<IccCurve>();
        AddStageCurves(curves, forward);
        AddStageCurves(curves, reverse);
        statistics.CurveCount = curves.Count;
        int index = 0;
        while (index < curves.Count)
        {
            IccCurveAnalysis analysis = CurveAnalyzer.AnalyzeCurve(curves[index], CurveSampleCount);
            if (!analysis.IsContinuous)
            {
                statistics.CurvesAreContinuous = false;
            }
            if (!analysis.IsInvertible)
            {
                statistics.CurvesAreInvertible = false;
            }
            if (analysis.MaximumRoundTripError > statistics.CurveMaximumRoundTripError)
            {
                statistics.CurveMaximumRoundTripError = analysis.MaximumRoundTripError;
            }
            index = index + 1;
        }
    }

    internal static void AddStageCurves(System.Collections.Generic.List<IccCurve> curves,
        PipelineStage[] stages)
    {
        int stageIndex = 0;
        while (stageIndex < stages.Length)
        {
            PipelineStage stage = stages[stageIndex];
            int curveIndex = 0;
            while (curveIndex < stage.Curves.Length)
            {
                AddUniqueCurve(curves, stage.Curves[curveIndex]);
                curveIndex = curveIndex + 1;
            }
            if (stage.ToneMap != null)
            {
                AddUniqueCurve(curves, stage.ToneMap.LuminanceCurve);
            }
            stageIndex = stageIndex + 1;
        }
    }

    internal static void AddUniqueCurve(System.Collections.Generic.List<IccCurve> curves, IccCurve curve)
    {
        int index = 0;
        while (index < curves.Count)
        {
            if (System.Object.ReferenceEquals(curves[index], curve))
            {
                return;
            }
            index = index + 1;
        }
        curves.Add(curve);
    }

    internal static double CalculateMean(double[] values)
    {
        double total = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            total = total + values[index];
            index = index + 1;
        }
        return total / values.Length;
    }

    internal static double CalculateMaximum(double[] values)
    {
        double maximum = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] > maximum)
            {
                maximum = values[index];
            }
            index = index + 1;
        }
        return maximum;
    }

    internal static IccOperationResult CreateResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }
}
