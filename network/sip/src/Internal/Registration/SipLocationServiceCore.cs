namespace sip;

internal class SipLocationServiceCore
{
    object sync;
    SipRegistrationBinding[] bindings;
    ISipLocationStorage? storage;

    internal SipLocationServiceCore()
    {
        sync = new object();
        bindings = new SipRegistrationBinding[0];
    }

    internal void ConfigureStorage(ISipLocationStorage configuredStorage)
    {
        if (configuredStorage == null)
        {
            throw new System.ArgumentNullException("storage");
        }
        lock (sync)
        {
            storage = configuredStorage;
        }
    }

    internal SipLocationResult Query(SipLocationQuery? query)
    {
        string? error = ValidateQuery(query);
        if (error != null)
        {
            return Failure(error);
        }

        lock (sync)
        {
            if (storage != null && query != null)
            {
                SipLocationResult stored = storage.QueryStoredLocations(
                    CopyQuery(query));
                return NormalizeStorageResult(stored, query, true);
            }
            return Success(CollectMatches(bindings, query, true));
        }
    }

    internal SipLocationResult Update(SipRegistrationBinding? binding)
    {
        string? error = ValidateBinding(binding);
        if (error != null)
        {
            return Failure(error);
        }
        if (binding == null)
        {
            return Failure("A registration binding is required.");
        }

        SipRegistrationBinding stored =
            SipSnapshotCopy.CopyRegistrationBindingArray(
                new SipRegistrationBinding[] { binding })[0];
        lock (sync)
        {
            if (storage != null)
            {
                SipLocationResult result = storage.UpdateStoredLocation(stored);
                SipLocationQuery storedQuery = new SipLocationQuery();
                storedQuery.AddressOfRecord = stored.AddressOfRecord;
                return NormalizeStorageResult(result, storedQuery, false);
            }
            int replaced = FindReplacement(bindings, stored);
            SipRegistrationBinding[] updated;
            if (replaced < 0)
            {
                updated = new SipRegistrationBinding[bindings.Length + 1];
                System.Array.Copy(bindings, updated, bindings.Length);
                updated[bindings.Length] = stored;
            }
            else
            {
                updated = new SipRegistrationBinding[bindings.Length];
                System.Array.Copy(bindings, updated, bindings.Length);
                updated[replaced] = stored;
            }

            bindings = updated;
            SipLocationQuery query = new SipLocationQuery();
            query.AddressOfRecord = stored.AddressOfRecord;
            return Success(CollectMatches(bindings, query, false));
        }
    }

    internal SipLocationResult Remove(SipLocationQuery? query)
    {
        string? error = ValidateQuery(query);
        if (error != null)
        {
            return Failure(error);
        }

        lock (sync)
        {
            if (storage != null && query != null)
            {
                SipLocationResult result = storage.RemoveStoredLocations(
                    CopyQuery(query));
                SipLocationQuery storedQuery = new SipLocationQuery();
                storedQuery.AddressOfRecord = query.AddressOfRecord;
                return NormalizeStorageResult(result, storedQuery, false);
            }
            SipRegistrationBinding[] kept = new SipRegistrationBinding[bindings.Length];
            int count = 0;
            int index = 0;
            while (index < bindings.Length)
            {
                if (!Matches(bindings[index], query, false))
                {
                    kept[count] = bindings[index];
                    count = count + 1;
                }
                index = index + 1;
            }

            bindings = Resize(kept, count);
            SipLocationQuery resultQuery = new SipLocationQuery();
            if (query != null)
            {
                resultQuery.AddressOfRecord = query.AddressOfRecord;
            }
            return Success(CollectMatches(bindings, resultQuery, false));
        }
    }

    internal SipLocationResult Replace(SipLocationUpdate? update)
    {
        string? error = ValidateUpdate(update);
        if (error != null)
        {
            return Failure(error);
        }
        if (update == null || update.AddressOfRecord == null)
        {
            return Failure("A complete location update is required.");
        }

        SipLocationUpdate stored = CopyUpdate(update);
        lock (sync)
        {
            if (storage != null)
            {
                SipLocationResult result = storage.ReplaceStoredLocations(stored);
                SipLocationQuery storedQuery = new SipLocationQuery();
                storedQuery.AddressOfRecord = stored.AddressOfRecord;
                return NormalizeStorageResult(result, storedQuery, false);
            }

            SipRegistrationBinding[] replaced =
                new SipRegistrationBinding[bindings.Length + stored.Bindings.Length];
            int count = 0;
            int index = 0;
            while (index < bindings.Length)
            {
                if (!SipUriComparison.CompareUriModels(
                        bindings[index].AddressOfRecord, stored.AddressOfRecord))
                {
                    replaced[count] = bindings[index];
                    count = count + 1;
                }
                index = index + 1;
            }
            index = 0;
            while (index < stored.Bindings.Length)
            {
                replaced[count] = stored.Bindings[index];
                count = count + 1;
                index = index + 1;
            }
            bindings = Resize(replaced, count);
            SipLocationQuery query = new SipLocationQuery();
            query.AddressOfRecord = stored.AddressOfRecord;
            return Success(CollectMatches(bindings, query, false));
        }
    }

