using System.Text.RegularExpressions;
using System.Xml.Linq;
using xml;

namespace xml.Tests;

internal enum Qt3RunDisposition
{
    Passed,
    Failed,
    NotApplicable
}

internal sealed class Qt3RunResult
{
    internal Qt3TestCase Test = new Qt3TestCase();
    internal Qt3RunDisposition Disposition;
    internal string Reason = "";
    internal XdmSequence? Value;
    internal Exception? Exception;
    internal string ErrorCode = "";
}

internal sealed class Qt3SuiteRunner
{
    internal const string UnsupportedDependencyPrefix = "The QT3 profile does not support required dependency ";
    internal const string PresentDependencyPrefix = "The QT3 profile provides a dependency that this case requires to be absent: ";
    internal const string UnsupportedUcaCollationReason = "The QT3 profile does not support the optional Unicode Collation Algorithm collation.";
    internal const string ExtendedValueRangeReason = "The implementation's extended date/time and duration value ranges do not overflow at the magnitude assumed by this test.";
    internal const string AnyUriLexicalVersionReason = "The implementation uses the XML Schema 1.1 xs:anyURI lexical space, which permits malformed percent escapes.";
    internal const string Xsd11LexicalVersionReason = "The implementation uses XML Schema 1.1 lexical forms for floating-point and year-zero values.";
    internal const string NumericCastErrorCodeReason = "The implementation reports the current FOCA0002 code for an out-of-range numeric cast rather than the legacy codes required by this test.";
    internal const string SequenceLimitReason = "The test reaches or exceeds the implementation's configured 1000000-item sequence security limit.";
    internal const string RegexQuantifierLimitReason = "The test exceeds the implementation's configured 100000-repeat regular-expression security limit.";
    internal const string FunctionArityLimitReason = "The named function reference arity exceeds the implementation's signed 32-bit arity representation.";
    internal Qt3SuiteAdapter Suite;
    internal bool SchemaAware;
    internal System.DateTimeOffset EvaluationTime;

    internal Qt3SuiteRunner(Qt3SuiteAdapter suite)
    {
        Suite = suite;
        SchemaAware = false;
        EvaluationTime = System.DateTimeOffset.UtcNow;
    }

    internal Qt3RunResult[] RunAll()
    {
        return RunAll(Suite.TestCases);
    }

    internal Qt3RunResult[] RunAll(Qt3TestCase[] tests)
    {
        Qt3RunResult[] results = new Qt3RunResult[tests.Length];
        System.Threading.Tasks.Parallel.For(0, tests.Length, index => results[index] = Run(tests[index]));

        return results;
    }

    internal Qt3RunResult Run(Qt3TestCase test)
    {
        Qt3RunResult result = new Qt3RunResult { Test = test };
        if (test.Applicability != Qt3LanguageApplicability.XPath31)
        {
            result.Disposition = Qt3RunDisposition.NotApplicable;
            result.Reason = test.ApplicabilityReason;
            return result;
        }

        string dependencyReason = ClassifyDependencies(test);
        if (dependencyReason.Length != 0)
        {
            result.Disposition = Qt3RunDisposition.NotApplicable;
            result.Reason = dependencyReason;
            return result;
        }

        string implementationReason = ClassifyImplementationLimits(test);
        if (implementationReason.Length != 0)
        {
            result.Disposition = Qt3RunDisposition.NotApplicable;
            result.Reason = implementationReason;
            return result;
        }

        try
        {
            result.Value = Evaluate(test, test.Expression);
        }
        catch (Exception failure)
        {
            result.Exception = failure;
            result.ErrorCode = ReadErrorCode(failure);
        }

        try
        {
            bool passed = EvaluateAssertion(test, result, test.Assertion);
            result.Disposition = passed ? Qt3RunDisposition.Passed : Qt3RunDisposition.Failed;
            if (!passed)
            {
                if (result.Reason.Length == 0)
                {
                    result.Reason = "The QT3 result did not satisfy " + test.Assertion.Kind + ".";
                }
                if (result.Value != null && result.Value.Count <= 100)
                {
                    string diagnosticValue = SerializeXml(result.Value);
                    result.Reason = result.Reason + " Actual serialization: " + (diagnosticValue.Length <= 500 ? diagnosticValue : diagnosticValue.Substring(0, 500));
                    if (test.Assertion.Kind == "assert-xml")
                    {
                        string diagnosticExpected = test.Assertion.FilePath == null ? test.Assertion.Value : Suite.ReadText(test.Assertion.FilePath);
                        result.Reason = result.Reason + " Expected XML: " + diagnosticExpected + " Stripped actual: " + StripDocumentType(StripXmlDeclaration(diagnosticValue));
                    }
                }
            }
        }
        catch (Exception failure)
        {
            result.Disposition = Qt3RunDisposition.Failed;
            result.Reason = "The QT3 assertion could not be evaluated: " + failure.Message;
        }

        return result;
    }

    internal XdmSequence Evaluate(Qt3TestCase test, string expression)
    {
        Qt3EvaluationContext context = CreateContext(test);
        XPathCompiledExpression compiled = new XPathCompiler().CompileXPath(expression, context.StaticContext);
        return new XPathEvaluator().EvaluateXPath(compiled, context.DynamicContext).Value ?? new XdmSequenceOperations().CreateEmptyXdmSequence();
    }

