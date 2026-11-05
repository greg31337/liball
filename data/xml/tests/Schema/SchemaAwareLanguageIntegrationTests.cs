using xml;

namespace xml.Tests;

[TestFixture]
public class SchemaAwareLanguageIntegrationTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void XPathSchemaTestsUseDeclarationsSubstitutionGroupsAndDerivedTypes()
    {
        XmlSchemaSet schemaSet = CompileSchema();
        XmlDocument memberDocument = Validate(schemaSet, "<member xmlns='urn:test' xmlns:t='urn:test' t:code='7' t:extra='x'/>");
        XmlDocument localDocument = Validate(schemaSet, "<local xmlns='urn:test' xmlns:t='urn:test' t:code='7'/>");
        XmlNode member = memberDocument.DocumentElement!;
        XmlNode local = localDocument.DocumentElement!;
        XmlNode code = member.Attributes!.Single(value => value.Name!.LocalName == "code");
        XmlNode localCode = local.Attributes!.Single(value => value.Name!.LocalName == "code");
        XPathSchemaNodeTest tests = new XPathSchemaNodeTest(schemaSet);
        XmlNode wrongType = new XmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = Name("member"),
            TypeInfo = new XmlSchemaTypeInfo
            {
                Validity = XmlSchemaValidity.Valid,
                Declaration = member.TypeInfo!.Declaration,
                TypeDefinition = code.TypeInfo!.TypeDefinition
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(tests.MatchesXPathSchemaElement(member, Name("member")), Is.True);
            Assert.That(tests.MatchesXPathSchemaElement(member, Name("head")), Is.True);
            Assert.That(tests.MatchesXPathSchemaElement(local, Name("local")), Is.True);
            Assert.That(tests.MatchesXPathSchemaElement(wrongType, Name("member")), Is.False);
            Assert.That(tests.MatchesXPathSchemaAttribute(code, Name("code")), Is.True);
            Assert.That(tests.MatchesXPathSchemaAttribute(localCode, Name("code")), Is.False);
            Assert.That(tests.MatchesXPathSchemaType(member, Name("Derived")), Is.True);
            Assert.That(tests.MatchesXPathSchemaType(member, Name("Base")), Is.True);
            Assert.That(member.TypeInfo!.Declaration, Is.Not.Null);
            Assert.That(member.TypeInfo.TypeDefinition, Is.Not.Null);
            Assert.That(code.TypeInfo!.Declaration, Is.Not.Null);
            Assert.That(code.TypeInfo.TypeDefinition, Is.Not.Null);
        });

        XPathStaticException missingElement = Assert.Throws<XPathStaticException>(() => tests.MatchesXPathSchemaElement(member, Name("missing")))!;
        XPathStaticException missingAttribute = Assert.Throws<XPathStaticException>(() => tests.MatchesXPathSchemaAttribute(code, Name("missing")))!;
        XPathStaticException missingType = Assert.Throws<XPathStaticException>(() => tests.MatchesXPathSchemaType(member, Name("MissingType")))!;
        Assert.Multiple(() =>
        {
            Assert.That(missingElement.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0008));
            Assert.That(missingAttribute.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0008));
            Assert.That(missingType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0051));
        });
    }

    [Test]
    public void SchemaIdentitySurvivesTreeCopies()
    {
        XmlSchemaSet schemaSet = CompileSchema();
        XmlDocument document = Validate(schemaSet, "<member xmlns='urn:test' xmlns:t='urn:test' t:code='7' t:extra='x'/>");
        XmlNode source = document.DocumentElement!;

        XmlNode copy = new XmlTreeEditor().CloneXmlTreeNode(source, true);

        Assert.Multiple(() =>
        {
            Assert.That(copy.TypeInfo, Is.Not.SameAs(source.TypeInfo));
            Assert.That(copy.TypeInfo!.Declaration, Is.SameAs(source.TypeInfo!.Declaration));
            Assert.That(copy.TypeInfo.TypeDefinition, Is.SameAs(source.TypeInfo.TypeDefinition));
            Assert.That(new XPathSchemaNodeTest(schemaSet).MatchesXPathSchemaElement(copy, Name("head")), Is.True);
        });
    }

    [Test]
    public void XsltSchemaImportsFeedTypedModesAndTypeAwarePatterns()
    {
        XmlSchemaSet schemaSet = CompileSchema();
        XmlDocument document = Validate(schemaSet, "<member xmlns='urn:test' xmlns:t='urn:test' t:code='7' t:extra='x'/>");
        XmlNode member = document.DocumentElement!;
        XmlNode code = member.Attributes!.Single(value => value.Name!.LocalName == "code");
        XsltCompileOptions options = new XsltCompileOptions
        {
            ConformanceLevel = XsltConformanceLevel.SchemaAware,
            SchemaSet = schemaSet
        };
        XsltSchemaIntegration integration = new XsltSchemaIntegration(options);
        XPathStaticException notImported = Assert.Throws<XPathStaticException>(() => integration.MatchesXsltSchemaElementPattern(member, Name("head")))!;
        integration.ImportXsltSchemaNamespace("urn:test");

        Assert.Multiple(() =>
        {
            Assert.That(notImported.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0008));
            Assert.That(integration.IsXsltSchemaNamespaceImported("urn:test"), Is.True);
            Assert.That(integration.MatchesXsltSchemaElementPattern(member, Name("head")), Is.True);
            Assert.That(integration.MatchesXsltSchemaAttributePattern(code, Name("code")), Is.True);
            Assert.That(integration.MatchesXsltTypeAwarePattern(member, Name("Base")), Is.True);
            Assert.That(integration.MatchesXsltTypedModeNameTest(member, Name("head"), XsltTypedMode.Strict), Is.True);
        });

        XmlNode typedUnknown = new XmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = Name("other"),
            TypeInfo = member.TypeInfo
        };
        Assert.That(integration.MatchesXsltTypedModeNameTest(typedUnknown, Name("other"), XsltTypedMode.Lax), Is.True);

        XsltTypeException typedRequired = Assert.Throws<XsltTypeException>(() => integration.EnsureXsltTypedModeSelection(
            new XmlNode { Kind = XmlNodeKind.Element, Name = Name("plain") },
            XsltTypedMode.Yes))!;
        XsltTypeException untypedRequired = Assert.Throws<XsltTypeException>(() => integration.EnsureXsltTypedModeSelection(member, XsltTypedMode.No))!;
        XsltStaticException strictDeclaration = Assert.Throws<XsltStaticException>(() => integration.MatchesXsltTypedModeNameTest(member, Name("missing"), XsltTypedMode.Strict))!;
        XsltSchemaIntegration basic = new XsltSchemaIntegration(new XsltCompileOptions { ConformanceLevel = XsltConformanceLevel.Basic });
        XsltStaticException importError = Assert.Throws<XsltStaticException>(() => basic.ImportXsltSchemaNamespace("urn:test"))!;
        XsltStaticException typedModeError = Assert.Throws<XsltStaticException>(() => basic.EnsureXsltTypedModeSelection(member, XsltTypedMode.Yes))!;
        XsltStaticException emptyNamespace = Assert.Throws<XsltStaticException>(() => integration.ImportXsltSchemaNamespace(""))!;
        XmlNode known = Validate(schemaSet, "<known xmlns='urn:test'>5</known>").DocumentElement!;
        XsltSchemaIntegration builtIns = new XsltSchemaIntegration(new XsltCompileOptions { ConformanceLevel = XsltConformanceLevel.SchemaAware });

        Assert.Multiple(() =>
        {
            Assert.That(typedRequired.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE3100));
            Assert.That(untypedRequired.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE3110));
            Assert.That(strictDeclaration.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3105));
            Assert.That(importError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE1650));
            Assert.That(typedModeError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE1660));
            Assert.That(emptyNamespace.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0215));
            Assert.That(builtIns.MatchesXsltTypeAwarePattern(known, XsdName("decimal")), Is.True);
        });
    }

    [Test]
    public void XsltStrictLaxPreserveAndStripValidationUseSchemaAnnotations()
    {
        XmlSchemaSet schemaSet = CompileSchema();
        XsltSchemaIntegration integration = new XsltSchemaIntegration(new XsltCompileOptions
        {
            ConformanceLevel = XsltConformanceLevel.SchemaAware,
            SchemaSet = schemaSet
        });
        XmlDocument strictDocument = Parse("<member xmlns='urn:test' xmlns:t='urn:test' t:code='7' t:extra='x'/>");

        XmlSchemaValidationResult strict = integration.ApplyXsltSchemaValidation(strictDocument, XsltSchemaValidationMode.Strict);
        XmlSchemaTypeInfo strictAnnotation = strictDocument.DocumentElement!.TypeInfo!;
        XmlSchemaValidationResult preserved = integration.ApplyXsltSchemaValidation(strictDocument, XsltSchemaValidationMode.Preserve);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(strictAnnotation.Declaration, Is.Not.Null);
            Assert.That(preserved.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(strictDocument.DocumentElement.TypeInfo, Is.SameAs(strictAnnotation));
        });

        XmlSchemaValidationResult stripped = integration.ApplyXsltSchemaValidation(strictDocument, XsltSchemaValidationMode.Strip);
        Assert.Multiple(() =>
        {
            Assert.That(stripped.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(strictDocument.DocumentElement!.TypeInfo!.TypeName!.LocalName, Is.EqualTo("untyped"));
            Assert.That(strictDocument.DocumentElement.Attributes!.Single(value => value.Name!.LocalName == "code").TypeInfo!.TypeName!.LocalName, Is.EqualTo("untypedAtomic"));
            Assert.That(integration.IsXsltTypedSchemaNode(strictDocument.DocumentElement), Is.False);
        });

        XmlDocument laxDocument = Parse("<wrapper xmlns='urn:test'><known>5</known></wrapper>");
        XmlSchemaValidationResult lax = integration.ApplyXsltSchemaValidation(laxDocument, XsltSchemaValidationMode.Lax);
        Assert.Multiple(() =>
        {
            Assert.That(lax.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(integration.IsXsltTypedSchemaNode(laxDocument.DocumentElement!), Is.False);
            Assert.That(integration.IsXsltTypedSchemaNode(laxDocument.DocumentElement!.Children![0]), Is.True);
        });

        XsltTypeException laxInvalid = Assert.Throws<XsltTypeException>(() => integration.ApplyXsltSchemaValidation(
            Parse("<wrapper xmlns='urn:test'><known>bad</known></wrapper>"),
            XsltSchemaValidationMode.Lax))!;
        XsltTypeException strictUnknown = Assert.Throws<XsltTypeException>(() => integration.ApplyXsltSchemaValidation(
            Parse("<wrapper xmlns='urn:test'/>"),
            XsltSchemaValidationMode.Strict))!;
        XsltTypeException strictInvalid = Assert.Throws<XsltTypeException>(() => integration.ApplyXsltSchemaValidation(
            Parse("<member xmlns='urn:test'/>"),
            XsltSchemaValidationMode.Strict))!;
        Assert.Multiple(() =>
        {
            Assert.That(laxInvalid.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE1515));
            Assert.That(strictUnknown.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE1512));
            Assert.That(strictInvalid.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE1510));
        });

        XmlDocument skippedDocument = Parse("<wrapper xmlns='urn:test'><known>bad</known></wrapper>");
        XmlSchemaValidationResult skipped = new XmlSchemaValidator().ValidateXmlSchema(
            skippedDocument,
            schemaSet,
            new XmlSchemaValidationOptions { ProcessContents = XmlSchemaProcessContents.Skip, AnnotateTree = true });
        Assert.Multiple(() =>
        {
            Assert.That(skipped.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(skipped.Diagnostics, Is.Empty);
            Assert.That(skippedDocument.DocumentElement!.TypeInfo!.Declaration, Is.Null);
            Assert.That(skippedDocument.DocumentElement.Children![0].TypeInfo!.Declaration, Is.Null);
        });

        XsltSchemaIntegration withoutSchemas = new XsltSchemaIntegration(new XsltCompileOptions { ConformanceLevel = XsltConformanceLevel.SchemaAware });
        XmlDocument unlocatedDocument = Parse("<wrapper xmlns='urn:test'/>");
        XmlSchemaValidationResult unlocated = withoutSchemas.ApplyXsltSchemaValidation(unlocatedDocument, XsltSchemaValidationMode.Lax);
        XsltSchemaIntegration basicValidation = new XsltSchemaIntegration(new XsltCompileOptions { ConformanceLevel = XsltConformanceLevel.Basic });
        XsltStaticException basicStrict = Assert.Throws<XsltStaticException>(() => basicValidation.ApplyXsltSchemaValidation(
            Parse("<wrapper xmlns='urn:test'/>"),
            XsltSchemaValidationMode.Strict))!;
        Assert.Multiple(() =>
        {
            Assert.That(unlocated.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(unlocatedDocument.DocumentElement!.TypeInfo!.TypeName!.LocalName, Is.EqualTo("untyped"));
            Assert.That(basicStrict.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE1660));
        });
    }

    private static XmlDocument Validate(XmlSchemaSet schemaSet, string text)
    {
        XmlDocument document = Parse(text);
        XmlSchemaValidationResult result = new XmlSchemaValidator().ValidateXmlSchema(
            document,
            schemaSet,
            new XmlSchemaValidationOptions { AnnotateTree = true });
        Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
        return document;
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema =
            "<xs:schema xmlns:xs='" + Xsd + "' xmlns:t='urn:test' targetNamespace='urn:test' elementFormDefault='qualified' attributeFormDefault='qualified'>" +
            "<xs:simpleType name='Code'><xs:restriction base='xs:int'><xs:minInclusive value='1'/></xs:restriction></xs:simpleType>" +
            "<xs:attribute name='code' type='t:Code'/>" +
            "<xs:complexType name='Base'><xs:attribute ref='t:code' use='required'/></xs:complexType>" +
            "<xs:complexType name='Derived'><xs:complexContent><xs:extension base='t:Base'><xs:attribute name='extra' type='xs:string'/></xs:extension></xs:complexContent></xs:complexType>" +
            "<xs:element name='head' type='t:Base' abstract='true'/>" +
            "<xs:element name='member' type='t:Derived' substitutionGroup='t:head'/>" +
            "<xs:element name='local'><xs:complexType><xs:attribute name='code' type='t:Code' use='required'/></xs:complexType></xs:element>" +
            "<xs:element name='known' type='xs:int'/>" +
            "</xs:schema>";
        XmlResolverPolicy policy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied, AllowedSchemes = [] };
        return new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = schema, BaseUri = "https://example.test/schema.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = policy },
            new XmlDefaultResolver());
    }

    private static XmlDocument Parse(string text)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
        return new XmlParser().ParseXmlDocument(
            new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = "https://example.test/input.xml" },
            options,
            null,
            null,
            null);
    }

    private static XmlName Name(string localName)
    {
        return new XmlName { LocalName = localName, NamespaceUri = "urn:test" };
    }

    private static XmlName XsdName(string localName)
    {
        return new XmlName { LocalName = localName, NamespaceUri = Xsd };
    }
}
