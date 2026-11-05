using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace pdf;

/// <summary>
/// Records low-level PDF drawing commands for text, paths, images, colors, and coordinate transforms.
/// </summary>
/// <remarks>
/// Coordinates are expressed in PDF points. Drawing calls append commands in order, so graphics-state and transform operations affect subsequent content.
/// </remarks>
public class PdfCanvas : IPdfCanvas
{
    internal StringBuilder _content = new StringBuilder();
    internal List<PdfFont> _fonts = new List<PdfFont>();
    internal List<PdfGraphicsState> _graphicsStates = new List<PdfGraphicsState>();
    internal List<PdfTrueTypeFontUsage> _trueTypeFontUsages = new List<PdfTrueTypeFontUsage>();
    internal List<PdfAccessibilityNode> _markedContentNodes = new List<PdfAccessibilityNode>();
    internal bool TextObjectOpen;
    internal bool HasCurrentTrueTypeFont;
    internal PdfTrueTypeFont? CurrentTrueTypeFont;
    internal bool UsesDeviceRgb;
    internal bool UsesDeviceCmyk;
    internal int MarkedContentDepth;
    internal int GraphicsStateDepth;
    /// <summary>
    /// Exposes the fonts currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfFont> Fonts;
    internal IReadOnlyList<PdfGraphicsState> GraphicsStates;
    internal IReadOnlyList<PdfTrueTypeFontUsage> TrueTypeFontUsages;
    internal IReadOnlyList<PdfAccessibilityNode> MarkedContentNodes;

    /// <summary>
    /// Initializes a new canvas.
    /// </summary>
    public PdfCanvas()
    {
        Fonts = _fonts;
        GraphicsStates = _graphicsStates;
        TrueTypeFontUsages = _trueTypeFontUsages;
        MarkedContentNodes = _markedContentNodes;
        TextObjectOpen = false;
        HasCurrentTrueTypeFont = false;
        CurrentTrueTypeFont = null;
        UsesDeviceRgb = false;
        UsesDeviceCmyk = false;
        MarkedContentDepth = 0;
        GraphicsStateDepth = 0;
    }

    /// <summary>
    /// Pushes the current PDF graphics state so later changes can be undone with RestoreState.
    /// </summary>
    public void SaveState()
    {
        EnsureTextObjectClosed();
        _content.AppendLine("q");
        GraphicsStateDepth++;
    }

    /// <summary>
    /// Restores the most recently saved PDF graphics state.
    /// </summary>
    public void RestoreState()
    {
        EnsureTextObjectClosed();
        if (GraphicsStateDepth == 0)
        {
            throw new InvalidOperationException("No saved PDF graphics state is available to restore.");
        }

        _content.AppendLine("Q");
        GraphicsStateDepth--;
    }

    /// <summary>
    /// Sets the opacity used by subsequent fill operations.
    /// </summary>
    /// <param name="opacity">The normalized opacity from zero for transparent through one for opaque.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when opacity is outside the supported range.</exception>
    public void SetFillOpacity(double opacity)
    {
        ValidateOpacity(opacity, "opacity");
        AddGraphicsState(true, opacity, false, 1, false, PdfBlendMode.Normal);
    }

    /// <summary>
    /// Sets the opacity used by subsequent stroke operations.
    /// </summary>
    /// <param name="opacity">The normalized opacity from zero for transparent through one for opaque.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when opacity is outside the supported range.</exception>
    public void SetStrokeOpacity(double opacity)
    {
        ValidateOpacity(opacity, "opacity");
        AddGraphicsState(false, 1, true, opacity, false, PdfBlendMode.Normal);
    }

    /// <summary>
    /// Sets the blend mode used by subsequent painting operations.
    /// </summary>
    /// <param name="blendMode">The blend mode to apply.</param>
    /// <exception cref="ArgumentException">Thrown when the blend mode is not supported.</exception>
    public void SetBlendMode(PdfBlendMode blendMode)
    {
        ValidateBlendMode(blendMode);
        AddGraphicsState(false, 1, false, 1, true, blendMode);
    }

    /// <summary>
    /// Applies fill opacity, stroke opacity, and blend mode in one transparency graphics state.
    /// </summary>
    /// <param name="fillOpacity">The normalized fill opacity.</param>
    /// <param name="strokeOpacity">The normalized stroke opacity.</param>
    /// <param name="blendMode">The blend mode to apply.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an opacity is outside the supported range.</exception>
    /// <exception cref="ArgumentException">Thrown when the blend mode is not supported.</exception>
    public void SetTransparency(double fillOpacity, double strokeOpacity, PdfBlendMode blendMode)
    {
        ValidateOpacity(fillOpacity, "fillOpacity");
        ValidateOpacity(strokeOpacity, "strokeOpacity");
        ValidateBlendMode(blendMode);
        AddGraphicsState(true, fillOpacity, true, strokeOpacity, true, blendMode);
    }

    /// <summary>
    /// Sets the line width used for subsequent PDF generation.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    public void SetLineWidth(double width)
    {
        ValidatePositiveFinite(width, "width", "Line width must be positive.");
        _content.AppendLine(Number(width) + " w");
    }

    /// <summary>
    /// Sets the line cap used for subsequent PDF generation.
    /// </summary>
    /// <param name="lineCap">The line cap to use for this operation.</param>
    public void SetLineCap(PdfLineCap lineCap)
    {
        ValidateLineCap(lineCap);
        _content.AppendLine(((int)lineCap).ToString(CultureInfo.InvariantCulture) + " J");
    }

    /// <summary>
    /// Sets the line join used for subsequent PDF generation.
    /// </summary>
    /// <param name="lineJoin">The line join to use for this operation.</param>
    public void SetLineJoin(PdfLineJoin lineJoin)
    {
        ValidateLineJoin(lineJoin);
        _content.AppendLine(((int)lineJoin).ToString(CultureInfo.InvariantCulture) + " j");
    }

