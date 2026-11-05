using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace odf;

internal interface IStreamingXmlEventTransform
{
    OdfStreamEvent TransformXmlEvent(OdfStreamEvent streamEvent);
}

internal class StreamingTransformFrame
{
    internal OdfQualifiedName OriginalName = new OdfQualifiedName();
    internal OdfQualifiedName OutputName = new OdfQualifiedName();
    internal bool Transforming;
}

internal static class StreamingCopyTransformer
{
    internal static void CopyXml(
        Stream input,
        Stream output,
        string partPath,
        OdfLoadOptions loadOptions,
        OdfSaveOptions saveOptions,
        IStreamingXmlEventTransform transform)
    {
        ValidateXmlCopyInputs(
            input,
            output,
            partPath,
            loadOptions,
            saveOptions,
            transform);
        OdfQualifiedName[] selectedNames =
            loadOptions.StreamSelection.ElementNames;
        OdfLoadOptions readerOptions = CreateCopyXmlOptions(loadOptions);
        XmlEventReader reader = new XmlEventReader(
            input,
            partPath,
            readerOptions);
        XmlEventWriter writer = new XmlEventWriter(output, saveOptions);
        CopyXmlEvents(reader, writer, selectedNames, transform);
    }

    internal static void CopyPackage(
        Stream input,
        Stream output,
        OdfLoadOptions loadOptions,
        OdfSaveOptions saveOptions,
        IStreamingXmlEventTransform transform)
    {
        ValidatePackageCopyInputs(
            input,
            output,
            loadOptions,
            saveOptions,
            transform);
        using (ZipArchive source = new ZipArchive(
            input,
            ZipArchiveMode.Read,
            loadOptions.LeaveInputOpen))
        {
            string rootMediaType = ReadRootMediaType(source);
            OdfManifest? sourceManifest = ReadSourceManifest(source);
            string version = ReadManifestVersion(sourceManifest);
            PackageEventWriter packageWriter = new PackageEventWriter(
                output,
                rootMediaType,
                version,
                saveOptions);
            int index = 0;
            while (index < source.Entries.Count)
            {
                CheckCopyCancellation(loadOptions, saveOptions);
                ZipArchiveEntry entry = source.Entries[index];
                string path = entry.FullName;
                if (path != OdfPartNames.Mimetype
                    && path != OdfPartNames.Manifest)
                {
                    CopyPackageEntry(
                        entry,
                        packageWriter,
                        sourceManifest,
                        loadOptions,
                        saveOptions,
                        transform);
                }
                index = index + 1;
            }
            packageWriter.CompletePackage();
        }
    }

    static void CopyXmlEvents(
        XmlEventReader reader,
        XmlEventWriter writer,
        OdfQualifiedName[] selectedNames,
        IStreamingXmlEventTransform transform)
    {
        List<StreamingTransformFrame> frames =
            new List<StreamingTransformFrame>();
        OdfStreamEvent streamEvent = reader.ReadNextEvent();
        while (streamEvent.Kind != OdfStreamEventKind.EndOfStream)
        {
            if (streamEvent.Kind == OdfStreamEventKind.ElementStart)
            {
                CopyElementStart(
                    streamEvent,
                    writer,
                    frames,
                    selectedNames,
                    transform);
            }
            else if (streamEvent.Kind == OdfStreamEventKind.ElementEnd)
            {
                CopyElementEnd(streamEvent, writer, frames, transform);
            }
            else
            {
                bool transforming = frames.Count > 0
                    && frames[frames.Count - 1].Transforming;
                OdfStreamEvent outputEvent = transforming
                    ? ApplyTransform(streamEvent, transform)
                    : streamEvent;
                EnsureEventKind(streamEvent.Kind, outputEvent.Kind);
                writer.WriteEvent(outputEvent);
            }
            streamEvent = reader.ReadNextEvent();
        }
        if (frames.Count != 0)
        {
            throw new OdfXmlException(
                "The XML copy stream ended before every element was closed.");
        }
        writer.CompleteXml();
    }

