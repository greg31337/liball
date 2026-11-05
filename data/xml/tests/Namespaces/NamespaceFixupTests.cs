using xml;

namespace xml.Tests;

[TestFixture]
public class NamespaceFixupTests
{
    [Test]
    public void PreservesCallerPrefixesWhenTheirBindingsAreLegal()
    {
        XmlNode element = Element(Name("p", "root", "urn:p"));
        element.NamespaceBindings =
        [
            Binding("p", "urn:p", false),
            Binding("a", "urn:a", false)
        ];
        element.Attributes = [Attribute(Name("a", "id", "urn:a"))];

        Fixup(element, XmlNamespaceHandling.Namespaces10, Context());

        AssertName(element.Name!, "p", "root", "urn:p", "p:root");
        AssertName(element.Attributes![0].Name!, "a", "id", "urn:a", "a:id");
        AssertBindings(element.NamespaceBindings!, new string[] { "p", "a" }, new string[] { "urn:p", "urn:a" });
    }

    [Test]
    public void UsesTheDefaultNamespaceForElementsAndGeneratesPrefixesForAttributes()
    {
        XmlNode element = Element(Name(null, "root", "urn:element"));
        element.Attributes =
        [
            Attribute(Name(null, "first", "urn:attribute")),
            Attribute(Name(null, "second", "urn:attribute"))
        ];

        Fixup(element, XmlNamespaceHandling.Namespaces11, Context());

        AssertName(element.Name!, null, "root", "urn:element", "root");
        AssertName(element.Attributes![0].Name!, "ns1", "first", "urn:attribute", "ns1:first");
        AssertName(element.Attributes[1].Name!, "ns1", "second", "urn:attribute", "ns1:second");
        AssertBindings(element.NamespaceBindings!, new string[] { "", "ns1" }, new string[] { "urn:element", "urn:attribute" });
    }

    [Test]
    public void GeneratesAPrefixInsteadOfOverwritingAConflictingLocalBinding()
    {
        XmlNode element = Element(Name("p", "root", "urn:element"));
        element.NamespaceBindings = [Binding("p", "urn:declared", false)];

        Fixup(element, XmlNamespaceHandling.Namespaces11, Context());

        AssertName(element.Name!, "ns1", "root", "urn:element", "ns1:root");
        AssertBindings(element.NamespaceBindings!, new string[] { "p", "ns1" }, new string[] { "urn:declared", "urn:element" });
    }

    [Test]
    public void RedeclaresAnInheritedConflictToPreserveACallerPrefix()
    {
        NamespaceContext inherited = Context(Binding("p", "urn:inherited", false));
        XmlNode element = Element(Name("p", "root", "urn:element"));

        Fixup(element, XmlNamespaceHandling.Namespaces11, inherited);

        AssertName(element.Name!, "p", "root", "urn:element", "p:root");
        AssertBindings(element.NamespaceBindings!, new string[] { "p" }, new string[] { "urn:element" });
        Assert.That(inherited.LookupXmlNamespaceUri("p"), Is.EqualTo("urn:inherited"));
    }

    [Test]
    public void RepairsAConflictingAttributePrefixWithoutInvalidatingTheElement()
    {
        XmlNode element = Element(Name("p", "root", "urn:element"));
        element.Attributes = [Attribute(Name("p", "value", "urn:attribute"))];

        Fixup(element, XmlNamespaceHandling.Namespaces11, Context());

        AssertName(element.Name!, "p", "root", "urn:element", "p:root");
        AssertName(element.Attributes![0].Name!, "ns1", "value", "urn:attribute", "ns1:value");
        AssertBindings(element.NamespaceBindings!, new string[] { "p", "ns1" }, new string[] { "urn:element", "urn:attribute" });
    }

    [Test]
    public void SkipsOccupiedGeneratedPrefixesDeterministically()
    {
        XmlNode element = Element(Name(null, "root", null));
        element.NamespaceBindings =
        [
            Binding("ns1", "urn:one", false),
            Binding("ns2", "urn:two", false)
        ];
        element.Attributes = [Attribute(Name(null, "value", "urn:new"))];

        Fixup(element, XmlNamespaceHandling.Namespaces11, Context());

        AssertName(element.Attributes![0].Name!, "ns3", "value", "urn:new", "ns3:value");
        AssertBindings(element.NamespaceBindings!, new string[] { "ns1", "ns2", "ns3" }, new string[] { "urn:one", "urn:two", "urn:new" });
    }

