using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class EventSourceReadResult
{
    internal JmapEvent? Event;
    internal bool HasEvent;
    internal bool IsComplete;
}

internal class EventSourceSubscriber
{
    internal string Id;
    internal EventSourceOptions Options;
    internal List<JmapEvent> Buffer = new List<JmapEvent>();
    internal SemaphoreSlim Signal = new SemaphoreSlim(0);
    internal DateTimeOffset LastActivityUtc;
    internal CancellationTokenRegistration CancellationRegistration;
    internal bool HasCancellationRegistration;
    internal bool Complete;
    internal bool Active = true;

    internal EventSourceSubscriber(string id, EventSourceOptions options, DateTimeOffset lastActivityUtc)
    {
        Id = id;
        Options = options;
        LastActivityUtc = lastActivityUtc;
    }
}

internal class EventSourceHub : IJmapStatePublisher
{
    internal JmapServerOptions Options;
    internal IJmapClock? Clock;
    internal int MaximumBufferedEvents;
    internal int MaximumHistoryEvents;
    internal int MaximumSubscribers = SecurityLimits.MaximumEventSourceSubscribers;
    internal Dictionary<string, EventSourceSubscriber> Subscribers = new Dictionary<string, EventSourceSubscriber>(StringComparer.Ordinal);
    internal List<JmapStatePublication> History = new List<JmapStatePublication>();
    internal StateChange? CurrentState;
    internal string? CurrentEventId;
    internal long NextSubscriberId;
    internal PushValidator Validator = new PushValidator();

    internal EventSourceHub(JmapServerOptions options, IJmapClock? clock, int maximumBufferedEvents, int maximumHistoryEvents)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (maximumBufferedEvents <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumBufferedEvents");
        }

        if (maximumHistoryEvents <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumHistoryEvents");
        }

