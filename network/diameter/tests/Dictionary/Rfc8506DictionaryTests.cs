namespace diameter.Tests;

public class Rfc8506DictionaryTests
{
    [Test]
    public void ModuleDeclaresDependencyApplicationTypesAndCompleteContentCounts()
    {
        DiameterDictionaryModule module = Rfc8506Dictionary.CreateModule();

        Assert.Multiple(() =>
        {
            Assert.That(module.Name, Is.EqualTo("rfc8506"));
            Assert.That(module.Version, Is.EqualTo("RFC 8506"));
            Assert.That(module.Dependencies, Is.EqualTo(new[] { "rfc6733" }));
            Assert.That(module.Vendors, Is.Empty);
            Assert.That(module.Applications, Has.Count.EqualTo(1));
            Assert.That(module.Applications[0].Id, Is.EqualTo(4U));
            Assert.That(module.Applications[0].Name, Is.EqualTo("Diameter Credit Control"));
            Assert.That(module.Types, Has.Count.EqualTo(15));
            Assert.That(module.EnumValues, Has.Count.EqualTo(51));
            Assert.That(module.Avps, Has.Count.EqualTo(70));
            Assert.That(module.Commands, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void ModuleRequiresAndComposesAfterRfc6733()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        DictionaryRegistrationResult missing = builder.RegisterDictionaryModule(
            Rfc8506Dictionary.CreateModule());
        DictionaryRegistrationResult baseResult = builder.RegisterDictionaryModule(
            Rfc6733Dictionary.CreateModule());
        DictionaryRegistrationResult creditResult = builder.RegisterDictionaryModule(
            Rfc8506Dictionary.CreateModule());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status, Is.EqualTo(DictionaryRegistrationStatus.Invalid));
            Assert.That(missing.Code, Is.EqualTo("missing-module-dependency"));
            Assert.That(baseResult.Status, Is.EqualTo(DictionaryRegistrationStatus.Success));
            Assert.That(creditResult.Status, Is.EqualTo(DictionaryRegistrationStatus.Success));
            Assert.That(creditResult.Snapshot.Revision, Is.EqualTo(2L));
            Assert.That(
                creditResult.Snapshot.Modules.ConvertAll(module => module.Name),
                Is.EqualTo(new[] { "rfc6733", "rfc8506" }));
            Assert.That(creditResult.Snapshot.Applications, Has.Count.EqualTo(4));
            Assert.That(creditResult.Snapshot.Types, Has.Count.EqualTo(39));
            Assert.That(creditResult.Snapshot.Avps, Has.Count.EqualTo(119));
            Assert.That(creditResult.Snapshot.Commands, Has.Count.EqualTo(18));
        });
    }

    [Test]
    public void EveryRfc8506AssignedAvpCodeIsDefinedAndIndexed()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;

        uint code = 411U;
        while (code <= 461U)
        {
            Assert.That(indexes.FindAvpByCode(0U, code), Is.Not.Null, "AVP code " + code);
            code = code + 1U;
        }

        code = 653U;
        while (code <= 669U)
        {
            Assert.That(indexes.FindAvpByCode(0U, code), Is.Not.Null, "AVP code " + code);
            code = code + 1U;
        }

