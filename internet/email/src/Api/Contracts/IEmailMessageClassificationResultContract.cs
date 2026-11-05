using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageClassificationResult"/>.
/// </summary>
public interface IEmailMessageClassificationResultContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailMessageClassificationKind GetKind();
    /// <summary>
    /// Gets the confidence.
    /// </summary>
    /// <returns>The current confidence.</returns>
    EmailMessageClassificationConfidence GetConfidence();
    /// <summary>
    /// Determines whether the current value or state is match.
    /// </summary>
    /// <returns><c>true</c> when the value or state is match; otherwise, <c>false</c>.</returns>
    bool IsMatch();
    /// <summary>
    /// Gets the reason count.
    /// </summary>
    /// <returns>The current reason count.</returns>
    int GetReasonCount();
    /// <summary>
    /// Gets the reason at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested reason.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailMessageClassificationReason GetReasonAt(int index);
    /// <summary>
    /// Gets the reasons.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current reasons.</returns>
    List<EmailMessageClassificationReason> GetReasons();
    /// <summary>
    /// Determines whether the collection contains reason.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsReason(EmailMessageClassificationReasonCode code);
}
