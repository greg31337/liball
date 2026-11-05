using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Controls delimited vendor output columns, quoting, delimiter, and embedded molecule format.
/// </summary>
public class VendorWriteOptions
{
    /// <summary>
    /// The field delimiter; <c>\0</c> uses a comma.
    /// </summary>
    public char Delimiter;
    /// <summary>
    /// Whether a header row is emitted.
    /// </summary>
    public bool WriteHeader;
    /// <summary>
    /// The name of the column containing serialized molecules.
    /// </summary>
    public string MoleculeColumn;
    /// <summary>
    /// The format used for embedded molecule text.
    /// </summary>
    public FormatKind MoleculeFormat;
    /// <summary>
    /// Caller-selected columns in output order.
    /// </summary>
    public List<string> Columns;
    /// <summary>
    /// Whether vendor metadata columns not listed in <see cref="Columns"/> are appended.
    /// </summary>
    public bool IncludeUnlistedVendorFields;
    /// <summary>
    /// Whether every field is quoted rather than only fields that require quoting.
    /// </summary>
    public bool QuoteAllFields;

    /// <summary>
    /// Initializes CSV output with a header, a SMILES column, lossless metadata columns, and required-field quoting.
    /// </summary>
    public VendorWriteOptions()
    {
        Delimiter = ',';
        WriteHeader = true;
        MoleculeColumn = "smiles";
        MoleculeFormat = FormatKind.Smiles;
        Columns = new List<string>();
        IncludeUnlistedVendorFields = true;
        QuoteAllFields = false;
    }
}
