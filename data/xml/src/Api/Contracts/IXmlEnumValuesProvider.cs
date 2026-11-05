namespace xml;

/// <summary>
/// Provides enumeration values used by an integration.
/// </summary>
public interface IXmlEnumValuesProvider
{
    /// <summary>
    /// Gets the enumeration values used by the integration.
    /// </summary>
    /// <returns>The enumeration values.</returns>
    XmlEnumValues GetXmlEnumValues();
}

