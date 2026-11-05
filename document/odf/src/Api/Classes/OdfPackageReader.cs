using System.IO;

namespace odf;

/// <summary>
/// Provides package reading and entry access using the built-in OpenDocument package core.
/// </summary>
public class OdfPackageReader : IOdfPackageReader
{
    /// <summary>
    /// Initializes a package reader.
    /// </summary>
    public OdfPackageReader()
    {
    }

    /// <summary>
    /// Reads a package from a file-system path.
    /// </summary>
    /// <param name="path">The path to read.</param>
    /// <param name="options">The load and security options.</param>
    /// <returns>The loaded package.</returns>
    public OdfPackage ReadPackagePath(string path, OdfLoadOptions options)
    {
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfPackageException("Package reading was cancelled.");
        }
        byte[] data = File.ReadAllBytes(path);
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfPackageException("Package reading was cancelled.");
        }
        return CreateLoadedPackage(data, options, path);
    }

    /// <summary>
    /// Reads a package from a caller-supplied stream.
    /// </summary>
    /// <param name="input">The readable package stream.</param>
    /// <param name="options">The load, security, and stream-ownership options.</param>
    /// <returns>The loaded package.</returns>
    public OdfPackage ReadPackageStream(Stream input, OdfLoadOptions options)
    {
        if (options.LeaveInputOpen)
        {
            return ReadKeepingInputOpen(input, options);
        }
        using (input)
        {
            return ReadKeepingInputOpen(input, options);
        }
    }

    /// <summary>
    /// Reads a package from bytes.
    /// </summary>
    /// <param name="data">The complete package bytes.</param>
    /// <param name="options">The load and security options.</param>
    /// <returns>The loaded package.</returns>
    public OdfPackage ReadPackageBytes(byte[] data, OdfLoadOptions options)
    {
        return CreateLoadedPackage(data, options, "");
    }

    /// <summary>
    /// Opens an independent read-only snapshot of a package entry.
    /// </summary>
    /// <param name="package">The loaded package.</param>
    /// <param name="path">The canonical package entry path.</param>
    /// <returns>A caller-owned entry stream.</returns>
    public Stream OpenPackageEntry(OdfPackage package, string path)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        return index.OpenEntry(path);
    }

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
        string destinationPath)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        return index.ExtractEntry(path, destinationPath);
    }

    internal OdfPackage ReadKeepingInputOpen(
        Stream input,
        OdfLoadOptions options)
    {
        if (!input.CanRead)
        {
            throw new OdfPackageException("The package input stream is not readable.");
        }
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfPackageException("Package reading was cancelled.");
        }
        using (MemoryStream buffer = new MemoryStream())
        {
            input.CopyTo(buffer);
            if (options.Cancellation.IsCancellationRequested)
            {
                throw new OdfPackageException("Package reading was cancelled.");
            }
            return CreateLoadedPackage(buffer.ToArray(), options, "");
        }
    }

    internal OdfPackage CreateLoadedPackage(
        byte[] data,
        OdfLoadOptions options,
        string sourceName)
    {
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfPackageException("Package reading was cancelled.");
        }
        ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithSourceName(
            data,
            options.SecurityLimits,
            0,
            sourceName);
        ZipEntryIndex index = new ZipEntryIndex(data, inspection);
        return PackageApiModel.CreatePackage(inspection, index);
    }
}
