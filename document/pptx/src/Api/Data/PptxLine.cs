using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents line data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxLine
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
    /// Identifies the join round value accepted by the API.
    /// </summary>
    public const string JoinRound = "round";
    /// <summary>
    /// Identifies the join bevel value accepted by the API.
    /// </summary>
    public const string JoinBevel = "bevel";
    /// <summary>
    /// Identifies the join miter value accepted by the API.
    /// </summary>
    public const string JoinMiter = "miter";

    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the color value associated with this object.
    /// </summary>
    public PptxColor Color;
    /// <summary>
    /// Stores the width in English Metric Units (EMUs).
    /// </summary>
    public long Width;
    /// <summary>
    /// Stores the dash associated with this object.
    /// </summary>
    public string Dash;
    /// <summary>
    /// Contains custom dash and space pairs. When non-empty, these replace the preset dash value.
    /// </summary>
    public List<PptxLineDash> CustomDashes;
    /// <summary>
    /// Stores the cap associated with this object.
    /// </summary>
    public string Cap;
    /// <summary>
    /// Stores the compound associated with this object.
    /// </summary>
    public string Compound;
    /// <summary>
    /// Stores the alignment associated with this object.
    /// </summary>
    public string Alignment;
    /// <summary>
    /// Stores the join associated with this object.
    /// </summary>
    public string Join;
    /// <summary>
    /// Stores the miter join limit in thousandths of one percent.
    /// </summary>
    public int MiterLimit;
    /// <summary>
    /// Stores the head arrow type associated with this object.
    /// </summary>
    public string HeadArrowType;
    /// <summary>
    /// Stores the head arrow width value.
    /// </summary>
    public string HeadArrowWidth;
    /// <summary>
    /// Stores the head arrow length value.
    /// </summary>
    public string HeadArrowLength;
    /// <summary>
    /// Stores the tail arrow type associated with this object.
    /// </summary>
    public string TailArrowType;
    /// <summary>
    /// Stores the tail arrow width value.
    /// </summary>
    public string TailArrowWidth;
    /// <summary>
    /// Stores the tail arrow length value.
    /// </summary>
    public string TailArrowLength;
    /// <summary>
    /// Stores the transparency value associated with this object.
    /// </summary>
    public int Transparency;
    internal string Xml;
    /// <summary>
    /// Indicates whether the present setting is enabled.
    /// </summary>
    public bool Present;

    /// <summary>
    /// Initializes a new line with safe defaults or the supplied values.
    /// </summary>
    public PptxLine()
    {
        Type = None;
        Color = new PptxColor();
        Width = 0L;
        Dash = "";
        CustomDashes = new List<PptxLineDash>();
        Cap = "";
        Compound = "";
        Alignment = "";
        Join = "";
        MiterLimit = 0;
        HeadArrowType = "";
        HeadArrowWidth = "";
        HeadArrowLength = "";
        TailArrowType = "";
        TailArrowWidth = "";
        TailArrowLength = "";
        Transparency = 0;
        Xml = "";
        Present = false;
    }
    internal static PptxLine CreateNone()
    {
        PptxLine line = new PptxLine();
        line.Type = None;
        line.Present = true;
        return line;
    }
    internal static PptxLine CreateSolid(PptxColor color, long width)
    {
        PptxLine line = new PptxLine();
        line.Type = Solid;
        line.Color = color;
        line.Width = width;
        line.Present = true;
        return line;
    }
    internal static PptxLine CreateSolidRgb(string rgb, long width)
    {
        return CreateSolid(PptxColor.CreateRgb(rgb), width);
    }
    internal void SetAppearance(string cap, string compound, string alignment)
    {
        Cap = PptxDiagnostic.CleanText(cap);
        Compound = PptxDiagnostic.CleanText(compound);
        Alignment = PptxDiagnostic.CleanText(alignment);
    }
    internal void SetPresetDash(string dash)
    {
        Dash = PptxDiagnostic.CleanText(dash);
        CustomDashes.Clear();
    }
    internal void SetCustomDash(List<PptxLineDash> dashes)
    {
        if (dashes == null)
        {
            throw new System.ArgumentNullException("dashes");
        }

        if (dashes.Count == 0)
        {
            throw new System.ArgumentException("At least one custom dash pair is required.", "dashes");
        }

        CustomDashes.Clear();
        foreach (PptxLineDash dash in dashes)
        {
            if (dash == null || dash.DashLength <= 0 || dash.SpaceLength <= 0)
            {
                throw new System.ArgumentException("Custom dash lengths must be greater than zero.", "dashes");
            }

            CustomDashes.Add(dash);
        }

        Dash = "";
    }
    internal void SetJoin(string join, int miterLimit)
    {
        string clean = PptxDiagnostic.CleanText(join);
        if (clean != JoinRound && clean != JoinBevel && clean != JoinMiter && clean.Length > 0)
        {
            throw new System.ArgumentException("Line join must be round, bevel, miter, or empty.", "join");
        }

        if (miterLimit < 0)
        {
            throw new System.ArgumentOutOfRangeException("miterLimit");
        }

        Join = clean;
        MiterLimit = miterLimit;
    }
    internal void SetArrows(string headType, string headWidth, string headLength, string tailType, string tailWidth, string tailLength)
    {
        HeadArrowType = PptxDiagnostic.CleanText(headType);
        HeadArrowWidth = PptxDiagnostic.CleanText(headWidth);
        HeadArrowLength = PptxDiagnostic.CleanText(headLength);
        TailArrowType = PptxDiagnostic.CleanText(tailType);
        TailArrowWidth = PptxDiagnostic.CleanText(tailWidth);
        TailArrowLength = PptxDiagnostic.CleanText(tailLength);
    }
    internal void SetTransparency(int transparency)
    {
        Transparency = ValidateTransparency(transparency);
        Color.Alpha = 100000 - Transparency;
    }

    internal static PptxLine FromShapeProperties(XmlElement? shapeProperties)
    {
        if (shapeProperties == null)
        {
            return new PptxLine();
        }

        XmlElement? lineElement = PptxXml.FindFirstChild(shapeProperties, PptxNamespaces.Drawing, "ln");
        if (lineElement == null)
        {
            return new PptxLine();
        }

        return FromElement(lineElement);
    }

    internal static PptxLine FromElement(XmlElement element)
    {
        PptxLine line = new PptxLine();
        line.Present = true;
        line.Xml = element.OuterXml;
        line.Width = PptxTransform.GetLongAttribute(element, "w", 0L);
        line.Cap = PptxXml.GetAttributeValue(element, "", "cap");
        line.Compound = PptxXml.GetAttributeValue(element, "", "cmpd");
        line.Alignment = PptxXml.GetAttributeValue(element, "", "algn");

        PptxFill fill = PptxFill.FromShapeProperties(element);
        if (fill.Type == PptxFill.Solid)
        {
            line.Type = Solid;
            line.Color = fill.Color;
            line.Transparency = 100000 - fill.Color.Alpha;
        }

        if (fill.Type == PptxFill.None)
        {
            line.Type = None;
        }

        XmlElement? dash = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "prstDash");
        if (dash != null)
        {
            line.Dash = PptxXml.GetAttributeValue(dash, "", "val");
        }

        XmlElement? customDash = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "custDash");
        if (customDash != null)
        {
            for (int index = 0; index < customDash.ChildNodes.Count; index++)
            {
                XmlNode? child = customDash.ChildNodes[index];
                if (child == null || child.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                XmlElement pair = (XmlElement)child;
                if (pair.NamespaceURI == PptxNamespaces.Drawing && pair.LocalName == "ds")
                {
                    int dashLength = PptxXml.GetIntAttribute(pair, "", "d", 100000);
                    int spaceLength = PptxXml.GetIntAttribute(pair, "", "sp", 100000);
                    line.CustomDashes.Add(PptxLineDash.Create(dashLength, spaceLength));
                }
            }
        }

        LoadJoin(line, element);
        XmlElement? headEnd = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "headEnd");
        if (headEnd != null)
        {
            line.HeadArrowType = PptxXml.GetAttributeValue(headEnd, "", "type");
            line.HeadArrowWidth = PptxXml.GetAttributeValue(headEnd, "", "w");
            line.HeadArrowLength = PptxXml.GetAttributeValue(headEnd, "", "len");
        }

        XmlElement? tailEnd = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "tailEnd");
        if (tailEnd != null)
        {
            line.TailArrowType = PptxXml.GetAttributeValue(tailEnd, "", "type");
            line.TailArrowWidth = PptxXml.GetAttributeValue(tailEnd, "", "w");
            line.TailArrowLength = PptxXml.GetAttributeValue(tailEnd, "", "len");
        }

        return line;
    }

    internal XmlElement ToElement(XmlDocument document)
    {
        XmlElement element = document.CreateElement("a", "ln", PptxNamespaces.Drawing);
        if (Width > 0L)
        {
            PptxXml.SetAttributeValue(element, "", "", "w", Width.ToString());
        }

        if (Cap.Length > 0)
        {
            PptxXml.SetAttributeValue(element, "", "", "cap", Cap);
        }

        if (Compound.Length > 0)
        {
            PptxXml.SetAttributeValue(element, "", "", "cmpd", Compound);
        }

        if (Alignment.Length > 0)
        {
            PptxXml.SetAttributeValue(element, "", "", "algn", Alignment);
        }

        AppendFill(document, element);
        AppendDash(document, element);
        AppendJoin(document, element);
        AppendArrow(document, element, "headEnd", HeadArrowType, HeadArrowWidth, HeadArrowLength);
        AppendArrow(document, element, "tailEnd", TailArrowType, TailArrowWidth, TailArrowLength);
        return element;
    }

    internal void AppendFill(XmlDocument document, XmlElement element)
    {
        if (Type == Solid)
        {
            int transparency = ValidateTransparency(Transparency);
            PptxColor color = PptxStyleResolverCore.CloneColor(Color);
            color.Alpha = 100000 - transparency;
            XmlElement fill = document.CreateElement("a", "solidFill", PptxNamespaces.Drawing);
            fill.AppendChild(color.ToElement(document));
            element.AppendChild(fill);
            return;
        }

        element.AppendChild(document.CreateElement("a", "noFill", PptxNamespaces.Drawing));
    }

    internal void AppendDash(XmlDocument document, XmlElement element)
    {
        if (CustomDashes.Count > 0)
        {
            XmlElement custom = document.CreateElement("a", "custDash", PptxNamespaces.Drawing);
            foreach (PptxLineDash dashValue in CustomDashes)
            {
                if (dashValue.DashLength <= 0 || dashValue.SpaceLength <= 0)
                {
                    throw new System.ArgumentOutOfRangeException("CustomDashes", "Dash and space lengths must be greater than zero.");
                }

                XmlElement pair = document.CreateElement("a", "ds", PptxNamespaces.Drawing);
                PptxXml.SetAttributeValue(pair, "", "", "d", dashValue.DashLength.ToString());
                PptxXml.SetAttributeValue(pair, "", "", "sp", dashValue.SpaceLength.ToString());
                custom.AppendChild(pair);
            }

            element.AppendChild(custom);
            return;
        }

        if (Dash.Length == 0)
        {
            return;
        }

        XmlElement dash = document.CreateElement("a", "prstDash", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(dash, "", "", "val", Dash);
        element.AppendChild(dash);
    }

    internal void AppendJoin(XmlDocument document, XmlElement element)
    {
        if (Join == JoinRound)
        {
            element.AppendChild(document.CreateElement("a", "round", PptxNamespaces.Drawing));
            return;
        }

        if (Join == JoinBevel)
        {
            element.AppendChild(document.CreateElement("a", "bevel", PptxNamespaces.Drawing));
            return;
        }

        if (Join == JoinMiter)
        {
            XmlElement miter = document.CreateElement("a", "miter", PptxNamespaces.Drawing);
            if (MiterLimit > 0)
            {
                PptxXml.SetAttributeValue(miter, "", "", "lim", MiterLimit.ToString());
            }

            element.AppendChild(miter);
        }
    }

    internal void AppendArrow(XmlDocument document, XmlElement element, string localName, string arrowType, string width, string length)
    {
        if (arrowType.Length == 0)
        {
            return;
        }

        XmlElement arrow = document.CreateElement("a", localName, PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(arrow, "", "", "type", arrowType);
        if (width.Length > 0)
        {
            PptxXml.SetAttributeValue(arrow, "", "", "w", PptxDiagnostic.CleanText(width));
        }

        if (length.Length > 0)
        {
            PptxXml.SetAttributeValue(arrow, "", "", "len", PptxDiagnostic.CleanText(length));
        }

        element.AppendChild(arrow);
    }

    internal static void LoadJoin(PptxLine line, XmlElement element)
    {
        if (PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "round") != null)
        {
            line.Join = JoinRound;
            return;
        }

        if (PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "bevel") != null)
        {
            line.Join = JoinBevel;
            return;
        }

        if (PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "miter") != null)
        {
            line.Join = JoinMiter;
            XmlElement? miter = PptxXml.FindFirstChild(element, PptxNamespaces.Drawing, "miter");
            if (miter != null)
            {
                line.MiterLimit = PptxXml.GetIntAttribute(miter, "", "lim", 0);
            }
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
}
