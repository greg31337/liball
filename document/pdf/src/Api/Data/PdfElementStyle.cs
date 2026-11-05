using System;

namespace pdf;

/// <summary>
/// Stores reusable layout, background, border, opacity, dash, and item-spacing settings.
/// </summary>
public class PdfElementStyle
{
    /// <summary>
    /// Stores space between the border and content.
    /// </summary>
    public PdfInsets Padding;
    /// <summary>
    /// Stores the four border widths.
    /// </summary>
    public PdfInsets Border;
    /// <summary>
    /// Stores space outside the border.
    /// </summary>
    public PdfInsets Margin;
    /// <summary>
    /// Indicates whether the element background is painted.
    /// </summary>
    public bool HasBackground;
    /// <summary>
    /// Stores the element background color.
    /// </summary>
    public PdfColor BackgroundColor;
    /// <summary>
    /// Specifies background opacity from zero through one.
    /// </summary>
    public double BackgroundOpacity;
    /// <summary>
    /// Stores the element border color.
    /// </summary>
    public PdfColor BorderColor;
    /// <summary>
    /// Specifies border opacity from zero through one.
    /// </summary>
    public double BorderOpacity;
    /// <summary>
    /// Stores alternating painted and unpainted border lengths; an empty array produces a solid border.
    /// </summary>
    public double[] BorderDashPattern;
    /// <summary>
    /// Specifies the distance into the border dash pattern at which painting begins.
    /// </summary>
    public double BorderDashPhase;
    /// <summary>
    /// Specifies spacing between adjacent children in compatible containers.
    /// </summary>
    public double ItemSpacing;

    /// <summary>
    /// Initializes a style with no insets, no background, an opaque black solid border, and no item spacing.
    /// </summary>
    public PdfElementStyle()
    {
        Padding = new PdfInsets(0, 0, 0, 0);
        Border = new PdfInsets(0, 0, 0, 0);
        Margin = new PdfInsets(0, 0, 0, 0);
        HasBackground = false;
        BackgroundColor = new PdfColor("GRAY", 1, 0, 0, 0);
        BackgroundOpacity = 1;
        BorderColor = new PdfColor("GRAY", 0, 0, 0, 0);
        BorderOpacity = 1;
        BorderDashPattern = new double[0];
        BorderDashPhase = 0;
        ItemSpacing = 0;
    }

    internal PdfElementStyle Copy()
    {
        Validate();
        PdfElementStyle copied = new PdfElementStyle();
        copied.Padding = CopyInsets(Padding);
        copied.Border = CopyInsets(Border);
        copied.Margin = CopyInsets(Margin);
        copied.HasBackground = HasBackground;
        copied.BackgroundColor = CopyColor(BackgroundColor);
        copied.BackgroundOpacity = BackgroundOpacity;
        copied.BorderColor = CopyColor(BorderColor);
        copied.BorderOpacity = BorderOpacity;
        copied.BorderDashPattern = CopyDash(BorderDashPattern);
        copied.BorderDashPhase = BorderDashPhase;
        copied.ItemSpacing = ItemSpacing;
        return copied;
    }

    internal void Validate()
    {
        if (Padding == null)
        {
            throw new ArgumentNullException("Padding");
        }

        if (Border == null)
        {
            throw new ArgumentNullException("Border");
        }

        if (Margin == null)
        {
            throw new ArgumentNullException("Margin");
        }

        ValidateInsets(Padding, "Padding");
        ValidateInsets(Border, "Border");
        ValidateInsets(Margin, "Margin");

        if (BackgroundColor == null)
        {
            throw new ArgumentNullException("BackgroundColor");
        }

        if (BorderColor == null)
        {
            throw new ArgumentNullException("BorderColor");
        }

        ValidateColor(BackgroundColor, "BackgroundColor");
        ValidateColor(BorderColor, "BorderColor");

        ValidateOpacity(BackgroundOpacity, "BackgroundOpacity");
        ValidateOpacity(BorderOpacity, "BorderOpacity");
        ValidateFiniteNotNegative(ItemSpacing, "ItemSpacing");
        ValidateFiniteNotNegative(BorderDashPhase, "BorderDashPhase");
        ValidateDash(BorderDashPattern);
    }

