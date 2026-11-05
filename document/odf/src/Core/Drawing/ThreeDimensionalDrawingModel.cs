using System.Collections.Generic;

namespace odf;

internal static class ThreeDimensionalDrawingModel
{
    internal static OdfThreeDimensionalGeometry Create(
        OdfNode node,
        OdfDrawingKind kind)
    {
        OdfThreeDimensionalGeometry result =
            new OdfThreeDimensionalGeometry();
        result.Transform = Read(node, "transform");
        result.MinimumEdge = ParseVector(Read(node, "min-edge"));
        result.MaximumEdge = ParseVector(Read(node, "max-edge"));
        result.Center = ParseVector(Read(node, "center"));
        result.Size = ParseVector(Read(node, "size"));
        if (kind == OdfDrawingKind.Scene3D)
        {
            result.Camera = CreateCamera(node);
            result.Lights = CreateLights(node);
        }
        result.Node = node;
        result.OriginalTransform = result.Transform;
        return result;
    }

    internal static void ResolveMaterials(
        OdfDocument document,
        OdfDrawingObject[] drawings)
    {
        int index = 0;
        while (index < drawings.Length)
        {
            OdfDrawingObject drawing = drawings[index];
            if (IsThreeDimensional(drawing.Kind))
            {
                OdfStyle? style = FindStyle(document, drawing);
                drawing.ThreeDimensional.Material = CreateMaterial(
                    document,
                    style);
            }
            index = index + 1;
        }
    }

    internal static void Apply(
        OdfDrawingObject drawing,
        OdfSaveOptions options)
    {
        OdfThreeDimensionalGeometry geometry = drawing.ThreeDimensional;
        if (geometry == null || geometry.Node == null)
        {
            return;
        }
        WriteChanged(
            geometry.Node,
            "transform",
            geometry.Transform,
            geometry.OriginalTransform);
        if (drawing.Kind == OdfDrawingKind.Scene3D)
        {
            ApplyCamera(geometry.Camera, geometry.Node, options);
            ApplyLights(geometry.Lights, options);
        }
        else if (drawing.Kind == OdfDrawingKind.Cube)
        {
            ApplyVector(
                geometry.MinimumEdge,
                geometry.Node,
                "min-edge",
                options);
            ApplyVector(
                geometry.MaximumEdge,
                geometry.Node,
                "max-edge",
                options);
        }
        else if (drawing.Kind == OdfDrawingKind.Sphere)
        {
            ApplyVector(
                geometry.Center,
                geometry.Node,
                "center",
                options);
            ApplyVector(
                geometry.Size,
                geometry.Node,
                "size",
                options);
        }
        ApplyMaterial(geometry.Material);
        geometry.OriginalTransform = geometry.Transform;
    }

    internal static OdfThreeDimensionalGeometry CreateSceneMetadata(
        OdfNode node)
    {
        return Create(node, OdfDrawingKind.Scene3D);
    }

    internal static void ApplySceneMetadata(
        OdfThreeDimensionalGeometry geometry)
    {
        if (geometry == null || geometry.Node == null)
        {
            return;
        }
        OdfDrawingObject drawing = new OdfDrawingObject();
        drawing.Kind = OdfDrawingKind.Scene3D;
        drawing.ThreeDimensional = geometry;
        Apply(drawing, new OdfSaveOptions());
    }

    static OdfThreeDimensionalCamera CreateCamera(OdfNode node)
    {
        OdfThreeDimensionalCamera result =
            new OdfThreeDimensionalCamera();
        result.ViewReferencePoint = ParseVector(Read(node, "vrp"));
        result.ViewPlaneNormal = ParseVector(Read(node, "vpn"));
        result.ViewUpVector = ParseVector(Read(node, "vup"));
        result.ProjectionValue = Read(node, "projection");
        RefreshProjection(result);
        result.Distance = Read(node, "distance");
        result.FocalLength = Read(node, "focal-length");
        result.ShadowSlant = Read(node, "shadow-slant");
        result.ShadeMode = Read(node, "shade-mode");
        result.AmbientColor = Read(node, "ambient-color");
        result.LightingModeValue = Read(node, "lighting-mode");
        RefreshLightingMode(result);
        CaptureCamera(result);
        return result;
    }