    /// <summary>
    /// Sets the dash used for subsequent PDF generation.
    /// </summary>
    /// <param name="pattern">The pattern to use for this operation.</param>
    /// <param name="phase">The phase to use for this operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public void SetDash(double[] pattern, double phase)
    {
        if (pattern == null)
        {
            throw new ArgumentNullException("pattern");
        }

        if (double.IsNaN(phase) || double.IsInfinity(phase) || phase < 0)
        {
            throw new ArgumentOutOfRangeException("phase", "Dash phase must be finite and non-negative.");
        }

        for (int index = 0; index < pattern.Length; index++)
        {
            if (double.IsNaN(pattern[index]) || double.IsInfinity(pattern[index]) || pattern[index] < 0)
            {
                throw new ArgumentOutOfRangeException("pattern", "Dash pattern values must be finite and non-negative.");
            }
        }

        _content.Append("[");
        for (int index = 0; index < pattern.Length; index++)
        {
            if (index > 0)
            {
                _content.Append(" ");
            }

            _content.Append(Number(pattern[index]));
        }

        _content.AppendLine("] " + Number(phase) + " d");
    }

    /// <summary>
    /// Clears the current dash.
    /// </summary>
    public void ClearDash()
    {
        _content.AppendLine("[] 0 d");
    }

    /// <summary>
    /// Concatenates the supplied six-value matrix with the canvas's current transformation matrix.
    /// </summary>
    /// <param name="a">The a to use for this operation.</param>
    /// <param name="b">The b to use for this operation.</param>
    /// <param name="c">The c to use for this operation.</param>
    /// <param name="d">The d to use for this operation.</param>
    /// <param name="e">The e to use for this operation.</param>
    /// <param name="f">The f to use for this operation.</param>
    public void Transform(double a, double b, double c, double d, double e, double f)
    {
        ValidateFinite(a, "a");
        ValidateFinite(b, "b");
        ValidateFinite(c, "c");
        ValidateFinite(d, "d");
        ValidateFinite(e, "e");
        ValidateFinite(f, "f");
        _content.AppendLine(Number(a) + " " + Number(b) + " " + Number(c) + " " + Number(d) + " " + Number(e) + " " + Number(f) + " cm");
    }

    /// <summary>
    /// Offsets the coordinate system used by subsequent drawing commands.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    public void Translate(double x, double y)
    {
        Transform(1, 0, 0, 1, x, y);
    }

    /// <summary>
    /// Scales the coordinate system used by subsequent drawing commands.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    public void Scale(double x, double y)
    {
        Transform(x, 0, 0, y, 0, 0);
    }

    /// <summary>
    /// Rotates the coordinate system used by subsequent drawing commands.
    /// </summary>
    /// <param name="degrees">The clockwise rotation angle in degrees.</param>
    public void Rotate(double degrees)
    {
        ValidateFinite(degrees, "degrees");
        double radians = degrees * Math.PI / 180;
        double cosine = Math.Cos(radians);
        double sine = Math.Sin(radians);

        Transform(cosine, sine, -sine, cosine, 0, 0);
    }

    /// <summary>
    /// Skews the coordinate system used by subsequent drawing commands.
    /// </summary>
    /// <param name="xDegrees">The x degrees to use for this operation.</param>
    /// <param name="yDegrees">The y degrees to use for this operation.</param>
    public void Skew(double xDegrees, double yDegrees)
    {
        ValidateFinite(xDegrees, "xDegrees");
        ValidateFinite(yDegrees, "yDegrees");
        double xRadians = xDegrees * Math.PI / 180;
        double yRadians = yDegrees * Math.PI / 180;

        Transform(1, Math.Tan(yRadians), Math.Tan(xRadians), 1, 0, 0);
    }

    /// <summary>
    /// Sets the stroke color used for subsequent PDF generation.
    /// </summary>
    /// <param name="red">The red to use for this operation.</param>
    /// <param name="green">The green to use for this operation.</param>
    /// <param name="blue">The blue to use for this operation.</param>
    public void SetStrokeColor(double red, double green, double blue)
    {
        SetStrokeColorValue(PdfColor.Rgb(red, green, blue));
    }

    /// <summary>
    /// Sets the stroke color used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    public void SetStrokeColorValue(PdfColor color)
    {
        if (color == null)
        {
            throw new ArgumentNullException("color");
        }

        if (color.Mode == "GRAY")
        {
            _content.AppendLine(Number(color.First) + " G");
        }
        else if (color.Mode == "CMYK")
        {
            UsesDeviceCmyk = true;
            _content.AppendLine(Number(color.First) + " " + Number(color.Second) + " " + Number(color.Third) + " " + Number(color.Fourth) + " K");
        }
        else
        {
            UsesDeviceRgb = true;
            _content.AppendLine(Number(color.First) + " " + Number(color.Second) + " " + Number(color.Third) + " RG");
        }
    }

    /// <summary>
    /// Sets the fill color used for subsequent PDF generation.
    /// </summary>
    /// <param name="red">The red to use for this operation.</param>
    /// <param name="green">The green to use for this operation.</param>
    /// <param name="blue">The blue to use for this operation.</param>
    public void SetFillColor(double red, double green, double blue)
    {
        SetFillColorValue(PdfColor.Rgb(red, green, blue));
    }

    /// <summary>
    /// Sets the fill color used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    public void SetFillColorValue(PdfColor color)
    {
        if (color == null)
        {
            throw new ArgumentNullException("color");
        }

        if (color.Mode == "GRAY")
        {
            _content.AppendLine(Number(color.First) + " g");
        }
        else if (color.Mode == "CMYK")
        {
            UsesDeviceCmyk = true;
            _content.AppendLine(Number(color.First) + " " + Number(color.Second) + " " + Number(color.Third) + " " + Number(color.Fourth) + " k");
        }
        else
        {
            UsesDeviceRgb = true;
            _content.AppendLine(Number(color.First) + " " + Number(color.Second) + " " + Number(color.Third) + " rg");
        }
    }

    /// <summary>
    /// Appends drawing commands for a line to the target PDF canvas.
    /// </summary>
    /// <param name="x1">The first horizontal coordinate in PDF points.</param>
    /// <param name="y1">The first vertical coordinate in PDF points.</param>
    /// <param name="x2">The second horizontal coordinate in PDF points.</param>
    /// <param name="y2">The second vertical coordinate in PDF points.</param>
    public void DrawLine(double x1, double y1, double x2, double y2)
    {
        ValidateFinite(x1, "x1");
        ValidateFinite(y1, "y1");
        ValidateFinite(x2, "x2");
        ValidateFinite(y2, "y2");
        _content.AppendLine(Number(x1) + " " + Number(y1) + " m " + Number(x2) + " " + Number(y2) + " l S");
    }

