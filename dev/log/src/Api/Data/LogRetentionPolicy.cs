using System;

namespace log;

/// <summary>
/// Describes count-based and age-based cleanup for rolled log files.
/// </summary>
public class LogRetentionPolicy
{
    /// <summary>
    /// The maximum retained rolled-file count; zero disables count-based cleanup.
    /// </summary>
    public int MaximumFileCount;
    /// <summary>
    /// The maximum retained file age; zero disables age-based cleanup.
    /// </summary>
    public TimeSpan MaximumFileAge;

    /// <summary>
    /// Creates a policy with retention disabled.
    /// </summary>
    public LogRetentionPolicy()
    {
        MaximumFileCount = 0;
        MaximumFileAge = TimeSpan.Zero;
    }

    /// <summary>
    /// Creates a retention policy with its optional behavior disabled.
    /// </summary>
    /// <returns>The resulting log retention policy.</returns>
    internal static LogRetentionPolicy None()
    {
        return new LogRetentionPolicy();
    }

    /// <summary>
    /// Creates a retention policy using count.
    /// </summary>
    /// <param name="maximumFileCount">The maximum number of rolled files to retain.</param>
    /// <returns>The resulting log retention policy.</returns>
    internal static LogRetentionPolicy ByCount(int maximumFileCount)
    {
        LogRetentionPolicy policy = new LogRetentionPolicy();
        policy.MaximumFileCount = LogRetentionPolicy.NormalizeMaximumFileCount(maximumFileCount);
        policy.MaximumFileAge = TimeSpan.Zero;
        return policy;
    }

    /// <summary>
    /// Creates a retention policy using age.
    /// </summary>
    /// <param name="maximumFileAge">The maximum age of retained rolled files.</param>
    /// <returns>The resulting log retention policy.</returns>
    internal static LogRetentionPolicy ByAge(TimeSpan maximumFileAge)
    {
        LogRetentionPolicy policy = new LogRetentionPolicy();
        policy.MaximumFileCount = 0;
        policy.MaximumFileAge = LogRetentionPolicy.NormalizeMaximumAge(maximumFileAge);
        return policy;
    }

    /// <summary>
    /// Creates a retention policy using count and age.
    /// </summary>
    /// <param name="maximumFileCount">The maximum number of rolled files to retain.</param>
    /// <param name="maximumFileAge">The maximum age of retained rolled files.</param>
    /// <returns>The resulting log retention policy.</returns>
    internal static LogRetentionPolicy ByCountAndAge(int maximumFileCount, TimeSpan maximumFileAge)
    {
        LogRetentionPolicy policy = new LogRetentionPolicy();
        policy.MaximumFileCount = LogRetentionPolicy.NormalizeMaximumFileCount(maximumFileCount);
        policy.MaximumFileAge = LogRetentionPolicy.NormalizeMaximumAge(maximumFileAge);
        return policy;
    }

