namespace barcode.Tests;

public class ValidationCoverageMatrixTests
{
    [Test]
    public void EveryIssueCodeSupportsEverySeverityAndTargetContext()
    {
        AssertIssueCatalog(DataIssueCodes, "Data");
        AssertIssueCatalog(SymbolIssueCodes, "Symbol");
        AssertIssueCatalog(RenderIssueCodes, "Render");
        AssertIssueCatalog(ImageIssueCodes, "Image");
    }

    [Test]
    public void EveryIssueCodeHasStableStrictAndTolerantSeverity()
    {
        global::System.Collections.Generic.HashSet<string> codes =
            new global::System.Collections.Generic.HashSet<string>();
        AddCodes(codes, DataIssueCodes);
        AddCodes(codes, SymbolIssueCodes);
        AddCodes(codes, RenderIssueCodes);
        AddCodes(codes, ImageIssueCodes);
        int warningCount = 0;
        int errorCount = 0;
        foreach (string code in codes)
        {
            BarcodeValidationSeverity strict =
                ValidationModePolicy.ResolveSeverity(
                    BarcodeValidationMode.Strict,
                    code);
            BarcodeValidationSeverity tolerant =
                ValidationModePolicy.ResolveSeverity(
                    BarcodeValidationMode.Tolerant,
                    code);
            BarcodeValidationSeverity expectedTolerant =
                IsExpectedRecoverable(code)
                    ? BarcodeValidationSeverity.Warning
                    : BarcodeValidationSeverity.Error;

            Assert.Multiple(() =>
            {
                Assert.That(
                    strict,
                    Is.EqualTo(BarcodeValidationSeverity.Error),
                    code);
                Assert.That(
                    tolerant,
                    Is.EqualTo(expectedTolerant),
                    code);
            });
            if (tolerant == BarcodeValidationSeverity.Warning)
            {
                warningCount = warningCount + 1;
            }
            else
            {
                errorCount = errorCount + 1;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(codes, Has.Count.EqualTo(257));
            Assert.That(warningCount, Is.GreaterThan(0));
            Assert.That(errorCount, Is.GreaterThan(0));
        });
    }

    [Test]
    public void EveryPublicValidationTargetPreservesBothModesAndFatalErrors()
    {
        BarcodeValidator validator = new BarcodeValidator();
        BarcodeValidationRequest strictRequest = new BarcodeValidationRequest();
        strictRequest.Mode = BarcodeValidationMode.Strict;
        BarcodeValidationRequest tolerantRequest = new BarcodeValidationRequest();
        tolerantRequest.Mode = BarcodeValidationMode.Tolerant;

        AssertTargetPair(
            validator.ValidateBarcodeData(strictRequest),
            validator.ValidateBarcodeData(tolerantRequest),
            "ENCODE_REQUEST_MISSING");
        AssertTargetPair(
            validator.ValidateBarcodeSymbol(strictRequest),
            validator.ValidateBarcodeSymbol(tolerantRequest),
            "SYMBOL_MISSING");
        AssertTargetPair(
            validator.ValidateBarcodeRender(strictRequest),
            validator.ValidateBarcodeRender(tolerantRequest),
            "RENDER_REQUEST_MISSING");
        AssertTargetPair(
            validator.ValidateBarcodeImage(strictRequest),
            validator.ValidateBarcodeImage(tolerantRequest),
            "IMAGE_MISSING");
    }

    static void AssertIssueCatalog(string[] codes, string target)
    {
        BarcodeValidationSeverity[] severities =
            new BarcodeValidationSeverity[]
            {
                BarcodeValidationSeverity.Information,
                BarcodeValidationSeverity.Warning,
                BarcodeValidationSeverity.Error
            };
        int codeIndex = 0;
        while (codeIndex < codes.Length)
        {
            int severityIndex = 0;
            while (severityIndex < severities.Length)
            {
                BarcodeValidationIssue issue = ValidationIssueFactory.Create(
                    severities[severityIndex],
                    codes[codeIndex],
                    "Validation coverage.",
                    target,
                    target,
                    -1,
                    "conformant",
                    "nonconformant");
                Assert.Multiple(() =>
                {
                    Assert.That(issue.Code, Is.EqualTo(codes[codeIndex]));
                    Assert.That(
                        issue.Severity,
                        Is.EqualTo(severities[severityIndex]));
                    Assert.That(issue.Standard, Is.EqualTo(target));
                    Assert.That(issue.Field, Is.EqualTo(target));
                });
                severityIndex = severityIndex + 1;
            }

            codeIndex = codeIndex + 1;
        }
    }

