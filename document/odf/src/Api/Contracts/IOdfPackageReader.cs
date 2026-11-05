using System.IO;

namespace odf;

/// <summary>
/// Reads and accesses editable OpenDocument packages.
/// </summary>
public interface IOdfPackageReader
{
    /// <summary>
    /// Reads a package from a file-system path.
    /// </summary>
    /// <param name="path">The path to read.</param>
    /// <param name="options">The load and security options.</param>
    /// <returns>The loaded package.</returns>
    public OdfPackage ReadPackagePath(string path, OdfLoadOptions options);
    /// <summary>
    /// Reads a package from a caller-supplied stream.
    /// </summary>
    /// <param name="input">The readable package stream.</param>
    /// <param name="options">The load, security, and stream-ownership options.</param>
    /// <returns>The loaded package.</returns>
    public OdfPackage ReadPackageStream(Stream input, OdfLoadOptions options);
    /// <summary>
    /// Reads a package from bytes.
    /// </summary>
    /// <param name="data">The complete package bytes.</param>
    /// <param name="options">The load and security options.</param>
    /// <returns>The loaded package.</returns>
    public OdfPackage ReadPackageBytes(byte[] data, OdfLoadOptions options);
    /// <summary>
    /// Opens an independent read-only snapshot of a package entry.
    /// </summary>
    /// <param name="package">The loaded package.</param>
    /// <param name="path">The canonical package entry path.</param>
    /// <returns>A caller-owned entry stream.</returns>
    public Stream OpenPackageEntry(OdfPackage package, string path);
    /// <summary>
    /// Extracts one package entry to an explicit destination.
    /// </summary>
    /// <param name="package">The loaded package.</param>
    /// <param name="path">The canonical package entry path.</param>
    /// <param name="destinationPath">The file-system destination.</param>
    /// <returns>The extraction result.</returns>
    public OdfEditResult ExtractPackageEntry(
        OdfPackage package,
        string path,
        string destinationPath);
}
