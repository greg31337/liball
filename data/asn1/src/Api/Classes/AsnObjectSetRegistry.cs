namespace asn1;

/// <summary>
/// Stores thread-safe dynamic additions for one codec or reflection catalog.
/// </summary>
public class AsnObjectSetRegistry : IAsnObjectSetRegistry
{
    internal object Gate = new object();
    internal System.Collections.Generic.Dictionary<AsnInformationObjectSet, ObjectSetRegistrationState> States =
        new System.Collections.Generic.Dictionary<AsnInformationObjectSet, ObjectSetRegistrationState>();
    internal DispatchKeyNormalizer Normalizer = new DispatchKeyNormalizer();

    /// <summary>
    /// Initializes an empty object-set registry with instance-owned state.
    /// </summary>
    public AsnObjectSetRegistry()
    {
    }

    /// <summary>
    /// Registers one dynamic information object and its optional open-type dispatch entry.
    /// </summary>
    /// <param name="objectSet">The extensible compiled object set.</param>
    /// <param name="item">The information object to add.</param>
    /// <param name="dispatchEntry">The open-type dispatch entry, or null when the object has no open-type mapping.</param>
    public void RegisterObject(
        AsnInformationObjectSet objectSet,
        AsnInformationObject item,
        AsnOpenTypeDispatchEntry? dispatchEntry)
    {
        if (objectSet == null)
        {
            throw new System.ArgumentNullException("objectSet");
        }

        if (item == null)
        {
            throw new System.ArgumentNullException("item");
        }

        if (!objectSet.Extensible)
        {
            throw new System.ArgumentException(
                "Dynamic objects can only be registered for an extensible object set.",
                "objectSet");
        }

        if (dispatchEntry != null
            && (dispatchEntry.Key == null || dispatchEntry.Type == null))
        {
            throw new System.ArgumentException(
                "A dynamic dispatch entry must contain both a key and a type.",
                "dispatchEntry");
        }

        lock (this.Gate)
        {
            ObjectSetRegistrationState state = this.GetOrCreateState(objectSet);
            if (this.ContainsObjectIdentity(objectSet, state, item))
            {
                throw new System.ArgumentException(
                    "The information object is already present in the object set.",
                    "item");
            }

            if (dispatchEntry != null
                && this.ContainsDispatchKey(objectSet, state, dispatchEntry.Key))
            {
                throw new System.ArgumentException(
                    "The dispatch key is already present in the object set.",
                    "dispatchEntry");
            }

            state.Objects = this.AppendObject(state.Objects, item);
            if (dispatchEntry != null)
            {
                state.DispatchEntries = this.AppendDispatchEntry(
                    state.DispatchEntries,
                    dispatchEntry);
            }
        }
    }

    /// <summary>
    /// Returns static objects followed by dynamic objects in registration order.
    /// </summary>
    /// <param name="objectSet">The compiled object set.</param>
    /// <returns>A snapshot containing the effective objects.</returns>
    public AsnInformationObject[] GetEffectiveObjects(AsnInformationObjectSet objectSet)
    {
        if (objectSet == null)
        {
            throw new System.ArgumentNullException("objectSet");
        }

        lock (this.Gate)
        {
            AsnInformationObject[] statics = this.GetStaticObjects(objectSet);
            ObjectSetRegistrationState? state = this.FindState(objectSet);
            if (state == null)
            {
                return this.CopyObjects(statics);
            }

            return this.CombineObjects(statics, state.Objects);
        }
    }

    /// <summary>
    /// Returns static entries followed by dynamic entries in registration order.
    /// </summary>
    /// <param name="objectSet">The compiled object set.</param>
    /// <returns>A snapshot containing the effective dispatch entries.</returns>
    public AsnOpenTypeDispatchEntry[] GetEffectiveDispatchEntries(
        AsnInformationObjectSet objectSet)
    {
        if (objectSet == null)
        {
            throw new System.ArgumentNullException("objectSet");
        }

        lock (this.Gate)
        {
            ObjectSetRegistrationState? state = this.FindState(objectSet);
            if (state == null)
            {
                return this.CopyDispatchEntries(objectSet.DispatchEntries);
            }

            return this.CombineDispatchEntries(
                objectSet.DispatchEntries,
                state.DispatchEntries);
        }
    }

    /// <summary>
    /// Resolves an open-type dispatch entry by its normalized UNIQUE-field value.
    /// </summary>
    /// <param name="objectSet">The compiled object set.</param>
    /// <param name="key">The dispatch key.</param>
    /// <returns>The matching entry, or null when no entry matches.</returns>
    public AsnOpenTypeDispatchEntry? ResolveDispatchEntry(
        AsnInformationObjectSet objectSet,
        AsnValue key)
    {
        if (objectSet == null)
        {
            throw new System.ArgumentNullException("objectSet");
        }

        if (key == null)
        {
            throw new System.ArgumentNullException("key");
        }

        NormalizedDispatchKey requested = this.Normalizer.NormalizeKey(key);
        lock (this.Gate)
        {
            AsnOpenTypeDispatchEntry? found = this.FindDispatchEntry(
                objectSet.DispatchEntries,
                requested.Identity);
            if (found != null)
            {
                return found;
            }

            ObjectSetRegistrationState? state = this.FindState(objectSet);
            return state == null
                ? null
                : this.FindDispatchEntry(state.DispatchEntries, requested.Identity);
        }
    }

