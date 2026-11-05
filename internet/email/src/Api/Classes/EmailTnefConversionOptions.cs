using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Controls how TNEF conversion operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailTnefConversionOptions : IEmailTnefConversionOptionsContract
{
    EmailTnefReaderOptions readerOptions;
    int maximumEntityCount;
    bool includeRichTextBody;
    /// <summary>
    /// Creates a new TNEF conversion options object.
    /// </summary>
    public EmailTnefConversionOptions()
    {
        readerOptions = new EmailTnefReaderOptions();
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
        includeRichTextBody = true;
    }

    /// <summary>
    /// Gets the reader options.
    /// </summary>
    /// <returns>The current reader options.</returns>
    public EmailTnefReaderOptions GetReaderOptions()
    {
        return readerOptions.Clone();
    }

    /// <summary>
    /// Sets the reader options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetReaderOptions(EmailTnefReaderOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        readerOptions = value.Clone();
    }

    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    public int GetMaximumEntityCount()
    {
        return maximumEntityCount;
    }

    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumEntityCount(int value)
    {
        maximumEntityCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the include rich text body.
    /// </summary>
    /// <returns><c>true</c> when the include rich text body setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeRichTextBody()
    {
        return includeRichTextBody;
    }

    /// <summary>
    /// Sets the include rich text body used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include rich text body behavior.</param>
    public void SetIncludeRichTextBody(bool value)
    {
        includeRichTextBody = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailTnefConversionOptions Clone()
    {
        EmailTnefConversionOptions copy = new EmailTnefConversionOptions();
        copy.SetReaderOptions(readerOptions);
        copy.SetMaximumEntityCount(maximumEntityCount);
        copy.SetIncludeRichTextBody(includeRichTextBody);
        return copy;
    }
}
