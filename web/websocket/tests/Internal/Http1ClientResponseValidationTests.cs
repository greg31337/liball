using websocket;

namespace websocket.Tests;

public class Http1ClientResponseValidationTests
{
    [Test]
    public void AcceptsCompleteUpgradeWithExactlyOfferedNegotiationResults()
    {
        var state = new Http1ClientHandshakeState();
        var options = Options();
        options.Subprotocols = ["chat", "superchat"];
        options.Extensions =
        [
            Offer("x-test", Parameter("mode", "fast")),
            Offer("x-test", Parameter("mode", "slow"))
        ];
        state.CreateRequest(options);
        var response = Success(state);
        response.Fields =
        [
            Field("Upgrade", "h2c, WebSocket"),
            Field("Connection", "keep-alive, upgrade"),
            Field("Sec-WebSocket-Accept", state.ExpectedAccept),
            Field("Sec-WebSocket-Protocol", "chat"),
            Field("Sec-WebSocket-Extensions", "x-test; mode=\"slow\"")
        ];

        var result = state.ValidateResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response, Is.Not.SameAs(response));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.ClientKey, Is.Not.Empty);
            Assert.That(result.HasSelectedSubprotocol, Is.True);
            Assert.That(result.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(result.Extensions[0].Name, Is.EqualTo("x-test"));
            Assert.That(result.Extensions[0].Status,
                Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(result.Extensions[0].HasAcceptedConfiguration, Is.True);
            Assert.That(result.Extensions[0].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("slow"));
            Assert.That(result.HasError, Is.False);
            AssertCleared(state);
        });
    }

    [Test]
    public void AcceptsUpgradeWhenOptionalNegotiationsAreOmitted()
    {
        var state = new Http1ClientHandshakeState();
        var options = Options();
        options.Subprotocols = ["chat"];
        options.Extensions = [Offer("x-test")];
        state.CreateRequest(options);

        var result = state.ValidateResponse(Success(state));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.HasSelectedSubprotocol, Is.False);
            Assert.That(result.SelectedSubprotocol, Is.Empty);
            Assert.That(result.Extensions, Is.Empty);
        });
    }

    [Test]
    public void InformationalResponseIsReportedWithoutConsumingPendingState()
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var informational = new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = 103,
            HasReasonPhrase = true,
            ReasonPhrase = "Early Hints",
            Fields = [Field("Link", "</style.css>; rel=preload")]
        };

        var interim = state.ValidateResponse(informational);
        var accepted = state.ValidateResponse(Success(state));

        Assert.Multiple(() =>
        {
            Assert.That(interim.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Incomplete));
            Assert.That(interim.HasResponse, Is.True);
            Assert.That(interim.Response.StatusCode, Is.EqualTo(103));
            Assert.That(interim.HasError, Is.False);
            Assert.That(accepted.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
        });
    }

    [Test]
    public void FinalNon101ResponseIsReportedAsADeepCopiedRejection()
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var body = new byte[] { 1, 2, 3 };
        var response = new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = 401,
            HasReasonPhrase = true,
            ReasonPhrase = "Unauthorized",
            Fields = [Field("WWW-Authenticate", "Basic realm=test")],
            HasBody = true,
            Body = body
        };

        var result = state.ValidateResponse(response);
        response.Fields[0].Value = "mutated";
        body[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response.StatusCode, Is.EqualTo(401));
            Assert.That(result.Response.Fields[0].Value, Is.EqualTo("Basic realm=test"));
            Assert.That(result.Response.Body, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.HasError, Is.False);
            AssertCleared(state);
        });
    }

    [TestCase(200)]
    [TestCase(204)]
    [TestCase(300)]
    [TestCase(399)]
    [TestCase(400)]
    [TestCase(499)]
    [TestCase(500)]
    [TestCase(599)]
    public void EveryFinalNon101StatusCategoryIsReportedAsARejection(int statusCode)
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var response = new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = statusCode,
            HasReasonPhrase = true,
            ReasonPhrase = "Final Response"
        };

        var result = state.ValidateResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response.StatusCode, Is.EqualTo(statusCode));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.HasError, Is.False);
            AssertCleared(state);
        });
    }

    [TestCase("missing-upgrade", "websocket-response-upgrade-invalid")]
    [TestCase("wrong-upgrade", "websocket-response-upgrade-invalid")]
    [TestCase("missing-connection", "websocket-response-connection-invalid")]
    [TestCase("wrong-connection", "websocket-response-connection-invalid")]
    [TestCase("missing-accept", "websocket-response-accept-invalid")]
    [TestCase("wrong-accept", "websocket-response-accept-invalid")]
    [TestCase("duplicate-accept", "websocket-response-accept-invalid")]
    public void RejectsMissingOrIncorrectRequiredUpgradeFields(
        string mutation,
        string expectedIdentifier)
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var response = Success(state);
        MutateRequiredField(response, mutation);

        var result = state.ValidateResponse(response);

        AssertFailure(result, expectedIdentifier);
        AssertCleared(state);
    }

    [TestCase("Upgrade", "web socket", "websocket-response-upgrade-invalid")]
    [TestCase("Connection", "keep alive", "websocket-response-connection-invalid")]
    [TestCase("Upgrade", "websocket; version=13", "websocket-response-upgrade-invalid")]
    [TestCase("Connection", "keep-alive, \"Upgrade\"", "websocket-response-connection-invalid")]
    public void RejectsMalformedRequiredTokenLists(
        string fieldName,
        string fieldValue,
        string expectedIdentifier)
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var response = Success(state);
        response.Fields.Single(field => field.Name == fieldName).Value = fieldValue;

        var result = state.ValidateResponse(response);

        AssertFailure(result, expectedIdentifier);
        AssertCleared(state);
    }

    [TestCase("Chat")]
    [TestCase("unoffered")]
    [TestCase("chat, superchat")]
    [TestCase("")]
    public void RejectsCaseMismatchedUnofferedMultipleOrEmptySubprotocols(string selected)
    {
        var state = new Http1ClientHandshakeState();
        var options = Options();
        options.Subprotocols = ["chat", "superchat"];
        state.CreateRequest(options);
        var response = Success(state);
        response.Fields = response.Fields.Append(
            Field("Sec-WebSocket-Protocol", selected)).ToArray();

        var result = state.ValidateResponse(response);

        AssertFailure(result, "websocket-response-subprotocol-invalid");
    }

    [Test]
    public void RejectsRepeatedSubprotocolResponseFields()
    {
        var state = new Http1ClientHandshakeState();
        var options = Options();
        options.Subprotocols = ["chat"];
        state.CreateRequest(options);
        var response = Success(state);
        response.Fields = response.Fields.Concat(new[]
        {
            Field("Sec-WebSocket-Protocol", "chat"),
            Field("sec-websocket-protocol", "chat")
        }).ToArray();

        var result = state.ValidateResponse(response);

        AssertFailure(result, "websocket-response-subprotocol-invalid");
    }

    [Test]
    public void RejectsAnUnsolicitedSubprotocolWhenNoneWasOffered()
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var response = Success(state);
        response.Fields = response.Fields.Append(
            Field("Sec-WebSocket-Protocol", "chat")).ToArray();

        var result = state.ValidateResponse(response);

        AssertFailure(result, "websocket-response-subprotocol-invalid");
        AssertCleared(state);
    }

    [TestCase("unoffered")]
    [TestCase("x-test; mode=slow")]
    [TestCase("x-test; mode=fast; extra")]
    [TestCase("x-test; mode=\"bad value\"")]
    public void RejectsUnofferedOrMalformedExtensionConfigurations(string extensionValue)
    {
        var state = new Http1ClientHandshakeState();
        var options = Options();
        options.Extensions = [Offer("x-test", Parameter("mode", "fast"))];
        state.CreateRequest(options);
        var response = Success(state);
        response.Fields = response.Fields.Append(
            Field("Sec-WebSocket-Extensions", extensionValue)).ToArray();

        var result = state.ValidateResponse(response);

        AssertFailure(result, "websocket-response-extensions-invalid");
    }

    [Test]
    public void RejectsRepeatedExtensionFieldsAndDuplicateNegotiatedNames()
    {
        var repeatedState = new Http1ClientHandshakeState();
        var repeatedOptions = Options();
        repeatedOptions.Extensions = [Offer("one"), Offer("two")];
        repeatedState.CreateRequest(repeatedOptions);
        var repeated = Success(repeatedState);
        repeated.Fields = repeated.Fields.Concat(new[]
        {
            Field("Sec-WebSocket-Extensions", "one"),
            Field("sec-websocket-extensions", "two")
        }).ToArray();
        var duplicateState = new Http1ClientHandshakeState();
        var duplicateOptions = Options();
        duplicateOptions.Extensions = [Offer("x-test"), Offer("x-test")];
        duplicateState.CreateRequest(duplicateOptions);
        var duplicate = Success(duplicateState);
        duplicate.Fields = duplicate.Fields.Append(
            Field("Sec-WebSocket-Extensions", "x-test, x-test")).ToArray();

        var repeatedResult = repeatedState.ValidateResponse(repeated);
        var duplicateResult = duplicateState.ValidateResponse(duplicate);

        Assert.Multiple(() =>
        {
            AssertFailure(repeatedResult, "websocket-response-extensions-invalid");
            AssertFailure(duplicateResult, "websocket-response-extensions-invalid");
        });
    }

    [Test]
    public void RejectsAnUnsolicitedExtensionWhenNoneWasOffered()
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var response = Success(state);
        response.Fields = response.Fields.Append(
            Field("Sec-WebSocket-Extensions", "x-test")).ToArray();

        var result = state.ValidateResponse(response);

        AssertFailure(result, "websocket-response-extensions-invalid");
        AssertCleared(state);
    }

    [TestCase("Transfer-Encoding", "chunked")]
    [TestCase("Content-Length", "1")]
    [TestCase("Host", "example.com")]
    [TestCase("Sec-WebSocket-Key", "value")]
    [TestCase("Origin", "https://example.com")]
    [TestCase("Sec-WebSocket-Version", "13")]
    public void RejectsBodyOrRequestOnlySemanticsOnA101Response(
        string fieldName,
        string fieldValue)
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var response = Success(state);
        response.Fields = response.Fields.Append(Field(fieldName, fieldValue)).ToArray();

        var result = state.ValidateResponse(response);

        AssertFailure(result, "websocket-response-metadata-invalid");
    }

    [Test]
    public void InvalidMetadataFailsAndValidationWithoutPendingStateThrows()
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var invalid = Success(state);
        invalid.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;

        var result = state.ValidateResponse(invalid);

        Assert.Multiple(() =>
        {
            AssertFailure(result, "websocket-response-metadata-invalid");
            Assert.Throws<WebSocketStateException>(() => state.ValidateResponse(invalid));
            Assert.Throws<WebSocketStateException>(() =>
                new Http1ClientHandshakeState().ValidateResponse(invalid));
        });
    }

    [TestCase("status-below-range")]
    [TestCase("status-above-range")]
    [TestCase("null-reason")]
    [TestCase("empty-present-reason")]
    [TestCase("unexpected-reason")]
    [TestCase("unsafe-reason")]
    [TestCase("null-fields")]
    [TestCase("null-body")]
    [TestCase("unexpected-body")]
    [TestCase("informational-body")]
    [TestCase("null-field")]
    [TestCase("null-field-name")]
    [TestCase("null-field-value")]
    [TestCase("invalid-field-name")]
    [TestCase("unsafe-field-value")]
    [TestCase("untrimmed-field-value")]
    [TestCase("duplicate-content-length")]
    public void RejectsEveryStructurallyInvalidResponseMetadataForm(string mutation)
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());
        var response = Success(state);
        MutateMetadata(response, mutation);

        var result = state.ValidateResponse(response);

        AssertFailure(result, "websocket-response-metadata-invalid");
        Assert.Multiple(() =>
        {
            Assert.That(result.HasResponse, Is.False);
            AssertCleared(state);
        });
    }

    [Test]
    public void RejectsNullResponseMetadataAndClearsPendingState()
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options());

        var result = state.ValidateResponse(null!);

        AssertFailure(result, "websocket-response-metadata-invalid");
        AssertCleared(state);
    }

    static WebSocketClientHandshakeOptions Options()
    {
        var options = new WebSocketClientHandshakeOptions();
        options.Uri = "wss://example.com/chat";
        return options;
    }

    static WebSocketHttpResponse Success(Http1ClientHandshakeState state)
    {
        return new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = 101,
            HasReasonPhrase = true,
            ReasonPhrase = "Switching Protocols",
            Fields =
            [
                Field("Upgrade", "websocket"),
                Field("Connection", "Upgrade"),
                Field("Sec-WebSocket-Accept", state.ExpectedAccept)
            ]
        };
    }

    static void MutateRequiredField(WebSocketHttpResponse response, string mutation)
    {
        if (mutation == "missing-upgrade")
        {
            response.Fields = response.Fields.Where(field => field.Name != "Upgrade").ToArray();
        }
        else if (mutation == "wrong-upgrade")
        {
            response.Fields.Single(field => field.Name == "Upgrade").Value = "not-websocket";
        }
        else if (mutation == "missing-connection")
        {
            response.Fields = response.Fields.Where(field => field.Name != "Connection").ToArray();
        }
        else if (mutation == "wrong-connection")
        {
            response.Fields.Single(field => field.Name == "Connection").Value = "keep-alive";
        }
        else if (mutation == "missing-accept")
        {
            response.Fields = response.Fields.Where(
                field => field.Name != "Sec-WebSocket-Accept").ToArray();
        }
        else if (mutation == "wrong-accept")
        {
            response.Fields.Single(
                field => field.Name == "Sec-WebSocket-Accept").Value =
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        }
        else
        {
            response.Fields = response.Fields.Append(
                Field("sec-websocket-accept", response.Fields.Single(
                    field => field.Name == "Sec-WebSocket-Accept").Value)).ToArray();
        }
    }

    static void MutateMetadata(WebSocketHttpResponse response, string mutation)
    {
        if (mutation == "status-below-range")
        {
            response.StatusCode = 99;
        }
        else if (mutation == "status-above-range")
        {
            response.StatusCode = 600;
        }
        else if (mutation == "null-reason")
        {
            response.ReasonPhrase = null!;
        }
        else if (mutation == "empty-present-reason")
        {
            response.ReasonPhrase = "";
        }
        else if (mutation == "unexpected-reason")
        {
            response.HasReasonPhrase = false;
        }
        else if (mutation == "unsafe-reason")
        {
            response.ReasonPhrase = "Switching\rProtocols";
        }
        else if (mutation == "null-fields")
        {
            response.Fields = null!;
        }
        else if (mutation == "null-body")
        {
            response.Body = null!;
        }
        else if (mutation == "unexpected-body")
        {
            response.Body = [1];
        }
        else if (mutation == "informational-body")
        {
            response.StatusCode = 103;
            response.HasBody = true;
            response.Body = [1];
        }
        else if (mutation == "null-field")
        {
            response.Fields[0] = null!;
        }
        else if (mutation == "null-field-name")
        {
            response.Fields[0].Name = null!;
        }
        else if (mutation == "null-field-value")
        {
            response.Fields[0].Value = null!;
        }
        else if (mutation == "invalid-field-name")
        {
            response.Fields[0].Name = "Bad Name";
        }
        else if (mutation == "unsafe-field-value")
        {
            response.Fields[0].Value = "websocket\nInjected";
        }
        else if (mutation == "untrimmed-field-value")
        {
            response.Fields[0].Value = " websocket";
        }
        else
        {
            response.Fields = response.Fields.Concat(new[]
            {
                Field("Content-Length", "0"),
                Field("content-length", "0")
            }).ToArray();
        }
    }

    static void AssertFailure(WebSocketHandshakeResult result, string identifier)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Error.Diagnostic, Does.Not.Contain("\r").And.Not.Contain("\n"));
        });
    }

    static void AssertCleared(Http1ClientHandshakeState state)
    {
        Assert.Multiple(() =>
        {
            Assert.That(state.HasPendingRequest, Is.False);
            Assert.That(state.ClientKey, Is.Empty);
            Assert.That(state.ExpectedAccept, Is.Empty);
            Assert.That(state.OfferedSubprotocols, Is.Empty);
            Assert.That(state.OfferedExtensions, Is.Empty);
        });
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }

    static WebSocketExtensionOffer Offer(
        string name,
        params WebSocketExtensionParameter[] parameters)
    {
        return new WebSocketExtensionOffer
        {
            Name = name,
            Parameters = parameters
        };
    }

    static WebSocketExtensionParameter Parameter(string name, string value)
    {
        return new WebSocketExtensionParameter
        {
            Name = name,
            HasValue = true,
            Value = value
        };
    }
}
