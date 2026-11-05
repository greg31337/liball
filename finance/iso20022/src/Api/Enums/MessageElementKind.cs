namespace iso20022;

/// <summary>Identifies the normalized subtype of a business or message element.</summary>
public enum MessageElementKind
{
    /// <summary>The element subtype is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>An element with a simple or complex attribute type.</summary>
    Attribute = 1,
    /// <summary>An association end relating two component types.</summary>
    AssociationEnd = 2
}
