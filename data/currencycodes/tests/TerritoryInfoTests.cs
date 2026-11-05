namespace currencycodes.Tests;

public class TerritoryInfoTests
{
    [Test]
    public void DefaultStateKeepsOptionalCodesAbsent()
    {
        TerritoryInfo territory = new TerritoryInfo();

        Assert.Multiple(() =>
        {
            Assert.That(territory.CanonicalName, Is.Empty);
            Assert.That(territory.HasAlpha2Code, Is.False);
            Assert.That(territory.Alpha2Code, Is.Empty);
            Assert.That(territory.HasAlpha3Code, Is.False);
            Assert.That(territory.Alpha3Code, Is.Empty);
            Assert.That(territory.HasNumericCode, Is.False);
            Assert.That(territory.NumericCode, Is.Empty);
            Assert.That(territory.NumericCodeValue, Is.Zero);
            Assert.That(territory.Kind, Is.EqualTo(TerritoryKind.Other));
        });
    }

    [Test]
    public void FieldsPreserveCompleteTerritoryIdentity()
    {
        TerritoryInfo territory = new TerritoryInfo
        {
            CanonicalName = "Bulgaria",
            HasAlpha2Code = true,
            Alpha2Code = "BG",
            HasAlpha3Code = true,
            Alpha3Code = "BGR",
            HasNumericCode = true,
            NumericCode = "100",
            NumericCodeValue = 100,
            Kind = TerritoryKind.Country
        };

        Assert.Multiple(() =>
        {
            Assert.That(territory.CanonicalName, Is.EqualTo("Bulgaria"));
            Assert.That(territory.HasAlpha2Code, Is.True);
            Assert.That(territory.Alpha2Code, Is.EqualTo("BG"));
            Assert.That(territory.HasAlpha3Code, Is.True);
            Assert.That(territory.Alpha3Code, Is.EqualTo("BGR"));
            Assert.That(territory.HasNumericCode, Is.True);
            Assert.That(territory.NumericCode, Is.EqualTo("100"));
            Assert.That(territory.NumericCodeValue, Is.EqualTo(100));
            Assert.That(territory.Kind, Is.EqualTo(TerritoryKind.Country));
        });
    }

    [Test]
    public void SpecialEntityCanHaveANameWithoutAssignedCodes()
    {
        TerritoryInfo territory = new TerritoryInfo
        {
            CanonicalName = "International Monetary Fund",
            Kind = TerritoryKind.Institution
        };

        Assert.Multiple(() =>
        {
            Assert.That(territory.CanonicalName, Is.EqualTo("International Monetary Fund"));
            Assert.That(territory.HasAlpha2Code, Is.False);
            Assert.That(territory.HasAlpha3Code, Is.False);
            Assert.That(territory.HasNumericCode, Is.False);
            Assert.That(territory.Kind, Is.EqualTo(TerritoryKind.Institution));
        });
    }
}
