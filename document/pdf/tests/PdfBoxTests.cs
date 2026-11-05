using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfBoxTests
{
    [Test]
    public void Insets_CalculateHorizontalAndVerticalTotals()
    {
        PdfInsets insets = new PdfInsets(1, 2, 3, 4);
        PdfInsets uniform = PdfInsets.Uniform(5);
        PdfInsets verticalHorizontal = PdfInsets.VerticalHorizontal(6, 7);

        Assert.That(insets.GetHorizontal(), Is.EqualTo(4));
        Assert.That(insets.GetVertical(), Is.EqualTo(6));
        Assert.That(uniform.Left, Is.EqualTo(5));
        Assert.That(uniform.Top, Is.EqualTo(5));
        Assert.That(uniform.Right, Is.EqualTo(5));
        Assert.That(uniform.Bottom, Is.EqualTo(5));
        Assert.That(verticalHorizontal.Left, Is.EqualTo(7));
        Assert.That(verticalHorizontal.Top, Is.EqualTo(6));
        Assert.That(verticalHorizontal.Right, Is.EqualTo(7));
        Assert.That(verticalHorizontal.Bottom, Is.EqualTo(6));
    }

    [Test]
    public void Box_ExpandsContentToPaddingBorderAndMarginRectangles()
    {
        PdfRectangle content = new PdfRectangle(50, 60, 100, 40);
        PdfInsets padding = new PdfInsets(5, 10, 15, 20);
        PdfInsets border = PdfInsets.Uniform(2);
        PdfInsets margin = new PdfInsets(1, 3, 5, 7);

        PdfBox box = new PdfBox(content, padding, border, margin);
        PdfRectangle paddingRectangle = box.GetPaddingRectangle();
        PdfRectangle borderRectangle = box.GetBorderRectangle();
        PdfRectangle marginRectangle = box.GetMarginRectangle();

        AssertRectangle(paddingRectangle, 45, 40, 120, 70);
        AssertRectangle(borderRectangle, 43, 38, 124, 74);
        AssertRectangle(marginRectangle, 42, 31, 130, 84);
        Assert.That(box.GetOuterWidth(), Is.EqualTo(130));
        Assert.That(box.GetOuterHeight(), Is.EqualTo(84));
        Assert.That(box.GetContentSize().Width, Is.EqualTo(100));
        Assert.That(box.GetContentSize().Height, Is.EqualTo(40));
        Assert.That(box.GetOuterSize().Width, Is.EqualTo(130));
        Assert.That(box.GetOuterSize().Height, Is.EqualTo(84));
    }

    [Test]
    public void Box_FromMarginRectangleShrinksToContentRectangle()
    {
        PdfRectangle marginRectangle = new PdfRectangle(42, 31, 130, 84);
        PdfInsets padding = new PdfInsets(5, 10, 15, 20);
        PdfInsets border = PdfInsets.Uniform(2);
        PdfInsets margin = new PdfInsets(1, 3, 5, 7);

        PdfBox box = PdfBox.FromMarginRectangle(marginRectangle, padding, border, margin);

        AssertRectangle(box.ContentRectangle, 50, 60, 100, 40);
        AssertRectangle(box.GetMarginRectangle(), 42, 31, 130, 84);
    }

    [Test]
    public void Box_FromCoordinatesUsesContentRectangleOnly()
    {
        PdfBox box = PdfBox.FromCoordinates(10, 20, 30, 40);

        AssertRectangle(box.ContentRectangle, 10, 20, 30, 40);
        AssertRectangle(box.GetPaddingRectangle(), 10, 20, 30, 40);
        AssertRectangle(box.GetBorderRectangle(), 10, 20, 30, 40);
        AssertRectangle(box.GetMarginRectangle(), 10, 20, 30, 40);
    }

    [Test]
    public void LayoutSize_AllowsZeroAndRejectsNegativeDimensions()
    {
        PdfLayoutSize size = new PdfLayoutSize(0, 0);

        Assert.That(size.Width, Is.EqualTo(0));
        Assert.That(size.Height, Is.EqualTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutSize(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutSize(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutSize(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutSize(0, double.PositiveInfinity));
    }

    [Test]
    public void BoxModel_RejectsInvalidInputs()
    {
        PdfRectangle content = new PdfRectangle(0, 0, 10, 10);
        PdfInsets none = PdfInsets.None();

        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInsets(-1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInsets(double.NaN, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInsets(0, 0, double.PositiveInfinity, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfRectangle(double.NaN, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfRectangle(0, double.NegativeInfinity, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfRectangle(0, 0, double.PositiveInfinity, 1));
        Assert.Throws<ArgumentNullException>(() => PdfBox.FromContentRectangle(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfBox(content, null!, none, none));
        Assert.Throws<ArgumentNullException>(() => new PdfBox(content, none, null!, none));
        Assert.Throws<ArgumentNullException>(() => new PdfBox(content, none, none, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfBox.FromContentRectangle(new PdfRectangle(0, 0, -1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfBox.FromContentRectangle(new PdfRectangle(0, 0, 1, -1)));
        Assert.Throws<ArgumentException>(() => PdfBox.FromMarginRectangle(new PdfRectangle(0, 0, 10, 10), PdfInsets.Uniform(6), none, none));
        Assert.Throws<ArgumentException>(() => PdfBox.FromMarginRectangle(new PdfRectangle(0, 0, 10, 10), none, PdfInsets.Uniform(6), none));
    }

    static void AssertRectangle(PdfRectangle rectangle, double x, double y, double width, double height)
    {
        Assert.That(rectangle.X, Is.EqualTo(x).Within(0.0001));
        Assert.That(rectangle.Y, Is.EqualTo(y).Within(0.0001));
        Assert.That(rectangle.Width, Is.EqualTo(width).Within(0.0001));
        Assert.That(rectangle.Height, Is.EqualTo(height).Within(0.0001));
    }
}
