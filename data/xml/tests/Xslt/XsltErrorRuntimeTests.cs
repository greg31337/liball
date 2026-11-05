using xml;

namespace xml.Tests;

[TestFixture]
public class XsltErrorRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Errors = "http://www.w3.org/2005/xqt-errors";

    [Test]
    public void TryRollsBackPartialValuesAndUsesTheFirstMatchingCatch()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:try>" +
            "<xsl:sequence select='999'/><xsl:sequence select='1 idiv 0'/>" +
            "<xsl:catch errors='err:FOAR0001' select=\"($err:code, $err:description, $err:line-number)\"/>" +
            "<xsl:catch errors='*' select=\"'wrong'\"/>" +
            "</xsl:try></xsl:template>", "xmlns:err='" + Errors + "'");

        XsltTransformationState state = Initialize(source, false);
        XdmSequence value = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(value.Count, Is.EqualTo(3));
            Assert.That(value.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.QName));
            Assert.That(value.AtomicValues[0].NameValue!.LocalName, Is.EqualTo("FOAR0001"));
            Assert.That(value.AtomicValues[0].NameValue!.NamespaceUri, Is.EqualTo(Errors));
            Assert.That(value.AtomicValues[1].StringValue, Does.Contain("zero"));
            Assert.That(value.AtomicValues[2].IntegerValue, Is.GreaterThan(0));
            Assert.That(value.AtomicValues.Any(item => item.IntegerValue == 999 && item.Type == XdmAtomicType.Integer), Is.False);
        });
    }

    [Test]
    public void TrySelectCanBeCaughtAndAnUnmatchedErrorPropagates()
    {
        string caughtSource = Stylesheet(
            "<xsl:template name='entry'><xsl:try select='1 idiv 0'>" +
            "<xsl:catch errors='err:FOAR0001' select=\"'caught'\"/>" +
            "</xsl:try></xsl:template>", "xmlns:err='" + Errors + "'");
        string unmatchedSource = Stylesheet(
            "<xsl:template name='entry'><xsl:try select='1 idiv 0'>" +
            "<xsl:catch errors='err:FODC0001' select=\"'wrong'\"/>" +
            "</xsl:try></xsl:template>", "xmlns:err='" + Errors + "'");

        XdmSequence caught = Initialize(caughtSource, false).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XPathDynamicException unmatched = Assert.Throws<XPathDynamicException>(() => Initialize(unmatchedSource, false).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(caught.AtomicValues!.Single().StringValue, Is.EqualTo("caught"));
            Assert.That(unmatched.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
        });
    }

    [Test]
    public void CatchReceivesTheCustomNameAndValueFromFnError()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:try select=\"error(QName('urn:custom', 'e:oops'), 'boom', 42)\">" +
            "<xsl:catch errors='e:oops' select='($err:code, $err:value)'/>" +
            "</xsl:try></xsl:template>", "xmlns:e='urn:custom' xmlns:err='" + Errors + "'");

        XdmSequence value = Initialize(source, false).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(value.Count, Is.EqualTo(2));
            Assert.That(value.AtomicValues![0].NameValue!.LocalName, Is.EqualTo("oops"));
            Assert.That(value.AtomicValues[0].NameValue!.NamespaceUri, Is.EqualTo("urn:custom"));
            Assert.That(value.AtomicValues[1].IntegerValue, Is.EqualTo(42));
        });
    }

    [Test]
    public void MessageRecordsConstructedContentAndDoesNotAddResultItems()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:message select=\"'prefix'\"><xsl:text>-suffix</xsl:text></xsl:message>" +
            "<xsl:sequence select=\"'after'\"/>" +
            "</xsl:template>", "");
        XsltTransformationState state = Initialize(source, false);

        XdmSequence value = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XsltMessage message = state.Result.Messages!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(value.AtomicValues!.Single().StringValue, Is.EqualTo("after"));
            Assert.That(message.Text, Is.EqualTo("prefix-suffix"));
            Assert.That(message.Terminate, Is.False);
            Assert.That(message.Code!.LocalName, Is.EqualTo(XsltErrorCodes.XTMM9000));
            Assert.That(message.Content!.Nodes!.Single().Kind, Is.EqualTo(XmlNodeKind.Document));
        });
    }

    [Test]
    public void TerminatingMessageCanBeCaughtByItsCustomCodeAndSuppliesErrorValue()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:try>" +
            "<xsl:message terminate='yes' error-code='e:halt' select=\"'stop'\"/>" +
            "<xsl:catch errors='e:halt' select='($err:code, $err:value)'/>" +
            "</xsl:try></xsl:template>", "xmlns:e='urn:errors' xmlns:err='" + Errors + "'");
        XsltTransformationState state = Initialize(source, false);

        XdmSequence value = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XsltMessage message = state.Result.Messages!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(value.Count, Is.EqualTo(2));
            Assert.That(value.AtomicValues!.Single().NameValue!.LocalName, Is.EqualTo("halt"));
            Assert.That(value.AtomicValues!.Single().NameValue!.NamespaceUri, Is.EqualTo("urn:errors"));
            Assert.That(new XdmNodeAccessor().GetXdmNodeStringValue(new XdmNodeAccessor().WrapXmlNode(value.Nodes!.Single())), Is.EqualTo("stop"));
            Assert.That(message.Text, Is.EqualTo("stop"));
            Assert.That(message.Terminate, Is.True);
            Assert.That(message.Code!.NamespaceUri, Is.EqualTo("urn:errors"));
        });
    }

    [Test]
    public void UnhandledTerminatingMessageThrowsTypedTermination()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:message terminate='yes' error-code='e:halt' select=\"'stop'\"/></xsl:template>",
            "xmlns:e='urn:errors'");
        XsltTransformationState state = Initialize(source, false);

        XsltTerminationException error = Assert.Throws<XsltTerminationException>(() => state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Is.EqualTo("stop"));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo("Q{urn:errors}halt"));
            Assert.That(error.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XsltDynamic));
            Assert.That(error.Diagnostic.Range!.Start!.SourceUri, Is.EqualTo("https://example.test/main.xsl"));
            Assert.That(state.Result.Succeeded, Is.False);
            Assert.That(state.Result.Messages!.Single().Terminate, Is.True);
        });
    }

    [Test]
    public void AssertionsAreDisabledByDefaultAndCatchableWhenEnabled()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:try>" +
            "<xsl:assert test='false()' select=\"'assertion failed'\"/>" +
            "<xsl:catch errors='err:XTMM9001' select='$err:value'/>" +
            "</xsl:try></xsl:template>", "xmlns:err='" + Errors + "'");
        XsltTransformationState disabled = Initialize(source, false);
        XsltTransformationState enabled = Initialize(source, true);

        XdmSequence disabledValue = disabled.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XdmSequence enabledValue = enabled.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.Multiple(() =>
        {
            Assert.That(disabledValue.Count, Is.EqualTo(0));
            Assert.That(disabled.Result.Messages, Is.Empty);
            Assert.That(enabledValue.Nodes, Has.Length.EqualTo(1));
            Assert.That(enabled.Result.Messages!.Single().Code!.LocalName, Is.EqualTo(XsltErrorCodes.XTMM9001));
            Assert.That(enabled.Result.Messages!.Single().Text, Is.EqualTo("assertion failed"));
        });
    }

    [Test]
    public void ForwardCompatibleUnknownInstructionEvaluatesFallbacks()
    {
        string source = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='4.0'>" +
            "<xsl:template name='entry'><xsl:future>" +
            "<xsl:fallback><xsl:sequence select=\"'fallback'\"/></xsl:fallback>" +
            "</xsl:future></xsl:template></xsl:stylesheet>";

        XdmSequence value = Initialize(source, false).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();

        Assert.That(value.AtomicValues!.Single().StringValue, Is.EqualTo("fallback"));
    }

    [Test]
    public void UnhandledErrorsCarrySourceAndTemplateStack()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:call-template name='child'/></xsl:template>" +
            "<xsl:template name='child'><xsl:sequence select='1 idiv 0'/></xsl:template>", "");

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Initialize(source, false).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Range!.Start!.SourceUri, Is.EqualTo("https://example.test/main.xsl"));
            Assert.That(error.Diagnostic.Range.Start.Line, Is.GreaterThan(0));
            Assert.That(error.Diagnostic.TemplateStack, Is.EqualTo(new[] { "Q{}entry", "Q{}child" }));
        });
    }

    private static XsltTransformationState Initialize(string source, bool assertions)
    {
        XsltCompileOptions compileOptions = new XsltCompileOptions
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
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, compileOptions, new XmlDefaultResolver());
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            EnableAssertions = assertions
        };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
    }

    private static string Stylesheet(string content, string attributes)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'>" + content + "</xsl:stylesheet>";
    }
}
