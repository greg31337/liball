namespace yaml;

/// <summary>
/// Creates independent high-level YAML processor instances.
/// </summary>
public class YamlProcessorFactory : IYamlProcessorFactory
{
    /// <summary>
    /// Initializes a YAML processor factory.
    /// </summary>
    public YamlProcessorFactory()
    {
    }

    /// <summary>
    /// Creates a processor with an independent copy of the supplied settings.
    /// </summary>
    /// <param name="options">The settings to copy; pass null to use library defaults.</param>
    /// <returns>The configured YAML processor.</returns>
    public YamlProcessor CreateProcessor(YamlOptions? options)
    {
        YamlProcessor processor = new YamlProcessor();
        processor.Configure(options);
        return processor;
    }
}