        Options = options;
        Clock = clock;
        MaximumBufferedEvents = maximumBufferedEvents;
        MaximumHistoryEvents = maximumHistoryEvents;
    }

    public Task PublishStateChangeAsync(JmapStatePublication publication, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Publish(publication);
        return Task.CompletedTask;
    }

    internal EventSourceSubscriber Subscribe(EventSourceOptions options, CancellationToken cancellationToken)
    {
        EventSourceOptions normalized = NormalizeOptions(options);
        EventSourceSubscriber subscriber;
        lock (Subscribers)
        {
            if (Subscribers.Count >= MaximumSubscribers)
            {
                throw new InvalidOperationException("The EventSource subscriber limit was reached.");
            }

            NextSubscriberId = NextSubscriberId + 1;
            string id = NextSubscriberId.ToString(CultureInfo.InvariantCulture);
            subscriber = new EventSourceSubscriber(id, normalized, CurrentTime());
            Subscribers.Add(id, subscriber);
            Replay(subscriber);
        }

        if (cancellationToken.CanBeCanceled)
        {
            CancellationTokenRegistration registration = cancellationToken.Register(() => CancelSubscriber(subscriber.Id));
            subscriber.CancellationRegistration = registration;
            subscriber.HasCancellationRegistration = true;
        }

        return subscriber;
    }

    internal EventSourceReadResult TryReadNext(EventSourceSubscriber subscriber)
    {
        if (subscriber == null)
        {
            throw new ArgumentNullException("subscriber");
        }

        EventSourceReadResult result = new EventSourceReadResult();
        bool remove = false;
        lock (subscriber.Buffer)
        {
            if (subscriber.Buffer.Count > 0)
            {
                result.Event = subscriber.Buffer[0];
                result.HasEvent = true;
                subscriber.Buffer.RemoveAt(0);
                subscriber.LastActivityUtc = CurrentTime();
                if (subscriber.Complete && subscriber.Buffer.Count == 0)
                {
                    remove = true;
                }
            }
            else if (subscriber.Complete)
            {
                result.IsComplete = true;
                remove = true;
            }
        }

        if (remove)
        {
            RemoveSubscriber(subscriber, true);
        }

        return result;
    }

    internal EventSourceReadResult ReadNext(EventSourceSubscriber subscriber, CancellationToken cancellationToken)
    {
        if (subscriber == null)
        {
            throw new ArgumentNullException("subscriber");
        }

        using CancellationTokenRegistration registration = cancellationToken.Register(() => CancelSubscriber(subscriber.Id));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EventSourceReadResult result = TryReadNext(subscriber);
            if (result.HasEvent || result.IsComplete)
            {
                return result;
            }

            int milliseconds = MillisecondsUntilPing(subscriber);
            bool signaled = subscriber.Signal.Wait(milliseconds, cancellationToken);
            if (!signaled)
            {
                EmitDuePings();
            }
        }
    }

    internal Task<EventSourceReadResult> ReadNextAsync(EventSourceSubscriber subscriber, CancellationToken cancellationToken)
    {
        return Task.Run(() => ReadNext(subscriber, cancellationToken), cancellationToken);
    }

    internal void Publish(JmapStatePublication publication)
    {
        ValidatePublication(publication);
        JmapStatePublication stored = ClonePublication(publication);
        lock (Subscribers)
        {
            History.Add(stored);
            while (History.Count > MaximumHistoryEvents)
            {
                History.RemoveAt(0);
            }

            StateChange? visible = FilterPublication(stored, null);
            if (visible != null && CurrentState == null)
            {
                CurrentState = CloneStateChange(visible);
            }
            else if (visible != null && CurrentState != null)
            {
                MergeStateChange(CurrentState, visible);
            }

            CurrentEventId = stored.EventId;
            foreach (EventSourceSubscriber subscriber in Subscribers.Values)
            {
                StateChange? filtered = FilterPublication(stored, subscriber.Options.Types);
                if (filtered != null)
                {
                    QueueState(subscriber, filtered, stored.EventId == null ? "" : stored.EventId);
                }
            }
        }
    }

    internal void EmitDuePings()
    {
        DateTimeOffset now = CurrentTime();
        lock (Subscribers)
        {
            foreach (EventSourceSubscriber subscriber in Subscribers.Values)
            {
                long interval = subscriber.Options.PingIntervalSeconds;
                if (interval > 0 && subscriber.LastActivityUtc.AddSeconds(interval) <= now)
                {
                    QueuePing(subscriber, interval, now);
                }
            }
        }
    }

    internal void CloseSubscriber(EventSourceSubscriber subscriber)
    {
        if (subscriber == null)
        {
            throw new ArgumentNullException("subscriber");
        }

        lock (subscriber.Buffer)
        {
            subscriber.Complete = true;
            subscriber.Signal.Release();
        }

        RemoveSubscriber(subscriber, true);
    }

    internal int GetSubscriberCount()
    {
        lock (Subscribers)
        {
            return Subscribers.Count;
        }
    }

    internal int GetBufferedEventCount(EventSourceSubscriber subscriber)
    {
        if (subscriber == null)
        {
            throw new ArgumentNullException("subscriber");
        }

        lock (subscriber.Buffer)
        {
            return subscriber.Buffer.Count;
        }
    }

    internal EventSourceOptions NormalizeOptions(EventSourceOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (options.PingIntervalSeconds < 0)
        {
            throw new ArgumentException("The EventSource ping interval cannot be negative.", "options");
        }

        List<string>? types = null;
        if (options.Types != null)
        {
            if (options.Types.Count == 1 && options.Types[0] == "*")
            {
                types = null;
            }
            else
            {
                if (options.Types.Count == 0 || options.Types.Contains("*"))
                {
                    throw new ArgumentException("EventSource types must be a non-empty list or a single wildcard.", "options");
                }

                List<string> invalid = new List<string>();
                Validator.ValidateTypes(options.Types, invalid);
                if (invalid.Count > 0)
                {
                    throw new ArgumentException("EventSource types must be valid and unique.", "options");
                }

                types = new List<string>(options.Types);
            }
        }

        if (options.LastEventId != null && options.LastEventId.Length > 0 && !IsSafeEventId(options.LastEventId))
        {
            throw new ArgumentException("The last EventSource event identifier is invalid.", "options");
        }

        EventSourceOptions normalized = new EventSourceOptions();
        normalized.Types = types;
        normalized.CloseAfterState = options.CloseAfterState;
        normalized.PingIntervalSeconds = ClampPingInterval(options.PingIntervalSeconds);
        normalized.LastEventId = options.LastEventId == null || options.LastEventId.Length == 0 ? null : options.LastEventId;
        return normalized;
    }

    internal long ClampPingInterval(long requested)
    {
        if (requested == 0)
        {
            return 0;
        }

        int minimum = Options.MinimumEventSourcePingSeconds;
        if (minimum < 0)
        {
            minimum = 0;
        }

        if (minimum > JmapProtocolLimits.MaximumEventSourceMinimumPingIntervalSeconds)
        {
            minimum = JmapProtocolLimits.MaximumEventSourceMinimumPingIntervalSeconds;
        }

        int maximum = Options.MaximumEventSourcePingSeconds;
        if (maximum < JmapProtocolLimits.MinimumEventSourceMaximumPingIntervalSeconds)
        {
            maximum = JmapProtocolLimits.MinimumEventSourceMaximumPingIntervalSeconds;
        }

        if (maximum < minimum)
        {
            maximum = minimum;
        }

        if (requested < minimum)
        {
            return minimum;
        }

        if (requested > maximum)
        {
            return maximum;
        }

        return requested;
    }

    internal void Replay(EventSourceSubscriber subscriber)
    {
        if (CurrentState == null || CurrentEventId == null)
        {
            return;
        }

        if (subscriber.Options.LastEventId == null)
        {
            return;
        }

        int lastIndex = -1;
        int index = 0;
        while (index < History.Count)
        {
            if (History[index].EventId == subscriber.Options.LastEventId)
            {
                lastIndex = index;
            }

            index = index + 1;
        }

        if (lastIndex == History.Count - 1)
        {
            return;
        }

        if (lastIndex < 0)
        {
            StateChange? current = Validator.FilterStateChange(CurrentState, subscriber.Options.Types);
            if (current != null)
            {
                QueueState(subscriber, current, CurrentEventId);
            }

            return;
        }

        StateChange? missed = null;
        int replayIndex = lastIndex + 1;
        while (replayIndex < History.Count)
        {
            JmapStatePublication publication = History[replayIndex];
            StateChange? filtered = FilterPublication(publication, subscriber.Options.Types);
            if (filtered != null)
            {
                if (missed == null)
                {
                    missed = CloneStateChange(filtered);
                }
                else
                {
                    MergeStateChange(missed, filtered);
                }
            }

            replayIndex = replayIndex + 1;
        }

        if (missed != null)
        {
            QueueState(subscriber, missed, CurrentEventId);
        }
    }

    internal void QueueState(EventSourceSubscriber subscriber, StateChange stateChange, string eventId)
    {
        JmapEvent value = new JmapEvent();
        value.Kind = JmapEventKind.State;
        value.Name = "state";
        value.Id = eventId;
        value.StateChange = CloneStateChange(stateChange);
        lock (subscriber.Buffer)
        {
            if (subscriber.Complete)
            {
                return;
            }

            int stateIndex = FindBufferedState(subscriber);
            if (stateIndex >= 0)
            {
                JmapEvent pending = subscriber.Buffer[stateIndex];
                if (pending.StateChange == null)
                {
                    pending.StateChange = CloneStateChange(stateChange);
                }
                else
                {
                    MergeStateChange(pending.StateChange, stateChange);
                }

                pending.Id = eventId;
            }
            else
            {
                if (subscriber.Buffer.Count >= MaximumBufferedEvents)
                {
                    subscriber.Buffer.RemoveAt(0);
                }

                subscriber.Buffer.Add(value);
                subscriber.Signal.Release();
            }

            if (subscriber.Options.CloseAfterState)
            {
                subscriber.Complete = true;
                subscriber.Signal.Release();
            }
        }
    }

    internal void QueuePing(EventSourceSubscriber subscriber, long interval, DateTimeOffset now)
    {
        lock (subscriber.Buffer)
        {
            if (subscriber.Complete)
            {
                return;
            }

            if (subscriber.Buffer.Count >= MaximumBufferedEvents)
            {
                return;
            }

            subscriber.LastActivityUtc = now;
            JmapEvent ping = new JmapEvent();
            ping.Kind = JmapEventKind.Ping;
            ping.Name = "ping";
            ping.PingIntervalSeconds = interval;
            ping.HasPingIntervalSeconds = true;
            subscriber.Buffer.Add(ping);
            subscriber.Signal.Release();
        }
    }

    internal int FindBufferedState(EventSourceSubscriber subscriber)
    {
        int index = subscriber.Buffer.Count - 1;
        while (index >= 0)
        {
            if (subscriber.Buffer[index].Kind == JmapEventKind.State)
            {
                return index;
            }

            index = index - 1;
        }

        return -1;
    }

    internal StateChange? FilterPublication(JmapStatePublication publication, List<string>? subscriberTypes)
    {
        StateChange filtered = new StateChange();
        filtered.Type = publication.StateChange.Type;
        foreach (KeyValuePair<string, JsonElement> unknown in publication.StateChange.UnknownProperties)
        {
            filtered.UnknownProperties.Add(unknown.Key, unknown.Value.Clone());
        }

        foreach (KeyValuePair<string, Dictionary<string, string>> account in publication.StateChange.Changed)
        {
            Dictionary<string, string> states = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> state in account.Value)
            {
                bool include = publication.HasNewEmailDelivery || state.Key != JmapDataTypes.EmailDelivery;
                if (include && publication.Types != null && !publication.Types.Contains(state.Key))
                {
                    include = false;
                }

                if (include && subscriberTypes != null && !subscriberTypes.Contains(state.Key))
                {
                    include = false;
                }

                if (include)
                {
                    states.Add(state.Key, state.Value);
                }
            }

            if (states.Count > 0)
            {
                filtered.Changed.Add(account.Key, states);
            }
        }

        return filtered.Changed.Count == 0 ? null : filtered;
    }

    internal void MergeStateChange(StateChange target, StateChange source)
    {
        target.Type = source.Type;
        foreach (KeyValuePair<string, JsonElement> unknown in source.UnknownProperties)
        {
            target.UnknownProperties[unknown.Key] = unknown.Value.Clone();
        }

        foreach (KeyValuePair<string, Dictionary<string, string>> account in source.Changed)
        {
            if (!target.Changed.ContainsKey(account.Key))
            {
                target.Changed.Add(account.Key, new Dictionary<string, string>(StringComparer.Ordinal));
            }

            foreach (KeyValuePair<string, string> state in account.Value)
            {
                target.Changed[account.Key][state.Key] = state.Value;
            }
        }
    }

    internal StateChange CloneStateChange(StateChange source)
    {
        StateChange clone = new StateChange();
        clone.Type = source.Type;
        foreach (KeyValuePair<string, Dictionary<string, string>> account in source.Changed)
        {
            clone.Changed.Add(account.Key, new Dictionary<string, string>(account.Value, StringComparer.Ordinal));
        }

        foreach (KeyValuePair<string, JsonElement> unknown in source.UnknownProperties)
        {
            clone.UnknownProperties.Add(unknown.Key, unknown.Value.Clone());
        }

        return clone;
    }

    internal JmapStatePublication ClonePublication(JmapStatePublication source)
    {
        JmapStatePublication clone = new JmapStatePublication();
        clone.EventId = source.EventId;
        clone.StateChange = CloneStateChange(source.StateChange);
        clone.HasNewEmailDelivery = source.HasNewEmailDelivery;
        if (source.Types != null)
        {
            clone.Types = new List<string>(source.Types);
        }

        return clone;
    }

    internal void ValidatePublication(JmapStatePublication publication)
    {
        if (publication == null || publication.EventId == null || !IsSafeEventId(publication.EventId) || publication.StateChange == null || publication.StateChange.Type != "StateChange")
        {
            throw new ArgumentException("A valid StateChange publication and event identifier are required.", "publication");
        }

        if (publication.Types != null)
        {
            List<string> invalid = new List<string>();
            Validator.ValidateTypes(publication.Types, invalid);
            if (invalid.Count > 0)
            {
                throw new ArgumentException("Publication types must be valid and unique.", "publication");
            }
        }

        int stateEntries = 0;
        long stateBytes = 0L;
        foreach (KeyValuePair<string, Dictionary<string, string>> account in publication.StateChange.Changed)
        {
            if (!Validator.Values.IsId(account.Key) || account.Value == null)
            {
                throw new ArgumentException("A StateChange account entry is invalid.", "publication");
            }

            stateBytes = stateBytes + System.Text.Encoding.UTF8.GetByteCount(account.Key);

            foreach (KeyValuePair<string, string> state in account.Value)
            {
                stateEntries = stateEntries + 1;
                if (stateEntries > SecurityLimits.MaximumEventSourceStateEntries ||
                    state.Key == null || state.Key.Length == 0 || state.Value == null ||
                    state.Value.Length == 0 ||
                    System.Text.Encoding.UTF8.GetByteCount(state.Key) > 255 ||
                    System.Text.Encoding.UTF8.GetByteCount(state.Value) >
                        SecurityLimits.MaximumEventSourceStateBytes ||
                    !Validator.Values.IsValidUnicode(state.Key) ||
                    !Validator.Values.IsValidUnicode(state.Value))
                {
                    throw new ArgumentException("A StateChange type state is invalid.", "publication");
                }

                stateBytes = stateBytes + System.Text.Encoding.UTF8.GetByteCount(state.Key) +
                    System.Text.Encoding.UTF8.GetByteCount(state.Value);
                if (stateBytes > SecurityLimits.MaximumEventSourcePublicationBytes)
                {
                    throw new ArgumentException("A StateChange exceeds the supported event size.", "publication");
                }
            }
        }
    }

    internal bool IsSafeEventId(string value)
    {
        if (value.Length == 0 || value.Length > EventSourceParser.MaximumLineCharacters || !Validator.Values.IsValidUnicode(value))
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\r' || character == '\n' || character == '\0')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void CancelSubscriber(string id)
    {
        EventSourceSubscriber? subscriber = null;
        lock (Subscribers)
        {
            if (Subscribers.ContainsKey(id))
            {
                subscriber = Subscribers[id];
                Subscribers.Remove(id);
                subscriber.Active = false;
            }
        }

        if (subscriber != null)
        {
            lock (subscriber.Buffer)
            {
                subscriber.Complete = true;
                subscriber.Signal.Release();
            }
        }
    }

    internal int MillisecondsUntilPing(EventSourceSubscriber subscriber)
    {
        lock (subscriber.Buffer)
        {
            if (subscriber.Options.PingIntervalSeconds == 0)
            {
                return Timeout.Infinite;
            }

            DateTimeOffset due = subscriber.LastActivityUtc.AddSeconds(subscriber.Options.PingIntervalSeconds);
            double milliseconds = (due - CurrentTime()).TotalMilliseconds;
            if (milliseconds <= 0)
            {
                return 0;
            }

            if (milliseconds >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)milliseconds + 1;
        }
    }

    internal void RemoveSubscriber(EventSourceSubscriber subscriber, bool disposeRegistration)
    {
        lock (Subscribers)
        {
            if (subscriber.Active)
            {
                Subscribers.Remove(subscriber.Id);
                subscriber.Active = false;
            }
        }

        if (disposeRegistration && subscriber.HasCancellationRegistration)
        {
            subscriber.CancellationRegistration.Dispose();
            subscriber.HasCancellationRegistration = false;
        }
    }

    internal DateTimeOffset CurrentTime()
    {
        return Clock == null ? DateTimeOffset.UtcNow : Clock.GetCurrentUtcDateTime();
    }
}
