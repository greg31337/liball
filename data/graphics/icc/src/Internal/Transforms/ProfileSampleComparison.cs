namespace icc;

internal static class ProfileSampleComparison
{
    internal const int MaximumSampleCount = 65536;
    internal static readonly int[] PrimeBases = new int[]
    {
        2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47
    };

    internal static IccComparisonReport Run(IccProfile firstProfile, IccProfile secondProfile,
        int sampleCount, double tolerance, bool includeDifferences)
    {
        ValidateSampleRequest(firstProfile, secondProfile, sampleCount, tolerance);
        ProfileDirectionPlan first = CompileForward(firstProfile, 0);
        ProfileDirectionPlan second = CompileForward(secondProfile, 1);
        if (first.SourceChannels != second.SourceChannels || first.DestinationChannels != 3
            || second.DestinationChannels != 3)
        {
            throw new IccTransformException(
                "Comparable profiles must expose equal input channels and three-channel PCS outputs.");
        }

        IccColorDifference[] differences = includeDifferences
            ? new IccColorDifference[sampleCount] : new IccColorDifference[0];
        double[] values = new double[sampleCount];
        double total = 0.0;
        double maximum = 0.0;
        int index = 0;
        while (index < sampleCount)
        {
            double[] input = CreateSample(index, sampleCount, first.SourceChannels);
            double[] firstLab = BlackPointCompensation.ExecuteDeviceToLab(first, input);
            double[] secondLab = BlackPointCompensation.ExecuteDeviceToLab(second, input);
            double difference = ColorDifferenceMath.CalculateColorDifference(firstLab, secondLab,
                IccColorDifferenceFormula.Ciede2000);
            values[index] = difference;
            total = total + difference;
            if (difference > maximum)
            {
                maximum = difference;
            }
            if (includeDifferences)
            {
                differences[index] = CreateDifference(firstLab, secondLab, difference);
            }
            index = index + 1;
        }

        IccComparisonReport report = new IccComparisonReport();
        report.IsApproximatelyEquivalent = maximum <= tolerance;
        report.Differences = differences;
        report.Statistics.SampleCount = sampleCount;
        report.Statistics.Mean = total / sampleCount;
        report.Statistics.Maximum = maximum;
        report.Statistics.Percentile95 = CalculatePercentile95(values);
        report.Result = CreateComparisonResult(report.IsApproximatelyEquivalent, tolerance);
        report.TransformPlan = CreateTransformPlan(first);
        return report;
    }

    internal static IccOperationResult Probe(IccProfile firstProfile, IccProfile secondProfile)
    {
        if (!HasBasicProfile(firstProfile) || !HasBasicProfile(secondProfile))
        {
            return CreateOperationResult(IccOperationStatus.InvalidData,
                "Profile comparison requires two profiles with headers, versions, and tag collections.");
        }
        if (!HasSupportedVersion(firstProfile) || !HasSupportedVersion(secondProfile))
        {
            return CreateOperationResult(IccOperationStatus.UnsupportedFeature,
                "Sampled comparison supports ICC version 2 and version 4 transforms.");
        }
        if (!HasCompatibleHeaders(firstProfile, secondProfile))
        {
            return CreateOperationResult(IccOperationStatus.Conflict,
                "The profiles do not have substitutable classes, device colour spaces, and connection spaces.");
        }
        if (!IsComparableClass(firstProfile.Header.ProfileClass))
        {
            return CreateOperationResult(IccOperationStatus.UnsupportedFeature,
                "Sampled comparison supports device and colour-space profiles with PCS output transforms.");
        }

        IntentSelection first = IntentSelector.SelectIcc1Transform(firstProfile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.AllowAny, false);
        IntentSelection second = IntentSelector.SelectIcc1Transform(secondProfile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.AllowAny, false);
        if (!first.Success || !second.Success)
        {
            return CreateOperationResult(IccOperationStatus.NotFound,
                "Both profiles must provide a selectable device-to-PCS transform.");
        }
        return CreateOperationResult(IccOperationStatus.Success,
            "The profiles provide compatible transforms for deterministic substitutability sampling.");
    }

    internal static void ValidateSampleRequest(IccProfile firstProfile, IccProfile secondProfile,
        int sampleCount, double tolerance)
    {
        IccOperationResult probe = Probe(firstProfile, secondProfile);
        if (probe.Status != IccOperationStatus.Success)
        {
            throw new IccTransformException(probe.Message);
        }
        if (sampleCount < 1 || sampleCount > MaximumSampleCount)
        {
            throw new IccProfileValidationException(
                $"The comparison sample count must be between 1 and {MaximumSampleCount}.");
        }
        if (!System.Double.IsFinite(tolerance) || tolerance < 0.0)
        {
            throw new IccProfileValidationException("The comparison tolerance must be finite and non-negative.");
        }
    }

