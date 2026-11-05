namespace yaml;

/// <summary>
/// Creates independently configured high-level YAML processors.
/// </summary>
public interface IYamlProcessorFactory
{
    /// <summary>
    /// Creates a processor with an independent copy of the supplied settings.
    /// </summary>
    /// <param name="options">The settings to copy; pass null to use library defaults.</param>
    /// <returns>The configured YAML processor.</returns>
    public YamlProcessor CreateProcessor(YamlOptions? options);
}
