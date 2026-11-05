namespace countrycodes;

/// <summary>
/// Holds a detached public snapshot of a current country record.
/// </summary>
public class Country
{
    /// <summary>
    /// The canonical ISO 3166-1 alpha-2 code.
    /// </summary>
    public string Alpha2;

    /// <summary>
    /// The canonical ISO 3166-1 alpha-3 code.
    /// </summary>
    public string Alpha3;

    /// <summary>
    /// The zero-padded ISO 3166-1 numeric code.
    /// </summary>
    public string NumericText;

    /// <summary>
    /// The ISO 3166-1 numeric code as an integer convenience value.
    /// </summary>
    public int NumericValue;

    /// <summary>
    /// The stable canonical identity used for joins.
    /// </summary>
    public string CanonicalCode;

    /// <summary>
    /// The assignment status of the country code.
    /// </summary>
    public CodeAssignmentStatus AssignmentStatus;

    /// <summary>
    /// The canonical short country name.
    /// </summary>
    public string ShortName;

    /// <summary>
    /// The official country name when one is available.
    /// </summary>
    public string? OfficialName;

    /// <summary>
    /// Alternate names associated with the country.
    /// </summary>
    public string[] Aliases;

    /// <summary>
    /// Localized names associated with the country.
    /// </summary>
    public LocalizedName[] LocalizedNames;

    /// <summary>
    /// The UN M49 region codes in the country's ancestry.
    /// </summary>
    public string[] RegionM49Codes;

    /// <summary>
    /// The regional-indicator flag emoji derived from the alpha-2 code.
    /// </summary>
    public string FlagEmoji;

    /// <summary>
    /// Creates a detached country snapshot.
    /// </summary>
    /// <param name="alpha2">The canonical alpha-2 code.</param>
    /// <param name="alpha3">The canonical alpha-3 code.</param>
    /// <param name="numericText">The zero-padded numeric code.</param>
    /// <param name="numericValue">The numeric convenience value.</param>
    /// <param name="canonicalCode">The stable canonical identity.</param>
    /// <param name="assignmentStatus">The code assignment status.</param>
    /// <param name="shortName">The canonical short name.</param>
    /// <param name="officialName">The official name, when available.</param>
    /// <param name="aliases">The alternate names.</param>
    /// <param name="localizedNames">The localized names.</param>
    /// <param name="regionM49Codes">The M49 ancestry codes.</param>
    /// <param name="flagEmoji">The derived regional-indicator flag emoji.</param>
    public Country(
        string alpha2,
        string alpha3,
        string numericText,
        int numericValue,
        string canonicalCode,
        CodeAssignmentStatus assignmentStatus,
        string shortName,
        string? officialName,
        string[] aliases,
        LocalizedName[] localizedNames,
        string[] regionM49Codes,
        string flagEmoji)
    {
        Alpha2 = alpha2;
        Alpha3 = alpha3;
        NumericText = numericText;
        NumericValue = numericValue;
        CanonicalCode = canonicalCode;
        AssignmentStatus = assignmentStatus;
        ShortName = shortName;
        OfficialName = officialName;
        Aliases = (string[])aliases.Clone();
        LocalizedNames = LocalizedName.CopyArray(localizedNames);
        RegionM49Codes = (string[])regionM49Codes.Clone();
        FlagEmoji = flagEmoji;
    }

    internal Country Copy()
    {
        return new Country(
            Alpha2,
            Alpha3,
            NumericText,
            NumericValue,
            CanonicalCode,
            AssignmentStatus,
            ShortName,
            OfficialName,
            Aliases,
            LocalizedNames,
            RegionM49Codes,
            FlagEmoji);
    }

    internal static Country[] CopyArray(Country[] values)
    {
        Country[] copies = new Country[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copies[index] = values[index].Copy();
            index += 1;
        }

        return copies;
    }
}