    [Test]
    public void UsesTheReservedXmlPrefixWithoutAddingADeclaration()
    {
        XmlNode element = Element(Name("wrong", "root", XmlNamespaces.Xml));
        element.Attributes = [Attribute(Name(null, "lang", XmlNamespaces.Xml))];

        Fixup(element, XmlNamespaceHandling.Namespaces10, Context());

        AssertName(element.Name!, "xml", "root", XmlNamespaces.Xml, "xml:root");
        AssertName(element.Attributes![0].Name!, "xml", "lang", XmlNamespaces.Xml, "xml:lang");
        Assert.That(element.NamespaceBindings, Is.Empty);
    }

    [Test]
    public void AddsADefaultUndeclarationForANoNamespaceElement()
    {
        XmlNode element = Element(Name(null, "child", null));
        NamespaceContext inherited = Context(Binding("", "urn:inherited", false));

        Fixup(element, XmlNamespaceHandling.Namespaces10, inherited);

        AssertName(element.Name!, null, "child", null, "child");
        Assert.That(element.NamespaceBindings, Has.Length.EqualTo(1));
        Assert.That(element.NamespaceBindings![0].Prefix, Is.EqualTo(""));
        Assert.That(element.NamespaceBindings[0].NamespaceUri, Is.EqualTo(""));
        Assert.That(element.NamespaceBindings[0].IsUndeclaration, Is.True);
    }

    [Test]
    public void ReusesGeneratedBindingsAcrossDescendantsAndAllocatesNewOnesInTraversalOrder()
    {
        XmlNode root = Element(Name(null, "root", null));
        root.Attributes = [Attribute(Name(null, "first", "urn:one"))];
        XmlNode child = Element(Name(null, "child", null));
        child.Attributes =
        [
            Attribute(Name(null, "same", "urn:one")),
            Attribute(Name(null, "other", "urn:two"))
        ];
        root.Children = [child];
        child.Parent = root;

        Fixup(root, XmlNamespaceHandling.Namespaces11, Context());

        AssertName(root.Attributes![0].Name!, "ns1", "first", "urn:one", "ns1:first");
        AssertName(child.Attributes![0].Name!, "ns1", "same", "urn:one", "ns1:same");
        AssertName(child.Attributes[1].Name!, "ns2", "other", "urn:two", "ns2:other");
        AssertBindings(root.NamespaceBindings!, new string[] { "ns1" }, new string[] { "urn:one" });
        AssertBindings(child.NamespaceBindings!, new string[] { "ns2" }, new string[] { "urn:two" });
    }

    [Test]
    public void ProducesTheSameNamesAndBindingsWhenFixupRunsAgain()
    {
        XmlNode element = Element(Name(null, "root", "urn:element"));
        element.Attributes = [Attribute(Name(null, "value", "urn:attribute"))];
        NamespaceFixup fixup = new NamespaceFixup(XmlNamespaceHandling.Namespaces11, null);

        fixup.FixupXmlNamespaceSubtree(element, Context());
        string firstElementName = element.Name!.QualifiedName!;
        string firstAttributeName = element.Attributes![0].Name!.QualifiedName!;
        string[] firstPrefixes = BindingPrefixes(element.NamespaceBindings!);
        string[] firstUris = BindingUris(element.NamespaceBindings!);
        fixup.FixupXmlNamespaceSubtree(element, Context());

        Assert.That(element.Name!.QualifiedName, Is.EqualTo(firstElementName));
        Assert.That(element.Attributes![0].Name!.QualifiedName, Is.EqualTo(firstAttributeName));
        Assert.That(BindingPrefixes(element.NamespaceBindings!), Is.EqualTo(firstPrefixes));
        Assert.That(BindingUris(element.NamespaceBindings!), Is.EqualTo(firstUris));
    }

    [Test]
    public void PreservesLegalXmlLikeCallerPrefixes()
    {
        XmlNode element = Element(Name("XMLtools", "root", "urn:tools"));

        Fixup(element, XmlNamespaceHandling.Namespaces11, Context());

        AssertName(element.Name!, "XMLtools", "root", "urn:tools", "XMLtools:root");
        AssertBindings(element.NamespaceBindings!, new string[] { "XMLtools" }, new string[] { "urn:tools" });
    }

