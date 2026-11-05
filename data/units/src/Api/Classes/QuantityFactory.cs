namespace units;

/// <summary>
/// Constructs dynamic quantities by resolving unit metadata from a read-only catalog.
/// </summary>
public class QuantityFactory : IQuantityFactory
{
    internal IUnitParser _unitParser;

    /// <summary>
    /// Initializes a quantity factory with the unit-expression parser to use.
    /// </summary>
    /// <param name="unitParser">The parser used to resolve unit expressions.</param>
    public QuantityFactory(IUnitParser unitParser)
    {
        if (unitParser == null)
        {
            throw new System.ArgumentNullException("unitParser");
        }

        _unitParser = unitParser;
    }

    /// <summary>
    /// Constructs a quantity by stable unit identifier without throwing for expected resolution failures.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unitId">The stable unit identifier.</param>
    /// <param name="catalog">The catalog that supplies definitions and metadata.</param>
    /// <param name="quantityKind">The optional quantity kind used for validation and ambiguity narrowing.</param>
    /// <returns>A result containing the constructed quantity or structured failure details.</returns>
    public QuantityConstructionResult CreateFromUnitId(double value, string unitId, IUnitCatalog catalog, QuantityKindDefinition? quantityKind)
    {
        QuantityConstructionResult? invalid = ValidateResultInputs(unitId, catalog, "Unit identifier");
        if (invalid != null)
        {
            return invalid;
        }

        return CreateFromLookup(value, catalog.FindUnitById(unitId), quantityKind, catalog);
    }

    /// <summary>
    /// Constructs a quantity by unit symbol without throwing for expected resolution failures.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unitSymbol">The unit symbol to resolve.</param>
    /// <param name="catalog">The catalog that supplies definitions and metadata.</param>
    /// <param name="quantityKind">The optional quantity kind used for validation and ambiguity narrowing.</param>
    /// <returns>A result containing the constructed quantity or structured failure details.</returns>
    public QuantityConstructionResult CreateFromUnitSymbol(double value, string unitSymbol, IUnitCatalog catalog, QuantityKindDefinition? quantityKind)
    {
        QuantityConstructionResult? invalid = ValidateResultInputs(unitSymbol, catalog, "Unit symbol");
        if (invalid != null)
        {
            return invalid;
        }

        UnitLookupResult lookup = catalog.FindUnit(unitSymbol);
        if (lookup.IsAmbiguous() && quantityKind != null)
        {
            lookup = UnitParser.ApplyContext(lookup, new UnitParseOptions(false, quantityKind, null), unitSymbol, catalog);
        }

        return CreateFromLookup(value, lookup, quantityKind, catalog);
    }

    /// <summary>
    /// Constructs a quantity by unit expression without throwing for expected resolution failures.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unitExpression">The unit expression to parse.</param>
    /// <param name="catalog">The catalog that supplies definitions and metadata.</param>
    /// <param name="quantityKind">The optional quantity kind used for validation and ambiguity narrowing.</param>
    /// <returns>A result containing the constructed quantity or structured failure details.</returns>
    public QuantityConstructionResult CreateFromUnitExpression(double value, string unitExpression, IUnitCatalog catalog, QuantityKindDefinition? quantityKind)
    {
        QuantityConstructionResult? invalid = ValidateResultInputs(unitExpression, catalog, "Unit expression");
        if (invalid != null)
        {
            return invalid;
        }

        UnitParseOptions options = new UnitParseOptions(false, quantityKind, null);
        UnitParseResult parseResult = _unitParser.ParseWithOptions(unitExpression, catalog, options);
        if (parseResult.IsFailure())
        {
            return QuantityConstructionResult.CreateFailure(parseResult.GetErrorCode(), parseResult.GetMessage(), parseResult.GetCandidates());
        }

        return CreateFromUnit(value, parseResult.GetUnit(), quantityKind, catalog);
    }

    /// <summary>
    /// Constructs a quantity by stable unit identifier and throws when resolution or validation fails.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unitId">The stable unit identifier.</param>
    /// <param name="catalog">The catalog that supplies definitions and metadata.</param>
    /// <param name="quantityKind">The optional quantity kind used for validation and ambiguity narrowing.</param>
    /// <returns>The constructed quantity value.</returns>
    /// <exception cref="System.ArgumentException">The identifier is empty.</exception>
    /// <exception cref="System.InvalidOperationException">The unit cannot be resolved or is incompatible with the quantity kind.</exception>
    public QuantityValue CreateFromUnitIdOrThrow(double value, string unitId, IUnitCatalog catalog, QuantityKindDefinition? quantityKind)
    {
        ValidateThrowingInputs(unitId, catalog, "unitId");
        return GetQuantityOrThrow(CreateFromUnitId(value, unitId, catalog, quantityKind));
    }

