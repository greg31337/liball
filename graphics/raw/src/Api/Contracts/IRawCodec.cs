namespace raw;

/// <summary>Defines the canonical instance-based raw inspection, decode, processing, render, and validation workflow.</summary>
/// <remarks>The contract uses bounded memory and opened documents as its stable core. Callers can perform their own path or stream buffering when needed.</remarks>
public interface IRawCodec
{
    /// <summary>Probes an exact byte-array input without decoding image payloads.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="fileName">An optional filename used only for mismatch diagnostics.</param>
    /// <param name="expectedFormat">The caller-expected format, or <see cref="RawFormat.Unknown"/>.</param>
    /// <param name="genericDescriptorProvided">Whether an authoritative generic descriptor is available.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>The content-based probe result.</returns>
    RawProbe ProbeBytes(byte[] source, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options);

    /// <summary>Probes an exact transient span without decoding image payloads.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="fileName">An optional filename used only for mismatch diagnostics.</param>
    /// <param name="expectedFormat">The caller-expected format, or <see cref="RawFormat.Unknown"/>.</param>
    /// <param name="genericDescriptorProvided">Whether an authoritative generic descriptor is available.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>The content-based probe result.</returns>
    RawProbe ProbeSpan(System.ReadOnlySpan<byte> source, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options);

    /// <summary>Probes a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="fileName">An optional filename used only for mismatch diagnostics.</param>
    /// <param name="expectedFormat">The caller-expected format, or <see cref="RawFormat.Unknown"/>.</param>
    /// <param name="genericDescriptorProvided">Whether an authoritative generic descriptor is available.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>The content-based probe result.</returns>
    RawProbe ProbeStream(System.IO.Stream source, long offset, long length, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options);

    /// <summary>Probes a path without decoding image payloads.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="expectedFormat">The caller-expected format, or <see cref="RawFormat.Unknown"/>.</param>
    /// <param name="genericDescriptorProvided">Whether an authoritative generic descriptor is available.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>The content-based probe result.</returns>
    RawProbe ProbePath(string path, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options);

    /// <summary>Probes a bounded memory input without decoding image payloads.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="fileName">An optional filename used only for mismatch diagnostics.</param>
    /// <param name="expectedFormat">The caller-expected format, or <see cref="RawFormat.Unknown"/>.</param>
    /// <param name="genericDescriptorProvided">Whether an authoritative generic descriptor is available.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>The content-based probe result.</returns>
    RawProbe Probe(System.ReadOnlyMemory<byte> source, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawOpenOptions options);

