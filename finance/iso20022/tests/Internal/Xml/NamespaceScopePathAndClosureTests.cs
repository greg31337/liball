using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class NamespaceScopePathAndClosureTests
{
    [Test]
    public void NestedScopesShadowRestoreAndSnapshotEffectiveDeclarations()
    {
        NamespaceScopeTracker tracker = new NamespaceScopeTracker();

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetScopeDepth(), Is.Zero);
            Assert.That(tracker.ResolvePrefix(string.Empty), Is.Empty);
            Assert.That(tracker.ResolvePrefix("missing"), Is.Null);
            Assert.That(tracker.ResolvePrefix("xml"), Is.EqualTo(Iso20022Namespaces.Xml));
            Assert.That(tracker.ResolvePrefix("xmlns"), Is.EqualTo(Iso20022Namespaces.Xmlns));
        });

        tracker.EnterScope();
        tracker.DeclareNamespace(string.Empty, "urn:outer:default");
        tracker.DeclareNamespace("p", "urn:outer:p");
        tracker.DeclareNamespace("q", "urn:outer:q");
        tracker.EnterScope();
        tracker.DeclareNamespace("p", "urn:inner:p");
        tracker.DeclareNamespace(string.Empty, string.Empty);

        List<NamespaceDeclarationData> inner = tracker.CaptureDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetScopeDepth(), Is.EqualTo(2));
            Assert.That(tracker.ResolvePrefix(string.Empty), Is.Empty);
            Assert.That(tracker.ResolvePrefix("p"), Is.EqualTo("urn:inner:p"));
            Assert.That(tracker.ResolvePrefix("q"), Is.EqualTo("urn:outer:q"));
            Assert.That(inner.Select(value => value.Prefix), Is.EqualTo(new[] { string.Empty, "p", "q" }));
            Assert.That(inner.Select(value => value.NamespaceUri), Is.EqualTo(new[] { string.Empty, "urn:inner:p", "urn:outer:q" }));
        });

        inner[0].NamespaceUri = "urn:mutated:snapshot";
        Assert.That(tracker.ResolvePrefix("p"), Is.EqualTo("urn:inner:p"));

        tracker.ExitScope();
        Assert.Multiple(() =>
        {
            Assert.That(tracker.ResolvePrefix(string.Empty), Is.EqualTo("urn:outer:default"));
            Assert.That(tracker.ResolvePrefix("p"), Is.EqualTo("urn:outer:p"));
            Assert.That(tracker.CaptureDeclarations().Select(value => value.Prefix), Is.EqualTo(new[] { string.Empty, "p", "q" }));
        });

        tracker.ExitScope();
        Assert.That(tracker.ResolvePrefix("p"), Is.Null);
        Assert.Throws<InvalidOperationException>(() => tracker.ExitScope());
    }

    [TestCase("bad:name", "urn:test")]
    [TestCase("xmlns", "urn:test")]
    [TestCase("p", "http://www.w3.org/2000/xmlns/")]
    [TestCase("xml", "urn:not-xml")]
    [TestCase("p", "http://www.w3.org/XML/1998/namespace")]
    [TestCase("p", "")]
    [TestCase("p", "namespace with spaces")]
    public void InvalidAndReservedNamespaceBindingsAreRejected(string prefix, string namespaceUri)
    {
        NamespaceScopeTracker tracker = new NamespaceScopeTracker();
        tracker.EnterScope();

        Assert.Throws<XmlException>(() => tracker.DeclareNamespace(prefix, namespaceUri));
    }

    [Test]
    public void DuplicatePrefixesAreRejectedWithinOneScopeButAllowedToShadowAncestors()
    {
        NamespaceScopeTracker tracker = new NamespaceScopeTracker();
        tracker.EnterScope();
        tracker.DeclareNamespace("p", "urn:first");

        Assert.Throws<XmlException>(() => tracker.DeclareNamespace("p", "urn:duplicate"));

        tracker.EnterScope();
        Assert.DoesNotThrow(() => tracker.DeclareNamespace("p", "urn:shadow"));
        Assert.That(tracker.ResolvePrefix("p"), Is.EqualTo("urn:shadow"));
    }

    [Test]
    public void RegisteredXmlBindingIsAcceptedAndCapturedOnlyWhenExplicit()
    {
        NamespaceScopeTracker tracker = new NamespaceScopeTracker();
        tracker.EnterScope();
        tracker.DeclareNamespace("xml", Iso20022Namespaces.Xml);

        List<NamespaceDeclarationData> declarations = tracker.CaptureDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(declarations, Has.Count.EqualTo(1));
            Assert.That(declarations[0].Prefix, Is.EqualTo("xml"));
            Assert.That(declarations[0].NamespaceUri, Is.EqualTo(Iso20022Namespaces.Xml));
        });
    }

    [Test]
    public void RawFragmentClosureCapturesEffectiveBindingsWithoutSharingMutableState()
    {
        NamespaceScopeTracker tracker = new NamespaceScopeTracker();
        tracker.EnterScope();
        tracker.DeclareNamespace(string.Empty, "urn:default");
        tracker.DeclareNamespace("p", "urn:outer");
        tracker.DeclareNamespace("unused", "urn:unused");
        tracker.EnterScope();
        tracker.DeclareNamespace("p", "urn:fragment");
        RawXmlFragment fragment = new RawXmlFragment
        {
            Xml = "<p:Extension><Item/></p:Extension>",
            RootName = new QualifiedNameData { Prefix = "p", LocalName = "Extension", NamespaceUri = "urn:fragment" },
            NamespaceDeclarations = new List<NamespaceDeclarationData> { new NamespaceDeclarationData { Prefix = "old", NamespaceUri = "urn:old" } }
        };

        RawFragmentNamespaceClosure.ApplyNamespaceClosure(fragment, tracker);

        Assert.Multiple(() =>
        {
            Assert.That(fragment.Xml, Is.EqualTo("<p:Extension><Item/></p:Extension>"));
            Assert.That(fragment.NamespaceDeclarations.Select(value => value.Prefix), Is.EqualTo(new[] { string.Empty, "p", "unused" }));
            Assert.That(fragment.NamespaceDeclarations.Select(value => value.NamespaceUri), Is.EqualTo(new[] { "urn:default", "urn:fragment", "urn:unused" }));
        });

        tracker.ExitScope();
        tracker.EnterScope();
        tracker.DeclareNamespace("p", "urn:later");
        Assert.That(fragment.NamespaceDeclarations.Single(value => value.Prefix == "p").NamespaceUri, Is.EqualTo("urn:fragment"));

        fragment.NamespaceDeclarations[0].NamespaceUri = "urn:changed-fragment";
        Assert.That(tracker.ResolvePrefix(string.Empty), Is.EqualTo("urn:default"));
    }

    [Test]
    public void RawFragmentClosureRejectsANameThatIsNotBoundByTheActiveScope()
    {
        NamespaceScopeTracker tracker = new NamespaceScopeTracker();
        tracker.EnterScope();
        tracker.DeclareNamespace("p", "urn:active");
        RawXmlFragment fragment = new RawXmlFragment
        {
            RootName = new QualifiedNameData { Prefix = "p", LocalName = "Extension", NamespaceUri = "urn:different" }
        };

        Assert.Throws<XmlException>(() => RawFragmentNamespaceClosure.ApplyNamespaceClosure(fragment, tracker));
    }

    [Test]
    public void RawFragmentClosurePreservesAnExplicitDefaultNamespaceUndeclaration()
    {
        NamespaceScopeTracker tracker = new NamespaceScopeTracker();
        tracker.EnterScope();
        tracker.DeclareNamespace(string.Empty, "urn:outer-default");
        tracker.EnterScope();
        tracker.DeclareNamespace(string.Empty, string.Empty);
        RawXmlFragment fragment = new RawXmlFragment
        {
            Xml = "<Extension xmlns=''/>",
            RootName = new QualifiedNameData { LocalName = "Extension" }
        };

        RawFragmentNamespaceClosure.ApplyNamespaceClosure(fragment, tracker);

        Assert.Multiple(() =>
        {
            Assert.That(fragment.NamespaceDeclarations, Has.Count.EqualTo(1));
            Assert.That(fragment.NamespaceDeclarations[0].Prefix, Is.Empty);
            Assert.That(fragment.NamespaceDeclarations[0].NamespaceUri, Is.Empty);
        });
    }

    [Test]
    public void QualifiedPathsArePrefixIndependentAndSnapshotsDoNotShareCallerObjects()
    {
        XmlPathTracker first = new XmlPathTracker();
        QualifiedNameData root = new QualifiedNameData { Prefix = "a", LocalName = "Document", NamespaceUri = "urn:message" };
        SourceLocation rootLocation = new SourceLocation
        {
            SourceName = "message.xml",
            HasLineInformation = true,
            LineNumber = 1,
            ColumnNumber = 2,
            HasCharacterOffset = true,
            CharacterOffset = 1
        };
        first.EnterElement(root, rootLocation);
        first.EnterElement(
            new QualifiedNameData { Prefix = "a", LocalName = "Payment", NamespaceUri = "urn:message" },
            new SourceLocation { SourceName = "message.xml", HasLineInformation = true, LineNumber = 2, ColumnNumber = 4 });
        root.Prefix = "mutated";
        root.LocalName = "Mutated";
        rootLocation.LineNumber = 99;

        XmlPathData firstPath = first.CapturePath();
        SourceLocation? firstLocation = first.CaptureCurrentLocation();
        XmlPathTracker second = new XmlPathTracker();
        second.EnterElement(
            new QualifiedNameData { Prefix = "different", LocalName = "Document", NamespaceUri = "urn:message" },
            new SourceLocation());
        second.EnterElement(
            new QualifiedNameData { Prefix = "other", LocalName = "Payment", NamespaceUri = "urn:message" },
            new SourceLocation());
        XmlPathData secondPath = second.CapturePath();

        Assert.Multiple(() =>
        {
            Assert.That(first.GetPathDepth(), Is.EqualTo(2));
            Assert.That(firstPath.Value, Is.EqualTo("/Q{urn:message}Document/Q{urn:message}Payment"));
            Assert.That(secondPath.Value, Is.EqualTo(firstPath.Value));
            Assert.That(firstPath.Segments[0].Prefix, Is.EqualTo("a"));
            Assert.That(firstPath.Segments[0].LocalName, Is.EqualTo("Document"));
            Assert.That(firstLocation?.LineNumber, Is.EqualTo(2));
            Assert.That(firstLocation?.ColumnNumber, Is.EqualTo(4));
        });

        firstPath.Segments[0].LocalName = "ChangedSnapshot";
        firstLocation!.LineNumber = 88;
        Assert.That(first.CapturePath().Segments[0].LocalName, Is.EqualTo("Document"));
        Assert.That(first.CaptureCurrentLocation()?.LineNumber, Is.EqualTo(2));

        first.ExitElement();
        Assert.That(first.CaptureCurrentLocation()?.LineNumber, Is.EqualTo(1));
        first.ExitElement();
        Assert.That(first.CaptureCurrentLocation(), Is.Null);
        Assert.Throws<InvalidOperationException>(() => first.ExitElement());
    }

    [Test]
    public void BoundedReaderProvidesQualifiedPathsAndRealLineLocations()
    {
        string xml = "<p:root xmlns:p='urn:test'>\n  <p:child/>\n</p:root>";
        BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromText(xml, new MessageReadOptions());
        XmlPathTracker tracker = new XmlPathTracker();
        XmlPathData? childPath = null;
        SourceLocation? childLocation = null;

        try
        {
            while (reader.ReadNext())
            {
                if (reader.GetNodeType() == XmlNodeType.Element)
                {
                    tracker.EnterReaderElement(reader, "message.xml");
                    if (reader.GetLocalName() == "child")
                    {
                        childPath = tracker.CapturePath();
                        childLocation = tracker.CaptureCurrentLocation();
                    }

                    if (reader.GetIsEmptyElement())
                    {
                        tracker.ExitElement();
                    }
                }
                else if (reader.GetNodeType() == XmlNodeType.EndElement)
                {
                    tracker.ExitElement();
                }
            }
        }
        finally
        {
            reader.DisposeReader();
        }

        Assert.Multiple(() =>
        {
            Assert.That(childPath?.Value, Is.EqualTo("/Q{urn:test}root/Q{urn:test}child"));
            Assert.That(childLocation?.SourceName, Is.EqualTo("message.xml"));
            Assert.That(childLocation?.HasLineInformation, Is.True);
            Assert.That(childLocation?.LineNumber, Is.EqualTo(2));
            Assert.That(childLocation?.ColumnNumber, Is.GreaterThan(0));
            Assert.That(tracker.GetPathDepth(), Is.Zero);
        });
    }

    [TestCase("", "", "bad:name")]
    [TestCase("p", "", "Element")]
    [TestCase("p", "namespace with spaces", "Element")]
    public void InvalidQualifiedPathSegmentsAreRejected(string prefix, string namespaceUri, string localName)
    {
        XmlPathTracker tracker = new XmlPathTracker();
        QualifiedNameData name = new QualifiedNameData { Prefix = prefix, NamespaceUri = namespaceUri, LocalName = localName };

        Assert.Throws<XmlException>(() => tracker.EnterElement(name, new SourceLocation()));
    }
}
