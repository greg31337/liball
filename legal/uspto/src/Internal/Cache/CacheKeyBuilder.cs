namespace uspto;

internal class CanonicalNameValue
{
    internal string Name = string.Empty;
    internal string Value = string.Empty;
    internal bool HasValue = false;
    internal int OriginalIndex = 0;
}

internal class CanonicalJsonProperty
{
    internal string Name = string.Empty;
    internal global::System.Text.Json.JsonElement Value = default(global::System.Text.Json.JsonElement);
    internal int OriginalIndex = 0;
}

internal static class CacheKeyBuilder
{
    internal static string Build(ApiService service, global::System.Net.Http.HttpRequestMessage request, string[]? credentialValues, global::System.Threading.CancellationToken cancellationToken)
    {
        string material = BuildCanonicalMaterial(service, request, credentialValues, cancellationToken);
        byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes(material);
        byte[] digest = global::System.Security.Cryptography.SHA256.HashData(bytes);
        return "sha256:" + global::System.Convert.ToHexString(digest).ToLowerInvariant();
    }

    internal static string BuildCanonicalMaterial(ApiService service, global::System.Net.Http.HttpRequestMessage request, string[]? credentialValues, global::System.Threading.CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ValidationException("An HTTP request is required for cache-key construction.", "request", "REQUIRED", null);
        }

