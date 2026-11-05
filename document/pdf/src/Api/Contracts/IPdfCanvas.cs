using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfCanvas"/>.
/// </summary>
public interface IPdfCanvas
{
    /// <summary>
    /// Pushes the current PDF graphics state so later changes can be undone with RestoreState.
    /// </summary>
    void SaveState();

    /// <summary>
    /// Restores the most recently saved PDF graphics state.
    /// </summary>
    void RestoreState();

    /// <summary>
    /// Sets the opacity used by subsequent fill operations.
    /// </summary>
    /// <param name="opacity">The normalized opacity from zero for transparent through one for opaque.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when opacity is outside the supported range.</exception>
    void SetFillOpacity(double opacity);

    /// <summary>
    /// Sets the opacity used by subsequent stroke operations.
    /// </summary>
    /// <param name="opacity">The normalized opacity from zero for transparent through one for opaque.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when opacity is outside the supported range.</exception>
    void SetStrokeOpacity(double opacity);

    /// <summary>
    /// Sets the blend mode used by subsequent painting operations.
    /// </summary>
    /// <param name="blendMode">The blend mode to apply.</param>
    /// <exception cref="ArgumentException">Thrown when the blend mode is not supported.</exception>
    void SetBlendMode(PdfBlendMode blendMode);

    /// <summary>
    /// Applies fill opacity, stroke opacity, and blend mode in one transparency graphics state.
    /// </summary>
    /// <param name="fillOpacity">The normalized fill opacity.</param>
    /// <param name="strokeOpacity">The normalized stroke opacity.</param>
    /// <param name="blendMode">The blend mode to apply.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an opacity is outside the supported range.</exception>
    /// <exception cref="ArgumentException">Thrown when the blend mode is not supported.</exception>
    void SetTransparency(double fillOpacity, double strokeOpacity, PdfBlendMode blendMode);

    /// <summary>
    /// Sets the line width used for subsequent PDF generation.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    void SetLineWidth(double width);

    /// <summary>
    /// Sets the line cap used for subsequent PDF generation.
    /// </summary>
    /// <param name="lineCap">The line cap to use for this operation.</param>
    void SetLineCap(PdfLineCap lineCap);

    /// <summary>
    /// Sets the line join used for subsequent PDF generation.
    /// </summary>
    /// <param name="lineJoin">The line join to use for this operation.</param>
    void SetLineJoin(PdfLineJoin lineJoin);

    /// <summary>
    /// Sets the dash used for subsequent PDF generation.
    /// </summary>
    /// <param name="pattern">The pattern to use for this operation.</param>
    /// <param name="phase">The phase to use for this operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    void SetDash(double[] pattern, double phase);

    /// <summary>
    /// Clears the current dash.
    /// </summary>
    void ClearDash();

    /// <summary>
    /// Concatenates the supplied six-value matrix with the canvas's current transformation matrix.
    /// </summary>
    /// <param name="a">The a to use for this operation.</param>
    /// <param name="b">The b to use for this operation.</param>
    /// <param name="c">The c to use for this operation.</param>
    /// <param name="d">The d to use for this operation.</param>
    /// <param name="e">The e to use for this operation.</param>
    /// <param name="f">The f to use for this operation.</param>
    void Transform(double a, double b, double c, double d, double e, double f);

    /// <summary>
    /// Offsets the coordinate system used by subsequent drawing commands.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    void Translate(double x, double y);

    /// <summary>
    /// Scales the coordinate system used by subsequent drawing commands.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    void Scale(double x, double y);

    /// <summary>
    /// Rotates the coordinate system used by subsequent drawing commands.
    /// </summary>
    /// <param name="degrees">The clockwise rotation angle in degrees.</param>
    void Rotate(double degrees);

    /// <summary>
    /// Skews the coordinate system used by subsequent drawing commands.
    /// </summary>
    /// <param name="xDegrees">The x degrees to use for this operation.</param>
    /// <param name="yDegrees">The y degrees to use for this operation.</param>
    void Skew(double xDegrees, double yDegrees);

