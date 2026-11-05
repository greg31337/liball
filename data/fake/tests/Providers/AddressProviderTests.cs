using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class AddressProviderTests
{
    [Test]
    public void ComponentApisAreDeterministicDeclaredSelections()
    {
        Fake firstFake = Fake.Create(7201);
        Fake secondFake = Fake.Create(7201);
        AddressLocaleData data = AddressLocaleDataParser.Resolve(firstFake.Context);
        AddressProvider first = firstFake.Address();
        AddressProvider second = secondFake.Address();

        int index = 0;
        while (index < 256)
        {
            string building = first.BuildingNumber();
            string streetName = first.StreetName();
            string street = first.Street();
            string streetType = first.StreetType();
            string streetTypeAbbreviation = first.StreetTypeAbbreviation();
            string unit = first.Unit();
            string poBox = first.PoBox();
            string district = first.District();
            string city = first.City();
            string county = first.County();
            string subdivision = first.Subdivision();
            string subdivisionAbbreviation = first.SubdivisionAbbreviation();
            string subdivisionCode = first.SubdivisionCode();
            string postalCode = first.PostalCode();
            string country = first.Country();
            string countryAbbreviation = first.CountryAbbreviation();
            string countryCode = first.CountryCode();
            string countryAlpha3Code = first.CountryAlpha3Code();
            string countryNumericCode = first.CountryNumericCode();
            string language = first.Language();
            string languageCode = first.LanguageCode();
            string currency = first.Currency();
            string currencyCode = first.CurrencyCode();
            string callingCode = first.CallingCode();
            string timeZoneId = first.TimeZoneId();

            Assert.Multiple(() =>
            {
                Assert.That(building, Is.EqualTo(second.BuildingNumber()));
                Assert.That(streetName, Is.EqualTo(second.StreetName()));
                Assert.That(street, Is.EqualTo(second.Street()));
                Assert.That(streetType, Is.EqualTo(second.StreetType()));
                Assert.That(streetTypeAbbreviation, Is.EqualTo(second.StreetTypeAbbreviation()));
                Assert.That(unit, Is.EqualTo(second.Unit()));
                Assert.That(poBox, Is.EqualTo(second.PoBox()));
                Assert.That(district, Is.EqualTo(second.District()));
                Assert.That(city, Is.EqualTo(second.City()));
                Assert.That(county, Is.EqualTo(second.County()));
                Assert.That(subdivision, Is.EqualTo(second.Subdivision()));
                Assert.That(subdivisionAbbreviation, Is.EqualTo(second.SubdivisionAbbreviation()));
                Assert.That(subdivisionCode, Is.EqualTo(second.SubdivisionCode()));
                Assert.That(postalCode, Is.EqualTo(second.PostalCode()));
                Assert.That(country, Is.EqualTo(second.Country()));
                Assert.That(countryAbbreviation, Is.EqualTo(second.CountryAbbreviation()));
                Assert.That(countryCode, Is.EqualTo(second.CountryCode()));
                Assert.That(countryAlpha3Code, Is.EqualTo(second.CountryAlpha3Code()));
                Assert.That(countryNumericCode, Is.EqualTo(second.CountryNumericCode()));
                Assert.That(language, Is.EqualTo(second.Language()));
                Assert.That(languageCode, Is.EqualTo(second.LanguageCode()));
                Assert.That(currency, Is.EqualTo(second.Currency()));
                Assert.That(currencyCode, Is.EqualTo(second.CurrencyCode()));
                Assert.That(callingCode, Is.EqualTo(second.CallingCode()));
                Assert.That(timeZoneId, Is.EqualTo(second.TimeZoneId()));
                Assert.That(MatchesAnyMask(building, data.BuildingNumberMasks), Is.True);
                Assert.That(data.StreetNames.Select(value => value.Value), Does.Contain(streetName));
                Assert.That(DeclaredStreet(street, data), Is.True);
                Assert.That(data.StreetTypes.Select(value => value.Name), Does.Contain(streetType));
                Assert.That(data.StreetTypes.Select(value => value.Abbreviation), Does.Contain(streetTypeAbbreviation));
                Assert.That(DeclaredUnit(unit, data), Is.True);
                Assert.That(poBox, Does.StartWith(data.PoBoxLabel + " "));
                Assert.That(MatchesAnyMask(poBox.Substring(data.PoBoxLabel.Length + 1), data.PoBoxMasks), Is.True);
                Assert.That(data.Districts.Select(value => value.Value), Does.Contain(district));
                Assert.That(data.Cities.Select(value => value.Value), Does.Contain(city));
                Assert.That(data.Counties.Select(value => value.Value), Does.Contain(county));
                Assert.That(data.Subdivisions.Select(value => value.Name), Does.Contain(subdivision));
                Assert.That(data.Subdivisions.Select(value => value.Abbreviation), Does.Contain(subdivisionAbbreviation));
                Assert.That(data.Subdivisions.Select(value => value.Code), Does.Contain(subdivisionCode));
                Assert.That(MatchesAnyMask(postalCode, data.PostalMasks), Is.True);
                Assert.That(data.Countries.Select(value => value.Name), Does.Contain(country));
                Assert.That(data.Countries.Select(value => value.Abbreviation), Does.Contain(countryAbbreviation));
                Assert.That(data.Countries.Select(value => value.Alpha2Code), Does.Contain(countryCode));
                Assert.That(data.Countries.Select(value => value.Alpha3Code), Does.Contain(countryAlpha3Code));
                Assert.That(data.Countries.Select(value => value.NumericCode), Does.Contain(countryNumericCode));
                Assert.That(data.Countries.Select(value => value.Language), Does.Contain(language));
                Assert.That(data.Countries.Select(value => value.LanguageCode), Does.Contain(languageCode));
                Assert.That(data.Countries.Select(value => value.Currency), Does.Contain(currency));
                Assert.That(data.Countries.Select(value => value.CurrencyCode), Does.Contain(currencyCode));
                Assert.That(data.Countries.Select(value => value.CallingCode), Does.Contain(callingCode));
                Assert.That(data.Countries.Select(value => value.TimeZoneId), Does.Contain(timeZoneId));
            });
            index++;
        }
    }

    [Test]
    public void CallerComponentsUseParentFallbackAndNormalization()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 7202);
        options.NormalizationMode = NormalizationMode.Nfc;
        Fake value = Fake.Create(options);
        Register(value, "fr", ValidJson("Cite\u0301 Exemple", false));
        AddressProvider address = value.Address();

        Assert.Multiple(() =>
        {
            Assert.That(address.BuildingNumber(), Does.Match("^[0-9]{3}$"));
            Assert.That(address.StreetName(), Is.EqualTo("Exemple"));
            Assert.That(address.Street(), Is.EqualTo("Exemple Rue"));
            Assert.That(address.StreetType(), Is.EqualTo("Rue"));
            Assert.That(address.StreetTypeAbbreviation(), Is.EqualTo("r."));
            Assert.That(address.Unit(), Does.Match("^Appartement [0-9]{2}$"));
            Assert.That(address.PoBox(), Does.Match("^Boîte postale [0-9]{4}$"));
            Assert.That(address.District(), Is.EqualTo("Quartier Exemple"));
            Assert.That(address.City(), Is.EqualTo("Cité Exemple"));
            Assert.That(address.County(), Is.EqualTo("Comté Exemple"));
            Assert.That(address.Subdivision(), Is.EqualTo("Région Exemple"));
            Assert.That(address.SubdivisionAbbreviation(), Is.EqualTo("ER"));
            Assert.That(address.SubdivisionCode(), Is.EqualTo("XZ-ER"));
            Assert.That(address.PostalCode(), Does.Match("^EX-[0-9]{4}$"));
            Assert.That(address.Country(), Is.EqualTo("Pays Exemple"));
            Assert.That(address.CountryAbbreviation(), Is.EqualTo("P. Ex."));
            Assert.That(address.CountryCode(), Is.EqualTo("XZ"));
            Assert.That(address.CountryAlpha3Code(), Is.EqualTo("XZZ"));
            Assert.That(address.CountryNumericCode(), Is.EqualTo("999"));
            Assert.That(address.Language(), Is.EqualTo("Langue Exemple"));
            Assert.That(address.LanguageCode(), Is.EqualTo("qaa"));
            Assert.That(address.Currency(), Is.EqualTo("Monnaie Exemple"));
            Assert.That(address.CurrencyCode(), Is.EqualTo("XCR"));
            Assert.That(address.CallingCode(), Is.EqualTo("+999"));
            Assert.That(address.TimeZoneId(), Is.EqualTo("Europe/Paris"));
            Assert.That(value.Locale().FallbackTrace("address.catalog").SourceLocale, Is.EqualTo("fr"));
        });
    }

    [Test]
    public void CountryDetailsKeepsEveryMetadataFieldCorrelated()
    {
        Fake value = Fake.Create(7205);
        AddressLocaleData data = AddressLocaleDataParser.Resolve(value.Context);

        int index = 0;
        while (index < 128)
        {
            CountryDetails details = value.Address().CountryDetails();
            AddressCountryData declared = data.Countries.Single(
                country => country.Alpha2Code == details.Alpha2Code);

            Assert.Multiple(() =>
            {
                Assert.That(details.Country, Is.EqualTo(declared.Name));
                Assert.That(details.Abbreviation, Is.EqualTo(declared.Abbreviation));
                Assert.That(details.Alpha3Code, Is.EqualTo(declared.Alpha3Code));
                Assert.That(details.NumericCode, Is.EqualTo(declared.NumericCode));
                Assert.That(details.Language, Is.EqualTo(declared.Language));
                Assert.That(details.LanguageCode, Is.EqualTo(declared.LanguageCode));
                Assert.That(details.Currency, Is.EqualTo(declared.Currency));
                Assert.That(details.CurrencyCode, Is.EqualTo(declared.CurrencyCode));
                Assert.That(details.CallingCode, Is.EqualTo(declared.CallingCode));
                Assert.That(details.TimeZoneId, Is.EqualTo(declared.TimeZoneId));
                Assert.That(details.Alpha2Code, Does.Match("^[A-Z]{2}$"));
                Assert.That(details.Alpha3Code, Does.Match("^[A-Z]{3}$"));
                Assert.That(details.NumericCode, Does.Match("^[0-9]{3}$"));
                Assert.That(details.LanguageCode, Does.Match("^[a-z]{2,3}$"));
                Assert.That(details.CurrencyCode, Does.Match("^[A-Z]{3}$"));
                Assert.That(details.CallingCode, Does.Match("^\\+[0-9]{1,3}$"));
            });
            index++;
        }
    }

    [Test]
    public void ZeroWeightComponentsAreNeverSelected()
    {
        Fake value = Fake.Create("en-US", 7203);
        Register(value, "en-US", ValidJson("Example City", true));

        int index = 0;
        while (index < 64)
        {
            Assert.That(value.Address().StreetName(), Is.EqualTo("Exemple"));
            index++;
        }
    }

    [Test]
    public void AddressStreamIsIndependentFromOtherProviderStreams()
    {
        Fake addressFirst = Fake.Create(7204);
        Fake untouchedPerson = Fake.Create(7204);
        Fake personFirst = Fake.Create(7204);
        Fake untouchedAddress = Fake.Create(7204);

        int index = 0;
        while (index < 32)
        {
            addressFirst.Address().BuildingNumber();
            addressFirst.Address().Street();
            addressFirst.Address().Unit();
            addressFirst.Address().PoBox();
            addressFirst.Address().PostalCode();
            personFirst.Person().FullName();
            personFirst.Education().Record();
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(addressFirst.Person().FullName(), Is.EqualTo(untouchedPerson.Person().FullName()));
            Assert.That(personFirst.Address().Street(), Is.EqualTo(untouchedAddress.Address().Street()));
        });
    }

    static bool DeclaredStreet(string value, AddressLocaleData data)
    {
        int nameIndex = 0;
        while (nameIndex < data.StreetNames.Length)
        {
            int typeIndex = 0;
            while (typeIndex < data.StreetTypes.Length)
            {
                if (value == data.StreetNames[nameIndex].Value + " " + data.StreetTypes[typeIndex].Name)
                {
                    return true;
                }
                typeIndex++;
            }
            nameIndex++;
        }
        return false;
    }

    static bool DeclaredUnit(string value, AddressLocaleData data)
    {
        int index = 0;
        while (index < data.UnitTypes.Length)
        {
            string prefix = data.UnitTypes[index].Name + " ";
            if (value.StartsWith(prefix) &&
                MatchesAnyMask(value.Substring(prefix.Length), data.UnitMasks))
            {
                return true;
            }
            index++;
        }
        return false;
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
        int valueIndex = 0;
        int maskIndex = 0;
        while (valueIndex < value.Length && maskIndex < mask.Length)
        {
            char expected = mask[maskIndex];
            if (expected == '#')
            {
                if (value[valueIndex] < '0' || value[valueIndex] > '9')
                {
                    return false;
                }
            }
            else if (expected == '?')
            {
                char actual = value[valueIndex];
                bool upper = actual >= 'A' && actual <= 'Z';
                bool lower = actual >= 'a' && actual <= 'z';
                if (!upper && !lower)
                {
                    return false;
                }
            }
            else if (expected == '*')
            {
                char actual = value[valueIndex];
                bool digit = actual >= '0' && actual <= '9';
                bool upper = actual >= 'A' && actual <= 'Z';
                bool lower = actual >= 'a' && actual <= 'z';
                if (!digit && !upper && !lower)
                {
                    return false;
                }
            }
            else if (value[valueIndex] != expected)
            {
                return false;
            }
            valueIndex++;
            maskIndex++;
        }
        return valueIndex == value.Length && maskIndex == mask.Length;
    }

    static void Register(Fake value, string locale, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        value.Locale().RegisterFromLocaleAndFields(
            locale,
            new Dictionary<string, object>
            {
                ["address.catalog"] = document.RootElement.Clone()
            });
    }

    static string ValidJson(string city, bool includeZeroWeightStreet)
    {
        string zeroWeightStreet = includeZeroWeightStreet
            ? ",{\"value\":\"Never Selected\",\"weight\":0}"
            : "";
        return $$"""
        {
          "components":{
            "buildingNumberMasks":[{"value":"###","weight":1}],
            "streetNames":[{"value":"Exemple","weight":1}{{zeroWeightStreet}}],
            "streetTypes":[{"name":"Rue","abbreviation":"r.","weight":1}],
            "unitTypes":[{"name":"Appartement","abbreviation":"app.","weight":1}],
            "unitMasks":[{"value":"##","weight":1}],
            "poBoxMasks":[{"value":"####","weight":1}],
            "poBoxLabel":"Boîte postale",
            "districts":[{"value":"Quartier Exemple","weight":1}],
            "cities":[{"value":"{{city}}","weight":1}],
            "counties":[{"value":"Comté Exemple","weight":1}],
            "subdivisions":[{"name":"Région Exemple","abbreviation":"ER","code":"XZ-ER","countryCode":"XZ","weight":1}],
            "postalMasks":[{"value":"EX-####","weight":1}],
            "countries":[{
              "name":"Pays Exemple","abbreviation":"P. Ex.",
              "alpha2Code":"XZ","alpha3Code":"XZZ","numericCode":"999",
              "language":"Langue Exemple","languageCode":"qaa",
              "currency":"Monnaie Exemple","currencyCode":"XCR",
              "callingCode":"+999","timeZoneId":"Europe/Paris",
              "postalMasks":[{"value":"EX-####","weight":1}],
              "bounds":{"southLatitude":41,"westLongitude":-5,"northLatitude":51,"eastLongitude":10},
              "weight":1
            }]
          },
          "templates":[{
            "id":"street",
            "weight":1,
            "lines":[{"value":"{buildingNumber} {streetName}","optional":false}],
            "singleLineSeparator":", ",
            "singleLineParts":[{"value":"{buildingNumber} {streetName}","optional":false}]
          }],
          "formatStyles":[{
            "id":"full",
            "streetType":"name",
            "unitType":"name",
            "subdivision":"name",
            "country":"name",
            "weight":1
          }]
        }
        """;
    }
}
