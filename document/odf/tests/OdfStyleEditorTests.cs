using System.Text;

namespace odf.Tests;

public class OdfStyleEditorTests
{
    [Test]
    public void FindsStylesAndReturnsPublicEffectiveResolution()
    {
        OdfDocument document = ReadFlat(
            "<office:styles>"
            + "<style:default-style style:family='paragraph'>"
            + "<style:paragraph-properties fo:margin-left='1cm'/>"
            + "</style:default-style>"
            + "<style:style style:name='Parent' style:family='paragraph'>"
            + "<style:paragraph-properties fo:text-align='center'/>"
            + "</style:style>"
            + "<style:style style:name='Child' style:family='paragraph' "
            + "style:parent-style-name='Parent'>"
            + "<style:paragraph-properties fo:margin-left='2cm'/>"
            + "</style:style>"
            + "<style:style style:name='Inline' style:family='text'/>"
            + "</office:styles>",
            "",
            "");
        IOdfStyleEditor editor = new OdfStyleEditor();

        OdfStyle? child = editor.FindStyle(
            document,
            "Child",
            OdfStyleFamily.Paragraph);
        OdfStyle[] paragraphs = editor.FindStyles(
            document,
            OdfStyleFamily.Paragraph);
        OdfStyleResolution resolution = editor.ResolveEffectiveProperties(
            document,
            child!);
        OdfNode properties = resolution.EffectiveProperties.Single();

        Assert.Multiple(() =>
        {
            Assert.That(child, Is.Not.Null);
            Assert.That(paragraphs, Has.Length.EqualTo(3));
            Assert.That(
                editor.FindStyle(document, "Missing", OdfStyleFamily.Paragraph),
                Is.Null);
            Assert.That(resolution.IsValid, Is.True);
            Assert.That(resolution.Style, Is.SameAs(child));
            Assert.That(
                resolution.Sources.Select(item => item.Name),
                Is.EqualTo(new[] { "", "Parent", "Child" }));
            Assert.That(
                AttributeValue(properties, OdfNamespaces.Fo, "margin-left"),
                Is.EqualTo("2cm"));
            Assert.That(
                AttributeValue(properties, OdfNamespaces.Fo, "text-align"),
                Is.EqualTo("center"));
            Assert.That(resolution.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CreatesIndependentCommonDefaultAndAutomaticStyles()
    {
        OdfDocument document = ReadFlat("<office:styles/>", "", "");
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle definition = ParagraphStyle("Body", "left");

        OdfStyle common = editor.CreateStyle(document, definition);
        OdfStyle automatic = editor.CreateAutomaticStyle(
            document,
            common,
            "P1");
        OdfStyle defaultStyle = editor.CreateStyle(
            document,
            new OdfStyle
            {
                Family = OdfStyleFamily.Paragraph,
                IsDefault = true
            });
        definition.ParagraphProperties[0].LexicalValue = "right";
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.Styles, Has.Length.EqualTo(3));
            Assert.That(common, Is.Not.SameAs(definition));
            Assert.That(common.IsCommon, Is.True);
            Assert.That(common.ParagraphProperties[0].LexicalValue, Is.EqualTo("left"));
            Assert.That(automatic.Name, Is.EqualTo("P1"));
            Assert.That(automatic.IsAutomatic, Is.True);
            Assert.That(defaultStyle.IsDefault, Is.True);
            Assert.That(defaultStyle.IsNamed, Is.False);
            Assert.That(reread.Styles, Has.Length.EqualTo(3));
            Assert.That(
                reread.Styles.Single(item => item.Name == "P1").IsAutomatic,
                Is.True);
            Assert.That(
                reread.Styles.Single(item => item.IsDefault).Family,
                Is.EqualTo(OdfStyleFamily.Paragraph));
            Assert.That(
                () => editor.CreateStyle(document, ParagraphStyle("Body", "left")),
                Throws.TypeOf<OdfEditException>());
        });
    }

    [Test]
    public void ModifiesAndRenamesStyleWhilePreservingReferences()
    {
        OdfDocument document = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='Body' style:family='paragraph'/>"
            + "<style:style style:name='Child' style:family='paragraph' "
            + "style:parent-style-name='Body'/>"
            + "</office:styles>",
            "",
            "<text:p text:style-name='Body'>Text</text:p>");
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle body = editor.FindStyle(
            document,
            "Body",
            OdfStyleFamily.Paragraph)!;
        OdfStyle replacement = ParagraphStyle("Edited", "center");
        replacement.DisplayName = "Edited Body";

        OdfStyle modified = editor.ModifyStyle(
            document,
            body,
            replacement);
        replacement.ParagraphProperties[0].LexicalValue = "right";
        editor.RenameStyle(document, body, "Final");
        OdfDocument reread = WriteAndRead(document);
        OdfNode paragraph = FindElement(reread, OdfNamespaces.Text, "p");

        Assert.Multiple(() =>
        {
            Assert.That(modified, Is.SameAs(body));
            Assert.That(body.Name, Is.EqualTo("Final"));
            Assert.That(body.DisplayName, Is.EqualTo("Edited Body"));
            Assert.That(body.ParagraphProperties[0].LexicalValue, Is.EqualTo("center"));
            Assert.That(
                reread.Styles.Single(item => item.Name == "Child")
                    .ParentStyleName,
                Is.EqualTo("Final"));
            Assert.That(
                AttributeValue(paragraph, OdfNamespaces.Text, "style-name"),
                Is.EqualTo("Final"));
        });
    }

