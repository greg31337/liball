using System.Text;

namespace odf.Tests;

public class StyleResolverTests
{
    [Test]
    public void ResolvesEveryRelationshipAndEffectiveInheritanceLayer()
    {
        OdfDocument document = ReadDocument(RelationshipXml());
        OdfStyle defaultStyle = document.Styles.Single(item => item.IsDefault);
        OdfStyle baseStyle = document.Styles.Single(item => item.Name == "Base");
        OdfStyle child = document.Styles.Single(item => item.Name == "Child");
        OdfStyle automatic = document.Styles.Single(item => item.Name == "Automatic");

        StyleResolutionResult resolution =
            StyleResolver.ResolveEffectiveProperties(document, child);
        OdfNode paragraph = FindProperty(
            resolution,
            "paragraph-properties");
        OdfNode text = FindProperty(resolution, "text-properties");
        OdfNode tabStops = paragraph.Children.Single(
            item => item.Name?.LocalName == "tab-stops");

        Assert.Multiple(() =>
        {
            Assert.That(child.ParentStyle, Is.SameAs(baseStyle));
            Assert.That(child.NextStyle, Is.SameAs(baseStyle));
            Assert.That(child.DefaultStyle, Is.SameAs(defaultStyle));
            Assert.That(child.ListStyleNode?.Name?.LocalName, Is.EqualTo("list-style"));
            Assert.That(child.MasterPageNode?.Name?.LocalName, Is.EqualTo("master-page"));
            Assert.That(child.DataStyleNode?.Name?.LocalName, Is.EqualTo("number-style"));
            Assert.That(automatic.ParentStyle, Is.SameAs(child));
            Assert.That(baseStyle.HasNextCycle, Is.True);
            Assert.That(baseStyle.HasParentCycle, Is.False);
            Assert.That(resolution.IsValid, Is.True);
            Assert.That(
                resolution.Sources,
                Is.EqualTo(new[] { defaultStyle, baseStyle, child }));
            Assert.That(resolution.EffectiveProperties, Has.Length.EqualTo(2));
            Assert.That(AttributeValue(paragraph, OdfNamespaces.Fo, "margin-left"), Is.EqualTo("3cm"));
            Assert.That(AttributeValue(paragraph, OdfNamespaces.Fo, "margin-right"), Is.EqualTo("2cm"));
            Assert.That(AttributeValue(paragraph, OdfNamespaces.Fo, "break-before"), Is.EqualTo("page"));
            Assert.That(AttributeValue(text, OdfNamespaces.Fo, "font-size"), Is.EqualTo("10pt"));
            Assert.That(AttributeValue(text, OdfNamespaces.Fo, "font-weight"), Is.EqualTo("bold"));
            Assert.That(
                AttributeValue(tabStops.Children.Single(), OdfNamespaces.Style, "position"),
                Is.EqualTo("2cm"));
        });

        FindAttribute(paragraph, OdfNamespaces.Fo, "margin-left").Value = "9cm";

        Assert.That(
            AttributeValue(
                FindNode(child, "paragraph-properties"),
                OdfNamespaces.Fo,
                "margin-left"),
            Is.EqualTo("3cm"),
            "Effective property nodes must be independent of source XML.");
    }

