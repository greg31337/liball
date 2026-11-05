namespace qrcode;

internal class FixedPatternDamageMeasurer
{
    // The reference and roles must come from canonical profile construction,
    // with verified codewords when payload/ECC damage is to be measured. Caller
    // role labels and confidence do not change the observed module values.
    // Results describe differences from that reference, not BCH/RS recoverability
    // or calibrated verifier grades. Quiet zones and remainder bits stay separate.
    internal static ModuleDamageMeasurement Measure(QrMatrix observed, BitMatrixBuffer expectedEncoding,
        QrModuleRole[] expectedRoles, int maximumSamples)
    {
        int area = ValidateInput(observed, expectedEncoding, expectedRoles, maximumSamples);
        ModuleDamageMeasurement result = new ModuleDamageMeasurement { DamagedModules = new bool[area] };
        for (int row = 0; row < observed.Height; row++)
        {
            for (int column = 0; column < observed.Width; column++)
            {
                int index = row * observed.Width + column;
                QrModuleRole role = expectedRoles[index];
                int roleIndex = (int)role;
                bool quiet = row < observed.QuietZoneTop || row >= observed.Height - observed.QuietZoneBottom
                    || column < observed.QuietZoneLeft || column >= observed.Width - observed.QuietZoneRight;
                if (roleIndex < (int)QrModuleRole.QuietZone || roleIndex > (int)QrModuleRole.Remainder
                    || quiet != (role == QrModuleRole.QuietZone))
                {
                    throw new QrInputException("Reference roles must be defined and agree with the encoding region.", "expectedRoles");
                }
                bool expectedDark = !quiet && expectedEncoding.GetModule(row - observed.QuietZoneTop, column - observed.QuietZoneLeft);
                QrModuleValue expected = expectedDark ? QrModuleValue.Dark : QrModuleValue.Light;
                QrModuleValue actual = observed.Values[index];
                result.ModuleCounts[roleIndex]++;
                if (actual != expected)
                {
                    result.DamagedModules[index] = true;
                    result.DamageCounts[roleIndex]++;
                    if (actual == QrModuleValue.Unknown) { result.UnknownCounts[roleIndex]++; }
                    else if (actual != QrModuleValue.Dark && actual != QrModuleValue.Light) { result.InvalidCounts[roleIndex]++; }
                    else { result.MismatchCounts[roleIndex]++; }
                }
            }
        }
        for (int roleIndex = (int)QrModuleRole.Finder; roleIndex <= (int)QrModuleRole.DarkModule; roleIndex++)
        {
            if (roleIndex == (int)QrModuleRole.Format || roleIndex == (int)QrModuleRole.Version)
            {
                result.CriticalMetadataModuleCount += result.ModuleCounts[roleIndex];
                result.CriticalMetadataDamageCount += result.DamageCounts[roleIndex];
            }
            else
            {
                result.FixedPatternModuleCount += result.ModuleCounts[roleIndex];
                result.FixedPatternDamageCount += result.DamageCounts[roleIndex];
            }
        }
        result.PayloadDamageCount = result.DamageCounts[(int)QrModuleRole.Data];
        result.ErrorCorrectionDamageCount = result.DamageCounts[(int)QrModuleRole.ErrorCorrection];
        if (result.FixedPatternModuleCount > 0)
        {
            result.FixedPatternDamageFraction = (double)result.FixedPatternDamageCount / result.FixedPatternModuleCount;
        }
        if (result.CriticalMetadataModuleCount > 0)
        {
            result.CriticalMetadataDamageFraction = (double)result.CriticalMetadataDamageCount / result.CriticalMetadataModuleCount;
        }
        return result;
    }

    static int ValidateInput(QrMatrix observed, BitMatrixBuffer expectedEncoding, QrModuleRole[] expectedRoles, int maximumSamples)
    {
        if (observed == null || expectedEncoding == null || expectedRoles == null)
        {
            throw new QrInputException("Observed modules, reference modules, and reference roles are required.", "matrix");
        }
        CheckedDimensions.ValidateDimensionBounds(observed.Width, observed.Height,
            QrLimits.DefaultMaximumImageDimension, QrLimits.DefaultMaximumImageDimension, maximumSamples);
        int area = CheckedDimensions.CalculateArea(observed.Width, observed.Height, maximumSamples);
        if (observed.Values == null || observed.Values.Length != area || expectedRoles.Length != area)
        {
            throw new QrInputException("Module and reference-role buffers must match the observed dimensions.", "matrix");
        }
        if (observed.QuietZoneLeft < 0 || observed.QuietZoneRight < 0 || observed.QuietZoneTop < 0 || observed.QuietZoneBottom < 0
            || (long)observed.Width - observed.QuietZoneLeft - observed.QuietZoneRight != expectedEncoding.GetWidth()
            || (long)observed.Height - observed.QuietZoneTop - observed.QuietZoneBottom != expectedEncoding.GetHeight())
        {
            throw new QrInputException("The reference must match the observed encoding region.", "matrix");
        }
        CheckedDimensions.CalculateSum(area, 512, QrLimits.DefaultMaximumAllocationBytes, "MaximumAllocationBytes");
        return area;
    }
}
