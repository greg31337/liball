using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArchiveIndex"/>.
/// </summary>
public interface IEmailArchiveIndexContract
{
    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    EmailArchiveIndexRecord CreateWithMessage(EmailMessage message);
    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailArchiveIndexRecord CreateWithMessageAndOptions(EmailMessage message, EmailArchiveIndexOptions options);
    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "file">The file used by the operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    EmailArchiveIndexRecord CreateWithFile(EmailFileLoadResult file);
    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "file">The file used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailArchiveIndexRecord CreateWithFileAndOptions(EmailFileLoadResult file, EmailArchiveIndexOptions options);
    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    EmailArchiveIndexRecord CreateWithEntryUsingEmailMboxEntry(EmailMboxEntry entry);
    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    EmailArchiveIndexRecord CreateWithEntryAndOptionsUsingEmailMboxEntryAndEmailArchiveIndexOptions(EmailMboxEntry entry, EmailArchiveIndexOptions options);
    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <param name = "sourcePath">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailArchiveIndexRecord CreateWithEntryAndSourcePathAndOptions(EmailMboxEntry entry, string? sourcePath, EmailArchiveIndexOptions options);
    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    EmailArchiveIndexRecord CreateWithEntryUsingEmailMaildirEntry(EmailMaildirEntry entry);
    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailArchiveIndexRecord CreateWithEntryAndOptionsUsingEmailMaildirEntryAndEmailArchiveIndexOptions(EmailMaildirEntry entry, EmailArchiveIndexOptions options);
}
