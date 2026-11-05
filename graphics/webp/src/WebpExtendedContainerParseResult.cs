using System.Collections.Generic;

namespace webp;

internal class WebpExtendedContainerParseResult
{
    internal bool IsValid;
    internal bool HasVp8X;
    internal WebpVp8XHeader Vp8X;
    internal WebpMetadata Metadata;
    internal byte[]? AlphaData;
    internal byte[]? ImagePayload;
    internal WebpBitstreamKind BitstreamKind;
    internal uint AnimationBackgroundColorBgra;
    internal ushort AnimationLoopCount;
    internal List<WebpFrame> Frames;
    internal List<WebpUnknownChunk> UnknownChunks;
    internal List<WebpChunkInfo> Chunks;
    internal List<WebpDiagnostic> Diagnostics;

    internal WebpExtendedContainerParseResult()
    {
        IsValid = true;
        HasVp8X = false;
        Vp8X = new WebpVp8XHeader();
        Metadata = new WebpMetadata();
        AlphaData = null;
        ImagePayload = null;
        BitstreamKind = WebpBitstreamKind.Unknown;
        AnimationBackgroundColorBgra = WebpConstants.AnimDefaultBackgroundColorBgra;
        AnimationLoopCount = WebpConstants.AnimDefaultLoopCount;
        Frames = new List<WebpFrame>();
        UnknownChunks = new List<WebpUnknownChunk>();
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
