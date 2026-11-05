namespace uri;

/// <summary>
/// Identifies the value shape bound to a URI Template variable.
/// </summary>
public enum UriTemplateValueKind
{
    /// <summary>The variable has no defined value.</summary>
    Undefined = 0,

    /// <summary>The variable has one scalar string value.</summary>
    Scalar = 1,

    /// <summary>The variable has an ordered list of string values.</summary>
    List = 2,

    /// <summary>The variable has an ordered associative map.</summary>
    Map = 3
}
