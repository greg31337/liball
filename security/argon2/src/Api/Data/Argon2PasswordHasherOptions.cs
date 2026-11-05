namespace argon2;

/// <summary>
/// Contains generation, execution, and verification settings for password hashing.
/// </summary>
public class Argon2PasswordHasherOptions
{
    /// <summary>
    /// The parameters used to generate new hashes.
    /// </summary>
    public Argon2Parameters Parameters = new Argon2Parameters();

    /// <summary>
    /// The random salt length in bytes used for new hashes.
    /// </summary>
    public int SaltLength = Argon2Profiles.SecondRecommendedSaltLength;

    /// <summary>
    /// The tag length in bytes used for new hashes.
    /// </summary>
    public int TagLength = Argon2Profiles.SecondRecommendedTagLength;

    /// <summary>
    /// The execution controls used by hashing and verification operations.
    /// </summary>
    public Argon2ExecutionOptions ExecutionOptions = new Argon2ExecutionOptions();

    /// <summary>
    /// The policy applied before verifying untrusted encoded hashes.
    /// </summary>
    public Argon2VerificationPolicy VerificationPolicy = new Argon2VerificationPolicy();
}
