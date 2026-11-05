namespace diameter.Tests;

public class Rfc6733DictionaryTests
{
    [Test]
    public void ModuleContainsRfcVendorApplicationsAndTypeHierarchy()
    {
        DiameterDictionaryModule module = Rfc6733Dictionary.CreateModule();
        DictionaryBuilder builder = new DictionaryBuilder();
        DictionaryRegistrationResult registration = builder.RegisterDictionaryModule(module);
        DictionaryIndexes indexes = builder.CurrentIndexes;

        Assert.Multiple(() =>
        {
            Assert.That(module.Name, Is.EqualTo("rfc6733"));
            Assert.That(module.Version, Is.EqualTo("RFC 6733"));
            Assert.That(module.Dependencies, Is.Empty);
            Assert.That(module.Vendors, Has.Count.EqualTo(1));
            Assert.That(module.Applications, Has.Count.EqualTo(3));
            Assert.That(module.Types, Has.Count.EqualTo(24));
            Assert.That(module.EnumValues, Has.Count.EqualTo(38));
            Assert.That(module.Avps, Has.Count.EqualTo(49));
            Assert.That(module.Commands, Has.Count.EqualTo(14));
            Assert.That(registration.Status, Is.EqualTo(DictionaryRegistrationStatus.Success));
            Assert.That(indexes.FindVendorById(DiameterVendorIds.Ietf)!.Name, Is.EqualTo("IETF"));
            Assert.That(
                indexes.FindApplicationById(0U, DiameterApplicationIds.Common)!.IsAccounting,
                Is.False);
            Assert.That(
                indexes.FindApplicationById(0U, DiameterApplicationIds.BaseAccounting)!
                    .IsAccounting,
                Is.True);
            Assert.That(
                indexes.FindApplicationById(0U, DiameterApplicationIds.Relay)!.Name,
                Is.EqualTo("Relay"));
        });

        DiameterTypeDefinition address = indexes.FindTypeByName(0U, "Address")!;
        DiameterTypeDefinition time = indexes.FindTypeByName(0U, "Time")!;
        DiameterTypeDefinition enumerated = indexes.FindTypeByName(0U, "Enumerated")!;
        DiameterTypeDefinition disconnect = indexes.FindTypeByName(0U, "Disconnect-Cause")!;
        Assert.Multiple(() =>
        {
            Assert.That(address.ParentTypeName, Is.EqualTo("OctetString"));
            Assert.That(address.FixedDataLength, Is.EqualTo(-1));
            Assert.That(time.ParentTypeName, Is.EqualTo("OctetString"));
            Assert.That(time.FixedDataLength, Is.EqualTo(4));
            Assert.That(enumerated.ParentTypeName, Is.EqualTo("Integer32"));
            Assert.That(enumerated.BaseType, Is.EqualTo(DiameterAvpBaseType.Integer32));
            Assert.That(disconnect.ParentTypeName, Is.EqualTo("Enumerated"));
            Assert.That(disconnect.FixedDataLength, Is.EqualTo(4));
        });
    }

