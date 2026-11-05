namespace qrcode;

/// <summary>Validates values supplied for the public QR classification enums.</summary>
public interface IQrEnumValueValidator
{
    /// <summary>Determines whether a symbol kind is defined.</summary>
    /// <param name="symbolKind">The symbol kind to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsSymbolKindDefined(QrSymbolKind symbolKind);

    /// <summary>Determines whether an encoding mode is defined.</summary>
    /// <param name="encodingMode">The encoding mode to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsEncodingModeDefined(QrEncodingMode encodingMode);

    /// <summary>Determines whether an error-correction level is defined.</summary>
    /// <param name="errorCorrectionLevel">The error-correction level to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsErrorCorrectionLevelDefined(QrErrorCorrectionLevel errorCorrectionLevel);

    /// <summary>Determines whether a mask pattern is defined.</summary>
    /// <param name="maskPattern">The mask pattern to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsMaskPatternDefined(QrMaskPattern maskPattern);

    /// <summary>Determines whether a pixel format is defined.</summary>
    /// <param name="pixelFormat">The pixel format to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsPixelFormatDefined(QrPixelFormat pixelFormat);

    /// <summary>Determines whether a binarization mode is defined.</summary>
    /// <param name="binarizationMode">The binarization mode to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsBinarizationModeDefined(QrBinarizationMode binarizationMode);

    /// <summary>Determines whether a polarity is defined.</summary>
    /// <param name="polarity">The polarity to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsPolarityDefined(QrPolarity polarity);

    /// <summary>Determines whether a module value is defined.</summary>
    /// <param name="moduleValue">The module value to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsModuleValueDefined(QrModuleValue moduleValue);

    /// <summary>Determines whether a module role is defined.</summary>
    /// <param name="moduleRole">The module role to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsModuleRoleDefined(QrModuleRole moduleRole);

    /// <summary>Determines whether a failure reason is defined.</summary>
    /// <param name="failureReason">The failure reason to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsFailureReasonDefined(QrFailureReason failureReason);

    /// <summary>Determines whether a quality grade is defined.</summary>
    /// <param name="qualityGrade">The quality grade to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    bool IsQualityGradeDefined(QrQualityGrade qualityGrade);
}
