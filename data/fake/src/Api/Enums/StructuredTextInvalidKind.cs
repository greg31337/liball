namespace fake;

/// <summary>Selects a deliberate defect in structured text.</summary>
public enum StructuredTextInvalidKind
{
    /// <summary>Leaves a quoted value without its closing quote.</summary>
    UnterminatedQuotedValue = 0,

    /// <summary>Omits a separator required by the selected text format.</summary>
    MissingSeparator = 1,

    /// <summary>Places a token where the selected grammar does not allow one.</summary>
    UnexpectedToken = 2
}
