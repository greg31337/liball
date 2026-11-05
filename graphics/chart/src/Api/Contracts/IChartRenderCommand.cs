namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartRenderCommand"/>.</summary>
public interface IChartRenderCommand
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartRenderCommand Copy();
    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    ChartRenderCommandKind GetKind();
    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetKind(ChartRenderCommandKind value);
    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    string GetId();
    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetId(string value);
    /// <summary>Returns the current x1.</summary>
    /// <returns>The current field value.</returns>
    double GetX1();
    /// <summary>Replaces the current x1.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetX1(double value);
    /// <summary>Returns the current y1.</summary>
    /// <returns>The current field value.</returns>
    double GetY1();
    /// <summary>Replaces the current y1.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetY1(double value);
    /// <summary>Returns the current x2.</summary>
    /// <returns>The current field value.</returns>
    double GetX2();
    /// <summary>Replaces the current x2.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetX2(double value);
    /// <summary>Returns the current y2.</summary>
    /// <returns>The current field value.</returns>
    double GetY2();
    /// <summary>Replaces the current y2.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetY2(double value);
    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    double GetWidth();
    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetWidth(double value);
    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    double GetHeight();
    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHeight(double value);
    /// <summary>Returns the current radius x.</summary>
    /// <returns>The current field value.</returns>
    double GetRadiusX();
    /// <summary>Replaces the current radius x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRadiusX(double value);
    /// <summary>Returns the current radius y.</summary>
    /// <returns>The current field value.</returns>
    double GetRadiusY();
    /// <summary>Replaces the current radius y.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRadiusY(double value);
    /// <summary>Returns the current points.</summary>
    /// <returns>The current field value.</returns>
    ChartPoint[] GetPoints();
    /// <summary>Replaces the current points.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPoints(ChartPoint[] value);
    /// <summary>Returns the current path.</summary>
    /// <returns>The current field value.</returns>
    ChartPath GetPath();
    /// <summary>Replaces the current path.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPath(ChartPath value);
    /// <summary>Returns the current text.</summary>
    /// <returns>The current field value.</returns>
    string GetText();
    /// <summary>Replaces the current text.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetText(string value);
    /// <summary>Returns the current marker shape.</summary>
    /// <returns>The current field value.</returns>
    ChartMarkerShape GetMarkerShape();
    /// <summary>Replaces the current marker shape.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMarkerShape(ChartMarkerShape value);
    /// <summary>Returns the current marker path.</summary>
    /// <returns>The current field value.</returns>
    ChartPath GetMarkerPath();
    /// <summary>Replaces the current marker path.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMarkerPath(ChartPath value);
    /// <summary>Returns the current marker size.</summary>
    /// <returns>The current field value.</returns>
    double GetMarkerSize();
    /// <summary>Replaces the current marker size.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMarkerSize(double value);
    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    ChartRenderStyle GetStyle();
    /// <summary>Replaces the current style.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStyle(ChartRenderStyle value);
    /// <summary>Returns whether this text command retains a complete text block.</summary>
    /// <returns>The text-block state.</returns>
    bool GetHasTextBlock();
    /// <summary>Returns an independent retained text block.</summary>
    /// <returns>A copied text block.</returns>
    ChartTextBlock GetTextBlock();
    /// <summary>Returns the arc start angle in radians.</summary>
    /// <returns>The retained start angle.</returns>
    double GetStartAngle();
    /// <summary>Replaces the arc start angle in radians.</summary>
    /// <param name = "value">The finite replacement angle.</param>
    void SetStartAngle(double value);
    /// <summary>Returns the arc end angle in radians.</summary>
    /// <returns>The retained end angle.</returns>
    double GetEndAngle();
    /// <summary>Replaces the arc end angle in radians.</summary>
    /// <param name = "value">The finite replacement angle.</param>
    void SetEndAngle(double value);
    /// <summary>Returns the inner radius of a wedge.</summary>
    /// <returns>The retained inner radius.</returns>
    double GetInnerRadius();
    /// <summary>Replaces the inner radius of a wedge.</summary>
    /// <param name = "value">The nonnegative replacement radius.</param>
    void SetInnerRadius(double value);
    /// <summary>Returns an independent copy of the retained affine transform.</summary>
    /// <returns>The retained transform.</returns>
    ChartMatrix GetTransform();
    /// <summary>Replaces the retained affine transform.</summary>
    /// <param name = "value">The replacement transform.</param>
    void SetTransform(ChartMatrix value);
    /// <summary>Returns an independent copy of the retained clip bounds.</summary>
    /// <returns>The retained clip bounds.</returns>
    ChartRect GetClipBounds();
    /// <summary>Replaces the retained clip bounds.</summary>
    /// <param name = "value">The replacement clip bounds.</param>
    void SetClipBounds(ChartRect value);
    /// <summary>Returns an independent copy of the retained raw image.</summary>
    /// <returns>The retained raw bitmap.</returns>
    RawBitmap GetImage();
    /// <summary>Replaces the retained raw image.</summary>
    /// <param name = "value">The replacement raw bitmap.</param>
    void SetImage(RawBitmap value);
}