    internal static LogResult Apply(LogSink sink, DateTimeOffset now)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.retention.null", "retention sink cannot be null");
        }

        if (!sink.HasRetentionPolicy || sink.RetentionPolicy == null)
        {
            return LogResult.Success();
        }

        LogRetentionPolicy.DeleteExpiredFiles(sink, now.UtcDateTime);
        LogRetentionPolicy.DeleteExcessFiles(sink);
        return LogResult.Success();
    }

    internal static void DeleteExpiredFiles(LogSink sink, DateTime nowUtc)
    {
        if (sink == null || sink.RetentionPolicy == null)
        {
            return;
        }

        if (sink.RetentionPolicy.MaximumFileAge <= TimeSpan.Zero)
        {
            return;
        }

        string[] files = LogRetentionPolicy.CandidateFiles(sink);
        foreach (string file in files)
        {
            if (LogRetentionPolicy.IsActivePath(sink, file))
            {
                continue;
            }

            DateTime lastWriteUtc = System.IO.File.GetLastWriteTimeUtc(file);
            if (nowUtc - lastWriteUtc > sink.RetentionPolicy.MaximumFileAge)
            {
                System.IO.File.Delete(file);
            }
        }
    }

    internal static void DeleteExcessFiles(LogSink sink)
    {
        if (sink == null || sink.RetentionPolicy == null)
        {
            return;
        }

        int maximumFileCount = sink.RetentionPolicy.MaximumFileCount;
        if (maximumFileCount <= 0)
        {
            return;
        }

        string[] files = LogRetentionPolicy.CandidateFiles(sink);
        while (LogRetentionPolicy.CountFiles(files) > maximumFileCount)
        {
            string oldestFile = LogRetentionPolicy.OldestDeletableFile(sink, files);
            if (oldestFile.Length == 0)
            {
                return;
            }

            System.IO.File.Delete(oldestFile);
            files = LogRetentionPolicy.CandidateFiles(sink);
        }
    }

    internal static string[] CandidateFiles(LogSink sink)
    {
        if (sink == null || sink.FilePath.Length == 0)
        {
            return new string[0];
        }

        string directory = "";
        string? nullableDirectory = System.IO.Path.GetDirectoryName(sink.FilePath);
        if (nullableDirectory != null)
        {
            directory = nullableDirectory;
        }

        if (directory.Length == 0)
        {
            directory = ".";
        }

        if (!System.IO.Directory.Exists(directory))
        {
            return new string[0];
        }

        string[] files = System.IO.Directory.GetFiles(directory);
        int count = 0;
        foreach (string file in files)
        {
            if (LogRetentionPolicy.IsCandidate(sink.FilePath, file))
            {
                count = count + 1;
            }
        }

        string[] candidates = new string[count];
        int index = 0;
        foreach (string file in files)
        {
            if (LogRetentionPolicy.IsCandidate(sink.FilePath, file))
            {
                candidates[index] = file;
                index = index + 1;
            }
        }

        return candidates;
    }

    internal static bool IsCandidate(string basePath, string filePath)
    {
        string baseName = System.IO.Path.GetFileNameWithoutExtension(FileLogSink.NormalizePath(basePath));
        string extension = System.IO.Path.GetExtension(FileLogSink.NormalizePath(basePath));
        string fileName = System.IO.Path.GetFileNameWithoutExtension(FileLogSink.NormalizePath(filePath));
        string fileExtension = System.IO.Path.GetExtension(FileLogSink.NormalizePath(filePath));

        if (!string.Equals(extension, fileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(baseName, fileName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fileName.StartsWith(baseName + "-", StringComparison.OrdinalIgnoreCase);
    }

    internal static string OldestDeletableFile(LogSink sink, string[] files)
    {
        if (files == null)
        {
            return "";
        }

        string oldestFile = "";
        DateTime oldestTime = DateTime.MaxValue;
        foreach (string file in files)
        {
            if (LogRetentionPolicy.IsActivePath(sink, file))
            {
                continue;
            }

            DateTime lastWriteUtc = System.IO.File.GetLastWriteTimeUtc(file);
            if (oldestFile.Length == 0 || lastWriteUtc < oldestTime || (lastWriteUtc == oldestTime && string.CompareOrdinal(file, oldestFile) < 0))
            {
                oldestFile = file;
                oldestTime = lastWriteUtc;
            }
        }

        return oldestFile;
    }

    internal static bool IsActivePath(LogSink sink, string file)
    {
        if (sink == null)
        {
            return false;
        }

        string activePath = System.IO.Path.GetFullPath(FileLogSink.CurrentPath(sink));
        string candidatePath = System.IO.Path.GetFullPath(LogResult.NormalizeText(file));
        return string.Equals(activePath, candidatePath, StringComparison.OrdinalIgnoreCase);
    }

    internal static int CountFiles(string[] files)
    {
        if (files == null)
        {
            return 0;
        }

        return files.Length;
    }

    internal static int NormalizeMaximumFileCount(int maximumFileCount)
    {
        if (maximumFileCount < 0)
        {
            return 0;
        }

        return maximumFileCount;
    }

    internal static TimeSpan NormalizeMaximumAge(TimeSpan maximumAge)
    {
        if (maximumAge < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return maximumAge;
    }
}
