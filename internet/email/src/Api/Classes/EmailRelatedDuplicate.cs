using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a related duplicate.
/// </summary>
public class EmailRelatedDuplicate : IEmailRelatedDuplicateContract
{
    EmailRelatedReferenceKind kind;
    string normalizedValue;
    List<MimeEntity> entities;
    /// <summary>
    /// Creates a new related duplicate.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "normalizedValue">The normalized value used by the operation.</param>
    /// <param name = "entities">The entities used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRelatedDuplicate(EmailRelatedReferenceKind kind, string normalizedValue, List<MimeEntity> entities)
    {
        if (kind != EmailRelatedReferenceKind.ContentId && kind != EmailRelatedReferenceKind.ContentLocation)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (normalizedValue == null || normalizedValue.Length == 0)
        {
            throw new ArgumentException("A duplicate resource key is required.", "normalizedValue");
        }

        if (entities == null)
        {
            throw new ArgumentNullException("entities");
        }

        if (entities.Count < 2)
        {
            throw new ArgumentException("A duplicate group requires at least two entities.", "entities");
        }

        ValidateEntities(entities);
        this.kind = kind;
        this.normalizedValue = normalizedValue;
        this.entities = new List<MimeEntity>(entities);
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailRelatedReferenceKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the normalized value.
    /// </summary>
    /// <returns>The current normalized value.</returns>
    public string GetNormalizedValue()
    {
        return normalizedValue;
    }

    /// <summary>
    /// Gets the entity count.
    /// </summary>
    /// <returns>The current entity count.</returns>
    public int GetEntityCount()
    {
        return entities.Count;
    }

    /// <summary>
    /// Gets the entity at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested entity.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public MimeEntity GetEntityAt(int index)
    {
        if (index < 0 || index >= entities.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return entities[index];
    }

    /// <summary>
    /// Gets the entities.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current entities.</returns>
    public List<MimeEntity> GetEntities()
    {
        return new List<MimeEntity>(entities);
    }

    static void ValidateEntities(List<MimeEntity> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Duplicate groups cannot contain null entities.", "entities");
            }

            index++;
        }
    }
}
