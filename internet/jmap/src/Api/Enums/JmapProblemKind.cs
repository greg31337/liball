namespace jmap;

/// <summary>
/// Classifies JMAP request-level problem types while allowing unknown wire values to be preserved separately.
/// </summary>
public enum JmapProblemKind
{
    /// <summary>The problem type is not recognized by this version of the library.</summary>
    Unknown = 0,

    /// <summary>The request opted in to a capability the server does not support.</summary>
    UnknownCapability = 1,

    /// <summary>The request media type or body is not valid I-JSON.</summary>
    NotJson = 2,

    /// <summary>The JSON body does not match the JMAP Request type.</summary>
    NotRequest = 3,

    /// <summary>The request exceeds an advertised server limit.</summary>
    Limit = 4
}
