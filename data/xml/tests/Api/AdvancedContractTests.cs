using xml;

namespace xml.Tests;

[TestFixture]
public class AdvancedContractTests
{
    [Test]
    public void SchemaAndXPathHooksCarryRequestsAndResults()
    {
        XPathContractStub stub = new XPathContractStub();
        XmlSchemaSet schemaSet = new XmlSchemaSet();
        XmlSchemaValidationOptions schemaOptions = new XmlSchemaValidationOptions();
        XmlEventBatch events = new XmlEventBatch();
        XmlSchemaValidationResult validation = stub.ValidateXmlSchemaEvents(events, schemaSet, schemaOptions);
        XmlName name = new XmlName();
        name.LocalName = "function";
        XPathStaticContext staticContext = new XPathStaticContext();
        IXPathFunction? function = stub.ResolveXPathFunction(name, 1, staticContext);
        XPathFunctionBinding binding = new XPathFunctionBinding();
        binding.Name = name;
        XdmSequence argument = new XdmSequence();
        XPathDynamicContext dynamicContext = new XPathDynamicContext();
        XdmSequence invoked = function!.InvokeXPathFunction(binding, [argument], dynamicContext);
        XPathVariableBinding variable = stub.ResolveXPathVariable(name, dynamicContext)!;
        XmlResolverPolicy policy = new XmlResolverPolicy();
        XmlDocument document = stub.ResolveXPathDocument("urn:document", policy)!;
        XdmSequence collection = stub.ResolveXPathCollection("urn:collection", policy);
        string[] uris = stub.ResolveXPathUriCollection("urn:collection", policy);
        SourceRange range = new SourceRange();
        stub.ReceiveXPathTrace("label", argument, range);

        Assert.That(validation.SchemaSet, Is.SameAs(schemaSet));
        Assert.That(validation.Options, Is.SameAs(schemaOptions));
        Assert.That(validation.Succeeded, Is.True);
        Assert.That(invoked, Is.SameAs(argument));
        Assert.That(stub.FunctionBinding, Is.SameAs(binding));
        Assert.That(stub.FunctionContext, Is.SameAs(dynamicContext));
        Assert.That(variable.Name, Is.SameAs(name));
        Assert.That(document.DocumentUri, Is.EqualTo("urn:document"));
        Assert.That(collection, Is.SameAs(stub.Collection));
        Assert.That(uris, Is.EqualTo(new string[] { "urn:item" }));
        Assert.That(stub.CompareXPathCollation("a", "b"), Is.LessThan(0));
        Assert.That(stub.CreateXPathCollationKey("abc"), Is.EqualTo(new byte[] { 97, 98, 99 }));
        Assert.That(stub.TraceLabel, Is.EqualTo("label"));
        Assert.That(stub.TraceValue, Is.SameAs(argument));
        Assert.That(stub.TraceRange, Is.SameAs(range));
    }

    [Test]
    public void XsltHooksCarryPackageExtensionAndTransactionalResultState()
    {
        XsltContractStub stub = new XsltContractStub();
        XmlName packageName = new XmlName();
        packageName.LocalName = "package";
        XsltPackageReference package = new XsltPackageReference();
        package.Name = packageName;
        package.Version = "1.0";
        XsltCompileOptions compileOptions = new XsltCompileOptions();
        XsltExecutable executable = stub.ResolveXsltPackage(package, compileOptions);
        XmlResolvedResource module = stub.ResolveXsltModule("module.xsl", "file:///base/", new XmlResolverPolicy());
        XsltExtensionBinding binding = new XsltExtensionBinding();
        XdmSequence content = new XdmSequence();
        XdmSequence extensionResult = stub.ExecuteXsltExtensionInstruction(binding, content, new XPathDynamicContext());
        XsltMessage message = new XsltMessage();
        message.Text = "message";
        stub.ReceiveXsltMessage(message);
        XsltTraceEvent trace = new XsltTraceEvent();
        stub.ReceiveXsltTrace(trace);
        XmlSerializationOptions serialization = new XmlSerializationOptions();
        XsltSecondaryResult secondary = stub.AllocateXsltResultDocument("file:///result.xml", serialization);
        stub.CommitXsltResultDocument(secondary);
        Assert.That(secondary.Disposition, Is.EqualTo(XsltResultDisposition.Commit));
        Assert.That(secondary.Succeeded, Is.True);
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        stub.RejectXsltResultDocument(secondary, diagnostic);
        Assert.That(secondary.Disposition, Is.EqualTo(XsltResultDisposition.Reject));
        Assert.That(stub.Diagnostic, Is.SameAs(diagnostic));
        stub.RollbackXsltResultDocument(secondary);
        XsltStreamabilityReport report = stub.AnalyzeXsltStreamability(executable);

        Assert.That(executable.PackageName, Is.SameAs(packageName));
        Assert.That(executable.PackageVersion, Is.EqualTo("1.0"));
        Assert.That(executable.CompileOptions, Is.SameAs(compileOptions));
        Assert.That(module.RequestedUri, Is.EqualTo("module.xsl"));
        Assert.That(module.AbsoluteUri, Is.EqualTo("file:///base/module.xsl"));
        Assert.That(extensionResult, Is.SameAs(content));
        Assert.That(stub.ExtensionBinding, Is.SameAs(binding));
        Assert.That(stub.Message, Is.SameAs(message));
        Assert.That(stub.Trace, Is.SameAs(trace));
        Assert.That(secondary.Uri, Is.EqualTo("file:///result.xml"));
        Assert.That(secondary.SerializationOptions, Is.SameAs(serialization));
        Assert.That(secondary.Disposition, Is.EqualTo(XsltResultDisposition.Rollback));
        Assert.That(secondary.Succeeded, Is.False);
        Assert.That(report, Is.SameAs(executable.StreamabilityReport));
        Assert.That(report.Streamability, Is.EqualTo(XsltStreamability.Streamable));
    }

