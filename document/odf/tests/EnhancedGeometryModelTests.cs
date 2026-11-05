using System.Text;

namespace odf.Tests;

public class EnhancedGeometryModelTests
{
    [Test]
    public void MaterializesPresetModifiersEquationsHandlesAndGluePoints()
    {
        OdfDocument document = ReadFlat(GeometryXml());
        OdfDrawingObject custom = Shape(document, "Custom");
        OdfEnhancedGeometry enhanced = custom.Geometry.EnhancedGeometry!;

        Assert.Multiple(() =>
        {
            Assert.That(custom.Kind, Is.EqualTo(OdfDrawingKind.CustomShape));
            Assert.That(enhanced.Preset.Name, Is.EqualTo("star5"));
            Assert.That(enhanced.Preset.IsSpecified, Is.True);
            Assert.That(enhanced.Preset.IsNonPrimitive, Is.False);
            Assert.That(enhanced.ViewBox, Is.EqualTo("0 0 1000 1000"));
            Assert.That(enhanced.ModifierText, Is.EqualTo("100 200 $0"));
            Assert.That(enhanced.Modifiers.Select(item => item.Value),
                Is.EqualTo(new[] { "100", "200", "$0" }));
            Assert.That(enhanced.EnhancedPath,
                Is.EqualTo("M 0 0 L ?f0 ?f1 Z"));
            Assert.That(enhanced.TextAreas, Is.EqualTo("0 0 1000 1000"));
            Assert.That(enhanced.GluePoints, Is.EqualTo("0 500 1000 500"));
            Assert.That(enhanced.GluePointType, Is.EqualTo("segments"));
            Assert.That(enhanced.GluePointLeavingDirections,
                Is.EqualTo("180 0"));
            Assert.That(enhanced.MirrorVertical, Is.True);
            Assert.That(enhanced.MirrorVerticalValid, Is.True);
            Assert.That(enhanced.MirrorHorizontal, Is.False);
            Assert.That(enhanced.Equations, Has.Length.EqualTo(2));
            Assert.That(enhanced.Equations[0].Name, Is.EqualTo("f0"));
            Assert.That(enhanced.Equations[0].Formula, Is.EqualTo("$0 / 2"));
            Assert.That(enhanced.Handles, Has.Length.EqualTo(3));
            Assert.That(enhanced.Handles[0].Kind,
                Is.EqualTo(OdfGeometryHandleKind.Cartesian));
            Assert.That(enhanced.Handles[0].PositionX, Is.EqualTo("$0"));
            Assert.That(enhanced.Handles[0].RangeXMaximum,
                Is.EqualTo("1000"));
            Assert.That(enhanced.Handles[0].MirrorVertical, Is.True);
            Assert.That(enhanced.Handles[1].Kind,
                Is.EqualTo(OdfGeometryHandleKind.Polar));
            Assert.That(enhanced.Handles[1].PolarAngle, Is.EqualTo("$1"));
            Assert.That(enhanced.Handles[2].Kind,
                Is.EqualTo(OdfGeometryHandleKind.Legacy));
            Assert.That(custom.Geometry.GluePoints, Has.Length.EqualTo(2));
            Assert.That(custom.Geometry.GluePoints[0].Id, Is.EqualTo(4));
            Assert.That(custom.Geometry.GluePoints[0].IsIdValid, Is.True);
            Assert.That(custom.Geometry.GluePoints[0].Align,
                Is.EqualTo("center"));
            Assert.That(custom.Geometry.GluePoints[1].IsIdValid, Is.False);
        });
    }

    [Test]
    public void ResolvesConnectorRouteEndpointsAndBrokenTargets()
    {
        OdfDocument document = ReadFlat(GeometryXml());
        OdfDrawingObject custom = Shape(document, "Custom");
        OdfDrawingObject target = Shape(document, "Target");
        OdfConnectorRouting route = Shape(document, "Connector")
            .Geometry.Routing;
        OdfConnectorRouting broken = Shape(document, "Broken Connector")
            .Geometry.Routing;

        Assert.Multiple(() =>
        {
            Assert.That(route.Type, Is.EqualTo("curve"));
            Assert.That(route.StartShapeId, Is.EqualTo("custom1"));
            Assert.That(route.EndShapeId, Is.EqualTo("target1"));
            Assert.That(route.StartShape, Is.SameAs(custom));
            Assert.That(route.EndShape, Is.SameAs(target));
            Assert.That(route.StartGluePoint, Is.EqualTo("4"));
            Assert.That(route.EndGluePoint, Is.EqualTo("5"));
            Assert.That(route.LineSkew, Is.EqualTo("1cm 2cm 3cm"));
            Assert.That(route.PathData, Is.EqualTo("M 0 0 C 1 1 2 2 3 3"));
            Assert.That(route.HasBrokenStartShape, Is.False);
            Assert.That(route.HasBrokenEndShape, Is.False);
            Assert.That(broken.HasBrokenStartShape, Is.True);
            Assert.That(broken.HasBrokenEndShape, Is.True);
        });
    }

