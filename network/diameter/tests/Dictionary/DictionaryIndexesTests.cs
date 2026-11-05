using System.Collections.Generic;

namespace diameter.Tests;

public class DictionaryIndexesTests
{
    [Test]
    public void CanonicalAndNumericLookupsResolveEveryTopLevelDefinitionKind()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = " RFC-6733 ";
        DiameterVendorDefinition ietf = CreateVendor(0U, "IETF");
        DiameterVendorDefinition vendor = CreateVendor(10415U, "3GPP");
        DiameterApplicationDefinition ietfApplication = CreateApplication(
            4U,
            0U,
            "Credit-Control");
        DiameterApplicationDefinition vendorApplication = CreateApplication(
            4U,
            10415U,
            "Credit-Control");
        DiameterTypeDefinition ietfType = CreateType(0U, "Disconnect-Cause");
        DiameterTypeDefinition vendorType = CreateType(10415U, "Disconnect-Cause");
        DiameterAvpDefinition ietfAvp = CreateAvp(263U, 0U, "Session-Id");
        DiameterAvpDefinition vendorAvp = CreateAvp(263U, 10415U, "Session-Id");
        DiameterCommandDefinition request = CreateCommand(
            4U,
            272U,
            true,
            "Credit-Control");
        DiameterCommandDefinition answer = CreateCommand(
            4U,
            272U,
            false,
            "Credit-Control");
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.Modules.Add(module);
        snapshot.Vendors.Add(ietf);
        snapshot.Vendors.Add(vendor);
        snapshot.Applications.Add(ietfApplication);
        snapshot.Applications.Add(vendorApplication);
        snapshot.Types.Add(ietfType);
        snapshot.Types.Add(vendorType);
        snapshot.Avps.Add(ietfAvp);
        snapshot.Avps.Add(vendorAvp);
        snapshot.Commands.Add(request);
        snapshot.Commands.Add(answer);

