using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEntityFactory"/>.
/// </summary>
public interface IEmailEntityFactoryContract
{
    /// <summary>
    /// Creates a default leaf configured for the requested behavior.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity CreateDefaultLeaf(EmailContent content);
    /// <summary>
    /// Creates a text configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateTextWithValue(string value);
    /// <summary>
    /// Creates a text configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateTextWithValueAndSubtypeAndCharset(string? value, string subtype, string charset);
    /// <summary>
    /// Creates a content configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateContent(string value, string type, string subtype, string? charset);
    /// <summary>
    /// Creates a calendar configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "method">The method used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateCalendar(string value, string? method, string? charset);
    /// <summary>
    /// Creates a binary configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity CreateBinaryWithValue(byte[] value);
    /// <summary>
    /// Creates a binary configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateBinaryWithValueAndTypeAndSubtype(byte[] value, string type, string subtype);
    /// <summary>
    /// Creates a binary configured for the requested behavior.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateBinaryWithContentAndMediaType(EmailContent content, EmailMediaType mediaType);
    /// <summary>
    /// Creates a multipart configured for the requested behavior.
    /// </summary>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity CreateMultipartWithSubtype(string subtype);
    /// <summary>
    /// Creates a multipart configured for the requested behavior.
    /// </summary>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "children">The children used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateMultipartWithSubtypeAndChildren(string subtype, List<MimeEntity> children);
    /// <summary>
    /// Creates a nested message configured for the requested behavior.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateNestedMessage(EmailMessage message);
    /// <summary>
    /// Creates a digest child configured for the requested behavior.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateDigestChild(EmailMessage message);
}