    /// <summary>
    /// Constructs a quantity by unit symbol and throws when resolution or validation fails.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unitSymbol">The unit symbol to resolve.</param>
    /// <param name="catalog">The catalog that supplies definitions and metadata.</param>
    /// <param name="quantityKind">The optional quantity kind used for validation and ambiguity narrowing.</param>
    /// <returns>The constructed quantity value.</returns>
    /// <exception cref="System.ArgumentException">The symbol is empty.</exception>
    /// <exception cref="System.InvalidOperationException">The unit cannot be resolved or is incompatible with the quantity kind.</exception>
    public QuantityValue CreateFromUnitSymbolOrThrow(double value, string unitSymbol, IUnitCatalog catalog, QuantityKindDefinition? quantityKind)
    {
        ValidateThrowingInputs(unitSymbol, catalog, "unitSymbol");
        return GetQuantityOrThrow(CreateFromUnitSymbol(value, unitSymbol, catalog, quantityKind));
    }

    /// <summary>
    /// Constructs a quantity by unit expression and throws when parsing or validation fails.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unitExpression">The unit expression to parse.</param>
    /// <param name="catalog">The catalog that supplies definitions and metadata.</param>
    /// <param name="quantityKind">The optional quantity kind used for validation and ambiguity narrowing.</param>
    /// <returns>The constructed quantity value.</returns>
    /// <exception cref="System.ArgumentException">The expression is empty or malformed.</exception>
    /// <exception cref="System.InvalidOperationException">The unit cannot be resolved or is incompatible with the quantity kind.</exception>
    public QuantityValue CreateFromUnitExpressionOrThrow(double value, string unitExpression, IUnitCatalog catalog, QuantityKindDefinition? quantityKind)
    {
        ValidateThrowingInputs(unitExpression, catalog, "unitExpression");
        return GetQuantityOrThrow(CreateFromUnitExpression(value, unitExpression, catalog, quantityKind));
    }

    internal static QuantityConstructionResult? ValidateResultInputs(string text, IUnitCatalog catalog, string description)
    {
        if (text == null)
        {
            return QuantityConstructionResult.CreateFailure(UnitErrorCode.ParseInvalidInput, description + " must not be null.", new UnitDefinition[0]);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return QuantityConstructionResult.CreateFailure(UnitErrorCode.ParseInvalidInput, description + " must not be empty.", new UnitDefinition[0]);
        }

        if (catalog == null)
        {
            return QuantityConstructionResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Unit catalog must not be null.", new UnitDefinition[0]);
        }

        return null;
    }

    internal static QuantityConstructionResult CreateFromLookup(double value, UnitLookupResult lookup, QuantityKindDefinition? quantityKind, IUnitCatalog catalog)
    {
        if (lookup.IsFailure())
        {
            return QuantityConstructionResult.CreateFailure(lookup.GetErrorCode(), lookup.GetMessage(), lookup.GetUnits());
        }

        return CreateFromUnit(value, lookup.GetUnit(), quantityKind, catalog);
    }

    internal static QuantityConstructionResult CreateFromUnit(double value, UnitDefinition unit, QuantityKindDefinition? quantityKind, IUnitCatalog catalog)
    {
        if (quantityKind != null && !IsAcceptedByQuantityKind(unit, quantityKind, catalog))
        {
            return QuantityConstructionResult.CreateFailure(UnitErrorCode.ConversionIncompatibleDimensions, "Resolved unit is not compatible with quantity kind: " + quantityKind.GetId(), new UnitDefinition[0]);
        }

        return QuantityConstructionResult.CreateSuccess(new QuantityValue(value, unit));
    }

    internal static bool IsAcceptedByQuantityKind(UnitDefinition unit, QuantityKindDefinition quantityKind, IUnitCatalog catalog)
    {
        if (quantityKind.AcceptsUnit(unit))
        {
            return true;
        }

        if (unit.GetConversionKind() != UnitConversionKind.Scale || !quantityKind.GetDimension().EqualsDimension(unit.GetDimension()))
        {
            return false;
        }

        UnitLookupResult defaultUnitResult = catalog.FindUnitById(quantityKind.GetDefaultUnitId());
        if (defaultUnitResult.IsFailure())
        {
            return false;
        }

        return defaultUnitResult.GetUnit().GetConversionKind() == UnitConversionKind.Scale;
    }

    internal static void ValidateThrowingInputs(string text, IUnitCatalog catalog, string fieldName)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new System.ArgumentException("Unit text must not be empty.");
        }

        if (catalog == null)
        {
            throw new System.ArgumentNullException("catalog");
        }
    }

    internal static QuantityValue GetQuantityOrThrow(QuantityConstructionResult result)
    {
        if (result.IsSuccess())
        {
            return result.GetQuantity();
        }

        if (result.GetErrorCode() == UnitErrorCode.ParseInvalidInput)
        {
            throw new System.ArgumentException(result.GetMessage());
        }

        throw new System.InvalidOperationException(result.GetMessage());
    }
}
