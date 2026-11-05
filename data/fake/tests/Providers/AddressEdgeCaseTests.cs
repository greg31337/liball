using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class AddressEdgeCaseTests
{
    const string LongRtlStreet = "الصفصاف الطويل الممتد عبر الحي التجريبي المرجعي البعيد";

    [Test]
    public void RuralDependentAlphanumericRtlLongLineUsesParentFallback()
    {
        FakeOptions options = FakeOptions.Create("ar-EG", 7801);
        options.MaximumOutputSize = 512;
        Fake value = Fake.Create(options);
        Register(value, "ar", LocaleJson("AB# #??"));

        string multiLine = value.Address().FullAddressFromStyle("full");
        string singleLine = value.Address().SingleLineAddressFromStyle("full");
        string[] lines = multiLine.Split('\n');

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Length.EqualTo(7));
            Assert.That(lines[0], Does.Match("^طريق ريفي [0-9]{4}$"));
            Assert.That(lines[1], Does.Contain(LongRtlStreet));
            Assert.That(lines[1].Length, Is.GreaterThan(60));
            Assert.That(lines[2], Does.Match("^وحدة [0-9]{2}$"));
            Assert.That(lines[3], Is.EqualTo("القرية التابعة"));
            Assert.That(lines[4], Is.EqualTo("مدينة المثال منطقة المثال"));
            Assert.That(lines[5], Does.Match("^[A-Za-z]{2}[0-9] [0-9][A-Za-z]{2}$"));
            Assert.That(lines[6], Is.EqualTo("بلاد المثال"));
            Assert.That(multiLine, Does.Contain("الصفصاف"));
            Assert.That(singleLine, Does.Not.Contain("\n"));
            Assert.That(singleLine, Does.Contain(" | القرية التابعة | "));
            Assert.That(value.Locale().FallbackTrace("address.catalog").SourceLocale, Is.EqualTo("ar"));
        });
    }

    [Test]
    public void MissingPostcodeIsEmptyAndItsOptionalSegmentsAreOmitted()
    {
        Fake value = Fake.Create("ar-EG", 7802);
        Register(value, "ar", LocaleJson(""));

        AddressRecord record = value.Address().Record();
        string multiLine = value.Address().FullAddressFromStyle("full");
        string singleLine = value.Address().SingleLineAddressFromStyle("full");
        string[] lines = multiLine.Split('\n');

        Assert.Multiple(() =>
        {
            Assert.That(record.PostalCode, Is.Empty);
            Assert.That(record.Geography.PostalCode, Is.Empty);
            Assert.That(value.Address().PostalCode(), Is.Empty);
            Assert.That(lines, Has.Length.EqualTo(6));
            Assert.That(lines, Has.None.Empty);
            Assert.That(multiLine, Does.Not.Contain("\n\n"));
            Assert.That(singleLine, Does.Not.Contain(" |  | "));
            Assert.That(singleLine, Does.Not.EndWith(" | "));
        });
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

    static string LocaleJson(string postalMask)
    {
        return $$"""
        {
          "components":{
            "buildingNumberMasks":[{"value":"###","weight":1}],
            "streetNames":[{"value":"{{LongRtlStreet}}","weight":1}],
            "streetTypes":[{"name":"طريق","abbreviation":"طر.","weight":1}],
            "unitTypes":[{"name":"وحدة","abbreviation":"و.","weight":1}],
            "unitMasks":[{"value":"##","weight":1}],
            "poBoxMasks":[{"value":"####","weight":1}],
            "poBoxLabel":"صندوق بريد",
            "districts":[{"value":"القرية التابعة","weight":1}],
            "cities":[{"value":"مدينة المثال","weight":1}],
            "counties":[{"value":"مقاطعة المثال","weight":1}],
            "subdivisions":[{
              "name":"منطقة المثال","abbreviation":"مم","code":"XZ-ER",
              "countryCode":"XZ","weight":1
            }],
            "postalMasks":[{"value":"{{postalMask}}","weight":1}],
            "countries":[{
              "name":"بلاد المثال","abbreviation":"ب.م.",
              "alpha2Code":"XZ","alpha3Code":"XZZ","numericCode":"999",
              "language":"العربية التجريبية","languageCode":"qaa",
              "currency":"عملة المثال","currencyCode":"XCR",
              "callingCode":"+999","timeZoneId":"Europe/Paris",
              "postalMasks":[{"value":"{{postalMask}}","weight":1}],
              "bounds":{"southLatitude":41,"westLongitude":-5,"northLatitude":51,"eastLongitude":10},
              "weight":1
            }]
          },
          "templates":[{
            "id":"rural",
            "weight":1,
            "lines":[
              {"value":"طريق ريفي {poBox}","optional":false},
              {"value":"{buildingNumber} {streetName} {streetType}","optional":false},
              {"value":"{unitType} {unit}","optional":false},
              {"value":"{district}","optional":false},
              {"value":"{city} {subdivision}","optional":false},
              {"value":"{postalCode}","optional":true},
              {"value":"{country}","optional":false}
            ],
            "singleLineSeparator":" | ",
            "singleLineParts":[
              {"value":"طريق ريفي {poBox}","optional":false},
              {"value":"{buildingNumber} {streetName} {streetType}","optional":false},
              {"value":"{unitType} {unit}","optional":false},
              {"value":"{district}","optional":false},
              {"value":"{city} {subdivision}","optional":false},
              {"value":"{postalCode}","optional":true},
              {"value":"{country}","optional":false}
            ]
          }],
          "formatStyles":[{
            "id":"full","streetType":"name","unitType":"name",
            "subdivision":"name","country":"name","weight":1
          }]
        }
        """;
    }
}
