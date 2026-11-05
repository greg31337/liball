using xml;

namespace xml.Tests;

[TestFixture]
public class XsltTransformationEngineTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void InitializesApplyTemplatesSourceGlobalContextModePoolClockAndDestination()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:m='urn:modes' version='3.0' default-mode='m:work'>" +
            "<xsl:mode name='m:work'/><xsl:output method='text'/><xsl:template match='/' mode='m:work'/></xsl:stylesheet>";
        XsltExecutable executable = Compile(source);
        XmlDocument document = Parse("<root/>", "https://example.test/source.xml");
        System.IO.StringWriter writer = new System.IO.StringWriter();
        XsltTransformOptions options = new XsltTransformOptions
        {
            SourceDocument = document,
            PrincipalOutput = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer, BaseUri = "https://example.test/result.xml" },
            SecurityLimits = new XmlSecurityLimits { MaximumExternalResources = 7, MaximumResourceBytes = 4096 },
            ResultDisposition = XsltResultDisposition.Pending
        };
        System.DateTimeOffset before = System.DateTimeOffset.UtcNow;

        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, options);

        System.DateTimeOffset after = System.DateTimeOffset.UtcNow;
        Assert.Multiple(() =>
        {
            Assert.That(state.InvocationKind, Is.EqualTo(XsltInvocationKind.ApplyTemplates));
            Assert.That(state.InitialMatchSelection.Count, Is.EqualTo(1));
            Assert.That(state.InitialMatchSelection.Nodes, Is.EqualTo(new[] { document.DocumentNode }));
            Assert.That(state.GlobalContextItem.Nodes, Is.EqualTo(new[] { document.DocumentNode }));
            Assert.That(state.InitialModeName, Is.EqualTo("Q{urn:modes}work"));
            Assert.That(state.InitialMode!.Name, Is.EqualTo("Q{urn:modes}work"));
            Assert.That(state.CurrentDateTime.UtcDateTime, Is.InRange(before.UtcDateTime, after.UtcDateTime));
            Assert.That(state.DynamicContext.CurrentDateTime, Is.EqualTo(state.CurrentDateTime));
            Assert.That(state.DynamicContext.ContextItem, Is.SameAs(state.GlobalContextItem));
            Assert.That(state.DocumentPool.MaximumResources, Is.EqualTo(7));
            Assert.That(state.DocumentPool.MaximumResourceBytes, Is.EqualTo(4096));
            Assert.That(state.DocumentPool.FindXPathDocumentResource("https://example.test/source.xml"), Is.SameAs(document));
            Assert.That(state.PrincipalDestination.Output, Is.Not.SameAs(options.PrincipalOutput));
            Assert.That(state.PrincipalDestination.Output!.TextWriter, Is.SameAs(writer));
            Assert.That(state.PrincipalDestination.SerializationOptions.Method, Is.EqualTo(XmlSerializationMethod.Text));
            Assert.That(state.PrincipalDestination.Writer, Is.Not.Null);
            Assert.That(state.Options, Is.Not.SameAs(options));
            Assert.That(state.Result.Options, Is.SameAs(state.Options));
        });
    }

    [Test]
    public void SelectsPublicInitialTemplateAndFunctionAndSnapshotsParameters()
    {
        string source = "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' xmlns:xs='http://www.w3.org/2001/XMLSchema' name='urn:main' package-version='1' version='3.0'>" +
            "<xsl:global-context-item as='xs:string' use='required'/><xsl:template name='entry' visibility='public'><xsl:context-item use='optional'/></xsl:template>" +
            "<xsl:function name='f:start' visibility='public'><xsl:sequence select='1'/></xsl:function></xsl:package>";
        XsltExecutable executable = Compile(source);
        XdmSequence global = AtomicString("context");
        XdmSequence parameterValue = AtomicString("supplied");
        XmlName templateName = new XmlName { LocalName = "entry" };
        XsltParameter parameter = new XsltParameter { Name = new XmlName { LocalName = "setting" }, Value = parameterValue };
        XsltTransformOptions templateOptions = new XsltTransformOptions
        {
            GlobalContextItem = global,
            InitialTemplate = templateName,
            Parameters = [parameter]
        };

        XsltTransformationState templateState = new XsltTransformationEngine().InitializeXsltTransformation(executable, templateOptions);
        templateName.LocalName = "changed";
        parameter.Name!.LocalName = "changed";
        parameterValue.AtomicValues![0].StringValue = "changed";
        XsltTransformationState functionState = new XsltTransformationEngine().InitializeXsltTransformation(executable, new XsltTransformOptions
        {
            GlobalContextItem = global,
            InitialFunction = new XmlName { LocalName = "start", NamespaceUri = "urn:functions" }
        });

        Assert.Multiple(() =>
        {
            Assert.That(templateState.InvocationKind, Is.EqualTo(XsltInvocationKind.NamedTemplate));
            Assert.That(templateState.InitialTemplate!.Name, Is.EqualTo("Q{}entry"));
            Assert.That(templateState.InitialContextItem, Is.SameAs(templateState.GlobalContextItem));
            Assert.That(templateState.Options.InitialTemplate!.LocalName, Is.EqualTo("entry"));
            Assert.That(templateState.Parameters.Single().Name!.LocalName, Is.EqualTo("setting"));
            Assert.That(templateState.Parameters.Single().Value!.AtomicValues![0].StringValue, Is.EqualTo("supplied"));
            Assert.That(templateState.Parameters.Single(), Is.Not.SameAs(parameter));
            Assert.That(templateState.Parameters.Single().Value, Is.Not.SameAs(parameterValue));
            Assert.That(functionState.InvocationKind, Is.EqualTo(XsltInvocationKind.Function));
            Assert.That(functionState.InitialFunction!.ExpandedName, Is.EqualTo("Q{urn:functions}start"));
            Assert.That(functionState.InitialContextItem.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void UsesTheStandardDefaultInitialTemplateWithoutASelection()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='xsl:initial-template'/></xsl:stylesheet>";

        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(Compile(source), new XsltTransformOptions());

        Assert.Multiple(() =>
        {
            Assert.That(state.InvocationKind, Is.EqualTo(XsltInvocationKind.NamedTemplate));
            Assert.That(state.InitialTemplate!.Name, Is.EqualTo("Q{" + Xsl + "}initial-template"));
            Assert.That(state.InitialContextItem.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsMissingOrIneligibleInitialComponentsAndInvalidInvocationCombinations()
    {
        XsltExecutable ordinary = Compile("<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='entry'/></xsl:stylesheet>");
        XsltDynamicException missingTemplate = Assert.Throws<XsltDynamicException>(() => Initialize(ordinary, new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "missing" } }))!;
        XsltDynamicException missingFunction = Assert.Throws<XsltDynamicException>(() => Initialize(ordinary, new XsltTransformOptions { InitialFunction = new XmlName { LocalName = "missing", NamespaceUri = "urn:functions" } }))!;
        XsltDynamicException conflictingMode = Assert.Throws<XsltDynamicException>(() => Initialize(ordinary, new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            InitialMode = new XmlName { LocalName = "#unnamed" }
        }))!;
        string packageSource = "<xsl:package xmlns:xsl='" + Xsl + "' name='urn:main' package-version='1' version='3.0' declared-modes='yes'>" +
            "<xsl:mode name='private-mode'/><xsl:template match='/' mode='private-mode'/></xsl:package>";
        XsltDynamicException privateMode = Assert.Throws<XsltDynamicException>(() => Initialize(Compile(packageSource), new XsltTransformOptions
        {
            SourceDocument = Parse("<root/>", "https://example.test/source.xml"),
            InitialMode = new XmlName { LocalName = "private-mode" }
        }))!;

        Assert.Multiple(() =>
        {
            Assert.That(missingTemplate.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0040));
            Assert.That(missingFunction.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0041));
            Assert.That(conflictingMode.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0044));
            Assert.That(privateMode.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE0045));
            Assert.That(new[] { missingTemplate, missingFunction, conflictingMode, privateMode }.All(item => item.Diagnostic!.Category == XmlErrorCategory.XsltDynamic), Is.True);
        });
    }

    [Test]
    public void EnforcesGlobalAndNamedTemplateContextRequirements()
    {
        string globalRequiredSource = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:global-context-item use='required'/><xsl:template name='entry'/></xsl:stylesheet>";
        XsltTypeException missingGlobal = Assert.Throws<XsltTypeException>(() => Initialize(Compile(globalRequiredSource), new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } }))!;
        string typedSource = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:global-context-item use='optional' as='document-node()'/><xsl:template name='entry'/></xsl:stylesheet>";
        XsltTypeException wrongGlobalType = Assert.Throws<XsltTypeException>(() => Initialize(Compile(typedSource), new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            GlobalContextItem = AtomicString("not-a-document")
        }))!;
        string templateRequiredSource = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='entry'><xsl:context-item use='required'/></xsl:template></xsl:stylesheet>";
        XsltTypeException missingTemplateContext = Assert.Throws<XsltTypeException>(() => Initialize(Compile(templateRequiredSource), new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } }))!;

        Assert.Multiple(() =>
        {
            Assert.That(missingGlobal.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE0590));
            Assert.That(wrongGlobalType.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE0590));
            Assert.That(missingTemplateContext.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTTE3090));
            Assert.That(new[] { missingGlobal, wrongGlobalType, missingTemplateContext }.All(item => item.Diagnostic!.Category == XmlErrorCategory.XsltType), Is.True);
        });
    }

    [Test]
    public void PublicTransformerReturnsTheIsolatedInitializedResult()
    {
        XsltExecutable executable = Compile("<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='entry'/></xsl:stylesheet>");
        XsltTransformOptions options = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };

        XsltTransformResult result = new XsltTransformer().TransformXslt(executable, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Executable, Is.SameAs(executable));
            Assert.That(result.Options, Is.Not.SameAs(options));
            Assert.That(result.Options!.Executable, Is.SameAs(executable));
            Assert.That(result.PrincipalValue!.Count, Is.EqualTo(0));
            Assert.That(result.SecondaryResults, Is.Empty);
            Assert.That(result.Messages, Is.Empty);
            Assert.That(result.TraceEvents, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Disposition, Is.EqualTo(XsltResultDisposition.Pending));
            Assert.That(result.Succeeded, Is.True);
        });
    }

    [Test]
    public void RejectsInvalidRuntimeParametersExecutablesAndDestinations()
    {
        XsltExecutable executable = Compile("<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'><xsl:template name='entry'/></xsl:stylesheet>");
        XsltTransformOptions entry = new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } };
        XsltParameter duplicate = new XsltParameter { Name = new XmlName { LocalName = "same" }, Value = AtomicString("value") };

        Assert.Multiple(() =>
        {
            Assert.Throws<System.ArgumentException>(() => Initialize(new XsltExecutable(), entry));
            Assert.Throws<System.ArgumentException>(() => Initialize(executable, new XsltTransformOptions
            {
                InitialTemplate = entry.InitialTemplate,
                Parameters = [duplicate, duplicate]
            }));
            Assert.Throws<System.ArgumentException>(() => Initialize(executable, new XsltTransformOptions
            {
                InitialTemplate = entry.InitialTemplate,
                Parameters = [new XsltParameter { Name = new XmlName { LocalName = "static" }, Value = AtomicString("value"), IsStatic = true }]
            }));
            Assert.Throws<System.ArgumentException>(() => Initialize(executable, new XsltTransformOptions
            {
                InitialTemplate = entry.InitialTemplate,
                PrincipalOutput = new XmlOutput { Kind = XmlOutputKind.TextWriter }
            }));
        });
    }

    private static XsltTransformationState Initialize(XsltExecutable executable, XsltTransformOptions options)
    {
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
    }

    private static XdmSequence AtomicString(string value)
    {
        XdmAtomicValue atomic = new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String);
        return new XdmSequenceOperations().CreateXdmAtomicSequence(atomic);
    }

    private static XsltExecutable Compile(string source)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Denied,
                MaximumDepth = 16,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new XmlDefaultResolver());
    }

    private static XmlDocument Parse(string source, string baseUri)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source, BaseUri = baseUri }, options, null, null, null);
    }
}
