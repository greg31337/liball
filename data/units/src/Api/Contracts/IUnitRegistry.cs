namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="UnitRegistry"/>.
/// </summary>
public interface IUnitRegistry
{
    /// <summary>
    /// Adds unit to the registry unless validation rejects it.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddUnit(UnitDefinition unit);
    /// <summary>
    /// Adds alias to the registry unless validation rejects it.
    /// </summary>
    /// <param name="unitId">The stable unit identifier.</param>
    /// <param name="alias">The alternate lookup text.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddAlias(string unitId, string alias);
    /// <summary>
    /// Adds quantity kind to the registry unless validation rejects it.
    /// </summary>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddQuantityKind(QuantityKindDefinition quantityKind);
    /// <summary>
    /// Adds system to the registry unless validation rejects it.
    /// </summary>
    /// <param name="system">The target unit-system definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddSystem(UnitSystemDefinition system);
    /// <summary>
    /// Adds constant to the registry unless validation rejects it.
    /// </summary>
    /// <param name="constant">The constant to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddConstant(PhysicalConstantDefinition constant);
    /// <summary>
    /// Adds equivalency to the registry unless validation rejects it.
    /// </summary>
    /// <param name="equivalency">The explicitly selected equivalency definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddEquivalency(EquivalencyDefinition equivalency);
    /// <summary>
    /// Adds prefix to the registry unless validation rejects it.
    /// </summary>
    /// <param name="prefix">The prefix to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddPrefix(PrefixDefinition prefix);
    /// <summary>
    /// Prevents any later registration or alias changes.
    /// </summary>
    public void Freeze();
    /// <summary>
    /// Reports whether no further changes may be registered.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsFrozen();
    /// <summary>
    /// Returns a copy of the units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] GetUnits();
    /// <summary>
    /// Returns a copy of the quantity kinds.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public QuantityKindDefinition[] GetQuantityKinds();
    /// <summary>
    /// Returns a copy of the systems.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitSystemDefinition[] GetSystems();
    /// <summary>
    /// Returns a copy of the constants.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public PhysicalConstantDefinition[] GetConstants();
    /// <summary>
    /// Finds the unit system matching the supplied identity text.
    /// </summary>
    /// <param name="idOrName">The identifier or name to find.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public UnitSystemLookupResult FindSystem(string idOrName);
    /// <summary>
    /// Finds the physical constant matching the supplied identity text.
    /// </summary>
    /// <param name="idOrName">The identifier, name, symbol, or alias to find.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public PhysicalConstantLookupResult FindPhysicalConstant(string idOrName);
    /// <summary>
    /// Finds the quantity kind or kinds that accept the supplied unit.
    /// </summary>
    /// <param name="unit">The applicable unit definition.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public QuantityKindLookupResult FindQuantityKindByUnit(UnitDefinition unit);
    /// <summary>
    /// Returns a copy of the equivalencies.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public EquivalencyDefinition[] GetEquivalencies();
    /// <summary>
    /// Returns the prefix matching the supplied id, or null when none matches.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindPrefixById(string? id);
    /// <summary>
    /// Returns the prefix matching the supplied name, or null when none matches.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindPrefixByName(string? name);
    /// <summary>
    /// Returns the prefix matching the supplied symbol, or null when none matches.
    /// </summary>
    /// <param name="symbol">The unit or metadata symbol.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindPrefixBySymbol(string? symbol);
    /// <summary>
    /// Returns every registered unit matching the supplied identity text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnits(string text);
    /// <summary>
    /// Returns the equivalency with the supplied identifier, or null when none matches.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <returns>The resulting equivalency definition.</returns>
    public EquivalencyDefinition? FindEquivalencyById(string id);
    /// <summary>
    /// Returns every registered unit matching the supplied id.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnitsById(string id);
    /// <summary>
    /// Returns every registered unit matching the supplied name.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnitsByName(string name);
    /// <summary>
    /// Returns every registered unit matching the supplied symbol.
    /// </summary>
    /// <param name="symbol">The unit or metadata symbol.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnitsBySymbol(string symbol);
    /// <summary>
    /// Returns every registered unit matching the supplied alias.
    /// </summary>
    /// <param name="alias">The alternate lookup text.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnitsByAlias(string alias);
    /// <summary>
    /// Validates the registry's unit and metadata relationships without changing the registry.
    /// </summary>
    /// <returns>The resulting registry validation report.</returns>
    public RegistryValidationReport Validate();
}
