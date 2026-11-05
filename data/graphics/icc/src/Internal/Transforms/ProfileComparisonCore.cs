namespace icc;

internal class ProfileComparisonCore
{
    internal ProfileComparisonCore()
    {
    }

    internal bool CompareExact(IccProfile firstProfile, IccProfile secondProfile)
    {
        byte[] first = ComputeFingerprint(firstProfile);
        byte[] second = ComputeFingerprint(secondProfile);
        return ProfileWriterCore.PayloadsEqual(first, second);
    }

    internal double CompareSamples(IccProfile firstProfile, IccProfile secondProfile, int sampleCount)
    {
        IccComparisonReport report = ProfileSampleComparison.Run(firstProfile, secondProfile,
            sampleCount, double.MaxValue, false);
        return report.Statistics.Maximum;
    }

    internal IccComparisonReport CompareReport(IccProfile firstProfile, IccProfile secondProfile,
        int sampleCount, double tolerance)
    {
        IccComparisonReport report = ProfileSampleComparison.Run(firstProfile, secondProfile,
            sampleCount, tolerance, true);
        report.FirstFingerprint = ComputeFingerprint(firstProfile);
        report.SecondFingerprint = ComputeFingerprint(secondProfile);
        report.IsExactMatch = ProfileWriterCore.PayloadsEqual(report.FirstFingerprint, report.SecondFingerprint);
        return report;
    }

    internal byte[] ComputeFingerprint(IccProfile profile)
    {
        return ProfileSemanticFingerprint.Compute(profile);
    }

    internal IccOperationResult Probe(IccProfile firstProfile, IccProfile secondProfile)
    {
        return ProfileSampleComparison.Probe(firstProfile, secondProfile);
    }
}
