using System.Text;

namespace odf.Tests;

public class DrawingDocumentModelTests
{
    [Test]
    public void MaterializesDrawingHierarchyMetadataAndReferences()
    {
        OdfDocument document = ReadFlat(DrawingXml());
        OdfDrawingObject page = Object(document, OdfDrawingKind.Page, "Page 1");
        OdfDrawingObject layer = Object(document, OdfDrawingKind.Layer, "layout");
        OdfDrawingObject group = Object(document, OdfDrawingKind.Group, "Group 1");
        OdfDrawingObject frame = Object(document, OdfDrawingKind.Frame, "Frame 1");
        OdfDrawingObject rectangle = Object(document, OdfDrawingKind.Rectangle, "Rectangle 1");
        OdfDrawingObject connector = Object(document, OdfDrawingKind.Connector, "Connector 1");
        OdfDrawingReference copy = frame.References.Single(
            item => item.Kind == OdfDrawingReferenceKind.CopyOf);
        OdfDrawingReference missing = connector.References.Single(
            item => item.Kind == OdfDrawingReferenceKind.Hyperlink);

        Assert.Multiple(() =>
        {
            Assert.That(document.DrawingObjects, Has.Length.EqualTo(6));
            Assert.That(page.Accessibility.Title, Is.EqualTo("Canvas title"));
            Assert.That(page.MasterPageName, Is.EqualTo("Standard"));
            Assert.That(page.References.Count(
                item => item.Kind == OdfDrawingReferenceKind.NavigationOrder),
                Is.EqualTo(2));
            Assert.That(layer.IsProtected, Is.True);
            Assert.That(layer.Display, Is.EqualTo("screen"));
            Assert.That(layer.Accessibility.Description,
                Is.EqualTo("Layout layer"));
            Assert.That(group.Parent, Is.SameAs(page));
            Assert.That(group.Children,
                Is.EqualTo(new[] { frame, rectangle, connector }));
            Assert.That(group.ZOrderedChildren,
                Is.EqualTo(new[] { rectangle, frame, connector }));
            Assert.That(frame.Page, Is.SameAs(page));
            Assert.That(frame.Layer, Is.SameAs(layer));
            Assert.That(frame.Anchor.Kind, Is.EqualTo(OdfTextAnchorKind.Page));
            Assert.That(frame.Anchor.PageNumber, Is.EqualTo(2));
            Assert.That(frame.Anchor.IsPageNumberValid, Is.True);
            Assert.That(frame.Anchor.EndCellAddress, Is.EqualTo("Sheet1.A1"));
            Assert.That(frame.ZOrder.Index, Is.EqualTo(3));
            Assert.That(frame.ZOrder.IsValid, Is.True);
            Assert.That(frame.Transform.Expression,
                Is.EqualTo("translate(1cm 2cm)"));
            Assert.That(frame.Accessibility.Title, Is.EqualTo("Frame title"));
            Assert.That(frame.Events, Has.Length.EqualTo(1));
            Assert.That(frame.Events[0].Kind,
                Is.EqualTo(OdfDrawingEventKind.Script));
            Assert.That(frame.Events[0].MacroName, Is.EqualTo("Standard.Run"));
            Assert.That(copy.TargetObject, Is.SameAs(rectangle));
            Assert.That(copy.IsBroken, Is.False);
            Assert.That(connector.References.Single(
                item => item.Kind == OdfDrawingReferenceKind.ConnectorStart)
                .TargetObject, Is.SameAs(frame));
            Assert.That(missing.IsExternal, Is.False);
            Assert.That(missing.IsBroken, Is.True);
            Assert.That(rectangle.Events[0].Kind,
                Is.EqualTo(OdfDrawingEventKind.Presentation));
            Assert.That(rectangle.Events[0].Action,
                Is.EqualTo("next-page"));
        });
    }

