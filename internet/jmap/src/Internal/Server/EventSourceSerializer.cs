using System;
using System.Globalization;
using System.Text;

namespace jmap;

internal class EventSourceSerializer
{
    internal BlobPushJsonCodec Codec;
    internal Encoding Utf8 = new UTF8Encoding(false, true);

    internal EventSourceSerializer()
    {
        Codec = new BlobPushJsonCodec(new JmapJsonReader(EventSourceParser.MaximumEventCharacters, 64));
    }

    internal byte[] Serialize(JmapEvent value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        StringBuilder result = new StringBuilder();
        if (value.Kind == JmapEventKind.Comment)
        {
            RequireBoundedText(value.Comment == null ? "" : value.Comment);
            AppendLines(result, ":", value.Comment == null ? "" : value.Comment);
            result.Append('\n');
            return EncodeBoundedEvent(result);
        }

        string? name = EventName(value);
        string? data = EventData(value);
        if (data == null)
        {
            throw new ArgumentException("EventSource event data is required.", "value");
        }

        RequireBoundedText(data);

        if (name != null)
        {
            if (name.Length == 0)
            {
                throw new ArgumentException("An EventSource event name cannot be empty.", "value");
            }

            RequireSafeField(name, "event name");
        }

        if (value.Kind != JmapEventKind.Ping && value.Id != null)
        {
            RequireSafeField(value.Id, "event identifier");
            result.Append("id: ");
            result.Append(value.Id);
            result.Append('\n');
        }

        if (value.HasRetryMilliseconds)
        {
            if (value.RetryMilliseconds < 0)
            {
                throw new ArgumentException("An EventSource retry interval cannot be negative.", "value");
            }

            result.Append("retry: ");
            result.Append(value.RetryMilliseconds.ToString(CultureInfo.InvariantCulture));
            result.Append('\n');
        }

        if (name != null)
        {
            result.Append("event: ");
            result.Append(name);
            result.Append('\n');
        }

        AppendLines(result, "data: ", data);
        result.Append('\n');
        return EncodeBoundedEvent(result);
    }

    internal string? EventName(JmapEvent value)
    {
        if (value.Name != null)
        {
            return value.Name;
        }

        if (value.Kind == JmapEventKind.State)
        {
            return "state";
        }

        if (value.Kind == JmapEventKind.Ping)
        {
            return "ping";
        }

        return null;
    }

    internal string? EventData(JmapEvent value)
    {
        if (value.Kind == JmapEventKind.State && value.StateChange != null)
        {
            return Codec.EncodeStateEventData(value.StateChange);
        }

        if (value.Kind == JmapEventKind.Ping && value.HasPingIntervalSeconds)
        {
            if (value.PingIntervalSeconds < 0)
            {
                throw new ArgumentException("An EventSource ping interval cannot be negative.", "value");
            }

            return Codec.EncodePingEventData(value.PingIntervalSeconds);
        }

        return value.Data;
    }

    internal void AppendLines(StringBuilder destination, string prefix, string value)
    {
        int start = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\r' || character == '\n')
            {
                destination.Append(prefix);
                destination.Append(value, start, index - start);
                destination.Append('\n');
                if (character == '\r' && index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index = index + 1;
                }

                start = index + 1;
            }

            index = index + 1;
        }

        destination.Append(prefix);
        destination.Append(value, start, value.Length - start);
        destination.Append('\n');
    }

    internal void RequireSafeField(string value, string description)
    {
        if (value.Length > EventSourceParser.MaximumLineCharacters)
        {
            throw new ArgumentException("The EventSource " + description + " exceeds the supported line length.", "value");
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\r' || character == '\n' || character == '\0')
            {
                throw new ArgumentException("The EventSource " + description + " contains a prohibited character.", "value");
            }

            index = index + 1;
        }
    }

    internal void RequireBoundedText(string value)
    {
        if (value.Length > EventSourceParser.MaximumEventCharacters)
        {
            throw new ArgumentException("The EventSource event exceeds the supported size.", "value");
        }
    }

    internal byte[] EncodeBoundedEvent(StringBuilder value)
    {
        byte[] encoded = Utf8.GetBytes(value.ToString());
        if (encoded.Length > EventSourceParser.MaximumEventCharacters)
        {
            throw new ArgumentException("The EventSource event exceeds the supported size.", "value");
        }

        return encoded;
    }
}
