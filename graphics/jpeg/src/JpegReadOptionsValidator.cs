namespace jpeg;

internal class JpegReadOptionsValidator
{
    internal void Validate(JpegReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.MetadataPolicy != JpegMetadataPolicy.PreserveAll
            && options.MetadataPolicy != JpegMetadataPolicy.PreserveKnown
            && options.MetadataPolicy != JpegMetadataPolicy.StripAll
            && options.MetadataPolicy != JpegMetadataPolicy.StripUnsafe
            && options.MetadataPolicy != JpegMetadataPolicy.ReplaceSelected)
        {
            throw JpegInvalidDataException.Create("The metadata policy is not supported.");
        }

        if (options.ColorPolicy != JpegColorPolicy.PreserveSource
            && options.ColorPolicy != JpegColorPolicy.ConvertToRgb
            && options.ColorPolicy != JpegColorPolicy.ConvertToCmyk
            && options.ColorPolicy != JpegColorPolicy.UseIccWhenSupported
            && options.ColorPolicy != JpegColorPolicy.FailIfUnsupported)
        {
            throw JpegInvalidDataException.Create("The color policy is not supported.");
        }

        if (options.IccBehaviorPolicy != JpegIccBehaviorPolicy.Preserve
            && options.IccBehaviorPolicy != JpegIccBehaviorPolicy.Ignore
            && options.IccBehaviorPolicy != JpegIccBehaviorPolicy.ConvertWhenSupported
            && options.IccBehaviorPolicy != JpegIccBehaviorPolicy.FailIfUnsupported)
        {
            throw JpegInvalidDataException.Create("The ICC behavior policy is not supported.");
        }

        if (options.ErrorPolicy != JpegErrorPolicy.Strict && options.ErrorPolicy != JpegErrorPolicy.Tolerant)
        {
            throw JpegInvalidDataException.Create("The error policy is not supported.");
        }

        if (options.OutputPixelFormat != JpegPixelFormat.Unknown
            && options.OutputPixelFormat != JpegPixelFormat.L8
            && options.OutputPixelFormat != JpegPixelFormat.Rgb24
            && options.OutputPixelFormat != JpegPixelFormat.Rgba32
            && options.OutputPixelFormat != JpegPixelFormat.Bgr24
            && options.OutputPixelFormat != JpegPixelFormat.Bgra32
            && options.OutputPixelFormat != JpegPixelFormat.Cmyk32
            && options.OutputPixelFormat != JpegPixelFormat.YCbCr24
            && options.OutputPixelFormat != JpegPixelFormat.Ycck32
            && options.OutputPixelFormat != JpegPixelFormat.RawComponents)
        {
            throw JpegInvalidDataException.Create("The output pixel format is not supported.");
        }

        if (options.ChromaUpsamplingMode != JpegChromaUpsamplingMode.Box && options.ChromaUpsamplingMode != JpegChromaUpsamplingMode.Fancy)
        {
            throw JpegInvalidDataException.Create("The chroma upsampling mode is not supported.");
        }

        if (options.UnknownColorHandling != JpegUnknownColorHandling.BestEffort && options.UnknownColorHandling != JpegUnknownColorHandling.Exact)
        {
            throw JpegInvalidDataException.Create("The unknown-color handling mode is not supported.");
        }

        if (options.AdobeInversionMode != JpegAdobeInversionMode.Automatic && options.AdobeInversionMode != JpegAdobeInversionMode.ForceInverted && options.AdobeInversionMode != JpegAdobeInversionMode.ForceNonInverted)
        {
            throw JpegInvalidDataException.Create("The Adobe inversion mode is not supported.");
        }

        if (options.YCbCrRange != JpegYCbCrRange.Full && options.YCbCrRange != JpegYCbCrRange.Limited)
        {
            throw JpegInvalidDataException.Create("The YCbCr range is not supported.");
        }

        if (options.InverseDctMethod != JpegInverseDctMethod.Accurate && options.InverseDctMethod != JpegInverseDctMethod.Fast)
        {
            throw JpegInvalidDataException.Create("The inverse DCT method is not supported.");
        }

        int scaleRatio = (int)options.IdctScaleRatio;
        if (scaleRatio < 0 || scaleRatio > 8)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.DecodeRegion == null)
        {
            throw new System.ArgumentNullException("options");
        }
    }
}
