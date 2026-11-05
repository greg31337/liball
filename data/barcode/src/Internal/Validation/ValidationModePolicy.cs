namespace barcode;

internal static class ValidationModePolicy
{
    internal static BarcodeValidationSeverity ResolveSeverity(
        BarcodeValidationMode mode,
        string code)
    {
        if (mode == BarcodeValidationMode.Tolerant &&
            IsRecoverableIssue(code))
        {
            return BarcodeValidationSeverity.Warning;
        }

        return BarcodeValidationSeverity.Error;
    }

    internal static bool IsConformant(BarcodeValidationIssue[] issues)
    {
        return issues.Length == 0;
    }

    static bool IsRecoverableIssue(string code)
    {
        return HasValidationCodePart(code, "CHECKSUM_INVALID") ||
            HasValidationCodePart(code, "CHECK_DIGIT_INVALID") ||
            HasValidationCodePart(code, "CHECK_CHARACTER_INVALID") ||
            HasValidationCodePart(code, "PATTERN_INVALID") ||
            HasValidationCodePart(code, "GUARD_OR_PATTERN_INVALID") ||
            HasValidationCodePart(code, "MODULE_PLACEMENT_INVALID") ||
            HasValidationCodePart(code, "ROW_INDICATOR_INVALID") ||
            HasValidationCodePart(code, "QUIET_ZONE_INSUFFICIENT") ||
            HasValidationCodePart(code, "BEARER_BAR_INVALID") ||
            HasValidationCodePart(code, "BEARER_BARS_MISSING") ||
            HasValidationCodePart(code, "BEARER_THICKNESS_INVALID") ||
            HasValidationCodePart(code, "IDENTIFIER_INVALID") ||
            HasValidationCodePart(code, "CONTRAST_INSUFFICIENT") ||
            HasValidationCodePart(code, "MODULATION_INSUFFICIENT") ||
            HasValidationCodePart(code, "MODULE_SIZE_INCONSISTENT") ||
            HasValidationCodePart(code, "MODULE_SIZE_OUT_OF_RANGE") ||
            HasValidationCodePart(code, "BAR_HEIGHT_INSUFFICIENT") ||
            HasValidationCodePart(code, "ASPECT_RATIO_MISMATCH") ||
            HasValidationCodePart(code, "HRT_CLEARANCE_INSUFFICIENT") ||
            HasValidationCodePart(code, "PHYSICAL_WIDTH_EXCEEDED") ||
            HasValidationCodePart(code, "SKEW_EXCESSIVE") ||
            HasValidationCodePart(code, "PERSPECTIVE_EXCESSIVE") ||
            HasValidationCodePart(code, "AXIAL_NONUNIFORMITY") ||
            HasValidationCodePart(code, "FIXED_PATTERN_DAMAGED") ||
            HasValidationCodePart(code, "ECC_MARGIN_LOW") ||
            HasValidationCodePart(code, "DECODE_CONFIDENCE_LOW") ||
            HasValidationCodePart(code, "TRUNCATED") ||
            HasValidationCodePart(code, "RECOVERY_APPLIED");
    }

    static bool HasValidationCodePart(string code, string part)
    {
        return code.IndexOf(
            part,
            global::System.StringComparison.Ordinal) >= 0;
    }
}
