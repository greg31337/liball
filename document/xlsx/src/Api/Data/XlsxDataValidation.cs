using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents data validation data used by the public workbook API.
/// </summary>
public class XlsxDataValidation
{
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the error style value used by this model.
    /// </summary>
    public string ErrorStyle;
    /// <summary>
    /// Stores the ime mode value used by this model.
    /// </summary>
    public string ImeMode;
    /// <summary>
    /// Stores the operator value used by this model.
    /// </summary>
    public string Operator;
    /// <summary>
    /// Stores the allow blank value used by this model.
    /// </summary>
    public bool AllowBlank;
    /// <summary>
    /// Indicates whether allow blank is present and should be written.
    /// </summary>
    public bool HasAllowBlank;
    /// <summary>
    /// Stores the show drop down value used by this model.
    /// </summary>
    public bool ShowDropDown;
    /// <summary>
    /// Indicates whether show drop down is present and should be written.
    /// </summary>
    public bool HasShowDropDown;
    /// <summary>
    /// Stores the show input message value used by this model.
    /// </summary>
    public bool ShowInputMessage;
    /// <summary>
    /// Indicates whether show input message is present and should be written.
    /// </summary>
    public bool HasShowInputMessage;
    /// <summary>
    /// Stores the show error message value used by this model.
    /// </summary>
    public bool ShowErrorMessage;
    /// <summary>
    /// Indicates whether show error message is present and should be written.
    /// </summary>
    public bool HasShowErrorMessage;
    /// <summary>
    /// Stores the sqref value used by this model.
    /// </summary>
    public string Sqref;
    /// <summary>
    /// Stores the prompt title value used by this model.
    /// </summary>
    public string PromptTitle;
    /// <summary>
    /// Stores the prompt value used by this model.
    /// </summary>
    public string Prompt;
    /// <summary>
    /// Stores the error title value used by this model.
    /// </summary>
    public string ErrorTitle;
    /// <summary>
    /// Stores the error value used by this model.
    /// </summary>
    public string Error;
    /// <summary>
    /// Stores the formula1 value used by this model.
    /// </summary>
    public string Formula1;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string Formula1RawXml;
    /// <summary>
    /// Stores the formula2 value used by this model.
    /// </summary>
    public string Formula2;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string Formula2RawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new data validation with its default state.
    /// </summary>
    public XlsxDataValidation()
    {
        Type = "";
        ErrorStyle = "";
        ImeMode = "";
        Operator = "";
        AllowBlank = false;
        HasAllowBlank = false;
        ShowDropDown = false;
        HasShowDropDown = false;
        ShowInputMessage = false;
        HasShowInputMessage = false;
        ShowErrorMessage = false;
        HasShowErrorMessage = false;
        Sqref = "";
        PromptTitle = "";
        Prompt = "";
        ErrorTitle = "";
        Error = "";
        Formula1 = "";
        Formula1RawXml = "";
        Formula2 = "";
        Formula2RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
