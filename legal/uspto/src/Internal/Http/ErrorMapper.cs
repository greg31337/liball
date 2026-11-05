namespace uspto;

internal class ErrorBodyCapture
{
    internal string Text = string.Empty;
    internal bool IsEmpty = true;
    internal bool IsTruncated = false;
}

internal class ParsedServiceError
{
    internal string? Code = null;
    internal string? Message = null;
    internal string? Details = null;
    internal string? RequestIdentifier = null;
    internal bool IsEmpty = false;
    internal bool IsMalformed = false;
    internal bool IsTruncated = false;
    internal bool HasRecognizedField = false;
}

internal class RetryAfterValue
{
    internal long Milliseconds = 0L;
    internal bool HasValue = false;
}

internal static class ErrorMapper
{
    internal static global::System.Exception MapResponse(global::System.Net.Http.HttpResponseMessage response, ApiService service, string? resourceIdentifier, UsptoLimits limits, string[]? secrets, global::System.Threading.CancellationToken cancellationToken)
    {
        if (response == null)
        {
            throw new ValidationException("An HTTP response is required.", "response", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("Error response limits are required.", "limits", "REQUIRED", null);
        }

        long maximumBytes = limits.MaximumRawResponseBytes;
        if (maximumBytes > limits.MaximumResponseBytes)
        {
            maximumBytes = limits.MaximumResponseBytes;
        }

        if (maximumBytes <= 0L || maximumBytes > global::System.Int32.MaxValue)
        {
            throw new ValidationException("The error body limit must be between one byte and the largest supported array.", "MaximumRawResponseBytes", "INVALID_LIMIT", null);
        }

        if (limits.MaximumStringCharacters <= 0)
        {
            throw new ValidationException("The error detail character limit must be positive.", "MaximumStringCharacters", "INVALID_LIMIT", null);
        }

        string[] responseSecrets = RequestCredentialStore.MergeResponseSecrets(response, secrets);
        HttpResponseMetadata metadata = ResponseReader.CaptureMetadata(response, responseSecrets);
        cancellationToken.ThrowIfCancellationRequested();
        ErrorBodyCapture capture = ReadErrorBody(response, maximumBytes, cancellationToken);
        ParsedServiceError parsed = ParseErrorBody(capture, metadata, limits.MaximumStringCharacters, responseSecrets);
        RetryAfterValue retryAfter = ParseRetryAfter(metadata.Headers);
        int statusCode = metadata.StatusCode;
        string? safeResourceIdentifier = LimitErrorText(SecretRedactor.RedactText(resourceIdentifier, responseSecrets), limits.MaximumStringCharacters);

        if (statusCode == 401)
        {
            return new AuthenticationException("The remote service rejected the request credential.", service, statusCode, true, parsed.RequestIdentifier, null);
        }

        if (statusCode == 403)
        {
            return new AuthenticationException("The remote service refused authorization for the request.", service, statusCode, true, parsed.RequestIdentifier, null);
        }

        if (statusCode == 404)
        {
            return new NotFoundException("The requested remote resource was not found.", service, safeResourceIdentifier, statusCode, true, parsed.RequestIdentifier, null);
        }

        if (statusCode == 406 && TsdrPolicy.IsTsdrService(service))
        {
            string? negotiationDetails = parsed.Details;
            if (negotiationDetails == null)
            {
                negotiationDetails = parsed.Message;
            }

            return new ApiException("The TSDR endpoint does not support the requested content negotiation.", service, statusCode, true, "CONTENT_NEGOTIATION_NOT_SUPPORTED", negotiationDetails, parsed.RequestIdentifier, false, null);
        }

        if (statusCode == 429)
        {
            return new RateLimitException("The remote service rate limit rejected the request.", service, statusCode, true, parsed.RequestIdentifier, retryAfter.Milliseconds, retryAfter.HasValue, null);
        }

        if (statusCode == 500 || statusCode == 502 || statusCode == 503 || statusCode == 504)
        {
            return new ServiceUnavailableException("The remote service is temporarily unavailable.", service, statusCode, true, parsed.RequestIdentifier, retryAfter.Milliseconds, retryAfter.HasValue, null);
        }

        string fallbackCode = SelectFallbackCode(parsed, statusCode);
        string errorCode = parsed.Code ?? fallbackCode;
        string? errorDetails = parsed.Details;
        if (errorDetails == null)
        {
            errorDetails = parsed.Message;
        }

        string exceptionMessage = "The remote service returned an error response.";
        if (statusCode == 413)
        {
            exceptionMessage = "The remote service rejected a payload that was too large.";
            if (parsed.Code == null)
            {
                errorCode = "PAYLOAD_TOO_LARGE";
            }
        }
        else if (statusCode == 400 || statusCode == 422)
        {
            exceptionMessage = "The remote service rejected the request as invalid.";
        }

        bool isRetryable = statusCode == 408 || statusCode == 425;
        return new ApiException(exceptionMessage, service, statusCode, true, errorCode, errorDetails, parsed.RequestIdentifier, isRetryable, null);
    }

    internal static global::System.Exception MapTsdrDownloadResponse(global::System.Net.Http.HttpResponseMessage response, ApiService service, string? resourceIdentifier, UsptoLimits limits, string[]? secrets, bool isBundleRequest, global::System.Threading.CancellationToken cancellationToken)
    {
        global::System.Exception failure = MapResponse(response, service, resourceIdentifier, limits, secrets, cancellationToken);
        if ((int)response.StatusCode == 413 && isBundleRequest && RetryClassifier.HasExceptionType(failure, "uspto.ApiException"))
        {
            ApiException apiFailure = (ApiException)failure;
            apiFailure.ErrorCode = "BUNDLE_TOO_LARGE";
        }

        return failure;
    }

    internal static ErrorBodyCapture ReadErrorBody(global::System.Net.Http.HttpResponseMessage response, long maximumBytes, global::System.Threading.CancellationToken cancellationToken)
    {
        ErrorBodyCapture capture = new ErrorBodyCapture();
        global::System.IO.Stream input = response.Content.ReadAsStream(cancellationToken);
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

            int accepted = read;
            if (total + read > maximumBytes)
            {
                accepted = (int)(maximumBytes - total);
                capture.IsTruncated = true;
            }

            if (accepted > 0)
            {
                output.Write(buffer, 0, accepted);
                total += accepted;
            }

            if (capture.IsTruncated)
            {
                break;
            }
        }

        if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.GetValueOrDefault() > total)
        {
            capture.IsTruncated = true;
        }

