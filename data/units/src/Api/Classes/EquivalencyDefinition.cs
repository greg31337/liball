namespace units;

/// <summary>
/// Describes an explicit formula between otherwise incompatible quantity dimensions.
/// </summary>
/// <remarks>
/// Equivalencies are opt-in and are never used by ordinary same-dimension conversion.
/// </remarks>
public class EquivalencyDefinition : IEquivalencyDefinition
{
    internal string _id;
    internal string _name;
    internal string _description;
    internal string _sourceQuantityKindId;
    internal DimensionVector _sourceDimension;
    internal string _targetQuantityKindId;
    internal DimensionVector _targetDimension;
    internal RationalNumber _forwardScale;
    internal int _forwardPower;
    internal RationalNumber _reverseScale;
    internal int _reversePower;
    internal string[] _aliases;

    /// <summary>
    /// Initializes a new equivalency definition from the supplied metadata.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="description">The plain-English description.</param>
    /// <param name="sourceQuantityKindId">The source quantity kind id to use.</param>
    /// <param name="sourceDimension">The source dimension to use.</param>
    /// <param name="targetQuantityKindId">The target quantity kind id to use.</param>
    /// <param name="targetDimension">The target dimension to use.</param>
    /// <param name="forwardScale">The forward scale to use.</param>
    /// <param name="forwardPower">The forward power to use.</param>
    /// <param name="reverseScale">The reverse scale to use.</param>
    /// <param name="reversePower">The reverse power to use.</param>
    /// <param name="aliases">The alternate lookup texts.</param>
    public EquivalencyDefinition(string id, string name, string? description, string sourceQuantityKindId, DimensionVector sourceDimension, string targetQuantityKindId, DimensionVector targetDimension, RationalNumber forwardScale, int forwardPower, RationalNumber reverseScale, int reversePower, string[]? aliases)
    {
        ValidateRequiredText(id, "id");
        ValidateRequiredText(name, "name");
        ValidateRequiredText(sourceQuantityKindId, "sourceQuantityKindId");
        ValidateRequiredValue(sourceDimension, "sourceDimension");
        ValidateRequiredText(targetQuantityKindId, "targetQuantityKindId");
        ValidateRequiredValue(targetDimension, "targetDimension");
        ValidateScale(forwardScale, "forwardScale");
        ValidatePower(forwardPower, "forwardPower");
        ValidateScale(reverseScale, "reverseScale");
        ValidatePower(reversePower, "reversePower");

        _id = id;
        _name = name;
        if (description == null)
        {
            _description = "";
        }
        else
        {
            _description = description;
        }

        _sourceQuantityKindId = sourceQuantityKindId;
        _sourceDimension = sourceDimension;
        _targetQuantityKindId = targetQuantityKindId;
        _targetDimension = targetDimension;
        _forwardScale = forwardScale;
        _forwardPower = forwardPower;
        _reverseScale = reverseScale;
        _reversePower = reversePower;
        _aliases = CopyTexts(aliases, "alias");
    }

    /// <summary>
    /// Gets the stable identifier.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetId()
    {
        return _id;
    }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetName()
    {
        return _name;
    }

    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetDescription()
    {
        return _description;
    }

    /// <summary>
    /// Gets the source quantity kind id.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSourceQuantityKindId()
    {
        return _sourceQuantityKindId;
    }

    /// <summary>
    /// Gets the source dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector GetSourceDimension()
    {
        return _sourceDimension;
    }

    /// <summary>
    /// Gets the target quantity kind id.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetTargetQuantityKindId()
    {
        return _targetQuantityKindId;
    }

    /// <summary>
    /// Gets the target dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector GetTargetDimension()
    {
        return _targetDimension;
    }

    /// <summary>
    /// Gets the forward scale.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetForwardScale()
    {
        return _forwardScale;
    }

    /// <summary>
    /// Gets the forward power.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetForwardPower()
    {
        return _forwardPower;
    }

