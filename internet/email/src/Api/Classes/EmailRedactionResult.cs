using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a redaction operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailRedactionResult : IEmailRedactionResultContract
{
    EmailMessage? message;
    MimeEntity? entity;
    List<EmailRedactionChange> changes;
    /// <summary>
    /// Creates a new redaction result.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "changes">The changes used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailRedactionResult(EmailMessage? message, MimeEntity? entity, List<EmailRedactionChange> changes)
    {
        if ((message == null) == (entity == null))
        {
            throw new ArgumentException("A redaction result requires exactly one redacted model.", "message");
        }

        if (changes == null)
        {
            throw new ArgumentNullException("changes");
        }

        this.message = message;
        this.entity = entity;
        this.changes = new List<EmailRedactionChange>(changes);
    }

    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    public bool HasMessage()
    {
        return message != null;
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    public EmailMessage? GetMessage()
    {
        return message;
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

    /// <summary>
    /// Determines whether the changes is present or available.
    /// </summary>
    /// <returns><c>true</c> when the changes is available; otherwise, <c>false</c>.</returns>
    public bool HasChanges()
    {
        return changes.Count > 0;
    }

    /// <summary>
    /// Gets the change count.
    /// </summary>
    /// <returns>The current change count.</returns>
    public int GetChangeCount()
    {
        return changes.Count;
    }

    /// <summary>
    /// Gets the change at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested change.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRedactionChange GetChangeAt(int index)
    {
        if (index < 0 || index >= changes.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return changes[index];
    }

    /// <summary>
    /// Gets the changes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current changes.</returns>
    public List<EmailRedactionChange> GetChanges()
    {
        return new List<EmailRedactionChange>(changes);
    }
}
