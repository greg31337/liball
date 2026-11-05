using System.IO;

namespace fix;

/// <summary>Writes deterministic Orchestra XML.</summary>
public interface IOrchestraWriter
{
    /// <summary>Writes one repository.</summary>
    /// <param name="destination">The writable XML stream.</param>
    /// <param name="repository">The repository to write.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The repository and ordered write diagnostics.</returns>
    OrchestraReadResult WriteRepository(Stream destination, OrchestraRepository repository, OrchestraWriteOptions options);
}
