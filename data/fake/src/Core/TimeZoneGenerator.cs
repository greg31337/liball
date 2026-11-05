using System;

namespace fake;

internal static class TimeZoneGenerator
{
    internal static string[] Ids(TimeZoneRegistryEntry[] entries)
    {
        string[] result = new string[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            result[index] = entries[index].IanaId;
            index++;
        }
        return result;
    }

    internal static string Id(
        FakeRandom random,
        TimeZoneRegistryEntry[] entries)
    {
        return entries[random.IntFromMaximumExclusive(entries.Length)].IanaId;
    }

    internal static TimeZoneInfo Runtime(string ianaId)
    {
        TimeZoneRegistryStore.Current().Find(ianaId);
        return RuntimeTimeZoneResolver.Resolve(ianaId);
    }

    internal static TimeSpan Offset(
        string ianaId,
        DateTimeOffset instant)
    {
        return Runtime(ianaId).GetUtcOffset(instant);
    }

    internal static DateTimeOffset Convert(
        string ianaId,
        DateTimeOffset instant)
    {
        return TimeZoneInfo.ConvertTime(instant, Runtime(ianaId));
    }

    internal static TimeZoneDetails Details(
        TimeZoneRegistryEntry entry,
        DateTimeOffset instant)
    {
        TimeZoneInfo runtime = RuntimeTimeZoneResolver.Resolve(entry.IanaId);
        TimeZoneDetails result = new TimeZoneDetails();
        result.IanaId = entry.IanaId;
        result.RuntimeId = runtime.Id;
        result.CountryCodes = new string[entry.CountryCodes.Length];
        Array.Copy(entry.CountryCodes, result.CountryCodes, entry.CountryCodes.Length);
        result.Coordinates = entry.Coordinates;
        result.Comment = entry.Comment;
        result.Instant = instant;
        result.Offset = runtime.GetUtcOffset(instant);
        result.LocalTime = TimeZoneInfo.ConvertTime(instant, runtime);
        result.SupportsDaylightSavingTime = runtime.SupportsDaylightSavingTime;
        return result;
    }

    internal static DaylightSavingTransition Transition(
        string ianaId,
        int year,
        DaylightSavingTransitionKind kind)
    {
        if (year < 1 || year > 9998)
        {
            throw new ArgumentOutOfRangeException(
                "year",
                year,
                "year must be between 1 and 9998, inclusive.");
        }
        if (kind != DaylightSavingTransitionKind.Gap &&
            kind != DaylightSavingTransitionKind.Overlap)
        {
            throw new ArgumentOutOfRangeException(
                "kind",
                kind,
                "kind must be Gap or Overlap.");
        }
        TimeZoneInfo zone = Runtime(ianaId);
        DateTime cursor = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime end = new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        while (cursor < end && !Matches(zone, cursor, kind))
        {
            cursor = cursor.AddMinutes(1.0);
        }
        if (cursor >= end)
        {
            throw FakeException.Create(
                $"Time zone '{ianaId}' has no {KindName(kind)} in {year} according to the runtime database.");
        }
        DateTime localStart = cursor;
        while (cursor < end && Matches(zone, cursor, kind))
        {
            cursor = cursor.AddMinutes(1.0);
        }
        DateTime localEnd = cursor;
        if (kind == DaylightSavingTransitionKind.Gap)
        {
            return Gap(ianaId, year, zone, localStart, localEnd);
        }
        return Overlap(ianaId, year, zone, localStart, localEnd);
    }

    static bool Matches(
        TimeZoneInfo zone,
        DateTime local,
        DaylightSavingTransitionKind kind)
    {
        if (kind == DaylightSavingTransitionKind.Gap)
        {
            return zone.IsInvalidTime(local);
        }
        return zone.IsAmbiguousTime(local);
    }

    static DaylightSavingTransition Gap(
        string ianaId,
        int year,
        TimeZoneInfo zone,
        DateTime localStart,
        DateTime localEnd)
    {
        DateTime beforeLocal = localStart.AddTicks(-1L);
        TimeSpan beforeOffset = zone.GetUtcOffset(beforeLocal);
        TimeSpan afterOffset = zone.GetUtcOffset(localEnd);
        DaylightSavingTransition result = NewTransition(
            ianaId,
            year,
            DaylightSavingTransitionKind.Gap,
            localStart,
            localEnd);
        result.OffsetBefore = beforeOffset;
        result.OffsetAfter = afterOffset;
        result.Before = new DateTimeOffset(beforeLocal, beforeOffset);
        result.After = new DateTimeOffset(localEnd, afterOffset);
        result.EarlierOccurrence = result.Before;
        result.LaterOccurrence = result.After;
        return result;
    }

    static DaylightSavingTransition Overlap(
        string ianaId,
        int year,
        TimeZoneInfo zone,
        DateTime localStart,
        DateTime localEnd)
    {
        TimeSpan[] offsets = zone.GetAmbiguousTimeOffsets(localStart);
        TimeSpan beforeOffset = offsets[0] > offsets[1] ? offsets[0] : offsets[1];
        TimeSpan afterOffset = offsets[0] < offsets[1] ? offsets[0] : offsets[1];
        DateTime middle = localStart.AddTicks((localEnd.Ticks - localStart.Ticks) / 2L);
        DaylightSavingTransition result = NewTransition(
            ianaId,
            year,
            DaylightSavingTransitionKind.Overlap,
            localStart,
            localEnd);
        result.OffsetBefore = beforeOffset;
        result.OffsetAfter = afterOffset;
        result.Before = new DateTimeOffset(localEnd.AddTicks(-1L), beforeOffset);
        result.After = new DateTimeOffset(localStart, afterOffset);
        result.EarlierOccurrence = new DateTimeOffset(middle, beforeOffset);
        result.LaterOccurrence = new DateTimeOffset(middle, afterOffset);
        return result;
    }

    static DaylightSavingTransition NewTransition(
        string ianaId,
        int year,
        DaylightSavingTransitionKind kind,
        DateTime localStart,
        DateTime localEnd)
    {
        DaylightSavingTransition result = new DaylightSavingTransition();
        result.IanaId = ianaId;
        result.Year = year;
        result.Kind = kind;
        result.LocalStart = localStart;
        result.LocalEnd = localEnd;
        return result;
    }

    static string KindName(DaylightSavingTransitionKind kind)
    {
        if (kind == DaylightSavingTransitionKind.Gap)
        {
            return "daylight-saving gap";
        }
        return "daylight-saving overlap";
    }
}
