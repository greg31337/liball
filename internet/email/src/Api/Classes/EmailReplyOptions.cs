using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Controls how reply operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailReplyOptions : IEmailReplyOptionsContract
{
    /// <summary>
    /// The default maximum reference count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumReferenceCount = 100;
    /// <summary>
    /// The default maximum quoted characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumQuotedCharacters = 1000000;
    /// <summary>
    /// The default maximum attachment count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumAttachmentCount = 100;
    /// <summary>
    /// The default maximum attachment bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumAttachmentBytes = 104857600;
    string replyText;
    string? replyHtml;
    bool quoteOriginal;
    EmailReplyAttachmentPolicy attachmentPolicy;
    int maximumReferenceCount;
    int maximumQuotedCharacters;
    int maximumAttachmentCount;
    long maximumAttachmentBytes;
    EmailComposerOptions composerOptions;
    List<EmailAddress> identities;
    /// <summary>
    /// Creates a new reply options object.
    /// </summary>
    public EmailReplyOptions()
    {
        replyText = "";
        replyHtml = null;
        quoteOriginal = true;
        attachmentPolicy = EmailReplyAttachmentPolicy.None;
        maximumReferenceCount = DefaultMaximumReferenceCount;
        maximumQuotedCharacters = DefaultMaximumQuotedCharacters;
        maximumAttachmentCount = DefaultMaximumAttachmentCount;
        maximumAttachmentBytes = DefaultMaximumAttachmentBytes;
        composerOptions = new EmailComposerOptions();
        identities = new List<EmailAddress>();
    }

    /// <summary>
    /// Gets the reply text.
    /// </summary>
    /// <returns>The current reply text.</returns>
    public string GetReplyText()
    {
        return replyText;
    }

    /// <summary>
    /// Sets the reply text used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetReplyText(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        replyText = value;
    }

    /// <summary>
    /// Determines whether the reply HTML is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reply HTML is available; otherwise, <c>false</c>.</returns>
    public bool HasReplyHtml()
    {
        return replyHtml != null;
    }

    /// <summary>
    /// Gets the reply HTML.
    /// </summary>
    /// <returns>The current reply HTML.</returns>
    public string? GetReplyHtml()
    {
        return replyHtml;
    }

    /// <summary>
    /// Sets the reply HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetReplyHtml(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        replyHtml = value;
    }

    /// <summary>
    /// Clears the stored reply HTML value or values.
    /// </summary>
    public void ClearReplyHtml()
    {
        replyHtml = null;
    }

    /// <summary>
    /// Gets the quote original.
    /// </summary>
    /// <returns><c>true</c> when the quote original setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetQuoteOriginal()
    {
        return quoteOriginal;
    }

    /// <summary>
    /// Sets the quote original used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the quote original behavior.</param>
    public void SetQuoteOriginal(bool value)
    {
        quoteOriginal = value;
    }

    /// <summary>
    /// Gets the attachment policy.
    /// </summary>
    /// <returns>The current attachment policy.</returns>
    public EmailReplyAttachmentPolicy GetAttachmentPolicy()
    {
        return attachmentPolicy;
    }

    /// <summary>
    /// Sets the attachment policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetAttachmentPolicy(EmailReplyAttachmentPolicy value)
    {
        if (value < EmailReplyAttachmentPolicy.None || value > EmailReplyAttachmentPolicy.AttachmentsAndInlineResources)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        attachmentPolicy = value;
    }

    /// <summary>
    /// Gets the maximum reference count.
    /// </summary>
    /// <returns>The current maximum reference count.</returns>
    public int GetMaximumReferenceCount()
    {
        return maximumReferenceCount;
    }

    /// <summary>
    /// Sets the maximum reference count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumReferenceCount(int value)
    {
        maximumReferenceCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum quoted characters.
    /// </summary>
    /// <returns>The current maximum quoted characters.</returns>
    public int GetMaximumQuotedCharacters()
    {
        return maximumQuotedCharacters;
    }

    /// <summary>
    /// Sets the maximum quoted characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumQuotedCharacters(int value)
    {
        maximumQuotedCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum attachment count.
    /// </summary>
    /// <returns>The current maximum attachment count.</returns>
    public int GetMaximumAttachmentCount()
    {
        return maximumAttachmentCount;
    }

    /// <summary>
    /// Sets the maximum attachment count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumAttachmentCount(int value)
    {
        maximumAttachmentCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum attachment bytes.
    /// </summary>
    /// <returns>The current maximum attachment bytes.</returns>
    public long GetMaximumAttachmentBytes()
    {
        return maximumAttachmentBytes;
    }

    /// <summary>
    /// Sets the maximum attachment bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumAttachmentBytes(long value)
    {
        if (value < 1 || value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumAttachmentBytes = value;
    }

    /// <summary>
    /// Gets the composer options.
    /// </summary>
    /// <returns>The current composer options.</returns>
    public EmailComposerOptions GetComposerOptions()
    {
        return composerOptions.Clone();
    }

    /// <summary>
    /// Sets the composer options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetComposerOptions(EmailComposerOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        composerOptions = value.Clone();
    }

    /// <summary>
    /// Adds an identity to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void AddIdentity(EmailAddress value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        identities.Add(value);
    }

    /// <summary>
    /// Clears the stored identities value or values.
    /// </summary>
    public void ClearIdentities()
    {
        identities.Clear();
    }

    /// <summary>
    /// Gets the identity count.
    /// </summary>
    /// <returns>The current identity count.</returns>
    public int GetIdentityCount()
    {
        return identities.Count;
    }

    /// <summary>
    /// Gets the identity at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested identity.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAddress GetIdentityAt(int index)
    {
        if (index < 0 || index >= identities.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return identities[index];
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailReplyOptions Clone()
    {
        EmailReplyOptions copy = new EmailReplyOptions();
        copy.SetReplyText(replyText);
        if (replyHtml == null)
        {
            copy.ClearReplyHtml();
        }
        else
        {
            copy.SetReplyHtml(replyHtml);
        }

        copy.SetQuoteOriginal(quoteOriginal);
        copy.SetAttachmentPolicy(attachmentPolicy);
        copy.SetMaximumReferenceCount(maximumReferenceCount);
        copy.SetMaximumQuotedCharacters(maximumQuotedCharacters);
        copy.SetMaximumAttachmentCount(maximumAttachmentCount);
        copy.SetMaximumAttachmentBytes(maximumAttachmentBytes);
        copy.SetComposerOptions(composerOptions);
        int index = 0;
        while (index < identities.Count)
        {
            copy.AddIdentity(identities[index]);
            index++;
        }

        return copy;
    }
}
