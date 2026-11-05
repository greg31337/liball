namespace cookies;

/// <summary>
/// Stores canonical cookies in memory with atomic mutation and defensive API boundaries.
/// </summary>
public class MemoryCookieStore : ICookieStore
{
    internal object SyncRoot;
    internal global::System.Collections.Generic.Dictionary<string, StoredCookie> CookiesByIdentity;
    internal global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>> IdentityKeysByDomain;
    internal global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>> IdentityKeysByPartition;
    internal long NextSequenceNumber;

    /// <summary>
    /// Creates an empty in-memory cookie store.
    /// </summary>
    public MemoryCookieStore()
    {
        SyncRoot = new object();
        CookiesByIdentity = new global::System.Collections.Generic.Dictionary<string, StoredCookie>();
        IdentityKeysByDomain = new global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>>();
        IdentityKeysByPartition = new global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>>();
    }

    /// <summary>
    /// Finds a stored cookie by its exact identity.
    /// </summary>
    /// <param name="identity">The identity to locate.</param>
    /// <returns>An independent stored cookie when found; otherwise, null.</returns>
    /// <exception cref="CookieStoreException">The identity is invalid.</exception>
    public StoredCookie? FindStoredCookie(CookieIdentity identity)
    {
        CookieStoreQuery query = new CookieStoreQuery
        {
            HasIdentity = true,
            Identity = CookieGraphCloner.CloneIdentity(identity)
        };
        if (!CookieStoreQueryEvaluator.IsValidStoreQuery(query))
        {
            throw new CookieStoreException("The cookie identity is invalid.");
        }

        string key = CookieIdentityUtility.CreateKey(query.Identity);
        lock (SyncRoot)
        {
            if (!CookiesByIdentity.ContainsKey(key))
            {
                return null;
            }

            return CookieGraphCloner.CloneStoredCookie(CookiesByIdentity[key]);
        }
    }

    /// <summary>
    /// Returns stored cookies selected by a candidate or management query.
    /// </summary>
    /// <param name="query">The query filters to apply.</param>
    /// <returns>Independent stored cookies in deterministic sequence order.</returns>
    /// <exception cref="CookieStoreException">The query is invalid.</exception>
    public StoredCookie[] QueryStoredCookies(CookieStoreQuery query)
    {
        if (!CookieStoreQueryEvaluator.IsValidStoreQuery(query))
        {
            throw new CookieStoreException("The cookie store query is invalid.");
        }

        CookieStoreQuery ownedQuery = CookieGraphCloner.CloneStoreQuery(query);
        lock (SyncRoot)
        {
            global::System.Collections.Generic.List<string> candidateKeys =
                GetCandidateKeys(ownedQuery);
            global::System.Collections.Generic.List<StoredCookie> matches =
                new global::System.Collections.Generic.List<StoredCookie>();
            int index = 0;
            while (index < candidateKeys.Count)
            {
                string key = candidateKeys[index];
                if (CookiesByIdentity.ContainsKey(key))
                {
                    StoredCookie cookie = CookiesByIdentity[key];
                    if (CookieStoreQueryEvaluator.Matches(cookie, ownedQuery))
                    {
                        matches.Add(CookieGraphCloner.CloneStoredCookie(cookie));
                    }
                }

                index = index + 1;
            }

            StoredCookie[] result = matches.ToArray();
            SortCookies(result);
            return result;
        }
    }

    /// <summary>
    /// Atomically applies a canonical cookie upsert or immediate-expiry deletion.
    /// </summary>
    /// <param name="cookie">The canonical cookie state to apply.</param>
    /// <returns>The mutation outcome with an independent stored model.</returns>
    /// <exception cref="CookieStoreException">The cookie or sequence state is invalid.</exception>
    public CookieSetResult ApplyStoredCookie(StoredCookie cookie)
    {
        if (!CookieStoreValidator.IsValidStoredCookie(cookie))
        {
            throw new CookieStoreException("The stored cookie model is invalid.");
        }

        StoredCookie candidate = CookieGraphCloner.CloneStoredCookie(cookie);
        if (!CookieStoreValidator.IsValidStoredCookie(candidate))
        {
            throw new CookieStoreException("The stored cookie model changed during copying.");
        }

        string key = CookieIdentityUtility.CreateStoredCookieKey(candidate);
        lock (SyncRoot)
        {
            if (CookieStoreValidator.IsImmediateDeletion(candidate))
            {
                if (!CookiesByIdentity.ContainsKey(key))
                {
                    return CreateMutationResult(
                        CookieSetStatus.Ignored,
                        CookieErrorCode.ExpiredDeletion,
                        null);
                }

                StoredCookie removed = CookiesByIdentity[key];
                RemoveInternal(key, removed);
                return CreateMutationResult(
                    CookieSetStatus.Deleted,
                    CookieErrorCode.ExpiredDeletion,
                    null);
            }

            StoredCookie? existing = null;
            if (CookiesByIdentity.ContainsKey(key))
            {
                existing = CookiesByIdentity[key];
            }

            CookieSequenceAssignment assignment = CookieSequenceAssigner.Assign(
                candidate,
                existing,
                NextSequenceNumber);
            CookiesByIdentity[key] = assignment.Cookie;
            if (!assignment.Replaced)
            {
                AddToIndexes(key, assignment.Cookie);
            }

            NextSequenceNumber = assignment.NextSequenceNumber;
            return CreateMutationResult(
                assignment.Replaced ? CookieSetStatus.Replaced : CookieSetStatus.Accepted,
                CookieErrorCode.None,
                assignment.Cookie);
        }
    }

