namespace tiff;

/// <summary>Contains the values and diagnostics produced by a pyramid detection operation.</summary>
public class TiffPyramidDetectionResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the pyramid.</summary>
    public TiffPyramid Pyramid;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Pyramid Detection Result class.</summary>
    public TiffPyramidDetectionResult()
    {
        Success = true;
        Pyramid = new TiffPyramid();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Pyramid Detection Result class.</summary>
    /// <param name="pyramid">The pyramid.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffPyramidDetectionResult Create(TiffPyramid pyramid, TiffDiagnostics diagnostics)
    {
        TiffPyramidDetectionResult value = new TiffPyramidDetectionResult();
        value.Initialize(pyramid, diagnostics);
        return value;
    }

    internal void Initialize(TiffPyramid pyramid, TiffDiagnostics diagnostics)
    {
        if (pyramid == null)
        {
            Pyramid = new TiffPyramid();
        }
        else
        {
            Pyramid = pyramid.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
