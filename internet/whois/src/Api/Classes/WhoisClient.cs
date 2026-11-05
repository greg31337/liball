namespace whois;

/// <summary>
/// Coordinates caller-directed WHOIS exchanges and automatic multi-server lookups.
/// </summary>
public class WhoisClient : IWhoisClient
{
    IWhoisQueryClassifier queryClassifier;
    IWhoisQueryFormatter queryFormatter;
    IWhoisServerSelector serverSelector;
    IWhoisTransport transport;
    IWhoisResponseDecoder responseDecoder;
    IWhoisResponseParser responseParser;
    IWhoisReferralParser referralParser;

    /// <summary>
    /// Initializes a WHOIS client with its classification, selection, exchange, and response-processing dependencies.
    /// </summary>
    /// <param name="classifier">The query classifier.</param>
    /// <param name="formatter">The endpoint-specific query formatter.</param>
    /// <param name="selector">The initial server selector.</param>
    /// <param name="exchangeTransport">The status-bearing wire transport.</param>
    /// <param name="decoder">The raw response decoder.</param>
    /// <param name="parser">The generic response parser.</param>
    /// <param name="referrals">The referral parser.</param>
    /// <exception cref="System.ArgumentNullException">A required dependency is <c>null</c>.</exception>
    public WhoisClient(IWhoisQueryClassifier classifier, IWhoisQueryFormatter formatter, IWhoisServerSelector selector, IWhoisTransport exchangeTransport, IWhoisResponseDecoder decoder, IWhoisResponseParser parser, IWhoisReferralParser referrals)
    {
        InputGuard.RequireReference(classifier, "classifier");
        InputGuard.RequireReference(formatter, "formatter");
        InputGuard.RequireReference(selector, "selector");
        InputGuard.RequireReference(exchangeTransport, "exchangeTransport");
        InputGuard.RequireReference(decoder, "decoder");
        InputGuard.RequireReference(parser, "parser");
        InputGuard.RequireReference(referrals, "referrals");
        queryClassifier = classifier;
        queryFormatter = formatter;
        serverSelector = selector;
        transport = exchangeTransport;
        responseDecoder = decoder;
        responseParser = parser;
        referralParser = referrals;
    }

    /// <summary>
    /// Performs exactly one caller-directed exchange and throws on operational failure.
    /// </summary>
    /// <param name="request">The exact caller-directed exchange request.</param>
    /// <returns>The complete raw response.</returns>
    /// <exception cref="System.ArgumentNullException">The request or a required nested request value is <c>null</c>.</exception>
    /// <exception cref="WhoisQueryException">The request endpoint, query, limits, encoding, or timeouts are invalid.</exception>
    /// <exception cref="WhoisTransportException">The exchange fails before a complete response is received.</exception>
    public WhoisRawResponse Query(WhoisRequest request)
    {
        WhoisRawResponse response = TryQuery(request);
        if (response == null)
        {
            throw new System.InvalidOperationException("The WHOIS transport returned no response.");
        }
        if (response.Status == WhoisExchangeStatus.Complete)
        {
            return response;
        }
        throw CreateTransportFailure(response);
    }

    /// <summary>
    /// Performs exactly one caller-directed status-bearing exchange.
    /// </summary>
    /// <param name="request">The exact caller-directed exchange request.</param>
    /// <returns>The raw response and expected failure status.</returns>
    /// <exception cref="System.ArgumentNullException">The request or a required nested request value is <c>null</c>.</exception>
    public WhoisRawResponse TryQuery(WhoisRequest request)
    {
        WhoisRequest copiedRequest = CopyRequest(request);
        copiedRequest.Source = WhoisEndpointSource.Direct;
        return transport.Exchange(copiedRequest);
    }

