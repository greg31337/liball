using System.Globalization;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XPathXsltParityTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void CoreSemanticsAndStaticAndDynamicContextsMatch()
    {
        const string expression = "(count(/root/item), $factor * sum(/root/item/@n), resolve-uri('asset.xml'), string(/root/@label))";
        const string baseUri = "https://example.test/styles/main.xsl";
        XmlDocument source = Parse("<root xmlns='urn:data' label='catalog'><item n='1'/><item n='3'/></root>", "https://example.test/source.xml");
        XdmSequence factor = AtomicInteger(3);
        XPathVariableBinding variable = new XPathVariableBinding
        {
            Name = Name("factor", ""),
            DeclaredType = XdmAtomicType.Integer,
            DeclaredOccurrence = XdmOccurrence.ExactlyOne,
            IsExternal = true,
            IsRequired = true
        };
        XPathStaticContext xpathStatic = new XPathStaticContext
        {
            BaseUri = baseUri,
            DefaultElementNamespace = "urn:data",
            Variables = [variable]
        };
        XPathDynamicContext xpathDynamic = new XPathDynamicContext
        {
            ContextItem = NodeSequence(source.DocumentNode!),
            Position = 1,
            Size = 1,
            Variables =
            [
                new XPathVariableBinding
                {
                    Name = Name("factor", ""),
                    Value = factor,
                    DeclaredType = XdmAtomicType.Integer,
                    DeclaredOccurrence = XdmOccurrence.ExactlyOne
                }
            ]
        };

        XdmSequence xpath = EvaluateXPath(expression, xpathStatic, xpathDynamic);
        XsltCompileOptions xsltCompile = CompileOptions(baseUri, AllowedPolicy());
        string declarations = "<xsl:param name='factor' as='xs:integer' required='yes'/>";
        XsltExecutable executable = CompileXslt(expression, "xpath-default-namespace='urn:data'", declarations, xsltCompile);
        XdmSequence xslt = Transform(executable, source, null, null,
        [
            new XsltParameter
            {
                Name = Name("factor", ""),
                Value = factor,
                DeclaredType = XdmAtomicType.Integer,
                DeclaredOccurrence = XdmOccurrence.ExactlyOne
            }
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(Signature(xslt), Is.EqualTo(Signature(xpath)));
            Assert.That(Signature(xpath), Is.EqualTo(new[]
            {
                "Integer:2",
                "Double:12",
                "AnyUri:https://example.test/styles/asset.xml",
                "String:catalog"
            }));
        });
    }

    [Test]
    public void RegisteredFunctionsAndCollationsMatch()
    {
        const string collationUri = "urn:collation:ignore-case";
        const string expression = "(ext:decorate('MiXeD'), 'Alpha' eq 'alpha', compare('b', 'A'))";
        DecoratingFunction xpathFunction = new DecoratingFunction();
        DecoratingFunction xsltFunction = new DecoratingFunction();
        IgnoreCaseCollation xpathCollation = new IgnoreCaseCollation();
        IgnoreCaseCollation xsltCollation = new IgnoreCaseCollation();
        XPathFunctionBinding xpathBinding = FunctionBinding(xpathFunction);
        XPathFunctionBinding xsltBinding = FunctionBinding(xsltFunction);
        XPathCollationBinding xpathCollationBinding = new XPathCollationBinding { Uri = collationUri, Implementation = xpathCollation };
        XPathCollationBinding xsltCollationBinding = new XPathCollationBinding { Uri = collationUri, Implementation = xsltCollation };
        XPathStaticContext xpathStatic = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "ext", NamespaceUri = "urn:extension" }],
            Functions = [xpathBinding],
            DefaultCollation = collationUri,
            CollationBindings = [xpathCollationBinding]
        };

        XdmSequence xpath = EvaluateXPath(expression, xpathStatic, new XPathDynamicContext());
        XsltCompileOptions xsltCompile = CompileOptions("https://example.test/main.xsl", AllowedPolicy());
        xsltCompile.Functions = [xsltBinding];
        xsltCompile.Collations = [xsltCollationBinding];
        XsltExecutable executable = CompileXslt(expression, "xmlns:ext='urn:extension' default-collation='" + collationUri + "'", "", xsltCompile);
        XdmSequence xslt = Transform(executable, null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(Signature(xslt), Is.EqualTo(Signature(xpath)));
            Assert.That(Signature(xpath), Is.EqualTo(new[] { "String:mixed!", "Boolean:true", "Integer:1" }));
            Assert.That(xpathFunction.Calls, Is.EqualTo(1));
            Assert.That(xsltFunction.Calls, Is.EqualTo(1));
            Assert.That(xpathCollation.CompareCalls, Is.GreaterThanOrEqualTo(2));
            Assert.That(xsltCollation.CompareCalls, Is.EqualTo(xpathCollation.CompareCalls));
        });
    }

    [Test]
    public void DocumentAndCollectionPoolsResolveAndCacheIdentically()
    {
        const string baseUri = "https://example.test/app/main.xsl";
        const string expression = "(doc('data.xml') is doc('data.xml'), string(doc('data.xml')/root/@value), count(collection('items')/*/item), count(collection('items')/*/item))";
        XmlResolverPolicy policy = AllowedPolicy();
        ParityCollectionResolver xpathResolver = new ParityCollectionResolver();
        XPathStaticContext xpathStatic = new XPathStaticContext { BaseUri = baseUri, ResolverPolicy = policy };
        XPathDynamicContext xpathDynamic = new XPathDynamicContext
        {
            CollectionResolver = xpathResolver,
            ResolverPolicy = policy
        };

        XdmSequence xpath = EvaluateXPath(expression, xpathStatic, xpathDynamic);
        ParityCollectionResolver xsltResolver = new ParityCollectionResolver();
        XsltExecutable executable = CompileXslt(expression, "", "", CompileOptions(baseUri, policy));
        XdmSequence xslt = Transform(executable, null, xsltResolver, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(Signature(xslt), Is.EqualTo(Signature(xpath)));
            Assert.That(Signature(xpath), Is.EqualTo(new[] { "Boolean:true", "String:loaded", "Integer:2", "Integer:2" }));
            Assert.That(xpathResolver.DocumentCalls, Is.EqualTo(1));
            Assert.That(xpathResolver.CollectionCalls, Is.EqualTo(1));
            Assert.That(xsltResolver.DocumentCalls, Is.EqualTo(1));
            Assert.That(xsltResolver.CollectionCalls, Is.EqualTo(1));
            Assert.That(xpathResolver.LastDocumentUri, Is.EqualTo("https://example.test/app/data.xml"));
            Assert.That(xsltResolver.LastDocumentUri, Is.EqualTo(xpathResolver.LastDocumentUri));
        });
    }

    [Test]
    public void SchemaAnnotationsAndTypedValuesMatch()
    {
        const string expression = "(/* instance of schema-element(t:amount), data(/*) + 1, /* instance of element(*, xs:int))";
        XmlSchemaSet schemas = CompileSchema();
        XmlDocument source = Parse("<amount xmlns='urn:typed'>41</amount>", "https://example.test/amount.xml");
        XmlSchemaValidationResult validation = new XmlSchemaValidator().ValidateXmlSchema(
            source,
            schemas,
            new XmlSchemaValidationOptions { AnnotateTree = true });
        Assert.That(validation.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
        XPathStaticContext xpathStatic = new XPathStaticContext
        {
            SchemaAware = true,
            SchemaSet = schemas,
            Namespaces =
            [
                new XmlNamespaceBinding { Prefix = "t", NamespaceUri = "urn:typed" },
                new XmlNamespaceBinding { Prefix = "xs", NamespaceUri = Xsd }
            ]
        };
        XPathDynamicContext xpathDynamic = new XPathDynamicContext
        {
            ContextItem = NodeSequence(source.DocumentNode!),
            Position = 1,
            Size = 1
        };

        XdmSequence xpath = EvaluateXPath(expression, xpathStatic, xpathDynamic);
        XsltCompileOptions xsltCompile = CompileOptions("https://example.test/typed.xsl", DeniedPolicy());
        xsltCompile.ConformanceLevel = XsltConformanceLevel.SchemaAware;
        xsltCompile.SchemaSet = schemas;
        string declarations = "<xsl:import-schema namespace='urn:typed'/>";
        XsltExecutable executable = CompileXslt(expression, "xmlns:t='urn:typed'", declarations, xsltCompile);
        XdmSequence xslt = Transform(executable, source, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(Signature(xslt), Is.EqualTo(Signature(xpath)));
            Assert.That(Signature(xpath), Is.EqualTo(new[] { "Boolean:true", "Integer:42", "Boolean:true" }));
            Assert.That(source.DocumentElement!.TypeInfo!.TypeName!.LocalName, Is.EqualTo("int"));
        });
    }

    [Test]
    public void DynamicErrorCodesMatchInsideAndOutsideStylesheets()
    {
        const string expression = "1 idiv 0";
        XPathCompiledExpression xpathExpression = new XPathCompiler().CompileXPath(expression, new XPathStaticContext { BaseUri = "https://example.test/error.xpath" });
        XsltExecutable executable = CompileXslt(expression, "", "", CompileOptions("https://example.test/error.xsl", DeniedPolicy()));

        XPathDynamicException xpath = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(xpathExpression, new XPathDynamicContext()))!;
        XPathDynamicException xslt = Assert.Throws<XPathDynamicException>(() => Transform(executable, null, null, null, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(xslt.Diagnostic!.Code, Is.EqualTo(xpath.Diagnostic!.Code));
            Assert.That(xslt.Diagnostic.Category, Is.EqualTo(xpath.Diagnostic.Category));
            Assert.That(xpath.Diagnostic.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(xpath.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("https://example.test/error.xpath"));
            Assert.That(xslt.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("https://example.test/error.xsl"));
        });
    }

    [Test]
    public void DeniedResourcePoliciesMatchAndDoNotInvokeResolvers()
    {
        const string expression = "doc('https://blocked.test/data.xml')";
        XmlResolverPolicy denied = DeniedPolicy();
        ParityCollectionResolver xpathResolver = new ParityCollectionResolver();
        XPathCompiledExpression xpathExpression = new XPathCompiler().CompileXPath(expression, new XPathStaticContext
        {
            BaseUri = "https://example.test/main.xpath",
            ResolverPolicy = denied
        });
        XPathDynamicContext xpathDynamic = new XPathDynamicContext
        {
            ResolverPolicy = denied,
            CollectionResolver = xpathResolver
        };
        ParityCollectionResolver xsltResolver = new ParityCollectionResolver();
        XsltExecutable executable = CompileXslt(expression, "", "", CompileOptions("https://example.test/main.xsl", denied));

        XPathDynamicException xpath = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(xpathExpression, xpathDynamic))!;
        XPathDynamicException xslt = Assert.Throws<XPathDynamicException>(() => Transform(executable, null, xsltResolver, null, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(xpath.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceDenied));
            Assert.That(xslt.Diagnostic!.Code, Is.EqualTo(xpath.Diagnostic.Code));
            Assert.That(xpathResolver.DocumentCalls, Is.Zero);
            Assert.That(xsltResolver.DocumentCalls, Is.Zero);
        });
    }

    private static XdmSequence EvaluateXPath(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XsltExecutable CompileXslt(string expression, string attributes, string declarations, XsltCompileOptions options)
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:xs='" + Xsd + "' version='3.0' " + attributes + ">" +
            declarations + "<xsl:template name='entry'><xsl:sequence select=\"" + EscapeAttribute(expression) + "\"/></xsl:template></xsl:stylesheet>";
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = options.BaseUri };
        return new XsltCompiler().CompileXslt(input, options, new XmlDefaultResolver());
    }

    private static XdmSequence Transform(XsltExecutable executable, XmlDocument? source, IXPathCollectionResolver? collections, IXmlResourceResolver? resources, XsltParameter[]? parameters)
    {
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = Name("entry", ""),
            SourceDocument = source,
            CollectionResolver = collections,
            ResourceResolver = resources,
            Parameters = parameters
        };
        return new XsltTransformer().TransformXslt(executable, options).PrincipalValue!;
    }

    private static XsltCompileOptions CompileOptions(string baseUri, XmlResolverPolicy policy)
    {
        return new XsltCompileOptions
        {
            BaseUri = baseUri,
            ResolverPolicy = policy
        };
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        return new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https"],
            MaximumDepth = 16,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 1048576,
            CacheResources = true
        };
    }

    private static XmlResolverPolicy DeniedPolicy()
    {
        return new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Denied,
            AllowedSchemes = [],
            MaximumDepth = 16,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 1048576,
            CacheResources = true
        };
    }

    private static XPathFunctionBinding FunctionBinding(IXPathFunction implementation)
    {
        return new XPathFunctionBinding
        {
            Name = Name("decorate", "urn:extension", "ext"),
            Arity = 1,
            ParameterTypes = [XdmAtomicType.String],
            ParameterOccurrences = [XdmOccurrence.ExactlyOne],
            ResultType = XdmAtomicType.String,
            ResultOccurrence = XdmOccurrence.ExactlyOne,
            BindingKey = "decorate",
            Implementation = implementation,
            IsDeterministic = true
        };
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema = "<xs:schema xmlns:xs='" + Xsd + "' targetNamespace='urn:typed' xmlns:t='urn:typed' elementFormDefault='qualified'>" +
            "<xs:element name='amount' type='xs:int'/></xs:schema>";
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions { ResolverPolicy = DeniedPolicy() };
        return new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = schema, BaseUri = "https://example.test/schema.xsd" }],
            options,
            new XmlDefaultResolver());
    }

    private static XmlDocument Parse(string source, string baseUri)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreservePrefixes = true
        };
        return new XmlParser().ParseXmlDocument(
            new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = baseUri },
            options,
            null,
            null,
            null);
    }

    private static XdmSequence NodeSequence(XmlNode node)
    {
        return new XdmSequenceOperations().CreateXdmNodeSequence(node);
    }

    private static XdmSequence AtomicInteger(long value)
    {
        XdmAtomicValue atomic = new XdmFactory().CreateXdmIntegerAtomicValue(value.ToString(CultureInfo.InvariantCulture), XdmAtomicType.Integer);
        return new XdmSequenceOperations().CreateXdmAtomicSequence(atomic);
    }

    private static XdmSequence AtomicString(string value)
    {
        XdmAtomicValue atomic = new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String);
        return new XdmSequenceOperations().CreateXdmAtomicSequence(atomic);
    }

    private static XmlName Name(string localName, string namespaceUri)
    {
        return Name(localName, namespaceUri, null);
    }

    private static XmlName Name(string localName, string namespaceUri, string? prefix)
    {
        return new XmlName
        {
            Prefix = prefix,
            LocalName = localName,
            NamespaceUri = namespaceUri,
            QualifiedName = prefix == null || prefix.Length == 0 ? localName : prefix + ":" + localName
        };
    }

    private static string EscapeAttribute(string value)
    {
        return value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal);
    }

    private static string[] Signature(XdmSequence sequence)
    {
        string[] result = new string[sequence.Count];
        int index = 0;
        while (index < sequence.Count)
        {
            Assert.That(sequence.ItemKinds![index], Is.EqualTo(XdmItemKind.Atomic));
            XdmAtomicValue value = sequence.AtomicValues![sequence.ItemIndexes![index]];
            string lexical = value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "";
            result[index] = value.Type + ":" + lexical;
            index = index + 1;
        }

        return result;
    }

    private sealed class DecoratingFunction : IXPathFunction
    {
        internal int Calls;

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            Calls = Calls + 1;
            string source = arguments[0].AtomicValues![arguments[0].ItemIndexes![0]].StringValue!;
            return AtomicString(source.ToLowerInvariant() + "!");
        }
    }

    private sealed class IgnoreCaseCollation : IXPathCollation
    {
        internal int CompareCalls;

        public int CompareXPathCollation(string left, string right)
        {
            CompareCalls = CompareCalls + 1;
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        public byte[] CreateXPathCollationKey(string value)
        {
            return Encoding.UTF8.GetBytes(value.ToUpperInvariant());
        }
    }

    private sealed class ParityCollectionResolver : IXPathCollectionResolver
    {
        internal int DocumentCalls;
        internal int CollectionCalls;
        internal string? LastDocumentUri;

        public XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy)
        {
            DocumentCalls = DocumentCalls + 1;
            LastDocumentUri = uri;
            return Parse("<root value='loaded'/>", uri);
        }

        public XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy)
        {
            CollectionCalls = CollectionCalls + 1;
            XmlDocument document = Parse("<items><item/><item/></items>", "https://example.test/app/items.xml");
            return NodeSequence(document.DocumentNode!);
        }

        public string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy)
        {
            return [];
        }
    }
}
