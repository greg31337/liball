using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAttachmentSource"/>.
/// </summary>
public interface IEmailAttachmentSourceContract
{
    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <returns>The current content.</returns>
    EmailContent GetContent();
    /// <summary>
    /// Determines whether the filename is present or available.
    /// </summary>
    /// <returns><c>true</c> when the filename is available; otherwise, <c>false</c>.</returns>
    bool HasFilename();
    /// <summary>
    /// Gets the filename.
    /// </summary>
    /// <returns>The current filename.</returns>
    string? GetFilename();
    /// <summary>
    /// Sets the filename used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetFilename(string value);
    /// <summary>
    /// Clears the stored filename value or values.
    /// </summary>
    void ClearFilename();
    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    EmailMediaType GetMediaType();
    /// <summary>
    /// Sets the media type used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMediaType(string value);
    /// <summary>
    /// Clears the stored media type value or values.
    /// </summary>
    void ClearMediaType();
    /// <summary>
    /// Determines whether the content ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content ID is available; otherwise, <c>false</c>.</returns>
    bool HasContentId();
    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    EmailMessageId? GetContentId();
    /// <summary>
    /// Sets the content ID used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetContentId(EmailMessageId? value);
    /// <summary>
    /// Clears the stored content ID value or values.
    /// </summary>
    void ClearContentId();
    /// <summary>
    /// Determines whether the content location is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content location is available; otherwise, <c>false</c>.</returns>
    bool HasContentLocation();
    /// <summary>
    /// Gets the content location.
    /// </summary>
    /// <returns>The current content location.</returns>
    string? GetContentLocation();
    /// <summary>
    /// Sets the content location used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    void SetContentLocation(string? value);
    /// <summary>
    /// Clears the stored content location value or values.
    /// </summary>
    void ClearContentLocation();
    /// <summary>
    /// Determines whether the description is present or available.
    /// </summary>
    /// <returns><c>true</c> when the description is available; otherwise, <c>false</c>.</returns>
    bool HasDescription();
    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The current description.</returns>
    string? GetDescription();
    /// <summary>
    /// Sets the description used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetDescription(string? value);
    /// <summary>
    /// Clears the stored description value or values.
    /// </summary>
    void ClearDescription();
    /// <summary>
    /// Creates an attachment entity configured for the requested behavior.
    /// </summary>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity CreateAttachmentEntity();
    /// <summary>
    /// Creates a resource entity configured for the requested behavior.
    /// </summary>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    MimeEntity CreateResourceEntity();
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailAttachmentSource Clone();
}
