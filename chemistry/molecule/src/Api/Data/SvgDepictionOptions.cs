using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Controls svg depiction options behavior.
/// </summary>
public class SvgDepictionOptions
{
    /// <summary>
    /// The scale.
    /// </summary>
    public double Scale;
    /// <summary>
    /// The padding.
    /// </summary>
    public double Padding;
    /// <summary>
    /// The font size.
    /// </summary>
    public double FontSize;
    /// <summary>
    /// The stroke width.
    /// </summary>
    public double StrokeWidth;
    /// <summary>
    /// The stereo width.
    /// </summary>
    public double StereoWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="SvgDepictionOptions"/> class.
    /// </summary>
    public SvgDepictionOptions()
    {
        Scale = 40.0;
        Padding = 20.0;
        FontSize = 14.0;
        StrokeWidth = 2.0;
        StereoWidth = 8.0;
    }
}

