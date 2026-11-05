using System;
using htmlgui;

namespace htmlgui.Tests;

public class LayoutTests
{
    [Test]
    public void LayoutModelsHaveSafeDefaults()
    {
        UiLayout layout = new UiLayout();
        UiLength length = new UiLength();
        UiSpacing spacing = new UiSpacing();
        UiGridTrack track = new UiGridTrack();
        UiGridPlacement placement = new UiGridPlacement();
        UiPosition position = new UiPosition();
        UiScrollOptions scroll = new UiScrollOptions();

        Assert.Multiple(() =>
        {
            Assert.That(layout.Kind, Is.EqualTo(UiLayoutKind.Flow));
            Assert.That(layout.Orientation, Is.EqualTo(UiOrientation.Horizontal));
            Assert.That(layout.Alignment, Is.EqualTo(UiAlignment.Start));
            Assert.That(layout.Justification, Is.EqualTo(UiJustification.Start));
            Assert.That(layout.Gap, Is.Not.Null);
            Assert.That(layout.Padding, Is.Not.Null);
            Assert.That(layout.Margin, Is.Not.Null);
            Assert.That(layout.Width, Is.Null);
            Assert.That(layout.Height, Is.Null);
            Assert.That(layout.MinimumWidth, Is.Null);
            Assert.That(layout.MaximumWidth, Is.Null);
            Assert.That(layout.MinimumHeight, Is.Null);
            Assert.That(layout.MaximumHeight, Is.Null);
            Assert.That(layout.HasAspectRatio, Is.False);
            Assert.That(layout.Columns, Is.Empty);
            Assert.That(layout.Rows, Is.Empty);
            Assert.That(layout.GridPlacement, Is.Null);
            Assert.That(layout.Position, Is.Null);
            Assert.That(layout.Scroll, Is.Null);
            Assert.That(layout.Children, Is.Empty);
            Assert.That(layout.Wrap, Is.False);
            Assert.That(layout.ContainSize, Is.False);
            Assert.That(layout.ContainLayout, Is.False);
            Assert.That(layout.ContainPaint, Is.False);
            Assert.That(layout.UseSafeAreaInsets, Is.False);
            Assert.That(layout.IsResizable, Is.False);
            Assert.That(layout.ResizeOrientation, Is.EqualTo(UiOrientation.Horizontal));
            Assert.That(length.HasValue, Is.False);
            Assert.That(length.Value, Is.Zero);
            Assert.That(length.Unit, Is.EqualTo(UiLengthUnit.Auto));
            Assert.That(length.Expression, Is.Null);
            Assert.That(spacing.BlockStart, Is.Not.SameAs(spacing.InlineEnd));
            Assert.That(spacing.BlockEnd, Is.Not.SameAs(spacing.InlineStart));
            Assert.That(track.Minimum, Is.Not.SameAs(track.Maximum));
            Assert.That(track.HasRepeatCount, Is.False);
            Assert.That(placement.Area, Is.Null);
            Assert.That(placement.HasRow, Is.False);
            Assert.That(placement.HasColumn, Is.False);
            Assert.That(position.HasZIndex, Is.False);
            Assert.That(position.IsSticky, Is.False);
            Assert.That(scroll.Horizontal, Is.EqualTo(UiOverflowMode.Visible));
            Assert.That(scroll.Vertical, Is.EqualTo(UiOverflowMode.Visible));
            Assert.That(scroll.PreservePosition, Is.True);
        });
    }

