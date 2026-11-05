using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMboxWriter"/>.
/// </summary>
public interface IEmailMboxWriterContract
{
    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    EmailMboxWriterOptions GetOptions();
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The mbox write result produced by the operation.</returns>
    EmailMboxWriteResult Write(EmailMessage message);
    /// <summary>
    /// Appends the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The mbox write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailMboxWriteResult Append(EmailMessage message);
    /// <summary>
    /// Appends the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "envelopeSender">The envelope sender used by the operation.</param>
    /// <param name = "envelopeTimestamp">The envelope timestamp used by the operation.</param>
    /// <returns>The mbox write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailMboxWriteResult AppendWithEnvelope(EmailMessage message, string envelopeSender, DateTimeOffset envelopeTimestamp);
    /// <summary>
    /// Flushes the supplied value.
    /// </summary>
    void Flush();
    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    void Close();
    /// <summary>
    /// Gets the entry count.
    /// </summary>
    /// <returns>The current entry count.</returns>
    int GetEntryCount();
    /// <summary>
    /// Gets the written archive byte count.
    /// </summary>
    /// <returns>The current written archive byte count.</returns>
    long GetWrittenArchiveByteCount();
    /// <summary>
    /// Determines whether the current value or state is closed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closed; otherwise, <c>false</c>.</returns>
    bool IsClosed();
}
