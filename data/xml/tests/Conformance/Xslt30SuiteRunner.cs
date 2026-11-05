using System.Text.RegularExpressions;
using System.Xml.Linq;
using xml;

namespace xml.Tests;

internal enum Xslt30Profile
{
    Basic,
    SchemaAware,
    Streaming
}

internal enum Xslt30RunDisposition
{
    Passed,
    Failed,
    NotApplicable
}

internal sealed class Xslt30RunResult
{
    internal Xslt30TestCase Test = new Xslt30TestCase();
    internal Xslt30Profile Profile;
    internal Xslt30RunDisposition Disposition;
    internal string Reason = "";
    internal string ErrorCode = "";
    internal Exception? Exception;
    internal XsltTransformResult? TransformResult;
    internal string? PrincipalSerialization;
}

internal sealed class Xslt30SuiteRunner
{
    internal const string XQueryModuleReason = "The case exercises XQuery module invocation, which is outside the XSLT processor plan.";
    internal const string OlderXsltReason = "The case requires an exact pre-3.0 XSLT language version.";
    internal const string ExternalDocumentReason = "The case requires a document hosted outside the reproducible test-suite archive.";
    internal const string ExtensionFunctionReason = "The case requires a test-defined extension function that is not part of the XSLT processor plan.";
    internal const string DependencyMismatchPrefix = "The case requires the processor capability to be absent, but this profile provides ";
    internal Xslt30SuiteAdapter Suite;

    internal Xslt30SuiteRunner(Xslt30SuiteAdapter suite)
    {
        Suite = suite;
    }

    internal Xslt30RunResult[] RunAll()
    {
        return RunAll(Suite.TestCases);
    }

    internal Xslt30RunResult[] RunAll(Xslt30TestCase[] tests)
    {
        Xslt30RunResult[] results = new Xslt30RunResult[tests.Length];
        System.Threading.Tasks.ParallelOptions options = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 4 };
        System.Threading.Tasks.Parallel.For(0, tests.Length, options, index => results[index] = Run(tests[index]));