    [Test]
    public void ReportsInvalidStackingAnchorsAndBrokenLayers()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:body><office:drawing>"
            + "<draw:page draw:name='Page' draw:master-page-name='Standard'>"
            + "<draw:frame draw:name='Invalid' draw:layer='missing' "
            + "draw:z-index='-1' text:anchor-type='page' "
            + "text:anchor-page-number='0'/>"
            + "</draw:page></office:drawing></office:body>"));
        OdfDrawingObject frame = Object(
            document,
            OdfDrawingKind.Frame,
            "Invalid");
        OdfDrawingReference layer = frame.References.Single(
            item => item.Kind == OdfDrawingReferenceKind.Layer);

        Assert.Multiple(() =>
        {
            Assert.That(frame.ZOrder.IsSpecified, Is.True);
            Assert.That(frame.ZOrder.IsValid, Is.False);
            Assert.That(frame.Anchor.HasPageNumber, Is.True);
            Assert.That(frame.Anchor.IsPageNumberValid, Is.False);
            Assert.That(frame.Layer, Is.Null);
            Assert.That(layer.IsBroken, Is.True);
        });
    }

    [Test]
    public void WritesDrawingModelChangesAndRebuildsResolvedState()
    {
        OdfDocument document = ReadFlat(DrawingXml());
        OdfDrawingObject layer = Object(document, OdfDrawingKind.Layer, "layout");
        OdfDrawingObject frame = Object(document, OdfDrawingKind.Frame, "Frame 1");
        OdfDrawingReference copy = frame.References.Single(
            item => item.Kind == OdfDrawingReferenceKind.CopyOf);
        layer.IsProtected = false;
        frame.Name = "Changed Frame";
        frame.Anchor.Kind = OdfTextAnchorKind.Character;
        frame.Anchor.PageNumber = 4;
        frame.ZOrder.Index = 8;
        frame.Transform.Expression = "rotate(0.5)";
        frame.Accessibility.Title = "Changed title";
        frame.Accessibility.Description = "Added description";
        frame.Events[0].MacroName = "Standard.Changed";
        copy.Target = "shapeA";

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        OdfDrawingObject changedLayer = Object(
            reread,
            OdfDrawingKind.Layer,
            "layout");
        OdfDrawingObject changed = Object(
            reread,
            OdfDrawingKind.Frame,
            "Changed Frame");

        Assert.Multiple(() =>
        {
            Assert.That(changedLayer.IsProtected, Is.False);
            Assert.That(changed.Anchor.Kind,
                Is.EqualTo(OdfTextAnchorKind.Character));
            Assert.That(changed.Anchor.PageNumber, Is.EqualTo(4));
            Assert.That(changed.ZOrder.Index, Is.EqualTo(8));
            Assert.That(changed.Transform.Expression,
                Is.EqualTo("rotate(0.5)"));
            Assert.That(changed.Accessibility.Title,
                Is.EqualTo("Changed title"));
            Assert.That(changed.Accessibility.Description,
                Is.EqualTo("Added description"));
            Assert.That(changed.Events[0].MacroName,
                Is.EqualTo("Standard.Changed"));
            Assert.That(changed.References.Single(
                item => item.Kind == OdfDrawingReferenceKind.CopyOf)
                .TargetObject, Is.SameAs(changed));
        });
    }

    static OdfDrawingObject Object(
        OdfDocument document,
        OdfDrawingKind kind,
        string name)
    {
        return document.DrawingObjects.Single(
            item => item.Kind == kind && item.Name == name);
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

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "drawing.fodg",
            new OdfLoadOptions());
    }

    static string DrawingXml()
    {
        return FlatXml(
            "<office:body><office:drawing>"
            + "<draw:page draw:name='Page 1' draw:master-page-name='Standard' "
            + "xml:id='page1' draw:nav-order='shapeA shapeB'>"
            + "<svg:title>Canvas title</svg:title>"
            + "<draw:layer-set><draw:layer draw:name='layout' "
            + "draw:protected='true' draw:display='screen'>"
            + "<svg:desc>Layout layer</svg:desc></draw:layer></draw:layer-set>"
            + "<draw:g draw:name='Group 1' draw:id='group1' draw:z-index='5'>"
            + "<draw:frame draw:name='Frame 1' draw:id='shapeA' "
            + "draw:layer='layout' draw:z-index='3' "
            + "draw:copy-of='shapeB' draw:transform='translate(1cm 2cm)' "
            + "text:anchor-type='page' text:anchor-page-number='2' "
            + "table:end-cell-address='Sheet1.A1' table:end-x='1cm' "
            + "table:end-y='2cm' svg:x='3cm' svg:y='4cm' "
            + "svg:width='5cm' svg:height='6cm'>"
            + "<office:event-listeners><script:event-listener "
            + "script:event-name='dom:click' script:language='ooo:script' "
            + "script:macro-name='Standard.Run'/></office:event-listeners>"
            + "<svg:title>Frame title</svg:title></draw:frame>"
            + "<draw:rect draw:name='Rectangle 1' xml:id='shapeB' "
            + "draw:z-index='1' draw:caption-id='shapeA'>"
            + "<office:event-listeners><presentation:event-listener "
            + "script:event-name='dom:click' presentation:action='next-page'/>"
            + "</office:event-listeners><svg:desc>Rectangle description</svg:desc>"
            + "</draw:rect>"
            + "<draw:connector draw:name='Connector 1' draw:start-shape='shapeA' "
            + "draw:end-shape='shapeB' xlink:href='#missing'/>"
            + "</draw:g></draw:page>"
            + "</office:drawing></office:body>");
    }

    static string FlatXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:script='" + OdfNamespaces.Script + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Graphics + "'>"
            + body
            + "</office:document>";
    }
}
