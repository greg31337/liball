namespace units;

/// <summary>
/// Provides public fields suitable for serializing a quantity without exposing runtime internals.
/// </summary>
/// <remarks>
/// Unit contains a canonical unit expression. QuantityKind is empty when no kind was supplied.
/// </remarks>
public class QuantityDto
{
    /// <summary>
    /// Gets or sets the quantity's numeric value for serialization.
    /// </summary>
    public double Value;
    /// <summary>
    /// Gets or sets the canonical unit expression for serialization.
    /// </summary>
    public string Unit;
    /// <summary>
    /// Gets or sets the optional quantity-kind identifier for serialization.
    /// </summary>
    public string QuantityKind;

    /// <summary>
    /// Initializes an empty quantity dto with default values.
    /// </summary>
    public QuantityDto()
    {
        Value = 0.0d;
        Unit = "";
        QuantityKind = "";
    }

    internal static QuantityDto Create(double value, string unit, string? quantityKind)
    {
        ValidateRequiredText(unit, "unit");

        QuantityDto result = new QuantityDto();
        result.Value = value;
        result.Unit = unit;
        if (quantityKind == null)
        {
            result.QuantityKind = "";
        }
        else
        {
            result.QuantityKind = quantityKind;
        }

        return result;
    }

    internal static QuantityDto FromQuantity(QuantityValue quantity)
    {
        return FromQuantity(quantity, "");
    }

    internal static QuantityDto FromQuantity(QuantityValue quantity, string? quantityKind)
    {
        ValidateRequiredValue(quantity, "quantity");
        string unitText = new UnitFormatter().FormatWithOptions(quantity.GetUnit(), UnitFormatOptions.CreateCanonical());
        return Create(quantity.GetValue(), unitText, quantityKind);
    }

    internal double GetValue()
    {
        return Value;
    }

    internal string GetUnit()
    {
        return Unit;
    }

    internal string GetQuantityKind()
    {
        return QuantityKind;
    }

    internal bool HasQuantityKind()
    {
        return !string.IsNullOrWhiteSpace(QuantityKind);
    }

    internal QuantityValue ToQuantity(IUnitCatalog registry)
    {
        ValidateRequiredValue(registry, "registry");
        ValidateRequiredText(Unit, "unit");

        UnitParser parser = new UnitParser();
        UnitDefinition unit = parser.ParseOrThrow(Unit, registry);
        return new QuantityValue(Value, unit);
    }

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Quantity DTO " + fieldName + " must not be empty.");
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
