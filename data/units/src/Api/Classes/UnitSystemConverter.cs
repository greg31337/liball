namespace units;

/// <summary>
/// Converts quantities to preferred units, base-unit expressions, and equivalent named units in a selected unit system.
/// </summary>
public class UnitSystemConverter : IUnitSystemConverter
{
    /// <summary>
    /// Initializes a converter that selects preferred units from unit-system metadata.
    /// </summary>
    public UnitSystemConverter()
    {
    }

    /// <summary>
    /// Converts the value to the unit preferred by the selected system for the quantity kind.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <param name="system">The target unit-system definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertToPreferredUnit(double value, UnitDefinition fromUnit, QuantityKindDefinition quantityKind, UnitSystemDefinition system, IUnitCatalog registry)
    {
        ValidateRequiredValue(fromUnit, "fromUnit");
        ValidateRequiredValue(quantityKind, "quantityKind");
        ValidateRequiredValue(system, "system");
        ValidateRequiredValue(registry, "registry");

        if (!quantityKind.GetDimension().EqualsDimension(fromUnit.GetDimension()))
        {
            return ConversionResult.CreateFailure(UnitErrorCode.ConversionIncompatibleDimensions, "Source unit is incompatible with the quantity kind.");
        }

        string? preferredUnitId = system.FindPreferredUnitId(quantityKind.GetId());
        if (preferredUnitId == null)
        {
            return ConversionResult.CreateFailure(UnitErrorCode.LookupNotFound, "System has no preferred unit for quantity kind: " + quantityKind.GetId());
        }

        UnitLookupResult lookup = registry.FindUnitById(preferredUnitId);
        if (lookup.IsFailure())
        {
            return ConversionResult.CreateFailure(lookup.GetErrorCode(), "Preferred unit was not found: " + preferredUnitId);
        }

        UnitConverter converter = new UnitConverter();
        return converter.Convert(value, fromUnit, lookup.GetUnit());
    }

    /// <summary>
    /// Converts a quantity to the unit preferred by the selected system for the quantity kind.
    /// </summary>
    /// <param name="quantity">The quantity to convert.</param>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <param name="system">The target unit-system definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertQuantityToPreferredUnit(QuantityValue quantity, QuantityKindDefinition quantityKind, UnitSystemDefinition system, IUnitCatalog registry)
    {
        ValidateRequiredValue(quantity, "quantity");
        return ConvertToPreferredUnit(quantity.GetValue(), quantity.GetUnit(), quantityKind, system, registry);
    }

    /// <summary>
    /// Converts a quantity to a compound expression built from the selected system's registered base units.
    /// </summary>
    /// <param name="quantity">The quantity to decompose.</param>
    /// <param name="system">The target unit-system definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the decomposed quantity or structured failure details.</returns>
    public ConversionResult DecomposeToBaseUnits(QuantityValue quantity, UnitSystemDefinition system, IUnitCatalog registry)
    {
        ValidateRequiredValue(quantity, "quantity");
        ValidateRequiredValue(system, "system");
        ValidateRequiredValue(registry, "registry");

        ConversionResult expressionResult = CreateBaseExpression(quantity.GetUnit(), system, registry);
        if (expressionResult.IsFailure())
        {
            return expressionResult;
        }

        return new UnitConverter().Convert(quantity.GetValue(), quantity.GetUnit(), expressionResult.GetUnit());
    }

