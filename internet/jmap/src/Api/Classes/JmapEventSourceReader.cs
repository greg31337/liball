using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Reads JMAP state, ping, comment, and extension events from a reconnecting EventSource stream.
/// </summary>
public class JmapEventSourceReader : IJmapEventSourceReader
{
    internal const long DefaultReconnectDelayMilliseconds = 3000;
    internal const int ReadBufferSize = 8192;
    internal string EventSourceUrl;
    internal IJmapTransport Transport;
    internal UriTemplateExpander Templates = new UriTemplateExpander();
    internal PushValidator Validator = new PushValidator();
    internal EventSourceStreamReader StreamReader = new EventSourceStreamReader();
    internal EventSourceOptions? ActiveOptions;
    internal string? ExpandedUrl;
    internal string? LastEventId;
    internal long ReconnectDelayMilliseconds = DefaultReconnectDelayMilliseconds;
    internal Stream? ActiveStream;
    internal EventSourceParser? Parser;
    internal Queue<JmapEvent> Pending = new Queue<JmapEvent>();
    internal CancellationTokenSource? ConnectionCancellation;
    internal object Gate = new object();
    internal bool Connected;
    internal bool ClosedByCaller;
    internal bool NeedsReconnect;

    /// <summary>Creates a reader for a Session EventSource URI template and transport.</summary>
    /// <param name="eventSourceUrl">The Session EventSource URI template.</param>
    /// <param name="transport">The transport used to open authenticated EventSource responses.</param>
    public JmapEventSourceReader(string eventSourceUrl, IJmapTransport transport)
    {
        if (eventSourceUrl == null)
        {
            throw new ArgumentNullException("eventSourceUrl");
        }

        if (transport == null)
        {
            throw new ArgumentNullException("transport");
        }

        EventSourceUrl = eventSourceUrl;
        Transport = transport;
    }

    /// <summary>Expands the configured URI template and opens an EventSource connection.</summary>
    /// <param name="options">The event filtering, close, ping, and resume options.</param>
    /// <param name="cancellationToken">A token that can cancel connection establishment.</param>
    /// <returns>A task representing connection establishment.</returns>
    public Task ConnectEventSourceAsync(EventSourceOptions options, CancellationToken cancellationToken)
    {
        return Task.Run(() => Connect(options, cancellationToken), cancellationToken);
    }

