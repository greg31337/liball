using System.Globalization;
using xml;

namespace xml.Tests;

[TestFixture]
public class ConcurrencyCorrectnessTests
{
    private const int OperationCount = 8;
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void ParserAndConfigurationCanBeReusedByParallelIndependentParses()
    {
        XmlParser parser = new XmlParser();
        XmlParserOptions options = ParserOptions();
        ManualResetEventSlim gate = new ManualResetEventSlim(false);
        Task<XmlDocument>[] operations = Enumerable.Range(0, OperationCount).Select(index => Task.Factory.StartNew(() =>
        {
            gate.Wait();
            string value = index.ToString(CultureInfo.InvariantCulture);
            return parser.ParseXmlDocument(
                new XmlInput { Text = "<root id='" + value + "'><value>" + value + "</value></root>", BaseUri = "https://example.test/input-" + value + ".xml" },
                options,
                null,
                null,
                null);
        }, TaskCreationOptions.LongRunning)).ToArray();

        gate.Set();
        XmlDocument[] documents = Task.WhenAll(operations).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(documents.Distinct().Count(), Is.EqualTo(OperationCount));
            Assert.That(documents.Select(document => document.DocumentElement).Distinct().Count(), Is.EqualTo(OperationCount));
            Assert.That(options.PreserveSourceLocations, Is.True);
            int index = 0;
            while (index < documents.Length)
            {
                string value = index.ToString(CultureInfo.InvariantCulture);
                XmlDocument document = documents[index];
                Assert.That(document.DocumentUri, Is.EqualTo("https://example.test/input-" + value + ".xml"), "document URI " + index);
                Assert.That(document.DocumentElement!.Attributes!.Single().Value, Is.EqualTo(value), "attribute " + index);
                Assert.That(document.DocumentElement.Children!.Single().Children!.Single().Value, Is.EqualTo(value), "text " + index);
                Assert.That(document.DocumentElement.OwnerDocument, Is.SameAs(document), "ownership " + index);
                Assert.That(document.DocumentElement.Range, Is.Not.Null, "source range " + index);
                index = index + 1;
            }
        });
    }

    [Test]
    public void CompiledXPathPlanKeepsVariablesAndResolverCachesIsolatedInParallel()
    {
        XPathVariableBinding declaration = new XPathVariableBinding
        {
            Name = new XmlName { LocalName = "value" },
            DeclaredType = XdmAtomicType.Integer,
            DeclaredOccurrence = XdmOccurrence.ExactlyOne,
            IsExternal = true,
            IsRequired = true
        };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("($value + 1, $value + 2)", new XPathStaticContext { Variables = [declaration] });
        PerRunVariableResolver[] resolvers = Enumerable.Range(0, OperationCount).Select(index => new PerRunVariableResolver(index)).ToArray();
        ManualResetEventSlim gate = new ManualResetEventSlim(false);
        Task<XPathEvaluationResult>[] operations = Enumerable.Range(0, OperationCount).Select(index => Task.Factory.StartNew(() =>
        {
            gate.Wait();
            return new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext { VariableResolver = resolvers[index] });
        }, TaskCreationOptions.LongRunning)).ToArray();

        gate.Set();
        XPathEvaluationResult[] results = Task.WhenAll(operations).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(expression.IsThreadSafe, Is.True);
            Assert.That(results.Select(result => result.DynamicContext).Distinct().Count(), Is.EqualTo(OperationCount));
            int index = 0;
            while (index < results.Length)
            {
                Assert.That(results[index].Value!.AtomicValues!.Select(value => value.IntegerValue), Is.EqualTo(new long[] { index + 1, index + 2 }), "value " + index);
                Assert.That(resolvers[index].Calls, Is.EqualTo(1), "resolver cache " + index);
                Assert.That(results[index].DynamicContext!.VariableResolver, Is.SameAs(resolvers[index]), "resolver identity " + index);
                index = index + 1;
            }
        });
    }

    [Test]
    public void CompiledSchemaSetCanValidateAndAnnotateIndependentTreesInParallel()
    {
        XmlSchemaSet schemas = CompileSchema();
        XmlParser parser = new XmlParser();
        XmlParserOptions parserOptions = ParserOptions();
        XmlDocument[] documents = Enumerable.Range(0, OperationCount).Select(index => parser.ParseXmlDocument(
            new XmlInput
            {
                Text = "<value xmlns='urn:concurrency'>" + (index % 3 == 0 ? "invalid-" + index.ToString(CultureInfo.InvariantCulture) : index.ToString(CultureInfo.InvariantCulture)) + "</value>",
                BaseUri = "https://example.test/schema-input-" + index.ToString(CultureInfo.InvariantCulture) + ".xml"
            },
            parserOptions,
            null,
            null,
            null)).ToArray();
        XmlSchemaValidator validator = new XmlSchemaValidator();
        XmlSchemaValidationOptions validationOptions = new XmlSchemaValidationOptions { AnnotateTree = true };
        ManualResetEventSlim gate = new ManualResetEventSlim(false);
        Task<XmlSchemaValidationResult>[] operations = Enumerable.Range(0, OperationCount).Select(index => Task.Factory.StartNew(() =>
        {
            gate.Wait();
            return validator.ValidateXmlSchema(documents[index], schemas, validationOptions);
        }, TaskCreationOptions.LongRunning)).ToArray();

        gate.Set();
        XmlSchemaValidationResult[] results = Task.WhenAll(operations).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(schemas.IsImmutable, Is.True);
            Assert.That(results.Select(result => result.Document).Distinct().Count(), Is.EqualTo(OperationCount));
            int index = 0;
            while (index < results.Length)
            {
                bool invalid = index % 3 == 0;
                Assert.That(results[index].Document, Is.SameAs(documents[index]), "document " + index);
                Assert.That(results[index].Validity, Is.EqualTo(invalid ? XmlSchemaValidity.Invalid : XmlSchemaValidity.Valid), "validity " + index);
                Assert.That(results[index].Diagnostics, invalid ? Is.Not.Empty : Is.Empty, "diagnostics " + index);
                if (!invalid)
                {
                    Assert.That(documents[index].DocumentElement!.TypeInfo!.ActualValue!.IntegerValue, Is.EqualTo(index), "annotation " + index);
                }

                index = index + 1;
            }
        });
    }

    [Test]
    public void CompiledXsltExecutableKeepsKeysMessagesAndResultsIsolatedInParallel()
    {
        string stylesheet =
            "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:xs='" + Xsd + "' version='3.0'>" +
            "<xsl:output method='text'/>" +
            "<xsl:param name='run' as='xs:integer' required='yes'/>" +
            "<xsl:key name='by-group' match='item' use='@group'/>" +
            "<xsl:template match='/'>" +
            "<xsl:message select=\"concat('run-', $run)\"/>" +
            "<xsl:result-document href='secondary-{$run}.txt'><xsl:sequence select='$run'/></xsl:result-document>" +
            "<xsl:value-of select=\"concat($run, ':', count(key('by-group', 'g')))\"/>" +
            "</xsl:template></xsl:stylesheet>";
        XsltCompileOptions compileOptions = new XsltCompileOptions
        {
            BaseUri = "https://example.test/concurrent.xsl",
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
        XsltExecutable executable = new XsltCompiler().CompileXslt(
            new XmlInput { Text = stylesheet, BaseUri = compileOptions.BaseUri },
            compileOptions,
            new XmlDefaultResolver());
        XmlDocument[] sources = Enumerable.Range(0, OperationCount).Select(index => ParseItems(index % 5 + 1)).ToArray();
        string[] output = new string[OperationCount];
        XsltTransformResult[] results = new XsltTransformResult[OperationCount];
        ManualResetEventSlim gate = new ManualResetEventSlim(false);
        XsltTransformer transformer = new XsltTransformer();
        Task[] operations = Enumerable.Range(0, OperationCount).Select(index => Task.Factory.StartNew(() =>
        {
            gate.Wait();
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            results[index] = transformer.TransformXslt(executable, new XsltTransformOptions
            {
                SourceDocument = sources[index],
                Parameters = [new XsltParameter { Name = new XmlName { LocalName = "run" }, Value = Integer(index) }],
                PrincipalOutput = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer },
                BaseOutputUri = "https://example.test/results/" + index.ToString(CultureInfo.InvariantCulture) + "/main.txt"
            });
            output[index] = writer.ToString();
        }, TaskCreationOptions.LongRunning)).ToArray();

        gate.Set();
        Task.WaitAll(operations);

        Assert.Multiple(() =>
        {
            Assert.That(executable.IsThreadSafe, Is.True);
            Assert.That(results.Distinct().Count(), Is.EqualTo(OperationCount));
            int index = 0;
            while (index < results.Length)
            {
                int itemCount = index % 5 + 1;
                XsltSecondaryResult secondary = results[index].SecondaryResults!.Single();
                Assert.That(output[index], Is.EqualTo(index.ToString(CultureInfo.InvariantCulture) + ":" + itemCount.ToString(CultureInfo.InvariantCulture)), "principal output " + index);
                Assert.That(results[index].Messages!.Single().Text, Is.EqualTo("run-" + index.ToString(CultureInfo.InvariantCulture)), "message " + index);
                Assert.That(secondary.Uri, Is.EqualTo("https://example.test/results/" + index.ToString(CultureInfo.InvariantCulture) + "/secondary-" + index.ToString(CultureInfo.InvariantCulture) + ".txt"), "result URI " + index);
                Assert.That(secondary.Value!.AtomicValues!.Single().IntegerValue, Is.EqualTo(index), "secondary value " + index);
                Assert.That(secondary.Disposition, Is.EqualTo(XsltResultDisposition.Commit), "result disposition " + index);
                index = index + 1;
            }
        });
    }

    private static XmlParserOptions ParserOptions()
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveSourceLocations = true
        };
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema = "<xs:schema xmlns:xs='" + Xsd + "' targetNamespace='urn:concurrency' elementFormDefault='qualified'><xs:element name='value' type='xs:int'/></xs:schema>";
        return new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Text = schema, BaseUri = "https://example.test/concurrency.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied } },
            new XmlDefaultResolver());
    }

    private static XmlDocument ParseItems(int count)
    {
        string content = "";
        int index = 0;
        while (index < count)
        {
            content = content + "<item group='g'/>";
            index = index + 1;
        }

        return new XmlParser().ParseXmlDocument(
            new XmlInput { Text = "<root>" + content + "</root>", BaseUri = "https://example.test/source.xml" },
            ParserOptions(),
            null,
            null,
            null);
    }

    private static XdmSequence Integer(long value)
    {
        XdmAtomicValue atomic = new XdmFactory().CreateXdmIntegerAtomicValue(value.ToString(CultureInfo.InvariantCulture), XdmAtomicType.Integer);
        return new XdmSequenceOperations().CreateXdmAtomicSequence(atomic);
    }

    private sealed class PerRunVariableResolver : IXPathVariableResolver
    {
        internal int Calls;
        internal long Value;

        internal PerRunVariableResolver(long value)
        {
            Value = value;
        }

        public XPathVariableBinding? ResolveXPathVariable(XmlName name, XPathDynamicContext context)
        {
            Calls = Calls + 1;
            return new XPathVariableBinding
            {
                Name = new XmlName { LocalName = "value" },
                Value = Integer(Value),
                DeclaredType = XdmAtomicType.Integer,
                DeclaredOccurrence = XdmOccurrence.ExactlyOne
            };
        }
    }
}
