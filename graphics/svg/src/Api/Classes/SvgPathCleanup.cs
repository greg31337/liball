namespace svg;

/// <summary>Provides bounded exact cleanup for normalized path geometry.</summary>
public class SvgPathCleanup : ISvgPathCleanup
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPathCleanup()
    {
    }

    /// <summary>Removes and compacts exactly redundant path commands with deterministic defaults.</summary>
    /// <param name="path">The valid mutable path.</param>
    /// <returns>The absolute explicit cleaned path or a stable failure.</returns>
    public SvgPathCleanupResult CleanWithPath(SvgPathData path) { return new SvgPathCleanup().CleanWithPathAndOptions(path, new SvgPathCleanupOptions()); }

    /// <summary>Removes and compacts exactly redundant commands within an explicit limit.</summary>
    /// <param name="path">The valid mutable path.</param><param name="options">The bounded cleanup options.</param>
    /// <returns>The absolute explicit cleaned path or a stable failure.</returns>
    public SvgPathCleanupResult CleanWithPathAndOptions(SvgPathData? path, SvgPathCleanupOptions? options)
    {
        if (path == null || options == null || options.MaximumCommands < 1) return new SvgPathCleanup().Failure(SvgPathCleanupError.InvalidArgument);
        SvgPathNormalizeOptions normalize = new SvgPathNormalizeOptions(); normalize.MaximumCommands = options.MaximumCommands; SvgPathNormalizeResult normalized = path.NormalizeWithOptions(normalize); if (!normalized.Success) return new SvgPathCleanup().Failure(new SvgPathCleanup().Map(normalized.Error));
        return new SvgPathCleanupState(normalized.PathData, options).Run();
    }

    internal SvgPathCleanupResult Failure(SvgPathCleanupError error) { return new SvgPathCleanupResult(false, error, null, 0, 0, 0, 0); }
SvgPathCleanupError Map(SvgPathNormalizeError error) { if (error == SvgPathNormalizeError.InvalidPath) return SvgPathCleanupError.InvalidPath; if (error == SvgPathNormalizeError.CommandLimitExceeded) return SvgPathCleanupError.CommandLimitExceeded; if (error == SvgPathNormalizeError.NonFiniteResult) return SvgPathCleanupError.NonFiniteResult; return SvgPathCleanupError.InvalidArgument; }
}
