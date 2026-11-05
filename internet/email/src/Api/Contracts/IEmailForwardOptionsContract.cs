using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailForwardOptions"/>.
/// </summary>
public interface IEmailForwardOptionsContract
{
    /// <summary>
    /// Gets the forward text.
    /// </summary>
    /// <returns>The current forward text.</returns>
    string GetForwardText();
    /// <summary>
    /// Sets the forward text used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetForwardText(string value);
    /// <summary>
    /// Determines whether the forward HTML is present or available.
    /// </summary>
    /// <returns><c>true</c> when the forward HTML is available; otherwise, <c>false</c>.</returns>
    bool HasForwardHtml();
    /// <summary>
    /// Gets the forward HTML.
    /// </summary>
    /// <returns>The current forward HTML.</returns>
    string? GetForwardHtml();
    /// <summary>
    /// Sets the forward HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetForwardHtml(string value);
    /// <summary>
    /// Clears the stored forward HTML value or values.
    /// </summary>
    void ClearForwardHtml();
    /// <summary>
    /// Gets the attached filename.
    /// </summary>
    /// <returns>The current attached filename.</returns>
    string GetAttachedFilename();
    /// <summary>
    /// Sets the attached filename used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetAttachedFilename(string value);
    /// <summary>
    /// Gets the header policy.
    /// </summary>
    /// <returns>The current header policy.</returns>
    EmailForwardHeaderPolicy GetHeaderPolicy();
    /// <summary>
    /// Sets the header policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetHeaderPolicy(EmailForwardHeaderPolicy value);
    /// <summary>
    /// Gets the body policy.
    /// </summary>
    /// <returns>The current body policy.</returns>
    EmailForwardBodyPolicy GetBodyPolicy();
    /// <summary>
    /// Sets the body policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetBodyPolicy(EmailForwardBodyPolicy value);
    /// <summary>
    /// Gets the attachment policy.
    /// </summary>
    /// <returns>The current attachment policy.</returns>
    EmailForwardAttachmentPolicy GetAttachmentPolicy();
    /// <summary>
    /// Sets the attachment policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetAttachmentPolicy(EmailForwardAttachmentPolicy value);
    /// <summary>
    /// Gets the maximum header count.
    /// </summary>
    /// <returns>The current maximum header count.</returns>
    int GetMaximumHeaderCount();
    /// <summary>
    /// Sets the maximum header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHeaderCount(int value);
    /// <summary>
    /// Gets the maximum header characters.
    /// </summary>
    /// <returns>The current maximum header characters.</returns>
    int GetMaximumHeaderCharacters();
    /// <summary>
    /// Sets the maximum header characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHeaderCharacters(int value);
    /// <summary>
    /// Gets the maximum body characters.
    /// </summary>
    /// <returns>The current maximum body characters.</returns>
    int GetMaximumBodyCharacters();
    /// <summary>
    /// Sets the maximum body characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumBodyCharacters(int value);
    /// <summary>
    /// Gets the maximum attachment count.
    /// </summary>
    /// <returns>The current maximum attachment count.</returns>
    int GetMaximumAttachmentCount();
    /// <summary>
    /// Sets the maximum attachment count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumAttachmentCount(int value);
    /// <summary>
    /// Gets the maximum attachment bytes.
    /// </summary>
    /// <returns>The current maximum attachment bytes.</returns>
    long GetMaximumAttachmentBytes();
    /// <summary>
    /// Sets the maximum attachment bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumAttachmentBytes(long value);
    /// <summary>
    /// Gets the composer options.
    /// </summary>
    /// <returns>The current composer options.</returns>
    EmailComposerOptions GetComposerOptions();
    /// <summary>
    /// Sets the composer options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetComposerOptions(EmailComposerOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailForwardOptions Clone();
}
