namespace uspto;

internal class HttpResponseMetadata
{
    internal int StatusCode = 0;
    internal string? ReasonPhrase = null;
    internal global::System.Uri? RequestUri = null;
    internal string? MediaType = null;
    internal string? CharacterSet = null;
    internal DocumentFormat Format = DocumentFormat.Unknown;
    internal long ContentLength = 0L;
    internal bool HasContentLength = false;
    internal string? RequestIdentifier = null;
    internal global::System.Collections.Generic.Dictionary<string, string[]> Headers = new global::System.Collections.Generic.Dictionary<string, string[]>(global::System.StringComparer.OrdinalIgnoreCase);
}

internal class BufferedHttpResponse
{
    internal HttpResponseMetadata Metadata = new HttpResponseMetadata();
    internal DocumentFormat ExpectedFormat = DocumentFormat.Unknown;
    internal string Text = string.Empty;
    internal long BodyBytes = 0L;
    internal string? RawText = null;
    internal long RawTextBytes = 0L;
    internal bool HasRawText = false;
    internal bool IsRawTextTruncated = false;
}

internal class StreamedHttpResponse
{
    internal HttpResponseMetadata Metadata = new HttpResponseMetadata();
    internal long BytesCopied = 0L;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
}

internal static class ResponseReader
{
    internal static BufferedHttpResponse ReadTextResponse(global::System.Net.Http.HttpResponseMessage response, DocumentFormat expectedFormat, RequestContext context, UsptoLimits limits, string[]? secrets)
    {
        if (response == null)
        {
            throw new ValidationException("An HTTP response is required.", "response", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("Response limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumResponseBytes <= 0L || limits.MaximumResponseBytes > global::System.Int32.MaxValue)
        {
            throw new ValidationException("The buffered response limit must be between one byte and the largest supported array.", "MaximumResponseBytes", "INVALID_LIMIT", null);
        }

        if (limits.MaximumRawResponseBytes < 0L || limits.MaximumRawResponseBytes > global::System.Int32.MaxValue)
        {
            throw new ValidationException("The raw response limit must fit in a supported array.", "MaximumRawResponseBytes", "INVALID_LIMIT", null);
        }

        if (context.RetainRawResponse && context.MaximumRawResponseBytes < 0L)
        {
            throw new ValidationException("The request raw response limit cannot be negative.", "MaximumRawResponseBytes", "INVALID_LIMIT", null);
        }

        string[] responseSecrets = RequestCredentialStore.MergeResponseSecrets(response, secrets);
        HttpResponseMetadata metadata = CaptureMetadata(response, responseSecrets);
        ValidateTextFormat(expectedFormat, metadata, context.Service);
        context.CancellationToken.ThrowIfCancellationRequested();

        if (metadata.HasContentLength && metadata.ContentLength > limits.MaximumResponseBytes)
        {
            throw new ApiException("The HTTP response exceeds the configured buffered response limit.", context.Service, metadata.StatusCode, true, "RESPONSE_TOO_LARGE", null, metadata.RequestIdentifier, false, null);
        }

        global::System.IO.Stream input = response.Content.ReadAsStream(context.CancellationToken);
        byte[] body = ReadBoundedBody(input, limits.MaximumResponseBytes, context.CancellationToken, context.Service, metadata);
        if (metadata.HasContentLength && metadata.ContentLength != body.Length)
        {
            throw new ApiException("The HTTP response ended before or after its declared content length.", context.Service, metadata.StatusCode, true, "RESPONSE_LENGTH_MISMATCH", null, metadata.RequestIdentifier, false, null);
        }

        string text = DecodeResponseBytes(body, body.Length, metadata.CharacterSet, expectedFormat);
        BufferedHttpResponse result = new BufferedHttpResponse();
        result.Metadata = metadata;
        result.ExpectedFormat = expectedFormat;
        result.Text = text;
        result.BodyBytes = body.Length;
        if (context.RetainRawResponse)
        {
            long rawLimit = context.MaximumRawResponseBytes;
            if (rawLimit > limits.MaximumRawResponseBytes)
            {
                rawLimit = limits.MaximumRawResponseBytes;
            }

            string? safeRawText = SecretRedactor.RedactText(text, responseSecrets);
            byte[] safeRawBytes = global::System.Text.Encoding.UTF8.GetBytes(safeRawText ?? string.Empty);
            int retainedBytes = safeRawBytes.Length;
            if (retainedBytes > rawLimit)
            {
                retainedBytes = (int)rawLimit;
            }

            if (retainedBytes < safeRawBytes.Length)
            {
                retainedBytes = FindUtf8Boundary(safeRawBytes, retainedBytes);
            }

            result.RawText = global::System.Text.Encoding.UTF8.GetString(safeRawBytes, 0, retainedBytes);
            result.RawTextBytes = retainedBytes;
            result.HasRawText = true;
            result.IsRawTextTruncated = retainedBytes < safeRawBytes.Length;
        }

        return result;
    }

    internal static StreamedHttpResponse CopyBinaryResponse(global::System.Net.Http.HttpResponseMessage response, global::System.IO.Stream destination, int bufferSize, RequestContext context, string[]? secrets)
    {
        if (response == null)
        {
            throw new ValidationException("An HTTP response is required.", "response", "REQUIRED", null);
        }

        if (destination == null)
        {
            throw new ValidationException("A destination stream is required.", "destination", "REQUIRED", null);
        }

        if (!destination.CanWrite)
        {
            throw new ValidationException("The destination stream must be writable.", "destination", "STREAM_NOT_WRITABLE", null);
        }

        if (bufferSize <= 0)
        {
            throw new ValidationException("The binary copy buffer size must be positive.", "bufferSize", "INVALID_LIMIT", null);
        }

        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        HttpResponseMetadata metadata = CaptureMetadata(response, secrets);
        context.CancellationToken.ThrowIfCancellationRequested();
        global::System.IO.Stream input = response.Content.ReadAsStream(context.CancellationToken);
        byte[] buffer = new byte[bufferSize];
        long total = 0L;
        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            int read = input.ReadAsync(buffer, 0, buffer.Length, context.CancellationToken).GetAwaiter().GetResult();
            if (read == 0)
            {
                break;
            }

            destination.WriteAsync(buffer, 0, read, context.CancellationToken).GetAwaiter().GetResult();
            total += read;
        }

        if (metadata.HasContentLength && metadata.ContentLength != total)
        {
            throw new ApiException("The HTTP response ended before or after its declared content length.", context.Service, metadata.StatusCode, true, "RESPONSE_LENGTH_MISMATCH", null, metadata.RequestIdentifier, false, null);
        }

        StreamedHttpResponse result = new StreamedHttpResponse();
        result.Metadata = metadata;
        result.BytesCopied = total;
        return result;
    }

    internal static StreamedHttpResponse CopyBoundedBinaryResponse(global::System.Net.Http.HttpResponseMessage response, global::System.IO.Stream destination, RequestContext context, string[]? secrets, DownloadPolicy policy, DownloadRequest request, string? fileName)
    {
        if (response == null)
        {
            throw new ValidationException("An HTTP response is required.", "response", "REQUIRED", null);
        }

        if (destination == null || !destination.CanWrite)
        {
            throw new ValidationException("A writable destination stream is required.", "destination", "STREAM_NOT_WRITABLE", null);
        }

        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        if (policy == null || policy.BufferSize <= 0)
        {
            throw new ValidationException("A valid download policy is required.", "policy", "INVALID_LIMIT", null);
        }

        if (request == null)
        {
            throw new ValidationException("A download request is required.", "request", "REQUIRED", null);
        }

        HttpResponseMetadata metadata = CaptureMetadata(response, secrets);
        global::System.IO.Stream input = response.Content.ReadAsStream(context.CancellationToken);
        byte[] buffer = new byte[policy.BufferSize];
        long total = 0L;
        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            int read = input.ReadAsync(buffer, 0, buffer.Length, context.CancellationToken).GetAwaiter().GetResult();
            if (read == 0)
            {
                break;
            }

            if (policy.HasMaximumBytes && total > policy.MaximumBytes - read)
            {
                return CreateStreamFailure(metadata, total, "PAYLOAD_TOO_LARGE", "The patent document exceeds the configured download limit.");
            }

            if (request.HasExpectedSize && total > request.ExpectedSize - read)
            {
                return CreateStreamFailure(metadata, total, "RESPONSE_LENGTH_MISMATCH", "The patent document is larger than its expected size.");
            }

            destination.WriteAsync(buffer, 0, read, context.CancellationToken).GetAwaiter().GetResult();
            total += read;
            ReportBinaryProgress(request, metadata, total, fileName);
        }

        if (metadata.HasContentLength && metadata.ContentLength != total)
        {
            return CreateStreamFailure(metadata, total, "RESPONSE_LENGTH_MISMATCH", "The patent document length does not match the HTTP response length.");
        }

        if (request.HasExpectedSize && request.ExpectedSize != total)
        {
            return CreateStreamFailure(metadata, total, "RESPONSE_LENGTH_MISMATCH", "The patent document length does not match its expected size.");
        }

        StreamedHttpResponse result = new StreamedHttpResponse();
        result.Metadata = metadata;
        result.BytesCopied = total;
        return result;
    }

    internal static StreamedHttpResponse CopyChecksummedBinaryResponse(global::System.Net.Http.HttpResponseMessage response, global::System.IO.Stream destination, RequestContext context, string[]? secrets, DownloadPolicy policy, DownloadRequest request, string? fileName, BulkChecksumSession checksumSession)
    {
        if (response == null)
        {
            throw new ValidationException("An HTTP response is required.", "response", "REQUIRED", null);
        }

        if (destination == null || !destination.CanWrite)
        {
            throw new ValidationException("A writable destination stream is required.", "destination", "STREAM_NOT_WRITABLE", null);
        }

        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        if (policy == null || policy.BufferSize <= 0)
        {
            throw new ValidationException("A valid download policy is required.", "policy", "INVALID_LIMIT", null);
        }

        if (request == null || checksumSession == null)
        {
            throw new ValidationException("A download request and checksum session are required.", "request", "REQUIRED", null);
        }

        HttpResponseMetadata metadata = CaptureMetadata(response, secrets);
        global::System.IO.Stream input = response.Content.ReadAsStream(context.CancellationToken);
        byte[] buffer = new byte[policy.BufferSize];
        long total = 0L;
        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            int read = input.ReadAsync(buffer, 0, buffer.Length, context.CancellationToken).GetAwaiter().GetResult();
            if (read == 0)
            {
                break;
            }

            if (policy.HasMaximumBytes && total > policy.MaximumBytes - read)
            {
                return CreateStreamFailure(metadata, total, "PAYLOAD_TOO_LARGE", "The bulk download exceeds the configured download limit.");
            }

            if (request.HasExpectedSize && total > request.ExpectedSize - read)
            {
                return CreateStreamFailure(metadata, total, "RESPONSE_LENGTH_MISMATCH", "The bulk download is larger than its expected size.");
            }

            destination.WriteAsync(buffer, 0, read, context.CancellationToken).GetAwaiter().GetResult();
            checksumSession.Append(buffer, 0, read);
            total += read;
            ReportBinaryProgress(request, metadata, total, fileName);
        }

        if (metadata.HasContentLength && metadata.ContentLength != total)
        {
            return CreateStreamFailure(metadata, total, "RESPONSE_LENGTH_MISMATCH", "The bulk download length does not match the HTTP response length.");
        }

        if (request.HasExpectedSize && request.ExpectedSize != total)
        {
            return CreateStreamFailure(metadata, total, "RESPONSE_LENGTH_MISMATCH", "The bulk download length does not match its expected size.");
        }

        StreamedHttpResponse result = new StreamedHttpResponse();
        result.Metadata = metadata;
        result.BytesCopied = total;
        return result;
    }

