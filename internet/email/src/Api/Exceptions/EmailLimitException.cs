using System;

namespace email;
/// <summary>
/// Represents a limit exception.
/// </summary>
public class EmailLimitException : System.Exception
{
    string limitName;
    long limit;
    long observed;
    /// <summary>
    /// Creates a new limit exception.
    /// </summary>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "limit">The limit used by the operation.</param>
    /// <param name = "observed">The observed used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailLimitException(string limitName, long limit, long observed) : base(CreateMessage(limitName, limit, observed))
    {
        if (limit < 0)
        {
            throw new ArgumentOutOfRangeException("limit");
        }

        if (observed < 0)
        {
            throw new ArgumentOutOfRangeException("observed");
        }

        this.limitName = limitName;
        this.limit = limit;
        this.observed = observed;
    }

    /// <summary>
    /// Gets the limit name.
    /// </summary>
    /// <returns>The current limit name.</returns>
    public string GetLimitName()
    {
        return limitName;
    }

    /// <summary>
    /// Gets the limit.
    /// </summary>
    /// <returns>The current limit.</returns>
    public long GetLimit()
    {
        return limit;
    }

    /// <summary>
    /// Gets the observed.
    /// </summary>
    /// <returns>The current observed.</returns>
    public long GetObserved()
    {
        return observed;
    }

    static string CreateMessage(string limitName, long limit, long observed)
    {
        if (limitName == null)
        {
            throw new ArgumentNullException("limitName");
        }

        if (limitName.Length == 0)
        {
            throw new ArgumentException("Limit names cannot be empty.", "limitName");
        }

        return limitName + " limit " + limit + " was exceeded by observed value " + observed + ".";
    }
}
