using sdp;

// Protocol messages use CRLF on the wire, regardless of the file's line endings.
string input = File.ReadAllText(SampleFiles.InputPath("protocols/audio-session.sdp")).ReplaceLineEndings("\r\n");
var parsed = new SdpParser().ParseText(input, new SdpParseOptions());
if (!parsed.Success || parsed.Description is null)
{
    throw new InvalidOperationException("SDP parsing failed.");
}

var session = parsed.Description;
var media = session.MediaDescriptions.Single();
Console.WriteLine($"Session: {session.SessionName}");
Console.WriteLine($"Media: {media.MediaType}, port {media.Port}, protocol {media.Protocol}");
Console.WriteLine($"Payload types: {string.Join(", ", media.Formats)}");
Console.WriteLine($"Direction: {new SdpQueryService().ResolveEffectiveDirection(session, 0)}");
string output = new SdpWriter().WriteTextOrThrow(session, new SdpWriteOptions());
Console.WriteLine($"\nSerialized session:\n{output}");
if (!new SdpParser().ParseText(output, new SdpParseOptions()).Success)
{
    throw new InvalidOperationException("Serialized SDP could not be parsed.");
}
