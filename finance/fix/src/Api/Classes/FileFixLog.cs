using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace fix;

/// <summary>Appends human-readable structured FIX audit entries to a UTF-8 file.</summary>
public class FileFixLog : IFixLog
{
    internal static readonly object LogLock = new object();
    internal string LogPath;
    internal bool RedactSensitiveFields;

    /// <summary>Creates an append-only file log with configurable sensitive-field redaction.</summary>
    /// <param name="filePath">The audit log file path.</param>
    /// <param name="redactSensitiveFields">Whether standard credentials and protected data fields are redacted.</param>
    /// <exception cref="FixStoreException">Thrown when <c>filePath</c> is missing.</exception>
    public FileFixLog(string filePath, bool redactSensitiveFields)
    {
        if (filePath == null || filePath.Length == 0)
        {
            throw new FixStoreException("The FIX audit log path is missing.");
        }
        LogPath = Path.GetFullPath(filePath);
        RedactSensitiveFields = redactSensitiveFields;
        string? directory = Path.GetDirectoryName(LogPath);
        if (directory != null && directory.Length != 0)
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>Appends one escaped structured audit entry and flushes it to durable storage.</summary>
    /// <param name="entry">The structured entry.</param>
    public void WriteLog(LogEntry entry)
    {
        if (entry == null)
        {
            throw new FixStoreException("The FIX audit log entry is missing.");
        }
        byte[] bytes = CloneFixLogBytes(entry.Bytes);
        bool redacted = entry.IsRedacted;
        if (RedactSensitiveFields)
        {
            FixLogRedactionResult redaction = FixLogRedactor.RedactFixLogBytes(bytes);
            bytes = redaction.Bytes;
            redacted = redacted || redaction.WasRedacted;
        }
        string line = FormatFixLogLine(entry, bytes, redacted);
        lock (LogLock)
        {
            using (FileStream stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.WriteLine(line);
                writer.Flush();
                stream.Flush(true);
            }
        }
    }

    internal static string FormatFixLogLine(LogEntry entry, byte[] bytes, bool redacted)
    {
        StringBuilder line = new StringBuilder();
        line.Append(entry.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
        line.Append('\t');
        line.Append((int)entry.Direction);
        line.Append('\t');
        line.Append((int)entry.EventKind);
        line.Append('\t');
        line.Append(EscapeFixLogText(entry.Id.BeginString));
        line.Append('\t');
        line.Append(EscapeFixLogText(entry.Id.SenderCompID));
        line.Append('\t');
        line.Append(EscapeFixLogText(entry.Id.TargetCompID));
        line.Append('\t');
        line.Append(EscapeFixLogText(entry.Id.Qualifier));
        line.Append('\t');
        line.Append(redacted ? '1' : '0');
        line.Append('\t');
        line.Append(EscapeFixLogText(entry.Text));
        line.Append('\t');
        line.Append(EscapeFixLogBytes(bytes));
        return line.ToString();
    }

    internal static string EscapeFixLogText(string value)
    {
        if (value == null || value.Length == 0) return "";
        return EscapeFixLogBytes(Encoding.UTF8.GetBytes(value));
    }

    internal static string EscapeFixLogBytes(byte[] bytes)
    {
        StringBuilder text = new StringBuilder();
        for (int index = 0; index < bytes.Length; index++)
        {
            byte value = bytes[index];
            if (value == FixDefaults.Soh)
            {
                text.Append('|');
            }
            else if (value == 92)
            {
                text.Append("\\\\");
            }
            else if (value == 9)
            {
                text.Append("\\t");
            }
            else if (value == 10)
            {
                text.Append("\\n");
            }
            else if (value == 13)
            {
                text.Append("\\r");
            }
            else if (value >= 32 && value <= 126)
            {
                text.Append((char)value);
            }
            else
            {
                text.Append("\\x");
                text.Append(value.ToString("X2", CultureInfo.InvariantCulture));
            }
        }
        return text.ToString();
    }

    internal static byte[] CloneFixLogBytes(byte[] source)
    {
        if (source == null || source.Length == 0) return [];
        byte[] copy = new byte[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }
}
