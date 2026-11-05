using System.Collections.Generic;
using System.Xml;

namespace pptx;

internal static class PptxGeometry
{
    internal static XmlElement CreateFreeformGeometryElement(XmlDocument document, List<PptxPoint> points, long width, long height)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        ValidateFreeformPoints(points);

        XmlElement geometry = document.CreateElement("a", "custGeom", PptxNamespaces.Drawing);
        geometry.AppendChild(document.CreateElement("a", "avLst", PptxNamespaces.Drawing));
        geometry.AppendChild(document.CreateElement("a", "gdLst", PptxNamespaces.Drawing));
        geometry.AppendChild(document.CreateElement("a", "ahLst", PptxNamespaces.Drawing));
        geometry.AppendChild(document.CreateElement("a", "cxnLst", PptxNamespaces.Drawing));

        XmlElement rectangle = document.CreateElement("a", "rect", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(rectangle, "", "", "l", "l");
        PptxXml.SetAttributeValue(rectangle, "", "", "t", "t");
        PptxXml.SetAttributeValue(rectangle, "", "", "r", "r");
        PptxXml.SetAttributeValue(rectangle, "", "", "b", "b");
        geometry.AppendChild(rectangle);

        XmlElement pathList = document.CreateElement("a", "pathLst", PptxNamespaces.Drawing);
        XmlElement path = document.CreateElement("a", "path", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(path, "", "", "w", NormalizeDimension(width).ToString());
        PptxXml.SetAttributeValue(path, "", "", "h", NormalizeDimension(height).ToString());
        AppendMoveTo(document, path, points[0]);
        for (int index = 1; index < points.Count; index++)
        {
            AppendLineTo(document, path, points[index]);
        }

        path.AppendChild(document.CreateElement("a", "close", PptxNamespaces.Drawing));
        pathList.AppendChild(path);
        geometry.AppendChild(pathList);
        return geometry;
    }

    internal static XmlElement CreateFreeformPathsElement(XmlDocument document, List<PptxFreeformPath> paths)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        ValidateFreeformPaths(paths);
        XmlElement geometry = document.CreateElement("a", "custGeom", PptxNamespaces.Drawing);
        geometry.AppendChild(document.CreateElement("a", "avLst", PptxNamespaces.Drawing));
        geometry.AppendChild(document.CreateElement("a", "gdLst", PptxNamespaces.Drawing));
        geometry.AppendChild(document.CreateElement("a", "ahLst", PptxNamespaces.Drawing));
        geometry.AppendChild(document.CreateElement("a", "cxnLst", PptxNamespaces.Drawing));

        XmlElement rectangle = document.CreateElement("a", "rect", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(rectangle, "", "", "l", "l");
        PptxXml.SetAttributeValue(rectangle, "", "", "t", "t");
        PptxXml.SetAttributeValue(rectangle, "", "", "r", "r");
        PptxXml.SetAttributeValue(rectangle, "", "", "b", "b");
        geometry.AppendChild(rectangle);

        XmlElement pathList = document.CreateElement("a", "pathLst", PptxNamespaces.Drawing);
        for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
        {
            PptxFreeformPath source = paths[pathIndex];
            XmlElement path = document.CreateElement("a", "path", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(path, "", "", "w", source.Width.ToString());
            PptxXml.SetAttributeValue(path, "", "", "h", source.Height.ToString());
            for (int commandIndex = 0; commandIndex < source.Commands.Count; commandIndex++)
            {
                AppendCommand(document, path, source.Commands[commandIndex]);
            }

            pathList.AppendChild(path);
        }

        geometry.AppendChild(pathList);
        return geometry;
    }

    internal static List<PptxFreeformPath> ParseFreeformPaths(XmlElement? geometry, long fallbackWidth, long fallbackHeight)
    {
        List<PptxFreeformPath> paths = new List<PptxFreeformPath>();
        if (geometry == null)
        {
            return paths;
        }

        XmlElement? pathList = PptxXml.FindFirstChild(geometry, PptxNamespaces.Drawing, "pathLst");
        if (pathList == null)
        {
            return paths;
        }

        for (int pathIndex = 0; pathIndex < pathList.ChildNodes.Count; pathIndex++)
        {
            XmlNode? pathNode = pathList.ChildNodes[pathIndex];
            if (pathNode == null || pathNode.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement pathElement = (XmlElement)pathNode;
            if (pathElement.NamespaceURI != PptxNamespaces.Drawing || pathElement.LocalName != "path")
            {
                continue;
            }

            long width = NormalizeDimension(PptxTransform.GetLongAttribute(pathElement, "w", fallbackWidth));
            long height = NormalizeDimension(PptxTransform.GetLongAttribute(pathElement, "h", fallbackHeight));
            PptxFreeformPath path = new PptxFreeformPath(width, height);
            for (int commandIndex = 0; commandIndex < pathElement.ChildNodes.Count; commandIndex++)
            {
                XmlNode? commandNode = pathElement.ChildNodes[commandIndex];
                if (commandNode == null || commandNode.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                PptxFreeformCommand? command = ParseCommand((XmlElement)commandNode);
                if (command != null)
                {
                    path.Commands.Add(command);
                }
            }

            paths.Add(path);
        }

        return paths;
    }

    internal static void ValidateFreeformPaths(List<PptxFreeformPath> paths)
    {
        if (paths == null)
        {
            throw new System.ArgumentNullException("paths");
        }

        if (paths.Count == 0)
        {
            throw new System.ArgumentException("At least one path is required.");
        }

        for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
        {
            PptxFreeformPath? path = paths[pathIndex];
            if (path == null || path.Width <= 0L || path.Height <= 0L || path.Commands.Count == 0)
            {
                throw new System.ArgumentException("Each freeform path requires positive dimensions and commands.");
            }

            for (int commandIndex = 0; commandIndex < path.Commands.Count; commandIndex++)
            {
                ValidateCommand(path.Commands[commandIndex]);
            }
        }
    }

    internal static void ValidateCommand(PptxFreeformCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentException("Freeform commands cannot contain null values.");
        }

        int expected = 0;
        switch (command.Type)
        {
            case PptxFreeformCommandType.MoveTo:
                expected = 1;
                break;
            case PptxFreeformCommandType.LineTo:
                expected = 1;
                break;
            case PptxFreeformCommandType.CubicBezierTo:
                expected = 3;
                break;
            case PptxFreeformCommandType.QuadraticBezierTo:
                expected = 2;
                break;
            case PptxFreeformCommandType.Close:
                expected = 0;
                break;
            default:
                throw new System.ArgumentException("Unsupported freeform command type.");
        }

        if (command.Points.Count != expected)
        {
            throw new System.ArgumentException("Freeform command contains an invalid number of points.");
        }

        for (int index = 0; index < command.Points.Count; index++)
        {
            if (command.Points[index] == null)
            {
                throw new System.ArgumentException("Freeform command points cannot contain null values.");
            }
        }
    }

    internal static void AppendCommand(XmlDocument document, XmlElement path, PptxFreeformCommand command)
    {
        ValidateCommand(command);
        string localName = "close";
        switch (command.Type)
        {
            case PptxFreeformCommandType.MoveTo:
                localName = "moveTo";
                break;
            case PptxFreeformCommandType.LineTo:
                localName = "lnTo";
                break;
            case PptxFreeformCommandType.CubicBezierTo:
                localName = "cubicBezTo";
                break;
            case PptxFreeformCommandType.QuadraticBezierTo:
                localName = "quadBezTo";
                break;
            case PptxFreeformCommandType.Close:
                localName = "close";
                break;
        }

        XmlElement element = document.CreateElement("a", localName, PptxNamespaces.Drawing);
        for (int index = 0; index < command.Points.Count; index++)
        {
            element.AppendChild(CreatePointElement(document, command.Points[index]));
        }

        path.AppendChild(element);
    }

    internal static PptxFreeformCommand? ParseCommand(XmlElement element)
    {
        PptxFreeformCommandType type = PptxFreeformCommandType.Close;
        if (element.NamespaceURI != PptxNamespaces.Drawing)
        {
            return null;
        }

        if (element.LocalName == "moveTo")
        {
            type = PptxFreeformCommandType.MoveTo;
        }
        else if (element.LocalName == "lnTo")
        {
            type = PptxFreeformCommandType.LineTo;
        }
        else if (element.LocalName == "cubicBezTo")
        {
            type = PptxFreeformCommandType.CubicBezierTo;
        }
        else if (element.LocalName == "quadBezTo")
        {
            type = PptxFreeformCommandType.QuadraticBezierTo;
        }
        else if (element.LocalName != "close")
        {
            return null;
        }

        PptxFreeformCommand command = new PptxFreeformCommand(type);
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlNode? pointNode = element.ChildNodes[index];
            if (pointNode == null || pointNode.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement point = (XmlElement)pointNode;
            if (point.NamespaceURI == PptxNamespaces.Drawing && point.LocalName == "pt")
            {
                command.Points.Add(new PptxPoint(PptxTransform.GetLongAttribute(point, "x", 0L), PptxTransform.GetLongAttribute(point, "y", 0L)));
            }
        }

        ValidateCommand(command);
        return command;
    }

    internal static void ValidateFreeformPoints(List<PptxPoint> points)
    {
        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        if (points.Count < 2)
        {
            throw new System.ArgumentException("At least two points are required.");
        }

        for (int index = 0; index < points.Count; index++)
        {
            if (points[index] == null)
            {
                throw new System.ArgumentException("Freeform points cannot contain null values.");
            }
        }
    }

    internal static long NormalizeDimension(long value)
    {
        if (value <= 0L)
        {
            return 1L;
        }

        return value;
    }

    internal static void AppendMoveTo(XmlDocument document, XmlElement path, PptxPoint point)
    {
        XmlElement moveTo = document.CreateElement("a", "moveTo", PptxNamespaces.Drawing);
        moveTo.AppendChild(CreatePointElement(document, point));
        path.AppendChild(moveTo);
    }

    internal static void AppendLineTo(XmlDocument document, XmlElement path, PptxPoint point)
    {
        XmlElement lineTo = document.CreateElement("a", "lnTo", PptxNamespaces.Drawing);
        lineTo.AppendChild(CreatePointElement(document, point));
        path.AppendChild(lineTo);
    }

    internal static XmlElement CreatePointElement(XmlDocument document, PptxPoint point)
    {
        XmlElement element = document.CreateElement("a", "pt", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(element, "", "", "x", point.X.ToString());
        PptxXml.SetAttributeValue(element, "", "", "y", point.Y.ToString());
        return element;
    }
}
