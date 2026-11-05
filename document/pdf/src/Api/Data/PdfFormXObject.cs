using System;

namespace pdf;

/// <summary>
/// Represents reusable PDF drawing content that can be placed repeatedly without duplicating its commands.
/// </summary>
public class PdfFormXObject
{
    /// <summary>
    /// Stores the name used by this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Specifies the width in PDF points.
    /// </summary>
    public double Width;
    /// <summary>
    /// Specifies the height in PDF points.
    /// </summary>
    public double Height;
    /// <summary>
    /// Stores the canvas used by this object.
    /// </summary>
    public PdfCanvas Canvas;

    /// <summary>
    /// Initializes a new form x object.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    public PdfFormXObject(string name, double width, double height)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Form resource name cannot be empty.", "name");
        }

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Form width must be finite and positive.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Form height must be finite and positive.");
        }

        Name = name;
        Width = width;
        Height = height;
        Canvas = new PdfCanvas();
    }
}
