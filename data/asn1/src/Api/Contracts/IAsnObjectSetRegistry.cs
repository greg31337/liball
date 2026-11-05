namespace asn1;

/// <summary>
/// Registers and resolves dynamic additions to extensible information object sets.
/// </summary>
public interface IAsnObjectSetRegistry
{
    /// <summary>
    /// Registers one dynamic information object and its optional open-type dispatch entry.
    /// </summary>
    /// <param name="objectSet">The extensible compiled object set.</param>
    /// <param name="item">The information object to add.</param>
    /// <param name="dispatchEntry">The open-type dispatch entry, or null when the object has no open-type mapping.</param>
    public void RegisterObject(
        AsnInformationObjectSet objectSet,
        AsnInformationObject item,
        AsnOpenTypeDispatchEntry? dispatchEntry);

    /// <summary>
    /// Returns static objects followed by dynamic objects in registration order.
    /// </summary>
    /// <param name="objectSet">The compiled object set.</param>
    /// <returns>A snapshot containing the effective objects.</returns>
    public AsnInformationObject[] GetEffectiveObjects(AsnInformationObjectSet objectSet);

    /// <summary>
    /// Returns static entries followed by dynamic entries in registration order.
    /// </summary>
    /// <param name="objectSet">The compiled object set.</param>
    /// <returns>A snapshot containing the effective dispatch entries.</returns>
    public AsnOpenTypeDispatchEntry[] GetEffectiveDispatchEntries(
        AsnInformationObjectSet objectSet);

    /// <summary>
    /// Resolves an open-type dispatch entry by its normalized UNIQUE-field value.
    /// </summary>
    /// <param name="objectSet">The compiled object set.</param>
    /// <param name="key">The dispatch key.</param>
    /// <returns>The matching entry, or null when no entry matches.</returns>
    public AsnOpenTypeDispatchEntry? ResolveDispatchEntry(
        AsnInformationObjectSet objectSet,
        AsnValue key);
}
