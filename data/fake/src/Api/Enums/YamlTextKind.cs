namespace fake;

/// <summary>Selects the top-level shape of generated YAML text.</summary>
public enum YamlTextKind
{
    /// <summary>Generates a YAML scalar.</summary>
    Scalar = 0,

    /// <summary>Generates a block-style YAML sequence.</summary>
    Sequence = 1,

    /// <summary>Generates a block-style YAML mapping.</summary>
    Mapping = 2
}