    /// <summary>
    /// Sets the stroke color used for subsequent PDF generation.
    /// </summary>
    /// <param name="red">The red to use for this operation.</param>
    /// <param name="green">The green to use for this operation.</param>
    /// <param name="blue">The blue to use for this operation.</param>
    void SetStrokeColor(double red, double green, double blue);

    /// <summary>
    /// Sets the stroke color used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    void SetStrokeColorValue(PdfColor color);

    /// <summary>
    /// Sets the fill color used for subsequent PDF generation.
    /// </summary>
    /// <param name="red">The red to use for this operation.</param>
    /// <param name="green">The green to use for this operation.</param>
    /// <param name="blue">The blue to use for this operation.</param>
    void SetFillColor(double red, double green, double blue);

    /// <summary>
    /// Sets the fill color used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    void SetFillColorValue(PdfColor color);

    /// <summary>
    /// Appends drawing commands for a line to the target PDF canvas.
    /// </summary>
    /// <param name="x1">The first horizontal coordinate in PDF points.</param>
    /// <param name="y1">The first vertical coordinate in PDF points.</param>
    /// <param name="x2">The second horizontal coordinate in PDF points.</param>
    /// <param name="y2">The second vertical coordinate in PDF points.</param>
    void DrawLine(double x1, double y1, double x2, double y2);

    /// <summary>
    /// Draws a horizontal rule using the current stroke settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate of the rule's left endpoint.</param>
    /// <param name="y">The vertical coordinate of the rule.</param>
    /// <param name="width">The positive rule width in PDF points.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when width is not positive.</exception>
    void DrawHorizontalRule(double x, double y, double width);

    /// <summary>
    /// Draws a vertical rule using the current stroke settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate of the rule.</param>
    /// <param name="y">The vertical coordinate of the rule's bottom endpoint.</param>
    /// <param name="height">The positive rule height in PDF points.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when height is not positive.</exception>
    void DrawVerticalRule(double x, double y, double height);

    /// <summary>
    /// Strokes a path using the canvas's current line settings.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    void StrokePath(PdfPath path);

    /// <summary>
    /// Fills a path using the canvas's current fill settings.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    void FillPath(PdfPath path);

    /// <summary>
    /// Fills and strokes a path using the canvas's current color and line settings.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    void FillStrokePath(PdfPath path);

    /// <summary>
    /// Restricts subsequent drawing to a path until the graphics state is restored.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    void ClipPath(PdfPath path);

    /// <summary>
    /// Restricts subsequent drawing to a path even odd until the graphics state is restored.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    void ClipPathEvenOdd(PdfPath path);

    /// <summary>
    /// Restricts subsequent drawing to a rectangle until the graphics state is restored.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    void ClipRectangle(double x, double y, double width, double height);

    /// <summary>
    /// Strokes a circle using the canvas's current line settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    void StrokeCircle(double centerX, double centerY, double radius);

    /// <summary>
    /// Fills a circle using the canvas's current fill settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    void FillCircle(double centerX, double centerY, double radius);

    /// <summary>
    /// Fills and strokes a circle using the canvas's current color and line settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    void FillStrokeCircle(double centerX, double centerY, double radius);

    /// <summary>
    /// Strokes an ellipse using the canvas's current line settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radiusX">The horizontal radius in PDF points; it must be positive.</param>
    /// <param name="radiusY">The vertical radius in PDF points; it must be positive.</param>
    void StrokeEllipse(double centerX, double centerY, double radiusX, double radiusY);

    /// <summary>
    /// Fills an ellipse using the canvas's current fill settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radiusX">The horizontal radius in PDF points; it must be positive.</param>
    /// <param name="radiusY">The vertical radius in PDF points; it must be positive.</param>
    void FillEllipse(double centerX, double centerY, double radiusX, double radiusY);

    /// <summary>
    /// Fills and strokes an ellipse using the canvas's current color and line settings.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radiusX">The horizontal radius in PDF points; it must be positive.</param>
    /// <param name="radiusY">The vertical radius in PDF points; it must be positive.</param>
    void FillStrokeEllipse(double centerX, double centerY, double radiusX, double radiusY);

