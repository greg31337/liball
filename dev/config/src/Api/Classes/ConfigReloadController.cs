using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace config;

/// <summary>Maintains a configuration snapshot that can be rebuilt manually or after watched files change.</summary>
/// <remarks>A reload replaces the current snapshot and invokes callbacks only when the candidate has no diagnostic errors. Failed reload diagnostics remain available while the previous valid snapshot stays current.</remarks>
public class ConfigReloadController : IConfigReloadController
{
    internal List<IConfigSource> Sources;
    internal string Delimiter;
    internal ConfigCaseMode CaseMode;
    internal ConfigMergeMode MergeMode;
    internal ConfigArrayMergeMode ArrayMergeMode;
    internal ConfigNullMergeMode NullMergeMode;
    internal ConfigDuplicateKeyMode DuplicateKeyMode;
    internal ConfigDotEnvExpansionMode DotEnvExpansionMode;
    internal ConfigDotEnvMissingValueMode DotEnvMissingValueMode;
    internal ConfigSourceGroupLabels SourceGroupLabels;
    internal ConfigResolutionOptions ResolutionOptions;
    internal ConfigAliasTable Aliases;
    internal Config Snapshot;
    internal List<FileSystemWatcher> Watchers;
    internal bool Watching;
    internal int DebounceMilliseconds;
    internal Timer? DebounceTimer;
    internal object DebounceLock;
    internal int ReloadCount;
    internal ConfigDiagnostics LastReloadDiagnostics;
    internal List<Action<Config>> ChangeCallbacks;
    internal object SnapshotLock;

    internal ConfigReloadController(ConfigBuilder builder)
    {
        Sources = CopySources(builder.Sources);
        Delimiter = builder.Delimiter;
        CaseMode = builder.CaseMode;
        MergeMode = builder.MergeMode;
        ArrayMergeMode = builder.ArrayMergeMode;
        NullMergeMode = builder.NullMergeMode;
        DuplicateKeyMode = builder.DuplicateKeyMode;
        DotEnvExpansionMode = builder.DotEnvExpansionMode;
        DotEnvMissingValueMode = builder.DotEnvMissingValueMode;
        SourceGroupLabels = builder.SourceGroupLabels.Copy();
        ResolutionOptions = builder.ResolutionOptions.Copy();
        Aliases = builder.Aliases.Copy();
        SnapshotLock = new object();
        Snapshot = BuildSnapshot();
        Watchers = new List<FileSystemWatcher>();
        Watching = false;
        DebounceMilliseconds = 100;
        DebounceTimer = null;
        DebounceLock = new object();
        ReloadCount = 0;
        LastReloadDiagnostics = Snapshot.Diagnostics();
        ChangeCallbacks = new List<Action<Config>>();
    }

    /// <summary>Gets a copy of the current valid snapshot.</summary>
    /// <returns>An independent snapshot copy.</returns>
    public Config Current()
    {
        lock (SnapshotLock)
        {
            return CopyConfig(Snapshot);
        }
    }

    /// <summary>Reloads all registered sources immediately.</summary>
    /// <returns>A copy of the current snapshot, which remains the previous snapshot if the candidate has errors.</returns>
    public Config Reload()
    {
        return ReloadWithFailureMode(false);
    }

    /// <summary>Reloads all registered sources and throws when the candidate contains errors.</summary>
    /// <returns>A copy of the newly active snapshot.</returns>
    /// <exception cref="ConfigReloadException">Thrown when the candidate cannot replace the active snapshot.</exception>
    public Config ReloadOrThrow()
    {
        return ReloadWithFailureMode(true);
    }

