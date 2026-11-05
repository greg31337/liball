namespace cookies;

internal static class CookieSnapshotWriter
{
    internal static string WriteSnapshot(CookieJarSnapshot snapshot)
    {
        CookieErrorCode validation =
            CookieSnapshotValidator.ValidateSnapshotForWriting(snapshot);
        if (validation != CookieErrorCode.None)
        {
            throw new CookieSnapshotException(CreateSnapshotFailureMessage(validation));
        }

        StoredCookie[] cookies = CookieGraphCloner.CloneStoredCookies(
            snapshot.Cookies);
        SortSnapshotCookies(cookies);
        global::System.IO.MemoryStream stream =
            new global::System.IO.MemoryStream();
        global::System.Text.Json.Utf8JsonWriter writer =
            new global::System.Text.Json.Utf8JsonWriter(stream);
        using (stream)
        using (writer)
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", snapshot.Version);
            writer.WriteNumber("nextSequenceNumber", snapshot.NextSequenceNumber);
            writer.WriteStartArray("cookies");
            int cookieIndex = 0;
            while (cookieIndex < cookies.Length)
            {
                WriteSnapshotCookie(writer, cookies[cookieIndex]);
                cookieIndex = cookieIndex + 1;
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            return global::System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal static void WriteSnapshotCookie(
        global::System.Text.Json.Utf8JsonWriter writer,
        StoredCookie cookie)
    {
        writer.WriteStartObject();
        writer.WriteString("name", cookie.Name);
        writer.WriteString("value", cookie.Value);
        writer.WriteString("domain", cookie.Domain);
        writer.WriteString("path", cookie.Path);
        writer.WriteBoolean("hasExpires", cookie.HasExpires);
        writer.WriteString("expires", FormatSnapshotTime(cookie.Expires));
        writer.WriteString("creationTime", FormatSnapshotTime(cookie.CreationTime));
        writer.WriteString("lastAccessTime", FormatSnapshotTime(cookie.LastAccessTime));
        writer.WriteBoolean("persistent", cookie.Persistent);
        writer.WriteBoolean("hostOnly", cookie.HostOnly);
        writer.WriteBoolean("secure", cookie.Secure);
        writer.WriteBoolean("httpOnly", cookie.HttpOnly);
        writer.WriteBoolean("hasSameSite", cookie.HasSameSite);
        writer.WriteNumber("sameSiteMode", (int)cookie.SameSiteMode);
        writer.WriteBoolean("hasPartitionKey", cookie.HasPartitionKey);
        writer.WriteString("partitionKey", cookie.PartitionKey);
        writer.WriteBoolean("isValueQuoted", cookie.IsValueQuoted);
        writer.WriteNumber("sequenceNumber", cookie.SequenceNumber);
        writer.WriteStartArray("extensions");
        int extensionIndex = 0;
        while (extensionIndex < cookie.Extensions.Length)
        {
            WriteSnapshotExtension(writer, cookie.Extensions[extensionIndex]);
            extensionIndex = extensionIndex + 1;
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal static void WriteSnapshotExtension(
        global::System.Text.Json.Utf8JsonWriter writer,
        CookieAttribute extension)
    {
        writer.WriteStartObject();
        writer.WriteString("name", extension.Name);
        writer.WriteBoolean("hasValue", extension.HasValue);
        writer.WriteString("value", extension.Value);
        writer.WriteEndObject();
    }

    internal static string FormatSnapshotTime(
        global::System.DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString(
            "O",
            global::System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void SortSnapshotCookies(StoredCookie[] cookies)
    {
        int index = 1;
        while (index < cookies.Length)
        {
            StoredCookie value = cookies[index];
            int insertion = index;
            while (insertion > 0 &&
                CompareSnapshotCookies(cookies[insertion - 1], value) > 0)
            {
                cookies[insertion] = cookies[insertion - 1];
                insertion = insertion - 1;
            }

            cookies[insertion] = value;
            index = index + 1;
        }
    }

    internal static int CompareSnapshotCookies(
        StoredCookie left,
        StoredCookie right)
    {
        if (left.SequenceNumber < right.SequenceNumber)
        {
            return -1;
        }

        if (left.SequenceNumber > right.SequenceNumber)
        {
            return 1;
        }

        return global::System.String.CompareOrdinal(
            CookieIdentityUtility.CreateStoredCookieKey(left),
            CookieIdentityUtility.CreateStoredCookieKey(right));
    }

    internal static string CreateSnapshotFailureMessage(
        CookieErrorCode errorCode)
    {
        return $"Cookie snapshot failed with error code {(int)errorCode}.";
    }
}
