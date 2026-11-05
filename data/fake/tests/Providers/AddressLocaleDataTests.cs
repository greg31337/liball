using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class AddressLocaleDataTests
{
    [Test]
    public void BundledCatalogDefinesComponentsTemplatesAndStyles()
    {
        Fake value = Fake.Create(7101);
        AddressLocaleData data = AddressLocaleDataParser.Resolve(value.Context);
        FallbackTrace trace = value.Locale().FallbackTrace("address.catalog");

        Assert.Multiple(() =>
        {
            Assert.That(data.BuildingNumberMasks, Has.Length.EqualTo(2));
            Assert.That(data.StreetNames, Has.Length.EqualTo(12));
            Assert.That(data.StreetTypes, Has.Length.EqualTo(6));
            Assert.That(data.UnitTypes, Has.Length.EqualTo(3));
            Assert.That(data.UnitMasks, Has.Length.EqualTo(2));
            Assert.That(data.PoBoxMasks, Has.Length.EqualTo(1));
            Assert.That(data.Districts, Has.Length.EqualTo(6));
            Assert.That(data.Cities, Has.Length.EqualTo(6));
            Assert.That(data.Counties, Has.Length.EqualTo(6));
            Assert.That(data.Subdivisions, Has.Length.EqualTo(6));
            Assert.That(data.PostalMasks, Has.Length.EqualTo(6));
            Assert.That(data.Countries, Has.Length.EqualTo(6));
            Assert.That(data.Templates, Has.Length.EqualTo(2));
            Assert.That(data.FormatStyles, Has.Length.EqualTo(2));
            Assert.That(data.StreetTypes[0].Abbreviation, Is.EqualTo("Ave."));
            Assert.That(data.Subdivisions[0].Code, Is.EqualTo("XA-NR"));
            Assert.That(data.Subdivisions[0].CountryCode, Is.EqualTo("XA"));
            Assert.That(data.PostalMasks[0].Value, Is.EqualTo("EX-####"));
            Assert.That(data.Countries[0].PostalMasks[0].Value, Is.EqualTo("EX-####"));
            Assert.That(data.Countries[0].SouthLatitude, Is.EqualTo(30.0));
            Assert.That(data.Countries[0].WestLongitude, Is.EqualTo(-125.0));
            Assert.That(data.Countries[0].NorthLatitude, Is.EqualTo(49.0));
            Assert.That(data.Countries[0].EastLongitude, Is.EqualTo(-114.0));
            Assert.That(data.Templates[0].Lines[1].Optional, Is.True);
            Assert.That(data.Templates[0].SingleLineSeparator, Is.EqualTo(", "));
            Assert.That(data.Templates[0].SingleLineParts[3].Value, Does.Contain("{postalCode}"));
            Assert.That(data.FormatStyles[1].StreetType, Is.EqualTo("abbreviation"));
            Assert.That(trace.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(trace.Provenance, Does.Contain("fictional address data"));
        });
    }

    [Test]
    public void WeightedAddressDataSelectionIsDeterministicAndDeclared()
    {
        Fake firstFake = Fake.Create(7102);
        Fake secondFake = Fake.Create(7102);
        AddressLocaleData firstData = AddressLocaleDataParser.Resolve(firstFake.Context);
        AddressLocaleData secondData = AddressLocaleDataParser.Resolve(secondFake.Context);
        HashSet<string> streets = new HashSet<string>();
        HashSet<string> types = new HashSet<string>();
        HashSet<string> subdivisions = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            AddressValueData firstStreet = firstData.Choose(
                firstFake.Address().RandomValue,
                firstData.StreetNames);
            AddressValueData secondStreet = secondData.Choose(
                secondFake.Address().RandomValue,
                secondData.StreetNames);
            AddressTypeData firstType = firstData.ChooseType(
                firstFake.Address().RandomValue,
                firstData.StreetTypes);
            AddressTypeData secondType = secondData.ChooseType(
                secondFake.Address().RandomValue,
                secondData.StreetTypes);
            AddressCodedData firstSubdivision = firstData.ChooseCoded(
                firstFake.Address().RandomValue,
                firstData.Subdivisions);
            AddressCodedData secondSubdivision = secondData.ChooseCoded(
                secondFake.Address().RandomValue,
                secondData.Subdivisions);

            Assert.Multiple(() =>
            {
                Assert.That(firstStreet.Value, Is.EqualTo(secondStreet.Value));
                Assert.That(firstType.Name, Is.EqualTo(secondType.Name));
                Assert.That(firstSubdivision.Code, Is.EqualTo(secondSubdivision.Code));
            });
            streets.Add(firstStreet.Value);
            types.Add(firstType.Name);
            subdivisions.Add(firstSubdivision.Code);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                streets.Count,
                Is.EqualTo(firstData.StreetNames.Count(value => firstData.Applicable(value.Safety))));
            Assert.That(types.Count, Is.EqualTo(firstData.StreetTypes.Length));
            Assert.That(
                subdivisions.Count,
                Is.EqualTo(firstData.Subdivisions.Count(value => firstData.Applicable(value.Safety))));
        });
    }

    [Test]
    public void CallerAddressDataUsesParentFallbackAndNormalization()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 7103);
        options.NormalizationMode = NormalizationMode.Nfc;
        Fake value = Fake.Create(options);
        Register(value, "fr", ValidJson("Cite\u0301 Exemple", "Re\u0301gion Exemple"));

        AddressLocaleData data = AddressLocaleDataParser.Resolve(value.Context);

        Assert.Multiple(() =>
        {
            Assert.That(data.Cities[0].Value, Is.EqualTo("Cité Exemple"));
            Assert.That(data.Subdivisions[0].Name, Is.EqualTo("Région Exemple"));
            Assert.That(data.StreetTypes[0].Name, Is.EqualTo("Rue"));
            Assert.That(data.StreetTypes[0].Abbreviation, Is.EqualTo("r."));
            Assert.That(data.Templates[0].Lines[0].Value, Is.EqualTo("{buildingNumber} {streetType} {streetName}"));
            Assert.That(data.FormatStyles[0].Subdivision, Is.EqualTo("abbreviation"));
            Assert.That(value.Locale().FallbackTrace("address.catalog").SourceLocale, Is.EqualTo("fr"));
        });
    }

    [Test]
    public void MalformedAddressSchemasProduceFieldSpecificDiagnostics()
    {
        string valid = ValidJson("Example City", "Example Region");
        int countryPostalIndex = valid.LastIndexOf("EX-####", StringComparison.Ordinal);
        string undeclaredCountryPostal = valid.Remove(countryPostalIndex, 7).Insert(countryPostalIndex, "FR-####");

        AssertInvalid(valid.Replace("\"components\":", "\"parts\":"), "requires member 'components'");
        AssertInvalid(valid.Replace("EX-####", "FIXED"), "must contain at least one mask placeholder");
        AssertInvalid(valid.Replace("EX-####", "   "), "must be empty or contain non-whitespace text");
        AssertInvalid(valid.Replace("{postalCode}", "{unknown}"), "references unknown component 'unknown'");
        AssertInvalid(valid.Replace("\"streetType\":\"name\"", "\"streetType\":\"short\""), "must be 'name' or 'abbreviation'");
        AssertInvalid(valid.Replace("\"optional\":false", "\"optional\":\"no\""), "optional must be a boolean");
        AssertInvalid(valid.Replace("\"weight\":1", "\"weight\":-1"), "weight must be finite and nonnegative");
        AssertInvalid(valid.Replace("\"alpha2Code\":\"XZ\"", "\"alpha2Code\":\"xz\""), "2 uppercase ASCII letters");
        AssertInvalid(valid.Replace("\"numericCode\":\"999\"", "\"numericCode\":\"99A\""), "3 ASCII digits");
        AssertInvalid(valid.Replace("\"languageCode\":\"qaa\"", "\"languageCode\":\"QAA\""), "lowercase ASCII letters");
        AssertInvalid(valid.Replace("\"callingCode\":\"+999\"", "\"callingCode\":\"999\""), "'+' followed by one to three ASCII digits");
        AssertInvalid(valid.Replace("Europe/Paris", "Mars/Olympus"), "is not a bundled IANA time-zone identifier");
        AssertInvalid(valid.Replace("\"countryCode\":\"XZ\"", "\"countryCode\":\"XY\""), "does not reference a declared country");
        AssertInvalid(valid.Replace("\"code\":\"XZ-ER\"", "\"code\":\"XY-ER\""), "must start with its countryCode and a hyphen");
        AssertInvalid(valid.Replace("\"northLatitude\":51", "\"northLatitude\":40"), "northLatitude must be greater than or equal to southLatitude");
        AssertInvalid(valid.Replace("\"eastLongitude\":10", "\"eastLongitude\":181"), "must be finite and between -180 and 180");
        AssertInvalid(
            valid.Replace(
                "\"streetNames\":[{\"value\":\"Exemple\",\"weight\":1}]",
                "\"streetNames\":[{\"value\":\"Exemple\",\"weight\":1,\"safetyMode\":\"unknown\"}]"),
            "safetyMode must be 'reserved', 'syntheticPlausible', or 'unrestrictedShape'");
        AssertInvalid(undeclaredCountryPostal, "is not declared in components.postalMasks");
        AssertInvalid(
            valid.Replace(
                "{\"name\":\"Example Region\",\"abbreviation\":\"ER\",\"code\":\"XZ-ER\",\"countryCode\":\"XZ\",\"weight\":1}",
                "{\"name\":\"Example Region\",\"abbreviation\":\"ER\",\"code\":\"XZ-ER\",\"countryCode\":\"XZ\",\"weight\":1},{\"name\":\"Other Region\",\"abbreviation\":\"OR\",\"code\":\"XZ-ER\",\"countryCode\":\"XZ\",\"weight\":1}"),
            "code duplicates 'XZ-ER'");
    }

    [Test]
    public void AddressDataLimitsFailWithoutConsumingAddressEntropy()
    {
        FakeOptions options = FakeOptions.Create("en-US", 7104);
        options.MaximumOutputSize = 10;
        Fake actual = Fake.Create(options);
        Register(actual, "en-US", ValidJson("Eleven chars", "Region"));
        Fake expected = Fake.Create(options);

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => AddressLocaleDataParser.Resolve(actual.Context)).ParamName,
            Is.EqualTo("value"));
        Assert.That(
            actual.Address().RandomValue.Long(),
            Is.EqualTo(expected.Address().RandomValue.Long()));
    }

    static void AssertInvalid(string json, string message)
    {
        Fake value = Fake.Create("en-US", 7199);
        Register(value, "en-US", json);

        FakeException exception = Assert.Throws<FakeException>(
            () => AddressLocaleDataParser.Resolve(value.Context));

        Assert.That(exception.Message, Does.StartWith("Resolved locale field 'address.catalog'"));
        Assert.That(exception.Message, Does.Contain(message));
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

    static string ValidJson(string city, string subdivision)
    {
        return $$"""
        {
          "components":{
            "buildingNumberMasks":[{"value":"###","weight":1}],
            "streetNames":[{"value":"Exemple","weight":1}],
            "streetTypes":[{"name":"Rue","abbreviation":"r.","weight":1}],
            "unitTypes":[{"name":"Appartement","abbreviation":"app.","weight":1}],
            "unitMasks":[{"value":"##","weight":1}],
            "poBoxMasks":[{"value":"####","weight":1}],
            "poBoxLabel":"Boîte postale",
            "districts":[{"value":"Quartier Exemple","weight":1}],
            "cities":[{"value":"{{city}}","weight":1}],
            "counties":[{"value":"Comté Exemple","weight":1}],
            "subdivisions":[{"name":"{{subdivision}}","abbreviation":"ER","code":"XZ-ER","countryCode":"XZ","weight":1}],
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
            "lines":[
              {"value":"{buildingNumber} {streetType} {streetName}","optional":false},
              {"value":"{postalCode} {city}","optional":false}
            ],
            "singleLineSeparator":" · ",
            "singleLineParts":[
              {"value":"{buildingNumber} {streetType} {streetName}","optional":false},
              {"value":"{postalCode} {city}","optional":false}
            ]
          }],
          "formatStyles":[{
            "id":"abbreviated",
            "streetType":"name",
            "unitType":"abbreviation",
            "subdivision":"abbreviation",
            "country":"name",
            "weight":1
          }]
        }
        """;
    }
}
