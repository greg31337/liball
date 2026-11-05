using System.Text;

namespace odf.Tests;

public class ThreeDimensionalDrawingModelTests
{
    [Test]
    public void MaterializesScenesLightsPrimitivesAndMaterials()
    {
        OdfDocument document = ReadFlat(ThreeDimensionalXml());
        OdfDrawingObject scene = Drawing(document, "Scene");
        OdfDrawingObject cube = Drawing(document, "Cube");
        OdfDrawingObject sphere = Drawing(document, "Sphere");
        OdfDrawingObject rotate = Drawing(document, "Rotate");
        OdfDrawingObject extrude = Drawing(document, "Extrude");
        OdfThreeDimensionalCamera camera = scene.ThreeDimensional.Camera;
        OdfThreeDimensionalLight light =
            scene.ThreeDimensional.Lights.Single();
        OdfThreeDimensionalMaterial material =
            cube.ThreeDimensional.Material;

        Assert.Multiple(() =>
        {
            Assert.That(scene.Kind, Is.EqualTo(OdfDrawingKind.Scene3D));
            Assert.That(scene.Children,
                Is.EqualTo(new[] { cube, sphere, rotate, extrude }));
            Assert.That(camera.ViewReferencePoint.IsValid, Is.True);
            Assert.That(camera.ViewReferencePoint.X, Is.EqualTo(1));
            Assert.That(camera.ViewPlaneNormal.Z, Is.EqualTo(1));
            Assert.That(camera.ViewUpVector.Y, Is.EqualTo(1));
            Assert.That(camera.Projection,
                Is.EqualTo(OdfThreeDimensionalProjection.Perspective));
            Assert.That(camera.IsProjectionValid, Is.True);
            Assert.That(camera.Distance, Is.EqualTo("10cm"));
            Assert.That(camera.FocalLength, Is.EqualTo("5cm"));
            Assert.That(camera.ShadowSlant, Is.EqualTo("45deg"));
            Assert.That(camera.ShadeMode, Is.EqualTo("phong"));
            Assert.That(camera.AmbientColor, Is.EqualTo("#101010"));
            Assert.That(camera.IsLightingModeEnabled, Is.True);
            Assert.That(scene.ThreeDimensional.Transform,
                Is.EqualTo("rotatex(10)"));
            Assert.That(light.DiffuseColor, Is.EqualTo("#ffffff"));
            Assert.That(light.Direction.Y, Is.EqualTo(-1));
            Assert.That(light.IsEnabled, Is.True);
            Assert.That(light.IsSpecular, Is.False);
            Assert.That(cube.ThreeDimensional.MinimumEdge.X,
                Is.EqualTo(-1));
            Assert.That(cube.ThreeDimensional.MaximumEdge.Z,
                Is.EqualTo(1));
            Assert.That(sphere.ThreeDimensional.Center.X, Is.EqualTo(5));
            Assert.That(sphere.ThreeDimensional.Size.Z, Is.EqualTo(10));
            Assert.That(rotate.Geometry.Path.IsValid, Is.True);
            Assert.That(rotate.Geometry.ViewBoxValue.Width,
                Is.EqualTo(100));
            Assert.That(extrude.Geometry.Path.IsValid, Is.True);
            Assert.That(material.Style, Is.Not.Null);
            Assert.That(material.AmbientColor, Is.EqualTo("#112233"));
            Assert.That(material.DiffuseColor, Is.EqualTo("#445566"));
            Assert.That(material.EmissiveColor, Is.EqualTo("#010203"));
            Assert.That(material.SpecularColor, Is.EqualTo("#abcdef"));
            Assert.That(material.Shininess, Is.EqualTo("25%"));
            Assert.That(material.BackfaceCulling,
                Is.EqualTo(OdfBackfaceCulling.Enabled));
            Assert.That(material.IsBackfaceCullingValid, Is.True);
        });
    }