    [Test]
    public void DetectsParentCyclesWithoutTreatingNextStyleLoopsAsInvalid()
    {
        string styles = "<office:styles>"
            + "<style:style style:name='A' style:family='paragraph' style:parent-style-name='B'/>"
            + "<style:style style:name='B' style:family='paragraph' style:parent-style-name='A'/>"
            + "<style:style style:name='C' style:family='paragraph' style:next-style-name='C'/>"
            + "</office:styles>";
        OdfDocument document = ReadDocument(FlatXml(styles, "", ""));
        OdfStyle a = document.Styles.Single(item => item.Name == "A");
        OdfStyle b = document.Styles.Single(item => item.Name == "B");
        OdfStyle c = document.Styles.Single(item => item.Name == "C");

        StyleResolutionResult resolution =
            StyleResolver.ResolveEffectiveProperties(document, a);
        OdfValidationOptions options = new OdfValidationOptions
        {
            Mode = OdfValidationMode.SemanticOnly
        };
        OdfDiagnostic[] diagnostics = new OdfValidator().ValidateDocument(
            document,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(a.HasParentCycle, Is.True);
            Assert.That(b.HasParentCycle, Is.True);
            Assert.That(c.HasParentCycle, Is.False);
            Assert.That(c.HasNextCycle, Is.True);
            Assert.That(resolution.IsValid, Is.False);
            Assert.That(resolution.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                diagnostics.Count(item => item.Code == OdfDiagnosticCodes.InvalidStyleReference),
                Is.EqualTo(2));
            Assert.That(
                diagnostics.All(item => !item.Message.Contains("next-style", StringComparison.Ordinal)),
                Is.True);
        });
    }

    [Test]
    public void WritesEditedAndNewRelationshipNamesAndResolvesThemAfterReload()
    {
        OdfDocument document = ReadDocument(RelationshipXml());
        OdfStyle child = document.Styles.Single(item => item.Name == "Child");
        child.ParentStyleName = "Solo";
        child.NextStyleName = "Base";
        child.ListStyleName = "ListOne";
        child.MasterPageName = "Master";
        child.DataStyleName = "NumberOne";
        OdfStyle generated = new OdfStyle
        {
            Name = "Generated",
            Family = OdfStyleFamily.Paragraph,
            IsCommon = true,
            ParentStyleName = "Child",
            NextStyleName = "Base",
            ListStyleName = "ListOne",
            MasterPageName = "Master",
            DataStyleName = "NumberOne"
        };
        document.Styles = [.. document.Styles, generated];

        byte[] written = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            written,
            "relationships.fodt",
            new OdfLoadOptions());
        OdfStyle reloadedChild = reread.Styles.Single(
            item => item.Name == "Child");
        OdfStyle reloadedGenerated = reread.Styles.Single(
            item => item.Name == "Generated");

        Assert.Multiple(() =>
        {
            Assert.That(reloadedChild.ParentStyle?.Name, Is.EqualTo("Solo"));
            Assert.That(reloadedChild.NextStyle?.Name, Is.EqualTo("Base"));
            Assert.That(reloadedChild.ListStyleNode, Is.Not.Null);
            Assert.That(reloadedChild.MasterPageNode, Is.Not.Null);
            Assert.That(reloadedChild.DataStyleNode, Is.Not.Null);
            Assert.That(reloadedGenerated.ParentStyle?.Name, Is.EqualTo("Child"));
            Assert.That(reloadedGenerated.NextStyle?.Name, Is.EqualTo("Base"));
            Assert.That(reloadedGenerated.ListStyleName, Is.EqualTo("ListOne"));
            Assert.That(reloadedGenerated.MasterPageName, Is.EqualTo("Master"));
            Assert.That(reloadedGenerated.DataStyleName, Is.EqualTo("NumberOne"));
            Assert.That(generated.Node, Is.Not.Null);
        });
    }

    private static OdfDocument ReadDocument(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "relationships.fodt",
            new OdfLoadOptions());
    }

    private static string RelationshipXml()
    {
        string styles = "<office:styles>"
            + "<style:default-style style:family='paragraph'>"
            + "<style:paragraph-properties fo:margin-left='1cm'>"
            + "<style:tab-stops><style:tab-stop style:position='1cm'/></style:tab-stops>"
            + "</style:paragraph-properties>"
            + "<style:text-properties fo:font-size='10pt'/>"
            + "</style:default-style>"
            + "<text:list-style style:name='ListOne'/>"
            + "<number:number-style style:name='NumberOne'/>"
            + "<style:style style:name='Base' style:family='paragraph' style:next-style-name='Base'>"
            + "<style:paragraph-properties fo:margin-left='2cm' fo:margin-right='2cm'/>"
            + "<style:text-properties fo:font-weight='normal'/>"
            + "</style:style>"
            + "<style:style style:name='Child' style:family='paragraph'"
            + " style:parent-style-name='Base' style:next-style-name='Base'"
            + " style:list-style-name='ListOne' style:master-page-name='Master'"
            + " style:data-style-name='NumberOne'>"
            + "<style:paragraph-properties fo:margin-left='3cm' fo:break-before='page'>"
            + "<style:tab-stops><style:tab-stop style:position='2cm'/></style:tab-stops>"
            + "</style:paragraph-properties>"
            + "<style:text-properties fo:font-weight='bold'/>"
            + "</style:style>"
            + "<style:style style:name='Solo' style:family='paragraph'/>"
            + "</office:styles>";
        string automatic = "<office:automatic-styles>"
            + "<style:style style:name='Automatic' style:family='paragraph'"
            + " style:parent-style-name='Child'/>"
            + "</office:automatic-styles>";
        string master = "<office:master-styles>"
            + "<style:master-page style:name='Master'/>"
            + "</office:master-styles>";
        return FlatXml(styles, automatic, master);
    }

    private static string FlatXml(
        string styles,
        string automatic,
        string master)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:number='" + OdfNamespaces.Number + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + automatic
            + master
            + "<office:body><office:text/></office:body>"
            + "</office:document>";
    }

    private static OdfNode FindProperty(
        StyleResolutionResult result,
        string localName)
    {
        return result.EffectiveProperties.Single(
            item => item.Name?.LocalName == localName);
    }

    private static OdfNode FindNode(OdfStyle style, string localName)
    {
        return style.Node!.Children.Single(
            item => item.Name?.LocalName == localName);
    }

    private static string AttributeValue(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return FindAttribute(node, namespaceName, localName).Value;
    }

    private static OdfAttribute FindAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Attributes.Single(
            item => item.Name.NamespaceName == namespaceName
                && item.Name.LocalName == localName);
    }
}
