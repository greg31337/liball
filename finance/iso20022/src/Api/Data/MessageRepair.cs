namespace iso20022;

/// <summary>Records one explicit repair made while interpreting or writing a message.</summary>
public class MessageRepair
{
    /// <summary>The stable repair code.</summary>
    public string Code = string.Empty;
    /// <summary>A human-readable repair description.</summary>
    public string Description = string.Empty;
    /// <summary>The qualified path affected by the repair when known.</summary>
    public XmlPathData? Path;
    /// <summary>The source location affected by the repair when known.</summary>
    public SourceLocation? Location;
    /// <summary>The source value before repair when applicable.</summary>
    public string OriginalValue = string.Empty;
    /// <summary>The value after repair when applicable.</summary>
    public string RepairedValue = string.Empty;
}
