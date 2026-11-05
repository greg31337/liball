namespace fake;

/// <summary>
/// Selects a phone-number modeling edge case.
/// </summary>
public enum PhoneEdgeCase
{
    /// <summary>Provides related fixture values of different valid-looking lengths.</summary>
    VariableLength = 0,

    /// <summary>Provides a calling code associated with more than one region.</summary>
    SharedCallingCode = 1,

    /// <summary>Provides an international form whose national number retains a leading zero.</summary>
    SignificantLeadingZero = 2,

    /// <summary>Provides a phone fixture with a separately retained extension.</summary>
    Extension = 3,

    /// <summary>Provides a service under a non-geographic calling code.</summary>
    NonGeographicService = 4,

    /// <summary>Provides a number shape that does not uniquely determine its category.</summary>
    AmbiguousCategory = 5
}
