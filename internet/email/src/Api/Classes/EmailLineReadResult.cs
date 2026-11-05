using System;

namespace email;
/// <summary>
/// Describes the outcome of a line read operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailLineReadResult : IEmailLineReadResultContract
{
    EmailLineReadStatus status;
    EmailLine? line;
    int consumedByteCount;
    EmailLineReadResult(EmailLineReadStatus status, EmailLine? line, int consumedByteCount)
    {
        this.status = status;
        this.line = line;
        this.consumedByteCount = consumedByteCount;
    }

    internal static EmailLineReadResult CreateNeedMoreDataImplementation(int consumedByteCount)
    {
        ValidateConsumedByteCount(consumedByteCount);
        return new EmailLineReadResult(EmailLineReadStatus.NeedMoreData, null, consumedByteCount);
    }

    internal static EmailLineReadResult CreateLineImplementation(EmailLine line, int consumedByteCount)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        ValidateConsumedByteCount(consumedByteCount);
        return new EmailLineReadResult(EmailLineReadStatus.Line, line, consumedByteCount);
    }

    internal static EmailLineReadResult CreateEndOfInputImplementation()
    {
        return new EmailLineReadResult(EmailLineReadStatus.EndOfInput, null, 0);
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailLineReadStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    public int GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    /// <summary>
    /// Determines whether the line is present or available.
    /// </summary>
    /// <returns><c>true</c> when the line is available; otherwise, <c>false</c>.</returns>
    public bool HasLine()
    {
        return line != null;
    }

    /// <summary>
    /// Gets the line.
    /// </summary>
    /// <returns>The current line.</returns>
    public EmailLine? GetLine()
    {
        return line;
    }

    static void ValidateConsumedByteCount(int consumedByteCount)
    {
        if (consumedByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("consumedByteCount");
        }
    }
}
