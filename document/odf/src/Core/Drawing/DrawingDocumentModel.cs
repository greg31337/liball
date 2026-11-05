using System.Collections.Generic;
using System.Text;

namespace odf;

internal static class DrawingDocumentModel
{
    internal static void LoadDrawings(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for drawing materialization.");
        }
        List<OdfDrawingObject> objects = new List<OdfDrawingObject>();
        long order = 0;
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Root != null)
            {
                order = CollectDrawings(
                    part.Root,
                    part.Path,
                    null,
                    null,
                    objects,
                    order);
            }
            index = index + 1;
        }
        document.DrawingObjects = objects.ToArray();
        ResolveShapeContent(document, document.DrawingObjects);
        ThreeDimensionalDrawingModel.ResolveMaterials(
            document,
            document.DrawingObjects);
        GraphicPropertyModel.Resolve(document, document.DrawingObjects);
        ResolveDrawingReferences(document.DrawingObjects);
        BuildStackingOrders(document.DrawingObjects);
        ResolveEventSounds(document, document.DrawingObjects);
    }

    internal static void SynchronizeDrawings(
        OdfDocument document,
        OdfSaveOptions options)
    {
        if (document == null || document.DrawingObjects == null)
        {
            throw new OdfDocumentException(
                "A document and drawing-object collection are required.");
        }
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            OdfDrawingObject drawing = document.DrawingObjects[index];
            if (drawing == null || drawing.Node == null)
            {
                throw new OdfEditException(
                    "A drawing object requires an underlying XML element.");
            }
            ApplyDrawingFields(drawing, options);
            ThreeDimensionalDrawingModel.Apply(drawing, options);
            GraphicPropertyModel.Apply(drawing);
            ApplyEvents(drawing.Events);
            ApplyReferences(drawing);
            drawing.References = CreateReferences(drawing, drawing.Node);
            CaptureOriginalValues(drawing);
            index = index + 1;
        }
        ThreeDimensionalDrawingModel.ResolveMaterials(
            document,
            document.DrawingObjects);
        GraphicPropertyModel.Resolve(document, document.DrawingObjects);
        ResolveDrawingReferences(document.DrawingObjects);
        BuildStackingOrders(document.DrawingObjects);
    }

    static long CollectDrawings(
        OdfNode node,
        string partPath,
        OdfDrawingObject? parent,
        OdfDrawingObject? page,
        List<OdfDrawingObject> objects,
        long order)
    {
        OdfDrawingKind kind = DrawingKind(node);
        OdfDrawingObject? current = parent;
        OdfDrawingObject? currentPage = page;
        if (kind != OdfDrawingKind.Unknown)
        {
            OdfDrawingObject drawing = CreateDrawingObject(
                node,
                partPath,
                kind,
                order);
            drawing.Parent = parent;
            if (kind == OdfDrawingKind.Page)
            {
                drawing.Page = drawing;
                currentPage = drawing;
            }
            else
            {
                drawing.Page = page;
            }
            if (parent != null)
            {
                parent.Children = AppendObject(parent.Children, drawing);
            }
            objects.Add(drawing);
            current = drawing;
            order = order + 1;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            order = CollectDrawings(
                node.Children[index],
                partPath,
                current,
                currentPage,
                objects,
                order);
            index = index + 1;
        }
        return order;
    }

    static OdfDrawingObject CreateDrawingObject(
        OdfNode node,
        string partPath,
        OdfDrawingKind kind,
        long order)
    {
        OdfDrawingObject result = new OdfDrawingObject();
        result.Kind = kind;
        result.ElementName = node.Name == null ? "" : node.Name.LocalName;
        result.Id = Read(node, OdfNamespaces.Drawing, "id");
        result.XmlId = Read(node, OdfNamespaces.Xml, "id");
        if (result.XmlId.Length == 0)
        {
            result.XmlId = node.XmlId;
        }
        result.Name = Read(node, OdfNamespaces.Drawing, "name");
        result.DisplayName = Read(
            node,
            OdfNamespaces.Drawing,
            "display-name");
        ReadStyleFields(result, node);
        result.TextStyleName = Read(
            node,
            OdfNamespaces.Drawing,
            "text-style-name");
        result.LayerName = Read(node, OdfNamespaces.Drawing, "layer");
        result.MasterPageName = Read(
            node,
            OdfNamespaces.Drawing,
            "master-page-name");
        result.PresentationPageLayoutName = Read(
            node,
            OdfNamespaces.Presentation,
            "presentation-page-layout-name");
        result.NavigationOrder = Read(
            node,
            OdfNamespaces.Drawing,
            "nav-order");
        result.Display = Read(node, OdfNamespaces.Drawing, "display");
        result.ProtectedValue = Read(
            node,
            OdfNamespaces.Drawing,
            "protected");
        OdfLexicalValue protectedValue =
            OdfLexicalParser.ParseBoolean(result.ProtectedValue);
        result.IsProtected = protectedValue.IsValid
            && protectedValue.BooleanValue;
        result.X = Read(node, OdfNamespaces.Svg, "x");
        result.Y = Read(node, OdfNamespaces.Svg, "y");
        result.Width = Read(node, OdfNamespaces.Svg, "width");
        result.Height = Read(node, OdfNamespaces.Svg, "height");
        result.Geometry = CreateGeometry(node);
        result.ThreeDimensional = ThreeDimensionalDrawingModel.Create(
            node,
            kind);
        result.ControlId = Read(
            node,
            OdfNamespaces.Drawing,
            "control");
        result.Anchor = CreateAnchor(node);
        result.ZOrder = CreateZOrder(node, order);
        result.Transform = CreateTransform(node);
        result.Contour = CreateContour(node);
        result.Accessibility = CreateAccessibility(node);
        result.Events = CreateEvents(node);
        result.References = CreateReferences(result, node);
        result.PartPath = partPath;
        result.Node = node;
        CaptureOriginalValues(result);
        return result;
    }

    static void ReadStyleFields(OdfDrawingObject result, OdfNode node)
    {
        string drawingStyle = Read(
            node,
            OdfNamespaces.Drawing,
            "style-name");
        string presentationStyle = Read(
            node,
            OdfNamespaces.Presentation,
            "style-name");
        if (drawingStyle.Length > 0)
        {
            result.StyleName = drawingStyle;
            result.StyleNamespace = OdfNamespaces.Drawing;
            result.ClassNames = Read(
                node,
                OdfNamespaces.Drawing,
                "class-names");
        }
        else if (presentationStyle.Length > 0)
        {
            result.StyleName = presentationStyle;
            result.StyleNamespace = OdfNamespaces.Presentation;
            result.ClassNames = Read(
                node,
                OdfNamespaces.Presentation,
                "class-names");
        }
        else
        {
            string drawingClasses = Read(
                node,
                OdfNamespaces.Drawing,
                "class-names");
            string presentationClasses = Read(
                node,
                OdfNamespaces.Presentation,
                "class-names");
            if (drawingClasses.Length > 0)
            {
                result.ClassNames = drawingClasses;
                result.StyleNamespace = OdfNamespaces.Drawing;
            }
            else if (presentationClasses.Length > 0)
            {
                result.ClassNames = presentationClasses;
                result.StyleNamespace = OdfNamespaces.Presentation;
            }
        }
    }

    static OdfDrawingAnchor CreateAnchor(OdfNode node)
    {
        OdfDrawingAnchor result = new OdfDrawingAnchor();
        result.Type = Read(node, OdfNamespaces.Text, "anchor-type");
        result.Kind = ParseAnchorKind(result.Type);
        result.PageNumberValue = Read(
            node,
            OdfNamespaces.Text,
            "anchor-page-number");
        result.HasPageNumber = result.PageNumberValue.Length > 0;
        OdfLexicalValue pageNumber = OdfLexicalParser.ParseInteger(
            result.PageNumberValue);
        result.IsPageNumberValid = result.HasPageNumber
            && pageNumber.IsValid
            && pageNumber.IntegerValue > 0;
        if (result.IsPageNumberValid)
        {
            result.PageNumber = pageNumber.IntegerValue;
        }
        result.EndCellAddress = Read(
            node,
            OdfNamespaces.Table,
            "end-cell-address");
        result.EndX = Read(node, OdfNamespaces.Table, "end-x");
        result.EndY = Read(node, OdfNamespaces.Table, "end-y");
        result.TableBackgroundValue = Read(
            node,
            OdfNamespaces.Table,
            "table-background");
        OdfLexicalValue background = OdfLexicalParser.ParseBoolean(
            result.TableBackgroundValue);
        result.IsTableBackgroundValid =
            result.TableBackgroundValue.Length == 0 || background.IsValid;
        result.IsTableBackground = background.IsValid
            && background.BooleanValue;
        CaptureAnchorOriginalValues(result);
        return result;
    }

    static OdfDrawingZOrder CreateZOrder(OdfNode node, long order)
    {
        OdfDrawingZOrder result = new OdfDrawingZOrder();
        result.LexicalValue = Read(
            node,
            OdfNamespaces.Drawing,
            "z-index");
        result.IsSpecified = result.LexicalValue.Length > 0;
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(
            result.LexicalValue);
        result.IsValid = !result.IsSpecified
            || parsed.IsValid && parsed.IntegerValue >= 0;
        if (result.IsSpecified && result.IsValid)
        {
            result.Index = parsed.IntegerValue;
        }
        result.DocumentOrder = order;
        result.OriginalLexicalValue = result.LexicalValue;
        result.OriginalIndex = result.Index;
        return result;
    }

    static OdfGeometry CreateGeometry(OdfNode node)
    {
        OdfGeometry result = new OdfGeometry();
        result.X1 = Read(node, OdfNamespaces.Svg, "x1");
        result.Y1 = Read(node, OdfNamespaces.Svg, "y1");
        result.X2 = Read(node, OdfNamespaces.Svg, "x2");
        result.Y2 = Read(node, OdfNamespaces.Svg, "y2");
        result.CenterX = Read(node, OdfNamespaces.Svg, "cx");
        result.CenterY = Read(node, OdfNamespaces.Svg, "cy");
        result.Radius = Read(node, OdfNamespaces.Svg, "r");
        result.RadiusX = Read(node, OdfNamespaces.Svg, "rx");
        result.RadiusY = Read(node, OdfNamespaces.Svg, "ry");
        result.CornerRadius = Read(
            node,
            OdfNamespaces.Drawing,
            "corner-radius");
        result.EllipseKind = Read(node, OdfNamespaces.Drawing, "kind");
        result.StartAngle = Read(
            node,
            OdfNamespaces.Drawing,
            "start-angle");
        result.EndAngle = Read(
            node,
            OdfNamespaces.Drawing,
            "end-angle");
        result.Points = Read(node, OdfNamespaces.Drawing, "points");
        result.PathData = Read(node, OdfNamespaces.Svg, "d");
        result.ViewBox = Read(node, OdfNamespaces.Svg, "viewBox");
        result.PointList = SvgGeometryModel.ParsePoints(result.Points);
        result.Path = SvgGeometryModel.ParsePath(result.PathData);
        result.ViewBoxValue = SvgGeometryModel.ParseViewBox(result.ViewBox);
        result.CaptionPointX = Read(
            node,
            OdfNamespaces.Drawing,
            "caption-point-x");
        result.CaptionPointY = Read(
            node,
            OdfNamespaces.Drawing,
            "caption-point-y");
        result.ConnectorType = Read(
            node,
            OdfNamespaces.Drawing,
            "type");
        result.StartGluePoint = Read(
            node,
            OdfNamespaces.Drawing,
            "start-glue-point");
        result.EndGluePoint = Read(
            node,
            OdfNamespaces.Drawing,
            "end-glue-point");
        result.LineSkew = Read(
            node,
            OdfNamespaces.Drawing,
            "line-skew");
        result.Corners = Read(
            node,
            OdfNamespaces.Drawing,
            "corners");
        result.ConcaveValue = Read(
            node,
            OdfNamespaces.Drawing,
            "concave");
        OdfLexicalValue concave = OdfLexicalParser.ParseBoolean(
            result.ConcaveValue);
        result.IsConcaveValid = result.ConcaveValue.Length == 0
            || concave.IsValid;
        result.IsConcave = concave.IsValid && concave.BooleanValue;
        result.Sharpness = Read(
            node,
            OdfNamespaces.Drawing,
            "sharpness");
        OdfNode? enhancedNode = FindDirectChild(
            node,
            OdfNamespaces.Drawing,
            "enhanced-geometry");
        if (enhancedNode != null)
        {
            result.EnhancedGeometry = CreateEnhancedGeometry(enhancedNode);
        }
        result.GluePoints = CreateGluePoints(node);
        result.Routing = CreateConnectorRouting(node);
        result.Node = node;
        CaptureGeometryOriginalValues(result);
        return result;
    }

    static OdfEnhancedGeometry CreateEnhancedGeometry(OdfNode node)
    {
        OdfEnhancedGeometry result = new OdfEnhancedGeometry();
        result.Preset.Name = Read(node, OdfNamespaces.Drawing, "type");
        result.Preset.IsSpecified = result.Preset.Name.Length > 0;
        result.Preset.IsNonPrimitive = result.Preset.Name == "non-primitive";
        result.Preset.OriginalName = result.Preset.Name;
        result.ViewBox = Read(node, OdfNamespaces.Svg, "viewBox");
        result.ViewBoxValue = SvgGeometryModel.ParseViewBox(result.ViewBox);
        result.ModifierText = Read(
            node,
            OdfNamespaces.Drawing,
            "modifiers");
        result.Modifiers = CreateModifiers(result.ModifierText);
        result.EnhancedPath = Read(
            node,
            OdfNamespaces.Drawing,
            "enhanced-path");
        result.StretchPointX = Read(
            node,
            OdfNamespaces.Drawing,
            "path-stretchpoint-x");
        result.StretchPointY = Read(
            node,
            OdfNamespaces.Drawing,
            "path-stretchpoint-y");
        result.TextAreas = Read(
            node,
            OdfNamespaces.Drawing,
            "text-areas");
        result.GluePoints = Read(
            node,
            OdfNamespaces.Drawing,
            "glue-points");
        result.GluePointType = Read(
            node,
            OdfNamespaces.Drawing,
            "glue-point-type");
        result.GluePointLeavingDirections = Read(
            node,
            OdfNamespaces.Drawing,
            "glue-point-leaving-directions");
        result.MirrorVerticalValue = Read(
            node,
            OdfNamespaces.Drawing,
            "mirror-vertical");
        result.MirrorHorizontalValue = Read(
            node,
            OdfNamespaces.Drawing,
            "mirror-horizontal");
        result.TextRotateAngle = Read(
            node,
            OdfNamespaces.Drawing,
            "text-rotate-angle");
        RefreshEnhancedBooleans(result);
        result.Equations = CreateEquations(node);
        result.Handles = CreateHandles(node);
        result.OriginalEquationNodes = EquationNodes(result.Equations);
        result.OriginalHandleNodes = HandleNodes(result.Handles);
        result.Node = node;
        CaptureEnhancedOriginalValues(result);
        return result;
    }

    static OdfGeometryModifier[] CreateModifiers(string text)
    {
        List<OdfGeometryModifier> result =
            new List<OdfGeometryModifier>();
        int index = 0;
        while (index < text.Length)
        {
            while (index < text.Length
                && global::System.Char.IsWhiteSpace(text[index]))
            {
                index = index + 1;
            }
            int start = index;
            while (index < text.Length
                && !global::System.Char.IsWhiteSpace(text[index]))
            {
                index = index + 1;
            }
            if (index > start)
            {
                OdfGeometryModifier modifier = new OdfGeometryModifier();
                modifier.Index = result.Count;
                modifier.Value = text.Substring(start, index - start);
                modifier.OriginalValue = modifier.Value;
                result.Add(modifier);
            }
        }
        return result.ToArray();
    }

    static OdfGeometryEquation[] CreateEquations(OdfNode node)
    {
        List<OdfGeometryEquation> result =
            new List<OdfGeometryEquation>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, OdfNamespaces.Drawing, "equation"))
            {
                OdfGeometryEquation equation = new OdfGeometryEquation();
                equation.Name = Read(
                    child,
                    OdfNamespaces.Drawing,
                    "name");
                equation.Formula = Read(
                    child,
                    OdfNamespaces.Drawing,
                    "formula");
                equation.Node = child;
                equation.OriginalName = equation.Name;
                equation.OriginalFormula = equation.Formula;
                result.Add(equation);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfGeometryHandle[] CreateHandles(OdfNode node)
    {
        List<OdfGeometryHandle> result = new List<OdfGeometryHandle>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, OdfNamespaces.Drawing, "handle"))
            {
                result.Add(CreateHandle(child));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfGeometryHandle CreateHandle(OdfNode node)
    {
        OdfGeometryHandle result = new OdfGeometryHandle();
        result.PositionX = Read(node, OdfNamespaces.Drawing, "handle-position-x");
        result.PositionY = Read(node, OdfNamespaces.Drawing, "handle-position-y");
        result.RangeXMinimum = Read(node, OdfNamespaces.Drawing, "handle-range-x-minimum");
        result.RangeXMaximum = Read(node, OdfNamespaces.Drawing, "handle-range-x-maximum");
        result.RangeYMinimum = Read(node, OdfNamespaces.Drawing, "handle-range-y-minimum");
        result.RangeYMaximum = Read(node, OdfNamespaces.Drawing, "handle-range-y-maximum");
        result.PolarPoleX = Read(node, OdfNamespaces.Drawing, "handle-polar-pole-x");
        result.PolarPoleY = Read(node, OdfNamespaces.Drawing, "handle-polar-pole-y");
        result.PolarRadius = Read(node, OdfNamespaces.Drawing, "handle-polar-radius");
        result.PolarAngle = Read(node, OdfNamespaces.Drawing, "handle-polar-angle");
        result.RadiusRangeMinimum = Read(node, OdfNamespaces.Drawing, "handle-radius-range-minimum");
        result.RadiusRangeMaximum = Read(node, OdfNamespaces.Drawing, "handle-radius-range-maximum");
        result.LegacyPosition = Read(node, OdfNamespaces.Drawing, "handle-position");
        result.LegacyPolar = Read(node, OdfNamespaces.Drawing, "handle-polar");
        result.MirrorVerticalValue = Read(node, OdfNamespaces.Drawing, "handle-mirror-vertical");
        result.MirrorHorizontalValue = Read(node, OdfNamespaces.Drawing, "handle-mirror-horizontal");
        result.SwitchedValue = Read(node, OdfNamespaces.Drawing, "handle-switched");
        RefreshHandleBooleans(result);
        result.Kind = HandleKind(result);
        result.Node = node;
        CaptureHandleOriginalValues(result);
        return result;
    }

    static OdfGluePoint[] CreateGluePoints(OdfNode node)
    {
        List<OdfGluePoint> result = new List<OdfGluePoint>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, OdfNamespaces.Drawing, "glue-point"))
            {
                OdfGluePoint point = new OdfGluePoint();
                point.IdValue = Read(child, OdfNamespaces.Drawing, "id");
                RefreshGluePointId(point);
                point.X = Read(child, OdfNamespaces.Svg, "x");
                point.Y = Read(child, OdfNamespaces.Svg, "y");
                point.Align = Read(child, OdfNamespaces.Drawing, "align");
                point.EscapeDirection = Read(
                    child,
                    OdfNamespaces.Drawing,
                    "escape-direction");
                point.Node = child;
                CaptureGluePointOriginalValues(point);
                result.Add(point);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfConnectorRouting CreateConnectorRouting(OdfNode node)
    {
        OdfConnectorRouting result = new OdfConnectorRouting();
        result.Type = Read(node, OdfNamespaces.Drawing, "type");
        result.StartShapeId = Read(
            node,
            OdfNamespaces.Drawing,
            "start-shape");
        result.EndShapeId = Read(
            node,
            OdfNamespaces.Drawing,
            "end-shape");
        result.StartGluePoint = Read(
            node,
            OdfNamespaces.Drawing,
            "start-glue-point");
        result.EndGluePoint = Read(
            node,
            OdfNamespaces.Drawing,
            "end-glue-point");
        result.LineSkew = Read(
            node,
            OdfNamespaces.Drawing,
            "line-skew");
        result.PathData = Read(node, OdfNamespaces.Svg, "d");
        CaptureRoutingOriginalValues(result);
        return result;
    }

    static OdfTransform CreateTransform(OdfNode node)
    {
        OdfTransform result = SvgGeometryModel.ParseTransform(Read(
            node,
            OdfNamespaces.Drawing,
            "transform"));
        result.Node = node;
        return result;
    }

    static OdfContour CreateContour(OdfNode node)
    {
        OdfContour result = new OdfContour();
        OdfNode? contour = FindDirectChild(
            node,
            OdfNamespaces.Drawing,
            "contour-polygon");
        if (contour != null)
        {
            result.Kind = OdfContourKind.Polygon;
        }
        else
        {
            contour = FindDirectChild(
                node,
                OdfNamespaces.Drawing,
                "contour-path");
            if (contour != null)
            {
                result.Kind = OdfContourKind.Path;
            }
        }
        if (contour == null)
        {
            return result;
        }
        result.Width = Read(contour, OdfNamespaces.Svg, "width");
        result.Height = Read(contour, OdfNamespaces.Svg, "height");
        result.ViewBox = SvgGeometryModel.ParseViewBox(Read(
            contour,
            OdfNamespaces.Svg,
            "viewBox"));
        result.PointList = SvgGeometryModel.ParsePoints(Read(
            contour,
            OdfNamespaces.Drawing,
            "points"));
        result.Path = SvgGeometryModel.ParsePath(Read(
            contour,
            OdfNamespaces.Svg,
            "d"));
        result.RecreateOnEditValue = Read(
            contour,
            OdfNamespaces.Drawing,
            "recreate-on-edit");
        RefreshContourBoolean(result);
        result.Node = contour;
        CaptureContourOriginalValues(result);
        return result;
    }

    static OdfDrawingAccessibility CreateAccessibility(OdfNode node)
    {
        OdfDrawingAccessibility result = new OdfDrawingAccessibility();
        result.TitleNode = FindDirectChild(
            node,
            OdfNamespaces.Svg,
            "title");
        result.DescriptionNode = FindDirectChild(
            node,
            OdfNamespaces.Svg,
            "desc");
        result.Title = TextContent(result.TitleNode);
        result.Description = TextContent(result.DescriptionNode);
        result.OriginalTitle = result.Title;
        result.OriginalDescription = result.Description;
        return result;
    }

    static OdfDrawingEvent[] CreateEvents(OdfNode node)
    {
        OdfNode? listeners = FindDirectChild(
            node,
            OdfNamespaces.Office,
            "event-listeners");
        if (listeners == null)
        {
            return [];
        }
        List<OdfDrawingEvent> result = new List<OdfDrawingEvent>();
        int index = 0;
        while (index < listeners.Children.Length)
        {
            OdfNode child = listeners.Children[index];
            OdfDrawingEventKind kind = EventKind(child);
            if (kind != OdfDrawingEventKind.Unknown)
            {
                result.Add(CreateEvent(child, kind));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfDrawingEvent CreateEvent(
        OdfNode node,
        OdfDrawingEventKind kind)
    {
        OdfDrawingEvent result = new OdfDrawingEvent();
        result.Kind = kind;
        result.EventName = Read(
            node,
            OdfNamespaces.Script,
            "event-name");
        result.Language = Read(node, OdfNamespaces.Script, "language");
        result.MacroName = Read(
            node,
            OdfNamespaces.Script,
            "macro-name");
        result.Href = Read(node, OdfNamespaces.XLink, "href");
        result.Action = Read(
            node,
            OdfNamespaces.Presentation,
            "action");
        result.Effect = Read(
            node,
            OdfNamespaces.Presentation,
            "effect");
        result.Direction = Read(
            node,
            OdfNamespaces.Presentation,
            "direction");
        result.Speed = Read(
            node,
            OdfNamespaces.Presentation,
            "speed");
        result.StartScale = Read(
            node,
            OdfNamespaces.Presentation,
            "start-scale");
        result.Verb = Read(
            node,
            OdfNamespaces.Presentation,
            "verb");
        result.Show = Read(node, OdfNamespaces.XLink, "show");
        result.Actuate = Read(node, OdfNamespaces.XLink, "actuate");
        OdfNode? sound = FindDirectChild(
            node,
            OdfNamespaces.Presentation,
            "sound");
        if (sound != null)
        {
            result.SoundHref = Read(sound, OdfNamespaces.XLink, "href");
            result.SoundType = Read(sound, OdfNamespaces.XLink, "type");
            result.SoundShow = Read(sound, OdfNamespaces.XLink, "show");
            result.SoundActuate = Read(
                sound,
                OdfNamespaces.XLink,
                "actuate");
            result.SoundPlayFullValue = Read(
                sound,
                OdfNamespaces.Presentation,
                "play-full");
            OdfLexicalValue playFull = OdfLexicalParser.ParseBoolean(
                result.SoundPlayFullValue);
            result.SoundPlayFull = playFull.IsValid
                && playFull.BooleanValue;
            result.IsSoundPlayFullValid =
                result.SoundPlayFullValue.Length == 0
                || playFull.IsValid;
            result.SoundNode = sound;
        }
        else
        {
            result.IsSoundPlayFullValid = true;
        }
        result.OriginalSoundPlayFullValue = result.SoundPlayFullValue;
        result.Node = node;
        return result;
    }

    static void ResolveEventSounds(
        OdfDocument document,
        OdfDrawingObject[] drawings)
    {
        int drawingIndex = 0;
        while (drawingIndex < drawings.Length)
        {
            int eventIndex = 0;
            while (eventIndex < drawings[drawingIndex].Events.Length)
            {
                ResolveEventSound(
                    document,
                    drawings[drawingIndex].Events[eventIndex]);
                eventIndex = eventIndex + 1;
            }
            drawingIndex = drawingIndex + 1;
        }
    }

    static void ResolveEventSound(
        OdfDocument document,
        OdfDrawingEvent item)
    {
        item.SoundPackagePath = "";
        item.SoundPackageEntryExists = false;
        item.IsSoundReferenceValid = item.SoundHref.Length == 0
            || OdfIriResolver.IsIriReference(item.SoundHref);
        if (!item.IsSoundReferenceValid
            || item.SoundHref.Length == 0
            || document.PackageKind != OdfPackageKind.Package
            || !OdfIriResolver.IsPackageRelativeReference(
                item.SoundHref))
        {
            return;
        }
        string partPath = item.SoundNode == null
            ? ""
            : item.SoundNode.Location.PartPath;
        string resolved = PackagePathRules.ResolveRelativeReference(
            partPath,
            item.SoundHref);
        item.SoundPackagePath = ResourcePathWithoutSuffix(resolved);
        int index = 0;
        while (index < document.PackageEntries.Length)
        {
            if (document.PackageEntries[index].Path
                == item.SoundPackagePath)
            {
                item.SoundPackageEntryExists = true;
                return;
            }
            index = index + 1;
        }
    }

    static string ResourcePathWithoutSuffix(string value)
    {
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        int end = value.Length;
        if (query >= 0 && query < end)
        {
            end = query;
        }
        if (fragment >= 0 && fragment < end)
        {
            end = fragment;
        }
        return value.Substring(0, end);
    }

    static OdfDrawingReference[] CreateReferences(
        OdfDrawingObject drawing,
        OdfNode node)
    {
        List<OdfDrawingReference> result =
            new List<OdfDrawingReference>();
        AddReference(
            result,
            OdfDrawingReferenceKind.CopyOf,
            Read(node, OdfNamespaces.Drawing, "copy-of"),
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.Caption,
            Read(node, OdfNamespaces.Drawing, "caption-id"),
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.ConnectorStart,
            Read(node, OdfNamespaces.Drawing, "start-shape"),
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.ConnectorEnd,
            Read(node, OdfNamespaces.Drawing, "end-shape"),
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.Hyperlink,
            Read(node, OdfNamespaces.XLink, "href"),
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.Layer,
            drawing.LayerName,
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.Style,
            drawing.StyleName,
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.TextStyle,
            drawing.TextStyleName,
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.MasterPage,
            drawing.MasterPageName,
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.PresentationPageLayout,
            drawing.PresentationPageLayoutName,
            node);
        AddReference(
            result,
            OdfDrawingReferenceKind.Control,
            drawing.ControlId,
            node);
        AddNavigationReferences(result, drawing.NavigationOrder, node);
        return result.ToArray();
    }

    static void AddReference(
        List<OdfDrawingReference> references,
        OdfDrawingReferenceKind kind,
        string target,
        OdfNode node)
    {
        if (target.Length == 0)
        {
            return;
        }
        OdfDrawingReference reference = new OdfDrawingReference();
        reference.Kind = kind;
        reference.Target = target;
        reference.OriginalTarget = target;
        reference.Node = node;
        if (kind == OdfDrawingReferenceKind.Hyperlink)
        {
            reference.IsExternal = target.Length == 0 || target[0] != '#';
        }
        references.Add(reference);
    }

    static void AddNavigationReferences(
        List<OdfDrawingReference> references,
        string value,
        OdfNode node)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length
                && global::System.Char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }
            int start = index;
            while (index < value.Length
                && !global::System.Char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }
            if (index > start)
            {
                AddReference(
                    references,
                    OdfDrawingReferenceKind.NavigationOrder,
                    value.Substring(start, index - start),
                    node);
            }
        }
    }

    static void ResolveDrawingReferences(OdfDrawingObject[] objects)
    {
        int index = 0;
        while (index < objects.Length)
        {
            OdfDrawingObject drawing = objects[index];
            drawing.Layer = null;
            int referenceIndex = 0;
            while (referenceIndex < drawing.References.Length)
            {
                OdfDrawingReference reference =
                    drawing.References[referenceIndex];
                reference.TargetObject = null;
                reference.IsBroken = false;
                reference.IsAmbiguous = false;
                if (IsDrawingTargetReference(reference))
                {
                    ResolveObjectReference(reference, objects);
                }
                else if (reference.Kind == OdfDrawingReferenceKind.Layer)
                {
                    ResolveLayerReference(reference, drawing, objects);
                    drawing.Layer = reference.TargetObject;
                }
                referenceIndex = referenceIndex + 1;
            }
            ResolveConnectorRouting(drawing);
            index = index + 1;
        }
    }

    static void ResolveConnectorRouting(OdfDrawingObject drawing)
    {
        OdfConnectorRouting routing = drawing.Geometry.Routing;
        routing.StartShape = null;
        routing.EndShape = null;
        routing.HasBrokenStartShape = false;
        routing.HasBrokenEndShape = false;
        int index = 0;
        while (index < drawing.References.Length)
        {
            OdfDrawingReference reference = drawing.References[index];
            if (reference.Kind == OdfDrawingReferenceKind.ConnectorStart)
            {
                routing.StartShapeId = reference.Target;
                routing.StartShape = reference.TargetObject;
                routing.HasBrokenStartShape = reference.IsBroken
                    || reference.IsAmbiguous;
            }
            else if (reference.Kind
                == OdfDrawingReferenceKind.ConnectorEnd)
            {
                routing.EndShapeId = reference.Target;
                routing.EndShape = reference.TargetObject;
                routing.HasBrokenEndShape = reference.IsBroken
                    || reference.IsAmbiguous;
            }
            index = index + 1;
        }
    }

    static void ResolveShapeContent(
        OdfDocument document,
        OdfDrawingObject[] objects)
    {
        int index = 0;
        while (index < objects.Length)
        {
            OdfDrawingObject drawing = objects[index];
            drawing.TextBlocks = DirectTextBlocks(
                document.TextBlocks,
                drawing.Node);
            drawing.TextBox = FindTextBox(document.TextFrames, drawing);
            index = index + 1;
        }
    }

    static OdfTextBlock[] DirectTextBlocks(
        OdfTextBlock[] blocks,
        OdfNode? drawingNode)
    {
        if (drawingNode == null)
        {
            return [];
        }
        List<OdfTextBlock> result = new List<OdfTextBlock>();
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextBlock block = blocks[index];
            if (block.Node != null
                && NearestDrawingAncestor(block.Node) == drawingNode)
            {
                result.Add(block);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfNode? NearestDrawingAncestor(OdfNode node)
    {
        OdfNode? current = node.Parent;
        while (current != null)
        {
            if (DrawingKind(current) != OdfDrawingKind.Unknown)
            {
                return current;
            }
            current = current.Parent;
        }
        return null;
    }

    static OdfTextBox? FindTextBox(
        OdfTextFrame[] frames,
        OdfDrawingObject drawing)
    {
        int index = 0;
        while (index < frames.Length)
        {
            OdfTextFrame frame = frames[index];
            if (drawing.Kind == OdfDrawingKind.Frame
                && frame.Node == drawing.Node)
            {
                return frame.TextBox;
            }
            if (drawing.Kind == OdfDrawingKind.TextBox
                && frame.TextBox != null
                && frame.TextBox.Node == drawing.Node)
            {
                return frame.TextBox;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsDrawingTargetReference(OdfDrawingReference reference)
    {
        return reference.Kind == OdfDrawingReferenceKind.CopyOf
            || reference.Kind == OdfDrawingReferenceKind.Caption
            || reference.Kind == OdfDrawingReferenceKind.ConnectorStart
            || reference.Kind == OdfDrawingReferenceKind.ConnectorEnd
            || reference.Kind == OdfDrawingReferenceKind.NavigationOrder
            || reference.Kind == OdfDrawingReferenceKind.Hyperlink
                && !reference.IsExternal;
    }

    static void ResolveObjectReference(
        OdfDrawingReference reference,
        OdfDrawingObject[] objects)
    {
        string target = reference.Target;
        if (reference.Kind == OdfDrawingReferenceKind.Hyperlink
            && target.Length > 0
            && target[0] == '#')
        {
            target = target.Substring(1);
        }
        int count = 0;
        int index = 0;
        while (index < objects.Length)
        {
            OdfDrawingObject candidate = objects[index];
            if (candidate.Id == target || candidate.XmlId == target)
            {
                reference.TargetObject = candidate;
                count = count + 1;
            }
            index = index + 1;
        }
        reference.IsBroken = count == 0;
        reference.IsAmbiguous = count > 1;
        if (count != 1)
        {
            reference.TargetObject = null;
        }
    }

    static void ResolveLayerReference(
        OdfDrawingReference reference,
        OdfDrawingObject owner,
        OdfDrawingObject[] objects)
    {
        int count = 0;
        int index = 0;
        if (owner.Page != null)
        {
            while (index < objects.Length)
            {
                OdfDrawingObject candidate = objects[index];
                if (candidate.Kind == OdfDrawingKind.Layer
                    && candidate.Name == reference.Target
                    && candidate.Page == owner.Page)
                {
                    reference.TargetObject = candidate;
                    count = count + 1;
                }
                index = index + 1;
            }
        }
        if (count == 0)
        {
            index = 0;
            while (index < objects.Length)
            {
                OdfDrawingObject candidate = objects[index];
                if (candidate.Kind == OdfDrawingKind.Layer
                    && candidate.Name == reference.Target
                    && candidate.PartPath == owner.PartPath)
                {
                    reference.TargetObject = candidate;
                    count = count + 1;
                }
                index = index + 1;
            }
        }
        if (count == 0)
        {
            index = 0;
            while (index < objects.Length)
            {
                OdfDrawingObject candidate = objects[index];
                if (candidate.Kind == OdfDrawingKind.Layer
                    && candidate.Name == reference.Target)
                {
                    reference.TargetObject = candidate;
                    count = count + 1;
                }
                index = index + 1;
            }
        }
        reference.IsBroken = count == 0;
        reference.IsAmbiguous = count > 1;
        if (count != 1)
        {
            reference.TargetObject = null;
        }
    }

    static void BuildStackingOrders(OdfDrawingObject[] objects)
    {
        int index = 0;
        while (index < objects.Length)
        {
            OdfDrawingObject drawing = objects[index];
            drawing.ZOrderedChildren = CopyObjects(drawing.Children);
            SortByZOrder(drawing.ZOrderedChildren);
            index = index + 1;
        }
    }

    static void SortByZOrder(OdfDrawingObject[] objects)
    {
        int index = 1;
        while (index < objects.Length)
        {
            OdfDrawingObject value = objects[index];
            int position = index;
            while (position > 0
                && CompareZOrder(value, objects[position - 1]) < 0)
            {
                objects[position] = objects[position - 1];
                position = position - 1;
            }
            objects[position] = value;
            index = index + 1;
        }
    }

    static int CompareZOrder(
        OdfDrawingObject left,
        OdfDrawingObject right)
    {
        if (left.ZOrder.IsSpecified
            && left.ZOrder.IsValid
            && right.ZOrder.IsSpecified
            && right.ZOrder.IsValid
            && left.ZOrder.Index != right.ZOrder.Index)
        {
            return left.ZOrder.Index < right.ZOrder.Index ? -1 : 1;
        }
        if (left.ZOrder.DocumentOrder == right.ZOrder.DocumentOrder)
        {
            return 0;
        }
        return left.ZOrder.DocumentOrder < right.ZOrder.DocumentOrder
            ? -1
            : 1;
    }

    static void ApplyDrawingFields(
        OdfDrawingObject drawing,
        OdfSaveOptions options)
    {
        OdfNode node = drawing.Node ?? new OdfNode();
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "id",
            OdfNamespaces.DrawingPrefix,
            drawing.Id,
            drawing.OriginalId);
        WriteChanged(
            node,
            OdfNamespaces.Xml,
            "id",
            OdfNamespaces.XmlPrefix,
            drawing.XmlId,
            drawing.OriginalXmlId);
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "name",
            OdfNamespaces.DrawingPrefix,
            drawing.Name,
            drawing.OriginalName);
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "display-name",
            OdfNamespaces.DrawingPrefix,
            drawing.DisplayName,
            drawing.OriginalDisplayName);
        ApplyStyleFields(drawing, node);
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "text-style-name",
            OdfNamespaces.DrawingPrefix,
            drawing.TextStyleName,
            drawing.OriginalTextStyleName);
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "layer",
            OdfNamespaces.DrawingPrefix,
            drawing.LayerName,
            drawing.OriginalLayerName);
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "master-page-name",
            OdfNamespaces.DrawingPrefix,
            drawing.MasterPageName,
            drawing.OriginalMasterPageName);
        WriteChanged(
            node,
            OdfNamespaces.Presentation,
            "presentation-page-layout-name",
            OdfNamespaces.PresentationPrefix,
            drawing.PresentationPageLayoutName,
            drawing.OriginalPresentationPageLayoutName);
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "nav-order",
            OdfNamespaces.DrawingPrefix,
            drawing.NavigationOrder,
            drawing.OriginalNavigationOrder);
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "display",
            OdfNamespaces.DrawingPrefix,
            drawing.Display,
            drawing.OriginalDisplay);
        ApplyProtectedField(drawing, node);
        WriteChanged(
            node,
            OdfNamespaces.Svg,
            "x",
            OdfNamespaces.SvgPrefix,
            drawing.X,
            drawing.OriginalX);
        WriteChanged(
            node,
            OdfNamespaces.Svg,
            "y",
            OdfNamespaces.SvgPrefix,
            drawing.Y,
            drawing.OriginalY);
        WriteChanged(
            node,
            OdfNamespaces.Svg,
            "width",
            OdfNamespaces.SvgPrefix,
            drawing.Width,
            drawing.OriginalWidth);
        WriteChanged(
            node,
            OdfNamespaces.Svg,
            "height",
            OdfNamespaces.SvgPrefix,
            drawing.Height,
            drawing.OriginalHeight);
        ApplyGeometry(drawing.Geometry, node, options);
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "control",
            OdfNamespaces.DrawingPrefix,
            drawing.ControlId,
            drawing.OriginalControlId);
        ApplyAnchor(drawing.Anchor, node);
        ApplyZOrder(drawing.ZOrder, node);
        ApplyTransform(drawing.Transform, node, options);
        ApplyContour(drawing.Contour, options);
        ApplyAccessibility(drawing.Accessibility, node);
    }

    static void ApplyStyleFields(
        OdfDrawingObject drawing,
        OdfNode node)
    {
        bool changed = drawing.StyleName != drawing.OriginalStyleName
            || drawing.StyleNamespace != drawing.OriginalStyleNamespace
            || drawing.ClassNames != drawing.OriginalClassNames;
        if (!changed)
        {
            return;
        }
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Drawing,
            "style-name");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Presentation,
            "style-name");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Drawing,
            "class-names");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Presentation,
            "class-names");
        string namespaceName = drawing.StyleNamespace;
        if (namespaceName != OdfNamespaces.Presentation)
        {
            namespaceName = OdfNamespaces.Drawing;
        }
        string prefix = namespaceName == OdfNamespaces.Presentation
            ? OdfNamespaces.PresentationPrefix
            : OdfNamespaces.DrawingPrefix;
        WriteOptional(
            node,
            namespaceName,
            "style-name",
            prefix,
            drawing.StyleName);
        WriteOptional(
            node,
            namespaceName,
            "class-names",
            prefix,
            drawing.ClassNames);
    }

    static void ApplyProtectedField(
        OdfDrawingObject drawing,
        OdfNode node)
    {
        string value = drawing.ProtectedValue;
        if (drawing.IsProtected != drawing.OriginalIsProtected
            && value == drawing.OriginalProtectedValue)
        {
            value = drawing.IsProtected ? "true" : "false";
            drawing.ProtectedValue = value;
        }
        WriteChanged(
            node,
            OdfNamespaces.Drawing,
            "protected",
            OdfNamespaces.DrawingPrefix,
            value,
            drawing.OriginalProtectedValue);
    }

    static void ApplyGeometry(
        OdfGeometry geometry,
        OdfNode node,
        OdfSaveOptions options)
    {
        if (geometry == null)
        {
            return;
        }
        ApplyConnectorRouting(geometry, node);
        ApplyEnhancedGeometry(geometry.EnhancedGeometry, options);
        ApplyGluePoints(geometry.GluePoints);
        ApplyStructuredGeometry(geometry, options);
        string concaveValue = geometry.ConcaveValue;
        if (geometry.IsConcave != geometry.OriginalIsConcave
            && concaveValue == geometry.OriginalConcaveValue)
        {
            concaveValue = geometry.IsConcave ? "true" : "false";
            geometry.ConcaveValue = concaveValue;
        }
        WriteChanged(node, OdfNamespaces.Svg, "x1", OdfNamespaces.SvgPrefix, geometry.X1, geometry.OriginalX1);
        WriteChanged(node, OdfNamespaces.Svg, "y1", OdfNamespaces.SvgPrefix, geometry.Y1, geometry.OriginalY1);
        WriteChanged(node, OdfNamespaces.Svg, "x2", OdfNamespaces.SvgPrefix, geometry.X2, geometry.OriginalX2);
        WriteChanged(node, OdfNamespaces.Svg, "y2", OdfNamespaces.SvgPrefix, geometry.Y2, geometry.OriginalY2);
        WriteChanged(node, OdfNamespaces.Svg, "cx", OdfNamespaces.SvgPrefix, geometry.CenterX, geometry.OriginalCenterX);
        WriteChanged(node, OdfNamespaces.Svg, "cy", OdfNamespaces.SvgPrefix, geometry.CenterY, geometry.OriginalCenterY);
        WriteChanged(node, OdfNamespaces.Svg, "r", OdfNamespaces.SvgPrefix, geometry.Radius, geometry.OriginalRadius);
        WriteChanged(node, OdfNamespaces.Svg, "rx", OdfNamespaces.SvgPrefix, geometry.RadiusX, geometry.OriginalRadiusX);
        WriteChanged(node, OdfNamespaces.Svg, "ry", OdfNamespaces.SvgPrefix, geometry.RadiusY, geometry.OriginalRadiusY);
        WriteChanged(node, OdfNamespaces.Drawing, "corner-radius", OdfNamespaces.DrawingPrefix, geometry.CornerRadius, geometry.OriginalCornerRadius);
        WriteChanged(node, OdfNamespaces.Drawing, "kind", OdfNamespaces.DrawingPrefix, geometry.EllipseKind, geometry.OriginalEllipseKind);
        WriteChanged(node, OdfNamespaces.Drawing, "start-angle", OdfNamespaces.DrawingPrefix, geometry.StartAngle, geometry.OriginalStartAngle);
        WriteChanged(node, OdfNamespaces.Drawing, "end-angle", OdfNamespaces.DrawingPrefix, geometry.EndAngle, geometry.OriginalEndAngle);
        WriteChanged(node, OdfNamespaces.Drawing, "points", OdfNamespaces.DrawingPrefix, geometry.Points, geometry.OriginalPoints);
        WriteChanged(node, OdfNamespaces.Svg, "d", OdfNamespaces.SvgPrefix, geometry.PathData, geometry.OriginalPathData);
        WriteChanged(node, OdfNamespaces.Svg, "viewBox", OdfNamespaces.SvgPrefix, geometry.ViewBox, geometry.OriginalViewBox);
        WriteChanged(node, OdfNamespaces.Drawing, "caption-point-x", OdfNamespaces.DrawingPrefix, geometry.CaptionPointX, geometry.OriginalCaptionPointX);
        WriteChanged(node, OdfNamespaces.Drawing, "caption-point-y", OdfNamespaces.DrawingPrefix, geometry.CaptionPointY, geometry.OriginalCaptionPointY);
        WriteChanged(node, OdfNamespaces.Drawing, "type", OdfNamespaces.DrawingPrefix, geometry.ConnectorType, geometry.OriginalConnectorType);
        WriteChanged(node, OdfNamespaces.Drawing, "start-glue-point", OdfNamespaces.DrawingPrefix, geometry.StartGluePoint, geometry.OriginalStartGluePoint);
        WriteChanged(node, OdfNamespaces.Drawing, "end-glue-point", OdfNamespaces.DrawingPrefix, geometry.EndGluePoint, geometry.OriginalEndGluePoint);
        WriteChanged(node, OdfNamespaces.Drawing, "line-skew", OdfNamespaces.DrawingPrefix, geometry.LineSkew, geometry.OriginalLineSkew);
        WriteChanged(node, OdfNamespaces.Drawing, "corners", OdfNamespaces.DrawingPrefix, geometry.Corners, geometry.OriginalCorners);
        WriteChanged(node, OdfNamespaces.Drawing, "concave", OdfNamespaces.DrawingPrefix, concaveValue, geometry.OriginalConcaveValue);
        WriteChanged(node, OdfNamespaces.Drawing, "sharpness", OdfNamespaces.DrawingPrefix, geometry.Sharpness, geometry.OriginalSharpness);
        RefreshGeometryValues(geometry);
        geometry.PointList = SvgGeometryModel.ParsePoints(geometry.Points);
        geometry.Path = SvgGeometryModel.ParsePath(geometry.PathData);
        geometry.ViewBoxValue = SvgGeometryModel.ParseViewBox(
            geometry.ViewBox);
        geometry.Routing = CreateConnectorRouting(node);
        geometry.Node = node;
        CaptureGeometryOriginalValues(geometry);
    }

    static void ApplyConnectorRouting(
        OdfGeometry geometry,
        OdfNode node)
    {
        OdfConnectorRouting routing = geometry.Routing;
        if (routing == null)
        {
            return;
        }
        if (routing.Type != routing.OriginalType)
        {
            geometry.ConnectorType = routing.Type;
        }
        if (routing.StartGluePoint != routing.OriginalStartGluePoint)
        {
            geometry.StartGluePoint = routing.StartGluePoint;
        }
        if (routing.EndGluePoint != routing.OriginalEndGluePoint)
        {
            geometry.EndGluePoint = routing.EndGluePoint;
        }
        if (routing.LineSkew != routing.OriginalLineSkew)
        {
            geometry.LineSkew = routing.LineSkew;
        }
        if (routing.PathData != routing.OriginalPathData)
        {
            geometry.PathData = routing.PathData;
        }
        WriteChanged(node, OdfNamespaces.Drawing, "start-shape", OdfNamespaces.DrawingPrefix, routing.StartShapeId, routing.OriginalStartShapeId);
        WriteChanged(node, OdfNamespaces.Drawing, "end-shape", OdfNamespaces.DrawingPrefix, routing.EndShapeId, routing.OriginalEndShapeId);
    }

    static void ApplyStructuredGeometry(
        OdfGeometry geometry,
        OdfSaveOptions options)
    {
        string points = geometry.Points;
        if (points == geometry.OriginalPoints
            && geometry.PointList.LexicalValue
                != geometry.PointList.OriginalLexicalValue)
        {
            points = geometry.PointList.LexicalValue;
        }
        string path = geometry.PathData;
        if (path == geometry.OriginalPathData
            && geometry.Path.LexicalValue
                != geometry.Path.OriginalLexicalValue)
        {
            path = geometry.Path.LexicalValue;
        }
        string viewBox = geometry.ViewBox;
        if (viewBox == geometry.OriginalViewBox
            && geometry.ViewBoxValue.LexicalValue
                != geometry.ViewBoxValue.OriginalLexicalValue)
        {
            viewBox = geometry.ViewBoxValue.LexicalValue;
        }
        geometry.Points = StructuredPointText(
            points,
            geometry.OriginalPoints,
            geometry.PointList,
            options.PreserveLexicalValues);
        geometry.PathData = StructuredPathText(
            path,
            geometry.OriginalPathData,
            geometry.Path,
            options.PreserveLexicalValues);
        geometry.ViewBox = StructuredViewBoxText(
            viewBox,
            geometry.OriginalViewBox,
            geometry.ViewBoxValue,
            options.PreserveLexicalValues);
    }

    static string StructuredPointText(
        string lexical,
        string original,
        OdfSvgPointList value,
        bool preserve)
    {
        if (lexical != original)
        {
            OdfSvgPointList parsed = SvgGeometryModel.ParsePoints(lexical);
            return !preserve && parsed.IsValid
                ? SvgGeometryModel.FormatPoints(parsed)
                : lexical;
        }
        if (value != null && value.IsValid)
        {
            string canonical = SvgGeometryModel.FormatPoints(value);
            if (!preserve
                || canonical != value.OriginalCanonicalValue)
            {
                return canonical;
            }
        }
        return lexical;
    }

    static string StructuredPathText(
        string lexical,
        string original,
        OdfSvgPathData value,
        bool preserve)
    {
        if (lexical != original)
        {
            OdfSvgPathData parsed = SvgGeometryModel.ParsePath(lexical);
            return !preserve && parsed.IsValid
                ? SvgGeometryModel.FormatPath(parsed)
                : lexical;
        }
        if (value != null && value.IsValid)
        {
            string canonical = SvgGeometryModel.FormatPath(value);
            if (!preserve
                || canonical != value.OriginalCanonicalValue)
            {
                return canonical;
            }
        }
        return lexical;
    }

    static string StructuredViewBoxText(
        string lexical,
        string original,
        OdfSvgViewBox value,
        bool preserve)
    {
        if (lexical != original)
        {
            OdfSvgViewBox parsed = SvgGeometryModel.ParseViewBox(lexical);
            return !preserve && parsed.IsValid
                ? SvgGeometryModel.FormatViewBox(parsed)
                : lexical;
        }
        if (value != null && value.IsValid)
        {
            string canonical = SvgGeometryModel.FormatViewBox(value);
            if (!preserve
                || canonical != value.OriginalCanonicalValue)
            {
                return canonical;
            }
        }
        return lexical;
    }

    static void ApplyEnhancedGeometry(
        OdfEnhancedGeometry? enhanced,
        OdfSaveOptions options)
    {
        if (enhanced == null || enhanced.Node == null)
        {
            return;
        }
        OdfNode node = enhanced.Node;
        string enhancedViewBox = enhanced.ViewBox;
        if (enhancedViewBox == enhanced.OriginalViewBox
            && enhanced.ViewBoxValue.LexicalValue
                != enhanced.ViewBoxValue.OriginalLexicalValue)
        {
            enhancedViewBox = enhanced.ViewBoxValue.LexicalValue;
        }
        enhanced.ViewBox = StructuredViewBoxText(
            enhancedViewBox,
            enhanced.OriginalViewBox,
            enhanced.ViewBoxValue,
            options.PreserveLexicalValues);
        string modifiers = enhanced.ModifierText;
        if (ModifiersChanged(enhanced.Modifiers)
            && modifiers == enhanced.OriginalModifierText)
        {
            modifiers = ModifierText(enhanced.Modifiers);
            enhanced.ModifierText = modifiers;
        }
        string vertical = enhanced.MirrorVerticalValue;
        if (enhanced.MirrorVertical != enhanced.OriginalMirrorVertical
            && vertical == enhanced.OriginalMirrorVerticalValue)
        {
            vertical = enhanced.MirrorVertical ? "true" : "false";
            enhanced.MirrorVerticalValue = vertical;
        }
        string horizontal = enhanced.MirrorHorizontalValue;
        if (enhanced.MirrorHorizontal != enhanced.OriginalMirrorHorizontal
            && horizontal == enhanced.OriginalMirrorHorizontalValue)
        {
            horizontal = enhanced.MirrorHorizontal ? "true" : "false";
            enhanced.MirrorHorizontalValue = horizontal;
        }
        WriteChanged(node, OdfNamespaces.Drawing, "type", OdfNamespaces.DrawingPrefix, enhanced.Preset.Name, enhanced.Preset.OriginalName);
        WriteChanged(node, OdfNamespaces.Svg, "viewBox", OdfNamespaces.SvgPrefix, enhanced.ViewBox, enhanced.OriginalViewBox);
        WriteChanged(node, OdfNamespaces.Drawing, "modifiers", OdfNamespaces.DrawingPrefix, modifiers, enhanced.OriginalModifierText);
        WriteChanged(node, OdfNamespaces.Drawing, "enhanced-path", OdfNamespaces.DrawingPrefix, enhanced.EnhancedPath, enhanced.OriginalEnhancedPath);
        WriteChanged(node, OdfNamespaces.Drawing, "path-stretchpoint-x", OdfNamespaces.DrawingPrefix, enhanced.StretchPointX, enhanced.OriginalStretchPointX);
        WriteChanged(node, OdfNamespaces.Drawing, "path-stretchpoint-y", OdfNamespaces.DrawingPrefix, enhanced.StretchPointY, enhanced.OriginalStretchPointY);
        WriteChanged(node, OdfNamespaces.Drawing, "text-areas", OdfNamespaces.DrawingPrefix, enhanced.TextAreas, enhanced.OriginalTextAreas);
        WriteChanged(node, OdfNamespaces.Drawing, "glue-points", OdfNamespaces.DrawingPrefix, enhanced.GluePoints, enhanced.OriginalGluePoints);
        WriteChanged(node, OdfNamespaces.Drawing, "glue-point-type", OdfNamespaces.DrawingPrefix, enhanced.GluePointType, enhanced.OriginalGluePointType);
        WriteChanged(node, OdfNamespaces.Drawing, "glue-point-leaving-directions", OdfNamespaces.DrawingPrefix, enhanced.GluePointLeavingDirections, enhanced.OriginalGluePointLeavingDirections);
        WriteChanged(node, OdfNamespaces.Drawing, "mirror-vertical", OdfNamespaces.DrawingPrefix, vertical, enhanced.OriginalMirrorVerticalValue);
        WriteChanged(node, OdfNamespaces.Drawing, "mirror-horizontal", OdfNamespaces.DrawingPrefix, horizontal, enhanced.OriginalMirrorHorizontalValue);
        WriteChanged(node, OdfNamespaces.Drawing, "text-rotate-angle", OdfNamespaces.DrawingPrefix, enhanced.TextRotateAngle, enhanced.OriginalTextRotateAngle);
        ApplyEquations(enhanced.Equations);
        ApplyHandles(enhanced.Handles);
        enhanced.ViewBoxValue = SvgGeometryModel.ParseViewBox(
            enhanced.ViewBox);
        RefreshEnhancedBooleans(enhanced);
        RefreshModifierOriginalValues(enhanced.Modifiers);
        CaptureEnhancedOriginalValues(enhanced);
    }

    static void ApplyEquations(OdfGeometryEquation[] equations)
    {
        int index = 0;
        while (index < equations.Length)
        {
            OdfGeometryEquation equation = equations[index];
            if (equation != null && equation.Node != null)
            {
                WriteChanged(equation.Node, OdfNamespaces.Drawing, "name", OdfNamespaces.DrawingPrefix, equation.Name, equation.OriginalName);
                WriteChanged(equation.Node, OdfNamespaces.Drawing, "formula", OdfNamespaces.DrawingPrefix, equation.Formula, equation.OriginalFormula);
                equation.OriginalName = equation.Name;
                equation.OriginalFormula = equation.Formula;
            }
            index = index + 1;
        }
    }

    static void ApplyHandles(OdfGeometryHandle[] handles)
    {
        int index = 0;
        while (index < handles.Length)
        {
            OdfGeometryHandle handle = handles[index];
            if (handle != null && handle.Node != null)
            {
                ApplyHandle(handle);
                CaptureHandleOriginalValues(handle);
            }
            index = index + 1;
        }
    }

    static void ApplyHandle(OdfGeometryHandle handle)
    {
        OdfNode node = handle.Node ?? new OdfNode();
        string vertical = handle.MirrorVerticalValue;
        if (handle.MirrorVertical != handle.OriginalMirrorVertical
            && vertical == handle.OriginalMirrorVerticalValue)
        {
            vertical = handle.MirrorVertical ? "true" : "false";
            handle.MirrorVerticalValue = vertical;
        }
        string horizontal = handle.MirrorHorizontalValue;
        if (handle.MirrorHorizontal != handle.OriginalMirrorHorizontal
            && horizontal == handle.OriginalMirrorHorizontalValue)
        {
            horizontal = handle.MirrorHorizontal ? "true" : "false";
            handle.MirrorHorizontalValue = horizontal;
        }
        string switched = handle.SwitchedValue;
        if (handle.IsSwitched != handle.OriginalIsSwitched
            && switched == handle.OriginalSwitchedValue)
        {
            switched = handle.IsSwitched ? "true" : "false";
            handle.SwitchedValue = switched;
        }
        WriteChanged(node, OdfNamespaces.Drawing, "handle-position-x", OdfNamespaces.DrawingPrefix, handle.PositionX, handle.OriginalPositionX);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-position-y", OdfNamespaces.DrawingPrefix, handle.PositionY, handle.OriginalPositionY);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-range-x-minimum", OdfNamespaces.DrawingPrefix, handle.RangeXMinimum, handle.OriginalRangeXMinimum);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-range-x-maximum", OdfNamespaces.DrawingPrefix, handle.RangeXMaximum, handle.OriginalRangeXMaximum);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-range-y-minimum", OdfNamespaces.DrawingPrefix, handle.RangeYMinimum, handle.OriginalRangeYMinimum);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-range-y-maximum", OdfNamespaces.DrawingPrefix, handle.RangeYMaximum, handle.OriginalRangeYMaximum);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-polar-pole-x", OdfNamespaces.DrawingPrefix, handle.PolarPoleX, handle.OriginalPolarPoleX);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-polar-pole-y", OdfNamespaces.DrawingPrefix, handle.PolarPoleY, handle.OriginalPolarPoleY);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-polar-radius", OdfNamespaces.DrawingPrefix, handle.PolarRadius, handle.OriginalPolarRadius);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-polar-angle", OdfNamespaces.DrawingPrefix, handle.PolarAngle, handle.OriginalPolarAngle);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-radius-range-minimum", OdfNamespaces.DrawingPrefix, handle.RadiusRangeMinimum, handle.OriginalRadiusRangeMinimum);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-radius-range-maximum", OdfNamespaces.DrawingPrefix, handle.RadiusRangeMaximum, handle.OriginalRadiusRangeMaximum);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-position", OdfNamespaces.DrawingPrefix, handle.LegacyPosition, handle.OriginalLegacyPosition);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-polar", OdfNamespaces.DrawingPrefix, handle.LegacyPolar, handle.OriginalLegacyPolar);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-mirror-vertical", OdfNamespaces.DrawingPrefix, vertical, handle.OriginalMirrorVerticalValue);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-mirror-horizontal", OdfNamespaces.DrawingPrefix, horizontal, handle.OriginalMirrorHorizontalValue);
        WriteChanged(node, OdfNamespaces.Drawing, "handle-switched", OdfNamespaces.DrawingPrefix, switched, handle.OriginalSwitchedValue);
        RefreshHandleBooleans(handle);
        handle.Kind = HandleKind(handle);
    }

    static void ApplyGluePoints(OdfGluePoint[] points)
    {
        int index = 0;
        while (index < points.Length)
        {
            OdfGluePoint point = points[index];
            if (point != null && point.Node != null)
            {
                string id = point.IdValue;
                if (point.Id != point.OriginalId
                    && id == point.OriginalIdValue)
                {
                    id = point.Id >= 0
                        ? point.Id.ToString(global::System.Globalization.CultureInfo.InvariantCulture)
                        : "";
                    point.IdValue = id;
                }
                WriteChanged(point.Node, OdfNamespaces.Drawing, "id", OdfNamespaces.DrawingPrefix, id, point.OriginalIdValue);
                WriteChanged(point.Node, OdfNamespaces.Svg, "x", OdfNamespaces.SvgPrefix, point.X, point.OriginalX);
                WriteChanged(point.Node, OdfNamespaces.Svg, "y", OdfNamespaces.SvgPrefix, point.Y, point.OriginalY);
                WriteChanged(point.Node, OdfNamespaces.Drawing, "align", OdfNamespaces.DrawingPrefix, point.Align, point.OriginalAlign);
                WriteChanged(point.Node, OdfNamespaces.Drawing, "escape-direction", OdfNamespaces.DrawingPrefix, point.EscapeDirection, point.OriginalEscapeDirection);
                RefreshGluePointId(point);
                CaptureGluePointOriginalValues(point);
            }
            index = index + 1;
        }
    }

    static void ApplyAnchor(OdfDrawingAnchor anchor, OdfNode node)
    {
        if (anchor == null)
        {
            return;
        }
        string type = anchor.Type;
        if (anchor.Kind != anchor.OriginalKind
            && type == anchor.OriginalType)
        {
            type = AnchorKindText(anchor.Kind);
            anchor.Type = type;
        }
        string pageNumber = anchor.PageNumberValue;
        if (anchor.PageNumber != anchor.OriginalPageNumber
            && pageNumber == anchor.OriginalPageNumberValue)
        {
            pageNumber = anchor.PageNumber > 0
                ? anchor.PageNumber.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture)
                : "";
            anchor.PageNumberValue = pageNumber;
        }
        string background = anchor.TableBackgroundValue;
        if (anchor.IsTableBackground != anchor.OriginalIsTableBackground
            && background == anchor.OriginalTableBackgroundValue)
        {
            background = anchor.IsTableBackground ? "true" : "false";
            anchor.TableBackgroundValue = background;
        }
        WriteChanged(node, OdfNamespaces.Text, "anchor-type", OdfNamespaces.TextPrefix, type, anchor.OriginalType);
        WriteChanged(node, OdfNamespaces.Text, "anchor-page-number", OdfNamespaces.TextPrefix, pageNumber, anchor.OriginalPageNumberValue);
        WriteChanged(node, OdfNamespaces.Table, "end-cell-address", OdfNamespaces.TablePrefix, anchor.EndCellAddress, anchor.OriginalEndCellAddress);
        WriteChanged(node, OdfNamespaces.Table, "end-x", OdfNamespaces.TablePrefix, anchor.EndX, anchor.OriginalEndX);
        WriteChanged(node, OdfNamespaces.Table, "end-y", OdfNamespaces.TablePrefix, anchor.EndY, anchor.OriginalEndY);
        WriteChanged(node, OdfNamespaces.Table, "table-background", OdfNamespaces.TablePrefix, background, anchor.OriginalTableBackgroundValue);
        RefreshAnchorValues(anchor);
        CaptureAnchorOriginalValues(anchor);
    }

    static void ApplyZOrder(OdfDrawingZOrder zOrder, OdfNode node)
    {
        if (zOrder == null)
        {
            return;
        }
        string value = zOrder.LexicalValue;
        if (zOrder.Index != zOrder.OriginalIndex
            && value == zOrder.OriginalLexicalValue)
        {
            value = zOrder.Index >= 0
                ? zOrder.Index.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture)
                : "";
            zOrder.LexicalValue = value;
        }
        WriteChanged(node, OdfNamespaces.Drawing, "z-index", OdfNamespaces.DrawingPrefix, value, zOrder.OriginalLexicalValue);
        RefreshZOrder(zOrder);
        zOrder.OriginalLexicalValue = zOrder.LexicalValue;
        zOrder.OriginalIndex = zOrder.Index;
    }

    static void ApplyTransform(
        OdfTransform transform,
        OdfNode node,
        OdfSaveOptions options)
    {
        if (transform == null)
        {
            return;
        }
        string expression = StructuredTransformText(transform, options);
        WriteChanged(node, OdfNamespaces.Drawing, "transform", OdfNamespaces.DrawingPrefix, expression, transform.OriginalExpression);
        OdfTransform refreshed = SvgGeometryModel.ParseTransform(expression);
        transform.Expression = refreshed.Expression;
        transform.IsSpecified = refreshed.IsSpecified;
        transform.IsValid = refreshed.IsValid;
        transform.Operations = refreshed.Operations;
        transform.Node = node;
        transform.OriginalExpression = refreshed.OriginalExpression;
        transform.OriginalCanonicalExpression =
            refreshed.OriginalCanonicalExpression;
    }

    static string StructuredTransformText(
        OdfTransform transform,
        OdfSaveOptions options)
    {
        if (transform.Expression != transform.OriginalExpression)
        {
            OdfTransform parsed = SvgGeometryModel.ParseTransform(
                transform.Expression);
            return !options.PreserveLexicalValues && parsed.IsValid
                ? SvgGeometryModel.FormatTransform(parsed)
                : transform.Expression;
        }
        if (transform.IsValid)
        {
            string canonical = SvgGeometryModel.FormatTransform(transform);
            if (!options.PreserveLexicalValues
                || canonical != transform.OriginalCanonicalExpression)
            {
                return canonical;
            }
        }
        return transform.Expression;
    }

    static void ApplyContour(
        OdfContour contour,
        OdfSaveOptions options)
    {
        if (contour == null || contour.Node == null)
        {
            return;
        }
        OdfNode node = contour.Node;
        string viewBox = StructuredViewBoxText(
            contour.ViewBox.LexicalValue,
            contour.ViewBox.OriginalLexicalValue,
            contour.ViewBox,
            options.PreserveLexicalValues);
        string points = StructuredPointText(
            contour.PointList.LexicalValue,
            contour.PointList.OriginalLexicalValue,
            contour.PointList,
            options.PreserveLexicalValues);
        string path = StructuredPathText(
            contour.Path.LexicalValue,
            contour.Path.OriginalLexicalValue,
            contour.Path,
            options.PreserveLexicalValues);
        string recreate = contour.RecreateOnEditValue;
        if (recreate != contour.OriginalRecreateOnEditValue)
        {
            if (!options.PreserveLexicalValues)
            {
                OdfLexicalValue parsed = OdfLexicalParser.ParseBoolean(
                    recreate);
                if (parsed.IsValid)
                {
                    recreate = parsed.BooleanValue ? "true" : "false";
                }
            }
        }
        else if (contour.RecreateOnEdit != contour.OriginalRecreateOnEdit
            && recreate == contour.OriginalRecreateOnEditValue)
        {
            recreate = contour.RecreateOnEdit ? "true" : "false";
        }
        else if (!options.PreserveLexicalValues
            && contour.IsRecreateOnEditValid
            && recreate.Length > 0)
        {
            recreate = contour.RecreateOnEdit ? "true" : "false";
        }
        string width = CanonicalLengthText(
            contour.Width,
            options.PreserveLexicalValues);
        string height = CanonicalLengthText(
            contour.Height,
            options.PreserveLexicalValues);
        WriteChanged(node, OdfNamespaces.Svg, "width", OdfNamespaces.SvgPrefix, width, contour.OriginalWidth);
        WriteChanged(node, OdfNamespaces.Svg, "height", OdfNamespaces.SvgPrefix, height, contour.OriginalHeight);
        WriteChanged(node, OdfNamespaces.Svg, "viewBox", OdfNamespaces.SvgPrefix, viewBox, contour.ViewBox.OriginalLexicalValue);
        WriteChanged(node, OdfNamespaces.Drawing, "points", OdfNamespaces.DrawingPrefix, points, contour.PointList.OriginalLexicalValue);
        WriteChanged(node, OdfNamespaces.Svg, "d", OdfNamespaces.SvgPrefix, path, contour.Path.OriginalLexicalValue);
        WriteChanged(node, OdfNamespaces.Drawing, "recreate-on-edit", OdfNamespaces.DrawingPrefix, recreate, contour.OriginalRecreateOnEditValue);
        contour.Width = width;
        contour.Height = height;
        contour.ViewBox = SvgGeometryModel.ParseViewBox(viewBox);
        contour.PointList = SvgGeometryModel.ParsePoints(points);
        contour.Path = SvgGeometryModel.ParsePath(path);
        contour.RecreateOnEditValue = recreate;
        RefreshContourBoolean(contour);
        CaptureContourOriginalValues(contour);
    }

    static string CanonicalLengthText(string value, bool preserve)
    {
        if (preserve)
        {
            return value;
        }
        OdfUnitValue parsed = OdfLexicalParser.ParseLength(value);
        return parsed.IsValid
            ? OdfLexicalFormatter.FormatMeasurement(parsed)
            : value;
    }

    static void ApplyAccessibility(
        OdfDrawingAccessibility accessibility,
        OdfNode node)
    {
        if (accessibility == null)
        {
            return;
        }
        if (accessibility.Title != accessibility.OriginalTitle)
        {
            accessibility.TitleNode = SetAccessibleText(
                node,
                accessibility.TitleNode,
                "title",
                accessibility.Title);
        }
        if (accessibility.Description
            != accessibility.OriginalDescription)
        {
            accessibility.DescriptionNode = SetAccessibleText(
                node,
                accessibility.DescriptionNode,
                "desc",
                accessibility.Description);
        }
        accessibility.OriginalTitle = accessibility.Title;
        accessibility.OriginalDescription = accessibility.Description;
    }

    static void ApplyEvents(OdfDrawingEvent[] events)
    {
        int index = 0;
        while (index < events.Length)
        {
            OdfDrawingEvent item = events[index];
            if (item != null && item.Node != null)
            {
                WriteOptional(item.Node, OdfNamespaces.Script, "event-name", OdfNamespaces.ScriptPrefix, item.EventName);
                WriteOptional(item.Node, OdfNamespaces.Script, "language", OdfNamespaces.ScriptPrefix, item.Language);
                WriteOptional(item.Node, OdfNamespaces.Script, "macro-name", OdfNamespaces.ScriptPrefix, item.MacroName);
                WriteOptional(item.Node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, item.Href);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "action", OdfNamespaces.PresentationPrefix, item.Action);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "effect", OdfNamespaces.PresentationPrefix, item.Effect);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "direction", OdfNamespaces.PresentationPrefix, item.Direction);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "speed", OdfNamespaces.PresentationPrefix, item.Speed);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "start-scale", OdfNamespaces.PresentationPrefix, item.StartScale);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "verb", OdfNamespaces.PresentationPrefix, item.Verb);
                WriteOptional(item.Node, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, item.Show);
                WriteOptional(item.Node, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, item.Actuate);
                OdfNode? sound = FindDirectChild(item.Node, OdfNamespaces.Presentation, "sound");
                if (sound == null && (item.SoundHref.Length > 0
                        || item.SoundType.Length > 0
                        || item.SoundShow.Length > 0
                        || item.SoundActuate.Length > 0
                        || item.SoundPlayFullValue.Length > 0
                        || item.SoundPlayFull))
                {
                    sound = CreateElement(
                        OdfNamespaces.Presentation,
                        "sound",
                        OdfNamespaces.PresentationPrefix,
                        item.Node.Location.PartPath);
                    AppendChild(item.Node, sound);
                    item.SoundNode = sound;
                }
                if (sound != null)
                {
                    WriteOptional(sound, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, item.SoundHref);
                    WriteOptional(sound, OdfNamespaces.XLink, "type", OdfNamespaces.XLinkPrefix, item.SoundType);
                    WriteOptional(sound, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, item.SoundShow);
                    WriteOptional(sound, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, item.SoundActuate);
                    string playFullValue = item.SoundPlayFullValue;
                    OdfLexicalValue original = OdfLexicalParser.ParseBoolean(
                        item.OriginalSoundPlayFullValue);
                    bool originalPlayFull = original.IsValid
                        && original.BooleanValue;
                    if (item.SoundPlayFull != originalPlayFull
                        && playFullValue
                            == item.OriginalSoundPlayFullValue)
                    {
                        playFullValue = item.SoundPlayFull
                            ? "true"
                            : "false";
                        item.SoundPlayFullValue = playFullValue;
                    }
                    WriteOptional(sound, OdfNamespaces.Presentation, "play-full", OdfNamespaces.PresentationPrefix, playFullValue);
                    item.OriginalSoundPlayFullValue = playFullValue;
                }
            }
            index = index + 1;
        }
    }

    static void ApplyReferences(OdfDrawingObject drawing)
    {
        OdfDrawingReference[] references = drawing.References;
        bool navigationChanged = false;
        int index = 0;
        while (index < references.Length)
        {
            OdfDrawingReference reference = references[index];
            if (reference != null
                && reference.Node != null
                && reference.Target != reference.OriginalTarget)
            {
                if (reference.Kind
                    == OdfDrawingReferenceKind.NavigationOrder)
                {
                    navigationChanged = true;
                }
                else
                {
                    WriteReference(reference, drawing);
                }
                reference.OriginalTarget = reference.Target;
            }
            index = index + 1;
        }
        if (navigationChanged && drawing.Node != null)
        {
            drawing.NavigationOrder = NavigationOrderText(references);
            WriteOptional(
                drawing.Node,
                OdfNamespaces.Drawing,
                "nav-order",
                OdfNamespaces.DrawingPrefix,
                drawing.NavigationOrder);
        }
    }

    static void WriteReference(
        OdfDrawingReference reference,
        OdfDrawingObject drawing)
    {
        OdfNode node = reference.Node ?? new OdfNode();
        if (reference.Kind == OdfDrawingReferenceKind.CopyOf)
        {
            WriteOptional(node, OdfNamespaces.Drawing, "copy-of", OdfNamespaces.DrawingPrefix, reference.Target);
        }
        else if (reference.Kind == OdfDrawingReferenceKind.Caption)
        {
            WriteOptional(node, OdfNamespaces.Drawing, "caption-id", OdfNamespaces.DrawingPrefix, reference.Target);
        }
        else if (reference.Kind == OdfDrawingReferenceKind.ConnectorStart)
        {
            WriteOptional(node, OdfNamespaces.Drawing, "start-shape", OdfNamespaces.DrawingPrefix, reference.Target);
        }
        else if (reference.Kind == OdfDrawingReferenceKind.ConnectorEnd)
        {
            WriteOptional(node, OdfNamespaces.Drawing, "end-shape", OdfNamespaces.DrawingPrefix, reference.Target);
        }
        else if (reference.Kind == OdfDrawingReferenceKind.Hyperlink)
        {
            WriteOptional(node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, reference.Target);
        }
        else if (reference.Kind == OdfDrawingReferenceKind.Layer)
        {
            drawing.LayerName = reference.Target;
            WriteOptional(node, OdfNamespaces.Drawing, "layer", OdfNamespaces.DrawingPrefix, reference.Target);
        }
        else if (reference.Kind == OdfDrawingReferenceKind.Style)
        {
            drawing.StyleName = reference.Target;
            string namespaceName = drawing.StyleNamespace
                == OdfNamespaces.Presentation
                ? OdfNamespaces.Presentation
                : OdfNamespaces.Drawing;
            string prefix = namespaceName == OdfNamespaces.Presentation
                ? OdfNamespaces.PresentationPrefix
                : OdfNamespaces.DrawingPrefix;
            WriteOptional(node, namespaceName, "style-name", prefix, reference.Target);
        }
        else if (reference.Kind == OdfDrawingReferenceKind.TextStyle)
        {
            drawing.TextStyleName = reference.Target;
            WriteOptional(node, OdfNamespaces.Drawing, "text-style-name", OdfNamespaces.DrawingPrefix, reference.Target);
        }
        else if (reference.Kind == OdfDrawingReferenceKind.MasterPage)
        {
            drawing.MasterPageName = reference.Target;
            WriteOptional(node, OdfNamespaces.Drawing, "master-page-name", OdfNamespaces.DrawingPrefix, reference.Target);
        }
        else if (reference.Kind
            == OdfDrawingReferenceKind.PresentationPageLayout)
        {
            drawing.PresentationPageLayoutName = reference.Target;
            WriteOptional(node, OdfNamespaces.Presentation, "presentation-page-layout-name", OdfNamespaces.PresentationPrefix, reference.Target);
        }
        else if (reference.Kind == OdfDrawingReferenceKind.Control)
        {
            drawing.ControlId = reference.Target;
            WriteOptional(node, OdfNamespaces.Drawing, "control", OdfNamespaces.DrawingPrefix, reference.Target);
        }
    }

    static string NavigationOrderText(OdfDrawingReference[] references)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < references.Length)
        {
            OdfDrawingReference reference = references[index];
            if (reference.Kind == OdfDrawingReferenceKind.NavigationOrder
                && reference.Target.Length > 0)
            {
                if (result.Length > 0)
                {
                    result.Append(' ');
                }
                result.Append(reference.Target);
            }
            index = index + 1;
        }
        return result.ToString();
    }

    static OdfNode? SetAccessibleText(
        OdfNode parent,
        OdfNode? node,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            if (node != null)
            {
                RemoveChild(parent, node);
            }
            return null;
        }
        OdfNode element = node ?? CreateElement(
            OdfNamespaces.Svg,
            localName,
            OdfNamespaces.SvgPrefix,
            parent.Location.PartPath);
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = element;
        text.Location.PartPath = parent.Location.PartPath;
        element.Children = [text];
        if (node == null)
        {
            AppendChild(parent, element);
        }
        return element;
    }

    static void CaptureOriginalValues(OdfDrawingObject drawing)
    {
        drawing.OriginalId = drawing.Id;
        drawing.OriginalXmlId = drawing.XmlId;
        drawing.OriginalName = drawing.Name;
        drawing.OriginalDisplayName = drawing.DisplayName;
        drawing.OriginalStyleName = drawing.StyleName;
        drawing.OriginalStyleNamespace = drawing.StyleNamespace;
        drawing.OriginalClassNames = drawing.ClassNames;
        drawing.OriginalTextStyleName = drawing.TextStyleName;
        drawing.OriginalLayerName = drawing.LayerName;
        drawing.OriginalMasterPageName = drawing.MasterPageName;
        drawing.OriginalPresentationPageLayoutName =
            drawing.PresentationPageLayoutName;
        drawing.OriginalNavigationOrder = drawing.NavigationOrder;
        drawing.OriginalDisplay = drawing.Display;
        drawing.OriginalProtectedValue = drawing.ProtectedValue;
        drawing.OriginalIsProtected = drawing.IsProtected;
        drawing.OriginalX = drawing.X;
        drawing.OriginalY = drawing.Y;
        drawing.OriginalWidth = drawing.Width;
        drawing.OriginalHeight = drawing.Height;
        drawing.OriginalControlId = drawing.ControlId;
    }

    static void CaptureAnchorOriginalValues(OdfDrawingAnchor anchor)
    {
        anchor.OriginalKind = anchor.Kind;
        anchor.OriginalType = anchor.Type;
        anchor.OriginalPageNumberValue = anchor.PageNumberValue;
        anchor.OriginalEndCellAddress = anchor.EndCellAddress;
        anchor.OriginalEndX = anchor.EndX;
        anchor.OriginalEndY = anchor.EndY;
        anchor.OriginalTableBackgroundValue = anchor.TableBackgroundValue;
        anchor.OriginalPageNumber = anchor.PageNumber;
        anchor.OriginalIsTableBackground = anchor.IsTableBackground;
    }

    static void CaptureGeometryOriginalValues(OdfGeometry geometry)
    {
        geometry.OriginalX1 = geometry.X1;
        geometry.OriginalY1 = geometry.Y1;
        geometry.OriginalX2 = geometry.X2;
        geometry.OriginalY2 = geometry.Y2;
        geometry.OriginalCenterX = geometry.CenterX;
        geometry.OriginalCenterY = geometry.CenterY;
        geometry.OriginalRadius = geometry.Radius;
        geometry.OriginalRadiusX = geometry.RadiusX;
        geometry.OriginalRadiusY = geometry.RadiusY;
        geometry.OriginalCornerRadius = geometry.CornerRadius;
        geometry.OriginalEllipseKind = geometry.EllipseKind;
        geometry.OriginalStartAngle = geometry.StartAngle;
        geometry.OriginalEndAngle = geometry.EndAngle;
        geometry.OriginalPoints = geometry.Points;
        geometry.OriginalPathData = geometry.PathData;
        geometry.OriginalViewBox = geometry.ViewBox;
        geometry.OriginalCaptionPointX = geometry.CaptionPointX;
        geometry.OriginalCaptionPointY = geometry.CaptionPointY;
        geometry.OriginalConnectorType = geometry.ConnectorType;
        geometry.OriginalStartGluePoint = geometry.StartGluePoint;
        geometry.OriginalEndGluePoint = geometry.EndGluePoint;
        geometry.OriginalLineSkew = geometry.LineSkew;
        geometry.OriginalCorners = geometry.Corners;
        geometry.OriginalConcaveValue = geometry.ConcaveValue;
        geometry.OriginalIsConcave = geometry.IsConcave;
        geometry.OriginalSharpness = geometry.Sharpness;
        geometry.OriginalGluePointNodes = GluePointNodes(
            geometry.GluePoints);
    }

    static void RefreshContourBoolean(OdfContour contour)
    {
        OdfLexicalValue recreate = OdfLexicalParser.ParseBoolean(
            contour.RecreateOnEditValue);
        contour.IsRecreateOnEditValid =
            contour.RecreateOnEditValue.Length == 0 || recreate.IsValid;
        contour.RecreateOnEdit = recreate.IsValid
            && recreate.BooleanValue;
    }

    static void CaptureContourOriginalValues(OdfContour contour)
    {
        contour.OriginalWidth = contour.Width;
        contour.OriginalHeight = contour.Height;
        contour.OriginalRecreateOnEditValue =
            contour.RecreateOnEditValue;
        contour.OriginalRecreateOnEdit = contour.RecreateOnEdit;
    }

    static void CaptureEnhancedOriginalValues(
        OdfEnhancedGeometry enhanced)
    {
        enhanced.Preset.IsSpecified = enhanced.Preset.Name.Length > 0;
        enhanced.Preset.IsNonPrimitive =
            enhanced.Preset.Name == "non-primitive";
        enhanced.Preset.OriginalName = enhanced.Preset.Name;
        enhanced.OriginalViewBox = enhanced.ViewBox;
        enhanced.OriginalModifierText = enhanced.ModifierText;
        enhanced.OriginalEnhancedPath = enhanced.EnhancedPath;
        enhanced.OriginalStretchPointX = enhanced.StretchPointX;
        enhanced.OriginalStretchPointY = enhanced.StretchPointY;
        enhanced.OriginalTextAreas = enhanced.TextAreas;
        enhanced.OriginalGluePoints = enhanced.GluePoints;
        enhanced.OriginalGluePointType = enhanced.GluePointType;
        enhanced.OriginalGluePointLeavingDirections =
            enhanced.GluePointLeavingDirections;
        enhanced.OriginalMirrorVerticalValue =
            enhanced.MirrorVerticalValue;
        enhanced.OriginalMirrorVertical = enhanced.MirrorVertical;
        enhanced.OriginalMirrorHorizontalValue =
            enhanced.MirrorHorizontalValue;
        enhanced.OriginalMirrorHorizontal = enhanced.MirrorHorizontal;
        enhanced.OriginalTextRotateAngle = enhanced.TextRotateAngle;
    }

    static void RefreshEnhancedBooleans(OdfEnhancedGeometry enhanced)
    {
        OdfLexicalValue vertical = OdfLexicalParser.ParseBoolean(
            enhanced.MirrorVerticalValue);
        enhanced.MirrorVerticalValid =
            enhanced.MirrorVerticalValue.Length == 0 || vertical.IsValid;
        enhanced.MirrorVertical = vertical.IsValid
            && vertical.BooleanValue;
        OdfLexicalValue horizontal = OdfLexicalParser.ParseBoolean(
            enhanced.MirrorHorizontalValue);
        enhanced.MirrorHorizontalValid =
            enhanced.MirrorHorizontalValue.Length == 0 || horizontal.IsValid;
        enhanced.MirrorHorizontal = horizontal.IsValid
            && horizontal.BooleanValue;
    }

    static bool ModifiersChanged(OdfGeometryModifier[] modifiers)
    {
        int index = 0;
        while (index < modifiers.Length)
        {
            if (modifiers[index].Value
                != modifiers[index].OriginalValue)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string ModifierText(OdfGeometryModifier[] modifiers)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < modifiers.Length)
        {
            if (result.Length > 0)
            {
                result.Append(' ');
            }
            result.Append(modifiers[index].Value);
            modifiers[index].Index = index;
            index = index + 1;
        }
        return result.ToString();
    }

    static void RefreshModifierOriginalValues(
        OdfGeometryModifier[] modifiers)
    {
        int index = 0;
        while (index < modifiers.Length)
        {
            modifiers[index].Index = index;
            modifiers[index].OriginalValue = modifiers[index].Value;
            index = index + 1;
        }
    }

    static OdfGeometryHandleKind HandleKind(OdfGeometryHandle handle)
    {
        if (handle.PositionX.Length > 0
            || handle.PositionY.Length > 0)
        {
            return OdfGeometryHandleKind.Cartesian;
        }
        if (handle.PolarPoleX.Length > 0
            || handle.PolarPoleY.Length > 0)
        {
            return OdfGeometryHandleKind.Polar;
        }
        if (handle.LegacyPosition.Length > 0
            || handle.LegacyPolar.Length > 0)
        {
            return OdfGeometryHandleKind.Legacy;
        }
        return OdfGeometryHandleKind.Unknown;
    }

    static void CaptureHandleOriginalValues(OdfGeometryHandle handle)
    {
        handle.OriginalPositionX = handle.PositionX;
        handle.OriginalPositionY = handle.PositionY;
        handle.OriginalRangeXMinimum = handle.RangeXMinimum;
        handle.OriginalRangeXMaximum = handle.RangeXMaximum;
        handle.OriginalRangeYMinimum = handle.RangeYMinimum;
        handle.OriginalRangeYMaximum = handle.RangeYMaximum;
        handle.OriginalPolarPoleX = handle.PolarPoleX;
        handle.OriginalPolarPoleY = handle.PolarPoleY;
        handle.OriginalPolarRadius = handle.PolarRadius;
        handle.OriginalPolarAngle = handle.PolarAngle;
        handle.OriginalRadiusRangeMinimum = handle.RadiusRangeMinimum;
        handle.OriginalRadiusRangeMaximum = handle.RadiusRangeMaximum;
        handle.OriginalLegacyPosition = handle.LegacyPosition;
        handle.OriginalLegacyPolar = handle.LegacyPolar;
        handle.OriginalMirrorVerticalValue = handle.MirrorVerticalValue;
        handle.OriginalMirrorVertical = handle.MirrorVertical;
        handle.OriginalMirrorHorizontalValue = handle.MirrorHorizontalValue;
        handle.OriginalMirrorHorizontal = handle.MirrorHorizontal;
        handle.OriginalSwitchedValue = handle.SwitchedValue;
        handle.OriginalIsSwitched = handle.IsSwitched;
    }

    static void RefreshHandleBooleans(OdfGeometryHandle handle)
    {
        OdfLexicalValue vertical = OdfLexicalParser.ParseBoolean(
            handle.MirrorVerticalValue);
        handle.MirrorVerticalValid = handle.MirrorVerticalValue.Length == 0
            || vertical.IsValid;
        handle.MirrorVertical = vertical.IsValid && vertical.BooleanValue;
        OdfLexicalValue horizontal = OdfLexicalParser.ParseBoolean(
            handle.MirrorHorizontalValue);
        handle.MirrorHorizontalValid =
            handle.MirrorHorizontalValue.Length == 0 || horizontal.IsValid;
        handle.MirrorHorizontal = horizontal.IsValid
            && horizontal.BooleanValue;
        OdfLexicalValue switched = OdfLexicalParser.ParseBoolean(
            handle.SwitchedValue);
        handle.IsSwitchedValid = handle.SwitchedValue.Length == 0
            || switched.IsValid;
        handle.IsSwitched = switched.IsValid && switched.BooleanValue;
    }

    static void RefreshGluePointId(OdfGluePoint point)
    {
        OdfLexicalValue id = OdfLexicalParser.ParseInteger(point.IdValue);
        point.IsIdValid = id.IsValid && id.IntegerValue >= 0;
        point.Id = point.IsIdValid ? id.IntegerValue : 0;
    }

    static void CaptureGluePointOriginalValues(OdfGluePoint point)
    {
        point.OriginalIdValue = point.IdValue;
        point.OriginalId = point.Id;
        point.OriginalX = point.X;
        point.OriginalY = point.Y;
        point.OriginalAlign = point.Align;
        point.OriginalEscapeDirection = point.EscapeDirection;
    }

    static void CaptureRoutingOriginalValues(OdfConnectorRouting routing)
    {
        routing.OriginalType = routing.Type;
        routing.OriginalStartShapeId = routing.StartShapeId;
        routing.OriginalEndShapeId = routing.EndShapeId;
        routing.OriginalStartGluePoint = routing.StartGluePoint;
        routing.OriginalEndGluePoint = routing.EndGluePoint;
        routing.OriginalLineSkew = routing.LineSkew;
        routing.OriginalPathData = routing.PathData;
    }

    static OdfNode[] EquationNodes(OdfGeometryEquation[] equations)
    {
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < equations.Length)
        {
            OdfNode? node = equations[index].Node;
            if (node != null)
            {
                result.Add(node);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfNode[] HandleNodes(OdfGeometryHandle[] handles)
    {
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < handles.Length)
        {
            OdfNode? node = handles[index].Node;
            if (node != null)
            {
                result.Add(node);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfNode[] GluePointNodes(OdfGluePoint[] points)
    {
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < points.Length)
        {
            OdfNode? node = points[index].Node;
            if (node != null)
            {
                result.Add(node);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void RefreshGeometryValues(OdfGeometry geometry)
    {
        OdfLexicalValue concave = OdfLexicalParser.ParseBoolean(
            geometry.ConcaveValue);
        geometry.IsConcaveValid = geometry.ConcaveValue.Length == 0
            || concave.IsValid;
        geometry.IsConcave = concave.IsValid && concave.BooleanValue;
    }

    static void RefreshAnchorValues(OdfDrawingAnchor anchor)
    {
        anchor.Kind = ParseAnchorKind(anchor.Type);
        anchor.HasPageNumber = anchor.PageNumberValue.Length > 0;
        OdfLexicalValue page = OdfLexicalParser.ParseInteger(
            anchor.PageNumberValue);
        anchor.IsPageNumberValid = anchor.HasPageNumber
            && page.IsValid
            && page.IntegerValue > 0;
        anchor.PageNumber = anchor.IsPageNumberValid
            ? page.IntegerValue
            : 0;
        OdfLexicalValue background = OdfLexicalParser.ParseBoolean(
            anchor.TableBackgroundValue);
        anchor.IsTableBackgroundValid =
            anchor.TableBackgroundValue.Length == 0 || background.IsValid;
        anchor.IsTableBackground = background.IsValid
            && background.BooleanValue;
    }

    static void RefreshZOrder(OdfDrawingZOrder zOrder)
    {
        zOrder.IsSpecified = zOrder.LexicalValue.Length > 0;
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(
            zOrder.LexicalValue);
        zOrder.IsValid = !zOrder.IsSpecified
            || parsed.IsValid && parsed.IntegerValue >= 0;
        zOrder.Index = zOrder.IsSpecified && zOrder.IsValid
            ? parsed.IntegerValue
            : 0;
    }

    static OdfDrawingKind DrawingKind(OdfNode node)
    {
        if (node.Kind != OdfNodeKind.Element || node.Name == null)
        {
            return OdfDrawingKind.Unknown;
        }
        string namespaceName = node.Name.NamespaceName;
        string localName = node.Name.LocalName;
        if (namespaceName == OdfNamespaces.Drawing)
        {
            return DrawingNamespaceKind(localName);
        }
        if (namespaceName == OdfNamespaces.Dr3d)
        {
            return ThreeDimensionalKind(localName);
        }
        return OdfDrawingKind.Unknown;
    }

    static OdfDrawingKind DrawingNamespaceKind(string localName)
    {
        if (localName == "page") return OdfDrawingKind.Page;
        if (localName == "layer") return OdfDrawingKind.Layer;
        if (localName == "frame") return OdfDrawingKind.Frame;
        if (localName == "g") return OdfDrawingKind.Group;
        if (localName == "rect") return OdfDrawingKind.Rectangle;
        if (localName == "ellipse" || localName == "circle") return OdfDrawingKind.Ellipse;
        if (localName == "line") return OdfDrawingKind.Line;
        if (localName == "polyline") return OdfDrawingKind.Polyline;
        if (localName == "polygon") return OdfDrawingKind.Polygon;
        if (localName == "regular-polygon") return OdfDrawingKind.RegularPolygon;
        if (localName == "path") return OdfDrawingKind.Path;
        if (localName == "connector") return OdfDrawingKind.Connector;
        if (localName == "measure") return OdfDrawingKind.Measure;
        if (localName == "caption") return OdfDrawingKind.Caption;
        if (localName == "control") return OdfDrawingKind.Control;
        if (localName == "text-box") return OdfDrawingKind.TextBox;
        if (localName == "custom-shape") return OdfDrawingKind.CustomShape;
        if (localName == "image") return OdfDrawingKind.Image;
        if (localName == "object" || localName == "object-ole") return OdfDrawingKind.EmbeddedObject;
        if (localName == "plugin" || localName == "applet" || localName == "floating-frame") return OdfDrawingKind.Media;
        return OdfDrawingKind.Unknown;
    }

    static OdfDrawingKind ThreeDimensionalKind(string localName)
    {
        if (localName == "scene") return OdfDrawingKind.Scene3D;
        if (localName == "cube") return OdfDrawingKind.Cube;
        if (localName == "sphere") return OdfDrawingKind.Sphere;
        if (localName == "rotate") return OdfDrawingKind.Rotate;
        if (localName == "extrude") return OdfDrawingKind.Extrude;
        return OdfDrawingKind.Unknown;
    }

    static OdfDrawingEventKind EventKind(OdfNode node)
    {
        if (IsElement(node, OdfNamespaces.Script, "event-listener"))
        {
            return OdfDrawingEventKind.Script;
        }
        if (IsElement(node, OdfNamespaces.Presentation, "event-listener"))
        {
            return OdfDrawingEventKind.Presentation;
        }
        return OdfDrawingEventKind.Unknown;
    }

    static OdfTextAnchorKind ParseAnchorKind(string value)
    {
        if (value == "paragraph") return OdfTextAnchorKind.Paragraph;
        if (value == "char") return OdfTextAnchorKind.Character;
        if (value == "as-char") return OdfTextAnchorKind.AsCharacter;
        if (value == "page") return OdfTextAnchorKind.Page;
        if (value == "frame") return OdfTextAnchorKind.Frame;
        if (value == "cell") return OdfTextAnchorKind.Cell;
        return OdfTextAnchorKind.Unknown;
    }

    static string AnchorKindText(OdfTextAnchorKind kind)
    {
        if (kind == OdfTextAnchorKind.Paragraph) return "paragraph";
        if (kind == OdfTextAnchorKind.Character) return "char";
        if (kind == OdfTextAnchorKind.AsCharacter) return "as-char";
        if (kind == OdfTextAnchorKind.Page) return "page";
        if (kind == OdfTextAnchorKind.Frame) return "frame";
        if (kind == OdfTextAnchorKind.Cell) return "cell";
        return "";
    }

    static OdfDrawingObject[] AppendObject(
        OdfDrawingObject[] values,
        OdfDrawingObject value)
    {
        OdfDrawingObject[] result = new OdfDrawingObject[values.Length + 1];
        global::System.Array.Copy(values, result, values.Length);
        result[result.Length - 1] = value;
        return result;
    }

    static OdfDrawingObject[] CopyObjects(OdfDrawingObject[] values)
    {
        OdfDrawingObject[] result = new OdfDrawingObject[values.Length];
        global::System.Array.Copy(values, result, values.Length);
        return result;
    }

    static string TextContent(OdfNode? node)
    {
        if (node == null)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        AppendText(node, result);
        return result.ToString();
    }

    static void AppendText(OdfNode node, StringBuilder result)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendText(node.Children[index], result);
            index = index + 1;
        }
    }

    static OdfNode? FindDirectChild(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsElement(node.Children[index], namespaceName, localName))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            namespaceName,
            localName,
            prefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        global::System.Array.Copy(
            parent.Children,
            children,
            parent.Children.Length);
        child.Parent = parent;
        children[children.Length - 1] = child;
        parent.Children = children;
    }

    static void RemoveChild(OdfNode parent, OdfNode child)
    {
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] != child)
            {
                children.Add(parent.Children[index]);
            }
            index = index + 1;
        }
        child.Parent = null;
        parent.Children = children.ToArray();
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
    }

    static void WriteChanged(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string original)
    {
        if (value != original)
        {
            WriteOptional(node, namespaceName, localName, prefix, value);
        }
    }

    static void WriteOptional(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }
}
