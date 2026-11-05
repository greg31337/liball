using System.Text;

namespace odf.Tests;

public class PresentationSlideEditingModelTests
{
    [Test]
    public void CreatesInsertsReordersAndDeletesSlides()
    {
        OdfDocument document = Read(PresentationXml());

        OdfSlide inserted = PresentationSlideEditingModel.InsertSlide(
            document,
            1,
            "Inserted");
        OdfSlide appended = PresentationSlideEditingModel.CreateSlide(
            document,
            "End");
        appended = PresentationSlideEditingModel.ReorderSlide(
            document,
            appended,
            0);
        inserted = document.Slides.Single(item =>
            item.Name == "Inserted");
        PresentationSlideEditingModel.DeleteSlide(document, inserted);

        Assert.Multiple(() =>
        {
            Assert.That(
                document.Slides.Select(item => item.Name),
                Is.EqualTo(new[] { "End", "First", "Second" }));
            Assert.That(
                document.Slides.Select(item => item.Index),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(appended.Id, Is.EqualTo("slide-4"));
            Assert.That(document.Slides.Select(item => item.Id),
                Is.Unique);
            Assert.That(() => PresentationSlideEditingModel.InsertSlide(
                document,
                4,
                "Invalid"),
                Throws.TypeOf<OdfEditException>());
            Assert.That(() => PresentationSlideEditingModel.ReorderSlide(
                document,
                document.Slides[0],
                3),
                Throws.TypeOf<OdfEditException>());
        });

        OdfDocument reread = ReadBytes(Write(document));
        Assert.That(
            reread.Slides.Select(item => item.Name),
            Is.EqualTo(new[] { "End", "First", "Second" }));
    }

    [Test]
    public void DuplicatesAndCopiesSlidesWithRemappedInternalReferences()
    {
        OdfDocument document = Read(PresentationXml());
        OdfSlide copy = PresentationSlideEditingModel.DuplicateSlide(
            document,
            document.Slides[0]);
        OdfDrawingObject copiedFirst = copy.DrawingObjects.Single(item =>
            item.Name == "Shape One Copy 2");
        OdfDrawingObject copiedSecond = copy.DrawingObjects.Single(item =>
            item.Name == "Shape Two Copy 2");
        string copiedHref = FindHref(copy.Node ?? new OdfNode());
        OdfSlide secondCopy = PresentationSlideEditingModel.CopySlide(
            document,
            document.Slides[0],
            document.Slides.Length);
        string[] identifiers = document.DrawingObjects
            .SelectMany(item => new[] { item.Id, item.XmlId })
            .Where(item => item.Length > 0)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(copy.Index, Is.EqualTo(1));
            Assert.That(copy.Name, Is.EqualTo("First Copy 2"));
            Assert.That(copy.Id, Is.EqualTo("page1-copy-2"));
            Assert.That(copy.XmlId, Is.EqualTo("page-xml-copy-2"));
            Assert.That(copy.NavigationObjects,
                Is.EqualTo(new[] { copiedSecond, copiedFirst }));
            Assert.That(copiedSecond.Geometry.Routing.StartShape,
                Is.SameAs(copiedFirst));
            Assert.That(copiedHref, Is.EqualTo("#" + copiedSecond.Id));
            Assert.That(FindTarget(copy.Node ?? new OdfNode()),
                Is.EqualTo(copiedFirst.Id));
            Assert.That(copy.Notes?.TextBlocks.Single().Text,
                Is.EqualTo("Notes one"));
            Assert.That(secondCopy.Index, Is.EqualTo(3));
            Assert.That(secondCopy.Name, Is.EqualTo("First Copy 3"));
            Assert.That(identifiers, Is.Unique);
        });

