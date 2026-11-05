using System;

namespace email;
/// <summary>
/// Represents a source info.
/// </summary>
public class EmailSourceInfo : IEmailSourceInfoContract
{
    EmailContent? content;
    EmailRange? messageRange;
    EmailRange? headerBlockRange;
    EmailRange? separatorRange;
    EmailRange? bodyRange;
    string? sourceName;
    bool complete;
    /// <summary>
    /// Creates source information for a complete message range.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "messageRange">The message range used by the operation.</param>
    public EmailSourceInfo(EmailContent content, EmailRange messageRange)
    {
        Initialize(content, messageRange, null, null, null, null, true);
    }

    internal static EmailSourceInfo CreateImplementationWithContentAndMessageRange(EmailContent content, EmailRange messageRange)
    {
        return new EmailSourceInfo(content, messageRange);
    }

    internal static EmailSourceInfo CreateImplementationWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(EmailContent? content, EmailRange? messageRange, EmailRange? headerBlockRange, EmailRange? separatorRange, EmailRange? bodyRange, string? sourceName, bool complete)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        if (messageRange == null)
        {
            throw new ArgumentNullException("messageRange");
        }

        EmailSourceInfo value = new EmailSourceInfo(content, messageRange);
        value.Initialize(content, messageRange, headerBlockRange, separatorRange, bodyRange, sourceName, complete);
        return value;
    }

    void Initialize(EmailContent? suppliedContent, EmailRange? suppliedMessageRange, EmailRange? suppliedHeaderBlockRange, EmailRange? suppliedSeparatorRange, EmailRange? suppliedBodyRange, string? suppliedSourceName, bool suppliedComplete)
    {
        if (suppliedContent == null)
        {
            throw new ArgumentNullException("suppliedContent");
        }

        if (suppliedMessageRange == null)
        {
            throw new ArgumentNullException("suppliedMessageRange");
        }

        if (suppliedSourceName != null && suppliedSourceName.Length == 0)
        {
            throw new ArgumentException("A source name cannot be empty when supplied.", "suppliedSourceName");
        }

        ValidateContained(suppliedMessageRange, suppliedHeaderBlockRange, "suppliedHeaderBlockRange");
        ValidateContained(suppliedMessageRange, suppliedSeparatorRange, "suppliedSeparatorRange");
        ValidateContained(suppliedMessageRange, suppliedBodyRange, "suppliedBodyRange");
        ValidateOrder(suppliedHeaderBlockRange, suppliedSeparatorRange, "suppliedSeparatorRange");
        ValidateOrder(suppliedSeparatorRange, suppliedBodyRange, "suppliedBodyRange");
        if (suppliedHeaderBlockRange != null && suppliedSeparatorRange == null)
        {
            ValidateOrder(suppliedHeaderBlockRange, suppliedBodyRange, "suppliedBodyRange");
        }

        if (suppliedContent.HasKnownLength() && suppliedMessageRange.GetEndOffsetExclusive() > suppliedContent.GetLength())
        {
            throw new ArgumentOutOfRangeException("suppliedMessageRange");
        }

        content = suppliedContent;
        messageRange = suppliedMessageRange;
        headerBlockRange = suppliedHeaderBlockRange;
        separatorRange = suppliedSeparatorRange;
        bodyRange = suppliedBodyRange;
        sourceName = suppliedSourceName;
        complete = suppliedComplete;
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <returns>The current content.</returns>
    public EmailContent? GetContent()
    {
        return content;
    }

    /// <summary>
    /// Gets the message range.
    /// </summary>
    /// <returns>The current message range.</returns>
    public EmailRange? GetMessageRange()
    {
        return messageRange;
    }

    /// <summary>
    /// Determines whether the header block range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the header block range is available; otherwise, <c>false</c>.</returns>
    public bool HasHeaderBlockRange()
    {
        return headerBlockRange != null;
    }

    /// <summary>
    /// Gets the header block range.
    /// </summary>
    /// <returns>The current header block range.</returns>
    public EmailRange? GetHeaderBlockRange()
    {
        return headerBlockRange;
    }

    /// <summary>
    /// Determines whether the separator range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the separator range is available; otherwise, <c>false</c>.</returns>
    public bool HasSeparatorRange()
    {
        return separatorRange != null;
    }

    /// <summary>
    /// Gets the separator range.
    /// </summary>
    /// <returns>The current separator range.</returns>
    public EmailRange? GetSeparatorRange()
    {
        return separatorRange;
    }

    /// <summary>
    /// Determines whether the body range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body range is available; otherwise, <c>false</c>.</returns>
    public bool HasBodyRange()
    {
        return bodyRange != null;
    }

    /// <summary>
    /// Gets the body range.
    /// </summary>
    /// <returns>The current body range.</returns>
    public EmailRange? GetBodyRange()
    {
        return bodyRange;
    }

    /// <summary>
    /// Determines whether the source name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source name is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceName()
    {
        return sourceName != null;
    }

    /// <summary>
    /// Gets the source name.
    /// </summary>
    /// <returns>The current source name.</returns>
    public string? GetSourceName()
    {
        return sourceName;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return complete;
    }

    static void ValidateContained(EmailRange container, EmailRange? value, string parameterName)
    {
        if (value != null && !container.ContainsRange(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateOrder(EmailRange? first, EmailRange? second, string parameterName)
    {
        if (first != null && second != null && first.GetEndOffsetExclusive() > second.GetStartOffset())
        {
            throw new ArgumentException("Source component ranges must be ordered and non-overlapping.", parameterName);
        }
    }
}
