namespace json;

internal class JsonWriteFrame
{
    JsonContainerKind kind;
    int valueCount;
    int memberCount;
    bool expectsPropertyName;

    internal JsonWriteFrame(JsonContainerKind kind)
    {
        if (kind != JsonContainerKind.Root &&
            kind != JsonContainerKind.Array &&
            kind != JsonContainerKind.Object)
        {
            throw new global::System.ArgumentOutOfRangeException("kind");
        }

        this.kind = kind;
        expectsPropertyName = kind == JsonContainerKind.Object;
    }

    internal JsonContainerKind GetContainerKind()
    {
        return kind;
    }

    internal int GetValueCount()
    {
        return valueCount;
    }

    internal int GetMemberCount()
    {
        return memberCount;
    }

    internal bool CanAcceptPropertyName()
    {
        return kind == JsonContainerKind.Object && expectsPropertyName;
    }

    internal bool CanAcceptValue()
    {
        if (kind == JsonContainerKind.Root)
        {
            return valueCount == 0;
        }

        if (kind == JsonContainerKind.Array)
        {
            return true;
        }

        return !expectsPropertyName;
    }

    internal bool CanCloseContainer()
    {
        return kind == JsonContainerKind.Array ||
            (kind == JsonContainerKind.Object && expectsPropertyName);
    }

    internal void AcceptPropertyName()
    {
        if (!CanAcceptPropertyName())
        {
            throw new global::System.InvalidOperationException(
                "The write frame cannot accept a property name.");
        }

        expectsPropertyName = false;
    }

    internal void AcceptCompletedValue()
    {
        if (!CanAcceptValue())
        {
            throw new global::System.InvalidOperationException(
                "The write frame cannot accept a value.");
        }

        if (kind == JsonContainerKind.Object)
        {
            memberCount++;
            expectsPropertyName = true;
            return;
        }

        valueCount++;
    }
}
