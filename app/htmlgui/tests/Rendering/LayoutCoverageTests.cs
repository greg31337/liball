using System;
using htmlgui;

namespace htmlgui.Tests;

public class LayoutCoverageTests
{
    [TestCase(UiLayoutKind.Flow, UiOrientation.Horizontal, "block", "", "", "")]
    [TestCase(UiLayoutKind.Inline, UiOrientation.Horizontal, "inline", "", "", "")]
    [TestCase(UiLayoutKind.VerticalStack, UiOrientation.Horizontal, "flex", "column", "", "")]
    [TestCase(UiLayoutKind.HorizontalStack, UiOrientation.Horizontal, "flex", "row", "", "")]
    [TestCase(UiLayoutKind.FlexRow, UiOrientation.Horizontal, "flex", "row", "", "")]
    [TestCase(UiLayoutKind.FlexColumn, UiOrientation.Horizontal, "flex", "column", "", "")]
    [TestCase(UiLayoutKind.Wrap, UiOrientation.Horizontal, "flex", "", "wrap", "")]
    [TestCase(UiLayoutKind.Grid, UiOrientation.Horizontal, "grid", "", "", "")]
    [TestCase(UiLayoutKind.UniformGrid, UiOrientation.Horizontal, "grid", "", "", "")]
    [TestCase(UiLayoutKind.Dock, UiOrientation.Horizontal, "grid", "", "", "")]
    [TestCase(UiLayoutKind.Overlay, UiOrientation.Horizontal, "grid", "", "", "")]
    [TestCase(UiLayoutKind.Absolute, UiOrientation.Horizontal, "", "", "", "relative")]
    [TestCase(UiLayoutKind.Split, UiOrientation.Horizontal, "flex", "row", "", "")]
    [TestCase(UiLayoutKind.Split, UiOrientation.Vertical, "flex", "column", "", "")]
    [TestCase(UiLayoutKind.Scroll, UiOrientation.Horizontal, "block", "", "", "")]
    [TestCase(UiLayoutKind.ResponsiveShell, UiOrientation.Horizontal, "grid", "", "", "")]
    public void EveryContainerKindMapsToItsExpectedCssAlgorithm(
        UiLayoutKind kind,
        UiOrientation orientation,
        string display,
        string flexDirection,
        string flexWrap,
        string position)
    {
        UiLayout layout = new UiLayout { Kind = kind, Orientation = orientation };
        DomNode result = new SemanticExpander().ExpandNode(LayoutNode("layout", "layout-id", layout));
        string style = AttributeValue(result, HtmlAttributes.Style);

        Assert.Multiple(() =>
        {
            Assert.That(PropertyValue(style, CssProperties.Display), Is.EqualTo(display));
            Assert.That(PropertyValue(style, CssProperties.FlexDirection), Is.EqualTo(flexDirection));
            Assert.That(PropertyValue(style, CssProperties.FlexWrap), Is.EqualTo(flexWrap));
            Assert.That(PropertyValue(style, CssProperties.Position), Is.EqualTo(position));
            Assert.That(AttributeValue(result, "data-layout"), Is.EqualTo(new LayoutExpander().GetLayoutToken(kind)));
        });
    }

    [TestCase(UiAlignment.Start, "flex-start")]
    [TestCase(UiAlignment.Center, "center")]
    [TestCase(UiAlignment.End, "flex-end")]
    [TestCase(UiAlignment.Stretch, "stretch")]
    [TestCase(UiAlignment.Baseline, "baseline")]
    public void EveryAlignmentMapsToCanonicalCss(UiAlignment alignment, string expected)
    {
        UiLayout layout = new UiLayout { Kind = UiLayoutKind.FlexRow, Alignment = alignment };

        DomNode result = new SemanticExpander().ExpandNode(LayoutNode("alignment", "alignment-id", layout));

        Assert.That(PropertyValue(AttributeValue(result, HtmlAttributes.Style), CssProperties.AlignItems), Is.EqualTo(expected));
    }

    [TestCase(UiJustification.Start, "flex-start")]
    [TestCase(UiJustification.Center, "center")]
    [TestCase(UiJustification.End, "flex-end")]
    [TestCase(UiJustification.SpaceBetween, "space-between")]
    [TestCase(UiJustification.SpaceAround, "space-around")]
    [TestCase(UiJustification.SpaceEvenly, "space-evenly")]
    public void EveryJustificationMapsToCanonicalCss(UiJustification justification, string expected)
    {
        UiLayout layout = new UiLayout { Kind = UiLayoutKind.FlexRow, Justification = justification };

        DomNode result = new SemanticExpander().ExpandNode(LayoutNode("justification", "justification-id", layout));

        Assert.That(PropertyValue(AttributeValue(result, HtmlAttributes.Style), CssProperties.JustifyContent), Is.EqualTo(expected));
    }

