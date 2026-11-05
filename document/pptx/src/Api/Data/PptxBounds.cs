namespace pptx;

/// <summary>
/// Stores a rectangular position and size in English Metric Units (EMUs).
/// </summary>
public class PptxBounds
{
    /// <summary>
    /// Stores the x in English Metric Units (EMUs).
    /// </summary>
    public long X;
    /// <summary>
    /// Stores the y in English Metric Units (EMUs).
    /// </summary>
    public long Y;
    /// <summary>
    /// Stores the width in English Metric Units (EMUs).
    /// </summary>
    public long Width;
    /// <summary>
    /// Stores the height in English Metric Units (EMUs).
    /// </summary>
    public long Height;

    /// <summary>
    /// Initializes bounds using the supplied position and size.
    /// </summary>
    /// <param name="x">The x value to use.</param>
    /// <param name="y">The y value to use.</param>
    /// <param name="width">The width in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    public PptxBounds(long x, long y, long width, long height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    internal long Right()
    {
        return X + Width;
    }
    internal long Bottom()
    {
        return Y + Height;
    }
    internal bool IsEmpty()
    {
        if (Width <= 0L)
        {
            return true;
        }

        if (Height <= 0L)
        {
            return true;
        }

        return false;
    }
    internal bool Contains(long x, long y)
    {
        if (x < X)
        {
            return false;
        }

        if (y < Y)
        {
            return false;
        }

        if (x > Right())
        {
            return false;
        }

        if (y > Bottom())
        {
            return false;
        }

        return true;
    }
    internal bool Intersects(PptxBounds other)
    {
        if (other == null)
        {
            throw new System.ArgumentNullException("other");
        }

        if (Right() <= other.X)
        {
            return false;
        }

        if (other.Right() <= X)
        {
            return false;
        }

        if (Bottom() <= other.Y)
        {
            return false;
        }

        if (other.Bottom() <= Y)
        {
            return false;
        }

        return true;
    }
    internal PptxBounds Union(PptxBounds other)
    {
        if (other == null)
        {
            throw new System.ArgumentNullException("other");
        }

        long left = Minimum(X, other.X);
        long top = Minimum(Y, other.Y);
        long right = Maximum(Right(), other.Right());
        long bottom = Maximum(Bottom(), other.Bottom());
        return new PptxBounds(left, top, right - left, bottom - top);
    }
    internal PptxBounds Offset(long dx, long dy)
    {
        return new PptxBounds(X + dx, Y + dy, Width, Height);
    }
    internal PptxBounds Scale(double scaleX, double scaleY)
    {
        return new PptxBounds(
            PptxUnitsCore.RoundToLong((double)X * scaleX),
            PptxUnitsCore.RoundToLong((double)Y * scaleY),
            PptxUnitsCore.RoundToLong((double)Width * scaleX),
            PptxUnitsCore.RoundToLong((double)Height * scaleY));
    }
    internal PptxBounds Inset(PptxMargins margins)
    {
        if (margins == null)
        {
            throw new System.ArgumentNullException("margins");
        }

        long width = Width - margins.Left - margins.Right;
        long height = Height - margins.Top - margins.Bottom;
        if (width < 0L || height < 0L)
        {
            throw new System.ArgumentException("Margins cannot exceed the bounds.");
        }

        return new PptxBounds(X + margins.Left, Y + margins.Top, width, height);
    }
    internal PptxBounds Expand(PptxMargins margins)
    {
        if (margins == null)
        {
            throw new System.ArgumentNullException("margins");
        }

        return new PptxBounds(
            X - margins.Left,
            Y - margins.Top,
            Width + margins.Left + margins.Right,
            Height + margins.Top + margins.Bottom);
    }
    internal static PptxBounds FromInches(double x, double y, double width, double height)
    {
        return new PptxBounds(
            PptxUnitsCore.InchesToEmu(x),
            PptxUnitsCore.InchesToEmu(y),
            PptxUnitsCore.InchesToEmu(width),
            PptxUnitsCore.InchesToEmu(height));
    }

    internal static long Minimum(long left, long right)
    {
        if (left < right)
        {
            return left;
        }

        return right;
    }

    internal static long Maximum(long left, long right)
    {
        if (left > right)
        {
            return left;
        }

        return right;
    }
}
