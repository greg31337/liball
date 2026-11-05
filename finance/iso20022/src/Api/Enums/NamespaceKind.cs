namespace iso20022;

/// <summary>Identifies the role of an ISO 20022 namespace.</summary>
public enum NamespaceKind
{
    /// <summary>The namespace role is not known.</summary>
    Unknown = 0,
    /// <summary>A message-definition namespace.</summary>
    Message = 1,
    /// <summary>A business-application-header namespace.</summary>
    Header = 2,
    /// <summary>A business-message or business-file envelope namespace.</summary>
    Envelope = 3,
    /// <summary>A supplementary-data namespace.</summary>
    Supplementary = 4,
    /// <summary>An ISO 20022 repository or metamodel namespace.</summary>
    Repository = 5
}
