using System;
using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class ChangeContinuationStatus
{
    internal bool Created;
    internal bool Updated;
    internal bool Destroyed;
}

internal class ChangesContinuation
{
    internal const int CreatedKind = 1;
    internal const int UpdatedKind = 2;
    internal const int DestroyedKind = 3;
    internal ValueValidator Values = new ValueValidator();
    internal List<string> Order = new List<string>();
    internal Dictionary<string, ChangeContinuationStatus> Statuses =
        new Dictionary<string, ChangeContinuationStatus>(StringComparer.Ordinal);
    internal HashSet<string> ReachedStates = new HashSet<string>(StringComparer.Ordinal);
    internal List<string> UpdatedProperties = new List<string>();
    internal HashSet<string> UpdatedPropertySet = new HashSet<string>(StringComparer.Ordinal);
    internal Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
    internal string? AccountId;
    internal string? OldState;
    internal string? NewState;
    internal bool HasPage;
    internal bool HasMoreChanges;
    internal bool HasKnownUpdatedProperties = true;

    internal void AddPage(
        string? accountId,
        string? oldState,
        string? newState,
        bool hasMoreChanges,
        List<string>? created,
        List<string>? updated,
        List<string>? destroyed,
        Dictionary<string, JsonElement>? unknownProperties)
    {
        if (!Values.IsId(accountId) || oldState == null || !Values.IsValidUnicode(oldState) ||
            newState == null || !Values.IsValidUnicode(newState) || created == null ||
            updated == null || destroyed == null || unknownProperties == null)
        {
            throw ProtocolFailure("A changes page contains invalid account, state, or collection metadata.");
        }

        if (!HasPage)
        {
            AccountId = accountId;
            OldState = oldState;
            HasPage = true;
            ReachedStates.Add(oldState);
        }
        else if (AccountId != accountId || NewState != oldState)
        {
            throw ProtocolFailure("A changes page does not continue the preceding account and state.");
        }

        bool stateAdvanced = oldState != newState;
        if (hasMoreChanges && !stateAdvanced || stateAdvanced && !ReachedStates.Add(newState))
        {
            throw ProtocolFailure("A changes page does not advance to a new continuation state.");
        }

        AddIds(created, CreatedKind);
        AddIds(updated, UpdatedKind);
        AddIds(destroyed, DestroyedKind);
        NewState = newState;
        HasMoreChanges = hasMoreChanges;
        foreach (KeyValuePair<string, JsonElement> property in unknownProperties)
        {
            UnknownProperties[property.Key] = property.Value.Clone();
        }
    }

    internal void AddUpdatedProperties(List<string>? properties)
    {
        if (properties == null)
        {
            HasKnownUpdatedProperties = false;
            UpdatedProperties.Clear();
            UpdatedPropertySet.Clear();
            return;
        }

        if (!HasKnownUpdatedProperties)
        {
            return;
        }

        foreach (string property in properties)
        {
            if (UpdatedPropertySet.Add(property))
            {
                UpdatedProperties.Add(property);
            }
        }
    }

    internal void AddIds(List<string> ids, int kind)
    {
        foreach (string id in ids)
        {
            if (!Values.IsId(id))
            {
                throw ProtocolFailure("A changes page contains an invalid identifier.");
            }

            ChangeContinuationStatus status;
            if (Statuses.ContainsKey(id))
            {
                status = Statuses[id];
            }
            else
            {
                status = new ChangeContinuationStatus();
                Statuses.Add(id, status);
                Order.Add(id);
            }

            if (kind == CreatedKind)
            {
                status.Created = true;
            }
            else if (kind == UpdatedKind)
            {
                status.Updated = true;
            }
            else if (kind == DestroyedKind)
            {
                status.Destroyed = true;
            }
        }
    }

    internal void CopyIds(
        List<string> created,
        List<string> updated,
        List<string> destroyed)
    {
        foreach (string id in Order)
        {
            ChangeContinuationStatus status = Statuses[id];
            if (status.Created && !status.Destroyed)
            {
                created.Add(id);
            }
            else if (status.Destroyed && !status.Created)
            {
                destroyed.Add(id);
            }
            else if (status.Updated && !status.Created && !status.Destroyed)
            {
                updated.Add(id);
            }
        }
    }

    internal JmapProtocolException ProtocolFailure(string message)
    {
        return new JmapProtocolException(message, "changes", default(JsonElement));
    }
}

internal class QueryChangesContinuation
{
    internal ValueValidator Values = new ValueValidator();

    internal JmapQuerySnapshot ContinueQuery(
        JmapQuerySnapshot? snapshot,
        string? accountId,
        string? oldQueryState,
        string? newQueryState,
        List<string>? removed,
        List<JmapAddedItem>? added)
    {
        if (snapshot == null || snapshot.Ids == null || !Values.IsId(snapshot.AccountId) ||
            snapshot.QueryState == null || !Values.IsValidUnicode(snapshot.QueryState))
        {
            throw ValidationFailure("A valid query snapshot is required.", "snapshot");
        }

        if (accountId != snapshot.AccountId || oldQueryState != snapshot.QueryState ||
            newQueryState == null || !Values.IsValidUnicode(newQueryState) || removed == null ||
            added == null)
        {
            throw ProtocolFailure("The queryChanges result does not continue the snapshot state.");
        }

        List<string> ids = new List<string>(snapshot.Ids);
        HashSet<string> present = ValidateSnapshotIds(ids);
        foreach (string id in removed)
        {
            if (!Values.IsId(id) || !present.Remove(id) || !ids.Remove(id))
            {
                throw ProtocolFailure("A removed query identifier is invalid or absent from the snapshot.");
            }
        }

        long previousIndex = 0L;
        bool hasPreviousIndex = false;
        foreach (JmapAddedItem item in added)
        {
            if (item == null || !Values.IsId(item.Id) || item.Id == null ||
                !Values.IsUnsignedInt(item.Index) || item.Index > ids.Count ||
                hasPreviousIndex && item.Index < previousIndex || !present.Add(item.Id))
            {
                throw ProtocolFailure("An added query item is invalid, out of order, or duplicated.");
            }

            ids.Insert((int)item.Index, item.Id);
            previousIndex = item.Index;
            hasPreviousIndex = true;
        }

        JmapQuerySnapshot result = new JmapQuerySnapshot();
        result.AccountId = snapshot.AccountId;
        result.QueryState = newQueryState;
        result.Ids = ids;
        return result;
    }

    internal HashSet<string> ValidateSnapshotIds(List<string> ids)
    {
        HashSet<string> present = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in ids)
        {
            if (!Values.IsId(id) || !present.Add(id))
            {
                throw ValidationFailure("The query snapshot contains an invalid or duplicate identifier.",
                    "snapshot/ids");
            }
        }

        return present;
    }

    internal JmapValidationException ValidationFailure(string message, string path)
    {
        return new JmapValidationException(
            message,
            new List<string> { path },
            default(JsonElement));
    }

    internal JmapProtocolException ProtocolFailure(string message)
    {
        return new JmapProtocolException(message, "queryChanges", default(JsonElement));
    }
}
