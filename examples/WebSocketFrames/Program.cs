using System.Text;
using websocket;

byte[] payload = Encoding.UTF8.GetBytes("Hello, WebSocket!");

// Client frames must be masked with a fresh key; the server reverses the mask.
var frame = new WebSocketFrameWriteRequest
{
    Header = new WebSocketFrameHeader
    {
        Final = true,
        Opcode = WebSocketOpcode.Text,
        Masked = true,
        PayloadLength = payload.Length
    },
    Payload = payload,
    PayloadCount = payload.Length,
    HasMaskKeySource = true,
    MaskKeySource = new CryptographicWebSocketMaskKeySource()
};

var client = new WebSocketFrameCodec(WebSocketRole.Client, 4096, Array.Empty<WebSocketExtensionNegotiation>());
byte[] bytes = new byte[client.MeasureFrame(frame)];
int written = client.WriteFrame(frame, bytes);
var server = new WebSocketFrameCodec(WebSocketRole.Server, 4096, Array.Empty<WebSocketExtensionNegotiation>());

// Decode locally with the server role; no network handshake is performed.
var decoded = server.ReadFrame(bytes.AsSpan(0, written));
if (decoded.HasError || !decoded.EndOfFrame)
{
    throw new InvalidOperationException("Frame decoding failed.");
}

string text = Encoding.UTF8.GetString(decoded.Payload, decoded.PayloadOffset, decoded.PayloadCount);
Console.WriteLine($"Wire size: {written} bytes; payload: {payload.Length} bytes");
Console.WriteLine(
    $"Opcode: {decoded.Header.Opcode}; final: {decoded.Header.Final}; masked: {decoded.Header.Masked}");
Console.WriteLine($"Decoded: {text}");
if (text != "Hello, WebSocket!")
{
    throw new InvalidOperationException("Frame payload changed.");
}
