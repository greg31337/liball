namespace icc;

/// <summary>
/// Converts FourCC values and provides stable names for registered signatures and public classifications.
/// </summary>
public class IccSignatureDescriber : IIccSignatureDescriber
{
    /// <summary>
    /// Initializes a signature describer.
    /// </summary>
    public IccSignatureDescriber()
    {
    }

    /// <summary>
    /// Performs the parse four cc operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public uint ParseFourCc(string value)
    {
        return FourCcCodec.Parse(value);
    }

    /// <summary>
    /// Performs the format four cc operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string FormatFourCc(uint value)
    {
        return FourCcCodec.Format(value);
    }

    /// <summary>
    /// Performs the format four cc hex operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string FormatFourCcHex(uint value)
    {
        return FourCcCodec.FormatHex(value);
    }

    /// <summary>
    /// Performs the describe profile class signature operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeProfileClassSignature(uint value)
    {
        return SignatureDatabase.DescribeProfileClass(value);
    }

    /// <summary>
    /// Performs the describe color space signature operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeColorSpaceSignature(uint value)
    {
        return SignatureDatabase.DescribeColorSpace(value);
    }

    /// <summary>
    /// Performs the describe tag signature operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeTagSignature(uint value)
    {
        return SignatureDatabase.DescribeTag(value);
    }

    /// <summary>
    /// Performs the describe type signature operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeTypeSignature(uint value)
    {
        return SignatureDatabase.DescribeType(value);
    }

    /// <summary>
    /// Performs the describe processing element signature operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeProcessingElementSignature(uint value)
    {
        return SignatureDatabase.DescribeProcessingElement(value);
    }

    /// <summary>
    /// Performs the describe array signature operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeArraySignature(uint value)
    {
        return SignatureDatabase.DescribeArray(value);
    }

    /// <summary>
    /// Performs the describe structure signature operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeStructureSignature(uint value)
    {
        return SignatureDatabase.DescribeStructure(value);
    }

    /// <summary>
    /// Performs the describe technology signature operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeTechnologySignature(uint value)
    {
        return SignatureDatabase.DescribeTechnology(value);
    }

    /// <summary>
    /// Performs the describe image state signature operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeImageStateSignature(uint value)
    {
        return SignatureDatabase.DescribeImageState(value);
    }

    /// <summary>
    /// Performs the describe version kind operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeVersionKind(IccVersionKind value)
    {
        return DescriptionFormatter.DescribeVersionKind(value);
    }

    /// <summary>
    /// Performs the describe profile class operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeProfileClass(IccProfileClass value)
    {
        return DescriptionFormatter.DescribeProfileClassKind(value);
    }

    /// <summary>
    /// Performs the describe color space kind operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeColorSpaceKind(IccColorSpaceKind value)
    {
        return DescriptionFormatter.DescribeColorSpaceKind(value);
    }

    /// <summary>
    /// Performs the describe pcs kind operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribePcsKind(IccPcsKind value)
    {
        return DescriptionFormatter.DescribePcsKind(value);
    }

    /// <summary>
    /// Performs the describe rendering intent operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeRenderingIntent(IccRenderingIntent value)
    {
        return DescriptionFormatter.DescribeRenderingIntentKind(value);
    }

    /// <summary>
    /// Performs the describe tag data kind operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeTagDataKind(IccTagDataKind value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe curve kind operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeCurveKind(IccCurveKind value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe curve segment kind operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeCurveSegmentKind(IccCurveSegmentKind value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe processing element kind operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeProcessingElementKind(IccProcessingElementKind value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe interpolation kind operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeInterpolationKind(IccInterpolationKind value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe validation severity operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeValidationSeverity(IccValidationSeverity value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe validation mode operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeValidationMode(IccValidationMode value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe write mode operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeWriteMode(IccWriteMode value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe read mode operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeReadMode(IccReadMode value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe transform direction operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeTransformDirection(IccTransformDirection value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe intent fallback operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeIntentFallback(IccIntentFallback value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe pcs encoding kind operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribePcsEncodingKind(IccPcsEncodingKind value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe color difference formula operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeColorDifferenceFormula(IccColorDifferenceFormula value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe inspection detail operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeInspectionDetail(IccInspectionDetail value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe gamut status operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeGamutStatus(IccGamutStatus value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe embedded format operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeEmbeddedFormat(IccEmbeddedFormat value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe embedded operation operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeEmbeddedOperation(IccEmbeddedOperation value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe measurement observer operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeMeasurementObserver(IccMeasurementObserver value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe measurement geometry operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeMeasurementGeometry(IccMeasurementGeometry value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe standard illuminant operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeStandardIlluminant(IccStandardIlluminant value)
    {
        return value.ToString();
    }

    /// <summary>
    /// Performs the describe operation status operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The stable operation result.</returns>
    public string DescribeOperationStatus(IccOperationStatus value)
    {
        return value.ToString();
    }
}
