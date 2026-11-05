namespace svg;

/// <summary>Configures bounded normalized-scene construction.</summary>
public class SvgSceneBuildOptions
{
    /// <summary>The maximum total main-tree and resource-content nodes constructed.</summary>
    public int MaxNodes;
    /// <summary>The maximum shared resources constructed.</summary>
    public int MaxResources;
    /// <summary>Whether the completed scene is attached to the resolved document.</summary>
    public bool AttachToDocument;

    /// <summary>Creates secure deterministic scene-construction defaults.</summary>
    public SvgSceneBuildOptions()
    {
        MaxNodes = 100000;
        MaxResources = 10000;
        AttachToDocument = true;
    }
}

/// <summary>Contains a normalized scene-construction outcome.</summary>
public class SvgSceneBuildResult
{
    /// <summary>The constructed scene, including partial data after a bounded failure.</summary>
    public SvgScene Scene;
    /// <summary>The scene construction diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The total number of main-tree and resource-content nodes constructed.</summary>
    public int NodeCount;
    /// <summary>The number of shared resources constructed.</summary>
    public int ResourceCount;
    /// <summary>Whether construction completed successfully.</summary>
    public bool Success;

    internal SvgSceneBuildResult(SvgScene scene)
    {
        Scene = scene;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        NodeCount = 0;
        ResourceCount = 0;
        Success = false;
    }
}
