using System;

namespace fix;

/// <summary>Provides deterministic UTC time to session logic.</summary>
public interface ISessionClock
{
    /// <summary>Gets the current UTC time.</summary>
    /// <returns>The current UTC time.</returns>
    DateTime GetUtcNow();
}
