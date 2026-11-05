using System;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>Resolves explicitly permitted remote JSON Web Key Sets with bounded caching.</summary>
public interface IRemoteJwksResolver
{
    /// <summary>Resolves a remote JSON Web Key Set.</summary>
    /// <param name="uri">The absolute HTTPS location of the key set.</param>
    /// <param name="policy">The opt-in remote access, host, limit, and cache policy.</param>
    /// <returns>A task containing the parsed key set or stable failure details.</returns>
    Task<JwksResult> Resolve(Uri uri, TokenPolicy policy);

    /// <summary>Resolves a remote JSON Web Key Set with caller cancellation.</summary>
    /// <param name="uri">The absolute HTTPS location of the key set.</param>
    /// <param name="policy">The opt-in remote access, host, limit, and cache policy.</param>
    /// <param name="cancellationToken">The token that cancels the request and response read.</param>
    /// <returns>A task containing the parsed key set or stable failure details.</returns>
    Task<JwksResult> ResolveWithCancellation(
        Uri uri,
        TokenPolicy policy,
        CancellationToken cancellationToken);

    /// <summary>Clears all cached response bytes.</summary>
    void ClearCache();
}
