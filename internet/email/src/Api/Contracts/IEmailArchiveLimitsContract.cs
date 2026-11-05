namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArchiveLimits"/>.
/// </summary>
public interface IEmailArchiveLimitsContract
{
    /// <summary>
    /// Gets the max archive bytes.
    /// </summary>
    /// <returns>The current max archive bytes.</returns>
    long GetMaxArchiveBytes();
    /// <summary>
    /// Sets the max archive bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxArchiveBytes(long value);
    /// <summary>
    /// Gets the max entry bytes.
    /// </summary>
    /// <returns>The current max entry bytes.</returns>
    long GetMaxEntryBytes();
    /// <summary>
    /// Sets the max entry bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxEntryBytes(long value);
    /// <summary>
    /// Gets the max entry count.
    /// </summary>
    /// <returns>The current max entry count.</returns>
    int GetMaxEntryCount();
    /// <summary>
    /// Sets the max entry count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxEntryCount(int value);
    /// <summary>
    /// Gets the max partial fragment count.
    /// </summary>
    /// <returns>The current max partial fragment count.</returns>
    int GetMaxPartialFragmentCount();
    /// <summary>
    /// Sets the max partial fragment count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxPartialFragmentCount(int value);
    /// <summary>
    /// Gets the max reassembled bytes.
    /// </summary>
    /// <returns>The current max reassembled bytes.</returns>
    long GetMaxReassembledBytes();
    /// <summary>
    /// Sets the max reassembled bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxReassembledBytes(long value);
    /// <summary>
    /// Gets the max TNEF attribute count.
    /// </summary>
    /// <returns>The current max TNEF attribute count.</returns>
    int GetMaxTnefAttributeCount();
    /// <summary>
    /// Sets the max TNEF attribute count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxTnefAttributeCount(int value);
    /// <summary>
    /// Gets the max path characters.
    /// </summary>
    /// <returns>The current max path characters.</returns>
    int GetMaxPathCharacters();
    /// <summary>
    /// Sets the max path characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxPathCharacters(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailArchiveLimits Clone();
}