    internal static StreamedHttpResponse CreateStreamFailure(HttpResponseMetadata metadata, long bytesCopied, string failureCode, string failureMessage)
    {
        StreamedHttpResponse result = new StreamedHttpResponse();
        result.Metadata = metadata;
        result.BytesCopied = bytesCopied;
        result.FailureCode = failureCode;
        result.FailureMessage = failureMessage;
        return result;
    }

    internal static void ReportBinaryProgress(DownloadRequest request, HttpResponseMetadata metadata, long bytesCopied, string? fileName)
    {
        if (request.Progress == null)
        {
            return;
        }

        DownloadProgress progress = new DownloadProgress();
        progress.State = DownloadState.Downloading;
        progress.BytesTransferred = bytesCopied;
        progress.FileName = fileName;
        progress.Timestamp = global::System.DateTimeOffset.UtcNow;
        progress.HasTimestamp = true;
        if (metadata.HasContentLength)
        {
            progress.TotalBytes = metadata.ContentLength;
            progress.HasTotalBytes = true;
        }
        else if (request.HasExpectedSize)
        {
            progress.TotalBytes = request.ExpectedSize;
            progress.HasTotalBytes = true;
        }

        request.Progress.Report(progress);
    }

    internal static HttpResponseMetadata CaptureMetadata(global::System.Net.Http.HttpResponseMessage response, string[]? secrets)
    {
        string[] responseSecrets = RequestCredentialStore.MergeResponseSecrets(response, secrets);
        HttpResponseMetadata metadata = new HttpResponseMetadata();
        metadata.StatusCode = (int)response.StatusCode;
        metadata.ReasonPhrase = SecretRedactor.RedactText(response.ReasonPhrase, responseSecrets);
        if (response.RequestMessage != null && response.RequestMessage.RequestUri != null)
        {
            metadata.RequestUri = SecretRedactor.RedactUri(response.RequestMessage.RequestUri, responseSecrets);
        }

        if (response.Content.Headers.ContentType != null)
        {
            metadata.MediaType = response.Content.Headers.ContentType.MediaType;
            metadata.CharacterSet = response.Content.Headers.ContentType.CharSet;
            metadata.Format = DetectDocumentFormat(metadata.MediaType);
        }

        if (response.Content.Headers.ContentLength.HasValue)
        {
            metadata.ContentLength = response.Content.Headers.ContentLength.GetValueOrDefault();
            metadata.HasContentLength = true;
        }

        global::System.Collections.Generic.Dictionary<string, string[]> responseHeaders = SecretRedactor.RedactHeaders(response.Headers, responseSecrets);
        global::System.Collections.Generic.Dictionary<string, string[]> contentHeaders = SecretRedactor.RedactHeaders(response.Content.Headers, responseSecrets);
        foreach (global::System.Collections.Generic.KeyValuePair<string, string[]> header in responseHeaders)
        {
            metadata.Headers.Add(header.Key, header.Value);
        }

        foreach (global::System.Collections.Generic.KeyValuePair<string, string[]> header in contentHeaders)
        {
            if (!metadata.Headers.ContainsKey(header.Key))
            {
                metadata.Headers.Add(header.Key, header.Value);
            }
            else
            {
                string[] existing = metadata.Headers[header.Key];
                string[] combined = new string[existing.Length + header.Value.Length];
                global::System.Array.Copy(existing, 0, combined, 0, existing.Length);
                global::System.Array.Copy(header.Value, 0, combined, existing.Length, header.Value.Length);
                metadata.Headers[header.Key] = combined;
            }
        }

        if (metadata.Headers.ContainsKey(UsptoHeaders.RequestIdentifier))
        {
            string[] requestIdentifiers = metadata.Headers[UsptoHeaders.RequestIdentifier];
            if (requestIdentifiers.Length != 0 && !global::System.String.IsNullOrWhiteSpace(requestIdentifiers[0]))
            {
                metadata.RequestIdentifier = requestIdentifiers[0];
            }
        }

        return metadata;
    }

