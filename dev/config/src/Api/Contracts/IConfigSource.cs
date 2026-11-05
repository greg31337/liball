namespace config;

/// <summary>Loads one configuration value tree at a position in the source precedence order.</summary>
/// <remarks>Implementations can represent application-specific sources without changing the built-in parser or merge pipeline. Reload controllers invoke custom sources again during reload but do not automatically watch them for changes.</remarks>
public interface IConfigSource
{
    /// <summary>Loads the source for one configuration build.</summary>
    /// <param name="precedenceRank">The one-based position of this source in builder registration order.</param>
    /// <returns>The loaded root and any diagnostics produced while loading it.</returns>
    ConfigSourceResult Load(int precedenceRank);
}
