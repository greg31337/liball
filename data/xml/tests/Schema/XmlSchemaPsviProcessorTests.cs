using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaPsviProcessorTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";
    private const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    [Test]
    public void AnnotatesSelectedUnionMemberNormalizedAndActualValues()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='Choice'><xs:union memberTypes='xs:int xs:boolean'/></xs:simpleType>" +
            "<xs:element name='root' type='t:Choice'/>");
        XmlDocument document = Parse("<root xmlns='urn:test'>  01  </root>");

        XmlSchemaAssessmentResult result = Assess(compiled, document, Options(true, false));
        XmlSchemaTypeInfo information = document.DocumentElement!.TypeInfo!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(result.TypeAnnotations, Has.Length.EqualTo(result.Assessments.Length));
            Assert.That(information.TypeName!.LocalName, Is.EqualTo("Choice"));
            Assert.That(information.MemberTypeName!.LocalName, Is.EqualTo("int"));
            Assert.That(information.AtomicType, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(information.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(information.NormalizedValue, Is.EqualTo("01"));
            Assert.That(information.ActualValue!.CanonicalValue, Is.EqualTo("1"));
            Assert.That(information.ActualValues, Has.Length.EqualTo(1));
            Assert.That(information.ActualValues![0].TypeInfo, Is.SameAs(information));
        });
    }

    [Test]
    public void ProjectsListActualValuesIntoTheXdmTypedValue()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='Numbers'><xs:list itemType='xs:int'/></xs:simpleType>" +
            "<xs:element name='root' type='t:Numbers'/>");
        XmlDocument document = Parse("<root xmlns='urn:test'> 01   2 </root>");
        Assess(compiled, document, Options(true, false));

        XmlSchemaTypeInfo information = document.DocumentElement!.TypeInfo!;
        XdmSequence typed = new XdmNodeAccessor().GetXdmNodeTypedValue(new XdmNodeAccessor().WrapXmlNode(document.DocumentElement));

        Assert.Multiple(() =>
        {
            Assert.That(information.AtomicType, Is.EqualTo(XdmAtomicType.AnyAtomicType));
            Assert.That(information.ActualValue, Is.Null);
            Assert.That(information.ActualValues, Has.Length.EqualTo(2));
            Assert.That(typed.Count, Is.EqualTo(2));
            Assert.That(typed.AtomicValues!.Select(item => item.CanonicalValue), Is.EqualTo(new[] { "1", "2" }));
        });
    }

    [Test]
    public void RecordsSelectedAndComplexSimpleContentTypeDefinitions()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='Base'><xs:restriction base='xs:string'/></xs:simpleType>" +
            "<xs:simpleType name='Derived'><xs:restriction base='t:Base'><xs:pattern value='[A-Z]+'/></xs:restriction></xs:simpleType>" +
            "<xs:complexType name='Amount'><xs:simpleContent><xs:extension base='xs:decimal'/></xs:simpleContent></xs:complexType>" +
            "<xs:element name='root' type='t:Base'/><xs:element name='amount' type='t:Amount'/>");
        XmlDocument selectedDocument = Parse("<root xmlns='urn:test' xmlns:xsi='" + Xsi + "' xmlns:t='urn:test' xsi:type='t:Derived'>ABC</root>");
        XmlDocument amountDocument = Parse("<amount xmlns='urn:test'>01.50</amount>");

        Assess(compiled, selectedDocument, Options(true, false));
        Assess(compiled, amountDocument, Options(true, false));
        XmlSchemaTypeInfo selected = selectedDocument.DocumentElement!.TypeInfo!;
        XmlSchemaTypeInfo amount = amountDocument.DocumentElement!.TypeInfo!;

        Assert.Multiple(() =>
        {
            Assert.That(selected.TypeName!.LocalName, Is.EqualTo("Derived"));
            Assert.That(selected.AtomicType, Is.EqualTo(XdmAtomicType.String));
            Assert.That(amount.TypeName!.LocalName, Is.EqualTo("Amount"));
            Assert.That(amount.AtomicType, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(amount.ActualValue!.CanonicalValue, Is.EqualTo("1.5"));
        });
    }

    [Test]
    public void AppliesAndAnnotatesElementAndAttributeDefaults()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:complexType name='Container'><xs:sequence><xs:element name='count' type='xs:int' default='007'/></xs:sequence>" +
            "<xs:attribute name='code' type='xs:token' default='  A   B  '/><xs:attribute name='fixed' type='xs:int' fixed='1'/></xs:complexType>" +
            "<xs:element name='root' type='t:Container'/>");
        XmlDocument document = Parse("<root xmlns='urn:test' fixed='01'><count/></root>");

        XmlSchemaAssessmentResult result = Assess(compiled, document, Options(true, true));
        XmlNode root = document.DocumentElement!;
        XmlNode code = root.Attributes!.Single(item => item.Name!.LocalName == "code");
        XmlNode count = root.Children!.Single(item => item.Kind == XmlNodeKind.Element);

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(code.Value, Is.EqualTo("A B"));
            Assert.That(code.TypeInfo!.IsDefaulted, Is.True);
            Assert.That(code.TypeInfo.NormalizedValue, Is.EqualTo("A B"));
            Assert.That(count.TypeInfo!.IsDefaulted, Is.True);
            Assert.That(count.TypeInfo.NormalizedValue, Is.EqualTo("007"));
            Assert.That(count.TypeInfo.ActualValue!.CanonicalValue, Is.EqualTo("7"));
            Assert.That(count.Children!.Single().Value, Is.EqualTo("007"));
            Assert.That(root.Attributes!.Single(item => item.Name!.LocalName == "fixed").TypeInfo!.IsDefaulted, Is.False);
        });
    }

    [Test]
    public void RetainsLogicalElementDefaultsWithoutMutatingWhenAugmentationIsDisabled()
    {
        CompiledXmlSchemaSet compiled = Compile("<xs:element name='root' type='xs:int' default='9'/>");
        XmlDocument document = Parse("<root xmlns='urn:test'/>");

        XmlSchemaAssessmentResult result = Assess(compiled, document, Options(true, false));
        XmlSchemaTypeInfo information = document.DocumentElement!.TypeInfo!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(document.DocumentElement.Children, Is.Empty);
            Assert.That(information.IsDefaulted, Is.True);
            Assert.That(information.NormalizedValue, Is.EqualTo("9"));
            Assert.That(information.ActualValue!.CanonicalValue, Is.EqualTo("9"));
        });
    }

    [Test]
    public void ComparesFixedConstraintsInTheTypeValueSpace()
    {
        CompiledXmlSchemaSet compiled = Compile("<xs:element name='root' type='xs:int' fixed='1'/>");
        XmlSchemaAssessmentResult equal = Assess(compiled, Parse("<root xmlns='urn:test'>01</root>"), Options(false, false));
        XmlSchemaAssessmentResult different = Assess(compiled, Parse("<root xmlns='urn:test'>2</root>"), Options(false, false));

        Assert.Multiple(() =>
        {
            Assert.That(equal.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(different.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(different.Diagnostics.Any(item => item.Message!.Contains("fixed value", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void AnnotatesNilledInvalidAndSkippedValidityOutcomes()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:complexType name='Open'><xs:sequence><xs:any processContents='skip' minOccurs='0' maxOccurs='unbounded'/></xs:sequence></xs:complexType>" +
            "<xs:element name='root' type='t:Open' nillable='true'/>" +
            "<xs:element name='number' type='xs:int'/>");
        XmlDocument nilledDocument = Parse("<root xmlns='urn:test' xmlns:xsi='" + Xsi + "' xsi:nil='true'/>");
        XmlDocument skippedDocument = Parse("<root xmlns='urn:test'><foreign xmlns='urn:foreign'/></root>");
        XmlDocument invalidDocument = Parse("<number xmlns='urn:test'>not-an-int</number>");

        XmlSchemaAssessmentResult nilled = Assess(compiled, nilledDocument, Options(true, false));
        XmlSchemaAssessmentResult skipped = Assess(compiled, skippedDocument, Options(true, false));
        XmlSchemaAssessmentResult invalid = Assess(compiled, invalidDocument, Options(true, false));

        Assert.Multiple(() =>
        {
            Assert.That(nilled.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(nilledDocument.DocumentElement!.TypeInfo!.IsNilled, Is.True);
            Assert.That(new XdmNodeAccessor().GetXdmNodeTypedValue(new XdmNodeAccessor().WrapXmlNode(nilledDocument.DocumentElement)).Count, Is.Zero);
            Assert.That(skipped.TypeAnnotations.Single(item => item.Validity == XmlSchemaValidity.NotKnown).TypeName, Is.Null);
            Assert.That(skippedDocument.DocumentElement!.Children![0].TypeInfo!.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(invalid.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(invalidDocument.DocumentElement!.TypeInfo!.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(invalidDocument.DocumentElement.TypeInfo.ActualValues, Is.Null);
        });
    }

    [Test]
    public void ContributesSchemaIdsAndIdReferencesToXdmAndDocumentLookup()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:complexType name='Identifiers'><xs:attribute name='id' type='xs:ID'/><xs:attribute name='references' type='xs:IDREFS'/></xs:complexType>" +
            "<xs:element name='root' type='t:Identifiers'/>");
        XmlDocument document = Parse("<root xmlns='urn:test' id='target' references='target other'/>");
        Assess(compiled, document, Options(true, false));
        XmlNode root = document.DocumentElement!;
        XmlNode id = root.Attributes!.Single(item => item.Name!.LocalName == "id");
        XmlNode references = root.Attributes!.Single(item => item.Name!.LocalName == "references");
        IdIndex index = new IdIndex();
        index.BuildXmlIdentifierIndex(document);

        Assert.Multiple(() =>
        {
            Assert.That(id.TypeInfo!.IsId, Is.True);
            Assert.That(id.TypeInfo.IsIdReference, Is.False);
            Assert.That(references.TypeInfo!.IsId, Is.False);
            Assert.That(references.TypeInfo.IsIdReference, Is.True);
            Assert.That(references.TypeInfo.ActualValues, Has.Length.EqualTo(2));
            Assert.That(index.FindIndexedXmlNode("target"), Is.SameAs(root));
        });
    }

    [Test]
    public void CollectsAnnotationsWithoutAttachingThemWhenTreeAnnotationIsDisabled()
    {
        CompiledXmlSchemaSet compiled = Compile("<xs:element name='root' type='xs:boolean'/>");
        XmlDocument document = Parse("<root xmlns='urn:test'>true</root>");

        XmlSchemaAssessmentResult result = Assess(compiled, document, Options(false, false));

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement!.TypeInfo, Is.Null);
            Assert.That(result.TypeAnnotations, Has.Length.EqualTo(1));
            Assert.That(result.TypeAnnotations[0].ActualValue!.BooleanValue, Is.True);
        });
    }

    private static XmlSchemaValidationOptions Options(bool annotate, bool defaults)
    {
        return new XmlSchemaValidationOptions { AnnotateTree = annotate, ApplyDefaultValues = defaults };
    }

    private static XmlSchemaAssessmentResult Assess(CompiledXmlSchemaSet compiled, XmlDocument document, XmlSchemaValidationOptions options)
    {
        return new XmlSchemaAssessmentEngine(compiled, options, new RejectingResolver()).AssessXmlSchemaDocument(document);
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = "https://example.test/value.xml" }, options, null, null, null);
    }

    private static CompiledXmlSchemaSet Compile(string body)
    {
        string schema = "<xs:schema xmlns:xs='" + Xsd + "' xmlns:t='urn:test' targetNamespace='urn:test' elementFormDefault='qualified'>" + body + "</xs:schema>";
        XmlSchemaSet result = new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = schema, BaseUri = "https://example.test/schema.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied, AllowedSchemes = [] } },
            new RejectingResolver());
        return (CompiledXmlSchemaSet)result.CompiledSet!;
    }

    private sealed class RejectingResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            throw new InvalidOperationException("No external resource was expected.");
        }
    }
}