    /// <summary>
    /// Performs an automatic WHOIS lookup and throws on operational failure.
    /// </summary>
    /// <param name="request">The caller input and lookup options.</param>
    /// <returns>The complete ordered lookup result.</returns>
    /// <exception cref="WhoisQueryException">The caller input or lookup options are invalid.</exception>
    /// <exception cref="WhoisServerNotFoundException">No initial server can route the classified query.</exception>
    /// <exception cref="WhoisTransportException">A resolution, connection, write, read, cancellation, timeout, or response-limit failure occurs.</exception>
    /// <exception cref="WhoisResponseException">Response decoding or generic parsing fails.</exception>
    /// <exception cref="WhoisReferralException">Referral traversal is rejected, loops, or exceeds its hop limit.</exception>
    public WhoisLookupResult Lookup(WhoisLookupRequest request)
    {
        WhoisLookupResult result = TryLookup(request);
        if (result.IsComplete)
        {
            return result;
        }

        System.Exception? failure = result.Error;
        if (failure == null)
        {
            failure = new System.InvalidOperationException("WHOIS lookup failed without error details.");
        }
        if (result.FailedStage == WhoisOperationStage.Select)
        {
            throw new WhoisServerNotFoundException(failure.Message, result.Query, failure);
        }
        if (result.FailedStage == WhoisOperationStage.Resolve ||
            result.FailedStage == WhoisOperationStage.Connect ||
            result.FailedStage == WhoisOperationStage.Write ||
            result.FailedStage == WhoisOperationStage.Read)
        {
            WhoisLookupHop transportHop = GetLastHop(result);
            throw CreateTransportFailure(transportHop.RawResponse);
        }
        if (result.FailedStage == WhoisOperationStage.Decode || result.FailedStage == WhoisOperationStage.Parse)
        {
            WhoisLookupHop responseHop = GetLastHop(result);
            string message = "WHOIS response decoding failed.";
            if (result.FailedStage == WhoisOperationStage.Parse)
            {
                message = "Generic WHOIS response parsing failed.";
            }
            throw new WhoisResponseException(message, result.FailedStage, responseHop.RawResponse, responseHop.TextResponse, failure);
        }
        if (result.FailedStage == WhoisOperationStage.Refer)
        {
            WhoisReferral? referral = GetLastSelectedReferral(result);
            string reason = "Referral";
            if (failure.Message == "A WHOIS referral loop was detected.")
            {
                reason = "Loop";
            }
            else if (failure.Message == "The maximum WHOIS referral hop count was reached.")
            {
                reason = "HopLimit";
            }
            throw new WhoisReferralException(failure.Message, referral, reason, result, failure);
        }
        throw new System.InvalidOperationException("WHOIS lookup returned an incomplete result without a failed operation stage.");
    }

    internal static WhoisLookupHop GetLastHop(WhoisLookupResult result)
    {
        if (result.Hops.Length == 0)
        {
            throw new System.InvalidOperationException("WHOIS lookup failure does not contain hop context.");
        }
        return result.Hops[result.Hops.Length - 1];
    }

    internal static WhoisReferral? GetLastSelectedReferral(WhoisLookupResult result)
    {
        int index = result.Hops.Length - 1;
        while (index >= 0)
        {
            WhoisReferral? referral = result.Hops[index].SelectedReferral;
            if (referral != null)
            {
                return referral;
            }
            index = index - 1;
        }
        return null;
    }

