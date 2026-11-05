using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailWriteOptions"/>.
/// </summary>
public interface IEmailWriteOptionsOperationsContract
{
    /// <summary>
    /// Creates an exact preservation configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    EmailWriteOptions CreateExactPreservation();
    /// <summary>
    /// Creates a minimal regeneration configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    EmailWriteOptions CreateMinimalRegeneration();
    /// <summary>
    /// Creates a preserving configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    EmailWriteOptions CreatePreserving();
    /// <summary>
    /// Creates a canonical internet configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    EmailWriteOptions CreateCanonicalInternet();
    /// <summary>
    /// Creates a canonical smtp UTF-8 configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    EmailWriteOptions CreateCanonicalSmtpUtf8();
}
