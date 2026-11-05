using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailNormalizationResult"/>.
/// </summary>
public interface IEmailNormalizationResultContract
{
    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    bool HasMessage();
    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    EmailMessage? GetMessage();
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
    /// <summary>
    /// Determines whether the changes is present or available.
    /// </summary>
    /// <returns><c>true</c> when the changes is available; otherwise, <c>false</c>.</returns>
    bool HasChanges();
    /// <summary>
    /// Gets the change count.
    /// </summary>
    /// <returns>The current change count.</returns>
    int GetChangeCount();
    /// <summary>
    /// Gets the change at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested change.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailNormalizationChange GetChangeAt(int index);
    /// <summary>
    /// Gets the changes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current changes.</returns>
    List<EmailNormalizationChange> GetChanges();
}
