namespace molecule;


/// <summary>
/// Provides vendor format data and operations for molecular graphs.
/// </summary>
public class VendorFormat
{
    /// <summary>
    /// The name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The delimiter.
    /// </summary>
    public char Delimiter;
    /// <summary>
    /// Whether the value has header.
    /// </summary>
    public bool HasHeader;
    /// <summary>
    /// The molecule column.
    /// </summary>
    public string MoleculeColumn;
    /// <summary>
    /// The molecule format.
    /// </summary>
    public FormatKind MoleculeFormat;
    /// <summary>
    /// Whether molecule column is preserved.
    /// </summary>
    public bool PreserveMoleculeColumn;

    /// <summary>
    /// Initializes a new instance of the <see cref="VendorFormat"/> class.
    /// </summary>
    public VendorFormat()
    {
        Name = "Delimited";
        Delimiter = ',';
        HasHeader = true;
        MoleculeColumn = "smiles";
        MoleculeFormat = FormatKind.Smiles;
        PreserveMoleculeColumn = true;
    }

    /// <summary>
    /// Use csv.
    /// </summary>
    internal void UseCsv()
    {
        Delimiter = ',';
    }

    /// <summary>
    /// Use tsv.
    /// </summary>
    internal void UseTsv()
    {
        Delimiter = '\t';
    }

    /// <summary>
    /// Returns the safe delimiter.
    /// </summary>
    /// <returns>The resulting char.</returns>
    internal char GetSafeDelimiter()
    {
        if (Delimiter == '\0')
        {
            return ',';
        }

        return Delimiter;
    }

    /// <summary>
    /// Returns the safe molecule column.
    /// </summary>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    internal string GetSafeMoleculeColumn()
    {
        if (MoleculeColumn == null || MoleculeColumn.Length == 0)
        {
            return "smiles";
        }

        return MoleculeColumn;
    }
}

