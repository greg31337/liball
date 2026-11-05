using System.Text.Json;

namespace diameter.Tests;

public class JsonDictionaryLoaderTests
{
    [Test]
    public void VersionOneDocumentLoadsEveryDefinitionAndRuleField()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "name": "custom-dictionary",
              "version": "2026.1",
              "dependencies": ["base"],
              "vendors": [
                { "id": 10415, "name": "3GPP" }
              ],
              "applications": [
                { "id": 4, "vendorId": 0, "name": "Credit-Control", "isAccounting": true }
              ],
              "types": [
                {
                  "name": "Example-Enum",
                  "vendorId": 0,
                  "baseType": "Integer32",
                  "parentTypeName": "Enumerated",
                  "parentTypeVendorId": 0,
                  "fixedDataLength": 4
                }
              ],
              "enumValues": [
                {
                  "name": "ENABLED",
                  "value": -1,
                  "typeName": "Example-Enum",
                  "typeVendorId": 0,
                  "avpCode": 100,
                  "avpVendorId": 0
                }
              ],
              "avps": [
                {
                  "code": 100,
                  "vendorId": 0,
                  "name": "Example-AVP",
                  "typeName": "Example-Enum",
                  "baseType": "integer32",
                  "requiredFlags": ["Mandatory", "Protected"],
                  "prohibitedFlags": ["VendorSpecific"],
                  "fixedDataLength": 4,
                  "groupedRules": [
                    {
                      "avpCode": 263,
                      "avpVendorId": 0,
                      "avpName": "Session-Id",
                      "minimumCount": 1,
                      "maximumCount": -1,
                      "fixedHeadPosition": 0,
                      "orderingGroup": 2
                    }
                  ],
                  "allowUnlistedAvps": false
                }
              ],
              "commands": [
                {
                  "code": 272,
                  "applicationId": 4,
                  "name": "Credit-Control",
                  "isRequest": true,
                  "requiredFlags": ["Request", "Proxyable"],
                  "prohibitedFlags": ["Error", "PotentiallyRetransmitted"],
                  "rules": [
                    {
                      "avpCode": 100,
                      "avpVendorId": 0,
                      "avpName": "Example-AVP",
                      "minimumCount": 1,
                      "maximumCount": 1,
                      "fixedHeadPosition": -1,
                      "orderingGroup": -1
                    }
                  ],
                  "allowUnlistedAvps": false
                }
              ]
            }
            """;

        DiameterDictionaryModule module = JsonDictionaryLoader.LoadDictionaryModule(json);

        Assert.Multiple(() =>
        {
            Assert.That(JsonDictionarySchema.CurrentVersion, Is.EqualTo(1));
            Assert.That(module.Name, Is.EqualTo("custom-dictionary"));
            Assert.That(module.Version, Is.EqualTo("2026.1"));
            Assert.That(module.Dependencies, Is.EqualTo(new[] { "base" }));
            Assert.That(module.Vendors[0].Id, Is.EqualTo(10415U));
            Assert.That(module.Vendors[0].Name, Is.EqualTo("3GPP"));
            Assert.That(module.Applications[0].Id, Is.EqualTo(4U));
            Assert.That(module.Applications[0].IsAccounting, Is.True);
            Assert.That(module.Types[0].BaseType, Is.EqualTo(DiameterAvpBaseType.Integer32));
            Assert.That(module.Types[0].ParentTypeName, Is.EqualTo("Enumerated"));
            Assert.That(module.Types[0].FixedDataLength, Is.EqualTo(4));
            Assert.That(module.EnumValues[0].Value, Is.EqualTo(-1));
            Assert.That(module.EnumValues[0].AvpCode, Is.EqualTo(100U));
            Assert.That(
                module.Avps[0].RequiredFlags,
                Is.EqualTo(DiameterAvpFlags.Mandatory | DiameterAvpFlags.Protected));
            Assert.That(
                module.Avps[0].ProhibitedFlags,
                Is.EqualTo(DiameterAvpFlags.VendorSpecific));
            Assert.That(module.Avps[0].AllowUnlistedAvps, Is.False);
            Assert.That(module.Avps[0].GroupedRules[0].MaximumCount, Is.EqualTo(-1));
            Assert.That(module.Avps[0].GroupedRules[0].FixedHeadPosition, Is.EqualTo(0));
            Assert.That(module.Avps[0].GroupedRules[0].OrderingGroup, Is.EqualTo(2));
            Assert.That(
                module.Commands[0].RequiredFlags,
                Is.EqualTo(DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable));
            Assert.That(
                module.Commands[0].ProhibitedFlags,
                Is.EqualTo(
                    DiameterMessageFlags.Error |
                    DiameterMessageFlags.PotentiallyRetransmitted));
            Assert.That(module.Commands[0].AllowUnlistedAvps, Is.False);
            Assert.That(module.Commands[0].Rules[0].AvpName, Is.EqualTo("Example-AVP"));
        });
    }

    [Test]
    public void OptionalCollectionsAndFieldsUseDocumentedSchemaDefaults()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "name": "minimal",
              "applications": [{ "id": 1, "name": "Application" }],
              "types": [{ "name": "Data", "baseType": "OctetString" }],
              "avps": [{ "code": 1, "name": "Data", "typeName": "Data", "baseType": "OctetString" }],
              "commands": [{ "code": 1, "applicationId": 1, "name": "Command", "isRequest": false }]
            }
            """;

        DiameterDictionaryModule module = JsonDictionaryLoader.LoadDictionaryModule(json);

        Assert.Multiple(() =>
        {
            Assert.That(module.Version, Is.Empty);
            Assert.That(module.Dependencies, Is.Empty);
            Assert.That(module.Vendors, Is.Empty);
            Assert.That(module.EnumValues, Is.Empty);
            Assert.That(module.Applications[0].VendorId, Is.Zero);
            Assert.That(module.Applications[0].IsAccounting, Is.False);
            Assert.That(module.Types[0].FixedDataLength, Is.EqualTo(-1));
            Assert.That(module.Avps[0].VendorId, Is.Zero);
            Assert.That(module.Avps[0].RequiredFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(module.Avps[0].GroupedRules, Is.Empty);
            Assert.That(module.Avps[0].AllowUnlistedAvps, Is.True);
            Assert.That(module.Commands[0].RequiredFlags, Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(module.Commands[0].Rules, Is.Empty);
            Assert.That(module.Commands[0].AllowUnlistedAvps, Is.True);
        });
    }

    [Test]
    public void LoadedModuleCanBeRegisteredWithoutRetainingJsonDocumentState()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "name": "loaded",
              "vendors": [{ "id": 9, "name": "Vendor" }]
            }
            """;
        DiameterDictionaryModule module = JsonDictionaryLoader.LoadDictionaryModule(json);
        DictionaryBuilder builder = new DictionaryBuilder();

        DictionaryRegistrationResult result = builder.RegisterDictionaryModule(module);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DictionaryRegistrationStatus.Success));
            Assert.That(result.Snapshot.Modules[0].Name, Is.EqualTo("loaded"));
            Assert.That(result.Snapshot.Vendors[0].Name, Is.EqualTo("Vendor"));
        });
    }

    [TestCase(
        "{\"schemaVersion\":2,\"name\":\"test\"}",
        "$.schemaVersion")]
    [TestCase(
        "{\"schemaVersion\":1,\"name\":\"test\",\"unknown\":true}",
        "$.unknown")]
    [TestCase(
        "{\"schemaVersion\":1,\"name\":\"test\",\"name\":\"again\"}",
        "$.name")]
    [TestCase(
        "{\"schemaVersion\":1,\"name\":\"test\",\"vendors\":[{\"id\":4294967296,\"name\":\"x\"}]}",
        "$.vendors[0].id")]
    [TestCase(
        "{\"schemaVersion\":1,\"name\":\"test\",\"types\":[{\"name\":\"x\",\"baseType\":\"Unknown\"}]}",
        "$.types[0].baseType")]
    [TestCase(
        "{\"schemaVersion\":1,\"name\":\"test\",\"commands\":[{\"code\":1,\"applicationId\":1,\"name\":\"x\",\"isRequest\":true,\"requiredFlags\":[\"Unknown\"]}]}",
        "$.commands[0].requiredFlags[0]")]
    [TestCase(
        "{\"schemaVersion\":1,\"name\":\"test\",\"commands\":[{\"code\":1,\"applicationId\":1,\"name\":\"x\",\"isRequest\":true,\"rules\":[{\"avpCode\":1,\"avpName\":\"x\",\"minimumCount\":2,\"maximumCount\":1}]}]}",
        "$.commands[0].rules[0].maximumCount")]
    [TestCase(
        "{\"schemaVersion\":1,\"name\":\"test\",\"enumValues\":[{\"name\":\"x\",\"value\":1}]}",
        "$.enumValues[0]")]
    public void SchemaViolationsIdentifyExactJsonPath(string json, string expectedPath)
    {
        DiameterDictionaryException? exception = Assert.Throws<DiameterDictionaryException>(
            () => JsonDictionaryLoader.LoadDictionaryModule(json));

        Assert.That(exception!.Message, Does.Contain("at " + expectedPath + ":"));
    }

    [Test]
    public void MissingDocumentAndRequiredPropertiesHaveStableErrors()
    {
        DiameterDictionaryException? missingDocument =
            Assert.Throws<DiameterDictionaryException>(
                () => JsonDictionaryLoader.LoadDictionaryModule(null));
        DiameterDictionaryException? missingSchema =
            Assert.Throws<DiameterDictionaryException>(
                () => JsonDictionaryLoader.LoadDictionaryModule("{\"name\":\"test\"}"));
        DiameterDictionaryException? missingName =
            Assert.Throws<DiameterDictionaryException>(
                () => JsonDictionaryLoader.LoadDictionaryModule("{\"schemaVersion\":1}"));

        Assert.Multiple(() =>
        {
            Assert.That(missingDocument!.Message, Does.Contain("at $:"));
            Assert.That(missingSchema!.Message, Does.Contain("at $.schemaVersion:"));
            Assert.That(missingName!.Message, Does.Contain("at $.name:"));
        });
    }

    [Test]
    public void MalformedJsonPreservesSystemTextJsonLineAndByteLocation()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "name":
            }
            """;

        JsonException? exception = Assert.Catch<JsonException>(
            () => JsonDictionaryLoader.LoadDictionaryModule(json));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.LineNumber, Is.Not.Null);
            Assert.That(exception.LineNumber, Is.GreaterThan(0L));
            Assert.That(exception.BytePositionInLine, Is.Not.Null);
            Assert.That(exception.Message, Does.Contain("LineNumber"));
            Assert.That(exception.Message, Does.Contain("BytePositionInLine"));
        });
    }

    [TestCase("-2147483648", int.MinValue)]
    [TestCase("2147483647", int.MaxValue)]
    public void SignedIntegerBoundariesLoadExactly(string value, int expected)
    {
        string json = "{\"schemaVersion\":1,\"name\":\"test\"," +
            "\"enumValues\":[{\"name\":\"value\",\"value\":" + value +
            ",\"typeName\":\"type\"}]}";

        DiameterDictionaryModule module = JsonDictionaryLoader.LoadDictionaryModule(json);

        Assert.That(module.EnumValues[0].Value, Is.EqualTo(expected));
    }
}
