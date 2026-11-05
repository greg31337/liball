using System;
using System.Collections.Generic;
using System.Text;

namespace htmlgui;

internal class LayoutExpander
{
    internal DomNode ExpandLayout(UiNode node, DomNode[] children)
    {
        UiLayout? layout = node.Layout;
        if (layout == null)
        {
            throw CreateRenderException(node, "A layout node requires a layout description.");
        }

        string token = GetLayoutToken(layout.Kind);
        string elementName = HtmlElements.Division;
        if (layout.Kind == UiLayoutKind.Inline)
        {
            elementName = HtmlElements.Span;
        }

        List<UiCssDeclaration> declarations = new List<UiCssDeclaration>();
        AddBaseLayout(layout, declarations);
        AddSizing(layout, declarations);
        AddSpacing(layout, declarations);
        AddAlignment(layout, declarations);
        AddGrid(layout, declarations, node);
        AddPosition(layout, declarations);
        AddScroll(layout, declarations);
        AddContainment(layout, declarations);
        AddResize(layout, declarations);
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-layout htmlgui-layout-" + token });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "layout", Value = token });
        AddScrollAttributes(layout.Scroll, attributes);
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Style, Value = SerializeDeclarations(declarations) });
        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = elementName,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal void AddBaseLayout(UiLayout layout, List<UiCssDeclaration> declarations)
    {
        switch (layout.Kind)
        {
            case UiLayoutKind.Flow:
            case UiLayoutKind.Scroll:
                SetDeclaration(declarations, CssProperties.Display, "block");
                break;
            case UiLayoutKind.Inline:
                SetDeclaration(declarations, CssProperties.Display, "inline");
                break;
            case UiLayoutKind.VerticalStack:
            case UiLayoutKind.FlexColumn:
                SetDeclaration(declarations, CssProperties.Display, "flex");
                SetDeclaration(declarations, CssProperties.FlexDirection, "column");
                break;
            case UiLayoutKind.HorizontalStack:
            case UiLayoutKind.FlexRow:
                SetDeclaration(declarations, CssProperties.Display, "flex");
                SetDeclaration(declarations, CssProperties.FlexDirection, "row");
                break;
            case UiLayoutKind.Wrap:
                SetDeclaration(declarations, CssProperties.Display, "flex");
                SetDeclaration(declarations, CssProperties.FlexWrap, "wrap");
                break;
            case UiLayoutKind.Grid:
            case UiLayoutKind.UniformGrid:
            case UiLayoutKind.Dock:
            case UiLayoutKind.Overlay:
            case UiLayoutKind.ResponsiveShell:
                SetDeclaration(declarations, CssProperties.Display, "grid");
                break;
            case UiLayoutKind.Absolute:
                SetDeclaration(declarations, CssProperties.Position, "relative");
                break;
            case UiLayoutKind.Split:
                SetDeclaration(declarations, CssProperties.Display, "flex");
                SetDeclaration(declarations, CssProperties.FlexDirection, GetOrientationToken(layout.Orientation));
                break;
            default:
                GetLayoutToken(layout.Kind);
                break;
        }

        if (layout.Wrap)
        {
            SetDeclaration(declarations, CssProperties.FlexWrap, "wrap");
        }
    }

    internal void AddSizing(UiLayout layout, List<UiCssDeclaration> declarations)
    {
        AddOptionalLength(declarations, CssProperties.Width, layout.Width);
        AddOptionalLength(declarations, CssProperties.Height, layout.Height);
        AddOptionalLength(declarations, CssProperties.MinimumWidth, layout.MinimumWidth);
        AddOptionalLength(declarations, CssProperties.MaximumWidth, layout.MaximumWidth);
        AddOptionalLength(declarations, CssProperties.MinimumHeight, layout.MinimumHeight);
        AddOptionalLength(declarations, CssProperties.MaximumHeight, layout.MaximumHeight);
        if (layout.HasAspectRatio)
        {
            if (layout.AspectRatio <= 0.0 || double.IsNaN(layout.AspectRatio) || double.IsInfinity(layout.AspectRatio))
            {
                throw new ArgumentOutOfRangeException("aspectRatio", layout.AspectRatio, "The aspect ratio must be finite and positive.");
            }

            SetDeclaration(declarations, CssProperties.AspectRatio, InvariantFormatter.FormatDouble(layout.AspectRatio));
        }
    }

    internal void AddSpacing(UiLayout layout, List<UiCssDeclaration> declarations)
    {
        AddOptionalLength(declarations, CssProperties.Gap, layout.Gap);
        AddSpacingEdges(declarations, layout.Margin, "margin");
        AddSpacingEdges(declarations, layout.Padding, "padding");
        if (layout.UseSafeAreaInsets)
        {
            SetDeclaration(declarations, "padding-block-start", CreateSafeAreaValue(layout.Padding.BlockStart, "top"));
            SetDeclaration(declarations, "padding-inline-end", CreateSafeAreaValue(layout.Padding.InlineEnd, "right"));
            SetDeclaration(declarations, "padding-block-end", CreateSafeAreaValue(layout.Padding.BlockEnd, "bottom"));
            SetDeclaration(declarations, "padding-inline-start", CreateSafeAreaValue(layout.Padding.InlineStart, "left"));
        }
    }

    internal void AddSpacingEdges(List<UiCssDeclaration> declarations, UiSpacing spacing, string prefix)
    {
        AddOptionalLength(declarations, prefix + "-block-start", spacing.BlockStart);
        AddOptionalLength(declarations, prefix + "-inline-end", spacing.InlineEnd);
        AddOptionalLength(declarations, prefix + "-block-end", spacing.BlockEnd);
        AddOptionalLength(declarations, prefix + "-inline-start", spacing.InlineStart);
    }

    internal string CreateSafeAreaValue(UiLength length, string physicalEdge)
    {
        string inset = "env(safe-area-inset-" + physicalEdge + ")";
        string explicitValue = FormatOptionalLength(length);
        if (explicitValue.Length == 0)
        {
            return inset;
        }

        return "max(" + explicitValue + ", " + inset + ")";
    }

    internal void AddAlignment(UiLayout layout, List<UiCssDeclaration> declarations)
    {
        SetDeclaration(declarations, CssProperties.AlignItems, GetAlignmentToken(layout.Alignment));
        SetDeclaration(declarations, CssProperties.JustifyContent, GetJustificationToken(layout.Justification));
    }

    internal void AddGrid(UiLayout layout, List<UiCssDeclaration> declarations, UiNode node)
    {
        string columns = FormatTracks(layout.Columns, node);
        if (columns.Length > 0)
        {
            SetDeclaration(declarations, CssProperties.GridTemplateColumns, columns);
        }

        string rows = FormatTracks(layout.Rows, node);
        if (rows.Length > 0)
        {
            SetDeclaration(declarations, CssProperties.GridTemplateRows, rows);
        }

        UiGridPlacement? placement = layout.GridPlacement;
        if (placement == null)
        {
            return;
        }

        if (placement.Area != null && placement.Area.Length > 0)
        {
            if (!IsCssIdentifier(placement.Area))
            {
                throw CreateRenderException(node, "Grid area names must be valid CSS identifiers.");
            }

            SetDeclaration(declarations, CssProperties.GridArea, placement.Area);
        }

        string row = FormatGridPlacement(placement.HasRow, placement.Row, placement.HasRowSpan, placement.RowSpan, "row");
        if (row.Length > 0)
        {
            SetDeclaration(declarations, CssProperties.GridRow, row);
        }

        string column = FormatGridPlacement(placement.HasColumn, placement.Column, placement.HasColumnSpan, placement.ColumnSpan, "column");
        if (column.Length > 0)
        {
            SetDeclaration(declarations, CssProperties.GridColumn, column);
        }
    }

    internal string FormatGridPlacement(bool hasStart, int start, bool hasSpan, int span, string parameterName)
    {
        if (hasStart && start < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, start, "Grid indexes must be positive.");
        }

        if (hasSpan && span < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName + "Span", span, "Grid spans must be positive.");
        }

        if (hasStart && hasSpan)
        {
            return InvariantFormatter.FormatInt32(start) + " / span " + InvariantFormatter.FormatInt32(span);
        }

        if (hasStart)
        {
            return InvariantFormatter.FormatInt32(start);
        }

        if (hasSpan)
        {
            return "span " + InvariantFormatter.FormatInt32(span);
        }

        return string.Empty;
    }

    internal string FormatTracks(UiGridTrack[]? tracks, UiNode node)
    {
        if (tracks == null || tracks.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < tracks.Length)
        {
            UiGridTrack? track = tracks[index];
            if (track == null)
            {
                throw CreateRenderException(node, "Grid tracks cannot contain null.");
            }

            if (index > 0)
            {
                result.Append(' ');
            }

            result.Append(FormatTrack(track, node));
            index++;
        }

        return result.ToString();
    }

    internal string FormatTrack(UiGridTrack track, UiNode node)
    {
        string minimum = FormatOptionalLength(track.Minimum);
        string maximum = FormatOptionalLength(track.Maximum);
        string size = "auto";
        if (minimum.Length > 0 || maximum.Length > 0)
        {
            if (minimum.Length == 0)
            {
                minimum = "auto";
            }

            if (maximum.Length == 0)
            {
                maximum = "auto";
            }

            size = "minmax(" + minimum + ", " + maximum + ")";
        }

        if (track.AutoFit && track.HasRepeatCount)
        {
            throw CreateRenderException(node, "A grid track cannot use auto-fit and a fixed repeat count together.");
        }

        if (track.AutoFit)
        {
            size = "repeat(auto-fit, " + size + ")";
        }
        else if (track.HasRepeatCount)
        {
            if (track.RepeatCount < 1)
            {
                throw new ArgumentOutOfRangeException("repeatCount", track.RepeatCount, "Grid repeat counts must be positive.");
            }

            size = "repeat(" + InvariantFormatter.FormatInt32(track.RepeatCount) + ", " + size + ")";
        }

        if (track.Name != null && track.Name.Length > 0)
        {
            if (!IsCssIdentifier(track.Name))
            {
                throw CreateRenderException(node, "Grid track names must be valid CSS identifiers.");
            }

            size = "[" + track.Name + "] " + size;
        }

        return size;
    }

    internal void AddPosition(UiLayout layout, List<UiCssDeclaration> declarations)
    {
        UiPosition? position = layout.Position;
        if (position == null)
        {
            return;
        }

        if (position.IsSticky)
        {
            SetDeclaration(declarations, CssProperties.Position, "sticky");
        }
        else if (position.HasBlockStart || position.HasInlineEnd || position.HasBlockEnd || position.HasInlineStart)
        {
            SetDeclaration(declarations, CssProperties.Position, "absolute");
        }

        AddFlaggedLength(declarations, "inset-block-start", position.HasBlockStart, position.BlockStart);
        AddFlaggedLength(declarations, "inset-inline-end", position.HasInlineEnd, position.InlineEnd);
        AddFlaggedLength(declarations, "inset-block-end", position.HasBlockEnd, position.BlockEnd);
        AddFlaggedLength(declarations, "inset-inline-start", position.HasInlineStart, position.InlineStart);
        if (position.HasZIndex)
        {
            SetDeclaration(declarations, CssProperties.ZIndex, InvariantFormatter.FormatInt32(position.ZIndex));
        }
    }

    internal void AddScroll(UiLayout layout, List<UiCssDeclaration> declarations)
    {
        UiScrollOptions? scroll = layout.Scroll;
        if (scroll == null)
        {
            return;
        }

        SetDeclaration(declarations, CssProperties.OverflowX, GetOverflowToken(scroll.Horizontal));
        SetDeclaration(declarations, CssProperties.OverflowY, GetOverflowToken(scroll.Vertical));
        if (scroll.Smooth)
        {
            SetDeclaration(declarations, CssProperties.ScrollBehavior, "smooth");
        }
    }

    internal void AddScrollAttributes(UiScrollOptions? scroll, List<DomAttribute> attributes)
    {
        if (scroll == null)
        {
            return;
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "preserve-scroll", Value = InvariantFormatter.FormatBoolean(scroll.PreservePosition) });
        if (scroll.AnchorKey != null && scroll.AnchorKey.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "scroll-anchor", Value = scroll.AnchorKey });
        }

        if (scroll.RevealNodeId != null && scroll.RevealNodeId.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "reveal-node", Value = scroll.RevealNodeId });
        }
    }

    internal void AddContainment(UiLayout layout, List<UiCssDeclaration> declarations)
    {
        List<string> tokens = new List<string>();
        if (layout.ContainSize)
        {
            tokens.Add("size");
        }

        if (layout.ContainLayout)
        {
            tokens.Add("layout");
        }

        if (layout.ContainPaint)
        {
            tokens.Add("paint");
        }

        if (tokens.Count > 0)
        {
            SetDeclaration(declarations, CssProperties.Contain, string.Join(" ", tokens));
        }
    }

    internal void AddResize(UiLayout layout, List<UiCssDeclaration> declarations)
    {
        if (layout.IsResizable)
        {
            string value;
            switch (layout.ResizeOrientation)
            {
                case UiOrientation.Horizontal:
                    value = "horizontal";
                    break;
                case UiOrientation.Vertical:
                    value = "vertical";
                    break;
                default:
                    throw new ArgumentOutOfRangeException("resizeOrientation", layout.ResizeOrientation, "The resize orientation is not supported.");
            }

            SetDeclaration(declarations, "resize", value);
        }
    }

    internal void AddOptionalLength(List<UiCssDeclaration> declarations, string property, UiLength? length)
    {
        string value = FormatOptionalLength(length);
        if (value.Length > 0)
        {
            SetDeclaration(declarations, property, value);
        }
    }

    internal void AddFlaggedLength(List<UiCssDeclaration> declarations, string property, bool isSupplied, UiLength length)
    {
        if (isSupplied)
        {
            string value = FormatOptionalLength(length);
            if (value.Length == 0)
            {
                value = "auto";
            }

            SetDeclaration(declarations, property, value);
        }
    }

    internal string FormatOptionalLength(UiLength? length)
    {
        if (length == null)
        {
            return string.Empty;
        }

        if (length.Expression != null && length.Expression.Length > 0)
        {
            if (length.Expression.Contains(";", StringComparison.Ordinal) || length.Expression.Contains("{", StringComparison.Ordinal) || length.Expression.Contains("}", StringComparison.Ordinal))
            {
                throw new ArgumentException("Length expressions cannot contain declaration delimiters.", "expression");
            }

            return length.Expression;
        }

        if (!length.HasValue)
        {
            return string.Empty;
        }

        if (length.Unit == UiLengthUnit.Auto)
        {
            throw new ArgumentException("A numeric length requires a concrete unit.", "unit");
        }

        return InvariantFormatter.FormatDouble(length.Value) + GetUnitSuffix(length.Unit);
    }

    internal string GetUnitSuffix(UiLengthUnit unit)
    {
        switch (unit)
        {
            case UiLengthUnit.Pixel:
                return CssUnits.Pixel;
            case UiLengthUnit.Percent:
                return CssUnits.Percent;
            case UiLengthUnit.Em:
                return CssUnits.Em;
            case UiLengthUnit.Rem:
                return CssUnits.Rem;
            case UiLengthUnit.ViewportWidth:
                return CssUnits.ViewportWidth;
            case UiLengthUnit.ViewportHeight:
                return CssUnits.ViewportHeight;
            case UiLengthUnit.Fraction:
                return CssUnits.Fraction;
            case UiLengthUnit.Character:
                return CssUnits.Character;
            case UiLengthUnit.Centimeter:
                return CssUnits.Centimeter;
            case UiLengthUnit.Millimeter:
                return CssUnits.Millimeter;
            case UiLengthUnit.Inch:
                return CssUnits.Inch;
            case UiLengthUnit.Point:
                return CssUnits.Point;
            case UiLengthUnit.Pica:
                return CssUnits.Pica;
            default:
                throw new ArgumentOutOfRangeException("unit", unit, "The length unit is not supported.");
        }
    }

    internal string GetAlignmentToken(UiAlignment alignment)
    {
        switch (alignment)
        {
            case UiAlignment.Start:
                return "flex-start";
            case UiAlignment.Center:
                return "center";
            case UiAlignment.End:
                return "flex-end";
            case UiAlignment.Stretch:
                return "stretch";
            case UiAlignment.Baseline:
                return "baseline";
            default:
                throw new ArgumentOutOfRangeException("alignment", alignment, "The alignment is not supported.");
        }
    }

    internal string GetJustificationToken(UiJustification justification)
    {
        switch (justification)
        {
            case UiJustification.Start:
                return "flex-start";
            case UiJustification.Center:
                return "center";
            case UiJustification.End:
                return "flex-end";
            case UiJustification.SpaceBetween:
                return "space-between";
            case UiJustification.SpaceAround:
                return "space-around";
            case UiJustification.SpaceEvenly:
                return "space-evenly";
            default:
                throw new ArgumentOutOfRangeException("justification", justification, "The justification is not supported.");
        }
    }

    internal string GetOverflowToken(UiOverflowMode overflow)
    {
        switch (overflow)
        {
            case UiOverflowMode.Visible:
                return "visible";
            case UiOverflowMode.Hidden:
                return "hidden";
            case UiOverflowMode.Clip:
                return "clip";
            case UiOverflowMode.Scroll:
                return "scroll";
            case UiOverflowMode.Auto:
                return "auto";
            default:
                throw new ArgumentOutOfRangeException("overflow", overflow, "The overflow mode is not supported.");
        }
    }

    internal string GetOrientationToken(UiOrientation orientation)
    {
        switch (orientation)
        {
            case UiOrientation.Horizontal:
                return "row";
            case UiOrientation.Vertical:
                return "column";
            default:
                throw new ArgumentOutOfRangeException("orientation", orientation, "The orientation is not supported.");
        }
    }

    internal void SetDeclaration(List<UiCssDeclaration> declarations, string property, string value)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index].Property == property)
            {
                declarations[index].Value = value;
                return;
            }

            index++;
        }

        declarations.Add(new UiCssDeclaration { Property = property, Value = value });
    }

    internal string SerializeDeclarations(List<UiCssDeclaration> declarations)
    {
        int index = 1;
        while (index < declarations.Count)
        {
            UiCssDeclaration candidate = declarations[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(declarations[insertionIndex].Property, candidate.Property) > 0)
            {
                declarations[insertionIndex + 1] = declarations[insertionIndex];
                insertionIndex--;
            }

            declarations[insertionIndex + 1] = candidate;
            index++;
        }

        StringBuilder result = new StringBuilder();
        index = 0;
        while (index < declarations.Count)
        {
            result.Append(declarations[index].Property);
            result.Append(':');
            result.Append(declarations[index].Value);
            result.Append(';');
            index++;
        }

        return result.ToString();
    }

    internal void SortAttributes(List<DomAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(attributes[insertionIndex].Name, candidate.Name) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal bool IsCssIdentifier(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        char first = value[0];
        bool firstIsLetter = first >= 'a' && first <= 'z' || first >= 'A' && first <= 'Z';
        if (!firstIsLetter && first != '_' && first != '-')
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool isLetter = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z';
            bool isDigit = character >= '0' && character <= '9';
            if (!isLetter && !isDigit && character != '-' && character != '_')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal string GetLayoutToken(UiLayoutKind kind)
    {
        switch (kind)
        {
            case UiLayoutKind.Flow:
                return "flow";
            case UiLayoutKind.Inline:
                return "inline";
            case UiLayoutKind.VerticalStack:
                return "vertical-stack";
            case UiLayoutKind.HorizontalStack:
                return "horizontal-stack";
            case UiLayoutKind.FlexRow:
                return "flex-row";
            case UiLayoutKind.FlexColumn:
                return "flex-column";
            case UiLayoutKind.Wrap:
                return "wrap";
            case UiLayoutKind.Grid:
                return "grid";
            case UiLayoutKind.UniformGrid:
                return "uniform-grid";
            case UiLayoutKind.Dock:
                return "dock";
            case UiLayoutKind.Overlay:
                return "overlay";
            case UiLayoutKind.Absolute:
                return "absolute";
            case UiLayoutKind.Split:
                return "split";
            case UiLayoutKind.Scroll:
                return "scroll";
            case UiLayoutKind.ResponsiveShell:
                return "responsive-shell";
            default:
                throw new ArgumentOutOfRangeException("kind", kind, "The layout kind is not supported.");
        }
    }

    internal string GetNodeKey(UiNode node)
    {
        UiNodeIdentity? identity = node.Identity;
        if (identity == null)
        {
            return string.Empty;
        }

        if (identity.AuthorKey.Length > 0)
        {
            return identity.AuthorKey;
        }

        return identity.SiblingIdentity;
    }

    internal string GetNodeId(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        return node.Identity.StableId;
    }

    internal int GetNodeVersion(UiNode node)
    {
        if (node.State == null)
        {
            return 0;
        }

        return node.State.Version;
    }

    internal UiRenderException CreateRenderException(UiNode node, string message)
    {
        string nodeId = GetNodeId(node);
        if (nodeId.Length == 0)
        {
            nodeId = "unknown";
        }

        return new UiRenderException(message, nodeId, "dom");
    }
}
