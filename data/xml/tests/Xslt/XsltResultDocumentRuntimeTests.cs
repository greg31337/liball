using System.IO;
using xml;

namespace xml.Tests;

[TestFixture]
public class XsltResultDocumentRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void ResultDocumentUsesNamedFormatResolvesUriAndRecordsResults()
    {
        string source = Stylesheet(
            "<xsl:output name='f:text' method='text' item-separator='|'/>" +
            "<xsl:template name='entry'><xsl:sequence select=\"'principal'\"/>" +
            "<xsl:result-document href='results/{1}.txt' format='f:text' item-separator='{\",\"}'><xsl:sequence select='1, 2'/></xsl:result-document>" +
            "<xsl:sequence select=\"'done'\"/></xsl:template>");
        ResultResolver resolver = new ResultResolver();
        XsltTransformationState state = Initialize(source, resolver, null);

        XdmSequence principal = state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation();
        XsltSecondaryResult secondary = state.Result.SecondaryResults!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(AtomicStrings(principal), Is.EqualTo(new[] { "principal", "done" }));
            Assert.That(AtomicStrings(state.Result.PrincipalValue!), Is.EqualTo(new[] { "principal", "done" }));
            Assert.That(state.Result.Succeeded, Is.True);
            Assert.That(secondary.Uri, Is.EqualTo("https://example.test/results/1.txt"));
            Assert.That(AtomicStrings(secondary.Value!), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(secondary.SerializationOptions!.Method, Is.EqualTo(XmlSerializationMethod.Text));
            Assert.That(secondary.SerializationOptions.ItemSeparator, Is.EqualTo(","));
            Assert.That(resolver.Text.ToString(), Is.EqualTo("1,2"));
            Assert.That(resolver.Commits, Is.EqualTo(1));
            Assert.That(secondary.Disposition, Is.EqualTo(XsltResultDisposition.Commit));
        });
    }

    [Test]
    public void DuplicateResultUriIsRejectedAndEarlierAllocationIsRolledBack()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:result-document href='same.xml'><one/></xsl:result-document>" +
            "<xsl:result-document href='./same.xml'><two/></xsl:result-document>" +
            "</xsl:template>");
        ResultResolver resolver = new ResultResolver();
        XsltTransformationState state = Initialize(source, resolver, null);

        XsltDynamicException error = Assert.Throws<XsltDynamicException>(() => state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1490));
            Assert.That(resolver.Allocations, Is.EqualTo(2));
            Assert.That(resolver.Rejections, Is.EqualTo(1));
            Assert.That(resolver.Rollbacks, Is.EqualTo(1));
            Assert.That(resolver.Commits, Is.Zero);
            Assert.That(state.Result.Succeeded, Is.False);
        });
    }

    [Test]
    public void PrincipalUriCollisionIsRejected()
    {
        string source = Stylesheet("<xsl:template name='entry'><xsl:result-document href='principal.xml'><out/></xsl:result-document></xsl:template>");
        ResultResolver resolver = new ResultResolver();
        XmlOutput principal = new XmlOutput
        {
            Kind = XmlOutputKind.TextWriter,
            TextWriter = new StringWriter(),
            BaseUri = "https://example.test/principal.xml"
        };
        XsltTransformationState state = Initialize(source, resolver, principal);

        XsltDynamicException error = Assert.Throws<XsltDynamicException>(() => state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1490));
            Assert.That(resolver.Rejections, Is.EqualTo(1));
            Assert.That(resolver.Commits, Is.Zero);
        });
    }

    [Test]
    public void FailedResultConstructionRollsBackPendingDestination()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'><xsl:result-document href='failed.xml'><xsl:sequence select=\"error(QName('', 'FAIL'))\"/></xsl:result-document></xsl:template>");
        ResultResolver resolver = new ResultResolver();
        XsltTransformationState state = Initialize(source, resolver, null);

        Assert.Throws<XPathDynamicException>(() => state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation());

        Assert.Multiple(() =>
        {
            Assert.That(resolver.Rollbacks, Is.EqualTo(1));
            Assert.That(resolver.Commits, Is.Zero);
            Assert.That(state.Result.SecondaryResults!.Single().Disposition, Is.EqualTo(XsltResultDisposition.Rollback));
        });
    }

    [Test]
    public void ResultUriCannotBeReadLaterInTheSameTransformation()
    {
        string source = Stylesheet(
            "<xsl:template name='entry'>" +
            "<xsl:result-document href='shared.xml'><out/></xsl:result-document>" +
            "<xsl:source-document href='shared.xml'><xsl:sequence select='.'/></xsl:source-document>" +
            "</xsl:template>");
        CombinedResolver resolver = new CombinedResolver();
        XsltTransformationState state = InitializeWithResources(source, resolver);

        XsltDynamicException error = Assert.Throws<XsltDynamicException>(() => state.TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1500));
            Assert.That(resolver.ResourceCalls, Is.Zero);
            Assert.That(resolver.Rollbacks, Is.EqualTo(1));
        });
    }

    [Test]
    public void MissingNamedFormatAndTemporaryOutputStateReportDynamicErrors()
    {
        string missing = Stylesheet("<xsl:template name='entry'><xsl:result-document href='out.xml' format='missing'/></xsl:template>");
        string temporary = Stylesheet("<xsl:template name='entry'><xsl:variable name='v'><xsl:result-document href='out.xml'/></xsl:variable><xsl:sequence select='$v'/></xsl:template>");

        XsltDynamicException formatError = Assert.Throws<XsltDynamicException>(() => Initialize(missing, new ResultResolver(), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;
        XsltDynamicException stateError = Assert.Throws<XsltDynamicException>(() => Initialize(temporary, new ResultResolver(), null).TemplateRuntime!.ExecuteXsltInitialTemplateInvocation())!;

        Assert.Multiple(() =>
        {
            Assert.That(formatError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1460));
            Assert.That(stateError.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTDE1480));
        });
    }

    private static XsltTransformationState Initialize(string source, IXsltResultDocumentResolver resolver, XmlOutput? principal)
    {
        XsltCompileOptions compileOptions = new XsltCompileOptions { BaseUri = "https://example.test/main.xsl" };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, compileOptions, new XmlDefaultResolver());
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            PrincipalOutput = principal,
            ResultDocumentResolver = resolver
        };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
    }

    private static XsltTransformationState InitializeWithResources(string source, CombinedResolver resolver)
    {
        XsltCompileOptions compileOptions = new XsltCompileOptions { BaseUri = "https://example.test/main.xsl" };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source }, compileOptions, new XmlDefaultResolver());
        XsltTransformOptions options = new XsltTransformOptions
        {
            InitialTemplate = new XmlName { LocalName = "entry" },
            ResourceResolver = resolver,
            ResultDocumentResolver = resolver
        };
        return new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
    }

    private static string[] AtomicStrings(XdmSequence sequence)
    {
        return sequence.AtomicValues!.Select(value => value.StringValue ?? value.CanonicalValue ?? value.LexicalValue ?? "").ToArray();
    }

    private static string Stylesheet(string content)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' xmlns:f='urn:format' version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private sealed class ResultResolver : IXsltResultDocumentResolver
    {
        internal int Allocations;
        internal int Commits;
        internal int Rejections;
        internal int Rollbacks;
        internal StringWriter Text = new StringWriter();

        public XsltSecondaryResult AllocateXsltResultDocument(string uri, XmlSerializationOptions options)
        {
            Allocations = Allocations + 1;
            return new XsltSecondaryResult
            {
                Uri = uri,
                SerializationOptions = options,
                Output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = Text, BaseUri = uri }
            };
        }

        public void CommitXsltResultDocument(XsltSecondaryResult result)
        {
            Commits = Commits + 1;
        }

        public void RejectXsltResultDocument(XsltSecondaryResult result, XmlDiagnostic diagnostic)
        {
            Rejections = Rejections + 1;
        }

        public void RollbackXsltResultDocument(XsltSecondaryResult result)
        {
            Rollbacks = Rollbacks + 1;
        }
    }

    private sealed class CombinedResolver : IXsltResultDocumentResolver, IXmlResourceResolver
    {
        internal int ResourceCalls;
        internal int Rollbacks;

        public XsltSecondaryResult AllocateXsltResultDocument(string uri, XmlSerializationOptions options)
        {
            return new XsltSecondaryResult { Uri = uri, SerializationOptions = options };
        }

        public void CommitXsltResultDocument(XsltSecondaryResult result)
        {
        }

        public void RejectXsltResultDocument(XsltSecondaryResult result, XmlDiagnostic diagnostic)
        {
        }

        public void RollbackXsltResultDocument(XsltSecondaryResult result)
        {
            Rollbacks = Rollbacks + 1;
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            ResourceCalls = ResourceCalls + 1;
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = systemIdentifier,
                Input = new XmlInput { Text = "<read/>", BaseUri = systemIdentifier }
            };
        }
    }
}
