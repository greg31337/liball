using xml;

namespace xml.Tests;

[TestFixture]
public class EntityExpanderTests
{
    [TestCase("&amp;", "&")]
    [TestCase("&lt;", "<")]
    [TestCase("&gt;", ">")]
    [TestCase("&apos;", "'")]
    [TestCase("&quot;", "\"")]
    public void ExpandsEveryPredefinedEntityWithoutADeclaration(string reference, string expected)
    {
        EntityExpander expander = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, null);

        XmlEntityDispatchResult result = expander.DispatchXmlReference(ScanReference(reference, XmlVersion.Xml10));

        Assert.That(result.Resolved, Is.True);
        Assert.That(result.Skipped, Is.False);
        Assert.That(result.Items, Has.Length.EqualTo(1));
        Assert.That(result.Items[0].Kind, Is.EqualTo(XmlEntityDispatchKind.Character));
        Assert.That(result.Items[0].Name, Is.EqualTo(reference.Substring(1, reference.Length - 2)));
        Assert.That(result.Items[0].Value, Is.EqualTo(expected));
        Assert.That(result.Items[0].ScalarValue, Is.EqualTo(expected[0]));
    }

    [Test]
    public void PreservesNamedEntityBoundariesWhenRequested()
    {
        DictionaryEntitySource source = new DictionaryEntitySource();
        source.AddDeclaration(CreateDeclaration("chapter", "<chapter/>", true, false, null));
        EntityExpander expander = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.PreserveReferences, false, source);

        XmlEntityDispatchResult predefined = expander.DispatchXmlReference(ScanReference("&amp;", XmlVersion.Xml10));
        XmlEntityDispatchResult general = expander.DispatchXmlReference(ScanReference("&chapter;", XmlVersion.Xml10));

        Assert.That(predefined.Items.Select(item => item.Kind), Is.EqualTo(new XmlEntityDispatchKind[]
        {
            XmlEntityDispatchKind.StartEntity,
            XmlEntityDispatchKind.Character,
            XmlEntityDispatchKind.EndEntity,
        }));
        Assert.That(predefined.Items.Select(item => item.Name), Is.EqualTo(new string?[] { "amp", "amp", "amp" }));
        Assert.That(general.Items.Select(item => item.Kind), Is.EqualTo(new XmlEntityDispatchKind[]
        {
            XmlEntityDispatchKind.StartEntity,
            XmlEntityDispatchKind.ReplacementText,
            XmlEntityDispatchKind.EndEntity,
        }));
        Assert.That(general.Items.Select(item => item.Name), Is.EqualTo(new string?[] { "chapter", "chapter", "chapter" }));
        Assert.That(general.Items[1].Value, Is.EqualTo("<chapter/>"));
    }

    [TestCase("&#65;", XmlVersion.Xml10, 65, "A", false)]
    [TestCase("&#x1F600;", XmlVersion.Xml10, 0x1F600, "\U0001F600", false)]
    [TestCase("&#x1;", XmlVersion.Xml11, 1, "\u0001", true)]
    [TestCase("&#x7F;", XmlVersion.Xml11, 0x7F, "\u007F", true)]
    public void DispatchesValidatedNumericCharacterReferences(string reference, XmlVersion version, int scalar, string expected, bool restricted)
    {
        EntityExpander expander = CreateExpander(version, XmlEntityHandling.PreserveReferences, false, null);

        XmlEntityDispatchResult result = expander.DispatchXmlReference(ScanReference(reference, version));

        Assert.That(result.Items, Has.Length.EqualTo(1));
        Assert.That(result.Items[0].Kind, Is.EqualTo(XmlEntityDispatchKind.Character));
        Assert.That(result.Items[0].Name, Is.Null);
        Assert.That(result.Items[0].Value, Is.EqualTo(expected));
        Assert.That(result.Items[0].ScalarValue, Is.EqualTo(scalar));
        Assert.That(result.Items[0].RestrictedCharacter, Is.EqualTo(restricted));
    }

    [Test]
    public void ResolvesGeneralEntityReplacementSourceInExpandMode()
    {
        DictionaryEntitySource source = new DictionaryEntitySource();
        source.AddDeclaration(CreateDeclaration("content", "<child/>text&amp;", true, false, null));
        EntityExpander expander = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, source);

        XmlEntityDispatchResult result = expander.DispatchXmlReference(ScanReference("&content;", XmlVersion.Xml10));

        Assert.That(source.RequestedNames, Is.EqualTo(new string[] { "content" }));
        Assert.That(result.Resolved, Is.True);
        Assert.That(result.Skipped, Is.False);
        Assert.That(result.Items, Has.Length.EqualTo(2));
        Assert.That(result.Items[0].Kind, Is.EqualTo(XmlEntityDispatchKind.ReplacementText));
        Assert.That(result.Items[0].Name, Is.EqualTo("content"));
        Assert.That(result.Items[0].Value, Is.EqualTo("<child/>text"));
        Assert.That(result.Items[1].Kind, Is.EqualTo(XmlEntityDispatchKind.Character));
        Assert.That(result.Items[1].Value, Is.EqualTo("&"));
    }

    [Test]
    public void EmptyEntityReplacementStillHasPreservedBoundaries()
    {
        DictionaryEntitySource source = new DictionaryEntitySource();
        source.AddDeclaration(CreateDeclaration("empty", "", true, false, null));

        EntityExpander expand = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, source);
        EntityExpander preserve = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.PreserveReferences, false, source);

        Assert.That(expand.DispatchXmlReference(ScanReference("&empty;", XmlVersion.Xml10)).Items, Is.Empty);
        Assert.That(preserve.DispatchXmlReference(ScanReference("&empty;", XmlVersion.Xml10)).Items.Select(item => item.Kind), Is.EqualTo(new XmlEntityDispatchKind[]
        {
            XmlEntityDispatchKind.StartEntity,
            XmlEntityDispatchKind.EndEntity,
        }));
    }

    [Test]
    public void CanReportAnUnresolvedEntityAsSkipped()
    {
        EntityExpander expander = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, true, null);

        XmlEntityDispatchResult result = expander.DispatchXmlReference(ScanReference("&missing;", XmlVersion.Xml10));

        Assert.That(result.Resolved, Is.False);
        Assert.That(result.Skipped, Is.True);
        Assert.That(result.Items, Has.Length.EqualTo(1));
        Assert.That(result.Items[0].Kind, Is.EqualTo(XmlEntityDispatchKind.SkippedEntity));
        Assert.That(result.Items[0].Name, Is.EqualTo("missing"));
        Assert.That(result.Items[0].Value, Is.Null);
    }

    [Test]
    public void StrictModeRejectsUndeclaredAndUnavailableEntities()
    {
        DictionaryEntitySource source = new DictionaryEntitySource();
        source.AddDeclaration(CreateDeclaration("external", null, true, true, null));
        EntityExpander expander = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, source);

        Assert.Throws<FormatException>(() => expander.DispatchXmlReference(ScanReference("&missing;", XmlVersion.Xml10)));
        Assert.Throws<FormatException>(() => expander.DispatchXmlReference(ScanReference("&external;", XmlVersion.Xml10)));
    }

    [Test]
    public void SkippingUnavailableEntitiesDoesNotPermitUnparsedOrParameterEntities()
    {
        DictionaryEntitySource source = new DictionaryEntitySource();
        source.AddDeclaration(CreateDeclaration("unparsed", null, false, true, "jpeg"));
        XmlEntityDeclaration parameter = CreateDeclaration("parameter", "value", true, false, null);
        parameter.IsParameter = true;
        source.AddDeclaration(parameter);
        EntityExpander expander = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, true, source);

        Assert.Throws<FormatException>(() => expander.DispatchXmlReference(ScanReference("&unparsed;", XmlVersion.Xml10)));
        Assert.Throws<FormatException>(() => expander.DispatchXmlReference(ScanReference("&parameter;", XmlVersion.Xml10)));
    }

    [Test]
    public void RejectsAResolvedDeclarationWhoseNameDoesNotMatch()
    {
        FixedEntitySource source = new FixedEntitySource(CreateDeclaration("different", "value", true, false, null));
        EntityExpander expander = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, source);

        Assert.Throws<FormatException>(() => expander.DispatchXmlReference(ScanReference("&requested;", XmlVersion.Xml10)));
    }

    [TestCase("bad\0text", XmlVersion.Xml10)]
    [TestCase("bad\u007Ftext", XmlVersion.Xml11)]
    public void RejectsIllegalDirectCharactersInGeneralEntityReplacementText(string replacement, XmlVersion version)
    {
        DictionaryEntitySource source = new DictionaryEntitySource();
        source.AddDeclaration(CreateDeclaration("bad", replacement, true, false, null));
        EntityExpander expander = CreateExpander(version, XmlEntityHandling.Expand, false, source);

        Assert.Throws<FormatException>(() => expander.DispatchXmlReference(ScanReference("&bad;", version)));
    }

    [Test]
    public void RejectsAnUnpairedSurrogateInGeneralEntityReplacementText()
    {
        DictionaryEntitySource source = new DictionaryEntitySource();
        source.AddDeclaration(CreateDeclaration("bad", "bad" + '\uD800' + "text", true, false, null));
        EntityExpander expander = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, source);

        Assert.Throws<FormatException>(() => expander.DispatchXmlReference(ScanReference("&bad;", XmlVersion.Xml10)));
    }

    [Test]
    public void AcceptsLegalSupplementaryCharactersAndReferenceMarkupInReplacementText()
    {
        DictionaryEntitySource source = new DictionaryEntitySource();
        source.AddDeclaration(CreateDeclaration("valid", "\U0001F600&#x1;", true, false, null));
        EntityExpander expander = CreateExpander(XmlVersion.Xml11, XmlEntityHandling.Expand, false, source);

        XmlEntityDispatchResult result = expander.DispatchXmlReference(ScanReference("&valid;", XmlVersion.Xml11));

        Assert.That(result.Items.Select(item => item.Value), Is.EqualTo(new string[] { "\U0001F600", "\u0001" }));
    }

    [Test]
    public void DefensivelyRejectsInconsistentCharacterReferenceTokens()
    {
        EntityExpander xml10 = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, null);
        EntityExpander xml11 = CreateExpander(XmlVersion.Xml11, XmlEntityHandling.Expand, false, null);
        XmlToken illegal = CreateCharacterToken(0, "\0", false);
        XmlToken wrongText = CreateCharacterToken(65, "B", false);
        XmlToken wrongRestrictedFlag = CreateCharacterToken(0x7F, "\u007F", false);
        XmlToken missingText = new XmlToken(XmlTokenKind.CharacterReference, null, null, "&#65;", 0, 5, 65, false, '\0', false, null);

        Assert.Throws<FormatException>(() => xml10.DispatchXmlReference(illegal));
        Assert.Throws<FormatException>(() => xml10.DispatchXmlReference(wrongText));
        Assert.Throws<FormatException>(() => xml11.DispatchXmlReference(wrongRestrictedFlag));
        Assert.Throws<FormatException>(() => xml10.DispatchXmlReference(missingText));
    }

    [Test]
    public void RejectsWrongTokenKindsAndMissingEntityNames()
    {
        EntityExpander expander = CreateExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, null);
        XmlToken text = new XmlToken(XmlTokenKind.Text, null, "text", "text", 0, 4, 0, false, '\0', false, null);
        XmlToken missingName = new XmlToken(XmlTokenKind.EntityReference, null, null, "&;", 0, 2, 0, false, '\0', false, null);

        Assert.Throws<ArgumentException>(() => expander.DispatchXmlReference(text));
        Assert.Throws<FormatException>(() => expander.DispatchXmlReference(missingName));
    }

    [Test]
    public void ConstructorRejectsUnknownVersionAndHandlingValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateExpander((XmlVersion)2, XmlEntityHandling.Expand, false, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateExpander(XmlVersion.Xml10, (XmlEntityHandling)2, false, null));
    }

    private static EntityExpander CreateExpander(XmlVersion version, XmlEntityHandling handling, bool skipUnresolved, IXmlGeneralEntitySource? source)
    {
        return new EntityExpander(version, handling, skipUnresolved, source);
    }

    private static XmlToken ScanReference(string text, XmlVersion version)
    {
        XmlScanner scanner = new XmlScanner(text.AsMemory(), version, XmlEncodingEntityKind.Document);
        return scanner.ScanNextXmlToken();
    }

    private static XmlToken CreateCharacterToken(int scalar, string value, bool restricted)
    {
        return new XmlToken(XmlTokenKind.CharacterReference, null, value, "reference", 0, 9, scalar, restricted, '\0', false, null);
    }

    private static XmlEntityDeclaration CreateDeclaration(string name, string? replacement, bool parsed, bool external, string? notation)
    {
        XmlEntityDeclaration declaration = new XmlEntityDeclaration();
        declaration.Name = name;
        declaration.ReplacementText = replacement;
        declaration.IsParsed = parsed;
        declaration.IsExternal = external;
        declaration.NotationName = notation;
        return declaration;
    }

    private class DictionaryEntitySource : IXmlGeneralEntitySource
    {
        public Dictionary<string, XmlEntityDeclaration> Declarations = new Dictionary<string, XmlEntityDeclaration>(StringComparer.Ordinal);
        public List<string> RequestedNames = [];

        public void AddDeclaration(XmlEntityDeclaration declaration)
        {
            Declarations.Add(declaration.Name!, declaration);
        }

        public XmlEntityDeclaration? FindXmlGeneralEntity(string name)
        {
            RequestedNames.Add(name);
            return Declarations.TryGetValue(name, out XmlEntityDeclaration? declaration) ? declaration : null;
        }
    }

    private class FixedEntitySource : IXmlGeneralEntitySource
    {
        public XmlEntityDeclaration Declaration;

        public FixedEntitySource(XmlEntityDeclaration declaration)
        {
            Declaration = declaration;
        }

        public XmlEntityDeclaration? FindXmlGeneralEntity(string name)
        {
            return Declaration;
        }
    }
}