    internal static void ValidateTextFormat(DocumentFormat expectedFormat, HttpResponseMetadata metadata, ApiService service)
    {
        bool supportedExpectedFormat = expectedFormat == DocumentFormat.Json || expectedFormat == DocumentFormat.Xml || expectedFormat == DocumentFormat.Html || expectedFormat == DocumentFormat.Csv || expectedFormat == DocumentFormat.Text;
        if (!supportedExpectedFormat)
        {
            throw new ValidationException("Buffered response reading requires a JSON, XML, HTML, CSV, or text format.", "expectedFormat", "UNSUPPORTED_FORMAT", null);
        }

        if (metadata.MediaType == null)
        {
            return;
        }

        bool matches = false;
        string mediaType = metadata.MediaType.ToLowerInvariant();
        switch (expectedFormat)
        {
            case DocumentFormat.Json:
                matches = mediaType == UsptoMediaTypes.Json || mediaType.EndsWith("+json", global::System.StringComparison.Ordinal);
                break;
            case DocumentFormat.Xml:
                matches = mediaType == UsptoMediaTypes.Xml || mediaType == UsptoMediaTypes.TextXml || mediaType.EndsWith("+xml", global::System.StringComparison.Ordinal);
                break;
            case DocumentFormat.Html:
                matches = mediaType == UsptoMediaTypes.Html;
                break;
            case DocumentFormat.Csv:
                matches = mediaType == UsptoMediaTypes.Csv;
                break;
            case DocumentFormat.Text:
                matches = mediaType.StartsWith("text/", global::System.StringComparison.Ordinal);
                break;
            default:
                matches = false;
                break;
        }

        if (!matches)
        {
            throw new ApiException("The HTTP response media type does not match the expected text representation.", service, metadata.StatusCode, true, "UNEXPECTED_MEDIA_TYPE", metadata.MediaType, metadata.RequestIdentifier, false, null);
        }
    }

