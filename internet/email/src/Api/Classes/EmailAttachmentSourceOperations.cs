using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailAttachmentSource"/>.
/// </summary>
public class EmailAttachmentSourceOperations : IEmailAttachmentSourceOperationsContract
{
    /// <summary>
    /// Creates a memory configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    public EmailAttachmentSource CreateMemoryWithValue(byte[] value)
    {
        return EmailAttachmentSource.CreateMemoryImplementationWithValue(value);
    }

    /// <summary>
    /// Creates a memory configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAttachmentSource CreateMemoryWithValueAndFilename(byte[] value, string? filename)
    {
        return EmailAttachmentSource.CreateMemoryImplementationWithValueAndFilename(value, filename);
    }

    /// <summary>
    /// Creates a stream configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "ownership">The ownership used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    public EmailAttachmentSource CreateStreamWithValueAndOwnership(Stream value, EmailContentOwnership ownership)
    {
        return EmailAttachmentSource.CreateStreamImplementationWithValueAndOwnership(value, ownership);
    }

    /// <summary>
    /// Creates a stream configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "ownership">The ownership used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    public EmailAttachmentSource CreateStreamWithValueAndOwnershipAndFilename(Stream value, EmailContentOwnership ownership, string? filename)
    {
        return EmailAttachmentSource.CreateStreamImplementationWithValueAndOwnershipAndFilename(value, ownership, filename);
    }

    /// <summary>
    /// Creates a file configured for the requested behavior.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    public EmailAttachmentSource CreateFileWithPath(string path)
    {
        return EmailAttachmentSource.CreateFileImplementationWithPath(path);
    }

    /// <summary>
    /// Creates a file configured for the requested behavior.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    public EmailAttachmentSource CreateFileWithPathAndFilename(string path, string filename)
    {
        return EmailAttachmentSource.CreateFileImplementationWithPathAndFilename(path, filename);
    }

    /// <summary>
    /// Creates a content configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    public EmailAttachmentSource CreateContentWithValue(EmailContent value)
    {
        return EmailAttachmentSource.CreateContentImplementationWithValue(value);
    }

    /// <summary>
    /// Creates a content configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The attachment source produced by the operation.</returns>
    public EmailAttachmentSource CreateContentWithValueAndFilename(EmailContent value, string? filename)
    {
        return EmailAttachmentSource.CreateContentImplementationWithValueAndFilename(value, filename);
    }
}
