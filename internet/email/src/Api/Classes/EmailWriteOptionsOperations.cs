using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailWriteOptions"/>.
/// </summary>
public class EmailWriteOptionsOperations : IEmailWriteOptionsOperationsContract
{
    /// <summary>
    /// Creates an exact preservation configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    public EmailWriteOptions CreateExactPreservation()
    {
        return EmailWriteOptions.CreateExactPreservationImplementation();
    }

    /// <summary>
    /// Creates a minimal regeneration configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    public EmailWriteOptions CreateMinimalRegeneration()
    {
        return EmailWriteOptions.CreateMinimalRegenerationImplementation();
    }

    /// <summary>
    /// Creates a preserving configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    public EmailWriteOptions CreatePreserving()
    {
        return EmailWriteOptions.CreatePreservingImplementation();
    }

    /// <summary>
    /// Creates a canonical internet configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    public EmailWriteOptions CreateCanonicalInternet()
    {
        return EmailWriteOptions.CreateCanonicalInternetImplementation();
    }

    /// <summary>
    /// Creates a canonical smtp UTF-8 configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    public EmailWriteOptions CreateCanonicalSmtpUtf8()
    {
        return EmailWriteOptions.CreateCanonicalSmtpUtf8Implementation();
    }
}
