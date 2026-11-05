using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefConvertedAttachment"/>.
/// </summary>
public interface IEmailTnefConvertedAttachmentContract
{
    /// <summary>
    /// Gets the source.
    /// </summary>
    /// <returns>The current source.</returns>
    EmailTnefAttachment GetSource();
    /// <summary>
    /// Determines whether the entity is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity is available; otherwise, <c>false</c>.</returns>
    bool HasEntity();
    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    MimeEntity? GetEntity();
}
