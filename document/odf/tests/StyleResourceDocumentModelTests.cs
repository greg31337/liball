using System.Text;

namespace odf.Tests;

public class StyleResourceDocumentModelTests
{
    [Test]
    public void MaterializesEveryStyleResourceKindAndPreservesContent()
    {
        OdfDocument document = ReadFlat(ResourceFlatXml(
            "<office:styles>"
            + "<draw:gradient draw:name='Gradient' draw:display-name='Gradient One' "
            + "draw:style='linear' draw:start-color='#112233' "
            + "draw:end-color='#445566' draw:angle='450' ext:token='keep'/>"
            + "<svg:linearGradient draw:name='SvgGradient' svg:x1='0%' svg:y1='0%' "
            + "svg:x2='100%' svg:y2='100%'><svg:stop svg:offset='0%' "
            + "svg:stop-color='#000000'/><svg:stop svg:offset='100%' "
            + "svg:stop-color='#ffffff'/></svg:linearGradient>"
            + "<draw:hatch draw:name='Hatch' draw:style='double' "
            + "draw:color='#778899' draw:distance='0.20cm' draw:rotation='900'/>"
            + "<draw:fill-image draw:name='Fill' draw:display-name='Fill Image' "
            + "xlink:href='Pictures/fill.png' xlink:type='simple' "
            + "xlink:show='embed' xlink:actuate='onLoad'/>"
            + "<draw:opacity draw:name='Opacity' draw:style='linear' "
            + "draw:start='10%' draw:end='90%' draw:angle='0'/>"
            + "<draw:stroke-dash draw:name='Dash' draw:style='round' "
            + "draw:dots1='2' draw:dots1-length='0.10cm' "
            + "draw:distance='0.05cm'/><draw:marker draw:name='Arrow' "
            + "svg:viewBox='0 0 20 20' svg:d='M0 0 L20 10 L0 20 z'/>"
            + "<style:presentation-page-layout style:name='TitleLayout'>"
            + "<presentation:placeholder presentation:object='title' "
            + "svg:x='2cm' svg:y='1cm' svg:width='20cm' svg:height='3cm'/>"
            + "</style:presentation-page-layout>"
            + "<style:default-page-layout><style:page-layout-properties>"
            + "<style:background-image xlink:href='Pictures/background.png' "
            + "xlink:type='simple' style:repeat='stretch'/>"
            + "</style:page-layout-properties></style:default-page-layout>"
            + "</office:styles>"
            + "<office:body><office:presentation><draw:page draw:name='Slide'>"
            + "<presentation:event-listener presentation:action='sound'>"
            + "<presentation:sound xlink:href='Sounds/chime.wav' "
            + "xlink:type='simple' xlink:show='new' xlink:actuate='onRequest' "
            + "presentation:play-full='true'/></presentation:event-listener>"
            + "</draw:page></office:presentation></office:body>"));

        OdfStyleResource gradient = NamedResource(document, "Gradient");
        OdfStyleResource fill = NamedResource(document, "Fill");
        OdfStyleResource layout = NamedResource(document, "TitleLayout");
        OdfStyleResource background = KindResource(
            document,
            OdfStyleResourceKind.BackgroundImage);
        OdfStyleResource sound = KindResource(
            document,
            OdfStyleResourceKind.Sound);

        Assert.Multiple(() =>
        {
            Assert.That(document.StyleResources, Has.Length.EqualTo(10));
            Assert.That(document.StyleResources.Count(
                item => item.Kind == OdfStyleResourceKind.Gradient),
                Is.EqualTo(2));
            Assert.That(gradient.DisplayName, Is.EqualTo("Gradient One"));
            Assert.That(PropertyNamed(
                gradient.Attributes,
                OdfNamespaces.Drawing,
                "start-color").LexicalValue, Is.EqualTo("#112233"));
            Assert.That(PropertyNamed(
                gradient.Attributes,
                "urn:example:resource",
                "token").LexicalValue, Is.EqualTo("keep"));
            Assert.That(NamedResource(document, "Hatch").Kind,
                Is.EqualTo(OdfStyleResourceKind.Hatch));
            Assert.That(fill.Kind, Is.EqualTo(OdfStyleResourceKind.FillImage));
            Assert.That(fill.Href, Is.EqualTo("Pictures/fill.png"));
            Assert.That(NamedResource(document, "Opacity").Kind,
                Is.EqualTo(OdfStyleResourceKind.Opacity));
            Assert.That(NamedResource(document, "Dash").Kind,
                Is.EqualTo(OdfStyleResourceKind.StrokeDash));
            Assert.That(NamedResource(document, "Arrow").Kind,
                Is.EqualTo(OdfStyleResourceKind.Marker));
            Assert.That(layout.Kind,
                Is.EqualTo(OdfStyleResourceKind.PresentationLayout));
            Assert.That(layout.Content, Has.Length.EqualTo(1));
            Assert.That(background.Href,
                Is.EqualTo("Pictures/background.png"));
            Assert.That(background.ContainerNode?.Name?.LocalName,
                Is.EqualTo("page-layout-properties"));
            Assert.That(sound.Href, Is.EqualTo("Sounds/chime.wav"));
            Assert.That(sound.ContainerNode?.Name?.LocalName,
                Is.EqualTo("event-listener"));
        });

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(WriteDocument(document)));
        Assert.That(KindResource(reread, OdfStyleResourceKind.Sound).Href,
            Is.EqualTo("Sounds/chime.wav"));
    }

    [Test]
    public void WritesResourceChangesNestedReferencesContentAndDeletion()
    {
        OdfDocument document = ReadFlat(ResourceFlatXml(
            "<office:styles><draw:gradient draw:name='Keep' "
            + "draw:start-color='#000000' draw:end-color='#ffffff'/>"
            + "<draw:hatch draw:name='Remove' draw:color='#000000'/>"
            + "<draw:fill-image draw:name='Image' "
            + "xlink:href='Pictures/old.png'/><style:presentation-page-layout "
            + "style:name='Layout'><presentation:placeholder "
            + "presentation:object='title'/></style:presentation-page-layout>"
            + "</office:styles><office:body><office:presentation>"
            + "<draw:page><style:background-image "
            + "xlink:href='Pictures/old-background.png'/><presentation:sound "
            + "xlink:href='Sounds/old.wav'/></draw:page>"
            + "</office:presentation></office:body>"));
        OdfStyleResource gradient = NamedResource(document, "Keep");
        gradient.Name = "Changed";
        gradient.DisplayName = "Changed Gradient";
        PropertyNamed(
            gradient.Attributes,
            OdfNamespaces.Drawing,
            "start-color").LexicalValue = "#123456";
        OdfStyleResource image = NamedResource(document, "Image");
        image.Href = "Pictures/new.png";
        OdfStyleResource layout = NamedResource(document, "Layout");
        layout.Content =
        [
            PresentationElement("placeholder", "subtitle")
        ];
        OdfStyleResource background = KindResource(
            document,
            OdfStyleResourceKind.BackgroundImage);
        background.Href = "Pictures/new-background.png";
        OdfStyleResource sound = KindResource(
            document,
            OdfStyleResourceKind.Sound);
        sound.Href = "Sounds/new.wav";
        document.StyleResources =
        [
            gradient,
            image,
            layout,
            background,
            sound
        ];

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(WriteDocument(document)));

        Assert.Multiple(() =>
        {
            Assert.That(reread.StyleResources, Has.Length.EqualTo(5));
            Assert.That(NamedResource(reread, "Changed").DisplayName,
                Is.EqualTo("Changed Gradient"));
            Assert.That(PropertyNamed(
                NamedResource(reread, "Changed").Attributes,
                OdfNamespaces.Drawing,
                "start-color").LexicalValue, Is.EqualTo("#123456"));
            Assert.That(NamedResource(reread, "Image").Href,
                Is.EqualTo("Pictures/new.png"));
            Assert.That(NamedResource(reread, "Layout").Content[0]
                .Attributes[0].Value, Is.EqualTo("subtitle"));
            Assert.That(KindResource(
                reread,
                OdfStyleResourceKind.BackgroundImage).Href,
                Is.EqualTo("Pictures/new-background.png"));
            Assert.That(KindResource(reread, OdfStyleResourceKind.Sound).Href,
                Is.EqualTo("Sounds/new.wav"));
            Assert.That(gradient.Node, Is.Not.Null);
            Assert.That(layout.Content[0].Parent, Is.SameAs(layout.Node));
        });
    }

    [Test]
    public void CreatesNamedResourcesAndPresentationLayoutInPackage()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Presentation);
        document.StyleResources =
        [
            Resource(OdfStyleResourceKind.Gradient, "Gradient"),
            Resource(OdfStyleResourceKind.Hatch, "Hatch"),
            Resource(OdfStyleResourceKind.FillImage, "Image"),
            Resource(OdfStyleResourceKind.Opacity, "Opacity"),
            Resource(OdfStyleResourceKind.StrokeDash, "Dash"),
            Resource(OdfStyleResourceKind.Marker, "Marker"),
            new OdfStyleResource
            {
                Kind = OdfStyleResourceKind.PresentationLayout,
                Name = "Layout",
                Content = [PresentationElement("placeholder", "title")]
            }
        ];
        document.StyleResources[2].Href = "Pictures/fill.png";

        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            WriteDocument(document),
            "resources.odp",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reread.StyleResources, Has.Length.EqualTo(7));
            Assert.That(reread.StyleResources.All(
                item => item.PartPath == OdfPartNames.Styles), Is.True);
            Assert.That(NamedResource(reread, "Image").Href,
                Is.EqualTo("Pictures/fill.png"));
            Assert.That(NamedResource(reread, "Layout").Content,
                Has.Length.EqualTo(1));
            Assert.That(reread.Manifest?.Entries.Select(item => item.FullPath),
                Does.Contain(OdfPartNames.Styles));
        });
    }

    static OdfStyleResource Resource(
        OdfStyleResourceKind kind,
        string name)
    {
        return new OdfStyleResource
        {
            Kind = kind,
            Name = name
        };
    }

    static OdfStyleResource NamedResource(
        OdfDocument document,
        string name)
    {
        return document.StyleResources.Single(item => item.Name == name);
    }

    static OdfStyleResource KindResource(
        OdfDocument document,
        OdfStyleResourceKind kind)
    {
        return document.StyleResources.Single(item => item.Kind == kind);
    }

    static OdfStyleProperty PropertyNamed(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName)
    {
        return properties.Single(item => item.Name.NamespaceName == namespaceName
            && item.Name.LocalName == localName);
    }

    static OdfNode PresentationElement(string localName, string objectName)
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Presentation,
                LocalName = localName,
                Prefix = OdfNamespaces.PresentationPrefix
            },
            Attributes =
            [
                new OdfAttribute
                {
                    Name = new OdfQualifiedName
                    {
                        NamespaceName = OdfNamespaces.Presentation,
                        LocalName = "object",
                        Prefix = OdfNamespaces.PresentationPrefix
                    },
                    Value = objectName
                }
            ]
        };
    }

    static byte[] WriteDocument(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "resources.fodp",
            new OdfLoadOptions());
    }

    static string ResourceFlatXml(string sections)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:ext='urn:example:resource'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Presentation + "'>"
            + sections
            + "</office:document>";
    }
}
