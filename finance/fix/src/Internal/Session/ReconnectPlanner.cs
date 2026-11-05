using System;
using System.Collections.Generic;

namespace fix;

internal class ReconnectPlanner
{
    internal List<SessionEndpoint> Endpoints = [];
    internal int InitialDelayMilliseconds;
    internal int MaximumDelayMilliseconds;
    internal int NextDelayMilliseconds;
    internal int EndpointIndex;
    internal bool Waiting;
    internal DateTime RetryAtUtc;

    internal ReconnectPlanner(List<SessionEndpoint> endpoints, int initialDelayMilliseconds, int maximumDelayMilliseconds)
    {
        if (endpoints == null || endpoints.Count == 0)
        {
            throw new FixSessionException("Reconnect planning requires at least one endpoint.");
        }
        if (initialDelayMilliseconds < 0 || maximumDelayMilliseconds < initialDelayMilliseconds)
        {
            throw new FixSessionException("The reconnect delay range is invalid.");
        }
        for (int index = 0; index < endpoints.Count; index++)
        {
            if (endpoints[index] == null)
            {
                throw new FixSessionException("A reconnect endpoint is missing.");
            }
            Endpoints.Add(endpoints[index]);
        }
        for (int index = 1; index < Endpoints.Count; index++)
        {
            SessionEndpoint selected = Endpoints[index];
            int position = index;
            while (position > 0 && Endpoints[position - 1].Priority > selected.Priority)
            {
                Endpoints[position] = Endpoints[position - 1];
                position--;
            }
            Endpoints[position] = selected;
        }
        InitialDelayMilliseconds = initialDelayMilliseconds;
        MaximumDelayMilliseconds = maximumDelayMilliseconds;
        NextDelayMilliseconds = initialDelayMilliseconds;
    }

    internal SessionEndpoint GetCurrentEndpoint()
    {
        return Endpoints[EndpointIndex];
    }

    internal void ScheduleNextAttempt(DateTime now)
    {
        ValidateUtc(now);
        EndpointIndex++;
        int delay = 0;
        if (EndpointIndex >= Endpoints.Count)
        {
            EndpointIndex = 0;
            delay = NextDelayMilliseconds;
            NextDelayMilliseconds = DoubleDelay(NextDelayMilliseconds, MaximumDelayMilliseconds);
        }
        RetryAtUtc = AddDelay(now, delay);
        Waiting = true;
    }

    internal bool IsAttemptDue(DateTime now)
    {
        ValidateUtc(now);
        return Waiting && now >= RetryAtUtc;
    }

    internal void MarkAttemptStarted()
    {
        Waiting = false;
    }

    internal void MarkConnected()
    {
        Waiting = false;
        NextDelayMilliseconds = InitialDelayMilliseconds;
    }

    internal void ResetPlan()
    {
        EndpointIndex = 0;
        Waiting = false;
        NextDelayMilliseconds = InitialDelayMilliseconds;
    }

    internal static int DoubleDelay(int delay, int maximum)
    {
        if (delay >= maximum)
        {
            return maximum;
        }
        long doubled = (long)delay * 2L;
        return doubled >= maximum ? maximum : (int)doubled;
    }

    internal static DateTime AddDelay(DateTime start, int delayMilliseconds)
    {
        long delayTicks = (long)delayMilliseconds * TimeSpan.TicksPerMillisecond;
        if (start.Ticks > DateTime.MaxValue.Ticks - delayTicks)
        {
            throw new FixSessionException("The reconnect deadline exceeds the supported UTC range.");
        }
        return start.AddTicks(delayTicks);
    }

    internal static void ValidateUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new FixSessionException("Reconnect planning requires UTC timestamps.");
        }
    }
}