        if (request.RequestUri == null || !request.RequestUri.IsAbsoluteUri)
        {
            throw new ValidationException("The cache-key request URI must be absolute.", "request", "ABSOLUTE_URI_REQUIRED", null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        string canonicalUri = CanonicalizeUri(request.RequestUri, credentialValues);
        global::System.Text.StringBuilder headers = new global::System.Text.StringBuilder();
        AppendSelectedHeader(headers, request.Headers, "Accept", credentialValues);
        AppendSelectedHeader(headers, request.Headers, "Accept-Encoding", credentialValues);
        AppendSelectedHeader(headers, request.Headers, "Accept-Language", credentialValues);
        AppendSelectedHeader(headers, request.Headers, "Range", credentialValues);
        if (request.Content != null)
        {
            AppendSelectedHeader(headers, request.Content.Headers, "Content-Language", credentialValues);
            AppendSelectedHeader(headers, request.Content.Headers, "Content-Type", credentialValues);
        }

        string canonicalBody = CanonicalizeBody(request.Content, credentialValues, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        global::System.Text.StringBuilder material = new global::System.Text.StringBuilder();
        AppendMaterialComponent(material, "service", ((int)service).ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        AppendMaterialComponent(material, "method", request.Method.Method.ToUpperInvariant());
        AppendMaterialComponent(material, "uri", canonicalUri);
        AppendMaterialComponent(material, "headers", headers.ToString());
        AppendMaterialComponent(material, "body", canonicalBody);
        return material.ToString();
    }

    internal static string CanonicalizeUri(global::System.Uri uri, string[]? credentialValues)
    {
        string scheme = uri.Scheme.ToLowerInvariant();
        string host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        if (uri.HostNameType == global::System.UriHostNameType.IPv6)
        {
            host = "[" + host + "]";
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        builder.Append(scheme);
        builder.Append("://");
        builder.Append(host);
        if (!uri.IsDefaultPort)
        {
            builder.Append(':');
            builder.Append(uri.Port.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        string path = NormalizePercentEncoding(uri.AbsolutePath);
        string? safePath = SecretRedactor.RedactText(path, credentialValues);
        if (global::System.String.IsNullOrEmpty(safePath))
        {
            builder.Append('/');
        }
        else
        {
            builder.Append(safePath);
        }

        string query = CanonicalizeQuery(uri.Query, credentialValues);
        if (query.Length != 0)
        {
            builder.Append('?');
            builder.Append(query);
        }

        return builder.ToString();
    }

    internal static string CanonicalizeQuery(string query, string[]? credentialValues)
    {
        if (query.Length == 0)
        {
            return string.Empty;
        }

        string source = query;
        if (source[0] == '?')
        {
            source = source.Substring(1);
        }

        string[] rawParts = source.Split('&');
        global::System.Collections.Generic.List<CanonicalNameValue> parts = new global::System.Collections.Generic.List<CanonicalNameValue>();
        for (int partIndex = 0; partIndex < rawParts.Length; partIndex += 1)
        {
            string rawPart = rawParts[partIndex];
            int equalsIndex = rawPart.IndexOf('=');
            string rawName = rawPart;
            string rawValue = string.Empty;
            bool hasValue = false;
            if (equalsIndex >= 0)
            {
                rawName = rawPart.Substring(0, equalsIndex);
                rawValue = rawPart.Substring(equalsIndex + 1);
                hasValue = true;
            }

            string decodedName = global::System.Uri.UnescapeDataString(rawName);
            if (!SecretRedactor.IsSensitiveName(decodedName))
            {
                string decodedValue = global::System.Uri.UnescapeDataString(rawValue);
                string? safeName = SecretRedactor.RedactText(decodedName, credentialValues);
                string? safeValue = SecretRedactor.RedactText(decodedValue, credentialValues);
                CanonicalNameValue part = new CanonicalNameValue();
                part.Name = global::System.Uri.EscapeDataString(safeName ?? string.Empty);
                part.Value = global::System.Uri.EscapeDataString(safeValue ?? string.Empty);
                part.HasValue = hasValue;
                part.OriginalIndex = partIndex;
                parts.Add(part);
            }
        }

        SortNameValues(parts);
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        for (int index = 0; index < parts.Count; index += 1)
        {
            if (index != 0)
            {
                builder.Append('&');
            }

            CanonicalNameValue part = parts[index];
            builder.Append(part.Name);
            if (part.HasValue)
            {
                builder.Append('=');
                builder.Append(part.Value);
            }
        }

        return builder.ToString();
    }

    internal static string CanonicalizeBody(global::System.Net.Http.HttpContent? content, string[]? credentialValues, global::System.Threading.CancellationToken cancellationToken)
    {
        if (content == null)
        {
            return string.Empty;
        }

        cancellationToken.ThrowIfCancellationRequested();
        string? mediaType = null;
        if (content.Headers.ContentType != null)
        {
            mediaType = content.Headers.ContentType.MediaType;
        }

        if (mediaType != null && (global::System.String.Equals(mediaType, UsptoMediaTypes.Json, global::System.StringComparison.OrdinalIgnoreCase) || mediaType.EndsWith("+json", global::System.StringComparison.OrdinalIgnoreCase)))
        {
            string json = content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            return CanonicalizeJson(json, credentialValues);
        }

        if (mediaType != null && global::System.String.Equals(mediaType, UsptoMediaTypes.FormUrlEncoded, global::System.StringComparison.OrdinalIgnoreCase))
        {
            string form = content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            return CanonicalizeForm(form, credentialValues);
        }

        byte[] body = content.ReadAsByteArrayAsync(cancellationToken).GetAwaiter().GetResult();
        byte[] digest = global::System.Security.Cryptography.SHA256.HashData(body);
        return "binary-sha256:" + global::System.Convert.ToHexString(digest).ToLowerInvariant();
    }

    internal static string CanonicalizeJson(string json, string[]? credentialValues)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
            using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
            {
                WriteCanonicalJson(writer, document.RootElement, credentialValues);
                writer.Flush();
            }

            string canonical = global::System.Text.Encoding.UTF8.GetString(output.ToArray());
            output.Dispose();
            return canonical;
        }
    }

    internal static void WriteCanonicalJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonElement element, string[]? credentialValues)
    {
        if (element.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            global::System.Collections.Generic.List<CanonicalJsonProperty> properties = new global::System.Collections.Generic.List<CanonicalJsonProperty>();
            int propertyIndex = 0;
            foreach (global::System.Text.Json.JsonProperty property in element.EnumerateObject())
            {
                if (!SecretRedactor.IsSensitiveName(property.Name))
                {
                    CanonicalJsonProperty item = new CanonicalJsonProperty();
                    string? safeName = SecretRedactor.RedactText(property.Name, credentialValues);
                    item.Name = safeName ?? string.Empty;
                    item.Value = property.Value;
                    item.OriginalIndex = propertyIndex;
                    properties.Add(item);
                }

                propertyIndex += 1;
            }

            SortJsonProperties(properties);
            writer.WriteStartObject();
            foreach (CanonicalJsonProperty property in properties)
            {
                writer.WritePropertyName(property.Name);
                WriteCanonicalJson(writer, property.Value, credentialValues);
            }

            writer.WriteEndObject();
            return;
        }

        if (element.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (global::System.Text.Json.JsonElement item in element.EnumerateArray())
            {
                WriteCanonicalJson(writer, item, credentialValues);
            }

            writer.WriteEndArray();
            return;
        }

        if (element.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            string? safeValue = SecretRedactor.RedactText(element.GetString(), credentialValues);
            writer.WriteStringValue(safeValue);
            return;
        }

        if (element.ValueKind == global::System.Text.Json.JsonValueKind.Number)
        {
            writer.WriteRawValue(NormalizeJsonNumber(element.GetRawText()), true);
            return;
        }

        if (element.ValueKind == global::System.Text.Json.JsonValueKind.True)
        {
            writer.WriteBooleanValue(true);
            return;
        }

        if (element.ValueKind == global::System.Text.Json.JsonValueKind.False)
        {
            writer.WriteBooleanValue(false);
            return;
        }

        writer.WriteNullValue();
    }

    internal static string CanonicalizeForm(string form, string[]? credentialValues)
    {
        string[] rawFields = form.Split('&');
        global::System.Collections.Generic.List<CanonicalNameValue> fields = new global::System.Collections.Generic.List<CanonicalNameValue>();
        for (int fieldIndex = 0; fieldIndex < rawFields.Length; fieldIndex += 1)
        {
            string rawField = rawFields[fieldIndex];
            int equalsIndex = rawField.IndexOf('=');
            string rawName = rawField;
            string rawValue = string.Empty;
            bool hasValue = false;
            if (equalsIndex >= 0)
            {
                rawName = rawField.Substring(0, equalsIndex);
                rawValue = rawField.Substring(equalsIndex + 1);
                hasValue = true;
            }

            string decodedName = DecodeFormComponent(rawName);
            if (!SecretRedactor.IsSensitiveName(decodedName))
            {
                string decodedValue = DecodeFormComponent(rawValue);
                string? safeName = SecretRedactor.RedactText(decodedName, credentialValues);
                string? safeValue = SecretRedactor.RedactText(decodedValue, credentialValues);
                CanonicalNameValue field = new CanonicalNameValue();
                field.Name = EncodeFormComponent(safeName ?? string.Empty);
                field.Value = EncodeFormComponent(safeValue ?? string.Empty);
                field.HasValue = hasValue;
                field.OriginalIndex = fieldIndex;
                fields.Add(field);
            }
        }

        SortNameValues(fields);
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        for (int index = 0; index < fields.Count; index += 1)
        {
            if (index != 0)
            {
                builder.Append('&');
            }

            CanonicalNameValue field = fields[index];
            builder.Append(field.Name);
            if (field.HasValue)
            {
                builder.Append('=');
                builder.Append(field.Value);
            }
        }

        return builder.ToString();
    }

    internal static string DecodeFormComponent(string value)
    {
        return global::System.Uri.UnescapeDataString(value.Replace("+", " ", global::System.StringComparison.Ordinal));
    }

    internal static string EncodeFormComponent(string value)
    {
        return global::System.Uri.EscapeDataString(value).Replace("%20", "+", global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeJsonNumber(string value)
    {
        int exponentIndex = value.IndexOf('e');
        if (exponentIndex < 0)
        {
            exponentIndex = value.IndexOf('E');
        }

        string mantissa = value;
        string exponent = string.Empty;
        if (exponentIndex >= 0)
        {
            mantissa = value.Substring(0, exponentIndex);
            exponent = value.Substring(exponentIndex + 1);
        }

        int decimalIndex = mantissa.IndexOf('.');
        if (decimalIndex >= 0)
        {
            int end = mantissa.Length;
            while (end > decimalIndex + 1 && mantissa[end - 1] == '0')
            {
                end -= 1;
            }

            if (end == decimalIndex + 1)
            {
                end = decimalIndex;
            }

            mantissa = mantissa.Substring(0, end);
        }

        bool negativeExponent = exponent.StartsWith("-", global::System.StringComparison.Ordinal);
        if (negativeExponent || exponent.StartsWith("+", global::System.StringComparison.Ordinal))
        {
            exponent = exponent.Substring(1);
        }

        int firstExponentDigit = 0;
        while (firstExponentDigit < exponent.Length && exponent[firstExponentDigit] == '0')
        {
            firstExponentDigit += 1;
        }

        exponent = exponent.Substring(firstExponentDigit);
        if (IsZeroJsonMantissa(mantissa))
        {
            return "0";
        }

        if (exponent.Length == 0)
        {
            return mantissa;
        }

        if (negativeExponent)
        {
            exponent = "-" + exponent;
        }

        return mantissa + "e" + exponent;
    }

    internal static bool IsZeroJsonMantissa(string value)
    {
        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (character >= '1' && character <= '9')
            {
                return false;
            }
        }

        return true;
    }

    internal static string NormalizePercentEncoding(string value)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (character == '%' && index + 2 < value.Length)
            {
                int high = HexValue(value[index + 1]);
                int low = HexValue(value[index + 2]);
                if (high >= 0 && low >= 0)
                {
                    char decoded = (char)(high * 16 + low);
                    if (IsUnreserved(decoded))
                    {
                        builder.Append(decoded);
                    }
                    else
                    {
                        builder.Append('%');
                        builder.Append(global::System.Char.ToUpperInvariant(value[index + 1]));
                        builder.Append(global::System.Char.ToUpperInvariant(value[index + 2]));
                    }

                    index += 2;
                }
                else
                {
                    builder.Append(character);
                }
            }
            else
            {
                builder.Append(character);
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

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        return -1;
    }

    internal static bool IsUnreserved(char character)
    {
        return character >= 'a' && character <= 'z'
            || character >= 'A' && character <= 'Z'
            || character >= '0' && character <= '9'
            || character == '-'
            || character == '.'
            || character == '_'
            || character == '~';
    }

    internal static void AppendSelectedHeader(global::System.Text.StringBuilder builder, global::System.Net.Http.Headers.HttpHeaders headers, string name, string[]? credentialValues)
    {
        if (!headers.Contains(name))
        {
            return;
        }

        global::System.Collections.Generic.List<string> values = new global::System.Collections.Generic.List<string>();
        foreach (string value in headers.GetValues(name))
        {
            string? safeValue = SecretRedactor.RedactText(value.Trim(), credentialValues);
            values.Add((safeValue ?? string.Empty).ToLowerInvariant());
        }

        values.Sort(global::System.StringComparer.Ordinal);
        foreach (string value in values)
        {
            AppendMaterialComponent(builder, name.ToLowerInvariant(), value);
        }
    }

    internal static void AppendMaterialComponent(global::System.Text.StringBuilder builder, string name, string value)
    {
        builder.Append(name);
        builder.Append(':');
        builder.Append(value.Length.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
        builder.Append('\n');
    }

    internal static void SortNameValues(global::System.Collections.Generic.List<CanonicalNameValue> values)
    {
        values.Sort((left, right) =>
        {
            int comparison = global::System.StringComparer.Ordinal.Compare(left.Name, right.Name);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = global::System.StringComparer.Ordinal.Compare(left.Value, right.Value);
            if (comparison != 0)
            {
                return comparison;
            }

            if (left.HasValue != right.HasValue)
            {
                return left.HasValue ? 1 : -1;
            }

            return left.OriginalIndex - right.OriginalIndex;
        });
    }

    internal static void SortJsonProperties(global::System.Collections.Generic.List<CanonicalJsonProperty> properties)
    {
        properties.Sort((left, right) =>
        {
            int comparison = global::System.StringComparer.Ordinal.Compare(left.Name, right.Name);
            if (comparison != 0)
            {
                return comparison;
            }

            return left.OriginalIndex - right.OriginalIndex;
        });
    }
}
