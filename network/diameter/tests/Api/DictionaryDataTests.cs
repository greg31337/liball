using System.Collections.Generic;

namespace diameter.Tests;

public class DictionaryDataTests
{
    [Test]
    public void DefinitionDefaultsRepresentEmptyPortableDictionaryData()
    {
        DiameterVendorDefinition vendor = new DiameterVendorDefinition();
        DiameterApplicationDefinition application = new DiameterApplicationDefinition();
        DiameterTypeDefinition type = new DiameterTypeDefinition();
        DiameterEnumValueDefinition enumValue = new DiameterEnumValueDefinition();
        DiameterAvpRuleDefinition rule = new DiameterAvpRuleDefinition();
        DiameterAvpDefinition avp = new DiameterAvpDefinition();
        DiameterCommandDefinition command = new DiameterCommandDefinition();
        DiameterDictionaryRegistrationResult registration =
            new DiameterDictionaryRegistrationResult();

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Id, Is.Zero);
            Assert.That(vendor.Name, Is.Empty);

            Assert.That(application.Id, Is.Zero);
            Assert.That(application.VendorId, Is.Zero);
            Assert.That(application.Name, Is.Empty);
            Assert.That(application.IsAccounting, Is.False);

            Assert.That(type.Name, Is.Empty);
            Assert.That(type.VendorId, Is.Zero);
            Assert.That(type.BaseType, Is.EqualTo(DiameterAvpBaseType.OctetString));
            Assert.That(type.ParentTypeName, Is.Empty);
            Assert.That(type.ParentTypeVendorId, Is.Zero);
            Assert.That(type.FixedDataLength, Is.EqualTo(-1));

            Assert.That(enumValue.Name, Is.Empty);
            Assert.That(enumValue.Value, Is.Zero);
            Assert.That(enumValue.TypeName, Is.Empty);
            Assert.That(enumValue.TypeVendorId, Is.Zero);
            Assert.That(enumValue.AvpCode, Is.Zero);
            Assert.That(enumValue.AvpVendorId, Is.Zero);

            Assert.That(rule.AvpCode, Is.Zero);
            Assert.That(rule.AvpVendorId, Is.Zero);
            Assert.That(rule.AvpName, Is.Empty);
            Assert.That(rule.MinimumCount, Is.Zero);
            Assert.That(rule.MaximumCount, Is.EqualTo(1));
            Assert.That(rule.FixedHeadPosition, Is.EqualTo(-1));
            Assert.That(rule.OrderingGroup, Is.EqualTo(-1));

