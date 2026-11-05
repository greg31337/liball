using System;
using System.Collections.Generic;
using System.Threading;

namespace diameter;

internal class ApplicationLifecycleResult
{
    internal string ApplicationName = string.Empty;
    internal bool Found;
    internal bool DrainRequested;
    internal bool Drained;
    internal bool Forced;
    internal bool TimedOut;
    internal bool Stopped;
    internal string Code = string.Empty;
}

internal class ApplicationShutdownResult
{
    internal int ApplicationCount;
    internal int DrainedCount;
    internal int ForcedCount;
    internal int StoppedCount;
    internal string Code = string.Empty;
}

internal class ApplicationLifecycleSnapshot
{
    internal string ApplicationName = string.Empty;
    internal bool Started;
    internal bool Removing;
    internal bool Stopped;
    internal ApplicationWorkQueueSnapshot Queue =
        new ApplicationWorkQueueSnapshot();
}

internal class ApplicationLifecycleEntry
{
    internal IDiameterApplication Application;
    internal DiameterApplicationRegistration Registration;
    internal ApplicationWorkQueue Queue;
    internal ManualResetEventSlim StartSignal =
        new ManualResetEventSlim(false);
    internal bool Started;
    internal bool Removing;
    internal bool Stopped;

    internal ApplicationLifecycleEntry(
        IDiameterApplication application,
        DiameterApplicationRegistration registration,
        ApplicationWorkQueue queue)
    {
        Application = application;
        Registration = registration;
        Queue = queue;
    }
}

internal class ApplicationLifecycleManager
{
    internal object LifecycleLock = new object();
    internal ApplicationRegistry Registry;
    internal Dictionary<string, ApplicationLifecycleEntry> Entries =
        new Dictionary<string, ApplicationLifecycleEntry>(
            StringComparer.OrdinalIgnoreCase);
    internal List<ApplicationLifecycleEntry> EntryOrder =
        new List<ApplicationLifecycleEntry>();
    internal bool ShutdownStarted;

    internal ApplicationLifecycleManager(ApplicationRegistry? registry)
    {
        if (registry == null)
        {
            throw new ArgumentException(
                "An application lifecycle manager requires a registry.");
        }

        Registry = registry;
    }

    internal DiameterApplicationRegistration StartApplicationModuleLifecycle(
        IDiameterApplication? application)
    {
        if (application == null)
        {
            throw new ArgumentException(
                "An application module is required.");
        }

        ApplicationLifecycleEntry entry;
        DiameterApplicationRegistration registration;
        lock (LifecycleLock)
        {
            if (ShutdownStarted)
            {
                throw new DiameterApplicationException(
                    "Application registration is unavailable during node shutdown.");
            }

            registration = Registry.RegisterApplicationModule(application);
            ApplicationWorkQueue queue =
                new ApplicationWorkQueue(registration);
            entry = new ApplicationLifecycleEntry(
                application,
                registration,
                queue);
            Entries.Add(registration.Name, entry);
            EntryOrder.Add(entry);
        }

        application.StartDiameterApplication();
        lock (LifecycleLock)
        {
            entry.Started = true;
            entry.StartSignal.Set();
        }

        return ApplicationRegistryCloner.CloneApplicationRegistration(
            registration);
    }

    internal ApplicationLifecycleResult UnregisterApplicationModuleLifecycle(
        string? registrationName,
        bool drain)
    {
        ApplicationLifecycleResult result =
            new ApplicationLifecycleResult();
        if (string.IsNullOrWhiteSpace(registrationName))
        {
            result.Code = "application-lifecycle-not-found";
            return result;
        }

        ApplicationLifecycleEntry? entry = null;
        lock (LifecycleLock)
        {
            if (!Entries.ContainsKey(registrationName))
            {
                result.Code = "application-lifecycle-not-found";
                return result;
            }

            ApplicationLifecycleEntry candidate =
                Entries[registrationName];
            result.ApplicationName = candidate.Registration.Name;
            result.Found = true;
            result.DrainRequested = drain;
            if (candidate.Removing)
            {
                result.Code = "application-removal-in-progress";
                return result;
            }

            candidate.Removing = true;
            Registry.RemoveApplicationModule(candidate.Registration.Name);
            candidate.Queue.BeginApplicationWorkDrain();
            entry = candidate;
        }

        entry.StartSignal.Wait();
        if (drain)
        {
            entry.Application.DrainDiameterApplication();
            result.Drained = entry.Queue.WaitForApplicationWorkDrain(
                entry.Registration.DrainTimeoutMilliseconds);
            if (!result.Drained)
            {
                result.TimedOut = true;
                result.Forced = true;
                entry.Queue.ForceStopApplicationWork();
            }
        }
        else
        {
            result.Forced = true;
            entry.Queue.ForceStopApplicationWork();
        }

        entry.Application.StopDiameterApplication();
        result.Stopped = true;
        result.Code = result.TimedOut ?
            "application-drain-timeout" :
            (result.Forced ?
            "application-forced-stop" :
            "application-gracefully-stopped");
        lock (LifecycleLock)
        {
            entry.Stopped = true;
            Entries.Remove(entry.Registration.Name);
            EntryOrder.Remove(entry);
        }

        return result;
    }

