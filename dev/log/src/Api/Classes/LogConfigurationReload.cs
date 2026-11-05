using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Watches a JSON configuration file and rebuilds its configuration and logger when the file changes.
/// </summary>
/// <remarks>
/// The watcher reloads synchronously when FileSystemWatcher reports a matching path. Reload failures are returned as LogResult values and recorded in the public counters.
/// </remarks>
public class LogConfigurationReload : ILogConfigurationReload
{
    /// <summary>
    /// The normalized path currently being watched.
    /// </summary>
    internal string FilePath = "";
    internal bool UseEnvironment;
    internal Dictionary<string, string> EnvironmentValues = new Dictionary<string, string>();
    internal System.IO.FileSystemWatcher? Watcher;
    /// <summary>
    /// Whether the file watcher is currently active.
    /// </summary>
    internal bool IsWatching;
    /// <summary>
    /// The number of successful reloads.
    /// </summary>
    internal int ReloadCount;
    /// <summary>
    /// The number of failed operations observed by this object.
    /// </summary>
    internal int FailureCount;
    /// <summary>
    /// The result produced by the most recent reload or watcher operation.
    /// </summary>
    internal LogResult LastResult = LogResult.Success();
    /// <summary>
    /// The configuration produced by the most recent successful reload.
    /// </summary>
    internal LogConfiguration LastConfiguration = LogConfiguration.Create();
    /// <summary>
    /// The logger produced by the most recent successful reload.
    /// </summary>
    internal Logger LastLogger = Logger.Root();
    internal Action<LogConfiguration>? ConfigurationReloadedAction;
    internal Action<Logger>? LoggerReloadedAction;
    internal object SyncRoot = new object();

    internal LogConfigurationReload(string filePath)
    {
        FilePath = LogResult.NormalizeText(filePath);
        UseEnvironment = false;
        EnvironmentValues = new Dictionary<string, string>();
        Watcher = null;
        IsWatching = false;
        ReloadCount = 0;
        FailureCount = 0;
        LastResult = LogResult.Success();
        LastConfiguration = LogConfiguration.Create();
        LastLogger = Logger.Root();
        ConfigurationReloadedAction = null;
        LoggerReloadedAction = null;
        SyncRoot = new object();
    }

    /// <summary>Returns the watched file path.</summary>
    public string GetFilePath()
    {
        return FilePath;
    }

    /// <summary>Reports whether the watcher is active.</summary>
    public bool GetIsWatching()
    {
        return IsWatching;
    }

    /// <summary>Returns the successful reload count.</summary>
    public int GetReloadCount()
    {
        return ReloadCount;
    }

    /// <summary>Returns the failure count.</summary>
    public int GetFailureCount()
    {
        return FailureCount;
    }

    /// <summary>Returns the most recent reload result.</summary>
    public LogResult GetLastResult()
    {
        return LastResult;
    }

    /// <summary>Returns the most recent successful configuration.</summary>
    public LogConfiguration GetLastConfiguration()
    {
        return LastConfiguration;
    }

    /// <summary>Returns the most recent successful logger.</summary>
    public Logger GetLastLogger()
    {
        return LastLogger;
    }

    /// <summary>
    /// Creates a watcher for the supplied JSON configuration file.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The configured file watcher.</returns>
    internal static LogConfigurationReload WatchJsonFile(string filePath)
    {
        return new LogConfigurationReload(filePath);
    }

    /// <summary>
    /// Creates a watcher for the supplied JSON configuration file.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The configured file watcher.</returns>
    internal static LogConfigurationReload WatchJsonFileWithEnvironment(string filePath)
    {
        LogConfigurationReload reload = new LogConfigurationReload(filePath);
        reload.UseEnvironment = true;
        return reload;
    }

    /// <summary>
    /// Supplies explicit environment values to apply after each JSON reload.
    /// </summary>
    /// <param name="environment">The environment values to apply instead of reading the current process.</param>
    /// <returns>The configured file watcher.</returns>
    public LogConfigurationReload Environment(Dictionary<string, string> environment)
    {
        if (environment == null)
        {
            EnvironmentValues = new Dictionary<string, string>();
        }
        else
        {
            EnvironmentValues = LogConfigurationReload.CopyEnvironment(environment);
        }

        UseEnvironment = true;
        return this;
    }

    /// <summary>
    /// Sets the callback invoked after a successful configuration reloaded.
    /// </summary>
    /// <param name="action">The callback invoked after a successful reload.</param>
    /// <returns>The configured file watcher.</returns>
    public LogConfigurationReload OnConfigurationReloaded(Action<LogConfiguration> action)
    {
        ConfigurationReloadedAction = action;
        return this;
    }