    /// <summary>
    /// Draws a horizontal rule using the current stroke settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate of the rule's left endpoint.</param>
    /// <param name="y">The vertical coordinate of the rule.</param>
    /// <param name="width">The positive rule width in PDF points.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when width is not positive.</exception>
    public void DrawHorizontalRule(double x, double y, double width)
    {
        ValidatePositiveFinite(width, "width", "Rule width must be positive.");
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        DrawLine(x, y, x + width, y);
    }

    /// <summary>
    /// Draws a vertical rule using the current stroke settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate of the rule.</param>
    /// <param name="y">The vertical coordinate of the rule's bottom endpoint.</param>
    /// <param name="height">The positive rule height in PDF points.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when height is not positive.</exception>
    public void DrawVerticalRule(double x, double y, double height)
    {
        ValidatePositiveFinite(height, "height", "Rule height must be positive.");
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        DrawLine(x, y, x, y + height);
    }

    /// <summary>
    /// Strokes a path using the canvas's current line settings.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    public void StrokePath(PdfPath path)
    {
        WritePath(path);
        _content.AppendLine("S");
    }

    /// <summary>
    /// Fills a path using the canvas's current fill settings.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    public void FillPath(PdfPath path)
    {
        WritePath(path);
        _content.AppendLine("f");
    }

    /// <summary>
    /// Fills and strokes a path using the canvas's current color and line settings.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    public void FillStrokePath(PdfPath path)
    {
        WritePath(path);
        _content.AppendLine("B");
    }

    internal void FillPathEvenOdd(PdfPath path)
    {
        WritePath(path);
        _content.AppendLine("f*");
    }

    internal void FillStrokePathEvenOdd(PdfPath path)
    {
        WritePath(path);
        _content.AppendLine("B*");
    }

    /// <summary>
    /// Restricts subsequent drawing to a path until the graphics state is restored.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    public void ClipPath(PdfPath path)
    {
        WritePath(path);
        _content.AppendLine("W");
        _content.AppendLine("n");
    }

    /// <summary>
    /// Restricts subsequent drawing to a path even odd until the graphics state is restored.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    public void ClipPathEvenOdd(PdfPath path)
    {
        WritePath(path);
        _content.AppendLine("W*");
        _content.AppendLine("n");
    }

    /// <summary>
    /// Restricts subsequent drawing to a rectangle until the graphics state is restored.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    public void ClipRectangle(double x, double y, double width, double height)
    {
        ValidateRectangle(width, height);
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        _content.AppendLine(Number(x) + " " + Number(y) + " " + Number(width) + " " + Number(height) + " re");
        _content.AppendLine("W");
        _content.AppendLine("n");
    }

    /// <summary>
    /// Strokes a circle using the canvas's current line settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    public void StrokeCircle(double centerX, double centerY, double radius)
    {
        StrokePath(PdfPath.Circle(centerX, centerY, radius));
    }

    /// <summary>
    /// Fills a circle using the canvas's current fill settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    public void FillCircle(double centerX, double centerY, double radius)
    {
        FillPath(PdfPath.Circle(centerX, centerY, radius));
    }

    /// <summary>
    /// Fills and strokes a circle using the canvas's current color and line settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    public void FillStrokeCircle(double centerX, double centerY, double radius)
    {
        FillStrokePath(PdfPath.Circle(centerX, centerY, radius));
    }

    /// <summary>
    /// Strokes an ellipse using the canvas's current line settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radiusX">The horizontal radius in PDF points; it must be positive.</param>
    /// <param name="radiusY">The vertical radius in PDF points; it must be positive.</param>
    public void StrokeEllipse(double centerX, double centerY, double radiusX, double radiusY)
    {
        StrokePath(PdfPath.Ellipse(centerX, centerY, radiusX, radiusY));
    }

    /// <summary>
    /// Fills an ellipse using the canvas's current fill settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radiusX">The horizontal radius in PDF points; it must be positive.</param>
    /// <param name="radiusY">The vertical radius in PDF points; it must be positive.</param>
    public void FillEllipse(double centerX, double centerY, double radiusX, double radiusY)
    {
        FillPath(PdfPath.Ellipse(centerX, centerY, radiusX, radiusY));
    }

    /// <summary>
    /// Fills and strokes an ellipse using the canvas's current color and line settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radiusX">The horizontal radius in PDF points; it must be positive.</param>
    /// <param name="radiusY">The vertical radius in PDF points; it must be positive.</param>
    public void FillStrokeEllipse(double centerX, double centerY, double radiusX, double radiusY)
    {
        FillStrokePath(PdfPath.Ellipse(centerX, centerY, radiusX, radiusY));
    }

    /// <summary>
    /// Strokes a rounded rectangle using the canvas's current line settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    public void StrokeRoundedRectangle(double x, double y, double width, double height, double radius)
    {
        StrokePath(PdfPath.RoundedRectangle(x, y, width, height, radius));
    }

    /// <summary>
    /// Fills a rounded rectangle using the canvas's current fill settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    public void FillRoundedRectangle(double x, double y, double width, double height, double radius)
    {
        FillPath(PdfPath.RoundedRectangle(x, y, width, height, radius));
    }

    /// <summary>
    /// Fills and strokes a rounded rectangle using the canvas's current color and line settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    public void FillStrokeRoundedRectangle(double x, double y, double width, double height, double radius)
    {
        FillStrokePath(PdfPath.RoundedRectangle(x, y, width, height, radius));
    }

    /// <summary>
    /// Strokes a rectangle using the canvas's current line settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    public void StrokeRectangle(double x, double y, double width, double height)
    {
        ValidateRectangle(width, height);
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        _content.AppendLine(Number(x) + " " + Number(y) + " " + Number(width) + " " + Number(height) + " re S");
    }

