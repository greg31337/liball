namespace qrcode;

/// <summary>Validates public QR data objects before they enter processing workflows.</summary>
public interface IQrDataValueValidator
{
    /// <summary>Determines whether a decode attempt result is internally consistent.</summary>
    /// <param name="attemptResult">The decode attempt result to examine.</param>
    /// <returns><c>true</c> when the result is valid; otherwise, <c>false</c>.</returns>
    bool IsAttemptResultValid(QrAttemptResult attemptResult);

    /// <summary>Determines whether a capacity request is internally consistent.</summary>
    /// <param name="capacityRequest">The capacity request to examine.</param>
    /// <returns><c>true</c> when the request is valid; otherwise, <c>false</c>.</returns>
    bool IsCapacityRequestValid(QrCapacityRequest capacityRequest);

    /// <summary>Determines whether a capacity result is internally consistent.</summary>
    /// <param name="capacityResult">The capacity result to examine.</param>
    /// <returns><c>true</c> when the result is valid; otherwise, <c>false</c>.</returns>
    bool IsCapacityResultValid(QrCapacityResult capacityResult);

    /// <summary>Determines whether an error-correction result is internally consistent.</summary>
    /// <param name="correctionResult">The error-correction result to examine.</param>
    /// <returns><c>true</c> when the result is valid; otherwise, <c>false</c>.</returns>
    bool IsCorrectionResultValid(QrCorrectionResult correctionResult);

    /// <summary>Determines whether an encode attempt result is internally consistent.</summary>
    /// <param name="encodeAttemptResult">The encode attempt result to examine.</param>
    /// <returns><c>true</c> when the result is valid; otherwise, <c>false</c>.</returns>
    bool IsEncodeAttemptResultValid(QrEncodeAttemptResult encodeAttemptResult);

    /// <summary>Determines whether a raw image and its layout are valid.</summary>
    /// <param name="image">The raw image to examine.</param>
    /// <returns><c>true</c> when the image is valid; otherwise, <c>false</c>.</returns>
    bool IsImageValid(QrImage image);

    /// <summary>Determines whether a quadrilateral contains valid image-space geometry.</summary>
    /// <param name="quadrilateral">The quadrilateral to examine.</param>
    /// <returns><c>true</c> when the geometry is valid; otherwise, <c>false</c>.</returns>
    bool IsQuadrilateralValid(QrQuadrilateral quadrilateral);

    /// <summary>Determines whether a grayscale raster and its layout are valid.</summary>
    /// <param name="raster">The grayscale raster to examine.</param>
    /// <returns><c>true</c> when the raster is valid; otherwise, <c>false</c>.</returns>
    bool IsRasterValid(QrRaster raster);

    /// <summary>Determines whether encode options are internally consistent.</summary>
    /// <param name="encodeOptions">The encode options to examine.</param>
    /// <returns><c>true</c> when the options are valid; otherwise, <c>false</c>.</returns>
    bool IsEncodeOptionsValid(QrEncodeOptions encodeOptions);

    /// <summary>Determines whether decode options are internally consistent.</summary>
    /// <param name="decodeOptions">The decode options to examine.</param>
    /// <returns><c>true</c> when the options are valid; otherwise, <c>false</c>.</returns>
    bool IsDecodeOptionsValid(QrDecodeOptions decodeOptions);

    /// <summary>Determines whether detection options are internally consistent.</summary>
    /// <param name="detectionOptions">The detection options to examine.</param>
    /// <returns><c>true</c> when the options are valid; otherwise, <c>false</c>.</returns>
    bool IsDetectionOptionsValid(QrDetectionOptions detectionOptions);

    /// <summary>Determines whether rendering options are internally consistent.</summary>
    /// <param name="renderOptions">The rendering options to examine.</param>
    /// <returns><c>true</c> when the options are valid; otherwise, <c>false</c>.</returns>
    bool IsRenderOptionsValid(QrRenderOptions renderOptions);

    /// <summary>Determines whether quality-analysis options are internally consistent.</summary>
    /// <param name="qualityOptions">The quality-analysis options to examine.</param>
    /// <returns><c>true</c> when the options are valid; otherwise, <c>false</c>.</returns>
    bool IsQualityOptionsValid(QrQualityOptions qualityOptions);

    /// <summary>Determines whether a symbol result is internally consistent.</summary>
    /// <param name="symbol">The symbol result to examine.</param>
    /// <returns><c>true</c> when the result is valid; otherwise, <c>false</c>.</returns>
    bool IsSymbolValid(QrSymbol symbol);

    /// <summary>Determines whether a symbol sequence is internally consistent.</summary>
    /// <param name="symbolSequence">The symbol sequence to examine.</param>
    /// <returns><c>true</c> when the sequence is valid; otherwise, <c>false</c>.</returns>
    bool IsSymbolSequenceValid(QrSymbolSequence symbolSequence);

    /// <summary>Determines whether a detection result is internally consistent.</summary>
    /// <param name="detection">The detection result to examine.</param>
    /// <returns><c>true</c> when the result is valid; otherwise, <c>false</c>.</returns>
    bool IsDetectionValid(QrDetection detection);

    /// <summary>Determines whether a detection set is internally consistent.</summary>
    /// <param name="detectionSet">The detection set to examine.</param>
    /// <returns><c>true</c> when the set is valid; otherwise, <c>false</c>.</returns>
    bool IsDetectionSetValid(QrDetectionSet detectionSet);

    /// <summary>Determines whether a decode result is internally consistent.</summary>
    /// <param name="decodeResult">The decode result to examine.</param>
    /// <returns><c>true</c> when the result is valid; otherwise, <c>false</c>.</returns>
    bool IsDecodeResultValid(QrDecodeResult decodeResult);

    /// <summary>Determines whether a decode set is internally consistent.</summary>
    /// <param name="decodeSet">The decode set to examine.</param>
    /// <returns><c>true</c> when the set is valid; otherwise, <c>false</c>.</returns>
    bool IsDecodeSetValid(QrDecodeSet decodeSet);

    /// <summary>Determines whether a validation result is internally consistent.</summary>
    /// <param name="validationResult">The validation result to examine.</param>
    /// <returns><c>true</c> when the result is valid; otherwise, <c>false</c>.</returns>
    bool IsValidationResultValid(QrValidationResult validationResult);

    /// <summary>Determines whether a quality metric is internally consistent.</summary>
    /// <param name="qualityMetric">The quality metric to examine.</param>
    /// <returns><c>true</c> when the metric is valid; otherwise, <c>false</c>.</returns>
    bool IsQualityMetricValid(QrQualityMetric qualityMetric);

    /// <summary>Determines whether a quality report is internally consistent.</summary>
    /// <param name="qualityReport">The quality report to examine.</param>
    /// <returns><c>true</c> when the report is valid; otherwise, <c>false</c>.</returns>
    bool IsQualityReportValid(QrQualityReport qualityReport);
}
