using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRelatedDuplicate"/>.
/// </summary>
public interface IEmailRelatedDuplicateContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailRelatedReferenceKind GetKind();
    /// <summary>
    /// Gets the normalized value.
    /// </summary>
    /// <returns>The current normalized value.</returns>
    string GetNormalizedValue();
    /// <summary>
    /// Gets the entity count.
    /// </summary>
    /// <returns>The current entity count.</returns>
    int GetEntityCount();
    /// <summary>
    /// Gets the entity at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested entity.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    MimeEntity GetEntityAt(int index);
    /// <summary>
    /// Gets the entities.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current entities.</returns>
    List<MimeEntity> GetEntities();
}
