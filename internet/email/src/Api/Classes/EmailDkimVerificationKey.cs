using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a DKIM verification key.
/// </summary>
public class EmailDkimVerificationKey : IEmailDkimVerificationKeyContract
{
    string domain;
    string selector;
    EmailDkimKey key;
    /// <summary>
    /// Creates a new DKIM verification key.
    /// </summary>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <param name = "selector">The selector used by the operation.</param>
    /// <param name = "key">The key used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimVerificationKey(string domain, string selector, EmailDkimKey key)
    {
        if (key == null)
            throw new ArgumentNullException("key");
        this.domain = EmailDkimVerificationValidation.RequireDomain(domain, "domain");
        this.selector = EmailDkimVerificationValidation.RequireDomain(selector, "selector");
        this.key = key;
    }

    /// <summary>
    /// Gets the domain.
    /// </summary>
    /// <returns>The current domain.</returns>
    public string GetDomain()
    {
        return domain;
    }

    /// <summary>
    /// Gets the selector.
    /// </summary>
    /// <returns>The current selector.</returns>
    public string GetSelector()
    {
        return selector;
    }

    /// <summary>
    /// Gets the key.
    /// </summary>
    /// <returns>The current key.</returns>
    public EmailDkimKey GetKey()
    {
        return key;
    }
}
