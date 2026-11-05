namespace cookies;

internal static class CookieSnapshotReader
{
    internal const int MaximumSnapshotCharacters =
        CookieDefaults.MaximumInputLineOctets * CookieDefaults.MaximumCookies;

    internal static CookieSnapshotParseResult ReadSnapshot(string text)
    {
        if (text == null || text.Length > MaximumSnapshotCharacters)
        {
            return CreateSnapshotReadFailure(CookieErrorCode.InvalidSnapshot);
        }

        global::System.Threading.Tasks.Task<global::System.Text.Json.JsonDocument> documentTask =
            new global::System.Threading.Tasks.Task<global::System.Text.Json.JsonDocument>(
                () => global::System.Text.Json.JsonDocument.Parse(text));
        documentTask.RunSynchronously(
            global::System.Threading.Tasks.TaskScheduler.Default);
        if (!documentTask.IsCompletedSuccessfully)
        {
            documentTask.Exception?.Handle(exception => true);
            return CreateSnapshotReadFailure(CookieErrorCode.InvalidSnapshot);
        }

        global::System.Text.Json.JsonDocument document = documentTask.Result;
        using (document)
        {
            CookieErrorCode schemaError = ValidateSnapshotSchema(
                document.RootElement);
            if (schemaError != CookieErrorCode.None)
            {
                return CreateSnapshotReadFailure(schemaError);
            }
        }

        global::System.Text.Json.JsonSerializerOptions options =
            new global::System.Text.Json.JsonSerializerOptions();
        options.IncludeFields = true;
        options.PropertyNameCaseInsensitive = true;
        global::System.Threading.Tasks.Task<CookieJarSnapshot> snapshotTask =
            new global::System.Threading.Tasks.Task<CookieJarSnapshot>(
                () => global::System.Text.Json.JsonSerializer.Deserialize<CookieJarSnapshot>(
                    text,
                    options) ?? new CookieJarSnapshot());
        snapshotTask.RunSynchronously(
            global::System.Threading.Tasks.TaskScheduler.Default);
        if (!snapshotTask.IsCompletedSuccessfully)
        {
            snapshotTask.Exception?.Handle(exception => true);
            return CreateSnapshotReadFailure(CookieErrorCode.InvalidSnapshot);
        }

        CookieJarSnapshot snapshot = snapshotTask.Result;
        CookieErrorCode validation =
            CookieSnapshotValidator.ValidateSnapshotForWriting(snapshot);
        if (validation != CookieErrorCode.None)
        {
            return CreateSnapshotReadFailure(validation);
        }

        CookieSnapshotParseResult result = new CookieSnapshotParseResult();
        result.Success = true;
        result.Snapshot = CookieGraphCloner.CloneJarSnapshot(snapshot);
        result.ErrorCode = CookieErrorCode.None;
        return result;
    }

    internal static CookieErrorCode ValidateSnapshotSchema(
        global::System.Text.Json.JsonElement root)
    {
        if (root.ValueKind != global::System.Text.Json.JsonValueKind.Object ||
            !HasOnlyRootProperties(root))
        {
            return CookieErrorCode.InvalidSnapshot;
        }

        if (!ValidateLongProperty(root, "version") ||
            !ValidateLongProperty(root, "nextSequenceNumber"))
        {
            return CookieErrorCode.InvalidSnapshot;
        }

        CookieJsonPropertyResult cookies = FindSnapshotProperty(root, "cookies");
        if (!cookies.Found || cookies.Duplicate ||
            cookies.Value.ValueKind != global::System.Text.Json.JsonValueKind.Array ||
            cookies.Value.GetArrayLength() > CookieDefaults.MaximumCookies)
        {
            return CookieErrorCode.InvalidSnapshot;
        }

        int index = 0;
        while (index < cookies.Value.GetArrayLength())
        {
            global::System.Text.Json.JsonElement cookie = cookies.Value[index];
            if (global::System.Text.Encoding.UTF8.GetByteCount(cookie.GetRawText()) >
                    CookieDefaults.MaximumInputLineOctets ||
                !ValidateCookieSchema(cookie))
            {
                return CookieErrorCode.InvalidSnapshot;
            }

            index = index + 1;
        }

        return CookieErrorCode.None;
    }

