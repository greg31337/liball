using websocket;

namespace websocket.Tests;

public class PerMessageDeflateNegotiatorTests
{
    [Test]
    public void ClientOfferWritesAllParametersInCanonicalOrder()
    {
        var options = new PerMessageDeflateOptions();
        options.OfferServerNoContextTakeover = true;
        options.OfferClientNoContextTakeover = true;
        options.OfferServerMaxWindowBits = true;
        options.HasServerMaxWindowBits = true;
        options.ServerMaxWindowBits = 8;
        options.OfferClientMaxWindowBits = true;

        var offer = PerMessageDeflateNegotiator.CreateClientOffer(options);

        Assert.Multiple(() =>
        {
            Assert.That(offer.Name, Is.EqualTo("permessage-deflate"));
            Assert.That(offer.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new[]
                {
                    "server_no_context_takeover",
                    "client_no_context_takeover",
                    "server_max_window_bits",
                    "client_max_window_bits"
                }));
            Assert.That(offer.Parameters[0].HasValue, Is.False);
            Assert.That(offer.Parameters[1].HasValue, Is.False);
            Assert.That(offer.Parameters[2].Value, Is.EqualTo("8"));
            Assert.That(offer.Parameters[3].HasValue, Is.False);
            Assert.That(offer.Parameters[3].Value, Is.Empty);
        });

        options.HasClientMaxWindowBits = true;
        options.ClientMaxWindowBits = 15;
        var valued = PerMessageDeflateNegotiator.CreateClientOffer(options);