            Assert.That(avp.Code, Is.Zero);
            Assert.That(avp.VendorId, Is.Zero);
            Assert.That(avp.Name, Is.Empty);
            Assert.That(avp.TypeName, Is.Empty);
            Assert.That(avp.BaseType, Is.EqualTo(DiameterAvpBaseType.OctetString));
            Assert.That(avp.RequiredFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(avp.ProhibitedFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(avp.FixedDataLength, Is.EqualTo(-1));
            Assert.That(avp.GroupedRules, Is.Empty);
            Assert.That(avp.AllowUnlistedAvps, Is.True);

            Assert.That(command.Code, Is.Zero);
            Assert.That(command.ApplicationId, Is.Zero);
            Assert.That(command.Name, Is.Empty);
            Assert.That(command.IsRequest, Is.False);
            Assert.That(command.RequiredFlags, Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(command.ProhibitedFlags, Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(command.Rules, Is.Empty);
            Assert.That(command.AllowUnlistedAvps, Is.True);

            Assert.That(registration.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
            Assert.That(registration.Code, Is.Empty);
            Assert.That(registration.Message, Is.Empty);
            Assert.That(registration.Snapshot, Is.Not.Null);
            Assert.That(registration.Snapshot.Revision, Is.Zero);
        });
    }

    [Test]
    public void ModulesAndSnapshotsStartWithIndependentOwnedCollections()
    {
        DiameterDictionaryModule firstModule = new DiameterDictionaryModule();
        DiameterDictionaryModule secondModule = new DiameterDictionaryModule();
        DiameterDictionarySnapshot firstSnapshot = new DiameterDictionarySnapshot();
        DiameterDictionarySnapshot secondSnapshot = new DiameterDictionarySnapshot();
        firstModule.Dependencies.Add("base");
        firstModule.Vendors.Add(new DiameterVendorDefinition());
        firstModule.Applications.Add(new DiameterApplicationDefinition());
        firstModule.Types.Add(new DiameterTypeDefinition());
        firstModule.EnumValues.Add(new DiameterEnumValueDefinition());
        firstModule.Avps.Add(new DiameterAvpDefinition());
        firstModule.Commands.Add(new DiameterCommandDefinition());
        firstSnapshot.Modules.Add(firstModule);
        firstSnapshot.Vendors.Add(new DiameterVendorDefinition());
        firstSnapshot.Applications.Add(new DiameterApplicationDefinition());
        firstSnapshot.Types.Add(new DiameterTypeDefinition());
        firstSnapshot.EnumValues.Add(new DiameterEnumValueDefinition());
        firstSnapshot.Avps.Add(new DiameterAvpDefinition());
        firstSnapshot.Commands.Add(new DiameterCommandDefinition());

        Assert.Multiple(() =>
        {
            Assert.That(firstModule.Name, Is.Empty);
            Assert.That(firstModule.Version, Is.Empty);
            Assert.That(secondModule.Dependencies, Is.Empty);
            Assert.That(secondModule.Vendors, Is.Empty);
            Assert.That(secondModule.Applications, Is.Empty);
            Assert.That(secondModule.Types, Is.Empty);
            Assert.That(secondModule.EnumValues, Is.Empty);
            Assert.That(secondModule.Avps, Is.Empty);
            Assert.That(secondModule.Commands, Is.Empty);
            Assert.That(firstModule.Dependencies, Is.Not.SameAs(secondModule.Dependencies));
            Assert.That(firstModule.Vendors, Is.Not.SameAs(secondModule.Vendors));
            Assert.That(firstModule.Applications, Is.Not.SameAs(secondModule.Applications));
            Assert.That(firstModule.Types, Is.Not.SameAs(secondModule.Types));
            Assert.That(firstModule.EnumValues, Is.Not.SameAs(secondModule.EnumValues));
            Assert.That(firstModule.Avps, Is.Not.SameAs(secondModule.Avps));
            Assert.That(firstModule.Commands, Is.Not.SameAs(secondModule.Commands));

            Assert.That(firstSnapshot.Revision, Is.Zero);
            Assert.That(secondSnapshot.Modules, Is.Empty);
            Assert.That(secondSnapshot.Vendors, Is.Empty);
            Assert.That(secondSnapshot.Applications, Is.Empty);
            Assert.That(secondSnapshot.Types, Is.Empty);
            Assert.That(secondSnapshot.EnumValues, Is.Empty);
            Assert.That(secondSnapshot.Avps, Is.Empty);
            Assert.That(secondSnapshot.Commands, Is.Empty);
            Assert.That(firstSnapshot.Modules, Is.Not.SameAs(secondSnapshot.Modules));
            Assert.That(firstSnapshot.Vendors, Is.Not.SameAs(secondSnapshot.Vendors));
            Assert.That(firstSnapshot.Applications, Is.Not.SameAs(secondSnapshot.Applications));
            Assert.That(firstSnapshot.Types, Is.Not.SameAs(secondSnapshot.Types));
            Assert.That(firstSnapshot.EnumValues, Is.Not.SameAs(secondSnapshot.EnumValues));
            Assert.That(firstSnapshot.Avps, Is.Not.SameAs(secondSnapshot.Avps));
            Assert.That(firstSnapshot.Commands, Is.Not.SameAs(secondSnapshot.Commands));
        });
    }

    [Test]
    public void DictionaryGraphStoresEveryDefinitionIdentityAndConstraintField()
    {
        DiameterVendorDefinition vendor = new DiameterVendorDefinition();
        vendor.Id = 10415U;
        vendor.Name = "3GPP";

        DiameterApplicationDefinition application = new DiameterApplicationDefinition();
        application.Id = 4U;
        application.VendorId = 10415U;
        application.Name = "credit-control";
        application.IsAccounting = true;

        DiameterTypeDefinition type = new DiameterTypeDefinition();
        type.Name = "Disconnect-Cause-Type";
        type.VendorId = 10415U;
        type.BaseType = DiameterAvpBaseType.Integer32;
        type.ParentTypeName = "Enumerated";
        type.ParentTypeVendorId = 0U;
        type.FixedDataLength = 4;

        DiameterEnumValueDefinition enumValue = new DiameterEnumValueDefinition();
        enumValue.Name = "REBOOTING";
        enumValue.Value = 0;
        enumValue.TypeName = type.Name;
        enumValue.TypeVendorId = type.VendorId;
        enumValue.AvpCode = DiameterAvpCodes.DisconnectCause;
        enumValue.AvpVendorId = 0U;

        DiameterAvpRuleDefinition rule = new DiameterAvpRuleDefinition();
        rule.AvpCode = DiameterAvpCodes.SessionId;
        rule.AvpVendorId = 0U;
        rule.AvpName = "Session-Id";
        rule.MinimumCount = 1;
        rule.MaximumCount = -1;
        rule.FixedHeadPosition = 0;
        rule.OrderingGroup = 2;

        DiameterAvpDefinition avp = new DiameterAvpDefinition();
        avp.Code = DiameterAvpCodes.ProxyInfo;
        avp.VendorId = 0U;
        avp.Name = "Proxy-Info";
        avp.TypeName = "Grouped";
        avp.BaseType = DiameterAvpBaseType.Grouped;
        avp.RequiredFlags = DiameterAvpFlags.Mandatory;
        avp.ProhibitedFlags = DiameterAvpFlags.Protected;
        avp.FixedDataLength = 12;
        avp.GroupedRules.Add(rule);
        avp.AllowUnlistedAvps = false;

        DiameterCommandDefinition command = new DiameterCommandDefinition();
        command.Code = DiameterCommandCodes.CreditControl;
        command.ApplicationId = DiameterApplicationIds.CreditControl;
        command.Name = "Credit-Control-Request";
        command.IsRequest = true;
        command.RequiredFlags = DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable;
        command.ProhibitedFlags = DiameterMessageFlags.Error;
        command.Rules.Add(rule);
        command.AllowUnlistedAvps = false;

        Assert.Multiple(() =>
        {
            Assert.That(vendor.Id, Is.EqualTo(10415U));
            Assert.That(vendor.Name, Is.EqualTo("3GPP"));
            Assert.That(application.Id, Is.EqualTo(4U));
            Assert.That(application.VendorId, Is.EqualTo(10415U));
            Assert.That(application.Name, Is.EqualTo("credit-control"));
            Assert.That(application.IsAccounting, Is.True);
            Assert.That(type.Name, Is.EqualTo("Disconnect-Cause-Type"));
            Assert.That(type.VendorId, Is.EqualTo(10415U));
            Assert.That(type.BaseType, Is.EqualTo(DiameterAvpBaseType.Integer32));
            Assert.That(type.ParentTypeName, Is.EqualTo("Enumerated"));
            Assert.That(type.ParentTypeVendorId, Is.Zero);
            Assert.That(type.FixedDataLength, Is.EqualTo(4));
            Assert.That(enumValue.Name, Is.EqualTo("REBOOTING"));
            Assert.That(enumValue.Value, Is.Zero);
            Assert.That(enumValue.TypeName, Is.EqualTo(type.Name));
            Assert.That(enumValue.TypeVendorId, Is.EqualTo(type.VendorId));
            Assert.That(enumValue.AvpCode, Is.EqualTo(DiameterAvpCodes.DisconnectCause));
            Assert.That(enumValue.AvpVendorId, Is.Zero);
            Assert.That(rule.AvpCode, Is.EqualTo(DiameterAvpCodes.SessionId));
            Assert.That(rule.AvpVendorId, Is.Zero);
            Assert.That(rule.AvpName, Is.EqualTo("Session-Id"));
            Assert.That(rule.MinimumCount, Is.EqualTo(1));
            Assert.That(rule.MaximumCount, Is.EqualTo(-1));
            Assert.That(rule.FixedHeadPosition, Is.Zero);
            Assert.That(rule.OrderingGroup, Is.EqualTo(2));
            Assert.That(avp.Code, Is.EqualTo(DiameterAvpCodes.ProxyInfo));
            Assert.That(avp.VendorId, Is.Zero);
            Assert.That(avp.Name, Is.EqualTo("Proxy-Info"));
            Assert.That(avp.TypeName, Is.EqualTo("Grouped"));
            Assert.That(avp.BaseType, Is.EqualTo(DiameterAvpBaseType.Grouped));
            Assert.That(avp.RequiredFlags, Is.EqualTo(DiameterAvpFlags.Mandatory));
            Assert.That(avp.ProhibitedFlags, Is.EqualTo(DiameterAvpFlags.Protected));
            Assert.That(avp.FixedDataLength, Is.EqualTo(12));
            Assert.That(avp.GroupedRules, Is.EqualTo(new[] { rule }));
            Assert.That(avp.AllowUnlistedAvps, Is.False);
            Assert.That(command.Code, Is.EqualTo(DiameterCommandCodes.CreditControl));
            Assert.That(command.ApplicationId, Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(command.Name, Is.EqualTo("Credit-Control-Request"));
            Assert.That(command.IsRequest, Is.True);
            Assert.That(
                command.RequiredFlags,
                Is.EqualTo(DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable));
            Assert.That(command.ProhibitedFlags, Is.EqualTo(DiameterMessageFlags.Error));
            Assert.That(command.Rules, Is.EqualTo(new[] { rule }));
            Assert.That(command.AllowUnlistedAvps, Is.False);
        });
    }

    [Test]
    public void ModuleSnapshotAndOperationalEventRetainTheComposedGraph()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "rfc-6733";
        module.Version = "1.0";
        module.Dependencies.Add("base-types");
        DiameterVendorDefinition vendor = new DiameterVendorDefinition();
        DiameterApplicationDefinition application = new DiameterApplicationDefinition();
        DiameterTypeDefinition type = new DiameterTypeDefinition();
        DiameterEnumValueDefinition enumValue = new DiameterEnumValueDefinition();
        DiameterAvpDefinition avp = new DiameterAvpDefinition();
        DiameterCommandDefinition command = new DiameterCommandDefinition();
        module.Vendors.Add(vendor);
        module.Applications.Add(application);
        module.Types.Add(type);
        module.EnumValues.Add(enumValue);
        module.Avps.Add(avp);
        module.Commands.Add(command);

        DiameterDictionarySnapshot snapshot = new DiameterDictionarySnapshot();
        snapshot.Revision = 7L;
        snapshot.Modules.Add(module);
        snapshot.Vendors.Add(vendor);
        snapshot.Applications.Add(application);
        snapshot.Types.Add(type);
        snapshot.EnumValues.Add(enumValue);
        snapshot.Avps.Add(avp);
        snapshot.Commands.Add(command);
        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.DictionarySnapshot = snapshot;

        Assert.Multiple(() =>
        {
            Assert.That(module.Name, Is.EqualTo("rfc-6733"));
            Assert.That(module.Version, Is.EqualTo("1.0"));
            Assert.That(module.Dependencies, Is.EqualTo(new[] { "base-types" }));
            Assert.That(module.Vendors, Is.EqualTo(new[] { vendor }));
            Assert.That(module.Applications, Is.EqualTo(new[] { application }));
            Assert.That(module.Types, Is.EqualTo(new[] { type }));
            Assert.That(module.EnumValues, Is.EqualTo(new[] { enumValue }));
            Assert.That(module.Avps, Is.EqualTo(new[] { avp }));
            Assert.That(module.Commands, Is.EqualTo(new[] { command }));
            Assert.That(snapshot.Revision, Is.EqualTo(7L));
            Assert.That(snapshot.Modules, Is.EqualTo(new[] { module }));
            Assert.That(snapshot.Vendors, Is.EqualTo(new[] { vendor }));
            Assert.That(snapshot.Applications, Is.EqualTo(new[] { application }));
            Assert.That(snapshot.Types, Is.EqualTo(new[] { type }));
            Assert.That(snapshot.EnumValues, Is.EqualTo(new[] { enumValue }));
            Assert.That(snapshot.Avps, Is.EqualTo(new[] { avp }));
            Assert.That(snapshot.Commands, Is.EqualTo(new[] { command }));
            Assert.That(eventData.DictionarySnapshot, Is.SameAs(snapshot));
        });
    }
}
