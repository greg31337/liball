namespace cookies;

internal static class CookieSequenceAssigner
{
    internal static CookieSequenceAssignment Assign(
        StoredCookie candidate,
        StoredCookie? existing,
        long nextSequenceNumber)
    {
        if (candidate == null)
        {
            throw new CookieStoreException("A sequence cannot be assigned to a null cookie.");
        }

        if (nextSequenceNumber < 0)
        {
            throw new CookieStoreException("The next cookie sequence number is invalid.");
        }

        StoredCookie stored = CookieGraphCloner.CloneStoredCookie(candidate);
        CookieSequenceAssignment result = new CookieSequenceAssignment();
        result.Cookie = stored;
        result.NextSequenceNumber = nextSequenceNumber;

        if (existing != null &&
            CookieIdentityUtility.HaveSameStoredIdentity(existing, stored))
        {
            stored.LastAccessTime = stored.CreationTime;
            stored.CreationTime = existing.CreationTime;
            stored.SequenceNumber = existing.SequenceNumber;
            result.Replaced = true;
            return result;
        }

        if (nextSequenceNumber == long.MaxValue)
        {
            throw new CookieStoreException("The cookie sequence space is exhausted.");
        }

        stored.LastAccessTime = stored.CreationTime;
        stored.SequenceNumber = nextSequenceNumber;
        result.NextSequenceNumber = nextSequenceNumber + 1;
        return result;
    }
}
