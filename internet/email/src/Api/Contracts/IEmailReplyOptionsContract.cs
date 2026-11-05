using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReplyOptions"/>.
/// </summary>
public interface IEmailReplyOptionsContract
{
    /// <summary>
    /// Gets the reply text.
    /// </summary>
    /// <returns>The current reply text.</returns>
    string GetReplyText();
    /// <summary>
    /// Sets the reply text used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetReplyText(string value);
    /// <summary>
    /// Determines whether the reply HTML is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reply HTML is available; otherwise, <c>false</c>.</returns>
    bool HasReplyHtml();
    /// <summary>
    /// Gets the reply HTML.
    /// </summary>
    /// <returns>The current reply HTML.</returns>
    string? GetReplyHtml();
    /// <summary>
    /// Sets the reply HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetReplyHtml(string value);
    /// <summary>
    /// Clears the stored reply HTML value or values.
    /// </summary>
    void ClearReplyHtml();
    /// <summary>
    /// Gets the quote original.
    /// </summary>
    /// <returns><c>true</c> when the quote original setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetQuoteOriginal();
    /// <summary>
    /// Sets the quote original used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the quote original behavior.</param>
    void SetQuoteOriginal(bool value);
    /// <summary>
    /// Gets the attachment policy.
    /// </summary>
    /// <returns>The current attachment policy.</returns>
    EmailReplyAttachmentPolicy GetAttachmentPolicy();
    /// <summary>
    /// Sets the attachment policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetAttachmentPolicy(EmailReplyAttachmentPolicy value);
    /// <summary>
    /// Gets the maximum reference count.
    /// </summary>
    /// <returns>The current maximum reference count.</returns>
    int GetMaximumReferenceCount();
    /// <summary>
    /// Sets the maximum reference count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumReferenceCount(int value);
    /// <summary>
    /// Gets the maximum quoted characters.
    /// </summary>
    /// <returns>The current maximum quoted characters.</returns>
    int GetMaximumQuotedCharacters();
    /// <summary>
    /// Sets the maximum quoted characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumQuotedCharacters(int value);
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
    /// Adds an identity to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void AddIdentity(EmailAddress value);
    /// <summary>
    /// Clears the stored identities value or values.
    /// </summary>
    void ClearIdentities();
    /// <summary>
    /// Gets the identity count.
    /// </summary>
    /// <returns>The current identity count.</returns>
    int GetIdentityCount();
    /// <summary>
    /// Gets the identity at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested identity.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailAddress GetIdentityAt(int index);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailReplyOptions Clone();
}
