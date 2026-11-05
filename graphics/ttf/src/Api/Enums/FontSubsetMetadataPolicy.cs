namespace ttf;

/// <summary>Controls optional descriptive metadata retained in output.</summary>
public enum FontSubsetMetadataPolicy
{
    /// <summary>Emit only metadata required for valid deterministic output.</summary>
    Minimal = 0,

    /// <summary>Retain supported safe metadata, including license records.</summary>
    RetainSafe = 1
}
