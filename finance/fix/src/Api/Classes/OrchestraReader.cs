using System.IO;

namespace fix;

/// <summary>Reads secure, bounded Orchestra repository and interfaces documents.</summary>
public class OrchestraReader : IOrchestraReader
{
    /// <summary>Reads one Orchestra repository or interfaces document.</summary>
    /// <param name="source">The readable XML stream.</param>
    /// <param name="options">The secure read and content-selection options.</param>
    /// <returns>The parsed repository and ordered load, schema, and semantic diagnostics.</returns>
    public OrchestraReadResult ReadRepository(Stream source, OrchestraReadOptions options)
    {
        return OrchestraReaderCore.ReadRepository(source, options);
    }
}
