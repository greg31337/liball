using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRedactionChange"/>.
/// </summary>
public interface IEmailRedactionChangeContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailRedactionKind GetKind();
    /// <summary>
    /// Determines whether the entity path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity path is available; otherwise, <c>false</c>.</returns>
    bool HasEntityPath();
    /// <summary>
    /// Gets the entity path.
    /// </summary>
    /// <returns>The current entity path.</returns>
    EmailPath GetEntityPath();
    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    string GetFieldName();
    /// <summary>
    /// Gets the occurrence count.
    /// </summary>
    /// <returns>The current occurrence count.</returns>
    int GetOccurrenceCount();
}
