namespace icc;

internal static class ProfileSemanticFingerprint
{
    internal static byte[] Compute(IccProfile profile)
    {
        ValidateProfile(profile);
        IccProfile canonical = ProfileClone.CloneProfile(profile);
        ResolveTags(canonical);
        canonical.Header.ProfileId.Bytes = new byte[IccConstants.ProfileIdSize];
        canonical.Header.ProfileId.IsPresent = false;

        IccWriteOptions options = new IccWriteOptions();
        options.Mode = IccWriteMode.Canonical;
        options.ComputeProfileId = false;
        options.PreserveOriginalCreationDateTime = false;
        options.PreserveUnknownData = true;
        options.RejectInvalidProfile = false;
        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(canonical, options);
        byte[] semanticBytes = ProfileWriterCore.EncodePlan(plan);
        return System.Security.Cryptography.SHA256.HashData(semanticBytes);
    }

    internal static void ValidateProfile(IccProfile profile)
    {
        if (profile == null || profile.Header == null || profile.Header.Version == null || profile.Tags == null)
        {
            throw new IccProfileValidationException(
                "A profile, header, version, and tag collection are required for semantic fingerprinting.");
        }
        IccVersionKind version = profile.Header.Version.Kind;
        if (version != IccVersionKind.Version2 && version != IccVersionKind.Version4
            && version != IccVersionKind.Version5)
        {
            throw new IccProfileValidationException(
                "Semantic fingerprinting requires a supported ICC version 2, version 4, or version 5 profile.");
        }
    }

    internal static void ResolveTags(IccProfile profile)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag == null)
            {
                throw new IccProfileValidationException($"Profile tag {index} cannot be null.");
            }
            if (tag.DecodeState != null)
            {
                TagDecoder.GetDecodedData(tag);
            }
            index = index + 1;
        }
    }
}
