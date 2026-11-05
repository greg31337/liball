using System.Xml;

namespace pptx;

/// <summary>
/// Stores shape position, size, rotation, flips, and optional child coordinates for grouped shapes.
/// </summary>
public class PptxTransform
{
    /// <summary>
    /// Stores the x in English Metric Units (EMUs).
    /// </summary>
    public long X;
    /// <summary>
    /// Stores the y in English Metric Units (EMUs).
    /// </summary>
    public long Y;
    /// <summary>
    /// Stores the width in English Metric Units (EMUs).
    /// </summary>
    public long Width;
    /// <summary>
    /// Stores the height in English Metric Units (EMUs).
    /// </summary>
    public long Height;
    /// <summary>
    /// Stores the child x value associated with this object.
    /// </summary>
    public long ChildX;
    /// <summary>
    /// Stores the child y value associated with this object.
    /// </summary>
    public long ChildY;
    /// <summary>
    /// Stores the child width in English Metric Units (EMUs).
    /// </summary>
    public long ChildWidth;
    /// <summary>
    /// Stores the child height in English Metric Units (EMUs).
    /// </summary>
    public long ChildHeight;
    /// <summary>
    /// Stores the rotation angle units in PresentationML angle units, where 60,000 units equal one degree.
    /// </summary>
    public int RotationAngleUnits;
    /// <summary>
    /// Indicates whether the flip horizontal setting is enabled.
    /// </summary>
    public bool FlipHorizontal;
    /// <summary>
    /// Indicates whether the flip vertical setting is enabled.
    /// </summary>
    public bool FlipVertical;

