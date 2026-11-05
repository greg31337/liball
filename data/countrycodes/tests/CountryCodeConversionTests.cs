using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class CountryCodeConversionTests
{
    [Test]
    public void EveryConversionUsesFieldsFromTheSameCountryRecord()
    {
        ICountryCatalog catalog = new CountryCodeService();
        Country[] countries = catalog.GetCountries();

        foreach (Country country in countries)
        {
            Assert.That(
                catalog.ConvertCountryAlpha2ToAlpha3(country.Alpha2),
                Is.EqualTo(country.Alpha3));
            Assert.That(
                catalog.ConvertCountryAlpha2ToNumericCode(country.Alpha2),
                Is.EqualTo(country.NumericText));
            Assert.That(
                catalog.ConvertCountryAlpha3ToAlpha2(country.Alpha3),
                Is.EqualTo(country.Alpha2));
            Assert.That(
                catalog.ConvertCountryAlpha3ToNumericCode(country.Alpha3),
                Is.EqualTo(country.NumericText));
            Assert.That(
                catalog.ConvertCountryNumericCodeToAlpha2(country.NumericText),
                Is.EqualTo(country.Alpha2));
            Assert.That(
                catalog.ConvertCountryNumericCodeToAlpha3(country.NumericText),
                Is.EqualTo(country.Alpha3));
        }
    }

    [Test]
    public void ConversionsNormalizeAlphaCaseAndPreserveNumericPadding()
    {
        ICountryCatalog catalog = new CountryCodeService();

        Assert.That(catalog.ConvertCountryAlpha2ToAlpha3("af"), Is.EqualTo("AFG"));
        Assert.That(catalog.ConvertCountryAlpha2ToNumericCode("af"), Is.EqualTo("004"));
        Assert.That(catalog.ConvertCountryAlpha3ToAlpha2("afg"), Is.EqualTo("AF"));
        Assert.That(catalog.ConvertCountryAlpha3ToNumericCode("afg"), Is.EqualTo("004"));
        Assert.That(catalog.ConvertCountryNumericCodeToAlpha2("004"), Is.EqualTo("AF"));
        Assert.That(catalog.ConvertCountryNumericCodeToAlpha3("004"), Is.EqualTo("AFG"));
    }

    [Test]
    public void ConversionsPreserveInvalidInputFailuresFromTheirSourceCodeKind()
    {
        ICountryCatalog catalog = new CountryCodeService();

        InvalidCountryCodeException alpha2 = Assert.Throws<InvalidCountryCodeException>(
            () => catalog.ConvertCountryAlpha2ToAlpha3("A1"))!;
        Assert.That(alpha2.Input, Is.EqualTo("A1"));
        Assert.That(alpha2.ExpectedKind, Is.EqualTo(CountryCodeKind.Alpha2));
        Assert.That(alpha2.FailureKind, Is.EqualTo(ValidationFailureKind.InvalidCharacter));

        InvalidCountryCodeException alpha3 = Assert.Throws<InvalidCountryCodeException>(
            () => catalog.ConvertCountryAlpha3ToNumericCode("GB"))!;
        Assert.That(alpha3.Input, Is.EqualTo("GB"));
        Assert.That(alpha3.ExpectedKind, Is.EqualTo(CountryCodeKind.Alpha3));
        Assert.That(alpha3.FailureKind, Is.EqualTo(ValidationFailureKind.InvalidLength));

        InvalidCountryCodeException numeric = Assert.Throws<InvalidCountryCodeException>(
            () => catalog.ConvertCountryNumericCodeToAlpha2("04"))!;
        Assert.That(numeric.Input, Is.EqualTo("04"));
        Assert.That(numeric.ExpectedKind, Is.EqualTo(CountryCodeKind.Numeric3));
        Assert.That(numeric.FailureKind, Is.EqualTo(ValidationFailureKind.InvalidLength));
    }

    [Test]
    public void ConversionsPreserveNotFoundFailuresFromTheirSourceCodeKind()
    {
        ICountryCatalog catalog = new CountryCodeService();

        CountryNotFoundException alpha2 = Assert.Throws<CountryNotFoundException>(
            () => catalog.ConvertCountryAlpha2ToNumericCode("ZZ"))!;
        Assert.That(alpha2.Input, Is.EqualTo("ZZ"));
        Assert.That(alpha2.Kind, Is.EqualTo(CountryCodeKind.Alpha2));

        CountryNotFoundException alpha3 = Assert.Throws<CountryNotFoundException>(
            () => catalog.ConvertCountryAlpha3ToAlpha2("ZZZ"))!;
        Assert.That(alpha3.Input, Is.EqualTo("ZZZ"));
        Assert.That(alpha3.Kind, Is.EqualTo(CountryCodeKind.Alpha3));

        CountryNotFoundException numeric = Assert.Throws<CountryNotFoundException>(
            () => catalog.ConvertCountryNumericCodeToAlpha3("000"))!;
        Assert.That(numeric.Input, Is.EqualTo("000"));
        Assert.That(numeric.Kind, Is.EqualTo(CountryCodeKind.Numeric3));
    }
}
