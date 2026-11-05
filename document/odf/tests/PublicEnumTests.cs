namespace odf.Tests;

public class PublicEnumTests
{
    [Test]
    public void EnumMembersHaveStableInt32Values()
    {
        AssertEnum(typeof(OdfVersion), ["Unknown", "Version10", "Version11", "Version12", "Version13", "Version14", "Future"]);
        AssertEnum(typeof(OdfConformanceClass), ["Unknown", "Strict", "Conforming", "Extended"]);
        AssertEnum(typeof(OdfDocumentKind), ["Unknown", "Text", "TextTemplate", "TextMaster", "TextMasterTemplate", "Spreadsheet", "SpreadsheetTemplate", "Presentation", "PresentationTemplate", "Drawing", "DrawingTemplate", "Chart", "ChartTemplate", "Formula", "FormulaTemplate", "Image", "ImageTemplate", "Database"]);
        AssertEnum(typeof(OdfPackageKind), ["Unknown", "Package", "Flat"]);
        AssertEnum(typeof(OdfCompressionMethod), ["Unknown", "Stored", "Deflated", "Unsupported"]);
        AssertEnum(typeof(OdfNodeKind), ["Unknown", "Element", "Text", "Comment", "ProcessingInstruction"]);
        AssertEnum(typeof(OdfStreamEventKind), ["Unknown", "PackageEntryStart", "PackageEntryEnd", "ElementStart", "ElementEnd", "Text", "Comment", "ProcessingInstruction", "EndOfStream"]);
        AssertEnum(typeof(OdfValueKind), ["Empty", "String", "Float", "Decimal", "Percentage", "Currency", "Date", "DateTime", "Time", "Duration", "Boolean", "Error", "Complex", "Reference", "Array"]);
        AssertEnum(typeof(OdfValueProvenance), ["Parsed", "Computed", "Defaulted", "Inherited", "Foreign", "InvalidPreserved", "Synthesized"]);
        AssertEnum(typeof(OdfUnitKind), ["Unknown", "Unitless", "Millimetre", "Centimetre", "Inch", "Point", "Pica", "Pixel", "Em", "Ex", "Percentage", "Degree", "Radian", "Grad"]);
        AssertEnum(typeof(OdfStyleFamily), ["Unknown", "Paragraph", "Text", "Section", "Table", "TableColumn", "TableRow", "TableCell", "Graphic", "Chart", "DrawingPage", "Presentation", "Ruby", "List"]);
        AssertEnum(typeof(OdfFormulaConformance), ["Unknown", "Small", "Medium", "Large"]);
        AssertEnum(typeof(OdfFormulaTokenKind), ["Unknown", "End", "Whitespace", "Number", "Percentage", "String", "Boolean", "Error", "Identifier", "Function", "CellReference", "RangeReference", "ExternalReference", "NamedExpression", "LeftParenthesis", "RightParenthesis", "LeftBrace", "RightBrace", "Separator", "ArrayRowSeparator", "Prefix", "Operator"]);
        AssertEnum(typeof(OdfFormulaNodeKind), ["Unknown", "Literal", "Error", "Name", "Reference", "Range", "ExternalReference", "UnaryOperation", "BinaryOperation", "FunctionCall", "Array", "Intersection", "ReferenceConcatenation"]);
        AssertEnum(typeof(OdfFormulaError), ["None", "Null", "DivisionByZero", "Value", "Reference", "Name", "Number", "NotAvailable", "UnsupportedFunction", "CircularReference"]);
        AssertEnum(typeof(OdfDrawingKind), ["Unknown", "Page", "Layer", "Frame", "Group", "Rectangle", "Ellipse", "Line", "Polyline", "Polygon", "RegularPolygon", "Path", "Connector", "Measure", "Caption", "Control", "TextBox", "CustomShape", "Scene3D", "Cube", "Sphere", "Rotate", "Extrude", "Image", "Media", "EmbeddedObject"]);
        AssertEnum(typeof(OdfChartKind), ["Unknown", "Bar", "Line", "Area", "Pie", "Ring", "Scatter", "Radar", "FilledRadar", "Stock", "Bubble", "Surface", "Gantt"]);
        AssertEnum(typeof(OdfAnimationKind), ["Unknown", "Sequence", "Parallel", "Iterate", "Animate", "AnimateColor", "AnimateMotion", "AnimateTransform", "Set", "Command", "Audio", "TransitionFilter"]);
        AssertEnum(typeof(OdfTransitionKind), ["Unknown", "None", "Fade", "BarWipe", "BoxWipe", "FourBoxWipe", "BarnDoorWipe", "DiagonalWipe", "BowTieWipe", "MiscDiagonalWipe", "VeeWipe", "BarnVeeWipe", "ZigZagWipe", "BarnZigZagWipe", "IrisWipe", "TriangleWipe", "ArrowHeadWipe", "PentagonWipe", "HexagonWipe", "EllipseWipe", "EyeWipe", "RoundRectWipe", "StarWipe", "MiscShapeWipe", "ClockWipe", "PinWheelWipe", "SingleSweepWipe", "FanWipe", "DoubleFanWipe", "DoubleSweepWipe", "SaloonDoorWipe", "WindshieldWipe", "SnakeWipe", "SpiralWipe", "ParallelSnakesWipe", "BoxSnakesWipe", "WaterfallWipe", "PushWipe", "SlideWipe", "SweepWipe"]);
        AssertEnum(typeof(OdfValidationSeverity), ["Information", "Warning", "Error", "Fatal"]);
        AssertEnum(typeof(OdfValidationMode), ["Strict", "Complete", "Recovery", "PackageOnly", "XmlOnly", "SemanticOnly", "Profile"]);
        AssertEnum(typeof(OdfEncryptionMethod), ["Unknown", "None", "BlowfishCfb", "Aes128Cbc", "Aes192Cbc", "Aes256Cbc", "Unsupported"]);
        AssertEnum(typeof(OdfSignatureStatus), ["Unknown", "NotSigned", "NotChecked", "Valid", "Invalid", "UnsupportedAlgorithm", "MissingReference", "DigestMismatch", "CertificateInvalid", "Error"]);
        AssertEnum(typeof(OdfChangeKind), ["Unknown", "Insertion", "Deletion", "FormatChange"]);
        AssertEnum(typeof(OdfAnnotationState), ["Unknown", "Open", "Resolved"]);
        AssertEnum(typeof(OdfTextFieldKind), ["Unknown", "PageNumber", "PageCount", "Date", "Time", "AuthorName", "Sender", "FileName", "TemplateName", "Chapter", "WordCount", "CharacterCount", "ParagraphCount", "TableCount", "ImageCount", "ObjectCount", "Variable", "UserField", "Sequence", "Expression", "Placeholder", "Input", "ConditionalText", "HiddenText", "HiddenParagraph", "Database", "Reference", "SheetName", "CellAddress", "Metadata"]);
        AssertEnum(typeof(OdfFormControlKind), ["Unknown", "Form", "Button", "Text", "TextArea", "CheckBox", "RadioButton", "ListBox", "ComboBox", "ValueRange", "Image", "File", "Date", "Time", "Frame", "Hidden", "Label", "FormattedText", "Number", "Password", "ImageFrame", "Grid", "GenericControl", "Column"]);
        AssertEnum(typeof(OdfFormReferenceKind), ["Unknown", "LabelTarget", "DrawingControl"]);
        AssertEnum(typeof(OdfDatabaseConnectionKind), ["Unknown", "Resource", "File", "Server"]);
        AssertEnum(typeof(OdfDatabaseComponentKind), ["Unknown", "Form", "Report", "Collection"]);
        AssertEnum(typeof(OdfSettingKind), ["Unknown", "Item", "Set", "IndexedMap", "NamedMap", "MapEntry"]);
        AssertEnum(typeof(OdfSettingScope), ["Unknown", "Portable", "View"]);
        AssertEnum(typeof(OdfSettingValueKind), ["Unknown", "Boolean", "Short", "Integer", "Long", "Double", "String", "DateTime", "Base64Binary"]);
        AssertEnum(typeof(OdfScriptKind), ["Unknown", "Embedded", "PackageEntry"]);
        AssertEnum(typeof(OdfEventListenerKind), ["Unknown", "Script", "Presentation"]);
        AssertEnum(typeof(OdfActiveContentClassification), ["Unknown", "Inert", "EmbeddedCode", "MacroInvocation", "LinkedScript", "ExternalReference", "PresentationAction"]);
        AssertEnum(typeof(OdfEditStatus), ["Success", "NoChange", "Conflict", "ValidationFailed", "Cancelled", "Failed"]);
    }

    private static void AssertEnum(Type enumType, string[] expectedNames)
    {
        Assert.That(Enum.GetUnderlyingType(enumType), Is.EqualTo(typeof(int)), enumType.Name);
        Assert.That(Enum.GetNames(enumType), Is.EqualTo(expectedNames), enumType.Name);

        Array values = Enum.GetValues(enumType);
        for (int index = 0; index < values.Length; index++)
        {
            Assert.That(Convert.ToInt32(values.GetValue(index)), Is.EqualTo(index), enumType.Name + "." + expectedNames[index]);
        }
    }
}
