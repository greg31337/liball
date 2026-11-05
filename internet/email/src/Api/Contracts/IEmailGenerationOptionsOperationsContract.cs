using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailGenerationOptions"/>.
/// </summary>
public interface IEmailGenerationOptionsOperationsContract
{
    /// <summary>
    /// Creates an all configured for the requested behavior.
    /// </summary>
    /// <returns>The generation options produced by the operation.</returns>
    EmailGenerationOptions CreateAll();
}
