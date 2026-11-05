using xml;

namespace xml.Tests;

[TestFixture]
public class XPathRuntimeTests
{
    [Test]
    public void EvaluatesConstantsThroughThePublicRuntimeDispatcher()
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("(1, 'two', true())", new XPathStaticContext());
        XPathDynamicContext input = new XPathDynamicContext
        {
            CurrentDateTime = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)
        };

        XPathEvaluationResult result = new XPathEvaluator().EvaluateXPath(expression, input);
        XdmSequence value = result.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.ErrorCode, Is.Null);
            Assert.That(result.DynamicContext, Is.Not.SameAs(input));
            Assert.That(result.DynamicContext!.CurrentDateTime, Is.EqualTo(input.CurrentDateTime));
            Assert.That(value.IsImmutable, Is.True);
            Assert.That(value.Count, Is.EqualTo(3));
            Assert.That(value.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(value.AtomicValues[0].IntegerValue, Is.EqualTo(1));
            Assert.That(value.AtomicValues[1].StringValue, Is.EqualTo("two"));
            Assert.That(value.AtomicValues[2].BooleanValue, Is.True);
        });
    }

    [Test]
    public void DispatchesContextAndDynamicVariablesInSequenceOrder()
    {
        XPathVariableBinding declaration = Variable("value", null);
        XPathStaticContext staticContext = new XPathStaticContext { Variables = [declaration] };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("($value, .)", staticContext);
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            ContextItem = Sequence(String("context")),
            Variables = [Variable("value", Sequence(Integer("7")))]
        };

        XPathEvaluationResult result = new XPathEvaluator().EvaluateXPath(expression, dynamicContext);
        XdmSequenceCursor cursor = new XdmSequenceCursor(result.Value!);

        Assert.That(cursor.MoveNextXdmSequenceItem(), Is.True);
        Assert.That(cursor.ReadCurrentXdmSequenceItem().AtomicValue!.IntegerValue, Is.EqualTo(7));
        Assert.That(cursor.MoveNextXdmSequenceItem(), Is.True);
        Assert.That(cursor.ReadCurrentXdmSequenceItem().AtomicValue!.StringValue, Is.EqualTo("context"));
        Assert.That(cursor.MoveNextXdmSequenceItem(), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(result.DynamicContext!.Position, Is.EqualTo(1));
            Assert.That(result.DynamicContext.Size, Is.EqualTo(1));
            Assert.That(result.DynamicContext.ContextItem, Is.Not.SameAs(dynamicContext.ContextItem));
            Assert.That(result.DynamicContext.Variables![0], Is.Not.SameAs(dynamicContext.Variables![0]));
        });
    }

    [Test]
    public void ReportsMissingContextAndVariableValuesWithSourceLocations()
    {
        XPathCompiledExpression contextExpression = new XPathCompiler().CompileXPath(".", new XPathStaticContext { BaseUri = "urn:context-source" });
        XPathVariableBinding declaration = Variable("required", null);
        XPathCompiledExpression variableExpression = new XPathCompiler().CompileXPath("$required", new XPathStaticContext
        {
            BaseUri = "urn:variable-source",
            Variables = [declaration]
        });

        XPathDynamicException missingContext = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(contextExpression, new XPathDynamicContext()))!;
        XPathDynamicException missingVariable = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(variableExpression, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(missingContext.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
            Assert.That(missingContext.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:context-source"));
            Assert.That(missingContext.Location, Is.SameAs(missingContext.Diagnostic.Range.Start));
            Assert.That(missingVariable.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0002));
            Assert.That(missingVariable.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:variable-source"));
        });
    }

    [Test]
    public void SnapshotsCurrentTimeTimezoneLimitsPolicyAndEnvironment()
    {
        string[] environmentNames = ["APP_MODE"];
        string[] environmentValues = ["test"];
        XmlSecurityLimits limits = new XmlSecurityLimits
        {
            MaximumDepth = 12,
            MaximumExternalResources = 3,
            CancellationPollingInterval = 4
        };
        XmlResolverPolicy policy = new XmlResolverPolicy { AllowedSchemes = ["urn"] };
        XPathDynamicContext input = new XPathDynamicContext
        {
            ContextItem = Sequence(String("item")),
            ImplicitTimezoneMinutes = 330,
            EnvironmentNames = environmentNames,
            EnvironmentValues = environmentValues,
            SecurityLimits = limits,
            ResolverPolicy = policy,
            MaximumOperations = 25
        };
        DateTimeOffset before = DateTimeOffset.UtcNow;

        XPathRuntimeContext snapshot = new XPathRuntimeContext(input);
        DateTimeOffset after = DateTimeOffset.UtcNow;
        environmentNames[0] = "CHANGED";
        environmentValues[0] = "changed";
        limits.MaximumDepth = 99;
        policy.AllowedSchemes![0] = "file";
        input.CurrentDateTime = after.AddDays(1);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Snapshot.CurrentDateTime.UtcDateTime, Is.InRange(before.UtcDateTime, after.UtcDateTime));
            Assert.That(snapshot.Snapshot.CurrentDateTime.Offset, Is.EqualTo(TimeSpan.FromMinutes(330)));
            Assert.That(snapshot.Snapshot.EnvironmentNames, Is.EqualTo(new[] { "APP_MODE" }));
            Assert.That(snapshot.Snapshot.EnvironmentValues, Is.EqualTo(new[] { "test" }));
            Assert.That(snapshot.Limits.MaximumDepth, Is.EqualTo(12));
            Assert.That(snapshot.ResourcePolicy.AllowedSchemes, Is.EqualTo(new[] { "urn" }));
            Assert.That(snapshot.MaximumOperations, Is.EqualTo(25));
            Assert.That(snapshot.MaximumResources, Is.EqualTo(3));
            Assert.That(snapshot.CancellationPollingInterval, Is.EqualTo(4));
        });
    }

    [Test]
    public void FocusVariableAndFunctionFramesRestoreTheirParents()
    {
        XdmSequence first = Sequence(String("first"));
        XdmSequence second = Sequence(String("second"));
        XPathFocusStack focus = new XPathFocusStack(first, 1, 1);
        XPathVariableStack variables = new XPathVariableStack([Variable("name", first)]);
        XPathFunctionCallStack calls = new XPathFunctionCallStack(1);

        focus.PushXPathFocus(second, 2, 3);
        variables.PushXPathVariableFrame();
        variables.SetXPathVariable(Variable("name", second));
        calls.PushXPathFunctionCall(Name("call"), [first], Range("urn:call"));

        Assert.Multiple(() =>
        {
            Assert.That(focus.ReadXPathFocus().Position, Is.EqualTo(2));
            Assert.That(focus.ReadXPathFocus().Size, Is.EqualTo(3));
            Assert.That(variables.FindXPathVariable(Name("name"))!.Value!.AtomicValues![0].StringValue, Is.EqualTo("second"));
            Assert.That(calls.ReadXPathFunctionCall().Name!.LocalName, Is.EqualTo("call"));
            Assert.That(calls.ReadXPathFunctionCall().Arguments[0], Is.Not.SameAs(first));
            Assert.That(Assert.Throws<XmlLimitException>(() => calls.PushXPathFunctionCall(Name("nested"), [], Range("urn:nested")))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        });

        calls.PopXPathFunctionCall();
        variables.PopXPathVariableFrame();
        focus.PopXPathFocus();
        Assert.Multiple(() =>
        {
            Assert.That(focus.ReadXPathFocus().ContextItem!.AtomicValues![0].StringValue, Is.EqualTo("first"));
            Assert.That(variables.FindXPathVariable(Name("name"))!.Value!.AtomicValues![0].StringValue, Is.EqualTo("first"));
            Assert.Throws<InvalidOperationException>(() => calls.ReadXPathFunctionCall());
        });
    }

    [Test]
    public void EnforcesImmediateCancellationAndOperationLimits()
    {
        XPathCompiledExpression constant = new XPathCompiler().CompileXPath("1", new XPathStaticContext());
        XPathDynamicContext canceledContext = new XPathDynamicContext { Cancellation = new AlwaysCanceled() };
        Assert.Throws<OperationCanceledException>(() => new XPathEvaluator().EvaluateXPath(constant, canceledContext));

        XPathVariableBinding declaration = Variable("value", null);
        XPathCompiledExpression repeated = new XPathCompiler().CompileXPath("($value, $value)", new XPathStaticContext { Variables = [declaration] });
        XPathDynamicContext limitedContext = new XPathDynamicContext
        {
            Variables = [Variable("value", Sequence(Integer("1")))],
            MaximumOperations = 2
        };

        XmlLimitException limit = Assert.Throws<XmlLimitException>(() => new XPathEvaluator().EvaluateXPath(repeated, limitedContext))!;
        Assert.Multiple(() =>
        {
            Assert.That(limit.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(limit.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Limit));
            Assert.That(limit.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void PollsCancellationAtConfiguredInstructionBoundaries()
    {
        CountingCancellation cancellation = new CountingCancellation(2);
        XPathRuntimeLimiter limiter = new XPathRuntimeLimiter(10, 10, 2, cancellation);
        SourceRange range = Range("urn:poll");

        limiter.EnterXPathInstruction(range);
        limiter.ExitXPathInstruction();
        limiter.EnterXPathInstruction(range);
        limiter.ExitXPathInstruction();
        limiter.EnterXPathInstruction(range);
        limiter.ExitXPathInstruction();

        Assert.Throws<OperationCanceledException>(() => limiter.EnterXPathInstruction(range));
        Assert.That(cancellation.Polls, Is.EqualTo(2));
    }

    [Test]
    public void EnforcesNestedInstructionDepthIndependentlyOfOperationCount()
    {
        XPathRuntimeLimiter limiter = new XPathRuntimeLimiter(10, 1, 10, null);
        SourceRange range = Range("urn:depth");

        limiter.EnterXPathInstruction(range);
        XmlLimitException exception = Assert.Throws<XmlLimitException>(() => limiter.EnterXPathInstruction(range))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(exception.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("urn:depth"));
            Assert.That(limiter.Operations, Is.EqualTo(1));
        });
    }

    [Test]
    public void ResourceRegistryCachesStableResultsAndEnforcesItsLimit()
    {
        RecordingCollectionResolver resolver = new RecordingCollectionResolver();
        XPathResourceRegistry registry = new XPathResourceRegistry(resolver, new XmlResolverPolicy(), 3, false);

        XmlDocument firstDocument = registry.ResolveXPathDocumentResource("urn:document");
        XmlDocument secondDocument = registry.ResolveXPathDocumentResource("urn:document");
        XdmSequence firstCollection = registry.ResolveXPathCollectionResource("urn:collection");
        XdmSequence secondCollection = registry.ResolveXPathCollectionResource("urn:collection");
        string[] firstUris = registry.ResolveXPathUriCollectionResource(null);
        firstUris[0] = "urn:changed";
        string[] secondUris = registry.ResolveXPathUriCollectionResource(null);

        Assert.Multiple(() =>
        {
            Assert.That(secondDocument, Is.SameAs(firstDocument));
            Assert.That(secondCollection, Is.SameAs(firstCollection));
            Assert.That(secondCollection.IsImmutable, Is.True);
            Assert.That(secondUris, Is.EqualTo(new[] { "urn:member" }));
            Assert.That(secondUris, Is.Not.SameAs(firstUris));
            Assert.That(resolver.DocumentCalls, Is.EqualTo(1));
            Assert.That(resolver.CollectionCalls, Is.EqualTo(1));
            Assert.That(resolver.UriCollectionCalls, Is.EqualTo(1));
            Assert.That(registry.ResourceCount, Is.EqualTo(3));
            Assert.That(Assert.Throws<XmlLimitException>(() => registry.RegisterXPathDocumentResource("urn:fourth", new XmlDocument()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        });
    }

    [TestCase(-841)]
    [TestCase(841)]
    public void RejectsInvalidImplicitTimezones(int minutes)
    {
        XPathDynamicContext context = new XPathDynamicContext { ImplicitTimezoneMinutes = minutes };

        Assert.Throws<ArgumentOutOfRangeException>(() => new XPathRuntimeContext(context));
    }

    private static XPathVariableBinding Variable(string localName, XdmSequence? value)
    {
        return new XPathVariableBinding
        {
            Name = Name(localName),
            Value = value,
            DeclaredType = XdmAtomicType.Integer,
            DeclaredOccurrence = XdmOccurrence.ExactlyOne
        };
    }

    private static XmlName Name(string localName)
    {
        return new XmlName { NamespaceUri = "", LocalName = localName, QualifiedName = localName };
    }

    private static XdmAtomicValue Integer(string lexical)
    {
        return new XdmFactory().CreateXdmIntegerAtomicValue(lexical, XdmAtomicType.Integer);
    }

    private static XdmAtomicValue String(string value)
    {
        return new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String);
    }

    private static XdmSequence Sequence(XdmAtomicValue value)
    {
        return new XdmSequenceOperations().CreateXdmAtomicSequence(value);
    }

    private static SourceRange Range(string sourceUri)
    {
        SourceLocation start = new SourceLocation { SourceUri = sourceUri, Line = 1, Column = 1 };
        SourceLocation end = new SourceLocation { SourceUri = sourceUri, CharacterOffset = 1, Line = 1, Column = 2 };
        return new SourceRange { Start = start, End = end };
    }

    private sealed class AlwaysCanceled : IXmlCancellation
    {
        public bool IsXmlCancellationRequested()
        {
            return true;
        }
    }

    private sealed class CountingCancellation : IXmlCancellation
    {
        internal int CancelAt;
        internal int Polls;

        internal CountingCancellation(int cancelAt)
        {
            CancelAt = cancelAt;
            Polls = 0;
        }

        public bool IsXmlCancellationRequested()
        {
            Polls = Polls + 1;
            return Polls >= CancelAt;
        }
    }

    private sealed class RecordingCollectionResolver : IXPathCollectionResolver
    {
        internal int DocumentCalls;
        internal int CollectionCalls;
        internal int UriCollectionCalls;

        public XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy)
        {
            DocumentCalls = DocumentCalls + 1;
            return new XmlDocument();
        }

        public XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy)
        {
            CollectionCalls = CollectionCalls + 1;
            return Sequence(String("collection"));
        }

        public string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy)
        {
            UriCollectionCalls = UriCollectionCalls + 1;
            return ["urn:member"];
        }
    }
}
