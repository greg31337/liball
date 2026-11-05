using System;

namespace websocket;

internal class WebSocketExtensionNegotiatorResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal WebSocketExtensionOffer[] Offers = Array.Empty<WebSocketExtensionOffer>();
    internal IWebSocketExtension[] Handlers = Array.Empty<IWebSocketExtension>();
    internal WebSocketExtensionNegotiation[] Negotiations =
        Array.Empty<WebSocketExtensionNegotiation>();
}

internal static class WebSocketExtensionNegotiator
{
    internal static WebSocketExtensionNegotiatorResult CreateClientOffers(
        WebSocketExtensionOffer[] configuredOffers,
        IWebSocketExtension[] handlers)
    {
        WebSocketExtensionNegotiatorResult result =
            new WebSocketExtensionNegotiatorResult();
        if (configuredOffers == null || handlers == null ||
            (configuredOffers.Length != 0 && handlers.Length != 0))
        {
            return result;
        }

        if (handlers.Length == 0)
        {
            WebSocketExtensionOffer[] offers = CopyOffers(configuredOffers);
            if (offers.Length != configuredOffers.Length)
            {
                return result;
            }

            result.Status = WebSocketParseStatus.Complete;
            result.Offers = offers;
            return result;
        }

        WebSocketExtensionOffer[] generated =
            new WebSocketExtensionOffer[handlers.Length];
        IWebSocketExtension[] retainedHandlers =
            new IWebSocketExtension[handlers.Length];
        int index = 0;
        while (index < handlers.Length)
        {
            if (handlers[index] == null)
            {
                return result;
            }

            WebSocketExtensionOffer offer =
                handlers[index].CreateClientExtensionOffer();
            if (!IsValidOffer(offer))
            {
                return result;
            }

            generated[index] = CopyOffer(offer);
            retainedHandlers[index] = handlers[index];
            index = index + 1;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Offers = generated;
        result.Handlers = retainedHandlers;
        return result;
    }

    internal static WebSocketExtensionNegotiatorResult ValidateServerExtensions(
        WebSocketExtensionOffer[] responses,
        WebSocketExtensionOffer[] offered,
        IWebSocketExtension[] handlers)
    {
        WebSocketExtensionNegotiatorResult result =
            new WebSocketExtensionNegotiatorResult();
        if (responses == null ||
            offered == null ||
            handlers == null ||
            offered.Length != handlers.Length)
        {
            return result;
        }

        WebSocketExtensionNegotiation[] negotiations =
            new WebSocketExtensionNegotiation[responses.Length];
        int responseIndex = 0;
        while (responseIndex < responses.Length)
        {
            WebSocketExtensionOffer response = responses[responseIndex];
            if (!IsValidOffer(response) ||
                ContainsNegotiatedName(negotiations, responseIndex, response.Name))
            {
                return result;
            }

            bool accepted = false;
            int offerIndex = 0;
            while (offerIndex < offered.Length && !accepted)
            {
                if (offered[offerIndex].Name == response.Name)
                {
                    WebSocketExtensionNegotiation negotiation =
                        handlers[offerIndex].ValidateServerExtensionResponse(
                            CopyOffer(response));
                    if (IsValidClientNegotiation(negotiation, response))
                    {
                        negotiations[responseIndex] = CopyNegotiation(negotiation);
                        accepted = true;
                    }
                    else if (negotiation == null ||
                        negotiation.HasError ||
                        (negotiation.Status != WebSocketExtensionStatus.Declined &&
                            negotiation.Status != WebSocketExtensionStatus.Invalid))
                    {
                        return result;
                    }
                }

                offerIndex = offerIndex + 1;
            }

            if (!accepted)
            {
                return result;
            }

            responseIndex = responseIndex + 1;
        }

        if (!WebSocketExtensionClaimValidator.ValidateClaims(negotiations))
        {
            return result;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Negotiations = negotiations;
        return result;
    }

    internal static WebSocketExtensionNegotiatorResult SelectServerExtensions(
        WebSocketExtensionOffer[] offers,
        IWebSocketExtension[] handlers)
    {
        WebSocketExtensionNegotiatorResult result =
            new WebSocketExtensionNegotiatorResult();
        if (offers == null || handlers == null)
        {
            return result;
        }

        WebSocketExtensionNegotiation[] selected =
            new WebSocketExtensionNegotiation[handlers.Length];
        int count = 0;
        int handlerIndex = 0;
        while (handlerIndex < handlers.Length)
        {
            if (handlers[handlerIndex] == null)
            {
                return result;
            }

            bool handlerComplete = false;
            int offerIndex = 0;
            while (offerIndex < offers.Length && !handlerComplete)
            {
                WebSocketExtensionNegotiation negotiation =
                    handlers[handlerIndex].AcceptClientExtensionOffer(
                        CopyOffer(offers[offerIndex]));
                if (negotiation == null)
                {
                    return result;
                }

                if (negotiation.Status == WebSocketExtensionStatus.Negotiated)
                {
                    if (!IsValidServerNegotiation(negotiation, offers[offerIndex]))
                    {
                        return result;
                    }

                    if (!ContainsNegotiatedName(selected, count, negotiation.Name))
                    {
                        selected[count] = CopyNegotiation(negotiation);
                        count = count + 1;
                    }

                    handlerComplete = true;
                }
                else if (negotiation.Status == WebSocketExtensionStatus.Invalid ||
                    negotiation.Status == WebSocketExtensionStatus.Offered ||
                    negotiation.HasError)
                {
                    return result;
                }

                offerIndex = offerIndex + 1;
            }

            handlerIndex = handlerIndex + 1;
        }

        WebSocketExtensionNegotiation[] negotiations =
            new WebSocketExtensionNegotiation[count];
        int index = 0;
        while (index < count)
        {
            negotiations[index] = selected[index];
            index = index + 1;
        }

        if (!WebSocketExtensionClaimValidator.ValidateClaims(negotiations))
        {
            return result;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Negotiations = negotiations;
        return result;
    }

    internal static WebSocketExtensionNegotiation CopyNegotiation(
        WebSocketExtensionNegotiation source)
    {
        WebSocketExtensionNegotiation copy = new WebSocketExtensionNegotiation();
        copy.Name = source.Name;
        copy.Status = source.Status;
        copy.HasAcceptedConfiguration = source.HasAcceptedConfiguration;
        copy.AcceptedConfiguration = CopyOffer(source.AcceptedConfiguration);
        copy.ClaimsRsv1 = source.ClaimsRsv1;
        copy.ClaimsRsv2 = source.ClaimsRsv2;
        copy.ClaimsRsv3 = source.ClaimsRsv3;
        copy.ClaimedOpcodes = CopyOpcodes(source.ClaimedOpcodes);
        copy.HasError = source.HasError;
        copy.Error = CopyError(source.Error);
        return copy;
    }

    internal static WebSocketExtensionOffer CopyOffer(WebSocketExtensionOffer source)
    {
        WebSocketExtensionOffer copy = new WebSocketExtensionOffer();
        copy.Name = source.Name;
        WebSocketExtensionParameter[] parameters =
            new WebSocketExtensionParameter[source.Parameters.Length];
        int index = 0;
        while (index < source.Parameters.Length)
        {
            WebSocketExtensionParameter parameter =
                new WebSocketExtensionParameter();
            parameter.Name = source.Parameters[index].Name;
            parameter.HasValue = source.Parameters[index].HasValue;
            parameter.Value = source.Parameters[index].Value;
            parameters[index] = parameter;
            index = index + 1;
        }

        copy.Parameters = parameters;
        return copy;
    }

    static WebSocketExtensionOffer[] CopyOffers(WebSocketExtensionOffer[] source)
    {
        WebSocketExtensionOffer[] copy = new WebSocketExtensionOffer[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            if (!IsValidOffer(source[index]))
            {
                return Array.Empty<WebSocketExtensionOffer>();
            }

            copy[index] = CopyOffer(source[index]);
            index = index + 1;
        }

        return copy;
    }

    static bool IsValidClientNegotiation(
        WebSocketExtensionNegotiation negotiation,
        WebSocketExtensionOffer response)
    {
        return IsValidNegotiation(negotiation, response.Name) &&
            OffersEqual(negotiation.AcceptedConfiguration, response);
    }

    static bool IsValidServerNegotiation(
        WebSocketExtensionNegotiation negotiation,
        WebSocketExtensionOffer offer)
    {
        return IsValidNegotiation(negotiation, offer.Name);
    }

    static bool IsValidNegotiation(
        WebSocketExtensionNegotiation negotiation,
        string expectedName)
    {
        return negotiation != null &&
            negotiation.Name == expectedName &&
            negotiation.Status == WebSocketExtensionStatus.Negotiated &&
            negotiation.HasAcceptedConfiguration &&
            negotiation.AcceptedConfiguration != null &&
            negotiation.AcceptedConfiguration.Name == expectedName &&
            IsValidOffer(negotiation.AcceptedConfiguration) &&
            negotiation.ClaimedOpcodes != null &&
            !negotiation.HasError &&
            negotiation.Error != null;
    }

    static bool IsValidOffer(WebSocketExtensionOffer offer)
    {
        if (offer == null ||
            offer.Name == null ||
            !AsciiToken.IsValid(offer.Name) ||
            offer.Parameters == null)
        {
            return false;
        }

        int index = 0;
        while (index < offer.Parameters.Length)
        {
            WebSocketExtensionParameter parameter = offer.Parameters[index];
            if (parameter == null ||
                parameter.Name == null ||
                !AsciiToken.IsValid(parameter.Name) ||
                parameter.Value == null ||
                (!parameter.HasValue && parameter.Value.Length != 0) ||
                (parameter.HasValue && !AsciiToken.IsValid(parameter.Value)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool OffersEqual(
        WebSocketExtensionOffer left,
        WebSocketExtensionOffer right)
    {
        if (left.Name != right.Name ||
            left.Parameters.Length != right.Parameters.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Parameters.Length)
        {
            if (left.Parameters[index].Name != right.Parameters[index].Name ||
                left.Parameters[index].HasValue != right.Parameters[index].HasValue ||
                left.Parameters[index].Value != right.Parameters[index].Value)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool ContainsNegotiatedName(
        WebSocketExtensionNegotiation[] negotiations,
        int count,
        string name)
    {
        int index = 0;
        while (index < count)
        {
            if (negotiations[index].Name == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static WebSocketOpcode[] CopyOpcodes(WebSocketOpcode[] source)
    {
        WebSocketOpcode[] copy = new WebSocketOpcode[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static WebSocketError CopyError(WebSocketError source)
    {
        WebSocketError copy = new WebSocketError();
        copy.Kind = source.Kind;
        copy.HasCloseCode = source.HasCloseCode;
        copy.CloseCode = source.CloseCode;
        copy.HasKnownCloseCode = source.HasKnownCloseCode;
        copy.KnownCloseCode = source.KnownCloseCode;
        copy.HasHttpStatusCode = source.HasHttpStatusCode;
        copy.HttpStatusCode = source.HttpStatusCode;
        copy.Identifier = source.Identifier;
        copy.Diagnostic = source.Diagnostic;
        return copy;
    }
}
