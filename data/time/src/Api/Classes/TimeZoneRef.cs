using System;

namespace time;

/// <summary>Wraps a platform <see cref="TimeZoneInfo"/> for instant conversion and local-time mapping.</summary>
/// <remarks>Zone IDs and historical rules come from the operating system and .NET runtime; this library does not bundle a time-zone database.</remarks>
public class TimeZoneRef : ITimeZoneRef
{
    const long TransitionSearchWindowTicks = 319680000000000L;
    const int MaxTimeZoneInfoOffsetSeconds = 50400;

    TimeZoneInfo timeZoneInfo;

    /// <summary>Wraps an existing platform time zone.</summary><param name="timeZoneInfo">The built-in zone used for all calculations.</param>
    public TimeZoneRef(TimeZoneInfo timeZoneInfo)
    {
        TimeValidation.RequireNotNull(timeZoneInfo, "timeZoneInfo");

        this.timeZoneInfo = timeZoneInfo;
    }

    /// <summary>Gets the built-in UTC zone.</summary><returns>A zone whose offset is always zero.</returns>
    internal static TimeZoneRef Utc()
    {
        return new TimeZoneRef(TimeZoneInfo.Utc);
    }

    /// <summary>Gets the operating system's configured local zone.</summary><returns>A wrapper around <see cref="TimeZoneInfo.Local"/>.</returns>
    internal static TimeZoneRef Local()
    {
        return new TimeZoneRef(TimeZoneInfo.Local);
    }

    /// <summary>Creates a transition-free time zone with a stable offset-derived identity.</summary>
    /// <param name="offset">A whole-minute offset in the range -14:00 through +14:00.</param>
    /// <returns>A custom <see cref="TimeZoneInfo"/> wrapper whose ID is <c>UTC</c> followed by the canonical offset when nonzero.</returns>
    /// <exception cref="ArgumentException">The offset contains a nonzero seconds component.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset is outside the range supported by <see cref="TimeZoneInfo"/>.</exception>
    internal static TimeZoneRef FromFixedOffset(Offset offset)
    {
        TimeValidation.RequireNotNull(offset, "offset");

        int totalSeconds = offset.GetTotalSeconds();
        if (totalSeconds % TimeConstants.SecondsPerMinute != 0)
        {
            throw new ArgumentException("TimeZoneInfo fixed offsets must use whole-minute precision.", "offset");
        }

        if (totalSeconds < -MaxTimeZoneInfoOffsetSeconds || totalSeconds > MaxTimeZoneInfoOffsetSeconds)
        {
            throw new ArgumentOutOfRangeException("offset", "TimeZoneInfo fixed offsets must be in the range -14:00 through +14:00.");
        }

        string id = GetFixedOffsetId(offset);
        TimeZoneInfo fixedZone = TimeZoneInfo.CreateCustomTimeZone(id, offset.ToTimeSpan(), id, id);
        return new TimeZoneRef(fixedZone);
    }

    /// <summary>Looks up a platform time zone or recreates a canonical fixed-offset zone ID.</summary><param name="id">An IANA or Windows ID recognized on the current platform, or a generated <c>UTC+HH:MM</c> or <c>UTC-HH:MM</c> ID.</param><returns>The matching zone.</returns><exception cref="TimeZoneNotFoundException">The current system does not recognize the ID.</exception>
    internal static TimeZoneRef FromSystemId(string id)
    {
        TimeValidation.RequireNotNull(id, "id");

        if (id.StartsWith("UTC+", StringComparison.Ordinal) || id.StartsWith("UTC-", StringComparison.Ordinal))
        {
            return FromFixedOffset(Offset.Parse(id.Substring(3)));
        }

        return new TimeZoneRef(TimeZoneInfo.FindSystemTimeZoneById(id));
    }

