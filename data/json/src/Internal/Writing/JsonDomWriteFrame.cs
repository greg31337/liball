namespace json;

internal class JsonDomWriteFrame
{
    internal JsonValue Value;
    internal global::System.Collections.Generic.List<JsonMember>? Members;
    internal int NextIndex;

    internal JsonDomWriteFrame(
        JsonValue value,
        global::System.Collections.Generic.List<JsonMember>? members)
    {
        Value = value;
        Members = members;
    }
}