        byte[] bytes = output.ToArray();
        int decodedCount = bytes.Length;
        if (capture.IsTruncated)
        {
            decodedCount = ResponseReader.FindUtf8Boundary(bytes, decodedCount);
        }

        string? characterSet = null;
        DocumentFormat format = DocumentFormat.Text;
        if (response.Content.Headers.ContentType != null)
        {
            characterSet = SelectErrorCharacterSet(response.Content.Headers.ContentType.CharSet);
            format = ResponseReader.DetectDocumentFormat(response.Content.Headers.ContentType.MediaType);
        }

        capture.Text = ResponseReader.DecodeResponseBytes(bytes, decodedCount, characterSet, format);
        capture.IsEmpty = global::System.String.IsNullOrWhiteSpace(capture.Text);
        return capture;
    }

    internal static string? SelectErrorCharacterSet(string? characterSet)
    {
        if (global::System.String.IsNullOrWhiteSpace(characterSet))
        {
            return null;
        }

        string normalized = characterSet.Trim().Trim('"').ToLowerInvariant();
        switch (normalized)
        {
            case "utf-8":
            case "utf8":
            case "utf-16":
            case "utf-16le":
            case "unicode":
            case "utf-16be":
            case "unicodefffe":
            case "utf-32":
            case "utf-32le":
            case "utf-32be":
            case "us-ascii":
            case "ascii":
            case "iso-8859-1":
            case "latin1":
                return normalized;
            default:
                return null;
        }
    }

    internal static ParsedServiceError ParseErrorBody(ErrorBodyCapture capture, HttpResponseMetadata metadata, int maximumCharacters, string[]? secrets)
    {
        ParsedServiceError parsed = new ParsedServiceError();
        parsed.IsEmpty = capture.IsEmpty;
        parsed.IsTruncated = capture.IsTruncated;
        parsed.RequestIdentifier = metadata.RequestIdentifier;
        if (capture.IsEmpty)
        {
            parsed.RequestIdentifier = LimitErrorText(SecretRedactor.RedactText(parsed.RequestIdentifier, secrets), maximumCharacters);
            return parsed;
        }

        string text = capture.Text.Trim();
        char first = text[0];
        if (first == '{' || first == '[')
        {
            parsed.IsMalformed = IsMalformedStructuredText(text, '{', '}', '[', ']');
            parsed.Code = FindFirstJsonField(text, new string[] { "errorCode", "code", "statusCode", "status" });
            parsed.Message = FindFirstJsonField(text, new string[] { "message", "error", "description", "title" });
            if (parsed.Message == "true" || parsed.Message == "false")
            {
                parsed.Message = null;
            }

            parsed.Details = FindFirstJsonField(text, new string[] { "detailedError", "detailedMessage", "errorDetails", "detail", "details", "reason" });
            string? bodyRequestIdentifier = FindFirstJsonField(text, new string[] { "requestIdentifier", "requestId", "correlationId", "traceId" });
            if (!global::System.String.IsNullOrWhiteSpace(bodyRequestIdentifier))
            {
                parsed.RequestIdentifier = bodyRequestIdentifier;
            }
        }
        else if (first == '<')
        {
            if (metadata.Format != DocumentFormat.Html)
            {
                parsed.IsMalformed = IsMalformedXmlText(text);
            }

            parsed.Code = FindFirstXmlField(text, new string[] { "errorCode", "code", "statusCode", "status" });
            parsed.Message = FindFirstXmlField(text, new string[] { "message", "error", "description", "title" });
            parsed.Details = FindFirstXmlField(text, new string[] { "detailedError", "detailedMessage", "errorDetails", "detail", "details", "reason" });
            string? bodyRequestIdentifier = FindFirstXmlField(text, new string[] { "requestIdentifier", "requestId", "correlationId", "traceId" });
            if (!global::System.String.IsNullOrWhiteSpace(bodyRequestIdentifier))
            {
                parsed.RequestIdentifier = bodyRequestIdentifier;
            }

            if (parsed.Message == null && parsed.Details == null)
            {
                parsed.Details = StripMarkup(text);
            }
        }
        else
        {
            parsed.Details = text;
        }

        if (metadata.Headers.ContainsKey("X-Status-Reason-Code") && parsed.Code == null)
        {
            string[] values = metadata.Headers["X-Status-Reason-Code"];
            if (values.Length != 0)
            {
                parsed.Code = values[0];
            }
        }

        if (metadata.Headers.ContainsKey("X-Status-Reason") && parsed.Details == null)
        {
            string[] values = metadata.Headers["X-Status-Reason"];
            if (values.Length != 0)
            {
                parsed.Details = values[0];
            }
        }

        parsed.Code = LimitErrorText(SecretRedactor.RedactText(parsed.Code, secrets), maximumCharacters);
        parsed.Message = LimitErrorText(SecretRedactor.RedactText(parsed.Message, secrets), maximumCharacters);
        parsed.Details = LimitErrorText(SecretRedactor.RedactText(parsed.Details, secrets), maximumCharacters);
        parsed.RequestIdentifier = LimitErrorText(SecretRedactor.RedactText(parsed.RequestIdentifier, secrets), maximumCharacters);
        parsed.HasRecognizedField = parsed.Code != null || parsed.Message != null || parsed.Details != null || parsed.RequestIdentifier != null;
        return parsed;
    }

    internal static string? FindFirstJsonField(string json, string[] names)
    {
        int nameIndex;
        for (nameIndex = 0; nameIndex < names.Length; nameIndex++)
        {
            string? value = FindJsonField(json, names[nameIndex]);
            if (!global::System.String.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    internal static string? FindJsonField(string json, string name)
    {
        string token = "\"" + name + "\"";
        int searchIndex = 0;
        while (searchIndex < json.Length)
        {
            int nameIndex = json.IndexOf(token, searchIndex, global::System.StringComparison.OrdinalIgnoreCase);
            if (nameIndex < 0)
            {
                return null;
            }

            int valueIndex = nameIndex + token.Length;
            while (valueIndex < json.Length && global::System.Char.IsWhiteSpace(json[valueIndex]))
            {
                valueIndex++;
            }

            if (valueIndex >= json.Length || json[valueIndex] != ':')
            {
                searchIndex = valueIndex;
                continue;
            }

            valueIndex++;
            while (valueIndex < json.Length && global::System.Char.IsWhiteSpace(json[valueIndex]))
            {
                valueIndex++;
            }

            if (valueIndex >= json.Length)
            {
                return null;
            }

            if (json[valueIndex] == '"')
            {
                int stringStart = valueIndex + 1;
                int stringIndex = stringStart;
                bool escaped = false;
                while (stringIndex < json.Length)
                {
                    char character = json[stringIndex];
                    if (!escaped && character == '"')
                    {
                        return UnescapeJsonValue(json.Substring(stringStart, stringIndex - stringStart));
                    }

                    if (!escaped && character == '\\')
                    {
                        escaped = true;
                    }
                    else
                    {
                        escaped = false;
                    }

                    stringIndex++;
                }

                return null;
            }

            int scalarEnd = valueIndex;
            while (scalarEnd < json.Length && json[scalarEnd] != ',' && json[scalarEnd] != '}' && json[scalarEnd] != ']' && json[scalarEnd] != '\r' && json[scalarEnd] != '\n')
            {
                scalarEnd++;
            }

            string scalar = json.Substring(valueIndex, scalarEnd - valueIndex).Trim();
            if (scalar == "null" || scalar.Length == 0 || scalar[0] == '{' || scalar[0] == '[')
            {
                return null;
            }

            return scalar;
        }

        return null;
    }

    internal static string UnescapeJsonValue(string value)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != '\\' || index + 1 >= value.Length)
            {
                builder.Append(character);
                index++;
                continue;
            }

            char escaped = value[index + 1];
            switch (escaped)
            {
                case '"':
                case '\\':
                case '/':
                    builder.Append(escaped);
                    index += 2;
                    break;
                case 'b':
                    builder.Append('\b');
                    index += 2;
                    break;
                case 'f':
                    builder.Append('\f');
                    index += 2;
                    break;
                case 'n':
                    builder.Append('\n');
                    index += 2;
                    break;
                case 'r':
                    builder.Append('\r');
                    index += 2;
                    break;
                case 't':
                    builder.Append('\t');
                    index += 2;
                    break;
                case 'u':
                    if (index + 5 < value.Length)
                    {
                        int first = HexValue(value[index + 2]);
                        int second = HexValue(value[index + 3]);
                        int third = HexValue(value[index + 4]);
                        int fourth = HexValue(value[index + 5]);
                        if (first >= 0 && second >= 0 && third >= 0 && fourth >= 0)
                        {
                            builder.Append((char)((first << 12) | (second << 8) | (third << 4) | fourth));
                            index += 6;
                            break;
                        }
                    }

                    builder.Append(escaped);
                    index += 2;
                    break;
                default:
                    builder.Append(escaped);
                    index += 2;
                    break;
            }
        }

        return builder.ToString();
    }

    internal static int HexValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        return -1;
    }

    internal static string? FindFirstXmlField(string xml, string[] names)
    {
        int nameIndex;
        for (nameIndex = 0; nameIndex < names.Length; nameIndex++)
        {
            string name = global::System.Text.RegularExpressions.Regex.Escape(names[nameIndex]);
            string pattern = "<(?:[A-Za-z_][A-Za-z0-9_.-]*:)?" + name + "(?:\\s[^>]*)?>(.*?)</(?:[A-Za-z_][A-Za-z0-9_.-]*:)?" + name + "\\s*>";
            global::System.Text.RegularExpressions.Match match = global::System.Text.RegularExpressions.Regex.Match(xml, pattern, global::System.Text.RegularExpressions.RegexOptions.IgnoreCase | global::System.Text.RegularExpressions.RegexOptions.Singleline | global::System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            if (match.Success)
            {
                string value = StripMarkup(match.Groups[1].Value);
                if (!global::System.String.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    internal static string StripMarkup(string text)
    {
        string value = text.Replace("<![CDATA[", string.Empty, global::System.StringComparison.OrdinalIgnoreCase).Replace("]]>", string.Empty, global::System.StringComparison.Ordinal);
        value = global::System.Text.RegularExpressions.Regex.Replace(value, "<[^>]*>", " ", global::System.Text.RegularExpressions.RegexOptions.Singleline | global::System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        return global::System.Net.WebUtility.HtmlDecode(value).Trim();
    }

    internal static bool IsMalformedStructuredText(string text, char objectOpen, char objectClose, char arrayOpen, char arrayClose)
    {
        int objectDepth = 0;
        int arrayDepth = 0;
        bool inString = false;
        bool escaped = false;
        bool rootClosed = false;
        int index;
        for (index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (rootClosed)
            {
                if (!global::System.Char.IsWhiteSpace(character))
                {
                    return true;
                }

                continue;
            }

            if (inString)
            {
                if (!escaped && character == '"')
                {
                    inString = false;
                }

                if (!escaped && character == '\\')
                {
                    escaped = true;
                }
                else
                {
                    escaped = false;
                }

                continue;
            }

            if (character == '"')
            {
                inString = true;
            }
            else if (character == objectOpen)
            {
                objectDepth++;
            }
            else if (character == objectClose)
            {
                objectDepth--;
            }
            else if (character == arrayOpen)
            {
                arrayDepth++;
            }
            else if (character == arrayClose)
            {
                arrayDepth--;
            }

            if (objectDepth < 0 || arrayDepth < 0)
            {
                return true;
            }

            if (index > 0 && objectDepth == 0 && arrayDepth == 0)
            {
                rootClosed = true;
            }
        }

        if (inString || objectDepth != 0 || arrayDepth != 0 || !rootClosed)
        {
            return true;
        }

        if (text[0] == objectOpen)
        {
            int contentIndex = 1;
            while (contentIndex < text.Length && global::System.Char.IsWhiteSpace(text[contentIndex]))
            {
                contentIndex++;
            }

            if (contentIndex < text.Length && text[contentIndex] != objectClose && text[contentIndex] != '"')
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsMalformedXmlText(string text)
    {
        global::System.Collections.Generic.List<string> elements = new global::System.Collections.Generic.List<string>();
        int searchIndex = 0;
        while (searchIndex < text.Length)
        {
            int openIndex = text.IndexOf('<', searchIndex);
            if (openIndex < 0)
            {
                break;
            }

            int closeIndex = text.IndexOf('>', openIndex + 1);
            if (closeIndex < 0)
            {
                return true;
            }

            string tag = text.Substring(openIndex + 1, closeIndex - openIndex - 1).Trim();
            if (tag.Length == 0)
            {
                return true;
            }

            if (tag[0] != '!' && tag[0] != '?')
            {
                bool closing = tag[0] == '/';
                bool selfClosing = tag[tag.Length - 1] == '/';
                int nameStart = closing ? 1 : 0;
                int nameEnd = nameStart;
                while (nameEnd < tag.Length && !global::System.Char.IsWhiteSpace(tag[nameEnd]) && tag[nameEnd] != '/')
                {
                    nameEnd++;
                }

                if (nameEnd == nameStart)
                {
                    return true;
                }

                string name = tag.Substring(nameStart, nameEnd - nameStart);
                if (closing)
                {
                    if (elements.Count == 0 || !global::System.String.Equals(elements[elements.Count - 1], name, global::System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    elements.RemoveAt(elements.Count - 1);
                }
                else if (!selfClosing)
                {
                    elements.Add(name);
                }
            }

            searchIndex = closeIndex + 1;
        }

        return elements.Count != 0;
    }

    internal static RetryAfterValue ParseRetryAfter(global::System.Collections.Generic.Dictionary<string, string[]> headers)
    {
        RetryAfterValue result = new RetryAfterValue();
        if (!headers.ContainsKey(UsptoHeaders.RetryAfter))
        {
            return result;
        }

        string[] values = headers[UsptoHeaders.RetryAfter];
        if (values.Length == 0)
        {
            return result;
        }

        string value = values[0].Trim();
        if (value.Length == 0)
        {
            return result;
        }

        long seconds = 0L;
        bool allDigits = true;
        int index;
        for (index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                allDigits = false;
                break;
            }

            int digit = character - '0';
            if (seconds > (global::System.Int64.MaxValue - digit) / 10L)
            {
                return result;
            }

            seconds = seconds * 10L + digit;
        }

        if (allDigits)
        {
            if (seconds > global::System.Int64.MaxValue / 1000L)
            {
                result.Milliseconds = global::System.Int64.MaxValue;
            }
            else
            {
                result.Milliseconds = seconds * 1000L;
            }

            result.HasValue = true;
            return result;
        }

        long dateDelay = ParseHttpDateDelay(value);
        if (dateDelay >= 0L)
        {
            result.Milliseconds = dateDelay;
            result.HasValue = true;
        }

        return result;
    }

    internal static long ParseHttpDateDelay(string value)
    {
        if (value.Length != 29 || value[3] != ',' || value[4] != ' ' || value[7] != ' ' || value[11] != ' ' || value[16] != ' ' || value[19] != ':' || value[22] != ':' || value[25] != ' ')
        {
            return -1L;
        }

        if (!global::System.String.Equals(value.Substring(26, 3), "GMT", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return -1L;
        }

        int day = ParseFixedDigits(value, 5, 2);
        int weekday = ParseHttpWeekday(value.Substring(0, 3));
        int month = ParseHttpMonth(value.Substring(8, 3));
        int year = ParseFixedDigits(value, 12, 4);
        int hour = ParseFixedDigits(value, 17, 2);
        int minute = ParseFixedDigits(value, 20, 2);
        int second = ParseFixedDigits(value, 23, 2);
        if (year < 1 || weekday < 0 || month < 1 || day < 1 || hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
        {
            return -1L;
        }

        if (day > global::System.DateTime.DaysInMonth(year, month))
        {
            return -1L;
        }

        global::System.DateTimeOffset retryAt = new global::System.DateTimeOffset(year, month, day, hour, minute, second, global::System.TimeSpan.Zero);
        if ((int)retryAt.DayOfWeek != weekday)
        {
            return -1L;
        }

        global::System.TimeSpan remaining = retryAt - global::System.DateTimeOffset.UtcNow;
        if (remaining <= global::System.TimeSpan.Zero)
        {
            return 0L;
        }

        return (long)global::System.Math.Ceiling(remaining.TotalMilliseconds);
    }

    internal static int ParseFixedDigits(string value, int start, int length)
    {
        int result = 0;
        int end = start + length;
        for (int index = start; index < end; index++)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            result = result * 10 + character - '0';
        }

        return result;
    }

    internal static int ParseHttpWeekday(string value)
    {
        if (global::System.String.Equals(value, "Sun", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (global::System.String.Equals(value, "Mon", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (global::System.String.Equals(value, "Tue", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (global::System.String.Equals(value, "Wed", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (global::System.String.Equals(value, "Thu", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (global::System.String.Equals(value, "Fri", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (global::System.String.Equals(value, "Sat", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        return -1;
    }

    internal static int ParseHttpMonth(string value)
    {
        if (global::System.String.Equals(value, "Jan", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (global::System.String.Equals(value, "Feb", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (global::System.String.Equals(value, "Mar", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (global::System.String.Equals(value, "Apr", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (global::System.String.Equals(value, "May", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (global::System.String.Equals(value, "Jun", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        if (global::System.String.Equals(value, "Jul", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 7;
        }

        if (global::System.String.Equals(value, "Aug", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 8;
        }

        if (global::System.String.Equals(value, "Sep", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 9;
        }

        if (global::System.String.Equals(value, "Oct", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        if (global::System.String.Equals(value, "Nov", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 11;
        }

        if (global::System.String.Equals(value, "Dec", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return 12;
        }

        return 0;
    }

    internal static string SelectFallbackCode(ParsedServiceError parsed, int statusCode)
    {
        if (parsed.IsEmpty)
        {
            return "EMPTY_ERROR_RESPONSE";
        }

        if (parsed.IsMalformed)
        {
            return "MALFORMED_ERROR_RESPONSE";
        }

        if (parsed.IsTruncated)
        {
            return "TRUNCATED_ERROR_RESPONSE";
        }

        if (!parsed.HasRecognizedField)
        {
            return "UNKNOWN_ERROR_RESPONSE";
        }

        return "HTTP_" + statusCode.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string? LimitErrorText(string? value, int maximumCharacters)
    {
        if (value == null)
        {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length <= maximumCharacters)
        {
            return trimmed;
        }

        return trimmed.Substring(0, maximumCharacters);
    }
}