    /// <summary>
    /// Fills a rectangle using the canvas's current fill settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    public void FillRectangle(double x, double y, double width, double height)
    {
        ValidateRectangle(width, height);
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        _content.AppendLine(Number(x) + " " + Number(y) + " " + Number(width) + " " + Number(height) + " re f");
    }

    /// <summary>
    /// Begins a caller-managed PDF text object.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a text object is already open.</exception>
    public void BeginText()
    {
        if (TextObjectOpen)
        {
            throw new InvalidOperationException("A PDF text object is already open.");
        }

        _content.AppendLine("BT");
        TextObjectOpen = true;
        HasCurrentTrueTypeFont = false;
        CurrentTrueTypeFont = null;
    }

    /// <summary>
    /// Ends the current caller-managed PDF text object.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no text object is open.</exception>
    public void EndText()
    {
        EnsureTextObjectOpen();
        _content.AppendLine("ET");
        TextObjectOpen = false;
        HasCurrentTrueTypeFont = false;
        CurrentTrueTypeFont = null;
    }

    /// <summary>
    /// Sets the font and size used by subsequent text-showing operations.
    /// </summary>
    /// <param name="font">The standard font to use.</param>
    /// <param name="fontSize">The positive font size in PDF points.</param>
    public void SetTextFont(PdfFont font, double fontSize)
    {
        EnsureTextObjectOpen();
        ValidatePositiveFinite(fontSize, "fontSize", "Font size must be positive.");
        string resourceName = FontResourceName(font);
        AddFont(font);
        HasCurrentTrueTypeFont = false;
        CurrentTrueTypeFont = null;
        _content.AppendLine("/" + resourceName + " " + Number(fontSize) + " Tf");
    }

    /// <summary>
    /// Sets an embeddable TrueType font and size for subsequent text-showing operations.
    /// </summary>
    /// <param name="font">The loaded TrueType font to embed and use.</param>
    /// <param name="fontSize">The positive font size in PDF points.</param>
    /// <exception cref="ArgumentNullException">Thrown when font is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the font cannot be embedded.</exception>
    public void SetTextTrueTypeFont(PdfTrueTypeFont font, double fontSize)
    {
        EnsureTextObjectOpen();
        ValidateTrueTypeFont(font);
        ValidatePositiveFinite(fontSize, "fontSize", "Font size must be positive.");
        GetTrueTypeFontUsage(font);
        HasCurrentTrueTypeFont = true;
        CurrentTrueTypeFont = font;
        _content.AppendLine("/" + font.ResourceName + " " + Number(fontSize) + " Tf");
    }

    /// <summary>
    /// Sets additional spacing between character glyphs in the current text object.
    /// </summary>
    /// <param name="spacing">The character spacing in unscaled text-space units.</param>
    public void SetTextCharacterSpacing(double spacing)
    {
        EnsureTextObjectOpen();
        ValidateFinite(spacing, "spacing");
        _content.AppendLine(Number(spacing) + " Tc");
    }

    /// <summary>
    /// Sets additional spacing applied to word-space characters in the current text object.
    /// </summary>
    /// <param name="spacing">The word spacing in unscaled text-space units.</param>
    public void SetTextWordSpacing(double spacing)
    {
        EnsureTextObjectOpen();
        ValidateFinite(spacing, "spacing");
        _content.AppendLine(Number(spacing) + " Tw");
    }

    /// <summary>
    /// Sets horizontal text scaling as a percentage of normal width.
    /// </summary>
    /// <param name="scale">The positive horizontal scaling percentage.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when scale is not positive.</exception>
    public void SetTextHorizontalScaling(double scale)
    {
        EnsureTextObjectOpen();
        ValidatePositiveFinite(scale, "scale", "Horizontal text scaling must be positive.");
        _content.AppendLine(Number(scale) + " Tz");
    }

    /// <summary>
    /// Sets the vertical distance used by the next-line text operator.
    /// </summary>
    /// <param name="leading">The text leading in PDF points.</param>
    public void SetTextLeading(double leading)
    {
        EnsureTextObjectOpen();
        ValidateFinite(leading, "leading");
        _content.AppendLine(Number(leading) + " TL");
    }

    /// <summary>
    /// Sets the vertical text rise used for superscript or subscript positioning.
    /// </summary>
    /// <param name="rise">The text rise in PDF points.</param>
    public void SetTextRise(double rise)
    {
        EnsureTextObjectOpen();
        ValidateFinite(rise, "rise");
        _content.AppendLine(Number(rise) + " Ts");
    }

    /// <summary>
    /// Sets how glyph outlines are painted or added to the clipping path.
    /// </summary>
    /// <param name="renderingMode">The text rendering mode to apply.</param>
    /// <exception cref="ArgumentException">Thrown when the rendering mode is not supported.</exception>
    public void SetTextRenderingMode(PdfTextRenderingMode renderingMode)
    {
        EnsureTextObjectOpen();
        ValidateTextRenderingMode(renderingMode);
        _content.AppendLine(((int)renderingMode).ToString(CultureInfo.InvariantCulture) + " Tr");
    }

    /// <summary>
    /// Replaces the text matrix and text line matrix with an explicit matrix.
    /// </summary>
    /// <param name="a">The horizontal scaling component.</param>
    /// <param name="b">The vertical shear component.</param>
    /// <param name="c">The horizontal shear component.</param>
    /// <param name="d">The vertical scaling component.</param>
    /// <param name="e">The horizontal translation component.</param>
    /// <param name="f">The vertical translation component.</param>
    public void SetTextMatrix(double a, double b, double c, double d, double e, double f)
    {
        EnsureTextObjectOpen();
        ValidateFinite(a, "a");
        ValidateFinite(b, "b");
        ValidateFinite(c, "c");
        ValidateFinite(d, "d");
        ValidateFinite(e, "e");
        ValidateFinite(f, "f");
        _content.AppendLine(Number(a) + " " + Number(b) + " " + Number(c) + " " + Number(d) + " " + Number(e) + " " + Number(f) + " Tm");
    }

    /// <summary>
    /// Moves the text line matrix by the supplied offset.
    /// </summary>
    /// <param name="x">The horizontal offset in text space.</param>
    /// <param name="y">The vertical offset in text space.</param>
    public void MoveTextPosition(double x, double y)
    {
        EnsureTextObjectOpen();
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        _content.AppendLine(Number(x) + " " + Number(y) + " Td");
    }

