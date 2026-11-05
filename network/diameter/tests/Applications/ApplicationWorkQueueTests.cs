using System;
using System.Collections.Generic;
using System.Threading;

namespace diameter.Tests;

public class ApplicationWorkQueueTests
{
    [Test]
    public void ConfiguredConcurrencyAndQueueCapacityBoundAcceptedWork()
    {
        DiameterApplicationRegistration registration =
            CreateRegistration(2, 3);
        ApplicationWorkQueue queue =
            new ApplicationWorkQueue(registration);
        ManualResetEventSlim release = new ManualResetEventSlim(false);
        CountdownEvent entered = new CountdownEvent(2);
        QueueTestHandler handler =
            new QueueTestHandler(false, release, entered);
        CountdownEvent completed = new CountdownEvent(5);
        QueueCompletionCollector collector =
            new QueueCompletionCollector(completed, queue, false);
        List<ApplicationWorkAdmission> admissions =
            new List<ApplicationWorkAdmission>();

        int index = 0;
        while (index < 5)
        {
            admissions.Add(queue.EnqueueApplicationWork(
                handler,
                CreateContext(registration, (uint)(index + 1)),
                collector));
            index = index + 1;
        }

        Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        ApplicationWorkAdmission rejected =
            queue.EnqueueApplicationWork(
                handler,
                CreateContext(registration, 6U),
                collector);
        ApplicationWorkQueueSnapshot saturated =
            queue.ReadApplicationWorkQueueSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(admissions[0].Started, Is.True);
            Assert.That(admissions[1].Started, Is.True);
            Assert.That(admissions[2].Queued, Is.True);
            Assert.That(admissions[3].Queued, Is.True);
            Assert.That(admissions[4].Queued, Is.True);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Overloaded, Is.True);
            Assert.That(rejected.RejectionResult, Is.Not.Null);
            Assert.That(rejected.RejectionResult!.Fail, Is.True);
            Assert.That(rejected.RejectionResult.ResultCode,
                Is.EqualTo(DiameterResultCodes.TooBusy));
            Assert.That(rejected.RejectionResult.Code,
                Is.EqualTo("application-queue-overloaded"));
            Assert.That(saturated.ActiveCount, Is.EqualTo(2));
            Assert.That(saturated.QueuedCount, Is.EqualTo(3));
            Assert.That(saturated.AcceptedCount, Is.EqualTo(5L));
            Assert.That(saturated.RejectedCount, Is.EqualTo(1L));
            Assert.That(handler.MaximumActive, Is.EqualTo(2));
        });

        release.Set();
        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        ApplicationWorkQueueSnapshot drained =
            queue.ReadApplicationWorkQueueSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(drained.ActiveCount, Is.Zero);
            Assert.That(drained.QueuedCount, Is.Zero);
            Assert.That(drained.CompletedCount, Is.EqualTo(5L));
            Assert.That(drained.FailedCount, Is.Zero);
            Assert.That(handler.MaximumActive, Is.LessThanOrEqualTo(2));
        });
    }

    [Test]
    public void QueuedWorkIsFifoAndOwnsRequestContexts()
    {
        DiameterApplicationRegistration registration =
            CreateRegistration(1, 2);
        ApplicationWorkQueue queue =
            new ApplicationWorkQueue(registration);
        ManualResetEventSlim release = new ManualResetEventSlim(false);
        CountdownEvent entered = new CountdownEvent(1);
        QueueTestHandler handler =
            new QueueTestHandler(false, release, entered);
        CountdownEvent completed = new CountdownEvent(3);
        QueueCompletionCollector collector =
            new QueueCompletionCollector(completed, queue, false);
        DiameterRequestContext first = CreateContext(registration, 11U);
        DiameterRequestContext second = CreateContext(registration, 12U);
        DiameterRequestContext third = CreateContext(registration, 13U);

        queue.EnqueueApplicationWork(handler, first, collector);
        Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        queue.EnqueueApplicationWork(handler, second, collector);
        queue.EnqueueApplicationWork(handler, third, collector);
        first.Request.HopByHopIdentifier = 91U;
        second.Request.HopByHopIdentifier = 92U;
        third.Request.HopByHopIdentifier = 93U;
        release.Set();

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(handler.ReadSeenIdentifiers(),
            Is.EqualTo(new uint[] { 11U, 12U, 13U }));
    }

    [Test]
    public void HandlerExceptionBecomesFailureAndDoesNotStallQueuedWork()
    {
        DiameterApplicationRegistration registration =
            CreateRegistration(1, 1);
        ApplicationWorkQueue queue =
            new ApplicationWorkQueue(registration);
        ManualResetEventSlim release = new ManualResetEventSlim(false);
        CountdownEvent entered = new CountdownEvent(1);
        QueueTestHandler failing =
            new QueueTestHandler(true, release, entered);
        QueueTestHandler succeeding =
            new QueueTestHandler(false, null, null);
        CountdownEvent completed = new CountdownEvent(2);
        QueueCompletionCollector collector =
            new QueueCompletionCollector(completed, queue, false);

        ApplicationWorkAdmission first = queue.EnqueueApplicationWork(
            failing,
            CreateContext(registration, 21U),
            collector);
        Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        ApplicationWorkAdmission second = queue.EnqueueApplicationWork(
            succeeding,
            CreateContext(registration, 22U),
            collector);
        release.Set();

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        ApplicationWorkCompletion failed =
            collector.FindCompletion(first.WorkIdentifier);
        ApplicationWorkCompletion succeeded =
            collector.FindCompletion(second.WorkIdentifier);
        ApplicationWorkQueueSnapshot snapshot =
            queue.ReadApplicationWorkQueueSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(first.Started, Is.True);
            Assert.That(second.Queued, Is.True);
            Assert.That(failed.HandlerFailed, Is.True);
            Assert.That(failed.Result.Fail, Is.True);
            Assert.That(failed.Result.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(failed.Result.Code,
                Is.EqualTo("application-handler-exception"));
            Assert.That(succeeded.HandlerFailed, Is.False);
            Assert.That(succeeded.Result.Drop, Is.True);
            Assert.That(snapshot.CompletedCount, Is.EqualTo(2L));
            Assert.That(snapshot.FailedCount, Is.EqualTo(1L));
            Assert.That(snapshot.ActiveCount, Is.Zero);
            Assert.That(snapshot.QueuedCount, Is.Zero);
        });
    }

    [Test]
    public void ThrowingCompletionRunsOutsideLockAndDoesNotStallQueue()
    {
        DiameterApplicationRegistration registration =
            CreateRegistration(1, 1);
        ApplicationWorkQueue queue =
            new ApplicationWorkQueue(registration);
        ManualResetEventSlim release = new ManualResetEventSlim(false);
        CountdownEvent entered = new CountdownEvent(1);
        QueueTestHandler handler =
            new QueueTestHandler(false, release, entered);
        CountdownEvent completed = new CountdownEvent(2);
        QueueCompletionCollector collector =
            new QueueCompletionCollector(completed, queue, true);

        queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 31U),
            collector);
        Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 32U),
            collector);
        release.Set();

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        ApplicationWorkQueueSnapshot snapshot =
            queue.ReadApplicationWorkQueueSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(collector.CallbackObservedQueueLock, Is.False);
            Assert.That(collector.CallbackCount, Is.EqualTo(2));
            Assert.That(snapshot.CompletedCount, Is.EqualTo(2L));
            Assert.That(snapshot.ActiveCount, Is.Zero);
            Assert.That(snapshot.QueuedCount, Is.Zero);
        });
    }

    private static DiameterApplicationRegistration CreateRegistration(
        int maximumConcurrency,
        int queueCapacity)
    {
        DiameterApplicationRegistration registration =
            new DiameterApplicationRegistration();
        registration.Name = "queue-test";
        registration.ApplicationId = 700U;
        registration.MaximumConcurrency = maximumConcurrency;
        registration.QueueCapacity = queueCapacity;
        DiameterCommandRegistration command =
            new DiameterCommandRegistration();
        command.CommandCode = 300U;
        command.Name = "Queue-Test";
        command.RequireValidation = false;
        registration.Commands.Add(command);
        return registration;
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
        context.RouteIdentifier = "route-a";
        context.ReceivedAt = DateTimeOffset.UtcNow;
        return context;
    }

    private class QueueTestHandler : IDiameterRequestHandler
    {
        private object Sync = new object();
        private bool Fail;
        private ManualResetEventSlim? Release;
        private CountdownEvent? Entered;
        private List<uint> SeenIdentifiers = new List<uint>();
        internal int Active;
        internal int MaximumActive;

        internal QueueTestHandler(
            bool fail,
            ManualResetEventSlim? release,
            CountdownEvent? entered)
        {
            Fail = fail;
            Release = release;
            Entered = entered;
        }

        public DiameterApplicationResult HandleDiameterApplicationRequest(
            DiameterRequestContext context)
        {
            lock (Sync)
            {
                Active = Active + 1;
                if (Active > MaximumActive)
                {
                    MaximumActive = Active;
                }

                SeenIdentifiers.Add(context.Request.HopByHopIdentifier);
            }

            if (Entered != null && Entered.CurrentCount > 0)
            {
                Entered.Signal();
            }

            if (Release != null)
            {
                Release.Wait();
            }

            try
            {
                if (Fail)
                {
                    throw new InvalidOperationException("handler failure");
                }

                DiameterApplicationResult result =
                    new DiameterApplicationResult();
                result.Continue = false;
                result.Drop = true;
                result.Code = "queue-test-complete";
                return result;
            }
            finally
            {
                lock (Sync)
                {
                    Active = Active - 1;
                }
            }
        }

        internal List<uint> ReadSeenIdentifiers()
        {
            lock (Sync)
            {
                return new List<uint>(SeenIdentifiers);
            }
        }
    }

    private class QueueCompletionCollector :
        IApplicationWorkCompletionHandler
    {
        private object Sync = new object();
        private CountdownEvent Completed;
        private ApplicationWorkQueue Queue;
        private bool ThrowFirst;
        private List<ApplicationWorkCompletion> Completions =
            new List<ApplicationWorkCompletion>();
        internal bool CallbackObservedQueueLock;
        internal int CallbackCount;

        internal QueueCompletionCollector(
            CountdownEvent completed,
            ApplicationWorkQueue queue,
            bool throwFirst)
        {
            Completed = completed;
            Queue = queue;
            ThrowFirst = throwFirst;
        }

        public void CompleteApplicationWork(
            ApplicationWorkCompletion completion)
        {
            bool throwCallback = false;
            lock (Sync)
            {
                CallbackObservedQueueLock =
                    CallbackObservedQueueLock ||
                    Monitor.IsEntered(Queue.QueueLock);
                CallbackCount = CallbackCount + 1;
                Completions.Add(completion);
                if (ThrowFirst && CallbackCount == 1)
                {
                    throwCallback = true;
                }
            }

            Completed.Signal();
            if (throwCallback)
            {
                throw new InvalidOperationException("callback failure");
            }
        }

        internal ApplicationWorkCompletion FindCompletion(
            long workIdentifier)
        {
            lock (Sync)
            {
                int index = 0;
                while (index < Completions.Count)
                {
                    if (Completions[index].WorkIdentifier == workIdentifier)
                    {
                        return Completions[index];
                    }

                    index = index + 1;
                }
            }

            throw new InvalidOperationException("Completion not found.");
        }
    }
}
