using xml;

namespace xml.Tests;

[TestFixture]
public class XmlIdIndexTests
{
    [Test]
    public void NormalizesXmlIdValuesAndIndexesOnlyValidNcNames()
    {
        XmlDocument document = Parse("<root><item xml:id=' &#x9;alpha&#xA; '/><item xml:id='1bad'/><item xml:id='two words'/><item xml:id='a:b'/><item xml:id='valid_名'/></root>", Options(false));
        XmlNode root = document.DocumentElement!;
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(root.Children![0].Attributes![0].Value, Is.EqualTo("alpha"));
        Assert.That(navigator.FindXmlNodeById(document, "alpha"), Is.SameAs(root.Children[0]));
        Assert.That(navigator.FindXmlNodeById(document, "valid_名"), Is.SameAs(root.Children[4]));
        Assert.That(navigator.FindXmlNodeById(document, "1bad"), Is.Null);
        Assert.That(navigator.FindXmlNodeById(document, "two words"), Is.Null);
        Assert.That(navigator.FindXmlNodeById(document, "a:b"), Is.Null);
        Assert.That(navigator.FindXmlNodeById(document, " &#x9;alpha&#xA; "), Is.Null);
    }

    [Test]
    public void RecognizesLexicalXmlIdWhenNamespaceProcessingIsDisabled()
    {
        XmlDocument document = Parse("<root><item xml:id='  lexical  '/></root>", Options(true));
        XmlNode item = document.DocumentElement!.Children![0];

        Assert.That(item.Attributes![0].Value, Is.EqualTo("lexical"));
        Assert.That(new XmlTreeNavigator().FindXmlNodeById(document, "lexical"), Is.SameAs(item));
    }

    [Test]
    public void ResolvesDtdXmlAndSchemaIdConflictsInDocumentOrder()
    {
        XmlParserOptions options = Options(false);
        options.DtdProcessing = XmlDtdProcessing.Parse;
        XmlDocument document = Parse("<!DOCTYPE root [<!ELEMENT root ANY><!ELEMENT item EMPTY><!ATTLIST item key ID #IMPLIED xml:id CDATA #IMPLIED>]><root><item key='shared'/><item xml:id='shared'/><item code='raw'/><item xml:id='primary' alias='raw'/><item key='  p:legacy  '/></root>", options);
        XmlNode root = document.DocumentElement!;
        XmlNode dtdElement = root.Children![0];
        XmlNode xmlElement = root.Children[1];
        XmlNode schemaElement = root.Children[2];
        XmlNode precedenceElement = root.Children[3];
        schemaElement.Attributes![0].TypeInfo = SchemaId(" shared ");
        precedenceElement.Attributes![0].TypeInfo = SchemaId("schema-alias");
        precedenceElement.Attributes[1].TypeInfo = SchemaId("secondary");
        XmlTreeNavigator navigator = new XmlTreeNavigator();
        XmlTreeEditor editor = new XmlTreeEditor();

        Assert.That(navigator.FindXmlNodeById(document, "shared"), Is.SameAs(dtdElement));
        Assert.That(navigator.FindXmlNodeById(document, "p:legacy"), Is.SameAs(root.Children[4]));
        Assert.That(navigator.FindXmlNodeById(document, "primary"), Is.SameAs(precedenceElement));
        Assert.That(navigator.FindXmlNodeById(document, "schema-alias"), Is.Null);
        Assert.That(navigator.FindXmlNodeById(document, "secondary"), Is.SameAs(precedenceElement));

        editor.RemoveXmlTreeAttribute(dtdElement, dtdElement.Attributes![0].Name!);
        Assert.That(navigator.FindXmlNodeById(document, "shared"), Is.SameAs(xmlElement));

        editor.RemoveXmlTreeAttribute(xmlElement, xmlElement.Attributes![0].Name!);
        Assert.That(navigator.FindXmlNodeById(document, "shared"), Is.SameAs(schemaElement));
    }

    [Test]
    public void RebuildsTheIndexAfterIdMutationAndDocumentReordering()
    {
        XmlDocument document = Parse("<root><item xml:id='same'/><item xml:id='same'/></root>", Options(false));
        XmlNode root = document.DocumentElement!;
        XmlNode first = root.Children![0];
        XmlNode second = root.Children[1];
        XmlTreeNavigator navigator = new XmlTreeNavigator();
        XmlTreeEditor editor = new XmlTreeEditor();

        Assert.That(navigator.FindXmlNodeById(document, "same"), Is.SameAs(first));

        editor.InsertXmlTreeBefore(root, second, first);

        Assert.That(navigator.FindXmlNodeById(document, "same"), Is.SameAs(second));
        editor.SetXmlTreeText(second.Attributes![0], "  changed  ");
        Assert.That(second.Attributes[0].Value, Is.EqualTo("changed"));
        Assert.That(navigator.FindXmlNodeById(document, "same"), Is.SameAs(first));
        Assert.That(navigator.FindXmlNodeById(document, "changed"), Is.SameAs(second));
    }

    [Test]
    public void NormalizesXmlIdValuesCreatedAndAttachedThroughTheEditor()
    {
        XmlDocument document = Parse("<root/>", Options(false));
        XmlNode root = document.DocumentElement!;
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlName name = new XmlName { Prefix = "xml", LocalName = "id", NamespaceUri = XmlNamespaces.Xml, QualifiedName = "xml:id" };
        XmlNode attribute = editor.CreateXmlTreeNode(document, XmlNodeKind.Attribute, name, " \tcreated\n ");

        Assert.That(attribute.Value, Is.EqualTo("created"));

        editor.SetXmlTreeText(attribute, " \trevised\n ");
        editor.SetXmlTreeAttribute(root, attribute);

        Assert.That(attribute.Value, Is.EqualTo("revised"));
        Assert.That(attribute.Name!.QualifiedName, Is.EqualTo("xml:id"));
        Assert.That(new XmlTreeNavigator().FindXmlNodeById(document, "revised"), Is.SameAs(root));
    }

    [Test]
    public void PreservesSchemaIdAnnotationsWhenNodesAreImported()
    {
        XmlDocument source = Parse("<source code='raw'/>", Options(false));
        XmlDocument target = Parse("<target/>", Options(false));
        XmlNode sourceRoot = source.DocumentElement!;
        sourceRoot.Attributes![0].TypeInfo = SchemaId("typed-id");
        XmlTreeEditor editor = new XmlTreeEditor();

        XmlNode imported = editor.ImportXmlTreeNode(target, sourceRoot, true);
        editor.AppendXmlTreeChild(target.DocumentElement!, imported);

        Assert.That(imported.Attributes![0].TypeInfo, Is.Not.Null);
        Assert.That(imported.Attributes[0].TypeInfo, Is.Not.SameAs(sourceRoot.Attributes[0].TypeInfo));
        Assert.That(new XmlTreeNavigator().FindXmlNodeById(target, "typed-id"), Is.SameAs(imported));
    }

    private static XmlSchemaTypeInfo SchemaId(string normalizedValue)
    {
        return new XmlSchemaTypeInfo
        {
            IsId = true,
            NormalizedValue = normalizedValue
        };
    }

    private static XmlDocument Parse(string text, XmlParserOptions options)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private static XmlParserOptions Options(bool namespacesDisabled)
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = namespacesDisabled ? XmlNamespaceHandling.Disabled : XmlNamespaceHandling.Namespaces10,
            PreservePrefixes = true,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
    }
}
