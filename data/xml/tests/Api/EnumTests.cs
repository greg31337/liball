using xml;

namespace xml.Tests;

[TestFixture]
public class EnumTests
{
    [Test]
    public void PublicEnumNamesAndIntegerValuesAreStable()
    {
        object[][] cases =
        [
            new object[] { typeof(XmlVersion), new string[] { "Xml10", "Xml11" }, new int[] { 0, 1 } },
            new object[] { typeof(XmlNodeKind), new string[] { "Document", "DocumentFragment", "Element", "Attribute", "Namespace", "Text", "CData", "EntityReference", "ProcessingInstruction", "Comment", "DocumentType" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } },
            new object[] { typeof(XmlEventKind), new string[] { "StartDocument", "EndDocument", "StartEntity", "EndEntity", "StartDtd", "EndDtd", "StartNamespace", "EndNamespace", "StartElement", "EndElement", "Text", "IgnorableWhitespace", "CData", "Comment", "ProcessingInstruction", "ElementDeclaration", "AttributeDeclaration", "EntityDeclaration", "NotationDeclaration", "SkippedEntity", "Diagnostic" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 } },
            new object[] { typeof(XmlInputKind), new string[] { "String", "CharacterMemory", "ByteMemory", "Stream", "TextReader" }, new int[] { 0, 1, 2, 3, 4 } },
            new object[] { typeof(XmlOutputKind), new string[] { "Stream", "TextWriter", "ByteSink" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XmlDiagnosticSeverity), new string[] { "Information", "Warning", "Error", "Fatal" }, new int[] { 0, 1, 2, 3 } },
            new object[] { typeof(XmlErrorCategory), new string[] { "General", "Xml", "Namespace", "Dtd", "Validation", "Resource", "Limit", "Include", "Serialization", "Canonicalization", "Schema", "XPathStatic", "XPathType", "XPathDynamic", "XsltStatic", "XsltType", "XsltDynamic" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 } },
            new object[] { typeof(XmlValidationMode), new string[] { "None", "Dtd", "Schema" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XmlDtdProcessing), new string[] { "Prohibit", "Ignore", "Parse" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XmlEntityHandling), new string[] { "Expand", "PreserveReferences" }, new int[] { 0, 1 } },
            new object[] { typeof(XmlNamespaceHandling), new string[] { "Disabled", "Namespaces10", "Namespaces11" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XmlStandalone), new string[] { "Unspecified", "Yes", "No" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XmlSpaceMode), new string[] { "Default", "Preserve" }, new int[] { 0, 1 } },
            new object[] { typeof(XmlParserStatus), new string[] { "Ready", "Running", "NeedInput", "Paused", "Completed", "Aborted", "Error", "Closed" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 } },
            new object[] { typeof(XmlHandlerAction), new string[] { "Continue", "Pause", "Abort" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XmlResourceKind), new string[] { "Document", "ExternalSubset", "GeneralEntity", "ParameterEntity", "XIncludeXml", "XIncludeText", "Catalog", "Schema", "Stylesheet", "Package", "UnparsedText", "Collection" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 } },
            new object[] { typeof(XmlResourceAccess), new string[] { "Denied", "Allowed" }, new int[] { 0, 1 } },
            new object[] { typeof(XmlCatalogEntryKind), new string[] { "Group", "Public", "System", "Uri", "RewriteSystem", "RewriteUri", "SystemSuffix", "UriSuffix", "DelegatePublic", "DelegateSystem", "DelegateUri", "NextCatalog" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 } },
            new object[] { typeof(XmlSerializationMethod), new string[] { "Xml", "Xhtml", "Html", "Text", "Json", "Adaptive", "Extension" }, new int[] { 0, 1, 2, 3, 4, 5, 6 } },
            new object[] { typeof(XmlCanonicalizationMethod), new string[] { "CanonicalXml11", "ExclusiveXml10", "CanonicalXml20Experimental" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XmlCharacterEscapeContext), new string[] { "Text", "AttributeDoubleQuoted", "AttributeSingleQuoted", "Comment", "ProcessingInstruction", "CData", "PublicIdentifier", "SystemLiteralDoubleQuoted", "SystemLiteralSingleQuoted", "XPathStringDoubleQuoted", "XPathStringSingleQuoted", "Uri", "Json" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } },
            new object[] { typeof(XmlInvalidCharacterPolicy), new string[] { "Error", "CharacterReference", "Replace" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XdmItemKind), new string[] { "Node", "Atomic", "Function", "Map", "Array" }, new int[] { 0, 1, 2, 3, 4 } },
            new object[] { typeof(XdmAtomicType), new string[] { "AnyAtomicType", "UntypedAtomic", "String", "NormalizedString", "Token", "Language", "Name", "NcName", "Nmtoken", "Id", "Idref", "Entity", "Boolean", "Decimal", "Integer", "NonPositiveInteger", "NegativeInteger", "Long", "Int", "Short", "Byte", "NonNegativeInteger", "UnsignedLong", "UnsignedInt", "UnsignedShort", "UnsignedByte", "PositiveInteger", "Float", "Double", "Duration", "YearMonthDuration", "DayTimeDuration", "DateTime", "DateTimeStamp", "Date", "Time", "GYearMonth", "GYear", "GMonthDay", "GMonth", "GDay", "HexBinary", "Base64Binary", "AnyUri", "QName", "Notation" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45 } },
            new object[] { typeof(XdmOccurrence), new string[] { "ExactlyOne", "ZeroOrOne", "ZeroOrMore", "OneOrMore" }, new int[] { 0, 1, 2, 3 } },
            new object[] { typeof(XPathLanguageVersion), new string[] { "XPath31" }, new int[] { 0 } },
            new object[] { typeof(XPathCompatibilityMode), new string[] { "Strict", "BackwardsCompatible" }, new int[] { 0, 1 } },
            new object[] { typeof(XmlSchemaValidity), new string[] { "NotKnown", "Valid", "Invalid" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XmlSchemaProcessContents), new string[] { "Strict", "Lax", "Skip" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XmlSchemaDerivationMethod), new string[] { "None", "Extension", "Restriction", "Substitution", "List", "Union", "All" }, new int[] { 0, 1, 2, 4, 8, 16, 31 } },
            new object[] { typeof(XsltConformanceLevel), new string[] { "Basic", "SchemaAware", "Streaming" }, new int[] { 0, 1, 2 } },
            new object[] { typeof(XsltComponentVisibility), new string[] { "Public", "Private", "Final", "Abstract", "Hidden" }, new int[] { 0, 1, 2, 3, 4 } },
            new object[] { typeof(XsltRecoveryPolicy), new string[] { "Strict", "Recover" }, new int[] { 0, 1 } },
            new object[] { typeof(XsltResultDisposition), new string[] { "Pending", "Commit", "Rollback", "Reject" }, new int[] { 0, 1, 2, 3 } },
            new object[] { typeof(XsltTraceEventKind), new string[] { "EnterTemplate", "ExitTemplate", "EnterFunction", "ExitFunction", "EnterInstruction", "ExitInstruction", "EnterMode", "ExitMode", "EnterComponent", "ExitComponent" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 } },
            new object[] { typeof(XsltStreamability), new string[] { "Unknown", "Streamable", "NotStreamable" }, new int[] { 0, 1, 2 } },
        ];

        foreach (object[] item in cases)
        {
            Type enumType = (Type)item[0];
            string[] expectedNames = (string[])item[1];
            int[] expectedValues = (int[])item[2];
            string[] actualNames = Enum.GetNames(enumType);
            Array actualValues = Enum.GetValues(enumType);
            Assert.That(actualNames, Is.EqualTo(expectedNames), enumType.Name);
            Assert.That(actualValues.Length, Is.EqualTo(expectedValues.Length), enumType.Name);
            for (int index = 0; index < actualValues.Length; index++)
            {
                Assert.That(Convert.ToInt32(actualValues.GetValue(index)), Is.EqualTo(expectedValues[index]), enumType.Name + "." + actualNames[index]);
            }
        }
    }