    static void AssertTargetPair(
        BarcodeValidationReport strict,
        BarcodeValidationReport tolerant,
        string expectedCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(strict.Mode, Is.EqualTo(BarcodeValidationMode.Strict));
            Assert.That(tolerant.Mode, Is.EqualTo(BarcodeValidationMode.Tolerant));
            Assert.That(strict.IsConformant, Is.False);
            Assert.That(tolerant.IsConformant, Is.False);
            Assert.That(strict.Issues, Has.Length.EqualTo(1));
            Assert.That(tolerant.Issues, Has.Length.EqualTo(1));
            Assert.That(strict.Issues![0].Code, Is.EqualTo(expectedCode));
            Assert.That(tolerant.Issues![0].Code, Is.EqualTo(expectedCode));
            Assert.That(
                strict.Issues[0].Severity,
                Is.EqualTo(BarcodeValidationSeverity.Error));
            Assert.That(
                tolerant.Issues[0].Severity,
                Is.EqualTo(BarcodeValidationSeverity.Error));
            Assert.That(strict.Metrics, Is.Empty);
            Assert.That(tolerant.Metrics, Is.Empty);
            Assert.That(strict.Diagnostics, Is.Empty);
            Assert.That(tolerant.Diagnostics, Is.Empty);
        });
    }

    static void AddCodes(
        global::System.Collections.Generic.HashSet<string> destination,
        string[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static bool IsExpectedRecoverable(string code)
    {
        return code.IndexOf("CHECKSUM_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("CHECK_DIGIT_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("CHECK_CHARACTER_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("PATTERN_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("GUARD_OR_PATTERN_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("MODULE_PLACEMENT_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("ROW_INDICATOR_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("QUIET_ZONE_INSUFFICIENT", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("BEARER_BAR_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("BEARER_BARS_MISSING", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("BEARER_THICKNESS_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("IDENTIFIER_INVALID", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("CONTRAST_INSUFFICIENT", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("MODULATION_INSUFFICIENT", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("MODULE_SIZE_INCONSISTENT", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("MODULE_SIZE_OUT_OF_RANGE", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("BAR_HEIGHT_INSUFFICIENT", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("ASPECT_RATIO_MISMATCH", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("HRT_CLEARANCE_INSUFFICIENT", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("PHYSICAL_WIDTH_EXCEEDED", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("SKEW_EXCESSIVE", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("PERSPECTIVE_EXCESSIVE", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("AXIAL_NONUNIFORMITY", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("FIXED_PATTERN_DAMAGED", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("ECC_MARGIN_LOW", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("DECODE_CONFIDENCE_LOW", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("TRUNCATED", global::System.StringComparison.Ordinal) >= 0 ||
            code.IndexOf("RECOVERY_APPLIED", global::System.StringComparison.Ordinal) >= 0;
    }

    static readonly string[] DataIssueCodes = new string[]
    {
        "CODABAR_CHARACTER_INVALID",
        "CODABAR_CHECKSUM_MODE_INVALID",
        "CODABAR_CHECK_CHARACTER_INVALID",
        "CODABAR_CHECK_CHARACTER_MISSING",
        "CODABAR_DATA_MISSING",
        "CODABAR_FRAMING_INCOMPLETE",
        "CODABAR_GAP_INVALID",
        "CODABAR_RATIO_INVALID",
        "CODABAR_TERMINAL_CONFLICT",
        "CODABAR_TERMINAL_INVALID",
        "CODE128_CAPACITY_EXCEEDED",
        "CODE128_CODE_SET_CHARACTER_INVALID",
        "CODE128_CODE_SET_C_DATA_INVALID",
        "CODE128_CODE_SET_INVALID",
        "CODE39_CHARACTER_INVALID",
        "CODE39_CHARACTER_MODE_INVALID",
        "CODE39_CHECKSUM_MODE_INVALID",
        "CODE39_CHECK_CHARACTER_INVALID",
        "CODE39_CHECK_CHARACTER_MISSING",
        "CODE39_GAP_INVALID",
        "CODE39_RATIO_INVALID",
        "DATAMATRIX_APPEND_COUNT_INVALID",
        "DATAMATRIX_APPEND_FILE_ID_INVALID",
        "DATAMATRIX_APPEND_FILE_ID_RANGE_INVALID",
        "DATAMATRIX_APPEND_FORMAT_INVALID",
        "DATAMATRIX_APPEND_INDEX_INVALID",
        "DATAMATRIX_APPEND_LAST_SEGMENT_CONFLICT",
        "DATAMATRIX_CAPACITY_EXCEEDED",
        "DATAMATRIX_DIMENSIONS_INCOMPLETE",
        "DATAMATRIX_DIMENSIONS_NEGATIVE",
        "DATAMATRIX_DIMENSIONS_UNSUPPORTED",
        "DATAMATRIX_ECI_INVALID",
        "DATAMATRIX_EDIFACT_CHARACTER_INVALID",
        "DATAMATRIX_ENCODING_MODE_INVALID",
        "DATAMATRIX_MACRO_TYPE_INVALID",
        "DATAMATRIX_SHAPE_INVALID",
        "DATAMATRIX_SYMBOL_CAPACITY_EXCEEDED",
        "DATAMATRIX_X12_CHARACTER_INVALID",
        "EAN_UPC_ADDON_LENGTH_INVALID",
        "EAN_UPC_ADDON_LENGTH_NOT_ALLOWED",
        "EAN_UPC_ADDON_LENGTH_POLICY_EMPTY",
        "EAN_UPC_ADDON_LENGTH_POLICY_INVALID",
        "EAN_UPC_ADDON_NON_DIGIT",
        "EAN_UPC_ADDON_NOT_APPLICABLE",
        "EAN_UPC_ADDON_REQUIRED",
        "EAN_UPC_CHECKSUM_MODE_INVALID",
        "EAN_UPC_LENGTH_INVALID",
        "EAN_UPC_NESTED_ADDON_INVALID",
        "EAN_UPC_NON_DIGIT",
        "ENCODE_CHARACTER_ENCODING_EMPTY",
        "ENCODE_CHARACTER_ENCODING_INVALID",
        "ENCODE_CHECKSUM_MODE_INVALID",
        "ENCODE_CHECKSUM_MODE_NOT_APPLICABLE",
        "ENCODE_CUSTOM_TEXT_MISSING",
        "ENCODE_ECI_CONFLICT",
        "ENCODE_ECI_NEGATIVE",
        "ENCODE_ECI_NOT_APPLICABLE",
        "ENCODE_ECI_SEGMENT_ASSIGNMENT_INVALID",
        "ENCODE_ECI_SEGMENT_NOT_APPLICABLE",
        "ENCODE_ECI_SEGMENT_NULL",
        "ENCODE_ECI_SEGMENT_ORDER_INVALID",
        "ENCODE_ECI_SEGMENT_RANGE_INVALID",
        "ENCODE_FORMAT_CONFLICT",
        "ENCODE_FORMAT_UNSUPPORTED",
        "ENCODE_OPTION_COMBINATION_INVALID",
        "ENCODE_OPTION_NOT_APPLICABLE",
        "ENCODE_PAYLOAD_APPEND_CONFLICT",
        "ENCODE_PAYLOAD_APPEND_NOT_APPLICABLE",
        "ENCODE_PAYLOAD_EMPTY",
        "ENCODE_PAYLOAD_MISSING",
        "ENCODE_REQUEST_MISSING",
        "ENCODE_STRUCTURED_APPEND_CONFLICT",
        "ENCODE_STRUCTURED_APPEND_NOT_APPLICABLE",
        "ENCODE_TEXT_MODE_INVALID",
        "ENCODE_TEXT_PAYLOAD_REQUIRED",
        "GS1128_CAPACITY_EXCEEDED",
        "GS1_DATAMATRIX_ECI_INVALID",
        "ITF14_BEARER_THICKNESS_INVALID",
        "ITF14_CHECK_DIGIT_INVALID",
        "ITF14_CHECK_DIGIT_STRIP_INVALID",
        "ITF14_LENGTH_INVALID",
        "ITF_CALCULATE_LENGTH_INVALID",
        "ITF_CHECKSUM_MODE_INVALID",
        "ITF_CHECK_DIGIT_INVALID",
        "ITF_CHECK_DIGIT_MISSING",
        "ITF_ENCODED_LENGTH_ODD",
        "ITF_LENGTH_NOT_ALLOWED",
        "ITF_LENGTH_POLICY_EMPTY",
        "ITF_LENGTH_POLICY_INVALID",
        "ITF_NON_DIGIT",
        "ITF_PROFILE_OPTION_NOT_APPLICABLE",
        "ITF_RATIO_INVALID",
        "MACRO_PDF417_APPEND_FORMAT_INVALID",
        "MACRO_PDF417_CHECKSUM_INVALID",
        "MACRO_PDF417_COUNT_INVALID",
        "MACRO_PDF417_FILE_ID_GROUP_INVALID",
        "MACRO_PDF417_FILE_ID_INVALID",
        "MACRO_PDF417_FILE_SIZE_INVALID",
        "MACRO_PDF417_FORMAT_CONFLICT",
        "MACRO_PDF417_INDEX_COUNT_CONFLICT",
        "MACRO_PDF417_INDEX_INVALID",
        "MACRO_PDF417_LAST_SEGMENT_CONFLICT",
        "MACRO_PDF417_METADATA_MISSING",
        "MACRO_PDF417_OPTION_WITHOUT_METADATA",
        "MACRO_PDF417_TEXT_FIELD_CHARACTER_INVALID",
        "MACRO_PDF417_TEXT_FIELD_EMPTY",
        "MACRO_PDF417_TIMESTAMP_INVALID",
        "PDF417_CAPACITY_EXCEEDED",
        "PDF417_COLUMNS_INVALID",
        "PDF417_COMPACTION_MODE_INVALID",
        "PDF417_DIMENSION_CAPACITY_INVALID",
        "PDF417_ECC_LEVEL_INVALID",
        "PDF417_ECI_INVALID",
        "PDF417_NUMERIC_CHARACTER_INVALID",
        "PDF417_ROWS_INVALID",
        "PDF417_SYMBOL_CAPACITY_EXCEEDED",
        "PDF417_TEXT_CHARACTER_INVALID",
        "PDF417_VARIANT_FORMAT_CONFLICT",
        "PDF417_VARIANT_INVALID",
        "UPCE_CHECK_DIGIT_MISSING",
        "UPCE_COMPRESSION_NOT_APPLICABLE",
        "UPCE_COMPRESSION_NOT_LOSSLESS",
        "UPCE_LENGTH_INVALID",
        "UPCE_NUMBER_SYSTEM_CONFLICT",
        "UPCE_NUMBER_SYSTEM_INVALID",
        "UPCE_NUMBER_SYSTEM_NOT_APPLICABLE"
    };

    static readonly string[] SymbolIssueCodes = new string[]
    {
        "CODABAR_CHECKSUM_INVALID",
        "CODABAR_CHECKSUM_MISSING",
        "CODABAR_CODEWORDS_INVALID",
        "CODABAR_START_STOP_OR_PATTERN_INVALID",
        "CODE128_CAPACITY_EXCEEDED",
        "CODE128_CHECKSUM_INVALID",
        "CODE128_CODEWORDS_MISSING",
        "CODE128_CODEWORD_PATTERN_INVALID",
        "CODE128_CODEWORD_SEQUENCE_INVALID",
        "CODE128_PATTERN_INVALID",
        "CODE128_START_PATTERN_INVALID",
        "CODE128_STOP_PATTERN_INVALID",
        "CODE39_CHECKSUM_INVALID",
        "CODE39_CHECKSUM_MISSING",
        "CODE39_CODEWORDS_MISSING",
        "CODE39_CODEWORD_INVALID",
        "CODE39_START_STOP_OR_PATTERN_INVALID",
        "DATAMATRIX_CHECKSUM_METADATA_INVALID",
        "DATAMATRIX_CODEWORD_COUNT_INVALID",
        "DATAMATRIX_CODEWORD_VALUE_INVALID",
        "DATAMATRIX_DIMENSIONS_UNSUPPORTED",
        "DATAMATRIX_ECC_INVALID",
        "DATAMATRIX_FINDER_PATTERN_INVALID",
        "DATAMATRIX_MODULE_PLACEMENT_INVALID",
        "EAN_UPC_ADDON_LENGTH_INVALID",
        "EAN_UPC_CHECKSUM_METADATA_INVALID",
        "EAN_UPC_CHECK_OR_LENGTH_INVALID",
        "EAN_UPC_GUARD_OR_PATTERN_INVALID",
        "EAN_UPC_PAYLOAD_INVALID",
        "GS1128_LEADING_FNC1_MISSING",
        "ITF14_BEARER_BARS_MISSING",
        "ITF14_BEARER_BAR_INVALID",
        "ITF14_CHECKSUM_MISSING",
        "ITF14_DIGIT_COUNT_INVALID",
        "ITF14_HORIZONTAL_BEARER_BARS_INVALID",
        "ITF14_VERTICAL_BEARER_BARS_INVALID",
        "ITF_CHECKSUM_INVALID",
        "ITF_CODEWORDS_INVALID",
        "ITF_START_STOP_OR_PATTERN_INVALID",
        "MACRO_PDF417_METADATA_MISSING",
        "PDF417_CHECKSUM_METADATA_INVALID",
        "PDF417_CODEWORD_COUNT_INVALID",
        "PDF417_CODEWORD_PATTERN_INVALID",
        "PDF417_CODEWORD_VALUE_INVALID",
        "PDF417_DIMENSIONS_INVALID",
        "PDF417_ECC_INVALID",
        "PDF417_LENGTH_OR_ECC_COUNT_INVALID",
        "PDF417_ROW_HEIGHT_INVALID",
        "PDF417_ROW_INDICATOR_INVALID",
        "PDF417_ROW_WIDTH_INVALID",
        "PDF417_START_STOP_PATTERN_INVALID",
        "PDF417_SYMBOL_CAPACITY_EXCEEDED",
        "PDF417_VARIANT_FORMAT_CONFLICT",
        "SYMBOL_BEARER_BARS_NOT_APPLICABLE",
        "SYMBOL_DIMENSIONS_INVALID",
        "SYMBOL_DIMENSION_CLASS_INVALID",
        "SYMBOL_FORMAT_CONFLICT",
        "SYMBOL_FORMAT_UNSUPPORTED",
        "SYMBOL_GS1_DATA_MISSING",
        "SYMBOL_GS1_STATE_INVALID",
        "SYMBOL_IDENTIFIER_INVALID",
        "SYMBOL_MISSING",
        "SYMBOL_MODULES_MISSING",
        "SYMBOL_MODULE_CAPACITY_EXCEEDED",
        "SYMBOL_MODULE_COUNT_INVALID",
        "SYMBOL_PAYLOAD_MISSING",
        "SYMBOL_QUIET_ZONE_INSUFFICIENT",
        "SYMBOL_QUIET_ZONE_MISSING",
        "UPCE_NUMBER_SYSTEM_INVALID"
    };

    static readonly string[] RenderIssueCodes = new string[]
    {
        "RENDER_ASPECT_RATIO_INVALID",
        "RENDER_ASPECT_RATIO_MISMATCH",
        "RENDER_BAR_HEIGHT_INSUFFICIENT",
        "RENDER_BAR_HEIGHT_INVALID",
        "RENDER_BAR_HEIGHT_NOT_APPLICABLE",
        "RENDER_COLOR_POLARITY_INVALID",
        "RENDER_CONTRAST_INSUFFICIENT",
        "RENDER_CUSTOM_QUIET_ZONE_MISSING",
        "RENDER_CUSTOM_TEXT_MISSING",
        "RENDER_DIMENSION_CAPACITY_EXCEEDED",
        "RENDER_DPI_INVALID",
        "RENDER_FOREGROUND_TRANSPARENT",
        "RENDER_FORMAT_INVALID",
        "RENDER_HEIGHT_MEASUREMENT_INVALID",
        "RENDER_HRT_BAR_EXTENSION_INVALID",
        "RENDER_HRT_BOUNDS_INVALID",
        "RENDER_HRT_CLEARANCE_INSUFFICIENT",
        "RENDER_HRT_HEIGHT_INVALID",
        "RENDER_HRT_RUN_INVALID",
        "RENDER_MODULE_HEIGHT_CONFLICT",
        "RENDER_MODULE_ROUNDING_INVALID",
        "RENDER_MODULE_SIZE_INVALID",
        "RENDER_MODULE_SIZE_OUT_OF_RANGE",
        "RENDER_MODULE_WIDTH_CONFLICT",
        "RENDER_OUTPUT_CAPACITY_EXCEEDED",
        "RENDER_PHYSICAL_HEIGHT_CONFLICT",
        "RENDER_PHYSICAL_SIZE_CONFLICT",
        "RENDER_PHYSICAL_SIZE_INVALID",
        "RENDER_PHYSICAL_WIDTH_EXCEEDED",
        "RENDER_PIXEL_SIZE_CONFLICT",
        "RENDER_QUIET_ZONE_INSUFFICIENT",
        "RENDER_QUIET_ZONE_NEGATIVE",
        "RENDER_QUIET_ZONE_POLICY_INVALID",
        "RENDER_REQUEST_MISSING",
        "RENDER_ROTATION_INVALID",
        "RENDER_SCALE_CAPACITY_EXCEEDED",
        "RENDER_SCALE_CONFLICT",
        "RENDER_SIZE_NEGATIVE",
        "RENDER_SVG_TEXT_NOT_APPLICABLE",
        "RENDER_SYMBOL_MISSING",
        "RENDER_TEXT_MODE_INVALID",
        "RENDER_TRANSPARENCY_UNSUPPORTED",
        "RENDER_WIDTH_MEASUREMENT_INVALID"
    };

    static readonly string[] ImageIssueCodes = new string[]
    {
        "IMAGE_AXIAL_NONUNIFORMITY",
        "IMAGE_BOUNDS_INVALID",
        "IMAGE_BOUNDS_MISSING",
        "IMAGE_BUFFER_BOUNDS_INVALID",
        "IMAGE_CAPACITY_EXCEEDED",
        "IMAGE_CONTRAST_INSUFFICIENT",
        "IMAGE_DECODE_CONFIDENCE_LOW",
        "IMAGE_DIMENSIONS_INVALID",
        "IMAGE_DPI_INVALID",
        "IMAGE_ECC_MARGIN_LOW",
        "IMAGE_FIXED_PATTERN_DAMAGED",
        "IMAGE_FORMAT_CONFLICT",
        "IMAGE_FORMAT_UNSUPPORTED",
        "IMAGE_MISSING",
        "IMAGE_MODULATION_INSUFFICIENT",
        "IMAGE_MODULE_SIZE_INCONSISTENT",
        "IMAGE_NOT_DECODED",
        "IMAGE_PERSPECTIVE_EXCESSIVE",
        "IMAGE_PIXELS_MISSING",
        "IMAGE_PIXEL_FORMAT_INVALID",
        "IMAGE_QUIET_ZONE_INFRINGEMENT",
        "IMAGE_RECOVERY_APPLIED",
        "IMAGE_SKEW_EXCESSIVE",
        "IMAGE_STRIDE_INVALID",
        "IMAGE_SYMBOL_CONTRAST_INSUFFICIENT",
        "IMAGE_TRUNCATED"
    };

}
