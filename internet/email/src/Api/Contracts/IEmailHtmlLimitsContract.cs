namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlLimits"/>.
/// </summary>
public interface IEmailHtmlLimitsContract
{
    /// <summary>
    /// Gets the max input characters.
    /// </summary>
    /// <returns>The current max input characters.</returns>
    int GetMaxInputCharacters();
    /// <summary>
    /// Sets the max input characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxInputCharacters(int value);
    /// <summary>
    /// Gets the max output characters.
    /// </summary>
    /// <returns>The current max output characters.</returns>
    int GetMaxOutputCharacters();
    /// <summary>
    /// Sets the max output characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxOutputCharacters(int value);
    /// <summary>
    /// Gets the max token count.
    /// </summary>
    /// <returns>The current max token count.</returns>
    int GetMaxTokenCount();
    /// <summary>
    /// Sets the max token count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxTokenCount(int value);
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
    /// Gets the max attributes per tag.
    /// </summary>
    /// <returns>The current max attributes per tag.</returns>
    int GetMaxAttributesPerTag();
    /// <summary>
    /// Sets the max attributes per tag used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxAttributesPerTag(int value);
    /// <summary>
    /// Gets the max url characters.
    /// </summary>
    /// <returns>The current max url characters.</returns>
    int GetMaxUrlCharacters();
    /// <summary>
    /// Sets the max url characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxUrlCharacters(int value);
    /// <summary>
    /// Gets the max data URI count.
    /// </summary>
    /// <returns>The current max data URI count.</returns>
    int GetMaxDataUriCount();
    /// <summary>
    /// Sets the max data URI count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxDataUriCount(int value);
    /// <summary>
    /// Gets the max data URI bytes.
    /// </summary>
    /// <returns>The current max data URI bytes.</returns>
    long GetMaxDataUriBytes();
    /// <summary>
    /// Sets the max data URI bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaxDataUriBytes(long value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHtmlLimits Clone();
}
