using System;
using System.Collections.Generic;

namespace diameter;

internal class HealthCollector
{
    internal const string ListenersSubsystem = "listeners";
    internal const string PeersSubsystem = "peers";
    internal const string RoutesSubsystem = "routes";
    internal const string QueuesSubsystem = "queues";
    internal const string TimersSubsystem = "timers";
    internal const string StoresSubsystem = "stores";
    internal const string OverloadSubsystem = "overload";

    internal object HealthLock = new object();
    internal Dictionary<string, DiameterSubsystemHealth> Subsystems =
        new Dictionary<string, DiameterSubsystemHealth>(
            StringComparer.OrdinalIgnoreCase);
    internal List<string> SubsystemOrder = new List<string>();
    internal List<DiameterOperationalEvent> RecentFailures =
        new List<DiameterOperationalEvent>();
    internal IDiameterClock Clock;
    internal int RecentFailureCapacity;
    internal bool AcceptingRequests;
    internal bool Draining;
    internal bool Stopped = true;

    internal HealthCollector(
        IDiameterClock clock,
        int recentFailureCapacity)
    {
        if (clock == null)
        {
            throw new ArgumentException(
                "The health collector requires a clock.");
        }

        if (recentFailureCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "recentFailureCapacity");
        }

        Clock = clock;
        RecentFailureCapacity = recentFailureCapacity;
    }

    internal void UpdateDiameterNodeActivity(
        bool acceptingRequests,
        bool draining,
        bool stopped)
    {
        if (acceptingRequests && (draining || stopped) ||
            draining && stopped)
        {
            throw new ArgumentException(
                "The node activity state is inconsistent.");
        }

        lock (HealthLock)
        {
            AcceptingRequests = acceptingRequests;
            Draining = draining;
            Stopped = stopped;
        }
    }

    internal void ReportDiameterSubsystemHealth(
        DiameterSubsystemHealth? health)
    {
        if (health == null || string.IsNullOrWhiteSpace(health.Name))
        {
            throw new ArgumentException(
                "A subsystem health report requires a name.");
        }

        int status = (int)health.Status;
        if (status < (int)DiameterHealthStatus.Healthy ||
            status > (int)DiameterHealthStatus.Stopped ||
            health.FailureCount < 0L || health.PendingWork < 0L ||
            health.UtilizationPercentage < 0 ||
            health.UtilizationPercentage > 100 ||
            health.TimerLagMilliseconds < 0L)
        {
            throw new ArgumentOutOfRangeException(
                "Diameter subsystem health value");
        }

        DiameterSubsystemHealth owned =
            CloneDiameterSubsystemHealth(health);
        owned.Name = owned.Name.Trim();
        if (owned.CheckedAt == default(DateTimeOffset))
        {
            owned.CheckedAt = Clock.ReadUtcTime();
        }

        lock (HealthLock)
        {
            if (Subsystems.ContainsKey(owned.Name))
            {
                ApplyDiameterSubsystemHealthHistory(
                    owned,
                    Subsystems[owned.Name]);
            }
            else
            {
                ApplyDiameterSubsystemHealthHistory(owned, null);
                SubsystemOrder.Add(owned.Name);
            }

            Subsystems[owned.Name] = owned;
        }
    }

    internal bool RemoveDiameterSubsystemHealth(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A subsystem name is required.");
        }

        string normalized = name.Trim();
        lock (HealthLock)
        {
            if (!Subsystems.ContainsKey(normalized))
            {
                return false;
            }

            Subsystems.Remove(normalized);
            int index = 0;
            while (index < SubsystemOrder.Count)
            {
                if (string.Equals(
                    SubsystemOrder[index],
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
                {
                    SubsystemOrder.RemoveAt(index);
                    return true;
                }

                index = index + 1;
            }

            return true;
        }
    }

    internal void RecordDiameterHealthFailure(
        DiameterOperationalEvent? failure)
    {
        if (failure == null ||
            string.IsNullOrWhiteSpace(failure.Source))
        {
            throw new ArgumentException(
                "A recent failure requires an event source.");
        }

        DiameterOperationalEvent owned =
            failure.CloneDiameterOperationalEvent();
        owned.Source = owned.Source.Trim();
        if (owned.OccurredAt == default(DateTimeOffset))
        {
            owned.OccurredAt = Clock.ReadUtcTime();
        }

        lock (HealthLock)
        {
            RecentFailures.Add(owned);
            while (RecentFailures.Count > RecentFailureCapacity)
            {
                RecentFailures.RemoveAt(0);
            }
        }
    }

    internal void ClearDiameterHealthFailures()
    {
        lock (HealthLock)
        {
            RecentFailures.Clear();
        }
    }

    internal void ApplyDiameterSubsystemHealthHistory(
        DiameterSubsystemHealth current,
        DiameterSubsystemHealth? previous)
    {
        if (previous != null)
        {
            current.LastHealthyAt = previous.LastHealthyAt;
            current.LastFailureAt = previous.LastFailureAt;
            current.FailureCount = previous.FailureCount;
        }

        if (current.Status == DiameterHealthStatus.Healthy)
        {
            current.LastHealthyAt = current.CheckedAt;
            return;
        }

        bool failed = current.Status == DiameterHealthStatus.Degraded ||
            current.Status == DiameterHealthStatus.Unhealthy;
        bool newFailure = previous == null ||
            previous.Status == DiameterHealthStatus.Healthy ||
            previous.Status == DiameterHealthStatus.Stopped;
        if (failed && newFailure)
        {
            current.LastFailureAt = current.CheckedAt;
            if (current.FailureCount == 0L)
            {
                current.FailureCount = 1L;
            }
            else if (current.FailureCount < long.MaxValue)
            {
                current.FailureCount = current.FailureCount + 1L;
            }
        }
    }

    internal DiameterNodeHealth ReadDiameterNodeHealth()
    {
        DateTimeOffset checkedAt = Clock.ReadUtcTime();
        lock (HealthLock)
        {
            DiameterNodeHealth result = new DiameterNodeHealth();
            result.CheckedAt = checkedAt;
            result.Draining = Draining;
            result.AcceptingRequests =
                AcceptingRequests && !Draining && !Stopped;

            int index = 0;
            while (index < SubsystemOrder.Count)
            {
                string name = SubsystemOrder[index];
                result.Subsystems.Add(CloneDiameterSubsystemHealth(
                    Subsystems[name]));
                index = index + 1;
            }

            index = 0;
            while (index < RecentFailures.Count)
            {
                result.RecentFailures.Add(
                    RecentFailures[index].CloneDiameterOperationalEvent());
                index = index + 1;
            }

            ApplyDiameterNodeHealthStatus(result);
            return result;
        }
    }

    internal void ApplyDiameterNodeHealthStatus(DiameterNodeHealth result)
    {
        if (Stopped)
        {
            result.Status = DiameterHealthStatus.Stopped;
            result.Summary = "The Diameter node is stopped.";
            result.AcceptingRequests = false;
            return;
        }

        DiameterHealthStatus aggregate = DiameterHealthStatus.Healthy;
        int index = 0;
        while (index < result.Subsystems.Count)
        {
            DiameterSubsystemHealth subsystem = result.Subsystems[index];
            if (subsystem.Status == DiameterHealthStatus.Degraded &&
                aggregate == DiameterHealthStatus.Healthy)
            {
                aggregate = DiameterHealthStatus.Degraded;
            }
            else if (subsystem.Status == DiameterHealthStatus.Unhealthy ||
                subsystem.Status == DiameterHealthStatus.Stopped)
            {
                if (subsystem.Required)
                {
                    aggregate = DiameterHealthStatus.Unhealthy;
                }
                else if (aggregate == DiameterHealthStatus.Healthy)
                {
                    aggregate = DiameterHealthStatus.Degraded;
                }
            }

            index = index + 1;
        }

        if (Draining && aggregate == DiameterHealthStatus.Healthy)
        {
            aggregate = DiameterHealthStatus.Degraded;
        }

        if (result.RecentFailures.Count > 0 &&
            aggregate == DiameterHealthStatus.Healthy)
        {
            aggregate = DiameterHealthStatus.Degraded;
        }

        result.Status = aggregate;
        if (aggregate == DiameterHealthStatus.Healthy)
        {
            result.Summary = "All reported subsystems are healthy.";
        }
        else if (aggregate == DiameterHealthStatus.Degraded)
        {
            result.Summary = "The Diameter node is operating with reduced capability.";
        }
        else
        {
            result.Summary = "A required Diameter subsystem is unavailable.";
            result.AcceptingRequests = false;
        }
    }

    internal static DiameterSubsystemHealth CloneDiameterSubsystemHealth(
        DiameterSubsystemHealth source)
    {
        DiameterSubsystemHealth result = new DiameterSubsystemHealth();
        result.Name = source.Name;
        result.Status = source.Status;
        result.Detail = source.Detail;
        result.CheckedAt = source.CheckedAt;
        result.LastHealthyAt = source.LastHealthyAt;
        result.LastFailureAt = source.LastFailureAt;
        result.FailureCount = source.FailureCount;
        result.Required = source.Required;
        result.PendingWork = source.PendingWork;
        result.UtilizationPercentage = source.UtilizationPercentage;
        result.TimerLagMilliseconds = source.TimerLagMilliseconds;
        return result;
    }
}
