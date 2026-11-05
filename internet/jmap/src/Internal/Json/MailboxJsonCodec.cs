using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class MailboxJsonCodec
{
    internal StandardMethodJsonCodec Standard;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Errors;

    internal MailboxJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Standard = new StandardMethodJsonCodec(reader);
        Values = new SessionJsonCodec(reader);
        Errors = new ProtocolJsonCodec(reader);
    }

    internal Mailbox DecodeMailbox(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Mailbox result = new Mailbox();
        result.Id = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        result.Name = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.Name), path + "/name");
        result.ParentId = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.ParentId), path + "/parentId");
        result.Role = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.Role), path + "/role");
        JsonElement sortOrder = Values.GetMember(value, JmapProperties.SortOrder);
        if (sortOrder.ValueKind != JsonValueKind.Undefined)
        {
            result.SortOrder = Values.ReadRequiredUnsignedInteger(sortOrder, path + "/sortOrder");
        }

        JsonElement totalEmails = Values.GetMember(value, JmapProperties.TotalEmails);
        if (totalEmails.ValueKind != JsonValueKind.Undefined)
        {
            result.TotalEmails = Values.ReadRequiredUnsignedInteger(totalEmails, path + "/totalEmails");
        }

        JsonElement unreadEmails = Values.GetMember(value, JmapProperties.UnreadEmails);
        if (unreadEmails.ValueKind != JsonValueKind.Undefined)
        {
            result.UnreadEmails = Values.ReadRequiredUnsignedInteger(unreadEmails, path + "/unreadEmails");
        }

        JsonElement totalThreads = Values.GetMember(value, JmapProperties.TotalThreads);
        if (totalThreads.ValueKind != JsonValueKind.Undefined)
        {
            result.TotalThreads = Values.ReadRequiredUnsignedInteger(totalThreads, path + "/totalThreads");
        }

        JsonElement unreadThreads = Values.GetMember(value, JmapProperties.UnreadThreads);
        if (unreadThreads.ValueKind != JsonValueKind.Undefined)
        {
            result.UnreadThreads = Values.ReadRequiredUnsignedInteger(unreadThreads, path + "/unreadThreads");
        }

        JsonElement rights = Values.GetMember(value, JmapProperties.MyRights);
        if (rights.ValueKind != JsonValueKind.Undefined)
        {
            result.MyRights = DecodeMailboxRights(rights, path + "/myRights");
        }

        JsonElement subscribed = Values.GetMember(value, JmapProperties.IsSubscribed);
        if (subscribed.ValueKind != JsonValueKind.Undefined)
        {
            result.IsSubscribed = Values.ReadRequiredBoolean(subscribed, path + "/isSubscribed");
            result.HasIsSubscribed = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, MailboxNames());
        return result;
    }

    internal void EncodeMailbox(Mailbox value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A Mailbox cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Id, value.Id, path + "/id");
        Values.WriteRequiredStringMember(writer, JmapProperties.Name, value.Name, path + "/name");
        writer.WriteStringMember(JmapProperties.ParentId, value.ParentId, true);
        writer.WriteStringMember(JmapProperties.Role, value.Role, true);
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.SortOrder, value.SortOrder, path + "/sortOrder");
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.TotalEmails, value.TotalEmails, path + "/totalEmails");
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.UnreadEmails, value.UnreadEmails, path + "/unreadEmails");
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.TotalThreads, value.TotalThreads, path + "/totalThreads");
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.UnreadThreads, value.UnreadThreads, path + "/unreadThreads");
        if (value.MyRights == null)
        {
            throw Standard.Validation("Mailbox rights are required.", path + "/myRights");
        }

        writer.WriteMemberName(JmapProperties.MyRights);
        EncodeMailboxRights(value.MyRights, writer, path + "/myRights");
        writer.WriteBooleanMember(JmapProperties.IsSubscribed, value.IsSubscribed, true);
        Values.WriteUnknownMembers(value.UnknownProperties, MailboxNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeMailboxCreate(Mailbox value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A Mailbox create value cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Name, value.Name, path + "/name");
        writer.WriteStringMember(JmapProperties.ParentId, value.ParentId, true);
        writer.WriteStringMember(JmapProperties.Role, value.Role, true);
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.SortOrder, value.SortOrder, path + "/sortOrder");
        if (value.HasIsSubscribed)
        {
            writer.WriteBooleanMember(JmapProperties.IsSubscribed, value.IsSubscribed, true);
        }
        Values.WriteUnknownMembers(value.UnknownProperties, MailboxNames(), writer);
        writer.WriteObjectEnd();
    }

    internal MailboxRights DecodeMailboxRights(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        MailboxRights result = new MailboxRights();
        result.MayReadItems = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.MayReadItems), path + "/mayReadItems");
        result.MayAddItems = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.MayAddItems), path + "/mayAddItems");
        result.MayRemoveItems = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.MayRemoveItems), path + "/mayRemoveItems");
        result.MaySetSeen = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.MaySetSeen), path + "/maySetSeen");
        result.MaySetKeywords = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.MaySetKeywords), path + "/maySetKeywords");
        result.MayCreateChild = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.MayCreateChild), path + "/mayCreateChild");
        result.MayRename = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.MayRename), path + "/mayRename");
        result.MayDelete = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.MayDelete), path + "/mayDelete");
        result.MaySubmit = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.MaySubmit), path + "/maySubmit");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, RightsNames());
        return result;
    }

    internal void EncodeMailboxRights(MailboxRights value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("Mailbox rights cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteBooleanMember(JmapProperties.MayReadItems, value.MayReadItems, true);
        writer.WriteBooleanMember(JmapProperties.MayAddItems, value.MayAddItems, true);
        writer.WriteBooleanMember(JmapProperties.MayRemoveItems, value.MayRemoveItems, true);
        writer.WriteBooleanMember(JmapProperties.MaySetSeen, value.MaySetSeen, true);
        writer.WriteBooleanMember(JmapProperties.MaySetKeywords, value.MaySetKeywords, true);
        writer.WriteBooleanMember(JmapProperties.MayCreateChild, value.MayCreateChild, true);
        writer.WriteBooleanMember(JmapProperties.MayRename, value.MayRename, true);
        writer.WriteBooleanMember(JmapProperties.MayDelete, value.MayDelete, true);
        writer.WriteBooleanMember(JmapProperties.MaySubmit, value.MaySubmit, true);
        Values.WriteUnknownMembers(value.UnknownProperties, RightsNames(), writer);
        writer.WriteObjectEnd();
    }

    internal MailboxFilter DecodeMailboxFilter(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        MailboxFilter result = new MailboxFilter();
        JsonElement operatorValue = Values.GetMember(value, JmapProperties.Operator);
        if (operatorValue.ValueKind != JsonValueKind.Undefined)
        {
            result.Operator = Standard.DecodeFilterOperator(Values.ReadRequiredString(operatorValue, path + "/operator"), path + "/operator");
            result.HasOperator = true;
        }

        JsonElement conditions = Values.GetMember(value, JmapProperties.Conditions);
        if (conditions.ValueKind != JsonValueKind.Undefined && conditions.ValueKind != JsonValueKind.Null)
        {
            if (conditions.ValueKind != JsonValueKind.Array)
            {
                throw new JmapProtocolException("Filter conditions must be an array.", path + "/conditions", Values.CloneContext(conditions));
            }

            result.Conditions = new List<MailboxFilter>();
            int index = 0;
            foreach (JsonElement condition in conditions.EnumerateArray())
            {
                result.Conditions.Add(DecodeMailboxFilter(condition, path + "/conditions/" + index));
                index = index + 1;
            }
        }

        JsonElement parentId = Values.GetMember(value, JmapProperties.ParentId);
        if (parentId.ValueKind != JsonValueKind.Undefined)
        {
            result.ParentId = Errors.ReadOptionalString(parentId, path + "/parentId");
            result.HasParentId = true;
        }

        result.Name = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.Name), path + "/name");
        JsonElement role = Values.GetMember(value, JmapProperties.Role);
        if (role.ValueKind != JsonValueKind.Undefined)
        {
            result.Role = Errors.ReadOptionalString(role, path + "/role");
            result.HasRole = true;
        }

        JsonElement hasAnyRole = Values.GetMember(value, JmapProperties.HasAnyRole);
        if (hasAnyRole.ValueKind != JsonValueKind.Undefined)
        {
            result.HasAnyRole = Values.ReadRequiredBoolean(hasAnyRole, path + "/hasAnyRole");
            result.HasHasAnyRole = true;
        }

        JsonElement subscribed = Values.GetMember(value, JmapProperties.IsSubscribed);
        if (subscribed.ValueKind != JsonValueKind.Undefined)
        {
            result.IsSubscribed = Values.ReadRequiredBoolean(subscribed, path + "/isSubscribed");
            result.HasIsSubscribed = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, MailboxFilterNames());
        return result;
    }

    internal void EncodeMailboxFilter(MailboxFilter value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A Mailbox filter cannot be null.", path);
        }

        writer.WriteObjectStart();
        if (value.HasOperator)
        {
            writer.WriteStringMember(JmapProperties.Operator, Standard.EncodeFilterOperator(value.Operator, path + "/operator"), false);
            if (value.Conditions == null)
            {
                throw Standard.Validation("An operator filter requires conditions.", path + "/conditions");
            }

            writer.WriteMemberName(JmapProperties.Conditions);
            writer.WriteArrayStart();
            int index = 0;
            foreach (MailboxFilter condition in value.Conditions)
            {
                EncodeMailboxFilter(condition, writer, path + "/conditions/" + index);
                index = index + 1;
            }

            writer.WriteArrayEnd();
        }

        if (value.HasParentId)
        {
            writer.WriteStringMember(JmapProperties.ParentId, value.ParentId, true);
        }

        writer.WriteStringMember(JmapProperties.Name, value.Name, false);
        if (value.HasRole)
        {
            writer.WriteStringMember(JmapProperties.Role, value.Role, true);
        }

        if (value.HasHasAnyRole)
        {
            writer.WriteBooleanMember(JmapProperties.HasAnyRole, value.HasAnyRole, true);
        }

        if (value.HasIsSubscribed)
        {
            writer.WriteBooleanMember(JmapProperties.IsSubscribed, value.IsSubscribed, true);
        }

        Values.WriteUnknownMembers(value.UnknownProperties, MailboxFilterNames(), writer);
        writer.WriteObjectEnd();
    }

    internal MailboxGetRequest DecodeMailboxGetRequest(JsonElement value)
    {
        JmapGetRequest common = Standard.DecodeGetRequest(value);
        MailboxGetRequest result = new MailboxGetRequest();
        result.AccountId = common.AccountId;
        result.Ids = common.Ids;
        result.Properties = common.Properties;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeMailboxGetRequest(MailboxGetRequest value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        JmapGetRequest common = new JmapGetRequest();
        common.AccountId = value.AccountId;
        common.Ids = value.Ids;
        common.Properties = value.Properties;
        common.UnknownProperties = value.UnknownProperties;
        Standard.EncodeGetRequest(common, writer);
    }

    internal MailboxGetResponse DecodeMailboxGetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        MailboxGetResponse result = new MailboxGetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.State = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.State), "/state");
        result.List = DecodeMailboxList(Values.GetMember(value, JmapProperties.List), "/list");
        result.NotFound = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound });
        return result;
    }

    internal void EncodeMailboxGetResponse(MailboxGetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.State, value.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        EncodeMailboxList(value.List, writer, "/list");
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound }, writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeProjectedMailboxGetResponse(MailboxGetResponse value, List<string>? properties, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.State, value.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        writer.WriteArrayStart();
        int index = 0;
        foreach (Mailbox mailbox in value.List)
        {
            EncodeProjectedMailbox(mailbox, properties, writer, "/list/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound }, writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeProjectedMailbox(Mailbox value, List<string>? properties, JmapJsonWriter writer, string path)
    {
        if (properties == null)
        {
            EncodeMailbox(value, writer, path);
            return;
        }

        if (value == null)
        {
            throw Standard.Validation("A Mailbox cannot be null.", path);
        }

        HashSet<string> included = new HashSet<string>(properties, System.StringComparer.Ordinal);
        included.Add(JmapProperties.Id);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Id, value.Id, path + "/id");
        if (included.Contains(JmapProperties.Name))
        {
            Values.WriteRequiredStringMember(writer, JmapProperties.Name, value.Name, path + "/name");
        }

        if (included.Contains(JmapProperties.ParentId))
        {
            writer.WriteStringMember(JmapProperties.ParentId, value.ParentId, true);
        }

        if (included.Contains(JmapProperties.Role))
        {
            writer.WriteStringMember(JmapProperties.Role, value.Role, true);
        }

        if (included.Contains(JmapProperties.SortOrder))
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.SortOrder, value.SortOrder, path + "/sortOrder");
        }

        if (included.Contains(JmapProperties.TotalEmails))
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.TotalEmails, value.TotalEmails, path + "/totalEmails");
        }

        if (included.Contains(JmapProperties.UnreadEmails))
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.UnreadEmails, value.UnreadEmails, path + "/unreadEmails");
        }

        if (included.Contains(JmapProperties.TotalThreads))
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.TotalThreads, value.TotalThreads, path + "/totalThreads");
        }

        if (included.Contains(JmapProperties.UnreadThreads))
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.UnreadThreads, value.UnreadThreads, path + "/unreadThreads");
        }

        if (included.Contains(JmapProperties.MyRights))
        {
            if (value.MyRights == null)
            {
                throw Standard.Validation("Mailbox rights are required.", path + "/myRights");
            }

            writer.WriteMemberName(JmapProperties.MyRights);
            EncodeMailboxRights(value.MyRights, writer, path + "/myRights");
        }

        if (included.Contains(JmapProperties.IsSubscribed))
        {
            writer.WriteBooleanMember(JmapProperties.IsSubscribed, value.IsSubscribed, true);
        }

        writer.WriteObjectEnd();
    }

    internal MailboxChangesRequest DecodeMailboxChangesRequest(JsonElement value)
    {
        JmapChangesRequest common = Standard.DecodeChangesRequest(value);
        MailboxChangesRequest result = new MailboxChangesRequest();
        result.AccountId = common.AccountId;
        result.SinceState = common.SinceState;
        result.MaxChanges = common.MaxChanges;
        result.HasMaxChanges = common.HasMaxChanges;
        result.IsMaxChangesNull = common.IsMaxChangesNull;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeMailboxChangesRequest(MailboxChangesRequest value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        JmapChangesRequest common = new JmapChangesRequest();
        common.AccountId = value.AccountId;
        common.SinceState = value.SinceState;
        common.MaxChanges = value.MaxChanges;
        common.HasMaxChanges = value.HasMaxChanges;
        common.IsMaxChangesNull = value.IsMaxChangesNull;
        common.UnknownProperties = value.UnknownProperties;
        Standard.EncodeChangesRequest(common, writer);
    }

    internal MailboxChangesResponse DecodeMailboxChangesResponse(JsonElement value)
    {
        JmapChangesResponse common = Standard.DecodeChangesResponse(value);
        MailboxChangesResponse result = new MailboxChangesResponse();
        result.AccountId = common.AccountId;
        result.OldState = common.OldState;
        result.NewState = common.NewState;
        result.HasMoreChanges = common.HasMoreChanges;
        result.Created = common.Created;
        result.Updated = common.Updated;
        result.Destroyed = common.Destroyed;
        result.UpdatedProperties = Errors.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.UpdatedProperties), "/updatedProperties");
        common.UnknownProperties.Remove(JmapProperties.UpdatedProperties);
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeMailboxChangesResponse(MailboxChangesResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.OldState, value.OldState, "/oldState");
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        writer.WriteBooleanMember(JmapProperties.HasMoreChanges, value.HasMoreChanges, true);
        writer.WriteMemberName(JmapProperties.Created);
        Values.EncodeStringArray(value.Created, writer, "/created");
        writer.WriteMemberName(JmapProperties.Updated);
        Values.EncodeStringArray(value.Updated, writer, "/updated");
        writer.WriteMemberName(JmapProperties.Destroyed);
        Values.EncodeStringArray(value.Destroyed, writer, "/destroyed");
        Errors.WriteOptionalStringArray(JmapProperties.UpdatedProperties, value.UpdatedProperties, writer, "/updatedProperties");
        List<string> names = Standard.ChangesResponseNames();
        names.Add(JmapProperties.UpdatedProperties);
        Values.WriteUnknownMembers(value.UnknownProperties, names, writer);
        writer.WriteObjectEnd();
    }

    internal MailboxQueryRequest DecodeMailboxQueryRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        MailboxQueryRequest result = new MailboxQueryRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        JsonElement filter = Values.GetMember(value, JmapProperties.Filter);
        if (filter.ValueKind != JsonValueKind.Undefined && filter.ValueKind != JsonValueKind.Null)
        {
            result.Filter = DecodeMailboxFilter(filter, "/filter");
        }

        result.Sort = Standard.DecodeOptionalComparatorList(Values.GetMember(value, JmapProperties.Sort), "/sort");
        ReadMailboxQueryWindow(value, result);
        JsonElement sortAsTree = Values.GetMember(value, JmapProperties.SortAsTree);
        if (sortAsTree.ValueKind != JsonValueKind.Undefined)
        {
            result.SortAsTree = Values.ReadRequiredBoolean(sortAsTree, "/sortAsTree");
            result.HasSortAsTree = true;
        }

        JsonElement filterAsTree = Values.GetMember(value, JmapProperties.FilterAsTree);
        if (filterAsTree.ValueKind != JsonValueKind.Undefined)
        {
            result.FilterAsTree = Values.ReadRequiredBoolean(filterAsTree, "/filterAsTree");
            result.HasFilterAsTree = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, MailboxQueryRequestNames());
        return result;
    }

    internal void EncodeMailboxQueryRequest(MailboxQueryRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        if (value.Filter != null)
        {
            writer.WriteMemberName(JmapProperties.Filter);
            EncodeMailboxFilter(value.Filter, writer, "/filter");
        }

        Standard.WriteOptionalComparatorList(JmapProperties.Sort, value.Sort, writer, "/sort");
        WriteMailboxQueryWindow(value, writer);
        if (value.HasSortAsTree)
        {
            writer.WriteBooleanMember(JmapProperties.SortAsTree, value.SortAsTree, true);
        }

        if (value.HasFilterAsTree)
        {
            writer.WriteBooleanMember(JmapProperties.FilterAsTree, value.FilterAsTree, true);
        }

        Values.WriteUnknownMembers(value.UnknownProperties, MailboxQueryRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal MailboxQueryResponse DecodeMailboxQueryResponse(JsonElement value)
    {
        JmapQueryResponse common = Standard.DecodeQueryResponse(value);
        MailboxQueryResponse result = new MailboxQueryResponse();
        result.AccountId = common.AccountId;
        result.QueryState = common.QueryState;
        result.CanCalculateChanges = common.CanCalculateChanges;
        result.Position = common.Position;
        result.Ids = common.Ids;
        result.Total = common.Total;
        result.HasTotal = common.HasTotal;
        result.Limit = common.Limit;
        result.HasLimit = common.HasLimit;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeMailboxQueryResponse(MailboxQueryResponse value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        JmapQueryResponse common = new JmapQueryResponse();
        common.AccountId = value.AccountId;
        common.QueryState = value.QueryState;
        common.CanCalculateChanges = value.CanCalculateChanges;
        common.Position = value.Position;
        common.Ids = value.Ids;
        common.Total = value.Total;
        common.HasTotal = value.HasTotal;
        common.Limit = value.Limit;
        common.HasLimit = value.HasLimit;
        common.UnknownProperties = value.UnknownProperties;
        Standard.EncodeQueryResponse(common, writer);
    }

    internal MailboxQueryChangesRequest DecodeMailboxQueryChangesRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        MailboxQueryChangesRequest result = new MailboxQueryChangesRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        JsonElement filter = Values.GetMember(value, JmapProperties.Filter);
        if (filter.ValueKind != JsonValueKind.Undefined && filter.ValueKind != JsonValueKind.Null)
        {
            result.Filter = DecodeMailboxFilter(filter, "/filter");
        }

        result.Sort = Standard.DecodeOptionalComparatorList(Values.GetMember(value, JmapProperties.Sort), "/sort");
        result.SinceQueryState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.SinceQueryState), "/sinceQueryState");
        ReadMailboxMaxChanges(Values.GetMember(value, JmapProperties.MaxChanges), result);
        result.UpToId = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.UpToId), "/upToId");
        JsonElement calculate = Values.GetMember(value, JmapProperties.CalculateTotal);
        if (calculate.ValueKind != JsonValueKind.Undefined)
        {
            result.CalculateTotal = Values.ReadRequiredBoolean(calculate, "/calculateTotal");
            result.HasCalculateTotal = true;
        }

        JsonElement sortAsTree = Values.GetMember(value, JmapProperties.SortAsTree);
        if (sortAsTree.ValueKind != JsonValueKind.Undefined)
        {
            result.SortAsTree = Values.ReadRequiredBoolean(sortAsTree, "/sortAsTree");
            result.HasSortAsTree = true;
        }

        JsonElement filterAsTree = Values.GetMember(value, JmapProperties.FilterAsTree);
        if (filterAsTree.ValueKind != JsonValueKind.Undefined)
        {
            result.FilterAsTree = Values.ReadRequiredBoolean(filterAsTree, "/filterAsTree");
            result.HasFilterAsTree = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, MailboxQueryChangesRequestNames());
        return result;
    }

    internal void EncodeMailboxQueryChangesRequest(MailboxQueryChangesRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        if (value.Filter != null)
        {
            writer.WriteMemberName(JmapProperties.Filter);
            EncodeMailboxFilter(value.Filter, writer, "/filter");
        }

        Standard.WriteOptionalComparatorList(JmapProperties.Sort, value.Sort, writer, "/sort");
        Values.WriteRequiredStringMember(writer, JmapProperties.SinceQueryState, value.SinceQueryState, "/sinceQueryState");
        Standard.WriteOptionalUnsigned(JmapProperties.MaxChanges, value.MaxChanges, value.HasMaxChanges, value.IsMaxChangesNull, writer, "/maxChanges");
        writer.WriteStringMember(JmapProperties.UpToId, value.UpToId, false);
        if (value.HasCalculateTotal)
        {
            writer.WriteBooleanMember(JmapProperties.CalculateTotal, value.CalculateTotal, true);
        }

        if (value.HasSortAsTree)
        {
            writer.WriteBooleanMember(JmapProperties.SortAsTree, value.SortAsTree, true);
        }

        if (value.HasFilterAsTree)
        {
            writer.WriteBooleanMember(JmapProperties.FilterAsTree, value.FilterAsTree, true);
        }

        Values.WriteUnknownMembers(value.UnknownProperties, MailboxQueryChangesRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal MailboxQueryChangesResponse DecodeMailboxQueryChangesResponse(JsonElement value)
    {
        JmapQueryChangesResponse common = Standard.DecodeQueryChangesResponse(value);
        MailboxQueryChangesResponse result = new MailboxQueryChangesResponse();
        result.AccountId = common.AccountId;
        result.OldQueryState = common.OldQueryState;
        result.NewQueryState = common.NewQueryState;
        result.Total = common.Total;
        result.HasTotal = common.HasTotal;
        result.Removed = common.Removed;
        result.Added = common.Added;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeMailboxQueryChangesResponse(MailboxQueryChangesResponse value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        JmapQueryChangesResponse common = new JmapQueryChangesResponse();
        common.AccountId = value.AccountId;
        common.OldQueryState = value.OldQueryState;
        common.NewQueryState = value.NewQueryState;
        common.Total = value.Total;
        common.HasTotal = value.HasTotal;
        common.Removed = value.Removed;
        common.Added = value.Added;
        common.UnknownProperties = value.UnknownProperties;
        Standard.EncodeQueryChangesResponse(common, writer);
    }

    internal MailboxSetRequest DecodeMailboxSetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        MailboxSetRequest result = new MailboxSetRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        result.Create = DecodeOptionalMailboxMap(Values.GetMember(value, JmapProperties.Create), "/create");
        result.Update = Standard.DecodeOptionalPatchMap(Values.GetMember(value, JmapProperties.Update), "/update");
        result.Destroy = Errors.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroy), "/destroy");
        JsonElement removeEmails = Values.GetMember(value, JmapProperties.OnDestroyRemoveEmails);
        if (removeEmails.ValueKind != JsonValueKind.Undefined)
        {
            result.OnDestroyRemoveEmails = Values.ReadRequiredBoolean(removeEmails, "/onDestroyRemoveEmails");
            result.HasOnDestroyRemoveEmails = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, MailboxSetRequestNames());
        return result;
    }

    internal void EncodeMailboxSetRequest(MailboxSetRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.IfInState, value.IfInState, false);
        WriteOptionalMailboxCreateMap(JmapProperties.Create, value.Create, writer, "/create");
        Standard.WriteOptionalPatchMap(JmapProperties.Update, value.Update, writer, "/update");
        Errors.WriteOptionalStringArray(JmapProperties.Destroy, value.Destroy, writer, "/destroy");
        if (value.HasOnDestroyRemoveEmails)
        {
            writer.WriteBooleanMember(JmapProperties.OnDestroyRemoveEmails, value.OnDestroyRemoveEmails, true);
        }

        Values.WriteUnknownMembers(value.UnknownProperties, MailboxSetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal MailboxSetResponse DecodeMailboxSetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        MailboxSetResponse result = new MailboxSetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.Created = DecodeOptionalMailboxMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.Updated = Standard.DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Updated), "/updated");
        result.Destroyed = Errors.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroyed), "/destroyed");
        result.NotCreated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        result.NotUpdated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotUpdated), "/notUpdated");
        result.NotDestroyed = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotDestroyed), "/notDestroyed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.SetResponseNames());
        return result;
    }

    internal void EncodeMailboxSetResponse(MailboxSetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.OldState, value.OldState, true);
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        WriteOptionalMailboxMap(JmapProperties.Created, value.Created, writer, "/created");
        Standard.WriteOptionalElementMap(JmapProperties.Updated, value.Updated, writer, "/updated");
        Errors.WriteOptionalStringArray(JmapProperties.Destroyed, value.Destroyed, writer, "/destroyed");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotUpdated, value.NotUpdated, writer, "/notUpdated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotDestroyed, value.NotDestroyed, writer, "/notDestroyed");
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.SetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal List<Mailbox> DecodeMailboxList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A Mailbox array is required.", path, Values.CloneContext(value));
        }

        List<Mailbox> result = new List<Mailbox>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeMailbox(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeMailboxList(List<Mailbox> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A Mailbox array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (Mailbox mailbox in value)
        {
            EncodeMailbox(mailbox, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, Mailbox>? DecodeOptionalMailboxMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, Mailbox> result = new Dictionary<string, Mailbox>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeMailbox(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalMailboxMap(string name, Dictionary<string, Mailbox>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeMailbox(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void WriteOptionalMailboxCreateMap(string name, Dictionary<string, Mailbox>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeMailboxCreate(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void ReadMailboxQueryWindow(JsonElement source, MailboxQueryRequest destination)
    {
        JsonElement position = Values.GetMember(source, JmapProperties.Position);
        if (position.ValueKind != JsonValueKind.Undefined)
        {
            destination.Position = Standard.ReadRequiredInteger(position, "/position");
            destination.HasPosition = true;
        }

        destination.Anchor = Errors.ReadOptionalString(Values.GetMember(source, JmapProperties.Anchor), "/anchor");
        JsonElement anchorOffset = Values.GetMember(source, JmapProperties.AnchorOffset);
        if (anchorOffset.ValueKind != JsonValueKind.Undefined)
        {
            destination.AnchorOffset = Standard.ReadRequiredInteger(anchorOffset, "/anchorOffset");
            destination.HasAnchorOffset = true;
        }

        JsonElement limit = Values.GetMember(source, JmapProperties.Limit);
        if (limit.ValueKind != JsonValueKind.Undefined)
        {
            destination.HasLimit = true;
            if (limit.ValueKind == JsonValueKind.Null)
            {
                destination.IsLimitNull = true;
            }
            else
            {
                destination.Limit = Values.ReadRequiredUnsignedInteger(limit, "/limit");
            }
        }

        JsonElement calculate = Values.GetMember(source, JmapProperties.CalculateTotal);
        if (calculate.ValueKind != JsonValueKind.Undefined)
        {
            destination.CalculateTotal = Values.ReadRequiredBoolean(calculate, "/calculateTotal");
            destination.HasCalculateTotal = true;
        }
    }

    internal void WriteMailboxQueryWindow(MailboxQueryRequest value, JmapJsonWriter writer)
    {
        if (value.HasPosition)
        {
            writer.WriteIntegerMember(JmapProperties.Position, value.Position, true);
        }

        writer.WriteStringMember(JmapProperties.Anchor, value.Anchor, false);
        if (value.HasAnchorOffset)
        {
            writer.WriteIntegerMember(JmapProperties.AnchorOffset, value.AnchorOffset, true);
        }

        Standard.WriteOptionalUnsigned(JmapProperties.Limit, value.Limit, value.HasLimit, value.IsLimitNull, writer, "/limit");
        if (value.HasCalculateTotal)
        {
            writer.WriteBooleanMember(JmapProperties.CalculateTotal, value.CalculateTotal, true);
        }
    }

    internal void ReadMailboxMaxChanges(JsonElement source, MailboxQueryChangesRequest destination)
    {
        if (source.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        destination.HasMaxChanges = true;
        if (source.ValueKind == JsonValueKind.Null)
        {
            destination.IsMaxChangesNull = true;
        }
        else
        {
            destination.MaxChanges = Values.ReadRequiredUnsignedInteger(source, "/maxChanges");
        }
    }

    internal List<string> MailboxNames()
    {
        return new List<string> { JmapProperties.Id, JmapProperties.Name, JmapProperties.ParentId, JmapProperties.Role, JmapProperties.SortOrder, JmapProperties.TotalEmails, JmapProperties.UnreadEmails, JmapProperties.TotalThreads, JmapProperties.UnreadThreads, JmapProperties.MyRights, JmapProperties.IsSubscribed };
    }

    internal List<string> RightsNames()
    {
        return new List<string> { JmapProperties.MayReadItems, JmapProperties.MayAddItems, JmapProperties.MayRemoveItems, JmapProperties.MaySetSeen, JmapProperties.MaySetKeywords, JmapProperties.MayCreateChild, JmapProperties.MayRename, JmapProperties.MayDelete, JmapProperties.MaySubmit };
    }

    internal List<string> MailboxFilterNames()
    {
        return new List<string> { JmapProperties.Operator, JmapProperties.Conditions, JmapProperties.ParentId, JmapProperties.Name, JmapProperties.Role, JmapProperties.HasAnyRole, JmapProperties.IsSubscribed };
    }

    internal List<string> MailboxQueryRequestNames()
    {
        List<string> result = Standard.QueryRequestNames();
        result.Add(JmapProperties.SortAsTree);
        result.Add(JmapProperties.FilterAsTree);
        return result;
    }

    internal List<string> MailboxQueryChangesRequestNames()
    {
        List<string> result = Standard.QueryChangesRequestNames();
        result.Add(JmapProperties.SortAsTree);
        result.Add(JmapProperties.FilterAsTree);
        return result;
    }

    internal List<string> MailboxSetRequestNames()
    {
        List<string> result = Standard.SetRequestNames();
        result.Add(JmapProperties.OnDestroyRemoveEmails);
        return result;
    }
}
