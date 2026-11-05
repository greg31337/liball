namespace tiff;

/// <summary>Describes the source samples supplied for a configured writer image.</summary>
/// <remarks>One bits-per-sample or sample-format value is repeated for every sample. Extra samples identify alpha association and other non-color samples.</remarks>
public class TiffWriterPixelFormat
{
    /// <summary>Contains the number of samples in each pixel.</summary>
    public int SamplesPerPixel;
    /// <summary>Contains one bit depth or one bit depth for every sample.</summary>
    public int[] BitsPerSample;
    /// <summary>Contains one sample format or one sample format for every sample.</summary>
    public TiffSampleFormat[] SampleFormats;
    /// <summary>Specifies how color samples are interpreted.</summary>
    public TiffPhotometric Photometric;
    /// <summary>Identifies non-color samples, including associated or unassociated alpha.</summary>
    public TiffExtraSample[] ExtraSamples;

    /// <summary>Creates an unsigned eight-bit grayscale pixel format.</summary>
    public TiffWriterPixelFormat()
    {
        SamplesPerPixel = 1;
        BitsPerSample = new int[] { 8 };
        SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        Photometric = TiffPhotometric.BlackIsZero;
        ExtraSamples = new TiffExtraSample[0];
    }
}
