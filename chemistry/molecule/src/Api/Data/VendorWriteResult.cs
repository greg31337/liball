using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains delimited vendor output, selected columns, counts, and diagnostics.
/// </summary>
public class VendorWriteResult
{
    /// <summary>
    /// The emitted delimited text.
    /// </summary>
    public string Text;
    /// <summary>
    /// Diagnostics produced while writing.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether output completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The columns emitted in output order.
    /// </summary>
    public List<string> Columns;
    /// <summary>
    /// The zero-based molecule column index.
    /// </summary>
    public int MoleculeColumnIndex;
    /// <summary>
    /// The number of emitted molecule records.
    /// </summary>
    public int WrittenCount;

    /// <summary>
    /// Initializes an empty vendor write result.
    /// </summary>
    public VendorWriteResult()
    {
        Text = "";
        Report = new MoleculeReport();
        Success = false;
        Columns = new List<string>();
        MoleculeColumnIndex = -1;
        WrittenCount = 0;
    }
}
