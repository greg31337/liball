namespace odf;

/// <summary>
/// Describes one command segment in SVG path data.
/// </summary>
public class OdfSvgPathCommand
{
    /// <summary>The case-preserving SVG command letter.</summary>
    public string Command = "";
    /// <summary>The numeric command parameters.</summary>
    public double[] Parameters = [];
}
