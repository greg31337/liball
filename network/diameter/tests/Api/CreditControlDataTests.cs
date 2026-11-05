using System;

namespace diameter.Tests;

public class CreditControlDataTests
{
    [Test]
    public void CreditControlDefaultsAreSafeAndIndependentlyOwned()
    {
        DiameterCreditControlRequest firstRequest =
            new DiameterCreditControlRequest();
        DiameterCreditControlRequest secondRequest =
            new DiameterCreditControlRequest();
        DiameterCreditControlAnswer firstAnswer =
            new DiameterCreditControlAnswer();
        DiameterCreditControlAnswer secondAnswer =
            new DiameterCreditControlAnswer();
        DiameterCreditControlSession firstSession =
            new DiameterCreditControlSession();
        DiameterCreditControlSession secondSession =
            new DiameterCreditControlSession();
        DiameterMultipleServiceCredit firstCredit =
            new DiameterMultipleServiceCredit();
        DiameterMultipleServiceCredit secondCredit =
            new DiameterMultipleServiceCredit();
        DiameterFinalUnitInstruction firstFinal =
            new DiameterFinalUnitInstruction();
        DiameterFinalUnitInstruction secondFinal =
            new DiameterFinalUnitInstruction();
        DiameterUserEquipmentInformation firstEquipment =
            new DiameterUserEquipmentInformation();
        DiameterUserEquipmentInformation secondEquipment =
            new DiameterUserEquipmentInformation();
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();

        firstRequest.ServiceIdentifiers.Add(1U);
        firstRequest.CorrelationId = new byte[] { 1 };
        firstAnswer.MultipleServiceCredits.Add(firstCredit);
        firstSession.PoolReferences.Add(new DiameterPoolReference());
        firstCredit.ServiceIdentifiers.Add(2U);
        firstFinal.FilterIdentifiers.Add("sample-filter");
        firstEquipment.Value = new byte[] { 3 };

        Assert.Multiple(() =>
        {
            Assert.That(firstRequest.RequestType,
                Is.EqualTo(DiameterCreditControlRequestType.Initial));
            Assert.That(firstRequest.TimeoutMilliseconds,
                Is.EqualTo(30000));
            Assert.That(firstRequest.AllowFailover, Is.True);
            Assert.That(secondRequest.ServiceIdentifiers, Is.Empty);
            Assert.That(secondRequest.CorrelationId, Is.Empty);
            Assert.That(firstAnswer.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(secondAnswer.MultipleServiceCredits, Is.Empty);
            Assert.That(firstSession.State,
                Is.EqualTo(DiameterCreditControlState.Idle));
            Assert.That(secondSession.PoolReferences, Is.Empty);
            Assert.That(secondCredit.ServiceIdentifiers, Is.Empty);
            Assert.That(secondFinal.FilterIdentifiers, Is.Empty);
            Assert.That(secondEquipment.Value, Is.Empty);
            Assert.That(result.Request,
                Is.Not.SameAs(firstRequest));
            Assert.That(result.Answer, Is.Null);
            Assert.That(result.Session, Is.Null);
            Assert.That(result.Disposition,
                Is.EqualTo(DiameterCreditControlDisposition.Granted));
        });
    }

    [Test]
    public void GroupedCreditDataValidatesUnitsPoolsFinalActionsAndIdentityData()
    {
        DiameterCostInformation cost = CreateCostInformation();
        DiameterServiceUnit unit = CreateServiceUnit();
        DiameterPoolReference pool = CreatePoolReference();
        DiameterSubscriptionIdentifier subscription =
            CreateSubscriptionIdentifier();
        DiameterUserEquipmentInformation equipment =
            CreateEquipmentInformation();
        DiameterFinalUnitInstruction finalUnit =
            CreateRedirectFinalUnit();
        DiameterMultipleServiceCredit multiple =
            CreateMultipleServiceCredit();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(
                cost.ValidateCreditControlCostInformation);
            Assert.DoesNotThrow(
                unit.ValidateCreditControlServiceUnit);
            Assert.DoesNotThrow(
                pool.ValidateCreditControlPoolReference);
            Assert.DoesNotThrow(
                subscription
                    .ValidateCreditControlSubscriptionIdentifier);
            Assert.DoesNotThrow(
                equipment
                    .ValidateCreditControlUserEquipmentInformation);
            Assert.DoesNotThrow(
                finalUnit.ValidateCreditControlFinalUnitInstruction);
            Assert.DoesNotThrow(
                multiple.ValidateCreditControlMultipleServiceCredit);
        });