        Assert.Multiple(() =>
        {
            Assert.That(valued.Parameters[3].HasValue, Is.True);
            Assert.That(valued.Parameters[3].Value, Is.EqualTo("15"));
        });
    }

    [Test]
    public void EveryParameterPresenceCombinationNegotiatesEndToEnd()
    {
        for (var mask = 0; mask < 16; mask++)
        {
            PerMessageDeflateOptions options = OptionsForParameterMask(mask);
            var client = new PerMessageDeflateExtension(options);
            var server = new PerMessageDeflateExtension(options);
            WebSocketExtensionOffer offer = client.CreateClientExtensionOffer();

            WebSocketExtensionNegotiation accepted =
                server.AcceptClientExtensionOffer(offer);
            WebSocketExtensionNegotiation validated =
                client.ValidateServerExtensionResponse(
                    accepted.AcceptedConfiguration);
            string[] expectedNames = ParameterNamesForMask(mask);

            Assert.Multiple(() =>
            {
                Assert.That(offer.Parameters.Select(parameter => parameter.Name),
                    Is.EqualTo(expectedNames), "offer mask " + mask);
                Assert.That(accepted.Status,
                    Is.EqualTo(WebSocketExtensionStatus.Negotiated),
                    "server mask " + mask);
                Assert.That(accepted.AcceptedConfiguration.Parameters.Select(
                    parameter => parameter.Name),
                    Is.EqualTo(expectedNames), "response mask " + mask);
                Assert.That(validated.Status,
                    Is.EqualTo(WebSocketExtensionStatus.Negotiated),
                    "client mask " + mask);
            });
        }
    }

    [TestCase("8")]
    [TestCase("9")]
    [TestCase("10")]
    [TestCase("11")]
    [TestCase("12")]
    [TestCase("13")]
    [TestCase("14")]
    [TestCase("15")]
    public void ParsesEveryPermittedWindowValue(string value)
    {
        var offer = Offer(
            Value("server_max_window_bits", value),
            Value("client_max_window_bits", value));

        var parsedOffer = PerMessageDeflateNegotiator.ParseOfferParameters(offer);
        var parsedResponse =
            PerMessageDeflateNegotiator.ParseResponseParameters(offer);

        Assert.Multiple(() =>
        {
            Assert.That(parsedOffer.Valid, Is.True);
            Assert.That(parsedOffer.ServerMaxWindowBits,
                Is.EqualTo(int.Parse(value)));
            Assert.That(parsedOffer.ClientMaxWindowBits,
                Is.EqualTo(int.Parse(value)));
            Assert.That(parsedResponse.Valid, Is.True);
        });
    }

    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    public void NegotiatesEveryPermittedWindowValueInBothDirections(int value)
    {
        var options = new PerMessageDeflateOptions
        {
            OfferServerMaxWindowBits = true,
            HasServerMaxWindowBits = true,
            ServerMaxWindowBits = value,
            OfferClientMaxWindowBits = true,
            HasClientMaxWindowBits = true,
            ClientMaxWindowBits = value
        };
        var clientExtension = new PerMessageDeflateExtension(options);
        var serverExtension = new PerMessageDeflateExtension(options);
        WebSocketExtensionOffer offer = clientExtension.CreateClientExtensionOffer();

        WebSocketExtensionNegotiation accepted =
            serverExtension.AcceptClientExtensionOffer(offer);
        WebSocketExtensionNegotiation validated =
            clientExtension.ValidateServerExtensionResponse(
                accepted.AcceptedConfiguration);
        PerMessageDeflateDirectionSettings server =
            PerMessageDeflateNegotiator.CreateDirectionSettings(
                WebSocketRole.Server,
                accepted.AcceptedConfiguration);
        PerMessageDeflateDirectionSettings client =
            PerMessageDeflateNegotiator.CreateDirectionSettings(
                WebSocketRole.Client,
                accepted.AcceptedConfiguration);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(validated.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(accepted.AcceptedConfiguration.Parameters.Select(
                parameter => parameter.Value),
                Is.EqualTo(new[] { value.ToString(), value.ToString() }));
            Assert.That(server.OutgoingWindowBits, Is.EqualTo(value));
            Assert.That(server.IncomingWindowBits, Is.EqualTo(value));
            Assert.That(client.OutgoingWindowBits, Is.EqualTo(value));
            Assert.That(client.IncomingWindowBits, Is.EqualTo(value));
        });
    }

    [TestCase(false, false, 15, false, 15)]
    [TestCase(true, false, 15, true, 12)]
    [TestCase(true, true, 10, true, 10)]
    public void DistinguishesAbsentValuelessAndValuedClientWindowOffers(
        bool offerParameter,
        bool offerHasValue,
        int offeredValue,
        bool responseHasParameter,
        int responseValue)
    {
        var clientOptions = new PerMessageDeflateOptions
        {
            OfferClientMaxWindowBits = offerParameter,
            HasClientMaxWindowBits = offerHasValue,
            ClientMaxWindowBits = offeredValue
        };
        var serverOptions = new PerMessageDeflateOptions
        {
            OfferClientMaxWindowBits = true,
            HasClientMaxWindowBits = true,
            ClientMaxWindowBits = 12
        };
        var client = new PerMessageDeflateExtension(clientOptions);
        var server = new PerMessageDeflateExtension(serverOptions);
        WebSocketExtensionOffer offer = client.CreateClientExtensionOffer();

        WebSocketExtensionNegotiation accepted =
            server.AcceptClientExtensionOffer(offer);
        WebSocketExtensionNegotiation validated =
            client.ValidateServerExtensionResponse(
                accepted.AcceptedConfiguration);
        WebSocketExtensionParameter? offeredParameter = FindParameter(
            offer,
            "client_max_window_bits");
        WebSocketExtensionParameter? responseParameter = FindParameter(
            accepted.AcceptedConfiguration,
            "client_max_window_bits");

        Assert.Multiple(() =>
        {
            Assert.That(offeredParameter != null, Is.EqualTo(offerParameter));
            if (offeredParameter != null)
            {
                Assert.That(offeredParameter.HasValue, Is.EqualTo(offerHasValue));
            }

            Assert.That(accepted.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(validated.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(responseParameter != null,
                Is.EqualTo(responseHasParameter));
            if (responseParameter != null)
            {
                Assert.That(responseParameter.HasValue, Is.True);
                Assert.That(responseParameter.Value,
                    Is.EqualTo(responseValue.ToString()));
            }
        });
    }

    [TestCase("7")]
    [TestCase("16")]
    [TestCase("08")]
    [TestCase("010")]
    [TestCase("")]
    [TestCase("9x")]
    public void RejectsMalformedWindowValues(string value)
    {
        var server = Offer(Value("server_max_window_bits", value));
        var client = Offer(Value("client_max_window_bits", value));

        Assert.Multiple(() =>
        {
            Assert.That(
                PerMessageDeflateNegotiator.ParseOfferParameters(server).Valid,
                Is.False);
            Assert.That(
                PerMessageDeflateNegotiator.ParseOfferParameters(client).Valid,
                Is.False);
        });
    }

    [Test]
    public void ParserRejectsDuplicatesUnknownParametersAndInvalidValueForms()
    {
        var duplicate = Offer(
            Flag("server_no_context_takeover"),
            Flag("SERVER_NO_CONTEXT_TAKEOVER"));
        var unknown = Offer(Flag("unknown"));
        var valuedFlag = Offer(Value("client_no_context_takeover", "1"));
        var valuelessServerWindow = Offer(Flag("server_max_window_bits"));
        var valuelessClientResponse = Offer(Flag("client_max_window_bits"));

        Assert.Multiple(() =>
        {
            Assert.That(
                PerMessageDeflateNegotiator.ParseOfferParameters(duplicate).Valid,
                Is.False);
            Assert.That(
                PerMessageDeflateNegotiator.ParseOfferParameters(unknown).Valid,
                Is.False);
            Assert.That(
                PerMessageDeflateNegotiator.ParseOfferParameters(valuedFlag).Valid,
                Is.False);
            Assert.That(
                PerMessageDeflateNegotiator.ParseOfferParameters(
                    valuelessServerWindow).Valid,
                Is.False);
            Assert.That(
                PerMessageDeflateNegotiator.ParseOfferParameters(
                    valuelessClientResponse).Valid,
                Is.True);
            Assert.That(
                PerMessageDeflateNegotiator.ParseResponseParameters(
                    valuelessClientResponse).Valid,
                Is.False);
        });
    }

    [TestCase("server_no_context_takeover", false)]
    [TestCase("client_no_context_takeover", false)]
    [TestCase("server_max_window_bits", true)]
    [TestCase("client_max_window_bits", true)]
    public void RejectsDuplicatesOfEveryDefinedParameter(
        string name,
        bool hasValue)
    {
        WebSocketExtensionParameter first = hasValue
            ? Value(name, "12")
            : Flag(name);
        WebSocketExtensionParameter second = hasValue
            ? Value(name.ToUpperInvariant(), "11")
            : Flag(name.ToUpperInvariant());
        WebSocketExtensionOffer duplicate = Offer(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(
                PerMessageDeflateNegotiator.ParseOfferParameters(duplicate).Valid,
                Is.False);
            Assert.That(
                PerMessageDeflateNegotiator.ParseResponseParameters(duplicate).Valid,
                Is.False);
        });
    }

    [Test]
    public void ServerSelectsTheFirstCompatibleAlternative()
    {
        var options = new PerMessageDeflateOptions();
        options.OfferClientMaxWindowBits = true;
        options.HasClientMaxWindowBits = true;
        options.ClientMaxWindowBits = 13;
        WebSocketExtensionOffer[] alternatives =
        [
            Offer(Value("server_max_window_bits", "10")),
            Offer(
                Flag("client_max_window_bits"),
                Flag("client_max_window_bits")),
            Offer(Flag("client_max_window_bits"))
        ];

        var result = PerMessageDeflateNegotiator.SelectServerOffer(
            alternatives,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(result.ClaimsRsv1, Is.True);
            Assert.That(result.AcceptedConfiguration.Parameters,
                Has.Length.EqualTo(1));
            Assert.That(result.AcceptedConfiguration.Parameters[0].Name,
                Is.EqualTo("client_max_window_bits"));
            Assert.That(result.AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("13"));
        });
    }

    [Test]
    public void ServerNegotiatesLimitsAndMapsThemByTrafficDirection()
    {
        var options = new PerMessageDeflateOptions();
        options.OfferServerNoContextTakeover = true;
        options.OfferClientNoContextTakeover = true;
        options.OfferServerMaxWindowBits = true;
        options.HasServerMaxWindowBits = true;
        options.ServerMaxWindowBits = 12;
        options.OfferClientMaxWindowBits = true;
        options.HasClientMaxWindowBits = true;
        options.ClientMaxWindowBits = 10;
        var offer = Offer(
            Flag("server_no_context_takeover"),
            Flag("client_no_context_takeover"),
            Value("server_max_window_bits", "14"),
            Value("client_max_window_bits", "11"));

        var result = PerMessageDeflateNegotiator.AcceptServerOffer(offer, options);
        var server = PerMessageDeflateNegotiator.CreateDirectionSettings(
            WebSocketRole.Server,
            result.AcceptedConfiguration);
        var client = PerMessageDeflateNegotiator.CreateDirectionSettings(
            WebSocketRole.Client,
            result.AcceptedConfiguration);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(result.AcceptedConfiguration.Parameters.Select(parameter =>
                parameter.Name), Is.EqualTo(new[]
                {
                    "server_no_context_takeover",
                    "client_no_context_takeover",
                    "server_max_window_bits",
                    "client_max_window_bits"
                }));
            Assert.That(result.AcceptedConfiguration.Parameters[2].Value,
                Is.EqualTo("12"));
            Assert.That(result.AcceptedConfiguration.Parameters[3].Value,
                Is.EqualTo("10"));
            Assert.That(server.Valid, Is.True);
            Assert.That(server.OutgoingWindowBits, Is.EqualTo(12));
            Assert.That(server.IncomingWindowBits, Is.EqualTo(10));
            Assert.That(server.OutgoingContextTakeover, Is.False);
            Assert.That(server.IncomingContextTakeover, Is.False);
            Assert.That(client.OutgoingWindowBits, Is.EqualTo(10));
            Assert.That(client.IncomingWindowBits, Is.EqualTo(12));
            Assert.That(client.OutgoingContextTakeover, Is.False);
            Assert.That(client.IncomingContextTakeover, Is.False);
        });
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void MapsEveryContextTakeoverCombinationForBothRoles(
        bool serverNoContextTakeover,
        bool clientNoContextTakeover)
    {
        var clientOptions = new PerMessageDeflateOptions();
        var serverOptions = new PerMessageDeflateOptions
        {
            OfferServerNoContextTakeover = serverNoContextTakeover,
            OfferClientNoContextTakeover = clientNoContextTakeover
        };
        var clientExtension = new PerMessageDeflateExtension(clientOptions);
        var serverExtension = new PerMessageDeflateExtension(serverOptions);
        WebSocketExtensionOffer offer = clientExtension.CreateClientExtensionOffer();

        WebSocketExtensionNegotiation accepted =
            serverExtension.AcceptClientExtensionOffer(offer);
        WebSocketExtensionNegotiation validated =
            clientExtension.ValidateServerExtensionResponse(
                accepted.AcceptedConfiguration);
        PerMessageDeflateDirectionSettings server =
            PerMessageDeflateNegotiator.CreateDirectionSettings(
                WebSocketRole.Server,
                accepted.AcceptedConfiguration);
        PerMessageDeflateDirectionSettings client =
            PerMessageDeflateNegotiator.CreateDirectionSettings(
                WebSocketRole.Client,
                accepted.AcceptedConfiguration);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(validated.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(server.OutgoingContextTakeover,
                Is.EqualTo(!serverNoContextTakeover));
            Assert.That(server.IncomingContextTakeover,
                Is.EqualTo(!clientNoContextTakeover));
            Assert.That(client.OutgoingContextTakeover,
                Is.EqualTo(!clientNoContextTakeover));
            Assert.That(client.IncomingContextTakeover,
                Is.EqualTo(!serverNoContextTakeover));
        });
    }

    [Test]
    public void ServerMayIntroduceThreeParametersButNotClientWindowBits()
    {
        var options = new PerMessageDeflateOptions();
        options.OfferServerNoContextTakeover = true;
        options.OfferClientNoContextTakeover = true;
        options.OfferServerMaxWindowBits = true;
        options.HasServerMaxWindowBits = true;
        options.ServerMaxWindowBits = 11;
        options.OfferClientMaxWindowBits = true;

        var result = PerMessageDeflateNegotiator.AcceptServerOffer(
            Offer(),
            options);

        Assert.That(result.AcceptedConfiguration.Parameters.Select(parameter =>
            parameter.Name), Is.EqualTo(new[]
            {
                "server_no_context_takeover",
                "client_no_context_takeover",
                "server_max_window_bits"
            }));
    }

    [Test]
    public void ClientMatchesAResponseAgainstAnyCompatibleAlternative()
    {
        var options = new PerMessageDeflateOptions();
        WebSocketExtensionOffer[] alternatives =
        [
            Offer(Value("server_max_window_bits", "10")),
            Offer(Flag("client_max_window_bits"))
        ];

        var bare = PerMessageDeflateNegotiator.ValidateServerResponse(
            Offer(),
            alternatives,
            options);
        var clientLimit = PerMessageDeflateNegotiator.ValidateServerResponse(
            Offer(Value("client_max_window_bits", "12")),
            alternatives,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(bare.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(clientLimit.Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(clientLimit.ClaimsRsv1, Is.True);
        });
    }

    [TestCase("client-window-unoffered")]
    [TestCase("server-window-too-large")]
    [TestCase("server-window-missing")]
    [TestCase("client-window-too-large")]
    [TestCase("required-server-takeover")]
    [TestCase("required-client-takeover")]
    public void ClientRejectsIncompatibleResponses(string mutation)
    {
        var options = new PerMessageDeflateOptions();
        WebSocketExtensionOffer offered;
        WebSocketExtensionOffer response;
        if (mutation == "client-window-unoffered")
        {
            offered = Offer();
            response = Offer(Value("client_max_window_bits", "10"));
        }
        else if (mutation == "server-window-too-large")
        {
            offered = Offer(Value("server_max_window_bits", "10"));
            response = Offer(Value("server_max_window_bits", "11"));
        }
        else if (mutation == "server-window-missing")
        {
            offered = Offer(Value("server_max_window_bits", "10"));
            response = Offer();
        }
        else if (mutation == "client-window-too-large")
        {
            offered = Offer(Value("client_max_window_bits", "10"));
            response = Offer(Value("client_max_window_bits", "11"));
        }
        else if (mutation == "required-server-takeover")
        {
            options.OfferServerNoContextTakeover = true;
            options.RequireServerNoContextTakeover = true;
            offered = Offer();
            response = Offer();
        }
        else
        {
            options.OfferClientNoContextTakeover = true;
            options.RequireClientNoContextTakeover = true;
            offered = Offer();
            response = Offer();
        }

        var result = PerMessageDeflateNegotiator.ValidateServerResponse(
            response,
            [offered],
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketExtensionStatus.Invalid));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Compression));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("permessage-deflate-response-invalid"));
        });
    }

    static WebSocketExtensionOffer Offer(
        params WebSocketExtensionParameter[] parameters)
    {
        return new WebSocketExtensionOffer
        {
            Name = "permessage-deflate",
            Parameters = parameters
        };
    }

    static WebSocketExtensionParameter Flag(string name)
    {
        return new WebSocketExtensionParameter { Name = name };
    }

    static WebSocketExtensionParameter Value(string name, string value)
    {
        return new WebSocketExtensionParameter
        {
            Name = name,
            HasValue = true,
            Value = value
        };
    }

    static PerMessageDeflateOptions OptionsForParameterMask(int mask)
    {
        return new PerMessageDeflateOptions
        {
            OfferServerNoContextTakeover = (mask & 1) != 0,
            OfferClientNoContextTakeover = (mask & 2) != 0,
            OfferServerMaxWindowBits = (mask & 4) != 0,
            HasServerMaxWindowBits = (mask & 4) != 0,
            ServerMaxWindowBits = 12,
            OfferClientMaxWindowBits = (mask & 8) != 0,
            HasClientMaxWindowBits = (mask & 8) != 0,
            ClientMaxWindowBits = 11
        };
    }

    static string[] ParameterNamesForMask(int mask)
    {
        var names = new List<string>();
        if ((mask & 1) != 0)
        {
            names.Add("server_no_context_takeover");
        }

        if ((mask & 2) != 0)
        {
            names.Add("client_no_context_takeover");
        }

        if ((mask & 4) != 0)
        {
            names.Add("server_max_window_bits");
        }

        if ((mask & 8) != 0)
        {
            names.Add("client_max_window_bits");
        }

        return names.ToArray();
    }

    static WebSocketExtensionParameter? FindParameter(
        WebSocketExtensionOffer offer,
        string name)
    {
        return offer.Parameters.FirstOrDefault(parameter => parameter.Name == name);
    }
}
