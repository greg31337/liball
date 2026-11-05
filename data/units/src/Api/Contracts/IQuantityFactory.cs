namespace units;

/// <summary>
/// Constructs dynamic quantities by resolving unit metadata from a read-only catalog.
/// </summary>
public interface IQuantityFactory
{
    /// <summary>
    /// Constructs a quantity by stable unit identifier without throwing for expected resolution failures.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unitId">The stable unit identifier.</param>
    /// <param name="catalog">The catalog that supplies definitions and metadata.</param>
    /// <param name="quantityKind">The optional quantity kind used for validation and ambiguity narrowing.</param>
    /// <returns>A result containing the constructed quantity or structured failure details.</returns>
    public QuantityConstructionResult CreateFromUnitId(double value, string unitId, IUnitCatalog catalog, QuantityKindDefinition? quantityKind);
    /// <summary>
    /// Constructs a quantity by unit symbol without throwing for expected resolution failures.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unitSymbol">The unit symbol to resolve.</param>
    /// <param name="catalog">The catalog that supplies definitions and metadata.</param>
    /// <param name="quantityKind">The optional quantity kind used for validation and ambiguity narrowing.</param>
    /// <returns>A result containing the constructed quantity or structured failure details.</returns>
    public QuantityConstructionResult CreateFromUnitSymbol(double value, string unitSymbol, IUnitCatalog catalog, QuantityKindDefinition? quantityKind);
    /// <summary>
    /// Constructs a quantity by unit expression without throwing for expected resolution failures.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unitExpression">The unit expression to parse.</param>
    /// <param name="catalog">The catalog that supplies definitions and metadata.</param>
    /// <param name="quantityKind">The optional quantity kind used for validation and ambiguity narrowing.</param>
    /// <returns>A result containing the constructed quantity or structured failure details.</returns>
    public QuantityConstructionResult CreateFromUnitExpression(double value, string unitExpression, IUnitCatalog catalog, QuantityKindDefinition? quantityKind);
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
    public QuantityValue CreateFromUnitIdOrThrow(double value, string unitId, IUnitCatalog catalog, QuantityKindDefinition? quantityKind);
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
    public QuantityValue CreateFromUnitSymbolOrThrow(double value, string unitSymbol, IUnitCatalog catalog, QuantityKindDefinition? quantityKind);
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
    public QuantityValue CreateFromUnitExpressionOrThrow(double value, string unitExpression, IUnitCatalog catalog, QuantityKindDefinition? quantityKind);
}