    private sealed class XPathContractStub : IXmlSchemaValidator, IXPathFunction, IXPathVariableResolver, IXPathFunctionResolver, IXPathCollectionResolver, IXPathCollation, IXPathTraceReceiver
    {
        internal XPathFunctionBinding? FunctionBinding;
        internal XPathDynamicContext? FunctionContext;
        internal XdmSequence Collection = new XdmSequence();
        internal string? TraceLabel;
        internal XdmSequence? TraceValue;
        internal SourceRange? TraceRange;

        public XmlSchemaValidationResult ValidateXmlSchema(XmlDocument document, XmlSchemaSet schemaSet, XmlSchemaValidationOptions options)
        {
            XmlSchemaValidationResult result = CreateValidationResult(schemaSet, options);
            result.Document = document;
            return result;
        }

        public XmlSchemaValidationResult ValidateXmlSchemaEvents(XmlEventBatch events, XmlSchemaSet schemaSet, XmlSchemaValidationOptions options)
        {
            return CreateValidationResult(schemaSet, options);
        }

        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            FunctionBinding = binding;
            FunctionContext = context;
            return arguments[0];
        }

        public XPathVariableBinding? ResolveXPathVariable(XmlName name, XPathDynamicContext context)
        {
            XPathVariableBinding result = new XPathVariableBinding();
            result.Name = name;
            return result;
        }

        public IXPathFunction? ResolveXPathFunction(XmlName name, int arity, XPathStaticContext context)
        {
            return this;
        }

        public XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy)
        {
            XmlDocument result = new XmlDocument();
            result.DocumentUri = uri;
            return result;
        }

        public XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy)
        {
            return Collection;
        }

        public string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy)
        {
            return ["urn:item"];
        }

        public int CompareXPathCollation(string left, string right)
        {
            return string.CompareOrdinal(left, right);
        }

        public byte[] CreateXPathCollationKey(string value)
        {
            return System.Text.Encoding.UTF8.GetBytes(value);
        }

        public void ReceiveXPathTrace(string label, XdmSequence value, SourceRange? range)
        {
            TraceLabel = label;
            TraceValue = value;
            TraceRange = range;
        }

        private static XmlSchemaValidationResult CreateValidationResult(XmlSchemaSet schemaSet, XmlSchemaValidationOptions options)
        {
            XmlSchemaValidationResult result = new XmlSchemaValidationResult();
            result.SchemaSet = schemaSet;
            result.Options = options;
            result.Validity = XmlSchemaValidity.Valid;
            result.Succeeded = true;
            return result;
        }
    }

    private sealed class XsltContractStub : IXsltPackageResolver, IXsltExtensionInstruction, IXsltMessageReceiver, IXsltResultDocumentResolver, IXsltTraceReceiver, IXsltStreamAnalyzer
    {
        internal XsltExtensionBinding? ExtensionBinding;
        internal XsltMessage? Message;
        internal XsltTraceEvent? Trace;
        internal XmlDiagnostic? Diagnostic;

        public XsltExecutable ResolveXsltPackage(XsltPackageReference package, XsltCompileOptions options)
        {
            XsltStreamabilityReport report = new XsltStreamabilityReport();
            report.Streamability = XsltStreamability.Streamable;
            XsltExecutable result = new XsltExecutable();
            result.PackageName = package.Name;
            result.PackageVersion = package.Version;
            result.CompileOptions = options;
            result.StreamabilityReport = report;
            return result;
        }

        public XmlResolvedResource ResolveXsltModule(string uri, string? baseUri, XmlResolverPolicy policy)
        {
            XmlResolvedResource result = new XmlResolvedResource();
            result.Kind = XmlResourceKind.Stylesheet;
            result.RequestedUri = uri;
            result.AbsoluteUri = baseUri + uri;
            return result;
        }

        public XdmSequence ExecuteXsltExtensionInstruction(XsltExtensionBinding binding, XdmSequence content, XPathDynamicContext context)
        {
            ExtensionBinding = binding;
            return content;
        }

        public void ReceiveXsltMessage(XsltMessage message)
        {
            Message = message;
        }

        public XsltSecondaryResult AllocateXsltResultDocument(string uri, XmlSerializationOptions options)
        {
            XsltSecondaryResult result = new XsltSecondaryResult();
            result.Uri = uri;
            result.SerializationOptions = options;
            result.Disposition = XsltResultDisposition.Pending;
            return result;
        }

        public void CommitXsltResultDocument(XsltSecondaryResult result)
        {
            result.Disposition = XsltResultDisposition.Commit;
            result.Succeeded = true;
        }

        public void RejectXsltResultDocument(XsltSecondaryResult result, XmlDiagnostic diagnostic)
        {
            result.Disposition = XsltResultDisposition.Reject;
            result.Succeeded = false;
            Diagnostic = diagnostic;
        }

        public void RollbackXsltResultDocument(XsltSecondaryResult result)
        {
            result.Disposition = XsltResultDisposition.Rollback;
            result.Succeeded = false;
        }

        public void ReceiveXsltTrace(XsltTraceEvent value)
        {
            Trace = value;
        }

        public XsltStreamabilityReport AnalyzeXsltStreamability(XsltExecutable executable)
        {
            return executable.StreamabilityReport!;
        }
    }
}
