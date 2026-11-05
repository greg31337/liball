namespace argon2;

/// <summary>
/// Contains the outcome of an exception-less encoded-hash decode operation.
/// </summary>
public class Argon2DecodeResult
{
    /// <summary>
    /// The decode outcome.
    /// </summary>
    public Argon2DecodeStatus Status = Argon2DecodeStatus.InvalidFormat;

    /// <summary>
    /// The decoded hash, or <c>null</c> when decoding did not succeed.
    /// </summary>
    public Argon2EncodedHash? EncodedHash = null;
}
