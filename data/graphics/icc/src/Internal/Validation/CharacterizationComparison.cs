namespace icc;

internal static class CharacterizationComparison
{
    internal static IccComparisonReport Compare(IccProfile profile,
        IccCharacterizationSample[] samples, double tolerance)
    {
        if (!System.Double.IsFinite(tolerance) || tolerance < 0.0)
        {
            throw new IccProfileValidationException(
                "The characterization tolerance must be finite and non-negative.");
        }

        IccOperationResult probe = Probe(profile, samples);
        if (probe.Status != IccOperationStatus.Success)
        {
            return CreateReport(probe);
        }

        ProfileDirectionPlan forward = ProfileSampleComparison.CompileForward(profile, 0);
        IccColorDifference[] differences = new IccColorDifference[samples.Length];
        double[] values = new double[samples.Length];
        double[] mediaWhite = HasMediaRelativeSample(samples)
            ? ProfileTransformCompiler.ReadMediaWhite(profile) : new double[0];
        int index = 0;
        while (index < samples.Length)
        {
            IccCharacterizationSample sample = samples[index];
            double[] reference = NormalizeReference(sample, mediaWhite);
            double[] actual = BlackPointCompensation.ExecuteDeviceToLab(forward,
                sample.DeviceColor.Components);
            double deltaE = ColorDifferenceMath.CalculateColorDifference(reference, actual,
                IccColorDifferenceFormula.Ciede2000);
            differences[index] = ProfileSampleComparison.CreateDifference(reference, actual, deltaE);
            values[index] = deltaE;
            index = index + 1;
        }

        IccComparisonReport report = new IccComparisonReport();
        report.Differences = differences;
        report.Statistics.SampleCount = samples.Length;
        report.Statistics.Mean = QualityValidator.CalculateMean(values);
        report.Statistics.Maximum = QualityValidator.CalculateMaximum(values);
        report.Statistics.Percentile95 = ProfileSampleComparison.CalculatePercentile95(values);
        report.IsApproximatelyEquivalent = report.Statistics.Maximum <= tolerance;
        report.TransformPlan = ProfileSampleComparison.CreateTransformPlan(forward);
        report.Result = CreateResult(IccOperationStatus.Success,
            report.IsApproximatelyEquivalent
                ? $"The profile output agrees with the supplied characterization data within the CIEDE2000 tolerance {tolerance}."
                : $"The profile output does not agree with the supplied characterization data within the CIEDE2000 tolerance {tolerance}.");
        return report;
    }

