using System.Collections.Concurrent;

namespace diameter.Tests;

public class DictionaryBuilderTests
{
    [Test]
    public void RegistrationCommitsCompleteDetachedSnapshotAndIndexes()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        DiameterDictionaryModule module = CreateCompleteModule("base");

        DictionaryRegistrationResult result = builder.RegisterDictionaryModule(module);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DictionaryRegistrationStatus.Success));
            Assert.That(result.Code, Is.EqualTo("module-registered"));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(1L));
            Assert.That(result.Snapshot.Modules, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.Vendors, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.Applications, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.Types, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.EnumValues, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.Avps, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.Commands, Has.Count.EqualTo(1));
            Assert.That(builder.CurrentIndexes.FindVendorById(10415U), Is.Not.Null);
            Assert.That(builder.CurrentIndexes.FindApplicationById(0U, 4U), Is.Not.Null);
            Assert.That(builder.CurrentIndexes.FindTypeByName(0U, "enumerated"), Is.Not.Null);
            Assert.That(builder.CurrentIndexes.FindAvpByCode(0U, 100U), Is.Not.Null);
            Assert.That(
                builder.CurrentIndexes.FindCommandByCode(4U, 272U, true),
                Is.Not.Null);
        });

        module.Name = "changed-input";
        module.Vendors[0].Name = "changed-vendor";
        result.Snapshot.Modules[0].Name = "changed-result";
        result.Snapshot.Avps[0].Name = "changed-avp";
        DiameterDictionarySnapshot firstRead = builder.ReadDictionarySnapshot();
        firstRead.Commands[0].Name = "changed-command";
        firstRead.Modules.Clear();
        DiameterDictionarySnapshot secondRead = builder.ReadDictionarySnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(secondRead.Modules[0].Name, Is.EqualTo("base"));
            Assert.That(secondRead.Vendors[0].Name, Is.EqualTo("3GPP"));
            Assert.That(secondRead.Avps[0].Name, Is.EqualTo("Example-AVP"));
            Assert.That(secondRead.Commands[0].Name, Is.EqualTo("Credit-Control"));
        });
    }

    [Test]
    public void EquivalentModuleIsAcceptedAsDuplicateWithoutNewRevision()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        DiameterDictionaryModule original = CreateCompleteModule("base");
        original.Dependencies.Add("dependency-a");
        original.Dependencies.Add("dependency-b");
        DiameterDictionaryModule dependencyA = CreateEmptyModule("dependency-a");
        DiameterDictionaryModule dependencyB = CreateEmptyModule("dependency-b");
        Assert.That(
            builder.RegisterDictionaryModule(dependencyB).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        Assert.That(
            builder.RegisterDictionaryModule(dependencyA).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        Assert.That(
            builder.RegisterDictionaryModule(original).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        DiameterDictionaryModule duplicate = DictionaryBuilder.CloneModule(original);
        duplicate.Name = " BASE ";
        duplicate.Dependencies.Reverse();
        duplicate.Vendors[0].Name = " 3gpp ";
        duplicate.Types[0].Name = " ENUMERATED ";
        duplicate.EnumValues[0].Name = " enabled ";
        duplicate.EnumValues[0].TypeName = " enumerated ";
        duplicate.Avps[0].Name = " example-avp ";
        duplicate.Avps[0].TypeName = " enumerated ";
        duplicate.Commands[0].Name = " credit-control ";

        DictionaryRegistrationResult result = builder.RegisterDictionaryModule(duplicate);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DictionaryRegistrationStatus.Duplicate));
            Assert.That(result.Code, Is.EqualTo("duplicate-module"));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(3L));
            Assert.That(result.Snapshot.Modules, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void EquivalentDefinitionsFromAnotherModuleAreMergedOnce()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        DiameterDictionaryModule first = CreateCompleteModule("first");
        DiameterDictionaryModule second = DictionaryBuilder.CloneModule(first);
        second.Name = "second";
        second.Vendors[0].Name = "3gpp";
        second.Commands[0].Name = "CREDIT-CONTROL";

        Assert.That(
            builder.RegisterDictionaryModule(first).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        DictionaryRegistrationResult result = builder.RegisterDictionaryModule(second);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DictionaryRegistrationStatus.Success));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(2L));
            Assert.That(result.Snapshot.Modules, Has.Count.EqualTo(2));
            Assert.That(result.Snapshot.Vendors, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.Applications, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.Types, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.EnumValues, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.Avps, Has.Count.EqualTo(1));
            Assert.That(result.Snapshot.Commands, Has.Count.EqualTo(1));
        });
    }

    [TestCase("vendor", "vendor-definition-conflict")]
    [TestCase("application", "application-definition-conflict")]
    [TestCase("type", "type-definition-conflict")]
    [TestCase("enum", "enum-value-definition-conflict")]
    [TestCase("avp", "avp-definition-conflict")]
    [TestCase("command", "command-definition-conflict")]
    public void ConflictingDefinitionsRejectWholeTransaction(string kind, string expectedCode)
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        Assert.That(
            builder.RegisterDictionaryModule(CreateCompleteModule("base")).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        DiameterDictionaryModule conflicting = CreateCompleteModule("conflicting");
        ApplyConflict(conflicting, kind);

        DictionaryRegistrationResult result = builder.RegisterDictionaryModule(conflicting);
        DiameterDictionarySnapshot snapshot = builder.ReadDictionarySnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DictionaryRegistrationStatus.Conflict));
            Assert.That(result.Code, Is.EqualTo(expectedCode));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(1L));
            Assert.That(snapshot.Revision, Is.EqualTo(1L));
            Assert.That(snapshot.Modules, Has.Count.EqualTo(1));
            Assert.That(snapshot.Modules[0].Name, Is.EqualTo("base"));
            Assert.That(snapshot.Vendors, Has.Count.EqualTo(1));
            Assert.That(snapshot.Commands, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void SameModuleNameWithDifferentContentIsRejectedWithoutMutation()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        DiameterDictionaryModule original = CreateEmptyModule("module");
        original.Version = "1";
        Assert.That(
            builder.RegisterDictionaryModule(original).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        DiameterDictionaryModule conflicting = CreateEmptyModule(" MODULE ");
        conflicting.Version = "2";

        DictionaryRegistrationResult result = builder.RegisterDictionaryModule(conflicting);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DictionaryRegistrationStatus.Conflict));
            Assert.That(result.Code, Is.EqualTo("module-name-conflict"));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(1L));
            Assert.That(result.Snapshot.Modules[0].Version, Is.EqualTo("1"));
        });
    }

    [Test]
    public void DependenciesAreValidatedAndComposedInStableTopologicalOrder()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        DiameterDictionaryModule missing = CreateEmptyModule("missing-dependent");
        missing.Dependencies.Add("not-registered");
        DictionaryRegistrationResult missingResult =
            builder.RegisterDictionaryModule(missing);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Status, Is.EqualTo(DictionaryRegistrationStatus.Invalid));
            Assert.That(missingResult.Code, Is.EqualTo("missing-module-dependency"));
            Assert.That(missingResult.Snapshot.Revision, Is.EqualTo(0L));
        });

        DiameterDictionaryModule zBase = CreateEmptyModule("z-base");
        DiameterDictionaryModule bExtension = CreateEmptyModule("b-extension");
        bExtension.Dependencies.Add("z-base");
        DiameterDictionaryModule aIndependent = CreateEmptyModule("a-independent");
        Assert.That(
            builder.RegisterDictionaryModule(zBase).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        Assert.That(
            builder.RegisterDictionaryModule(bExtension).Status,
            Is.EqualTo(DictionaryRegistrationStatus.Success));
        DictionaryRegistrationResult result =
            builder.RegisterDictionaryModule(aIndependent);

        Assert.That(
            result.Snapshot.Modules.ConvertAll(module => module.Name),
            Is.EqualTo(new[] { "a-independent", "z-base", "b-extension" }));

        DiameterDictionaryModule cycleA = CreateEmptyModule("cycle-a");
        DiameterDictionaryModule cycleB = CreateEmptyModule("cycle-b");
        cycleA.Dependencies.Add("cycle-b");
        cycleB.Dependencies.Add("cycle-a");
        DictionaryRegistrationResult cycleResult = builder.ComposeModules(
            new List<DiameterDictionaryModule> { cycleB, cycleA },
            99L);

        Assert.Multiple(() =>
        {
            Assert.That(cycleResult.Status, Is.EqualTo(DictionaryRegistrationStatus.Invalid));
            Assert.That(cycleResult.Code, Is.EqualTo("circular-module-dependency"));
            Assert.That(builder.ReadDictionarySnapshot().Revision, Is.EqualTo(3L));
        });
    }

    [Test]
    public void InvalidModulesAreRejectedWithoutChangingSnapshot()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        DiameterDictionaryModule missingName = CreateEmptyModule(" ");
        DiameterDictionaryModule missingCollections = CreateEmptyModule("collections");
        missingCollections.Commands = null!;
        DiameterDictionaryModule invalidEntry = CreateEmptyModule("entry");
        invalidEntry.Avps.Add(null!);

        DictionaryRegistrationResult nullResult = builder.RegisterDictionaryModule(null);
        DictionaryRegistrationResult nameResult = builder.RegisterDictionaryModule(missingName);
        DictionaryRegistrationResult collectionsResult =
            builder.RegisterDictionaryModule(missingCollections);
        DictionaryRegistrationResult entryResult = builder.RegisterDictionaryModule(invalidEntry);

        Assert.Multiple(() =>
        {
            Assert.That(nullResult.Code, Is.EqualTo("missing-dictionary-module"));
            Assert.That(nameResult.Code, Is.EqualTo("missing-module-name"));
            Assert.That(collectionsResult.Code, Is.EqualTo("missing-module-definitions"));
            Assert.That(entryResult.Code, Is.EqualTo("invalid-module-definition"));
            Assert.That(nullResult.Status, Is.EqualTo(DictionaryRegistrationStatus.Invalid));
            Assert.That(nameResult.Status, Is.EqualTo(DictionaryRegistrationStatus.Invalid));
            Assert.That(collectionsResult.Status, Is.EqualTo(DictionaryRegistrationStatus.Invalid));
            Assert.That(entryResult.Status, Is.EqualTo(DictionaryRegistrationStatus.Invalid));
            Assert.That(builder.ReadDictionarySnapshot().Revision, Is.EqualTo(0L));
            Assert.That(builder.ReadDictionarySnapshot().Modules, Is.Empty);
        });
    }

    [Test]
    public void ConcurrentRegistrationsCommitAtomicallyWithDeterministicSnapshotOrder()
    {
        DictionaryBuilder builder = new DictionaryBuilder();
        ConcurrentBag<DictionaryRegistrationStatus> statuses =
            new ConcurrentBag<DictionaryRegistrationStatus>();

        Parallel.For(0, 24, index =>
        {
            string name = $"module-{index:D2}";
            statuses.Add(builder.RegisterDictionaryModule(CreateEmptyModule(name)).Status);
        });

        DiameterDictionarySnapshot snapshot = builder.ReadDictionarySnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(statuses, Has.Count.EqualTo(24));
            Assert.That(statuses, Has.All.EqualTo(DictionaryRegistrationStatus.Success));
            Assert.That(snapshot.Revision, Is.EqualTo(24L));
            Assert.That(snapshot.Modules, Has.Count.EqualTo(24));
            Assert.That(snapshot.Modules[0].Name, Is.EqualTo("module-00"));
            Assert.That(snapshot.Modules[23].Name, Is.EqualTo("module-23"));
        });
    }

    private static void ApplyConflict(DiameterDictionaryModule module, string kind)
    {
        switch (kind)
        {
            case "vendor":
                module.Vendors[0].Name = "Different-Vendor";
                break;
            case "application":
                module.Applications[0].Name = "Different-Application";
                break;
            case "type":
                module.Types[0].FixedDataLength = 8;
                break;
            case "enum":
                module.EnumValues[0].Name = "DISABLED";
                break;
            case "avp":
                module.Avps[0].Name = "Different-AVP";
                break;
            case "command":
                module.Commands[0].Name = "Different-Command";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static DiameterDictionaryModule CreateEmptyModule(string name)
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = name;
        module.Version = "1.0";
        return module;
    }

    private static DiameterDictionaryModule CreateCompleteModule(string name)
    {
        DiameterDictionaryModule module = CreateEmptyModule(name);

        DiameterVendorDefinition vendor = new DiameterVendorDefinition();
        vendor.Id = 10415U;
        vendor.Name = "3GPP";
        module.Vendors.Add(vendor);

        DiameterApplicationDefinition application = new DiameterApplicationDefinition();
        application.Id = 4U;
        application.VendorId = 0U;
        application.Name = "Credit-Control";
        module.Applications.Add(application);

        DiameterTypeDefinition type = new DiameterTypeDefinition();
        type.Name = "Enumerated";
        type.VendorId = 0U;
        type.BaseType = DiameterAvpBaseType.Integer32;
        type.FixedDataLength = 4;
        module.Types.Add(type);

        DiameterEnumValueDefinition enumValue = new DiameterEnumValueDefinition();
        enumValue.Name = "ENABLED";
        enumValue.Value = 1;
        enumValue.TypeName = "Enumerated";
        enumValue.TypeVendorId = 0U;
        enumValue.AvpCode = 100U;
        enumValue.AvpVendorId = 0U;
        module.EnumValues.Add(enumValue);

        DiameterAvpRuleDefinition groupedRule = CreateRule(263U, "Session-Id");
        DiameterAvpDefinition avp = new DiameterAvpDefinition();
        avp.Code = 100U;
        avp.VendorId = 0U;
        avp.Name = "Example-AVP";
        avp.TypeName = "Enumerated";
        avp.BaseType = DiameterAvpBaseType.Integer32;
        avp.RequiredFlags = DiameterAvpFlags.Mandatory;
        avp.FixedDataLength = 4;
        avp.GroupedRules.Add(groupedRule);
        module.Avps.Add(avp);

        DiameterCommandDefinition command = new DiameterCommandDefinition();
        command.Code = 272U;
        command.ApplicationId = 4U;
        command.Name = "Credit-Control";
        command.IsRequest = true;
        command.RequiredFlags = DiameterMessageFlags.Request;
        command.Rules.Add(CreateRule(100U, "Example-AVP"));
        module.Commands.Add(command);

        return module;
    }

    private static DiameterAvpRuleDefinition CreateRule(uint code, string name)
    {
        DiameterAvpRuleDefinition rule = new DiameterAvpRuleDefinition();
        rule.AvpCode = code;
        rule.AvpVendorId = 0U;
        rule.AvpName = name;
        rule.MinimumCount = 1;
        rule.MaximumCount = 1;
        return rule;
    }
}
