using System;
namespace token;

/// <summary>
/// Resolves a candidate cryptographic key from unverified token metadata.
/// </summary>
public interface ITokenKeyResolver
{
    /// <summary>Resolves a key for a token operation.</summary>
    /// <param name="context">The unverified token metadata and requested operation.</param>
    /// <returns>A result containing one candidate key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>context</c> is null.</exception>
    TokenKeyResult Resolve(TokenKeyResolutionContext context);
}