    internal static ProfileDirectionPlan CompileForward(IccProfile profile, int profileIndex)
    {
        return ProfileTransformCompiler.CompileProfileDirection(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.AllowAny, IccInterpolationKind.Multilinear, profileIndex);
    }

    internal static double[] CreateSample(int sampleIndex, int sampleCount, int channels)
    {
        double[] sample = new double[channels];
        if (sampleIndex == 1 && sampleCount > 1)
        {
            int channel = 0;
            while (channel < channels)
            {
                sample[channel] = 1.0;
                channel = channel + 1;
            }
            return sample;
        }
        if (sampleIndex < 2)
        {
            return sample;
        }

        int index = 0;
        while (index < channels)
        {
            sample[index] = RadicalInverse(sampleIndex - 1, PrimeBases[index]);
            index = index + 1;
        }
        return sample;
    }

    internal static double RadicalInverse(int value, int numberBase)
    {
        double inverse = 1.0 / numberBase;
        double factor = inverse;
        double result = 0.0;
        int remaining = value;
        while (remaining > 0)
        {
            result = result + ((remaining % numberBase) * factor);
            remaining = remaining / numberBase;
            factor = factor * inverse;
        }
        return result;
    }

    internal static IccColorDifference CreateDifference(double[] first, double[] second, double deltaE)
    {
        IccColorDifference difference = new IccColorDifference();
        difference.Formula = IccColorDifferenceFormula.Ciede2000;
        difference.First.L = first[0];
        difference.First.A = first[1];
        difference.First.B = first[2];
        difference.Second.L = second[0];
        difference.Second.A = second[1];
        difference.Second.B = second[2];
        difference.DeltaE = deltaE;
        return difference;
    }

    internal static double CalculatePercentile95(double[] values)
    {
        double[] sorted = new double[values.Length];
        values.CopyTo(sorted, 0);
        System.Array.Sort(sorted);
        int index = (int)System.Math.Ceiling(sorted.Length * 0.95) - 1;
        if (index < 0)
        {
            index = 0;
        }
        return sorted[index];
    }

    internal static IccTransformPlan CreateTransformPlan(ProfileDirectionPlan direction)
    {
        IccTransformOptions options = new IccTransformOptions();
        options.Direction = IccTransformDirection.DeviceToPcs;
        options.RenderingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        options.IntentFallback = IccIntentFallback.AllowAny;
        options.Interpolation = IccInterpolationKind.Multilinear;
        options.ClipOutput = false;
        return ColorTransformCore.CreatePublicPlan(direction.Stages, direction.SourceChannels,
            direction.DestinationChannels, options, 0);
    }

    internal static IccOperationResult CreateComparisonResult(bool equivalent, double tolerance)
    {
        return CreateOperationResult(IccOperationStatus.Success,
            equivalent
                ? $"The sampled transforms are substitutable within the CIEDE2000 tolerance {tolerance}."
                : $"The sampled transforms are not substitutable within the CIEDE2000 tolerance {tolerance}.");
    }

    internal static IccOperationResult CreateOperationResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }

    internal static bool HasBasicProfile(IccProfile profile)
    {
        return profile != null && profile.Header != null && profile.Header.Version != null
            && profile.Tags != null;
    }

    internal static bool HasSupportedVersion(IccProfile profile)
    {
        return profile.Header.Version.Kind == IccVersionKind.Version2
            || profile.Header.Version.Kind == IccVersionKind.Version4;
    }

    internal static bool HasCompatibleHeaders(IccProfile firstProfile, IccProfile secondProfile)
    {
        return firstProfile.Header.ProfileClass == secondProfile.Header.ProfileClass
            && firstProfile.Header.DataColorSpaceSignature == secondProfile.Header.DataColorSpaceSignature
            && firstProfile.Header.ProfileConnectionSpace == secondProfile.Header.ProfileConnectionSpace
            && firstProfile.Header.ProfileConnectionSpaceSignature
                == secondProfile.Header.ProfileConnectionSpaceSignature
            && ColorSpaceChannelCount.Decode(firstProfile.Header.DataColorSpaceSignature) > 0;
    }

    internal static bool IsComparableClass(IccProfileClass profileClass)
    {
        return profileClass == IccProfileClass.Input || profileClass == IccProfileClass.Display
            || profileClass == IccProfileClass.Output || profileClass == IccProfileClass.ColorSpace;
    }
}