        OdfDocument reread = ReadBytes(Write(document));
        OdfSlide roundTripCopy = reread.Slides[1];
        Assert.Multiple(() =>
        {
            Assert.That(roundTripCopy.IsNavigationOrderValid, Is.True);
            Assert.That(roundTripCopy.NavigationObjects, Has.Length.EqualTo(2));
            Assert.That(roundTripCopy.Notes?.TextBlocks.Single().Text,
                Is.EqualTo("Notes one"));
            Assert.That(FindHref(roundTripCopy.Node ?? new OdfNode()),
                Is.EqualTo("#" + roundTripCopy.NavigationObjects[0].Id));
            Assert.That(FindTarget(roundTripCopy.Node ?? new OdfNode()),
                Is.EqualTo(roundTripCopy.NavigationObjects[1].Id));
        });
    }

    [Test]
    public void DeletesSlideAndClearsReferencesFromRemainingSlides()
    {
        OdfDocument document = Read(PresentationXml());
        PresentationSlideEditingModel.DeleteSlide(
            document,
            document.Slides[0]);
        OdfSlide remaining = document.Slides.Single();
        OdfDrawingObject connector = remaining.DrawingObjects.Single(item =>
            item.Name == "External Connector");

        Assert.Multiple(() =>
        {
            Assert.That(remaining.Name, Is.EqualTo("Second"));
            Assert.That(remaining.NavigationOrder, Is.EqualTo("external"));
            Assert.That(remaining.IsNavigationOrderValid, Is.True);
            Assert.That(connector.Geometry.Routing.StartShapeId, Is.Empty);
            Assert.That(connector.Geometry.Routing.EndShapeId, Is.Empty);
            Assert.That(FindHref(remaining.Node ?? new OdfNode()), Is.Empty);
            Assert.That(FindTarget(remaining.Node ?? new OdfNode()), Is.Empty);
            Assert.That(document.DrawingObjects.Any(item =>
                item.Id == "shape1" || item.Id == "page1"), Is.False);
        });

        OdfDocument reread = ReadBytes(Write(document));
        OdfDrawingObject roundTripConnector = reread.Slides[0]
            .DrawingObjects.Single(item =>
                item.Name == "External Connector");
        Assert.Multiple(() =>
        {
            Assert.That(reread.Slides, Has.Length.EqualTo(1));
            Assert.That(roundTripConnector.Geometry.Routing.StartShapeId,
                Is.Empty);
            Assert.That(roundTripConnector.Geometry.Routing.EndShapeId,
                Is.Empty);
            Assert.That(FindHref(reread.Slides[0].Node ?? new OdfNode()),
                Is.Empty);
            Assert.That(FindTarget(reread.Slides[0].Node ?? new OdfNode()),
                Is.Empty);
        });
    }

    static string FindTarget(OdfNode node)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Smil
                && attribute.Name.LocalName == "targetElement")
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            string result = FindTarget(node.Children[index]);
            if (result.Length > 0)
            {
                return result;
            }
            index = index + 1;
        }
        return "";
    }

    static string FindHref(OdfNode node)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.XLink
                && attribute.Name.LocalName == "href")
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            string result = FindHref(node.Children[index]);
            if (result.Length > 0)
            {
                return result;
            }
            index = index + 1;
        }
        return "";
    }

    static OdfDocument Read(string xml)
    {
        return ReadBytes(Encoding.UTF8.GetBytes(xml));
    }

    static OdfDocument ReadBytes(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "slides.fodp",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static string PresentationXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + Namespaces()
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:body><office:presentation>"
            + "<presentation:header-decl presentation:name='Header1'>"
            + "Header</presentation:header-decl>"
            + "<draw:page draw:name='First' draw:id='page1' "
            + "xml:id='page-xml' draw:nav-order='shape2 shape1'>"
            + "<draw:rect draw:name='Shape One' draw:id='shape1'/>"
            + "<draw:connector draw:name='Shape Two' draw:id='shape2' "
            + "draw:start-shape='shape1'/>"
            + "<draw:frame draw:name='Internal Link'>"
            + "<draw:a xlink:href='#shape2'><draw:rect "
            + "draw:name='Linked Shape' draw:id='linked'/></draw:a>"
            + "</draw:frame>"
            + "<presentation:notes><draw:frame draw:name='Notes Frame' "
            + "draw:id='note1'><draw:text-box><text:p>Notes one</text:p>"
            + "</draw:text-box></draw:frame></presentation:notes>"
            + "<anim:par smil:targetElement='shape1'/>"
            + "</draw:page>"
            + "<draw:page draw:name='Second' draw:id='page2' "
            + "draw:nav-order='external shape1'>"
            + "<draw:connector draw:name='External Connector' "
            + "draw:id='external' draw:start-shape='shape1' "
            + "draw:end-shape='shape1'/>"
            + "<draw:frame draw:name='External Link'>"
            + "<draw:a xlink:href='#shape1'><draw:rect "
            + "draw:name='Second Shape' draw:id='second-shape'/></draw:a>"
            + "</draw:frame><anim:par smil:targetElement='shape1'/>"
            + "</draw:page>"
            + "<presentation:settings/>"
            + "</office:presentation></office:body>"
            + "</office:document>";
    }

    static string Namespaces()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:anim='" + OdfNamespaces.Animation + "'"
            + " xmlns:smil='" + OdfNamespaces.Smil + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:xml='" + OdfNamespaces.Xml + "'";
    }
}