    static void CopyElementStart(
        OdfStreamEvent streamEvent,
        XmlEventWriter writer,
        List<StreamingTransformFrame> frames,
        OdfQualifiedName[] selectedNames,
        IStreamingXmlEventTransform transform)
    {
        bool transforming = frames.Count > 0
            && frames[frames.Count - 1].Transforming;
        if (!transforming && MatchesSelectedName(
            streamEvent.Name,
            selectedNames))
        {
            transforming = true;
        }
        OdfStreamEvent outputEvent = transforming
            ? ApplyTransform(streamEvent, transform)
            : streamEvent;
        EnsureEventKind(OdfStreamEventKind.ElementStart, outputEvent.Kind);
        StreamingTransformFrame frame = new StreamingTransformFrame();
        frame.OriginalName = CopyRequiredName(streamEvent.Name);
        frame.OutputName = CopyRequiredName(outputEvent.Name);
        frame.Transforming = transforming;
        frames.Add(frame);
        writer.WriteEvent(outputEvent);
    }

    static void CopyElementEnd(
        OdfStreamEvent streamEvent,
        XmlEventWriter writer,
        List<StreamingTransformFrame> frames,
        IStreamingXmlEventTransform transform)
    {
        if (frames.Count == 0)
        {
            throw new OdfXmlException(
                "The XML copy stream contains an unmatched closing element.");
        }
        int index = frames.Count - 1;
        StreamingTransformFrame frame = frames[index];
        if (!NamesMatch(frame.OriginalName, streamEvent.Name))
        {
            throw new OdfXmlException(
                "The XML copy stream contains mismatched input boundaries.");
        }
        OdfStreamEvent outputEvent = frame.Transforming
            ? ApplyTransform(streamEvent, transform)
            : streamEvent;
        EnsureEventKind(OdfStreamEventKind.ElementEnd, outputEvent.Kind);
        outputEvent.Name = CopyRequiredName(frame.OutputName);
        writer.WriteEvent(outputEvent);
        frames.RemoveAt(index);
    }

    static OdfStreamEvent ApplyTransform(
        OdfStreamEvent streamEvent,
        IStreamingXmlEventTransform transform)
    {
        OdfStreamEvent result = transform.TransformXmlEvent(streamEvent);
        if (result == null)
        {
            throw new OdfXmlException(
                "An XML event transform cannot return a null event.");
        }
        return result;
    }

    static void EnsureEventKind(
        OdfStreamEventKind expected,
        OdfStreamEventKind actual)
    {
        if (expected != actual)
        {
            throw new OdfXmlException(
                "An XML event transform must preserve the event kind.");
        }
    }