    internal SipLocationResult Expire(long nowMilliseconds)
    {
        if (nowMilliseconds < 0)
        {
            return Failure("The expiry timestamp cannot be negative.");
        }

        lock (sync)
        {
            if (storage != null)
            {
                SipLocationResult result =
                    storage.ExpireStoredLocations(nowMilliseconds);
                return NormalizeExpiredStorageResult(result);
            }
            SipRegistrationBinding[] kept = new SipRegistrationBinding[bindings.Length];
            SipRegistrationBinding[] expired =
                new SipRegistrationBinding[bindings.Length];
            int count = 0;
            int expiredCount = 0;
            int index = 0;
            while (index < bindings.Length)
            {
                if (bindings[index].ExpiresAtMilliseconds > nowMilliseconds)
                {
                    kept[count] = bindings[index];
                    count = count + 1;
                }
                else
                {
                    expired[expiredCount] = bindings[index];
                    expiredCount = expiredCount + 1;
                }
                index = index + 1;
            }

            bindings = Resize(kept, count);
            return SuccessWithExpired(
                Order(bindings), Order(Resize(expired, expiredCount)));
        }
    }

    static string? ValidateQuery(SipLocationQuery? query)
    {
        if (query == null)
        {
            return "A location query is required.";
        }
        if (query.AddressOfRecord == null || !IsValidSipUri(query.AddressOfRecord))
        {
            return "A valid SIP or SIPS address of record is required.";
        }
        if (query.RegistrationId < -1 || query.RegistrationId == 0)
        {
            return "The registration identifier must be positive or absent.";
        }
        if (query.NowMilliseconds < -1)
        {
            return "The query timestamp must be nonnegative or absent.";
        }
        return null;
    }

    static SipLocationQuery CopyQuery(SipLocationQuery source)
    {
        SipLocationQuery copy = new SipLocationQuery();
        if (source.AddressOfRecord != null)
        {
            copy.AddressOfRecord =
                SipUriModelCopy.CreateNormalizedCopy(source.AddressOfRecord);
        }
        copy.InstanceId = source.InstanceId;
        copy.RegistrationId = source.RegistrationId;
        copy.NowMilliseconds = source.NowMilliseconds;
        return copy;
    }

    static SipLocationUpdate CopyUpdate(SipLocationUpdate source)
    {
        SipLocationUpdate copy = new SipLocationUpdate();
        if (source.AddressOfRecord != null)
        {
            copy.AddressOfRecord =
                SipUriModelCopy.CreateNormalizedCopy(source.AddressOfRecord);
        }
        copy.Bindings =
            SipSnapshotCopy.CopyRegistrationBindingArray(source.Bindings);
        return copy;
    }

    static SipLocationResult NormalizeStorageResult(SipLocationResult? result,
        SipLocationQuery query, bool applyTimestamp)
    {
        if (result == null)
        {
            return Failure("The location storage provider returned no result.");
        }
        if (!result.Success)
        {
            return Failure(result.ErrorMessage == null
                ? "The location storage operation failed." : result.ErrorMessage);
        }
        if (!ValidStorageBindings(result.Bindings))
        {
            return Failure(
                "The location storage provider returned invalid binding data.");
        }
        return Success(CollectMatches(result.Bindings, query, applyTimestamp));
    }

    static SipLocationResult NormalizeExpiredStorageResult(
        SipLocationResult? result)
    {
        if (result == null)
        {
            return Failure("The location storage provider returned no result.");
        }
        if (!result.Success)
        {
            return Failure(result.ErrorMessage == null
                ? "The location storage operation failed." : result.ErrorMessage);
        }
        if (!ValidStorageBindings(result.Bindings)
            || result.ExpiredBindings != null
                && !ValidStorageBindings(result.ExpiredBindings))
        {
            return Failure(
                "The location storage provider returned invalid binding data.");
        }
        SipRegistrationBinding[] expired = result.ExpiredBindings == null
            ? new SipRegistrationBinding[0] : result.ExpiredBindings;
        return SuccessWithExpired(
            Order(result.Bindings), Order(expired));
    }

