using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a disposition notification user agent.
/// </summary>
public class EmailDispositionNotificationUserAgent : IEmailDispositionNotificationUserAgentContract
{
    string name;
    string? product;
    bool hasProduct;
    /// <summary>
    /// Creates a new disposition notification user agent.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "product">The product used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDispositionNotificationUserAgent(string name, string? product)
    {
        if (name == null)
            throw new ArgumentNullException("name");
        if (name.Trim().Length == 0 || name.IndexOf(';') >= 0)
            throw new ArgumentException("A reporting user-agent name must be nonempty and cannot contain a semicolon.", "name");
        this.name = name.Trim();
        this.product = product;
        hasProduct = product != null;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>
    /// Determines whether the product is present or available.
    /// </summary>
    /// <returns><c>true</c> when the product is available; otherwise, <c>false</c>.</returns>
    public bool HasProduct()
    {
        return hasProduct;
    }

    /// <summary>
    /// Gets the product.
    /// </summary>
    /// <returns>The current product.</returns>
    public string? GetProduct()
    {
        return product;
    }
}
