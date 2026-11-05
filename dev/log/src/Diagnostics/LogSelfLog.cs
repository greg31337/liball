using System;
using System.Threading;

namespace log;

/// <summary>
/// Collects diagnostics produced by the logging library itself.
/// </summary>
/// <remarks>
/// The self-log guards against recursive writes. EnableMemory stores entries in process memory; Enable also forwards each entry to the supplied action.
/// </remarks>
internal class LogSelfLog
{
    internal static readonly LogSelfLogState State = new LogSelfLogState();

    /// <summary>
    /// Whether this logger or sink currently accepts eligible events.
    /// </summary>
    /// <returns>true when self-diagnostics are enabled; otherwise, false.</returns>
    internal static bool IsEnabled()
    {
        lock (State.SyncRoot)
        {
            return State.IsEnabled;
        }
    }

    /// <summary>
    /// The cumulative number of accepted write operations.
    /// </summary>
    /// <returns>The cumulative number of accepted write operations.</returns>
    internal static int WrittenCount()
    {
        lock (State.SyncRoot)
        {
            return State.WrittenCount;
        }
    }

    /// <summary>
    /// The number of self-log writes discarded to prevent recursion.
    /// </summary>
    /// <returns>The number of self-log writes discarded to prevent recursion.</returns>
    internal static int DroppedReentrantCount()
    {
        lock (State.SyncRoot)
        {
            return State.DroppedReentrantCount;
        }
    }

    /// <summary>
    /// Enables self-diagnostics and forwards each message to the supplied action.
    /// </summary>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    internal static void Enable(Action<string>? writeAction)
    {
        lock (State.SyncRoot)
        {
            State.Messages = new string[0];
            State.WrittenCount = 0;
            State.DroppedReentrantCount = 0;
            State.IsEnabled = true;
            State.WriteAction = writeAction;
        }
    }

    /// <summary>
    /// Enables self-diagnostics using only the in-memory entry list.
    /// </summary>
    internal static void EnableMemory()
    {
        Enable(null);
    }

    /// <summary>
    /// Disables self-diagnostics and removes the forwarding action.
    /// </summary>
    internal static void Disable()
    {
        lock (State.SyncRoot)
        {
            State.IsEnabled = false;
            State.IsWriting.Value = false;
            State.WriteAction = null;
        }
    }

    /// <summary>
    /// Clears stored messages and counters without changing whether self-logging is enabled.
    /// </summary>
    internal static void Clear()
    {
        lock (State.SyncRoot)
        {
            State.Messages = new string[0];
            State.WrittenCount = 0;
            State.DroppedReentrantCount = 0;
        }
    }

    /// <summary>
    /// Returns a defensive copy of the stored self-log messages.
    /// </summary>
    /// <returns>A defensive copy of the requested items.</returns>
    internal static string[] Entries()
    {
        lock (State.SyncRoot)
        {
            return LogSelfLog.CopyMessages(State.Messages);
        }
    }

    internal static LogResult Write(string message)
    {
        Action<string>? writeAction = null;
        string normalizedMessage = LogResult.NormalizeText(message);
        lock (State.SyncRoot)
        {
            if (!State.IsEnabled)
            {
                return LogResult.Success();
            }

            if (State.IsWriting.Value)
            {
                State.DroppedReentrantCount = LogResult.IncrementCount(State.DroppedReentrantCount);
                return LogResult.Dropped("self log reentrant write");
            }

            State.IsWriting.Value = true;
            State.Messages = LogSelfLog.AppendMessage(State.Messages, normalizedMessage);
            State.WrittenCount = LogResult.IncrementCount(State.WrittenCount);
            writeAction = State.WriteAction;
        }

        if (writeAction != null)
        {
            writeAction(normalizedMessage);
        }

        State.IsWriting.Value = false;
        return LogResult.Success();
    }

    internal static LogResult WriteResult(string source, LogResult result)
    {
        if (result == null)
        {
            return LogResult.Success();
        }

        if (!result.IsFailure && !result.IsDiagnostic)
        {
            return LogResult.Success();
        }

        return LogSelfLog.Write(LogSelfLog.FormatResult(source, result));
    }

    internal static string FormatResult(string source, LogResult result)
    {
        if (result == null)
        {
            return LogResult.NormalizeText(source);
        }

        string text = LogResult.NormalizeText(source);
        if (result.Code.Length > 0)
        {
            text = text + " " + result.Code;
        }

        if (result.Message.Length > 0)
        {
            text = text + " " + result.Message;
        }

        return text;
    }

    internal static string[] AppendMessage(string[] messages, string message)
    {
        string[] source = messages;
        if (source == null)
        {
            source = new string[0];
        }

        string[] copy = new string[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        copy[source.Length] = LogResult.NormalizeText(message);
        return copy;
    }

    internal static string[] CopyMessages(string[] messages)
    {
        if (messages == null)
        {
            return new string[0];
        }

        string[] copy = new string[messages.Length];
        int index = 0;
        while (index < messages.Length)
        {
            copy[index] = messages[index];
            index = index + 1;
        }

        return copy;
    }
}

internal class LogSelfLogState
{
    internal bool IsEnabled;
    internal AsyncLocal<bool> IsWriting = new AsyncLocal<bool>();
    internal Action<string>? WriteAction;
    internal string[] Messages = new string[0];
    internal int WrittenCount;
    internal int DroppedReentrantCount;
    internal object SyncRoot = new object();
}
