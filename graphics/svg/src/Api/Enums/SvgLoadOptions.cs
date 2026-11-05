namespace svg;

/// <summary>Defines SVG loading processing modes.</summary>
public enum SvgProcessingMode
{
    /// <summary>Preserves dynamic metadata without executing script.</summary>
    Dynamic = 0,
    /// <summary>Loads a static document while preserving unsupported content.</summary>
    Static = 1,
    /// <summary>Uses secure static defaults and caller-mediated resources.</summary>
    SecureStatic = 2
}

/// <summary>Defines secure document type declaration handling.</summary>
public enum SvgDtdPolicy
{
    /// <summary>Rejects every document type declaration.</summary>
    Prohibit = 0,
    /// <summary>Retains the declaration as raw metadata without parsing or expanding entities.</summary>
    PreserveOnly = 1
}
