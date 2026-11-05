using System.IO;

namespace fix;

/// <summary>Writes deterministic Orchestra repository and interfaces XML.</summary>
public class OrchestraWriter : IOrchestraWriter
{
    /// <summary>Writes one repository or interfaces document.</summary>
    /// <param name="destination">The writable XML stream.</param>
    /// <param name="repository">The Orchestra content to write.</param>
    /// <param name="options">The output version, selection, ordering, and encoding options.</param>
    /// <returns>The source repository and ordered write diagnostics.</returns>
    public OrchestraReadResult WriteRepository(Stream destination, OrchestraRepository repository, OrchestraWriteOptions options)
    {
        return OrchestraXmlEmitter.WriteRepository(destination, repository, options);
    }
}