    [Test]
    public void PopulatedLayoutRetainsSizingPlacementAndScrollOptions()
    {
        UiLength gap = Length(8, UiLengthUnit.Pixel);
        UiGridTrack column = new UiGridTrack
        {
            Name = "content",
            Minimum = Length(12, UiLengthUnit.Rem),
            Maximum = Length(1, UiLengthUnit.Fraction),
            HasRepeatCount = true,
            RepeatCount = 0,
            AutoFit = true
        };
        UiGridPlacement placement = new UiGridPlacement
        {
            Area = "main",
            HasRow = true,
            Row = 1,
            HasRowSpan = true,
            RowSpan = 2,
            HasColumn = true,
            Column = 1,
            HasColumnSpan = true,
            ColumnSpan = 3
        };
        UiPosition position = new UiPosition
        {
            HasBlockStart = true,
            BlockStart = Length(0, UiLengthUnit.Pixel),
            HasInlineEnd = true,
            InlineEnd = Length(2, UiLengthUnit.Rem),
            HasBlockEnd = true,
            BlockEnd = Length(1, UiLengthUnit.Pixel),
            HasInlineStart = true,
            InlineStart = Length(3, UiLengthUnit.Percent),
            HasZIndex = true,
            ZIndex = 0,
            IsSticky = true
        };
        UiScrollOptions scroll = new UiScrollOptions
        {
            Horizontal = UiOverflowMode.Hidden,
            Vertical = UiOverflowMode.Auto,
            PreservePosition = false,
            AnchorKey = "row-10",
            RevealNodeId = "ui-row-20",
            Smooth = true
        };
        UiNode child = new UiNode { Name = "main" };
        UiLayout layout = new UiLayout
        {
            Kind = UiLayoutKind.Grid,
            Orientation = UiOrientation.Vertical,
            Alignment = UiAlignment.Stretch,
            Justification = UiJustification.SpaceBetween,
            Gap = gap,
            Padding = new UiSpacing { BlockStart = Length(1, UiLengthUnit.Rem) },
            Margin = new UiSpacing { InlineStart = Length(2, UiLengthUnit.Percent) },
            Width = new UiLength { Expression = "min(100%, 80rem)" },
            Height = Length(100, UiLengthUnit.ViewportHeight),
            MinimumWidth = Length(20, UiLengthUnit.Rem),
            MaximumWidth = Length(90, UiLengthUnit.Percent),
            MinimumHeight = Length(10, UiLengthUnit.Rem),
            MaximumHeight = Length(80, UiLengthUnit.ViewportHeight),
            HasAspectRatio = true,
            AspectRatio = 1.5,
            Columns = new UiGridTrack[] { column },
            Rows = new UiGridTrack[] { new UiGridTrack() },
            GridPlacement = placement,
            Position = position,
            Scroll = scroll,
            Children = new UiNode[] { child },
            Wrap = true,
            ContainSize = true,
            ContainLayout = true,
            ContainPaint = true,
            UseSafeAreaInsets = true,
            IsResizable = true,
            ResizeOrientation = UiOrientation.Vertical
        };

        Assert.Multiple(() =>
        {
            Assert.That(layout.Kind, Is.EqualTo(UiLayoutKind.Grid));
            Assert.That(layout.Gap, Is.SameAs(gap));
            Assert.That(layout.Width?.Expression, Is.EqualTo("min(100%, 80rem)"));
            Assert.That(layout.MinimumWidth?.Value, Is.EqualTo(20));
            Assert.That(layout.MaximumWidth?.Unit, Is.EqualTo(UiLengthUnit.Percent));
            Assert.That(layout.MinimumHeight?.Unit, Is.EqualTo(UiLengthUnit.Rem));
            Assert.That(layout.MaximumHeight?.Value, Is.EqualTo(80));
            Assert.That(layout.HasAspectRatio, Is.True);
            Assert.That(layout.AspectRatio, Is.EqualTo(1.5));
            Assert.That(layout.Columns[0], Is.SameAs(column));
            Assert.That(column.HasRepeatCount, Is.True);
            Assert.That(column.RepeatCount, Is.Zero);
            Assert.That(placement.RowSpan, Is.EqualTo(2));
            Assert.That(placement.ColumnSpan, Is.EqualTo(3));
            Assert.That(position.HasBlockStart, Is.True);
            Assert.That(position.BlockStart.Value, Is.Zero);
            Assert.That(position.HasZIndex, Is.True);
            Assert.That(position.ZIndex, Is.Zero);
            Assert.That(scroll.Vertical, Is.EqualTo(UiOverflowMode.Auto));
            Assert.That(scroll.PreservePosition, Is.False);
            Assert.That(scroll.AnchorKey, Is.EqualTo("row-10"));
            Assert.That(layout.Children[0], Is.SameAs(child));
            Assert.That(layout.ContainSize, Is.True);
            Assert.That(layout.ContainLayout, Is.True);
            Assert.That(layout.ContainPaint, Is.True);
            Assert.That(layout.UseSafeAreaInsets, Is.True);
            Assert.That(layout.IsResizable, Is.True);
            Assert.That(layout.ResizeOrientation, Is.EqualTo(UiOrientation.Vertical));
        });
    }

    private static UiLength Length(double value, UiLengthUnit unit)
    {
        return new UiLength { HasValue = true, Value = value, Unit = unit };
    }
}
