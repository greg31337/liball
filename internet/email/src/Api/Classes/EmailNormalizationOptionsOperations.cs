using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailNormalizationOptions"/>.
/// </summary>
public class EmailNormalizationOptionsOperations : IEmailNormalizationOptionsOperationsContract
{
    /// <summary>
    /// Creates an all configured for the requested behavior.
    /// </summary>
    /// <returns>The normalization options produced by the operation.</returns>
    public EmailNormalizationOptions CreateAll()
    {
        return EmailNormalizationOptions.CreateAllImplementation();
    }
}