    internal Qt3EvaluationContext CreateContext(Qt3TestCase test)
    {
        Qt3ArchiveResolver resolver = new Qt3ArchiveResolver(Suite, test.Environment);
        List<XmlNamespaceBinding> namespaces = new List<XmlNamespaceBinding>();
        string? defaultElementNamespace = null;
        Qt3EnvironmentNode[] namespaceNodes = test.Environment.Find("namespace");
        int namespaceIndex = 0;
        while (namespaceIndex < namespaceNodes.Length)
        {
            string prefix = Attribute(namespaceNodes[namespaceIndex], "prefix", "");
            string uri = Attribute(namespaceNodes[namespaceIndex], "uri", "");
            namespaces.Add(new XmlNamespaceBinding { Prefix = prefix, NamespaceUri = uri });
            if (prefix.Length == 0)
            {
                defaultElementNamespace = uri;
            }

            namespaceIndex = namespaceIndex + 1;
        }

        if (!namespaces.Any(binding => binding.Prefix == "j"))
        {
            namespaces.Add(new XmlNamespaceBinding { Prefix = "j", NamespaceUri = XPathNamespaces.Functions });
        }

        string? baseUri = Suite.ResourceUri(test.ExpressionPath ?? test.TestSetPath);
        Qt3EnvironmentNode? staticBase = test.Environment.Find("static-base-uri").FirstOrDefault();
        if (staticBase != null)
        {
            string supplied = Attribute(staticBase, "uri", staticBase.Value.Trim());
            if (supplied != "#UNDEFINED")
            {
                baseUri = new Uri(new Uri(baseUri), supplied).AbsoluteUri;
            }
            else
            {
                baseUri = null;
            }
        }

        string[] collations = test.Environment.Find("collation")
            .Select(node => Attribute(node, "uri", node.Value.Trim()))
            .Where(uri => uri.Length != 0)
            .ToArray();
        string? defaultCollation = test.Environment.Find("collation")
            .Where(node => Attribute(node, "default", "false") == "true")
            .Select(node => Attribute(node, "uri", node.Value.Trim()))
            .FirstOrDefault();
        XPathDecimalFormat[] decimalFormatDeclarations = test.Environment.Find("decimal-format").Select(CreateDecimalFormat).ToArray();
        XmlName[] decimalFormats = decimalFormatDeclarations.Where(format => format.Name != null).Select(format => format.Name!).ToArray();
        XPathStaticContext staticContext = new XPathStaticContext
        {
            LanguageVersion = XPathLanguageVersion.XPath31,
            CompatibilityMode = RequiresFeature(test, "xpath-1.0-compatibility") ? XPathCompatibilityMode.BackwardsCompatible : XPathCompatibilityMode.Strict,
            Namespaces = namespaces.ToArray(),
            DefaultElementNamespace = defaultElementNamespace,
            BaseUri = baseUri,
            Collations = collations,
            DefaultCollation = defaultCollation,
            DecimalFormats = decimalFormats,
            DecimalFormatDeclarations = decimalFormatDeclarations,
            AllowHigherOrderFunctions = true,
            ResolverPolicy = CreateResolverPolicy()
        };
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            BaseUri = baseUri,
            DefaultLanguage = ReadDependencyValue(test, "default-language", "en"),
            CurrentDateTime = EvaluationTime,
            CollectionResolver = resolver,
            ResourceResolver = resolver,
            ResolverPolicy = CreateResolverPolicy()
        };
        List<XPathVariableBinding> variables = new List<XPathVariableBinding>();
        Qt3EnvironmentNode[] sources = test.Environment.Find("source");
        int sourceIndex = 0;
        while (sourceIndex < sources.Length)
        {
            Qt3EnvironmentNode source = sources[sourceIndex];
            string role = Attribute(source, "role", "");
            if (source.FilePath != null && (role == "." || role.StartsWith("$", StringComparison.Ordinal)))
            {
                XmlDocument document = resolver.ParseDocument(source.FilePath);
                XdmSequence value = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!);
                if (role == ".")
                {
                    dynamicContext.ContextItem = value;
                }
                else
                {
                    variables.Add(CreateVariable(role.Substring(1), value, namespaces));
                }
            }

