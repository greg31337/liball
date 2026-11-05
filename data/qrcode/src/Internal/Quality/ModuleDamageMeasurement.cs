namespace qrcode;

internal class ModuleDamageMeasurement
{
    // Arrays are indexed by QrModuleRole. Damage includes known mismatches,
    // unknown observations, and invalid observations, counted separately below.
    internal int[] ModuleCounts = new int[12];
    internal int[] DamageCounts = new int[12];
    internal int[] MismatchCounts = new int[12];
    internal int[] UnknownCounts = new int[12];
    internal int[] InvalidCounts = new int[12];
    internal bool[] DamagedModules = new bool[0];
    internal int FixedPatternModuleCount;
    internal int FixedPatternDamageCount;
    internal int CriticalMetadataModuleCount;
    internal int CriticalMetadataDamageCount;
    internal int PayloadDamageCount;
    internal int ErrorCorrectionDamageCount;
    internal double FixedPatternDamageFraction;
    internal double CriticalMetadataDamageFraction;
}
