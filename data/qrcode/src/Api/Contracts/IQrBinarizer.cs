namespace qrcode;

/// <summary>Converts raw images to luminance data and normalized binary module matrices.</summary>
public interface IQrBinarizer
{
    /// <summary>Converts a supported raw image to a tightly packed Gray8 luminance image.</summary>
    /// <param name="image">The caller-owned raw source image.</param>
    /// <param name="options">The preprocessing and resource-limit options.</param>
    /// <returns>A newly allocated Gray8 image that does not share caller memory.</returns>
    QrImage CreateLuminanceImage(QrImage image, QrDetectionOptions options);

    /// <summary>Creates normalized binary matrices for the requested binarization mode and polarity.</summary>
    /// <param name="image">The caller-owned raw or supplied-binary source image.</param>
    /// <param name="options">The binarization, polarity, cleanup, and resource-limit options.</param>
    /// <returns>One matrix for an explicit polarity, or dark-on-light then light-on-dark matrices for automatic polarity.</returns>
    QrMatrix[] BinarizeImage(QrImage image, QrDetectionOptions options);
}
