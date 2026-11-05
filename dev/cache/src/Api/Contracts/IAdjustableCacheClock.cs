namespace cache;

/// <summary>
/// Defines controls for switching and advancing a mutable cache clock.
/// </summary>
public interface IAdjustableCacheClock
{
    /// <summary>Switches future reads back to the system UTC clock.</summary>
    void UseSystemUtcTicks();

    /// <summary>Switches to manual time and sets its current tick value.</summary>
    /// <param name="utcTicks">The new manual tick value.</param>
    void SetUtcTicks(long utcTicks);

    /// <summary>Moves manual time by a number of ticks.</summary>
    /// <param name="ticks">The signed number of ticks to add.</param>
    void AdvanceTicks(long ticks);

    /// <summary>Moves manual time by a number of milliseconds.</summary>
    /// <param name="milliseconds">The signed number of milliseconds to add.</param>
    void AdvanceMilliseconds(long milliseconds);

    /// <summary>Moves manual time by a number of seconds.</summary>
    /// <param name="seconds">The signed number of seconds to add.</param>
    void AdvanceSeconds(long seconds);
}
