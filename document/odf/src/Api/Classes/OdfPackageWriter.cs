using System.IO;

namespace odf;

/// <summary>
/// Provides package writing and entry mutations using the built-in OpenDocument package core.
/// </summary>
public class OdfPackageWriter : IOdfPackageWriter
{
    /// <summary>
    /// Initializes a package writer.
    /// </summary>
    public OdfPackageWriter()
    {
    }

    /// <summary>
    /// Writes a package to a file-system path.
    /// </summary>
    /// <param name="package">The package to write.</param>
    /// <param name="path">The destination path.</param>
    /// <param name="options">The save options.</param>
    public void WritePackagePath(
        OdfPackage package,
        string path,
        OdfSaveOptions options)
    {
        byte[] data = WritePackageBytes(package, options);
        File.WriteAllBytes(path, data);
    }

    /// <summary>
    /// Writes a package to a caller-supplied stream.
    /// </summary>
    /// <param name="package">The package to write.</param>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="options">The save and stream-ownership options.</param>
    public void WritePackageStream(
        OdfPackage package,
        Stream output,
        OdfSaveOptions options)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        ZipPackageWriter.WriteStream(index, output, options);
    }

    /// <summary>
    /// Writes a package to a new byte array.
    /// </summary>
    /// <param name="package">The package to write.</param>
    /// <param name="options">The save options.</param>
    /// <returns>The complete package bytes.</returns>
    public byte[] WritePackageBytes(
        OdfPackage package,
        OdfSaveOptions options)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        return ZipPackageWriter.WriteBytes(index, options);
    }

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
        OdfCompressionMethod compressionMethod)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        OdfEditResult result = index.AddEntry(path, data, compressionMethod);
        RefreshAfterEdit(package, result);
        return result;
    }

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
        byte[] data)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        OdfEditResult result = index.ReplaceEntry(path, data);
        RefreshAfterEdit(package, result);
        return result;
    }

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
        string newName)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        OdfEditResult result = index.RenameEntry(path, newName);
        RefreshAfterEdit(package, result);
        return result;
    }

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
        string destinationPath)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        OdfEditResult result = index.MoveEntry(path, destinationPath);
        RefreshAfterEdit(package, result);
        return result;
    }

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
        string destinationPath)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        OdfEditResult result = index.CopyEntry(path, destinationPath);
        RefreshAfterEdit(package, result);
        return result;
    }

    /// <summary>
    /// Removes a package entry.
    /// </summary>
    /// <param name="package">The package to edit.</param>
    /// <param name="path">The entry path to remove.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult RemovePackageEntry(OdfPackage package, string path)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        OdfEditResult result = index.RemoveEntry(path);
        RefreshAfterEdit(package, result);
        return result;
    }

    internal void RefreshAfterEdit(
        OdfPackage package,
        OdfEditResult result)
    {
        if (result.Changed)
        {
            PackageApiModel.RefreshPackage(package);
        }
    }
}
