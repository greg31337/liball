namespace raw;

/// <summary>Provides the default stateless implementation of <see cref="IRawCodec"/>.</summary>
/// <remarks>This class is intended for constructor injection and test substitution. It delegates to the library's internal codec implementation.</remarks>
public class RawCodecService : IRawCodec
{
    /// <summary>Creates a stateless codec service.</summary>
    public RawCodecService()
    {
    }

    /// <summary>Probes an exact byte-array input without decoding image payloads.</summary>
    public RawProbe ProbeBytes(byte[] source, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options)
    {
        return RawCodec.Probe(RawCodec.GetByteMemory(source), fileName, expectedFormat, genericDescriptorProvided, options);
    }

    /// <summary>Probes an exact transient span without decoding image payloads.</summary>
    public RawProbe ProbeSpan(System.ReadOnlySpan<byte> source, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options)
    {
        RawCodec.ValidateOpenOptions(options);
        byte[] bytes = RawCodec.CopySpan(source, options.Limits, options.CancellationToken);
        return RawCodec.Probe(bytes, fileName, expectedFormat, genericDescriptorProvided, options);
    }

    /// <summary>Probes a bounded caller-owned stream without closing it.</summary>
    public RawProbe ProbeStream(System.IO.Stream source, long offset, long length, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options)
    {
        return RawCodec.Probe(source, offset, length, fileName, expectedFormat, genericDescriptorProvided, options);
    }

    /// <summary>Probes a path without decoding image payloads.</summary>
    public RawProbe ProbePath(string path, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options)
    {
        return RawCodec.Probe(path, expectedFormat, genericDescriptorProvided, options);
    }

    /// <summary>Probes a bounded memory input without decoding image payloads.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="fileName">An optional filename used only for mismatch diagnostics.</param>
    /// <param name="expectedFormat">The caller-expected format, or <see cref="RawFormat.Unknown"/>.</param>
    /// <param name="genericDescriptorProvided">Whether an authoritative generic descriptor is available.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>The content-based probe result.</returns>
    public RawProbe Probe(System.ReadOnlyMemory<byte> source, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options)
    {
        return RawCodec.Probe(source, fileName, expectedFormat, genericDescriptorProvided, options);
    }

    /// <summary>Opens an exact byte-array input according to the requested read selections.</summary>
    public RawDocument OpenBytes(byte[] source, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        return RawCodec.Open(RawCodec.GetByteMemory(source), genericDescriptor, options);
    }

