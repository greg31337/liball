using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a TNEF message.
/// </summary>
public class EmailTnefMessage : IEmailTnefMessageContract
{
    EmailTnefContainer container;
    List<EmailTnefProperty> properties;
    List<EmailTnefRecipient> recipients;
    List<EmailTnefBody> bodies;
    List<EmailTnefAttachment> attachments;
    string? subject;
    string? messageClass;
    /// <summary>
    /// Creates a new TNEF message.
    /// </summary>
    /// <param name = "container">The container used by the operation.</param>
    /// <param name = "properties">The properties used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "bodies">The bodies used by the operation.</param>
    /// <param name = "attachments">The attachments used by the operation.</param>
    /// <param name = "subject">The subject used by the operation.</param>
    /// <param name = "messageClass">The message class used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTnefMessage(EmailTnefContainer container, List<EmailTnefProperty> properties, List<EmailTnefRecipient> recipients, List<EmailTnefBody> bodies, List<EmailTnefAttachment> attachments, string? subject, string? messageClass)
    {
        if (container == null)
            throw new ArgumentNullException("container");
        if (properties == null)
            throw new ArgumentNullException("properties");
        if (recipients == null)
            throw new ArgumentNullException("recipients");
        if (bodies == null)
            throw new ArgumentNullException("bodies");
        if (attachments == null)
            throw new ArgumentNullException("attachments");
        this.container = container;
        this.properties = new List<EmailTnefProperty>(properties);
        this.recipients = new List<EmailTnefRecipient>(recipients);
        this.bodies = new List<EmailTnefBody>(bodies);
        this.attachments = new List<EmailTnefAttachment>(attachments);
        this.subject = subject;
        this.messageClass = messageClass;
    }

    /// <summary>
    /// Gets the container.
    /// </summary>
    /// <returns>The current container.</returns>
    public EmailTnefContainer GetContainer()
    {
        return container;
    }

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    public List<EmailTnefProperty> GetProperties()
    {
        return new List<EmailTnefProperty>(properties);
    }

    /// <summary>
    /// Gets the recipients.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current recipients.</returns>
    public List<EmailTnefRecipient> GetRecipients()
    {
        return new List<EmailTnefRecipient>(recipients);
    }

    /// <summary>
    /// Gets the bodies.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bodies.</returns>
    public List<EmailTnefBody> GetBodies()
    {
        return new List<EmailTnefBody>(bodies);
    }

    /// <summary>
    /// Gets the attachments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attachments.</returns>
    public List<EmailTnefAttachment> GetAttachments()
    {
        return new List<EmailTnefAttachment>(attachments);
    }

    /// <summary>
    /// Gets the recipient count.
    /// </summary>
    /// <returns>The current recipient count.</returns>
    public int GetRecipientCount()
    {
        return recipients.Count;
    }

    /// <summary>
    /// Gets the body count.
    /// </summary>
    /// <returns>The current body count.</returns>
    public int GetBodyCount()
    {
        return bodies.Count;
    }

    /// <summary>
    /// Gets the attachment count.
    /// </summary>
    /// <returns>The current attachment count.</returns>
    public int GetAttachmentCount()
    {
        return attachments.Count;
    }

    /// <summary>
    /// Determines whether the subject is present or available.
    /// </summary>
    /// <returns><c>true</c> when the subject is available; otherwise, <c>false</c>.</returns>
    public bool HasSubject()
    {
        return subject != null;
    }

    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <returns>The current subject.</returns>
    public string? GetSubject()
    {
        return subject;
    }

    /// <summary>
    /// Determines whether the message class is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message class is available; otherwise, <c>false</c>.</returns>
    public bool HasMessageClass()
    {
        return messageClass != null;
    }

    /// <summary>
    /// Gets the message class.
    /// </summary>
    /// <returns>The current message class.</returns>
    public string? GetMessageClass()
    {
        return messageClass;
    }

    /// <summary>
    /// Finds the matching properties.
    /// </summary>
    /// <param name = "propertyId">The property ID used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public List<EmailTnefProperty> FindProperties(int propertyId)
    {
        if (propertyId < 0 || propertyId > 65535)
            throw new ArgumentOutOfRangeException("propertyId");
        List<EmailTnefProperty> result = new List<EmailTnefProperty>();
        int index = 0;
        while (index < properties.Count)
        {
            if (properties[index].GetPropertyId() == propertyId)
                result.Add(properties[index]);
            index++;
        }

        return result;
    }
}
