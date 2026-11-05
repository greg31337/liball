using System.Globalization;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XPathPublicIntegrationTests
{
    [Test]
    public void CompiledExpressionCanBeEvaluatedConcurrentlyWithIndependentContexts()
    {
        XPathVariableBinding declaration = Variable("value", null, XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        declaration.IsExternal = true;
        declaration.IsRequired = true;
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("$value + 1", new XPathStaticContext
        {
            Variables = [declaration]
        });
        long[] values = new long[32];

        Parallel.For(0, values.Length, index =>
        {
            XPathDynamicContext context = new XPathDynamicContext
            {
                Variables = [Variable("value", Sequence(Integer(index.ToString(CultureInfo.InvariantCulture))), XdmAtomicType.Integer, XdmOccurrence.ExactlyOne)]
            };
            XPathEvaluationResult result = new XPathEvaluator().EvaluateXPath(expression, context);
            values[index] = result.Value!.AtomicValues![0].IntegerValue;
        });

        Assert.Multiple(() =>
        {
            Assert.That(expression.IsThreadSafe, Is.True);
            Assert.That(values, Is.EqualTo(Enumerable.Range(1, values.Length).Select(value => (long)value).ToArray()));
        });
    }

    [Test]
    public void ExternalVariableResolverConvertsAndCachesResolvedValuesPerEvaluation()
    {
        XPathVariableBinding declaration = Variable("value", null, XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        declaration.IsExternal = true;
        declaration.IsRequired = true;
        RecordingVariableResolver resolver = new RecordingVariableResolver(Variable("value", Sequence(new XdmTypeConverter().CreateXdmUntypedAtomicValue("7")), XdmAtomicType.UntypedAtomic, XdmOccurrence.ExactlyOne));
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("($value, $value + 1)", new XPathStaticContext
        {
            Variables = [declaration]
        });

        XPathEvaluationResult result = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext
        {
            VariableResolver = resolver
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Value!.AtomicValues!.Select(value => value.IntegerValue).ToArray(), Is.EqualTo(new long[] { 7, 8 }));
            Assert.That(result.Value.AtomicValues!.Select(value => value.Type).ToArray(), Is.EqualTo(new[] { XdmAtomicType.Integer, XdmAtomicType.Integer }));
            Assert.That(resolver.Calls, Is.EqualTo(1));
            Assert.That(result.DynamicContext!.VariableResolver, Is.SameAs(resolver));
        });
    }

    [Test]
    public void ExternalVariableResolverHonorsRequiredAndOptionalDeclarations()
    {
        XPathVariableBinding required = Variable("required", null, XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        required.IsExternal = true;
        required.IsRequired = true;
        XPathVariableBinding optional = Variable("optional", null, XdmAtomicType.Integer, XdmOccurrence.ZeroOrMore);
        optional.IsExternal = true;
        optional.IsRequired = false;
        XPathCompiler compiler = new XPathCompiler();
        XPathCompiledExpression requiredExpression = compiler.CompileXPath("$required", new XPathStaticContext { Variables = [required] });
        XPathCompiledExpression optionalExpression = compiler.CompileXPath("$optional", new XPathStaticContext { Variables = [optional] });
        XPathDynamicContext context = new XPathDynamicContext { VariableResolver = new RecordingVariableResolver(null) };

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(requiredExpression, context))!;
        XPathEvaluationResult empty = new XPathEvaluator().EvaluateXPath(optionalExpression, context);

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
            Assert.That(empty.Value!.Count, Is.EqualTo(0));
            Assert.That(empty.Value.IsImmutable, Is.True);
        });
    }

    [Test]
    public void FunctionResolverBindsOnceAtCompilationAndReusesTheImplementation()
    {
        RecordingFunction implementation = new RecordingFunction();
        RecordingFunctionResolver resolver = new RecordingFunctionResolver(implementation);
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "ext", NamespaceUri = "urn:extensions" }],
            Functions = [Function("twice", 1)],
            FunctionResolver = resolver
        };

        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("ext:twice(5)", context);
        resolver.Enabled = false;
        XPathEvaluationResult first = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext());
        XPathEvaluationResult second = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(resolver.Calls, Is.EqualTo(1));
            Assert.That(implementation.Calls, Is.EqualTo(2));
            Assert.That(first.Value!.AtomicValues![0].IntegerValue, Is.EqualTo(10));
            Assert.That(second.Value!.AtomicValues![0].IntegerValue, Is.EqualTo(10));
        });
    }

    [Test]
    public void RegisteredCollationControlsStringComparisons()
    {
        const string uri = "urn:collation:ignore-case";
        CaseInsensitiveCollation collation = new CaseInsensitiveCollation();
        XPathCollationBinding binding = new XPathCollationBinding { Uri = uri, Implementation = collation };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("'Alpha' eq 'alpha'", new XPathStaticContext
        {
            DefaultCollation = uri,
            CollationBindings = [binding]
        });

        binding.Uri = "urn:changed";
        binding.Implementation = new XPathCodepointCollation();
        XPathEvaluationResult result = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Value!.AtomicValues![0].BooleanValue, Is.True);
            Assert.That(collation.CompareCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void TraceReturnsItsInputAndPublishesImmutableEvaluationEvents()
    {
        XPathTraceCollector collector = new XPathTraceCollector();
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("fn:trace((1, 2), 'numbers')", new XPathStaticContext
        {
            BaseUri = "urn:trace-source"
        });

        XPathEvaluationResult result = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext
        {
            TraceReceiver = collector
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Value!.AtomicValues!.Select(value => value.IntegerValue).ToArray(), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(result.TraceEvents, Has.Length.EqualTo(1));
            Assert.That(result.TraceEvents![0].Label, Is.EqualTo("numbers"));
            Assert.That(result.TraceEvents[0].Value!.AtomicValues!.Select(value => value.IntegerValue).ToArray(), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(result.TraceEvents[0].Value!.IsImmutable, Is.True);
            Assert.That(result.TraceEvents[0].Range!.Start!.SourceUri, Is.EqualTo("urn:trace-source"));
            Assert.That(result.DynamicContext!.TraceReceiver, Is.SameAs(collector));
        });
    }

    [Test]
    public void BaselineResolversReturnStableEmptyOrCopiedResults()
    {
        XPathVariableBinding binding = Variable("item", Sequence(Integer("3")), XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        XPathDynamicContext context = new XPathDynamicContext { Variables = [binding] };
        XPathVariableBinding resolved = new XPathVariableResolver().ResolveXPathVariable(Name("item", ""), context)!;
        XPathCollectionResolver collections = new XPathCollectionResolver();
        XmlResolverPolicy policy = new XmlResolverPolicy();

        binding.Value!.AtomicValues = [];

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.Not.SameAs(binding));
            Assert.That(resolved.Value!.AtomicValues![0].IntegerValue, Is.EqualTo(3));
            Assert.That(resolved.Value.IsImmutable, Is.True);
            Assert.That(collections.ResolveXPathDocument("urn:missing", policy), Is.Null);
            Assert.That(collections.ResolveXPathCollection(null, policy).Count, Is.EqualTo(0));
            Assert.That(collections.ResolveXPathUriCollection(null, policy), Is.Empty);
            Assert.That(new XPathFunctionResolver().ResolveXPathFunction(Name("missing", "urn:extensions"), 0, new XPathStaticContext()), Is.Null);
        });
    }

    [Test]
    public void JsonUriQNameFormattingAndSerializationComposeThroughThePublicApi()
    {
        string source = "let $json := parse-json('{\"relative\":\"../catalog/item.json\",\"amount\":1234.5,\"name\":\"p:item\"}'), " +
            "$name := QName('urn:items', $json?name), " +
            "$tree := json-to-xml('{\"ok\":true,\"items\":[1,2]}') " +
            "return (resolve-uri($json?relative), namespace-uri-from-QName($name), local-name-from-QName($name), " +
            "format-number($json?amount, '#,##0.00'), xml-to-json($tree), " +
            "serialize($tree/*, map{'omit-xml-declaration':true()}))";
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, new XPathStaticContext
        {
            BaseUri = "https://example.test/app/query.xq",
            AllowHigherOrderFunctions = true
        });

        XdmSequence result = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()).Value!;
        string[] values = result.AtomicValues!.Select(value => value.StringValue!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Is.EqualTo("https://example.test/catalog/item.json"));
            Assert.That(values[1], Is.EqualTo("urn:items"));
            Assert.That(values[2], Is.EqualTo("item"));
            Assert.That(values[3], Is.EqualTo("1,234.50"));
            Assert.That(values[4], Is.EqualTo("{\"ok\":true,\"items\":[1,2]}"));
            Assert.That(values[5], Does.StartWith("<map xmlns=\"http://www.w3.org/2005/xpath-functions\">"));
            Assert.That(values[5], Does.Contain("<boolean key=\"ok\">true</boolean>"));
        });
    }

    [Test]
    public void DateTimeAndNumberFormattingShareTheDeterministicDynamicContext()
    {
        XPathDynamicContext context = new XPathDynamicContext
        {
            CurrentDateTime = new DateTimeOffset(2024, 2, 29, 23, 5, 7, TimeSpan.FromMinutes(330)),
            ImplicitTimezoneMinutes = 330,
            DefaultLanguage = "en"
        };
        string source = "(format-dateTime(current-dateTime(), '[Y0001]-[M01]-[D01]T[H01]:[m01]:[s01][Z00:00]'), " +
            "format-date(current-date(), '[D01] [MNn] [Y0001]', default-language(), (), ()), " +
            "format-time(current-time(), '[h]:[m01] [PN]', default-language(), (), ()), " +
            "format-integer(21, 'w;o'), format-number(12345.6, '#,##0.00'))";
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, new XPathStaticContext());

        XdmSequence result = new XPathEvaluator().EvaluateXPath(expression, context).Value!;

        Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[]
        {
            "2024-02-29T23:05:07+05:30",
            "29 February 2024",
            "11:05 PM",
            "twenty-first",
            "12,345.60"
        }));
    }

    [Test]
    public void JsonAndXmlResourcesRespectPolicyBaseUriCachingAndEnvironmentAllowlist()
    {
        IntegrationResourceResolver resolver = new IntegrationResourceResolver();
        XPathDynamicContext context = new XPathDynamicContext
        {
            ResourceResolver = resolver,
            CollectionResolver = resolver,
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                CacheResources = true
            },
            EnvironmentNames = ["DEPLOYMENT"],
            EnvironmentValues = ["production"]
        };
        string source = "let $settings := json-doc('settings.json') return " +
            "(environment-variable($settings?environment), count(environment-variable('PATH')), " +
            "string(doc($settings?document)/*/@value), doc-available($settings?document), " +
            "resolve-uri($settings?document), serialize(doc($settings?document)/*, map{'omit-xml-declaration':true()}))";
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, new XPathStaticContext
        {
            BaseUri = "https://example.test/app/query.xq",
            AllowHigherOrderFunctions = true
        });

        XdmSequence result = new XPathEvaluator().EvaluateXPath(expression, context).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[]
            {
                "production",
                "0",
                "loaded",
                "true",
                "https://example.test/app/document.xml",
                "<config value=\"loaded\"/>"
            }));
            Assert.That(resolver.TextCalls, Is.EqualTo(1));
            Assert.That(resolver.DocumentCalls, Is.EqualTo(1));
            Assert.That(resolver.LastTextUri, Is.EqualTo("https://example.test/app/settings.json"));
            Assert.That(resolver.LastDocumentUri, Is.EqualTo("https://example.test/app/document.xml"));
            Assert.That(resolver.LastPolicy!.AllowedSchemes, Is.EqualTo(new[] { "https" }));
        });
    }

    [Test]
    public void SeededRandomResultsRemainReproducibleWhenTraced()
    {
        string source = "trace(random-number-generator('integration-seed')?permute((1,2,3,4,5)), 'permutation')";
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, new XPathStaticContext
        {
            BaseUri = "urn:integration-random",
            AllowHigherOrderFunctions = true
        });
        XPathTraceCollector firstCollector = new XPathTraceCollector();
        XPathTraceCollector secondCollector = new XPathTraceCollector();

        XPathEvaluationResult first = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext { TraceReceiver = firstCollector });
        XPathEvaluationResult second = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext { TraceReceiver = secondCollector });
        long[] firstValues = first.Value!.AtomicValues!.Select(value => value.IntegerValue).ToArray();
        long[] secondValues = second.Value!.AtomicValues!.Select(value => value.IntegerValue).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(firstValues, Is.EqualTo(secondValues));
            Assert.That(firstValues.OrderBy(value => value), Is.EqualTo(new long[] { 1, 2, 3, 4, 5 }));
            Assert.That(first.TraceEvents, Has.Length.EqualTo(1));
            Assert.That(second.TraceEvents, Has.Length.EqualTo(1));
            Assert.That(first.TraceEvents![0].Label, Is.EqualTo("permutation"));
            Assert.That(first.TraceEvents[0].Value!.AtomicValues!.Select(value => value.IntegerValue), Is.EqualTo(firstValues));
            Assert.That(first.TraceEvents[0].Range!.Start!.SourceUri, Is.EqualTo("urn:integration-random"));
        });
    }

    [Test]
    public void TransformAndErrorDiagnosticsCrossThePublicEvaluationBoundary()
    {
        XPathStaticContext context = new XPathStaticContext { BaseUri = "urn:integration-errors", AllowHigherOrderFunctions = true };
        XPathCompiledExpression transformExpression = new XPathCompiler().CompileXPath("transform(map{'stylesheet-location':'style.xsl'})", context);
        XPathCompiledExpression errorExpression = new XPathCompiler().CompileXPath("error(QName('urn:app-errors','app:failure'), 'pipeline failed', map{'stage':'test'})", context);

        XPathDynamicException transform = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(transformExpression, new XPathDynamicContext()))!;
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(errorExpression, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(transform.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOXT0003));
            Assert.That(transform.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:integration-errors"));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo("failure"));
            Assert.That(error.Diagnostic.Message, Is.EqualTo("pipeline failed"));
            Assert.That(error.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:integration-errors"));
        });
    }

    private static XPathVariableBinding Variable(string localName, XdmSequence? value, XdmAtomicType type, XdmOccurrence occurrence)
    {
        return new XPathVariableBinding
        {
            Name = Name(localName, ""),
            Value = value,
            DeclaredType = type,
            DeclaredOccurrence = occurrence
        };
    }

    private static XPathFunctionBinding Function(string localName, int arity)
    {
        return new XPathFunctionBinding
        {
            Name = Name(localName, "urn:extensions"),
            Arity = arity,
            ParameterTypes = [XdmAtomicType.Integer],
            ParameterOccurrences = [XdmOccurrence.ExactlyOne],
            ResultType = XdmAtomicType.Integer,
            ResultOccurrence = XdmOccurrence.ExactlyOne,
            BindingKey = "twice",
            IsDeterministic = true
        };
    }

    private static XmlName Name(string localName, string namespaceUri)
    {
        return new XmlName { LocalName = localName, NamespaceUri = namespaceUri, QualifiedName = localName };
    }

    private static XdmAtomicValue Integer(string lexical)
    {
        return new XdmFactory().CreateXdmIntegerAtomicValue(lexical, XdmAtomicType.Integer);
    }

    private static XdmAtomicValue String(string value, XdmAtomicType type)
    {
        return new XdmFactory().CreateXdmStringAtomicValue(value, type);
    }

    private static XdmSequence Sequence(XdmAtomicValue value)
    {
        return new XdmSequenceOperations().CreateXdmAtomicSequence(value);
    }

    private sealed class RecordingVariableResolver : IXPathVariableResolver
    {
        internal XPathVariableBinding? Binding;
        internal int Calls;

        internal RecordingVariableResolver(XPathVariableBinding? binding)
        {
            Binding = binding;
        }

        public XPathVariableBinding? ResolveXPathVariable(XmlName name, XPathDynamicContext context)
        {
            Calls = Calls + 1;
            return Binding;
        }
    }

    private sealed class RecordingFunctionResolver : IXPathFunctionResolver
    {
        internal IXPathFunction Implementation;
        internal int Calls;
        internal bool Enabled;

        internal RecordingFunctionResolver(IXPathFunction implementation)
        {
            Implementation = implementation;
            Enabled = true;
        }

        public IXPathFunction? ResolveXPathFunction(XmlName name, int arity, XPathStaticContext context)
        {
            Calls = Calls + 1;
            return Enabled ? Implementation : null;
        }
    }

    private sealed class RecordingFunction : IXPathFunction
    {
        internal int Calls;

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            Calls = Calls + 1;
            long input = arguments[0].AtomicValues![0].IntegerValue;
            return Sequence(Integer((input * 2).ToString(CultureInfo.InvariantCulture)));
        }
    }

    private sealed class CaseInsensitiveCollation : IXPathCollation
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

    private sealed class IntegrationResourceResolver : IXmlResourceResolver, IXPathCollectionResolver
    {
        internal int TextCalls;
        internal int DocumentCalls;
        internal string? LastTextUri;
        internal string? LastDocumentUri;
        internal XmlResolverPolicy? LastPolicy;

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            TextCalls = TextCalls + 1;
            LastTextUri = systemIdentifier;
            LastPolicy = policy;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                Input = new XmlInput
                {
                    Kind = XmlInputKind.ByteMemory,
                    Bytes = Encoding.UTF8.GetBytes("{\"environment\":\"DEPLOYMENT\",\"document\":\"document.xml\"}")
                },
                RequestedUri = systemIdentifier,
                AbsoluteUri = systemIdentifier
            };
        }

        public XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy)
        {
            DocumentCalls = DocumentCalls + 1;
            LastDocumentUri = uri;
            LastPolicy = policy;
            return new XmlParser().ParseXmlDocument(new XmlInput { Text = "<config value='loaded'/>" }, new XmlParserOptions(), null, null, null);
        }

        public XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy)
        {
            return new XdmSequenceOperations().CreateEmptyXdmSequence();
        }

        public string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy)
        {
            return [];
        }
    }
}
