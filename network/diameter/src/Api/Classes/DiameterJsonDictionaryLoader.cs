namespace diameter;

/// <summary>
/// Loads versioned Diameter dictionary modules from JSON documents.
/// </summary>
public class DiameterJsonDictionaryLoader : IDiameterDictionaryLoader
{
    /// <summary>Initializes a JSON dictionary loader.</summary>
    public DiameterJsonDictionaryLoader()
    {
    }

    /// <summary>Loads and validates one JSON dictionary module.</summary>
    /// <param name="json">The versioned dictionary JSON document.</param>
    /// <returns>The loaded dictionary module.</returns>
    /// <exception cref="DiameterDictionaryException">The document is invalid.</exception>
    public DiameterDictionaryModule LoadDictionaryModule(string? json)
    {
        return JsonDictionaryLoader.LoadDictionaryModule(json);
    }
}
