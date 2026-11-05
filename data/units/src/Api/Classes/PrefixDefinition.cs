namespace units;

/// <summary>
/// Describes a named unit prefix and its exact scale factor.
/// </summary>
public class PrefixDefinition : IPrefixDefinition
{
    internal string _id;
    internal string _name;
    internal string _symbol;
    internal RationalNumber _factor;

    /// <summary>
    /// Initializes a new prefix definition from the supplied metadata.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="symbol">The unit or metadata symbol.</param>
    /// <param name="factor">The scalar multiplier.</param>
    public PrefixDefinition(string id, string name, string symbol, RationalNumber factor)
    {
        ValidateRequiredText(id, "id");
        ValidateRequiredText(name, "name");
        ValidateRequiredText(symbol, "symbol");
        ValidateRequiredValue(factor, "factor");

        if (!factor.IsPositive())
        {
            throw new System.ArgumentException("Prefix factor must be positive.");
        }

        _id = id;
        _name = name;
        _symbol = symbol;
        _factor = factor;
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
    /// Gets the factor.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetFactor()
    {
        return _factor;
    }

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Prefix " + fieldName + " must not be empty.");
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