    /// <summary>Enumerates zones provided by the current platform.</summary><returns>New wrappers in the order returned by .NET.</returns>
    internal static TimeZoneRef[] GetAvailableSystemZones()
    {
        System.Collections.ObjectModel.ReadOnlyCollection<TimeZoneInfo> zones = TimeZoneInfo.GetSystemTimeZones();
        TimeZoneRef[] result = new TimeZoneRef[zones.Count];
        int index = 0;

        foreach (TimeZoneInfo zone in zones)
        {
            result[index] = new TimeZoneRef(zone);
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets the platform zone identifier.</summary><returns>The underlying zone ID.</returns>
    public string GetId()
    {
        return timeZoneInfo.Id;
    }

    /// <summary>Gets the platform-provided display name.</summary><returns>A human-readable name whose text depends on the platform.</returns>
    public string GetDisplayName()
    {
        return timeZoneInfo.DisplayName;
    }

    /// <summary>Gets the underlying built-in zone object.</summary><returns>The wrapped <see cref="TimeZoneInfo"/>.</returns>
    public TimeZoneInfo GetTimeZoneInfo()
    {
        return timeZoneInfo;
    }

    /// <summary>Gets the UTC offset in effect at an instant.</summary><param name="instant">The timeline point to query.</param><returns>The zone's applicable offset.</returns>
    public Offset GetOffset(Instant instant)
    {
        TimeValidation.RequireNotNull(instant, "instant");

        return Offset.FromTimeSpan(timeZoneInfo.GetUtcOffset(instant.ToDateTimeOffset()));
    }

    /// <summary>Converts an instant to local fields and offset in this zone.</summary><param name="instant">The timeline point to convert.</param><returns>An offset date-time representing the same instant.</returns>
    public OffsetDateTime ConvertInstant(Instant instant)
    {
        TimeValidation.RequireNotNull(instant, "instant");

        return OffsetDateTime.FromInstant(instant, GetOffset(instant));
    }

    /// <summary>Reports whether a forward transition skipped a local wall-clock value.</summary><param name="localDateTime">The floating local value to test.</param><returns><c>true</c> when no instant maps to the value.</returns>
    public bool IsInvalidLocal(LocalDateTime localDateTime)
    {
        TimeValidation.RequireNotNull(localDateTime, "localDateTime");

        return timeZoneInfo.IsInvalidTime(localDateTime.ToDateTimeUnspecified());
    }

    /// <summary>Reports whether a backward transition repeated a local wall-clock value.</summary><param name="localDateTime">The floating local value to test.</param><returns><c>true</c> when two instants map to the value.</returns>
    public bool IsAmbiguousLocal(LocalDateTime localDateTime)
    {
        TimeValidation.RequireNotNull(localDateTime, "localDateTime");

        return timeZoneInfo.IsAmbiguousTime(localDateTime.ToDateTimeUnspecified());
    }

    /// <summary>Gets both offsets that apply to a repeated local time.</summary><param name="localDateTime">The floating local value to inspect.</param><returns>Two platform-provided offsets for an ambiguous value, or an empty array when it is not ambiguous.</returns>
    public Offset[] GetAmbiguousOffsets(LocalDateTime localDateTime)
    {
        TimeValidation.RequireNotNull(localDateTime, "localDateTime");

        if (!IsAmbiguousLocal(localDateTime))
        {
            return new Offset[0];
        }

        TimeSpan[] offsets = timeZoneInfo.GetAmbiguousTimeOffsets(localDateTime.ToDateTimeUnspecified());
        Offset[] result = new Offset[offsets.Length];
        int index = 0;
        while (index < offsets.Length)
        {
            result[index] = Offset.FromTimeSpan(offsets[index]);
            index = index + 1;
        }

        return result;
    }

    /// <summary>Maps a local wall-clock value to every valid offset interpretation.</summary><param name="localDateTime">The floating local value.</param><returns>No values for a gap, one for normal time, or two for an overlap ordered by instant from earlier to later.</returns>
    public OffsetDateTime[] MapLocal(LocalDateTime localDateTime)
    {
        TimeValidation.RequireNotNull(localDateTime, "localDateTime");

        if (IsInvalidLocal(localDateTime))
        {
            return new OffsetDateTime[0];
        }

        if (IsAmbiguousLocal(localDateTime))
        {
            Offset[] offsets = GetAmbiguousOffsets(localDateTime);
            OffsetDateTime[] ambiguousResults = new OffsetDateTime[offsets.Length];
            int index = 0;
            while (index < offsets.Length)
            {
                ambiguousResults[index] = OffsetDateTime.Create(localDateTime, offsets[index]);
                index = index + 1;
            }

            Array.Sort(ambiguousResults, (left, right) => left.GetInstant().CompareTo(right.GetInstant()));
            return ambiguousResults;
        }

        Offset offset = Offset.FromTimeSpan(timeZoneInfo.GetUtcOffset(localDateTime.ToDateTimeUnspecified()));
        return new OffsetDateTime[]
        {
            OffsetDateTime.Create(localDateTime, offset)
        };
    }

    /// <summary>Maps a local value using an explicit policy for gaps and overlaps.</summary><param name="localDateTime">The requested wall-clock value.</param><param name="resolver">The resolution policy.</param><returns>One offset date-time chosen or shifted according to the policy.</returns><exception cref="InvalidOperationException">The resolver is strict for the encountered gap or overlap.</exception>
    public OffsetDateTime ResolveLocal(LocalDateTime localDateTime, IZoneResolver resolver)
    {
        TimeValidation.RequireNotNull(localDateTime, "localDateTime");
        TimeValidation.RequireNotNull(resolver, "resolver");

        return resolver.ResolveLocal(this, localDateTime);
    }

    /// <summary>Searches forward for the next change in UTC offset.</summary><remarks>The bounded search covers at most 370 days and returns null when no change is found in that window.</remarks><param name="instant">The instant after which to search.</param><returns>The first tick using the new offset, or null.</returns>
    public Instant? GetNextTransition(Instant instant)
    {
        TimeValidation.RequireNotNull(instant, "instant");

        long startTicks = instant.GetUtcTicks();
        long limitTicks = TimeConstants.MaxUtcTicks;
        if (startTicks <= TimeConstants.MaxUtcTicks - TransitionSearchWindowTicks)
        {
            limitTicks = startTicks + TransitionSearchWindowTicks;
        }

        int startOffsetSeconds = GetOffsetAtUtcTicks(startTicks).GetTotalSeconds();
        long low = startTicks;
        long high = startTicks;
        while (high < limitTicks)
        {
            if (limitTicks - high < TimeConstants.TicksPerHour)
            {
                high = limitTicks;
            }
            else
            {
                high = high + TimeConstants.TicksPerHour;
            }

            if (GetOffsetAtUtcTicks(high).GetTotalSeconds() != startOffsetSeconds)
            {
                return Instant.FromUtcTicks(FindNextTransitionTicks(low, high, startOffsetSeconds));
            }

            low = high;
        }

        return null;
    }

    /// <summary>Searches backward for the previous change in UTC offset.</summary><remarks>The bounded search covers at most 370 days and returns null when no change is found in that window.</remarks><param name="instant">The instant before which to search.</param><returns>The first tick using the current offset after the prior transition, or null.</returns>
    public Instant? GetPreviousTransition(Instant instant)
    {
        TimeValidation.RequireNotNull(instant, "instant");

        long startTicks = instant.GetUtcTicks();
        long limitTicks = TimeConstants.MinUtcTicks;
        if (startTicks >= TimeConstants.MinUtcTicks + TransitionSearchWindowTicks)
        {
            limitTicks = startTicks - TransitionSearchWindowTicks;
        }

        int startOffsetSeconds = GetOffsetAtUtcTicks(startTicks).GetTotalSeconds();
        long high = startTicks;
        long low = startTicks;
        while (low > limitTicks)
        {
            if (low - limitTicks < TimeConstants.TicksPerHour)
            {
                low = limitTicks;
            }
            else
            {
                low = low - TimeConstants.TicksPerHour;
            }

            if (GetOffsetAtUtcTicks(low).GetTotalSeconds() != startOffsetSeconds)
            {
                return Instant.FromUtcTicks(FindPreviousTransitionTicks(low, high, startOffsetSeconds));
            }

            high = low;
        }

        return null;
    }

    internal ZoneMapping CreateMapping(LocalDateTime localDateTime)
    {
        OffsetDateTime[] mappings = MapLocal(localDateTime);
        if (mappings.Length == 1)
        {
            return ZoneMapping.CreateNormal(localDateTime, this, mappings[0].GetInstant());
        }

        if (mappings.Length == 2)
        {
            return ZoneMapping.CreateAmbiguous(localDateTime, this, mappings[0].GetInstant(), mappings[1].GetInstant());
        }

        return CreateInvalidMapping(localDateTime);
    }

    ZoneMapping CreateInvalidMapping(LocalDateTime localDateTime)
    {
        long invalidTicks = localDateTime.ToDateTimeUnspecified().Ticks;
        long validBeforeTicks = FindValidTicksBefore(invalidTicks);
        long validAfterTicks = FindValidTicksAfter(invalidTicks);
        LocalDateTime validBefore = LocalDateTime.FromDateTime(new DateTime(validBeforeTicks, DateTimeKind.Unspecified));
        LocalDateTime validAfter = LocalDateTime.FromDateTime(new DateTime(validAfterTicks, DateTimeKind.Unspecified));
        OffsetDateTime[] beforeMappings = MapLocal(validBefore);
        OffsetDateTime[] afterMappings = MapLocal(validAfter);
        long gapTicks = validAfterTicks - (validBeforeTicks + 1L);

        return ZoneMapping.CreateInvalid(
            localDateTime,
            this,
            beforeMappings[0].GetInstant(),
            afterMappings[0].GetInstant(),
            Duration.FromTicks(gapTicks));
    }

    long FindValidTicksBefore(long invalidTicks)
    {
        long low = invalidTicks;
        while (low > 0L && IsInvalidTick(low))
        {
            low = low - TimeConstants.TicksPerHour;
            if (low < 0L)
            {
                low = 0L;
            }
        }

        if (IsInvalidTick(low))
        {
            throw new InvalidOperationException("Could not find a valid local time before the invalid mapping.");
        }

        long high = invalidTicks;
        while (high - low > 1L)
        {
            long mid = low + ((high - low) / 2L);
            if (IsInvalidTick(mid))
            {
                high = mid;
            }
            else
            {
                low = mid;
            }
        }

        return low;
    }

    long FindValidTicksAfter(long invalidTicks)
    {
        long maxTicks = TimeConstants.MaxUtcTicks;
        long high = invalidTicks;
        while (high < maxTicks && IsInvalidTick(high))
        {
            high = high + TimeConstants.TicksPerHour;
            if (high > maxTicks)
            {
                high = maxTicks;
            }
        }

        if (IsInvalidTick(high))
        {
            throw new InvalidOperationException("Could not find a valid local time after the invalid mapping.");
        }

        long low = invalidTicks;
        while (high - low > 1L)
        {
            long mid = low + ((high - low) / 2L);
            if (IsInvalidTick(mid))
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return high;
    }

    bool IsInvalidTick(long ticks)
    {
        return timeZoneInfo.IsInvalidTime(new DateTime(ticks, DateTimeKind.Unspecified));
    }

    Offset GetOffsetAtUtcTicks(long utcTicks)
    {
        return GetOffset(Instant.FromUtcTicks(utcTicks));
    }

    long FindNextTransitionTicks(long low, long high, int oldOffsetSeconds)
    {
        while (high - low > 1L)
        {
            long mid = low + ((high - low) / 2L);
            if (GetOffsetAtUtcTicks(mid).GetTotalSeconds() == oldOffsetSeconds)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return high;
    }

    long FindPreviousTransitionTicks(long low, long high, int currentOffsetSeconds)
    {
        while (high - low > 1L)
        {
            long mid = low + ((high - low) / 2L);
            if (GetOffsetAtUtcTicks(mid).GetTotalSeconds() == currentOffsetSeconds)
            {
                high = mid;
            }
            else
            {
                low = mid;
            }
        }

        return high;
    }

    static string GetFixedOffsetId(Offset offset)
    {
        if (offset.GetTotalSeconds() == 0)
        {
            return "UTC";
        }

        return "UTC" + offset.ToIsoString();
    }
}