    /// <summary>Reads the next event, reconnecting after a clean remote close.</summary>
    /// <param name="cancellationToken">A token that can cancel the read or reconnection.</param>
    /// <returns>A task containing the next parsed event or comment.</returns>
    public Task<JmapEvent> ReadEventSourceAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => ReadEvent(cancellationToken), cancellationToken);
    }

    /// <summary>Cancels pending reads and closes the current EventSource connection.</summary>
    public void CloseEventSource()
    {
        lock (Gate)
        {
            ClosedByCaller = true;
            Connected = false;
            Pending.Clear();
            if (ConnectionCancellation != null)
            {
                ConnectionCancellation.Cancel();
                ConnectionCancellation.Dispose();
                ConnectionCancellation = null;
            }

            if (ActiveStream != null)
            {
                ActiveStream.Dispose();
                ActiveStream = null;
            }

            Parser = null;
            NeedsReconnect = false;
        }
    }

    internal void Connect(EventSourceOptions options, CancellationToken cancellationToken)
    {
        EventSourceOptions normalized = NormalizeOptions(options);
        string types = JoinTypes(normalized.Types);
        string closeAfter = normalized.CloseAfterState ? "state" : "no";
        string ping = normalized.PingIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        string expanded = Templates.ExpandEventSourceUrl(EventSourceUrl, types, closeAfter, ping);
        Stream stream = Transport.OpenTransportEventSourceAsync(
            expanded,
            normalized.LastEventId,
            cancellationToken).GetAwaiter().GetResult();
        if (stream == null || !stream.CanRead)
        {
            if (stream != null)
            {
                stream.Dispose();
            }

            throw new JmapProtocolException(
                "The EventSource transport returned an unreadable stream.",
                "eventSource",
                default(System.Text.Json.JsonElement));
        }

        lock (Gate)
        {
            DisposePreviousConnection();
            ActiveOptions = normalized;
            ExpandedUrl = expanded;
            LastEventId = normalized.LastEventId;
            ReconnectDelayMilliseconds = DefaultReconnectDelayMilliseconds;
            ActiveStream = stream;
            Parser = CreateParser();
            Pending.Clear();
            ConnectionCancellation = new CancellationTokenSource();
            ClosedByCaller = false;
            Connected = true;
            NeedsReconnect = false;
        }
    }

    internal JmapEvent ReadEvent(CancellationToken cancellationToken)
    {
        CancellationTokenSource? connectionCancellation;
        lock (Gate)
        {
            if (!Connected || ClosedByCaller || ConnectionCancellation == null)
            {
                throw new InvalidOperationException("The EventSource reader is not connected.");
            }

            connectionCancellation = ConnectionCancellation;
        }

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            connectionCancellation.Token);
        while (true)
        {
            JmapEvent? pending = TakePendingEvent();
            if (pending != null)
            {
                ApplyEventMetadata(pending);
                return pending;
            }

            if (MustReconnect())
            {
                Reconnect(linked.Token);
            }

            Stream input = RequireActiveStream();
            EventSourceParser parser = RequireParser();
            byte[] buffer = new byte[ReadBufferSize];
            string endpoint = ExpandedUrl == null ? EventSourceUrl : ExpandedUrl;
            int read = StreamReader.ReadStreamAsync(input, buffer, endpoint, linked.Token).GetAwaiter().GetResult();
            List<JmapEvent> parsed = parser.Append(buffer, 0, read, read == 0);
            CaptureParserMetadata(parser, read == 0);
            EnqueueParsedEvents(parsed);
            if (read == 0)
            {
                MarkConnectionEnded();
            }
        }
    }

    internal EventSourceOptions NormalizeOptions(EventSourceOptions options)
    {
        if (options == null)
        {
            throw Validation("EventSource options are required.", "options");
        }

        List<string>? types = null;
        if (options.Types != null)
        {
            if (options.Types.Count == 0 || options.Types.Contains("*"))
            {
                if (options.Types.Count != 1 || options.Types[0] != "*")
                {
                    throw Validation("EventSource types must be a non-empty list or a single wildcard.", "types");
                }
            }
            else
            {
                List<string> invalid = new List<string>();
                Validator.ValidateTypes(options.Types, invalid);
                int index = 0;
                while (index < options.Types.Count)
                {
                    if (options.Types[index].IndexOf(',') >= 0)
                    {
                        invalid.Add("types/" + index);
                    }

                    index = index + 1;
                }

                if (invalid.Count > 0)
                {
                    throw Validation("EventSource types must be valid and unique.", "types");
                }

                types = new List<string>(options.Types);
            }
        }

        if (!Validator.Values.IsUnsignedInt(options.PingIntervalSeconds))
        {
            throw Validation("The EventSource ping interval must be an UnsignedInt.", "pingIntervalSeconds");
        }

        if (!IsSafeEventId(options.LastEventId))
        {
            throw Validation("The EventSource last event identifier is invalid.", "lastEventId");
        }

        EventSourceOptions normalized = new EventSourceOptions();
        normalized.Types = types;
        normalized.CloseAfterState = options.CloseAfterState;
        normalized.PingIntervalSeconds = options.PingIntervalSeconds;
        normalized.LastEventId = options.LastEventId;
        return normalized;
    }

    internal string JoinTypes(List<string>? types)
    {
        if (types == null)
        {
            return "*";
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < types.Count)
        {
            if (index > 0)
            {
                result.Append(',');
            }

            result.Append(types[index]);
            index = index + 1;
        }

        return result.ToString();
    }

    internal bool IsSafeEventId(string? eventId)
    {
        if (eventId == null)
        {
            return true;
        }

        if (eventId.Length > EventSourceParser.MaximumLineCharacters || !Validator.Values.IsValidUnicode(eventId))
        {
            return false;
        }

        int index = 0;
        while (index < eventId.Length)
        {
            char character = eventId[index];
            if (character == '\r' || character == '\n' || character == '\0')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal EventSourceParser CreateParser()
    {
        EventSourceParser parser = new EventSourceParser();
        parser.LastEventId = LastEventId;
        return parser;
    }

    internal JmapEvent? TakePendingEvent()
    {
        lock (Gate)
        {
            if (Pending.Count == 0)
            {
                return null;
            }

            return Pending.Dequeue();
        }
    }

    internal void ApplyEventMetadata(JmapEvent value)
    {
        lock (Gate)
        {
            if (value.Id != null)
            {
                LastEventId = value.Id;
                if (ActiveOptions != null)
                {
                    ActiveOptions.LastEventId = value.Id;
                }
            }

            if (value.HasRetryMilliseconds)
            {
                ReconnectDelayMilliseconds = value.RetryMilliseconds;
            }
        }
    }

    internal void CaptureParserMetadata(EventSourceParser parser, bool endOfStream)
    {
        lock (Gate)
        {
            if (parser.HasReconnectionDelay)
            {
                ReconnectDelayMilliseconds = parser.ReconnectionDelayMilliseconds;
            }

            if (endOfStream && parser.LastEventId != null)
            {
                LastEventId = parser.LastEventId;
            }
        }
    }

    internal void EnqueueParsedEvents(List<JmapEvent> events)
    {
        lock (Gate)
        {
            foreach (JmapEvent value in events)
            {
                Pending.Enqueue(value);
            }
        }
    }

    internal Stream RequireActiveStream()
    {
        lock (Gate)
        {
            if (!Connected || ClosedByCaller || ActiveStream == null)
            {
                throw new InvalidOperationException("The EventSource reader is not connected.");
            }

            return ActiveStream;
        }
    }

    internal EventSourceParser RequireParser()
    {
        lock (Gate)
        {
            if (Parser == null)
            {
                throw new InvalidOperationException("The EventSource reader is not connected.");
            }

            return Parser;
        }
    }

    internal void Reconnect(CancellationToken cancellationToken)
    {
        long delay = ReconnectDelayMilliseconds;
        if (delay < 0)
        {
            delay = DefaultReconnectDelayMilliseconds;
        }

        WaitForReconnectDelay(delay, cancellationToken);
        string endpoint = ExpandedUrl == null ? EventSourceUrl : ExpandedUrl;
        Stream stream = Transport.OpenTransportEventSourceAsync(
            endpoint,
            LastEventId,
            cancellationToken).GetAwaiter().GetResult();
        if (stream == null || !stream.CanRead)
        {
            if (stream != null)
            {
                stream.Dispose();
            }

            throw new JmapProtocolException(
                "The EventSource transport returned an unreadable stream.",
                "eventSource",
                default(System.Text.Json.JsonElement));
        }

        lock (Gate)
        {
            if (cancellationToken.IsCancellationRequested || ClosedByCaller)
            {
                stream.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
                throw new InvalidOperationException("The EventSource reader was closed during reconnection.");
            }

            if (ActiveStream != null)
            {
                ActiveStream.Dispose();
            }

            ActiveStream = stream;
            Parser = CreateParser();
            Connected = true;
            NeedsReconnect = false;
        }
    }

    internal void WaitForReconnectDelay(long delay, CancellationToken cancellationToken)
    {
        long remaining = delay;
        while (remaining > 0)
        {
            int waitMilliseconds = remaining >= int.MaxValue ? int.MaxValue : (int)remaining;
            if (cancellationToken.WaitHandle.WaitOne(waitMilliseconds))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            remaining = remaining - waitMilliseconds;
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    internal bool MustReconnect()
    {
        lock (Gate)
        {
            return NeedsReconnect;
        }
    }

    internal void MarkConnectionEnded()
    {
        lock (Gate)
        {
            if (ActiveStream != null)
            {
                ActiveStream.Dispose();
                ActiveStream = null;
            }

            NeedsReconnect = true;
        }
    }

    internal void DisposePreviousConnection()
    {
        if (ConnectionCancellation != null)
        {
            ConnectionCancellation.Cancel();
            ConnectionCancellation.Dispose();
            ConnectionCancellation = null;
        }

        if (ActiveStream != null)
        {
            ActiveStream.Dispose();
            ActiveStream = null;
        }

        Connected = false;
        Parser = null;
        NeedsReconnect = false;
        Pending.Clear();
    }

    internal JmapValidationException Validation(string message, string path)
    {
        List<string> invalid = new List<string>();
        invalid.Add(path);
        return new JmapValidationException(message, invalid, default(System.Text.Json.JsonElement));
    }
}
