namespace iso20022;

/// <summary>Represents the lower and optional finite upper bounds of a repository member.</summary>
public class MultiplicityDefinition
{
    /// <summary>The inclusive minimum occurrence count.</summary>
    public int MinimumOccurs;
    /// <summary>Whether the source declares a finite maximum occurrence count.</summary>
    public bool HasMaximumOccurs;
    /// <summary>The inclusive maximum occurrence count when finite.</summary>
    public int MaximumOccurs;
    /// <summary>The source location of the multiplicity.</summary>
    public SourceLocation Location = new SourceLocation();
}
