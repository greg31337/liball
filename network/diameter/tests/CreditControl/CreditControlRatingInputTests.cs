using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class CreditControlRatingInputTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void RatingIdentifiersAndParametersRoundTripInDictionaryValidCcr(
        bool equipmentExtension)
    {
        CreditControlCommandFactory factory = CreateFactory();
        CreditControlCommandParser parser = CreateParser();
        DiameterCreditControlRequest request = CreateRatingRequest();
        request.SubscriptionIdentifiers.Add(CreateSubscription(
            DiameterSubscriptionIdType.EndUserE164,
            "+12065550100",
            false));
        request.SubscriptionIdentifiers.Add(CreateSubscription(
            DiameterSubscriptionIdType.EndUserNai,
            "alice@example.net",
            true));
        request.UserEquipmentInformation = CreateEquipment(
            equipmentExtension);
        request.ServiceParameterAvps.Add(CreateServiceParameter());
        request.ServiceAvps.Add(CreateRatingAvp());

        DiameterMessage message = factory.CreateCreditControlRequest(
            request);
        DiameterValidationResult validation = new DiameterValidator()
            .ValidateMessage(
                message,
                new DiameterDictionary().ReadDictionarySnapshot(),
                new DiameterLimitConfiguration());
        DiameterCreditControlRequest parsed =
            parser.ParseCreditControlRequest(message);
        request.SubscriptionIdentifiers[0].Data = "changed";
        request.UserEquipmentInformation.Value[0] = 99;
        request.ServiceParameterAvps[0].Data[0] = 99;
        request.ServiceAvps[0].Data[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(validation.Status,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(parsed.CreditControlSubSessionIdPresent, Is.True);
            Assert.That(parsed.CreditControlSubSessionId,
                Is.EqualTo(44UL));
            Assert.That(parsed.CorrelationId,
                Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(parsed.SubscriptionIdentifiers,
                Has.Count.EqualTo(2));
            Assert.That(parsed.SubscriptionIdentifiers[0].Data,
                Is.EqualTo("+12065550100"));
            Assert.That(parsed.SubscriptionIdentifiers[0].Extension,
                Is.False);
            Assert.That(parsed.SubscriptionIdentifiers[1].Type,
                Is.EqualTo(DiameterSubscriptionIdType.EndUserNai));
            Assert.That(parsed.SubscriptionIdentifiers[1].Extension,
                Is.True);
            Assert.That(parsed.UserEquipmentInformation!.Extension,
                Is.EqualTo(equipmentExtension));
            Assert.That(parsed.UserEquipmentInformation.Imei,
                Is.EqualTo(equipmentExtension));
            Assert.That(parsed.UserEquipmentInformation.Value,
                Is.EqualTo(new byte[] { 8, 6, 7, 5, 3, 0, 9 }));
            Assert.That(parsed.ServiceParameterAvps,
                Has.Count.EqualTo(1));
            Assert.That(parsed.ServiceAvps, Has.Count.EqualTo(1));
            Assert.That(parsed.ServiceAvps[0].Data,
                Is.EqualTo(new byte[] { 10, 20, 30 }));
        });
    }

    [Test]
    public void ServerRatingHandoffOwnsEveryServiceSpecificInput()
    {
        DiameterCreditControlRequest request = CreateRatingRequest();
        request.SubscriptionIdentifiers.Add(CreateSubscription(
            DiameterSubscriptionIdType.EndUserImsi,
            "001010123456789",
            true));
        request.UserEquipmentInformation = CreateEquipment(true);
        request.ServiceParameterAvps.Add(CreateServiceParameter());
        request.ServiceAvps.Add(CreateRatingAvp());
        DiameterCreditControlRequest parsed = CreateParser()
            .ParseCreditControlRequest(
                CreateFactory().CreateCreditControlRequest(request));
        CreditControlRatingInputHandler handler =
            new CreditControlRatingInputHandler();
        CreditControlServerStateMachine server =
            new CreditControlServerStateMachine(
                new TestCreditControlStore(),
                handler,
                CreateClock());

        DiameterCreditControlResult result =
            server.ProcessCreditControlServerRequest(parsed);
        DiameterCreditControlRequest received = handler.Received!;
        DiameterCreditControlSession session = result.Session!;

        Assert.Multiple(() =>
        {
            Assert.That(received.CreditControlSubSessionId,
                Is.EqualTo(44UL));
            Assert.That(received.CorrelationId,
                Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(received.SubscriptionIdentifiers[0].Data,
                Is.EqualTo("001010123456789"));
            Assert.That(received.UserEquipmentInformation!.Imei, Is.True);
            Assert.That(received.ServiceParameterAvps,
                Has.Count.EqualTo(1));
            Assert.That(received.ServiceAvps[0].Data,
                Is.EqualTo(new byte[] { 10, 20, 30 }));
            Assert.That(session.CreditControlSubSessionId,
                Is.EqualTo(44UL));
            Assert.That(session.CorrelationId,
                Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(session.SubscriptionIdentifiers[0].Data,
                Is.EqualTo("001010123456789"));
            Assert.That(session.UserEquipmentInformation!.Value,
                Is.EqualTo(new byte[] { 8, 6, 7, 5, 3, 0, 9 }));
            Assert.That(result.Request.ServiceAvps[0].Data,
                Is.EqualTo(new byte[] { 10, 20, 30 }));
        });
    }

    [Test]
    public void MalformedServiceParameterAndExtensionGroupsAreRejected()
    {
        DiameterCreditControlRequest request = CreateRatingRequest();
        List<DiameterAvp> incompleteChildren = new List<DiameterAvp>();
        incompleteChildren.Add(CreditControlCommandCodec
            .CreateCreditControlUnsigned32ValueAvp(
                DiameterAvpCodes.ServiceParameterType,
                7U));
        request.ServiceParameterAvps.Add(CreditControlCommandCodec
            .CreateCreditControlGroupedAvp(
                DiameterAvpCodes.ServiceParameterInfo,
                incompleteChildren));
        List<DiameterAvp> duplicateChildren = new List<DiameterAvp>();
        DiameterAvp first = new DiameterBaseAvpFactory().CreateTextAvp(
            DiameterAvpCodes.SubscriptionIdNai,
            "first@example.net");
        first.Flags = DiameterAvpFlags.None;
        duplicateChildren.Add(first);
        DiameterAvp second = new DiameterBaseAvpFactory().CreateTextAvp(
            DiameterAvpCodes.SubscriptionIdNai,
            "second@example.net");
        second.Flags = DiameterAvpFlags.None;
        duplicateChildren.Add(second);
        DiameterAvp duplicateExtension = CreditControlCommandCodec
            .CreateCreditControlGroupedAvp(
                DiameterAvpCodes.SubscriptionIdExtension,
                duplicateChildren);

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterCreditControlException>(() =>
                CreateFactory().CreateCreditControlRequest(request));
            Assert.Throws<DiameterCreditControlException>(() =>
                CreateParser().ReadCreditControlSubscriptionAvp(
                    duplicateExtension));
        });
    }

    private static DiameterCreditControlRequest CreateRatingRequest()
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "rating-client.example;credit;1";
        result.DestinationRealm.Value = "rating.example";
        result.ServiceContextId = "example.rating";
        result.RequestType = DiameterCreditControlRequestType.Initial;
        result.CreditControlSubSessionId = 44UL;
        result.CreditControlSubSessionIdPresent = true;
        result.CorrelationId = new byte[] { 4, 5, 6, 7 };
        return result;
    }

    private static DiameterSubscriptionIdentifier CreateSubscription(
        DiameterSubscriptionIdType type,
        string data,
        bool extension)
    {
        DiameterSubscriptionIdentifier result =
            new DiameterSubscriptionIdentifier();
        result.Type = type;
        result.Data = data;
        result.Extension = extension;
        return result;
    }

    private static DiameterUserEquipmentInformation CreateEquipment(
        bool extension)
    {
        DiameterUserEquipmentInformation result =
            new DiameterUserEquipmentInformation();
        result.Type = DiameterUserEquipmentInfoType.Imeisv;
        result.Value = new byte[] { 8, 6, 7, 5, 3, 0, 9 };
        result.Extension = extension;
        result.Imei = extension;
        return result;
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

    private static DiameterAvp CreateRatingAvp()
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = 9001U;
        result.VendorId = 4242U;
        result.Flags = DiameterAvpFlags.VendorSpecific;
        result.Data = new byte[] { 10, 20, 30 };
        return result;
    }

    private static CreditControlCommandFactory CreateFactory()
    {
        DiameterIdentity host = new DiameterIdentity();
        host.Value = "rating-client.example";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "example";
        return new CreditControlCommandFactory(
            new CreditControlCommandIdentifierSource(),
            host,
            realm);
    }

    private static CreditControlCommandParser CreateParser()
    {
        return new CreditControlCommandParser(CreateClock());
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

internal class CreditControlRatingInputHandler :
    ICreditControlRequestHandler
{
    internal DiameterCreditControlRequest? Received;

    public DiameterCreditControlAnswer HandleDiameterCreditControlRequest(
        DiameterCreditControlRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("A rating request is required.");
        }

        Received = CreditControlDataCloner.CloneCreditControlRequest(
            request);
        request.CorrelationId[0] = 99;
        request.SubscriptionIdentifiers[0].Data = "mutated";
        request.UserEquipmentInformation!.Value[0] = 99;
        request.ServiceParameterAvps[0].Data[0] = 99;
        request.ServiceAvps[0].Data[0] = 99;
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = request.SessionId;
        result.RequestType = request.RequestType;
        result.RequestNumber = request.RequestNumber;
        result.ResultCode = DiameterResultCodes.Success;
        result.Disposition = DiameterCreditControlDisposition.Granted;
        return result;
    }
}
