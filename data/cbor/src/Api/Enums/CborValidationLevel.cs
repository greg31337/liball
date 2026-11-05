namespace cbor;

/// <summary>
/// Selects the highest validation layer applied to CBOR data.
/// </summary>
public enum CborValidationLevel
{
    /// <summary>Checks only the CBOR well-formedness rules.</summary>
    WellFormedness = 0,
    /// <summary>Also checks basic data-model validity.</summary>
    Basic = 1,
    /// <summary>Also checks the validity of registered semantic tags.</summary>
    TagValidity = 2,
    /// <summary>Also checks the selected deterministic encoding profile.</summary>
    Deterministic = 3,
    /// <summary>Also applies caller-selected application policies.</summary>
    Application = 4
}
