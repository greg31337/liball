namespace pptx;

/// <summary>
/// Stores a connector endpoint position and its optional attachment to a shape connection site.
/// </summary>
public class PptxConnectionPoint
{
    /// <summary>
    /// Indicates whether the endpoint is attached to a shape.
    /// </summary>
    public bool Connected;
    /// <summary>
    /// Stores the identifier of the attached shape.
    /// </summary>
    public int ShapeId;
    /// <summary>
    /// Stores the zero-based connection-site index on the attached shape.
    /// </summary>
    public int SiteIndex;
    /// <summary>
    /// Stores the endpoint position in English Metric Units (EMUs).
    /// </summary>
    public PptxPoint Position;

    /// <summary>
    /// Initializes a disconnected endpoint at the supplied position.
    /// </summary>
    /// <param name="position">The endpoint position in English Metric Units (EMUs).</param>
    public PptxConnectionPoint(PptxPoint position)
    {
        if (position == null)
        {
            throw new System.ArgumentNullException("position");
        }

        Connected = false;
        ShapeId = 0;
        SiteIndex = 0;
        Position = new PptxPoint(position.X, position.Y);
    }
}
