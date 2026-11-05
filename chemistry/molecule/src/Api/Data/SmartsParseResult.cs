using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by smarts parse.
/// </summary>
public class SmartsParseResult
{
    /// <summary>
    /// The query.
    /// </summary>
    public QueryMolecule Query;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The error message.
    /// </summary>
    public string ErrorMessage;
    /// <summary>
    /// The error index.
    /// </summary>
    public int ErrorIndex;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartsParseResult"/> class.
    /// </summary>
    public SmartsParseResult()
    {
        Query = new QueryMolecule();
        Success = false;
        ErrorMessage = "";
        ErrorIndex = -1;
        Report = new MoleculeReport();
    }
}