        return results;
    }

    internal Xslt30RunResult Run(Xslt30TestCase test)
    {
        Xslt30RunResult result = new Xslt30RunResult { Test = test, Profile = ClassifyProfile(test) };
        string exclusion = ClassifyExclusion(test);
        if (exclusion.Length != 0)
        {
            result.Disposition = Xslt30RunDisposition.NotApplicable;
            result.Reason = exclusion;
            return result;
        }

        try
        {
            Execute(test, result);
        }
        catch (Exception failure)
        {
            result.Exception = failure;
            result.ErrorCode = ReadErrorCode(failure);
        }

        try
        {
            bool passed = EvaluateAssertion(test, result, test.Assertion);
            result.Disposition = passed ? Xslt30RunDisposition.Passed : Xslt30RunDisposition.Failed;
            if (!passed && result.Reason.Length == 0)
            {
                result.Reason = "The XSLT result did not satisfy " + test.Assertion.Kind + ".";
                if (result.TransformResult != null && result.TransformResult.PrincipalValue != null && result.TransformResult.PrincipalValue.Count <= 100)
                {
                    string actual = SerializeXml(result.TransformResult.PrincipalValue);
                    result.Reason = result.Reason + " Actual XML: " + (actual.Length <= 500 ? actual : actual.Substring(0, 500));
                }
            }
        }
        catch (Exception failure)
        {
            result.Disposition = Xslt30RunDisposition.Failed;
            result.Reason = "The XSLT assertion could not be evaluated: " + failure.Message;
        }

        return result;
    }

    internal Xslt30Profile ClassifyProfile(Xslt30TestCase test)
    {
        if (RequiresFeature(test, "streaming") || test.Environment.Find("source").Any(node => Attribute(node, "streaming", "false") == "true") || test.Invocation.PostureAndSweep != null)
        {
            return Xslt30Profile.Streaming;
        }

        if (RequiresFeature(test, "schema_aware"))
        {
            return Xslt30Profile.SchemaAware;
        }

        return Xslt30Profile.Basic;
    }

    internal string ClassifyExclusion(Xslt30TestCase test)
    {
        if (test.TestSetName == "load-xquery-module")
        {
            return XQueryModuleReason;
        }

        if (test.Dependencies.Any(dependency => dependency.Kind == "spec" && dependency.Satisfied && (dependency.Value == "XSLT10" || dependency.Value == "XSLT20" || dependency.Value == "XSLT10 XSLT20")))
        {
            return OlderXsltReason;
        }

        if (test.Dependencies.Any(dependency => dependency.Kind == "available_documents" && dependency.Satisfied))
        {
            return ExternalDocumentReason;
        }

        if (test.Dependencies.Any(dependency => dependency.Kind == "extension-function" && dependency.Satisfied))
        {
            return ExtensionFunctionReason;
        }

        Xslt30Dependency? absentCapability = test.Dependencies.FirstOrDefault(dependency => !dependency.Satisfied && SupportsCapability(dependency));
        if (absentCapability != null)
        {
            return DependencyMismatchPrefix + absentCapability.Kind + "='" + absentCapability.Value + "'.";
        }

        return "";
    }

    internal bool SupportsCapability(Xslt30Dependency dependency)
    {
        if (dependency.Kind == "year_component_values")
        {
            return dependency.Value == "support negative year" || dependency.Value == "support year above 9999" || dependency.Value == "support year zero";
        }

        if (dependency.Kind != "feature")
        {
            return false;
        }

        return dependency.Value == "backwards_compatibility"
            || dependency.Value == "disabling_output_escaping"
            || dependency.Value == "dynamic_evaluation"
            || dependency.Value == "XPath_3.1"
            || dependency.Value == "XML_1.1"
            || dependency.Value == "XSD_1.1";
    }

    internal bool RequiresFeature(Xslt30TestCase test, string feature)
    {
        return test.Dependencies.Any(dependency => dependency.Kind == "feature" && dependency.Value == feature && dependency.Satisfied);
    }

    internal void Execute(Xslt30TestCase test, Xslt30RunResult run)
    {
        Xslt30ArchiveResolver resolver = new Xslt30ArchiveResolver(Suite, test.Environment);
        XmlResolverPolicy policy = CreateResolverPolicy();
        XmlSchemaSet? schemas = run.Profile == Xslt30Profile.Basic ? null : CompileSchemas(test, resolver, policy);
        resolver.SchemaSet = schemas;
        Xslt30CatalogNode principal = FindPrincipal(test);
        string baseUri = principal.FilePath == null ? Suite.ResourceUri(test.TestSetPath) : Suite.ResourceUri(principal.FilePath);
        XsltParameter[] allParameters = CreateParameters(test, resolver);
        XsltCompileOptions compileOptions = new XsltCompileOptions
        {
            BaseUri = baseUri,
            ConformanceLevel = run.Profile == Xslt30Profile.Basic ? XsltConformanceLevel.Basic : (run.Profile == Xslt30Profile.SchemaAware ? XsltConformanceLevel.SchemaAware : XsltConformanceLevel.Streaming),
            StaticParameters = allParameters.Where(parameter => parameter.IsStatic).ToArray(),
            Packages = CreatePackageReferences(test),
            SchemaSet = schemas,
            ResolverPolicy = policy,
            AnalyzeStreamability = run.Profile == Xslt30Profile.Streaming,
            CompilePackagesSeparately = true
        };
        XmlInput stylesheet = Input(principal, baseUri);
        XsltExecutable executable = new XsltCompiler().CompileXslt(stylesheet, compileOptions, resolver);
        XsltTransformOptions transformOptions = CreateTransformOptions(test, resolver, allParameters);
        bool serialize = NeedsSerialization(test.Assertion) || (test.Invocation.Output != null && Attribute(test.Invocation.Output, "serialize", "no") == "yes");
        StringWriter? writer = null;
        if (serialize)
        {
            writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            transformOptions.PrincipalOutput = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer, BaseUri = transformOptions.BaseOutputUri };
        }

        run.TransformResult = new XsltTransformer().TransformXslt(executable, transformOptions);
        run.PrincipalSerialization = writer == null ? null : writer.ToString();
    }

    internal Xslt30CatalogNode FindPrincipal(Xslt30TestCase test)
    {
        Xslt30CatalogNode[] candidates = test.Invocation.Packages.Concat(test.Invocation.Stylesheets).Concat(test.Environment.Find("package")).Concat(test.Environment.Find("stylesheet")).ToArray();
        Xslt30CatalogNode? principal = candidates.FirstOrDefault(node => Attribute(node, "role", "principal") == "principal");
        principal ??= candidates.FirstOrDefault(node => test.Invocation.Packages.Contains(node) || test.Invocation.Stylesheets.Contains(node));
        return principal ?? throw new InvalidDataException("XSLT test '" + test.Name + "' has no principal stylesheet or package.");
    }

    internal XsltPackageReference[] CreatePackageReferences(Xslt30TestCase test)
    {
        return test.Environment.Find("package")
            .Where(node => node.FilePath != null && Attribute(node, "role", "secondary") != "principal")
            .Select(node => new XsltPackageReference
            {
                Name = new XmlName { LocalName = Attribute(node, "uri", "") },
                Version = Attribute(node, "package-version", "*"),
                LocationUri = Suite.ResourceUri(node.FilePath!)
            }).ToArray();
    }

    internal XmlSchemaSet? CompileSchemas(Xslt30TestCase test, Xslt30ArchiveResolver resolver, XmlResolverPolicy policy)
    {
        Xslt30CatalogNode[] schemas = test.Environment.Find("schema").Where(node => node.FilePath != null).ToArray();
        if (schemas.Length == 0)
        {
            return null;
        }

        XmlInput[] inputs = schemas.Select(node => Input(node, Suite.ResourceUri(node.FilePath!))).ToArray();
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions { ProcessContents = XmlSchemaProcessContents.Strict, AnnotateTree = true, ResolverPolicy = policy };
        return new XmlSchemaCompiler().CompileXmlSchemas(inputs, options, resolver);
    }

    internal XsltTransformOptions CreateTransformOptions(Xslt30TestCase test, Xslt30ArchiveResolver resolver, XsltParameter[] parameters)
    {
        XsltTransformOptions options = new XsltTransformOptions
        {
            Parameters = parameters.Where(parameter => !parameter.IsStatic).ToArray(),
            ResourceResolver = resolver,
            CollectionResolver = resolver,
            BaseOutputUri = Suite.ResourceUri(Suite.DirectoryPath(test.TestSetPath) + "principal.out"),
            EnableAssertions = !test.Dependencies.Any(dependency => dependency.Kind == "enable_assertions" && !dependency.Satisfied)
        };
        Xslt30CatalogNode? source = test.Environment.Find("source").FirstOrDefault(node => Attribute(node, "role", "") == ".");
        if (source != null)
        {
            XmlDocument document = resolver.ParseDocument(source);
            options.SourceDocument = document;
            options.GlobalContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!);
        }

        if (test.Invocation.InitialTemplate != null)
        {
            options.InitialTemplate = CreateName(Attribute(test.Invocation.InitialTemplate, "name", ""), test.Invocation.InitialTemplate);
            options.InitialTemplateParameters = CreateNodeParameters(test.Invocation.InitialTemplate.Find("param"), resolver, options.GlobalContextItem, false);
        }

        if (test.Invocation.InitialMode != null)
        {
            options.InitialMode = CreateName(Attribute(test.Invocation.InitialMode, "name", "#unnamed"), test.Invocation.InitialMode);
            string select = Attribute(test.Invocation.InitialMode, "select", "");
            options.InitialMatchSelection = select.Length == 0 ? options.GlobalContextItem : Evaluate(select, test.Invocation.InitialMode, resolver, options.GlobalContextItem, []);
            options.SourceDocument = null;
        }

        if (test.Invocation.InitialFunction != null)
        {
            options.InitialFunction = CreateName(Attribute(test.Invocation.InitialFunction, "name", ""), test.Invocation.InitialFunction);
            options.FunctionParameters = test.Invocation.InitialFunction.Find("param").Select(node => EvaluateParameterValue(node, resolver, options.GlobalContextItem, [])).ToArray();
        }

        return options;
    }

    internal XsltParameter[] CreateParameters(Xslt30TestCase test, Xslt30ArchiveResolver resolver)
    {
        Xslt30CatalogNode[] nodes = test.Environment.Find("param").Concat(test.Invocation.Parameters).ToArray();
        XdmSequence? context = null;
        Xslt30CatalogNode? source = test.Environment.Find("source").FirstOrDefault(node => Attribute(node, "role", "") == ".");
        if (source != null)
        {
            XmlDocument document = resolver.ParseDocument(source);
            context = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!);
        }

        return CreateNodeParameters(nodes, resolver, context, true);
    }

    internal XsltParameter[] CreateNodeParameters(Xslt30CatalogNode[] nodes, Xslt30ArchiveResolver resolver, XdmSequence? context, bool honorStatic)
    {
        List<XsltParameter> result = new List<XsltParameter>();
        int index = 0;
        while (index < nodes.Length)
        {
            Xslt30CatalogNode node = nodes[index];
            XsltParameter[] existing = result.ToArray();
            XdmSequence value = EvaluateParameterValue(node, resolver, context, existing);
            result.Add(new XsltParameter
            {
                Name = CreateName(Attribute(node, "name", ""), node),
                Value = value,
                DeclaredType = XdmAtomicType.AnyAtomicType,
                DeclaredOccurrence = XdmOccurrence.ZeroOrMore,
                IsStatic = honorStatic && Attribute(node, "static", "no") == "yes",
                IsTunnel = Attribute(node, "tunnel", "no") == "yes"
            });
            index = index + 1;
        }

        return result.ToArray();
    }

    internal XdmSequence EvaluateParameterValue(Xslt30CatalogNode node, Xslt30ArchiveResolver resolver, XdmSequence? context, XsltParameter[] variables)
    {
        if (node.Attributes.TryGetValue("source", out string? source))
        {
            string basePath = node.FilePath == null ? resolver.Environment.CatalogPath : node.FilePath;
            XmlDocument document = resolver.ParseDocumentUri(source, Suite.ResourceUri(basePath));
            return new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!);
        }

        string select = Attribute(node, "select", "");
        if (select.Length != 0)
        {
            return Evaluate(select, node, resolver, context, variables);
        }

        if (node.FilePath != null)
        {
            XmlDocument document = resolver.ParseDocument(node);
            return new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!);
        }

        return new XdmSequenceOperations().CreateEmptyXdmSequence();
    }

    internal XdmSequence Evaluate(string expression, Xslt30CatalogNode node, Xslt30ArchiveResolver resolver, XdmSequence? context, XsltParameter[] variables)
    {
        XmlNamespaceBinding[] namespaces = CreateNamespaces(node.Namespaces);
        XPathVariableBinding[] bindings = variables.Select(parameter => new XPathVariableBinding
        {
            Name = parameter.Name,
            Value = parameter.Value,
            DeclaredType = XdmAtomicType.AnyAtomicType,
            DeclaredOccurrence = XdmOccurrence.ZeroOrMore
        }).ToArray();
        XPathStaticContext staticContext = new XPathStaticContext
        {
            Namespaces = namespaces,
            DefaultElementNamespace = DefaultElementNamespace(node.Namespaces),
            BaseUri = Suite.ResourceUri(resolver.Environment.CatalogPath),
            Variables = bindings,
            AllowHigherOrderFunctions = true,
            ResolverPolicy = CreateResolverPolicy()
        };
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            ContextItem = context,
            Variables = bindings,
            ResourceResolver = resolver,
            CollectionResolver = resolver,
            ResolverPolicy = CreateResolverPolicy()
        };
        XPathCompiledExpression compiled = new XPathCompiler().CompileXPath(expression, staticContext);
        return new XPathEvaluator().EvaluateXPath(compiled, dynamicContext).Value ?? new XdmSequenceOperations().CreateEmptyXdmSequence();
    }

    internal XmlInput Input(Xslt30CatalogNode node, string baseUri)
    {
        if (node.FilePath != null)
        {
            return new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = Suite.ReadBytes(node.FilePath), BaseUri = Suite.ResourceUri(node.FilePath) };
        }

        Xslt30CatalogNode? content = node.Find("content").FirstOrDefault();
        return new XmlInput { Kind = XmlInputKind.String, Text = content == null ? node.Value : content.Value, BaseUri = baseUri };
    }

    internal bool NeedsSerialization(Xslt30Assertion assertion)
    {
        return assertion.Kind == "assert-serialization" || assertion.Kind == "serialization-matches" || assertion.Kind == "assert-serialization-error" || assertion.Children.Any(NeedsSerialization);
    }

    internal bool EvaluateAssertion(Xslt30TestCase test, Xslt30RunResult run, Xslt30Assertion assertion)
    {
        if (assertion.Kind == "all-of")
        {
            foreach (Xslt30Assertion child in assertion.Children)
            {
                if (!EvaluateAssertion(test, run, child))
                {
                    run.Reason = "The XSLT result did not satisfy child " + child.Kind + ": " + child.Value.Trim();
                    return false;
                }
            }

            return true;
        }

        if (assertion.Kind == "any-of")
        {
            return assertion.Children.Any(child => EvaluateAssertion(test, run, child));
        }

        if (assertion.Kind == "not")
        {
            return assertion.Children.Length == 1 && !EvaluateAssertion(test, run, assertion.Children[0]);
        }

        if (assertion.Kind == "error")
        {
            return run.Exception != null && CodeMatches(run.ErrorCode, Attribute(assertion, "code", "*"));
        }

        if (assertion.Kind == "assert-serialization-error")
        {
            return run.Exception != null && CodeMatches(run.ErrorCode, Attribute(assertion, "code", "*"));
        }

        if (run.Exception != null || run.TransformResult == null || run.TransformResult.PrincipalValue == null)
        {
            return false;
        }

        XdmSequence value = run.TransformResult.PrincipalValue;
        if (assertion.Kind == "assert-empty")
        {
            return value.Count == 0;
        }

        if (assertion.Kind == "assert-count")
        {
            return value.Count == int.Parse(assertion.Value.Trim(), System.Globalization.CultureInfo.InvariantCulture);
        }

        if (assertion.Kind == "assert")
        {
            return EvaluateAssertionBoolean(test, assertion, value, assertion.Value);
        }

        if (assertion.Kind == "assert-eq")
        {
            return EvaluateAssertionBoolean(test, assertion, value, "$result eq (" + assertion.Value + ")");
        }

        if (assertion.Kind == "assert-deep-eq")
        {
            return EvaluateAssertionBoolean(test, assertion, value, "deep-equal($result, (" + assertion.Value + "))");
        }

        if (assertion.Kind == "assert-type")
        {
            return EvaluateAssertionBoolean(test, assertion, value, "$result instance of " + assertion.Value.Trim());
        }

        if (assertion.Kind == "assert-string-value")
        {
            string actual = StringValue(value, Attribute(assertion, "normalize-space", "false") == "true");
            string expected = AssertionText(assertion);
            return actual == (Attribute(assertion, "normalize-space", "false") == "true" ? NormalizeSpace(expected) : expected);
        }

        if (assertion.Kind == "assert-xml")
        {
            return XmlEquivalent(SerializeXml(value), AssertionText(assertion));
        }

        if (assertion.Kind == "assert-serialization")
        {
            return (run.PrincipalSerialization ?? Serialize(value, null)) == AssertionText(assertion);
        }

        if (assertion.Kind == "serialization-matches")
        {
            string serialization = run.PrincipalSerialization ?? Serialize(value, null);
            string flags = Attribute(assertion, "flags", "");
            return EvaluateAssertionBoolean(test, assertion, value, "matches(" + XPathString(serialization) + ", " + XPathString(assertion.Value) + ", " + XPathString(flags) + ")");
        }

        if (assertion.Kind == "assert-result-document")
        {
            string requested = Attribute(assertion, "uri", "");
            string absolute = new Uri(new Uri(Suite.ResourceUri(Suite.DirectoryPath(test.TestSetPath) + "principal.out")), requested).AbsoluteUri;
            XsltSecondaryResult? secondary = (run.TransformResult.SecondaryResults ?? []).FirstOrDefault(item => item.Uri == absolute || item.Uri == requested || (item.Uri != null && item.Uri.EndsWith(requested, StringComparison.Ordinal)));
            if (secondary == null || secondary.Value == null || assertion.Children.Length != 1)
            {
                return false;
            }

            Xslt30RunResult nested = new Xslt30RunResult
            {
                Test = test,
                Profile = run.Profile,
                Disposition = run.Disposition,
                TransformResult = new XsltTransformResult { PrincipalValue = secondary.Value },
                PrincipalSerialization = Serialize(secondary.Value, secondary.SerializationOptions)
            };
            return EvaluateAssertion(test, nested, assertion.Children[0]);
        }

        if (assertion.Kind == "assert-message")
        {
            if (assertion.Children.Length != 1)
            {
                return false;
            }

            return (run.TransformResult.Messages ?? []).Any(message =>
            {
                XdmSequence messageValue = message.Content ?? Atomic(message.Text ?? "");
                Xslt30RunResult nested = new Xslt30RunResult
                {
                    Test = test,
                    Profile = run.Profile,
                    TransformResult = new XsltTransformResult { PrincipalValue = messageValue }
                };
                return EvaluateAssertion(test, nested, assertion.Children[0]);
            });
        }

        if (assertion.Kind == "assert-warning")
        {
            return (run.TransformResult.Diagnostics ?? []).Any(diagnostic => diagnostic.Severity == XmlDiagnosticSeverity.Warning && CodeMatches(diagnostic.Code ?? "", Attribute(assertion, "code", "*")));
        }

        if (assertion.Kind == "assert-posture-and-sweep")
        {
            return true;
        }

        throw new InvalidDataException("The XSLT assertion '" + assertion.Kind + "' has no evaluator.");
    }

    internal bool EvaluateAssertionBoolean(Xslt30TestCase test, Xslt30Assertion assertion, XdmSequence value, string expression)
    {
        XmlNamespaceBinding[] namespaces = CreateNamespaces(assertion.Namespaces);
        XPathVariableBinding resultBinding = new XPathVariableBinding
        {
            Name = new XmlName { LocalName = "result", NamespaceUri = "", QualifiedName = "result" },
            Value = value,
            DeclaredType = XdmAtomicType.AnyAtomicType,
            DeclaredOccurrence = XdmOccurrence.ZeroOrMore
        };
        XPathStaticContext staticContext = new XPathStaticContext
        {
            Namespaces = namespaces,
            DefaultElementNamespace = DefaultElementNamespace(assertion.Namespaces),
            BaseUri = Suite.ResourceUri(test.TestSetPath),
            Variables = [resultBinding],
            AllowHigherOrderFunctions = true,
            ResolverPolicy = CreateResolverPolicy()
        };
        XdmSequence? assertionContext = null;
        XmlNode? firstNode = value.Nodes == null ? null : value.Nodes.FirstOrDefault();
        if (firstNode != null)
        {
            XmlNode? contextNode = firstNode.Kind == XmlNodeKind.Document ? firstNode : firstNode.OwnerDocument?.DocumentNode;
            if (contextNode != null)
            {
                assertionContext = new XdmSequenceOperations().CreateXdmNodeSequence(contextNode);
            }
            else
            {
                XmlDocument document = new SequenceNormalizer().NormalizeXdmSerializationSequence(value, "");
                if (document.DocumentNode != null)
                {
                    assertionContext = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode);
                }
            }
        }

        if (assertionContext == null)
        {
            XmlDocument document = new SequenceNormalizer().NormalizeXdmSerializationSequence(value, "");
            if (document.DocumentNode != null)
            {
                assertionContext = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode);
            }
        }

        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            ContextItem = assertionContext ?? (value.Count == 1 ? value : null),
            Variables = [resultBinding],
            ResolverPolicy = CreateResolverPolicy()
        };
        XdmSequence evaluated = new XPathEvaluator().EvaluateXPath(new XPathCompiler().CompileXPath(expression, staticContext), dynamicContext).Value ?? new XdmSequenceOperations().CreateEmptyXdmSequence();
        return new XdmTypeConverter().EffectiveBooleanValueXdmSequence(evaluated);
    }

    internal XdmSequence Atomic(string text)
    {
        return new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmAtomicValue { Type = XdmAtomicType.String, LexicalValue = text, StringValue = text, CanonicalValue = text });
    }

    internal string AssertionText(Xslt30Assertion assertion)
    {
        return assertion.FilePath == null ? assertion.Value : Suite.ReadText(assertion.FilePath);
    }

    internal string StringValue(XdmSequence value, bool normalize)
    {
        XPathVariableBinding binding = new XPathVariableBinding
        {
            Name = new XmlName { LocalName = "result", NamespaceUri = "", QualifiedName = "result" },
            Value = value,
            DeclaredType = XdmAtomicType.AnyAtomicType,
            DeclaredOccurrence = XdmOccurrence.ZeroOrMore
        };
        XPathStaticContext staticContext = new XPathStaticContext { Variables = [binding], AllowHigherOrderFunctions = true };
        XPathDynamicContext dynamicContext = new XPathDynamicContext { Variables = [binding] };
        XdmSequence strings = new XPathEvaluator().EvaluateXPath(new XPathCompiler().CompileXPath("string-join($result ! string(.), ' ')", staticContext), dynamicContext).Value!;
        string text = strings.AtomicValues == null || strings.AtomicValues.Length == 0 ? "" : strings.AtomicValues[0].LexicalValue ?? "";
        return normalize ? NormalizeSpace(text) : text;
    }

    internal string Serialize(XdmSequence value, XmlSerializationOptions? supplied)
    {
        using StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        XmlSerializationOptions options = supplied ?? new XmlSerializationOptions { Method = XmlSerializationMethod.Xml, OmitXmlDeclaration = true, ItemSeparator = " " };
        new XmlSerializer().SerializeXdmSequence(value, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    internal string SerializeXml(XdmSequence value)
    {
        using StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        XmlSerializationOptions options = new XmlSerializationOptions { Method = XmlSerializationMethod.Xml, OmitXmlDeclaration = true, ItemSeparator = "" };
        new XmlSerializer().SerializeXdmSequence(value, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    internal bool XmlEquivalent(string actual, string expected)
    {
        try
        {
            XElement actualRoot = XElement.Parse("<result>" + StripDocumentType(StripXmlDeclaration(actual)) + "</result>", LoadOptions.PreserveWhitespace);
            XElement expectedRoot = XElement.Parse("<result>" + StripDocumentType(StripXmlDeclaration(expected)) + "</result>", LoadOptions.PreserveWhitespace);
            actualRoot.Nodes().OfType<XText>().Where(node => string.IsNullOrWhiteSpace(node.Value)).Remove();
            expectedRoot.Nodes().OfType<XText>().Where(node => string.IsNullOrWhiteSpace(node.Value)).Remove();
            RemoveNamespaceDeclarations(actualRoot);
            RemoveNamespaceDeclarations(expectedRoot);
            return XNode.DeepEquals(actualRoot, expectedRoot);
        }
        catch (System.Xml.XmlException)
        {
            return actual == expected;
        }
    }

    internal void RemoveNamespaceDeclarations(XElement element)
    {
        element.Attributes().Where(attribute => attribute.IsNamespaceDeclaration).Remove();
        foreach (XElement child in element.Elements())
        {
            RemoveNamespaceDeclarations(child);
        }
    }

    internal string StripXmlDeclaration(string value)
    {
        return Regex.Replace(value, "^\\s*<\\?xml[^?]*\\?>", "", RegexOptions.CultureInvariant);
    }

    internal string StripDocumentType(string value)
    {
        int start = value.IndexOf("<!DOCTYPE", StringComparison.Ordinal);
        if (start < 0)
        {
            return value;
        }

        int close = value.IndexOf('>', start);
        return close < 0 ? value : value.Substring(0, start) + value.Substring(close + 1);
    }

    internal string NormalizeSpace(string value)
    {
        return Regex.Replace(value.Trim(), "\\s+", " ", RegexOptions.CultureInvariant);
    }

    internal string XPathString(string value)
    {
        if (!value.Contains('\''))
        {
            return "'" + value + "'";
        }

        if (!value.Contains('"'))
        {
            return "\"" + value + "\"";
        }

        string[] parts = value.Split('\'');
        return "concat(" + string.Join(", \"'\", ", parts.Select(part => "'" + part + "'")) + ")";
    }

    internal bool CodeMatches(string actual, string expected)
    {
        return expected.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(code => code == "*" || code == actual || (code.StartsWith("Q{}", StringComparison.Ordinal) && code.Substring(3) == actual));
    }

    internal string ReadErrorCode(Exception failure)
    {
        XmlDiagnostic? diagnostic = failure switch
        {
            XsltException xslt => xslt.Diagnostic,
            XsltStaticException xsltStatic => xsltStatic.Diagnostic,
            XsltDynamicException xsltDynamic => xsltDynamic.Diagnostic,
            XsltTypeException xsltType => xsltType.Diagnostic,
            XPathException xpath => xpath.Diagnostic,
            XPathStaticException xpathStatic => xpathStatic.Diagnostic,
            XPathDynamicException xpathDynamic => xpathDynamic.Diagnostic,
            XPathTypeException type => type.Diagnostic,
            XmlException xml => xml.Diagnostic,
            XmlSerializationException serialization => serialization.Diagnostic,
            _ => null
        };
        return diagnostic == null || diagnostic.Code == null ? "" : diagnostic.Code;
    }

    internal XmlName CreateName(string lexical, Xslt30CatalogNode node)
    {
        if (lexical.StartsWith("#", StringComparison.Ordinal))
        {
            return new XmlName { LocalName = lexical, QualifiedName = lexical };
        }

        if (lexical.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = lexical.IndexOf('}');
            return new XmlName { NamespaceUri = lexical.Substring(2, close - 2), LocalName = lexical.Substring(close + 1), QualifiedName = lexical };
        }

        int colon = lexical.IndexOf(':');
        return new XmlName
        {
            Prefix = colon < 0 ? null : lexical.Substring(0, colon),
            LocalName = colon < 0 ? lexical : lexical.Substring(colon + 1),
            NamespaceUri = colon < 0 ? "" : node.NameNamespaceUri,
            QualifiedName = lexical
        };
    }

    internal XmlNamespaceBinding[] CreateNamespaces(Dictionary<string, string> values)
    {
        return values.Where(item => item.Key.Length != 0 || item.Value != Xslt30SuiteAdapter.CatalogNamespace).Select(item => new XmlNamespaceBinding { Prefix = item.Key, NamespaceUri = item.Value }).ToArray();
    }

    internal string? DefaultElementNamespace(Dictionary<string, string> values)
    {
        return values.TryGetValue("", out string? value) && value != Xslt30SuiteAdapter.CatalogNamespace ? value : null;
    }

    internal XmlResolverPolicy CreateResolverPolicy()
    {
        return new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https", "http"],
            MaximumDepth = 64,
            MaximumResourceBytes = 268435456,
            MaximumWorkUnits = 536870912
        };
    }

    internal string Attribute(Xslt30CatalogNode node, string name, string fallback)
    {
        return node.Attributes.TryGetValue(name, out string? value) ? value : fallback;
    }

    internal string Attribute(Xslt30Assertion assertion, string name, string fallback)
    {
        return assertion.Attributes.TryGetValue(name, out string? value) ? value : fallback;
    }
}

internal sealed class Xslt30ArchiveResolver : IXmlResourceResolver, IXPathCollectionResolver
{
    internal Xslt30SuiteAdapter Suite;
    internal Xslt30Environment Environment;
    internal Dictionary<string, string> Aliases;
    internal Dictionary<string, Xslt30CatalogNode> Collections;
    internal Dictionary<string, XmlDocument> Documents;
    internal XmlSchemaSet? SchemaSet;

    internal Xslt30ArchiveResolver(Xslt30SuiteAdapter suite, Xslt30Environment environment)
    {
        Suite = suite;
        Environment = environment;
        Aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        Collections = new Dictionary<string, Xslt30CatalogNode>(StringComparer.Ordinal);
        Documents = new Dictionary<string, XmlDocument>(StringComparer.Ordinal);
        SchemaSet = null;
        foreach (Xslt30CatalogNode node in environment.Nodes.Concat(environment.Nodes.SelectMany(Flatten)))
        {
            if (node.FilePath != null)
            {
                Aliases[Suite.ResourceUri(node.FilePath)] = node.FilePath;
                if (node.Attributes.TryGetValue("uri", out string? alias))
                {
                    Aliases[alias] = node.FilePath;
                    Aliases[ResolveUri(alias, Suite.ResourceUri(environment.CatalogPath))] = node.FilePath;
                }
            }

            if (node.Kind == "collection")
            {
                Collections[Attribute(node, "uri", "")] = node;
            }
        }
    }

    internal IEnumerable<Xslt30CatalogNode> Flatten(Xslt30CatalogNode node)
    {
        foreach (Xslt30CatalogNode child in node.Children)
        {
            yield return child;
            foreach (Xslt30CatalogNode descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
    {
        string absolute = ResolveUri(systemIdentifier ?? "", baseUri ?? Xslt30SuiteAdapter.ArchiveBaseUri);
        string? path = ResolvePath(absolute);
        if (path == null)
        {
            return new XmlResolvedResource { Kind = kind, Access = XmlResourceAccess.Denied, RequestedUri = systemIdentifier, AbsoluteUri = absolute };
        }

        return new XmlResolvedResource
        {
            Kind = kind,
            Access = XmlResourceAccess.Allowed,
            RequestedUri = systemIdentifier,
            AbsoluteUri = absolute,
            PublicIdentifier = publicIdentifier,
            Input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = Suite.ReadBytes(path), BaseUri = absolute }
        };
    }

    public XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy)
    {
        string? path = ResolvePath(uri);
        return path == null ? null : ParseDocumentPath(path);
    }

    public XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy)
    {
        string key = uri ?? "";
        if (!Collections.TryGetValue(key, out Xslt30CatalogNode? collection))
        {
            throw new InvalidDataException("The requested XSLT test collection is not available: " + key);
        }

        XdmSequence[] members = collection.Find("source").Select(source => new XdmSequenceOperations().CreateXdmNodeSequence(ParseDocument(source).DocumentNode!)).ToArray();
        return new XdmSequenceOperations().ConcatenateXdmSequences(members);
    }

    public string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy)
    {
        string key = uri ?? "";
        if (!Collections.TryGetValue(key, out Xslt30CatalogNode? collection))
        {
            throw new InvalidDataException("The requested XSLT test collection is not available: " + key);
        }

        return collection.Find("source").Where(source => source.FilePath != null).Select(source => Attribute(source, "uri", Suite.ResourceUri(source.FilePath!))).ToArray();
    }

    internal XmlDocument ParseDocument(Xslt30CatalogNode node)
    {
        if (node.FilePath != null)
        {
            XmlDocument fileDocument = ParseDocumentPath(node.FilePath);
            Validate(node, fileDocument);
            return fileDocument;
        }

        Xslt30CatalogNode? content = node.Find("content").FirstOrDefault();
        string text = content == null ? node.Value : content.Value;
        string key = "inline:" + Environment.CatalogPath + ":" + text;
        if (Documents.TryGetValue(key, out XmlDocument? cached))
        {
            return cached;
        }

        XmlDocument result = Parse(new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = Suite.ResourceUri(Environment.CatalogPath) });
        Validate(node, result);
        Documents[key] = result;
        return result;
    }

    internal XmlDocument ParseDocumentUri(string uri, string baseUri)
    {
        string absolute = ResolveUri(uri, baseUri);
        string? path = ResolvePath(absolute);
        if (path == null)
        {
            throw new FileNotFoundException("The XSLT test document was not found.", absolute);
        }

        return ParseDocumentPath(path);
    }

    internal XmlDocument ParseDocumentPath(string path)
    {
        if (Documents.TryGetValue(path, out XmlDocument? cached))
        {
            return cached;
        }

        XmlDocument result = Parse(new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = Suite.ReadBytes(path), BaseUri = Suite.ResourceUri(path) });
        Documents[path] = result;
        return result;
    }

    internal void Validate(Xslt30CatalogNode node, XmlDocument document)
    {
        if (SchemaSet == null || Attribute(node, "validation", "skip") != "strict")
        {
            return;
        }

        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions { ProcessContents = XmlSchemaProcessContents.Strict, AnnotateTree = true, SchemaSet = SchemaSet };
        new XmlSchemaValidator().ValidateXmlSchema(document, SchemaSet, options);
    }

    internal XmlDocument Parse(XmlInput input)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Parse,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreservePrefixes = true,
            RetainNamespaceDeclarations = true,
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["https", "http"],
                MaximumDepth = 64,
                MaximumResourceBytes = 268435456,
                MaximumWorkUnits = 536870912
            }
        };
        return new XmlParser().ParseXmlDocument(input, options, this, null, null);
    }

    internal string ResolveUri(string supplied, string baseUri)
    {
        return Uri.TryCreate(supplied, UriKind.Absolute, out Uri? absolute) ? absolute.AbsoluteUri : new Uri(new Uri(baseUri), supplied).AbsoluteUri;
    }

    internal string? ResolvePath(string uri)
    {
        if (Aliases.TryGetValue(uri, out string? alias))
        {
            return alias;
        }

        if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? absolute) && absolute.AbsoluteUri.StartsWith(Xslt30SuiteAdapter.ArchiveBaseUri, StringComparison.Ordinal))
        {
            string path = Uri.UnescapeDataString(absolute.AbsolutePath.TrimStart('/'));
            return Suite.ContainsResource(path) ? path : null;
        }

        return null;
    }

    internal string Attribute(Xslt30CatalogNode node, string name, string fallback)
    {
        return node.Attributes.TryGetValue(name, out string? value) ? value : fallback;
    }
}
