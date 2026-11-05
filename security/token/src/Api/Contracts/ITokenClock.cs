using System;
namespace token;

/// <summary>
/// Supplies the current time for token creation and validation.
/// </summary>
public interface ITokenClock
{
    /// <summary>Gets the current instant in UTC.</summary>
    /// <returns>The current UTC instant.</returns>
    DateTimeOffset GetUtcNow();
}
