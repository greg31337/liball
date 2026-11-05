namespace diameter.Tests;

public class RfcExtensionDictionaryTests
{
    [Test]
    public void ModulesDeclareTheirRfcIdentityDependenciesAndDefinitionCounts()
    {
        DiameterDictionaryModule capabilities = Rfc6737Dictionary.CreateModule();
        DiameterDictionaryModule redirect = Rfc7075Dictionary.CreateModule();
        DiameterDictionaryModule overload = Rfc7683Dictionary.CreateModule();
        DiameterDictionaryModule priority = Rfc7944Dictionary.CreateModule();
        DiameterDictionaryModule peer = Rfc8581Dictionary.CreateModule();
        DiameterDictionaryModule rate = Rfc8582Dictionary.CreateModule();
        DiameterDictionaryModule load = Rfc8583Dictionary.CreateModule();

        Assert.Multiple(() =>
        {
            AssertModule(capabilities, "rfc6737", "RFC 6737", "rfc6733", 1, 0, 0, 0, 2);
            AssertModule(redirect, "rfc7075", "RFC 7075", "rfc6733", 0, 0, 1, 1, 0);
            AssertModule(overload, "rfc7683", "RFC 7683", "rfc6733", 0, 1, 3, 7, 0);
            AssertModule(priority, "rfc7944", "RFC 7944", "rfc6733", 0, 1, 16, 1, 0);
            AssertModule(peer, "rfc8581", "RFC 8581", "rfc7683", 0, 0, 2, 2, 0);
            AssertModule(rate, "rfc8582", "RFC 8582", "rfc8581", 0, 0, 1, 1, 0);
            AssertModule(load, "rfc8583", "RFC 8583", "rfc8581", 0, 1, 2, 3, 0);
        });
    }

