using System;
using System.Text;

namespace http;

internal class Http2ToHttp1AuthorityResult
{
    internal bool IsValid = true;
    internal string Authority = "";
    internal bool IsSensitive;
    internal bool NeverIndex;
    internal string ErrorCodeName = "";
    internal string ErrorMessage = "";
}

internal static class Http2ToHttp1Conversion
{
    internal static Http2MessageResult Convert(
        Http2MessageResult message,
        HttpLimits limits)
    {
        if (message == null)
        {
            return RejectConvertedMessage(
                0,
                HttpMessageKind.Request,
                "invalid-http2-conversion-input",
                "An HTTP/2 message result is required for conversion.");
        }

        if (message.Status != HttpOperationStatus.Done || message.IsMalformed)
        {
            return RejectConvertedMessage(
                message.StreamId,
                message.MessageKind,
                "invalid-http2-conversion-input",
                "Only a successfully mapped HTTP/2 message can be converted.");
        }

        if (message.MessageKind == HttpMessageKind.Request)
        {
            return ConvertMappedRequest(message, limits);
        }

        if (message.MessageKind == HttpMessageKind.Response ||
            message.MessageKind == HttpMessageKind.InformationalResponse)
        {
            return ConvertMappedResponse(message, limits);
        }

        return RejectConvertedMessage(
            message.StreamId,
            message.MessageKind,
            "invalid-http2-conversion-kind",
            "Only mapped HTTP/2 requests and responses can be converted.");
    }

    internal static Http2MessageResult ConvertMappedRequest(
        Http2MessageResult message,
        HttpLimits limits)
    {
        HttpRequest? source = message.Request;
        if (source == null || message.Response != null ||
            source.ProtocolVersion != HttpProtocolVersion.Http2)
        {
            return RejectConvertedMessage(
                message.StreamId,
                message.MessageKind,
                "invalid-http2-request-conversion-input",
                "The mapping result must contain only an HTTP/2 request model.");
        }

        Http2MessageResult validated = Http2EncodingSemantics.EncodeRequest(
            message.StreamId, source, false, limits);
        if (validated.Status != HttpOperationStatus.Done)
        {
            return RejectFromValidation(message, validated);
        }

        if (source.ConnectProtocol != null)
        {
            return UnsupportedConvertedMessage(
                message.StreamId,
                message.MessageKind,
                "http2-extended-connect-not-representable-in-http1",
                "Extended CONNECT control data has no direct HTTP/1 request representation.");
        }

        Http2MessageResult trailerValidation =
            Http2EncodingSemantics.EncodeTrailers(
                message.StreamId, source.Trailers, limits);
        if (trailerValidation.Status != HttpOperationStatus.Done)
        {
            return RejectFromValidation(message, trailerValidation);
        }

        HttpHeaders regular = CreateRegularHeaders(validated.Headers);
        Http2ToHttp1AuthorityResult authority = ResolveAuthority(
            source, regular);
        if (!authority.IsValid)
        {
            return RejectConvertedMessage(
                message.StreamId,
                message.MessageKind,
                authority.ErrorCodeName,
                authority.ErrorMessage);
        }

        HttpRequest converted = CreateRequestModel(
            source, authority, trailerValidation.Headers);
        bool retainTe = HasTeTrailers(regular);
        Http1BodyFraming framing = SelectRequestFraming(
            source, converted.Trailers);
        converted.Headers = CreateRequestHeaders(
            regular, authority, retainTe, framing,
            converted.Content.Length, converted.Trailers);

        Http1SerializationResult serialized = ValidateRequestSerialization(
            converted, framing, limits);
        if (serialized.Status != HttpOperationStatus.Done)
        {
            return RejectConvertedMessage(
                message.StreamId,
                message.MessageKind,
                serialized.ErrorCode ?? "invalid-http1-request-conversion",
                serialized.ErrorMessage ??
                    "The converted request cannot be represented by HTTP/1.1.");
        }

        Http2MessageResult result = CreateSuccessfulResult(
            message, converted.Headers, converted.Content,
            converted.Trailers);
        result.Request = converted;
        if (source.Method != HttpMethods.Connect)
        {
            AddConversionNote(
                result,
                "http2-request-scheme-retained-out-of-band",
                true);
        }

        AddFramingNote(result, framing);
        AddSharedLossNotes(result, message, source.Headers, source.Trailers);
        return result;
    }

