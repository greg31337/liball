namespace icc;

/// <summary>
/// Computes, verifies, and updates the ICC Profile ID.
/// </summary>
public interface IIccProfileIdCalculator
{
    /// <summary>
    /// Computes the Profile ID for encoded profile bytes using the mandated digest-field preparation.
    /// </summary>
    /// <param name="profileBytes">The complete encoded profile bytes.</param>
    /// <returns>The computed Profile ID.</returns>
    IccProfileId ComputeProfileIdBytes(System.ReadOnlySpan<byte> profileBytes);

    /// <summary>
    /// Computes the Profile ID for a profile model without mutating it.
    /// </summary>
    /// <param name="profile">The profile model.</param>
    /// <param name="options">The serialization options used to obtain canonical bytes.</param>
    /// <returns>The computed Profile ID.</returns>
    IccProfileId ComputeProfileIdData(IccProfile profile, IccWriteOptions options);

    /// <summary>
    /// Verifies the Profile ID stored in a profile model.
    /// </summary>
    /// <param name="profile">The profile model to verify.</param>
    /// <param name="options">The serialization options used to obtain verification bytes.</param>
    /// <returns>The verification outcome.</returns>
    IccOperationResult VerifyProfileId(IccProfile profile, IccWriteOptions options);

    /// <summary>
    /// Updates the Profile ID field of a profile model.
    /// </summary>
    /// <param name="profile">The profile model to update.</param>
    /// <param name="options">The serialization options used to obtain digest bytes.</param>
    void UpdateProfileId(IccProfile profile, IccWriteOptions options);
}
