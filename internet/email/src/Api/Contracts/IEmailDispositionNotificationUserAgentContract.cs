using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotificationUserAgent"/>.
/// </summary>
public interface IEmailDispositionNotificationUserAgentContract
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string GetName();
    /// <summary>
    /// Determines whether the product is present or available.
    /// </summary>
    /// <returns><c>true</c> when the product is available; otherwise, <c>false</c>.</returns>
    bool HasProduct();
    /// <summary>
    /// Gets the product.
    /// </summary>
    /// <returns>The current product.</returns>
    string? GetProduct();
}
