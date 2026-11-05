using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrEnumValueValidatorTests
{
    [Test]
    public void RecognizesEveryDefinedClassificationValue()
    {
        IQrEnumValueValidator validator = new QrEnumValueValidator();

        foreach (QrSymbolKind value in Enum.GetValues<QrSymbolKind>())
        {
            Assert.That(validator.IsSymbolKindDefined(value), Is.True, value.ToString());
        }

        foreach (QrEncodingMode value in Enum.GetValues<QrEncodingMode>())
        {
            Assert.That(validator.IsEncodingModeDefined(value), Is.True, value.ToString());
        }

        foreach (QrErrorCorrectionLevel value in Enum.GetValues<QrErrorCorrectionLevel>())
        {
            Assert.That(validator.IsErrorCorrectionLevelDefined(value), Is.True, value.ToString());
        }

        foreach (QrMaskPattern value in Enum.GetValues<QrMaskPattern>())
        {
            Assert.That(validator.IsMaskPatternDefined(value), Is.True, value.ToString());
        }

        foreach (QrPixelFormat value in Enum.GetValues<QrPixelFormat>())
        {
            Assert.That(validator.IsPixelFormatDefined(value), Is.True, value.ToString());
        }

        foreach (QrBinarizationMode value in Enum.GetValues<QrBinarizationMode>())
        {
            Assert.That(validator.IsBinarizationModeDefined(value), Is.True, value.ToString());
        }

        foreach (QrPolarity value in Enum.GetValues<QrPolarity>())
        {
            Assert.That(validator.IsPolarityDefined(value), Is.True, value.ToString());
        }

        foreach (QrModuleValue value in Enum.GetValues<QrModuleValue>())
        {
            Assert.That(validator.IsModuleValueDefined(value), Is.True, value.ToString());
        }

        foreach (QrModuleRole value in Enum.GetValues<QrModuleRole>())
        {
            Assert.That(validator.IsModuleRoleDefined(value), Is.True, value.ToString());
        }

        foreach (QrFailureReason value in Enum.GetValues<QrFailureReason>())
        {
            Assert.That(validator.IsFailureReasonDefined(value), Is.True, value.ToString());
        }

        foreach (QrQualityGrade value in Enum.GetValues<QrQualityGrade>())
        {
            Assert.That(validator.IsQualityGradeDefined(value), Is.True, value.ToString());
        }
    }

    [Test]
    public void RejectsOutOfRangeAndReservedClassificationValues()
    {
        IQrEnumValueValidator validator = new QrEnumValueValidator();

        Assert.Multiple(() =>
        {
            Assert.That(validator.IsSymbolKindDefined((QrSymbolKind)(-1)), Is.False);
            Assert.That(validator.IsSymbolKindDefined((QrSymbolKind)4), Is.False);
            Assert.That(validator.IsEncodingModeDefined((QrEncodingMode)6), Is.False);
            Assert.That(validator.IsEncodingModeDefined((QrEncodingMode)12), Is.False);
            Assert.That(validator.IsErrorCorrectionLevelDefined((QrErrorCorrectionLevel)(-2)), Is.False);
            Assert.That(validator.IsErrorCorrectionLevelDefined((QrErrorCorrectionLevel)4), Is.False);
            Assert.That(validator.IsMaskPatternDefined((QrMaskPattern)(-2)), Is.False);
            Assert.That(validator.IsMaskPatternDefined((QrMaskPattern)8), Is.False);
            Assert.That(validator.IsPixelFormatDefined((QrPixelFormat)4), Is.False);
            Assert.That(validator.IsBinarizationModeDefined((QrBinarizationMode)4), Is.False);
            Assert.That(validator.IsPolarityDefined((QrPolarity)3), Is.False);
            Assert.That(validator.IsModuleValueDefined((QrModuleValue)3), Is.False);
            Assert.That(validator.IsModuleRoleDefined((QrModuleRole)12), Is.False);
            Assert.That(validator.IsFailureReasonDefined((QrFailureReason)14), Is.False);
            Assert.That(validator.IsQualityGradeDefined((QrQualityGrade)(-2)), Is.False);
            Assert.That(validator.IsQualityGradeDefined((QrQualityGrade)5), Is.False);
        });
    }

    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void RejectsNegativeValuesForClassificationsWithoutNegativeSentinels(int value)
    {
        IQrEnumValueValidator validator = new QrEnumValueValidator();
        Assert.Multiple(() =>
        {
            Assert.That(validator.IsPixelFormatDefined((QrPixelFormat)value), Is.False);
            Assert.That(validator.IsBinarizationModeDefined((QrBinarizationMode)value), Is.False);
            Assert.That(validator.IsPolarityDefined((QrPolarity)value), Is.False);
            Assert.That(validator.IsModuleValueDefined((QrModuleValue)value), Is.False);
            Assert.That(validator.IsModuleRoleDefined((QrModuleRole)value), Is.False);
        });
    }

    [Test]
    public void SeparateInstancesRemainDeterministicUnderConcurrentUse()
    {
        IQrEnumValueValidator first = new QrEnumValueValidator();
        IQrEnumValueValidator second = new QrEnumValueValidator();
        int failureCount = 0;

        Parallel.For(0, 2048, iteration =>
        {
            IQrEnumValueValidator validator = iteration % 2 == 0 ? first : second;
            bool valid = validator.IsEncodingModeDefined(QrEncodingMode.Byte)
                && validator.IsMaskPatternDefined(QrMaskPattern.Five)
                && validator.IsFailureReasonDefined(QrFailureReason.InvalidData);
            bool invalid = validator.IsEncodingModeDefined((QrEncodingMode)6)
                || validator.IsMaskPatternDefined((QrMaskPattern)9)
                || validator.IsFailureReasonDefined((QrFailureReason)(-1));
            if (!valid || invalid)
            {
                Interlocked.Increment(ref failureCount);
            }
        });

        Assert.That(failureCount, Is.EqualTo(0));
    }
}
