namespace qrcode;

/// <summary>Contains binary-image evidence for the required quiet zone around a detected symbol.</summary>
/// <remarks>Measurements use image pixel centers and detected geometry, and are not calibrated print-quality certification.</remarks>
public class QrQuietZoneMeasurement
{
    /// <summary>The required quiet-zone width in modules for the selected profile.</summary>
    public int RequiredModules;

    /// <summary>Whether the required margin lies inside the image and each side contains observed pixels.</summary>
    public bool IsFullyObserved;

    /// <summary>Whether the complete required margin was observed without dark-pixel contamination.</summary>
    public bool IsConformant;

    /// <summary>Whether part of the required margin extends outside the supplied image.</summary>
    public bool IsCropped;

    /// <summary>Whether perspective extrapolation could not represent the required margin as a finite quadrilateral.</summary>
    public bool HasUnmappableBoundary;

    /// <summary>Whether this quiet-zone result permits retaining the candidate under the detection tolerance option.</summary>
    /// <remarks>This does not establish that the candidate is a valid QR symbol.</remarks>
    public bool AcceptForDetection;

    /// <summary>The number of image pixel centers observed in the required margin, including its corners.</summary>
    public int ObservedPixelCount;

    /// <summary>The number of observed margin pixels that are dark in the polarity-normalized binary image.</summary>
    public int ContaminatedPixelCount;

    /// <summary>The number of observed pixels in the left margin, including its corners.</summary>
    public int LeftPixelCount;

    /// <summary>The number of observed pixels in the top margin, including its corners.</summary>
    public int TopPixelCount;

    /// <summary>The number of observed pixels in the right margin, including its corners.</summary>
    public int RightPixelCount;

    /// <summary>The number of observed pixels in the bottom margin, including its corners.</summary>
    public int BottomPixelCount;
}
