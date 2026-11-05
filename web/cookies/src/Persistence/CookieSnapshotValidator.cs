namespace cookies;

internal static class CookieSnapshotValidator
{
    internal static CookieErrorCode ValidateSnapshotForWriting(
        CookieJarSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Cookies == null ||
            snapshot.NextSequenceNumber < 0 ||
            snapshot.Cookies.Length > CookieDefaults.MaximumCookies)
        {
            return CookieErrorCode.InvalidSnapshot;
        }

        if (snapshot.Version != CookieDefaults.SnapshotVersion)
        {
            return CookieErrorCode.UnsupportedSnapshotVersion;
        }

        global::System.Collections.Generic.Dictionary<string, bool> identities =
            new global::System.Collections.Generic.Dictionary<string, bool>();
        global::System.Collections.Generic.Dictionary<long, bool> sequences =
            new global::System.Collections.Generic.Dictionary<long, bool>();
        int index = 0;
        while (index < snapshot.Cookies.Length)
        {
            StoredCookie cookie = snapshot.Cookies[index];
            CookieErrorCode cookieError = ValidateSnapshotCookie(
                cookie,
                snapshot.NextSequenceNumber);
            if (cookieError != CookieErrorCode.None)
            {
                return cookieError;
            }

            string identity = CookieIdentityUtility.CreateStoredCookieKey(cookie);
            if (identities.ContainsKey(identity))
            {
                return CookieErrorCode.DuplicateIdentity;
            }

            if (sequences.ContainsKey(cookie.SequenceNumber))
            {
                return CookieErrorCode.InvalidSnapshot;
            }

            identities.Add(identity, true);
            sequences.Add(cookie.SequenceNumber, true);
            index = index + 1;
        }

        return CookieErrorCode.None;
    }

    internal static CookieErrorCode ValidateSnapshotCookie(
        StoredCookie cookie,
        long nextSequenceNumber)
    {
        if (!CookieStoreValidator.IsValidStoredCookie(cookie) ||
            cookie.SequenceNumber < 0 ||
            cookie.SequenceNumber >= nextSequenceNumber ||
            ContainsSnapshotInjection(cookie.Name) ||
            ContainsSnapshotInjection(cookie.Value) ||
            ContainsSnapshotInjection(cookie.Domain) ||
            ContainsSnapshotInjection(cookie.Path) ||
            ContainsSnapshotInjection(cookie.PartitionKey) ||
            (!cookie.HasPartitionKey && cookie.PartitionKey.Length != 0) ||
            cookie.SameSiteMode < CookieSameSiteMode.Default ||
            cookie.SameSiteMode > CookieSameSiteMode.None)
        {
            return CookieErrorCode.InvalidSnapshot;
        }

        if ((long)global::System.Text.Encoding.UTF8.GetByteCount(cookie.Name) +
                global::System.Text.Encoding.UTF8.GetByteCount(cookie.Value) >
                CookieDefaults.MaximumNameValueOctets)
        {
            return CookieErrorCode.InvalidSnapshot;
        }

        int extensionIndex = 0;
        while (extensionIndex < cookie.Extensions.Length)
        {
            CookieAttribute extension = cookie.Extensions[extensionIndex];
            if (extension.Name.Length == 0 ||
                ContainsSnapshotInjection(extension.Name) ||
                ContainsSnapshotInjection(extension.Value) ||
                global::System.Text.Encoding.UTF8.GetByteCount(extension.Value) >
                    CookieDefaults.MaximumAttributeValueOctets)
            {
                return CookieErrorCode.InvalidSnapshot;
            }

            extensionIndex = extensionIndex + 1;
        }

        return CookieErrorCode.None;
    }

    internal static bool ContainsSnapshotInjection(string value)
    {
        if (value == null)
        {
            return true;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (CookieCharacterRules.IsControl(value[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
