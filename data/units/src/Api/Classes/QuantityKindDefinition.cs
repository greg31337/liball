namespace units;

/// <summary>
/// Describes a named physical quantity kind and the units accepted for it.
/// </summary>
public class QuantityKindDefinition : IQuantityKindDefinition
{
    internal string _id;
    internal string _name;
    internal DimensionVector _dimension;
    internal string _defaultUnitId;
    internal string[] _aliases;
    internal string[] _acceptedUnitIds;

    /// <summary>
    /// Initializes a new quantity kind definition from the supplied metadata.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="dimension">The dimension vector to use.</param>
    /// <param name="defaultUnitId">The identifier of the default unit.</param>
    /// <param name="aliases">The alternate lookup texts.</param>
    /// <param name="acceptedUnitIds">The identifiers of units accepted for the quantity kind.</param>
    public QuantityKindDefinition(string id, string name, DimensionVector dimension, string defaultUnitId, string[]? aliases, string[]? acceptedUnitIds)
    {
        ValidateRequiredText(id, "id");
        ValidateRequiredText(name, "name");
        ValidateRequiredValue(dimension, "dimension");
        ValidateRequiredText(defaultUnitId, "defaultUnitId");

        _id = id;
        _name = name;
        _dimension = dimension;
        _defaultUnitId = defaultUnitId;
        _aliases = CopyTexts(aliases, "alias");
        _acceptedUnitIds = CopyAcceptedUnitIds(defaultUnitId, acceptedUnitIds);
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
    /// Gets the dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector GetDimension()
    {
        return _dimension;
    }

    /// <summary>
    /// Gets the default unit id.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetDefaultUnitId()
    {
        return _defaultUnitId;
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
    /// Returns a copy of the accepted unit ids.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetAcceptedUnitIds()
    {
        return CopyTexts(_acceptedUnitIds, "acceptedUnitId");
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
    /// Reports whether the supplied unit identifier is accepted for this quantity kind.
    /// </summary>
    /// <param name="unitId">The stable unit identifier.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool AcceptsUnitId(string? unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        return ContainsText(_acceptedUnitIds, unitId);
    }

    /// <summary>
    /// Reports whether the supplied unit has this quantity kind's dimension and is accepted by identifier.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool AcceptsUnit(UnitDefinition? unit)
    {
        if (unit == null)
        {
            return false;
        }

        return _dimension.EqualsDimension(unit.GetDimension()) && AcceptsUnitId(unit.GetId());
    }

    internal static string[] CopyAcceptedUnitIds(string defaultUnitId, string[]? acceptedUnitIds)
    {
        string[] copied = CopyTexts(acceptedUnitIds, "acceptedUnitId");

        if (ContainsText(copied, defaultUnitId))
        {
            return copied;
        }

        string[] withDefault = new string[copied.Length + 1];
        int index = 0;

        while (index < copied.Length)
        {
            withDefault[index] = copied[index];
            index = index + 1;
        }

        withDefault[copied.Length] = defaultUnitId;
        return withDefault;
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
            throw new System.ArgumentException("Quantity kind " + fieldName + " must not be empty.");
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
