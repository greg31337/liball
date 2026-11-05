using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailNormalizationOptions"/>.
/// </summary>
public interface IEmailNormalizationOptionsOperationsContract
{
    /// <summary>
    /// Creates an all configured for the requested behavior.
    /// </summary>
    /// <returns>The normalization options produced by the operation.</returns>
    EmailNormalizationOptions CreateAll();
}
