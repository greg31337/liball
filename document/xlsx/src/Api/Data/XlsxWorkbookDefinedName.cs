using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook defined name data used by the public workbook API.
/// </summary>
public class XlsxWorkbookDefinedName
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string? Name;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the local sheet id value used by this model.
    /// </summary>
    public int LocalSheetId;
    /// <summary>
    /// Indicates whether local sheet id is present and should be written.
    /// </summary>
    public bool HasLocalSheetId;
    /// <summary>
    /// Stores the comment value used by this model.
    /// </summary>
    public string? Comment;
    /// <summary>
    /// Stores the custom menu value used by this model.
    /// </summary>
    public string? CustomMenu;
    /// <summary>
    /// Stores the description value used by this model.
    /// </summary>
    public string? Description;
    /// <summary>
    /// Stores the help value used by this model.
    /// </summary>
    public string? Help;
    /// <summary>
    /// Stores the status bar value used by this model.
    /// </summary>
    public string? StatusBar;
    /// <summary>
    /// Stores the shortcut key value used by this model.
    /// </summary>
    public string? ShortcutKey;
    /// <summary>
    /// Stores the vb procedure value used by this model.
    /// </summary>
    public bool VbProcedure;
    /// <summary>
    /// Indicates whether vb procedure is present and should be written.
    /// </summary>
    public bool HasVbProcedure;
    /// <summary>
    /// Stores the xlm value used by this model.
    /// </summary>
    public bool Xlm;
    /// <summary>
    /// Indicates whether xlm is present and should be written.
    /// </summary>
    public bool HasXlm;
    /// <summary>
    /// Stores the function group id value used by this model.
    /// </summary>
    public uint FunctionGroupId;
    /// <summary>
    /// Indicates whether function group id is present and should be written.
    /// </summary>
    public bool HasFunctionGroupId;
    /// <summary>
    /// Stores the hidden value used by this model.
    /// </summary>
    public bool Hidden;
    /// <summary>
    /// Indicates whether hidden is present and should be written.
    /// </summary>
    public bool HasHidden;
    /// <summary>
    /// Stores the function value used by this model.
    /// </summary>
    public bool Function;
    /// <summary>
    /// Indicates whether function is present and should be written.
    /// </summary>
    public bool HasFunction;
    /// <summary>
    /// Stores the publish to server value used by this model.
    /// </summary>
    public bool PublishToServer;
    /// <summary>
    /// Indicates whether publish to server is present and should be written.
    /// </summary>
    public bool HasPublishToServer;
    /// <summary>
    /// Stores the workbook parameter value used by this model.
    /// </summary>
    public bool WorkbookParameter;
    /// <summary>
    /// Indicates whether workbook parameter is present and should be written.
    /// </summary>
    public bool HasWorkbookParameter;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new workbook defined name with its default state.
    /// </summary>
    public XlsxWorkbookDefinedName()
    {
        Name = "";
        Text = "";
        LocalSheetId = 0;
        HasLocalSheetId = false;
        Comment = "";
        CustomMenu = "";
        Description = "";
        Help = "";
        StatusBar = "";
        ShortcutKey = "";
        VbProcedure = false;
        HasVbProcedure = false;
        Xlm = false;
        HasXlm = false;
        FunctionGroupId = 0;
        HasFunctionGroupId = false;
        Hidden = false;
        HasHidden = false;
        Function = false;
        HasFunction = false;
        PublishToServer = false;
        HasPublishToServer = false;
        WorkbookParameter = false;
        HasWorkbookParameter = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
