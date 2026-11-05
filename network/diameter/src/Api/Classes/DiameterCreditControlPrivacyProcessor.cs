using System;
using System.Collections.Generic;
using System.Text;

namespace diameter;

/// <summary>Minimizes or redacts privacy-sensitive credit-control AVPs.</summary>
public class DiameterCreditControlPrivacyProcessor :
    ICreditControlPrivacyProcessor
{
    /// <summary>Creates a credit-control privacy processor.</summary>
    public DiameterCreditControlPrivacyProcessor()
    {
    }

    /// <summary>Minimizes or redacts sensitive AVPs without changing the input.</summary>
    /// <param name="message">The credit-control request or answer.</param>
    /// <param name="policy">The policy invoked for each sensitive AVP.</param>
    /// <param name="agentRole">The role forwarding the message.</param>
    /// <returns>An owned message containing the permitted AVPs.</returns>
    /// <exception cref="ArgumentException">The message or policy is absent.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The role or policy action is invalid.</exception>
    public DiameterMessage ApplyDiameterCreditControlPrivacyPolicy(
        DiameterMessage? message,
        ICreditControlPrivacyPolicy? policy,
        DiameterAgentRole agentRole)
    {
        int role = (int)agentRole;
        if (message == null || policy == null)
        {
            throw new ArgumentException(
                "A credit-control message and privacy policy are required.");
        }

        if (role < (int)DiameterAgentRole.None ||
            role > (int)DiameterAgentRole.Translation)
        {
            throw new ArgumentOutOfRangeException("agentRole");
        }

        if (message.ApplicationId != DiameterApplicationIds.CreditControl ||
            message.CommandCode != DiameterCommandCodes.CreditControl)
        {
            throw new ArgumentException(
                "A Credit-Control request or answer is required.");
        }

        DiameterMessage result =
            CreditControlCommandCodec.CloneCreditControlMessage(message);
        result.Avps.Clear();
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp source = message.Avps[index];
            DiameterCreditControlAvpPrivacy privacy =
                ClassifyCreditControlAvp(source, agentRole);
            bool sensitive = privacy.Sensitive;
            bool required = privacy.RequiredForRouting ||
                privacy.RequiredForCorrelation;
            DiameterCreditControlPrivacyAction action =
                DiameterCreditControlPrivacyAction.Preserve;
            if (sensitive)
            {
                action = policy.SelectDiameterCreditControlPrivacyAction(
                    privacy);
                int actionValue = (int)action;
                if (actionValue <
                        (int)DiameterCreditControlPrivacyAction.Preserve ||
                    actionValue >
                        (int)DiameterCreditControlPrivacyAction.Redact)
                {
                    throw new ArgumentOutOfRangeException(
                        "Credit-control privacy action");
                }
            }

            if (!sensitive || required ||
                action == DiameterCreditControlPrivacyAction.Preserve)
            {
                result.Avps.Add(
                    CreditControlCommandCodec.CloneCreditControlAvp(source));
            }
            else if (action == DiameterCreditControlPrivacyAction.Redact)
            {
                result.Avps.Add(RedactCreditControlAvp(source));
            }

            index = index + 1;
        }

        return result;
    }

    internal DiameterCreditControlAvpPrivacy ClassifyCreditControlAvp(
        DiameterAvp avp,
        DiameterAgentRole agentRole)
    {
        DiameterCreditControlAvpPrivacy result =
            new DiameterCreditControlAvpPrivacy();
        result.AvpCode = avp.Code;
        result.VendorId = avp.VendorId;
        result.AgentRole = agentRole;
        if (avp.VendorId != DiameterVendorIds.Ietf)
        {
            return result;
        }

        if (IsCreditControlSubscriberAvp(avp.Code))
        {
            result.Classification =
                DiameterCreditControlPrivacyClass.SubscriberIdentity;
        }
        else if (IsCreditControlEquipmentAvp(avp.Code))
        {
            result.Classification =
                DiameterCreditControlPrivacyClass.EquipmentIdentity;
        }
        else if (IsCreditControlCorrelationPrivacyAvp(avp.Code))
        {
            result.Classification =
                DiameterCreditControlPrivacyClass.Correlation;
        }
        else if (IsCreditControlServicePrivacyAvp(avp.Code))
        {
            result.Classification =
                DiameterCreditControlPrivacyClass.ServiceData;
        }

        result.Sensitive = result.Classification !=
            DiameterCreditControlPrivacyClass.None;
        result.RequiredForCorrelation =
            IsCreditControlRequiredCorrelationAvp(avp.Code);
        result.RequiredForRouting =
            (agentRole == DiameterAgentRole.Relay ||
            agentRole == DiameterAgentRole.Proxy) &&
            IsCreditControlRequiredRoutingAvp(avp.Code);
        result.ValidateCreditControlAvpPrivacy();
        return result;
    }

    internal bool IsCreditControlSubscriberAvp(uint code)
    {
        return code == DiameterAvpCodes.UserName ||
            code == DiameterAvpCodes.SubscriptionId ||
            code == DiameterAvpCodes.SubscriptionIdData ||
            code == DiameterAvpCodes.SubscriptionIdExtension ||
            code == DiameterAvpCodes.SubscriptionIdE164 ||
            code == DiameterAvpCodes.SubscriptionIdImsi ||
            code == DiameterAvpCodes.SubscriptionIdSipUri ||
            code == DiameterAvpCodes.SubscriptionIdNai ||
            code == DiameterAvpCodes.SubscriptionIdPrivate;
    }

    internal bool IsCreditControlEquipmentAvp(uint code)
    {
        return code == DiameterAvpCodes.UserEquipmentInfo ||
            code == DiameterAvpCodes.UserEquipmentInfoValue ||
            code == DiameterAvpCodes.UserEquipmentInfoExtension ||
            code == DiameterAvpCodes.UserEquipmentInfoImeisv ||
            code == DiameterAvpCodes.UserEquipmentInfoMac ||
            code == DiameterAvpCodes.UserEquipmentInfoEui64 ||
            code == DiameterAvpCodes.UserEquipmentInfoModifiedEui64 ||
            code == DiameterAvpCodes.UserEquipmentInfoImei;
    }

    internal bool IsCreditControlCorrelationPrivacyAvp(uint code)
    {
        return code == DiameterAvpCodes.SessionId ||
            code == DiameterAvpCodes.CcCorrelationId ||
            code == DiameterAvpCodes.AcctMultiSessionId ||
            code == DiameterAvpCodes.CcSubSessionId;
    }

    internal bool IsCreditControlRequiredCorrelationAvp(uint code)
    {
        return IsCreditControlCorrelationPrivacyAvp(code) ||
            code == DiameterAvpCodes.CcRequestNumber ||
            code == DiameterAvpCodes.CcRequestType;
    }

    internal bool IsCreditControlRequiredRoutingAvp(uint code)
    {
        return IsCreditControlSubscriberAvp(code) ||
            code == DiameterAvpCodes.ServiceIdentifier ||
            code == DiameterAvpCodes.RatingGroup ||
            code == DiameterAvpCodes.ServiceContextId ||
            code == DiameterAvpCodes.MultipleServicesCreditControl;
    }

    internal bool IsCreditControlServicePrivacyAvp(uint code)
    {
        return code == DiameterAvpCodes.CheckBalanceResult ||
            code == DiameterAvpCodes.CostInformation ||
            code == DiameterAvpCodes.CostUnit ||
            code == DiameterAvpCodes.CurrencyCode ||
            code == DiameterAvpCodes.CcMoney ||
            code == DiameterAvpCodes.CcInputOctets ||
            code == DiameterAvpCodes.CcOutputOctets ||
            code == DiameterAvpCodes.CcServiceSpecificUnits ||
            code == DiameterAvpCodes.CcTime ||
            code == DiameterAvpCodes.CcTotalOctets ||
            code == DiameterAvpCodes.RequestedServiceUnit ||
            code == DiameterAvpCodes.GrantedServiceUnit ||
            code == DiameterAvpCodes.UsedServiceUnit ||
            code == DiameterAvpCodes.UnitValue ||
            code == DiameterAvpCodes.ValueDigits ||
            code == DiameterAvpCodes.Exponent ||
            code == DiameterAvpCodes.ServiceIdentifier ||
            code == DiameterAvpCodes.RatingGroup ||
            code == DiameterAvpCodes.RestrictionFilterRule ||
            code == DiameterAvpCodes.FilterId ||
            code == DiameterAvpCodes.QosFilterRule ||
            code == DiameterAvpCodes.RedirectServer ||
            code == DiameterAvpCodes.RedirectServerAddress ||
            code == DiameterAvpCodes.RedirectServerExtension ||
            code == DiameterAvpCodes.RedirectAddressIpAddress ||
            code == DiameterAvpCodes.RedirectAddressUrl ||
            code == DiameterAvpCodes.RedirectAddressSipUri ||
            code == DiameterAvpCodes.ServiceContextId ||
            code == DiameterAvpCodes.ServiceParameterInfo ||
            code == DiameterAvpCodes.ServiceParameterValue ||
            code == DiameterAvpCodes.MultipleServicesCreditControl ||
            code == DiameterAvpCodes.FinalUnitIndication ||
            code == DiameterAvpCodes.QosFinalUnitIndication;
    }

    internal DiameterAvp RedactCreditControlAvp(DiameterAvp source)
    {
        if (IsCreditControlGroupedPrivacyAvp(source.Code))
        {
            return RedactCreditControlGroupedAvp(source);
        }

        DiameterAvp result =
            CreditControlCommandCodec.CloneCreditControlAvp(source);
        if (IsCreditControlTextPrivacyAvp(source.Code))
        {
            result.Data = Encoding.UTF8.GetBytes("[redacted]");
        }
        else if (source.Code == DiameterAvpCodes.RedirectAddressIpAddress)
        {
            result.Data = new byte[] { 0, 1, 0, 0, 0, 0 };
        }
        else if (IsCreditControlNumericPrivacyAvp(source.Code))
        {
            result.Data = new byte[4];
        }
        else if (IsCreditControlWideNumericPrivacyAvp(source.Code))
        {
            result.Data = new byte[8];
        }
        else
        {
            result.Data = new byte[] { 0 };
        }

        return result;
    }

    internal DiameterAvp RedactCreditControlGroupedAvp(DiameterAvp source)
    {
        DiameterValueResult decoded = AvpDecoder.DecodeAvpSequence(source.Data);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterCreditControlException(
                "A privacy-sensitive grouped AVP is invalid.");
        }

        List<DiameterAvp> children = new List<DiameterAvp>();
        int index = 0;
        while (index < decoded.Avps.Count)
        {
            DiameterAvp child = decoded.Avps[index];
            DiameterCreditControlAvpPrivacy privacy =
                ClassifyCreditControlAvp(child, DiameterAgentRole.None);
            if (privacy.Sensitive)
            {
                children.Add(RedactCreditControlAvp(child));
            }
            else
            {
                children.Add(
                    CreditControlCommandCodec.CloneCreditControlAvp(child));
            }

            index = index + 1;
        }

        DiameterAvp result = CreditControlCommandCodec
            .CreateCreditControlGroupedAvp(source.Code, children);
        result.Flags = source.Flags;
        result.VendorId = source.VendorId;
        return result;
    }

    internal bool IsCreditControlGroupedPrivacyAvp(uint code)
    {
        return code == DiameterAvpCodes.CostInformation ||
            code == DiameterAvpCodes.CcMoney ||
            code == DiameterAvpCodes.UnitValue ||
            code == DiameterAvpCodes.RequestedServiceUnit ||
            code == DiameterAvpCodes.GrantedServiceUnit ||
            code == DiameterAvpCodes.UsedServiceUnit ||
            code == DiameterAvpCodes.RedirectServer ||
            code == DiameterAvpCodes.RedirectServerExtension ||
            code == DiameterAvpCodes.ServiceParameterInfo ||
            code == DiameterAvpCodes.SubscriptionId ||
            code == DiameterAvpCodes.SubscriptionIdExtension ||
            code == DiameterAvpCodes.UserEquipmentInfo ||
            code == DiameterAvpCodes.UserEquipmentInfoExtension ||
            code == DiameterAvpCodes.MultipleServicesCreditControl ||
            code == DiameterAvpCodes.FinalUnitIndication ||
            code == DiameterAvpCodes.QosFinalUnitIndication;
    }

    internal bool IsCreditControlTextPrivacyAvp(uint code)
    {
        return code == DiameterAvpCodes.UserName ||
            code == DiameterAvpCodes.CostUnit ||
            code == DiameterAvpCodes.RestrictionFilterRule ||
            code == DiameterAvpCodes.FilterId ||
            code == DiameterAvpCodes.QosFilterRule ||
            code == DiameterAvpCodes.RedirectServerAddress ||
            code == DiameterAvpCodes.RedirectAddressUrl ||
            code == DiameterAvpCodes.RedirectAddressSipUri ||
            code == DiameterAvpCodes.ServiceContextId ||
            code == DiameterAvpCodes.SubscriptionIdData ||
            code == DiameterAvpCodes.SubscriptionIdE164 ||
            code == DiameterAvpCodes.SubscriptionIdImsi ||
            code == DiameterAvpCodes.SubscriptionIdSipUri ||
            code == DiameterAvpCodes.SubscriptionIdNai ||
            code == DiameterAvpCodes.SubscriptionIdPrivate;
    }

    internal bool IsCreditControlNumericPrivacyAvp(uint code)
    {
        return code == DiameterAvpCodes.CheckBalanceResult ||
            code == DiameterAvpCodes.CurrencyCode ||
            code == DiameterAvpCodes.CcTime ||
            code == DiameterAvpCodes.Exponent ||
            code == DiameterAvpCodes.ServiceIdentifier ||
            code == DiameterAvpCodes.RatingGroup;
    }

    internal bool IsCreditControlWideNumericPrivacyAvp(uint code)
    {
        return code == DiameterAvpCodes.CcInputOctets ||
            code == DiameterAvpCodes.CcOutputOctets ||
            code == DiameterAvpCodes.CcServiceSpecificUnits ||
            code == DiameterAvpCodes.CcTotalOctets ||
            code == DiameterAvpCodes.ValueDigits;
    }
}