    [Test]
    public void CopiesStyleDependenciesThroughPublicEditor()
    {
        OdfDocument sourceDocument = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='Parent' style:family='paragraph'>"
            + "<style:paragraph-properties fo:text-align='center'/>"
            + "</style:style>"
            + "<style:style style:name='Child' style:family='paragraph' "
            + "style:parent-style-name='Parent'/>"
            + "</office:styles>",
            "",
            "");
        OdfDocument targetDocument = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='Parent' style:family='paragraph'/>"
            + "</office:styles>",
            "",
            "");
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle source = editor.FindStyle(
            sourceDocument,
            "Child",
            OdfStyleFamily.Paragraph)!;

        OdfStyle copied = editor.CopyStyle(
            sourceDocument,
            targetDocument,
            source,
            "Imported");

        Assert.Multiple(() =>
        {
            Assert.That(copied.Name, Is.EqualTo("Imported"));
            Assert.That(copied.ParentStyleName, Is.EqualTo("Parent2"));
            Assert.That(copied.ParentStyle?.Name, Is.EqualTo("Parent2"));
            Assert.That(targetDocument.Styles, Has.Length.EqualTo(3));
            Assert.That(copied, Is.Not.SameAs(source));
        });
    }

    [Test]
    public void CleansDuplicateAndUnusedStylesThroughPublicEditor()
    {
        OdfDocument document = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='Unused' style:family='paragraph'/>"
            + "</office:styles>",
            "<office:automatic-styles>"
            + "<style:style style:name='A1' style:family='paragraph'>"
            + "<style:paragraph-properties fo:text-align='center'/>"
            + "</style:style>"
            + "<style:style style:name='A2' style:family='paragraph'>"
            + "<style:paragraph-properties fo:text-align='center'/>"
            + "</style:style>"
            + "</office:automatic-styles>",
            "<text:p text:style-name='A2'>Text</text:p>");
        IOdfStyleEditor editor = new OdfStyleEditor();

        OdfStyle[] duplicates = editor.DeduplicateAutomaticStyles(document);
        OdfStyle[] unused = editor.FindUnusedStyles(document);
        OdfStyle[] removed = editor.RemoveUnusedStyles(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                duplicates.Select(item => item.Name),
                Is.EqualTo(new[] { "A2" }));
            Assert.That(
                unused.Select(item => item.Name),
                Is.EqualTo(new[] { "Unused" }));
            Assert.That(removed, Is.EqualTo(unused));
            Assert.That(document.Styles.Select(item => item.Name), Is.EqualTo(
                new[] { "A1" }));
            Assert.That(
                AttributeValue(
                    FindElement(document, OdfNamespaces.Text, "p"),
                    OdfNamespaces.Text,
                    "style-name"),
                Is.EqualTo("A1"));
        });
    }

    private static OdfStyle ParagraphStyle(string name, string alignment)
    {
        return new OdfStyle
        {
            Name = name,
            Family = OdfStyleFamily.Paragraph,
            ParagraphProperties =
            [
                new OdfStyleProperty
                {
                    Name = new OdfQualifiedName
                    {
                        NamespaceName = OdfNamespaces.Fo,
                        LocalName = "text-align",
                        Prefix = OdfNamespaces.FoPrefix
                    },
                    LexicalValue = alignment
                }
            ]
        };
    }

    private static OdfDocument ReadFlat(
        string styles,
        string automaticStyles,
        string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + automaticStyles
            + "<office:body><office:text>" + body + "</office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "style-editor.fodt",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "style-editor.fodt",
            new OdfLoadOptions());
    }

    private static OdfNode FindElement(
        OdfDocument document,
        string namespaceName,
        string localName)
    {
        return new OdfTreeEditor(document.Nodes).TraverseAllNodes().Single(
            item => item.Name?.NamespaceName == namespaceName
                && item.Name.LocalName == localName);
    }

    private static string AttributeValue(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Attributes.Single(
            item => item.Name.NamespaceName == namespaceName
                && item.Name.LocalName == localName).Value;
    }
}
