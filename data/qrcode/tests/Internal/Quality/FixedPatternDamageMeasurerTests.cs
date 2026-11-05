using qrcode;
using static qrcode.Tests.MatrixValidatorTests;

namespace qrcode.Tests;

[TestFixture]
public class FixedPatternDamageMeasurerTests
{
    [TestCase(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, QrMaskPattern.Zero)]
    [TestCase(QrSymbolKind.Model2, 7, QrErrorCorrectionLevel.Quartile, QrMaskPattern.Five)]
    [TestCase(QrSymbolKind.Micro, 1, QrErrorCorrectionLevel.None, QrMaskPattern.Zero)]
    [TestCase(QrSymbolKind.Micro, 4, QrErrorCorrectionLevel.Quartile, QrMaskPattern.Three)]
    [TestCase(QrSymbolKind.rMQR, 0, QrErrorCorrectionLevel.Medium, QrMaskPattern.Four)]
    [TestCase(QrSymbolKind.rMQR, 31, QrErrorCorrectionLevel.High, QrMaskPattern.Four)]
    public void UndamagedProfilesHaveZeroDamageAndAccurateRoleDenominators(QrSymbolKind kind, int version,
        QrErrorCorrectionLevel level, QrMaskPattern mask)
    {
        QrMatrix matrix = CreateValidSymbol(kind, version, level, mask).Matrix!;
        ModuleDamageMeasurement result = Measure(matrix, Reference(matrix), matrix.Roles!);
        Assert.That(result.ModuleCounts.Sum(), Is.EqualTo(matrix.Values.Length));
        for (int role = 1; role <= 11; role++)
            Assert.That(result.ModuleCounts[role], Is.EqualTo(matrix.Roles!.Count(r => (int)r == role)));
        Assert.That(result.DamageCounts.Sum(), Is.Zero);
        Assert.That(result.DamagedModules, Is.All.False);
        Assert.That(result.UnknownCounts.Sum(), Is.Zero);
        Assert.That(result.MismatchCounts.Sum(), Is.Zero);
        Assert.That(result.InvalidCounts.Sum(), Is.Zero);
        Assert.That(result.FixedPatternModuleCount, Is.EqualTo(matrix.Roles!.Count(IsFixed)));
        Assert.That(result.CriticalMetadataModuleCount, Is.EqualTo(matrix.Roles!.Count(IsMetadata)));
        Assert.That(result.FixedPatternDamageFraction, Is.Zero);
        Assert.That(result.CriticalMetadataDamageFraction, Is.Zero);
    }

    [Test, Combinatorial]
    public void MeasuresEveryRoleWithoutMixingDamageCategories(
        [Values(QrModuleRole.QuietZone, QrModuleRole.Finder, QrModuleRole.Separator, QrModuleRole.Timing,
            QrModuleRole.Alignment, QrModuleRole.Format, QrModuleRole.Version, QrModuleRole.DarkModule,
            QrModuleRole.Data, QrModuleRole.ErrorCorrection, QrModuleRole.Remainder)] QrModuleRole role,
        [Values(false, true)] bool unknown)
    {
        QrMatrix matrix = CreateValidSymbol(QrSymbolKind.Model2, role == QrModuleRole.Version ? 7 : 2,
            QrErrorCorrectionLevel.Low, QrMaskPattern.Zero).Matrix!;
        BitMatrixBuffer reference = Reference(matrix);
        int index = Array.IndexOf(matrix.Roles!, role);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        if (unknown) matrix.Values[index] = QrModuleValue.Unknown;
        else Flip(matrix, index);
        ModuleDamageMeasurement result = Measure(matrix, reference, matrix.Roles!);
        Assert.That(result.DamageCounts[(int)role], Is.EqualTo(1));
        Assert.That(result.DamageCounts.Sum(), Is.EqualTo(1));
        Assert.That(result.UnknownCounts[(int)role], Is.EqualTo(unknown ? 1 : 0));
        Assert.That(result.MismatchCounts[(int)role], Is.EqualTo(unknown ? 0 : 1));
        Assert.That(result.InvalidCounts.Sum(), Is.Zero);
        Assert.That(result.DamagedModules[index], Is.True);
        Assert.That(result.DamagedModules.Count(d => d), Is.EqualTo(1));
        Assert.That(result.FixedPatternDamageCount, Is.EqualTo(IsFixed(role) ? 1 : 0));
        Assert.That(result.CriticalMetadataDamageCount, Is.EqualTo(IsMetadata(role) ? 1 : 0));
        Assert.That(result.PayloadDamageCount, Is.EqualTo(role == QrModuleRole.Data ? 1 : 0));
        Assert.That(result.ErrorCorrectionDamageCount, Is.EqualTo(role == QrModuleRole.ErrorCorrection ? 1 : 0));
        Assert.That(result.FixedPatternDamageFraction, Is.EqualTo(IsFixed(role) ? 1.0 / result.FixedPatternModuleCount : 0));
        Assert.That(result.CriticalMetadataDamageFraction, Is.EqualTo(IsMetadata(role) ? 1.0 / result.CriticalMetadataModuleCount : 0));
    }