    internal ApplicationShutdownResult ShutdownApplicationModules()
    {
        List<ApplicationLifecycleEntry> shutdownEntries =
            new List<ApplicationLifecycleEntry>();
        lock (LifecycleLock)
        {
            if (ShutdownStarted)
            {
                ApplicationShutdownResult repeated =
                    new ApplicationShutdownResult();
                repeated.Code = "application-shutdown-already-started";
                return repeated;
            }

            ShutdownStarted = true;
            int index = 0;
            while (index < EntryOrder.Count)
            {
                ApplicationLifecycleEntry entry = EntryOrder[index];
                if (!entry.Removing)
                {
                    entry.Removing = true;
                    Registry.RemoveApplicationModule(
                        entry.Registration.Name);
                    entry.Queue.BeginApplicationWorkDrain();
                    shutdownEntries.Add(entry);
                }

                index = index + 1;
            }
        }

        int startIndex = 0;
        while (startIndex < shutdownEntries.Count)
        {
            shutdownEntries[startIndex].StartSignal.Wait();
            startIndex = startIndex + 1;
        }

        int drainIndex = 0;
        while (drainIndex < shutdownEntries.Count)
        {
            shutdownEntries[drainIndex].Application
                .DrainDiameterApplication();
            drainIndex = drainIndex + 1;
        }

        ApplicationShutdownResult result =
            new ApplicationShutdownResult();
        result.ApplicationCount = shutdownEntries.Count;
        int waitIndex = 0;
        while (waitIndex < shutdownEntries.Count)
        {
            ApplicationLifecycleEntry entry = shutdownEntries[waitIndex];
            bool drained = entry.Queue.WaitForApplicationWorkDrain(
                entry.Registration.DrainTimeoutMilliseconds);
            if (drained)
            {
                result.DrainedCount = result.DrainedCount + 1;
            }
            else
            {
                entry.Queue.ForceStopApplicationWork();
                result.ForcedCount = result.ForcedCount + 1;
            }

            waitIndex = waitIndex + 1;
        }

        int stopIndex = shutdownEntries.Count - 1;
        while (stopIndex >= 0)
        {
            ApplicationLifecycleEntry entry = shutdownEntries[stopIndex];
            entry.Application.StopDiameterApplication();
            entry.Stopped = true;
            result.StoppedCount = result.StoppedCount + 1;
            stopIndex = stopIndex - 1;
        }

        lock (LifecycleLock)
        {
            int removeIndex = 0;
            while (removeIndex < shutdownEntries.Count)
            {
                ApplicationLifecycleEntry entry =
                    shutdownEntries[removeIndex];
                Entries.Remove(entry.Registration.Name);
                EntryOrder.Remove(entry);
                removeIndex = removeIndex + 1;
            }
        }

        result.Code = result.ForcedCount == 0 ?
            "application-shutdown-complete" :
            "application-shutdown-forced";
        return result;
    }

    internal ApplicationWorkQueue? ReadApplicationWorkQueue(
        string? registrationName)
    {
        if (string.IsNullOrWhiteSpace(registrationName))
        {
            return null;
        }

        lock (LifecycleLock)
        {
            if (!Entries.ContainsKey(registrationName))
            {
                return null;
            }

            return Entries[registrationName].Queue;
        }
    }

    internal ApplicationLifecycleSnapshot? ReadApplicationLifecycleSnapshot(
        string? registrationName)
    {
        if (string.IsNullOrWhiteSpace(registrationName))
        {
            return null;
        }

        lock (LifecycleLock)
        {
            if (!Entries.ContainsKey(registrationName))
            {
                return null;
            }

            ApplicationLifecycleEntry entry = Entries[registrationName];
            ApplicationLifecycleSnapshot result =
                new ApplicationLifecycleSnapshot();
            result.ApplicationName = entry.Registration.Name;
            result.Started = entry.Started;
            result.Removing = entry.Removing;
            result.Stopped = entry.Stopped;
            result.Queue = entry.Queue.ReadApplicationWorkQueueSnapshot();
            return result;
        }
    }
}
