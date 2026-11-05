namespace units;

/// <summary>
/// Identifies the type of registry consistency problem that was found.
/// </summary>
public enum RegistryValidationIssueKind
{
    /// <summary>
    /// Reports a definition that has no dimension vector.
    /// </summary>
    MissingDimension = 0,
    /// <summary>
    /// Reports a quantity kind whose default unit is not registered.
    /// </summary>
    MissingDefaultUnit = 1,
    /// <summary>
    /// Reports an alias that is registered more than once.
    /// </summary>
    DuplicateAlias = 2,
    /// <summary>
    /// Reports a unit mapped to a quantity kind with a different dimension.
    /// </summary>
    IncompatibleQuantityKindMapping = 3,
    /// <summary>
    /// Reports an accepted quantity-kind unit that is not registered.
    /// </summary>
    MissingAcceptedUnit = 4,
    /// <summary>
    /// Reports a unit-system base unit that is not registered.
    /// </summary>
    MissingSystemBaseUnit = 5,
    /// <summary>
    /// Reports a preferred-unit mapping whose quantity kind is not registered.
    /// </summary>
    MissingPreferredQuantityKind = 6,
    /// <summary>
    /// Reports a preferred unit that is not registered.
    /// </summary>
    MissingPreferredUnit = 7,
    /// <summary>
    /// Reports a preferred unit whose dimension differs from its quantity kind.
    /// </summary>
    IncompatiblePreferredUnitMapping = 8,
    /// <summary>
    /// Reports a repeated identifier within one metadata catalog.
    /// </summary>
    DuplicateMetadataId = 9,
    /// <summary>
    /// Reports a repeated alias within one metadata catalog.
    /// </summary>
    DuplicateMetadataAlias = 10,
    /// <summary>
    /// Reports equivalency metadata that cannot consistently connect its quantity kinds.
    /// </summary>
    InvalidEquivalency = 11,
    /// <summary>
    /// Reports special-unit conversion metadata that cannot be used by the special conversion pipeline.
    /// </summary>
    InvalidSpecialConversionMetadata = 12,
    /// <summary>
    /// Reports physical-constant metadata that is incomplete or incompatible with registered units.
    /// </summary>
    InvalidPhysicalConstantMetadata = 13
}
