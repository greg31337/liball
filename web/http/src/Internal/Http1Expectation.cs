namespace http;

internal class Http1ExpectationResult
{
    internal HttpOperationStatus Status;
    internal bool HasContinueExpectation;
    internal bool HasUnsupportedExpectations;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class Http1Expectation
{
    internal static Http1ExpectationResult Analyze(
        HttpRequest request,
        Http1FramingDecision framing,
        HttpLimits limits)
    {
        Http1ExpectationResult result = Parse(request.Headers, limits);
        if (result.Status != HttpOperationStatus.Done)
        {
            return result;
        }

        request.HasContinueExpectation = result.HasContinueExpectation;
        request.HasUnsupportedExpectations = result.HasUnsupportedExpectations;
        request.ShouldSendContinue = result.HasContinueExpectation &&
            request.ProtocolVersion == HttpProtocolVersion.Http11 &&
            HasContentFollowing(framing);
        return result;
    }

    static Http1ExpectationResult Parse(HttpHeaders headers, HttpLimits limits)
    {
        Http1ExpectationResult result = new Http1ExpectationResult();
        int remainingExpectations = limits.MaximumListMemberCount;
        int fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Expect))
            {
                CommonFieldParseResult parsed = FieldValueParser.ParseExpect(
                    field.Value,
                    limits.MaximumFieldValueSize,
                    remainingExpectations,
                    limits.MaximumParameterCount);
                if (parsed.IsLimitExceeded)
                {
                    return Reject(
                        result,
                        "http1-expectation-limit-exceeded",
                        "The Expect field contains too many expectations or parameters.");
                }

                if (parsed.Status != HttpOperationStatus.Done)
                {
                    return Reject(
                        result,
                        "invalid-http1-expectation",
                        "The Expect field value is malformed.");
                }

                int expectationIndex = 0;
                while (expectationIndex < parsed.Expectations.Count)
                {
                    HttpExpectationValue expectation =
                        parsed.Expectations[expectationIndex];
                    if (Ascii.EqualsStringsIgnoreCase(
                        expectation.Name,
                        "100-continue") && !expectation.HasValue)
                    {
                        result.HasContinueExpectation = true;
                    }
                    else
                    {
                        result.HasUnsupportedExpectations = true;
                    }

                    expectationIndex = expectationIndex + 1;
                }

                remainingExpectations =
                    remainingExpectations - parsed.Expectations.Count;
            }

            fieldIndex = fieldIndex + 1;
        }

        result.Status = HttpOperationStatus.Done;
        return result;
    }

    static bool HasContentFollowing(Http1FramingDecision framing)
    {
        if (framing.BodyFraming == Http1BodyFraming.Chunked)
        {
            return true;
        }

        return framing.BodyFraming == Http1BodyFraming.ContentLength &&
            framing.DeclaredLength > 0;
    }

    static Http1ExpectationResult Reject(
        Http1ExpectationResult result,
        string code,
        string message)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
