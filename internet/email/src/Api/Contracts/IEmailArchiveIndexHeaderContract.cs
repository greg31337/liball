using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArchiveIndexHeader"/>.
/// </summary>
public interface IEmailArchiveIndexHeaderContract
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string GetName();
    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    string GetValue();
    /// <summary>
    /// Gets the occurrence index.
    /// </summary>
    /// <returns>The current occurrence index.</returns>
    int GetOccurrenceIndex();
}
