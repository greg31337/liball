using System.Collections.Generic;

namespace gltf;

/// <summary>Controls container conversion, resource layout, validation, and optional transforms.</summary>
public class GltfConversionOptions
{
    /// <summary>The expected input format or automatic detection.</summary>
    public GltfContainerFormat InputFormat = GltfContainerFormat.Auto;

    /// <summary>The output container format.</summary>
    public GltfContainerFormat OutputFormat = GltfContainerFormat.Glb;

    /// <summary>The output resource representation.</summary>
    public GltfResourceMode ResourceMode = GltfResourceMode.Preserve;

    /// <summary>Whether eligible images are embedded.</summary>
    public bool EmbedImages;

    /// <summary>Whether binary buffers may be partitioned at buffer-view boundaries.</summary>
    public bool PartitionBuffers;

    /// <summary>The maximum byte length of each partitioned output buffer.</summary>
    public int MaximumBufferByteLength =
        GltfConstants.DefaultMaximumResourceByteLength;

    /// <summary>Whether output file replacement is atomic.</summary>
    public bool AtomicOutput = true;

    /// <summary>Whether existing external resource names may be replaced.</summary>
    public bool OverwriteResources;

    /// <summary>Read options used by conversion entry points.</summary>
    public GltfReadOptions ReadOptions = new GltfReadOptions();

    /// <summary>Write options used by conversion entry points.</summary>
    public GltfWriteOptions WriteOptions = new GltfWriteOptions();

    /// <summary>An optional ordered transform plan applied before writing.</summary>
    public GltfTransformPlan? TransformPlan;

    /// <summary>Transform options used when a plan is supplied.</summary>
    public GltfTransformOptions TransformOptions = new GltfTransformOptions();
}

/// <summary>Contains converted document/output data, resource manifest, and diagnostics.</summary>
public class GltfConversionResult
{
    /// <summary>The overall conversion status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The converted document.</summary>
    public GltfDocument? Document;

    /// <summary>The owned primary output bytes.</summary>
    public byte[] PrimaryBytes = [];

    /// <summary>The deterministic hexadecimal hash of the primary output.</summary>
    public string? PrimaryContentHash;

    /// <summary>The output primary path when a file entry point was used.</summary>
    public string? PrimaryPath;

    /// <summary>The output resources.</summary>
    public GltfResourceSet Resources = new GltfResourceSet();

    /// <summary>The deterministic resource manifest.</summary>
    public List<GltfResourceManifestEntry> Manifest = new List<GltfResourceManifestEntry>();

    /// <summary>Deterministic extension and codec decisions made by conversion.</summary>
    public List<GltfConversionDecision> Decisions = new List<GltfConversionDecision>();

    /// <summary>The optional transform report.</summary>
    public GltfTransformReport? TransformReport;

    /// <summary>Conversion diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

/// <summary>Describes one extension declaration or codec capability decision.</summary>
public class GltfConversionDecision
{
    /// <summary>The kind of conversion decision.</summary>
    public GltfConversionDecisionKind Kind = GltfConversionDecisionKind.Extension;

    /// <summary>The extension or codec capability name.</summary>
    public string Name = "";

    /// <summary>The decision made for the extension or codec capability.</summary>
    public GltfConversionDecisionAction Action = GltfConversionDecisionAction.Preserved;

    /// <summary>Whether the associated extension declaration is required.</summary>
    public bool Required;

    /// <summary>The related extension name for a codec decision.</summary>
    public string? RelatedExtension;

    /// <summary>The outcome of the individual decision.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>Additional deterministic decision information.</summary>
    public string? Detail;
}