    internal static Http2MessageResult ConvertMappedResponse(
        Http2MessageResult message,
        HttpLimits limits)
    {
        HttpResponse? source = message.Response;
        if (source == null || message.Request != null ||
            source.ProtocolVersion != HttpProtocolVersion.Http2)
        {
            return RejectConvertedMessage(
                message.StreamId,
                message.MessageKind,
                "invalid-http2-response-conversion-input",
                "The mapping result must contain only an HTTP/2 response model.");
        }

        bool informational = source.StatusCode >= 100 &&
            source.StatusCode <= 199;
        if (source.IsInformational != informational ||
            informational &&
                message.MessageKind != HttpMessageKind.InformationalResponse ||
            !informational && message.MessageKind != HttpMessageKind.Response)
        {
            return RejectConvertedMessage(
                message.StreamId,
                message.MessageKind,
                "inconsistent-http2-response-conversion-kind",
                "The response status, informational flag, and message kind must agree.");
        }

        if (message.IsTunnel &&
            (informational || source.StatusCode < 200 ||
                source.StatusCode > 299 || source.Content.Length != 0 ||
                source.Trailers.Fields.Count != 0))
        {
            return RejectConvertedMessage(
                message.StreamId,
                message.MessageKind,
                "invalid-http2-tunnel-response-conversion",
                "Only an empty successful final tunnel response can map to HTTP/1.");
        }

        Http2MessageResult validated = Http2EncodingSemantics.EncodeResponse(
            message.StreamId, source, false, limits);
        if (validated.Status != HttpOperationStatus.Done)
        {
            return RejectFromValidation(message, validated);
        }

        Http2MessageResult trailerValidation =
            Http2EncodingSemantics.EncodeTrailers(
                message.StreamId, source.Trailers, limits);
        if (trailerValidation.Status != HttpOperationStatus.Done)
        {
            return RejectFromValidation(message, trailerValidation);
        }

        HttpHeaders regular = CreateRegularHeaders(validated.Headers);
        HttpResponse converted = CreateResponseModel(
            source, trailerValidation.Headers);
        Http1BodyFraming framing = SelectResponseFraming(
            converted, message.IsTunnel);
        converted.Headers = CreateResponseHeaders(
            regular, framing, converted.StatusCode,
            converted.Content.Length, converted.Trailers);

        Http1SerializationResult serialized = ValidateResponseSerialization(
            converted, framing, message.IsTunnel, limits);
        if (serialized.Status != HttpOperationStatus.Done)
        {
            return RejectConvertedMessage(
                message.StreamId,
                message.MessageKind,
                serialized.ErrorCode ?? "invalid-http1-response-conversion",
                serialized.ErrorMessage ??
                    "The converted response cannot be represented by HTTP/1.1.");
        }

        Http2MessageResult result = CreateSuccessfulResult(
            message, converted.Headers, converted.Content,
            converted.Trailers);
        result.Response = converted;
        AddConversionNote(
            result,
            "http1-reason-phrase-synthesized",
            true);
        AddFramingNote(result, framing);
        AddSharedLossNotes(result, message, source.Headers, source.Trailers);
        return result;
    }

