using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMediaTypeStatistic"/>.
/// </summary>
public interface IEmailMediaTypeStatisticContract
{
    /// <summary>
    /// Gets the top level type.
    /// </summary>
    /// <returns>The current top level type.</returns>
    string GetTopLevelType();
    /// <summary>
    /// Gets the subtype.
    /// </summary>
    /// <returns>The current subtype.</returns>
    string GetSubtype();
    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    string GetMediaType();
    /// <summary>
    /// Gets the entity count.
    /// </summary>
    /// <returns>The current entity count.</returns>
    int GetEntityCount();
}
