using System.Collections.Concurrent;
using System.Text.Json;

namespace diameter.Tests;

public class PublicDictionaryCoverageTests
{
    [Test]
    public void CompleteJsonModulePreservesEveryFieldThroughPublicRegistrationAndLookups()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "name": "complete-public-module",
              "version": "2026.2",
              "dependencies": ["rfc6733"],
              "vendors": [{ "id": 424242, "name": "Example Vendor" }],
              "applications": [{
                "id": 7001,
                "vendorId": 424242,
                "name": "Example Application",
                "isAccounting": true
              }],
              "types": [{
                "name": "Example-Enum",
                "vendorId": 424242,
                "baseType": "Integer32",
                "parentTypeName": "Enumerated",
                "parentTypeVendorId": 0,
                "fixedDataLength": 4
              }],
              "enumValues": [{
                "name": "NEGATIVE",
                "value": -7,
                "typeName": "Example-Enum",
                "typeVendorId": 424242,
                "avpCode": 7002,
                "avpVendorId": 424242
              }],
              "avps": [
                {
                  "code": 7002,
                  "vendorId": 424242,
                  "name": "Example-Child",
                  "typeName": "Example-Enum",
                  "baseType": "Integer32",
                  "requiredFlags": ["VendorSpecific", "Mandatory"],
                  "prohibitedFlags": ["Protected"],
                  "fixedDataLength": 4,
                  "allowUnlistedAvps": true
                },
                {
                  "code": 7003,
                  "vendorId": 424242,
                  "name": "Example-Group",
                  "typeName": "Grouped",
                  "baseType": "Grouped",
                  "requiredFlags": ["VendorSpecific"],
                  "prohibitedFlags": ["Protected"],
                  "fixedDataLength": -1,
                  "groupedRules": [{
                    "avpCode": 7002,
                    "avpVendorId": 424242,
                    "avpName": "Example-Child",
                    "minimumCount": 1,
                    "maximumCount": 2,
                    "fixedHeadPosition": 0,
                    "orderingGroup": 3
                  }],
                  "allowUnlistedAvps": false
                }
              ],
              "commands": [{
                "code": 7004,
                "applicationId": 7001,
                "name": "Example-Command",
                "isRequest": true,
                "requiredFlags": ["Request", "Proxyable"],
                "prohibitedFlags": ["Error", "PotentiallyRetransmitted"],
                "rules": [{
                  "avpCode": 7003,
                  "avpVendorId": 424242,
                  "avpName": "Example-Group",
                  "minimumCount": 1,
                  "maximumCount": 1,
                  "fixedHeadPosition": 0,
                  "orderingGroup": 1
                }],
                "allowUnlistedAvps": false
              }]
            }
            """;
        IDiameterDictionaryLoader loader = new DiameterJsonDictionaryLoader();
        IDiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionaryModule module = loader.LoadDictionaryModule(json);
        DiameterDictionaryRegistrationResult registration =
            dictionary.RegisterDictionaryModule(module);
        DiameterVendorDefinition vendor = dictionary.FindDictionaryVendorById(424242U)!;
        DiameterApplicationDefinition application =
            dictionary.FindDictionaryApplicationByName(424242U, "example application")!;
        DiameterTypeDefinition type =
            dictionary.FindDictionaryTypeByName(424242U, "example-enum")!;
        DiameterEnumValueDefinition enumValue =
            dictionary.FindDictionaryEnumValueByAvpNumber(424242U, 7002U, -7)!;
        DiameterAvpDefinition child =
            dictionary.FindDictionaryAvpByName(424242U, "example-child")!;
        DiameterAvpDefinition group =
            dictionary.FindDictionaryAvpByCode(424242U, 7003U)!;
        DiameterAvpRuleDefinition groupedRule =
            dictionary.FindDictionaryGroupedRuleByName(
                424242U,
                "example-group",
                424242U,
                "example-child")!;
        DiameterCommandDefinition command =
            dictionary.FindDictionaryCommandByName(7001U, "example-command", true)!;
        DiameterAvpRuleDefinition commandRule =
            dictionary.FindDictionaryCommandRuleByCode(
                7001U,
                7004U,
                true,
                424242U,
                7003U)!;

        Assert.Multiple(() =>
        {
            Assert.That(module.Name, Is.EqualTo("complete-public-module"));
            Assert.That(module.Version, Is.EqualTo("2026.2"));
            Assert.That(module.Dependencies, Is.EqualTo(new[] { "rfc6733" }));
            Assert.That(module.Vendors, Has.Count.EqualTo(1));
            Assert.That(module.Applications, Has.Count.EqualTo(1));
            Assert.That(module.Types, Has.Count.EqualTo(1));
            Assert.That(module.EnumValues, Has.Count.EqualTo(1));
            Assert.That(module.Avps, Has.Count.EqualTo(2));
            Assert.That(module.Commands, Has.Count.EqualTo(1));

            Assert.That(vendor.Id, Is.EqualTo(424242U));
            Assert.That(vendor.Name, Is.EqualTo("Example Vendor"));
            Assert.That(application.Id, Is.EqualTo(7001U));
            Assert.That(application.VendorId, Is.EqualTo(424242U));
            Assert.That(application.Name, Is.EqualTo("Example Application"));
            Assert.That(application.IsAccounting, Is.True);
            Assert.That(type.Name, Is.EqualTo("Example-Enum"));
            Assert.That(type.VendorId, Is.EqualTo(424242U));
            Assert.That(type.BaseType, Is.EqualTo(DiameterAvpBaseType.Integer32));
            Assert.That(type.ParentTypeName, Is.EqualTo("Enumerated"));
            Assert.That(type.ParentTypeVendorId, Is.Zero);
            Assert.That(type.FixedDataLength, Is.EqualTo(4));
            Assert.That(enumValue.Name, Is.EqualTo("NEGATIVE"));
            Assert.That(enumValue.Value, Is.EqualTo(-7));
            Assert.That(enumValue.TypeName, Is.EqualTo("Example-Enum"));
            Assert.That(enumValue.TypeVendorId, Is.EqualTo(424242U));
            Assert.That(enumValue.AvpCode, Is.EqualTo(7002U));
            Assert.That(enumValue.AvpVendorId, Is.EqualTo(424242U));
            Assert.That(child.Code, Is.EqualTo(7002U));
            Assert.That(child.VendorId, Is.EqualTo(424242U));
            Assert.That(child.Name, Is.EqualTo("Example-Child"));
            Assert.That(child.TypeName, Is.EqualTo("Example-Enum"));
            Assert.That(child.BaseType, Is.EqualTo(DiameterAvpBaseType.Integer32));
            Assert.That(child.RequiredFlags, Is.EqualTo(
                DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory));
            Assert.That(child.ProhibitedFlags, Is.EqualTo(DiameterAvpFlags.Protected));
            Assert.That(child.FixedDataLength, Is.EqualTo(4));
            Assert.That(child.GroupedRules, Is.Empty);
            Assert.That(child.AllowUnlistedAvps, Is.True);
            Assert.That(group.Code, Is.EqualTo(7003U));
            Assert.That(group.VendorId, Is.EqualTo(424242U));
            Assert.That(group.Name, Is.EqualTo("Example-Group"));
            Assert.That(group.TypeName, Is.EqualTo("Grouped"));
            Assert.That(group.BaseType, Is.EqualTo(DiameterAvpBaseType.Grouped));
            Assert.That(group.RequiredFlags, Is.EqualTo(DiameterAvpFlags.VendorSpecific));
            Assert.That(group.ProhibitedFlags, Is.EqualTo(DiameterAvpFlags.Protected));
            Assert.That(group.FixedDataLength, Is.EqualTo(-1));
            Assert.That(group.GroupedRules, Has.Count.EqualTo(1));
            Assert.That(group.AllowUnlistedAvps, Is.False);
            AssertRule(groupedRule, 7002U, 424242U, "Example-Child", 1, 2, 0, 3);
            Assert.That(command.Code, Is.EqualTo(7004U));
            Assert.That(command.ApplicationId, Is.EqualTo(7001U));
            Assert.That(command.Name, Is.EqualTo("Example-Command"));
            Assert.That(command.IsRequest, Is.True);
            Assert.That(command.RequiredFlags, Is.EqualTo(
                DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable));
            Assert.That(command.ProhibitedFlags, Is.EqualTo(
                DiameterMessageFlags.Error |
                DiameterMessageFlags.PotentiallyRetransmitted));
            Assert.That(command.Rules, Has.Count.EqualTo(1));
            Assert.That(command.AllowUnlistedAvps, Is.False);
            AssertRule(commandRule, 7003U, 424242U, "Example-Group", 1, 1, 0, 1);
            Assert.That(registration.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
            Assert.That(registration.Code, Is.EqualTo("module-registered"));
            Assert.That(registration.Message, Is.Not.Empty);
            Assert.That(registration.Snapshot.Revision, Is.EqualTo(10L));
        });
    }

    [Test]
    public void EveryPublicLookupReturnsNullForMissingIdentityScopeNameOrDirection()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.FindDictionaryModuleByName(null), Is.Null);
            Assert.That(dictionary.FindDictionaryModuleByName("missing"), Is.Null);
            Assert.That(dictionary.FindDictionaryVendorById(uint.MaxValue), Is.Null);
            Assert.That(dictionary.FindDictionaryVendorByName("missing"), Is.Null);
            Assert.That(dictionary.FindDictionaryApplicationById(424242U, 4U), Is.Null);
            Assert.That(dictionary.FindDictionaryApplicationByName(0U, "missing"), Is.Null);
            Assert.That(dictionary.FindDictionaryTypeByName(424242U, "Unsigned32"), Is.Null);
            Assert.That(dictionary.FindDictionaryEnumValueByTypeNumber(
                0U,
                "Disconnect-Cause",
                int.MaxValue), Is.Null);
            Assert.That(dictionary.FindDictionaryEnumValueByTypeName(
                0U,
                "Disconnect-Cause",
                "missing"), Is.Null);
            Assert.That(dictionary.FindDictionaryEnumValueByAvpNumber(
                0U,
                DiameterAvpCodes.DisconnectCause,
                int.MinValue), Is.Null);
            Assert.That(dictionary.FindDictionaryEnumValueByAvpName(
                0U,
                DiameterAvpCodes.DisconnectCause,
                "missing"), Is.Null);
            Assert.That(dictionary.FindDictionaryAvpByCode(424242U, 264U), Is.Null);
            Assert.That(dictionary.FindDictionaryAvpByName(0U, null), Is.Null);
            Assert.That(dictionary.FindDictionaryCommandByCode(
                0U,
                DiameterCommandCodes.DeviceWatchdog,
                false), Is.Not.Null);
            Assert.That(dictionary.FindDictionaryCommandByCode(4U, 280U, true), Is.Null);
            Assert.That(dictionary.FindDictionaryCommandByName(
                0U,
                "missing",
                true), Is.Null);
            Assert.That(dictionary.FindDictionaryGroupedRuleByCode(
                0U,
                DiameterAvpCodes.ProxyInfo,
                424242U,
                DiameterAvpCodes.ProxyState), Is.Null);
            Assert.That(dictionary.FindDictionaryGroupedRuleByName(
                0U,
                "missing",
                0U,
                "proxy-state"), Is.Null);
            Assert.That(dictionary.FindDictionaryGroupedRuleByName(
                0U,
                "proxy-info",
                424242U,
                "proxy-state"), Is.Null);
            Assert.That(dictionary.FindDictionaryCommandRuleByCode(
                0U,
                DiameterCommandCodes.DeviceWatchdog,
                true,
                424242U,
                DiameterAvpCodes.OriginHost), Is.Null);
            Assert.That(dictionary.FindDictionaryCommandRuleByName(
                0U,
                "missing",
                true,
                0U,
                "origin-host"), Is.Null);
            Assert.That(dictionary.FindDictionaryCommandRuleByName(
                0U,
                "device-watchdog",
                true,
                424242U,
                "origin-host"), Is.Null);
        });
    }

    [Test]
    public void PublicSnapshotsDeepCloneEveryDefinitionCollectionAndNestedRule()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionarySnapshot first = dictionary.ReadDictionarySnapshot();
        DiameterDictionarySnapshot second = dictionary.ReadDictionarySnapshot();
        DiameterAvpDefinition firstProxy = FindSnapshotAvp(first, DiameterAvpCodes.ProxyInfo);
        DiameterAvpDefinition secondProxy = FindSnapshotAvp(second, DiameterAvpCodes.ProxyInfo);
        DiameterCommandDefinition firstWatchdog = FindSnapshotCommand(
            first,
            DiameterCommandCodes.DeviceWatchdog,
            true);
        DiameterCommandDefinition secondWatchdog = FindSnapshotCommand(
            second,
            DiameterCommandCodes.DeviceWatchdog,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(first.Modules, Is.Not.SameAs(second.Modules));
            Assert.That(first.Vendors, Is.Not.SameAs(second.Vendors));
            Assert.That(first.Applications, Is.Not.SameAs(second.Applications));
            Assert.That(first.Types, Is.Not.SameAs(second.Types));
            Assert.That(first.EnumValues, Is.Not.SameAs(second.EnumValues));
            Assert.That(first.Avps, Is.Not.SameAs(second.Avps));
            Assert.That(first.Commands, Is.Not.SameAs(second.Commands));
            Assert.That(first.Modules[0], Is.Not.SameAs(second.Modules[0]));
            Assert.That(first.Modules[0].Dependencies,
                Is.Not.SameAs(second.Modules[0].Dependencies));
            Assert.That(firstProxy, Is.Not.SameAs(secondProxy));
            Assert.That(firstProxy.GroupedRules, Is.Not.SameAs(secondProxy.GroupedRules));
            Assert.That(firstProxy.GroupedRules[0], Is.Not.SameAs(secondProxy.GroupedRules[0]));
            Assert.That(firstWatchdog, Is.Not.SameAs(secondWatchdog));
            Assert.That(firstWatchdog.Rules, Is.Not.SameAs(secondWatchdog.Rules));
            Assert.That(firstWatchdog.Rules[0], Is.Not.SameAs(secondWatchdog.Rules[0]));
        });

        first.Revision = -1L;
        first.Modules.Clear();
        first.Vendors[0].Name = "changed-vendor";
        first.Applications[0].Name = "changed-application";
        first.Types[0].Name = "changed-type";
        first.EnumValues[0].Name = "changed-enum";
        firstProxy.Name = "changed-avp";
        firstProxy.GroupedRules[0].AvpName = "changed-grouped-rule";
        firstWatchdog.Name = "changed-command";
        firstWatchdog.Rules[0].AvpName = "changed-command-rule";
        DiameterDictionarySnapshot third = dictionary.ReadDictionarySnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(third.Revision, Is.EqualTo(9L));
            Assert.That(third.Modules, Has.Count.EqualTo(9));
            Assert.That(third.Vendors[0].Name, Is.Not.EqualTo("changed-vendor"));
            Assert.That(third.Applications[0].Name, Is.Not.EqualTo("changed-application"));
            Assert.That(third.Types[0].Name, Is.Not.EqualTo("changed-type"));
            Assert.That(third.EnumValues[0].Name, Is.Not.EqualTo("changed-enum"));
            Assert.That(FindSnapshotAvp(third, DiameterAvpCodes.ProxyInfo).Name,
                Is.EqualTo("Proxy-Info"));
            Assert.That(FindSnapshotAvp(third, DiameterAvpCodes.ProxyInfo)
                .GroupedRules[0].AvpName, Is.Not.EqualTo("changed-grouped-rule"));
            Assert.That(FindSnapshotCommand(
                third,
                DiameterCommandCodes.DeviceWatchdog,
                true).Name, Is.EqualTo("Device-Watchdog"));
            Assert.That(FindSnapshotCommand(
                third,
                DiameterCommandCodes.DeviceWatchdog,
                true).Rules[0].AvpName, Is.Not.EqualTo("changed-command-rule"));
        });
    }

    [Test]
    public void PublicRegistrationComposesDependenciesAndMergesEquivalentDefinitions()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionaryModule zBase = CreateEmptyModule("z-public-base");
        DiameterDictionaryModule extension = CreateEmptyModule("b-public-extension");
        extension.Dependencies.Add("z-public-base");
        DiameterDictionaryModule independent = CreateEmptyModule("a-public-independent");
        DiameterDictionaryModule firstShared = CreateEmptyModule("shared-definition-one");
        firstShared.Vendors.Add(CreateVendor(424243U, "Shared Vendor"));
        DiameterDictionaryModule secondShared = CreateEmptyModule("shared-definition-two");
        secondShared.Vendors.Add(CreateVendor(424243U, " shared vendor "));

        Assert.That(dictionary.RegisterDictionaryModule(zBase).Status,
            Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
        Assert.That(dictionary.RegisterDictionaryModule(extension).Status,
            Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
        Assert.That(dictionary.RegisterDictionaryModule(independent).Status,
            Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
        Assert.That(dictionary.RegisterDictionaryModule(firstShared).Status,
            Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
        DiameterDictionaryRegistrationResult result =
            dictionary.RegisterDictionaryModule(secondShared);
        DiameterDictionarySnapshot snapshot = dictionary.ReadDictionarySnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
            Assert.That(result.Code, Is.EqualTo("module-registered"));
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(result.Snapshot.Revision, Is.EqualTo(14L));
            Assert.That(FindModuleIndex(snapshot, "a-public-independent"),
                Is.LessThan(FindModuleIndex(snapshot, "z-public-base")));
            Assert.That(FindModuleIndex(snapshot, "z-public-base"),
                Is.LessThan(FindModuleIndex(snapshot, "b-public-extension")));
            Assert.That(CountVendor(snapshot, 424243U), Is.EqualTo(1));
            Assert.That(dictionary.FindDictionaryVendorByName("shared vendor")!.Id,
                Is.EqualTo(424243U));
        });
    }

    [TestCase("vendor", "vendor-definition-conflict")]
    [TestCase("application", "application-definition-conflict")]
    [TestCase("type", "type-definition-conflict")]
    [TestCase("enum", "enum-value-definition-conflict")]
    [TestCase("avp", "avp-definition-conflict")]
    [TestCase("command", "command-definition-conflict")]
    public void EveryPublicDefinitionConflictRejectsTheWholeTransaction(
        string kind,
        string expectedCode)
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionaryModule conflicting = CreateConflictModule(kind);

        DiameterDictionaryRegistrationResult result =
            dictionary.RegisterDictionaryModule(conflicting);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Conflict));
            Assert.That(result.Code, Is.EqualTo(expectedCode));
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(result.Snapshot.Revision, Is.EqualTo(9L));
            Assert.That(result.Snapshot.Modules, Has.Count.EqualTo(9));
            Assert.That(dictionary.ReadDictionarySnapshot().Revision, Is.EqualTo(9L));
            Assert.That(dictionary.FindDictionaryModuleByName(conflicting.Name), Is.Null);
        });
    }

    [Test]
    public void EveryPublicInvalidRegistrationOutcomeKeepsTheActiveSnapshot()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionaryModule blank = CreateEmptyModule(" ");
        DiameterDictionaryModule missingDependency = CreateEmptyModule("dependent");
        missingDependency.Dependencies.Add("not-registered");
        DiameterDictionaryModule selfDependency = CreateEmptyModule("self-dependent");
        selfDependency.Dependencies.Add("self-dependent");
        DiameterDictionaryModule missingCollections = CreateEmptyModule("missing-collections");
        missingCollections.Commands = null!;
        DiameterDictionaryModule missingEntry = CreateEmptyModule("missing-entry");
        missingEntry.Avps.Add(null!);

        DiameterDictionaryRegistrationResult missing =
            dictionary.RegisterDictionaryModule(null);
        DiameterDictionaryRegistrationResult missingName =
            dictionary.RegisterDictionaryModule(blank);
        DiameterDictionaryRegistrationResult dependency =
            dictionary.RegisterDictionaryModule(missingDependency);
        DiameterDictionaryRegistrationResult self =
            dictionary.RegisterDictionaryModule(selfDependency);
        DiameterDictionaryRegistrationResult collections =
            dictionary.RegisterDictionaryModule(missingCollections);
        DiameterDictionaryRegistrationResult entry =
            dictionary.RegisterDictionaryModule(missingEntry);

        Assert.Multiple(() =>
        {
            AssertInvalidRegistration(missing, "missing-dictionary-module");
            AssertInvalidRegistration(missingName, "missing-module-name");
            AssertInvalidRegistration(dependency, "missing-module-dependency");
            AssertInvalidRegistration(self, "invalid-module-definition");
            AssertInvalidRegistration(collections, "missing-module-definitions");
            AssertInvalidRegistration(entry, "invalid-module-definition");
            Assert.That(dictionary.ReadDictionarySnapshot().Revision, Is.EqualTo(9L));
            Assert.That(dictionary.ReadDictionarySnapshot().Modules, Has.Count.EqualTo(9));
        });
    }

    [Test]
    public void PublicDuplicateOutcomeIsCanonicalAndDoesNotAdvanceRevision()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionaryModule first = CreateEmptyModule("duplicate-public-module");
        first.Version = "1";
        DiameterDictionaryModule duplicate = CreateEmptyModule(" DUPLICATE-PUBLIC-MODULE ");
        duplicate.Version = "1";
        DiameterDictionaryRegistrationResult success =
            dictionary.RegisterDictionaryModule(first);
        DiameterDictionaryRegistrationResult result =
            dictionary.RegisterDictionaryModule(duplicate);

        Assert.Multiple(() =>
        {
            Assert.That(success.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
            Assert.That(result.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Duplicate));
            Assert.That(result.Code, Is.EqualTo("duplicate-module"));
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(result.Snapshot.Revision, Is.EqualTo(10L));
            Assert.That(dictionary.ReadDictionarySnapshot().Revision, Is.EqualTo(10L));
        });
    }

    [Test]
    public void ConcurrentPublicRegistrationsProduceAtomicMonotonicSnapshots()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        ConcurrentBag<DiameterDictionaryRegistrationStatus> statuses =
            new ConcurrentBag<DiameterDictionaryRegistrationStatus>();

        Parallel.For(0, 12, index =>
        {
            statuses.Add(dictionary.RegisterDictionaryModule(
                CreateEmptyModule($"public-concurrent-{index:D2}")).Status);
        });

        DiameterDictionarySnapshot snapshot = dictionary.ReadDictionarySnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(statuses, Has.Count.EqualTo(12));
            Assert.That(statuses,
                Has.All.EqualTo(DiameterDictionaryRegistrationStatus.Success));
            Assert.That(snapshot.Revision, Is.EqualTo(21L));
            Assert.That(snapshot.Modules, Has.Count.EqualTo(21));
            Assert.That(FindModuleIndex(snapshot, "public-concurrent-00"),
                Is.LessThan(FindModuleIndex(snapshot, "public-concurrent-11")));
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
    public void EveryPublicJsonSchemaFailureRetainsItsExactDocumentPath(
        string json,
        string expectedPath)
    {
        IDiameterDictionaryLoader loader = new DiameterJsonDictionaryLoader();

        DiameterDictionaryException? exception = Assert.Throws<DiameterDictionaryException>(
            () => loader.LoadDictionaryModule(json));

        Assert.That(exception!.Message, Does.Contain("at " + expectedPath + ":"));
    }

    [Test]
    public void PublicJsonLoaderCoversDefaultsBoundariesMissingInputAndMalformedInput()
    {
        IDiameterDictionaryLoader loader = new DiameterJsonDictionaryLoader();
        DiameterDictionaryModule defaults = loader.LoadDictionaryModule(
            "{\"schemaVersion\":1,\"name\":\"defaults\"," +
            "\"applications\":[{\"id\":1,\"name\":\"app\"}]," +
            "\"types\":[{\"name\":\"type\",\"baseType\":\"OctetString\"}]," +
            "\"avps\":[{\"code\":1,\"name\":\"avp\",\"typeName\":\"type\"," +
            "\"baseType\":\"OctetString\"}]," +
            "\"commands\":[{\"code\":1,\"applicationId\":1,\"name\":\"command\"," +
            "\"isRequest\":false}]}");
        DiameterDictionaryModule boundaries = loader.LoadDictionaryModule(
            "{\"schemaVersion\":1,\"name\":\"boundaries\",\"enumValues\":[" +
            "{\"name\":\"minimum\",\"value\":-2147483648,\"typeName\":\"type\"}," +
            "{\"name\":\"maximum\",\"value\":2147483647,\"typeName\":\"type\"}]}");
        JsonException? malformed = Assert.Catch<JsonException>(
            () => loader.LoadDictionaryModule("{\n\"schemaVersion\":1,\n\"name\":"));

        Assert.Multiple(() =>
        {
            Assert.That(defaults.Version, Is.Empty);
            Assert.That(defaults.Dependencies, Is.Empty);
            Assert.That(defaults.Vendors, Is.Empty);
            Assert.That(defaults.EnumValues, Is.Empty);
            Assert.That(defaults.Applications[0].VendorId, Is.Zero);
            Assert.That(defaults.Applications[0].IsAccounting, Is.False);
            Assert.That(defaults.Types[0].VendorId, Is.Zero);
            Assert.That(defaults.Types[0].ParentTypeName, Is.Empty);
            Assert.That(defaults.Types[0].ParentTypeVendorId, Is.Zero);
            Assert.That(defaults.Types[0].FixedDataLength, Is.EqualTo(-1));
            Assert.That(defaults.Avps[0].RequiredFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(defaults.Avps[0].ProhibitedFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(defaults.Avps[0].FixedDataLength, Is.EqualTo(-1));
            Assert.That(defaults.Avps[0].GroupedRules, Is.Empty);
            Assert.That(defaults.Avps[0].AllowUnlistedAvps, Is.True);
            Assert.That(defaults.Commands[0].RequiredFlags,
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(defaults.Commands[0].ProhibitedFlags,
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(defaults.Commands[0].Rules, Is.Empty);
            Assert.That(defaults.Commands[0].AllowUnlistedAvps, Is.True);
            Assert.That(boundaries.EnumValues[0].Value, Is.EqualTo(int.MinValue));
            Assert.That(boundaries.EnumValues[1].Value, Is.EqualTo(int.MaxValue));
            Assert.That(() => loader.LoadDictionaryModule(null),
                Throws.TypeOf<DiameterDictionaryException>());
            Assert.That(() => loader.LoadDictionaryModule("{\"name\":\"missing-schema\"}"),
                Throws.TypeOf<DiameterDictionaryException>());
            Assert.That(() => loader.LoadDictionaryModule("{\"schemaVersion\":1}"),
                Throws.TypeOf<DiameterDictionaryException>());
            Assert.That(malformed!.LineNumber, Is.GreaterThan(0L));
            Assert.That(malformed.BytePositionInLine, Is.Not.Null);
        });
    }

    private static void AssertRule(
        DiameterAvpRuleDefinition rule,
        uint code,
        uint vendorId,
        string name,
        int minimum,
        int maximum,
        int fixedHead,
        int ordering)
    {
        Assert.Multiple(() =>
        {
            Assert.That(rule.AvpCode, Is.EqualTo(code));
            Assert.That(rule.AvpVendorId, Is.EqualTo(vendorId));
            Assert.That(rule.AvpName, Is.EqualTo(name));
            Assert.That(rule.MinimumCount, Is.EqualTo(minimum));
            Assert.That(rule.MaximumCount, Is.EqualTo(maximum));
            Assert.That(rule.FixedHeadPosition, Is.EqualTo(fixedHead));
            Assert.That(rule.OrderingGroup, Is.EqualTo(ordering));
        });
    }

    private static void AssertInvalidRegistration(
        DiameterDictionaryRegistrationResult result,
        string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Invalid));
            Assert.That(result.Code, Is.EqualTo(code));
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(result.Snapshot.Revision, Is.EqualTo(9L));
            Assert.That(result.Snapshot.Modules, Has.Count.EqualTo(9));
        });
    }

    private static DiameterDictionaryModule CreateConflictModule(string kind)
    {
        DiameterDictionaryModule module = CreateEmptyModule("public-conflict-" + kind);
        if (kind == "vendor")
        {
            module.Vendors.Add(CreateVendor(0U, "Different Vendor"));
        }
        else if (kind == "application")
        {
            DiameterApplicationDefinition application = new DiameterApplicationDefinition();
            application.Id = DiameterApplicationIds.CreditControl;
            application.VendorId = DiameterVendorIds.Ietf;
            application.Name = "Different Application";
            module.Applications.Add(application);
        }
        else if (kind == "type")
        {
            DiameterTypeDefinition type = new DiameterTypeDefinition();
            type.Name = "Unsigned32";
            type.VendorId = DiameterVendorIds.Ietf;
            type.BaseType = DiameterAvpBaseType.Unsigned32;
            type.FixedDataLength = 8;
            module.Types.Add(type);
        }
        else if (kind == "enum")
        {
            DiameterEnumValueDefinition enumValue = new DiameterEnumValueDefinition();
            enumValue.Name = "DIFFERENT";
            enumValue.Value = 0;
            enumValue.TypeName = "Disconnect-Cause";
            enumValue.TypeVendorId = DiameterVendorIds.Ietf;
            enumValue.AvpCode = DiameterAvpCodes.DisconnectCause;
            enumValue.AvpVendorId = DiameterVendorIds.Ietf;
            module.EnumValues.Add(enumValue);
        }
        else if (kind == "avp")
        {
            DiameterAvpDefinition avp = new DiameterAvpDefinition();
            avp.Code = DiameterAvpCodes.OriginHost;
            avp.Name = "Different-AVP";
            avp.TypeName = "DiameterIdentity";
            avp.BaseType = DiameterAvpBaseType.OctetString;
            module.Avps.Add(avp);
        }
        else
        {
            DiameterCommandDefinition command = new DiameterCommandDefinition();
            command.Code = DiameterCommandCodes.DeviceWatchdog;
            command.ApplicationId = DiameterApplicationIds.Common;
            command.Name = "Different-Command";
            command.IsRequest = true;
            module.Commands.Add(command);
        }

        return module;
    }

    private static DiameterDictionaryModule CreateEmptyModule(string name)
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = name;
        module.Version = "1";
        return module;
    }

    private static DiameterVendorDefinition CreateVendor(uint id, string name)
    {
        DiameterVendorDefinition vendor = new DiameterVendorDefinition();
        vendor.Id = id;
        vendor.Name = name;
        return vendor;
    }

    private static DiameterAvpDefinition FindSnapshotAvp(
        DiameterDictionarySnapshot snapshot,
        uint code)
    {
        int index = 0;
        while (index < snapshot.Avps.Count)
        {
            if (snapshot.Avps[index].Code == code && snapshot.Avps[index].VendorId == 0U)
            {
                return snapshot.Avps[index];
            }

            index = index + 1;
        }

        Assert.Fail("Expected AVP " + code + ".");
        return new DiameterAvpDefinition();
    }

    private static DiameterCommandDefinition FindSnapshotCommand(
        DiameterDictionarySnapshot snapshot,
        uint code,
        bool request)
    {
        int index = 0;
        while (index < snapshot.Commands.Count)
        {
            DiameterCommandDefinition command = snapshot.Commands[index];
            if (command.Code == code && command.ApplicationId == 0U &&
                command.IsRequest == request)
            {
                return command;
            }

            index = index + 1;
        }

        Assert.Fail("Expected command " + code + ".");
        return new DiameterCommandDefinition();
    }

    private static int FindModuleIndex(DiameterDictionarySnapshot snapshot, string name)
    {
        int index = 0;
        while (index < snapshot.Modules.Count)
        {
            if (string.Equals(
                snapshot.Modules[index].Name,
                name,
                StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    private static int CountVendor(DiameterDictionarySnapshot snapshot, uint id)
    {
        int count = 0;
        int index = 0;
        while (index < snapshot.Vendors.Count)
        {
            if (snapshot.Vendors[index].Id == id)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
