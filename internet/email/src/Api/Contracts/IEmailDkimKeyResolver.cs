using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Resolves a caller-supplied DKIM or ARC verification key for a signing identity.
/// </summary>
/// <remarks>
/// Implementations may use an in-memory collection, a cache, or an external lookup. The email library does not perform DNS or other network access.
/// </remarks>
public interface IEmailDkimKeyResolver
{
    /// <summary>
    /// Resolves the verification key for a signing domain and selector.
    /// </summary>
    /// <param name = "domain">The validated signing domain from the signature.</param>
    /// <param name = "selector">The validated selector from the signature.</param>
    /// <returns>A matching verification-key binding, or null when no key is available.</returns>
    EmailDkimVerificationKey? Resolve(string domain, string selector);
}
