using System.IO;

namespace iso20022;

/// <summary>Imports raw and archived ISO 20022 repositories from streams and files.</summary>
public interface IRepositoryImporter
{
    /// <summary>Imports one raw XML repository stream.</summary>
    /// <param name="input">The raw repository stream.</param><param name="options">The import options.</param><returns>The import result.</returns>
    RepositoryImportResult ImportRawStream(Stream? input, RepositoryImportOptions? options);
    /// <summary>Imports one ZIP repository stream.</summary>
    /// <param name="input">The ZIP artifact stream.</param><param name="options">The import options.</param><returns>The import result.</returns>
    RepositoryImportResult ImportZipStream(Stream? input, RepositoryImportOptions? options);
    /// <summary>Imports one raw XML repository file.</summary>
    /// <param name="path">The raw repository path.</param><param name="options">The import options.</param><returns>The import result.</returns>
    RepositoryImportResult ImportRawFile(string? path, RepositoryImportOptions? options);
    /// <summary>Imports one ZIP repository file.</summary>
    /// <param name="path">The ZIP artifact path.</param><param name="options">The import options.</param><returns>The import result.</returns>
    RepositoryImportResult ImportZipFile(string? path, RepositoryImportOptions? options);
}
