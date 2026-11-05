using xml;

namespace xml.Tests;

[TestFixture]
public class ResolverGuardTests
{
    [Test]
    public void DeniesExternalAccessByDefaultWithAResourceDiagnostic()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        RecordingResourceReporter reporter = new RecordingResourceReporter();
        ResolverGuard guard = new ResolverGuard(policy, reporter);

        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => guard.BeginXmlResource("https://example.test/data.xml", null))!;

        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceDenied));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Resource));
        Assert.That(failure.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
    }

    [Test]
    public void RestrictsSchemesCaseInsensitivelyAndResolvesRelativeUris()
    {
        XmlResolverPolicy policy = AllowedPolicy();
        policy.AllowedSchemes = ["HTTPS"];
        ResolverGuard guard = new ResolverGuard(policy, null);

        using (ResolverGuardScope scope = guard.BeginXmlResource("../data.xml", "https://example.test/a/document.xml"))
        {
            Assert.That(scope.CurrentUri.AbsoluteUri, Is.EqualTo("https://example.test/data.xml"));
        }

        XmlResourceException denied = Assert.Throws<XmlResourceException>(() => guard.BeginXmlResource("http://example.test/data.xml", null))!;
        Assert.That(denied.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceDenied));
    }

    [Test]
    public void RestrictsFileResourcesToWholeAllowedRootSegments()
    {
        XmlResolverPolicy policy = AllowedPolicy();
        policy.AllowedSchemes = ["file"];
        policy.AllowedRoots = ["/safe/root"];
        ResolverGuard guard = new ResolverGuard(policy, null);

        using (ResolverGuardScope scope = guard.BeginXmlResource("file:///safe/root/folder/data.xml", null))
        {
            Assert.That(scope.CurrentUri.IsFile, Is.True);
        }

        Assert.Throws<XmlResourceException>(() => guard.BeginXmlResource("file:///safe/root-sibling/data.xml", null));
        Assert.Throws<XmlResourceException>(() => guard.BeginXmlResource("file:///safe/root/../secret.xml", null));
    }

    [Test]
    public void EnforcesRedirectPolicyLimitsAndCycles()
    {
        XmlResolverPolicy deniedPolicy = AllowedPolicy();
        deniedPolicy.AllowRedirects = false;
        ResolverGuard deniedGuard = new ResolverGuard(deniedPolicy, null);
        using (ResolverGuardScope deniedScope = deniedGuard.BeginXmlResource("https://example.test/a", null))
        {
            Assert.Throws<XmlResourceException>(() => deniedGuard.FollowXmlResourceRedirect(deniedScope, "/b"));
        }

        XmlResolverPolicy limitedPolicy = AllowedPolicy();
        limitedPolicy.AllowRedirects = true;
        limitedPolicy.MaximumRedirects = 1;
        ResolverGuard limitedGuard = new ResolverGuard(limitedPolicy, null);
        using (ResolverGuardScope limitedScope = limitedGuard.BeginXmlResource("https://example.test/a", null))
        {
            Assert.That(limitedGuard.FollowXmlResourceRedirect(limitedScope, "/b").AbsoluteUri, Is.EqualTo("https://example.test/b"));
            XmlResourceException limit = Assert.Throws<XmlResourceException>(() => limitedGuard.FollowXmlResourceRedirect(limitedScope, "/c"))!;
            Assert.That(limit.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        }

        XmlResolverPolicy cyclePolicy = AllowedPolicy();
        cyclePolicy.AllowRedirects = true;
        ResolverGuard cycleGuard = new ResolverGuard(cyclePolicy, null);
        using (ResolverGuardScope cycleScope = cycleGuard.BeginXmlResource("https://example.test/a", null))
        {
            cycleGuard.FollowXmlResourceRedirect(cycleScope, "/b");
            XmlResourceException cycle = Assert.Throws<XmlResourceException>(() => cycleGuard.FollowXmlResourceRedirect(cycleScope, "/a"))!;
            Assert.That(cycle.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceCycle));
        }
    }

    [Test]
    public void DetectsNestedCyclesAndEnforcesActiveDepth()
    {
        XmlResolverPolicy cyclePolicy = AllowedPolicy();
        ResolverGuard cycleGuard = new ResolverGuard(cyclePolicy, null);
        using (ResolverGuardScope outer = cycleGuard.BeginXmlResource("urn:resource", null))
        {
            XmlResourceException cycle = Assert.Throws<XmlResourceException>(() => cycleGuard.BeginXmlResource("urn:resource", null))!;
            Assert.That(cycle.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceCycle));
            Assert.That(cycleGuard.ActiveXmlResourceDepth(), Is.EqualTo(1));
        }

        XmlResolverPolicy depthPolicy = AllowedPolicy();
        depthPolicy.MaximumDepth = 1;
        ResolverGuard depthGuard = new ResolverGuard(depthPolicy, null);
        using (ResolverGuardScope outer = depthGuard.BeginXmlResource("urn:first", null))
        {
            XmlResourceException depth = Assert.Throws<XmlResourceException>(() => depthGuard.BeginXmlResource("urn:second", null))!;
            Assert.That(depth.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        }

        Assert.That(depthGuard.ActiveXmlResourceDepth(), Is.Zero);
    }

    [Test]
    public void DetectsNestedResourceKeysWithoutFragmentsAsCycles()
    {
        ResolverGuard guard = new ResolverGuard(AllowedPolicy(), null);
        using (ResolverGuardScope outer = guard.BeginXmlResource("https://example.test/data.xml", null))
        {
            XmlResourceException cycle = Assert.Throws<XmlResourceException>(() => guard.BeginXmlResource("https://EXAMPLE.test:443/data.xml", null))!;

            Assert.That(cycle.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceCycle));
        }
    }

    [Test]
    public void EnforcesByteAndDeterministicWorkLimits()
    {
        XmlResolverPolicy bytePolicy = AllowedPolicy();
        bytePolicy.MaximumResourceBytes = 4;
        ResolverGuard byteGuard = new ResolverGuard(bytePolicy, null);
        using (ResolverGuardScope scope = byteGuard.BeginXmlResource("urn:bytes", null))
        {
            byteGuard.ObserveXmlResourceBytes(scope, 3);
            XmlResourceException bytes = Assert.Throws<XmlResourceException>(() => byteGuard.ObserveXmlResourceBytes(scope, 2))!;
            Assert.That(bytes.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        }

        XmlResolverPolicy workPolicy = AllowedPolicy();
        workPolicy.MaximumWorkUnits = 10;
        ResolverGuard workGuard = new ResolverGuard(workPolicy, null);
        using (ResolverGuardScope scope = workGuard.BeginXmlResource("urn:a", null))
        {
            workGuard.ConsumeXmlResourceWork(4);
            XmlResourceException work = Assert.Throws<XmlResourceException>(() => workGuard.ConsumeXmlResourceWork(1))!;
            Assert.That(work.Diagnostic!.Category, Is.EqualTo(XmlErrorCategory.Limit));
        }
    }

    [Test]
    public void ValidatesResolvedResourcesAndPropagatesMetadataAndOwnership()
    {
        ResolverGuard guard = new ResolverGuard(AllowedPolicy(), null);
        using (ResolverGuardScope scope = guard.BeginXmlResource("urn:data", null))
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = "data";
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = XmlResourceKind.Document;
            resource.Access = XmlResourceAccess.Allowed;
            resource.Input = input;
            resource.MediaType = "application/xml";
            resource.EncodingName = "UTF-8";
            resource.OwnsResource = true;

            guard.ValidateXmlResolvedResource(scope, XmlResourceKind.Document, resource);

            Assert.That(resource.RequestedUri, Is.EqualTo("urn:data"));
            Assert.That(resource.AbsoluteUri, Is.EqualTo("urn:data"));
            Assert.That(input.BaseUri, Is.EqualTo("urn:data"));
            Assert.That(input.MediaType, Is.EqualTo("application/xml"));
            Assert.That(input.EncodingName, Is.EqualTo("UTF-8"));
            Assert.That(input.OwnsResource, Is.True);
            Assert.That(scope.ResourceBytes, Is.EqualTo(4));
        }
    }

    [Test]
    public void RejectsWrongKindMissingInputAndFragmentResources()
    {
        ResolverGuard wrongKindGuard = new ResolverGuard(AllowedPolicy(), null);
        using (ResolverGuardScope scope = wrongKindGuard.BeginXmlResource("urn:data", null))
        {
            XmlResolvedResource wrongKind = Resource(XmlResourceKind.Schema, new XmlInput());
            Assert.Throws<XmlResourceException>(() => wrongKindGuard.ValidateXmlResolvedResource(scope, XmlResourceKind.Document, wrongKind));
        }

        ResolverGuard missingGuard = new ResolverGuard(AllowedPolicy(), null);
        using (ResolverGuardScope scope = missingGuard.BeginXmlResource("urn:data", null))
        {
            XmlResolvedResource missing = Resource(XmlResourceKind.Document, null);
            Assert.Throws<XmlResourceException>(() => missingGuard.ValidateXmlResolvedResource(scope, XmlResourceKind.Document, missing));
        }

        ResolverGuard fragmentGuard = new ResolverGuard(AllowedPolicy(), null);
        Assert.Throws<XmlResourceException>(() => fragmentGuard.BeginXmlResource("https://example.test/data.xml#part", null));
    }

    [Test]
    public void ValidatesPolicyLimitsAndAllowlistEntries()
    {
        XmlResolverPolicy negativeRedirects = AllowedPolicy();
        negativeRedirects.MaximumRedirects = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResolverGuard(negativeRedirects, null));

        XmlResolverPolicy negativeDepth = AllowedPolicy();
        negativeDepth.MaximumDepth = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResolverGuard(negativeDepth, null));

        XmlResolverPolicy emptyScheme = AllowedPolicy();
        emptyScheme.AllowedSchemes = [""];
        Assert.Throws<ArgumentException>(() => new ResolverGuard(emptyScheme, null).BeginXmlResource("https://example.test/data.xml", null));

        XmlResolverPolicy emptyRoot = AllowedPolicy();
        emptyRoot.AllowedSchemes = ["file"];
        emptyRoot.AllowedRoots = [""];
        Assert.Throws<ArgumentException>(() => new ResolverGuard(emptyRoot, null).BeginXmlResource("file:///data.xml", null));
    }

    [Test]
    public void AcceptsFileUriRootsAndRejectsAnEmptyRootAllowlist()
    {
        string directory = Path.Combine(Path.GetTempPath(), "xml-resolver-root");
        string child = Path.Combine(directory, "nested", "data.xml");
        XmlResolverPolicy allowed = AllowedPolicy();
        allowed.AllowedSchemes = ["file"];
        allowed.AllowedRoots = [new Uri(directory + Path.DirectorySeparatorChar).AbsoluteUri];
        using (ResolverGuardScope scope = new ResolverGuard(allowed, null).BeginXmlResource(new Uri(child).AbsoluteUri, null))
        {
            Assert.That(scope.CurrentUri.LocalPath, Is.EqualTo(child));
        }

        XmlResolverPolicy denied = AllowedPolicy();
        denied.AllowedSchemes = ["file"];
        denied.AllowedRoots = [];
        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => new ResolverGuard(denied, null).BeginXmlResource(new Uri(child).AbsoluteUri, null))!;
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceDenied));
    }

    [Test]
    public void DisposedScopesRestoreDepthAndRejectFurtherUse()
    {
        ResolverGuard guard = new ResolverGuard(AllowedPolicy(), null);
        ResolverGuardScope scope = guard.BeginXmlResource("urn:resource", null);

        scope.Dispose();
        scope.Dispose();

        Assert.That(guard.ActiveXmlResourceDepth(), Is.Zero);
        Assert.That(scope.Active, Is.False);
        Assert.Throws<InvalidOperationException>(() => guard.ObserveXmlResourceBytes(scope, 1));
        Assert.Throws<InvalidOperationException>(() => guard.FollowXmlResourceRedirect(scope, "urn:other"));
    }

    [Test]
    public void ResourceFailuresCarryExactSourceLocationsAndRanges()
    {
        XmlResolverPolicy schemePolicy = AllowedPolicy();
        schemePolicy.AllowedSchemes = ["https"];
        XmlResourceException scheme = Assert.Throws<XmlResourceException>(() => new ResolverGuard(schemePolicy, null).BeginXmlResource("http://example.test/data.xml", null))!;
        AssertResourceLocation(scheme, "http://example.test/data.xml");

        XmlResolverPolicy bytePolicy = AllowedPolicy();
        bytePolicy.MaximumResourceBytes = 1;
        ResolverGuard byteGuard = new ResolverGuard(bytePolicy, null);
        using (ResolverGuardScope scope = byteGuard.BeginXmlResource("urn:bytes", null))
        {
            XmlResourceException limit = Assert.Throws<XmlResourceException>(() => byteGuard.ObserveXmlResourceBytes(scope, 2))!;
            AssertResourceLocation(limit, "urn:bytes");
        }
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        policy.MaximumRedirects = 4;
        policy.MaximumDepth = 4;
        policy.MaximumResourceBytes = 1024;
        policy.MaximumWorkUnits = 4096;
        return policy;
    }

    private static XmlResolvedResource Resource(XmlResourceKind kind, XmlInput? input)
    {
        XmlResolvedResource resource = new XmlResolvedResource();
        resource.Kind = kind;
        resource.Access = XmlResourceAccess.Allowed;
        resource.Input = input;
        return resource;
    }

    private static void AssertResourceLocation(XmlResourceException failure, string expectedUri)
    {
        Assert.That(failure.Location!.SourceUri, Is.EqualTo(expectedUri));
        Assert.That(failure.Location.CharacterOffset, Is.Zero);
        Assert.That(failure.Location.ByteOffset, Is.EqualTo(-1));
        Assert.That(failure.Location.Line, Is.EqualTo(1));
        Assert.That(failure.Location.Column, Is.EqualTo(1));
        Assert.That(failure.Diagnostic!.Range!.Start, Is.SameAs(failure.Location));
        Assert.That(failure.Diagnostic.Range.End, Is.SameAs(failure.Location));
    }

    private sealed class RecordingResourceReporter : IXmlErrorReporter
    {
        public XmlDiagnostic? Diagnostic;

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }
}
