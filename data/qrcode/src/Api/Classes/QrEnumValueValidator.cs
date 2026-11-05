namespace qrcode;

/// <summary>Provides stateless validation for public QR classification enum values.</summary>
public class QrEnumValueValidator : IQrEnumValueValidator
{
    /// <summary>Initializes a reusable stateless enum validator.</summary>
    public QrEnumValueValidator()
    {
    }

    /// <summary>Determines whether a symbol kind is defined.</summary>
    /// <param name="symbolKind">The symbol kind to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsSymbolKindDefined(QrSymbolKind symbolKind)
    {
        int value = (int)symbolKind;
        return value >= (int)QrSymbolKind.Model2 && value <= (int)QrSymbolKind.LegacyModel1;
    }

    /// <summary>Determines whether an encoding mode is defined.</summary>
    /// <param name="encodingMode">The encoding mode to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsEncodingModeDefined(QrEncodingMode encodingMode)
    {
        int value = (int)encodingMode;
        return value == (int)QrEncodingMode.Terminator
            || value == (int)QrEncodingMode.Numeric
            || value == (int)QrEncodingMode.Alphanumeric
            || value == (int)QrEncodingMode.StructuredAppend
            || value == (int)QrEncodingMode.Byte
            || value == (int)QrEncodingMode.Fnc1First
            || value == (int)QrEncodingMode.Eci
            || value == (int)QrEncodingMode.Kanji
            || value == (int)QrEncodingMode.Fnc1Second
            || value == (int)QrEncodingMode.Hanzi;
    }

    /// <summary>Determines whether an error-correction level is defined.</summary>
    /// <param name="errorCorrectionLevel">The error-correction level to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsErrorCorrectionLevelDefined(QrErrorCorrectionLevel errorCorrectionLevel)
    {
        int value = (int)errorCorrectionLevel;
        return value >= (int)QrErrorCorrectionLevel.None && value <= (int)QrErrorCorrectionLevel.High;
    }

    /// <summary>Determines whether a mask pattern is defined.</summary>
    /// <param name="maskPattern">The mask pattern to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsMaskPatternDefined(QrMaskPattern maskPattern)
    {
        int value = (int)maskPattern;
        return value >= (int)QrMaskPattern.Automatic && value <= (int)QrMaskPattern.Seven;
    }

    /// <summary>Determines whether a pixel format is defined.</summary>
    /// <param name="pixelFormat">The pixel format to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsPixelFormatDefined(QrPixelFormat pixelFormat)
    {
        int value = (int)pixelFormat;
        return value >= (int)QrPixelFormat.Gray8 && value <= (int)QrPixelFormat.Bgra32;
    }

    /// <summary>Determines whether a binarization mode is defined.</summary>
    /// <param name="binarizationMode">The binarization mode to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsBinarizationModeDefined(QrBinarizationMode binarizationMode)
    {
        int value = (int)binarizationMode;
        return value >= (int)QrBinarizationMode.Automatic
            && value <= (int)QrBinarizationMode.SuppliedBinary;
    }

    /// <summary>Determines whether a polarity is defined.</summary>
    /// <param name="polarity">The polarity to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsPolarityDefined(QrPolarity polarity)
    {
        int value = (int)polarity;
        return value >= (int)QrPolarity.Automatic && value <= (int)QrPolarity.LightOnDark;
    }

    /// <summary>Determines whether a module value is defined.</summary>
    /// <param name="moduleValue">The module value to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsModuleValueDefined(QrModuleValue moduleValue)
    {
        int value = (int)moduleValue;
        return value >= (int)QrModuleValue.Unknown && value <= (int)QrModuleValue.Dark;
    }

    /// <summary>Determines whether a module role is defined.</summary>
    /// <param name="moduleRole">The module role to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsModuleRoleDefined(QrModuleRole moduleRole)
    {
        int value = (int)moduleRole;
        return value >= (int)QrModuleRole.Unknown && value <= (int)QrModuleRole.Remainder;
    }

    /// <summary>Determines whether a failure reason is defined.</summary>
    /// <param name="failureReason">The failure reason to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsFailureReasonDefined(QrFailureReason failureReason)
    {
        int value = (int)failureReason;
        return value >= (int)QrFailureReason.None && value <= (int)QrFailureReason.ResourceLimit;
    }

    /// <summary>Determines whether a quality grade is defined.</summary>
    /// <param name="qualityGrade">The quality grade to examine.</param>
    /// <returns><c>true</c> when the value is defined; otherwise, <c>false</c>.</returns>
    public bool IsQualityGradeDefined(QrQualityGrade qualityGrade)
    {
        int value = (int)qualityGrade;
        return value >= (int)QrQualityGrade.NotGraded && value <= (int)QrQualityGrade.Grade4;
    }
}
