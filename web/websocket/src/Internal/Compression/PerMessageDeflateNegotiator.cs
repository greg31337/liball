using System;

namespace websocket;

internal static class PerMessageDeflateNegotiator
{
    const string ExtensionName = "permessage-deflate";
    const string ServerNoContextTakeoverName = "server_no_context_takeover";
    const string ClientNoContextTakeoverName = "client_no_context_takeover";
    const string ServerMaxWindowBitsName = "server_max_window_bits";
    const string ClientMaxWindowBitsName = "client_max_window_bits";

    internal static WebSocketExtensionOffer CreateClientOffer(
        PerMessageDeflateOptions options)
    {
        WebSocketOptionValidator.ValidatePerMessageDeflateOptions(options);
        PerMessageDeflateParameters parameters =
            new PerMessageDeflateParameters();
        parameters.Valid = true;
        parameters.ServerNoContextTakeover =
            options.OfferServerNoContextTakeover;
        parameters.ClientNoContextTakeover =
            options.OfferClientNoContextTakeover;
        parameters.HasServerMaxWindowBits = options.OfferServerMaxWindowBits;
        if (options.HasServerMaxWindowBits)
        {
            parameters.ServerMaxWindowBits = options.ServerMaxWindowBits;
        }

        parameters.HasClientMaxWindowBits = options.OfferClientMaxWindowBits;
        parameters.ClientMaxWindowBitsHasValue =
            options.HasClientMaxWindowBits;
        if (options.HasClientMaxWindowBits)
        {
            parameters.ClientMaxWindowBits = options.ClientMaxWindowBits;
        }

        return CreateConfiguration(parameters);
    }

    internal static WebSocketExtensionNegotiation SelectServerOffer(
        WebSocketExtensionOffer[] offers,
        PerMessageDeflateOptions options)
    {
        WebSocketOptionValidator.ValidatePerMessageDeflateOptions(options);
        if (offers == null)
        {
            throw new WebSocketExtensionException();
        }

        int offerIndex = 0;
        while (offerIndex < offers.Length)
        {
            WebSocketExtensionNegotiation negotiation = AcceptServerOffer(
                offers[offerIndex],
                options);
            if (negotiation.Status == WebSocketExtensionStatus.Negotiated)
            {
                return negotiation;
            }

            offerIndex = offerIndex + 1;
        }

        return CreateDeclined(ExtensionName);
    }

    internal static WebSocketExtensionNegotiation AcceptServerOffer(
        WebSocketExtensionOffer offer,
        PerMessageDeflateOptions options)
    {
        WebSocketOptionValidator.ValidatePerMessageDeflateOptions(options);
        PerMessageDeflateParameters offered = ParseOfferParameters(offer);
        if (!offered.Valid)
        {
            return CreateDeclined(ExtensionName);
        }

        if ((offered.ServerNoContextTakeover &&
                !options.OfferServerNoContextTakeover) ||
            (offered.HasServerMaxWindowBits &&
                !options.OfferServerMaxWindowBits))
        {
            return CreateDeclined(offer.Name);
        }

        PerMessageDeflateParameters accepted =
            new PerMessageDeflateParameters();
        accepted.Valid = true;
        accepted.ServerNoContextTakeover =
            options.OfferServerNoContextTakeover;
        accepted.ClientNoContextTakeover =
            options.OfferClientNoContextTakeover;

        if (options.OfferServerMaxWindowBits)
        {
            accepted.HasServerMaxWindowBits = true;
            accepted.ServerMaxWindowBits = 15;
            if (options.HasServerMaxWindowBits)
            {
                accepted.ServerMaxWindowBits = options.ServerMaxWindowBits;
            }

            if (offered.HasServerMaxWindowBits &&
                offered.ServerMaxWindowBits < accepted.ServerMaxWindowBits)
            {
                accepted.ServerMaxWindowBits = offered.ServerMaxWindowBits;
            }
        }

        if (options.OfferClientMaxWindowBits &&
            offered.HasClientMaxWindowBits)
        {
            accepted.HasClientMaxWindowBits = true;
            accepted.ClientMaxWindowBitsHasValue = true;
            accepted.ClientMaxWindowBits = 15;
            if (options.HasClientMaxWindowBits)
            {
                accepted.ClientMaxWindowBits = options.ClientMaxWindowBits;
            }

            if (offered.ClientMaxWindowBitsHasValue &&
                offered.ClientMaxWindowBits < accepted.ClientMaxWindowBits)
            {
                accepted.ClientMaxWindowBits = offered.ClientMaxWindowBits;
            }
        }

        WebSocketExtensionOffer configuration = CreateConfiguration(accepted);
        configuration.Name = offer.Name;
        return CreateNegotiated(configuration);
    }

