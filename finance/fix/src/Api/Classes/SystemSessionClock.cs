using System;

namespace fix;

/// <summary>Provides the operating system UTC clock to session logic.</summary>
public class SystemSessionClock : ISessionClock
{
    /// <summary>Creates a system session clock.</summary>
    public SystemSessionClock()
    {
    }

    /// <summary>Gets the current operating system UTC time.</summary>
    /// <returns>The current UTC time.</returns>
    public DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }
}
