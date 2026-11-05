namespace xml;

/// <summary>
/// Carries one extension serialization method binding.
/// </summary>
public class XmlSerializationMethodBinding
{
    /// <summary>
    /// The expanded serialization method name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The serialization method implementation.
    /// </summary>
    public IXmlSerializationMethod? Implementation;
}
