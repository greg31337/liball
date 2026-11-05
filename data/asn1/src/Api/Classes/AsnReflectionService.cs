namespace asn1;

/// <summary>
/// Exposes reflection-catalog operations through a composed service implementation.
/// </summary>
public class AsnReflectionService : IAsnReflectionService
{
    internal IAsnReflectionService Implementation;

    /// <summary>
    /// Initializes a reflection-service facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that provides reflection operations.</param>
    public AsnReflectionService(IAsnReflectionService implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Builds a reflection catalog from a validation result.
    /// </summary>
    /// <param name="validation">The validated schema.</param>
    /// <returns>The reflection catalog.</returns>
    public AsnReflectionCatalog BuildCatalog(AsnValidationResult validation)
    {
        return this.Implementation.BuildCatalog(validation);
    }

    /// <summary>
    /// Resolves a module by name.
    /// </summary>
    /// <param name="catalog">The reflection catalog.</param>
    /// <param name="moduleName">The module name.</param>
    /// <returns>The matching module descriptor, or null.</returns>
    public AsnModuleDescriptor? ResolveModule(AsnReflectionCatalog catalog, string moduleName)
    {
        return this.Implementation.ResolveModule(catalog, moduleName);
    }

    /// <summary>
    /// Resolves a type by module and type name.
    /// </summary>
    /// <param name="catalog">The reflection catalog.</param>
    /// <param name="moduleName">The module name.</param>
    /// <param name="typeName">The type name.</param>
    /// <returns>The matching type descriptor, or null.</returns>
    public AsnTypeDescriptor? ResolveType(AsnReflectionCatalog catalog, string moduleName, string typeName)
    {
        return this.Implementation.ResolveType(catalog, moduleName, typeName);
    }

    /// <summary>
    /// Resolves a value by module and value name.
    /// </summary>
    /// <param name="catalog">The reflection catalog.</param>
    /// <param name="moduleName">The module name.</param>
    /// <param name="valueName">The value name.</param>
    /// <returns>The matching value descriptor, or null.</returns>
    public AsnValueDescriptor? ResolveValue(AsnReflectionCatalog catalog, string moduleName, string valueName)
    {
        return this.Implementation.ResolveValue(catalog, moduleName, valueName);
    }

    /// <summary>
    /// Returns all reflected types in deterministic order.
    /// </summary>
    /// <param name="catalog">The reflection catalog.</param>
    /// <returns>The reflected type descriptors.</returns>
    public AsnTypeDescriptor[] GetTypes(AsnReflectionCatalog catalog)
    {
        return this.Implementation.GetTypes(catalog);
    }
}
