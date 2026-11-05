using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Represents a S/MIME chain status.
/// </summary>
public class EmailSmimeChainStatus : IEmailSmimeChainStatusContract
{
    X509ChainStatusFlags status;
    string information;
    /// <summary>
    /// Creates a new S/MIME chain status.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "information">The information used by the operation.</param>
    public EmailSmimeChainStatus(X509ChainStatusFlags status, string information)
    {
        this.status = status;
        this.information = information == null ? "" : information.Trim();
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public X509ChainStatusFlags GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Gets the information.
    /// </summary>
    /// <returns>The current information.</returns>
    public string GetInformation()
    {
        return information;
    }
}
