using System;

namespace email;
/// <summary>
/// Represents a redaction change.
/// </summary>
public class EmailRedactionChange : IEmailRedactionChangeContract
{
    EmailRedactionKind kind;
    EmailPath entityPath;
    string fieldName;
    int occurrenceCount;
    /// <summary>
    /// Creates a new redaction change.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "entityPath">The file-system or message path to use.</param>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "occurrenceCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRedactionChange(EmailRedactionKind kind, EmailPath entityPath, string fieldName, int occurrenceCount)
    {
        if (kind < EmailRedactionKind.Address || kind > EmailRedactionKind.AttachmentContent)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (fieldName == null || fieldName.Length == 0)
        {
            throw new ArgumentException("A redacted field name is required.", "fieldName");
        }

        if (occurrenceCount < 1)
        {
            throw new ArgumentOutOfRangeException("occurrenceCount");
        }

        this.kind = kind;
        this.entityPath = entityPath;
        this.fieldName = fieldName;
        this.occurrenceCount = occurrenceCount;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailRedactionKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Determines whether the entity path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity path is available; otherwise, <c>false</c>.</returns>
    public bool HasEntityPath()
    {
        return entityPath != null;
    }

    /// <summary>
    /// Gets the entity path.
    /// </summary>
    /// <returns>The current entity path.</returns>
    public EmailPath GetEntityPath()
    {
        return entityPath;
    }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    public string GetFieldName()
    {
        return fieldName;
    }

    /// <summary>
    /// Gets the occurrence count.
    /// </summary>
    /// <returns>The current occurrence count.</returns>
    public int GetOccurrenceCount()
    {
        return occurrenceCount;
    }
}
