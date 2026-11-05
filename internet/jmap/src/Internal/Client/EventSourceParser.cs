using System;
using System.Collections.Generic;
using System.Text;

namespace jmap;

internal class EventSourceParser
{
    internal const int MaximumLineCharacters = 65536;
    internal const int MaximumEventCharacters = 1048576;
    internal Encoding Utf8 = new UTF8Encoding(false, true);
    internal Decoder Utf8Decoder;
    internal StringBuilder Line = new StringBuilder();
    internal List<string> DataLines = new List<string>();
    internal BlobPushJsonCodec Codec;
    internal bool AtStart = true;
    internal bool SawCarriageReturn;
    internal bool Completed;
    internal bool HasData;
    internal string? EventName;
    internal string? LastEventId;
    internal long EventCharacters;
    internal long ReconnectionDelayMilliseconds;
    internal bool HasReconnectionDelay;
    internal long PendingRetryMilliseconds;
    internal bool HasPendingRetry;

    internal EventSourceParser()
    {
        Utf8Decoder = Utf8.GetDecoder();
        Codec = new BlobPushJsonCodec(new JmapJsonReader(MaximumEventCharacters, 64));
    }

    internal List<JmapEvent> Append(byte[] bytes, int offset, int count, bool endOfStream)
    {
        if (Completed)
        {
            throw new InvalidOperationException("The EventSource parser is already complete.");
        }

        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (offset < 0 || count < 0 || offset > bytes.Length - count)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        List<JmapEvent> events = new List<JmapEvent>();
        int capacity = Utf8.GetMaxCharCount(count);
        char[] characters = new char[capacity];
        int written = Utf8Decoder.GetChars(bytes.AsSpan(offset, count), characters.AsSpan(), endOfStream);
        ConsumeCharacters(characters, written, events);
        if (endOfStream)
        {
            if (Line.Length > 0)
            {
                FinishLine(events);
            }

            ResetFrame();
            Completed = true;
        }

        return events;
    }

    internal void ConsumeCharacters(char[] characters, int count, List<JmapEvent> events)
    {
        int index = 0;
        while (index < count)
        {
            char character = characters[index];
            if (AtStart)
            {
                AtStart = false;
                if (character == '\ufeff')
                {
                    index = index + 1;
                    continue;
                }
            }

            ConsumeCharacter(character, events);
            index = index + 1;
        }
    }

    internal void ConsumeCharacter(char character, List<JmapEvent> events)
    {
        if (SawCarriageReturn)
        {
            SawCarriageReturn = false;
            if (character == '\n')
            {
                return;
            }
        }

        if (character == '\r')
        {
            FinishLine(events);
            SawCarriageReturn = true;
            return;
        }

        if (character == '\n')
        {
            FinishLine(events);
            return;
        }

        Line.Append(character);
        if (Line.Length > MaximumLineCharacters)
        {
            throw ProtocolFailure("An EventSource line exceeds the parser limit.");
        }
    }

    internal void FinishLine(List<JmapEvent> events)
    {
        string value = Line.ToString();
        Line.Clear();
        if (value.Length == 0)
        {
            Dispatch(events);
            return;
        }

        if (value[0] == ':')
        {
            string comment = value.Substring(1);
            if (comment.StartsWith(" ", StringComparison.Ordinal))
            {
                comment = comment.Substring(1);
            }

            events.Add(Codec.DecodeEventRecord(null, null, null, comment, 0, false));
            return;
        }

        ProcessField(value);
    }

    internal void ProcessField(string line)
    {
        int colon = line.IndexOf(':');
        string field = colon < 0 ? line : line.Substring(0, colon);
        string value = colon < 0 ? "" : line.Substring(colon + 1);
        if (value.StartsWith(" ", StringComparison.Ordinal))
        {
            value = value.Substring(1);
        }

        if (field == "event")
        {
            EventName = value;
            AddEventCharacters(value.Length);
        }
        else if (field == "data")
        {
            DataLines.Add(value);
            HasData = true;
            AddEventCharacters(value.Length + 1);
        }
        else if (field == "id")
        {
            if (!ContainsNull(value))
            {
                LastEventId = value;
            }
        }
        else if (field == "retry")
        {
            long retry = ParseRetry(value);
            if (retry >= 0)
            {
                ReconnectionDelayMilliseconds = retry;
                HasReconnectionDelay = true;
                PendingRetryMilliseconds = retry;
                HasPendingRetry = true;
            }
        }
    }

    internal void Dispatch(List<JmapEvent> events)
    {
        if (!HasData)
        {
            EventName = null;
            EventCharacters = 0;
            return;
        }

        string name = EventName == null || EventName.Length == 0 ? "message" : EventName;
        string data = JoinDataLines();
        JmapEvent parsed = Codec.DecodeEventRecord(name, LastEventId, data, null, PendingRetryMilliseconds, HasPendingRetry);
        events.Add(parsed);
        DataLines.Clear();
        HasData = false;
        EventName = null;
        EventCharacters = 0;
        HasPendingRetry = false;
        PendingRetryMilliseconds = 0;
    }

    internal void ResetFrame()
    {
        Line.Clear();
        DataLines.Clear();
        HasData = false;
        EventName = null;
        EventCharacters = 0;
        SawCarriageReturn = false;
        HasPendingRetry = false;
        PendingRetryMilliseconds = 0;
    }

    internal string JoinDataLines()
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < DataLines.Count)
        {
            if (index > 0)
            {
                result.Append('\n');
            }

            result.Append(DataLines[index]);
            index = index + 1;
        }

        return result.ToString();
    }

    internal long ParseRetry(string value)
    {
        if (value.Length == 0)
        {
            return -1;
        }

        long result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            int digit = character - '0';
            if (result > (long.MaxValue - digit) / 10)
            {
                return -1;
            }

            result = (result * 10) + digit;
            index = index + 1;
        }

        return result;
    }

    internal bool ContainsNull(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\0')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddEventCharacters(int count)
    {
        EventCharacters = EventCharacters + count;
        if (EventCharacters > MaximumEventCharacters)
        {
            throw ProtocolFailure("An EventSource event exceeds the parser limit.");
        }
    }

    internal JmapProtocolException ProtocolFailure(string message)
    {
        return new JmapProtocolException(message, "eventSource", default(System.Text.Json.JsonElement));
    }
}
