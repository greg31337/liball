using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimBodyCanonicalizationOutput"/>.
/// </summary>
public interface IEmailDkimBodyCanonicalizationOutputContract
{
    /// <summary>
    /// Gets the canonicalized body.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current canonicalized body.</returns>
    byte[] GetCanonicalizedBody();
    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The current result.</returns>
    EmailDkimBodyCanonicalizationResult GetResult();
}
