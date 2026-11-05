namespace units;

/// <summary>
/// Stores units and related metadata used by lookup, parsing, and conversion workflows.
/// </summary>
/// <remarks>
/// Registration is allowed until Freeze is called. Lookup reports ambiguity instead of choosing silently when several definitions match the same text.
/// </remarks>
public class UnitRegistry : IUnitCatalog, IUnitRegistry
{
    internal System.Collections.Generic.List<UnitDefinition> _units;
    internal System.Collections.Generic.List<string> _aliasUnitIds;
    internal System.Collections.Generic.List<string> _aliasTexts;
    internal System.Collections.Generic.List<QuantityKindDefinition> _quantityKinds;
    internal System.Collections.Generic.List<UnitSystemDefinition> _systems;
    internal System.Collections.Generic.List<PhysicalConstantDefinition> _constants;
    internal System.Collections.Generic.List<EquivalencyDefinition> _equivalencies;
    internal System.Collections.Generic.List<PrefixDefinition> _prefixes;
    internal bool _frozen;

    /// <summary>
    /// Initializes an empty unit registry with default values.
    /// </summary>
    public UnitRegistry()
    {
        _units = new System.Collections.Generic.List<UnitDefinition>();
        _aliasUnitIds = new System.Collections.Generic.List<string>();
        _aliasTexts = new System.Collections.Generic.List<string>();
        _quantityKinds = new System.Collections.Generic.List<QuantityKindDefinition>();
        _systems = new System.Collections.Generic.List<UnitSystemDefinition>();
        _constants = new System.Collections.Generic.List<PhysicalConstantDefinition>();
        _equivalencies = new System.Collections.Generic.List<EquivalencyDefinition>();
        _prefixes = new System.Collections.Generic.List<PrefixDefinition>();
        _frozen = false;
    }

    /// <summary>
    /// Adds unit to the registry unless validation rejects it.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (_frozen)
        {
            return CreateFrozenFailure();
        }

        if (FindUnitsById(unit.GetId()).Length > 0)
        {
            return OperationResult.CreateFailure(UnitErrorCode.RegistryDuplicateId, "Unit id is already registered.");
        }

        if (HasAmbiguousSymbol(unit))
        {
            return OperationResult.CreateFailure(UnitErrorCode.RegistryAmbiguousSymbol, "Unit symbol is already registered for a non-equivalent unit.");
        }

