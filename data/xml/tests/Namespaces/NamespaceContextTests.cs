using xml;

namespace xml.Tests;

[TestFixture]
public class NamespaceContextTests
{
    [Test]
    public void ResolvesDefaultPrefixedAndImplicitXmlNamespaces()
    {
        NamespaceContext context = new NamespaceContext(null,
        [
            Binding("", "urn:default", false),
            Binding("p", "urn:prefixed", false)
        ]);

        Assert.That(context.DefaultXmlNamespaceUri(), Is.EqualTo("urn:default"));
        Assert.That(context.LookupXmlNamespaceUri(""), Is.EqualTo("urn:default"));
        Assert.That(context.LookupXmlNamespaceUri("p"), Is.EqualTo("urn:prefixed"));
        Assert.That(context.LookupXmlNamespaceUri("missing"), Is.Null);
        Assert.That(context.LookupXmlNamespaceUri("xml"), Is.EqualTo(XmlNamespaces.Xml));
        Assert.That(context.LookupXmlPrefix("urn:prefixed"), Is.EqualTo("p"));
        Assert.That(context.LookupXmlPrefix(XmlNamespaces.Xml), Is.EqualTo("xml"));
        Assert.That(context.LookupXmlPrefix("urn:missing"), Is.Null);
    }

    [Test]
    public void PushShadowsBindingsWithoutChangingItsParent()
    {
        NamespaceContext root = new NamespaceContext(null,
        [
            Binding("", "urn:root-default", false),
            Binding("p", "urn:root-p", false),
            Binding("shared", "urn:shared", false)
        ]);
        NamespaceContext child = root.PushXmlNamespaceFrame(
        [
            Binding("p", "urn:child-p", false),
            Binding("q", "urn:shared", false)
        ]);

        Assert.That(root.CurrentXmlNamespaceDepth(), Is.Zero);
        Assert.That(child.CurrentXmlNamespaceDepth(), Is.EqualTo(1));
        Assert.That(root.LookupXmlNamespaceUri("p"), Is.EqualTo("urn:root-p"));
        Assert.That(root.LookupXmlNamespaceUri("q"), Is.Null);
        Assert.That(child.DefaultXmlNamespaceUri(), Is.EqualTo("urn:root-default"));
        Assert.That(child.LookupXmlNamespaceUri("p"), Is.EqualTo("urn:child-p"));
        Assert.That(child.LookupXmlNamespaceUri("q"), Is.EqualTo("urn:shared"));
        Assert.That(child.LookupXmlPrefix("urn:shared"), Is.EqualTo("q"));
        Assert.That(child.PopXmlNamespaceFrame(), Is.SameAs(root));
    }

    [Test]
    public void UndeclarationsHideInheritedBindingsAndDefaultNamespace()
    {
        NamespaceContext root = new NamespaceContext(null,
        [
            Binding("", "urn:default", false),
            Binding("p", "urn:p", false),
            Binding("q", "urn:shared", false)
        ]);
        NamespaceContext child = root.PushXmlNamespaceFrame(
        [
            Binding("", "", true),
            Binding("p", "", true),
            Binding("local", "urn:local", false)
        ]);

        Assert.That(child.DefaultXmlNamespaceUri(), Is.Null);
        Assert.That(child.LookupXmlNamespaceUri("p"), Is.Null);
        Assert.That(child.LookupXmlNamespaceUri("q"), Is.EqualTo("urn:shared"));
        Assert.That(child.LookupXmlNamespaceUri("local"), Is.EqualTo("urn:local"));

        XmlNamespaceBinding[] inScope = child.EnumerateInScopeXmlNamespaces();
        Assert.That(Prefixes(inScope), Is.EqualTo(new string[] { "local", "q", "xml" }));
        Assert.That(NamespaceUris(inScope), Is.EqualTo(new string[] { "urn:local", "urn:shared", XmlNamespaces.Xml }));
    }

