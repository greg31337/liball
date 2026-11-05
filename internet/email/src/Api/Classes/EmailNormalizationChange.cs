using System;

namespace email;
/// <summary>
/// Represents a normalization change.
/// </summary>
public class EmailNormalizationChange : IEmailNormalizationChangeContract
{
    EmailNormalizationChangeKind kind;
    EmailPath entityPath;
    string fieldName;
    string? originalValue;
    string? normalizedValue;
    /// <summary>
    /// Creates a new normalization change.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "entityPath">The file-system or message path to use.</param>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "originalValue">The original value used by the operation.</param>
    /// <param name = "normalizedValue">The normalized value used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailNormalizationChange(EmailNormalizationChangeKind kind, EmailPath entityPath, string fieldName, string? originalValue, string? normalizedValue)
    {
        if (kind < EmailNormalizationChangeKind.LineEnding || kind > EmailNormalizationChangeKind.Filename)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (fieldName == null)
        {
            throw new ArgumentNullException("fieldName");
        }

        this.kind = kind;
        this.entityPath = entityPath;
        this.fieldName = fieldName;
        this.originalValue = originalValue;
        this.normalizedValue = normalizedValue;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailNormalizationChangeKind GetKind()
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
    /// Determines whether the original value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original value is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalValue()
    {
        return originalValue != null;
    }

    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    public string? GetOriginalValue()
    {
        return originalValue;
    }

    /// <summary>
    /// Determines whether the normalized value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the normalized value is available; otherwise, <c>false</c>.</returns>
    public bool HasNormalizedValue()
    {
        return normalizedValue != null;
    }

    /// <summary>
    /// Gets the normalized value.
    /// </summary>
    /// <returns>The current normalized value.</returns>
    public string? GetNormalizedValue()
    {
        return normalizedValue;
    }
}
