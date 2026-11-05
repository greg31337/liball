using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitErrorCodeTests
{
    [Test]
    public void UnitErrorCode_DefinesSuccessAndFailureCategories()
    {
        Assert.That(Enum.IsDefined(UnitErrorCode.None), Is.True);
        Assert.That(Enum.IsDefined(UnitErrorCode.ParseInvalidInput), Is.True);
        Assert.That(Enum.IsDefined(UnitErrorCode.LookupNotFound), Is.True);
        Assert.That(Enum.IsDefined(UnitErrorCode.ArithmeticIncompatibleDimensions), Is.True);
        Assert.That(Enum.IsDefined(UnitErrorCode.RegistryDuplicateId), Is.True);
        Assert.That(Enum.IsDefined(UnitErrorCode.RegistryInvalidMetadata), Is.True);
        Assert.That(Enum.IsDefined(UnitErrorCode.ConversionIncompatibleDimensions), Is.True);
    }

    [Test]
    public void UnitErrorCode_UsesStableCategoryRanges()
    {
        Assert.That((int)UnitErrorCode.None, Is.EqualTo(0));
        Assert.That((int)UnitErrorCode.ParseInvalidInput, Is.EqualTo(100));
        Assert.That((int)UnitErrorCode.LookupNotFound, Is.EqualTo(200));
        Assert.That((int)UnitErrorCode.ArithmeticIncompatibleDimensions, Is.EqualTo(300));
        Assert.That((int)UnitErrorCode.RegistryDuplicateId, Is.EqualTo(400));
        Assert.That((int)UnitErrorCode.RegistryInvalidMetadata, Is.EqualTo(403));
        Assert.That((int)UnitErrorCode.ConversionIncompatibleDimensions, Is.EqualTo(500));
    }

    [Test]
    public void UnitErrorCode_DefinesSpecificFailureReasons()
    {
        UnitErrorCode[] values = Enum.GetValues<UnitErrorCode>();

        Assert.That(values, Does.Contain(UnitErrorCode.ParseUnknownUnit));
        Assert.That(values, Does.Contain(UnitErrorCode.ParseAmbiguousUnit));
        Assert.That(values, Does.Contain(UnitErrorCode.LookupAmbiguous));
        Assert.That(values, Does.Contain(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.That(values, Does.Contain(UnitErrorCode.RegistryAmbiguousSymbol));
        Assert.That(values, Does.Contain(UnitErrorCode.RegistryFrozen));
        Assert.That(values, Does.Contain(UnitErrorCode.RegistryInvalidMetadata));
        Assert.That(values, Does.Contain(UnitErrorCode.ConversionUnsupported));
        Assert.That(values, Does.Contain(UnitErrorCode.ConversionInvalidAffineOperation));
    }
}