        Assert.Multiple(() =>
        {
            Assert.That(indexes.FindAvpByCode(0U, DiameterAvpCodes.FilterId), Is.Not.Null);
            Assert.That(indexes.FindAvpByCode(0U, DiameterAvpCodes.FilterRule), Is.Not.Null);
        });
    }

    [Test]
    public void AvpWireTypesSizesAndMBitPoliciesMatchRfcTable()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterAvpDefinition correlation = indexes.FindAvpByCode(0U, 411U)!;
        DiameterAvpDefinition input = indexes.FindAvpByCode(0U, 412U)!;
        DiameterAvpDefinition requestType = indexes.FindAvpByCode(0U, 416U)!;
        DiameterAvpDefinition tariffTime = indexes.FindAvpByCode(0U, 451U)!;
        DiameterAvpDefinition serviceParameter = indexes.FindAvpByCode(0U, 440U)!;
        DiameterAvpDefinition extension = indexes.FindAvpByCode(0U, 653U)!;

        Assert.Multiple(() =>
        {
            Assert.That(correlation.BaseType, Is.EqualTo(DiameterAvpBaseType.OctetString));
            Assert.That(correlation.RequiredFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(correlation.ProhibitedFlags, Is.EqualTo(DiameterAvpFlags.VendorSpecific));
            Assert.That(input.BaseType, Is.EqualTo(DiameterAvpBaseType.Unsigned64));
            Assert.That(input.FixedDataLength, Is.EqualTo(8));
            Assert.That(input.RequiredFlags, Is.EqualTo(DiameterAvpFlags.Mandatory));
            Assert.That(requestType.TypeName, Is.EqualTo("CC-Request-Type"));
            Assert.That(requestType.FixedDataLength, Is.EqualTo(4));
            Assert.That(tariffTime.TypeName, Is.EqualTo("Time"));
            Assert.That(tariffTime.FixedDataLength, Is.EqualTo(4));
            Assert.That(serviceParameter.RequiredFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(extension.RequiredFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(extension.BaseType, Is.EqualTo(DiameterAvpBaseType.Grouped));
        });
    }

    [Test]
    public void EveryNamedValueResolvesAndCcUnitTypeUsesRfcWireNumbers()
    {
        DictionaryBuilder builder = CreateRegisteredBuilder();
        DictionaryIndexes indexes = builder.CurrentIndexes;
        DiameterDictionarySnapshot snapshot = builder.ReadDictionarySnapshot();
        DiameterDictionaryModule module = snapshot.Modules[1];

        int index = 0;
        while (index < module.EnumValues.Count)
        {
            DiameterEnumValueDefinition value = module.EnumValues[index];
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, value.AvpCode, value.Name),
                Is.Not.Null,
                value.Name);
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That((int)DiameterCreditControlUnitType.Time, Is.EqualTo(0));
            Assert.That((int)DiameterCreditControlUnitType.Money, Is.EqualTo(1));
            Assert.That((int)DiameterCreditControlUnitType.TotalOctets, Is.EqualTo(2));
            Assert.That((int)DiameterCreditControlUnitType.InputOctets, Is.EqualTo(3));
            Assert.That((int)DiameterCreditControlUnitType.OutputOctets, Is.EqualTo(4));
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, 454U, "TOTAL-OCTETS")!.Value,
                Is.EqualTo(2));
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, 416U, "EVENT_REQUEST")!.Value,
                Is.EqualTo(4));
            Assert.That(
                indexes.FindEnumValueByAvpName(0U, 450U, "END_USER_PRIVATE")!.Value,
                Is.EqualTo(4));
        });
    }

    [Test]
    public void AllFiveCreditControlResultCodesAreNamedInResultCodeDomain()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;

        Assert.Multiple(() =>
        {
            AssertResult(indexes, "DIAMETER_END_USER_SERVICE_DENIED", DiameterResultCodes.EndUserServiceDenied);
            AssertResult(indexes, "DIAMETER_CREDIT_CONTROL_NOT_APPLICABLE", DiameterResultCodes.CreditControlNotApplicable);
            AssertResult(indexes, "DIAMETER_CREDIT_LIMIT_REACHED", DiameterResultCodes.CreditLimitReached);
            AssertResult(indexes, "DIAMETER_USER_UNKNOWN", DiameterResultCodes.UserUnknown);
            AssertResult(indexes, "DIAMETER_RATING_FAILED", DiameterResultCodes.RatingFailed);
        });
    }

    [Test]
    public void RepresentativeGroupedDefinitionsMatchRfcGrammars()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterAvpDefinition multiple = indexes.FindAvpByCode(0U, 456U)!;
        DiameterAvpDefinition granted = indexes.FindAvpByCode(0U, 431U)!;
        DiameterAvpDefinition finalUnit = indexes.FindAvpByCode(0U, 430U)!;
        DiameterAvpDefinition equipmentExtension = indexes.FindAvpByCode(0U, 653U)!;
        DiameterAvpDefinition qosFinal = indexes.FindAvpByCode(0U, 669U)!;

        Assert.Multiple(() =>
        {
            Assert.That(multiple.GroupedRules, Has.Count.EqualTo(11));
            Assert.That(multiple.AllowUnlistedAvps, Is.True);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 456U, 0U, 446U), 0, -1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 456U, 0U, 439U), 0, -1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 456U, 0U, 268U), 0, 1);
            Assert.That(granted.GroupedRules, Has.Count.EqualTo(7));
            AssertRule(indexes.FindGroupedRuleByCode(0U, 431U, 0U, 451U), 0, 1);
            Assert.That(finalUnit.AllowUnlistedAvps, Is.False);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 430U, 0U, 449U), 1, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 430U, 0U, 438U), 0, -1);
            Assert.That(equipmentExtension.GroupedRules, Has.Count.EqualTo(5));
            Assert.That(equipmentExtension.AllowUnlistedAvps, Is.True);
            Assert.That(qosFinal.GroupedRules, Has.Count.EqualTo(4));
            AssertRule(indexes.FindGroupedRuleByCode(0U, 669U, 0U, 509U), 0, -1);
        });
    }

    [Test]
    public void EveryGroupedAndCommandRuleReferencesAComposedAvp()
    {
        DictionaryBuilder builder = CreateRegisteredBuilder();
        DiameterDictionarySnapshot snapshot = builder.ReadDictionarySnapshot();
        DictionaryIndexes indexes = builder.CurrentIndexes;
        DiameterDictionaryModule module = snapshot.Modules[1];

        int avpIndex = 0;
        while (avpIndex < module.Avps.Count)
        {
            DiameterAvpDefinition avp = module.Avps[avpIndex];
            int ruleIndex = 0;
            while (ruleIndex < avp.GroupedRules.Count)
            {
                DiameterAvpRuleDefinition rule = avp.GroupedRules[ruleIndex];
                Assert.That(
                    indexes.FindAvpByCode(rule.AvpVendorId, rule.AvpCode),
                    Is.Not.Null,
                    avp.Name + "/" + rule.AvpName);
                ruleIndex = ruleIndex + 1;
            }

            avpIndex = avpIndex + 1;
        }

        int commandIndex = 0;
        while (commandIndex < module.Commands.Count)
        {
            DiameterCommandDefinition command = module.Commands[commandIndex];
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
    public void CcrAndCcaContainCompleteOccurrenceRulesAndHeaderFlags()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterCommandDefinition ccr = indexes.FindCommandByCode(4U, 272U, true)!;
        DiameterCommandDefinition cca = indexes.FindCommandByCode(4U, 272U, false)!;

        Assert.Multiple(() =>
        {
            Assert.That(ccr.Name, Is.EqualTo("Credit-Control"));
            Assert.That(ccr.Rules, Has.Count.EqualTo(29));
            Assert.That(
                ccr.RequiredFlags,
                Is.EqualTo(DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable));
            Assert.That(
                indexes.FindCommandRuleByCode(4U, 272U, true, 0U, 263U)!
                    .FixedHeadPosition,
                Is.EqualTo(0));
            AssertRule(indexes.FindCommandRuleByCode(4U, 272U, true, 0U, 461U), 1, 1);
            AssertRule(indexes.FindCommandRuleByCode(4U, 272U, true, 0U, 443U), 0, -1);
            AssertRule(indexes.FindCommandRuleByCode(4U, 272U, true, 0U, 456U), 0, -1);
            Assert.That(cca.Name, Is.EqualTo("Credit-Control"));
            Assert.That(cca.Rules, Has.Count.EqualTo(28));
            Assert.That(cca.RequiredFlags, Is.EqualTo(DiameterMessageFlags.Proxyable));
            AssertRule(indexes.FindCommandRuleByCode(4U, 272U, false, 0U, 268U), 1, 1);
            AssertRule(indexes.FindCommandRuleByCode(4U, 272U, false, 0U, 279U), 0, -1);
            AssertRule(indexes.FindCommandRuleByCode(4U, 272U, false, 0U, 669U), 0, 1);
        });
    }

    [Test]
    public void ApplicationSpecificRarAndRaaIncludeAllFourCreditControlExtensions()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterCommandDefinition rar = indexes.FindCommandByCode(4U, 258U, true)!;
        DiameterCommandDefinition raa = indexes.FindCommandByCode(4U, 258U, false)!;
        uint[] extensionCodes = new uint[] { 419U, 453U, 439U, 432U };

        Assert.Multiple(() =>
        {
            Assert.That(rar.Rules, Has.Count.EqualTo(15));
            Assert.That(raa.Rules, Has.Count.EqualTo(17));
            Assert.That(
                indexes.FindCommandByCode(0U, 258U, true),
                Is.Not.SameAs(rar));
        });

        int index = 0;
        while (index < extensionCodes.Length)
        {
            AssertRule(
                indexes.FindCommandRuleByCode(4U, 258U, true, 0U, extensionCodes[index]),
                0,
                1);
            AssertRule(
                indexes.FindCommandRuleByCode(4U, 258U, false, 0U, extensionCodes[index]),
                0,
                1);
            index = index + 1;
        }
    }

    [Test]
    public void RecreatedRfc8506ModuleIsAnEquivalentDuplicate()
    {
        DictionaryBuilder builder = CreateRegisteredBuilder();

        DictionaryRegistrationResult result = builder.RegisterDictionaryModule(
            Rfc8506Dictionary.CreateModule());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DictionaryRegistrationStatus.Duplicate));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(2L));
            Assert.That(result.Snapshot.Modules, Has.Count.EqualTo(2));
        });
    }

    private static DictionaryBuilder CreateRegisteredBuilder()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        Assert.That(
            builder.RegisterDictionaryModule(Rfc6733Dictionary.CreateModule()).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        Assert.That(
            builder.RegisterDictionaryModule(Rfc8506Dictionary.CreateModule()).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        return builder;
    }

    private static void AssertResult(
        DictionaryIndexes indexes,
        string name,
        uint expected)
    {
        DiameterEnumValueDefinition? value = indexes.FindEnumValueByAvpName(
            0U,
            DiameterAvpCodes.ResultCode,
            name);
        Assert.That(value, Is.Not.Null);
        Assert.That(value!.Value, Is.EqualTo((int)expected));
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
