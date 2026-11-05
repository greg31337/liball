using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class ExtensionRegistryTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void XPathFunctionBindingCapturesImplementationAtCompileTime()
    {
        ConstantFunction original = new ConstantFunction("original");
        XPathFunctionBinding binding = FunctionBinding(original);
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "e", NamespaceUri = "urn:extensions" }],
            Functions = [binding]
        };

        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("e:value()", context);
        binding.Name!.LocalName = "changed";
        binding.Implementation = new ConstantFunction("changed");
        context.Functions = [];

        XdmSequence value = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()).Value!;

        Assert.That(StringValues(value), Is.EqualTo(new[] { "original" }));
    }

    [Test]
    public void XsltUsesRegisteredFunctionsInstructionsAndTheirCompileSnapshot()
    {
        ConstantFunction function = new ConstantFunction("function");
        RecordingInstruction instruction = new RecordingInstruction();
        XPathFunctionBinding functionBinding = FunctionBinding(function);
        XsltExtensionBinding instructionBinding = new XsltExtensionBinding
        {
            Name = new XmlName { LocalName = "emit", NamespaceUri = "urn:extensions" },
            BindingKey = "instruction",
            IsInstruction = true,
            InstructionImplementation = instruction
        };
        XsltCompileOptions options = CompileOptions();
        options.Functions = [functionBinding];
        options.Extensions = [instructionBinding];
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:sequence select='e:value()'/><e:emit><xsl:sequence select=\"'content'\"/><xsl:fallback><xsl:sequence select=\"'unused'\"/></xsl:fallback></e:emit></xsl:template>",
            "extension-element-prefixes='e'");

        XsltExecutable executable = Compile(source, options);
        functionBinding.Implementation = new ConstantFunction("changed");
        instructionBinding.InstructionImplementation = new RecordingInstruction();
        instructionBinding.Name!.LocalName = "changed";
        executable.CompileOptions!.Functions = [];
        executable.CompileOptions.Extensions = [];

        XsltTransformResult result = new XsltTransformer().TransformXslt(executable, new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } });

        Assert.Multiple(() =>
        {
            Assert.That(StringValues(result.PrincipalValue!), Is.EqualTo(new[] { "function", "instruction" }));
            Assert.That(StringValues(instruction.Content!), Is.EqualTo(new[] { "content" }));
        });
    }

    [Test]
    public void XsltUsesRegisteredCollationForExpressionsAndSorting()
    {
        XsltCompileOptions options = CompileOptions();
        options.Collations = [new XPathCollationBinding { Uri = "urn:reverse", Implementation = new ReverseCollation() }];
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:perform-sort select=\"('a','b')\"><xsl:sort collation='urn:reverse'/></xsl:perform-sort></xsl:template>",
            "default-collation='urn:reverse'");

        XsltExecutable executable = Compile(source, options);
        options.Collations[0].Uri = "urn:changed";
        options.Collations[0].Implementation = new XPathCodepointCollation();
        executable.CompileOptions!.Collations = [];
        XsltTransformResult result = new XsltTransformer().TransformXslt(executable, new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } });

        Assert.That(StringValues(result.PrincipalValue!), Is.EqualTo(new[] { "b", "a" }));
    }

    [Test]
    public void XsltSerializationMethodBindingIsImmutableAfterCompilation()
    {
        RecordingSerializationMethod method = new RecordingSerializationMethod("serialized");
        XmlSerializationMethodBinding binding = new XmlSerializationMethodBinding
        {
            Name = new XmlName { LocalName = "custom", NamespaceUri = "urn:extensions" },
            Implementation = method
        };
        XsltCompileOptions options = CompileOptions();
        options.SerializationMethods = [binding];
        string source = Stylesheet("<xsl:output method='e:custom'/><xsl:template name='entry'><xsl:sequence select=\"'payload'\"/></xsl:template>", "");
        XsltExecutable executable = Compile(source, options);
        binding.Name!.LocalName = "changed";
        binding.Implementation = new RecordingSerializationMethod("changed");
        executable.CompileOptions!.SerializationMethods = [];
        StringWriter writer = new StringWriter();

        XsltTransformResult result = new XsltTransformer().TransformXslt(executable, new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            PrincipalOutput = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(writer.ToString(), Is.EqualTo("serialized"));
            Assert.That(StringValues(method.Sequence!), Is.EqualTo(new[] { "payload" }));
        });
    }

    [Test]
    public void XsltCopiesDynamicResolverTraceAndCancellationHooksPerRun()
    {
        XsltExecutable executable = Compile(Stylesheet("<xsl:template name='entry'/>", ""), CompileOptions());
        XPathCollectionResolver collections = new XPathCollectionResolver();
        XPathTraceCollector trace = new XPathTraceCollector();
        XmlCancellationFlag cancellation = new XmlCancellationFlag();
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            CollectionResolver = collections,
            XPathTraceReceiver = trace,
            Cancellation = cancellation
        };

        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
        options.CollectionResolver = null;
        options.XPathTraceReceiver = null;
        options.Cancellation = null;

        Assert.Multiple(() =>
        {
            Assert.That(state.DynamicContext.CollectionResolver, Is.SameAs(collections));
            Assert.That(state.DocumentPool.Resolver, Is.SameAs(collections));
            Assert.That(state.DynamicContext.TraceReceiver, Is.SameAs(trace));
            Assert.That(state.DynamicContext.Cancellation, Is.SameAs(cancellation));
        });
    }

    private static XPathFunctionBinding FunctionBinding(IXPathFunction implementation)
    {
        return new XPathFunctionBinding
        {
            Name = new XmlName { Prefix = "e", LocalName = "value", NamespaceUri = "urn:extensions", QualifiedName = "e:value" },
            Arity = 0,
            ResultType = XdmAtomicType.String,
            ResultOccurrence = XdmOccurrence.ExactlyOne,
            BindingKey = "value",
            Implementation = implementation,
            IsDeterministic = true
        };
    }

    private static XsltExecutable Compile(string source, XsltCompileOptions options)
    {
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source, BaseUri = "https://example.test/main.xsl" }, options, new XmlDefaultResolver());
    }

    private static XsltCompileOptions CompileOptions()
    {
        return new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied, MaximumDepth = 16, MaximumResourceBytes = 1048576, MaximumWorkUnits = 1048576 }
        };
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:e='urn:extensions' version='3.0' " + attributes + ">" + content + "</xsl:stylesheet>";
    }

    private static string[] StringValues(XdmSequence sequence)
    {
        return sequence.AtomicValues == null ? [] : sequence.AtomicValues.Select(value => value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "").ToArray();
    }

    private sealed class ConstantFunction : IXPathFunction
    {
        internal string Value;

        internal ConstantFunction(string value)
        {
            Value = value;
        }

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            return Atomic(Value);
        }
    }

    private sealed class RecordingInstruction : IXsltExtensionInstruction
    {
        internal XdmSequence? Content;

        public XdmSequence ExecuteXsltExtensionInstruction(XsltExtensionBinding binding, XdmSequence content, XPathDynamicContext context)
        {
            Content = content;
            return Atomic("instruction");
        }
    }

    private sealed class ReverseCollation : IXPathCollation
    {
        public int CompareXPathCollation(string left, string right)
        {
            return string.Compare(right, left, StringComparison.Ordinal);
        }

        public byte[] CreateXPathCollationKey(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }

    private sealed class RecordingSerializationMethod : IXmlSerializationMethod
    {
        internal string Text;
        internal XdmSequence? Sequence;

        internal RecordingSerializationMethod(string text)
        {
            Text = text;
        }

        public void SerializeXmlSequence(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options)
        {
            Sequence = sequence;
            output.TextWriter!.Write(Text);
        }
    }

    private static XdmSequence Atomic(string value)
    {
        return new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = value }]
        };
    }
}
