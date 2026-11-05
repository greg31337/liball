using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Represents worksheet outline display and summary settings.
/// </summary>
public class XlsxWorksheetOutlineProperties
{
    /// <summary>Stores whether styles are applied to outlined rows and columns.</summary>
    public bool ApplyStyles;
    /// <summary>Indicates whether apply styles is present.</summary>
    public bool HasApplyStyles;
    /// <summary>Stores whether summary rows appear below detail rows.</summary>
    public bool SummaryBelow;
    /// <summary>Indicates whether summary below is present.</summary>
    public bool HasSummaryBelow;
    /// <summary>Stores whether summary columns appear to the right of detail columns.</summary>
    public bool SummaryRight;
    /// <summary>Indicates whether summary right is present.</summary>
    public bool HasSummaryRight;
    /// <summary>Stores whether outline symbols are displayed.</summary>
    public bool ShowOutlineSymbols;
    /// <summary>Indicates whether show outline symbols is present.</summary>
    public bool HasShowOutlineSymbols;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Initializes worksheet outline properties.</summary>
    public XlsxWorksheetOutlineProperties()
    {
        ApplyStyles = false;
        HasApplyStyles = false;
        SummaryBelow = true;
        HasSummaryBelow = false;
        SummaryRight = true;
        HasSummaryRight = false;
        ShowOutlineSymbols = true;
        HasShowOutlineSymbols = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}

/// <summary>
/// Represents worksheet page-setup properties stored under sheet properties.
/// </summary>
public class XlsxWorksheetPageSetupProperties
{
    /// <summary>Stores whether automatic page breaks are enabled.</summary>
    public bool AutoPageBreaks;
    /// <summary>Indicates whether automatic page breaks is present.</summary>
    public bool HasAutoPageBreaks;
    /// <summary>Stores whether fit-to-page behavior is enabled.</summary>
    public bool FitToPage;
    /// <summary>Indicates whether fit to page is present.</summary>
    public bool HasFitToPage;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Initializes worksheet page-setup properties.</summary>
    public XlsxWorksheetPageSetupProperties()
    {
        AutoPageBreaks = false;
        HasAutoPageBreaks = false;
        FitToPage = false;
        HasFitToPage = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}

/// <summary>
/// Represents one input cell in a worksheet scenario.
/// </summary>
public class XlsxWorksheetScenarioInputCell
{
    /// <summary>Stores the input cell reference.</summary>
    public string Reference;
    /// <summary>Stores the scenario value for the input cell.</summary>
    public string Value;
    /// <summary>Stores the number format identifier.</summary>
    public uint NumberFormatId;
    /// <summary>Indicates whether number format id is present.</summary>
    public bool HasNumberFormatId;
    /// <summary>Stores whether the input was deleted.</summary>
    public bool Deleted;
    /// <summary>Indicates whether deleted is present.</summary>
    public bool HasDeleted;
    /// <summary>Stores whether the input was undone.</summary>
    public bool Undone;
    /// <summary>Indicates whether undone is present.</summary>
    public bool HasUndone;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Initializes a worksheet scenario input cell.</summary>
    public XlsxWorksheetScenarioInputCell()
    {
        Reference = "";
        Value = "";
        NumberFormatId = 0;
        HasNumberFormatId = false;
        Deleted = false;
        HasDeleted = false;
        Undone = false;
        HasUndone = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}

/// <summary>
/// Represents a named worksheet scenario.
/// </summary>
public class XlsxWorksheetScenario
{
    /// <summary>Stores the scenario name.</summary>
    public string Name;
    /// <summary>Stores the scenario comment.</summary>
    public string Comment;
    /// <summary>Stores the scenario user name.</summary>
    public string User;
    /// <summary>Stores whether the scenario is locked.</summary>
    public bool Locked;
    /// <summary>Indicates whether locked is present.</summary>
    public bool HasLocked;
    /// <summary>Stores whether the scenario is hidden.</summary>
    public bool Hidden;
    /// <summary>Indicates whether hidden is present.</summary>
    public bool HasHidden;
    /// <summary>Contains scenario input cells.</summary>
    public List<XlsxWorksheetScenarioInputCell> InputCells;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Contains preserved unknown child XML.</summary>
    public List<string> UnknownXml;
    /// <summary>Initializes a worksheet scenario.</summary>
    public XlsxWorksheetScenario()
    {
        Name = "";
        Comment = "";
        User = "";
        Locked = false;
        HasLocked = false;
        Hidden = false;
        HasHidden = false;
        InputCells = new List<XlsxWorksheetScenarioInputCell>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}

/// <summary>
/// Represents the worksheet scenario collection and selection state.
/// </summary>
public class XlsxWorksheetScenarios
{
    /// <summary>Stores the current scenario index.</summary>
    public uint Current;
    /// <summary>Indicates whether current is present.</summary>
    public bool HasCurrent;
    /// <summary>Stores the shown scenario index.</summary>
    public uint Show;
    /// <summary>Indicates whether show is present.</summary>
    public bool HasShow;
    /// <summary>Stores the scenario input-cell sequence reference.</summary>
    public string SquareReference;
    /// <summary>Contains scenarios.</summary>
    public List<XlsxWorksheetScenario> Items;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Contains preserved unknown child XML.</summary>
    public List<string> UnknownXml;
    /// <summary>Initializes a worksheet scenario collection.</summary>
    public XlsxWorksheetScenarios()
    {
        Current = 0;
        HasCurrent = false;
        Show = 0;
        HasShow = false;
        SquareReference = "";
        Items = new List<XlsxWorksheetScenario>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}

/// <summary>
/// Represents a custom worksheet view.
/// </summary>
public class XlsxWorksheetCustomView
{
    /// <summary>Stores the required custom-view identifier.</summary>
    public string Guid;
    /// <summary>Stores the view scale percentage.</summary>
    public uint Scale;
    /// <summary>Indicates whether scale is present.</summary>
    public bool HasScale;
    /// <summary>Stores the view color identifier.</summary>
    public uint ColorId;
    /// <summary>Indicates whether color id is present.</summary>
    public bool HasColorId;
    /// <summary>Stores whether formulas are shown.</summary>
    public bool ShowFormulas;
    /// <summary>Indicates whether show formulas is present.</summary>
    public bool HasShowFormulas;
    /// <summary>Stores whether grid lines are shown.</summary>
    public bool ShowGridLines;
    /// <summary>Indicates whether show grid lines is present.</summary>
    public bool HasShowGridLines;
    /// <summary>Stores whether row and column headers are shown.</summary>
    public bool ShowRowColumnHeaders;
    /// <summary>Indicates whether show row and column headers is present.</summary>
    public bool HasShowRowColumnHeaders;
    /// <summary>Stores whether page breaks are shown.</summary>
    public bool ShowPageBreaks;
    /// <summary>Indicates whether show page breaks is present.</summary>
    public bool HasShowPageBreaks;
    /// <summary>Stores preserved full XML for unsupported custom-view children.</summary>
    public string RawXml;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Initializes a custom worksheet view.</summary>
    public XlsxWorksheetCustomView()
    {
        Guid = "";
        Scale = 0;
        HasScale = false;
        ColorId = 0;
        HasColorId = false;
        ShowFormulas = false;
        HasShowFormulas = false;
        ShowGridLines = true;
        HasShowGridLines = false;
        ShowRowColumnHeaders = true;
        HasShowRowColumnHeaders = false;
        ShowPageBreaks = false;
        HasShowPageBreaks = false;
        RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}

/// <summary>
/// Represents an ignored-error rule applied to worksheet cells.
/// </summary>
public class XlsxWorksheetIgnoredError
{
    /// <summary>Stores the whitespace-separated target ranges.</summary>
    public string SquareReference;
    /// <summary>Stores whether evaluation errors are ignored.</summary>
    public bool EvaluationError;
    /// <summary>Indicates whether evaluation error is present.</summary>
    public bool HasEvaluationError;
    /// <summary>Stores whether two-digit years are ignored.</summary>
    public bool TwoDigitTextYear;
    /// <summary>Indicates whether two-digit year is present.</summary>
    public bool HasTwoDigitTextYear;
    /// <summary>Stores whether numbers stored as text are ignored.</summary>
    public bool NumberStoredAsText;
    /// <summary>Indicates whether number stored as text is present.</summary>
    public bool HasNumberStoredAsText;
    /// <summary>Stores whether inconsistent formulas are ignored.</summary>
    public bool Formula;
    /// <summary>Indicates whether formula is present.</summary>
    public bool HasFormula;
    /// <summary>Stores whether formula-range errors are ignored.</summary>
    public bool FormulaRange;
    /// <summary>Indicates whether formula range is present.</summary>
    public bool HasFormulaRange;
    /// <summary>Stores whether unlocked formulas are ignored.</summary>
    public bool UnlockedFormula;
    /// <summary>Indicates whether unlocked formula is present.</summary>
    public bool HasUnlockedFormula;
    /// <summary>Stores whether empty-cell references are ignored.</summary>
    public bool EmptyCellReference;
    /// <summary>Indicates whether empty-cell reference is present.</summary>
    public bool HasEmptyCellReference;
    /// <summary>Stores whether list-data-validation errors are ignored.</summary>
    public bool ListDataValidation;
    /// <summary>Indicates whether list data validation is present.</summary>
    public bool HasListDataValidation;
    /// <summary>Stores whether calculated-column errors are ignored.</summary>
    public bool CalculatedColumn;
    /// <summary>Indicates whether calculated column is present.</summary>
    public bool HasCalculatedColumn;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Initializes an ignored-error rule.</summary>
    public XlsxWorksheetIgnoredError()
    {
        SquareReference = "";
        EvaluationError = false;
        HasEvaluationError = false;
        TwoDigitTextYear = false;
        HasTwoDigitTextYear = false;
        NumberStoredAsText = false;
        HasNumberStoredAsText = false;
        Formula = false;
        HasFormula = false;
        FormulaRange = false;
        HasFormulaRange = false;
        UnlockedFormula = false;
        HasUnlockedFormula = false;
        EmptyCellReference = false;
        HasEmptyCellReference = false;
        ListDataValidation = false;
        HasListDataValidation = false;
        CalculatedColumn = false;
        HasCalculatedColumn = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}

/// <summary>
/// Represents one smart-tag property.
/// </summary>
public class XlsxWorksheetSmartTagProperty
{
    /// <summary>Stores the property key.</summary>
    public string Key;
    /// <summary>Stores the property value.</summary>
    public string Value;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Initializes a smart-tag property.</summary>
    public XlsxWorksheetSmartTagProperty()
    {
        Key = "";
        Value = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}

/// <summary>
/// Represents a smart tag attached to a worksheet cell.
/// </summary>
public class XlsxWorksheetSmartTag
{
    /// <summary>Stores the target cell reference.</summary>
    public string Reference;
    /// <summary>Stores the smart-tag type identifier.</summary>
    public uint TypeId;
    /// <summary>Indicates whether type id is present.</summary>
    public bool HasTypeId;
    /// <summary>Stores whether the tag is deleted.</summary>
    public bool Deleted;
    /// <summary>Indicates whether deleted is present.</summary>
    public bool HasDeleted;
    /// <summary>Stores whether the tag is XML based.</summary>
    public bool XmlBased;
    /// <summary>Indicates whether XML based is present.</summary>
    public bool HasXmlBased;
    /// <summary>Contains smart-tag properties.</summary>
    public List<XlsxWorksheetSmartTagProperty> Properties;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Contains preserved unknown child XML.</summary>
    public List<string> UnknownXml;
    /// <summary>Initializes a worksheet smart tag.</summary>
    public XlsxWorksheetSmartTag()
    {
        Reference = "";
        TypeId = 0;
        HasTypeId = false;
        Deleted = false;
        HasDeleted = false;
        XmlBased = false;
        HasXmlBased = false;
        Properties = new List<XlsxWorksheetSmartTagProperty>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}

/// <summary>
/// Represents an item published from a worksheet to a web destination.
/// </summary>
public class XlsxWorksheetWebPublishItem
{
    /// <summary>Stores the publish item identifier.</summary>
    public uint Id;
    /// <summary>Indicates whether id is present.</summary>
    public bool HasId;
    /// <summary>Stores the HTML division identifier.</summary>
    public string DivisionId;
    /// <summary>Stores the source type.</summary>
    public string SourceType;
    /// <summary>Stores the source cell or range reference.</summary>
    public string SourceReference;
    /// <summary>Stores the source object name.</summary>
    public string SourceObject;
    /// <summary>Stores the destination file.</summary>
    public string DestinationFile;
    /// <summary>Stores the published title.</summary>
    public string Title;
    /// <summary>Stores whether the item is automatically republished.</summary>
    public bool AutoRepublish;
    /// <summary>Indicates whether automatic republish is present.</summary>
    public bool HasAutoRepublish;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Initializes a web-publish item.</summary>
    public XlsxWorksheetWebPublishItem()
    {
        Id = 0;
        HasId = false;
        DivisionId = "";
        SourceType = "";
        SourceReference = "";
        SourceObject = "";
        DestinationFile = "";
        Title = "";
        AutoRepublish = false;
        HasAutoRepublish = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}

/// <summary>
/// Contains the result of a typed worksheet metadata mutation.
/// </summary>
public class XlsxWorksheetMetadataResult
{
    /// <summary>Indicates whether the operation completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains validation diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the affected scenario.</summary>
    public XlsxWorksheetScenario? Scenario;
    /// <summary>Stores the affected custom view.</summary>
    public XlsxWorksheetCustomView? CustomView;
    /// <summary>Stores the affected ignored-error rule.</summary>
    public XlsxWorksheetIgnoredError? IgnoredError;
    /// <summary>Stores the affected smart tag.</summary>
    public XlsxWorksheetSmartTag? SmartTag;
    /// <summary>Stores the affected web-publish item.</summary>
    public XlsxWorksheetWebPublishItem? WebPublishItem;
    /// <summary>Initializes a worksheet metadata result.</summary>
    public XlsxWorksheetMetadataResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Scenario = null;
        CustomView = null;
        IgnoredError = null;
        SmartTag = null;
        WebPublishItem = null;
    }

    internal void AddError(string code, string message)
    {
        Validation.AddError(code, message);
        IsValid = Validation.IsValid;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
