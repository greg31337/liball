using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirWriter"/>.
/// </summary>
public interface IEmailMaildirWriterContract
{
    /// <summary>
    /// Gets the root path.
    /// </summary>
    /// <returns>The current root path.</returns>
    string? GetRootPath();
    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    EmailMaildirWriterOptions GetOptions();
    /// <summary>
    /// Gets the delivery count.
    /// </summary>
    /// <returns>The current delivery count.</returns>
    int GetDeliveryCount();
    /// <summary>
    /// Delivers the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The maildir write result produced by the operation.</returns>
    EmailMaildirWriteResult Deliver(EmailMessage message);
    /// <summary>
    /// Delivers the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <param name = "flags">The flags used by the operation.</param>
    /// <returns>The maildir write result produced by the operation.</returns>
    EmailMaildirWriteResult DeliverToFolder(EmailMessage message, EmailMaildirFolder folder, EmailMaildirFlags flags);
    /// <summary>
    /// Delivers the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "uniqueName">The unique name used by the operation.</param>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <param name = "flags">The flags used by the operation.</param>
    /// <returns>The maildir write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailMaildirWriteResult DeliverWithUniqueName(EmailMessage message, string uniqueName, EmailMaildirFolder folder, EmailMaildirFlags flags);
    /// <summary>
    /// Atomically moves a Maildir entry while retaining applicable current-folder flags.
    /// </summary>
    /// <param name = "sourceFolder">The folder currently containing the entry.</param>
    /// <param name = "sourceFileName">The current filename.</param>
    /// <param name = "destinationFolder">The destination folder.</param>
    /// <returns>The Maildir operation result.</returns>
    EmailMaildirOperationResult Move(EmailMaildirFolder sourceFolder, string sourceFileName, EmailMaildirFolder destinationFolder);
    /// <summary>
    /// Atomically moves a Maildir entry and applies standard current-folder flags in canonical order.
    /// </summary>
    /// <param name = "sourceFolder">The folder currently containing the entry.</param>
    /// <param name = "sourceFileName">The current filename.</param>
    /// <param name = "destinationFolder">The destination folder.</param>
    /// <param name = "flags">The resulting standard flags.</param>
    /// <returns>The Maildir operation result.</returns>
    EmailMaildirOperationResult MoveWithFlags(EmailMaildirFolder sourceFolder, string sourceFileName, EmailMaildirFolder destinationFolder, EmailMaildirFlags flags);
    /// <summary>
    /// Atomically updates a current-folder entry's standard flags while retaining supported unknown flags.
    /// </summary>
    /// <param name = "sourceFileName">The current filename.</param>
    /// <param name = "flags">The resulting standard flags.</param>
    /// <returns>The Maildir operation result.</returns>
    EmailMaildirOperationResult UpdateFlags(string sourceFileName, EmailMaildirFlags flags);
}
