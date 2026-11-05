using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMboxReader"/>.
/// </summary>
public interface IEmailMboxReaderContract
{
    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    EmailMboxReaderOptions GetOptions();
    /// <summary>
    /// Reads the next from the input.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    bool ReadNext();
    /// <summary>
    /// Determines whether the current is present or available.
    /// </summary>
    /// <returns><c>true</c> when the current is available; otherwise, <c>false</c>.</returns>
    bool HasCurrent();
    /// <summary>
    /// Gets the current.
    /// </summary>
    /// <returns>The current current.</returns>
    EmailMboxEntry? GetCurrent();
    /// <summary>
    /// Gets the entry count.
    /// </summary>
    /// <returns>The current entry count.</returns>
    int GetEntryCount();
    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    long GetConsumedByteCount();
    /// <summary>
    /// Gets the read byte count.
    /// </summary>
    /// <returns>The current read byte count.</returns>
    long GetReadByteCount();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Determines whether the current value or state is closed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closed; otherwise, <c>false</c>.</returns>
    bool IsClosed();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    void Close();
}
