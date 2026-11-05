using System;
using System.Globalization;
using htmlgui;

namespace htmlgui.Tests;

public class LayoutOptionExpansionTests
{
    [Test]
    public void ExpandsSizingSpacingAlignmentContainmentAndResizeOptionsCanonically()
    {
        UiLayout layout = new UiLayout
        {
            Kind = UiLayoutKind.FlexRow,
            Alignment = UiAlignment.Baseline,
            Justification = UiJustification.SpaceEvenly,
            Gap = Length(1.5, UiLengthUnit.Rem),
            Padding = new UiSpacing
            {
                BlockStart = Length(1, UiLengthUnit.Pixel),
                InlineEnd = Length(2, UiLengthUnit.Percent),
                BlockEnd = Length(3, UiLengthUnit.Em),
                InlineStart = Length(4, UiLengthUnit.Rem)
            },
            Margin = new UiSpacing
            {
                BlockStart = Length(5, UiLengthUnit.Pixel),
                InlineStart = Length(6, UiLengthUnit.Character)
            },
            Width = new UiLength { Expression = "min(100%, 80rem)" },
            Height = Length(100, UiLengthUnit.ViewportHeight),
            MinimumWidth = Length(20, UiLengthUnit.Rem),
            MaximumWidth = Length(90, UiLengthUnit.Percent),
            MinimumHeight = Length(10, UiLengthUnit.ViewportWidth),
            MaximumHeight = Length(30, UiLengthUnit.Centimeter),
            HasAspectRatio = true,
            AspectRatio = 1.5,
            Wrap = true,
            ContainSize = true,
            ContainLayout = true,
            ContainPaint = true,
            IsResizable = true,
            ResizeOrientation = UiOrientation.Vertical
        };
        UiNode node = LayoutNode(layout);
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(node);
        string style = AttributeValue(result, HtmlAttributes.Style);

        Assert.Multiple(() =>
        {
            Assert.That(style, Does.Contain("align-items:baseline;"));
            Assert.That(style, Does.Contain("aspect-ratio:1.5;"));
            Assert.That(style, Does.Contain("contain:size layout paint;"));
            Assert.That(style, Does.Contain("flex-direction:row;"));
            Assert.That(style, Does.Contain("flex-wrap:wrap;"));
            Assert.That(style, Does.Contain("gap:1.5rem;"));
            Assert.That(style, Does.Contain("height:100vh;"));
            Assert.That(style, Does.Contain("justify-content:space-evenly;"));
            Assert.That(style, Does.Contain("margin-block-start:5px;"));
            Assert.That(style, Does.Contain("margin-inline-start:6ch;"));
            Assert.That(style, Does.Contain("max-height:30cm;"));
            Assert.That(style, Does.Contain("max-width:90%;"));
            Assert.That(style, Does.Contain("min-height:10vw;"));
            Assert.That(style, Does.Contain("min-width:20rem;"));
            Assert.That(style, Does.Contain("padding-block-start:1px;"));
            Assert.That(style, Does.Contain("padding-inline-end:2%;"));
            Assert.That(style, Does.Contain("padding-block-end:3em;"));
            Assert.That(style, Does.Contain("padding-inline-start:4rem;"));
            Assert.That(style, Does.Contain("resize:vertical;"));
            Assert.That(style, Does.Contain("width:min(100%, 80rem);"));
            Assert.That(IsOrdinallySorted(style), Is.True);
        });
    }

