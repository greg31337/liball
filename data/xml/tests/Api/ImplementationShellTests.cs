using xml;

namespace xml.Tests;

[TestFixture]
public class ImplementationShellTests
{
    [Test]
    public void PlannedImplementationClassesConstructAsTheirContracts()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new XmlParser(), Is.InstanceOf<IXmlParser>());
            Assert.That(new XmlPushParser(), Is.InstanceOf<IXmlPushParser>());
            Assert.That(new XmlEventCollector(), Is.InstanceOf<IXmlEventHandler>());
            Assert.That(new XmlPullReader(), Is.InstanceOf<IXmlPullReader>());
            Assert.That(new XmlTreeNavigator(), Is.InstanceOf<IXmlTreeNavigator>());
            Assert.That(new XmlTreeEditor(), Is.InstanceOf<IXmlTreeEditor>());
            Assert.That(new XmlEventEmitter(), Is.InstanceOf<IXmlEventEmitter>());
            Assert.That(new XmlDefaultResolver(), Is.InstanceOf<IXmlResourceResolver>());
            Assert.That(new XmlCatalogResolver(), Is.InstanceOf<IXmlCatalogResolver>());
            Assert.That(new XmlDiagnosticCollector(), Is.InstanceOf<IXmlErrorReporter>());
            Assert.That(new XmlCancellationFlag(), Is.InstanceOf<IXmlCancellation>());
            Assert.That(new DtdValidator(), Is.InstanceOf<IDtdValidator>());
            Assert.That(new XmlIncludeProcessor(), Is.InstanceOf<IXmlIncludeProcessor>());
            Assert.That(new XmlEscaper(), Is.InstanceOf<IXmlEscaper>());
            Assert.That(new XmlSerializer(), Is.InstanceOf<IXmlSerializer>());
            Assert.That(new XmlCanonicalizer(), Is.InstanceOf<IXmlCanonicalizer>());
            Assert.That(new XmlByteCollector(), Is.InstanceOf<IXmlByteSink>());
            Assert.That(new XmlByteCollector(), Is.InstanceOf<IXmlByteCollector>());
            Assert.That(new XmlSchemaCompiler(), Is.InstanceOf<IXmlSchemaCompiler>());
            Assert.That(new XmlSchemaValidator(), Is.InstanceOf<IXmlSchemaValidator>());
            Assert.That(new XPathCompiler(), Is.InstanceOf<IXPathCompiler>());
            Assert.That(new XPathEvaluator(), Is.InstanceOf<IXPathEvaluator>());
            Assert.That(new XPathFunctionAdapter(), Is.InstanceOf<IXPathFunction>());
            Assert.That(new XPathVariableResolver(), Is.InstanceOf<IXPathVariableResolver>());
            Assert.That(new XPathFunctionResolver(), Is.InstanceOf<IXPathFunctionResolver>());
            Assert.That(new XPathCollectionResolver(), Is.InstanceOf<IXPathCollectionResolver>());
            Assert.That(new XPathCodepointCollation(), Is.InstanceOf<IXPathCollation>());
            Assert.That(new XPathTraceCollector(), Is.InstanceOf<IXPathTraceReceiver>());
            Assert.That(new XsltCompiler(), Is.InstanceOf<IXsltCompiler>());
            Assert.That(new XsltTransformer(), Is.InstanceOf<IXsltTransformer>());
            Assert.That(new XsltPackageResolver(), Is.InstanceOf<IXsltPackageResolver>());
            Assert.That(new XsltExtensionInstructionAdapter(), Is.InstanceOf<IXsltExtensionInstruction>());
            Assert.That(new XsltMessageCollector(), Is.InstanceOf<IXsltMessageReceiver>());
            Assert.That(new XsltResultDocumentCollector(), Is.InstanceOf<IXsltResultDocumentResolver>());
            Assert.That(new XsltTraceCollector(), Is.InstanceOf<IXsltTraceReceiver>());
            Assert.That(new XsltStreamAnalyzer(), Is.InstanceOf<IXsltStreamAnalyzer>());
        });
    }

    [Test]
    public void StatefulShellsExposeSafeBaselineBehavior()
    {
        XmlPushParser push = new XmlPushParser();
        Assert.That(push.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Ready));
        XmlInput pushInput = new XmlInput();
        pushInput.Text = "<root/>";
        Assert.That(push.BeginXmlPush(pushInput, new XmlParserOptions(), null, null, null, null), Is.EqualTo(XmlParserStatus.Running));
        Assert.That(push.PauseXmlPush(), Is.EqualTo(XmlParserStatus.Paused));
        Assert.That(push.ResumeXmlPush(), Is.EqualTo(XmlParserStatus.Running));
        XmlEventBatch final = push.FinishXmlPush();
        Assert.That(final.IsFinal, Is.True);
        Assert.That(final.Status, Is.EqualTo(XmlParserStatus.Completed));
        push.ResetXmlPush();
        Assert.That(push.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Ready));
        push.CloseXmlPush();
        Assert.That(push.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Closed));

        XmlDocument document = new XmlDocument();
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode parent = editor.CreateXmlTreeNode(document, XmlNodeKind.Element, new XmlName(), null);
        XmlNode child = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "text");
        editor.AppendXmlTreeChild(parent, child);
        XmlTreeNavigator navigator = new XmlTreeNavigator();
        Assert.That(navigator.FindXmlParent(child), Is.SameAs(parent));
        Assert.That(navigator.SelectXmlChildren(parent).Nodes, Is.EqualTo(new XmlNode[] { child }));
        Assert.That(navigator.GetXmlNodeStringValue(child), Is.EqualTo("text"));

        XmlSchemaSet schemaSet = new XmlSchemaCompiler().CompileXmlSchemas([], new XmlSchemaValidationOptions(), new XmlDefaultResolver());
        XmlSchemaValidationOptions validationOptions = new XmlSchemaValidationOptions();
        XmlSchemaValidationResult validation = new XmlSchemaValidator().ValidateXmlSchema(document, schemaSet, validationOptions);
        Assert.That(validation.Document, Is.SameAs(document));
        Assert.That(validation.SchemaSet, Is.SameAs(schemaSet));
        Assert.That(validation.Options, Is.SameAs(validationOptions));
        Assert.That(validation.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));

        XPathStaticContext staticContext = new XPathStaticContext();
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(".", staticContext);
        XPathDynamicContext dynamicContext = new XPathDynamicContext();
        dynamicContext.ContextItem = new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmStringAtomicValue("context", XdmAtomicType.String));
        XPathEvaluationResult evaluation = new XPathEvaluator().EvaluateXPath(expression, dynamicContext);
        Assert.That(expression.Source, Is.EqualTo("."));
        Assert.That(expression.StaticContext, Is.SameAs(staticContext));
        Assert.That(evaluation.Expression, Is.SameAs(expression));
        Assert.That(evaluation.DynamicContext, Is.Not.SameAs(dynamicContext));
        Assert.That(evaluation.Value!.Count, Is.EqualTo(1));
        Assert.That(evaluation.Succeeded, Is.True);

        XPathCodepointCollation collation = new XPathCodepointCollation();
        Assert.That(collation.CompareXPathCollation("a", "b"), Is.LessThan(0));
        Assert.That(collation.CreateXPathCollationKey("abc"), Is.EqualTo(new byte[] { 97, 98, 99 }));
        Assert.That(new XmlCancellationFlag().IsXmlCancellationRequested(), Is.False);
        Assert.That(new XmlEventCollector().HandleXmlEvent(new XmlEvent()), Is.EqualTo(XmlHandlerAction.Continue));
        Assert.That(new XmlEscaper().EscapeXmlValue("text", XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0'), Is.EqualTo("text"));
        Assert.That(new XmlIncludeProcessor().ProcessXmlIncludes(document, new XmlIncludeOptions(), new XmlDefaultResolver()), Is.SameAs(document));
    }

    [Test]
    public void XsltShellsPreserveRequestsAndResultDocumentTransitions()
    {
        XmlName name = new XmlName();
        name.LocalName = "package";
        XsltPackageReference package = new XsltPackageReference();
        package.Name = name;
        package.Version = "1.0";
        XsltCompileOptions compileOptions = new XsltCompileOptions();
        Assert.That(() => new XsltPackageResolver().ResolveXsltPackage(package, compileOptions), Throws.ArgumentException);
        XsltExecutable executable = new XsltExecutable();

        XdmSequence content = new XdmSequence();
        XdmSequence extensionResult = new XsltExtensionInstructionAdapter().ExecuteXsltExtensionInstruction(new XsltExtensionBinding(), content, new XPathDynamicContext());
        Assert.That(extensionResult, Is.SameAs(content));

        XsltResultDocumentCollector collector = new XsltResultDocumentCollector();
        XmlSerializationOptions serialization = new XmlSerializationOptions();
        XsltSecondaryResult secondary = collector.AllocateXsltResultDocument("file:///secondary.xml", serialization);
        Assert.That(secondary.Uri, Is.EqualTo("file:///secondary.xml"));
        Assert.That(secondary.SerializationOptions, Is.SameAs(serialization));
        Assert.That(secondary.Disposition, Is.EqualTo(XsltResultDisposition.Pending));
        collector.CommitXsltResultDocument(secondary);
        Assert.That(secondary.Disposition, Is.EqualTo(XsltResultDisposition.Commit));
        Assert.That(secondary.Succeeded, Is.True);
        collector.RejectXsltResultDocument(secondary, new XmlDiagnostic());
        Assert.That(secondary.Disposition, Is.EqualTo(XsltResultDisposition.Reject));
        Assert.That(secondary.Succeeded, Is.False);
        collector.RollbackXsltResultDocument(secondary);
        Assert.That(secondary.Disposition, Is.EqualTo(XsltResultDisposition.Rollback));

        XsltStreamabilityReport report = new XsltStreamabilityReport();
        executable.StreamabilityReport = report;
        Assert.That(new XsltStreamAnalyzer().AnalyzeXsltStreamability(executable), Is.SameAs(report));
    }
}