    internal static bool ValidateCookieSchema(
        global::System.Text.Json.JsonElement cookie)
    {
        if (cookie.ValueKind != global::System.Text.Json.JsonValueKind.Object ||
            !HasOnlyCookieProperties(cookie) ||
            !ValidateStringProperty(cookie, "name") ||
            !ValidateStringProperty(cookie, "value") ||
            !ValidateStringProperty(cookie, "domain") ||
            !ValidateStringProperty(cookie, "path") ||
            !ValidateBooleanProperty(cookie, "hasExpires") ||
            !ValidateTimeProperty(cookie, "expires") ||
            !ValidateTimeProperty(cookie, "creationTime") ||
            !ValidateTimeProperty(cookie, "lastAccessTime") ||
            !ValidateBooleanProperty(cookie, "persistent") ||
            !ValidateBooleanProperty(cookie, "hostOnly") ||
            !ValidateBooleanProperty(cookie, "secure") ||
            !ValidateBooleanProperty(cookie, "httpOnly") ||
            !ValidateBooleanProperty(cookie, "hasSameSite") ||
            !ValidateLongProperty(cookie, "sameSiteMode") ||
            !ValidateBooleanProperty(cookie, "hasPartitionKey") ||
            !ValidateStringProperty(cookie, "partitionKey") ||
            !ValidateBooleanProperty(cookie, "isValueQuoted") ||
            !ValidateLongProperty(cookie, "sequenceNumber"))
        {
            return false;
        }

        CookieJsonPropertyResult extensions =
            FindSnapshotProperty(cookie, "extensions");
        if (!extensions.Found || extensions.Duplicate ||
            extensions.Value.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            return false;
        }

        int index = 0;
        while (index < extensions.Value.GetArrayLength())
        {
            if (!ValidateExtensionSchema(extensions.Value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ValidateExtensionSchema(
        global::System.Text.Json.JsonElement extension)
    {
        return extension.ValueKind == global::System.Text.Json.JsonValueKind.Object &&
            HasOnlyExtensionProperties(extension) &&
            ValidateStringProperty(extension, "name") &&
            ValidateBooleanProperty(extension, "hasValue") &&
            ValidateStringProperty(extension, "value");
    }

    internal static CookieJsonPropertyResult FindSnapshotProperty(
        global::System.Text.Json.JsonElement value,
        string name)
    {
        CookieJsonPropertyResult result = new CookieJsonPropertyResult();
        foreach (global::System.Text.Json.JsonProperty property
            in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                if (result.Found)
                {
                    result.Duplicate = true;
                }
                else
                {
                    result.Found = true;
                    result.Value = property.Value;
                }
            }
        }

        return result;
    }

    internal static bool ValidateStringProperty(
        global::System.Text.Json.JsonElement value,
        string name)
    {
        CookieJsonPropertyResult property = FindSnapshotProperty(value, name);
        return property.Found && !property.Duplicate &&
            property.Value.ValueKind == global::System.Text.Json.JsonValueKind.String;
    }

    internal static bool ValidateBooleanProperty(
        global::System.Text.Json.JsonElement value,
        string name)
    {
        CookieJsonPropertyResult property = FindSnapshotProperty(value, name);
        return property.Found && !property.Duplicate &&
            (property.Value.ValueKind == global::System.Text.Json.JsonValueKind.True ||
                property.Value.ValueKind == global::System.Text.Json.JsonValueKind.False);
    }

    internal static bool ValidateLongProperty(
        global::System.Text.Json.JsonElement value,
        string name)
    {
        CookieJsonPropertyResult property = FindSnapshotProperty(value, name);
        if (!property.Found || property.Duplicate ||
            property.Value.ValueKind != global::System.Text.Json.JsonValueKind.Number)
        {
            return false;
        }

        DecimalSecondsParseResult parsed = DecimalSecondsParser.ParseMaxAgeSeconds(
            property.Value.GetRawText());
        return parsed.Success && !parsed.Saturated;
    }

    internal static bool ValidateTimeProperty(
        global::System.Text.Json.JsonElement value,
        string name)
    {
        CookieJsonPropertyResult property = FindSnapshotProperty(value, name);
        if (!property.Found || property.Duplicate ||
            property.Value.ValueKind != global::System.Text.Json.JsonValueKind.String)
        {
            return false;
        }

        string text = property.Value.GetString() ?? "";
        global::System.Threading.Tasks.Task<global::System.DateTimeOffset> parseTask =
            new global::System.Threading.Tasks.Task<global::System.DateTimeOffset>(
                () => global::System.DateTimeOffset.ParseExact(
                    text,
                    "O",
                    global::System.Globalization.CultureInfo.InvariantCulture,
                    global::System.Globalization.DateTimeStyles.RoundtripKind));
        parseTask.RunSynchronously(
            global::System.Threading.Tasks.TaskScheduler.Default);
        if (!parseTask.IsCompletedSuccessfully)
        {
            parseTask.Exception?.Handle(exception => true);
            return false;
        }

        return true;
    }

    internal static bool HasOnlyRootProperties(
        global::System.Text.Json.JsonElement value)
    {
        foreach (global::System.Text.Json.JsonProperty property
            in value.EnumerateObject())
        {
            if (property.Name != "version" &&
                property.Name != "nextSequenceNumber" &&
                property.Name != "cookies")
            {
                return false;
            }
        }

        return true;
    }

    internal static bool HasOnlyCookieProperties(
        global::System.Text.Json.JsonElement value)
    {
        foreach (global::System.Text.Json.JsonProperty property
            in value.EnumerateObject())
        {
            string name = property.Name;
            if (name != "name" && name != "value" && name != "domain" &&
                name != "path" && name != "hasExpires" && name != "expires" &&
                name != "creationTime" && name != "lastAccessTime" &&
                name != "persistent" && name != "hostOnly" && name != "secure" &&
                name != "httpOnly" && name != "hasSameSite" &&
                name != "sameSiteMode" && name != "hasPartitionKey" &&
                name != "partitionKey" && name != "isValueQuoted" &&
                name != "sequenceNumber" && name != "extensions")
            {
                return false;
            }
        }

        return true;
    }

    internal static bool HasOnlyExtensionProperties(
        global::System.Text.Json.JsonElement value)
    {
        foreach (global::System.Text.Json.JsonProperty property
            in value.EnumerateObject())
        {
            if (property.Name != "name" && property.Name != "hasValue" &&
                property.Name != "value")
            {
                return false;
            }
        }

        return true;
    }

    internal static CookieSnapshotParseResult CreateSnapshotReadFailure(
        CookieErrorCode errorCode)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = CookieParseSeverity.Error;
        issue.SafeFragment = "";
        CookieSnapshotParseResult result = new CookieSnapshotParseResult();
        result.ErrorCode = errorCode;
        result.Issues = [issue];
        return result;
    }
}
