using System;
using System.Collections.ObjectModel;

namespace fix;

internal class SessionScheduler
{
    internal SessionSchedule Schedule;
    internal ISessionClock Clock;
    internal TimeZoneInfo TimeZone;
    internal bool Initialized;
    internal bool WasActive;
    internal DateTime LastUtcNow;

    internal SessionScheduler(SessionSchedule schedule, ISessionClock clock)
    {
        if (schedule == null || schedule.Windows == null || clock == null)
        {
            throw new FixSessionException("Session schedule settings are missing.");
        }
        if (schedule.TimeZoneId == null || schedule.TimeZoneId.Length == 0)
        {
            throw new FixSessionException("The session schedule timezone is missing.");
        }
        ValidateWindows(schedule);
        Schedule = schedule;
        Clock = clock;
        TimeZone = ResolveTimeZone(schedule.TimeZoneId);
    }

    internal SessionScheduleEvaluation EvaluateSchedule()
    {
        DateTime now = Clock.GetUtcNow();
        if (now.Kind != DateTimeKind.Utc)
        {
            throw new FixSessionException("The session schedule clock returned a non-UTC value.");
        }
        DateTime local = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZone);
        bool active = IsActiveLocalTime(local);
        SessionScheduleEvaluation result = new SessionScheduleEvaluation();
        result.Active = active;
        result.UtcNow = now;
        result.LocalNow = local;
        if (Initialized)
        {
            result.BoundaryStarted = !WasActive && active;
            result.BoundaryEnded = WasActive && !active;
        }
        Initialized = true;
        WasActive = active;
        LastUtcNow = now;
        return result;
    }

    internal bool IsActiveLocalTime(DateTime local)
    {
        if (Schedule.NonStop)
        {
            return true;
        }
        long position = (long)local.DayOfWeek * TimeSpan.TicksPerDay + local.TimeOfDay.Ticks;
        long weekTicks = 7L * TimeSpan.TicksPerDay;
        for (int index = 0; index < Schedule.Windows.Count; index++)
        {
            SessionTimeWindow window = Schedule.Windows[index];
            if (!window.Enabled)
            {
                continue;
            }
            long start = (long)window.StartDay * TimeSpan.TicksPerDay + window.StartTime.Ticks;
            long end = ResolveWindowEnd(window, start);
            if (ContainsPosition(position, start, end, weekTicks) || ContainsPosition(position + weekTicks, start, end, weekTicks))
            {
                return true;
            }
        }
        return false;
    }

    internal static long ResolveWindowEnd(SessionTimeWindow window, long start)
    {
        long end = (long)window.EndDay * TimeSpan.TicksPerDay + window.EndTime.Ticks;
        if (window.StartDay == window.EndDay && window.EndTime <= window.StartTime)
        {
            end += TimeSpan.TicksPerDay;
        }
        else if (end <= start)
        {
            end += 7L * TimeSpan.TicksPerDay;
        }
        return end;
    }

    internal static bool ContainsPosition(long position, long start, long end, long weekTicks)
    {
        return position >= start && position < end && end - start <= weekTicks;
    }

    internal static void ValidateWindows(SessionSchedule schedule)
    {
        if (!schedule.NonStop && schedule.Windows.Count == 0)
        {
            throw new FixSessionException("A scheduled session requires at least one time window.");
        }
        for (int index = 0; index < schedule.Windows.Count; index++)
        {
            SessionTimeWindow window = schedule.Windows[index];
            if (window == null || (int)window.StartDay < 0 || (int)window.StartDay > 6 || (int)window.EndDay < 0 || (int)window.EndDay > 6 || window.StartTime < TimeSpan.Zero || window.StartTime >= TimeSpan.FromDays(1) || window.EndTime < TimeSpan.Zero || window.EndTime >= TimeSpan.FromDays(1))
            {
                throw new FixSessionException("A session schedule window contains an invalid local time.");
            }
        }
    }

    internal static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.Equals(timeZoneId, "UTC", StringComparison.Ordinal))
        {
            return TimeZoneInfo.Utc;
        }
        ReadOnlyCollection<TimeZoneInfo> zones = TimeZoneInfo.GetSystemTimeZones();
        for (int index = 0; index < zones.Count; index++)
        {
            if (string.Equals(zones[index].Id, timeZoneId, StringComparison.Ordinal))
            {
                return zones[index];
            }
        }
        throw new FixSessionException("The configured session schedule timezone is unavailable: " + timeZoneId);
    }
}
