using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents cell format definition data used by the public workbook API.
/// </summary>
public class XlsxCellFormatDefinition
{
    /// <summary>
    /// Stores the number format id value used by this model.
    /// </summary>
    public uint NumberFormatId;
    /// <summary>
    /// Indicates whether number format id is present and should be written.
    /// </summary>
    public bool HasNumberFormatId;
    /// <summary>
    /// Stores the font id value used by this model.
    /// </summary>
    public uint FontId;
    /// <summary>
    /// Indicates whether font id is present and should be written.
    /// </summary>
    public bool HasFontId;
    /// <summary>
    /// Stores the fill id value used by this model.
    /// </summary>
    public uint FillId;
    /// <summary>
    /// Indicates whether fill id is present and should be written.
    /// </summary>
    public bool HasFillId;
    /// <summary>
    /// Stores the border id value used by this model.
    /// </summary>
    public uint BorderId;
    /// <summary>
    /// Indicates whether border id is present and should be written.
    /// </summary>
    public bool HasBorderId;
    /// <summary>
    /// Stores the format id value used by this model.
    /// </summary>
    public uint FormatId;
    /// <summary>
    /// Indicates whether format id is present and should be written.
    /// </summary>
    public bool HasFormatId;
    /// <summary>
    /// Stores the apply number format value used by this model.
    /// </summary>
    public bool ApplyNumberFormat;
    /// <summary>
    /// Indicates whether apply number format is present and should be written.
    /// </summary>
    public bool HasApplyNumberFormat;
    /// <summary>
    /// Stores the apply font value used by this model.
    /// </summary>
    public bool ApplyFont;
    /// <summary>
    /// Indicates whether apply font is present and should be written.
    /// </summary>
    public bool HasApplyFont;
    /// <summary>
    /// Stores the apply fill value used by this model.
    /// </summary>
    public bool ApplyFill;
    /// <summary>
    /// Indicates whether apply fill is present and should be written.
    /// </summary>
    public bool HasApplyFill;
    /// <summary>
    /// Stores the apply border value used by this model.
    /// </summary>
    public bool ApplyBorder;
    /// <summary>
    /// Indicates whether apply border is present and should be written.
    /// </summary>
    public bool HasApplyBorder;
    /// <summary>Stores typed alignment settings.</summary>
    public XlsxStyleAlignment Alignment;
    /// <summary>Indicates whether alignment is present.</summary>
    public bool HasAlignment;
    /// <summary>Stores typed protection settings.</summary>
    public XlsxStyleProtection Protection;
    /// <summary>Indicates whether protection is present.</summary>
    public bool HasProtection;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new cell format definition with its default state.
    /// </summary>
    public XlsxCellFormatDefinition()
    {
        NumberFormatId = 0;
        HasNumberFormatId = false;
        FontId = 0;
        HasFontId = false;
        FillId = 0;
        HasFillId = false;
        BorderId = 0;
        HasBorderId = false;
        FormatId = 0;
        HasFormatId = false;
        ApplyNumberFormat = false;
        HasApplyNumberFormat = false;
        ApplyFont = false;
        HasApplyFont = false;
        ApplyFill = false;
        HasApplyFill = false;
        ApplyBorder = false;
        HasApplyBorder = false;
        Alignment = new XlsxStyleAlignment();
        HasAlignment = false;
        Protection = new XlsxStyleProtection();
        HasProtection = false;
        RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
