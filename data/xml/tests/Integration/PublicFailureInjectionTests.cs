using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class PublicFailureInjectionTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void InputReadFailuresAreTypedAndReleaseOwnedResourcesForEveryParserShape()
    {
        ThrowingReadStream documentStream = new ThrowingReadStream();
        ThrowingTextReader fragmentReader = new ThrowingTextReader();
        ThrowingReadStream pushStream = new ThrowingReadStream();
        ThrowingTextReader pullReader = new ThrowingTextReader();

        XmlResourceException documentFailure = Assert.Throws<XmlResourceException>(() => new XmlParser().ParseXmlDocument(StreamInput(documentStream), ParserOptions(), null, null, null))!;
        XmlResourceException fragmentFailure = Assert.Throws<XmlResourceException>(() => new XmlParser().ParseXmlFragment(ReaderInput(fragmentReader), ParserOptions(), null, null, null))!;
        XmlPushParser push = new XmlPushParser();
        push.BeginXmlPush(StreamInput(pushStream), ParserOptions(), null, null, null, null);
        XmlResourceException pushFailure = Assert.Throws<XmlResourceException>(() => push.FinishXmlPush())!;
        XmlPullReader pull = new XmlPullReader();
        pull.BeginXmlPull(ReaderInput(pullReader), ParserOptions(), null, null, null);
        XmlResourceException pullFailure = Assert.Throws<XmlResourceException>(() => pull.AdvanceXmlPull())!;

        Assert.Multiple(() =>
        {
            Assert.That(documentFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.GeneralFailure));
            Assert.That(fragmentFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.GeneralFailure));
            Assert.That(pushFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.GeneralFailure));
            Assert.That(pullFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.GeneralFailure));
            Assert.That(push.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Error));
            Assert.That(pull.InspectXmlPull().Status, Is.EqualTo(XmlParserStatus.Error));
        });

        push.ResetXmlPush();
        pull.CloseXmlPull();
        Assert.Multiple(() =>
        {
            Assert.That(documentStream.DisposeCalls, Is.EqualTo(1));
            Assert.That(fragmentReader.DisposeCalls, Is.EqualTo(1));
            Assert.That(pushStream.DisposeCalls, Is.EqualTo(1));
            Assert.That(pullReader.DisposeCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void DecodeFailuresAreTypedAndLeaveIncrementalParsersRecoverable()
    {
        byte[] malformed = [0x3C, 0x72, 0x3E, 0xC0, 0x80, 0x3C, 0x2F, 0x72, 0x3E];
        XmlWellFormednessException documentFailure = Assert.Throws<XmlWellFormednessException>(() => new XmlParser().ParseXmlDocument(ByteInput(malformed), ParserOptions(), null, null, null))!;
        XmlPushParser push = new XmlPushParser();
        push.BeginXmlPush(ByteInput([]), ParserOptions(), null, null, null, null);
        XmlWellFormednessException pushFailure = Assert.Throws<XmlWellFormednessException>(() => push.FeedXmlPushBytes(malformed, true))!;
        XmlPullReader pull = new XmlPullReader();
        pull.BeginXmlPull(ByteInput(malformed), ParserOptions(), null, null, null);
        XmlWellFormednessException pullFailure = Assert.Throws<XmlWellFormednessException>(() => pull.AdvanceXmlPull())!;

        push.ResetXmlPush();
        push.BeginXmlPush(ByteInput(Encoding.UTF8.GetBytes("<ok/>")), ParserOptions(), null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(documentFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.MalformedMarkup));
            Assert.That(pushFailure.Diagnostic!.Code, Is.EqualTo(documentFailure.Diagnostic.Code));
            Assert.That(pullFailure.Diagnostic!.Code, Is.EqualTo(documentFailure.Diagnostic.Code));
            Assert.That(push.FinishXmlPush().Status, Is.EqualTo(XmlParserStatus.Completed));
            Assert.That(pull.InspectXmlPull().Status, Is.EqualTo(XmlParserStatus.Error));
        });
    }

    [Test]
    public void HandlerFailuresAreTypedAndDoNotLeavePendingEvents()
    {
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(StringInput("<root/>"), ParserOptions(), new ThrowingEventHandler(), null, null, null);

        XmlException failure = Assert.Throws<XmlException>(() => parser.FinishXmlPush())!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.GeneralFailure));
            Assert.That(failure.Message, Does.Contain("handler failure"));
            Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Error));
        });

        parser.ResetXmlPush();
        parser.BeginXmlPush(StringInput("<recovered/>"), ParserOptions(), null, null, null, null);
        Assert.That(parser.FinishXmlPush().Status, Is.EqualTo(XmlParserStatus.Completed));
    }

    [Test]
    public void ResolutionFailuresAreTypedAndLeaveSourceTreesUsable()
    {
        ThrowingResourceResolver dtdResolver = new ThrowingResourceResolver();
        XmlParserOptions dtdOptions = ParserOptions();
        dtdOptions.DtdProcessing = XmlDtdProcessing.Parse;
        dtdOptions.ResolverPolicy = AllowedPolicy();
        XmlResourceException dtdFailure = Assert.Throws<XmlResourceException>(() => new XmlParser().ParseXmlDocument(
            StringInput("<!DOCTYPE root SYSTEM 'missing.dtd'><root/>"), dtdOptions, dtdResolver, null, null))!;

        string includeNamespace = "http://www.w3.org/2001/XInclude";
        XmlDocument includeDocument = Parse("<root xmlns:xi='" + includeNamespace + "'><xi:include href='missing.xml'/></root>");
        XmlResourceException includeFailure = Assert.Throws<XmlResourceException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(
            includeDocument,
            new XmlIncludeOptions { ResolverPolicy = AllowedPolicy(), MaximumDepth = 8, MaximumResourceBytes = 4096 },
            new ThrowingResourceResolver()))!;

        ThrowingCollectionResolver xpathResolver = new ThrowingCollectionResolver();
        XPathCompiledExpression xpathExpression = new XPathCompiler().CompileXPath("doc('missing.xml')", new XPathStaticContext
        {
            BaseUri = "https://example.test/query.xq",
            ResolverPolicy = AllowedPolicy()
        });
        XPathDynamicException xpathFailure = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(xpathExpression, new XPathDynamicContext
        {
            CollectionResolver = xpathResolver,
            ResolverPolicy = AllowedPolicy()
        }))!;

        Assert.Multiple(() =>
        {
            Assert.That(dtdFailure.Diagnostic!.Category, Is.EqualTo(XmlErrorCategory.Resource));
            Assert.That(includeFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.IncludeFallbackMissing));
            Assert.That(includeFailure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Include));
            Assert.That(includeDocument.DocumentElement!.Children![0].Name!.LocalName, Is.EqualTo("include"));
            Assert.That(xpathFailure.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0002));
            Assert.That(xpathResolver.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExtensionFailuresAreWrappedAtEveryPublicExtensionBoundary()
    {
        XPathFunctionBinding xpathBinding = FunctionBinding(new ThrowingXPathFunction());
        XPathStaticContext xpathContext = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "e", NamespaceUri = "urn:extension" }],
            Functions = [xpathBinding]
        };
        XPathCompiledExpression xpathExpression = new XPathCompiler().CompileXPath("e:fail()", xpathContext);
        XPathDynamicException xpathFailure = Assert.Throws<XPathDynamicException>(() => new XPathEvaluator().EvaluateXPath(xpathExpression, new XPathDynamicContext()))!;

        XsltCompileOptions xsltOptions = CompileOptions();
        xsltOptions.Functions = [FunctionBinding(new ThrowingXPathFunction())];
        XsltExecutable executable = CompileXslt("<xsl:template name='entry'><xsl:sequence select='e:fail()'/></xsl:template>", xsltOptions);
        XPathDynamicException xsltFailure = Assert.Throws<XPathDynamicException>(() => Transform(executable, null, null))!;

        XmlSerializationOptions serialization = new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Extension,
            ExtensionMethodName = Name("serialize", "urn:extension", "e"),
            ExtensionMethods =
            [
                new XmlSerializationMethodBinding
                {
                    Name = Name("serialize", "urn:extension", "e"),
                    Implementation = new ThrowingSerializationMethod()
                }
            ],
            InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error
        };
        XmlSerializationException serializationFailure = Assert.Throws<XmlSerializationException>(() => new XmlSerializer().SerializeXdmSequence(
            AtomicString("value"),
            new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = new StringWriter() },
            serialization))!;

        Assert.Multiple(() =>
        {
            Assert.That(xpathFailure.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOER0000));
            Assert.That(xsltFailure.Diagnostic!.Code, Is.EqualTo(xpathFailure.Diagnostic.Code));
            Assert.That(serializationFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SESU0013));
            Assert.That(xpathFailure.Message, Does.Contain("extension failure"));
            Assert.That(serializationFailure.Message, Does.Contain("serialization failure"));
        });
    }

    [Test]
    public void TreeMutationFailuresAreAtomic()
    {
        XmlDocument document = Parse("<root><parent><child/></parent><sibling/></root>");
        XmlNode root = document.DocumentElement!;
        XmlNode parent = root.Children![0];
        XmlNode child = parent.Children![0];
        XmlNode sibling = root.Children[1];
        long version = document.MutationVersion;
        XmlTreeEditor editor = new XmlTreeEditor();

        Assert.Throws<ArgumentException>(() => editor.AppendXmlTreeChild(child, root));
        Assert.Throws<ArgumentException>(() => editor.ReplaceXmlTreeNode(parent, root, child));

        Assert.Multiple(() =>
        {
            Assert.That(document.MutationVersion, Is.EqualTo(version));
            Assert.That(root.Parent, Is.SameAs(document.DocumentNode));
            Assert.That(root.Children, Is.EqualTo(new[] { parent, sibling }));
            Assert.That(parent.Children, Is.EqualTo(new[] { child }));
            Assert.That(child.Parent, Is.SameAs(parent));
            Assert.That(sibling.Parent, Is.SameAs(root));
        });
    }

    [Test]
    public void OutputWriteAndFlushFailuresAreTypedAndCleanedUpForEveryOutputOperation()
    {
        XmlDocument document = Parse("<root>value</root>");
        XmlEventBatch events = new XmlEventEmitter().EmitXmlNodeEvents(document.DocumentNode!, ParserOptions());
        XdmSequence sequence = new XdmSequenceOperations().CreateXdmNodeSequence(document.DocumentNode!);
        XmlSerializationOptions serialization = new XmlSerializationOptions { Method = XmlSerializationMethod.Xml, InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error };
        XmlCanonicalizationOptions canonicalization = new XmlCanonicalizationOptions { Method = XmlCanonicalizationMethod.CanonicalXml11 };

        AssertOutputFailure(output => new XmlSerializer().SerializeXmlDocument(document, output, serialization), false);
        AssertOutputFailure(output => new XmlSerializer().SerializeXmlNode(document.DocumentElement!, output, serialization), true);
        AssertOutputFailure(output => new XmlSerializer().SerializeXdmSequence(sequence, output, serialization), false);
        AssertOutputFailure(output => new XmlSerializer().SerializeXmlEvents(events, output, serialization), true);
        AssertOutputFailure(output => new XmlCanonicalizer().CanonicalizeXml(document, output, canonicalization), false);
        AssertOutputFailure(output => new XmlCanonicalizer().CanonicalizeXmlEvents(events, output, canonicalization), true);
    }

    [Test]
    public void ResultCommitFailuresRollbackPendingResultsWithTypedDiagnostics()
    {
        string body = "<xsl:template name='entry'><xsl:result-document href='secondary.xml'><secondary/></xsl:result-document><principal/></xsl:template>";
        XsltExecutable executable = CompileXslt(body, CompileOptions());
        FailingCommitResolver resolver = new FailingCommitResolver();

        XsltDynamicException failure = Assert.Throws<XsltDynamicException>(() => Transform(executable, resolver, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1490));
            Assert.That(failure.Message, Does.Contain("commit failure"));
            Assert.That(resolver.Allocations, Is.EqualTo(1));
            Assert.That(resolver.Commits, Is.EqualTo(1));
            Assert.That(resolver.Rollbacks, Is.EqualTo(1));
            Assert.That(resolver.Result!.Disposition, Is.EqualTo(XsltResultDisposition.Rollback));
            Assert.That(resolver.Result.Succeeded, Is.False);
        });
    }

    [Test]
    public void CancellationStopsParserXPathAndXsltBeforeObservableWork()
    {
        RequestedCancellation cancellation = new RequestedCancellation();
        TrackingReadStream parserStream = new TrackingReadStream(Encoding.UTF8.GetBytes("<root/>"));
        Assert.Throws<OperationCanceledException>(() => new XmlParser().ParseXmlDocument(StreamInput(parserStream), ParserOptions(), null, null, cancellation));

        CountingXPathFunction function = new CountingXPathFunction();
        XPathCompiledExpression xpath = new XPathCompiler().CompileXPath("e:count()", new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "e", NamespaceUri = "urn:extension" }],
            Functions = [FunctionBinding(function)]
        });
        Assert.Throws<OperationCanceledException>(() => new XPathEvaluator().EvaluateXPath(xpath, new XPathDynamicContext { Cancellation = cancellation }));

        CountingXPathFunction xsltFunction = new CountingXPathFunction();
        XsltCompileOptions xsltOptions = CompileOptions();
        xsltOptions.Functions = [FunctionBinding(xsltFunction)];
        XsltExecutable executable = CompileXslt("<xsl:template name='entry'><xsl:sequence select='e:count()'/></xsl:template>", xsltOptions);
        Assert.Throws<OperationCanceledException>(() => new XsltTransformer().TransformXslt(executable, new XsltTransformOptions
        {
            InitialTemplate = Name("entry", "", null),
            Cancellation = cancellation
        }));

        Assert.Multiple(() =>
        {
            Assert.That(parserStream.ReadCalls, Is.Zero);
            Assert.That(parserStream.DisposeCalls, Is.EqualTo(1));
            Assert.That(function.Calls, Is.Zero);
            Assert.That(xsltFunction.Calls, Is.Zero);
        });
    }

    private static void AssertOutputFailure(Action<XmlOutput> operation, bool failOnFlush)
    {
        FailingOutputStream stream = new FailingOutputStream(failOnFlush);
        XmlSerializationException failure = Assert.Throws<XmlSerializationException>(() => operation(new XmlOutput
        {
            Kind = XmlOutputKind.Stream,
            ByteStream = stream,
            OwnsResource = true
        }))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.Diagnostic!.Category, Is.EqualTo(XmlErrorCategory.Serialization));
            Assert.That(stream.DisposeCalls, Is.EqualTo(1));
            Assert.That(stream.WriteCalls, Is.GreaterThanOrEqualTo(1));
            Assert.That(stream.FlushCalls, Is.EqualTo(failOnFlush ? 1 : 0));
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
            PreservePrefixes = true,
            RetainNamespaceDeclarations = true
        };
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        return new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https"],
            MaximumDepth = 8,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 1048576
        };
    }

    private static XmlInput StringInput(string value)
    {
        return new XmlInput { Kind = XmlInputKind.String, Text = value, BaseUri = "https://example.test/input.xml" };
    }

    private static XmlInput ByteInput(byte[] value)
    {
        return new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = value, BaseUri = "https://example.test/input.xml" };
    }

    private static XmlInput StreamInput(Stream stream)
    {
        return new XmlInput { Kind = XmlInputKind.Stream, ByteStream = stream, OwnsResource = true, BaseUri = "https://example.test/input.xml" };
    }

    private static XmlInput ReaderInput(TextReader reader)
    {
        return new XmlInput { Kind = XmlInputKind.TextReader, TextReader = reader, OwnsResource = true, BaseUri = "https://example.test/input.xml" };
    }

    private static XmlDocument Parse(string source)
    {
        return new XmlParser().ParseXmlDocument(StringInput(source), ParserOptions(), null, null, null);
    }

    private static XPathFunctionBinding FunctionBinding(IXPathFunction implementation)
    {
        return new XPathFunctionBinding
        {
            Name = Name(implementation is ThrowingXPathFunction ? "fail" : "count", "urn:extension", "e"),
            Arity = 0,
            ResultType = XdmAtomicType.String,
            ResultOccurrence = XdmOccurrence.ExactlyOne,
            BindingKey = "failure-injection",
            Implementation = implementation,
            IsDeterministic = true
        };
    }

    private static XdmSequence AtomicString(string value)
    {
        return new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String));
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

    private static XsltCompileOptions CompileOptions()
    {
        return new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = AllowedPolicy()
        };
    }

    private static XsltExecutable CompileXslt(string body, XsltCompileOptions options)
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:e='urn:extension' version='3.0'>" + body + "</xsl:stylesheet>";
        return new XsltCompiler().CompileXslt(new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = options.BaseUri }, options, new XmlDefaultResolver());
    }

    private static XdmSequence Transform(XsltExecutable executable, IXsltResultDocumentResolver? results, IXmlCancellation? cancellation)
    {
        XsltTransformResult transformed = new XsltTransformer().TransformXslt(executable, new XsltTransformOptions
        {
            InitialTemplate = Name("entry", "", null),
            ResultDocumentResolver = results,
            Cancellation = cancellation,
            BaseOutputUri = "https://example.test/output/main.xml"
        });
        return transformed.PrincipalValue!;
    }

    private sealed class ThrowingReadStream : MemoryStream
    {
        internal int DisposeCalls;

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException("stream read failure");
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCalls = DisposeCalls + 1;
            base.Dispose(disposing);
        }
    }

    private sealed class ThrowingTextReader : TextReader
    {
        internal int DisposeCalls;

        public override int Read(char[] buffer, int index, int count)
        {
            throw new IOException("reader failure");
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCalls = DisposeCalls + 1;
            base.Dispose(disposing);
        }
    }

    private sealed class TrackingReadStream : MemoryStream
    {
        internal int ReadCalls;
        internal int DisposeCalls;

        internal TrackingReadStream(byte[] source) : base(source)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls = ReadCalls + 1;
            return base.Read(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCalls = DisposeCalls + 1;
            base.Dispose(disposing);
        }
    }

    private sealed class ThrowingEventHandler : IXmlEventHandler
    {
        public XmlHandlerAction HandleXmlEvent(XmlEvent value)
        {
            throw new InvalidOperationException("handler failure");
        }
    }

    private sealed class ThrowingResourceResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            throw new IOException("resolution failure");
        }
    }

    private sealed class ThrowingCollectionResolver : IXPathCollectionResolver
    {
        internal int Calls;

        public XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy)
        {
            Calls = Calls + 1;
            throw new IOException("document resolution failure");
        }

        public XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy)
        {
            throw new IOException("collection resolution failure");
        }

        public string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy)
        {
            throw new IOException("URI collection resolution failure");
        }
    }

    private sealed class ThrowingXPathFunction : IXPathFunction
    {
        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            throw new InvalidOperationException("extension failure");
        }
    }

    private sealed class CountingXPathFunction : IXPathFunction
    {
        internal int Calls;

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            Calls = Calls + 1;
            return AtomicString("called");
        }
    }

    private sealed class ThrowingSerializationMethod : IXmlSerializationMethod
    {
        public void SerializeXmlSequence(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options)
        {
            throw new IOException("serialization failure");
        }
    }

    private sealed class FailingOutputStream : MemoryStream
    {
        internal bool FailOnFlush;
        internal int WriteCalls;
        internal int FlushCalls;
        internal int DisposeCalls;

        internal FailingOutputStream(bool failOnFlush)
        {
            FailOnFlush = failOnFlush;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteCalls = WriteCalls + 1;
            if (!FailOnFlush)
            {
                throw new IOException("write failure");
            }

            base.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            FlushCalls = FlushCalls + 1;
            throw new IOException("flush failure");
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCalls = DisposeCalls + 1;
            base.Dispose(disposing);
        }
    }

    private sealed class FailingCommitResolver : IXsltResultDocumentResolver
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
            throw new IOException("commit failure");
        }

        public void RejectXsltResultDocument(XsltSecondaryResult result, XmlDiagnostic diagnostic)
        {
        }

        public void RollbackXsltResultDocument(XsltSecondaryResult result)
        {
            Rollbacks = Rollbacks + 1;
        }
    }

    private sealed class RequestedCancellation : IXmlCancellation
    {
        public bool IsXmlCancellationRequested()
        {
            return true;
        }
    }
}
