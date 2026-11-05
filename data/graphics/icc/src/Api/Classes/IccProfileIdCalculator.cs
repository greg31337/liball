namespace icc;

/// <summary>Computes, verifies, and updates ICC Profile IDs.</summary>
public class IccProfileIdCalculator : IIccProfileIdCalculator
{
    /// <summary>Initializes a Profile ID calculator.</summary>
    public IccProfileIdCalculator()
    {
    }

    /// <summary>Computes an ID from encoded bytes.</summary>
    /// <param name="profileBytes">The encoded profile bytes.</param>
    /// <returns>The computed ID.</returns>
    public IccProfileId ComputeProfileIdBytes(System.ReadOnlySpan<byte> profileBytes)
    {
        return ProfileIdCore.Compute(profileBytes);
    }

    /// <summary>Computes an ID from a profile model.</summary>
    /// <param name="profile">The profile model.</param>
    /// <param name="options">The writing options.</param>
    /// <returns>The computed ID.</returns>
    public IccProfileId ComputeProfileIdData(IccProfile profile, IccWriteOptions options)
    {
        if (profile == null || options == null)
        {
            throw new IccProfileWriteException("A profile and write options are required for Profile ID calculation.");
        }

        ProfileWriterCore.ValidateInputs(profile, options);
        IccProfile calculationProfile = ProfileClone.CloneProfile(profile);
        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(calculationProfile, options);
        byte[] profileBytes = ProfileWriterCore.EncodePlan(plan);
        return ProfileIdCore.Compute(profileBytes);
    }

    /// <summary>Verifies a profile ID.</summary>
    /// <param name="profile">The profile model.</param>
    /// <param name="options">The writing options.</param>
    /// <returns>The verification result.</returns>
    public IccOperationResult VerifyProfileId(IccProfile profile, IccWriteOptions options)
    {
        if (profile == null || profile.Header == null || profile.Header.Version == null || profile.Header.ProfileId == null || options == null)
        {
            return ProfileIdCore.CreateResult(IccOperationStatus.InvalidData, "A profile, header, Profile ID, version, and write options are required for verification.");
        }

        if (profile.Header.Version.Kind != IccVersionKind.Version4 && profile.Header.Version.Kind != IccVersionKind.Version5)
        {
            return ProfileIdCore.CreateResult(IccOperationStatus.UnsupportedFeature, "Profile ID verification is supported only for ICC version 4 and version 5 profiles.");
        }

        if (profile.Header.ProfileId.Bytes == null || profile.Header.ProfileId.Bytes.Length != IccConstants.ProfileIdSize)
        {
            return ProfileIdCore.CreateResult(IccOperationStatus.InvalidData, "Profile ID verification requires exactly 16 stored Profile ID bytes.");
        }

        bool containsId = HeaderCodec.ContainsNonZeroBytes(profile.Header.ProfileId.Bytes, 0, IccConstants.ProfileIdSize);
        if (containsId != profile.Header.ProfileId.IsPresent)
        {
            return ProfileIdCore.CreateResult(IccOperationStatus.InvalidData, "The stored Profile ID bytes and presence indicator are inconsistent.");
        }

        if (!containsId)
        {
            return ProfileIdCore.CreateResult(IccOperationStatus.NotFound, "The profile does not contain a Profile ID.");
        }

        IccProfileId computed = ComputeProfileIdData(profile, options);
        if (!ProfileIdCore.BytesEqual(profile.Header.ProfileId.Bytes, computed.Bytes))
        {
            return ProfileIdCore.CreateResult(IccOperationStatus.Conflict, "The stored Profile ID does not match the computed Profile ID.");
        }

        return ProfileIdCore.CreateResult(IccOperationStatus.Success, "The stored Profile ID matches the computed Profile ID.");
    }

    /// <summary>Updates a profile ID.</summary>
    /// <param name="profile">The profile model.</param>
    /// <param name="options">The writing options.</param>
    public void UpdateProfileId(IccProfile profile, IccWriteOptions options)
    {
        IccProfileId computed = ComputeProfileIdData(profile, options);
        int index = 0;
        while (index < IccConstants.ProfileIdSize)
        {
            profile.Header.ProfileId.Bytes[index] = computed.Bytes[index];
            index = index + 1;
        }

        profile.Header.ProfileId.IsPresent = true;
    }
}
