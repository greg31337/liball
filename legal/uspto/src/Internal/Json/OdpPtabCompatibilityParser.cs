namespace uspto;

internal static class OdpPtabCompatibilityParser
{
    internal static JsonFieldValue ReadAliasedArray(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadArray(names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return reader.ReadArray(names[0]);
    }

    internal static JsonFieldValue ReadAliasedObject(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadObject(names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return reader.ReadObject(names[0]);
    }

    internal static JsonFieldValue ReadAliasedString(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadString(names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return reader.ReadString(names[0]);
    }

    internal static JsonFieldValue ReadStringCollection(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue raw = reader.ReadRaw(names[index]);
            if (raw.IsPresent)
            {
                if (raw.IsNull)
                {
                    raw.IsValid = true;
                    return raw;
                }

                if (raw.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Array)
                {
                    return reader.ReadArray(names[index]);
                }

                if (raw.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.String)
                {
                    raw.ArrayItems = new global::System.Text.Json.JsonElement[] { raw.ElementValue };
                    raw.ArrayItemPaths = new string[] { raw.Path };
                    raw.IsValid = true;
                }
                else
                {
                    raw.IsValid = false;
                }

                return raw;
            }
        }

        return reader.ReadArray(names[0]);
    }
}