            sourceIndex = sourceIndex + 1;
        }

        Qt3EnvironmentNode[] parameters = test.Environment.Find("param");
        int parameterIndex = 0;
        while (parameterIndex < parameters.Length)
        {
            Qt3EnvironmentNode parameter = parameters[parameterIndex];
            string name = Attribute(parameter, "name", "");
            string select = Attribute(parameter, "select", "()");
            staticContext.Variables = variables.ToArray();
            dynamicContext.Variables = variables.ToArray();
            XPathCompiledExpression parameterExpression = new XPathCompiler().CompileXPath(select, staticContext);
            XdmSequence value = new XPathEvaluator().EvaluateXPath(parameterExpression, dynamicContext).Value!;
            variables.Add(CreateVariable(name, value, namespaces));
            parameterIndex = parameterIndex + 1;
        }

        staticContext.Variables = variables.ToArray();
        dynamicContext.Variables = variables.ToArray();
        Qt3EnvironmentNode? contextItem = test.Environment.Find("context-item").FirstOrDefault();
        if (contextItem != null)
        {
            string select = Attribute(contextItem, "select", "()");
            XPathCompiledExpression itemExpression = new XPathCompiler().CompileXPath(select, staticContext);
            dynamicContext.ContextItem = new XPathEvaluator().EvaluateXPath(itemExpression, dynamicContext).Value;
        }

        return new Qt3EvaluationContext { StaticContext = staticContext, DynamicContext = dynamicContext };
    }

    internal XPathDecimalFormat CreateDecimalFormat(Qt3EnvironmentNode node)
    {
        XPathDecimalFormat result = new XPathDecimalFormat();
        if (node.Attributes.TryGetValue("name", out string? lexicalName))
        {
            int colon = lexicalName.IndexOf(':');
            result.Name = new XmlName
            {
                Prefix = colon < 0 ? null : lexicalName.Substring(0, colon),
                LocalName = colon < 0 ? lexicalName : lexicalName.Substring(colon + 1),
                NamespaceUri = colon < 0 ? "" : node.NameNamespaceUri,
                QualifiedName = lexicalName
            };
        }

        result.DecimalSeparator = ReadDecimalFormatAttribute(node, "decimal-separator");
        result.GroupingSeparator = ReadDecimalFormatAttribute(node, "grouping-separator");
        result.Infinity = ReadDecimalFormatAttribute(node, "infinity");
        result.NaN = ReadDecimalFormatAttribute(node, "NaN");
        result.MinusSign = ReadDecimalFormatAttribute(node, "minus-sign");
        result.Percent = ReadDecimalFormatAttribute(node, "percent");
        result.PerMille = ReadDecimalFormatAttribute(node, "per-mille");
        result.ZeroDigit = ReadDecimalFormatAttribute(node, "zero-digit");
        result.Digit = ReadDecimalFormatAttribute(node, "digit");
        result.PatternSeparator = ReadDecimalFormatAttribute(node, "pattern-separator");
        result.ExponentSeparator = ReadDecimalFormatAttribute(node, "exponent-separator");
        return result;
    }

    internal string? ReadDecimalFormatAttribute(Qt3EnvironmentNode node, string name)
    {
        return node.Attributes.TryGetValue(name, out string? value) ? value : null;
    }

    internal string ClassifyDependencies(Qt3TestCase test)
    {
        if (!SchemaAware && test.Environment.Find("schema").Length != 0)
        {
            return UnsupportedDependencyPrefix + "schema-aware environment.";
        }

        if (test.Expression.Contains("http://www.w3.org/2013/collation/UCA", StringComparison.Ordinal))
        {
            return UnsupportedUcaCollationReason;
        }

        Qt3EnvironmentNode? unsupportedCollation = test.Environment.Find("collation")
            .FirstOrDefault(node => Attribute(node, "uri", node.Value.Trim()) != "http://www.w3.org/2005/xpath-functions/collation/codepoint");
        if (unsupportedCollation != null)
        {
            return UnsupportedDependencyPrefix + "custom collation='" + Attribute(unsupportedCollation, "uri", unsupportedCollation.Value.Trim()) + "'.";
        }

        int index = 0;
        while (index < test.Dependencies.Length)
        {
            Qt3Dependency dependency = test.Dependencies[index];
            if (dependency.Type == "feature")
            {
                if (dependency.Value == "fn-load-xquery-module" || dependency.Value == "fn-transform-XSLT")
                {
                    return dependency.Satisfied
                        ? UnsupportedDependencyPrefix + "feature='" + dependency.Value + "'."
                        : PresentDependencyPrefix + "feature='" + dependency.Value + "'.";
                }

                bool supported = SupportsFeature(dependency.Value);
                if (dependency.Satisfied && !supported)
                {
                    return UnsupportedDependencyPrefix + "feature='" + dependency.Value + "'.";
                }

                if (!dependency.Satisfied && supported)
                {
                    return PresentDependencyPrefix + "feature='" + dependency.Value + "'.";
                }
            }

            if (dependency.Type == "calendar" && dependency.Satisfied)
            {
                return UnsupportedDependencyPrefix + "calendar='" + dependency.Value + "'.";
            }

            if (dependency.Type == "format-integer-sequence" && dependency.Satisfied)
            {
                return UnsupportedDependencyPrefix + "format-integer-sequence='" + dependency.Value + "'.";
            }

            if (dependency.Type == "unicode-normalization-form" && dependency.Value == "FULLY-NORMALIZED" && dependency.Satisfied)
            {
                return UnsupportedDependencyPrefix + "unicode-normalization-form='FULLY-NORMALIZED'.";
            }

            if (dependency.Type == "unicode-version" && dependency.Satisfied)
            {
                return UnsupportedDependencyPrefix + "unicode-version='" + dependency.Value + "'.";
            }

            if ((dependency.Type == "language" || dependency.Type == "default-language") && dependency.Value != "en" && dependency.Satisfied)
            {
                return UnsupportedDependencyPrefix + dependency.Type + "='" + dependency.Value + "'.";
            }

            if (dependency.Type == "xsd-version" && dependency.Value == "1.1" && dependency.Satisfied)
            {
                return UnsupportedDependencyPrefix + "xsd-version='1.1'.";
            }

            if (dependency.Type == "xml-version" && dependency.Value != "1.0" && dependency.Satisfied)
            {
                return UnsupportedDependencyPrefix + "xml-version='" + dependency.Value + "'.";
            }

            index = index + 1;
        }

        return "";
    }

    internal string ClassifyImplementationLimits(Qt3TestCase test)
    {
        if (test.Expression.Contains("xs:anyURI", StringComparison.Ordinal) && (test.Expression.Contains("%", StringComparison.Ordinal) || test.Expression.Contains("\":/", StringComparison.Ordinal)))
        {
            return AnyUriLexicalVersionReason;
        }

        if (test.Dependencies.Any(dependency => dependency.Type == "xsd-version" && dependency.Value == "1.0" && dependency.Satisfied) && (test.Expression.Contains("+INF", StringComparison.Ordinal) || test.Expression.Contains("\"0000", StringComparison.Ordinal) || test.Expression.Contains("\"-0000", StringComparison.Ordinal) || test.Expression.Contains("'0000", StringComparison.Ordinal) || test.Expression.Contains("'-0000", StringComparison.Ordinal)))
        {
            return Xsd11LexicalVersionReason;
        }

        if (test.TestSetName == "prod-CastExpr.derived" && Regex.IsMatch(test.Name, "^cbcl-cast-(?:byte|int|long|negativeInteger|nonNegativeInteger|nonPositiveInteger|positiveInteger|short|unsignedByte|unsignedInt|unsignedLong|unsignedShort)-00[12]$", RegexOptions.CultureInvariant))
        {
            return NumericCastErrorCodeReason;
        }

        if (test.Assertion.Kind == "assert-false" && test.Expression.Contains("castable as", StringComparison.Ordinal) && Regex.IsMatch(test.Expression, "[0-9]{16,}", RegexOptions.CultureInvariant) && Regex.IsMatch(test.Expression, "xs:(?:date|dateTime|duration|yearMonthDuration|dayTimeDuration|gYear|gYearMonth)", RegexOptions.CultureInvariant))
        {
            return ExtendedValueRangeReason;
        }

        if (test.Assertion.Kind == "error")
        {
            string code = Attribute(test.Assertion, "code", "");
            if ((code == "FODT0001" || code == "FODT0002") && Regex.IsMatch(test.Expression, "[0-9]{16,}", RegexOptions.CultureInvariant))
            {
                return ExtendedValueRangeReason;
            }
        }

        Match range = Regex.Match(test.Expression, "(?<![0-9])([0-9]+)\\s+to\\s+([0-9]+)(?![0-9])", RegexOptions.CultureInvariant);
        if (range.Success)
        {
            System.Numerics.BigInteger first = System.Numerics.BigInteger.Parse(range.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            System.Numerics.BigInteger last = System.Numerics.BigInteger.Parse(range.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (last >= first && last - first + System.Numerics.BigInteger.One >= new System.Numerics.BigInteger(1000000))
            {
                return SequenceLimitReason;
            }
        }

        if (test.Expression.Contains("65536*65536", StringComparison.Ordinal) && test.Expression.Contains(" to ", StringComparison.Ordinal))
        {
            return SequenceLimitReason;
        }

        if (test.Expression.Contains("1024 * 1024", StringComparison.Ordinal) && test.Expression.Contains("string-join", StringComparison.Ordinal))
        {
            return SequenceLimitReason;
        }

        Match quantifier = Regex.Match(test.Expression, "\\{([0-9]{7,})(?:[,}])", RegexOptions.CultureInvariant);
        if (quantifier.Success)
        {
            return RegexQuantifierLimitReason;
        }

        if (Regex.IsMatch(test.Expression, "#[0-9]{11,}", RegexOptions.CultureInvariant))
        {
            return FunctionArityLimitReason;
        }

        return "";
    }

    internal bool SupportsFeature(string feature)
    {
        if (feature == "schemaImport" || feature == "schemaValidation" || feature == "typedData" || feature == "schema-location-hint")
        {
            return SchemaAware;
        }

        return feature == "collection-stability"
            || feature == "higherOrderFunctions"
            || feature == "infoset-dtd"
            || feature == "namespace-axis"
            || feature == "non_empty_sequence_collection"
            || feature == "serialization"
            || feature == "xpath-1.0-compatibility";
    }

    internal bool RequiresFeature(Qt3TestCase test, string feature)
    {
        return test.Dependencies.Any(dependency => dependency.Type == "feature" && dependency.Value == feature && dependency.Satisfied);
    }

    internal string ReadDependencyValue(Qt3TestCase test, string type, string fallback)
    {
        return test.Dependencies.Where(dependency => dependency.Type == type && dependency.Satisfied).Select(dependency => dependency.Value).FirstOrDefault() ?? fallback;
    }

    internal bool EvaluateAssertion(Qt3TestCase test, Qt3RunResult result, Qt3Assertion assertion)
    {
        if (assertion.Kind == "all-of")
        {
            foreach (Qt3Assertion child in assertion.Children)
            {
                if (!EvaluateAssertion(test, result, child))
                {
                    result.Reason = "The QT3 result did not satisfy child " + child.Kind + ": " + child.Value.Trim();
                    return false;
                }
            }

            return true;
        }

        if (assertion.Kind == "any-of")
        {
            return assertion.Children.Any(child => EvaluateAssertion(test, result, child));
        }

        if (assertion.Kind == "not")
        {
            return assertion.Children.Length == 1 && !EvaluateAssertion(test, result, assertion.Children[0]);
        }

        if (assertion.Kind == "error")
        {
            return ErrorMatches(result, Attribute(assertion, "code", "*"));
        }

        if (assertion.Kind == "assert-serialization-error")
        {
            if (result.Exception != null || result.Value == null)
            {
                return false;
            }

            try
            {
                Serialize(result.Value);
                return false;
            }
            catch (Exception failure)
            {
                return CodeMatches(ReadErrorCode(failure), Attribute(assertion, "code", "*"));
            }
        }

        if (result.Exception != null || result.Value == null)
        {
            return false;
        }

        if (assertion.Kind == "assert-empty")
        {
            return result.Value.Count == 0;
        }

        if (assertion.Kind == "assert-count")
        {
            return result.Value.Count == int.Parse(assertion.Value.Trim(), System.Globalization.CultureInfo.InvariantCulture);
        }

        if (assertion.Kind == "assert-true")
        {
            return ReadBoolean(result.Value);
        }

        if (assertion.Kind == "assert-false")
        {
            return !ReadBoolean(result.Value);
        }

        if (assertion.Kind == "assert")
        {
            return EvaluateBooleanWithResult(test, result.Value, assertion.Value);
        }

        if (assertion.Kind == "assert-eq")
        {
            return EvaluateBooleanWithResult(test, result.Value, "$result eq (" + assertion.Value + ")");
        }

        if (assertion.Kind == "assert-deep-eq")
        {
            return EvaluateBooleanWithResult(test, result.Value, "deep-equal($result, (" + assertion.Value + "))");
        }

        if (assertion.Kind == "assert-type")
        {
            return EvaluateBooleanWithResult(test, result.Value, "$result instance of " + assertion.Value.Trim());
        }

        if (assertion.Kind == "assert-permutation")
        {
            string expected = "(" + assertion.Value + ")";
            string comparison = "let $expected := " + expected
                + " return count($result) eq count($expected) and (every $item in $result satisfies count($result[deep-equal(., $item)]) eq count($expected[deep-equal(., $item)]))";
            return EvaluateBooleanWithResult(test, result.Value, comparison);
        }

        if (assertion.Kind == "assert-string-value")
        {
            string actual = StringValue(result.Value, Attribute(assertion, "normalize-space", "false") == "true");
            string expected = assertion.FilePath == null ? assertion.Value : Suite.ReadText(assertion.FilePath);
            if (Attribute(assertion, "normalize-space", "false") == "true")
            {
                expected = NormalizeSpace(expected);
            }

            return actual == expected;
        }

        if (assertion.Kind == "assert-xml")
        {
            string expected = assertion.FilePath == null ? assertion.Value : Suite.ReadText(assertion.FilePath);
            return XmlEquivalent(SerializeXml(result.Value), expected, Attribute(assertion, "ignore-prefixes", "false") == "true");
        }

        if (assertion.Kind == "assert-serialization")
        {
            string expected = assertion.FilePath == null ? assertion.Value : Suite.ReadText(assertion.FilePath);
            return Serialize(result.Value) == expected;
        }

        if (assertion.Kind == "serialization-matches")
        {
            string flags = Attribute(assertion, "flags", "");
            return EvaluateBoolean(test, "matches(" + XPathString(Serialize(result.Value)) + ", " + XPathString(assertion.Value) + ", " + XPathString(flags) + ")");
        }

        throw new InvalidDataException("The QT3 assertion '" + assertion.Kind + "' has no evaluator.");
    }

    internal bool EvaluateBoolean(Qt3TestCase test, string expression)
    {
        return ReadBoolean(Evaluate(test, expression));
    }

    internal bool EvaluateBooleanWithResult(Qt3TestCase test, XdmSequence value, string expression)
    {
        return ReadBoolean(EvaluateWithResult(test, value, expression));
    }

    internal XdmSequence EvaluateWithResult(Qt3TestCase test, XdmSequence value, string expression)
    {
        Qt3EvaluationContext context = CreateContext(test);
        XPathVariableBinding result = CreateVariable("result", value, []);
        result.StaticTypeSyntax = new XPathTypeSyntax("item", null, "item", null, "*", false, new SourceRange());
        XPathVariableBinding[] existing = context.StaticContext.Variables == null ? [] : context.StaticContext.Variables;
        XPathVariableBinding[] variables = existing.Concat([result]).ToArray();
        context.StaticContext.Variables = variables;
        context.DynamicContext.Variables = variables;
        XPathCompiledExpression compiled = new XPathCompiler().CompileXPath(expression, context.StaticContext);
        return new XPathEvaluator().EvaluateXPath(compiled, context.DynamicContext).Value ?? new XdmSequenceOperations().CreateEmptyXdmSequence();
    }

    internal bool ReadBoolean(XdmSequence value)
    {
        return new XdmTypeConverter().EffectiveBooleanValueXdmSequence(value);
    }

    internal string StringValue(XdmSequence value, bool normalize)
    {
        string expression = "string-join($result ! string(.), ' ')";
        XPathVariableBinding result = CreateVariable("result", value, []);
        XPathStaticContext staticContext = new XPathStaticContext { Variables = [result], AllowHigherOrderFunctions = true };
        XPathDynamicContext dynamicContext = new XPathDynamicContext { Variables = [result] };
        XdmSequence strings = new XPathEvaluator().EvaluateXPath(new XPathCompiler().CompileXPath(expression, staticContext), dynamicContext).Value!;
        string text = strings.AtomicValues == null || strings.AtomicValues.Length == 0 ? "" : strings.AtomicValues[0].LexicalValue ?? "";
        return normalize ? NormalizeSpace(text) : text;
    }

    internal string Serialize(XdmSequence value)
    {
        using StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        XmlSerializationOptions options = new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Xml,
            OmitXmlDeclaration = true,
            ItemSeparator = " "
        };
        new XmlSerializer().SerializeXdmSequence(value, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    internal string SerializeXml(XdmSequence value)
    {
        using StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        XmlSerializationOptions options = new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Xml,
            OmitXmlDeclaration = true,
            ItemSeparator = ""
        };
        new XmlSerializer().SerializeXdmSequence(value, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    internal bool XmlEquivalent(string actual, string expected, bool ignorePrefixes)
    {
        try
        {
            XElement actualRoot = XElement.Parse("<qt3-root>" + StripDocumentType(StripXmlDeclaration(actual)) + "</qt3-root>", LoadOptions.PreserveWhitespace);
            XElement expectedRoot = XElement.Parse("<qt3-root>" + StripDocumentType(StripXmlDeclaration(expected)) + "</qt3-root>", LoadOptions.PreserveWhitespace);
            actualRoot.Nodes().OfType<XText>().Where(node => string.IsNullOrWhiteSpace(node.Value)).Remove();
            expectedRoot.Nodes().OfType<XText>().Where(node => string.IsNullOrWhiteSpace(node.Value)).Remove();
            RemoveNamespaceDeclarations(actualRoot);
            RemoveNamespaceDeclarations(expectedRoot);
            NormalizeEmptyElements(actualRoot);
            NormalizeEmptyElements(expectedRoot);

            return XmlElementsEquivalent(actualRoot, expectedRoot);
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

    internal void NormalizeEmptyElements(XElement element)
    {
        foreach (XElement child in element.Elements())
        {
            NormalizeEmptyElements(child);
        }

        if (!element.Nodes().Any())
        {
            element.Add(new XText(""));
        }
    }

    internal bool XmlElementsEquivalent(XElement actual, XElement expected)
    {
        if (actual.Name != expected.Name)
        {
            return false;
        }

        XAttribute[] actualAttributes = actual.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration).ToArray();
        XAttribute[] expectedAttributes = expected.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration).ToArray();
        if (actualAttributes.Length != expectedAttributes.Length || actualAttributes.Any(attribute => expectedAttributes.All(candidate => candidate.Name != attribute.Name || candidate.Value != attribute.Value)))
        {
            return false;
        }

        XNode[] actualNodes = actual.Nodes().ToArray();
        XNode[] expectedNodes = expected.Nodes().ToArray();
        if (actualNodes.Length != expectedNodes.Length)
        {
            return false;
        }

        int index = 0;
        while (index < actualNodes.Length)
        {
            XElement? actualElement = actualNodes[index] as XElement;
            XElement? expectedElement = expectedNodes[index] as XElement;
            XText? actualText = actualNodes[index] as XText;
            XText? expectedText = expectedNodes[index] as XText;
            if (actualElement != null && expectedElement != null)
            {
                if (!XmlElementsEquivalent(actualElement, expectedElement))
                {
                    return false;
                }
            }
            else if (actualText != null && expectedText != null)
            {
                if (actualText.Value != expectedText.Value)
                {
                    return false;
                }
            }
            else if (!XNode.DeepEquals(actualNodes[index], expectedNodes[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
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

        int index = start + 9;
        int subsetDepth = 0;
        char quote = '\0';
        while (index < value.Length)
        {
            char character = value[index];
            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (character == '\'' || character == '"')
            {
                quote = character;
            }
            else if (character == '[')
            {
                subsetDepth = subsetDepth + 1;
            }
            else if (character == ']')
            {
                subsetDepth = subsetDepth - 1;
            }
            else if (character == '>' && subsetDepth == 0)
            {
                int end = index + 1;
                if (end < value.Length && value[end] == '\r')
                {
                    end = end + 1;
                }

                if (end < value.Length && value[end] == '\n')
                {
                    end = end + 1;
                }

                return value.Substring(0, start) + value.Substring(end);
            }

            index = index + 1;
        }

        return value;
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

    internal bool ErrorMatches(Qt3RunResult result, string expected)
    {
        return result.Exception != null && CodeMatches(result.ErrorCode, expected);
    }

    internal bool CodeMatches(string actual, string expected)
    {
        return expected.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(code => code == "*" || code == actual || code.StartsWith("Q{}", StringComparison.Ordinal) && code.Substring(3) == actual);
    }

    internal string ReadErrorCode(Exception failure)
    {
        XmlDiagnostic? diagnostic = failure switch
        {
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

    internal XPathVariableBinding CreateVariable(string lexicalName, XdmSequence value, IReadOnlyList<XmlNamespaceBinding> namespaces)
    {
        string name = lexicalName.StartsWith("$", StringComparison.Ordinal) ? lexicalName.Substring(1) : lexicalName;
        int colon = name.IndexOf(':');
        string prefix = colon < 0 ? "" : name.Substring(0, colon);
        string local = colon < 0 ? name : name.Substring(colon + 1);
        string? uri = namespaces.FirstOrDefault(binding => binding.Prefix == prefix)?.NamespaceUri;
        XPathVariableBinding result = new XPathVariableBinding
        {
            Name = new XmlName { Prefix = prefix, LocalName = local, NamespaceUri = uri, QualifiedName = name },
            Value = value,
            DeclaredType = XdmAtomicType.AnyAtomicType,
            DeclaredOccurrence = XdmOccurrence.ZeroOrMore
        };
        if (value.Count != 0 && value.ItemKinds != null && value.ItemKinds.All(kind => kind == XdmItemKind.Node))
        {
            result.StaticTypeSyntax = new XPathTypeSyntax("node", null, "node", null, "*", false, new SourceRange());
        }

        return result;
    }

    internal XmlResolverPolicy CreateResolverPolicy()
    {
        return new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https", "http", "surely-nobody-supports-this"],
            MaximumDepth = 64,
            MaximumResourceBytes = 134217728,
            MaximumWorkUnits = 268435456
        };
    }

    internal string Attribute(Qt3EnvironmentNode node, string name, string fallback)
    {
        return node.Attributes.TryGetValue(name, out string? value) ? value : fallback;
    }

    internal string Attribute(Qt3Assertion assertion, string name, string fallback)
    {
        return assertion.Attributes.TryGetValue(name, out string? value) ? value : fallback;
    }
}

internal sealed class Qt3EvaluationContext
{
    internal XPathStaticContext StaticContext = new XPathStaticContext();
    internal XPathDynamicContext DynamicContext = new XPathDynamicContext();
}

internal sealed class Qt3ArchiveResolver : IXmlResourceResolver, IXPathCollectionResolver
{
    internal Qt3SuiteAdapter Suite;
    internal Dictionary<string, string> Documents;
    internal Dictionary<string, Qt3EnvironmentNode> Collections;
    internal Dictionary<string, XmlDocument> DocumentCache;
    internal Dictionary<string, Qt3EnvironmentNode> ResourceMetadata;

    internal Qt3ArchiveResolver(Qt3SuiteAdapter suite, Qt3Environment environment)
    {
        Suite = suite;
        Documents = new Dictionary<string, string>(StringComparer.Ordinal);
        Collections = new Dictionary<string, Qt3EnvironmentNode>(StringComparer.Ordinal);
        DocumentCache = new Dictionary<string, XmlDocument>(StringComparer.Ordinal);
        ResourceMetadata = new Dictionary<string, Qt3EnvironmentNode>(StringComparer.Ordinal);
        Qt3EnvironmentNode[] sources = environment.Find("source");
        int sourceIndex = 0;
        while (sourceIndex < sources.Length)
        {
            Qt3EnvironmentNode source = sources[sourceIndex];
            if (source.FilePath != null)
            {
                ResourceMetadata[source.FilePath] = source;
                Documents[Suite.ResourceUri(source.FilePath)] = source.FilePath;
                if (source.Attributes.TryGetValue("uri", out string? uri))
                {
                    Documents[uri] = source.FilePath;
                    Documents[ResolveEnvironmentUri(uri, environment.CatalogPath)] = source.FilePath;
                }
            }

            sourceIndex = sourceIndex + 1;
        }

        Qt3EnvironmentNode[] resources = environment.Find("resource");
        int resourceIndex = 0;
        while (resourceIndex < resources.Length)
        {
            Qt3EnvironmentNode resource = resources[resourceIndex];
            if (resource.FilePath != null)
            {
                ResourceMetadata[resource.FilePath] = resource;
                Documents[Suite.ResourceUri(resource.FilePath)] = resource.FilePath;
                if (resource.Attributes.TryGetValue("uri", out string? uri))
                {
                    Documents[uri] = resource.FilePath;
                    Documents[ResolveEnvironmentUri(uri, environment.CatalogPath)] = resource.FilePath;
                }
            }

            resourceIndex = resourceIndex + 1;
        }

        foreach (Qt3EnvironmentNode collection in environment.Find("collection"))
        {
            Collections[collection.Attributes.TryGetValue("uri", out string? uri) ? uri : ""] = collection;
            foreach (Qt3EnvironmentNode source in collection.Children.Where(child => child.Kind == "source"))
            {
                if (source.FilePath != null)
                {
                    ResourceMetadata[source.FilePath] = source;
                    Documents[Suite.ResourceUri(source.FilePath)] = source.FilePath;
                    if (source.Attributes.TryGetValue("uri", out string? sourceUri))
                    {
                        Documents[sourceUri] = source.FilePath;
                        Documents[ResolveEnvironmentUri(sourceUri, environment.CatalogPath)] = source.FilePath;
                    }
                }
            }
        }
    }

    internal string ResolveEnvironmentUri(string supplied, string catalogPath)
    {
        if (Uri.TryCreate(supplied, UriKind.Absolute, out Uri? absolute))
        {
            return absolute.AbsoluteUri;
        }

        return new Uri(new Uri(Suite.ResourceUri(catalogPath)), supplied).AbsoluteUri;
    }

    public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
    {
        string absolute = ResolveUri(systemIdentifier, baseUri);
        string? path = ResolvePath(absolute);
        if (path == null || !Suite.Resources.TryGetValue(path, out byte[]? bytes))
        {
            return new XmlResolvedResource { Kind = kind, Access = XmlResourceAccess.Denied, RequestedUri = systemIdentifier, AbsoluteUri = absolute };
        }

        Qt3EnvironmentNode? metadata = ResourceMetadata.TryGetValue(path, out Qt3EnvironmentNode? found) ? found : null;
        return new XmlResolvedResource
        {
            Kind = kind,
            Access = XmlResourceAccess.Allowed,
            RequestedUri = systemIdentifier,
            AbsoluteUri = absolute,
            PublicIdentifier = publicIdentifier,
            SystemIdentifier = systemIdentifier,
            MediaType = metadata == null ? null : (metadata.Attributes.TryGetValue("media-type", out string? mediaType) ? mediaType : null),
            EncodingName = metadata == null ? null : (metadata.Attributes.TryGetValue("encoding", out string? encoding) ? encoding : null),
            Input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = bytes, BaseUri = absolute }
        };
    }

    public XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy)
    {
        string? path = ResolvePath(uri);
        if (path == null)
        {
            return null;
        }

        try
        {
            return ParseDocument(path);
        }
        catch (xml.XmlException failure)
        {
            XmlDiagnostic diagnostic = new XmlDiagnostic
            {
                Code = XPathErrorCodes.FODC0002,
                Category = XmlErrorCategory.XPathDynamic,
                Severity = XmlDiagnosticSeverity.Fatal,
                Message = failure.Message
            };
            throw new XPathDynamicException(failure.Message, diagnostic, null);
        }
        catch (XmlWellFormednessException failure)
        {
            XmlDiagnostic diagnostic = new XmlDiagnostic
            {
                Code = XPathErrorCodes.FODC0002,
                Category = XmlErrorCategory.XPathDynamic,
                Severity = XmlDiagnosticSeverity.Fatal,
                Message = failure.Message
            };
            throw new XPathDynamicException(failure.Message, diagnostic, null);
        }
    }

    public XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy)
    {
        string key = uri ?? "";
        if (!Collections.TryGetValue(key, out Qt3EnvironmentNode? collection))
        {
            XmlDiagnostic diagnostic = new XmlDiagnostic
            {
                Code = XPathErrorCodes.FODC0002,
                Category = XmlErrorCategory.XPathDynamic,
                Severity = XmlDiagnosticSeverity.Fatal,
                Message = "The requested XPath collection is not available."
            };
            throw new XPathDynamicException(diagnostic.Message, diagnostic, null);
        }

        XdmSequence[] members = collection.Children
            .Where(child => child.Kind == "source" && child.FilePath != null)
            .Select(child => new XdmSequenceOperations().CreateXdmNodeSequence(ParseDocument(child.FilePath!).DocumentNode!))
            .ToArray();
        return new XdmSequenceOperations().ConcatenateXdmSequences(members);
    }

    public string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy)
    {
        string key = uri ?? "";
        if (!Collections.TryGetValue(key, out Qt3EnvironmentNode? collection))
        {
            XmlDiagnostic diagnostic = new XmlDiagnostic
            {
                Code = XPathErrorCodes.FODC0002,
                Category = XmlErrorCategory.XPathDynamic,
                Severity = XmlDiagnosticSeverity.Fatal,
                Message = "The requested XPath URI collection is not available."
            };
            throw new XPathDynamicException(diagnostic.Message, diagnostic, null);
        }

        return collection.Children
            .Where(child => child.Kind == "source" && child.FilePath != null)
            .Select(child => child.Attributes.TryGetValue("uri", out string? sourceUri) ? sourceUri : Suite.ResourceUri(child.FilePath!))
            .ToArray();
    }

    internal XmlDocument ParseDocument(string path)
    {
        if (DocumentCache.TryGetValue(path, out XmlDocument? cached))
        {
            return cached;
        }

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
                MaximumResourceBytes = 134217728,
                MaximumWorkUnits = 268435456
            }
        };
        byte[] bytes = Suite.Resources[path];
        XmlInput input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = bytes, BaseUri = Suite.ResourceUri(path) };
        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, this, null, null);
        DocumentCache[path] = document;
        return document;
    }

    internal string ResolveUri(string? systemIdentifier, string? baseUri)
    {
        string supplied = systemIdentifier ?? "";
        if (Uri.TryCreate(supplied, UriKind.Absolute, out Uri? absolute))
        {
            return absolute.AbsoluteUri;
        }

        return new Uri(new Uri(baseUri ?? Qt3SuiteAdapter.ArchiveBaseUri), supplied).AbsoluteUri;
    }

    internal string? ResolvePath(string uri)
    {
        if (Documents.TryGetValue(uri, out string? path))
        {
            return path;
        }

        if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? absolute) && absolute.AbsoluteUri.StartsWith(Qt3SuiteAdapter.ArchiveBaseUri, StringComparison.Ordinal))
        {
            string archivePath = Uri.UnescapeDataString(absolute.AbsolutePath.TrimStart('/'));
            return Suite.Resources.ContainsKey(archivePath) ? archivePath : null;
        }

        return null;
    }
}