    static bool ValidStorageBindings(
        SipRegistrationBinding[]? storedBindings)
    {
        if (storedBindings == null)
        {
            return false;
        }
        int index = 0;
        while (index < storedBindings.Length)
        {
            if (ValidateBinding(storedBindings[index]) != null)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static string? ValidateBinding(SipRegistrationBinding? binding)
    {
        if (binding == null || !IsValidSipUri(binding.AddressOfRecord))
        {
            return "A binding with a valid SIP or SIPS address of record is required.";
        }
        SipContactValue? contact = binding.Contact;
        SipUri? contactUri = contact == null || contact.Address == null
            ? null : contact.Address.Uri;
        if (contact == null || contact.Wildcard || !IsValidSipUri(contactUri))
        {
            return "A binding must contain one valid concrete SIP Contact.";
        }
        if (binding.CallId == null || binding.CallId.Length == 0 || binding.CSeq < 0)
        {
            return "A binding must contain Call-ID and CSeq metadata.";
        }
        if (binding.ExpiresAtMilliseconds < 0 || binding.QualityThousandths < 0
            || binding.QualityThousandths > 1000)
        {
            return "Binding expiry and quality metadata are invalid.";
        }
        if (binding.RegistrationId < -1 || binding.RegistrationId == 0
            || contact.RegistrationId != binding.RegistrationId
            || !SameText(contact.InstanceId, binding.InstanceId))
        {
            return "Binding and Contact outbound identities must match.";
        }
        if (binding.RegistrationId > 0 && binding.InstanceId == null)
        {
            return "A registration identifier requires an instance identifier.";
        }
        if (binding.Flow != null
            && (binding.RegistrationId <= 0
                || binding.Flow.RegistrationId != binding.RegistrationId
                || !SameText(binding.Flow.InstanceId, binding.InstanceId)))
        {
            return "The outbound flow identity must match the binding identity.";
        }
        return null;
    }

    static string? ValidateUpdate(SipLocationUpdate? update)
    {
        if (update == null || !IsValidSipUri(update.AddressOfRecord)
            || update.Bindings == null)
        {
            return "A complete location update with a valid address of record is required.";
        }
        int index = 0;
        while (index < update.Bindings.Length)
        {
            SipRegistrationBinding? binding = update.Bindings[index];
            string? error = ValidateBinding(binding);
            if (error != null)
            {
                return error;
            }
            if (binding == null || !SipUriComparison.CompareUriModels(
                    binding.AddressOfRecord, update.AddressOfRecord))
            {
                return "Every replacement binding must use the updated address of record.";
            }
            int earlier = 0;
            while (earlier < index)
            {
                if (BindingsMatch(update.Bindings[earlier], binding))
                {
                    return "A replacement binding set cannot contain duplicate contacts.";
                }
                earlier = earlier + 1;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsValidSipUri(SipUri? uri)
    {
        return uri != null
            && (uri.Scheme == SipUriScheme.Sip || uri.Scheme == SipUriScheme.Sips)
            && SipUriValidator.ValidateUriModel(uri).Length == 0;
    }

    static bool SameText(string? left, string? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }
        return left.Equals(right, System.StringComparison.OrdinalIgnoreCase);
    }

    static int FindReplacement(SipRegistrationBinding[] source,
        SipRegistrationBinding binding)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (SipUriComparison.CompareUriModels(source[index].AddressOfRecord,
                    binding.AddressOfRecord)
                && BindingsMatch(source[index], binding))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool BindingsMatch(SipRegistrationBinding left,
        SipRegistrationBinding right)
    {
        if (left.InstanceId != null && right.InstanceId != null
            && left.RegistrationId > 0 && right.RegistrationId > 0)
        {
            return left.RegistrationId == right.RegistrationId
                && left.InstanceId.Equals(right.InstanceId,
                    System.StringComparison.OrdinalIgnoreCase);
        }
        return SipContactRouting.ContactsMatch(left.Contact, right.Contact);
    }

    static SipRegistrationBinding[] CollectMatches(SipRegistrationBinding[] source,
        SipLocationQuery? query, bool applyTimestamp)
    {
        SipRegistrationBinding[] selected = new SipRegistrationBinding[source.Length];
        int count = 0;
        int index = 0;
        while (index < source.Length)
        {
            if (Matches(source[index], query, applyTimestamp))
            {
                selected[count] = source[index];
                count = count + 1;
            }
            index = index + 1;
        }
        return Order(Resize(selected, count));
    }

    static bool Matches(SipRegistrationBinding binding, SipLocationQuery? query,
        bool applyTimestamp)
    {
        if (query == null
            || !SipUriComparison.CompareUriModels(binding.AddressOfRecord,
                query.AddressOfRecord))
        {
            return false;
        }
        if (query.InstanceId != null
            && !SameText(binding.InstanceId, query.InstanceId))
        {
            return false;
        }
        if (query.RegistrationId >= 0
            && binding.RegistrationId != query.RegistrationId)
        {
            return false;
        }
        return !applyTimestamp || query.NowMilliseconds < 0
            || binding.ExpiresAtMilliseconds > query.NowMilliseconds;
    }

    static SipRegistrationBinding[] Resize(SipRegistrationBinding[] source,
        int count)
    {
        SipRegistrationBinding[] result = new SipRegistrationBinding[count];
        if (count > 0)
        {
            System.Array.Copy(source, result, count);
        }
        return result;
    }

    static SipRegistrationBinding[] Order(SipRegistrationBinding[] source)
    {
        SipRegistrationBinding[] ordered =
            SipSnapshotCopy.CopyRegistrationBindingArray(source);
        int index = 1;
        while (index < ordered.Length)
        {
            SipRegistrationBinding current = ordered[index];
            int insertion = index;
            while (insertion > 0
                && ComesBefore(current, ordered[insertion - 1]))
            {
                ordered[insertion] = ordered[insertion - 1];
                insertion = insertion - 1;
            }
            ordered[insertion] = current;
            index = index + 1;
        }
        return ordered;
    }

    static bool ComesBefore(SipRegistrationBinding left,
        SipRegistrationBinding right)
    {
        int addressOrder = System.String.Compare(FormatUri(left.AddressOfRecord),
            FormatUri(right.AddressOfRecord), System.StringComparison.Ordinal);
        if (addressOrder != 0)
        {
            return addressOrder < 0;
        }
        if (left.QualityThousandths != right.QualityThousandths)
        {
            return left.QualityThousandths > right.QualityThousandths;
        }
        if (left.ExpiresAtMilliseconds != right.ExpiresAtMilliseconds)
        {
            return left.ExpiresAtMilliseconds > right.ExpiresAtMilliseconds;
        }
        int contactOrder = System.String.Compare(FormatContact(left.Contact),
            FormatContact(right.Contact), System.StringComparison.Ordinal);
        if (contactOrder != 0)
        {
            return contactOrder < 0;
        }
        int instanceOrder = System.String.Compare(left.InstanceId,
            right.InstanceId, System.StringComparison.OrdinalIgnoreCase);
        if (instanceOrder != 0)
        {
            return instanceOrder < 0;
        }
        if (left.RegistrationId != right.RegistrationId)
        {
            return left.RegistrationId < right.RegistrationId;
        }
        int callIdOrder = System.String.Compare(left.CallId, right.CallId,
            System.StringComparison.Ordinal);
        if (callIdOrder != 0)
        {
            return callIdOrder < 0;
        }
        return left.CSeq < right.CSeq;
    }

    static string FormatContact(SipContactValue? contact)
    {
        SipUri? uri = contact == null || contact.Address == null
            ? null : contact.Address.Uri;
        return FormatUri(uri);
    }

    static string FormatUri(SipUri? uri)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        SipWriteResult formatted = SipUriFormatter.FormatUriModel(uri, options);
        if (!formatted.Success)
        {
            return "";
        }
        return System.Text.Encoding.UTF8.GetString(formatted.Bytes);
    }

    static SipLocationResult Success(SipRegistrationBinding[] resultBindings)
    {
        SipLocationResult result = new SipLocationResult();
        result.Success = true;
        result.Bindings = resultBindings;
        return result;
    }

    static SipLocationResult SuccessWithExpired(
        SipRegistrationBinding[] resultBindings,
        SipRegistrationBinding[] expiredBindings)
    {
        SipLocationResult result = new SipLocationResult();
        result.Success = true;
        result.Bindings = resultBindings;
        result.ExpiredBindings = expiredBindings;
        return result;
    }

    static SipLocationResult Failure(string message)
    {
        SipLocationResult result = new SipLocationResult();
        result.Success = false;
        result.ErrorMessage = message;
        return result;
    }
}
