using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Stores one command and its ordered points in a DrawingML freeform path.
/// </summary>
public class PptxFreeformCommand
{
    /// <summary>
    /// Stores the command kind.
    /// </summary>
    public PptxFreeformCommandType Type;
    /// <summary>
    /// Contains the command points in DrawingML order.
    /// </summary>
    public List<PptxPoint> Points;

    /// <summary>
    /// Initializes a command with an empty point collection.
    /// </summary>
    /// <param name="type">The command kind.</param>
    public PptxFreeformCommand(PptxFreeformCommandType type)
    {
        Type = type;
        Points = new List<PptxPoint>();
    }
}
