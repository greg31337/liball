using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAttachmentSource"/>.
/// </summary>
public interface IEmailAttachmentSourceOperationsContract
{
    /// <summary>
    /// Creates a memory configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    EmailAttachmentSource CreateMemoryWithValue(byte[] value);
    /// <summary>
    /// Creates a memory configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailAttachmentSource CreateMemoryWithValueAndFilename(byte[] value, string? filename);
    /// <summary>
    /// Creates a stream configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "ownership">The ownership used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    EmailAttachmentSource CreateStreamWithValueAndOwnership(Stream value, EmailContentOwnership ownership);
    /// <summary>
    /// Creates a stream configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "ownership">The ownership used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    EmailAttachmentSource CreateStreamWithValueAndOwnershipAndFilename(Stream value, EmailContentOwnership ownership, string? filename);
    /// <summary>
    /// Creates a file configured for the requested behavior.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    EmailAttachmentSource CreateFileWithPath(string path);
    /// <summary>
    /// Creates a file configured for the requested behavior.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    EmailAttachmentSource CreateFileWithPathAndFilename(string path, string filename);
    /// <summary>
    /// Creates a content configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    EmailAttachmentSource CreateContentWithValue(EmailContent value);
    /// <summary>
    /// Creates a content configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    EmailAttachmentSource CreateContentWithValueAndFilename(EmailContent value, string? filename);
}