    internal Config ReloadWithFailureMode(bool throwOnFailure)
    {
        bool replaced = false;
        Config current;
        lock (SnapshotLock)
        {
            Config candidate = BuildSnapshot();
            LastReloadDiagnostics = candidate.Diagnostics();
            if (ReloadCount < int.MaxValue)
            {
                ReloadCount = ReloadCount + 1;
            }

            if (!LastReloadDiagnostics.HasErrors())
            {
                Snapshot = candidate;
                replaced = true;
            }

            current = CopyConfig(Snapshot);

            if (throwOnFailure)
            {
                ConfigExceptionFactory.ThrowReloadFailure(LastReloadDiagnostics);
            }
        }

        if (replaced)
        {
            NotifyChangeCallbacks();
        }

        return current;
    }

    /// <summary>Reloads the complete source stack and returns a transient snapshot containing only selected paths.</summary>
    /// <remarks>The active controller snapshot, callbacks, reload count, and last-reload diagnostics are not changed. An empty path selects the complete fresh root.</remarks>
    /// <param name="paths">Configuration paths to copy from the fresh candidate.</param>
    /// <returns>An independent transient snapshot with diagnostics from the fresh build.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path array or one of its entries is null.</exception>
    public Config FreshKeys(string[] paths)
    {
        if (paths == null)
        {
            throw new ArgumentNullException("paths");
        }

        int pathIndex = 0;
        while (pathIndex < paths.Length)
        {
            if (paths[pathIndex] == null)
            {
                throw new ArgumentNullException("paths");
            }

            pathIndex = pathIndex + 1;
        }

        lock (SnapshotLock)
        {
            Config candidate = BuildSnapshot();
            return SelectFreshPaths(candidate, paths);
        }
    }

    /// <summary>Reloads selected registered sources and returns their transient merged snapshot.</summary>
    /// <remarks>Indexes are zero-based. Sources are loaded and merged in their original registration order and retain their original precedence ranks. The active controller state is not changed.</remarks>
    /// <param name="sourceIndexes">Zero-based registered source indexes to load.</param>
    /// <returns>An independent transient snapshot containing only selected source data and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the index array is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an index is outside the registered source collection.</exception>
    public Config FreshSources(int[] sourceIndexes)
    {
        if (sourceIndexes == null)
        {
            throw new ArgumentNullException("sourceIndexes");
        }

        lock (SnapshotLock)
        {
            bool[] selected = SourceSelection(sourceIndexes);
            ConfigSourceResult[] results = LoadSelectedSources(selected);
            ConfigMergeResult merge = ConfigMerger.MergeWithOptions(results, MergeMode, ArrayMergeMode, NullMergeMode);
            Config config = new Config(merge.Root, merge.Diagnostics, Aliases, Delimiter, CaseMode, ResolutionOptions);
            config.AddResolutionDiagnostics();
            return config;
        }
    }

    /// <summary>Registers a callback invoked after each successful snapshot replacement.</summary>
    /// <param name="callback">The action that receives a copied current snapshot.</param>
    /// <returns>This controller for fluent calls.</returns>
    public ConfigReloadController OnChange(Action<Config> callback)
    {
        lock (SnapshotLock)
        {
            ChangeCallbacks.Add(callback);
        }

        return this;
    }

    internal int ChangeCallbackCount()
    {
        lock (SnapshotLock)
        {
            return ChangeCallbacks.Count;
        }
    }

    internal void NotifyChangeCallbacks()
    {
        Action<Config>[] callbacks;
        lock (SnapshotLock)
        {
            callbacks = new Action<Config>[ChangeCallbacks.Count];
            ChangeCallbacks.CopyTo(callbacks, 0);
        }

        int index = 0;
        while (index < callbacks.Length)
        {
            callbacks[index](Current());
            index = index + 1;
        }
    }

    /// <summary>Gets diagnostics from the most recent initial build or reload attempt.</summary>
    /// <returns>An independent diagnostics copy.</returns>
    public ConfigDiagnostics LastDiagnostics()
    {
        lock (SnapshotLock)
        {
            return LastReloadDiagnostics.Copy();
        }
    }

