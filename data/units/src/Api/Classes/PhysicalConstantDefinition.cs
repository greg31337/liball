namespace units;

/// <summary>
/// Describes a physical constant, its quantity, provenance, uncertainty, aliases, and exactness label.
/// </summary>
/// <remarks>
/// Exactness is descriptive metadata. Measured constants still store their representative numeric value as a double.
/// </remarks>
public class PhysicalConstantDefinition : IPhysicalConstantDefinition
{
    internal string _id;
    internal string _name;
    internal string _symbol;
    internal string _description;
    internal string _exactnessLabel;
    internal string _sourceLabel;
    internal string _adjustmentOrEdition;
    internal double _standardUncertainty;
    internal QuantityValue _quantity;
    internal string[] _aliases;

    /// <summary>
    /// Initializes a new physical constant definition from the supplied metadata.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="symbol">The unit or metadata symbol.</param>
    /// <param name="description">The plain-English description.</param>
    /// <param name="exactnessLabel">The label indicating whether the constant is exact or measured.</param>
    /// <param name="sourceLabel">The source label for the metadata.</param>
    /// <param name="adjustmentOrEdition">The adjustment or edition identifying the source values.</param>
    /// <param name="standardUncertainty">The absolute standard uncertainty in the quantity's stored unit.</param>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="aliases">The alternate lookup texts.</param>
    public PhysicalConstantDefinition(string id, string name, string? symbol, string? description, string exactnessLabel, string sourceLabel, string adjustmentOrEdition, double standardUncertainty, QuantityValue quantity, string[]? aliases)
    {
        ValidateRequiredText(id, "id");
        ValidateRequiredText(name, "name");
        ValidateRequiredText(exactnessLabel, "exactnessLabel");
        ValidateRequiredText(sourceLabel, "sourceLabel");
        ValidateRequiredText(adjustmentOrEdition, "adjustmentOrEdition");
        ValidateStandardUncertainty(standardUncertainty);
        ValidateRequiredValue(quantity, "quantity");

        if (string.Equals(exactnessLabel, "exact", System.StringComparison.Ordinal) && standardUncertainty != 0.0d)
        {
            throw new System.ArgumentException("Exact physical constants must have zero standard uncertainty.");
        }

        _id = id;
        _name = name;
        if (symbol == null)
        {
            _symbol = "";
        }
        else
        {
            _symbol = symbol;
        }

        if (description == null)
        {
            _description = "";
        }
        else
        {
            _description = description;
        }

        _exactnessLabel = exactnessLabel;
        _sourceLabel = sourceLabel;
        _adjustmentOrEdition = adjustmentOrEdition;
        _standardUncertainty = standardUncertainty;
        _quantity = new QuantityValue(quantity.GetValue(), quantity.GetUnit());
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
    /// Gets the symbol.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSymbol()
    {
        return _symbol;
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
    /// Gets the exactness label.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetExactnessLabel()
    {
        return _exactnessLabel;
    }

    /// <summary>
    /// Reports whether the constant is labeled as exact.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsExact()
    {
        return string.Equals(_exactnessLabel, "exact", System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the source label for the constant metadata.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSourceLabel()
    {
        return _sourceLabel;
    }

    /// <summary>
    /// Gets the source adjustment or edition label.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetAdjustmentOrEdition()
    {
        return _adjustmentOrEdition;
    }

    /// <summary>
    /// Gets the absolute standard uncertainty expressed in the quantity's stored unit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double GetStandardUncertainty()
    {
        return _standardUncertainty;
    }

    /// <summary>
    /// Gets the quantity.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue GetQuantity()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
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

        if (string.Equals(_id, text, System.StringComparison.Ordinal) || string.Equals(_name, text, System.StringComparison.Ordinal) || string.Equals(_symbol, text, System.StringComparison.Ordinal))
        {
            return true;
        }

        return ContainsText(_aliases, text);
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

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Physical constant " + fieldName + " must not be empty.");
        }
    }

    internal static void ValidateRequiredValue(object value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateStandardUncertainty(double value)
    {
        if (value < 0.0d || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new System.ArgumentException("Physical constant standardUncertainty must be nonnegative and finite.");
        }
    }
}