    [Test]
    public void EnumerationUsesNearestBindingsInDeterministicFrameOrder()
    {
        NamespaceContext root = new NamespaceContext(null,
        [
            Binding("", "urn:default", false),
            Binding("p", "urn:root-p", false),
            Binding("r", "urn:r", false)
        ]);
        NamespaceContext child = root.PushXmlNamespaceFrame(
        [
            Binding("p", "urn:child-p", false),
            Binding("q", "urn:q", false)
        ]);

        XmlNamespaceBinding[] bindings = child.EnumerateInScopeXmlNamespaces();

        Assert.That(Prefixes(bindings), Is.EqualTo(new string[] { "p", "q", "", "r", "xml" }));
        Assert.That(NamespaceUris(bindings), Is.EqualTo(new string[] { "urn:child-p", "urn:q", "urn:default", "urn:r", XmlNamespaces.Xml }));
    }

    [Test]
    public void ContextAndEnumerationAreDetachedFromMutableBindingBags()
    {
        XmlNamespaceBinding original = Binding("p", "urn:original", false);
        original.Range = RangeAt(4);
        XmlNamespaceBinding[] declarations = [original];
        NamespaceContext context = new NamespaceContext(null, declarations);

        original.Prefix = "changed";
        original.NamespaceUri = "urn:changed";
        original.Range.Start!.CharacterOffset = 99;
        declarations[0] = Binding("replacement", "urn:replacement", false);

        Assert.That(context.LookupXmlNamespaceUri("p"), Is.EqualTo("urn:original"));
        Assert.That(context.LookupXmlNamespaceUri("changed"), Is.Null);
        Assert.That(context.LookupXmlNamespaceUri("replacement"), Is.Null);

        XmlNamespaceBinding[] first = context.EnumerateInScopeXmlNamespaces();
        first[0].Prefix = "mutated";
        first[0].NamespaceUri = "urn:mutated";
        first[0].Range!.Start!.CharacterOffset = 100;
        XmlNamespaceBinding[] second = context.EnumerateInScopeXmlNamespaces();

        Assert.That(second[0].Prefix, Is.EqualTo("p"));
        Assert.That(second[0].NamespaceUri, Is.EqualTo("urn:original"));
        Assert.That(second[0].Range!.Start!.CharacterOffset, Is.EqualTo(4));
    }

    [Test]
    public void RejectsInvalidFrameAndLookupInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new NamespaceContext(null, null!));
        Assert.Throws<ArgumentException>(() => new NamespaceContext(null, new XmlNamespaceBinding[] { null! }));
        Assert.Throws<ArgumentException>(() => new NamespaceContext(null, [Binding(null, "urn:value", false)]));
        Assert.Throws<ArgumentException>(() => new NamespaceContext(null, [Binding("p", null, false)]));

        NamespaceContext root = new NamespaceContext(null, []);
        Assert.Throws<ArgumentNullException>(() => root.PushXmlNamespaceFrame(null!));
        Assert.Throws<InvalidOperationException>(() => root.PopXmlNamespaceFrame());
        Assert.Throws<ArgumentNullException>(() => root.LookupXmlNamespaceUri(null!));
        Assert.Throws<ArgumentNullException>(() => root.LookupXmlPrefix(null!));
    }

    private static XmlNamespaceBinding Binding(string? prefix, string? namespaceUri, bool undeclaration)
    {
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceUri;
        binding.IsUndeclaration = undeclaration;
        return binding;
    }

    private static SourceRange RangeAt(long characterOffset)
    {
        SourceLocation location = new SourceLocation();
        location.CharacterOffset = characterOffset;
        SourceRange range = new SourceRange();
        range.Start = location;
        range.End = location;
        return range;
    }

    private static string[] Prefixes(XmlNamespaceBinding[] bindings)
    {
        string[] prefixes = new string[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            string? prefix = bindings[index].Prefix;
            prefixes[index] = prefix == null ? "" : prefix;
            index = index + 1;
        }

        return prefixes;
    }

    private static string[] NamespaceUris(XmlNamespaceBinding[] bindings)
    {
        string[] namespaceUris = new string[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            string? namespaceUri = bindings[index].NamespaceUri;
            namespaceUris[index] = namespaceUri == null ? "" : namespaceUri;
            index = index + 1;
        }

        return namespaceUris;
    }
}
