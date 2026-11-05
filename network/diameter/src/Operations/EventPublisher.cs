using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal class EventPublisher
{
    internal object EventLock = new object();
    internal List<DiameterOperationalEvent> PendingEvents =
        new List<DiameterOperationalEvent>();
    internal Dictionary<string, long> SourceSequences =
        new Dictionary<string, long>(StringComparer.Ordinal);
    internal IDiameterClock Clock;
    internal IDiameterEventSink? Sink;
    internal IRedactionPolicy? RedactionPolicy;
    internal ManualResetEventSlim IdleSignal =
        new ManualResetEventSlim(true);
    internal bool Dispatching;
    internal long PublicationCount;
    internal long DeliveryCount;
    internal long FailureCount;

    internal EventPublisher(
        IDiameterClock clock,
        IDiameterEventSink? sink,
        IRedactionPolicy? redactionPolicy)
    {
        if (clock == null)
        {
            throw new ArgumentException(
                "The event publisher requires a clock.");
        }

        Clock = clock;
        Sink = sink;
        RedactionPolicy = redactionPolicy;
    }

    internal void PublishDiameterEvent(DiameterOperationalEvent? eventData)
    {
        if (eventData == null ||
            string.IsNullOrWhiteSpace(eventData.Source))
        {
            throw new ArgumentException(
                "An operational event requires a source.");
        }

        DiameterOperationalEvent owned =
            eventData.CloneDiameterOperationalEvent();
        owned.Source = owned.Source.Trim();
        if (owned.OccurredAt == default(DateTimeOffset))
        {
            owned.OccurredAt = Clock.ReadUtcTime();
        }

        bool startDelivery = false;
        lock (EventLock)
        {
            long sequence = 1L;
            if (SourceSequences.ContainsKey(owned.Source))
            {
                sequence = SourceSequences[owned.Source] + 1L;
            }

            SourceSequences[owned.Source] = sequence;
            owned.SequenceNumber = sequence;
            PendingEvents.Add(owned);
            PublicationCount = PublicationCount + 1L;
            IdleSignal.Reset();
            if (!Dispatching)
            {
                Dispatching = true;
                startDelivery = true;
            }
        }

        if (startDelivery)
        {
            StartDiameterEventDelivery();
        }
    }

    internal bool WaitForDiameterEvents(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                "timeoutMilliseconds");
        }

        return IdleSignal.Wait(timeoutMilliseconds);
    }

    internal long ReadDiameterEventPublicationCount()
    {
        lock (EventLock)
        {
            return PublicationCount;
        }
    }

    internal long ReadDiameterEventDeliveryCount()
    {
        lock (EventLock)
        {
            return DeliveryCount;
        }
    }

    internal long ReadDiameterEventFailureCount()
    {
        lock (EventLock)
        {
            return FailureCount;
        }
    }

    internal void StartDiameterEventDelivery()
    {
        DiameterOperationalEvent? eventData = null;
        lock (EventLock)
        {
            if (PendingEvents.Count > 0)
            {
                eventData = PendingEvents[0];
                PendingEvents.RemoveAt(0);
            }
            else
            {
                Dispatching = false;
                IdleSignal.Set();
            }
        }

        if (eventData != null)
        {
            DiameterOperationalEvent deliveryEvent = eventData;
            Task delivery = Task.Run(() =>
                DeliverDiameterEvent(deliveryEvent));
            delivery.ContinueWith(completed =>
                CompleteDiameterEventDelivery(completed),
                TaskScheduler.Default);
        }
    }

    internal void DeliverDiameterEvent(
        DiameterOperationalEvent eventData)
    {
        if (Sink == null)
        {
            return;
        }

        DiameterOperationalEvent published = eventData;
        if (RedactionPolicy != null)
        {
            published = RedactionPolicy.RedactDiameterEvent(eventData);
            if (published == null)
            {
                throw new ArgumentException(
                    "The redaction policy returned no event.");
            }

            published.SequenceNumber = eventData.SequenceNumber;
            published.OccurredAt = eventData.OccurredAt;
            published.Source = eventData.Source;
        }

        Sink.HandleDiameterEvent(published);
    }

    internal void CompleteDiameterEventDelivery(Task delivery)
    {
        bool failed = delivery.IsFaulted || delivery.IsCanceled;
        if (delivery.IsFaulted)
        {
            delivery.Exception?.GetBaseException();
        }

        lock (EventLock)
        {
            if (failed)
            {
                FailureCount = FailureCount + 1L;
            }
            else
            {
                DeliveryCount = DeliveryCount + 1L;
            }
        }

        StartDiameterEventDelivery();
    }
}