    /// <summary>
    /// Performs an exception-less automatic WHOIS lookup.
    /// </summary>
    /// <param name="request">The caller input and lookup options.</param>
    /// <returns>The status-bearing lookup result.</returns>
    /// <exception cref="System.ArgumentNullException">The request or required nested options are <c>null</c>.</exception>
    /// <exception cref="WhoisQueryException">The caller input or lookup options are invalid.</exception>
    public WhoisLookupResult TryLookup(WhoisLookupRequest request)
    {
        WhoisLookupRequest copiedRequest = CopyLookupRequest(request);
        WhoisQuery query = queryClassifier.Classify(copiedRequest);
        InputGuard.ValidateLimits(copiedRequest.Options.Limits, query.OriginalText, query.Kind);
        InputGuard.ValidateTimeouts(copiedRequest.Options.Timeouts, query.OriginalText, query.Kind);
        WhoisDnsResolver.ValidatePreference(copiedRequest.Options.AddressPreference);
        WhoisLookupResult result = new WhoisLookupResult();
        result.Query = query;

        System.Threading.Tasks.Task<WhoisServerSelection> selectionTask = System.Threading.Tasks.Task.Run<WhoisServerSelection>(() => serverSelector.SelectServer(query, copiedRequest));
        System.Threading.Tasks.Task completedSelectionTask = System.Threading.Tasks.Task.WhenAny(new System.Threading.Tasks.Task[] { selectionTask }).Result;
        if (completedSelectionTask != selectionTask || !selectionTask.IsCompletedSuccessfully)
        {
            result.FailedStage = WhoisOperationStage.Select;
            result.Error = GetSelectionError(selectionTask);
            return result;
        }

        WhoisServerSelection selection = selectionTask.Result;
        if (selection == null || selection.Endpoint == null)
        {
            throw new System.InvalidOperationException("The server selector returned no endpoint selection.");
        }
        WhoisServerSelection initialSelection = CopySelection(selection);
        WhoisServerSelection currentSelection = initialSelection;
        System.Collections.Generic.List<WhoisLookupHop> hops = new System.Collections.Generic.List<WhoisLookupHop>();
        System.Collections.Generic.List<string> visitedKeys = new System.Collections.Generic.List<string>();
        int referralHopCount = 0;
        bool finished = false;
        while (!finished)
        {
            string lookupKey = BuildLookupKey(currentSelection.Endpoint, query);
            visitedKeys.Add(lookupKey);
            string formattedQuery = queryFormatter.FormatQuery(query, currentSelection);
            InputGuard.RequireReference(formattedQuery, "formattedQuery");

            WhoisRequest exchangeRequest = CreateLookupExchangeRequest(currentSelection, formattedQuery, copiedRequest);
            WhoisLookupHop hop = new WhoisLookupHop();
            hop.Selection = currentSelection;
            hops.Add(hop);
            WhoisRawResponse rawResponse = transport.Exchange(exchangeRequest);
            if (rawResponse == null)
            {
                throw new System.InvalidOperationException("The WHOIS transport returned no response.");
            }
            hop.RawResponse = rawResponse;
            hop.TextResponse.RawResponse = rawResponse;
            hop.ParsedResponse.TextResponse = hop.TextResponse;
            if (rawResponse.Status != WhoisExchangeStatus.Complete)
            {
                result.FailedStage = rawResponse.Stage;
                result.Error = rawResponse.Error;
                if (result.Error == null)
                {
                    result.Error = new System.InvalidOperationException("The WHOIS exchange failed without error details.");
                }
                SetVisibleHops(result, hops, initialSelection, copiedRequest.Options.RetainDiscoveryResponses);
                return result;
            }

            WhoisTextResponse textResponse = responseDecoder.DecodeResponse(rawResponse, exchangeRequest.Encoding);
            if (textResponse == null)
            {
                throw new System.InvalidOperationException("The response decoder returned no result.");
            }
            hop.TextResponse = textResponse;
            hop.ParsedResponse.TextResponse = textResponse;
            if (!textResponse.IsDecoded)
            {
                result.FailedStage = WhoisOperationStage.Decode;
                result.Error = textResponse.Error;
                if (result.Error == null)
                {
                    result.Error = new System.InvalidOperationException("WHOIS response decoding failed without error details.");
                }
                SetVisibleHops(result, hops, initialSelection, copiedRequest.Options.RetainDiscoveryResponses);
                return result;
            }

            WhoisParsedResponse parsedResponse = responseParser.ParseResponse(textResponse, exchangeRequest.Limits);
            if (parsedResponse == null)
            {
                throw new System.InvalidOperationException("The response parser returned no result.");
            }
            hop.ParsedResponse = parsedResponse;
            if (!parsedResponse.IsParsed)
            {
                result.FailedStage = WhoisOperationStage.Parse;
                result.Error = parsedResponse.Error;
                if (result.Error == null)
                {
                    result.Error = new System.InvalidOperationException("WHOIS response parsing failed without error details.");
                }
                SetVisibleHops(result, hops, initialSelection, copiedRequest.Options.RetainDiscoveryResponses);
                return result;
            }

            WhoisReferral[] referrals = referralParser.ParseReferrals(parsedResponse);
            if (referrals == null)
            {
                throw new System.InvalidOperationException("The referral parser returned no result.");
            }
            hop.Referrals = referrals;
            WhoisReferral? selectedReferral = null;
            if (copiedRequest.Options.FollowReferrals)
            {
                selectedReferral = SelectNextReferral(referrals);
            }
            if (selectedReferral == null)
            {
                finished = true;
            }
            else
            {
                hop.SelectedReferral = selectedReferral;
                WhoisEndpoint? referralEndpoint = selectedReferral.Endpoint;
                if (referralEndpoint == null)
                {
                    throw new System.InvalidOperationException("An eligible referral does not contain an endpoint.");
                }
                string referralKey = BuildLookupKey(referralEndpoint, query);
                if (ContainsLookupKey(visitedKeys, referralKey))
                {
                    result.FailedStage = WhoisOperationStage.Refer;
                    result.Error = new System.InvalidOperationException("A WHOIS referral loop was detected.");
                    SetVisibleHops(result, hops, initialSelection, copiedRequest.Options.RetainDiscoveryResponses);
                    return result;
                }
                if (referralHopCount >= copiedRequest.Options.Limits.MaximumReferralHops)
                {
                    result.FailedStage = WhoisOperationStage.Refer;
                    result.Error = new System.InvalidOperationException("The maximum WHOIS referral hop count was reached.");
                    SetVisibleHops(result, hops, initialSelection, copiedRequest.Options.RetainDiscoveryResponses);
                    return result;
                }
                referralHopCount = referralHopCount + 1;
                currentSelection = CreateReferralSelection(referralEndpoint);
            }
        }

        SetVisibleHops(result, hops, initialSelection, copiedRequest.Options.RetainDiscoveryResponses);
        result.IsComplete = true;
        result.FailedStage = WhoisOperationStage.None;
        result.Error = null;
        return result;
    }

