namespace http;

/// <summary>
/// Represents one ordered Structured Field parameter.
/// </summary>
public class StructuredFieldParameter
{
    /// <summary>
    /// The lowercase parameter key.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// The parameter bare item, or <c>null</c> when it has not been assigned.
    /// </summary>
    public StructuredFieldMember? BareItem;
}
