namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailParsingLimits"/>.
/// </summary>
public interface IEmailParsingLimitsContract
{
    /// <summary>
    /// Gets the max message bytes.
    /// </summary>
    /// <returns>The current max message bytes.</returns>
    long GetMaxMessageBytes();
    /// <summary>
    /// Sets the max message bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxMessageBytes(long value);
    /// <summary>
    /// Gets the max header bytes.
    /// </summary>
    /// <returns>The current max header bytes.</returns>
    long GetMaxHeaderBytes();
    /// <summary>
    /// Sets the max header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxHeaderBytes(long value);
    /// <summary>
    /// Gets the max header field count.
    /// </summary>
    /// <returns>The current max header field count.</returns>
    int GetMaxHeaderFieldCount();
    /// <summary>
    /// Sets the max header field count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxHeaderFieldCount(int value);
    /// <summary>
    /// Gets the max line bytes.
    /// </summary>
    /// <returns>The current max line bytes.</returns>
    int GetMaxLineBytes();
    /// <summary>
    /// Sets the max line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxLineBytes(int value);
    /// <summary>
    /// Gets the max nesting depth.
    /// </summary>
    /// <returns>The current max nesting depth.</returns>
    int GetMaxNestingDepth();
    /// <summary>
    /// Sets the max nesting depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxNestingDepth(int value);
    /// <summary>
    /// Gets the max part count.
    /// </summary>
    /// <returns>The current max part count.</returns>
    int GetMaxPartCount();
    /// <summary>
    /// Sets the max part count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxPartCount(int value);
    /// <summary>
    /// Gets the max parameters per header.
    /// </summary>
    /// <returns>The current max parameters per header.</returns>
    int GetMaxParametersPerHeader();
    /// <summary>
    /// Sets the max parameters per header used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxParametersPerHeader(int value);
    /// <summary>
    /// Gets the max addresses per header.
    /// </summary>
    /// <returns>The current max addresses per header.</returns>
    int GetMaxAddressesPerHeader();
    /// <summary>
    /// Sets the max addresses per header used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxAddressesPerHeader(int value);
    /// <summary>
    /// Gets the max encoded words per header.
    /// </summary>
    /// <returns>The current max encoded words per header.</returns>
    int GetMaxEncodedWordsPerHeader();
    /// <summary>
    /// Sets the max encoded words per header used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxEncodedWordsPerHeader(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailParsingLimits Clone();
}