    [Test]
    public void EnumValueBagRoundTripsEveryEnumType()
    {
        XmlEnumValues values = new XmlEnumValues();
        values.Version = XmlVersion.Xml11;
        values.NodeKind = XmlNodeKind.DocumentType;
        values.EventKind = XmlEventKind.Diagnostic;
        values.InputKind = XmlInputKind.TextReader;
        values.OutputKind = XmlOutputKind.ByteSink;
        values.DiagnosticSeverity = XmlDiagnosticSeverity.Fatal;
        values.ErrorCategory = XmlErrorCategory.XsltDynamic;
        values.ValidationMode = XmlValidationMode.Schema;
        values.DtdProcessing = XmlDtdProcessing.Parse;
        values.EntityHandling = XmlEntityHandling.PreserveReferences;
        values.NamespaceHandling = XmlNamespaceHandling.Namespaces11;
        values.Standalone = XmlStandalone.No;
        values.SpaceMode = XmlSpaceMode.Preserve;
        values.ParserStatus = XmlParserStatus.Closed;
        values.HandlerAction = XmlHandlerAction.Abort;
        values.ResourceKind = XmlResourceKind.Collection;
        values.ResourceAccess = XmlResourceAccess.Allowed;
        values.CatalogEntryKind = XmlCatalogEntryKind.NextCatalog;
        values.SerializationMethod = XmlSerializationMethod.Extension;
        values.CanonicalizationMethod = XmlCanonicalizationMethod.CanonicalXml20Experimental;
        values.CharacterEscapeContext = XmlCharacterEscapeContext.Json;
        values.InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Replace;
        values.ItemKind = XdmItemKind.Array;
        values.AtomicType = XdmAtomicType.Notation;
        values.Occurrence = XdmOccurrence.OneOrMore;
        values.XPathVersion = XPathLanguageVersion.XPath31;
        values.XPathCompatibility = XPathCompatibilityMode.BackwardsCompatible;
        values.SchemaValidity = XmlSchemaValidity.Invalid;
        values.SchemaProcessContents = XmlSchemaProcessContents.Skip;
        values.SchemaDerivationMethod = XmlSchemaDerivationMethod.All;
        values.XsltConformance = XsltConformanceLevel.Streaming;
        values.XsltVisibility = XsltComponentVisibility.Hidden;
        values.XsltRecovery = XsltRecoveryPolicy.Recover;
        values.XsltResultDisposition = XsltResultDisposition.Reject;
        values.XsltTraceEventKind = XsltTraceEventKind.ExitComponent;
        values.XsltStreamability = XsltStreamability.NotStreamable;

        Assert.That(values.Version, Is.EqualTo(XmlVersion.Xml11));
        Assert.That(values.NodeKind, Is.EqualTo(XmlNodeKind.DocumentType));
        Assert.That(values.EventKind, Is.EqualTo(XmlEventKind.Diagnostic));
        Assert.That(values.InputKind, Is.EqualTo(XmlInputKind.TextReader));
        Assert.That(values.OutputKind, Is.EqualTo(XmlOutputKind.ByteSink));
        Assert.That(values.DiagnosticSeverity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        Assert.That(values.ErrorCategory, Is.EqualTo(XmlErrorCategory.XsltDynamic));
        Assert.That(values.ValidationMode, Is.EqualTo(XmlValidationMode.Schema));
        Assert.That(values.DtdProcessing, Is.EqualTo(XmlDtdProcessing.Parse));
        Assert.That(values.EntityHandling, Is.EqualTo(XmlEntityHandling.PreserveReferences));
        Assert.That(values.NamespaceHandling, Is.EqualTo(XmlNamespaceHandling.Namespaces11));
        Assert.That(values.Standalone, Is.EqualTo(XmlStandalone.No));
        Assert.That(values.SpaceMode, Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(values.ParserStatus, Is.EqualTo(XmlParserStatus.Closed));
        Assert.That(values.HandlerAction, Is.EqualTo(XmlHandlerAction.Abort));
        Assert.That(values.ResourceKind, Is.EqualTo(XmlResourceKind.Collection));
        Assert.That(values.ResourceAccess, Is.EqualTo(XmlResourceAccess.Allowed));
        Assert.That(values.CatalogEntryKind, Is.EqualTo(XmlCatalogEntryKind.NextCatalog));
        Assert.That(values.SerializationMethod, Is.EqualTo(XmlSerializationMethod.Extension));
        Assert.That(values.CanonicalizationMethod, Is.EqualTo(XmlCanonicalizationMethod.CanonicalXml20Experimental));
        Assert.That(values.CharacterEscapeContext, Is.EqualTo(XmlCharacterEscapeContext.Json));
        Assert.That(values.InvalidCharacterPolicy, Is.EqualTo(XmlInvalidCharacterPolicy.Replace));
        Assert.That(values.ItemKind, Is.EqualTo(XdmItemKind.Array));
        Assert.That(values.AtomicType, Is.EqualTo(XdmAtomicType.Notation));
        Assert.That(values.Occurrence, Is.EqualTo(XdmOccurrence.OneOrMore));
        Assert.That(values.XPathVersion, Is.EqualTo(XPathLanguageVersion.XPath31));
        Assert.That(values.XPathCompatibility, Is.EqualTo(XPathCompatibilityMode.BackwardsCompatible));
        Assert.That(values.SchemaValidity, Is.EqualTo(XmlSchemaValidity.Invalid));
        Assert.That(values.SchemaProcessContents, Is.EqualTo(XmlSchemaProcessContents.Skip));
        Assert.That(values.SchemaDerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.All));
        Assert.That(values.XsltConformance, Is.EqualTo(XsltConformanceLevel.Streaming));
        Assert.That(values.XsltVisibility, Is.EqualTo(XsltComponentVisibility.Hidden));
        Assert.That(values.XsltRecovery, Is.EqualTo(XsltRecoveryPolicy.Recover));
        Assert.That(values.XsltResultDisposition, Is.EqualTo(XsltResultDisposition.Reject));
        Assert.That(values.XsltTraceEventKind, Is.EqualTo(XsltTraceEventKind.ExitComponent));
        Assert.That(values.XsltStreamability, Is.EqualTo(XsltStreamability.NotStreamable));
    }

    [Test]
    public void EnumValueProviderContractReturnsTheSameValueBag()
    {
        XmlEnumValues expected = new XmlEnumValues();
        expected.Version = XmlVersion.Xml11;
        EnumValuesProvider provider = new EnumValuesProvider(expected);

        Assert.That(provider.GetXmlEnumValues(), Is.SameAs(expected));
    }

    private sealed class EnumValuesProvider : IXmlEnumValuesProvider
    {
        private readonly XmlEnumValues values;

        internal EnumValuesProvider(XmlEnumValues values)
        {
            this.values = values;
        }

        public XmlEnumValues GetXmlEnumValues()
        {
            return values;
        }
    }
}