    /// <summary>
    /// Gets the reverse scale.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetReverseScale()
    {
        return _reverseScale;
    }

    /// <summary>
    /// Gets the reverse power.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetReversePower()
    {
        return _reversePower;
    }

    /// <summary>
    /// Returns a copy of the aliases.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetAliases()
    {
        return CopyTexts(_aliases, "alias");
    }

    /// <summary>
    /// Reports whether the supplied text matches the definition's identity or aliases.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool MatchesText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (string.Equals(_id, text, System.StringComparison.Ordinal) || string.Equals(_name, text, System.StringComparison.Ordinal))
        {
            return true;
        }

        return ContainsText(_aliases, text);
    }

    /// <summary>
    /// Reports whether the supplied source and target units match the forward direction.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvertForward(UnitDefinition? fromUnit, UnitDefinition? toUnit)
    {
        if (fromUnit == null || toUnit == null)
        {
            return false;
        }

        return fromUnit.GetDimension().EqualsDimension(_sourceDimension) && toUnit.GetDimension().EqualsDimension(_targetDimension);
    }

    /// <summary>
    /// Reports whether the supplied source and target units match the reverse direction.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvertReverse(UnitDefinition? fromUnit, UnitDefinition? toUnit)
    {
        if (fromUnit == null || toUnit == null)
        {
            return false;
        }

        return fromUnit.GetDimension().EqualsDimension(_targetDimension) && toUnit.GetDimension().EqualsDimension(_sourceDimension);
    }

    /// <summary>
    /// Reports whether the supplied units can be converted with the selected equivalency.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvert(UnitDefinition? fromUnit, UnitDefinition? toUnit)
    {
        return CanConvertForward(fromUnit, toUnit) || CanConvertReverse(fromUnit, toUnit);
    }

    /// <summary>
    /// Converts a source system value with the forward equivalency formula.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <returns>The requested numeric value.</returns>
    public double ConvertForwardSystemValue(double value)
    {
        return ApplyFormula(value, _forwardScale, _forwardPower);
    }

    /// <summary>
    /// Converts a target system value with the reverse equivalency formula.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <returns>The requested numeric value.</returns>
    public double ConvertReverseSystemValue(double value)
    {
        return ApplyFormula(value, _reverseScale, _reversePower);
    }

    /// <summary>
    /// Selects and applies the forward or reverse equivalency formula for the supplied units.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>The requested numeric value.</returns>
    public double ConvertSystemValue(double value, UnitDefinition fromUnit, UnitDefinition toUnit)
    {
        if (CanConvertForward(fromUnit, toUnit))
        {
            return ConvertForwardSystemValue(value);
        }

        if (CanConvertReverse(fromUnit, toUnit))
        {
            return ConvertReverseSystemValue(value);
        }

        throw new System.InvalidOperationException("Equivalency does not support the requested unit dimensions.");
    }

    internal static double ApplyFormula(double value, RationalNumber scale, int power)
    {
        if (power < 0 && value == 0.0d)
        {
            throw new System.ArgumentException("Cannot apply a negative-power equivalency formula to zero.");
        }

        return scale.ToDouble() * System.Math.Pow(value, (double)power);
    }

    internal static string[] CopyTexts(string[]? values, string fieldName)
    {
        if (values == null)
        {
            return new string[0];
        }

        string[] copy = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            ValidateRequiredText(values[index], fieldName);
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static bool ContainsText(string[] values, string text)
    {
        int index = 0;

        while (index < values.Length)
        {
            if (string.Equals(values[index], text, System.StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateScale(RationalNumber value, string fieldName)
    {
        ValidateRequiredValue(value, fieldName);
        if (value.IsZero())
        {
            throw new System.ArgumentException("Equivalency " + fieldName + " must not be zero.");
        }
    }

    internal static void ValidatePower(int value, string fieldName)
    {
        if (value == 0)
        {
            throw new System.ArgumentException("Equivalency " + fieldName + " must not be zero.");
        }
    }

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Equivalency " + fieldName + " must not be empty.");
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
