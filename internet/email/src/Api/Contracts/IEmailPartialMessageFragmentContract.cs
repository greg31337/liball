using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPartialMessageFragment"/>.
/// </summary>
public interface IEmailPartialMessageFragmentContract
{
    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    MimeEntity GetEntity();
    /// <summary>
    /// Gets the outer headers.
    /// </summary>
    /// <returns>The current outer headers.</returns>
    EmailHeaders GetOuterHeaders();
    /// <summary>
    /// Gets the envelope.
    /// </summary>
    /// <returns>The current envelope.</returns>
    EmailEnvelope GetEnvelope();
    /// <summary>
    /// Gets the metadata.
    /// </summary>
    /// <returns>The current metadata.</returns>
    EmailPartialMessageMetadata? GetMetadata();
    /// <summary>
    /// Gets the fragment ID.
    /// </summary>
    /// <returns>The current fragment ID.</returns>
    string? GetFragmentId();
    /// <summary>
    /// Gets the fragment number.
    /// </summary>
    /// <returns>The current fragment number.</returns>
    long GetFragmentNumber();
    /// <summary>
    /// Determines whether the total fragments is present or available.
    /// </summary>
    /// <returns><c>true</c> when the total fragments is available; otherwise, <c>false</c>.</returns>
    bool HasTotalFragments();
    /// <summary>
    /// Gets the total fragments.
    /// </summary>
    /// <returns>The current total fragments.</returns>
    long GetTotalFragments();
    /// <summary>
    /// Gets the insertion index.
    /// </summary>
    /// <returns>The current insertion index.</returns>
    int GetInsertionIndex();
}
