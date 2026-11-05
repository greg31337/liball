using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaCompilerTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void CompilesIncludesImportsAndChameleonSchemasIntoOneResolvedSet()
    {
        SchemaResolver resolver = new SchemaResolver();
        resolver.Add("https://example.test/schema/parts/common.xsd", Schema("<xs:element name='shared' type='xs:string'/>", null));
        resolver.Add("https://example.test/schema/other.xsd", Schema("<xs:element name='other' type='xs:string'/>", "urn:other"));
        string root = Schema(
            "<xs:include schemaLocation='parts/common.xsd'/>" +
            "<xs:import namespace='urn:other' schemaLocation='other.xsd'/>" +
            "<xs:element name='root' type='xs:string'/>",
            "urn:root");

        XmlSchemaSet result = Compile([Input(root, "https://example.test/schema/root.xsd")], resolver);
        CompiledXmlSchemaSet compiled = (CompiledXmlSchemaSet)result.CompiledSet!;
        XmlSchemaModule rootModule = compiled.Modules[0];
        XmlSchemaModule included = rootModule.Directives[0].TargetModule!;
        XmlSchemaModule imported = rootModule.Directives[1].TargetModule!;
        XmlDocument[] documents = result.SchemaDocuments!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsImmutable, Is.True);
            Assert.That(compiled.IsImmutable, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.SchemaDocuments, Has.Length.EqualTo(3));
            Assert.That(result.TargetNamespaces, Is.EqualTo(new string[] { "urn:root", "urn:other" }));
            Assert.That(compiled.Modules, Has.Length.EqualTo(3));
            Assert.That(rootModule.Relation, Is.EqualTo(XmlSchemaDirectiveKind.Root));
            Assert.That(rootModule.EffectiveTargetNamespace, Is.EqualTo("urn:root"));
            Assert.That(included.DeclaredTargetNamespace, Is.Null);
            Assert.That(included.EffectiveTargetNamespace, Is.EqualTo("urn:root"));
            Assert.That(included.IsChameleon, Is.True);
            Assert.That(imported.DeclaredTargetNamespace, Is.EqualTo("urn:other"));
            Assert.That(imported.EffectiveTargetNamespace, Is.EqualTo("urn:other"));
            Assert.That(rootModule.Directives[0].Kind, Is.EqualTo(XmlSchemaDirectiveKind.Include));
            Assert.That(rootModule.Directives[1].Kind, Is.EqualTo(XmlSchemaDirectiveKind.Import));
            Assert.That(resolver.Requests, Is.EqualTo(new string[] { "https://example.test/schema/parts/common.xsd", "https://example.test/schema/other.xsd" }));
            Assert.That(documents.All(document => document.IsReadOnly), Is.True);
            Assert.That(documents.All(document => document.DocumentNode!.IsReadOnly), Is.True);
        });
    }

    [Test]
    public void ResolvesRedefineOverrideAndDirectiveXmlBase()
    {
        SchemaResolver resolver = new SchemaResolver();
        resolver.Add("https://example.test/base/legacy.xsd", Schema("<xs:simpleType name='Legacy'><xs:restriction base='xs:string'/></xs:simpleType>", "urn:root"));
        resolver.Add("https://example.test/base/nested/current.xsd", Schema("<xs:simpleType name='Current'><xs:restriction base='xs:string'/></xs:simpleType>", null));
        string root = Schema(
            "<xs:redefine schemaLocation='legacy.xsd'/>" +
            "<xs:override xml:base='nested/' schemaLocation='current.xsd'/>",
            "urn:root");

        XmlSchemaSet result = Compile([Input(root, "https://example.test/base/root.xsd")], resolver);
        XmlSchemaModule module = ((CompiledXmlSchemaSet)result.CompiledSet!).Modules[0];

        Assert.Multiple(() =>
        {
            Assert.That(module.Directives, Has.Length.EqualTo(2));
            Assert.That(module.Directives[0].Kind, Is.EqualTo(XmlSchemaDirectiveKind.Redefine));
            Assert.That(module.Directives[0].TargetModule!.Relation, Is.EqualTo(XmlSchemaDirectiveKind.Redefine));
            Assert.That(module.Directives[0].TargetModule!.IsChameleon, Is.False);
            Assert.That(module.Directives[1].Kind, Is.EqualTo(XmlSchemaDirectiveKind.Override));
            Assert.That(module.Directives[1].TargetModule!.Relation, Is.EqualTo(XmlSchemaDirectiveKind.Override));
            Assert.That(module.Directives[1].TargetModule!.IsChameleon, Is.True);
            Assert.That(resolver.Requests, Is.EqualTo(new string[] { "https://example.test/base/legacy.xsd", "https://example.test/base/nested/current.xsd" }));
        });
    }

    [Test]
    public void TerminatesCyclesAndReusesRepeatedSchemaLocations()
    {
        SchemaResolver resolver = new SchemaResolver();
        resolver.Add("https://example.test/b.xsd", Schema("<xs:include schemaLocation='a.xsd'/>", "urn:cycle"));
        string root = Schema(
            "<xs:include schemaLocation='b.xsd'/>" +
            "<xs:include schemaLocation='b.xsd'/>",
            "urn:cycle");

        XmlSchemaSet result = Compile([Input(root, "https://example.test/a.xsd")], resolver);
        CompiledXmlSchemaSet compiled = (CompiledXmlSchemaSet)result.CompiledSet!;
        XmlSchemaModule a = compiled.Modules[0];
        XmlSchemaModule b = compiled.Modules[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaDocuments, Has.Length.EqualTo(2));
            Assert.That(compiled.Modules, Has.Length.EqualTo(2));
            Assert.That(a.Directives[0].TargetModule, Is.SameAs(b));
            Assert.That(a.Directives[1].TargetModule, Is.SameAs(b));
            Assert.That(b.Directives[0].TargetModule, Is.SameAs(a));
            Assert.That(a.IsComplete, Is.True);
            Assert.That(b.IsComplete, Is.True);
            Assert.That(resolver.Requests, Is.EqualTo(new string[] { "https://example.test/b.xsd" }));
        });
    }

    [Test]
    public void CompilesOneChameleonDocumentSeparatelyForEachIncludingNamespace()
    {
        SchemaResolver resolver = new SchemaResolver();
        resolver.Add("https://example.test/common.xsd", Schema("<xs:element name='shared' type='xs:string'/>", null));
        string first = Schema("<xs:include schemaLocation='common.xsd'/>", "urn:first");
        string second = Schema("<xs:include schemaLocation='common.xsd'/>", "urn:second");

        XmlSchemaSet result = Compile(
            [Input(first, "https://example.test/first.xsd"), Input(second, "https://example.test/second.xsd")],
            resolver);
        CompiledXmlSchemaSet compiled = (CompiledXmlSchemaSet)result.CompiledSet!;
        XmlSchemaModule firstChameleon = compiled.Modules[0].Directives[0].TargetModule!;
        XmlSchemaModule secondChameleon = compiled.Modules[2].Directives[0].TargetModule!;

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaDocuments, Has.Length.EqualTo(3));
            Assert.That(compiled.Modules, Has.Length.EqualTo(4));
            Assert.That(firstChameleon.Document, Is.SameAs(secondChameleon.Document));
            Assert.That(firstChameleon, Is.Not.SameAs(secondChameleon));
            Assert.That(firstChameleon.EffectiveTargetNamespace, Is.EqualTo("urn:first"));
            Assert.That(secondChameleon.EffectiveTargetNamespace, Is.EqualTo("urn:second"));
            Assert.That(resolver.Requests, Is.EqualTo(new string[] { "https://example.test/common.xsd" }));
        });
    }

    [Test]
    public void DeduplicatesRequestedAndResolvedAliasesForOneSchemaDocument()
    {
        SchemaResolver resolver = new SchemaResolver();
        resolver.AddRedirected(
            "https://example.test/alias.xsd",
            "https://example.test/canonical.xsd",
            Schema("<xs:element name='shared' type='xs:string'/>", "urn:root"));
        string root = Schema(
            "<xs:include schemaLocation='alias.xsd'/>" +
            "<xs:include schemaLocation='canonical.xsd'/>",
            "urn:root");

        XmlSchemaSet result = Compile([Input(root, "https://example.test/root.xsd")], resolver);
        XmlSchemaModule module = ((CompiledXmlSchemaSet)result.CompiledSet!).Modules[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaDocuments, Has.Length.EqualTo(2));
            Assert.That(module.Directives[0].TargetModule, Is.SameAs(module.Directives[1].TargetModule));
            Assert.That(module.Directives[0].TargetModule!.Location, Is.EqualTo("https://example.test/canonical.xsd"));
            Assert.That(resolver.Requests, Is.EqualTo(new string[] { "https://example.test/alias.xsd" }));
        });
    }

    [Test]
    public void CompilesMultipleRootsAndOptionalImportsDeterministically()
    {
        SchemaResolver resolver = new SchemaResolver();
        string first = Schema("<xs:import namespace='urn:unavailable'/>", "urn:first");
        string second = Schema("<xs:element name='local' type='xs:string'/>", null);
        string duplicateNamespace = Schema("<xs:element name='again' type='xs:string'/>", "urn:first");

        XmlSchemaSet result = Compile(
            [
                Input(first, "https://example.test/first.xsd"),
                Input(second, "https://example.test/local.xsd"),
                Input(duplicateNamespace, "https://example.test/again.xsd")
            ],
            resolver);
        CompiledXmlSchemaSet compiled = (CompiledXmlSchemaSet)result.CompiledSet!;

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaDocuments, Has.Length.EqualTo(3));
            Assert.That(compiled.Modules, Has.Length.EqualTo(3));
            Assert.That(result.TargetNamespaces, Is.EqualTo(new string[] { "urn:first", "" }));
            Assert.That(compiled.Modules[0].Directives[0].TargetModule, Is.Null);
            Assert.That(compiled.Modules[0].Directives[0].NamespaceUri, Is.EqualTo("urn:unavailable"));
            Assert.That(resolver.Requests, Is.Empty);
        });
    }

    [TestCase("include", "urn:wrong", null)]
    [TestCase("redefine", "urn:wrong", null)]
    [TestCase("override", "urn:wrong", null)]
    [TestCase("import", "urn:wrong", "urn:expected")]
    public void RejectsReferencedSchemasWithIncompatibleTargetNamespaces(string directiveName, string loadedNamespace, string? importedNamespace)
    {
        SchemaResolver resolver = new SchemaResolver();
        resolver.Add("https://example.test/reference.xsd", Schema("", loadedNamespace));
        string namespaceAttribute = importedNamespace == null ? "" : " namespace='" + importedNamespace + "'";
        string root = Schema("<xs:" + directiveName + namespaceAttribute + " schemaLocation='reference.xsd'/>", "urn:root");

        XmlSchemaException exception = Assert.Throws<XmlSchemaException>(() => Compile([Input(root, "https://example.test/root.xsd")], resolver))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SchemaFailure));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Schema));
            Assert.That(exception.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
            Assert.That(exception.Location!.SourceUri, Is.EqualTo("https://example.test/reference.xsd"));
        });
    }

    [TestCase("<root/>")]
    [TestCase("<schema/>")]
    [TestCase("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='' />")]
    [TestCase("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:include/></xs:schema>")]
    [TestCase("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:redefine/></xs:schema>")]
    [TestCase("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:override/></xs:schema>")]
    [TestCase("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='urn:self'><xs:import namespace='urn:self'/></xs:schema>")]
    [TestCase("<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:import/></xs:schema>")]
    public void RejectsInvalidSchemaRootsNamespacesAndDirectives(string source)
    {
        XmlSchemaException exception = Assert.Throws<XmlSchemaException>(() => Compile([Input(source, "https://example.test/root.xsd")], new SchemaResolver()))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SchemaFailure));
    }

    [Test]
    public void RequiresExplicitResourceAccessAndEnforcesSchemaResourceLimits()
    {
        string root = Schema("<xs:include schemaLocation='one.xsd'/><xs:include schemaLocation='two.xsd'/>", "urn:root");
        SchemaResolver deniedResolver = new SchemaResolver();
        XmlResourceException denied = Assert.Throws<XmlResourceException>(() => new XmlSchemaCompiler().CompileXmlSchemas(
            [Input(root, "https://example.test/root.xsd")],
            new XmlSchemaValidationOptions(),
            deniedResolver))!;

        SchemaResolver limitedResolver = new SchemaResolver();
        limitedResolver.Add("https://example.test/one.xsd", Schema("", "urn:root"));
        limitedResolver.Add("https://example.test/two.xsd", Schema("", "urn:root"));
        XmlSchemaValidationOptions limitedOptions = Options();
        limitedOptions.SecurityLimits = Limits(1);
        XmlResourceException limited = Assert.Throws<XmlResourceException>(() => new XmlSchemaCompiler().CompileXmlSchemas(
            [Input(root, "https://example.test/root.xsd")],
            limitedOptions,
            limitedResolver))!;

        Assert.Multiple(() =>
        {
            Assert.That(denied.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceDenied));
            Assert.That(deniedResolver.Requests, Is.Empty);
            Assert.That(limited.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(limited.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Limit));
            Assert.That(limitedResolver.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ValidatesArgumentsAndProducesAnImmutableEmptyCompiledSet()
    {
        XmlSchemaCompiler compiler = new XmlSchemaCompiler();
        XmlSchemaValidationOptions options = Options();
        SchemaResolver resolver = new SchemaResolver();
        XmlSchemaSet empty = compiler.CompileXmlSchemas([], options, resolver);

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsImmutable, Is.True);
            Assert.That(empty.SchemaDocuments, Is.Empty);
            Assert.That(empty.TargetNamespaces, Is.Empty);
            Assert.That(empty.Diagnostics, Is.Empty);
            Assert.That(((CompiledXmlSchemaSet)empty.CompiledSet!).Modules, Is.Empty);
            Assert.Throws<ArgumentNullException>(() => compiler.CompileXmlSchemas(null!, options, resolver));
            Assert.Throws<ArgumentNullException>(() => compiler.CompileXmlSchemas([], null!, resolver));
            Assert.Throws<ArgumentNullException>(() => compiler.CompileXmlSchemas([], options, null!));
            Assert.Throws<ArgumentException>(() => compiler.CompileXmlSchemas([null!], options, resolver));
        });
    }

    private static XmlSchemaSet Compile(XmlInput[] inputs, SchemaResolver resolver)
    {
        return new XmlSchemaCompiler().CompileXmlSchemas(inputs, Options(), resolver);
    }

    private static XmlSchemaValidationOptions Options()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https"],
            MaximumDepth = 8,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 2097152
        };
        return new XmlSchemaValidationOptions { ResolverPolicy = policy };
    }

    private static XmlSecurityLimits Limits(int maximumExternalResources)
    {
        return new XmlSecurityLimits
        {
            MaximumDepth = 256,
            MaximumNodeCount = 1000000,
            MaximumAttributesPerElement = 10000,
            MaximumNameLength = 16384,
            MaximumTextLength = 67108864,
            MaximumTokenLength = 67108864,
            MaximumInputCharacters = 67108864,
            MaximumInputBytes = 134217728,
            MaximumDtdCharacters = 16777216,
            MaximumEntityDepth = 32,
            MaximumEntityCount = 100000,
            MaximumEntityCharacters = 67108864,
            MaximumEntityAmplification = 100,
            MaximumExternalResources = maximumExternalResources,
            MaximumResourceDepth = 4,
            MaximumResourceBytes = 67108864,
            CancellationPollingInterval = 1024
        };
    }

    private static XmlInput Input(string source, string baseUri)
    {
        return new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = baseUri };
    }

    private static string Schema(string content, string? targetNamespace)
    {
        string target = targetNamespace == null ? "" : " targetNamespace='" + targetNamespace + "'";
        return "<xs:schema xmlns:xs='" + Xsd + "'" + target + ">" + content + "</xs:schema>";
    }

    private sealed class SchemaResolver : IXmlResourceResolver
    {
        private readonly Dictionary<string, string> resources = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> resolvedLocations = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly List<string> Requests = new List<string>();

        public void Add(string absoluteUri, string source)
        {
            resources.Add(absoluteUri, source);
        }

        public void AddRedirected(string requestedUri, string absoluteUri, string source)
        {
            resources.Add(requestedUri, source);
            resolvedLocations.Add(requestedUri, absoluteUri);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Assert.That(kind, Is.EqualTo(XmlResourceKind.Schema));
            Assert.That(publicIdentifier, Is.Null);
            Assert.That(policy.Access, Is.EqualTo(XmlResourceAccess.Allowed));
            string absolute = new Uri(new Uri(baseUri!), systemIdentifier!).AbsoluteUri;
            Requests.Add(absolute);
            if (!resources.ContainsKey(absolute))
            {
                throw new InvalidOperationException("No schema resource was registered for " + absolute + ".");
            }

            string resolved = resolvedLocations.ContainsKey(absolute) ? resolvedLocations[absolute] : absolute;
            return new XmlResolvedResource
            {
                Kind = XmlResourceKind.Schema,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = resolved,
                SystemIdentifier = systemIdentifier,
                Input = new XmlInput { Kind = XmlInputKind.String, Text = resources[absolute], BaseUri = resolved }
            };
        }
    }
}