    [Test]
    public void PublicValidationDistinguishesCorrectableMetadataPayloadAndEccDamage()
    {
        QrSymbol symbol = CreateValidSymbol(QrSymbolKind.Model2, 7, QrErrorCorrectionLevel.Low, QrMaskPattern.Zero);
        QrMatrix matrix = symbol.Matrix!;
        foreach (QrModuleRole role in new[] { QrModuleRole.Finder, QrModuleRole.Format, QrModuleRole.Version, QrModuleRole.Data, QrModuleRole.ErrorCorrection })
            Flip(matrix, Array.IndexOf(matrix.Roles!, role));
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        QrValidationResult result = new QrMatrixProcessor().ValidateMatrix(symbol);
        Assert.That(result.IsConformant, Is.False);
        Assert.That(result.IsRecoverable, Is.True);
        Assert.That(result.FormatInformationConsistent, Is.True);
        Assert.That(result.VersionInformationConsistent, Is.True);
        Assert.That(result.FixedPatternDamageCount, Is.EqualTo(1));
        Assert.That(result.CriticalMetadataDamageCount, Is.EqualTo(2));
        Assert.That(result.PayloadDamageCount, Is.EqualTo(1));
        Assert.That(result.ErrorCorrectionDamageCount, Is.EqualTo(1));
        Assert.That(result.DamagedModuleIndices, Has.Length.EqualTo(5));
        Assert.That(matrix.Values, Is.EqualTo(before));
    }

    [Test]
    public void IncorrectRoleLabelsAreNotCountedAsPhysicalDamage()
    {
        QrSymbol symbol = CreateValidSymbol(QrSymbolKind.Model2, 7, QrErrorCorrectionLevel.Low, QrMaskPattern.Zero);
        QrMatrix matrix = symbol.Matrix!;
        int index = Array.IndexOf(matrix.Roles!, QrModuleRole.Format);
        matrix.Roles![index] = QrModuleRole.Data;
        QrValidationResult result = new QrMatrixProcessor().ValidateMatrix(symbol);
        Assert.That(result.IsConformant, Is.False);
        Assert.That(result.DamagedRoles, Does.Contain(QrModuleRole.Format));
        Assert.That(result.FixedPatternDamageCount, Is.Zero);
        Assert.That(result.CriticalMetadataDamageCount, Is.Zero);
        Assert.That(result.PayloadDamageCount, Is.Zero);
        Assert.That(result.ErrorCorrectionDamageCount, Is.Zero);
    }

    [Test]
    public void PreservesEvidenceAndReportsInvalidObservationsSeparatelyFromUnknowns()
    {
        QrMatrix matrix = CreateValidSymbol(QrSymbolKind.Model2, 7, QrErrorCorrectionLevel.Low, QrMaskPattern.Zero).Matrix!;
        BitMatrixBuffer reference = Reference(matrix);
        QrModuleRole[] roles = (QrModuleRole[])matrix.Roles!.Clone();
        int[] finders = Enumerable.Range(0, roles.Length).Where(i => roles[i] == QrModuleRole.Finder).Take(3).ToArray();
        Flip(matrix, finders[0]);
        matrix.Values[finders[1]] = QrModuleValue.Unknown;
        matrix.Values[finders[2]] = (QrModuleValue)99;
        matrix.Roles = Enumerable.Repeat(QrModuleRole.Data, roles.Length).ToArray();
        matrix.Confidence = new byte[roles.Length];
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        ModuleDamageMeasurement result = Measure(matrix, reference, roles);
        Assert.That(result.FixedPatternDamageCount, Is.EqualTo(3));
        Assert.That(result.UnknownCounts[(int)QrModuleRole.Finder], Is.EqualTo(1));
        Assert.That(result.InvalidCounts[(int)QrModuleRole.Finder], Is.EqualTo(1));
        Assert.That(result.MismatchCounts[(int)QrModuleRole.Finder], Is.EqualTo(1));
        Assert.That(result.PayloadDamageCount, Is.Zero);
        Assert.That(result.DamagedModules.Count(d => d), Is.EqualTo(3));
        Assert.That(matrix.Values, Is.EqualTo(before));
        Assert.That(matrix.Roles, Is.All.EqualTo(QrModuleRole.Data));
        Assert.That(matrix.Confidence, Is.All.Zero);
        result.DamageCounts[2] = 999;
        Assert.That(Measure(matrix, reference, roles).FixedPatternDamageCount, Is.EqualTo(3));
    }