    internal static IccOperationResult Probe(IccProfile profile, IccCharacterizationSample[] samples)
    {
        if (samples == null || samples.Length == 0)
        {
            return CreateResult(IccOperationStatus.NotFound,
                "No characterization colour pairs were supplied; comparison is not applicable.");
        }
        if (profile == null || profile.Header == null || profile.Header.Version == null
            || profile.Tags == null)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "Characterization comparison requires a profile with a header, version, and tag collection.");
        }
        if (profile.Header.ProfileClass != IccProfileClass.Input
            && profile.Header.ProfileClass != IccProfileClass.Display
            && profile.Header.ProfileClass != IccProfileClass.Output
            && profile.Header.ProfileClass != IccProfileClass.ColorSpace)
        {
            return CreateResult(IccOperationStatus.UnsupportedFeature,
                "Characterization comparison supports device and colour-space profiles with PCS output transforms.");
        }
        if (profile.Header.Version.Kind != IccVersionKind.Version2
            && profile.Header.Version.Kind != IccVersionKind.Version4
            && profile.Header.Version.Kind != IccVersionKind.Version5)
        {
            return CreateResult(IccOperationStatus.UnsupportedFeature,
                "Characterization comparison supports ICC version 2, version 4, and version 5 transforms.");
        }

        IntentSelection selection = QualityValidator.SelectTransform(profile,
            IccTransformDirection.DeviceToPcs);
        if (!selection.Success)
        {
            return CreateResult(IccOperationStatus.NotFound,
                "Characterization comparison requires a selectable device-to-PCS transform.");
        }
        int expectedChannels = ColorSpaceChannelCount.Decode(
            profile.Header.DataColorSpaceSignature);
        int index = 0;
        while (index < samples.Length)
        {
            IccOperationResult validation = ValidateSample(samples[index], profile,
                expectedChannels, index);
            if (validation.Status != IccOperationStatus.Success)
            {
                return validation;
            }
            index = index + 1;
        }
        if (HasMediaRelativeSample(samples) && !HasValidMediaWhite(profile))
        {
            return CreateResult(IccOperationStatus.NotFound,
                "Media-relative characterization comparison is not applicable without one finite, positive decoded media-white-point value.");
        }
        return CreateResult(IccOperationStatus.Success,
            "The profile and supplied characterization pairs can be compared.");
    }

    internal static IccOperationResult ValidateSample(IccCharacterizationSample sample,
        IccProfile profile, int expectedChannels, int index)
    {
        if (sample == null || sample.DeviceColor == null || sample.ReferenceLab == null
            || sample.DeviceColor.Components == null)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                $"Characterization sample {index} is incomplete.");
        }
        if (expectedChannels < 1 || sample.DeviceColor.Components.Length != expectedChannels
            || sample.DeviceColor.ColorSpaceSignature != profile.Header.DataColorSpaceSignature
            || sample.DeviceColor.ColorSpace != profile.Header.DataColorSpace)
        {
            return CreateResult(IccOperationStatus.Conflict,
                $"Characterization sample {index} does not match the profile data colour space and channel count.");
        }
        int channel = 0;
        while (channel < sample.DeviceColor.Components.Length)
        {
            double value = sample.DeviceColor.Components[channel];
            if (!System.Double.IsFinite(value) || value < 0.0 || value > 1.0)
            {
                return CreateResult(IccOperationStatus.InvalidData,
                    $"Characterization sample {index} device component {channel} must be finite and normalized from zero through one.");
            }
            channel = channel + 1;
        }
        if (!System.Double.IsFinite(sample.ReferenceLab.L)
            || !System.Double.IsFinite(sample.ReferenceLab.A)
            || !System.Double.IsFinite(sample.ReferenceLab.B))
        {
            return CreateResult(IccOperationStatus.InvalidData,
                $"Characterization sample {index} CIELAB reference must contain finite components.");
        }
        return CreateResult(IccOperationStatus.Success, "The characterization sample is valid.");
    }

    internal static double[] NormalizeReference(IccCharacterizationSample sample,
        double[] mediaWhite)
    {
        double[] lab = new double[]
        {
            sample.ReferenceLab.L, sample.ReferenceLab.A, sample.ReferenceLab.B
        };
        if (!sample.ReferenceIsMediaRelative)
        {
            return lab;
        }
        double[] d50 = new double[]
        {
            IccStandardColorimetry.D50X,
            IccStandardColorimetry.D50Y,
            IccStandardColorimetry.D50Z
        };
        double[] relativeXyz = ColorMathCore.ConvertLabToXyz(lab, d50);
        double[] perfectDiffuserXyz = PcsConverter.ConvertMediaRelativeToIccAbsolute(
            relativeXyz, mediaWhite, d50);
        return ColorMathCore.ConvertXyzToLab(perfectDiffuserXyz, d50);
    }

    internal static bool HasMediaRelativeSample(IccCharacterizationSample[] samples)
    {
        int index = 0;
        while (index < samples.Length)
        {
            if (samples[index] != null && samples[index].ReferenceIsMediaRelative)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasValidMediaWhite(IccProfile profile)
    {
        IccTag? tag = IntentSelector.FindTag(profile, IccTagSignatures.MediaWhitePoint);
        if (tag == null || tag.Data == null || tag.Data.XyzValues == null
            || tag.Data.XyzValues.Length != 1 || tag.Data.XyzValues[0] == null)
        {
            return false;
        }
        IccXyzNumber white = tag.Data.XyzValues[0];
        return System.Double.IsFinite(white.X) && System.Double.IsFinite(white.Y)
            && System.Double.IsFinite(white.Z) && white.X > 0.0 && white.Y > 0.0
            && white.Z > 0.0;
    }

    internal static IccComparisonReport CreateReport(IccOperationResult result)
    {
        IccComparisonReport report = new IccComparisonReport();
        report.Result = result;
        return report;
    }

    internal static IccOperationResult CreateResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }
}