    [Test]
    public void RtlDocumentsRetainLogicalSpacingAndPositionProperties()
    {
        UiLayout layout = new UiLayout
        {
            Kind = UiLayoutKind.Flow,
            Margin = new UiSpacing
            {
                InlineStart = Length(1, UiLengthUnit.Rem),
                InlineEnd = Length(2, UiLengthUnit.Rem)
            },
            Padding = new UiSpacing
            {
                InlineStart = Length(3, UiLengthUnit.Rem),
                InlineEnd = Length(4, UiLengthUnit.Rem)
            },
            Position = new UiPosition
            {
                HasInlineStart = true,
                InlineStart = Length(5, UiLengthUnit.Pixel),
                HasInlineEnd = true,
                InlineEnd = Length(6, UiLengthUnit.Pixel)
            }
        };
        UiPage page = new UiPage
        {
            Key = "rtl-page",
            Id = "rtl-page-id",
            TextDirection = "rtl",
            ContentView = new UiView
            {
                Key = "content",
                Id = "content-id",
                ContentRoot = LayoutNode("logical-layout", "logical-layout-id", layout)
            }
        };
        UiWindow window = new UiWindow { Key = "window", Id = "window-id" };

        DomNode document = new DocumentComposer().ComposePageDocument(window, page);
        DomNode html = document.Children[1];
        DomNode expandedLayout = html.Children[1].Children[0].Children[0];
        string style = AttributeValue(expandedLayout, HtmlAttributes.Style);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(html, HtmlAttributes.Direction), Is.EqualTo("rtl"));
            Assert.That(PropertyValue(style, "margin-inline-start"), Is.EqualTo("1rem"));
            Assert.That(PropertyValue(style, "margin-inline-end"), Is.EqualTo("2rem"));
            Assert.That(PropertyValue(style, "padding-inline-start"), Is.EqualTo("3rem"));
            Assert.That(PropertyValue(style, "padding-inline-end"), Is.EqualTo("4rem"));
            Assert.That(PropertyValue(style, "inset-inline-start"), Is.EqualTo("5px"));
            Assert.That(PropertyValue(style, "inset-inline-end"), Is.EqualTo("6px"));
            Assert.That(style, Does.Not.Contain("margin-left:"));
            Assert.That(style, Does.Not.Contain("margin-right:"));
            Assert.That(style, Does.Not.Contain("padding-left:"));
            Assert.That(style, Does.Not.Contain("padding-right:"));
            Assert.That(style, Does.Not.Contain("left:"));
            Assert.That(style, Does.Not.Contain("right:"));
        });
    }

    [Test]
    public void FormatsAutoMinimumMaximumNamedRepeatedAndAutoFitGridTracks()
    {
        UiLayout layout = new UiLayout
        {
            Kind = UiLayoutKind.Grid,
            Columns = new UiGridTrack[]
            {
                new UiGridTrack(),
                new UiGridTrack { Minimum = Length(10, UiLengthUnit.Pixel) },
                new UiGridTrack { Maximum = Length(1, UiLengthUnit.Fraction) },
                new UiGridTrack
                {
                    Name = "main",
                    Minimum = Length(1, UiLengthUnit.Rem),
                    Maximum = Length(2, UiLengthUnit.Fraction),
                    HasRepeatCount = true,
                    RepeatCount = 2
                }
            },
            Rows = new UiGridTrack[] { new UiGridTrack { AutoFit = true } }
        };

        DomNode result = new SemanticExpander().ExpandNode(LayoutNode("grid", "grid-id", layout));
        string style = AttributeValue(result, HtmlAttributes.Style);

        Assert.Multiple(() =>
        {
            Assert.That(PropertyValue(style, CssProperties.GridTemplateColumns), Is.EqualTo("auto minmax(10px, auto) minmax(auto, 1fr) [main] repeat(2, minmax(1rem, 2fr))"));
            Assert.That(PropertyValue(style, CssProperties.GridTemplateRows), Is.EqualTo("repeat(auto-fit, auto)"));
        });
    }

    [Test]
    public void RejectsNullTracksAndEveryInvalidGridStartOrSpan()
    {
        UiNode nullColumn = LayoutNode("null-column", "null-column-id", new UiLayout
        {
            Kind = UiLayoutKind.Grid,
            Columns = new UiGridTrack[] { null! }
        });
        UiNode invalidArea = LayoutNode("invalid-area", "invalid-area-id", new UiLayout
        {
            Kind = UiLayoutKind.Grid,
            GridPlacement = new UiGridPlacement { Area = "two words" }
        });
        UiNode invalidRow = PlacementNode("invalid-row", new UiGridPlacement { HasRow = true, Row = 0 });
        UiNode invalidRowSpan = PlacementNode("invalid-row-span", new UiGridPlacement { HasRowSpan = true, RowSpan = 0 });
        UiNode invalidColumn = PlacementNode("invalid-column", new UiGridPlacement { HasColumn = true, Column = -1 });
        UiNode invalidColumnSpan = PlacementNode("invalid-column-span", new UiGridPlacement { HasColumnSpan = true, ColumnSpan = -1 });
        UiNode invalidTrackLength = LayoutNode("invalid-track-length", "invalid-track-length-id", new UiLayout
        {
            Kind = UiLayoutKind.Grid,
            Rows = new UiGridTrack[] { new UiGridTrack { Minimum = new UiLength { HasValue = true, Value = 1, Unit = UiLengthUnit.Auto } } }
        });
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullColumn));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(invalidArea));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidRow))!.ParamName, Is.EqualTo("row"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidRowSpan))!.ParamName, Is.EqualTo("rowSpan"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidColumn))!.ParamName, Is.EqualTo("column"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidColumnSpan))!.ParamName, Is.EqualTo("columnSpan"));
            Assert.Throws<ArgumentException>(() => expander.ExpandNode(invalidTrackLength));
        });
    }

    [Test]
    public void MapsIndependentOverflowPositionStickySafeAreaAndHorizontalResizeOptions()
    {
        UiLayout scrollLayout = new UiLayout
        {
            Kind = UiLayoutKind.Scroll,
            Scroll = new UiScrollOptions
            {
                Horizontal = UiOverflowMode.Clip,
                Vertical = UiOverflowMode.Auto
            }
        };
        UiLayout absoluteLayout = new UiLayout
        {
            Kind = UiLayoutKind.Flow,
            Position = new UiPosition
            {
                HasBlockEnd = true,
                HasInlineStart = true,
                HasZIndex = true,
                ZIndex = -2
            }
        };
        UiLayout stickySafeLayout = new UiLayout
        {
            Kind = UiLayoutKind.Flow,
            Position = new UiPosition { IsSticky = true },
            UseSafeAreaInsets = true,
            IsResizable = true,
            ResizeOrientation = UiOrientation.Horizontal
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode scroll = expander.ExpandNode(LayoutNode("scroll", "scroll-id", scrollLayout));
        DomNode absolute = expander.ExpandNode(LayoutNode("absolute", "absolute-id", absoluteLayout));
        DomNode stickySafe = expander.ExpandNode(LayoutNode("sticky", "sticky-id", stickySafeLayout));
        string scrollStyle = AttributeValue(scroll, HtmlAttributes.Style);
        string absoluteStyle = AttributeValue(absolute, HtmlAttributes.Style);
        string stickyStyle = AttributeValue(stickySafe, HtmlAttributes.Style);

        Assert.Multiple(() =>
        {
            Assert.That(PropertyValue(scrollStyle, CssProperties.OverflowX), Is.EqualTo("clip"));
            Assert.That(PropertyValue(scrollStyle, CssProperties.OverflowY), Is.EqualTo("auto"));
            Assert.That(AttributeValue(scroll, "data-preserve-scroll"), Is.EqualTo("true"));
            Assert.That(HasAttribute(scroll, "data-scroll-anchor"), Is.False);
            Assert.That(HasAttribute(scroll, "data-reveal-node"), Is.False);
            Assert.That(PropertyValue(absoluteStyle, CssProperties.Position), Is.EqualTo("absolute"));
            Assert.That(PropertyValue(absoluteStyle, "inset-block-end"), Is.EqualTo("auto"));
            Assert.That(PropertyValue(absoluteStyle, "inset-inline-start"), Is.EqualTo("auto"));
            Assert.That(PropertyValue(absoluteStyle, CssProperties.ZIndex), Is.EqualTo("-2"));
            Assert.That(PropertyValue(stickyStyle, CssProperties.Position), Is.EqualTo("sticky"));
            Assert.That(PropertyValue(stickyStyle, "padding-block-start"), Is.EqualTo("env(safe-area-inset-top)"));
            Assert.That(PropertyValue(stickyStyle, "padding-inline-end"), Is.EqualTo("env(safe-area-inset-right)"));
            Assert.That(PropertyValue(stickyStyle, "padding-block-end"), Is.EqualTo("env(safe-area-inset-bottom)"));
            Assert.That(PropertyValue(stickyStyle, "padding-inline-start"), Is.EqualTo("env(safe-area-inset-left)"));
            Assert.That(PropertyValue(stickyStyle, "resize"), Is.EqualTo("horizontal"));
        });
    }

    private static UiNode PlacementNode(string key, UiGridPlacement placement)
    {
        return LayoutNode(key, key + "-id", new UiLayout { Kind = UiLayoutKind.Grid, GridPlacement = placement });
    }

    private static UiNode LayoutNode(string key, string id, UiLayout layout)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Layout,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            Layout = layout
        };
    }

    private static UiLength Length(double value, UiLengthUnit unit)
    {
        return new UiLength { HasValue = true, Value = value, Unit = unit };
    }

    private static string AttributeValue(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index].Value;
            }

            index++;
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    private static string PropertyValue(string style, string property)
    {
        string prefix = property + ":";
        string[] declarations = style.Split(';', StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        while (index < declarations.Length)
        {
            if (declarations[index].StartsWith(prefix, StringComparison.Ordinal))
            {
                return declarations[index].Substring(prefix.Length);
            }

            index++;
        }

        return string.Empty;
    }
}
