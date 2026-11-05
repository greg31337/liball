using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace pdf;

internal class PdfSvgRenderer
{
    internal Dictionary<string, XmlElement> ClipPaths;

    internal PdfSvgRenderer()
    {
        ClipPaths = new Dictionary<string, XmlElement>(StringComparer.Ordinal);
    }

    internal void Render(PdfSvgData data, PdfCanvas canvas)
    {
        ValidateElementAttributes(data.Root);
        CollectDefinitions(data.Root);
        PdfSvgStyle style = PdfSvgStyleParser.Parse(data.Root, new PdfSvgStyle());
        canvas.SaveState();
        ApplyViewBox(data, canvas);
        ApplyTransformAttribute(data.Root, canvas);
        ApplyClipAttribute(data.Root, canvas, style);
        RenderChildren(data.Root, canvas, style);
        canvas.RestoreState();
    }

    internal void CollectDefinitions(XmlElement root)
    {
        foreach (XmlNode child in root.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)child;
                ValidateNamespace(element);
                if (element.LocalName == "defs")
                {
                    ValidateElementAttributes(element);
                    PdfSvgStyleParser.RejectUnsafeAttributes(element);
                    foreach (XmlNode definitionNode in element.ChildNodes)
                    {
                        if (definitionNode.NodeType == XmlNodeType.Element)
                        {
                            XmlElement definition = (XmlElement)definitionNode;
                            ValidateNamespace(definition);
                            if (definition.LocalName != "clipPath")
                            {
                                throw new InvalidDataException("SVG definition is unsupported: " + definition.LocalName + ".");
                            }

                            ValidateElementAttributes(definition);
                            PdfSvgStyleParser.RejectUnsafeAttributes(definition);
                            string id = definition.GetAttribute("id");
                            if (id.Length == 0 || ClipPaths.ContainsKey(id))
                            {
                                throw new InvalidDataException("SVG clip paths require unique non-empty identifiers.");
                            }

                            ClipPaths.Add(id, definition);
                        }
                    }
                }
            }
        }
    }

    internal void ApplyViewBox(PdfSvgData data, PdfCanvas canvas)
    {
        double scaleX = data.Width / data.ViewBoxWidth;
        double scaleY = data.Height / data.ViewBoxHeight;
        double offsetX = 0;
        double offsetY = 0;
        string preserve = data.Root.HasAttribute("preserveAspectRatio") ? data.Root.GetAttribute("preserveAspectRatio").Trim() : "xMidYMid meet";
        if (preserve == "none")
        {
            canvas.Transform(scaleX, 0, 0, -scaleY, -data.ViewBoxX * scaleX, data.Height + data.ViewBoxY * scaleY);
            return;
        }

        string[] pieces = preserve.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length > 2 || pieces.Length == 0 || pieces.Length == 2 && pieces[1] != "meet")
        {
            throw new InvalidDataException("SVG preserveAspectRatio value is unsupported.");
        }

        string alignment = pieces[0];
        if (alignment != "xMinYMin" && alignment != "xMidYMin" && alignment != "xMaxYMin" && alignment != "xMinYMid" && alignment != "xMidYMid" && alignment != "xMaxYMid" && alignment != "xMinYMax" && alignment != "xMidYMax" && alignment != "xMaxYMax")
        {
            throw new InvalidDataException("SVG preserveAspectRatio alignment is unsupported.");
        }

        double scale = Math.Min(scaleX, scaleY);
        double extraX = data.Width - data.ViewBoxWidth * scale;
        double extraY = data.Height - data.ViewBoxHeight * scale;
        if (alignment.StartsWith("xMid", StringComparison.Ordinal))
        {
            offsetX = extraX / 2;
        }
        else if (alignment.StartsWith("xMax", StringComparison.Ordinal))
        {
            offsetX = extraX;
        }

        if (alignment.EndsWith("YMid", StringComparison.Ordinal))
        {
            offsetY = extraY / 2;
        }
        else if (alignment.EndsWith("YMax", StringComparison.Ordinal))
        {
            offsetY = extraY;
        }

        canvas.Transform(scale, 0, 0, -scale, offsetX - data.ViewBoxX * scale, data.Height - offsetY + data.ViewBoxY * scale);
    }

    internal void RenderChildren(XmlElement parent, PdfCanvas canvas, PdfSvgStyle inherited)
    {
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                RenderElement((XmlElement)child, canvas, inherited);
            }
            else if (child.NodeType != XmlNodeType.Text && child.NodeType != XmlNodeType.CDATA && child.NodeType != XmlNodeType.Whitespace && child.NodeType != XmlNodeType.SignificantWhitespace)
            {
                throw new InvalidDataException("SVG contains an unsupported XML node.");
            }
        }
    }

    internal void RenderElement(XmlElement element, PdfCanvas canvas, PdfSvgStyle inherited)
    {
        ValidateNamespace(element);
        ValidateElementAttributes(element);
        string name = element.LocalName;
        if (name == "defs")
        {
            return;
        }

        if (name == "title" || name == "desc" || name == "metadata")
        {
            ValidatePlainMetadata(element);
            return;
        }

        if (name == "script" || name == "image" || name == "use" || name == "foreignObject" || name == "style" || name == "linearGradient" || name == "radialGradient" || name == "filter" || name == "mask" || name == "pattern" || name == "symbol")
        {
            throw new InvalidDataException("SVG element is unsafe or unsupported: " + name + ".");
        }

        PdfSvgStyle style = PdfSvgStyleParser.Parse(element, inherited);
        canvas.SaveState();
        ApplyTransformAttribute(element, canvas);
        ApplyClipAttribute(element, canvas, style);
        if (style.Visible)
        {
            if (name == "g")
            {
                RenderChildren(element, canvas, style);
            }
            else if (name == "text")
            {
                RenderText(element, canvas, style);
            }
            else if (IsShape(name))
            {
                PdfPath path = CreateShapePath(element);
                PaintPath(path, canvas, style, name == "line" || name == "polyline");
            }
            else
            {
                throw new InvalidDataException("SVG element is unsupported: " + name + ".");
            }
        }

        canvas.RestoreState();
    }

    internal bool IsShape(string name)
    {
        return name == "path" || name == "rect" || name == "circle" || name == "ellipse" || name == "line" || name == "polyline" || name == "polygon";
    }

    internal void ValidatePlainMetadata(XmlElement element)
    {
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                throw new InvalidDataException("SVG metadata containing elements is unsupported.");
            }
        }
    }

    internal PdfPath CreateShapePath(XmlElement element)
    {
        string name = element.LocalName;
        if (name == "path")
        {
            string data = RequireAttribute(element, "d");
            return PdfSvgPathParser.Parse(data);
        }

        if (name == "rect")
        {
            return CreateRectanglePath(element);
        }

        if (name == "circle")
        {
            double radius = RequiredLength(element, "r");
            if (radius <= 0)
            {
                throw new InvalidDataException("SVG circle radius must be positive.");
            }

            return PdfPath.Circle(OptionalLength(element, "cx", 0), OptionalLength(element, "cy", 0), radius);
        }

        if (name == "ellipse")
        {
            double radiusX = RequiredLength(element, "rx");
            double radiusY = RequiredLength(element, "ry");
            if (radiusX <= 0 || radiusY <= 0)
            {
                throw new InvalidDataException("SVG ellipse radii must be positive.");
            }

            return PdfPath.Ellipse(OptionalLength(element, "cx", 0), OptionalLength(element, "cy", 0), radiusX, radiusY);
        }

        if (name == "line")
        {
            PdfPath line = new PdfPath();
            line.MoveTo(OptionalLength(element, "x1", 0), OptionalLength(element, "y1", 0));
            line.LineTo(OptionalLength(element, "x2", 0), OptionalLength(element, "y2", 0));
            return line;
        }

        return CreatePointsPath(element, name == "polygon");
    }

    internal PdfPath CreateRectanglePath(XmlElement element)
    {
        double x = OptionalLength(element, "x", 0);
        double y = OptionalLength(element, "y", 0);
        double width = RequiredLength(element, "width");
        double height = RequiredLength(element, "height");
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("SVG rectangle dimensions must be positive.");
        }

        bool hasRadiusX = element.HasAttribute("rx");
        bool hasRadiusY = element.HasAttribute("ry");
        if (!hasRadiusX && !hasRadiusY)
        {
            PdfPath rectangle = new PdfPath();
            rectangle.MoveTo(x, y).LineTo(x + width, y).LineTo(x + width, y + height).LineTo(x, y + height).Close();
            return rectangle;
        }

        double radiusX = hasRadiusX ? OptionalLength(element, "rx", 0) : OptionalLength(element, "ry", 0);
        double radiusY = hasRadiusY ? OptionalLength(element, "ry", 0) : radiusX;
        if (radiusX < 0 || radiusY < 0)
        {
            throw new InvalidDataException("SVG rectangle corner radii cannot be negative.");
        }

        radiusX = Math.Min(radiusX, width / 2);
        radiusY = Math.Min(radiusY, height / 2);
        return CreateRoundedRectanglePath(x, y, width, height, radiusX, radiusY);
    }

    internal PdfPath CreateRoundedRectanglePath(double x, double y, double width, double height, double radiusX, double radiusY)
    {
        double controlX = radiusX * PdfPath.BezierCircleControl;
        double controlY = radiusY * PdfPath.BezierCircleControl;
        double right = x + width;
        double bottom = y + height;
        PdfPath path = new PdfPath();
        path.MoveTo(x + radiusX, y).LineTo(right - radiusX, y);
        path.CurveTo(right - radiusX + controlX, y, right, y + radiusY - controlY, right, y + radiusY);
        path.LineTo(right, bottom - radiusY);
        path.CurveTo(right, bottom - radiusY + controlY, right - radiusX + controlX, bottom, right - radiusX, bottom);
        path.LineTo(x + radiusX, bottom);
        path.CurveTo(x + radiusX - controlX, bottom, x, bottom - radiusY + controlY, x, bottom - radiusY);
        path.LineTo(x, y + radiusY);
        path.CurveTo(x, y + radiusY - controlY, x + radiusX - controlX, y, x + radiusX, y).Close();
        return path;
    }

    internal PdfPath CreatePointsPath(XmlElement element, bool close)
    {
        double[] points = PdfSvgValueParser.ParseNumberList(RequireAttribute(element, "points"));
        if (points.Length < 4 || points.Length % 2 != 0)
        {
            throw new InvalidDataException("SVG points must contain at least two coordinate pairs.");
        }

        PdfPath path = new PdfPath();
        path.MoveTo(points[0], points[1]);
        for (int index = 2; index < points.Length; index += 2)
        {
            path.LineTo(points[index], points[index + 1]);
        }

        if (close)
        {
            path.Close();
        }

        return path;
    }

    internal void PaintPath(PdfPath path, PdfCanvas canvas, PdfSvgStyle style, bool openShape)
    {
        bool fill = !openShape && style.Fill != "none" && style.Fill != "transparent";
        bool stroke = style.Stroke != "none" && style.Stroke != "transparent" && style.StrokeWidth > 0;
        if (!fill && !stroke)
        {
            return;
        }

        if (fill)
        {
            canvas.SetFillColorValue(PdfSvgStyleParser.ParseColor(style.Fill));
        }

        if (stroke)
        {
            canvas.SetStrokeColorValue(PdfSvgStyleParser.ParseColor(style.Stroke));
            canvas.SetLineWidth(style.StrokeWidth);
            canvas.SetLineCap(style.LineCap);
            canvas.SetLineJoin(style.LineJoin);
            canvas.SetDash(style.DashPattern, style.DashOffset);
        }

        canvas.SetTransparency(style.FillOpacity * style.Opacity, style.StrokeOpacity * style.Opacity, PdfBlendMode.Normal);
        if (fill && stroke)
        {
            if (style.FillRuleEvenOdd)
            {
                canvas.FillStrokePathEvenOdd(path);
            }
            else
            {
                canvas.FillStrokePath(path);
            }
        }
        else if (fill)
        {
            if (style.FillRuleEvenOdd)
            {
                canvas.FillPathEvenOdd(path);
            }
            else
            {
                canvas.FillPath(path);
            }
        }
        else
        {
            canvas.StrokePath(path);
        }
    }

    internal void ApplyTransformAttribute(XmlElement element, PdfCanvas canvas)
    {
        if (!element.HasAttribute("transform"))
        {
            return;
        }

        PdfSvgNumberReader reader = new PdfSvgNumberReader(element.GetAttribute("transform"));
        while (reader.HasMore())
        {
            string function = reader.ReadIdentifier();
            reader.RequireCharacter('(');
            List<double> values = new List<double>();
            while (reader.NextIsNumber())
            {
                values.Add(reader.ReadNumber());
            }

            reader.RequireCharacter(')');
            ApplyTransformFunction(function, values, canvas);
        }
    }

    internal void ApplyTransformFunction(string function, List<double> values, PdfCanvas canvas)
    {
        if (function == "matrix" && values.Count == 6)
        {
            canvas.Transform(values[0], values[1], values[2], values[3], values[4], values[5]);
        }
        else if (function == "translate" && (values.Count == 1 || values.Count == 2))
        {
            canvas.Translate(values[0], values.Count == 2 ? values[1] : 0);
        }
        else if (function == "scale" && (values.Count == 1 || values.Count == 2))
        {
            canvas.Scale(values[0], values.Count == 2 ? values[1] : values[0]);
        }
        else if (function == "rotate" && (values.Count == 1 || values.Count == 3))
        {
            if (values.Count == 3)
            {
                canvas.Translate(values[1], values[2]);
                canvas.Rotate(values[0]);
                canvas.Translate(-values[1], -values[2]);
            }
            else
            {
                canvas.Rotate(values[0]);
            }
        }
        else if (function == "skewX" && values.Count == 1)
        {
            canvas.Skew(values[0], 0);
        }
        else if (function == "skewY" && values.Count == 1)
        {
            canvas.Skew(0, values[0]);
        }
        else
        {
            throw new InvalidDataException("SVG transform function or argument count is unsupported.");
        }
    }

    internal void ApplyClipAttribute(XmlElement element, PdfCanvas canvas, PdfSvgStyle style)
    {
        if (!element.HasAttribute("clip-path"))
        {
            return;
        }

        string reference = element.GetAttribute("clip-path").Trim();
        if (!reference.StartsWith("url(#", StringComparison.Ordinal) || !reference.EndsWith(")", StringComparison.Ordinal) || reference.Length <= 6)
        {
            throw new InvalidDataException("SVG clip path must be a local fragment reference.");
        }

        string id = reference.Substring(5, reference.Length - 6);
        if (!ClipPaths.ContainsKey(id))
        {
            throw new InvalidDataException("SVG clip path reference was not found.");
        }

        XmlElement clip = ClipPaths[id];
        string units = clip.HasAttribute("clipPathUnits") ? clip.GetAttribute("clipPathUnits") : "userSpaceOnUse";
        if (units != "userSpaceOnUse")
        {
            throw new InvalidDataException("SVG object-bounding-box clip paths are unsupported.");
        }

        XmlElement? shape = null;
        foreach (XmlNode node in clip.ChildNodes)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                if (shape != null)
                {
                    throw new InvalidDataException("SVG clip paths must contain exactly one supported shape.");
                }

                shape = (XmlElement)node;
            }
        }

        if (shape == null || !IsShape(shape.LocalName) || shape.LocalName == "line" || shape.LocalName == "polyline")
        {
            throw new InvalidDataException("SVG clip path must contain one closed shape.");
        }

        ValidateNamespace(shape);
        ValidateElementAttributes(shape);
        PdfSvgStyle clipStyle = PdfSvgStyleParser.Parse(clip, style);
        clipStyle = PdfSvgStyleParser.Parse(shape, clipStyle);
        if (clip.HasAttribute("transform") || shape.HasAttribute("transform"))
        {
            throw new InvalidDataException("SVG transformed clip paths are unsupported.");
        }

        PdfPath path = CreateShapePath(shape);
        if (clipStyle.ClipRuleEvenOdd)
        {
            canvas.ClipPathEvenOdd(path);
        }
        else
        {
            canvas.ClipPath(path);
        }

    }

    internal void RenderText(XmlElement element, PdfCanvas canvas, PdfSvgStyle style)
    {
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                throw new InvalidDataException("SVG nested text content is unsupported; use plain text fallback content.");
            }
        }

        if (style.Stroke != "none" && style.Stroke != "transparent")
        {
            throw new InvalidDataException("SVG stroked text fallback is unsupported.");
        }

        if (style.Fill == "none" || style.Fill == "transparent")
        {
            return;
        }

        string text = element.InnerText;
        double x = OptionalLength(element, "x", 0) + OptionalLength(element, "dx", 0);
        double y = OptionalLength(element, "y", 0) + OptionalLength(element, "dy", 0);
        PdfFont font = SelectFont(style);
        double width = PdfCanvas.MeasureText(text, style.FontSize, font);
        if (style.TextAnchor == "middle")
        {
            x -= width / 2;
        }
        else if (style.TextAnchor == "end")
        {
            x -= width;
        }

        canvas.SetFillColorValue(PdfSvgStyleParser.ParseColor(style.Fill));
        canvas.SetTransparency(style.FillOpacity * style.Opacity, style.StrokeOpacity * style.Opacity, PdfBlendMode.Normal);
        canvas.Transform(1, 0, 0, -1, 0, 2 * y);
        canvas.DrawTextWithFont(text, x, y, style.FontSize, font);
    }

    internal PdfFont SelectFont(PdfSvgStyle style)
    {
        if (style.FontBold && style.FontItalic)
        {
            return PdfFont.HelveticaBoldOblique;
        }

        if (style.FontBold)
        {
            return PdfFont.HelveticaBold;
        }

        if (style.FontItalic)
        {
            return PdfFont.HelveticaOblique;
        }

        return PdfFont.Helvetica;
    }

    internal void ValidateNamespace(XmlElement element)
    {
        if (element.NamespaceURI.Length > 0 && element.NamespaceURI != "http://www.w3.org/2000/svg")
        {
            throw new InvalidDataException("SVG element uses an unsupported namespace.");
        }
    }

    internal void ValidateElementAttributes(XmlElement element)
    {
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (attribute.Name == "xmlns" || attribute.Prefix == "xmlns")
            {
                continue;
            }

            string name = attribute.LocalName;
            if (!IsAllowedAttribute(name, element.LocalName))
            {
                throw new InvalidDataException("SVG attribute is unsupported: " + name + ".");
            }
        }
    }

    internal bool IsAllowedAttribute(string name, string elementName)
    {
        if (name == "id" || name == "style" || name == "transform" || name == "clip-path" || name == "fill" || name == "stroke" || name == "stroke-width" || name == "fill-rule" || name == "clip-rule" || name == "fill-opacity" || name == "stroke-opacity" || name == "opacity" || name == "stroke-linecap" || name == "stroke-linejoin" || name == "stroke-dasharray" || name == "stroke-dashoffset" || name == "font-size" || name == "font-family" || name == "font-weight" || name == "font-style" || name == "text-anchor" || name == "display" || name == "visibility")
        {
            return true;
        }

        if (elementName == "svg")
        {
            return name == "width" || name == "height" || name == "viewBox" || name == "preserveAspectRatio" || name == "version";
        }

        if (elementName == "path")
        {
            return name == "d";
        }

        if (elementName == "rect")
        {
            return name == "x" || name == "y" || name == "width" || name == "height" || name == "rx" || name == "ry";
        }

        if (elementName == "circle")
        {
            return name == "cx" || name == "cy" || name == "r";
        }

        if (elementName == "ellipse")
        {
            return name == "cx" || name == "cy" || name == "rx" || name == "ry";
        }

        if (elementName == "line")
        {
            return name == "x1" || name == "y1" || name == "x2" || name == "y2";
        }

        if (elementName == "polyline" || elementName == "polygon")
        {
            return name == "points";
        }

        if (elementName == "text")
        {
            return name == "x" || name == "y" || name == "dx" || name == "dy";
        }

        if (elementName == "clipPath")
        {
            return name == "clipPathUnits";
        }

        return false;
    }

    internal string RequireAttribute(XmlElement element, string name)
    {
        if (!element.HasAttribute(name) || element.GetAttribute(name).Trim().Length == 0)
        {
            throw new InvalidDataException("SVG " + element.LocalName + " requires the " + name + " attribute.");
        }

        return element.GetAttribute(name);
    }

    internal double RequiredLength(XmlElement element, string name)
    {
        return PdfSvgValueParser.ParseLengthUserUnits(RequireAttribute(element, name));
    }

    internal double OptionalLength(XmlElement element, string name, double fallback)
    {
        if (!element.HasAttribute(name))
        {
            return fallback;
        }

        return PdfSvgValueParser.ParseLengthUserUnits(element.GetAttribute(name));
    }
}