        DiameterCostInformation invalidCurrency =
            CreateCostInformation();
        invalidCurrency.CurrencyCode = 1000U;
        DiameterServiceUnit invalidTariff = CreateServiceUnit();
        invalidTariff.TariffChangeUsage =
            (DiameterTariffChangeUsage)99;
        DiameterPoolReference invalidPool = CreatePoolReference();
        invalidPool.UnitType = (DiameterCreditControlUnitType)99;
        DiameterSubscriptionIdentifier invalidSubscription =
            CreateSubscriptionIdentifier();
        invalidSubscription.Data = string.Empty;
        DiameterUserEquipmentInformation invalidEquipment =
            CreateEquipmentInformation();
        invalidEquipment.Imei = true;
        invalidEquipment.Extension = false;
        DiameterFinalUnitInstruction invalidFinal =
            CreateRedirectFinalUnit();
        invalidFinal.RedirectAddress = string.Empty;
        DiameterMultipleServiceCredit invalidMultiple =
            CreateMultipleServiceCredit();
        invalidMultiple.ValidityTimeMilliseconds = 0;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidCurrency.ValidateCreditControlCostInformation);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidTariff.ValidateCreditControlServiceUnit);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidPool.ValidateCreditControlPoolReference);
            Assert.Throws<ArgumentException>(
                invalidSubscription
                    .ValidateCreditControlSubscriptionIdentifier);
            Assert.Throws<ArgumentException>(
                invalidEquipment
                    .ValidateCreditControlUserEquipmentInformation);
            Assert.Throws<ArgumentException>(
                invalidFinal.ValidateCreditControlFinalUnitInstruction);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidMultiple
                    .ValidateCreditControlMultipleServiceCredit);
        });
    }

    [Test]
    public void CreditControlRequestValidatesTypeCorrelationAndOwnedGroups()
    {
        DiameterCreditControlRequest valid = CreateValidRequest();
        valid.RequestedServiceUnit = CreateServiceUnit();
        valid.UsedServiceUnits.Add(CreateServiceUnit());
        valid.MultipleServicesIndicatorPresent = true;
        valid.MultipleServicesIndicator =
            DiameterMultipleServicesIndicator.Supported;
        valid.MultipleServiceCredits.Add(
            CreateMultipleServiceCredit());
        valid.SubscriptionIdentifiers.Add(
            CreateSubscriptionIdentifier());
        valid.UserEquipmentInformation =
            CreateEquipmentInformation();
        valid.ServiceParameterAvps.Add(new DiameterAvp());
        valid.ServiceAvps.Add(new DiameterAvp());
        valid.EventTimestampPresent = true;
        valid.EventTimestamp = DateTimeOffset.FromUnixTimeSeconds(1000);

        DiameterCreditControlRequest eventRequest =
            CreateValidRequest();
        eventRequest.RequestType =
            DiameterCreditControlRequestType.Event;
        eventRequest.RequestedActionPresent = true;
        eventRequest.RequestedAction =
            DiameterRequestedAction.DirectDebiting;
        DiameterCreditControlRequest terminationRequest =
            CreateValidRequest();
        terminationRequest.RequestType =
            DiameterCreditControlRequestType.Termination;
        terminationRequest.RequestNumber = 2U;
        terminationRequest.TerminationCausePresent = true;
        terminationRequest.TerminationCause =
            DiameterTerminationCause.Logout;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(valid.ValidateCreditControlRequest);
            Assert.DoesNotThrow(
                eventRequest.ValidateCreditControlRequest);
            Assert.DoesNotThrow(
                terminationRequest.ValidateCreditControlRequest);
        });

        DiameterCreditControlRequest invalidNumber =
            CreateValidRequest();
        invalidNumber.RequestNumber = 1U;
        DiameterCreditControlRequest invalidAction =
            CreateValidRequest();
        invalidAction.RequestedActionPresent = true;
        DiameterCreditControlRequest invalidCause =
            CreateValidRequest();
        invalidCause.TerminationCausePresent = true;
        DiameterCreditControlRequest invalidRealm =
            CreateValidRequest();
        invalidRealm.DestinationRealm.Value = "bad realm";
        DiameterCreditControlRequest invalidAvp =
            CreateValidRequest();
        DiameterAvp missingPayload = new DiameterAvp();
        missingPayload.Data = null!;
        invalidAvp.ServiceAvps.Add(missingPayload);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidNumber.ValidateCreditControlRequest);
            Assert.Throws<ArgumentException>(
                invalidAction.ValidateCreditControlRequest);
            Assert.Throws<ArgumentException>(
                invalidCause.ValidateCreditControlRequest);
            Assert.Throws<ArgumentException>(
                invalidRealm.ValidateCreditControlRequest);
            Assert.Throws<ArgumentException>(
                invalidAvp.ValidateCreditControlRequest);
        });
    }

    [Test]
    public void CreditControlAnswerValidatesPolicyAndDecisionGroups()
    {
        DiameterCreditControlAnswer valid = CreateValidAnswer();
        valid.GrantedServiceUnit = CreateServiceUnit();
        valid.MultipleServiceCredits.Add(
            CreateMultipleServiceCredit());
        valid.CostInformation = CreateCostInformation();
        valid.FinalUnitInstruction = CreateRedirectFinalUnit();
        valid.QosFinalUnitInstruction =
            CreateQosFinalUnit();
        valid.CheckBalanceResultPresent = true;
        valid.CheckBalanceResult =
            DiameterCheckBalanceResult.EnoughCredit;
        valid.FailureHandlingPresent = true;
        valid.FailureHandling =
            DiameterCreditControlFailureHandling.RetryAndTerminate;
        valid.DirectDebitFailureHandlingPresent = true;
        valid.DirectDebitFailureHandling =
            DiameterDirectDebitFailureHandling.Continue;
        valid.ValidityTimePresent = true;
        valid.ValidityTimeMilliseconds = 60000;
        valid.RedirectHostUsagePresent = true;
        valid.RedirectHostUsage = DiameterRedirectHostUsage.AllRealm;
        valid.RedirectMaxCacheTimePresent = true;
        valid.RedirectMaxCacheTimeMilliseconds = 30000;
        DiameterUri redirect = new DiameterUri();
        redirect.Host.Value = "redirect.remote.example";
        valid.RedirectHosts.Add(redirect);
        valid.ServiceAvps.Add(new DiameterAvp());
        valid.FailedAvps.Add(new DiameterAvp());
        valid.Message = new DiameterMessage();

        Assert.DoesNotThrow(valid.ValidateCreditControlAnswer);

        DiameterCreditControlAnswer invalidResult =
            CreateValidAnswer();
        invalidResult.ResultCode = 999U;
        DiameterCreditControlAnswer invalidValidity =
            CreateValidAnswer();
        invalidValidity.ValidityTimePresent = true;
        DiameterCreditControlAnswer invalidCost =
            CreateValidAnswer();
        invalidCost.CostInformation = CreateCostInformation();
        invalidCost.CostInformation.CurrencyCodePresent = false;
        DiameterCreditControlAnswer invalidQos =
            CreateValidAnswer();
        invalidQos.QosFinalUnitInstruction =
            CreateQosFinalUnit();
        invalidQos.QosFinalUnitInstruction.Qos = false;
        DiameterCreditControlAnswer invalidMessage =
            CreateValidAnswer();
        invalidMessage.Message = new DiameterMessage();
        invalidMessage.Message.Avps = null!;
        DiameterCreditControlAnswer invalidRedirectCache =
            CreateValidAnswer();
        invalidRedirectCache.RedirectMaxCacheTimePresent = true;
        invalidRedirectCache.RedirectMaxCacheTimeMilliseconds = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidResult.ValidateCreditControlAnswer);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidValidity.ValidateCreditControlAnswer);
            Assert.Throws<ArgumentException>(
                invalidCost.ValidateCreditControlAnswer);
            Assert.Throws<ArgumentException>(
                invalidQos.ValidateCreditControlAnswer);
            Assert.Throws<ArgumentException>(
                invalidMessage.ValidateCreditControlAnswer);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidRedirectCache.ValidateCreditControlAnswer);
        });
    }

    [Test]
    public void CreditControlSessionAndResultValidatePersistedOutcomes()
    {
        DateTimeOffset created = DateTimeOffset.FromUnixTimeSeconds(1000);
        DiameterCreditControlSession session =
            CreateValidSession(created);
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request = CreateValidRequest();
        result.Answer = CreateValidAnswer();
        result.Session = session;
        result.State = DiameterCreditControlState.Open;
        result.Disposition =
            DiameterCreditControlDisposition.PartiallyGranted;
        result.Accepted = true;
        result.AnswerReceived = true;
        result.ContinueService = true;
        result.ResultCode = DiameterResultCodes.Success;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(
                session.ValidateCreditControlSession);
            Assert.DoesNotThrow(
                result.ValidateCreditControlResult);
        });

        DiameterCreditControlSession invalidTime =
            CreateValidSession(created);
        invalidTime.UpdatedAt = created.AddSeconds(-1);
        DiameterCreditControlSession invalidDeadline =
            CreateValidSession(created);
        invalidDeadline.PendingRequestPresent = false;
        invalidDeadline.TransactionDeadlinePresent = true;
        DiameterCreditControlSession invalidCollection =
            CreateValidSession(created);
        invalidCollection.PoolReferences = null!;
        DiameterCreditControlResult missingAnswer =
            new DiameterCreditControlResult();
        missingAnswer.Request = CreateValidRequest();
        missingAnswer.AnswerReceived = true;
        DiameterCreditControlResult conflictingService =
            new DiameterCreditControlResult();
        conflictingService.Request = CreateValidRequest();
        conflictingService.ContinueService = true;
        conflictingService.TerminateService = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                invalidTime.ValidateCreditControlSession);
            Assert.Throws<ArgumentException>(
                invalidDeadline.ValidateCreditControlSession);
            Assert.Throws<ArgumentException>(
                invalidCollection.ValidateCreditControlSession);
            Assert.Throws<ArgumentException>(
                missingAnswer.ValidateCreditControlResult);
            Assert.Throws<ArgumentException>(
                conflictingService.ValidateCreditControlResult);
        });
    }

    [Test]
    public void GroupedCreditDataRejectsEveryInvalidBoundaryAndShape()
    {
        AssertEveryMutationIsRejected(
            CreateServiceUnit,
            value => value.ValidateCreditControlServiceUnit(),
            value => value.TariffChangeUsage =
                (DiameterTariffChangeUsage)(-1),
            value => value.TariffChangeUsage =
                (DiameterTariffChangeUsage)3,
            value => value.TariffChangeTime = default(DateTimeOffset));

        AssertEveryMutationIsRejected(
            CreateCostInformation,
            value => value.ValidateCreditControlCostInformation(),
            value => value.CostUnit = null!,
            value => value.CostUnit = "bad\ud800unit",
            value => value.CurrencyCode = 0U,
            value => value.CurrencyCode = 1000U);

        AssertEveryMutationIsRejected(
            CreatePoolReference,
            value => value.ValidateCreditControlPoolReference(),
            value => value.UnitType =
                (DiameterCreditControlUnitType)(-1),
            value => value.UnitType =
                (DiameterCreditControlUnitType)6);

        AssertEveryMutationIsRejected(
            CreateSubscriptionIdentifier,
            value => value.ValidateCreditControlSubscriptionIdentifier(),
            value => value.Type = (DiameterSubscriptionIdType)(-1),
            value => value.Type = (DiameterSubscriptionIdType)6,
            value => value.Data = null!,
            value => value.Data = string.Empty,
            value => value.Data = "bad\ud800subscriber");

        AssertEveryMutationIsRejected(
            CreateEquipmentInformation,
            value => value.ValidateCreditControlUserEquipmentInformation(),
            value => value.Type = (DiameterUserEquipmentInfoType)(-1),
            value => value.Type = (DiameterUserEquipmentInfoType)5,
            value => value.Value = null!,
            value => value.Value = new byte[0],
            value =>
            {
                value.Imei = true;
                value.Extension = false;
            },
            value =>
            {
                value.Imei = true;
                value.Extension = true;
                value.Type = DiameterUserEquipmentInfoType.Mac;
            });

        DiameterUserEquipmentInformation imei =
            CreateEquipmentInformation();
        imei.Extension = true;
        imei.Imei = true;

        Assert.DoesNotThrow(
            imei.ValidateCreditControlUserEquipmentInformation);
    }

    [Test]
    public void FinalUnitInstructionRejectsEveryInvalidActionAndPayload()
    {
        AssertEveryMutationIsRejected(
            CreateRedirectFinalUnit,
            value => value.ValidateCreditControlFinalUnitInstruction(),
            value => value.Action = (DiameterFinalUnitAction)(-1),
            value => value.Action = (DiameterFinalUnitAction)3,
            value => value.RedirectAddressType =
                (DiameterRedirectAddressType)(-1),
            value => value.RedirectAddressType =
                (DiameterRedirectAddressType)6,
            value => value.RestrictionFilterRules = null!,
            value => value.FilterIdentifiers = null!,
            value => value.RedirectAddress = null!,
            value => value.RedirectAddress = "bad\ud800address",
            value => value.RedirectAddress = string.Empty,
            value =>
            {
                value.RedirectExtension = true;
                value.RedirectAddressTypePresent = true;
            },
            value =>
            {
                value.RedirectExtension = true;
                value.RedirectAddressTypePresent = false;
                value.Qos = false;
            },
            value =>
            {
                value.RedirectExtension = false;
                value.Qos = true;
            },
            value =>
            {
                value.Action = DiameterFinalUnitAction.Terminate;
                value.RedirectAddressTypePresent = true;
            },
            value =>
            {
                value.Action = DiameterFinalUnitAction.Terminate;
                value.RedirectAddressTypePresent = false;
                value.RedirectAddress = "unexpected";
            },
            value =>
            {
                value.Action = DiameterFinalUnitAction.Terminate;
                value.RedirectAddressTypePresent = false;
                value.RedirectAddress = string.Empty;
                value.RedirectExtension = true;
            });

        AssertEveryMutationIsRejected(
            CreateQosFinalUnit,
            value => value.ValidateCreditControlFinalUnitInstruction(),
            value => value.RestrictionFilterRules.Add(null!),
            value => value.RestrictionFilterRules.Add(string.Empty),
            value => value.RestrictionFilterRules.Add("bad\ud800rule"),
            value => value.FilterIdentifiers[0] = null!,
            value => value.FilterIdentifiers[0] = string.Empty,
            value => value.FilterIdentifiers[0] = "bad\ud800filter",
            value =>
            {
                value.RestrictionFilterRules.Clear();
                value.FilterIdentifiers.Clear();
            });

        DiameterFinalUnitInstruction extensionRedirect =
            CreateRedirectFinalUnit();
        extensionRedirect.RedirectAddressTypePresent = false;
        extensionRedirect.RedirectExtension = true;
        extensionRedirect.Qos = true;

        Assert.DoesNotThrow(
            extensionRedirect.ValidateCreditControlFinalUnitInstruction);
    }

    [Test]
    public void MultipleServiceCreditRejectsEveryInvalidOwnedValue()
    {
        AssertEveryMutationIsRejected(
            CreateMultipleServiceCredit,
            value => value.ValidateCreditControlMultipleServiceCredit(),
            value => value.UsedUnits = null!,
            value => value.ServiceIdentifiers = null!,
            value => value.PoolReferences = null!,
            value => value.UsedUnits.Add(null!),
            value => value.PoolReferences.Add(null!),
            value => value.TariffChangeUsage =
                (DiameterTariffChangeUsage)(-1),
            value => value.TariffChangeUsage =
                (DiameterTariffChangeUsage)3,
            value => value.Disposition =
                (DiameterCreditControlDisposition)(-1),
            value => value.Disposition =
                (DiameterCreditControlDisposition)6,
            value => value.ValidityTimeMilliseconds = 0,
            value => value.ValidityTimeMilliseconds = 1,
            value =>
            {
                value.ValidityTimePresent = false;
                value.ValidityDeadlinePresent = true;
                value.ValidityDeadline = default(DateTimeOffset);
            },
            value =>
            {
                value.ResultCodePresent = true;
                value.ResultCode = 999U;
            },
            value =>
            {
                value.ResultCodePresent = true;
                value.ResultCode = 6000U;
            },
            value => value.QosFinalUnitInstruction!.Qos = false);

        DiameterMultipleServiceCredit withThreshold =
            CreateMultipleServiceCredit();
        withThreshold.QuotaThreshold = CreateServiceUnit();
        withThreshold.ValidityDeadlinePresent = true;
        withThreshold.ValidityDeadline =
            DateTimeOffset.FromUnixTimeSeconds(3000);

        Assert.DoesNotThrow(
            withThreshold.ValidateCreditControlMultipleServiceCredit);
    }

    [Test]
    public void CreditControlServerAndPrivacyRejectEveryInvalidShape()
    {
        AssertEveryMutationIsRejected(
            CreateCreditControlServer,
            value => value.ValidateCreditControlServer(),
            value => value.RouteIdentifier = null!,
            value => value.RouteIdentifier = "bad\ud800route",
            value =>
            {
                value.DestinationHost = null;
                value.PeerIdentity = null;
                value.RouteIdentifier = string.Empty;
            },
            value => value.DestinationHost!.Value = "bad host",
            value => value.PeerIdentity = CreateIdentity("bad peer"));

        AssertEveryMutationIsRejected(
            CreatePrivacy,
            value => value.ValidateCreditControlAvpPrivacy(),
            value => value.Classification =
                (DiameterCreditControlPrivacyClass)(-1),
            value => value.Classification =
                (DiameterCreditControlPrivacyClass)7,
            value => value.AgentRole = (DiameterAgentRole)(-1),
            value => value.AgentRole = (DiameterAgentRole)6,
            value =>
            {
                value.Sensitive = false;
                value.Classification =
                    DiameterCreditControlPrivacyClass.SubscriberIdentity;
            },
            value =>
            {
                value.Sensitive = true;
                value.Classification =
                    DiameterCreditControlPrivacyClass.None;
            });
    }

    [Test]
    public void CreditControlRequestRejectsEveryInvalidBoundaryAndOwnedValue()
    {
        AssertEveryMutationIsRejected(
            CreateValidRequest,
            value => value.ValidateCreditControlRequest(),
            value => value.RequestType =
                (DiameterCreditControlRequestType)(-1),
            value => value.RequestType =
                (DiameterCreditControlRequestType)5,
            value =>
            {
                value.RequestType = DiameterCreditControlRequestType.Event;
                value.RequestedActionPresent = true;
                value.RequestedAction = (DiameterRequestedAction)(-1);
            },
            value =>
            {
                value.RequestType = DiameterCreditControlRequestType.Event;
                value.RequestedActionPresent = true;
                value.RequestedAction = (DiameterRequestedAction)5;
            },
            value =>
            {
                value.MultipleServicesIndicatorPresent = true;
                value.MultipleServicesIndicator =
                    (DiameterMultipleServicesIndicator)(-1);
            },
            value =>
            {
                value.MultipleServicesIndicatorPresent = true;
                value.MultipleServicesIndicator =
                    (DiameterMultipleServicesIndicator)2;
            },
            value =>
            {
                value.RequestType =
                    DiameterCreditControlRequestType.Termination;
                value.TerminationCausePresent = true;
                value.TerminationCause = (DiameterTerminationCause)(-1);
            },
            value =>
            {
                value.RequestType =
                    DiameterCreditControlRequestType.Termination;
                value.TerminationCausePresent = true;
                value.TerminationCause = (DiameterTerminationCause)9;
            },
            value => value.FailureHandling =
                (DiameterCreditControlFailureHandling)(-1),
            value => value.FailureHandling =
                (DiameterCreditControlFailureHandling)3,
            value => value.DirectDebitFailureHandling =
                (DiameterDirectDebitFailureHandling)(-1),
            value => value.DirectDebitFailureHandling =
                (DiameterDirectDebitFailureHandling)2,
            value => value.SessionId = null!,
            value => value.SessionId = string.Empty,
            value => value.ServiceContextId = null!,
            value => value.ServiceContextId = string.Empty,
            value => value.UserName = null!,
            value => value.AccountingMultiSessionId = null!,
            value => value.ServiceIdentifiers = null!,
            value => value.UsedServiceUnits = null!,
            value => value.MultipleServiceCredits = null!,
            value => value.ServiceParameterAvps = null!,
            value => value.CorrelationId = null!,
            value => value.SubscriptionIdentifiers = null!,
            value => value.ServiceAvps = null!,
            value => value.AlternateServers = null!,
            value => value.RouteIdentifier = null!,
            value => value.CorrelationText = null!,
            value => value.ApplicationState = null!,
            value => value.SessionId = "bad\ud800session",
            value => value.ServiceContextId = "bad\ud800context",
            value => value.UserName = "bad\ud800user",
            value => value.AccountingMultiSessionId = "bad\ud800accounting",
            value => value.RouteIdentifier = "bad\ud800route",
            value => value.CorrelationText = "bad\ud800correlation",
            value => value.DestinationRealm = null!,
            value => value.DestinationRealm!.Value = "bad realm",
            value => value.DestinationHost = CreateIdentity("bad host"),
            value => value.PeerIdentity = CreateIdentity("bad peer"),
            value => value.TimeoutMilliseconds = 0,
            value => value.RequestNumber = 1U,
            value =>
            {
                value.RequestType = DiameterCreditControlRequestType.Event;
                value.RequestedActionPresent = true;
                value.RequestNumber = 1U;
            },
            value =>
            {
                value.RequestType = DiameterCreditControlRequestType.Event;
                value.RequestedActionPresent = false;
            },
            value => value.RequestedActionPresent = true,
            value => value.TerminationCausePresent = true,
            value =>
            {
                value.EventTimestampPresent = true;
                value.EventTimestamp = default(DateTimeOffset);
            },
            value => value.UsedServiceUnits.Add(null!),
            value => value.MultipleServiceCredits.Add(null!),
            value => value.SubscriptionIdentifiers.Add(null!),
            value => value.ServiceParameterAvps.Add(null!),
            value =>
            {
                DiameterAvp avp = new DiameterAvp();
                avp.Data = null!;
                value.ServiceParameterAvps.Add(avp);
            },
            value => value.ServiceAvps.Add(null!),
            value => value.AlternateServers.Add(null!),
            value => value.AlternateServers.Add(
                CreateInvalidCreditControlServer()),
            value =>
            {
                value.UserEquipmentInformation =
                    CreateEquipmentInformation();
                value.UserEquipmentInformation.Value = null!;
            });

        DiameterCreditControlRequest complete = CreateValidRequest();
        complete.QuotaThreshold = CreateServiceUnit();
        complete.AlternateServers.Add(CreateCreditControlServer());

        Assert.DoesNotThrow(complete.ValidateCreditControlRequest);
    }

    [Test]
    public void CreditControlAnswerRejectsEveryInvalidBoundaryAndOwnedValue()
    {
        AssertEveryMutationIsRejected(
            CreateValidAnswer,
            value => value.ValidateCreditControlAnswer(),
            value => value.RequestType =
                (DiameterCreditControlRequestType)(-1),
            value => value.RequestType =
                (DiameterCreditControlRequestType)5,
            value => value.ResultCode = 999U,
            value => value.ResultCode = 6000U,
            value =>
            {
                value.CheckBalanceResultPresent = true;
                value.CheckBalanceResult =
                    (DiameterCheckBalanceResult)(-1);
            },
            value =>
            {
                value.CheckBalanceResultPresent = true;
                value.CheckBalanceResult =
                    (DiameterCheckBalanceResult)2;
            },
            value =>
            {
                value.FailureHandlingPresent = true;
                value.FailureHandling =
                    (DiameterCreditControlFailureHandling)(-1);
            },
            value =>
            {
                value.FailureHandlingPresent = true;
                value.FailureHandling =
                    (DiameterCreditControlFailureHandling)3;
            },
            value =>
            {
                value.DirectDebitFailureHandlingPresent = true;
                value.DirectDebitFailureHandling =
                    (DiameterDirectDebitFailureHandling)(-1);
            },
            value =>
            {
                value.DirectDebitFailureHandlingPresent = true;
                value.DirectDebitFailureHandling =
                    (DiameterDirectDebitFailureHandling)2;
            },
            value => value.Disposition =
                (DiameterCreditControlDisposition)(-1),
            value => value.Disposition =
                (DiameterCreditControlDisposition)6,
            value =>
            {
                value.RedirectHostUsagePresent = true;
                value.RedirectHostUsage =
                    (DiameterRedirectHostUsage)(-1);
            },
            value =>
            {
                value.RedirectHostUsagePresent = true;
                value.RedirectHostUsage =
                    (DiameterRedirectHostUsage)7;
            },
            value => value.SessionId = null!,
            value => value.SessionId = string.Empty,
            value => value.UserName = null!,
            value => value.AccountingMultiSessionId = null!,
            value => value.MultipleServiceCredits = null!,
            value => value.RedirectHosts = null!,
            value => value.ServiceAvps = null!,
            value => value.FailedAvps = null!,
            value => value.SessionId = "bad\ud800session",
            value => value.UserName = "bad\ud800user",
            value => value.AccountingMultiSessionId = "bad\ud800accounting",
            value =>
            {
                value.Message = new DiameterMessage();
                value.Message.Avps = null!;
            },
            value =>
            {
                value.ValidityTimePresent = true;
                value.ValidityTimeMilliseconds = 0;
            },
            value =>
            {
                value.ValidityTimePresent = true;
                value.ValidityTimeMilliseconds = 1;
            },
            value =>
            {
                value.RedirectMaxCacheTimePresent = true;
                value.RedirectMaxCacheTimeMilliseconds = -1;
            },
            value =>
            {
                value.RedirectMaxCacheTimePresent = true;
                value.RedirectMaxCacheTimeMilliseconds = 1;
            },
            value =>
            {
                value.EventTimestampPresent = true;
                value.EventTimestamp = default(DateTimeOffset);
            },
            value => value.MultipleServiceCredits.Add(null!),
            value => value.RedirectHosts.Add(null!),
            value =>
            {
                DiameterUri redirect = new DiameterUri();
                redirect.Host.Value = "bad host";
                value.RedirectHosts.Add(redirect);
            },
            value =>
            {
                DiameterUri redirect = new DiameterUri();
                redirect.Protocol = "aaa";
                redirect.Host.Value = "redirect.example";
                value.RedirectHosts.Add(redirect);
            },
            value => value.ServiceAvps.Add(null!),
            value =>
            {
                DiameterAvp avp = new DiameterAvp();
                avp.Data = null!;
                value.ServiceAvps.Add(avp);
            },
            value => value.FailedAvps.Add(null!),
            value =>
            {
                DiameterAvp avp = new DiameterAvp();
                avp.Data = null!;
                value.FailedAvps.Add(avp);
            });
    }

    [Test]
    public void CreditControlSessionRejectsEveryInvalidBoundaryAndOwnedValue()
    {
        DateTimeOffset created = DateTimeOffset.FromUnixTimeSeconds(1000);
        Func<DiameterCreditControlSession> factory =
            () => CreateValidSession(created);

        AssertEveryMutationIsRejected(
            factory,
            value => value.ValidateCreditControlSession(),
            value => value.Role = (DiameterSessionRole)(-1),
            value => value.Role = (DiameterSessionRole)2,
            value => value.State = (DiameterCreditControlState)(-1),
            value => value.State = (DiameterCreditControlState)12,
            value => value.PendingRequestType =
                (DiameterCreditControlRequestType)(-1),
            value => value.PendingRequestType =
                (DiameterCreditControlRequestType)5,
            value => value.FailureHandling =
                (DiameterCreditControlFailureHandling)(-1),
            value => value.FailureHandling =
                (DiameterCreditControlFailureHandling)3,
            value => value.DirectDebitFailureHandling =
                (DiameterDirectDebitFailureHandling)(-1),
            value => value.DirectDebitFailureHandling =
                (DiameterDirectDebitFailureHandling)2,
            value => value.Disposition =
                (DiameterCreditControlDisposition)(-1),
            value => value.Disposition =
                (DiameterCreditControlDisposition)6,
            value =>
            {
                value.MultipleServicesIndicatorPresent = true;
                value.MultipleServicesIndicator =
                    (DiameterMultipleServicesIndicator)(-1);
            },
            value =>
            {
                value.MultipleServicesIndicatorPresent = true;
                value.MultipleServicesIndicator =
                    (DiameterMultipleServicesIndicator)2;
            },
            value => value.ReAuthRequestType =
                (DiameterReAuthRequestType)(-1),
            value => value.ReAuthRequestType =
                (DiameterReAuthRequestType)2,
            value => value.Version = -1,
            value => value.AttemptCount = -1,
            value => value.PendingRequestIdentifier = -1,
            value => value.LastCompletedRequestIdentifier = -1,
            value => value.SessionId = null!,
            value => value.SessionId = string.Empty,
            value => value.ServiceContextId = null!,
            value => value.ServiceContextId = string.Empty,
            value => value.UserName = null!,
            value => value.AccountingMultiSessionId = null!,
            value => value.CorrelationId = null!,
            value => value.MultipleServiceCredits = null!,
            value => value.PoolReferences = null!,
            value => value.SubscriptionIdentifiers = null!,
            value => value.AlternateServers = null!,
            value => value.AttemptedServers = null!,
            value => value.RouteIdentifier = null!,
            value => value.ApplicationState = null!,
            value => value.Code = null!,
            value => value.Message = null!,
            value => value.SessionId = "bad\ud800session",
            value => value.ServiceContextId = "bad\ud800context",
            value => value.UserName = "bad\ud800user",
            value => value.AccountingMultiSessionId = "bad\ud800accounting",
            value => value.RouteIdentifier = "bad\ud800route",
            value => value.Code = "bad\ud800code",
            value => value.Message = "bad\ud800message",
            value => value.DestinationRealm = null!,
            value => value.DestinationRealm!.Value = "bad realm",
            value => value.DestinationHost = CreateIdentity("bad host"),
            value => value.PeerIdentity = CreateIdentity("bad peer"),
            value => value.CreatedAt = default(DateTimeOffset),
            value => value.UpdatedAt = created.AddSeconds(-1),
            value => value.TransactionDeadline = value.UpdatedAt.AddSeconds(-1),
            value => value.ValidityDeadline = value.UpdatedAt.AddSeconds(-1),
            value =>
            {
                value.TariffChangeTimePresent = true;
                value.TariffChangeTime = created.AddSeconds(-1);
            },
            value => value.NextInterrogationTime =
                value.UpdatedAt.AddSeconds(-1),
            value => value.PendingRequestIdentifier = 0,
            value =>
            {
                value.PendingRequestPresent = false;
                value.TransactionDeadlinePresent = true;
                value.PendingRequestIdentifier = 0;
            },
            value =>
            {
                value.PendingRequestPresent = false;
                value.TransactionDeadlinePresent = false;
                value.PendingRequestIdentifier = 1;
            },
            value => value.MultipleServiceCredits.Add(null!),
            value => value.PoolReferences.Add(null!),
            value => value.SubscriptionIdentifiers.Add(null!),
            value => value.AlternateServers.Add(null!),
            value => value.AttemptedServers.Add(null!),
            value => value.AttemptedServers.Add(string.Empty),
            value => value.AttemptedServers.Add("bad\ud800server"));

        DiameterCreditControlSession optionalUnits = factory();
        optionalUnits.QuotaThreshold = CreateServiceUnit();
        optionalUnits.QosFinalUnitInstruction = CreateQosFinalUnit();
        optionalUnits.AppliedFinalUnitInstruction =
            CreateRedirectFinalUnit();
        optionalUnits.PrimaryServer = CreateCreditControlServer();
        optionalUnits.AlternateServers.Add(CreateCreditControlServer());
        optionalUnits.AttemptedServers.Add("route:credit-route");

        Assert.DoesNotThrow(
            optionalUnits.ValidateCreditControlSession);
    }

    [Test]
    public void CreditControlResultRejectsEveryInvalidBoundaryAndConflict()
    {
        AssertEveryMutationIsRejected(
            CreateValidResult,
            value => value.ValidateCreditControlResult(),
            value => value.State = (DiameterCreditControlState)(-1),
            value => value.State = (DiameterCreditControlState)12,
            value => value.Disposition =
                (DiameterCreditControlDisposition)(-1),
            value => value.Disposition =
                (DiameterCreditControlDisposition)6,
            value => value.ResultCode = 999U,
            value => value.ResultCode = 6000U,
            value => value.Request = null!,
            value => value.Code = null!,
            value => value.Message = null!,
            value => value.Code = "bad\ud800code",
            value => value.Message = "bad\ud800message",
            value =>
            {
                value.Answer = null;
                value.AnswerReceived = true;
            },
            value =>
            {
                value.ContinueService = true;
                value.TerminateService = true;
            },
            value =>
            {
                value.RetryRequired = true;
                value.TerminateService = true;
            });
    }

    private static DiameterCreditControlRequest CreateValidRequest()
    {
        DiameterCreditControlRequest result =
            new DiameterCreditControlRequest();
        result.SessionId = "client.example;credit;1";
        result.DestinationRealm = CreateIdentity("remote.example");
        result.DestinationHost =
            CreateIdentity("credit.remote.example");
        result.PeerIdentity = CreateIdentity("peer.example");
        result.ServiceContextId = "example.service";
        result.UserName = "alice@remote.example";
        result.RouteIdentifier = "credit-route";
        result.CorrelationText = "credit-operation";
        return result;
    }

    private static DiameterCreditControlResult CreateValidResult()
    {
        DiameterCreditControlResult result =
            new DiameterCreditControlResult();
        result.Request = CreateValidRequest();
        result.Answer = CreateValidAnswer();
        result.AnswerReceived = true;
        result.ResultCode = DiameterResultCodes.Success;
        return result;
    }

    private static DiameterCreditControlAnswer CreateValidAnswer()
    {
        DiameterCreditControlAnswer result =
            new DiameterCreditControlAnswer();
        result.SessionId = "client.example;credit;1";
        result.RequestType = DiameterCreditControlRequestType.Initial;
        result.RequestNumber = 0U;
        result.ResultCode = DiameterResultCodes.Success;
        return result;
    }

    private static DiameterCreditControlSession CreateValidSession(
        DateTimeOffset created)
    {
        DiameterCreditControlSession result =
            new DiameterCreditControlSession();
        result.SessionId = "client.example;credit;1";
        result.Version = 2;
        result.Role = DiameterSessionRole.Client;
        result.State = DiameterCreditControlState.PendingUpdate;
        result.ServiceContextId = "example.service";
        result.DestinationRealm = CreateIdentity("remote.example");
        result.DestinationHost =
            CreateIdentity("credit.remote.example");
        result.PeerIdentity = CreateIdentity("peer.example");
        result.UserName = "alice@remote.example";
        result.PendingRequestPresent = true;
        result.PendingRequestIdentifier = 7;
        result.PendingRequestType =
            DiameterCreditControlRequestType.Update;
        result.PendingRequestNumber = 1U;
        result.NextRequestNumber = 2U;
        result.RequestedServiceUnit = CreateServiceUnit();
        result.GrantedServiceUnit = CreateServiceUnit();
        result.UsedServiceUnit = CreateServiceUnit();
        result.MultipleServiceCredits.Add(
            CreateMultipleServiceCredit());
        result.PoolReferences.Add(CreatePoolReference());
        result.FinalUnitInstruction = CreateRedirectFinalUnit();
        result.CostInformation = CreateCostInformation();
        result.SubscriptionIdentifiers.Add(
            CreateSubscriptionIdentifier());
        result.UserEquipmentInformation =
            CreateEquipmentInformation();
        result.RouteIdentifier = "credit-route";
        result.CreatedAt = created;
        result.UpdatedAt = created.AddSeconds(1);
        result.TransactionDeadlinePresent = true;
        result.TransactionDeadline = created.AddSeconds(31);
        result.ValidityDeadlinePresent = true;
        result.ValidityDeadline = created.AddMinutes(1);
        result.NextInterrogationTimePresent = true;
        result.NextInterrogationTime = created.AddSeconds(45);
        result.LastRequest = CreateValidRequest();
        result.LastRequest.RequestType =
            DiameterCreditControlRequestType.Update;
        result.LastRequest.RequestNumber = 1U;
        result.LastAnswer = CreateValidAnswer();
        result.AttemptCount = 1;
        result.ApplicationState = new byte[] { 1, 2 };
        result.Code = "credit.pending-update";
        result.Message = "A quota update is pending.";
        return result;
    }

    private static DiameterServiceUnit CreateServiceUnit()
    {
        DiameterServiceUnit result = new DiameterServiceUnit();
        result.Time = 60U;
        result.TimePresent = true;
        result.Money = CreateCostInformation();
        result.TotalOctets = 1000UL;
        result.TotalOctetsPresent = true;
        result.InputOctets = 400UL;
        result.InputOctetsPresent = true;
        result.OutputOctets = 600UL;
        result.OutputOctetsPresent = true;
        result.ServiceSpecificUnits = 5UL;
        result.ServiceSpecificUnitsPresent = true;
        result.TariffChangeTimePresent = true;
        result.TariffChangeTime =
            DateTimeOffset.FromUnixTimeSeconds(2000);
        result.TariffChangeUsagePresent = true;
        result.TariffChangeUsage =
            DiameterTariffChangeUsage.UnitBeforeTariffChange;
        return result;
    }

    private static DiameterCostInformation CreateCostInformation()
    {
        DiameterCostInformation result =
            new DiameterCostInformation();
        result.ValueDigits = 125L;
        result.Exponent = -2;
        result.ExponentPresent = true;
        result.CurrencyCode = 840U;
        result.CurrencyCodePresent = true;
        result.CostUnit = "USD";
        return result;
    }

    private static DiameterPoolReference CreatePoolReference()
    {
        DiameterPoolReference result = new DiameterPoolReference();
        result.PoolIdentifier = 7U;
        result.UnitType = DiameterCreditControlUnitType.TotalOctets;
        result.ValueDigits = 1L;
        return result;
    }

    private static DiameterSubscriptionIdentifier
        CreateSubscriptionIdentifier()
    {
        DiameterSubscriptionIdentifier result =
            new DiameterSubscriptionIdentifier();
        result.Type = DiameterSubscriptionIdType.EndUserImsi;
        result.Data = "001010123456789";
        return result;
    }

    private static DiameterUserEquipmentInformation
        CreateEquipmentInformation()
    {
        DiameterUserEquipmentInformation result =
            new DiameterUserEquipmentInformation();
        result.Type = DiameterUserEquipmentInfoType.Imeisv;
        result.Value = new byte[] { 1, 2, 3, 4 };
        return result;
    }

    private static DiameterFinalUnitInstruction CreateRedirectFinalUnit()
    {
        DiameterFinalUnitInstruction result =
            new DiameterFinalUnitInstruction();
        result.Action = DiameterFinalUnitAction.Redirect;
        result.RedirectAddressType = DiameterRedirectAddressType.Url;
        result.RedirectAddressTypePresent = true;
        result.RedirectAddress = "https://portal.example/credit";
        return result;
    }

    private static DiameterFinalUnitInstruction CreateQosFinalUnit()
    {
        DiameterFinalUnitInstruction result =
            new DiameterFinalUnitInstruction();
        result.Action = DiameterFinalUnitAction.RestrictAccess;
        result.FilterIdentifiers.Add("limited-access");
        result.Qos = true;
        return result;
    }

    private static DiameterMultipleServiceCredit
        CreateMultipleServiceCredit()
    {
        DiameterMultipleServiceCredit result =
            new DiameterMultipleServiceCredit();
        result.RequestedUnit = CreateServiceUnit();
        result.GrantedUnit = CreateServiceUnit();
        result.UsedUnits.Add(CreateServiceUnit());
        result.ServiceIdentifiers.Add(10U);
        result.RatingGroup = 20U;
        result.RatingGroupPresent = true;
        result.PoolReferences.Add(CreatePoolReference());
        result.TariffChangeUsagePresent = true;
        result.TariffChangeUsage =
            DiameterTariffChangeUsage.UnitAfterTariffChange;
        result.ValidityTimePresent = true;
        result.ValidityTimeMilliseconds = 30000;
        result.ResultCodePresent = true;
        result.ResultCode = DiameterResultCodes.Success;
        result.FinalUnitInstruction = CreateRedirectFinalUnit();
        result.QosFinalUnitInstruction = CreateQosFinalUnit();
        result.Disposition =
            DiameterCreditControlDisposition.PartiallyGranted;
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterCreditControlServer
        CreateCreditControlServer()
    {
        DiameterCreditControlServer result =
            new DiameterCreditControlServer();
        result.DestinationHost = CreateIdentity("credit.example");
        result.RouteIdentifier = "credit-route";
        return result;
    }

    private static DiameterCreditControlServer
        CreateInvalidCreditControlServer()
    {
        DiameterCreditControlServer result =
            CreateCreditControlServer();
        result.DestinationHost!.Value = "bad host";
        return result;
    }

    private static DiameterCreditControlAvpPrivacy CreatePrivacy()
    {
        DiameterCreditControlAvpPrivacy result =
            new DiameterCreditControlAvpPrivacy();
        result.Classification =
            DiameterCreditControlPrivacyClass.SubscriberIdentity;
        result.Sensitive = true;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static void AssertEveryMutationIsRejected<T>(
        Func<T> factory,
        Action<T> validate,
        params Action<T>[] mutations)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            T value = factory();
            mutations[index](value);
            Assert.That(
                new TestDelegate(() => validate(value)),
                Throws.InstanceOf<ArgumentException>(),
                $"Mutation {index} was accepted.");
            index = index + 1;
        }
    }
}
