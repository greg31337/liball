using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

/// <summary>Represents an ordered list of document transformation steps.</summary>
public class GltfTransformPlan
{
    /// <summary>The ordered transform steps.</summary>
    public List<GltfTransformStep> Steps = new List<GltfTransformStep>();
}

/// <summary>Describes one built-in or hook-driven transformation.</summary>
public class GltfTransformStep
{
    /// <summary>The transformation kind.</summary>
    public GltfTransformKind Kind = GltfTransformKind.Custom;

    /// <summary>An optional application-defined step name.</summary>
    public string? Name;

    /// <summary>Whether the step is enabled.</summary>
    public bool Enabled = true;

    /// <summary>Owned free-form step settings.</summary>
    public JsonObject Settings = new JsonObject();
}

/// <summary>Provides mutable working state and registered capabilities to a transform hook.</summary>
public class GltfTransformContext
{
    /// <summary>The mutable working document.</summary>
    public GltfDocument? Document;

    /// <summary>The mutable working resource set.</summary>
    public GltfResourceSet Resources = new GltfResourceSet();

    /// <summary>The current transformation step.</summary>
    public GltfTransformStep? Step;

    /// <summary>The current reference remap.</summary>
    public GltfIndexRemap Remap = new GltfIndexRemap();

    /// <summary>Registered geometry compression codecs.</summary>
    public List<IGltfCompressionCodec> CompressionCodecs = new List<IGltfCompressionCodec>();

    /// <summary>Registered image codecs.</summary>
    public List<IGltfImageCodec> ImageCodecs = new List<IGltfImageCodec>();

    /// <summary>Registered custom transform hooks.</summary>
    public List<IGltfTransformHook> Hooks = new List<IGltfTransformHook>();

    /// <summary>Diagnostics accumulated before the current hook.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

/// <summary>Controls transactional transform execution and validation.</summary>
public class GltfTransformOptions
{
    /// <summary>Whether transformations operate on an independent clone.</summary>
    public bool CloneDocument = true;

    /// <summary>Whether validation runs before the first step.</summary>
    public bool ValidateBefore = true;

    /// <summary>Whether validation runs after the last step.</summary>
    public bool ValidateAfter = true;

    /// <summary>Whether a failed step discards all working changes.</summary>
    public bool RollbackOnFailure = true;

    /// <summary>Whether unknown extensions without handlers block potentially unsafe steps.</summary>
    public bool RequireSafeUnknownExtensions = true;

    /// <summary>The validation options used by the transform pipeline.</summary>
    public GltfValidationOptions ValidationOptions = new GltfValidationOptions();

    /// <summary>Registered geometry compression codecs.</summary>
    public List<IGltfCompressionCodec> CompressionCodecs = new List<IGltfCompressionCodec>();

    /// <summary>Registered image codecs.</summary>
    public List<IGltfImageCodec> ImageCodecs = new List<IGltfImageCodec>();

    /// <summary>Registered custom transform hooks.</summary>
    public List<IGltfTransformHook> Hooks = new List<IGltfTransformHook>();
}

/// <summary>Describes one extension declaration added or removed by a transform.</summary>
public class GltfTransformExtensionChange
{
    /// <summary>The extension name.</summary>
    public string Name = "";

    /// <summary>Whether the change applies to the required rather than used declaration.</summary>
    public bool Required;

    /// <summary>Whether the declaration was added rather than removed.</summary>
    public bool Added;
}

/// <summary>Describes the outcome and changes of one transform step.</summary>
public class GltfTransformStepResult
{
    /// <summary>The executed step.</summary>
    public GltfTransformStep? Step;

    /// <summary>The step outcome.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The old-to-new mappings produced by the step.</summary>
    public GltfIndexRemap Remap = new GltfIndexRemap();

    /// <summary>Changed object identifiers.</summary>
    public List<string> ChangedObjects = new List<string>();

    /// <summary>Changed resource identifiers.</summary>
    public List<string> ChangedResources = new List<string>();

    /// <summary>Extension declaration changes produced by the step.</summary>
    public List<GltfTransformExtensionChange> ExtensionChanges =
        new List<GltfTransformExtensionChange>();

    /// <summary>Codec implementation identifiers used by the step.</summary>
    public List<string> Codecs = new List<string>();

    /// <summary>The reason a step was skipped or unavailable.</summary>
    public string? Reason;

    /// <summary>Step diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

/// <summary>Contains transactional transform output, per-step results, and size changes.</summary>
public class GltfTransformReport
{
    /// <summary>The overall transform status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The transformed document when execution succeeded.</summary>
    public GltfDocument? Document;

    /// <summary>The transformed resource set.</summary>
    public GltfResourceSet Resources = new GltfResourceSet();

    /// <summary>Per-step results in plan order.</summary>
    public List<GltfTransformStepResult> Steps = new List<GltfTransformStepResult>();

    /// <summary>Unique changed object identifiers in deterministic order.</summary>
    public List<string> ChangedObjects = new List<string>();

    /// <summary>Unique changed resource identifiers in deterministic order.</summary>
    public List<string> ChangedResources = new List<string>();

    /// <summary>Unique extension declaration changes in deterministic order.</summary>
    public List<GltfTransformExtensionChange> ExtensionChanges =
        new List<GltfTransformExtensionChange>();

    /// <summary>Unique codec implementation identifiers in deterministic order.</summary>
    public List<string> Codecs = new List<string>();

    /// <summary>Unique skipped-step reasons in deterministic order.</summary>
    public List<string> SkippedReasons = new List<string>();

    /// <summary>The primary size before transformation.</summary>
    public long OriginalByteLength;

    /// <summary>The primary size after transformation.</summary>
    public long ResultByteLength;

    /// <summary>The deterministic input hash.</summary>
    public string? OriginalHash;

    /// <summary>The deterministic result hash.</summary>
    public string? ResultHash;

    /// <summary>Aggregate transform diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}
