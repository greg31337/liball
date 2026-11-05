namespace argon2;

/// <summary>
/// Contains the outcome of a password verification operation.
/// </summary>
public class Argon2VerificationResult
{
    /// <summary>
    /// The verification outcome.
    /// </summary>
    public Argon2VerificationStatus Status = Argon2VerificationStatus.InvalidHash;

    /// <summary>
    /// Indicates whether a matching hash should be replaced using the current configuration.
    /// </summary>
    public bool NeedsRehash = false;
}
