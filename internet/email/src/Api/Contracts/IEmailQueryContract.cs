using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailQuery"/>.
/// </summary>
public interface IEmailQueryContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailQueryKind GetKind();
    /// <summary>
    /// Determines whether the field name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the field name is available; otherwise, <c>false</c>.</returns>
    bool HasFieldName();
    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    string? GetFieldName();
    /// <summary>
    /// Determines whether the text is present or available.
    /// </summary>
    /// <returns><c>true</c> when the text is available; otherwise, <c>false</c>.</returns>
    bool HasText();
    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    string? GetText();
    /// <summary>
    /// Gets the child count.
    /// </summary>
    /// <returns>The current child count.</returns>
    int GetChildCount();
    /// <summary>
    /// Gets the child.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current child.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailQuery GetChild(int index);
    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current children.</returns>
    List<EmailQuery> GetChildren();
    /// <summary>
    /// Matches the es.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool Matches(EmailMessage message);
    /// <summary>
    /// Matches the es.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool MatchesWithOptions(EmailMessage message, EmailQueryOptions options);
}