    [Test]
    public void ExpandsGridTracksPlacementPositionSafeAreaAndZOrder()
    {
        UiLayout layout = new UiLayout
        {
            Kind = UiLayoutKind.Grid,
            Columns = new UiGridTrack[]
            {
                new UiGridTrack
                {
                    Name = "main",
                    Minimum = Length(10, UiLengthUnit.Rem),
                    Maximum = Length(1, UiLengthUnit.Fraction)
                },
                new UiGridTrack
                {
                    Maximum = Length(100, UiLengthUnit.Pixel),
                    HasRepeatCount = true,
                    RepeatCount = 3
                }
            },
            Rows = new UiGridTrack[]
            {
                new UiGridTrack { Minimum = Length(5, UiLengthUnit.Rem), AutoFit = true }
            },
            GridPlacement = new UiGridPlacement
            {
                Area = "content",
                HasRow = true,
                Row = 2,
                HasRowSpan = true,
                RowSpan = 3,
                HasColumn = true,
                Column = 1,
                HasColumnSpan = true,
                ColumnSpan = 2
            },
            Position = new UiPosition
            {
                IsSticky = true,
                HasBlockStart = true,
                BlockStart = Length(0, UiLengthUnit.Pixel),
                HasInlineEnd = true,
                InlineEnd = Length(2, UiLengthUnit.Rem),
                HasZIndex = true,
                ZIndex = 0
            },
            Padding = new UiSpacing { BlockStart = Length(8, UiLengthUnit.Pixel) },
            UseSafeAreaInsets = true
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(LayoutNode(layout));
        string style = AttributeValue(result, HtmlAttributes.Style);

        Assert.Multiple(() =>
        {
            Assert.That(style, Does.Contain("grid-area:content;"));
            Assert.That(style, Does.Contain("grid-column:1 / span 2;"));
            Assert.That(style, Does.Contain("grid-row:2 / span 3;"));
            Assert.That(style, Does.Contain("grid-template-columns:[main] minmax(10rem, 1fr) repeat(3, minmax(auto, 100px));"));
            Assert.That(style, Does.Contain("grid-template-rows:repeat(auto-fit, minmax(5rem, auto));"));
            Assert.That(style, Does.Contain("inset-block-start:0px;"));
            Assert.That(style, Does.Contain("inset-inline-end:2rem;"));
            Assert.That(style, Does.Contain("padding-block-start:max(8px, env(safe-area-inset-top));"));
            Assert.That(style, Does.Contain("padding-inline-end:env(safe-area-inset-right);"));
            Assert.That(style, Does.Contain("padding-block-end:env(safe-area-inset-bottom);"));
            Assert.That(style, Does.Contain("padding-inline-start:env(safe-area-inset-left);"));
            Assert.That(style, Does.Contain("position:sticky;"));
            Assert.That(style, Does.Contain("z-index:0;"));
        });
    }

    [TestCase(UiOverflowMode.Visible, "visible")]
    [TestCase(UiOverflowMode.Hidden, "hidden")]
    [TestCase(UiOverflowMode.Clip, "clip")]
    [TestCase(UiOverflowMode.Scroll, "scroll")]
    [TestCase(UiOverflowMode.Auto, "auto")]
    public void ExpandsEveryOverflowModeAndRuntimeScrollIntent(UiOverflowMode mode, string token)
    {
        UiLayout layout = new UiLayout
        {
            Kind = UiLayoutKind.Scroll,
            Scroll = new UiScrollOptions
            {
                Horizontal = mode,
                Vertical = mode,
                PreservePosition = false,
                AnchorKey = "item-10",
                RevealNodeId = "node-20",
                Smooth = true
            }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(LayoutNode(layout));
        string style = AttributeValue(result, HtmlAttributes.Style);

        Assert.Multiple(() =>
        {
            Assert.That(style, Does.Contain("overflow-x:" + token + ";"));
            Assert.That(style, Does.Contain("overflow-y:" + token + ";"));
            Assert.That(style, Does.Contain("scroll-behavior:smooth;"));
            Assert.That(AttributeValue(result, "data-preserve-scroll"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, "data-scroll-anchor"), Is.EqualTo("item-10"));
            Assert.That(AttributeValue(result, "data-reveal-node"), Is.EqualTo("node-20"));
            Assert.That(AreAttributeNamesSorted(result.Attributes), Is.True);
        });
    }

    [TestCase(UiLengthUnit.Pixel, "12.5px")]
    [TestCase(UiLengthUnit.Percent, "12.5%")]
    [TestCase(UiLengthUnit.Em, "12.5em")]
    [TestCase(UiLengthUnit.Rem, "12.5rem")]
    [TestCase(UiLengthUnit.ViewportWidth, "12.5vw")]
    [TestCase(UiLengthUnit.ViewportHeight, "12.5vh")]
    [TestCase(UiLengthUnit.Fraction, "12.5fr")]
    [TestCase(UiLengthUnit.Character, "12.5ch")]
    [TestCase(UiLengthUnit.Centimeter, "12.5cm")]
    [TestCase(UiLengthUnit.Millimeter, "12.5mm")]
    [TestCase(UiLengthUnit.Inch, "12.5in")]
    [TestCase(UiLengthUnit.Point, "12.5pt")]
    [TestCase(UiLengthUnit.Pica, "12.5pc")]
    public void FormatsEveryConcreteLengthUnitInvariantly(UiLengthUnit unit, string expected)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        string result;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            result = new LayoutExpander().FormatOptionalLength(Length(12.5, unit));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void RejectsInvalidSizingGridAndEnumOptions()
    {
        LayoutExpander expander = new LayoutExpander();
        UiNode invalidAspect = LayoutNode(new UiLayout { HasAspectRatio = true, AspectRatio = 0 });
        UiNode invalidExpression = LayoutNode(new UiLayout { Width = new UiLength { Expression = "1px;color:red" } });
        UiNode autoNumber = LayoutNode(new UiLayout { Width = new UiLength { HasValue = true, Value = 1, Unit = UiLengthUnit.Auto } });
        UiNode invalidGridIndex = LayoutNode(new UiLayout { GridPlacement = new UiGridPlacement { HasRow = true, Row = 0 } });
        UiNode invalidGridSpan = LayoutNode(new UiLayout { GridPlacement = new UiGridPlacement { HasColumnSpan = true, ColumnSpan = 0 } });
        UiNode invalidRepeat = LayoutNode(new UiLayout { Columns = new UiGridTrack[] { new UiGridTrack { HasRepeatCount = true, RepeatCount = 0 } } });
        UiNode conflictingRepeat = LayoutNode(new UiLayout { Columns = new UiGridTrack[] { new UiGridTrack { HasRepeatCount = true, RepeatCount = 2, AutoFit = true } } });
        UiNode invalidTrackName = LayoutNode(new UiLayout { Columns = new UiGridTrack[] { new UiGridTrack { Name = "bad name" } } });
        UiNode invalidResize = LayoutNode(new UiLayout { IsResizable = true, ResizeOrientation = (UiOrientation)999 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticExpander().ExpandNode(invalidAspect));
            Assert.Throws<ArgumentException>(() => new SemanticExpander().ExpandNode(invalidExpression));
            Assert.Throws<ArgumentException>(() => new SemanticExpander().ExpandNode(autoNumber));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticExpander().ExpandNode(invalidGridIndex));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticExpander().ExpandNode(invalidGridSpan));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticExpander().ExpandNode(invalidRepeat));
            Assert.Throws<UiRenderException>(() => new SemanticExpander().ExpandNode(conflictingRepeat));
            Assert.Throws<UiRenderException>(() => new SemanticExpander().ExpandNode(invalidTrackName));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticExpander().ExpandNode(invalidResize));
            Assert.Throws<ArgumentOutOfRangeException>(() => expander.GetAlignmentToken((UiAlignment)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => expander.GetJustificationToken((UiJustification)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => expander.GetOverflowToken((UiOverflowMode)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => expander.GetOrientationToken((UiOrientation)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => expander.GetUnitSuffix((UiLengthUnit)999));
        });
    }

    private static UiNode LayoutNode(UiLayout layout)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Layout,
            Identity = new UiNodeIdentity { AuthorKey = "layout", SiblingIdentity = "layout", StableId = "layout-id" },
            Layout = layout
        };
    }

    private static UiLength Length(double value, UiLengthUnit unit)
    {
        return new UiLength { HasValue = true, Value = value, Unit = unit };
    }

    private static string AttributeValue(DomNode node, string name)
    {
        DomAttribute? attribute = Array.Find(node.Attributes, item => item.Name == name);
        Assert.That(attribute, Is.Not.Null);
        return attribute!.Value;
    }

    private static bool IsOrdinallySorted(string style)
    {
        string[] declarations = style.Split(';', StringSplitOptions.RemoveEmptyEntries);
        string previous = string.Empty;
        int index = 0;
        while (index < declarations.Length)
        {
            string current = declarations[index].Substring(0, declarations[index].IndexOf(':'));
            if (index > 0 && string.CompareOrdinal(previous, current) > 0)
            {
                return false;
            }

            previous = current;
            index++;
        }

        return true;
    }

    private static bool AreAttributeNamesSorted(DomAttribute[] attributes)
    {
        int index = 1;
        while (index < attributes.Length)
        {
            if (string.CompareOrdinal(attributes[index - 1].Name, attributes[index].Name) > 0)
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
