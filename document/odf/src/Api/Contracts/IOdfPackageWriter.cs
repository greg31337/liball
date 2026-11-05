using System.IO;

namespace odf;

/// <summary>
/// Writes and edits OpenDocument packages.
/// </summary>
public interface IOdfPackageWriter
{
    /// <summary>
    /// Writes a package to a file-system path.
    /// </summary>
    /// <param name="package">The package to write.</param>
    /// <param name="path">The destination path.</param>
    /// <param name="options">The save options.</param>
    public void WritePackagePath(
        OdfPackage package,
        string path,
        OdfSaveOptions options);
    /// <summary>
    /// Writes a package to a caller-supplied stream.
    /// </summary>
    /// <param name="package">The package to write.</param>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="options">The save and stream-ownership options.</param>
    public void WritePackageStream(
        OdfPackage package,
        Stream output,
        OdfSaveOptions options);
    /// <summary>
    /// Writes a package to a new byte array.
    /// </summary>
    /// <param name="package">The package to write.</param>
    /// <param name="options">The save options.</param>
    /// <returns>The complete package bytes.</returns>
    public byte[] WritePackageBytes(OdfPackage package, OdfSaveOptions options);
    /// <summary>
    /// Adds a package entry.
    /// </summary>
    /// <param name="package">The package to edit.</param>
    /// <param name="path">The new canonical entry path.</param>
    /// <param name="data">The entry content.</param>
    /// <param name="compressionMethod">The entry compression method.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult AddPackageEntry(
        OdfPackage package,
        string path,
        byte[] data,
        OdfCompressionMethod compressionMethod);
    /// <summary>
    /// Replaces package entry content.
    /// </summary>
    /// <param name="package">The package to edit.</param>
    /// <param name="path">The existing entry path.</param>
    /// <param name="data">The replacement content.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult ReplacePackageEntry(
        OdfPackage package,
        string path,
        byte[] data);
    /// <summary>
    /// Renames a package entry within its current directory.
    /// </summary>
    /// <param name="package">The package to edit.</param>
    /// <param name="path">The existing entry path.</param>
    /// <param name="newName">The new single-segment name.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult RenamePackageEntry(
        OdfPackage package,
        string path,
        string newName);
    /// <summary>
    /// Moves a package entry to another canonical path.
    /// </summary>
    /// <param name="package">The package to edit.</param>
    /// <param name="path">The existing entry path.</param>
    /// <param name="destinationPath">The destination entry path.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult MovePackageEntry(
        OdfPackage package,
        string path,
        string destinationPath);
    /// <summary>
    /// Copies a package entry to another canonical path.
    /// </summary>
    /// <param name="package">The package to edit.</param>
    /// <param name="path">The source entry path.</param>
    /// <param name="destinationPath">The destination entry path.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult CopyPackageEntry(
        OdfPackage package,
        string path,
        string destinationPath);
    /// <summary>
    /// Removes a package entry.
    /// </summary>
    /// <param name="package">The package to edit.</param>
    /// <param name="path">The entry path to remove.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult RemovePackageEntry(OdfPackage package, string path);
}
