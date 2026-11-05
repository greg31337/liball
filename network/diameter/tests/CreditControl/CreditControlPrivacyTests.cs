using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlPrivacyTests
{
    [Test]
    public void MinimizationRemovesOptionalSensitiveDataAndOwnsResult()
    {
        DiameterMessage source = CreatePrivacyRequest();
        CreditControlPrivacyPolicy policy = new CreditControlPrivacyPolicy();
        policy.Action = DiameterCreditControlPrivacyAction.Remove;

        ICreditControlPrivacyProcessor processor =
            new DiameterCreditControlPrivacyProcessor();
        DiameterMessage result = processor
            .ApplyDiameterCreditControlPrivacyPolicy(
                source,
                policy,
                DiameterAgentRole.None);

        Assert.Multiple(() =>
        {
            Assert.That(FindAvp(result, DiameterAvpCodes.SessionId),
                Is.Not.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.CcCorrelationId),
                Is.Not.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.DestinationRealm),
                Is.Not.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.ServiceContextId),
                Is.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.SubscriptionId),
                Is.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.UserEquipmentInfo),
                Is.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.ServiceParameterInfo),
                Is.Null);
            Assert.That(FindAvp(source, DiameterAvpCodes.ServiceContextId),
                Is.Not.Null);
            Assert.That(FindAvp(source, DiameterAvpCodes.SubscriptionId),
                Is.Not.Null);
            Assert.That(policy.Observed, Has.Count.GreaterThanOrEqualTo(6));
        });

        result.Avps[0].Data[0] = 99;
        Assert.That(source.Avps[0].Data[0], Is.Not.EqualTo(99));
    }

    [Test]
    public void AgentMinimizationPreservesRoutingAndCorrelationInputs()
    {
        DiameterMessage source = CreatePrivacyRequest();
        CreditControlPrivacyPolicy policy = new CreditControlPrivacyPolicy();
        policy.Action = DiameterCreditControlPrivacyAction.Remove;

        DiameterMessage result = new DiameterCreditControlPrivacyProcessor()
            .ApplyDiameterCreditControlPrivacyPolicy(
                source,
                policy,
                DiameterAgentRole.Relay);

        DiameterCreditControlAvpPrivacy subscription =
            FindObservation(policy, DiameterAvpCodes.SubscriptionId);
        DiameterCreditControlAvpPrivacy correlation =
            FindObservation(policy, DiameterAvpCodes.CcCorrelationId);
        DiameterCreditControlAvpPrivacy equipment =
            FindObservation(policy, DiameterAvpCodes.UserEquipmentInfo);
        Assert.Multiple(() =>
        {
            Assert.That(FindAvp(result, DiameterAvpCodes.ServiceContextId),
                Is.Not.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.SubscriptionId),
                Is.Not.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.CcCorrelationId),
                Is.Not.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.UserEquipmentInfo),
                Is.Null);
            Assert.That(FindAvp(result, DiameterAvpCodes.ServiceParameterInfo),
                Is.Null);
            Assert.That(subscription.Classification, Is.EqualTo(
                DiameterCreditControlPrivacyClass.SubscriberIdentity));
            Assert.That(subscription.RequiredForRouting, Is.True);
            Assert.That(correlation.Classification, Is.EqualTo(
                DiameterCreditControlPrivacyClass.Correlation));
            Assert.That(correlation.RequiredForCorrelation, Is.True);
            Assert.That(equipment.Classification, Is.EqualTo(
                DiameterCreditControlPrivacyClass.EquipmentIdentity));
            Assert.That(subscription.Sensitive, Is.True);
            Assert.That(correlation.Sensitive, Is.True);
            Assert.That(equipment.Sensitive, Is.True);
            Assert.That(equipment.RequiredForRouting, Is.False);
        });
    }

    [Test]
    public void RedactionKeepsGroupedAvpsValidAndReplacesTheirValues()
    {
        DiameterMessage source = CreatePrivacyRequest();
        CreditControlPrivacyPolicy policy = new CreditControlPrivacyPolicy();
        policy.Action = DiameterCreditControlPrivacyAction.Redact;

        DiameterMessage result = new DiameterCreditControlPrivacyProcessor()
            .ApplyDiameterCreditControlPrivacyPolicy(
                source,
                policy,
                DiameterAgentRole.None);
        DiameterCreditControlRequest parsed = new CreditControlCommandParser(
            CreateClock()).ParseCreditControlRequest(result);
        DiameterCreditControlRequest original = new CreditControlCommandParser(
            CreateClock()).ParseCreditControlRequest(source);
        DiameterValueResult parameter = AvpDecoder.DecodeAvpSequence(
            parsed.ServiceParameterAvps[0].Data);
        DiameterAvp parameterValue = FindChildAvp(
            parameter.Avps,
            DiameterAvpCodes.ServiceParameterValue);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.SessionId,
                Is.EqualTo("privacy-client.example;session;1"));
            Assert.That(parsed.CorrelationId,
                Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(parsed.ServiceContextId, Is.EqualTo("[redacted]"));
            Assert.That(parsed.SubscriptionIdentifiers[0].Data,
                Is.EqualTo("[redacted]"));
            Assert.That(parsed.UserEquipmentInformation!.Value,
                Is.EqualTo(new byte[] { 0 }));
            Assert.That(parameter.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(parameterValue.Data, Is.EqualTo(new byte[] { 0 }));
            Assert.That(original.ServiceContextId,
                Is.EqualTo("example.privacy"));
            Assert.That(original.SubscriptionIdentifiers[0].Data,
                Is.EqualTo("001010123456789"));
            Assert.That(original.UserEquipmentInformation!.Value,
                Is.EqualTo(new byte[] { 8, 6, 7, 5, 3, 0, 9 }));
        });
    }

    [Test]
    public void InvalidPolicyActionIsRejectedWithoutMutatingInput()
    {
        DiameterMessage source = CreatePrivacyRequest();
        CreditControlPrivacyPolicy policy = new CreditControlPrivacyPolicy();
        policy.Action = (DiameterCreditControlPrivacyAction)99;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiameterCreditControlPrivacyProcessor()
                .ApplyDiameterCreditControlPrivacyPolicy(
                    source,
                    policy,
                    DiameterAgentRole.None));
        Assert.That(FindAvp(source, DiameterAvpCodes.SubscriptionId),
            Is.Not.Null);
    }

    [Test]
    public void RedactionPreservesNestedUsageTypeAndWidth()
    {
        DiameterMessage source = CreatePrivacyRequest();
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreditControlCommandCodec
            .CreateCreditControlUnsigned64Avp(
                DiameterAvpCodes.CcTotalOctets,
                9876543210UL));
        source.Avps.Add(CreditControlCommandCodec
            .CreateCreditControlGroupedAvp(
                DiameterAvpCodes.UsedServiceUnit,
                children));
        CreditControlPrivacyPolicy policy = new CreditControlPrivacyPolicy();
        policy.Action = DiameterCreditControlPrivacyAction.Redact;

        DiameterMessage result = new DiameterCreditControlPrivacyProcessor()
            .ApplyDiameterCreditControlPrivacyPolicy(
                source,
                policy,
                DiameterAgentRole.None);
        DiameterAvp usage = FindAvp(
            result,
            DiameterAvpCodes.UsedServiceUnit)!;
        DiameterValueResult decoded = AvpDecoder.DecodeAvpSequence(usage.Data);
        DiameterAvp total = FindChildAvp(
            decoded.Avps,
            DiameterAvpCodes.CcTotalOctets);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(total.Data, Is.EqualTo(new byte[8]));
            Assert.That(FindAvp(
                source,
                DiameterAvpCodes.UsedServiceUnit), Is.Not.Null);
        });
    }

    [Test]
    public void PrivacyGuardsAndRedactionKindsCoverEveryBoundary()
    {
        DiameterCreditControlPrivacyProcessor processor =
            new DiameterCreditControlPrivacyProcessor();
        CreditControlPrivacyPolicy policy = new CreditControlPrivacyPolicy();
        DiameterMessage request = CreatePrivacyRequest();
        DiameterMessage wrongApplication = CreatePrivacyRequest();
        wrongApplication.ApplicationId = 1U;
        DiameterMessage wrongCommand = CreatePrivacyRequest();
        wrongCommand.CommandCode = 1U;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                processor.ApplyDiameterCreditControlPrivacyPolicy(
                    null, policy, DiameterAgentRole.None));
            Assert.Throws<ArgumentException>(() =>
                processor.ApplyDiameterCreditControlPrivacyPolicy(
                    request, null, DiameterAgentRole.None));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                processor.ApplyDiameterCreditControlPrivacyPolicy(
                    request, policy, (DiameterAgentRole)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                processor.ApplyDiameterCreditControlPrivacyPolicy(
                    request, policy, (DiameterAgentRole)5));
            Assert.Throws<ArgumentException>(() =>
                processor.ApplyDiameterCreditControlPrivacyPolicy(
                    wrongApplication, policy, DiameterAgentRole.None));
            Assert.Throws<ArgumentException>(() =>
                processor.ApplyDiameterCreditControlPrivacyPolicy(
                    wrongCommand, policy, DiameterAgentRole.None));
        });

        DiameterAvp vendor = new DiameterAvp();
        vendor.Code = DiameterAvpCodes.UserName;
        vendor.VendorId = 99U;
        Assert.That(processor.ClassifyCreditControlAvp(
            vendor,
            DiameterAgentRole.None).Sensitive, Is.False);
        Assert.That(processor.ClassifyCreditControlAvp(
            new DiameterAvp { Code = DiameterAvpCodes.ServiceIdentifier },
            DiameterAgentRole.Proxy).RequiredForRouting, Is.True);

        DiameterAvp redirectAddress = new DiameterAvp();
        redirectAddress.Code = DiameterAvpCodes.RedirectAddressIpAddress;
        redirectAddress.Data = new byte[] { 1 };
        Assert.That(processor.RedactCreditControlAvp(redirectAddress).Data,
            Is.EqualTo(new byte[] { 0, 1, 0, 0, 0, 0 }));

        uint[] numericCodes = new uint[]
        {
            DiameterAvpCodes.CheckBalanceResult,
            DiameterAvpCodes.CurrencyCode,
            DiameterAvpCodes.CcTime,
            DiameterAvpCodes.Exponent,
            DiameterAvpCodes.ServiceIdentifier,
            DiameterAvpCodes.RatingGroup
        };
        int index = 0;
        while (index < numericCodes.Length)
        {
            DiameterAvp numeric = new DiameterAvp();
            numeric.Code = numericCodes[index];
            numeric.Data = new byte[] { 1 };
            Assert.That(processor.RedactCreditControlAvp(numeric).Data,
                Is.EqualTo(new byte[4]));
            index = index + 1;
        }

        DiameterAvp malformedGrouped = new DiameterAvp();
        malformedGrouped.Code = DiameterAvpCodes.CostInformation;
        malformedGrouped.Data = new byte[] { 1 };
        Assert.Throws<DiameterCreditControlException>(() =>
            processor.RedactCreditControlAvp(malformedGrouped));

        policy.Action = (DiameterCreditControlPrivacyAction)(-1);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            processor.ApplyDiameterCreditControlPrivacyPolicy(
                request,
                policy,
                DiameterAgentRole.None));
    }

    private static DiameterMessage CreatePrivacyRequest()
    {
        DiameterCreditControlRequest request =
            new DiameterCreditControlRequest();
        request.SessionId = "privacy-client.example;session;1";
        request.DestinationRealm.Value = "privacy.example";
        request.ServiceContextId = "example.privacy";
        request.RequestType = DiameterCreditControlRequestType.Initial;
        request.CorrelationId = new byte[] { 4, 5, 6, 7 };
        DiameterSubscriptionIdentifier subscription =
            new DiameterSubscriptionIdentifier();
        subscription.Type = DiameterSubscriptionIdType.EndUserImsi;
        subscription.Data = "001010123456789";
        request.SubscriptionIdentifiers.Add(subscription);
        DiameterUserEquipmentInformation equipment =
            new DiameterUserEquipmentInformation();
        equipment.Type = DiameterUserEquipmentInfoType.Imeisv;
        equipment.Value = new byte[] { 8, 6, 7, 5, 3, 0, 9 };
        request.UserEquipmentInformation = equipment;
        request.ServiceParameterAvps.Add(CreateServiceParameter());
        DiameterIdentity host = new DiameterIdentity();
        host.Value = "privacy-client.example";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "example";
        return new CreditControlCommandFactory(
            new CreditControlCommandIdentifierSource(),
            host,
            realm).CreateCreditControlRequest(request);
    }

    private static DiameterAvp CreateServiceParameter()
    {
        List<DiameterAvp> children = new List<DiameterAvp>();
        DiameterAvp type = CreditControlCommandCodec
            .CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.ServiceParameterType,
                77U);
        type.Flags = DiameterAvpFlags.None;
        children.Add(type);
        DiameterAvp value = new DiameterAvp();
        value.Code = DiameterAvpCodes.ServiceParameterValue;
        value.Data = new byte[] { 1, 3, 3, 7 };
        children.Add(value);
        DiameterAvp result = CreditControlCommandCodec
            .CreateCreditControlGroupedAvp(
                DiameterAvpCodes.ServiceParameterInfo,
                children);
        result.Flags = DiameterAvpFlags.None;
        return result;
    }

    private static DiameterAvp? FindAvp(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                return message.Avps[index];
            }

            index = index + 1;
        }

        return null;
    }

    private static DiameterAvp FindChildAvp(
        List<DiameterAvp> avps,
        uint code)
    {
        int index = 0;
        while (index < avps.Count)
        {
            if (avps[index].Code == code)
            {
                return avps[index];
            }

            index = index + 1;
        }

        throw new AssertionException("Expected child AVP was not present.");
    }

    private static DiameterCreditControlAvpPrivacy FindObservation(
        CreditControlPrivacyPolicy policy,
        uint code)
    {
        int index = 0;
        while (index < policy.Observed.Count)
        {
            if (policy.Observed[index].AvpCode == code)
            {
                return policy.Observed[index];
            }

            index = index + 1;
        }

        throw new AssertionException(
            "Expected privacy observation was not present.");
    }

    private static TestDiameterClock CreateClock()
    {
        return new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                23,
                0,
                0,
                TimeSpan.Zero),
            0);
    }
}

internal class CreditControlPrivacyPolicy : ICreditControlPrivacyPolicy
{
    internal DiameterCreditControlPrivacyAction Action;
    internal List<DiameterCreditControlAvpPrivacy> Observed =
        new List<DiameterCreditControlAvpPrivacy>();

    public DiameterCreditControlPrivacyAction
        SelectDiameterCreditControlPrivacyAction(
            DiameterCreditControlAvpPrivacy privacy)
    {
        Observed.Add(privacy);
        return Action;
    }
}
