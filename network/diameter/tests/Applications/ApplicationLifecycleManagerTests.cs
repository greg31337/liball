using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class ApplicationLifecycleManagerTests
{
    [Test]
    public void GracefulUnregisterRemovesDispatchThenDrainsAcceptedWork()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        ApplicationLifecycleManager manager =
            new ApplicationLifecycleManager(registry);
        List<string> log = new List<string>();
        ManualResetEventSlim release = new ManualResetEventSlim(false);
        CountdownEvent entered = new CountdownEvent(1);
        LifecycleHandler handler =
            new LifecycleHandler(release, entered);
        LifecycleApplication application = new LifecycleApplication(
            "graceful",
            701U,
            1,
            2,
            5000,
            handler,
            registry,
            manager,
            log);
        DiameterApplicationRegistration registration =
            manager.StartApplicationModuleLifecycle(application);
        ApplicationWorkQueue queue =
            manager.ReadApplicationWorkQueue("graceful")!;
        CountdownEvent completed = new CountdownEvent(2);
        LifecycleCompletionCollector collector =
            new LifecycleCompletionCollector(completed, null, null);

        queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 1U),
            collector);
        Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 2U),
            collector);
        Task<ApplicationLifecycleResult> removal = Task.Run(() =>
            manager.UnregisterApplicationModuleLifecycle(
                "graceful",
                true));

        Assert.That(application.DrainEntered.Wait(
            TimeSpan.FromSeconds(5)), Is.True);
        ApplicationWorkAdmission rejected =
            queue.EnqueueApplicationWork(
                handler,
                CreateContext(registration, 3U),
                collector);

        Assert.Multiple(() =>
        {
            Assert.That(registry.ReadApplicationCount(), Is.Zero);
            Assert.That(removal.IsCompleted, Is.False);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Overloaded, Is.False);
            Assert.That(rejected.RejectionResult, Is.Not.Null);
            Assert.That(rejected.RejectionResult!.Code,
                Is.EqualTo("application-queue-not-accepting"));
        });

        release.Set();
        Assert.That(removal.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        ApplicationLifecycleResult result = removal.Result;
        ApplicationWorkQueueSnapshot snapshot =
            queue.ReadApplicationWorkQueueSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.DrainRequested, Is.True);
            Assert.That(result.Drained, Is.True);
            Assert.That(result.Forced, Is.False);
            Assert.That(result.Stopped, Is.True);
            Assert.That(result.Code,
                Is.EqualTo("application-gracefully-stopped"));
            Assert.That(handler.CallCount, Is.EqualTo(2));
            Assert.That(snapshot.ActiveCount, Is.Zero);
            Assert.That(snapshot.QueuedCount, Is.Zero);
            Assert.That(snapshot.CallbackCount, Is.Zero);
            Assert.That(snapshot.Accepting, Is.False);
            Assert.That(log,
                Is.EqualTo(new string[]
                {
                    "start:graceful",
                    "drain:graceful",
                    "stop:graceful"
                }));
            Assert.That(application.RegistryCountAtDrain, Is.Zero);
            Assert.That(application.ObservedLifecycleLock, Is.False);
            Assert.That(manager.ReadApplicationLifecycleSnapshot(
                "graceful"), Is.Null);
        });
    }

    [Test]
    public void GracefulDrainWaitsForCompletionCallback()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        ApplicationLifecycleManager manager =
            new ApplicationLifecycleManager(registry);
        List<string> log = new List<string>();
        LifecycleHandler handler =
            new LifecycleHandler(null, null);
        LifecycleApplication application = new LifecycleApplication(
            "callback-drain",
            702U,
            1,
            1,
            5000,
            handler,
            registry,
            manager,
            log);
        DiameterApplicationRegistration registration =
            manager.StartApplicationModuleLifecycle(application);
        ApplicationWorkQueue queue =
            manager.ReadApplicationWorkQueue("callback-drain")!;
        ManualResetEventSlim callbackRelease =
            new ManualResetEventSlim(false);
        CountdownEvent callbackEntered = new CountdownEvent(1);
        CountdownEvent completed = new CountdownEvent(1);
        LifecycleCompletionCollector collector =
            new LifecycleCompletionCollector(
                completed,
                callbackRelease,
                callbackEntered);

        queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 10U),
            collector);
        Assert.That(callbackEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Task<ApplicationLifecycleResult> removal = Task.Run(() =>
            manager.UnregisterApplicationModuleLifecycle(
                "callback-drain",
                true));
        Assert.That(application.DrainEntered.Wait(
            TimeSpan.FromSeconds(5)), Is.True);

        Assert.That(removal.Wait(TimeSpan.FromMilliseconds(50)), Is.False);
        callbackRelease.Set();
        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(removal.Wait(TimeSpan.FromSeconds(5)), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(removal.Result.Drained, Is.True);
            Assert.That(removal.Result.Forced, Is.False);
            Assert.That(application.StopCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DrainTimeoutForcesQueuedWorkAndStopsModule()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        ApplicationLifecycleManager manager =
            new ApplicationLifecycleManager(registry);
        List<string> log = new List<string>();
        ManualResetEventSlim release = new ManualResetEventSlim(false);
        CountdownEvent entered = new CountdownEvent(1);
        LifecycleHandler handler =
            new LifecycleHandler(release, entered);
        LifecycleApplication application = new LifecycleApplication(
            "timeout",
            703U,
            1,
            1,
            25,
            handler,
            registry,
            manager,
            log);
        DiameterApplicationRegistration registration =
            manager.StartApplicationModuleLifecycle(application);
        ApplicationWorkQueue queue =
            manager.ReadApplicationWorkQueue("timeout")!;
        CountdownEvent completed = new CountdownEvent(2);
        LifecycleCompletionCollector collector =
            new LifecycleCompletionCollector(completed, null, null);

        queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 20U),
            collector);
        Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        ApplicationWorkAdmission queued = queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 21U),
            collector);

        ApplicationLifecycleResult result =
            manager.UnregisterApplicationModuleLifecycle("timeout", true);
        ApplicationWorkCompletion forced =
            collector.WaitForCompletion(
                queued.WorkIdentifier,
                TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(result.Drained, Is.False);
            Assert.That(result.Forced, Is.True);
            Assert.That(result.TimedOut, Is.True);
            Assert.That(result.Stopped, Is.True);
            Assert.That(result.Code,
                Is.EqualTo("application-drain-timeout"));
            Assert.That(forced.HandlerFailed, Is.True);
            Assert.That(forced.Result.Code,
                Is.EqualTo("application-work-forced-stop"));
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(application.StopCount, Is.EqualTo(1));
        });

        release.Set();
        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(queue.WaitForApplicationWorkDrain(5000), Is.True);
    }

    [Test]
    public void ImmediateUnregisterForcesStopWithoutDrainCallback()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        ApplicationLifecycleManager manager =
            new ApplicationLifecycleManager(registry);
        List<string> log = new List<string>();
        LifecycleHandler handler =
            new LifecycleHandler(null, null);
        LifecycleApplication application = new LifecycleApplication(
            "forced",
            704U,
            1,
            1,
            5000,
            handler,
            registry,
            manager,
            log);

        manager.StartApplicationModuleLifecycle(application);
        ApplicationLifecycleResult result =
            manager.UnregisterApplicationModuleLifecycle("forced", false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.DrainRequested, Is.False);
            Assert.That(result.Forced, Is.True);
            Assert.That(result.Stopped, Is.True);
            Assert.That(result.Code,
                Is.EqualTo("application-forced-stop"));
            Assert.That(application.DrainCount, Is.Zero);
            Assert.That(application.StopCount, Is.EqualTo(1));
            Assert.That(registry.ReadApplicationCount(), Is.Zero);
            Assert.That(log,
                Is.EqualTo(new string[]
                {
                    "start:forced",
                    "stop:forced"
                }));
        });
    }

    [Test]
    public void NodeShutdownDrainsForwardAndStopsInReverseOrder()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        ApplicationLifecycleManager manager =
            new ApplicationLifecycleManager(registry);
        List<string> log = new List<string>();
        LifecycleApplication first = new LifecycleApplication(
            "first",
            705U,
            1,
            1,
            5000,
            new LifecycleHandler(null, null),
            registry,
            manager,
            log);
        LifecycleApplication second = new LifecycleApplication(
            "second",
            706U,
            1,
            1,
            5000,
            new LifecycleHandler(null, null),
            registry,
            manager,
            log);

        manager.StartApplicationModuleLifecycle(first);
        manager.StartApplicationModuleLifecycle(second);
        ApplicationShutdownResult result =
            manager.ShutdownApplicationModules();
        ApplicationShutdownResult repeated =
            manager.ShutdownApplicationModules();

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationCount, Is.EqualTo(2));
            Assert.That(result.DrainedCount, Is.EqualTo(2));
            Assert.That(result.ForcedCount, Is.Zero);
            Assert.That(result.StoppedCount, Is.EqualTo(2));
            Assert.That(result.Code,
                Is.EqualTo("application-shutdown-complete"));
            Assert.That(repeated.Code,
                Is.EqualTo("application-shutdown-already-started"));
            Assert.That(registry.ReadApplicationCount(), Is.Zero);
            Assert.That(log,
                Is.EqualTo(new string[]
                {
                    "start:first",
                    "start:second",
                    "drain:first",
                    "drain:second",
                    "stop:second",
                    "stop:first"
                }));
            Assert.That(first.RegistryCountAtDrain, Is.Zero);
            Assert.That(second.RegistryCountAtDrain, Is.Zero);
            Assert.That(first.ObservedLifecycleLock, Is.False);
            Assert.That(second.ObservedLifecycleLock, Is.False);
        });
    }

    private static DiameterRequestContext CreateContext(
        DiameterApplicationRegistration registration,
        uint hopByHopIdentifier)
    {
        DiameterRequestContext context = new DiameterRequestContext();
        context.Registration = registration;
        context.Command = registration.Commands[0];
        context.Request.Version = DiameterProtocolConstants.Version;
        context.Request.Flags = DiameterMessageFlags.Request;
        context.Request.CommandCode = context.Command.CommandCode;
        context.Request.ApplicationId = registration.ApplicationId;
        context.Request.HopByHopIdentifier = hopByHopIdentifier;
        context.PeerIdentity.Value = "peer.example";
        context.RouteIdentifier = "route-lifecycle";
        context.ReceivedAt = DateTimeOffset.UtcNow;
        return context;
    }

    private class LifecycleApplication : IDiameterApplication
    {
        private string Name;
        private uint ApplicationId;
        private int MaximumConcurrency;
        private int QueueCapacity;
        private int DrainTimeoutMilliseconds;
        private IDiameterRequestHandler Handler;
        private ApplicationRegistry Registry;
        private ApplicationLifecycleManager Manager;
        private List<string> Log;
        internal ManualResetEventSlim DrainEntered =
            new ManualResetEventSlim(false);
        internal int DrainCount;
        internal int StopCount;
        internal int RegistryCountAtDrain = -1;
        internal bool ObservedLifecycleLock;

        internal LifecycleApplication(
            string name,
            uint applicationId,
            int maximumConcurrency,
            int queueCapacity,
            int drainTimeoutMilliseconds,
            IDiameterRequestHandler handler,
            ApplicationRegistry registry,
            ApplicationLifecycleManager manager,
            List<string> log)
        {
            Name = name;
            ApplicationId = applicationId;
            MaximumConcurrency = maximumConcurrency;
            QueueCapacity = queueCapacity;
            DrainTimeoutMilliseconds = drainTimeoutMilliseconds;
            Handler = handler;
            Registry = registry;
            Manager = manager;
            Log = log;
        }

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            DiameterApplicationRegistration registration =
                new DiameterApplicationRegistration();
            registration.Name = Name;
            registration.ApplicationId = ApplicationId;
            registration.MaximumConcurrency = MaximumConcurrency;
            registration.QueueCapacity = QueueCapacity;
            registration.DrainTimeoutMilliseconds =
                DrainTimeoutMilliseconds;
            DiameterCommandRegistration command =
                new DiameterCommandRegistration();
            command.CommandCode = 300U;
            command.Name = "Lifecycle-Command";
            command.RequireValidation = false;
            registration.Commands.Add(command);
            return registration;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            return commandCode == 300U ? Handler : null;
        }

        public List<IMessageInterceptor>
            ReadDiameterApplicationInterceptors()
        {
            return new List<IMessageInterceptor>();
        }

        public void StartDiameterApplication()
        {
            ObservedLifecycleLock =
                ObservedLifecycleLock ||
                Monitor.IsEntered(Manager.LifecycleLock);
            lock (Log)
            {
                Log.Add("start:" + Name);
            }
        }

        public void DrainDiameterApplication()
        {
            ObservedLifecycleLock =
                ObservedLifecycleLock ||
                Monitor.IsEntered(Manager.LifecycleLock);
            RegistryCountAtDrain = Registry.ReadApplicationCount();
            DrainCount = DrainCount + 1;
            lock (Log)
            {
                Log.Add("drain:" + Name);
            }

            DrainEntered.Set();
        }

        public void StopDiameterApplication()
        {
            ObservedLifecycleLock =
                ObservedLifecycleLock ||
                Monitor.IsEntered(Manager.LifecycleLock);
            StopCount = StopCount + 1;
            lock (Log)
            {
                Log.Add("stop:" + Name);
            }
        }
    }

    private class LifecycleHandler : IDiameterRequestHandler
    {
        private ManualResetEventSlim? Release;
        private CountdownEvent? Entered;
        internal int CallCount;

        internal LifecycleHandler(
            ManualResetEventSlim? release,
            CountdownEvent? entered)
        {
            Release = release;
            Entered = entered;
        }

        public DiameterApplicationResult HandleDiameterApplicationRequest(
            DiameterRequestContext context)
        {
            Interlocked.Increment(ref CallCount);
            if (Entered != null && Entered.CurrentCount > 0)
            {
                Entered.Signal();
            }

            if (Release != null)
            {
                Release.Wait();
            }

            DiameterApplicationResult result =
                new DiameterApplicationResult();
            result.Continue = false;
            result.Drop = true;
            result.Code = "lifecycle-handler-complete";
            return result;
        }
    }

    private class LifecycleCompletionCollector :
        IApplicationWorkCompletionHandler
    {
        private object Sync = new object();
        private CountdownEvent Completed;
        private ManualResetEventSlim? CallbackRelease;
        private CountdownEvent? CallbackEntered;
        private List<ApplicationWorkCompletion> Completions =
            new List<ApplicationWorkCompletion>();

        internal LifecycleCompletionCollector(
            CountdownEvent completed,
            ManualResetEventSlim? callbackRelease,
            CountdownEvent? callbackEntered)
        {
            Completed = completed;
            CallbackRelease = callbackRelease;
            CallbackEntered = callbackEntered;
        }

        public void CompleteApplicationWork(
            ApplicationWorkCompletion completion)
        {
            if (CallbackEntered != null &&
                CallbackEntered.CurrentCount > 0)
            {
                CallbackEntered.Signal();
            }

            if (CallbackRelease != null)
            {
                CallbackRelease.Wait();
            }

            lock (Sync)
            {
                Completions.Add(completion);
            }

            Completed.Signal();
        }

        internal ApplicationWorkCompletion WaitForCompletion(
            long workIdentifier,
            TimeSpan timeout)
        {
            DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                lock (Sync)
                {
                    int index = 0;
                    while (index < Completions.Count)
                    {
                        if (Completions[index].WorkIdentifier ==
                            workIdentifier)
                        {
                            return Completions[index];
                        }

                        index = index + 1;
                    }
                }

                Thread.Sleep(1);
            }

            throw new InvalidOperationException("Completion not found.");
        }
    }
}