    /// <summary>
    /// Moves the text line matrix and sets leading to the negative vertical offset.
    /// </summary>
    /// <param name="x">The horizontal offset in text space.</param>
    /// <param name="y">The vertical offset in text space.</param>
    public void MoveTextPositionAndSetLeading(double x, double y)
    {
        EnsureTextObjectOpen();
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        _content.AppendLine(Number(x) + " " + Number(y) + " TD");
    }

    /// <summary>
    /// Moves to the start of the next text line using the current leading.
    /// </summary>
    public void MoveToNextTextLine()
    {
        EnsureTextObjectOpen();
        _content.AppendLine("T*");
    }

    /// <summary>
    /// Shows text at the current text position.
    /// </summary>
    /// <param name="text">The text to encode and show.</param>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    public void ShowText(string text)
    {
        EnsureTextObjectOpen();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (HasCurrentTrueTypeFont && CurrentTrueTypeFont != null)
        {
            PdfTrueTypeFontUsage usage = GetTrueTypeFontUsage(CurrentTrueTypeFont);
            _content.AppendLine(usage.EncodeText(text) + " Tj");
        }
        else
        {
            _content.AppendLine(PdfUnicodeMap.EncodeTextString(text) + " Tj");
        }
    }

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    public void DrawText(string text, double x, double y, double fontSize)
    {
        DrawTextWithFont(text, x, y, fontSize, PdfFont.Regular);
    }

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <param name="font">The font to use for this operation.</param>
    public void DrawTextWithFont(string text, double x, double y, double fontSize, PdfFont font)
    {
        EnsureTextObjectClosed();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        ValidatePositiveFinite(fontSize, "fontSize", "Font size must be positive.");
        FontResourceName(font);
        BeginText();
        SetTextFont(font, fontSize);
        MoveTextPosition(x, y);
        ShowText(text);
        EndText();
    }

    /// <summary>
    /// Draws Unicode text with an embedded TrueType font.
    /// </summary>
    /// <param name="text">The Unicode text to draw.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="fontSize">The positive font size in PDF points.</param>
    /// <param name="font">The loaded TrueType font to embed and use.</param>
    /// <exception cref="ArgumentNullException">Thrown when text or font is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the font does not contain text that was supplied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the font cannot be embedded.</exception>
    public void DrawTextWithTrueTypeFont(string text, double x, double y, double fontSize, PdfTrueTypeFont font)
    {
        EnsureTextObjectClosed();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        ValidateTrueTypeFont(font);
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        ValidatePositiveFinite(fontSize, "fontSize", "Font size must be positive.");
        MeasureTrueTypeText(text, 0, font);
        BeginText();
        SetTextTrueTypeFont(font, fontSize);
        MoveTextPosition(x, y);
        ShowText(text);
        EndText();
    }

    /// <summary>
    /// Measures Unicode text with the loaded TrueType font without emitting drawing commands.
    /// </summary>
    /// <param name="text">The Unicode text to measure.</param>
    /// <param name="fontSize">The nonnegative font size in PDF points.</param>
    /// <param name="font">The loaded TrueType font whose metrics are used.</param>
    /// <returns>The calculated text width in PDF points.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text or font is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the font does not contain text that was supplied.</exception>
    public double MeasureTrueTypeText(string text, double fontSize, PdfTrueTypeFont font)
    {
        return MeasureTrueTypeWidth(text, fontSize, font);
    }

    internal static double MeasureTrueTypeWidth(string text, double fontSize, PdfTrueTypeFont font)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (font == null)
        {
            throw new ArgumentNullException("font");
        }

        if (double.IsNaN(fontSize) || double.IsInfinity(fontSize) || fontSize < 0)
        {
            throw new ArgumentOutOfRangeException("fontSize", "Font size cannot be negative and must be finite.");
        }

        long width = 0;
        int index = 0;
        while (index < text.Length)
        {
            int unicodeValue = PdfTrueTypeFontUsage.ReadUnicodeValue(text, index);
            int glyphIndex = font.GetGlyphIndex(unicodeValue);
            if (glyphIndex < 0)
            {
                throw new ArgumentException("The selected font does not contain every Unicode value in the text.", "text");
            }

            width += font.AdvanceWidths[glyphIndex];
            if (unicodeValue > 0xFFFF)
            {
                index++;
            }

            index++;
        }

