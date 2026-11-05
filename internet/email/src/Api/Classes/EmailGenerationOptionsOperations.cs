using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailGenerationOptions"/>.
/// </summary>
public class EmailGenerationOptionsOperations : IEmailGenerationOptionsOperationsContract
{
    /// <summary>
    /// Creates an all configured for the requested behavior.
    /// </summary>
    /// <returns>The generation options produced by the operation.</returns>
    public EmailGenerationOptions CreateAll()
    {
        return EmailGenerationOptions.CreateAllImplementation();
    }
}