    /// <summary>
    /// Converts an equivalent expression to one registered named derived unit preferred by the selected system.
    /// </summary>
    /// <param name="quantity">The quantity expression to compose.</param>
    /// <param name="system">The target unit-system definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the named quantity or structured failure details.</returns>
    public ConversionResult ComposeToNamedUnit(QuantityValue quantity, UnitSystemDefinition system, IUnitCatalog registry)
    {
        ValidateRequiredValue(quantity, "quantity");
        ValidateRequiredValue(system, "system");
        ValidateRequiredValue(registry, "registry");

        System.Collections.Generic.List<UnitDefinition> matches = new System.Collections.Generic.List<UnitDefinition>();
        string[] preferredUnitIds = system.GetPreferredUnitIds();
        int index = 0;
        while (index < preferredUnitIds.Length)
        {
            string candidateId = preferredUnitIds[index];
            if (IsNamedDerivedId(candidateId, system))
            {
                UnitLookupResult lookup = registry.FindUnitById(candidateId);
                if (lookup.IsAmbiguous())
                {
                    return ConversionResult.CreateFailure(UnitErrorCode.LookupAmbiguous, "Named derived unit lookup was ambiguous: " + candidateId);
                }

                if (lookup.IsSuccess())
                {
                    UnitDefinition candidate = lookup.GetUnit();
                    if (quantity.GetUnit().IsEquivalentTo(candidate))
                    {
                        AddUniqueMatch(matches, candidate);
                    }
                }
            }

            index = index + 1;
        }

        if (matches.Count == 0)
        {
            return ConversionResult.CreateFailure(UnitErrorCode.LookupNotFound, "No equivalent registered named derived unit was found for system: " + system.GetId());
        }

        if (matches.Count > 1)
        {
            return ConversionResult.CreateFailure(UnitErrorCode.LookupAmbiguous, "More than one equivalent registered named derived unit was found for system: " + system.GetId());
        }

        return new UnitConverter().Convert(quantity.GetValue(), quantity.GetUnit(), matches[0]);
    }

    internal static ConversionResult CreateBaseExpression(UnitDefinition sourceUnit, UnitSystemDefinition system, IUnitCatalog registry)
    {
        DimensionVector dimension = sourceUnit.GetDimension();
        if (dimension.IsDimensionless())
        {
            UnitLookupResult dimensionlessLookup = registry.FindUnitById("one");
            if (dimensionlessLookup.IsFailure())
            {
                return ConversionResult.CreateFailure(dimensionlessLookup.GetErrorCode(), "Dimensionless identity unit was not found.");
            }

            return ConversionResult.CreateSuccess(0.0d, dimensionlessLookup.GetUnit());
        }

        UnitDefinition?[] baseUnits = new UnitDefinition?[DimensionVector.GetSlotCount()];
        string[] baseUnitIds = system.GetBaseUnitIds();
        int baseIndex = 0;
        while (baseIndex < baseUnitIds.Length)
        {
            UnitLookupResult lookup = registry.FindUnitById(baseUnitIds[baseIndex]);
            if (lookup.IsAmbiguous())
            {
                return ConversionResult.CreateFailure(UnitErrorCode.LookupAmbiguous, "System base unit lookup was ambiguous: " + baseUnitIds[baseIndex]);
            }

            if (lookup.IsSuccess())
            {
                UnitDefinition baseUnit = lookup.GetUnit();
                int slot = FindSingleBaseSlot(baseUnit.GetDimension());
                if (slot < 0)
                {
                    return ConversionResult.CreateFailure(UnitErrorCode.RegistryInvalidMetadata, "System base unit must represent exactly one base dimension: " + baseUnit.GetId());
                }

                if (baseUnits[slot] != null)
                {
                    return ConversionResult.CreateFailure(UnitErrorCode.RegistryInvalidMetadata, "System defines more than one unit for a base dimension.");
                }

                if (!baseUnit.CanUseInCompound() && !(IsAffineUnit(sourceUnit) && dimension.EqualsDimension(baseUnit.GetDimension())))
                {
                    ConversionResult alternative = FindScaleBaseAlternative(baseUnit, system, registry);
                    if (alternative.IsFailure())
                    {
                        return alternative;
                    }

                    baseUnit = alternative.GetUnit();
                }

                baseUnits[slot] = baseUnit;
            }

            baseIndex = baseIndex + 1;
        }

        UnitDefinition? numerator = null;
        UnitDefinition? denominator = null;
        int dimensionSlot = 0;
        while (dimensionSlot < DimensionVector.GetSlotCount())
        {
            int exponent = dimension.GetExponent(dimensionSlot);
            if (exponent != 0)
            {
                UnitDefinition? baseUnit = baseUnits[dimensionSlot];
                if (baseUnit == null)
                {
                    return ConversionResult.CreateFailure(UnitErrorCode.LookupNotFound, "System base unit is not resolvable for dimension slot: " + FormatInt(dimensionSlot));
                }

                if (!baseUnit.CanUseInCompound())
                {
                    if (exponent == 1 && dimension.EqualsDimension(baseUnit.GetDimension()))
                    {
                        return ConversionResult.CreateSuccess(0.0d, baseUnit);
                    }

                    return ConversionResult.CreateFailure(UnitErrorCode.ConversionUnsupported, "Affine or special base units cannot be used in a compound expression.");
                }

                if (exponent == int.MinValue)
                {
                    return ConversionResult.CreateFailure(UnitErrorCode.ConversionUnsupported, "Dimension exponent is too small to decompose.");
                }

                int magnitude = exponent;
                if (magnitude < 0)
                {
                    magnitude = 0 - magnitude;
                }

                UnitDefinition factor = baseUnit;
                if (magnitude != 1)
                {
                    factor = baseUnit.Pow(magnitude);
                }

                if (exponent > 0)
                {
                    numerator = AddExpressionFactor(numerator, factor);
                }
                else
                {
                    denominator = AddExpressionFactor(denominator, factor);
                }
            }

            dimensionSlot = dimensionSlot + 1;
        }

        UnitDefinition expression;
        if (numerator != null && denominator != null)
        {
            expression = numerator.Divide(denominator);
        }
        else if (numerator != null)
        {
            expression = numerator;
        }
        else if (denominator != null)
        {
            expression = denominator.Inverse();
        }
        else
        {
            return ConversionResult.CreateFailure(UnitErrorCode.ConversionUnsupported, "Base-unit expression could not be created.");
        }

        return ConversionResult.CreateSuccess(0.0d, expression);
    }

