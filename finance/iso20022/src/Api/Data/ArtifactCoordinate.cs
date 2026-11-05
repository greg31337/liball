namespace iso20022;

/// <summary>Identifies one versioned artifact independently of its storage location.</summary>
public class ArtifactCoordinate
{
    /// <summary>The artifact kind.</summary>
    public ArtifactKind Kind;
    /// <summary>The artifact edition.</summary>
    public string Edition = string.Empty;
    /// <summary>The artifact identifier within the edition.</summary>
    public string Identifier = string.Empty;
    /// <summary>The independently versioned artifact version.</summary>
    public string Version = string.Empty;
}
