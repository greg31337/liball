using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBodySelection"/>.
/// </summary>
public interface IEmailBodySelectionContract
{
    /// <summary>
    /// Determines whether the body is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body is available; otherwise, <c>false</c>.</returns>
    bool HasBody();
    /// <summary>
    /// Gets the body.
    /// </summary>
    /// <returns>The current body.</returns>
    MimeEntity? GetBody();
    /// <summary>
    /// Gets the preference.
    /// </summary>
    /// <returns>The current preference.</returns>
    EmailBodyPreference GetPreference();
    /// <summary>
    /// Gets the quality.
    /// </summary>
    /// <returns>The current quality.</returns>
    EmailBodySelectionQuality GetQuality();
    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    EmailBodySelectionReason GetReason();
    /// <summary>
    /// Determines whether the path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the path is available; otherwise, <c>false</c>.</returns>
    bool HasPath();
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    EmailPath? GetPath();
    /// <summary>
    /// Gets the alternative count.
    /// </summary>
    /// <returns>The current alternative count.</returns>
    int GetAlternativeCount();
    /// <summary>
    /// Gets the alternative at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested alternative.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    MimeEntity GetAlternativeAt(int index);
    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current alternatives.</returns>
    List<MimeEntity> GetAlternatives();
    /// <summary>
    /// Gets the context count.
    /// </summary>
    /// <returns>The current context count.</returns>
    int GetContextCount();
    /// <summary>
    /// Gets the context at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested context.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailBodySelectionContext GetContextAt(int index);
    /// <summary>
    /// Gets the contexts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current contexts.</returns>
    List<EmailBodySelectionContext> GetContexts();
    /// <summary>
    /// Determines whether the context is present or available.
    /// </summary>
    /// <param name = "context">The context used by the operation.</param>
    /// <returns><c>true</c> when the context is available; otherwise, <c>false</c>.</returns>
    bool HasContext(EmailBodySelectionContext context);
}
