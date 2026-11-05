namespace raw;

/// <summary>Represents one inspected raw input without exposing parser implementation types.</summary>
public class RawDocument
{
    /// <summary>The content-detected input family.</summary>
    public RawFormat Format;
    /// <summary>The selected camera or format profile name, or an empty string.</summary>
    public string ProfileName;
    /// <summary>The authoritative generic raw descriptor, or <c>null</c> for container formats.</summary>
    public RawDescriptor? GenericDescriptor;
    /// <summary>Optional CR2, Canon, or Nikon inspection results isolated from the generic document surface.</summary>
    public RawFormatDetails? FormatDetails;
    /// <summary>Discovered image descriptions in deterministic document order.</summary>
    public RawImageInfo[]? Images;
    /// <summary>Retained embedded previews and thumbnails.</summary>
    public RawThumbnail[] Thumbnails;
    /// <summary>The exact known payload ranges.</summary>
    public RawPayloadMap PayloadMap;
    /// <summary>Independently reported operations supported for this document.</summary>
    public RawCapabilities Capabilities;
    /// <summary>Open and inspection diagnostics.</summary>
    public RawDiagnostic[] Diagnostics;
    /// <summary>The typed TIFF, Exif, GPS, and interoperability metadata view.</summary>
    public RawMetadata Metadata;
    /// <summary>Whether metadata directory structure was requested and inspected.</summary>
    public bool MetadataLoaded;
    /// <summary>The number of inspected metadata directories.</summary>
    public int MetadataDirectoryCount;
    /// <summary>Whether embedded preview bytes were requested and retained.</summary>
    public bool PreviewsLoaded;
    /// <summary>Whether raw-payload decode was enabled when the document was opened.</summary>
    public bool RawPayloadAvailable;
    /// <summary>Whether bounded source bytes remain available to document methods.</summary>
    public bool SourceRetained;
    /// <summary>Whether the document owns its retained source storage.</summary>
    public bool OwnsSource;
    /// <summary>Caller-authorized preview replacements staged for a later writer.</summary>
    public RawPreviewMetadataEdit[] PendingPreviewEdits;

    internal System.ReadOnlyMemory<byte> Source;
    internal TiffIfdGraph? TiffGraph;
    internal RawReadProfile? ReadProfile;

    /// <summary>Creates a document model without attaching private source storage.</summary>
    /// <param name="format">The content-detected format.</param>
    /// <param name="profileName">The selected profile name.</param>
    /// <param name="genericDescriptor">The generic descriptor, or <c>null</c>.</param>
    /// <param name="images">Discovered image descriptions.</param>
    /// <param name="thumbnails">Retained previews and thumbnails.</param>
    /// <param name="payloadMap">The known payload map.</param>
    /// <param name="capabilities">Independent operation capabilities.</param>
    /// <param name="diagnostics">Open diagnostics.</param>
    /// <param name="metadataLoaded">Whether metadata structure was inspected.</param>
    /// <param name="metadataDirectoryCount">The metadata directory count.</param>
    /// <param name="previewsLoaded">Whether preview bytes were retained.</param>
    /// <param name="rawPayloadAvailable">Whether raw decode was enabled.</param>
    public RawDocument(RawFormat format, string profileName, RawDescriptor? genericDescriptor, RawImageInfo[] images, RawThumbnail[]? thumbnails, RawPayloadMap payloadMap, RawCapabilities capabilities, RawDiagnostic[]? diagnostics, bool metadataLoaded, int metadataDirectoryCount, bool previewsLoaded, bool rawPayloadAvailable)
    {
        Format = format;
        ProfileName = profileName == null ? string.Empty : profileName;
        GenericDescriptor = genericDescriptor;
        FormatDetails = new RawFormatDetails();
        Images = images == null ? System.Array.Empty<RawImageInfo>() : images;
        Thumbnails = thumbnails == null ? System.Array.Empty<RawThumbnail>() : thumbnails;
        PayloadMap = payloadMap == null ? new RawPayloadMap(0, null) : payloadMap;
        Capabilities = capabilities == null ? new RawCapabilities(false, false, false, false, false, false, false) : capabilities;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
        Metadata = new RawMetadata(null);
        MetadataLoaded = metadataLoaded;
        MetadataDirectoryCount = metadataDirectoryCount;
        PreviewsLoaded = previewsLoaded;
        RawPayloadAvailable = rawPayloadAvailable;
        SourceRetained = false;
        OwnsSource = false;
        PendingPreviewEdits = System.Array.Empty<RawPreviewMetadataEdit>();
        Source = default(System.ReadOnlyMemory<byte>);
        TiffGraph = null;
        ReadProfile = null;
    }

