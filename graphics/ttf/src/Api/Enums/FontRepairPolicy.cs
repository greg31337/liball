namespace ttf;

/// <summary>Controls deterministic repair of inconsistent font data.</summary>
public enum FontRepairPolicy
{
    /// <summary>Do not repair inconsistent data.</summary>
    None = 0,

    /// <summary>Apply only defined, unambiguous repairs and diagnose every repair.</summary>
    Safe = 1
}
