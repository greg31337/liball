namespace asn1;

/// <summary>
/// Builds and queries compiled ASN.1 reflection catalogs.
/// </summary>
public interface IAsnReflectionService
{
    /// <summary>
    /// Builds a reflection catalog from a validation result.
    /// </summary>
    /// <param name="validation">The validated schema.</param>
    /// <returns>The reflection catalog.</returns>
    public AsnReflectionCatalog BuildCatalog(AsnValidationResult validation);

    /// <summary>
    /// Resolves a module by name.
    /// </summary>
    /// <param name="catalog">The reflection catalog.</param>
    /// <param name="moduleName">The qualified module name.</param>
    /// <returns>The module descriptor, or null when no module matches.</returns>
    public AsnModuleDescriptor? ResolveModule(AsnReflectionCatalog catalog, string moduleName);

    /// <summary>
    /// Resolves a type by module and type name.
    /// </summary>
    /// <param name="catalog">The reflection catalog.</param>
    /// <param name="moduleName">The qualified module name.</param>
    /// <param name="typeName">The type name.</param>
    /// <returns>The type descriptor, or null when no type matches.</returns>
    public AsnTypeDescriptor? ResolveType(AsnReflectionCatalog catalog, string moduleName, string typeName);

    /// <summary>
    /// Resolves a value by module and value name.
    /// </summary>
    /// <param name="catalog">The reflection catalog.</param>
    /// <param name="moduleName">The qualified module name.</param>
    /// <param name="valueName">The value name.</param>
    /// <returns>The value descriptor, or null when no value matches.</returns>
    public AsnValueDescriptor? ResolveValue(AsnReflectionCatalog catalog, string moduleName, string valueName);

    /// <summary>
    /// Returns all types in deterministic traversal order.
    /// </summary>
    /// <param name="catalog">The reflection catalog.</param>
    /// <returns>The type descriptors.</returns>
    public AsnTypeDescriptor[] GetTypes(AsnReflectionCatalog catalog);
}
