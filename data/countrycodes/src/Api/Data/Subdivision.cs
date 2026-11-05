namespace countrycodes;

/// <summary>
/// Holds a detached public snapshot of a country subdivision.
/// </summary>
public class Subdivision
{
    /// <summary>
    /// The full canonical ISO 3166-2 code.
    /// </summary>
    public string Code;

    /// <summary>
    /// The owning country's canonical alpha-2 code.
    /// </summary>
    public string CountryCode;

    /// <summary>
    /// The local suffix of the subdivision code.
    /// </summary>
    public string Suffix;

    /// <summary>
    /// The canonical subdivision name.
    /// </summary>
    public string Name;

    /// <summary>
    /// The source-defined subdivision type.
    /// </summary>
    public string Type;

    /// <summary>
    /// The full parent subdivision code when one is available.
    /// </summary>
    public string? ParentCode;

    /// <summary>
    /// The lifecycle status of the subdivision.
    /// </summary>
    public SubdivisionStatus Status;

    /// <summary>
    /// Alternate names or codes associated with the subdivision.
    /// </summary>
    public string[] Aliases;

    /// <summary>
    /// Localized names associated with the subdivision.
    /// </summary>
    public LocalizedName[] LocalizedNames;

    /// <summary>
    /// Replacement subdivision or country codes for deprecated records.
    /// </summary>
    public string[] ReplacementCodes;

    /// <summary>
    /// Creates a detached subdivision snapshot.
    /// </summary>
    /// <param name="code">The full canonical subdivision code.</param>
    /// <param name="countryCode">The owning country alpha-2 code.</param>
    /// <param name="suffix">The local subdivision suffix.</param>
    /// <param name="name">The canonical subdivision name.</param>
    /// <param name="type">The source-defined subdivision type.</param>
    /// <param name="parentCode">The parent code, when available.</param>
    /// <param name="status">The lifecycle status.</param>
    /// <param name="aliases">The alternate names or codes.</param>
    /// <param name="localizedNames">The localized names.</param>
    /// <param name="replacementCodes">The replacement codes.</param>
    public Subdivision(
        string code,
        string countryCode,
        string suffix,
        string name,
        string type,
        string? parentCode,
        SubdivisionStatus status,
        string[] aliases,
        LocalizedName[] localizedNames,
        string[] replacementCodes)
    {
        Code = code;
        CountryCode = countryCode;
        Suffix = suffix;
        Name = name;
        Type = type;
        ParentCode = parentCode;
        Status = status;
        Aliases = (string[])aliases.Clone();
        LocalizedNames = LocalizedName.CopyArray(localizedNames);
        ReplacementCodes = (string[])replacementCodes.Clone();
    }

    internal Subdivision Copy()
    {
        return new Subdivision(
            Code,
            CountryCode,
            Suffix,
            Name,
            Type,
            ParentCode,
            Status,
            Aliases,
            LocalizedNames,
            ReplacementCodes);
    }

    internal static Subdivision[] CopyArray(Subdivision[] values)
    {
        Subdivision[] copies = new Subdivision[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copies[index] = values[index].Copy();
            index += 1;
        }

        return copies;
    }
}
