namespace cbor;

internal class CborUtf8Result
{
    internal CborReadStatus Status;
    internal string Text = "";
    internal int Consumed;
    internal long TotalBytes;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal long ErrorOffset = -1;
}
