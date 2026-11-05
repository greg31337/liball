using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class AddressFormattingTests
{
    [Test]
    public void MultiAndSingleLineAddressesAreDeterministicAndWellFormed()
    {
        AddressProvider first = Fake.Create(7301).Address();
        AddressProvider second = Fake.Create(7301).Address();
        bool observedUnit = false;
        bool observedOmittedUnit = false;
        bool observedDistrict = false;
        bool observedOmittedDistrict = false;

        int index = 0;
        while (index < 128)
        {
            string multiLine = first.FullAddressFromStyle("full");
            string expectedMultiLine = second.FullAddressFromStyle("full");
            string singleLine = first.SingleLineAddressFromStyle("full");
            string expectedSingleLine = second.SingleLineAddressFromStyle("full");
            string[] lines = multiLine.Split('\n');

            Assert.Multiple(() =>
            {
                Assert.That(multiLine, Is.EqualTo(expectedMultiLine));
                Assert.That(singleLine, Is.EqualTo(expectedSingleLine));
                Assert.That(multiLine, Does.Not.Contain("\r"));
                Assert.That(multiLine, Does.Not.Contain("\n\n"));
                Assert.That(lines, Has.All.Not.Empty);
                Assert.That(singleLine, Does.Not.Contain("\n"));
                Assert.That(singleLine, Does.Not.Contain(", ,"));
                Assert.That(singleLine, Does.Not.StartWith(","));
                Assert.That(singleLine, Does.Not.EndWith(","));
            });
            ObserveOptionalLines(
                lines,
                ref observedUnit,
                ref observedOmittedUnit,
                ref observedDistrict,
                ref observedOmittedDistrict);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(observedUnit, Is.True);
            Assert.That(observedOmittedUnit, Is.True);
            Assert.That(observedDistrict, Is.True);
            Assert.That(observedOmittedDistrict, Is.True);
        });
    }

    [Test]
    public void ExplicitStylesUseCorrelatedFullOrAbbreviatedForms()
    {
        AddressProvider full = Fake.Create(7302).Address();
        AddressProvider abbreviated = Fake.Create(7302).Address();

        int index = 0;
        while (index < 64)
        {
            string fullValue = full.SingleLineAddressFromStyle("full");
            string abbreviatedValue = abbreviated.SingleLineAddressFromStyle("abbreviated");

            Assert.Multiple(() =>
            {
                Assert.That(fullValue, Does.Match("(Exampleland|Samplestan|Demovia)$"));
                Assert.That(abbreviatedValue, Does.Match("(Ex\\.|Sm\\.|Dm\\.)$"));
                Assert.That(fullValue, Does.Match("(North|Central|South) Region"));
                Assert.That(abbreviatedValue, Does.Match("(NR|CR|SR) (EX|SA|DM)-"));
            });
            index++;
        }
    }

    [Test]
    public void CallerTemplatesControlOrderSeparatorsAndOmittedSegments()
    {
        Fake value = Fake.Create("fr-CA", 7303);
        Register(value, "fr", ValidJson());
        bool observedUnit = false;
        bool observedOmittedUnit = false;

        int index = 0;
        while (index < 64)
        {
            string singleLine = value.Address().SingleLineAddressFromStyle("full");
            string[] parts = singleLine.Split(" · ");

            Assert.Multiple(() =>
            {
                Assert.That(parts[0], Does.Match("^EX-[0-9]{4} Cité Exemple$"));
                Assert.That(parts[1], Does.Match("^[0-9]{3} Rue Exemple$"));
                Assert.That(parts[parts.Length - 1], Is.EqualTo("Pays Exemple"));
                Assert.That(singleLine, Does.Not.Contain(" ·  · "));
                Assert.That(parts.Length, Is.InRange(3, 4));
            });
            if (parts.Length == 4)
            {
                observedUnit = true;
                Assert.That(parts[2], Does.Match("^Appartement [0-9]{2}$"));
            }
            else
            {
                observedOmittedUnit = true;
            }
            index++;
        }

        string multiLine = value.Address().FullAddressFromStyle("full");
        string[] lines = multiLine.Split('\n');
        Assert.Multiple(() =>
        {
            Assert.That(observedUnit, Is.True);
            Assert.That(observedOmittedUnit, Is.True);
            Assert.That(lines[0], Does.Match("^EX-[0-9]{4} Cité Exemple$"));
            Assert.That(lines[1], Does.Match("^[0-9]{3} Rue Exemple$"));
            Assert.That(lines[lines.Length - 1], Is.EqualTo("Pays Exemple"));
            Assert.That(value.Locale().FallbackTrace("address.catalog").SourceLocale, Is.EqualTo("fr"));
        });
    }

    [Test]
    public void NativeAndLocalizedFormsUseTheirDocumentedStyles()
    {
        AddressProvider native = Fake.Create(7304).Address();
        AddressProvider explicitFull = Fake.Create(7304).Address();
        AddressProvider localized = Fake.Create(7305).Address();
        AddressProvider defaultStyle = Fake.Create(7305).Address();

        Assert.Multiple(() =>
        {
            Assert.That(native.NativeAddress(), Is.EqualTo(explicitFull.FullAddressFromStyle("full")));
            Assert.That(localized.LocalizedAddress(), Is.EqualTo(defaultStyle.FullAddress()));
        });
    }

    [Test]
    public void InvalidStylesAreRejectedWithoutConsumingAddressEntropy()
    {
        Fake actual = Fake.Create(7306);
        Fake expected = Fake.Create(7306);

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => actual.Address().FullAddressFromStyle(null)).ParamName,
                Is.EqualTo("style"));
            Assert.That(
                Assert.Throws<ArgumentException>(
                    () => actual.Address().SingleLineAddressFromStyle("missing")).ParamName,
                Is.EqualTo("style"));
        });
        Assert.That(
            actual.Address().RandomValue.Long(),
            Is.EqualTo(expected.Address().RandomValue.Long()));
    }

    [Test]
    public void AggregateFormattedOutputHonorsTheConfiguredLimit()
    {
        FakeOptions options = FakeOptions.Create("fr", 7307);
        options.MaximumOutputSize = 45;
        Fake value = Fake.Create(options);
        Register(value, "fr", ValidJson());

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => value.Address().FullAddressFromStyle("full"));

        Assert.That(exception.ParamName, Is.EqualTo("address"));
    }

    static void ObserveOptionalLines(
        string[] lines,
        ref bool observedUnit,
        ref bool observedOmittedUnit,
        ref bool observedDistrict,
        ref bool observedOmittedDistrict)
    {
        bool unit = lines.Any(line =>
            line.StartsWith("Apartment ") ||
            line.StartsWith("Suite ") ||
            line.StartsWith("Unit "));
        bool district = lines.Any(line =>
            line.EndsWith("District") ||
            line.EndsWith("Quarter") ||
            line.EndsWith("Ward"));
        observedUnit = observedUnit || unit;
        observedOmittedUnit = observedOmittedUnit || !unit;
        observedDistrict = observedDistrict || district;
        observedOmittedDistrict = observedOmittedDistrict || !district;
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

    static string ValidJson()
    {
        return """
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
            "cities":[{"value":"Cité Exemple","weight":1}],
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
            "lines":[
              {"value":"{postalCode} {city}","optional":false},
              {"value":"{buildingNumber} {streetType} {streetName}","optional":false},
              {"value":"{unitType} {unit}","optional":true},
              {"value":"{country}","optional":false}
            ],
            "singleLineSeparator":" · ",
            "singleLineParts":[
              {"value":"{postalCode} {city}","optional":false},
              {"value":"{buildingNumber} {streetType} {streetName}","optional":false},
              {"value":"{unitType} {unit}","optional":true},
              {"value":"{country}","optional":false}
            ]
          }],
          "formatStyles":[
            {"id":"full","streetType":"name","unitType":"name","subdivision":"name","country":"name","weight":1},
            {"id":"abbreviated","streetType":"abbreviation","unitType":"abbreviation","subdivision":"abbreviation","country":"abbreviation","weight":1}
          ]
        }
        """;
    }
}