    /// <summary>
    /// Atomically updates the last-access time of existing cookie identities.
    /// </summary>
    /// <param name="identities">The cookie identities to update in input order.</param>
    /// <param name="accessTime">The last-access instant to apply.</param>
    /// <returns>Independent updated cookies in input order, excluding absent identities.</returns>
    /// <exception cref="CookieStoreException">An identity is invalid or duplicated.</exception>
    public StoredCookie[] UpdateStoredCookieAccess(
        CookieIdentity[] identities,
        global::System.DateTimeOffset accessTime)
    {
        if (identities == null)
        {
            throw new CookieStoreException("The cookie identities are invalid.");
        }

        CookieIdentity[] ownedIdentities = new CookieIdentity[identities.Length];
        global::System.Collections.Generic.Dictionary<string, bool> keys =
            new global::System.Collections.Generic.Dictionary<string, bool>();
        int validationIndex = 0;
        while (validationIndex < identities.Length)
        {
            CookieStoreQuery query = new CookieStoreQuery();
            query.HasIdentity = true;
            query.Identity = CookieGraphCloner.CloneIdentity(
                identities[validationIndex]);
            if (!CookieStoreQueryEvaluator.IsValidStoreQuery(query))
            {
                throw new CookieStoreException("A cookie identity is invalid.");
            }

            string key = CookieIdentityUtility.CreateKey(query.Identity);
            if (keys.ContainsKey(key))
            {
                throw new CookieStoreException("A cookie identity is duplicated.");
            }

            keys.Add(key, true);
            ownedIdentities[validationIndex] = query.Identity;
            validationIndex = validationIndex + 1;
        }

        lock (SyncRoot)
        {
            global::System.Collections.Generic.List<StoredCookie> updated =
                new global::System.Collections.Generic.List<StoredCookie>();
            int index = 0;
            while (index < ownedIdentities.Length)
            {
                string key = CookieIdentityUtility.CreateKey(ownedIdentities[index]);
                if (CookiesByIdentity.ContainsKey(key))
                {
                    StoredCookie cookie = CookiesByIdentity[key];
                    cookie.LastAccessTime = accessTime;
                    updated.Add(CookieGraphCloner.CloneStoredCookie(cookie));
                }

                index = index + 1;
            }

            return updated.ToArray();
        }
    }

    /// <summary>
    /// Removes stored cookies selected by removal filters.
    /// </summary>
    /// <param name="query">The removal filters to apply.</param>
    /// <returns>The number of stored cookies removed atomically.</returns>
    /// <exception cref="CookieStoreException">The removal query is invalid.</exception>
    public int RemoveStoredCookies(CookieRemovalQuery query)
    {
        if (!CookieStoreQueryEvaluator.IsValidRemovalQuery(query))
        {
            throw new CookieStoreException("The cookie removal query is invalid.");
        }

        CookieRemovalQuery ownedRemoval = CookieGraphCloner.CloneRemovalQuery(query);
        CookieStoreQuery storeQuery =
            CookieStoreQueryEvaluator.CreateStoreQuery(ownedRemoval);
        lock (SyncRoot)
        {
            global::System.Collections.Generic.List<string> candidates =
                GetCandidateKeys(storeQuery);
            global::System.Collections.Generic.List<string> removals =
                new global::System.Collections.Generic.List<string>();
            int index = 0;
            while (index < candidates.Count)
            {
                string key = candidates[index];
                if (CookiesByIdentity.ContainsKey(key) &&
                    CookieStoreQueryEvaluator.Matches(
                        CookiesByIdentity[key],
                        storeQuery))
                {
                    removals.Add(key);
                }

                index = index + 1;
            }

            int removalIndex = 0;
            while (removalIndex < removals.Count)
            {
                string key = removals[removalIndex];
                StoredCookie removed = CookiesByIdentity[key];
                RemoveInternal(key, removed);
                removalIndex = removalIndex + 1;
            }

            return removals.Count;
        }
    }

