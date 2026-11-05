namespace raw;

internal static class RawWriteCompatibilityMatrixBuilder
{
    internal static RawWriteCompatibilityMatrix Create(RawWriteProfile[] profiles, RawFormat format)
    {
        if (profiles == null || format <= RawFormat.Unknown || format > RawFormat.Nef)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidArgument, "A profile collection and defined format are required for a write compatibility matrix.", -1);
        }
        System.Collections.Generic.List<RawWriteCompatibilityEntry> entries = new System.Collections.Generic.List<RawWriteCompatibilityEntry>();
        int profileIndex = 0;
        while (profileIndex < profiles.Length)
        {
            RawWriteProfile profile = profiles[profileIndex];
            if (profile != null && profile.Identity != null && profile.Identity.Format == format)
            {
                bool applicationFound = false;
                int evidenceIndex = 0;
                while (profile.Evidence != null && evidenceIndex < profile.Evidence.Length)
                {
                    RawProfileEvidence evidence = profile.Evidence[evidenceIndex];
                    if (evidence != null && evidence.Kind == RawProfileEvidenceKind.Application)
                    {
                        entries.Add(new RawWriteCompatibilityEntry(profile.Id, format, evidence.Tool, evidence.Version, evidence.SampleId, evidence.Check, evidence.Result, evidence.Details, profile.Experimental));
                        applicationFound = true;
                    }
                    evidenceIndex++;
                }
                if (!applicationFound)
                {
                    entries.Add(new RawWriteCompatibilityEntry(profile.Id, format, string.Empty, string.Empty, string.Empty, string.Empty, RawProfileEvidenceResult.Unknown, "No application/version compatibility result is recorded.", profile.Experimental));
                }
            }
            profileIndex++;
        }
        return new RawWriteCompatibilityMatrix(format, entries.ToArray());
    }
}
