using xml;

namespace xml.Tests;

[TestFixture]
public class ExtensionSafetyTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void TrustPolicyRejectsDeniedNamespacesAndMalformedSignatures()
    {
        XPathFunctionBinding denied = Binding(new ConstantFunction("value"));
        XPathStaticContext deniedContext = Context(denied);
        deniedContext.ExtensionPolicy = new XmlExtensionPolicy
        {
            Trust = XmlExtensionTrust.AllowListed,
            AllowedNamespaceUris = ["urn:allowed"]
        };
        XPathFunctionBinding malformed = Binding(new ConstantFunction("value"));
        malformed.Arity = 1;
        malformed.ParameterTypes = [XdmAtomicType.String];
        malformed.ParameterOccurrences = [];

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new XPathCompiler().CompileXPath("e:value()", deniedContext));
            Assert.Throws<ArgumentException>(() => new XPathCompiler().CompileXPath("e:value('x')", Context(malformed)));
        });
    }

    [Test]
    public void ExtensionFunctionsReceiveAndReturnConvertedValues()
    {
        TypedFunction implementation = new TypedFunction();
        XPathFunctionBinding binding = Binding(implementation);
        binding.Arity = 1;
        binding.ParameterTypes = [XdmAtomicType.Integer];
        binding.ParameterOccurrences = [XdmOccurrence.ExactlyOne];
        binding.ResultType = XdmAtomicType.Integer;

        XdmSequence result = Evaluate("e:value(xs:untypedAtomic('7'))", Context(binding), new XPathDynamicContext());

        Assert.Multiple(() =>
        {
            Assert.That(implementation.ArgumentType, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(result.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(result.AtomicValues[0].StringValue, Is.EqualTo("8"));
        });
    }

    [Test]
    public void ExtensionFunctionsAreLookedUpByExpandedNameAndArity()
    {
        XPathFunctionBinding zero = Binding(new ConstantFunction("zero"));
        XPathFunctionBinding one = Binding(new ConstantFunction("one"));
        one.Arity = 1;
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "e", NamespaceUri = "urn:extensions" }],
            Functions = [zero, one]
        };

        XdmSequence result = Evaluate("(e:value(), e:value('argument'))", context, new XPathDynamicContext());

        Assert.That(result.AtomicValues!.Select(value => value.StringValue), Is.EqualTo(new[] { "zero", "one" }));
    }

    [Test]
    public void DuplicateExtensionRegistrationsAreRejected()
    {
        XPathFunctionBinding firstFunction = Binding(new ConstantFunction("first"));
        XPathFunctionBinding secondFunction = Binding(new ConstantFunction("second"));
        XPathStaticContext xpath = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "e", NamespaceUri = "urn:extensions" }],
            Functions = [firstFunction, secondFunction]
        };
        XsltCompileOptions functions = CompileOptions();
        functions.Functions = [firstFunction, secondFunction];
        XsltExtensionBinding firstInstruction = InstructionBinding(new ConstantInstruction("first"));
        XsltExtensionBinding secondInstruction = InstructionBinding(new ConstantInstruction("second"));
        XsltCompileOptions instructions = CompileOptions();
        instructions.Extensions = [firstInstruction, secondInstruction];
        XPathCollationBinding firstCollation = new XPathCollationBinding { Uri = "urn:collation", Implementation = new ThrowingCollation() };
        XPathCollationBinding secondCollation = new XPathCollationBinding { Uri = "urn:collation", Implementation = new ThrowingCollation() };
        XsltCompileOptions collations = CompileOptions();
        collations.Collations = [firstCollation, secondCollation];
        XmlSerializationMethodBinding firstSerialization = SerializationBinding(new ThrowingSerializationMethod());
        XmlSerializationMethodBinding secondSerialization = SerializationBinding(new ThrowingSerializationMethod());
        XsltCompileOptions serializations = CompileOptions();
        serializations.SerializationMethods = [firstSerialization, secondSerialization];

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new XPathCompiler().CompileXPath("e:value()", xpath));
            Assert.Throws<ArgumentException>(() => Compile(Stylesheet("<xsl:template name='entry'/>", ""), functions));
            Assert.Throws<ArgumentException>(() => Compile(Stylesheet("<xsl:template name='entry'/>", ""), instructions));
            Assert.Throws<ArgumentException>(() => Compile(Stylesheet("<xsl:template name='entry'/>", ""), collations));
            Assert.Throws<ArgumentException>(() => Compile(Stylesheet("<xsl:template name='entry'/>", ""), serializations));
        });
    }

    [Test]
    public void ExtensionFunctionsRejectWrongArityArgumentTypeAndResultType()
    {
        XPathFunctionBinding typed = Binding(new ConstantFunction("not-an-integer"));
        typed.Arity = 1;
        typed.ParameterTypes = [XdmAtomicType.Integer];
        typed.ParameterOccurrences = [XdmOccurrence.ExactlyOne];
        typed.ResultType = XdmAtomicType.Integer;

        XPathStaticException arity = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("e:value()", Context(typed)))!;
        XPathTypeException argument = Assert.Throws<XPathTypeException>(() => new XPathCompiler().CompileXPath("e:value('text')", Context(typed)))!;
        XPathTypeException result = Assert.Throws<XPathTypeException>(() => Evaluate("e:value(1)", Context(typed), new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(arity.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
            Assert.That(argument.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void ExtensionFunctionFailuresAreWrappedAndInvocationLimitsAreEnforced()
    {
        XPathDynamicException failure = Assert.Throws<XPathDynamicException>(() => Evaluate("e:value()", Context(Binding(new ThrowingFunction())), new XPathDynamicContext()))!;
        XPathStaticContext limited = Context(Binding(new ConstantFunction("value")));
        limited.ExtensionPolicy = new XmlExtensionPolicy { Trust = XmlExtensionTrust.Trusted, MaximumInvocations = 1 };
        XPathDynamicException limit = Assert.Throws<XPathDynamicException>(() => Evaluate("(e:value(), e:value())", limited, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOER0000));
            Assert.That(failure.Message, Does.Contain("callback failure"));
            Assert.That(limit.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOER0000));
            Assert.That(limit.Message, Does.Contain("invocation limit"));
        });
    }

    [Test]
    public void ExtensionFunctionReentrancyIsDeniedUnlessEnabled()
    {
        ReentrantFunction implementation = new ReentrantFunction();
        XPathStaticContext context = Context(Binding(implementation));
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("e:value()", context);
        implementation.Expression = expression;

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()))!;

        Assert.That(error.Message, Does.Contain("Reentrant extension execution"));
    }

    [Test]
    public void ExtensionFunctionReentrancyCanBeEnabledExplicitly()
    {
        BoundedReentrantFunction implementation = new BoundedReentrantFunction();
        XPathStaticContext context = Context(Binding(implementation));
        context.ExtensionPolicy = new XmlExtensionPolicy { Trust = XmlExtensionTrust.Trusted, AllowReentrancy = true, MaximumInvocations = 2 };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("e:value()", context);
        implementation.Expression = expression;

        XdmSequence result = new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues![0].StringValue, Is.EqualTo("nested"));
            Assert.That(implementation.Calls, Is.EqualTo(2));
        });
    }

    [Test]
    public void ExtensionFunctionPollsCancellationBeforeCallingUserCode()
    {
        CountingFunction implementation = new CountingFunction();
        XPathDynamicContext dynamicContext = new XPathDynamicContext { Cancellation = new RequestedCancellation() };

        Assert.Throws<OperationCanceledException>(() => Evaluate("e:value()", Context(Binding(implementation)), dynamicContext));
        Assert.That(implementation.Calls, Is.EqualTo(0));
    }

    [Test]
    public void ExtensionFunctionPollsCancellationAfterCallingUserCode()
    {
        ToggleCancellation cancellation = new ToggleCancellation();
        CancelingFunction implementation = new CancelingFunction(cancellation);
        XPathDynamicContext dynamicContext = new XPathDynamicContext { Cancellation = cancellation };

        Assert.Throws<OperationCanceledException>(() => Evaluate("e:value()", Context(Binding(implementation)), dynamicContext));
        Assert.That(implementation.Calls, Is.EqualTo(1));
    }

    [Test]
    public void ExtensionInvocationLimitsAreIsolatedBetweenEvaluations()
    {
        CountingFunction implementation = new CountingFunction();
        XPathStaticContext context = Context(Binding(implementation));
        context.ExtensionPolicy = new XmlExtensionPolicy { Trust = XmlExtensionTrust.Trusted, MaximumInvocations = 1 };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("e:value()", context);
        XPathDynamicContext dynamicContext = new XPathDynamicContext();

        XdmSequence first = new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
        XdmSequence second = new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(first.AtomicValues![0].StringValue, Is.EqualTo("value"));
            Assert.That(second.AtomicValues![0].StringValue, Is.EqualTo("value"));
            Assert.That(implementation.Calls, Is.EqualTo(2));
        });
    }

    [Test]
    public void MissingXsltInstructionUsesFallbackOrReportsDeterministicError()
    {
        string recovered = Stylesheet("<xsl:template name='entry'><e:missing><xsl:fallback><xsl:sequence select=\"'fallback'\"/></xsl:fallback></e:missing></xsl:template>", "extension-element-prefixes='e'");
        string unavailable = Stylesheet("<xsl:template name='entry'><e:missing/></xsl:template>", "extension-element-prefixes='e'");

        XsltTransformResult result = Transform(recovered, CompileOptions());
        XsltDynamicException error = Assert.Throws<XsltDynamicException>(() => Transform(unavailable, CompileOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.PrincipalValue!.AtomicValues![0].StringValue, Is.EqualTo("fallback"));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1450));
        });
    }

    [Test]
    public void XsltInstructionFailuresAreWrappedAtTheExtensionBoundary()
    {
        XsltCompileOptions options = CompileOptions();
        options.Extensions = [new XsltExtensionBinding
        {
            Name = new XmlName { Prefix = "e", LocalName = "fail", NamespaceUri = "urn:extensions", QualifiedName = "e:fail" },
            IsInstruction = true,
            InstructionImplementation = new ThrowingInstruction()
        }];
        string source = Stylesheet("<xsl:template name='entry'><e:fail/></xsl:template>", "extension-element-prefixes='e'");

        XsltDynamicException error = Assert.Throws<XsltDynamicException>(() => Transform(source, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1450));
            Assert.That(error.Message, Does.Contain("instruction failure"));
        });
    }

    [Test]
    public void XsltAvailabilityUsesExactRegisteredFunctionArity()
    {
        XsltCompileOptions options = CompileOptions();
        options.Functions = [Binding(new ConstantFunction("value"))];
        string source = Stylesheet("<xsl:template name='entry'><xsl:sequence select=\"(function-available('e:value', 0), function-available('e:value', 1))\"/></xsl:template>", "");

        XsltTransformResult result = Transform(source, options);

        Assert.That(result.PrincipalValue!.AtomicValues!.Select(value => value.BooleanValue), Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public void SerializationFailuresAreWrappedByTheExtensionBoundary()
    {
        XmlSerializationOptions options = new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Extension,
            ExtensionMethodName = new XmlName { Prefix = "e", LocalName = "custom", NamespaceUri = "urn:extensions", QualifiedName = "e:custom" },
            ExtensionMethods = [new XmlSerializationMethodBinding
            {
                Name = new XmlName { Prefix = "e", LocalName = "custom", NamespaceUri = "urn:extensions", QualifiedName = "e:custom" },
                Implementation = new ThrowingSerializationMethod()
            }],
            InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error
        };

        XmlSerializationException error = Assert.Throws<XmlSerializationException>(() => new XmlSerializer().SerializeXdmSequence(Atomic("value", XdmAtomicType.String), new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = new StringWriter() }, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SESU0013));
            Assert.That(error.Message, Does.Contain("serialization failure"));
        });
    }

    [Test]
    public void CollationFailuresAreWrappedByTheExtensionBoundary()
    {
        XPathStaticContext context = new XPathStaticContext
        {
            CollationBindings = [new XPathCollationBinding { Uri = "urn:failing-collation", Implementation = new ThrowingCollation() }]
        };

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("compare('a', 'b', 'urn:failing-collation')", context, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOER0000));
            Assert.That(error.Message, Does.Contain("collation failure"));
        });
    }

    [Test]
    public void ResolverWorkLimitIsEnforcedBeforeCallingCollectionResolver()
    {
        CountingResolver resolver = new CountingResolver();
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            CollectionResolver = resolver,
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["urn"],
                MaximumWorkUnits = 2
            }
        };

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("collection('urn:collection')", new XPathStaticContext(), dynamicContext))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(resolver.Calls, Is.EqualTo(0));
        });
    }

    [Test]
    public void DeniedResourcePolicyPreventsCallingCollectionResolver()
    {
        CountingResolver resolver = new CountingResolver();
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            CollectionResolver = resolver,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("collection('urn:collection')", new XPathStaticContext(), dynamicContext))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceDenied));
            Assert.That(resolver.Calls, Is.EqualTo(0));
        });
    }

    [Test]
    public void ExtensionFailureRollsBackPendingResultDocuments()
    {
        XsltCompileOptions options = CompileOptions();
        options.Extensions = [InstructionBinding(new ThrowingInstruction())];
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:result-document href='secondary.xml'><created/></xsl:result-document><e:instruction/></xsl:template>",
            "extension-element-prefixes='e'");
        XsltExecutable executable = Compile(source, options);
        RecordingResultResolver resolver = new RecordingResultResolver();

        Assert.Throws<XsltDynamicException>(() => new XsltTransformer().TransformXslt(executable, new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            ResultDocumentResolver = resolver
        }));

        Assert.Multiple(() =>
        {
            Assert.That(resolver.Allocations, Is.EqualTo(1));
            Assert.That(resolver.Rollbacks, Is.EqualTo(1));
            Assert.That(resolver.Commits, Is.EqualTo(0));
            Assert.That(resolver.Result!.Disposition, Is.EqualTo(XsltResultDisposition.Rollback));
            Assert.That(resolver.Result.Succeeded, Is.False);
        });
    }

    private static XPathFunctionBinding Binding(IXPathFunction implementation)
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

    private static XsltExtensionBinding InstructionBinding(IXsltExtensionInstruction implementation)
    {
        return new XsltExtensionBinding
        {
            Name = new XmlName { Prefix = "e", LocalName = "instruction", NamespaceUri = "urn:extensions", QualifiedName = "e:instruction" },
            BindingKey = "instruction",
            IsInstruction = true,
            InstructionImplementation = implementation
        };
    }

    private static XmlSerializationMethodBinding SerializationBinding(IXmlSerializationMethod implementation)
    {
        return new XmlSerializationMethodBinding
        {
            Name = new XmlName { Prefix = "e", LocalName = "serialization", NamespaceUri = "urn:extensions", QualifiedName = "e:serialization" },
            Implementation = implementation
        };
    }

    private static XPathStaticContext Context(XPathFunctionBinding binding)
    {
        return new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "e", NamespaceUri = "urn:extensions" }],
            Functions = [binding]
        };
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext context, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, context);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XsltTransformResult Transform(string source, XsltCompileOptions options)
    {
        XsltExecutable executable = Compile(source, options);
        return new XsltTransformer().TransformXslt(executable, new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } });
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

    private static XdmSequence Atomic(string value, XdmAtomicType type)
    {
        return new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [new XdmAtomicValue { Type = type, StringValue = value, LexicalValue = value, CanonicalValue = value }]
        };
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
            return Atomic(Value, XdmAtomicType.String);
        }
    }

    private sealed class TypedFunction : IXPathFunction
    {
        internal XdmAtomicType ArgumentType;

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            ArgumentType = arguments[0].AtomicValues![0].Type;
            return Atomic("8", XdmAtomicType.UntypedAtomic);
        }
    }

    private sealed class CountingFunction : IXPathFunction
    {
        internal int Calls;

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            Calls = Calls + 1;
            return Atomic("value", XdmAtomicType.String);
        }
    }

    private sealed class ThrowingFunction : IXPathFunction
    {
        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            throw new InvalidOperationException("callback failure");
        }
    }

    private sealed class ReentrantFunction : IXPathFunction
    {
        internal XPathCompiledExpression? Expression;

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            return new XPathEvaluator().EvaluateXPath(Expression!, context).Value!;
        }
    }

    private sealed class BoundedReentrantFunction : IXPathFunction
    {
        internal int Calls;
        internal XPathCompiledExpression? Expression;

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            Calls = Calls + 1;
            return Calls == 1 ? new XPathEvaluator().EvaluateXPath(Expression!, context).Value! : Atomic("nested", XdmAtomicType.String);
        }
    }

    private sealed class CancelingFunction : IXPathFunction
    {
        internal int Calls;
        internal ToggleCancellation Cancellation;

        internal CancelingFunction(ToggleCancellation cancellation)
        {
            Cancellation = cancellation;
        }

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            Calls = Calls + 1;
            Cancellation.Requested = true;
            return Atomic("canceled", XdmAtomicType.String);
        }
    }

    private sealed class ConstantInstruction : IXsltExtensionInstruction
    {
        internal string Value;

        internal ConstantInstruction(string value)
        {
            Value = value;
        }

        public XdmSequence ExecuteXsltExtensionInstruction(XsltExtensionBinding binding, XdmSequence content, XPathDynamicContext context)
        {
            return Atomic(Value, XdmAtomicType.String);
        }
    }

    private sealed class ThrowingSerializationMethod : IXmlSerializationMethod
    {
        public void SerializeXmlSequence(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options)
        {
            throw new InvalidOperationException("serialization failure");
        }
    }

    private sealed class ThrowingInstruction : IXsltExtensionInstruction
    {
        public XdmSequence ExecuteXsltExtensionInstruction(XsltExtensionBinding binding, XdmSequence content, XPathDynamicContext context)
        {
            throw new InvalidOperationException("instruction failure");
        }
    }

    private sealed class RequestedCancellation : IXmlCancellation
    {
        public bool IsXmlCancellationRequested()
        {
            return true;
        }
    }

    private sealed class ToggleCancellation : IXmlCancellation
    {
        internal bool Requested;

        public bool IsXmlCancellationRequested()
        {
            return Requested;
        }
    }

    private sealed class CountingResolver : IXPathCollectionResolver
    {
        internal int Calls;

        public XmlDocument? ResolveXPathDocument(string absoluteUri, XmlResolverPolicy policy)
        {
            Calls = Calls + 1;
            return null;
        }

        public XdmSequence ResolveXPathCollection(string? absoluteUri, XmlResolverPolicy policy)
        {
            Calls = Calls + 1;
            return Atomic("member", XdmAtomicType.String);
        }

        public string[] ResolveXPathUriCollection(string? absoluteUri, XmlResolverPolicy policy)
        {
            Calls = Calls + 1;
            return [];
        }
    }

    private sealed class ThrowingCollation : IXPathCollation
    {
        public int CompareXPathCollation(string left, string right)
        {
            throw new InvalidOperationException("collation failure");
        }

        public byte[] CreateXPathCollationKey(string value)
        {
            throw new InvalidOperationException("collation failure");
        }
    }

    private sealed class RecordingResultResolver : IXsltResultDocumentResolver
    {
        internal int Allocations;
        internal int Commits;
        internal int Rollbacks;
        internal XsltSecondaryResult? Result;

        public XsltSecondaryResult AllocateXsltResultDocument(string uri, XmlSerializationOptions options)
        {
            Allocations = Allocations + 1;
            Result = new XsltSecondaryResult { Uri = uri, SerializationOptions = options };
            return Result;
        }

        public void CommitXsltResultDocument(XsltSecondaryResult result)
        {
            Commits = Commits + 1;
        }

        public void RejectXsltResultDocument(XsltSecondaryResult result, XmlDiagnostic diagnostic)
        {
        }

        public void RollbackXsltResultDocument(XsltSecondaryResult result)
        {
            Rollbacks = Rollbacks + 1;
        }
    }
}
