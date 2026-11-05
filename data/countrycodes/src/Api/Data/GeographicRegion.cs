namespace countrycodes;

/// <summary>
/// Holds a detached public snapshot of a UN M49 geographic region.
/// </summary>
public class GeographicRegion
{
    /// <summary>
    /// The zero-padded UN M49 code.
    /// </summary>
    public string M49Code;

    /// <summary>
    /// The UN M49 code as an integer convenience value.
    /// </summary>
    public int M49Value;

    /// <summary>
    /// The canonical English region name.
    /// </summary>
    public string Name;

    /// <summary>
    /// Localized names associated with the region.
    /// </summary>
    public LocalizedName[] LocalizedNames;

    /// <summary>
    /// The level of the region in the geographic hierarchy.
    /// </summary>
    public GeographicRegionLevel Level;

    /// <summary>
    /// The parent UN M49 code when one is available.
    /// </summary>
    public string? ParentM49Code;

    /// <summary>
    /// The direct child UN M49 codes.
    /// </summary>
    public string[] ChildM49Codes;

    /// <summary>
    /// The canonical alpha-2 codes of countries belonging to the region.
    /// </summary>
    public string[] CountryCodes;

    /// <summary>
    /// Creates a detached geographic-region snapshot.
    /// </summary>
    /// <param name="m49Code">The zero-padded UN M49 code.</param>
    /// <param name="m49Value">The numeric convenience value.</param>
    /// <param name="name">The canonical English name.</param>
    /// <param name="localizedNames">The localized names.</param>
    /// <param name="level">The hierarchy level.</param>
    /// <param name="parentM49Code">The parent M49 code, when available.</param>
    /// <param name="childM49Codes">The direct child M49 codes.</param>
    /// <param name="countryCodes">The member country alpha-2 codes.</param>
    public GeographicRegion(
        string m49Code,
        int m49Value,
        string name,
        LocalizedName[] localizedNames,
        GeographicRegionLevel level,
        string? parentM49Code,
        string[] childM49Codes,
        string[] countryCodes)
    {
        M49Code = m49Code;
        M49Value = m49Value;
        Name = name;
        LocalizedNames = LocalizedName.CopyArray(localizedNames);
        Level = level;
        ParentM49Code = parentM49Code;
        ChildM49Codes = (string[])childM49Codes.Clone();
        CountryCodes = (string[])countryCodes.Clone();
    }
}
