namespace sql;

/// <summary>Specifies why part of an AST could not be normalized losslessly.</summary>
public enum SqlNormalizationMarkerKind
{
    /// <summary>Represents preserved syntax without a supported semantic AST representation.</summary>
    UnsupportedSyntax = 0,
    /// <summary>Represents an unknown statement without a lossless normalized representation.</summary>
    UnknownStatement = 1,
    /// <summary>Represents an unknown expression without a lossless normalized representation.</summary>
    UnknownExpression = 2,
    /// <summary>Represents a diagnostic produced while reparsing normalized text.</summary>
    ReparseDiagnostic = 3
}
