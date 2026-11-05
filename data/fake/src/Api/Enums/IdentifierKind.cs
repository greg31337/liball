namespace fake;

/// <summary>
/// Selects an identifier contract for detailed validation and generation metadata.
/// </summary>
public enum IdentifierKind
{
    /// <summary>Canonical UUID text without a required version.</summary>
    Uuid = 0,
    /// <summary>Canonical RFC-variant UUID version 4 text.</summary>
    Uuid4 = 1,
    /// <summary>Canonical RFC-variant UUID version 7 text.</summary>
    Uuid7 = 2,
    /// <summary>Canonical 128-bit ULID text.</summary>
    Ulid = 3,
    /// <summary>MongoDB ObjectId-shaped hexadecimal text.</summary>
    ObjectId = 4,
    /// <summary>Sortable timestamp identifier hexadecimal text.</summary>
    SortableTimestampId = 5,
    /// <summary>Restricted DOI-shaped text.</summary>
    Doi = 6,
    /// <summary>Compact ISBN-10 text.</summary>
    Isbn10 = 7,
    /// <summary>Compact ISBN-13 text.</summary>
    Isbn13 = 8,
    /// <summary>Compact ISSN text.</summary>
    Issn = 9,
    /// <summary>Eight-digit EAN/GTIN text.</summary>
    Ean8 = 10,
    /// <summary>Twelve-digit UPC/GTIN text.</summary>
    UpcA = 11,
    /// <summary>A supported compact GTIN shape.</summary>
    Gtin = 12,
    /// <summary>Fifteen-digit IMEI text.</summary>
    Imei = 13,
    /// <summary>Sixteen-digit IMEISV text.</summary>
    ImeiSv = 14,
    /// <summary>Seventeen-character VIN text.</summary>
    Vin = 15,
    /// <summary>Generic project-fictional SKU text.</summary>
    Sku = 16,
    /// <summary>Generic project-fictional serial text.</summary>
    Serial = 17,
    /// <summary>Generic project-fictional lot text.</summary>
    Lot = 18,
    /// <summary>Generic project-fictional order text.</summary>
    Order = 19,
    /// <summary>Generic project-fictional invoice text.</summary>
    Invoice = 20,
    /// <summary>Generic project-fictional tracking text.</summary>
    Tracking = 21,
    /// <summary>A locale-backed passport identifier.</summary>
    Passport = 22,
    /// <summary>A locale-backed driving-licence identifier.</summary>
    DrivingLicence = 23,
    /// <summary>A locale-backed national identity identifier.</summary>
    NationalIdentity = 24,
    /// <summary>A locale-backed social identity identifier.</summary>
    SocialIdentity = 25,
    /// <summary>A locale-backed personal tax identifier.</summary>
    PersonalTax = 26,
    /// <summary>A locale-backed business-registration identifier.</summary>
    BusinessRegistration = 27,
    /// <summary>A locale-backed vehicle-registration identifier.</summary>
    VehicleRegistration = 28
}
