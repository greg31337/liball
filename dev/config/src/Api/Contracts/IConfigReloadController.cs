using System;

namespace config;

/// <summary>Defines manual and watched configuration reload operations.</summary>
public interface IConfigReloadController
{
    /// <summary>Gets a copy of the current snapshot.</summary>
    Config Current();
    /// <summary>Performs a reload and returns the active snapshot.</summary>
    Config Reload();
    /// <summary>Performs a reload or throws when the candidate cannot replace the active snapshot.</summary>
    Config ReloadOrThrow();
    /// <summary>Builds a transient fresh snapshot containing selected keys.</summary>
    Config FreshKeys(string[] paths);
    /// <summary>Builds a transient fresh snapshot from selected registered sources.</summary>
    Config FreshSources(int[] sourceIndexes);
    /// <summary>Gets the number of registered sources available for fresh selection.</summary>
    int SourceCount();
    /// <summary>Registers a successful-reload callback.</summary>
    ConfigReloadController OnChange(Action<Config> callback);
    /// <summary>Gets diagnostics from the last reload attempt.</summary>
    ConfigDiagnostics LastDiagnostics();
    /// <summary>Starts file watching.</summary>
    void Start();
    /// <summary>Stops file watching.</summary>
    void Stop();
    /// <summary>Checks whether file watching is active.</summary>
    bool IsWatching();
    /// <summary>Sets the file-watch debounce interval.</summary>
    ConfigReloadController SetDebounceMilliseconds(int milliseconds);
    /// <summary>Gets the number of observed reload events.</summary>
    int ReloadEventCount();
}
