using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEditor"/>.
/// </summary>
public interface IEmailEditorContract
{
    /// <summary>
    /// Wraps the in multipart.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity WrapInMultipartWithMessageAndSubtype(EmailMessage message, string subtype);
    /// <summary>
    /// Wraps the in multipart.
    /// </summary>
    /// <param name = "parent">The parent used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity WrapInMultipartWithParentAndPartIndexAndSubtype(MimeEntity? parent, int partIndex, string subtype);
    /// <summary>
    /// Unwraps the multipart.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity UnwrapMultipartWithMessage(EmailMessage message);
    /// <summary>
    /// Unwraps the multipart.
    /// </summary>
    /// <param name = "parent">The parent used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity UnwrapMultipartWithParentAndPartIndex(MimeEntity parent, int partIndex);
    /// <summary>
    /// Adds a part to the current collection or model.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "part">The part used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity AddPart(MimeEntity multipart, MimeEntity part);
    /// <summary>
    /// Inserts a part at the requested position.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <param name = "part">The part used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity InsertPart(MimeEntity multipart, int partIndex, MimeEntity part);
    /// <summary>
    /// Removes the matching part at value or values.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity RemovePartAt(MimeEntity multipart, int partIndex);
    /// <summary>
    /// Moves the part.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "fromIndex">The zero-based from index.</param>
    /// <param name = "toIndex">The zero-based to index.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity MovePart(MimeEntity multipart, int fromIndex, int toIndex);
    /// <summary>
    /// Replaces the matching part at value or values.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity ReplacePartAt(MimeEntity multipart, int partIndex, MimeEntity replacement);
    /// <summary>
    /// Adds an alternative to the current collection or model.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "alternative">The alternative used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity AddAlternative(EmailMessage message, MimeEntity alternative);
    /// <summary>
    /// Sets the alternatives used by this instance.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "alternatives">The alternatives used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    MimeEntity SetAlternatives(EmailMessage message, List<MimeEntity> alternatives);
    /// <summary>
    /// Adds a related resource to the current collection or model.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "resource">The resource used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    MimeEntity AddRelatedResource(EmailMessage message, MimeEntity resource);
    /// <summary>
    /// Adds an attachment to the current collection or model.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity AddAttachment(EmailMessage message, MimeEntity attachment);
    /// <summary>
    /// Detaches the content.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    MimeEntity DetachContent(MimeEntity content);
    /// <summary>
    /// Embeds the message.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "embeddedMessage">The embedded message used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity EmbedMessage(EmailMessage message, EmailMessage embeddedMessage);
}
