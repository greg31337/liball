using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents cell value data used by the public workbook API.
/// </summary>
public class XlsxCellValue
{
    /// <summary>
    /// Stores the kind value used by this model.
    /// </summary>
    public XlsxCellValueKind Kind;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the number value used by this model.
    /// </summary>
    public double Number;
    /// <summary>
    /// Stores an exact base-10 number value.
    /// </summary>
    public decimal Decimal;
    /// <summary>
    /// Stores the boolean value used by this model.
    /// </summary>
    public bool Boolean;
    /// <summary>
    /// Stores the date time value used by this model.
    /// </summary>
    public System.DateTime DateTime;
    /// <summary>
    /// Stores an elapsed duration value.
    /// </summary>
    public System.TimeSpan Duration;
    /// <summary>
    /// Stores the invariant lexical value used to preserve exact numeric and ISO date text.
    /// </summary>
    public string ExactText;
    /// <summary>
    /// Indicates whether invariant exact text is present and should be written.
    /// </summary>
    public bool HasExactText;
    /// <summary>
    /// Stores a typed Excel error kind.
    /// </summary>
    public XlsxCellErrorKind ErrorKind;
    /// <summary>
    /// Indicates whether a typed Excel error kind is present.
    /// </summary>
    public bool HasErrorKind;
    /// <summary>
    /// Stores the shared string index value used by this model.
    /// </summary>
    public uint SharedStringIndex;
    /// <summary>
    /// Stores the formula value used by this model.
    /// </summary>
    public string Formula;
    /// <summary>
    /// Stores the cached value value used by this model.
    /// </summary>
    public string CachedValue;
    /// <summary>
    /// Stores the cached type value used by this model.
    /// </summary>
    public string CachedType;
    /// <summary>
    /// Stores the typed cached result of a formula.
    /// </summary>
    public XlsxCellValue? TypedCachedValue;
    /// <summary>
    /// Indicates whether a typed formula cached value is present.
    /// </summary>
    public bool HasTypedCachedValue;
    /// <summary>
    /// Stores the raw type value used by this model.
    /// </summary>
    public string RawType;
    /// <summary>
    /// Stores the raw value value used by this model.
    /// </summary>
    public string RawValue;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RichTextRawXml;
    /// <summary>
    /// Contains typed rich-text runs.
    /// </summary>
    public List<XlsxSharedStringRichTextRun> RichTextRuns;
    /// <summary>
    /// Contains typed phonetic text runs.
    /// </summary>
    public List<XlsxPhoneticText> PhoneticTexts;
    /// <summary>
    /// Stores typed phonetic display properties.
    /// </summary>
    public XlsxPhoneticProperties PhoneticProperties;
    /// <summary>
    /// Indicates whether typed phonetic display properties are present.
    /// </summary>
    public bool HasPhoneticProperties;
    /// <summary>
    /// Initializes a new cell value with its default state.
    /// </summary>
    public XlsxCellValue()
    {
        Kind = XlsxCellValueKind.Blank;
        Text = "";
        Number = 0;
        Decimal = 0;
        Boolean = false;
        DateTime = default(System.DateTime);
        Duration = default(System.TimeSpan);
        ExactText = "";
        HasExactText = false;
        ErrorKind = XlsxCellErrorKind.Null;
        HasErrorKind = false;
        SharedStringIndex = 0;
        Formula = "";
        CachedValue = "";
        CachedType = "";
        TypedCachedValue = null;
        HasTypedCachedValue = false;
        RawType = "";
        RawValue = "";
        RichTextRawXml = "";
        RichTextRuns = new List<XlsxSharedStringRichTextRun>();
        PhoneticTexts = new List<XlsxPhoneticText>();
        PhoneticProperties = new XlsxPhoneticProperties();
        HasPhoneticProperties = false;
    }

    /// <summary>
    /// Creates blank and initializes the fields required by the OOXML format.
    /// </summary>
    /// <returns>The cell value produced by the operation.</returns>
    internal static XlsxCellValue CreateBlank()
    {
        return new XlsxCellValue();
    }

