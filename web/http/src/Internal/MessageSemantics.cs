namespace http;

internal static class MessageSemantics
{
    internal static bool AllowsResponseContent(HttpRequest request, HttpResponse response)
    {
        ValidateRequestParts(request);
        ValidateResponseParts(response);
        HttpMethodMetadata method = MethodSemantics.Describe(request.Method);
        HttpStatusMetadata status = StatusSemantics.Describe(response.StatusCode);
        ValidateInformationalState(response, status);

        if (method.Method == HttpMethods.Head)
        {
            return false;
        }

        if (method.Method == HttpMethods.Connect &&
            status.StatusClass == HttpStatusClass.Successful)
        {
            return false;
        }

        return status.AllowsContent;
    }

    internal static bool IsRequestComplete(HttpRequest request)
    {
        ValidateRequestParts(request);
        HttpMethodMetadata method = MethodSemantics.Describe(request.Method);
        ValidateTrailers(request.Trailers);

        if ((method.Method == HttpMethods.Connect || method.Method == HttpMethods.Trace) &&
            (request.Content.Length != 0 || request.Trailers.Fields.Count != 0))
        {
            throw CreateProtocolError(
                "request-content-not-allowed",
                "The request method does not permit request content or trailers.");
        }

        return request.IsComplete;
    }

    internal static bool IsResponseComplete(HttpRequest request, HttpResponse response)
    {
        bool allowsContent = AllowsResponseContent(request, response);
        ValidateTrailers(response.Trailers);

        if (!allowsContent &&
            (response.Content.Length != 0 || response.Trailers.Fields.Count != 0))
        {
            throw CreateProtocolError(
                "response-content-not-allowed",
                "The response method or status does not permit content or trailers.");
        }

        return response.IsComplete;
    }

    internal static bool IsInformationalResponse(HttpResponse response)
    {
        ValidateResponseParts(response);
        HttpStatusMetadata status = StatusSemantics.Describe(response.StatusCode);
        ValidateInformationalState(response, status);
        return status.StatusClass == HttpStatusClass.Informational;
    }

    static void ValidateInformationalState(
        HttpResponse response,
        HttpStatusMetadata status)
    {
        bool informational = status.StatusClass == HttpStatusClass.Informational;
        if (response.IsInformational != informational)
        {
            throw CreateProtocolError(
                "inconsistent-informational-response",
                "The informational response flag does not match the status class.");
        }
    }

    static void ValidateRequestParts(HttpRequest request)
    {
        if (request == null || request.Headers == null || request.Headers.Fields == null ||
            request.Content == null || request.Trailers == null ||
            request.Trailers.Fields == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The HTTP request contains incomplete message data.");
        }
    }

    static void ValidateResponseParts(HttpResponse response)
    {
        if (response == null || response.Headers == null || response.Headers.Fields == null ||
            response.Content == null || response.Trailers == null ||
            response.Trailers.Fields == null)
        {
            throw CreateProtocolError(
                "invalid-response",
                "The HTTP response contains incomplete message data.");
        }
    }

    static void ValidateTrailers(HttpHeaders trailers)
    {
        int index = 0;
        while (index < trailers.Fields.Count)
        {
            HttpHeaderField field = trailers.Fields[index];
            if (field == null || field.Name == null || field.Value == null)
            {
                throw CreateProtocolError(
                    "invalid-trailers",
                    "The message contains an incomplete trailer field.");
            }

            index = index + 1;
        }

        if (!HeaderStore.AreValidTrailers(trailers))
        {
            throw CreateProtocolError(
                "invalid-trailers",
                "The message contains a prohibited or malformed trailer field.");
        }
    }

    static HttpProtocolException CreateProtocolError(string code, string message)
    {
        return new HttpProtocolException(message, code);
    }
}
