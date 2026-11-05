using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefMessage"/>.
/// </summary>
public interface IEmailTnefMessageContract
{
    /// <summary>
    /// Gets the container.
    /// </summary>
    /// <returns>The current container.</returns>
    EmailTnefContainer GetContainer();
    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    List<EmailTnefProperty> GetProperties();
    /// <summary>
    /// Gets the recipients.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current recipients.</returns>
    List<EmailTnefRecipient> GetRecipients();
    /// <summary>
    /// Gets the bodies.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bodies.</returns>
    List<EmailTnefBody> GetBodies();
    /// <summary>
    /// Gets the attachments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attachments.</returns>
    List<EmailTnefAttachment> GetAttachments();
    /// <summary>
    /// Gets the recipient count.
    /// </summary>
    /// <returns>The current recipient count.</returns>
    int GetRecipientCount();
    /// <summary>
    /// Gets the body count.
    /// </summary>
    /// <returns>The current body count.</returns>
    int GetBodyCount();
    /// <summary>
    /// Gets the attachment count.
    /// </summary>
    /// <returns>The current attachment count.</returns>
    int GetAttachmentCount();
    /// <summary>
    /// Determines whether the subject is present or available.
    /// </summary>
    /// <returns><c>true</c> when the subject is available; otherwise, <c>false</c>.</returns>
    bool HasSubject();
    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <returns>The current subject.</returns>
    string? GetSubject();
    /// <summary>
    /// Determines whether the message class is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message class is available; otherwise, <c>false</c>.</returns>
    bool HasMessageClass();
    /// <summary>
    /// Gets the message class.
    /// </summary>
    /// <returns>The current message class.</returns>
    string? GetMessageClass();
    /// <summary>
    /// Finds the matching properties.
    /// </summary>
    /// <param name = "propertyId">The property ID used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    List<EmailTnefProperty> FindProperties(int propertyId);
}
