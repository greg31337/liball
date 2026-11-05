namespace sdp;

internal class SdpLineReadResult
{
    internal bool HasLine = false;
    internal bool EndOfInput = false;
    internal bool LineLengthExceeded = false;
    internal bool LineCountExceeded = false;
    internal int NextOffset = 0;
    internal int CharacterLength = 0;
    internal int ByteLength = 0;
    internal int CharactersConsumed = 0;
    internal int BytesConsumed = 0;
    internal SdpRawLine Line = new SdpRawLine();
}
