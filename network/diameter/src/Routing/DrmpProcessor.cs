using System;

namespace diameter;

internal class DrmpInspectionResult
{
    internal bool Valid;
    internal bool Present;
    internal DiameterRoutingPriority Priority =
        DiameterRoutingPriority.Priority10;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal static class DrmpProcessor
{
    internal static DrmpInspectionResult InspectRequestPriority(
        DiameterMessage? message,
        DiameterRoutingPriority defaultPriority)
    {
        return InspectMessagePriority(
            message,
            defaultPriority,
            "request");
    }

    internal static DrmpInspectionResult InspectAnswerPriority(
        DiameterMessage? message,
        DiameterRoutingPriority requestPriority)
    {
        return InspectMessagePriority(
            message,
            requestPriority,
            "answer");
    }

    internal static DrmpInspectionResult InspectMessagePriority(
        DiameterMessage? message,
        DiameterRoutingPriority fallbackPriority,
        string messageKind)
    {
        DrmpInspectionResult result = new DrmpInspectionResult();
        int fallbackValue = (int)fallbackPriority;
        if (fallbackValue < (int)DiameterRoutingPriority.Priority0 ||
            fallbackValue > (int)DiameterRoutingPriority.Priority15)
        {
            result.Code = "drmp-default-priority-invalid";
            result.Message = "The configured Diameter routing priority is invalid.";
            return result;
        }

        result.Priority = fallbackPriority;
        if (message == null || message.Avps == null)
        {
            result.Code = "drmp-message-invalid";
            result.Message = "The Diameter " + messageKind +
                " does not contain valid AVP storage.";
            return result;
        }

        int foundCount = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp == null || avp.Data == null)
            {
                result.Code = "drmp-message-invalid";
                result.Message = "The Diameter " + messageKind +
                    " contains invalid AVP storage.";
                return result;
            }

            if (avp.Code == DiameterAvpCodes.Drmp)
            {
                foundCount = foundCount + 1;
                if (foundCount > 1)
                {
                    result.Code = "drmp-duplicate";
                    result.Message = "A Diameter message cannot contain multiple DRMP AVPs.";
                    return result;
                }

                if ((avp.Flags & DiameterAvpFlags.VendorSpecific) !=
                        DiameterAvpFlags.None ||
                    avp.VendorId != DiameterVendorIds.Ietf)
                {
                    result.Code = "drmp-vendor-invalid";
                    result.Message = "The RFC 7944 DRMP AVP cannot be vendor-specific.";
                    return result;
                }

                DiameterValueResult decoded =
                    ValueCodecs.DecodeEnumerated(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success ||
                    decoded.ConsumedCount != avp.Data.Length ||
                    decoded.Integer32 <
                        (int)DiameterRoutingPriority.Priority0 ||
                    decoded.Integer32 >
                        (int)DiameterRoutingPriority.Priority15)
                {
                    result.Code = "drmp-value-invalid";
                    result.Message = "The DRMP AVP must contain one RFC 7944 priority value.";
                    return result;
                }

                result.Priority =
                    (DiameterRoutingPriority)decoded.Integer32;
                result.Present = true;
            }

            index = index + 1;
        }

        result.Valid = true;
        result.Code = result.Present ?
            "drmp-explicit-priority" :
            "drmp-default-priority";
        result.Message = result.Present ?
            "The explicit DRMP priority was selected." :
            "The configured default routing priority was selected.";
        return result;
    }
}
