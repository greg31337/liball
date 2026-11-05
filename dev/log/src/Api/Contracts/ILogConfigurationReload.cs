using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Defines configuration-file reload and watcher lifecycle operations.
/// </summary>
public interface ILogConfigurationReload
{
    /// <summary>Returns the watched file path.</summary>
    string GetFilePath();
    /// <summary>Reports whether the watcher is active.</summary>
    bool GetIsWatching();
    /// <summary>Returns the successful reload count.</summary>
    int GetReloadCount();
    /// <summary>Returns the failure count.</summary>
    int GetFailureCount();
    /// <summary>Returns the most recent reload result.</summary>
    LogResult GetLastResult();
    /// <summary>Returns the most recent successful configuration.</summary>
    LogConfiguration GetLastConfiguration();
    /// <summary>Returns the most recent successful logger.</summary>
    Logger GetLastLogger();
    /// <summary>Supplies explicit environment values for reloads.</summary>
    LogConfigurationReload Environment(Dictionary<string, string> environment);
    /// <summary>Sets the successful configuration callback.</summary>
    LogConfigurationReload OnConfigurationReloaded(Action<LogConfiguration> action);
    /// <summary>Sets the successful logger callback.</summary>
    LogConfigurationReload OnLoggerReloaded(Action<Logger> action);
    /// <summary>Starts the watcher and performs an initial reload.</summary>
    LogResult Start();
    /// <summary>Stops the watcher.</summary>
    LogResult Stop();
    /// <summary>Stops and releases watcher resources.</summary>
    void Close();
    /// <summary>Releases watcher resources.</summary>
    void Dispose();
    /// <summary>Reloads the configuration immediately.</summary>
    LogResult Reload();
}
