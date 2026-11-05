namespace cookies;

internal static class CookieIdentityUtility
{
    internal static CookieIdentity CreateFromStoredCookie(StoredCookie cookie)
    {
        if (cookie == null)
        {
            return new CookieIdentity();
        }

        CookieIdentity identity = new CookieIdentity
        {
            Name = cookie.Name,
            Domain = cookie.Domain,
            Path = cookie.Path,
            HasPartitionKey = cookie.HasPartitionKey
        };
        if (cookie.HasPartitionKey)
        {
            identity.PartitionKey = cookie.PartitionKey;
        }

        return identity;
    }

    internal static bool AreEqual(CookieIdentity left, CookieIdentity right)
    {
        if (left == null || right == null ||
            left.Name != right.Name ||
            left.Domain != right.Domain ||
            left.Path != right.Path ||
            left.HasPartitionKey != right.HasPartitionKey)
        {
            return false;
        }

        if (!left.HasPartitionKey)
        {
            return true;
        }

        return left.PartitionKey == right.PartitionKey;
    }

    internal static bool MatchesStoredCookie(
        StoredCookie cookie,
        CookieIdentity identity)
    {
        if (cookie == null || identity == null)
        {
            return false;
        }

        return AreEqual(CreateFromStoredCookie(cookie), identity);
    }

    internal static bool HaveSameStoredIdentity(
        StoredCookie left,
        StoredCookie right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return MatchesStoredCookie(left, CreateFromStoredCookie(right));
    }

    internal static string CreateKey(CookieIdentity identity)
    {
        if (identity == null)
        {
            return "";
        }

        string partition = "0";
        if (identity.HasPartitionKey)
        {
            partition = $"1{EncodeComponent(identity.PartitionKey)}";
        }

        return $"{EncodeComponent(identity.Name)}" +
            $"{EncodeComponent(identity.Domain)}" +
            $"{EncodeComponent(identity.Path)}" +
            partition;
    }

    internal static string CreateStoredCookieKey(StoredCookie cookie)
    {
        return CreateKey(CreateFromStoredCookie(cookie));
    }

    internal static string CreatePartitionIndexKey(
        bool hasPartitionKey,
        string partitionKey)
    {
        if (!hasPartitionKey)
        {
            return "0";
        }

        return $"1{EncodeComponent(partitionKey)}";
    }

    static string EncodeComponent(string value)
    {
        if (value == null)
        {
            return "-1:";
        }

        return $"{value.Length}:{value}";
    }
}
