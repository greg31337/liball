using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a message classification operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailMessageClassificationResult : IEmailMessageClassificationResultContract
{
    EmailMessageClassificationKind kind;
    EmailMessageClassificationConfidence confidence;
    List<EmailMessageClassificationReason> reasons;
    /// <summary>
    /// Creates a new message classification result.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "confidence">The confidence used by the operation.</param>
    /// <param name = "reasons">The reasons used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMessageClassificationResult(EmailMessageClassificationKind kind, EmailMessageClassificationConfidence confidence, List<EmailMessageClassificationReason> reasons)
    {
        if (kind < EmailMessageClassificationKind.Automated || kind > EmailMessageClassificationKind.Receipt)
            throw new ArgumentOutOfRangeException("kind");
        if (confidence < EmailMessageClassificationConfidence.None || confidence > EmailMessageClassificationConfidence.High)
            throw new ArgumentOutOfRangeException("confidence");
        if (reasons == null)
            throw new ArgumentNullException("reasons");
        int index = 0;
        while (index < reasons.Count)
        {
            if (reasons[index] == null)
                throw new ArgumentException("Classification reasons cannot contain null entries.", "reasons");
            if (reasons[index].GetClassificationKind() != kind)
                throw new ArgumentException("Every reason must describe the result classification kind.", "reasons");
            index++;
        }

        this.kind = kind;
        this.confidence = confidence;
        this.reasons = new List<EmailMessageClassificationReason>(reasons);
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailMessageClassificationKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the confidence.
    /// </summary>
    /// <returns>The current confidence.</returns>
    public EmailMessageClassificationConfidence GetConfidence()
    {
        return confidence;
    }

    /// <summary>
    /// Determines whether the current value or state is match.
    /// </summary>
    /// <returns><c>true</c> when the value or state is match; otherwise, <c>false</c>.</returns>
    public bool IsMatch()
    {
        return confidence != EmailMessageClassificationConfidence.None;
    }

    /// <summary>
    /// Gets the reason count.
    /// </summary>
    /// <returns>The current reason count.</returns>
    public int GetReasonCount()
    {
        return reasons.Count;
    }

    /// <summary>
    /// Gets the reason at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested reason.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMessageClassificationReason GetReasonAt(int index)
    {
        if (index < 0 || index >= reasons.Count)
            throw new ArgumentOutOfRangeException("index");
        return reasons[index];
    }

    /// <summary>
    /// Gets the reasons.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current reasons.</returns>
    public List<EmailMessageClassificationReason> GetReasons()
    {
        return new List<EmailMessageClassificationReason>(reasons);
    }

    /// <summary>
    /// Determines whether the collection contains reason.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsReason(EmailMessageClassificationReasonCode code)
    {
        int index = 0;
        while (index < reasons.Count)
        {
            if (reasons[index].GetCode() == code)
                return true;
            index++;
        }

        return false;
    }
}
