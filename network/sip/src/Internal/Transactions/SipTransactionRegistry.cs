namespace sip;

internal class SipTransactionRegistry
{
    System.Collections.Generic.List<SipTransactionRegistryEntry> entries;
    object synchronization;
    int maximumTransactionCount;
    bool allowLegacyMatching;

    internal SipTransactionRegistry(SipTransactionOptions? options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.MaxTransactionCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        entries = new System.Collections.Generic.List<SipTransactionRegistryEntry>();
        synchronization = new object();
        maximumTransactionCount = options.MaxTransactionCount;
        allowLegacyMatching = options.AllowLegacyMatching;
    }

    internal SipTransactionAdmissionResult AdmitTransaction(string? transactionId,
        SipMessage? request, SipTransactionRole role)
    {
        string? validationError = ValidateAdmission(transactionId, request, role);
        if (validationError != null || transactionId == null || request == null)
        {
            return CreateRejectedResult(false, true, false, SipStatusCodes.BadRequest,
                validationError == null ? "The transaction input is malformed." : validationError);
        }

        SipTransactionKey key = SipTransactionMatching.CreateKey(request, role);
        lock (synchronization)
        {
            int index = 0;
            while (index < entries.Count)
            {
                SipTransactionRegistryEntry entry = entries[index];
                if (entry.Id == transactionId)
                {
                    if (EntryMatches(entry, request, key, role))
                    {
                        SipTransactionAdmissionResult existing =
                            new SipTransactionAdmissionResult();
                        existing.Existing = true;
                        existing.TransactionId = entry.Id;
                        return existing;
                    }
                    return CreateRejectedResult(false, true, false,
                        SipStatusCodes.BadRequest,
                        "The transaction identifier is already assigned to another request.");
                }
                index = index + 1;
            }

            index = 0;
            while (index < entries.Count)
            {
                SipTransactionRegistryEntry entry = entries[index];
                if (EntryMatches(entry, request, key, role))
                {
                    SipTransactionAdmissionResult existing = new SipTransactionAdmissionResult();
                    existing.Existing = true;
                    existing.TransactionId = entry.Id;
                    return existing;
                }
                if (role == SipTransactionRole.Server
                    && entry.Role == SipTransactionRole.Server
                    && CanCheckMerged(entry.Request, request)
                    && SipRequestRouting.IsMergedRequest(entry.Request, request))
                {
                    SipTransactionAdmissionResult merged = CreateRejectedResult(true, false,
                        false, SipStatusCodes.LoopDetected,
                        "The request is merged with an existing server transaction.");
                    merged.TransactionId = entry.Id;
                    return merged;
                }
                index = index + 1;
            }

            if (entries.Count >= maximumTransactionCount)
            {
                return CreateRejectedResult(false, false, true,
                    SipStatusCodes.ServiceUnavailable,
                    "The live transaction limit has been reached.");
            }

            SipTransactionRegistryEntry added = new SipTransactionRegistryEntry();
            added.Id = transactionId;
            added.Role = role;
            added.Key = key;
            added.Request = CopyRequiredMessage(request);
            entries.Add(added);
            SipTransactionAdmissionResult accepted = new SipTransactionAdmissionResult();
            accepted.Accepted = true;
            accepted.TransactionId = transactionId;
            return accepted;
        }
    }

    internal bool RemoveTransaction(string? transactionId)
    {
        if (!IdentifierIsValid(transactionId))
        {
            return false;
        }

        lock (synchronization)
        {
            int index = 0;
            while (index < entries.Count)
            {
                if (entries[index].Id == transactionId)
                {
                    entries.RemoveAt(index);
                    return true;
                }
                index = index + 1;
            }
            return false;
        }
    }

    internal int GetTransactionCount()
    {
        lock (synchronization)
        {
            return entries.Count;
        }
    }

    internal SipMessage? FindTransactionRequest(string? transactionId)
    {
        if (!IdentifierIsValid(transactionId))
        {
            return null;
        }

        lock (synchronization)
        {
            int index = 0;
            while (index < entries.Count)
            {
                if (entries[index].Id == transactionId)
                {
                    return SipSnapshotCopy.CopyMessageModel(entries[index].Request);
                }
                index = index + 1;
            }
            return null;
        }
    }

    string? ValidateAdmission(string? transactionId, SipMessage? request,
        SipTransactionRole role)
    {
        if (!IdentifierIsValid(transactionId))
        {
            return "A bounded transaction identifier is required.";
        }
        if (role != SipTransactionRole.Client && role != SipTransactionRole.Server)
        {
            return "A client or server transaction role is required.";
        }
        if (request == null || request.Kind != SipMessageKind.Request
            || request.Method == SipMethod.Ack)
        {
            return "A transactional SIP request other than ACK is required.";
        }
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(request, true);
        if (SipMessageValidator.HasErrorIssues(issues))
        {
            return "The transaction request is malformed.";
        }
        if (request.ViaValues == null || request.ViaValues.Length == 0
            || request.CSeq == null || request.From == null || request.To == null
            || request.RequestUri == null)
        {
            return "The transaction request is incomplete.";
        }
        if (!allowLegacyMatching && !SipTransactionMatching.HasMagicCookie(
            request.ViaValues[0].Branch))
        {
            return "The transaction request requires a magic-cookie branch.";
        }
        return null;
    }

    bool EntryMatches(SipTransactionRegistryEntry entry, SipMessage request,
        SipTransactionKey key, SipTransactionRole role)
    {
        if (entry.Role != role || entry.Key == null)
        {
            return false;
        }
        if (role == SipTransactionRole.Server)
        {
            return SipTransactionMatching.MatchRequest(entry.Key, request, null,
                allowLegacyMatching);
        }
        return entry.Key.Branch == key.Branch && entry.Key.SentBy == key.SentBy
            && entry.Key.Method == key.Method && entry.Key.MethodToken == key.MethodToken;
    }

    static bool CanCheckMerged(SipMessage? first, SipMessage second)
    {
        return first != null && first.ViaValues != null && first.ViaValues.Length > 0
            && first.ViaValues[0] != null && SipAscii.IsToken(first.ViaValues[0].Branch)
            && second.ViaValues != null && second.ViaValues.Length > 0
            && second.ViaValues[0] != null && SipAscii.IsToken(second.ViaValues[0].Branch);
    }

    static bool IdentifierIsValid(string? transactionId)
    {
        return transactionId != null && transactionId.Length > 0
            && transactionId.Length <= SipDefaults.MaxHeaderLineBytes;
    }

    static SipTransactionAdmissionResult CreateRejectedResult(bool merged, bool malformed,
        bool overloaded, int statusCode, string errorMessage)
    {
        SipTransactionAdmissionResult result = new SipTransactionAdmissionResult();
        result.Merged = merged;
        result.Malformed = malformed;
        result.Overloaded = overloaded;
        result.StatusCode = statusCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    static SipMessage CopyRequiredMessage(SipMessage source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException("The transaction request cannot be copied.");
        }
        return copy;
    }
}