    [Test]
    public void EveryEnumValueResolvesByAvpAndTypedValuesResolveByBothDomains()
    {
        DictionaryBuilder builder = CreateRegisteredBuilder();
        DiameterDictionarySnapshot snapshot = builder.ReadDictionarySnapshot();
        DictionaryIndexes indexes = builder.CurrentIndexes;

        int index = 0;
        while (index < snapshot.EnumValues.Count)
        {
            DiameterEnumValueDefinition value = snapshot.EnumValues[index];
            Assert.That(
                indexes.FindEnumValueByAvpValue(value.AvpVendorId, value.AvpCode, value.Value),
                Is.Not.Null,
                value.Name);
            Assert.That(
                indexes.FindEnumValueByAvpName(value.AvpVendorId, value.AvpCode, value.Name),
                Is.Not.Null,
                value.Name);
            if (value.TypeName.Length > 0)
            {
                Assert.That(
                    indexes.FindEnumValueByTypeValue(
                        value.TypeVendorId,
                        value.TypeName,
                        value.Value),
                    Is.Not.Null,
                    value.Name);
                Assert.That(
                    indexes.FindEnumValueByTypeName(
                        value.TypeVendorId,
                        value.TypeName,
                        value.Name),
                    Is.Not.Null,
                    value.Name);
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, 273U, "DO_NOT_WANT_TO_TALK_TO_YOU")!
                    .Value,
                Is.EqualTo(2));
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, 261U, "ALL_USER")!.Value,
                Is.EqualTo(6));
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, 295U, "DIAMETER_SESSION_TIMEOUT")!
                    .Value,
                Is.EqualTo(8));
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, 299U, "TLS")!.Value,
                Is.EqualTo(1));
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, 480U, "STOP_RECORD")!.Value,
                Is.EqualTo(4));
        });
    }

    [Test]
    public void AvpDefinitionsCarryRfcTypesSizesAndFlagRules()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterAvpDefinition proxyState = indexes.FindAvpByCode(0U, 33U)!;
        DiameterAvpDefinition eventTimestamp = indexes.FindAvpByCode(0U, 55U)!;
        DiameterAvpDefinition originHost = indexes.FindAvpByCode(0U, 264U)!;
        DiameterAvpDefinition firmware = indexes.FindAvpByCode(0U, 267U)!;
        DiameterAvpDefinition accountingSubSession = indexes.FindAvpByCode(0U, 287U)!;

        Assert.Multiple(() =>
        {
            Assert.That(DiameterAvpCodes.ProxyState, Is.EqualTo(33U));
            Assert.That(proxyState.Name, Is.EqualTo("Proxy-State"));
            Assert.That(proxyState.TypeName, Is.EqualTo("OctetString"));
            Assert.That(proxyState.RequiredFlags, Is.EqualTo(DiameterAvpFlags.Mandatory));
            Assert.That(
                proxyState.ProhibitedFlags,
                Is.EqualTo(DiameterAvpFlags.VendorSpecific));
            Assert.That(eventTimestamp.TypeName, Is.EqualTo("Time"));
            Assert.That(eventTimestamp.FixedDataLength, Is.EqualTo(4));
            Assert.That(originHost.TypeName, Is.EqualTo("DiameterIdentity"));
            Assert.That(originHost.FixedDataLength, Is.EqualTo(-1));
            Assert.That(firmware.RequiredFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(
                firmware.ProhibitedFlags,
                Is.EqualTo(DiameterAvpFlags.Mandatory | DiameterAvpFlags.VendorSpecific));
            Assert.That(accountingSubSession.BaseType, Is.EqualTo(DiameterAvpBaseType.Unsigned64));
            Assert.That(accountingSubSession.FixedDataLength, Is.EqualTo(8));
        });
    }

    [Test]
    public void GroupedAvpsContainTheirRfcOccurrenceRules()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterAvpDefinition vendorApplication = indexes.FindAvpByCode(0U, 260U)!;
        DiameterAvpDefinition failed = indexes.FindAvpByCode(0U, 279U)!;
        DiameterAvpDefinition proxyInfo = indexes.FindAvpByCode(0U, 284U)!;
        DiameterAvpDefinition experimental = indexes.FindAvpByCode(0U, 297U)!;

        Assert.Multiple(() =>
        {
            Assert.That(vendorApplication.GroupedRules, Has.Count.EqualTo(3));
            Assert.That(vendorApplication.AllowUnlistedAvps, Is.False);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 260U, 0U, 266U), 1, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 260U, 0U, 258U), 0, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 260U, 0U, 259U), 0, 1);
            Assert.That(failed.AllowUnlistedAvps, Is.True);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 279U, 0U, 0U), 1, -1);
            Assert.That(proxyInfo.GroupedRules, Has.Count.EqualTo(2));
            AssertRule(indexes.FindGroupedRuleByCode(0U, 284U, 0U, 280U), 1, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 284U, 0U, 33U), 1, 1);
            Assert.That(experimental.AllowUnlistedAvps, Is.False);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 297U, 0U, 266U), 1, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 297U, 0U, 298U), 1, 1);
        });
    }

    [TestCase(0U, 257U, true, "Capabilities-Exchange", false)]
    [TestCase(0U, 257U, false, "Capabilities-Exchange", false)]
    [TestCase(0U, 282U, true, "Disconnect-Peer", false)]
    [TestCase(0U, 282U, false, "Disconnect-Peer", false)]
    [TestCase(0U, 280U, true, "Device-Watchdog", false)]
    [TestCase(0U, 280U, false, "Device-Watchdog", false)]
    [TestCase(0U, 258U, true, "Re-Auth", true)]
    [TestCase(0U, 258U, false, "Re-Auth", true)]
    [TestCase(0U, 275U, true, "Session-Termination", true)]
    [TestCase(0U, 275U, false, "Session-Termination", true)]
    [TestCase(0U, 274U, true, "Abort-Session", true)]
    [TestCase(0U, 274U, false, "Abort-Session", true)]
    [TestCase(3U, 271U, true, "Accounting", true)]
    [TestCase(3U, 271U, false, "Accounting", true)]
    public void EveryBaseCommandDirectionIsIndexedWithRequiredHeaderFlags(
        uint applicationId,
        uint commandCode,
        bool request,
        string name,
        bool proxyable)
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;

        DiameterCommandDefinition command = indexes.FindCommandByCode(
            applicationId,
            commandCode,
            request)!;

        Assert.Multiple(() =>
        {
            Assert.That(command.Name, Is.EqualTo(name));
            Assert.That(
                indexes.FindCommandByName(applicationId, name, request),
                Is.SameAs(command));
            Assert.That(
                (command.RequiredFlags & DiameterMessageFlags.Proxyable) != 0,
                Is.EqualTo(proxyable));
            Assert.That(
                (command.RequiredFlags & DiameterMessageFlags.Request) != 0,
                Is.EqualTo(request));
            Assert.That(
                (command.ProhibitedFlags & DiameterMessageFlags.Error) != 0,
                Is.True);
            Assert.That(
                (command.ProhibitedFlags & DiameterMessageFlags.PotentiallyRetransmitted) != 0,
                Is.EqualTo(!request || !proxyable));
        });
    }

    [Test]
    public void RepresentativeCommandGrammarsMatchRequiredOptionalAndFixedHeadRules()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterCommandDefinition cer = indexes.FindCommandByCode(0U, 257U, true)!;
        DiameterCommandDefinition cea = indexes.FindCommandByCode(0U, 257U, false)!;
        DiameterCommandDefinition rar = indexes.FindCommandByCode(0U, 258U, true)!;
        DiameterCommandDefinition acr = indexes.FindCommandByCode(3U, 271U, true)!;
        DiameterCommandDefinition aca = indexes.FindCommandByCode(3U, 271U, false)!;

        Assert.Multiple(() =>
        {
            Assert.That(cer.Rules, Has.Count.EqualTo(12));
            Assert.That(cea.Rules, Has.Count.EqualTo(15));
            AssertRule(
                indexes.FindCommandRuleByCode(0U, 257U, true, 0U, 257U),
                1,
                -1);
            AssertRule(
                indexes.FindCommandRuleByCode(0U, 257U, false, 0U, 268U),
                1,
                1);
            Assert.That(rar.Rules, Has.Count.EqualTo(11));
            Assert.That(
                indexes.FindCommandRuleByCode(0U, 258U, true, 0U, 263U)!
                    .FixedHeadPosition,
                Is.EqualTo(0));
            AssertRule(
                indexes.FindCommandRuleByCode(0U, 258U, true, 0U, 293U),
                1,
                1);
            Assert.That(acr.Rules, Has.Count.EqualTo(19));
            Assert.That(aca.Rules, Has.Count.EqualTo(20));
            Assert.That(
                indexes.FindCommandRuleByCode(3U, 271U, true, 0U, 263U)!
                    .FixedHeadPosition,
                Is.EqualTo(0));
            AssertRule(
                indexes.FindCommandRuleByCode(3U, 271U, true, 0U, 480U),
                1,
                1);
            AssertRule(
                indexes.FindCommandRuleByCode(3U, 271U, false, 0U, 268U),
                1,
                1);
        });
    }

    [Test]
    public void EveryRuleReferencesAnRfcAvpExceptFailedAvpWildcard()
    {
        DictionaryBuilder builder = CreateRegisteredBuilder();
        DiameterDictionarySnapshot snapshot = builder.ReadDictionarySnapshot();
        DictionaryIndexes indexes = builder.CurrentIndexes;

        int avpIndex = 0;
        while (avpIndex < snapshot.Avps.Count)
        {
            DiameterAvpDefinition avp = snapshot.Avps[avpIndex];
            int ruleIndex = 0;
            while (ruleIndex < avp.GroupedRules.Count)
            {
                DiameterAvpRuleDefinition rule = avp.GroupedRules[ruleIndex];
                if (rule.AvpCode != 0U)
                {
                    Assert.That(
                        indexes.FindAvpByCode(rule.AvpVendorId, rule.AvpCode),
                        Is.Not.Null,
                        avp.Name + "/" + rule.AvpName);
                }

                ruleIndex = ruleIndex + 1;
            }

            avpIndex = avpIndex + 1;
        }

        int commandIndex = 0;
        while (commandIndex < snapshot.Commands.Count)
        {
            DiameterCommandDefinition command = snapshot.Commands[commandIndex];
            int ruleIndex = 0;
            while (ruleIndex < command.Rules.Count)
            {
                DiameterAvpRuleDefinition rule = command.Rules[ruleIndex];
                Assert.That(
                    indexes.FindAvpByCode(rule.AvpVendorId, rule.AvpCode),
                    Is.Not.Null,
                    command.Name + "/" + rule.AvpName);
                ruleIndex = ruleIndex + 1;
            }

            commandIndex = commandIndex + 1;
        }
    }

    [Test]
    public void RecreatedBuiltInModuleIsAcceptedAsAnEquivalentDuplicate()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        DictionaryRegistrationResult first = builder.RegisterDictionaryModule(
            Rfc6733Dictionary.CreateModule());
        DictionaryRegistrationResult duplicate = builder.RegisterDictionaryModule(
            Rfc6733Dictionary.CreateModule());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DictionaryRegistrationStatus.Success));
            Assert.That(duplicate.Status, Is.EqualTo(DictionaryRegistrationStatus.Duplicate));
            Assert.That(duplicate.Snapshot.Revision, Is.EqualTo(1L));
            Assert.That(duplicate.Snapshot.Modules, Has.Count.EqualTo(1));
        });
    }

    private static DictionaryBuilder CreateRegisteredBuilder()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        DictionaryRegistrationResult result = builder.RegisterDictionaryModule(
            Rfc6733Dictionary.CreateModule());
        Assert.That(result.Status, Is.EqualTo(DictionaryRegistrationStatus.Success));
        return builder;
    }

    private static void AssertRule(
        DiameterAvpRuleDefinition? rule,
        int minimum,
        int maximum)
    {
        Assert.That(rule, Is.Not.Null);
        Assert.That(rule!.MinimumCount, Is.EqualTo(minimum));
        Assert.That(rule.MaximumCount, Is.EqualTo(maximum));
    }
}
