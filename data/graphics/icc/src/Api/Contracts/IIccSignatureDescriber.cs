namespace icc;

/// <summary>
/// Provides stable human-readable descriptions for ICC signatures and public classifications.
/// </summary>
public interface IIccSignatureDescriber
{
    /// <summary>
    /// Parses a four-character ASCII signature or an eight-digit hexadecimal signature.
    /// </summary>
    /// <param name="value">The four-character text or <c>0x12345678</c> representation.</param>
    /// <returns>The unsigned signature value.</returns>
    uint ParseFourCc(string value);

    /// <summary>
    /// Formats a signature as four printable ASCII characters or hexadecimal when it is not printable.
    /// </summary>
    /// <param name="value">The unsigned signature value.</param>
    /// <returns>The printable ASCII or hexadecimal representation.</returns>
    string FormatFourCc(uint value);

    /// <summary>
    /// Formats a signature as exactly eight uppercase hexadecimal digits with a <c>0x</c> prefix.
    /// </summary>
    /// <param name="value">The unsigned signature value.</param>
    /// <returns>The hexadecimal representation.</returns>
    string FormatFourCcHex(uint value);

    /// <summary>
    /// Describes a registered profile-class signature.
    /// </summary>
    /// <param name="value">The signature value.</param>
    /// <returns>The registered name or an unknown/private representation.</returns>
    string DescribeProfileClassSignature(uint value);

    /// <summary>
    /// Describes a registered colour-space signature.
    /// </summary>
    /// <param name="value">The signature value.</param>
    /// <returns>The registered name or an unknown/private representation.</returns>
    string DescribeColorSpaceSignature(uint value);

    /// <summary>
    /// Describes a registered tag signature.
    /// </summary>
    /// <param name="value">The signature value.</param>
    /// <returns>The registered name or an unknown/private representation.</returns>
    string DescribeTagSignature(uint value);

    /// <summary>
    /// Describes a registered tag-type signature.
    /// </summary>
    /// <param name="value">The signature value.</param>
    /// <returns>The registered name or an unknown/private representation.</returns>
    string DescribeTypeSignature(uint value);

    /// <summary>
    /// Describes a registered processing-element signature.
    /// </summary>
    /// <param name="value">The signature value.</param>
    /// <returns>The registered name or an unknown/private representation.</returns>
    string DescribeProcessingElementSignature(uint value);

    /// <summary>
    /// Describes a registered ICC.2 array signature.
    /// </summary>
    /// <param name="value">The signature value.</param>
    /// <returns>The registered name or an unknown/private representation.</returns>
    string DescribeArraySignature(uint value);

    /// <summary>
    /// Describes a registered ICC.2 structure signature.
    /// </summary>
    /// <param name="value">The signature value.</param>
    /// <returns>The registered name or an unknown/private representation.</returns>
    string DescribeStructureSignature(uint value);

    /// <summary>
    /// Describes a registered device-technology signature.
    /// </summary>
    /// <param name="value">The signature value.</param>
    /// <returns>The registered name or an unknown/private representation.</returns>
    string DescribeTechnologySignature(uint value);

    /// <summary>
    /// Describes a registered colorimetric image-state signature.
    /// </summary>
    /// <param name="value">The signature value.</param>
    /// <returns>The registered name or an unknown/private representation.</returns>
    string DescribeImageStateSignature(uint value);

    /// <summary>
    /// Describes a profile generation.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeVersionKind(IccVersionKind value);

    /// <summary>
    /// Describes a profile class.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeProfileClass(IccProfileClass value);

    /// <summary>
    /// Describes a colour-space family.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeColorSpaceKind(IccColorSpaceKind value);

    /// <summary>
    /// Describes a Profile Connection Space family.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribePcsKind(IccPcsKind value);

    /// <summary>
    /// Describes a rendering intent.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeRenderingIntent(IccRenderingIntent value);

    /// <summary>
    /// Describes a tag-data representation.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeTagDataKind(IccTagDataKind value);

    /// <summary>
    /// Describes a tone-curve representation.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeCurveKind(IccCurveKind value);

    /// <summary>
    /// Describes a curve-segment representation.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeCurveSegmentKind(IccCurveSegmentKind value);

    /// <summary>
    /// Describes a processing-element representation.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeProcessingElementKind(IccProcessingElementKind value);

    /// <summary>
    /// Describes an interpolation algorithm.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeInterpolationKind(IccInterpolationKind value);

    /// <summary>
    /// Describes a validation severity.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeValidationSeverity(IccValidationSeverity value);

    /// <summary>
    /// Describes a validation mode.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeValidationMode(IccValidationMode value);

    /// <summary>
    /// Describes a write mode.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeWriteMode(IccWriteMode value);

    /// <summary>
    /// Describes a read mode.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeReadMode(IccReadMode value);

    /// <summary>
    /// Describes a transform direction.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeTransformDirection(IccTransformDirection value);

    /// <summary>
    /// Describes an intent fallback policy.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeIntentFallback(IccIntentFallback value);

    /// <summary>
    /// Describes a PCS encoding.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribePcsEncodingKind(IccPcsEncodingKind value);

    /// <summary>
    /// Describes a colour-difference formula.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeColorDifferenceFormula(IccColorDifferenceFormula value);

    /// <summary>
    /// Describes an inspection detail level.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeInspectionDetail(IccInspectionDetail value);

    /// <summary>
    /// Describes a gamut status.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeGamutStatus(IccGamutStatus value);

    /// <summary>
    /// Describes an embedded-profile carrier format.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeEmbeddedFormat(IccEmbeddedFormat value);

    /// <summary>
    /// Describes an embedded-profile operation.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeEmbeddedOperation(IccEmbeddedOperation value);

    /// <summary>
    /// Describes a measurement observer.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeMeasurementObserver(IccMeasurementObserver value);

    /// <summary>
    /// Describes a measurement geometry.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeMeasurementGeometry(IccMeasurementGeometry value);

    /// <summary>
    /// Describes a standard illuminant.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeStandardIlluminant(IccStandardIlluminant value);

    /// <summary>
    /// Describes an operation status.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>A stable description.</returns>
    string DescribeOperationStatus(IccOperationStatus value);
}