    /// <summary>
    /// Strokes a rounded rectangle using the canvas's current line settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    void StrokeRoundedRectangle(double x, double y, double width, double height, double radius);

    /// <summary>
    /// Fills a rounded rectangle using the canvas's current fill settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    void FillRoundedRectangle(double x, double y, double width, double height, double radius);

    /// <summary>
    /// Fills and strokes a rounded rectangle using the canvas's current color and line settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    void FillStrokeRoundedRectangle(double x, double y, double width, double height, double radius);

    /// <summary>
    /// Strokes a rectangle using the canvas's current line settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    void StrokeRectangle(double x, double y, double width, double height);

    /// <summary>
    /// Fills a rectangle using the canvas's current fill settings.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    void FillRectangle(double x, double y, double width, double height);

    /// <summary>
    /// Begins a caller-managed PDF text object.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a text object is already open.</exception>
    void BeginText();

    /// <summary>
    /// Ends the current caller-managed PDF text object.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no text object is open.</exception>
    void EndText();

    /// <summary>
    /// Sets the font and size used by subsequent text-showing operations.
    /// </summary>
    /// <param name="font">The standard font to use.</param>
    /// <param name="fontSize">The positive font size in PDF points.</param>
    void SetTextFont(PdfFont font, double fontSize);

    /// <summary>
    /// Sets an embeddable TrueType font and size for subsequent text-showing operations.
    /// </summary>
    /// <param name="font">The loaded TrueType font to embed and use.</param>
    /// <param name="fontSize">The positive font size in PDF points.</param>
    /// <exception cref="ArgumentNullException">Thrown when font is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the font cannot be embedded.</exception>
    void SetTextTrueTypeFont(PdfTrueTypeFont font, double fontSize);

    /// <summary>
    /// Sets additional spacing between character glyphs in the current text object.
    /// </summary>
    /// <param name="spacing">The character spacing in unscaled text-space units.</param>
    void SetTextCharacterSpacing(double spacing);

    /// <summary>
    /// Sets additional spacing applied to word-space characters in the current text object.
    /// </summary>
    /// <param name="spacing">The word spacing in unscaled text-space units.</param>
    void SetTextWordSpacing(double spacing);

    /// <summary>
    /// Sets horizontal text scaling as a percentage of normal width.
    /// </summary>
    /// <param name="scale">The positive horizontal scaling percentage.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when scale is not positive.</exception>
    void SetTextHorizontalScaling(double scale);

    /// <summary>
    /// Sets the vertical distance used by the next-line text operator.
    /// </summary>
    /// <param name="leading">The text leading in PDF points.</param>
    void SetTextLeading(double leading);

    /// <summary>
    /// Sets the vertical text rise used for superscript or subscript positioning.
    /// </summary>
    /// <param name="rise">The text rise in PDF points.</param>
    void SetTextRise(double rise);

    /// <summary>
    /// Sets how glyph outlines are painted or added to the clipping path.
    /// </summary>
    /// <param name="renderingMode">The text rendering mode to apply.</param>
    /// <exception cref="ArgumentException">Thrown when the rendering mode is not supported.</exception>
    void SetTextRenderingMode(PdfTextRenderingMode renderingMode);

    /// <summary>
    /// Replaces the text matrix and text line matrix with an explicit matrix.
    /// </summary>
    /// <param name="a">The horizontal scaling component.</param>
    /// <param name="b">The vertical shear component.</param>
    /// <param name="c">The horizontal shear component.</param>
    /// <param name="d">The vertical scaling component.</param>
    /// <param name="e">The horizontal translation component.</param>
    /// <param name="f">The vertical translation component.</param>
    void SetTextMatrix(double a, double b, double c, double d, double e, double f);

    /// <summary>
    /// Moves the text line matrix by the supplied offset.
    /// </summary>
    /// <param name="x">The horizontal offset in text space.</param>
    /// <param name="y">The vertical offset in text space.</param>
    void MoveTextPosition(double x, double y);

