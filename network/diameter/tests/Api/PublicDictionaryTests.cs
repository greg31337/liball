using System.Text.Json;

namespace diameter.Tests;

public class PublicDictionaryTests
{
    [Test]
    public void NameLookupsRejectRulesFromADifferentRequestedVendor()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();

        DiameterAvpRuleDefinition? grouped =
            dictionary.FindDictionaryGroupedRuleByName(
                DiameterVendorIds.Ietf,
                "proxy-info",
                999U,
                "proxy-state");
        DiameterAvpRuleDefinition? command =
            dictionary.FindDictionaryCommandRuleByName(
                DiameterApplicationIds.Common,
                "device-watchdog",
                true,
                999U,
                "origin-host");

        Assert.Multiple(() =>
        {
            Assert.That(grouped, Is.Null);
            Assert.That(command, Is.Null);
        });
    }

    [Test]
    public void BuiltInDictionaryExposesNumericAndCanonicalNameLookups()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionarySnapshot snapshot = dictionary.ReadDictionarySnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Revision, Is.EqualTo(9L));
            Assert.That(snapshot.Modules, Has.Count.EqualTo(9));
            Assert.That(dictionary.FindDictionaryModuleByName(" RFC6733 ")!.Version,
                Is.EqualTo("RFC 6733"));
            Assert.That(dictionary.FindDictionaryVendorById(0U)!.Name, Is.EqualTo("IETF"));
            Assert.That(dictionary.FindDictionaryVendorByName(" ietf ")!.Id, Is.Zero);
            Assert.That(dictionary.FindDictionaryApplicationById(0U, 4U)!.Name,
                Is.EqualTo("Diameter Credit Control"));
            Assert.That(dictionary.FindDictionaryApplicationByName(
                0U,
                " diameter credit control ")!.Id, Is.EqualTo(4U));
            Assert.That(dictionary.FindDictionaryTypeByName(0U, " diameteridentity ")!.Name,
                Is.EqualTo("DiameterIdentity"));
            Assert.That(dictionary.FindDictionaryEnumValueByTypeNumber(
                0U,
                "Disconnect-Cause",
                0)!.Name, Is.EqualTo("REBOOTING"));
            Assert.That(dictionary.FindDictionaryEnumValueByTypeName(
                0U,
                "Disconnect-Cause",
                " rebooting ")!.Value, Is.Zero);
            Assert.That(dictionary.FindDictionaryEnumValueByAvpNumber(
                0U,
                DiameterAvpCodes.DisconnectCause,
                0)!.Name, Is.EqualTo("REBOOTING"));
            Assert.That(dictionary.FindDictionaryEnumValueByAvpName(
                0U,
                DiameterAvpCodes.DisconnectCause,
                "REBOOTING")!.Value, Is.Zero);
            Assert.That(dictionary.FindDictionaryAvpByCode(
                0U,
                DiameterAvpCodes.OriginHost)!.Name, Is.EqualTo("Origin-Host"));
            Assert.That(dictionary.FindDictionaryAvpByName(0U, " origin-host ")!.Code,
                Is.EqualTo(DiameterAvpCodes.OriginHost));
            Assert.That(dictionary.FindDictionaryCommandByCode(
                0U,
                DiameterCommandCodes.DeviceWatchdog,
                true)!.Name, Is.EqualTo("Device-Watchdog"));
            Assert.That(dictionary.FindDictionaryCommandByName(
                0U,
                " device-watchdog ",
                false)!.IsRequest, Is.False);
            Assert.That(dictionary.FindDictionaryGroupedRuleByCode(
                0U,
                DiameterAvpCodes.ProxyInfo,
                0U,
                DiameterAvpCodes.ProxyState)!.MinimumCount, Is.EqualTo(1));
            Assert.That(dictionary.FindDictionaryGroupedRuleByName(
                0U,
                "proxy-info",
                0U,
                "proxy-state")!.MaximumCount, Is.EqualTo(1));
            Assert.That(dictionary.FindDictionaryCommandRuleByCode(
                0U,
                DiameterCommandCodes.DeviceWatchdog,
                true,
                0U,
                DiameterAvpCodes.OriginHost)!.MinimumCount, Is.EqualTo(1));
            Assert.That(dictionary.FindDictionaryCommandRuleByName(
                0U,
                "device-watchdog",
                true,
                0U,
                "origin-host")!.FixedHeadPosition, Is.EqualTo(-1));
        });
    }

    [Test]
    public void SnapshotsLookupsAndRegistrationInputAreDetachedFromDictionaryState()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionarySnapshot first = dictionary.ReadDictionarySnapshot();
        first.Modules.Clear();
        DiameterAvpDefinition found = dictionary.FindDictionaryAvpByCode(
            0U,
            DiameterAvpCodes.OriginHost)!;
        found.Name = "changed";
        DiameterDictionaryModule custom = CreateCustomModule();

        DiameterDictionaryRegistrationResult registration =
            dictionary.RegisterDictionaryModule(custom);
        custom.Vendors[0].Name = "changed";
        registration.Snapshot.Modules.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.ReadDictionarySnapshot().Modules, Has.Count.EqualTo(10));
            Assert.That(dictionary.FindDictionaryAvpByCode(
                0U,
                DiameterAvpCodes.OriginHost)!.Name, Is.EqualTo("Origin-Host"));
            Assert.That(dictionary.FindDictionaryVendorById(424242U)!.Name,
                Is.EqualTo("Example Vendor"));
            Assert.That(registration.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
            Assert.That(registration.Snapshot.Revision, Is.EqualTo(10L));
        });
    }

    [Test]
    public void RegistrationReportsDuplicateInvalidAndConflictWithoutChangingRevision()
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionaryModule custom = CreateCustomModule();
        DiameterDictionaryRegistrationResult success =
            dictionary.RegisterDictionaryModule(custom);
        DiameterDictionaryRegistrationResult duplicate =
            dictionary.RegisterDictionaryModule(CreateCustomModule());
        DiameterDictionaryRegistrationResult invalid =
            dictionary.RegisterDictionaryModule(null);
        DiameterDictionaryModule conflictModule = CreateCustomModule();
        conflictModule.Vendors[0].Name = "Different Vendor";
        DiameterDictionaryRegistrationResult conflict =
            dictionary.RegisterDictionaryModule(conflictModule);

        Assert.Multiple(() =>
        {
            Assert.That(success.Status, Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
            Assert.That(duplicate.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Duplicate));
            Assert.That(invalid.Status, Is.EqualTo(DiameterDictionaryRegistrationStatus.Invalid));
            Assert.That(conflict.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Conflict));
            Assert.That(dictionary.ReadDictionarySnapshot().Revision, Is.EqualTo(10L));
        });
    }

    [Test]
    public void PublicJsonLoaderLoadsVersionOneAndPreservesMalformedJsonDetails()
    {
        IDiameterDictionaryLoader loader = new DiameterJsonDictionaryLoader();
        DiameterDictionaryModule module = loader.LoadDictionaryModule(
            "{\"schemaVersion\":1,\"name\":\"public-json\"}");

        Assert.Multiple(() =>
        {
            Assert.That(module.Name, Is.EqualTo("public-json"));
            Assert.That(() => loader.LoadDictionaryModule(null),
                Throws.TypeOf<DiameterDictionaryException>());
            Assert.That(() => loader.LoadDictionaryModule("{"),
                Throws.InstanceOf<JsonException>());
        });
    }

    private static DiameterDictionaryModule CreateCustomModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "example-module";
        module.Version = "1";
        DiameterVendorDefinition vendor = new DiameterVendorDefinition();
        vendor.Id = 424242U;
        vendor.Name = "Example Vendor";
        module.Vendors.Add(vendor);
        return module;
    }
}
