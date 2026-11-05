namespace qrcode;

/// <summary>Decodes QR-family matrices and supplied image detections with verified recovery.</summary>
/// <remarks>Invalid caller input and limits that reject the input before decoding throw exceptions in both API forms. Try methods return expected symbol, payload, text-policy, and recovery failures without throwing or exposing partial payloads.</remarks>
public interface IQrDecoder
{
    /// <summary>Decodes a module matrix or throws a decode exception with recovery diagnostics.</summary>
    /// <param name="matrix">The module values, optional confidence, and quiet-zone extents.</param>
    /// <param name="options">The accepted families, parsing policy, and resource limits.</param>
    /// <returns>The complete verified decoded symbol and payload.</returns>
    QrDecodeResult DecodeMatrix(QrMatrix matrix, QrDecodeOptions options);

    /// <summary>Resamples and decodes a supplied image detection or throws a decode exception.</summary>
    /// <param name="image">The raw image, or Gray8 zero/255 pixels for supplied-binary sampling.</param>
    /// <param name="detection">The symbol boundary and estimated dimensions; cached samples are not reused.</param>
    /// <param name="sampling">The preprocessing, sampling, refinement, and image limits.</param>
    /// <param name="options">The accepted families, parsing policy, and decode limits.</param>
    /// <returns>The complete decoded result with fresh detection and sampling diagnostics.</returns>
    QrDecodeResult DecodeDetection(QrImage image, QrDetection detection, QrDetectionOptions sampling, QrDecodeOptions options);

    /// <summary>Attempts matrix decoding without throwing for expected decode failures.</summary>
    /// <param name="matrix">The module matrix to decode without modifying caller buffers.</param>
    /// <param name="options">The accepted families, parsing policy, and resource limits.</param>
    /// <returns>The complete result or a failure reason and actual recovery attempt count.</returns>
    QrAttemptResult TryDecodeMatrix(QrMatrix matrix, QrDecodeOptions options);

    /// <summary>Attempts fresh image sampling and decoding without throwing for expected decode failures.</summary>
    /// <param name="image">The raw or supplied-binary source image.</param>
    /// <param name="detection">The boundary and dimensions of one candidate.</param>
    /// <param name="sampling">The preprocessing, sampling, refinement, and image limits.</param>
    /// <param name="options">The accepted families, parsing policy, and shared recovery limits.</param>
    /// <returns>The complete result or a failure reason and the shared image/matrix attempt count.</returns>
    QrAttemptResult TryDecodeDetection(QrImage image, QrDetection detection, QrDetectionOptions sampling, QrDecodeOptions options);
}
