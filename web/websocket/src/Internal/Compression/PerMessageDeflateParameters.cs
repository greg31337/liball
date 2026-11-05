namespace websocket;

internal class PerMessageDeflateParameters
{
    internal bool Valid;
    internal bool ServerNoContextTakeover;
    internal bool ClientNoContextTakeover;
    internal bool HasServerMaxWindowBits;
    internal int ServerMaxWindowBits = 15;
    internal bool HasClientMaxWindowBits;
    internal bool ClientMaxWindowBitsHasValue;
    internal int ClientMaxWindowBits = 15;
}

internal class PerMessageDeflateDirectionSettings
{
    internal bool Valid;
    internal bool OutgoingContextTakeover = true;
    internal bool IncomingContextTakeover = true;
    internal int OutgoingWindowBits = 15;
    internal int IncomingWindowBits = 15;
}
