using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a TNEF converted attachment.
/// </summary>
public class EmailTnefConvertedAttachment : IEmailTnefConvertedAttachmentContract
{
    EmailTnefAttachment source;
    MimeEntity? entity;
    /// <summary>
    /// Creates a new TNEF converted attachment.
    /// </summary>
    /// <param name = "source">The source TNEF attachment to read or copy from.</param>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTnefConvertedAttachment(EmailTnefAttachment source, MimeEntity? entity)
    {
        if (source == null)
            throw new ArgumentNullException("source");
        this.source = source;
        this.entity = entity;
    }

    /// <summary>
    /// Gets the source.
    /// </summary>
    /// <returns>The current source.</returns>
    public EmailTnefAttachment GetSource()
    {
        return source;
    }

    /// <summary>
    /// Determines whether the entity is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity is available; otherwise, <c>false</c>.</returns>
    public bool HasEntity()
    {
        return entity != null;
    }

    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    public MimeEntity? GetEntity()
    {
        return entity;
    }
}
