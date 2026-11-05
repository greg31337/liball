using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageClassification"/>.
/// </summary>
public interface IEmailMessageClassificationContract
{
    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current result.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailMessageClassificationResult GetResult(EmailMessageClassificationKind kind);
    /// <summary>
    /// Gets the automated.
    /// </summary>
    /// <returns>The current automated.</returns>
    EmailMessageClassificationResult GetAutomated();
    /// <summary>
    /// Gets the mailing list.
    /// </summary>
    /// <returns>The current mailing list.</returns>
    EmailMessageClassificationResult GetMailingList();
    /// <summary>
    /// Gets the bounce.
    /// </summary>
    /// <returns>The current bounce.</returns>
    EmailMessageClassificationResult GetBounce();
    /// <summary>
    /// Gets the receipt.
    /// </summary>
    /// <returns>The current receipt.</returns>
    EmailMessageClassificationResult GetReceipt();
    /// <summary>
    /// Determines whether the current value or state is automated.
    /// </summary>
    /// <returns><c>true</c> when the value or state is automated; otherwise, <c>false</c>.</returns>
    bool IsAutomated();
    /// <summary>
    /// Determines whether the current value or state is mailing list.
    /// </summary>
    /// <returns><c>true</c> when the value or state is mailing list; otherwise, <c>false</c>.</returns>
    bool IsMailingList();
    /// <summary>
    /// Determines whether the current value or state is bounce.
    /// </summary>
    /// <returns><c>true</c> when the value or state is bounce; otherwise, <c>false</c>.</returns>
    bool IsBounce();
    /// <summary>
    /// Determines whether the current value or state is receipt.
    /// </summary>
    /// <returns><c>true</c> when the value or state is receipt; otherwise, <c>false</c>.</returns>
    bool IsReceipt();
    /// <summary>
    /// Gets the results.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current results.</returns>
    List<EmailMessageClassificationResult> GetResults();
}