    /// <summary>Opens an exact byte-array input according to the requested read selections.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Read selections, limits, and cancellation state.</param>
    /// <returns>The inspected document.</returns>
    RawDocument OpenBytes(byte[] source, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Copies and opens an exact transient span according to the requested read selections.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Read selections, limits, and cancellation state.</param>
    /// <returns>The inspected document backed by an owned bounded copy.</returns>
    RawDocument OpenSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Opens a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Read selections, limits, and cancellation state.</param>
    /// <returns>The inspected document backed by an owned bounded copy.</returns>
    RawDocument OpenStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Opens a path according to the requested read selections.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Read selections, limits, and cancellation state.</param>
    /// <returns>The inspected document backed by an owned bounded copy.</returns>
    RawDocument OpenPath(string path, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Opens a bounded memory input according to the requested read selections.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Read selections, limits, and cancellation state.</param>
    /// <returns>The inspected document.</returns>
    RawDocument Open(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Opens selected metadata from an exact byte-array input.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Metadata selections, limits, and reconciliation policy.</param>
    /// <returns>A metadata-only inspected document.</returns>
    RawDocument OpenMetadataBytes(byte[] source, RawDescriptor? genericDescriptor, RawMetadataOptions options);

    /// <summary>Copies an exact transient span and opens selected metadata.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Metadata selections, limits, and reconciliation policy.</param>
    /// <returns>A metadata-only inspected document backed by an owned bounded copy.</returns>
    RawDocument OpenMetadataSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawMetadataOptions options);

    /// <summary>Opens selected metadata from a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Metadata selections, limits, and reconciliation policy.</param>
    /// <returns>A metadata-only inspected document backed by an owned bounded copy.</returns>
    RawDocument OpenMetadataStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawMetadataOptions options);

    /// <summary>Opens selected metadata from a path.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Metadata selections, limits, and reconciliation policy.</param>
    /// <returns>A metadata-only inspected document backed by an owned bounded copy.</returns>
    RawDocument OpenMetadataPath(string path, RawDescriptor? genericDescriptor, RawMetadataOptions options);

    /// <summary>Opens selected metadata from bounded memory and applies the requested reconciliation policy.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Metadata selections, limits, and reconciliation policy.</param>
    /// <returns>A metadata-only inspected document.</returns>
    RawDocument OpenMetadata(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawMetadataOptions options);

    /// <summary>Opens only preview descriptions and bytes from an exact byte-array input.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>A preview-only inspected document.</returns>
    RawDocument OpenPreviewsBytes(byte[] source, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Copies an exact transient span and opens only preview descriptions and bytes.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>A preview-only inspected document backed by an owned bounded copy.</returns>
    RawDocument OpenPreviewsSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Opens only preview descriptions and bytes from a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>A preview-only inspected document backed by an owned bounded copy.</returns>
    RawDocument OpenPreviewsStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Opens only preview descriptions and bytes from a path.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>A preview-only inspected document backed by an owned bounded copy.</returns>
    RawDocument OpenPreviewsPath(string path, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Opens only embedded preview and thumbnail descriptions and bytes from bounded memory.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Open limits and cancellation state.</param>
    /// <returns>A preview-only inspected document.</returns>
    RawDocument OpenPreviews(System.ReadOnlyMemory<byte> source, RawDescriptor? genericDescriptor, RawOpenOptions options);

    /// <summary>Reports independently supported operations for a format and optional generic descriptor.</summary>
    /// <param name="format">The content format.</param>
    /// <param name="genericDescriptor">The generic descriptor when applicable, or <c>null</c>.</param>
    /// <returns>The independent capability flags.</returns>
    RawCapabilities GetCapabilities(RawFormat format, RawDescriptor? genericDescriptor);

    /// <summary>Decodes the selected image from an opened document.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    RawSensorImage Decode(RawDocument document, RawDecodeOptions options);

    /// <summary>Opens and decodes an exact byte-array input.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    RawSensorImage DecodeBytes(byte[] source, RawDescriptor? genericDescriptor, RawDecodeOptions options);

    /// <summary>Copies, opens, and decodes an exact transient span.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    RawSensorImage DecodeSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawDecodeOptions options);

    /// <summary>Opens and decodes a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    RawSensorImage DecodeStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawDecodeOptions options);

    /// <summary>Opens and decodes a path.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Decode selection, limits, and cancellation state.</param>
    /// <returns>The normalized sensor image.</returns>
    RawSensorImage DecodePath(string path, RawDescriptor? genericDescriptor, RawDecodeOptions options);

    /// <summary>Renders one selected image and acquisition plane through the complete processing pipeline.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="options">Image, plane, crop, processing, geometry, and output controls.</param>
    /// <returns>A tightly pixel-interleaved gray, RGB, or RGBA image.</returns>
    RawRgbImage Render(RawDocument document, RawRenderOptions options);

    /// <summary>Renders an existing normalized sensor image through the complete processing pipeline.</summary>
    /// <param name="sensorImage">The normalized sensor image and its calibration.</param>
    /// <param name="options">Crop, processing, geometry, and output controls.</param>
    /// <returns>A tightly pixel-interleaved gray, RGB, or RGBA image.</returns>
    RawRgbImage RenderSensorImage(RawSensorImage sensorImage, RawRenderOptions options);

    /// <summary>Opens and renders an exact byte-array input.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Render controls, limits, and cancellation state.</param>
    /// <returns>The rendered image.</returns>
    RawRgbImage RenderBytes(byte[] source, RawDescriptor? genericDescriptor, RawRenderOptions options);

    /// <summary>Copies, opens, and renders an exact transient span.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Render controls, limits, and cancellation state.</param>
    /// <returns>The rendered image.</returns>
    RawRgbImage RenderSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawRenderOptions options);

    /// <summary>Opens and renders a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Render controls, limits, and cancellation state.</param>
    /// <returns>The rendered image.</returns>
    RawRgbImage RenderStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawRenderOptions options);

    /// <summary>Opens and renders a path.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Render controls, limits, and cancellation state.</param>
    /// <returns>The rendered image.</returns>
    RawRgbImage RenderPath(string path, RawDescriptor? genericDescriptor, RawRenderOptions options);

    /// <summary>Validates retained structure and public document semantics.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="options">Validation subsystem selection and limits.</param>
    /// <returns>All selected validation findings.</returns>
    RawValidationReport Validate(RawDocument document, RawValidationOptions options);

    /// <summary>Opens and validates an exact byte-array input.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Validation subsystem selection and limits.</param>
    /// <returns>All selected validation findings.</returns>
    RawValidationReport ValidateBytes(byte[] source, RawDescriptor? genericDescriptor, RawValidationOptions options);

    /// <summary>Copies, opens, and validates an exact transient span.</summary>
    /// <param name="source">The exact bounded input.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Validation subsystem selection and limits.</param>
    /// <returns>All selected validation findings.</returns>
    RawValidationReport ValidateSpan(System.ReadOnlySpan<byte> source, RawDescriptor? genericDescriptor, RawValidationOptions options);

    /// <summary>Opens and validates a bounded caller-owned stream without closing it.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="offset">The bounded input offset.</param>
    /// <param name="length">The bounded input length.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Validation subsystem selection and limits.</param>
    /// <returns>All selected validation findings.</returns>
    RawValidationReport ValidateStream(System.IO.Stream source, long offset, long length, RawDescriptor? genericDescriptor, RawValidationOptions options);

    /// <summary>Opens and validates a path.</summary>
    /// <param name="path">The input path.</param>
    /// <param name="genericDescriptor">The authoritative generic descriptor, or <c>null</c> for a container.</param>
    /// <param name="options">Validation subsystem selection and limits.</param>
    /// <returns>All selected validation findings.</returns>
    RawValidationReport ValidatePath(string path, RawDescriptor? genericDescriptor, RawValidationOptions options);

    /// <summary>Validates open options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateOpenOptions(RawOpenOptions value, RawValidationOptions options);
    /// <summary>Validates decode options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateDecodeOptions(RawDecodeOptions value, RawValidationOptions options);
    /// <summary>Validates normalization options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateNormalizationOptions(RawNormalizationOptions value, RawValidationOptions options);
    /// <summary>Validates defect-correction options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateDefectOptions(RawDefectOptions value, RawValidationOptions options);
    /// <summary>Validates lens-correction options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateLensCorrectionOptions(RawLensCorrectionOptions value, RawValidationOptions options);
    /// <summary>Validates preview options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidatePreviewOptions(RawPreviewOptions value, RawValidationOptions options);
    /// <summary>Validates analysis options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateAnalysisOptions(RawAnalysisOptions value, RawValidationOptions options);
    /// <summary>Validates histogram options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateHistogramOptions(RawHistogramOptions value, RawValidationOptions options);
    /// <summary>Validates render options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateRenderOptions(RawRenderOptions value, RawValidationOptions options);
    /// <summary>Validates ICC transform options through the selected semantic category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateIccTransformOptions(RawIccTransformOptions value, RawValidationOptions options);
    /// <summary>Validates metadata options through the selected metadata category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateMetadataOptions(RawMetadataOptions value, RawValidationOptions options);
    /// <summary>Validates write options through the selected writer category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateWriteOptions(RawWriteOptions value, RawValidationOptions options);
    /// <summary>Validates generic encoding options through the selected writer category.</summary>
    /// <param name="value">The option set.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateGenericEncodeOptions(RawGenericEncodeOptions value, RawValidationOptions options);
    /// <summary>Validates category selection, mode, recovery policy, and limits.</summary>
    /// <param name="value">The validation option set.</param><returns>The option findings.</returns>
    RawValidationReport ValidateValidationOptions(RawValidationOptions value);

    /// <summary>Validates a descriptor through selected structural validation.</summary>
    /// <param name="value">The descriptor.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateDescriptorModel(RawDescriptor value, RawValidationOptions options);
    /// <summary>Validates a profile document through selected profile validation.</summary>
    /// <param name="value">The profile document.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateProfileDocumentModel(RawProfileSchemaDocument value, RawValidationOptions options);
    /// <summary>Validates an XMP packet through selected metadata validation.</summary>
    /// <param name="value">The packet model.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateXmpPacket(RawXmpPacket value, RawValidationOptions options);
    /// <summary>Validates an IPTC record through selected metadata validation.</summary>
    /// <param name="value">The record model.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateIptcRecord(RawIptcRecord value, RawValidationOptions options);
    /// <summary>Validates an ICC profile through selected metadata validation.</summary>
    /// <param name="value">The profile model.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateIccProfileModel(RawIccProfile value, RawValidationOptions options);
    /// <summary>Validates a maker note through selected metadata validation.</summary>
    /// <param name="value">The maker-note model.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateMakerNoteModel(RawMakerNote value, RawValidationOptions options);

    /// <summary>Validates an image area through selected semantic validation.</summary>
    /// <param name="value">The area.</param><param name="imageWidth">The image width.</param><param name="imageHeight">The image height.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateArea(RawArea value, int imageWidth, int imageHeight, RawValidationOptions options);
    /// <summary>Validates plane identity and geometry through selected semantic validation.</summary>
    /// <param name="value">The plane model.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidatePlaneInfo(RawPlaneInfo value, RawValidationOptions options);
    /// <summary>Validates image metadata through selected semantic validation.</summary>
    /// <param name="value">The image metadata.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateImageInfo(RawImageInfo value, RawValidationOptions options);
    /// <summary>Validates exact stored samples through selected semantic validation.</summary>
    /// <param name="value">The stored image.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateStoredImage(RawStoredImage value, RawValidationOptions options);
    /// <summary>Validates normalized sensor samples through selected semantic validation.</summary>
    /// <param name="value">The sensor image.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateSensorImage(RawSensorImage value, RawValidationOptions options);
    /// <summary>Validates rendered RGB samples through selected semantic validation.</summary>
    /// <param name="value">The RGB image.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateRgbImage(RawRgbImage value, RawValidationOptions options);
    /// <summary>Validates a thumbnail through selected semantic validation.</summary>
    /// <param name="value">The thumbnail.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateThumbnail(RawThumbnail value, RawValidationOptions options);
    /// <summary>Validates a dry-run plan through selected profile and writer validation.</summary>
    /// <param name="value">The write plan.</param><param name="options">Validation controls.</param><returns>The selected findings.</returns>
    RawValidationReport ValidateWritePlan(RawWritePlan value, RawValidationOptions options);
    /// <summary>Reports whether a validation result contains no errors.</summary>
    /// <param name="report">The validation result.</param><returns><c>true</c> when no error is present.</returns>
    bool ValidationReportIsValid(RawValidationReport report);
    /// <summary>Reports whether selected categories conform in the result's strict or tolerant mode.</summary>
    /// <param name="report">The validation result.</param><returns><c>true</c> when selected categories conform.</returns>
    bool ValidationReportIsConformant(RawValidationReport report);
    /// <summary>Reports whether a validation result contains warnings.</summary>
    /// <param name="report">The validation result.</param><returns><c>true</c> when a warning is present.</returns>
    bool ValidationReportHasWarnings(RawValidationReport report);
    /// <summary>Returns the stable summary for one validation category.</summary>
    /// <param name="report">The validation result.</param><param name="category">The category.</param><returns>The category summary, or <c>null</c>.</returns>
    RawValidationCategoryReport? FindValidationCategory(RawValidationReport report, RawValidationCategory category);

    /// <summary>Estimates repeating per-channel black calibration and optional row or column residuals from optical-black samples.</summary>
    /// <param name="storedImage">The exact decoded stored samples.</param>
    /// <param name="options">Optical areas, repeat geometry, linearization, cancellation, and resource controls.</param>
    /// <returns>Black calibration in the post-linearization sample domain.</returns>
    RawBlackLevel EstimateOpticalBlackLevel(RawStoredImage storedImage, RawNormalizationOptions options);

    /// <summary>Normalizes exact stored values through calibration, crop modeling, and clipping detection.</summary>
    /// <param name="storedImage">The exact decoded stored samples.</param>
    /// <param name="options">Calibration, crop, retention, cancellation, and resource controls.</param>
    /// <returns>A full-size normalized sensor image.</returns>
    RawSensorImage PreprocessStoredImage(RawStoredImage storedImage, RawNormalizationOptions options);

    /// <summary>Preprocesses retained exact values from an existing decoded sensor image.</summary>
    /// <param name="sensorImage">A decoded sensor image retaining exact stored samples and calibration.</param>
    /// <param name="options">Calibration overrides, crop, retention, cancellation, and resource controls.</param>
    /// <returns>A full-size normalized sensor image preserving applicable render calibration.</returns>
    RawSensorImage PreprocessSensorImage(RawSensorImage sensorImage, RawNormalizationOptions options);

    /// <summary>Normalizes exact stored values through calibration, crop modeling, and clipping detection.</summary>
    /// <param name="storedImage">The exact decoded stored samples.</param>
    /// <param name="options">Calibration, crop, retention, cancellation, and resource controls.</param>
    /// <returns>A full-size normalized sensor image.</returns>
    RawSensorImage Preprocess(RawStoredImage storedImage, RawNormalizationOptions options);

    /// <summary>Repairs declared or detected sensor defects.</summary>
    /// <param name="sensorImage">The normalized sensor image.</param>
    /// <param name="options">Defect declarations, detection, cancellation, and resource controls.</param>
    /// <returns>A repaired copy of the sensor image.</returns>
    RawSensorImage RepairDefects(RawSensorImage sensorImage, RawDefectOptions options);

    /// <summary>Applies embedded raw-domain lens corrections.</summary>
    /// <param name="sensorImage">The normalized sensor image.</param>
    /// <param name="options">Correction selection, cancellation, and resource controls.</param>
    /// <returns>A corrected copy of the sensor image.</returns>
    RawSensorImage CorrectLens(RawSensorImage sensorImage, RawLensCorrectionOptions options);

    /// <summary>Creates a fast half-size preview from a compatible Bayer sensor image.</summary>
    /// <param name="sensorImage">The normalized Bayer sensor image.</param>
    /// <param name="options">Preview cancellation and resource controls.</param>
    /// <returns>A crop-aware floating camera-RGB preview.</returns>
    RawRgbImage CreateHalfSizePreview(RawSensorImage sensorImage, RawPreviewOptions options);

    /// <summary>Computes raw statistics and diagnostic planes.</summary>
    /// <param name="sensorImage">The normalized sensor image.</param>
    /// <param name="options">Analysis geometry, selection, cancellation, and resource controls.</param>
    /// <returns>The selected raw analysis.</returns>
    RawAnalysisResult Analyze(RawSensorImage sensorImage, RawAnalysisOptions options);

    /// <summary>Resolves stored, active, default, and caller crop precedence.</summary>
    /// <param name="info">Stored image geometry and camera-defined areas.</param>
    /// <param name="userCrop">An optional caller crop in stored image coordinates.</param>
    /// <returns>The validated non-destructive crop hierarchy.</returns>
    RawCropModel ResolveCrop(RawImageInfo info, RawArea? userCrop);

    /// <summary>Reconstructs cropped camera-linear samples or demosaics a CFA sensor image.</summary>
    /// <param name="sensorImage">The normalized monochrome, linear RGB, or CFA sensor image.</param>
    /// <param name="options">Demosaic, cancellation, and resource controls.</param>
    /// <returns>A floating camera-linear image.</returns>
    RawRgbImage ReconstructSensorImage(RawSensorImage sensorImage, RawRenderOptions options);

    /// <summary>Applies the selected white balance to camera-linear RGB samples.</summary>
    /// <param name="image">A floating camera-linear RGB or RGBA image.</param>
    /// <param name="cameraWhiteBalance">The camera calibration used by camera mode, or <c>null</c>.</param>
    /// <param name="options">White-balance, cancellation, and resource controls.</param>
    /// <returns>A balanced floating camera-linear image.</returns>
    RawRgbImage ApplyWhiteBalance(RawRgbImage image, RawWhiteBalance? cameraWhiteBalance, RawRenderOptions options);

    /// <summary>Converts camera-linear RGB through matrix profiles into D50 or D65 XYZ.</summary>
    /// <param name="image">A white-balanced floating camera-linear RGB or RGBA image.</param>
    /// <param name="profiles">One to three applicable matrix profiles.</param>
    /// <param name="illuminantTemperatureKelvin">The scene illuminant temperature.</param>
    /// <param name="options">XYZ target, adaptation, cancellation, and resource controls.</param>
    /// <returns>A floating linear XYZ or XYZA image.</returns>
    RawRgbImage ConvertCameraToXyz(RawRgbImage image, RawColorProfile[] profiles, double illuminantTemperatureKelvin, RawRenderOptions options);

    /// <summary>Converts linear XYZ or raw-camera RGB into a selected built-in output color space.</summary>
    /// <param name="image">Floating linear XYZ, XYZA, raw-camera RGB, or raw-camera RGBA samples.</param>
    /// <param name="options">Output color, transfer, adaptation, cancellation, and resource controls.</param>
    /// <returns>A floating image in the selected built-in output representation.</returns>
    RawRgbImage ConvertOutputColor(RawRgbImage image, RawRenderOptions options);

    /// <summary>Converts ICC RGB or RGBA samples through an input profile to linear D50 XYZ.</summary>
    /// <param name="image">Floating ICC-encoded samples.</param>
    /// <param name="profile">The validated input profile.</param>
    /// <param name="options">Rendering intent, cancellation, and resource controls.</param>
    /// <returns>A floating linear D50 XYZ or XYZA image.</returns>
    RawRgbImage ConvertIccToXyz(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options);

    /// <summary>Converts linear D50 XYZ or XYZA samples through an output ICC profile.</summary>
    /// <param name="image">Floating linear D50 XYZ or XYZA samples.</param>
    /// <param name="profile">The validated output profile.</param>
    /// <param name="options">Rendering intent, cancellation, and resource controls.</param>
    /// <returns>A floating ICC-encoded RGB or RGBA image.</returns>
    RawRgbImage ConvertXyzToIcc(RawRgbImage image, RawIccProfile profile, RawIccTransformOptions options);

    /// <summary>Applies exposure, brightness, shadow, saturation, and highlight controls.</summary>
    /// <param name="image">A floating scene-linear RGB or RGBA image.</param>
    /// <param name="options">Exposure, highlight, cancellation, and resource controls.</param>
    /// <returns>A floating scene-linear RGB or RGBA image.</returns>
    RawRgbImage ApplyExposureAndHighlights(RawRgbImage image, RawRenderOptions options);

    /// <summary>Applies managed denoise, false-color suppression, chromatic-aberration correction, and capture sharpening.</summary>
    /// <param name="image">A floating scene-linear RGB or RGBA image.</param>
    /// <param name="chromaticAberration">Optional post-demosaic chromatic-aberration calibration.</param>
    /// <param name="options">Detail, cancellation, and resource controls.</param>
    /// <returns>An enhanced floating scene-linear RGB or RGBA image.</returns>
    RawRgbImage EnhanceDetails(RawRgbImage image, RawChromaticAberration? chromaticAberration, RawRenderOptions options);

    /// <summary>Applies tone-curve, contrast, gamut, and transfer-function processing.</summary>
    /// <param name="image">A floating scene-linear managed RGB or RGBA image.</param>
    /// <param name="options">Tone, gamut, transfer, cancellation, and resource controls.</param>
    /// <returns>A floating tone-processed RGB or RGBA image.</returns>
    RawRgbImage ApplyToneAndGamut(RawRgbImage image, RawRenderOptions options);

    /// <summary>Applies crop, orientation, resampling, and final component-depth conversion.</summary>
    /// <param name="image">A floating gray, RGB, or RGBA image.</param>
    /// <param name="crop">An optional crop in current image coordinates.</param>
    /// <param name="options">Geometry, resampling, output-depth, cancellation, and resource controls.</param>
    /// <returns>A tightly packed final output image.</returns>
    RawRgbImage CreateOutputImage(RawRgbImage image, RawArea? crop, RawRenderOptions options);

    /// <summary>Computes bounded per-channel histograms over exact stored samples.</summary>
    /// <param name="storedImage">The exact decoded stored samples.</param>
    /// <param name="options">Region, histogram, percentile, cancellation, and resource controls.</param>
    /// <returns>The per-channel stored-domain histogram result.</returns>
    RawHistogramResult HistogramStoredImage(RawStoredImage storedImage, RawHistogramOptions options);

    /// <summary>Computes bounded per-channel histograms over normalized sensor samples.</summary>
    /// <param name="sensorImage">The normalized sensor image.</param>
    /// <param name="options">Region, histogram, percentile, cancellation, and resource controls.</param>
    /// <returns>The per-channel normalized-domain histogram result.</returns>
    RawHistogramResult HistogramSensorImage(RawSensorImage sensorImage, RawHistogramOptions options);

    /// <summary>Computes bounded per-channel histograms over rendered samples.</summary>
    /// <param name="image">The rendered byte, unsigned-short, or floating image.</param>
    /// <param name="options">Region, histogram, percentile, cancellation, and resource controls.</param>
    /// <returns>The per-channel rendered-domain histogram result.</returns>
    RawHistogramResult HistogramRenderedImage(RawRgbImage image, RawHistogramOptions options);

    /// <summary>Computes bounded per-channel histograms over normalized sensor samples.</summary>
    /// <param name="sensorImage">The normalized sensor image.</param>
    /// <param name="options">Histogram geometry, percentiles, cancellation, and resource controls.</param>
    /// <returns>The per-channel histogram result.</returns>
    RawHistogramResult Histogram(RawSensorImage sensorImage, RawHistogramOptions options);

    /// <summary>Converts floating samples between two RGB ICC profiles.</summary>
    /// <param name="image">Samples encoded by the input profile.</param>
    /// <param name="inputProfile">The validated input profile.</param>
    /// <param name="outputProfile">The validated output profile.</param>
    /// <param name="options">Rendering intent, cancellation, and resource controls.</param>
    /// <returns>Samples encoded by the output profile.</returns>
    RawRgbImage ConvertIcc(RawRgbImage image, RawIccProfile inputProfile, RawIccProfile outputProfile, RawIccTransformOptions options);

    /// <summary>Creates a tightly stored grayscale generic raw descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful sample precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The grayscale descriptor.</returns>
    RawDescriptor CreateGrayDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder);

