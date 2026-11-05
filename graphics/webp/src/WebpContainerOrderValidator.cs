using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpContainerOrderValidator
{
    internal static WebpValidationResult Validate(byte[] data, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(data);

        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        WebpValidationResult result = new WebpValidationResult();
        CopyRiffDiagnostics(result, riff);
        if (riff.HasErrors())
        {
            Finish(result);
            return result;
        }

        ValidateChunksInto(result, riff.Chunks, strictness);
        Finish(result);
        return result;
    }

    internal static WebpValidationResult ValidateChunks(List<WebpChunkInfo> chunks, WebpStrictness strictness)
    {
        WebpValidationResult result = new WebpValidationResult();
        ValidateChunksInto(result, chunks, strictness);
        Finish(result);
        return result;
    }

    internal static void ValidateChunksInto(WebpValidationResult result, List<WebpChunkInfo> chunks, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        int highestOrder = -1;
        string highestFourCc = "";
        int index = 0;
        while (index < chunks.Count)
        {
            WebpChunkInfo chunk = chunks[index];
            if (chunk == null)
            {
                throw new ArgumentException("chunk list contains a null chunk", "chunks");
            }

            int order = GetReconstructionOrder(chunk.FourCc);
            if (order >= 0)
            {
                if (order < highestOrder)
                {
                    AddOrderDiagnostic(result, strictness, chunk.Offset, chunk.FourCc, "chunk appears after later reconstruction chunk " + highestFourCc);
                }
                else
                {
                    highestOrder = order;
                    highestFourCc = chunk.FourCc;
                }
            }

            index = index + 1;
        }
    }

    internal static int GetReconstructionOrder(string fourCc)
    {
        if (fourCc == WebpConstants.FourCcVp8XString)
        {
            return 0;
        }

        if (fourCc == WebpConstants.FourCcIccProfileString)
        {
            return 1;
        }

        if (fourCc == WebpConstants.FourCcAnimString)
        {
            return 2;
        }

        if (fourCc == WebpConstants.FourCcAnimationFrameString)
        {
            return 3;
        }

        if (fourCc == WebpConstants.FourCcAlphaString)
        {
            return 4;
        }

        if (fourCc == WebpConstants.FourCcVp8String)
        {
            return 5;
        }

        if (fourCc == WebpConstants.FourCcVp8LString)
        {
            return 5;
        }

        return -1;
    }

    internal static void AddOrderDiagnostic(WebpValidationResult result, WebpStrictness strictness, long offset, string fourCc, string message)
    {
        if (strictness == WebpStrictness.Strict)
        {
            result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, offset, fourCc, message));
        }
        else
        {
            result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Warning, offset, fourCc, message));
        }
    }

    internal static void CopyRiffDiagnostics(WebpValidationResult result, WebpRiffParseResult riff)
    {
        int index = 0;
        while (index < riff.Diagnostics.Count)
        {
            result.Diagnostics.Add(riff.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void Finish(WebpValidationResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