    internal static WebSocketExtensionNegotiation ValidateServerResponse(
        WebSocketExtensionOffer response,
        WebSocketExtensionOffer[] offeredAlternatives,
        PerMessageDeflateOptions options)
    {
        WebSocketOptionValidator.ValidatePerMessageDeflateOptions(options);
        if (offeredAlternatives == null)
        {
            throw new WebSocketExtensionException();
        }

        PerMessageDeflateParameters accepted =
            ParseResponseParameters(response);
        if (!accepted.Valid ||
            (options.RequireServerNoContextTakeover &&
                !accepted.ServerNoContextTakeover) ||
            (options.RequireClientNoContextTakeover &&
                !accepted.ClientNoContextTakeover))
        {
            return CreateInvalid(ResponseName(response));
        }

        int offerIndex = 0;
        while (offerIndex < offeredAlternatives.Length)
        {
            PerMessageDeflateParameters offered = ParseOfferParameters(
                offeredAlternatives[offerIndex]);
            if (offered.Valid && ResponseMatchesOffer(accepted, offered))
            {
                return CreateNegotiated(
                    WebSocketExtensionNegotiator.CopyOffer(response));
            }

            offerIndex = offerIndex + 1;
        }

        return CreateInvalid(ResponseName(response));
    }

    internal static PerMessageDeflateParameters ParseOfferParameters(
        WebSocketExtensionOffer offer)
    {
        return ParseParameters(offer, false);
    }

    internal static PerMessageDeflateParameters ParseResponseParameters(
        WebSocketExtensionOffer response)
    {
        return ParseParameters(response, true);
    }

    internal static PerMessageDeflateDirectionSettings CreateDirectionSettings(
        WebSocketRole role,
        WebSocketExtensionOffer acceptedConfiguration)
    {
        PerMessageDeflateDirectionSettings settings =
            new PerMessageDeflateDirectionSettings();
        PerMessageDeflateParameters parameters =
            ParseResponseParameters(acceptedConfiguration);
        if (!parameters.Valid ||
            (role != WebSocketRole.Client && role != WebSocketRole.Server))
        {
            return settings;
        }

        int serverWindowBits = 15;
        if (parameters.HasServerMaxWindowBits)
        {
            serverWindowBits = parameters.ServerMaxWindowBits;
        }

        int clientWindowBits = 15;
        if (parameters.HasClientMaxWindowBits)
        {
            clientWindowBits = parameters.ClientMaxWindowBits;
        }

        if (role == WebSocketRole.Client)
        {
            settings.OutgoingContextTakeover =
                !parameters.ClientNoContextTakeover;
            settings.IncomingContextTakeover =
                !parameters.ServerNoContextTakeover;
            settings.OutgoingWindowBits = clientWindowBits;
            settings.IncomingWindowBits = serverWindowBits;
        }
        else
        {
            settings.OutgoingContextTakeover =
                !parameters.ServerNoContextTakeover;
            settings.IncomingContextTakeover =
                !parameters.ClientNoContextTakeover;
            settings.OutgoingWindowBits = serverWindowBits;
            settings.IncomingWindowBits = clientWindowBits;
        }

        settings.Valid = true;
        return settings;
    }

