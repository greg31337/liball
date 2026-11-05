namespace fix;

/// <summary>Identifies an output produced by model generation.</summary>
public enum GenerationTarget
{
    /// <summary>C# source code.</summary>
    CSharp = 0,
    /// <summary>An SBE XML schema.</summary>
    SbeSchema = 1
}