    internal Config BuildSnapshot()
    {
        ConfigSourceResult[] results = LoadSources();
        ConfigMergeResult merge = ConfigMerger.MergeWithOptions(results, MergeMode, ArrayMergeMode, NullMergeMode);
        Config config = new Config(merge.Root, merge.Diagnostics, Aliases, Delimiter, CaseMode, ResolutionOptions);
        config.AddResolutionDiagnostics();
        return config;
    }

    internal ConfigSourceResult[] LoadSources()
    {
        ConfigParserOptions options = new ConfigParserOptions(DuplicateKeyMode, DotEnvExpansionMode, DotEnvMissingValueMode, ResolutionOptions.MissingIncludes, ResolutionOptions.IncludeDepthLimit);
        ConfigSourceResult[] results = ConfigSourceLoader.LoadAllWithParserOptions(Sources, options);
        ConfigBuilder.ApplySourceGroupLabels(results, Sources, SourceGroupLabels);
        return results;
    }

    /// <summary>Gets the number of registered sources available to <see cref="FreshSources"/>.</summary>
    /// <returns>The registered source count.</returns>
    public int SourceCount()
    {
        lock (SnapshotLock)
        {
            return Sources.Count;
        }
    }

    internal Config SelectFreshPaths(Config candidate, string[] paths)
    {
        ConfigMap selected = ConfigMap.Create(candidate.CaseMode);

        int index = 0;
        while (index < paths.Length)
        {
            string normalized = ConfigPath.Normalize(paths[index], candidate.Delimiter);
            if (normalized.Length == 0)
            {
                return CopyConfig(candidate);
            }

            string resolved = candidate.Aliases.Resolve(normalized, candidate.Delimiter);
            ConfigValue value = candidate.RawValue(resolved);
            if (value.Kind != ConfigValueKind.Missing)
            {
                selected.SetPath(resolved, value, candidate.Delimiter);
            }

            index = index + 1;
        }

        ConfigValue root = ConfigValue.Object(selected);
        candidate.Root.CopyMetadataTo(root);
        return new Config(root, candidate.DiagnosticData, candidate.Aliases, candidate.Delimiter, candidate.CaseMode, candidate.ResolutionOptions);
    }

    internal bool[] SourceSelection(int[] sourceIndexes)
    {
        bool[] selected = new bool[Sources.Count];

        int index = 0;
        while (index < sourceIndexes.Length)
        {
            int sourceIndex = sourceIndexes[index];
            if (sourceIndex < 0 || sourceIndex >= Sources.Count)
            {
                throw new ArgumentOutOfRangeException("sourceIndexes");
            }

            selected[sourceIndex] = true;
            index = index + 1;
        }

        return selected;
    }