    internal static WhoisReferral? SelectNextReferral(WhoisReferral[] referrals)
    {
        WhoisReferral? selected = null;
        int selectedPriority = 2147483647;
        int index = 0;
        while (index < referrals.Length)
        {
            WhoisReferral referral = referrals[index];
            if (referral == null)
            {
                throw new System.InvalidOperationException("The referral parser returned a null referral.");
            }
            WhoisEndpoint? endpoint = referral.Endpoint;
            if (referral.IsValid && endpoint != null && endpoint.Scheme == WhoisReferralScheme.Whois && referral.FollowPriority >= 0 && referral.FollowPriority < selectedPriority)
            {
                selected = referral;
                selectedPriority = referral.FollowPriority;
            }
            index = index + 1;
        }
        return selected;
    }

    internal static WhoisServerSelection CreateReferralSelection(WhoisEndpoint endpoint)
    {
        WhoisServerSelection selection = new WhoisServerSelection();
        selection.Endpoint = CopyEndpoint(endpoint);
        selection.Source = WhoisEndpointSource.Referral;
        selection.QueryOverride = null;
        selection.Reason = "Selected the highest-priority eligible WHOIS referral.";
        return selection;
    }

    internal static string BuildLookupKey(WhoisEndpoint endpoint, WhoisQuery query)
    {
        return EndpointParser.GetEqualityKey(endpoint) + "|" + query.RoutingKey;
    }