    /// <summary>
    /// Captures a complete independent snapshot of stored state.
    /// </summary>
    /// <returns>The deterministic store snapshot.</returns>
    public CookieJarSnapshot CaptureStoreSnapshot()
    {
        lock (SyncRoot)
        {
            global::System.Collections.Generic.List<StoredCookie> cookies =
                new global::System.Collections.Generic.List<StoredCookie>();
            foreach (global::System.Collections.Generic.KeyValuePair<string, StoredCookie> entry
                in CookiesByIdentity)
            {
                cookies.Add(CookieGraphCloner.CloneStoredCookie(entry.Value));
            }

            StoredCookie[] ordered = cookies.ToArray();
            SortCookies(ordered);
            return new CookieJarSnapshot
            {
                Version = CookieDefaults.SnapshotVersion,
                Cookies = ordered,
                NextSequenceNumber = NextSequenceNumber
            };
        }
    }

    /// <summary>
    /// Atomically replaces all stored state with a validated independent snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot state to install.</param>
    /// <exception cref="CookieStoreException">The snapshot violates store invariants.</exception>
    public void ReplaceStoreSnapshot(CookieJarSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Cookies == null)
        {
            throw new CookieStoreException("The store snapshot is invalid.");
        }

        if (snapshot.Version != CookieDefaults.SnapshotVersion ||
            snapshot.NextSequenceNumber < 0)
        {
            throw new CookieStoreException("The store snapshot metadata is invalid.");
        }

        int sourceIndex = 0;
        while (sourceIndex < snapshot.Cookies.Length)
        {
            StoredCookie sourceCookie = snapshot.Cookies[sourceIndex];
            if (!CookieStoreValidator.IsValidStoredCookie(sourceCookie) ||
                sourceCookie.SequenceNumber < 0)
            {
                throw new CookieStoreException("A snapshot cookie is invalid.");
            }

            sourceIndex = sourceIndex + 1;
        }

        CookieJarSnapshot ownedSnapshot = CookieGraphCloner.CloneJarSnapshot(snapshot);

        global::System.Collections.Generic.Dictionary<string, StoredCookie> replacementCookies =
            new global::System.Collections.Generic.Dictionary<string, StoredCookie>();
        global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>> replacementDomains =
            new global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>>();
        global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>> replacementPartitions =
            new global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>>();
        global::System.Collections.Generic.Dictionary<long, bool> sequenceNumbers =
            new global::System.Collections.Generic.Dictionary<long, bool>();

        int index = 0;
        while (index < ownedSnapshot.Cookies.Length)
        {
            StoredCookie cookie = ownedSnapshot.Cookies[index];
            if (!CookieStoreValidator.IsValidStoredCookie(cookie) ||
                cookie.SequenceNumber >= ownedSnapshot.NextSequenceNumber)
            {
                throw new CookieStoreException("A snapshot cookie is invalid.");
            }

            string key = CookieIdentityUtility.CreateStoredCookieKey(cookie);
            if (replacementCookies.ContainsKey(key) ||
                sequenceNumbers.ContainsKey(cookie.SequenceNumber))
            {
                throw new CookieStoreException("The store snapshot contains duplicate state.");
            }

            replacementCookies.Add(key, cookie);
            sequenceNumbers.Add(cookie.SequenceNumber, true);
            AddToReplacementIndex(replacementDomains, cookie.Domain, key);
            string partitionKey = CookieIdentityUtility.CreatePartitionIndexKey(
                cookie.HasPartitionKey,
                cookie.PartitionKey);
            AddToReplacementIndex(replacementPartitions, partitionKey, key);
            index = index + 1;
        }

