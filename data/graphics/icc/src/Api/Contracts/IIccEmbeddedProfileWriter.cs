namespace icc;

/// <summary>
/// Inserts, replaces, and removes embedded ICC profiles while preserving unrelated carrier data.
/// </summary>
public interface IIccEmbeddedProfileWriter
{
    /// <summary>
    /// Inserts a profile into carrier bytes.
    /// </summary>
    /// <param name="carrierBytes">The original carrier bytes.</param>
    /// <param name="profile">The profile to insert.</param>
    /// <param name="format">The carrier format.</param>
    /// <param name="options">The profile serialization options.</param>
    /// <returns>The updated carrier bytes.</returns>
    /// <exception cref="IccEmbeddingException">The carrier cannot accept the profile.</exception>
    byte[] InsertProfileBytes(System.ReadOnlySpan<byte> carrierBytes, IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options);

    /// <summary>
    /// Replaces one embedded profile in carrier bytes.
    /// </summary>
    /// <param name="carrierBytes">The original carrier bytes.</param>
    /// <param name="profileIndex">The zero-based embedded profile index.</param>
    /// <param name="profile">The replacement profile.</param>
    /// <param name="format">The carrier format.</param>
    /// <param name="options">The profile serialization options.</param>
    /// <returns>The updated carrier bytes.</returns>
    /// <exception cref="IccEmbeddingException">The carrier or requested profile index is invalid.</exception>
    byte[] ReplaceProfileBytes(System.ReadOnlySpan<byte> carrierBytes, int profileIndex, IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options);

    /// <summary>
    /// Removes one embedded profile from carrier bytes.
    /// </summary>
    /// <param name="carrierBytes">The original carrier bytes.</param>
    /// <param name="profileIndex">The zero-based embedded profile index.</param>
    /// <param name="format">The carrier format.</param>
    /// <returns>The updated carrier bytes.</returns>
    /// <exception cref="IccEmbeddingException">The carrier or requested profile index is invalid.</exception>
    byte[] RemoveProfileBytes(System.ReadOnlySpan<byte> carrierBytes, int profileIndex, IccEmbeddedFormat format);

    /// <summary>
    /// Inserts a profile while copying a source carrier stream to a destination stream.
    /// </summary>
    /// <param name="sourceStream">The readable source carrier stream.</param>
    /// <param name="destinationStream">The writable destination carrier stream.</param>
    /// <param name="profile">The profile to insert.</param>
    /// <param name="format">The carrier format.</param>
    /// <param name="options">The profile serialization options.</param>
    /// <exception cref="IccEmbeddingException">The carrier cannot accept the profile.</exception>
    void InsertProfileStream(System.IO.Stream sourceStream, System.IO.Stream destinationStream, IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options);

    /// <summary>
    /// Replaces one embedded profile while copying a source carrier stream to a destination stream.
    /// </summary>
    /// <param name="sourceStream">The readable source carrier stream.</param>
    /// <param name="destinationStream">The writable destination carrier stream.</param>
    /// <param name="profileIndex">The zero-based embedded profile index.</param>
    /// <param name="profile">The replacement profile.</param>
    /// <param name="format">The carrier format.</param>
    /// <param name="options">The profile serialization options.</param>
    /// <exception cref="IccEmbeddingException">The carrier or requested profile index is invalid.</exception>
    void ReplaceProfileStream(System.IO.Stream sourceStream, System.IO.Stream destinationStream, int profileIndex, IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options);

    /// <summary>
    /// Removes one embedded profile while copying a source carrier stream to a destination stream.
    /// </summary>
    /// <param name="sourceStream">The readable source carrier stream.</param>
    /// <param name="destinationStream">The writable destination carrier stream.</param>
    /// <param name="profileIndex">The zero-based embedded profile index.</param>
    /// <param name="format">The carrier format.</param>
    /// <exception cref="IccEmbeddingException">The carrier or requested profile index is invalid.</exception>
    void RemoveProfileStream(System.IO.Stream sourceStream, System.IO.Stream destinationStream, int profileIndex, IccEmbeddedFormat format);

    /// <summary>
    /// Checks whether the requested carrier operation is supported without throwing for expected capability failures.
    /// </summary>
    /// <param name="format">The carrier format.</param>
    /// <param name="operation">The requested embedding operation.</param>
    /// <returns>The capability outcome.</returns>
    IccOperationResult ProbeEmbeddingOperation(IccEmbeddedFormat format, IccEmbeddedOperation operation);
}
