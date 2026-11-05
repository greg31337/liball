using System.IO;

namespace fix;

/// <summary>Reads secure, bounded Orchestra XML.</summary>
public interface IOrchestraReader
{
    /// <summary>Reads one repository or interface document.</summary>
    /// <param name="source">The readable XML stream.</param>
    /// <param name="options">The secure read options.</param>
    /// <returns>The repository and ordered diagnostics.</returns>
    OrchestraReadResult ReadRepository(Stream source, OrchestraReadOptions options);
}
