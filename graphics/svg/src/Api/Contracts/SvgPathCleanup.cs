namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPathCleanup"/>.</summary>
public interface ISvgPathCleanup
{
    /// <summary>Removes and compacts exactly redundant path commands with deterministic defaults.</summary>
    /// <param name="path">The valid mutable path.</param>
    /// <returns>The absolute explicit cleaned path or a stable failure.</returns>
    SvgPathCleanupResult CleanWithPath(SvgPathData path);
    /// <summary>Removes and compacts exactly redundant commands within an explicit limit.</summary>
    /// <param name="path">The valid mutable path.</param><param name="options">The bounded cleanup options.</param>
    /// <returns>The absolute explicit cleaned path or a stable failure.</returns>
    SvgPathCleanupResult CleanWithPathAndOptions(SvgPathData? path, SvgPathCleanupOptions? options);
}
