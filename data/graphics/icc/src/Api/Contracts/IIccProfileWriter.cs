namespace icc;

/// <summary>
/// Preflights and serializes ICC profiles to deterministic byte and stream outputs.
/// </summary>
public interface IIccProfileWriter
{
    /// <summary>
    /// Calculates the exact encoded profile size without writing output.
    /// </summary>
    /// <param name="profile">The profile to preflight.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The exact output size in bytes.</returns>
    /// <exception cref="IccProfileWriteException">The profile cannot be serialized.</exception>
    int MeasureProfileBytes(IccProfile profile, IccWriteOptions options);

    /// <summary>
    /// Serializes a profile to a newly allocated byte array.
    /// </summary>
    /// <param name="profile">The profile to serialize.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The complete encoded profile.</returns>
    /// <exception cref="IccProfileWriteException">The profile cannot be serialized.</exception>
    byte[] WriteProfileBytes(IccProfile profile, IccWriteOptions options);

    /// <summary>
    /// Serializes a profile into caller-provided storage.
    /// </summary>
    /// <param name="profile">The profile to serialize.</param>
    /// <param name="destination">The destination storage.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The write outcome, including insufficient-storage failures.</returns>
    IccOperationResult TryWriteProfileBuffer(IccProfile profile, System.Span<byte> destination, IccWriteOptions options);

    /// <summary>
    /// Serializes a profile at the current position of a writable stream.
    /// </summary>
    /// <param name="profile">The profile to serialize.</param>
    /// <param name="destinationStream">The writable destination stream.</param>
    /// <param name="options">The serialization options.</param>
    /// <exception cref="IccProfileWriteException">The profile cannot be serialized or written.</exception>
    void WriteProfileStream(IccProfile profile, System.IO.Stream destinationStream, IccWriteOptions options);
}