    [Test]
    public void AllExtensionModulesComposeWithTheirDependencies()
    {
        DictionaryBuilder builder = CreateRegisteredBuilder();
        DiameterDictionarySnapshot snapshot = builder.ReadDictionarySnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Revision, Is.EqualTo(8L));
            Assert.That(
                snapshot.Modules.ConvertAll(module => module.Name),
                Is.EqualTo(new[]
                {
                    "rfc6733", "rfc6737", "rfc7075", "rfc7683",
                    "rfc7944", "rfc8581", "rfc8582", "rfc8583"
                }));
            Assert.That(snapshot.Applications, Has.Count.EqualTo(4));
            Assert.That(snapshot.Types, Has.Count.EqualTo(27));
            Assert.That(snapshot.Avps, Has.Count.EqualTo(64));
            Assert.That(snapshot.Commands, Has.Count.EqualTo(16));
        });
    }

    [Test]
    public void ExtensionModulesRejectRegistrationBeforeTheirDependencies()
    {
        DictionaryBuilder builder = new DictionaryBuilder();

        Assert.Multiple(() =>
        {
            AssertMissingDependency(builder, Rfc6737Dictionary.CreateModule());
            AssertMissingDependency(builder, Rfc7075Dictionary.CreateModule());
            AssertMissingDependency(builder, Rfc7683Dictionary.CreateModule());
            AssertMissingDependency(builder, Rfc7944Dictionary.CreateModule());
            AssertMissingDependency(builder, Rfc8581Dictionary.CreateModule());
            AssertMissingDependency(builder, Rfc8582Dictionary.CreateModule());
            AssertMissingDependency(builder, Rfc8583Dictionary.CreateModule());
        });
    }

    [Test]
    public void CapabilitiesUpdateCommandsMatchRfc6737GrammarAndHeaderFlags()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterCommandDefinition request = indexes.FindCommandByCode(10U, 328U, true)!;
        DiameterCommandDefinition answer = indexes.FindCommandByCode(10U, 328U, false)!;

        Assert.Multiple(() =>
        {
            Assert.That(request.Name, Is.EqualTo("Capabilities-Update"));
            Assert.That(request.RequiredFlags, Is.EqualTo(DiameterMessageFlags.Request));
            Assert.That(
                request.ProhibitedFlags,
                Is.EqualTo(
                    DiameterMessageFlags.Proxyable |
                    DiameterMessageFlags.Error |
                    DiameterMessageFlags.PotentiallyRetransmitted));
            Assert.That(request.Rules, Has.Count.EqualTo(11));
            AssertRule(indexes.FindCommandRuleByCode(10U, 328U, true, 0U, 257U), 1, -1);
            AssertRule(indexes.FindCommandRuleByCode(10U, 328U, true, 0U, 260U), 0, -1);
            Assert.That(answer.RequiredFlags, Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(answer.Rules, Has.Count.EqualTo(4));
            AssertRule(indexes.FindCommandRuleByCode(10U, 328U, false, 0U, 268U), 1, 1);
            AssertRule(indexes.FindCommandRuleByCode(10U, 328U, false, 0U, 281U), 0, 1);
        });
    }

    [Test]
    public void RealmRedirectDefinitionsMatchRfc7075()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterAvpDefinition avp = indexes.FindAvpByCode(0U, 620U)!;
        DiameterEnumValueDefinition result = indexes.FindEnumValueByAvpName(
            0U,
            DiameterAvpCodes.ResultCode,
            "DIAMETER_REALM_REDIRECT_INDICATION")!;

        Assert.Multiple(() =>
        {
            Assert.That(avp.Name, Is.EqualTo("Redirect-Realm"));
            Assert.That(avp.TypeName, Is.EqualTo("DiameterIdentity"));
            Assert.That(avp.BaseType, Is.EqualTo(DiameterAvpBaseType.OctetString));
            Assert.That(avp.RequiredFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(avp.ProhibitedFlags, Is.EqualTo(DiameterAvpFlags.VendorSpecific));
            Assert.That(result.Value, Is.EqualTo(3011));
            Assert.That(DiameterResultCodes.RealmRedirectIndication, Is.EqualTo(3011U));
        });
    }

    [Test]
    public void DoicDefinitionsMatchRfc7683AndLaterFeatureAssignments()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterAvpDefinition supported = indexes.FindAvpByCode(0U, 621U)!;
        DiameterAvpDefinition feature = indexes.FindAvpByCode(0U, 622U)!;
        DiameterAvpDefinition report = indexes.FindAvpByCode(0U, 623U)!;

        Assert.Multiple(() =>
        {
            Assert.That(supported.BaseType, Is.EqualTo(DiameterAvpBaseType.Grouped));
            Assert.That(supported.AllowUnlistedAvps, Is.True);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 621U, 0U, 622U), 0, 1);
            Assert.That(feature.BaseType, Is.EqualTo(DiameterAvpBaseType.Unsigned64));
            Assert.That(feature.FixedDataLength, Is.EqualTo(8));
            Assert.That(report.GroupedRules, Has.Count.EqualTo(4));
            AssertRule(indexes.FindGroupedRuleByCode(0U, 623U, 0U, 624U), 1, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 623U, 0U, 626U), 1, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 623U, 0U, 627U), 0, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 623U, 0U, 625U), 0, 1);
            AssertValue(indexes, 622U, "OLR_DEFAULT_ALGO", 1);
            AssertValue(indexes, 622U, "OLR_RATE_ALGORITHM", 4);
            AssertValue(indexes, 622U, "OC_PEER_REPORT", 16);
            AssertValue(indexes, 626U, "HOST_REPORT", 0);
            AssertValue(indexes, 626U, "REALM_REPORT", 1);
            AssertValue(indexes, 626U, "PEER_REPORT", 2);
        });
    }

    [Test]
    public void RoutingPriorityDefinesAllSixteenRfc7944WireValues()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterAvpDefinition avp = indexes.FindAvpByCode(0U, 301U)!;

        Assert.Multiple(() =>
        {
            Assert.That(avp.Name, Is.EqualTo("DRMP"));
            Assert.That(avp.TypeName, Is.EqualTo("DRMP"));
            Assert.That(avp.BaseType, Is.EqualTo(DiameterAvpBaseType.Integer32));
            Assert.That(avp.FixedDataLength, Is.EqualTo(4));
        });

        int priority = 0;
        while (priority <= 15)
        {
            AssertValue(indexes, 301U, "PRIORITY_" + priority, priority);
            priority = priority + 1;
        }
    }

    [Test]
    public void PeerRateAndLoadAvpsUseAssignedWireTypesFlagsAndGrammar()
    {
        DictionaryIndexes indexes = CreateRegisteredBuilder().CurrentIndexes;
        DiameterAvpDefinition peerAlgorithm = indexes.FindAvpByCode(0U, 648U)!;
        DiameterAvpDefinition source = indexes.FindAvpByCode(0U, 649U)!;
        DiameterAvpDefinition load = indexes.FindAvpByCode(0U, 650U)!;
        DiameterAvpDefinition loadType = indexes.FindAvpByCode(0U, 651U)!;
        DiameterAvpDefinition loadValue = indexes.FindAvpByCode(0U, 652U)!;
        DiameterAvpDefinition maximumRate = indexes.FindAvpByCode(0U, 670U)!;

        Assert.Multiple(() =>
        {
            AssertAvp(peerAlgorithm, DiameterAvpBaseType.Unsigned64, 8);
            AssertAvp(source, DiameterAvpBaseType.OctetString, -1);
            AssertAvp(load, DiameterAvpBaseType.Grouped, -1);
            AssertAvp(loadType, DiameterAvpBaseType.Integer32, 4);
            AssertAvp(loadValue, DiameterAvpBaseType.Unsigned64, 8);
            AssertAvp(maximumRate, DiameterAvpBaseType.Unsigned32, 4);
            Assert.That(load.AllowUnlistedAvps, Is.True);
            Assert.That(load.GroupedRules, Has.Count.EqualTo(3));
            AssertRule(indexes.FindGroupedRuleByCode(0U, 650U, 0U, 651U), 0, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 650U, 0U, 652U), 0, 1);
            AssertRule(indexes.FindGroupedRuleByCode(0U, 650U, 0U, 649U), 0, 1);
            AssertValue(indexes, 651U, "HOST", 0);
            AssertValue(indexes, 651U, "PEER", 1);
            Assert.That((int)DiameterLoadSource.Host, Is.EqualTo(0));
            Assert.That((int)DiameterLoadSource.Peer, Is.EqualTo(1));
        });
    }

    private static DictionaryBuilder CreateRegisteredBuilder()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        Register(builder, Rfc6733Dictionary.CreateModule());
        Register(builder, Rfc6737Dictionary.CreateModule());
        Register(builder, Rfc7075Dictionary.CreateModule());
        Register(builder, Rfc7683Dictionary.CreateModule());
        Register(builder, Rfc7944Dictionary.CreateModule());
        Register(builder, Rfc8581Dictionary.CreateModule());
        Register(builder, Rfc8582Dictionary.CreateModule());
        Register(builder, Rfc8583Dictionary.CreateModule());
        return builder;
    }

    private static void Register(DictionaryBuilder builder, DiameterDictionaryModule module)
    {
        Assert.That(
            builder.RegisterDictionaryModule(module).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success),
            module.Name);
    }

    private static void AssertModule(
        DiameterDictionaryModule module,
        string name,
        string version,
        string dependency,
        int applications,
        int types,
        int values,
        int avps,
        int commands)
    {
        Assert.That(module.Name, Is.EqualTo(name));
        Assert.That(module.Version, Is.EqualTo(version));
        Assert.That(module.Dependencies, Is.EqualTo(new[] { dependency }));
        Assert.That(module.Applications, Has.Count.EqualTo(applications));
        Assert.That(module.Types, Has.Count.EqualTo(types));
        Assert.That(module.EnumValues, Has.Count.EqualTo(values));
        Assert.That(module.Avps, Has.Count.EqualTo(avps));
        Assert.That(module.Commands, Has.Count.EqualTo(commands));
    }

    private static void AssertMissingDependency(
        DictionaryBuilder builder,
        DiameterDictionaryModule module)
    {
        DictionaryRegistrationResult result = builder.RegisterDictionaryModule(module);
        Assert.That(result.Status, Is.EqualTo(DictionaryRegistrationStatus.Invalid));
        Assert.That(result.Code, Is.EqualTo("missing-module-dependency"));
    }

    private static void AssertAvp(
        DiameterAvpDefinition avp,
        DiameterAvpBaseType baseType,
        int fixedLength)
    {
        Assert.That(avp.BaseType, Is.EqualTo(baseType));
        Assert.That(avp.FixedDataLength, Is.EqualTo(fixedLength));
        Assert.That(avp.RequiredFlags, Is.EqualTo(DiameterAvpFlags.None));
        Assert.That(avp.ProhibitedFlags, Is.EqualTo(DiameterAvpFlags.VendorSpecific));
    }

    private static void AssertValue(
        DictionaryIndexes indexes,
        uint avpCode,
        string name,
        int expected)
    {
        DiameterEnumValueDefinition? value = indexes.FindEnumValueByAvpName(
            0U,
            avpCode,
            name);
        Assert.That(value, Is.Not.Null, name);
        Assert.That(value!.Value, Is.EqualTo(expected), name);
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
