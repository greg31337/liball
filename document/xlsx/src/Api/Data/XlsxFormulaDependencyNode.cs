using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents formula dependency node data used by the public workbook API.
/// </summary>
public class XlsxFormulaDependencyNode
{
    /// <summary>
    /// Stores the worksheet name containing this node.
    /// </summary>
    public string SheetName;
    /// <summary>
    /// Stores the sheet-qualified A1 reference used by workbook-wide graphs.
    /// </summary>
    public string QualifiedReference;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string CellReference;
    /// <summary>
    /// Stores the formula value used by this model.
    /// </summary>
    public string Formula;
    /// <summary>
    /// Contains precedents.
    /// </summary>
    public List<string> Precedents;
    /// <summary>
    /// Contains dependents.
    /// </summary>
    public List<string> Dependents;
    /// <summary>
    /// Contains external precedents.
    /// </summary>
    public List<string> ExternalPrecedents;
    /// <summary>
    /// Contains names.
    /// </summary>
    public List<string> Names;
    /// <summary>
    /// Initializes a new formula dependency node with its default state.
    /// </summary>
    public XlsxFormulaDependencyNode()
    {
        SheetName = "";
        QualifiedReference = "";
        CellReference = "";
        Formula = "";
        Precedents = new List<string>();
        Dependents = new List<string>();
        ExternalPrecedents = new List<string>();
        Names = new List<string>();
    }
}