        _units.Add(unit);
        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Adds alias to the registry unless validation rejects it.
    /// </summary>
    /// <param name="unitId">The stable unit identifier.</param>
    /// <param name="alias">The alternate lookup text.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddAlias(string unitId, string alias)
    {
        ValidateRequiredText(unitId, "unitId");
        ValidateRequiredText(alias, "alias");

        if (_frozen)
        {
            return CreateFrozenFailure();
        }

        if (FindUnitsById(unitId).Length == 0)
        {
            return OperationResult.CreateFailure(UnitErrorCode.LookupNotFound, "Unit id was not found.");
        }

        _aliasUnitIds.Add(unitId);
        _aliasTexts.Add(alias);
        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Adds quantity kind to the registry unless validation rejects it.
    /// </summary>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddQuantityKind(QuantityKindDefinition quantityKind)
    {
        if (quantityKind == null)
        {
            throw new System.ArgumentNullException("quantityKind");
        }

        if (_frozen)
        {
            return CreateFrozenFailure();
        }

        if (FindQuantityKindById(quantityKind.GetId()) != null)
        {
            return OperationResult.CreateFailure(UnitErrorCode.RegistryDuplicateId, "Quantity kind id is already registered.");
        }

        OperationResult relationshipResult = ValidateQuantityKindRelationships(quantityKind);
        if (relationshipResult.IsFailure())
        {
            return relationshipResult;
        }

        _quantityKinds.Add(quantityKind);
        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Adds system to the registry unless validation rejects it.
    /// </summary>
    /// <param name="system">The target unit-system definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddSystem(UnitSystemDefinition system)
    {
        if (system == null)
        {
            throw new System.ArgumentNullException("system");
        }

        if (_frozen)
        {
            return CreateFrozenFailure();
        }

        if (FindSystemById(system.GetId()) != null)
        {
            return OperationResult.CreateFailure(UnitErrorCode.RegistryDuplicateId, "Unit system id is already registered.");
        }

        OperationResult relationshipResult = ValidateSystemRelationships(system);
        if (relationshipResult.IsFailure())
        {
            return relationshipResult;
        }

        _systems.Add(system);
        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Adds constant to the registry unless validation rejects it.
    /// </summary>
    /// <param name="constant">The constant to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddConstant(PhysicalConstantDefinition constant)
    {
        if (constant == null)
        {
            throw new System.ArgumentNullException("constant");
        }

        if (_frozen)
        {
            return CreateFrozenFailure();
        }

        if (FindPhysicalConstantById(constant.GetId()) != null)
        {
            return OperationResult.CreateFailure(UnitErrorCode.RegistryDuplicateId, "Physical constant id is already registered.");
        }

        _constants.Add(constant);
        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Adds equivalency to the registry unless validation rejects it.
    /// </summary>
    /// <param name="equivalency">The explicitly selected equivalency definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddEquivalency(EquivalencyDefinition equivalency)
    {
        if (equivalency == null)
        {
            throw new System.ArgumentNullException("equivalency");
        }

        if (_frozen)
        {
            return CreateFrozenFailure();
        }

        if (FindEquivalencyById(equivalency.GetId()) != null)
        {
            return OperationResult.CreateFailure(UnitErrorCode.RegistryDuplicateId, "Equivalency id is already registered.");
        }

        _equivalencies.Add(equivalency);
        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Adds prefix to the registry unless validation rejects it.
    /// </summary>
    /// <param name="prefix">The prefix to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult AddPrefix(PrefixDefinition prefix)
    {
        if (prefix == null)
        {
            throw new System.ArgumentNullException("prefix");
        }

        if (_frozen)
        {
            return CreateFrozenFailure();
        }

        if (FindPrefixById(prefix.GetId()) != null || FindPrefixByName(prefix.GetName()) != null || FindPrefixBySymbol(prefix.GetSymbol()) != null)
        {
            return OperationResult.CreateFailure(UnitErrorCode.RegistryDuplicateId, "Prefix is already registered.");
        }

        _prefixes.Add(prefix);
        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Prevents any later registration or alias changes.
    /// </summary>
    public void Freeze()
    {
        _frozen = true;
    }

    /// <summary>
    /// Reports whether no further changes may be registered.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsFrozen()
    {
        return _frozen;
    }

    /// <summary>
    /// Returns a copy of the units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] GetUnits()
    {
        return _units.ToArray();
    }

    /// <summary>
    /// Returns a copy of the quantity kinds.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public QuantityKindDefinition[] GetQuantityKinds()
    {
        return _quantityKinds.ToArray();
    }

    /// <summary>
    /// Returns a copy of the systems.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitSystemDefinition[] GetSystems()
    {
        return _systems.ToArray();
    }

    /// <summary>
    /// Returns a copy of the constants.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public PhysicalConstantDefinition[] GetConstants()
    {
        return _constants.ToArray();
    }

    /// <summary>
    /// Finds the unit system matching the supplied identity text.
    /// </summary>
    /// <param name="idOrName">The identifier or name to find.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public UnitSystemLookupResult FindSystem(string idOrName)
    {
        ValidateRequiredText(idOrName, "idOrName");

        System.Collections.Generic.List<UnitSystemDefinition> results = new System.Collections.Generic.List<UnitSystemDefinition>();
        int index = 0;
        while (index < _systems.Count)
        {
            UnitSystemDefinition system = _systems[index];
            if (string.Equals(system.GetId(), idOrName, System.StringComparison.Ordinal) || string.Equals(system.GetName(), idOrName, System.StringComparison.Ordinal))
            {
                results.Add(system);
            }

            index = index + 1;
        }

        return UnitSystemLookupResult.FromMatches(results.ToArray(), idOrName);
    }

    /// <summary>
    /// Finds the physical constant matching the supplied identity text.
    /// </summary>
    /// <param name="idOrName">The identifier, name, symbol, or alias to find.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public PhysicalConstantLookupResult FindPhysicalConstant(string idOrName)
    {
        ValidateRequiredText(idOrName, "idOrName");

        System.Collections.Generic.List<PhysicalConstantDefinition> results = new System.Collections.Generic.List<PhysicalConstantDefinition>();
        int index = 0;
        while (index < _constants.Count)
        {
            PhysicalConstantDefinition definition = _constants[index];
            if (definition.MatchesText(idOrName))
            {
                results.Add(definition);
            }

            index = index + 1;
        }

        return PhysicalConstantLookupResult.FromMatches(results.ToArray(), idOrName);
    }

    /// <summary>
    /// Returns a copy of the equivalencies.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public EquivalencyDefinition[] GetEquivalencies()
    {
        return _equivalencies.ToArray();
    }

    /// <summary>
    /// Returns a copy of the prefixes.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public PrefixDefinition[] GetPrefixes()
    {
        return _prefixes.ToArray();
    }

    /// <summary>
    /// Returns the prefix matching the supplied id, or null when none matches.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindPrefixById(string? id)
    {
        return FindPrefixByText(id, 0);
    }

    /// <summary>
    /// Returns the prefix matching the supplied name, or null when none matches.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindPrefixByName(string? name)
    {
        return FindPrefixByText(name, 1);
    }

    /// <summary>
    /// Returns the prefix matching the supplied symbol, or null when none matches.
    /// </summary>
    /// <param name="symbol">The unit or metadata symbol.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindPrefixBySymbol(string? symbol)
    {
        return FindPrefixByText(symbol, 2);
    }

    /// <summary>
    /// Returns every registered unit matching the supplied identity text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnits(string text)
    {
        ValidateRequiredText(text, "text");

        System.Collections.Generic.List<UnitDefinition> results = new System.Collections.Generic.List<UnitDefinition>();
        AddMatchesById(results, text);
        AddMatchesByName(results, text);
        AddMatchesBySymbol(results, text);
        AddMatchesByAlias(results, text);
        return results.ToArray();
    }

    /// <summary>
    /// Looks up one unit and reports not-found or ambiguous matches explicitly.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result that describes success or failure.</returns>
    public UnitLookupResult FindUnit(string text)
    {
        return UnitLookupResult.FromMatches(FindUnits(text), text);
    }

    /// <summary>
    /// Looks up one unit and reports not-found or ambiguous matches explicitly.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <returns>A result that describes success or failure.</returns>
    public UnitLookupResult FindUnitById(string id)
    {
        return UnitLookupResult.FromMatches(FindUnitsById(id), id);
    }

    /// <summary>
    /// Looks up one quantity kind and reports not-found or ambiguous matches explicitly.
    /// </summary>
    /// <param name="idOrName">The identifier or name to find.</param>
    /// <returns>A result that describes success or failure.</returns>
    public QuantityKindLookupResult FindQuantityKind(string idOrName)
    {
        ValidateRequiredText(idOrName, "idOrName");

        System.Collections.Generic.List<QuantityKindDefinition> results = new System.Collections.Generic.List<QuantityKindDefinition>();
        int index = 0;

        while (index < _quantityKinds.Count)
        {
            QuantityKindDefinition quantityKind = _quantityKinds[index];
            if (quantityKind.MatchesText(idOrName))
            {
                AddUniqueQuantityKind(results, quantityKind);
            }

            index = index + 1;
        }

        return QuantityKindLookupResult.FromMatches(results.ToArray(), idOrName);
    }

    /// <summary>
    /// Finds the quantity kind or kinds that accept the supplied unit.
    /// </summary>
    /// <param name="unit">The applicable unit definition.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public QuantityKindLookupResult FindQuantityKindByUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        System.Collections.Generic.List<QuantityKindDefinition> results = new System.Collections.Generic.List<QuantityKindDefinition>();
        int index = 0;

        while (index < _quantityKinds.Count)
        {
            QuantityKindDefinition quantityKind = _quantityKinds[index];
            if (quantityKind.AcceptsUnit(unit))
            {
                results.Add(quantityKind);
            }

            index = index + 1;
        }

        return QuantityKindLookupResult.FromMatches(results.ToArray(), unit.GetId());
    }

    internal QuantityKindDefinition? FindQuantityKindById(string id)
    {
        int index = 0;
        while (index < _quantityKinds.Count)
        {
            QuantityKindDefinition quantityKind = _quantityKinds[index];
            if (string.Equals(quantityKind.GetId(), id, System.StringComparison.Ordinal))
            {
                return quantityKind;
            }

            index = index + 1;
        }

        return null;
    }

    internal UnitSystemDefinition? FindSystemById(string id)
    {
        int index = 0;
        while (index < _systems.Count)
        {
            UnitSystemDefinition system = _systems[index];
            if (string.Equals(system.GetId(), id, System.StringComparison.Ordinal))
            {
                return system;
            }

            index = index + 1;
        }

        return null;
    }

    internal PhysicalConstantDefinition? FindPhysicalConstantById(string id)
    {
        int index = 0;
        while (index < _constants.Count)
        {
            PhysicalConstantDefinition constant = _constants[index];
            if (string.Equals(constant.GetId(), id, System.StringComparison.Ordinal))
            {
                return constant;
            }

            index = index + 1;
        }

        return null;
    }

    internal OperationResult ValidateQuantityKindRelationships(QuantityKindDefinition quantityKind)
    {
        string[] unitIds = quantityKind.GetAcceptedUnitIds();
        int index = 0;
        while (index < unitIds.Length)
        {
            UnitDefinition[] units = FindUnitsById(unitIds[index]);
            if (units.Length == 0)
            {
                return OperationResult.CreateFailure(UnitErrorCode.RegistryInvalidMetadata, "Quantity kind references an unregistered unit: " + unitIds[index]);
            }

            if (!quantityKind.GetDimension().EqualsDimension(units[0].GetDimension()))
            {
                return OperationResult.CreateFailure(UnitErrorCode.RegistryInvalidMetadata, "Quantity kind references an incompatible unit: " + unitIds[index]);
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    internal OperationResult ValidateSystemRelationships(UnitSystemDefinition system)
    {
        string[] baseUnitIds = system.GetBaseUnitIds();
        int baseIndex = 0;
        while (baseIndex < baseUnitIds.Length)
        {
            if (FindUnitsById(baseUnitIds[baseIndex]).Length == 0)
            {
                return OperationResult.CreateFailure(UnitErrorCode.RegistryInvalidMetadata, "Unit system references an unregistered base unit: " + baseUnitIds[baseIndex]);
            }

            baseIndex = baseIndex + 1;
        }

        string[] quantityKindIds = system.GetPreferredQuantityKindIds();
        string[] preferredUnitIds = system.GetPreferredUnitIds();
        int preferredIndex = 0;
        while (preferredIndex < quantityKindIds.Length)
        {
            QuantityKindDefinition? quantityKind = FindQuantityKindById(quantityKindIds[preferredIndex]);
            if (quantityKind == null)
            {
                return OperationResult.CreateFailure(UnitErrorCode.RegistryInvalidMetadata, "Unit system references an unregistered quantity kind: " + quantityKindIds[preferredIndex]);
            }

            UnitDefinition[] units = FindUnitsById(preferredUnitIds[preferredIndex]);
            if (units.Length == 0)
            {
                return OperationResult.CreateFailure(UnitErrorCode.RegistryInvalidMetadata, "Unit system references an unregistered preferred unit: " + preferredUnitIds[preferredIndex]);
            }

            if (!quantityKind.GetDimension().EqualsDimension(units[0].GetDimension()))
            {
                return OperationResult.CreateFailure(UnitErrorCode.RegistryInvalidMetadata, "Unit system references an incompatible preferred unit: " + preferredUnitIds[preferredIndex]);
            }

            preferredIndex = preferredIndex + 1;
        }

        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Returns the equivalency matching the supplied identity text, or null when none matches.
    /// </summary>
    /// <param name="idOrName">The identifier or name to find.</param>
    /// <returns>The resulting equivalency definition.</returns>
    public EquivalencyDefinition? FindEquivalency(string idOrName)
    {
        ValidateRequiredText(idOrName, "idOrName");

        int index = 0;
        while (index < _equivalencies.Count)
        {
            EquivalencyDefinition equivalency = _equivalencies[index];
            if (equivalency.MatchesText(idOrName))
            {
                return equivalency;
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Returns the equivalency with the supplied identifier, or null when none matches.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <returns>The resulting equivalency definition.</returns>
    public EquivalencyDefinition? FindEquivalencyById(string id)
    {
        ValidateRequiredText(id, "id");

        int index = 0;
        while (index < _equivalencies.Count)
        {
            EquivalencyDefinition equivalency = _equivalencies[index];
            if (string.Equals(equivalency.GetId(), id, System.StringComparison.Ordinal))
            {
                return equivalency;
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Returns every registered unit matching the supplied id.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnitsById(string id)
    {
        ValidateRequiredText(id, "id");

        System.Collections.Generic.List<UnitDefinition> results = new System.Collections.Generic.List<UnitDefinition>();
        AddMatchesById(results, id);
        return results.ToArray();
    }

    /// <summary>
    /// Returns every registered unit matching the supplied name.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnitsByName(string name)
    {
        ValidateRequiredText(name, "name");

        System.Collections.Generic.List<UnitDefinition> results = new System.Collections.Generic.List<UnitDefinition>();
        AddMatchesByName(results, name);
        return results.ToArray();
    }

    /// <summary>
    /// Returns every registered unit matching the supplied symbol.
    /// </summary>
    /// <param name="symbol">The unit or metadata symbol.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnitsBySymbol(string symbol)
    {
        ValidateRequiredText(symbol, "symbol");

        System.Collections.Generic.List<UnitDefinition> results = new System.Collections.Generic.List<UnitDefinition>();
        AddMatchesBySymbol(results, symbol);
        return results.ToArray();
    }

    /// <summary>
    /// Returns every registered unit matching the supplied alias.
    /// </summary>
    /// <param name="alias">The alternate lookup text.</param>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] FindUnitsByAlias(string alias)
    {
        ValidateRequiredText(alias, "alias");

        System.Collections.Generic.List<UnitDefinition> results = new System.Collections.Generic.List<UnitDefinition>();
        AddMatchesByAlias(results, alias);
        return results.ToArray();
    }

    /// <summary>
    /// Validates the registry's unit and metadata relationships without changing the registry.
    /// </summary>
    /// <returns>The resulting registry validation report.</returns>
    public RegistryValidationReport Validate()
    {
        System.Collections.Generic.List<RegistryValidationIssue> issues = new System.Collections.Generic.List<RegistryValidationIssue>();

        AddMissingDimensionIssues(issues);
        AddQuantityKindMappingIssues(issues);
        AddSystemMappingIssues(issues);
        AddDuplicateMetadataIdIssues(issues);
        AddDuplicateAliasIssues(issues);
        AddDuplicateMetadataAliasIssues(issues);
        AddEquivalencyIssues(issues);
        AddSpecialConversionIssues(issues);
        AddPhysicalConstantIssues(issues);

        return new RegistryValidationReport(issues.ToArray());
    }

    internal void AddMissingDimensionIssues(System.Collections.Generic.List<RegistryValidationIssue> issues)
    {
        int unitIndex = 0;
        while (unitIndex < _units.Count)
        {
            UnitDefinition unit = _units[unitIndex];
            if (unit.GetDimension() == null)
            {
                AddIssue(issues, RegistryValidationIssueKind.MissingDimension, unit.GetId(), "", "Unit is missing a dimension.");
            }

            unitIndex = unitIndex + 1;
        }

        int kindIndex = 0;
        while (kindIndex < _quantityKinds.Count)
        {
            QuantityKindDefinition definition = _quantityKinds[kindIndex];
            if (definition.GetDimension() == null)
            {
                AddIssue(issues, RegistryValidationIssueKind.MissingDimension, definition.GetId(), "", "Quantity kind is missing a dimension.");
            }

            kindIndex = kindIndex + 1;
        }
    }

    internal void AddQuantityKindMappingIssues(System.Collections.Generic.List<RegistryValidationIssue> issues)
    {
        int index = 0;
        while (index < _quantityKinds.Count)
        {
            AddQuantityKindMappingIssuesForKind(issues, _quantityKinds[index]);

            index = index + 1;
        }
    }

    internal void AddQuantityKindMappingIssuesForKind(System.Collections.Generic.List<RegistryValidationIssue> issues, QuantityKindDefinition quantityKind)
    {
        UnitDefinition[] defaultUnits = FindUnitsById(quantityKind.GetDefaultUnitId());
        if (defaultUnits.Length == 0)
        {
            AddIssue(issues, RegistryValidationIssueKind.MissingDefaultUnit, quantityKind.GetId(), quantityKind.GetDefaultUnitId(), "Quantity kind default unit is not registered.");
        }
        else
        {
            AddIncompatibleQuantityKindUnits(issues, quantityKind, defaultUnits, quantityKind.GetDefaultUnitId());
        }

        string[] acceptedUnitIds = quantityKind.GetAcceptedUnitIds();
        int index = 0;
        while (index < acceptedUnitIds.Length)
        {
            string unitId = acceptedUnitIds[index];
            if (!string.Equals(unitId, quantityKind.GetDefaultUnitId(), System.StringComparison.Ordinal))
            {
                UnitDefinition[] units = FindUnitsById(unitId);
                if (units.Length == 0)
                {
                    AddIssue(issues, RegistryValidationIssueKind.MissingAcceptedUnit, quantityKind.GetId(), unitId, "Quantity kind accepted unit is not registered.");
                }
                else
                {
                    AddIncompatibleQuantityKindUnits(issues, quantityKind, units, unitId);
                }
            }

            index = index + 1;
        }
    }

    internal void AddSystemMappingIssues(System.Collections.Generic.List<RegistryValidationIssue> issues)
    {
        int systemIndex = 0;
        while (systemIndex < _systems.Count)
        {
            UnitSystemDefinition system = _systems[systemIndex];
            string[] baseUnitIds = system.GetBaseUnitIds();
            int baseIndex = 0;
            while (baseIndex < baseUnitIds.Length)
            {
                if (FindUnitsById(baseUnitIds[baseIndex]).Length == 0)
                {
                    AddIssue(issues, RegistryValidationIssueKind.MissingSystemBaseUnit, system.GetId(), baseUnitIds[baseIndex], "Unit system base unit is not registered.");
                }

                baseIndex = baseIndex + 1;
            }

            AddPreferredUnitMappingIssues(issues, system);
            systemIndex = systemIndex + 1;
        }
    }

    internal void AddPreferredUnitMappingIssues(System.Collections.Generic.List<RegistryValidationIssue> issues, UnitSystemDefinition system)
    {
        string[] quantityKindIds = system.GetPreferredQuantityKindIds();
        string[] preferredUnitIds = system.GetPreferredUnitIds();
        int index = 0;
        while (index < quantityKindIds.Length)
        {
            QuantityKindDefinition? quantityKind = FindQuantityKindById(quantityKindIds[index]);
            UnitDefinition[] units = FindUnitsById(preferredUnitIds[index]);
            if (quantityKind == null)
            {
                AddIssue(issues, RegistryValidationIssueKind.MissingPreferredQuantityKind, system.GetId(), quantityKindIds[index], "Unit system preferred-unit quantity kind is not registered.");
            }

            if (units.Length == 0)
            {
                AddIssue(issues, RegistryValidationIssueKind.MissingPreferredUnit, system.GetId(), preferredUnitIds[index], "Unit system preferred unit is not registered.");
            }
            else if (quantityKind != null)
            {
                int unitIndex = 0;
                while (unitIndex < units.Length)
                {
                    if (!quantityKind.GetDimension().EqualsDimension(units[unitIndex].GetDimension()))
                    {
                        AddIssue(issues, RegistryValidationIssueKind.IncompatiblePreferredUnitMapping, system.GetId(), preferredUnitIds[index], "Unit system preferred unit has an incompatible dimension.");
                    }

                    unitIndex = unitIndex + 1;
                }
            }

            index = index + 1;
        }
    }

    internal void AddDuplicateMetadataIdIssues(System.Collections.Generic.List<RegistryValidationIssue> issues)
    {
        System.Collections.Generic.List<string> ids = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < _units.Count)
        {
            AddMetadataIdOccurrence(issues, ids, _units[index].GetId(), "unit");
            index = index + 1;
        }

        ids = new System.Collections.Generic.List<string>();
        index = 0;
        while (index < _quantityKinds.Count)
        {
            AddMetadataIdOccurrence(issues, ids, _quantityKinds[index].GetId(), "quantity kind");
            index = index + 1;
        }

        ids = new System.Collections.Generic.List<string>();
        index = 0;
        while (index < _systems.Count)
        {
            AddMetadataIdOccurrence(issues, ids, _systems[index].GetId(), "unit system");
            index = index + 1;
        }

        ids = new System.Collections.Generic.List<string>();
        index = 0;
        while (index < _constants.Count)
        {
            AddMetadataIdOccurrence(issues, ids, _constants[index].GetId(), "physical constant");
            index = index + 1;
        }

        ids = new System.Collections.Generic.List<string>();
        index = 0;
        while (index < _equivalencies.Count)
        {
            AddMetadataIdOccurrence(issues, ids, _equivalencies[index].GetId(), "equivalency");
            index = index + 1;
        }

        ids = new System.Collections.Generic.List<string>();
        index = 0;
        while (index < _prefixes.Count)
        {
            AddMetadataIdOccurrence(issues, ids, _prefixes[index].GetId(), "prefix");
            index = index + 1;
        }
    }

    internal static void AddMetadataIdOccurrence(System.Collections.Generic.List<RegistryValidationIssue> issues, System.Collections.Generic.List<string> ids, string id, string category)
    {
        if (ContainsOrdinalText(ids, id))
        {
            AddIssue(issues, RegistryValidationIssueKind.DuplicateMetadataId, id, category, "Metadata id is registered more than once in its catalog.");
            return;
        }

        ids.Add(id);
    }

    internal void AddIncompatibleQuantityKindUnits(System.Collections.Generic.List<RegistryValidationIssue> issues, QuantityKindDefinition quantityKind, UnitDefinition[] units, string unitId)
    {
        int index = 0;
        while (index < units.Length)
        {
            UnitDefinition unit = units[index];
            if (!quantityKind.GetDimension().EqualsDimension(unit.GetDimension()))
            {
                AddIssue(issues, RegistryValidationIssueKind.IncompatibleQuantityKindMapping, quantityKind.GetId(), unitId, "Quantity kind unit mapping has an incompatible dimension.");
            }

            index = index + 1;
        }
    }

    internal void AddDuplicateAliasIssues(System.Collections.Generic.List<RegistryValidationIssue> issues)
    {
        System.Collections.Generic.List<string> aliases = new System.Collections.Generic.List<string>();
        int unitIndex = 0;
        while (unitIndex < _units.Count)
        {
            UnitDefinition unit = _units[unitIndex];
            string[] unitAliases = unit.GetAliases();
            int aliasIndex = 0;

            while (aliasIndex < unitAliases.Length)
            {
                AddAliasOccurrence(issues, aliases, unitAliases[aliasIndex], unit.GetId());
                aliasIndex = aliasIndex + 1;
            }

            unitIndex = unitIndex + 1;
        }

        int registeredIndex = 0;
        while (registeredIndex < _aliasTexts.Count)
        {
            AddAliasOccurrence(issues, aliases, _aliasTexts[registeredIndex], _aliasUnitIds[registeredIndex]);
            registeredIndex = registeredIndex + 1;
        }
    }

    internal static void AddAliasOccurrence(System.Collections.Generic.List<RegistryValidationIssue> issues, System.Collections.Generic.List<string> aliases, string alias, string unitId)
    {
        int index = 0;
        while (index < aliases.Count)
        {
            if (string.Equals(aliases[index], alias, System.StringComparison.Ordinal))
            {
                AddIssue(issues, RegistryValidationIssueKind.DuplicateAlias, unitId, alias, "Alias is registered more than once.");
                return;
            }

            index = index + 1;
        }

        aliases.Add(alias);
    }

    internal void AddDuplicateMetadataAliasIssues(System.Collections.Generic.List<RegistryValidationIssue> issues)
    {
        System.Collections.Generic.List<string> aliases = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < _quantityKinds.Count)
        {
            AddMetadataAliases(issues, aliases, _quantityKinds[index].GetAliases(), _quantityKinds[index].GetId());
            index = index + 1;
        }

        aliases = new System.Collections.Generic.List<string>();
        index = 0;
        while (index < _constants.Count)
        {
            AddMetadataAliases(issues, aliases, _constants[index].GetAliases(), _constants[index].GetId());
            index = index + 1;
        }

        aliases = new System.Collections.Generic.List<string>();
        index = 0;
        while (index < _equivalencies.Count)
        {
            AddMetadataAliases(issues, aliases, _equivalencies[index].GetAliases(), _equivalencies[index].GetId());
            index = index + 1;
        }
    }

    internal static void AddMetadataAliases(System.Collections.Generic.List<RegistryValidationIssue> issues, System.Collections.Generic.List<string> aliases, string[] values, string subjectId)
    {
        int index = 0;
        while (index < values.Length)
        {
            string alias = values[index];
            if (ContainsOrdinalText(aliases, alias))
            {
                AddIssue(issues, RegistryValidationIssueKind.DuplicateMetadataAlias, subjectId, alias, "Metadata alias is registered more than once in its catalog.");
            }
            else
            {
                aliases.Add(alias);
            }

            index = index + 1;
        }
    }

    internal void AddEquivalencyIssues(System.Collections.Generic.List<RegistryValidationIssue> issues)
    {
        int index = 0;
        while (index < _equivalencies.Count)
        {
            EquivalencyDefinition equivalency = _equivalencies[index];
            QuantityKindDefinition? sourceKind = FindQuantityKindById(equivalency.GetSourceQuantityKindId());
            QuantityKindDefinition? targetKind = FindQuantityKindById(equivalency.GetTargetQuantityKindId());
            if (sourceKind == null)
            {
                AddIssue(issues, RegistryValidationIssueKind.InvalidEquivalency, equivalency.GetId(), equivalency.GetSourceQuantityKindId(), "Equivalency source quantity kind is not registered.");
            }
            else if (!sourceKind.GetDimension().EqualsDimension(equivalency.GetSourceDimension()))
            {
                AddIssue(issues, RegistryValidationIssueKind.InvalidEquivalency, equivalency.GetId(), equivalency.GetSourceQuantityKindId(), "Equivalency source dimension does not match its quantity kind.");
            }

            if (targetKind == null)
            {
                AddIssue(issues, RegistryValidationIssueKind.InvalidEquivalency, equivalency.GetId(), equivalency.GetTargetQuantityKindId(), "Equivalency target quantity kind is not registered.");
            }
            else if (!targetKind.GetDimension().EqualsDimension(equivalency.GetTargetDimension()))
            {
                AddIssue(issues, RegistryValidationIssueKind.InvalidEquivalency, equivalency.GetId(), equivalency.GetTargetQuantityKindId(), "Equivalency target dimension does not match its quantity kind.");
            }

            if (string.Equals(equivalency.GetSourceQuantityKindId(), equivalency.GetTargetQuantityKindId(), System.StringComparison.Ordinal)
                || equivalency.GetSourceDimension().EqualsDimension(equivalency.GetTargetDimension()))
            {
                AddIssue(issues, RegistryValidationIssueKind.InvalidEquivalency, equivalency.GetId(), "quantityKinds", "Equivalency must connect distinct quantity kinds with different dimensions.");
            }

            if (!HasInverseEquivalencyFormula(equivalency))
            {
                AddIssue(issues, RegistryValidationIssueKind.InvalidEquivalency, equivalency.GetId(), "formula", "Equivalency forward and reverse formulas are not exact inverses.");
            }

            index = index + 1;
        }
    }

    internal static bool HasInverseEquivalencyFormula(EquivalencyDefinition equivalency)
    {
        int forwardPower = equivalency.GetForwardPower();
        int reversePower = equivalency.GetReversePower();
        if (forwardPower == 1 && reversePower == 1)
        {
            return equivalency.GetForwardScale().Multiply(equivalency.GetReverseScale()).EqualsValue(new RationalNumber(1, 1));
        }

        if (forwardPower == -1 && reversePower == -1)
        {
            return equivalency.GetForwardScale().EqualsValue(equivalency.GetReverseScale());
        }

        return false;
    }

    internal void AddSpecialConversionIssues(System.Collections.Generic.List<RegistryValidationIssue> issues)
    {
        int index = 0;
        while (index < _units.Count)
        {
            UnitDefinition unit = _units[index];
            if (unit.GetConversionKind() == UnitConversionKind.Special
                && (!unit.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)) || !unit.GetOffsetToSystemUnit().IsZero() || unit.IsPrefixable()))
            {
                AddIssue(issues, RegistryValidationIssueKind.InvalidSpecialConversionMetadata, unit.GetId(), "conversion", "Special units must use neutral scale and offset metadata and must not be prefixable.");
            }

            if (unit.GetConversionKind() != UnitConversionKind.Unsupported
                && unit.GetConversionKind() != UnitConversionKind.Scale
                && unit.GetConversionKind() != UnitConversionKind.Affine
                && unit.GetConversionKind() != UnitConversionKind.Special)
            {
                AddIssue(issues, RegistryValidationIssueKind.InvalidSpecialConversionMetadata, unit.GetId(), "conversionKind", "Unit conversion kind is not supported.");
            }

            index = index + 1;
        }
    }

    internal void AddPhysicalConstantIssues(System.Collections.Generic.List<RegistryValidationIssue> issues)
    {
        int index = 0;
        while (index < _constants.Count)
        {
            PhysicalConstantDefinition constant = _constants[index];
            AddPhysicalConstantMetadataIssues(issues, constant);
            index = index + 1;
        }
    }

    internal void AddPhysicalConstantMetadataIssues(System.Collections.Generic.List<RegistryValidationIssue> issues, PhysicalConstantDefinition constant)
    {
        if (string.IsNullOrWhiteSpace(constant.GetName())
            || string.IsNullOrWhiteSpace(constant.GetExactnessLabel())
            || string.IsNullOrWhiteSpace(constant.GetSourceLabel())
            || string.IsNullOrWhiteSpace(constant.GetAdjustmentOrEdition()))
        {
            AddIssue(issues, RegistryValidationIssueKind.InvalidPhysicalConstantMetadata, constant.GetId(), "metadata", "Physical constant metadata is incomplete.");
        }

        bool exact = string.Equals(constant.GetExactnessLabel(), "exact", System.StringComparison.Ordinal);
        bool measured = string.Equals(constant.GetExactnessLabel(), "measured", System.StringComparison.Ordinal);
        double uncertainty = constant.GetStandardUncertainty();
        if (!exact && !measured)
        {
            AddIssue(issues, RegistryValidationIssueKind.InvalidPhysicalConstantMetadata, constant.GetId(), "exactnessLabel", "Physical constant exactness label must be exact or measured.");
        }

        if (uncertainty < 0.0d || double.IsNaN(uncertainty) || double.IsInfinity(uncertainty) || (exact && uncertainty != 0.0d))
        {
            AddIssue(issues, RegistryValidationIssueKind.InvalidPhysicalConstantMetadata, constant.GetId(), "standardUncertainty", "Physical constant uncertainty is inconsistent with its exactness metadata.");
        }

        QuantityValue quantity = constant.GetQuantity();
        UnitDefinition quantityUnit = quantity.GetUnit();
        UnitDefinition[] registeredUnits = FindUnitsById(quantityUnit.GetId());
        int unitIndex = 0;
        while (unitIndex < registeredUnits.Length)
        {
            if (quantityUnit.IsEquivalentTo(registeredUnits[unitIndex]))
            {
                return;
            }

            unitIndex = unitIndex + 1;
        }

        UnitParseResult parsed = new UnitParser().Parse(quantityUnit.GetCanonicalExpression(), this);
        if (parsed.IsSuccess() && quantityUnit.IsEquivalentTo(parsed.GetUnit()))
        {
            return;
        }

        if (registeredUnits.Length == 0)
        {
            AddIssue(issues, RegistryValidationIssueKind.InvalidPhysicalConstantMetadata, constant.GetId(), quantityUnit.GetId(), "Physical constant quantity unit cannot be resolved from registered unit metadata.");
        }
        else
        {
            AddIssue(issues, RegistryValidationIssueKind.InvalidPhysicalConstantMetadata, constant.GetId(), quantityUnit.GetId(), "Physical constant quantity unit is incompatible with the registered unit metadata.");
        }
    }

    internal static bool ContainsOrdinalText(System.Collections.Generic.List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], value, System.StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void AddIssue(System.Collections.Generic.List<RegistryValidationIssue> issues, RegistryValidationIssueKind kind, string subjectId, string detail, string message)
    {
        issues.Add(new RegistryValidationIssue(kind, subjectId, detail, message));
    }

    internal void AddMatchesById(System.Collections.Generic.List<UnitDefinition> results, string id)
    {
        int index = 0;
        while (index < _units.Count)
        {
            UnitDefinition unit = _units[index];
            if (string.Equals(unit.GetId(), id, System.StringComparison.Ordinal))
            {
                AddUnique(results, unit);
            }

            index = index + 1;
        }
    }

    internal void AddMatchesByName(System.Collections.Generic.List<UnitDefinition> results, string name)
    {
        int index = 0;
        while (index < _units.Count)
        {
            UnitDefinition unit = _units[index];
            if (string.Equals(unit.GetName(), name, System.StringComparison.Ordinal) || string.Equals(unit.GetPluralName(), name, System.StringComparison.Ordinal))
            {
                AddUnique(results, unit);
            }

            index = index + 1;
        }
    }

    internal void AddMatchesBySymbol(System.Collections.Generic.List<UnitDefinition> results, string symbol)
    {
        int index = 0;
        while (index < _units.Count)
        {
            UnitDefinition unit = _units[index];
            if (string.Equals(unit.GetSymbol(), symbol, System.StringComparison.Ordinal))
            {
                AddUnique(results, unit);
            }

            index = index + 1;
        }
    }

    internal void AddMatchesByAlias(System.Collections.Generic.List<UnitDefinition> results, string alias)
    {
        int unitIndex = 0;
        while (unitIndex < _units.Count)
        {
            UnitDefinition unit = _units[unitIndex];
            string[] aliases = unit.GetAliases();
            int aliasIndex = 0;

            while (aliasIndex < aliases.Length)
            {
                if (string.Equals(aliases[aliasIndex], alias, System.StringComparison.Ordinal))
                {
                    AddUnique(results, unit);
                }

                aliasIndex = aliasIndex + 1;
            }

            unitIndex = unitIndex + 1;
        }

        AddMatchesByRegisteredAlias(results, alias);
    }

    internal bool HasAmbiguousSymbol(UnitDefinition unit)
    {
        UnitDefinition[] matches = FindUnitsBySymbol(unit.GetSymbol());
        int index = 0;

        while (index < matches.Length)
        {
            if (!matches[index].IsEquivalentTo(unit))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal PrefixDefinition? FindPrefixByText(string? value, int field)
    {
        if (value == null)
        {
            return null;
        }

        int index = 0;
        while (index < _prefixes.Count)
        {
            PrefixDefinition prefix = _prefixes[index];
            if (field == 0 && string.Equals(prefix.GetId(), value, System.StringComparison.Ordinal))
            {
                return prefix;
            }

            if (field == 1 && string.Equals(prefix.GetName(), value, System.StringComparison.Ordinal))
            {
                return prefix;
            }

            if (field == 2 && string.Equals(prefix.GetSymbol(), value, System.StringComparison.Ordinal))
            {
                return prefix;
            }

            index = index + 1;
        }

        return null;
    }

    internal void AddMatchesByRegisteredAlias(System.Collections.Generic.List<UnitDefinition> results, string alias)
    {
        int index = 0;
        while (index < _aliasTexts.Count)
        {
            if (string.Equals(_aliasTexts[index], alias, System.StringComparison.Ordinal))
            {
                AddMatchesById(results, _aliasUnitIds[index]);
            }

            index = index + 1;
        }
    }

    internal static void AddUnique(System.Collections.Generic.List<UnitDefinition> results, UnitDefinition unit)
    {
        int index = 0;
        while (index < results.Count)
        {
            if (results[index] == unit)
            {
                return;
            }

            index = index + 1;
        }

        results.Add(unit);
    }

    internal static void AddUniqueQuantityKind(System.Collections.Generic.List<QuantityKindDefinition> results, QuantityKindDefinition value)
    {
        int index = 0;
        while (index < results.Count)
        {
            if (results[index] == value)
            {
                return;
            }

            index = index + 1;
        }

        results.Add(value);
    }

    internal static OperationResult CreateFrozenFailure()
    {
        return OperationResult.CreateFailure(UnitErrorCode.RegistryFrozen, "Registry is frozen.");
    }

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Registry " + fieldName + " must not be empty.");
        }
    }

}
