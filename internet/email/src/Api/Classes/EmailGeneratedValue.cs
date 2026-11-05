using System;

namespace email;
/// <summary>
/// Represents a generated value.
/// </summary>
public class EmailGeneratedValue : IEmailGeneratedValueContract
{
    EmailGenerationKind kind;
    EmailPath entityPath;
    string fieldName;
    string value;
    /// <summary>
    /// Creates a new generated value.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "entityPath">The file-system or message path to use.</param>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailGeneratedValue(EmailGenerationKind kind, EmailPath entityPath, string fieldName, string value)
    {
        if (kind < EmailGenerationKind.Date || kind > EmailGenerationKind.Boundary)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (fieldName == null || fieldName.Length == 0)
        {
            throw new ArgumentException("A generated field name is required.", "fieldName");
        }

        if (value == null || value.Length == 0)
        {
            throw new ArgumentException("A generated value is required.", "value");
        }

        this.kind = kind;
        this.entityPath = entityPath;
        this.fieldName = fieldName;
        this.value = value;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailGenerationKind GetKind()
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
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    public string GetValue()
    {
        return value;
    }
}