    internal static byte[] ReadBoundedBody(global::System.IO.Stream input, long maximumBytes, global::System.Threading.CancellationToken cancellationToken, ApiService service, HttpResponseMetadata metadata)
    {
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        byte[] buffer = new byte[UsptoDefaults.DownloadBufferSize];
        long total = 0L;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long remaining = maximumBytes - total;
            int requested = buffer.Length;
            if (remaining < buffer.Length)
            {
                requested = (int)remaining + 1;
            }

            int read = input.ReadAsync(buffer, 0, requested, cancellationToken).GetAwaiter().GetResult();
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > maximumBytes)
            {
                throw new ApiException("The HTTP response exceeds the configured buffered response limit.", service, metadata.StatusCode, true, "RESPONSE_TOO_LARGE", null, metadata.RequestIdentifier, false, null);
            }

            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }

    internal static string DecodeResponseBytes(byte[] bytes, int count, string? characterSet, DocumentFormat format)
    {
        if (count >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            return new global::System.Text.UTF32Encoding(true, true, false).GetString(bytes, 4, count - 4);
        }

        if (count >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            return new global::System.Text.UTF32Encoding(false, true, false).GetString(bytes, 4, count - 4);
        }

        if (count >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return global::System.Text.Encoding.UTF8.GetString(bytes, 3, count - 3);
        }

        if (count >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return global::System.Text.Encoding.BigEndianUnicode.GetString(bytes, 2, count - 2);
        }

        if (count >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return global::System.Text.Encoding.Unicode.GetString(bytes, 2, count - 2);
        }

        if (characterSet == null || global::System.String.IsNullOrWhiteSpace(characterSet))
        {
            return global::System.Text.Encoding.UTF8.GetString(bytes, 0, count);
        }

        string normalized = characterSet.Trim().Trim('"').ToLowerInvariant();
        switch (normalized)
        {
            case "utf-8":
            case "utf8":
                return global::System.Text.Encoding.UTF8.GetString(bytes, 0, count);
            case "utf-16":
            case "utf-16le":
            case "unicode":
                return global::System.Text.Encoding.Unicode.GetString(bytes, 0, count);
            case "utf-16be":
            case "unicodefffe":
                return global::System.Text.Encoding.BigEndianUnicode.GetString(bytes, 0, count);
            case "utf-32":
            case "utf-32le":
                return global::System.Text.Encoding.UTF32.GetString(bytes, 0, count);
            case "utf-32be":
                return new global::System.Text.UTF32Encoding(true, false, false).GetString(bytes, 0, count);
            case "us-ascii":
            case "ascii":
                return global::System.Text.Encoding.ASCII.GetString(bytes, 0, count);
            case "iso-8859-1":
            case "latin1":
                return global::System.Text.Encoding.Latin1.GetString(bytes, 0, count);
            default:
                throw new ParseException("The HTTP response character set is not supported.", format, null, null, 0L, false, 0, false, null);
        }
    }

    internal static DocumentFormat DetectDocumentFormat(string? mediaType)
    {
        if (mediaType == null)
        {
            return DocumentFormat.Unknown;
        }

        string normalized = mediaType.ToLowerInvariant();
        if (normalized == UsptoMediaTypes.Json || normalized.EndsWith("+json", global::System.StringComparison.Ordinal))
        {
            return DocumentFormat.Json;
        }

        if (normalized == UsptoMediaTypes.Xml || normalized == UsptoMediaTypes.TextXml || normalized.EndsWith("+xml", global::System.StringComparison.Ordinal))
        {
            return DocumentFormat.Xml;
        }

        if (normalized == UsptoMediaTypes.Html)
        {
            return DocumentFormat.Html;
        }

        if (normalized == UsptoMediaTypes.Pdf)
        {
            return DocumentFormat.Pdf;
        }

        if (normalized == UsptoMediaTypes.Zip)
        {
            return DocumentFormat.Zip;
        }

        if (normalized == UsptoMediaTypes.Csv)
        {
            return DocumentFormat.Csv;
        }

        if (normalized.StartsWith("text/", global::System.StringComparison.Ordinal))
        {
            return DocumentFormat.Text;
        }

        return DocumentFormat.Unknown;
    }

    internal static int FindUtf8Boundary(byte[] bytes, int count)
    {
        if (count == 0)
        {
            return count;
        }

        int start = count - 1;
        while (start > 0 && (bytes[start] & 0xC0) == 0x80)
        {
            start--;
        }

        int requiredBytes;
        byte leadingByte = bytes[start];
        if ((leadingByte & 0x80) == 0)
        {
            requiredBytes = 1;
        }
        else if ((leadingByte & 0xE0) == 0xC0)
        {
            requiredBytes = 2;
        }
        else if ((leadingByte & 0xF0) == 0xE0)
        {
            requiredBytes = 3;
        }
        else
        {
            requiredBytes = 4;
        }

        if (count - start < requiredBytes)
        {
            return start;
        }

        return count;
    }
}
