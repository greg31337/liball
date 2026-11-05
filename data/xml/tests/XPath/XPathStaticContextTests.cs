using xml;

namespace xml.Tests;

[TestFixture]
public class XPathStaticContextTests
{
    [Test]
    public void RejectsTheNamespaceDeclarationNamespaceInExpandedNames()
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("/Q{ http://www.w3.org/2000/xmlns/}element", new XPathStaticContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XQST0070));
    }

    [Test]
    public void ConstructsStandardDefaultsAndPredeclaredNamespaces()
    {
        XPathStaticEnvironment environment = new XPathStaticEnvironment(new XPathStaticContext());

        Assert.Multiple(() =>
        {
            Assert.That(environment.DefaultElementNamespace, Is.Empty);
            Assert.That(environment.DefaultFunctionNamespace, Is.EqualTo(XPathNamespaces.Functions));
            Assert.That(environment.DefaultCollation, Is.EqualTo(XPathNamespaces.CodepointCollation));
            Assert.That(environment.BaseUri, Is.Null);
            Assert.That(environment.FindXPathNamespace("xml"), Is.EqualTo(XmlNamespaces.Xml));
            Assert.That(environment.FindXPathNamespace("xs"), Is.EqualTo(XPathNamespaces.XmlSchema));
            Assert.That(environment.FindXPathNamespace("fn"), Is.EqualTo(XPathNamespaces.Functions));
            Assert.That(environment.FindXPathNamespace("math"), Is.EqualTo(XPathNamespaces.Math));
            Assert.That(environment.FindXPathNamespace("map"), Is.EqualTo(XPathNamespaces.Map));
            Assert.That(environment.FindXPathNamespace("array"), Is.EqualTo(XPathNamespaces.Array));
            Assert.That(environment.FindXPathNamespace("err"), Is.EqualTo(XPathNamespaces.Errors));
            Assert.That(environment.HasXPathCollation(XPathNamespaces.CodepointCollation), Is.True);
            Assert.That(environment.Variables, Is.Empty);
            Assert.That(environment.Functions, Is.Empty);
            Assert.That(environment.DecimalFormats, Is.Empty);
            Assert.That(environment.EnabledFeatures, Is.Empty);
        });
    }

    [Test]
    public void SnapshotsCustomContextRegistriesAndFeatureFlags()
    {
        XmlNamespaceBinding prefix = Namespace("p", "urn:original");
        XPathVariableBinding variable = Variable(null, "external", "value");
        XPathFunctionBinding function = Function("urn:functions", "make", 2, "make-2");
        XmlName decimalFormat = Name("urn:formats", "decimal");
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [prefix],
            DefaultElementNamespace = "urn:elements",
            DefaultFunctionNamespace = "urn:functions",
            BaseUri = "https://example.test/query/../query/main.xpath",
            DefaultCollation = "urn:collation:test",
            Collations = ["urn:collation:test"],
            Variables = [variable],
            Functions = [function],
            DecimalFormats = [decimalFormat],
            EnabledFeatures = ["vendor-feature", "vendor-feature"],
            SchemaAware = true,
            AllowHigherOrderFunctions = true
        };

        XPathStaticEnvironment environment = new XPathStaticEnvironment(context);
        prefix.NamespaceUri = "urn:changed";
        variable.Name!.LocalName = "changed";
        function.BindingKey = "changed";
        decimalFormat.LocalName = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(environment.FindXPathNamespace("p"), Is.EqualTo("urn:original"));
            Assert.That(environment.BaseUri, Is.EqualTo("https://example.test/query/main.xpath"));
            Assert.That(environment.HasXPathCollation("urn:collation:test"), Is.True);
            Assert.That(environment.Variables[0].Name!.LocalName, Is.EqualTo("external"));
            Assert.That(environment.Functions[0].BindingKey, Is.EqualTo("make-2"));
            Assert.That(environment.DecimalFormats[0].LocalName, Is.EqualTo("decimal"));
            Assert.That(environment.EnabledFeatures, Is.EqualTo(new[] { "vendor-feature", "schema-aware", "higher-order-functions" }));
            Assert.That(environment.HasXPathFeature("schema-aware"), Is.True);
            Assert.That(environment.HasXPathFeature("higher-order-functions"), Is.True);
        });
    }

    [Test]
    public void ResolvesNamesUsingTheNamespaceRulesForEachNameRole()
    {
        XPathStaticEnvironment environment = new XPathStaticEnvironment(new XPathStaticContext
        {
            Namespaces = [Namespace("p", "urn:prefix")],
            DefaultElementNamespace = "urn:elements",
            DefaultFunctionNamespace = "urn:functions"
        });
        SourceRange range = Range();

        XmlName prefixed = environment.ResolveXPathName("p", "item", null, XPathNameUse.Element, range);
        XmlName element = environment.ResolveXPathName(null, "item", null, XPathNameUse.Element, range);
        XmlName type = environment.ResolveXPathName(null, "type", null, XPathNameUse.Type, range);
        XmlName function = environment.ResolveXPathName(null, "call", null, XPathNameUse.Function, range);
        XmlName variable = environment.ResolveXPathName(null, "value", null, XPathNameUse.Variable, range);
        XmlName attribute = environment.ResolveXPathName(null, "id", null, XPathNameUse.Attribute, range);
        XmlName braced = environment.ResolveXPathName(null, "item", "urn:explicit", XPathNameUse.Element, range);

        Assert.Multiple(() =>
        {
            Assert.That(prefixed.NamespaceUri, Is.EqualTo("urn:prefix"));
            Assert.That(element.NamespaceUri, Is.EqualTo("urn:elements"));
            Assert.That(type.NamespaceUri, Is.EqualTo("urn:elements"));
            Assert.That(function.NamespaceUri, Is.EqualTo("urn:functions"));
            Assert.That(variable.NamespaceUri, Is.Null);
            Assert.That(attribute.NamespaceUri, Is.Null);
            Assert.That(braced.NamespaceUri, Is.EqualTo("urn:explicit"));
        });
    }

    [Test]
    public void BindsExternalVariablesAndFunctionsByExpandedNameAndArity()
    {
        XPathStaticContext context = new XPathStaticContext
        {
            DefaultFunctionNamespace = "urn:functions",
            Variables = [Variable(null, "external", "value")],
            Functions =
            [
                Function("urn:functions", "make", 1, "make-1"),
                Function("urn:functions", "make", 2, "make-2")
            ]
        };
        XPathAstNode result = Bind("($external, make(1), make#2, 1 => make(2))", context);
        XPathAstNode[] expressions = result.Operand!.Children;

        Assert.Multiple(() =>
        {
            Assert.That(expressions[0].VariableBinding!.Value!.AtomicValues![0].LexicalValue, Is.EqualTo("value"));
            Assert.That(expressions[1].FunctionBinding!.BindingKey, Is.EqualTo("make-1"));
            Assert.That(expressions[1].ResolvedName!.NamespaceUri, Is.EqualTo("urn:functions"));
            Assert.That(expressions[2].FunctionBinding!.BindingKey, Is.EqualTo("make-2"));
            Assert.That(expressions[3].Right!.FunctionBinding!.BindingKey, Is.EqualTo("make-2"));
        });
    }

    [Test]
    public void BindsForClausesAndInlineParametersWithLexicalScope()
    {
        XPathStaticContext context = new XPathStaticContext
        {
            Variables = [Variable(null, "external", "outside")]
        };
        XPathAstNode result = Bind("for $x in $external, $y in $x return function($p as xs:string) as xs:string {($x, $y, $p)}", context);
        XPathAstNode inline = result.Body!;
        XPathAstNode[] references = inline.Body!.Operand!.Children;

        Assert.Multiple(() =>
        {
            Assert.That(result.Bindings[0].Expression.VariableBinding, Is.Not.Null);
            Assert.That(result.Bindings[1].Expression.LocalBinding, Is.SameAs(result.Bindings[0]));
            Assert.That(references[0].LocalBinding, Is.SameAs(result.Bindings[0]));
            Assert.That(references[1].LocalBinding, Is.SameAs(result.Bindings[1]));
            Assert.That(references[2].ParameterBinding, Is.SameAs(inline.Parameters[0]));
            Assert.That(inline.Parameters[0].Type!.AtomicType!.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(inline.Type!.AtomicType!.Type, Is.EqualTo(XdmAtomicType.String));
        });
    }

    [Test]
    public void ResolvesPathNameTestsWildcardsAndTypeNames()
    {
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [Namespace("p", "urn:prefix")],
            DefaultElementNamespace = "urn:elements"
        };
        XPathAstNode path = Bind("book/@id/p:item/p:*/element(book, p:type?)/attribute(code, xs:ID)", context);
        XPathAstNode cast = Bind("1 cast as xs:int", context);

        Assert.Multiple(() =>
        {
            Assert.That(path.Children[0].ResolvedName!.NamespaceUri, Is.EqualTo("urn:elements"));
            Assert.That(path.Children[1].ResolvedName!.NamespaceUri, Is.Null);
            Assert.That(path.Children[2].ResolvedName!.NamespaceUri, Is.EqualTo("urn:prefix"));
            Assert.That(path.Children[3].NodeTest!.NamespaceUri, Is.EqualTo("urn:prefix"));
            Assert.That(path.Children[4].NodeTest!.ResolvedName!.NamespaceUri, Is.EqualTo("urn:elements"));
            Assert.That(path.Children[4].NodeTest!.Type!.ResolvedName!.NamespaceUri, Is.EqualTo("urn:prefix"));
            Assert.That(path.Children[5].NodeTest!.ResolvedName!.NamespaceUri, Is.Null);
            Assert.That(path.Children[5].NodeTest!.Type!.AtomicType!.Type, Is.EqualTo(XdmAtomicType.Id));
            Assert.That(cast.Type!.AtomicType!.Type, Is.EqualTo(XdmAtomicType.Int));
        });
    }

    [Test]
    public void ResolvesImportedSchemaTypeNamesWhenSchemaAware()
    {
        XmlSchemaSet schemaSet = CompileSchema();
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [Namespace("t", "urn:types")],
            SchemaAware = true,
            SchemaSet = schemaSet
        };

        XPathAstNode result = Bind("1 instance of t:Code", context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Type!.ResolvedName!.NamespaceUri, Is.EqualTo("urn:types"));
            Assert.That(result.Type.SchemaType, Is.True);
            Assert.That(result.Type.AtomicType, Is.Null);
        });
    }

    [Test]
    public void ReportsUnboundPrefixesAtTheirExpressionRange()
    {
        XPathAstNode expression = new XPathParser("$missing:value", "urn:query").ParseXPathExpression();

        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => new XPathStaticBinder(new XPathStaticContext()).BindXPathExpression(expression))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0081));
            Assert.That(exception.Diagnostic.Range, Is.SameAs(expression.Range));
            Assert.That(exception.Location!.SourceUri, Is.EqualTo("urn:query"));
            Assert.That(exception.Location.CharacterOffset, Is.Zero);
        });
    }

    [Test]
    public void RejectsStructurallyInvalidStaticContexts()
    {
        XPathStaticContext relativeBase = new XPathStaticContext { BaseUri = "relative/path" };
        XPathStaticContext fragmentedBase = new XPathStaticContext { BaseUri = "https://example.test/query#part" };
        XPathStaticContext unavailableCollation = new XPathStaticContext { DefaultCollation = "urn:missing" };
        XPathStaticContext invalidXml = new XPathStaticContext { Namespaces = [Namespace("xml", "urn:not-xml")] };
        XPathStaticContext duplicateVariables = new XPathStaticContext
        {
            Variables = [Variable(null, "same", "one"), Variable(null, "same", "two")]
        };
        XPathFunctionBinding malformedFunction = Function("urn:functions", "bad", 2, "bad");
        malformedFunction.ParameterTypes = [XdmAtomicType.String];
        XPathStaticContext malformedFunctions = new XPathStaticContext { Functions = [malformedFunction] };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new XPathStaticEnvironment(relativeBase));
            Assert.Throws<ArgumentException>(() => new XPathStaticEnvironment(fragmentedBase));
            Assert.Throws<ArgumentException>(() => new XPathStaticEnvironment(unavailableCollation));
            Assert.Throws<ArgumentException>(() => new XPathStaticEnvironment(invalidXml));
            Assert.Throws<ArgumentException>(() => new XPathStaticEnvironment(duplicateVariables));
            Assert.Throws<ArgumentException>(() => new XPathStaticEnvironment(malformedFunctions));
        });
    }

    private static XPathAstNode Bind(string source, XPathStaticContext context)
    {
        XPathAstNode expression = new XPathParser(source, "urn:test").ParseXPathExpression();
        return new XPathStaticBinder(context).BindXPathExpression(expression);
    }

    private static XmlNamespaceBinding Namespace(string prefix, string namespaceUri)
    {
        return new XmlNamespaceBinding { Prefix = prefix, NamespaceUri = namespaceUri };
    }

    private static XmlName Name(string? namespaceUri, string localName)
    {
        return new XmlName { LocalName = localName, NamespaceUri = namespaceUri, QualifiedName = localName };
    }

    private static XPathVariableBinding Variable(string? namespaceUri, string localName, string value)
    {
        return new XPathVariableBinding
        {
            Name = Name(namespaceUri, localName),
            Value = new XdmSequence
            {
                AtomicValues = [new XdmAtomicValue { Type = XdmAtomicType.String, LexicalValue = value }],
                ItemKinds = [XdmItemKind.Atomic],
                ItemIndexes = [0],
                Count = 1,
                IsImmutable = true
            }
        };
    }

    private static XPathFunctionBinding Function(string namespaceUri, string localName, int arity, string key)
    {
        return new XPathFunctionBinding
        {
            Name = Name(namespaceUri, localName),
            Arity = arity,
            BindingKey = key
        };
    }

    private static SourceRange Range()
    {
        return new SourceRange { Start = new SourceLocation(), End = new SourceLocation() };
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema =
            "<xs:schema xmlns:xs='" + XPathNamespaces.XmlSchema + "' xmlns:t='urn:types' targetNamespace='urn:types'>" +
            "<xs:simpleType name='Code'><xs:restriction base='xs:string'/></xs:simpleType>" +
            "</xs:schema>";
        XmlResolverPolicy policy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied, AllowedSchemes = [] };
        return new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = schema, BaseUri = "https://example.test/types.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = policy },
            new XmlDefaultResolver());
    }
}
