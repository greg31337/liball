using System;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Represents an openPGP algorithm support.
/// </summary>
public class EmailOpenPgpAlgorithmSupport : IEmailOpenPgpAlgorithmSupportContract
{
    EmailOpenPgpAlgorithmCategory category;
    int identifier;
    string name;
    bool supported;
    bool deprecated;
    string reason;
    /// <summary>
    /// Creates a new openPGP algorithm support.
    /// </summary>
    /// <param name = "category">The category used by the operation.</param>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "supported">Whether to enable the supported behavior.</param>
    /// <param name = "deprecated">Whether to enable the deprecated behavior.</param>
    /// <param name = "reason">The reason used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailOpenPgpAlgorithmSupport(EmailOpenPgpAlgorithmCategory category, int identifier, string name, bool supported, bool deprecated, string reason)
    {
        if (identifier < 0 || identifier > 255)
            throw new ArgumentOutOfRangeException("identifier");
        if (name == null || reason == null)
            throw new ArgumentNullException("name");
        this.category = category;
        this.identifier = identifier;
        this.name = name;
        this.supported = supported;
        this.deprecated = deprecated;
        this.reason = reason;
    }

    /// <summary>
    /// Gets the category.
    /// </summary>
    /// <returns>The current category.</returns>
    public EmailOpenPgpAlgorithmCategory GetCategory()
    {
        return category;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    /// <returns>The current identifier.</returns>
    public int GetIdentifier()
    {
        return identifier;
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
    /// Determines whether the current value or state is supported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is supported; otherwise, <c>false</c>.</returns>
    public bool IsSupported()
    {
        return supported;
    }

    /// <summary>
    /// Determines whether the current value or state is deprecated.
    /// </summary>
    /// <returns><c>true</c> when the value or state is deprecated; otherwise, <c>false</c>.</returns>
    public bool IsDeprecated()
    {
        return deprecated;
    }

    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    public string GetReason()
    {
        return reason;
    }
}
