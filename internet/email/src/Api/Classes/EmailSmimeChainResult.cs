using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Describes the outcome of a S/MIME chain operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailSmimeChainResult : IEmailSmimeChainResultContract
{
    bool valid;
    List<EmailSmimeChainStatus> statuses;
    List<EmailSmimeCertificateInfo> elements;
    /// <summary>
    /// Creates a new S/MIME chain result.
    /// </summary>
    /// <param name = "valid">Whether to enable the valid behavior.</param>
    /// <param name = "statuses">The statuses used by the operation.</param>
    /// <param name = "elements">The elements used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeChainResult(bool valid, List<EmailSmimeChainStatus> statuses, List<EmailSmimeCertificateInfo> elements)
    {
        if (statuses == null || elements == null)
            throw new ArgumentNullException("statuses");
        this.valid = valid;
        this.statuses = new List<EmailSmimeChainStatus>(statuses);
        this.elements = new List<EmailSmimeCertificateInfo>(elements);
    }

    /// <summary>
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return valid;
    }

    /// <summary>
    /// Gets the status count.
    /// </summary>
    /// <returns>The current status count.</returns>
    public int GetStatusCount()
    {
        return statuses.Count;
    }

    /// <summary>
    /// Gets the status at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested status.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeChainStatus GetStatusAt(int index)
    {
        if (index < 0 || index >= statuses.Count)
            throw new ArgumentOutOfRangeException("index");
        return statuses[index];
    }

    /// <summary>
    /// Gets the element count.
    /// </summary>
    /// <returns>The current element count.</returns>
    public int GetElementCount()
    {
        return elements.Count;
    }

    /// <summary>
    /// Gets the element at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested element.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeCertificateInfo GetElementAt(int index)
    {
        if (index < 0 || index >= elements.Count)
            throw new ArgumentOutOfRangeException("index");
        return elements[index];
    }
}