    /// <summary>Creates a tightly interleaved RGB generic raw descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful sample precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The RGB descriptor.</returns>
    RawDescriptor CreateRgbDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder);

    /// <summary>Creates a tightly interleaved BGR generic raw descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful sample precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The BGR descriptor.</returns>
    RawDescriptor CreateBgrDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder);

    /// <summary>Creates a tightly interleaved RGBA generic raw descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful sample precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The RGBA descriptor.</returns>
    RawDescriptor CreateRgbaDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder);

    /// <summary>Creates a tightly interleaved BGRA generic raw descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful sample precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The BGRA descriptor.</returns>
    RawDescriptor CreateBgraDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder);

    /// <summary>Creates a tightly stored byte-aligned standard Bayer descriptor.</summary>
    /// <param name="width">The sensor width.</param>
    /// <param name="height">The sensor height.</param>
    /// <param name="bitDepth">The meaningful sample precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <param name="layout">The standard Bayer phase.</param>
    /// <returns>The Bayer descriptor.</returns>
    RawDescriptor CreateBayerDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder, RawCfaLayout layout);

    /// <summary>Creates a tightly stored byte-aligned arbitrary-CFA descriptor.</summary>
    /// <param name="width">The sensor width.</param>
    /// <param name="height">The sensor height.</param>
    /// <param name="bitDepth">The meaningful sample precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <param name="repeatWidth">The CFA repeat width.</param>
    /// <param name="repeatHeight">The CFA repeat height.</param>
    /// <param name="colors">The row-major CFA colors.</param>
    /// <returns>The arbitrary-CFA descriptor.</returns>
    RawDescriptor CreateArbitraryCfaDescriptor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder, int repeatWidth, int repeatHeight, RawCfaColor[] colors);

    /// <summary>Creates a tightly RAW10-packed standard Bayer descriptor.</summary>
    /// <param name="width">The sensor width.</param>
    /// <param name="height">The sensor height.</param>
    /// <param name="layout">The standard Bayer phase.</param>
    /// <returns>The RAW10 descriptor.</returns>
    RawDescriptor CreateRaw10Descriptor(int width, int height, RawCfaLayout layout);

    /// <summary>Creates a tightly RAW12-packed standard Bayer descriptor.</summary>
    /// <param name="width">The sensor width.</param>
    /// <param name="height">The sensor height.</param>
    /// <param name="layout">The standard Bayer phase.</param>
    /// <returns>The RAW12 descriptor.</returns>
    RawDescriptor CreateRaw12Descriptor(int width, int height, RawCfaLayout layout);

    /// <summary>Creates a tightly RAW14-packed standard Bayer descriptor.</summary>
    /// <param name="width">The sensor width.</param>
    /// <param name="height">The sensor height.</param>
    /// <param name="layout">The standard Bayer phase.</param>
    /// <returns>The RAW14 descriptor.</returns>
    RawDescriptor CreateRaw14Descriptor(int width, int height, RawCfaLayout layout);

    /// <summary>Validates descriptor fields and storage geometry against resource limits.</summary>
    /// <param name="descriptor">The descriptor to validate.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>All validation findings in deterministic order.</returns>
    RawValidationReport ValidateDescriptor(RawDescriptor descriptor, RawLimits limits);

    /// <summary>Validates a descriptor against an exact bounded source length.</summary>
    /// <param name="descriptor">The descriptor to validate.</param>
    /// <param name="sourceLength">The exact bounded source length.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>All validation findings in deterministic order.</returns>
    RawValidationReport ValidateDescriptorSourceLength(RawDescriptor descriptor, long sourceLength, RawLimits limits);

    /// <summary>Computes the bounded minimum stored bytes in one descriptor row.</summary>
    /// <param name="descriptor">The descriptor with complete storage geometry.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The minimum row byte count.</returns>
    long GetDescriptorMinimumRowBytes(RawDescriptor descriptor, RawLimits limits);

    /// <summary>Computes the bounded bytes required through the final stored row and plane.</summary>
    /// <param name="descriptor">The descriptor with complete storage geometry.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The required payload byte count.</returns>
    long GetDescriptorRequiredDataLength(RawDescriptor descriptor, RawLimits limits);

    /// <summary>Computes the bounded intentional trailing bytes after stored sample geometry.</summary>
    /// <param name="descriptor">The descriptor with complete storage geometry.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The trailing byte count.</returns>
    long GetDescriptorTrailingByteCount(RawDescriptor descriptor, RawLimits limits);

    /// <summary>Creates one canonical standard Bayer CFA pattern.</summary>
    /// <param name="layout">One of the four standard Bayer phases.</param>
    /// <returns>The canonical two-by-two pattern.</returns>
    RawCfaPattern CreateBayerCfaPattern(RawCfaLayout layout);

    /// <summary>Creates one arbitrary repeating CFA pattern.</summary>
    /// <param name="repeatWidth">The repeat width.</param>
    /// <param name="repeatHeight">The repeat height.</param>
    /// <param name="colors">The row-major CFA colors.</param>
    /// <returns>The arbitrary CFA pattern.</returns>
    RawCfaPattern CreateArbitraryCfaPattern(int repeatWidth, int repeatHeight, RawCfaColor[] colors);

    /// <summary>Validates CFA dimensions, colors, and named-layout identity.</summary>
    /// <param name="pattern">The CFA pattern to validate.</param>
    /// <returns>All validation findings in deterministic order.</returns>
    RawValidationReport ValidateCfaPattern(RawCfaPattern pattern);

    /// <summary>Queries the repeating CFA color at one nonnegative sensor coordinate.</summary>
    /// <param name="pattern">The valid CFA pattern.</param>
    /// <param name="x">The sensor column.</param>
    /// <param name="y">The sensor row.</param>
    /// <returns>The color at the coordinate.</returns>
    RawCfaColor GetCfaColor(RawCfaPattern pattern, int x, int y);

    /// <summary>Creates an exact unsigned TIFF rational.</summary>
    /// <param name="numerator">The unsigned numerator.</param>
    /// <param name="denominator">The unsigned denominator, including zero for malformed input.</param>
    /// <returns>The exact unsigned rational.</returns>
    RawRational CreateUnsignedRational(uint numerator, uint denominator);

    /// <summary>Creates an exact signed TIFF rational.</summary>
    /// <param name="numerator">The signed numerator.</param>
    /// <param name="denominator">The signed denominator, including zero for malformed input.</param>
    /// <returns>The exact signed rational.</returns>
    RawRational CreateSignedRational(int numerator, int denominator);

    /// <summary>Validates a rational denominator and its declared signed or unsigned component ranges.</summary>
    /// <param name="value">The rational to validate.</param>
    /// <returns>All validation findings in deterministic order.</returns>
    RawValidationReport ValidateRational(RawRational value);

    /// <summary>Tests rational signedness and exact stored-component equality.</summary>
    /// <param name="first">The first rational.</param>
    /// <param name="second">The second rational, or <c>null</c>.</param>
    /// <returns>Whether the exact representations are equal.</returns>
    bool RationalsEqualExact(RawRational first, RawRational? second);

    /// <summary>Tests exact numeric equality of two valid rationals.</summary>
    /// <param name="first">The first rational.</param>
    /// <param name="second">The second rational, or <c>null</c>.</param>
    /// <returns>Whether the represented numeric values are equal.</returns>
    bool RationalsEqualValue(RawRational first, RawRational? second);

    /// <summary>Converts a valid rational to double precision.</summary>
    /// <param name="value">The rational to convert.</param>
    /// <returns>The numerator divided by the denominator.</returns>
    double ConvertRationalToDouble(RawRational value);

    /// <summary>Converts a valid rational to single precision.</summary>
    /// <param name="value">The rational to convert.</param>
    /// <returns>The numerator divided by the denominator rounded to single precision.</returns>
    float ConvertRationalToSingle(RawRational value);

    /// <summary>Creates an exact TIFF BYTE value from an independently copied array.</summary>
    RawTagValue CreateByteTagValue(byte[] values);

    /// <summary>Creates a null-terminated TIFF ASCII value.</summary>
    RawTagValue CreateAsciiTagValue(string text);

    /// <summary>Creates an exact endian-aware TIFF SHORT value.</summary>
    RawTagValue CreateUnsignedShortTagValue(ushort[] values, RawByteOrder byteOrder);

    /// <summary>Creates an exact endian-aware TIFF LONG value.</summary>
    RawTagValue CreateUnsignedLongTagValue(uint[] values, RawByteOrder byteOrder);

    /// <summary>Creates an exact endian-aware TIFF RATIONAL value without floating-point coercion.</summary>
    RawTagValue CreateUnsignedRationalTagValue(RawRational[] values, RawByteOrder byteOrder);

    /// <summary>Creates an exact TIFF SBYTE value.</summary>
    RawTagValue CreateSignedByteTagValue(sbyte[] values);

    /// <summary>Creates an exact independently copied TIFF UNDEFINED value.</summary>
    RawTagValue CreateUndefinedTagValue(byte[] values);

    /// <summary>Creates an exact endian-aware TIFF SSHORT value.</summary>
    RawTagValue CreateSignedShortTagValue(short[] values, RawByteOrder byteOrder);

    /// <summary>Creates an exact endian-aware TIFF SLONG value.</summary>
    RawTagValue CreateSignedLongTagValue(int[] values, RawByteOrder byteOrder);

    /// <summary>Creates an exact endian-aware TIFF SRATIONAL value without floating-point coercion.</summary>
    RawTagValue CreateSignedRationalTagValue(RawRational[] values, RawByteOrder byteOrder);

    /// <summary>Creates a bit-exact endian-aware TIFF FLOAT value.</summary>
    RawTagValue CreateFloatTagValue(float[] values, RawByteOrder byteOrder);

    /// <summary>Creates a bit-exact endian-aware TIFF DOUBLE value.</summary>
    RawTagValue CreateDoubleTagValue(double[] values, RawByteOrder byteOrder);

    /// <summary>Creates an exact endian-aware classic TIFF IFD-offset value.</summary>
    RawTagValue CreateIfdTagValue(uint[] values, RawByteOrder byteOrder);

    /// <summary>Creates an exact endian-aware BigTIFF LONG8 value.</summary>
    RawTagValue CreateUnsignedLong8TagValue(ulong[] values, RawByteOrder byteOrder);

    /// <summary>Creates an exact endian-aware BigTIFF SLONG8 value.</summary>
    RawTagValue CreateSignedLong8TagValue(long[] values, RawByteOrder byteOrder);

    /// <summary>Creates an exact endian-aware BigTIFF IFD8-offset value.</summary>
    RawTagValue CreateIfd8TagValue(ulong[] values, RawByteOrder byteOrder);

    /// <summary>Creates a null-terminated Exif 3 UTF-8 value without a byte-order mark.</summary>
    RawTagValue CreateUtf8TagValue(string text);

    /// <summary>Creates an Exif character-prefixed UNDEFINED text value.</summary>
    RawTagValue CreateExifPrefixedTextTagValue(string text, RawTextEncoding encoding, RawByteOrder byteOrder);

    /// <summary>Returns an independent copy of any tag value's exact encoded bytes.</summary>
    byte[] CopyTagValueBytes(RawTagValue value);

    /// <summary>Extracts exact TIFF BYTE values.</summary>
    byte[] GetByteTagValues(RawTagValue value);

    /// <summary>Decodes TIFF ASCII text together with its exact encoding details.</summary>
    RawTextValue GetAsciiTagText(RawTagValue value);

    /// <summary>Extracts exact TIFF SHORT values.</summary>
    ushort[] GetUnsignedShortTagValues(RawTagValue value);

    /// <summary>Extracts exact TIFF LONG values without accepting IFD offsets.</summary>
    uint[] GetUnsignedLongTagValues(RawTagValue value);

    /// <summary>Extracts exact TIFF RATIONAL components without floating-point coercion.</summary>
    RawRational[] GetUnsignedRationalTagValues(RawTagValue value);

    /// <summary>Extracts exact TIFF SBYTE values.</summary>
    sbyte[] GetSignedByteTagValues(RawTagValue value);

    /// <summary>Returns an independent copy of exact TIFF UNDEFINED bytes.</summary>
    byte[] GetUndefinedTagBytes(RawTagValue value);

    /// <summary>Extracts exact TIFF SSHORT values.</summary>
    short[] GetSignedShortTagValues(RawTagValue value);

    /// <summary>Extracts exact TIFF SLONG values.</summary>
    int[] GetSignedLongTagValues(RawTagValue value);

    /// <summary>Extracts exact TIFF SRATIONAL components without floating-point coercion.</summary>
    RawRational[] GetSignedRationalTagValues(RawTagValue value);

    /// <summary>Extracts bit-exact TIFF FLOAT values.</summary>
    float[] GetFloatTagValues(RawTagValue value);

    /// <summary>Extracts bit-exact TIFF DOUBLE values.</summary>
    double[] GetDoubleTagValues(RawTagValue value);

    /// <summary>Extracts exact classic TIFF IFD offsets without accepting LONG values.</summary>
    uint[] GetIfdTagValues(RawTagValue value);

    /// <summary>Extracts exact BigTIFF LONG8 values without accepting IFD8 offsets.</summary>
    ulong[] GetUnsignedLong8TagValues(RawTagValue value);

    /// <summary>Extracts exact BigTIFF SLONG8 values.</summary>
    long[] GetSignedLong8TagValues(RawTagValue value);

    /// <summary>Extracts exact BigTIFF IFD8 offsets without accepting LONG8 values.</summary>
    ulong[] GetIfd8TagValues(RawTagValue value);

    /// <summary>Decodes Exif 3 UTF-8 text together with its exact encoding details.</summary>
    RawTextValue GetUtf8TagText(RawTagValue value);

    /// <summary>Decodes Exif character-prefixed UNDEFINED text together with its exact encoding details.</summary>
    RawTextValue GetExifPrefixedTagText(RawTagValue value);

    /// <summary>Finds a metadata directory by its exact stable path.</summary>
    RawIfd? FindMetadataIfdByPath(RawMetadata metadata, string path);

    /// <summary>Finds a tag identifier within an exact stable directory path.</summary>
    RawTag? FindMetadataTagByPathAndId(RawMetadata metadata, string path, ushort tagId);

    /// <summary>Finds the first tag identifier within a semantic directory kind.</summary>
    RawTag? FindMetadataTagByKindAndId(RawMetadata metadata, RawIfdKind kind, ushort tagId);

    /// <summary>Finds the first named tag in deterministic metadata graph order.</summary>
    RawTag? FindMetadataTagByName(RawMetadata metadata, string name);

    /// <summary>Creates an Exif timestamp from an offset-aware .NET value with selected subsecond precision.</summary>
    RawExifTimestamp CreateExifTimestampFromDateTimeOffset(System.DateTimeOffset value, int subsecondDigits);

    /// <summary>Converts a valid offset-bearing Exif timestamp to an offset-aware .NET value.</summary>
    System.DateTimeOffset ConvertExifTimestampToDateTimeOffset(RawExifTimestamp timestamp);

    /// <summary>Encodes an Exif timestamp's date-time component as TIFF ASCII.</summary>
    RawTagValue CreateExifDateTimeTagValue(RawExifTimestamp timestamp);

    /// <summary>Encodes an Exif timestamp's UTC-offset component as TIFF ASCII.</summary>
    RawTagValue CreateExifOffsetTagValue(RawExifTimestamp timestamp);

    /// <summary>Encodes an Exif timestamp's subsecond component as TIFF ASCII.</summary>
    RawTagValue CreateExifSubsecondsTagValue(RawExifTimestamp timestamp);

    /// <summary>Parses a bounded standalone XMP packet with safe XML settings.</summary>
    RawXmpPacket ParseXmpPacket(byte[] rawBytes, RawLimits limits);

    /// <summary>Returns a validated independent encoding of a standalone XMP packet.</summary>
    byte[] EncodeXmpPacketBytes(RawXmpPacket packet, RawLimits limits);

    /// <summary>Finds the first exact namespace, name, and optional-language XMP field.</summary>
    RawXmpField? FindXmpField(RawXmpPacket packet, string namespaceUri, string name, string? language);

    /// <summary>Finds every exact namespace and name XMP field in encounter order.</summary>
    RawXmpField[] FindXmpFields(RawXmpPacket packet, string namespaceUri, string name);

    /// <summary>Replaces XMP root XML while retaining encoding, wrapper, and padding.</summary>
    RawXmpPacket ReplaceXmpPacketXml(RawXmpPacket packet, string xml, RawLimits limits);

    /// <summary>Replaces XMP root XML while retaining the exact total packet length.</summary>
    RawXmpPacket ReplaceXmpPacketXmlFixedSize(RawXmpPacket packet, string xml, RawLimits limits);

    /// <summary>Encodes a standalone XMP packet as a TIFF BYTE value.</summary>
    RawTagValue CreateXmpTagValue(RawXmpPacket packet, RawLimits limits);

    /// <summary>Parses a bounded standalone IPTC IIM record.</summary>
    RawIptcRecord ParseIptcRecord(byte[] rawBytes, RawLimits limits);

    /// <summary>Creates an uninterpreted IPTC IIM DataSet.</summary>
    RawIptcDataset CreateBinaryIptcDataset(byte recordNumber, byte datasetNumber, byte[] bytes);

    /// <summary>Creates an ASCII or UTF-8 IPTC IIM text DataSet.</summary>
    RawIptcDataset CreateTextIptcDataset(byte recordNumber, byte datasetNumber, string text, bool utf8);

    /// <summary>Creates a two-octet unsigned IPTC IIM DataSet.</summary>
    RawIptcDataset CreateUnsignedIptcDataset(byte recordNumber, byte datasetNumber, ushort value);

    /// <summary>Creates an eight-digit IPTC IIM date DataSet.</summary>
    RawIptcDataset CreateDateIptcDataset(byte recordNumber, byte datasetNumber, System.DateTime value);

    /// <summary>Creates an IPTC IIM time DataSet with an optional UTC offset.</summary>
    RawIptcDataset CreateTimeIptcDataset(byte recordNumber, byte datasetNumber, int hour, int minute, int second, bool hasUtcOffset, int utcOffsetMinutes);

    /// <summary>Returns an independent copy of an IPTC DataSet's exact value bytes.</summary>
    byte[] CopyIptcDatasetBytes(RawIptcDataset dataset);

    /// <summary>Creates a canonical standalone IPTC IIM record from ordered DataSets.</summary>
    RawIptcRecord CreateIptcRecord(RawIptcDataset[] datasets, RawLimits limits);

    /// <summary>Returns an independent copy of the original IPTC IIM bytes including padding.</summary>
    byte[] CopyIptcRecordBytes(RawIptcRecord record);

    /// <summary>Canonically encodes an IPTC IIM record's ordered DataSets without container padding.</summary>
    byte[] EncodeIptcRecordBytes(RawIptcRecord record, RawLimits limits);

    /// <summary>Finds the first IPTC DataSet with exact record and DataSet numbers.</summary>
    RawIptcDataset? FindIptcDataset(RawIptcRecord record, byte recordNumber, byte datasetNumber);

    /// <summary>Finds every repeatable IPTC DataSet with exact record and DataSet numbers.</summary>
    RawIptcDataset[] FindIptcDatasets(RawIptcRecord record, byte recordNumber, byte datasetNumber);

    /// <summary>Replaces one ordered IPTC DataSet while retaining all others and trailing padding.</summary>
    RawIptcRecord ReplaceIptcDatasetAtIndex(RawIptcRecord record, int index, RawIptcDataset replacement, RawLimits limits);

    /// <summary>Encodes an IPTC IIM record as a TIFF UNDEFINED value.</summary>
    RawTagValue CreateIptcTagValue(RawIptcRecord record, RawLimits limits);

    /// <summary>Parses and validates a bounded standalone ICC v2 or v4 profile.</summary>
    RawIccProfile ParseIccProfile(byte[] rawBytes, RawLimits limits);

    /// <summary>Returns a validated independent encoding of an ICC profile.</summary>
    byte[] EncodeIccProfileBytes(RawIccProfile profile, RawLimits limits);

    /// <summary>Finds an ICC tag by its exact four-character signature.</summary>
    RawIccTag? FindIccTag(RawIccProfile profile, string signature);

    /// <summary>Returns an independent copy of an ICC tag's exact data bytes.</summary>
    byte[] CopyIccTagBytes(RawIccTag tag);

    /// <summary>Reports whether an ICC profile exposes a complete RGB matrix and tone curves.</summary>
    bool IccProfileHasMatrixTrc(RawIccProfile profile);

    /// <summary>Validates the mutable public ICC profile model.</summary>
    RawValidationReport ValidateIccProfile(RawIccProfile profile);

    /// <summary>Replaces one ICC tag's data without changing its size or unrelated bytes.</summary>
    RawIccProfile ReplaceIccTagDataFixedSize(RawIccProfile profile, string signature, byte[] replacementBytes, RawLimits limits);

    /// <summary>Encodes an ICC profile as a TIFF UNDEFINED value.</summary>
    RawTagValue CreateIccTagValue(RawIccProfile profile, RawLimits limits);

    /// <summary>Rebuilds the unified metadata view and conflict report using explicit options.</summary>
    RawMetadata ReconcileMetadata(RawMetadata metadata, RawMetadataOptions options);

    /// <summary>Finds one unified metadata field by stable name and optional language.</summary>
    RawMetadataField? FindUnifiedMetadataField(RawMetadata metadata, string name, string? language);

    /// <summary>Creates an empty ordered metadata edit set.</summary>
    RawMetadataEditSet CreateMetadataEditSet();

    /// <summary>Appends an add operation for one metadata representation.</summary>
    RawMetadataEditSet AddMetadataFieldEdit(RawMetadataEditSet editSet, string name, string[] values, string? language, RawMetadataSource source);

    /// <summary>Appends a replacement for one or every existing metadata representation.</summary>
    RawMetadataEditSet ReplaceMetadataFieldEdit(RawMetadataEditSet editSet, string name, string[] values, string? language, RawMetadataSource source);

    /// <summary>Appends removal of one or every existing metadata representation.</summary>
    RawMetadataEditSet RemoveMetadataFieldEdit(RawMetadataEditSet editSet, string name, string? language, RawMetadataSource source);

    /// <summary>Appends the selected conservative privacy scrub preset.</summary>
    RawMetadataEditSet AddPrivacyScrubPreset(RawMetadataEditSet editSet, RawPrivacyScrubPreset preset);

    /// <summary>Appends removal of capture, creation, and modification timestamps across all representations.</summary>
    RawMetadataEditSet AddTimestampRemovalEdits(RawMetadataEditSet editSet);

    /// <summary>Appends removal of GPS coordinates, altitude, and GPS date/time across all representations.</summary>
    RawMetadataEditSet AddGpsRemovalEdits(RawMetadataEditSet editSet);

    /// <summary>Appends a replacement preview with synchronized geometry and orientation metadata.</summary>
    RawMetadataEditSet AddPreviewReplacementEdit(RawMetadataEditSet editSet, int index, RawThumbnailKind kind, int width, int height, RawOrientation orientation, string mimeType, byte[] encodedBytes);

    /// <summary>Plans metadata-model field edits without mutation.</summary>
    RawMetadataEditReport DryRunMetadataEdits(RawMetadata metadata, RawMetadataEditSet editSet);

    /// <summary>Atomically applies metadata-model field edits and stages them for preserving writers.</summary>
    RawMetadataEditReport ApplyMetadataEdits(RawMetadata metadata, RawMetadataEditSet editSet);

    /// <summary>Plans document metadata and preview edits without mutation.</summary>
    RawMetadataEditReport DryRunDocumentMetadataEdits(RawDocument document, RawMetadataEditSet editSet);

    /// <summary>Atomically applies document metadata and preview edits for a preserving rewrite.</summary>
    RawMetadataEditReport ApplyDocumentMetadataEdits(RawDocument document, RawMetadataEditSet editSet);

    /// <summary>Returns whether an edit report contains no error diagnostic.</summary>
    bool MetadataEditSucceeded(RawMetadataEditReport report);

    /// <summary>Parses standalone maker-note bytes using their containing TIFF context.</summary>
    RawMakerNote ParseMakerNote(byte[] rawBytes, long offset, long entryOffset, string ifdPath, string manufacturer, long tiffBaseOffset, RawByteOrder tiffByteOrder, RawLimits limits);

    /// <summary>Returns an independent copy of complete maker-note bytes.</summary>
    byte[] CopyMakerNoteBytes(RawMakerNote note);

    /// <summary>Validates preserved maker-note bytes, offsets, and relocation evidence.</summary>
    RawValidationReport ValidateMakerNote(RawMakerNote note, RawLimits limits);

    /// <summary>Plans byte-exact anchoring, proven relocation, or safe rejection.</summary>
    RawMakerNoteWritePlan PlanMakerNoteRelocation(RawMakerNote note, long plannedOffset, RawLimits limits);

    /// <summary>Finds a unique Canon maker-note tag by identifier.</summary>
    RawCanonMakerNoteTag? FindCanonMakerNoteTag(RawMakerNote note, ushort tagId);

    /// <summary>Returns an independent copy of exact Canon maker-note tag bytes.</summary>
    byte[] CopyCanonMakerNoteTagBytes(RawCanonMakerNoteTag tag);

    /// <summary>Decodes a reviewed fixed-width Canon ASCII tag.</summary>
    string GetCanonMakerNoteTagText(RawCanonMakerNoteTag tag);

    /// <summary>Creates one version-gated Canon maker-note replacement.</summary>
    RawCanonMakerNoteEdit CreateCanonMakerNoteEdit(ushort tagId, RawCanonMakerNoteVersion version, RawTagValue value);

    /// <summary>Encodes reviewed Canon tag replacements without changing private layout or unknown bytes.</summary>
    RawMakerNote ApplyCanonMakerNoteEdits(RawMakerNote note, RawCanonMakerNoteEdit[] edits, RawLimits limits);

    /// <summary>Finds a unique Nikon maker-note tag by identifier.</summary>
    RawNikonMakerNoteTag? FindNikonMakerNoteTag(RawMakerNote note, ushort tagId);

    /// <summary>Returns an independent copy of exact Nikon maker-note tag bytes.</summary>
    byte[] CopyNikonMakerNoteTagBytes(RawNikonMakerNoteTag tag);

    /// <summary>Decodes a reviewed fixed-width Nikon ASCII tag.</summary>
    string GetNikonMakerNoteTagText(RawNikonMakerNoteTag tag);

    /// <summary>Creates one version-gated Nikon maker-note replacement.</summary>
    RawNikonMakerNoteEdit CreateNikonMakerNoteEdit(ushort tagId, RawNikonMakerNoteVersion version, RawTagValue value);

    /// <summary>Encodes reviewed Nikon tag replacements without changing protected or unknown bytes.</summary>
    RawMakerNote ApplyNikonMakerNoteEdits(RawMakerNote note, RawNikonMakerNoteEdit[] edits, RawLimits limits);

    /// <summary>Decodes exact stored unsigned generic raw samples from bounded memory.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region and destination layout.</param>
    /// <param name="destination">The caller-owned unsigned sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericUnsigned(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, uint[] destination, RawDecodeOptions options);

    /// <summary>Decodes exact stored signed generic raw samples from bounded memory.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region, row, and channel selection and destination layout.</param>
    /// <param name="destination">The caller-owned signed sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericSigned(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, int[] destination, RawDecodeOptions options);

    /// <summary>Decodes exact stored half- or single-precision generic raw samples from bounded memory.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region, row, and channel selection and destination layout.</param>
    /// <param name="destination">The caller-owned floating sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericFloating(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options);

    /// <summary>Decodes generic raw samples normalized to the complete unsigned sixteen-bit range from bounded memory.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region, row, and channel selection and destination layout.</param>
    /// <param name="destination">The caller-owned normalized unsigned sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericNormalizedUnsigned(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, ushort[] destination, RawDecodeOptions options);

    /// <summary>Decodes generic raw samples normalized to floating point from bounded memory.</summary>
    /// <param name="source">The exact bounded generic raw input.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region, row, and channel selection and destination layout.</param>
    /// <param name="destination">The caller-owned normalized floating sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericNormalizedFloating(System.ReadOnlyMemory<byte> source, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options);

    /// <summary>Decodes exact stored unsigned generic raw samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region, row, channel, and plane selection and destination layout.</param>
    /// <param name="destination">The caller-owned unsigned sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericUnsignedStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, uint[] destination, RawDecodeOptions options);

    /// <summary>Decodes exact stored signed generic raw samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region, row, channel, and plane selection and destination layout.</param>
    /// <param name="destination">The caller-owned signed sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericSignedStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, int[] destination, RawDecodeOptions options);

    /// <summary>Decodes exact stored floating generic raw samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region, row, channel, and plane selection and destination layout.</param>
    /// <param name="destination">The caller-owned floating sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericFloatingStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options);

    /// <summary>Decodes normalized unsigned generic raw samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region, row, channel, and plane selection and destination layout.</param>
    /// <param name="destination">The caller-owned normalized unsigned sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericNormalizedUnsignedStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, ushort[] destination, RawDecodeOptions options);

    /// <summary>Decodes normalized floating generic raw samples from a bounded caller-owned stream.</summary>
    /// <param name="source">The readable caller-owned stream.</param>
    /// <param name="sourceOffset">The bounded input start.</param>
    /// <param name="sourceLength">The exact bounded input length.</param>
    /// <param name="descriptor">The authoritative source descriptor.</param>
    /// <param name="region">The source region, row, channel, and plane selection and destination layout.</param>
    /// <param name="destination">The caller-owned normalized floating sample buffer.</param>
    /// <param name="options">Decode limits and cancellation state.</param>
    void DecodeGenericNormalizedFloatingStream(System.IO.Stream source, long sourceOffset, long sourceLength, RawDescriptor descriptor, RawDecodeRegion region, float[] destination, RawDecodeOptions options);

    /// <summary>Suggests exact-length generic raw descriptors without selecting a candidate.</summary>
    /// <param name="sourceLength">The exact bounded source length.</param>
    /// <param name="hints">Caller-supplied descriptor constraints.</param>
    /// <param name="limits">Resource limits.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    /// <returns>All bounded candidates and ambiguity diagnostics.</returns>
    RawDescriptorSuggestionResult SuggestGenericDescriptor(long sourceLength, RawDescriptorHints hints, RawLimits limits, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads a generic raw descriptor sidecar from exact UTF-8 JSON bytes.</summary>
    /// <param name="json">The complete sidecar bytes.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The parsed sidecar.</returns>
    RawSidecar ReadSidecar(byte[] json, RawLimits limits);

    /// <summary>Reads a generic raw descriptor sidecar from exact bounded UTF-8 memory.</summary>
    /// <param name="json">The complete bounded sidecar memory.</param>
    /// <param name="limits">Resource limits.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    /// <returns>The parsed sidecar with unknown top-level properties retained.</returns>
    RawSidecar ReadSidecarMemory(System.ReadOnlyMemory<byte> json, RawLimits limits, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads a generic raw descriptor sidecar from an exact caller-owned stream window.</summary>
    /// <param name="source">The readable caller-owned stream, which remains open.</param>
    /// <param name="offset">The bounded sidecar offset.</param>
    /// <param name="length">The exact sidecar length.</param>
    /// <param name="limits">Resource limits.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    /// <returns>The parsed sidecar with unknown top-level properties retained.</returns>
    RawSidecar ReadSidecarStream(System.IO.Stream source, long offset, long length, RawLimits limits, System.Threading.CancellationToken cancellationToken);

    /// <summary>Writes a generic raw descriptor sidecar as deterministic UTF-8 JSON bytes.</summary>
    /// <param name="sidecar">The schema-valid sidecar document.</param>
    /// <param name="limits">Resource limits.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    /// <returns>The complete deterministic sidecar bytes.</returns>
    byte[] WriteSidecarBytes(RawSidecar sidecar, RawLimits limits, System.Threading.CancellationToken cancellationToken);

    /// <summary>Writes a generic raw descriptor sidecar as deterministic UTF-8 JSON to a caller-owned stream.</summary>
    /// <param name="destination">The writable caller-owned stream, which remains open.</param>
    /// <param name="sidecar">The schema-valid sidecar document.</param>
    /// <param name="limits">Resource limits.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    void WriteSidecarStream(System.IO.Stream destination, RawSidecar sidecar, RawLimits limits, System.Threading.CancellationToken cancellationToken);

    /// <summary>Creates an independent catalogue containing the reviewed built-in read and write profiles.</summary>
    /// <param name="limits">Resource limits used to validate and copy the built-in profile document.</param>
    /// <returns>A new built-in profile catalogue.</returns>
    RawProfileCatalogue CreateBuiltInProfileCatalogue(RawLimits limits);

    /// <summary>Validates a mutable read/write profile document without registering it.</summary>
    /// <param name="document">The profile document.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>All profile validation findings.</returns>
    RawValidationReport ValidateProfileDocument(RawProfileSchemaDocument document, RawLimits limits);

    /// <summary>Reads and validates a complete strict profile document from UTF-8 JSON.</summary>
    /// <param name="json">The exact profile JSON bytes.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The validated profile document.</returns>
    RawProfileSchemaDocument ReadProfileDocument(byte[] json, RawLimits limits);

    /// <summary>Writes a validated profile document as deterministic UTF-8 JSON.</summary>
    /// <param name="document">The read/write profile document.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The complete deterministic profile JSON bytes.</returns>
    byte[] WriteProfileDocument(RawProfileSchemaDocument document, RawLimits limits);

    /// <summary>Retrieves one retained preview or thumbnail from an opened document.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="index">The zero-based thumbnail collection index.</param>
    /// <returns>The selected retained thumbnail.</returns>
    RawThumbnail GetDocumentThumbnail(RawDocument document, int index);

    /// <summary>Copies any mapped payload block from an opened document's retained source.</summary>
    /// <param name="document">The opened document retaining bounded source bytes.</param>
    /// <param name="block">A payload block belonging to the document.</param>
    /// <param name="limits">Resource limits for the independent byte copy.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    /// <returns>An independent copy of the complete mapped payload.</returns>
    byte[] ExtractDocumentPayload(RawDocument document, RawPayloadBlock block, RawLimits limits, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes a supported retained thumbnail or preview without applying its orientation.</summary>
    /// <param name="thumbnail">The baseline-JPEG, uncompressed gray, or uncompressed RGB thumbnail.</param>
    /// <param name="limits">Parsing and decoded-image resource limits.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    /// <returns>The decoded eight-bit gray or RGB image.</returns>
    RawRgbImage DecodeThumbnail(RawThumbnail thumbnail, RawLimits limits, System.Threading.CancellationToken cancellationToken);

    /// <summary>Encodes an eight-bit sRGB grayscale or RGB rendered image as a deterministic baseline-JPEG thumbnail.</summary>
    /// <param name="image">The rendered image, including explicit channel order and orientation state.</param>
    /// <param name="index">The nonnegative thumbnail identity used by CR2 or NEF write inputs.</param>
    /// <param name="quality">The JPEG quality from one through 100.</param>
    /// <param name="limits">Input, output, and allocation resource limits.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    /// <returns>A validated baseline-JPEG thumbnail suitable for CR2 or NEF writing.</returns>
    RawThumbnail EncodeJpegThumbnail(RawRgbImage image, int index, int quality, RawLimits limits, System.Threading.CancellationToken cancellationToken);
}