    /// <summary>Returns an independent copy of the document capability flags.</summary>
    /// <returns>The copied capability set.</returns>
    internal RawCapabilities GetCapabilities()
    {
        if (Capabilities == null)
        {
            return new RawCapabilities(false, false, false, false, false, false, false);
        }
        return new RawCapabilities(Capabilities.Recognized, Capabilities.Inspected, Capabilities.Decoded, Capabilities.Rendered, Capabilities.PreservingRewrite, Capabilities.Encoded, Capabilities.VendorConfirmed);
    }

    /// <summary>Returns one retained thumbnail by its collection index.</summary>
    /// <param name="index">The zero-based thumbnail collection index.</param>
    /// <returns>The retained thumbnail.</returns>
    /// <exception cref="RawFormatException">The index is outside the retained thumbnail collection.</exception>
    internal RawThumbnail GetThumbnail(int index)
    {
        if (Thumbnails == null || index < 0 || index >= Thumbnails.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The thumbnail index is outside the retained collection.", -1);
        }
        return Thumbnails[index];
    }

    /// <summary>Copies one mapped payload from the retained bounded source.</summary>
    /// <param name="block">The mapped payload block.</param>
    /// <returns>An independent byte copy of the complete block.</returns>
    /// <exception cref="RawFormatException">The source or payload range is unavailable.</exception>
    internal byte[] ExtractPayload(RawPayloadBlock? block)
    {
        if (!SourceRetained || block == null || PayloadMap == null || !block.Validate(Source.Length).IsValid() || block.Length > int.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A retained source and valid mapped payload are required.", -1);
        }
        bool mapped = false;
        int index = 0;
        while (index < PayloadMap.Blocks.Length)
        {
            if (PayloadMap.Blocks[index] == block)
            {
                mapped = true;
                break;
            }
            index++;
        }
        if (!mapped)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The payload block does not belong to this document.", block.Offset);
        }
        return Source.Slice((int)block.Offset, (int)block.Length).ToArray();
    }

    /// <summary>Decodes the selected raw image through the document facade.</summary>
    /// <param name="options">Raw decode options.</param>
    /// <returns>The normalized sensor image.</returns>
    internal RawSensorImage Decode(RawDecodeOptions options)
    {
        return RawCodec.Decode(this, options);
    }

    /// <summary>Renders the selected raw image through the complete processing pipeline.</summary>
    /// <param name="options">Render options.</param>
    /// <returns>The rendered gray, RGB, or RGBA image.</returns>
    internal RawRgbImage Render(RawRenderOptions options)
    {
        return RawCodec.Render(this, options);
    }

    /// <summary>Validates retained structure and public document semantics.</summary>
    /// <param name="options">Validation subsystem selection and limits.</param>
    /// <returns>All selected validation findings.</returns>
    internal RawValidationReport Validate(RawValidationOptions options)
    {
        return RawCodec.Validate(this, options);
    }

    /// <summary>Applies staged metadata and preview edits atomically or performs an equivalent dry run.</summary>
    /// <param name="editSet">The metadata edit set.</param>
    /// <param name="dryRun">Whether to report without mutating.</param>
    /// <returns>The ordered changes and diagnostics.</returns>
    internal RawMetadataEditReport EditMetadata(RawMetadataEditSet editSet, bool dryRun)
    {
        return MetadataEditor.Apply(this, editSet, dryRun);
    }

    internal void AttachSource(System.ReadOnlyMemory<byte> source, bool ownsSource, TiffIfdGraph? graph)
    {
        Source = source;
        SourceRetained = true;
        OwnsSource = ownsSource;
        TiffGraph = graph;
    }

    internal void AttachMetadata(RawMetadata? metadata)
    {
        Metadata = metadata == null ? new RawMetadata(null) : metadata;
    }

    internal void AttachCr2Header(RawCr2Header? header)
    {
        (FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header = header;
    }

    internal void AttachCanonCr2Metadata(RawCanonCr2Metadata metadata)
    {
        (FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon = metadata;
    }

    internal void AttachNikonNefMetadata(RawNikonNefMetadata metadata)
    {
        (FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Nikon = metadata;
    }

    internal void AttachReadProfile(RawReadProfile? profile)
    {
        ReadProfile = profile;
    }
}
