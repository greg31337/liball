using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents fill data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxFill
{
    /// <summary>
    /// Identifies the none value accepted by the API.
    /// </summary>
    public const string None = "none";
    /// <summary>
    /// Identifies the solid value accepted by the API.
    /// </summary>
    public const string Solid = "solid";
    /// <summary>
    /// Identifies the gradient value accepted by the API.
    /// </summary>
    public const string Gradient = "gradient";
    /// <summary>
    /// Identifies the picture value accepted by the API.
    /// </summary>
    public const string Picture = "picture";
    /// <summary>
    /// Identifies the pattern value accepted by the API.
    /// </summary>
    public const string Pattern = "pattern";
    /// <summary>
    /// Identifies the group value accepted by the API.
    /// </summary>
    public const string Group = "group";

    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the color value associated with this object.
    /// </summary>
    public PptxColor Color;
    /// <summary>
    /// Stores the secondary color value associated with this object.
    /// </summary>
    public PptxColor SecondaryColor;
    /// <summary>
    /// Contains all gradient stops in position order.
    /// </summary>
    public List<PptxGradientStop> GradientStops;
    /// <summary>
    /// Stores the pattern type associated with this object.
    /// </summary>
    public string PatternType;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the gradient angle units in PresentationML angle units, where 60,000 units equal one degree.
    /// </summary>
    public int GradientAngleUnits;
    /// <summary>
    /// Indicates whether a linear gradient is scaled with its bounds.
    /// </summary>
    public bool GradientScaled;
    /// <summary>
    /// Indicates whether the fill rotates with its owning shape.
    /// </summary>
    public bool RotateWithShape;
    /// <summary>
    /// Stores fill transparency from zero through 100,000.
    /// </summary>
    public int Transparency;
    /// <summary>
    /// Stores the source crop rectangle for a picture fill.
    /// </summary>
    public PptxImageCrop Crop;
    /// <summary>
    /// Indicates that the picture relationship is linked instead of embedded.
    /// </summary>
    public bool PictureIsLinked;
    /// <summary>
    /// Indicates that picture stretch settings are active.
    /// </summary>
    public bool UseStretch;
    /// <summary>
    /// Indicates that picture tile settings are active.
    /// </summary>
    public bool UseTile;
    /// <summary>
    /// Stores picture stretch settings.
    /// </summary>
    public PptxFillStretch Stretch;
    /// <summary>
    /// Stores picture tile settings.
    /// </summary>
    public PptxFillTile Tile;
    internal string Xml;
    /// <summary>
    /// Indicates whether the present setting is enabled.
    /// </summary>
    public bool Present;

    /// <summary>
    /// Initializes a new fill with safe defaults or the supplied values.
    /// </summary>
    public PptxFill()
    {
        Type = None;
        Color = new PptxColor();
        SecondaryColor = new PptxColor();
        GradientStops = new List<PptxGradientStop>();
        PatternType = "";
        RelationshipId = "";
        GradientAngleUnits = 0;
        GradientScaled = true;
        RotateWithShape = true;
        Transparency = 0;
        Crop = new PptxImageCrop();
        PictureIsLinked = false;
        UseStretch = true;
        UseTile = false;
        Stretch = new PptxFillStretch();
        Tile = new PptxFillTile();
        Xml = "";
        Present = false;
    }
    internal static PptxFill CreateNone()
    {
        PptxFill fill = new PptxFill();
        fill.Type = None;
        fill.Present = true;
        return fill;
    }
    internal static PptxFill CreateSolid(PptxColor color)
    {
        PptxFill fill = new PptxFill();
        fill.Type = Solid;
        fill.Color = color;
        fill.Present = true;
        return fill;
    }
    internal static PptxFill CreateSolidRgb(string rgb)
    {
        return CreateSolid(PptxColor.CreateRgb(rgb));
    }
    internal static PptxFill CreateGradient(PptxColor startColor, PptxColor endColor, double angleDegrees)
    {
        PptxFill fill = new PptxFill();
        fill.Type = Gradient;
        fill.Color = startColor;
        fill.SecondaryColor = endColor;
        fill.GradientAngleUnits = PptxUnitsCore.DegreesToAngleUnits(angleDegrees);
        fill.GradientStops.Add(PptxGradientStop.Create(0, startColor));
        fill.GradientStops.Add(PptxGradientStop.Create(100000, endColor));
        fill.Present = true;
        return fill;
    }
    internal static PptxFill CreateMultiStopGradient(List<PptxGradientStop> stops, double angleDegrees)
    {
        ValidateGradientStops(stops);
        PptxFill fill = new PptxFill();
        fill.Type = Gradient;
        fill.GradientAngleUnits = PptxUnitsCore.DegreesToAngleUnits(angleDegrees);
        foreach (PptxGradientStop stop in stops)
        {
            fill.GradientStops.Add(stop);
        }

        fill.Color = fill.GradientStops[0].Color;
        fill.SecondaryColor = fill.GradientStops[fill.GradientStops.Count - 1].Color;
        fill.Present = true;
        return fill;
    }
    internal static PptxFill CreatePicture(string relationshipId)
    {
        PptxFill fill = new PptxFill();
        fill.Type = Picture;
        fill.RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        fill.Present = true;
        return fill;
    }
    internal static PptxFill CreatePattern(string patternType, PptxColor foreground, PptxColor background)
    {
        PptxFill fill = new PptxFill();
        fill.Type = Pattern;
        fill.PatternType = PptxDiagnostic.CleanText(patternType);
        fill.Color = foreground;
        fill.SecondaryColor = background;
        fill.Present = true;
        return fill;
    }
    internal static PptxFill CreateGroup()
    {
        PptxFill fill = new PptxFill();
        fill.Type = Group;
        fill.Present = true;
        return fill;
    }
    internal void SetTransparency(int transparency)
    {
        int clean = ValidateTransparency(transparency);
        if (Type == None || Type == Group)
        {
            throw new System.ArgumentException("Transparency requires a solid, gradient, picture, or pattern fill.");
        }

        Transparency = clean;
        int alpha = 100000 - clean;
        if (Type == Solid)
        {
            Color.Alpha = alpha;
        }

        if (Type == Gradient)
        {
            foreach (PptxGradientStop stop in GradientStops)
            {
                stop.Color.Alpha = alpha;
            }

            Color.Alpha = alpha;
            SecondaryColor.Alpha = alpha;
        }

        if (Type == Pattern)
        {
            Color.Alpha = alpha;
            SecondaryColor.Alpha = alpha;
        }
    }
    internal void SetGradientRotation(double angleDegrees, bool rotateWithShape)
    {
        if (Type != Gradient)
        {
            throw new System.ArgumentException("Gradient rotation requires a gradient fill.");
        }

        GradientAngleUnits = PptxUnitsCore.DegreesToAngleUnits(angleDegrees);
        RotateWithShape = rotateWithShape;
    }
    internal void SetPictureCrop(PptxImageCrop crop)
    {
        if (Type != Picture)
        {
            throw new System.ArgumentException("Picture crop settings require a picture fill.");
        }

        if (crop == null)
        {
            throw new System.ArgumentNullException("crop");
        }

        if (!crop.IsValid())
        {
            throw new System.ArgumentException("Picture crop values must describe a valid rectangle.", "crop");
        }

        Crop.Left = crop.Left;
        Crop.Top = crop.Top;
        Crop.Right = crop.Right;
        Crop.Bottom = crop.Bottom;
    }
    internal void SetPictureStretch(PptxFillStretch stretch)
    {
        if (Type != Picture)
        {
            throw new System.ArgumentException("Picture stretch settings require a picture fill.");
        }

        if (stretch == null)
        {
            throw new System.ArgumentNullException("stretch");
        }

        Stretch.Left = stretch.Left;
        Stretch.Top = stretch.Top;
        Stretch.Right = stretch.Right;
        Stretch.Bottom = stretch.Bottom;
        UseStretch = true;
        UseTile = false;
    }
    internal void SetPictureTile(PptxFillTile tile)
    {
        if (Type != Picture)
        {
            throw new System.ArgumentException("Picture tile settings require a picture fill.");
        }

        if (tile == null)
        {
            throw new System.ArgumentNullException("tile");
        }

        if (tile.ScaleX <= 0 || tile.ScaleY <= 0)
        {
            throw new System.ArgumentOutOfRangeException("tile", "Picture tile scales must be greater than zero.");
        }

        Tile.OffsetX = tile.OffsetX;
        Tile.OffsetY = tile.OffsetY;
        Tile.ScaleX = tile.ScaleX;
        Tile.ScaleY = tile.ScaleY;
        Tile.Alignment = PptxDiagnostic.CleanText(tile.Alignment);
        Tile.Flip = PptxDiagnostic.CleanText(tile.Flip);
        UseTile = true;
        UseStretch = false;
    }
    internal void SetPictureRotation(bool rotateWithShape)
    {
        if (Type != Picture)
        {
            throw new System.ArgumentException("Picture rotation requires a picture fill.");
        }

        RotateWithShape = rotateWithShape;
    }

    internal static PptxFill FromShapeProperties(XmlElement? shapeProperties)
    {
        if (shapeProperties == null)
        {
            return new PptxFill();
        }

        XmlElement? fillElement = FindFillElement(shapeProperties);
        if (fillElement == null)
        {
            return new PptxFill();
        }

        return FromElement(fillElement);
    }

    internal static PptxFill FromElement(XmlElement element)
    {
        PptxFill fill = new PptxFill();
        fill.Present = true;
        fill.Xml = element.OuterXml;
        if (element.LocalName == "solidFill")
        {
            fill.Type = Solid;
            fill.Color = PptxColor.FromParent(element);
            fill.Transparency = 100000 - PptxColor.ClampPercentage(fill.Color.Alpha);
            return fill;
        }

        if (element.LocalName == "gradFill")
        {
            fill.Type = Gradient;
            LoadGradient(fill, element);
            return fill;
        }

        if (element.LocalName == "blipFill")
        {
            fill.Type = Picture;
            LoadPicture(fill, element);
            return fill;
        }

        if (element.LocalName == "pattFill")
        {
            fill.Type = Pattern;
            LoadPattern(fill, element);
            return fill;
        }

        if (element.LocalName == "grpFill")
        {
            fill.Type = Group;
            return fill;
        }

        fill.Type = None;
        return fill;
    }

    internal XmlElement ToElement(XmlDocument document)
    {
        if (Type == Solid)
        {
            return CreateSolidElement(document);
        }

        if (Type == Gradient)
        {
            return CreateGradientElement(document);
        }

        if (Type == Picture)
        {
            return CreatePictureElement(document);
        }

        if (Type == Pattern)
        {
            return CreatePatternElement(document);
        }

        if (Type == Group)
        {
            return document.CreateElement("a", "grpFill", PptxNamespaces.Drawing);
        }

        return document.CreateElement("a", "noFill", PptxNamespaces.Drawing);
    }

    internal XmlElement CreateSolidElement(XmlDocument document)
    {
        XmlElement element = document.CreateElement("a", "solidFill", PptxNamespaces.Drawing);
        element.AppendChild(ColorWithTransparency(Color).ToElement(document));
        return element;
    }

    internal XmlElement CreateGradientElement(XmlDocument document)
    {
        XmlElement element = document.CreateElement("a", "gradFill", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(element, "", "", "rotWithShape", PptxTransform.BoolToXml(RotateWithShape));
        XmlElement stopList = document.CreateElement("a", "gsLst", PptxNamespaces.Drawing);
        if (GradientStops.Count > 0)
        {
            ValidateGradientStops(GradientStops);
            foreach (PptxGradientStop stop in GradientStops)
            {
                stopList.AppendChild(CreateGradientStop(document, stop.Position, stop.Color));
            }
        }
        else
        {
            stopList.AppendChild(CreateGradientStop(document, 0, Color));
            stopList.AppendChild(CreateGradientStop(document, 100000, SecondaryColor));
        }

        element.AppendChild(stopList);
        XmlElement linear = document.CreateElement("a", "lin", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(linear, "", "", "ang", GradientAngleUnits.ToString());
        PptxXml.SetAttributeValue(linear, "", "", "scaled", PptxTransform.BoolToXml(GradientScaled));
        element.AppendChild(linear);
        return element;
    }

    internal XmlElement CreatePictureElement(XmlDocument document)
    {
        XmlElement element = document.CreateElement("a", "blipFill", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(element, "", "", "rotWithShape", PptxTransform.BoolToXml(RotateWithShape));
        XmlElement blip = document.CreateElement("a", "blip", PptxNamespaces.Drawing);
        if (PictureIsLinked)
        {
            PptxXml.SetAttributeValue(blip, "r", PptxNamespaces.Relationships, "link", RelationshipId);
        }
        else
        {
            PptxXml.SetAttributeValue(blip, "r", PptxNamespaces.Relationships, "embed", RelationshipId);
        }

        if (Transparency > 0)
        {
            XmlElement alpha = document.CreateElement("a", "alphaModFix", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(alpha, "", "", "amt", (100000 - ValidateTransparency(Transparency)).ToString());
            blip.AppendChild(alpha);
        }

        element.AppendChild(blip);
        AppendCrop(document, element);
        if (UseTile)
        {
            AppendTile(document, element);
        }
        else if (UseStretch)
        {
            AppendStretch(document, element);
        }

        return element;
    }

    internal XmlElement CreatePatternElement(XmlDocument document)
    {
        XmlElement element = document.CreateElement("a", "pattFill", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(element, "", "", "prst", PatternType);
        XmlElement foreground = document.CreateElement("a", "fgClr", PptxNamespaces.Drawing);
        foreground.AppendChild(ColorWithTransparency(Color).ToElement(document));
        element.AppendChild(foreground);
        XmlElement background = document.CreateElement("a", "bgClr", PptxNamespaces.Drawing);
        background.AppendChild(ColorWithTransparency(SecondaryColor).ToElement(document));
        element.AppendChild(background);
        return element;
    }

    internal XmlElement CreateGradientStop(XmlDocument document, int position, PptxColor color)
    {
        XmlElement stop = document.CreateElement("a", "gs", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(stop, "", "", "pos", position.ToString());
        stop.AppendChild(ColorWithTransparency(color).ToElement(document));
        return stop;
    }

    internal static XmlElement? FindFillElement(XmlElement shapeProperties)
    {
        for (int index = 0; index < shapeProperties.ChildNodes.Count; index++)
        {
            XmlNode? child = shapeProperties.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing && IsFillElement(element.LocalName))
            {
                return element;
            }
        }

        return null;
    }

    internal static bool IsFillElement(string localName)
    {
        if (localName == "noFill")
        {
            return true;
        }

        if (localName == "solidFill")
        {
            return true;
        }

        if (localName == "gradFill")
        {
            return true;
        }

        if (localName == "blipFill")
        {
            return true;
        }

        if (localName == "pattFill")
        {
            return true;
        }

        if (localName == "grpFill")
        {
            return true;
        }

        return false;
    }

    internal static void LoadGradient(PptxFill fill, XmlElement element)
    {
        fill.RotateWithShape = PptxXml.GetBoolAttribute(element, "", "rotWithShape", true);
        XmlElement? stopList = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "gsLst");
        if (stopList != null)
        {
            for (int index = 0; index < stopList.ChildNodes.Count; index++)
            {
                XmlNode? child = stopList.ChildNodes[index];
                if (child == null || child.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                XmlElement stop = (XmlElement)child;
                if (stop.NamespaceURI == PptxNamespaces.Drawing && stop.LocalName == "gs")
                {
                    int position = PptxXml.GetIntAttribute(stop, "", "pos", 0);
                    fill.GradientStops.Add(PptxGradientStop.Create(position, PptxColor.FromParent(stop)));
                }
            }

            if (fill.GradientStops.Count > 0)
            {
                fill.Color = fill.GradientStops[0].Color;
                fill.SecondaryColor = fill.GradientStops[fill.GradientStops.Count - 1].Color;
                fill.LoadGradientTransparency();
            }
        }

        XmlElement? linear = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "lin");
        if (linear != null)
        {
            fill.GradientAngleUnits = PptxXml.GetIntAttribute(linear, "", "ang", 0);
            fill.GradientScaled = PptxXml.GetBoolAttribute(linear, "", "scaled", true);
        }
    }

    internal static void LoadPicture(PptxFill fill, XmlElement element)
    {
        fill.RotateWithShape = PptxXml.GetBoolAttribute(element, "", "rotWithShape", true);
        fill.UseStretch = false;
        fill.UseTile = false;
        XmlElement? blip = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "blip");
        if (blip != null)
        {
            fill.RelationshipId = PptxXml.GetAttributeValue(blip, PptxNamespaces.Relationships, "embed");
            if (fill.RelationshipId.Length == 0)
            {
                fill.RelationshipId = PptxXml.GetAttributeValue(blip, PptxNamespaces.Relationships, "link");
                fill.PictureIsLinked = fill.RelationshipId.Length > 0;
            }

            XmlElement? alpha = PptxXml.FindFirstChild(blip, PptxNamespaces.Drawing, "alphaModFix");
            if (alpha != null)
            {
                fill.Transparency = 100000 - PptxColor.ClampPercentage(PptxXml.GetIntAttribute(alpha, "", "amt", 100000));
            }
        }

        XmlElement? crop = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "srcRect");
        if (crop != null)
        {
            fill.Crop.Left = PptxXml.GetIntAttribute(crop, "", "l", 0);
            fill.Crop.Top = PptxXml.GetIntAttribute(crop, "", "t", 0);
            fill.Crop.Right = PptxXml.GetIntAttribute(crop, "", "r", 0);
            fill.Crop.Bottom = PptxXml.GetIntAttribute(crop, "", "b", 0);
        }

        XmlElement? stretch = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "stretch");
        if (stretch != null)
        {
            fill.UseStretch = true;
            XmlElement? rectangle = PptxXml.FindFirstChild(stretch, PptxNamespaces.Drawing, "fillRect");
            if (rectangle != null)
            {
                fill.Stretch.Left = PptxXml.GetIntAttribute(rectangle, "", "l", 0);
                fill.Stretch.Top = PptxXml.GetIntAttribute(rectangle, "", "t", 0);
                fill.Stretch.Right = PptxXml.GetIntAttribute(rectangle, "", "r", 0);
                fill.Stretch.Bottom = PptxXml.GetIntAttribute(rectangle, "", "b", 0);
            }
        }

        XmlElement? tile = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "tile");
        if (tile != null)
        {
            fill.UseTile = true;
            fill.UseStretch = false;
            fill.Tile.OffsetX = PptxTransform.GetLongAttribute(tile, "tx", 0L);
            fill.Tile.OffsetY = PptxTransform.GetLongAttribute(tile, "ty", 0L);
            fill.Tile.ScaleX = PptxXml.GetIntAttribute(tile, "", "sx", 100000);
            fill.Tile.ScaleY = PptxXml.GetIntAttribute(tile, "", "sy", 100000);
            fill.Tile.Alignment = PptxXml.GetAttributeValue(tile, "", "algn");
            fill.Tile.Flip = PptxXml.GetAttributeValue(tile, "", "flip");
        }
    }

    internal static void LoadPattern(PptxFill fill, XmlElement element)
    {
        fill.PatternType = PptxXml.GetAttributeValue(element, "", "prst");
        XmlElement? foreground = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "fgClr");
        if (foreground != null)
        {
            fill.Color = PptxColor.FromParent(foreground);
        }

        XmlElement? background = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "bgClr");
        if (background != null)
        {
            fill.SecondaryColor = PptxColor.FromParent(background);
        }

        if (fill.Color.Alpha == fill.SecondaryColor.Alpha)
        {
            fill.Transparency = 100000 - PptxColor.ClampPercentage(fill.Color.Alpha);
        }
    }

    internal void LoadGradientTransparency()
    {
        int alpha = PptxColor.ClampPercentage(GradientStops[0].Color.Alpha);
        foreach (PptxGradientStop stop in GradientStops)
        {
            if (PptxColor.ClampPercentage(stop.Color.Alpha) != alpha)
            {
                return;
            }
        }

        Transparency = 100000 - alpha;
    }

    internal PptxColor ColorWithTransparency(PptxColor color)
    {
        PptxColor result = PptxStyleResolverCore.CloneColor(color);
        if (Transparency > 0)
        {
            result.Alpha = 100000 - ValidateTransparency(Transparency);
        }

        return result;
    }

    internal void AppendCrop(XmlDocument document, XmlElement element)
    {
        if (!Crop.IsValid())
        {
            throw new System.ArgumentException("Picture crop values must describe a valid rectangle.");
        }

        if (Crop.Left == 0 && Crop.Top == 0 && Crop.Right == 0 && Crop.Bottom == 0)
        {
            return;
        }

        XmlElement crop = document.CreateElement("a", "srcRect", PptxNamespaces.Drawing);
        AppendNonzeroAttribute(crop, "l", Crop.Left);
        AppendNonzeroAttribute(crop, "t", Crop.Top);
        AppendNonzeroAttribute(crop, "r", Crop.Right);
        AppendNonzeroAttribute(crop, "b", Crop.Bottom);
        element.AppendChild(crop);
    }

    internal void AppendStretch(XmlDocument document, XmlElement element)
    {
        XmlElement stretch = document.CreateElement("a", "stretch", PptxNamespaces.Drawing);
        XmlElement rectangle = document.CreateElement("a", "fillRect", PptxNamespaces.Drawing);
        AppendNonzeroAttribute(rectangle, "l", Stretch.Left);
        AppendNonzeroAttribute(rectangle, "t", Stretch.Top);
        AppendNonzeroAttribute(rectangle, "r", Stretch.Right);
        AppendNonzeroAttribute(rectangle, "b", Stretch.Bottom);
        stretch.AppendChild(rectangle);
        element.AppendChild(stretch);
    }

    internal void AppendTile(XmlDocument document, XmlElement element)
    {
        if (Tile.ScaleX <= 0 || Tile.ScaleY <= 0)
        {
            throw new System.ArgumentOutOfRangeException("Tile", "Picture tile scales must be greater than zero.");
        }

        XmlElement tile = document.CreateElement("a", "tile", PptxNamespaces.Drawing);
        if (Tile.OffsetX != 0L)
        {
            PptxXml.SetAttributeValue(tile, "", "", "tx", Tile.OffsetX.ToString());
        }

        if (Tile.OffsetY != 0L)
        {
            PptxXml.SetAttributeValue(tile, "", "", "ty", Tile.OffsetY.ToString());
        }

        if (Tile.ScaleX != 100000)
        {
            PptxXml.SetAttributeValue(tile, "", "", "sx", Tile.ScaleX.ToString());
        }

        if (Tile.ScaleY != 100000)
        {
            PptxXml.SetAttributeValue(tile, "", "", "sy", Tile.ScaleY.ToString());
        }

        if (Tile.Alignment.Length > 0)
        {
            PptxXml.SetAttributeValue(tile, "", "", "algn", PptxDiagnostic.CleanText(Tile.Alignment));
        }

        if (Tile.Flip.Length > 0)
        {
            PptxXml.SetAttributeValue(tile, "", "", "flip", PptxDiagnostic.CleanText(Tile.Flip));
        }

        element.AppendChild(tile);
    }

    internal static void AppendNonzeroAttribute(XmlElement element, string name, int value)
    {
        if (value != 0)
        {
            PptxXml.SetAttributeValue(element, "", "", name, value.ToString());
        }
    }

    internal static int ValidateTransparency(int transparency)
    {
        if (transparency < 0 || transparency > 100000)
        {
            throw new System.ArgumentOutOfRangeException("transparency", "Transparency must be between zero and 100,000.");
        }

        return transparency;
    }

    internal static void ValidateGradientStops(List<PptxGradientStop> stops)
    {
        if (stops == null)
        {
            throw new System.ArgumentNullException("stops");
        }

        if (stops.Count < 2)
        {
            throw new System.ArgumentException("A gradient requires at least two stops.", "stops");
        }

        int previous = -1;
        foreach (PptxGradientStop stop in stops)
        {
            if (stop == null || stop.Color == null)
            {
                throw new System.ArgumentException("Every gradient stop requires a color.", "stops");
            }

            if (stop.Position < 0 || stop.Position > 100000 || stop.Position < previous)
            {
                throw new System.ArgumentException("Gradient stop positions must be ordered between zero and 100,000.", "stops");
            }

            previous = stop.Position;
        }
    }
}
