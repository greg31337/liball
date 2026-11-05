using xml;

namespace xml.Tests;

[TestFixture]
public class XsltDeclarationCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void CompilesPackageMetadataAndBasicInputAnnotationSemantics()
    {
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:package' package-version='2.1' id='package-id' " +
            "version='3.0' declared-modes='no' input-type-annotations='preserve' default-mode='named' default-validation='preserve' " +
            "default-collation='http://www.w3.org/2005/xpath-functions/collation/codepoint'/>";

        XsltStylesheetPlan stylesheet = Plan(Compile(source, XsltConformanceLevel.Basic, null));
        XsltPackageMetadataPlan metadata = stylesheet.Metadata!;
        XsltPackagePlan package = stylesheet.Package!;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Id, Is.EqualTo("package-id"));
            Assert.That(metadata.Name, Is.EqualTo("urn:package"));
            Assert.That(metadata.PackageVersion, Is.EqualTo("2.1"));
            Assert.That(metadata.IsExplicit, Is.True);
            Assert.That(metadata.DeclaredModes, Is.False);
            Assert.That(metadata.InputTypeAnnotations, Is.EqualTo(XsltInputTypeAnnotations.Preserve));
            Assert.That(metadata.EffectiveInputTypeAnnotations, Is.EqualTo(XsltInputTypeAnnotations.Strip));
            Assert.That(metadata.DefaultMode, Is.EqualTo("Q{}named"));
            Assert.That(metadata.DefaultValidation, Is.EqualTo(XsltDefaultValidation.Preserve));
            Assert.That(package.Id, Is.EqualTo(metadata.Id));
            Assert.That(package.EffectiveInputTypeAnnotations, Is.EqualTo(XsltInputTypeAnnotations.Strip));
            Assert.That(package.DefaultMode, Is.EqualTo(metadata.DefaultMode));
        });
    }

    [Test]
    public void RejectsInconsistentInputAnnotationsAcrossIncludedModules()
    {
        DeclarationResolver resolver = new DeclarationResolver();
        resolver.Resources.Add("https://example.test/included.xsl", Stylesheet("", "input-type-annotations='strip'"));
        string source = Stylesheet("<xsl:include href='included.xsl'/>", "input-type-annotations='preserve'");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, XsltConformanceLevel.SchemaAware, resolver))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0265));
    }

    [Test]
    public void CompilesConsistentGlobalContextDeclarationsByExpandedTypeName()
    {
        DeclarationResolver resolver = new DeclarationResolver();
        resolver.Resources.Add(
            "https://example.test/included.xsl",
            Stylesheet("<xsl:global-context-item as='b:string' use='required'/>", "xmlns:b='http://www.w3.org/2001/XMLSchema'"));
        string source = Stylesheet(
            "<xsl:include href='included.xsl'/><xsl:global-context-item as='a:string' use='required'/>",
            "xmlns:a='http://www.w3.org/2001/XMLSchema'");

        XsltGlobalContextItemPlan context = Plan(Compile(source, XsltConformanceLevel.Basic, resolver)).GlobalContextItem;

        Assert.Multiple(() =>
        {
            Assert.That(context.IsDeclared, Is.True);
            Assert.That(context.Use, Is.EqualTo(XsltContextItemUse.Required));
            Assert.That(context.Declarations, Has.Length.EqualTo(2));
            Assert.That(context.Type!.ResolvedName!.NamespaceUri, Is.EqualTo(Xs));
            Assert.That(context.Type.ResolvedName.LocalName, Is.EqualTo("string"));
            Assert.That(context.Declarations.Select(item => item.TypeIdentity).Distinct().Count(), Is.EqualTo(1));
        });
    }

    [TestCase("<xsl:global-context-item/><xsl:global-context-item/>", XsltErrorCodes.XTSE3087)]
    [TestCase("<xsl:global-context-item use='absent' as='item()'/>", XsltErrorCodes.XTSE3089)]
    [TestCase("<xsl:global-context-item use='sometimes'/>", XsltErrorCodes.XTSE0020)]
    [TestCase("<xsl:global-context-item><xsl:text>value</xsl:text></xsl:global-context-item>", XsltErrorCodes.XTSE0010)]
    public void RejectsInvalidGlobalContextDeclarations(string declaration, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declaration, ""), XsltConformanceLevel.Basic, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void RejectsInconsistentGlobalContextDeclarationsAcrossModules()
    {
        DeclarationResolver resolver = new DeclarationResolver();
        resolver.Resources.Add("https://example.test/included.xsl", Stylesheet("<xsl:global-context-item use='optional'/>", ""));
        string source = Stylesheet("<xsl:include href='included.xsl'/><xsl:global-context-item use='required'/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, XsltConformanceLevel.Basic, resolver))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE3087));
    }

    [Test]
    public void RejectsRequiredGlobalContextForLibraryPackage()
    {
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:library' package-version='1' version='3.0'>" +
            "<xsl:global-context-item use='required'/></xsl:package>";
        XsltCompileOptions options = Options(XsltConformanceLevel.Basic, null);
        XsltStylesheetLoader loader = new XsltStylesheetLoader(options, new XmlDefaultResolver(), new XsltPackageCompilationContext(), true);

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => loader.CompileXsltStylesheet(new XmlInput { Text = source }))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE0590));
    }

    [Test]
    public void IgnoresOptionalGlobalContextConstraintsForLibraryPackage()
    {
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:library' package-version='1' version='3.0'>" +
            "<xsl:global-context-item use='optional' as='xs:string' xmlns:xs='" + Xs + "'/></xsl:package>";
        XsltStylesheetLoader loader = new XsltStylesheetLoader(Options(XsltConformanceLevel.Basic, null), new XmlDefaultResolver(), new XsltPackageCompilationContext(), true);

        XsltGlobalContextItemPlan context = Plan(loader.CompileXsltStylesheet(new XmlInput { Text = source })).GlobalContextItem;

        Assert.Multiple(() =>
        {
            Assert.That(context.Declarations, Has.Length.EqualTo(1));
            Assert.That(context.IsDeclared, Is.False);
            Assert.That(context.Use, Is.EqualTo(XsltContextItemUse.Optional));
            Assert.That(context.Type, Is.Null);
        });
    }

    [Test]
    public void GatesImportSchemaInBasicConformance()
    {
        string source = Stylesheet("<xsl:import-schema namespace='urn:types'/>", "");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, XsltConformanceLevel.Basic, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE1650));
    }

    [Test]
    public void GatesSchemaDependentGlobalContextTypesInBasicConformance()
    {
        string source = Stylesheet("<xsl:global-context-item as='t:Code'/>", "xmlns:t='urn:types'");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, XsltConformanceLevel.Basic, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE1660));
    }

    [Test]
    public void CompilesInlineSchemaBeforeSchemaTypedDeclarations()
    {
        string schema = "<xs:schema xmlns:xs='" + Xs + "' targetNamespace='urn:types'>" +
            "<xs:simpleType name='Code'><xs:restriction base='xs:string'/></xs:simpleType></xs:schema>";
        string source = Stylesheet(
            "<xsl:import-schema namespace='urn:types'>" + schema + "</xsl:import-schema>" +
            "<xsl:global-context-item as='t:Code' use='optional'/>" +
            "<xsl:variable name='code' as='t:Code' select=\"'ok'\"/>",
            "xmlns:t='urn:types'");

        XsltStylesheetPlan stylesheet = Plan(Compile(source, XsltConformanceLevel.SchemaAware, null));
        XsltSchemaImportPlan import = stylesheet.SchemaImports.Single();

        Assert.Multiple(() =>
        {
            Assert.That(import.NamespaceUri, Is.EqualTo("urn:types"));
            Assert.That(import.InlineSchema, Is.Not.Null);
            Assert.That(import.CompiledSchema, Is.Not.Null);
            Assert.That(stylesheet.SchemaSet, Is.SameAs(import.CompiledSchema));
            Assert.That(stylesheet.ImportedSchemaNamespaces, Is.EqualTo(new[] { "urn:types" }));
            Assert.That(stylesheet.StaticContexts.All(context => context.ImportedSchemaNamespaces.Contains("urn:types")), Is.True);
            Assert.That(stylesheet.GlobalContextItem.Type!.SchemaType, Is.True);
            Assert.That(stylesheet.Variables.Single().SequenceType!.SchemaType, Is.True);
        });
    }

    [Test]
    public void ResolvesAndCompilesExternalSchemaLocations()
    {
        DeclarationResolver resolver = new DeclarationResolver();
        resolver.Resources.Add("https://example.test/types.xsd", "<xs:schema xmlns:xs='" + Xs + "' targetNamespace='urn:external'><xs:element name='root' type='xs:string'/></xs:schema>");
        string source = Stylesheet("<xsl:import-schema namespace='urn:external' schema-location='types.xsd'/>", "");

        XsltSchemaImportPlan import = Plan(Compile(source, XsltConformanceLevel.SchemaAware, resolver)).SchemaImports.Single();

        Assert.Multiple(() =>
        {
            Assert.That(import.CompiledSchema, Is.Not.Null);
            Assert.That(import.CompiledSchema!.TargetNamespaces, Does.Contain("urn:external"));
            Assert.That(resolver.Kinds, Does.Contain(XmlResourceKind.Schema));
        });
    }

    [Test]
    public void CombinesImportedSchemasWithCallerSuppliedSchemas()
    {
        XmlSchemaSet supplied = new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Text = "<xs:schema xmlns:xs='" + Xs + "' targetNamespace='urn:supplied'><xs:element name='old' type='xs:string'/></xs:schema>" }],
            new XmlSchemaValidationOptions(),
            new XmlDefaultResolver());
        string imported = "<xs:schema xmlns:xs='" + Xs + "' targetNamespace='urn:imported'><xs:element name='new' type='xs:string'/></xs:schema>";
        string source = Stylesheet("<xsl:import-schema namespace='urn:imported'>" + imported + "</xsl:import-schema>", "");
        XsltCompileOptions options = Options(XsltConformanceLevel.SchemaAware, null);
        options.SchemaSet = supplied;

        XsltStylesheetPlan stylesheet = Plan(new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new XmlDefaultResolver()));

        Assert.That(stylesheet.SchemaSet!.TargetNamespaces, Is.EquivalentTo(new[] { "urn:supplied", "urn:imported" }));
    }

    [TestCase("<xsl:import-schema namespace=''/>", XsltErrorCodes.XTSE0215)]
    [TestCase("<xsl:import-schema namespace='urn:a'><xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='urn:b'/></xsl:import-schema>", XsltErrorCodes.XTSE0215)]
    [TestCase("<xsl:import-schema namespace='urn:a' schema-location='a.xsd'><xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='urn:a'/></xsl:import-schema>", XsltErrorCodes.XTSE0215)]
    [TestCase("<xsl:import-schema namespace='urn:a'><xsl:text>bad</xsl:text></xsl:import-schema>", XsltErrorCodes.XTSE0010)]
    public void RejectsInvalidSchemaImportDeclarations(string declaration, string expectedCode)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(Stylesheet(declaration, ""), XsltConformanceLevel.SchemaAware, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void ReportsInvalidInlineSchemasAsSchemaConstructionErrors()
    {
        string source = Stylesheet(
            "<xsl:import-schema namespace='urn:bad'><xs:schema xmlns:xs='" + Xs + "' targetNamespace='urn:bad'>" +
            "<xs:element name='root' type='missing:Type'/></xs:schema></xsl:import-schema>",
            "xmlns:missing='urn:missing'");

        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Compile(source, XsltConformanceLevel.SchemaAware, null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0220));
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XsltStylesheetPlan Plan(XsltExecutable executable)
    {
        return (XsltStylesheetPlan)executable.Plan!;
    }

    private static XsltExecutable Compile(string source, XsltConformanceLevel level, IXmlResourceResolver? resolver)
    {
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, Options(level, resolver), resolver == null ? new XmlDefaultResolver() : resolver);
    }

    private static XsltCompileOptions Options(XsltConformanceLevel level, IXmlResourceResolver? resolver)
    {
        return new XsltCompileOptions
        {
            ConformanceLevel = level,
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = resolver == null ? XmlResourceAccess.Denied : XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
    }

    private sealed class DeclarationResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources = new Dictionary<string, string>();
        internal List<XmlResourceKind> Kinds = new List<XmlResourceKind>();

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string absolute = new Uri(new Uri(baseUri!), systemIdentifier!).AbsoluteUri;
            Kinds.Add(kind);
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                Input = new XmlInput { Text = Resources[absolute] },
                RequestedUri = systemIdentifier,
                AbsoluteUri = absolute
            };
        }
    }
}
