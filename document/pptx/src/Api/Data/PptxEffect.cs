using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents effect data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxEffect
{
    /// <summary>
    /// Identifies an outer shadow.
    /// </summary>
    public const string OuterShadow = "outer";
    /// <summary>
    /// Identifies an inner shadow.
    /// </summary>
    public const string InnerShadow = "inner";
    internal string Xml;
    /// <summary>
    /// Indicates whether the has shadow setting is enabled.
    /// </summary>
    public bool HasShadow;
    /// <summary>
    /// Indicates whether the has glow setting is enabled.
    /// </summary>
    public bool HasGlow;
    /// <summary>
    /// Indicates whether the has reflection setting is enabled.
    /// </summary>
    public bool HasReflection;
    /// <summary>
    /// Indicates whether a soft-edge effect is enabled.
    /// </summary>
    public bool HasSoftEdge;
    /// <summary>
    /// Indicates whether the has custom effect setting is enabled.
    /// </summary>
    public bool HasCustomEffect;
    /// <summary>
    /// Stores the shadow color value associated with this object.
    /// </summary>
    public PptxColor ShadowColor;
    /// <summary>
    /// Stores whether the shadow is an outer or inner shadow.
    /// </summary>
    public string ShadowType;
    /// <summary>
    /// Stores the shadow blur radius in English Metric Units (EMUs).
    /// </summary>
    public long ShadowBlurRadius;
    /// <summary>
    /// Stores the shadow distance in English Metric Units (EMUs).
    /// </summary>
    public long ShadowDistance;
    /// <summary>
    /// Stores the shadow direction value associated with this object.
    /// </summary>
    public int ShadowDirection;
    /// <summary>
    /// Stores the shadow alignment associated with this object.
    /// </summary>
    public string ShadowAlignment;
    /// <summary>
    /// Stores the shadow transparency value associated with this object.
    /// </summary>
    public int ShadowTransparency;
    /// <summary>
    /// Indicates whether the shadow rotates with its shape.
    /// </summary>
    public bool ShadowRotateWithShape;
    /// <summary>
    /// Stores horizontal shadow scale in thousandths of one percent.
    /// </summary>
    public int ShadowScaleX;
    /// <summary>
    /// Stores vertical shadow scale in thousandths of one percent.
    /// </summary>
    public int ShadowScaleY;
    /// <summary>
    /// Stores horizontal shadow skew in DrawingML angle units.
    /// </summary>
    public int ShadowSkewX;
    /// <summary>
    /// Stores vertical shadow skew in DrawingML angle units.
    /// </summary>
    public int ShadowSkewY;
    /// <summary>
    /// Stores the glow color value associated with this object.
    /// </summary>
    public PptxColor GlowColor;
    /// <summary>
    /// Stores the glow radius in English Metric Units (EMUs).
    /// </summary>
    public long GlowRadius;
    /// <summary>
    /// Stores the glow transparency value associated with this object.
    /// </summary>
    public int GlowTransparency;
    /// <summary>
    /// Stores the reflection blur radius in English Metric Units (EMUs).
    /// </summary>
    public long ReflectionBlurRadius;
    /// <summary>
    /// Stores the reflection distance in English Metric Units (EMUs).
    /// </summary>
    public long ReflectionDistance;
    /// <summary>
    /// Stores the reflection direction value associated with this object.
    /// </summary>
    public int ReflectionDirection;
    /// <summary>
    /// Stores the reflection start alpha value associated with this object.
    /// </summary>
    public int ReflectionStartAlpha;
    /// <summary>
    /// Stores the reflection end alpha value associated with this object.
    /// </summary>
    public int ReflectionEndAlpha;
    /// <summary>
    /// Stores the reflection start position in thousandths of one percent.
    /// </summary>
    public int ReflectionStartPosition;
    /// <summary>
    /// Stores the reflection end position in thousandths of one percent.
    /// </summary>
    public int ReflectionEndPosition;
    /// <summary>
    /// Stores the reflection fade direction in DrawingML angle units.
    /// </summary>
    public int ReflectionFadeDirection;
    /// <summary>
    /// Indicates whether the reflection rotates with its shape.
    /// </summary>
    public bool ReflectionRotateWithShape;
    /// <summary>
    /// Stores horizontal reflection scale in thousandths of one percent.
    /// </summary>
    public int ReflectionScaleX;
    /// <summary>
    /// Stores vertical reflection scale in thousandths of one percent.
    /// </summary>
    public int ReflectionScaleY;
    /// <summary>
    /// Stores horizontal reflection skew in DrawingML angle units.
    /// </summary>
    public int ReflectionSkewX;
    /// <summary>
    /// Stores vertical reflection skew in DrawingML angle units.
    /// </summary>
    public int ReflectionSkewY;
    /// <summary>
    /// Stores the reflection alignment.
    /// </summary>
    public string ReflectionAlignment;
    /// <summary>
    /// Stores the soft-edge radius in English Metric Units (EMUs).
    /// </summary>
    public long SoftEdgeRadius;
    /// <summary>
    /// Stores scene, bevel, extrusion, and material settings.
    /// </summary>
    public PptxThreeD ThreeD;
    /// <summary>
    /// Contains unsupported effect XML preserved during effect-list rewrites.
    /// </summary>
    public List<string> UnsupportedEffectXml;

    /// <summary>
    /// Initializes a new effect with safe defaults or the supplied values.
    /// </summary>
    public PptxEffect()
    {
        Xml = "";
        HasShadow = false;
        HasGlow = false;
        HasReflection = false;
        HasSoftEdge = false;
        HasCustomEffect = false;
        ShadowColor = new PptxColor();
        ShadowType = OuterShadow;
        ShadowBlurRadius = 0L;
        ShadowDistance = 0L;
        ShadowDirection = 0;
        ShadowAlignment = "";
        ShadowTransparency = 0;
        ShadowRotateWithShape = true;
        ShadowScaleX = 100000;
        ShadowScaleY = 100000;
        ShadowSkewX = 0;
        ShadowSkewY = 0;
        GlowColor = new PptxColor();
        GlowRadius = 0L;
        GlowTransparency = 0;
        ReflectionBlurRadius = 0L;
        ReflectionDistance = 0L;
        ReflectionDirection = 0;
        ReflectionStartAlpha = 0;
        ReflectionEndAlpha = 0;
        ReflectionStartPosition = 0;
        ReflectionEndPosition = 100000;
        ReflectionFadeDirection = 5400000;
        ReflectionRotateWithShape = true;
        ReflectionScaleX = 100000;
        ReflectionScaleY = 100000;
        ReflectionSkewX = 0;
        ReflectionSkewY = 0;
        ReflectionAlignment = "";
        SoftEdgeRadius = 0L;
        ThreeD = new PptxThreeD();
        UnsupportedEffectXml = new List<string>();
    }
    internal static PptxEffect CreateNone()
    {
        return new PptxEffect();
    }
    internal static PptxEffect CreateOuterShadow(PptxColor color, long blurRadius, long distance, int direction)
    {
        PptxEffect effect = new PptxEffect();
        effect.SetOuterShadow(color, blurRadius, distance, direction);
        return effect;
    }
    internal static PptxEffect CreateGlow(PptxColor color, long radius)
    {
        PptxEffect effect = new PptxEffect();
        effect.SetGlow(color, radius);
        return effect;
    }
    internal static PptxEffect CreateInnerShadow(PptxColor color, long blurRadius, long distance, int direction)
    {
        PptxEffect effect = new PptxEffect();
        effect.SetInnerShadow(color, blurRadius, distance, direction);
        return effect;
    }
    internal static PptxEffect CreateReflection(long blurRadius, long distance, int direction)
    {
        PptxEffect effect = new PptxEffect();
        effect.SetReflection(blurRadius, distance, direction, 50000, 0);
        return effect;
    }
    internal void SetOuterShadow(PptxColor color, long blurRadius, long distance, int direction)
    {
        ValidateRadius(blurRadius, "blurRadius");
        ValidateRadius(distance, "distance");
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        HasShadow = true;
        ShadowType = OuterShadow;
        ShadowColor = color;
        ShadowBlurRadius = blurRadius;
        ShadowDistance = distance;
        ShadowDirection = direction;
        Xml = "";
    }
    internal void SetInnerShadow(PptxColor color, long blurRadius, long distance, int direction)
    {
        SetOuterShadow(color, blurRadius, distance, direction);
        ShadowType = InnerShadow;
    }
    internal void SetShadowTransform(string alignment, bool rotateWithShape, int scaleX, int scaleY, int skewX, int skewY, int transparency)
    {
        ShadowAlignment = PptxDiagnostic.CleanText(alignment);
        ShadowRotateWithShape = rotateWithShape;
        ShadowScaleX = ValidatePositivePercentage(scaleX, "scaleX");
        ShadowScaleY = ValidatePositivePercentage(scaleY, "scaleY");
        ShadowSkewX = skewX;
        ShadowSkewY = skewY;
        ShadowTransparency = ValidatePercentage(transparency, "transparency");
        Xml = "";
    }
    internal void SetGlow(PptxColor color, long radius)
    {
        ValidateRadius(radius, "radius");
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        HasGlow = true;
        GlowColor = color;
        GlowRadius = radius;
        Xml = "";
    }
    internal void SetGlowTransparency(int transparency)
    {
        GlowTransparency = ValidatePercentage(transparency, "transparency");
        Xml = "";
    }
    internal void SetReflection(long blurRadius, long distance, int direction, int startAlpha, int endAlpha)
    {
        ValidateRadius(blurRadius, "blurRadius");
        ValidateRadius(distance, "distance");
        HasReflection = true;
        ReflectionBlurRadius = blurRadius;
        ReflectionDistance = distance;
        ReflectionDirection = direction;
        ReflectionStartAlpha = ValidatePercentage(startAlpha, "startAlpha");
        ReflectionEndAlpha = ValidatePercentage(endAlpha, "endAlpha");
        Xml = "";
    }
    internal void SetReflectionTransform(int startPosition, int endPosition, int fadeDirection, bool rotateWithShape, int scaleX, int scaleY, int skewX, int skewY, string alignment)
    {
        ReflectionStartPosition = ValidatePercentage(startPosition, "startPosition");
        ReflectionEndPosition = ValidatePercentage(endPosition, "endPosition");
        ReflectionFadeDirection = fadeDirection;
        ReflectionRotateWithShape = rotateWithShape;
        ReflectionScaleX = ValidatePositivePercentage(scaleX, "scaleX");
        ReflectionScaleY = ValidatePositivePercentage(scaleY, "scaleY");
        ReflectionSkewX = skewX;
        ReflectionSkewY = skewY;
        ReflectionAlignment = PptxDiagnostic.CleanText(alignment);
        Xml = "";
    }
    internal void SetSoftEdge(long radius)
    {
        ValidateRadius(radius, "radius");
        HasSoftEdge = true;
        SoftEdgeRadius = radius;
        Xml = "";
    }
    internal void SetTopBevel(long width, long height, string preset)
    {
        SetBevel(ThreeD.TopBevel, width, height, preset);
        ThreeD.HasShape = true;
        Xml = "";
    }
    internal void SetBottomBevel(long width, long height, string preset)
    {
        SetBevel(ThreeD.BottomBevel, width, height, preset);
        ThreeD.HasShape = true;
        Xml = "";
    }
    internal void SetShapeThreeD(long z, long extrusionHeight, long contourWidth, string material, PptxColor extrusionColor, PptxColor contourColor)
    {
        ValidateRadius(extrusionHeight, "extrusionHeight");
        ValidateRadius(contourWidth, "contourWidth");
        if (extrusionColor == null || contourColor == null)
        {
            throw new System.ArgumentNullException("extrusionColor");
        }

        ThreeD.HasShape = true;
        ThreeD.Z = z;
        ThreeD.ExtrusionHeight = extrusionHeight;
        ThreeD.ContourWidth = contourWidth;
        ThreeD.Material = PptxDiagnostic.CleanText(material);
        ThreeD.ExtrusionColor = extrusionColor;
        ThreeD.ContourColor = contourColor;
        Xml = "";
    }
    internal void SetSceneThreeD(string cameraPreset, int fieldOfView, int zoom, int cameraLatitude, int cameraLongitude, int cameraRevolution, string lightRig, string lightDirection, int lightLatitude, int lightLongitude, int lightRevolution)
    {
        ThreeD.HasScene = true;
        ThreeD.CameraPreset = PptxDiagnostic.CleanText(cameraPreset);
        ThreeD.CameraFieldOfView = fieldOfView;
        ThreeD.CameraZoom = ValidatePositivePercentage(zoom, "zoom");
        ThreeD.CameraLatitude = cameraLatitude;
        ThreeD.CameraLongitude = cameraLongitude;
        ThreeD.CameraRevolution = cameraRevolution;
        ThreeD.LightRig = PptxDiagnostic.CleanText(lightRig);
        ThreeD.LightDirection = PptxDiagnostic.CleanText(lightDirection);
        ThreeD.LightLatitude = lightLatitude;
        ThreeD.LightLongitude = lightLongitude;
        ThreeD.LightRevolution = lightRevolution;
        Xml = "";
    }

    internal static PptxEffect FromShapeProperties(XmlElement? shapeProperties)
    {
        if (shapeProperties == null)
        {
            return CreateNone();
        }

        PptxEffect effect = new PptxEffect();
        XmlElement? effectList = PptxXml.FindFirstChild(shapeProperties, PptxNamespaces.Drawing, "effectLst");
        if (effectList != null)
        {
            effect = FromElement(effectList);
        }
        else
        {
            XmlElement? effectDag = PptxXml.FindFirstChild(shapeProperties, PptxNamespaces.Drawing, "effectDag");
            if (effectDag != null)
            {
                effect.HasCustomEffect = true;
                effect.Xml = effectDag.OuterXml;
            }
        }

        LoadThreeD(effect, shapeProperties);
        return effect;
    }

    internal static PptxEffect FromElement(XmlElement element)
    {
        PptxEffect effect = new PptxEffect();
        effect.Xml = element.OuterXml;
        if (element.LocalName == "effectDag")
        {
            effect.HasCustomEffect = true;
            return effect;
        }

        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlNode? node = element.ChildNodes[index];
            if (node == null || node.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement child = (XmlElement)node;
            if (child.NamespaceURI != PptxNamespaces.Drawing)
            {
                effect.UnsupportedEffectXml.Add(child.OuterXml);
            }
            else if (child.LocalName == "outerShdw" || child.LocalName == "innerShdw")
            {
                LoadShadow(effect, child);
            }
            else if (child.LocalName == "glow")
            {
                LoadGlow(effect, child);
            }
            else if (child.LocalName == "reflection")
            {
                LoadReflection(effect, child);
            }
            else if (child.LocalName == "softEdge")
            {
                effect.HasSoftEdge = true;
                effect.SoftEdgeRadius = PptxTransform.GetLongAttribute(child, "rad", 0L);
            }
            else
            {
                effect.UnsupportedEffectXml.Add(child.OuterXml);
            }
        }

        return effect;
    }

    internal XmlElement ToElement(XmlDocument document)
    {
        if (HasCustomEffect && Xml.Length > 0)
        {
            XmlDocument effectDocument = PptxXml.LoadString(Xml);
            XmlElement? root = effectDocument.DocumentElement;
            if (root != null)
            {
                XmlNode imported = document.ImportNode(root, true);
                return (XmlElement)imported;
            }
        }

        XmlElement effectList = document.CreateElement("a", "effectLst", PptxNamespaces.Drawing);
        if (HasShadow)
        {
            effectList.AppendChild(CreateShadowElement(document));
        }

        if (HasGlow)
        {
            effectList.AppendChild(CreateGlowElement(document));
        }

        if (HasReflection)
        {
            effectList.AppendChild(CreateReflectionElement(document));
        }

        if (HasSoftEdge)
        {
            XmlElement softEdge = document.CreateElement("a", "softEdge", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(softEdge, "", "", "rad", SoftEdgeRadius.ToString());
            effectList.AppendChild(softEdge);
        }

        foreach (string xml in UnsupportedEffectXml)
        {
            XmlDocument effectDocument = PptxXml.LoadString(xml);
            XmlElement? unsupported = effectDocument.DocumentElement;
            if (unsupported != null)
            {
                effectList.AppendChild(document.ImportNode(unsupported, true));
            }
        }

        return effectList;
    }

    internal XmlElement CreateShadowElement(XmlDocument document)
    {
        string localName = "outerShdw";
        if (ShadowType == InnerShadow)
        {
            localName = "innerShdw";
        }

        XmlElement shadow = document.CreateElement("a", localName, PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(shadow, "", "", "blurRad", ShadowBlurRadius.ToString());
        PptxXml.SetAttributeValue(shadow, "", "", "dist", ShadowDistance.ToString());
        PptxXml.SetAttributeValue(shadow, "", "", "dir", ShadowDirection.ToString());
        if (ShadowAlignment.Length > 0)
        {
            PptxXml.SetAttributeValue(shadow, "", "", "algn", ShadowAlignment);
        }

        if (ShadowScaleX != 100000)
        {
            PptxXml.SetAttributeValue(shadow, "", "", "sx", ShadowScaleX.ToString());
        }

        if (ShadowScaleY != 100000)
        {
            PptxXml.SetAttributeValue(shadow, "", "", "sy", ShadowScaleY.ToString());
        }

        if (ShadowSkewX != 0)
        {
            PptxXml.SetAttributeValue(shadow, "", "", "kx", ShadowSkewX.ToString());
        }

        if (ShadowSkewY != 0)
        {
            PptxXml.SetAttributeValue(shadow, "", "", "ky", ShadowSkewY.ToString());
        }

        PptxXml.SetAttributeValue(shadow, "", "", "rotWithShape", PptxTransform.BoolToXml(ShadowRotateWithShape));

        PptxColor color = CloneColorWithTransparency(ShadowColor, ShadowTransparency);
        shadow.AppendChild(color.ToElement(document));
        return shadow;
    }

    internal XmlElement CreateGlowElement(XmlDocument document)
    {
        XmlElement glow = document.CreateElement("a", "glow", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(glow, "", "", "rad", GlowRadius.ToString());
        PptxColor color = CloneColorWithTransparency(GlowColor, GlowTransparency);
        glow.AppendChild(color.ToElement(document));
        return glow;
    }

    internal XmlElement CreateReflectionElement(XmlDocument document)
    {
        XmlElement reflection = document.CreateElement("a", "reflection", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(reflection, "", "", "blurRad", ReflectionBlurRadius.ToString());
        PptxXml.SetAttributeValue(reflection, "", "", "dist", ReflectionDistance.ToString());
        PptxXml.SetAttributeValue(reflection, "", "", "dir", ReflectionDirection.ToString());
        PptxXml.SetAttributeValue(reflection, "", "", "stA", ReflectionStartAlpha.ToString());
        PptxXml.SetAttributeValue(reflection, "", "", "endA", ReflectionEndAlpha.ToString());
        PptxXml.SetAttributeValue(reflection, "", "", "stPos", ReflectionStartPosition.ToString());
        PptxXml.SetAttributeValue(reflection, "", "", "endPos", ReflectionEndPosition.ToString());
        PptxXml.SetAttributeValue(reflection, "", "", "fadeDir", ReflectionFadeDirection.ToString());
        PptxXml.SetAttributeValue(reflection, "", "", "rotWithShape", PptxTransform.BoolToXml(ReflectionRotateWithShape));
        if (ReflectionScaleX != 100000)
        {
            PptxXml.SetAttributeValue(reflection, "", "", "sx", ReflectionScaleX.ToString());
        }

        if (ReflectionScaleY != 100000)
        {
            PptxXml.SetAttributeValue(reflection, "", "", "sy", ReflectionScaleY.ToString());
        }

        if (ReflectionSkewX != 0)
        {
            PptxXml.SetAttributeValue(reflection, "", "", "kx", ReflectionSkewX.ToString());
        }

        if (ReflectionSkewY != 0)
        {
            PptxXml.SetAttributeValue(reflection, "", "", "ky", ReflectionSkewY.ToString());
        }

        if (ReflectionAlignment.Length > 0)
        {
            PptxXml.SetAttributeValue(reflection, "", "", "algn", ReflectionAlignment);
        }

        return reflection;
    }

    internal static PptxColor CloneColorWithTransparency(PptxColor source, int transparency)
    {
        PptxColor color = PptxStyleResolverCore.CloneColor(source);
        color.Alpha = 100000 - ValidatePercentage(transparency, "transparency");
        return color;
    }

    internal static void LoadShadow(PptxEffect effect, XmlElement shadow)
    {
        effect.HasShadow = true;
        effect.ShadowType = OuterShadow;
        if (shadow.LocalName == "innerShdw")
        {
            effect.ShadowType = InnerShadow;
        }

        effect.ShadowBlurRadius = PptxTransform.GetLongAttribute(shadow, "blurRad", 0L);
        effect.ShadowDistance = PptxTransform.GetLongAttribute(shadow, "dist", 0L);
        effect.ShadowDirection = PptxXml.GetIntAttribute(shadow, "", "dir", 0);
        effect.ShadowAlignment = PptxXml.GetAttributeValue(shadow, "", "algn");
        effect.ShadowColor = PptxColor.FromParent(shadow);
        effect.ShadowTransparency = 100000 - effect.ShadowColor.Alpha;
        effect.ShadowRotateWithShape = PptxXml.GetBoolAttribute(shadow, "", "rotWithShape", true);
        effect.ShadowScaleX = PptxXml.GetIntAttribute(shadow, "", "sx", 100000);
        effect.ShadowScaleY = PptxXml.GetIntAttribute(shadow, "", "sy", 100000);
        effect.ShadowSkewX = PptxXml.GetIntAttribute(shadow, "", "kx", 0);
        effect.ShadowSkewY = PptxXml.GetIntAttribute(shadow, "", "ky", 0);
    }

    internal static void LoadGlow(PptxEffect effect, XmlElement glow)
    {
        effect.HasGlow = true;
        effect.GlowRadius = PptxTransform.GetLongAttribute(glow, "rad", 0L);
        effect.GlowColor = PptxColor.FromParent(glow);
        effect.GlowTransparency = 100000 - effect.GlowColor.Alpha;
    }

    internal static void LoadReflection(PptxEffect effect, XmlElement reflection)
    {
        effect.HasReflection = true;
        effect.ReflectionBlurRadius = PptxTransform.GetLongAttribute(reflection, "blurRad", 0L);
        effect.ReflectionDistance = PptxTransform.GetLongAttribute(reflection, "dist", 0L);
        effect.ReflectionDirection = PptxXml.GetIntAttribute(reflection, "", "dir", 0);
        effect.ReflectionStartAlpha = PptxXml.GetIntAttribute(reflection, "", "stA", 0);
        effect.ReflectionEndAlpha = PptxXml.GetIntAttribute(reflection, "", "endA", 0);
        effect.ReflectionStartPosition = PptxXml.GetIntAttribute(reflection, "", "stPos", 0);
        effect.ReflectionEndPosition = PptxXml.GetIntAttribute(reflection, "", "endPos", 100000);
        effect.ReflectionFadeDirection = PptxXml.GetIntAttribute(reflection, "", "fadeDir", 5400000);
        effect.ReflectionRotateWithShape = PptxXml.GetBoolAttribute(reflection, "", "rotWithShape", true);
        effect.ReflectionScaleX = PptxXml.GetIntAttribute(reflection, "", "sx", 100000);
        effect.ReflectionScaleY = PptxXml.GetIntAttribute(reflection, "", "sy", 100000);
        effect.ReflectionSkewX = PptxXml.GetIntAttribute(reflection, "", "kx", 0);
        effect.ReflectionSkewY = PptxXml.GetIntAttribute(reflection, "", "ky", 0);
        effect.ReflectionAlignment = PptxXml.GetAttributeValue(reflection, "", "algn");
    }

    internal void AppendThreeD(XmlDocument document, XmlElement parent)
    {
        if (ThreeD.HasScene)
        {
            parent.AppendChild(CreateSceneThreeDElement(document));
        }

        if (ThreeD.HasShape)
        {
            parent.AppendChild(CreateShapeThreeDElement(document));
        }
    }

    internal XmlElement CreateSceneThreeDElement(XmlDocument document)
    {
        XmlElement scene = document.CreateElement("a", "scene3d", PptxNamespaces.Drawing);
        XmlElement camera = document.CreateElement("a", "camera", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(camera, "", "", "prst", ThreeD.CameraPreset);
        if (ThreeD.CameraFieldOfView != 0)
        {
            PptxXml.SetAttributeValue(camera, "", "", "fov", ThreeD.CameraFieldOfView.ToString());
        }

        if (ThreeD.CameraZoom != 100000)
        {
            PptxXml.SetAttributeValue(camera, "", "", "zoom", ThreeD.CameraZoom.ToString());
        }

        AppendRotation(document, camera, ThreeD.CameraLatitude, ThreeD.CameraLongitude, ThreeD.CameraRevolution);
        scene.AppendChild(camera);
        XmlElement light = document.CreateElement("a", "lightRig", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(light, "", "", "rig", ThreeD.LightRig);
        PptxXml.SetAttributeValue(light, "", "", "dir", ThreeD.LightDirection);
        AppendRotation(document, light, ThreeD.LightLatitude, ThreeD.LightLongitude, ThreeD.LightRevolution);
        scene.AppendChild(light);
        return scene;
    }

    internal XmlElement CreateShapeThreeDElement(XmlDocument document)
    {
        XmlElement shape = document.CreateElement("a", "sp3d", PptxNamespaces.Drawing);
        if (ThreeD.Z != 0L)
        {
            PptxXml.SetAttributeValue(shape, "", "", "z", ThreeD.Z.ToString());
        }

        if (ThreeD.ExtrusionHeight != 0L)
        {
            PptxXml.SetAttributeValue(shape, "", "", "extrusionH", ThreeD.ExtrusionHeight.ToString());
        }

        if (ThreeD.ContourWidth != 0L)
        {
            PptxXml.SetAttributeValue(shape, "", "", "contourW", ThreeD.ContourWidth.ToString());
        }

        if (ThreeD.Material.Length > 0)
        {
            PptxXml.SetAttributeValue(shape, "", "", "prstMaterial", ThreeD.Material);
        }

        AppendBevel(document, shape, "bevelT", ThreeD.TopBevel);
        AppendBevel(document, shape, "bevelB", ThreeD.BottomBevel);
        AppendThreeDColor(document, shape, "extrusionClr", ThreeD.ExtrusionColor);
        AppendThreeDColor(document, shape, "contourClr", ThreeD.ContourColor);
        return shape;
    }

    internal static void AppendRotation(XmlDocument document, XmlElement parent, int latitude, int longitude, int revolution)
    {
        if (latitude == 0 && longitude == 0 && revolution == 0)
        {
            return;
        }

        XmlElement rotation = document.CreateElement("a", "rot", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(rotation, "", "", "lat", latitude.ToString());
        PptxXml.SetAttributeValue(rotation, "", "", "lon", longitude.ToString());
        PptxXml.SetAttributeValue(rotation, "", "", "rev", revolution.ToString());
        parent.AppendChild(rotation);
    }

    internal static void AppendBevel(XmlDocument document, XmlElement parent, string localName, PptxBevel bevel)
    {
        if (!bevel.Enabled)
        {
            return;
        }

        XmlElement element = document.CreateElement("a", localName, PptxNamespaces.Drawing);
        if (bevel.Width > 0L)
        {
            PptxXml.SetAttributeValue(element, "", "", "w", bevel.Width.ToString());
        }

        if (bevel.Height > 0L)
        {
            PptxXml.SetAttributeValue(element, "", "", "h", bevel.Height.ToString());
        }

        if (bevel.Preset.Length > 0)
        {
            PptxXml.SetAttributeValue(element, "", "", "prst", bevel.Preset);
        }

        parent.AppendChild(element);
    }

    internal static void AppendThreeDColor(XmlDocument document, XmlElement parent, string localName, PptxColor color)
    {
        if (color.Type == PptxColor.Empty)
        {
            return;
        }

        XmlElement wrapper = document.CreateElement("a", localName, PptxNamespaces.Drawing);
        wrapper.AppendChild(color.ToElement(document));
        parent.AppendChild(wrapper);
    }

    internal static void LoadThreeD(PptxEffect effect, XmlElement parent)
    {
        XmlElement? scene = PptxXml.FindFirstChild(parent, PptxNamespaces.Drawing, "scene3d");
        if (scene != null)
        {
            LoadSceneThreeD(effect.ThreeD, scene);
        }

        XmlElement? shape = PptxXml.FindFirstChild(parent, PptxNamespaces.Drawing, "sp3d");
        if (shape != null)
        {
            LoadShapeThreeD(effect.ThreeD, shape);
        }
    }

    internal static void LoadSceneThreeD(PptxThreeD threeD, XmlElement scene)
    {
        threeD.HasScene = true;
        XmlElement? camera = PptxXml.FindFirstChild(scene, PptxNamespaces.Drawing, "camera");
        if (camera != null)
        {
            threeD.CameraPreset = PptxXml.GetAttributeValue(camera, "", "prst");
            threeD.CameraFieldOfView = PptxXml.GetIntAttribute(camera, "", "fov", 0);
            threeD.CameraZoom = PptxXml.GetIntAttribute(camera, "", "zoom", 100000);
            XmlElement? rotation = PptxXml.FindFirstChild(camera, PptxNamespaces.Drawing, "rot");
            if (rotation != null)
            {
                threeD.CameraLatitude = PptxXml.GetIntAttribute(rotation, "", "lat", 0);
                threeD.CameraLongitude = PptxXml.GetIntAttribute(rotation, "", "lon", 0);
                threeD.CameraRevolution = PptxXml.GetIntAttribute(rotation, "", "rev", 0);
            }
        }

        XmlElement? light = PptxXml.FindFirstChild(scene, PptxNamespaces.Drawing, "lightRig");
        if (light != null)
        {
            threeD.LightRig = PptxXml.GetAttributeValue(light, "", "rig");
            threeD.LightDirection = PptxXml.GetAttributeValue(light, "", "dir");
            XmlElement? rotation = PptxXml.FindFirstChild(light, PptxNamespaces.Drawing, "rot");
            if (rotation != null)
            {
                threeD.LightLatitude = PptxXml.GetIntAttribute(rotation, "", "lat", 0);
                threeD.LightLongitude = PptxXml.GetIntAttribute(rotation, "", "lon", 0);
                threeD.LightRevolution = PptxXml.GetIntAttribute(rotation, "", "rev", 0);
            }
        }
    }

    internal static void LoadShapeThreeD(PptxThreeD threeD, XmlElement shape)
    {
        threeD.HasShape = true;
        threeD.Z = PptxTransform.GetLongAttribute(shape, "z", 0L);
        threeD.ExtrusionHeight = PptxTransform.GetLongAttribute(shape, "extrusionH", 0L);
        threeD.ContourWidth = PptxTransform.GetLongAttribute(shape, "contourW", 0L);
        threeD.Material = PptxXml.GetAttributeValue(shape, "", "prstMaterial");
        XmlElement? top = PptxXml.FindFirstChild(shape, PptxNamespaces.Drawing, "bevelT");
        if (top != null)
        {
            LoadBevel(threeD.TopBevel, top);
        }

        XmlElement? bottom = PptxXml.FindFirstChild(shape, PptxNamespaces.Drawing, "bevelB");
        if (bottom != null)
        {
            LoadBevel(threeD.BottomBevel, bottom);
        }

        XmlElement? extrusionColor = PptxXml.FindFirstChild(shape, PptxNamespaces.Drawing, "extrusionClr");
        if (extrusionColor != null)
        {
            threeD.ExtrusionColor = PptxColor.FromParent(extrusionColor);
        }

        XmlElement? contourColor = PptxXml.FindFirstChild(shape, PptxNamespaces.Drawing, "contourClr");
        if (contourColor != null)
        {
            threeD.ContourColor = PptxColor.FromParent(contourColor);
        }
    }

    internal static void LoadBevel(PptxBevel bevel, XmlElement element)
    {
        bevel.Enabled = true;
        bevel.Width = PptxTransform.GetLongAttribute(element, "w", 0L);
        bevel.Height = PptxTransform.GetLongAttribute(element, "h", 0L);
        bevel.Preset = PptxXml.GetAttributeValue(element, "", "prst");
    }

    internal static void SetBevel(PptxBevel bevel, long width, long height, string preset)
    {
        ValidateRadius(width, "width");
        ValidateRadius(height, "height");
        bevel.Enabled = true;
        bevel.Width = width;
        bevel.Height = height;
        bevel.Preset = PptxDiagnostic.CleanText(preset);
    }

    internal static int ValidatePercentage(int value, string parameterName)
    {
        if (value < 0 || value > 100000)
        {
            throw new System.ArgumentOutOfRangeException(parameterName, "The value must be between zero and 100,000.");
        }

        return value;
    }

    internal static int ValidatePositivePercentage(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new System.ArgumentOutOfRangeException(parameterName, "The value must be greater than zero.");
        }

        return value;
    }

    internal static void ValidateRadius(long value, string parameterName)
    {
        if (value < 0L)
        {
            throw new System.ArgumentOutOfRangeException(parameterName, "The value cannot be negative.");
        }
    }
}
