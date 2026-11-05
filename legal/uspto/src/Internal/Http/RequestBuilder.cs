namespace uspto;

internal static class RequestBuilder
{
    internal static global::System.Uri BuildUri(global::System.Uri baseUri, string[] pathSegments, global::System.Collections.Generic.Dictionary<string, string?> query)
    {
        if (baseUri == null)
        {
            throw new ValidationException("A base URI is required.", "baseUri", "REQUIRED", null);
        }

        if (!baseUri.IsAbsoluteUri)
        {
            throw new ValidationException("The base URI must be absolute.", "baseUri", "ABSOLUTE_URI_REQUIRED", null);
        }

        if (baseUri.Query.Length != 0 || baseUri.Fragment.Length != 0)
        {
            throw new ValidationException("The base URI cannot contain a query or fragment.", "baseUri", "BASE_URI_COMPONENT", null);
        }

        if (pathSegments == null)
        {
            throw new ValidationException("Path segments are required.", "pathSegments", "REQUIRED", null);
        }

        if (query == null)
        {
            throw new ValidationException("A query collection is required.", "query", "REQUIRED", null);
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(baseUri.GetLeftPart(global::System.UriPartial.Path));
        if (builder.Length == 0 || builder[builder.Length - 1] != '/')
        {
            builder.Append('/');
        }

        int pathIndex;
        for (pathIndex = 0; pathIndex < pathSegments.Length; pathIndex++)
        {
            string segment = pathSegments[pathIndex];
            if (global::System.String.IsNullOrEmpty(segment))
            {
                throw new ValidationException("Path segments cannot be null or empty.", "pathSegments", "EMPTY_PATH_SEGMENT", null);
            }

            string encodedSegment = global::System.Uri.EscapeDataString(segment);
            if (segment == ".")
            {
                encodedSegment = "%2E";
            }
            else if (segment == "..")
            {
                encodedSegment = "%2E%2E";
            }

            builder.Append(encodedSegment);
            if (pathIndex + 1 < pathSegments.Length)
            {
                builder.Append('/');
            }
        }

        if (query.Count != 0)
        {
            string[] keys = new string[query.Count];
            query.Keys.CopyTo(keys, 0);
            global::System.Array.Sort(keys, global::System.StringComparer.Ordinal);
            builder.Append('?');
            int queryIndex;
            for (queryIndex = 0; queryIndex < keys.Length; queryIndex++)
            {
                string key = keys[queryIndex];
                if (key.Length == 0)
                {
                    throw new ValidationException("Query names cannot be empty.", "query", "EMPTY_QUERY_NAME", null);
                }

                if (queryIndex != 0)
                {
                    builder.Append('&');
                }

                builder.Append(global::System.Uri.EscapeDataString(key));
                string? value = query[key];
                if (value != null)
                {
                    builder.Append('=');
                    builder.Append(global::System.Uri.EscapeDataString(value));
                }
            }
        }

        global::System.UriCreationOptions creationOptions = new global::System.UriCreationOptions();
        creationOptions.DangerousDisablePathAndQueryCanonicalization = true;
        return new global::System.Uri(builder.ToString(), creationOptions);
    }

    internal static global::System.Net.Http.HttpRequestMessage CreateGetRequest(global::System.Uri baseUri, string[] pathSegments, global::System.Collections.Generic.Dictionary<string, string?> query)
    {
        global::System.Uri uri = BuildUri(baseUri, pathSegments, query);
        return new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Get, uri);
    }

    internal static global::System.Net.Http.HttpRequestMessage CreateJsonPostRequest(global::System.Uri baseUri, string[] pathSegments, global::System.Collections.Generic.Dictionary<string, string?> query, string json)
    {
        if (json == null)
        {
            throw new ValidationException("A JSON request body is required.", "json", "REQUIRED", null);
        }

        global::System.Uri uri = BuildUri(baseUri, pathSegments, query);
        global::System.Net.Http.HttpRequestMessage request = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Post, uri);
        request.Content = new global::System.Net.Http.StringContent(json, global::System.Text.Encoding.UTF8, UsptoMediaTypes.Json);
        return request;
    }

    internal static global::System.Net.Http.HttpRequestMessage CreateFormPostRequest(global::System.Uri baseUri, string[] pathSegments, global::System.Collections.Generic.Dictionary<string, string?> query, global::System.Collections.Generic.Dictionary<string, string> fields)
    {
        if (fields == null)
        {
            throw new ValidationException("A form field collection is required.", "fields", "REQUIRED", null);
        }

        string[] keys = new string[fields.Count];
        fields.Keys.CopyTo(keys, 0);
        global::System.Array.Sort(keys, global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.List<global::System.Collections.Generic.KeyValuePair<string, string>> pairs = new global::System.Collections.Generic.List<global::System.Collections.Generic.KeyValuePair<string, string>>();
        int fieldIndex;
        for (fieldIndex = 0; fieldIndex < keys.Length; fieldIndex++)
        {
            string key = keys[fieldIndex];
            if (key.Length == 0)
            {
                throw new ValidationException("Form field names cannot be empty.", "fields", "EMPTY_FORM_NAME", null);
            }

            pairs.Add(new global::System.Collections.Generic.KeyValuePair<string, string>(key, fields[key]));
        }

        global::System.Uri uri = BuildUri(baseUri, pathSegments, query);
        global::System.Net.Http.HttpRequestMessage request = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.Post, uri);
        request.Content = new global::System.Net.Http.FormUrlEncodedContent(pairs);
        return request;
    }
}
