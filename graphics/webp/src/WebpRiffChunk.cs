using System;

namespace webp;

internal class WebpRiffChunk
{
    internal string FourCc;
    internal byte[] Payload;

    internal WebpRiffChunk(string fourCc, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(fourCc);
        ArgumentNullException.ThrowIfNull(payload);

        FourCc = fourCc;
        Payload = WebpUnknownChunk.CopyBytes(payload);
    }
}
