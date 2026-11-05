using System;
namespace token;

/// <summary>
/// Supplies the current time from the system clock or a caller-provided callback.
/// </summary>
public class TokenClock : ITokenClock
{
    internal Func<DateTimeOffset> utcNow;

    /// <summary>
    /// Initializes a clock backed by a caller-provided callback.
    /// </summary>
    /// <param name="utcNow">The callback that supplies the current instant.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>utcNow</c> is null.</exception>
    public TokenClock(Func<DateTimeOffset> utcNow)
    {
        if (utcNow == null)
        {
            throw new ArgumentNullException("utcNow");
        }

        this.utcNow = utcNow;
    }

    /// <summary>
    /// Creates a clock backed by the system UTC clock.
    /// </summary>
    /// <returns>A clock that supplies <see cref="DateTimeOffset.UtcNow"/>.</returns>
    internal static TokenClock CreateSystem()
    {
        return new TokenClock(() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Gets the current instant normalized to a zero UTC offset.
    /// </summary>
    /// <returns>The current UTC instant.</returns>
    public DateTimeOffset GetUtcNow()
    {
        return utcNow().ToUniversalTime();
    }
}
