namespace cbor;

internal class CborValidationNode
{
    internal CborValue Value;
    internal string Path;

    internal CborValidationNode(CborValue value, string path)
    {
        Value = value;
        Path = path;
    }
}
