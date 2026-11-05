using fake;

namespace fake.Tests;

[TestFixture]
public class AddressRecordTests
{
    [Test]
    public void AddressRecordsAreDeterministicAndInternallyCorrelated()
    {
        Fake firstFake = Fake.Create(7601);
        Fake secondFake = Fake.Create(7601);
        AddressProvider first = firstFake.Address();
        AddressProvider second = secondFake.Address();
        AddressLocaleData data = AddressLocaleDataParser.Resolve(firstFake.Context);
        HashSet<string> observedCountries = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            AddressRecord record = first.Record();
            AddressRecord matching = second.Record();
            AddressCountryData country = Country(data, record.CountryCode);
            AddressCodedData subdivision = Subdivision(data, record.SubdivisionCode);

            Assert.Multiple(() =>
            {
                AssertAddressEqual(record, matching);
                Assert.That(record.SafetyMode, Is.EqualTo(SafetyMode.Reserved));
                Assert.That(record.NonDeliverabilityGuaranteed, Is.True);
                Assert.That(record.NonexistenceGuaranteed, Is.True);
                Assert.That(record.Country, Is.EqualTo(country.Name));
                Assert.That(record.CountryAbbreviation, Is.EqualTo(country.Abbreviation));
                Assert.That(record.TimeZoneId, Is.EqualTo(country.TimeZoneId));
                Assert.That(subdivision.CountryCode, Is.EqualTo(country.Alpha2Code));
                Assert.That(record.Subdivision, Is.EqualTo(subdivision.Name));
                Assert.That(record.SubdivisionAbbreviation, Is.EqualTo(subdivision.Abbreviation));
                Assert.That(MatchesAnyMask(record.PostalCode, country.PostalMasks), Is.True);
                Assert.That(record.Street, Is.EqualTo(record.StreetName + " " + record.StreetType));
                Assert.That(record.StreetAddress, Is.EqualTo(record.BuildingNumber + " " + record.Street));
                AssertGeography(record.Geography, country, subdivision);
                Assert.That(record.Geography.PostalCode, Is.EqualTo(record.PostalCode));
                Assert.That(record.Geography.TimeZoneId, Is.EqualTo(record.TimeZoneId));
            });
            observedCountries.Add(record.CountryCode);
            index++;
        }

