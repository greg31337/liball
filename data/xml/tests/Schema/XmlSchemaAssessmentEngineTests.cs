using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaAssessmentEngineTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";
    private const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    [Test]
    public void ValidatesSequenceChoiceOccurrencesAndAssessesChildren()
    {
        string schema = Schema(
            "<xs:group name='content'><xs:sequence>" +
            "<xs:element name='a' minOccurs='1' maxOccurs='2'/><xs:choice><xs:element name='b'/><xs:element name='c'/></xs:choice>" +
            "</xs:sequence></xs:group><xs:element name='root'><xs:complexType><xs:group ref='t:content'/></xs:complexType></xs:element>",
            "");

        XmlSchemaAssessmentResult valid = Assess(schema, "<root xmlns='urn:test'><a/><a/><c/></root>");
        XmlSchemaAssessmentResult wrongOrder = Assess(schema, "<root xmlns='urn:test'><c/><a/></root>");
        XmlSchemaAssessmentResult tooMany = Assess(schema, "<root xmlns='urn:test'><a/><a/><a/><b/></root>");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(valid.Diagnostics, Is.Empty);
            Assert.That(valid.Assessments.Count(value => value.ElementDeclaration != null), Is.EqualTo(4));
            Assert.That(valid.Assessments.Where(value => value.Node.Kind == XmlNodeKind.Element).All(value => value.Validity == XmlSchemaValidity.Valid), Is.True);
            Assert.That(wrongOrder.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(tooMany.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
        });
    }

    [Test]
    public void ValidatesAllParticlesInDocumentOrderIndependently()
    {
        string schema = Schema(
            "<xs:element name='root'><xs:complexType><xs:all><xs:element name='a'/><xs:element name='b' minOccurs='0'/><xs:element name='c'/></xs:all></xs:complexType></xs:element>",
            "");

        Assert.Multiple(() =>
        {
            Assert.That(Assess(schema, "<root xmlns='urn:test'><c/><b/><a/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Assess(schema, "<root xmlns='urn:test'><a/><c/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Assess(schema, "<root xmlns='urn:test'><a/><b/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
        });
    }

    [Test]
    public void ResolvesSubstitutionGroupMembersDuringContentValidation()
    {
        string schema = Schema(
            "<xs:element name='head' abstract='true'/><xs:element name='member' substitutionGroup='t:head'/>" +
            "<xs:element name='root'><xs:complexType><xs:sequence><xs:element ref='t:head'/></xs:sequence></xs:complexType></xs:element>",
            "");

        XmlSchemaAssessmentResult member = Assess(schema, "<root xmlns='urn:test'><member/></root>");
        XmlSchemaAssessmentResult head = Assess(schema, "<root xmlns='urn:test'><head/></root>");

        Assert.Multiple(() =>
        {
            Assert.That(member.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(member.Assessments.Single(value => value.Node.Name!.LocalName == "member").ElementDeclaration!.Name.LocalName, Is.EqualTo("member"));
            Assert.That(head.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(head.Diagnostics.Any(value => value.Message!.Contains("abstract", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void AppliesStrictLaxAndSkipElementWildcards()
    {
        string schema = Schema(
            "<xs:element name='strict'/><xs:element name='lax'/>" +
            "<xs:element name='root'><xs:complexType><xs:sequence>" +
            "<xs:any namespace='##targetNamespace' processContents='strict'/>" +
            "<xs:any namespace='##targetNamespace' processContents='lax'/>" +
            "<xs:any namespace='##targetNamespace' processContents='skip'/>" +
            "</xs:sequence></xs:complexType></xs:element>",
            "");
        XmlSchemaAssessmentResult result = Assess(schema, "<root xmlns='urn:test'><strict/><lax/><unknown/></root>");
        XmlSchemaNodeAssessment strict = result.Assessments.Single(value => value.Node.Name!.LocalName == "strict");
        XmlSchemaNodeAssessment lax = result.Assessments.Single(value => value.Node.Name!.LocalName == "lax");
        XmlSchemaNodeAssessment skipped = result.Assessments.Single(value => value.Node.Name!.LocalName == "unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(strict.ProcessContents, Is.EqualTo(XmlSchemaProcessContents.Strict));
            Assert.That(strict.ElementDeclaration, Is.Not.Null);
            Assert.That(lax.ProcessContents, Is.EqualTo(XmlSchemaProcessContents.Lax));
            Assert.That(lax.ElementDeclaration, Is.Not.Null);
            Assert.That(skipped.ProcessContents, Is.EqualTo(XmlSchemaProcessContents.Skip));
            Assert.That(skipped.ElementDeclaration, Is.Null);
            Assert.That(skipped.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
        });
    }

    [Test]
    public void RejectsUndeclaredStrictWildcardContentAndExcludedNames()
    {
        string strictSchema = Schema(
            "<xs:element name='root'><xs:complexType><xs:sequence><xs:any processContents='strict'/></xs:sequence></xs:complexType></xs:element>",
            "");
        string excludedSchema = Schema(
            "<xs:element name='blocked'/><xs:element name='root'><xs:complexType><xs:sequence><xs:any notQName='##defined' processContents='skip'/></xs:sequence></xs:complexType></xs:element>",
            "");

        Assert.Multiple(() =>
        {
            Assert.That(Assess(strictSchema, "<root xmlns='urn:test'><other/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Assess(excludedSchema, "<root xmlns='urn:test'><blocked/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Assess(excludedSchema, "<root xmlns='urn:test'><other/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
        });
    }

    [Test]
    public void ValidatesDeclaredAttributesAndAttributeWildcards()
    {
        string schema = Schema(
            "<xs:attribute name='extra' type='xs:string'/>" +
            "<xs:element name='root'><xs:complexType>" +
            "<xs:attribute name='id' type='xs:int' use='required'/><xs:attribute name='removed' use='prohibited'/>" +
            "<xs:anyAttribute namespace='##targetNamespace' processContents='lax'/>" +
            "</xs:complexType></xs:element>",
            "");

        XmlSchemaAssessmentResult valid = Assess(schema, "<root xmlns='urn:test' xmlns:t='urn:test' id='1' t:extra='x' t:unknown='y'/>");
        XmlSchemaNodeAssessment extra = valid.Assessments.Single(value => value.Node.Name!.LocalName == "extra");
        XmlSchemaNodeAssessment unknown = valid.Assessments.Single(value => value.Node.Name!.LocalName == "unknown");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(extra.AttributeDeclaration, Is.Not.Null);
            Assert.That(extra.ProcessContents, Is.EqualTo(XmlSchemaProcessContents.Lax));
            Assert.That(unknown.AttributeDeclaration, Is.Null);
            Assert.That(unknown.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(Assess(schema, "<root xmlns='urn:test'/>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Assess(schema, "<root xmlns='urn:test' id='1' removed='x'/>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Assess(schema, "<root xmlns='urn:test' id='1' other='x'/>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
        });
    }

    [Test]
    public void SelectsValidDerivedXsiTypesAndRejectsBlockedOrUnknownTypes()
    {
        string schema = Schema(
            "<xs:complexType name='Base'><xs:sequence><xs:element name='a'/></xs:sequence></xs:complexType>" +
            "<xs:complexType name='Derived'><xs:complexContent><xs:extension base='t:Base'><xs:sequence><xs:element name='b'/></xs:sequence></xs:extension></xs:complexContent></xs:complexType>" +
            "<xs:element name='root' type='t:Base'/><xs:element name='blocked' type='t:Base' block='extension'/>",
            "");

        XmlSchemaAssessmentResult valid = Assess(schema, "<root xmlns='urn:test' xmlns:xsi='" + Xsi + "' xmlns:t='urn:test' xsi:type='t:Derived'><a/><b/></root>");
        XmlSchemaNodeAssessment root = valid.Assessments[0];

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(root.ComplexType!.Name!.LocalName, Is.EqualTo("Derived"));
            Assert.That(Assess(schema, "<blocked xmlns='urn:test' xmlns:xsi='" + Xsi + "' xmlns:t='urn:test' xsi:type='t:Derived'><a/><b/></blocked>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Assess(schema, "<root xmlns='urn:test' xmlns:xsi='" + Xsi + "' xmlns:t='urn:test' xsi:type='t:Missing'><a/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
        });
    }

    [Test]
    public void AllowsAConcreteSimpleXsiTypeForAnAnyTypeDeclaration()
    {
        string schema = Schema("<xs:element name='root'/>", "");
        string instance = "<root xmlns='urn:test' xmlns:xsi='" + Xsi + "' xmlns:xs='" + Xsd + "' xsi:type='xs:string'>value</root>";

        XmlSchemaAssessmentResult result = Assess(schema, instance);

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(result.Assessments[0].SimpleType!.AtomicType, Is.EqualTo(XdmAtomicType.String));
        });
    }

    [Test]
    public void EnforcesXsiNilDeclarationAndContentRules()
    {
        string schema = Schema("<xs:element name='allowed' nillable='true'/><xs:element name='denied'/>", "");

        XmlSchemaAssessmentResult allowed = Assess(schema, "<allowed xmlns='urn:test' xmlns:xsi='" + Xsi + "' xsi:nil='true'/>");
        XmlSchemaAssessmentResult content = Assess(schema, "<allowed xmlns='urn:test' xmlns:xsi='" + Xsi + "' xsi:nil='1'>text</allowed>");
        XmlSchemaAssessmentResult denied = Assess(schema, "<denied xmlns='urn:test' xmlns:xsi='" + Xsi + "' xsi:nil='true'/>");
        XmlSchemaAssessmentResult lexical = Assess(schema, "<allowed xmlns='urn:test' xmlns:xsi='" + Xsi + "' xsi:nil='yes'/>");

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(allowed.Assessments[0].IsNilled, Is.True);
            Assert.That(content.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(denied.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(lexical.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
        });
    }

    [Test]
    public void ValidatesSuffixAndInterleaveOpenContent()
    {
        string suffix = Schema(
            "<xs:element name='root'><xs:complexType><xs:openContent mode='suffix'><xs:any namespace='##other' processContents='skip'/></xs:openContent><xs:sequence><xs:element name='a'/></xs:sequence></xs:complexType></xs:element>",
            "");
        string interleave = Schema(
            "<xs:element name='root'><xs:complexType><xs:openContent mode='interleave'><xs:any namespace='##other' processContents='skip'/></xs:openContent><xs:sequence><xs:element name='a'/></xs:sequence></xs:complexType></xs:element>",
            "");
        string suffixInstance = "<root xmlns='urn:test' xmlns:o='urn:other'><a/><o:x/></root>";
        string interleaveInstance = "<root xmlns='urn:test' xmlns:o='urn:other'><o:x/><a/><o:y/></root>";

        Assert.Multiple(() =>
        {
            Assert.That(Assess(suffix, suffixInstance).Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Assess(suffix, interleaveInstance).Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Assess(interleave, interleaveInstance).Validity, Is.EqualTo(XmlSchemaValidity.Valid));
        });
    }

    [Test]
    public void EnforcesSimpleAndElementOnlyCharacterContentRules()
    {
        string simple = Schema("<xs:element name='root' type='xs:string'/>", "");
        string elementOnly = Schema("<xs:element name='root'><xs:complexType><xs:sequence><xs:element name='a'/></xs:sequence></xs:complexType></xs:element>", "");

        Assert.Multiple(() =>
        {
            Assert.That(Assess(simple, "<root xmlns='urn:test'>text</root>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Assess(simple, "<root xmlns='urn:test'><child/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Assess(elementOnly, "<root xmlns='urn:test'>  <a/>  </root>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Assess(elementOnly, "<root xmlns='urn:test'>text<a/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
        });
    }

    [Test]
    public void ParsesSchemaLocationHintsAndAppliesResolverPolicy()
    {
        string schema = Schema("<xs:element name='root'/>", "");
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions
        {
            UseLocationHints = true,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Allowed, AllowedSchemes = ["https"] }
        };
        string instance = "<root xmlns='urn:test' xmlns:xsi='" + Xsi + "' xsi:schemaLocation='urn:test ../schemas/test.xsd urn:other http://example.test/other.xsd' xsi:noNamespaceSchemaLocation='local.xsd'/>";

        XmlSchemaAssessmentResult result = Assess(schema, instance, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(result.LocationHints, Has.Length.EqualTo(3));
            Assert.That(result.LocationHints[0].AbsoluteUri, Is.EqualTo("https://example.test/schemas/test.xsd"));
            Assert.That(result.LocationHints[0].IsAllowed, Is.True);
            Assert.That(result.LocationHints[1].IsAllowed, Is.False);
            Assert.That(result.LocationHints[2].AbsoluteUri, Is.EqualTo("https://example.test/docs/local.xsd"));
            Assert.That(result.LocationHints[2].IsAllowed, Is.True);
        });

        XmlSchemaAssessmentResult malformed = Assess(schema, "<root xmlns='urn:test' xmlns:xsi='" + Xsi + "' xsi:schemaLocation='urn:test'/>", options);
        Assert.That(malformed.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
    }

    [Test]
    public void LoadsAnUndeclaredRootSchemaFromAnAllowedLocationHint()
    {
        string initial = Schema("<xs:element name='known'/>", "");
        string hinted = "<xs:schema xmlns:xs='" + Xsd + "' targetNamespace='urn:hint'><xs:element name='root'><xs:complexType><xs:attribute name='id' use='required'/></xs:complexType></xs:element></xs:schema>";
        MemoryResolver resolver = new MemoryResolver();
        resolver.Add("https://example.test/schemas/hint.xsd", hinted);
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions
        {
            UseLocationHints = true,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Allowed, AllowedSchemes = ["https"] }
        };

        XmlSchemaAssessmentResult result = AssessWithResolver(
            initial,
            "<root xmlns='urn:hint' xmlns:xsi='" + Xsi + "' xsi:schemaLocation='urn:hint ../schemas/hint.xsd' id='x'/>",
            options,
            resolver);

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(result.Assessments[0].ElementDeclaration!.Name.NamespaceUri, Is.EqualTo("urn:hint"));
            Assert.That(resolver.Requests, Is.EqualTo(new[] { "https://example.test/schemas/hint.xsd" }));
        });

        MemoryResolver deniedResolver = new MemoryResolver();
        deniedResolver.Add("http://example.test/hint.xsd", hinted);
        XmlSchemaAssessmentResult denied = AssessWithResolver(
            initial,
            "<root xmlns='urn:hint' xmlns:xsi='" + Xsi + "' xsi:schemaLocation='urn:hint http://example.test/hint.xsd' id='x'/>",
            options,
            deniedResolver);
        Assert.Multiple(() =>
        {
            Assert.That(denied.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(deniedResolver.Requests, Is.Empty);
        });
    }

    [Test]
    public void HonorsFailFastWhenCollectingAssessmentDiagnostics()
    {
        string schema = Schema("<xs:element name='root'><xs:complexType><xs:attribute name='a' use='required'/><xs:attribute name='b' use='required'/></xs:complexType></xs:element>", "");
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions { FailFast = true };

        XmlSchemaAssessmentResult result = Assess(schema, "<root xmlns='urn:test' unexpected='x'/>", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        });
    }

    private static XmlSchemaAssessmentResult Assess(string schema, string instance)
    {
        return Assess(schema, instance, new XmlSchemaValidationOptions());
    }

    private static XmlSchemaAssessmentResult Assess(string schema, string instance, XmlSchemaValidationOptions options)
    {
        CompiledXmlSchemaSet compiled = Compile(schema);
        XmlDocument document = ParseInstance(instance);
        return new XmlSchemaAssessmentEngine(compiled, options, new RejectingResolver()).AssessXmlSchemaDocument(document);
    }

    private static XmlSchemaAssessmentResult AssessWithResolver(string schema, string instance, XmlSchemaValidationOptions options, IXmlResourceResolver resolver)
    {
        CompiledXmlSchemaSet compiled = Compile(schema);
        XmlDocument document = ParseInstance(instance);
        return new XmlSchemaAssessmentEngine(compiled, options, resolver).AssessXmlSchemaDocument(document);
    }

    private static XmlDocument ParseInstance(string instance)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = instance, BaseUri = "https://example.test/docs/instance.xml" };
        XmlParserOptions parserOptions = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
        return new XmlParser().ParseXmlDocument(input, parserOptions, null, null, null);
    }

    private static CompiledXmlSchemaSet Compile(string schema)
    {
        XmlResolverPolicy policy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied, AllowedSchemes = [] };
        XmlSchemaSet result = new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = schema, BaseUri = "https://example.test/schema.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = policy },
            new RejectingResolver());
        return (CompiledXmlSchemaSet)result.CompiledSet!;
    }

    private static string Schema(string content, string extraAttributes)
    {
        return "<xs:schema xmlns:xs='" + Xsd + "' xmlns:t='urn:test' targetNamespace='urn:test' elementFormDefault='qualified'" + extraAttributes + ">" + content + "</xs:schema>";
    }

    private sealed class RejectingResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            throw new InvalidOperationException("No external resource was expected.");
        }
    }

    private sealed class MemoryResolver : IXmlResourceResolver
    {
        private readonly Dictionary<string, string> resources = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly List<string> Requests = new List<string>();

        public void Add(string absoluteUri, string source)
        {
            resources.Add(absoluteUri, source);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string absolute = baseUri == null ? systemIdentifier! : new Uri(new Uri(baseUri), systemIdentifier!).AbsoluteUri;
            Requests.Add(absolute);
            if (!resources.ContainsKey(absolute))
            {
                throw new InvalidOperationException("No schema was registered for " + absolute + ".");
            }

            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = absolute,
                Input = new XmlInput { Kind = XmlInputKind.String, Text = resources[absolute], BaseUri = absolute }
            };
        }
    }
}