    static OdfThreeDimensionalLight[] CreateLights(OdfNode node)
    {
        List<OdfThreeDimensionalLight> result =
            new List<OdfThreeDimensionalLight>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, "light"))
            {
                result.Add(CreateLight(child));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfThreeDimensionalLight CreateLight(OdfNode node)
    {
        OdfThreeDimensionalLight result =
            new OdfThreeDimensionalLight();
        result.DiffuseColor = Read(node, "diffuse-color");
        result.Direction = ParseVector(Read(node, "direction"));
        result.EnabledValue = Read(node, "enabled");
        result.SpecularValue = Read(node, "specular");
        RefreshLight(result);
        result.Node = node;
        CaptureLight(result);
        return result;
    }

    static OdfThreeDimensionalVector ParseVector(string text)
    {
        OdfThreeDimensionalVector result =
            new OdfThreeDimensionalVector();
        result.LexicalValue = text;
        result.IsSpecified = text.Length > 0;
        if (result.IsSpecified)
        {
            OdfVectorValue parsed = OdfLexicalParser.ParseVector(text);
            result.IsValid = parsed.IsValid;
            if (parsed.IsValid)
            {
                result.X = parsed.X;
                result.Y = parsed.Y;
                result.Z = parsed.Z;
            }
        }
        CaptureVector(result);
        return result;
    }

    static OdfThreeDimensionalMaterial CreateMaterial(
        OdfDocument document,
        OdfStyle? style)
    {
        OdfThreeDimensionalMaterial result =
            new OdfThreeDimensionalMaterial();
        result.Style = style;
        if (style != null)
        {
            OdfNode? properties = EffectiveGraphicProperties(
                document,
                style);
            result.AmbientColor = ReadProperty(
                properties,
                "ambient-color");
            result.DiffuseColor = ReadProperty(
                properties,
                "diffuse-color");
            result.EmissiveColor = ReadProperty(
                properties,
                "emissive-color");
            result.SpecularColor = ReadProperty(
                properties,
                "specular-color");
            result.Shininess = ReadProperty(
                properties,
                "shininess");
            result.BackfaceCullingValue = ReadProperty(
                properties,
                "backface-culling");
        }
        RefreshBackface(result);
        CaptureMaterial(result);
        return result;
    }

    static OdfStyle? FindStyle(
        OdfDocument document,
        OdfDrawingObject drawing)
    {
        if (drawing.StyleName.Length == 0)
        {
            return null;
        }
        OdfStyleFamily family = drawing.StyleNamespace
                == OdfNamespaces.Presentation
            ? OdfStyleFamily.Presentation
            : OdfStyleFamily.Graphic;
        OdfStyle? fallback = null;
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == drawing.StyleName && style.Family == family)
            {
                if (style.PartPath == drawing.PartPath)
                {
                    return style;
                }
                if (fallback == null)
                {
                    fallback = style;
                }
            }
            index = index + 1;
        }
        return fallback;
    }

    static void ApplyCamera(
        OdfThreeDimensionalCamera camera,
        OdfNode node,
        OdfSaveOptions options)
    {
        if (camera == null)
        {
            return;
        }
        ApplyVector(camera.ViewReferencePoint, node, "vrp", options);
        ApplyVector(camera.ViewPlaneNormal, node, "vpn", options);
        ApplyVector(camera.ViewUpVector, node, "vup", options);
        string projection = camera.ProjectionValue;
        if (camera.Projection != camera.OriginalProjection
            && projection == camera.OriginalProjectionValue)
        {
            projection = ProjectionText(camera.Projection);
            camera.ProjectionValue = projection;
        }
        else if (!options.PreserveLexicalValues
            && camera.Projection != OdfThreeDimensionalProjection.Unknown)
        {
            projection = ProjectionText(camera.Projection);
            camera.ProjectionValue = projection;
        }
        WriteChanged(
            node,
            "projection",
            projection,
            camera.OriginalProjectionValue);
        WriteChanged(node, "distance", camera.Distance, camera.OriginalDistance);
        WriteChanged(
            node,
            "focal-length",
            camera.FocalLength,
            camera.OriginalFocalLength);
        WriteChanged(
            node,
            "shadow-slant",
            camera.ShadowSlant,
            camera.OriginalShadowSlant);
        WriteChanged(
            node,
            "shade-mode",
            camera.ShadeMode,
            camera.OriginalShadeMode);
        WriteChanged(
            node,
            "ambient-color",
            camera.AmbientColor,
            camera.OriginalAmbientColor);
        string lighting = camera.LightingModeValue;
        if (camera.IsLightingModeEnabled
                != camera.OriginalIsLightingModeEnabled
            && lighting == camera.OriginalLightingModeValue)
        {
            lighting = camera.IsLightingModeEnabled ? "true" : "false";
            camera.LightingModeValue = lighting;
        }
        WriteChanged(
            node,
            "lighting-mode",
            lighting,
            camera.OriginalLightingModeValue);
        RefreshProjection(camera);
        RefreshLightingMode(camera);
        CaptureCamera(camera);
    }

    static void ApplyLights(
        OdfThreeDimensionalLight[] lights,
        OdfSaveOptions options)
    {
        if (lights == null)
        {
            return;
        }
        int index = 0;
        while (index < lights.Length)
        {
            OdfThreeDimensionalLight light = lights[index];
            if (light != null && light.Node != null)
            {
                WriteChanged(
                    light.Node,
                    "diffuse-color",
                    light.DiffuseColor,
                    light.OriginalDiffuseColor);
                ApplyVector(
                    light.Direction,
                    light.Node,
                    "direction",
                    options);
                string enabled = light.EnabledValue;
                if (light.IsEnabled != light.OriginalIsEnabled
                    && enabled == light.OriginalEnabledValue)
                {
                    enabled = light.IsEnabled ? "true" : "false";
                    light.EnabledValue = enabled;
                }
                WriteChanged(
                    light.Node,
                    "enabled",
                    enabled,
                    light.OriginalEnabledValue);
                string specular = light.SpecularValue;
                if (light.IsSpecular != light.OriginalIsSpecular
                    && specular == light.OriginalSpecularValue)
                {
                    specular = light.IsSpecular ? "true" : "false";
                    light.SpecularValue = specular;
                }
                WriteChanged(
                    light.Node,
                    "specular",
                    specular,
                    light.OriginalSpecularValue);
                RefreshLight(light);
                CaptureLight(light);
            }
            index = index + 1;
        }
    }

    static void ApplyVector(
        OdfThreeDimensionalVector vector,
        OdfNode node,
        string localName,
        OdfSaveOptions options)
    {
        if (vector == null)
        {
            return;
        }
        string value = vector.LexicalValue;
        string canonical = FormatVector(vector);
        bool typedChanged = canonical != vector.OriginalCanonicalValue
            || vector.IsSpecified != (vector.OriginalLexicalValue.Length > 0);
        if (vector.LexicalValue == vector.OriginalLexicalValue
            && typedChanged)
        {
            value = vector.IsSpecified ? canonical : "";
            vector.LexicalValue = value;
        }
        else if (!options.PreserveLexicalValues
            && vector.IsSpecified
            && vector.IsValid)
        {
            value = canonical;
            vector.LexicalValue = value;
        }
        WriteChanged(
            node,
            localName,
            value,
            vector.OriginalLexicalValue);
        OdfThreeDimensionalVector refreshed = ParseVector(value);
        CopyVector(refreshed, vector);
    }

    static void ApplyMaterial(OdfThreeDimensionalMaterial material)
    {
        if (material == null || material.Style == null
            || material.Style.Node == null)
        {
            return;
        }
        string backface = material.BackfaceCullingValue;
        if (material.BackfaceCulling != material.OriginalBackfaceCulling
            && backface == material.OriginalBackfaceCullingValue)
        {
            backface = BackfaceText(material.BackfaceCulling);
            material.BackfaceCullingValue = backface;
        }
        bool changed = material.AmbientColor != material.OriginalAmbientColor
            || material.DiffuseColor != material.OriginalDiffuseColor
            || material.EmissiveColor != material.OriginalEmissiveColor
            || material.SpecularColor != material.OriginalSpecularColor
            || material.Shininess != material.OriginalShininess
            || backface != material.OriginalBackfaceCullingValue;
        if (!changed)
        {
            return;
        }
        OdfStyle style = material.Style;
        if (material.AmbientColor != material.OriginalAmbientColor)
        {
            SetProperty(style, "ambient-color", material.AmbientColor);
        }
        if (material.DiffuseColor != material.OriginalDiffuseColor)
        {
            SetProperty(style, "diffuse-color", material.DiffuseColor);
        }
        if (material.EmissiveColor != material.OriginalEmissiveColor)
        {
            SetProperty(style, "emissive-color", material.EmissiveColor);
        }
        if (material.SpecularColor != material.OriginalSpecularColor)
        {
            SetProperty(style, "specular-color", material.SpecularColor);
        }
        if (material.Shininess != material.OriginalShininess)
        {
            SetProperty(style, "shininess", material.Shininess);
        }
        if (backface != material.OriginalBackfaceCullingValue)
        {
            SetProperty(style, "backface-culling", backface);
        }
        style.GraphicPropertiesNode =
            StylePropertyDocumentModel.SynchronizePropertySet(
                style.Node,
                "graphic-properties",
                style.GraphicProperties,
                style.GraphicPropertyChildren,
                style.OriginalGraphicPropertiesNode,
                style.OriginalGraphicPropertyAttributes,
                style.OriginalGraphicPropertyChildren);
        style.OriginalGraphicPropertiesNode = style.GraphicPropertiesNode;
        style.OriginalGraphicPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(
                style.GraphicProperties);
        style.OriginalGraphicPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                style.GraphicPropertyChildren);
        RefreshBackface(material);
        CaptureMaterial(material);
    }

    static void SetProperty(
        OdfStyle style,
        string localName,
        string value)
    {
        int found = -1;
        int index = 0;
        while (index < style.GraphicProperties.Length)
        {
            OdfStyleProperty property = style.GraphicProperties[index];
            if (property.Name.NamespaceName == OdfNamespaces.Dr3d
                && property.Name.LocalName == localName)
            {
                found = index;
                index = style.GraphicProperties.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        if (value.Length == 0)
        {
            if (found >= 0)
            {
                List<OdfStyleProperty> remaining =
                    new List<OdfStyleProperty>();
                index = 0;
                while (index < style.GraphicProperties.Length)
                {
                    if (index != found)
                    {
                        remaining.Add(style.GraphicProperties[index]);
                    }
                    index = index + 1;
                }
                style.GraphicProperties = remaining.ToArray();
            }
            return;
        }
        if (found >= 0)
        {
            style.GraphicProperties[found].LexicalValue = value;
            return;
        }
        OdfStyleProperty added = new OdfStyleProperty();
        added.Name.NamespaceName = OdfNamespaces.Dr3d;
        added.Name.LocalName = localName;
        added.Name.Prefix = OdfNamespaces.Dr3dPrefix;
        added.LexicalValue = value;
        OdfStyleProperty[] properties = new OdfStyleProperty[
            style.GraphicProperties.Length + 1];
        global::System.Array.Copy(
            style.GraphicProperties,
            properties,
            style.GraphicProperties.Length);
        properties[properties.Length - 1] = added;
        style.GraphicProperties = properties;
    }

    static void RefreshProjection(OdfThreeDimensionalCamera camera)
    {
        camera.Projection = ParseProjection(camera.ProjectionValue);
        camera.IsProjectionValid = camera.ProjectionValue.Length == 0
            || camera.Projection != OdfThreeDimensionalProjection.Unknown;
    }

    static OdfThreeDimensionalProjection ParseProjection(string value)
    {
        if (value == "parallel")
        {
            return OdfThreeDimensionalProjection.Parallel;
        }
        if (value == "perspective")
        {
            return OdfThreeDimensionalProjection.Perspective;
        }
        return OdfThreeDimensionalProjection.Unknown;
    }

    static string ProjectionText(OdfThreeDimensionalProjection value)
    {
        if (value == OdfThreeDimensionalProjection.Parallel)
        {
            return "parallel";
        }
        if (value == OdfThreeDimensionalProjection.Perspective)
        {
            return "perspective";
        }
        return "";
    }

    static void RefreshLightingMode(OdfThreeDimensionalCamera camera)
    {
        OdfLexicalValue parsed = OdfLexicalParser.ParseBoolean(
            camera.LightingModeValue);
        camera.IsLightingModeValid = camera.LightingModeValue.Length == 0
            || parsed.IsValid;
        camera.IsLightingModeEnabled = parsed.IsValid
            && parsed.BooleanValue;
    }

    static void RefreshLight(OdfThreeDimensionalLight light)
    {
        OdfLexicalValue enabled = OdfLexicalParser.ParseBoolean(
            light.EnabledValue);
        light.IsEnabledValid = light.EnabledValue.Length == 0
            || enabled.IsValid;
        light.IsEnabled = enabled.IsValid && enabled.BooleanValue;
        OdfLexicalValue specular = OdfLexicalParser.ParseBoolean(
            light.SpecularValue);
        light.IsSpecularValid = light.SpecularValue.Length == 0
            || specular.IsValid;
        light.IsSpecular = specular.IsValid && specular.BooleanValue;
    }

    static void RefreshBackface(OdfThreeDimensionalMaterial material)
    {
        material.BackfaceCulling = ParseBackface(
            material.BackfaceCullingValue);
        material.IsBackfaceCullingValid =
            material.BackfaceCullingValue.Length == 0
            || material.BackfaceCulling != OdfBackfaceCulling.Unknown;
    }

    static OdfBackfaceCulling ParseBackface(string value)
    {
        if (value == "enabled")
        {
            return OdfBackfaceCulling.Enabled;
        }
        if (value == "disabled")
        {
            return OdfBackfaceCulling.Disabled;
        }
        return OdfBackfaceCulling.Unknown;
    }

    static string BackfaceText(OdfBackfaceCulling value)
    {
        if (value == OdfBackfaceCulling.Enabled)
        {
            return "enabled";
        }
        if (value == OdfBackfaceCulling.Disabled)
        {
            return "disabled";
        }
        return "";
    }

    static string FormatVector(OdfThreeDimensionalVector value)
    {
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }
        OdfVectorValue formatted = new OdfVectorValue();
        formatted.X = value.X;
        formatted.Y = value.Y;
        formatted.Z = value.Z;
        formatted.IsValid = true;
        return OdfLexicalFormatter.FormatVector(formatted);
    }

    static void CopyVector(
        OdfThreeDimensionalVector source,
        OdfThreeDimensionalVector target)
    {
        target.LexicalValue = source.LexicalValue;
        target.X = source.X;
        target.Y = source.Y;
        target.Z = source.Z;
        target.IsSpecified = source.IsSpecified;
        target.IsValid = source.IsValid;
        target.OriginalLexicalValue = source.OriginalLexicalValue;
        target.OriginalCanonicalValue = source.OriginalCanonicalValue;
    }

    static void CaptureVector(OdfThreeDimensionalVector vector)
    {
        vector.OriginalLexicalValue = vector.LexicalValue;
        vector.OriginalCanonicalValue = vector.IsValid
            ? FormatVector(vector)
            : "";
    }

    static void CaptureCamera(OdfThreeDimensionalCamera camera)
    {
        camera.OriginalProjectionValue = camera.ProjectionValue;
        camera.OriginalProjection = camera.Projection;
        camera.OriginalDistance = camera.Distance;
        camera.OriginalFocalLength = camera.FocalLength;
        camera.OriginalShadowSlant = camera.ShadowSlant;
        camera.OriginalShadeMode = camera.ShadeMode;
        camera.OriginalAmbientColor = camera.AmbientColor;
        camera.OriginalLightingModeValue = camera.LightingModeValue;
        camera.OriginalIsLightingModeEnabled =
            camera.IsLightingModeEnabled;
    }

    static void CaptureLight(OdfThreeDimensionalLight light)
    {
        light.OriginalDiffuseColor = light.DiffuseColor;
        light.OriginalEnabledValue = light.EnabledValue;
        light.OriginalIsEnabled = light.IsEnabled;
        light.OriginalSpecularValue = light.SpecularValue;
        light.OriginalIsSpecular = light.IsSpecular;
    }

    static void CaptureMaterial(OdfThreeDimensionalMaterial material)
    {
        material.OriginalAmbientColor = material.AmbientColor;
        material.OriginalDiffuseColor = material.DiffuseColor;
        material.OriginalEmissiveColor = material.EmissiveColor;
        material.OriginalSpecularColor = material.SpecularColor;
        material.OriginalShininess = material.Shininess;
        material.OriginalBackfaceCullingValue =
            material.BackfaceCullingValue;
        material.OriginalBackfaceCulling = material.BackfaceCulling;
    }

    static OdfNode? EffectiveGraphicProperties(
        OdfDocument document,
        OdfStyle style)
    {
        StyleResolutionResult resolution =
            StyleResolver.ResolveEffectiveProperties(document, style);
        int index = 0;
        while (index < resolution.EffectiveProperties.Length)
        {
            OdfNode node = resolution.EffectiveProperties[index];
            if (node.Name != null
                && node.Name.NamespaceName == OdfNamespaces.Style
                && node.Name.LocalName == "graphic-properties")
            {
                return node;
            }
            index = index + 1;
        }
        return null;
    }

    static string ReadProperty(
        OdfNode? properties,
        string localName)
    {
        if (properties == null)
        {
            return "";
        }
        int index = 0;
        while (index < properties.Attributes.Length)
        {
            OdfAttribute property = properties.Attributes[index];
            if (property.Name.NamespaceName == OdfNamespaces.Dr3d
                && property.Name.LocalName == localName)
            {
                return property.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static string Read(OdfNode node, string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Dr3d,
            localName);
    }

    static void WriteChanged(
        OdfNode node,
        string localName,
        string value,
        string original)
    {
        if (value == original)
        {
            return;
        }
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Dr3d,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Dr3d,
                localName,
                OdfNamespaces.Dr3dPrefix,
                value);
        }
    }

    static bool IsElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Dr3d
            && node.Name.LocalName == localName;
    }

    static bool IsThreeDimensional(OdfDrawingKind kind)
    {
        return kind == OdfDrawingKind.Scene3D
            || kind == OdfDrawingKind.Cube
            || kind == OdfDrawingKind.Sphere
            || kind == OdfDrawingKind.Rotate
            || kind == OdfDrawingKind.Extrude;
    }
}