    /// <summary>
    /// Sets the callback invoked after a successful logger reloaded.
    /// </summary>
    /// <param name="action">The callback invoked after a successful reload.</param>
    /// <returns>The configured file watcher.</returns>
    public LogConfigurationReload OnLoggerReloaded(Action<Logger> action)
    {
        LoggerReloadedAction = action;
        return this;
    }

    /// <summary>
    /// Starts watching the configured file and performs an initial reload.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Start()
    {
        if (FilePath.Length == 0)
        {
            LastResult = LogResult.FailureCode("configuration.reload.path.missing", "configuration file path cannot be empty");
            FailureCount = LogResult.IncrementCount(FailureCount);
            return LastResult;
        }

        Stop();
        string directory = LogConfigurationReload.DirectoryName(FilePath);
        string fileName = System.IO.Path.GetFileName(FilePath);
        Watcher = new System.IO.FileSystemWatcher(directory, fileName);
        Watcher.NotifyFilter = System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.Size;
        Watcher.Changed += OnChanged;
        Watcher.Created += OnChanged;
        Watcher.Renamed += OnRenamed;
        Watcher.EnableRaisingEvents = true;
        IsWatching = true;
        return Reload();
    }

    /// <summary>
    /// Stops and disposes the active file watcher.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Stop()
    {
        if (Watcher != null)
        {
            Watcher.EnableRaisingEvents = false;
            Watcher.Changed -= OnChanged;
            Watcher.Created -= OnChanged;
            Watcher.Renamed -= OnRenamed;
            Watcher.Dispose();
            Watcher = null;
        }

        IsWatching = false;
        LastResult = LogResult.Success();
        return LastResult;
    }

    /// <summary>
    /// Stops the watcher.
    /// </summary>
    public void Close()
    {
        Stop();
    }

    /// <summary>
    /// Stops the watcher.
    /// </summary>
    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Reloads the file immediately and invokes configured callbacks after success.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Reload()
    {
        lock (SyncRoot)
        {
            if (FilePath.Length == 0)
            {
                LastResult = LogResult.FailureCode("configuration.reload.path.missing", "configuration file path cannot be empty");
                FailureCount = LogResult.IncrementCount(FailureCount);
                return LastResult;
            }

            LogConfiguration configuration = LogJsonConfigurationLoader.LoadFile(FilePath);
            if (UseEnvironment)
            {
                if (EnvironmentValues != null && EnvironmentValues.Count > 0)
                {
                    LogEnvironmentConfiguration.Apply(configuration, EnvironmentValues);
                }
                else
                {
                    LogEnvironmentConfiguration.Apply(configuration);
                }
            }

            Logger logger = configuration.CreateLogger();
            LastConfiguration = configuration;
            LastLogger = logger;
            ReloadCount = LogResult.IncrementCount(ReloadCount);
            LastResult = configuration.ValidateResult();
            NotifyReloaded(configuration, logger);
            return LastResult;
        }
    }

    internal void OnChanged(object sender, System.IO.FileSystemEventArgs args)
    {
        if (args == null)
        {
            return;
        }

        if (!MatchesPath(args.FullPath))
        {
            return;
        }

        Reload();
    }

    internal void OnRenamed(object sender, System.IO.RenamedEventArgs args)
    {
        if (args == null)
        {
            return;
        }

        if (!MatchesPath(args.FullPath))
        {
            return;
        }

        Reload();
    }

    internal bool MatchesPath(string path)
    {
        string left = System.IO.Path.GetFullPath(LogResult.NormalizeText(path));
        string right = System.IO.Path.GetFullPath(FilePath);
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal void NotifyReloaded(LogConfiguration configuration, Logger logger)
    {
        if (ConfigurationReloadedAction != null)
        {
            ConfigurationReloadedAction(configuration);
        }

        if (LoggerReloadedAction != null)
        {
            LoggerReloadedAction(logger);
        }
    }

    internal static string DirectoryName(string filePath)
    {
        string normalizedPath = LogResult.NormalizeText(filePath);
        string? directory = System.IO.Path.GetDirectoryName(normalizedPath);
        if (directory == null || directory.Length == 0)
        {
            return System.Environment.CurrentDirectory;
        }

        return directory;
    }

    internal static Dictionary<string, string> CopyEnvironment(Dictionary<string, string> environment)
    {
        Dictionary<string, string> copy = new Dictionary<string, string>();
        if (environment == null)
        {
            return copy;
        }

        foreach (KeyValuePair<string, string> entry in environment)
        {
            copy[entry.Key] = entry.Value;
        }

        return copy;
    }
}
