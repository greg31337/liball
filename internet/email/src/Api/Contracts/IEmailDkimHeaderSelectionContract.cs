using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimHeaderSelection"/>.
/// </summary>
public interface IEmailDkimHeaderSelectionContract
{
    /// <summary>
    /// Gets the requested name.
    /// </summary>
    /// <returns>The current requested name.</returns>
    string GetRequestedName();
    /// <summary>
    /// Determines whether the header is present or available.
    /// </summary>
    /// <returns><c>true</c> when the header is available; otherwise, <c>false</c>.</returns>
    bool HasHeader();
    /// <summary>
    /// Gets the source index.
    /// </summary>
    /// <returns>The current source index.</returns>
    int GetSourceIndex();
    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    EmailHeader? GetHeader();
}