    internal ObjectSetRegistrationState GetOrCreateState(
        AsnInformationObjectSet objectSet)
    {
        if (this.States.ContainsKey(objectSet))
        {
            return this.States[objectSet];
        }

        ObjectSetRegistrationState state = new ObjectSetRegistrationState();
        this.States.Add(objectSet, state);
        return state;
    }

    internal ObjectSetRegistrationState? FindState(AsnInformationObjectSet objectSet)
    {
        return this.States.ContainsKey(objectSet) ? this.States[objectSet] : null;
    }

    internal bool ContainsObjectIdentity(
        AsnInformationObjectSet objectSet,
        ObjectSetRegistrationState state,
        AsnInformationObject item)
    {
        AsnInformationObject[] statics = this.GetStaticObjects(objectSet);
        int index = 0;
        while (index < statics.Length)
        {
            if (this.HasSameObjectIdentity(statics[index], item))
            {
                return true;
            }

            index = index + 1;
        }

        index = 0;
        while (index < state.Objects.Length)
        {
            if (this.HasSameObjectIdentity(state.Objects[index], item))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasSameObjectIdentity(
        AsnInformationObject left,
        AsnInformationObject right)
    {
        if (left == right)
        {
            return true;
        }

        return left.StableId.Length > 0
            && right.StableId.Length > 0
            && left.StableId == right.StableId;
    }

    internal bool ContainsDispatchKey(
        AsnInformationObjectSet objectSet,
        ObjectSetRegistrationState state,
        AsnValue? key)
    {
        NormalizedDispatchKey requested = this.Normalizer.NormalizeKey(key);
        return this.FindDispatchEntry(objectSet.DispatchEntries, requested.Identity) != null
            || this.FindDispatchEntry(state.DispatchEntries, requested.Identity) != null;
    }

    internal AsnOpenTypeDispatchEntry? FindDispatchEntry(
        AsnOpenTypeDispatchEntry[] entries,
        string identity)
    {
        int index = 0;
        while (index < entries.Length)
        {
            NormalizedDispatchKey candidate = this.Normalizer.NormalizeKey(
                entries[index].Key);
            if (candidate.Success && candidate.Identity == identity)
            {
                return entries[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnInformationObject[] GetStaticObjects(AsnInformationObjectSet objectSet)
    {
        if (objectSet.Objects.Length > 0)
        {
            return objectSet.Objects;
        }

        return this.CombineObjects(objectSet.RootObjects, objectSet.AdditionalObjects);
    }

    internal AsnInformationObject[] AppendObject(
        AsnInformationObject[] source,
        AsnInformationObject item)
    {
        AsnInformationObject[] result = new AsnInformationObject[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        result[source.Length] = item;
        return result;
    }

    internal AsnOpenTypeDispatchEntry[] AppendDispatchEntry(
        AsnOpenTypeDispatchEntry[] source,
        AsnOpenTypeDispatchEntry item)
    {
        AsnOpenTypeDispatchEntry[] result = new AsnOpenTypeDispatchEntry[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        result[source.Length] = item;
        return result;
    }

    internal AsnInformationObject[] CopyObjects(AsnInformationObject[] source)
    {
        return this.CombineObjects(source, []);
    }

    internal AsnOpenTypeDispatchEntry[] CopyDispatchEntries(
        AsnOpenTypeDispatchEntry[] source)
    {
        return this.CombineDispatchEntries(source, []);
    }

    internal AsnInformationObject[] CombineObjects(
        AsnInformationObject[] first,
        AsnInformationObject[] second)
    {
        AsnInformationObject[] result = new AsnInformationObject[
            first.Length + second.Length];
        int index = 0;
        while (index < first.Length)
        {
            result[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            result[index] = second[secondIndex];
            index = index + 1;
            secondIndex = secondIndex + 1;
        }

        return result;
    }

    internal AsnOpenTypeDispatchEntry[] CombineDispatchEntries(
        AsnOpenTypeDispatchEntry[] first,
        AsnOpenTypeDispatchEntry[] second)
    {
        AsnOpenTypeDispatchEntry[] result = new AsnOpenTypeDispatchEntry[
            first.Length + second.Length];
        int index = 0;
        while (index < first.Length)
        {
            result[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            result[index] = second[secondIndex];
            index = index + 1;
            secondIndex = secondIndex + 1;
        }

        return result;
    }
}
