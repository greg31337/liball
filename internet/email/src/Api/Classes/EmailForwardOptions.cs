using System;

namespace email;
/// <summary>
/// Controls how forward operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailForwardOptions : IEmailForwardOptionsContract
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderCount = 100;
    /// <summary>
    /// The default maximum header characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderCharacters = 100000;
    /// <summary>
    /// The default maximum body characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumBodyCharacters = 1000000;
    /// <summary>
    /// The default maximum attachment count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumAttachmentCount = 100;
    /// <summary>
    /// The default maximum attachment bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumAttachmentBytes = 104857600;
    string forwardText;
    string? forwardHtml;
    string attachedFilename;
    EmailForwardHeaderPolicy headerPolicy;
    EmailForwardBodyPolicy bodyPolicy;
    EmailForwardAttachmentPolicy attachmentPolicy;
    int maximumHeaderCount;
    int maximumHeaderCharacters;
    int maximumBodyCharacters;
    int maximumAttachmentCount;
    long maximumAttachmentBytes;
    EmailComposerOptions composerOptions;
    /// <summary>
    /// Creates a new forward options object.
    /// </summary>
    public EmailForwardOptions()
    {
        forwardText = "";
        forwardHtml = null;
        attachedFilename = "forwarded-message.eml";
        headerPolicy = EmailForwardHeaderPolicy.Safe;
        bodyPolicy = EmailForwardBodyPolicy.IncludeOriginal;
        attachmentPolicy = EmailForwardAttachmentPolicy.None;
        maximumHeaderCount = DefaultMaximumHeaderCount;
        maximumHeaderCharacters = DefaultMaximumHeaderCharacters;
        maximumBodyCharacters = DefaultMaximumBodyCharacters;
        maximumAttachmentCount = DefaultMaximumAttachmentCount;
        maximumAttachmentBytes = DefaultMaximumAttachmentBytes;
        composerOptions = new EmailComposerOptions();
    }

    /// <summary>
    /// Gets the forward text.
    /// </summary>
    /// <returns>The current forward text.</returns>
    public string GetForwardText()
    {
        return forwardText;
    }

    /// <summary>
    /// Sets the forward text used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetForwardText(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        forwardText = value;
    }

    /// <summary>
    /// Determines whether the forward HTML is present or available.
    /// </summary>
    /// <returns><c>true</c> when the forward HTML is available; otherwise, <c>false</c>.</returns>
    public bool HasForwardHtml()
    {
        return forwardHtml != null;
    }

    /// <summary>
    /// Gets the forward HTML.
    /// </summary>
    /// <returns>The current forward HTML.</returns>
    public string? GetForwardHtml()
    {
        return forwardHtml;
    }

    /// <summary>
    /// Sets the forward HTML used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetForwardHtml(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        forwardHtml = value;
    }

    /// <summary>
    /// Clears the stored forward HTML value or values.
    /// </summary>
    public void ClearForwardHtml()
    {
        forwardHtml = null;
    }

    /// <summary>
    /// Gets the attached filename.
    /// </summary>
    /// <returns>The current attached filename.</returns>
    public string GetAttachedFilename()
    {
        return attachedFilename;
    }

    /// <summary>
    /// Sets the attached filename used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetAttachedFilename(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("An attached-message filename cannot be empty.", "value");
        }

        attachedFilename = new EmailFilename().SanitizeWithFilename(value).GetFilename();
    }

    /// <summary>
    /// Gets the header policy.
    /// </summary>
    /// <returns>The current header policy.</returns>
    public EmailForwardHeaderPolicy GetHeaderPolicy()
    {
        return headerPolicy;
    }

    /// <summary>
    /// Sets the header policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetHeaderPolicy(EmailForwardHeaderPolicy value)
    {
        if (value < EmailForwardHeaderPolicy.Minimal || value > EmailForwardHeaderPolicy.Preserve)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        headerPolicy = value;
    }

    /// <summary>
    /// Gets the body policy.
    /// </summary>
    /// <returns>The current body policy.</returns>
    public EmailForwardBodyPolicy GetBodyPolicy()
    {
        return bodyPolicy;
    }

    /// <summary>
    /// Sets the body policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetBodyPolicy(EmailForwardBodyPolicy value)
    {
        if (value < EmailForwardBodyPolicy.IncludeOriginal || value > EmailForwardBodyPolicy.OmitOriginal)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        bodyPolicy = value;
    }

    /// <summary>
    /// Gets the attachment policy.
    /// </summary>
    /// <returns>The current attachment policy.</returns>
    public EmailForwardAttachmentPolicy GetAttachmentPolicy()
    {
        return attachmentPolicy;
    }

    /// <summary>
    /// Sets the attachment policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetAttachmentPolicy(EmailForwardAttachmentPolicy value)
    {
        if (value < EmailForwardAttachmentPolicy.None || value > EmailForwardAttachmentPolicy.AttachmentsAndInlineResources)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        attachmentPolicy = value;
    }

    /// <summary>
    /// Gets the maximum header count.
    /// </summary>
    /// <returns>The current maximum header count.</returns>
    public int GetMaximumHeaderCount()
    {
        return maximumHeaderCount;
    }

    /// <summary>
    /// Sets the maximum header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHeaderCount(int value)
    {
        maximumHeaderCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum header characters.
    /// </summary>
    /// <returns>The current maximum header characters.</returns>
    public int GetMaximumHeaderCharacters()
    {
        return maximumHeaderCharacters;
    }

    /// <summary>
    /// Sets the maximum header characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHeaderCharacters(int value)
    {
        maximumHeaderCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum body characters.
    /// </summary>
    /// <returns>The current maximum body characters.</returns>
    public int GetMaximumBodyCharacters()
    {
        return maximumBodyCharacters;
    }

    /// <summary>
    /// Sets the maximum body characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumBodyCharacters(int value)
    {
        maximumBodyCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailForwardOptions Clone()
    {
        EmailForwardOptions copy = new EmailForwardOptions();
        copy.SetForwardText(forwardText);
        if (forwardHtml == null)
        {
            copy.ClearForwardHtml();
        }
        else
        {
            copy.SetForwardHtml(forwardHtml);
        }

        copy.SetAttachedFilename(attachedFilename);
        copy.SetHeaderPolicy(headerPolicy);
        copy.SetBodyPolicy(bodyPolicy);
        copy.SetAttachmentPolicy(attachmentPolicy);
        copy.SetMaximumHeaderCount(maximumHeaderCount);
        copy.SetMaximumHeaderCharacters(maximumHeaderCharacters);
        copy.SetMaximumBodyCharacters(maximumBodyCharacters);
        copy.SetMaximumAttachmentCount(maximumAttachmentCount);
        copy.SetMaximumAttachmentBytes(maximumAttachmentBytes);
        copy.SetComposerOptions(composerOptions);
        return copy;
    }
}