    [Test]
    public void AccountsForAsymmetricQuietZonesWithoutChangingEncodingDamageFractions()
    {
        QrMatrix matrix = CreateValidSymbol(QrSymbolKind.Model2, 2, QrErrorCorrectionLevel.Low, QrMaskPattern.Zero).Matrix!;
        matrix = MatrixTransformer.Crop(matrix, 1, 2, matrix.Width - 1, matrix.Height - 2);
        BitMatrixBuffer reference = Reference(matrix);
        Flip(matrix, Array.IndexOf(matrix.Roles!, QrModuleRole.Timing));
        Flip(matrix, 0);
        ModuleDamageMeasurement result = Measure(matrix, reference, matrix.Roles!);
        Assert.That(result.FixedPatternDamageCount, Is.EqualTo(1));
        Assert.That(result.DamageCounts[(int)QrModuleRole.QuietZone], Is.EqualTo(1));
        Assert.That(result.FixedPatternDamageFraction, Is.EqualTo(1.0 / result.FixedPatternModuleCount));
    }

    [Test]
    public void RejectsMismatchedReferencesAndBoundsMeasurementStorage()
    {
        QrMatrix matrix = CreateValidSymbol(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, QrMaskPattern.Zero).Matrix!;
        BitMatrixBuffer reference = Reference(matrix);
        QrModuleRole[] roles = matrix.Roles!;
        Assert.Throws<QrInputException>(() => Measure(null!, reference, roles));
        Assert.Throws<QrInputException>(() => Measure(matrix, null!, roles));
        Assert.Throws<QrInputException>(() => Measure(matrix, reference, null!));
        Assert.Throws<QrInputException>(() => Measure(matrix, new BitMatrixBuffer(1, 1), roles));
        Assert.Throws<QrInputException>(() => Measure(matrix, reference, new QrModuleRole[1]));
        Assert.Throws<QrResourceLimitException>(() => FixedPatternDamageMeasurer.Measure(matrix, reference, roles, matrix.Values.Length - 1));
        Assert.Throws<QrInputException>(() => FixedPatternDamageMeasurer.Measure(matrix, reference, roles, 0));
        QrModuleRole[] invalid = (QrModuleRole[])roles.Clone();
        invalid[0] = QrModuleRole.Data;
        Assert.Throws<QrInputException>(() => Measure(matrix, reference, invalid));
        invalid[0] = (QrModuleRole)99;
        Assert.Throws<QrInputException>(() => Measure(matrix, reference, invalid));
        matrix.Values = new QrModuleValue[1];
        Assert.Throws<QrInputException>(() => Measure(matrix, reference, roles));
    }

    static ModuleDamageMeasurement Measure(QrMatrix matrix, BitMatrixBuffer reference, QrModuleRole[] roles) =>
        FixedPatternDamageMeasurer.Measure(matrix, reference, roles, 1048576);

    static BitMatrixBuffer Reference(QrMatrix matrix) => QrMatrixProcessor.ConvertQrMatrix(MatrixTransformer.RemoveQuietZone(matrix));

    static bool IsFixed(QrModuleRole role) => role is QrModuleRole.Finder or QrModuleRole.Separator or QrModuleRole.Timing or QrModuleRole.Alignment or QrModuleRole.DarkModule;

    static bool IsMetadata(QrModuleRole role) => role is QrModuleRole.Format or QrModuleRole.Version;

    static void Flip(QrMatrix matrix, int index) => matrix.Values[index] = matrix.Values[index] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
}
