namespace units;

/// <summary>
/// Converts quantities to and from canonical text and data-transfer objects.
/// </summary>
public class QuantitySerializer : IQuantitySerializer
{
    internal QuantityFormatter _formatter;
    internal QuantityParser _parser;

    /// <summary>
    /// Initializes an empty quantity serializer with default values.
    /// </summary>
    public QuantitySerializer()
    {
        _formatter = new QuantityFormatter();
        _parser = QuantityParser.CreateDefault();
    }

    /// <summary>
    /// Serializes the quantity as culture-invariant canonical text.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <returns>The canonical serialized text.</returns>
    public string SerializeCanonical(QuantityValue quantity)
    {
        ValidateRequiredValue(quantity, "quantity");
        return _formatter.FormatWithOptions(quantity, CreateCanonicalFormatOptions());
    }

    /// <summary>
    /// Parses culture-invariant canonical text into a quantity.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue DeserializeCanonical(string text, IUnitCatalog registry)
    {
        ValidateRequiredText(text, "text");
        ValidateRequiredValue(registry, "registry");
        return _parser.ParseOrThrowWithOptions(text, registry, CreateCanonicalParseOptions());
    }

    /// <summary>
    /// Parses culture-invariant canonical text without throwing for expected deserialization failures.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the quantity or structured failure details.</returns>
    public QuantityParseResult DeserializeCanonicalResult(string text, IUnitCatalog registry)
    {
        return _parser.ParseWithOptions(text, registry, CreateCanonicalParseOptions());
    }

    /// <summary>
    /// Creates a data-transfer object from the supplied quantity.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <returns>The resulting quantity dto.</returns>
    public QuantityDto ToDto(QuantityValue quantity)
    {
        return QuantityDto.FromQuantity(quantity);
    }

    /// <summary>
    /// Creates a data-transfer object from the supplied quantity.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <returns>The resulting quantity dto.</returns>
    public QuantityDto ToDtoWithQuantityKind(QuantityValue quantity, string? quantityKind)
    {
        return QuantityDto.FromQuantity(quantity, quantityKind);
    }

    /// <summary>
    /// Creates a quantity from the supplied data-transfer object.
    /// </summary>
    /// <param name="dto">The data-transfer object to read.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue FromDto(QuantityDto dto, IUnitCatalog registry)
    {
        ValidateRequiredValue(dto, "dto");
        ValidateRequiredValue(registry, "registry");
        return QuantityFactory.GetQuantityOrThrow(FromDtoResult(dto, registry));
    }

    /// <summary>
    /// Creates a quantity from a data-transfer object without throwing for expected deserialization failures.
    /// </summary>
    /// <param name="dto">The data-transfer object to read.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the quantity or structured failure details.</returns>
    public QuantityConstructionResult FromDtoResult(QuantityDto dto, IUnitCatalog registry)
    {
        if (dto == null)
        {
            return QuantityConstructionResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Quantity DTO must not be null.", new UnitDefinition[0]);
        }

        if (registry == null)
        {
            return QuantityConstructionResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Unit catalog must not be null.", new UnitDefinition[0]);
        }

        if (string.IsNullOrWhiteSpace(dto.GetUnit()))
        {
            return QuantityConstructionResult.CreateFailure(UnitErrorCode.ParseInvalidInput, "Quantity DTO unit must not be empty.", new UnitDefinition[0]);
        }

        UnitParseResult unitResult = new UnitParser().ParseWithOptions(dto.GetUnit(), registry, new UnitParseOptions(true, null, null));
        if (unitResult.IsFailure())
        {
            return QuantityConstructionResult.CreateFailure(unitResult.GetErrorCode(), unitResult.GetMessage(), unitResult.GetCandidates());
        }

        QuantityValue quantity = new QuantityValue(dto.GetValue(), unitResult.GetUnit());
        if (!dto.HasQuantityKind())
        {
            return QuantityConstructionResult.CreateSuccess(quantity);
        }

        QuantityKindLookupResult kindResult = registry.FindQuantityKind(dto.GetQuantityKind());
        if (kindResult.IsFailure())
        {
            return QuantityConstructionResult.CreateFailure(kindResult.GetErrorCode(), kindResult.GetMessage(), new UnitDefinition[0]);
        }

        QuantityKindDefinition quantityKind = kindResult.GetQuantityKind();
        if (!IsAcceptedDtoUnit(quantity.GetUnit(), quantityKind, registry))
        {
            return QuantityConstructionResult.CreateFailure(UnitErrorCode.ConversionIncompatibleDimensions, "DTO unit is not accepted for quantity kind: " + quantityKind.GetId(), new UnitDefinition[0]);
        }

        return QuantityConstructionResult.CreateSuccess(quantity);
    }

    internal static bool IsAcceptedDtoUnit(UnitDefinition unit, QuantityKindDefinition quantityKind, IUnitCatalog registry)
    {
        return QuantityFactory.IsAcceptedByQuantityKind(unit, quantityKind, registry);
    }

    internal static QuantityFormatOptions CreateCanonicalFormatOptions()
    {
        return new QuantityFormatOptions(
            "R",
            System.Globalization.CultureInfo.InvariantCulture,
            UnitFormatOptions.CreateCanonical());
    }

    internal static QuantityParseOptions CreateCanonicalParseOptions()
    {
        return QuantityParseOptions.CreateDefault()
            .WithStrict(true)
            .WithCulture(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void ValidateQuantityKind(QuantityDto dto, QuantityValue quantity, IUnitCatalog registry)
    {
        if (!dto.HasQuantityKind())
        {
            return;
        }

        QuantityKindLookupResult result = registry.FindQuantityKind(dto.GetQuantityKind());
        if (result.IsFailure())
        {
            throw new System.InvalidOperationException(result.GetMessage());
        }

        QuantityKindDefinition quantityKind = (QuantityKindDefinition)result.GetQuantityKind();
        if (quantityKind.AcceptsUnit(quantity.GetUnit()))
        {
            return;
        }

        if (quantity.GetUnit().GetConversionKind() == UnitConversionKind.Scale && quantityKind.GetDimension().EqualsDimension(quantity.GetUnit().GetDimension()))
        {
            return;
        }

        throw new System.InvalidOperationException("DTO unit is not accepted for quantity kind: " + quantityKind.GetId());
    }

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Quantity serializer " + fieldName + " must not be empty.");
        }
    }

    internal static void ValidateRequiredValue(object value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }
}
