namespace qrcode;

internal class QualityGradingProfile
{
    // Rules must describe the actual measurement definition and the selected
    // symbology/application profile. Raw fixed-pattern damage fractions, minimum
    // module modulation and decoder RS budgets are not interchangeable with
    // verifier parameters merely because their names match.
    // No default normative thresholds are inferred from public verifier guides;
    // the controlled edition's tables require the source review in specs/.
    internal QrSymbolKind SymbolKind;
    internal QualityMetricRule[] Rules = new QualityMetricRule[0];
}