    [Test]
    public void WritesTypedSceneLightPrimitiveAndMaterialChanges()
    {
        OdfDocument document = ReadFlat(ThreeDimensionalXml());
        OdfDrawingObject scene = Drawing(document, "Scene");
        OdfDrawingObject cube = Drawing(document, "Cube");
        OdfDrawingObject sphere = Drawing(document, "Sphere");
        scene.ThreeDimensional.Camera.Projection =
            OdfThreeDimensionalProjection.Parallel;
        scene.ThreeDimensional.Camera.ViewReferencePoint.X = 9;
        scene.ThreeDimensional.Camera.IsLightingModeEnabled = false;
        scene.ThreeDimensional.Lights[0].Direction.Z = 2;
        scene.ThreeDimensional.Lights[0].IsEnabled = false;
        scene.ThreeDimensional.Lights[0].IsSpecular = true;
        cube.ThreeDimensional.MaximumEdge.X = 4;
        cube.ThreeDimensional.Transform = "scale(3 3 3)";
        sphere.ThreeDimensional.Size.Z = 20;
        cube.ThreeDimensional.Material.DiffuseColor = "#123456";
        cube.ThreeDimensional.Material.BackfaceCulling =
            OdfBackfaceCulling.Disabled;

        OdfDocument reread = ReadFlat(Write(document, true));
        OdfDrawingObject changedScene = Drawing(reread, "Scene");
        OdfDrawingObject changedCube = Drawing(reread, "Cube");
        OdfDrawingObject changedSphere = Drawing(reread, "Sphere");

        Assert.Multiple(() =>
        {
            Assert.That(changedScene.ThreeDimensional.Camera.Projection,
                Is.EqualTo(OdfThreeDimensionalProjection.Parallel));
            Assert.That(changedScene.ThreeDimensional.Camera
                .ViewReferencePoint.LexicalValue,
                Is.EqualTo("(9 2 3)"));
            Assert.That(changedScene.ThreeDimensional.Camera
                .IsLightingModeEnabled, Is.False);
            Assert.That(changedScene.ThreeDimensional.Lights[0]
                .Direction.LexicalValue, Is.EqualTo("(0 -1 2)"));
            Assert.That(changedScene.ThreeDimensional.Lights[0].IsEnabled,
                Is.False);
            Assert.That(changedScene.ThreeDimensional.Lights[0].IsSpecular,
                Is.True);
            Assert.That(changedCube.ThreeDimensional.MaximumEdge.X,
                Is.EqualTo(4));
            Assert.That(changedCube.ThreeDimensional.Transform,
                Is.EqualTo("scale(3 3 3)"));
            Assert.That(changedSphere.ThreeDimensional.Size.Z,
                Is.EqualTo(20));
            Assert.That(changedCube.ThreeDimensional.Material.DiffuseColor,
                Is.EqualTo("#123456"));
            Assert.That(changedCube.ThreeDimensional.Material.BackfaceCulling,
                Is.EqualTo(OdfBackfaceCulling.Disabled));
            Assert.That(changedSphere.ThreeDimensional.Material.DiffuseColor,
                Is.EqualTo("#123456"));
        });
    }

    [Test]
    public void PreservesInvalidThreeDimensionalMetadata()
    {
        OdfDocument document = ReadFlat(InvalidThreeDimensionalXml());
        OdfDrawingObject scene = Drawing(document, "Invalid Scene");
        OdfDrawingObject cube = Drawing(document, "Invalid Cube");

        Assert.Multiple(() =>
        {
            Assert.That(scene.ThreeDimensional.Camera
                .ViewReferencePoint.IsValid, Is.False);
            Assert.That(scene.ThreeDimensional.Camera.IsProjectionValid,
                Is.False);
            Assert.That(scene.ThreeDimensional.Camera.IsLightingModeValid,
                Is.False);
            Assert.That(scene.ThreeDimensional.Lights[0].Direction.IsValid,
                Is.False);
            Assert.That(scene.ThreeDimensional.Lights[0].IsEnabledValid,
                Is.False);
            Assert.That(cube.ThreeDimensional.MinimumEdge.IsValid,
                Is.False);
            Assert.That(cube.ThreeDimensional.Material
                .IsBackfaceCullingValid, Is.False);
        });

        OdfDocument reread = ReadFlat(Write(document, false));
        Assert.Multiple(() =>
        {
            Assert.That(reread.DrawingObjects.Single(item =>
                item.Name == "Invalid Scene").ThreeDimensional.Camera
                .ViewReferencePoint.LexicalValue, Is.EqualTo("(1 2)"));
            Assert.That(Drawing(reread, "Invalid Cube").ThreeDimensional
                .MinimumEdge.LexicalValue, Is.EqualTo("bad-vector"));
            Assert.That(Drawing(reread, "Invalid Cube").ThreeDimensional
                .Material.BackfaceCullingValue, Is.EqualTo("sometimes"));
        });
    }

