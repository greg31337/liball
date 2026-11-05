using System.Text;

namespace odf.Tests;

public class StyleMaintenanceModelTests
{
    [Test]
    public void SynthesizesAndReusesIndependentAutomaticStyle()
    {
        OdfDocument document = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='Body' style:family='paragraph'>"
            + "<style:text-properties fo:font-weight='bold'/>"
            + "</style:style>"
            + "</office:styles>",
            "",
            "<text:p text:style-name='Body'>Text</text:p>");
        OdfStyle source = document.Styles.Single();

        OdfStyle synthesized = StyleMaintenanceModel.SynthesizeAutomaticStyle(
            document,
            source,
            "P1");
        OdfStyle reused = StyleMaintenanceModel.SynthesizeAutomaticStyle(
            document,
            source,
            "P2");
        synthesized.TextProperties[0].LexicalValue = "normal";

        Assert.Multiple(() =>
        {
            Assert.That(document.Styles, Has.Length.EqualTo(2));
            Assert.That(reused, Is.SameAs(synthesized));
            Assert.That(synthesized.Name, Is.EqualTo("P1"));
            Assert.That(synthesized.IsAutomatic, Is.True);
            Assert.That(synthesized.IsCommon, Is.False);
            Assert.That(synthesized.Node, Is.Null);
            Assert.That(
                source.TextProperties[0].LexicalValue,
                Is.EqualTo("bold"));
            Assert.That(
                synthesized.TextProperties[0],
                Is.Not.SameAs(source.TextProperties[0]));
        });
    }

    [Test]
    public void RenamesStyleAndUpdatesOnlyMatchingFamilyReferences()
    {
        OdfDocument document = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='Body' style:family='paragraph'/>"
            + "<style:style style:name='Child' style:family='paragraph' "
            + "style:parent-style-name='Body'/>"
            + "<style:style style:name='Body' style:family='text'/>"
            + "</office:styles>",
            "",
            "<text:p text:style-name='Body'>"
            + "<text:span text:style-name='Body'>Text</text:span>"
            + "</text:p>");
        OdfStyle paragraph = document.Styles.Single(
            item => item.Name == "Body"
                && item.Family == OdfStyleFamily.Paragraph);

        StyleMaintenanceModel.RenameStyle(document, paragraph, "Renamed");
        OdfDocument reread = WriteAndRead(document);
        OdfNode paragraphNode = FindElement(reread, OdfNamespaces.Text, "p");
        OdfNode spanNode = FindElement(reread, OdfNamespaces.Text, "span");

        Assert.Multiple(() =>
        {
            Assert.That(
                reread.Styles.Single(item => item.Name == "Child")
                    .ParentStyleName,
                Is.EqualTo("Renamed"));
            Assert.That(
                AttributeValue(paragraphNode, OdfNamespaces.Text, "style-name"),
                Is.EqualTo("Renamed"));
            Assert.That(
                AttributeValue(spanNode, OdfNamespaces.Text, "style-name"),
                Is.EqualTo("Body"));
            Assert.That(
                reread.Styles.Any(item => item.Name == "Body"
                    && item.Family == OdfStyleFamily.Text),
                Is.True);
        });
    }

    [Test]
    public void DependencyClosureIncludesParentAndNextOnceAcrossCycles()
    {
        OdfDocument document = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='A' style:family='paragraph' "
            + "style:parent-style-name='B' style:next-style-name='C'/>"
            + "<style:style style:name='B' style:family='paragraph' "
            + "style:parent-style-name='A'/>"
            + "<style:style style:name='C' style:family='paragraph'/>"
            + "</office:styles>",
            "",
            "");
        OdfStyle root = document.Styles.Single(item => item.Name == "A");

        OdfStyle[] closure = StyleMaintenanceModel.BuildDependencyClosure(
            document,
            root);

        Assert.Multiple(() =>
        {
            Assert.That(closure.Select(item => item.Name), Is.EqualTo(
                new[] { "A", "B", "C" }));
            Assert.That(closure.Distinct().ToArray(), Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void CopiesStyleClosureAcrossDocumentsWithIndependentData()
    {
        OdfDocument sourceDocument = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='Parent' style:family='paragraph'>"
            + "<style:text-properties fo:font-weight='bold'/>"
            + "</style:style>"
            + "<style:style style:name='Child' style:family='paragraph' "
            + "style:parent-style-name='Parent'/>"
            + "</office:styles>",
            "",
            "");
        OdfDocument targetDocument = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='Parent' style:family='paragraph'>"
            + "<style:text-properties fo:font-weight='normal'/>"
            + "</style:style>"
            + "</office:styles>",
            "",
            "");
        OdfStyle source = sourceDocument.Styles.Single(
            item => item.Name == "Child");

        OdfStyle copied = StyleMaintenanceModel.CopyStyleAcrossDocuments(
            sourceDocument,
            targetDocument,
            source,
            "Copied");
        OdfStyle copiedParent = targetDocument.Styles.Single(
            item => item.Name == "Parent2");
        copiedParent.TextProperties[0].LexicalValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(copied.Name, Is.EqualTo("Copied"));
            Assert.That(copied.ParentStyleName, Is.EqualTo("Parent2"));
            Assert.That(copied.ParentStyle, Is.SameAs(copiedParent));
            Assert.That(targetDocument.Styles, Has.Length.EqualTo(3));
            Assert.That(
                sourceDocument.Styles.Single(item => item.Name == "Parent")
                    .TextProperties[0].LexicalValue,
                Is.EqualTo("bold"));
        });
    }

    [Test]
    public void DeduplicatesAutomaticStylesAndFindsUnusedNamedStyles()
    {
        OdfDocument document = ReadFlat(
            "<office:styles>"
            + "<style:style style:name='Unused' style:family='paragraph'/>"
            + "<style:style style:name='Parent' style:family='paragraph'/>"
            + "<style:style style:name='Child' style:family='paragraph' "
            + "style:parent-style-name='Parent'/>"
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

        OdfStyle[] removed = StyleMaintenanceModel.DeduplicateAutomaticStyles(
            document);
        OdfStyle[] unused = StyleMaintenanceModel.FindUnusedStyles(document);
        OdfNode paragraph = FindElement(document, OdfNamespaces.Text, "p");

        Assert.Multiple(() =>
        {
            Assert.That(removed.Select(item => item.Name), Is.EqualTo(
                new[] { "A2" }));
            Assert.That(
                AttributeValue(paragraph, OdfNamespaces.Text, "style-name"),
                Is.EqualTo("A1"));
            Assert.That(document.Styles.Any(item => item.Name == "A2"), Is.False);
            Assert.That(unused.Select(item => item.Name), Is.EquivalentTo(
                new[] { "Unused", "Child" }));
            Assert.That(unused.Any(item => item.Name == "Parent"), Is.False);
            Assert.That(unused.Any(item => item.Name == "A1"), Is.False);
        });
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
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + automaticStyles
            + "<office:body><office:text>" + body + "</office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "maintenance.fodt",
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
            "maintenance.fodt",
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
