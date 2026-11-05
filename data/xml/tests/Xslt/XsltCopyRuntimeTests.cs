using xml;

namespace xml.Tests;

[TestFixture]
public class XsltCopyRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void CopyOfDeepCopiesNodesAndRetainsNonNodeItemsInOrder()
    {
        XmlDocument source = Parse("<root code='a'><child>text</child></root>", "https://source.test/input.xml");
        string stylesheet = Stylesheet("<xsl:template name='entry'><xsl:copy-of select='., 7' validation='preserve'/></xsl:template>", "");

        XdmSequence result = Initialize(stylesheet, Node(source.DocumentElement!), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XmlNode copy = result.Nodes!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Node, XdmItemKind.Atomic }));
            Assert.That(result.AtomicValues!.Single().IntegerValue, Is.EqualTo(7));
            Assert.That(copy, Is.Not.SameAs(source.DocumentElement));
            Assert.That(copy.Attributes!.Single().Value, Is.EqualTo("a"));
            Assert.That(copy.Children!.Single().Children!.Single().Value, Is.EqualTo("text"));
            Assert.That(new TreeNavigation().GetXmlNodeBaseUriOperation(copy), Is.EqualTo("https://source.test/input.xml"));
        });
    }

    [Test]
    public void CopyOfRemovesUnusedNamespacesAndStripsTypeAnnotationsByDefault()
    {
        XmlDocument source = Parse("<p:root xmlns:p='urn:p' xmlns:q='urn:q' xmlns:u='urn:unused' q:code='v'><p:child/></p:root>", "https://source.test/input.xml");
        XmlNode root = source.DocumentElement!;
        root.TypeInfo = Type("custom");
        root.Attributes!.Single(item => item.Name!.LocalName == "code").TypeInfo = Type("customAttribute");
        string stylesheet = Stylesheet("<xsl:template name='entry'><xsl:copy-of select='.' copy-namespaces='no'/></xsl:template>", "");

        XmlNode copy = Initialize(stylesheet, Node(root), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation().Nodes!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(copy.NamespaceBindings!.Any(item => item.NamespaceUri == "urn:p"), Is.True);
            Assert.That(copy.NamespaceBindings!.Any(item => item.NamespaceUri == "urn:q"), Is.True);
            Assert.That(copy.NamespaceBindings!.Any(item => item.NamespaceUri == "urn:unused"), Is.False);
            Assert.That(copy.Children!.Single().NamespaceBindings!.Any(item => item.NamespaceUri == "urn:p"), Is.True);
            Assert.That(copy.TypeInfo!.TypeName!.LocalName, Is.EqualTo("untyped"));
            Assert.That(copy.Attributes!.Single(item => item.Name!.LocalName == "code").TypeInfo!.TypeName!.LocalName, Is.EqualTo("untypedAtomic"));
        });
    }

    [Test]
    public void CopyOfCopiesInScopeNamespacesFromAncestors()
    {
        XmlDocument source = Parse("<outer xmlns:p='urn:inherited'><inner/></outer>", "https://source.test/input.xml");
        string stylesheet = Stylesheet("<xsl:template name='entry'><xsl:copy-of select='/*/inner' validation='preserve'/></xsl:template>", "");

        XmlNode copy = Initialize(stylesheet, Node(source.DocumentNode!), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation().Nodes!.Single();

        Assert.That(copy.NamespaceBindings!.Any(item => item.Prefix == "p" && item.NamespaceUri == "urn:inherited"), Is.True);
    }

    [Test]
    public void CopyOfPreservesAnnotationsAndRejectsNamespaceSensitiveRemoval()
    {
        XmlDocument source = Parse("<p:root xmlns:p='urn:p'/>", "https://source.test/input.xml");
        XmlNode root = source.DocumentElement!;
        root.TypeInfo = Type("custom");
        string preserve = Stylesheet("<xsl:template name='entry'><xsl:copy-of select='.' validation='preserve'/></xsl:template>", "");

        XmlNode copy = Initialize(preserve, Node(root), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation().Nodes!.Single();
        root.TypeInfo!.AtomicType = XdmAtomicType.QName;
        string invalid = Stylesheet("<xsl:template name='entry'><xsl:copy-of select='.' validation='preserve' copy-namespaces='no'/></xsl:template>", "");
        XsltTypeException error = Assert.Throws<XsltTypeException>(() => Initialize(invalid, Node(root), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(copy.TypeInfo!.TypeName!.LocalName, Is.EqualTo("custom"));
            Assert.That(copy.TypeInfo, Is.Not.SameAs(root.TypeInfo));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE0950));
        });
    }

    [Test]
    public void CopyUsesSelectedItemAsFocusAndConstructsShallowContent()
    {
        XmlDocument source = Parse("<root><item old='not-copied' id='42'><child/></item></root>", "https://source.test/input.xml");
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:copy select='/*/item'>" +
            "<xsl:attribute name='new' select='@id'/><xsl:value-of select='name()'/>" +
            "</xsl:copy></xsl:template>", "");

        XmlNode copy = Initialize(stylesheet, Node(source.DocumentNode!), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation().Nodes!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(copy.Name!.LocalName, Is.EqualTo("item"));
            Assert.That(copy.Attributes, Has.Length.EqualTo(1));
            Assert.That(copy.Attributes![0].Name!.LocalName, Is.EqualTo("new"));
            Assert.That(copy.Attributes[0].Value, Is.EqualTo("42"));
            Assert.That(copy.Children!.Single().Value, Is.EqualTo("item"));
            Assert.That(new TreeNavigation().GetXmlNodeBaseUriOperation(copy), Is.EqualTo("https://source.test/input.xml"));
        });
    }

    [Test]
    public void CopyCanConstructAReplacementDocumentFromTheSelectedFocus()
    {
        XmlDocument source = Parse("<root><child/></root>", "https://source.test/input.xml");
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:copy select='.' validation='preserve'><xsl:copy-of select='node()' validation='preserve'/></xsl:copy></xsl:template>", "");

        XmlNode copy = Initialize(stylesheet, Node(source.DocumentNode!), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation().Nodes!.Single();
        XmlDocument copiedDocument = copy.OwnerDocument!;
        XmlNode copiedElement = copy.Children!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(copy.Kind, Is.EqualTo(XmlNodeKind.Document));
            Assert.That(copiedElement.Name!.LocalName, Is.EqualTo("root"));
            Assert.That(copiedDocument.DocumentNode, Is.SameAs(copy));
            Assert.That(copiedDocument.DocumentElement, Is.SameAs(copiedElement));
        });
    }

    [Test]
    public void CopyResolvesCopiedXmlBaseAgainstTheInstructionBase()
    {
        XmlDocument source = Parse("<root xml:base='relative/'/>", "https://source.test/path/input.xml");
        string stylesheet = Stylesheet("<xsl:template name='entry'><xsl:copy-of select='.' validation='preserve'/></xsl:template>", "");

        XmlNode copy = Initialize(stylesheet, Node(source.DocumentElement!), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation().Nodes!.Single();

        Assert.That(new TreeNavigation().GetXmlNodeBaseUriOperation(copy), Is.EqualTo("https://example.test/relative/"));
    }

    [Test]
    public void CopyDetectsDuplicateAttributesAndConflictingNamespaces()
    {
        XmlDocument source = Parse("<p:root xmlns:p='urn:one'/>", "https://source.test/input.xml");
        string attribute = Stylesheet(
            "<xsl:template name='entry'><xsl:copy select='.'><xsl:attribute name='same'/><xsl:attribute name='same'/></xsl:copy></xsl:template>", "");
        string namespaceCopy = Stylesheet(
            "<xsl:template name='entry'><xsl:copy select='.' copy-namespaces='no'>" +
            "<xsl:namespace name='p' select=\"'urn:one'\"/><xsl:namespace name='p' select=\"'urn:two'\"/>" +
            "</xsl:copy></xsl:template>", "");

        XsltDynamicException duplicate = Assert.Throws<XsltDynamicException>(() => Initialize(attribute, Node(source.DocumentElement!), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;
        XsltDynamicException conflict = Assert.Throws<XsltDynamicException>(() => Initialize(namespaceCopy, Node(source.DocumentElement!), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0410));
            Assert.That(conflict.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0430));
        });
    }

    [Test]
    public void CopyOfRecordsAccumulatorCorrespondenceOnlyWhenRequested()
    {
        XmlDocument source = Parse("<root><child>text</child></root>", "https://source.test/input.xml");
        string stylesheet = Stylesheet(
            "<xsl:template name='entry'><xsl:copy-of select='.' copy-accumulators='yes' validation='preserve'/>" +
            "<xsl:copy-of select='.' validation='preserve'/></xsl:template>", "");
        XsltTransformationState state = Initialize(stylesheet, Node(source.DocumentElement!), null);

        XdmSequence result = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(state.AccumulatorCopies, Has.Count.EqualTo(3));
            Assert.That(state.AccumulatorCopies[0].Source, Is.SameAs(source.DocumentElement));
            Assert.That(state.AccumulatorCopies[0].Copy, Is.SameAs(result.Nodes![0]));
        });
    }

    [Test]
    public void CopyEnforcesMissingContextAndSingletonSelection()
    {
        string absent = Stylesheet("<xsl:template name='entry'><xsl:copy/></xsl:template>", "");
        string multiple = Stylesheet("<xsl:template name='entry'><xsl:copy select='1 to 2'/></xsl:template>", "");

        XsltTypeException missing = Assert.Throws<XsltTypeException>(() => Initialize(absent, null, null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;
        XsltTypeException cardinality = Assert.Throws<XsltTypeException>(() => Initialize(multiple, null, null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(missing.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE0945));
            Assert.That(cardinality.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE3180));
        });
    }

    [Test]
    public void CopyOfValidatesAgainstAnExplicitSchemaType()
    {
        XmlSchemaSet schemas = BuiltInSchemas();
        XmlDocument source = Parse("<value>content</value>", "https://source.test/input.xml");
        string stylesheet = Stylesheet("<xsl:template name='entry'><xsl:copy-of select='.' type='xs:string'/></xsl:template>", "xmlns:xs='" + Xs + "'");

        XmlNode copy = Initialize(stylesheet, Node(source.DocumentElement!), schemas).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation().Nodes!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(copy.TypeInfo!.TypeName!.NamespaceUri, Is.EqualTo(Xs));
            Assert.That(copy.TypeInfo.TypeName.LocalName, Is.EqualTo("string"));
            Assert.That(copy.TypeInfo.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
        });

        XmlDocument invalidSource = Parse("<value><child/></value>", "https://source.test/input.xml");
        XsltTypeException invalid = Assert.Throws<XsltTypeException>(() => Initialize(stylesheet, Node(invalidSource.DocumentElement!), schemas).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;
        Assert.That(invalid.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE1540));
    }

    private static XsltTransformationState Initialize(string source, XdmSequence? context, XmlSchemaSet? schemas)
    {
        XsltCompileOptions compileOptions = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ConformanceLevel = schemas == null ? XsltConformanceLevel.Basic : XsltConformanceLevel.SchemaAware,
            SchemaSet = schemas,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, compileOptions, new XmlDefaultResolver());
        XsltTransformOptions options = new XsltTransformOptions { InitialTemplate = Name("entry"), GlobalContextItem = context };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
    }

    private static XmlSchemaSet BuiltInSchemas()
    {
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions { ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied } };
        return new XmlSchemaCompiler().CompileXmlSchemas([], options, new XmlDefaultResolver());
    }

    private static XmlSchemaTypeInfo Type(string localName)
    {
        return new XmlSchemaTypeInfo { TypeName = new XmlName { NamespaceUri = "urn:types", LocalName = localName, QualifiedName = "t:" + localName }, Validity = XmlSchemaValidity.Valid };
    }

    private static XdmSequence Node(XmlNode node)
    {
        return new XdmSequenceOperations().CreateXdmNodeSequence(node);
    }

    private static XmlName Name(string localName)
    {
        return new XmlName { LocalName = localName, QualifiedName = localName, NamespaceUri = "" };
    }

    private static XmlDocument Parse(string source, string baseUri)
    {
        XmlParserOptions options = new XmlParserOptions { NamespaceHandling = XmlNamespaceHandling.Namespaces10, PreservePrefixes = true };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source, BaseUri = baseUri }, options, null, null, null);
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }
}