    static PerMessageDeflateParameters ParseParameters(
        WebSocketExtensionOffer offer,
        bool response)
    {
        PerMessageDeflateParameters result =
            new PerMessageDeflateParameters();
        if (offer == null ||
            offer.Name == null ||
            !AsciiToken.EqualsIgnoreCase(offer.Name, ExtensionName) ||
            offer.Parameters == null)
        {
            return result;
        }

        int parameterIndex = 0;
        while (parameterIndex < offer.Parameters.Length)
        {
            WebSocketExtensionParameter parameter =
                offer.Parameters[parameterIndex];
            if (parameter == null ||
                parameter.Name == null ||
                parameter.Value == null)
            {
                return result;
            }

            if (AsciiToken.EqualsIgnoreCase(
                parameter.Name,
                ServerNoContextTakeoverName))
            {
                if (result.ServerNoContextTakeover ||
                    parameter.HasValue ||
                    parameter.Value.Length != 0)
                {
                    return result;
                }

                result.ServerNoContextTakeover = true;
            }
            else if (AsciiToken.EqualsIgnoreCase(
                parameter.Name,
                ClientNoContextTakeoverName))
            {
                if (result.ClientNoContextTakeover ||
                    parameter.HasValue ||
                    parameter.Value.Length != 0)
                {
                    return result;
                }

                result.ClientNoContextTakeover = true;
            }
            else if (AsciiToken.EqualsIgnoreCase(
                parameter.Name,
                ServerMaxWindowBitsName))
            {
                if (result.HasServerMaxWindowBits || !parameter.HasValue)
                {
                    return result;
                }

                int value = ParseWindowBits(parameter.Value);
                if (value == 0)
                {
                    return result;
                }

                result.HasServerMaxWindowBits = true;
                result.ServerMaxWindowBits = value;
            }
            else if (AsciiToken.EqualsIgnoreCase(
                parameter.Name,
                ClientMaxWindowBitsName))
            {
                if (result.HasClientMaxWindowBits ||
                    (!parameter.HasValue && parameter.Value.Length != 0) ||
                    (response && !parameter.HasValue))
                {
                    return result;
                }

                result.HasClientMaxWindowBits = true;
                result.ClientMaxWindowBitsHasValue = parameter.HasValue;
                if (parameter.HasValue)
                {
                    int value = ParseWindowBits(parameter.Value);
                    if (value == 0)
                    {
                        return result;
                    }

                    result.ClientMaxWindowBits = value;
                }
            }
            else
            {
                return result;
            }

            parameterIndex = parameterIndex + 1;
        }

        result.Valid = true;
        return result;
    }

    static bool ResponseMatchesOffer(
        PerMessageDeflateParameters response,
        PerMessageDeflateParameters offer)
    {
        if (offer.ServerNoContextTakeover &&
            !response.ServerNoContextTakeover)
        {
            return false;
        }

        if (offer.HasServerMaxWindowBits &&
            (!response.HasServerMaxWindowBits ||
                response.ServerMaxWindowBits > offer.ServerMaxWindowBits))
        {
            return false;
        }

        if (response.HasClientMaxWindowBits)
        {
            if (!offer.HasClientMaxWindowBits)
            {
                return false;
            }

            if (offer.ClientMaxWindowBitsHasValue &&
                response.ClientMaxWindowBits > offer.ClientMaxWindowBits)
            {
                return false;
            }
        }

        return true;
    }

