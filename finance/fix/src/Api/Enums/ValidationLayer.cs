namespace fix;

/// <summary>Identifies independently selectable validation layers.</summary>
public enum ValidationLayer
{
    /// <summary>No validation layer is selected.</summary>
    None = 0,
    /// <summary>Wire framing validation.</summary>
    Framing = 1,
    /// <summary>FIX envelope validation.</summary>
    Envelope = 2,
    /// <summary>Session-header validation.</summary>
    SessionHeader = 4,
    /// <summary>Dictionary grammar validation.</summary>
    DictionaryGrammar = 8,
    /// <summary>Datatype lexical and range validation.</summary>
    Datatype = 16,
    /// <summary>Code-set membership validation.</summary>
    CodeSet = 32,
    /// <summary>Repeating-group validation.</summary>
    Group = 64,
    /// <summary>Conditional-rule validation.</summary>
    ConditionalRule = 128,
    /// <summary>Application-semantic validation.</summary>
    ApplicationSemantic = 256,
    /// <summary>All defined validation layers.</summary>
    All = 511
}
