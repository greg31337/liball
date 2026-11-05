namespace iso20022;

/// <summary>Describes one repository query match without exposing the internal index.</summary>
public class RepositoryQueryItem
{
    /// <summary>The stable repository identifier of the matched entity.</summary>
    public string Identifier = string.Empty;
    /// <summary>The repository name of the matched entity when present.</summary>
    public string Name = string.Empty;
    /// <summary>The normalized kind of the matched entity.</summary>
    public RepositoryEntityKind Kind;
    /// <summary>The normalized registration state of the matched entity.</summary>
    public RegistrationStatus RegistrationStatus;
    /// <summary>The business-area code associated with the match.</summary>
    public string BusinessAreaCode = string.Empty;
    /// <summary>The exact message identifier associated with the match.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The primary XML namespace associated with the match.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The message version associated with the match.</summary>
    public string Version = string.Empty;
    /// <summary>The normalized repository data object represented by the match.</summary>
    public object? Value;
}