    /// <summary>
    /// Moves the text line matrix and sets leading to the negative vertical offset.
    /// </summary>
    /// <param name="x">The horizontal offset in text space.</param>
    /// <param name="y">The vertical offset in text space.</param>
    void MoveTextPositionAndSetLeading(double x, double y);

    /// <summary>
    /// Moves to the start of the next text line using the current leading.
    /// </summary>
    void MoveToNextTextLine();

    /// <summary>
    /// Shows text at the current text position.
    /// </summary>
    /// <param name="text">The text to encode and show.</param>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    void ShowText(string text);

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    void DrawText(string text, double x, double y, double fontSize);

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <param name="font">The font to use for this operation.</param>
    void DrawTextWithFont(string text, double x, double y, double fontSize, PdfFont font);

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
    void DrawTextWithTrueTypeFont(string text, double x, double y, double fontSize, PdfTrueTypeFont font);

    /// <summary>
    /// Measures Unicode text with the loaded TrueType font without emitting drawing commands.
    /// </summary>
    /// <param name="text">The Unicode text to measure.</param>
    /// <param name="fontSize">The nonnegative font size in PDF points.</param>
    /// <param name="font">The loaded TrueType font whose metrics are used.</param>
    /// <returns>The calculated text width in PDF points.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text or font is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the font does not contain text that was supplied.</exception>
    double MeasureTrueTypeText(string text, double fontSize, PdfTrueTypeFont font);

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void DrawStyledText(string text, double x, double y, PdfTextStyle style);

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
    void DrawAlignedStyledText(string text, double x, double y, double width, PdfTextStyle style, PdfTextAlignment alignment);

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void DrawParagraphText(string text, double x, double y, double width, PdfParagraphStyle style);

    /// <summary>
    /// Appends drawing commands for the text to the target PDF canvas.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void DrawRichText(PdfRichText text, double x, double y);

    /// <summary>
    /// Appends drawing commands for an image to the target PDF canvas.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    void DrawImage(PdfImage image, double x, double y, double width, double height);

    /// <summary>
    /// Appends drawing commands for an image preserve aspect fit to the target PDF canvas.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    void DrawImagePreserveAspectFit(PdfImage image, double x, double y, double width, double height);

    /// <summary>
    /// Appends drawing commands for an image crop fill to the target PDF canvas.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    void DrawImageCropFill(PdfImage image, double x, double y, double width, double height);

    /// <summary>
    /// Appends drawing commands for a form to the target PDF canvas.
    /// </summary>
    /// <param name="form">The form to use for this operation.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    void DrawForm(PdfFormXObject form, double x, double y, double width, double height);

    /// <summary>Begins a marked-content span owned by a structure element.</summary>
    /// <param name="node">The structure element.</param>
    /// <returns>The page-local marked-content identifier.</returns>
    int BeginStructureContent(PdfAccessibilityNode node);

    /// <summary>Begins content excluded from the logical reading order.</summary>
    /// <param name="type">The artifact type.</param>
    void BeginArtifact(PdfArtifactType type);

    /// <summary>Begins a repeating pagination artifact.</summary>
    /// <param name="type">The header, footer, or watermark subtype.</param>
    void BeginPaginationArtifact(PdfPaginationArtifactType type);

    /// <summary>Ends the most recently opened marked-content span.</summary>
    void EndMarkedContent();

    /// <summary>
    /// Prepends a canvas so its drawing commands run before the existing content.
    /// </summary>
    /// <param name="canvas">The canvas to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void PrependCanvas(PdfCanvas canvas);

    /// <summary>
    /// Appends a canvas so its drawing commands run after the existing content.
    /// </summary>
    /// <param name="canvas">The canvas to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AppendCanvas(PdfCanvas canvas);

    /// <summary>
    /// Converts the object's current state to a string.
    /// </summary>
    /// <returns>The resulting text.</returns>
    string ToPdfString();
}