    [Test]
    public void WritesEnhancedGeometryGluePointAndRoutingChanges()
    {
        OdfDocument document = ReadFlat(GeometryXml());
        OdfEnhancedGeometry enhanced = Shape(document, "Custom")
            .Geometry.EnhancedGeometry!;
        OdfGluePoint point = Shape(document, "Custom")
            .Geometry.GluePoints[0];
        OdfConnectorRouting route = Shape(document, "Connector")
            .Geometry.Routing;
        enhanced.Preset.Name = "cloud";
        enhanced.Modifiers[1].Value = "250";
        enhanced.EnhancedPath = "M 0 0 L 500 500 Z";
        enhanced.MirrorVertical = false;
        enhanced.Equations[0].Formula = "$0 * 2";
        enhanced.Handles[0].PositionX = "$2";
        enhanced.Handles[0].IsSwitched = true;
        point.Id = 7;
        point.X = "75%";
        point.EscapeDirection = "left";
        route.Type = "lines";
        route.StartGluePoint = "7";
        route.EndShapeId = "custom1";

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        OdfDrawingObject changedCustom = Shape(reread, "Custom");
        OdfEnhancedGeometry changed =
            changedCustom.Geometry.EnhancedGeometry!;
        OdfConnectorRouting changedRoute = Shape(reread, "Connector")
            .Geometry.Routing;

        Assert.Multiple(() =>
        {
            Assert.That(changed.Preset.Name, Is.EqualTo("cloud"));
            Assert.That(changed.ModifierText, Is.EqualTo("100 250 $0"));
            Assert.That(changed.Modifiers[1].Value, Is.EqualTo("250"));
            Assert.That(changed.EnhancedPath,
                Is.EqualTo("M 0 0 L 500 500 Z"));
            Assert.That(changed.MirrorVertical, Is.False);
            Assert.That(changed.Equations[0].Formula, Is.EqualTo("$0 * 2"));
            Assert.That(changed.Handles[0].PositionX, Is.EqualTo("$2"));
            Assert.That(changed.Handles[0].IsSwitched, Is.True);
            Assert.That(changedCustom.Geometry.GluePoints[0].Id,
                Is.EqualTo(7));
            Assert.That(changedCustom.Geometry.GluePoints[0].X,
                Is.EqualTo("75%"));
            Assert.That(changedCustom.Geometry.GluePoints[0].EscapeDirection,
                Is.EqualTo("left"));
            Assert.That(changedRoute.Type, Is.EqualTo("lines"));
            Assert.That(changedRoute.StartGluePoint, Is.EqualTo("7"));
            Assert.That(changedRoute.EndShape, Is.SameAs(changedCustom));
        });
    }

    static OdfDrawingObject Shape(OdfDocument document, string name)
    {
        return document.DrawingObjects.Single(item => item.Name == name);
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
            "geometry.fodg",
            new OdfLoadOptions());
    }

    static string GeometryXml()
    {
        return FlatXml(
            "<office:body><office:drawing>"
            + "<draw:page draw:name='Page' draw:master-page-name='Standard'>"
            + "<draw:custom-shape draw:name='Custom' draw:id='custom1'>"
            + "<draw:enhanced-geometry draw:type='star5' "
            + "svg:viewBox='0 0 1000 1000' draw:modifiers='100 200 $0' "
            + "draw:enhanced-path='M 0 0 L ?f0 ?f1 Z' "
            + "draw:path-stretchpoint-x='500' draw:path-stretchpoint-y='500' "
            + "draw:text-areas='0 0 1000 1000' "
            + "draw:glue-points='0 500 1000 500' "
            + "draw:glue-point-type='segments' "
            + "draw:glue-point-leaving-directions='180 0' "
            + "draw:mirror-vertical='true' draw:mirror-horizontal='false' "
            + "draw:text-rotate-angle='15deg'>"
            + "<draw:equation draw:name='f0' draw:formula='$0 / 2'/>"
            + "<draw:equation draw:name='f1' draw:formula='$1 + 10'/>"
            + "<draw:handle draw:handle-position-x='$0' "
            + "draw:handle-position-y='$1' draw:handle-range-x-minimum='0' "
            + "draw:handle-range-x-maximum='1000' "
            + "draw:handle-mirror-vertical='true'/>"
            + "<draw:handle draw:handle-polar-pole-x='500' "
            + "draw:handle-polar-pole-y='500' draw:handle-polar-radius='$0' "
            + "draw:handle-polar-angle='$1' "
            + "draw:handle-radius-range-minimum='0' "
            + "draw:handle-radius-range-maximum='500'/>"
            + "<draw:handle draw:handle-position='$0 $1'/>"
            + "</draw:enhanced-geometry>"
            + "<draw:glue-point draw:id='4' svg:x='50%' svg:y='0%' "
            + "draw:align='center' draw:escape-direction='right'/>"
            + "<draw:glue-point draw:id='bad' svg:x='0%' svg:y='50%' "
            + "draw:escape-direction='auto'/>"
            + "</draw:custom-shape>"
            + "<draw:rect draw:name='Target' draw:id='target1'/>"
            + "<draw:connector draw:name='Connector' draw:type='curve' "
            + "draw:start-shape='custom1' draw:end-shape='target1' "
            + "draw:start-glue-point='4' draw:end-glue-point='5' "
            + "draw:line-skew='1cm 2cm 3cm' "
            + "svg:d='M 0 0 C 1 1 2 2 3 3'/>"
            + "<draw:connector draw:name='Broken Connector' "
            + "draw:start-shape='missing1' draw:end-shape='missing2'/>"
            + "</draw:page></office:drawing></office:body>");
    }

    static string FlatXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Graphics + "'>"
            + body
            + "</office:document>";
    }
}