    /// <summary>
    /// Creates number and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "number">The number value used by the operation.</param>
    /// <returns>The cell value produced by the operation.</returns>
    internal static XlsxCellValue CreateNumber(double number)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.Number;
        value.Number = number;
        value.Text = XlsxXmlValueWriter.WriteDouble(number);
        value.ExactText = value.Text;
        value.HasExactText = true;
        return value;
    }

    internal static XlsxCellValue CreateDecimal(decimal number)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.Decimal;
        value.Decimal = number;
        value.ExactText = number.ToString(CultureInfo.InvariantCulture);
        value.HasExactText = true;
        value.Text = value.ExactText;
        return value;
    }

    /// <summary>
    /// Creates text and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "text">The text value used by the operation.</param>
    /// <returns>The cell value produced by the operation.</returns>
    internal static XlsxCellValue CreateText(string? text)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.Text;
        value.Text = XlsxCellValueConverter.NormalizeText(text);
        return value;
    }

    /// <summary>
    /// Creates shared string and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "index">The index value used by the operation.</param>
    /// <returns>The cell value produced by the operation.</returns>
    internal static XlsxCellValue CreateSharedString(uint index)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.SharedString;
        value.SharedStringIndex = index;
        value.RawValue = XlsxXmlValueWriter.WriteUnsignedInteger(index);
        return value;
    }

    /// <summary>
    /// Creates inline string and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "text">The text value used by the operation.</param>
    /// <returns>The cell value produced by the operation.</returns>
    internal static XlsxCellValue CreateInlineString(string text)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.InlineString;
        value.Text = XlsxCellValueConverter.NormalizeText(text);
        return value;
    }

    /// <summary>
    /// Creates boolean and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "boolean">The boolean value used by the operation.</param>
    /// <returns>The cell value produced by the operation.</returns>
    internal static XlsxCellValue CreateBoolean(bool boolean)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.Boolean;
        value.Boolean = boolean;
        value.RawValue = XlsxXmlValueWriter.WriteBoolean(boolean);
        return value;
    }

    /// <summary>
    /// Creates error and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "error">The error value used by the operation.</param>
    /// <returns>The cell value produced by the operation.</returns>
    internal static XlsxCellValue CreateError(string error)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.Error;
        value.Text = XlsxCellValueConverter.NormalizeText(error);
        value.RawValue = value.Text;
        value.HasErrorKind = TryGetErrorKind(value.Text, value);
        return value;
    }

    internal static XlsxCellValue CreateTypedError(XlsxCellErrorKind error)
    {
        XlsxCellValue value = CreateError(GetErrorText(error));
        value.ErrorKind = error;
        value.HasErrorKind = true;
        return value;
    }

    /// <summary>
    /// Creates date time and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "dateTime">The date time value used by the operation.</param>
    /// <returns>The cell value produced by the operation.</returns>
    internal static XlsxCellValue CreateDateTime(System.DateTime dateTime)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.DateTime;
        value.DateTime = dateTime;
        value.RawValue = XlsxXmlValueWriter.WriteDateTime(dateTime);
        value.ExactText = value.RawValue;
        value.HasExactText = true;
        return value;
    }

    internal static XlsxCellValue CreateIsoDate(string isoDateText)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.DateTime;
        value.ExactText = XlsxCellValueConverter.NormalizeText(isoDateText);
        value.HasExactText = true;
        value.RawValue = value.ExactText;
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseDateTime(value.ExactText);
        if (parsed.IsValid)
        {
            value.DateTime = parsed.DateTimeValue;
        }
        return value;
    }

    internal static XlsxCellValue CreateDuration(System.TimeSpan duration)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.Duration;
        value.Duration = duration;
        decimal days = (decimal)duration.Ticks / 864000000000m;
        value.ExactText = days.ToString(CultureInfo.InvariantCulture);
        value.HasExactText = true;
        value.Text = value.ExactText;
        return value;
    }

    internal static XlsxCellValue CreateFormulaCachedValue(string formula, string cachedValue, string cachedType)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.FormulaCachedValue;
        value.Formula = XlsxCellValueConverter.NormalizeText(formula);
        value.CachedValue = XlsxCellValueConverter.NormalizeText(cachedValue);
        value.CachedType = XlsxCellValueConverter.NormalizeText(cachedType);
        value.Text = value.CachedValue;
        return value;
    }

    internal static XlsxCellValue CreateFormulaValue(string formula, XlsxCellValue cachedValue)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.FormulaCachedValue;
        value.Formula = XlsxCellValueConverter.NormalizeText(formula);
        value.TypedCachedValue = cachedValue;
        value.HasTypedCachedValue = cachedValue != null;
        return value;
    }

    /// <summary>
    /// Creates rich text and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "plainText">The plain text value used by the operation.</param>
    /// <param name = "rawXml">The raw xml value used by the operation.</param>
    /// <returns>The cell value produced by the operation.</returns>
    internal static XlsxCellValue CreateRichText(string plainText, string rawXml)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.RichText;
        value.Text = XlsxCellValueConverter.NormalizeText(plainText);
        value.RichTextRawXml = XlsxCellValueConverter.NormalizeText(rawXml);
        return value;
    }

    internal static XlsxCellValue CreateRichTextValue(XlsxInlineString inlineString)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.RichText;
        if (inlineString == null)
        {
            return value;
        }
        value.Text = XlsxCellValueConverter.NormalizeText(inlineString.Text);
        foreach (XlsxSharedStringRichTextRun run in inlineString.RichTextRuns)
        {
            value.RichTextRuns.Add(XlsxSharedStringTableWriter.CloneRun(run));
        }
        foreach (XlsxPhoneticText phoneticText in inlineString.PhoneticTexts)
        {
            value.PhoneticTexts.Add(XlsxSharedStringTableWriter.ClonePhoneticText(phoneticText));
        }
        value.PhoneticProperties = ClonePhoneticProperties(inlineString.PhoneticProperties);
        value.HasPhoneticProperties = inlineString.HasPhoneticProperties;
        return value;
    }

    internal static string GetErrorText(XlsxCellErrorKind error)
    {
        switch (error)
        {
            case XlsxCellErrorKind.Null: return "#NULL!";
            case XlsxCellErrorKind.DivisionByZero: return "#DIV/0!";
            case XlsxCellErrorKind.Value: return "#VALUE!";
            case XlsxCellErrorKind.Reference: return "#REF!";
            case XlsxCellErrorKind.Name: return "#NAME?";
            case XlsxCellErrorKind.Number: return "#NUM!";
            case XlsxCellErrorKind.NotAvailable: return "#N/A";
            case XlsxCellErrorKind.GettingData: return "#GETTING_DATA";
            case XlsxCellErrorKind.Spill: return "#SPILL!";
            case XlsxCellErrorKind.Calculation: return "#CALC!";
            case XlsxCellErrorKind.Field: return "#FIELD!";
            case XlsxCellErrorKind.Blocked: return "#BLOCKED!";
            case XlsxCellErrorKind.Unknown: return "#UNKNOWN!";
            case XlsxCellErrorKind.Connection: return "#CONNECT!";
            case XlsxCellErrorKind.Data: return "#DATA!";
            case XlsxCellErrorKind.Busy: return "#BUSY!";
            case XlsxCellErrorKind.Python: return "#PYTHON!";
            default: return "#VALUE!";
        }
    }

    internal static bool TryGetErrorKind(string text, XlsxCellValue value)
    {
        int error = 0;
        if (text == "#NULL!") { error = 0; }
        else if (text == "#DIV/0!") { error = 1; }
        else if (text == "#VALUE!") { error = 2; }
        else if (text == "#REF!") { error = 3; }
        else if (text == "#NAME?") { error = 4; }
        else if (text == "#NUM!") { error = 5; }
        else if (text == "#N/A") { error = 6; }
        else if (text == "#GETTING_DATA") { error = 7; }
        else if (text == "#SPILL!") { error = 8; }
        else if (text == "#CALC!") { error = 9; }
        else if (text == "#FIELD!") { error = 10; }
        else if (text == "#BLOCKED!") { error = 11; }
        else if (text == "#UNKNOWN!") { error = 12; }
        else if (text == "#CONNECT!") { error = 13; }
        else if (text == "#DATA!") { error = 14; }
        else if (text == "#BUSY!") { error = 15; }
        else if (text == "#PYTHON!") { error = 16; }
        else { return false; }
        value.ErrorKind = (XlsxCellErrorKind)error;
        return true;
    }

    internal static XlsxPhoneticProperties ClonePhoneticProperties(XlsxPhoneticProperties properties)
    {
        XlsxPhoneticProperties clone = new XlsxPhoneticProperties();
        if (properties == null)
        {
            return clone;
        }
        clone.FontId = properties.FontId;
        clone.HasFontId = properties.HasFontId;
        clone.Type = XlsxCellValueConverter.NormalizeText(properties.Type);
        clone.Alignment = XlsxCellValueConverter.NormalizeText(properties.Alignment);
        foreach (XlsxXmlAttribute attribute in properties.UnknownAttributes)
        {
            clone.UnknownAttributes.Add(attribute);
        }
        return clone;
    }

    internal static XlsxCellValue CreateUnknownRawValue(string rawType, string rawValue)
    {
        XlsxCellValue value = new XlsxCellValue();
        value.Kind = XlsxCellValueKind.UnknownRawValue;
        value.RawType = XlsxCellValueConverter.NormalizeText(rawType);
        value.RawValue = XlsxCellValueConverter.NormalizeText(rawValue);
        value.Text = value.RawValue;
        return value;
    }
}