        Assert.That(observedCountries, Is.EquivalentTo(new string[] { "XA", "XB", "XC" }));
    }

    [Test]
    public void GeoRecordsStayInsideTheirSelectedCountryBounds()
    {
        Fake firstFake = Fake.Create(7602);
        Fake secondFake = Fake.Create(7602);
        AddressProvider first = firstFake.Address();
        AddressProvider second = secondFake.Address();
        AddressLocaleData data = AddressLocaleDataParser.Resolve(firstFake.Context);

        int index = 0;
        while (index < 512)
        {
            GeoRecord record = first.GeoRecord();
            GeoRecord matching = second.GeoRecord();
            AddressCountryData country = Country(data, record.CountryCode);
            AddressCodedData subdivision = Subdivision(data, record.SubdivisionCode);

            Assert.Multiple(() =>
            {
                AssertGeoEqual(record, matching);
                AssertGeography(record, country, subdivision);
            });
            index++;
        }
    }

    [Test]
    public void PlausibleModeIsDeterministicAndCarriesNoSafetyGuarantees()
    {
        FakeOptions firstOptions = FakeOptions.Create(7603);
        firstOptions.SafetyMode = SafetyMode.SyntheticPlausible;
        FakeOptions secondOptions = FakeOptions.Create(firstOptions);
        AddressProvider first = Fake.Create(firstOptions).Address();
        AddressProvider second = Fake.Create(secondOptions).Address();
        string[] countries = ["Alderland", "Bellweather", "Calderia"];
        string[] streets = ["Willow", "Cedar", "Harbor", "Meadow", "Summit", "Orchard"];
        string[] cities = ["Alderbrook", "Bellhaven", "Calder Vale"];

        int index = 0;
        while (index < 256)
        {
            AddressRecord record = first.Record();
            AddressRecord matching = second.Record();
            Assert.Multiple(() =>
            {
                AssertAddressEqual(record, matching);
                Assert.That(record.Country, Is.AnyOf(countries));
                Assert.That(record.StreetName, Is.AnyOf(streets));
                Assert.That(record.City, Is.AnyOf(cities));
                Assert.That(record.SafetyMode, Is.EqualTo(SafetyMode.SyntheticPlausible));
                Assert.That(record.NonDeliverabilityGuaranteed, Is.False);
                Assert.That(record.NonexistenceGuaranteed, Is.False);
                Assert.That(record.Geography.SafetyMode, Is.EqualTo(SafetyMode.SyntheticPlausible));
            });
            index++;
        }
    }

    [Test]
    public void UnrestrictedModeUsesPlausibleFixturesWithoutGuarantees()
    {
        FakeOptions options = FakeOptions.Create(7604);
        options.SafetyMode = SafetyMode.UnrestrictedShape;
        AddressProvider address = Fake.Create(options).Address();
        string[] countries = ["Alderland", "Bellweather", "Calderia"];
        string[] streetNames = ["Willow", "Cedar", "Harbor", "Meadow", "Summit", "Orchard"];
        string[] cities = ["Alderbrook", "Bellhaven", "Calder Vale"];

        Assert.Multiple(() =>
        {
            Assert.That(address.StreetName(), Is.AnyOf(streetNames));
            Assert.That(address.City(), Is.AnyOf(cities));
            Assert.That(address.Country(), Is.AnyOf(countries));
            Assert.That(address.PostalCode(), Does.Match("^(AL|BE|CA)-"));
            Assert.That(address.FullAddressFromStyle("full"), Does.Match("(Alderland|Bellweather|Calderia)$"));
        });

        AddressRecord record = address.Record();
        Assert.Multiple(() =>
        {
            Assert.That(record.SafetyMode, Is.EqualTo(SafetyMode.UnrestrictedShape));
            Assert.That(record.NonDeliverabilityGuaranteed, Is.False);
            Assert.That(record.NonexistenceGuaranteed, Is.False);
        });
    }

    static void AssertGeography(
        GeoRecord record,
        AddressCountryData country,
        AddressCodedData subdivision)
    {
        Assert.That(record.Country, Is.EqualTo(country.Name));
        Assert.That(record.CountryCode, Is.EqualTo(country.Alpha2Code));
        Assert.That(record.Subdivision, Is.EqualTo(subdivision.Name));
        Assert.That(subdivision.CountryCode, Is.EqualTo(country.Alpha2Code));
        Assert.That(record.TimeZoneId, Is.EqualTo(country.TimeZoneId));
        Assert.That(MatchesAnyMask(record.PostalCode, country.PostalMasks), Is.True);
        Assert.That(record.Coordinate.Latitude, Is.InRange(country.SouthLatitude, country.NorthLatitude));
        if (country.WestLongitude <= country.EastLongitude)
        {
            Assert.That(record.Coordinate.Longitude, Is.InRange(country.WestLongitude, country.EastLongitude));
        }
        else
        {
            Assert.That(
                record.Coordinate.Longitude >= country.WestLongitude ||
                record.Coordinate.Longitude <= country.EastLongitude,
                Is.True);
        }
    }

    static void AssertAddressEqual(AddressRecord first, AddressRecord second)
    {
        Assert.That(first.SafetyMode, Is.EqualTo(second.SafetyMode));
        Assert.That(first.NonDeliverabilityGuaranteed, Is.EqualTo(second.NonDeliverabilityGuaranteed));
        Assert.That(first.NonexistenceGuaranteed, Is.EqualTo(second.NonexistenceGuaranteed));
        Assert.That(first.BuildingNumber, Is.EqualTo(second.BuildingNumber));
        Assert.That(first.StreetName, Is.EqualTo(second.StreetName));
        Assert.That(first.StreetType, Is.EqualTo(second.StreetType));
        Assert.That(first.Street, Is.EqualTo(second.Street));
        Assert.That(first.StreetAddress, Is.EqualTo(second.StreetAddress));
        Assert.That(first.Unit, Is.EqualTo(second.Unit));
        Assert.That(first.District, Is.EqualTo(second.District));
        Assert.That(first.City, Is.EqualTo(second.City));
        Assert.That(first.County, Is.EqualTo(second.County));
        Assert.That(first.Subdivision, Is.EqualTo(second.Subdivision));
        Assert.That(first.SubdivisionAbbreviation, Is.EqualTo(second.SubdivisionAbbreviation));
        Assert.That(first.SubdivisionCode, Is.EqualTo(second.SubdivisionCode));
        Assert.That(first.PostalCode, Is.EqualTo(second.PostalCode));
        Assert.That(first.Country, Is.EqualTo(second.Country));
        Assert.That(first.CountryAbbreviation, Is.EqualTo(second.CountryAbbreviation));
        Assert.That(first.CountryCode, Is.EqualTo(second.CountryCode));
        Assert.That(first.TimeZoneId, Is.EqualTo(second.TimeZoneId));
        AssertGeoEqual(first.Geography, second.Geography);
    }

    static void AssertGeoEqual(GeoRecord first, GeoRecord second)
    {
        Assert.That(first.SafetyMode, Is.EqualTo(second.SafetyMode));
        Assert.That(first.NonDeliverabilityGuaranteed, Is.EqualTo(second.NonDeliverabilityGuaranteed));
        Assert.That(first.NonexistenceGuaranteed, Is.EqualTo(second.NonexistenceGuaranteed));
        Assert.That(first.Country, Is.EqualTo(second.Country));
        Assert.That(first.CountryCode, Is.EqualTo(second.CountryCode));
        Assert.That(first.Subdivision, Is.EqualTo(second.Subdivision));
        Assert.That(first.SubdivisionCode, Is.EqualTo(second.SubdivisionCode));
        Assert.That(first.PostalCode, Is.EqualTo(second.PostalCode));
        Assert.That(first.TimeZoneId, Is.EqualTo(second.TimeZoneId));
        Assert.That(first.Coordinate.Latitude, Is.EqualTo(second.Coordinate.Latitude));
        Assert.That(first.Coordinate.Longitude, Is.EqualTo(second.Coordinate.Longitude));
    }

    static AddressCountryData Country(AddressLocaleData data, string code)
    {
        return data.Countries.Single(country => country.Alpha2Code == code);
    }

    static AddressCodedData Subdivision(AddressLocaleData data, string code)
    {
        return data.Subdivisions.Single(subdivision => subdivision.Code == code);
    }

    static bool MatchesAnyMask(string value, AddressValueData[] masks)
    {
        int index = 0;
        while (index < masks.Length)
        {
            if (MatchesMask(value, masks[index].Value))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static bool MatchesMask(string value, string mask)
    {
        if (value.Length != mask.Length)
        {
            return false;
        }
        int index = 0;
        while (index < mask.Length)
        {
            char expected = mask[index];
            char actual = value[index];
            if (expected == '#' && (actual < '0' || actual > '9'))
            {
                return false;
            }
            if (expected == '?' &&
                (actual < 'A' || actual > 'Z') &&
                (actual < 'a' || actual > 'z'))
            {
                return false;
            }
            if (expected != '#' && expected != '?' && expected != actual)
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
