using System.Collections.Generic;

namespace http;

internal static class PriorityCodec
{
    const int DefaultUrgency = 3;
    const int MaximumUrgency = 7;

    internal static HttpPriority Parse(byte[] input)
    {
        StructuredFieldParser parser = new StructuredFieldParser(input);
        StructuredFieldValue structured = parser.ParseTopDictionary();
        HttpPriority result = new HttpPriority();
        result.StructuredValue = structured;

        int index = 0;
        while (index < structured.Members.Count)
        {
            StructuredFieldMember member = structured.Members[index];
            if (member.DictionaryKey == "u")
            {
                if (!member.IsInnerList && member.ValueKind == HttpFieldValueKind.Integer &&
                    member.IntegerValue >= 0 && member.IntegerValue <= MaximumUrgency)
                {
                    result.HasUrgency = true;
                    result.Urgency = (int)member.IntegerValue;
                }
            }
            else if (member.DictionaryKey == "i")
            {
                if (!member.IsInnerList && member.ValueKind == HttpFieldValueKind.Boolean)
                {
                    result.HasIncremental = true;
                    result.Incremental = member.BooleanValue;
                }
            }
            else
            {
                result.UnknownParameters.Add(member);
            }

            index = index + 1;
        }

        return result;
    }

    internal static HttpPriority? ParseValidated(byte[] input)
    {
        if (input == null)
        {
            return null;
        }

        StructuredFieldDictionaryValidator validator =
            new StructuredFieldDictionaryValidator(input);
        if (!validator.IsValidDictionary())
        {
            return null;
        }

        return Parse(input);
    }

    internal static byte[] Serialize(HttpPriority value)
    {
        StructuredFieldValue structured = BuildDictionary(value);
        return StructuredFieldWriter.SerializeValue(structured);
    }

    internal static HttpPriority MergeClientAndServer(
        HttpPriority client,
        HttpPriority server)
    {
        ValidatePriority(client);
        ValidatePriority(server);

        HttpPriority result = new HttpPriority();
        if (server.HasUrgency)
        {
            result.HasUrgency = true;
            result.Urgency = server.Urgency;
        }
        else if (client.HasUrgency)
        {
            result.HasUrgency = true;
            result.Urgency = client.Urgency;
        }

        if (server.HasIncremental)
        {
            result.HasIncremental = true;
            result.Incremental = server.Incremental;
        }
        else if (client.HasIncremental)
        {
            result.HasIncremental = true;
            result.Incremental = client.Incremental;
        }

        int index = 0;
        while (index < client.UnknownParameters.Count)
        {
            result.UnknownParameters.Add(client.UnknownParameters[index]);
            index = index + 1;
        }

        index = 0;
        while (index < server.UnknownParameters.Count)
        {
            ReplaceUnknown(result.UnknownParameters, server.UnknownParameters[index]);
            index = index + 1;
        }

        return result;
    }

    static StructuredFieldValue BuildDictionary(HttpPriority value)
    {
        ValidatePriority(value);

        StructuredFieldValue structured = new StructuredFieldValue();
        structured.IsDictionary = true;
        if (value.HasUrgency)
        {
            StructuredFieldMember urgency = new StructuredFieldMember();
            urgency.DictionaryKey = "u";
            urgency.HasExplicitDictionaryValue = true;
            urgency.ValueKind = HttpFieldValueKind.Integer;
            urgency.IntegerValue = value.Urgency;
            structured.Members.Add(urgency);
        }

        if (value.HasIncremental)
        {
            StructuredFieldMember incremental = new StructuredFieldMember();
            incremental.DictionaryKey = "i";
            incremental.HasExplicitDictionaryValue = !value.Incremental;
            incremental.ValueKind = HttpFieldValueKind.Boolean;
            incremental.BooleanValue = value.Incremental;
            structured.Members.Add(incremental);
        }

        int index = 0;
        while (index < value.UnknownParameters.Count)
        {
            structured.Members.Add(value.UnknownParameters[index]);
            index = index + 1;
        }

        return structured;
    }

    static void ValidatePriority(HttpPriority value)
    {
        if (value == null || value.UnknownParameters == null ||
            value.Urgency < 0 || value.Urgency > MaximumUrgency ||
            !value.HasUrgency && value.Urgency != DefaultUrgency ||
            !value.HasIncremental && value.Incremental)
        {
            throw CreateFieldError();
        }

        StructuredFieldValue unknown = new StructuredFieldValue();
        unknown.IsDictionary = true;
        int index = 0;
        while (index < value.UnknownParameters.Count)
        {
            StructuredFieldMember member = value.UnknownParameters[index];
            if (member == null || member.DictionaryKey == null ||
                member.DictionaryKey == "u" || member.DictionaryKey == "i")
            {
                throw CreateFieldError();
            }

            unknown.Members.Add(member);
            index = index + 1;
        }

        StructuredFieldWriter.SerializeValue(unknown);
    }

    static void ReplaceUnknown(
        List<StructuredFieldMember> members,
        StructuredFieldMember replacement)
    {
        int index = 0;
        while (index < members.Count)
        {
            if (members[index].DictionaryKey == replacement.DictionaryKey)
            {
                members[index] = replacement;
                return;
            }

            index = index + 1;
        }

        members.Add(replacement);
    }

    static HttpFieldException CreateFieldError()
    {
        return new HttpFieldException(
            "The Priority field value cannot be serialized.",
            "invalid-priority",
            HttpHeaderNames.Priority);
    }
}
