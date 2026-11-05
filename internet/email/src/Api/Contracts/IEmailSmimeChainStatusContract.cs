using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeChainStatus"/>.
/// </summary>
public interface IEmailSmimeChainStatusContract
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    X509ChainStatusFlags GetStatus();
    /// <summary>
    /// Gets the information.
    /// </summary>
    /// <returns>The current information.</returns>
    string GetInformation();
}