        DictionaryIndexes indexes = new DictionaryIndexes(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(indexes.FindModuleByName("rfc-6733"), Is.SameAs(module));
            Assert.That(indexes.FindModuleByName("  RFC-6733  "), Is.SameAs(module));
            Assert.That(indexes.FindVendorById(0U), Is.SameAs(ietf));
            Assert.That(indexes.FindVendorById(10415U), Is.SameAs(vendor));
            Assert.That(indexes.FindVendorByName(" ietf "), Is.SameAs(ietf));
            Assert.That(indexes.FindVendorByName("3gpp"), Is.SameAs(vendor));
            Assert.That(indexes.FindApplicationById(0U, 4U), Is.SameAs(ietfApplication));
            Assert.That(
                indexes.FindApplicationById(10415U, 4U),
                Is.SameAs(vendorApplication));
            Assert.That(
                indexes.FindApplicationByName(0U, " credit-CONTROL "),
                Is.SameAs(ietfApplication));
            Assert.That(
                indexes.FindApplicationByName(10415U, "CREDIT-control"),
                Is.SameAs(vendorApplication));
            Assert.That(indexes.FindTypeByName(0U, "disconnect-cause"), Is.SameAs(ietfType));
            Assert.That(
                indexes.FindTypeByName(10415U, " DISCONNECT-CAUSE "),
                Is.SameAs(vendorType));
            Assert.That(indexes.FindAvpByCode(0U, 263U), Is.SameAs(ietfAvp));
            Assert.That(indexes.FindAvpByCode(10415U, 263U), Is.SameAs(vendorAvp));
            Assert.That(indexes.FindAvpByName(0U, " session-ID "), Is.SameAs(ietfAvp));
            Assert.That(
                indexes.FindAvpByName(10415U, "SESSION-id"),
                Is.SameAs(vendorAvp));
            Assert.That(indexes.FindCommandByCode(4U, 272U, true), Is.SameAs(request));
            Assert.That(indexes.FindCommandByCode(4U, 272U, false), Is.SameAs(answer));
            Assert.That(
                indexes.FindCommandByName(4U, " credit-control ", true),
                Is.SameAs(request));
            Assert.That(
                indexes.FindCommandByName(4U, "CREDIT-CONTROL", false),
                Is.SameAs(answer));
        });
    }

    [Test]
    public void EnumValuesResolveIndependentlyByTypeAndAvpDomains()
    {
        DiameterEnumValueDefinition ietfValue = CreateEnumValue(
            "REBOOTING",
            0,
            "Disconnect-Cause",
            0U,
            273U,
            0U);
        DiameterEnumValueDefinition vendorValue = CreateEnumValue(
            "REBOOTING",
            0,
            "Disconnect-Cause",
            10415U,
            273U,
            10415U);
        DiameterEnumValueDefinition negative = CreateEnumValue(
            "NEGATIVE",
            int.MinValue,
            "Signed-Domain",
            0U,
            999U,
            0U);
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.EnumValues.Add(ietfValue);
        snapshot.EnumValues.Add(vendorValue);
        snapshot.EnumValues.Add(negative);
        DictionaryIndexes indexes = new DictionaryIndexes(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(
                indexes.FindEnumValueByTypeValue(0U, "disconnect-cause", 0),
                Is.SameAs(ietfValue));
            Assert.That(
                indexes.FindEnumValueByTypeName(0U, " DISCONNECT-CAUSE ", " rebooting "),
                Is.SameAs(ietfValue));
            Assert.That(
                indexes.FindEnumValueByTypeValue(10415U, "Disconnect-Cause", 0),
                Is.SameAs(vendorValue));
            Assert.That(
                indexes.FindEnumValueByAvpValue(0U, 273U, 0),
                Is.SameAs(ietfValue));
            Assert.That(
                indexes.FindEnumValueByAvpName(10415U, 273U, "REBOOTING"),
                Is.SameAs(vendorValue));
            Assert.That(
                indexes.FindEnumValueByTypeValue(0U, "signed-domain", int.MinValue),
                Is.SameAs(negative));
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, 999U, " negative "),
                Is.SameAs(negative));
        });
    }

    [Test]
    public void GroupedAndCommandRulesResolveWithinTheirParentScopes()
    {
        DiameterAvpRuleDefinition groupedRule = CreateRule(263U, 0U, "Session-Id");
        DiameterAvpRuleDefinition vendorGroupedRule = CreateRule(
            263U,
            10415U,
            "Session-Id");
        DiameterAvpDefinition ietfGroup = CreateAvp(284U, 0U, "Proxy-Info");
        ietfGroup.GroupedRules.Add(groupedRule);
        DiameterAvpDefinition vendorGroup = CreateAvp(284U, 10415U, "Proxy-Info");
        vendorGroup.GroupedRules.Add(vendorGroupedRule);

        DiameterAvpRuleDefinition requestRule = CreateRule(264U, 0U, "Origin-Host");
        DiameterAvpRuleDefinition answerRule = CreateRule(268U, 0U, "Result-Code");
        DiameterCommandDefinition request = CreateCommand(0U, 280U, true, "Device-Watchdog");
        request.Rules.Add(requestRule);
        DiameterCommandDefinition answer = CreateCommand(0U, 280U, false, "Device-Watchdog");
        answer.Rules.Add(answerRule);

        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.Avps.Add(ietfGroup);
        snapshot.Avps.Add(vendorGroup);
        snapshot.Commands.Add(request);
        snapshot.Commands.Add(answer);
        DictionaryIndexes indexes = new DictionaryIndexes(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(
                indexes.FindGroupedRuleByCode(0U, 284U, 0U, 263U),
                Is.SameAs(groupedRule));
            Assert.That(
                indexes.FindGroupedRuleByName(0U, 284U, " session-ID "),
                Is.SameAs(groupedRule));
            Assert.That(
                indexes.FindGroupedRuleByCode(10415U, 284U, 10415U, 263U),
                Is.SameAs(vendorGroupedRule));
            Assert.That(
                indexes.FindCommandRuleByCode(0U, 280U, true, 0U, 264U),
                Is.SameAs(requestRule));
            Assert.That(
                indexes.FindCommandRuleByName(0U, 280U, true, " ORIGIN-host "),
                Is.SameAs(requestRule));
            Assert.That(
                indexes.FindCommandRuleByCode(0U, 280U, false, 0U, 268U),
                Is.SameAs(answerRule));
            Assert.That(
                indexes.FindCommandRuleByName(0U, 280U, false, "result-code"),
                Is.SameAs(answerRule));
        });
    }

    [Test]
    public void MissingNamesScopesDirectionsAndNumericKeysReturnNull()
    {
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.Modules.Add(new DiameterDictionaryModule());
        snapshot.Vendors.Add(CreateVendor(0U, "IETF"));
        snapshot.Applications.Add(CreateApplication(4U, 0U, "Credit-Control"));
        snapshot.Types.Add(CreateType(0U, "Enumerated"));
        snapshot.EnumValues.Add(CreateEnumValue(
            "VALUE",
            1,
            "Enumerated",
            0U,
            100U,
            0U));
        snapshot.Avps.Add(CreateAvp(100U, 0U, "Example"));
        snapshot.Commands.Add(CreateCommand(4U, 272U, true, "Credit-Control"));
        DictionaryIndexes indexes = new DictionaryIndexes(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(indexes.FindModuleByName("missing"), Is.Null);
            Assert.That(indexes.FindModuleByName(null), Is.Null);
            Assert.That(indexes.FindVendorById(1U), Is.Null);
            Assert.That(indexes.FindVendorByName("missing"), Is.Null);
            Assert.That(indexes.FindApplicationById(10415U, 4U), Is.Null);
            Assert.That(indexes.FindApplicationByName(0U, "missing"), Is.Null);
            Assert.That(indexes.FindTypeByName(10415U, "Enumerated"), Is.Null);
            Assert.That(indexes.FindEnumValueByTypeValue(0U, "Enumerated", 2), Is.Null);
            Assert.That(
                indexes.FindEnumValueByTypeName(0U, "Enumerated", "missing"),
                Is.Null);
            Assert.That(indexes.FindEnumValueByAvpValue(0U, 101U, 1), Is.Null);
            Assert.That(indexes.FindEnumValueByAvpName(0U, 100U, "missing"), Is.Null);
            Assert.That(indexes.FindAvpByCode(10415U, 100U), Is.Null);
            Assert.That(indexes.FindAvpByName(0U, null), Is.Null);
            Assert.That(indexes.FindCommandByCode(4U, 272U, false), Is.Null);
            Assert.That(indexes.FindCommandByName(4U, "Credit-Control", false), Is.Null);
            Assert.That(indexes.FindGroupedRuleByCode(0U, 100U, 0U, 1U), Is.Null);
            Assert.That(indexes.FindGroupedRuleByName(0U, 100U, "missing"), Is.Null);
            Assert.That(indexes.FindCommandRuleByCode(4U, 272U, true, 0U, 1U), Is.Null);
            Assert.That(indexes.FindCommandRuleByName(4U, 272U, true, "missing"), Is.Null);
        });
    }

    [Test]
    public void NullSnapshotsCollectionsAndEntriesProduceEmptyIndexes()
    {
        DictionaryIndexes missing = new DictionaryIndexes(null);
        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.Modules = null!;
        snapshot.Vendors = null!;
        snapshot.Applications = null!;
        snapshot.Types = null!;
        snapshot.EnumValues = null!;
        snapshot.Avps = null!;
        snapshot.Commands = null!;
        DictionaryIndexes nullCollections = new DictionaryIndexes(snapshot);

        DiameterDictionarySnapshot entries = new DiameterDictionarySnapshot();
        entries.Modules.Add(null!);
        entries.Vendors.Add(null!);
        entries.Applications.Add(null!);
        entries.Types.Add(null!);
        entries.EnumValues.Add(null!);
        entries.Avps.Add(null!);
        entries.Commands.Add(null!);
        DictionaryIndexes nullEntries = new DictionaryIndexes(entries);

        Assert.Multiple(() =>
        {
            Assert.That(missing.FindVendorById(0U), Is.Null);
            Assert.That(nullCollections.FindVendorByName("ietf"), Is.Null);
            Assert.That(nullCollections.FindCommandByCode(0U, 0U, false), Is.Null);
            Assert.That(nullEntries.ModulesByName, Is.Empty);
            Assert.That(nullEntries.VendorsById, Is.Empty);
            Assert.That(nullEntries.ApplicationsById, Is.Empty);
            Assert.That(nullEntries.TypesByName, Is.Empty);
            Assert.That(nullEntries.EnumValuesByTypeValue, Is.Empty);
            Assert.That(nullEntries.AvpsByCode, Is.Empty);
            Assert.That(nullEntries.CommandsByCode, Is.Empty);
        });
    }

    private static DiameterVendorDefinition CreateVendor(uint id, string name)
    {
        DiameterVendorDefinition result = new DiameterVendorDefinition();
        result.Id = id;
        result.Name = name;
        return result;
    }

    private static DiameterApplicationDefinition CreateApplication(
        uint id,
        uint vendorId,
        string name)
    {
        DiameterApplicationDefinition result = new DiameterApplicationDefinition();
        result.Id = id;
        result.VendorId = vendorId;
        result.Name = name;
        return result;
    }

    private static DiameterTypeDefinition CreateType(uint vendorId, string name)
    {
        DiameterTypeDefinition result = new DiameterTypeDefinition();
        result.VendorId = vendorId;
        result.Name = name;
        return result;
    }

    private static DiameterEnumValueDefinition CreateEnumValue(
        string name,
        int value,
        string typeName,
        uint typeVendorId,
        uint avpCode,
        uint avpVendorId)
    {
        DiameterEnumValueDefinition result = new DiameterEnumValueDefinition();
        result.Name = name;
        result.Value = value;
        result.TypeName = typeName;
        result.TypeVendorId = typeVendorId;
        result.AvpCode = avpCode;
        result.AvpVendorId = avpVendorId;
        return result;
    }

    private static DiameterAvpDefinition CreateAvp(uint code, uint vendorId, string name)
    {
        DiameterAvpDefinition result = new DiameterAvpDefinition();
        result.Code = code;
        result.VendorId = vendorId;
        result.Name = name;
        return result;
    }

    private static DiameterCommandDefinition CreateCommand(
        uint applicationId,
        uint code,
        bool request,
        string name)
    {
        DiameterCommandDefinition result = new DiameterCommandDefinition();
        result.ApplicationId = applicationId;
        result.Code = code;
        result.IsRequest = request;
        result.Name = name;
        return result;
    }

    private static DiameterAvpRuleDefinition CreateRule(
        uint avpCode,
        uint vendorId,
        string name)
    {
        DiameterAvpRuleDefinition result = new DiameterAvpRuleDefinition();
        result.AvpCode = avpCode;
        result.AvpVendorId = vendorId;
        result.AvpName = name;
        return result;
    }
}