    internal ConfigSourceResult[] LoadSelectedSources(bool[] selected)
    {
        int selectedCount = 0;
        int index = 0;
        while (index < selected.Length)
        {
            if (selected[index])
            {
                selectedCount = selectedCount + 1;
            }

            index = index + 1;
        }

        ConfigSourceResult[] results = new ConfigSourceResult[selectedCount];
        ConfigParserOptions options = new ConfigParserOptions(DuplicateKeyMode, DotEnvExpansionMode, DotEnvMissingValueMode, ResolutionOptions.MissingIncludes, ResolutionOptions.IncludeDepthLimit);
        int resultIndex = 0;
        index = 0;
        while (index < selected.Length)
        {
            if (selected[index])
            {
                ConfigSourceResult result = ConfigSourceLoader.LoadWithParserOptions(Sources[index], index + 1, options);
                ConfigSource? source = Sources[index] as ConfigSource;
                if (source != null)
                {
                    result.SourceGroup = ConfigBuilder.SourceGroupForKind(source.Kind, SourceGroupLabels);
                }

                results[resultIndex] = result;
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return results;
    }

    /// <summary>Starts file watchers for sources registered with <see cref="ConfigReload.OnChange"/>.</summary>
    /// <remarks>Calling this method while watching is already active has no effect.</remarks>
    public void Start()
    {
        if (Watching)
        {
            return;
        }

        int index = 0;
        while (index < Sources.Count)
        {
            ConfigSource? source = Sources[index] as ConfigSource;
            if (source != null && ShouldWatchSource(source))
            {
                Watchers.Add(CreateWatcher(source));
            }

            index = index + 1;
        }

        Watching = true;
    }

    /// <summary>Stops and disposes all file watchers and any pending debounce timer.</summary>
    /// <remarks>The controller can be started again after it is stopped.</remarks>
    public void Stop()
    {
        DisposeDebounceTimer();

        int index = 0;
        while (index < Watchers.Count)
        {
            Watchers[index].EnableRaisingEvents = false;
            Watchers[index].Dispose();
            index = index + 1;
        }

        Watchers.Clear();
        Watching = false;
    }

    /// <summary>Checks whether automatic file watching has been started.</summary>
    /// <returns>True after <see cref="Start"/> and before <see cref="Stop"/>.</returns>
    public bool IsWatching()
    {
        return Watching;
    }

    internal int WatcherCount()
    {
        return Watchers.Count;
    }

    /// <summary>Sets the quiet period used to combine rapid file-system events into one reload.</summary>
    /// <param name="milliseconds">The delay in milliseconds; zero or a negative value reloads immediately.</param>
    /// <returns>This controller for fluent calls.</returns>
    public ConfigReloadController SetDebounceMilliseconds(int milliseconds)
    {
        DebounceMilliseconds = milliseconds;
        return this;
    }

    /// <summary>Gets the number of manual or watcher-triggered reload attempts.</summary>
    /// <returns>The reload-attempt count, including failed candidates.</returns>
    public int ReloadEventCount()
    {
        lock (SnapshotLock)
        {
            return ReloadCount;
        }
    }

    internal bool ShouldWatchSource(ConfigSource source)
    {
        if (source.Kind != ConfigSourceKind.File)
        {
            return false;
        }

        return source.Reload == ConfigReload.OnChange;
    }

    internal FileSystemWatcher CreateWatcher(ConfigSource source)
    {
        string fullPath = Path.GetFullPath(source.Path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (directory == null)
        {
            directory = Directory.GetCurrentDirectory();
        }

        if (directory.Length == 0)
        {
            directory = Directory.GetCurrentDirectory();
        }

        FileSystemWatcher watcher = new FileSystemWatcher(directory);
        watcher.Filter = Path.GetFileName(fullPath);
        watcher.IncludeSubdirectories = false;
        watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileRenamed;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    internal void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        ScheduleReload();
    }

    internal void OnFileRenamed(object sender, RenamedEventArgs args)
    {
        ScheduleReload();
    }

    internal void ScheduleReload()
    {
        if (DebounceMilliseconds <= 0)
        {
            Reload();
            return;
        }

        lock (DebounceLock)
        {
            if (DebounceTimer == null)
            {
                DebounceTimer = new Timer(OnDebounceTimer, null, DebounceMilliseconds, Timeout.Infinite);
            }
            else
            {
                DebounceTimer.Change(DebounceMilliseconds, Timeout.Infinite);
            }
        }
    }

    internal void OnDebounceTimer(object? state)
    {
        Reload();
    }

    internal void DisposeDebounceTimer()
    {
        lock (DebounceLock)
        {
            if (DebounceTimer != null)
            {
                DebounceTimer.Dispose();
                DebounceTimer = null;
            }
        }
    }

    internal static Config CopyConfig(Config config)
    {
        return new Config(config.Root, config.DiagnosticData, config.Aliases, config.Delimiter, config.CaseMode, config.ResolutionOptions);
    }

    internal static List<IConfigSource> CopySources(List<IConfigSource> sources)
    {
        List<IConfigSource> copy = new List<IConfigSource>();

        int index = 0;
        while (index < sources.Count)
        {
            ConfigSource? source = sources[index] as ConfigSource;
            if (source == null)
            {
                copy.Add(sources[index]);
            }
            else
            {
                copy.Add(source.Copy());
            }

            index = index + 1;
        }

        return copy;
    }
}
