namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDecodingLimits"/>.
/// </summary>
public interface IEmailDecodingLimitsContract
{
    /// <summary>
    /// Gets the max decoded bytes.
    /// </summary>
    /// <returns>The current max decoded bytes.</returns>
    long GetMaxDecodedBytes();
    /// <summary>
    /// Sets the max decoded bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxDecodedBytes(long value);
    /// <summary>
    /// Gets the max decompressed bytes.
    /// </summary>
    /// <returns>The current max decompressed bytes.</returns>
    long GetMaxDecompressedBytes();
    /// <summary>
    /// Sets the max decompressed bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxDecompressedBytes(long value);
    /// <summary>
    /// Gets the max expansion ratio.
    /// </summary>
    /// <returns>The current max expansion ratio.</returns>
    int GetMaxExpansionRatio();
    /// <summary>
    /// Sets the max expansion ratio used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxExpansionRatio(int value);
    /// <summary>
    /// Gets the max text characters.
    /// </summary>
    /// <returns>The current max text characters.</returns>
    int GetMaxTextCharacters();
    /// <summary>
    /// Sets the max text characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxTextCharacters(int value);
    /// <summary>
    /// Gets the max charset name length.
    /// </summary>
    /// <returns>The current max charset name length.</returns>
    int GetMaxCharsetNameLength();
    /// <summary>
    /// Sets the max charset name length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxCharsetNameLength(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailDecodingLimits Clone();
}