    /// <summary>Copies and opens an exact transient span according to the requested read selections.</summary>
    public RawDocument OpenSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        RawCodec.ValidateOpenOptions(options);
        byte[] bytes = RawCodec.CopySpan(source, options.Limits, options.CancellationToken);
        return RawCodec.OpenCore(bytes, genericDescriptor, options, true);
    }

    /// <summary>Opens a bounded caller-owned stream without closing it.</summary>
    public RawDocument OpenStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        return RawCodec.Open(source, offset, length, genericDescriptor, options);
    }

    /// <summary>Opens a path according to the requested read selections.</summary>
    public RawDocument OpenPath(string path, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        return RawCodec.Open(path, genericDescriptor, options);
    }

    /// <summary>Opens a bounded memory input according to the requested read selections.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Read selections, limits, and cancellation state.</param>
    /// <returns>The inspected document.</returns>
    public RawDocument Open(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        return RawCodec.Open(source, genericDescriptor, options);
    }

    /// <summary>Opens selected metadata from an exact byte-array input.</summary>
    public RawDocument OpenMetadataBytes(byte[] source, RawDescriptor? genericDescriptor, RawMetadataOptions options)
    {
        return RawCodec.OpenMetadata(RawCodec.GetByteMemory(source), genericDescriptor, options);
    }

    /// <summary>Copies an exact transient span and opens selected metadata.</summary>
    public RawDocument OpenMetadataSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawMetadataOptions options)
    {
        RawOpenOptions openOptions = RawCodec.CreateMetadataOpenOptions(options, string.Empty);
        byte[] bytes = RawCodec.CopySpan(source, options.Limits, options.CancellationToken);
        return RawCodec.OpenCore(bytes, genericDescriptor, openOptions, true, options);
    }

    /// <summary>Opens selected metadata from a bounded caller-owned stream without closing it.</summary>
    public RawDocument OpenMetadataStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawMetadataOptions options)
    {
        return RawCodec.OpenMetadata(source, offset, length, genericDescriptor, options);
    }

    /// <summary>Opens selected metadata from a path.</summary>
    public RawDocument OpenMetadataPath(string path, RawDescriptor? genericDescriptor, RawMetadataOptions options)
    {
        return RawCodec.OpenMetadata(path, genericDescriptor, options);
    }

    /// <summary>Opens selected metadata from bounded memory and applies the requested reconciliation policy.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Metadata selections, limits, and reconciliation policy.</param>
    /// <returns>A metadata-only inspected document.</returns>
    public RawDocument OpenMetadata(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawMetadataOptions options)
    {
        return RawCodec.OpenMetadata(source, genericDescriptor, options);
    }

    /// <summary>Opens only preview descriptions and bytes from an exact byte-array input.</summary>
    public RawDocument OpenPreviewsBytes(byte[] source, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        return RawCodec.OpenPreviews(RawCodec.GetByteMemory(source), genericDescriptor, options);
    }

    /// <summary>Copies an exact transient span and opens only preview descriptions and bytes.</summary>
    public RawDocument OpenPreviewsSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        RawCodec.ValidateOpenOptions(options);
        byte[] bytes = RawCodec.CopySpan(source, options.Limits, options.CancellationToken);
        RawOpenOptions selected = RawCodec.CopyOpenOptions(options, false, true, false, options.FileNameHint);
        return RawCodec.OpenCore(bytes, genericDescriptor, selected, true);
    }

    /// <summary>Opens only preview descriptions and bytes from a bounded caller-owned stream without closing it.</summary>
    public RawDocument OpenPreviewsStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        return RawCodec.OpenPreviews(source, offset, length, genericDescriptor, options);
    }

    /// <summary>Opens only preview descriptions and bytes from a path.</summary>
    public RawDocument OpenPreviewsPath(string path, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        return RawCodec.OpenPreviews(path, genericDescriptor, options);
    }

    /// <summary>Opens only embedded preview and thumbnail descriptions and bytes from bounded memory.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>A preview-only inspected document.</returns>
    public RawDocument OpenPreviews(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options)
    {
        return RawCodec.OpenPreviews(source, genericDescriptor, options);
    }

    /// <summary>Reports independently supported operations for a format and optional generic descriptor.</summary>
    /// <param name="format">The content format.</param>
    /// <param name="genericDescriptor">The generic descriptor when applicable, or <c>null</c>.</param>
    /// <returns>The independent capability flags.</returns>
    public RawCapabilities GetCapabilities(RawFormat format, RawDescriptor? genericDescriptor)
    {
        return RawCodec.GetCapabilities(format, genericDescriptor);
    }

    /// <summary>Decodes the selected image from an opened document.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    public RawSensorImage Decode(RawDocument document, RawDecodeOptions options)
    {
        return RawCodec.Decode(document, options);
    }

    /// <summary>Opens and decodes an exact byte-array input.</summary>
    public RawSensorImage DecodeBytes(byte[] source, RawDescriptor? genericDescriptor, RawDecodeOptions options)
    {
        return RawCodec.Decode(RawCodec.GetByteMemory(source), genericDescriptor, options);
    }

    /// <summary>Copies, opens, and decodes an exact transient span.</summary>
    public RawSensorImage DecodeSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawDecodeOptions options)
    {
        RawOpenOptions openOptions = RawCodec.DecodeOpenOptions(options, string.Empty);
        byte[] bytes = RawCodec.CopySpan(source, options.Limits, options.CancellationToken);
        return RawCodec.Decode(RawCodec.OpenCore(bytes, genericDescriptor, openOptions, true), options);
    }

    /// <summary>Opens and decodes a bounded caller-owned stream without closing it.</summary>
    public RawSensorImage DecodeStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawDecodeOptions options)
    {
        return RawCodec.Decode(source, offset, length, genericDescriptor, options);
    }

    /// <summary>Opens and decodes a path.</summary>
    public RawSensorImage DecodePath(string path, RawDescriptor? genericDescriptor, RawDecodeOptions options)
    {
        return RawCodec.Decode(path, genericDescriptor, options);
    }

    /// <summary>Renders one selected image and acquisition plane through the complete processing pipeline.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="options">Image, plane, crop, processing, geometry, and output controls.</param>
    /// <returns>A tightly pixel-interleaved gray, RGB, or RGBA image.</returns>
    public RawRgbImage Render(RawDocument document, RawRenderOptions options)
    {
        return RawCodec.Render(document, options);
    }

    /// <summary>Renders an existing normalized sensor image through the complete processing pipeline.</summary>
    /// <param name="sensorImage">The normalized sensor image and its calibration.</param>
    /// <param name="options">Crop, processing, geometry, and output controls.</param>
    /// <returns>A tightly pixel-interleaved gray, RGB, or RGBA image.</returns>
    public RawRgbImage RenderSensorImage(RawSensorImage sensorImage, RawRenderOptions options)
    {
        return RawRenderer.Render(sensorImage, options);
    }

    /// <summary>Opens and renders an exact byte-array input.</summary>
    public RawRgbImage RenderBytes(byte[] source, RawDescriptor? genericDescriptor, RawRenderOptions options)
    {
        return RawCodec.Render(RawCodec.GetByteMemory(source), genericDescriptor, options);
    }

    /// <summary>Copies, opens, and renders an exact transient span.</summary>
    public RawRgbImage RenderSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawRenderOptions options)
    {
        RawOpenOptions openOptions = RawCodec.RenderOpenOptions(options, string.Empty);
        byte[] bytes = RawCodec.CopySpan(source, options.Limits, options.CancellationToken);
        return RawCodec.Render(RawCodec.OpenCore(bytes, genericDescriptor, openOptions, true), options);
    }

    /// <summary>Opens and renders a bounded caller-owned stream without closing it.</summary>
    public RawRgbImage RenderStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawRenderOptions options)
    {
        return RawCodec.Render(source, offset, length, genericDescriptor, options);
    }

    /// <summary>Opens and renders a path.</summary>
    public RawRgbImage RenderPath(string path, RawDescriptor? genericDescriptor, RawRenderOptions options)
    {
        return RawCodec.Render(path, genericDescriptor, options);
    }

    /// <summary>Validates retained structure and public document semantics.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="options">Validation subsystem selection and limits.</param>
    /// <returns>All selected validation findings.</returns>
    public RawValidationReport Validate(RawDocument document, RawValidationOptions options)
    {
        return RawCodec.Validate(document, options);
    }

    /// <summary>Opens and validates an exact byte-array input.</summary>
    public RawValidationReport ValidateBytes(byte[] source, RawDescriptor? genericDescriptor, RawValidationOptions options)
    {
        return RawCodec.Validate(RawCodec.GetByteMemory(source), genericDescriptor, options);
    }

    /// <summary>Copies, opens, and validates an exact transient span.</summary>
    public RawValidationReport ValidateSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawValidationOptions options)
    {
        RawOpenOptions openOptions = RawCodec.ValidationOpenOptions(options, string.Empty);
        byte[] bytes = RawCodec.CopySpan(source, options.Limits, options.CancellationToken);
        return RawCodec.Validate(RawCodec.OpenCore(bytes, genericDescriptor, openOptions, true), options);
    }

    /// <summary>Opens and validates a bounded caller-owned stream without closing it.</summary>
    public RawValidationReport ValidateStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawValidationOptions options)
    {
        return RawCodec.Validate(source, offset, length, genericDescriptor, options);
    }

    /// <summary>Opens and validates a path.</summary>
    public RawValidationReport ValidatePath(string path, RawDescriptor? genericDescriptor, RawValidationOptions options)
    {
        return RawCodec.Validate(path, genericDescriptor, options);
    }

    /// <summary>Validates open options through selected semantic validation.</summary>
    public RawValidationReport ValidateOpenOptions(RawOpenOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Open", "Open options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates decode options through selected semantic validation.</summary>
    public RawValidationReport ValidateDecodeOptions(RawDecodeOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Decode", "Decode options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates normalization options through selected semantic validation.</summary>
    public RawValidationReport ValidateNormalizationOptions(RawNormalizationOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Normalization", "Normalization options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates defect-correction options through selected semantic validation.</summary>
    public RawValidationReport ValidateDefectOptions(RawDefectOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Defect", "Defect-correction options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates lens-correction options through selected semantic validation.</summary>
    public RawValidationReport ValidateLensCorrectionOptions(RawLensCorrectionOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "LensCorrection", "Lens-correction options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates preview options through selected semantic validation.</summary>
    public RawValidationReport ValidatePreviewOptions(RawPreviewOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Preview", "Preview options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates analysis options through selected semantic validation.</summary>
    public RawValidationReport ValidateAnalysisOptions(RawAnalysisOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Analysis", "Analysis options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates histogram options through selected semantic validation.</summary>
    public RawValidationReport ValidateHistogramOptions(RawHistogramOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Histogram", "Histogram options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates render options through selected semantic validation.</summary>
    public RawValidationReport ValidateRenderOptions(RawRenderOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Render", "Render options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates ICC transform options through selected semantic validation.</summary>
    public RawValidationReport ValidateIccTransformOptions(RawIccTransformOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "IccTransform", "ICC transform options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates metadata options through selected metadata validation.</summary>
    public RawValidationReport ValidateMetadataOptions(RawMetadataOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Metadata, "Metadata", "Metadata options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Metadata);
    }

    /// <summary>Validates write options through selected writer validation.</summary>
    public RawValidationReport ValidateWriteOptions(RawWriteOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Writer, "Write", "Write options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Writer);
    }

    /// <summary>Validates generic encoding options through selected writer validation.</summary>
    public RawValidationReport ValidateGenericEncodeOptions(RawGenericEncodeOptions value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Writer, "GenericEncode", "Generic encoding options are required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Writer);
    }

    /// <summary>Validates category selection, mode, recovery policy, and limits.</summary>
    public RawValidationReport ValidateValidationOptions(RawValidationOptions value)
    {
        RawPublicValidation.RequireOptions(value);
        return value.Validate();
    }

    /// <summary>Validates a descriptor through selected structural validation.</summary>
    public RawValidationReport ValidateDescriptorModel(RawDescriptor value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Structural, "Descriptor", "A descriptor is required.");
        return RawPublicValidation.FromReport(value.Validate(options.Limits), options, RawValidationCategory.Structural);
    }

    /// <summary>Validates a profile document through selected profile validation.</summary>
    public RawValidationReport ValidateProfileDocumentModel(RawProfileSchemaDocument value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Profile, "ProfileDocument", "A profile document is required.");
        return RawPublicValidation.FromReport(RawProfileSchemaValidation.Validate(value, options.Limits), options, RawValidationCategory.Profile);
    }

    /// <summary>Validates an XMP packet through selected metadata validation.</summary>
    public RawValidationReport ValidateXmpPacket(RawXmpPacket value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Metadata, "Xmp", "An XMP packet is required.");
        return RawPublicValidation.FromReport(RawPublicValidation.ValidateXmpPacket(value, options.Limits), options, RawValidationCategory.Metadata);
    }

    /// <summary>Validates an IPTC record through selected metadata validation.</summary>
    public RawValidationReport ValidateIptcRecord(RawIptcRecord value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Metadata, "Iptc", "An IPTC record is required.");
        return RawPublicValidation.FromReport(RawPublicValidation.ValidateIptcRecord(value, options.Limits), options, RawValidationCategory.Metadata);
    }

    /// <summary>Validates an ICC profile through selected metadata validation.</summary>
    public RawValidationReport ValidateIccProfileModel(RawIccProfile value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Metadata, "Icc", "An ICC profile is required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Metadata);
    }

    /// <summary>Validates a maker note through selected metadata validation.</summary>
    public RawValidationReport ValidateMakerNoteModel(RawMakerNote value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Metadata, "MakerNote", "A maker note is required.");
        return RawPublicValidation.FromReport(value.Validate(options.Limits), options, RawValidationCategory.Metadata);
    }

    /// <summary>Validates an image area through selected semantic validation.</summary>
    public RawValidationReport ValidateArea(RawArea value, int imageWidth, int imageHeight, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Area", "An image area is required.");
        return RawPublicValidation.FromReport(value.Validate(imageWidth, imageHeight), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates plane identity and geometry through selected semantic validation.</summary>
    public RawValidationReport ValidatePlaneInfo(RawPlaneInfo value, RawValidationOptions options)
    {
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Plane", "A plane model is required.");
        return RawPublicValidation.FromReport(value.Validate(), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates image metadata through selected semantic validation.</summary>
    public RawValidationReport ValidateImageInfo(RawImageInfo value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "ImageInfo", "Image metadata is required.");
        return RawPublicValidation.FromReport(value.Validate(options.Limits), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates exact stored samples through selected semantic validation.</summary>
    public RawValidationReport ValidateStoredImage(RawStoredImage value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "StoredImage", "A stored image is required.");
        return RawPublicValidation.FromReport(value.Validate(options.Limits), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates normalized sensor samples through selected semantic validation.</summary>
    public RawValidationReport ValidateSensorImage(RawSensorImage value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "SensorImage", "A sensor image is required.");
        return RawPublicValidation.FromReport(value.Validate(options.Limits), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates rendered RGB samples through selected semantic validation.</summary>
    public RawValidationReport ValidateRgbImage(RawRgbImage value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "RgbImage", "An RGB image is required.");
        return RawPublicValidation.FromReport(value.Validate(options.Limits), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates a thumbnail through selected semantic validation.</summary>
    public RawValidationReport ValidateThumbnail(RawThumbnail value, RawValidationOptions options)
    {
        RawPublicValidation.RequireOptions(options);
        if (value == null) return RawPublicValidation.Missing(options, RawValidationCategory.Semantic, "Thumbnail", "A thumbnail is required.");
        return RawPublicValidation.FromReport(value.Validate(options.Limits), options, RawValidationCategory.Semantic);
    }

    /// <summary>Validates a dry-run plan through selected profile and writer validation.</summary>
    public RawValidationReport ValidateWritePlan(RawWritePlan value, RawValidationOptions options)
    {
        return RawCodec.Validate(value, options);
    }

    /// <summary>Reports whether a validation result contains no errors.</summary>
    public bool ValidationReportIsValid(RawValidationReport report)
    {
        return report != null && report.IsValid();
    }

    /// <summary>Reports whether selected categories conform in strict or tolerant mode.</summary>
    public bool ValidationReportIsConformant(RawValidationReport report)
    {
        return report != null && report.IsConformant();
    }

    /// <summary>Reports whether a validation result contains warnings.</summary>
    public bool ValidationReportHasWarnings(RawValidationReport report)
    {
        return report != null && report.HasWarnings();
    }

    /// <summary>Returns the stable summary for one validation category.</summary>
    public RawValidationCategoryReport? FindValidationCategory(RawValidationReport report, RawValidationCategory category)
    {
        if (report == null) return null;
        return report.GetCategory(category);
    }

    /// <summary>Estimates black calibration from declared optical-black samples.</summary>
    public RawBlackLevel EstimateOpticalBlackLevel(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        return RawRenderer.EstimateBlackLevel(storedImage, options);
    }

    /// <summary>Normalizes exact stored values through calibration, crop modeling, and clipping detection.</summary>
    public RawSensorImage PreprocessStoredImage(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        return RawRenderer.Preprocess(storedImage, options);
    }

    /// <summary>Preprocesses retained exact values from an existing decoded sensor image.</summary>
    public RawSensorImage PreprocessSensorImage(RawSensorImage sensorImage, RawNormalizationOptions options)
    {
        return RawRenderer.Preprocess(sensorImage, options);
    }

    /// <summary>Normalizes exact stored values through calibration, crop modeling, and clipping detection.</summary>
    public RawSensorImage Preprocess(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        return RawRenderer.Preprocess(storedImage, options);
    }

    /// <summary>Repairs declared or detected sensor defects.</summary>
    public RawSensorImage RepairDefects(RawSensorImage sensorImage, RawDefectOptions options)
    {
        return RawRenderer.RepairDefects(sensorImage, options);
    }

    /// <summary>Applies embedded raw-domain lens corrections.</summary>
    public RawSensorImage CorrectLens(RawSensorImage sensorImage, RawLensCorrectionOptions options)
    {
        return RawRenderer.CorrectLens(sensorImage, options);
    }

    /// <summary>Creates a fast half-size preview from a compatible Bayer sensor image.</summary>
    public RawRgbImage CreateHalfSizePreview(RawSensorImage sensorImage, RawPreviewOptions options)
    {
        return RawRenderer.CreateHalfSizePreview(sensorImage, options);
    }

    /// <summary>Computes raw statistics and diagnostic planes.</summary>
    public RawAnalysisResult Analyze(RawSensorImage sensorImage, RawAnalysisOptions options)
    {
        return RawRenderer.Analyze(sensorImage, options);
    }

    /// <summary>Resolves stored, active, default, and caller crop precedence.</summary>
    public RawCropModel ResolveCrop(RawImageInfo info, RawArea? userCrop)
    {
        return RawRenderer.GetCropModel(info, userCrop);
    }

    /// <summary>Reconstructs cropped camera-linear samples or demosaics a CFA sensor image.</summary>
    public RawRgbImage ReconstructSensorImage(RawSensorImage sensorImage, RawRenderOptions options)
    {
        return RawRenderer.Reconstruct(sensorImage, options);
    }

    /// <summary>Applies the selected white balance to camera-linear RGB samples.</summary>
    public RawRgbImage ApplyWhiteBalance(RawRgbImage image, RawWhiteBalance? cameraWhiteBalance, RawRenderOptions options)
    {
        return RawRenderer.ApplyWhiteBalance(image, cameraWhiteBalance, options);
    }

    /// <summary>Converts camera-linear RGB through matrix profiles into D50 or D65 XYZ.</summary>
    public RawRgbImage ConvertCameraToXyz(RawRgbImage image, RawColorProfile[] profiles, double illuminantTemperatureKelvin, RawRenderOptions options)
    {
        return RawRenderer.ConvertCameraToXyz(image, profiles, illuminantTemperatureKelvin, options);
    }

    /// <summary>Converts linear XYZ or raw-camera RGB into a selected built-in output color space.</summary>
    public RawRgbImage ConvertOutputColor(RawRgbImage image, RawRenderOptions options)
    {
        return RawRenderer.ConvertOutputColor(image, options);
    }

    /// <summary>Converts ICC RGB or RGBA samples through an input profile to linear D50 XYZ.</summary>
    public RawRgbImage ConvertIccToXyz(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options)
    {
        return RawRenderer.ConvertIccToXyz(image, profile, options);
    }

    /// <summary>Converts linear D50 XYZ or XYZA samples through an output ICC profile.</summary>
    public RawRgbImage ConvertXyzToIcc(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options)
    {
        return RawRenderer.ConvertXyzToIcc(image, profile, options);
    }

    /// <summary>Applies exposure, brightness, shadow, saturation, and highlight controls.</summary>
    public RawRgbImage ApplyExposureAndHighlights(RawRgbImage image, RawRenderOptions options)
    {
        return RawRenderer.ApplyExposureAndHighlights(image, options);
    }

    /// <summary>Applies managed detail enhancement and optional chromatic-aberration correction.</summary>
    public RawRgbImage EnhanceDetails(RawRgbImage image, RawChromaticAberration? chromaticAberration, RawRenderOptions options)
    {
        return RawRenderer.EnhanceDetails(image, chromaticAberration, options);
    }

    /// <summary>Applies tone-curve, contrast, gamut, and transfer-function processing.</summary>
    public RawRgbImage ApplyToneAndGamut(RawRgbImage image, RawRenderOptions options)
    {
        return RawRenderer.ApplyToneAndGamut(image, options);
    }

    /// <summary>Applies crop, orientation, resampling, and final component-depth conversion.</summary>
    public RawRgbImage CreateOutputImage(RawRgbImage image, RawArea? crop, RawRenderOptions options)
    {
        return RawRenderer.CreateOutput(image, crop, options);
    }

    /// <summary>Computes bounded per-channel histograms over exact stored samples.</summary>
    public RawHistogramResult HistogramStoredImage(RawStoredImage storedImage, RawHistogramOptions options)
    {
        return RawRenderer.Histogram(storedImage, options);
    }

    /// <summary>Computes bounded per-channel histograms over normalized sensor samples.</summary>
    public RawHistogramResult HistogramSensorImage(RawSensorImage sensorImage, RawHistogramOptions options)
    {
        return RawRenderer.Histogram(sensorImage, options);
    }

    /// <summary>Computes bounded per-channel histograms over rendered samples.</summary>
    public RawHistogramResult HistogramRenderedImage(RawRgbImage image, RawHistogramOptions options)
    {
        return RawRenderer.Histogram(image, options);
    }

    /// <summary>Computes bounded per-channel histograms over normalized sensor samples.</summary>
    public RawHistogramResult Histogram(RawSensorImage sensorImage, RawHistogramOptions options)
    {
        return RawRenderer.Histogram(sensorImage, options);
    }

    /// <summary>Converts floating samples between two RGB ICC profiles.</summary>
    public RawRgbImage ConvertIcc(RawRgbImage image, RawIccProfile inputProfile, RawIccProfile outputProfile, RawIccTransformOptions options)
    {
        return RawRenderer.ConvertIcc(image, inputProfile, outputProfile, options);
    }

    /// <summary>Creates a tightly stored grayscale generic raw descriptor.</summary>
    public RawDescriptor CreateGrayDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return RawDescriptor.CreateGray(width, height, bitDepth, storedWordBits, byteOrder);
    }

    /// <summary>Creates a tightly interleaved RGB generic raw descriptor.</summary>
    public RawDescriptor CreateRgbDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return RawDescriptor.CreateRgb(width, height, bitDepth, storedWordBits, byteOrder);
    }

    /// <summary>Creates a tightly interleaved BGR generic raw descriptor.</summary>
    public RawDescriptor CreateBgrDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return RawDescriptor.CreateBgr(width, height, bitDepth, storedWordBits, byteOrder);
    }

    /// <summary>Creates a tightly interleaved RGBA generic raw descriptor.</summary>
    public RawDescriptor CreateRgbaDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return RawDescriptor.CreateRgba(width, height, bitDepth, storedWordBits, byteOrder);
    }

    /// <summary>Creates a tightly interleaved BGRA generic raw descriptor.</summary>
    public RawDescriptor CreateBgraDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return RawDescriptor.CreateBgra(width, height, bitDepth, storedWordBits, byteOrder);
    }

    /// <summary>Creates a tightly stored byte-aligned standard Bayer descriptor.</summary>
    public RawDescriptor CreateBayerDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder, RawCfaLayout layout)
    {
        return RawDescriptor.CreateBayer(width, height, bitDepth, storedWordBits, byteOrder, layout);
    }

    /// <summary>Creates a tightly stored byte-aligned arbitrary-CFA descriptor.</summary>
    public RawDescriptor CreateArbitraryCfaDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder, int repeatWidth, int repeatHeight, RawCfaColor[] colors)
    {
        return RawDescriptor.CreateArbitraryCfa(width, height, bitDepth, storedWordBits, byteOrder, repeatWidth, repeatHeight, colors);
    }

    /// <summary>Creates a tightly RAW10-packed standard Bayer descriptor.</summary>
    public RawDescriptor CreateRaw10Descriptor(int width, int height, RawCfaLayout layout)
    {
        return RawDescriptor.CreatePackedBayer(width, height, RawPacking.Raw10, layout);
    }

    /// <summary>Creates a tightly RAW12-packed standard Bayer descriptor.</summary>
    public RawDescriptor CreateRaw12Descriptor(int width, int height, RawCfaLayout layout)
    {
        return RawDescriptor.CreatePackedBayer(width, height, RawPacking.Raw12, layout);
    }

    /// <summary>Creates a tightly RAW14-packed standard Bayer descriptor.</summary>
    public RawDescriptor CreateRaw14Descriptor(int width, int height, RawCfaLayout layout)
    {
        return RawDescriptor.CreatePackedBayer(width, height, RawPacking.Raw14, layout);
    }

    /// <summary>Validates descriptor fields and storage geometry against resource limits.</summary>
    public RawValidationReport ValidateDescriptor(RawDescriptor descriptor, RawLimits limits)
    {
        if (limits == null)
        {
            return CreateServiceValidation(RawValidationCategory.Semantic, RawFormat.GenericRaw, "Descriptor.Limits", "Resource limits are required.");
        }
        RawValidationReport limitReport = limits.Validate();
        if (!limitReport.IsValid())
        {
            return limitReport;
        }
        if (descriptor == null)
        {
            return CreateServiceValidation(RawValidationCategory.Semantic, RawFormat.GenericRaw, "Descriptor", "A generic raw descriptor is required.");
        }
        return descriptor.Validate(limits);
    }

    /// <summary>Validates a descriptor against an exact bounded source length.</summary>
    public RawValidationReport ValidateDescriptorSourceLength(RawDescriptor descriptor, long sourceLength, RawLimits limits)
    {
        if (limits == null)
        {
            return CreateServiceValidation(RawValidationCategory.Semantic, RawFormat.GenericRaw, "Descriptor.Limits", "Resource limits are required.");
        }
        RawValidationReport limitReport = limits.Validate();
        if (!limitReport.IsValid())
        {
            return limitReport;
        }
        if (descriptor == null)
        {
            return CreateServiceValidation(RawValidationCategory.Semantic, RawFormat.GenericRaw, "Descriptor", "A generic raw descriptor is required.");
        }
        return descriptor.ValidateSourceLength(sourceLength, limits);
    }

    /// <summary>Computes the bounded minimum stored bytes in one descriptor row.</summary>
    public long GetDescriptorMinimumRowBytes(RawDescriptor descriptor, RawLimits limits)
    {
        RequireValidDescriptor(descriptor, limits);
        return descriptor.GetMinimumRowBytes();
    }

    /// <summary>Computes the bounded bytes required through the final stored row and plane.</summary>
    public long GetDescriptorRequiredDataLength(RawDescriptor descriptor, RawLimits limits)
    {
        RequireValidDescriptor(descriptor, limits);
        return descriptor.GetRequiredDataLength();
    }

    /// <summary>Computes the bounded intentional trailing bytes after stored sample geometry.</summary>
    public long GetDescriptorTrailingByteCount(RawDescriptor descriptor, RawLimits limits)
    {
        RequireValidDescriptor(descriptor, limits);
        return descriptor.GetTrailingByteCount();
    }

    /// <summary>Creates one canonical standard Bayer CFA pattern.</summary>
    public RawCfaPattern CreateBayerCfaPattern(RawCfaLayout layout)
    {
        return RawCfaPattern.CreateBayer(layout);
    }

    /// <summary>Creates one arbitrary repeating CFA pattern.</summary>
    public RawCfaPattern CreateArbitraryCfaPattern(int repeatWidth, int repeatHeight, RawCfaColor[] colors)
    {
        return RawCfaPattern.CreateArbitrary(repeatWidth, repeatHeight, colors);
    }

    /// <summary>Validates CFA dimensions, colors, and named-layout identity.</summary>
    public RawValidationReport ValidateCfaPattern(RawCfaPattern pattern)
    {
        if (pattern == null)
        {
            return CreateServiceValidation(RawValidationCategory.Semantic, RawFormat.GenericRaw, "Cfa", "A CFA pattern is required.");
        }
        return pattern.Validate();
    }

    /// <summary>Queries the repeating CFA color at one nonnegative sensor coordinate.</summary>
    public RawCfaColor GetCfaColor(RawCfaPattern pattern, int x, int y)
    {
        if (pattern == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A CFA pattern is required.", -1);
        }
        return pattern.GetColor(x, y);
    }

    /// <summary>Creates an exact unsigned TIFF rational.</summary>
    public RawRational CreateUnsignedRational(uint numerator, uint denominator)
    {
        return new RawRational(numerator, denominator);
    }

    /// <summary>Creates an exact signed TIFF rational.</summary>
    public RawRational CreateSignedRational(int numerator, int denominator)
    {
        return RawRational.CreateSigned(numerator, denominator);
    }

    /// <summary>Validates a rational denominator and its declared signed or unsigned component ranges.</summary>
    public RawValidationReport ValidateRational(RawRational value)
    {
        if (value == null)
        {
            return CreateServiceValidation(RawValidationCategory.Metadata, RawFormat.Tiff, "Rational", "A rational value is required.");
        }
        return value.Validate();
    }

    /// <summary>Tests rational signedness and exact stored-component equality.</summary>
    public bool RationalsEqualExact(RawRational first, RawRational? second)
    {
        RequireRational(first);
        return first.EqualsExact(second);
    }

    /// <summary>Tests exact numeric equality of two valid rationals.</summary>
    public bool RationalsEqualValue(RawRational first, RawRational? second)
    {
        RequireRational(first);
        return first.EqualsValue(second);
    }

    /// <summary>Converts a valid rational to double precision.</summary>
    public double ConvertRationalToDouble(RawRational value)
    {
        RequireRational(value);
        return value.ToDouble();
    }

    /// <summary>Converts a valid rational to single precision.</summary>
    public float ConvertRationalToSingle(RawRational value)
    {
        RequireRational(value);
        return value.ToSingle();
    }

    /// <summary>Creates an exact TIFF BYTE value.</summary>
    public RawTagValue CreateByteTagValue(byte[] values)
    {
        return RawTagValue.CreateByte(values);
    }

    /// <summary>Creates a null-terminated TIFF ASCII value.</summary>
    public RawTagValue CreateAsciiTagValue(string text)
    {
        return RawTagValue.CreateAscii(text);
    }

    /// <summary>Creates an exact TIFF SHORT value.</summary>
    public RawTagValue CreateUnsignedShortTagValue(ushort[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateUInt16(values, byteOrder);
    }

    /// <summary>Creates an exact TIFF LONG value.</summary>
    public RawTagValue CreateUnsignedLongTagValue(uint[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateUInt32(values, byteOrder);
    }

    /// <summary>Creates an exact TIFF RATIONAL value.</summary>
    public RawTagValue CreateUnsignedRationalTagValue(RawRational[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateUnsignedRational(values, byteOrder);
    }

    /// <summary>Creates an exact TIFF SBYTE value.</summary>
    public RawTagValue CreateSignedByteTagValue(sbyte[] values)
    {
        return RawTagValue.CreateSignedByte(values);
    }

    /// <summary>Creates an exact TIFF UNDEFINED value.</summary>
    public RawTagValue CreateUndefinedTagValue(byte[] values)
    {
        return RawTagValue.CreateUndefined(values);
    }

    /// <summary>Creates an exact TIFF SSHORT value.</summary>
    public RawTagValue CreateSignedShortTagValue(short[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateInt16(values, byteOrder);
    }

    /// <summary>Creates an exact TIFF SLONG value.</summary>
    public RawTagValue CreateSignedLongTagValue(int[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateInt32(values, byteOrder);
    }

    /// <summary>Creates an exact TIFF SRATIONAL value.</summary>
    public RawTagValue CreateSignedRationalTagValue(RawRational[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateSignedRational(values, byteOrder);
    }

    /// <summary>Creates an exact TIFF FLOAT value.</summary>
    public RawTagValue CreateFloatTagValue(float[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateSingle(values, byteOrder);
    }

    /// <summary>Creates an exact TIFF DOUBLE value.</summary>
    public RawTagValue CreateDoubleTagValue(double[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateDouble(values, byteOrder);
    }

    /// <summary>Creates an exact classic TIFF IFD-offset value.</summary>
    public RawTagValue CreateIfdTagValue(uint[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateIfd(values, byteOrder);
    }

    /// <summary>Creates an exact BigTIFF LONG8 value.</summary>
    public RawTagValue CreateUnsignedLong8TagValue(ulong[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateUInt64(values, byteOrder);
    }

    /// <summary>Creates an exact BigTIFF SLONG8 value.</summary>
    public RawTagValue CreateSignedLong8TagValue(long[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateInt64(values, byteOrder);
    }

    /// <summary>Creates an exact BigTIFF IFD8-offset value.</summary>
    public RawTagValue CreateIfd8TagValue(ulong[] values, RawByteOrder byteOrder)
    {
        return RawTagValue.CreateIfd8(values, byteOrder);
    }

    /// <summary>Creates a null-terminated Exif 3 UTF-8 value.</summary>
    public RawTagValue CreateUtf8TagValue(string text)
    {
        return RawTagValue.CreateUtf8(text);
    }

    /// <summary>Creates an Exif character-prefixed UNDEFINED text value.</summary>
    public RawTagValue CreateExifPrefixedTextTagValue(string text, RawTextEncoding encoding, RawByteOrder byteOrder)
    {
        return RawTagValue.CreatePrefixedText(text, encoding, byteOrder);
    }

    /// <summary>Copies a tag value's exact encoded bytes.</summary>
    public byte[] CopyTagValueBytes(RawTagValue value)
    {
        RequireTagValue(value);
        return value.CopyBytes();
    }

    /// <summary>Extracts exact TIFF BYTE values.</summary>
    public byte[] GetByteTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Byte);
        return value.GetByteValues();
    }

    /// <summary>Decodes a TIFF ASCII value with exact encoding details.</summary>
    public RawTextValue GetAsciiTagText(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Ascii);
        return value.GetText();
    }

    /// <summary>Extracts exact TIFF SHORT values.</summary>
    public ushort[] GetUnsignedShortTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Short);
        return value.GetUInt16Values();
    }

    /// <summary>Extracts exact TIFF LONG values.</summary>
    public uint[] GetUnsignedLongTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Long);
        return value.GetUInt32Values();
    }

    /// <summary>Extracts exact TIFF RATIONAL values.</summary>
    public RawRational[] GetUnsignedRationalTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Rational);
        return value.GetRationalValues();
    }

    /// <summary>Extracts exact TIFF SBYTE values.</summary>
    public sbyte[] GetSignedByteTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.SignedByte);
        return value.GetSignedByteValues();
    }

    /// <summary>Copies exact TIFF UNDEFINED bytes.</summary>
    public byte[] GetUndefinedTagBytes(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Undefined);
        return value.CopyBytes();
    }

    /// <summary>Extracts exact TIFF SSHORT values.</summary>
    public short[] GetSignedShortTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.SignedShort);
        return value.GetInt16Values();
    }

    /// <summary>Extracts exact TIFF SLONG values.</summary>
    public int[] GetSignedLongTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.SignedLong);
        return value.GetInt32Values();
    }

    /// <summary>Extracts exact TIFF SRATIONAL values.</summary>
    public RawRational[] GetSignedRationalTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.SignedRational);
        return value.GetRationalValues();
    }

    /// <summary>Extracts bit-exact TIFF FLOAT values.</summary>
    public float[] GetFloatTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Float);
        return value.GetSingleValues();
    }

    /// <summary>Extracts bit-exact TIFF DOUBLE values.</summary>
    public double[] GetDoubleTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Double);
        return value.GetDoubleValues();
    }

    /// <summary>Extracts exact classic TIFF IFD offsets.</summary>
    public uint[] GetIfdTagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Ifd);
        return value.GetUInt32Values();
    }

    /// <summary>Extracts exact BigTIFF LONG8 values.</summary>
    public ulong[] GetUnsignedLong8TagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Long8);
        return value.GetUInt64Values();
    }

    /// <summary>Extracts exact BigTIFF SLONG8 values.</summary>
    public long[] GetSignedLong8TagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.SignedLong8);
        return value.GetInt64Values();
    }

    /// <summary>Extracts exact BigTIFF IFD8 offsets.</summary>
    public ulong[] GetIfd8TagValues(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Ifd8);
        return value.GetUInt64Values();
    }

    /// <summary>Decodes an Exif 3 UTF-8 value with exact encoding details.</summary>
    public RawTextValue GetUtf8TagText(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Utf8);
        return value.GetText();
    }

    /// <summary>Decodes an Exif character-prefixed UNDEFINED value with exact encoding details.</summary>
    public RawTextValue GetExifPrefixedTagText(RawTagValue value)
    {
        RequireTagType(value, RawTagType.Undefined);
        return value.GetText();
    }

    /// <summary>Finds a metadata directory by exact stable path.</summary>
    public RawIfd? FindMetadataIfdByPath(RawMetadata metadata, string path)
    {
        RequireMetadata(metadata);
        return metadata.FindIfd(path);
    }

    /// <summary>Finds a tag identifier within an exact stable directory path.</summary>
    public RawTag? FindMetadataTagByPathAndId(RawMetadata metadata, string path, ushort tagId)
    {
        RequireMetadata(metadata);
        RawIfd? ifd = metadata.FindIfd(path);
        return ifd == null ? null : ifd.FindTag(tagId);
    }

    /// <summary>Finds the first tag identifier within a semantic directory kind.</summary>
    public RawTag? FindMetadataTagByKindAndId(RawMetadata metadata, RawIfdKind kind, ushort tagId)
    {
        RequireMetadata(metadata);
        return ExifCodec.FindTag(metadata, kind, tagId);
    }

    /// <summary>Finds the first named tag in deterministic metadata graph order.</summary>
    public RawTag? FindMetadataTagByName(RawMetadata metadata, string name)
    {
        RequireMetadata(metadata);
        return metadata.FindTag(name);
    }

    /// <summary>Creates an Exif timestamp from an offset-aware .NET value.</summary>
    public RawExifTimestamp CreateExifTimestampFromDateTimeOffset(System.DateTimeOffset value, int subsecondDigits)
    {
        return RawExifTimestamp.Create(value, subsecondDigits);
    }

    /// <summary>Converts a valid offset-bearing Exif timestamp to a .NET value.</summary>
    public System.DateTimeOffset ConvertExifTimestampToDateTimeOffset(RawExifTimestamp timestamp)
    {
        RequireExifTimestamp(timestamp);
        return timestamp.ToDateTimeOffset();
    }

    /// <summary>Encodes an Exif timestamp's date-time component.</summary>
    public RawTagValue CreateExifDateTimeTagValue(RawExifTimestamp timestamp)
    {
        RequireExifTimestamp(timestamp);
        return timestamp.EncodeDateTime();
    }

    /// <summary>Encodes an Exif timestamp's UTC-offset component.</summary>
    public RawTagValue CreateExifOffsetTagValue(RawExifTimestamp timestamp)
    {
        RequireExifTimestamp(timestamp);
        return timestamp.EncodeOffset();
    }

    /// <summary>Encodes an Exif timestamp's subsecond component.</summary>
    public RawTagValue CreateExifSubsecondsTagValue(RawExifTimestamp timestamp)
    {
        RequireExifTimestamp(timestamp);
        return timestamp.EncodeSubseconds();
    }

    /// <summary>Parses a bounded standalone XMP packet with safe XML settings.</summary>
    public RawXmpPacket ParseXmpPacket(byte[] rawBytes, RawLimits limits)
    {
        return RawXmpPacket.Parse(rawBytes, limits);
    }

    /// <summary>Returns a validated independent encoding of a standalone XMP packet.</summary>
    public byte[] EncodeXmpPacketBytes(RawXmpPacket packet, RawLimits limits)
    {
        RequireXmpPacket(packet);
        return XmpCodec.Parse(packet.CopyBytes(), limits, packet.Offset, packet.IfdPath).CopyBytes();
    }

    /// <summary>Finds the first exact namespace, name, and optional-language XMP field.</summary>
    public RawXmpField? FindXmpField(RawXmpPacket packet, string namespaceUri, string name, string? language)
    {
        RequireXmpPacket(packet);
        return packet.GetField(namespaceUri, name, language);
    }

    /// <summary>Finds every exact namespace and name XMP field in encounter order.</summary>
    public RawXmpField[] FindXmpFields(RawXmpPacket packet, string namespaceUri, string name)
    {
        RequireXmpPacket(packet);
        return packet.GetFields(namespaceUri, name);
    }

    /// <summary>Replaces XMP root XML while retaining encoding, wrapper, and padding.</summary>
    public RawXmpPacket ReplaceXmpPacketXml(RawXmpPacket packet, string xml, RawLimits limits)
    {
        RequireXmpPacket(packet);
        return packet.ReplaceXml(xml, limits);
    }

    /// <summary>Replaces XMP root XML while retaining the exact total packet length.</summary>
    public RawXmpPacket ReplaceXmpPacketXmlFixedSize(RawXmpPacket packet, string xml, RawLimits limits)
    {
        RequireXmpPacket(packet);
        return packet.ReplaceXmlFixedSize(xml, limits);
    }

    /// <summary>Encodes a standalone XMP packet as a TIFF BYTE value.</summary>
    public RawTagValue CreateXmpTagValue(RawXmpPacket packet, RawLimits limits)
    {
        return RawTagValue.CreateByte(EncodeXmpPacketBytes(packet, limits));
    }

    /// <summary>Parses a bounded standalone IPTC IIM record.</summary>
    public RawIptcRecord ParseIptcRecord(byte[] rawBytes, RawLimits limits)
    {
        return RawIptcRecord.Parse(rawBytes, limits);
    }

    /// <summary>Creates an uninterpreted IPTC IIM DataSet.</summary>
    public RawIptcDataset CreateBinaryIptcDataset(byte recordNumber, byte datasetNumber, byte[] bytes)
    {
        return RawIptcDataset.CreateBinary(recordNumber, datasetNumber, bytes);
    }

    /// <summary>Creates an ASCII or UTF-8 IPTC IIM text DataSet.</summary>
    public RawIptcDataset CreateTextIptcDataset(byte recordNumber, byte datasetNumber, string text, bool utf8)
    {
        return RawIptcDataset.CreateText(recordNumber, datasetNumber, text, utf8);
    }

    /// <summary>Creates a two-octet unsigned IPTC IIM DataSet.</summary>
    public RawIptcDataset CreateUnsignedIptcDataset(byte recordNumber, byte datasetNumber, ushort value)
    {
        return RawIptcDataset.CreateUnsignedInteger(recordNumber, datasetNumber, value);
    }

    /// <summary>Creates an eight-digit IPTC IIM date DataSet.</summary>
    public RawIptcDataset CreateDateIptcDataset(byte recordNumber, byte datasetNumber, System.DateTime value)
    {
        return RawIptcDataset.CreateDate(recordNumber, datasetNumber, value);
    }

    /// <summary>Creates an IPTC IIM time DataSet with an optional UTC offset.</summary>
    public RawIptcDataset CreateTimeIptcDataset(byte recordNumber, byte datasetNumber, int hour, int minute, int second, bool hasUtcOffset, int utcOffsetMinutes)
    {
        return RawIptcDataset.CreateTime(recordNumber, datasetNumber, hour, minute, second, hasUtcOffset, utcOffsetMinutes);
    }

    /// <summary>Returns an independent copy of an IPTC DataSet's exact value bytes.</summary>
    public byte[] CopyIptcDatasetBytes(RawIptcDataset dataset)
    {
        RequireIptcDataset(dataset);
        return dataset.CopyBytes();
    }

    /// <summary>Creates a canonical standalone IPTC IIM record from ordered DataSets.</summary>
    public RawIptcRecord CreateIptcRecord(RawIptcDataset[] datasets, RawLimits limits)
    {
        return RawIptcRecord.Create(datasets, limits);
    }

    /// <summary>Returns an independent copy of the original IPTC IIM bytes including padding.</summary>
    public byte[] CopyIptcRecordBytes(RawIptcRecord record)
    {
        RequireIptcRecord(record);
        return record.CopyBytes();
    }

    /// <summary>Canonically encodes an IPTC IIM record's ordered DataSets without container padding.</summary>
    public byte[] EncodeIptcRecordBytes(RawIptcRecord record, RawLimits limits)
    {
        RequireIptcRecord(record);
        return record.Encode(limits);
    }

    /// <summary>Finds the first IPTC DataSet with an exact record and DataSet number.</summary>
    public RawIptcDataset? FindIptcDataset(RawIptcRecord record, byte recordNumber, byte datasetNumber)
    {
        RequireIptcRecord(record);
        return record.GetDataset(recordNumber, datasetNumber);
    }

    /// <summary>Finds every repeatable IPTC DataSet with exact record and DataSet numbers.</summary>
    public RawIptcDataset[] FindIptcDatasets(RawIptcRecord record, byte recordNumber, byte datasetNumber)
    {
        RequireIptcRecord(record);
        return record.GetDatasets(recordNumber, datasetNumber);
    }

    /// <summary>Replaces one ordered IPTC DataSet while retaining all others and trailing padding.</summary>
    public RawIptcRecord ReplaceIptcDatasetAtIndex(RawIptcRecord record, int index, RawIptcDataset replacement, RawLimits limits)
    {
        RequireIptcRecord(record);
        RequireIptcDataset(replacement);
        if (index < 0 || index >= (record.Datasets ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The IPTC DataSet replacement index is outside the record.", -1);
        }
        RawIptcDataset[] datasets = new RawIptcDataset[record.Datasets.Length];
        record.Datasets.CopyTo(datasets, 0);
        datasets[index] = replacement;
        byte[] encoded = IptcCodec.Encode(datasets, limits);
        byte[] padding = record.PaddingBytes ?? throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Complete IPTC padding information is required.", record.Offset);
        long total = CheckedMath.Add(encoded.Length, padding.Length);
        if (total > limits.MaxMetadataBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The replaced IPTC IIM payload exceeds the configured metadata limit.", record.Offset);
        }
        CheckedMath.AllocationSize(total, 1, limits);
        byte[] combined = new byte[CheckedMath.ToInt32(total)];
        encoded.CopyTo(combined, 0);
        padding.CopyTo(combined, encoded.Length);
        return IptcCodec.Parse(combined, limits, record.Offset, record.IfdPath == null ? string.Empty : record.IfdPath);
    }

    /// <summary>Encodes an IPTC IIM record as a TIFF UNDEFINED value.</summary>
    public RawTagValue CreateIptcTagValue(RawIptcRecord record, RawLimits limits)
    {
        RequireIptcRecord(record);
        return record.ToTagValue(limits);
    }

    /// <summary>Parses and validates a bounded standalone ICC v2 or v4 profile.</summary>
    public RawIccProfile ParseIccProfile(byte[] rawBytes, RawLimits limits)
    {
        return RawIccProfile.Parse(rawBytes, limits);
    }

    /// <summary>Returns a validated independent encoding of an ICC profile.</summary>
    public byte[] EncodeIccProfileBytes(RawIccProfile profile, RawLimits limits)
    {
        RequireIccProfile(profile);
        return IccCodec.Parse(profile.CopyBytes(), limits, profile.Offset, profile.IfdPath == null ? string.Empty : profile.IfdPath).CopyBytes();
    }

    /// <summary>Finds an ICC tag by its exact four-character signature.</summary>
    public RawIccTag? FindIccTag(RawIccProfile profile, string signature)
    {
        RequireIccProfile(profile);
        return profile.GetTag(signature);
    }

    /// <summary>Returns an independent copy of an ICC tag's exact data bytes.</summary>
    public byte[] CopyIccTagBytes(RawIccTag tag)
    {
        if (tag == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "An ICC tag is required.", -1);
        }
        return tag.CopyBytes();
    }

    /// <summary>Reports whether an ICC profile exposes a complete RGB matrix and tone curves.</summary>
    public bool IccProfileHasMatrixTrc(RawIccProfile profile)
    {
        RequireIccProfile(profile);
        return profile.HasMatrixTrc();
    }

    /// <summary>Validates the mutable public ICC profile model.</summary>
    public RawValidationReport ValidateIccProfile(RawIccProfile profile)
    {
        if (profile == null)
        {
            return CreateServiceValidation(RawValidationCategory.Metadata, RawFormat.Unknown, "Icc", "An ICC profile is required.");
        }
        return profile.Validate();
    }

    /// <summary>Replaces one ICC tag's data without changing its size or unrelated bytes.</summary>
    public RawIccProfile ReplaceIccTagDataFixedSize(RawIccProfile profile, string signature, byte[] replacementBytes, RawLimits limits)
    {
        RequireIccProfile(profile);
        return IccCodec.ReplaceTagDataFixedSize(profile, signature, replacementBytes, limits);
    }

    /// <summary>Encodes an ICC profile as a TIFF UNDEFINED value.</summary>
    public RawTagValue CreateIccTagValue(RawIccProfile profile, RawLimits limits)
    {
        return RawTagValue.CreateUndefined(EncodeIccProfileBytes(profile, limits));
    }

    /// <summary>Rebuilds the unified metadata view and conflict report using explicit options.</summary>
    public RawMetadata ReconcileMetadata(RawMetadata metadata, RawMetadataOptions options)
    {
        RequireMetadata(metadata);
        return metadata.Reconcile(options);
    }

    /// <summary>Finds one unified metadata field by stable name and optional language.</summary>
    public RawMetadataField? FindUnifiedMetadataField(RawMetadata metadata, string name, string? language)
    {
        RequireMetadata(metadata);
        return metadata.Get(name, language);
    }

    /// <summary>Creates an empty ordered metadata edit set.</summary>
    public RawMetadataEditSet CreateMetadataEditSet()
    {
        return new RawMetadataEditSet();
    }

    /// <summary>Appends an add operation for one metadata representation.</summary>
    public RawMetadataEditSet AddMetadataFieldEdit(RawMetadataEditSet editSet, string name, string[] values, string? language, RawMetadataSource source)
    {
        RequireMetadataEditSet(editSet);
        return editSet.Add(name, values, language, source);
    }

    /// <summary>Appends a replacement for one or every existing metadata representation.</summary>
    public RawMetadataEditSet ReplaceMetadataFieldEdit(RawMetadataEditSet editSet, string name, string[] values, string? language, RawMetadataSource source)
    {
        RequireMetadataEditSet(editSet);
        return editSet.Replace(name, values, language, source);
    }

    /// <summary>Appends removal of one or every existing metadata representation.</summary>
    public RawMetadataEditSet RemoveMetadataFieldEdit(RawMetadataEditSet editSet, string name, string? language, RawMetadataSource source)
    {
        RequireMetadataEditSet(editSet);
        return editSet.Remove(name, language, source);
    }

    /// <summary>Appends the selected conservative privacy scrub preset.</summary>
    public RawMetadataEditSet AddPrivacyScrubPreset(RawMetadataEditSet editSet, RawPrivacyScrubPreset preset)
    {
        RequireMetadataEditSet(editSet);
        return editSet.Scrub(preset);
    }

    /// <summary>Appends removal of capture, creation, and modification timestamps across all representations.</summary>
    public RawMetadataEditSet AddTimestampRemovalEdits(RawMetadataEditSet editSet)
    {
        RequireMetadataEditSet(editSet);
        editSet.Remove("DateCreated", null, RawMetadataSource.None);
        editSet.Remove("CreateDate", null, RawMetadataSource.None);
        editSet.Remove("ModifyDate", null, RawMetadataSource.None);
        return editSet;
    }

    /// <summary>Appends removal of GPS coordinates, altitude, and GPS date/time across all representations.</summary>
    public RawMetadataEditSet AddGpsRemovalEdits(RawMetadataEditSet editSet)
    {
        RequireMetadataEditSet(editSet);
        editSet.Remove("GPSLatitude", null, RawMetadataSource.None);
        editSet.Remove("GPSLongitude", null, RawMetadataSource.None);
        editSet.Remove("GPSAltitude", null, RawMetadataSource.None);
        editSet.Remove("GPSTimeStamp", null, RawMetadataSource.None);
        editSet.Remove("GPSDateStamp", null, RawMetadataSource.None);
        return editSet;
    }

    /// <summary>Appends a replacement preview with synchronized geometry and orientation metadata.</summary>
    public RawMetadataEditSet AddPreviewReplacementEdit(RawMetadataEditSet editSet, int index, RawThumbnailKind kind, int width, int height, RawOrientation orientation, string mimeType, byte[] encodedBytes)
    {
        RequireMetadataEditSet(editSet);
        return editSet.ReplacePreview(index, kind, width, height, orientation, mimeType, encodedBytes);
    }

    /// <summary>Plans metadata-model field edits without mutation.</summary>
    public RawMetadataEditReport DryRunMetadataEdits(RawMetadata metadata, RawMetadataEditSet editSet)
    {
        RequireMetadata(metadata);
        RequireMetadataEditSet(editSet);
        return editSet.ApplyToMetadata(metadata, true);
    }

    /// <summary>Atomically applies metadata-model field edits and stages them for preserving writers.</summary>
    public RawMetadataEditReport ApplyMetadataEdits(RawMetadata metadata, RawMetadataEditSet editSet)
    {
        RequireMetadata(metadata);
        RequireMetadataEditSet(editSet);
        return editSet.ApplyToMetadata(metadata, false);
    }

    /// <summary>Plans document metadata and preview edits without mutation.</summary>
    public RawMetadataEditReport DryRunDocumentMetadataEdits(RawDocument document, RawMetadataEditSet editSet)
    {
        RequireDocument(document);
        RequireMetadataEditSet(editSet);
        return editSet.Apply(document, true);
    }

    /// <summary>Atomically applies document metadata and preview edits for a preserving rewrite.</summary>
    public RawMetadataEditReport ApplyDocumentMetadataEdits(RawDocument document, RawMetadataEditSet editSet)
    {
        RequireDocument(document);
        RequireMetadataEditSet(editSet);
        return editSet.Apply(document, false);
    }

    /// <summary>Returns whether an edit report contains no error diagnostic.</summary>
    public bool MetadataEditSucceeded(RawMetadataEditReport report)
    {
        if (report == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A metadata edit report is required.", -1);
        }
        return report.Succeeded();
    }

    /// <summary>Parses standalone maker-note bytes using their containing TIFF context.</summary>
    public RawMakerNote ParseMakerNote(byte[] rawBytes, long offset, long entryOffset, string ifdPath, string manufacturer, long tiffBaseOffset, RawByteOrder tiffByteOrder, RawLimits limits)
    {
        return RawMakerNote.Parse(rawBytes, offset, entryOffset, ifdPath, manufacturer, tiffBaseOffset, tiffByteOrder, limits);
    }

    /// <summary>Returns an independent copy of complete maker-note bytes.</summary>
    public byte[] CopyMakerNoteBytes(RawMakerNote note)
    {
        RequireMakerNote(note);
        return note.CopyBytes();
    }

    /// <summary>Validates preserved maker-note bytes, offsets, and relocation evidence.</summary>
    public RawValidationReport ValidateMakerNote(RawMakerNote note, RawLimits limits)
    {
        RequireMakerNote(note);
        return note.Validate(limits);
    }

    /// <summary>Plans byte-exact anchoring, proven relocation, or safe rejection.</summary>
    public RawMakerNoteWritePlan PlanMakerNoteRelocation(RawMakerNote note, long plannedOffset, RawLimits limits)
    {
        RequireMakerNote(note);
        return note.PlanWrite(plannedOffset, limits);
    }

    /// <summary>Finds a unique Canon maker-note tag by identifier.</summary>
    public RawCanonMakerNoteTag? FindCanonMakerNoteTag(RawMakerNote note, ushort tagId)
    {
        RequireMakerNote(note);
        return note.FindCanonTag(tagId);
    }

    /// <summary>Returns an independent copy of exact Canon maker-note tag bytes.</summary>
    public byte[] CopyCanonMakerNoteTagBytes(RawCanonMakerNoteTag tag)
    {
        if (tag == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A Canon maker-note tag is required.", -1);
        }
        return tag.CopyBytes();
    }

    /// <summary>Decodes a reviewed fixed-width Canon ASCII tag.</summary>
    public string GetCanonMakerNoteTagText(RawCanonMakerNoteTag tag)
    {
        if (tag == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A Canon maker-note tag is required.", -1);
        }
        return tag.GetText();
    }

    /// <summary>Creates one version-gated Canon maker-note replacement.</summary>
    public RawCanonMakerNoteEdit CreateCanonMakerNoteEdit(ushort tagId, RawCanonMakerNoteVersion version, RawTagValue value)
    {
        RequireTagValue(value);
        return new RawCanonMakerNoteEdit(tagId, version, value);
    }

    /// <summary>Encodes reviewed Canon tag replacements without changing private layout or unknown bytes.</summary>
    public RawMakerNote ApplyCanonMakerNoteEdits(RawMakerNote note, RawCanonMakerNoteEdit[] edits, RawLimits limits)
    {
        RequireMakerNote(note);
        return note.EncodeCanon(edits, limits);
    }

    /// <summary>Finds a unique Nikon maker-note tag by identifier.</summary>
    public RawNikonMakerNoteTag? FindNikonMakerNoteTag(RawMakerNote note, ushort tagId)
    {
        RequireMakerNote(note);
        return note.FindNikonTag(tagId);
    }

    /// <summary>Returns an independent copy of exact Nikon maker-note tag bytes.</summary>
    public byte[] CopyNikonMakerNoteTagBytes(RawNikonMakerNoteTag tag)
    {
        if (tag == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A Nikon maker-note tag is required.", -1);
        }
        return tag.CopyBytes();
    }

    /// <summary>Decodes a reviewed fixed-width Nikon ASCII tag.</summary>
    public string GetNikonMakerNoteTagText(RawNikonMakerNoteTag tag)
    {
        if (tag == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A Nikon maker-note tag is required.", -1);
        }
        return tag.GetText();
    }

    /// <summary>Creates one version-gated Nikon maker-note replacement.</summary>
    public RawNikonMakerNoteEdit CreateNikonMakerNoteEdit(ushort tagId, RawNikonMakerNoteVersion version, RawTagValue value)
    {
        RequireTagValue(value);
        return new RawNikonMakerNoteEdit(tagId, version, value);
    }

    /// <summary>Encodes reviewed Nikon tag replacements without changing protected or unknown bytes.</summary>
    public RawMakerNote ApplyNikonMakerNoteEdits(RawMakerNote note, RawNikonMakerNoteEdit[] edits, RawLimits limits)
    {
        RequireMakerNote(note);
        return note.EncodeNikon(edits, limits);
    }

    internal RawValidationReport CreateServiceValidation(RawValidationCategory category, RawFormat format, string context, string message)
    {
        RawValidationMessage finding = new RawValidationMessage(category, RawDiagnosticCode.InvalidArgument, RawSeverity.Error, format, -1, context, message, false);
        return new RawValidationReport(true, new RawValidationMessage[] { finding });
    }

    internal void RequireValidDescriptor(RawDescriptor descriptor, RawLimits limits)
    {
        RawValidationReport report = ValidateDescriptor(descriptor, limits);
        if (report.IsValid())
        {
            return;
        }
        RawValidationMessage finding = (report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable."))[0];
        if (finding.Code == RawDiagnosticCode.ResourceLimitExceeded)
        {
            throw new RawLimitException(finding.Code, finding.Message, finding.Offset);
        }
        throw new RawFormatException(finding.Code, finding.Message, finding.Offset);
    }

    internal void RequireRational(RawRational value)
    {
        if (value == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A rational value is required.", -1);
        }
    }

    internal void RequireTagValue(RawTagValue value)
    {
        if (value == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A tag value is required.", -1);
        }
    }

    internal void RequireTagType(RawTagValue value, RawTagType type)
    {
        RequireTagValue(value);
        value.RequireType(type);
    }

    internal void RequireMetadata(RawMetadata metadata)
    {
        if (metadata == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Metadata is required.", -1);
        }
    }

    internal void RequireExifTimestamp(RawExifTimestamp timestamp)
    {
        if (timestamp == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "An Exif timestamp is required.", -1);
        }
    }

    internal void RequireXmpPacket(RawXmpPacket packet)
    {
        if (packet == null || packet.RawBytes == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A complete XMP packet is required.", -1);
        }
    }

    internal void RequireIptcDataset(RawIptcDataset dataset)
    {
        if (dataset == null || dataset.RawBytes == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A complete IPTC DataSet is required.", -1);
        }
    }

    internal void RequireIptcRecord(RawIptcRecord record)
    {
        if (record == null || record.RawBytes == null || record.Datasets == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A complete IPTC IIM record is required.", -1);
        }
    }

    internal void RequireIccProfile(RawIccProfile profile)
    {
        if (profile == null || profile.RawBytes == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A complete ICC profile is required.", -1);
        }
    }

    internal void RequireMetadataEditSet(RawMetadataEditSet editSet)
    {
        if (editSet == null || editSet.Edits == null || editSet.PreviewEdits == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A complete metadata edit set is required.", -1);
        }
    }

    internal void RequireDocument(RawDocument document)
    {
        if (document == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A document is required.", -1);
        }
    }

    internal void RequireMakerNote(RawMakerNote note)
    {
        if (note == null || note.RawBytes == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A complete maker note is required.", -1);
        }
    }

    /// <summary>Decodes exact stored unsigned generic raw samples from bounded memory.</summary>
    public void DecodeGenericUnsigned(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, uint[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeStored(source, descriptor, region, destination, options);
    }

    /// <summary>Decodes exact stored signed generic raw samples from bounded memory.</summary>
    public void DecodeGenericSigned(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, int[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeStored(source, descriptor, region, destination, options);
    }

    /// <summary>Decodes exact stored floating generic raw samples from bounded memory.</summary>
    public void DecodeGenericFloating(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeStored(source, descriptor, region, destination, options);
    }

    /// <summary>Decodes normalized unsigned generic raw samples from bounded memory.</summary>
    public void DecodeGenericNormalizedUnsigned(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, ushort[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeNormalized(source, descriptor, region, destination, options);
    }

    /// <summary>Decodes normalized floating generic raw samples from bounded memory.</summary>
    public void DecodeGenericNormalizedFloating(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeNormalized(source, descriptor, region, destination, options);
    }

    /// <summary>Decodes exact stored unsigned generic raw samples from a bounded caller-owned stream.</summary>
    public void DecodeGenericUnsignedStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, uint[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeStored(source, sourceOffset, sourceLength, descriptor, region, destination, options);
    }

    /// <summary>Decodes exact stored signed generic raw samples from a bounded caller-owned stream.</summary>
    public void DecodeGenericSignedStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, int[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeStored(source, sourceOffset, sourceLength, descriptor, region, destination, options);
    }

    /// <summary>Decodes exact stored floating generic raw samples from a bounded caller-owned stream.</summary>
    public void DecodeGenericFloatingStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeStored(source, sourceOffset, sourceLength, descriptor, region, destination, options);
    }

    /// <summary>Decodes normalized unsigned generic raw samples from a bounded caller-owned stream.</summary>
    public void DecodeGenericNormalizedUnsignedStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, ushort[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeNormalized(source, sourceOffset, sourceLength, descriptor, region, destination, options);
    }

    /// <summary>Decodes normalized floating generic raw samples from a bounded caller-owned stream.</summary>
    public void DecodeGenericNormalizedFloatingStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options)
    {
        GenericRawCodec.DecodeNormalized(source, sourceOffset, sourceLength, descriptor, region, destination, options);
    }

    /// <summary>Suggests exact-length generic raw descriptors without selecting a candidate.</summary>
    public RawDescriptorSuggestionResult SuggestGenericDescriptor(long sourceLength, RawDescriptorHints hints, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        return GenericRawDescriptorSuggester.Suggest(sourceLength, hints, limits, cancellationToken);
    }

    /// <summary>Reads a generic raw descriptor sidecar from exact UTF-8 JSON bytes.</summary>
    public RawSidecar ReadSidecar(byte[] json, RawLimits limits)
    {
        return RawSidecarCodec.Read(json, limits);
    }

    /// <summary>Reads a generic raw descriptor sidecar from exact bounded UTF-8 memory.</summary>
    public RawSidecar ReadSidecarMemory(System.ReadOnlyMemory<byte> json, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        return RawSidecarCodec.ReadMemory(json, limits, cancellationToken);
    }

    /// <summary>Reads a generic raw descriptor sidecar from an exact caller-owned stream window.</summary>
    public RawSidecar ReadSidecarStream(System.IO.Stream source, long offset, long length, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        return RawSidecarCodec.ReadStream(source, offset, length, limits, cancellationToken);
    }

    /// <summary>Writes a generic raw descriptor sidecar as deterministic UTF-8 JSON bytes.</summary>
    public byte[] WriteSidecarBytes(RawSidecar sidecar, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        return RawSidecarCodec.WriteBytes(sidecar, limits, cancellationToken);
    }

    /// <summary>Writes a generic raw descriptor sidecar as deterministic UTF-8 JSON to a caller-owned stream.</summary>
    public void WriteSidecarStream(System.IO.Stream destination, RawSidecar sidecar, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        RawSidecarCodec.WriteStream(destination, sidecar, limits, cancellationToken);
    }

    /// <summary>Creates an independent catalogue containing the reviewed built-in read and write profiles.</summary>
    public RawProfileCatalogue CreateBuiltInProfileCatalogue(RawLimits limits)
    {
        return RawProfileCatalogue.CreateBuiltIn(limits);
    }

    /// <summary>Validates a mutable read/write profile document without registering it.</summary>
    public RawValidationReport ValidateProfileDocument(RawProfileSchemaDocument document, RawLimits limits)
    {
        return RawProfileSchemaValidation.Validate(document, limits);
    }

    /// <summary>Reads and validates a complete strict profile document from UTF-8 JSON.</summary>
    public RawProfileSchemaDocument ReadProfileDocument(byte[] json, RawLimits limits)
    {
        return RawProfileSchemaCodec.Read(json, limits);
    }

    /// <summary>Writes a validated profile document as deterministic UTF-8 JSON.</summary>
    public byte[] WriteProfileDocument(RawProfileSchemaDocument document, RawLimits limits)
    {
        return RawProfileSchemaCodec.Write(document, limits);
    }

    /// <summary>Retrieves one retained preview or thumbnail from an opened document.</summary>
    public RawThumbnail GetDocumentThumbnail(RawDocument document, int index)
    {
        return RawThumbnailCodec.GetDocumentThumbnail(document, index);
    }

    /// <summary>Copies any mapped payload block from an opened document's retained source.</summary>
    public byte[] ExtractDocumentPayload(RawDocument document, RawPayloadBlock block, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        return RawThumbnailCodec.ExtractDocumentPayload(document, block, limits, cancellationToken);
    }

    /// <summary>Decodes a supported retained thumbnail or preview without applying its orientation.</summary>
    public RawRgbImage DecodeThumbnail(RawThumbnail thumbnail, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        return RawThumbnailCodec.DecodeThumbnail(thumbnail, limits, cancellationToken);
    }

    /// <summary>Encodes an eight-bit sRGB grayscale or RGB rendered image as a deterministic baseline-JPEG thumbnail.</summary>
    public RawThumbnail EncodeJpegThumbnail(RawRgbImage image, int index, int quality, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        return RawThumbnailCodec.EncodeJpegThumbnail(image, index, quality, limits, cancellationToken);
    }
}
