using System;
using System.IO;

namespace odf;

/// <summary>
/// Reads package and flat OpenDocument documents into one document aggregate.
/// </summary>
public class OdfDocumentReader : IOdfDocumentReader
{
    /// <summary>
    /// Initializes a document reader.
    /// </summary>
    public OdfDocumentReader()
    {
    }

    /// <summary>
    /// Reads a document from a file-system path.
    /// </summary>
    /// <param name="path">The path to read.</param>
    /// <param name="options">The load and stream-ownership options.</param>
    /// <returns>The loaded document.</returns>
    public OdfDocument ReadDocumentPath(string path, OdfLoadOptions options)
    {
        ValidateReadInputs(path, options);
        CheckCancellation(options);
        byte[] data = File.ReadAllBytes(path);
        CheckCancellation(options);
        return ReadDocumentBytes(data, path, options);
    }

    /// <summary>
    /// Reads a document from a caller-supplied stream.
    /// </summary>
    /// <param name="input">The readable document stream.</param>
    /// <param name="sourceName">An optional source name used for media-type inference.</param>
    /// <param name="options">The load and stream-ownership options.</param>
    /// <returns>The loaded document.</returns>
    public OdfDocument ReadDocumentStream(
        Stream input,
        string sourceName,
        OdfLoadOptions options)
    {
        ValidateReadInputs(sourceName, options);
        if (input == null || !input.CanRead)
        {
            throw new OdfDocumentException(
                "The document input stream is not readable.");
        }
        if (options.LeaveInputOpen)
        {
            return ReadOpenStream(input, sourceName, options);
        }
        using (input)
        {
            return ReadOpenStream(input, sourceName, options);
        }
    }

    /// <summary>
    /// Reads a document from complete input bytes.
    /// </summary>
    /// <param name="data">The complete package or flat XML bytes.</param>
    /// <param name="sourceName">An optional source name used for media-type inference.</param>
    /// <param name="options">The load options.</param>
    /// <returns>The loaded document.</returns>
    public OdfDocument ReadDocumentBytes(
        byte[] data,
        string sourceName,
        OdfLoadOptions options)
    {
        ValidateReadInputs(sourceName, options);
        if (data == null || data.Length == 0)
        {
            throw new OdfDocumentException("Document input bytes are required.");
        }
        CheckCancellation(options);
        if (IsZipPackage(data))
        {
            return ReadPackageDocument(data, sourceName, options);
        }
        return ReadFlatDocument(data, sourceName, options);
    }

    internal OdfDocument ReadOpenStream(
        Stream input,
        string sourceName,
        OdfLoadOptions options)
    {
        CheckCancellation(options);
        using (MemoryStream buffer = new MemoryStream())
        {
            input.CopyTo(buffer);
            CheckCancellation(options);
            return ReadDocumentBytes(buffer.ToArray(), sourceName, options);
        }
    }

    internal OdfDocument ReadPackageDocument(
        byte[] data,
        string sourceName,
        OdfLoadOptions options)
    {
        OdfPackageReader packageReader = new OdfPackageReader();
        OdfPackage package = packageReader.CreateLoadedPackage(
            data,
            options,
            sourceName);
        if (!options.MaterializeXmlTrees)
        {
            OdfDocument shallow = CreateShallowPackageDocument(
                package,
                data,
                options);
            return shallow;
        }

        byte[] contentData = ReadPackagePart(
            packageReader,
            package,
            OdfPartNames.Content,
            true,
            options);
        byte[] stylesData = ReadPackagePart(
            packageReader,
            package,
            OdfPartNames.Styles,
            false,
            options);
        byte[] metadataData = ReadPackagePart(
            packageReader,
            package,
            OdfPartNames.Metadata,
            false,
            options);
        byte[] settingsData = ReadPackagePart(
            packageReader,
            package,
            OdfPartNames.Settings,
            false,
            options);
        OdfDocument document = OdfDocumentAssembly.AssembleSeparateParts(
            ReadXmlPart(contentData, OdfPartNames.Content, options),
            ReadOptionalXmlPart(stylesData, OdfPartNames.Styles, options),
            ReadOptionalXmlPart(metadataData, OdfPartNames.Metadata, options),
            ReadOptionalXmlPart(settingsData, OdfPartNames.Settings, options));
        PopulatePackageState(document, package, data);
        OdfDocumentModel.ApplyContext(
            document,
            options,
            new OdfSaveOptions(),
            package.MediaType);
        RdfXmlDocumentModel.LoadRdfParts(document, options);
        CheckCancellation(options);
        return document;
    }

