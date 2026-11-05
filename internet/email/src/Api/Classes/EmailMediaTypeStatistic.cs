using System;

namespace email;
/// <summary>
/// Represents a media type statistic.
/// </summary>
public class EmailMediaTypeStatistic : IEmailMediaTypeStatisticContract
{
    string topLevelType;
    string subtype;
    int entityCount;
    /// <summary>
    /// Creates a new media type statistic.
    /// </summary>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "entityCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMediaTypeStatistic(string topLevelType, string subtype, int entityCount)
    {
        if (topLevelType == null)
        {
            throw new ArgumentNullException("topLevelType");
        }

        if (subtype == null)
        {
            throw new ArgumentNullException("subtype");
        }

        if (topLevelType.Length == 0 || subtype.Length == 0)
        {
            throw new ArgumentException("Media type tokens cannot be empty.");
        }

        if (entityCount < 1)
        {
            throw new ArgumentOutOfRangeException("entityCount");
        }

        this.topLevelType = topLevelType.ToLowerInvariant();
        this.subtype = subtype.ToLowerInvariant();
        this.entityCount = entityCount;
    }

    /// <summary>
    /// Gets the top level type.
    /// </summary>
    /// <returns>The current top level type.</returns>
    public string GetTopLevelType()
    {
        return topLevelType;
    }

    /// <summary>
    /// Gets the subtype.
    /// </summary>
    /// <returns>The current subtype.</returns>
    public string GetSubtype()
    {
        return subtype;
    }

    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    public string GetMediaType()
    {
        return topLevelType + "/" + subtype;
    }

    /// <summary>
    /// Gets the entity count.
    /// </summary>
    /// <returns>The current entity count.</returns>
    public int GetEntityCount()
    {
        return entityCount;
    }
}
