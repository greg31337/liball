using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxBounds values.
/// </summary>
public class PptxBoundsOperations : IPptxBoundsOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxBoundsOperations()
    {
    }

    /// <summary>
    /// Returns the x-coordinate of the rectangle's right edge in English Metric Units (EMUs).
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long Right(PptxBounds target)
    {
        return target.Right();
    }

    /// <summary>
    /// Returns the y-coordinate of the rectangle's bottom edge in English Metric Units (EMUs).
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long Bottom(PptxBounds target)
    {
        return target.Bottom();
    }

    /// <summary>
    /// Reports whether the bounds is empty.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsEmpty(PptxBounds target)
    {
        return target.IsEmpty();
    }

    /// <summary>
    /// Reports whether the bounds contains the supplied value or coordinates.
    /// </summary>
    /// <param name="x">The x value to use.</param>
    /// <param name="y">The y value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Contains(PptxBounds target, long x, long y)
    {
        return target.Contains(x, y);
    }

    /// <summary>
    /// Reports whether this rectangle overlaps another rectangle with positive area.
    /// </summary>
    /// <param name="other">The other value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Intersects(PptxBounds target, PptxBounds other)
    {
        return target.Intersects(other);
    }

    /// <summary>
    /// Returns the smallest rectangle that contains both rectangles.
    /// </summary>
    /// <param name="other">The other value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds Union(PptxBounds target, PptxBounds other)
    {
        return target.Union(other);
    }

    /// <summary>
    /// Returns a copy translated by the supplied horizontal and vertical distances.
    /// </summary>
    /// <param name="dx">The dx value to use.</param>
    /// <param name="dy">The dy value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds Offset(PptxBounds target, long dx, long dy)
    {
        return target.Offset(dx, dy);
    }

    /// <summary>
    /// Returns a copy with its position and size multiplied by the supplied scale factors.
    /// </summary>
    /// <param name="scaleX">The scale x value to use.</param>
    /// <param name="scaleY">The scale y value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds Scale(PptxBounds target, double scaleX, double scaleY)
    {
        return target.Scale(scaleX, scaleY);
    }

    /// <summary>
    /// Returns the rectangle inside the supplied edge margins.
    /// </summary>
    /// <param name="margins">The edge margins in English Metric Units (EMUs).</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds Inset(PptxBounds target, PptxMargins margins)
    {
        return target.Inset(margins);
    }

    /// <summary>
    /// Returns the rectangle expanded by the supplied edge margins.
    /// </summary>
    /// <param name="margins">The edge margins in English Metric Units (EMUs).</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds Expand(PptxBounds target, PptxMargins margins)
    {
        return target.Expand(margins);
    }

    /// <summary>
    /// Creates bounds by converting inches to English Metric Units (EMUs).
    /// </summary>
    /// <param name="x">The x value to use.</param>
    /// <param name="y">The y value to use.</param>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <returns>The resulting bounds value.</returns>
    public PptxBounds FromInches(double x, double y, double width, double height)
    {
        return PptxBounds.FromInches(x, y, width, height);
    }
}