    internal void DrawBox(PdfLayoutDrawContext context, PdfLayoutResult result, PdfInsets margin, PdfInsets border)
    {
        double x = context.X + margin.Left;
        double y = context.Y + margin.Bottom;
        double width = result.Size.Width - margin.GetHorizontal();
        double height = result.Size.Height - margin.GetVertical();
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (!HasBackground && border.GetHorizontal() == 0 && border.GetVertical() == 0)
        {
            return;
        }

        PdfCanvas canvas = context.Page.Canvas;
        canvas.SaveState();
        if (HasBackground)
        {
            canvas.SetFillColorValue(BackgroundColor);
            canvas.SetFillOpacity(BackgroundOpacity);
            canvas.FillRectangle(x, y, width, height);
        }

        if (border.GetHorizontal() > 0 || border.GetVertical() > 0)
        {
            canvas.SetStrokeColorValue(BorderColor);
            canvas.SetStrokeOpacity(BorderOpacity);
            if (BorderDashPattern.Length > 0)
            {
                canvas.SetDash(BorderDashPattern, BorderDashPhase);
            }
            else
            {
                canvas.ClearDash();
            }

            DrawBorderLines(canvas, x, y, width, height, border);
        }

        canvas.RestoreState();
    }

    internal void DrawBorderLines(PdfCanvas canvas, double x, double y, double width, double height, PdfInsets border)
    {
        if (border.Bottom > 0)
        {
            canvas.SetLineWidth(border.Bottom);
            canvas.DrawLine(x, y + border.Bottom / 2, x + width, y + border.Bottom / 2);
        }

        if (border.Top > 0)
        {
            canvas.SetLineWidth(border.Top);
            canvas.DrawLine(x, y + height - border.Top / 2, x + width, y + height - border.Top / 2);
        }

        if (border.Left > 0)
        {
            canvas.SetLineWidth(border.Left);
            canvas.DrawLine(x + border.Left / 2, y, x + border.Left / 2, y + height);
        }

        if (border.Right > 0)
        {
            canvas.SetLineWidth(border.Right);
            canvas.DrawLine(x + width - border.Right / 2, y, x + width - border.Right / 2, y + height);
        }
    }

    internal static PdfInsets CopyInsets(PdfInsets value)
    {
        return new PdfInsets(value.Left, value.Top, value.Right, value.Bottom);
    }

    internal static PdfColor CopyColor(PdfColor value)
    {
        return new PdfColor(value.Mode, value.First, value.Second, value.Third, value.Fourth);
    }

    internal static double[] CopyDash(double[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("BorderDashPattern");
        }

        double[] copied = new double[value.Length];
        for (int index = 0; index < value.Length; index++)
        {
            copied[index] = value[index];
        }

        return copied;
    }

    internal static void ValidateDash(double[] pattern)
    {
        if (pattern == null)
        {
            throw new ArgumentNullException("BorderDashPattern");
        }

        bool hasPositive = false;
        for (int index = 0; index < pattern.Length; index++)
        {
            ValidateFiniteNotNegative(pattern[index], "BorderDashPattern");
            if (pattern[index] > 0)
            {
                hasPositive = true;
            }
        }

        if (pattern.Length > 0 && !hasPositive)
        {
            throw new ArgumentException("A non-empty border dash pattern must contain a positive length.", "BorderDashPattern");
        }
    }

    internal static void ValidateOpacity(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(name, "Style opacity must be between zero and one.");
        }
    }

    internal static void ValidateInsets(PdfInsets value, string name)
    {
        if (double.IsNaN(value.Left) || double.IsInfinity(value.Left) || double.IsNaN(value.Top) || double.IsInfinity(value.Top) || double.IsNaN(value.Right) || double.IsInfinity(value.Right) || double.IsNaN(value.Bottom) || double.IsInfinity(value.Bottom))
        {
            throw new ArgumentOutOfRangeException(name, "Style insets must be finite.");
        }
    }

    internal static void ValidateColor(PdfColor value, string name)
    {
        if (value.Mode != "GRAY" && value.Mode != "RGB" && value.Mode != "CMYK")
        {
            throw new ArgumentException("Style colors must use GRAY, RGB, or CMYK mode.", name);
        }

        ValidateColorChannel(value.First, name);
        if (value.Mode == "RGB" || value.Mode == "CMYK")
        {
            ValidateColorChannel(value.Second, name);
            ValidateColorChannel(value.Third, name);
        }

        if (value.Mode == "CMYK")
        {
            ValidateColorChannel(value.Fourth, name);
        }
    }

    internal static void ValidateColorChannel(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(name, "Style color channels must be between zero and one.");
        }
    }

    internal static void ValidateFiniteNotNegative(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Style measurements must be non-negative and finite.");
        }
    }
}
