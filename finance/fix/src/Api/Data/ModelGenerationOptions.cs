using System.Collections.Generic;

namespace fix;

/// <summary>Controls deterministic generated model output.</summary>
public class ModelGenerationOptions
{
    /// <summary>The generation target.</summary>
    public GenerationTarget Target = GenerationTarget.CSharp;
    /// <summary>The generated C# namespace.</summary>
    public string Namespace = "fix.Generated";
    /// <summary>An optional generated type-name prefix.</summary>
    public string TypePrefix = "";
    /// <summary>The requested dictionary version name, BeginString, or application version, or an empty string to use the supplied dictionary.</summary>
    public string Version = "";
    /// <summary>The selected scenario.</summary>
    public string Scenario = "";
    /// <summary>The selected message categories, or an empty list for no category filter.</summary>
    public List<string> CategoryNames = [];
    /// <summary>The selected message names, or an empty list for all messages.</summary>
    public List<string> MessageNames = [];
    /// <summary>The selected field tags, or an empty list for referenced fields.</summary>
    public List<int> FieldTags = [];
    /// <summary>Whether XML documentation is emitted.</summary>
    public bool EmitDocumentation = true;
    /// <summary>Whether unchanged output is identified by content hash.</summary>
    public bool DetectUnchangedFiles = true;
    /// <summary>A prior deterministic manifest used to identify unchanged output files.</summary>
    public string PreviousManifest = "";
    /// <summary>The maximum generated files.</summary>
    public int MaxFiles = 10000;
}
