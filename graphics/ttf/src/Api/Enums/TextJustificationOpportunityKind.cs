namespace ttf;

/// <summary>Identifies an optional advanced justification mechanism.</summary>
public enum TextJustificationOpportunityKind
{
    /// <summary>Use a font-provided JSTF extension opportunity.</summary>
    Jstf = 0,

    /// <summary>Use a script-approved kashida extension opportunity.</summary>
    Kashida = 1
}
