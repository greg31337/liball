using System;
namespace token;

/// <summary>
/// Adapts a key-resolution callback to <see cref="ITokenKeyResolver"/>.
/// </summary>
public class TokenKeyResolver : ITokenKeyResolver
{
    internal Func<TokenKeyResolutionContext, TokenKeyResult> resolveCallback;

    /// <summary>Initializes a resolver with a caller-provided callback.</summary>
    /// <param name="resolveCallback">The callback that resolves one candidate key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>resolveCallback</c> is null.</exception>
    public TokenKeyResolver(
        Func<TokenKeyResolutionContext, TokenKeyResult> resolveCallback)
    {
        if (resolveCallback == null)
        {
            throw new ArgumentNullException("resolveCallback");
        }

        this.resolveCallback = resolveCallback;
    }

    /// <summary>Creates a resolver without an application callback.</summary>
    /// <returns>A resolver that reports that it is not configured.</returns>
    internal static TokenKeyResolver CreateEmpty()
    {
        return new TokenKeyResolver(ResolveEmpty);
    }

    /// <summary>Resolves a key for a token operation.</summary>
    /// <param name="context">The unverified token metadata and requested operation.</param>
    /// <returns>A result containing one candidate key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>context</c> is null.</exception>
    public TokenKeyResult Resolve(TokenKeyResolutionContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return TokenKeyResolution.ValidateResult(resolveCallback(context));
    }

    internal static TokenKeyResult ResolveEmpty(TokenKeyResolutionContext context)
    {
        return Failure(
            "key_resolver_not_configured",
            "The token key resolver callback is not configured.");
    }

    internal static TokenKeyResult Failure(string code, string message)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
