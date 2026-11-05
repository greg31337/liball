using System;

namespace cache;

/// <summary>
/// Supplies UTC tick values to a cache from either the system clock or a manually controlled timeline.
/// </summary>
/// <remarks>
/// Manual clocks are useful for deterministic expiration tests. A clock instance is mutable and may be shared with a cache so advancing it immediately affects subsequent cache operations.
/// </remarks>
public class CacheClock : ICacheClock, IAdjustableCacheClock
{
    internal bool UsesManualTicks;
    internal long ManualUtcTicks;

    /// <summary>Creates a clock that reads <see cref="DateTime.UtcNow"/>.</summary>
    public CacheClock()
    {
        UsesManualTicks = false;
        ManualUtcTicks = 0L;
    }

    /// <summary>Creates a clock that reads the current system UTC time.</summary>
    /// <returns>A new system-backed clock.</returns>
    internal static CacheClock SystemUtc()
    {
        return new CacheClock();
    }

    /// <summary>Creates a clock fixed initially at a caller-supplied UTC tick value.</summary>
    /// <param name="utcTicks">The initial tick value. The clock accepts the full <see cref="long"/> range.</param>
    /// <returns>A new manually controlled clock.</returns>
    internal static CacheClock Manual(long utcTicks)
    {
        CacheClock clock = new CacheClock();
        clock.UsesManualTicks = true;
        clock.ManualUtcTicks = utcTicks;
        return clock;
    }

    /// <summary>Reads the current tick value from the active system or manual timeline.</summary>
    /// <returns>The current UTC-compatible tick value used for cache timing.</returns>
    public long GetUtcTicks()
    {
        if (UsesManualTicks)
        {
            return ManualUtcTicks;
        }

        return DateTime.UtcNow.Ticks;
    }

    /// <summary>Switches future reads back to the system UTC clock.</summary>
    public void UseSystemUtcTicks()
    {
        UsesManualTicks = false;
    }

    /// <summary>Switches to manual time and sets its current tick value.</summary>
    /// <param name="utcTicks">The new manual tick value.</param>
    public void SetUtcTicks(long utcTicks)
    {
        UsesManualTicks = true;
        ManualUtcTicks = utcTicks;
    }

    /// <summary>Moves manual time by a number of ticks.</summary>
    /// <remarks>If the clock is system-backed, it first captures the current system UTC time and then applies the adjustment. A result outside the <see cref="long"/> range saturates at the nearest limit.</remarks>
    /// <param name="ticks">The signed number of ticks to add.</param>
    public void AdvanceTicks(long ticks)
    {
        AdvanceScaledTicks(ticks, 1L);
    }

    /// <summary>Moves manual time by a number of milliseconds.</summary>
    /// <remarks>A result outside the <see cref="long"/> range saturates at the nearest limit.</remarks>
    /// <param name="milliseconds">The signed number of milliseconds to add.</param>
    public void AdvanceMilliseconds(long milliseconds)
    {
        AdvanceScaledTicks(milliseconds, 10000L);
    }

    /// <summary>Moves manual time by a number of seconds.</summary>
    /// <remarks>A result outside the <see cref="long"/> range saturates at the nearest limit.</remarks>
    /// <param name="seconds">The signed number of seconds to add.</param>
    public void AdvanceSeconds(long seconds)
    {
        AdvanceScaledTicks(seconds, 10000000L);
    }

    internal void AdvanceScaledTicks(long value, long ticksPerUnit)
    {
        if (!UsesManualTicks)
        {
            ManualUtcTicks = DateTime.UtcNow.Ticks;
            UsesManualTicks = true;
        }

        ManualUtcTicks = CacheMath.AddScaled(ManualUtcTicks, value, ticksPerUnit);
    }
}
