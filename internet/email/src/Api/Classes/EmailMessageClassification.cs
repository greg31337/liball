using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a message classification.
/// </summary>
public class EmailMessageClassification : IEmailMessageClassificationContract
{
    List<EmailMessageClassificationResult> results;
    /// <summary>
    /// Creates a new message classification.
    /// </summary>
    /// <param name = "results">The results used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMessageClassification(List<EmailMessageClassificationResult> results)
    {
        if (results == null)
            throw new ArgumentNullException("results");
        if (results.Count != 4)
            throw new ArgumentException("A message classification requires one result for every classification kind.", "results");
        this.results = new List<EmailMessageClassificationResult>();
        int index = 0;
        while (index < 4)
        {
            EmailMessageClassificationResult result = results[index];
            if (result == null || (int)result.GetKind() != index)
                throw new ArgumentException("Classification results must be ordered by kind.", "results");
            this.results.Add(result);
            index++;
        }
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current result.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMessageClassificationResult GetResult(EmailMessageClassificationKind kind)
    {
        if (kind < EmailMessageClassificationKind.Automated || kind > EmailMessageClassificationKind.Receipt)
            throw new ArgumentOutOfRangeException("kind");
        return results[(int)kind];
    }

    /// <summary>
    /// Gets the automated.
    /// </summary>
    /// <returns>The current automated.</returns>
    public EmailMessageClassificationResult GetAutomated()
    {
        return results[(int)EmailMessageClassificationKind.Automated];
    }

    /// <summary>
    /// Gets the mailing list.
    /// </summary>
    /// <returns>The current mailing list.</returns>
    public EmailMessageClassificationResult GetMailingList()
    {
        return results[(int)EmailMessageClassificationKind.MailingList];
    }

    /// <summary>
    /// Gets the bounce.
    /// </summary>
    /// <returns>The current bounce.</returns>
    public EmailMessageClassificationResult GetBounce()
    {
        return results[(int)EmailMessageClassificationKind.Bounce];
    }

    /// <summary>
    /// Gets the receipt.
    /// </summary>
    /// <returns>The current receipt.</returns>
    public EmailMessageClassificationResult GetReceipt()
    {
        return results[(int)EmailMessageClassificationKind.Receipt];
    }

    /// <summary>
    /// Determines whether the current value or state is automated.
    /// </summary>
    /// <returns><c>true</c> when the value or state is automated; otherwise, <c>false</c>.</returns>
    public bool IsAutomated()
    {
        return GetAutomated().IsMatch();
    }

    /// <summary>
    /// Determines whether the current value or state is mailing list.
    /// </summary>
    /// <returns><c>true</c> when the value or state is mailing list; otherwise, <c>false</c>.</returns>
    public bool IsMailingList()
    {
        return GetMailingList().IsMatch();
    }

    /// <summary>
    /// Determines whether the current value or state is bounce.
    /// </summary>
    /// <returns><c>true</c> when the value or state is bounce; otherwise, <c>false</c>.</returns>
    public bool IsBounce()
    {
        return GetBounce().IsMatch();
    }

    /// <summary>
    /// Determines whether the current value or state is receipt.
    /// </summary>
    /// <returns><c>true</c> when the value or state is receipt; otherwise, <c>false</c>.</returns>
    public bool IsReceipt()
    {
        return GetReceipt().IsMatch();
    }

    /// <summary>
    /// Gets the results.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current results.</returns>
    public List<EmailMessageClassificationResult> GetResults()
    {
        return new List<EmailMessageClassificationResult>(results);
    }
}