    static OdfDrawingObject Drawing(OdfDocument document, string name)
    {
        return document.DrawingObjects.Single(item => item.Name == name);
    }

    static string Write(OdfDocument document, bool preserve)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true,
                PreserveLexicalValues = preserve
            });
        return Encoding.UTF8.GetString(bytes);
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "three-dimensional.fodg",
            new OdfLoadOptions());
    }

    static string ThreeDimensionalXml()
    {
        return FlatXml(
            "<office:automatic-styles>"
            + GraphicStyle("enabled")
            + "</office:automatic-styles>"
            + "<office:body><office:drawing>"
            + "<draw:page draw:name='Page'>"
            + "<dr3d:scene draw:name='Scene' dr3d:vrp='(1 2 3)' "
            + "dr3d:vpn='(0 0 1)' dr3d:vup='(0 1 0)' "
            + "dr3d:projection='perspective' dr3d:distance='10cm' "
            + "dr3d:focal-length='5cm' dr3d:shadow-slant='45deg' "
            + "dr3d:shade-mode='phong' dr3d:ambient-color='#101010' "
            + "dr3d:lighting-mode='true' dr3d:transform='rotatex(10)'>"
            + "<dr3d:light dr3d:diffuse-color='#ffffff' "
            + "dr3d:direction='(0 -1 1)' dr3d:enabled='true' "
            + "dr3d:specular='false'/>"
            + "<dr3d:cube draw:name='Cube' draw:style-name='ThreeD' "
            + "dr3d:min-edge='(-1 -1 -1)' dr3d:max-edge='(1 1 1)' "
            + "dr3d:transform='scale(2 2 2)'/>"
            + "<dr3d:sphere draw:name='Sphere' draw:style-name='ThreeD' "
            + "dr3d:center='(5 5 5)' dr3d:size='(10 10 10)'/>"
            + "<dr3d:rotate draw:name='Rotate' draw:style-name='ThreeD' "
            + "svg:viewBox='0 0 100 100' svg:d='M0 0L100 100'/>"
            + "<dr3d:extrude draw:name='Extrude' draw:style-name='ThreeD' "
            + "svg:viewBox='0 0 50 50' svg:d='M0 0L50 0L50 50z'/>"
            + "</dr3d:scene></draw:page>"
            + "</office:drawing></office:body>");
    }

    static string InvalidThreeDimensionalXml()
    {
        return FlatXml(
            "<office:automatic-styles>"
            + GraphicStyle("sometimes")
            + "</office:automatic-styles>"
            + "<office:body><office:drawing>"
            + "<draw:page draw:name='Page'>"
            + "<dr3d:scene draw:name='Invalid Scene' dr3d:vrp='(1 2)' "
            + "dr3d:projection='fisheye' dr3d:lighting-mode='sometimes'>"
            + "<dr3d:light dr3d:direction='north' "
            + "dr3d:enabled='sometimes'/>"
            + "<dr3d:cube draw:name='Invalid Cube' draw:style-name='ThreeD' "
            + "dr3d:min-edge='bad-vector'/>"
            + "</dr3d:scene></draw:page>"
            + "</office:drawing></office:body>");
    }

    static string GraphicStyle(string backface)
    {
        return "<style:style style:name='ThreeD' style:family='graphic'>"
            + "<style:graphic-properties dr3d:ambient-color='#112233' "
            + "dr3d:diffuse-color='#445566' "
            + "dr3d:emissive-color='#010203' "
            + "dr3d:specular-color='#abcdef' dr3d:shininess='25%' "
            + "dr3d:backface-culling='" + backface + "'/>"
            + "</style:style>";
    }

    static string FlatXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:dr3d='" + OdfNamespaces.Dr3d + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Graphics + "'>"
            + body
            + "</office:document>";
    }
}