    internal static Http2ToHttp1AuthorityResult ResolveAuthority(
        HttpRequest request,
        HttpHeaders headers)
    {
        Http2ToHttp1AuthorityResult result =
            new Http2ToHttp1AuthorityResult();
        int hostCount = 0;
        string host = "";
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (Ascii.EqualsStringsIgnoreCase(
                field.Name, HttpHeaderNames.Host))
            {
                hostCount = hostCount + 1;
                host = Http2FieldSemantics.DecodeHost(field.Value);
                result.IsSensitive = result.IsSensitive || field.IsSensitive;
                result.NeverIndex = result.NeverIndex || field.NeverIndex;
            }

            index = index + 1;
        }

        if (hostCount > 1)
        {
            return RejectAuthority(
                result,
                "multiple-http2-host-fields",
                "The HTTP/2 request contains more than one Host field.");
        }

        if (request.Authority != null)
        {
            result.Authority = request.Authority;
        }
        else if (hostCount == 1)
        {
            result.Authority = host;
        }

        if (request.Method == HttpMethods.Connect &&
            result.Authority.Length == 0)
        {
            return RejectAuthority(
                result,
                "missing-http2-request-authority",
                "A CONNECT request requires authority data for HTTP/1 conversion.");
        }

        return result;
    }

    internal static HttpRequest CreateRequestModel(
        HttpRequest source,
        Http2ToHttp1AuthorityResult authority,
        HttpHeaders trailers)
    {
        HttpRequest result = new HttpRequest();
        result.ProtocolVersion = HttpProtocolVersion.Http11;
        result.Method = source.Method;
        result.MethodMetadata = MethodSemantics.Describe(source.Method);
        result.Scheme = source.Scheme;
        result.Authority = authority.Authority;
        result.HasQuery = source.HasQuery;
        result.Query = source.Query;
        result.Path = source.Path;
        if (source.Method == HttpMethods.Connect)
        {
            result.Target = authority.Authority;
            result.TargetForm = HttpRequestTargetForm.Authority;
            result.Path = null;
            result.HasQuery = false;
            result.Query = null;
        }
        else if (source.Target == "*")
        {
            result.Target = "*";
            result.TargetForm = HttpRequestTargetForm.Asterisk;
            result.Path = null;
            result.HasQuery = false;
            result.Query = null;
        }
        else
        {
            result.Target = source.Target;
            result.TargetForm = HttpRequestTargetForm.Origin;
        }

        result.Content = CopyConvertedBytes(source.Content);
        result.Trailers = trailers;
        result.Priority = source.Priority;
        result.HasContinueExpectation = source.HasContinueExpectation;
        result.ShouldSendContinue = source.ShouldSendContinue;
        result.HasUnsupportedExpectations = source.HasUnsupportedExpectations;
        result.IsComplete = source.IsComplete;
        Http1ToHttp2Conversion.CopyRequestLists(source, result);
        return result;
    }

    internal static HttpResponse CreateResponseModel(
        HttpResponse source,
        HttpHeaders trailers)
    {
        HttpResponse result = new HttpResponse();
        result.ProtocolVersion = HttpProtocolVersion.Http11;
        result.StatusCode = source.StatusCode;
        result.HasReasonPhrase = true;
        HttpStatusMetadata metadata = StatusSemantics.Describe(
            source.StatusCode);
        result.ReasonPhrase = metadata.ReasonPhrase == null ?
            Array.Empty<byte>() : Encoding.ASCII.GetBytes(metadata.ReasonPhrase);
        result.Content = CopyConvertedBytes(source.Content);
        result.Trailers = trailers;
        result.IsInformational = source.IsInformational;
        result.IsComplete = source.IsComplete;
        return result;
    }

    internal static HttpHeaders CreateRegularHeaders(HttpHeaders encoded)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < encoded.Fields.Count)
        {
            HttpHeaderField field = encoded.Fields[index];
            if (field.Name.Length == 0 || field.Name[0] != ':')
            {
                result.Fields.Add(CopyConvertedField(field));
            }

            index = index + 1;
        }

        Http2CookieSemantics.CombineDecodedCookies(result);
        return result;
    }

    internal static HttpHeaders CreateRequestHeaders(
        HttpHeaders source,
        Http2ToHttp1AuthorityResult authority,
        bool retainTe,
        Http1BodyFraming framing,
        int contentLength,
        HttpHeaders trailers)
    {
        HttpHeaders result = new HttpHeaders();
        HttpHeaderField host = CreateTextField(
            HttpHeaderNames.Host, authority.Authority);
        host.IsSensitive = authority.IsSensitive;
        host.NeverIndex = authority.NeverIndex;
        result.Fields.Add(host);
        bool teSensitive = false;
        bool teNeverIndex = false;
        int index = 0;
        while (index < source.Fields.Count)
        {
            HttpHeaderField field = source.Fields[index];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.TE))
            {
                teSensitive = teSensitive || field.IsSensitive;
                teNeverIndex = teNeverIndex || field.NeverIndex;
            }
            else if (!IsControlledRequestField(field.Name))
            {
                result.Fields.Add(CopyConvertedField(field));
            }

            index = index + 1;
        }

        if (retainTe)
        {
            HttpHeaderField te = CreateTextField(
                HttpHeaderNames.TE, HttpHeaderValues.Trailers);
            te.IsSensitive = teSensitive;
            te.NeverIndex = teNeverIndex;
            result.Fields.Add(te);
            result.Fields.Add(CreateTextField(
                HttpHeaderNames.Connection, HttpHeaderNames.TE));
        }

        AddRequestFramingFields(
            result, framing, contentLength, trailers);
        return result;
    }

    internal static HttpHeaders CreateResponseHeaders(
        HttpHeaders source,
        Http1BodyFraming framing,
        int statusCode,
        int contentLength,
        HttpHeaders trailers)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < source.Fields.Count)
        {
            HttpHeaderField field = source.Fields[index];
            bool metadataLength = statusCode == HttpStatusCodes.NotModified &&
                Ascii.EqualsStringsIgnoreCase(
                    field.Name, HttpHeaderNames.ContentLength);
            if (metadataLength || !IsControlledResponseField(field.Name))
            {
                result.Fields.Add(CopyConvertedField(field));
            }

            index = index + 1;
        }

        AddResponseFramingFields(
            result, framing, contentLength, trailers);
        return result;
    }

    internal static void AddRequestFramingFields(
        HttpHeaders headers,
        Http1BodyFraming framing,
        int contentLength,
        HttpHeaders trailers)
    {
        if (framing == Http1BodyFraming.ContentLength)
        {
            headers.Fields.Add(CreateTextField(
                HttpHeaderNames.ContentLength,
                contentLength.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
        else if (framing == Http1BodyFraming.Chunked)
        {
            headers.Fields.Add(CreateTextField(
                HttpHeaderNames.TransferEncoding,
                HttpTransferCodings.Chunked));
            AddTrailerDeclaration(headers, trailers);
        }
    }

    internal static void AddResponseFramingFields(
        HttpHeaders headers,
        Http1BodyFraming framing,
        int contentLength,
        HttpHeaders trailers)
    {
        if (framing == Http1BodyFraming.ContentLength)
        {
            headers.Fields.Add(CreateTextField(
                HttpHeaderNames.ContentLength,
                contentLength.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
        else if (framing == Http1BodyFraming.Chunked)
        {
            headers.Fields.Add(CreateTextField(
                HttpHeaderNames.TransferEncoding,
                HttpTransferCodings.Chunked));
            AddTrailerDeclaration(headers, trailers);
        }
    }

    internal static void AddTrailerDeclaration(
        HttpHeaders headers,
        HttpHeaders trailers)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < trailers.Fields.Count)
        {
            string name = trailers.Fields[index].Name;
            if (!AppearsBefore(trailers, index, name))
            {
                if (value.Length != 0)
                {
                    value.Append(", ");
                }

                value.Append(name);
            }

            index = index + 1;
        }

        if (value.Length != 0)
        {
            headers.Fields.Add(CreateTextField(
                HttpHeaderNames.Trailer, value.ToString()));
        }
    }

    internal static bool AppearsBefore(
        HttpHeaders headers,
        int end,
        string name)
    {
        int index = 0;
        while (index < end)
        {
            if (Ascii.EqualsStringsIgnoreCase(
                headers.Fields[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static Http1BodyFraming SelectRequestFraming(
        HttpRequest source,
        HttpHeaders trailers)
    {
        if (trailers.Fields.Count != 0)
        {
            return Http1BodyFraming.Chunked;
        }

        if (source.Content.Length != 0 || Http1ToHttp2Conversion.HasField(
            source.Headers, HttpHeaderNames.ContentLength))
        {
            return Http1BodyFraming.ContentLength;
        }

        return Http1BodyFraming.None;
    }

    internal static Http1BodyFraming SelectResponseFraming(
        HttpResponse source,
        bool tunnel)
    {
        if (tunnel)
        {
            return Http1BodyFraming.Tunnel;
        }

        if (source.IsInformational ||
            source.StatusCode == HttpStatusCodes.NoContent ||
            source.StatusCode == HttpStatusCodes.NotModified)
        {
            return Http1BodyFraming.None;
        }

        if (source.Trailers.Fields.Count != 0)
        {
            return Http1BodyFraming.Chunked;
        }

        return Http1BodyFraming.ContentLength;
    }

    internal static Http1SerializationResult ValidateRequestSerialization(
        HttpRequest request,
        Http1BodyFraming framing,
        HttpLimits limits)
    {
        Http1SerializationOptions options = CreateSerializationOptions(
            framing, limits);
        return Http1RequestSerializer.Serialize(request, options);
    }

    internal static Http1SerializationResult ValidateResponseSerialization(
        HttpResponse response,
        Http1BodyFraming framing,
        bool tunnel,
        HttpLimits limits)
    {
        Http1SerializationOptions options = CreateSerializationOptions(
            framing, limits);
        if (tunnel)
        {
            options.ResponseRequestMethod = HttpMethods.Connect;
        }

        return Http1ResponseSerializer.SerializeResponse(response, options);
    }

    internal static Http1SerializationOptions CreateSerializationOptions(
        Http1BodyFraming framing,
        HttpLimits limits)
    {
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = framing;
        options.DeclareTrailers = false;
        options.Limits = limits;
        return options;
    }

    internal static bool HasTeTrailers(HttpHeaders headers)
    {
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (Ascii.EqualsStringsIgnoreCase(
                    field.Name, HttpHeaderNames.TE) &&
                Http2FieldSemantics.IsTrailersValue(field.Value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsControlledRequestField(string name)
    {
        return Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.Host) ||
            Ascii.EqualsStringsIgnoreCase(
                name, HttpHeaderNames.ContentLength) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.Trailer) ||
            Http2FieldSemantics.IsConnectionSpecific(
                Http2EncodingSemantics.LowercaseName(name));
    }

    internal static bool IsControlledResponseField(string name)
    {
        return Ascii.EqualsStringsIgnoreCase(
                name, HttpHeaderNames.ContentLength) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.Trailer) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.TE) ||
            Http2FieldSemantics.IsConnectionSpecific(
                Http2EncodingSemantics.LowercaseName(name));
    }

    internal static bool ContainsIndexingMetadata(HttpHeaders headers)
    {
        if (headers == null || headers.Fields == null)
        {
            return false;
        }

        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field != null &&
                (field.IsSensitive || field.NeverIndex))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void AddSharedLossNotes(
        Http2MessageResult result,
        Http2MessageResult source,
        HttpHeaders headers,
        HttpHeaders trailers)
    {
        if (ContainsIndexingMetadata(headers) ||
            ContainsIndexingMetadata(trailers) ||
            ContainsIndexingMetadata(source.Headers))
        {
            AddConversionNote(
                result,
                "hpack-indexing-metadata-has-no-http1-wire-form",
                true);
        }

        if (source.IsPushed)
        {
            AddConversionNote(
                result,
                "http2-server-push-association-has-no-http1-wire-form",
                true);
        }
    }

    internal static void AddFramingNote(
        Http2MessageResult result,
        Http1BodyFraming framing)
    {
        if (framing == Http1BodyFraming.Chunked)
        {
            AddConversionNote(
                result, "http1-chunked-framing-selected", false);
        }
        else if (framing == Http1BodyFraming.ContentLength)
        {
            AddConversionNote(
                result, "http1-content-length-framing-selected", false);
        }
        else if (framing == Http1BodyFraming.Tunnel)
        {
            AddConversionNote(
                result, "http1-tunnel-framing-selected", false);
        }
        else
        {
            AddConversionNote(
                result, "http1-no-body-framing-selected", false);
        }
    }

    internal static void AddConversionNote(
        Http2MessageResult result,
        string note,
        bool lossy)
    {
        result.ConversionNotes.Add(note);
        result.IsLossyConversion = result.IsLossyConversion || lossy;
    }

    internal static Http2MessageResult CreateSuccessfulResult(
        Http2MessageResult source,
        HttpHeaders headers,
        byte[] content,
        HttpHeaders trailers)
    {
        Http2MessageResult result = new Http2MessageResult();
        result.Status = HttpOperationStatus.Done;
        result.StreamId = source.StreamId;
        result.MessageKind = source.MessageKind;
        result.Headers = headers;
        result.Content = content;
        if (trailers.Fields.Count != 0)
        {
            result.Trailers = trailers;
        }

        result.EndStream = source.EndStream;
        result.IsComplete = source.IsComplete;
        result.IsTunnel = source.IsTunnel;
        result.IsPushed = source.IsPushed;
        result.HasAssociatedStreamId = source.HasAssociatedStreamId;
        result.AssociatedStreamId = source.AssociatedStreamId;
        result.PushRequest = source.PushRequest;
        result.IsPushCacheable = source.IsPushCacheable;
        return result;
    }

    internal static Http2MessageResult RejectFromValidation(
        Http2MessageResult source,
        Http2MessageResult validation)
    {
        return RejectConvertedMessage(
            source.StreamId,
            source.MessageKind,
            validation.ErrorCodeName ?? "invalid-http2-conversion-input",
            validation.ErrorMessage ??
                "The mapped HTTP/2 message cannot be converted.");
    }

    internal static Http2ToHttp1AuthorityResult RejectAuthority(
        Http2ToHttp1AuthorityResult result,
        string errorCodeName,
        string errorMessage)
    {
        result.IsValid = false;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2MessageResult RejectConvertedMessage(
        int streamId,
        HttpMessageKind messageKind,
        string errorCodeName,
        string errorMessage)
    {
        Http2MessageResult result = new Http2MessageResult();
        result.Status = HttpOperationStatus.Rejected;
        result.StreamId = streamId;
        result.MessageKind = messageKind;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2MessageResult UnsupportedConvertedMessage(
        int streamId,
        HttpMessageKind messageKind,
        string errorCodeName,
        string errorMessage)
    {
        Http2MessageResult result = RejectConvertedMessage(
            streamId, messageKind, errorCodeName, errorMessage);
        result.Status = HttpOperationStatus.Unsupported;
        return result;
    }

    internal static HttpHeaderField CopyConvertedField(HttpHeaderField source)
    {
        HttpHeaderField result = new HttpHeaderField();
        result.Name = source.Name;
        result.Value = CopyConvertedBytes(source.Value);
        result.IsSensitive = source.IsSensitive;
        result.NeverIndex = source.NeverIndex;
        return result;
    }

    internal static HttpHeaderField CreateTextField(
        string name,
        string value)
    {
        HttpHeaderField result = new HttpHeaderField();
        result.Name = name;
        result.Value = Encoding.ASCII.GetBytes(value);
        return result;
    }

    internal static byte[] CopyConvertedBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }
}