    [Test]
    public void RejectsNamespaceDeclarationNamespaceOnOrdinaryNames()
    {
        RecordingReporter reporter = new RecordingReporter();
        NamespaceFixup fixup = new NamespaceFixup(XmlNamespaceHandling.Namespaces11, reporter);
        XmlNode element = Element(Name(null, "root", XmlNamespaces.Xmlns));

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => fixup.FixupXmlNamespaceSubtree(element, Context()))!;

        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.NamespaceReservedBinding));
    }

    [Test]
    public void RejectsVersionInvalidExistingBindings()
    {
        XmlNode element = Element(Name(null, "root", null));
        element.NamespaceBindings = [Binding("p", "", true)];

        Assert.Throws<XmlNamespaceException>(() => Fixup(element, XmlNamespaceHandling.Namespaces10, Context()));

        Assert.That(element.Name!.QualifiedName, Is.Null);
        Assert.That(element.NamespaceBindings![0].Prefix, Is.EqualTo("p"));
    }

    [Test]
    public void RejectsDuplicateExpandedAttributesBeforeApplyingNameChanges()
    {
        XmlNode element = Element(Name(null, "root", null));
        element.NamespaceBindings =
        [
            Binding("p", "urn:shared", false),
            Binding("q", "urn:shared", false)
        ];
        XmlNode first = Attribute(Name("p", "id", "urn:shared"));
        XmlNode second = Attribute(Name("q", "id", "urn:shared"));
        element.Attributes = [first, second];

        Assert.Throws<XmlNamespaceException>(() => Fixup(element, XmlNamespaceHandling.Namespaces11, Context()));

        Assert.That(first.Name!.QualifiedName, Is.Null);
        Assert.That(second.Name!.QualifiedName, Is.Null);
    }

    [Test]
    public void RejectsInvalidConstructionAndTreeInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NamespaceFixup(XmlNamespaceHandling.Disabled, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NamespaceFixup((XmlNamespaceHandling)99, null));

        NamespaceFixup fixup = new NamespaceFixup(XmlNamespaceHandling.Namespaces10, null);
        Assert.Throws<ArgumentNullException>(() => fixup.FixupXmlNamespaceSubtree(null!, Context()));
        Assert.Throws<ArgumentNullException>(() => fixup.FixupXmlNamespaceSubtree(Element(Name(null, "root", null)), null!));
        Assert.Throws<ArgumentException>(() => fixup.FixupXmlElementNamespaces(new XmlNode(), Context()));

        XmlNode unnamed = new XmlNode();
        unnamed.Kind = XmlNodeKind.Element;
        Assert.Throws<ArgumentException>(() => fixup.FixupXmlElementNamespaces(unnamed, Context()));
    }

    private static void Fixup(XmlNode node, XmlNamespaceHandling handling, NamespaceContext inherited)
    {
        new NamespaceFixup(handling, null).FixupXmlNamespaceSubtree(node, inherited);
    }

    private static NamespaceContext Context(params XmlNamespaceBinding[] bindings)
    {
        return new NamespaceContext(null, bindings);
    }

    private static XmlNode Element(XmlName name)
    {
        XmlNode node = new XmlNode();
        node.Kind = XmlNodeKind.Element;
        node.Name = name;
        node.Children = [];
        node.Attributes = [];
        node.NamespaceBindings = [];
        return node;
    }

    private static XmlNode Attribute(XmlName name)
    {
        XmlNode node = new XmlNode();
        node.Kind = XmlNodeKind.Attribute;
        node.Name = name;
        return node;
    }

    private static XmlName Name(string? prefix, string localName, string? namespaceUri)
    {
        XmlName name = new XmlName();
        name.Prefix = prefix;
        name.LocalName = localName;
        name.NamespaceUri = namespaceUri;
        return name;
    }

    private static XmlNamespaceBinding Binding(string prefix, string namespaceUri, bool undeclaration)
    {
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceUri;
        binding.IsUndeclaration = undeclaration;
        return binding;
    }

    private static void AssertName(XmlName name, string? prefix, string localName, string? namespaceUri, string qualifiedName)
    {
        Assert.That(name.Prefix, Is.EqualTo(prefix));
        Assert.That(name.LocalName, Is.EqualTo(localName));
        Assert.That(name.NamespaceUri, Is.EqualTo(namespaceUri));
        Assert.That(name.QualifiedName, Is.EqualTo(qualifiedName));
    }

    private static void AssertBindings(XmlNamespaceBinding[] bindings, string[] prefixes, string[] namespaceUris)
    {
        Assert.That(BindingPrefixes(bindings), Is.EqualTo(prefixes));
        Assert.That(BindingUris(bindings), Is.EqualTo(namespaceUris));
    }

    private static string[] BindingPrefixes(XmlNamespaceBinding[] bindings)
    {
        string[] prefixes = new string[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            prefixes[index] = bindings[index].Prefix!;
            index = index + 1;
        }

        return prefixes;
    }

    private static string[] BindingUris(XmlNamespaceBinding[] bindings)
    {
        string[] namespaceUris = new string[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            namespaceUris[index] = bindings[index].NamespaceUri!;
            index = index + 1;
        }

        return namespaceUris;
    }

    private sealed class RecordingReporter : IXmlErrorReporter
    {
        internal XmlDiagnostic? Diagnostic;

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }
}