    internal OdfDocument ReadFlatDocument(
        byte[] data,
        string sourceName,
        OdfLoadOptions options)
    {
        OdfNode[] nodes = OdfTreeBuilder.ReadBytes(data, sourceName, options);
        OdfDocument document = OdfDocumentAssembly.AssembleFlatDocument(nodes);
        string mediaType = document.MediaType;
        if (mediaType.Length == 0)
        {
            mediaType = InferFlatMediaType(sourceName);
        }
        OdfDocumentModel.ApplyContext(
            document,
            options,
            new OdfSaveOptions(),
            mediaType);
        document.OriginalData = CopyBytes(data);
        if (!options.MaterializeXmlTrees)
        {
            document.Nodes = [];
            document.Parts = [];
            document.PartTrees = null;
            document.Body = null;
            document.PassthroughNodes = [];
        }
        CheckCancellation(options);
        return document;
    }

    internal OdfDocument CreateShallowPackageDocument(
        OdfPackage package,
        byte[] data,
        OdfLoadOptions options)
    {
        OdfDocument document = new OdfDocument();
        document.PackageKind = OdfPackageKind.Package;
        PopulatePackageState(document, package, data);
        OdfDocumentModel.ApplyContext(
            document,
            options,
            new OdfSaveOptions(),
            package.MediaType);
        document.Kind = package.DocumentKind;
        if (package.Manifest != null && package.Manifest.Version.Length > 0)
        {
            document.VersionInfo = OdfVersionRules.ParseVersion(
                package.Manifest.Version);
            document.IsFutureVersion = document.VersionInfo.IsFuture;
        }
        return document;
    }

    internal void PopulatePackageState(
        OdfDocument document,
        OdfPackage package,
        byte[] data)
    {
        document.Package = package;
        document.PackageEntries = package.Entries;
        document.Manifest = package.Manifest;
        document.Diagnostics = package.Diagnostics;
        document.OriginalData = CopyBytes(data);
    }

    internal byte[] ReadPackagePart(
        OdfPackageReader reader,
        OdfPackage package,
        string path,
        bool required,
        OdfLoadOptions options)
    {
        bool found = false;
        foreach (OdfPackageEntry entry in package.Entries)
        {
            if (entry.Path == path && !entry.IsDirectory)
            {
                found = true;
            }
        }
        if (!found)
        {
            if (required)
            {
                throw new OdfDocumentException(
                    "The OpenDocument package does not contain content.xml.");
            }
            return [];
        }
        CheckCancellation(options);
        using (Stream input = reader.OpenPackageEntry(package, path))
        using (MemoryStream buffer = new MemoryStream())
        {
            input.CopyTo(buffer);
            CheckCancellation(options);
            return buffer.ToArray();
        }
    }

    internal OdfNode[] ReadXmlPart(
        byte[] data,
        string partPath,
        OdfLoadOptions options)
    {
        return OdfTreeBuilder.ReadBytes(data, partPath, options);
    }

    internal OdfNode[] ReadOptionalXmlPart(
        byte[] data,
        string partPath,
        OdfLoadOptions options)
    {
        if (data.Length == 0)
        {
            return [];
        }
        return OdfTreeBuilder.ReadBytes(data, partPath, options);
    }

    internal bool IsZipPackage(byte[] data)
    {
        if (data.Length < 4 || data[0] != 80 || data[1] != 75)
        {
            return false;
        }
        return data[2] == 3 && data[3] == 4
            || data[2] == 5 && data[3] == 6
            || data[2] == 7 && data[3] == 8;
    }

    internal string InferFlatMediaType(string sourceName)
    {
        string extension = Path.GetExtension(sourceName).ToLowerInvariant();
        if (extension == ".fodt")
        {
            return OdfMediaTypes.Text;
        }
        if (extension == ".fods")
        {
            return OdfMediaTypes.Spreadsheet;
        }
        if (extension == ".fodp")
        {
            return OdfMediaTypes.Presentation;
        }
        if (extension == ".fodg")
        {
            return OdfMediaTypes.Graphics;
        }
        return "";
    }

    internal void ValidateReadInputs(string sourceName, OdfLoadOptions options)
    {
        if (sourceName == null || options == null)
        {
            throw new OdfDocumentException(
                "A source name and load options are required.");
        }
    }

    internal void CheckCancellation(OdfLoadOptions options)
    {
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfDocumentException("Document reading was cancelled.");
        }
    }

    internal byte[] CopyBytes(byte[] data)
    {
        byte[] result = new byte[data.Length];
        Array.Copy(data, result, data.Length);
        return result;
    }
}