    /// <summary>
    /// Initializes a new transform using the supplied bounds and default group and rotation values.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    public PptxTransform(PptxBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        X = bounds.X;
        Y = bounds.Y;
        Width = bounds.Width;
        Height = bounds.Height;
        ChildX = 0L;
        ChildY = 0L;
        ChildWidth = 0L;
        ChildHeight = 0L;
        RotationAngleUnits = 0;
        FlipHorizontal = false;
        FlipVertical = false;
    }
    internal static PptxTransform CreateDefault()
    {
        return new PptxTransform(new PptxBounds(0L, 0L, 0L, 0L));
    }
    internal PptxBounds Bounds()
    {
        return new PptxBounds(X, Y, Width, Height);
    }
    internal PptxBounds AnchorBounds()
    {
        PptxPoint topLeft = RotatePoint(new PptxPoint(X, Y), X, Y, Width, Height);
        PptxPoint topRight = RotatePoint(new PptxPoint(X + Width, Y), X, Y, Width, Height);
        PptxPoint bottomLeft = RotatePoint(new PptxPoint(X, Y + Height), X, Y, Width, Height);
        PptxPoint bottomRight = RotatePoint(new PptxPoint(X + Width, Y + Height), X, Y, Width, Height);
        return BoundsFromPoints(topLeft, topRight, bottomLeft, bottomRight);
    }
    internal void SetBounds(PptxBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        X = bounds.X;
        Y = bounds.Y;
        Width = bounds.Width;
        Height = bounds.Height;
    }
    internal double RotationDegrees()
    {
        return PptxUnitsCore.AngleUnitsToDegrees(RotationAngleUnits);
    }
    internal void SetRotationDegrees(double degrees)
    {
        RotationAngleUnits = PptxUnitsCore.DegreesToAngleUnits(degrees);
    }
    internal bool HasChildBounds()
    {
        if (ChildWidth == 0L && ChildHeight == 0L)
        {
            return false;
        }

        return true;
    }
    internal PptxBounds ChildBounds()
    {
        return new PptxBounds(ChildX, ChildY, ChildWidth, ChildHeight);
    }
    internal void SetChildBounds(PptxBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        ChildX = bounds.X;
        ChildY = bounds.Y;
        ChildWidth = bounds.Width;
        ChildHeight = bounds.Height;
    }
    internal PptxBounds MapChildBounds(PptxBounds childBounds)
    {
        if (childBounds == null)
        {
            throw new System.ArgumentNullException("childBounds");
        }

        if (ChildWidth == 0L || ChildHeight == 0L)
        {
            return childBounds.Offset(X, Y);
        }

        long width = ScaleCoordinate(childBounds.Width, Width, ChildWidth);
        long height = ScaleCoordinate(childBounds.Height, Height, ChildHeight);
        long x = X + ScaleCoordinate(childBounds.X - ChildX, Width, ChildWidth);
        long y = Y + ScaleCoordinate(childBounds.Y - ChildY, Height, ChildHeight);
        if (FlipHorizontal)
        {
            x = X + Width - ScaleCoordinate(childBounds.X - ChildX + childBounds.Width, Width, ChildWidth);
        }

        if (FlipVertical)
        {
            y = Y + Height - ScaleCoordinate(childBounds.Y - ChildY + childBounds.Height, Height, ChildHeight);
        }

        return new PptxBounds(x, y, width, height);
    }
    internal PptxPoint MapChildPoint(PptxPoint childPoint)
    {
        if (childPoint == null)
        {
            throw new System.ArgumentNullException("childPoint");
        }

        long x = X + childPoint.X;
        long y = Y + childPoint.Y;
        if (ChildWidth != 0L && ChildHeight != 0L)
        {
            x = X + ScaleCoordinate(childPoint.X - ChildX, Width, ChildWidth);
            y = Y + ScaleCoordinate(childPoint.Y - ChildY, Height, ChildHeight);
        }

        if (FlipHorizontal)
        {
            x = X + Width - (x - X);
        }

        if (FlipVertical)
        {
            y = Y + Height - (y - Y);
        }

        return RotatePoint(new PptxPoint(x, y), X, Y, Width, Height);
    }
    internal PptxBounds MapChildAnchorBounds(PptxBounds childBounds)
    {
        if (childBounds == null)
        {
            throw new System.ArgumentNullException("childBounds");
        }

        PptxPoint topLeft = MapChildPoint(new PptxPoint(childBounds.X, childBounds.Y));
        PptxPoint topRight = MapChildPoint(new PptxPoint(childBounds.X + childBounds.Width, childBounds.Y));
        PptxPoint bottomLeft = MapChildPoint(new PptxPoint(childBounds.X, childBounds.Y + childBounds.Height));
        PptxPoint bottomRight = MapChildPoint(new PptxPoint(childBounds.X + childBounds.Width, childBounds.Y + childBounds.Height));
        return BoundsFromPoints(topLeft, topRight, bottomLeft, bottomRight);
    }
    internal PptxPoint RotatePoint(PptxPoint point, long x, long y, long width, long height)
    {
        if (RotationAngleUnits == 0)
        {
            return new PptxPoint(point.X, point.Y);
        }

        double radians = RotationDegrees() * System.Math.PI / 180.0;
        double centerX = (double)x + ((double)width / 2.0);
        double centerY = (double)y + ((double)height / 2.0);
        double offsetX = (double)point.X - centerX;
        double offsetY = (double)point.Y - centerY;
        long rotatedX = PptxUnitsCore.RoundToLong(centerX + (offsetX * System.Math.Cos(radians)) - (offsetY * System.Math.Sin(radians)));
        long rotatedY = PptxUnitsCore.RoundToLong(centerY + (offsetX * System.Math.Sin(radians)) + (offsetY * System.Math.Cos(radians)));
        return new PptxPoint(rotatedX, rotatedY);
    }
    internal static PptxBounds BoundsFromPoints(PptxPoint first, PptxPoint second, PptxPoint third, PptxPoint fourth)
    {
        long left = PptxBounds.Minimum(PptxBounds.Minimum(first.X, second.X), PptxBounds.Minimum(third.X, fourth.X));
        long top = PptxBounds.Minimum(PptxBounds.Minimum(first.Y, second.Y), PptxBounds.Minimum(third.Y, fourth.Y));
        long right = PptxBounds.Maximum(PptxBounds.Maximum(first.X, second.X), PptxBounds.Maximum(third.X, fourth.X));
        long bottom = PptxBounds.Maximum(PptxBounds.Maximum(first.Y, second.Y), PptxBounds.Maximum(third.Y, fourth.Y));
        return new PptxBounds(left, top, right - left, bottom - top);
    }
    internal static long ScaleCoordinate(long value, long numerator, long denominator)
    {
        if (denominator == 0L)
        {
            return value;
        }

        double scaled = ((double)value * (double)numerator) / (double)denominator;
        return PptxUnitsCore.RoundToLong(scaled);
    }

    internal static PptxTransform FromShape(XmlElement shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        XmlElement? transformElement = FindTransformElement(shape);
        if (transformElement == null)
        {
            return CreateDefault();
        }

        return FromElement(transformElement);
    }

    internal static PptxTransform FromElement(XmlElement transformElement)
    {
        if (transformElement == null)
        {
            throw new System.ArgumentNullException("transformElement");
        }

        PptxTransform transform = CreateDefault();
        transform.RotationAngleUnits = PptxXml.GetIntAttribute(transformElement, "", "rot", 0);
        transform.FlipHorizontal = PptxXml.GetBoolAttribute(transformElement, "", "flipH", false);
        transform.FlipVertical = PptxXml.GetBoolAttribute(transformElement, "", "flipV", false);

        XmlElement? offset = PptxXml.FindFirstChild(transformElement, PptxNamespaces.Drawing, "off");
        if (offset != null)
        {
            transform.X = GetLongAttribute(offset, "x", 0L);
            transform.Y = GetLongAttribute(offset, "y", 0L);
        }

        XmlElement? extent = PptxXml.FindFirstChild(transformElement, PptxNamespaces.Drawing, "ext");
        if (extent != null)
        {
            transform.Width = GetLongAttribute(extent, "cx", 0L);
            transform.Height = GetLongAttribute(extent, "cy", 0L);
        }

        XmlElement? childOffset = PptxXml.FindFirstChild(transformElement, PptxNamespaces.Drawing, "chOff");
        if (childOffset != null)
        {
            transform.ChildX = GetLongAttribute(childOffset, "x", 0L);
            transform.ChildY = GetLongAttribute(childOffset, "y", 0L);
        }

        XmlElement? childExtent = PptxXml.FindFirstChild(transformElement, PptxNamespaces.Drawing, "chExt");
        if (childExtent != null)
        {
            transform.ChildWidth = GetLongAttribute(childExtent, "cx", 0L);
            transform.ChildHeight = GetLongAttribute(childExtent, "cy", 0L);
        }

        return transform;
    }

    internal void ApplyToShape(XmlElement shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        XmlElement transformElement = GetOrCreateTransformElement(shape);
        WriteToElement(transformElement);
    }

    internal void WriteToElement(XmlElement transformElement)
    {
        if (transformElement == null)
        {
            throw new System.ArgumentNullException("transformElement");
        }

        PptxXml.SetAttributeValue(transformElement, "", "", "rot", RotationAngleUnits.ToString());
        PptxXml.SetAttributeValue(transformElement, "", "", "flipH", BoolToXml(FlipHorizontal));
        PptxXml.SetAttributeValue(transformElement, "", "", "flipV", BoolToXml(FlipVertical));

        XmlElement offset = PptxXml.GetOrCreateChild(transformElement, "a", "off", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(offset, "", "", "x", X.ToString());
        PptxXml.SetAttributeValue(offset, "", "", "y", Y.ToString());

        XmlElement extent = PptxXml.GetOrCreateChild(transformElement, "a", "ext", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(extent, "", "", "cx", Width.ToString());
        PptxXml.SetAttributeValue(extent, "", "", "cy", Height.ToString());

        if (HasChildBounds())
        {
            XmlElement childOffset = PptxXml.GetOrCreateChild(transformElement, "a", "chOff", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(childOffset, "", "", "x", ChildX.ToString());
            PptxXml.SetAttributeValue(childOffset, "", "", "y", ChildY.ToString());

            XmlElement childExtent = PptxXml.GetOrCreateChild(transformElement, "a", "chExt", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(childExtent, "", "", "cx", ChildWidth.ToString());
            PptxXml.SetAttributeValue(childExtent, "", "", "cy", ChildHeight.ToString());
        }
    }

    internal static XmlElement GetOrCreateTransformElement(XmlElement shape)
    {
        XmlElement? properties = FindShapeProperties(shape);
        if (properties == null)
        {
            properties = shape.OwnerDocument.CreateElement("p", ShapePropertiesLocalName(shape), PptxNamespaces.Presentation);
            shape.AppendChild(properties);
        }

        XmlElement? transformElement = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "xfrm");
        if (transformElement == null)
        {
            transformElement = shape.OwnerDocument.CreateElement("a", "xfrm", PptxNamespaces.Drawing);
            properties.AppendChild(transformElement);
        }

        return transformElement;
    }

    internal static XmlElement? FindTransformElement(XmlElement shape)
    {
        XmlElement? properties = FindShapeProperties(shape);
        if (properties == null)
        {
            return null;
        }

        return PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "xfrm");
    }

    internal static XmlElement? FindShapeProperties(XmlElement shape)
    {
        if (shape.NamespaceURI != PptxNamespaces.Presentation)
        {
            return null;
        }

        if (shape.LocalName == "grpSp")
        {
            return PptxXml.FindFirstChild(shape, PptxNamespaces.Presentation, "grpSpPr");
        }

        return PptxXml.FindFirstChild(shape, PptxNamespaces.Presentation, ShapePropertiesLocalName(shape));
    }

    internal static string ShapePropertiesLocalName(XmlElement shape)
    {
        if (shape.LocalName == "grpSp")
        {
            return "grpSpPr";
        }

        return "spPr";
    }

    internal static long GetLongAttribute(XmlElement element, string localName, long fallback)
    {
        string value = PptxXml.GetAttributeValue(element, "", localName);
        long parsed = 0L;
        if (long.TryParse(value, out parsed))
        {
            return parsed;
        }

        return fallback;
    }

    internal static string BoolToXml(bool value)
    {
        if (value)
        {
            return "1";
        }

        return "0";
    }
}
