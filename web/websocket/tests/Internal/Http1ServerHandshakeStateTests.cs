using websocket;

namespace websocket.Tests;

public class Http1ServerHandshakeStateTests
{
    const string ValidKey = "dGhlIHNhbXBsZSBub25jZQ==";

    [Test]
    public void AcceptsAMinimalValidRequestAndRetainsADeepCopy()
    {
        var state = new Http1ServerHandshakeState();
        var request = ValidRequest();
        var result = state.EvaluateRequest(request, new WebSocketServerHandshakeOptions());

        request.Method = "MUTATED";
        request.Fields[0].Value = "mutated.example";

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.Request, Is.Not.SameAs(request));
            Assert.That(result.Request.Method, Is.EqualTo("GET"));
            Assert.That(result.Request.Fields[0].Value, Is.EqualTo("example.com"));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.ClientKey, Is.EqualTo(ValidKey));
            Assert.That(result.HasSelectedSubprotocol, Is.False);
            Assert.That(result.Extensions, Is.Empty);
            Assert.That(result.HasError, Is.False);
            Assert.That(state.HasEvaluation, Is.True);
            Assert.That(state.Accepted, Is.True);
            Assert.That(state.ResponseStatusCode, Is.EqualTo(101));
            Assert.That(state.ClientKey, Is.EqualTo(ValidKey));
            Assert.That(state.AdvertiseVersion, Is.False);
        });
    }

    [TestCase("POST", "HTTP/1.1", true)]
    [TestCase("get", "HTTP/1.1", true)]
    [TestCase("GET", "HTTP/1.0", true)]
    [TestCase("GET", "HTTP/1.1", false)]
    public void RejectsInvalidRequestLineMetadata(string method, string version, bool hasTarget)
    {
        var request = ValidRequest();
        request.Method = method;
        request.HttpVersion = version;
        request.HasRequestTarget = hasTarget;

        var result = new Http1ServerHandshakeState().EvaluateRequest(
            request,
            new WebSocketServerHandshakeOptions());

        AssertFailure(result, 400);
    }

    [TestCase("HTTP/1.1", "/")]
    [TestCase("HTTP/1.2", "/chat?room=blue&mode=fast")]
    [TestCase("HTTP/2.0", "/encoded/%7Euser")]
    [TestCase("HTTP/9.9", "/a/b;c=d")]
    public void AcceptsGetWithHttp11OrNewerAndValidOriginFormTargets(
        string httpVersion,
        string requestTarget)
    {
        var request = ValidRequest();
        request.HttpVersion = httpVersion;
        request.RequestTarget = requestTarget;

        var result = Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Request.HttpVersion, Is.EqualTo(httpVersion));
            Assert.That(result.Request.RequestTarget, Is.EqualTo(requestTarget));
        });
    }

    [TestCase("")]
    [TestCase("*")]
    [TestCase("chat")]
    [TestCase("http://example.com/chat")]
    [TestCase("/chat#fragment")]
    [TestCase("/bad target")]
    [TestCase("/bad%")]
    [TestCase("/bad%2")]
    [TestCase("/bad%GG")]
    [TestCase("/café")]
    public void RejectsTargetsThatAreNotSafeOriginForm(string requestTarget)
    {
        var request = ValidRequest();
        request.RequestTarget = requestTarget;

        var result = Evaluate(request);

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400);
            Assert.That(result.Error.Identifier,
                Is.EqualTo("websocket-request-metadata-invalid"));
        });
    }

    [TestCase("example.com")]
    [TestCase("example.com:8080")]
    [TestCase("192.0.2.10:8000")]
    [TestCase("[2001:db8::1]")]
    [TestCase("[2001:db8::1]:8443")]
    [TestCase("BÜCHER.example")]
    public void AcceptsValidHostAuthorityVariants(string authority)
    {
        var request = ValidRequest();
        Replace(request, "Host", authority);

        var result = Evaluate(request);

        Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
    }

    [TestCase("")]
    [TestCase("bad host")]
    [TestCase("user@example.com")]
    [TestCase("example.com/path")]
    [TestCase("example.com:0")]
    [TestCase("example.com:65536")]
    [TestCase("2001:db8::1")]
    [TestCase("[2001:db8::1")]
    [TestCase("bad_name.example")]
    public void RejectsInvalidHostAuthorityVariants(string authority)
    {
        var request = ValidRequest();
        Replace(request, "Host", authority);

        var result = Evaluate(request);

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400);
            Assert.That(result.Error.Identifier,
                Is.EqualTo("websocket-request-host-invalid"));
        });
    }

    [TestCase("Host", "websocket-request-host-invalid")]
    [TestCase("Upgrade", "websocket-request-upgrade-invalid")]
    [TestCase("Connection", "websocket-request-connection-invalid")]
    [TestCase("Sec-WebSocket-Key", "websocket-request-key-invalid")]
    [TestCase("Sec-WebSocket-Version", "websocket-request-version-invalid")]
    public void RejectsMissingRequiredFields(string fieldName, string identifier)
    {
        var request = ValidRequest();
        request.Fields = request.Fields.Where(field => field.Name != fieldName).ToArray();

        var result = new Http1ServerHandshakeState().EvaluateRequest(
            request,
            new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400);
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
        });
    }

    [Test]
    public void AcceptsUpgradeAndConnectionTokensAcrossRepeatedFields()
    {
        var request = ValidRequest();
        Replace(request, "Upgrade", "h2c");
        Replace(request, "Connection", "keep-alive");
        request.Fields = request.Fields.Concat(new[]
        {
            Field("upgrade", "WebSocket"),
            Field("connection", "close, UPGRADE")
        }).ToArray();

        var result = new Http1ServerHandshakeState().EvaluateRequest(
            request,
            new WebSocketServerHandshakeOptions());

        Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
    }

    [TestCase("Upgrade", "web socket", "websocket-request-upgrade-invalid")]
    [TestCase("Connection", "keep-alive,", "websocket-request-connection-invalid")]
    public void RejectsMalformedOrMissingUpgradeTokens(
        string fieldName,
        string value,
        string identifier)
    {
        var request = ValidRequest();
        Replace(request, fieldName, value);

        var result = new Http1ServerHandshakeState().EvaluateRequest(
            request,
            new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400);
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
        });
    }

    [Test]
    public void RejectsRepeatedHostKeyAndVersionFields()
    {
        var duplicateHost = ValidRequest(Field("host", "example.com"));
        var duplicateKey = ValidRequest(Field("sec-websocket-key", ValidKey));
        var duplicateVersion = ValidRequest(Field("sec-websocket-version", "13"));

        var hostResult = Evaluate(duplicateHost);
        var keyResult = Evaluate(duplicateKey);
        var versionResult = Evaluate(duplicateVersion);

        Assert.Multiple(() =>
        {
            Assert.That(hostResult.Error.Identifier, Is.EqualTo("websocket-request-host-invalid"));
            Assert.That(keyResult.Error.Identifier, Is.EqualTo("websocket-request-key-invalid"));
            Assert.That(versionResult.Error.Identifier, Is.EqualTo("websocket-request-version-invalid"));
        });
    }

    [TestCase("not-base64")]
    [TestCase("ZHVtbXk=")]
    [TestCase("dGhlIHNhbXBsZSBub25jZQ")]
    public void RejectsNonCanonicalOrWrongLengthKeys(string key)
    {
        var request = ValidRequest();
        Replace(request, "Sec-WebSocket-Key", key);

        var result = Evaluate(request);

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400);
            Assert.That(result.Error.Identifier, Is.EqualTo("websocket-request-key-invalid"));
        });
    }

    [Test]
    public void AcceptsTheCorrectedCanonicalRfc6455ErrataKey()
    {
        const string correctedKey = "AQIDBAUGBwgJCgsMDQ4PEA==";
        const string expectedAccept = "C/0nmHhBztSRGR1CwL6Tf4ZjwpY=";
        var request = ValidRequest();
        Replace(request, "Sec-WebSocket-Key", correctedKey);

        var result = Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.ClientKey, Is.EqualTo(correctedKey));
            Assert.That(result.Response.Fields.Single(field =>
                field.Name == "Sec-WebSocket-Accept").Value, Is.EqualTo(expectedAccept));
        });
    }

    [TestCase("")]
    [TestCase("013")]
    [TestCase("13, 8")]
    [TestCase("256")]
    public void RejectsMalformedVersionValues(string version)
    {
        var request = ValidRequest();
        Replace(request, "Sec-WebSocket-Version", version);

        var result = Evaluate(request);

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400);
            Assert.That(result.Error.Identifier, Is.EqualTo("websocket-request-version-invalid"));
        });
    }

    [Test]
    public void UnsupportedVersionProducesA426RejectionAndAdvertisementState()
    {
        var state = new Http1ServerHandshakeState();
        var request = ValidRequest();
        Replace(request, "Sec-WebSocket-Version", "8");

        var result = state.EvaluateRequest(request, new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            AssertRejection(result, 426);
            Assert.That(result.Error.Identifier, Is.EqualTo("websocket-version-unsupported"));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(state.AdvertiseVersion, Is.True);
            Assert.That(state.Accepted, Is.False);
        });
    }

    [TestCase("0")]
    [TestCase("8")]
    [TestCase("12")]
    [TestCase("14")]
    [TestCase("255")]
    public void EveryWellFormedUnsupportedWebSocketVersionProduces426(string version)
    {
        var request = ValidRequest();
        Replace(request, "Sec-WebSocket-Version", version);

        var result = Evaluate(request);

        Assert.Multiple(() =>
        {
            AssertRejection(result, 426);
            Assert.That(result.Error.Identifier, Is.EqualTo("websocket-version-unsupported"));
            Assert.That(result.Response.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "Sec-WebSocket-Version" }));
            Assert.That(result.Response.Fields[0].Value, Is.EqualTo("13"));
        });
    }

    [TestCase("null-method")]
    [TestCase("invalid-method")]
    [TestCase("null-target")]
    [TestCase("missing-http-version")]
    [TestCase("null-http-version")]
    [TestCase("malformed-http-version")]
    [TestCase("unexpected-scheme")]
    [TestCase("unexpected-authority")]
    [TestCase("unexpected-path")]
    [TestCase("unexpected-protocol")]
    [TestCase("null-fields")]
    [TestCase("null-field")]
    [TestCase("null-field-name")]
    [TestCase("null-field-value")]
    [TestCase("invalid-field-name")]
    [TestCase("unsafe-field-value")]
    public void RejectsEveryUnsafeRequestMetadataShape(string mutation)
    {
        var request = ValidRequest();
        MutateMetadata(request, mutation);

        var result = Evaluate(request);

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400);
            Assert.That(result.Error.Identifier,
                Is.EqualTo("websocket-request-metadata-invalid"));
            Assert.That(result.Response.StatusCode, Is.EqualTo(400));
        });
    }

    [Test]
    public void NullRequestProducesSafe400MetadataFailure()
    {
        var result = new Http1ServerHandshakeState().EvaluateRequest(
            null!,
            new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400);
            Assert.That(result.Error.Identifier,
                Is.EqualTo("websocket-request-metadata-invalid"));
            Assert.That(result.HasRequest, Is.False);
            Assert.That(result.HasResponse, Is.True);
        });
    }

    [Test]
    public void DefaultOriginPolicyAllowsSameOriginAndRejectsCrossOrigin()
    {
        var same = Evaluate(ValidRequest(Field("Origin", "http://EXAMPLE.com:80")));
        var cross = Evaluate(ValidRequest(Field("Origin", "https://other.example")));

        Assert.Multiple(() =>
        {
            Assert.That(same.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            AssertRejection(cross, 403);
            Assert.That(cross.Error.Identifier, Is.EqualTo("websocket-origin-rejected"));
        });
    }

    [TestCase("")]
    [TestCase("https://example.com/path")]
    [TestCase("https://one.example https://two.example")]
    public void MalformedOriginIsA400Failure(string origin)
    {
        var result = Evaluate(ValidRequest(Field("Origin", origin)));

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400);
            Assert.That(result.Error.Identifier, Is.EqualTo("websocket-request-origin-invalid"));
        });
    }

    [Test]
    public void ApplicationOriginPolicyControlsRejectionMetadataWithDefensiveCopies()
    {
        var state = new Http1ServerHandshakeState();
        var policy = new RecordingOriginPolicy(false);
        var rejectionField = Field("X-Rejection", "original");
        var rejectionBody = new byte[] { 1, 2, 3 };
        var options = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = policy,
            RejectionStatusCode = 404,
            RejectionFields = [rejectionField],
            HasRejectionBody = true,
            RejectionBody = rejectionBody
        };

        var result = state.EvaluateRequest(ValidRequest(), options);
        rejectionField.Value = "mutated";
        rejectionBody[0] = 9;

        Assert.Multiple(() =>
        {
            AssertRejection(result, 404);
            Assert.That(policy.Request, Is.Not.Null);
            Assert.That(policy.Request!.Fields, Is.Not.SameAs(result.Request.Fields));
            Assert.That(state.ResponseFields[0].Value, Is.EqualTo("original"));
            Assert.That(state.HasResponseBody, Is.True);
            Assert.That(state.ResponseBody, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void DefaultAndServerOrderSelectorsPreserveConfiguredPreference()
    {
        var request = ValidRequest(Field("Sec-WebSocket-Protocol", "video, chat"));
        var defaults = new WebSocketServerHandshakeOptions
        {
            SupportedSubprotocols = ["chat", "video"]
        };
        var custom = new WebSocketServerHandshakeOptions
        {
            SupportedSubprotocols = ["chat", "video"],
            HasSubprotocolSelector = true,
            SubprotocolSelector = new DefaultWebSocketSubprotocolSelector(true)
        };

        var clientOrder = new Http1ServerHandshakeState().EvaluateRequest(request, defaults);
        var selected = new Http1ServerHandshakeState().EvaluateRequest(request, custom);

        Assert.Multiple(() =>
        {
            Assert.That(clientOrder.SelectedSubprotocol, Is.EqualTo("video"));
            Assert.That(selected.SelectedSubprotocol, Is.EqualTo("chat"));
        });
    }

    [Test]
    public void NoOverlapAndCaseOnlyDifferencesAcceptWithoutSelection()
    {
        var noOverlapOptions = new WebSocketServerHandshakeOptions
        {
            SupportedSubprotocols = ["mqtt", "binary"]
        };
        var noOverlap = new Http1ServerHandshakeState().EvaluateRequest(
            ValidRequest(Field("Sec-WebSocket-Protocol", "chat, superchat")),
            noOverlapOptions);
        var caseOptions = new WebSocketServerHandshakeOptions
        {
            SupportedSubprotocols = ["chat"]
        };
        var caseOnly = new Http1ServerHandshakeState().EvaluateRequest(
            ValidRequest(Field("Sec-WebSocket-Protocol", "CHAT")),
            caseOptions);

        Assert.Multiple(() =>
        {
            Assert.That(noOverlap.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(noOverlap.HasSelectedSubprotocol, Is.False);
            Assert.That(noOverlap.SelectedSubprotocol, Is.Empty);
            Assert.That(noOverlap.Response.Fields.Any(field =>
                field.Name == "Sec-WebSocket-Protocol"), Is.False);
            Assert.That(caseOnly.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(caseOnly.HasSelectedSubprotocol, Is.False);
        });
    }

    [Test]
    public void RejectsMalformedDuplicateOrInvalidlySelectedSubprotocols()
    {
        var malformed = Evaluate(ValidRequest(Field("Sec-WebSocket-Protocol", "chat, bad protocol")));
        var duplicate = Evaluate(ValidRequest(
            Field("Sec-WebSocket-Protocol", "chat"),
            Field("sec-websocket-protocol", "chat")));
        var options = new WebSocketServerHandshakeOptions
        {
            SupportedSubprotocols = ["chat"],
            HasSubprotocolSelector = true,
            SubprotocolSelector = new SelectingSubprotocolSelector("other")
        };
        var invalidSelection = new Http1ServerHandshakeState().EvaluateRequest(
            ValidRequest(Field("Sec-WebSocket-Protocol", "chat")),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Error.Identifier,
                Is.EqualTo("websocket-request-subprotocol-invalid"));
            Assert.That(duplicate.Error.Identifier,
                Is.EqualTo("websocket-request-subprotocol-invalid"));
            Assert.That(invalidSelection.Error.Identifier,
                Is.EqualTo("websocket-subprotocol-selection-invalid"));
        });
    }

    [Test]
    public void NegotiatesExactExtensionOffersInServerPreferenceOrder()
    {
        var state = new Http1ServerHandshakeState();
        var fast = Offer("x-mode", Parameter("speed", "fast"));
        var plain = Offer("x-plain");
        var options = new WebSocketServerHandshakeOptions
        {
            SupportedExtensions = [plain, fast]
        };
        var request = ValidRequest(
            Field("Sec-WebSocket-Extensions", "x-mode; speed=fast"),
            Field("sec-websocket-extensions", "x-plain, x-unsupported"));

        var result = state.EvaluateRequest(request, options);
        plain.Name = "mutated";
        fast.Parameters[0].Value = "slow";

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Extensions.Select(item => item.Name),
                Is.EqualTo(new[] { "x-plain", "x-mode" }));
            Assert.That(result.Extensions[1].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("fast"));
            Assert.That(state.NegotiatedExtensions[0].Name, Is.EqualTo("x-plain"));
            Assert.That(state.NegotiatedExtensions[1].AcceptedConfiguration.Parameters[0].Value,
                Is.EqualTo("fast"));
        });
    }

    [Test]
    public void DeclinesUnsupportedExtensionsAndRejectsMalformedOffers()
    {
        var declined = Evaluate(ValidRequest(
            Field("Sec-WebSocket-Extensions", "x-unsupported")));
        var malformed = Evaluate(ValidRequest(
            Field("Sec-WebSocket-Extensions", "x-test; =bad")));

        Assert.Multiple(() =>
        {
            Assert.That(declined.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(declined.Extensions, Is.Empty);
            AssertFailure(malformed, 400);
            Assert.That(malformed.Error.Identifier,
                Is.EqualTo("websocket-request-extensions-invalid"));
        });
    }

    [TestCase("Upgrade")]
    [TestCase("Sec-WebSocket-Accept")]
    [TestCase("Sec-WebSocket-Version")]
    [TestCase("Content-Length")]
    public void RejectsCriticalCallerResponseFields(string fieldName)
    {
        var successOptions = new WebSocketServerHandshakeOptions
        {
            ExtraFields = [Field(fieldName.ToLowerInvariant(), "value")]
        };
        var rejectionOptions = new WebSocketServerHandshakeOptions
        {
            RejectionFields = [Field(fieldName.ToLowerInvariant(), "value")]
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() =>
                new Http1ServerHandshakeState().EvaluateRequest(ValidRequest(), successOptions));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new Http1ServerHandshakeState().EvaluateRequest(ValidRequest(), rejectionOptions));
        });
    }

    [Test]
    public void RejectsConfigurationThatDoesNotSupportHttp1AndClearsPriorState()
    {
        var state = new Http1ServerHandshakeState();
        state.EvaluateRequest(ValidRequest(), new WebSocketServerHandshakeOptions());
        var options = new WebSocketServerHandshakeOptions
        {
            SupportedTransports = [WebSocketHandshakeTransport.Http2ExtendedConnect],
            ExtendedConnectEnabled = true
        };

        Assert.Throws<WebSocketHandshakeException>(() => state.EvaluateRequest(ValidRequest(), options));
        Assert.Multiple(() =>
        {
            Assert.That(state.HasEvaluation, Is.False);
            Assert.That(state.Accepted, Is.False);
            Assert.That(state.ClientKey, Is.Empty);
            Assert.That(state.ResponseFields, Is.Empty);
        });
    }

    static WebSocketHandshakeResult Evaluate(WebSocketHttpRequest request)
    {
        return new Http1ServerHandshakeState().EvaluateRequest(
            request,
            new WebSocketServerHandshakeOptions());
    }

    static WebSocketHttpRequest ValidRequest(params WebSocketHeaderField[] additionalFields)
    {
        var fields = new List<WebSocketHeaderField>
        {
            Field("Host", "example.com"),
            Field("Upgrade", "websocket"),
            Field("Connection", "Upgrade"),
            Field("Sec-WebSocket-Key", ValidKey),
            Field("Sec-WebSocket-Version", "13")
        };
        fields.AddRange(additionalFields);
        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Method = "GET",
            HasRequestTarget = true,
            RequestTarget = "/chat",
            HasHttpVersion = true,
            HttpVersion = "HTTP/1.1",
            Fields = fields.ToArray()
        };
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

    static void Replace(WebSocketHttpRequest request, string fieldName, string value)
    {
        request.Fields.Single(field => string.Equals(
            field.Name,
            fieldName,
            StringComparison.OrdinalIgnoreCase)).Value = value;
    }

    static void MutateMetadata(WebSocketHttpRequest request, string mutation)
    {
        if (mutation == "null-method")
        {
            request.Method = null!;
        }
        else if (mutation == "invalid-method")
        {
            request.Method = "GE T";
        }
        else if (mutation == "null-target")
        {
            request.RequestTarget = null!;
        }
        else if (mutation == "missing-http-version")
        {
            request.HasHttpVersion = false;
        }
        else if (mutation == "null-http-version")
        {
            request.HttpVersion = null!;
        }
        else if (mutation == "malformed-http-version")
        {
            request.HttpVersion = "HTTP/1.x";
        }
        else if (mutation == "unexpected-scheme")
        {
            request.HasScheme = true;
            request.Scheme = "https";
        }
        else if (mutation == "unexpected-authority")
        {
            request.HasAuthority = true;
            request.Authority = "example.com";
        }
        else if (mutation == "unexpected-path")
        {
            request.HasPath = true;
            request.Path = "/chat";
        }
        else if (mutation == "unexpected-protocol")
        {
            request.HasProtocol = true;
            request.Protocol = "websocket";
        }
        else if (mutation == "null-fields")
        {
            request.Fields = null!;
        }
        else if (mutation == "null-field")
        {
            request.Fields[0] = null!;
        }
        else if (mutation == "null-field-name")
        {
            request.Fields[0].Name = null!;
        }
        else if (mutation == "null-field-value")
        {
            request.Fields[0].Value = null!;
        }
        else if (mutation == "invalid-field-name")
        {
            request.Fields[0].Name = "Bad Name";
        }
        else
        {
            request.Fields[0].Value = "example.com\r\nInjected: yes";
        }
    }

    static void AssertFailure(WebSocketHandshakeResult result, int statusCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(result.Error.HasHttpStatusCode, Is.True);
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(statusCode));
        });
    }

    static void AssertRejection(WebSocketHandshakeResult result, int statusCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(statusCode));
        });
    }
}

internal class RecordingOriginPolicy : IWebSocketOriginPolicy
{
    readonly bool allowed;

    internal WebSocketHttpRequest? Request;

    internal RecordingOriginPolicy(bool allowedValue)
    {
        allowed = allowedValue;
    }

    public bool IsOriginAllowed(WebSocketHttpRequest request)
    {
        Request = request;
        return allowed;
    }
}

internal class SelectingSubprotocolSelector : IWebSocketSubprotocolSelector
{
    readonly string selection;

    internal SelectingSubprotocolSelector(string selectedProtocol)
    {
        selection = selectedProtocol;
    }

    public string SelectSubprotocol(string[] clientProtocols, string[] serverProtocols)
    {
        return selection;
    }
}
