namespace svg.Tests;

public class SvgExceptionTests
{
    [Test]
    public void MessageConstructorsPreserveFailureMessages()
    {
        Exception[] exceptions =
        [
            SvgException.Create("API failed."),
            SvgXmlException.Create("XML failed."),
            SvgResourceException.Create("Resource failed."),
            SvgRenderingException.Create("Rendering failed.")
        ];

        Assert.That(
            exceptions.Select(exception => exception.Message),
            Is.EqualTo(new[]
            {
                "API failed.",
                "XML failed.",
                "Resource failed.",
                "Rendering failed."
            }));
    }

    [Test]
    public void InnerExceptionConstructorsPreserveCauses()
    {
        InvalidOperationException cause = new InvalidOperationException("Underlying failure.");
        Exception[] exceptions =
        [
            SvgException.Create("API failed.", cause),
            SvgXmlException.Create("XML failed.", cause),
            SvgResourceException.Create("Resource failed.", cause),
            SvgRenderingException.Create("Rendering failed.", cause)
        ];

        Assert.That(exceptions, Has.All.Matches<Exception>(exception => ReferenceEquals(exception.InnerException, cause)));
    }

    [Test]
    public void FullConstructorsAttachPrimaryDiagnostic()
    {
        SvgDiagnostic diagnostic = new SvgDiagnostic(
            SvgDiagnosticSeverity.Error,
            SvgDiagnosticCode.InternalInvariant,
            "Operation cannot continue.",
            null);
        SvgException api = new SvgException("API failed.", null, diagnostic);
        SvgXmlException xml = new SvgXmlException("XML failed.", null, diagnostic);
        SvgResourceException resource = new SvgResourceException("Resource failed.", null, diagnostic);
        SvgRenderingException rendering = new SvgRenderingException("Rendering failed.", null, diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(api.PrimaryDiagnostic, Is.SameAs(diagnostic));
            Assert.That(api.Diagnostics, Is.EqualTo(new[] { diagnostic }));
            Assert.That(xml.PrimaryDiagnostic, Is.SameAs(diagnostic));
            Assert.That(xml.Diagnostics, Is.EqualTo(new[] { diagnostic }));
            Assert.That(resource.PrimaryDiagnostic, Is.SameAs(diagnostic));
            Assert.That(resource.Diagnostics, Is.EqualTo(new[] { diagnostic }));
            Assert.That(rendering.PrimaryDiagnostic, Is.SameAs(diagnostic));
            Assert.That(rendering.Diagnostics, Is.EqualTo(new[] { diagnostic }));
        });
    }

    [Test]
    public void AddingDiagnosticsSetsPrimaryOnceAndPreservesInsertionOrder()
    {
        SvgDiagnostic first = new SvgDiagnostic(
            SvgDiagnosticSeverity.Error,
            SvgDiagnosticCode.ResourceFailed,
            "Primary failure.",
            null);
        SvgDiagnostic second = new SvgDiagnostic(
            SvgDiagnosticSeverity.Warning,
            SvgDiagnosticCode.CallbackRequired,
            "Related fallback.",
            null);
        SvgException api = SvgException.Create();
        SvgXmlException xml = SvgXmlException.Create();
        SvgResourceException resource = SvgResourceException.Create();
        SvgRenderingException rendering = SvgRenderingException.Create();

        api.AddDiagnostic(first);
        api.AddDiagnostic(second);
        xml.AddDiagnostic(first);
        xml.AddDiagnostic(second);
        resource.AddDiagnostic(first);
        resource.AddDiagnostic(second);
        rendering.AddDiagnostic(first);
        rendering.AddDiagnostic(second);

        Assert.Multiple(() =>
        {
            Assert.That(api.PrimaryDiagnostic, Is.SameAs(first));
            Assert.That(api.Diagnostics, Is.EqualTo(new[] { first, second }));
            Assert.That(xml.PrimaryDiagnostic, Is.SameAs(first));
            Assert.That(xml.Diagnostics, Is.EqualTo(new[] { first, second }));
            Assert.That(resource.PrimaryDiagnostic, Is.SameAs(first));
            Assert.That(resource.Diagnostics, Is.EqualTo(new[] { first, second }));
            Assert.That(rendering.PrimaryDiagnostic, Is.SameAs(first));
            Assert.That(rendering.Diagnostics, Is.EqualTo(new[] { first, second }));
        });
    }

    [Test]
    public void DefaultConstructorsAllocateIndependentDiagnosticLists()
    {
        SvgException first = SvgException.Create();
        SvgException second = SvgException.Create();

        first.AddDiagnostic(new SvgDiagnostic(
            SvgDiagnosticSeverity.Error,
            SvgDiagnosticCode.InvalidArgument,
            "Invalid argument.",
            null));

        Assert.Multiple(() =>
        {
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(second.PrimaryDiagnostic, Is.Null);
        });
    }

    [Test]
    public void SpecializedExceptionsCanBeThrownAndCaught()
    {
        SvgXmlException xml = Assert.Throws<SvgXmlException>(() => ThrowXmlFailure());
        SvgResourceException resource = Assert.Throws<SvgResourceException>(() => ThrowResourceFailure());
        SvgRenderingException rendering = Assert.Throws<SvgRenderingException>(() => ThrowRenderingFailure());

        Assert.Multiple(() =>
        {
            Assert.That(xml.Message, Is.EqualTo("Malformed document."));
            Assert.That(resource.Message, Is.EqualTo("Resource unavailable."));
            Assert.That(rendering.Message, Is.EqualTo("Surface limit exceeded."));
        });
    }

    [Test]
    public void OptionalPrimaryDiagnosticsAndAddedDiagnosticValidationAreHandled()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new SvgException("failure", null, null).PrimaryDiagnostic, Is.Null);
            Assert.That(new SvgXmlException("failure", null, null).PrimaryDiagnostic, Is.Null);
            Assert.That(new SvgResourceException("failure", null, null).PrimaryDiagnostic, Is.Null);
            Assert.That(new SvgRenderingException("failure", null, null).PrimaryDiagnostic, Is.Null);
            Assert.Throws<ArgumentNullException>(() => SvgException.Create().AddDiagnostic(null));
            Assert.Throws<ArgumentNullException>(() => SvgXmlException.Create().AddDiagnostic(null));
            Assert.Throws<ArgumentNullException>(() => SvgResourceException.Create().AddDiagnostic(null));
            Assert.Throws<ArgumentNullException>(() => SvgRenderingException.Create().AddDiagnostic(null));
        });
    }

    static void ThrowXmlFailure()
    {
        throw SvgXmlException.Create("Malformed document.");
    }

    static void ThrowResourceFailure()
    {
        throw SvgResourceException.Create("Resource unavailable.");
    }

    static void ThrowRenderingFailure()
    {
        throw SvgRenderingException.Create("Surface limit exceeded.");
    }
}