    static bool MatchesSelectedName(
        OdfQualifiedName? name,
        OdfQualifiedName[] selectedNames)
    {
        if (selectedNames == null || selectedNames.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < selectedNames.Length)
        {
            OdfQualifiedName? selected = selectedNames[index];
            if (selected != null && name != null
                && selected.NamespaceName == name.NamespaceName
                && selected.LocalName == name.LocalName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool NamesMatch(
        OdfQualifiedName expected,
        OdfQualifiedName? actual)
    {
        return actual != null
            && expected.NamespaceName == actual.NamespaceName
            && expected.LocalName == actual.LocalName;
    }

    static OdfQualifiedName CopyRequiredName(OdfQualifiedName? source)
    {
        if (source == null)
        {
            throw new OdfXmlException(
                "An XML copy event requires a qualified name.");
        }
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = source.NamespaceName;
        result.LocalName = source.LocalName;
        result.Prefix = source.Prefix;
        return result;
    }

    static void CopyPackageEntry(
        ZipArchiveEntry sourceEntry,
        PackageEventWriter packageWriter,
        OdfManifest? sourceManifest,
        OdfLoadOptions loadOptions,
        OdfSaveOptions saveOptions,
        IStreamingXmlEventTransform transform)
    {
        string path = sourceEntry.FullName;
        string mediaType = FindPartMediaType(sourceManifest, path);
        if (mediaType.Length == 0)
        {
            mediaType = InferPartMediaType(path);
        }
        OdfCompressionMethod compression = SelectSourceCompression(
            sourceEntry);
        Stream target = packageWriter.BeginPackageEntry(
            path,
            mediaType,
            compression);
        using (Stream source = sourceEntry.Open())
        {
            if (IsSelectedXmlPart(path, loadOptions.StreamSelection))
            {
                OdfSaveOptions partSaveOptions =
                    CreateEntrySaveOptions(saveOptions);
                CopyXml(
                    source,
                    target,
                    path,
                    loadOptions,
                    partSaveOptions,
                    transform);
            }
            else
            {
                CopyUnchangedEntryContent(
                    source,
                    packageWriter,
                    loadOptions,
                    saveOptions);
            }
        }
        packageWriter.EndPackageEntry();
    }

    static OdfCompressionMethod SelectSourceCompression(
        ZipArchiveEntry sourceEntry)
    {
        if (sourceEntry.FullName.EndsWith("/", StringComparison.Ordinal)
            || sourceEntry.Length == 0
            || sourceEntry.CompressedLength >= sourceEntry.Length)
        {
            return OdfCompressionMethod.Stored;
        }
        return OdfCompressionMethod.Deflated;
    }

    static void CopyUnchangedEntryContent(
        Stream source,
        PackageEventWriter packageWriter,
        OdfLoadOptions loadOptions,
        OdfSaveOptions saveOptions)
    {
        byte[] buffer = new byte[81920];
        int read = source.Read(buffer, 0, buffer.Length);
        while (read > 0)
        {
            CheckCopyCancellation(loadOptions, saveOptions);
            packageWriter.WritePackageContent(buffer, 0, read);
            read = source.Read(buffer, 0, buffer.Length);
        }
    }

    static bool IsSelectedXmlPart(
        string path,
        OdfStreamSelection selection)
    {
        if (!path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (selection.PartPaths == null
            || selection.PartPaths.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < selection.PartPaths.Length)
        {
            if (selection.PartPaths[index] == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string ReadRootMediaType(ZipArchive source)
    {
        ZipArchiveEntry? entry = FindArchiveEntry(
            source,
            OdfPartNames.Mimetype);
        if (entry == null)
        {
            throw new OdfPackageException(
                "A streaming package transform requires a mimetype entry.");
        }
        byte[] content = ReadSmallEntry(entry, 4096);
        return Encoding.UTF8.GetString(content);
    }

    static OdfManifest? ReadSourceManifest(ZipArchive source)
    {
        ZipArchiveEntry? entry = FindArchiveEntry(
            source,
            OdfPartNames.Manifest);
        if (entry == null)
        {
            return null;
        }
        return ManifestParser.ParseBytes(ReadSmallEntry(entry, 16777216));
    }

    static ZipArchiveEntry? FindArchiveEntry(
        ZipArchive source,
        string path)
    {
        int index = 0;
        while (index < source.Entries.Count)
        {
            if (source.Entries[index].FullName == path)
            {
                return source.Entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    static byte[] ReadSmallEntry(ZipArchiveEntry entry, int maximumBytes)
    {
        if (entry.Length > maximumBytes || entry.Length > int.MaxValue)
        {
            throw new OdfPackageException(
                "A package metadata entry exceeds the streaming transform limit.");
        }
        using (Stream input = entry.Open())
        using (MemoryStream output = new MemoryStream())
        {
            byte[] buffer = new byte[4096];
            int read = input.Read(buffer, 0, buffer.Length);
            while (read > 0)
            {
                if (output.Length > maximumBytes - read)
                {
                    throw new OdfPackageException(
                        "A package metadata entry exceeds the streaming transform limit.");
                }
                output.Write(buffer, 0, read);
                read = input.Read(buffer, 0, buffer.Length);
            }
            return output.ToArray();
        }
    }

    static string ReadManifestVersion(OdfManifest? manifest)
    {
        if (manifest != null && (manifest.Version == "1.2"
            || manifest.Version == "1.3" || manifest.Version == "1.4"))
        {
            return manifest.Version;
        }
        return "1.4";
    }

    static string FindPartMediaType(
        OdfManifest? manifest,
        string path)
    {
        if (manifest == null)
        {
            return "";
        }
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            if (manifest.Entries[index].FullPath == path)
            {
                return manifest.Entries[index].MediaType;
            }
            index = index + 1;
        }
        return "";
    }

    static string InferPartMediaType(string path)
    {
        if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.Xml;
        }
        if (path.EndsWith(".rdf", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.RdfXml;
        }
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.Png;
        }
        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.Jpeg;
        }
        if (path.EndsWith("/", StringComparison.Ordinal))
        {
            return "";
        }
        return OdfMediaTypes.OctetStream;
    }

    static OdfLoadOptions CreateCopyXmlOptions(OdfLoadOptions source)
    {
        OdfLoadOptions result = new OdfLoadOptions();
        result.SecurityLimits = source.SecurityLimits;
        result.Cancellation = source.Cancellation;
        result.LeaveInputOpen = source.LeaveInputOpen;
        result.PreserveLexicalValues = source.PreserveLexicalValues;
        result.PreserveUnknownContent = true;
        result.MaterializeXmlTrees = false;
        result.IndexPackageEntries = false;
        result.ValidateOnLoad = false;
        result.AllowRecovery = source.AllowRecovery;
        result.StreamSelection = new OdfStreamSelection();
        result.StreamSelection.IncludePackageEntryEvents = false;
        result.StreamSelection.IncludeElementEvents = true;
        result.StreamSelection.IncludeTextEvents = true;
        result.StreamSelection.IncludeCommentEvents = true;
        result.StreamSelection.IncludeProcessingInstructionEvents = true;
        result.StreamSelection.IncludeBinaryContent = false;
        result.StreamSelection.SkipUnselectedSubtrees = false;
        return result;
    }

    static OdfSaveOptions CreateEntrySaveOptions(OdfSaveOptions source)
    {
        OdfSaveOptions result = new OdfSaveOptions();
        result.Cancellation = source.Cancellation;
        result.LeaveOutputOpen = true;
        result.PreserveUnknownContent = true;
        result.PreserveLexicalValues = source.PreserveLexicalValues;
        result.PreserveTimestamps = source.PreserveTimestamps;
        result.Deterministic = source.Deterministic;
        result.IndentXml = source.IndentXml;
        result.SortManifestEntries = source.SortManifestEntries;
        result.CompatibilityProfile = source.CompatibilityProfile;
        result.CompressionMethod = source.CompressionMethod;
        result.TargetVersion = source.TargetVersion;
        result.TargetConformanceClass = source.TargetConformanceClass;
        return result;
    }

    static void CheckCopyCancellation(
        OdfLoadOptions loadOptions,
        OdfSaveOptions saveOptions)
    {
        if (loadOptions.Cancellation.IsCancellationRequested
            || saveOptions.Cancellation.IsCancellationRequested)
        {
            throw new OdfDocumentException(
                "Streaming copy transformation was cancelled.");
        }
    }

    static void ValidateXmlCopyInputs(
        Stream input,
        Stream output,
        string partPath,
        OdfLoadOptions loadOptions,
        OdfSaveOptions saveOptions,
        IStreamingXmlEventTransform transform)
    {
        if (input == null || !input.CanRead || output == null
            || !output.CanWrite || partPath == null || loadOptions == null
            || loadOptions.StreamSelection == null
            || loadOptions.SecurityLimits == null
            || loadOptions.Cancellation == null || saveOptions == null
            || saveOptions.Cancellation == null || transform == null)
        {
            throw new OdfDocumentException(
                "Readable input, writable output, options, and an event transform are required.");
        }
    }

    static void ValidatePackageCopyInputs(
        Stream input,
        Stream output,
        OdfLoadOptions loadOptions,
        OdfSaveOptions saveOptions,
        IStreamingXmlEventTransform transform)
    {
        ValidateXmlCopyInputs(
            input,
            output,
            "package",
            loadOptions,
            saveOptions,
            transform);
        if (!input.CanSeek)
        {
            throw new OdfPackageException(
                "The source package stream must be seekable for entry copy transformation.");
        }
    }
}
