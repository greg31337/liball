namespace yaml;

/// <summary>
/// Provides shared stateless entry points for creating processors and accessing YAML services.
/// </summary>
public static class YamlApi
{
    /// <summary>
    /// Creates independently configured high-level processors.
    /// </summary>
    public static readonly IYamlProcessorFactory Processors = new YamlProcessorFactory();

    /// <summary>
    /// Exposes low-level scanning, parsing, composition, schema, and emission operations.
    /// </summary>
    public static readonly IYamlPipeline Pipeline = new YamlPipeline();

    /// <summary>
    /// Exposes YAML model construction, conversion, query, and transformation operations.
    /// </summary>
    public static readonly IYamlModel Model = new YamlModel();
}