        return width * fontSize / font.UnitsPerEm;
    }

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void DrawStyledText(string text, double x, double y, PdfTextStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        SetFillColorValue(style.FillColor);
        if (style.TrueTypeFont != null)
        {
            DrawTextWithTrueTypeFont(text, x, y, style.FontSize, style.TrueTypeFont);
        }
        else
        {
            DrawTextWithFont(text, x, y, style.FontSize, style.Font);
        }
    }

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <param name="alignment">The alignment to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void DrawAlignedStyledText(string text, double x, double y, double width, PdfTextStyle style, PdfTextAlignment alignment)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        ValidateTextBoxWidth(width);
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        ValidateAlignment(alignment);
        SetFillColorValue(style.FillColor);
        if (style.TrueTypeFont != null)
        {
            DrawAlignedTrueTypeText(text, x, y, width, style.FontSize, style.TrueTypeFont, alignment);
        }
        else
        {
            DrawAlignedText(text, x, y, width, style.FontSize, style.Font, alignment);
        }
    }

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void DrawParagraphText(string text, double x, double y, double width, PdfParagraphStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        DrawAlignedStyledText(text, x, y, width, style.TextStyle, style.Alignment);
    }

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void DrawRichText(PdfRichText text, double x, double y)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        double currentX = x;

        foreach (PdfTextSpan span in text.Spans)
        {
            DrawStyledText(span.Text, currentX, y, span.Style);
            currentX += span.MeasureWidth();
        }
    }

    /// <summary>
    /// Appends drawing commands for an image to the target PDF canvas.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    public void DrawImage(PdfImage image, double x, double y, double width, double height)
    {
        PdfImageFit.ValidateImage(image);
        PdfImageFit.ValidateBox(width, height);
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        bool hasMarkedContent = BeginImageMarkedContent(image);
        _content.AppendLine("q");
        _content.AppendLine(Number(width) + " 0 0 " + Number(height) + " " + Number(x) + " " + Number(y) + " cm");
        _content.AppendLine("/" + image.Name + " Do");
        _content.AppendLine("Q");
        if (hasMarkedContent)
        {
            _content.AppendLine("EMC");
        }
    }

    internal bool BeginImageMarkedContent(PdfImage image)
    {
        if (image.IsDecorative)
        {
            _content.AppendLine("/Artifact BMC");
            return true;
        }

        if (image.AlternateText.Length > 0)
        {
            _content.AppendLine("/Figure << /Alt " + PdfUnicodeMap.EncodeTextString(image.AlternateText) + " >> BDC");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Appends drawing commands for an image preserve aspect fit to the target PDF canvas.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    public void DrawImagePreserveAspectFit(PdfImage image, double x, double y, double width, double height)
    {
        PdfImagePlacement placement = new PdfImageFit().PreserveAspectFit(image, x, y, width, height);

        DrawImage(image, placement.X, placement.Y, placement.Width, placement.Height);
    }

    /// <summary>
    /// Appends drawing commands for an image crop fill to the target PDF canvas.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    public void DrawImageCropFill(PdfImage image, double x, double y, double width, double height)
    {
        PdfImagePlacement placement = new PdfImageFit().CropFill(image, x, y, width, height);

        SaveState();
        ClipRectangle(x, y, width, height);
        DrawImage(image, placement.X, placement.Y, placement.Width, placement.Height);
        RestoreState();
    }

    /// <summary>
    /// Appends drawing commands for a form to the target PDF canvas.
    /// </summary>
    /// <param name="form">The form to use for this operation.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    public void DrawForm(PdfFormXObject form, double x, double y, double width, double height)
    {
        if (form == null)
        {
            throw new ArgumentNullException("form");
        }

        ValidateRectangle(width, height);
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");
        _content.AppendLine("q");
        _content.AppendLine(Number(width) + " 0 0 " + Number(height) + " " + Number(x) + " " + Number(y) + " cm");
        _content.AppendLine("/" + form.Name + " Do");
        _content.AppendLine("Q");
    }

    /// <summary>
    /// Begins a marked-content span owned by a structure element on this page.
    /// </summary>
    /// <param name="node">The registered structure element that receives the next page-content identifier.</param>
    /// <returns>The page-local marked-content identifier.</returns>
    public int BeginStructureContent(PdfAccessibilityNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.OwnerDocument == null || !node.OwnerDocument.HasTaggedPdf)
        {
            throw new InvalidOperationException("Tagged PDF output must be enabled before marking structure content.");
        }

        if (node.Page.Canvas != this)
        {
            throw new ArgumentException("Structure content must be recorded on the node's page canvas.", "node");
        }

        int markedContentId = _markedContentNodes.Count;
        _markedContentNodes.Add(node);
        node._markedContentIds.Add(markedContentId);
        _content.AppendLine("/" + node.Role + " <</MCID " + markedContentId.ToString(CultureInfo.InvariantCulture) + ">> BDC");
        MarkedContentDepth++;
        return markedContentId;
    }

    /// <summary>
    /// Begins content excluded from the logical reading order.
    /// </summary>
    /// <param name="type">The artifact type written in the marked-content property list.</param>
    public void BeginArtifact(PdfArtifactType type)
    {
        string name = GetArtifactTypeName(type);
        _content.AppendLine("/Artifact <</Type /" + name + ">> BDC");
        MarkedContentDepth++;
    }

    /// <summary>
    /// Begins a repeating header, footer, or watermark excluded from the reading order.
    /// </summary>
    /// <param name="type">The pagination artifact subtype.</param>
    public void BeginPaginationArtifact(PdfPaginationArtifactType type)
    {
        string name = GetPaginationArtifactTypeName(type);
        _content.AppendLine("/Artifact <</Type /Pagination /Subtype /" + name + ">> BDC");
        MarkedContentDepth++;
    }

    /// <summary>
    /// Ends the most recently opened structure or artifact marked-content span.
    /// </summary>
    public void EndMarkedContent()
    {
        if (MarkedContentDepth == 0)
        {
            throw new InvalidOperationException("No marked-content span is open.");
        }

        _content.AppendLine("EMC");
        MarkedContentDepth--;
    }

    internal static string GetArtifactTypeName(PdfArtifactType type)
    {
        if (type == PdfArtifactType.Layout)
        {
            return "Layout";
        }

        if (type == PdfArtifactType.Page)
        {
            return "Page";
        }

        if (type == PdfArtifactType.Background)
        {
            return "Background";
        }

        throw new ArgumentException("Artifact type is not supported.", "type");
    }

    internal static string GetPaginationArtifactTypeName(PdfPaginationArtifactType type)
    {
        if (type == PdfPaginationArtifactType.Header)
        {
            return "Header";
        }

        if (type == PdfPaginationArtifactType.Footer)
        {
            return "Footer";
        }

        if (type == PdfPaginationArtifactType.Watermark)
        {
            return "Watermark";
        }

        throw new ArgumentException("Pagination artifact type is not supported.", "type");
    }

    /// <summary>
    /// Prepends a canvas so its drawing commands run before the existing content.
    /// </summary>
    /// <param name="canvas">The canvas to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void PrependCanvas(PdfCanvas canvas)
    {
        if (canvas == null)
        {
            throw new ArgumentNullException("canvas");
        }

        if (canvas.MarkedContentNodes.Count > 0)
        {
            throw new InvalidOperationException("Tagged page content cannot be prepended to another canvas.");
        }

        ValidateCompleteState();
        canvas.ValidateCompleteState();

        _content.Insert(0, canvas.ToPdfString());
        AddFontsFromCanvas(canvas);
        AddGraphicsStatesFromCanvas(canvas);
        AddTrueTypeFontUsagesFromCanvas(canvas);
        UsesDeviceRgb = UsesDeviceRgb || canvas.UsesDeviceRgb;
        UsesDeviceCmyk = UsesDeviceCmyk || canvas.UsesDeviceCmyk;
    }

    /// <summary>
    /// Appends a canvas so its drawing commands run after the existing content.
    /// </summary>
    /// <param name="canvas">The canvas to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AppendCanvas(PdfCanvas canvas)
    {
        if (canvas == null)
        {
            throw new ArgumentNullException("canvas");
        }

        if (canvas.MarkedContentNodes.Count > 0)
        {
            throw new InvalidOperationException("Tagged page content cannot be appended to another canvas.");
        }

        ValidateCompleteState();
        canvas.ValidateCompleteState();

        _content.Append(canvas.ToPdfString());
        AddFontsFromCanvas(canvas);
        AddGraphicsStatesFromCanvas(canvas);
        AddTrueTypeFontUsagesFromCanvas(canvas);
        UsesDeviceRgb = UsesDeviceRgb || canvas.UsesDeviceRgb;
        UsesDeviceCmyk = UsesDeviceCmyk || canvas.UsesDeviceCmyk;
    }

    /// <summary>
    /// Converts the object's current state to a string.
    /// </summary>
    /// <returns>The resulting text.</returns>
    public string ToPdfString()
    {
        return _content.ToString();
    }

    /// <summary>
    /// Measures the text without emitting drawing commands.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    internal static double MeasureText(string text, double fontSize)
    {
        return MeasureText(text, fontSize, PdfFont.Regular);
    }

    /// <summary>
    /// Measures the text without emitting drawing commands.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    internal static double MeasureText(string text, double fontSize, PdfFont font)
    {
        return PdfFontMetrics.MeasureText(font, text, fontSize);
    }

    /// <summary>
    /// Measures the text without emitting drawing commands.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static double MeasureText(string text, PdfTextStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        if (style.TrueTypeFont != null)
        {
            return MeasureTrueTypeWidth(text, style.FontSize, style.TrueTypeFont);
        }

        return MeasureText(text, style.FontSize, style.Font);
    }

    /// <summary>
    /// Measures the text without emitting drawing commands.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static double MeasureText(PdfRichText text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return text.MeasureWidth();
    }

    internal static string FontResourceName(PdfFont font)
    {
        return PdfFontRegistry.GetStandardResourceName(font);
    }

    internal void AddFont(PdfFont font)
    {
        foreach (PdfFont existingFont in _fonts)
        {
            if (existingFont == font)
            {
                return;
            }
        }

        _fonts.Add(font);
    }

    internal void AddFontsFromCanvas(PdfCanvas canvas)
    {
        foreach (PdfFont font in canvas.Fonts)
        {
            AddFont(font);
        }
    }

    internal PdfTrueTypeFontUsage GetTrueTypeFontUsage(PdfTrueTypeFont font)
    {
        foreach (PdfTrueTypeFontUsage usage in _trueTypeFontUsages)
        {
            if (usage.Font.Identity == font.Identity)
            {
                return usage;
            }
        }

        PdfTrueTypeFontUsage newUsage = new PdfTrueTypeFontUsage(font);
        _trueTypeFontUsages.Add(newUsage);
        return newUsage;
    }

    internal void AddTrueTypeFontUsagesFromCanvas(PdfCanvas canvas)
    {
        foreach (PdfTrueTypeFontUsage sourceUsage in canvas.TrueTypeFontUsages)
        {
            GetTrueTypeFontUsage(sourceUsage.Font).Merge(sourceUsage);
        }
    }

    internal static void ValidateTrueTypeFont(PdfTrueTypeFont font)
    {
        if (font == null)
        {
            throw new ArgumentNullException("font");
        }

        if (font.IsOpenType)
        {
            throw new InvalidOperationException("OpenType CFF embedding is not supported by the TrueType embedding workflow.");
        }

        if (!font.EmbeddingAllowed)
        {
            throw new InvalidOperationException("The font's embedding permissions prohibit outline embedding.");
        }
    }

    internal void AddGraphicsState(bool hasFillOpacity, double fillOpacity, bool hasStrokeOpacity, double strokeOpacity, bool hasBlendMode, PdfBlendMode blendMode)
    {
        string name = CreateGraphicsStateName(hasFillOpacity, fillOpacity, hasStrokeOpacity, strokeOpacity, hasBlendMode, blendMode);
        foreach (PdfGraphicsState state in _graphicsStates)
        {
            if (state.Name == name)
            {
                _content.AppendLine("/" + name + " gs");
                return;
            }
        }

        PdfGraphicsState graphicsState = new PdfGraphicsState(name, hasFillOpacity, fillOpacity, hasStrokeOpacity, strokeOpacity, hasBlendMode, blendMode);
        _graphicsStates.Add(graphicsState);
        _content.AppendLine("/" + name + " gs");
    }

    internal void AddGraphicsStatesFromCanvas(PdfCanvas canvas)
    {
        foreach (PdfGraphicsState sourceState in canvas.GraphicsStates)
        {
            bool found = false;
            foreach (PdfGraphicsState state in _graphicsStates)
            {
                if (state.Name == sourceState.Name)
                {
                    found = true;
                }
            }

            if (!found)
            {
                _graphicsStates.Add(sourceState);
            }
        }
    }

    internal static string CreateGraphicsStateName(bool hasFillOpacity, double fillOpacity, bool hasStrokeOpacity, double strokeOpacity, bool hasBlendMode, PdfBlendMode blendMode)
    {
        string name = "GS";
        if (hasFillOpacity)
        {
            name += "F" + NameNumber(fillOpacity);
        }

        if (hasStrokeOpacity)
        {
            name += "S" + NameNumber(strokeOpacity);
        }

        if (hasBlendMode)
        {
            name += "B" + ((int)blendMode).ToString(CultureInfo.InvariantCulture);
        }

        return name;
    }

    internal static string NameNumber(double value)
    {
        return Number(value).Replace(".", "p");
    }

    internal static void ValidateOpacity(double opacity, string name)
    {
        if (double.IsNaN(opacity) || double.IsInfinity(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(name, "Opacity must be between zero and one.");
        }
    }

    internal static void ValidateBlendMode(PdfBlendMode blendMode)
    {
        if ((int)blendMode < 0 || (int)blendMode > 15)
        {
            throw new ArgumentException("Blend mode is not supported.", "blendMode");
        }
    }

    internal static void ValidateLineCap(PdfLineCap lineCap)
    {
        if (lineCap == PdfLineCap.Butt || lineCap == PdfLineCap.Round || lineCap == PdfLineCap.Square)
        {
            return;
        }

        throw new ArgumentException("Line cap is not supported.", "lineCap");
    }

    internal static void ValidateLineJoin(PdfLineJoin lineJoin)
    {
        if (lineJoin == PdfLineJoin.Miter || lineJoin == PdfLineJoin.Round || lineJoin == PdfLineJoin.Bevel)
        {
            return;
        }

        throw new ArgumentException("Line join is not supported.", "lineJoin");
    }

    internal static void ValidateTextRenderingMode(PdfTextRenderingMode renderingMode)
    {
        if ((int)renderingMode < 0 || (int)renderingMode > 7)
        {
            throw new ArgumentException("Text rendering mode is not supported.", "renderingMode");
        }
    }

    internal static void ValidateFinite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }
    }

    internal static void ValidatePositiveFinite(double value, string name, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }

    internal void EnsureTextObjectOpen()
    {
        if (!TextObjectOpen)
        {
            throw new InvalidOperationException("A PDF text object must be open for this operation.");
        }
    }

    internal void EnsureTextObjectClosed()
    {
        if (TextObjectOpen)
        {
            throw new InvalidOperationException("The current PDF text object must be ended before using a convenience text drawing method.");
        }
    }

    internal void ValidateCompleteState()
    {
        if (TextObjectOpen)
        {
            throw new InvalidOperationException("A PDF text object must be ended before serialization or canvas composition.");
        }

        if (GraphicsStateDepth != 0)
        {
            throw new InvalidOperationException("Every saved PDF graphics state must be restored before serialization or canvas composition.");
        }

        if (MarkedContentDepth != 0)
        {
            throw new InvalidOperationException("Every marked-content span must be ended before serialization or canvas composition.");
        }
    }

    internal void WritePath(PdfPath path)
    {
        if (!path.HasCommands())
        {
            throw new ArgumentException("A path must contain at least one command.", "path");
        }

        foreach (PdfPathCommand command in path.Commands)
        {
            if (command.OperatorName == "m" || command.OperatorName == "l")
            {
                _content.AppendLine(Number(command.X1) + " " + Number(command.Y1) + " " + command.OperatorName);
            }
            else if (command.OperatorName == "c")
            {
                _content.AppendLine(Number(command.X1) + " " + Number(command.Y1) + " " + Number(command.X2) + " " + Number(command.Y2) + " " + Number(command.X3) + " " + Number(command.Y3) + " c");
            }
            else
            {
                _content.AppendLine("h");
            }
        }
    }

    internal static void ValidateRectangle(double width, double height)
    {
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Rectangle width must be positive.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Rectangle height must be positive.");
        }
    }

    internal static void ValidateTextBoxWidth(double width)
    {
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Text box width must be positive.");
        }
    }

    internal static void ValidateAlignment(PdfTextAlignment alignment)
    {
        if (alignment == PdfTextAlignment.Left)
        {
            return;
        }

        if (alignment == PdfTextAlignment.Center)
        {
            return;
        }

        if (alignment == PdfTextAlignment.Right)
        {
            return;
        }

        if (alignment == PdfTextAlignment.Justify)
        {
            return;
        }

        throw new ArgumentException("Text alignment is not supported.", "alignment");
    }

    internal void DrawAlignedText(string text, double x, double y, double width, double fontSize, PdfFont font, PdfTextAlignment alignment)
    {
        EnsureTextObjectClosed();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        ValidatePositiveFinite(fontSize, "fontSize", "Font size must be positive.");
        FontResourceName(font);
        double textWidth = MeasureText(text, fontSize, font);
        double alignedX = x;
        double wordSpacing = 0;

        if (alignment == PdfTextAlignment.Center)
        {
            alignedX = x + (width - textWidth) / 2;
        }
        else if (alignment == PdfTextAlignment.Right)
        {
            alignedX = x + width - textWidth;
        }
        else if (alignment == PdfTextAlignment.Justify)
        {
            int spaceCount = CountSpaces(text);
            if (spaceCount > 0 && textWidth < width)
            {
                wordSpacing = (width - textWidth) / spaceCount;
            }
        }

        BeginText();
        SetTextFont(font, fontSize);
        if (wordSpacing > 0)
        {
            SetTextWordSpacing(wordSpacing);
        }

        MoveTextPosition(alignedX, y);
        ShowText(text);
        if (wordSpacing > 0)
        {
            SetTextWordSpacing(0);
        }

        EndText();
    }

    internal void DrawAlignedTrueTypeText(string text, double x, double y, double width, double fontSize, PdfTrueTypeFont font, PdfTextAlignment alignment)
    {
        EnsureTextObjectClosed();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        ValidateTrueTypeFont(font);
        ValidatePositiveFinite(fontSize, "fontSize", "Font size must be positive.");
        double textWidth = MeasureTrueTypeWidth(text, fontSize, font);
        double alignedX = x;
        double wordSpacing = 0;
        if (alignment == PdfTextAlignment.Center)
        {
            alignedX = x + (width - textWidth) / 2;
        }
        else if (alignment == PdfTextAlignment.Right)
        {
            alignedX = x + width - textWidth;
        }
        else if (alignment == PdfTextAlignment.Justify)
        {
            int spaceCount = CountSpaces(text);
            if (spaceCount > 0 && textWidth < width)
            {
                wordSpacing = (width - textWidth) / spaceCount;
            }
        }

        BeginText();
        SetTextTrueTypeFont(font, fontSize);
        if (wordSpacing > 0)
        {
            SetTextWordSpacing(wordSpacing);
        }

        MoveTextPosition(alignedX, y);
        ShowText(text);
        if (wordSpacing > 0)
        {
            SetTextWordSpacing(0);
        }

        EndText();
    }

    internal static int CountSpaces(string text)
    {
        int count = 0;

        foreach (char character in text)
        {
            if (character == ' ')
            {
                count++;
            }
        }

        return count;
    }

    internal static string Number(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