    internal static int FindSingleBaseSlot(DimensionVector dimension)
    {
        int match = -1;
        int slot = 0;
        while (slot < DimensionVector.GetSlotCount())
        {
            int exponent = dimension.GetExponent(slot);
            if (exponent != 0)
            {
                if (exponent != 1 || match >= 0)
                {
                    return -1;
                }

                match = slot;
            }

            slot = slot + 1;
        }

        return match;
    }

    internal static ConversionResult FindScaleBaseAlternative(UnitDefinition baseUnit, UnitSystemDefinition system, IUnitCatalog registry)
    {
        System.Collections.Generic.List<UnitDefinition> matches = new System.Collections.Generic.List<UnitDefinition>();
        string[] preferredUnitIds = system.GetPreferredUnitIds();
        int index = 0;
        while (index < preferredUnitIds.Length)
        {
            UnitLookupResult lookup = registry.FindUnitById(preferredUnitIds[index]);
            if (lookup.IsAmbiguous())
            {
                return ConversionResult.CreateFailure(UnitErrorCode.LookupAmbiguous, "Scale base alternative lookup was ambiguous: " + preferredUnitIds[index]);
            }

            if (lookup.IsSuccess())
            {
                UnitDefinition candidate = lookup.GetUnit();
                if (candidate.CanUseInCompound()
                    && candidate.GetDimension().EqualsDimension(baseUnit.GetDimension())
                    && candidate.GetScaleToSystemUnit().EqualsValue(baseUnit.GetScaleToSystemUnit()))
                {
                    AddUniqueMatch(matches, candidate);
                }
            }

            index = index + 1;
        }

        if (matches.Count == 1)
        {
            return ConversionResult.CreateSuccess(0.0d, matches[0]);
        }

        if (matches.Count > 1)
        {
            return ConversionResult.CreateFailure(UnitErrorCode.LookupAmbiguous, "More than one scale base alternative was found.");
        }

        return ConversionResult.CreateFailure(UnitErrorCode.ConversionUnsupported, "Affine system base unit has no resolvable scale interval alternative.");
    }

    internal static UnitDefinition AddExpressionFactor(UnitDefinition? expression, UnitDefinition factor)
    {
        if (expression == null)
        {
            return factor;
        }

        return expression.Multiply(factor);
    }

    internal static bool IsNamedDerivedId(string unitId, UnitSystemDefinition system)
    {
        return !system.UsesBaseUnitId(unitId) && !unitId.StartsWith("(", System.StringComparison.Ordinal);
    }

    internal static bool IsAffineUnit(UnitDefinition unit)
    {
        return unit.GetConversionKind() == UnitConversionKind.Affine || !unit.GetOffsetToSystemUnit().IsZero();
    }

    internal static void AddUniqueMatch(System.Collections.Generic.List<UnitDefinition> matches, UnitDefinition candidate)
    {
        int index = 0;
        while (index < matches.Count)
        {
            if (string.Equals(matches[index].GetId(), candidate.GetId(), System.StringComparison.Ordinal))
            {
                return;
            }

            index = index + 1;
        }

        matches.Add(candidate);
    }

    internal static string FormatInt(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void ValidateRequiredValue(object value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }
}