        lock (SyncRoot)
        {
            CookiesByIdentity = replacementCookies;
            IdentityKeysByDomain = replacementDomains;
            IdentityKeysByPartition = replacementPartitions;
            NextSequenceNumber = ownedSnapshot.NextSequenceNumber;
        }
    }

    internal global::System.Collections.Generic.List<string> GetCandidateKeys(
        CookieStoreQuery query)
    {
        global::System.Collections.Generic.List<string> candidates =
            new global::System.Collections.Generic.List<string>();
        if (query.HasIdentity)
        {
            string identityKey = CookieIdentityUtility.CreateKey(query.Identity);
            if (CookiesByIdentity.ContainsKey(identityKey))
            {
                candidates.Add(identityKey);
            }

            return candidates;
        }

        if (query.HasDomain && !query.IncludeSubdomains)
        {
            if (IdentityKeysByDomain.ContainsKey(query.Domain))
            {
                CopyKeys(IdentityKeysByDomain[query.Domain], candidates);
            }

            return candidates;
        }

        if (query.FilterByPartition)
        {
            string partitionKey = CookieIdentityUtility.CreatePartitionIndexKey(
                query.HasPartitionKey,
                query.PartitionKey);
            if (IdentityKeysByPartition.ContainsKey(partitionKey))
            {
                CopyKeys(IdentityKeysByPartition[partitionKey], candidates);
            }

            return candidates;
        }

        if (query.HasDomain)
        {
            foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Collections.Generic.List<string>> entry
                in IdentityKeysByDomain)
            {
                if (CookieStoreQueryEvaluator.DomainMatchesQuery(
                    entry.Key,
                    query.Domain,
                    true))
                {
                    CopyKeys(entry.Value, candidates);
                }
            }

            return candidates;
        }

        foreach (global::System.Collections.Generic.KeyValuePair<string, StoredCookie> entry
            in CookiesByIdentity)
        {
            candidates.Add(entry.Key);
        }

        return candidates;
    }

    internal void AddToIndexes(string key, StoredCookie cookie)
    {
        AddToReplacementIndex(IdentityKeysByDomain, cookie.Domain, key);
        string partitionKey = CookieIdentityUtility.CreatePartitionIndexKey(
            cookie.HasPartitionKey,
            cookie.PartitionKey);
        AddToReplacementIndex(IdentityKeysByPartition, partitionKey, key);
    }

    internal void RemoveInternal(string key, StoredCookie cookie)
    {
        CookiesByIdentity.Remove(key);
        RemoveFromIndex(IdentityKeysByDomain, cookie.Domain, key);
        string partitionKey = CookieIdentityUtility.CreatePartitionIndexKey(
            cookie.HasPartitionKey,
            cookie.PartitionKey);
        RemoveFromIndex(IdentityKeysByPartition, partitionKey, key);
    }

    internal static void AddToReplacementIndex(
        global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>> index,
        string indexKey,
        string identityKey)
    {
        if (!index.ContainsKey(indexKey))
        {
            index.Add(indexKey, new global::System.Collections.Generic.List<string>());
        }

        index[indexKey].Add(identityKey);
    }

    internal static void RemoveFromIndex(
        global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<string>> index,
        string indexKey,
        string identityKey)
    {
        if (!index.ContainsKey(indexKey))
        {
            return;
        }

        global::System.Collections.Generic.List<string> keys = index[indexKey];
        keys.Remove(identityKey);
        if (keys.Count == 0)
        {
            index.Remove(indexKey);
        }
    }

    internal static void CopyKeys(
        global::System.Collections.Generic.List<string> source,
        global::System.Collections.Generic.List<string> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void SortCookies(StoredCookie[] cookies)
    {
        int index = 1;
        while (index < cookies.Length)
        {
            StoredCookie value = cookies[index];
            int insertion = index;
            while (insertion > 0 && CompareCookies(cookies[insertion - 1], value) > 0)
            {
                cookies[insertion] = cookies[insertion - 1];
                insertion = insertion - 1;
            }

            cookies[insertion] = value;
            index = index + 1;
        }
    }

    internal static int CompareCookies(StoredCookie left, StoredCookie right)
    {
        if (left.SequenceNumber < right.SequenceNumber)
        {
            return -1;
        }

        if (left.SequenceNumber > right.SequenceNumber)
        {
            return 1;
        }

        return global::System.String.Compare(
            CookieIdentityUtility.CreateStoredCookieKey(left),
            CookieIdentityUtility.CreateStoredCookieKey(right),
            global::System.StringComparison.Ordinal);
    }

    internal static CookieSetResult CreateMutationResult(
        CookieSetStatus status,
        CookieErrorCode errorCode,
        StoredCookie? cookie)
    {
        CookieSetResult result = new CookieSetResult();
        result.Status = status;
        result.ErrorCode = errorCode;
        if (cookie != null)
        {
            result.Cookie = CookieGraphCloner.CloneStoredCookie(cookie);
        }

        return result;
    }
}
