using System;
using System.Collections.Generic;

namespace webp;

internal class WebpBasicContainerParseResult
{
    internal bool IsValid;
    internal WebpContainerKind ContainerKind;
    internal WebpBitstreamKind BitstreamKind;
    internal int Width;
    internal int Height;
    internal bool HasAlpha;
    internal byte[] ImagePayload;
    internal WebpChunkInfo ImageChunk;
    internal List<WebpChunkInfo> Chunks;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpBasicContainerParseResult()
    {
        IsValid = true;
        ContainerKind = WebpContainerKind.Unknown;
        BitstreamKind = WebpBitstreamKind.Unknown;
        Width = 0;
        Height = 0;
        HasAlpha = false;
        ImagePayload = Array.Empty<byte>();
        ImageChunk = new WebpChunkInfo();
        Chunks = new List<WebpChunkInfo>();
        Diagnostics = new List<WebpDiagnostic>();
    }

    internal bool HasErrors()
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].Severity == WebpDiagnosticSeverity.Error)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