    internal static bool ContainsLookupKey(System.Collections.Generic.List<string> visitedKeys, string lookupKey)
    {
        int index = 0;
        while (index < visitedKeys.Count)
        {
            if (string.Equals(visitedKeys[index], lookupKey, System.StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void SetVisibleHops(WhoisLookupResult result, System.Collections.Generic.List<WhoisLookupHop> hops, WhoisServerSelection initialSelection, bool retainDiscoveryResponses)
    {
        int startIndex = 0;
        if (!retainDiscoveryResponses && IsIanaDiscoverySelection(initialSelection) && hops.Count > 0)
        {
            startIndex = 1;
        }
        int visibleCount = hops.Count - startIndex;
        WhoisLookupHop[] visibleHops = new WhoisLookupHop[visibleCount];
        int index = 0;
        while (index < visibleCount)
        {
            visibleHops[index] = hops[index + startIndex];
            index = index + 1;
        }
        result.Hops = visibleHops;
    }

    internal static bool IsIanaDiscoverySelection(WhoisServerSelection selection)
    {
        WhoisEndpoint endpoint = selection.Endpoint;
        return selection.Source == WhoisEndpointSource.InitialSelection &&
            endpoint.Scheme == WhoisReferralScheme.Whois &&
            endpoint.Port == WhoisDefaults.WhoisPort &&
            string.Equals(endpoint.Host, WhoisDefaults.IanaHost, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static WhoisLookupRequest CopyLookupRequest(WhoisLookupRequest request)
    {
        InputGuard.RequireReference(request, "request");
        InputGuard.RequireReference(request.Text, "request.Text");
        InputGuard.RequireReference(request.Options, "request.Options");
        InputGuard.RequireReference(request.Options.Timeouts, "request.Options.Timeouts");
        InputGuard.RequireReference(request.Options.Limits, "request.Options.Limits");
        InputGuard.RequireReference(request.Options.Encoding, "request.Options.Encoding");

        WhoisLookupRequest copy = new WhoisLookupRequest();
        copy.Text = request.Text;
        copy.Kind = request.Kind;
        copy.Options = CopyClientOptions(request.Options);
        if (request.EndpointOverride != null)
        {
            copy.EndpointOverride = CopyEndpoint(request.EndpointOverride);
        }
        else
        {
            copy.EndpointOverride = null;
        }
        copy.CancellationToken = request.CancellationToken;
        return copy;
    }

    internal static WhoisClientOptions CopyClientOptions(WhoisClientOptions options)
    {
        WhoisClientOptions copy = new WhoisClientOptions();
        copy.Timeouts = CopyTimeouts(options.Timeouts);
        copy.Limits = CopyLimits(options.Limits);
        copy.AddressPreference = options.AddressPreference;
        copy.Encoding = CopyEncoding(options.Encoding);
        copy.FollowReferrals = options.FollowReferrals;
        copy.RetainDiscoveryResponses = options.RetainDiscoveryResponses;
        copy.AllowNonPublicDirectEndpoints = options.AllowNonPublicDirectEndpoints;
        copy.AllowNonPublicAutomaticEndpoints = options.AllowNonPublicAutomaticEndpoints;
        return copy;
    }

    internal static WhoisServerSelection CopySelection(WhoisServerSelection selection)
    {
        WhoisServerSelection copy = new WhoisServerSelection();
        copy.Endpoint = CopyEndpoint(selection.Endpoint);
        copy.Source = selection.Source;
        copy.QueryOverride = selection.QueryOverride;
        copy.Reason = selection.Reason;
        return copy;
    }

    internal static WhoisRequest CreateLookupExchangeRequest(WhoisServerSelection selection, string formattedQuery, WhoisLookupRequest request)
    {
        WhoisRequest exchangeRequest = new WhoisRequest();
        exchangeRequest.Endpoint = CopyEndpoint(selection.Endpoint);
        exchangeRequest.Source = selection.Source;
        exchangeRequest.QueryText = formattedQuery;
        exchangeRequest.Encoding = CopyEncoding(request.Options.Encoding);
        exchangeRequest.Timeouts = CopyTimeouts(request.Options.Timeouts);
        exchangeRequest.Limits = CopyLimits(request.Options.Limits);
        exchangeRequest.CancellationToken = request.CancellationToken;
        return exchangeRequest;
    }

    internal static System.Exception GetSelectionError(System.Threading.Tasks.Task<WhoisServerSelection> selectionTask)
    {
        System.AggregateException? aggregate = selectionTask.Exception;
        if (aggregate != null && aggregate.InnerException != null)
        {
            aggregate.Handle((error) => true);
            return aggregate.InnerException;
        }
        if (aggregate != null)
        {
            aggregate.Handle((error) => true);
            return aggregate;
        }
        if (selectionTask.IsCanceled)
        {
            return new System.OperationCanceledException("WHOIS server selection was cancelled.");
        }
        return new System.InvalidOperationException("WHOIS server selection did not reach a terminal state.");
    }

    internal static WhoisRequest CopyRequest(WhoisRequest request)
    {
        InputGuard.RequireReference(request, "request");
        InputGuard.RequireReference(request.Endpoint, "request.Endpoint");
        InputGuard.RequireReference(request.QueryText, "request.QueryText");
        InputGuard.RequireReference(request.Encoding, "request.Encoding");
        InputGuard.RequireReference(request.Timeouts, "request.Timeouts");
        InputGuard.RequireReference(request.Limits, "request.Limits");

        WhoisRequest copy = new WhoisRequest();
        copy.Endpoint = CopyEndpoint(request.Endpoint);
        copy.Source = request.Source;
        copy.QueryText = request.QueryText;
        copy.Encoding = CopyEncoding(request.Encoding);
        copy.Timeouts = CopyTimeouts(request.Timeouts);
        copy.Limits = CopyLimits(request.Limits);
        copy.CancellationToken = request.CancellationToken;
        return copy;
    }

    internal static WhoisTransportException CreateTransportFailure(WhoisRawResponse response)
    {
        string message;
        switch (response.Status)
        {
            case WhoisExchangeStatus.Cancelled:
                message = "The WHOIS exchange was cancelled.";
                break;
            case WhoisExchangeStatus.TimedOut:
                message = "The WHOIS exchange timed out.";
                break;
            case WhoisExchangeStatus.ResponseLimitReached:
                message = "The WHOIS response exceeded the configured byte limit.";
                break;
            case WhoisExchangeStatus.ResolveFailed:
                message = "WHOIS host resolution failed.";
                break;
            case WhoisExchangeStatus.ConnectFailed:
                message = "WHOIS connection failed.";
                break;
            case WhoisExchangeStatus.WriteFailed:
                message = "WHOIS query write failed.";
                break;
            case WhoisExchangeStatus.ReadFailed:
                message = "WHOIS response read failed.";
                break;
            case WhoisExchangeStatus.Complete:
                throw new System.InvalidOperationException("A complete WHOIS response is not a transport failure.");
            default:
                throw new System.InvalidOperationException("The WHOIS transport returned an unsupported status.");
        }

        WhoisRequest responseRequest = response.Request;
        if (responseRequest == null || responseRequest.Endpoint == null)
        {
            throw new System.InvalidOperationException("The failed WHOIS response does not contain request endpoint context.");
        }
        return new WhoisTransportException(message, response.Stage, responseRequest.Endpoint, response.ApprovedAddress, response, response.Error);
    }

    internal static WhoisEndpoint CopyEndpoint(WhoisEndpoint endpoint)
    {
        WhoisEndpoint copy = new WhoisEndpoint();
        copy.Scheme = endpoint.Scheme;
        copy.Host = endpoint.Host;
        copy.Port = endpoint.Port;
        copy.AdvertisedValue = endpoint.AdvertisedValue;
        return copy;
    }

    internal static WhoisEncodingOptions CopyEncoding(WhoisEncodingOptions encoding)
    {
        InputGuard.RequireReference(encoding.RequestEncoding, "request.Encoding.RequestEncoding");
        WhoisEncodingOptions copy = new WhoisEncodingOptions();
        copy.RequestEncoding = (System.Text.Encoding)encoding.RequestEncoding.Clone();
        System.Text.Encoding? responseEncoding = encoding.ResponseEncoding;
        if (responseEncoding != null)
        {
            copy.ResponseEncoding = (System.Text.Encoding)responseEncoding.Clone();
        }
        else
        {
            copy.ResponseEncoding = null;
        }
        copy.DetectByteOrderMark = encoding.DetectByteOrderMark;
        copy.DecodingMode = encoding.DecodingMode;
        return copy;
    }

    internal static WhoisTimeoutOptions CopyTimeouts(WhoisTimeoutOptions timeouts)
    {
        WhoisTimeoutOptions copy = new WhoisTimeoutOptions();
        copy.Resolve = timeouts.Resolve;
        copy.Connect = timeouts.Connect;
        copy.Write = timeouts.Write;
        copy.ReadIdle = timeouts.ReadIdle;
        copy.Overall = timeouts.Overall;
        return copy;
    }

    internal static WhoisLimits CopyLimits(WhoisLimits limits)
    {
        WhoisLimits copy = new WhoisLimits();
        copy.MaximumEncodedQueryBytes = limits.MaximumEncodedQueryBytes;
        copy.MaximumResponseBytes = limits.MaximumResponseBytes;
        copy.MaximumReferralHops = limits.MaximumReferralHops;
        copy.MaximumLines = limits.MaximumLines;
        copy.MaximumFields = limits.MaximumFields;
        return copy;
    }
}
