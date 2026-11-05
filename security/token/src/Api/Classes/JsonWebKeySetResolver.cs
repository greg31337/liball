using System;
namespace token;

/// <summary>
/// Resolves token keys from a caller-supplied JSON Web Key Set.
/// </summary>
public class JsonWebKeySetResolver : ITokenKeyResolver
{
    internal JsonWebKeySet set;

    /// <summary>Initializes a resolver backed by the supplied key set.</summary>
    /// <param name="set">The key set used for resolution.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>set</c> is null.</exception>
    public JsonWebKeySetResolver(JsonWebKeySet set)
    {
        if (set == null)
        {
            throw new ArgumentNullException("set");
        }

        this.set = set;
    }

    /// <summary>Resolves a key for a token operation.</summary>
    /// <param name="context">The unverified token metadata and requested operation.</param>
    /// <returns>A result containing one candidate key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>context</c> is null.</exception>
    public TokenKeyResult Resolve(TokenKeyResolutionContext context)
    {
        return set.Resolve(context);
    }
}
