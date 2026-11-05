using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class EmailQueryJsonCodec
{
    internal StandardMethodJsonCodec Standard;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Optional;

    internal EmailQueryJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Standard = new StandardMethodJsonCodec(reader);
        Values = new SessionJsonCodec(reader);
        Optional = new ProtocolJsonCodec(reader);
    }

    internal EmailFilter DecodeEmailFilter(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailFilter result = new EmailFilter();
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

            result.Conditions = new List<EmailFilter>();
            int index = 0;
            foreach (JsonElement condition in conditions.EnumerateArray())
            {
                result.Conditions.Add(DecodeEmailFilter(condition, path + "/conditions/" + index));
                index = index + 1;
            }
        }

        result.InMailbox = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.InMailbox), path + "/inMailbox");
        result.InMailboxOtherThan = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.InMailboxOtherThan), path + "/inMailboxOtherThan");
        result.Before = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Before), path + "/before");
        result.After = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.After), path + "/after");
        JsonElement minimum = Values.GetMember(value, JmapProperties.MinSize);
        if (minimum.ValueKind != JsonValueKind.Undefined)
        {
            result.MinSize = Values.ReadRequiredUnsignedInteger(minimum, path + "/minSize");
            result.HasMinSize = true;
        }

        JsonElement maximum = Values.GetMember(value, JmapProperties.MaxSize);
        if (maximum.ValueKind != JsonValueKind.Undefined)
        {
            result.MaxSize = Values.ReadRequiredUnsignedInteger(maximum, path + "/maxSize");
            result.HasMaxSize = true;
        }

        result.AllInThreadHaveKeyword = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.AllInThreadHaveKeyword), path + "/allInThreadHaveKeyword");
        result.SomeInThreadHaveKeyword = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.SomeInThreadHaveKeyword), path + "/someInThreadHaveKeyword");
        result.NoneInThreadHaveKeyword = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.NoneInThreadHaveKeyword), path + "/noneInThreadHaveKeyword");
        result.HasKeyword = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.HasKeyword), path + "/hasKeyword");
        result.NotKeyword = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.NotKeyword), path + "/notKeyword");
        JsonElement hasAttachment = Values.GetMember(value, JmapProperties.HasAttachment);
        if (hasAttachment.ValueKind != JsonValueKind.Undefined)
        {
            result.HasAttachment = Values.ReadRequiredBoolean(hasAttachment, path + "/hasAttachment");
            result.HasHasAttachment = true;
        }

        result.Text = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Text), path + "/text");
        result.From = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.From), path + "/from");
        result.To = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.To), path + "/to");
        result.Cc = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Cc), path + "/cc");
        result.Bcc = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Bcc), path + "/bcc");
        result.Subject = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Subject), path + "/subject");
        result.Body = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Body), path + "/body");
        result.Header = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Header), path + "/header");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailFilterNames());
        return result;
    }

    internal void EncodeEmailFilter(EmailFilter value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email filter cannot be null.", path);
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
            foreach (EmailFilter condition in value.Conditions)
            {
                EncodeEmailFilter(condition, writer, path + "/conditions/" + index);
                index = index + 1;
            }

            writer.WriteArrayEnd();
        }

        writer.WriteStringMember(JmapProperties.InMailbox, value.InMailbox, false);
        Optional.WriteOptionalStringArray(JmapProperties.InMailboxOtherThan, value.InMailboxOtherThan, writer, path + "/inMailboxOtherThan");
        writer.WriteStringMember(JmapProperties.Before, value.Before, false);
        writer.WriteStringMember(JmapProperties.After, value.After, false);
        if (value.HasMinSize)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.MinSize, value.MinSize, path + "/minSize");
        }

        if (value.HasMaxSize)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.MaxSize, value.MaxSize, path + "/maxSize");
        }

        writer.WriteStringMember(JmapProperties.AllInThreadHaveKeyword, value.AllInThreadHaveKeyword, false);
        writer.WriteStringMember(JmapProperties.SomeInThreadHaveKeyword, value.SomeInThreadHaveKeyword, false);
        writer.WriteStringMember(JmapProperties.NoneInThreadHaveKeyword, value.NoneInThreadHaveKeyword, false);
        writer.WriteStringMember(JmapProperties.HasKeyword, value.HasKeyword, false);
        writer.WriteStringMember(JmapProperties.NotKeyword, value.NotKeyword, false);
        if (value.HasHasAttachment)
        {
            writer.WriteBooleanMember(JmapProperties.HasAttachment, value.HasAttachment, true);
        }

        writer.WriteStringMember(JmapProperties.Text, value.Text, false);
        writer.WriteStringMember(JmapProperties.From, value.From, false);
        writer.WriteStringMember(JmapProperties.To, value.To, false);
        writer.WriteStringMember(JmapProperties.Cc, value.Cc, false);
        writer.WriteStringMember(JmapProperties.Bcc, value.Bcc, false);
        writer.WriteStringMember(JmapProperties.Subject, value.Subject, false);
        writer.WriteStringMember(JmapProperties.Body, value.Body, false);
        Optional.WriteOptionalStringArray(JmapProperties.Header, value.Header, writer, path + "/header");
        Values.WriteUnknownMembers(value.UnknownProperties, EmailFilterNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailComparator DecodeEmailComparator(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailComparator result = new EmailComparator();
        result.Property = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Property), path + "/property");
        result.Keyword = Optional.ReadOptionalString(Values.GetMember(value, "keyword"), path + "/keyword");
        JsonElement ascending = Values.GetMember(value, JmapProperties.IsAscending);
        if (ascending.ValueKind != JsonValueKind.Undefined)
        {
            result.IsAscending = Values.ReadRequiredBoolean(ascending, path + "/isAscending");
            result.HasIsAscending = true;
        }

        result.Collation = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Collation), path + "/collation");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailComparatorNames());
        return result;
    }

    internal void EncodeEmailComparator(EmailComparator value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email comparator cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Property, value.Property, path + "/property");
        writer.WriteStringMember("keyword", value.Keyword, false);
        if (value.HasIsAscending)
        {
            writer.WriteBooleanMember(JmapProperties.IsAscending, value.IsAscending, true);
        }

        writer.WriteStringMember(JmapProperties.Collation, value.Collation, false);
        Values.WriteUnknownMembers(value.UnknownProperties, EmailComparatorNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailQueryRequest DecodeEmailQueryRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailQueryRequest result = new EmailQueryRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        JsonElement filter = Values.GetMember(value, JmapProperties.Filter);
        if (filter.ValueKind != JsonValueKind.Undefined && filter.ValueKind != JsonValueKind.Null)
        {
            result.Filter = DecodeEmailFilter(filter, "/filter");
        }

        result.Sort = DecodeOptionalEmailComparatorList(Values.GetMember(value, JmapProperties.Sort), "/sort");
        ReadEmailQueryWindow(value, result);
        JsonElement collapse = Values.GetMember(value, JmapProperties.CollapseThreads);
        if (collapse.ValueKind != JsonValueKind.Undefined)
        {
            result.CollapseThreads = Values.ReadRequiredBoolean(collapse, "/collapseThreads");
            result.HasCollapseThreads = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailQueryRequestNames());
        return result;
    }

    internal void EncodeEmailQueryRequest(EmailQueryRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        if (value.Filter != null)
        {
            writer.WriteMemberName(JmapProperties.Filter);
            EncodeEmailFilter(value.Filter, writer, "/filter");
        }

        WriteOptionalEmailComparatorList(JmapProperties.Sort, value.Sort, writer, "/sort");
        WriteEmailQueryWindow(value, writer);
        if (value.HasCollapseThreads)
        {
            writer.WriteBooleanMember(JmapProperties.CollapseThreads, value.CollapseThreads, true);
        }

        Values.WriteUnknownMembers(value.UnknownProperties, EmailQueryRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailQueryResponse DecodeEmailQueryResponse(JsonElement value)
    {
        JmapQueryResponse common = Standard.DecodeQueryResponse(value);
        EmailQueryResponse result = new EmailQueryResponse();
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

    internal void EncodeEmailQueryResponse(EmailQueryResponse value, JmapJsonWriter writer)
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

    internal EmailQueryChangesRequest DecodeEmailQueryChangesRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailQueryChangesRequest result = new EmailQueryChangesRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        JsonElement filter = Values.GetMember(value, JmapProperties.Filter);
        if (filter.ValueKind != JsonValueKind.Undefined && filter.ValueKind != JsonValueKind.Null)
        {
            result.Filter = DecodeEmailFilter(filter, "/filter");
        }

        result.Sort = DecodeOptionalEmailComparatorList(Values.GetMember(value, JmapProperties.Sort), "/sort");
        result.SinceQueryState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.SinceQueryState), "/sinceQueryState");
        ReadEmailMaxChanges(Values.GetMember(value, JmapProperties.MaxChanges), result);
        result.UpToId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.UpToId), "/upToId");
        JsonElement calculate = Values.GetMember(value, JmapProperties.CalculateTotal);
        if (calculate.ValueKind != JsonValueKind.Undefined)
        {
            result.CalculateTotal = Values.ReadRequiredBoolean(calculate, "/calculateTotal");
            result.HasCalculateTotal = true;
        }

        JsonElement collapse = Values.GetMember(value, JmapProperties.CollapseThreads);
        if (collapse.ValueKind != JsonValueKind.Undefined)
        {
            result.CollapseThreads = Values.ReadRequiredBoolean(collapse, "/collapseThreads");
            result.HasCollapseThreads = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailQueryChangesRequestNames());
        return result;
    }

    internal void EncodeEmailQueryChangesRequest(EmailQueryChangesRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        if (value.Filter != null)
        {
            writer.WriteMemberName(JmapProperties.Filter);
            EncodeEmailFilter(value.Filter, writer, "/filter");
        }

        WriteOptionalEmailComparatorList(JmapProperties.Sort, value.Sort, writer, "/sort");
        Values.WriteRequiredStringMember(writer, JmapProperties.SinceQueryState, value.SinceQueryState, "/sinceQueryState");
        Standard.WriteOptionalUnsigned(JmapProperties.MaxChanges, value.MaxChanges, value.HasMaxChanges, value.IsMaxChangesNull, writer, "/maxChanges");
        writer.WriteStringMember(JmapProperties.UpToId, value.UpToId, false);
        if (value.HasCalculateTotal)
        {
            writer.WriteBooleanMember(JmapProperties.CalculateTotal, value.CalculateTotal, true);
        }

        if (value.HasCollapseThreads)
        {
            writer.WriteBooleanMember(JmapProperties.CollapseThreads, value.CollapseThreads, true);
        }

        Values.WriteUnknownMembers(value.UnknownProperties, EmailQueryChangesRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailQueryChangesResponse DecodeEmailQueryChangesResponse(JsonElement value)
    {
        JmapQueryChangesResponse common = Standard.DecodeQueryChangesResponse(value);
        EmailQueryChangesResponse result = new EmailQueryChangesResponse();
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

    internal void EncodeEmailQueryChangesResponse(EmailQueryChangesResponse value, JmapJsonWriter writer)
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

    internal SearchSnippet DecodeSearchSnippet(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        SearchSnippet result = new SearchSnippet();
        result.EmailId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.EmailId), path + "/emailId");
        result.Subject = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.Subject), path + "/subject");
        result.Preview = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.Preview), path + "/preview");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, SearchSnippetNames());
        return result;
    }

    internal void EncodeSearchSnippet(SearchSnippet value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A SearchSnippet cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.EmailId, value.EmailId, path + "/emailId");
        writer.WriteStringMember(JmapProperties.Subject, value.Subject, true);
        writer.WriteStringMember(JmapProperties.Preview, value.Preview, true);
        Values.WriteUnknownMembers(value.UnknownProperties, SearchSnippetNames(), writer);
        writer.WriteObjectEnd();
    }

    internal SearchSnippetGetRequest DecodeSearchSnippetGetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        SearchSnippetGetRequest result = new SearchSnippetGetRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        JsonElement filter = Values.GetMember(value, JmapProperties.Filter);
        if (filter.ValueKind != JsonValueKind.Undefined && filter.ValueKind != JsonValueKind.Null)
        {
            result.Filter = DecodeEmailFilter(filter, "/filter");
        }

        result.EmailIds = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.EmailIds), "/emailIds");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, SearchSnippetRequestNames());
        return result;
    }

    internal void EncodeSearchSnippetGetRequest(SearchSnippetGetRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        if (value.Filter != null)
        {
            writer.WriteMemberName(JmapProperties.Filter);
            EncodeEmailFilter(value.Filter, writer, "/filter");
        }

        writer.WriteMemberName(JmapProperties.EmailIds);
        Values.EncodeStringArray(value.EmailIds, writer, "/emailIds");
        Values.WriteUnknownMembers(value.UnknownProperties, SearchSnippetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal SearchSnippetGetResponse DecodeSearchSnippetGetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        SearchSnippetGetResponse result = new SearchSnippetGetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.List = DecodeSearchSnippetList(Values.GetMember(value, JmapProperties.List), "/list");
        JsonElement notFound = Values.GetMember(value, JmapProperties.NotFound);
        if (notFound.ValueKind == JsonValueKind.Null)
        {
            result.NotFound = null;
        }
        else
        {
            result.NotFound = Values.DecodeStringArray(notFound, "/notFound");
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, SearchSnippetResponseNames());
        return result;
    }

    internal void EncodeSearchSnippetGetResponse(SearchSnippetGetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteMemberName(JmapProperties.List);
        EncodeSearchSnippetList(value.List, writer, "/list");
        if (value.NotFound == null)
        {
            writer.WriteNullMember(JmapProperties.NotFound);
        }
        else
        {
            writer.WriteMemberName(JmapProperties.NotFound);
            Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        }

        Values.WriteUnknownMembers(value.UnknownProperties, SearchSnippetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal List<EmailComparator>? DecodeOptionalEmailComparatorList(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An Email comparator array is required.", path, Values.CloneContext(value));
        }

        List<EmailComparator> result = new List<EmailComparator>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeEmailComparator(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void WriteOptionalEmailComparatorList(string name, List<EmailComparator>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailComparator comparator in value)
        {
            EncodeEmailComparator(comparator, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal List<SearchSnippet> DecodeSearchSnippetList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A SearchSnippet array is required.", path, Values.CloneContext(value));
        }

        List<SearchSnippet> result = new List<SearchSnippet>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeSearchSnippet(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeSearchSnippetList(List<SearchSnippet> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A SearchSnippet array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (SearchSnippet item in value)
        {
            EncodeSearchSnippet(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal void ReadEmailQueryWindow(JsonElement source, EmailQueryRequest destination)
    {
        JsonElement position = Values.GetMember(source, JmapProperties.Position);
        if (position.ValueKind != JsonValueKind.Undefined)
        {
            destination.Position = Standard.ReadRequiredInteger(position, "/position");
            destination.HasPosition = true;
        }

        destination.Anchor = Optional.ReadOptionalString(Values.GetMember(source, JmapProperties.Anchor), "/anchor");
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

    internal void WriteEmailQueryWindow(EmailQueryRequest value, JmapJsonWriter writer)
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

    internal void ReadEmailMaxChanges(JsonElement source, EmailQueryChangesRequest destination)
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

    internal List<string> EmailFilterNames()
    {
        return new List<string>
        {
            JmapProperties.Operator, JmapProperties.Conditions, JmapProperties.InMailbox, JmapProperties.InMailboxOtherThan,
            JmapProperties.Before, JmapProperties.After, JmapProperties.MinSize, JmapProperties.MaxSize,
            JmapProperties.AllInThreadHaveKeyword, JmapProperties.SomeInThreadHaveKeyword,
            JmapProperties.NoneInThreadHaveKeyword, JmapProperties.HasKeyword, JmapProperties.NotKeyword,
            JmapProperties.HasAttachment, JmapProperties.Text, JmapProperties.From, JmapProperties.To,
            JmapProperties.Cc, JmapProperties.Bcc, JmapProperties.Subject, JmapProperties.Body, JmapProperties.Header
        };
    }

    internal List<string> EmailComparatorNames()
    {
        return new List<string> { JmapProperties.Property, "keyword", JmapProperties.IsAscending, JmapProperties.Collation };
    }

    internal List<string> EmailQueryRequestNames()
    {
        List<string> result = Standard.QueryRequestNames();
        result.Add(JmapProperties.CollapseThreads);
        return result;
    }

    internal List<string> EmailQueryChangesRequestNames()
    {
        List<string> result = Standard.QueryChangesRequestNames();
        result.Add(JmapProperties.CollapseThreads);
        return result;
    }

    internal List<string> SearchSnippetNames()
    {
        return new List<string> { JmapProperties.EmailId, JmapProperties.Subject, JmapProperties.Preview };
    }

    internal List<string> SearchSnippetRequestNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.Filter, JmapProperties.EmailIds };
    }

    internal List<string> SearchSnippetResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.List, JmapProperties.NotFound };
    }
}
