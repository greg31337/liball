namespace units;

/// <summary>
/// Describes a named unit system and its preferred units by quantity kind.
/// </summary>
public class UnitSystemDefinition : IUnitSystemDefinition
{
    internal string _id;
    internal string _name;
    internal string[] _baseUnitIds;
    internal string[] _preferredQuantityKindIds;
    internal string[] _preferredUnitIds;

    /// <summary>
    /// Initializes a new unit system definition from the supplied metadata.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="baseUnitIds">The base unit ids to use.</param>
    /// <param name="preferredQuantityKindIds">The preferred quantity kind ids to use.</param>
    /// <param name="preferredUnitIds">The preferred unit ids to use.</param>
    public UnitSystemDefinition(string id, string name, string[]? baseUnitIds, string[]? preferredQuantityKindIds, string[]? preferredUnitIds)
    {
        ValidateRequiredText(id, "id");
        ValidateRequiredText(name, "name");

        _id = id;
        _name = name;
        _baseUnitIds = CopyTexts(baseUnitIds, "baseUnitId");
        _preferredQuantityKindIds = CopyTexts(preferredQuantityKindIds, "preferredQuantityKindId");
        _preferredUnitIds = CopyTexts(preferredUnitIds, "preferredUnitId");

        if (_preferredQuantityKindIds.Length != _preferredUnitIds.Length)
        {
            throw new System.ArgumentException("Preferred quantity kind ids and preferred unit ids must have the same length.");
        }

        ValidateNoDuplicates(_baseUnitIds, "base unit id");
        ValidateNoDuplicates(_preferredQuantityKindIds, "preferred quantity kind id");
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
    /// Returns a copy of the base unit ids.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetBaseUnitIds()
    {
        return CopyTexts(_baseUnitIds, "baseUnitId");
    }

    /// <summary>
    /// Returns a copy of the preferred quantity kind ids.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetPreferredQuantityKindIds()
    {
        return CopyTexts(_preferredQuantityKindIds, "preferredQuantityKindId");
    }

    /// <summary>
    /// Returns a copy of the preferred unit ids.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetPreferredUnitIds()
    {
        return CopyTexts(_preferredUnitIds, "preferredUnitId");
    }

    /// <summary>
    /// Reports whether the supplied identifier is one of this system's base units.
    /// </summary>
    /// <param name="unitId">The stable unit identifier.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool UsesBaseUnitId(string? unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        return ContainsText(_baseUnitIds, unitId);
    }

    /// <summary>
    /// Reports whether this system defines a preferred unit for the quantity kind.
    /// </summary>
    /// <param name="quantityKindId">The quantity kind id to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasPreferredUnitFor(string? quantityKindId)
    {
        if (string.IsNullOrWhiteSpace(quantityKindId))
        {
            return false;
        }

        return FindPreferredIndex(quantityKindId) >= 0;
    }

    /// <summary>
    /// Returns the preferred unit identifier for the quantity kind, or null when none is configured.
    /// </summary>
    /// <param name="quantityKindId">The quantity kind id to use.</param>
    /// <returns>The requested text.</returns>
    public string? FindPreferredUnitId(string? quantityKindId)
    {
        if (string.IsNullOrWhiteSpace(quantityKindId))
        {
            return null;
        }

        int index = FindPreferredIndex(quantityKindId);
        if (index < 0)
        {
            return null;
        }

        return _preferredUnitIds[index];
    }

    internal int FindPreferredIndex(string quantityKindId)
    {
        int index = 0;

        while (index < _preferredQuantityKindIds.Length)
        {
            if (string.Equals(_preferredQuantityKindIds[index], quantityKindId, System.StringComparison.Ordinal))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
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

    internal static void ValidateNoDuplicates(string[] values, string label)
    {
        int index = 0;

        while (index < values.Length)
        {
            int other = index + 1;

            while (other < values.Length)
            {
                if (string.Equals(values[index], values[other], System.StringComparison.Ordinal))
                {
                    throw new System.ArgumentException("Duplicate " + label + " is not allowed.");
                }

                other = other + 1;
            }

            index = index + 1;
        }
    }

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Unit system " + fieldName + " must not be empty.");
        }
    }
}