    static WebSocketExtensionOffer CreateConfiguration(
        PerMessageDeflateParameters parameters)
    {
        int count = 0;
        if (parameters.ServerNoContextTakeover)
        {
            count = count + 1;
        }

        if (parameters.ClientNoContextTakeover)
        {
            count = count + 1;
        }

        if (parameters.HasServerMaxWindowBits)
        {
            count = count + 1;
        }

        if (parameters.HasClientMaxWindowBits)
        {
            count = count + 1;
        }

        WebSocketExtensionOffer configuration = new WebSocketExtensionOffer();
        configuration.Name = ExtensionName;
        configuration.Parameters = new WebSocketExtensionParameter[count];
        int index = 0;
        if (parameters.ServerNoContextTakeover)
        {
            configuration.Parameters[index] = CreateParameter(
                ServerNoContextTakeoverName,
                false,
                "");
            index = index + 1;
        }

        if (parameters.ClientNoContextTakeover)
        {
            configuration.Parameters[index] = CreateParameter(
                ClientNoContextTakeoverName,
                false,
                "");
            index = index + 1;
        }

        if (parameters.HasServerMaxWindowBits)
        {
            configuration.Parameters[index] = CreateParameter(
                ServerMaxWindowBitsName,
                true,
                FormatWindowBits(parameters.ServerMaxWindowBits));
            index = index + 1;
        }

        if (parameters.HasClientMaxWindowBits)
        {
            string value = "";
            if (parameters.ClientMaxWindowBitsHasValue)
            {
                value = FormatWindowBits(parameters.ClientMaxWindowBits);
            }

            configuration.Parameters[index] = CreateParameter(
                ClientMaxWindowBitsName,
                parameters.ClientMaxWindowBitsHasValue,
                value);
        }

        return configuration;
    }

    static WebSocketExtensionParameter CreateParameter(
        string name,
        bool hasValue,
        string value)
    {
        WebSocketExtensionParameter parameter =
            new WebSocketExtensionParameter();
        parameter.Name = name;
        parameter.HasValue = hasValue;
        parameter.Value = value;
        return parameter;
    }

    static int ParseWindowBits(string value)
    {
        switch (value)
        {
            case "8":
                return 8;
            case "9":
                return 9;
            case "10":
                return 10;
            case "11":
                return 11;
            case "12":
                return 12;
            case "13":
                return 13;
            case "14":
                return 14;
            case "15":
                return 15;
            default:
                return 0;
        }
    }

    static string FormatWindowBits(int value)
    {
        switch (value)
        {
            case 8:
                return "8";
            case 9:
                return "9";
            case 10:
                return "10";
            case 11:
                return "11";
            case 12:
                return "12";
            case 13:
                return "13";
            case 14:
                return "14";
            default:
                return "15";
        }
    }

    static WebSocketExtensionNegotiation CreateNegotiated(
        WebSocketExtensionOffer configuration)
    {
        WebSocketExtensionNegotiation result =
            new WebSocketExtensionNegotiation();
        result.Name = configuration.Name;
        result.Status = WebSocketExtensionStatus.Negotiated;
        result.HasAcceptedConfiguration = true;
        result.AcceptedConfiguration = configuration;
        result.ClaimsRsv1 = true;
        return result;
    }

    static WebSocketExtensionNegotiation CreateDeclined(string name)
    {
        WebSocketExtensionNegotiation result =
            new WebSocketExtensionNegotiation();
        result.Name = name;
        result.Status = WebSocketExtensionStatus.Declined;
        return result;
    }

    static WebSocketExtensionNegotiation CreateInvalid(string name)
    {
        WebSocketExtensionNegotiation result =
            new WebSocketExtensionNegotiation();
        result.Name = name;
        result.Status = WebSocketExtensionStatus.Invalid;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Compression;
        result.Error.Identifier = "permessage-deflate-response-invalid";
        result.Error.Diagnostic =
            "The permessage-deflate response is invalid or incompatible.";
        return result;
    }

    static string ResponseName(WebSocketExtensionOffer response)
    {
        if (response == null || response.Name == null)
        {
            return ExtensionName;
        }

        return response.Name;
    }
}
