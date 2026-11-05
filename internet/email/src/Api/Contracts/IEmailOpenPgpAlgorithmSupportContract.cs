using System;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpAlgorithmSupport"/>.
/// </summary>
public interface IEmailOpenPgpAlgorithmSupportContract
{
    /// <summary>
    /// Gets the category.
    /// </summary>
    /// <returns>The current category.</returns>
    EmailOpenPgpAlgorithmCategory GetCategory();
    /// <summary>
    /// Gets the identifier.
    /// </summary>
    /// <returns>The current identifier.</returns>
    int GetIdentifier();
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string GetName();
    /// <summary>
    /// Determines whether the current value or state is supported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is supported; otherwise, <c>false</c>.</returns>
    bool IsSupported();
    /// <summary>
    /// Determines whether the current value or state is deprecated.
    /// </summary>
    /// <returns><c>true</c> when the value or state is deprecated; otherwise, <c>false</c>.</returns>
    bool IsDeprecated();
    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    string GetReason();
}
