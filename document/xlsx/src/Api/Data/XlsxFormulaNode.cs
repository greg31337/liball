using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents formula node data used by the public workbook API.
/// </summary>
public class XlsxFormulaNode
{
    /// <summary>
    /// Stores the kind value used by this model.
    /// </summary>
    public XlsxFormulaNodeKind Kind;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the operator value used by this model.
    /// </summary>
    public string Operator;
    /// <summary>
    /// Stores the start value used by this model.
    /// </summary>
    public int Start;
    /// <summary>
    /// Stores the length value used by this model.
    /// </summary>
    public int Length;
    /// <summary>
    /// Stores the children value used by this model.
    /// </summary>
    public List<XlsxFormulaNode> Children;
    /// <summary>
    /// Initializes a new formula node with its default state.
    /// </summary>
    public XlsxFormulaNode()
    {
        Kind = XlsxFormulaNodeKind.Unknown;
        Text = "";
        Operator = "";
        Start = 0;
        Length = 0;
        Children = new List<XlsxFormulaNode>();
    }
}
