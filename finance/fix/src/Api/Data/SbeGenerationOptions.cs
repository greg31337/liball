namespace fix;

/// <summary>Controls deterministic dictionary-to-SBE schema generation.</summary>
public class SbeGenerationOptions
{
    /// <summary>The generated schema package name.</summary>
    public string Package = "fix.generated";
    /// <summary>The SBE schema identifier.</summary>
    public int SchemaId;
    /// <summary>The SBE schema version.</summary>
    public int Version;
    /// <summary>The first generated template identifier.</summary>
    public int FirstTemplateId = 1;
    /// <summary>Whether generated fixed-width values use little-endian byte order.</summary>
    public bool LittleEndian = true;
    /// <summary>Whether FIX semantic types are emitted.</summary>
    public bool EmitSemanticTypes = true;
    /// <summary>The selected dictionary scenario.</summary>
    public string Scenario = "";
}
